using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// Languages kept online, in the Languages folder of the repository (Languages/languages.json lists them, each one is
	/// Languages/xx.json). The settings download one into <see cref="Lang.UserFolder"/>; from then on it is a language like
	/// the ones that come with the program. A downloaded file is only data: it must be a JSON list of texts.
	/// </summary>
	internal static class LanguageStore
	{
		/// <summary>One language of the online list.</summary>
		public sealed class Item
		{
			public string Code { get; set; }
			/// <summary>Name in the language itself ("Français").</summary>
			public string Name { get; set; }
			/// <summary>Name in English ("French").</summary>
			public string English { get; set; }
			/// <summary>Version of the file (a hash of its content, see Tools\update-languages-json.ps1): a different one is offered as an update.</summary>
			public string Updated { get; set; }
			public long Size { get; set; }
		}

		sealed class Catalog { public List<Item> Languages { get; set; } }

		/// <summary>Where the languages are: the repository, or another address or a folder ("--languages=…", for tests).</summary>
		public static string BaseUrl = AppInfo.LanguagesRawUrl;

		const long MaxSize = 4 * 1024 * 1024;
		static readonly Regex CodeOk = new Regex(@"^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})?$");

		static string UserFile(string code) => Path.Combine(Lang.UserFolder, code + ".json");
		static string VersionFile(string code) => Path.Combine(Lang.UserFolder, code + ".version");

		static async Task<string> GetText(string name)
		{
			string text;
			if (!BaseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				var file = new FileInfo(Path.Combine(BaseUrl, name));
				if (file.Length > MaxSize) throw new InvalidDataException(name + ": too big");
				text = File.ReadAllText(file.FullName, Encoding.UTF8);
			}
			else
			{
				using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
				using (var response = await http.GetAsync(BaseUrl + name, HttpCompletionOption.ResponseHeadersRead))
				{
					response.EnsureSuccessStatusCode();
					if (response.Content.Headers.ContentLength > MaxSize) throw new InvalidDataException(name + ": too big");
					var bytes = await response.Content.ReadAsByteArrayAsync();
					if (bytes.Length > MaxSize) throw new InvalidDataException(name + ": too big");
					text = Encoding.UTF8.GetString(bytes);
				}
			}
			return text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text;
		}

		/// <summary>The languages online, by their English name.</summary>
		public static async Task<List<Item>> CatalogAsync()
		{
			var catalog = JsonText.Deserialize<Catalog>(await GetText("languages.json"));
			return (catalog?.Languages ?? new List<Item>())
				.Where(i => i?.Code != null && CodeOk.IsMatch(i.Code))
				.GroupBy(i => i.Code, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
				.OrderBy(i => i.English ?? i.Name ?? i.Code, StringComparer.OrdinalIgnoreCase).ToList();
		}

		/// <summary>The language comes with the program (lang folder next to it, or inside the exe).</summary>
		public static bool IsBuiltIn(string code) =>
			File.Exists(Path.Combine(Lang.Folder, code + ".json")) || code.Equals("en", StringComparison.OrdinalIgnoreCase);

		public static bool IsDownloaded(string code) => CodeOk.IsMatch(code ?? "") && File.Exists(UserFile(code));

		/// <summary>The online file is newer than the one downloaded.</summary>
		public static bool HasUpdate(Item item)
		{
			if (!IsDownloaded(item.Code) || string.IsNullOrEmpty(item.Updated)) return false;
			try { return File.Exists(VersionFile(item.Code)) && File.ReadAllText(VersionFile(item.Code)).Trim() != item.Updated.Trim(); }
			catch (Exception) { return false; }
		}

		/// <summary>The language in use, as chosen in the settings ("auto" = the Windows language).</summary>
		public static bool InUse(string code) => string.Equals(Lang.Current, code, StringComparison.OrdinalIgnoreCase);

		/// <summary>Download a language (or its newer version) into the user's lang folder.</summary>
		public static async Task DownloadAsync(Item item)
		{
			if (item?.Code == null || !CodeOk.IsMatch(item.Code)) throw new ArgumentException("Invalid language code");
			string text = await GetText(item.Code + ".json");
			// data only: a list of texts with the language's name
			var texts = JsonText.Deserialize<Dictionary<string, string>>(text);
			if (texts == null || !texts.TryGetValue("language.name", out string name) || string.IsNullOrWhiteSpace(name))
				throw new InvalidDataException("not a language file");
			Directory.CreateDirectory(Lang.UserFolder);
			string target = UserFile(item.Code), temp = target + ".tmp";
			File.WriteAllText(temp, text, new UTF8Encoding(false));
			File.Copy(temp, target, true);
			File.Delete(temp);
			File.WriteAllText(VersionFile(item.Code), item.Updated ?? "");
			Lang.Refresh();
			if (InUse(item.Code)) Lang.Use(item.Code);   // an update of the language in use shows at once
		}

		/// <summary>Remove a downloaded language; if it was in use, the program follows Windows again.</summary>
		public static void Remove(string code)
		{
			if (!CodeOk.IsMatch(code ?? "")) return;
			bool inUse = InUse(code) || string.Equals(Program.MyData.GetLanguage(), code, StringComparison.OrdinalIgnoreCase);
			foreach (var file in new[] { UserFile(code), VersionFile(code) })
				if (File.Exists(file)) File.Delete(file);
			Lang.Refresh();
			if (inUse)
			{
				Program.MyData.SetLanguage("auto");
				Lang.Use("auto");
			}
		}

		/// <summary>Use a language now (and keep it as the setting).</summary>
		public static void Use(string code)
		{
			Program.MyData.SetLanguage(code);
			Lang.Use(code);
		}
	}

	/// <summary>Download languages: the online list, each one with Download / Update / Use and ✕ to remove a downloaded one.</summary>
	internal sealed class FormLanguages : ThemedDialog
	{
		readonly LanguageList list;
		const int W = 560;

		public FormLanguages()
		{
			Text = Lang.T("langdl.title");
			iconEmoji = "🌐";
			var close = AddButton(Lang.T("langdl.close"), DialogResult.OK, true);
			CancelButton = close;
			list = new LanguageList(this) { Location = new Point(Pad, 104), Size = new Size(W - Pad * 2, 380) };
			Controls.Add(list);
			close.Font = fText;
			ClientSize = new Size(W, 100);
			ApplyTheme(close);
			int bottom = LayoutButtons(list.Bottom + 26, W);
			ClientSize = new Size(W, bottom + Pad);
			Shown += (s, e) => list.Load();
		}

		/// <summary>The list as it is now (self-test).</summary>
		internal List<LanguageStore.Item> TestItems => list.Items;
		internal bool TestLoading => list.Loading;
		internal string TestError => list.Error;

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			PaintIcon(g);
			Theme.DrawText(g, Lang.T("langdl.title"), fTitle, new Rectangle(Pad + 64, Pad - 2, W - Pad * 2 - 64, 30), Theme.Text, TextFormatFlags.SingleLine);
			Theme.DrawText(g, Lang.T("langdl.intro"), fSmall, new Rectangle(Pad + 64, Pad + 30, W - Pad * 2 - 64, 44), Theme.TextMuted, TextFormatFlags.WordBreak);
			using (var p = new Pen(Theme.Border)) g.DrawLine(p, Pad, buttons[0].Top - 12, W - Pad, buttons[0].Top - 12);
		}

		sealed class LanguageList : Control
		{
			readonly FormLanguages form;
			internal List<LanguageStore.Item> Items = new List<LanguageStore.Item>();
			internal bool Loading;
			internal string Error;
			readonly HashSet<string> busy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			readonly Dictionary<string, string> failed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			readonly List<(Rectangle r, string code, string what)> hits = new List<(Rectangle, string, string)>();
			int scroll;
			(string code, string what) hover;

			public LanguageList(FormLanguages f)
			{
				form = f;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
			}

			public async void Load()
			{
				Loading = true; Error = null; Invalidate();
				try { Items = await LanguageStore.CatalogAsync(); }
				catch (Exception ex) { Error = ex.Message; LogFile.Ignored(ex); }
				Loading = false;
				if (!IsDisposed) Invalidate();
			}

			async void Download(LanguageStore.Item item)
			{
				busy.Add(item.Code); failed.Remove(item.Code); Invalidate();
				try { await LanguageStore.DownloadAsync(item); }
				catch (Exception ex) { failed[item.Code] = ex.Message; LogFile.Ignored(ex); }
				busy.Remove(item.Code);
				if (!IsDisposed) Invalidate();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Background);
				hits.Clear();
				var r0 = new Rectangle(0, 0, Width - 1, Height - 1);
				Theme.FillRounded(g, Theme.Surface, r0, 12);
				Theme.DrawRounded(g, Theme.Border, r0, 12);
				if (Loading || Error != null || Items.Count == 0)
				{
					string msg = Loading ? Lang.T("langdl.loading") : Error != null ? Lang.T("langdl.error", Error) : Lang.T("langdl.error", "—");
					Theme.DrawText(g, "🌐", form.fIcon, new Rectangle(0, Height / 2 - 70, Width, 36), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
					Theme.DrawText(g, msg, form.fText, new Rectangle(24, Height / 2 - 26, Width - 48, 60), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
					if (!Loading) Button(g, new Rectangle(Width / 2 - 80, Height / 2 + 40, 160, 34), Lang.T("langdl.retry"), true, "", "retry");
					return;
				}
				int rowH = 58, max = Math.Max(0, Items.Count * rowH + 12 - Height);
				scroll = Math.Max(0, Math.Min(max, scroll));
				g.SetClip(new Rectangle(1, 1, Width - 2, Height - 2));
				for (int i = 0; i < Items.Count; i++)
				{
					var it = Items[i];
					var r = new Rectangle(8, 6 + i * rowH - scroll, Width - 16, rowH - 6);
					if (r.Bottom < 0 || r.Top > Height) continue;
					if (i % 2 == 1) Theme.FillRounded(g, Theme.SurfaceAlt, r, 8);
					bool builtIn = LanguageStore.IsBuiltIn(it.Code), have = LanguageStore.IsDownloaded(it.Code), inUse = LanguageStore.InUse(it.Code);
					string status = failed.TryGetValue(it.Code, out var err) ? Lang.T("langdl.failed", it.Name ?? it.Code, err)
						: builtIn ? Lang.T("langdl.builtIn") : have ? Lang.T("langdl.downloaded") : "";
					int textW = r.Width - 190;
					Theme.DrawText(g, it.Name ?? it.Code, form.fText, new Rectangle(r.X + 12, r.Y + 5, textW, 22), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					string sub = (it.English ?? it.Code) + (status.Length > 0 ? " · " + status : "");
					Theme.DrawText(g, sub, form.fSmall, new Rectangle(r.X + 12, r.Y + 27, textW, 18), err != null ? Theme.Danger : Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					// actions on the right: the main button, and ✕ for a downloaded language
					var main = new Rectangle(r.Right - 150, r.Y + (r.Height - 32) / 2, 110, 32);
					if (busy.Contains(it.Code)) Button(g, main, Lang.T("langdl.downloading"), false, it.Code, "");
					else if (!builtIn && !have) Button(g, main, Lang.T("langdl.download"), true, it.Code, "download");
					else if (have && LanguageStore.HasUpdate(it)) Button(g, main, Lang.T("langdl.update"), true, it.Code, "download");
					else if (inUse) Button(g, main, Lang.T("langdl.inUse"), false, it.Code, "");
					else Button(g, main, Lang.T("langdl.use"), false, it.Code, "use");
					if (have && !busy.Contains(it.Code))
					{
						var x = new Rectangle(r.Right - 34, r.Y + (r.Height - 28) / 2, 28, 28);
						bool hx = hover.code == it.Code && hover.what == "remove";
						if (hx) Theme.FillRounded(g, Color.FromArgb(40, Theme.Danger), x, 8);
						Theme.DrawText(g, "✕", form.fText, x, hx ? Theme.Danger : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
						hits.Add((x, it.Code, "remove"));
					}
				}
			}

			void Button(Graphics g, Rectangle r, string text, bool primary, string code, string what)
			{
				bool active = what.Length > 0, isHover = active && hover.code == code && hover.what == what;
				var back = !active ? Theme.SurfaceAlt : primary ? (isHover ? ControlPaint.Light(Theme.Accent, 0.15f) : Theme.Accent) : (isHover ? Theme.AccentSoft : Theme.Surface);
				Theme.FillRounded(g, back, r, 8);
				if (!primary || !active) Theme.DrawRounded(g, Theme.Border, r, 8);
				var fore = !active ? Theme.TextMuted : primary ? Color.White : Theme.Text;
				Theme.DrawText(g, text, form.fSmall, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				if (active) hits.Add((r, code, what));
			}

			protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); scroll -= e.Delta / 120 * 50; Invalidate(); }

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var h = hits.FirstOrDefault(x => x.r.Contains(e.Location));
				(string code, string what) now = h.what == null ? (null, null) : (h.code, h.what);
				Cursor = now.what != null ? Cursors.Hand : Cursors.Default;
				if (now != hover) { hover = now; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = (null, null); Invalidate(); }

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				var h = hits.FirstOrDefault(x => x.r.Contains(e.Location));
				if (h.what == null) return;
				if (h.what == "retry") { Load(); return; }
				var item = Items.FirstOrDefault(i => string.Equals(i.Code, h.code, StringComparison.OrdinalIgnoreCase));
				if (item == null) return;
				switch (h.what)
				{
					case "download": Download(item); break;
					case "use": LanguageStore.Use(item.Code); form.Invalidate(true); break;
					case "remove": LanguageStore.Remove(item.Code); form.Invalidate(true); break;
				}
				Invalidate();
			}
		}
	}
}
