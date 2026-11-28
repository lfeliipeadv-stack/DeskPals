using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig.Content;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// The tests of "searchable" and of the OCR engines: the words are really in the PDF (read back with PdfPig), at their
	/// place, on turned pages too; text is never doubled; the engine choice and the fallback; Tesseract for real only with
	/// DESKPALS_TESSERACT=1 (it downloads its language data).
	/// </summary>
	internal static class OcrPdfTests
	{
		internal static void Register()
		{
			DevTests.Add("ocrpdf searchable", Basic);
			DevTests.Add("ocrpdf rotated page", Rotated);
			DevTests.Add("ocrpdf placement", Placement);
			DevTests.Add("ocrpdf not doubled", NotDoubled);
			DevTests.Add("ocrpdf pictures", Pictures);
			DevTests.Add("ocrpdf every script", Scripts);
			DevTests.Add("ocrpdf engine choice", EngineChoice);
			DevTests.Add("ocrpdf tesseract live", TesseractLive);
		}

		static bool NoOcr(DevTests.Ctx t)
		{
			if (OcrWork.Languages().Length > 0) return false;
			t.Skip("ocrpdf: no OCR language in this Windows");
			return true;
		}

		/// <summary>The text of a page as PdfPig reads it: capitals, no spaces.</summary>
		static string Flat(string s) => new string(s.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();

		static List<string> PageTexts(string pdf)
		{
			using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
			return doc.GetPages().Select(p => Flat(p.Text)).ToList();
		}

		static string Hash(string file) { using var s = File.OpenRead(file); return Convert.ToHexString(SHA256.HashData(s)); }

		static int Count(string text, string word) { int n = 0; for (int i = text.IndexOf(word, StringComparison.Ordinal); i >= 0; i = text.IndexOf(word, i + 1, StringComparison.Ordinal)) n++; return n; }

		/// <summary>The letters of a word on a page (the first place it is), with PdfPig.</summary>
		static List<Letter> LettersOf(string pdf, int page, string word)
		{
			using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
			var letters = doc.GetPage(page + 1).Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();
			string flat = string.Concat(letters.Select(l => l.Value)).ToUpperInvariant();
			int at = flat.IndexOf(word, StringComparison.Ordinal);
			return at < 0 || flat.Length != letters.Count ? new List<Letter>() : letters.GetRange(at, word.Length);
		}

		static async Task<OcrImage> Render(string pdf, int page)
		{
			using var input = File.OpenRead(pdf);
			var doc = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			using var p = doc.GetPage((uint)page);
			return await OcrText.FromPdfPage(p, 60, 4000);
		}

		// ------------------------------------------------------------------ the tests

		static async Task Basic(DevTests.Ctx t)
		{
			string pdf = await t.Pdf("scan.pdf", "DESKPALS SEARCH", "HELLO PETS");
			var plan = Jobs.Plan(new[] { pdf }).Select(j => j.Id).ToList();
			t.Check("ocrpdf: a PDF → \"searchable\" comes before \"ocr\"", plan.IndexOf("searchable") >= 0 && plan.IndexOf("searchable") < plan.IndexOf("ocr"), string.Join(",", plan));
			string pic = t.Picture("pic.png", "HELLO", Color.White);
			plan = Jobs.Plan(new[] { pic }).Select(j => j.Id).ToList();
			t.Check("ocrpdf: a picture → \"searchable\" right after \"ocr\"", plan.IndexOf("searchable") == plan.IndexOf("ocr") + 1, string.Join(",", plan));
			t.Check("ocrpdf: not for a Word file", !Jobs.Plan(new[] { WriteEmpty(t, "x.docx") }).Any(j => j.Id == "searchable"));
			if (NoOcr(t)) return;

			string before = Hash(pdf);
			var r = await Jobs.Run("searchable", new[] { pdf }, null);
			t.Check("ocrpdf: it works", r.ok && r.outputs.Count == 1, r.message);
			if (!r.ok) return;
			string o = r.outputs[0];
			t.Check("ocrpdf: the name says it", Path.GetFileName(o) == "scan (" + T.S("ocrpdf.name") + ").pdf", Path.GetFileName(o));
			t.Check("ocrpdf: the original is untouched", Hash(pdf) == before);
			t.Check("ocrpdf: the same pages", PdfWork.Pages(o) == 2);
			var texts = PageTexts(o);
			t.Check("ocrpdf: the words of page 1 are in the PDF", texts[0].Contains("DESKPALS") && texts[0].Contains("SEARCH"), texts[0]);
			t.Check("ocrpdf: the words of page 2 are in the PDF", texts[1].Contains("HELLO") && texts[1].Contains("PETS"), texts[1]);
			t.Check("ocrpdf: the original had no text", PageTexts(pdf).All(s => s.Length == 0));
			t.Check("ocrpdf: the bubble says how many words", r.preview.Length > 0, r.preview);

			// at their place: the picture (1200×400) fills a landscape A4 (842×595) with 18 of margin; the words are drawn at
			// 40 px from its left (GDI+ adds about a sixth of the letter size before the first letter: they are seen at ~56 px),
			// a third of the way down, 80 px tall
			var letters = LettersOf(o, 0, "DESKPALS");
			double k = Math.Min((841.89 - 36) / 1200, (595.28 - 36) / 400), imgTop = 18 + ((595.28 - 36) - 400 * k) / 2;
			double left = letters.Count > 0 ? letters.Min(l => l.BoundingBox.Left) : -1;
			double middle = letters.Count > 0 ? 595.28 - letters.Average(l => l.BoundingBox.Centroid.Y) : -1;
			double wantLeft = 18 + 56 * k, wantMiddle = imgTop + (400 / 3.0 + 45) * k;
			t.Check("ocrpdf: the word is where it is seen (left)", Math.Abs(left - wantLeft) < 15, $"{left:0} vs {wantLeft:0}");
			t.Check("ocrpdf: the word is where it is seen (height)", Math.Abs(middle - wantMiddle) < 25, $"{middle:0} vs {wantMiddle:0}");
			double width = letters.Count > 0 ? letters.Max(l => l.BoundingBox.Right) - left : 0;
			t.Info($"ocrpdf: DESKPALS at x={left:0}, y={middle:0} (from the top), {width:0} wide");

			// it looks the same: the invisible words change no pixel
			var a = await Render(pdf, 0);
			var b = await Render(o, 0);
			int diff = 0;
			if (a.Bgra.Length == b.Bgra.Length) for (int i = 0; i < a.Bgra.Length; i++) diff = Math.Max(diff, Math.Abs(a.Bgra[i] - b.Bgra[i]));
			t.Check("ocrpdf: it looks the same", a.Bgra.Length == b.Bgra.Length && diff <= 8, "largest difference " + diff);

			// a second time: the text is already there
			r = await Jobs.Run("searchable", new[] { o }, null);
			t.Check("ocrpdf: a searchable PDF is not done again", !r.ok && r.message.Contains(T.S("ocrpdf.already")), r.message);
			string broken = Path.Combine(t.Dir, "broken.pdf");
			File.WriteAllText(broken, "not a pdf");
			r = await Jobs.Run("searchable", new[] { broken }, null);
			t.Check("ocrpdf: a broken PDF says so", !r.ok && r.message.Length > 0, r.message);
		}

		static string WriteEmpty(DevTests.Ctx t, string name) { string f = Path.Combine(t.Dir, name); File.WriteAllText(f, ""); return f; }

		/// <summary>A page whose picture lies on its side and that /Rotate 90 shows upright, with a CropBox that is not at 0,0.</summary>
		static async Task Rotated(DevTests.Ctx t)
		{
			// the turning of the page (clockwise when shown) maps the seen corners right, for every angle
			var box = new XRect(20, 30, 500, 700);
			double mediaH = 800;
			XPoint Pdf(int rot, double x, double y) => Searchable.ShownToPage(rot, box, mediaH).Transform(new XPoint(x, mediaH - y));
			bool Near(XPoint p, double x, double y) => Math.Abs(p.X - x) < 0.01 && Math.Abs(p.Y - y) < 0.01;
			t.Check("ocrpdf: 0° — the seen top-left is the box's top-left", Near(Pdf(0, 0, 0), 20, 730) && Near(Pdf(0, 500, 700), 520, 30));
			t.Check("ocrpdf: 90° — the seen top-left is the box's bottom-left", Near(Pdf(90, 0, 0), 20, 30) && Near(Pdf(90, 700, 500), 520, 730));
			t.Check("ocrpdf: 180° — the seen top-left is the box's bottom-right", Near(Pdf(180, 0, 0), 520, 30));
			t.Check("ocrpdf: 270° — the seen top-left is the box's top-right", Near(Pdf(270, 0, 0), 520, 730));
			if (NoOcr(t)) return;

			string pic = t.Picture("side.png", "ROTATED WORDS", Color.White);
			using (var bmp = new Bitmap(pic))
			{
				// lying on its side (turned left): the page turned right shows it upright
				bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
				bmp.Save(Path.Combine(t.Dir, "side2.png"));
			}
			string raw = Path.Combine(t.Dir, "raw.pdf"), pdf = Path.Combine(t.Dir, "turned.pdf");
			await PdfWork.FromImages(new[] { Path.Combine(t.Dir, "side2.png") }, raw);
			using (var doc = PdfReader.Open(raw, PdfDocumentOpenMode.Modify))
			{
				var page = doc.Pages[0];
				page.Rotate = 90;
				page.CropBox = new PdfRectangle(new XPoint(10, 12), new XPoint(page.MediaBox.X2, page.MediaBox.Y2));
				doc.Save(pdf);
			}
			var r = await Jobs.Run("searchable", new[] { pdf }, null);
			t.Check("ocrpdf: a turned page works", r.ok, r.message);
			if (!r.ok) return;
			string text = PageTexts(r.outputs[0])[0];
			t.Check("ocrpdf: the words of a turned page are in the PDF", text.Contains("ROTATED") && text.Contains("WORDS"), text);
			using (var check = PdfReader.Open(r.outputs[0], PdfDocumentOpenMode.Import))
				t.Check("ocrpdf: the page stays turned", check.Pages[0].Rotate == 90, check.Pages[0].Rotate.ToString());
			// PdfPig gives the places as the page is seen (turned): a copy that is not turned shows where they really are
			string flat = Path.Combine(t.Dir, "unturned.pdf");
			using (var copy = PdfReader.Open(r.outputs[0], PdfDocumentOpenMode.Modify)) { copy.Pages[0].Rotate = 0; copy.Save(flat); }
			var letters = LettersOf(flat, 0, "ROTATED");
			// the picture (400×1200) sits in the middle of a portrait A4: x from 163 to 432
			bool inside = letters.Count > 0 && letters.All(l => l.BoundingBox.Centroid.X > 160 && l.BoundingBox.Centroid.X < 435 && l.BoundingBox.Centroid.Y > 12 && l.BoundingBox.Centroid.Y < 842);
			t.Check("ocrpdf: the words of a turned page are on the picture", inside, letters.Count == 0 ? "not found" : string.Join(" ", letters.Select(l => $"{l.BoundingBox.Centroid.X:0},{l.BoundingBox.Centroid.Y:0}")));
			// they read upwards on the unturned page: what is shown turned right reads left to right
			bool upwards = letters.Count > 1 && letters[^1].BoundingBox.Centroid.Y > letters[0].BoundingBox.Centroid.Y + 20 && Math.Abs(letters[^1].BoundingBox.Centroid.X - letters[0].BoundingBox.Centroid.X) < 15;
			t.Check("ocrpdf: the words of a turned page run the way they are seen", upwards, letters.Count == 0 ? "" : $"{letters[0].BoundingBox.Centroid.X:0},{letters[0].BoundingBox.Centroid.Y:0} → {letters[^1].BoundingBox.Centroid.X:0},{letters[^1].BoundingBox.Centroid.Y:0}");
		}

		/// <summary>
		/// The words land where they are seen for every turning, on a page whose MediaBox and CropBox are not at 0,0 (no OCR
		/// needed: the words are given). Checked on an unturned copy, in PDF units.
		/// </summary>
		static Task Placement(DevTests.Ctx t)
		{
			foreach (bool crop in new[] { false, true })
				foreach (int rot in new[] { 0, 90, 180, 270 })
				{
					string pdf = Path.Combine(t.Dir, $"place-{rot}-{crop}.pdf"), flat = Path.Combine(t.Dir, $"place-{rot}-{crop}-flat.pdf");
					bool side = rot is 90 or 270;
					// the box that is shown, in PDF units
					double x0 = crop ? 150 : 100, y0 = crop ? 250 : 200, x1 = crop ? 650 : 700, y1 = crop ? 950 : 1000;
					using (var doc = new PdfDocument())
					{
						var page = doc.AddPage();
						page.MediaBox = new PdfRectangle(new XPoint(100, 200), new XPoint(700, 1000));
						if (crop) page.CropBox = new PdfRectangle(new XPoint(x0, y0), new XPoint(x1, y1));
						page.Rotate = rot;
						// what is seen, in pixels at 2 per point; the word "PLACEHERE" 200×40 pt at 60,80 pt from the seen top-left
						double seenW = side ? y1 - y0 : x1 - x0, seenH = side ? x1 - x0 : y1 - y0;
						var read = new OcrPage { Width = (int)(seenW * 2), Height = (int)(seenH * 2) };
						read.Words.Add(new OcrWord("PLACEHERE", 120, 160, 400, 80, 0));
						t.Check($"ocrpdf: placement {rot}° {(crop ? "with" : "without")} CropBox — one word", Searchable.WriteText(page, read) == 1);
						t.Check($"ocrpdf: placement {rot}° — the page keeps its turning", page.Rotate == rot);
						doc.Save(pdf);
					}
					using (var copy = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify)) { copy.Pages[0].Rotate = 0; copy.Save(flat); }
					var letters = LettersOf(flat, 0, "PLACEHERE");
					// the middle of the word as seen (160, 100 pt), then where that is on the unturned page
					double sx = 160, sy = 100;
					var want = rot switch
					{
						90 => (x: x0 + sy, y: y0 + sx),
						180 => (x: x1 - sx, y: y0 + sy),
						270 => (x: x1 - sy, y: y1 - sx),
						_ => (x: x0 + sx, y: y1 - sy),
					};
					// PdfPig counts from the shown box's bottom-left corner; the corners of the turned letters give the extent
					var corners = letters.SelectMany(l => new[] { l.BoundingBox.BottomLeft, l.BoundingBox.BottomRight, l.BoundingBox.TopLeft, l.BoundingBox.TopRight }).ToList();
					double minX = corners.Count > 0 ? corners.Min(p => p.X) + x0 : 0, maxX = corners.Count > 0 ? corners.Max(p => p.X) + x0 : 0;
					double minY = corners.Count > 0 ? corners.Min(p => p.Y) + y0 : 0, maxY = corners.Count > 0 ? corners.Max(p => p.Y) + y0 : 0;
					double gx = letters.Count > 0 ? (minX + maxX) / 2 : -1;
					double gy = letters.Count > 0 ? (minY + maxY) / 2 : -1;
					t.Check($"ocrpdf: placement {rot}° {(crop ? "with" : "without")} CropBox — at its place", Math.Abs(gx - want.x) < 12 && Math.Abs(gy - want.y) < 12,
						$"{gx:0},{gy:0} vs {want.x:0},{want.y:0}");
					// stretched to its box: 200 pt along the line
					double along = side ? maxY - minY : maxX - minX;
					t.Check($"ocrpdf: placement {rot}° {(crop ? "with" : "without")} CropBox — as long as its box", Math.Abs(along - 200) < 20, $"{along:0}");
				}
			return Task.CompletedTask;
		}

		static async Task NotDoubled(DevTests.Ctx t)
		{
			// a page with real text, written by PDFsharp
			string typed = Path.Combine(t.Dir, "typed.pdf");
			using (var doc = new PdfDocument())
			{
				var page = doc.AddPage();
				using var g = XGraphics.FromPdfPage(page);
				g.DrawString("THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG", new XFont("Arial", 18), XBrushes.Black, 40, 100);
				g.DrawString("A SECOND LINE OF REAL TEXT", new XFont("Arial", 18), XBrushes.Black, 40, 140);
				doc.Save(typed);
			}
			t.Check("ocrpdf: a page with real text is seen", Searchable.PagesWithText(typed).SetEquals(new[] { 0 }));
			var r = await Jobs.Run("searchable", new[] { typed }, null);
			t.Check("ocrpdf: a PDF that has text is left alone", !r.ok && r.outputs.Count == 0 && r.message.Contains(T.S("ocrpdf.already")), r.message);
			if (NoOcr(t)) return;

			// a scanned page and a typed one in one PDF: only the scanned one gets text
			string scan = await t.Pdf("scan.pdf", "SCANNED PAGE");
			string mixed = Path.Combine(t.Dir, "mixed.pdf");
			PdfWork.Merge(new[] { scan, typed }, mixed);
			r = await Jobs.Run("searchable", new[] { mixed }, null);
			t.Check("ocrpdf: a PDF with a scanned page and a typed one", r.ok, r.message);
			if (!r.ok) return;
			var texts = PageTexts(r.outputs[0]);
			t.Check("ocrpdf: the scanned page gets its words", texts[0].Contains("SCANNED"), texts[0]);
			t.Check("ocrpdf: the typed page is not doubled", Count(texts[1], "QUICKBROWN") == 1 && Count(texts[1], "SECONDLINE") == 1, texts[1]);
		}

		static async Task Pictures(DevTests.Ctx t)
		{
			if (NoOcr(t)) return;
			string a = t.Picture("first.jpg", "FIRST PICTURE", Color.White);
			string b = t.Picture("second.png", "SECOND PICTURE", Color.LightYellow);
			var r = await Jobs.Run("searchable", new[] { a, b }, null);
			t.Check("ocrpdf: pictures → one searchable PDF", r.ok && r.outputs.Count == 1 && PdfWork.Pages(r.outputs[0]) == 2, r.message);
			if (!r.ok) return;
			t.Check("ocrpdf: named after the first picture", Path.GetFileName(r.outputs[0]) == "first (" + T.S("ocrpdf.name") + ").pdf", Path.GetFileName(r.outputs[0]));
			var texts = PageTexts(r.outputs[0]);
			t.Check("ocrpdf: the words of each picture", texts[0].Contains("FIRST") && texts[1].Contains("SECOND"), string.Join(" | ", texts));
		}

		/// <summary>
		/// Words of every script of the program are really written (read back with PdfPig), not empty boxes; a script no
		/// font of this Windows has is left out and not counted. No OCR needed: the words are given.
		/// </summary>
		static Task Scripts(DevTests.Ctx t)
		{
			var words = new (string word, string family)[]
			{
				("ação", null), ("русский", null), ("العربية", null), ("日本語", "Yu Gothic"), ("ひらがな", "Yu Gothic"), ("中文", "Microsoft YaHei"),
				("한국어", "Malgun Gothic"), ("ภาษาไทย", "Leelawadee UI"), ("हिन्दी", "Nirmala UI"), ("বাংলা", "Nirmala UI"), ("தமிழ்", "Nirmala UI"),
			};
			string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
			foreach (var (word, family) in words)
			{
				var probe = new Dictionary<(string, double), XFont>();
				if (ScriptFonts.For(word, 12, probe) == null) { t.Skip($"ocrpdf: no font of this Windows has \"{word}\" ({family})"); continue; }
				string pdf = Path.Combine(t.Dir, $"script-{Array.FindIndex(words, w => w.word == word)}.pdf");
				int written;
				using (var doc = new PdfDocument())
				{
					var page = doc.AddPage();
					var read = new OcrPage { Width = 1000, Height = 1400 };
					read.Words.Add(new OcrWord(word, 100, 100, 300, 60, 0));
					written = Searchable.WriteText(page, read);
					doc.Save(pdf);
				}
				string got = PageTexts(pdf)[0];
				string want = Flat(word);
				t.Check($"ocrpdf: \"{word}\" is written and found again", written == 1 && got.Normalize() == want.Normalize(), $"written={written}, got '{got}'");
			}
			// a letter no font has (a private-use code): left out, not counted, no empty box written
			using (var doc = new PdfDocument())
			{
				var page = doc.AddPage();
				var read = new OcrPage { Width = 1000, Height = 1400 };
				read.Words.Add(new OcrWord("", 100, 100, 300, 60, 0));
				read.Words.Add(new OcrWord("OK", 500, 100, 100, 60, 0));
				t.Check("ocrpdf: a word no font has is left out", Searchable.WriteText(page, read) == 1);
			}
			// a .ttc file (several fonts in one) is given to PDFsharp as its first font: a real font file
			string ttc = new[] { "YuGothR.ttc", "msgothic.ttc", "msyh.ttc", "Nirmala.ttc" }.Select(f => Path.Combine(fonts, f)).FirstOrDefault(File.Exists);
			if (ttc == null) t.Skip("ocrpdf: no .ttc font in this Windows");
			else
			{
				byte[] one = ScriptFonts.FirstFont(File.ReadAllBytes(ttc));
				t.Check("ocrpdf: the first font of a .ttc is a font file", one.Length > 1000 && one[0] == 0 && one[1] == 1 || System.Text.Encoding.ASCII.GetString(one, 0, 4) is "OTTO" or "true", Path.GetFileName(ttc));
			}
			return Task.CompletedTask;
		}

		static async Task EngineChoice(DevTests.Ctx t)
		{
			string folder = TessData.Folder, url = TessData.BaseUrl;
			try
			{
				t.Check("ocrpdf: Windows reads by default", OcrText.Choice() == "windows");
				var pref = Prefs.All().FirstOrDefault(p => p.Key == OcrText.Key);
				t.Check("ocrpdf: the switch is in the settings, off", pref != null && !pref.Get());
				pref?.Set(true);
				t.Check("ocrpdf: the switch chooses Tesseract", OcrText.Choice() == "tesseract" && ToolSettings.Text(OcrText.Key) == "tesseract" && pref.Get());
				pref?.Set(false);
				t.Check("ocrpdf: and back to Windows", OcrText.Choice() == "windows" && ToolSettings.Text(OcrText.Key) == "windows");
				ToolSettings.Set(OcrText.Key, null);
				ToolSettings.Set("tesseract", true);
				t.Check("ocrpdf: the older true/false setting is honoured", OcrText.Choice() == "tesseract");
				ToolSettings.Set("tesseract", null);
				ToolSettings.Set(OcrText.Key, "something else");
				t.Check("ocrpdf: anything unknown is Windows", OcrText.Choice() == "windows");

				t.Check("ocrpdf: Tesseract languages for Portuguese", TessData.LanguagesFor("pt-BR") == "por+eng");
				t.Check("ocrpdf: Tesseract languages for English", TessData.LanguagesFor("en") == "eng");
				t.Check("ocrpdf: Tesseract languages for Chinese and an unknown one", TessData.LanguagesFor("zh-CN") == "chi_sim+eng" && TessData.LanguagesFor("xx") == "eng" && TessData.LanguagesFor("de-AT") == "deu+eng");

				using (var w = await OcrText.Open(null, default, null))
					t.Check("ocrpdf: with Windows chosen, Windows reads", w is WindowsOcr);

				// Tesseract chosen but its download fails (nothing listens there): Windows does the work and says so
				TessData.Folder = Path.Combine(t.Dir, "tessdata");
				TessData.BaseUrl = "http://127.0.0.1:9/";
				OcrText.Choose(true);
				var notes = new List<string>();
				if (OcrWork.Languages().Length > 0)
					using (var e = await OcrText.Open(null, default, notes.Add))
						t.Check("ocrpdf: Tesseract unavailable → Windows reads, with a message", e is WindowsOcr && notes.Count == 1 && notes[0].Contains(T.S("ocrpdf.err.download")), string.Join(" | ", notes));
				t.Check("ocrpdf: a failed download leaves no file", Directory.GetFiles(TessData.Dir).Length == 0, string.Join(", ", Directory.GetFiles(TessData.Dir).Select(Path.GetFileName)));

				// the size check: a file far too small, and one that stops halfway
				foreach (var (what, reply) in new[] { ("too small", Reply(10, 10)), ("cut short", Reply(500_000, 2000)) })
				{
					using var server = new TinyServer(reply);
					TessData.BaseUrl = server.Url;
					string error = null;
					try { await TessData.Ensure("eng", null, default); }
					catch (Exception ex) { error = ex.Message; }
					t.Check("ocrpdf: a download " + what + " is refused", error == T.S("ocrpdf.err.download") && Directory.GetFiles(TessData.Dir).Length == 0, error ?? "accepted");
				}
				// a server that takes the call and never answers: given up after AnswerWait, not minutes later
				var silent = new TcpListener(IPAddress.Loopback, 0);
				var wait = TessData.AnswerWait;
				try
				{
					silent.Start();
					TessData.BaseUrl = "http://127.0.0.1:" + ((IPEndPoint)silent.LocalEndpoint).Port + "/";
					TessData.AnswerWait = TimeSpan.FromSeconds(1);
					var clock = System.Diagnostics.Stopwatch.StartNew();
					string error = null;
					try { await TessData.Ensure("eng", null, default); }
					catch (Exception ex) { error = ex.Message; }
					t.Check("ocrpdf: a server that never answers is given up quickly", error == T.S("ocrpdf.err.download") && clock.Elapsed.TotalSeconds < 10, $"{error} after {clock.Elapsed.TotalSeconds:0.0} s");
				}
				finally { TessData.AnswerWait = wait; silent.Stop(); }
				if (OcrWork.Languages().Length > 0)
				{
					// the "ocr" job's way in: the text with the chosen engine (falling back here)
					string pic = t.Picture("read.png", "FALLBACK WORKS", Color.White);
					notes.Clear();
					TessData.BaseUrl = "http://127.0.0.1:9/";
					string text = await OcrText.ReadImage(pic, default, notes.Add);
					t.Check("ocrpdf: plain text through the engine choice", Flat(text).Contains("FALLBACK") && notes.Count == 1, text + " " + string.Join(" | ", notes));
				}
			}
			finally
			{
				TessData.Folder = folder; TessData.BaseUrl = url;
				ToolSettings.Set(OcrText.Key, null);
			}
		}

		/// <summary>An HTTP answer that announces <paramref name="announced"/> bytes and sends <paramref name="sent"/>.</summary>
		static byte[] Reply(int announced, int sent) => System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {announced}\r\nConnection: close\r\n\r\n").Concat(new byte[sent]).ToArray();

		/// <summary>A one-answer web server on this computer (no internet, no rights needed): every request gets the same bytes.</summary>
		sealed class TinyServer : IDisposable
		{
			readonly TcpListener listener = new(IPAddress.Loopback, 0);
			public string Url { get; }
			public TinyServer(byte[] reply)
			{
				listener.Start();
				Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/";
				_ = Task.Run(async () =>
				{
					try
					{
						while (true)
						{
							using var client = await listener.AcceptTcpClientAsync();
							var s = client.GetStream();
							var buf = new byte[4096];
							// the request itself does not matter: its first bytes are enough to answer
							await s.ReadAtLeastAsync(buf, 1, throwOnEndOfStream: false);
							await s.WriteAsync(reply);
							await s.FlushAsync();
						}
					}
					catch (Exception) { }
				});
			}
			public void Dispose() => listener.Stop();
		}

		/// <summary>Tesseract for real: downloads its language data (only with DESKPALS_TESSERACT=1), into this test's folder.</summary>
		static async Task TesseractLive(DevTests.Ctx t)
		{
			if (Environment.GetEnvironmentVariable("DESKPALS_TESSERACT") != "1") { t.Skip("ocrpdf: Tesseract for real (set DESKPALS_TESSERACT=1; it downloads about 6 MB)"); return; }
			string folder = TessData.Folder, native = TesseractOcr.NativeFolder;
			try
			{
				TessData.Folder = Path.Combine(t.Dir, "tessdata");
				TesseractOcr.NativeFolder = Path.Combine(t.Dir, "native");
				OcrText.Choose(true);
				var steps = new List<string>();
				using (var e = await OcrText.Open((p, s) => steps.Add(s), default, s => steps.Add("NOTICE " + s)))
				{
					t.Check("ocrpdf: Tesseract starts", e.Name == "tesseract", string.Join(" | ", steps.Where(s => s.StartsWith("NOTICE"))));
					t.Check("ocrpdf: the download reports its progress", steps.Any(s => !s.StartsWith("NOTICE")));
				}
				t.Check("ocrpdf: the language data is here", Directory.GetFiles(TessData.Dir, "*.traineddata").Length >= 1);
				string pdf = await t.Pdf("bad.pdf", "TESSERACT READS");
				var r = await Jobs.Run("searchable", new[] { pdf }, null);
				t.Check("ocrpdf: Tesseract makes a searchable PDF", r.ok && r.message.Length == 0, r.message);
				if (r.ok)
				{
					string text = PageTexts(r.outputs[0])[0];
					t.Check("ocrpdf: Tesseract's words are in the PDF", text.Contains("TESSERACT") && text.Contains("READS"), text);
				}
				string pic = t.Picture("t.png", "HELLO TESSERACT", Color.White);
				string plain = await OcrText.ReadImage(pic, default, null);
				t.Check("ocrpdf: Tesseract reads plain text", Flat(plain).Contains("TESSERACT"), plain);
			}
			finally
			{
				TessData.Folder = folder; TesseractOcr.NativeFolder = native;
				ToolSettings.Set(OcrText.Key, null);
			}
		}
	}
}
