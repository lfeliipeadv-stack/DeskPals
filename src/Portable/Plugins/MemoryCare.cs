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
	// ====================================================================== 12. memory

	/// <summary>
	/// Off unless turned on. Watches how much of the memory (RAM) is in use; when it stays high for a few minutes the pet
	/// offers to tidy it up (or does it by itself). Simple tidy: every program it may open gives back the memory it is not
	/// using (no administrator needed). Full tidy: also empties the standby, modified and file caches of Windows, through
	/// a short-lived copy of the program started as administrator (Windows asks first). Our own code (Windows API).
	/// </summary>
	public static class MemoryCare
	{
		static Timer timer;
		static DateTime? highSince;
		static DateTime lastClean = DateTime.MinValue, lastAsk = DateTime.MinValue;
		static bool cleaning;

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("MemoryCare", false);
			set { Program.MyData.SetSetting("MemoryCare", value); Apply(); }
		}

		/// <summary>Percentage of memory in use that counts as high.</summary>
		public static int Limit
		{
			get { int v = Program.MyData.GetSettingInt("MemoryLimit", 85); return v >= 50 && v <= 98 ? v : 85; }
			set => Program.MyData.SetSetting("MemoryLimit", value);
		}

		/// <summary>Tidy by itself (simple tidy) instead of asking first.</summary>
		public static bool Auto
		{
			get => Program.MyData.GetSettingBool("MemoryAuto", false);
			set => Program.MyData.SetSetting("MemoryAuto", value);
		}

		/// <summary>Minutes the memory has to stay high, and minimum minutes between two tidies / two questions.</summary>
		internal const int HighMinutes = 2, CleanEveryMinutes = 10, AskEveryMinutes = 30;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 30000 }; timer.Tick += (s, e) => Check(DateTime.Now, Status().load); }
			if (Enabled) timer.Start(); else { timer.Stop(); highSince = null; }
		}

		// ------------------------------------------------------------------ reading the memory

		[StructLayout(LayoutKind.Sequential)]
		struct MEMORYSTATUSEX
		{
			public uint dwLength, dwMemoryLoad;
			public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
		}
		[DllImport("kernel32.dll", SetLastError = true)] static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);

		/// <summary>Memory in use now (percentage), total and available (bytes).</summary>
		public static (int load, long total, long available) Status()
		{
			var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
			if (!GlobalMemoryStatusEx(ref m)) return (0, 0, 0);
			return ((int)m.dwMemoryLoad, (long)m.ullTotalPhys, (long)m.ullAvailPhys);
		}

		/// <summary>"1,2 GB" / "350 MB".</summary>
		public static string Size(long bytes)
		{
			double mb = Math.Abs(bytes) / 1048576.0;
			return mb >= 1024 ? (mb / 1024).ToString("0.0") + " GB" : Math.Round(mb) + " MB";
		}

		// ------------------------------------------------------------------ when to offer

		/// <summary>What to do now: nothing, ask, or tidy. Separate from the clock, so the self-test can play with time.</summary>
		internal enum Decision { None, Ask, Clean }

		internal static Decision Decide(DateTime now, int load, int limit, bool auto, ref DateTime? since, DateTime lastClean, DateTime lastAsk, bool quiet)
		{
			if (load < limit) { since = null; return Decision.None; }
			if (since == null) since = now;
			if (quiet || (now - since.Value).TotalMinutes < HighMinutes) return Decision.None;
			if ((now - lastClean).TotalMinutes < CleanEveryMinutes) return Decision.None;
			if (auto) return Decision.Clean;
			return (now - lastAsk).TotalMinutes < AskEveryMinutes ? Decision.None : Decision.Ask;
		}

		static void Check(DateTime now, int load)
		{
			if (!Enabled || cleaning) return;
			bool quiet = SpeechBubble.QuietNow || FullScreenWatch.Active;   // never in the middle of a game or a video
			switch (Decide(now, load, Limit, Auto, ref highSince, lastClean, lastAsk, quiet))
			{
				case Decision.Clean: Tidy(false); break;
				case Decision.Ask:
					lastAsk = now;
					SpeechBubble.Notify(Lang.T("mem.high", load), 0,
						new SpeechBubble.Choice(Lang.T("mem.tidy"), () => Tidy(false), true),
						new SpeechBubble.Choice(Lang.T("mem.later"), null));
					break;
			}
		}

		// ------------------------------------------------------------------ tidying

		const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, PROCESS_SET_QUOTA = 0x0100;
		[DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
		[DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
		[DllImport("psapi.dll", SetLastError = true)] static extern bool EmptyWorkingSet(IntPtr process);
		[DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int pid);

		/// <summary>
		/// Simple tidy: every process this user may open gives back the memory it is not using (it comes back from the
		/// disk cache when needed). The program in front is left alone, so what you are using does not slow down.
		/// Returns how many programs were tidied.
		/// </summary>
		public static int TrimPrograms()
		{
			GetWindowThreadProcessId(GetForegroundWindow(), out int front);
			int n = 0;
			foreach (var p in Process.GetProcesses())
			{
				try
				{
					if (p.Id <= 4 || p.Id == front) continue;
					IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_SET_QUOTA, false, p.Id);
					if (h == IntPtr.Zero) continue;   // a program of the system or of another user: not ours to touch
					try { if (EmptyWorkingSet(h)) n++; } finally { CloseHandle(h); }
				}
				catch (Exception) { }
				finally { p.Dispose(); }
			}
			return n;
		}

		[DllImport("ntdll.dll")] static extern int NtSetSystemInformation(int infoClass, ref int info, int length);
		[DllImport("kernel32.dll", SetLastError = true)] static extern bool SetSystemFileCacheSize(IntPtr min, IntPtr max, uint flags);
		[DllImport("advapi32.dll", SetLastError = true)] static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern bool LookupPrivilegeValue(string system, string name, out long luid);
		[DllImport("advapi32.dll", SetLastError = true)] static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TOKEN_PRIVILEGES state, int length, IntPtr previous, IntPtr returnLength);
		[DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
		[StructLayout(LayoutKind.Sequential, Pack = 4)] struct TOKEN_PRIVILEGES { public int Count; public long Luid; public int Attributes; }

		const int SystemMemoryListInformation = 80;
		const int MemoryEmptyWorkingSets = 2, MemoryFlushModifiedList = 3, MemoryPurgeStandbyList = 4;

		static bool Privilege(string name)
		{
			if (!OpenProcessToken(GetCurrentProcess(), 0x0020 /* adjust */ | 0x0008 /* query */, out IntPtr token)) return false;
			try
			{
				if (!LookupPrivilegeValue(null, name, out long luid)) return false;
				var tp = new TOKEN_PRIVILEGES { Count = 1, Luid = luid, Attributes = 2 /* enabled */ };
				return AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero) && Marshal.GetLastWin32Error() == 0;
			}
			finally { CloseHandle(token); }
		}

		static bool MemoryList(int command) { int c = command; return NtSetSystemInformation(SystemMemoryListInformation, ref c, sizeof(int)) == 0; }

		/// <summary>True if this copy of the program runs as administrator.</summary>
		public static bool IsAdmin
		{
			get { using (var id = System.Security.Principal.WindowsIdentity.GetCurrent()) return new System.Security.Principal.WindowsPrincipal(id).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); }
		}

		/// <summary>
		/// Full tidy (needs administrator): the working sets of every process, the file cache, the modified pages
		/// (written to disk) and the standby list. Returns one line per step: "name ok" or "name refused".
		/// </summary>
		public static List<string> FullClean()
		{
			var steps = new List<string>();
			bool profile = Privilege("SeProfileSingleProcessPrivilege"), quota = Privilege("SeIncreaseQuotaPrivilege");
			steps.Add("workingsets " + (profile && MemoryList(MemoryEmptyWorkingSets) ? "ok" : "refused"));
			steps.Add("filecache " + (quota && SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0) ? "ok" : "refused"));
			steps.Add("modified " + (profile && MemoryList(MemoryFlushModifiedList) ? "ok" : "refused"));
			steps.Add("standby " + (profile && MemoryList(MemoryPurgeStandbyList) ? "ok" : "refused"));
			return steps;
		}

		/// <summary>"--clean-memory=&lt;file&gt;": the copy started as administrator does the full tidy, writes how it went, and ends.</summary>
		public static void FullCleanToFile(string file)
		{
			var steps = IsAdmin ? FullClean() : new List<string> { "admin refused" };
			try { File.WriteAllLines(file, steps); } catch (Exception) { }
		}

		/// <summary>Full tidy from this copy: directly if it is already administrator, else through an elevated copy.</summary>
		static async Task<List<string>> FullCleanElevated()
		{
			if (IsAdmin) return await Task.Run(FullClean);
			string file = Path.Combine(Path.GetTempPath(), "desktoppet-memory-" + Guid.NewGuid().ToString("N") + ".txt");
			var psi = new ProcessStartInfo(Environment.ProcessPath, "\"--clean-memory=" + file + "\"") { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
			try
			{
				using (var p = Process.Start(psi))
					await Task.Run(() => p.WaitForExit(60000));
				return File.Exists(file) ? File.ReadAllLines(file).ToList() : new List<string> { "admin refused" };
			}
			catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) { return null; }   // "No" on the Windows question
			finally { try { File.Delete(file); } catch (Exception) { } }
		}

		/// <summary>Tidy now and tell how much was freed (simple, or full with administrator).</summary>
		public static async void Tidy(bool full)
		{
			if (cleaning) return;
			cleaning = true;
			try
			{
				var before = Status();
				Program.Mainthread?.ReactAll("clean", "sweep", "work", "happy");
				List<string> steps = null;
				if (full)
				{
					steps = await FullCleanElevated();
					if (steps == null) { SpeechBubble.Say(Lang.T("mem.adminNo"), 6); return; }
				}
				await Task.Run(TrimPrograms);
				await Task.Delay(1500);   // Windows takes a moment to count the memory again
				var after = Status();
				lastClean = DateTime.Now; highSince = null;
				long freed = after.available - before.available;
				bool refused = steps != null && steps.Any(s => s.EndsWith("refused"));
				string text = freed > 50L * 1048576 ? Lang.T("mem.freed", Size(freed), after.load) : Lang.T("mem.nothing", after.load);
				if (refused) text += " " + Lang.T("mem.partly");
				Program.Mainthread?.ReactAll("happy", "cheer", "dance");
				SpeechBubble.Say(text, 8);
			}
			catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "memory tidy: " + ex.Message); }
			finally { cleaning = false; }
		}
	}
}
