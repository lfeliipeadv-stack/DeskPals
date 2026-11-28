using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// "DeskPalsTools.exe --devtests[=report]": every job on files made here (pictures with words, PDFs, a short sound),
	/// in a folder of the temp folder that is removed at the end. The exit code is the number of failures.
	/// </summary>
	internal static class DevTests
	{
		/// <summary>What a test gets: its own empty folder, Check/Skip/Info, and files to work on.</summary>
		public sealed class Ctx
		{
			internal List<string> Lines;
			internal int Fails;
			public string Dir { get; internal set; }
			public void Check(string name, bool ok, string detail = "") { lock (Lines) { Lines.Add((ok ? "PASS  " : "FAIL  ") + name + (ok || string.IsNullOrEmpty(detail) ? "" : "  →  " + detail)); if (!ok) Fails++; } }
			public void Skip(string text) { lock (Lines) Lines.Add("SKIP  " + text); }
			public void Info(string text) { lock (Lines) Lines.Add("INFO  " + text); }
			/// <summary>A picture with big black words on a colour (PNG, or JPEG for ".jpg").</summary>
			public string Picture(string name, string words, Color back, int w = 1200, int h = 400) => DevTests.Picture(Path.Combine(Dir, name), words, back, w, h);
			/// <summary>Seconds of a 440 Hz tone (16-bit mono WAV).</summary>
			public string Tone(string name, double seconds = 2) => DevTests.Tone(Path.Combine(Dir, name), seconds);
			/// <summary>A PDF made of pictures with these words, one page each.</summary>
			public async Task<string> Pdf(string name, params string[] pageWords)
			{
				var pics = pageWords.Select((w, i) => Picture(name + "-p" + i + ".png", w, Color.White)).ToList();
				string pdf = Path.Combine(Dir, name);
				await PdfWork.FromImages(pics, pdf);
				return pdf;
			}
			public Size SizeOf(string path) => DevTests.Size(path);
		}

		static readonly List<(string name, Func<Ctx, Task> test)> tests = new();

		/// <summary>A test of a feature (registered by its own file, from a [ModuleInitializer]).</summary>
		public static void Add(string name, Func<Ctx, Task> test) { lock (tests) tests.Add((name, test)); }

		/// <summary>Every test, or only those whose name contains <paramref name="only"/>. The exit code is the number of failures.</summary>
		public static int Run(string report, string only = null)
		{
			var lines = new List<string>();
			string root = Path.Combine(Path.GetTempPath(), "deskpals-tools-tests-" + Environment.ProcessId);
			int fails = 0;
			Directory.CreateDirectory(root);
			// the tests never touch the user's own settings and history
			ToolSettings.File = Path.Combine(root, "tools.json");
			History.File = Path.Combine(root, "tools-history.json");
			List<(string name, Func<Ctx, Task> test)> list;
			lock (tests) list = tests.ToList();
			foreach (var (name, test) in list)
			{
				if (only != null && name.IndexOf(only, StringComparison.OrdinalIgnoreCase) < 0) continue;
				var ctx = new Ctx { Lines = lines, Dir = Path.Combine(root, name.Replace(' ', '-')) };
				Directory.CreateDirectory(ctx.Dir);
				try { Task.Run(() => test(ctx)).GetAwaiter().GetResult(); }
				catch (Exception ex) { ctx.Check(name + ": no exception", false, ex.ToString()); }
				fails += ctx.Fails;
			}
			try { Directory.Delete(root, true); } catch (Exception) { }
			lines.Insert(0, $"DeskPals Tools tests {DateTime.Now:yyyy-MM-dd HH:mm:ss}  —  {fails} failure(s)");
			File.WriteAllLines(report ?? Path.Combine(Path.GetTempPath(), "DeskPalsTools-devtests.txt"), lines);
			Ending.Now();
			return fails;
		}

		[System.Runtime.CompilerServices.ModuleInitializer]
		internal static void Init() => Add("core", Core);

		/// <summary>The first jobs: to PDF, join, split, turn, pictures, OCR, ZIP, MP3, Office, and the texts of every language.</summary>
		static async Task Core(Ctx t)
		{
			string dir = t.Dir;
			void Check(string name, bool ok, string detail = "") => t.Check(name, ok, detail);
			void Skip(string text) => t.Skip(text);
			var lines = new InfoLines(t);
			{
				{
					// --- the jobs offered
					string[] Ids(params string[] names)
					{
						var paths = names.Select(n => Path.Combine(dir, n)).ToList();
						foreach (var f in paths) if (!File.Exists(f)) File.WriteAllText(f, "");
						return Jobs.Plan(paths).Select(j => j.Id).ToArray();
					}
					// the order checks look only at the core jobs (the features add their own in between)
					var coreIds = new HashSet<string> { "topdf", "merge", "unzip", "ocr", "smaller", "tojpg", "topng", "tomp3", "split", "rotate", "zip" };
					bool IsCoreJob(string id) => coreIds.Contains(id);
					string a = Picture(Path.Combine(dir, "a.png"), "DESKPALS 2026", Color.White);
					string b = Picture(Path.Combine(dir, "b.jpg"), "HELLO PETS", Color.LightYellow);
					Check("plan: a Word file → PDF first", Ids("x.docx").FirstOrDefault() == "topdf");
					Check("plan: a PDF → read, split (turning one PDF is in See and edit)", Ids("x.pdf").Where(IsCoreJob).Take(2).SequenceEqual(new[] { "ocr", "split" }) && !Ids("x.pdf").Contains("rotate"), string.Join(",", Ids("x.pdf")));
					Check("plan: several PDFs → turn them all at once", Ids("x.pdf", "y.pdf").Contains("rotate"), string.Join(",", Ids("x.pdf", "y.pdf")));
					Check("plan: a picture → PDF, read, smaller, another format", Ids("a.png").Where(IsCoreJob).Take(4).SequenceEqual(new[] { "topdf", "ocr", "smaller", "tojpg" }), string.Join(",", Ids("a.png")));
					Check("plan: a WhatsApp audio → MP3", Ids("x.ogg").Contains("tomp3"));
					Check("plan: an MP3 is not made MP3 again", !Ids("x.mp3").Contains("tomp3"));
					Check("plan: many documents → join in one PDF first", Ids("a.png", "b.jpg", "x.pdf").FirstOrDefault() == "merge", string.Join(",", Ids("a.png", "b.jpg", "x.pdf")));
					Check("plan: a ZIP → extract, not zip again", Ids("x.zip").SequenceEqual(new[] { "unzip" }));
					Check("plan: anything can be zipped", Ids("x.unknown").SequenceEqual(new[] { "zip" }));

					// --- names that never overwrite
					string taken = Jobs.OutName(a, ".pdf");
					File.WriteAllText(taken, "");
					Check("names: a second one gets (2)", Path.GetFileName(Jobs.OutName(a, ".pdf")) == "a (2).pdf", Jobs.OutName(a, ".pdf"));
					File.Delete(taken);

					// --- pictures to PDF, join, split, turn
					var r = await Jobs.Run("topdf", new[] { a }, null);
					Check("pdf: a picture becomes a PDF", r.ok && r.outputs.Count == 1 && PdfWork.Pages(r.outputs[0]) == 1, r.message);
					string onePdf = r.outputs.FirstOrDefault();
					r = await Jobs.Run("merge", new[] { a, onePdf, b }, null);
					Check("pdf: pictures and a PDF joined, in order", r.ok && PdfWork.Pages(r.outputs[0]) == 3, r.message);
					string three = r.outputs.FirstOrDefault();
					r = await Jobs.Run("split", new[] { three }, null);
					Check("pdf: split in one file per page", r.ok && r.outputs.Count == 3 && r.outputs.All(f => PdfWork.Pages(f) == 1), r.message);
					r = await Jobs.Run("rotate", new[] { onePdf }, null);
					bool turned = false;
					if (r.ok) using (var d = PdfReader.Open(r.outputs[0], PdfDocumentOpenMode.Import)) turned = d.Pages[0].Rotate == 90;
					Check("pdf: pages turned 90°", turned, r.message);
					string broken = Path.Combine(dir, "broken.pdf");
					File.WriteAllText(broken, "not a pdf");
					r = await Jobs.Run("rotate", new[] { broken }, null);
					Check("pdf: a broken PDF says so, nothing crashes", !r.ok && r.message.Length > 0);

					// --- pictures
					r = await Jobs.Run("tojpg", new[] { a }, null);
					Check("pictures: PNG → JPG", r.ok && Size(r.outputs[0]) == new Size(1200, 400) && Jobs.Ext(r.outputs[0]) == ".jpg", r.message);
					string big = Picture(Path.Combine(dir, "big.png"), "BIG", Color.White, 3000, 1000);
					r = await Jobs.Run("smaller", new[] { big }, null);
					Check("pictures: smaller (at most 1920)", r.ok && Size(r.outputs[0]) == new Size(1920, 640), r.ok ? Size(r.outputs[0]).ToString() : r.message);
					r = await Jobs.Run("topng", new[] { b }, null);
					Check("pictures: JPG → PNG", r.ok && Jobs.Ext(r.outputs[0]) == ".png", r.message);

					// --- OCR (the languages of Windows)
					string langs = OcrWork.Languages();
					if (langs.Length == 0) Skip("ocr: no OCR language in this Windows");
					else
					{
						lines.Add("INFO  ocr languages: " + langs);
						r = await Jobs.Run("ocr", new[] { a }, null);
						string text = r.ok ? File.ReadAllText(r.outputs[0]) : "";
						Check("ocr: the words of a picture", text.Replace(" ", "").Contains("DESKPALS", StringComparison.OrdinalIgnoreCase), text.Length > 80 ? text[..80] : text + " " + r.message);
						r = await Jobs.Run("ocr", new[] { three }, null);
						text = r.ok ? File.ReadAllText(r.outputs[0]) : "";
						Check("ocr: the words of every page of a PDF", text.Replace(" ", "").Contains("DESKPALS", StringComparison.OrdinalIgnoreCase) && text.Replace(" ", "").Contains("HELLOPETS", StringComparison.OrdinalIgnoreCase), text.Length > 120 ? text[..120] : text + " " + r.message);
					}

					// --- ZIP
					string folder = Path.Combine(dir, "folder");
					Directory.CreateDirectory(Path.Combine(folder, "inner"));
					File.WriteAllText(Path.Combine(folder, "inner", "note.txt"), "hello");
					r = await Jobs.Run("zip", new[] { a, folder }, null);
					Check("zip: files and a folder", r.ok && ZipFile.OpenRead(r.outputs[0]).Entries.Count == 2, r.message);
					string zipped = r.outputs.FirstOrDefault();
					r = await Jobs.Run("unzip", new[] { zipped }, null);
					Check("zip: extracted to a folder of its own", r.ok && File.ReadAllText(Path.Combine(r.outputs[0], "folder", "inner", "note.txt")) == "hello", r.message);
					string evil = Path.Combine(dir, "evil.zip");
					using (var z = ZipFile.Open(evil, ZipArchiveMode.Create)) { using var w = new StreamWriter(z.CreateEntry("../outside.txt").Open()); w.Write("x"); }
					r = await Jobs.Run("unzip", new[] { evil }, null);
					Check("zip: a name with .. never writes outside", !File.Exists(Path.Combine(dir, "outside.txt")));

					// --- sound to MP3 (Windows' own converter)
					string wav = Tone(Path.Combine(dir, "tone.wav"));
					r = await Jobs.Run("tomp3", new[] { wav }, null);
					Check("audio: WAV → MP3", r.ok && new FileInfo(r.outputs[0]).Length > 1000, r.message);

					// --- Office (only when there is Word or LibreOffice here)
					string rtf = Path.Combine(dir, "letter.rtf");
					File.WriteAllText(rtf, @"{\rtf1\ansi{\fonttbl{\f0 Arial;}}\f0\fs48 Dear pets, hello.\par}");
					string conv = OfficeWork.Converter(".rtf");
					if (conv == null) Skip("office: no Word nor LibreOffice here");
					else
					{
						lines.Add("INFO  office: " + conv);
						r = await Jobs.Run("topdf", new[] { rtf }, null);
						Check("office: a document becomes a PDF (" + conv + ")", r.ok && PdfWork.Pages(r.outputs[0]) >= 1, r.message);
					}

					// --- texts: every language has every key of English
					var en = T.Read("en");
					foreach (var code in T.Codes().Where(c => c != "en"))
					{
						var other = T.Read(code);
						var missing = en.Keys.Where(k => !other.ContainsKey(k)).ToList();
						Check("texts: " + code + " has every text", missing.Count == 0, string.Join(", ", missing.Take(8)));
					}
					var noText = Jobs.All().Select(d => d.Id).Where(j => !en.ContainsKey("job." + j) || !en.ContainsKey("doing." + j)).ToList();
					Check("texts: every job has a name and a \"doing\" line", noText.Count == 0, string.Join(", ", noText));
				}
			}
		}

		/// <summary>The "lines.Add("INFO …")" of the core test.</summary>
		sealed class InfoLines { readonly Ctx t; public InfoLines(Ctx t) { this.t = t; } public void Add(string s) => t.Info(s.StartsWith("INFO  ") ? s[6..] : s); }

		/// <summary>A picture with big black words on a colour.</summary>
		internal static string Picture(string path, string words, Color back, int w = 1200, int h = 400)
		{
			using var bmp = new Bitmap(w, h);
			using (var g = Graphics.FromImage(bmp))
			{
				g.Clear(back);
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
				using var font = new Font("Arial", h / 5f, FontStyle.Bold, GraphicsUnit.Pixel);
				g.DrawString(words, font, Brushes.Black, 40, h / 3f);
			}
			bmp.Save(path, Jobs.Ext(path) == ".jpg" ? ImageFormat.Jpeg : ImageFormat.Png);
			return path;
		}

		internal static Size Size(string path) { using var img = Image.FromFile(path); return img.Size; }

		/// <summary>Seconds of a 440 Hz tone (16-bit mono WAV).</summary>
		internal static string Tone(string path, double seconds = 2)
		{
			int rate = 22050, n = (int)(rate * seconds);
			using var w = new BinaryWriter(File.Create(path));
			w.Write("RIFF"u8.ToArray()); w.Write(36 + n * 2); w.Write("WAVEfmt "u8.ToArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
			w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16); w.Write("data"u8.ToArray()); w.Write(n * 2);
			for (int i = 0; i < n; i++) w.Write((short)(Math.Sin(2 * Math.PI * 440 * i / rate) * 8000));
			return path;
		}
	}
}
