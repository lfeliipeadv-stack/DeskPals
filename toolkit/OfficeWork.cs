using System.Diagnostics;
using System.IO.Compression;

namespace DeskPalsTools
{
	/// <summary>
	/// Office documents to PDF with the program that opens them: Microsoft Word, Excel or PowerPoint when installed
	/// (their own "Save as PDF", the same result as by hand), otherwise LibreOffice. Nothing is changed in the document.
	/// </summary>
	internal static class OfficeWork
	{
		static readonly string[] Word = { ".doc", ".docx", ".docm", ".rtf", ".odt" };
		static readonly string[] Excel = { ".xls", ".xlsx", ".xlsm", ".ods", ".csv" };

		public static async Task ToPdf(string file, string pdf)
		{
			file = Path.GetFullPath(file); pdf = Path.GetFullPath(pdf);
			string ext = Jobs.Ext(file);
			string progId = Word.Contains(ext) ? "Word.Application" : Excel.Contains(ext) ? "Excel.Application" : "PowerPoint.Application";
			Exception officeError = null;
			if (Type.GetTypeFromProgID(progId) != null)
			{
				try { await Sta(() => WithOffice(progId, file, pdf)); if (File.Exists(pdf)) return; }
				catch (Exception ex) { officeError = ex; }
			}
			string soffice = LibreOffice();
			if (soffice != null) { await WithLibreOffice(soffice, file, pdf); return; }
			throw officeError != null ? new InvalidOperationException(T.S("err.office", officeError.Message)) : new InvalidOperationException(T.S("err.noOffice"));
		}

		/// <summary>Microsoft Office by automation, hidden, closed at the end (also when it fails).</summary>
		static void WithOffice(string progId, string file, string pdf)
		{
			dynamic app = Activator.CreateInstance(Type.GetTypeFromProgID(progId));
			try
			{
				switch (progId)
				{
					case "Word.Application":
						app.Visible = false; app.DisplayAlerts = 0;
						dynamic doc = app.Documents.Open(file, false, true, false);   // ConfirmConversions, ReadOnly, AddToRecentFiles
						try { doc.ExportAsFixedFormat(pdf, 17); }   // wdExportFormatPDF
						finally { doc.Close(false); }
						break;
					case "Excel.Application":
						app.Visible = false; app.DisplayAlerts = false;
						dynamic book = app.Workbooks.Open(file, 0, true);   // UpdateLinks, ReadOnly
						try { book.ExportAsFixedFormat(0, pdf); }   // xlTypePDF
						finally { book.Close(false); }
						break;
					default:
						dynamic pres = app.Presentations.Open(file, -1, 0, 0);   // ReadOnly, Untitled, WithWindow (msoTrue = -1)
						try { pres.SaveAs(pdf, 32); }   // ppSaveAsPDF
						finally { pres.Close(); }
						break;
				}
			}
			finally
			{
				try { app.Quit(); } catch (Exception) { }
				System.Runtime.InteropServices.Marshal.FinalReleaseComObject(app);
			}
		}

		/// <summary>Office automation wants a thread of its own (STA); the window stays free meanwhile.</summary>
		static Task Sta(Action work)
		{
			var done = new TaskCompletionSource();
			var t = new Thread(() => { try { work(); done.SetResult(); } catch (Exception ex) { done.SetException(ex); } }) { IsBackground = true };
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			return done.Task;
		}

		public static string LibreOffice()
		{
			foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
			{
				if (string.IsNullOrEmpty(root)) continue;
				string p = Path.Combine(root, "LibreOffice", "program", "soffice.exe");
				if (File.Exists(p)) return p;
			}
			return null;
		}

		/// <summary>Which program turns Office files into PDF here ("Microsoft Word", "LibreOffice"), or null.</summary>
		public static string Converter(string ext)
		{
			string progId = Word.Contains(ext) ? "Word.Application" : Excel.Contains(ext) ? "Excel.Application" : "PowerPoint.Application";
			if (Type.GetTypeFromProgID(progId) != null) return "Microsoft " + progId.Split('.')[0];
			return LibreOffice() != null ? "LibreOffice" : null;
		}

		static async Task WithLibreOffice(string soffice, string file, string pdf)
		{
			string outDir = Path.Combine(Path.GetTempPath(), "deskpals-tools-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(outDir);
			try
			{
				var start = new ProcessStartInfo(soffice) { UseShellExecute = false, CreateNoWindow = true };
				foreach (var a in new[] { "--headless", "--norestore", "--convert-to", "pdf", "--outdir", outDir, file }) start.ArgumentList.Add(a);
				using var p = Process.Start(start);
				using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
				await p.WaitForExitAsync(timeout.Token);
				string made = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file) + ".pdf");
				if (!File.Exists(made)) throw new InvalidOperationException(T.S("err.office", "LibreOffice"));
				File.Move(made, pdf);
			}
			finally { try { Directory.Delete(outDir, true); } catch (Exception) { } }
		}
	}

	/// <summary>Files and folders to a ZIP, and a ZIP to a folder (never outside it).</summary>
	internal static class ZipWork
	{
		public static void Zip(IEnumerable<string> items, string zip)
		{
			using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
			foreach (var item in items)
			{
				if (File.Exists(item)) archive.CreateEntryFromFile(item, Path.GetFileName(item), CompressionLevel.Optimal);
				else if (Directory.Exists(item))
				{
					string root = Path.GetFullPath(item).TrimEnd('\\');
					string name = Path.GetFileName(root);
					foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
						archive.CreateEntryFromFile(f, name + "/" + Path.GetRelativePath(root, f).Replace('\\', '/'), CompressionLevel.Optimal);
				}
			}
		}

		public static void Unzip(string zip, string folder)
		{
			string root = Path.GetFullPath(folder).TrimEnd('\\') + "\\";
			using var archive = ZipFile.OpenRead(zip);
			foreach (var e in archive.Entries)
			{
				string target = Path.GetFullPath(Path.Combine(root, e.FullName));
				// a name with ".." would write outside the folder ("zip slip"): skipped
				if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.FullName.EndsWith('/')) { Directory.CreateDirectory(target); continue; }
				Directory.CreateDirectory(Path.GetDirectoryName(target));
				e.ExtractToFile(target, true);
			}
		}
	}
}
