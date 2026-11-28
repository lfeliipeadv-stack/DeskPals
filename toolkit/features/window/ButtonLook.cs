using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// Every button of the extension drawn by itself: rounded, in the theme's colours, with colour emoji (Windows draws a
	/// button's text with GDI, which has emoji only in black and white; <see cref="Theme.DrawText"/> draws them in colour
	/// through DirectWrite, as DeskPals does). The colours come from the button itself (BackColor, ForeColor, its
	/// FlatAppearance), so the code that sets them (<see cref="Look"/>, the theme) stays as it is.
	/// <see cref="Hook"/> a window once: its buttons, and the ones added later, are drawn this way.
	/// </summary>
	internal static class ButtonLook
	{
		static readonly MethodInfo SetStyle = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
		static readonly HashSet<Button> done = new();
		/// <summary>The focus ring only after the keyboard was used (Tab), as Windows does.</summary>
		static bool FocusByKeyboard => (Control.ModifierKeys & Keys.Alt) != 0 || keyboard;
		static bool keyboard;

		/// <summary>The buttons in this control and in everything added to it later.</summary>
		public static void Hook(Control root)
		{
			if (root == null) return;
			if (root is Button b) { One(b); return; }
			root.ControlAdded -= Added;
			root.ControlAdded += Added;
			foreach (Control c in root.Controls) Hook(c);
		}

		static void Added(object sender, ControlEventArgs e) => Hook(e.Control);

		/// <summary>Is this button drawn here (the tests look)?</summary>
		public static bool IsHooked(Button b) { lock (done) return done.Contains(b); }

		static void One(Button b)
		{
			lock (done) if (!done.Add(b)) return;
			try { SetStyle?.Invoke(b, new object[] { ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor, true }); } catch (Exception) { }
			bool over = false, down = false;
			b.MouseEnter += (s, e) => { over = true; b.Invalidate(); };
			b.MouseLeave += (s, e) => { over = down = false; b.Invalidate(); };
			b.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { down = true; b.Invalidate(); } };
			b.MouseUp += (s, e) => { down = false; b.Invalidate(); };
			b.EnabledChanged += (s, e) => b.Invalidate();
			b.GotFocus += (s, e) => { keyboard = (Control.MouseButtons == MouseButtons.None); b.Invalidate(); };
			b.LostFocus += (s, e) => b.Invalidate();
			b.Paint += (s, e) => Draw(b, e.Graphics, over, down);
			b.Disposed += (s, e) => { lock (done) done.Remove(b); };
			b.Invalidate();
		}

		/// <summary>The button over what Windows drew: the whole of it, so nothing of the old look shows.</summary>
		static void Draw(Button b, Graphics g, bool over, bool down)
		{
			var back = b.Parent?.BackColor ?? Theme.Background;
			g.Clear(back);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			bool primary = b.BackColor.ToArgb() == Theme.Accent.ToArgb();
			Color fill = b.BackColor, border = b.FlatAppearance.BorderSize > 0 ? b.FlatAppearance.BorderColor : Color.Empty, text = b.ForeColor;
			if (!b.Enabled)
			{
				fill = primary ? Theme.Mix(Theme.Accent, back, 0.55f) : Theme.Mix(fill, back, 0.4f);
				text = primary ? Theme.Mix(Theme.AccentText, back, 0.35f) : Theme.TextMuted;
				if (!border.IsEmpty) border = Theme.Mix(border, back, 0.5f);
			}
			else if (down) fill = primary ? Theme.Mix(Theme.Accent, Color.Black, 0.18f) : !b.FlatAppearance.MouseDownBackColor.IsEmpty ? b.FlatAppearance.MouseDownBackColor : Theme.Border;
			else if (over) fill = primary ? Theme.Mix(Theme.Accent, Color.White, 0.12f) : !b.FlatAppearance.MouseOverBackColor.IsEmpty ? b.FlatAppearance.MouseOverBackColor : Theme.Hover;
			float radius = Math.Min(b.Height / 2f, 8f * b.DeviceDpi / 96f);
			var r = new RectangleF(0.5f, 0.5f, b.Width - 1.5f, b.Height - 1.5f);
			if (fill.A > 0 && fill.ToArgb() != back.ToArgb()) Theme.FillRounded(g, fill, r, radius);
			else if (over || down) Theme.FillRounded(g, Theme.Hover, r, radius);
			if (!border.IsEmpty && border.ToArgb() != fill.ToArgb() && !primary) Theme.DrawRounded(g, border, r, radius);
			if (b.Focused && b.FindForm() is Form ff && FocusByKeyboard) Theme.DrawRounded(g, Theme.Accent, new RectangleF(1, 1, b.Width - 3, b.Height - 3), radius, 2f);
			var box = Rectangle.FromLTRB(b.Padding.Left + 2, 0, b.Width - b.Padding.Right - 2, b.Height);
			var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
				| (b.TextAlign is ContentAlignment.MiddleLeft or ContentAlignment.TopLeft or ContentAlignment.BottomLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter);
			Theme.DrawText(g, b.Text, b.Font, box, text, flags);
		}
	}
}
