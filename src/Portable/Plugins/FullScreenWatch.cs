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
	// ====================================================================== 1. full screen and "Do not disturb"

	/// <summary>
	/// While a game, a video or a presentation fills the screen, the pets hide by themselves (and come back after);
	/// while Windows is in "Do not disturb", they stay but keep quiet (notices wait, see <see cref="SpeechBubble.QuietNow"/>).
	/// </summary>
	public static class FullScreenWatch
	{
		[DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int state);
		const int Busy = 2, D3dFullScreen = 3, Presentation = 4, QuietTime = 6, App = 7;

		static Timer timer;
		static DateTime gameSince = DateTime.MinValue;

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("AutoHide", false);
			set { Program.MyData.SetSetting("AutoHide", value); Apply(); }
		}

		/// <summary>The pets are hidden because something fills the screen (not by Ctrl+Alt+P).</summary>
		public static bool Active { get; private set; }
		/// <summary>Windows is in "Do not disturb".</summary>
		public static bool Dnd { get; private set; }

		/// <summary>What Windows reports when something is filling the screen (a game, a presentation).</summary>
		internal const int FullState = Busy;
		/// <summary>What Windows reports when nothing is filling the screen.</summary>
		internal const int NoFullState = 0;

		/// <summary>Self-test: what Windows reports, instead of asking it (see <see cref="MusicWatch.FakeTitle"/>).</summary>
		internal static int? TestState;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 2000 }; timer.Tick += (s, e) => Tick(); }
			if (Enabled) timer.Start();
			else
			{
				timer.Stop();
				if (Active) Show();
				if (Dnd) { Dnd = false; SpeechBubble.QuietChanged(); }
			}
		}

		static int State()
		{
			if (TestState.HasValue) return TestState.Value;
			try { return SHQueryUserNotificationState(out int s) == 0 ? s : 5; }
			catch (Exception skipped) { LogFile.Ignored(skipped); return 5; }
		}

		internal static void Tick()
		{
			if (SelfTest.Running && !SelfTest.Live) return;
			int st = State();
			bool full = st == Busy || st == D3dFullScreen || st == Presentation || st == App;
			bool dnd = st == QuietTime;
			if (full && !Active && !HideHotkey.Hidden)
			{
				Active = true;
				if (st == D3dFullScreen) gameSince = DateTime.Now;
				HideHotkey.SetHidden(true);
			}
			else if (!full && Active) Show();
			if (dnd != Dnd) { Dnd = dnd; SpeechBubble.QuietChanged(); }
		}

		static void Show()
		{
			Active = false;
			if (HideHotkey.Hidden) HideHotkey.SetHidden(false);
			// back from a game of more than 5 minutes: "did you win?"
			if (gameSince != DateTime.MinValue)
			{
				var played = DateTime.Now - gameSince;
				gameSince = DateTime.MinValue;
				if (played.TotalMinutes >= 5) MusicWatch.GameOver(played);
			}
		}
	}
}
