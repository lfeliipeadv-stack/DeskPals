using System.Drawing;
using System.Runtime.CompilerServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of the second round: several files in "See and edit", the languages of Tesseract, the stronger way to make
	/// a scan smaller, "Ask about the file" and the buttons drawn with colour emoji.
	/// </summary>
	internal static class MoreTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("more viewer several", Several);
			DevTests.Add("more tesseract languages", Languages);
			DevTests.Add("more compress scans", Scans);
			DevTests.Add("more askai", AskAiTest);
			DevTests.Add("more buttons", Buttons);
			DevTests.Add("more askai live", AskLive);
		}

		static Dictionary<string, string> Opt(params string[] kv)
		{
			var d = new Dictionary<string, string>();
			for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
			return d;
		}

		/// <summary>A PDF whose pages are told apart by their width (first page 301 points + offset…).</summary>
		static string Widths(string file, int pages, int offset)
		{
			using var doc = new PdfSharp.Pdf.PdfDocument();
			for (int i = 1; i <= pages; i++) { var p = doc.AddPage(); p.Width = XUnit.FromPoint(300 + offset + i); p.Height = XUnit.FromPoint(400); }
			doc.Save(file);
			return file;
		}

		static int Width(string pdf, int page)
		{
			using var d = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			return (int)Math.Round(d.Pages[page - 1].MediaBox.Width);
		}

		// ------------------------------------------------------------------ several files in the viewer

		static async Task Several(DevTests.Ctx t)
		{
			string a = Widths(Path.Combine(t.Dir, "sa.pdf"), 2, 0), b = Widths(Path.Combine(t.Dir, "sb.pdf"), 3, 10);
			string pic = t.Picture("sc.png", "C", Color.White, 400, 300);
			var (pdf, from) = await ViewerJobs.Combine(new[] { a, b, pic });
			try
			{
				t.Check("several: the pages of all the files, in order (a picture is one page)", PdfWork.Pages(pdf) == 6 && from.Count == 6
					&& from[0] == (0, 1) && from[2] == (1, 1) && from[4] == (1, 3) && from[5] == (2, 1), string.Join(" ", from));
			}
			finally { File.Delete(pdf); }

			var r = await Jobs.Run("view", new[] { a, b, pic }, null, default, Opt("kind", "pdf", "order", "5,1,6", "rotate", "1:90", "save", "replace"));
			bool good = r.ok && r.outputs.Count == 1 && PdfWork.Pages(r.outputs[0]) == 3;
			t.Check("several: one new PDF with the pages chosen, in the new order", good && Width(r.outputs[0], 1) == 313 && Width(r.outputs[0], 2) == 301, r.message);
			t.Check("several: “replace” is never done with several files (a copy instead)", good && r.outputs[0] != a && PdfWork.Pages(a) == 2 && PdfWork.Pages(b) == 3, r.outputs.FirstOrDefault());

			await ViewerTests.OnWindowThread(async () =>
			{
				using var f = new ViewerForm(new[] { a, b }) { SkipCloseQuestion = true };
				f.Show();
				bool drawn = await ViewerTests.Until(() => f.ThumbsDone && f.BigDone && f.View.Picture != null);
				t.Check("several: the window shows the pages of both files", drawn && f.Grid.Tiles.Count == 5, $"{f.Grid.Tiles.Count} pages");
				t.Check("several: under each page, its file and its page there", f.Grid.LabelOf?.Invoke(f.Grid.Tiles[3]) == T.S("viewer.pageOf", "sb", 2), f.Grid.LabelOf?.Invoke(f.Grid.Tiles[3]));
				f.Close();
			});
		}

		// ------------------------------------------------------------------ the languages of Tesseract

		static Task Languages(DevTests.Ctx t)
		{
			string before = ToolSettings.Text(TessLanguages.Key);
			try
			{
				ToolSettings.Set(TessLanguages.Key, "");
				t.Check("tesseract languages: nothing chosen → the program's language (and English)", TessLanguages.Current() == TessData.LanguagesFor(T.Code), TessLanguages.Current());
				TessLanguages.Set(new[] { "spa", "jpn" });
				t.Check("tesseract languages: the ones chosen are used", TessLanguages.Current() == "spa+jpn", TessLanguages.Current());
				ToolSettings.Set(TessLanguages.Key, "xyz+por+../../etc");
				t.Check("tesseract languages: an unknown name is never used (nor a path)", TessLanguages.Current() == "por", TessLanguages.Current());
				var all = TessData.All();
				t.Check("tesseract languages: every language of the program can be chosen, once", all.Count == all.Select(x => x.tess).Distinct().Count() && all.Count >= 20, all.Count.ToString());
				t.Check("tesseract languages: their names in their own language", TessLanguages.NameOf("pt-BR") == "Português" && TessLanguages.NameOf("ja").Length > 0, TessLanguages.NameOf("pt-BR"));
			}
			finally { ToolSettings.Set(TessLanguages.Key, before); }
			return Task.CompletedTask;
		}

		// ------------------------------------------------------------------ a stronger way for scans

		static async Task Scans(DevTests.Ctx t)
		{
			// a scan stored badly (big lossless colour pictures): redrawing it at 150 dpi is much smaller
			string png = t.Picture("scan.png", "SCANNED PAGE", Color.FromArgb(250, 248, 240), 2480, 3508);
			string scan = Path.Combine(t.Dir, "scan.pdf");
			await PdfWork.FromImages(new[] { png, png }, scan);
			t.Check("scans: a PDF of pictures is seen as a scan", PdfRaster.ScanOnly(scan));
			string redrawn = await PdfRaster.Make(scan, null, default);
			try
			{
				t.Check("scans: every page redrawn, the same count and size", redrawn != null && PdfWork.Pages(redrawn) == 2 && Math.Abs(Width(redrawn, 1) - Width(scan, 1)) <= 1, redrawn == null ? "null" : Width(redrawn, 1) + " / " + Width(scan, 1));
				t.Info($"scans: {new FileInfo(scan).Length / 1024} KB → redrawn {(redrawn == null ? 0 : new FileInfo(redrawn).Length / 1024)} KB");
			}
			finally { if (redrawn != null) File.Delete(redrawn); }

			// a PDF with real text is never redrawn (its text would be lost)
			string text = Path.Combine(t.Dir, "text.pdf");
			using (var doc = new PdfSharp.Pdf.PdfDocument())
			{
				var p = doc.AddPage();
				using var g = XGraphics.FromPdfPage(p);
				g.DrawString("This page has real text that can be selected and searched, so it stays text.", new XFont("Arial", 12), XBrushes.Black, 40, 60);
				g.Dispose();
				doc.Save(text);
			}
			t.Check("scans: a PDF with real text is not a scan", !PdfRaster.ScanOnly(text));
			var r = await Jobs.Run("compress", new[] { text }, null);
			t.Check("scans: and “make smaller” keeps its text (not redrawn)", !r.ok || Keeps(r.outputs[0]), r.message);
		}

		static bool Keeps(string pdf)
		{
			using var d = UglyToad.PdfPig.PdfDocument.Open(pdf);
			return d.GetPage(1).Text.Contains("real text");
		}

		// ------------------------------------------------------------------ ask about the file

		static async Task AskAiTest(DevTests.Ctx t)
		{
			string doc = Path.Combine(t.Dir, "contract.txt");
			File.WriteAllText(doc, "Contract.\nThe rent is due on the fifth day of each month.");
			var ids = Jobs.Plan(new[] { doc }).Select(j => j.Id).ToList();
			t.Check("askai plan: a document can be asked about, right after summarize", ids.Contains("askai") && ids.IndexOf("askai") == ids.IndexOf("summarize") + 1, string.Join(",", ids));
			string pic = t.Picture("p.png", "P", Color.White, 200, 200);
			t.Check("askai plan: not for a picture, nor for two files", !Jobs.Plan(new[] { pic }).Any(j => j.Id == "askai") && !Jobs.Plan(new[] { doc, doc }).Any(j => j.Id == "askai"));

			string shortText = "The rent is due on the fifth day.";
			t.Check("askai: a short text is sent whole", AskAi.Parts(shortText, "When is the rent due?") == shortText);
			var filler = string.Join("\n", Enumerable.Range(1, 400).Select(i => $"Paragraph {i} speaks of weather, gardens and trains."));
			string longText = filler + "\nThe deposit is three months of rent, returned at the end of the lease.\n" + filler;
			string parts = AskAi.Parts(longText, "How much is the deposit?");
			t.Check("askai: a long text → the parts with the question's words, not too long", parts.Contains("deposit is three months") && parts.Length <= AskAi.Whole + 200, parts.Length.ToString());
			string marked = AskAiForm.Plain("## Prazo\nNo **quinto dia** de __cada__ mês.");
			t.Check("askai: the marks of formatted text are taken out", marked == "Prazo\nNo quinto dia de cada mês.", marked);
			t.Check("askai: words without accents and case", AskAi.Words("Quando é o PRAZO?").SetEquals(new[] { "quando", "prazo" }));
			string msg = AskAi.Message(shortText, "And the day?", new List<(string, string)> { ("When is the rent due?", "On the fifth.") });
			t.Check("askai: a follow-up carries the earlier question and answer", msg.Contains("On the fifth.") && msg.Contains("Question: And the day?"));

			// the conversation kept next to the file when the window closes
			string talk = Path.Combine(t.Dir, "talk.txt");
			File.WriteAllText(talk, "❓ When?\n\nOn the fifth.\n");
			var r = await Jobs.Run("askai", new[] { doc }, null, default, Opt("conversation", talk, "last", "On the fifth."));
			t.Check("askai: the questions and answers are kept as “… (questions).txt”", r.ok && r.outputs.Count == 1 && File.ReadAllText(r.outputs[0]).Contains("On the fifth.") && r.preview == "On the fifth." && !File.Exists(talk), r.message);
			r = await Jobs.Run("askai", new[] { doc }, null, default, Opt("other", "1"));
			t.Check("askai: closed without a question → done, nothing written", r.ok && r.outputs.Count == 0, r.message);
		}

		/// <summary>A real question to the real assistant (only with DESKPALS_AI_LIVE=1: it needs the internet and the user's key).</summary>
		static async Task AskLive(DevTests.Ctx t)
		{
			if (Environment.GetEnvironmentVariable("DESKPALS_AI_LIVE") != "1") { t.Skip("askai live: set DESKPALS_AI_LIVE=1 to ask the real assistant"); return; }
			var cfg = AiService.Ready(needModel: true);
			string doc = "LEASE CONTRACT. The tenant pays a rent of 1,200 reais. The rent is due on the fifth day of each month. The deposit is three months of rent.";
			string a1 = await AiService.Chat(cfg, AskAi.System, AskAi.Message(doc, "Qual é o dia do vencimento do aluguel?", new List<(string, string)>()), default, null, 900);
			t.Check("askai live: the answer comes from the document", a1.Contains("5") || a1.ToLowerInvariant().Contains("quinto") || a1.ToLowerInvariant().Contains("cinco"), a1);
			t.Info("askai live answer: " + a1.Replace("\n", " "));
			string a2 = await AiService.Chat(cfg, AskAi.System, AskAi.Message(doc, "E o valor?", new List<(string, string)> { ("Qual é o dia do vencimento do aluguel?", a1) }), default, null, 900);
			t.Check("askai live: a follow-up question is understood", a2.Contains("1.200") || a2.Contains("1,200") || a2.Contains("1200"), a2);
			t.Info("askai live follow-up: " + a2.Replace("\n", " "));
		}
		
		// ------------------------------------------------------------------ the buttons

		static Task Buttons(DevTests.Ctx t) => ViewerTests.OnWindowThread(async () =>
		{
			string pdf = await t.Pdf("b.pdf", "B");
			using (var f = new MainForm(new[] { pdf }))
			{
				f.Show();
				await ViewerTests.Until(() => f.JobButtons.Any());
				var all = f.Controls.OfType<Control>().SelectMany(Flatten).OfType<Button>().ToList();
				t.Check("buttons: every button of the window is drawn with colour emoji", all.Count > 5 && all.All(ButtonLook.IsHooked), $"{all.Count(ButtonLook.IsHooked)}/{all.Count}");
				t.Check("buttons: the job buttons too (made after the window)", f.JobButtons.All(ButtonLook.IsHooked));
				// what is drawn is not GDI's black-and-white glyph: some pixel of the emoji is coloured
				// a job whose emoji has colour (the eye of "See and edit" is grey)
				var b = f.JobButtons.FirstOrDefault(x => x.Name == "searchable") ?? f.JobButtons.First();
				using var bmp = new Bitmap(b.Width, b.Height);
				b.DrawToBitmap(bmp, new Rectangle(0, 0, b.Width, b.Height));
				bool colour = false;
				for (int x = 0; x < bmp.Width && !colour; x++)
					for (int y = 0; y < bmp.Height && !colour; y++)
					{
						var c = bmp.GetPixel(x, y);
						colour = Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) > 90;
					}
				t.Check("buttons: the emoji are in colour", colour, b.Text);
				f.Close();
			}
		});

		static IEnumerable<Control> Flatten(Control c) => new[] { c }.Concat(c.Controls.OfType<Control>().SelectMany(Flatten));
	}
}
