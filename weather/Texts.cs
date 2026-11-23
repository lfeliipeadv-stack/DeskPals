using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

namespace DeskPalsWeather
{
	/// <summary>
	/// The texts of DeskPals Weather in the 23 languages of DeskPals (lang\*.txt inside the program, "key=value" per line;
	/// English fills what a language lacks). The language is the one chosen here, else the one of DeskPals, else Windows'.
	/// </summary>
	internal static class T
	{
		public static readonly (string code, string name)[] Languages =
		{
			("en", "English"), ("pt-BR", "Português (Brasil)"), ("es", "Español"), ("fr", "Français"), ("de", "Deutsch"), ("it", "Italiano"),
			("ru", "Русский"), ("tr", "Türkçe"), ("id", "Bahasa Indonesia"), ("vi", "Tiếng Việt"), ("sw", "Kiswahili"), ("ar", "العربية"),
			("fa", "فارسی"), ("ur", "اردو"), ("hi", "हिन्दी"), ("bn", "বাংলা"), ("mr", "मराठी"), ("ta", "தமிழ்"), ("te", "తెలుగు"),
			("th", "ไทย"), ("ja", "日本語"), ("ko", "한국어"), ("zh-CN", "中文 (简体)"),
		};

		static Dictionary<string, string> english, current;
		public static string Code { get; private set; } = "en";
		public static bool RightToLeft => Code is "ar" or "fa" or "ur";
		public static event Action Changed;

		public static string S(string key, params object[] args)
		{
			if (current == null) Use(Pick(Controller.Config));
			string s = current.TryGetValue(key, out var v) ? v : english.TryGetValue(key, out v) ? v : key;
			if (args.Length == 0) return s;
			try { return string.Format(s, args); } catch (FormatException) { return s; }
		}

		public static bool Has(string key) { if (current == null) Use(Pick(Controller.Config)); return english.ContainsKey(key); }

		/// <summary>Show the texts of this language now.</summary>
		public static void Use(string code)
		{
			english ??= Read("en");
			Code = Languages.Any(l => l.code == code) ? code : "en";
			current = Code == "en" ? english : Read(Code);
			Changed?.Invoke();
		}

		/// <summary>The language to use: chosen here, or DeskPals', or Windows' (the closest there is).</summary>
		public static string Pick(Config c)
		{
			if (!string.IsNullOrEmpty(c.Lang)) return Closest(c.Lang);
			string deskPals = DeskPals.Setting(c, "Language");
			if (!string.IsNullOrEmpty(deskPals) && deskPals != "auto") return Closest(deskPals);
			return Closest(CultureInfo.CurrentUICulture.Name);
		}

		public static string Closest(string code)
		{
			if (string.IsNullOrEmpty(code)) return "en";
			var exact = Languages.FirstOrDefault(l => l.code.Equals(code, StringComparison.OrdinalIgnoreCase)).code;
			if (exact != null) return exact;
			string two = code.Split('-')[0];
			return Languages.FirstOrDefault(l => l.code.Split('-')[0].Equals(two, StringComparison.OrdinalIgnoreCase)).code ?? "en";
		}

		/// <summary>The texts of one language, from the program itself.</summary>
		public static Dictionary<string, string> Read(string code)
		{
			var d = new Dictionary<string, string>();
			using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DeskPalsWeather.lang." + code + ".txt");
			if (s == null) return d;
			using var r = new StreamReader(s);
			string line;
			while ((line = r.ReadLine()) != null)
			{
				int eq = line.IndexOf('=');
				if (eq <= 0 || line.StartsWith('#')) continue;
				d[line[..eq].Trim()] = line[(eq + 1)..].Trim().Replace("\\n", "\n");
			}
			return d;
		}
	}

	/// <summary>What DeskPals itself has set (its language, theme and accent colour), read from its settings file.</summary>
	internal static class DeskPals
	{
		static readonly Dictionary<string, (DateTime at, Dictionary<string, string> values)> cache = new();

		public static string File(Config c)
		{
			if (!string.IsNullOrEmpty(c.DeskPalsConfig) && System.IO.File.Exists(c.DeskPalsConfig)) return c.DeskPalsConfig;
			string installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "DesktopPet.config");
			return System.IO.File.Exists(installed) ? installed : null;
		}

		public static string Setting(Config c, string name)
		{
			string file = File(c);
			if (file == null) return null;
			try
			{
				var at = System.IO.File.GetLastWriteTimeUtc(file);
				if (!cache.TryGetValue(file, out var e) || e.at != at)
				{
					var values = new Dictionary<string, string>();
					foreach (var add in XDocument.Load(file).Descendants("add"))
					{
						string k = (string)add.Attribute("key");
						if (k is "Language" or "Theme" or "Accent") values[k] = (string)add.Attribute("value");
					}
					cache[file] = e = (at, values);
				}
				return e.values.TryGetValue(name, out var v) ? v : null;
			}
			catch (Exception) { return null; }
		}

		/// <summary>The theme and accent colour of DeskPals on this window too.</summary>
		public static void ApplyTheme(Config c)
		{
			// the one chosen here, or else the one of DeskPals
			DesktopPet.Theme.Mode = !string.IsNullOrEmpty(c.Theme) ? c.Theme : Setting(c, "Theme") ?? "auto";
			DesktopPet.Theme.AccentId = !string.IsNullOrEmpty(c.Accent) ? c.Accent : Setting(c, "Accent") ?? "green";
		}
	}
}
