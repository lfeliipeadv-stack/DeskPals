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
	// ====================================================================== time in each program

	/// <summary>
	/// Off unless turned on. Counts how long each program is in front today (only while you are at the computer) and
	/// shows the list; when one program stays in front for a long time without a pause, the pet says so.
	/// Only the program's name and the minutes are kept, on this computer, for 7 days.
	/// </summary>
	public static class AppTime
	{
		[DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int pid);

		static Timer timer;
		static Dictionary<string, Dictionary<string, int>> days;   // "yyyy-MM-dd" -> program -> seconds
		static string current;
		static DateTime currentSince = DateTime.Now, lastSave = DateTime.Now;
		static readonly HashSet<string> told = new HashSet<string>();
		const int StepSeconds = 5;

		internal static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "apptime.json");
		/// <summary>Read the file again (a backup was restored).</summary>
		internal static void Reload() => days = null;

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("AppTime", false);
			set { Program.MyData.SetSetting("AppTime", value); Apply(); }
		}

		/// <summary>Minutes in one program without a pause before the pet comments (0: never).</summary>
		public static int Limit
		{
			get { int v = Program.MyData.GetSettingInt("AppTimeLimit", 90); return v >= 0 ? v : 90; }
			set => Program.MyData.SetSetting("AppTimeLimit", value);
		}
		internal static readonly int[] LimitChoices = { 0, 30, 60, 90, 120, 180 };

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = StepSeconds * 1000 }; timer.Tick += (s, e) => Step(); }
			if (Enabled && !(SelfTest.Running && !SelfTest.Live)) timer.Start(); else { timer.Stop(); Save(); }
		}

		static Dictionary<string, int> Day(DateTime d)
		{
			Load();
			string k = d.ToString("yyyy-MM-dd");
			if (!days.TryGetValue(k, out var day)) days[k] = day = new Dictionary<string, int>();
			return day;
		}

		/// <summary>Today's programs, the most used first: (name, minutes).</summary>
		public static List<(string name, int minutes)> Today(int top = 5) =>
			Day(DateTime.Today).OrderByDescending(kv => kv.Value).Take(top).Select(kv => (kv.Key, (int)Math.Round(kv.Value / 60.0))).Where(x => x.Item2 > 0).ToList();

		/// <summary>Minutes in front of the computer (all programs) on each of the last 7 days, the oldest first.</summary>
		public static int[] Week()
		{
			var r = new int[7];
			for (int i = 0; i < 7; i++)
			{
				var d = Day(DateTime.Today.AddDays(i - 6));
				r[i] = (int)Math.Round(d.Values.Sum() / 60.0);
			}
			return r;
		}

		/// <summary>The name a person knows ("Google Chrome", "Word"), not the file name.</summary>
		internal static string NameOf(Process p)
		{
			try
			{
				string d = p.MainModule?.FileVersionInfo?.FileDescription;
				if (!string.IsNullOrWhiteSpace(d)) return d.Trim();
			}
			catch (Exception) { }
			return p.ProcessName;
		}

		static void Step()
		{
			if (Activity.IdleSeconds() > 120) { current = null; return; }   // away from the computer: not counted
			string name = null;
			try
			{
				GetWindowThreadProcessId(GetForegroundWindow(), out int pid);
				if (pid != 0 && pid != Environment.ProcessId)
					using (var p = Process.GetProcessById(pid)) name = NameOf(p);
			}
			catch (Exception) { }
			if (string.IsNullOrEmpty(name) || name == "Windows Explorer" || name == "explorer") { current = null; return; }
			var day = Day(DateTime.Today);
			day[name] = (day.TryGetValue(name, out int s) ? s : 0) + StepSeconds;
			if (name != current) { current = name; currentSince = DateTime.Now; }
			int minutes = (int)(DateTime.Now - currentSince).TotalMinutes;
			if (Limit > 0 && minutes >= Limit && told.Add(name + "|" + currentSince.Ticks) && !SpeechBubble.QuietNow && !FullScreenWatch.Active)
			{
				Program.Mainthread?.ReactAll("tired", "stretch", "yawn", "think");
				SpeechBubble.Notify(Lang.T("apptime.long", name, Pomodoro.Minutes(minutes)), 12);
			}
			if ((DateTime.Now - lastSave).TotalMinutes >= 2) Save();
		}

		/// <summary>The list of today in a bubble.</summary>
		public static void Show()
		{
			var list = Today(6);
			if (list.Count == 0) { SpeechBubble.Say(Lang.T(Enabled ? "apptime.none" : "apptime.off"), 8); return; }
			SpeechBubble.Say(Lang.T("apptime.today") + "\n" + string.Join("\n", list.Select(x => "• " + x.name + ": " + Pomodoro.Minutes(x.minutes))), 20);
		}

		static void Load()
		{
			if (days != null) return;
			try { days = File.Exists(FilePath) ? JsonText.Deserialize<Dictionary<string, Dictionary<string, int>>>(File.ReadAllText(FilePath)) : null; }
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			days ??= new Dictionary<string, Dictionary<string, int>>();
		}

		/// <summary>Write the minutes counted so far (a backup takes the file).</summary>
		internal static void Flush() { if (!SelfTest.Running) Save(); }

		static void Save()
		{
			if (days == null) return;
			lastSave = DateTime.Now;
			string oldest = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
			foreach (var k in days.Keys.Where(k => string.CompareOrdinal(k, oldest) < 0).ToList()) days.Remove(k);
			try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonText.SerializeIndented(days)); }
			catch (Exception skipped) { LogFile.Ignored(skipped); }
		}

		/// <summary>Self-test: count seconds for a program today (not saved).</summary>
		internal static void TestAdd(string name, int seconds) { var d = Day(DateTime.Today); d[name] = (d.TryGetValue(name, out int s) ? s : 0) + seconds; }
		internal static void TestRemove(string name) => Day(DateTime.Today).Remove(name);
	}
}
