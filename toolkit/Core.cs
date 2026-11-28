using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// Where the extension keeps its own things: %LocalAppData%\DesktopPet\extensions (next to DeskPals' data). DeskPals
	/// reads <see cref="ToolSettings"/> too (the watched folders).
	/// </summary>
	internal static class Places
	{
		public static string Folder
		{
			get
			{
				string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "extensions");
				Directory.CreateDirectory(f);
				return f;
			}
		}
	}

	/// <summary>
	/// The extension's settings ("tools.json" in <see cref="Places.Folder"/>): a JSON object, read and written whole (small).
	/// Keys used: "watch" (array of {folder, job}), "explorerMenu" (bool), "tesseract" (bool)… Tests can point it elsewhere.
	/// </summary>
	internal static class ToolSettings
	{
		public static string File = Path.Combine(Places.Folder, "tools.json");
		static readonly object gate = new();

		public static JsonObject Read()
		{
			lock (gate)
			{
				try { return System.IO.File.Exists(File) ? JsonNode.Parse(System.IO.File.ReadAllText(File)) as JsonObject ?? new JsonObject() : new JsonObject(); }
				catch (Exception) { return new JsonObject(); }
			}
		}

		public static void Write(JsonObject o)
		{
			lock (gate)
			{
				string tmp = File + ".tmp";
				System.IO.File.WriteAllText(tmp, o.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
				System.IO.File.Move(tmp, File, true);
			}
		}

		public static bool Bool(string key, bool fallback = false) { try { return (bool?)Read()[key] ?? fallback; } catch (Exception) { return fallback; } }
		public static string Text(string key, string fallback = "") { try { return (string)Read()[key] ?? fallback; } catch (Exception) { return fallback; } }
		public static void Set(string key, JsonNode value) { var o = Read(); o[key] = value; Write(o); }
	}

	/// <summary>
	/// Settings of the features, shown by the window in its "Settings" part: a switch (<see cref="Pref.Get"/>/<see cref="Pref.Set"/>),
	/// a button (<see cref="Pref.ButtonKey"/>/<see cref="Pref.Click"/>) or both. Registered by each feature's own file.
	/// </summary>
	internal static class Prefs
	{
		public sealed class Pref
		{
			public string Key, TextKey, HintKey;
			/// <summary>A switch, optional.</summary>
			public Func<bool> Get;
			public Action<bool> Set;
			/// <summary>A button ("Choose folders…"), optional.</summary>
			public string ButtonKey;
			public Action<IWin32Window> Click;
			/// <summary>Order in the list (lower first).</summary>
			public int Order = 50;
		}

		static readonly List<Pref> list = new();
		public static void Register(Pref p) { lock (list) { list.RemoveAll(x => x.Key == p.Key); list.Add(p); } }
		public static List<Pref> All() { lock (list) return list.OrderBy(p => p.Order).ThenBy(p => p.Key, StringComparer.Ordinal).ToList(); }
	}

	/// <summary>
	/// The last jobs done ("tools-history.json", the newest first, at most 60): when, which job, the files and what was
	/// made. The window shows it (with Open and Show in folder).
	/// </summary>
	internal static class History
	{
		public static string File = Path.Combine(Places.Folder, "tools-history.json");
		public const int Max = 60;

		public sealed class Entry
		{
			public DateTime at { get; set; }
			public string job { get; set; }
			public List<string> files { get; set; } = new();
			public List<string> outputs { get; set; } = new();
		}

		public static List<Entry> Read()
		{
			try { return System.IO.File.Exists(File) ? JsonSerializer.Deserialize<List<Entry>>(System.IO.File.ReadAllText(File)) ?? new() : new(); }
			catch (Exception) { return new(); }
		}

		/// <summary>The steps of a recipe are not written one by one (only the recipe is).</summary>
		public static readonly AsyncLocal<bool> Quiet = new();
		
		public static void Add(string job, IList<string> files, Jobs.Result r)
		{
			if (Quiet.Value) return;
			try
			{
				var list = Read();
				list.Insert(0, new Entry { at = DateTime.Now, job = job, files = files.ToList(), outputs = r.outputs.ToList() });
				if (list.Count > Max) list.RemoveRange(Max, list.Count - Max);
				System.IO.File.WriteAllText(File, JsonSerializer.Serialize(list));
				Changed?.Invoke();
			}
			catch (Exception) { }
		}

		public static void Clear() { try { System.IO.File.Delete(File); Changed?.Invoke(); } catch (Exception) { } }

		public static event Action Changed;
	}

	/// <summary>
	/// A small window that asks what a job needs before it runs: texts, numbers, a choice, a password, a yes/no, in the
	/// theme of DeskPals. <see cref="Show"/> gives the answers by key, or null when the user canceled.
	/// </summary>
	internal static class Ask
	{
		public enum Kind { Text, Number, Choice, Password, Check, Info, Lines }

		public sealed class Field
		{
			public string Key, Label, Value = "", Hint;
			public Kind Kind = Kind.Text;
			/// <summary>Choice: (value, text) pairs.</summary>
			public (string value, string text)[] Choices;
			public decimal Min = 0, Max = 100000;
			/// <summary>Return an error message (shown under the field) or null when the value is fine.</summary>
			public Func<string, string> Check;
		}

		public static Dictionary<string, string> Show(IWin32Window owner, string title, string intro, params Field[] fields)
		{
			using var form = new Form
			{
				Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = owner == null,
				StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent, AutoScaleMode = AutoScaleMode.Dpi,
				AutoScaleDimensions = new SizeF(96, 96), BackColor = Theme.Background, ForeColor = Theme.Text, Padding = new Padding(18), TopMost = owner == null,
			};
			try { form.Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(form);
			var font = Theme.CreateFont(13);
			var small = Theme.CreateFont(11.5f);
			var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, BackColor = Theme.Background };
			int width = 380;
			if (!string.IsNullOrEmpty(intro)) flow.Controls.Add(new Label { Text = intro, AutoSize = true, MaximumSize = new Size(width, 0), Font = font, ForeColor = Theme.TextMuted, Margin = new Padding(0, 0, 0, 10) });
			var getters = new List<(Field f, Func<string> get, Label error)>();
			foreach (var f in fields)
			{
				if (f.Kind != Kind.Check && f.Kind != Kind.Info) flow.Controls.Add(new Label { Text = f.Label, AutoSize = true, Font = font, ForeColor = Theme.Text, Margin = new Padding(0, 4, 0, 2) });
				Func<string> get;
				switch (f.Kind)
				{
					case Kind.Choice:
						var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Font = font, BackColor = Theme.Surface, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat };
						foreach (var c in f.Choices ?? Array.Empty<(string, string)>()) combo.Items.Add(c.text);
						combo.SelectedIndex = Math.Max(0, Array.FindIndex(f.Choices ?? Array.Empty<(string, string)>(), c => c.value == f.Value));
						flow.Controls.Add(combo);
						get = () => combo.SelectedIndex >= 0 ? f.Choices[combo.SelectedIndex].value : "";
						break;
					case Kind.Number:
						var num = new NumericUpDown { Width = 140, Font = font, Minimum = f.Min, Maximum = f.Max, BackColor = Theme.Surface, ForeColor = Theme.Text, Value = decimal.TryParse(f.Value, out var d) ? Math.Clamp(d, f.Min, f.Max) : f.Min };
						flow.Controls.Add(num);
						get = () => ((int)num.Value).ToString();
						break;
					case Kind.Check:
						var cb = new CheckBox { Text = f.Label, AutoSize = true, Font = font, ForeColor = Theme.Text, Checked = f.Value == "1" || f.Value == "true", Margin = new Padding(0, 6, 0, 2) };
						flow.Controls.Add(cb);
						get = () => cb.Checked ? "1" : "0";
						break;
					case Kind.Info:
						flow.Controls.Add(new Label { Text = f.Label, AutoSize = true, MaximumSize = new Size(width, 0), Font = small, ForeColor = Theme.TextMuted, Margin = new Padding(0, 4, 0, 4) });
						get = () => "";
						break;
					default:
						var box = new TextBox { Text = f.Value ?? "", Width = width, Font = font, BackColor = Theme.Surface, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = f.Kind == Kind.Password };
						// several lines: Enter makes a new line (the OK button is clicked with the mouse or Ctrl+Enter)
						if (f.Kind == Kind.Lines) { box.Multiline = true; box.AcceptsReturn = true; box.Height = (int)(font.GetHeight() * 4) + 8; box.ScrollBars = ScrollBars.Vertical; }
						flow.Controls.Add(box);
						get = () => box.Text;
						break;
				}
				if (!string.IsNullOrEmpty(f.Hint)) flow.Controls.Add(new Label { Text = f.Hint, AutoSize = true, MaximumSize = new Size(width, 0), Font = small, ForeColor = Theme.TextMuted, Margin = new Padding(0, 2, 0, 2) });
				var err = new Label { AutoSize = true, MaximumSize = new Size(width, 0), Font = small, ForeColor = Theme.Danger, Visible = false };
				flow.Controls.Add(err);
				getters.Add((f, get, err));
			}
			var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Width = width, Margin = new Padding(0, 14, 0, 0), BackColor = Theme.Background };
			var ok = new Button { Text = T.S("ok"), AutoSize = true, Font = font, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Theme.AccentText, Padding = new Padding(10, 2, 10, 2) };
			var cancel = new Button { Text = T.S("cancel"), AutoSize = true, Font = font, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text, Padding = new Padding(10, 2, 10, 2), DialogResult = DialogResult.Cancel };
			ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderColor = Theme.Border;
			buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
			flow.Controls.Add(buttons);
			form.Controls.Add(flow);
			form.AcceptButton = ok; form.CancelButton = cancel;
			form.AutoSize = true; form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			// many questions (a long form): the window keeps within the screen and its questions scroll
			var preferred = flow.GetPreferredSize(Size.Empty);
			int most = (int)((owner is Control oc ? Screen.FromControl(oc) : Screen.PrimaryScreen).WorkingArea.Height * 0.85);
			if (preferred.Height + form.Padding.Vertical > most)
			{
				form.AutoSize = false;
				flow.AutoSize = false;
				flow.AutoScroll = true;
				form.ClientSize = new Size(preferred.Width + SystemInformation.VerticalScrollBarWidth + form.Padding.Horizontal + 8, most);
			}
			Dictionary<string, string> answers = null;
			ok.Click += (s, e) =>
			{
				bool good = true;
				var a = new Dictionary<string, string>();
				foreach (var (f, get, err) in getters)
				{
					string v = get();
					string problem = f.Check?.Invoke(v);
					err.Text = problem ?? ""; err.Visible = problem != null;
					if (problem != null) good = false;
					if (f.Key != null) a[f.Key] = v;
				}
				if (!good) return;
				answers = a;
				form.DialogResult = DialogResult.OK;
			};
			ButtonLook.Hook(form);
			var result = owner == null ? form.ShowDialog() : form.ShowDialog(owner);
			font.Dispose(); small.Dispose();
			return result == DialogResult.OK ? answers : null;
		}
	}
}
