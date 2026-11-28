using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of the Explorer menu (in a sandbox key of HKCU and a "Send to" folder of the test, never the real ones) and
	/// of the gathering of the copies Explorer starts (the program itself started three times).
	/// </summary>
	internal static class ExplorerTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("explorer menu", Menu);
			DevTests.Add("explorer gather", Gather);
			DevTests.Add("explorer gather many", GatherMany);
		}

		static Task Menu(DevTests.Ctx t)
		{
			string oldRoot = ExplorerMenu.Root, oldSendTo = ExplorerMenu.SendToFolder;
			string sandbox = @"Software\DeskPalsToolsTest-" + Environment.ProcessId;
			ExplorerMenu.Root = sandbox;
			ExplorerMenu.SendToFolder = Path.Combine(t.Dir, "SendTo");
			Directory.CreateDirectory(ExplorerMenu.SendToFolder);
			try
			{
				// the program's own path (it has spaces on most PCs: "C:\Users\Some Name\…")
				string exe = Environment.ProcessPath;
				ExplorerMenu.Register(exe);
				using (var files = Registry.CurrentUser.OpenSubKey(sandbox + @"\*\shell\DeskPalsTools"))
				{
					t.Check("menu: the files' submenu is there", files != null);
					if (files != null)
					{
						t.Check("menu: its name", files.GetValue("MUIVerb") as string == T.S("app"));
						t.Check("menu: it is a cascading menu (SubCommands empty)", files.GetValue("SubCommands") is string sc && sc.Length == 0);
						t.Check("menu: its icon is the program", files.GetValue("Icon") as string == "\"" + exe + "\",0", files.GetValue("Icon") as string);
						var names = files.OpenSubKey("shell")?.GetSubKeyNames().OrderBy(n => n, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
						t.Check("menu: the six items, in order", names.SequenceEqual(new[] { "1topdf", "2merge", "3ocr", "4smaller", "5zip", "6open" }), string.Join(",", names));
						foreach (var id in new[] { "topdf", "merge", "ocr", "smaller", "zip", "open" })
						{
							using var item = files.OpenSubKey(@"shell\" + names.FirstOrDefault(n => n.EndsWith(id)));
							string cmd = item?.OpenSubKey("command")?.GetValue("") as string;
							string want = "\"" + exe + "\" --from-explorer --do=" + id + " \"%1\"";
							t.Check("menu: " + id + " runs the program, quoted", cmd == want, cmd + "  ≠  " + want);
							t.Check("menu: " + id + " has its text", item?.GetValue("MUIVerb") as string == (id == "open" ? T.S("explorer.open") : T.S("job." + id)));
						}
					}
				}
				using (var folders = Registry.CurrentUser.OpenSubKey(sandbox + @"\Directory\shell\DeskPalsTools"))
				{
					var names = folders?.OpenSubKey("shell")?.GetSubKeyNames().OrderBy(n => n, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
					t.Check("menu: folders get ZIP and Open", names.SequenceEqual(new[] { "5zip", "6open" }), string.Join(",", names));
					string cmd = folders?.OpenSubKey(@"shell\5zip\command")?.GetValue("") as string;
					t.Check("menu: a folder's ZIP command", cmd == "\"" + exe + "\" --from-explorer --do=zip \"%1\"", cmd);
				}
				string lnk = Path.Combine(ExplorerMenu.SendToFolder, "DeskPals Tools.lnk");
				t.Check("send to: the shortcut is there", File.Exists(lnk));
				t.Check("send to: it opens the program", string.Equals(ShellLink.Target(lnk), exe, StringComparison.OrdinalIgnoreCase), ShellLink.Target(lnk));
				t.Check("menu: up to date for this program", ExplorerMenu.IsCurrent(exe));

				// the program moved (DeskPals installed it elsewhere): at start the menu follows it
				string old = Path.Combine(t.Dir, "old place", "DeskPalsTools.exe");
				ExplorerMenu.Register(old);
				t.Check("moved: the menu sees it is old", !ExplorerMenu.IsCurrent(exe));
				ToolSettings.Set(ExplorerMenu.SettingKey, true);
				ExplorerMenu.Refresh(exe);
				t.Check("moved: written again for the new place", ExplorerMenu.IsCurrent(exe));
				t.Check("moved: the Send to shortcut follows", string.Equals(ShellLink.Target(lnk), exe, StringComparison.OrdinalIgnoreCase), ShellLink.Target(lnk));

				// the user deleted the Send to shortcut but keeps the menu: it is not brought back
				File.Delete(lnk);
				ExplorerMenu.Refresh(exe);
				t.Check("own choice: a deleted Send to shortcut stays deleted", !File.Exists(lnk));
				ExplorerMenu.Register(old);
				File.Delete(lnk);
				ExplorerMenu.Refresh(exe);
				t.Check("own choice: the menu still follows the program", ExplorerMenu.IsCurrent(exe) && !File.Exists(lnk));

				// a copy started by Explorer may speak another language than DeskPals: it only fixes the place, not the texts
				using (var k = Registry.CurrentUser.OpenSubKey(sandbox + @"\*\shell\DeskPalsTools", true)) k?.SetValue("MUIVerb", "Another language");
				t.Check("language: the place alone is still right", ExplorerMenu.IsCurrent(exe, placeOnly: true) && !ExplorerMenu.IsCurrent(exe));
				ExplorerMenu.Refresh(exe, placeOnly: true);
				using (var k = Registry.CurrentUser.OpenSubKey(sandbox + @"\*\shell\DeskPalsTools"))
					t.Check("language: a copy from Explorer leaves the texts alone", k?.GetValue("MUIVerb") as string == "Another language");
				ExplorerMenu.Refresh(exe);
				t.Check("language: the window start writes them again", ExplorerMenu.IsCurrent(exe));
				ExplorerMenu.Register(exe);
				ToolSettings.Set(ExplorerMenu.SettingKey, false);
				ExplorerMenu.Register(old);
				ExplorerMenu.Refresh(exe);
				t.Check("moved: switch off → left alone", !ExplorerMenu.IsCurrent(exe));

				// the switch
				var pref = Prefs.All().FirstOrDefault(p => p.Key == ExplorerMenu.SettingKey);
				t.Check("switch: in the settings", pref != null && pref.Get != null && pref.Set != null);
				ExplorerMenu.Turn(true, exe);
				t.Check("switch on: registered and saved", ExplorerMenu.IsCurrent(exe) && pref?.Get() == true);
				ExplorerMenu.Turn(false, exe);
				t.Check("switch off: saved", pref?.Get() == false);
				t.Check("switch off: no files' submenu", Registry.CurrentUser.OpenSubKey(sandbox + @"\*\shell\DeskPalsTools") == null);
				t.Check("switch off: no folders' submenu", Registry.CurrentUser.OpenSubKey(sandbox + @"\Directory\shell\DeskPalsTools") == null);
				t.Check("switch off: no shortcut", !File.Exists(lnk));
				// the sandbox had nothing: the "*" and "Directory" keys made on the way are gone too
				t.Check("switch off: no empty keys left behind", Registry.CurrentUser.OpenSubKey(sandbox + @"\*") == null && Registry.CurrentUser.OpenSubKey(sandbox + @"\Directory") == null);
				// only our things go: anything else in the same place stays
				Registry.CurrentUser.CreateSubKey(sandbox + @"\*\shell\SomethingElse")?.Dispose();
				File.WriteAllText(Path.Combine(ExplorerMenu.SendToFolder, "Other.lnk"), "");
				ExplorerMenu.Register(exe);
				ExplorerMenu.Unregister();
				t.Check("off: other menus stay", Registry.CurrentUser.OpenSubKey(sandbox + @"\*\shell\SomethingElse") != null);
				t.Check("off: other shortcuts stay", File.Exists(Path.Combine(ExplorerMenu.SendToFolder, "Other.lnk")));
				t.Check("off: hint mentions Windows 11's \"more options\"", T.Read("en")["explorer.prefHint"].Contains("Show more options"));

				// a read-only shortcut with our name (copied with an old profile): replaced, and removed when turned off
				ExplorerMenu.Register(exe);
				File.SetAttributes(lnk, FileAttributes.ReadOnly);
				ExplorerMenu.Register(exe);
				t.Check("read-only: the shortcut is written again", string.Equals(ShellLink.Target(lnk), exe, StringComparison.OrdinalIgnoreCase));
				File.SetAttributes(lnk, FileAttributes.ReadOnly);
				string err = null;
				try { ExplorerMenu.Turn(false, exe); } catch (Exception ex) { err = ex.Message; }
				t.Check("read-only: turned off without a problem", err == null && !File.Exists(lnk), err);

				// turning on fails half-way ("Send to" cannot be written): nothing is left and the switch stays off
				string sendTo = ExplorerMenu.SendToFolder;
				string notAFolder = Path.Combine(t.Dir, "a file, not a folder");
				File.WriteAllText(notAFolder, "");
				ExplorerMenu.SendToFolder = notAFolder;
				err = null;
				try { ExplorerMenu.Turn(true, exe); } catch (Exception ex) { err = ex.Message; }
				t.Check("half-way: the problem is told", err != null);
				t.Check("half-way: no submenu left", Registry.CurrentUser.OpenSubKey(sandbox + @"\*\shell\DeskPalsTools") == null && Registry.CurrentUser.OpenSubKey(sandbox + @"\Directory\shell\DeskPalsTools") == null);
				t.Check("half-way: the switch stays off", !ToolSettings.Bool(ExplorerMenu.SettingKey));
				ExplorerMenu.SendToFolder = sendTo;
			}
			finally
			{
				try { foreach (var f in Directory.GetFiles(Path.Combine(t.Dir, "SendTo"))) File.SetAttributes(f, FileAttributes.Normal); } catch (Exception) { }
				Registry.CurrentUser.DeleteSubKeyTree(sandbox, false);
				ExplorerMenu.Root = oldRoot;
				ExplorerMenu.SendToFolder = oldSendTo;
			}
			t.Check("clean: the sandbox key is gone", Registry.CurrentUser.OpenSubKey(sandbox) == null);
			return Task.CompletedTask;
		}

		/// <summary>Like Explorer with three files selected: three copies at once → one job with the three files, by name.</summary>
		static async Task Gather(DevTests.Ctx t)
		{
			var lines = await Spawn(t, t.Dir, new[] { "c 10.txt", "a.txt", "c 9.txt" }, "gather");
			t.Check("gather: one job ran", lines.Length == 1, string.Join(" / ", lines));
			t.Check("gather: with the three files, sorted by name", lines.FirstOrDefault() == "a.txt|c 9.txt|c 10.txt", lines.FirstOrDefault());

			// the order alone
			var sorted = ExplorerGather.Sorted(new[] { @"C:\x\b.pdf", @"C:\x\page 10.pdf", @"C:\x\page 2.pdf", @"C:\x\B.PDF" });
			t.Check("gather: Explorer's order, each file once", sorted.Select(Path.GetFileName).SequenceEqual(new[] { "b.pdf", "page 2.pdf", "page 10.pdf" }), string.Join(",", sorted));
		}

		/// <summary>A big selection on a busy PC (50 copies at once start slowly): still ONE job with every file.</summary>
		static async Task GatherMany(DevTests.Ctx t)
		{
			var names = Enumerable.Range(1, 50).Select(i => "page " + i + ".txt").ToArray();
			var lines = await Spawn(t, t.Dir, names.Reverse().ToArray(), "many");
			t.Check("many: one job ran", lines.Length == 1, lines.Length + " jobs: " + string.Join(" / ", lines.Select(l => l.Split('|').Length + " files")));
			t.Check("many: with the 50 files, in order", lines.FirstOrDefault() == string.Join("|", names), lines.FirstOrDefault());
		}

		/// <summary>Starts the program once per file, like Explorer does, and gives back the record of the jobs that ran
		/// (one line per job: its files).</summary>
		static async Task<string[]> Spawn(DevTests.Ctx t, string dir, string[] names, string what)
		{
			var paths = names.Select(n => { string p = Path.Combine(dir, n); File.WriteAllText(p, n); return p; }).ToList();
			var procs = new List<Process>();
			try
			{
				foreach (var p in paths)
				{
					var psi = new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false };
					foreach (var a in new[] { "--from-explorer", "--quiet", "--do=" + ExplorerHook.TestJob, p }) psi.ArgumentList.Add(a);
					psi.Environment[ExplorerMenu.TestVariable] = dir;
					procs.Add(Process.Start(psi));
				}
				var codes = new List<int>();
				foreach (var pr in procs)
				{
					using var cts = new CancellationTokenSource(120000);
					try { await pr.WaitForExitAsync(cts.Token); codes.Add(pr.ExitCode); }
					catch (OperationCanceledException) { try { pr.Kill(); } catch (Exception) { } codes.Add(-1); }
				}
				t.Check(what + ": every copy ended well", codes.All(c => c == 0), string.Join(",", codes));
			}
			finally { foreach (var pr in procs) pr.Dispose(); }
			string record = Path.Combine(dir, "record.txt");
			return File.Exists(record) ? File.ReadAllLines(record).Where(l => l.Length > 0).ToArray() : Array.Empty<string>();
		}
	}
}
