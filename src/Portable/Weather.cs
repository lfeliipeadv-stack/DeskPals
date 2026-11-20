using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// Weather plugin: the pet says the weather of the day in the morning and warns before rain ("take the umbrella!").
	/// Uses Open-Meteo (free, no key, no account): only the chosen city (its coordinates) leaves this computer.
	/// </summary>
	public static class Weather
	{
		public sealed class Place { public string Name = ""; public double Lat, Lon; }
		public sealed class Today
		{
			public int Code, Max, Min, RainChance;
			public double Now;
			/// <summary>Highest chance of rain in the next 2 hours.</summary>
			public int RainSoon;
		}

		static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
		static Timer timer;
		static Today last;
		static DateTime lastLoad = DateTime.MinValue;

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("WeatherOn", false) && City != null;
			set { Program.MyData.SetSetting("WeatherOn", value); Apply(); }
		}

		/// <summary>The chosen city (null if none yet).</summary>
		public static Place City
		{
			get
			{
				string s = Program.MyData.GetSetting("WeatherCity");
				var p = s.Split('|');
				if (p.Length != 3 || !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat)
					|| !double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon)) return null;
				return new Place { Name = p[0], Lat = lat, Lon = lon };
			}
			set
			{
				Program.MyData.SetSetting("WeatherCity", value == null ? "" : value.Name.Replace("|", " ") + "|"
					+ value.Lat.ToString(CultureInfo.InvariantCulture) + "|" + value.Lon.ToString(CultureInfo.InvariantCulture));
				last = null;
				forecast = null;
			}
		}

		/// <summary>Warn when rain is coming (default on).</summary>
		public static bool RainAlerts
		{
			get => Program.MyData.GetSettingBool("WeatherRain", true);
			set => Program.MyData.SetSetting("WeatherRain", value);
		}

		/// <summary>The weather of the day in the morning (default on).</summary>
		public static bool Morning
		{
			get => Program.MyData.GetSettingBool("WeatherMorning", true);
			set => Program.MyData.SetSetting("WeatherMorning", value);
		}

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 10 * 60 * 1000 }; timer.Tick += async (s, e) => await Tick(); }
			if (Enabled)
			{
				timer.Start();
				var soon = new Timer { Interval = 20000 };   // a moment after the start
				soon.Tick += async (s, e) => { soon.Stop(); soon.Dispose(); await Tick(); };
				soon.Start();
			}
			else timer.Stop();
		}

		// ------------------------------------------------------------------ Open-Meteo

		/// <summary>Find a city by name (the first match, in the language in use).</summary>
		public static async Task<Place> FindCity(string name) => (await FindCities(name, 1)).FirstOrDefault();

		/// <summary>The cities with this name ("Curitiba, Paraná, BR"), the biggest first; for suggestions while typing.</summary>
		public static async Task<List<Place>> FindCities(string name, int count = 5)
		{
			string lang = Lang.Current.Split('-')[0];
			string url = "https://geocoding-api.open-meteo.com/v1/search?count=" + count + "&format=json&language=" + Uri.EscapeDataString(lang) + "&name=" + Uri.EscapeDataString(name.Trim());
			var json = JsonNode.Parse(await http.GetStringAsync(url));
			var list = new List<Place>();
			foreach (var r in json?["results"]?.AsArray() ?? new JsonArray())
			{
				if (r == null) continue;
				string label = string.Join(", ", new[] { (string)r["name"], (string)r["admin1"], (string)r["country_code"] }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
				var place = new Place { Name = label, Lat = (double)r["latitude"], Lon = (double)r["longitude"] };
				if (!list.Any(p => p.Name == place.Name)) list.Add(place);
			}
			return list;
		}

		/// <summary>Today's weather at a place.</summary>
		public static async Task<Today> Load(Place p)
		{
			string url = "https://api.open-meteo.com/v1/forecast?timezone=auto&forecast_days=1"
				+ "&latitude=" + p.Lat.ToString(CultureInfo.InvariantCulture) + "&longitude=" + p.Lon.ToString(CultureInfo.InvariantCulture)
				+ "&current=temperature_2m,weather_code&hourly=precipitation_probability"
				+ "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max";
			var j = JsonNode.Parse(await http.GetStringAsync(url));
			var t = new Today
			{
				Now = (double?)j?["current"]?["temperature_2m"] ?? 0,
				Code = (int?)j?["daily"]?["weather_code"]?[0] ?? (int?)j?["current"]?["weather_code"] ?? 0,
				Max = (int)Math.Round((double?)j?["daily"]?["temperature_2m_max"]?[0] ?? 0),
				Min = (int)Math.Round((double?)j?["daily"]?["temperature_2m_min"]?[0] ?? 0),
				RainChance = (int?)j?["daily"]?["precipitation_probability_max"]?[0] ?? 0,
			};
			// the next 2 hours, from the hourly list (local time of the place)
			var times = j?["hourly"]?["time"]?.AsArray();
			var probs = j?["hourly"]?["precipitation_probability"]?.AsArray();
			string nowText = (string)j?["current"]?["time"];
			if (times != null && probs != null && DateTime.TryParse(nowText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var placeNow))
				for (int i = 0; i < times.Count && i < probs.Count; i++)
					if (DateTime.TryParse((string)times[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var h)
						&& h > placeNow.AddHours(-1) && h <= placeNow.AddHours(2))
						t.RainSoon = Math.Max(t.RainSoon, (int?)probs[i] ?? 0);
			return t;
		}

		/// <summary>The weather now (cached for 10 minutes), or null without a city or internet.</summary>
		public static async Task<Today> Current()
		{
			var city = City;
			if (city == null) return null;
			if (last != null && (DateTime.Now - lastLoad).TotalMinutes < 10) return last;
			try { last = await Load(city); lastLoad = DateTime.Now; }
			catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "weather: " + ex.Message); }
			return last;
		}

		/// <summary>The last weather read (for the Home page), without going to the internet.</summary>
		public static Today Cached => last;

		// ------------------------------------------------------------------ the full forecast (Home page)

		/// <summary>The weather now, hour by hour and day by day (times are the local time of the place).</summary>
		public sealed class Forecast
		{
			public DateTime Now;                 // local time of the place when it was read
			public double Temp, Feels, Wind;
			public int Humidity, Code, RainNow;
			public bool IsDay = true;
			public readonly List<(DateTime time, double temp, int rain, double wind, int code)> Hours = new List<(DateTime, double, int, double, int)>();
			public readonly List<(DateTime date, int code, int max, int min, int rain)> Days = new List<(DateTime, int, int, int, int)>();
		}

		static Forecast forecast;
		static DateTime forecastLoad = DateTime.MinValue;
		static bool forecastLoading;

		/// <summary>The last full forecast read (null until the first one arrives).</summary>
		public static Forecast ForecastCached => forecast != null && City != null ? forecast : null;

		/// <summary>True when the forecast is older than 30 minutes (or missing): time to ask again.</summary>
		public static bool ForecastStale => forecast == null || (DateTime.Now - forecastLoad).TotalMinutes >= 30;

		/// <summary>Read the full forecast again when it is stale; <paramref name="done"/> runs when a new one arrived.</summary>
		public static async void RefreshForecast(Action done = null)
		{
			var city = City;
			if (city == null || forecastLoading || !ForecastStale) return;
			forecastLoading = true;
			try
			{
				forecast = await LoadForecast(city);
				forecastLoad = DateTime.Now;
				done?.Invoke();
			}
			catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "forecast: " + ex.Message); forecastLoad = DateTime.Now.AddMinutes(-25); }   // try again in 5 min
			finally { forecastLoading = false; }
		}

		/// <summary>Now, 8 days of hours and 8 days of highs and lows from Open-Meteo (the same free service).</summary>
		public static async Task<Forecast> LoadForecast(Place p)
		{
			string url = "https://api.open-meteo.com/v1/forecast?timezone=auto&forecast_days=8&wind_speed_unit=kmh"
				+ "&latitude=" + p.Lat.ToString(CultureInfo.InvariantCulture) + "&longitude=" + p.Lon.ToString(CultureInfo.InvariantCulture)
				+ "&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code,is_day"
				+ "&hourly=temperature_2m,precipitation_probability,wind_speed_10m,weather_code"
				+ "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max";
			var j = JsonNode.Parse(await http.GetStringAsync(url));
			var c = j?["current"];
			var f = new Forecast
			{
				Temp = (double?)c?["temperature_2m"] ?? 0,
				Feels = (double?)c?["apparent_temperature"] ?? 0,
				Humidity = (int?)c?["relative_humidity_2m"] ?? 0,
				Wind = (double?)c?["wind_speed_10m"] ?? 0,
				Code = (int?)c?["weather_code"] ?? 0,
				IsDay = ((int?)c?["is_day"] ?? 1) == 1,
			};
			DateTime.TryParse((string)c?["time"], CultureInfo.InvariantCulture, DateTimeStyles.None, out f.Now);
			var h = j?["hourly"];
			var times = h?["time"]?.AsArray();
			if (times != null)
				for (int i = 0; i < times.Count; i++)
					if (DateTime.TryParse((string)times[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
						f.Hours.Add((t, (double?)h["temperature_2m"]?[i] ?? 0, (int?)h["precipitation_probability"]?[i] ?? 0,
							(double?)h["wind_speed_10m"]?[i] ?? 0, (int?)h["weather_code"]?[i] ?? 0));
			var d = j?["daily"];
			var dates = d?["time"]?.AsArray();
			if (dates != null)
				for (int i = 0; i < dates.Count; i++)
					if (DateTime.TryParse((string)dates[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
						f.Days.Add((day, (int?)d["weather_code"]?[i] ?? 0, (int)Math.Round((double?)d["temperature_2m_max"]?[i] ?? 0),
							(int)Math.Round((double?)d["temperature_2m_min"]?[i] ?? 0), (int?)d["precipitation_probability_max"]?[i] ?? 0));
			// chance of rain now: the hour we are in
			var hour = f.Hours.FirstOrDefault(x => x.time <= f.Now && x.time.AddHours(1) > f.Now);
			f.RainNow = hour.time == default ? 0 : hour.rain;
			return f;
		}

		/// <summary>The full forecast, read again when it is older than 30 minutes (null without a city or internet).</summary>
		public static async Task<Forecast> CurrentForecast()
		{
			var city = City;
			if (city == null) return null;
			if (!ForecastStale) return forecast;
			try { forecast = await LoadForecast(city); forecastLoad = DateTime.Now; }
			catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "forecast: " + ex.Message); }
			return forecast;
		}

		static bool Rainy(int code) => (code >= 51 && code <= 67) || (code >= 80 && code <= 82) || code >= 95;

		/// <summary>
		/// The forecast in one or two sentences: when the rain should start (or that it rains now), a storm, and whether
		/// tomorrow gets cooler or warmer. Empty when there is nothing worth saying.
		/// </summary>
		public static string Outlook(Forecast f)
		{
			if (f == null || f.Hours.Count == 0) return "";
			var parts = new List<string>();
			var next = f.Hours.Where(h => h.time > f.Now.AddHours(-1) && h.time <= f.Now.AddHours(24)).ToList();
			var storm = next.FirstOrDefault(h => h.code >= 95 && h.time <= f.Now.AddHours(12));
			var rain = next.FirstOrDefault(h => h.rain >= 50);
			if (storm.time != default) parts.Add(Lang.T("weather.outlookStorm", storm.time.ToString("HH:mm")));
			else if (Rainy(f.Code) && f.RainNow >= 30) parts.Add(Lang.T("weather.outlookRainNow", f.RainNow));
			else if (rain.time != default) parts.Add(Lang.T("weather.outlookRain", rain.time.ToString("HH:mm"), rain.rain));
			if (f.Days.Count > 1)
			{
				var today = f.Days[0]; var tomorrow = f.Days[1];
				if (tomorrow.min <= today.min - 3) parts.Add(Lang.T("weather.outlookCooler", tomorrow.min));
				else if (tomorrow.max >= today.max + 3) parts.Add(Lang.T("weather.outlookWarmer", tomorrow.max));
			}
			return string.Join(" ", parts);
		}

		/// <summary>
		/// The pet shows the weather: an umbrella or rain cloud while it rains, heat or cold when it is hot or cold
		/// (only pets that have such an animation, at most once every 2 hours for each, not during quiet time).
		/// </summary>
		static void ReactToWeather(Forecast f)
		{
			if (f == null || SpeechBubble.QuietNow || DateTime.Now.Hour < 7 || DateTime.Now.Hour >= 22) return;
			string kind = Rainy(f.Code) ? "rain" : f.Temp >= 32 ? "hot" : f.Temp <= 12 ? "cold" : null;
			if (kind == null) return;
			string key = "WeatherReact." + kind;
			if (DateTime.TryParse(Program.MyData.GetSetting(key), CultureInfo.InvariantCulture, DateTimeStyles.None, out var said) && (DateTime.Now - said).TotalHours < 2) return;
			Program.MyData.SetSetting(key, DateTime.Now.ToString("s", CultureInfo.InvariantCulture));
			switch (kind)
			{
				case "rain": Program.Mainthread?.ReactAll("umbrella", "rain cloud", "rain", "puddle splash"); break;
				case "hot": Program.Mainthread?.ReactAll("hot", "hand fan", "sunbathing", "ice cream"); break;
				default: Program.Mainthread?.ReactAll("cold", "shiver", "scarf", "snow cap"); break;
			}
		}

		/// <summary>The emoji of a weather code, with the moon at night for a clear or partly cloudy sky.</summary>
		public static string Emoji(int code, bool day = true)
		{
			if (!day && code == 0) return "🌙";
			if (!day && code <= 2) return "☁️";
			return Describe(code).emoji;
		}

		// ------------------------------------------------------------------ what the pet says

		/// <summary>WMO weather code → text key and emoji.</summary>
		public static (string key, string emoji) Describe(int code)
		{
			if (code == 0) return ("weather.clear", "☀️");
			if (code <= 2) return ("weather.partly", "⛅");
			if (code == 3) return ("weather.cloudy", "☁️");
			if (code == 45 || code == 48) return ("weather.fog", "🌫️");
			if (code >= 51 && code <= 57) return ("weather.drizzle", "🌦️");
			if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) return ("weather.rain", "🌧️");
			if ((code >= 71 && code <= 77) || code == 85 || code == 86) return ("weather.snow", "❄️");
			if (code >= 95) return ("weather.storm", "⛈️");
			return ("weather.cloudy", "☁️");
		}

		/// <summary>"Today in São Paulo: rain 🌧️, 18° to 26°." plus a tip (umbrella, heat, cold).</summary>
		public static string Summary(Today t, Place p)
		{
			var (key, emoji) = Describe(t.Code);
			string s = Lang.T("weather.today", p.Name.Split(',')[0], Lang.T(key), emoji, t.Min, t.Max);
			if (t.RainChance >= 60) s += " " + Lang.T("weather.tipUmbrella", t.RainChance);
			else if (t.Max >= 32) s += " " + Lang.T("weather.tipHot");
			else if (t.Min <= 10) s += " " + Lang.T("weather.tipCold");
			return s;
		}

		/// <summary>Morning summary once a day; a rain warning at most every 3 hours.</summary>
		static async Task Tick()
		{
			if (!Enabled) return;
			var city = City;
			var t = await Current();
			if (t == null) return;
			var now = DateTime.Now;
			string today = now.ToString("yyyy-MM-dd");
			var f = await CurrentForecast();
			if (Morning && now.Hour >= 6 && now.Hour < 12 && Program.MyData.GetSetting("WeatherSaid") != today)
			{
				Program.MyData.SetSetting("WeatherSaid", today);
				Program.Mainthread?.ReactAll("wave", "hello", "happy");
				// the summary of the day, then what is coming (rain at what time, a storm, tomorrow cooler or warmer)
				string outlook = Outlook(f);
				SpeechBubble.Notify(UserProfile.Greet(Lang.T("weather.morning")) + " " + Summary(t, city) + (outlook.Length > 0 ? " " + outlook : ""), 14);
				return;
			}
			// a thunderstorm in the next 3 hours: its own warning (at most every 3 hours)
			var storm = f?.Hours.FirstOrDefault(h => h.code >= 95 && h.time > f.Now.AddHours(-1) && h.time <= f.Now.AddHours(3)) ?? default;
			if (RainAlerts && storm.time != default && now.Hour >= 6 && now.Hour < 23
				&& (!DateTime.TryParse(Program.MyData.GetSetting("WeatherStormSaid"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var stormSaid) || (now - stormSaid).TotalHours >= 3))
			{
				Program.MyData.SetSetting("WeatherStormSaid", now.ToString("s", CultureInfo.InvariantCulture));
				Program.MyData.SetSetting("WeatherRainSaid", now.ToString("s", CultureInfo.InvariantCulture));   // no rain warning on top of it
				Program.Mainthread?.ReactAll("surprised", "scared", "look up");
				SpeechBubble.Notify(Lang.T("weather.stormSoon", city.Name.Split(',')[0], storm.time.ToString("HH:mm")), 14);
				return;
			}
			ReactToWeather(f);
			if (RainAlerts && t.RainSoon >= 70 && now.Hour >= 6 && now.Hour < 23
				&& (!DateTime.TryParse(Program.MyData.GetSetting("WeatherRainSaid"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var said) || (now - said).TotalHours >= 3))
			{
				Program.MyData.SetSetting("WeatherRainSaid", now.ToString("s", CultureInfo.InvariantCulture));
				Program.Mainthread?.ReactAll("surprised", "look up", "think");
				SpeechBubble.Notify(Lang.T("weather.rainSoon", city.Name.Split(',')[0], t.RainSoon), 12);
			}
		}

		/// <summary>Pet menu "How is the weather?": say it now.</summary>
		public static async void SayNow()
		{
			var city = City;
			if (city == null) { SpeechBubble.Say(Lang.T("weather.noCity"), 8, new SpeechBubble.Choice(Lang.T("weather.choose"), () => FormSettings.ShowPage(FormSettings.Page.Plugins), true)); return; }
			var t = await Current();
			if (t == null) { SpeechBubble.Say(Lang.T("weather.offline"), 6); return; }
			Program.Mainthread?.ReactAll("look up", "think", "happy");
			SpeechBubble.Say(Summary(t, city) + " " + Lang.T("weather.nowTemp", Math.Round(t.Now)), 10);
		}
	}
}

namespace DesktopPet
{
	/// <summary>On/off of the plugins that have no switch of their own (reminders, focus, games, shortcuts, notes): off by default.</summary>
	public static class PluginSwitch
	{
		public static bool On(string id) => Program.MyData.GetSettingBool("Plug." + id, false);

		/// <summary>Settings of every plugin that used to be on by default (since 2.6.2 they all start off).</summary>
		internal static readonly string[] WereOnByDefault =
		{
			"Plug.reminders", "Plug.focus", "Plug.games", "Plug.shortcuts", "Plug.notes",
			"ActivityCare", "PetsMeet", "PcEvents", "AutoHide", "HiddenToasts", "MusicGames", "Visitors",
		};

		/// <summary>
		/// Once, at the start: on a new installation every plugin starts off. On a computer that already used an older
		/// version, the plugins that were on (because they were on by default and never turned off) stay on.
		/// </summary>
		public static void KeepOldDefaults()
		{
			if (Program.MyData.GetSetting("PluginDefaults").Length > 0) return;
			if (Program.MyData.GetSetting("LastVersion").Length > 0)
				foreach (var key in WereOnByDefault)
					if (Program.MyData.GetSetting(key).Length == 0) Program.MyData.SetSetting(key, true);
			Program.MyData.SetSetting("PluginDefaults", "off");
		}

		public static void Set(string id, bool on)
		{
			Program.MyData.SetSetting("Plug." + id, on);
			if (id == "focus" && !on && Pomodoro.Current != Pomodoro.Phase.Off) Pomodoro.Stop();
			ContextMenus.UpdateTexts();
		}
	}
}
