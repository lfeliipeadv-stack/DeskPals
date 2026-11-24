using DesktopPet.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static DesktopPet.LocalData;

namespace DesktopPet
{
	/// <summary>About page.</summary>
	public sealed partial class FormSettings
	{
		// ================================================================== About

		/// <summary>The license and notices (the Licenses window; the pictures of the windows).</summary>
		internal static string LicensesText() => AboutView.LicensesText();

		/// <summary>The numbers of a pet's detail panel (self-test).</summary>
		internal static List<(string value, string label)> PetStats(XmlDocument doc, string folder) => PetsView.ReadStats(doc, folder);

		/// <summary>The privacy part of the About page ("What DeskPals looks at").</summary>
		internal static (string title, string intro, (string heading, string[] items)[] parts) PrivacyText() => AboutView.PrivacyText();

		private sealed class AboutView : ScrollSurface
		{
			private readonly FormSettings form;
			private readonly List<(Rectangle rect, string url)> links = new List<(Rectangle, string)>();

			public AboutView(FormSettings f) { form = f; }

			private string petTitle = "", petAuthor = "", petVersion = "", petInfo = "";

			/// <summary>Take the header of the pet currently running (no XML parsing, no settings written).</summary>
			protected override void OnVisibleChanged(EventArgs e)
			{
				base.OnVisibleChanged(e);
				if (Visible) ReadHeader();
			}

			/// <summary>
			/// The pet in use, read at every paint: the page is shown by adding it to the window (its visibility does not
			/// change), so a window opened before the pet was loaded, or a pet adopted meanwhile, showed an empty or old card.
			/// </summary>
			private void ReadHeader()
			{
				var header = Program.Mainthread?.CurrentHeader;
				if (header == null) return;
				petTitle = header.Title ?? "";
				petAuthor = header.Author ?? "";
				petVersion = header.Version ?? "";
				petInfo = CleanInfo(header.Info);
			}

