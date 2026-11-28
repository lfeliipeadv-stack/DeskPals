using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== extension: DeskPals Tools

	/// <summary>
	/// The tools extension, a program of its own (DeskPalsTools.exe, like DeskPals Weather): it converts, joins, splits and
	/// reads files. It does not stay running. Files dropped on a pet: DeskPals asks it what can be done ("--list"), the pet
	/// offers that in a bubble, and the chosen job runs ("--do"), with its own small progress window; at the end the pet
	/// says it is done, with "Open" and "Show in folder". Without the extension, the pet offers to download it.
	/// </summary>
	public static class ToolsExtension
	{
		public const string Asset = "DeskPals-Tools.zip";
		const string Exe = "DeskPalsTools.exe";

		static string Folder => Path.Combine(InstallPlace.DataDir, "extensions", "tools");
		static string HashFile => Path.Combine(Folder, "installed.sha256");

		/// <summary>The extension on this computer: next to DeskPals, downloaded, or (developer) built in the repository.</summary>
		public static string Installed()
		{
			// self-test: the copy being tested is outside the repository; the test says where the built extension is
			string test = SelfTest.Running ? Environment.GetEnvironmentVariable("DESKPALS_TOOLS_EXE") : null;
			if (!string.IsNullOrEmpty(test) && File.Exists(test)) return test;
			string here = AppContext.BaseDirectory;
			var places = new List<string> { Path.Combine(here, "extensions", "tools", Exe), Path.Combine(Folder, Exe) };
			var dir = new DirectoryInfo(here);
			for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
				foreach (var cfg in new[] { "Release", "Debug" })
					places.Add(Path.Combine(dir.FullName, "toolkit", "bin", cfg, "net10.0-windows10.0.19041.0", "win-x64", Exe));
			return places.FirstOrDefault(File.Exists);
		}

		public static bool IsInstalled => Installed() != null;

		public static string Status() => Lang.T(IsInstalled ? "ext.state.installed" : "ext.state.missing");

		/// <summary>The card's button: its window (download it first when it is not here).</summary>
		public static async Task OpenAsync(IWin32Window owner, Action<string> status, IEnumerable<string> files = null)
		{
			if (!IsInstalled && !await InstallAsync(owner, status)) return;
			try
			{
				var start = Start();
				foreach (var f in files ?? Enumerable.Empty<string>()) start.ArgumentList.Add(f);
				Process.Start(start);
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}

		static ProcessStartInfo Start()
		{
			string exe = Installed();
			var start = new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe) };
			start.ArgumentList.Add("--config");
			start.ArgumentList.Add(LocalData.ConfigPath());
			start.ArgumentList.Add("--lang=" + Lang.Current);
			return start;
		}

		public static async Task<bool> InstallAsync(IWin32Window owner, Action<string> status)
		{
			if (IsInstalled) return true;
			string title = Lang.T("ext.tools.title");
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
			Achievements.Unlock("extension");
			return IsInstalled;
		}

		static async Task Download(Updater.Release release)
		{
			string zip = Path.Combine(Path.GetTempPath(), Asset);
			await Updater.GetAssetAsync(release, Asset, zip);
			Directory.CreateDirectory(Folder);
			ZipFile.ExtractToDirectory(zip, Folder, true);
			File.WriteAllText(HashFile, Updater.Sha256(File.ReadAllBytes(zip)));
			File.Delete(zip);
		}

		/// <summary>A new version in the latest release replaces the downloaded one (it never runs for long, so no need to close it).</summary>
		public static async Task UpdateAsync()
		{
			try
			{
				if (Installed() != Path.Combine(Folder, Exe)) return;
				var release = await Updater.GetLatestAsync();
				if (release == null || !release.Assets.ContainsKey(Asset)) return;
				string hash = await Updater.AssetHashAsync(release, Asset);
				string mine = File.Exists(HashFile) ? File.ReadAllText(HashFile).Trim() : "";
				if (hash == null || string.Equals(hash, mine, StringComparison.OrdinalIgnoreCase)) return;
				await Download(release);
				LogFile.Important("DeskPals Tools updated");
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}

		static bool updateChecked;
		public static void Apply()
		{
			if (!updateChecked && Updater.Auto) { updateChecked = true; _ = Task.Delay(TimeSpan.FromMinutes(3)).ContinueWith(_ => UpdateAsync(), TaskScheduler.FromCurrentSynchronizationContext()); }
		}

		/// <summary>One thing the extension can do with the files: its id, the text and an emoji.</summary>
		public sealed class Job { public string Id, Text, Emoji; public bool WindowOnly; }

		/// <summary>What can be done with these files (asked to the extension), or an empty list.</summary>
		public static async Task<List<Job>> ListAsync(IList<string> files)
		{
			var list = new List<Job>();
			string exe = Installed();
			if (exe == null) return list;
			try
			{
				var start = Start();
				start.ArgumentList.Add("--list");
				foreach (var f in files) start.ArgumentList.Add(f);
				start.RedirectStandardOutput = true;
				start.StandardOutputEncoding = Encoding.UTF8;
				start.CreateNoWindow = true;
				using var p = Process.Start(start);
				string json = await p.StandardOutput.ReadToEndAsync();
				await p.WaitForExitAsync();
				foreach (var n in JsonNode.Parse(json)?.AsArray() ?? new JsonArray())
					list.Add(new Job { Id = (string)n["id"], Text = (string)n["text"], Emoji = (string)n["emoji"], WindowOnly = (bool?)n["windowOnly"] ?? false });
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
			return list;
		}

		/// <summary>The result of a job: done, the files made, a message.</summary>
		public sealed class Result { public bool Ok; public List<string> Outputs = new List<string>(); public string Message = "", Preview = ""; }

		/// <summary>Do a job (the extension shows its progress) and wait for it.</summary>
		public static async Task<Result> RunAsync(string job, IList<string> files)
		{
			var r = new Result();
			string resultFile = Path.Combine(Path.GetTempPath(), "deskpals-tools-" + Guid.NewGuid().ToString("N") + ".json");
			try
			{
				var start = Start();
				start.ArgumentList.Add("--do=" + job);
				start.ArgumentList.Add("--result=" + resultFile);
				foreach (var f in files) start.ArgumentList.Add(f);
				using var p = Process.Start(start);
				await p.WaitForExitAsync();
				if (File.Exists(resultFile))
				{
					var j = JsonNode.Parse(File.ReadAllText(resultFile));
					r.Ok = (bool?)j?["ok"] ?? false;
					r.Message = (string)j?["message"] ?? "";
					r.Preview = (string)j?["preview"] ?? "";
					foreach (var o in j?["outputs"]?.AsArray() ?? new JsonArray()) r.Outputs.Add((string)o);
				}
			}
			catch (Exception ex) { r.Message = ex.Message; LogFile.Ignored(ex); }
			finally { try { File.Delete(resultFile); } catch (Exception) { } }
			return r;
		}

		// ------------------------------------------------------------------ files dropped on a pet

		/// <summary>A pet file (an animations.xml): changes the pet, as before. Anything else goes to the tools.</summary>
		public static bool IsPetFile(string file)
		{
			try
			{
				if (!file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || !File.Exists(file)) return false;
				using var r = new StreamReader(file);
				var buf = new char[4096];
				int n = r.Read(buf, 0, buf.Length);
				return new string(buf, 0, n).IndexOf("<animations", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			catch (Exception) { return false; }
		}

		/// <summary>Files were dropped on a pet: it offers what can be done with them.</summary>
		public static async void Offer(IList<string> files)
		{
			if (files == null || files.Count == 0) return;
			string what = files.Count == 1 ? Path.GetFileName(files[0]) : null;
			if (!IsInstalled)
			{
				Program.Mainthread?.ReactAll("think", "look around", "curious");
				SpeechBubble.Say(Lang.T("tools.install"), 0,
					new SpeechBubble.Choice(Lang.T("tools.download"), async () => { if (await InstallAsync(null, null)) Offer(files); }, true),
					new SpeechBubble.Choice(Lang.T("tools.notNow"), null));
				return;
			}
			Program.Mainthread?.ReactAll("think", "read", "look around");
			// the jobs that need the extension's own window (choosing pages…) are behind "More…"
			var jobs = (await ListAsync(files)).Where(j => !j.WindowOnly).ToList();
			if (jobs.Count == 0) { SpeechBubble.Say(Lang.T("tools.nothing"), 8); return; }
			var choices = jobs.Take(4).Select((j, i) => new SpeechBubble.Choice(j.Emoji + " " + j.Text, () => Do(j, files), i == 0)).ToList();
			choices.Add(new SpeechBubble.Choice(Lang.T("tools.more"), () => _ = OpenAsync(null, null, files)));
			SpeechBubble.Say(what != null ? Lang.T("tools.ask", what) : Lang.T("tools.askMany", files.Count), 0, choices.ToArray());
		}

		static async void Do(Job job, IList<string> files)
		{
			Program.Mainthread?.ReactAll("work", "think", "read", "type");
			SpeechBubble.Say(Lang.T("tools.working"), 4);
			var r = await RunAsync(job.Id, files);
			if (r.Outputs.Count > 0)
			{
				Program.Mainthread?.ReactAll("cheer", "happy", "thumbs up", "jump");
				Achievements.Count("tool");
				string first = r.Outputs[0];
				string text = r.Outputs.Count == 1 ? Lang.T("tools.done", Path.GetFileName(first)) : Lang.T("tools.doneMany", r.Outputs.Count);
				if (!string.IsNullOrEmpty(r.Message)) text += "\n" + Lang.T("tools.partly", r.Message);
				// a taste of what was made (the start of a transcription, of a summary, of the text read)
				if (!string.IsNullOrEmpty(r.Preview)) text += "\n\n“" + Short(r.Preview) + "”";
				SpeechBubble.Say(text, 0,
					new SpeechBubble.Choice(Lang.T("tools.open"), () => OpenFile(first), true),
					new SpeechBubble.Choice(Lang.T("tools.showFolder"), () => Reveal(first)));
			}
			else if (r.Ok && !string.IsNullOrEmpty(r.Preview))
			{
				// done without a file (a help that opened the Store…): its sentence is the answer
				Program.Mainthread?.ReactAll("happy", "thumbs up");
				SpeechBubble.Say(r.Preview, 12);
			}
			else if (r.Ok) { }   // done, nothing to tell (a window closed without anything to keep)
			else
			{
				Program.Mainthread?.ReactAll("sad", "confused", "shrug", "think");
				SpeechBubble.Say(Lang.T("tools.failed", string.IsNullOrEmpty(r.Message) ? "?" : r.Message), 12);
			}
		}

		static string Short(string s)
		{
			s = s.Replace("\r", " ").Replace("\n", " ").Trim();
			while (s.Contains("  ")) s = s.Replace("  ", " ");
			return s.Length > 140 ? s[..140].TrimEnd() + "…" : s;
		}

		static void OpenFile(string path) { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { LogFile.Ignored(ex); } }
		static void Reveal(string path) { try { var s = new ProcessStartInfo("explorer.exe") { UseShellExecute = false }; s.ArgumentList.Add("/select,"); s.ArgumentList.Add(path); Process.Start(s); } catch (Exception ex) { LogFile.Ignored(ex); } }
	}
}
