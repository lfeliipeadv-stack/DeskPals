using System.Runtime.InteropServices;
using System.Text.Json;

namespace DeskPalsWeather
{
	/// <summary>What is paused now: the effects (what falls, fog, sun, grass), the sound, and why.</summary>
	internal readonly record struct Paused(bool Effects, bool Sound, string By)
	{
		public static readonly Paused None = new(false, false, "");
	}

	/// <summary>
	/// Decides what shows, every few seconds: the weather of the city (or the inverted one, or the seasons on dry days),
	/// the effects chosen, the pauses (off, a program in full screen, a program in front, the battery), and tells the
	/// snow engine, the overlay and the sound. It writes what falls in a small file DeskPals reads, so the
	/// pets react, follows the language and theme of DeskPals, and keeps the shortcuts.
	/// </summary>
	internal static class Controller
	{
		public static Config Config = Config.Load();
		static readonly System.Windows.Forms.Timer timer = new() { Interval = 4000 };
		static Forecast.Now weather;
		static DateTime weatherAt = DateTime.MinValue;
		public static string Problem = "";
		public static Paused Now = Paused.None;
		public static Kind? Falling;
		public static Forecast.Now Weather => weather;
		static readonly Random rnd = new();
		static (double fog, double sun, double wind, double grass) drift = (0.3, 0.3, 0.3, 0.3), driftTo = (0.3, 0.3, 0.3, 0.3);
		static DateTime driftAt;
		static bool busy;

		public static event Action Changed;

		public static void Start()
		{
			timer.Tick += async (s, e) => await Update();
			timer.Start();
			Hotkeys.Apply();
			_ = Update();
		}

		/// <summary>Something changed in the settings: save and show it now.</summary>
		public static void Apply() { Config.Save(); Hotkeys.Apply(); _ = Update(); }

		public static async Task Update()
		{
			if (busy) return;
			busy = true;
			try { await UpdateNow(); } finally { busy = false; }
		}

		static async Task UpdateNow()
		{
			var c = Config;
			// the language and theme of DeskPals (or the language chosen here)
			string lang = T.Pick(c);
			if (lang != T.Code) T.Use(lang);
			DeskPals.ApplyTheme(c);

			if (c.Follow > 0 && c.City.Length > 0 && (DateTime.Now - weatherAt).TotalMinutes > 15)
			{
				weatherAt = DateTime.Now;
				try { weather = await Forecast.Read(c.Lat, c.Lon); } catch (Exception) { weatherAt = DateTime.Now.AddMinutes(-12); }
			}
			bool battery = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;
			var p = Pauses(c, DesktopWindows.FrontProgram(), c.PauseFullscreen && DesktopWindows.FullScreenInFront(), battery);
			Now = p;
			// "Test now": a few seconds of strong snow, whatever the pauses say
			bool testing = DateTime.Now < TestUntil;
			if (testing) { p = Paused.None; Now = p; }
			Front = DesktopWindows.FrontProgram();
			bool less = battery && c.Battery == 1;

			var (kind, fog, sun, wind) = Decide(c, weather, DateTime.Now);
			double grass = c.Grass / 100.0;
			if (c.Random)
			{
				if (DateTime.Now > driftAt) { driftAt = DateTime.Now.AddMinutes(2); driftTo = (rnd.NextDouble() * 0.6, rnd.NextDouble() * 0.6, rnd.NextDouble(), 0.2 + rnd.NextDouble() * 0.6); }
				double k = 0.08;
				drift = (drift.fog + (driftTo.fog - drift.fog) * k, drift.sun + (driftTo.sun - drift.sun) * k, drift.wind + (driftTo.wind - drift.wind) * k, drift.grass + (driftTo.grass - drift.grass) * k);
				if (c.Follow == 0) { fog = drift.fog; sun = drift.sun; wind = drift.wind; grass = drift.grass; }
			}
			if (less) fog = 0;
			if (testing) kind = kind ?? Kind.Snow;
			Falling = p.Effects ? null : kind;
			int intensity = testing ? 100 : less ? c.Intensity / 2 : c.Intensity;

			// the snow engine: a pause keeps what has piled up and goes on after
			BehindFailed = false;
			if (kind == null || (!c.On && !testing)) Engine.Stop();
			else
			{
				// rain behind the windows: none on them either (Snowhush draws what lies on a window in front of it, and for
				// the rain that is the drops running down the glass)
				bool behind = kind == Kind.Rain && c.RainPlace == 1;
				Problem = Engine.Show(kind, intensity, c.OnWindows && !behind, c.Melt, c.InScreenshots, c.ColorOf(Name(kind.Value)), c.FlakeSize, behind);
				Engine.Pause(p.Effects);
				BehindFailed = behind && Engine.BehindFailed();
			}
			Overlay.Fog = fog; Overlay.Sun = sun; Overlay.Wind = wind; Overlay.Grass = grass;
			Overlay.FogColor = c.ColorOf("fog"); Overlay.SunColor = c.ColorOf("sun"); Overlay.GrassColor = c.ColorOf("grass");
			Overlay.OnTop = c.OnTop; Overlay.InScreenshots = c.InScreenshots; Overlay.Slow = less;
			Overlay.Lights = (c.On || testing) && c.LightsToday(DateTime.Now);
			ApplyLights(c);
			Overlay.Apply(p.Effects);
			Ambience.QuietOnCalls = c.QuietOnCalls;
			Ambience.Set(c.Sound && !p.Sound && c.On ? kind ?? (wind > 0.5 ? Kind.Snow : (Kind?)null) : null, c.Volume / 100.0);
			WriteState(kind, fog, sun, wind, p.Effects);
			Shown = (p.Effects ? null : kind, intensity, p.Effects ? 0 : fog, p.Effects ? 0 : sun, p.Effects ? 0 : grass, c.Sound && !p.Sound && (c.On || testing));
			WriteScenes();
			Changed?.Invoke();
		}

