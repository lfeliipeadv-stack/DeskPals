using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// The Pet Editor from the program: found where it is installed and opened; not found, the user is asked before
		/// anything is downloaded, and saying no downloads and opens nothing. The menu text says whether it is here.
		/// Nothing is really started or downloaded.
		/// </summary>
		static async Task PetEditorOpen()
		{
			string tmp = Path.Combine(Path.GetTempPath(), "DesktopPet-selftest-editor");
			Directory.CreateDirectory(tmp);
			string fake = Path.Combine(tmp, Updater.EditorAsset), missing = Path.Combine(tmp, "nothing", Updater.EditorAsset);
			File.WriteAllText(fake, "not a real program");
			try
			{
				PetEditorApp.TestNoStart = true;
				PetEditorApp.TestAnswer = DialogResult.No;

				// installed: found and opened, no question
				PetEditorApp.TestCandidates = new List<string> { missing, fake };
				PetEditorApp.LastStarted = null; PetEditorApp.LastMessage = null;
				Check("editor: found where it is installed", PetEditorApp.Installed() == fake && PetEditorApp.IsInstalled, PetEditorApp.Installed());
				Check("editor: menu says it opens", PetEditorApp.MenuText() == Lang.T("editor.menu"), PetEditorApp.MenuText());
				bool opened = await PetEditorApp.OpenAsync(null);
				Check("editor: installed, it opens", opened && PetEditorApp.LastStarted == fake, PetEditorApp.LastStarted ?? PetEditorApp.LastMessage);

				// not installed: nothing found, the menu offers the download, and "no" downloads and opens nothing
				PetEditorApp.TestCandidates = new List<string> { missing };
				PetEditorApp.LastStarted = null; PetEditorApp.LastMessage = null;
				Check("editor: not installed is seen", PetEditorApp.Installed() == null && !PetEditorApp.IsInstalled);
				Check("editor: menu offers the download", PetEditorApp.MenuText() == Lang.T("editor.menuDownload"), PetEditorApp.MenuText());
				opened = await PetEditorApp.OpenAsync(null);
				Check("editor: not installed and the user says no: nothing opens", !opened && PetEditorApp.LastStarted == null, PetEditorApp.LastMessage ?? "");
				Check("editor: the question tells the size when there is one", Lang.T("editor.notInstalled", Lang.T("editor.size", "3.4")).Contains("3.4"));

				// the real places: next to DesktopPet.exe or the downloaded copy, in this order
				PetEditorApp.TestCandidates = null;
				lines.Add("INFO  editor: on this computer: " + (PetEditorApp.Installed() ?? "not installed"));
			}
			finally
			{
				PetEditorApp.TestCandidates = null; PetEditorApp.TestAnswer = null; PetEditorApp.TestNoStart = false;
				try { Directory.Delete(tmp, true); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			}
		}
	}
}
