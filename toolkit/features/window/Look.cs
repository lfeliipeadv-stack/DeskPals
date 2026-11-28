using System.Drawing;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// The fonts of the window, made once for its DPI (Theme.CreateFont is in pixels, so they are scaled by hand) and
	/// shared by its parts.
	/// </summary>
	internal sealed class UiFonts : IDisposable
	{
		public readonly Font Title, Section, Text, Small, Bold;

		public UiFonts(float dpi)
		{
			float k = dpi / 96f;
			Title = Theme.CreateFont(22 * k, FontStyle.Bold);
			Section = Theme.CreateFont(17 * k, FontStyle.Bold);
			Text = Theme.CreateFont(13 * k);
			Small = Theme.CreateFont(12 * k);
			Bold = Theme.CreateFont(13 * k, FontStyle.Bold);
		}

		public void Dispose() { Title.Dispose(); Section.Dispose(); Text.Dispose(); Small.Dispose(); Bold.Dispose(); }
	}

	/// <summary>
	/// The colours of DeskPals' theme on ordinary controls. Each control says what it is in its Tag ("muted", "card",
	/// "primary", "tab", "tab-on"…) so the whole window can be painted again when the theme changes (light ↔ dark).
	/// </summary>
	internal static class Look
	{
		/// <summary>A size in pixels for this control's screen (96 DPI = 100%).</summary>
		public static int Px(Control c, int logical) => (int)Math.Round(logical * c.DeviceDpi / 96f);

		public static Button Button(string text, Font font, string role = null)
		{
			var b = new Button { Text = text, AutoSize = true, Font = font, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = role, UseMnemonic = false, Padding = new Padding(8, 3, 8, 3), Margin = new Padding(0, 0, 8, 8) };
			Style(b);
			return b;
		}

		public static Label Label(string text, Font font, string role = null) => new Label { Text = text, Font = font, AutoSize = true, Tag = role, UseMnemonic = false };

		public static LinkLabel Link(string text, Font font, Action act)
		{
			var l = new LinkLabel { Text = text, AutoSize = true, Font = font, LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, 2, 12, 0), UseMnemonic = false };
			// LinkClicked comes from the mouse and from the keyboard (Tab to it, then Enter or Space)
			l.LinkClicked += (s, e) => act();
			Style(l);
			return l;
		}

		/// <summary>
		/// The file can be dragged from this control to another program (an e-mail, WhatsApp Web, a folder): a press and a
		/// small move start it. The hand cursor and a hint say so.
		/// </summary>
		public static void DragOut(Control c, string path)
		{
			Point down = Point.Empty;
			bool pressed = false;
			c.Cursor = Cursors.Hand;
			new ToolTip().SetToolTip(c, T.S("window.dragOut"));
			c.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { pressed = true; down = e.Location; } };
			c.MouseUp += (s, e) => pressed = false;
			c.MouseMove += (s, e) =>
			{
				if (!pressed || (Math.Abs(e.X - down.X) < SystemInformation.DragSize.Width && Math.Abs(e.Y - down.Y) < SystemInformation.DragSize.Height)) return;
				pressed = false;
				if (!File.Exists(path) && !Directory.Exists(path)) return;
				var data = new DataObject(DataFormats.FileDrop, new[] { path });
				try { c.DoDragDrop(data, DragDropEffects.Copy); } catch (Exception) { }
			};
		}
		
		/// <summary>Paint a control and everything in it with the theme's colours.</summary>
		public static void Paint(Control root)
		{
			Style(root);
			foreach (Control c in root.Controls) Paint(c);
		}

		static Color Back(Control c) => c.Parent == null ? Theme.Background : c.Parent.BackColor;

		[System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
		static extern int SetWindowTheme(IntPtr window, string app, string idList);

		/// <summary>
		/// Scroll bars in the theme's colours: Windows has dark ones ("DarkMode_Explorer") for a window that asks. Only once
		/// the control has its handle; the lists call it again when they get one.
		/// </summary>
		public static void ScrollBars(Control c)
		{
			if (!c.IsHandleCreated) return;
			try { SetWindowTheme(c.Handle, Theme.IsDark ? "DarkMode_Explorer" : "Explorer", null); } catch (Exception) { }
		}

		static void Style(Control c)
		{
			string role = c.Tag as string;
			if (c is ListBox || (c is ScrollableControl sc && sc.AutoScroll)) ScrollBars(c);
			switch (c)
			{
				case Button b:
					b.FlatAppearance.BorderSize = 1;
					if (role == "primary") { Theme.MakePrimary(b); break; }
					bool on = role == "tab-on";
					b.BackColor = on ? Theme.AccentSoft : role == "tab" ? Theme.Background : Theme.Surface;
					b.ForeColor = Theme.Text;
					b.FlatAppearance.BorderColor = on ? Theme.Accent : role == "tab" ? Theme.Background : Theme.Border;
					b.FlatAppearance.MouseOverBackColor = on ? Theme.AccentSoft : Theme.Hover;
					b.FlatAppearance.MouseDownBackColor = Theme.Border;
					break;
				case LinkLabel l:
					l.LinkColor = l.ActiveLinkColor = l.VisitedLinkColor = Theme.Accent;
					l.DisabledLinkColor = Theme.TextMuted;
					l.BackColor = Back(c);
					break;
				case Label l:
					l.ForeColor = role == "muted" ? Theme.TextMuted : role == "danger" ? Theme.Danger : Theme.Text;
					l.BackColor = Back(c);
					break;
				case ListBox lb:
					lb.BackColor = Theme.Surface; lb.ForeColor = Theme.Text;
					lb.Invalidate();
					break;
				case ProgressBar:
					break;
				case Form f:
					f.BackColor = Theme.Background; f.ForeColor = Theme.Text;
					break;
				default:
					// a card is on the surface colour; everything else takes its parent's (so what is in a card is too)
					c.BackColor = role == "card" ? Theme.Surface : Back(c);
					c.ForeColor = Theme.Text;
					c.Invalidate();
					break;
			}
		}

		/// <summary>When a job was done, the way people say it: "Today, 14:32", "Yesterday, 09:10", or the date.</summary>
		public static string When(DateTime at, DateTime now)
		{
			var culture = System.Globalization.CultureInfo.CurrentCulture;
			try { culture = System.Globalization.CultureInfo.GetCultureInfo(T.Code); } catch (Exception) { }
			string time = at.ToString("t", culture);
			if (at.Date == now.Date) return T.S("window.today", time);
			if (at.Date == now.Date.AddDays(-1)) return T.S("window.yesterday", time);
			return at.ToString("d", culture) + "  " + time;
		}

		/// <summary>"1.2 MB" (or "KB"), for the line under a file's name.</summary>
		public static string Size(long bytes)
		{
			var culture = System.Globalization.CultureInfo.CurrentCulture;
			try { culture = System.Globalization.CultureInfo.GetCultureInfo(T.Code); } catch (Exception) { }
			if (bytes < 1024) return bytes.ToString(culture) + " B";
			if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0", culture) + " KB";
			if (bytes < 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("0.0", culture) + " MB";
			return (bytes / 1024.0 / 1024 / 1024).ToString("0.0", culture) + " GB";
		}
	}
}
