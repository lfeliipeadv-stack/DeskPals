using System.Drawing;
using System.Text;
using System.Text.Json;

namespace DeskPalsWeather
{
	/// <summary>What falls (Snowhush's numbers: snow 0, leaves 1, rain 2, blossom 3); None: nothing falls.</summary>
	public enum Kind { None = -1, Snow = 0, Leaves = 1, Rain = 2, Blossom = 3 }

	/// <summary>The settings of DeskPals Weather, kept in %LocalAppData%\DesktopPet\extensions\weather.json.</summary>
	public sealed class Config
	{
		public bool On { get; set; } = true;
		/// <summary>0 = what I choose; 1 = the city's weather; 2 = the weather and the seasons on dry days; 3 = inverted.</summary>
		public int Follow { get; set; } = 0;
		public string City { get; set; } = "";
		public double Lat { get; set; }
		public double Lon { get; set; }
		public Kind Kind { get; set; } = Kind.Snow;
		public int Intensity { get; set; } = 50;
		public bool OnWindows { get; set; } = true;
		public bool Melt { get; set; } = true;
		/// <summary>Where the rain is: 0 over the windows (its drops run down them), 1 behind them (only on the desktop and round them).</summary>
		public int RainPlace { get; set; } = 0;
		/// <summary>How big the flakes and leaves are: 0 small (Snowhush's own), 1 medium, 2 large.</summary>
		public int FlakeSize { get; set; } = 1;
		// effects of our own, 0..100 (0 = off); Random lets each drift slowly by itself
		public int Fog { get; set; } = 0;
		public int Sun { get; set; } = 0;
		public int Grass { get; set; } = 0;
		public int Wind { get; set; } = 30;
		public bool Random { get; set; }
		/// <summary>The colour of each effect ("snow", "leaves", "rain", "blossom", "fog", "sun", "grass") as 0xRRGGBB; absent = its own.</summary>
		public Dictionary<string, int> Colors { get; set; } = new();
		// Christmas lights round the windows
		public bool Lights { get; set; }
		/// <summary>The lights come on by themselves in December.</summary>
		public bool LightsDecember { get; set; } = true;
		/// <summary>The shape of the bulbs (LightsArt.BulbShape: 5 = as in the video).</summary>
		public int LightsShape { get; set; } = 5;
		/// <summary>How they light up (LightsMode: steady, twinkle, chase, changing).</summary>
		public int LightsMode { get; set; } = 2;
		/// <summary>Many colours (the wreath's order) or one colour for all.</summary>
		public bool LightsMulti { get; set; } = true;
		/// <summary>The colours of the 24 bulbs of the wreath, in their order along the wire (0xRRGGBB); empty: the video's.</summary>
		public List<int> LightsColors { get; set; } = new();
		public int LightsColor { get; set; } = 0xE23842;
		public int LightsBrightness { get; set; } = 90;
		public bool LightsRandom { get; set; } = true;
		/// <summary>How far apart the bulbs hang: 20, 25 or 30 points.</summary>
		public int LightsSpacing { get; set; } = 30;
		/// <summary>Every bulb turned its own way (random), or each shape in its own order.</summary>
		public bool LightsRandomAngles { get; set; } = true;
		/// <summary>How fast they blink, chase and change: 0 slow, 1 normal, 2 fast.</summary>
		public int LightsSpeed { get; set; } = 1;

		/// <summary>The lights are on today (turned on, or December and they come by themselves).</summary>
		public bool LightsToday(DateTime today) => Lights || (LightsDecember && today.Month == 12);

