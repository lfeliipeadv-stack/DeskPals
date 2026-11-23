using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace DeskPalsWeather
{
	/// <summary>
	/// The snow, leaves, rain and blossom: Snowhush (PolyForm Noncommercial, by Mykhailo Bezruchko), next to this program as
	/// DeskPals-Snowhush.exe, run with "--hosted". Its settings are written under HKCU\Software\DeskPals\Snowhush with its
	/// own names; window messages tell it to read them again, to pause, or to clear the snow.
	/// </summary>
	internal static class Engine
	{
		const string Key = @"Software\DeskPals\Snowhush";
		static Process process;

		/// <summary>Next to this program; in the repository, the build of extensions/snowhush-src.</summary>
		public static string Exe()
		{
			string here = AppContext.BaseDirectory;
			string next = Path.Combine(here, "DeskPals-Snowhush.exe");
			if (File.Exists(next)) return next;
			var dir = new DirectoryInfo(here);
			for (int i = 0; i < 7 && dir != null; i++, dir = dir.Parent)
			{
				string dev = Path.Combine(dir.FullName, "extensions", "snowhush-src", "windows", "target", "release", "Snowhush.exe");
				if (File.Exists(dev)) return dev;
			}
			return null;
		}

		public static bool Running => process != null && !process.HasExited;

		/// <summary>Fall this kind now (or stop with null).</summary>
		public static string Show(Kind? kind, int intensity, bool onWindows, bool melt, bool inScreenshots = false, System.Drawing.Color? color = null, int flakeSize = 0, bool behind = false)
		{
			if (kind == null || kind == Kind.None) { Stop(); return ""; }
			string exe = Exe();
			if (exe == null) return "noEngine";
			using (var k = Registry.CurrentUser.CreateSubKey(Key))
			{
				void Set(string name, int v) => k.SetValue(name, v, RegistryValueKind.DWord);
				Set("welcomed", 1); Set("intensity", Math.Clamp(intensity, 0, 100)); Set("mode", (int)kind.Value); Set("autumn", kind == Kind.Leaves ? 1 : 0);
				Set("mixed", 0); Set("settleGround", 1); Set("snowOnWindows", onWindows ? 1 : 0); Set("meltSnow", melt ? 1 : 0);
				if (k.GetValue("fallingOpacity") == null) Set("fallingOpacity", 100);
				Set("showInScreenshots", inScreenshots ? 1 : 0);
				Set("tint", Tint(color));
				Set("flakeScale", Scale(flakeSize)); Set("behindWindows", behind ? 1 : 0);
			}
			if (Running) { Post("DeskPalsSnowhushReload", 0); return ""; }
			if (Process.GetProcessesByName("Snowhush").Any(p => { try { return !p.MainModule.FileName.Equals(exe, StringComparison.OrdinalIgnoreCase); } catch (Exception) { return true; } finally { p.Dispose(); } }))
				return "standalone";
			try { process = Process.Start(new ProcessStartInfo(exe, "--hosted --parent " + Environment.ProcessId) { UseShellExecute = false }); StartedAt = DateTime.Now; return ""; }
			catch (Exception ex) { return ex.Message; }
		}

		/// <summary>The colour for Snowhush (its "tint"): 0 = its own, else 0x01RRGGBB.</summary>
		/// <summary>The size of the flakes for Snowhush, in hundredths: small 100 (its own), medium 160, large 230.</summary>
		public static int Scale(int flakeSize) => flakeSize switch { 2 => 230, 1 => 160, _ => 100 };

		/// <summary>When it was started (it takes some seconds before the first flakes show).</summary>
		public static DateTime StartedAt { get; private set; }

		public static int Tint(System.Drawing.Color? c) => c == null ? 0 : 0x0100_0000 | (c.Value.R << 16) | (c.Value.G << 8) | c.Value.B;

		public static void Pause(bool paused) { if (Running) Post("DeskPalsSnowhushPause", paused ? 1 : 0); }
		public static void Clear() { if (Running) Post("DeskPalsSnowhushClear", 0); }

		public static void Stop()
		{
			if (!Running) { process = null; return; }
			var p = process; process = null;
			if (!Post(0x0010, 0)) try { p.Kill(); } catch (Exception) { }
			Task.Run(() => { try { if (!p.WaitForExit(4000)) p.Kill(); } catch (Exception) { } });
		}

		/// <summary>
		/// Asked to fall behind the windows, Snowhush could not (Windows refused its window there) and falls in front of
		/// them: its log says so, and its window is one of those on top of the screen instead of under the icons.
		/// </summary>
		public static bool BehindFailed()
		{
			if (!Running) return false;
			try
			{
				string log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Snowhush", "snowhush.log");
				if (!File.Exists(log)) return false;
				using var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
				if (!new StreamReader(stream).ReadToEnd().Contains("overlay behind windows failed")) return false;
				uint id = (uint)process.Id;
				bool onTop = false;
				var cls = new StringBuilder(32);
				EnumWindows((h, l) =>
				{
					GetWindowThreadProcessId(h, out uint pid);
					if (pid != id || !IsWindowVisible(h)) return true;
					cls.Clear();
					GetClassName(h, cls, cls.Capacity);
					if (cls.ToString() == "SnowhushOverlay") { onTop = true; return false; }
					return true;
				}, IntPtr.Zero);
				return onTop;
			}
			catch (Exception) { return false; }
		}

		delegate bool EnumProc(IntPtr h, IntPtr l);
		[DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr l);
		[DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder name, int max);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
		[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
		[DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string name);

		static bool Post(string message, int w) => Post(RegisterWindowMessage(message), w);
		static bool Post(uint message, int w)
		{
			if (process == null) return false;
			IntPtr h = IntPtr.Zero;
			while ((h = FindWindowEx(IntPtr.Zero, h, "SnowhushController", null)) != IntPtr.Zero)
			{
				GetWindowThreadProcessId(h, out uint pid);
				if (pid == (uint)process.Id) return PostMessage(h, message, (IntPtr)w, IntPtr.Zero);
			}
			return false;
		}
	}

	/// <summary>The weather now at a place (Open-Meteo, no key): its WMO code, cloud cover, wind and whether it is day.</summary>
	internal static class Forecast
	{
		static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

		public sealed class Now { public int Code; public double Wind; public int Clouds; public bool Day; public double Temp; }

		public static async Task<Now> Read(double lat, double lon)
		{
			string url = FormattableString.Invariant($"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=weather_code,wind_speed_10m,cloud_cover,is_day,temperature_2m");
			var c = JsonNode.Parse(await http.GetStringAsync(url))?["current"];
			if (c == null) return null;
			return new Now { Code = (int?)c["weather_code"] ?? 0, Wind = (double?)c["wind_speed_10m"] ?? 0, Clouds = (int?)c["cloud_cover"] ?? 0, Day = ((int?)c["is_day"] ?? 1) == 1, Temp = (double?)c["temperature_2m"] ?? 20 };
		}

		/// <summary>A city by its name: its full name and position, or null.</summary>
		public static async Task<(string name, double lat, double lon)?> Find(string name)
		{
			string lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			var j = JsonNode.Parse(await http.GetStringAsync("https://geocoding-api.open-meteo.com/v1/search?count=1&language=" + lang + "&name=" + Uri.EscapeDataString(name)));
			var r = j?["results"]?[0];
			if (r == null) return null;
			string full = string.Join(", ", new[] { (string)r["name"], (string)r["admin1"], (string)r["country"] }.Where(s => !string.IsNullOrEmpty(s)));
			return (full, (double)r["latitude"], (double)r["longitude"]);
		}

		public static bool Snow(int code) => (code >= 71 && code <= 77) || code == 85 || code == 86;
		public static bool Rain(int code) => (code >= 51 && code <= 67) || (code >= 80 && code <= 82) || code >= 95;
		public static bool Fog(int code) => code == 45 || code == 48;
	}

	/// <summary>The windows on the screen, from the front one to the back (visible, not minimized, not hidden, with a title; ours left out).</summary>
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
		[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
		[StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }

		public static List<(IntPtr handle, Rectangle bounds)> List()
		{
			var list = new List<(IntPtr, Rectangle)>();
			uint me = (uint)Environment.ProcessId;
			var title = new StringBuilder(8);
			EnumWindows((h, l) =>
			{
				if (!IsWindowVisible(h) || IsIconic(h) || (GetWindowLong(h, -20) & 0x80) != 0) return true;
				if (DwmGetWindowAttribute(h, 14, out int cloaked, 4) == 0 && cloaked != 0) return true;
				GetWindowThreadProcessId(h, out uint pid);
				if (pid == me) return true;
				title.Clear();
				if (GetWindowText(h, title, title.Capacity) == 0 || !GetWindowRect(h, out var r) || r.Right - r.Left < 80 || r.Bottom - r.Top < 40) return true;
				if (title.ToString() == "Sheep") return true;   // the pets of DeskPals
				list.Add((h, Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom)));
				return true;
			}, IntPtr.Zero);
			return list;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int max);
		[DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int state);

		public static string ClassOf(IntPtr h) { var s = new StringBuilder(64); GetClassName(h, s, s.Capacity); return s.ToString(); }

		/// <summary>The desktop or the taskbar (not a program's window).</summary>
		public static bool IsShell(IntPtr h) => h == IntPtr.Zero || ClassOf(h) is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";

		[DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] static extern int DwmGetWindowRect(IntPtr hWnd, int attr, out RECT value, int size);
		[DllImport("user32.dll")] static extern bool IsZoomed(IntPtr hWnd);

		/// <summary>
		/// The windows the Christmas lights go round, from the front one to the back: their edges as one sees them (not the
		/// invisible border Windows keeps round them), over this screen; maximized windows and ones as big as the screen are
		/// left out (their edges are off the screen).
		/// </summary>
		public static List<(Rectangle bounds, bool lit)> Frames(Rectangle screen)
		{
			var frames = new List<(Rectangle bounds, bool lit)>();
			foreach (var (h, bounds) in List())
			{
				var r = DwmGetWindowRect(h, 9, out var f, Marshal.SizeOf<RECT>()) == 0 ? Rectangle.FromLTRB(f.Left, f.Top, f.Right, f.Bottom) : bounds;
				if (!r.IntersectsWith(screen)) continue;
				if (frames.Any(front => front.bounds.Contains(r))) continue;   // wholly behind a window in front: nothing of it shows
				// a maximized window, or one as big as the screen, has no lights (its edges are off the screen), but it hides those behind it
				frames.Add((r, !IsZoomed(h) && !CoversScreen(r, screen)));
			}
			return frames;
		}

		/// <summary>A program fills a whole screen in front (a game, a film, a presentation).</summary>
		public static bool FullScreenInFront()
		{
			try { if (SHQueryUserNotificationState(out int s) == 0 && (s == 3 || s == 4)) return true; } catch (Exception) { }
			IntPtr h = GetForegroundWindow();
			if (IsShell(h)) return false;
			GetWindowThreadProcessId(h, out uint pid);
			if (pid == (uint)Environment.ProcessId || !GetWindowRect(h, out var r)) return false;
			return CoversScreen(Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom), Screen.FromHandle(h).Bounds);
		}

		public static bool CoversScreen(Rectangle window, Rectangle screen) =>
			window.Left <= screen.Left && window.Top <= screen.Top && window.Right >= screen.Right && window.Bottom >= screen.Bottom;

		/// <summary>The pets of DeskPals on the screen (their windows are called "Sheep", as in the eSheep).</summary>
		public static List<(IntPtr handle, Rectangle bounds)> Pets()
		{
			var list = new List<(IntPtr, Rectangle)>();
			var title = new StringBuilder(8);
			EnumWindows((h, l) =>
			{
				if (!IsWindowVisible(h)) return true;
				title.Clear();
				if (GetWindowText(h, title, title.Capacity) == 0 || title.ToString() != "Sheep" || !GetWindowRect(h, out var r)) return true;
				list.Add((h, Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom)));
				return true;
			}, IntPtr.Zero);
			return list;
		}

		/// <summary>The program in front ("chrome", "powerpnt"), lower case.</summary>
		public static string FrontProgram()
		{
			try
			{
				GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
				using var p = Process.GetProcessById((int)pid);
				return p.ProcessName.ToLowerInvariant();
			}
			catch (Exception) { return ""; }
		}

		/// <summary>The visible top edges of the windows over a screen, as (x0, x1, y): parts hidden behind a window in front are left out.</summary>
		public static List<(int x0, int x1, int y)> Tops(Rectangle screen)
		{
			// the edges one sees (not the invisible border Windows keeps round a window to resize it)
			var list = List();
			var wins = list.Select(w => DwmGetWindowRect(w.handle, 9, out var f, Marshal.SizeOf<RECT>()) == 0 ? Rectangle.FromLTRB(f.Left, f.Top, f.Right, f.Bottom) : w.bounds).ToList();
			var tops = new List<(int, int, int)>();
			for (int w = 0; w < wins.Count; w++)
			{
				var r = wins[w];
				int round = Rounding(list[w].handle), mine = tops.Count;
				if (r.Top <= screen.Top + 4 || r.Top >= screen.Bottom || r.Right <= screen.Left || r.Left >= screen.Right) continue;
				bool Seen(int x) { for (int a = 0; a < w; a++) if (wins[a].Contains(x, r.Top)) return false; return true; }
				int left = Math.Max(r.Left, screen.Left), right = Math.Min(r.Right, screen.Right), start = -1;
				for (int x = left; x < right; x += 4)
				{
					bool seen = Seen(x);
					if (seen && start < 0) { start = x; while (start > left && Seen(start - 1)) start--; }   // the exact point it comes out
					if (!seen && start >= 0) { int end = x; while (end > start && !Seen(end - 1)) end--; tops.Add((start, end, r.Top)); start = -1; }
				}
				if (start >= 0) tops.Add((start, right, r.Top));
				// the ends of the window itself stop before its rounded corners (a cut by a window in front stays where it is)
				for (int i = tops.Count - 1; i >= mine; i--)
				{
					var (x0, x1, y) = tops[i];
					if (x0 == r.Left) x0 += round;
					if (x1 == r.Right) x1 -= round;
					if (x1 - x0 < 8) tops.RemoveAt(i); else tops[i] = (x0, x1, y);
				}
			}
			return tops;
		}

		[DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);

		/// <summary>How far the rounded corners of a window reach in: 8 points on Windows 11 (more on a screen with zoom), none on a maximized window or before Windows 11.</summary>
		public static int Rounding(IntPtr h)
		{
			if (Environment.OSVersion.Version.Build < 22000 || IsZoomed(h)) return 0;
			// a window that asked for square corners (preference 1) has none
			if (DwmGetWindowAttribute(h, 33, out int pref, 4) == 0 && pref == 1) return 0;
			uint dpi = 96;
			try { dpi = GetDpiForWindow(h); } catch (Exception) { }
			return (int)Math.Round(8 * (dpi == 0 ? 96 : dpi) / 96.0);
		}
	}
}
