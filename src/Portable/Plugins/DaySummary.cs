using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== summary of the day

	/// <summary>
	/// Off unless turned on. At the chosen hour the pet tells how the day went in two sentences: focus, breaks, keys,
	/// the programs used most. With the AI set up (and its notice accepted) the AI writes it; otherwise the pet does.
	/// Only these numbers go to the AI, never what you wrote or opened.
	/// </summary>
	public static class DaySummary
	{
		static Timer timer;

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("DaySummary", false);
			set { Program.MyData.SetSetting("DaySummary", value); Apply(); }
		}

		public static int Hour
		{
			get { int v = Program.MyData.GetSettingInt("DaySummaryHour", 18); return v >= 0 && v <= 23 ? v : 18; }
			set => Program.MyData.SetSetting("DaySummaryHour", value);
		}
		internal static readonly int[] HourChoices = { 12, 16, 17, 18, 19, 20, 21, 22 };

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 60000 }; timer.Tick += (s, e) => Check(); }
			if (Enabled && !(SelfTest.Running && !SelfTest.Live)) timer.Start(); else timer.Stop();
		}

		static void Check()
		{
			string today = DateTime.Today.ToString("yyyy-MM-dd");
			if (DateTime.Now.Hour < Hour || Program.MyData.GetSetting("DaySummaryDone") == today || SpeechBubble.QuietNow) return;
			Program.MyData.SetSetting("DaySummaryDone", today);
			Tell(false);
		}

		/// <summary>The numbers of today, as short sentences (also what the AI receives).</summary>
		internal static string Facts()
		{
			var f = new List<string>();
			int focus = DayStats.Get("focusMin"), breaks = DayStats.Get("breaks"), keys = TypingPet.Today;
			if (focus > 0) f.Add(Lang.T("summary.factFocus", Pomodoro.Minutes(focus)));
			if (breaks > 0) f.Add(Lang.T("summary.factBreaks", breaks));
			if (keys > 0) f.Add(Lang.T("summary.factKeys", keys.ToString("N0")));
			var apps = AppTime.Today(3);
			if (apps.Count > 0) f.Add(Lang.T("summary.factApps", string.Join(", ", apps.Select(a => a.name + " (" + Pomodoro.Minutes(a.minutes) + ")"))));
			int reminders = Reminders.All.Count(r => r.Enabled);
			if (reminders > 0) f.Add(Lang.T("summary.factReminders", reminders));
			return string.Join(" ", f);
		}

		/// <summary>Tell it now (also "How was my day?" in the menu). From the menu the AI notice may be asked.</summary>
		public static async void Tell(bool asked = true)
		{
			string facts = Facts();
			if (facts.Length == 0) { SpeechBubble.Say(Lang.T("summary.nothing"), 8); return; }
			string text = null;
			bool aiOk = AiAssistant.Configured && (Program.MyData.GetSetting("AiPrivacyOk") == "1" || AiAssistant.IsLocal(AiAssistant.BaseUrl) || (asked && FormAsk.PrivacyOk()));
			if (aiOk)
			{
				SpeechBubble.Say(Lang.T("ask.thinking"), 0);
				try
				{
					string pet = Program.Mainthread?.CurrentHeader?.Petname ?? "Pet";
					text = await AiAssistant.Ask(AiAssistant.BaseUrl, AiAssistant.Model, AiAssistant.Key, Lang.T("summary.prompt") + "\n\n" + facts, pet);
				}
				catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "summary: " + ex.Message); }
			}
			if (string.IsNullOrWhiteSpace(text)) text = Lang.T("summary.local") + " " + facts;
			Program.Mainthread?.ReactAll("happy", "wave", "talk");
			SpeechBubble.Say(text, 0, new SpeechBubble.Choice("OK", null, true));
		}
	}
}
