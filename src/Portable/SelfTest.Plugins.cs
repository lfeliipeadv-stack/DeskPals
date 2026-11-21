using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// The self-test, part: plugins. Check, Try and the list of tests are in SelfTest.cs.
	internal static partial class SelfTest
	{
		/// <summary>The plugins of 2.7: links, quiet hours, levels, the agents' command and their settings files.</summary>
		static void NewPlugins()
		{
			Check("clipboard: a link is recognized", ClipboardWatch.IsLink("https://example.com/a?b=1", out var u) && u.Host == "example.com");
			Check("clipboard: plain text is not a link", !ClipboardWatch.IsLink("olá mundo https://x.com", out _));
			Check("clipboard: off unless turned on", !ClipboardWatch.Enabled || Program.MyData.GetSetting("ClipWatch").Length > 0);
			Check("ask shortcut: off unless turned on", HideHotkey.AskEnabled == (Program.MyData.GetSetting("AskHotkey").Length > 0 && Program.MyData.GetSettingBool("AskHotkey", false)));

			// memory: reading, when to offer, the simple tidy
			var mem = MemoryCare.Status();
			Check("memory: reads the RAM", mem.load > 0 && mem.load <= 100 && mem.total > 0 && mem.available > 0 && mem.available <= mem.total, $"{mem.load}% of {MemoryCare.Size(mem.total)}");
			Check("memory: off unless turned on", !MemoryCare.Enabled || Program.MyData.GetSetting("MemoryCare").Length > 0);
			Check("memory: sizes in words", MemoryCare.Size(1288490189) == 1.2.ToString("0.0") + " GB" && MemoryCare.Size(350L * 1048576) == "350 MB");
			{
				var t0 = new DateTime(2026, 1, 1, 10, 0, 0); DateTime? since = null; var never = DateTime.MinValue;
				Check("memory: below the limit, nothing", MemoryCare.Decide(t0, 70, 85, false, ref since, never, never, false) == MemoryCare.Decision.None && since == null);
				MemoryCare.Decide(t0, 90, 85, false, ref since, never, never, false);
				Check("memory: just went high, it waits", since == t0 && MemoryCare.Decide(t0.AddMinutes(1), 90, 85, false, ref since, never, never, false) == MemoryCare.Decision.None);
				Check("memory: high for a while, it asks", MemoryCare.Decide(t0.AddMinutes(3), 90, 85, false, ref since, never, never, false) == MemoryCare.Decision.Ask);
				Check("memory: high for a while, tidies by itself when set", MemoryCare.Decide(t0.AddMinutes(3), 90, 85, true, ref since, never, never, false) == MemoryCare.Decision.Clean);
				Check("memory: not during a game, video or quiet time", MemoryCare.Decide(t0.AddMinutes(3), 90, 85, true, ref since, never, never, true) == MemoryCare.Decision.None);
				Check("memory: not again right after a tidy", MemoryCare.Decide(t0.AddMinutes(3), 90, 85, true, ref since, t0.AddMinutes(-5), never, false) == MemoryCare.Decision.None);
				Check("memory: does not ask again soon", MemoryCare.Decide(t0.AddMinutes(3), 90, 85, false, ref since, never, t0.AddMinutes(-10), false) == MemoryCare.Decision.None);
				MemoryCare.Decide(t0.AddMinutes(4), 60, 85, false, ref since, never, never, false);
				Check("memory: back below the limit, the wait starts over", since == null);
			}
			{
				var clock = System.Diagnostics.Stopwatch.StartNew();
				int trimmed = MemoryCare.TrimPrograms();
				Check("memory: simple tidy reaches the user's programs", trimmed > 0, $"{trimmed} programs, {clock.ElapsedMilliseconds} ms");
				Check("memory: the pet keeps working after a tidy", Application.OpenForms.OfType<FormPet>().Any(p => !p.IsDisposed));
				string tmp = Path.Combine(Path.GetTempPath(), "desktoppet-memtest.txt");
				MemoryCare.FullCleanToFile(tmp);
				var lines = File.Exists(tmp) ? File.ReadAllLines(tmp) : new string[0];
				try { File.Delete(tmp); } catch (Exception) { }
				Check("memory: full tidy writes how it went", MemoryCare.IsAdmin ? lines.Length == 4 : lines.SequenceEqual(new[] { "admin refused" }), string.Join(", ", lines));
			}

			// every visitor drawing: rows of the same width, only letters that have a color
			foreach (Visitors.Kind kind in Enum.GetValues(typeof(Visitors.Kind)))
			{
				var colors = Visitors.Colors(kind, new Random(1));
				var bad = Visitors.Drawings(kind).SelectMany((d, i) => d.Select((row, j) => (i, j, row)))
					.Where(r => r.row.Length != Visitors.Drawings(kind)[r.i][0].Length || r.row.Any(ch => ch != '.' && !colors.ContainsKey(ch)))
					.Select(r => $"drawing {r.i} row {r.j}").ToList();
				Check("visitor " + kind.ToString().ToLowerInvariant() + ": drawing is clean", bad.Count == 0, string.Join(", ", bad));
			}

			int from = NoticeOptions.QuietFrom, to = NoticeOptions.QuietTo;
			try
			{
				int h = DateTime.Now.Hour;
				NoticeOptions.QuietFrom = h; NoticeOptions.QuietTo = (h + 1) % 24;
				Check("quiet hours: now is inside", NoticeOptions.InQuietHours && SpeechBubble.QuietNow);
				NoticeOptions.QuietFrom = (h + 2) % 24; NoticeOptions.QuietTo = (h + 3) % 24;
				Check("quiet hours: now is outside", !NoticeOptions.InQuietHours);
				NoticeOptions.QuietFrom = -1;
				Check("quiet hours: off", !NoticeOptions.InQuietHours);
			}
			finally { NoticeOptions.QuietFrom = from; NoticeOptions.QuietTo = to; }

			Check("level: 1 at the start, 2 after 3 tasks, 3 after 8", AgentLevel.LevelFor(0) == 1 && AgentLevel.LevelFor(3) == 2 && AgentLevel.LevelFor(8) == 3 && AgentLevel.LevelFor(7) == 2);
			Check("agents: unknown event refused", OtherAgents.Signal("nonsense") == 2);
			Check("agents: command calls this program", OtherAgents.Command("done").Contains("--agent=done"));

			string dir = Path.Combine(Path.GetTempPath(), "desktoppet-agents-" + Guid.NewGuid().ToString("N"));
			OtherAgents.HomeOverride = dir;
			try
			{
				// Codex: notify before the first table, removed again, and never over someone else's notify
				Directory.CreateDirectory(Path.GetDirectoryName(OtherAgents.CodexConfig));
				File.WriteAllText(OtherAgents.CodexConfig, "model = \"o3\"\n\n[profiles.x]\nmodel = \"y\"\n");
				Check("codex: added", OtherAgents.SetCodex(true) == null && OtherAgents.CodexInstalled);
				var lines = File.ReadAllLines(OtherAgents.CodexConfig).ToList();
				Check("codex: notify before the first table", lines.FindIndex(l => l.StartsWith("notify")) < lines.FindIndex(l => l.StartsWith("[")));
				Check("codex: removed", OtherAgents.SetCodex(false) == null && !OtherAgents.CodexInstalled && File.ReadAllText(OtherAgents.CodexConfig).Contains("[profiles.x]"));
				// someone else's notify (like the Codex app's own): run after the pet, put back exactly when turned off
				string theirs = "notify = [ \"C:\\\\Program Files\\\\Other\\\\codex-computer-use.exe\", \"turn-ended\" ]";
				File.WriteAllText(OtherAgents.CodexConfig, theirs + "\nmodel = \"o3\"\n");
				Check("codex: their notify read", string.Join("|", OtherAgents.TomlStrings(theirs)) == "C:\\Program Files\\Other\\codex-computer-use.exe|turn-ended");
				Check("codex: chained with their notify", OtherAgents.SetCodex(true) == null && OtherAgents.CodexInstalled);
				var chained = OtherAgents.TomlStrings(File.ReadAllLines(OtherAgents.CodexConfig).First(l => l.StartsWith("notify")));
				Check("codex: the pet first, then their program with its arguments", chained != null && chained.Count == 5 && chained[1] == "--agent=done" && chained[2] == "--then"
					&& chained[3] == "C:\\Program Files\\Other\\codex-computer-use.exe" && chained[4] == "turn-ended", chained == null ? "?" : string.Join("|", chained));
				Check("codex: only one notify line", File.ReadAllLines(OtherAgents.CodexConfig).Count(l => l.StartsWith("notify")) == 1);
				OtherAgents.SetCodex(false);
				Check("codex: their notify put back exactly", File.ReadAllText(OtherAgents.CodexConfig) == theirs + "\r\nmodel = \"o3\"\r\n" || File.ReadAllText(OtherAgents.CodexConfig) == theirs + "\nmodel = \"o3\"\n",
					File.ReadAllText(OtherAgents.CodexConfig));
				// Cursor: hooks merged with the user's own
				Directory.CreateDirectory(Path.GetDirectoryName(OtherAgents.CursorHooks));
				File.WriteAllText(OtherAgents.CursorHooks, "{\"version\":1,\"hooks\":{\"stop\":[{\"command\":\"mine.exe\"}]}}");
				Check("cursor: added", OtherAgents.SetCursor(true) == null && OtherAgents.CursorInstalled);
				Check("cursor: the user's hook is kept", File.ReadAllText(OtherAgents.CursorHooks).Contains("mine.exe"));
				Check("cursor: removed", OtherAgents.SetCursor(false) == null && !OtherAgents.CursorInstalled && File.ReadAllText(OtherAgents.CursorHooks).Contains("mine.exe"));
			}
			finally
			{
				OtherAgents.HomeOverride = null;
				try { Directory.Delete(dir, true); } catch (Exception) { }
				AgentBridge.Apply();
			}

			Check("notes: tasks saved", Notes.Tasks != null);
			Check("api: example uses the secret file", LocalApi.Example.Contains("agent.json") && LocalApi.Example.Contains("X-DesktopPet"));
		}

		/// <summary>
		/// The plugins for real: another process calling "--agent" like Codex does (with a program chained after it),
		/// a butterfly flying to the pet, a window pushed by the walking pet, a full-screen window hiding the pets,
		/// a new song making the pet react.
		/// </summary>
		static async Task LivePlugins()
		{
			Live = true;
			try
			{
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				await Task.Delay(300);

				// ---- agents: a second DesktopPet process, called exactly like Codex calls its notify program
				bool apiWas = LocalApi.Enabled, levelWas = AgentLevel.Enabled;
				int tasks = AgentLevel.Tasks;
				try
				{
					LocalApi.Enabled = true; AgentLevel.Enabled = true;
					await Task.Delay(300);
					Check("live agents: the bridge listens for the API", AgentBridge.Listening);
					var psi = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false, CreateNoWindow = true };
					foreach (var a in new[] { "--agent=done", "--then", Environment.ProcessPath, "--agent=thinking", "{\"type\":\"agent-turn-complete\"}" }) psi.ArgumentList.Add(a);
					int exit = -1;
					await Task.Run(() => { using (var p = System.Diagnostics.Process.Start(psi)) { if (p.WaitForExit(20000)) exit = p.ExitCode; else p.Kill(); } });
					await Task.Delay(800);
					Check("live agents: the command ends well", exit == 0, "exit " + exit);
					Check("live agents: the pet counted the finished task", AgentLevel.Tasks == tasks + 1, $"{tasks} -> {AgentLevel.Tasks}");
					Check("live agents: the chained program ran after (thinking)", AgentBridge.Last == AgentBridge.Activity.Thinking, AgentBridge.Last.ToString());
				}
				finally
				{
					Program.MyData.SetSetting("AgentTasks", tasks);
					LocalApi.Enabled = apiWas; AgentLevel.Enabled = levelWas;
					foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				}

				var pet = Pet();
				if (pet == null) { lines.Add("SKIP  live plugins: no pet on screen"); return; }

				// ---- music: a new song (Spotify's title) makes the pet react; a paused player does not count
				MusicWatch.FakeTitle = "Spotify Premium";
				Check("live music: paused Spotify is not a song", MusicWatch.Playing() == "");
				int react = pet.FindAnimation("dance", "party", "happy", "cheer", "jump");
				string before = pet.CurrentAnimationName;
				MusicWatch.FakeTitle = "Daft Punk - One More Time";
				MusicWatch.Tick();
				await Task.Delay(400);
				if (react >= 0) Check("live music: the pet reacts to a new song", pet.CurrentAnimationName != before || new[] { "dance", "party", "happy", "cheer", "jump" }.Contains(pet.CurrentAnimationName), pet.CurrentAnimationName);
				else lines.Add("SKIP  live music: this pet has no dance or happy animation");
				MusicWatch.FakeTitle = null;
				string real = MusicWatch.Playing();
				lines.Add("INFO  live music: Spotify now: " + (real.Length > 0 ? real : "nothing playing"));
				pet.React("walk");
				await Task.Delay(1500);

				// ---- visitors: each one flies to the pet, visits it, and goes on to the other side of the screen
				foreach (Visitors.Kind kind in Enum.GetValues(typeof(Visitors.Kind)))
				{
					string who = "live visitor " + kind.ToString().ToLowerInvariant();
					Visitors.Visit(kind);
					await Task.Delay(300);
					var fly = Visitors.Flying;
					Check(who + ": appears", fly != null && Visitors.FlyingKind == kind);
					if (fly == null) continue;
					var area = Screen.FromControl(pet).WorkingArea;
					bool fromRight = fly.Left > area.Left + area.Width / 2;
					double nearest = double.MaxValue;
					int lastLeft = fly.Left;
					var clock = System.Diagnostics.Stopwatch.StartNew();
					while (Visitors.Flying != null && clock.Elapsed.TotalSeconds < 90)
					{
						var c = new Point(fly.Left + fly.Width / 2, fly.Top + fly.Height / 2);
						var head = new Point(pet.Left + pet.Width / 2, pet.Top + pet.Height / 3);
						nearest = Math.Min(nearest, Math.Sqrt(Math.Pow(c.X - head.X, 2) + Math.Pow(c.Y - head.Y, 2)));
						lastLeft = fly.Left;
						await Task.Delay(50);
					}
					Check(who + ": reaches the pet", nearest < 80, $"closest {nearest:0} px");
					Check(who + ": goes away and closes", Visitors.Flying == null, $"{clock.Elapsed.TotalSeconds:0} s");
					if (Visitors.CaughtLast)
						// the frog caught it with its tongue: it ends inside the pet, not on the other side
						Check(who + ": caught by the pet, gone inside it", lastLeft > pet.Left - fly.Width && lastLeft < pet.Right, $"last seen at x={lastLeft}, pet {pet.Left}-{pet.Right}");
					else
						Check(who + ": leaves on the other side of the screen", fromRight ? lastLeft < area.Left + 40 : lastLeft > area.Right - 80,
							$"came from the {(fromRight ? "right" : "left")}, last seen at x={lastLeft}");
				}

				// ---- full screen: something filling the screen hides the pets, and it comes back after
				bool fsWas = FullScreenWatch.Enabled;
				FullScreenWatch.Enabled = true;
				try
				{
					// the real thing: Windows only says that something is in full screen when there really is one
					// (a game, a presentation); a window that covers the screen is not enough, so on a computer
					// without one there is nothing to check here
					using (var full = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, Bounds = Screen.PrimaryScreen.Bounds, BackColor = Color.Black, Text = "full screen test" })
					{
						try
						{
							full.Show(); full.Activate();
							for (int i = 0; i < 10 && !FullScreenWatch.Active; i++) { await Task.Delay(300); FullScreenWatch.Tick(); }
							lines.Add($"INFO  live full screen: Windows reports a full screen app: {FullScreenWatch.Active}; the test window is in front: {GetForegroundWindow() == full.Handle}");
						}
						finally { full.Close(); }
						for (int i = 0; i < 10 && FullScreenWatch.Active; i++) { await Task.Delay(300); FullScreenWatch.Tick(); }
					}
					// for sure: the plugin is told that something is filling the screen, and then that it is over
					FullScreenWatch.TestState = FullScreenWatch.NoFullState;   // first: nothing is filling the screen
					FullScreenWatch.Tick();
					if (HideHotkey.Hidden) HideHotkey.SetHidden(false);        // and the pets are showing
					FullScreenWatch.TestState = FullScreenWatch.FullState;     // now: something is filling the screen
					FullScreenWatch.Tick();
					Check("live full screen: the pets hide", FullScreenWatch.Active && HideHotkey.Hidden && !pet.Visible,
						$"active {FullScreenWatch.Active}, hidden {HideHotkey.Hidden}, visible {pet.Visible}");
					FullScreenWatch.TestState = FullScreenWatch.NoFullState;   // and then it is over
					FullScreenWatch.Tick();
					Check("live full screen: the pets are shown after", !FullScreenWatch.Active && !HideHotkey.Hidden && pet.Visible,
						$"active {FullScreenWatch.Active}, hidden {HideHotkey.Hidden}, visible {pet.Visible}");
				}
				finally
				{
					FullScreenWatch.TestState = null;
					FullScreenWatch.Enabled = fsWas;
					if (HideHotkey.Hidden) HideHotkey.SetHidden(false);
				}
			}
			finally { Live = false; MusicWatch.FakeTitle = null; FullScreenWatch.TestState = null; }
		}

		/// <summary>Typing along, time in programs, the summary of the day and help with copied errors.</summary>
		static async Task FunPlugins()
		{
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			// typing: only a number
			int keys = TypingPet.Today;
			TypingPet.TestKey(); TypingPet.TestKey();
			Check("typing: keys counted", TypingPet.Today == keys + 2, TypingPet.Today.ToString());

			// copied errors
			Check("copied text: an error is recognized", ClipboardWatch.LooksLikeError("System.NullReferenceException: Object reference not set") && ClipboardWatch.LooksLikeError("Erro 404: página não encontrada") && !ClipboardWatch.LooksLikeError("Hoje o dia está bonito e vamos passear no parque."));

			// time in each program and the summary of the day
			AppTime.TestAdd("Programa de teste", 25 * 60);
			Check("time in programs: counted for today", AppTime.Today(10).Any(x => x.name == "Programa de teste" && x.minutes == 25));
			Check("summary: the facts of today", DaySummary.Facts().Contains("Programa de teste"), DaySummary.Facts());
			DaySummary.Tell(false);
			SpeechBubble said = null;
			// with an AI set up the summary is written by it (in its own words): any answer that is not "thinking…" counts
			bool withAi = AiAssistant.Configured;
			for (int i = 0; i < (withAi ? 300 : 100) && said == null; i++) { await Task.Delay(100); said = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible && (withAi ? b.TestText.Length > 0 && b.TestText != Lang.T("ask.thinking") : b.TestText.Contains("Programa de teste"))); }
			if (said == null && QuietReason() != null) lines.Add("SKIP  summary: the pet tells it (the program keeps quiet now: " + QuietReason() + ")");
			else Check("summary: the pet tells it", said != null);
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			AppTime.TestRemove("Programa de teste");

		}

		/// <summary>Colors, the week chart, "do not disturb", the chat shortcut, updates.</summary>
		static async Task NewTools()
		{
			// settings: always next to the exe; settings left in the start folder by older versions come along
			Check("settings: next to the program", string.Equals(Path.GetDirectoryName(LocalData.ConfigPath()).TrimEnd('\\'), AppContext.BaseDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase), LocalData.ConfigPath());
			string progDir = Path.Combine(Path.GetTempPath(), "dp-selftest-prog"), startDir = Path.Combine(Path.GetTempPath(), "dp-selftest-start");
			try
			{
				Directory.CreateDirectory(progDir); Directory.CreateDirectory(startDir);
				string target = Path.Combine(progDir, "DesktopPet.config");
				if (File.Exists(target)) File.Delete(target);
				File.WriteAllText(Path.Combine(startDir, "DesktopPet.config"), "antigo");
				Check("settings: brought from the start folder", LocalData.RecoverStray(target, startDir) && File.ReadAllText(target) == "antigo");
				File.WriteAllText(target, "atual");
				Check("settings: the program's own are never overwritten", !LocalData.RecoverStray(target, startDir) && File.ReadAllText(target) == "atual");
			}
			finally { try { Directory.Delete(progDir, true); Directory.Delete(startDir, true); } catch (Exception skipped) { LogFile.Ignored(skipped); } }
			// the number choice of the reminder editor (instead of the classic spin boxes)
			using (var st = new PetStepper { Values = new[] { 5, 10, 15, 30 } })
			{
				st.Value = 12;
				bool nearest = st.Value == 10;
				st.Step(5); bool top = st.Value == 30;
				st.Step(-9); bool bottom = st.Value == 5;
				Check("stepper: nearest choice and limits", nearest && top && bottom, st.Value.ToString());
			}

			// first setup: the steps (the city only with the weather), then name, plugins and city are saved together
			var before = FormSetup.Choices().Select(c => (c, on: c.Get())).ToList();
			string nameBefore = UserProfile.Name, cityBefore = Program.MyData.GetSetting("WeatherCity"), weatherOnBefore = Program.MyData.GetSetting("WeatherOn");
			bool setupDone = Program.MyData.GetSettingBool("SetupDone", false), startupBefore = WindowsStartup.Enabled;
			try
			{
				using (var w = new FormSetup())
				{
					foreach (var c in before) w.TestTick(c.c.Id, false);
					bool noCity = !w.TestSteps.Contains(FormSetup.Step.City);
					w.TestTick("games", true); w.TestTick("timer", true); w.TestTick("weather", true);
					Check("setup: steps (city only with the weather)", noCity && w.TestSteps.SequenceEqual(new[] { FormSetup.Step.Name, FormSetup.Step.Plugins, FormSetup.Step.City, FormSetup.Step.Ai, FormSetup.Step.Done }), string.Join(",", w.TestSteps));
					w.TestName("  Luan   Teste ");
					w.TestPlace(new Weather.Place { Name = "Curitiba, Paraná, BR", Lat = -25.43, Lon = -49.27 });
					w.TestStartup(startupBefore);
					w.Apply();
				}
				Check("setup: chosen plugins on, others off", PluginSwitch.On("games") && PluginSwitch.On("timer") && !PluginSwitch.On("notes") && !Mood.Daily);
				Check("setup: city of the weather", Weather.City?.Name == "Curitiba, Paraná, BR" && Weather.Enabled, Weather.City?.Name);
				Check("setup: name saved (spaces tidied)", UserProfile.Name == "Luan Teste", UserProfile.Name);
				Check("name: in the greetings", UserProfile.Greet("Oi! Eu sou Rex.") == "Oi, Luan Teste! Eu sou Rex." && UserProfile.Greet("Bom dia! ☀️") == "Bom dia, Luan Teste! ☀️"
					&& UserProfile.Greet("Hi! I'm Rex.") == "Hi, Luan Teste! I'm Rex." && UserProfile.Greet("Que dia bonito!") == "Que dia bonito!", UserProfile.Greet("Oi! Eu sou Rex."));
				var said = await FormChat.LocalAnswer("qual é o meu nome?");
				Check("name: the chat knows it", said != null && said.Text.Contains("Luan Teste"), said?.Text);
				UserProfile.Name = "";
				Check("name: no name, greetings as before", UserProfile.Greet("Oi! Eu sou Rex.") == "Oi! Eu sou Rex.");
			}
			finally
			{
				foreach (var (c, on) in before) if (c.Get() != on) c.Set(on);
				UserProfile.Name = nameBefore;
				Program.MyData.SetSetting("WeatherCity", cityBefore); Program.MyData.SetSetting("WeatherOn", weatherOnBefore);
				Program.MyData.SetSetting("SetupDone", setupDone);
			}

			// installed in Program Files: settings in the user's folder (the same file older installs used)
			string pf = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			Check("install: goes to Program Files", InstallPlace.ProgramDir.StartsWith(pf, StringComparison.OrdinalIgnoreCase) && InstallPlace.InProgramFiles(InstallPlace.ProgramDir) && !InstallPlace.InProgramFiles(InstallPlace.DataDir), InstallPlace.ProgramDir);
			Check("install: settings out of Program Files", string.Equals(Path.GetDirectoryName(LocalData.ConfigPath(InstallPlace.ProgramDir, Path.GetTempPath())), InstallPlace.DataDir, StringComparison.OrdinalIgnoreCase));
			Check("install: older install keeps its settings file", string.Equals(LocalData.ConfigPath(InstallPlace.DataDir, Path.GetTempPath()), LocalData.ConfigPath(InstallPlace.ProgramDir, Path.GetTempPath()), StringComparison.OrdinalIgnoreCase));
			string uninstall = UninstallScript.Build("DesktopPet", new[] { @"C:\x\DesktopPet.url" }, InstallPlace.UninstallKey, true);
			Check("install: the uninstaller asks for administrator and cleans HKLM", uninstall.Contains("-Verb RunAs") && uninstall.Contains(@"reg delete ""HKLM\" + InstallPlace.UninstallKey) && uninstall.Contains(@"%LOCALAPPDATA%\DesktopPet"));
			// colors: every accent changes the palette and green is the original one
			string wasAccent = Theme.AccentId;
			var greenAccent = Theme.Accent;
			Theme.AccentId = "blue";
			Check("colors: another accent", Theme.Accent != greenAccent && Theme.AccentSoft != Theme.Accent, Theme.Accent.ToString());
			Theme.AccentId = "nao-existe";
			Check("colors: unknown falls back to green", Theme.AccentId == "green" && Theme.Accent == greenAccent);
			Check("colors: 12 to choose", Theme.Accents.Length == 12 && Theme.Accents.All(a => Lang.T("accent." + a.Id) != "accent." + a.Id));
			Theme.AccentId = wasAccent;

			// week chart: past days are kept, today is today's number
			DayStats.TestSet("selftestWeek", DateTime.Today.AddDays(-2), 7);
			DayStats.Add("selftestWeek", 3);
			var week = DayStats.Week("selftestWeek");
			Check("week: 7 days, past days kept", week.Length == 7 && week[4] == 7 && week[6] >= 3, string.Join(",", week));
			Check("week: computer time per day", AppTime.Week().Length == 7);
			Program.MyData.SetSetting("Hist.selftestWeek", ""); Program.MyData.SetSetting("Day.selftestWeek", "");

			// do not disturb: quiet for a while, notices wait, then they come back
			DoNotDisturb.Start(30);
			Check("do not disturb: on for 30 min", DoNotDisturb.Active && SpeechBubble.QuietNow && DoNotDisturb.StatusText != null, DoNotDisturb.StatusText);
			int held = SpeechBubble.HeldCount;
			SpeechBubble.Notify("teste não perturbe", 3);
			Check("do not disturb: notices wait", SpeechBubble.HeldCount == held + 1 && !Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible && b.TestText == "teste não perturbe"));
			DoNotDisturb.Stop();
			Check("do not disturb: off", !DoNotDisturb.Active);
			bool wasCalls = DoNotDisturb.Calls;
			DoNotDisturb.TestCall = true; DoNotDisturb.Calls = true;
			Check("do not disturb: quiet in a call", DoNotDisturb.Active && DoNotDisturb.InCall);
			DoNotDisturb.TestCall = null; DoNotDisturb.Calls = wasCalls;
			// the held notice comes back a few seconds after the quiet time ends ("1 notice arrived")
			SpeechBubble back = null;
			for (int i = 0; i < 120 && back == null; i++) { await Task.Delay(100); back = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible && b.TestText == Lang.T("quiet.heldOne", 1)); }
			Check("do not disturb: notices come back after", back != null);
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			SpeechBubble.TestClearHeld();

			// chat shortcut: opens, and closes when in front
			FormChat.Toggle();
			await Task.Delay(400);
			Check("shortcut: opens the chat", FormChat.IsOpen);
			Application.OpenForms.OfType<FormChat>().FirstOrDefault()?.Activate();
			await Task.Delay(300);
			FormChat.Toggle();
			await Task.Delay(400);
			if (FormChat.IsOpen) { lines.Add("SKIP  shortcut: the chat could not get the focus (closing it by hand)"); Application.OpenForms.OfType<FormChat>().FirstOrDefault()?.Close(); }
			else Check("shortcut: closes the chat", true);

			// updates: on by default, nothing found while the repository is private
			Check("updates: automatic by default", Program.MyData.GetSetting("AutoUpdate").Length > 0 || Updater.Auto);
		}

		/// <summary>The timer with the pet: reading the time, the panel, pause, +5 min, the end and closing.</summary>
		static async Task TimerCheck()
		{
			Check("timer: reads times", SimpleTimer.Parse("10") == TimeSpan.FromMinutes(10) && SimpleTimer.Parse("1:30") == TimeSpan.FromSeconds(90)
				&& SimpleTimer.Parse("1:00:00") == TimeSpan.FromHours(1) && SimpleTimer.Parse("2h") == TimeSpan.FromHours(2) && SimpleTimer.Parse("90s") == TimeSpan.FromSeconds(90)
				&& SimpleTimer.Parse("1,5") == TimeSpan.FromSeconds(90) && SimpleTimer.Parse("abc") == null && SimpleTimer.Parse("0") == null && SimpleTimer.Parse("30h") == null);
			Check("timer: shows the time", SimpleTimer.Format(TimeSpan.FromSeconds(3599)) == "59:59" && SimpleTimer.Format(TimeSpan.FromHours(1)) == "1:00:00" && SimpleTimer.Format(TimeSpan.FromSeconds(0.2)) == "0:01");
			var pet = Pet();
			if (pet == null) { lines.Add("SKIP  timer: no pet on screen"); return; }
			for (int i = 0; i < 60 && !(pet.OnGround && Screen.FromControl(pet).WorkingArea.Contains(pet.Bounds)); i++) await Task.Delay(150);   // arriving, or in a scene below the taskbar
			FormPet.Hold(true);
			try
			{
				SimpleTimer.Start(TimeSpan.FromMinutes(1));
				await Task.Delay(500);
				var panel = SimpleTimer.Panel;
				Check("timer: the panel is with the pet", panel != null && Rectangle.Inflate(pet.Bounds, panel.Width + 60, panel.Height + 60).IntersectsWith(panel.Bounds), $"panel {panel?.Bounds}, pet {pet.Bounds}");
				Check("timer: counts down", SimpleTimer.Left < TimeSpan.FromMinutes(1) && SimpleTimer.Left > TimeSpan.FromSeconds(55), SimpleTimer.LeftText);
				if (panel != null) Shot("timer-1", Rectangle.Union(panel.Bounds, pet.Bounds));
				if (panel != null)
				{
					var area = Screen.FromControl(pet).WorkingArea;
					var spot = new Point(area.Left + 120, area.Top + 120);
					SimpleTimer.TestMove(spot);
					await Task.Delay(700);
					Check("timer: dragged somewhere, it stays there", panel.Location == spot, $"{panel.Location}");
				}
				SimpleTimer.TestPress("pause");
				var frozen = SimpleTimer.Left;
				await Task.Delay(1200);
				Check("timer: pause stops the time", SimpleTimer.Paused && SimpleTimer.Left == frozen);
				if (panel != null) Shot("timer-2-pausado", Rectangle.Union(panel.Bounds, pet.Bounds));
				SimpleTimer.TestPress("pause");
				SimpleTimer.TestPress("add");
				Check("timer: +5 min", !SimpleTimer.Paused && SimpleTimer.Left > TimeSpan.FromMinutes(5.5));
				SimpleTimer.TestEndNow();
				await Task.Delay(1200);
				Check("timer: at the end it stays on screen until closed", SimpleTimer.Finished && SimpleTimer.Panel != null);
				if (SimpleTimer.Panel != null) Shot("timer-3-fim", Rectangle.Union(SimpleTimer.Panel.Bounds, pet.Bounds));
				SimpleTimer.TestPress("close");
				await Task.Delay(200);
				Check("timer: closed", !SimpleTimer.Active && SimpleTimer.Panel == null);
			}
			finally { SimpleTimer.Close(); FormPet.Hold(false); }
		}

		/// <summary>Weather: texts from the codes, and the real Open-Meteo (skipped without internet).</summary>
		static async Task WeatherCheck()
		{
			Check("weather: codes have texts", new[] { 0, 2, 3, 45, 53, 63, 73, 81, 95 }.All(c => Lang.T(Weather.Describe(c).key) != Weather.Describe(c).key));
			var t = new Weather.Today { Code = 63, Min = 18, Max = 26, RainChance = 80 };
			string s = Weather.Summary(t, new Weather.Place { Name = "São Paulo, SP, BR" });
			Check("weather: summary with the umbrella tip", s.Contains("São Paulo") && s.Contains("18") && s.Contains("26") && s.Contains("80") && !s.Contains("{"), s);
			// the forecast in words: rain at what time, a storm first, tomorrow cooler
			var now = new DateTime(2026, 5, 4, 9, 30, 0);
			var fc = new Weather.Forecast { Now = now, Code = 2, RainNow = 10 };
			for (int i = 0; i < 30; i++) fc.Hours.Add((now.Date.AddHours(9 + i), 25, i == 6 ? 70 : 10, 8, 2));
			fc.Days.Add((now.Date, 2, 30, 20, 70)); fc.Days.Add((now.Date.AddDays(1), 3, 26, 15, 10));
			string o = Weather.Outlook(fc);
			Check("weather outlook: when the rain starts", o.Contains("15:00") && o.Contains("70"), o);
			Check("weather outlook: tomorrow cooler", o.Contains("15") && !o.Contains("{"), o);
			fc.Hours[3] = (fc.Hours[3].time, 25, 40, 8, 95);
			string o2 = Weather.Outlook(fc);
			Check("weather outlook: a storm comes first", o2.Contains("12:00") && !o2.Contains("15:00"), o2);
			try
			{
				var place = await Weather.FindCity("Curitiba");
				Check("weather online: city found", place != null && Math.Abs(place.Lat + 25.4) < 1 && Math.Abs(place.Lon + 49.3) < 1, place?.Name + " " + place?.Lat + "," + place?.Lon);
				if (place != null)
				{
					var w = await Weather.Load(place);
					Check("weather online: forecast read", w != null && w.Max >= w.Min && w.Max < 60 && w.Min > -40 && w.RainChance >= 0 && w.RainChance <= 100, w == null ? "" : $"{w.Min}..{w.Max} code {w.Code} rain {w.RainChance}% soon {w.RainSoon}%");
				}
			}
			catch (Exception ex) when (ex is System.Net.Http.HttpRequestException || ex is TaskCanceledException)
			{
				lines.Add("SKIP  weather online: no internet (" + ex.Message + ")");
			}
		}

		/// <summary>Breaks, coming back and long programs, with a made-up clock (every 30 s, like the real timer).</summary>
		static void ActivityCare()
		{
			Activity.Reset();
			var t = new DateTime(2026, 11, 11, 9, 0, 0);
			string said = null;
			int when = -1;
			// 100 minutes of work without a pause: the break is suggested once, at 90 minutes
			for (int i = 0; i <= 200; i++)
			{
				string s = Activity.Step(t.AddSeconds(i * 30), 5, false);
				if (s != null && said == null) { said = s; when = i / 2; }
				else if (s != null) { Check("activity: break said only once", false, s + " at " + i / 2 + " min"); break; }
			}
			Check("activity: break after 90 minutes without a pause", said == "break" && when == Activity.BreakAfter, said + " at " + when + " min");

			// a 6-minute pause starts again: no break 80 minutes later
			Activity.Reset();
			bool breakSaid = false;
			for (int i = 0; i <= 120; i++) breakSaid |= Activity.Step(t.AddMinutes(i), 5, false) == "break";
			for (int i = 0; i < 6; i++) Activity.Step(t.AddMinutes(121 + i), 600, false);   // away: idle
			Activity.Reset();
			var t2 = t.AddMinutes(200);
			for (int i = 0; i < 60; i++) Activity.Step(t2.AddMinutes(i), 5, false);
			for (int i = 0; i < 6; i++) Activity.Step(t2.AddMinutes(60 + i), 400, false);
			bool afterPause = false;
			for (int i = 0; i <= 80; i++) afterPause |= Activity.Step(t2.AddMinutes(66 + i), 5, false) == "break";
			Check("activity: a 5-minute pause counts as a break", !afterPause);
			Check("activity: during Pomodoro the pet does not suggest breaks", Enumerable.Range(0, 200).All(i => { if (i == 0) Activity.Reset(); return Activity.Step(t.AddMinutes(i), 5, true) != "break"; }));

			// coming back after 40 minutes away (sleep, lock or just out)
			Activity.Reset();
			Activity.Step(t, 5, false);
			Check("activity: back after 40 min away", Activity.Step(t.AddMinutes(40), 5, false) == "welcome");
			Check("activity: back after 10 min is nothing special", Activity.Step(t.AddMinutes(50), 5, false) == null);

			// a program used 2h10 today and then closed: celebrated once; a short one: nothing
			Activity.Reset();
			Activity.Track(111, "Excel", 130 * 60, t);
			Activity.Track(222, "Bloco de notas", 10 * 60, t);
			Check("activity: still open, nothing yet", Activity.EndedApp(t, pid => true) == null);
			string ended = Activity.EndedApp(t, pid => pid != 111);
			Check("activity: long program closed is celebrated", ended == "app|Excel|2h10", ended);
			Check("activity: short program closed is not", Activity.EndedApp(t, pid => false) == null);
			Activity.Reset();
		}

		static async Task PomodoroCycle()
		{
			Pomodoro.StartFocus();
			Check("pomodoro: focus started", Pomodoro.Current == Pomodoro.Phase.Focus && Pomodoro.Left.TotalMinutes > Pomodoro.FocusMinutes - 1);
			Check("pomodoro: the pets stay still while focusing", Pomodoro.HoldingPets && FormPet.Held);
			Check("pomodoro: more lengths to choose", Pomodoro.FocusChoices.Length >= 10 && Pomodoro.FocusChoices.Contains(90) && Pomodoro.Minutes(90) == "1 h 30 min" && Pomodoro.Minutes(60) == "1 h");
			Pomodoro.TogglePause();
			var left = Pomodoro.Left;
			await Task.Delay(1200);
			Check("pomodoro: paused time stands still", Pomodoro.Paused && Pomodoro.Left == left);
			Check("pomodoro: paused, the pets are free", !Pomodoro.HoldingPets);
			Pomodoro.TogglePause();
			for (int i = 1; i <= 4; i++)
			{
				Pomodoro.TestFinishPhase();   // focus → break
				var expected = i == 4 ? Pomodoro.Phase.LongBreak : Pomodoro.Phase.ShortBreak;
				Check($"pomodoro: break after focus {i}", Pomodoro.Current == expected, Pomodoro.Current.ToString());
				Pomodoro.TestFinishPhase();   // break → focus
				Check($"pomodoro: focus again after break {i}", Pomodoro.Current == Pomodoro.Phase.Focus, Pomodoro.Current.ToString());
			}
			Check("pomodoro: status text", Pomodoro.Status().Contains(":"));
			Pomodoro.Stop();
			Check("pomodoro: stopped, the pets are free", Pomodoro.Current == Pomodoro.Phase.Off && !Pomodoro.HoldingPets);
			await Task.Delay(300);
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
		}

		// ------------------------------------------------------------------ agenda

		static async Task QuietTime()
		{
			void CloseBubbles() { foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close(); }
			bool AnyBubble() => Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible);

			// focus: notices wait
			SpeechBubble.TestClearHeld();
			Pomodoro.StartFocus();
			await Task.Delay(200);
			CloseBubbles();
			SpeechBubble.Notify("lembrete de teste", 5);
			SpeechBubble.Notify("reunião de teste", 5);
			await Task.Delay(300);
			Check("focus: notices wait (no bubble)", !AnyBubble() && SpeechBubble.HeldCount == 2, SpeechBubble.HeldCount.ToString());
			Check("focus: pet sounds off", SpeechBubble.QuietNow);
			SpeechBubble.Say("pedido do usuário", 3);
			await Task.Delay(200);
			Check("focus: what the user asks still answers", AnyBubble());
			CloseBubbles();
			Pomodoro.Stop();
			await Task.Delay(300);
			CloseBubbles();
			await Task.Delay(7500);
			var summary = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible);
			Check("focus over: summary of what arrived", summary != null);
			CloseBubbles();

			// Ctrl+Alt+P: silent too (before: the notices became Windows notifications)
			HideHotkey.SetHidden(true);
			SpeechBubble.Notify("aviso escondido", 5);
			await Task.Delay(200);
			Check("hidden: notices wait, nothing on screen", SpeechBubble.HeldCount >= 1 && !AnyBubble());
			HideHotkey.SetHidden(false);
			await Task.Delay(7600);
			Check("shown again: summary of what arrived", AnyBubble());
			CloseBubbles();
		}

		static async Task DownloadsWatcher()
		{
			string dir = Path.Combine(Path.GetTempPath(), "selftest-downloads");
			if (Directory.Exists(dir)) Directory.Delete(dir, true);
			Directory.CreateDirectory(dir);
			try
			{
				ComputerEvents.TestWatch(dir);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				// like a browser: first the unfinished file, then renamed to the final name
				File.WriteAllText(Path.Combine(dir, "relatorio.pdf.crdownload"), "x");
				await Task.Delay(300);
				File.Move(Path.Combine(dir, "relatorio.pdf.crdownload"), Path.Combine(dir, "relatorio.pdf"));
				await Task.Delay(4600);
				ComputerEvents.TestCheckDownloads();
				await Task.Delay(300);
				Check("downloads: finished file announced", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			}
			finally
			{
				ComputerEvents.TestWatch(null);
				try { Directory.Delete(dir, true); } catch (Exception) { }
			}
		}
	}
}
