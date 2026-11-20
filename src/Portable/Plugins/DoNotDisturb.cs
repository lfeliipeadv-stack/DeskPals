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
	// ====================================================================== do not disturb

	/// <summary>
	/// "Do not disturb": the pet keeps quiet (no bubbles of its own, no sounds; notices wait, see
	/// <see cref="SpeechBubble.QuietNow"/>) for a while chosen by the user, and, if chosen, by itself during a call
	/// (the microphone or the camera in use by another program, read from what Windows records for privacy).
	/// </summary>
	public static class DoNotDisturb
	{
		static Timer timer;
		static bool wasActive;

		/// <summary>Raised when the mode starts or ends (menus, the start page).</summary>
		public static event Action Changed;

		/// <summary>Turned on by hand until this moment (MinValue = off, MaxValue = until turned off).</summary>
		public static DateTime Until
		{
			get => long.TryParse(Program.MyData.GetSetting("DndUntil"), out long t) && t > 0 ? new DateTime(t) : DateTime.MinValue;
			private set => Program.MyData.SetSetting("DndUntil", value == DateTime.MinValue ? "" : value.Ticks.ToString());
		}

		/// <summary>Keep quiet by itself while the microphone or the camera is in use (a call, a meeting).</summary>
		public static bool Calls
		{
			get => Program.MyData.GetSettingBool("DndCalls", false);
			set { Program.MyData.SetSetting("DndCalls", value); Apply(); }
		}

		public static bool Manual => DateTime.Now < Until;
		/// <summary>The microphone or the camera is in use right now (checked every few seconds).</summary>
		public static bool InCall { get; private set; }
		public static bool Active => Manual || (Calls && InCall);

		/// <summary>Self-test: pretend a call is going on.</summary>
		internal static bool? TestCall;

		/// <summary>Choices in minutes; 0 = until turned off.</summary>
		internal static readonly int[] Choices = { 30, 60, 120, 240, 0 };

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 4000 }; timer.Tick += (s, e) => Check(); }
			timer.Start();   // also ends the manual mode on time
			Check();
		}

		/// <summary>Turn on for some minutes (0 = until turned off).</summary>
		public static void Start(int minutes)
		{
			Until = minutes <= 0 ? DateTime.MaxValue : DateTime.Now.AddMinutes(minutes);
			Check();
			Program.Mainthread?.ReactAll("sleep", "sit");
		}

		public static void Stop()
		{
			Until = DateTime.MinValue;
			Check();
		}

		/// <summary>"until 15:30", "until you turn it off", "in a call".</summary>
		public static string StatusText =>
			Manual ? (Until == DateTime.MaxValue ? Lang.T("dnd.untilOff") : Lang.T("dnd.until", Until.ToString("HH:mm")))
			: Calls && InCall ? Lang.T("dnd.inCall") : null;

		/// <summary>Text of a choice: "30 min", "1 h", "Until I turn it off".</summary>
		public static string ChoiceText(int minutes) => minutes <= 0 ? Lang.T("dnd.forever") : Pomodoro.Minutes(minutes);

		internal static void Check()
		{
			InCall = Calls && (TestCall ?? (DeviceInUse("microphone") || DeviceInUse("webcam")));
			if (Until != DateTime.MinValue && !Manual) Until = DateTime.MinValue;   // time is over
			bool now = Active;
			if (now == wasActive) return;
			wasActive = now;
			SpeechBubble.QuietChanged();   // notices held during the quiet time come back
			Changed?.Invoke();
		}

		/// <summary>
		/// Another program uses the microphone or the camera: Windows writes LastUsedTimeStop = 0 for it while in use
		/// (Settings › Privacy shows the same). The pet's own voice input (PowerShell) does not count.
		/// </summary>
		static bool DeviceInUse(string device)
		{
			try
			{
				using (var root = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + device))
				{
					if (root == null) return false;
					foreach (var name in root.GetSubKeyNames())
					{
						if (name == "NonPackaged")
						{
							using (var np = root.OpenSubKey(name))
								foreach (var app in np?.GetSubKeyNames() ?? new string[0])
								{
									if (app.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0 || app.IndexOf("DesktopPet", StringComparison.OrdinalIgnoreCase) >= 0) continue;
									using (var k = np.OpenSubKey(app)) if (InUse(k)) return true;
								}
						}
						else using (var k = root.OpenSubKey(name)) if (InUse(k)) return true;
					}
				}
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			return false;
		}

		static bool InUse(Microsoft.Win32.RegistryKey k) =>
			k != null && k.GetValue("LastUsedTimeStop") is long stop && stop == 0 && k.GetValue("LastUsedTimeStart") is long start && start > 0;
	}
}