		/// <summary>The rain was asked to fall behind the windows, and Windows did not let it: it falls in front of them.</summary>
		public static bool BehindFailed;

		public static DateTime TestUntil;
		public static string Front = "";
		/// <summary>What shows now (for the status line and the tray icon).</summary>
		public static (Kind? kind, int intensity, double fog, double sun, double grass, bool sound) Shown;

		/// <summary>Snowhush was just started: its first flakes take some seconds.</summary>
		public static bool Starting => Falling != null && Engine.Running && (DateTime.Now - Engine.StartedAt).TotalSeconds < 12;

		/// <summary>Fifteen seconds of strong snow (or of what is chosen), even when paused or off: to see it working.</summary>
		public static void Test() { TestUntil = DateTime.Now.AddSeconds(15); _ = Update(); }

		public static bool Testing => DateTime.Now < TestUntil;

		/// <summary>The settings of the Christmas lights handed to their drawing (the shape, the colours, how bright, how they light up).</summary>
		internal static void ApplyLights(Config c)
		{
			LightsArt.Shape = (LightsArt.BulbShape)Math.Clamp(c.LightsShape, 0, 5);
			LightsArt.Pattern = c.LightsColors.Count > 0 ? c.LightsColors.Select(FromRgb).ToArray() : null;
			LightsArt.Single = c.LightsMulti ? null : FromRgb(c.LightsColor);
			LightsArt.RandomColors = c.LightsRandom;
			LightsArt.Spacing = c.LightsSpacing is 20 or 25 or 30 ? c.LightsSpacing : 25;
			LightsArt.RandomAngles = c.LightsRandomAngles;
			LightsArt.Brightness = Math.Clamp(c.LightsBrightness, 0, 100) / 100f;
			LightsArt.Speed = Speeds[Math.Clamp(c.LightsSpeed, 0, 2)];
			Overlay.LightsMode = (LightsMode)Math.Clamp(c.LightsMode, 0, 3);
			string now = string.Join(",", c.LightsShape, c.LightsMode, c.LightsMulti, c.LightsColor, c.LightsBrightness, c.LightsRandom, c.LightsSpacing, c.LightsRandomAngles, c.LightsSpeed, string.Join(".", c.LightsColors));
			if (now != lightsBefore) { lightsBefore = now; LightsArt.Version++; }
		}

