using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// Watched folders: a list of folders, each with a job that needs no questions. The list lives in tools.json
	/// ("watch": [{folder, job}]) and DeskPals reads the same file: while it runs, a new file in one of those folders is
	/// handed to this program ("--do=job --quiet") and what was made goes to a "DeskPals" folder inside it. Here there is
	/// only the list and its window (a button in the settings).
	/// </summary>
	internal static class WatchFolders
	{
		public const string Key = "watch";
		/// <summary>The folder inside a watched folder where DeskPals puts the results (it is never watched).</summary>
		public const string OutFolderName = "DeskPals";

		/// <summary>The jobs a folder can have, in this order: those that run without asking anything.</summary>
		static readonly string[] candidates = { "topdf", "searchable", "ocr", "smaller", "compress" };

		public sealed record Entry(string Folder, string Job);

		/// <summary>The jobs there are in this copy ("searchable" and "compress" come from other features).</summary>
		public static List<string> JobChoices() => candidates.Where(id => Jobs.Find(id) != null).ToList();

		/// <summary>The list saved (entries without a folder or a known job are left out).</summary>
		public static List<Entry> Load()
		{
			var list = new List<Entry>();
			try
			{
				var known = JobChoices();
				foreach (var n in ToolSettings.Read()[Key] as JsonArray ?? new JsonArray())
				{
					string folder = (string)n?["folder"], job = (string)n?["job"];
					if (string.IsNullOrWhiteSpace(folder) || !known.Contains(job)) continue;
					if (list.Any(e => Same(e.Folder, folder))) continue;
					list.Add(new Entry(folder, job));
				}
			}
			catch (Exception) { }
			return list;
		}

		/// <summary>Saves the list (the other settings stay as they were).</summary>
		public static void Save(IEnumerable<Entry> entries)
		{
			var arr = new JsonArray();
			var seen = new List<string>();
			foreach (var e in entries ?? Enumerable.Empty<Entry>())
			{
				if (string.IsNullOrWhiteSpace(e?.Folder) || string.IsNullOrWhiteSpace(e.Job) || seen.Any(s => Same(s, e.Folder))) continue;
				seen.Add(e.Folder);
				arr.Add(new JsonObject { ["folder"] = Path.GetFullPath(e.Folder), ["job"] = e.Job });
			}
			ToolSettings.Set(Key, arr);
		}

		/// <summary>Why this folder cannot be added (a text for the user), or null when it can.</summary>
		public static string Problem(string folder, IEnumerable<Entry> current)
		{
			if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return T.S("watch.missing");
			string full = Path.GetFullPath(folder).TrimEnd('\\');
			if (Path.GetPathRoot(full)?.TrimEnd('\\') == full) return T.S("watch.drive");
			if (current.Any(e => Same(e.Folder, full))) return T.S("watch.already");
			// the results folder of a watched folder: its files would be done again and again
			if (string.Equals(Path.GetFileName(full), OutFolderName, StringComparison.OrdinalIgnoreCase)) return T.S("watch.outFolder");
			return null;
		}

		static bool Same(string a, string b)
		{
			try { return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); }
			catch (Exception) { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
		}

		/// <summary>
		/// "--nohistory" (DeskPals' watched folders add it): the job is not put in the list of last jobs. DeskPals hands
		/// over a temp copy of the file and moves what was made right after, so such an entry would point nowhere and
		/// push the user's real ones out of the list. Without a file, History reads an empty list and writes nothing.
		/// </summary>
		internal static void NoHistory(IEnumerable<string> args)
		{
			if (args != null && args.Contains("--nohistory")) History.File = null;
		}

		[ModuleInitializer]
		internal static void Init()
		{
			try { NoHistory(Environment.GetCommandLineArgs()); } catch (Exception) { }
			Prefs.Register(new Prefs.Pref
			{
				Key = "watch", TextKey = "watch.pref", HintKey = "watch.hint", ButtonKey = "watch.button", Order = 40,
				Click = owner => { using var f = new WatchDialog(Load()); f.ShowDialog(owner); },
			});
			WatchTests.Register();
		}
	}

	/// <summary>The window of the watched folders: each folder with its job, "Add a folder…", "Remove", OK saves.</summary>
	internal sealed class WatchDialog : Form
	{
		readonly List<(string folder, ComboBox job, Control row)> rows = new();
		readonly FlowLayoutPanel list;
		readonly Label empty;
		readonly List<string> choices = WatchFolders.JobChoices();
		readonly Font font = Theme.CreateFont(13), small = Theme.CreateFont(11.5f);
		const int ListWidth = 560;

		public WatchDialog(IEnumerable<WatchFolders.Entry> start)
		{
			Text = T.S("watch.title");
			FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			BackColor = Theme.Background; ForeColor = Theme.Text; Padding = new Padding(18);
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);

			var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, BackColor = Theme.Background };
			flow.Controls.Add(new Label { Text = T.S("watch.intro"), AutoSize = true, MaximumSize = new Size(ListWidth, 0), Font = font, ForeColor = Theme.TextMuted, Margin = new Padding(0, 0, 0, 10) });
			list = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Width = ListWidth + 24, Height = 230, BackColor = Theme.Surface, Padding = new Padding(8) };
			empty = new Label { Text = T.S("watch.empty"), AutoSize = true, Font = font, ForeColor = Theme.TextMuted, Margin = new Padding(4, 8, 4, 8) };
			list.Controls.Add(empty);
			flow.Controls.Add(list);
			flow.Controls.Add(new Label { Text = T.S("watch.where", WatchFolders.OutFolderName), AutoSize = true, MaximumSize = new Size(ListWidth, 0), Font = small, ForeColor = Theme.TextMuted, Margin = new Padding(0, 6, 0, 0) });

			var bottom = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Margin = new Padding(0, 14, 0, 0), BackColor = Theme.Background };
			var add = MakeButton(T.S("watch.add"), false);
			add.Click += (s, e) => Choose();
			var ok = MakeButton(T.S("watch.save"), true);
			var cancel = MakeButton(T.S("cancel"), false);
			cancel.DialogResult = DialogResult.Cancel;
			ok.Click += (s, e) =>
			{
				try { WatchFolders.Save(Entries()); DialogResult = DialogResult.OK; }
				catch (Exception ex) { MessageBox.Show(this, Jobs.Friendly(ex), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
			};
			bottom.Controls.Add(add);
			bottom.Controls.Add(new Panel { Width = 150, Height = 1, BackColor = Theme.Background });
			bottom.Controls.Add(cancel);
			bottom.Controls.Add(ok);
			flow.Controls.Add(bottom);
			Controls.Add(flow);
			AcceptButton = ok; CancelButton = cancel;
			AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;

			foreach (var e in start ?? Enumerable.Empty<WatchFolders.Entry>()) AddRow(e.Folder, e.Job);
		}

		Button MakeButton(string text, bool primary)
		{
			var b = new Button { Text = text, AutoSize = true, Font = font, FlatStyle = FlatStyle.Flat, BackColor = primary ? Theme.Accent : Theme.Surface, ForeColor = primary ? Theme.AccentText : Theme.Text, Padding = new Padding(10, 2, 10, 2), Margin = new Padding(0, 0, 8, 0) };
			if (primary) b.FlatAppearance.BorderSize = 0; else b.FlatAppearance.BorderColor = Theme.Border;
			return b;
		}

		/// <summary>The folders and jobs as they are in the window now.</summary>
		public List<WatchFolders.Entry> Entries() => rows.Select(r => new WatchFolders.Entry(r.folder, choices[Math.Max(0, r.job.SelectedIndex)])).ToList();

		void Choose()
		{
			using var dlg = new FolderBrowserDialog { Description = T.S("watch.pick"), UseDescriptionForTitle = true };
			if (dlg.ShowDialog(this) != DialogResult.OK) return;
			string problem = TryAdd(dlg.SelectedPath, "topdf");
			if (problem != null) MessageBox.Show(this, problem, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>Adds a folder (with this job), or says why not.</summary>
		public string TryAdd(string folder, string job)
		{
			string problem = WatchFolders.Problem(folder, Entries());
			if (problem == null) AddRow(Path.GetFullPath(folder).TrimEnd('\\'), job);
			return problem;
		}

		void AddRow(string folder, string job)
		{
			if (choices.Count == 0) return;
			var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Surface, Margin = new Padding(0, 0, 0, 6) };
			var name = new Label { Text = "📁  " + folder, AutoSize = false, AutoEllipsis = true, Width = 290, Height = 30, Font = font, ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft };
			new ToolTip().SetToolTip(name, folder);
			var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Font = font, BackColor = Theme.Surface, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat, Margin = new Padding(6, 2, 6, 0) };
			foreach (var id in choices) combo.Items.Add(T.S("job." + id));
			combo.SelectedIndex = Math.Max(0, choices.IndexOf(job));
			var remove = new Button { Text = T.S("watch.remove"), AutoSize = true, Font = small, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text };
			remove.FlatAppearance.BorderColor = Theme.Border;
			row.Controls.Add(name); row.Controls.Add(combo); row.Controls.Add(remove);
			remove.Click += (s, e) => RemoveAt(rows.FindIndex(r => r.row == row));
			rows.Add((folder, combo, row));
			list.Controls.Add(row);
			empty.Visible = false;
		}

		/// <summary>Removes the row of a folder (the "Remove" button, also for the tests).</summary>
		public void RemoveAt(int index)
		{
			var (_, _, row) = rows[index];
			rows.RemoveAt(index);
			list.Controls.Remove(row);
			row.Dispose();
			empty.Visible = rows.Count == 0;
		}

		/// <summary>Chooses the job of a row by its id (for the tests).</summary>
		public void SetJob(int index, string id) => rows[index].job.SelectedIndex = Math.Max(0, choices.IndexOf(id));

		protected override void Dispose(bool disposing)
		{
			if (disposing) { font.Dispose(); small.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
