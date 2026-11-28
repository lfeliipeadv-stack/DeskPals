using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// "Read text on the screen" ("--snip"): the screen stays still and darker, a box is dragged over the text (a
	/// picture, a video, a program that does not let text be copied), and what the OCR reads goes to the clipboard,
	/// with a short note that says so. Esc or a right click leaves.
	/// </summary>
	internal sealed class SnipForm : Form
	{
		readonly Bitmap shot;
		readonly Font fHint = Theme.CreateFont(15), fNote = Theme.CreateFont(13);
		Point down;
		Rectangle box;
		bool dragging, reading;
		string note;

		/// <summary>What was read (the tests look).</summary>
		internal string Read { get; private set; }

		internal SnipForm(Bitmap screen = null)
		{
			var all = SystemInformation.VirtualScreen;
			shot = screen ?? Capture(all);
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			Bounds = screen == null ? all : new Rectangle(all.Location, screen.Size);
			TopMost = true;
			ShowInTaskbar = false;
			DoubleBuffered = true;
			Cursor = Cursors.Cross;
			KeyPreview = true;
			Text = T.S("job.snip");
			KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
		}

		static Bitmap Capture(Rectangle all)
		{
			var b = new Bitmap(all.Width, all.Height, PixelFormat.Format32bppArgb);
			using var g = Graphics.FromImage(b);
			g.CopyFromScreen(all.Location, Point.Empty, all.Size);
			return b;
		}

		protected override void OnShown(EventArgs e) { base.OnShown(e); Activate(); }

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.DrawImageUnscaled(shot, 0, 0);
			using (var dim = new SolidBrush(Color.FromArgb(reading ? 90 : 120, 0, 0, 0)))
			{
				if (box.Width > 0 && box.Height > 0)
				{
					using var outside = new Region(ClientRectangle);
					outside.Exclude(box);
					g.FillRegion(dim, outside);
					using var pen = new Pen(Theme.Accent, 2);
					g.DrawRectangle(pen, box);
				}
				else g.FillRectangle(dim, ClientRectangle);
			}
			// the hint at the top of the screen the mouse is on
			var screen = Screen.FromPoint(Cursor.Position).Bounds;
			screen.Offset(-Bounds.X, -Bounds.Y);
			string hint = note ?? T.S("snip.hint");
			var size = Theme.MeasureText(g, hint, fHint, screen.Width - 80, TextFormatFlags.SingleLine);
			var pill = new Rectangle(screen.X + (screen.Width - size.Width) / 2 - 18, screen.Y + 24, size.Width + 36, size.Height + 18);
			Theme.Prepare(g);
			Theme.FillRounded(g, Theme.Surface, pill, pill.Height / 2f);
			Theme.DrawText(g, hint, fHint, pill, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (reading) return;
			if (e.Button == MouseButtons.Right) { Close(); return; }
			down = e.Location; dragging = true; box = Rectangle.Empty;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (!dragging) return;
			box = Rectangle.FromLTRB(Math.Min(down.X, e.X), Math.Min(down.Y, e.Y), Math.Max(down.X, e.X), Math.Max(down.Y, e.Y));
			Invalidate();
		}

		protected override async void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (!dragging) return;
			dragging = false;
			if (box.Width < 6 || box.Height < 6) { box = Rectangle.Empty; Invalidate(); return; }
			await ReadBox(box);
		}

		/// <summary>The text in this part of the picture, to the clipboard; then a short note and the end.</summary>
		internal async Task ReadBox(Rectangle r)
		{
			reading = true;
			note = T.S("snip.reading");
			Cursor = Cursors.WaitCursor;
			Invalidate();
			string text = "";
			try { text = (await ReadText(shot, r)).Trim(); }
			catch (Exception ex) { note = Jobs.Friendly(ex); }
			Read = text;
			if (text.Length > 0)
			{
				try { Clipboard.SetText(text); } catch (Exception) { }
				string first = text.Replace("\r", " ").Replace("\n", " ");
				note = "📋  " + T.S("snip.copied", first.Length > 60 ? first[..60] + "…" : first);
			}
			else if (note == T.S("snip.reading")) note = T.S("snip.nothing");
			Cursor = Cursors.Default;
			Invalidate();
			await Task.Delay(text.Length > 0 ? 1600 : 2200);
			Close();
		}

		/// <summary>The text in a part of a picture, with the OCR chosen in the settings (small text made bigger first).</summary>
		internal static async Task<string> ReadText(Bitmap picture, Rectangle r)
		{
			r.Intersect(new Rectangle(Point.Empty, picture.Size));
			if (r.Width <= 0 || r.Height <= 0) return "";
			// the OCR reads letters from about 20 pixels: a small box is enlarged (up to 4 times)
			double k = Math.Clamp(60.0 / Math.Max(1, Math.Min(r.Height, 60)), 1, 4);
			int w = (int)(r.Width * k), h = (int)(r.Height * k);
			using var part = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			using (var g = Graphics.FromImage(part))
			{
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.DrawImage(picture, new Rectangle(0, 0, w, h), r, GraphicsUnit.Pixel);
			}
			var data = part.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var bytes = new byte[w * h * 4];
			try { for (int y = 0; y < h; y++) Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * w * 4, w * 4); }
			finally { part.UnlockBits(data); }
			for (int i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
			using var engine = await OcrText.Open(null, default, null);
			var page = await engine.Read(new OcrImage { Bgra = bytes, Width = w, Height = h, Dpi = 96 * k }, default);
			return page.Text ?? "";
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { shot.Dispose(); fHint.Dispose(); fNote.Dispose(); }
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// Keys that work anywhere in Windows, through shortcuts in the Start menu (Windows itself listens for their keys, so
	/// nothing has to keep running): Ctrl+Alt+D opens DeskPals Tools, Ctrl+Alt+T reads the text on the screen. A switch
	/// in the settings; the shortcuts follow the program when it moves.
	/// </summary>
	internal static class Shortcuts
	{
		public const string Key = "shortcuts";
		const short Ctrl = 2, Alt = 4;
		public static readonly short OpenKey = (short)(((Ctrl | Alt) << 8) | 'D'), SnipKey = (short)(((Ctrl | Alt) << 8) | 'T');
		/// <summary>The tests point it elsewhere.</summary>
		public static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DeskPals");

		public static string OpenLink => Path.Combine(Folder, "DeskPals Tools.lnk");
		public static string SnipLink => Path.Combine(Folder, T.S("job.snip").Replace(":", "").Replace("/", "-") + ".lnk");

		[ModuleInitializer]
		internal static void Init()
		{
			Prefs.Register(new Prefs.Pref
			{
				Key = Key, TextKey = "shortcuts.pref", HintKey = "shortcuts.hint", Order = 40,
				Get = () => ToolSettings.Bool(Key),
				Set = on => { ToolSettings.Set(Key, on); if (on) Write(Environment.ProcessPath); else Remove(); },
			});
		}

		/// <summary>The two shortcuts with their keys, pointing to this program.</summary>
		public static void Write(string exe)
		{
			Directory.CreateDirectory(Folder);
			Remove(keepFolder: true);
			ShellLink.Save(OpenLink, exe, "", T.S("app"), OpenKey);
			ShellLink.Save(SnipLink, exe, "--snip", T.S("job.snip"), SnipKey);
			Changed();
		}

		public static void Remove(bool keepFolder = false)
		{
			try
			{
				if (!Directory.Exists(Folder)) return;
				foreach (var f in Directory.GetFiles(Folder, "*.lnk"))
					if (string.Equals(Path.GetFileName(f), "DeskPals Tools.lnk", StringComparison.OrdinalIgnoreCase) || ShellLink.Hotkey(f) == SnipKey) File.Delete(f);
				if (!keepFolder && Directory.GetFileSystemEntries(Folder).Length == 0) Directory.Delete(Folder);
				Changed();
			}
			catch (Exception) { }
		}

		/// <summary>At the window's start: shortcuts that point to an old place (DeskPals updated or moved the program) are written again.</summary>
		public static void Refresh()
		{
			try
			{
				if (!ToolSettings.Bool(Key)) return;
				string exe = Environment.ProcessPath;
				if (!string.Equals(ShellLink.Target(OpenLink), exe, StringComparison.OrdinalIgnoreCase) || !File.Exists(SnipLink)) Write(exe);
			}
			catch (Exception) { }
		}

		[DllImport("shell32.dll")]
		static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

		/// <summary>Windows is told the Start menu changed (so it learns the keys at once).</summary>
		static void Changed() { try { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); } catch (Exception) { } }
	}
}
