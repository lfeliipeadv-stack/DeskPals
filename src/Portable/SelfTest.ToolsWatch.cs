using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// Watched folders of DeskPals Tools, in a temp folder with its own settings and memory (never the user's): a
		/// picture dropped in the folder becomes a PDF inside its "DeskPals" folder, temporary names are left alone, the
		/// pet tells it, and a restart does not do it again. Needs the built extension (DESKPALS_TOOLS_EXE).
		/// </summary>
		static async Task ToolsWatchCheck()
		{
			string root = Path.Combine(Path.GetTempPath(), "deskpals-tools-watch-" + Environment.ProcessId);
			string folder = Path.Combine(root, "Scans");
			string settings = Path.Combine(root, "tools.json"), state = Path.Combine(root, "tools-watch-done.json");
			Directory.CreateDirectory(folder);
			try
			{
				Check("tools watch: unfinished and temp names ignored", ToolsWatch.Ignored(Path.Combine(folder, "~$report.docx")) && ToolsWatch.Ignored(Path.Combine(folder, "photo.jpg.crdownload"))
					&& ToolsWatch.Ignored(Path.Combine(folder, "a.tmp")) && ToolsWatch.Ignored(Path.Combine(folder, "movie.part")) && !ToolsWatch.Ignored(Path.Combine(folder, "photo.png")));
				if (!ToolsExtension.IsInstalled) { lines.Add("SKIP  tools watch: the extension is not here (DESKPALS_TOOLS_EXE)"); return; }

				// a file already there when the folder is added is left alone
				string old = Path.Combine(folder, "old.png");
				Picture(old);
				File.WriteAllText(settings, new JsonObject { ["watch"] = new JsonArray(new JsonObject { ["folder"] = folder, ["job"] = "topdf" }) }.ToJsonString());
				string outDir = Path.Combine(folder, ToolsWatch.OutFolderName);
				// only looked at (its date), never written: the jobs below must leave it as it is
				string history = Path.Combine(InstallPlace.DataDir, "extensions", "tools-history.json");
				long historyBefore = Stamp(history);
				ToolsWatch.TestStart(settings, state, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
				SpeechBubble.TestClearHeld();

				// a temp name and a new picture (written elsewhere, then moved in, as a browser does)
				File.WriteAllText(Path.Combine(folder, "download.png.crdownload"), "not yet");
				string temp = Path.Combine(root, "photo.png");
				Picture(temp);
				File.Move(temp, Path.Combine(folder, "photo.png"));
				string pdf = Path.Combine(outDir, "photo.pdf");
				for (int i = 0; i < 600 && !(File.Exists(pdf) && ToolsWatch.Idle && ToolsWatch.Told.Count > 0); i++) await Task.Delay(100);
				Check("tools watch: a new picture becomes a PDF in the DeskPals folder", File.Exists(pdf) && new FileInfo(pdf).Length > 500, Directory.Exists(outDir) ? string.Join(",", Directory.GetFileSystemEntries(outDir).Select(Path.GetFileName)) : "no DeskPals folder");
				Check("tools watch: nothing new next to the user's files", Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(n => n).SequenceEqual(new[] { "download.png.crdownload", "old.png", "photo.png" }), string.Join(",", Directory.GetFiles(folder).Select(Path.GetFileName)));
				Check("tools watch: the temp name and the old file were left alone", Directory.GetFiles(outDir).Length == 1, string.Join(",", Directory.GetFiles(outDir).Select(Path.GetFileName)));
				Check("tools watch: the pet tells it", ToolsWatch.Told.Any(t => t.Contains("photo.png") && t.Contains("Scans")), string.Join(" | ", ToolsWatch.Told));

				Check("tools watch: the files already there are not listed one by one", File.Exists(state) && !File.ReadAllText(state).Contains("old.png"));

				// DeskPals closes, a picture arrives, DeskPals starts again: only that one is done (nothing again)
				ToolsWatch.Stop();
				Picture(Path.Combine(folder, "while closed.png"));
				ToolsWatch.TestStart(settings, state, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
				string closedPdf = Path.Combine(outDir, "while closed.pdf");
				for (int i = 0; i < 600 && !(File.Exists(closedPdf) && ToolsWatch.Idle && ToolsWatch.Told.Count > 0); i++) await Task.Delay(100);
				await Task.Delay(2000);
				for (int i = 0; i < 300 && !ToolsWatch.Idle; i++) await Task.Delay(100);
				var made = Directory.GetFiles(outDir).Select(Path.GetFileName).OrderBy(n => n).ToList();
				Check("tools watch: a restart does not do it again", made.SequenceEqual(new[] { "photo.pdf", "while closed.pdf" }), string.Join(",", made) + " " + string.Join(" | ", ToolsWatch.Told));
				Check("tools watch: what came while closed is done at the start", ToolsWatch.Told.Count == 1 && ToolsWatch.Told[0].Contains("while closed.png"), string.Join(" | ", ToolsWatch.Told));

				// an old file (there before the folder was added) renamed while tidying up: left alone
				File.Move(old, Path.Combine(folder, "old renamed.png"));
				await Task.Delay(4000);
				for (int i = 0; i < 300 && !ToolsWatch.Idle; i++) await Task.Delay(100);
				Check("tools watch: an old file renamed is left alone", !File.Exists(Path.Combine(outDir, "old renamed.pdf")), string.Join(",", Directory.GetFiles(outDir).Select(Path.GetFileName)));

				// a download that keeps an empty file under its final name until it is done: no false failure
				lock (ToolsWatch.Told) ToolsWatch.Told.Clear();
				string placeholder = Path.Combine(folder, "download.png");
				File.WriteAllBytes(placeholder, new byte[0]);
				await Task.Delay(4000);
				string real = Path.Combine(root, "download.png");
				Picture(real);
				File.Move(real, placeholder, true);
				string downloadPdf = Path.Combine(outDir, "download.pdf");
				for (int i = 0; i < 600 && !(File.Exists(downloadPdf) && ToolsWatch.Idle && ToolsWatch.Told.Count > 0); i++) await Task.Delay(100);
				Check("tools watch: an empty file waits for its content (no false failure)", File.Exists(downloadPdf) && ToolsWatch.Told.Count == 1 && ToolsWatch.Told[0] == Lang.T("toolswatch.done.topdf", "download.png", "Scans"), string.Join(" | ", ToolsWatch.Told));
				Check("tools watch: the file name is not repeated in a failure", ToolsWatch.Plain("photo.png: It is damaged.\nphoto.png: It is damaged.", "photo.png") == "It is damaged." && ToolsWatch.Plain("?", "a.png") == "" && ToolsWatch.Plain(null, "a.png") == "");

				// the list saved again (the window, or another switch of the extension) while files wait in the queue: all are done
				var queued = Enumerable.Range(1, 4).Select(n => Path.Combine(folder, "q" + n + ".png")).ToList();
				foreach (var q in queued) { string tmp = Path.Combine(root, Path.GetFileName(q)); Picture(tmp); File.Move(tmp, q); }
				for (int i = 0; i < 200 && ToolsWatch.Queued == 0; i++) await Task.Delay(50);
				bool wasQueued = ToolsWatch.Queued > 0;
				File.WriteAllText(settings, new JsonObject { ["watch"] = new JsonArray(new JsonObject { ["folder"] = folder, ["job"] = "topdf" }), ["explorerMenu"] = true }.ToJsonString());
				for (int i = 0; i < 1200 && !(queued.All(q => File.Exists(Path.Combine(outDir, Path.GetFileNameWithoutExtension(q) + ".pdf"))) && ToolsWatch.Idle); i++) await Task.Delay(100);
				Check("tools watch: files in the queue are done when the list is saved again", wasQueued && queued.All(q => File.Exists(Path.Combine(outDir, Path.GetFileNameWithoutExtension(q) + ".pdf"))), (wasQueued ? "" : "(nothing was queued) ") + string.Join(",", Directory.GetFiles(outDir).Select(Path.GetFileName)));

				// a folder on the list that is not there yet (a USB drive, a network folder): watched when it comes
				string later = Path.Combine(root, "Later");
				File.WriteAllText(settings, new JsonObject { ["watch"] = new JsonArray(new JsonObject { ["folder"] = folder, ["job"] = "topdf" }, new JsonObject { ["folder"] = later, ["job"] = "topdf" }) }.ToJsonString());
				await Task.Delay(2500);
				Directory.CreateDirectory(later);
				await Task.Delay(3500);
				string laterPic = Path.Combine(root, "arrived.png");
				Picture(laterPic);
				File.Move(laterPic, Path.Combine(later, "arrived.png"));
				string laterPdf = Path.Combine(later, ToolsWatch.OutFolderName, "arrived.pdf");
				for (int i = 0; i < 600 && !(File.Exists(laterPdf) && ToolsWatch.Idle); i++) await Task.Delay(100);
				Check("tools watch: a folder that comes later is watched", File.Exists(laterPdf));

				// the list changes (the window saved it): a folder removed is no longer watched
				int before = Directory.GetFiles(outDir).Length;
				File.WriteAllText(settings, new JsonObject { ["watch"] = new JsonArray() }.ToJsonString());
				await Task.Delay(2500);
				Picture(Path.Combine(folder, "later.png"));
				await Task.Delay(5000);
				for (int i = 0; i < 300 && !ToolsWatch.Idle; i++) await Task.Delay(100);
				Check("tools watch: the list is read again when it changes", Directory.GetFiles(outDir).Length == before, string.Join(",", Directory.GetFiles(outDir).Select(Path.GetFileName)));

				// the extension was told not to add these jobs to its list of last jobs (the user's real one)
				Check("tools watch: the extension's list of last jobs is not touched", Stamp(history) == historyBefore, history);
			}
			finally
			{
				ToolsWatch.Stop();
				foreach (var b in System.Windows.Forms.Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				SpeechBubble.TestClearHeld();
				try { Directory.Delete(root, true); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			}

			static long Stamp(string path) { try { return File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks : 0; } catch (Exception) { return -1; } }

			static void Picture(string path)
			{
				using (var bmp = new System.Drawing.Bitmap(300, 200))
				{
					using (var g = System.Drawing.Graphics.FromImage(bmp)) g.Clear(System.Drawing.Color.SkyBlue);
					bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
				}
			}
		}
	}
}
