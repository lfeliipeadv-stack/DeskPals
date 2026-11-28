using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.CompilerServices;
using DesktopPet;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of the window: made off-screen on a thread of its own (windows need one), with pictures made in the test's
	/// folder. The order of the files is changed with the same buttons and methods the user has, and a job registered only
	/// for the test says in which order it got them. Pictures of the window (light and dark) go next to the report.
	/// </summary>
	internal static class WindowTests
	{
		const string FakeJob = "windowtest-order";

		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("window icon", Icon);
			DevTests.Add("window files order", Order);
			DevTests.Add("window long list and keyboard", LongList);
			DevTests.Add("window files dropped during a job", WhileBusy);
			DevTests.Add("window many jobs fit", ManyJobs);
			DevTests.Add("window recent jobs", Recent);
			DevTests.Add("window settings", Settings);
			DevTests.Add("window pictures", Pictures);
			DevTests.Add("window texts", Texts);
		}

		// ------------------------------------------------------------------ helpers

		/// <summary>Run this on a new single-threaded (STA) thread with a message loop, as a window needs.</summary>
		/// <summary>
		/// The Windows objects made on this window thread (thumbnails, PDF drawing) let go while the thread is still there:
		/// let go later, from another thread, they can keep the program from ending.
		/// </summary>
		static void ReleaseHere()
		{
			for (int i = 0; i < 2; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
			Application.DoEvents();
		}

		static Task OnWindowThread(Func<Task> work) => UiThread.Run(work);

		/// <summary>The window, shown far off the screen (so its parts are laid out and can be drawn) and not in the taskbar.</summary>
		static MainForm Window(IEnumerable<string> files = null)
		{
			var f = new MainForm(files ?? Array.Empty<string>()) { StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000), ShowInTaskbar = false };
			f.Show();
			Application.DoEvents();
			return f;
		}

		/// <summary>Let the window handle what is waiting (BeginInvoke, layout).</summary>
		static async Task Settle(int ms = 60)
		{
			Application.DoEvents();
			await Task.Delay(ms);
			Application.DoEvents();
		}

		/// <summary>A job that only writes down the files it got, in their order. Removed after the test (it has no texts).</summary>
		static List<string> RegisterFake(string dir)
		{
			var got = new List<string>();
			Jobs.Register(new Jobs.JobDef
			{
				Id = FakeJob, Emoji = "🧪",
				Rank = f => f.All(x => x.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) ? 5 : -1,
				Run = c =>
				{
					lock (got) { got.Clear(); got.AddRange(c.Files); }
					string o = Path.Combine(dir, "order-" + Guid.NewGuid().ToString("N")[..6] + ".txt");
					File.WriteAllLines(o, c.Files);
					c.Outputs.Add(o);
					return Task.CompletedTask;
				},
			});
			return got;
		}

		static void Unregister(string id)
		{
			var field = typeof(Jobs).GetField("registry", BindingFlags.NonPublic | BindingFlags.Static);
			var registry = (List<Jobs.JobDef>)field.GetValue(null);
			lock (registry) registry.RemoveAll(d => d.Id == id);
		}

		static List<Prefs.Pref> PrefList()
		{
			var field = typeof(Prefs).GetField("list", BindingFlags.NonPublic | BindingFlags.Static);
			return (List<Prefs.Pref>)field.GetValue(null);
		}

		static string Names(IEnumerable<string> files) => string.Join(",", files.Select(Path.GetFileNameWithoutExtension));

		/// <summary>Where the pictures of the window go: next to the report ("--devtests=…"), else the temp folder.</summary>
		static string PictureFolder()
		{
			string arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--devtests=", StringComparison.Ordinal));
			string report = arg?[(arg.IndexOf('=') + 1)..].Trim('"');
			string dir = string.IsNullOrEmpty(report) ? Path.GetTempPath() : Path.GetDirectoryName(Path.GetFullPath(report));
			return Directory.Exists(dir) ? dir : Path.GetTempPath();
		}

		/// <summary>The 256-pixel frame of an .ico (its size byte is 0), opened from its PNG bytes; null when it is not one.</summary>
		static Bitmap BigFrame(byte[] ico)
		{
			int count = BitConverter.ToInt16(ico, 4);
			for (int i = 0; i < count; i++)
			{
				int at = 6 + i * 16;
				if (ico[at] != 0) continue;
				int length = BitConverter.ToInt32(ico, at + 8), offset = BitConverter.ToInt32(ico, at + 12);
				if (ico[offset] != 0x89 || ico[offset + 1] != (byte)'P') return null;
				using var ms = new MemoryStream(ico, offset, length);
				return new Bitmap(ms);
			}
			return null;
		}

		/// <summary>A picture is not all one colour (something was drawn).</summary>
		static bool HasDrawing(Bitmap bmp)
		{
			var colours = new HashSet<int>();
			for (int y = 0; y < bmp.Height; y += 7) for (int x = 0; x < bmp.Width; x += 7) colours.Add(bmp.GetPixel(x, y).ToArgb());
			return colours.Count > 8;
		}

		// ------------------------------------------------------------------ the icon

		static Task Icon(DevTests.Ctx t)
		{
			string ico = Path.Combine(t.Dir, "icon.ico");
			t.Check("icon: --make-icon writes the file", ToolIcon.MakeIcon(ico) == 0 && File.Exists(ico));
			var bytes = File.ReadAllBytes(ico);
			int count = BitConverter.ToInt16(bytes, 4);
			var sizes = Enumerable.Range(0, count).Select(i => bytes[6 + i * 16] == 0 ? 256 : bytes[6 + i * 16]).ToArray();
			t.Check("icon: 16, 20, 24, 32, 40, 48, 64 and 256 pixels", sizes.SequenceEqual(ToolIcon.Sizes), string.Join(",", sizes));
			using (var small = new System.Drawing.Icon(ico, 16, 16))
			using (var bmp = small.ToBitmap())
			{
				// at 16 pixels it is still a green tile with something white on it
				int green = 0, white = 0;
				for (int y = 0; y < bmp.Height; y++) for (int x = 0; x < bmp.Width; x++)
					{
						var c = bmp.GetPixel(x, y);
						if (c.A > 200 && c.G > c.R + 40 && c.G > c.B + 20) green++;
						if (c.A > 200 && c.R > 220 && c.G > 220 && c.B > 220) white++;
					}
				t.Check("icon: readable at 16 px (green tile, white wrench)", bmp.Width == 16 && green > 60 && white > 15, $"green {green}, white {white}");
			}
			// System.Drawing skips the PNG frame of an .ico (Windows itself reads it): the 256 one is read as the PNG it is
			using (var big = BigFrame(bytes)) t.Check("icon: the 256 px one is a PNG that opens", big != null && big.Width == 256 && big.Height == 256, big == null ? "none" : big.Width.ToString());
			t.Check("icon: the window gets it", ToolIcon.ForWindow().Width > 0);
			string shots = PictureFolder();
			using (var sheet = new Bitmap(16 + 20 + 24 + 32 + 40 + 48 + 64 + 256 + 9 * 8, 256 + 16))
			{
				using (var g = Graphics.FromImage(sheet))
				{
					g.Clear(Color.FromArgb(240, 240, 240));
					int x = 8;
					foreach (int s in ToolIcon.Sizes) { using var b = ToolIcon.Draw(s); g.DrawImage(b, x, 8, s, s); x += s + 8; }
				}
				string png = Path.Combine(shots, "window-icon-sizes.png");
				sheet.Save(png, ImageFormat.Png);
				t.Info("icon sizes picture: " + png);
			}
			return Task.CompletedTask;
		}

		// ------------------------------------------------------------------ the order of the files

		static Task Order(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			string a = t.Picture("a.png", "A", Color.Red, 300, 200), b = t.Picture("b.png", "B", Color.Blue, 300, 200), c = t.Picture("c.png", "C", Color.Lime, 300, 200);
			var got = RegisterFake(t.Dir);
			try
			{
				using var f = Window(new[] { a, b });
				t.Check("order: the files given at the start are listed", Names(f.Files) == "a,b" && f.List.Items.Count == 2, Names(f.Files));
				f.Add(new[] { c, a });
				t.Check("order: a file already there is not added twice", Names(f.Files) == "a,b,c", Names(f.Files));

				// the buttons, as the user clicks them
				f.SelectFile(2);
				t.Check("order: ↑ is there for the last file, ↓ is not", f.UpButton.Enabled && !f.DownButton.Enabled);
				f.UpButton.PerformClick();
				t.Check("order: ↑ moves the file up (and keeps it selected)", Names(f.Files) == "a,c,b" && f.List.SelectedIndex == 1, Names(f.Files) + " sel " + f.List.SelectedIndex);
				f.UpButton.PerformClick();
				t.Check("order: ↑ again: it is the first", Names(f.Files) == "c,a,b" && !f.UpButton.Enabled, Names(f.Files));
				f.DownButton.PerformClick();
				t.Check("order: ↓ moves it down", Names(f.Files) == "a,c,b", Names(f.Files));

				// dragging inside the list (the line where it is let go: 0 = before the first, 3 = after the last)
				f.List.DropMove(0, 3);
				t.Check("order: dragging the first file to the end", Names(f.Files) == "c,b,a", Names(f.Files));
				f.List.DropMove(2, 0);
				t.Check("order: dragging the last file to the top", Names(f.Files) == "a,c,b", Names(f.Files));
				f.List.DropMove(1, 2);
				t.Check("order: letting go where it was changes nothing", Names(f.Files) == "a,c,b", Names(f.Files));
				var r1 = f.List.GetItemRectangle(1);
				t.Check("order: the drop line follows the mouse (upper half: before, lower half: after)", f.List.LineAt(new Point(10, r1.Top + 3)) == 1 && f.List.LineAt(new Point(10, r1.Bottom - 3)) == 2);
				t.Check("order: the list shows the same order", Names(f.List.Files) == Names(f.Files));

				// the job gets them in this order
				var job = f.JobButtons.FirstOrDefault(x => x.Name == FakeJob);
				t.Check("order: the jobs for the files are offered", job != null && f.JobButtons.Any(x => x.Name == "merge"), string.Join(",", f.JobButtons.Select(x => x.Name)));
				await f.Do(FakeJob);
				t.Check("order: the job gets the files in the list's order", Names(got) == "a,c,b", Names(got));

				// joining follows it too: three pages, each the colour of its picture (a red, c green, b blue), in the list's order
				await f.Do("merge");
				var merged = History.Read().FirstOrDefault(e => e.job == "merge")?.outputs.FirstOrDefault();
				t.Check("order: joining makes one PDF with a page per file", merged != null && PdfWork.Pages(merged) == 3, merged ?? "none");
				string colours = merged == null ? "none" : string.Join(",", await PageColours(merged));
				t.Check("order: the joined pages follow the list (a, c, b = red, green, blue)", colours == "red,green,blue", colours);

				// removing, and files dropped from Explorer at a place of the list
				f.SelectFile(1);
				f.RemoveButton.PerformClick();
				t.Check("order: Remove takes the file out of the list only", Names(f.Files) == "a,b" && File.Exists(c), Names(f.Files));
				f.Add(new[] { c }, 0);
				t.Check("order: files dropped on the list go where they are let go", Names(f.Files) == "c,a,b", Names(f.Files));
				await f.Do(FakeJob);
				t.Check("order: and the job follows the new order", Names(got) == "c,a,b", Names(got));
				// each file asked once at the list's size, then again once the thread that makes them is done
				int thumbSize = Math.Max(32, (f.List.ItemHeight - Look.Px(f.List, 10)) * 2);
				foreach (var x in f.Files) Thumbnails.Get(x, thumbSize);
				bool idle = Thumbnails.WaitIdle(TimeSpan.FromSeconds(15));
				var missing = f.Files.Where(x => Thumbnails.Get(x, thumbSize) == null).Select(Path.GetFileName).ToList();
				t.Check("order: a thumbnail is made for each file", idle && missing.Count == 0, (idle ? "" : "still busy; ") + string.Join(",", missing));
			}
			finally { Unregister(FakeJob); }
		});

		/// <summary>The colour of each page of a PDF (as Windows draws it): "red", "green", "blue" or "other".</summary>
		static async Task<List<string>> PageColours(string pdf)
		{
			var result = new List<string>();
			using var input = File.OpenRead(pdf);
			var doc = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			for (uint i = 0; i < doc.PageCount; i++)
			{
				using var page = doc.GetPage(i);
				using var ms = new InMemoryRandomAccessStream();
				await page.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions { DestinationWidth = 240 });
				ms.Seek(0);
				using var stream = ms.AsStreamForRead();
				using var bmp = new Bitmap(stream);
				int red = 0, green = 0, blue = 0;
				for (int y = 0; y < bmp.Height; y += 2) for (int x = 0; x < bmp.Width; x += 2)
					{
						var c = bmp.GetPixel(x, y);
						if (c.R > 160 && c.G < 110 && c.B < 110) red++;
						else if (c.G > 160 && c.R < 110 && c.B < 110) green++;
						else if (c.B > 160 && c.R < 110 && c.G < 110) blue++;
					}
				int most = Math.Max(red, Math.Max(green, blue));
				result.Add(most < 50 ? "other" : most == red ? "red" : most == green ? "green" : "blue");
			}
			return result;
		}

		/// <summary>
		/// A picture of the window. Off the screen it is first painted here, and only then are its thumbnails asked for:
		/// it is drawn once, the thumbnails are waited for, and drawn again (so the picture shows them, not the emoji).
		/// </summary>
		static async Task<Bitmap> Shot(Form f)
		{
			var bmp = new Bitmap(f.Width, f.Height);
			f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
			Thumbnails.WaitIdle(TimeSpan.FromSeconds(10));
			await Settle(80);
			f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
			return bmp;
		}

		/// <summary>The controls Tab goes through, in order, from the window's start.</summary>
		static List<Control> TabWalk(Form f)
		{
			var seen = new List<Control>();
			Control c = null;
			for (int i = 0; i < 400; i++)
			{
				c = f.GetNextControl(c, true);
				if (c == null) break;
				if (c.CanSelect && c.TabStop) seen.Add(c);
			}
			return seen;
		}

		// ------------------------------------------------------------------ a long list, and the keyboard

		static Task LongList(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			var pics = Enumerable.Range(1, 10).Select(i => t.Picture($"scan{i:00}.png", i.ToString(), Color.White, 200, 280)).ToArray();
			using var f = Window(pics);
			await Settle();
			var l = f.List;
			t.Check("long list: at 720×600 at least three files are seen whole", l.ClientSize.Height >= l.ItemHeight * 3, $"{l.ClientSize.Height} / {l.ItemHeight}");
			l.TopIndex = 0;
			bool hidden = !l.LastInView;
			if (!hidden) t.Skip("long list: all 10 files fit (a big screen), nothing to scroll");
			else
			{
				l.TopIndex = 0;
				t.Check("long list: dragging near the bottom edge scrolls down", l.EdgeAt(new Point(20, l.ClientSize.Height - 3)) == 1);
				t.Check("long list: in the middle it does not", l.EdgeAt(new Point(20, l.ClientSize.Height / 2)) == 0);
				t.Check("long list: at the top, already at the start, it does not", l.EdgeAt(new Point(20, 2)) == 0);
				l.DragScroll(1); l.DragScroll(1);
				t.Check("long list: it scrolls one file at a time", l.TopIndex == 2, l.TopIndex.ToString());
				t.Check("long list: then near the top edge it scrolls up", l.EdgeAt(new Point(20, 2)) == -1);
				// let go after scrolling: the file goes to the line under the mouse, as it is now
				int line = l.LineAt(new Point(20, l.GetItemRectangle(l.TopIndex + 1).Top + 2));
				l.DropMove(0, line);
				t.Check("long list: a file dragged down to a place that was out of view lands there", Path.GetFileNameWithoutExtension(f.Files[line - 1]) == "scan01", Names(f.Files));
			}

			// Tab goes as the eye does: the parts at the top, the list and its buttons, then the jobs
			f.SelectFile(1);
			var walk = TabWalk(f);
			int tab = walk.FindIndex(c => c is Button b && b.AccessibleRole == AccessibleRole.PageTab);
			int list = walk.IndexOf(f.List), side = walk.IndexOf(f.RemoveButton);
			int job = walk.FindIndex(c => f.JobButtons.Contains(c));
			t.Check("keyboard: Tab goes tabs → list → its buttons → jobs", tab >= 0 && tab < list && list < side && side < job, $"tab {tab}, list {list}, side {side}, job {job}: " + string.Join(" > ", walk.Select(c => c.AccessibleName ?? c.Text)));
		});

		// ------------------------------------------------------------------ files dropped while a job runs

		static Task WhileBusy(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			const string slow = "windowtest-slow";
			string a = t.Picture("a.png", "A", Color.White, 200, 200), extra = t.Picture("late.png", "L", Color.White, 200, 200);
			var release = new TaskCompletionSource();
			Jobs.Register(new Jobs.JobDef
			{
				Id = slow, Emoji = "🐢",
				Rank = f => f.All(x => x.StartsWith(t.Dir, StringComparison.OrdinalIgnoreCase)) ? 5 : -1,
				Run = async c => await release.Task.WaitAsync(TimeSpan.FromSeconds(30)),
			});
			try
			{
				using var f = Window(new[] { a });
				var running = f.Do(slow);
				await Settle();
				f.Add(new[] { extra });
				string early = t.Picture("early.png", "E", Color.White, 200, 200);
				// dropped at the top of the list: it will go there, not at the end
				f.Add(new[] { early }, 0);
				t.Check("busy: a file dropped during a job waits (the list is not changed under the job)", !f.Files.Contains(extra), Names(f.Files));
				t.Check("busy: and the user is told it will be added", f.CountLabel.Text == T.S("window.busy"), f.CountLabel.Text);
				release.TrySetResult();
				await running;
				await Settle();
				t.Check("busy: once the job is done, it is added", f.Files.Contains(extra) && f.Files.Count == 3, Names(f.Files));
				t.Check("busy: a file dropped at a place during the job goes to that place", f.Files.FirstOrDefault() == early && f.Files.Last() == extra, Names(f.Files));
				t.Check("busy: and the message goes away", f.CountLabel.Text != T.S("window.busy"), f.CountLabel.Text);
			}
			finally { release.TrySetResult(); Unregister(slow); }
		});

		// ------------------------------------------------------------------ many jobs: every button is seen

		static Task ManyJobs(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			// a PDF with fourteen jobs in all (the real ones plus stand-ins): all the features give a PDF, and a few more
			string pdf = await t.Pdf("one.pdf", "ONE");
			int real = Jobs.Plan(new List<string> { pdf }).Count;
			var ids = Enumerable.Range(1, Math.Max(0, 14 - real)).Select(i => "windowtest-many-" + i).ToList();
			foreach (var id in ids)
				Jobs.Register(new Jobs.JobDef { Id = id, Emoji = "🧩", Rank = f => f.All(x => x.StartsWith(t.Dir, StringComparison.OrdinalIgnoreCase)) ? 50 : -1, Run = c => Task.CompletedTask });
			// the jobs are grouped: folded, the first few and "More" are seen whole at every size; opened (all of them, under
			// their groups' titles) they are seen whole in a big window, and in a small one the list may scroll
			bool wasExpanded = ToolSettings.Bool("jobsExpanded");
			ToolSettings.Set("jobsExpanded", false);
			try
			{
				using var f = Window(new[] { pdf });
				await Settle();
				void Seen(string when)
				{
					var area = f.JobsArea;
					var buttons = f.JobButtons.Append(f.MoreButton).Where(b => b != null).ToList();
					var cut = buttons.Where(b => b.Top < 0 || b.Left < 0 || b.Bottom > area.ClientSize.Height || b.Right > area.ClientSize.Width).Select(b => b.Name).ToList();
					int want = when.StartsWith("opened") ? 14 : JobGroups.Top + 1;
					t.Check($"many jobs: {when}: all {buttons.Count} job buttons are seen whole", buttons.Count >= want && cut.Count == 0 && !area.VerticalScroll.Visible, $"{buttons.Count} buttons, cut: {string.Join(",", cut)}; area {area.ClientSize}");
					t.Check($"many jobs: {when}: there is still room for what is made", f.DoneArea.Height >= Look.Px(f, 44) && f.List.Height >= Look.Px(f, 100), $"done {f.DoneArea.Height}, list {f.List.Height}");
				}
				Seen("720×600");
				using (var bmp = await Shot(f))
				{
					string png = Path.Combine(PictureFolder(), "window-light-many-jobs.png");
					bmp.Save(png, ImageFormat.Png);
					t.Info("window picture: " + png);
				}
				f.ClientSize = new Size(1880, 980);
				await Settle(150);
				Seen("maximised");
				f.ClientSize = new Size(720, 600);
				await Settle(150);
				Seen("back to 720×600");
				f.ClientSize = new Size(1880, 980);
				f.MoreButton?.PerformClick();
				await Settle(150);
				Seen("opened, maximised");
			}
			finally { foreach (var id in ids) Unregister(id); ToolSettings.Set("jobsExpanded", wasExpanded); }
		});

		// ------------------------------------------------------------------ recent jobs

		static Task Recent(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			History.Clear();
			string a = t.Picture("a.png", "A", Color.White, 300, 200);
			var got = RegisterFake(t.Dir);
			try
			{
				using var f = Window(new[] { a });
				f.ShowPart(MainForm.Part.Recent);
				await Settle();
				t.Check("recent: empty, it says so", f.Recent.ShowsEmpty && f.Recent.Cards.Count == 0 && !f.Recent.ClearButton.Enabled);
				f.ShowPart(MainForm.Part.Files);
				await f.Do(FakeJob);
				await f.Do("topdf");
				f.ShowPart(MainForm.Part.Recent);
				await Settle();
				var cards = f.Recent.Cards;
				string text(Control c) => string.Join(" | ", c.Controls.Cast<Control>().SelectMany(x => x.Controls.Cast<Control>().Prepend(x)).Select(x => x.Text));
				t.Check("recent: the jobs done are listed, the newest first", cards.Count == 2 && text(cards[0]).Contains(T.S("job.topdf")), string.Join(" // ", cards.Select(text)));
				t.Check("recent: each file made has Open and Show in folder", cards.Count > 0 && text(cards[0]).Contains("a.pdf") && text(cards[0]).Contains(T.S("open")) && text(cards[0]).Contains(T.S("showFolder")), cards.Count > 0 ? text(cards[0]) : "");
				t.Check("recent: it says when", cards.Count > 0 && text(cards[0]).Contains(T.S("window.today", "").Trim().TrimEnd(',', '，', '،')));

				// a job done while the page is open appears by itself (History.Changed)
				f.ShowPart(MainForm.Part.Files);
				await f.Do(FakeJob);
				f.ShowPart(MainForm.Part.Recent);
				await Settle();
				t.Check("recent: a new job appears by itself", f.Recent.Cards.Count == 3, f.Recent.Cards.Count.ToString());
				History.Add(FakeJob, new[] { a }, new Jobs.Result { ok = true, outputs = { Path.Combine(t.Dir, "gone.pdf") } });
				await Settle(120);
				t.Check("recent: refreshed while shown", f.Recent.Cards.Count == 4, f.Recent.Cards.Count.ToString());
				t.Check("recent: a file no longer there says so", text(f.Recent.Cards[0]).Contains(T.S("window.recent.gone")), text(f.Recent.Cards[0]));

				f.Recent.ClearHistory(false);
				await Settle();
				t.Check("recent: Clear history empties the list", f.Recent.Cards.Count == 0 && f.Recent.ShowsEmpty && History.Read().Count == 0);
				t.Check("recent: and the files made stay", File.Exists(Path.Combine(t.Dir, "a.pdf")));
				t.Check("recent: dates said simply", Look.When(new DateTime(2026, 1, 2, 9, 5, 0), new DateTime(2026, 1, 3, 12, 0, 0)).StartsWith(T.S("window.yesterday", "").Trim().TrimEnd(',', '，', '،')));
			}
			finally { Unregister(FakeJob); }
		});

		// ------------------------------------------------------------------ settings

		static Task Settings(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			var list = PrefList();
			List<Prefs.Pref> before;
			lock (list) { before = list.ToList(); list.Clear(); }
			try
			{
				using var f = Window();
				f.ShowPart(MainForm.Part.Settings);
				await Settle();
				t.Check("settings: with no settings it says so", f.SettingsPart.ShowsEmpty && f.SettingsPart.Cards.Count == 0);

				bool on = false; int clicks = 0;
				Prefs.Register(new Prefs.Pref { Key = "windowtest.switch", TextKey = "window.tab.files", HintKey = "window.order", Get = () => on, Set = v => on = v, Order = 1 });
				Prefs.Register(new Prefs.Pref { Key = "windowtest.both", TextKey = "window.recent.title", HintKey = "window.recent.empty", Get = () => true, Set = v => throw new UnauthorizedAccessException(), ButtonKey = "main.choose", Click = w => clicks++, Order = 2 });
				Prefs.Register(new Prefs.Pref { Key = "windowtest.button", TextKey = "window.settings.title", ButtonKey = "open", Click = w => clicks += 10, Order = 3 });
				f.ShowPart(MainForm.Part.Files);
				f.ShowPart(MainForm.Part.Settings);
				await Settle();
				var cards = f.SettingsPart.Cards;
				t.Check("settings: one line per setting, in order", cards.Select(c => c.Name).SequenceEqual(new[] { "windowtest.switch", "windowtest.both", "windowtest.button" }) && !f.SettingsPart.ShowsEmpty, string.Join(",", cards.Select(c => c.Name)));
				Control[] all(Control c) => c.Controls.Cast<Control>().SelectMany(x => all(x).Prepend(x)).ToArray();
				var sw = all(cards[0]).OfType<Switch>().Single();
				t.Check("settings: the text and the hint are shown", all(cards[0]).OfType<Label>().Any(l => l.Text == T.S("window.tab.files")) && all(cards[0]).OfType<Label>().Any(l => l.Text == T.S("window.order")));
				sw.Checked = !sw.Checked;
				t.Check("settings: the switch changes the setting", on && sw.Checked);
				sw.Checked = !sw.Checked;
				t.Check("settings: and back", !on && !sw.Checked);
				t.Check("settings: a switch and a button together", all(cards[1]).OfType<Switch>().Count() == 1 && all(cards[1]).OfType<Button>().Count() == 1);
				all(cards[1]).OfType<Button>().First(b => b.Text == T.S("main.choose")).PerformClick();
				all(cards[2]).OfType<Button>().First().PerformClick();
				t.Check("settings: the buttons do their thing", clicks == 11, clicks.ToString());
				t.Check("settings: only a button when there is no switch", !all(cards[2]).OfType<Switch>().Any());
			}
			finally
			{
				lock (list) { list.Clear(); list.AddRange(before); }
			}
		});

		// ------------------------------------------------------------------ pictures of the window, light and dark

		static Task Pictures(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			string mode = Theme.Mode;
			string folder = PictureFolder();
			var made = new List<string>();
			History.Clear();
			string a = t.Picture("Contract scan.png", "PAGE 1", Color.White, 900, 1200);
			string b = t.Picture("Receipt.jpg", "PAGE 2", Color.LightYellow, 900, 1200);
			string pdf = await t.Pdf("Report.pdf", "REPORT");
			var list = PrefList();
			List<Prefs.Pref> before;
			lock (list) before = list.ToList();
			// two settings of the kind the other features have, when none is registered yet
			if (before.Count == 0)
			{
				Prefs.Register(new Prefs.Pref { Key = "windowtest.pic1", TextKey = "window.tab.recent", HintKey = "window.recent.empty", Get = () => true, Set = v => { } });
				Prefs.Register(new Prefs.Pref { Key = "windowtest.pic2", TextKey = "window.settings.title", HintKey = "window.order", ButtonKey = "main.choose", Click = w => { } });
			}
			try
			{
				foreach (string look in new[] { "light", "dark" })
				{
					Theme.Mode = look;
					using var f = Window(new[] { a, b, pdf });
					await f.Do("topdf");
					Thumbnails.WaitIdle(TimeSpan.FromSeconds(10));
					foreach (var part in new[] { MainForm.Part.Files, MainForm.Part.Recent, MainForm.Part.Settings })
					{
						f.ShowPart(part);
						await Settle(150);
						using var bmp = await Shot(f);
						string png = Path.Combine(folder, $"window-{look}-{part.ToString().ToLowerInvariant()}.png");
						bmp.Save(png, ImageFormat.Png);
						made.Add(png);
						t.Check($"pictures: {look} {part} drawn", HasDrawing(bmp));
						// the background is the theme's: dark is dark, light is light
						var corner = bmp.GetPixel(bmp.Width / 2, bmp.Height - 20);
						t.Check($"pictures: {look} {part} in the {look} colours", look == "dark" ? corner.GetBrightness() < 0.3f : corner.GetBrightness() > 0.7f, corner.ToString());
					}
					// maximised (as big as a full-HD screen), the list grows and nothing is cut
					f.ClientSize = new Size(1880, 980);
					f.ShowPart(MainForm.Part.Files);
					await Settle(150);
					t.Check($"pictures: {look} big window gives the list more room", f.List.Height > Look.Px(f, 250), f.List.Height.ToString());
					if (look == "light")
					{
						using var bmp = await Shot(f);
						string png = Path.Combine(folder, "window-light-files-big.png");
						bmp.Save(png, ImageFormat.Png);
						made.Add(png);
					}
					f.Close();
				}
			}
			finally
			{
				Theme.Mode = mode;
				lock (list) { list.Clear(); list.AddRange(before); }
				History.Clear();
			}
			foreach (var p in made) t.Info("window picture: " + p);
		});

		// ------------------------------------------------------------------ texts

		static Task Texts(DevTests.Ctx t)
		{
			string[] codes = { "en", "pt-BR", "es", "de", "fr", "it", "ru", "tr", "id", "vi", "ja", "ko", "zh-CN", "ar", "fa", "ur", "hi", "bn", "mr", "ta", "te", "th", "sw" };
			var en = T.Read("en").Where(k => k.Key.StartsWith("window.")).ToDictionary(k => k.Key, k => k.Value);
			t.Check("texts: the window's English texts are there", en.Count >= 19, en.Count.ToString());
			foreach (var code in codes)
			{
				var other = T.Read(code);
				var missing = en.Keys.Where(k => !other.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)).ToList();
				var badArgs = en.Where(k => k.Value.Contains("{0}") && other.TryGetValue(k.Key, out var v) && !v.Contains("{0}")).Select(k => k.Key).ToList();
				t.Check("texts: window in " + code, missing.Count == 0 && badArgs.Count == 0, string.Join(", ", missing.Concat(badArgs).Take(8)));
			}
			return Task.CompletedTask;
		}
	}
}
