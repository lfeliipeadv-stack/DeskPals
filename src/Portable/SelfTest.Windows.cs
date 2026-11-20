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
	// The self-test, part: windows. Check, Try and the list of tests are in SelfTest.cs.
	internal static partial class SelfTest
	{
		static void Languages()
		{
			foreach (string code in new[] { "pt-BR", "es" })
			{
				string file = Path.Combine(Lang.Folder, code + ".json");
				if (!File.Exists(file)) { Check($"{code}: file next to the program", false, file); continue; }
				var pt = JsonText.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
				var missing = Lang.Keys.Where(k => !pt.ContainsKey(k)).ToList();
				Check($"{code} has every text", missing.Count == 0, string.Join(", ", missing.Take(15)) + (missing.Count > 15 ? $" … ({missing.Count})" : ""));
				var extra = pt.Keys.Where(k => !Lang.Keys.Contains(k)).ToList();
				Check($"{code} has no unknown text", extra.Count == 0, string.Join(", ", extra.Take(15)));
				// placeholders: the same {n} in both languages
				var bad = Lang.Keys.Where(pt.ContainsKey).Where(k => Placeholders(pt[k]) != Placeholders(Lang.English(k))).ToList();
				Check($"{code} placeholders match", bad.Count == 0, string.Join(", ", bad.Take(10)));
			}
			Check("es: listed among the languages", Lang.Available().Any(a => a.Code == "es"));
			DownloadableLanguages();
		}

		/// <summary>
		/// The languages to download (Languages folder of the repository): every text, the same placeholders, listed in
		/// languages.json; then the whole way from that folder: the list, download, use, remove.
		/// </summary>
		static void DownloadableLanguages()
		{
			string dir = AppContext.BaseDirectory;
			while (dir != null && !File.Exists(Path.Combine(dir, "Languages", "languages.json"))) dir = Path.GetDirectoryName(dir);
			if (dir == null) { lines.Add("SKIP  languages to download: Languages folder not found"); return; }
			string folder = Path.Combine(dir, "Languages");
			var files = Directory.GetFiles(folder, "*.json").Where(f => Path.GetFileName(f) != "languages.json").ToList();
			Check("languages to download: at least 20", files.Count >= 20, files.Count.ToString());
			foreach (var file in files)
			{
				string code = Path.GetFileNameWithoutExtension(file);
				Dictionary<string, string> t;
				try { t = JsonText.Deserialize<Dictionary<string, string>>(File.ReadAllText(file, Encoding.UTF8)); }
				catch (Exception ex) { Check($"{code} (download): valid JSON", false, ex.Message); continue; }
				var missing = Lang.Keys.Where(k => !t.ContainsKey(k) || string.IsNullOrWhiteSpace(t[k])).ToList();
				var extra = t.Keys.Where(k => !Lang.Keys.Contains(k)).ToList();
				var bad = Lang.Keys.Where(t.ContainsKey).Where(k => Placeholders(t[k]) != Placeholders(Lang.English(k))).ToList();
				Check($"{code} (download): every text, no unknown, placeholders match", missing.Count == 0 && extra.Count == 0 && bad.Count == 0,
					$"missing {missing.Count} ({string.Join(", ", missing.Take(5))}), unknown {extra.Count}, placeholders {string.Join(", ", bad.Take(5))}");
				Check($"{code} (download): its own name", t.TryGetValue("language.name", out var n) && !string.IsNullOrWhiteSpace(n) && n != "English");
			}

			string baseUrl = LanguageStore.BaseUrl, languageWas = Program.MyData.GetLanguage();
			LanguageStore.BaseUrl = folder;
			string code0 = "fr";
			bool hadIt = LanguageStore.IsDownloaded(code0);
			try
			{
				var catalog = LanguageStore.CatalogAsync().GetAwaiter().GetResult();
				Check("languages to download: languages.json lists every file", files.All(f => catalog.Any(c => c.Code == Path.GetFileNameWithoutExtension(f))) && catalog.Count == files.Count,
					$"{catalog.Count} listed, {files.Count} files");
				var item = catalog.FirstOrDefault(c => c.Code == code0);
				if (item == null) { Check("languages to download: French listed", false); return; }
				LanguageStore.DownloadAsync(item).GetAwaiter().GetResult();
				Check("download: the file is in the user's lang folder", LanguageStore.IsDownloaded(code0));
				Check("download: listed among the languages", Lang.Available().Any(a => a.Code == code0));
				Check("download: not offered as an update right after", !LanguageStore.HasUpdate(item));
				LanguageStore.Use(code0);
				Check("download: the program speaks it", Lang.Current == code0 && Lang.T("langdl.close") != Lang.English("langdl.close"), Lang.Current + ": " + Lang.T("langdl.close"));
				if (!hadIt)
				{
					LanguageStore.Remove(code0);
					Check("remove: gone from the list", !LanguageStore.IsDownloaded(code0) && !Lang.Available().Any(a => a.Code == code0));
					Check("remove: the language in use goes back to automatic", Program.MyData.GetLanguage() == "auto");
				}
				// a broken file is refused and nothing is written
				string broken = Path.Combine(Path.GetTempPath(), "dp-lang-test");
				Directory.CreateDirectory(broken);
				File.WriteAllText(Path.Combine(broken, "xx.json"), "{ \"not\": \"a language\" }");
				LanguageStore.BaseUrl = broken;
				bool refused = false;
				try { LanguageStore.DownloadAsync(new LanguageStore.Item { Code = "xx", Name = "xx" }).GetAwaiter().GetResult(); }
				catch (Exception) { refused = true; }
				Check("download: a file that is not a language is refused", refused && !LanguageStore.IsDownloaded("xx"));
				// no internet: the list says so, the program goes on
				LanguageStore.BaseUrl = "https://nowhere.invalid/";
				bool failed = false;
				try { Task.Run(() => LanguageStore.CatalogAsync()).GetAwaiter().GetResult(); } catch (Exception) { failed = true; }   // off the UI thread: no deadlock
				Check("languages to download: no connection gives an error, no crash", failed);
			}
			finally
			{
				LanguageStore.BaseUrl = baseUrl;
				Program.MyData.SetLanguage(languageWas);
				Lang.Use(languageWas);
			}
		}

		/// <summary>Every window with a text box keeps the pets still while it is open.</summary>
		static async Task DialogsHold()
		{
			foreach (var t in new[] { typeof(FormAsk), typeof(FormAiSettings), typeof(FormLaunchers), typeof(FormReminder) })
				Check($"dialog holds the pets: {t.Name}", typeof(PetDialog).IsAssignableFrom(t));
			var pet = Pet();
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			await Task.Delay(200);
			pet?.React("walk");
			await Task.Delay(1700);
			// a pet still walking in from the edge of the screen is not stopped half outside (on purpose): wait until it is in
			for (int i = 0; i < 60 && pet != null && !Screen.FromControl(pet).WorkingArea.Contains(pet.Bounds); i++) await Task.Delay(100);
			bool inside = pet == null || Screen.FromControl(pet).WorkingArea.Contains(pet.Bounds);
			using (var d = new FormLaunchers())
			{
				d.Show();
				await Task.Delay(600);
				Check("dialog open: pets held", FormPet.Held);
				if (pet != null)
				{
					int x1 = pet.Left;
					await Task.Delay(500);
					if (!inside) lines.Add("SKIP  dialog open: the pet does not walk (it is still coming in from the edge of the screen)");
					else Check("dialog open: the pet does not walk", pet.Left == x1 || pet.StillAnimation < 0, $"{pet.CurrentAnimationName}, moved {pet.Left - x1}px");
				}
				d.Close();
			}
			await Task.Delay(200);
			Check("dialog closed: pets free again", !FormPet.Held);
		}

		/// <summary>The settings window: every page draws, the cards switch features, old pages lead to new ones.</summary>
		static void SettingsPages()
		{
			foreach (var (name, ok, detail) in FormSettings.SelfCheck()) Check("settings: " + name, ok, detail);
			int before = DayStats.Get("selftest");
			DayStats.Add("selftest", 2);
			Check("today counters: added", DayStats.Get("selftest") == before + 2);
		}

		static async Task Onboard()
		{
			bool done = Program.MyData.GetSettingBool("TourDone", false);
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			Onboarding.Tour();
			Check("tour: the pet offers to show what it does", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
			Check("tour: the pets stay still during the tour", Onboarding.Holding && FormPet.Held);
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			Check("tour: closing it does not mark it as seen", Program.MyData.GetSettingBool("TourDone", false) == done);
			await Task.Delay(2600);
			Check("tour: bubbles gone, the pets are let go", !Onboarding.Holding);
			Onboarding.StopHolding();
			// the text of this version, or of the next one (the version number goes up when it is released)
			var cur = Updater.CurrentVersion;
			Check("what's new: text for this (or the next) version", Lang.Keys.Any(k => k.StartsWith("whatsnew.") && Version.TryParse(k.Substring(9), out var v) && v >= new Version(cur.Major, cur.Minor, cur.Build)), cur.ToString(3));
			// the ask shortcut: when one is taken by another program, another one can be chosen and works
			bool wasOn = HideHotkey.AskEnabled; int wasKey = HideHotkey.AskKey;
			try
			{
				Program.MyData.SetSetting("AskHotkey", true);
				var free = Enumerable.Range(0, HideHotkey.AskKeyCount).Where(k => { HideHotkey.AskKey = k; return !HideHotkey.AskBusy; }).ToList();
				var taken = Enumerable.Range(0, HideHotkey.AskKeyCount).Except(free).Select(HideHotkey.AskKeyText).ToList();
				Check("hotkey: at least one ask shortcut is free", free.Count > 0, "taken: " + string.Join(", ", taken));
				if (taken.Count > 0) lines.Add("INFO  hotkey: taken by another program here: " + string.Join(", ", taken));
				Check("hotkey: names in the language", HideHotkey.AskKeyText(0).EndsWith(Lang.T("key.space")), HideHotkey.AskKeyText(0));
			}
			finally { Program.MyData.SetSetting("AskHotkey", wasOn); HideHotkey.AskKey = wasKey; }
		}

		static void Backup_Export()
		{
			string file = Path.Combine(Path.GetTempPath(), "selftest" + PetBackup.Extension);
			int n = PetBackup.Export(file);
			using (var zip = ZipFile.OpenRead(file))
			{
				int xmls = zip.Entries.Count(e => e.FullName.EndsWith("/animations.xml"));
				Check("export: every downloaded pet", xmls == n && n == PetStore.Folders().Count, $"{xmls} in zip, {n} exported, {PetStore.Folders().Count} on disk");
				Check("export: settings inside", zip.GetEntry("settings.txt") != null);
			}
			File.Delete(file);
		}

		static void Backup_ImportSafety()
		{
			string file = Path.Combine(Path.GetTempPath(), "selftest-bad" + PetBackup.Extension);
			if (File.Exists(file)) File.Delete(file);
			using (var zip = ZipFile.Open(file, ZipArchiveMode.Create))
			{
				void Add(string name, string text) { using (var w = new StreamWriter(zip.CreateEntry(name).Open())) w.Write(text); }
				Add("pets/../evil/animations.xml", "<animations><image/></animations>");
				Add("pets/not a name!/animations.xml", "<animations><image/></animations>");
				Add("pets/broken/animations.xml", "this is not xml");
				Add("pets/selftest_ok/animations.xml", "<animations xmlns=\"https://esheep.petrucci.ch/\"><image/><animations/></animations>");
				Add("settings.txt", "current=selftest_ok\nmuted=selftest_ok;esheep64_not_imported");
			}
			bool esheepMutedBefore = Program.MyData.IsPetMuted("esheep64");
			var (imported, current) = PetBackup.Import(file);
			Check("import: only the valid pet, nothing outside the store", imported.Count == 1 && imported[0] == "selftest_ok"
				&& !Directory.Exists(Path.Combine(Path.GetTempPath(), "evil")), string.Join(",", imported));
			Check("import: pet in use of the other computer", current == "selftest_ok");
			Check("import: sound setting of the imported pet", Program.MyData.IsPetMuted("selftest_ok"));
			// clean up
			PetStore.Delete("selftest_ok");
			Program.MyData.SetPetMuted("selftest_ok", false);
			Check("import: other settings untouched", Program.MyData.IsPetMuted("esheep64") == esheepMutedBefore);
			File.Delete(file);
		}

		// ------------------------------------------------------------------ windows

		static void BackupMyThings()
		{
			string file = Path.Combine(Path.GetTempPath(), "selftest-full" + PetBackup.Extension);
			// a preference of several lines and with a backslash must come back the same
			string multi = "line 1\nline 2 \\ end", keepMulti = Program.MyData.GetSetting("SelftestMulti", null);
			Program.MyData.SetSetting("SelftestMulti", multi);
			try
			{
				PetBackup.Export(file);
				using (var zip = ZipFile.OpenRead(file))
				{
					var settings = new StreamReader(zip.GetEntry("settings.txt").Open()).ReadToEnd();
					Check("backup: settings inside", settings.Contains("setx."), settings.Length.ToString());
					Check("backup: every preference (not a short list)", settings.Contains("setx.SelftestMulti=") && settings.Contains("setx.Theme=") || !Program.MyData.SettingKeys().Contains("Theme"));
					Check("backup: no Claude Code secret", !settings.Contains("AgentToken") && !settings.Contains(AgentBridge.Token));
					Check("backup: no AI key, no pet data, nothing of this computer",
						!settings.Contains("setx.AiKey=") && !settings.Contains("setx.AiProfiles=") && !settings.Contains("setx.xml=") && !settings.Contains("setx.BackupFolder=") && !settings.Contains("setx.UpdateChecked="));
					Check("backup: reminders inside (when there are any)", !File.Exists(Reminders.FilePath) || zip.GetEntry("data/reminders.json") != null);
					Check("backup: notes inside (when there are any)", !File.Exists(Notes.FilePath) || zip.GetEntry("data/notes.json") != null);
				}
				Check("backup: recognized as full", PetBackup.HasMyThings(file));
				Check("backup: secrets never carried", !PetBackup.Carried("AiKey") && !PetBackup.Carried("AgentToken") && !PetBackup.Carried("SomeApiKey") && !PetBackup.Carried("../x") && PetBackup.Carried("PomodoroFocus"));
				Program.MyData.SetSetting("SelftestMulti", "changed");
				PetBackup.RestoreMyThings(file, dataFiles: false);   // only the settings: the data of this computer stays as it is
				Check("backup: a preference of several lines comes back the same", Program.MyData.GetSetting("SelftestMulti") == multi, Program.MyData.GetSetting("SelftestMulti"));
			}
			finally { Program.MyData.SetSetting("SelftestMulti", keepMulti ?? ""); File.Delete(file); }

			// the automatic copy: written into the chosen folder, then off again
			string keepFolder = PetBackup.AutoFolder, keepLast = Program.MyData.GetSetting("BackupLast");
			string dir = Path.Combine(Path.GetTempPath(), "selftest-autobackup");
			Directory.CreateDirectory(dir);
			try
			{
				PetBackup.AutoFolder = dir;
				Check("automatic backup: written in the folder", File.Exists(PetBackup.AutoFile) && PetBackup.HasMyThings(PetBackup.AutoFile), PetBackup.AutoFile);
				PetBackup.AutoFolder = Path.Combine(dir, "missing");
				Check("automatic backup: a missing folder only fails", !PetBackup.RunAuto());
			}
			finally
			{
				Program.MyData.SetSetting("BackupFolder", keepFolder);
				Program.MyData.SetSetting("BackupLast", keepLast);
				try { Directory.Delete(dir, true); } catch (Exception) { }
			}

			// "What's new": the notes saved by the update are shown once, only by that version
			string wn = Updater.WhatsNewFile, keepWn = File.Exists(wn) ? File.ReadAllText(wn) : null;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(wn));
				File.WriteAllText(wn, "9.8.7\n## Novidades\n- algo");
				Check("what's new: another version shows nothing", Updater.TakeWhatsNew("9.8.6") == null);
				File.WriteAllText(wn, "9.8.7\n## Novidades\n- algo");
				Check("what's new: the notes of this version", Updater.TakeWhatsNew("9.8.7")?.Contains("algo") == true);
				Check("what's new: only once", Updater.TakeWhatsNew("9.8.7") == null && !File.Exists(wn));
			}
			finally { if (keepWn != null) File.WriteAllText(wn, keepWn); }
		}

		// ------------------------------------------------------------------ Claude Code

		static async Task ShortcutError()
		{
			Launchers.Open(new Launcher { Name = "nada", Target = @"C:\this\does\not\exist.txt" });
			bool shown = false;
			for (int i = 0; i < 20 && !shown; i++) { await Task.Delay(100); shown = Application.OpenForms.OfType<SpeechBubble>().Any(); }
			var p = Pet();
			Check("shortcut: a missing file only shows a message", shown,
				p == null ? "no pet on screen (the message went to the tray)" : $"pet {p.Bounds}, opacity {p.Opacity}, hidden {HideHotkey.Hidden}");
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
		}
	}
}
