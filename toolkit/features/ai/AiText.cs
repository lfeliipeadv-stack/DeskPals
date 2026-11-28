using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DeskPalsTools
{
	/// <summary>
	/// The words of a document, to be summarized: PDFs (their text, or the OCR of Windows when they are scans), Word and
	/// LibreOffice documents (.docx and .odt read directly; .doc and .rtf by Word, the RTF control, or LibreOffice) and
	/// text files. Nothing is changed in the document.
	/// </summary>
	internal static class AiText
	{
		public static bool CanRead(string f) => Jobs.IsPdf(f) || Jobs.IsWord(f) || Jobs.IsText(f);

		public static async Task<string> Read(string file, Action<double, string> progress, CancellationToken cancel)
		{
			string ext = Jobs.Ext(file);
			try
			{
				string text = ext switch
				{
					".pdf" => await Pdf(file, progress, cancel),
					".docx" or ".docm" => Docx(file),
					".odt" => Odt(file),
					".rtf" => await Rtf(file, cancel),
					".doc" => await WithWordOrLibreOffice(file),
					".html" or ".htm" => Html(Decode(await Bytes(file, cancel))),
					_ => Decode(await Bytes(file, cancel)),
				};
				return Tidy(text);
			}
			// a document open in Word (or another program that does not let others read it): said in words, without the path
			catch (IOException ex) when (ex is not FileNotFoundException and not DirectoryNotFoundException) { throw new InvalidOperationException(T.S("err.noAccess")); }
			catch (UnauthorizedAccessException) { throw new InvalidOperationException(T.S("err.noAccess")); }
		}

		/// <summary>The file, read even while another program has it open for writing (Word does): it is only read.</summary>
		static FileStream Open(string file) => new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

		static async Task<byte[]> Bytes(string file, CancellationToken cancel)
		{
			using var s = Open(file);
			using var ms = new MemoryStream();
			await s.CopyToAsync(ms, cancel);
			return ms.ToArray();
		}

		static AiText() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		/// <summary>
		/// The text of a file: what its first bytes say (a BOM), else UTF-8, else Windows' old encoding (Windows-1252:
		/// older Notepad files and spreadsheet exports), so "ação" never comes out as "a��o".
		/// </summary>
		public static string Decode(byte[] bytes)
		{
			if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
			if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
			if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
			try { return new UTF8Encoding(false, true).GetString(bytes); }
			catch (DecoderFallbackException) { return Encoding.GetEncoding(1252).GetString(bytes); }
		}

		/// <summary>No runs of empty lines or spaces (fewer words to send, same meaning).</summary>
		static string Tidy(string s) => Regex.Replace(Regex.Replace((s ?? "").Replace("\r\n", "\n"), @"[ \t]+", " "), @"\n\s*\n\s*\n+", "\n\n").Trim();

		// ------------------------------------------------------------------ PDF

		/// <summary>The text of every page; a PDF with (almost) no text is a scan, read with the OCR of Windows.</summary>
		static async Task<string> Pdf(string file, Action<double, string> progress, CancellationToken cancel)
		{
			var sb = new StringBuilder();
			int pages;
			// opened apart: a file open elsewhere is said by Read, a damaged one below
			using (var stream = Open(file))
			try
			{
				using var doc = PdfDocument.Open(stream);
				pages = doc.NumberOfPages;
				foreach (var page in doc.GetPages())
				{
					cancel.ThrowIfCancellationRequested();
					string t;
					try { t = ContentOrderTextExtractor.GetText(page); } catch (Exception) { t = page.Text; }
					sb.AppendLine(t).AppendLine();
				}
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception) { throw new InvalidOperationException(T.S("err.pdfLocked")); }
			string text = sb.ToString();
			if (text.Count(char.IsLetterOrDigit) >= 20 * Math.Max(1, pages) || text.Count(char.IsLetterOrDigit) >= 2000) return text;
			// a scan: the pictures of the pages are read
			try { return await OcrWork.ReadPdf(file, progress, cancel); }
			catch (OperationCanceledException) { throw; }
			catch (Exception) when (text.Trim().Length > 0) { return text; }
		}

		// ------------------------------------------------------------------ Word, LibreOffice

		static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
		static readonly XNamespace Mc = "http://schemas.openxmlformats.org/markup-compatibility/2006";

		/// <summary>One part of a .docx or .odt (a zip), opened even while Word or LibreOffice has the file open.</summary>
		static XDocument ZipPart(string file, string name)
		{
			try
			{
				using var zip = new ZipArchive(Open(file), ZipArchiveMode.Read);
				var entry = zip.GetEntry(name) ?? throw new InvalidDataException();
				using var s = entry.Open();
				return XDocument.Load(s);
			}
			catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException) { throw new InvalidOperationException(T.S("ai.badDocument")); }
		}

		/// <summary>
		/// Word writes a text box twice (a new way, and an old one in mc:Fallback for older programs): only the first is read.
		/// </summary>
		static bool InFallback(XElement e) => e.Ancestors(Mc + "Fallback").Any();

		/// <summary>The paragraphs of a .docx (word/document.xml: text, tabs and line breaks), without Word.</summary>
		public static string Docx(string file)
		{
			var xml = ZipPart(file, "word/document.xml");
			var sb = new StringBuilder();
			foreach (var p in xml.Descendants(W + "p").Where(p => !InFallback(p)))
			{
				// a paragraph inside a text box is a paragraph of its own: read when its turn comes, not twice
				foreach (var e in p.Descendants().Where(e => e.Ancestors(W + "p").First() == p && !InFallback(e)))
				{
					if (e.Name == W + "t") sb.Append(e.Value);
					else if (e.Name == W + "tab") sb.Append('\t');
					else if (e.Name == W + "br" || e.Name == W + "cr") sb.Append('\n');
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}

		static readonly XNamespace OdtText = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

		/// <summary>The paragraphs and titles of an .odt (content.xml), without LibreOffice.</summary>
		static string Odt(string file)
		{
			var xml = ZipPart(file, "content.xml");
			var sb = new StringBuilder();
			foreach (var p in xml.Descendants().Where(e => e.Name == OdtText + "p" || e.Name == OdtText + "h"))
			{
				foreach (var n in p.DescendantNodes())
				{
					if (n is XText t) sb.Append(t.Value);
					else if (n is XElement e && e.Name == OdtText + "tab") sb.Append('\t');
					else if (n is XElement b && b.Name == OdtText + "line-break") sb.Append('\n');
					else if (n is XElement sp && sp.Name == OdtText + "s") sb.Append(' ');
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}

		/// <summary>An RTF read by Windows' own rich text control (no Word needed).</summary>
		static async Task<string> Rtf(string file, CancellationToken cancel)
		{
			// RTF is plain ASCII (accents are written as codes): read byte for byte
			string rtf = Encoding.Latin1.GetString(await Bytes(file, cancel));
			return await Sta(() =>
			{
				using var box = new RichTextBox();
				box.Rtf = rtf;
				return box.Text;
			}, TimeSpan.FromMinutes(1));
		}

		/// <summary>An old .doc: Word (hidden, read-only), else LibreOffice to plain text.</summary>
		static async Task<string> WithWordOrLibreOffice(string file)
		{
			file = Path.GetFullPath(file);
			if (Type.GetTypeFromProgID("Word.Application") != null)
			{
				try { return await Sta(() => WithWord(file), TimeSpan.FromMinutes(3)); }
				catch (Exception) when (OfficeWork.LibreOffice() != null) { }
			}
			string soffice = OfficeWork.LibreOffice() ?? throw new InvalidOperationException(T.S("ai.noWord"));
			return await WithLibreOffice(soffice, file);
		}

		static string WithWord(string file)
		{
			dynamic app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application"));
			try
			{
				app.Visible = false; app.DisplayAlerts = 0;
				dynamic doc = app.Documents.Open(file, false, true, false);   // ConfirmConversions, ReadOnly, AddToRecentFiles
				try { return (string)doc.Content.Text; }
				finally { doc.Close(false); System.Runtime.InteropServices.Marshal.FinalReleaseComObject(doc); }
			}
			finally
			{
				try { app.Quit(false); } catch (Exception) { }
				System.Runtime.InteropServices.Marshal.FinalReleaseComObject(app);
			}
		}

		static async Task<string> WithLibreOffice(string soffice, string file)
		{
			string outDir = Path.Combine(Path.GetTempPath(), "deskpals-tools-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(outDir);
			try
			{
				var start = new ProcessStartInfo(soffice) { UseShellExecute = false, CreateNoWindow = true };
				foreach (var a in new[] { "--headless", "--norestore", "--convert-to", "txt:Text (encoded):UTF8", "--outdir", outDir, file }) start.ArgumentList.Add(a);
				using var p = Process.Start(start);
				using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
				try { await p.WaitForExitAsync(timeout.Token); }
				catch (OperationCanceledException) { try { p.Kill(true); } catch (Exception) { } }
				string made = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file) + ".txt");
				if (!File.Exists(made)) throw new InvalidOperationException(T.S("err.office", "LibreOffice"));
				return await File.ReadAllTextAsync(made);
			}
			finally { try { Directory.Delete(outDir, true); } catch (Exception) { } }
		}

		/// <summary>Word and the RTF control want a thread of their own (STA), and may never hang the job.</summary>
		static async Task<string> Sta(Func<string> work, TimeSpan limit)
		{
			var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
			var t = new Thread(() => { try { done.SetResult(work()); } catch (Exception ex) { done.SetException(ex); } }) { IsBackground = true };
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			if (await Task.WhenAny(done.Task, Task.Delay(limit)) != done.Task) throw new InvalidOperationException(T.S("ai.badDocument"));
			return await done.Task;
		}

		// ------------------------------------------------------------------ web pages

		static string Html(string html)
		{
			html = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			html = Regex.Replace(html, @"<(br|/p|/div|/h\d|/li|/tr)[^>]*>", "\n", RegexOptions.IgnoreCase);
			return WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
		}
	}
}
