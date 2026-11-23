using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopPet
{
	/// <summary>The windows on the screen as the user sees them: visible, not minimized, not hidden by Windows (cloaked), with a title.</summary>
	internal static class DesktopWindows
	{
		delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
		[DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
		[DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
		[DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
		[DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hWnd, StringBuilder s, int max);
		[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
		[DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int index);
		[DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out int value, int size);
		[StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }

		public sealed class Win { public IntPtr Handle; public Rectangle Bounds; public string Title; public uint Pid; }

		/// <summary>The windows from the front one to the back one. Our own windows (pets, bubbles, notes) are left out.</summary>
		public static List<Win> List()
		{
			var list = new List<Win>();
			uint me = (uint)Environment.ProcessId;
			var title = new StringBuilder(256);
			EnumWindows((h, l) =>
			{
				if (!IsWindowVisible(h) || IsIconic(h)) return true;
				if ((GetWindowLong(h, -20) & 0x00000080) != 0) return true;   // tool windows (floating palettes, overlays)
				if (DwmGetWindowAttribute(h, 14 /* DWMWA_CLOAKED */, out int cloaked, 4) == 0 && cloaked != 0) return true;
				GetWindowThreadProcessId(h, out uint pid);
				if (pid == me) return true;
				title.Clear();
				GetWindowText(h, title, title.Capacity);
				if (title.Length == 0 || !GetWindowRect(h, out var r) || r.Right - r.Left < 40 || r.Bottom - r.Top < 20) return true;
				list.Add(new Win { Handle = h, Bounds = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom), Title = title.ToString(), Pid = pid });
				return true;
			}, IntPtr.Zero);
			return list;
		}

		static readonly Dictionary<uint, (string name, DateTime at)> names = new Dictionary<uint, (string, DateTime)>();

		/// <summary>The program of a window, as Windows names its process ("chrome", "code"), lower case; "" when unknown.</summary>
		public static string ProgramOf(IntPtr hWnd)
		{
			GetWindowThreadProcessId(hWnd, out uint pid);
			return ProgramOfPid(pid);
		}

		public static string ProgramOfPid(uint pid)
		{
			if (pid == 0) return "";
			lock (names)
			{
				if (names.TryGetValue(pid, out var n) && (DateTime.Now - n.at).TotalSeconds < 60) return n.name;
				string name = "";
				try { using (var p = Process.GetProcessById((int)pid)) name = p.ProcessName.ToLowerInvariant(); }
				catch (Exception) { }
				names[pid] = (name, DateTime.Now);
				return name;
			}
		}
	}

	// ====================================================================== programs the pets stay away from

	/// <summary>
	/// Off unless turned on. Programs whose windows the pets never walk on, land on or follow (a video editor, a game, a
	/// presentation): their windows are as if they were not there for the pets.
	/// </summary>
	public static class WindowRules
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("IgnoreAppsOn", false);
			set => Program.MyData.SetSetting("IgnoreAppsOn", value);
		}

		/// <summary>The programs (process names, lower case, without ".exe").</summary>
		public static List<string> Apps
		{
			get => Program.MyData.GetSetting("IgnoreApps").Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Clean).Where(a => a.Length > 0).Distinct().ToList();
			set => Program.MyData.SetSetting("IgnoreApps", string.Join("|", (value ?? new List<string>()).Select(Clean).Where(a => a.Length > 0).Distinct()));
		}

		/// <summary>"Code.exe" → "code".</summary>
		public static string Clean(string name)
		{
			name = (name ?? "").Trim().ToLowerInvariant();
			if (name.EndsWith(".exe")) name = name.Substring(0, name.Length - 4);
			return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' || c == ' ').ToArray()).Trim();
		}

		static HashSet<string> cache;
		static string cacheOf;

		/// <summary>True when the pets must not use this window.</summary>
		public static bool Ignored(IntPtr hWnd)
		{
			if (!Enabled) return false;
			string raw = Program.MyData.GetSetting("IgnoreApps");
			if (raw.Length == 0) return false;
			if (cacheOf != raw) { cache = new HashSet<string>(Apps); cacheOf = raw; }
			return cache.Contains(DesktopWindows.ProgramOf(hWnd));
		}

		/// <summary>The programs with a window open now (to choose from), with one of their titles.</summary>
		public static List<(string app, string title)> OpenPrograms() =>
			DesktopWindows.List().Select(w => (app: DesktopWindows.ProgramOfPid(w.Pid), w.Title)).Where(x => x.app.Length > 0)
				.GroupBy(x => x.app).Select(g => g.First()).OrderBy(x => x.app).ToList();
	}
}
