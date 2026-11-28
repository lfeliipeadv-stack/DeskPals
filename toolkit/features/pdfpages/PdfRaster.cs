using System.Runtime.InteropServices.WindowsRuntime;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// The stronger way to make a scanned PDF smaller, for what <see cref="PdfCompress"/> cannot shrink (CMYK or palette
	/// pictures, JBIG2 or CCITT scans, pictures with masks…): every page drawn again by Windows at 150 dpi and kept as
	/// one JPEG (grey when the page has no colour). Only for PDFs whose pages are pictures: a page with real text would
	/// lose it, so such a PDF is never redrawn.
	/// </summary>
	internal static class PdfRaster
	{
		const double Dpi = 150, Quality = 0.6;
		/// <summary>A page with at least this many letters has real text.</summary>
		const int TextLetters = 25;

		/// <summary>Are all the pages pictures (no page with real text)? A PDF that cannot be read is not.</summary>
		public static bool ScanOnly(string pdf)
		{
			try
			{
				using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
				foreach (var page in doc.GetPages())
					if (page.Letters.Count(l => !string.IsNullOrWhiteSpace(l.Value)) >= TextLetters) return false;
				return doc.NumberOfPages > 0;
			}
			catch (Exception) { return false; }
		}

		/// <summary>A new temporary PDF with every page redrawn, or null when Windows cannot draw it.</summary>
		public static async Task<string> Make(string pdf, Action<double> progress, CancellationToken cancel)
		{
			var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdf));
			WinPdf.PdfDocument src;
			try { src = await WinPdf.PdfDocument.LoadFromFileAsync(file); }
			catch (Exception) { return null; }
			string temp = Jobs.Temp(".pdf");
			var pictures = new List<string>();
			try
			{
				using var doc = new PdfDocument();
				doc.Info.Creator = "DeskPals Tools";
				for (uint i = 0; i < src.PageCount; i++)
				{
					cancel.ThrowIfCancellationRequested();
					progress?.Invoke(i / (double)src.PageCount);
					using var page = src.GetPage(i);
					// the page's size as the reader sees it (turned), in points
					double wPt = page.Size.Width * 72 / 96, hPt = page.Size.Height * 72 / 96;
					string jpg = Jobs.Temp(".jpg");
					pictures.Add(jpg);
					await Draw(page, jpg, (uint)Math.Max(1, Math.Round(page.Size.Width * Dpi / 96)));
					var p = doc.AddPage();
					p.Width = XUnit.FromPoint(wPt);
					p.Height = XUnit.FromPoint(hPt);
					using var g = XGraphics.FromPdfPage(p);
					using var img = XImage.FromFile(jpg);
					g.DrawImage(img, 0, 0, wPt, hPt);
				}
				doc.Options.CompressContentStreams = true;
				doc.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;
				doc.Save(temp);
				return temp;
			}
			catch (Exception)
			{
				try { File.Delete(temp); } catch (Exception) { }
				if (cancel.IsCancellationRequested) throw;
				return null;
			}
			finally { foreach (var p in pictures) try { File.Delete(p); } catch (Exception) { } }
		}

		/// <summary>One page as a JPEG this wide: grey when it has (almost) no colour, which is a third of the size.</summary>
		static async Task Draw(WinPdf.PdfPage page, string jpg, uint width)
		{
			using var ms = new InMemoryRandomAccessStream();
			await page.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions { DestinationWidth = width, BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255) });
			var decoder = await BitmapDecoder.CreateAsync(ms);
			var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
			byte[] px = data.DetachPixelData();
			uint w = decoder.PixelWidth, h = decoder.PixelHeight;
			bool gray = Gray(px);
			using var output = File.Create(jpg);
			var props = new BitmapPropertySet { { "ImageQuality", new BitmapTypedValue(Quality, Windows.Foundation.PropertyType.Single) } };
			var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output.AsRandomAccessStream(), props);
			if (gray)
			{
				var g = new byte[w * h];
				for (int i = 0, j = 0; j < g.Length; i += 4, j++) g[j] = px[i + 1];
				try { encoder.SetPixelData(BitmapPixelFormat.Gray8, BitmapAlphaMode.Ignore, w, h, Dpi, Dpi, g); await encoder.FlushAsync(); return; }
				catch (Exception)
				{
					// some encoders refuse grey: then in colour
					output.SetLength(0);
					encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output.AsRandomAccessStream(), props);
				}
			}
			encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, w, h, Dpi, Dpi, px);
			await encoder.FlushAsync();
		}

		/// <summary>No pixel (of a sample) far from grey.</summary>
		static bool Gray(byte[] px)
		{
			int step = Math.Max(4, px.Length / 4 / 200_000 * 4);
			for (int i = 0; i + 2 < px.Length; i += step)
			{
				int b = px[i], g = px[i + 1], r = px[i + 2];
				if (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 24) return false;
			}
			return true;
		}
	}
}
