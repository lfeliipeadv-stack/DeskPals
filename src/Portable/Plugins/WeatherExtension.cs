using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== extension: DeskPals Weather

	/// <summary>
	/// The weather extension, a program of its own (DeskPalsWeather.exe, like the Pet Editor): snow, leaves, rain and
	/// blossom from Snowhush (PolyForm Noncommercial), fog, sun rays, grass and wind, ambient sound, scenes, colours,
	/// the local weather, pauses and shortcuts, all set in its own window (in the language and theme of
	/// DeskPals, which it reads from DeskPals' settings file). DeskPals finds it, starts it, opens its settings, offers to
	/// download it, brings its new versions, and reads what falls on the screen (weather-state.json) so the pets react: an
	/// umbrella in the rain, shivering in the snow, dancing in the leaves, love in the blossom, looking around in the fog,
	/// sunbathing in the sun.
	/// </summary>
	public static class WeatherExtension
	{
		public const string Asset = "DeskPals-Weather.zip";
		const string Exe = "DeskPalsWeather.exe";

		static string Folder => Path.Combine(InstallPlace.DataDir, "extensions", "weather");
		static string StateFile => Path.Combine(InstallPlace.DataDir, "extensions", "weather-state.json");
		static string HashFile => Path.Combine(Folder, "installed.sha256");

		/// <summary>The extension on this computer: next to DesktopPet.exe, downloaded, or (developer) built in the repository.</summary>
		public static string Installed()
		{
			string here = AppContext.BaseDirectory;
			var places = new[] { Path.Combine(here, "extensions", "weather", Exe), Path.Combine(Folder, Exe) }.ToList();
			var dir = new DirectoryInfo(here);
			for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
				places.Add(Path.Combine(dir.FullName, "weather", "bin", "Release", "net10.0-windows", "win-x64", Exe));
			return places.FirstOrDefault(File.Exists);
		}

		public static bool IsInstalled => Installed() != null;

		/// <summary>DeskPals starts it with itself (in the background).</summary>
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("ExtWeather", false);
			set { Program.MyData.SetSetting("ExtWeather", value); if (value) Start(true); else Quit(); }
		}

		/// <summary>It runs: its named signal exists.</summary>
		public static bool Running
		{
			get { if (EventWaitHandle.TryOpenExisting(@"Local\DeskPalsWeatherShow", out var s)) { s.Dispose(); return true; } return false; }
		}

		/// <summary>Start it (in the background), or, when it runs, show its settings. It is told where DeskPals keeps its settings.</summary>
		public static void Start(bool background)
		{
			string exe = Installed();
			if (exe == null) return;
			try
			{
				var start = new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe) };
				if (background) start.ArgumentList.Add("--background");
				start.ArgumentList.Add("--config");
				start.ArgumentList.Add(LocalData.ConfigPath());
				Process.Start(start);
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}

		public static void Quit()
		{
			string exe = Installed();
			if (exe == null || !Running) return;
			try { Process.Start(new ProcessStartInfo(exe, "--quit") { UseShellExecute = false }); } catch (Exception ex) { LogFile.Ignored(ex); }
		}

		/// <summary>With DeskPals: started when it is on, the pets watch what falls, and new versions come in.</summary>
		public static void Apply()
		{
			if (Enabled && IsInstalled && !Running) Start(true);
			if (watch == null) { watch = new System.Windows.Forms.Timer { Interval = 15000 }; watch.Tick += (s, e) => React(); watch.Start(); }
			if (!updateChecked && Updater.Auto) { updateChecked = true; _ = Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(_ => UpdateAsync(), TaskScheduler.FromCurrentSynchronizationContext()); }
		}

		/// <summary>The card's button: open its settings; not here, ask and download it first.</summary>
		public static async Task OpenAsync(IWin32Window owner, Action<string> status)
		{
			if (!IsInstalled && !await InstallAsync(owner, status)) return;
			if (!Enabled) Program.MyData.SetSetting("ExtWeather", true);
			Start(false);
		}

		public static async Task<bool> InstallAsync(IWin32Window owner, Action<string> status)
		{
			if (IsInstalled) return true;
			string title = Lang.T("ext.title");
			Updater.Release release;
			try { release = await Updater.GetLatestAsync(); }
			catch (Exception ex) { PetMessage.Show(owner, Lang.T("ext.installFail", ex.Message), title); return false; }
			if (release == null || !release.Assets.ContainsKey(Asset)) { PetMessage.Show(owner, Lang.T("ext.notYet"), title); return false; }
			long bytes = release.Sizes.TryGetValue(Asset, out long b) ? b : 0;
			string size = bytes > 0 ? Lang.T("editor.size", (bytes / 1048576.0).ToString("0.0", CultureInfo.CurrentCulture)) : "";
			if (PetMessage.Show(owner, Lang.T("ext.install", size), title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
			status?.Invoke(Lang.T("opt.editor.downloading").Replace(Lang.T("opt.editor.title"), title));
			try { await Download(release); }
			catch (Exception ex) { status?.Invoke(""); PetMessage.Show(owner, Lang.T("ext.installFail", ex.Message), title); return false; }
			status?.Invoke("");
			return IsInstalled;
		}

		/// <summary>The extension of this release, checked against SHA256SUMS, put in its folder (it remembers which one).</summary>
		static async Task Download(Updater.Release release)
		{
			string zip = Path.Combine(Path.GetTempPath(), Asset);
			await Updater.GetAssetAsync(release, Asset, zip);
			Directory.CreateDirectory(Folder);
			ZipFile.ExtractToDirectory(zip, Folder, true);
			File.WriteAllText(HashFile, Updater.Sha256(File.ReadAllBytes(zip)));
			File.Delete(zip);
		}

		static bool updateChecked;

		/// <summary>
		/// A new version of the extension in the latest release (another SHA-256 than the one installed): downloaded in the
		/// background, put in place of the old one (closed for a moment) and started again. Only the downloaded copy is
		/// brought up to date (not one next to DesktopPet.exe, which comes with the installer, nor a developer's build).
		/// </summary>
		public static async Task UpdateAsync()
		{
			try
			{
				if (!File.Exists(Path.Combine(Folder, Exe)) || Installed() != Path.Combine(Folder, Exe)) return;
				var release = await Updater.GetLatestAsync();
				if (release == null || !release.Assets.ContainsKey(Asset)) return;
				string hash = await Updater.AssetHashAsync(release, Asset);
				string mine = File.Exists(HashFile) ? File.ReadAllText(HashFile).Trim() : "";
				if (hash == null || string.Equals(hash, mine, StringComparison.OrdinalIgnoreCase)) return;
				bool was = Running;
				if (was) { Quit(); for (int i = 0; i < 40 && Running; i++) await Task.Delay(250); await Task.Delay(1500); }
				await Download(release);
				LogFile.Important("DeskPals Weather updated");
				if (was || Enabled) Start(true);
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}

		public static string Status() => !IsInstalled ? Lang.T("ext.state.missing") : Running ? "▶ " + Lang.T("ext.state.installed") : Lang.T("ext.state.installed");

		// ------------------------------------------------------------------ the pets react to the weather on the screen

		static System.Windows.Forms.Timer watch;
		static string lastKind;
		static DateTime nextReaction;
		static readonly Random rnd = new Random();

		/// <summary>What the extension shows now: what falls ("rain", "snow"…, or null), and fog and sun (0..1).</summary>
		internal static (bool on, string kind, double fog, double sun) State()
		{
			try
			{
				if (!File.Exists(StateFile) || (DateTime.Now - File.GetLastWriteTime(StateFile)).TotalSeconds > 60) return (false, null, 0, 0);
				var j = JsonNode.Parse(File.ReadAllText(StateFile));
				return ((bool?)j?["on"] ?? false, (string)j?["kind"], (double?)j?["fog"] ?? 0, (double?)j?["sun"] ?? 0);
			}
			catch (Exception) { return (false, null, 0, 0); }
		}

		/// <summary>The animations of a weather, the pet plays the first it has.</summary>
		internal static string[] Reaction(string kind, double fog, double sun) => kind switch
		{
			"rain" => new[] { "umbrella", "rain cloud", "puddle splash", "rain", "look up" },
			"snow" => new[] { "cold", "shiver", "snow cap", "scarf", "look up" },
			"leaves" => new[] { "dance", "happy", "look up", "jump" },
			"blossom" => new[] { "love", "hearts", "happy", "look up" },
			_ => fog > 0.4 ? new[] { "look around", "think", "scared" } : sun > 0.4 ? new[] { "sunbathing", "hot", "happy" } : null,
		};

		/// <summary>When the weather changes, and every few minutes while it lasts, the pets show it.</summary>
		static void React()
		{
			if (SelfTest.Running) return;   // the tests look at the pets and their bubbles: the weather stays out of them
			var (on, kind, fog, sun) = State();
			string now = on ? kind ?? (fog > 0.4 ? "fog" : sun > 0.4 ? "sun" : null) : null;
			bool changed = now != lastKind;
			lastKind = now;
			if (now == null || SpeechBubble.QuietNow) return;
			if (!changed && DateTime.Now < nextReaction) return;
			nextReaction = DateTime.Now.AddMinutes(3 + rnd.NextDouble() * 4);
			var names = Reaction(kind, fog, sun);
			if (names != null) Program.Mainthread?.ReactAll(names);
			// when it starts, a pet says something about it ("It's snowing!")
			if (changed) SpeechBubble.Say(Lang.T("weather.say." + now), 5);
		}

		// ------------------------------------------------------------------ in DeskPals' tray menu

		static string ScenesFile => Path.Combine(InstallPlace.DataDir, "extensions", "weather-scenes.json");
		static string CommandFile => Path.Combine(InstallPlace.DataDir, "extensions", "weather-command.json");

		/// <summary>
		/// The "DeskPals Weather" item of the tray menu (while it runs): on or off, its scenes (the one on is checked),
		/// a test and its settings. The extension writes its scenes, with their names in its language, in a small file.
		/// </summary>
		public static void FillMenu(ToolStripMenuItem item)
		{
			if (item == null) return;
			item.Text = Lang.T("ext.title");
			item.DropDownItems.Clear();
			JsonNode j = null;
			try { if (On && File.Exists(ScenesFile)) j = JsonNode.Parse(File.ReadAllText(ScenesFile)); } catch (Exception ex) { LogFile.Ignored(ex); }
			item.Visible = j != null;
			if (j == null) return;
			var labels = j["labels"];
			var on = new ToolStripMenuItem((string)labels?["on"] ?? Lang.T("ext.title")) { Checked = (bool?)j["on"] ?? false };
			on.Click += (s, e) => Send(new { on = !on.Checked });
			item.DropDownItems.Add(on);
			item.DropDownItems.Add(new ToolStripSeparator());
			string current = (string)j["current"];
			foreach (var scene in j["scenes"]?.AsArray() ?? new JsonArray())
			{
				string id = (string)scene?["id"];
				var s = new ToolStripMenuItem((string)scene?["name"]) { Checked = id == current };
				s.Click += (o, e) => Send(new { scene = id });
				item.DropDownItems.Add(s);
			}
			item.DropDownItems.Add(new ToolStripSeparator());
			if (Piling is string pile)
			{
				var clear = new ToolStripMenuItem(ClearText(pile));
				clear.Click += (s, e) => ClearSnow();
				item.DropDownItems.Add(clear);
			}
			var test = new ToolStripMenuItem((string)labels?["test"] ?? "Test");
			test.Click += (s, e) => Send(new { test = true });
			item.DropDownItems.Add(test);
			var settings = new ToolStripMenuItem((string)labels?["settings"] ?? Lang.T("opt.editor.button"));
			settings.Click += (s, e) => Start(false);
			item.DropDownItems.Add(settings);
		}

		/// <summary>It is turned on and runs (the pet's menu and the tray show "clear the snow" only then).</summary>
		public static bool On => Enabled && IsInstalled && Running;

		/// <summary>What piles up on the windows now ("snow", "leaves", "blossom"), or null (rain, fog, sun, nothing, paused, off).</summary>
		public static string Piling
		{
			get
			{
				if (!On) return null;
				var (on, kind, _, _) = State();
				return on && kind is "snow" or "leaves" or "blossom" ? kind : null;
			}
		}

		/// <summary>"❄ Clear the snow", "🍂 Sweep the leaves", "🌸 Sweep the blossom".</summary>
		public static string ClearText(string kind) => (kind switch { "leaves" => "🍂 ", "blossom" => "🌸 ", _ => "❄ " }) + Lang.T("ext.clear." + kind);

		/// <summary>What piled up on the windows and the taskbar goes away; the pet (when it was asked from its menu) sweeps and says so.</summary>
		public static void ClearSnow(FormPet pet = null)
		{
			Send(new { clear = true });
			pet?.React("sweep", "broom", "clean", "happy", "jump");
			if (!SpeechBubble.QuietNow) SpeechBubble.Say(Lang.T("ext.cleared"), 3);
		}

		/// <summary>The self-test writes the request but does not wake the extension that runs (the user's own).</summary>
		internal static bool TestQuiet;

		/// <summary>Ask the extension: the request in a small file, then its signal.</summary>
		static void Send(object request)
		{
			try
			{
				var node = JsonSerializer.SerializeToNode(request).AsObject();
				node["time"] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
				File.WriteAllText(CommandFile, node.ToJsonString());
				if (!TestQuiet && EventWaitHandle.TryOpenExisting(@"Local\DeskPalsWeatherCommand", out var signal)) { signal.Set(); signal.Dispose(); }
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}
	}
}