		/// <summary>Fog and sun over every window (false: behind the window in use, over the others and the desktop).</summary>
		public bool OnTop { get; set; }
		/// <summary>The effects show in screenshots and screen recordings (false: they are left out).</summary>
		public bool InScreenshots { get; set; }
		// sound
		public bool Sound { get; set; }
		public int Volume { get; set; } = 0;
		public bool QuietOnCalls { get; set; } = true;
		// pauses
		public List<string> PauseApps { get; set; } = new();
		/// <summary>What pauses for each program in front: 0 everything, 1 the effects only (the sound goes on), 2 the sound only.</summary>
		public Dictionary<string, int> PauseMode { get; set; } = new();
		public bool PauseFullscreen { get; set; } = true;
		/// <summary>0 nothing, 1 less, 2 pause.</summary>
		public int Battery { get; set; } = 1;
		// shortcuts (index in Hotkeys.Choices, -1 none)
		public int KeyToggle { get; set; } = 0;
		public int KeySettings { get; set; } = -1;
		// scenes of one's own (the built-in ones are in Scene.BuiltIn)
		public List<Scene> Scenes { get; set; } = new();
		/// <summary>The theme of the window: "" = as DeskPals, "auto" (as Windows), "light" or "dark".</summary>
		public string Theme { get; set; } = "";
		/// <summary>The accent colour (one of DeskPals' twelve): "" = as DeskPals.</summary>
		public string Accent { get; set; } = "";
		/// <summary>The language of the window: "" = as DeskPals (or Windows).</summary>
		public string Lang { get; set; } = "";
		/// <summary>Where DeskPals keeps its settings (given by DeskPals when it starts this program): its language and theme.</summary>
		public string DeskPalsConfig { get; set; } = "";

		public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "extensions");
		public static string DefaultFile => Path.Combine(Folder, "weather.json");
		static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

		/// <summary>Where this one is kept when saved (the tests keep theirs apart); not in the file itself.</summary>
		[System.Text.Json.Serialization.JsonIgnore] public string SavedAt { get; set; }

		public static Config Load(string file = null)
		{
			file ??= DefaultFile;
			try { if (File.Exists(file)) return JsonSerializer.Deserialize<Config>(File.ReadAllText(file)) ?? new Config(); }
			catch (Exception) { }
			return new Config();
		}

		public void Save(string file = null)
		{
			file ??= SavedAt ?? DefaultFile;
			try { Directory.CreateDirectory(Path.GetDirectoryName(file)); File.WriteAllText(file, JsonSerializer.Serialize(this, Pretty)); }
			catch (Exception) { }
		}

		/// <summary>The colour chosen for an effect, or null (its own).</summary>
		public Color? ColorOf(string effect) => Colors.TryGetValue(effect, out int c) ? Color.FromArgb(255, (c >> 16) & 255, (c >> 8) & 255, c & 255) : null;

		public void SetColor(string effect, Color? c)
		{
			if (c == null) Colors.Remove(effect);
			else Colors[effect] = (c.Value.R << 16) | (c.Value.G << 8) | c.Value.B;
		}

