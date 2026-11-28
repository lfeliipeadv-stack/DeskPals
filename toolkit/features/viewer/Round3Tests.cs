using System.Drawing;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of the third round: protection with limits, subtitles, Word from a scan, translation parts, tables to Excel,
	/// comparing, PDF to pictures and its photos, resizing, the background taken out, reading the screen, the keyboard
	/// shortcuts, the marks of the viewer (and the black box burnt in), and the translations' placeholders.
	/// </summary>
	internal static class Round3Tests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("r3 protect", Protect);
			DevTests.Add("r3 subtitles", Subs);
			DevTests.Add("r3 word", Word);
			DevTests.Add("r3 translate parts", TranslateParts);
			DevTests.Add("r3 tables", TablesTest);
			DevTests.Add("r3 compare", CompareTest);
			DevTests.Add("r3 pictures", PicturesTest);
			DevTests.Add("r3 photos", PhotosTest);
			DevTests.Add("r3 snip", SnipTest);
			DevTests.Add("r3 shortcuts", ShortcutsTest);
			DevTests.Add("r3 marks", MarksTest);
			DevTests.Add("r3 texts placeholders", Placeholders);
			DevTests.Add("r3 live", Live);
		}

		static Dictionary<string, string> Opt(params string[] kv)
		{
			var d = new Dictionary<string, string>();
			for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
			return d;
		}

		/// <summary>A PDF with real text: each page its lines, at (x, y) in points.</summary>
		static string TextPdf(string file, params (double x, double y, string text)[][] pages)
		{
			using var doc = new PdfSharp.Pdf.PdfDocument();
			var font = new XFont("Arial", 11);
			foreach (var lines in pages)
			{
				var p = doc.AddPage();
				using var g = XGraphics.FromPdfPage(p);
				foreach (var (x, y, text) in lines) g.DrawString(text, font, XBrushes.Black, x, y);
			}
			doc.Save(file);
			return file;
		}

		static string PageText(string pdf, int page)
		{
			using var d = UglyToad.PdfPig.PdfDocument.Open(pdf);
			return d.GetPage(page).Text;
		}

		// ------------------------------------------------------------------ protect with limits

		static async Task Protect(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "p.pdf"), new[] { (50.0, 80.0, "Protected text") });
			var r = await Jobs.Run("protect", new[] { pdf }, null, default, Opt("password", "", "print", "0", "copy", "0", "edit", "1"));
			t.Check("protect: only limits (no password to open) → anyone opens it, with limits", r.ok && PdfLock.Of(r.outputs[0]) == PdfLock.State.Limited, r.message + " " + (r.ok ? PdfLock.Of(r.outputs[0]).ToString() : ""));
			r = await Jobs.Run("protect", new[] { pdf }, null, default, Opt("password", "abc", "owner", "xyz", "print", "1", "copy", "1", "edit", "1"));
			t.Check("protect: a password to open it", r.ok && PdfLock.Of(r.outputs[0]) == PdfLock.State.Locked, r.message);
			string back = Path.Combine(t.Dir, "back.pdf");
			try { PdfLock.Unprotect(r.outputs[0], back, "xyz"); } catch (Exception) { }
			t.Check("protect: the owner's password takes it all off", File.Exists(back) && PdfLock.Of(back) == PdfLock.State.Open);
			r = await Jobs.Run("protect", new[] { pdf }, null, default, Opt("password", "", "print", "1", "copy", "1", "edit", "1"));
			t.Check("protect: no password and nothing limited → says so", !r.ok && r.message.Length > 0, r.message);
		}

		// ------------------------------------------------------------------ subtitles

		static Task Subs(DevTests.Ctx t)
		{
			t.Check("subtitles: times as SubRip writes them", Subtitles.Time(3661.25) == "01:01:01,250" && Subtitles.Time(0.5) == "00:00:00,500", Subtitles.Time(3661.25));
			var segs = new List<AiService.Segment>
			{
				new(1.5, 4, "Hello there."),
				new(4, 14, string.Join(" ", Enumerable.Repeat("word", 50))),
			};
			string srt = Subtitles.Srt(segs);
			t.Check("subtitles: numbered, with from --> to", srt.StartsWith("1\r\n00:00:01,500 --> 00:00:04,000\r\nHello there.\r\n\r\n"), srt[..Math.Min(80, srt.Length)]);
			var blocks = srt.Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries);
			t.Check("subtitles: a long stretch → several subtitles of at most two lines of about 42 letters", blocks.Length >= 3 && blocks.All(b => b.Split("\r\n").Skip(2).All(l => l.Length <= 50) && b.Split("\r\n").Length <= 4), blocks.Length.ToString());
			var ids = Jobs.Plan(new[] { t.Tone("v.wav") }).Select(j => j.Id).ToList();
			t.Check("subtitles: offered for a recording", ids.Contains("subtitles"), string.Join(",", ids));
			return Task.CompletedTask;
		}

		// ------------------------------------------------------------------ Word without Word, Word from a scan

		static async Task Word(DevTests.Ctx t)
		{
			string docx = Path.Combine(t.Dir, "made.docx");
			OfficeFiles.Docx(docx, new[] { new OfficeFiles.Para("Title", true), new OfficeFiles.Para("First line\nsecond line"), new OfficeFiles.Para("New page & <tags>", false, true) }, "pt-BR");
			string back = AiText.Docx(docx);
			t.Check("word: a document made here opens and has its text", back.Contains("Title") && back.Contains("second line") && back.Contains("New page & <tags>"), back);
			var paras = ToWord.Paragraphs(new[] { "This is a long line of a paragraph that goes on\nand ends here.\nShort title\n• an item\n• another item docu-\nment" });
			t.Check("word: the lines of a scan are joined in paragraphs (titles, list items and cut words kept apart or put back)",
				paras.Any(p => p.Text.StartsWith("This is a long line of a paragraph that goes on and ends here.")) && paras.Any(p => p.Text == "• an item") && paras.Any(p => p.Text.Contains("document")),
				string.Join(" | ", paras.Select(p => p.Text)));
			if (OcrWork.Languages().Length == 0) { t.Skip("word scan: no OCR language"); return; }
			string scan = await t.Pdf("scan.pdf", "INVOICE NUMBER 42", "TOTAL DUE TODAY");
			var r = await Jobs.Run("toword", new[] { scan }, null);
			string text = r.ok ? AiText.Docx(r.outputs[0]) : "";
			t.Check("word: a scanned PDF → a Word document with its text (editable, not pictures)", r.ok && text.Contains("INVOICE") && text.Contains("TOTAL"), r.message + " " + text);
		}

		static Task TranslateParts(DevTests.Ctx t)
		{
			string text = string.Join("\n\n", Enumerable.Range(1, 40).Select(i => $"Paragraph {i}. " + string.Join(" ", Enumerable.Repeat("words", 40))));
			var parts = Translate.Parts(text);
			t.Check("translate: a long text in parts not bigger than the limit, nothing lost", parts.Count > 1 && parts.All(p => p.Length <= Translate.Part) && parts.Sum(p => Regex.Matches(p, "Paragraph").Count) == 40, parts.Count.ToString());
			string huge = new string('a', 9000);
			t.Check("translate: one very long paragraph is cut too", Translate.Parts(huge).All(p => p.Length <= Translate.Part));
			t.Check("translate: every language of the program can be chosen", Translate.Languages().Count >= 23);
			return Task.CompletedTask;
		}

		// ------------------------------------------------------------------ tables

		static async Task TablesTest(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "table.pdf"), new[]
			{
				(50.0, 60.0, "Monthly report"),
				(50.0, 100.0, "Item"), (250.0, 100.0, "Amount"), (400.0, 100.0, "Due"),
				(50.0, 120.0, "Rent"), (250.0, 120.0, "1,200.50"), (400.0, 120.0, "05/12"),
				(50.0, 140.0, "Water bill"), (250.0, 140.0, "80"),
			});
			var r = await Jobs.Run("toexcel", new[] { pdf }, null);
			t.Check("tables: a spreadsheet is made", r.ok && r.outputs.Count == 1 && r.outputs[0].EndsWith(".xlsx"), r.message);
			if (!r.ok) return;
			string sheet;
			using (var zip = ZipFile.OpenRead(r.outputs[0])) using (var s = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml").Open())) sheet = s.ReadToEnd();
			t.Check("tables: the columns are kept (Amount and its numbers in the same column)", Regex.IsMatch(sheet, "r=\"B\\d+\"[^>]*><is><t[^>]*>Amount") && sheet.Contains("<c r=\"B4\"><v>80</v>"), sheet.Length > 600 ? sheet[..600] : sheet);
			t.Check("tables: numbers become numbers, a date stays text", sheet.Contains("<v>1200.5</v>") && sheet.Contains(">05/12<"), "");
			t.Check("tables: a text line stays one cell", sheet.Contains(">Monthly report<"));
			t.Check("tables: numbers in both ways of writing", OfficeFiles.Number("1.234,56", out double a) && a == 1234.56 && OfficeFiles.Number("1,234.56", out double b) && b == 1234.56 && !OfficeFiles.Number("007", out _) && !OfficeFiles.Number("12/05", out _));
		}

		// ------------------------------------------------------------------ compare

		static async Task CompareTest(DevTests.Ctx t)
		{
			string a = Path.Combine(t.Dir, "v1.txt"), b = Path.Combine(t.Dir, "v2.txt");
			File.WriteAllText(a, "The rent is 1,000 reais.\n\nThe deposit is two months.\n\nSigned in Lisbon.");
			File.WriteAllText(b, "The rent is 1,200 reais.\n\nThe deposit is two months.\n\nA new clause about pets.\n\nSigned in Lisbon.");
			var ids = Jobs.Plan(new[] { a, b }).Select(j => j.Id).ToList();
			t.Check("compare: offered for two documents", ids.Contains("compare"), string.Join(",", ids));
			var r = await Jobs.Run("compare", new[] { a, b }, null);
			string html = r.ok ? File.ReadAllText(r.outputs[0]) : "";
			t.Check("compare: what changed is marked (old struck, new underlined), the rest as it is", r.ok && html.Contains("<del>000</del>") && html.Contains("<ins>200</ins>") && html.Contains("<ins>A new clause about pets.") && html.Contains("The deposit is two months."), r.message + " " + Regex.Match(html, "<div class=\"doc\">.{0,300}").Value);
			File.WriteAllText(b, File.ReadAllText(a));
			r = await Jobs.Run("compare", new[] { a, b }, null);
			t.Check("compare: the same text → says so", r.ok && r.preview == T.S("compare.same"), r.preview);
			var big = string.Join("\n\n", Enumerable.Range(1, 3000).Select(i => "Paragraph number " + i + " with some words."));
			var parts = Compare.Diff(big, big.Replace("Paragraph number 1500 ", "Paragraph number 1501 "));
			t.Check("compare: a long document is quick and exact", parts.Count(p => p.kind != 0) == 2, parts.Count(p => p.kind != 0).ToString());
		}

		// ------------------------------------------------------------------ PDF to pictures, its photos, resize, background

		static async Task PicturesTest(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "two.pdf"), new[] { (50.0, 80.0, "Page one") }, new[] { (50.0, 80.0, "Page two") });
			var r = await Jobs.Run("topictures", new[] { pdf }, null);
			var pngs = r.ok ? Directory.GetFiles(r.outputs[0], "*.png") : Array.Empty<string>();
			t.Check("pdf to pictures: one PNG per page, in a folder", r.ok && pngs.Length == 2 && t.SizeOf(pngs[0]).Width > 1000, r.message + " " + pngs.Length);

			string withPhoto = Path.Combine(t.Dir, "photo.pdf");
			await PdfWork.FromImages(new[] { t.Picture("ph.png", "PHOTO", Color.SkyBlue, 640, 480) }, withPhoto);
			r = await Jobs.Run("photos", new[] { withPhoto }, null);
			t.Check("photos: the picture inside a PDF is saved", r.ok && Directory.GetFiles(r.outputs[0]).Length == 1, r.message);
			r = await Jobs.Run("photos", new[] { pdf }, null);
			t.Check("photos: a PDF without photos says so", !r.ok && r.message.Contains(T.S("photos.none")), r.message);
		}

		static async Task PhotosTest(DevTests.Ctx t)
		{
			string a = t.Picture("big1.png", "ONE", Color.White, 3000, 2000), b = t.Picture("big2.png", "TWO", Color.White, 2000, 3000);
			var r = await Jobs.Run("resize", new[] { a, b }, null, default, Opt("side", "1280"));
			var made = r.ok ? Directory.GetFiles(r.outputs[0]) : Array.Empty<string>();
			t.Check("resize: several photos → a folder, the longest side 1280", r.ok && made.Length == 2 && made.All(f => Math.Max(t.SizeOf(f).Width, t.SizeOf(f).Height) == 1280), r.message);
			r = await Jobs.Run("resize", new[] { t.Picture("tiny.png", "T", Color.White, 300, 200) }, null, default, Opt("side", "1920"));
			t.Check("resize: a small photo is never made bigger", r.ok && t.SizeOf(r.outputs[0]) == new Size(300, 200), r.message);

			// a red disc on a plain light grey background
			string disc = Path.Combine(t.Dir, "disc.png");
			using (var bmp = new Bitmap(400, 300))
			{
				using var g = Graphics.FromImage(bmp);
				g.Clear(Color.FromArgb(235, 235, 232));
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.FillEllipse(Brushes.Firebrick, 120, 70, 160, 160);
				bmp.Save(disc, System.Drawing.Imaging.ImageFormat.Png);
			}
			r = await Jobs.Run("nobackground", new[] { disc }, null, default, Opt("to", "clear"));
			bool clear = false;
			if (r.ok) using (var o = new Bitmap(r.outputs[0])) clear = o.GetPixel(5, 5).A == 0 && o.GetPixel(200, 150).A == 255 && o.GetPixel(200, 150).R > 120;
			t.Check("background: a plain background becomes see-through, the subject stays", clear, r.message);
			r = await Jobs.Run("nobackground", new[] { disc }, null, default, Opt("to", "white"));
			bool white = false;
			if (r.ok) using (var o = new Bitmap(r.outputs[0])) { var c = o.GetPixel(5, 5); white = c.R > 245 && c.G > 245 && c.B > 245; }
			t.Check("background: or white, for a document photo", white, r.message);
			// noise everywhere: not a plain background
			string busy = Path.Combine(t.Dir, "busy.png");
			using (var bmp = new Bitmap(200, 150))
			{
				var rnd = new Random(3);
				for (int x = 0; x < 200; x++) for (int y = 0; y < 150; y++) bmp.SetPixel(x, y, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
				bmp.Save(busy, System.Drawing.Imaging.ImageFormat.Png);
			}
			r = await Jobs.Run("nobackground", new[] { busy }, null, default, Opt("to", "clear"));
			t.Check("background: a busy background is not touched, and it says why", !r.ok && r.message.Contains(T.S("nobg.notPlain")), r.message);
		}

		// ------------------------------------------------------------------ the screen, the keys

		static async Task SnipTest(DevTests.Ctx t)
		{
			if (OcrWork.Languages().Length == 0) { t.Skip("snip: no OCR language"); return; }
			using var shot = new Bitmap(900, 300);
			using (var g = Graphics.FromImage(shot))
			{
				g.Clear(Color.FromArgb(30, 30, 34));
				using var f = new Font("Segoe UI", 28, FontStyle.Bold, GraphicsUnit.Pixel);
				g.DrawString("COPY THIS SENTENCE", f, Brushes.White, 40, 40);
				g.DrawString("NOT THIS ONE", f, Brushes.White, 40, 200);
			}
			string text = await SnipForm.ReadText(shot, new Rectangle(30, 30, 500, 60));
			t.Check("snip: the text inside the box is read (light on dark too)", text.Contains("COPY") && text.Contains("SENTENCE") && !text.Contains("NOT"), text);
			string tiny = await SnipForm.ReadText(shot, new Rectangle(40, 45, 160, 30));
			t.Check("snip: a small box is enlarged before reading", tiny.Contains("COPY"), tiny);
		}

		static Task ShortcutsTest(DevTests.Ctx t)
		{
			string before = Shortcuts.Folder;
			Shortcuts.Folder = Path.Combine(t.Dir, "start");
			try
			{
				Shortcuts.Write(Environment.ProcessPath);
				t.Check("shortcuts: DeskPals Tools with Ctrl+Alt+D", File.Exists(Shortcuts.OpenLink) && ShellLink.Hotkey(Shortcuts.OpenLink) == Shortcuts.OpenKey && string.Equals(ShellLink.Target(Shortcuts.OpenLink), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase));
				t.Check("shortcuts: read the screen with Ctrl+Alt+T", File.Exists(Shortcuts.SnipLink) && ShellLink.Hotkey(Shortcuts.SnipLink) == Shortcuts.SnipKey);
				Shortcuts.Remove();
				t.Check("shortcuts: turned off → gone", !File.Exists(Shortcuts.OpenLink) && !File.Exists(Shortcuts.SnipLink));
			}
			finally { Shortcuts.Folder = before; }
			return Task.CompletedTask;
		}

		// ------------------------------------------------------------------ the marks of the viewer

		static async Task MarksTest(DevTests.Ctx t)
		{
			var r0 = new RectangleF(0.1f, 0.2f, 0.3f, 0.1f);
			static bool Near(RectangleF a, RectangleF b) => Math.Abs(a.X - b.X) < 1e-4 && Math.Abs(a.Y - b.Y) < 1e-4 && Math.Abs(a.Width - b.Width) < 1e-4 && Math.Abs(a.Height - b.Height) < 1e-4;
			t.Check("marks: a box turns with its page (four quarter turns: back where it was)", Near(Marks.Turn(Marks.Turn(Marks.Turn(Marks.Turn(r0, 90), 90), 90), 90), r0) && Near(Marks.Turn(r0, 90), new RectangleF(0.7f, 0.1f, 0.1f, 0.3f)) && Near(Marks.Turn(Marks.Turn(r0, 90), 270), r0), Marks.Turn(r0, 90).ToString());

			string pdf = TextPdf(Path.Combine(t.Dir, "secret.pdf"),
				new[] { (60.0, 100.0, "Name: Maria Silva"), (60.0, 130.0, "ID number 123.456.789-00") },
				new[] { (60.0, 100.0, "Second page stays text") });
			// a signature picture
			string sign = Path.Combine(t.Dir, "sign.png");
			using (var bmp = new Bitmap(300, 100))
			{
				using var g = Graphics.FromImage(bmp);
				g.Clear(Color.Transparent);
				g.DrawBezier(new Pen(Color.Navy, 4), 10, 80, 80, 0, 160, 100, 290, 20);
				bmp.Save(sign, System.Drawing.Imaging.ImageFormat.Png);
			}
			var marks = new List<Marks.Placed>
			{
				// the ID line, hidden (page 1); a highlight and a text and a signature on page 2
				new() { at = 0, kind = (int)MarkKind.Redact, x = 0.05f, y = 0.13f, w = 0.6f, h = 0.05f },
				new() { at = 1, kind = (int)MarkKind.Highlight, x = 0.05f, y = 0.1f, w = 0.4f, h = 0.04f },
				new() { at = 1, kind = (int)MarkKind.Text, x = 0.1f, y = 0.5f, w = 0.5f, h = 0.05f, text = "Approved" },
				new() { at = 1, kind = (int)MarkKind.Sign, x = 0.5f, y = 0.8f, w = 0.3f, h = 0.08f, image = sign },
			};
			var r = await Jobs.Run("view", new[] { pdf }, null, default, Opt("kind", "pdf", "order", "1-2", "save", "copy", "marks", Marks.Write(marks)));
			t.Check("marks: saved", r.ok && PdfWork.Pages(r.outputs[0]) == 2, r.message);
			if (!r.ok) return;
			string p1 = PageText(r.outputs[0], 1), p2 = PageText(r.outputs[0], 2);
			t.Check("marks: what is under a black box is gone (the page has no text left to copy)", !p1.Contains("123") && !p1.Contains("Maria"), p1);
			t.Check("marks: a page without a black box keeps its text, with the text added", p2.Contains("Second page stays text") && p2.Contains("Approved"), p2);
			using (var d = UglyToad.PdfPig.PdfDocument.Open(r.outputs[0]))
				t.Check("marks: the signature is on the page (a picture)", d.GetPage(2).GetImages().Any());

			await ViewerTests.OnWindowThread(async () =>
			{
				using var f = new ViewerForm(new[] { pdf }) { SkipCloseQuestion = true };
				f.Show();
				await ViewerTests.Until(() => f.BigDone && f.View.Picture != null);
				f.TestMark(MarkKind.Highlight, new RectangleF(0.1f, 0.1f, 0.3f, 0.05f));
				t.Check("marks window: a highlight drawn is a change, and is shown", f.Dirty && f.View.MarksShown.Count == 1 && f.Build(false).ContainsKey("marks"));
				f.Grid.Turn(90);
				var placed = Marks.Read(f.Build(false)["marks"]);
				t.Check("marks window: the page turned → the mark turns with it", placed.Count == 1 && Math.Abs(placed[0].x - (1 - 0.15f)) < 0.001 && f.View.MarksShown[0].Box.X > 0.8f, placed.Count == 1 ? placed[0].x.ToString() : "");
				f.View.SelectedMark = 0;
				t.Check("marks window: Delete takes the picked mark away (not the page)", f.RemovePickedMark() && f.Grid.Tiles.Count == 2 && !f.Build(false).ContainsKey("marks"));
				f.Undo();
				t.Check("marks window: undo brings it back", f.Build(false).ContainsKey("marks"));
				f.Close();
			});
		}

		/// <summary>The real assistant (only with DESKPALS_AI_LIVE=1): a translation, and subtitles of speech made by Windows' own voice.</summary>
		static async Task Live(DevTests.Ctx t)
		{
			if (Environment.GetEnvironmentVariable("DESKPALS_AI_LIVE") != "1") { t.Skip("r3 live: set DESKPALS_AI_LIVE=1 to use the real assistant"); return; }
			string doc = Path.Combine(t.Dir, "lease.txt");
			File.WriteAllText(doc, "LEASE\n\nThe rent is due on the fifth day of each month.\n\nThe tenant may keep a cat.");
			var r = await Jobs.Run("translate", new[] { doc }, null, default, Opt("to", "pt-BR"));
			string text = r.ok ? AiText.Docx(r.outputs[0]) : "";
			t.Check("live: translated into Portuguese, as a Word document", r.ok && text.ToLowerInvariant().Contains("aluguel") && text.ToLowerInvariant().Contains("gato"), r.message + " " + text);
			t.Info("live translation: " + text.Replace("\n", " / "));
			// speech made by Windows
			string wav = Path.Combine(t.Dir, "speech.wav");
			using (var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
			{
				var english = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Language.StartsWith("en"));
				if (english != null) synth.Voice = english;
				using var stream = await synth.SynthesizeTextToStreamAsync("Hello. This is a test of the subtitles. The pet is sleeping on the keyboard.");
				using var file = File.Create(wav);
				await stream.AsStreamForRead().CopyToAsync(file);
			}
			r = await Jobs.Run("subtitles", new[] { wav }, null);
			string srt = r.ok ? File.ReadAllText(r.outputs[0]) : "";
			t.Check("live: subtitles with times", r.ok && srt.Contains("-->") && srt.ToLowerInvariant().Contains("keyboard"), r.message + " " + srt);
			t.Info("live subtitles: " + srt.Replace("\r\n", " | "));
		}
		
		// ------------------------------------------------------------------ the translations

		/// <summary>Every translation keeps the {0} {1}… of the English text (a lost one would show a wrong or empty value).</summary>
		static Task Placeholders(DevTests.Ctx t)
		{
			var en = T.Read("en");
			var bad = new List<string>();
			foreach (var code in T.Codes().Where(c => c != "en"))
			{
				var lang = T.Read(code);
				foreach (var (key, text) in en)
				{
					if (!lang.TryGetValue(key, out var other)) continue;
					var need = Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).ToHashSet();
					var have = Regex.Matches(other, @"\{\d+\}").Select(m => m.Value).ToHashSet();
					if (!need.SetEquals(have)) bad.Add(code + ":" + key);
				}
			}
			t.Check("texts: every language keeps the {0} {1}… of each text", bad.Count == 0, string.Join(", ", bad.Take(20)));
			return Task.CompletedTask;
		}
	}
}
