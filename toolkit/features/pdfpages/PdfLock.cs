using System.Collections.Concurrent;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// Passwords on PDFs: is a PDF locked (asks a password to open), only limited (opens, but forbids changes) or open;
	/// putting a password on a copy, and saving an unlocked copy. The strongest kind PDFsharp writes is used (AES 256).
	/// </summary>
	internal static class PdfLock
	{
		public enum State { Open, Limited, Locked, Broken }

		// the jobs' ranks ask this for every job: the PDF is read once per file (and again only when it changed)
		static readonly ConcurrentDictionary<string, (DateTime when, long size, State state)> known = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>What a PDF needs before it can be changed. A missing or damaged file is <see cref="State.Broken"/>.</summary>
		public static State Of(string pdf)
		{
			try
			{
				var info = new FileInfo(pdf);
				if (!info.Exists || info.Length == 0) return State.Broken;
				if (known.TryGetValue(info.FullName, out var k) && k.when == info.LastWriteTimeUtc && k.size == info.Length) return k.state;
				var s = Read(info.FullName);
				known[info.FullName] = (info.LastWriteTimeUtc, info.Length, s);
				return s;
			}
			catch (Exception) { return State.Broken; }
		}

		public static bool IsLocked(string pdf) => Of(pdf) is State.Locked or State.Limited;

		static State Read(string pdf)
		{
			// reading a whole PDF is slow for a big one, and the pet waits for the list: a quick look at the bytes first
			// (a password always leaves "/Encrypt" in plain text, at the end of the file)
			var (isPdf, encrypted) = Peek(pdf);
			if (!isPdf) return State.Broken;
			if (!encrypted) return State.Open;
			bool asked = false;
			try
			{
				// opened to be changed: a PDF with only limits asks the owner's password here too
				using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify, a => { asked = true; a.Abort = true; });
				if (doc != null) return State.Open;
			}
			catch (Exception) { if (!asked) return State.Broken; }
			if (!asked) return State.Broken;
			// it asked: a password to open it at all, or only to change it?
			asked = false;
			try
			{
				using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import, a => { asked = true; a.Abort = true; });
				if (doc != null) return State.Limited;
			}
			catch (Exception) { if (!asked) return State.Broken; }
			return State.Locked;
		}

		/// <summary>Starts like a PDF ("%PDF-" in its first KB), and whether "/Encrypt" is anywhere in it.</summary>
		static (bool isPdf, bool encrypted) Peek(string pdf)
		{
			byte[] mark = "/Encrypt"u8.ToArray(), head = "%PDF-"u8.ToArray();
			using var s = new FileStream(pdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16);
			var buf = new byte[1 << 20];
			int n = s.Read(buf, 0, buf.Length);
			if (buf.AsSpan(0, Math.Min(n, 1024)).IndexOf(head) < 0) return (false, false);
			// a big PDF: its start (where a "fast web view" PDF says it) and its last MBs (the trailer) are enough
			if (s.Length > 16L << 20)
			{
				if (buf.AsSpan(0, n).IndexOf(mark) >= 0) return (true, true);
				var end = new byte[4 << 20];
				s.Seek(-end.Length, SeekOrigin.End);
				int got = 0, more;
				while (got < end.Length && (more = s.Read(end, got, end.Length - got)) > 0) got += more;
				return (true, end.AsSpan(0, got).IndexOf(mark) >= 0);
			}
			while (n > 0)
			{
				if (buf.AsSpan(0, n).IndexOf(mark) >= 0) return (true, true);
				if (n < mark.Length) break;
				// the last bytes stay at the start, so a mark cut in two is still found
				int keep = mark.Length - 1;
				Array.Copy(buf, n - keep, buf, 0, keep);
				int more = s.Read(buf, keep, buf.Length - keep);
				if (more <= 0) break;
				n = keep + more;
			}
			return (true, false);
		}

		/// <summary>A copy of <paramref name="pdf"/> that asks <paramref name="password"/> to be opened.</summary>
		public static void Protect(string pdf, string output, string password) => Protect(pdf, output, password, null, true, true, true);
		
		/// <summary>
		/// A copy with a password to open it (optional when something is limited) and what may be done with it: print, copy
		/// the text, change it. The owner's password allows everything; without one, the password to open it is used, and
		/// with neither (only limits) a random one is made, so the limits hold (the original file stays as it was).
		/// </summary>
		public static void Protect(string pdf, string output, string password, string owner, bool print, bool copy, bool edit)
		{
			bool limited = !print || !copy || !edit;
			if (string.IsNullOrEmpty(password) && !limited) throw new InvalidOperationException(T.S("pdfpages.err.noPassword"));
			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify);
			var s = doc.SecuritySettings;
			if (!string.IsNullOrEmpty(password)) s.UserPassword = password;
			s.OwnerPassword = !string.IsNullOrEmpty(owner) ? owner : !string.IsNullOrEmpty(password) ? password : Guid.NewGuid().ToString("N");
			s.PermitPrint = s.PermitFullQualityPrint = print;
			s.PermitExtractContent = copy;
			s.PermitModifyDocument = s.PermitAssembleDocument = s.PermitAnnotations = s.PermitFormsFill = edit;
			doc.SecurityHandler.SetEncryptionToV5();
			doc.Save(output);
		}

		/// <summary>
		/// A copy of <paramref name="pdf"/> without password nor limits. A wrong password throws a friendly message, and
		/// so does a password that only lets it be read (its maker kept another one for changes).
		/// </summary>
		public static void Unprotect(string pdf, string output, string password)
		{
			password ??= "";
			using (var doc = Open(pdf, password, PdfDocumentOpenMode.Modify))
				if (doc != null)
				{
					doc.SecurityHandler.SetEncryptionToNoneAndResetPasswords();
					doc.Save(output);
					return;
				}
			// the password opens it, but its maker kept another one for changes: that choice is respected
			using var reading = Open(pdf, password, PdfDocumentOpenMode.Import);
			throw new InvalidOperationException(T.S(reading != null ? "pdfpages.err.readOnlyPassword" : "pdfpages.err.wrongPassword"));
		}

		/// <summary>Opened with this password (tried once), or null when it is not the right one.</summary>
		static PdfDocument Open(string pdf, string password, PdfDocumentOpenMode mode)
		{
			int tries = 0;
			try
			{
				return PdfReader.Open(pdf, mode, a =>
				{
					if (tries++ == 0 && password.Length > 0) a.Password = password;
					else a.Abort = true;
				});
			}
			catch (PdfReaderException) when (tries > 0) { return null; }
		}
	}
}
