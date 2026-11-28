using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PdfSharp.Drawing;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of the "docs" jobs on files made here: photos of a paper (a sheet with big words, turned, on a dark wooden
	/// table, with uneven light and a shadow), a PDF with real text for Word, and a HEIC when Windows can write one.
	/// </summary>
	internal static class DocsTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("docs plan", Plan);
			DevTests.Add("docs cleanscan", CleanOnePhoto);
			DevTests.Add("docs cleanscan many", CleanManyPhotos);
			DevTests.Add("docs cleanscan modes", Modes);
			DevTests.Add("docs cleanscan pictures", PicturesOnPage);
			DevTests.Add("docs cleanscan sideways", Sideways);
			DevTests.Add("docs cleanscan upside down", UpsideDown);
			DevTests.Add("docs in use", InUse);
			DevTests.Add("docs toword", ToWordTest);
			DevTests.Add("docs heic", Heic);
		}

		static readonly string[] Words = { "INVOICE", "DESKPALS", "SCANNER", "PAPER", "KITTEN" };

		// ------------------------------------------------------------------ the list of jobs

		static Task Plan(DevTests.Ctx t)
		{
			string pic = t.Picture("a.png", "HELLO", Color.White);
			string pic2 = t.Picture("b.jpg", "WORLD", Color.White);
			string pdf = Path.Combine(t.Dir, "x.pdf"); File.WriteAllText(pdf, "");
			string heic = Path.Combine(t.Dir, "x.heic"); File.WriteAllText(heic, "");
			string[] Ids(params string[] f) => Jobs.Plan(f).Select(j => j.Id).ToArray();
			var one = Ids(pic);
			t.Check("plan: a picture → \"Convert to PDF\" then the clean scan", one.Length > 1 && one[0] == "topdf" && one[1] == "cleanscan", string.Join(",", one));
			t.Check("plan: many pictures → a clean scan in one PDF is offered", Ids(pic, pic2).Contains("cleanscan"), string.Join(",", Ids(pic, pic2)));
			t.Check("plan: a PDF → Word is offered", Ids(pdf).Contains("toword"), string.Join(",", Ids(pdf)));
			t.Check("plan: two PDFs → no Word (one at a time)", !Ids(pdf, pdf).Contains("toword"));
			t.Check("plan: a PDF and a picture → no clean scan", !Ids(pdf, pic).Contains("cleanscan"));
			bool can = HeicHelp.CanDecode();
			t.Info("heic: Windows " + (can ? "can" : "cannot") + " open HEIC photos here");
			var h = Ids(heic);
			t.Check("plan: an iPhone photo → the HEIC help only when Windows cannot open it", can ? !h.Contains("heichelp") : h.FirstOrDefault() == "heichelp", string.Join(",", h));
			t.Check("plan: no HEIC help for other pictures", !one.Contains("heichelp"));
			return Task.CompletedTask;
		}

		// ------------------------------------------------------------------ clean scan

		static async Task CleanOnePhoto(DevTests.Ctx t)
		{
			string photo = Photo(Path.Combine(t.Dir, "photo.jpg"), Words, 4000, 3000, 6, 1, shadow: 0.8, shadowOnText: true);
			t.Check("scan: the test photo has a dark table around the sheet", await BorderBrightness(photo) < 110);
			var sw = Stopwatch.StartNew();
			var r = await Jobs.Run("cleanscan", new[] { photo }, null);
			sw.Stop();
			t.Check("scan: a 12-megapixel photo becomes a one-page PDF", r.ok && r.outputs.Count == 1 && PdfWork.Pages(r.outputs[0]) == 1, r.message);
			if (!r.ok) return;
			t.Info($"scan: 12 MP in {sw.ElapsedMilliseconds} ms, PDF {new FileInfo(r.outputs[0]).Length / 1024} KB");
			t.Check("scan: quick enough (under 15 s)", sw.Elapsed < TimeSpan.FromSeconds(15), sw.ElapsedMilliseconds + " ms");
			t.Check("scan: the name says scan, next to the photo", Path.GetDirectoryName(r.outputs[0]) == t.Dir && Path.GetFileName(r.outputs[0]).StartsWith("photo (", StringComparison.Ordinal), r.outputs[0]);

			// the page itself: cut to the sheet (no table at the edges), white paper
			var page = await RenderPdf(r.outputs[0], 0, 600);
			double border = Border(page), paper = Median(page);
			t.Check("scan: the table is gone (light edges)", border > 225, $"edge brightness {border:0}");
			t.Check("scan: the paper is white", paper >= 245, $"median {paper:0}");
			double dark = page.Data.Where((_, i) => i % 4 == 1).Count(v => v < 100) / (double)(page.W * page.H);
			t.Check("scan: the ink is still there (dark words)", dark > 0.01 && dark < 0.35, $"dark share {dark:P1}");

			// straightened and cut: the cleaned picture has the shape of the sheet (A4)
			string img = await CleanScan.CleanOne(photo, ScanImage.Mode.Color, default);
			try
			{
				var size = DevTests.Size(img);
				double ratio = size.Height / (double)size.Width;
				t.Check("scan: cut to the sheet (A4 shape, straight)", Math.Abs(ratio - 1.414) < 0.06, $"{size.Width}×{size.Height}");
			}
			finally { File.Delete(img); }

			string langs = OcrWork.Languages();
			if (langs.Length == 0) { t.Skip("scan ocr: no OCR language in this Windows"); return; }
			string original = await OcrWork.ReadImage(photo);
			string text = await OcrWork.ReadPdf(r.outputs[0], null, default);
			int before = Found(original), after = Found(text);
			int smallBefore = SmallPrint(original), smallAfter = SmallPrint(text);
			t.Info($"scan ocr: big words read on the photo {before}/{Words.Length}, on the clean scan {after}/{Words.Length}; small print {smallBefore} → {smallAfter} of {SmallPrintTotal}");
			t.Check("scan ocr: every word is readable on the clean scan", after == Words.Length, text.Replace("\n", " ").Replace("\r", ""));
			// the big words are easy even on the photo; the small print in the shadow is where the cleaning shows
			t.Check("scan ocr: better than the photo", after >= before && smallAfter > smallBefore, $"big {before}→{after}, small {smallBefore}→{smallAfter}");
		}

		static async Task CleanManyPhotos(DevTests.Ctx t)
		{
			var names = new[] { "ALPHA", "BRAVO", "CHARLIE" };
			var photos = names.Select((n, i) => Photo(Path.Combine(t.Dir, "p" + (i + 1) + ".jpg"), new[] { "PAGE", n }, 2400, 1800, i == 1 ? -5 : 4 + i, 10 + i)).ToList();
			// given out of name order: the PDF follows the order given, not the names
			var order = new[] { photos[2], photos[0], photos[1] };
			var r = await Jobs.Run("cleanscan", order, null);
			t.Check("scan many: 3 photos → one PDF", r.ok && r.outputs.Count == 1, r.message);
			if (!r.ok) return;
			t.Check("scan many: 3 pages", PdfWork.Pages(r.outputs[0]) == 3, PdfWork.Pages(r.outputs[0]).ToString());
			t.Check("scan many: named after the first photo", Path.GetFileName(r.outputs[0]).StartsWith("p3 (", StringComparison.Ordinal), r.outputs[0]);
			if (OcrWork.Languages().Length == 0) { t.Skip("scan many ocr: no OCR language"); return; }
			string text = (await OcrWork.ReadPdf(r.outputs[0], null, default)).ToUpperInvariant().Replace(" ", "");
			int c = text.IndexOf("CHARLIE", StringComparison.Ordinal), a = text.IndexOf("ALPHA", StringComparison.Ordinal), b = text.IndexOf("BRAVO", StringComparison.Ordinal);
			t.Check("scan many: pages in the order given", c >= 0 && a > c && b > a, text.Replace("\n", " ").Replace("\r", ""));

			// one bad file among good ones: the others still make the PDF, the bad one is told
			string bad = Path.Combine(t.Dir, "broken.jpg");
			File.WriteAllText(bad, "not a picture");
			r = await Jobs.Run("cleanscan", new[] { photos[0], bad }, null);
			t.Check("scan many: a broken photo is told, the good one is still done", r.ok && PdfWork.Pages(r.outputs[0]) == 1 && r.message.Contains("broken.jpg"), r.message);
			r = await Jobs.Run("cleanscan", new[] { bad }, null);
			t.Check("scan many: only a broken photo → nothing made, a message", !r.ok && r.message.Length > 0 && !Directory.GetFiles(t.Dir, "broken (*").Any(), r.message);
		}

		static async Task Modes(DevTests.Ctx t)
		{
			string photo = Photo(Path.Combine(t.Dir, "modes.jpg"), Words, 2400, 1800, 6, 3);
			foreach (var mode in new[] { "gray", "bw" })
			{
				var r = await Jobs.Run("cleanscan", new[] { photo }, null, default, new Dictionary<string, string> { ["mode"] = mode });
				t.Check("scan " + mode + ": a PDF", r.ok && PdfWork.Pages(r.outputs[0]) == 1, r.message);
				if (!r.ok) continue;
				var page = await RenderPdf(r.outputs[0], 0, 500);
				int colourful = 0;
				for (int i = 0; i < page.Data.Length; i += 4)
					if (Math.Max(Math.Abs(page.Data[i] - page.Data[i + 1]), Math.Abs(page.Data[i + 2] - page.Data[i + 1])) > 12) colourful++;
				t.Check("scan " + mode + ": no colour left", colourful < page.W * page.H / 200, colourful.ToString());
				t.Check("scan " + mode + ": white paper, no table", Median(page) >= 245 && Border(page) > 225, $"median {Median(page):0}, edge {Border(page):0}");
			}

			// a photo that is all paper (no table to find): it is still made level by its lines of text
			string close = Path.Combine(t.Dir, "close.png");
			using (var bmp = new Bitmap(2000, 1500))
			{
				using (var g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.FromArgb(235, 232, 225));
					g.TranslateTransform(1000, 750); g.RotateTransform(5);
					using var font = new Font("Arial", 110, FontStyle.Bold, GraphicsUnit.Pixel);
					for (int i = 0; i < 4; i++) g.DrawString("LEVEL TEXT " + i, font, Brushes.Black, -850, -600 + i * 280);
				}
				bmp.Save(close, ImageFormat.Png);
			}
			var photoPx = await ScanImage.Load(close);
			var straight = ScanImage.Straighten(photoPx, out bool found);
			t.Check("scan close-up: no sheet edge is invented", !found);
			var g2 = Gray(straight, out int w2, out int h2, 800);
			double angle = ScanImage.TextAngle(g2, w2, h2);
			t.Check("scan close-up: the text ends up level", Math.Abs(angle) < 1.0, $"{angle:0.0}° left");
		}

		/// <summary>An ID copy or a form: a big photo, a coloured box and a grey band must keep their look; the shading must still go.</summary>
		static Task PicturesOnPage(DevTests.Ctx t)
		{
			const int W = 2480, H = 3508;
			var p = new Pixels(W, H);
			void Fill(int x0, int y0, int w, int h, byte r, byte g, byte b)
			{
				for (int y = y0; y < y0 + h; y++)
					for (int x = x0; x < x0 + w; x++) { int o = (y * W + x) * 4; p.Data[o] = b; p.Data[o + 1] = g; p.Data[o + 2] = r; }
			}
			Fill(0, 0, W, H, 245, 243, 238);
			Fill(150, 150, 700, 900, 120, 120, 120);       // a photo
			Fill(1300, 300, 800, 600, 200, 60, 60);        // a red box
			Fill(0, 1400, W, 300, 190, 190, 190);          // a grey header band
			Fill(354, 2200, 354, 472, 110, 95, 85);        // a 3×4 photo
			for (int i = 0; i < 12; i++) Fill(200, 2800 + i * 50, 1800, 14, 30, 30, 30);   // lines of text
			// the light falls off towards the bottom-right, down to 60%
			for (int y = 0; y < H; y++)
				for (int x = 0; x < W; x++)
				{
					int o = (y * W + x) * 4;
					double l = 1 - 0.4 * (x / (double)W * 0.5 + y / (double)H * 0.5);
					for (int ch = 0; ch < 3; ch++) p.Data[o + ch] = (byte)(p.Data[o + ch] * l);
					p.Data[o + 3] = 255;
				}
			ScanImage.Whiten(p, ScanImage.Mode.Color);
			(int r, int g, int b) At(int x, int y) { int o = (y * W + x) * 4; return (p.Data[o + 2], p.Data[o + 1], p.Data[o]); }
			var photo = At(500, 600); var red = At(1700, 600); var band = At(1240, 1550); var small = At(531, 2436);
			t.Check("scan pictures: a big photo keeps its grey", photo.g < 170, photo.ToString());
			t.Check("scan pictures: a red box stays red", red.r > 170 && red.g < 120 && red.b < 120, red.ToString());
			t.Check("scan pictures: a grey band stays grey", band.g < 235, band.ToString());
			t.Check("scan pictures: a small photo stays", small.g < 170, small.ToString());
			var top = At(1240, 1250); var corner = At(2400, 3450); var mid = At(2300, 2000);
			t.Check("scan pictures: the paper is white everywhere (the shading is gone)", Math.Min(top.g, Math.Min(corner.g, mid.g)) >= 245, $"{top} {mid} {corner}");
			var ink = At(1000, 2807);
			t.Check("scan pictures: the text stays dark", ink.g < 60, ink.ToString());
			return Task.CompletedTask;
		}

		/// <summary>A sheet turned more than 45° or photographed sideways: the page comes out upright, with or without OCR.</summary>
		static async Task Sideways(DevTests.Ctx t)
		{
			bool ocr = OcrWork.Languages().Length > 0;
			foreach (var angle in new[] { 60f, 90f, -90f, -50f })
			{
				string photo = Photo(Path.Combine(t.Dir, $"side{angle}.jpg"), Words, 3200, 2600, angle, (int)angle + 200, size: 0.62);
				var r = await Jobs.Run("cleanscan", new[] { photo }, null);
				t.Check($"scan {angle}°: a PDF", r.ok, r.message);
				if (!r.ok) continue;
				var page = await RenderPdf(r.outputs[0], 0, 500);
				t.Check($"scan {angle}°: a standing page", page.H > page.W, $"{page.W}×{page.H}");
				if (!ocr) continue;
				string text = await OcrWork.ReadPdf(r.outputs[0], null, default);
				t.Check($"scan {angle}°: the words read (upright)", Found(text) >= 4, text.Replace("\n", " ").Replace("\r", ""));
			}
			if (!ocr) { t.Skip("scan sideways ocr: no OCR language"); return; }
			// without OCR, the left margin decides
			string side = Photo(Path.Combine(t.Dir, "noocr.jpg"), Words, 3200, 2600, 90, 7, size: 0.62);
			foreach (var turn in new[] { true, false })
			{
				var px = ScanImage.Clean(await ScanImage.Load(side), ScanImage.Mode.Color);
				// the test photo's sheet lies one way; turn it the other way too so both directions are checked
				if (!turn) px = ScanImage.Turn(ScanImage.Turn(px, true), true);
				t.Check("scan no-ocr: lying page seen as lying", ScanImage.Sideways(px));
				t.Info($"scan no-ocr: left-margin score clockwise {ScanImage.LeftAligned(ScanImage.Turn(px, true)):0.00}, other way {ScanImage.LeftAligned(ScanImage.Turn(px, false)):0.00}");
				var up = await CleanScan.Upright(px, false);
				string img = Path.Combine(t.Dir, $"up{turn}.jpg");
				await ScanImage.Save(up, img, ScanImage.Mode.Color);
				string text = await OcrWork.ReadImage(img);
				t.Check("scan no-ocr: upright by the left margin" + (turn ? "" : " (other way)"), up.H > up.W && Found(text) >= 4, text.Replace("\n", " ").Replace("\r", ""));
			}
		}

		/// <summary>A sheet photographed upside down (or nearly): the page comes out the right way up, read by the OCR.</summary>
		static async Task UpsideDown(DevTests.Ctx t)
		{
			if (OcrWork.Languages().Length == 0) { t.Skip("scan upside down: no OCR language"); return; }
			foreach (var angle in new[] { 180f, 172f })
			{
				string photo = Photo(Path.Combine(t.Dir, $"down{angle}.jpg"), Words, 3200, 2600, angle, (int)angle + 31, size: 0.62);
				{
					var cleaned = ScanImage.Clean(await ScanImage.Load(photo), ScanImage.Mode.Color);
					var small = ScanImage.Smaller(cleaned, 1600);
					t.Info($"scan {angle}°: words read as it is {await CleanScan.Words(small)}, turned round {await CleanScan.Words(ScanImage.Turn(ScanImage.Turn(small, true), true))}, sideways {ScanImage.Sideways(cleaned)}");
				}
				var r = await Jobs.Run("cleanscan", new[] { photo }, null);
				t.Check($"scan {angle}°: a PDF", r.ok, r.message);
				if (!r.ok) continue;
				string text = await OcrWork.ReadPdf(r.outputs[0], null, default);
				t.Check($"scan {angle}°: turned the right way up (the words read)", Found(text) >= 4, text.Replace("\n", " ").Replace("\r", ""));
			}
			// a page the right way up is never turned round
			string straight = Photo(Path.Combine(t.Dir, "straight.jpg"), Words, 3200, 2600, 2, 77, size: 0.62);
			var px = ScanImage.Clean(await ScanImage.Load(straight), ScanImage.Mode.Color);
			var sm = ScanImage.Smaller(px, 1600);
			t.Info($"scan straight: words read as it is {await CleanScan.Words(sm)}, turned round {await CleanScan.Words(ScanImage.Turn(ScanImage.Turn(sm, true), true))}");
			t.Check("scan: a page the right way up is not seen as upside down", !await CleanScan.UpsideDown(px));
		}
		
		/// <summary>A file another program keeps to itself, and a PDF that forbids copying: the real reason, at once.</summary>
		static async Task InUse(DevTests.Ctx t)
		{
			string photo = Photo(Path.Combine(t.Dir, "lock.jpg"), Words, 1600, 1200, 3, 5);
			using (new FileStream(photo, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				var r = await Jobs.Run("cleanscan", new[] { photo }, null);
				t.Check("in use: a photo open elsewhere says so (no raw message)", !r.ok && r.message.Contains(T.S("docs.inUse")) && !r.message.Contains(t.Dir), r.message);
			}
			string pdf = Path.Combine(t.Dir, "held.pdf");
			using (var doc = new PdfSharp.Pdf.PdfDocument())
			{
				var page = doc.AddPage();
				using (var g = XGraphics.FromPdfPage(page)) g.DrawString("Held words", new XFont("Arial", 18), XBrushes.Black, new XPoint(72, 100));
				doc.Save(pdf);
			}
			using (new FileStream(pdf, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				var sw = Stopwatch.StartNew();
				var r = await Jobs.Run("toword", new[] { pdf }, null);
				t.Check("in use: a PDF open elsewhere says so, at once", !r.ok && r.message.Contains(T.S("docs.inUse")) && sw.Elapsed < TimeSpan.FromSeconds(5), r.message);
			}

			string noCopy = Path.Combine(t.Dir, "statement.pdf");
			using (var doc = new PdfSharp.Pdf.PdfDocument())
			{
				var page = doc.AddPage();
				using (var g = XGraphics.FromPdfPage(page)) g.DrawString("Bank statement", new XFont("Arial", 18), XBrushes.Black, new XPoint(72, 100));
				doc.SecuritySettings.OwnerPassword = "owner";
				doc.SecuritySettings.PermitExtractContent = false;
				doc.Save(noCopy);
			}
			t.Check("no copy: seen as protected", ToWord.ForbidsCopy(noCopy));
			t.Check("no copy: an ordinary PDF is not", !ToWord.ForbidsCopy(pdf));
			if (ToWord.Converter() != null)
			{
				var sw = Stopwatch.StartNew();
				var r = await Jobs.Run("toword", new[] { noCopy }, null);
				t.Check("no copy: says why, at once", !r.ok && r.message.Contains(T.S("docs.word.protected")) && sw.Elapsed < TimeSpan.FromSeconds(5), r.message);
			}
		}

		// ------------------------------------------------------------------ PDF → Word

		static async Task ToWordTest(DevTests.Ctx t)
		{
			string conv = ToWord.Converter();
			if (conv == null) { t.Skip("toword: no Word nor LibreOffice here"); return; }
			t.Info("toword: " + conv);
			string pdf = Path.Combine(t.Dir, "letter.pdf");
			using (var doc = new PdfSharp.Pdf.PdfDocument())
			{
				var page = doc.AddPage();
				using var g = XGraphics.FromPdfPage(page);
				var font = new XFont("Arial", 18);
				g.DrawString("Dear DeskPals friends,", font, XBrushes.Black, new XPoint(72, 100));
				g.DrawString("The quick brown fox jumps over the lazy dog.", font, XBrushes.Black, new XPoint(72, 140));
				g.DrawString("Hippopotamus marmalade 2026.", font, XBrushes.Black, new XPoint(72, 180));
				doc.Save(pdf);
			}
			string version = ToWord.WordVersion();
			object regBefore = version == null ? null : Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Office\" + version + @"\Word\Options", "DisableConvertPdfWarning", null);
			var wordsBefore = Process.GetProcessesByName("WINWORD").Select(p => p.Id).ToHashSet();
			var sw = Stopwatch.StartNew();
			var r = await Jobs.Run("toword", new[] { pdf }, null);
			t.Check("toword: a PDF becomes a .docx (" + conv + ")", r.ok && r.outputs.Count == 1 && Jobs.Ext(r.outputs[0]) == ".docx", r.message);
			t.Info($"toword: {sw.ElapsedMilliseconds} ms");
			if (r.ok)
			{
				string xml;
				using (var zip = ZipFile.OpenRead(r.outputs[0]))
				using (var s = new StreamReader(zip.GetEntry("word/document.xml").Open())) xml = s.ReadToEnd();
				string text = Regex.Replace(Regex.Replace(xml, "<w:tab/>|</w:p>", " "), "<[^>]+>", "");
				t.Check("toword: the document has the PDF's words", text.Contains("DeskPals") && text.Contains("quick brown fox") && text.Contains("Hippopotamus"), text.Length > 200 ? text[..200] : text);
				t.Check("toword: the PDF is untouched", File.Exists(pdf) && PdfWork.Pages(pdf) == 1);
				r = await Jobs.Run("toword", new[] { pdf }, null);
				t.Check("toword: a second time gets (2), nothing overwritten", r.ok && Path.GetFileName(r.outputs[0]) == "letter (2).docx", r.ok ? r.outputs[0] : r.message);
			}
			if (version != null)
			{
				object regAfter = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Office\" + version + @"\Word\Options", "DisableConvertPdfWarning", null);
				t.Check("toword: Word's settings are put back as they were", Equals(regBefore, regAfter), $"{regBefore} → {regAfter}");
			}
			// Word quits by itself; give it a few seconds
			bool left = true;
			for (int i = 0; i < 20 && left; i++)
			{
				left = Process.GetProcessesByName("WINWORD").Any(p => !wordsBefore.Contains(p.Id));
				if (left) await Task.Delay(500);
			}
			t.Check("toword: no Word left running", !left);

			// a PDF with a password: Word must say it cannot, not wait for a password nobody can type
			string locked = Path.Combine(t.Dir, "locked.pdf");
			using (var doc = new PdfSharp.Pdf.PdfDocument())
			{
				var page = doc.AddPage();
				using (var g = XGraphics.FromPdfPage(page)) g.DrawString("Secret words", new XFont("Arial", 18), XBrushes.Black, new XPoint(72, 100));
				doc.SecuritySettings.UserPassword = "secret";
				doc.Save(locked);
			}
			var old = ToWord.Timeout;
			ToWord.Timeout = TimeSpan.FromSeconds(90);
			try
			{
				var sw2 = Stopwatch.StartNew();
				r = await Jobs.Run("toword", new[] { locked }, null);
				t.Check("toword: a PDF with a password says so, leaves nothing", !r.ok && r.message.Length > 0 && !File.Exists(Path.Combine(t.Dir, "locked.docx")), r.message);
				t.Info($"toword locked ({sw2.ElapsedMilliseconds} ms): {r.message}");
			}
			finally { ToWord.Timeout = old; }
		}

		// ------------------------------------------------------------------ HEIC

		static async Task Heic(DevTests.Ctx t)
		{
			// the help job: the Store page (opened by a stand-in here) and one friendly sentence
			string opened = null;
			var keep = HeicHelp.Open;
			HeicHelp.Open = link => { opened = link; return true; };
			try
			{
				string fake = Path.Combine(t.Dir, "IMG_0001.heic"); File.WriteAllBytes(fake, new byte[] { 0, 0, 0, 24 });
				var r = await Jobs.Run("heichelp", new[] { fake }, null);
				t.Check("heic help: opens the free HEIF page of the Store", opened == HeicHelp.StoreLink, opened ?? "(nothing)");
				t.Check("heic help: says so in one friendly sentence", r.preview.Length > 20 && !r.preview.Contains('\n'), r.preview);
			}
			finally { HeicHelp.Open = keep; }

			if (!HeicHelp.CanDecode()) { t.Skip("heic: Windows here cannot open HEIC (the help job is offered instead)"); return; }
			string png = t.Picture("iphone.png", "IPHONE 2026", Color.White, 1200, 900);
			string heic = Path.Combine(t.Dir, "iphone.heic");
			try
			{
				var px = await ScanImage.Load(png);
				using var output = File.Create(heic);
				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.HeifEncoderId, output.AsRandomAccessStream());
				encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)px.W, (uint)px.H, 96, 96, px.Data);
				await encoder.FlushAsync();
			}
			catch (Exception ex) { t.Skip("heic: Windows here cannot write a HEIC to test with (" + ex.GetType().Name + ")"); return; }
			var j = await Jobs.Run("tojpg", new[] { heic }, null);
			t.Check("heic: an iPhone photo → JPG with the core job", j.ok && DevTests.Size(j.outputs[0]) == new Size(1200, 900), j.message);
			var s = await Jobs.Run("cleanscan", new[] { heic }, null);
			t.Check("heic: an iPhone photo → clean scan", s.ok && PdfWork.Pages(s.outputs[0]) == 1, s.message);
		}

		// ------------------------------------------------------------------ helpers

		static int Found(string text)
		{
			string t = (text ?? "").ToUpperInvariant().Replace(" ", "");
			return Words.Count(w => t.Contains(w, StringComparison.Ordinal));
		}

		/// <summary>The small print has 6 lines of "small print line number N of the test document": its words read right.</summary>
		const int SmallPrintTotal = 6 * 9;
		static readonly string[] SmallWords = { "SMALL", "PRINT", "LINE", "NUMBER", "OF", "THE", "TEST", "DOCUMENT" };

		static int SmallPrint(string text)
		{
			int n = 0;
			foreach (var line in (text ?? "").ToUpperInvariant().Split('\n'))
			{
				var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (!words.Contains("PRINT") && !words.Contains("DOCUMENT") && !words.Contains("SMALL")) continue;
				n += words.Count(w => SmallWords.Contains(w) || (w.Length == 1 && char.IsDigit(w[0])));
			}
			return n;
		}

		/// <summary>
		/// A phone photo of a paper: a dark wooden table, an off-white A4 sheet with big dark words and small print, turned by
		/// <paramref name="angle"/>° and seen a little from below (the top looks narrower), light falling off towards one
		/// corner, a soft shadow (<paramref name="shadow"/>: the share of light it takes away at its middle) and some grain (JPEG).
		/// </summary>
		internal static string Photo(string path, string[] lines, int w, int h, float angle, int seed, double shadow = 0.45, bool shadowOnText = false, double size = 0.8)
		{
			var rnd = new Random(seed);
			int sh = (int)(h * size), sw = (int)(sh / 1.414);
			using var sheet = new Bitmap(sw, sh, PixelFormat.Format24bppRgb);
			using (var g = Graphics.FromImage(sheet))
			{
				g.Clear(Color.FromArgb(240, 238, 230));
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
				using var font = new Font("Arial", sh / 15f, FontStyle.Bold, GraphicsUnit.Pixel);
				using var ink = new SolidBrush(Color.FromArgb(40, 38, 48));
				for (int i = 0; i < lines.Length; i++) g.DrawString(lines[i], font, ink, sw * 0.08f, sh * 0.08f + i * sh * 0.14f);
				// the small print is pale, like pencil or a faded print
				using var small = new Font("Arial", sh / 95f, GraphicsUnit.Pixel);
				using var pale = new SolidBrush(Color.FromArgb(120, 118, 128));
				for (int i = 0; i < 6; i++) g.DrawString("small print line number " + i + " of the test document", small, pale, sw * 0.08f, sh * 0.82f + i * sh / 40f);
			}
			using var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
			using (var g = Graphics.FromImage(bmp))
			{
				g.Clear(Color.FromArgb(78, 56, 40));
				for (int i = 0; i < 500; i++)
				{
					using var pen = new Pen(Color.FromArgb(rnd.Next(45, 115), rnd.Next(32, 80), rnd.Next(20, 55)), rnd.Next(2, 12));
					int y = rnd.Next(-100, h + 100);
					g.DrawLine(pen, 0, y, w, y + rnd.Next(-80, 80));
				}
			}
			// where the sheet's corners land: narrower at the top, turned, a little off-centre
			double r = angle * Math.PI / 180, cos = Math.Cos(r), sin = Math.Sin(r), cx0 = w / 2.0 + w * 0.04, cy0 = h / 2.0 + h * 0.01;
			var corners = new[] { (-sw / 2.0 * 0.9, -sh / 2.0), (sw / 2.0 * 0.9, -sh / 2.0), (sw / 2.0, sh / 2.0), (-sw / 2.0, sh / 2.0) }
				.Select(p => (cx0 + p.Item1 * cos - p.Item2 * sin, cy0 + p.Item1 * sin + p.Item2 * cos)).ToArray();
			var m = ScanImage.SquareToQuad(corners);
			// photo → sheet: the inverse of the 3×3 matrix [a b c; d e f; g h 1]
			double a = m[0], b = m[1], c = m[2], d = m[3], e = m[4], f = m[5], gg = m[6], hh = m[7];
			double A = e - f * hh, B = c * hh - b, C = b * f - c * e, D = f * gg - d, E = a - c * gg, F = c * d - a * f, G = d * hh - e * gg, H = b * gg - a * hh, I = a * e - b * d;
			var sd = sheet.LockBits(new Rectangle(0, 0, sw, sh), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			var pd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			try
			{
				var src = new byte[sd.Stride * sh];
				System.Runtime.InteropServices.Marshal.Copy(sd.Scan0, src, 0, src.Length);
				var row = new byte[pd.Stride];
				// the shadow lies beside the text, or (the hard case) right over the small print
				double lx = shadowOnText ? w * 0.47 : w * 0.68, ly = shadowOnText ? h * 0.8 : h * 0.72, r2 = Math.Pow(h * 0.28, 2);
				for (int y = 0; y < h; y++)
				{
					System.Runtime.InteropServices.Marshal.Copy(pd.Scan0 + y * pd.Stride, row, 0, pd.Stride);
					for (int x = 0; x < w; x++)
					{
						double X = x + 0.5, Y = y + 0.5, z = G * X + H * Y + I;
						double s = (A * X + B * Y + C) / z, t = (D * X + E * Y + F) / z;
						if (s >= 0 && s < 1 && t >= 0 && t < 1)
						{
							int o = (int)(t * sh) * sd.Stride + (int)(s * sw) * 3;
							row[x * 3] = src[o]; row[x * 3 + 1] = src[o + 1]; row[x * 3 + 2] = src[o + 2];
						}
						// the light: brighter at the top-left, dim at the bottom-right, a round shadow (a hand, a phone)
						double l = 1.05 - 0.6 * (x / (double)w * 0.55 + y / (double)h * 0.45);
						double d2 = (x - lx) * (x - lx) + (y - ly) * (y - ly);
						l *= 1 - shadow * Math.Exp(-d2 / (2 * r2));
						int noise = rnd.Next(-6, 7);
						for (int ch = 0; ch < 3; ch++) row[x * 3 + ch] = (byte)Math.Clamp(row[x * 3 + ch] * l + noise, 0, 255);
					}
					System.Runtime.InteropServices.Marshal.Copy(row, 0, pd.Scan0 + y * pd.Stride, pd.Stride);
				}
			}
			finally { sheet.UnlockBits(sd); bmp.UnlockBits(pd); }
			bmp.Save(path, ImageFormat.Jpeg);
			return path;
		}

		static async Task<double> BorderBrightness(string picture) => Border(await ScanImage.Load(picture));

		/// <summary>The average brightness of the outer 3% of a picture.</summary>
		static double Border(Pixels p)
		{
			int mx = Math.Max(1, p.W * 3 / 100), my = Math.Max(1, p.H * 3 / 100);
			double sum = 0; long n = 0;
			for (int y = 0; y < p.H; y++)
				for (int x = 0; x < p.W; x++)
				{
					if (x >= mx && x < p.W - mx && y >= my && y < p.H - my) continue;
					sum += p.Data[(y * p.W + x) * 4 + 1]; n++;
				}
			return sum / n;
		}

		static double Median(Pixels p)
		{
			var hist = new long[256];
			for (int i = 1; i < p.Data.Length; i += 4) hist[p.Data[i]]++;
			long half = p.W * (long)p.H / 2, acc = 0;
			for (int v = 0; v < 256; v++) { acc += hist[v]; if (acc >= half) return v; }
			return 255;
		}

		static float[] Gray(Pixels p, out int w, out int h, int side)
		{
			double k = Math.Min(1.0, side / (double)Math.Max(p.W, p.H));
			w = Math.Max(1, (int)(p.W * k)); h = Math.Max(1, (int)(p.H * k));
			var g = new float[w * h];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					int i = ((int)(y / k) * p.W + (int)(x / k)) * 4;
					g[y * w + x] = 0.114f * p.Data[i] + 0.587f * p.Data[i + 1] + 0.299f * p.Data[i + 2];
				}
			return g;
		}

		/// <summary>A page of a PDF drawn by Windows, as pixels.</summary>
		static async Task<Pixels> RenderPdf(string pdf, uint index, uint width)
		{
			using var input = File.OpenRead(pdf);
			var doc = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			using var page = doc.GetPage(index);
			using var ms = new InMemoryRandomAccessStream();
			await page.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions { DestinationWidth = width, BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255) });
			var decoder = await BitmapDecoder.CreateAsync(ms);
			var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
			return new Pixels((int)decoder.PixelWidth, (int)decoder.PixelHeight, data.DetachPixelData());
		}
	}
}
