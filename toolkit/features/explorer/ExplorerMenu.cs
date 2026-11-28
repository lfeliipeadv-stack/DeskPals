using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DeskPalsTools
{
	/// <summary>
	/// "DeskPals Tools" in Windows Explorer, for this user only (no admin): a submenu in the right-click menu of files
	/// (and of folders, for the ZIP) under HKCU\Software\Classes, and a shortcut in "Send to". Each item runs
	/// "&lt;exe&gt;" --from-explorer --do=&lt;id&gt; "%1" (Windows starts one per selected file: <see cref="ExplorerGather"/>
	/// puts them back together). The switch in the window's settings turns it on and off; at start it follows the program
	/// when it moved (<see cref="Refresh"/>). <see cref="Root"/> and <see cref="SendToFolder"/> can point elsewhere, so the
	/// tests use a sandbox and never the real menu.
	/// </summary>
	internal static class ExplorerMenu
	{
		/// <summary>The key under HKEY_CURRENT_USER where the menu goes (the tests use a key of their own).</summary>
		public static string Root = @"Software\Classes";
		/// <summary>The "Send to" folder (null: Windows' own, shell:sendto).</summary>
		public static string SendToFolder;

		public const string Verb = "DeskPalsTools";
		public const string SettingKey = "explorerMenu";
		/// <summary>Set in the processes the tests start, so they never move the real menu (see <see cref="Refresh"/>).</summary>
		public const string TestVariable = "DESKPALS_TOOLS_EXPLORER_TEST";

		/// <summary>The submenu of files, in order: (key name, job id). The digit keeps Windows' order (it sorts by key name).</summary>
		public static readonly (string key, string id)[] FileItems = { ("1topdf", "topdf"), ("2merge", "merge"), ("3ocr", "ocr"), ("4smaller", "smaller"), ("5zip", "zip"), ("6open", "open") };
		/// <summary>The submenu of folders: only what makes sense for a folder.</summary>
		public static readonly (string key, string id)[] FolderItems = { ("5zip", "zip"), ("6open", "open") };

		/// <summary>The classes the submenu is added to: every file, and folders.</summary>
		public static string FilesKey => Root + @"\*\shell\" + Verb;
		public static string FoldersKey => Root + @"\Directory\shell\" + Verb;
		/// <summary>The "Send to" shortcut. Its name is fixed (not translated), so a change of language never leaves an old one behind.</summary>
		public static string Shortcut => Path.Combine(SendToFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.SendTo), "DeskPals Tools.lnk");

		/// <summary>The command of one item, quoted so paths with spaces work.</summary>
		public static string Command(string exe, string id) => "\"" + exe + "\" --from-explorer --do=" + id + Config() + " \"%1\"";
		
		/// <summary>
		/// DeskPals' settings file, when the program was started by DeskPals (a portable DeskPals keeps it next to itself):
		/// a copy started by Explorer then speaks DeskPals' language and uses its theme too.
		/// </summary>
		static string Config()
		{
			// the last one DeskPals gave is remembered: the program opened by itself (from Start) keeps the same menu
			string c = Settings.DeskPalsConfig;
			try
			{
				if (!string.IsNullOrEmpty(c) && File.Exists(c)) { if (ToolSettings.Text("deskpalsConfig") != c) ToolSettings.Set("deskpalsConfig", c); }
				else c = ToolSettings.Text("deskpalsConfig", null);
			}
			catch (Exception) { }
			return !string.IsNullOrEmpty(c) && File.Exists(c) && !c.Contains('"') ? " --config \"" + Path.GetFullPath(c) + "\"" : "";
		}

		/// <summary>The text of one item: the job's own name, or "Open DeskPals Tools".</summary>
		public static string ItemText(string id) => id == "open" ? T.S("explorer.open") : T.S("job." + id);

		[ModuleInitializer]
		internal static void Init()
		{
			Prefs.Register(new Prefs.Pref
			{
				Key = SettingKey, TextKey = "explorer.pref", HintKey = "explorer.prefHint", Order = 20,
				Get = () => ToolSettings.Bool(SettingKey),
				Set = on =>
				{
					try { Turn(on, Environment.ProcessPath); }
					catch (Exception ex) { MessageBox.Show(T.S("explorer.failed", Jobs.Friendly(ex)), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
				},
			});
		}

		/// <summary>
		/// The switch: on → the menu and "Send to" point to this program; off → everything removed. When turning on fails
		/// half-way, what was written is taken away again, so the menu is never left half there.
		/// </summary>
		public static void Turn(bool on, string exe)
		{
			if (on)
			{
				try { Register(exe); }
				catch (Exception)
				{
					try { Unregister(); } catch (Exception) { }
					throw;
				}
				ToolSettings.Set(SettingKey, true);
			}
			else
			{
				// saved first: even if a part cannot be removed now, it is not written again at the next start
				ToolSettings.Set(SettingKey, false);
				Unregister();
			}
		}

		/// <summary>
		/// At start: when the switch is on but the menu points to another place (the program moved) or speaks another
		/// language, it is written again. <paramref name="placeOnly"/>: only the place counts (a copy started by Explorer
		/// does not know DeskPals' language). The "Send to" shortcut is only moved along when it is there: one the user
		/// deleted stays deleted. Never in the processes the tests start.
		/// </summary>
		public static void Refresh(string exe = null, bool placeOnly = false)
		{
			try
			{
				if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TestVariable))) return;
				exe ??= Environment.ProcessPath;
				if (string.IsNullOrEmpty(exe) || !ToolSettings.Bool(SettingKey)) return;
				if (!IsCurrent(exe, placeOnly))
				{
					RemoveKeys();
					WriteKeys(exe);
					Changed();
				}
				string target = ShellLink.Target(Shortcut);
				if (target != null && !string.Equals(target, exe, StringComparison.OrdinalIgnoreCase)) WriteShortcut(exe);
			}
			catch (Exception) { }
		}

		/// <summary>Are both submenus there, pointing to this program (and, unless <paramref name="placeOnly"/>, in the current language)?</summary>
		public static bool IsCurrent(string exe, bool placeOnly = false) => Current(FilesKey, exe, FileItems, placeOnly) && Current(FoldersKey, exe, FolderItems, placeOnly);

		static bool Current(string path, string exe, (string key, string id)[] items, bool placeOnly)
		{
			using var verb = Registry.CurrentUser.OpenSubKey(path);
			if (verb == null || (!placeOnly && verb.GetValue("MUIVerb") as string != T.S("app"))) return false;
			foreach (var (key, id) in items)
			{
				using var item = verb.OpenSubKey(@"shell\" + key);
				using var cmd = item?.OpenSubKey("command");
				if (cmd?.GetValue("") as string != Command(exe, id)) return false;
				if (!placeOnly && item.GetValue("MUIVerb") as string != ItemText(id)) return false;
			}
			return true;
		}

		/// <summary>Writes the submenus and the "Send to" shortcut for this program (what was there before is replaced).</summary>
		public static void Register(string exe)
		{
			RemoveKeys();
			WriteKeys(exe);
			WriteShortcut(exe);
			Changed();
		}

		static void WriteKeys(string exe)
		{
			Write(FilesKey, exe, FileItems);
			Write(FoldersKey, exe, FolderItems);
		}

		static void WriteShortcut(string exe)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Shortcut));
			DeleteShortcut();
			ShellLink.Save(Shortcut, exe, "", T.S("explorer.sendToTip"));
		}

		/// <summary>Removes the submenus and the shortcut (nothing else). Both are tried; the first problem is told after.</summary>
		public static void Unregister()
		{
			Exception first = null;
			try { RemoveKeys(); } catch (Exception ex) { first = ex; }
			try { DeleteShortcut(); } catch (Exception ex) { first ??= ex; }
			Changed();
			if (first != null) throw first;
		}

		/// <summary>Only our shortcut (also a read-only one, e.g. copied with an old profile).</summary>
		static void DeleteShortcut()
		{
			string lnk = Shortcut;
			if (!File.Exists(lnk)) return;
			File.SetAttributes(lnk, FileAttributes.Normal);
			File.Delete(lnk);
		}

		static void RemoveKeys()
		{
			Registry.CurrentUser.DeleteSubKeyTree(FilesKey, false);
			Registry.CurrentUser.DeleteSubKeyTree(FoldersKey, false);
			// the keys above ours that writing them created (on a PC that had none): removed only when they are empty
			foreach (var parent in new[] { @"\*\shell", @"\*", @"\Directory\shell", @"\Directory" }) RemoveIfEmpty(Root + parent);
		}

		static void RemoveIfEmpty(string path)
		{
			using (var key = Registry.CurrentUser.OpenSubKey(path))
				if (key == null || key.SubKeyCount > 0 || key.ValueCount > 0) return;
			try { Registry.CurrentUser.DeleteSubKey(path, false); } catch (Exception) { /* something else just wrote there: it stays */ }
		}

		/// <summary>A cascading menu: the verb has a name, an icon and SubCommands="" (its items are in its own shell key).</summary>
		static void Write(string path, string exe, (string key, string id)[] items)
		{
			using var verb = Registry.CurrentUser.CreateSubKey(path);
			verb.SetValue("MUIVerb", T.S("app"));
			verb.SetValue("Icon", "\"" + exe + "\",0");
			verb.SetValue("SubCommands", "");
			// "Player": the menu stays when many files are selected (the default hides it above 15)
			verb.SetValue("MultiSelectModel", "Player");
			foreach (var (key, id) in items)
			{
				using var item = verb.CreateSubKey(@"shell\" + key);
				item.SetValue("MUIVerb", ItemText(id));
				item.SetValue("MultiSelectModel", "Player");
				// a line above "Open DeskPals Tools" (ECF_SEPARATORBEFORE)
				if (id == "open") item.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
				using var cmd = item.CreateSubKey("command");
				cmd.SetValue("", Command(exe, id));
			}
		}

		/// <summary>Tells Explorer the menus changed (only for the real classes).</summary>
		static void Changed()
		{
			if (!string.Equals(Root, @"Software\Classes", StringComparison.OrdinalIgnoreCase)) return;
			try { SHChangeNotify(0x08000000 /* SHCNE_ASSOCCHANGED */, 0, IntPtr.Zero, IntPtr.Zero); } catch (Exception) { }
		}

		[DllImport("shell32.dll")]
		static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
	}
}
