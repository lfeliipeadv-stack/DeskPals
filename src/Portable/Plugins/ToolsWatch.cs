using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace DesktopPet
{
	// ====================================================================== extension: DeskPals Tools, watched folders

	/// <summary>
	/// Watched folders of DeskPals Tools: the extension's window keeps a list of folders, each with a job that needs no
	/// questions ("watch" in tools.json: [{folder, job}]). While DeskPals runs, a new file in one of those folders (top
	/// level only) is waited until it is complete, then handed to the extension ("--do=job --quiet"), one at a time. The
	/// job works on a copy in the temp folder, so nothing new ever appears next to the user's file, and what it made is
	/// moved into a "DeskPals" folder inside the watched folder (never watched, so it never starts a job again). Files
	/// already handled are remembered (path, size and date), so a restart does not do them again; files that were already
	/// in a folder when it was added are left alone. The pet tells it with a notice (it waits during quiet time).
	/// </summary>
	public static class ToolsWatch
	{
		/// <summary>The folder inside each watched folder where the results go.</summary>
		public const string OutFolderName = "DeskPals";

		internal static string SettingsFile = Path.Combine(InstallPlace.DataDir, "extensions", "tools.json");
		internal static string StateFile = Path.Combine(InstallPlace.DataDir, "extensions", "tools-watch-done.json");
		/// <summary>How long a file must stay the same size before it is taken (a copy or a download still going on).</summary>
		internal static TimeSpan Settle = TimeSpan.FromSeconds(4);
		/// <summary>How often folders on the list that are not there (a USB drive, a network folder) are looked for again.</summary>
		internal static TimeSpan MissingCheck = TimeSpan.FromSeconds(30);
		static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15), ListTimeout = TimeSpan.FromSeconds(60);
		/// <summary>A safety limit only: files handled that are gone or changed are forgotten at each start anyway.</summary>
		const int MaxRemembered = 50000;

		sealed class Rule { public string Folder, Job; public bool Missing; }
		/// <summary>
		/// A file being waited. <see cref="CheckDates"/>: it came from a rename or a change, not as a new file, so it may
		/// be an old file that was already in the folder (renamed while tidying up): left alone when its dates are older.
		/// </summary>
		sealed class Waiting { public Rule Rule; public long Size = -1; public DateTime Write; public DateTime Since; public bool CheckDates; }
		sealed class Batch { public int Done, Failed; public string OneName, FailName, Error; public string OutDir; }

		static readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
		static readonly Dictionary<string, Waiting> waiting = new Dictionary<string, Waiting>(StringComparer.OrdinalIgnoreCase);
		static readonly Queue<(string file, Rule rule)> queue = new Queue<(string, Rule)>();
		static readonly Dictionary<(string folder, string job), Batch> batches = new Dictionary<(string, string), Batch>();
		static List<Rule> rules = new List<Rule>();
		static FileSystemWatcher settingsWatcher;
		static Timer timer;
		static bool started, busy, lookingForMissing;
		static int readFailures;
		static DateTime lastMissingCheck;
		static volatile bool reload;

		// what is remembered between runs: files handled (in order, to forget the oldest) and, for each folder, when it
		// was first watched (files older than that were already there and are left alone, without listing them all)
		static readonly List<string> doneList = new List<string>();
		static readonly HashSet<string> done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		static readonly Dictionary<string, DateTime> knownFolders = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

		/// <summary>Self-test: the notices the pet gave.</summary>
		internal static readonly List<string> Told = new List<string>();

		/// <summary>At start-up (next to <see cref="ToolsExtension.Apply"/>). The self-test starts it itself, on its own folders.</summary>
		public static void Apply()
		{
			if (started || SelfTest.Running) return;
			started = true;
			Start();
		}

		static void Start()
		{
			if (timer == null) { timer = new Timer { Interval = 1000 }; timer.Tick += (s, e) => Tick(); }
			LoadState();
			WatchSettings();
			Reload();
			timer.Start();
		}

		/// <summary>Stops watching (the queue and the files being waited are forgotten; they are found again at the next start).</summary>
		internal static void Stop()
		{
			timer?.Stop();
			foreach (var w in watchers) w.Dispose();
			watchers.Clear();
			settingsWatcher?.Dispose();
			settingsWatcher = null;
			lock (waiting) waiting.Clear();
			queue.Clear();
			started = false;
		}

		/// <summary>Self-test: watch with these files (settings and memory) instead of the user's own.</summary>
		internal static void TestStart(string settingsFile, string stateFile, TimeSpan settle, TimeSpan? missingCheck = null)
		{
			Stop();
			SettingsFile = settingsFile; StateFile = stateFile; Settle = settle;
			MissingCheck = missingCheck ?? TimeSpan.FromSeconds(30);
			lock (Told) Told.Clear();
			batches.Clear();
			started = true;
			Start();
		}

		/// <summary>Nothing waiting, queued or running.</summary>
		internal static bool Idle { get { lock (waiting) return waiting.Count == 0 && queue.Count == 0 && !busy; } }

		/// <summary>Self-test: files in the queue (not counting the one being done).</summary>
		internal static int Queued { get { lock (waiting) return queue.Count; } }

		// ------------------------------------------------------------------ the list of folders

		/// <summary>
		/// The folders and jobs in tools.json (repeated ones, whole drives and result folders left out; folders that are
		/// not there right now are kept, marked <see cref="Rule.Missing"/>, and looked for again later), or null when the
		/// file could not be read right now (the window is saving it): then the list stays as it was.
		/// </summary>
		static List<Rule> ReadRules()
		{
			var list = new List<Rule>();
			try
			{
				if (!File.Exists(SettingsFile)) return list;
				var arr = JsonNode.Parse(File.ReadAllText(SettingsFile))?["watch"] as JsonArray;
				foreach (var n in arr ?? new JsonArray())
				{
					if (!(n is JsonObject)) continue;
					string folder = n["folder"] is JsonValue f && f.TryGetValue(out string fs) ? fs : null;
					string job = n["job"] is JsonValue j && j.TryGetValue(out string js) ? js : null;
					if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(job)) continue;
					try { folder = Path.GetFullPath(folder).TrimEnd('\\'); } catch (Exception) { continue; }
					// a whole drive is never watched (the window refuses it too)
					if (string.Equals(Path.GetPathRoot(folder)?.TrimEnd('\\'), folder, StringComparison.OrdinalIgnoreCase)) continue;
					if (list.Any(r => string.Equals(r.Folder, folder, StringComparison.OrdinalIgnoreCase))) continue;
					list.Add(new Rule { Folder = folder, Job = job.Trim(), Missing = !Directory.Exists(folder) });
				}
			}
			catch (Exception ex) { LogFile.Ignored(ex); return null; }
			// a result folder ("…\DeskPals") is never watched: its files would start the job again
			return list.Where(r => !list.Any(o => string.Equals(Path.Combine(o.Folder, OutFolderName), r.Folder, StringComparison.OrdinalIgnoreCase))).ToList();
		}

		/// <summary>tools.json changed (the extension's window saved the list): read it again.</summary>
		static void WatchSettings()
		{
			try
			{
				string dir = Path.GetDirectoryName(SettingsFile);
				Directory.CreateDirectory(dir);
				settingsWatcher = new FileSystemWatcher(dir, Path.GetFileName(SettingsFile)) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size };
				void Changed(object s, FileSystemEventArgs e) => reload = true;
				settingsWatcher.Changed += Changed;
				settingsWatcher.Created += Changed;
				settingsWatcher.Deleted += Changed;
				settingsWatcher.Renamed += (s, e) => reload = true;
				settingsWatcher.EnableRaisingEvents = true;
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}

		static void Reload()
		{
			var read = ReadRules();
			// tried again at the next ticks (a damaged file is not read again and again forever)
			if (read == null) { if (++readFailures < 10) reload = true; return; }
			readFailures = 0;
			foreach (var w in watchers) w.Dispose();
			watchers.Clear();
			rules = read;
			// the files waited keep going with the folder's rule as it is now (the list saved again is the same list,
			// or the folder got another job); those of a folder taken off the list are dropped
			lock (waiting)
				foreach (var (path, w) in waiting.ToList())
				{
					var now = Current(w.Rule.Folder);
					if (now == null || now.Missing) waiting.Remove(path); else w.Rule = now;
				}
			// a folder taken off the list is forgotten: added again later, what is in it then is left alone again (one
			// that is only missing for now keeps its date: what arrived while it was away is done when it comes back)
			foreach (var f in knownFolders.Keys.Where(f => Current(f) == null).ToList()) knownFolders.Remove(f);
			foreach (var rule in rules)
			{
				if (rule.Missing) continue;
				try
				{
					var w = new FileSystemWatcher(rule.Folder) { IncludeSubdirectories = false, InternalBufferSize = 65536, NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite };
					var r = rule;
					// a new file (made, copied or moved in) is always taken; a renamed or changed one only when it is
					// not an old file that was already there (someone tidying up old photos gets no PDF for each)
					w.Created += (s, e) => Seen(e.FullPath, r, false);
					w.Changed += (s, e) => Seen(e.FullPath, r, true);
					w.Renamed += (s, e) => Seen(e.FullPath, r, true);
					// too many files at once (the watcher lost some): look at the whole folder again
					w.Error += (s, e) => reload = true;
					w.EnableRaisingEvents = true;
					watchers.Add(w);
				}
				catch (Exception ex) { LogFile.Ignored(ex); continue; }
				Scan(rule);
			}
			SaveState();
		}

		/// <summary>The rule of this folder on the list now (compared by folder: a list read again makes new rules), or null.</summary>
		static Rule Current(string folder) => rules.FirstOrDefault(r => string.Equals(r.Folder, folder, StringComparison.OrdinalIgnoreCase));

		/// <summary>
		/// Folders on the list that were not there (a USB drive, a network or cloud folder not ready yet) are looked for
		/// now and then, away from the window's thread (a network folder that is gone can take long to answer).
		/// </summary>
		static void LookForMissing()
		{
			if (lookingForMissing || DateTime.Now - lastMissingCheck < MissingCheck) return;
			var missing = rules.Where(r => r.Missing).Select(r => r.Folder).ToList();
			if (missing.Count == 0) return;
			lastMissingCheck = DateTime.Now;
			lookingForMissing = true;
			Task.Run(() =>
			{
				try { if (missing.Any(Directory.Exists)) reload = true; }
				catch (Exception ex) { LogFile.Ignored(ex); }
				finally { lookingForMissing = false; }
			});
		}

		/// <summary>The file was already in the folder before it was watched (both its dates are older).</summary>
		static bool OldFile(FileInfo fi, string folder)
			=> knownFolders.TryGetValue(folder, out var since) && fi.CreationTimeUtc < since && fi.LastWriteTimeUtc < since;

		/// <summary>
		/// The files already in a folder: the first time it is seen they are left alone (only new files are handled: the
		/// moment is remembered, not every file, so a folder with thousands of files costs nothing); later (DeskPals was
		/// closed for a while) the files that arrived since then and were not handled yet are taken.
		/// </summary>
		static void Scan(Rule rule)
		{
			if (!knownFolders.TryGetValue(rule.Folder, out var since))
			{
				knownFolders[rule.Folder] = DateTime.UtcNow;
				return;
			}
			string[] files;
			try { files = Directory.GetFiles(rule.Folder); }
			catch (Exception ex) { LogFile.Ignored(ex); return; }
			foreach (var f in files)
			{
				if (Ignored(f)) continue;
				try
				{
					// a file copied in gets a new creation date, one saved there a new write date
					var fi = new FileInfo(f);
					if (!fi.Exists || OldFile(fi, rule.Folder)) continue;
				}
				catch (Exception) { continue; }
				string key = Key(f);
				if (key == null || done.Contains(key)) continue;
				Seen(f, rule, false);
			}
		}

		// ------------------------------------------------------------------ new files

		/// <summary>Names never handled: unfinished downloads, Office's "~$" files, temporary and hidden files, shortcuts.</summary>
		internal static bool Ignored(string path)
		{
			string name = Path.GetFileName(path);
			if (ComputerEvents.IsUnfinished(name)) return true;
			string ext = Path.GetExtension(name).ToLowerInvariant();
			if (ext == ".lnk" || ext == ".url" || ext == ".db" || name.StartsWith("deskpals-tools-", StringComparison.OrdinalIgnoreCase)) return true;
			try { if ((File.GetAttributes(path) & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Directory)) != 0) return true; }
			catch (Exception) { }
			return false;
		}

		/// <summary>(From the watcher's thread.) A file appeared or changed: it is waited until it stops changing.</summary>
		static void Seen(string path, Rule rule, bool checkDates)
		{
			if (Ignored(path)) return;
			lock (waiting) if (!waiting.ContainsKey(path)) waiting[path] = new Waiting { Rule = rule, Since = DateTime.Now, CheckDates = checkDates };
		}

		/// <summary>The file, its size and its date: the same file changed later is a new one.</summary>
		static string Key(string path)
		{
			try
			{
				var fi = new FileInfo(path);
				return fi.Exists ? Path.GetFullPath(path) + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks : null;
			}
			catch (Exception) { return null; }
		}

		/// <summary>Still being written by someone (a copy, a download, Word saving it).</summary>
		static bool Locked(string path)
		{
			try { using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) return false; }
			catch (Exception) { return true; }
		}

		static void Tick()
		{
			if (reload) { reload = false; Reload(); }
			LookForMissing();
			var ready = new List<(string, Rule)>();
			lock (waiting)
			{
				foreach (var (path, w) in waiting.ToList())
				{
					FileInfo fi;
					try { fi = new FileInfo(path); } catch (Exception) { waiting.Remove(path); continue; }
					if (!fi.Exists) { waiting.Remove(path); continue; }
					if (fi.Length != w.Size || fi.LastWriteTimeUtc != w.Write) { w.Size = fi.Length; w.Write = fi.LastWriteTimeUtc; w.Since = DateTime.Now; continue; }
					// an empty file is a name kept for a download still going on (Firefox does that): it waits for its content
					if (fi.Length == 0 || DateTime.Now - w.Since < Settle || Locked(path)) continue;
					waiting.Remove(path);
					if (w.CheckDates && OldFile(fi, w.Rule.Folder)) continue;
					if (!queue.Any(q => string.Equals(q.file, path, StringComparison.OrdinalIgnoreCase))) ready.Add((path, w.Rule));
				}
				foreach (var r in ready) queue.Enqueue(r);
			}
			if (busy) return;
			if (queue.Count > 0) _ = Next();
			else if (batches.Count > 0) Tell();
		}

		static async Task Next()
		{
			busy = true;
			try
			{
				(string file, Rule rule) item;
				lock (waiting) item = queue.Dequeue();
				await Handle(item.file, item.rule);
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
			finally { busy = false; }
		}

		// ------------------------------------------------------------------ doing the job

		/// <summary>The job on a copy of the file in the temp folder; what it made goes to the "DeskPals" folder.</summary>
		static async Task Handle(string file, Rule queued)
		{
			string key = Key(file);
			// the folder's rule as it is now: the list may have been saved again while the file was in the queue
			var rule = Current(queued.Folder);
			if (key == null || done.Contains(key) || rule == null || rule.Missing) return;
			string exe = ToolsExtension.Installed();
			if (exe == null) return;   // not remembered: done when the extension comes back
			// only files the job is for (a ZIP in a "Convert to PDF" folder is left alone, quietly)
			var jobs = await Task.Run(() => ListJobs(exe, file));
			if (jobs == null) return;
			if (!jobs.Contains(rule.Job)) { Remember(key); SaveState(); return; }

			string work = Path.Combine(Path.GetTempPath(), "deskpals-watch-" + Guid.NewGuid().ToString("N"));
			string resultFile = Path.Combine(Path.GetTempPath(), "deskpals-watch-" + Guid.NewGuid().ToString("N") + ".json");
			var batch = BatchFor(rule);
			try
			{
				Directory.CreateDirectory(work);
				string copy = Path.Combine(work, Path.GetFileName(file));
				File.Copy(file, copy);
				var (ok, outputs, message) = await Task.Run(() => RunJob(exe, rule.Job, copy, resultFile));
				var moved = new List<string>();
				if (outputs.Count > 0)
				{
					string outDir = Path.Combine(rule.Folder, OutFolderName);
					Directory.CreateDirectory(outDir);
					foreach (var o in outputs)
					{
						// only what was made in the work folder is moved (never anything elsewhere)
						if (!Path.GetFullPath(o).StartsWith(work + "\\", StringComparison.OrdinalIgnoreCase)) continue;
						moved.Add(MoveTo(o, outDir));
					}
					batch.OutDir = outDir;
				}
				if (moved.Count > 0) { batch.Done++; batch.OneName = Path.GetFileName(file); }
				if (moved.Count == 0 || !string.IsNullOrEmpty(message)) { batch.Failed++; batch.FailName = Path.GetFileName(file); batch.Error = Plain(message, Path.GetFileName(file)); }
				// a file that failed is remembered too: it would fail again at every start (the pet already told it)
				Remember(key);
				SaveState();
			}
			catch (Exception ex) { batch.Failed++; batch.FailName = Path.GetFileName(file); batch.Error = ""; LogFile.Ignored(ex); }
			finally
			{
				try { if (Directory.Exists(work)) Directory.Delete(work, true); } catch (Exception ex) { LogFile.Ignored(ex); }
				try { File.Delete(resultFile); } catch (Exception) { }
			}
		}

		/// <summary>
		/// The extension's message for the pet: without the file name it starts with ("photo.png: …", the pet already
		/// names the file), one line. Empty when there is nothing to add (the pet then says it plainly).
		/// </summary>
		internal static string Plain(string message, string name)
		{
			var lines = (message ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0)
				.Select(l => l.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase) ? l.Substring(name.Length + 1).Trim() : l)
				.Where(l => l.Length > 0 && l != "?").Distinct().ToList();
			return string.Join(" ", lines);
		}

		static Batch BatchFor(Rule rule)
		{
			if (!batches.TryGetValue((rule.Folder, rule.Job), out var b)) batches[(rule.Folder, rule.Job)] = b = new Batch();
			return b;
		}

		/// <summary>A file or folder moved to <paramref name="dir"/>, with " (2)", " (3)"… when the name is taken (nothing is overwritten).</summary>
		static string MoveTo(string path, string dir)
		{
			bool isDir = Directory.Exists(path);
			string name = Path.GetFileNameWithoutExtension(path), ext = isDir ? "" : Path.GetExtension(path);
			if (isDir) name = Path.GetFileName(path);
			string dest = Path.Combine(dir, name + ext);
			for (int n = 2; File.Exists(dest) || Directory.Exists(dest); n++) dest = Path.Combine(dir, name + " (" + n + ")" + ext);
			if (!isDir) { File.Move(path, dest); return dest; }
			// folders cannot be moved to another drive: copied, then the temp one goes with the work folder
			CopyFolder(path, dest);
			return dest;
		}

		static void CopyFolder(string from, string to)
		{
			Directory.CreateDirectory(to);
			foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
			foreach (var d in Directory.GetDirectories(from)) CopyFolder(d, Path.Combine(to, Path.GetFileName(d)));
		}

		/// <summary>The extension with the language and settings of DeskPals, no window.</summary>
		static ProcessStartInfo Start(string exe)
		{
			var start = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(exe) };
			start.ArgumentList.Add("--config");
			start.ArgumentList.Add(LocalData.ConfigPath());
			start.ArgumentList.Add("--lang=" + Lang.Current);
			return start;
		}

		/// <summary>The ids of the jobs the extension offers for this file, or null when it could not be asked.</summary>
		static List<string> ListJobs(string exe, string file)
		{
			try
			{
				var start = Start(exe);
				start.ArgumentList.Add("--list");
				start.ArgumentList.Add(file);
				start.RedirectStandardOutput = true;
				start.StandardOutputEncoding = Encoding.UTF8;
				using (var p = Process.Start(start))
				{
					var read = p.StandardOutput.ReadToEndAsync();
					if (!p.WaitForExit((int)ListTimeout.TotalMilliseconds)) { Kill(p); return null; }
					var arr = JsonNode.Parse(read.Result) as JsonArray;
					return (arr ?? new JsonArray()).Select(n => (string)n?["id"]).Where(s => s != null).ToList();
				}
			}
			catch (Exception ex) { LogFile.Ignored(ex); return null; }
		}

		/// <summary>The job, without any window ("--quiet"), and what it says it made.</summary>
		static (bool ok, List<string> outputs, string message) RunJob(string exe, string job, string file, string resultFile)
		{
			var outputs = new List<string>();
			try
			{
				var start = Start(exe);
				start.ArgumentList.Add("--do=" + job);
				start.ArgumentList.Add("--quiet");
				// not in the extension's list of last jobs: it would show the temp copy, gone a moment later
				start.ArgumentList.Add("--nohistory");
				start.ArgumentList.Add("--result=" + resultFile);
				start.ArgumentList.Add(file);
				using (var p = Process.Start(start))
				{
					if (!p.WaitForExit((int)JobTimeout.TotalMilliseconds)) { Kill(p); return (false, outputs, Lang.T("toolswatch.tooLong")); }
				}
				if (!File.Exists(resultFile)) return (false, outputs, "");
				var j = JsonNode.Parse(File.ReadAllText(resultFile));
				foreach (var o in j?["outputs"]?.AsArray() ?? new JsonArray()) if ((string)o != null && (File.Exists((string)o) || Directory.Exists((string)o))) outputs.Add((string)o);
				return ((bool?)j?["ok"] ?? false, outputs, (string)j?["message"] ?? "");
			}
			catch (Exception ex) { LogFile.Ignored(ex); return (false, outputs, ex.Message); }
		}

		static void Kill(Process p) { try { p.Kill(true); } catch (Exception ex) { LogFile.Ignored(ex); } }

		// ------------------------------------------------------------------ the pet tells it

		/// <summary>The queue is empty: one notice per folder and job ("I turned 3 files in Scans into PDF").</summary>
		static void Tell()
		{
			foreach (var ((folder, job), b) in batches.ToList())
			{
				string where = Path.GetFileName(folder.TrimEnd('\\'));
				if (string.IsNullOrEmpty(where)) where = folder;
				string outDir = b.OutDir;
				var show = outDir == null ? new SpeechBubble.Choice[0] : new[] { new SpeechBubble.Choice(Lang.T("tools.showFolder"), () => OpenFolder(outDir), true) };
				if (b.Done > 0)
				{
					string what = b.Done == 1 ? b.OneName : Lang.T("toolswatch.files", b.Done);
					string text = Lang.T("toolswatch.done." + job, what, where);
					if (text == "toolswatch.done." + job) text = Lang.T("toolswatch.done.other", what, where);
					Say(text, show);
				}
				if (b.Failed > 0)
				{
					string what = b.Failed == 1 ? b.FailName : Lang.T("toolswatch.files", b.Failed);
					Say(string.IsNullOrEmpty(b.Error) ? Lang.T("toolswatch.failedPlain", what, where) : Lang.T("toolswatch.failed", what, where, b.Error), new SpeechBubble.Choice[0]);
				}
			}
			batches.Clear();
		}

		static void Say(string text, SpeechBubble.Choice[] choices)
		{
			lock (Told) Told.Add(text);
			try { SpeechBubble.Notify(text, 12, choices); } catch (Exception ex) { LogFile.Ignored(ex); }
		}

		static void OpenFolder(string dir) { try { Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true }); } catch (Exception ex) { LogFile.Ignored(ex); } }

		// ------------------------------------------------------------------ memory

		static void Remember(string key)
		{
			if (!done.Add(key)) return;
			doneList.Add(key);
			if (doneList.Count > MaxRemembered) { done.Remove(doneList[0]); doneList.RemoveAt(0); }
		}

		static void LoadState()
		{
			done.Clear(); doneList.Clear(); knownFolders.Clear();
			try
			{
				if (!File.Exists(StateFile)) return;
				var j = JsonNode.Parse(File.ReadAllText(StateFile));
				// a file handled that is gone or was changed since is forgotten (its key would never be met again)
				foreach (var n in j?["done"] as JsonArray ?? new JsonArray())
					if (n is JsonValue v && v.TryGetValue(out string key) && StillThere(key)) Remember(key);
				foreach (var n in j?["folders"] as JsonArray ?? new JsonArray())
				{
					string folder = n?["folder"] is JsonValue f && f.TryGetValue(out string fs) ? fs : null;
					if (folder != null && n["since"] is JsonValue s && s.TryGetValue(out long ticks) && ticks > 0 && ticks <= DateTime.MaxValue.Ticks)
						knownFolders[folder] = new DateTime(ticks, DateTimeKind.Utc);
				}
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}

		/// <summary>The file of a remembered key is still there, the same size and date.</summary>
		static bool StillThere(string key)
		{
			int cut = key.LastIndexOf('|');
			cut = cut > 0 ? key.LastIndexOf('|', cut - 1) : -1;
			return cut > 0 && string.Equals(Key(key.Substring(0, cut)), key, StringComparison.OrdinalIgnoreCase);
		}

		static void SaveState()
		{
			try
			{
				var o = new JsonObject
				{
					["done"] = new JsonArray(doneList.Select(k => (JsonNode)k).ToArray()),
					["folders"] = new JsonArray(knownFolders.Select(k => (JsonNode)new JsonObject { ["folder"] = k.Key, ["since"] = k.Value.Ticks }).ToArray()),
				};
				Directory.CreateDirectory(Path.GetDirectoryName(StateFile));
				string tmp = StateFile + ".tmp";
				File.WriteAllText(tmp, o.ToJsonString());
				File.Move(tmp, StateFile, true);
			}
			catch (Exception ex) { LogFile.Ignored(ex); }
		}
	}
}