			protected override int PaintContent(Graphics g, int scrollY)
			{
				ReadHeader();
				links.Clear();
				int w = Math.Min(760, Width - ScrollBarWidth - 16);
				int x = 8, y = 8 - scrollY;

				var infoSize = Theme.MeasureText(g, petInfo, form.fontSmall, w - 40);
				var card = new Rectangle(x, y, w, 110 + infoSize.Height);
				Theme.FillRounded(g, Theme.Surface, card, 12);
				Theme.DrawRounded(g, Theme.Border, card, 12);
				Theme.DrawText(g, Lang.T("about.currentPet"), form.fontSmall, new Rectangle(card.X + 20, card.Y + 16, 300, 16), Theme.Accent, TextFormatFlags.SingleLine);
				Theme.DrawText(g, petTitle, form.fontH2, new Rectangle(card.X + 20, card.Y + 34, w - 40, 26), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, Lang.T("about.petLine", petAuthor, petVersion), form.fontSmall, new Rectangle(card.X + 20, card.Y + 62, w - 40, 18), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, petInfo, form.fontSmall, new Rectangle(card.X + 20, card.Y + 90, w - 40, infoSize.Height), Theme.Text);
				y = card.Bottom + 16;

				// what the program looks at on this computer, and what goes online
				y = PaintPrivacy(g, x, y, w) + 16;

				// credits: who made what, the data used and the notices (the licenses open in a window)
				var credits = Credits();
				int roleW = Math.Min(300, (w - 40) * 2 / 5);
				var fan = Lang.T("about.credit.claudeFan");
				var fanSize = Theme.MeasureText(g, fan, form.fontSmall, w - 40);
				var cr = new Rectangle(x, y, w, 52 + credits.Count * 30 + 12 + fanSize.Height + 56);
				Theme.FillRounded(g, Theme.Surface, cr, 12);
				Theme.DrawRounded(g, Theme.Border, cr, 12);
				Theme.DrawText(g, Lang.T("about.credits"), form.fontSmall, new Rectangle(cr.X + 20, cr.Y + 16, 300, 16), Theme.Accent, TextFormatFlags.SingleLine);
				int cy = cr.Y + 42;
				foreach (var (role, who, url) in credits)
				{
					Theme.DrawText(g, role, form.fontSmall, new Rectangle(cr.X + 20, cy + 2, roleW - 10, 20), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					var wr = new Rectangle(cr.X + 20 + roleW, cy, w - 40 - roleW, 22);
					string text = url == null ? who : who + "  ↗";
					Theme.DrawText(g, text, form.fontText, wr, url == null ? Theme.Text : Theme.Link, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					if (url != null) links.Add((new Rectangle(wr.X, wr.Y, Math.Min(wr.Width, Theme.MeasureText(g, text, form.fontText).Width), wr.Height), url));
					cy += 30;
				}
				Theme.DrawText(g, fan, form.fontSmall, new Rectangle(cr.X + 20, cy + 6, w - 40, fanSize.Height), Theme.TextMuted);
				var lic = new Rectangle(cr.X + 20, cr.Bottom - 46, Theme.MeasureText(g, Lang.T("about.licenses"), form.fontSmall).Width + 36, 32);
				Theme.DrawButton(g, lic, Lang.T("about.licenses"), form.fontSmall, false, licensesHover);
				licensesButton = lic;
				y = cr.Bottom + 16;

				// links
				var linkList = new[] {
					(Lang.T("about.link.github"), AppInfo.RepoUrl),
					(Lang.T("about.link.gallery"), AppInfo.PetsWebUrl),
					(Lang.T("about.link.help"), AppInfo.HelpUrl) };
				var lr = new Rectangle(x, y, w, 52 + linkList.Length * 30);
				Theme.FillRounded(g, Theme.Surface, lr, 12);
				Theme.DrawRounded(g, Theme.Border, lr, 12);
				Theme.DrawText(g, Lang.T("about.links"), form.fontSmall, new Rectangle(lr.X + 20, lr.Y + 16, 300, 16), Theme.Accent, TextFormatFlags.SingleLine);
				int ly = lr.Y + 42;
				foreach (var l in linkList)
				{
					var r = new Rectangle(lr.X + 20, ly, w - 40, 22);
					Theme.DrawText(g, l.Item1 + "  ↗", form.fontText, r, Theme.Link, TextFormatFlags.SingleLine);
					links.Add((new Rectangle(r.X, r.Y, Theme.MeasureText(g, l.Item1 + "  ↗", form.fontText).Width, r.Height), l.Item2));
					ly += 30;
				}
				y = lr.Bottom + 16;
				return y + scrollY;
			}

			private Rectangle licensesButton;
			private bool licensesHover;

			/// <summary>
			/// "What DeskPals looks at": the plugins that are off until turned on, what the pet notices while it runs,
			/// and what goes online. Every line names what is read, never more (see each plugin's own summary).
			/// </summary>
			internal static (string title, string intro, (string heading, string[] items)[] parts) PrivacyText() =>
				(Lang.T("privacy.title"), Lang.T("privacy.intro"), new[]
				{
					(Lang.T("privacy.off"), new[] { Lang.T("privacy.appTime"), Lang.T("privacy.typing"), Lang.T("privacy.clipboard"), Lang.T("privacy.memory") }),
					(Lang.T("privacy.always"), new[] { Lang.T("privacy.activity"), Lang.T("privacy.screen"), Lang.T("privacy.music"), Lang.T("privacy.calls") }),
					(Lang.T("privacy.online"), new[] { Lang.T("privacy.net") }),
				});

			private int PaintPrivacy(Graphics g, int x, int y, int w)
			{
				var (title, intro, parts) = PrivacyText();
				int inner = w - 40;
				// measure first: the card is as tall as its text
				int h = 42 + Theme.MeasureText(g, intro, form.fontSmall, inner).Height + 10;
				foreach (var (heading, items) in parts)
				{
					h += 30;
					foreach (var it in items) h += Theme.MeasureText(g, it, form.fontSmall, inner - 16).Height + 6;
				}
				var card = new Rectangle(x, y, w, h + 12);
				Theme.FillRounded(g, Theme.Surface, card, 12);
				Theme.DrawRounded(g, Theme.Border, card, 12);
				Theme.DrawText(g, title, form.fontSmall, new Rectangle(card.X + 20, card.Y + 16, inner, 16), Theme.Accent, TextFormatFlags.SingleLine);
				int cy = card.Y + 42;
				var introSize = Theme.MeasureText(g, intro, form.fontSmall, inner);
				Theme.DrawText(g, intro, form.fontSmall, new Rectangle(card.X + 20, cy, inner, introSize.Height), Theme.Text, TextFormatFlags.WordBreak);
				cy += introSize.Height + 10;
				foreach (var (heading, items) in parts)
				{
					Theme.DrawText(g, heading, form.fontBold, new Rectangle(card.X + 20, cy + 4, inner, 22), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					cy += 30;
					foreach (var it in items)
					{
						var size = Theme.MeasureText(g, it, form.fontSmall, inner - 16);
						using (var b = new SolidBrush(Theme.TextMuted)) g.FillEllipse(b, card.X + 22, cy + 6, 5, 5);
						Theme.DrawText(g, it, form.fontSmall, new Rectangle(card.X + 36, cy, inner - 16, size.Height), Theme.TextMuted, TextFormatFlags.WordBreak);
						cy += size.Height + 6;
					}
				}
				return card.Bottom;
			}

			/// <summary>The people and the data behind the program (role, who, link or null).</summary>
			internal static List<(string role, string who, string url)> Credits() => new List<(string, string, string)>
			{
				(Lang.T("about.credit.art"), "Tatsutoshi Nomura (Stray Sheep, 1994)", null),
				(Lang.T("about.credit.app"), "Adriano Petrucci", "https://github.com/Adrianotiger/desktopPet"),
				(Lang.T("about.credit.tray"), "Sergi Fumanya Grunwaldt", null),
				(Lang.T("about.credit.toss"), "Zecheriah", null),
				(Lang.T("about.credit.fork"), "Luan Felipe", AppInfo.RepoUrl),
				(Lang.T("about.credit.pets"), "Oliver B. · TheSuperCat · Adriano Petrucci", AppInfo.PetsWebUrl),
				(Lang.T("about.credit.pingus"), Lang.T("about.credit.pingusWho"), "https://pingus.seul.org/"),
				(Lang.T("about.credit.assistants"), "Microsoft · BONZI Software · ClippyJS", "https://github.com/pithings/clippy"),
				(Lang.T("about.credit.weather"), "Open-Meteo.com (CC BY 4.0)", "https://open-meteo.com/"),
				(Lang.T("about.credit.places"), "GeoNames.org (CC BY 4.0)", "https://www.geonames.org/"),
			};

			/// <summary>The license of the program (LICENSE.md, embedded) and the notices of what came from others.</summary>
			internal static string LicensesText()
			{
				string mit = "";
				try
				{
					using (var s = typeof(FormSettings).Assembly.GetManifestResourceStream("LICENSE.md"))
					using (var r = new System.IO.StreamReader(s))
						// one line per paragraph (the file breaks its lines at 80 columns: the window breaks them by itself)
						mit = Regex.Replace(r.ReadToEnd().Replace("\r", "").Trim(), @"(?<!\n)\n(?!\n)", " ");
				}
				catch (Exception skipped) { LogFile.Ignored(skipped); }
				return string.Join("\n\n", new[]
				{
					Lang.T("about.licenses.intro"),
					mit,
					"—",
					Lang.T("about.licenses.sheep"),
					Lang.T("about.licenses.pingus"),
					Lang.T("about.licenses.assistants"),
					Lang.T("about.licenses.data"),
					Lang.T("about.credit.claudeFan"),
				});
			}

			private static void ShowLicenses(IWin32Window owner) =>
				TextViewer.Show(owner, Lang.T("about.licenses.title"), LicensesText(), "📜");

			private static string CleanInfo(string info)
			{
				if (string.IsNullOrEmpty(info)) return "";
				info = info.Replace("[br]", "\n");
				info = Regex.Replace(info, @"\[link:(.*?)\]", "$1");
				return info.Trim();
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var l = links.FirstOrDefault(x => x.rect.Contains(e.Location));
				bool overLicenses = licensesButton.Contains(e.Location);
				if (overLicenses != licensesHover) { licensesHover = overLicenses; Invalidate(); }
				Cursor = l.url != null || overLicenses ? Cursors.Hand : Cursors.Default;
				if (l.url != null) form.StatusText = l.url;
			}

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				if (licensesButton.Contains(e.Location)) { ShowLicenses(form); return; }
				var l = links.FirstOrDefault(x => x.rect.Contains(e.Location));
				if (l.url != null) Program.OpenLink(l.url);
			}
		}

	}
}
