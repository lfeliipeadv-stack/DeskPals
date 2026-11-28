using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>What can be put on a page in the viewer.</summary>
	internal enum MarkKind { Highlight, Text, Redact, Sign }

	/// <summary>
	/// One mark on a page: its kind, where (0..1 of the page as it is before the user's turn, so turning the page turns
	/// the mark with it), its text, the picture of a signature.
	/// </summary>
	internal sealed record Mark(MarkKind Kind, RectangleF Box, string Text = null, string Image = null)
	{
		public string Key => $"{(int)Kind}:{Box.X:0.####},{Box.Y:0.####},{Box.Width:0.####},{Box.Height:0.####}:{Text}:{Image}";
	}

	/// <summary>
	/// The marks of the viewer: turned with their page, written to the job's options ("marks", JSON) and back, drawn on
	/// the saved pages (highlight, text, signature), and the black boxes burnt in: a page with one is drawn again as a
	/// picture, so what is under the box is gone, not only covered.
	/// </summary>
	internal static class Marks
	{
		/// <summary>A box of a page (0..1) as it is after turning the page by these degrees (clockwise).</summary>
		public static RectangleF Turn(RectangleF r, int degrees) => (((degrees % 360) + 360) % 360) switch
		{
			90 => new RectangleF(1 - r.Bottom, r.X, r.Height, r.Width),
			180 => new RectangleF(1 - r.Right, 1 - r.Bottom, r.Width, r.Height),
			270 => new RectangleF(r.Y, 1 - r.Right, r.Height, r.Width),
			_ => r,
		};

		/// <summary>The colours of the highlighter (the mark's text names it): yellow, green, pink, blue.</summary>
		public static (int r, int g, int b) Highlight(string name) => name switch
		{
			"green" => (90, 220, 90), "pink" => (255, 110, 190), "blue" => (90, 170, 255), _ => (255, 221, 0),
		};
		
		/// <summary>One mark as the job gets it: the page's place in the new order, and its box on the saved (turned) page.</summary>
		public sealed class Placed
		{
			public int at { get; set; }
			public int kind { get; set; }
			public float x { get; set; }
			public float y { get; set; }
			public float w { get; set; }
			public float h { get; set; }
			public string text { get; set; }
			public string image { get; set; }
			public RectangleF Box => new(x, y, w, h);
			public MarkKind Kind => (MarkKind)kind;
		}

		public static string Write(IEnumerable<Placed> marks) => JsonSerializer.Serialize(marks.ToList());

		public static List<Placed> Read(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new();
			try { return (JsonSerializer.Deserialize<List<Placed>>(json) ?? new()).Where(m => Enum.IsDefined(typeof(MarkKind), m.kind) && m.w > 0 && m.h > 0).ToList(); }
			catch (Exception) { return new(); }
		}

		/// <summary>The marks of one page drawn (the page as the reader sees it: width and height in points).</summary>
		public static void Draw(XGraphics g, double w, double h, IEnumerable<Placed> marks, StampText writer)
		{
			foreach (var m in marks)
			{
				double x = m.x * w, y = m.y * h, bw = m.w * w, bh = m.h * h;
				switch (m.Kind)
				{
					case MarkKind.Highlight:
						var (hr, hg, hb) = Highlight(m.text);
						g.DrawRectangle(new XSolidBrush(XColor.FromArgb(96, hr, hg, hb)), x, y, bw, bh);
						break;
					case MarkKind.Redact:
						g.DrawRectangle(XBrushes.Black, x, y, bw, bh);
						break;
					case MarkKind.Text:
						if (string.IsNullOrWhiteSpace(m.text)) break;
						// one or more lines, each as tall as its share of the box
						var lines = m.text.Replace("\r", "").Split('\n');
						double lineH = bh / lines.Length;
						double size = Math.Clamp(lineH * 0.72, 4, 72);
						// a long text in a narrow box gets smaller letters, so it stays inside
						double width = Math.Max(1, lines.Max(l => writer.Width(g, l.Length == 0 ? " " : l, size, false)));
						if (width > bw) size = Math.Max(4, size * bw / width);
						for (int k = 0; k < lines.Length; k++)
							if (lines[k].Length > 0) writer.Draw(g, lines[k], size, false, XColor.FromArgb(255, 20, 20, 20), new XPoint(x, y + lineH * (k + 0.5)), XStringFormats.CenterLeft);
						break;
					case MarkKind.Sign:
						if (string.IsNullOrEmpty(m.image) || !File.Exists(m.image)) break;
						using (var img = XImage.FromFile(m.image))
						{
							double k = Math.Min(bw / img.PointWidth, bh / img.PointHeight);
							double iw = img.PointWidth * k, ih = img.PointHeight * k;
							g.DrawImage(img, x + (bw - iw) / 2, y + (bh - ih) / 2, iw, ih);
						}
						break;
				}
			}
		}

		/// <summary>
		/// The pages with a black box drawn again as pictures (200 dpi), the boxes painted in, into a new temporary PDF;
		/// the other pages are copied as they are. Gives the new file (the old one is deleted), or the same when no page
		/// has a box.
		/// </summary>
		public static async Task<string> Burn(string pdf, List<Placed> marks, CancellationToken cancel)
		{
			var pages = marks.Where(m => m.Kind == MarkKind.Redact).GroupBy(m => m.at).ToDictionary(g => g.Key, g => g.ToList());
			if (pages.Count == 0) return pdf;
			var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdf));
			var win = await WinPdf.PdfDocument.LoadFromFileAsync(file);
			string output = Jobs.Temp(".pdf");
			var pictures = new List<string>();
			try
			{
				using (var src = PdfReader.Open(pdf, PdfDocumentOpenMode.Import))
				using (var doc = new PdfDocument())
				{
					doc.Info.Creator = "DeskPals Tools";
					for (int i = 0; i < src.PageCount; i++)
					{
						cancel.ThrowIfCancellationRequested();
						if (!pages.TryGetValue(i, out var boxes)) { doc.AddPage(src.Pages[i]); continue; }
						using var page = win.GetPage((uint)i);
						double wPt = page.Size.Width * 72 / 96, hPt = page.Size.Height * 72 / 96;
						string png = Jobs.Temp(".png");
						pictures.Add(png);
						await Picture(page, png, boxes);
						var p = doc.AddPage();
						p.Width = XUnit.FromPoint(wPt);
						p.Height = XUnit.FromPoint(hPt);
						using var g = XGraphics.FromPdfPage(p);
						using var img = XImage.FromFile(png);
						g.DrawImage(img, 0, 0, wPt, hPt);
					}
					doc.Save(output);
				}
				try { File.Delete(pdf); } catch (Exception) { }
				return output;
			}
			catch (Exception)
			{
				try { File.Delete(output); } catch (Exception) { }
				throw;
			}
			finally { foreach (var p in pictures) try { File.Delete(p); } catch (Exception) { } }
		}

		/// <summary>The page drawn by Windows at 200 dpi, with its black boxes painted over, as a PNG.</summary>
		static async Task Picture(WinPdf.PdfPage page, string png, List<Placed> boxes)
		{
			using var ms = new InMemoryRandomAccessStream();
			await page.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions
			{
				DestinationWidth = (uint)Math.Max(1, Math.Round(page.Size.Width * 200 / 96)),
				BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255),
			});
			using var s = ms.AsStreamForRead();
			using var loaded = new Bitmap(s);
			using var bmp = new Bitmap(loaded.Width, loaded.Height, PixelFormat.Format24bppRgb);
			using (var g = Graphics.FromImage(bmp))
			{
				g.DrawImage(loaded, 0, 0, loaded.Width, loaded.Height);
				foreach (var b in boxes)
					g.FillRectangle(Brushes.Black, (float)Math.Floor(b.x * bmp.Width) - 1, (float)Math.Floor(b.y * bmp.Height) - 1, (float)Math.Ceiling(b.w * bmp.Width) + 2, (float)Math.Ceiling(b.h * bmp.Height) + 2);
			}
			bmp.Save(png, ImageFormat.Png);
		}

		// ------------------------------------------------------------------ the signature

		/// <summary>The signature kept for next time (a see-through PNG), or null.</summary>
		public static string Signature
		{
			get { string f = Path.Combine(Places.Folder, "signature.png"); return File.Exists(f) ? f : null; }
		}

		/// <summary>Keeps a signature (drawn or a picture): cut to what has ink, the paper made see-through.</summary>
		public static string KeepSignature(Bitmap drawn)
		{
			string f = Path.Combine(Places.Folder, "signature.png");
			using var clean = Ink(drawn);
			clean.Save(f, ImageFormat.Png);
			return f;
		}

		/// <summary>The ink of a picture: light paper see-through, cut to the ink with a small margin.</summary>
		internal static Bitmap Ink(Bitmap src)
		{
			int w = src.Width, h = src.Height;
			using var argb = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			using (var g = Graphics.FromImage(argb)) g.DrawImage(src, 0, 0, w, h);
			var data = argb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			var px = new byte[data.Stride * h];
			Marshal.Copy(data.Scan0, px, 0, px.Length);
			int minX = w, minY = h, maxX = -1, maxY = -1;
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					int i = y * data.Stride + x * 4;
					int light = (px[i] + px[i + 1] + px[i + 2]) / 3;
					// paper (light) becomes see-through, ink keeps its colour, what is between is half
					int a = Math.Min(px[i + 3], Math.Clamp((235 - light) * 255 / 120, 0, 255));
					px[i + 3] = (byte)a;
					if (a > 40) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
				}
			Marshal.Copy(px, 0, data.Scan0, px.Length);
			argb.UnlockBits(data);
			if (maxX < 0) return new Bitmap(argb);
			int m = 6;
			var crop = Rectangle.FromLTRB(Math.Max(0, minX - m), Math.Max(0, minY - m), Math.Min(w, maxX + m + 1), Math.Min(h, maxY + m + 1));
			return argb.Clone(crop, PixelFormat.Format32bppArgb);
		}
	}
}
