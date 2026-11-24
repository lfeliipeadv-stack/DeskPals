using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// The Pet Editor (DeskPals-Editor.exe; PetEditor.exe before 1.2.0), a separate program to create and change pets. It is looked for where it can be:
	/// next to DeskPals.exe (installed together), the copy downloaded before into the user's folder, or in a developer
	/// build the one of the repository. When it is found it opens; when it is not, the user is asked whether to download it
	/// (the size is said), and only then it is downloaded from the latest release, checked against its SHA256 and opened.
	/// </summary>
	internal static class PetEditorApp
	{
		/// <summary>Self-test: when set, the question "download it?" gets this answer without showing the window.</summary>
		internal static DialogResult? TestAnswer;
		/// <summary>Self-test: when set, programs are not really started; the path that would be started is kept in <see cref="LastStarted"/>.</summary>
		internal static bool TestNoStart;
		internal static string LastStarted;
		/// <summary>Self-test: places to look at instead of the real ones.</summary>
		internal static List<string> TestCandidates;

		/// <summary>The places the editor can be, in order.</summary>
		static IEnumerable<string> Candidates()
		{
			if (TestCandidates != null) { foreach (var c in TestCandidates) yield return c; yield break; }
			string here = AppContext.BaseDirectory;
			yield return Path.Combine(here, Updater.EditorAsset);
			yield return Updater.EditorPath;
			// the names before 1.2.0 (PetEditor.exe): an install updated from then, or a copy downloaded then
			yield return Path.Combine(here, Updater.OldEditorAsset);
			yield return Updater.OldEditorPath;
			// a developer build: the editor built in the repository
			var dir = new DirectoryInfo(here);
			for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
				yield return Path.Combine(dir.FullName, "editor", "bin", "Release", "net10.0-windows", "win-x64", Updater.EditorAsset);
		}

		/// <summary>The editor on this computer, or null when it is not here.</summary>
		public static string Installed()
		{
			try { return Candidates().FirstOrDefault(File.Exists); }
			catch (Exception skipped) { LogFile.Ignored(skipped); return null; }
		}

		/// <summary>True when the editor is on this computer.</summary>
		public static bool IsInstalled => Installed() != null;

		/// <summary>The editor's icon, for a menu (null when it is not installed).</summary>
		public static Image MenuIcon()
		{
			string exe = Installed();
			if (exe == null) return null;
			try { using (var ico = Icon.ExtractAssociatedIcon(exe)) return ico?.ToBitmap(); }
			catch (Exception skipped) { LogFile.Ignored(skipped); return null; }
		}

		/// <summary>The text of a menu item: "Pet Editor", or "Pet Editor (download)" when it is not here yet.</summary>
		public static string MenuText() => Lang.T(IsInstalled ? "editor.menu" : "editor.menuDownload");

		/// <summary>
		/// Open the editor (with these arguments, such as a pet to open). Not on this computer: ask whether to download it,
		/// then download it and open it. <paramref name="status"/> tells what is going on (downloading…), or a bubble
		/// of the pet when there is none. Returns true when the editor was started.
		/// </summary>
		public static async Task<bool> OpenAsync(IWin32Window owner, string args = null, Action<string> status = null)
		{
			status ??= text => { if (!string.IsNullOrEmpty(text)) SpeechBubble.Say(text, 6); };
			string exe = Installed();
			// a copy downloaded before is brought up to date (it is the user's own: they chose to download it)
			if (exe != null && (string.Equals(exe, Updater.EditorPath, StringComparison.OrdinalIgnoreCase) || string.Equals(exe, Updater.OldEditorPath, StringComparison.OrdinalIgnoreCase)) && TestCandidates == null)
				try { exe = await Updater.GetEditorAsync() ?? exe; }
				catch (Exception skipped) { LogFile.Ignored(skipped); }
			if (exe == null)
			{
				// not here: what is there to download, and how big it is
				Updater.Release release = null;
				try { release = await Updater.GetLatestAsync(); }
				catch (Exception ex) { Tell(owner, Lang.T("opt.editor.downloadFail", ex.Message)); return false; }
				string asset = Updater.EditorAssetOf(release);
				if (asset == null) { Tell(owner, Lang.T("opt.editor.missing")); return false; }
				long bytes = release.Sizes.TryGetValue(asset, out long b) ? b : 0;
				string size = bytes > 0 ? Lang.T("editor.size", (bytes / 1048576.0).ToString("0.0")) : "";
				var answer = TestAnswer ?? PetMessage.Show(owner, Lang.T("editor.notInstalled", size), Lang.T("editor.title"),
					MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
				if (answer != DialogResult.Yes) return false;
				status(Lang.T("opt.editor.downloading"));
				try { exe = await Updater.GetEditorAsync(); }
				catch (Exception ex) { status(""); Tell(owner, Lang.T("opt.editor.downloadFail", ex.Message)); return false; }
				if (exe == null) { status(""); Tell(owner, Lang.T("opt.editor.missing")); return false; }
				status(Lang.T("editor.downloaded"));
			}
			return Start(exe, args, owner);
		}

		/// <summary>Start the editor; a message when Windows does not let it start.</summary>
		static bool Start(string exe, string args, IWin32Window owner)
		{
			LastStarted = exe;
			if (TestNoStart) return true;
			try
			{
				Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, Arguments = args ?? "", WorkingDirectory = Path.GetDirectoryName(exe) });
				return true;
			}
			catch (Exception ex) { Tell(owner, ex.Message); return false; }
		}

		static void Tell(IWin32Window owner, string text)
		{
			if (TestAnswer != null) { LastMessage = text; return; }
			PetMessage.Show(owner, text, Lang.T("editor.title"));
		}

		/// <summary>Self-test: the last message that would have been shown.</summary>
		internal static string LastMessage;
	}
}
