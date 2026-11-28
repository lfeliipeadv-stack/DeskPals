using System.Text.Json;

namespace DeskPalsTools
{
	/// <summary>
	/// What can be done with some files, and doing it. Every job is a <see cref="JobDef"/> registered by its own file
	/// (a [ModuleInitializer] calls <see cref="Register"/>), so each feature lives in a file of its own: its texts in
	/// lang\&lt;code&gt;.&lt;feature&gt;.txt and its tests in <see cref="DevTests.Add"/>. The results go next to the first file, with a
	/// name that never overwrites anything ("Contract.pdf", "Contract (2).pdf"…). DeskPals asks the list ("--list"),
	/// shows it in a bubble and then asks for one ("--do").
	/// </summary>
	internal static class Jobs
	{
		public static readonly string[] Office = { ".doc", ".docx", ".docm", ".rtf", ".odt", ".xls", ".xlsx", ".xlsm", ".ods", ".csv", ".ppt", ".pptx", ".pptm", ".odp" };
		public static readonly string[] Images = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".heic", ".heif", ".jfif" };
		public static readonly string[] Audio = { ".ogg", ".opus", ".oga", ".m4a", ".aac", ".wav", ".wma", ".flac", ".amr", ".mp3", ".3gp" };
		public static readonly string[] Video = { ".mp4", ".mov", ".wmv", ".avi", ".mkv", ".webm", ".m4v" };
		public static readonly string[] Text = { ".txt", ".md", ".csv", ".log", ".html", ".htm", ".json", ".xml" };

		public static string Ext(string f) => Path.GetExtension(f).ToLowerInvariant();
		public static bool IsPdf(string f) => Ext(f) == ".pdf";
		public static bool IsOffice(string f) => Office.Contains(Ext(f));
		public static bool IsWord(string f) => Ext(f) is ".doc" or ".docx" or ".docm" or ".rtf" or ".odt";
		public static bool IsImage(string f) => Images.Contains(Ext(f));
		public static bool IsAudio(string f) => Audio.Contains(Ext(f));
		public static bool IsVideo(string f) => Video.Contains(Ext(f));
		public static bool IsText(string f) => Text.Contains(Ext(f));
		public static bool IsZip(string f) => Ext(f) == ".zip";
		/// <summary>Can become a page of a PDF.</summary>
		public static bool IsPageable(string f) => IsPdf(f) || IsOffice(f) || IsImage(f);

		// ------------------------------------------------------------------ the registry

		/// <summary>
		/// One kind of job. <see cref="Rank"/> says whether it fits these files (-1: no) and where it goes in the list (lower
		/// first: the pet's bubble shows the first four). <see cref="Ask"/>, when set, asks what it needs (pages, a password,
		/// the text of a watermark…) before running: null = the user canceled. <see cref="Run"/> does it.
		/// </summary>
		public sealed class JobDef
		{
			public string Id, Emoji;
			public Func<IList<string>, int> Rank;
			public Func<IWin32Window, IList<string>, Dictionary<string, string>> Ask;
			public Func<JobContext, Task> Run;
			/// <summary>Only in the window or from Explorer, never in the pet's short list (jobs that need a lot of choices).</summary>
			public bool WindowOnly;
				/// <summary>A name of its own (a recipe the user made), instead of the texts "job.&lt;id&gt;" and "doing.&lt;id&gt;".</summary>
				public string Name;
			}

		static readonly List<JobDef> registry = new();

		public static void Register(JobDef def)
		{
			lock (registry)
			{
				registry.RemoveAll(d => d.Id == def.Id);
				registry.Add(def);
			}
		}

		public static JobDef Find(string id) { lock (registry) return registry.FirstOrDefault(d => d.Id == id); }
		public static List<JobDef> All() { lock (registry) return registry.ToList(); }
		public static void Unregister(string id) { lock (registry) registry.RemoveAll(d => d.Id == id); }
		
		/// <summary>What a job is called ("Convert to PDF", or a recipe's own name), and what it says while it runs.</summary>
		public static string TextOf(string id) => Find(id ?? "")?.Name ?? T.S("job." + id);
		public static string DoingOf(string id) => Find(id ?? "")?.Name is string n ? n + "…" : T.S("doing." + id);

		/// <summary>One thing that can be done: its id (for "--do"), the text for the user and an emoji.</summary>
		public sealed record Job(string Id, string Emoji, bool WindowOnly = false)
		{
			public string Text => TextOf(Id);
		}

		/// <summary>The jobs for these files, the most useful first ("zip" always last).</summary>
		public static List<Job> Plan(IList<string> files)
		{
			if (files == null || files.Count == 0) return new List<Job>();
			var ok = files.Where(f => File.Exists(f) || Directory.Exists(f)).ToList();
			if (ok.Count == 0) return new List<Job>();
			return All()
				.Select(d => { int r; try { r = d.Rank?.Invoke(ok) ?? -1; } catch (Exception) { r = -1; } return (d, r); })
				.Where(x => x.r >= 0)
				.OrderBy(x => x.r).ThenBy(x => x.d.Id, StringComparer.Ordinal)
				.Select(x => new Job(x.d.Id, x.d.Emoji, x.d.WindowOnly))
				.ToList();
		}

