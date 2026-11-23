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
			CancelButton = close;
			add.Click += async (s, e) => await AddSource();
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
		async Task AddSource()
		{
			string text = PromptDialog.Ask(Lang.T("sources.addAsk"), "https://github.com/");
			if (string.IsNullOrWhiteSpace(text)) return;
			var source = GallerySources.Parse(text);
			if (source == null) { PetMessage.Show(Lang.T("sources.invalid", text.Trim()), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
			if (list.Sources.Any(s => s.Owner.Equals(source.Owner, StringComparison.OrdinalIgnoreCase) && s.Repo.Equals(source.Repo, StringComparison.OrdinalIgnoreCase)))
			{
				PetMessage.Show(Lang.T("sources.exists", source.Owner + "/" + source.Repo), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			list.Checking = source.Owner + "/" + source.Repo;
			list.Invalidate();
			int count;
			try { count = await GallerySources.CheckAsync(source); }
			catch (Exception ex)
			{
				list.Checking = null; list.Invalidate();
				PetMessage.Show(ex is System.IO.InvalidDataException ? ex.Message : Lang.T("sources.notGallery", source.Owner + "/" + source.Repo) + "\n\n" + ex.Message,
					Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			list.Checking = null; list.Invalidate();
			if (PetMessage.Show(Lang.T("sources.warn", source.Owner + "/" + source.Repo, count), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
			list.Counts[source.Key] = count;
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
				string who = s.IsOfficial ? Lang.T("sources.official")
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
				var rows = Sources.Select(s => (s.Owner + "/" + s.Repo + (s.Branch.Equals("master", StringComparison.OrdinalIgnoreCase) ? "" : " @" + s.Branch), Subtitle(s))).ToList();
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
}
