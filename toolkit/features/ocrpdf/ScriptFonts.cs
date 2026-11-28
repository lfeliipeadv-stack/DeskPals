using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// The fonts for the invisible text of "searchable" in every script. Arial has no Japanese, Chinese, Korean, Thai or
	/// Indian letters: a word written with it becomes empty boxes that no search finds. So each word gets the first font of
	/// Windows that really has all its letters (checked glyph by glyph), and a word no font has is left out instead of being
	/// written wrong. The fonts Windows keeps in .ttc files (several fonts in one file) are given to PDFsharp as their first
	/// font alone, which it reads like any .ttf.
	/// </summary>
	internal sealed class ScriptFonts : IFontResolver
	{
		const string Prefix = "ocrpdf:";

		/// <summary>The fonts this class adds, and their files (the first one that is there).</summary>
		static readonly Dictionary<string, string[]> Families = new(StringComparer.OrdinalIgnoreCase)
		{
			["Yu Gothic"] = new[] { "YuGothR.ttc", "YuGothM.ttc" },
			["MS Gothic"] = new[] { "msgothic.ttc" },
			["Microsoft YaHei"] = new[] { "msyh.ttc", "msyh.ttf" },
			["SimSun"] = new[] { "simsun.ttc" },
			["Malgun Gothic"] = new[] { "malgun.ttf" },
			["Leelawadee UI"] = new[] { "LeelawUI.ttf" },
			["Leelawadee"] = new[] { "LEELAWAD.TTF" },
			["Nirmala UI"] = new[] { "Nirmala.ttc", "Nirmala.ttf" },
			["Tahoma"] = new[] { "tahoma.ttf" },
		};

		readonly IFontResolver inner;
		readonly Dictionary<string, byte[]> cache = new();

		ScriptFonts(IFontResolver inner) => this.inner = inner;

		/// <summary>
		/// Puts these fonts in front of the program's own (WindowsFonts), before any PDF is written. Program.cs only sets its
		/// resolver when none is set, so this one, which hands everything else to it, stays.
		/// </summary>
		internal static void Install()
		{
			try
			{
				if (GlobalFontSettings.FontResolver is ScriptFonts) return;
				GlobalFontSettings.FontResolver = new ScriptFonts(GlobalFontSettings.FontResolver ?? new WindowsFonts());
			}
			catch (Exception) { /* fonts already in use: the words of other scripts are then left out, never written wrong */ }
		}

		static string Dir => Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

		static string FileOf(string family) =>
			Families.TryGetValue(family ?? "", out var files) ? files.FirstOrDefault(f => File.Exists(Path.Combine(Dir, f))) : null;

		public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
		{
			string file = FileOf(familyName);
			// only the regular style is needed for invisible text; bold or italic is simulated by PDFsharp
			if (file != null) return new FontResolverInfo(Prefix + file, bold, italic);
			return inner.ResolveTypeface(familyName, bold, italic);
		}

		public byte[] GetFont(string faceName)
		{
			if (!faceName.StartsWith(Prefix, StringComparison.Ordinal)) return inner.GetFont(faceName);
			lock (cache)
			{
				if (!cache.TryGetValue(faceName, out var bytes))
					cache[faceName] = bytes = FirstFont(File.ReadAllBytes(Path.Combine(Dir, faceName[Prefix.Length..])));
				return bytes;
			}
		}

		// ------------------------------------------------------------------ choosing a font for a word

		/// <summary>The fonts to try for a word, best first: the ones made for its script, then Arial and the others.</summary>
		public static IEnumerable<string> Candidates(string word)
		{
			var list = new List<string>();
			bool kana = false, hangul = false, han = false, thai = false, indic = false;
			foreach (var r in word.EnumerateRunes())
			{
				int v = r.Value;
				if (v is >= 0x3040 and <= 0x30FF or >= 0x31F0 and <= 0x31FF or >= 0xFF66 and <= 0xFF9F) kana = true;
				else if (v is >= 0x1100 and <= 0x11FF or >= 0x3130 and <= 0x318F or >= 0xAC00 and <= 0xD7AF) hangul = true;
				else if (v is >= 0x3400 and <= 0x9FFF or >= 0xF900 and <= 0xFAFF or >= 0x3000 and <= 0x303F or >= 0xFF00 and <= 0xFFEF or >= 0x20000 and <= 0x3FFFF) han = true;
				else if (v is >= 0x0E00 and <= 0x0E7F) thai = true;
				else if (v is >= 0x0900 and <= 0x0DFF) indic = true;
			}
			if (kana || (han && T.Code == "ja")) list.AddRange(new[] { "Yu Gothic", "MS Gothic" });
			if (hangul || (han && T.Code == "ko")) list.Add("Malgun Gothic");
			if (han) list.AddRange(new[] { "Microsoft YaHei", "SimSun" });
			if (thai) list.AddRange(new[] { "Leelawadee UI", "Leelawadee", "Tahoma" });
			if (indic) list.Add("Nirmala UI");
			list.AddRange(new[] { "Arial", "Segoe UI", "Nirmala UI", "Leelawadee UI", "Malgun Gothic", "Microsoft YaHei", "Yu Gothic", "Tahoma" });
			return list.Distinct(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>True when the font has a real letter (not the empty box) for every character of the word.</summary>
		public static bool Covers(XFont font, string word)
		{
			try
			{
				foreach (var r in word.EnumerateRunes())
					if (!Rune.IsWhiteSpace(r) && !Rune.IsControl(r) && GlyphHelper.GlyphIndexFromCodePoint(r.Value, font) == 0) return false;
				return true;
			}
			catch (Exception) { return false; }
		}

		/// <summary>
		/// The font (at this size) that has all the letters of the word, or null when no font of this Windows has them.
		/// <paramref name="fonts"/> keeps the fonts made, so one is made once per family and size.
		/// </summary>
		public static XFont For(string word, double size, Dictionary<(string, double), XFont> fonts)
		{
			foreach (string family in Candidates(word))
			{
				if (!fonts.TryGetValue((family, size), out var font))
				{
					try { font = new XFont(family, size, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); }
					catch (Exception) { font = null; }
					fonts[(family, size)] = font;
				}
				if (font != null && Covers(font, word)) return font;
			}
			return null;
		}

		// ------------------------------------------------------------------ .ttc files

		/// <summary>
		/// The first font of a .ttc file as a .ttf of its own: its table list with offsets from the start of the new file
		/// and its tables copied after it. A .ttf (or anything else) is given back as it is.
		/// </summary>
		internal static byte[] FirstFont(byte[] data)
		{
			if (data.Length < 16 || Encoding.ASCII.GetString(data, 0, 4) != "ttcf") return data;
			int face = (int)U32(data, 12);
			int tables = U16(data, face + 4);
			int head = 12 + 16 * tables;
			var output = new MemoryStream();
			output.Write(data, face, 12);
			// the new offsets: the tables one after the other, each starting on 4 bytes
			var offsets = new int[tables];
			int at = head;
			for (int i = 0; i < tables; i++)
			{
				offsets[i] = at;
				at += (int)((U32(data, face + 12 + 16 * i + 12) + 3) & ~3u);
			}
			for (int i = 0; i < tables; i++)
			{
				int rec = face + 12 + 16 * i;
				output.Write(data, rec, 8);
				output.Write(BigEndian((uint)offsets[i]));
				output.Write(data, rec + 12, 4);
			}
			for (int i = 0; i < tables; i++)
			{
				int rec = face + 12 + 16 * i;
				int from = (int)U32(data, rec + 8), length = (int)U32(data, rec + 12);
				output.Write(data, from, length);
				int pad = (4 - length % 4) % 4;
				output.Write(new byte[pad], 0, pad);
			}
			return output.ToArray();
		}

		static uint U32(byte[] b, int i) => (uint)(b[i] << 24 | b[i + 1] << 16 | b[i + 2] << 8 | b[i + 3]);
		static int U16(byte[] b, int i) => b[i] << 8 | b[i + 1];
		static byte[] BigEndian(uint v) => new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
	}
}