		public int PauseModeOf(string app) => PauseMode.TryGetValue(app, out int m) ? m : 0;
	}

	/// <summary>
	/// A scene: what falls and how much, fog, sun, grass, wind, the sound and the colours, under a name. Built-in ones,
	/// and one's own, saved, sent to a friend as a short code or a file, and brought back the same way.
	/// </summary>
	public sealed class Scene
	{
		public string Name { get; set; } = "";
		public Kind Kind { get; set; } = Kind.Snow;
		public int Intensity { get; set; } = 50;
		public bool OnWindows { get; set; } = true;
		public bool Melt { get; set; } = true;
		public int Fog { get; set; }
		public int Sun { get; set; }
		public int Grass { get; set; }
		public int Wind { get; set; } = 30;
		public bool Random { get; set; }
		public bool Sound { get; set; }
		public Dictionary<string, int> Colors { get; set; } = new();
		public bool Lights { get; set; }

		public const string CodePrefix = "DPW1:";
		public const string FileExtension = ".dpweather";

		public static Scene From(Config c, string name) => new()
		{
			Name = name, Kind = c.Kind, Intensity = c.Intensity, OnWindows = c.OnWindows, Melt = c.Melt, Fog = c.Fog, Sun = c.Sun, Grass = c.Grass,
			Wind = c.Wind, Random = c.Random, Sound = c.Sound, Lights = c.Lights, Colors = new Dictionary<string, int>(c.Colors),
		};

		/// <summary>Put it on: what one chooses (no longer the city's weather).</summary>
		public void ApplyTo(Config c)
		{
			c.Follow = 0; c.Kind = Kind; c.Intensity = Math.Clamp(Intensity, 0, 100); c.OnWindows = OnWindows; c.Melt = Melt;
			c.Fog = Math.Clamp(Fog, 0, 100); c.Sun = Math.Clamp(Sun, 0, 100); c.Grass = Math.Clamp(Grass, 0, 100); c.Wind = Math.Clamp(Wind, 0, 100);
			c.Random = Random; c.Sound = Sound; c.Lights = Lights; c.Colors = new Dictionary<string, int>(Colors ?? new());
		}

		public bool SameAs(Config c) => c.Follow == 0 && c.Kind == Kind && c.Intensity == Intensity && c.Fog == Fog && c.Sun == Sun && c.Grass == Grass && c.Wind == Wind
			&& c.Random == Random && c.Lights == Lights && c.Colors.Count == (Colors?.Count ?? 0) && c.Colors.All(kv => Colors.TryGetValue(kv.Key, out int v) && v == kv.Value);

		/// <summary>A short text to send to a friend ("DPW1:" and the scene, packed).</summary>
		public string ToCode() => CodePrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

		/// <summary>A scene from a code or from the text of a file; null when it is not one.</summary>
		public static Scene Parse(string text)
		{
			try
			{
				text = (text ?? "").Trim();
				string json = text;
				int at = text.IndexOf(CodePrefix, StringComparison.Ordinal);
				if (at >= 0)
				{
					string b = new string(text[(at + CodePrefix.Length)..].TakeWhile(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray()).Replace('-', '+').Replace('_', '/');
					b = b.PadRight(b.Length + (4 - b.Length % 4) % 4, '=');
					json = Encoding.UTF8.GetString(Convert.FromBase64String(b));
				}
				var s = JsonSerializer.Deserialize<Scene>(json);
				if (s == null || !Enum.IsDefined(s.Kind)) return null;
				s.Name = (s.Name ?? "").Trim();
				if (s.Name.Length > 60) s.Name = s.Name[..60];
				s.Colors ??= new();
				return s;
			}
			catch (Exception) { return null; }
		}

		/// <summary>The scenes that come with the program (their names are translated: "scene.winter"…).</summary>
		public static readonly (string key, Scene scene)[] BuiltIn =
		{
			("scene.winter", new Scene { Kind = Kind.Snow, Intensity = 55, Fog = 12, Wind = 30, Sound = true }),
			("scene.blizzard", new Scene { Kind = Kind.Snow, Intensity = 100, Fog = 22, Wind = 95, Sound = true }),
			("scene.blueNight", new Scene { Kind = Kind.Snow, Intensity = 40, Wind = 15, Colors = new() { ["snow"] = 0xA8C8FF } }),
			("scene.rainy", new Scene { Kind = Kind.Rain, Intensity = 70, Fog = 30, Wind = 45, Sound = true }),
			("scene.autumn", new Scene { Kind = Kind.Leaves, Intensity = 45, Sun = 20, Wind = 50, Grass = 25 }),
			("scene.spring", new Scene { Kind = Kind.Blossom, Intensity = 40, Sun = 35, Grass = 55, Wind = 20 }),
			("scene.foggy", new Scene { Kind = Kind.None, Fog = 75, Wind = 15 }),
			("scene.sunny", new Scene { Kind = Kind.None, Sun = 70, Grass = 45, Wind = 15 }),
			("scene.christmas", new Scene { Kind = Kind.Snow, Intensity = 45, Wind = 20, Sound = true, Lights = true }),
		};
	}
}
