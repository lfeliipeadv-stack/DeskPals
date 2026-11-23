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
	// ====================================================================== 5. notices while the pets are hidden, quiet hours

	/// <summary>
	/// With the pets hidden by Ctrl+Alt+P, notices can go to Windows notifications instead of waiting; during quiet
	/// hours (for example 22h to 7h) nothing is shown and the notices wait until the morning.
	/// </summary>
	public static class NoticeOptions
	{
		static Timer timer;
		static bool wasQuiet;

		public static bool ToastsWhenHidden
		{
			get => Program.MyData.GetSettingBool("HiddenToasts", false);
			set => Program.MyData.SetSetting("HiddenToasts", value);
		}

		/// <summary>Quiet hours: from / to (hour of the day); -1 = off.</summary>
		public static int QuietFrom { get => Program.MyData.GetSettingInt("QuietFrom", -1); set { Program.MyData.SetSetting("QuietFrom", value); Check(); } }
		public static int QuietTo { get => Program.MyData.GetSettingInt("QuietTo", 7); set { Program.MyData.SetSetting("QuietTo", value); Check(); } }

		public static bool InQuietHours
		{
			get
			{
				int from = QuietFrom, to = QuietTo, h = DateTime.Now.Hour;
				if (from < 0 || from == to) return false;
				return from < to ? h >= from && h < to : h >= from || h < to;
			}
		}

		public static void Start()
		{
			wasQuiet = InQuietHours;
			timer = new Timer { Interval = 60000 };
			timer.Tick += (s, e) => Check();
			timer.Start();
		}

		static void Check()
		{
			bool q = InQuietHours;
			if (q != wasQuiet) { wasQuiet = q; SpeechBubble.QuietChanged(); }
		}

		/// <summary>The pets are hidden by the user: send the notice to Windows (true), or let it wait (false).</summary>
		public static bool ToToast(string text)
		{
			if (!HideHotkey.Hidden || FullScreenWatch.Active || InQuietHours || !ToastsWhenHidden) return false;
			ProcessIcon.Current?.Notify(Program.Mainthread?.CurrentHeader?.Petname ?? "DeskPals", text, () => HideHotkey.SetHidden(false));
			return true;
		}
	}
}
