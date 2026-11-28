using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DeskPalsTools
{
	/// <summary>
	/// Explorer starts one copy of the program per selected file. The first copy of a job (it holds a mutex named after
	/// the job) listens on a pipe for ~700 ms after the last path it got; the others send their paths there and quit at
	/// once. Then ONE job runs with every file, sorted by name like Explorer shows them. A copy that finds nobody listening
	/// takes the lead itself, so no file is ever lost.
	/// </summary>
	internal static class ExplorerGather
	{
		public const int Wait = 700;
		/// <summary>The most the first copy waits in all (a big selection on a busy PC starts its copies slowly).</summary>
		const int MaxGather = 60000;
		/// <summary>The most a later copy tries to hand its files over before doing the job alone (a bit more than the first
		/// copy listens, so it never gives up while the first one still takes files).</summary>
		const int MaxHandOver = MaxGather + 10000;
		/// <summary>Copies of the program started this long before the first one, or longer ago than <see cref="MaxYoung"/>,
		/// are not waited for: they belong to another selection (or are stuck).</summary>
		const int Before = 5000, MaxYoung = 30000;

		/// <summary>Every file of the selection (the first copy), or null when they were handed to the first copy (quit now).</summary>
		public static List<string> Collect(string job, IList<string> files, int wait = Wait)
		{
			string name = Name(job);
			using var mutex = new Mutex(false, @"Local\" + name);
			var clock = Stopwatch.StartNew();
			while (clock.ElapsedMilliseconds < MaxHandOver)
			{
				if (Take(mutex))
				{
					try { return Sorted(Task.Run(() => Listen(name, files, wait)).GetAwaiter().GetResult()); }
					finally { mutex.ReleaseMutex(); }
				}
				// the first copy holds the mutex while it listens: keep trying (it may be busy with another copy)
				if (Send(name, files)) return null;
				Thread.Sleep(50);
			}
			// nobody answered in time: do it with these files only
			return Sorted(files);
		}

		static bool Take(Mutex m)
		{
			try { return m.WaitOne(0); }
			catch (AbandonedMutexException) { return true; }
		}

		/// <summary>A name of its own for each job and Windows session (the pipe only accepts this user).</summary>
		static string Name(string job)
		{
			var safe = new string((job ?? "open").Where(ch => char.IsAsciiLetterOrDigit(ch) || ch == '-').ToArray());
			return "DeskPalsTools-gather-" + safe + "-" + Process.GetCurrentProcess().SessionId;
		}

		/// <summary>The first copy: takes the paths the others send until nobody came for <paramref name="wait"/> ms.</summary>
		static async Task<List<string>> Listen(string name, IList<string> mine, int wait)
		{
			var all = new List<string>(mine);
			var handed = new HashSet<int>();
			var siblings = new Siblings();
			var total = Stopwatch.StartNew();
			var quiet = Stopwatch.StartNew();
			while (total.ElapsedMilliseconds < MaxGather)
			{
				if (quiet.ElapsedMilliseconds >= wait)
				{
					// quiet for a while, but other copies of this selection may still be starting (a busy PC, many files)
					if (!siblings.Waiting(handed)) break;
					quiet.Restart();
					wait = Math.Min(wait, 300);
				}
				int left = (int)Math.Max(1, Math.Min(wait - quiet.ElapsedMilliseconds, MaxGather - total.ElapsedMilliseconds));
				using var server = new NamedPipeServerStream(name, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
				using (var cts = new CancellationTokenSource(left))
				{
					try { await server.WaitForConnectionAsync(cts.Token); }
					catch (OperationCanceledException) { continue; }
				}
				try
				{
					using var cts = new CancellationTokenSource(3000);
					var lines = await ReadLines(server, cts.Token);
					// the first line is the sender's process id: it is not waited for any more
					if (lines.Count > 0 && int.TryParse(lines[0], out int pid)) handed.Add(pid);
					all.AddRange(lines.Skip(1));
					await server.WriteAsync(new byte[] { 1 }, cts.Token);
					await server.FlushAsync(cts.Token);
				}
				catch (Exception) { /* a copy that broke off retries by itself */ }
				quiet.Restart();
			}
			return all;
		}

		/// <summary>
		/// The other copies started from Explorer for the same item, at about the same time as this one (same program,
		/// "--from-explorer" and the same "--do=" on their command line). While one of them is alive and has not handed its
		/// files over yet, the first copy keeps listening, so a big selection never becomes two jobs.
		/// </summary>
		sealed class Siblings
		{
			readonly string exe = Environment.ProcessPath, doArg;
			readonly DateTime myStart = Process.GetCurrentProcess().StartTime;
			readonly int me = Environment.ProcessId;
			readonly HashSet<int> others = new();

			public Siblings()
			{
				doArg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--do=")) ?? "";
			}

			public bool Waiting(HashSet<int> handed)
			{
				if (string.IsNullOrEmpty(exe)) return false;
				foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)))
				{
					using (p)
					{
						try
						{
							if (p.Id == me || handed.Contains(p.Id) || p.HasExited) continue;
							var start = p.StartTime;
							if (start < myStart.AddMilliseconds(-Before) || (DateTime.Now - start).TotalMilliseconds > MaxYoung) continue;
							if (others.Contains(p.Id) || IsSibling(p.Id)) { others.Add(p.Id); return true; }
						}
						catch (Exception) { /* gone, or not ours to look at */ }
					}
				}
				return false;
			}

			bool IsSibling(int pid)
			{
				string line = CommandLine(pid);
				if (line == null) return false;
				var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				return words.Contains("--from-explorer") && words.Contains(doArg);
			}
		}

		/// <summary>The command line of another process of this user (null when it cannot be read).</summary>
		static string CommandLine(int pid)
		{
			IntPtr h = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, pid);
			if (h == IntPtr.Zero) return null;
			try
			{
				int size = 4096;
				for (int tries = 0; tries < 3; tries++)
				{
					IntPtr buf = Marshal.AllocHGlobal(size);
					try
					{
						// ProcessCommandLineInformation (60): a UNICODE_STRING followed by its text
						int status = NtQueryInformationProcess(h, 60, buf, size, out int needed);
						if (status == 0)
						{
							int bytes = Marshal.ReadInt16(buf) & 0xFFFF;
							IntPtr text = Marshal.ReadIntPtr(buf, IntPtr.Size);
							return text == IntPtr.Zero ? "" : Marshal.PtrToStringUni(text, bytes / 2);
						}
						if (needed <= size) return null;
						size = needed;
					}
					finally { Marshal.FreeHGlobal(buf); }
				}
				return null;
			}
			finally { CloseHandle(h); }
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr OpenProcess(int access, bool inherit, int pid);
		[DllImport("kernel32.dll")]
		static extern bool CloseHandle(IntPtr h);
		[DllImport("ntdll.dll")]
		static extern int NtQueryInformationProcess(IntPtr process, int infoClass, IntPtr info, int length, out int returned);

		/// <summary>Lines up to an empty line.</summary>
		static async Task<List<string>> ReadLines(Stream s, CancellationToken cancel)
		{
			var got = new List<byte>();
			var buf = new byte[4096];
			while (true)
			{
				int n = await s.ReadAsync(buf, cancel);
				if (n == 0) break;
				got.AddRange(buf.Take(n));
				int c = got.Count;
				if (c >= 2 && got[c - 1] == '\n' && got[c - 2] == '\n') break;
			}
			return Encoding.UTF8.GetString(got.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
		}

		/// <summary>A later copy: gives its paths to the first one. False when nobody is listening (then it may lead).</summary>
		static bool Send(string name, IList<string> files)
		{
			try
			{
				using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
				client.Connect(400);
				return Task.Run(async () =>
				{
					using var cts = new CancellationTokenSource(3000);
					// who sends (so the first copy stops waiting for it), then its paths
					var data = Encoding.UTF8.GetBytes(Environment.ProcessId + "\n" + string.Join("\n", files) + "\n\n");
					await client.WriteAsync(data, cts.Token);
					await client.FlushAsync(cts.Token);
					var ack = new byte[1];
					return await client.ReadAsync(ack, cts.Token) == 1 && ack[0] == 1;
				}).GetAwaiter().GetResult();
			}
			catch (Exception) { return false; }
		}

		/// <summary>Each file once, by name as Explorer sorts them ("2" before "10"), then by folder.</summary>
		public static List<string> Sorted(IEnumerable<string> files)
		{
			var list = files.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			list.Sort((a, b) =>
			{
				int r = StrCmpLogicalW(Path.GetFileName(a.TrimEnd('\\')), Path.GetFileName(b.TrimEnd('\\')));
				return r != 0 ? r : StrCmpLogicalW(a, b);
			});
			return list;
		}

		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		static extern int StrCmpLogicalW(string a, string b);
	}

	/// <summary>
	/// What runs when Explorer starts the program ("--from-explorer"): gathers the files of the selection, then opens the
	/// window ("open", or a job that does not fit these files) or does the job with its small progress window, and says
	/// how it went (the result shown in its folder, or the problem in a message).
	/// </summary>
	internal static class ExplorerHook
	{
		public static int Run(string[] args, List<string> files)
		{
			string job = args.FirstOrDefault(a => a.StartsWith("--do="))?[5..] ?? "open";
			var all = ExplorerGather.Collect(job, files);
			if (all == null) return 0;
			// the menu that started us may point to an old place of the program: it follows it now. Only the place is
			// compared here: Explorer does not pass DeskPals' settings, so this copy may speak another language than DeskPals
			ExplorerMenu.Refresh(placeOnly: true);
			all = all.Where(f => File.Exists(f) || Directory.Exists(f)).ToList();
			bool quiet = args.Contains("--quiet");
			var def = job == "open" ? null : Jobs.Find(job);
			if (def == null || all.Count == 0 || Rank(def, all) < 0)
			{
				// "Open DeskPals Tools", or a job that does not fit (e.g. "Convert to PDF" on a song): the window shows what does
				if (quiet) return 1;
				Application.Run(new MainForm(all));
				return 0;
			}
			var given = args.Where(a => a.StartsWith("--opt:") && a.Contains('=')).ToDictionary(a => a[6..a.IndexOf('=')], a => a[(a.IndexOf('=') + 1)..]);
			if (quiet)
			{
				if (def.Ask != null && given.Count == 0) return 1;
				var q = Task.Run(() => Jobs.Run(job, all, null, default, given)).GetAwaiter().GetResult();
				return q.ok ? 0 : 1;
			}
			var form = new ProgressForm(job, all, given.Count > 0 ? given : null);
			Application.Run(form);
			var r = form.Result ?? new Jobs.Result { ok = false, message = T.S("err.canceled") };
			Tell(r);
			return r.ok ? 0 : 1;
		}

		static int Rank(Jobs.JobDef def, IList<string> files)
		{
			try { return def.Rank?.Invoke(files) ?? -1; } catch (Exception) { return -1; }
		}

		/// <summary>Done: the first file made is shown in its folder; problems are told (but not "Canceled", the user knows).</summary>
		static void Tell(Jobs.Result r)
		{
			if (!string.IsNullOrEmpty(r.message) && r.message != T.S("err.canceled"))
				MessageBox.Show(r.message, T.S("app"), MessageBoxButtons.OK, r.ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
			else if (!r.ok && r.message != T.S("err.canceled"))
				MessageBox.Show(T.S("main.failed"), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
			if (r.ok && r.outputs.Count > 0) MainForm.Reveal(r.outputs[0]);
		}

		/// <summary>
		/// Only for the tests: a copy started by the gathering test (with <see cref="ExplorerMenu.TestVariable"/> set to its
		/// folder, and "--quiet --do=explorer-gather-test") goes through Program's normal "--do" path into a test job, which
		/// then runs the Explorer path (<see cref="Run"/>, what the Program.cs hook calls) with a second test job that writes
		/// down the files it got. History and settings stay in that folder. Without the variable nothing is registered.
		/// (Not from the module initializer itself: the pipes use other threads, which wait for it to finish.)
		/// </summary>
		[ModuleInitializer]
		internal static void TestEntry()
		{
			string dir = Environment.GetEnvironmentVariable(ExplorerMenu.TestVariable);
			var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || !args.Contains("--from-explorer") || !args.Contains("--do=" + TestJob)) return;
			ToolSettings.File = Path.Combine(dir, "tools.json");
			History.File = Path.Combine(dir, "tools-history.json");
			string record = Path.Combine(dir, "record.txt");
			Jobs.Register(new Jobs.JobDef
			{
				Id = TestJob, Emoji = "🧪", Rank = f => 0,
				Run = c =>
				{
					var explorerArgs = args.Select(a => a == "--do=" + TestJob ? "--do=" + RecordJob : a).ToArray();
					Environment.Exit(Run(explorerArgs, c.Files.ToList()));
					return Task.CompletedTask;
				},
			});
			Jobs.Register(new Jobs.JobDef
			{
				Id = RecordJob, Emoji = "🧪", Rank = f => 0,
				Run = c =>
				{
					// one line per job that ran: the files it got, in order
					using (var lk = new Mutex(false, @"Local\DeskPalsTools-explorer-test-record"))
					{
						bool held;
						try { held = lk.WaitOne(5000); } catch (AbandonedMutexException) { held = true; }
						try { File.AppendAllText(record, string.Join("|", c.Files.Select(Path.GetFileName)) + "\n"); }
						finally { if (held) lk.ReleaseMutex(); }
					}
					c.Outputs.Add(record);
					return Task.CompletedTask;
				},
			});
		}

		public const string TestJob = "explorer-gather-test", RecordJob = "explorer-gather-record";
	}
}
