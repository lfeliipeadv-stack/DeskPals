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
	/// <summary>
	/// Developer check of the features that have no window of their own to look at:
	/// "DesktopPet.exe --selftest=C:\folder\result.txt" runs every check with the real code, writes PASS/FAIL lines
	/// and closes the program (exit code = number of failures). The user's reminders, needs, mood and shortcuts
	/// files are not touched; settings changed by a check are put back.
	/// </summary>
	internal static partial class SelfTest
	{
		[System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
		[System.Runtime.InteropServices.DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int index);

		static readonly List<string> lines = new List<string>();
		static int failures;
		static string resultFile = "";

		static void Check(string name, bool ok, string detail = "")
		{
			if (!ok) failures++;
			lines.Add((ok ? "PASS  " : "FAIL  ") + name + (ok || detail.Length == 0 ? "" : "  →  " + detail));
		}

		/// <summary>Developer: "--only=name" runs only the tests whose name contains it.</summary>
		public static string Only;

		static void Try(string name, Action test)
		{
			if (Only != null && name.IndexOf(Only, StringComparison.OrdinalIgnoreCase) < 0) return;
			Progress(name);
			try { test(); }
			catch (Exception ex) { Check(name, false, ex.GetType().Name + ": " + ex.Message); }
		}

		/// <summary>The self-test is running: the plugins that react to the computer on their own (music, visits, full screen) wait.</summary>
		public static bool Running;
		/// <summary>The live part of the self-test: those plugins run for real (a butterfly, a pushed window, full screen).</summary>
		public static bool Live;

		public static async void Run(string outFile)
		{
			Running = true;
			resultFile = outFile;
			await Task.Delay(3000);   // pets on screen, menus created

			Try("reminder schedule", Reminders_Next);
			Try("reminder json", Reminders_Json);
			Try("virtual pet decay", VirtualPet_Decay);
			Try("per-pet sound", PetSound);
			Try("languages", Languages);
			Try("games texts", Games);
			Try("mood history text", () => Check("mood: 14 days shown", Mood.Recent(14).Split(' ').Length == 14, Mood.Recent(14)));
			Try("start with Windows (read)", () => { bool _ = WindowsStartup.Enabled; Check("start with Windows: readable", true); });
			Try("backup export", Backup_Export);
			Try("backup import safety", Backup_ImportSafety);
			await TryAsync("bubble", Bubble);
			await TryAsync("hide hotkey", Hide);
			await TryAsync("pet menu", Menu);
			await TryAsync("pc monitor ram", PcMonitorRam);
			await TryAsync("pomodoro", PomodoroCycle);
			await TryAsync("shortcut error", ShortcutError);
			Try("agenda", AgendaParse);
			Try("pets meet", () =>
			{
				Check("pets meet: side by side", PetsMeet.Close(new System.Drawing.Rectangle(100, 500, 80, 80), new System.Drawing.Rectangle(150, 502, 80, 80)));
				Check("pets meet: far apart", !PetsMeet.Close(new System.Drawing.Rectangle(100, 500, 80, 80), new System.Drawing.Rectangle(400, 500, 80, 80)));
				Check("pets meet: one on a window above", !PetsMeet.Close(new System.Drawing.Rectangle(100, 500, 80, 80), new System.Drawing.Rectangle(120, 300, 80, 80)));
			});
			Try("downloads", () =>
			{
				Check("downloads: unfinished files ignored", ComputerEvents.IsUnfinished("Unconfirmed 1234.crdownload") && ComputerEvents.IsUnfinished("video.mp4.part") && ComputerEvents.IsUnfinished("desktop.ini"));
				Check("downloads: finished file noticed", !ComputerEvents.IsUnfinished("relatorio.pdf") && !ComputerEvents.IsUnfinished("setup.exe"));
				Check("downloads: folder found", Directory.Exists(ComputerEvents.DownloadsFolder()), ComputerEvents.DownloadsFolder());
			});
			await TryAsync("ask the pet", AskThePet);
			await TryAsync("ai live", AiLive);
			Try("pet actions", PetActionNames);
			await TryAsync("dialogs hold the pets", DialogsHold);
			Try("colour emoji", ColorEmoji);
			Try("sheep talk", SheepTalk);
			Try("smart reminders", SmartReminders);
			Try("activity", ActivityCare);
			Try("settings pages", SettingsPages);
			Try("ai providers", AiProviders);
			await TryAsync("chat", ChatBox);
			await TryAsync("hover chat", HoverChat);
			await TryAsync("simple timer", TimerCheck);
			await TryAsync("fun plugins", FunPlugins);
			await TryAsync("new tools", NewTools);
			await TryAsync("weather", WeatherCheck);
			await TryAsync("pets chat", PetsChat);
			await TryAsync("sky arrival", SkyArrival);
			await TryAsync("pet change", PetChangeHello);
			await TryAsync("floating pet change", FloatingPetChange);
			await TryAsync("anim picker", PickerScenes);
			await TryAsync("gsheep picker", GSheepPicker);
			await TryAsync("gsheep dive", GSheepDives);
			await TryAsync("gsheep ground bath", GSheepGroundBath);
			await TryAsync("gsheep abduction", GSheepAbduction);
			Try("picker all pets", PickerAllPets);
			Try("sounds all pets", SoundsAllPets);
			Try("pets deep", PetsDeep);
			await TryAsync("pet safety", PetSafety);
			await TryAsync("gallery features", GalleryFeatures);
			await TryAsync("pet editor", PetEditorOpen);
			await TryAsync("plugins 1.1", Plugins11);
			await TryAsync("weather extension", WeatherExtensionTests);
			await TryAsync("pets live", PetsLive);
			await TryAsync("tour", Onboard);
			await TryAsync("talking pet", TalkingPet);
			await TryAsync("quiet time", QuietTime);
			await TryAsync("agenda online", AgendaOnline);
			await TryAsync("downloads watcher", DownloadsWatcher);
			Try("mcp", Mcp);
			Try("backup of my things", BackupMyThings);
			Try("log file", () =>
			{
				StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "selftest log line");
				Check("log: warnings saved to disk", File.Exists(LogFile.Today) && File.ReadAllText(LogFile.Today).Contains("selftest log line"), LogFile.Today);
			});
			Try("claude hooks file", ClaudeHooksFile);
			Try("claude events", ClaudeEvents);
			await TryAsync("claude endpoint", ClaudeEndpoint);
			Try("new plugins", NewPlugins);
			await TryAsync("live plugins", LivePlugins);
			await TryAsync("achievements", AchievementsCheck);
			await TryAsync("media controls", MediaControls);
			await TryAsync("new pets", NewPetsCheck);
			await TryAsync("source filter", SourceFilterCheck);
			await TryAsync("interact", InteractCheck);
			await TryAsync("calm and visits", CalmAndVisitsCheck);
			await TryAsync("tools extension", ToolsCheck);
			await TryAsync("tools watch", ToolsWatchCheck);

			lines.Insert(0, $"DesktopPet self-test {DateTime.Now:yyyy-MM-dd HH:mm:ss}  —  {failures} failure(s)");
			try { File.WriteAllLines(outFile, lines, new UTF8Encoding(false)); } catch (Exception) { }
			Environment.Exit(failures);
		}

		/// <summary>
		/// Developer: "--ai-check=result.txt" with the key in the environment variable DESKTOPPET_AI_KEY (never in a file)
		/// and the service in DESKTOPPET_AI_PROVIDER (default gemini): the real service, the real code, no settings changed.
		/// </summary>
		public static async void AiCheck(string outFile)
		{
			var o = new List<string>();
			try
			{
				string key = Environment.GetEnvironmentVariable("DESKTOPPET_AI_KEY") ?? "";
				var p = AiAssistant.Preset(Environment.GetEnvironmentVariable("DESKTOPPET_AI_PROVIDER") ?? "gemini");
				var models = await AiAssistant.ListModels(p.BaseUrl, key);
				o.Add($"models: {models.Count}");
				var sw = System.Diagnostics.Stopwatch.StartNew();
				string m = await AiAssistant.FindWorkingModel(p, p.BaseUrl, key, p.Model, models);
				o.Add($"working model: {m} ({sw.ElapsedMilliseconds} ms)");
				// what happened before the fix: an old saved model the service no longer serves
				string old = await AiAssistant.FindWorkingModel(p, p.BaseUrl, key, "gemini-2.5-flash", models);
				o.Add($"from an old saved model it moves to: {old}");
				sw.Restart();
				string answer = await AiAssistant.Ask(p.BaseUrl, m, key, "Em uma frase: por que o céu é azul?", "Rex");
				o.Add($"answer ({sw.ElapsedMilliseconds} ms): {answer}");
			}
			catch (Exception ex) { o.Add("ERROR " + ex.GetType().Name + ": " + ex.Message); }
			try { File.WriteAllLines(outFile, o, new UTF8Encoding(false)); } catch (Exception) { }
			Environment.Exit(0);
		}

		static async Task TryAsync(string name, Func<Task> test)
		{
			if (Only != null && name.IndexOf(Only, StringComparison.OrdinalIgnoreCase) < 0) return;
			Progress(name);
			try
			{
				// a test that never ends would hold the whole self-test (and write no result): after 3 minutes it fails
				Task run = test();
				if (await Task.WhenAny(run, Task.Delay(TimeSpan.FromMinutes(3))) != run) Check(name, false, "did not finish in 3 minutes");
				else await run;
			}
			catch (Exception ex) { Check(name, false, ex.GetType().Name + ": " + ex.Message); }
		}

		static bool heldNoted;

		/// <summary>"result.txt.progress": the test running now, to know where a self-test stopped.</summary>
		static void Progress(string name)
		{
			// a hold left by the test before (a bubble or a window that never let go) keeps every pet still in the next ones
			if (FormPet.Held && !heldNoted) { heldNoted = true; lines.Add("INFO  pets already held when \"" + name + "\" started"); }
			else if (!FormPet.Held) heldNoted = false;
			try { File.WriteAllText(resultFile + ".progress", DateTime.Now.ToString("HH:mm:ss") + "  " + name + "  (" + lines.Count + " checks so far, " + failures + " failure(s))"); } catch (Exception) { }
		}

		// ------------------------------------------------------------------ reminders

		/// <summary>Falling from a window that went away: a pet in the air cannot stand still until it lands.</summary>
		static bool InAir(FormPet pet) => (pet.CurrentAnimationName ?? "").IndexOf("fall", StringComparison.OrdinalIgnoreCase) >= 0;

		/// <summary>
		/// Leaves the pet walking on the ground, fully inside the screen: that is the only state in which the menu
		/// can stop it (a pet still arriving from the sky, in a scene like the bath, or half out of the screen goes on).
		/// </summary>
		static async Task<bool> PetWalkingOnGround(FormPet pet)
		{
			var anims = Program.Mainthread.GetAnimations()?.SheepAnimations;
			int walk = anims == null ? -1
				: anims.Where(kv => kv.Value.Name != null && kv.Value.Name.StartsWith("walk"))
					  .Select(kv => kv.Key).FirstOrDefault(-1);
			for (int i = 0; i < 50; i++)
			{
				var area = Screen.FromControl(pet).WorkingArea;
				string name = pet.CurrentAnimationName ?? "";
				bool walking = name.StartsWith("walk");
				if (pet.OnGround && area.Contains(pet.Bounds) && (walking || walk < 0)) return true;
				// it may be in a scene (the bath), standing still, or half out of the screen: it starts walking again
				// (a pet in the air is left alone: it has to land first; a walking pet is left to walk, so that it
				// can go back in from the edge of the screen)
				if (walk >= 0 && !InAir(pet) && !walking) pet.TestPlay(walk);
				await Task.Delay(200);
			}
			return false;
		}

		/// <summary>A picture of part of the screen next to the result file (to look at the windows).</summary>
		static void Shot(string name, Rectangle r)
		{
			try
			{
				r = Rectangle.Inflate(r, 24, 24);
				using (var b = new Bitmap(r.Width, r.Height))
				{
					using (var g = Graphics.FromImage(b)) g.CopyFromScreen(r.Location, Point.Empty, r.Size);
					b.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(resultFile)) ?? ".", "shot-" + name + ".png"));
				}
			}
			catch (Exception) { }
		}

		static FormPet Pet() => Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow);

		/// <summary>Why the program keeps quiet now (no bubbles, pets do not chat), or null.</summary>
		static string QuietReason()
		{
			if (!SpeechBubble.QuietNow) return null;
			var why = new List<string>();
			if (HideHotkey.Hidden) why.Add("pets hidden");
			if (Pomodoro.Current == Pomodoro.Phase.Focus && !Pomodoro.Paused) why.Add("focus time");
			if (FullScreenWatch.Dnd) why.Add("a program in full screen");
			if (NoticeOptions.InQuietHours) why.Add("quiet hours");
			if (DoNotDisturb.Active) why.Add("do not disturb");
			return why.Count > 0 ? string.Join(", ", why) : "quiet";
		}

		static string Placeholders(string s) => string.Join(",", System.Text.RegularExpressions.Regex.Matches(s ?? "", @"\{(\d+)").Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x));
	}
}
