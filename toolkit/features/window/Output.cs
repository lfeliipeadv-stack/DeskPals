using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeskPalsTools
{
	/// <summary>
	/// Where the files made go ("output" in the extension's settings): next to the original (the default), in the
	/// Downloads folder, or in a folder chosen once. A folder that is gone (a USB stick taken out) falls back to next
	/// to the original, so nothing is ever lost.
	/// </summary>
	internal static class Output
	{
		public const string Key = "output", FolderKey = "outputFolder";

		/// <summary>The folder a file made from <paramref name="from"/> goes to.</summary>
		public static string Folder(string from)
		{
			string next = Path.GetDirectoryName(Path.GetFullPath(from));
			try
			{
				string mode = ToolSettings.Text(Key, "next");
				string dir = mode switch
				{
					"downloads" => Downloads(),
					"folder" => ToolSettings.Text(FolderKey),
					_ => null,
				};
				if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
			}
			catch (Exception) { }
			return next;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, uint flags, IntPtr token, out string path);

		/// <summary>The user's Downloads folder (wherever they moved it).</summary>
		public static string Downloads()
		{
			try { if (SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero, out string p) == 0 && Directory.Exists(p)) return p; }
			catch (Exception) { }
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
		}

		[ModuleInitializer]
		internal static void Init()
		{
			Prefs.Register(new Prefs.Pref
			{
				Key = Key, TextKey = "output.pref", HintKey = "output.hint", Order = 5,
				ButtonKey = "output.choose",
				Click = Choose,
			});
		}

		/// <summary>The current choice in words ("Next to the original", "Downloads", the folder's path).</summary>
		public static string Now()
		{
			string mode = ToolSettings.Text(Key, "next");
			return mode == "downloads" ? T.S("output.downloads") : mode == "folder" && !string.IsNullOrEmpty(ToolSettings.Text(FolderKey)) ? ToolSettings.Text(FolderKey) : T.S("output.next");
		}

		static void Choose(IWin32Window owner)
		{
			var a = Ask.Show(owner, T.S("output.pref"), T.S("output.now", Now()),
				new Ask.Field
				{
					Key = "mode", Label = T.S("output.where"), Kind = Ask.Kind.Choice, Value = ToolSettings.Text(Key, "next"),
					Choices = new[] { ("next", T.S("output.next")), ("downloads", T.S("output.downloads")), ("folder", T.S("output.folder")) },
				});
			if (a == null) return;
			if (a["mode"] == "folder")
			{
				using var pick = new FolderBrowserDialog { Description = T.S("output.folder"), UseDescriptionForTitle = true, SelectedPath = ToolSettings.Text(FolderKey) };
				if (pick.ShowDialog(owner) != DialogResult.OK || string.IsNullOrEmpty(pick.SelectedPath)) return;
				ToolSettings.Set(FolderKey, pick.SelectedPath);
			}
			ToolSettings.Set(Key, a["mode"]);
		}
	}
}