		static string lightsBefore;
		/// <summary>Slow, normal, fast.</summary>
		internal static readonly float[] Speeds = { 0.5f, 1f, 2f };

		public static System.Drawing.Color FromRgb(int c) => System.Drawing.Color.FromArgb(255, (c >> 16) & 255, (c >> 8) & 255, c & 255);
		public static int ToRgb(System.Drawing.Color c) => (c.R << 16) | (c.G << 8) | c.B;

		public static string Name(Kind k) => k.ToString().ToLowerInvariant();

		/// <summary>What pauses: off, a program in full screen, a program of the list in front (everything, the effects or the sound), the battery.</summary>
		internal static Paused Pauses(Config c, string front, bool fullScreen, bool onBattery)
		{
			if (!c.On) return new(true, true, "off");
			if (fullScreen) return new(true, true, "fullscreen");
			if (!string.IsNullOrEmpty(front) && c.PauseApps.Contains(front))
				return c.PauseModeOf(front) switch { 1 => new(true, false, front), 2 => new(false, true, front), _ => new(true, true, front) };
			if (onBattery && c.Battery == 2) return new(true, true, "battery");
			return Paused.None;
		}

		/// <summary>What falls and how much fog, sun and wind (0..1), from the settings and the weather.</summary>
		internal static (Kind? kind, double fog, double sun, double wind) Decide(Config c, Forecast.Now w, DateTime today)
		{
			if (c.Follow == 0 || w == null) return (c.Kind == Kind.None ? null : c.Kind, c.Fog / 100.0, c.Sun / 100.0, c.Wind / 100.0);
			bool snow = Forecast.Snow(w.Code), rain = Forecast.Rain(w.Code), fogOut = Forecast.Fog(w.Code), wet = snow || rain;
			double windOut = Math.Clamp(w.Wind / 40.0, 0, 1);
			if (c.Follow == 3)
			{
				// inverted: wet outside → sun; dry → rain, or snow on a hot day (at night nothing); fog clears; calm ↔ windy
				if (wet) return (null, 0, 0.9, 1 - windOut);
				Kind k = w.Temp >= 27 ? Kind.Snow : Kind.Rain;
				return (w.Day ? k : null, 0, w.Day ? 0 : 0.7, 1 - windOut);
			}
			Kind? kind = snow ? Kind.Snow : rain ? Kind.Rain : null;
			if (kind == null && c.Follow == 2)
			{
				bool south = c.Lat < 0;
				int m = today.Month;
				if (south ? m >= 3 && m <= 5 : m >= 9 && m <= 11) kind = Kind.Leaves;
				else if (south ? m >= 9 && m <= 11 : m >= 3 && m <= 5) kind = Kind.Blossom;
			}
			double fog = fogOut ? 0.75 : w.Clouds > 85 && !wet ? 0.2 : 0;
			double sun = w.Day && !wet && w.Clouds < 50 ? 0.6 : 0;
			return (kind, fog, sun, windOut);
		}

		public static string StateFile => Path.Combine(Config.Folder, "weather-state.json");

		/// <summary>For DeskPals: what is on the screen now, so its pets react (%LocalAppData%\DesktopPet\extensions\weather-state.json).</summary>
		internal static void WriteState(Kind? kind, double fog, double sun, double wind, bool paused, string file = null)
		{
			try
			{
				bool on = Config.On && !paused;
				var state = new { on, kind = on ? kind is Kind k ? Name(k) : null : null, fog = on ? fog : 0, sun = on ? sun : 0, wind, time = DateTime.Now.ToString("s") };
				file ??= StateFile;
				Directory.CreateDirectory(Path.GetDirectoryName(file));
				File.WriteAllText(file, JsonSerializer.Serialize(state));
			}
			catch (Exception) { }
		}

		// ------------------------------------------------------------------ the scenes, for DeskPals' menu

		public static string ScenesFile => Path.Combine(Config.Folder, "weather-scenes.json");
		public static string CommandFile => Path.Combine(Config.Folder, "weather-command.json");
		static string scenesWritten;

