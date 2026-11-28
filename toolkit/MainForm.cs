using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// The window, in three parts chosen at the top:
	/// "Files": the files dropped (or chosen), in a list that can be put in order (drag a file, or ↑/↓), the jobs that fit
	/// them as buttons, and what was made, each with "Open" and "Show in folder". The jobs get the files in the list's
	/// order (a joined PDF follows it). The same jobs a pet offers when files are dropped on it, and also those only for
	/// the window.
	/// "Recent jobs": the history. "Settings": the switches and buttons of the features (<see cref="Prefs"/>).
	/// </summary>
	internal sealed class MainForm : Form
	{
		public enum Part { Files, Recent, Settings }

		readonly List<string> files = new();
		internal readonly UiFonts F;
		readonly Button[] tabs;
		readonly Panel filesPage, fileArea, listArea;
		readonly RecentPage recentPage;
		readonly SettingsPage settingsPage;
		readonly DropBox drop;
		readonly FileList list;
		readonly Button add, up, down, remove, clear;
		readonly FlowLayoutPanel jobs, done;
		readonly Label intro, hint, status, count;
		readonly ProgressBar bar;
		// files dropped (or chosen) while a job runs: added once it is done, so none is lost
		/// <summary>Files dropped while a job runs, with the place they were dropped at (-1: at the end).</summary>
		readonly List<(List<string> items, int at)> waiting = new();
		bool busy;

		public Part Showing { get; private set; } = Part.Files;
		/// <summary>The files, in the order the jobs get them.</summary>
		public IReadOnlyList<string> Files => files;
		internal FileList List => list;
		internal Button UpButton => up;
		internal Button DownButton => down;
		internal Button RemoveButton => remove;
		internal RecentPage Recent => recentPage;
		internal SettingsPage SettingsPart => settingsPage;
		internal IEnumerable<Button> JobButtons => jobs.Controls.OfType<Button>().Where(b => b.Name != "more");
		internal Button MoreButton => jobs.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "more");
		internal TextBox SearchBox => search;
		internal ScrollableControl JobsArea => jobs;
		internal Control DoneArea => done;
		internal Label CountLabel => count;

		public MainForm(IEnumerable<string> start)
		{
			F = new UiFonts(DeviceDpi);
			Text = T.S("app");
			StartPosition = FormStartPosition.CenterScreen;
			AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			ClientSize = new Size(720, 600);
			MinimumSize = new Size(560, 500);
			try { Icon = ToolIcon.ForWindow(); } catch (Exception) { try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { } }
			Theme.ApplyWindowChrome(this);
			Padding = new Padding(24, 16, 24, 14);
			// every button drawn by the extension (rounded, colour emoji), also the ones made later
			ButtonLook.Hook(this);

			// --- the top: the name and the three parts
			var title = new Label { Text = T.S("app"), Font = F.Title, AutoSize = true, Dock = DockStyle.Top, UseMnemonic = false };
			var tabRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Padding = new Padding(0, 6, 0, 10) };
			tabs = new[] { ("📁", "window.tab.files"), ("🕘", "window.tab.recent"), ("⚙️", "window.tab.settings") }
				.Select((t, i) =>
				{
					var b = Look.Button(t.Item1 + "  " + T.S(t.Item2), F.Text, "tab");
					b.Margin = new Padding(0, 0, 6, 0);
					b.AccessibleRole = AccessibleRole.PageTab;
					b.Click += (s, e) => ShowPart((Part)i);
					tabRow.Controls.Add(b);
					return b;
				}).ToArray();
			// "Read text on the screen": not a part of the window, a tool of its own (also Ctrl+Alt+T with the shortcuts on)
			var snip = Look.Button("🔤  " + T.S("job.snip"), F.Text, "tab");
			snip.Margin = new Padding(18, 0, 0, 0);
			snip.Name = "snip";
			new ToolTip().SetToolTip(snip, T.S("snip.tip"));
			snip.Click += async (s, e) => await SnipHere();
			tabRow.Controls.Add(snip);

			// --- "Files"
			filesPage = new Panel { Dock = DockStyle.Fill, Tag = "page" };
			intro = new Label { Text = T.S("main.intro"), Font = F.Text, AutoSize = false, Height = 42, Dock = DockStyle.Top, Tag = "muted", UseMnemonic = false };
			fileArea = new Panel { Dock = DockStyle.Top, Height = 190, Padding = new Padding(0, 4, 0, 4) };
			drop = new DropBox(this) { Dock = DockStyle.Fill };

			listArea = new Panel { Dock = DockStyle.Fill, Visible = false };
			list = new FileList(F) { Dock = DockStyle.Fill, AccessibleName = T.S("window.tab.files") };
			list.MoveAsked += (from, to) => MoveFile(from, to);
			list.RemoveAsked += i => RemoveAt(i);
			list.FilesDropped += (paths, at) => Add(paths, at);
			list.SelectedIndexChanged += (s, e) => Buttons();
			var listFrame = new ListFrame(list) { Dock = DockStyle.Fill };
			var side = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoScroll = true, Padding = new Padding(10, 0, 0, 0) };
			add = SideButton("＋  " + T.S("main.choose"), (s, e) => Choose());
			up = SideButton("↑  " + T.S("window.up"), (s, e) => MoveUp());
			down = SideButton("↓  " + T.S("window.down"), (s, e) => MoveDown());
			remove = SideButton("✕  " + T.S("window.remove"), (s, e) => RemoveSelected());
			clear = SideButton("🧹  " + T.S("main.clear"), (s, e) => Clear());
			// as wide as the longest text of this language needs (within limits), so "Choose files…" is not cut
			int sideWidth = Math.Clamp(new[] { add, up, down, remove, clear }.Max(b => TextRenderer.MeasureText(b.Text, F.Text).Width) * 96 / Math.Max(96, DeviceDpi) + 28, 150, 230);
			foreach (var b in new[] { add, up, down, remove, clear }) { b.Width = sideWidth; side.Controls.Add(b); }
			// the hint about the order goes under the list only, so the buttons have the whole height
			count = new Label { Dock = DockStyle.Bottom, AutoSize = false, Height = 22, Font = F.Small, Tag = "muted", TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false };
			var listColumn = new Panel { Dock = DockStyle.Fill };
			listColumn.Controls.Add(listFrame);
			listColumn.Controls.Add(count);
			listArea.Controls.Add(listColumn);
			listArea.Controls.Add(side);
			fileArea.Controls.Add(listArea);
			fileArea.Controls.Add(drop);
			// docked parts are added bottom-up, so Tab would go backwards: it follows what is seen, top to bottom
			listFrame.TabIndex = 0; count.TabIndex = 1;
			listColumn.TabIndex = 0; side.TabIndex = 1;
			listArea.TabIndex = 0; drop.TabIndex = 1;

			hint = new Label { Text = T.S("main.what"), Font = F.Bold, AutoSize = false, Height = 30, Dock = DockStyle.Top, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false };
			// find a job by its name, at the right of the line (only when there are many jobs)
			search = new TextBox { Font = F.Small, Width = Look.Px(this, 220), Anchor = AnchorStyles.Top | AnchorStyles.Right, PlaceholderText = T.S("jobs.search"), BorderStyle = BorderStyle.FixedSingle, Visible = false, AccessibleName = T.S("jobs.search") };
			hint.Controls.Add(search);
			hint.Resize += (s, e) => search.Location = new Point(hint.ClientSize.Width - search.Width, Math.Max(0, hint.ClientSize.Height - search.Height - 2));
			search.TextChanged += (s, e) => Refill(list.SelectedIndex);
			search.KeyDown += (s, e) =>
			{
				// Enter does the only job found; Esc empties the search
				if (e.KeyCode == Keys.Escape) { search.Text = ""; e.SuppressKeyPress = true; }
				else if (e.KeyCode == Keys.Enter && JobButtons.Count() == 1) { JobButtons.First().PerformClick(); e.SuppressKeyPress = true; }
			};
			// as tall as its buttons need (see Fit); it scrolls only when even that does not fit
			jobs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };
			bar = new ProgressBar { Dock = DockStyle.Top, Height = 6, Visible = false, Style = ProgressBarStyle.Continuous };
			status = new Label { Font = F.Small, AutoSize = false, Height = 24, Dock = DockStyle.Top, Tag = "muted", TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
			done = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
			filesPage.Controls.Add(done);
			foreach (var c in new Control[] { status, bar, jobs, hint, fileArea, intro }) filesPage.Controls.Add(c);
			int order = 0;
			foreach (var c in new Control[] { intro, fileArea, hint, jobs, bar, status, done }) c.TabIndex = order++;

			recentPage = new RecentPage(F) { Visible = false };
			settingsPage = new SettingsPage(F) { Visible = false };
			var pages = new Panel { Dock = DockStyle.Fill, Tag = "page" };
			pages.Controls.Add(filesPage);
			pages.Controls.Add(recentPage);
			pages.Controls.Add(settingsPage);
			Controls.Add(pages);
			Controls.Add(tabRow);
			Controls.Add(title);
			title.TabIndex = 0; tabRow.TabIndex = 1; pages.TabIndex = 2;
			filesPage.TabIndex = 0; recentPage.TabIndex = 1; settingsPage.TabIndex = 2;

			// files dropped anywhere on the window (the list and the box have their own, for the place they go)
			AllowDrop = true;
			DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
			DragDrop += (s, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) { Add(paths); ShowPart(Part.Files); } };
			Theme.Changed += ThemeChanged;
			// the page's own size (the window's Resize comes before its parts are laid out again)
			filesPage.SizeChanged += (s, e) => Fit();
			Look.Paint(this);
			ShowPart(Part.Files);
			Add(start);
		}

		Button SideButton(string text, EventHandler click)
		{
			var b = Look.Button(text, F.Text);
			b.AutoSize = false;
			b.TextAlign = ContentAlignment.MiddleLeft;
			// a long word in some language is cut with "…" (and shown whole when the mouse is on it)
			b.AutoEllipsis = true;
			b.Size = new Size(160, 31);
			b.Padding = new Padding(8, 0, 4, 0);
			b.Margin = new Padding(0, 0, 0, 5);
			b.Click += click;
			return b;
		}

		// ------------------------------------------------------------------ the parts

		public void ShowPart(Part part)
		{
			Showing = part;
			filesPage.Visible = part == Part.Files;
			recentPage.Visible = part == Part.Recent;
			settingsPage.Visible = part == Part.Settings;
			for (int i = 0; i < tabs.Length; i++)
			{
				bool on = i == (int)part;
				tabs[i].Tag = on ? "tab-on" : "tab";
				tabs[i].Font = on ? F.Bold : F.Text;
				tabs[i].AccessibleDescription = on ? T.S("window.selected") : null;
				Look.Paint(tabs[i]);
			}
			if (part == Part.Files) Fit();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			// Ctrl+1/2/3 and Ctrl+Tab go through the parts; Ctrl+O chooses files
			switch (keyData)
			{
				case Keys.Control | Keys.D1: ShowPart(Part.Files); return true;
				case Keys.Control | Keys.D2: ShowPart(Part.Recent); return true;
				case Keys.Control | Keys.D3: ShowPart(Part.Settings); return true;
				case Keys.Control | Keys.Tab: ShowPart((Part)(((int)Showing + 1) % 3)); return true;
				case Keys.Control | Keys.Shift | Keys.Tab: ShowPart((Part)(((int)Showing + 2) % 3)); return true;
				case Keys.Control | Keys.O: ShowPart(Part.Files); Choose(); return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		/// <summary>
		/// Shares the height of "Files": every job button is seen whole (the jobs take the rows they need), the list gets
		/// more room when the window is bigger or there are many files (never less than at 720×600), and what was made
		/// keeps a few lines at the bottom.
		/// </summary>
		void Fit()
		{
			int page = filesPage.ClientSize.Height, width = filesPage.ClientSize.Width;
			if (page <= 0 || width <= 0) return;
			// at least the five buttons beside the list; what was made keeps a few lines (one, when the jobs need room)
			int minList = Look.Px(this, 190), minDone = Look.Px(this, 70), tightDone = Look.Px(this, 48);
			int jobsNeed = JobsHeight(width);
			// what is shown is counted from the state (Visible says false for all of them while another part is shown)
			bool hintOn = jobs.Controls.Count > 0;
			int others = status.Height + (busy ? bar.Height : 0) + (hintOn ? hint.Height : 0);
			// the line that explains the window gives its place to the jobs when they need it
			bool introOn = files.Count == 0 || page - others - intro.Height - minList - tightDone >= jobsNeed;
			intro.Visible = introOn;
			int fixedParts = others + (introOn ? intro.Height : 0);
			// the list: 40% of the page, or more when there are many files to put in order and room is left
			int list = Math.Max(minList, (int)(page * 0.4));
			if (files.Count > 3) list = Math.Max(list, page - fixedParts - jobsNeed - minDone * 2);
			list = Math.Max(minList, Math.Min(list, page - fixedParts - jobsNeed - minDone));
			if (fileArea.Height != list) fileArea.Height = list;
			// the jobs: all their rows, unless the window is too small even for that (then they scroll)
			int room = Math.Max(Look.Px(this, 46), page - fixedParts - list - tightDone);
			int h = Math.Min(jobsNeed, room);
			if (jobs.Height != h) jobs.Height = h;
		}

		/// <summary>The height the job buttons need, wrapped in rows as wide as the page (as the FlowLayoutPanel wraps them).</summary>
		int JobsHeight(int width)
		{
			int inner = Math.Max(1, width - jobs.Padding.Horizontal), top = 0, x = 0, rowHeight = 0;
			bool breakNext = false;
			foreach (Control b in jobs.Controls)
			{
				var size = b.GetPreferredSize(Size.Empty);
				int w = Math.Max(b.Width, size.Width) + b.Margin.Horizontal, h = Math.Max(b.Height, size.Height) + b.Margin.Vertical;
				// a group's title starts a line of its own, and so do its buttons
				if (x > 0 && (breakNext || x + w > inner)) { top += rowHeight; x = 0; rowHeight = 0; }
				x += w;
				rowHeight = Math.Max(rowHeight, h);
				breakNext = jobs.GetFlowBreak(b);
			}
			int counted = top + rowHeight + jobs.Padding.Vertical + Look.Px(this, 2);
			// and where the panel really put them (the titles of the groups can take more than counted): the larger
			if (jobs.Controls.Count == 0) return counted;
			jobs.PerformLayout();
			int placed = jobs.Controls.Cast<Control>().Max(c => c.Bottom + c.Margin.Bottom) - jobs.AutoScrollPosition.Y + jobs.Padding.Bottom + Look.Px(this, 2);
			return Math.Max(counted, placed);
		}

		void ThemeChanged()
		{
			if (IsDisposed) return;
			if (!IsHandleCreated) { Look.Paint(this); return; }
			try
			{
				BeginInvoke(() =>
				{
					Theme.ApplyWindowChrome(this);
					Look.Paint(this);
					drop.Invalidate();
					list.Invalidate();
				});
			}
			catch (Exception) { }
		}

		// ------------------------------------------------------------------ the files and their order

		/// <summary>Add files at the end (or at a place of the list); one already there is not added again.</summary>
		/// <summary>
		/// The files dropped during the job, each group where it was dropped. A group dropped earlier, above a later one,
		/// moves that one down by the files it brought.
		/// </summary>
		void AddWaiting()
		{
			var groups = waiting.ToList();
			waiting.Clear();
			for (int i = 0; i < groups.Count; i++)
			{
				int before = files.Count;
				var (items, at) = groups[i];
				Add(items, at);
				int added = files.Count - before;
				for (int k = i + 1; k < groups.Count; k++)
					if (at >= 0 && groups[k].at >= at) groups[k] = (groups[k].items, groups[k].at + added);
			}
		}

		public void Add(IEnumerable<string> items, int at = -1)
		{
			if (items == null) return;
			if (busy)
			{
				// a job is using the list: these wait for it to end (nothing dropped is lost), and the user is told so
				var those = items.Where(p => !string.IsNullOrEmpty(p)).ToList();
				if (those.Count > 0) waiting.Add((those, at));
				if (waiting.Count > 0) count.Text = T.S("window.busy");
				return;
			}
			int place = at < 0 || at > files.Count ? files.Count : at, first = place;
			foreach (var f in items.Select(Full).Where(f => f != null))
			{
				if (files.Contains(f, StringComparer.OrdinalIgnoreCase)) continue;
				files.Insert(place++, f);
			}
			Refill(place > first ? first : list.SelectedIndex);
		}

		static string Full(string path) { try { return Path.GetFullPath(path); } catch (Exception) { return null; } }

		/// <summary>Move a file so that it is the (to)th: what the drag in the list, ↑/↓ and Alt+↑/↓ do.</summary>
		public void MoveFile(int from, int to)
		{
			if (busy || from < 0 || from >= files.Count) return;
			to = Math.Clamp(to, 0, files.Count - 1);
			if (to == from) return;
			string f = files[from];
			files.RemoveAt(from);
			files.Insert(to, f);
			Refill(to);
		}

		public void MoveUp() { int i = list.SelectedIndex; if (i > 0) MoveFile(i, i - 1); }
		public void MoveDown() { int i = list.SelectedIndex; if (i >= 0 && i < files.Count - 1) MoveFile(i, i + 1); }
		public void RemoveSelected() => RemoveAt(list.SelectedIndex);

		/// <summary>Take a file out of the list (only out of the list: the file itself stays where it is).</summary>
		public void RemoveAt(int i)
		{
			if (busy || i < 0 || i >= files.Count) return;
			files.RemoveAt(i);
			Refill(Math.Min(i, files.Count - 1));
		}

		/// <summary>Select a file of the list (as a click would).</summary>
		public void SelectFile(int i) { if (i >= 0 && i < list.Items.Count) list.SelectedIndex = i; }

		void Clear() { if (busy) return; files.Clear(); done.Controls.Clear(); status.Text = ""; Refill(-1); }

		public void Choose()
		{
			using var dlg = new OpenFileDialog { Multiselect = true, Title = T.S("main.choose") };
			if (dlg.ShowDialog(this) == DialogResult.OK) Add(dlg.FileNames);
		}

		void Refill(int selected)
		{
			bool any = files.Count > 0;
			listArea.Visible = any;
			drop.Visible = !any;
			list.ShowFiles(files, selected);
			count.Text = busy && waiting.Count > 0 ? T.S("window.busy") : files.Count > 1 ? T.S("window.order") : "";
			drop.Invalidate();
			Buttons();
			jobs.SuspendLayout();
			foreach (Control c in jobs.Controls.Cast<Control>().ToList()) c.Dispose();
			jobs.Controls.Clear();
			var plan = Jobs.Plan(files);
			hint.Visible = plan.Count > 0;
			search.Visible = plan.Count > JobGroups.Fold;
			string find = search.Visible ? search.Text.Trim() : "";
			if (find.Length > 0)
			{
				// a search: the jobs whose name has its words, in one row (no groups)
				var found = plan.Where(j => JobGroups.Matches(j.Text, find)).ToList();
				foreach (var j in found) jobs.Controls.Add(JobButton(j, found.Count > 10));
				if (found.Count == 0) jobs.Controls.Add(new Label { Text = T.S("jobs.none"), Font = F.Text, AutoSize = true, Tag = "muted", Margin = new Padding(2, 8, 0, 0), UseMnemonic = false });
			}
			else if (plan.Count <= JobGroups.Fold)
				foreach (var j in plan) jobs.Controls.Add(JobButton(j, false));
			else
			{
				// many jobs: the most useful first, then "More options" with the others in their groups
				foreach (var j in plan.Take(JobGroups.Top)) jobs.Controls.Add(JobButton(j, true));
				var more = Look.Button((expanded ? "➖  " + T.S("jobs.less") : "➕  " + T.S("jobs.more", plan.Count - JobGroups.Top)), F.Text, "tab");
				more.Name = "more";
				more.Padding = new Padding(5, 1, 5, 1); more.Margin = new Padding(2);
				more.Click += (s, e) => { expanded = !expanded; try { ToolSettings.Set("jobsExpanded", expanded); } catch (Exception) { } Refill(list.SelectedIndex); };
				jobs.Controls.Add(more);
				if (expanded)
				{
					var rest = plan.Skip(JobGroups.Top).ToList();
					foreach (var g in JobGroups.Order)
					{
						var inGroup = rest.Where(j => JobGroups.GroupOf(j.Id) == g).ToList();
						if (inGroup.Count == 0) continue;
						// a title on a line of its own, its jobs under it
						jobs.SetFlowBreak(jobs.Controls[jobs.Controls.Count - 1], true);
						var title = new Label { Text = JobGroups.Emoji(g) + "  " + T.S("jobs.group." + g), Font = F.Small, AutoSize = true, Tag = "muted", Margin = new Padding(2, 8, 0, 2), UseMnemonic = false };
						jobs.Controls.Add(title);
						jobs.SetFlowBreak(title, true);
						foreach (var j in inGroup) jobs.Controls.Add(JobButton(j, true));
					}
				}
			}
			Look.Paint(jobs);
			jobs.ResumeLayout();
			Fit();
		}

		/// <summary>The folded jobs shown (remembered).</summary>
		bool expanded = ToolSettings.Bool("jobsExpanded");

		/// <summary>The box to find a job by its name, at the right of "What shall I do?" (only when there are many).</summary>
		TextBox search;

		Button JobButton(Jobs.Job j, bool tight)
		{
			var b = Look.Button(j.Emoji + "  " + j.Text, F.Text);
			// many jobs (a PDF has a dozen): tighter buttons, so every one is seen at 720×600
			b.Padding = tight ? new Padding(5, 1, 5, 1) : new Padding(8, 4, 8, 4);
			if (tight) b.Margin = new Padding(2);
			b.Name = j.Id;
			b.Enabled = !busy;
			b.Click += async (s, e) => await Do(j.Id);
			return b;
		}

		/// <summary>↑, ↓ and Remove only when they can do something.</summary>
		void Buttons()
		{
			int i = list.SelectedIndex;
			up.Enabled = !busy && i > 0;
			down.Enabled = !busy && i >= 0 && i < files.Count - 1;
			remove.Enabled = !busy && i >= 0;
			add.Enabled = clear.Enabled = !busy;
		}

		// ------------------------------------------------------------------ doing a job

		internal async Task Do(string id)
		{
			if (busy || files.Count == 0) return;
			// a job that needs answers (pages, a password, a text…) asks them first
			var def = Jobs.Find(id);
			Dictionary<string, string> options = null;
			if (def?.Ask != null && (options = def.Ask(this, files.ToList())) == null) return;
			busy = true;
			var started = DateTime.Now;
			foreach (Control c in jobs.Controls) c.Enabled = false;
			Buttons();
			bar.Visible = true; bar.Value = 0;
			Fit();
			status.Text = Jobs.DoingOf(id);
			// the files go in the list's order: the first one is the first page of a joined PDF
			var r = await Jobs.Run(id, files.ToList(), (p, d) => { try { BeginInvoke(() => Progress(id, p, d)); } catch (Exception) { } }, default, options);
			if (IsDisposed) return;
			bar.Visible = false;
			busy = false;
			Fit();
			foreach (Control c in jobs.Controls) c.Enabled = true;
			Buttons();
			if (waiting.Count > 0) AddWaiting();
			status.Text = r.outputs.Count > 0 ? T.S("main.done", r.outputs.Count) : r.ok ? "" : T.S("main.failed");
			// a long job done while the window was not looked at: Windows says so (a click brings the window back)
			if ((DateTime.Now - started).TotalSeconds >= Notice.After && (WindowState == FormWindowState.Minimized || !ContainsFocus))
				Notice.Show(this, Jobs.TextOf(id), r.ok ? (r.outputs.Count > 0 ? T.S("main.done", r.outputs.Count) : r.preview) : T.S("main.failed") + (string.IsNullOrEmpty(r.message) ? "" : ": " + r.message), r.ok);
			// a job that only answered (no file): its sentence is the message
			if (r.ok && r.outputs.Count == 0 && !string.IsNullOrEmpty(r.preview)) MessageBox.Show(this, r.preview, T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			else if (!string.IsNullOrEmpty(r.message)) MessageBox.Show(this, r.message, T.S("app"), MessageBoxButtons.OK, r.ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
			foreach (var o in r.outputs) done.Controls.Add(Made(o));
			Look.Paint(done);
			if (done.Controls.Count > 0) done.ScrollControlIntoView(done.Controls[done.Controls.Count - 1]);
		}

		void Progress(string id, double p, string doing)
		{
			// a report that arrives after the job ended must not cover "Done" again
			if (IsDisposed || !busy) return;
			bar.Value =(int)Math.Round(Math.Clamp(p, 0, 1) * 100);
			if (!string.IsNullOrEmpty(doing)) status.Text = Jobs.DoingOf(id) + "  ·  " + doing;
		}

		/// <summary>A line for a file made: its name, "Open" and "Show in folder".</summary>
		Control Made(string path)
		{
			var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 4) };
			var made = new Label { Text = "✔  " + Path.GetFileName(path.TrimEnd('\\')), AutoSize = true, Font = F.Text, Margin = new Padding(0, 0, 12, 0), UseMnemonic = false };
			// the file can be dragged from here straight into an e-mail or a chat
			Look.DragOut(made, path);
			row.Controls.Add(made);
			row.Controls.Add(Look.Link(T.S("open"), F.Small, () => Open(path)));
			row.Controls.Add(Look.Link(T.S("showFolder"), F.Small, () => Reveal(path)));
			return row;
		}

		/// <summary>The window steps aside (so it is not in the picture), the screen is read, the window comes back.</summary>
		async Task SnipHere()
		{
			var state = WindowState;
			WindowState = FormWindowState.Minimized;
			await Task.Delay(350);
			try { using var snip = new SnipForm(); snip.ShowDialog(); }
			finally { WindowState = state; Activate(); }
		}

		public static void Open(string path)
		{
			try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception) { }
		}

		public static void Reveal(string path)
		{
			try { Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { "/select,", path } }); } catch (Exception) { }
		}

		// painted again once the parts have their handles: the scroll bars follow the theme only then
		protected override void OnLoad(EventArgs e) { base.OnLoad(e); Look.Paint(this); Fit(); }

		protected override void Dispose(bool disposing)
		{
			if (disposing) { Theme.Changed -= ThemeChanged; F.Dispose(); }
			base.Dispose(disposing);
		}

		/// <summary>A thin rounded frame around the list, on the surface colour (a ListBox has no rounded border of its own).</summary>
		sealed class ListFrame : Panel
		{
			public ListFrame(Control inner)
			{
				Tag = "card";
				Padding = new Padding(6);
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
				Controls.Add(inner);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				g.Clear(Parent?.BackColor ?? Theme.Background);
				Theme.Prepare(g);
				Theme.FillRounded(g, Theme.Surface, new RectangleF(0, 0, Width - 1, Height - 1), Look.Px(this, 12));
				Theme.DrawRounded(g, Theme.Border, new RectangleF(0, 0, Width, Height), Look.Px(this, 12), 1.5f);
			}
		}

		/// <summary>The box shown while there are no files: "Drop files here" with a dashed border, and "Choose files…".</summary>
		sealed class DropBox : Control
		{
			readonly MainForm form;

			public DropBox(MainForm f)
			{
				form = f;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
				TabStop = true;
				AccessibleName = T.S("main.drop");
				AccessibleRole = AccessibleRole.PushButton;
				AllowDrop = true;
				DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
				DragDrop += (s, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) form.Add(paths); };
				Cursor = Cursors.Hand;
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Parent?.BackColor ?? Theme.Background);
				var r = new Rectangle(1, 1, Width - 3, Height - 3);
				int radius = Look.Px(this, 12);
				Theme.FillRounded(g, Theme.Surface, r, radius);
				using (var pen = new Pen(Theme.Accent, 1.5f) { DashStyle = DashStyle.Dash })
				using (var path = Theme.RoundedRect(r, radius)) g.DrawPath(pen, path);
				int line = Look.Px(this, 24);
				Theme.DrawText(g, "📥", form.F.Title, new Rectangle(0, Height / 2 - line * 2, Width, line * 2 - 4), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				Theme.DrawText(g, T.S("main.drop"), form.F.Bold, new Rectangle(0, Height / 2 - line / 3, Width, line), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				string choose = T.S("main.choose");
				var size = TextRenderer.MeasureText(choose, form.F.Small);
				Theme.DrawText(g, choose, form.F.Small, new Rectangle((Width - size.Width) / 2, Height / 2 + line, size.Width + 4, line), Theme.Accent, TextFormatFlags.SingleLine);
				if (Focused && ShowFocusCues) Theme.DrawRounded(g, Theme.Accent, new RectangleF(3, 3, Width - 7, Height - 7), radius, 2);
			}

			protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
			protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

			// the whole box chooses files: with the mouse, or Enter / Space when it has the focus
			protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); if (e.Button == MouseButtons.Left) form.Choose(); }
			protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);
			protected override void OnKeyDown(KeyEventArgs e)
			{
				base.OnKeyDown(e);
				if (e.KeyCode is Keys.Enter or Keys.Space) { e.Handled = true; form.Choose(); }
			}
		}
	}
}