		/// <summary>What a job gives back to DeskPals (and to the window): done, the files made, a message.</summary>
		public sealed class Result
		{
			public bool ok { get; set; }
			public List<string> outputs { get; set; } = new();
			public string message { get; set; } = "";
			/// <summary>A short text for the pet's bubble (a summary, the start of a transcription), optional.</summary>
			public string preview { get; set; } = "";
			public string Json() => JsonSerializer.Serialize(this);
		}

		/// <summary>Progress for the window: (done so far 0..1, what is being done).</summary>
		public delegate void Progress(double share, string doing);

		/// <summary>Everything a job needs while it runs, and where it puts what it made.</summary>
		public sealed class JobContext
		{
			public IList<string> Files { get; init; }
			public Dictionary<string, string> Options { get; init; } = new();
			public Progress Report { get; init; } = (_, _) => { };
			public CancellationToken Cancel { get; init; }
			public List<string> Outputs { get; } = new();
			public List<string> Errors { get; } = new();
			/// <summary>A short text for the pet's bubble (see <see cref="Result.preview"/>).</summary>
			public string Preview { get; set; } = "";
			/// <summary>The job answered without making a file (a help that opened the Store…): it still went well.</summary>
			public bool Done { get; set; }

			public string Option(string key, string fallback = "") => Options.TryGetValue(key, out var v) && v != null ? v : fallback;
			public void Fail(string file, Exception ex) => Errors.Add((file == null ? "" : Path.GetFileName(file) + ": ") + Friendly(ex));

			/// <summary>Each file in turn, its errors kept (one file failing does not stop the others).</summary>
			public async Task Each(Func<string, int, Task> work)
			{
				for (int i = 0; i < Files.Count; i++)
				{
					Cancel.ThrowIfCancellationRequested();
					Report(i / (double)Files.Count, Path.GetFileName(Files[i]));
					try { await work(Files[i], i); }
					catch (OperationCanceledException) { throw; }
					catch (Exception ex) { Fail(Files[i], ex); }
				}
			}
		}

		/// <summary>Do one job. Errors of one file do not stop the others: they are told in the message.</summary>
		public static async Task<Result> Run(string id, IList<string> files, Progress progress, CancellationToken cancel = default, Dictionary<string, string> options = null)
		{
			var ctx = new JobContext { Files = files, Options = options ?? new(), Report = progress ?? ((_, _) => { }), Cancel = cancel };
			try
			{
				var def = Find(id) ?? throw new InvalidOperationException(T.S("err.unknownJob", id));
				await def.Run(ctx);
			}
			catch (OperationCanceledException) { ctx.Errors.Add(T.S("err.canceled")); }
			catch (Exception ex) { ctx.Errors.Add(Friendly(ex)); }
			ctx.Report(1, "");
			// some done and some failed: it is done, and the message tells which ones failed
			var r = new Result { ok = ctx.Outputs.Count > 0 || (ctx.Done && ctx.Errors.Count == 0), outputs = ctx.Outputs, message = string.Join("\n", ctx.Errors), preview = ctx.Preview ?? "" };
			if (r.ok) History.Add(id, files, r);
			return r;
		}

		/// <summary>A PDF from an Office document (Word, Excel, PowerPoint or LibreOffice), a picture or a PDF (copied).</summary>
		public static async Task<string> ToPdf(string file, string pdf)
		{
			if (IsPdf(file)) { File.Copy(file, pdf, false); return pdf; }
			if (IsImage(file)) { await PdfWork.FromImages(new[] { file }, pdf); return pdf; }
			if (IsOffice(file)) { await OfficeWork.ToPdf(file, pdf); return pdf; }
			throw new InvalidOperationException(T.S("err.notForPdf"));
		}

		/// <summary>A new temporary file name (deleted by who asked for it).</summary>
		public static string Temp(string ext) => Path.Combine(Path.GetTempPath(), "deskpals-tools-" + Guid.NewGuid().ToString("N") + ext);

		/// <summary>A message for the user from an error.</summary>
		public static string Friendly(Exception ex) => ex switch
		{
			PdfSharp.Pdf.IO.PdfReaderException => T.S("err.pdfLocked"),
			UnauthorizedAccessException => T.S("err.noAccess"),
			FileNotFoundException => T.S("err.missing"),
			_ => ex.Message,
		};

		/// <summary>"folder\Name.ext", or with a word ("Name (joined).pdf"), and " (2)", " (3)"… when it exists.</summary>
		public static string OutName(string from, string ext, string word = null)
		{
			string dir = Output.Folder(from);
			string name = Path.GetFileNameWithoutExtension(from.TrimEnd('\\', '/')) + (string.IsNullOrEmpty(word) ? "" : " (" + word + ")");
			string path = Path.Combine(dir, name + ext);
			for (int n = 2; File.Exists(path) || Directory.Exists(path); n++) path = Path.Combine(dir, name + " (" + n + ")" + ext);
			return path;
		}

		/// <summary>A new folder next to the file ("Name (pages)"), never one that exists.</summary>
		public static string OutFolder(string from, string word)
		{
			string path = OutName(from, "", word);
			Directory.CreateDirectory(path);
			return path;
		}
	}
}