		/// <summary>Every scene, built-in ("b:scene.winter") and one's own ("m:name"), with its name in the language of now.</summary>
		public static List<(string id, string name, Scene scene)> AllScenes() =>
			Scene.BuiltIn.Select(b => ("b:" + b.key, T.S(b.key), b.scene)).Concat(Config.Scenes.Select(s => ("m:" + s.Name, s.Name, s))).ToList();

		/// <summary>For DeskPals: the scenes and which is on, so its tray menu can show them (written only when it changes).</summary>
		internal static void WriteScenes(string file = null)
		{
			try
			{
				var all = AllScenes();
				var now = new
				{
					on = Config.On,
					current = all.FirstOrDefault(s => s.scene.SameAs(Config)).id,
					scenes = all.Select(s => new { s.id, s.name }).ToArray(),
					labels = new { on = T.S("tray.on"), test = T.S("w.test.button"), settings = T.S("tray.settings"), scenes = T.S("tab.scenes") },
				};
				string json = JsonSerializer.Serialize(now);
				if (file == null && json == scenesWritten) return;
				File.WriteAllText(file ?? ScenesFile, json);
				if (file == null) scenesWritten = json;
			}
			catch (Exception) { }
		}

		/// <summary>What DeskPals asks (from its menu): a scene, on or off, a test.</summary>
		internal static bool RunCommand(string json)
		{
			try
			{
				var j = System.Text.Json.Nodes.JsonNode.Parse(json);
				if (DateTime.TryParse((string)j?["time"], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var at) && (DateTime.Now - at).TotalSeconds > 30) return false;
				if ((string)j?["scene"] is string id)
				{
					var s = AllScenes().FirstOrDefault(x => x.id == id).scene;
					if (s == null) return false;
					s.ApplyTo(Config); Config.On = true;
				}
				else if ((bool?)j?["on"] is bool on) Config.On = on;
				else if ((bool?)j?["test"] == true) { Test(); return true; }
				else if ((bool?)j?["clear"] == true) { Engine.Clear(); return true; }
				else return false;
				Apply();
				return true;
			}
			catch (Exception) { return false; }
		}

		public static void Quit()
		{
			Engine.Stop();
			Ambience.Set(null, 0);
			Overlay.Apply(true);
			try { File.Delete(StateFile); File.Delete(ScenesFile); } catch (Exception) { }
		}
	}

	/// <summary>The shortcuts: turn the weather on or off, open the settings. Chosen from a list; one taken by another program is said.</summary>
	internal static class Hotkeys
	{
		public static readonly (string text, int mods, Keys key)[] Choices =
		{
			("Ctrl+Alt+W", 0x1 | 0x2, Keys.W), ("Ctrl+Shift+W", 0x2 | 0x4, Keys.W), ("Ctrl+Alt+Shift+W", 0x1 | 0x2 | 0x4, Keys.W),
			("Ctrl+Alt+N", 0x1 | 0x2, Keys.N), ("Ctrl+Alt+C", 0x1 | 0x2, Keys.C), ("Ctrl+Alt+F9", 0x1 | 0x2, Keys.F9), ("Ctrl+Alt+F10", 0x1 | 0x2, Keys.F10),
		};

		[DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, int mods, int key);
		[DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
		static Sink sink;
		public static string Busy = "";

		public static void Apply()
		{
			sink ??= new Sink();
			UnregisterHotKey(sink.Handle, 1); UnregisterHotKey(sink.Handle, 2);
			Busy = "";
			void Reg(int id, int index) { if (index >= 0 && index < Choices.Length && !RegisterHotKey(sink.Handle, id, Choices[index].mods | 0x4000, (int)Choices[index].key)) Busy += Choices[index].text + " "; }
			Reg(1, Controller.Config.KeyToggle);
			if (Controller.Config.KeySettings != Controller.Config.KeyToggle) Reg(2, Controller.Config.KeySettings);
		}

		sealed class Sink : NativeWindow
		{
			public Sink() { CreateHandle(new CreateParams()); }
			protected override void WndProc(ref Message m)
			{
				if (m.Msg == 0x0312)
				{
					if ((int)m.WParam == 1) { Controller.Config.On = !Controller.Config.On; Controller.Apply(); }
					else if ((int)m.WParam == 2) Program.ShowSettings();
				}
				base.WndProc(ref m);
			}
		}
	}
}
