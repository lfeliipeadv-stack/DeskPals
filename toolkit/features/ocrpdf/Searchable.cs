using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using static DeskPalsTools.Jobs;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// "searchable": a scanned PDF (or pictures) becomes a PDF that looks the same but whose text can be found (Ctrl+F) and
	/// copied. Each page is drawn by Windows, read by the OCR (words with their boxes) and the words are written, invisible,
	/// at their place over the ORIGINAL page (nothing of the page is redrawn or lost). Pages that already have real text are
	/// left as they are. Also the "use Tesseract" setting of the window.
	/// </summary>
	internal static class Searchable
	{
		/// <summary>A page with at least this many letters already has real text (a stamp or a page number alone does not count).</summary>
		public const int MinLetters = 25;

		static bool AllFiles(IList<string> f) => f.Count > 0 && f.All(File.Exists);

		[ModuleInitializer]
		internal static void Init()
		{
			ScriptFonts.Install();
			Register(new JobDef
			{
				Id = "searchable", Emoji = "🔍",
				// before "ocr" (20) for a PDF; after it for pictures (the pictures' main jobs come first)
				Rank = f => !AllFiles(f) ? -1 : f.All(IsPdf) ? 18 : f.All(IsImage) ? 21 : -1,
				Run = Run,
			});
			Prefs.Register(new Prefs.Pref
			{
				Key = OcrText.Key, TextKey = "ocrpdf.pref.tesseract", HintKey = "ocrpdf.pref.tesseractHint", Order = 60,
				Get = () => OcrText.Choice() == "tesseract",
				Set = on => OcrText.Choose(on),
				// which languages it reads (the program's and English unless chosen otherwise)
				ButtonKey = "ocrpdf.pref.languages",
				Click = owner => TessLanguages.Pick(owner),
			});
			OcrPdfTests.Register();
		}

		static async Task Run(JobContext c)
		{
			var notices = new HashSet<string>();
			void Notice(string s) { lock (notices) if (notices.Add(s)) c.Errors.Add(s); }
			int words = 0, pages = 0;
			if (c.Files.All(IsImage))
			{
				// pictures: one PDF of them first (one page each), then its text
				string temp = Temp(".pdf");
				try
				{
					c.Report(0, T.S("doing.topdf"));
					await PdfWork.FromImages(c.Files, temp);
					var (w, p) = await Make(temp, OutName(c.Files[0], ".pdf", T.S("ocrpdf.name")), c, 0, 1, Notice);
					words += w; pages += p;
				}
				finally { try { File.Delete(temp); } catch (Exception) { } }
			}
			else
				await c.Each(async (file, i) =>
				{
					var (w, p) = await Make(file, OutName(file, ".pdf", T.S("ocrpdf.name")), c, i / (double)c.Files.Count, 1.0 / c.Files.Count, Notice);
					words += w; pages += p;
				});
			if (words > 0) c.Preview = T.S("ocrpdf.preview", words, pages);
		}

		/// <summary>
		/// The searchable copy of one PDF, saved as <paramref name="output"/> (the original is only read). Gives the words
		/// written and the pages read. Progress goes from <paramref name="from"/> over <paramref name="share"/> of the bar.
		/// </summary>
		internal static async Task<(int words, int pages)> Make(string pdf, string output, JobContext c, double from, double share, Action<string> notice)
		{
			// the slow reading and writing of the file away from the window, so it keeps answering (and Cancel too)
			var hasText = await Task.Run(() => PagesWithText(pdf), c.Cancel);
			using var doc = await Task.Run(() => PdfReader.Open(pdf, PdfDocumentOpenMode.Modify), c.Cancel);
			if (doc.PageCount == 0) throw new InvalidOperationException(T.S("err.noPages"));
			var todo = Enumerable.Range(0, doc.PageCount).Where(i => !hasText.Contains(i)).ToList();
			if (todo.Count == 0) throw new InvalidOperationException(T.S("ocrpdf.already"));

			using var engine = await OcrText.Open((p, s) => c.Report(from + share * 0.1 * p, s), c.Cancel, notice);
			using var input = File.OpenRead(pdf);
			var drawn = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			int words = 0;
			for (int n = 0; n < todo.Count; n++)
			{
				c.Cancel.ThrowIfCancellationRequested();
				int i = todo[n];
				c.Report(from + share * (0.1 + 0.9 * n / todo.Count), T.S("doing.page", i + 1, doc.PageCount));
				OcrImage image;
				using (var page = drawn.GetPage((uint)i))
					image = await OcrText.FromPdfPage(page, engine is WindowsOcr ? 250 : 300, engine.MaxSide);
				var read = await engine.Read(image, c.Cancel);
				words += WriteText(doc.Pages[i], read);
			}
			if (words == 0) throw new InvalidOperationException(T.S("ocrpdf.noWords"));
			// saved apart first: a half-written file never takes the name
			string temp = Temp(".pdf");
			try
			{
				doc.Info.Creator = "DeskPals Tools";
				c.Cancel.ThrowIfCancellationRequested();
				await Task.Run(() => { doc.Save(temp); File.Move(temp, output, false); });
			}
			finally { try { File.Delete(temp); } catch (Exception) { } }
			c.Outputs.Add(output);
			return (words, todo.Count);
		}

		/// <summary>
		/// The pages (from 0) that already have real text, read by PdfPig: a page with any invisible text (an earlier OCR's,
		/// ours or another program's) or with at least <see cref="MinLetters"/> letters, so the text is never doubled.
		/// A PDF PdfPig cannot read: none (every page is read).
		/// </summary>
		public static HashSet<int> PagesWithText(string pdf)
		{
			var set = new HashSet<int>();
			try
			{
				using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
				foreach (var page in doc.GetPages())
				{
					var letters = page.Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();
					if (letters.Count >= MinLetters || letters.Any(l => l.RenderingMode == UglyToad.PdfPig.Core.TextRenderingMode.Neither))
						set.Add(page.Number - 1);
				}
			}
			catch (Exception) { set.Clear(); }
			return set;
		}

		// ------------------------------------------------------------------ the invisible text

		/// <summary>
		/// The words of the OCR written, invisible, over one page: placed where they are seen, as tall as their line and
		/// stretched to the width of their box, so a search or a selection marks the right place. Gives how many.
		/// </summary>
		public static int WriteText(PdfPage page, OcrPage read)
		{
			if (read.Words.Count == 0 || read.Width <= 0 || read.Height <= 0) return 0;
			int rotate = ((page.Rotate % 360) + 360) % 360;
			var box = ShownBox(page);
			// what is seen (turned): its width and height in points, and the size of a pixel of the picture in points
			bool sideways = rotate is 90 or 270;
			double shownW = sideways ? box.Height : box.Width, shownH = sideways ? box.Width : box.Height;
			double kx = shownW / read.Width, ky = shownH / read.Height;

			// PDFsharp draws on the unturned page, from the top-left of the MediaBox: the words are drawn as they are seen and
			// the turning (and the CropBox's place) is put around them afterwards, as one "cm" of our own
			page.Rotate = 0;
			page.Orientation = PdfSharp.PageOrientation.Portrait;
			double mediaH = page.Height.Point;
			var before = page.Contents.Elements.ToList();
			int count;
			try
			{
				using (var g = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
					count = DrawWords(g, read, kx, ky);
				Finish(page, before, ShownToPage(rotate, box, mediaH));
			}
			finally { page.Rotate = rotate; }
			return count;
		}

		/// <summary>Each line as tall as its letters, each word stretched to its box (sizes in points of what is seen).</summary>
		static int DrawWords(XGraphics g, OcrPage read, double kx, double ky)
		{
			int count = 0;
			var fonts = new Dictionary<(string, double), XFont>();
			foreach (var line in read.Words.GroupBy(w => w.Line))
			{
				// the line's box goes from the top of the tallest letter to the bottom of the lowest one
				double top = line.Min(w => w.Y) * ky, bottom = line.Max(w => w.Y + w.H) * ky;
				double size = Math.Round(Math.Max(1, (bottom - top) / 0.8) * 2) / 2;
				double baseline = bottom - size * 0.1;
				foreach (var w in line)
				{
					// the first font of Windows with all its letters; none: left out (empty boxes would be found by no search)
					var font = ScriptFonts.For(w.Text, size, fonts);
					if (font == null) continue;
					double measured = g.MeasureString(w.Text, font).Width;
					if (measured <= 0 || w.W <= 0) continue;
					var state = g.Save();
					g.TranslateTransform(w.X * kx, baseline);
					g.ScaleTransform(w.W * kx / measured, 1);
					g.DrawString(w.Text, font, XBrushes.Black, 0, 0, XStringFormats.BaseLineLeft);
					g.Restore(state);
					count++;
				}
			}
			return count;
		}

		/// <summary>
		/// The content just drawn (only ours: the page's own is not touched) put in place with <paramref name="place"/> and
		/// made invisible: text render mode 3, what every OCR uses, is there for search and copy but paints nothing.
		/// PDFsharp has no way to say either, so "q … cm" goes around it and "3 Tr" after each of its "BT".
		/// </summary>
		static void Finish(PdfPage page, List<PdfItem> before, XMatrix place)
		{
			var inv = System.Globalization.CultureInfo.InvariantCulture;
			string cm = string.Join(" ", new[] { place.M11, place.M12, place.M21, place.M22, place.OffsetX, place.OffsetY }.Select(v => v.ToString("0.#####", inv)));
			foreach (var item in page.Contents.Elements.ToList())
			{
				if (before.Contains(item)) continue;
				var content = (item as PdfSharp.Pdf.Advanced.PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
				if (content?.Stream == null) continue;
				string ops = System.Text.Encoding.Latin1.GetString(content.Stream.UnfilteredValue);
				// "BT" alone as an operator (the words are <hex> strings, which never hold a "T")
				string hidden = System.Text.RegularExpressions.Regex.Replace(ops, @"(?<=^|\s)BT(?=\s)", "BT 3 Tr");
				if (hidden == ops) continue;
				content.Elements.Remove("/Filter");
				content.Elements.Remove("/DecodeParms");
				content.Stream.Value = System.Text.Encoding.Latin1.GetBytes("q " + cm + " cm\n" + hidden + "\nQ\n");
			}
		}

		/// <summary>The part of the page that is shown: the CropBox inside the MediaBox (the MediaBox when there is no CropBox).</summary>
		static XRect ShownBox(PdfPage page)
		{
			var media = page.MediaBox.ToXRect();
			if (!page.Elements.ContainsKey("/CropBox")) return media;
			var crop = page.CropBox.ToXRect();
			var both = XRect.Intersect(crop, media);
			return both.IsEmpty || both.Width <= 0 || both.Height <= 0 ? media : both;
		}

		/// <summary>
		/// Where the words drawn as seen must go, in PDF units. PDFsharp puts a point (x, y) seen from the top-left at
		/// (x, mediaH − y) of the unturned page; this matrix moves that to its real place. /Rotate turns the page clockwise
		/// when it is shown, so the seen top-left corner is a different corner of the CropBox for each angle, and the seen
		/// x runs up (90), left (180) or down (270).
		/// </summary>
		public static XMatrix ShownToPage(int rotate, XRect box, double mediaH)
		{
			// the box in PDF units (y up): left, bottom, right, top
			double x0 = box.X, y0 = box.Y, x1 = box.X + box.Width, y1 = box.Y + box.Height;
			return rotate switch
			{
				// X = x0 + seen y, Y = y0 + seen x
				90 => new XMatrix(0, 1, -1, 0, x0 + mediaH, y0),
				// X = x1 − seen x, Y = y0 + seen y
				180 => new XMatrix(-1, 0, 0, -1, x1, y0 + mediaH),
				// X = x1 − seen y, Y = y1 − seen x
				270 => new XMatrix(0, -1, 1, 0, x1 - mediaH, y1),
				// X = x0 + seen x, Y = y1 − seen y
				_ => new XMatrix(1, 0, 0, 1, x0, y1 - mediaH),
			};
		}
	}
}
