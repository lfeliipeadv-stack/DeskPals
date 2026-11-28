using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using Windows.Media.Ocr;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// Pictures with the codecs of Windows (JPEG, PNG, BMP, GIF, TIFF, and WebP/HEIC when their extensions are there):
	/// another format, smaller, and white behind what was transparent when it becomes a JPEG.
	/// </summary>
	internal static class ImageWork
	{
		/// <summary>Opens a picture; a format Windows cannot read (HEIC without its extension…) gets a sentence a person understands.</summary>
		public static async Task<BitmapDecoder> Decoder(Stream input, string file)
		{
			try { return await BitmapDecoder.CreateAsync(input.AsRandomAccessStream()); }
			catch (Exception ex) when (ex is not (IOException or UnauthorizedAccessException or OperationCanceledException))
			{
				throw new InvalidOperationException(HeicHelp.Explain(file) ?? T.S("docs.scan.noPicture"));
			}
		}

		public static async Task Convert(string src, string dst, bool png, int maxSide)
		{
			using var input = File.OpenRead(src);
			var decoder = await Decoder(input, src);
			uint w = decoder.OrientedPixelWidth, h = decoder.OrientedPixelHeight;
			var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(),
				ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
			byte[] pixels = data.DetachPixelData();
			if (!png)
				// a JPEG has no transparency: what was see-through becomes white (premultiplied: colour + (255 - alpha))
				for (int i = 0; i < pixels.Length; i += 4)
				{
					int a = pixels[i + 3];
					if (a == 255) continue;
					pixels[i] = (byte)Math.Min(255, pixels[i] + 255 - a);
					pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] + 255 - a);
					pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] + 255 - a);
					pixels[i + 3] = 255;
				}
			using var output = File.Create(dst);
			var props = new BitmapPropertySet();
			if (!png) props.Add("ImageQuality", new BitmapTypedValue(0.85, Windows.Foundation.PropertyType.Single));
			var encoder = await BitmapEncoder.CreateAsync(png ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId, output.AsRandomAccessStream(), props);
			encoder.SetPixelData(BitmapPixelFormat.Bgra8, png ? BitmapAlphaMode.Premultiplied : BitmapAlphaMode.Ignore, w, h, 96, 96, pixels);
			if (maxSide > 0 && Math.Max(w, h) > maxSide)
			{
				double k = maxSide / (double)Math.Max(w, h);
				encoder.BitmapTransform.ScaledWidth = (uint)Math.Max(1, Math.Round(w * k));
				encoder.BitmapTransform.ScaledHeight = (uint)Math.Max(1, Math.Round(h * k));
				encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
			}
			await encoder.FlushAsync();
		}
	}

	/// <summary>
	/// Text from pictures and scanned PDFs with the OCR of Windows (no download, works offline, in the languages Windows
	/// has: Portuguese on a Windows in Portuguese). The pages of a PDF are drawn by Windows itself and read one by one.
	/// </summary>
	internal static class OcrWork
	{
		static OcrEngine Engine()
		{
			var e = OcrEngine.TryCreateFromUserProfileLanguages();
			if (e == null)
				foreach (var lang in OcrEngine.AvailableRecognizerLanguages)
					if ((e = OcrEngine.TryCreateFromLanguage(lang)) != null) break;
			return e ?? throw new InvalidOperationException(T.S("err.noOcr"));
		}

		/// <summary>The OCR languages there are on this computer ("pt-BR, en-US").</summary>
		public static string Languages() => string.Join(", ", OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag));

		public static async Task<string> ReadImage(string file)
		{
			using var input = File.OpenRead(file);
			var decoder = await ImageWork.Decoder(input, file);
			return await Read(decoder);
		}

		static async Task<string> Read(BitmapDecoder decoder)
		{
			// the OCR takes pictures up to a size: a bigger one is made smaller first
			uint max = OcrEngine.MaxImageDimension;
			var transform = new BitmapTransform();
			uint w = decoder.PixelWidth, h = decoder.PixelHeight;
			if (Math.Max(w, h) > max)
			{
				double k = max / (double)Math.Max(w, h);
				transform.ScaledWidth = (uint)(w * k); transform.ScaledHeight = (uint)(h * k);
				transform.InterpolationMode = BitmapInterpolationMode.Fant;
			}
			using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform,
				ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
			var result = await Engine().RecognizeAsync(bitmap);
			return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text));
		}

		public static async Task<string> ReadPdf(string file, Action<double, string> progress, CancellationToken cancel)
		{
			using var input = File.OpenRead(file);
			var pdf = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			var sb = new System.Text.StringBuilder();
			for (uint i = 0; i < pdf.PageCount; i++)
			{
				cancel.ThrowIfCancellationRequested();
				progress?.Invoke(i / (double)pdf.PageCount, T.S("doing.page", i + 1, pdf.PageCount));
				using var page = pdf.GetPage(i);
				using var ms = new InMemoryRandomAccessStream();
				// about 200 dpi: small letters are read well, the OCR stays quick
				double width = page.Size.Width * 200 / 96;
				await page.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions { DestinationWidth = (uint)Math.Min(OcrEngine.MaxImageDimension, Math.Max(800, width)) });
				var decoder = await BitmapDecoder.CreateAsync(ms);
				string text = await Read(decoder);
				if (pdf.PageCount > 1) sb.AppendLine("— " + T.S("name.page", i + 1) + " —");
				sb.AppendLine(text);
				sb.AppendLine();
			}
			return sb.ToString().TrimEnd();
		}
	}

	/// <summary>
	/// Audio (and the sound of a video) to MP3 with Windows' own converter: OGG and OPUS (WhatsApp) need the "Web Media
	/// Extensions" of Windows, which most computers have.
	/// </summary>
	internal static class MediaWork
	{
		public static async Task ToMp3(string src, string dst, Action<double> progress, CancellationToken cancel)
		{
			var source = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(src));
			File.WriteAllBytes(dst, Array.Empty<byte>());
			var target = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(dst));
			var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
			var transcoder = new MediaTranscoder();
			var prepared = await transcoder.PrepareFileTranscodeAsync(source, target, profile);
			if (!prepared.CanTranscode)
			{
				try { File.Delete(dst); } catch (Exception) { }
				throw new InvalidOperationException(prepared.FailureReason == TranscodeFailureReason.CodecNotFound ? T.S("err.codec", Jobs.Ext(src)) : T.S("err.cantConvert", prepared.FailureReason));
			}
			var op = prepared.TranscodeAsync();
			op.Progress = (_, p) => progress?.Invoke(p / 100.0);
			using (cancel.Register(() => op.Cancel()))
			{
				try { await op; }
				catch (Exception) { try { File.Delete(dst); } catch (Exception) { } throw; }
			}
		}
	}
}
