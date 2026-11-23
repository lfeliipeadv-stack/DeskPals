using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== plugins made by anyone

	/// <summary>
	/// Off unless turned on. Plugins anyone can write and share, as a small JSON file of rules: when something happens
	/// (at a time, every N minutes, on a date, when a program comes to the front, when the program starts, when a Pomodoro
	/// focus ends, a level is won, it snows or rains), the pet says something, plays animations, shows snow or confetti, or
	/// brings a note. A plugin is only data: it cannot run anything, open anything or read anything, so a plugin from
	/// anyone is safe. They are kept in the user's folder ("plugins"); the gallery (PluginGallery in this repository)
	/// lists the ones anyone can install with a click. The format is in docs/plugins.md.
	/// </summary>
	public static class CommunityPlugins
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("Community", false);
			set { Program.MyData.SetSetting("Community", value); Apply(); }
		}

		public static string Folder => Path.Combine(InstallPlace.DataDir, "plugins");

		/// <summary>The gallery: an index.json and the plugin files next to it.</summary>
		public static string GalleryUrl => "https://raw.githubusercontent.com/" + AppInfo.RepoOwner + "/" + AppInfo.RepoName + "/" + AppInfo.Branch + "/PluginGallery/";
		/// <summary>Developer and self-test: a local folder instead of the online gallery.</summary>
		internal static string GalleryDir;

		// ------------------------------------------------------------------ a plugin

		public sealed class Rule
		{
			public string When;          // "start", "at", "every", "date", "app", "event"
			public TimeSpan At;           // at
			public string Days = "all";   // at: "all", "weekdays", "weekend" or days ("mon,fri")
			public int Every;             // every (minutes)
			public TimeSpan From = TimeSpan.Zero, To = TimeSpan.FromHours(24);   // every: only between these hours
			public int Month, DayOfMonth; // date
			public string App;            // app
			public string Event;          // event: focusDone, levelUp, snow, rain
			public List<string> Say = new List<string>(), Note = new List<string>(), Play = new List<string>();
			public ScreenEffects.Kind? Effect;
			public int Chance = 100;
		}

		public sealed class Plugin
		{
			public string Id, Name, Description, Author, Version, Emoji = "🧩", File;
			public List<Rule> Rules = new List<Rule>();
		}

		static readonly Regex IdRx = new Regex("^[a-z0-9][a-z0-9-]{1,39}$");
		static readonly string[] Events = { "focusDone", "levelUp", "snow", "rain" };

		/// <summary>A text in the user's language: a string, or {"pt": …, "en": …} (English, then the first one, when the language is missing).</summary>
		static JsonNode Localized(JsonNode n)
		{
			if (n is JsonObject o)
			{
				string lang = Lang.Current, two = lang.Split('-')[0];
				return o[lang] ?? o[two] ?? o.FirstOrDefault(kv => kv.Key.StartsWith(two + "-", StringComparison.OrdinalIgnoreCase)).Value ?? o["en"] ?? o.FirstOrDefault().Value;
			}
			return n;
		}

		static List<string> Texts(JsonNode n, int max)
		{
			n = Localized(n);
			var list = new List<string>();
			if (n is JsonArray a) foreach (var x in a) { if (x is JsonValue v && v.TryGetValue(out string s)) list.Add(s); }
			else if (n is JsonValue v1 && v1.TryGetValue(out string one)) list.Add(one);
			return list.Select(s => s.Replace("\r", " ").Replace("\n", " ").Trim()).Where(s => s.Length > 0)
				.Select(s => s.Length > max ? s.Substring(0, max - 1) + "…" : s).Take(20).ToList();
		}

		static string Text(JsonNode n, int max) => Texts(n, max).FirstOrDefault() ?? "";

		static bool Time(string s, out TimeSpan t) => TimeSpan.TryParseExact((s ?? "").Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out t) && t < TimeSpan.FromHours(24);

		/// <summary>Read a plugin. Throws with a readable reason when the file is not a valid plugin.</summary>
		public static Plugin Parse(string json)
		{
			if (json == null || json.Length > 64 * 1024) throw new InvalidDataException("too big");
			var j = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("not an object");
			var p = new Plugin
			{
				Id = (string)j["id"] ?? "",
				Name = Text(j["name"], 40),
				Description = Text(j["description"], 200),
				Author = Text(j["author"], 40),
				Version = Text(j["version"], 12),
			};
			string emoji = Text(j["emoji"], 4);
			if (emoji.Length > 0) p.Emoji = emoji;
			if (!IdRx.IsMatch(p.Id)) throw new InvalidDataException("id");
			if (p.Name.Length == 0) throw new InvalidDataException("name");
			if (!(j["rules"] is JsonArray rules) || rules.Count == 0 || rules.Count > 50) throw new InvalidDataException("rules");
			foreach (var node in rules)
			{
				if (!(node is JsonObject r)) throw new InvalidDataException("rule");
				var rule = new Rule();
				var when = r["when"];
				if (when is JsonValue wv && wv.TryGetValue(out string ws) && ws == "start") rule.When = "start";
				else if (when is JsonObject w)
				{
					if (w["at"] != null)
					{
						rule.When = "at";
						if (!Time((string)w["at"], out rule.At)) throw new InvalidDataException("at");
						rule.Days = ((string)w["days"] ?? "all").ToLowerInvariant();
						rule.Days = rule.Days.Replace(" ", "");
						if (rule.Days != "all" && rule.Days != "weekdays" && rule.Days != "weekend" && !Regex.IsMatch(rule.Days, "^(mon|tue|wed|thu|fri|sat|sun)(,(mon|tue|wed|thu|fri|sat|sun))*$")) throw new InvalidDataException("days");
					}
					else if (w["every"] != null)
					{
						rule.When = "every";
						rule.Every = (int?)w["every"] ?? 0;
						if (rule.Every < 15 || rule.Every > 24 * 60) throw new InvalidDataException("every (15 to 1440 minutes)");
						string between = (string)w["between"];
						if (between != null)
						{
							var parts = between.Split('-');
							if (parts.Length != 2 || !Time(parts[0], out rule.From) || !Time(parts[1], out rule.To)) throw new InvalidDataException("between");
						}
					}
					else if (w["date"] != null)
					{
						rule.When = "date";
						var parts = ((string)w["date"] ?? "").Split('-');
						if (parts.Length != 2 || !int.TryParse(parts[0], out rule.Month) || !int.TryParse(parts[1], out rule.DayOfMonth)
							|| rule.Month < 1 || rule.Month > 12 || rule.DayOfMonth < 1 || rule.DayOfMonth > DateTime.DaysInMonth(2024, rule.Month)) throw new InvalidDataException("date (MM-DD)");
					}
					else if (w["app"] != null)
					{
						rule.When = "app";
						rule.App = WindowRules.Clean((string)w["app"]);
						if (rule.App.Length == 0 || rule.App.Length > 60) throw new InvalidDataException("app");
					}
					else if (w["event"] != null)
					{
						rule.When = "event";
						rule.Event = Events.FirstOrDefault(e => string.Equals(e, (string)w["event"], StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException("event");
					}
				}
				if (rule.When == null) throw new InvalidDataException("when");
				rule.Say = Texts(r["say"], 160);
				rule.Note = Texts(r["note"], 300);
				if (r["play"] is JsonArray play)
					rule.Play = play.Select(x => (x as JsonValue)?.TryGetValue(out string s) == true ? s.Trim() : "").Where(s => s.Length > 0 && s.Length <= 30).Take(10).ToList();
				string fx = (string)r["effect"];
				if (fx != null)
				{
					if (!Enum.TryParse(fx, true, out ScreenEffects.Kind k)) throw new InvalidDataException("effect");
					rule.Effect = k;
				}
				rule.Chance = Math.Max(1, Math.Min(100, (int?)r["chance"] ?? 100));
				if (rule.Say.Count == 0 && rule.Note.Count == 0 && rule.Play.Count == 0 && rule.Effect == null) throw new InvalidDataException("a rule does nothing");
				p.Rules.Add(rule);
			}
			return p;
		}

		// ------------------------------------------------------------------ the installed plugins

		static List<Plugin> installed;

		/// <summary>The plugins in the user's folder (a broken file is left out and noted in the log).</summary>
		public static List<Plugin> Installed
		{
			get
			{
				if (installed != null) return installed;
				installed = new List<Plugin>();
				try
				{
					if (Directory.Exists(Folder))
						foreach (var f in Directory.GetFiles(Folder, "*.json").OrderBy(x => x))
						{
							try { var p = Parse(File.ReadAllText(f)); p.File = f; if (!installed.Any(x => x.Id == p.Id)) installed.Add(p); }
							catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "plugin " + Path.GetFileName(f) + ": " + ex.Message); }
						}
				}
				catch (Exception skipped) { LogFile.Ignored(skipped); }
				return installed;
			}
		}

		public static void Reload() { installed = null; }

		static HashSet<string> Off => new HashSet<string>(Program.MyData.GetSetting("CommunityOff").Split('|', StringSplitOptions.RemoveEmptyEntries));

		public static bool IsOn(string id) => !Off.Contains(id);

		public static void SetOn(string id, bool on)
		{
			var off = Off;
			if (on) off.Remove(id); else off.Add(id);
			Program.MyData.SetSetting("CommunityOff", string.Join("|", off));
		}

		/// <summary>Save a plugin (after checking it) in the user's folder.</summary>
		public static Plugin Install(string json)
		{
			var p = Parse(json);
			Directory.CreateDirectory(Folder);
			File.WriteAllText(Path.Combine(Folder, p.Id + ".json"), json);
			Reload();
			SetOn(p.Id, true);
			return p;
		}

		public static void Remove(string id)
		{
			var p = Installed.FirstOrDefault(x => x.Id == id);
			if (p?.File != null) try { File.Delete(p.File); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			Reload();
		}

		// ------------------------------------------------------------------ the gallery

		public sealed class Entry { public string Id, File, Name, Description, Author, Emoji = "🧩", Version; }

		static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

		static async Task<string> Read(string name)
		{
			if (GalleryDir != null) return File.ReadAllText(Path.Combine(GalleryDir, name));
			return await http.GetStringAsync(GalleryUrl + name);
		}

		/// <summary>What the gallery offers.</summary>
		public static async Task<List<Entry>> Gallery()
		{
			var j = JsonNode.Parse(await Read("index.json"));
			var list = new List<Entry>();
			if (j?["plugins"] is JsonArray a)
				foreach (var n in a.OfType<JsonObject>())
				{
					var e = new Entry { Id = (string)n["id"] ?? "", File = (string)n["file"] ?? "", Name = Text(n["name"], 40), Description = Text(n["description"], 200), Author = Text(n["author"], 40), Version = Text(n["version"], 12) };
					string emoji = Text(n["emoji"], 4);
					if (emoji.Length > 0) e.Emoji = emoji;
					if (IdRx.IsMatch(e.Id) && Regex.IsMatch(e.File, @"^[a-z0-9-]+\.json$") && e.Name.Length > 0) list.Add(e);
				}
			return list;
		}

		/// <summary>Download a plugin of the gallery and install it.</summary>
		public static async Task<Plugin> InstallFromGallery(Entry e) => Install(await Read(e.File));

		// ------------------------------------------------------------------ the rules at work

		static Timer timer;
		static readonly Dictionary<string, DateTime> lastFired = new Dictionary<string, DateTime>();
		static DateTime lastAny = DateTime.MinValue, started = DateTime.Now;
		static string lastApp = "";
		/// <summary>Self-test: what was done last (the plugin id and what it did).</summary>
		internal static string LastDone;
		/// <summary>Self-test: the clock.</summary>
		internal static DateTime? TestNow;
		static DateTime Now => TestNow ?? DateTime.Now;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 30000 }; timer.Tick += (s, e) => Check(); }
			if (Enabled)
			{
				if (!timer.Enabled) { started = DateTime.Now; timer.Start(); }
				var t = new Timer { Interval = 20000 };   // "start" rules: a little after the pets are on the screen
				t.Tick += (s, e) => { t.Stop(); t.Dispose(); Fire(r => r.When == "start"); };
				t.Start();
			}
			else timer.Stop();
		}

		/// <summary>Something happened in the program ("focusDone", "levelUp", "snow", "rain").</summary>
		public static void Event(string name)
		{
			if (Enabled) Fire(r => r.When == "event" && r.Event == name, true);
		}

		/// <summary>Every 30 s: the rules of time and of programs.</summary>
		internal static void Check()
		{
			if (!Enabled) return;
			string app = "";
			try { app = DesktopWindows.ProgramOf(ForegroundWindow()); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			bool appChanged = app != lastApp;
			lastApp = app;
			var now = Now;
			Fire(r => Due(r, now, appChanged ? app : null));
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetForegroundWindow")] static extern IntPtr ForegroundWindow();

		static bool Due(Rule r, DateTime now, string newApp)
		{
			switch (r.When)
			{
				case "at":
					bool weekend = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
					if ((r.Days == "weekdays" && weekend) || (r.Days == "weekend" && !weekend)) return false;
					if (r.Days != "all" && r.Days != "weekdays" && r.Days != "weekend" && !r.Days.Split(',').Contains(now.DayOfWeek.ToString().Substring(0, 3).ToLowerInvariant())) return false;
					return now.TimeOfDay >= r.At && now.TimeOfDay < r.At.Add(TimeSpan.FromMinutes(30));
				case "every": return now.TimeOfDay >= r.From && now.TimeOfDay < r.To;
				case "date": return now.Month == r.Month && now.Day == r.DayOfMonth && now.Hour >= 9;
				case "app": return newApp != null && newApp == r.App;
				default: return false;
			}
		}

		/// <summary>Do the rules that match (each one at its own pace; at most one plugin every 20 s, not during quiet time unless the event asked).</summary>
		static void Fire(Func<Rule, bool> match, bool fromEvent = false)
		{
			if (!Enabled || (!fromEvent && SpeechBubble.QuietNow)) return;
			var now = Now;
			var rnd = new Random();
			foreach (var p in Installed.Where(p => IsOn(p.Id)))
				for (int i = 0; i < p.Rules.Count; i++)
				{
					var r = p.Rules[i];
					if (!match(r)) continue;
					string key = p.Id + "#" + i;
					lastFired.TryGetValue(key, out var last);
					// each kind of rule at its own pace
					if (r.When == "at" || r.When == "date")
					{
						string day = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
						if (Program.MyData.GetSetting("Cp." + key) == day) continue;
						Program.MyData.SetSetting("Cp." + key, day);
					}
					else if (r.When == "every") { if (last == default) { lastFired[key] = now; continue; } if ((now - last).TotalMinutes < r.Every) continue; }
					else if (r.When == "app" && (now - last).TotalMinutes < 30) continue;
					lastFired[key] = now;
					if (rnd.Next(100) >= r.Chance) continue;
					if ((now - lastAny).TotalSeconds < 20 && !fromEvent) continue;
					lastAny = now;
					Do(p, r, rnd);
					return;
				}
		}

		static void Do(Plugin p, Rule r, Random rnd)
		{
			var done = new List<string>();
			if (r.Play.Count > 0) { Program.Mainthread?.ReactAll(r.Play.ToArray()); done.Add("play"); }
			if (r.Say.Count > 0)
			{
				string text = r.Say[rnd.Next(r.Say.Count)];
				if (AgentBridge.IsSafeToSay(text)) { SpeechBubble.Notify(UserProfile.Greet(text), 10); done.Add("say"); }
			}
			if (r.Note.Count > 0)
			{
				string text = r.Note[rnd.Next(r.Note.Count)];
				if (AgentBridge.IsSafeToSay(text)) { NoteDelivery.Bring(p.Emoji, p.Name, text); done.Add("note"); }
			}
			if (r.Effect != null) { ScreenEffects.Start(r.Effect.Value, 20, true); done.Add("effect"); }
			LastDone = p.Id + ":" + string.Join(",", done);
		}
	}
}
