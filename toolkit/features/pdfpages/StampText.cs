using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using PdfSharp.Drawing;

namespace DeskPalsTools
{
	/// <summary>
	/// Writes a short text on a page (page numbers, a watermark) in any language. PDFsharp only lays letters side by side,
	/// in Arial: right for Latin, Greek and Cyrillic, wrong for Japanese, Chinese, Korean (no such letters in Arial),
	/// Hindi, Bengali, Tamil, Thai (letters that join and move) and Arabic, Persian, Urdu (joined, right to left). Text
	/// like that is drawn by Windows instead, in a Windows font that has its letters, and put on the page as a see-through
	/// picture: it then looks exactly as the reader expects (it just cannot be selected as text).
	/// </summary>
	internal sealed class StampText
	{
		/// <summary>Pixels per point of the pictures (300 dpi): sharp when printed.</summary>
		const double Scale = 300 / 72.0;
		/// <summary>The longest side of a picture, so a huge watermark stays a reasonable size.</summary>
		const int MaxSide = 3200;

		/// <summary>A picture already made, with the size of the text in it (points) and its margin (points).</summary>
		sealed record Picture(XImage Image, MemoryStream Data, double Width, double Height, double Pad);

		readonly Dictionary<(string, int, bool, int), Picture> pictures = new();
		static readonly Dictionary<string, string> fonts = new();
		static readonly object gate = new();

		// ------------------------------------------------------------------ which way

		/// <summary>
		/// True when PDFsharp writes this text right with Arial: no letters that join, move or go right to left, no
		/// accents typed as separate marks, and Arial has every letter.
		/// </summary>
		public static bool Plain(string text)
		{
			lock (gate)
			{
				if (plain.TryGetValue(text, out bool known)) return known;
				bool ok = text.All(ch =>
				{
					int c = ch;
					return c < 0x0300 || (c >= 0x0370 && c < 0x0483) || (c >= 0x048A && c < 0x0530)
						|| (c >= 0x1E00 && c < 0x2000) || (c >= 0x2010 && c < 0x2070) || (c >= 0x20A0 && c < 0x20D0) || (c >= 0x2100 && c < 0x2150);
				}) && Missing("Arial", text) == 0;
				if (plain.Count > 2000) plain.Clear();
				plain[text] = ok;
				return ok;
			}
		}
		static readonly Dictionary<string, bool> plain = new();

		/// <summary>True when the text reads right to left (its first letter that has a direction is Hebrew, Arabic…).</summary>
		static bool RightToLeft(string text)
		{
			foreach (char ch in text)
			{
				if (ch is >= '֐' and <= 'ࣿ' or >= 'יִ' and <= '﷿' or >= 'ﹰ' and <= '﻿') return true;
				if (char.IsLetter(ch)) return false;
			}
			return false;
		}

		/// <summary>The Windows font that has every letter of the text (the one made for its script first).</summary>
		public static string FontFor(string text)
		{
			lock (gate)
			{
				if (fonts.TryGetValue(text, out var known)) return known;
				bool kana = text.Any(c => c is >= '぀' and <= 'ヿ');
				bool japanese = kana || T.Code == "ja";
				var order = new List<string> { "Segoe UI", "Nirmala UI", "Leelawadee UI" };
				if (text.Any(c => c is >= '가' and <= '힯' or >= 'ᄀ' and <= 'ᇿ')) order.Add("Malgun Gothic");
				order.AddRange(japanese ? new[] { "Yu Gothic UI", "Meiryo UI", "Microsoft YaHei UI" } : new[] { "Microsoft YaHei UI", "Microsoft JhengHei UI", "Yu Gothic UI" });
				order.AddRange(new[] { "Malgun Gothic", "Ebrima", "Gadugi", "Segoe UI Historic", "Segoe UI Symbol", "Arial" });
				string best = "Segoe UI";
				int fewest = int.MaxValue;
				foreach (var family in order.Distinct())
				{
					int n = Missing(family, text);
					if (n < fewest) { fewest = n; best = family; }
					if (n == 0) break;
				}
				fonts[text] = best;
				return best;
			}
		}

		// ------------------------------------------------------------------ measuring and drawing

		/// <summary>How wide the text is at this size, in points.</summary>
		public double Width(XGraphics g, string text, double size, bool bold)
		{
			if (Plain(text)) return g.MeasureString(text, new XFont("Arial", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular)).Width;
			return Measure(text, size, bold).Width;
		}

		/// <summary>The text at a point, placed like PDFsharp's own text (left/centre/right, top/centre/bottom of the point).</summary>
		public void Draw(XGraphics g, string text, double size, bool bold, XColor color, XPoint at, XStringFormat format)
		{
			if (Plain(text))
			{
				g.DrawString(text, new XFont("Arial", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular), new XSolidBrush(color), at, format);
				return;
			}
			var p = PictureOf(text, size, bold, color);
			double x = format.Alignment switch { XStringAlignment.Center => at.X - p.Width / 2, XStringAlignment.Far => at.X - p.Width, _ => at.X };
			double y = format.LineAlignment switch { XLineAlignment.Center => at.Y - p.Height / 2, XLineAlignment.Far or XLineAlignment.BaseLine => at.Y - p.Height, _ => at.Y };
			g.DrawImage(p.Image, x - p.Pad, y - p.Pad, p.Width + 2 * p.Pad, p.Height + 2 * p.Pad);
		}

