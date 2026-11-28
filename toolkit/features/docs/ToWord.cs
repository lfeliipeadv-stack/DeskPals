using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "toword": a PDF → an editable Word document (.docx). Microsoft Word opens PDFs itself (it rebuilds the paragraphs),
	/// so it is asked to, hidden; without Word, LibreOffice does it. The PDF is never changed and nothing is overwritten.
	/// </summary>
	internal static class ToWord
	{
		/// <summary>How long Word or LibreOffice may take before it is closed (a big PDF takes a while; a stuck one never ends).</summary>
		internal static TimeSpan Timeout = TimeSpan.FromMinutes(4);

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "toword", Emoji = "📝",
				Rank = f => f.Count == 1 && File.Exists(f[0]) && IsPdf(f[0]) ? 30 : -1,
				Run = c => c.Each(async (file, i) =>
				{
					string o = OutName(file, ".docx");
					// a scan (only pictures): its text read first, so the document can be edited (Word would give pictures)
					// a PDF whose maker forbids copying its text is not read, by any way
					if (!NeedsPassword(file) && ForbidsCopy(file)) throw new InvalidOperationException(T.S("docs.word.protected"));
					if (PdfRaster.ScanOnly(file) && !NeedsPassword(file)) await FromScan(file, o, c);
					else
					{
						try { await Convert(file, o, c.Cancel); }
						catch (InvalidOperationException ex) when (ex.Message == T.S("docs.word.none"))
						{
							// no Word and no LibreOffice here: the text of the PDF, page by page, in a document made here
							FromText(file, o);
							c.Errors.Add(T.S("docs.word.textOnly"));
						}
					}
					c.Outputs.Add(o);
				}),
			});
		}

		/// <summary>A scanned PDF as an editable document: each page read by the OCR, its lines joined in paragraphs.</summary>
		static async Task FromScan(string pdf, string docx, JobContext c)
		{
			void Notice(string s) { lock (c.Errors) if (!c.Errors.Contains(s)) c.Errors.Add(s); }
			using var engine = await OcrText.Open((p, s) => c.Report(0, s), c.Cancel, Notice);
			var pages = await OcrText.ReadPdfPages(engine, pdf, (p, s) => c.Report(p * 0.95, s), c.Cancel);
			if (pages.All(p => p.Count(char.IsLetterOrDigit) < 3)) throw new InvalidOperationException(T.S("docs.word.noText"));
			OfficeFiles.Docx(docx, Paragraphs(pages), T.Code);
		}
		
		/// <summary>A PDF with text, without Word: its text page by page (the layout is not kept).</summary>
		static void FromText(string pdf, string docx)
		{
			using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
			var pages = doc.GetPages().Select(p => UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(p)).ToList();
			OfficeFiles.Docx(docx, Paragraphs(pages), T.Code);
		}
		
		/// <summary>
		/// The lines of each page joined in paragraphs: a line goes on with the next one unless it is short (the end of a
		/// paragraph, a title), ends a sentence with a colon, or the next one starts a list. Each page starts on a new page.
		/// </summary>
		internal static List<OfficeFiles.Para> Paragraphs(IList<string> pages)
		{
			var list = new List<OfficeFiles.Para>();
			for (int p = 0; p < pages.Count; p++)
			{
				var lines = (pages[p] ?? "").Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).ToList();
				int longest = Math.Max(20, lines.Count == 0 ? 0 : lines.Max(l => l.Length));
				var cur = new System.Text.StringBuilder();
				bool first = true;
				void Close()
				{
					if (cur.Length == 0) return;
					list.Add(new OfficeFiles.Para(cur.ToString(), false, first && p > 0));
					first = false;
					cur.Clear();
				}
				for (int i = 0; i < lines.Count; i++)
				{
					string l = lines[i];
					if (l.Length == 0) { Close(); continue; }
					bool listItem = l.Length > 1 && (l[0] is '•' or '-' or '–' or '*' || (char.IsDigit(l[0]) && l.Length > 2 && (l[1] is '.' or ')' || (char.IsDigit(l[1]) && l[2] is '.' or ')'))));
					if (listItem) Close();
					if (cur.Length > 0)
					{
						// a word cut at the end of the line ("docu-" + "mento") is put back together
						if (cur[^1] == '-' && char.IsLower(l[0])) cur.Length--;
						else cur.Append(' ');
					}
					cur.Append(l);
					if ((l.Length < longest * 0.6 && !l.EndsWith('-')) || l.EndsWith(':')) Close();
				}
				Close();
				if (first && p > 0) list.Add(new OfficeFiles.Para("", false, true));
			}
			return list;
		}
		
		/// <summary>Which program will do it here ("Microsoft Word", "LibreOffice"), or null.</summary>
		public static string Converter() => HasWord() ? "Microsoft Word" : OfficeWork.LibreOffice() != null ? "LibreOffice" : null;

		static bool HasWord() => Type.GetTypeFromProgID("Word.Application") != null;

		/// <summary>The PDF as a .docx at <paramref name="docx"/> (made in a temporary file first, so a failure leaves nothing half-written).</summary>
		public static async Task Convert(string pdf, string docx, CancellationToken cancel)
		{
			pdf = Path.GetFullPath(pdf); docx = Path.GetFullPath(docx);
			// Word would ask for the password in a window nobody sees, and wait until it is closed by force
			DocsFiles.CheckFree(pdf);
			if (NeedsPassword(pdf)) throw new InvalidOperationException(T.S("err.pdfLocked"));
			// Word refuses these after a long wait, with a reason it does not pass on: say it at once
			if (ForbidsCopy(pdf)) throw new InvalidOperationException(T.S("docs.word.protected"));
			string tmp = Temp(".docx");
			try
			{
				Exception wordError = null;
				bool done = false;
				if (HasWord())
				{
					try { await WithWord(pdf, tmp, cancel); done = File.Exists(tmp) && new FileInfo(tmp).Length > 0; }
					catch (OperationCanceledException) { throw; }
					catch (Exception ex) { wordError = ex; }
				}
				if (!done)
				{
					string soffice = OfficeWork.LibreOffice();
					if (soffice != null) { await WithLibreOffice(soffice, pdf, tmp, cancel); done = File.Exists(tmp); }
				}
				if (!done)
					throw wordError is TimeoutException ? wordError
						: wordError != null ? new InvalidOperationException(T.S("docs.word.failed"))
						: new InvalidOperationException(T.S("docs.word.none"));
				File.Move(tmp, docx, false);
			}
			finally { try { File.Delete(tmp); } catch (Exception) { } }
		}

		/// <summary>Whether the PDF can only be opened with a password (PDFsharp is asked; other problems are left to Word).</summary>
		internal static bool NeedsPassword(string pdf)
		{
			bool asked = false;
			try { using var d = PdfSharp.Pdf.IO.PdfReader.Open(pdf, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import, args => { asked = true; args.Abort = true; }); }
			catch (Exception) { }
			return asked;
		}

		/// <summary>
		/// Whether the PDF's author forbade copying its content (an "owner" password, common on bank statements): bit 5 of the
		/// permissions (/P) in its encryption settings is off. Such a PDF opens without a password, but Word will not convert it.
		/// </summary>
		internal static bool ForbidsCopy(string pdf)
		{
			try
			{
				using var d = PdfSharp.Pdf.IO.PdfReader.Open(pdf, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import, args => args.Abort = true);
				// opened without the owner's password, PDFsharp reports the author's permissions
				return !d.SecuritySettings.PermitExtractContent;
			}
			catch (Exception) { return false; }
		}

		// ------------------------------------------------------------------ Microsoft Word

		/// <summary>
		/// Word by automation on a thread of its own (STA), hidden. When it takes too long or the user cancels, the Word
		/// started here (and only it) is closed by force, so nothing stays running.
		/// </summary>
		static async Task WithWord(string pdf, string docx, CancellationToken cancel)
		{
			var started = new HashSet<int>();
			var before = WordProcesses();
			using var quiet = new PdfPromptOff();
			var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var thread = new Thread(() =>
			{
				try { RunWord(pdf, docx, () => { lock (started) foreach (var id in WordProcesses()) if (!before.Contains(id)) started.Add(id); }); done.TrySetResult(); }
				catch (Exception ex) { done.TrySetException(ex); }
			}) { IsBackground = true, Name = "Word" };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			var wait = Task.Delay(Timeout, cancel);
			var first = await Task.WhenAny(done.Task, wait);
			if (first == done.Task) { await done.Task; return; }
			// stuck or canceled: close our Word; the thread then gets an error from the gone Word and ends
			lock (started) foreach (var id in started) try { using var p = Process.GetProcessById(id); p.Kill(); } catch (Exception) { }
			try { await done.Task.WaitAsync(TimeSpan.FromSeconds(15)); } catch (Exception) { }
			cancel.ThrowIfCancellationRequested();
			throw new TimeoutException(T.S("docs.word.slow"));
		}

		static HashSet<int> WordProcesses()
		{
			var set = new HashSet<int>();
			foreach (var p in Process.GetProcessesByName("WINWORD")) { set.Add(p.Id); p.Dispose(); }
			return set;
		}

		static void RunWord(string pdf, string docx, Action noteProcess)
		{
			dynamic app = null, docs = null, doc = null;
			try
			{
				app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application"));
				noteProcess();
				app.Visible = false;
				app.DisplayAlerts = 0;   // wdAlertsNone
				try { app.AutomationSecurity = 3; } catch (Exception) { }   // msoAutomationSecurityForceDisable: no macros
				docs = app.Documents;
				// FileName, ConfirmConversions, ReadOnly, AddToRecentFiles, PasswordDocument: a PDF with a password then fails
				// at once instead of asking for it in a window nobody sees
				doc = docs.Open(pdf, false, true, false, "deskpals-no-password");
				doc.SaveAs2(docx, 16);   // wdFormatXMLDocument (.docx)
			}
			finally
			{
				if (doc != null) { try { doc.Close(false); } catch (Exception) { } Release(doc); }
				if (docs != null) Release(docs);
				if (app != null) { try { app.Quit(false); } catch (Exception) { } Release(app); }
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		static void Release(object o) { try { if (o != null && Marshal.IsComObject(o)) Marshal.FinalReleaseComObject(o); } catch (Exception) { } }

		/// <summary>
		/// Word asks "this will convert your PDF to an editable document" before opening a PDF; hidden, that question could
		/// wait forever. Word's own setting "DisableConvertPdfWarning" turns it off: set only for the conversion and put back
		/// as it was (removed when it was not there) at the end.
		/// </summary>
		sealed class PdfPromptOff : IDisposable
		{
			const string Name = "DisableConvertPdfWarning";
			readonly string keyPath;
			readonly object old;
			readonly bool changed;

			public PdfPromptOff()
			{
				try
				{
					string version = WordVersion();
					if (version == null) return;
					keyPath = @"Software\Microsoft\Office\" + version + @"\Word\Options";
					using var key = Registry.CurrentUser.CreateSubKey(keyPath, true);
					old = key.GetValue(Name);
					if (old is int v && v == 1) return;
					key.SetValue(Name, 1, RegistryValueKind.DWord);
					changed = true;
				}
				catch (Exception) { }
			}

			public void Dispose()
			{
				if (!changed) return;
				try
				{
					using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
					if (key == null) return;
					if (old == null) key.DeleteValue(Name, false);
					else key.SetValue(Name, old, old is int ? RegistryValueKind.DWord : RegistryValueKind.String);
				}
				catch (Exception) { }
			}
		}

		/// <summary>"16.0" from "Word.Application.16" (the Word installed), or null.</summary>
		internal static string WordVersion()
		{
			try
			{
				using var key = Registry.ClassesRoot.OpenSubKey(@"Word.Application\CurVer");
				string cur = key?.GetValue(null) as string;
				if (cur == null) return null;
				string n = cur[(cur.LastIndexOf('.') + 1)..];
				return int.TryParse(n, out _) ? n + ".0" : null;
			}
			catch (Exception) { return null; }
		}

		// ------------------------------------------------------------------ LibreOffice

		/// <summary>
		/// LibreOffice's PDF import (it opens a PDF as a text document) and "save as .docx", hidden, with a profile of its own
		/// so an open LibreOffice does not get in the way; closed by force when it takes too long.
		/// </summary>
		static async Task WithLibreOffice(string soffice, string pdf, string docx, CancellationToken cancel)
		{
			string work = Path.Combine(Path.GetTempPath(), "deskpals-tools-" + Guid.NewGuid().ToString("N"));
			string outDir = Path.Combine(work, "out");
			Directory.CreateDirectory(outDir);
			try
			{
				var start = new ProcessStartInfo(soffice) { UseShellExecute = false, CreateNoWindow = true };
				foreach (var a in new[] { "-env:UserInstallation=" + new Uri(Path.Combine(work, "profile")).AbsoluteUri, "--headless", "--norestore", "--infilter=writer_pdf_import",
					"--convert-to", "docx:MS Word 2007 XML", "--outdir", outDir, pdf })
					start.ArgumentList.Add(a);
				using var p = Process.Start(start);
				using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancel);
				limit.CancelAfter(Timeout);
				try { await p.WaitForExitAsync(limit.Token); }
				catch (OperationCanceledException)
				{
					try { p.Kill(true); } catch (Exception) { }
					cancel.ThrowIfCancellationRequested();
					throw new TimeoutException(T.S("docs.word.slow"));
				}
				string made = Path.Combine(outDir, Path.GetFileNameWithoutExtension(pdf) + ".docx");
				if (!File.Exists(made)) throw new InvalidOperationException(T.S("docs.word.failed"));
				File.Move(made, docx, true);
			}
			finally { try { Directory.Delete(work, true); } catch (Exception) { } }
		}
	}
}
