using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>The Achievements page: how many were won, the notice at the top of the screen, and a card for each one.</summary>
	public sealed partial class FormSettings
	{
		private sealed class AchievementsView : ScrollSurface
		{
			private readonly FormSettings form;
			private Rectangle toggleRect, previewRect;
			private bool hoverPreview;

			public AchievementsView(FormSettings f)
			{
				form = f;
				Achievements.Changed += Changed;
				Disposed += (s, e) => Achievements.Changed -= Changed;
			}

			void Changed()
			{
				if (IsDisposed) return;
				if (InvokeRequired) { BeginInvoke(new Action(Changed)); return; }
				Invalidate();
				if (form.page == Page.Achievements) form.headerSubtitle.Text = Lang.T("page.achievements.subtitle", Achievements.WonCount, Achievements.All.Length);
			}

			protected override int PaintContent(Graphics g, int scrollY)
			{
				int w = Width - ScrollBarWidth - 16;
				int y = 8 - scrollY;
				int won = Achievements.WonCount, total = Achievements.All.Length;

				// --- summary: big trophy, "7 of 20", a bar; the notice switch and a preview on the right
				int rightW = Math.Min(300, Math.Max(200, w / 3));
				// narrow window: the notice switch and the preview go under the count instead of beside it
				bool stacked = w - 104 - rightW - 24 < 260;
				var top = new Rectangle(8, y, w, stacked ? 150 : 104);
				Theme.FillRounded(g, Theme.Surface, top, 10);
				Theme.DrawRounded(g, Theme.Border, top, 10);
				Achievements.DrawTrophy(g, new RectangleF(top.X + 18, top.Y + 18, 68, 68), won > 0);
				int tx = top.X + 104, tw = stacked ? top.Width - 104 - 16 : top.Width - 104 - rightW - 24;
				Theme.DrawText(g, Lang.T("ach.count", won, total), form.fontH2, new Rectangle(tx, top.Y + 20, tw, 28), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				var bar = new Rectangle(tx, top.Y + 56, Math.Max(40, tw), 10);
				Theme.FillRounded(g, Theme.SurfaceAlt, bar, 5);
				if (won > 0) Theme.FillRounded(g, Color.FromArgb(222, 164, 40), new Rectangle(bar.X, bar.Y, Math.Max(10, bar.Width * won / total), bar.Height), 5);
				Theme.DrawText(g, won == total ? Lang.T("ach.allDone") : Lang.T("ach.keepGoing"), form.fontSmall, new Rectangle(tx, top.Y + 72, Math.Max(40, tw), 20), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

				string pv = Lang.T("ach.preview");
				int pvW = Theme.MeasureText(g, pv, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 28;
				if (stacked)
				{
					// one row under the count: [switch] Notice at the top …… [See how it looks]
					int ry = top.Y + 104;
					toggleRect = new Rectangle(tx, ry + 4, 44, 22);
					previewRect = new Rectangle(top.Right - 16 - pvW, ry, pvW, 30);
					Theme.DrawText(g, Lang.T("ach.toast"), form.fontText, new Rectangle(tx + 54, ry, Math.Max(40, previewRect.X - tx - 64), 30), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
				}
				else
				{
					int rx = top.Right - rightW - 16;
					toggleRect = new Rectangle(top.Right - 16 - 44, top.Y + 22, 44, 22);
					previewRect = new Rectangle(top.Right - 16 - pvW, top.Y + 60, pvW, 30);
					Theme.DrawText(g, Lang.T("ach.toast"), form.fontText, new Rectangle(rx, top.Y + 20, rightW - 56, 26), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
				}
				Theme.DrawToggle(g, toggleRect, Achievements.ShowToast);
				Theme.DrawButton(g, previewRect, pv, form.fontSmall, false, hoverPreview);
				y = top.Bottom + 16;

				// --- a section for each group ("First steps 3/12"), its cards as many per row as fit
				const int gap = 12, minW = 250, h = 112;
				int cols = Math.Max(1, (w + gap) / (minW + gap));
				int cw = (w - gap * (cols - 1)) / cols;
				foreach (Achievements.Group grp in Enum.GetValues(typeof(Achievements.Group)))
				{
					var list = Achievements.All.Where(a => a.Group == grp).ToList();
					if (list.Count == 0) continue;
					int got = list.Count(a => Achievements.IsWon(a.Id));
					string head = Lang.T("ach.group." + grp.ToString().ToLowerInvariant()).ToUpperInvariant() + "   " + got + " / " + list.Count;
					Theme.DrawText(g, head, form.fontSmall, new Rectangle(14, y + 4, w, 22), Theme.Accent, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
					y += 32;
					for (int i = 0; i < list.Count; i++)
					{
						var r = new Rectangle(8 + (i % cols) * (cw + gap), y + (i / cols) * (h + gap), cw, h);
						if (r.Bottom >= 0 && r.Top <= Height) DrawCard(g, r, list[i]);
					}
					y += (list.Count + cols - 1) / cols * (h + gap) + 8;
				}
				return y + scrollY;
			}
			void DrawCard(Graphics g, Rectangle r, Achievements.Achievement a)
			{
				var day = Achievements.WonOn(a.Id);
				bool won = day != null, hidden = a.Secret && !won;
				Theme.FillRounded(g, won ? Theme.Surface : Theme.Mix(Theme.Surface, Theme.Background, 0.5f), r, 10);
				Theme.DrawRounded(g, won ? Color.FromArgb(Theme.IsDark ? 140 : 170, 222, 164, 40) : Theme.Border, r, 10);
				var icon = new RectangleF(r.X + 14, r.Y + 16, 52, 52);
				Achievements.DrawTrophy(g, icon, won);
				if (hidden) TextRenderer.DrawText(g, "?", form.fontBold, Rectangle.Round(new RectangleF(icon.X, icon.Y + 4, icon.Width, icon.Height * 0.5f)), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				int tx = r.X + 78, tw = r.Width - 78 - 12;
				string title = hidden ? Lang.T("ach.secret.title") : a.Title;
				string desc = hidden ? Lang.T("ach.secret.desc") : a.Description;
				Theme.DrawText(g, title, form.fontBold, new Rectangle(tx, r.Y + 12, tw, 22), won ? Theme.Text : Theme.Mix(Theme.Text, Theme.TextMuted, 0.4f), TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, desc, form.fontSmall, new Rectangle(tx, r.Y + 36, tw, 40), Theme.TextMuted, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
				var bottom = new Rectangle(tx, r.Bottom - 28, tw, 18);
				if (won)
					Theme.DrawText(g, "🏆 " + Lang.T("ach.wonOn", day.Value.ToString("d", System.Globalization.CultureInfo.CurrentCulture)), form.fontSmall, bottom, Theme.IsDark ? Color.FromArgb(240, 196, 90) : Color.FromArgb(170, 112, 10), TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
				else if ((a.Goal > 1 || a.Id == "adoptAll" && Achievements.GalleryProgress().total > 0) && !hidden)
				{
					int p = Achievements.Progress(a), goal = a.Goal;
					if (a.Id == "adoptAll") (p, goal) = Achievements.GalleryProgress();
					string n = a.Scale == 60 ? p / 60 + " / " + goal / 60 + " h" : p + " / " + goal;
					int nw = Theme.MeasureText(g, n, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width;
					var bar = new Rectangle(tx, bottom.Y + 6, Math.Max(20, tw - nw - 10), 7);
					Theme.FillRounded(g, Theme.SurfaceAlt, bar, 3.5f);
					if (p > 0) Theme.FillRounded(g, Theme.Accent, new Rectangle(bar.X, bar.Y, Math.Max(7, (int)((long)bar.Width * p / goal)), bar.Height), 3.5f);
					Theme.DrawText(g, n, form.fontSmall, new Rectangle(bar.Right + 8, bottom.Y, nw + 4, bottom.Height), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
				}
				else
					Theme.DrawText(g, "🔒 " + Lang.T("ach.locked"), form.fontSmall, bottom, Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				bool p = previewRect.Contains(e.Location);
				Cursor = p || toggleRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
				if (p != hoverPreview) { hoverPreview = p; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);
				if (hoverPreview) { hoverPreview = false; Invalidate(); }
			}

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				if (e.Button != MouseButtons.Left) return;
				if (Rectangle.Inflate(toggleRect, 6, 6).Contains(e.Location)) { Achievements.ShowToast = !Achievements.ShowToast; Invalidate(); }
				else if (previewRect.Contains(e.Location)) Achievements.Preview();
			}
		}
	}
}
