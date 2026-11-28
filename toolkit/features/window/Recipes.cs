using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopPet;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// Recipes: jobs one after the other with one click ("Join → make smaller → protect"), made by the user in the
	/// settings and kept there ("recipes"). Each recipe is a job of its own (🧪 and its name), offered first when its
	/// first step fits the files. The questions of every step are asked at the start; each step gets what the one
	/// before it made, and only the last step's files are kept (the ones between are deleted).
	/// </summary>
	internal static class Recipes
	{
		public const string Key = "recipes";
		public const int MaxSteps = 4;

		public sealed class Recipe
		{
			public string name { get; set; } = "";
			public List<string> steps { get; set; } = new();
		}

		public static List<Recipe> Read()
		{
			try
			{
				var node = ToolSettings.Read()[Key];
				return node == null ? new() : (JsonSerializer.Deserialize<List<Recipe>>(node.ToJsonString()) ?? new()).Where(r => r.steps.Count > 0 && !string.IsNullOrWhiteSpace(r.name)).ToList();
			}
			catch (Exception) { return new(); }
		}

		public static void Save(List<Recipe> list)
		{
			ToolSettings.Set(Key, JsonNode.Parse(JsonSerializer.Serialize(list)));
			Refresh();
		}

		[ModuleInitializer]
		internal static void Init()
		{
			Prefs.Register(new Prefs.Pref { Key = Key, TextKey = "recipes.pref", HintKey = "recipes.hint", ButtonKey = "recipes.manage", Click = Manage, Order = 8 });
			Refresh();
		}

		/// <summary>The recipes as jobs ("recipe-0", "recipe-1"…), the old ones taken away first.</summary>
		public static void Refresh()
		{
			foreach (var d in All().Where(d => d.Id.StartsWith("recipe-"))) Unregister(d.Id);
			var list = Read();
			for (int i = 0; i < list.Count; i++)
			{
				var r = list[i];
				Register(new JobDef
				{
					Id = "recipe-" + i, Emoji = "🧪", Name = r.name,
					// first of all, when its first step fits these files
					Rank = f => { var first = Find(r.steps[0]); try { return first?.Rank != null && first.Rank(f) >= 0 ? 7 : -1; } catch (Exception) { return -1; } },
					Ask = (owner, files) => AskAll(owner, files, r),
					Run = c => RunAll(c, r),
				});
			}
		}

		/// <summary>The questions of every step, at the start (their answers kept as "s0.…", "s1.…").</summary>
		static Dictionary<string, string> AskAll(IWin32Window owner, IList<string> files, Recipe r)
		{
			var all = new Dictionary<string, string>();
			for (int k = 0; k < r.steps.Count; k++)
			{
				var def = Find(r.steps[k]);
				if (def?.Ask == null) continue;
				var a = def.Ask(owner, files);
				if (a == null) return null;
				foreach (var (key, value) in a) all["s" + k + "." + key] = value;
			}
			return all;
		}

		static async Task RunAll(JobContext c, Recipe r)
		{
			var current = c.Files.ToList();
			var between = new List<string>();
			try
			{
				for (int k = 0; k < r.steps.Count; k++)
				{
					string id = r.steps[k];
					if (Find(id) == null) throw new InvalidOperationException(T.S("recipes.missing", id));
					string prefix = "s" + k + ".";
					var options = c.Options.Where(o => o.Key.StartsWith(prefix)).ToDictionary(o => o.Key[prefix.Length..], o => o.Value);
					int step = k;
					Result res;
					// the steps are not written in the history one by one: only the recipe is
					History.Quiet.Value = true;
					try { res = await Jobs.Run(id, current, (p, d) => c.Report((step + p) / r.steps.Count, TextOf(id) + (string.IsNullOrEmpty(d) ? "" : " · " + d)), c.Cancel, options); }
					finally { History.Quiet.Value = false; }
					if (!res.ok) throw new InvalidOperationException(T.S("recipes.failed", TextOf(id), res.message));
					if (!string.IsNullOrEmpty(res.message)) c.Errors.Add(res.message);
					if (k < r.steps.Count - 1) { between.AddRange(res.outputs); current = res.outputs.ToList(); }
					else { c.Outputs.AddRange(res.outputs); c.Preview = res.preview; }
				}
			}
			finally
			{
				foreach (var p in between.Except(c.Outputs))
					try { if (Directory.Exists(p)) Directory.Delete(p, true); else File.Delete(p); } catch (Exception) { }
			}
		}

		// ------------------------------------------------------------------ the window to make and delete them

		/// <summary>The jobs a recipe can use (not recipes, not the tests' own).</summary>
		internal static List<(string id, string text)> Steps() => All()
			.Where(d => !d.Id.StartsWith("recipe-") && !d.Id.Contains("test") && d.Id != "heichelp" && d.Name == null && d.Run != null)
			.Select(d => (d.Id, (d.Emoji ?? "") + "  " + TextOf(d.Id))).OrderBy(x => x.Item2, StringComparer.CurrentCulture).ToList();

		static void Manage(IWin32Window owner)
		{
			using var form = new Form
			{
				Text = T.S("recipes.pref"), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false,
				StartPosition = FormStartPosition.CenterParent, AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new SizeF(96, 96),
				ClientSize = new Size(520, 360), BackColor = Theme.Background, ForeColor = Theme.Text, Padding = new Padding(16),
			};
			Theme.ApplyWindowChrome(form);
			var font = Theme.CreateFont(13);
			var intro = new Label { Text = T.S("recipes.hint"), Font = font, ForeColor = Theme.TextMuted, Dock = DockStyle.Top, Height = 48 };
			var list = new ListBox { Dock = DockStyle.Fill, Font = font, BackColor = Theme.Surface, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false };
			var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0), BackColor = Theme.Background };
			Button B(string text, bool primary)
			{
				var b = new Button { Text = text, AutoSize = true, Font = font, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(8, 2, 8, 2), BackColor = primary ? Theme.Accent : Theme.Surface, ForeColor = primary ? Theme.AccentText : Theme.Text };
				b.FlatAppearance.BorderSize = primary ? 0 : 1; b.FlatAppearance.BorderColor = Theme.Border;
				return b;
			}
			var close = B(T.S("recipes.close"), false); close.DialogResult = DialogResult.Cancel;
			var add = B("➕  " + T.S("recipes.new"), true);
			var remove = B("🗑️  " + T.S("recipes.delete"), false);
			bottom.Controls.Add(close); bottom.Controls.Add(add); bottom.Controls.Add(remove);
			void Fill()
			{
				list.Items.Clear();
				foreach (var r in Read()) list.Items.Add("🧪 " + r.name + "  —  " + string.Join(" → ", r.steps.Select(TextOf)));
				remove.Enabled = list.Items.Count > 0;
				if (list.Items.Count > 0) list.SelectedIndex = 0;
			}
			add.Click += (s, e) => { if (New(form)) Fill(); };
			remove.Click += (s, e) =>
			{
				int i = list.SelectedIndex;
				var all = Read();
				if (i < 0 || i >= all.Count) return;
				all.RemoveAt(i);
				Save(all);
				Fill();
			};
			form.Controls.Add(list); form.Controls.Add(bottom); form.Controls.Add(intro);
			form.CancelButton = close;
			ButtonLook.Hook(form);
			Fill();
			form.ShowDialog(owner);
			font.Dispose();
		}

		/// <summary>A new recipe: its name and up to four steps.</summary>
		static bool New(IWin32Window owner)
		{
			var steps = Steps();
			var choices = steps.Select(x => (x.id, x.text)).ToArray();
			var fields = new List<Ask.Field> { new() { Key = "name", Label = T.S("recipes.name"), Check = v => string.IsNullOrWhiteSpace(v) ? T.S("recipes.noName") : null } };
			for (int k = 0; k < MaxSteps; k++)
				fields.Add(new Ask.Field { Key = "s" + k, Label = T.S("recipes.step", k + 1), Kind = Ask.Kind.Choice, Value = k == 0 ? "merge" : "", Choices = k == 0 ? choices : choices.Prepend(("", "—")).ToArray() });
			var a = Ask.Show(owner, T.S("recipes.new"), T.S("recipes.newIntro"), fields.ToArray());
			if (a == null) return false;
			var r = new Recipe { name = a["name"].Trim() };
			for (int k = 0; k < MaxSteps; k++) if (a.TryGetValue("s" + k, out var id) && id.Length > 0) r.steps.Add(id);
			if (r.steps.Count == 0) return false;
			var all = Read();
			all.Add(r);
			Save(all);
			return true;
		}
	}
}
