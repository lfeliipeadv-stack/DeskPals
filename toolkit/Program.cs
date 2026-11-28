using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

namespace DeskPalsTools
{
	/// <summary>
	/// DeskPals Tools. Without arguments (or with files) it opens its window. DeskPals calls it with:
	/// "--list [files]" → the jobs for those files, as JSON on the output;
	/// "--do=&lt;job&gt; [--result=&lt;file&gt;] files" → does it with a small progress window and writes the result (JSON);
	/// "--config &lt;file&gt;" where DeskPals keeps its settings (its language and theme are used here), "--lang=pt-BR";
	/// "--devtests[=report]" runs its tests and quits (the exit code is the number of failures).
	/// </summary>
	internal static class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			Timing.Mark("main");
			// whatever way the program ends, the graphics are given back first
			AppDomain.CurrentDomain.ProcessExit += (s, e) => Ending.Now();
			int at = Array.IndexOf(args, "--config");
			Settings.DeskPalsConfig = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
			var files = args.Where((a, i) => !a.StartsWith("--") && !(at >= 0 && i == at + 1)).Select(a => a.Trim('"')).Where(a => File.Exists(a) || Directory.Exists(a)).Select(Path.GetFullPath).ToList();
			string lang = args.FirstOrDefault(a => a.StartsWith("--lang="))?[7..];
			T.Use(lang ?? T.Pick());
			Timing.Mark("texts");
			// the pet asks the list often: it needs no window, theme nor fonts, so it is answered first (quicker)
			if (args.Contains("--list"))
			{
				var plan = Jobs.Plan(files);
				Timing.Mark("plan");
				// written by hand (the serializer takes long to start, and this is all there is)
				using (var output = Console.OpenStandardOutput())
				using (var w = new Utf8JsonWriter(output))
				{
					w.WriteStartArray();
					foreach (var j in plan)
					{
						w.WriteStartObject();
						w.WriteString("id", j.Id); w.WriteString("text", j.Text); w.WriteString("emoji", j.Emoji); w.WriteBoolean("windowOnly", j.WindowOnly);
						w.WriteEndObject();
					}
					w.WriteEndArray();
				}
				Timing.Mark("json");
				Timing.Write();
				return 0;
			}
			ApplicationConfiguration.Initialize();
			// the PDFs written with text (page numbers, watermarks, the hidden text of the OCR) use the fonts of Windows
			PdfSharp.Fonts.GlobalFontSettings.FontResolver ??= new WindowsFonts();
			DesktopPet.Theme.Mode = Settings.DeskPals("Theme") ?? "auto";
			DesktopPet.Theme.AccentId = Settings.DeskPals("Accent") ?? "green";
			Timing.Mark("theme");

			// developer: real use on the main thread (window, viewer, a PDF made), to see the program end
			if (args.Contains("--scenario")) return Scenario.Run(files);

			string devtests = args.FirstOrDefault(a => a.StartsWith("--devtests"));
			if (devtests != null) return DevTests.Run(devtests.Contains('=') ? devtests[(devtests.IndexOf('=') + 1)..].Trim('"') : null, args.FirstOrDefault(a => a.StartsWith("--only="))?[7..]);

			// started from Windows Explorer's right-click menu (one copy per selected file): gather them, then one job
			if (args.Contains("--from-explorer")) return ExplorerHook.Run(args, files);
			
			// "Read text on the screen" (its key, or DeskPals): the screen, a box, the text to the clipboard
			if (args.Contains("--snip")) { Application.Run(new SnipForm()); Ending.Now(); return 0; }


			string job = args.FirstOrDefault(a => a.StartsWith("--do="))?[5..];
			if (job != null)
			{
				string resultFile = args.FirstOrDefault(a => a.StartsWith("--result="))?[9..].Trim('"');
				// answers given beforehand: "--opt:pages=1-3" (then the job does not ask)
				var given = args.Where(a => a.StartsWith("--opt:") && a.Contains('=')).ToDictionary(a => a[6..a.IndexOf('=')], a => a[(a.IndexOf('=') + 1)..]);
				// "--quiet": no window at all (DeskPals' watched folders); a job that must ask fails instead
				if (args.Contains("--quiet"))
				{
					var def = Jobs.Find(job);
					Jobs.Result q;
					if (def?.Ask != null && given.Count == 0) q = new Jobs.Result { ok = false, message = T.S("err.needsAnswers") };
					else q = Task.Run(() => Jobs.Run(job, files, null, default, given)).GetAwaiter().GetResult();
					if (resultFile != null) File.WriteAllText(resultFile, q.Json());
					return q.ok ? 0 : 1;
				}
				var form = new ProgressForm(job, files, given.Count > 0 ? given : null);
				Application.Run(form);
				Ending.Now();
				var r = form.Result ?? new Jobs.Result { ok = false, message = T.S("err.canceled") };
				if (resultFile != null) File.WriteAllText(resultFile, r.Json());
				return r.ok ? 0 : 1;
			}

