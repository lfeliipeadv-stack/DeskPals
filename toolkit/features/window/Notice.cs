using System.Drawing;

namespace DeskPalsTools
{
	/// <summary>
	/// A Windows notification at the end of a long job when the window was not being looked at (minimised or behind
	/// another one): Windows 10 and 11 show it as their own notification. A click on it brings the window back.
	/// </summary>
	internal static class Notice
	{
		/// <summary>Jobs shorter than this (seconds) end without a notification: the user is still there.</summary>
		public const double After = 6;

		/// <summary>The last one shown (the tests look).</summary>
		internal static string Last;

		public static void Show(Form window, string title, string text, bool ok)
		{
			Last = title + " · " + text;
			try
			{
				var icon = new NotifyIcon { Visible = true, Text = T.S("app").Length > 60 ? T.S("app")[..60] : T.S("app") };
				try { icon.Icon = window.Icon ?? SystemIcons.Application; } catch (Exception) { icon.Icon = SystemIcons.Application; }
				void Back()
				{
					if (window.IsDisposed) return;
					if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
					window.Activate();
				}
				icon.BalloonTipClicked += (s, e) => Back();
				icon.Click += (s, e) => Back();
				icon.ShowBalloonTip(8000, (ok ? "✅ " : "⚠️ ") + title, string.IsNullOrWhiteSpace(text) ? " " : (text.Length > 200 ? text[..200] + "…" : text), ok ? ToolTipIcon.Info : ToolTipIcon.Warning);
				// the icon goes once the notification had its time (or with the window)
				var timer = new System.Windows.Forms.Timer { Interval = 12000 };
				timer.Tick += (s, e) => { timer.Dispose(); icon.Dispose(); };
				timer.Start();
				window.FormClosed += (s, e) => { try { icon.Dispose(); } catch (Exception) { } };
			}
			catch (Exception) { }
		}
	}
}
