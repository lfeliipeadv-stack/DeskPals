using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// Where the gallery's pets come from: this program's repository (always first), the original desktopPet and the
	/// repositories the user added. Each one can be turned off, moved up (the higher one wins a pet both have) or removed.
	/// </summary>
	internal sealed class FormGallerySources : ThemedDialog
	{
		readonly SourceList list;
		const int W = 600;
		/// <summary>The sources changed: the gallery must read them again.</summary>
		public bool Changed { get; private set; }

		public FormGallerySources()
		{
			Text = Lang.T("sources.title");
			iconEmoji = "📦";
			var close = AddButton(Lang.T("langdl.close"), DialogResult.OK, true);
			var add = AddButton(Lang.T("sources.add"), DialogResult.None, false);
			// a folder of this computer (whoever makes pets tests a whole collection without uploading it)
			var addFolder = AddButton("📁 " + Lang.T("sources.addFolder"), DialogResult.None, false);
			CancelButton = close;
			add.Click += async (s, e) => await AddSource();
			addFolder.Click += async (s, e) =>
			{
				using (var dlg = new FolderBrowserDialog { Description = Lang.T("sources.addFolderAsk"), UseDescriptionForTitle = true, ShowNewFolderButton = false })
					if (dlg.ShowDialog(this) == DialogResult.OK) await AddSource(dlg.SelectedPath);
			};
			list = new SourceList(this) { Location = new Point(Pad, 112), Size = new Size(W - Pad * 2, 300) };
			Controls.Add(list);
			foreach (var b in buttons) b.Font = fText;
			ClientSize = new Size(W, 100);
			ApplyTheme(close);
			int bottom = LayoutButtons(list.Bottom + 26, W);
			ClientSize = new Size(W, bottom + Pad);
		}

		/// <summary>The sources as they are now (self-test).</summary>
		internal List<GallerySource> TestSources => list.Sources;

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			PaintIcon(g);
			Theme.DrawText(g, Lang.T("sources.title"), fTitle, new Rectangle(Pad + 64, Pad - 2, W - Pad * 2 - 64, 30), Theme.Text, TextFormatFlags.SingleLine);
			Theme.DrawText(g, Lang.T("sources.intro"), fSmall, new Rectangle(Pad + 64, Pad + 30, W - Pad * 2 - 64, 60), Theme.TextMuted, TextFormatFlags.WordBreak);
			using (var p = new Pen(Theme.Border)) g.DrawLine(p, Pad, buttons[0].Top - 12, W - Pad, buttons[0].Top - 12);
		}

		/// <summary>Ask for a link, check that it is a pet gallery, warn, and add it at the end of the list.</summary>
		async Task AddSource(string text = null)
		{
			text ??= PromptDialog.Ask(Lang.T("sources.addAsk"), "https://github.com/");
			if (string.IsNullOrWhiteSpace(text)) return;
			var source = GallerySources.Parse(text);
			if (source == null) { PetMessage.Show(Lang.T("sources.invalid", text.Trim()), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
			list.Checking = source.DisplayName;
			list.Invalidate();
			List<OptionsPet> pets;
			try { pets = await GallerySources.CheckAsync(source); }
			catch (Exception ex)
			{
				list.Checking = null; list.Invalidate();
				PetMessage.Show(ex is System.IO.InvalidDataException ? ex.Message : Lang.T("sources.notGallery", source.DisplayName) + "\n\n" + ex.Message,
					Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			// the same repository and folder already in the list (known only now: the branch and the folder were found)
			if (list.Sources.Any(s => s.Key.Equals(source.Key, StringComparison.OrdinalIgnoreCase)))
			{
				list.Checking = null; list.Invalidate();
				PetMessage.Show(Lang.T("sources.exists", source.DisplayName), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			var icons = await GalleryPreview.LoadIcons(source, pets);
			list.Checking = null; list.Invalidate();
			using (var preview = new GalleryPreview(source, pets, icons))
				if (preview.ShowDialog(this) != DialogResult.Yes) return;
			list.Counts[source.Key] = pets.Count;
			list.Sources.Add(source);
			Save();
		}

		void Save()
		{
			GallerySources.Save(list.Sources);
			Changed = true;
			list.Invalidate();
		}

		sealed class SourceList : Control
		{
			readonly FormGallerySources form;
			internal readonly List<GallerySource> Sources = GallerySources.All();
			internal readonly Dictionary<string, int> Counts = new Dictionary<string, int>(OptionsGit.SourceTotals, StringComparer.OrdinalIgnoreCase);
			internal string Checking;
			readonly List<(Rectangle r, int index, string what)> hits = new List<(Rectangle, int, string)>();
			(int index, string what) hover = (-1, null);
			readonly ToolTip tip = new ToolTip();

			public SourceList(FormGallerySources f)
			{
				form = f;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
			}

			string Subtitle(GallerySource s)
			{
				string who = s.LocalFolder != null ? Lang.T("sources.localFolder")
					: s.IsOfficial ? Lang.T("sources.official")
					: s.Owner.Equals(GallerySources.OriginalOwner, StringComparison.OrdinalIgnoreCase) && s.Repo.Equals(GallerySources.OriginalRepo, StringComparison.OrdinalIgnoreCase) ? Lang.T("sources.original")
					: s.WebUrl.Replace("https://", "");
				if (!s.Enabled) return who + " · " + Lang.T("sources.off");
				return Counts.TryGetValue(s.Key, out int n) ? who + " · " + Lang.T("sources.count", n) : who;
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
				int rowH = 62;
				var rows = Sources.Select(s => (s.DisplayName + (s.LocalFolder != null || s.Branch.Equals("master", StringComparison.OrdinalIgnoreCase) ? "" : " @" + s.Branch), Subtitle(s))).ToList();
				if (Checking != null) rows.Add((Checking, Lang.T("sources.checking", Checking)));
				for (int i = 0; i < rows.Count; i++)
				{
					var r = new Rectangle(8, 6 + i * rowH, Width - 16, rowH - 6);
					if (i % 2 == 1) Theme.FillRounded(g, Theme.SurfaceAlt, r, 8);
					bool real = i < Sources.Count;
					var s = real ? Sources[i] : null;
					int textW = r.Width - 170;
					var color = s != null && !s.Enabled ? Theme.TextMuted : Theme.Text;
					Theme.DrawText(g, (s?.IsOfficial == true ? "★ " : "") + rows[i].Item1, form.fText, new Rectangle(r.X + 12, r.Y + 6, textW, 22), color, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					Theme.DrawText(g, rows[i].Item2, form.fSmall, new Rectangle(r.X + 12, r.Y + 30, textW, 18), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					if (!real || s.IsOfficial) continue;
					// on/off, move up (not above the official one), remove
					var toggle = new Rectangle(r.Right - 156, r.Y + (r.Height - 20) / 2, 44, 20);
					Theme.DrawToggle(g, toggle, s.Enabled);
					hits.Add((Rectangle.Inflate(toggle, 4, 6), i, "toggle"));
					if (i > 1)
					{
						var up = new Rectangle(r.Right - 98, r.Y + (r.Height - 30) / 2, 30, 30);
						Icon(g, up, "▲", hover.index == i && hover.what == "up", false);
						hits.Add((up, i, "up"));
					}
					var x = new Rectangle(r.Right - 52, r.Y + (r.Height - 30) / 2, 30, 30);
					Icon(g, x, "✕", hover.index == i && hover.what == "remove", true);
					hits.Add((x, i, "remove"));
				}
			}

			void Icon(Graphics g, Rectangle r, string glyph, bool isHover, bool danger)
			{
				if (isHover) Theme.FillRounded(g, danger ? Color.FromArgb(40, Theme.Danger) : Theme.Hover, r, 8);
				Theme.DrawText(g, glyph, form.fSmall, r, isHover && danger ? Theme.Danger : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var h = hits.FirstOrDefault(x => x.r.Contains(e.Location));
				(int, string) now = h.what == null ? (-1, null) : (h.index, h.what);
				Cursor = now.Item2 != null ? Cursors.Hand : Cursors.Default;
				if (now != hover)
				{
					hover = now;
					tip.SetToolTip(this, now.Item2 == "up" ? Lang.T("sources.up") : now.Item2 == "remove" ? Lang.T("sources.remove") : null);
					Invalidate();
				}
			}

			protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = (-1, null); Invalidate(); }

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				var h = hits.FirstOrDefault(x => x.r.Contains(e.Location));
				if (h.what == null || h.index < 1 || h.index >= Sources.Count) return;
				var s = Sources[h.index];
				switch (h.what)
				{
					case "toggle": s.Enabled = !s.Enabled; break;
					case "up": Sources.RemoveAt(h.index); Sources.Insert(h.index - 1, s); break;
					case "remove": Sources.RemoveAt(h.index); break;
				}
				form.Save();
			}
		}
	}

	/// <summary>
	/// What a repository brings, before it is added: the icons and names of its pets, 8 at a time (‹ › show the others;
	/// their icons are downloaded when their page is shown), how many there are, and the warning that DeskPals does not
	/// check them. "Add" answers Yes.
	/// </summary>
	internal sealed class GalleryPreview : ThemedDialog
	{
		const int W = 600, Cell = 132, PerPage = 8, RowH = 92;
		readonly string caption, intro;
		readonly GallerySource source;
		readonly List<OptionsPet> pets;
		readonly Dictionary<string, Image> icons;
		readonly Rectangle grid;
		readonly Button prev, next;
		int page;
		bool loading;

		int Pages => Math.Max(1, (pets.Count + PerPage - 1) / PerPage);
		/// <summary>Self-test: the page shown (0 = the first) and a way to turn it.</summary>
		internal int Page => page;
		internal Task TestGo(int to) => Go(to);

		public GalleryPreview(GallerySource source, List<OptionsPet> pets, Dictionary<string, Image> icons)
		{
			this.source = source;
			this.pets = pets;
			this.icons = icons ?? new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
			caption = source.DisplayName;
			Text = Lang.T("sources.title");
			iconEmoji = "📦";
			intro = Lang.T("sources.warn", source.DisplayName, pets.Count);
			var add = AddButton(Lang.T("sources.add"), DialogResult.Yes, true);
			AddButton(Lang.T("rem.cancel"), DialogResult.No, false);
			foreach (var b in buttons) b.Font = fText;
			int introH;
			using (var g = CreateGraphics()) introH = Theme.MeasureText(g, intro, fSmall, W - Pad * 2 - 64, TextFormatFlags.WordBreak).Height;
			int top = Pad + Math.Max(56, 34 + introH) + 14;
			int perRow = (W - Pad * 2) / Cell;
			// as tall as a full page, so the window keeps its size from page to page
			int rows = (Math.Min(pets.Count, PerPage) + perRow - 1) / perRow;
			bool paged = pets.Count > PerPage;
			grid = new Rectangle(Pad, top, W - Pad * 2, Math.Max(1, rows) * RowH + (paged ? 40 : 0) + 12);
			if (paged)
			{
				prev = PageButton("‹", -1);
				next = PageButton("›", +1);
				prev.Location = new Point(grid.X + grid.Width / 2 - 110, grid.Bottom - 40);
				next.Location = new Point(grid.X + grid.Width / 2 + 110 - next.Width, grid.Bottom - 40);
			}
			ClientSize = new Size(W, 100);
			ApplyTheme(add);
			// the arrows look like buttons: a border, the text color, the surface they sit on
			foreach (var b in new[] { prev, next })
				if (b != null)
				{
					b.BackColor = Theme.Surface; b.ForeColor = Theme.Text;
					b.FlatAppearance.BorderColor = Theme.Border; b.FlatAppearance.BorderSize = 1;
					b.FlatAppearance.MouseOverBackColor = Theme.Hover;
				}
			int bottom = LayoutButtons(grid.Bottom + 20, W);
			ClientSize = new Size(W, bottom + Pad);
			UpdatePageButtons();
		}

		Button PageButton(string glyph, int step)
		{
			var b = new Button { Text = glyph, Size = new Size(44, 32), FlatStyle = FlatStyle.Flat, Font = fTitle, TabStop = true, Padding = Padding.Empty };
			b.Click += async (s, e) => await Go(page + step);
			Controls.Add(b);
			return b;
		}

		void UpdatePageButtons()
		{
			if (prev == null) return;
			prev.Enabled = !loading && page > 0;
			next.Enabled = !loading && page < Pages - 1;
		}

		/// <summary>Show another page: its icons are downloaded first (a missing one shows the paw).</summary>
		async Task Go(int to)
		{
			if (to < 0 || to >= Pages || loading) return;
			page = to;
			var missing = pets.Skip(page * PerPage).Take(PerPage).Where(p => !icons.ContainsKey(p.folder)).ToList();
			if (missing.Count > 0)
			{
				loading = true; UpdatePageButtons(); Invalidate();
				var got = await LoadIcons(source, missing);
				foreach (var kv in got) icons[kv.Key] = kv.Value;
				loading = false;
			}
			if (IsDisposed) return;
			UpdatePageButtons();
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			PaintIcon(g);
			Theme.DrawText(g, caption, fTitle, new Rectangle(Pad + 64, Pad - 2, W - Pad * 2 - 64, 30), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			Theme.DrawText(g, intro, fSmall, new Rectangle(Pad + 64, Pad + 32, W - Pad * 2 - 64, grid.Top - Pad - 32), Theme.TextMuted, TextFormatFlags.WordBreak);
			Theme.FillRounded(g, Theme.Surface, grid, 12);
			Theme.DrawRounded(g, Theme.Border, grid, 12);
			int perRow = grid.Width / Cell, x0 = grid.X + (grid.Width - perRow * Cell) / 2;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;   // pixel art stays sharp
			var shown = pets.Skip(page * PerPage).Take(PerPage).ToList();
			for (int i = 0; i < shown.Count; i++)
			{
				var cell = new Rectangle(x0 + (i % perRow) * Cell, grid.Y + 8 + (i / perRow) * RowH, Cell, RowH - 4);
				var iconRect = new Rectangle(cell.X + (Cell - 48) / 2, cell.Y + 6, 48, 48);
				if (icons.TryGetValue(shown[i].folder, out var icon) && icon != null) g.DrawImage(icon, iconRect);
				else Theme.DrawText(g, loading ? "…" : "🐾", fIcon, iconRect, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				Theme.DrawText(g, shown[i].DisplayName, fSmall, new Rectangle(cell.X + 4, cell.Y + 58, Cell - 8, 22), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			}
			// "9–16 / 22" between the arrows
			if (prev != null)
			{
				int first = page * PerPage + 1, last = Math.Min(pets.Count, (page + 1) * PerPage);
				Theme.DrawText(g, first + "–" + last + " / " + pets.Count, fSmall, new Rectangle(prev.Right, prev.Top, next.Left - prev.Right, prev.Height), Theme.TextMuted,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (prev == null) return;
			if (e.KeyCode == Keys.Left || e.KeyCode == Keys.PageUp) { _ = Go(page - 1); e.Handled = true; }
			if (e.KeyCode == Keys.Right || e.KeyCode == Keys.PageDown) { _ = Go(page + 1); e.Handled = true; }
		}

		/// <summary>The icons of these pets (at most one page; a missing one is just left out), a few seconds in all.</summary>
		public static async Task<Dictionary<string, Image>> LoadIcons(GallerySource source, List<OptionsPet> pets)
		{
			var icons = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
			using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(6) })
			{
				http.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
				var tasks = pets.Take(PerPage).Select(async p =>
				{
					try
					{
						string where = p.path ?? p.folder;
						string url = source.BaseUrl + (string.IsNullOrEmpty(where) ? "" : string.Join("/", where.Split('/').Select(Uri.EscapeDataString)) + "/") + "icon.png";
						var bytes = url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? await System.IO.File.ReadAllBytesAsync(new Uri(url).LocalPath) : await http.GetByteArrayAsync(url);
						if (bytes.Length > 512 * 1024) return;
						using (var ms = new System.IO.MemoryStream(bytes))
						using (var img = Image.FromStream(ms))
						{
							var copy = new Bitmap(img);
							lock (icons) icons[p.folder] = copy;
						}
					}
					catch (Exception) { }   // no icon: the paw is shown
				}).ToList();
				await Task.WhenAll(tasks);
			}
			return icons;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) foreach (var i in icons.Values) i?.Dispose();
			base.Dispose(disposing);
		}
	}
}