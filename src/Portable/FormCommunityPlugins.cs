using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// The plugins made by the community: the ones installed (on/off, remove) and the ones the gallery offers (install).
	/// "Open the folder" for the user's own plugin files, "How to make one" for the format.
	/// </summary>
	internal sealed class FormCommunityPlugins : ThemedDialog
	{
		readonly PluginList list;
		const int W = 640;

		public FormCommunityPlugins()
		{
			Text = Lang.T("cp.title");
			iconEmoji = "🧩";
			var close = AddButton(Lang.T("langdl.close"), DialogResult.OK, true);
			var folder = AddButton(Lang.T("cp.folder"), DialogResult.None, false);
			var how = AddButton(Lang.T("cp.howto"), DialogResult.None, false);
			CancelButton = close;
			folder.Click += (s, e) =>
			{
				try { System.IO.Directory.CreateDirectory(CommunityPlugins.Folder); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(CommunityPlugins.Folder) { UseShellExecute = true }); }
				catch (Exception skipped) { LogFile.Ignored(skipped); }
				CommunityPlugins.Reload(); list.Invalidate();
			};
			how.Click += (s, e) =>
			{
				try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppInfo.RepoUrl + "/blob/" + AppInfo.Branch + "/docs/plugins.md") { UseShellExecute = true }); }
				catch (Exception skipped) { LogFile.Ignored(skipped); }
			};
			list = new PluginList(this) { Location = new Point(Pad, 112), Size = new Size(W - Pad * 2, 360) };
			Controls.Add(list);
			foreach (var b in buttons) b.Font = fText;
			ClientSize = new Size(W, 100);
			ApplyTheme(close);
			int bottom = LayoutButtons(list.Bottom + 26, W);
			ClientSize = new Size(W, bottom + Pad);
			Shown += async (s, e) => await list.LoadGallery();
		}

		/// <summary>Self-test: the rows shown.</summary>
		internal int TestRows => list.RowCount;

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			PaintIcon(g);
			Theme.DrawText(g, Lang.T("cp.title"), fTitle, new Rectangle(Pad + 64, Pad - 2, W - Pad * 2 - 64, 30), Theme.Text, TextFormatFlags.SingleLine);
			Theme.DrawText(g, Lang.T("cp.intro"), fSmall, new Rectangle(Pad + 64, Pad + 30, W - Pad * 2 - 64, 60), Theme.TextMuted, TextFormatFlags.WordBreak);
			using (var p = new Pen(Theme.Border)) g.DrawLine(p, Pad, buttons[0].Top - 12, W - Pad, buttons[0].Top - 12);
		}

		sealed class PluginList : FormSettings.ScrollSurface
		{
			readonly FormCommunityPlugins form;
			List<CommunityPlugins.Entry> gallery;
			string galleryState = "";   // "" loading, "offline", "ok"
			string busy;
			readonly List<(Rectangle r, string id, string what)> hits = new List<(Rectangle, string, string)>();
			(string id, string what) hover;
			Font emoji;

			public PluginList(FormCommunityPlugins f) { form = f; }

			internal int RowCount;

			public async Task LoadGallery()
			{
				try { gallery = await CommunityPlugins.Gallery(); galleryState = "ok"; }
				catch (Exception ex) { gallery = new List<CommunityPlugins.Entry>(); galleryState = "offline"; StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "plugin gallery: " + ex.Message); }
				if (!IsDisposed) Invalidate();
			}

			protected override int PaintContent(Graphics g, int scrollY)
			{
				emoji ??= Theme.CreateFont(16);
				hits.Clear();
				g.Clear(Theme.Background);
				var installed = CommunityPlugins.Installed;
				var rows = installed.Select(p => (id: p.Id, emoji: p.Emoji, name: p.Name, sub: Lang.T("cp.by", p.Author.Length > 0 ? p.Author : "?") + (p.Description.Length > 0 ? " · " + p.Description : ""), isInstalled: true)).ToList();
				if (gallery != null)
					rows.AddRange(gallery.Where(e => !installed.Any(p => p.Id == e.Id)).Select(e => (id: e.Id, emoji: e.Emoji, name: e.Name, sub: Lang.T("cp.by", e.Author.Length > 0 ? e.Author : "?") + (e.Description.Length > 0 ? " · " + e.Description : ""), isInstalled: false)));
				RowCount = rows.Count;
				int y = 4 - scrollY, rowH = 70, w = Width - ScrollBarWidth - 8;
				if (rows.Count == 0)
				{
					string empty = galleryState == "" ? Lang.T("cp.loading") : galleryState == "offline" ? Lang.T("cp.offline") : Lang.T("cp.empty");
					Theme.DrawText(g, empty, form.fSmall, new Rectangle(12, y + 12, w - 24, 60), Theme.TextMuted, TextFormatFlags.WordBreak);
					return 80;
				}
				foreach (var row in rows)
				{
					var r = new Rectangle(4, y, w - 4, rowH - 6);
					Theme.FillRounded(g, Theme.Surface, r, 10);
					Theme.DrawRounded(g, Theme.Border, r, 10);
					Theme.DrawText(g, row.emoji, emoji, new Rectangle(r.X + 10, r.Y + 10, 34, 34), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					int textW = r.Width - 60 - 170;
					Theme.DrawText(g, row.name, form.fText, new Rectangle(r.X + 52, r.Y + 8, textW, 22), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					Theme.DrawText(g, row.sub, form.fSmall, new Rectangle(r.X + 52, r.Y + 32, textW, 28), Theme.TextMuted, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
					if (row.isInstalled)
					{
						var toggle = new Rectangle(r.Right - 110, r.Y + (r.Height - 20) / 2, 44, 20);
						Theme.DrawToggle(g, toggle, CommunityPlugins.IsOn(row.id));
						hits.Add((Rectangle.Inflate(toggle, 4, 6), row.id, "toggle"));
						var x = new Rectangle(r.Right - 52, r.Y + (r.Height - 30) / 2, 30, 30);
						bool hx = hover == (row.id, "remove");
						if (hx) Theme.FillRounded(g, Color.FromArgb(40, Theme.Danger), x, 8);
						Theme.DrawText(g, "✕", form.fSmall, x, hx ? Theme.Danger : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
						hits.Add((x, row.id, "remove"));
					}
					else
					{
						var b = new Rectangle(r.Right - 140, r.Y + (r.Height - 30) / 2, 126, 30);
						Theme.DrawButton(g, b, busy == row.id ? "…" : Lang.T("cp.install"), form.fSmall, true, hover == (row.id, "install"));
						hits.Add((b, row.id, "install"));
					}
					y += rowH;
				}
				return y + scrollY + 4;
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var h = hits.FirstOrDefault(x => x.r.Contains(e.Location));
				var now = h.what == null ? (null, null) : (h.id, h.what);
				Cursor = now.Item2 != null ? Cursors.Hand : Cursors.Default;
				if (now != hover) { hover = now; Invalidate(); }
			}

			protected override async void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				var h = hits.FirstOrDefault(x => x.r.Contains(e.Location));
				if (h.what == null || busy != null) return;
				switch (h.what)
				{
					case "toggle": CommunityPlugins.SetOn(h.id, !CommunityPlugins.IsOn(h.id)); break;
					case "remove": CommunityPlugins.Remove(h.id); break;
					case "install":
						var entry = gallery?.FirstOrDefault(x => x.Id == h.id);
						if (entry == null) return;
						busy = h.id; Invalidate();
						try { await CommunityPlugins.InstallFromGallery(entry); if (!CommunityPlugins.Enabled) CommunityPlugins.Enabled = true; }
						catch (Exception ex) { PetMessage.Show(form, Lang.T("cp.installFail", entry.Name, ex.Message), form.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
						busy = null;
						break;
				}
				if (!IsDisposed) Invalidate();
			}
		}
	}

	/// <summary>The programs whose windows the pets stay away from: the list (remove), and add one of the programs open now.</summary>
	internal sealed class FormIgnoredApps : ThemedDialog
	{
		readonly AppList list;
		const int W = 560;

		public FormIgnoredApps()
		{
			Text = Lang.T("apps.title");
			iconEmoji = "🪟";
			var close = AddButton(Lang.T("langdl.close"), DialogResult.OK, true);
			var add = AddButton(Lang.T("apps.add"), DialogResult.None, false);
			CancelButton = close;
			add.Click += (s, e) =>
			{
				var open = WindowRules.OpenPrograms().Where(p => !WindowRules.Apps.Contains(p.app)).ToList();
				var menu = new ContextMenuStrip();
				Theme.ApplyTo(menu);
				if (open.Count == 0) menu.Items.Add(new ToolStripMenuItem(Lang.T("apps.none")) { Enabled = false });
				foreach (var p in open)
				{
					string title = p.title.Length > 40 ? p.title.Substring(0, 39) + "…" : p.title;
					menu.Items.Add(p.app + "  —  " + title, null, (o, a) => { var apps = WindowRules.Apps; apps.Add(p.app); WindowRules.Apps = apps; if (!WindowRules.Enabled) WindowRules.Enabled = true; list.Invalidate(); });
				}
				menu.Show(add, new Point(0, add.Height));
			};
			list = new AppList(this) { Location = new Point(Pad, 112), Size = new Size(W - Pad * 2, 240) };
			Controls.Add(list);
			foreach (var b in buttons) b.Font = fText;
			ClientSize = new Size(W, 100);
			ApplyTheme(close);
			int bottom = LayoutButtons(list.Bottom + 26, W);
			ClientSize = new Size(W, bottom + Pad);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			PaintIcon(g);
			Theme.DrawText(g, Lang.T("apps.title"), fTitle, new Rectangle(Pad + 64, Pad - 2, W - Pad * 2 - 64, 30), Theme.Text, TextFormatFlags.SingleLine);
			Theme.DrawText(g, Lang.T("apps.intro"), fSmall, new Rectangle(Pad + 64, Pad + 30, W - Pad * 2 - 64, 60), Theme.TextMuted, TextFormatFlags.WordBreak);
			using (var p = new Pen(Theme.Border)) g.DrawLine(p, Pad, buttons[0].Top - 12, W - Pad, buttons[0].Top - 12);
		}

		sealed class AppList : FormSettings.ScrollSurface
		{
			readonly FormIgnoredApps form;
			readonly List<(Rectangle r, string app)> hits = new List<(Rectangle, string)>();
			string hover;
			public AppList(FormIgnoredApps f) { form = f; }

			protected override int PaintContent(Graphics g, int scrollY)
			{
				g.Clear(Theme.Background);
				hits.Clear();
				var apps = WindowRules.Apps;
				int y = 4 - scrollY, w = Width - ScrollBarWidth - 8;
				if (apps.Count == 0) { Theme.DrawText(g, Lang.T("apps.empty"), form.fSmall, new Rectangle(12, y + 12, w - 24, 60), Theme.TextMuted, TextFormatFlags.WordBreak); return 80; }
				foreach (var app in apps)
				{
					var r = new Rectangle(4, y, w - 4, 44);
					Theme.FillRounded(g, Theme.Surface, r, 10);
					Theme.DrawRounded(g, Theme.Border, r, 10);
					Theme.DrawText(g, app, form.fText, new Rectangle(r.X + 14, r.Y + 10, r.Width - 70, 24), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					var x = new Rectangle(r.Right - 44, r.Y + 7, 30, 30);
					if (hover == app) Theme.FillRounded(g, Color.FromArgb(40, Theme.Danger), x, 8);
					Theme.DrawText(g, "✕", form.fSmall, x, hover == app ? Theme.Danger : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					hits.Add((x, app));
					y += 50;
				}
				return y + scrollY + 4;
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				string h = hits.FirstOrDefault(x => x.r.Contains(e.Location)).app;
				Cursor = h != null ? Cursors.Hand : Cursors.Default;
				if (h != hover) { hover = h; Invalidate(); }
			}

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				string h = hits.FirstOrDefault(x => x.r.Contains(e.Location)).app;
				if (h == null) return;
				var apps = WindowRules.Apps; apps.Remove(h); WindowRules.Apps = apps;
				Invalidate();
			}
		}
	}
}
