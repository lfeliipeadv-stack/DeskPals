using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== 13. simple timer

	/// <summary>
	/// Off unless turned on. A timer that stays with the pet: a small panel under it (beside it when the pet stands
	/// on the taskbar) with the time left, a progress bar, Pause, +5 min and Cancel. When the time is up the pet
	/// reacts and the panel stays, saying so, until it is closed.
	/// </summary>
	public static class SimpleTimer
	{
		static Timer tick;
		static TimerPanel panel;
		static DateTime end;
		static TimeSpan total, left;
		static bool paused;

		/// <summary>Minutes offered in the menu.</summary>
		internal static readonly int[] Presets = { 1, 5, 10, 15, 25, 30, 60 };

		/// <summary>A timer is on screen (running, paused or finished and not closed yet).</summary>
		public static bool Active => total > TimeSpan.Zero;
		public static bool Finished { get; private set; }
		public static bool Running => Active && !Finished;
		public static bool Paused => paused && Running;

		public static TimeSpan Left
		{
			get
			{
				if (!Active || Finished) return TimeSpan.Zero;
				if (paused) return left;
				var l = end - DateTime.Now;
				return l > TimeSpan.Zero ? l : TimeSpan.Zero;
			}
		}

		/// <summary>0 at the start, 1 when the time is up.</summary>
		public static double Progress => !Active ? 0 : Finished ? 1 : Math.Max(0, Math.Min(1, 1 - Left.TotalSeconds / total.TotalSeconds));

		public static string LeftText => Format(Left);

		/// <summary>"4:59", "1:00:00" (the seconds rounded up, so it never shows 0:00 before the end).</summary>
		public static string Format(TimeSpan t)
		{
			t = TimeSpan.FromSeconds(Math.Ceiling(Math.Max(0, t.TotalSeconds)));
			return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{(int)t.TotalMinutes}:{t.Seconds:00}";
		}

		public static void Start(TimeSpan length)
		{
			if (length <= TimeSpan.Zero) return;
			total = left = length;
			end = DateTime.Now + length;
			paused = false;
			Finished = false;
			if (tick == null) { tick = new Timer { Interval = 200 }; tick.Tick += (s, e) => Step(); }
			tick.Start();
			if (panel == null || panel.IsDisposed) panel = new TimerPanel();
			panel.Pinned = false;   // a new timer starts next to the pet
			Step();
			Program.Mainthread?.ReactAll("ok", "wave", "happy");
		}

		public static void TogglePause()
		{
			if (!Running) return;
			if (paused) { end = DateTime.Now + left; paused = false; }
			else { left = Left; paused = true; }
			Step();
		}

		/// <summary>More time (after the end: a new timer of that length).</summary>
		public static void AddMinutes(int minutes)
		{
			var more = TimeSpan.FromMinutes(minutes);
			if (!Active || Finished) { Start(more); return; }
			total += more;
			if (paused) left += more; else end += more;
			Step();
		}

		/// <summary>Cancel it, or close it when the time is up.</summary>
		public static void Close()
		{
			total = TimeSpan.Zero;
			Finished = paused = false;
			tick?.Stop();
			if (panel != null && !panel.IsDisposed) panel.Close();
			panel = null;
		}

		static void Step()
		{
			if (!Active) { tick?.Stop(); return; }
			if (Running && !paused && DateTime.Now >= end) { Finished = true; TimeUp(); }
			panel?.Follow();
		}

		static void TimeUp()
		{
			Program.Mainthread?.ReactAll("surprise", "jump", "wave", "happy");
			if (!SpeechBubble.QuietNow) System.Media.SystemSounds.Asterisk.Play();
			if (HideHotkey.Hidden) SpeechBubble.Notify(Lang.T("timer.done"));   // the pets are hidden: a notification instead of the panel
		}

		/// <summary>"10" → 10 minutes, "1:30" → 1 min 30 s, "1:00:00" → 1 hour, "90s", "2h". Null if it cannot be read.</summary>
		internal static TimeSpan? Parse(string text)
		{
			text = (text ?? "").Trim().ToLowerInvariant().Replace(',', '.').Replace(" ", "");
			if (text.Length == 0) return null;
			TimeSpan t;
			var parts = text.Split(':');
			if (parts.Length >= 2 && parts.Length <= 3 && parts.All(p => p.Length > 0 && p.All(char.IsDigit)))
			{
				var n = parts.Select(int.Parse).ToArray();
				t = n.Length == 3 ? new TimeSpan(n[0], n[1], n[2]) : new TimeSpan(0, n[0], n[1]);
			}
			else
			{
				var m = System.Text.RegularExpressions.Regex.Match(text, @"^(\d+(?:\.\d+)?)(h|hora|horas|min|m|minutos?|s|seg|segundos?)?$");
				if (!m.Success) return null;
				double v = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
				string unit = m.Groups[2].Value;
				t = unit.StartsWith("h") ? TimeSpan.FromHours(v) : unit.StartsWith("s") ? TimeSpan.FromSeconds(v) : TimeSpan.FromMinutes(v);
			}
			return t >= TimeSpan.FromSeconds(1) && t <= TimeSpan.FromHours(24) ? t : (TimeSpan?)null;
		}

		/// <summary>Ask how long (the Plugins card and "Other time…" in the menu).</summary>
		public static void AskAndStart()
		{
			string answer = FormLaunchers.Prompt(Lang.T("timer.ask"), "10");
			if (answer == null || answer.Trim().Length == 0) return;
			var t = Parse(answer);
			if (t == null) { SpeechBubble.Say(Lang.T("timer.bad"), 8); return; }
			Start(t.Value);
		}

		/// <summary>Self-test: the panel on screen.</summary>
		internal static Form Panel => panel != null && !panel.IsDisposed && panel.Visible ? panel : null;
		/// <summary>Self-test: click one of the panel's buttons ("pause", "add", "cancel", "close").</summary>
		internal static void TestPress(string button) => panel?.Press(button);
		/// <summary>Self-test: the user dragged the panel to this place.</summary>
		internal static void TestMove(Point to) { if (panel == null) return; panel.Location = to; panel.Pinned = true; }
		/// <summary>Self-test: the time is up now.</summary>
		internal static void TestEndNow() { if (Running) { paused = false; end = DateTime.Now; Step(); } }

		/// <summary>The panel: time left, progress bar and buttons. Never takes the focus; follows the pet.</summary>
		sealed class TimerPanel : Form
		{
			const int W = 264, H = 92;
			readonly Font font = Theme.CreateFont(12.5f), bold = Theme.CreateFont(12.5f, FontStyle.Bold), icon = Theme.CreateFont(13);
			readonly List<(Rectangle rect, string id, string text)> buttons = new List<(Rectangle, string, string)>();
			string hover;

			public TimerPanel()
			{
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; DoubleBuffered = true;
				BackColor = Theme.Surface;
				Size = new Size(W, H);
				using (var path = Theme.RoundedRect(new RectangleF(0, 0, W, H), 14)) Region = new Region(path);
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008;   // tool window, no activate, topmost
					return cp;
				}
			}

			/// <summary>Moved by the user: it stays where it was dropped (a double click sends it back to the pet).</summary>
			public bool Pinned { get; set; }

			/// <summary>Under the pet; beside it (left) when the pet stands on the bottom of the screen.</summary>
			public void Follow()
			{
				var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow && p.Opacity > 0 && !p.Dying);
				if (HideHotkey.Hidden || (pet == null && !Pinned)) { if (Visible) Hide(); return; }
				if (Pinned) { if (!Visible) Show(); Invalidate(); return; }
				var wa = Screen.FromControl(pet).WorkingArea;
				int x = pet.Left + pet.Width / 2 - W / 2, y = pet.Bottom + 6;
				if (y + H > wa.Bottom)
				{
					y = pet.Bottom - H;
					x = pet.Left - W - 8;
					if (x < wa.Left) x = pet.Right + 8 + 40;   // the right side also has the hover buttons
				}
				x = Math.Max(wa.Left + 4, Math.Min(wa.Right - W - 4, x));
				y = Math.Max(wa.Top + 4, Math.Min(wa.Bottom - H - 4, y));
				if (Left != x || Top != y) Location = new Point(x, y);
				if (!Visible) Show();
				Invalidate();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Surface);
				using (var path = Theme.RoundedRect(new RectangleF(0.5f, 0.5f, W - 1, H - 1), 14))
				using (var p = new Pen(Theme.Border)) g.DrawPath(p, path);

				// ⏱ Timer · 4:59 left
				Theme.DrawText(g, "⏱", icon, new Rectangle(10, 8, 22, 22), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				string line = Finished ? Lang.T("timer.done") : Lang.T("timer.title") + " · " + Lang.T(Paused ? "timer.pausedLeft" : "timer.left", LeftText);
				Theme.DrawText(g, line, Finished ? bold : font, new Rectangle(34, 8, W - 44, 22), Finished ? Theme.Text : Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

				// progress bar
				var bar = new Rectangle(12, 36, W - 24, 6);
				Theme.FillRounded(g, Theme.SurfaceAlt, bar, 3);
				int done = (int)Math.Round(bar.Width * Progress);
				if (done >= 6) Theme.FillRounded(g, Paused ? Theme.TextMuted : Finished ? ChatIcons.Green : Theme.Accent, new Rectangle(bar.X, bar.Y, done, bar.Height), 3);

				// buttons
				buttons.Clear();
				if (Finished) Layout2(g, ("close", Lang.T("timer.close")), ("add", Lang.T("timer.add5")));
				else Layout2(g, ("pause", Lang.T(Paused ? "timer.resume" : "timer.pause")), ("add", Lang.T("timer.add5")), ("cancel", Lang.T("timer.cancel")));
				foreach (var (r, id, text) in buttons)
				{
					bool main = id == "pause" || id == "close", danger = id == "cancel";
					var fill = main ? Theme.Accent : danger ? Theme.Danger : Theme.SurfaceAlt;
					if (hover == id) fill = main || danger ? ControlPaint.Light(fill, 0.25f) : Theme.Hover;
					Theme.FillRounded(g, fill, r, 8);
					if (!main && !danger) Theme.DrawRounded(g, Theme.Border, r, 8);
					Theme.DrawText(g, text, bold, r, main ? Theme.AccentText : danger ? Color.White : Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				}
			}

			/// <summary>The buttons side by side, each as wide as its text needs (the spare room shared out).</summary>
			void Layout2(Graphics g, params (string id, string text)[] items)
			{
				int gap = 6, room = W - 24 - gap * (items.Length - 1);
				var need = items.Select(it => Theme.MeasureText(g, it.text, bold, int.MaxValue, TextFormatFlags.SingleLine).Width + 16).ToArray();
				int spare = Math.Max(0, room - need.Sum()) / items.Length;
				int x = 12;
				for (int i = 0; i < items.Length; i++)
				{
					int w = i == items.Length - 1 ? W - 12 - x : need[i] + spare;
					buttons.Add((new Rectangle(x, 52, w, 28), items[i].id, items[i].text));
					x += w + gap;
				}
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				string h = buttons.FirstOrDefault(b => b.rect.Contains(e.Location)).id;
				Cursor = h != null ? Cursors.Hand : Cursors.SizeAll;   // outside the buttons it can be dragged
				if (h != hover) { hover = h; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; Invalidate(); }

			[DllImport("user32.dll")] static extern bool ReleaseCapture();
			[DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				if (e.Button != MouseButtons.Left) return;
				string id = buttons.FirstOrDefault(b => b.rect.Contains(e.Location)).id;
				if (id != null) { Press(id); return; }
				if (e.Clicks >= 2) { Pinned = false; Follow(); return; }   // double click: back to the pet
				// drag it anywhere; once dropped somewhere else it stays there
				var before = Location;
				ReleaseCapture();
				SendMessage(Handle, 0xA1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
				if (Location != before) Pinned = true;
			}

			public void Press(string id)
			{
				switch (id)
				{
					case "pause": TogglePause(); break;
					case "add": AddMinutes(5); break;
					case "cancel": case "close": SimpleTimer.Close(); break;
				}
			}

			protected override void Dispose(bool disposing) { if (disposing) { font.Dispose(); bold.Dispose(); icon.Dispose(); } base.Dispose(disposing); }
		}
	}
}