			// the right-click menu follows the program when DeskPals updated or moved it (and speaks DeskPals' language)
			ExplorerMenu.Refresh();
			Shortcuts.Refresh();
			Application.Run(new MainForm(files));
			Ending.Now();
			return 0;
		}
	}

	/// <summary>What is read from DeskPals' settings file (its language, theme and accent colour).</summary>
	internal static class Settings
	{
		public static string DeskPalsConfig;

		static string File()
		{
			if (!string.IsNullOrEmpty(DeskPalsConfig) && System.IO.File.Exists(DeskPalsConfig)) return DeskPalsConfig;
			string installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "DesktopPet.config");
			return System.IO.File.Exists(installed) ? installed : null;
		}

		static XDocument doc;
		static string docFile;
		static DateTime docWhen;

		/// <summary>DeskPals' settings, read once (again only when the file changed).</summary>
		static XDocument Doc(string f)
		{
			var when = System.IO.File.GetLastWriteTimeUtc(f);
			if (doc == null || docFile != f || docWhen != when) { doc = XDocument.Load(f); docFile = f; docWhen = when; }
			return doc;
		}

		public static string DeskPals(string name)
		{
			try
			{
				string f = File();
				if (f == null) return null;
				var add = Doc(f)?.Descendants("add").FirstOrDefault(a => (string)a.Attribute("key") == name);
				string v = (string)add?.Attribute("value");
				return string.IsNullOrEmpty(v) ? null : v;
			}
			catch (Exception) { return null; }
		}
	}

	/// <summary>
	/// The texts in the languages of DeskPals (lang\*.txt inside the program, "key=value" per line; English fills what a
	/// language lacks). The language is DeskPals', else Windows'.
	/// </summary>
	internal static class T
	{
		static Dictionary<string, string> english, current;
		public static string Code { get; private set; } = "en";

		public static string S(string key, params object[] args)
		{
			if (current == null) Use(Pick());
			string s = current.TryGetValue(key, out var v) ? v : english.TryGetValue(key, out v) ? v : key;
			if (args.Length == 0) return s;
			try { return string.Format(CultureInfo.CurrentCulture, s, args); } catch (FormatException) { return s; }
		}

		public static void Use(string code)
		{
			english ??= Read("en");
			var have = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			var codes = Codes().ToList();
			Code = codes.Contains(code) ? code : codes.Contains(code.Split('-')[0]) ? code.Split('-')[0] : "en";
			current = Code == "en" ? english : Read(Code);
		}

		public static string Pick()
		{
			string deskPals = Settings.DeskPals("Language");
			string code = !string.IsNullOrEmpty(deskPals) && deskPals != "auto" ? deskPals : CultureInfo.CurrentUICulture.Name;
			return code.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt-BR" : code;
		}

		/// <summary>
		/// Every key of a language (the tests compare them with English): lang\&lt;code&gt;.txt and each feature's own
		/// lang\&lt;code&gt;.&lt;feature&gt;.txt.
		/// </summary>
		public static Dictionary<string, string> Read(string code)
		{
			var d = new Dictionary<string, string>(StringComparer.Ordinal);
			var asm = Assembly.GetExecutingAssembly();
			foreach (var name in asm.GetManifestResourceNames().Where(n => n == "DeskPalsTools.lang." + code + ".txt" || (n.StartsWith("DeskPalsTools.lang." + code + ".") && n.EndsWith(".txt"))).OrderBy(n => n.Length))
			{
				using var s = asm.GetManifestResourceStream(name);
				using var r = new StreamReader(s);
				string line;
				while ((line = r.ReadLine()) != null)
				{
					if (line.Length == 0 || line[0] == '#') continue;
					int eq = line.IndexOf('=');
					if (eq > 0) d[line[..eq].Trim()] = line[(eq + 1)..].Replace("\\n", "\n");
				}
			}
			return d;
		}

		/// <summary>The languages there are ("en", "pt-BR"…), from the files' names (the part before a feature's name).</summary>
		public static IEnumerable<string> Codes() => Assembly.GetExecutingAssembly().GetManifestResourceNames()
			.Where(n => n.StartsWith("DeskPalsTools.lang.") && n.EndsWith(".txt")).Select(n => n["DeskPalsTools.lang.".Length..^4].Split('.')[0]).Distinct();
	}
}

namespace DeskPalsTools
{
	/// <summary>Developer: with DESKPALS_TIMING=1, how long each part of the start took (written to the error output).</summary>
	internal static class Timing
	{
		static readonly bool on = Environment.GetEnvironmentVariable("DESKPALS_TIMING") == "1";
		static readonly List<string> marks = new();
		public static void Mark(string what)
		{
			if (!on) return;
			var since = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
			marks.Add(what + " " + (int)since.TotalMilliseconds + " ms");
		}
		public static void Write() { if (on) Console.Error.WriteLine(string.Join(" · ", marks)); }
	}
}