		/// <summary>The size of the text in points (Windows' own measure).</summary>
		static SizeF Measure(string text, double size, bool bold)
		{
			lock (gate)
			{
				using var font = FontOf(FontFor(text), (float)size, bold);
				using var bmp = new Bitmap(1, 1);
				using var gr = Graphics.FromImage(bmp);
				gr.TextRenderingHint = TextRenderingHint.AntiAlias;
				using var f = Format(text);
				var s = gr.MeasureString(text, font, new PointF(0, 0), f);
				return new SizeF(s.Width, Math.Max(s.Height, font.GetHeight(gr)));
			}
		}

		static StringFormat Format(string text)
		{
			var f = (StringFormat)StringFormat.GenericTypographic.Clone();
			f.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
			if (RightToLeft(text)) f.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
			return f;
		}

		/// <summary>The font in pixels (bold when the family has it).</summary>
		static Font FontOf(string family, float pixels, bool bold)
		{
			using var ff = new FontFamily(family);
			var style = bold && ff.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : ff.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;
			return new Font(ff, Math.Max(1f, pixels), style, GraphicsUnit.Pixel);
		}

		/// <summary>The text drawn by Windows on a see-through picture, in the colour and opacity asked (made once per size).</summary>
		Picture PictureOf(string text, double size, bool bold, XColor color)
		{
			int argb = ((int)Math.Round(color.A * 255) << 24) | (color.R << 16) | (color.G << 8) | color.B;
			var key = (text, (int)Math.Round(size * 10), bold, argb);
			lock (pictures)
			{
				if (pictures.TryGetValue(key, out var made)) return made;
				var s = Measure(text, size, bold);
				double pad = size * 0.25;
				double k = Math.Min(Scale, MaxSide / Math.Max(1, s.Width + 2 * pad));
				int w = Math.Max(1, (int)Math.Ceiling((s.Width + 2 * pad) * k)), h = Math.Max(1, (int)Math.Ceiling((s.Height + 2 * pad) * k));
				var ms = new MemoryStream();
				lock (gate)
				{
					using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
					using (var gr = Graphics.FromImage(bmp))
					{
						gr.Clear(Color.Transparent);
						gr.SmoothingMode = SmoothingMode.AntiAlias;
						gr.TextRenderingHint = TextRenderingHint.AntiAlias;
						using var font = FontOf(FontFor(text), (float)(size * k), bold);
						using var brush = new SolidBrush(Color.FromArgb(argb));
						using var f = Format(text);
						gr.DrawString(text, font, brush, new RectangleF((float)(pad * k), (float)(pad * k), (float)(s.Width * k) + 1, (float)(s.Height * k) + 1), f);
					}
					bmp.Save(ms, ImageFormat.Png);
				}
				ms.Position = 0;
				made = new Picture(XImage.FromStream(ms), ms, s.Width, s.Height, pad);
				pictures[key] = made;
				return made;
			}
		}

		// ------------------------------------------------------------------ letters a font does not have

		[DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
		[DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern uint GetGlyphIndicesW(IntPtr hdc, string text, int count, [Out] ushort[] glyphs, uint flags);
		const uint MarkMissing = 1; // GGI_MARK_NONEXISTING_GLYPHS

		/// <summary>How many letters of the text the font does not have (would show as empty boxes); spaces and joiners do not count.</summary>
		public static int Missing(string family, string text)
		{
			try
			{
				using var ff = new FontFamily(family);
			}
			catch (ArgumentException) { return int.MaxValue; } // not on this PC
			lock (gate)
			{
				using var font = new Font(family, 20, FontStyle.Regular, GraphicsUnit.Pixel);
				if (!string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase)) return int.MaxValue;
				using var bmp = new Bitmap(1, 1);
				using var gr = Graphics.FromImage(bmp);
				IntPtr hdc = gr.GetHdc(), hfont = font.ToHfont();
				try
				{
					IntPtr old = SelectObject(hdc, hfont);
					var glyphs = new ushort[text.Length];
					GetGlyphIndicesW(hdc, text, text.Length, glyphs, MarkMissing);
					SelectObject(hdc, old);
					int n = 0;
					for (int i = 0; i < text.Length; i++)
					{
						char c = text[i];
						if (char.IsWhiteSpace(c) || char.IsControl(c) || char.IsSurrogate(c) || c is >= '​' and <= '‏') continue;
						if (glyphs[i] == 0xFFFF) n++;
					}
					return n;
				}
				finally { DeleteObject(hfont); gr.ReleaseHdc(hdc); }
			}
		}
	}
}
