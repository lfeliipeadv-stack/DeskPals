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
	/// <summary>Pets page: the cards, tabs and the detail panel.</summary>
	public sealed partial class FormSettings
	{
		// ================================================================== Pets gallery

		private sealed class PetsView : ScrollSurface
		{
			private readonly FormSettings form;
			private OptionsPet selected;
			private OptionsPet hoverPet;
			private bool hoverAdopt, hoverClose;
			// the whole name and author of the card under the mouse (a long author is cut on the card)
			private readonly ToolTip cardTip = new ToolTip { InitialDelay = 500, ReshowDelay = 100 };
			private string filter = "";
			private readonly List<(Rectangle rect, OptionsPet pet)> cards = new List<(Rectangle, OptionsPet)>();
			private readonly List<(Rectangle rect, string url)> links = new List<(Rectangle, string)>();
			private Rectangle adoptRect, closeRect;
			private readonly List<(Rectangle rect, int index)> swatches = new List<(Rectangle, int)>();
			private int hoverSwatch = -1;
			public bool Loading = true;
			public string CurrentPet;
			/// <summary>Pet being downloaded (its button becomes a progress bar), and how far (0..1).</summary>
			public string DownloadingFolder;
			public double DownloadProgress;
			/// <summary>Pet whose last download failed: its button offers to try again.</summary>
			public string FailedFolder;
			/// <summary>The gallery list could not be fetched: only pets on this computer are shown.</summary>
			public bool Offline;
			private Rectangle cancelRect, removeRect, retryListRect, soundRect, playRect;
			private bool hoverCancel, hoverRemove, hoverRetryList, hoverSound, hoverPlay;
			private readonly Random soundPick = new Random();

			/// <summary>
			/// Play one of the pet's sounds (a different one each click), at the program's volume, or softly when the
			/// volume is 0, so the user hears what the pet sounds like before turning its sounds on.
			/// </summary>
			private void PlayPreview(OptionsPet pet)
			{
				try
				{
					var sounds = pet.Detail?.DocumentElement?.SelectSingleNode("*[local-name()='sounds']")?.ChildNodes.OfType<XmlElement>()
						.Select(s => s.SelectSingleNode("*[local-name()='base64']")?.InnerText?.Trim()).Where(b => !string.IsNullOrEmpty(b)).ToList();
					if (sounds == null || sounds.Count == 0) return;
					string b64 = sounds[soundPick.Next(sounds.Count)];
					int i = b64.IndexOf(";base64,");
					if (i > 0) b64 = b64.Substring(i + 8);
					float volume = Program.MyData.GetVolume(true);
					Mp3Player.Load(Convert.FromBase64String(b64)).Play(volume > 0 ? volume : 0.3, 0);
				}
				catch (Exception ex) { form.StatusText = ex.Message; LogFile.Ignored(ex); }
			}

			// with the detail panel open, the page scroll bar sits next to the cards, not on the panel
			protected override int ScrollBarX => selected != null ? GridArea.Right : base.ScrollBarX;
			private Rectangle readmeArea;
			private int detailScroll, detailMax;
			private const int CardW = 150, CardH = 150, Gap = 14;
			private const int DetailW = 340;

			public PetsView(FormSettings f)
			{
				form = f;
				// nothing downloaded yet: start where the pets are
				if (PetStore.Folders().Count == 0) CurrentTab = Tab.Download;
			}

			public bool DetailOpen => selected != null;

			public string Filter
			{
				get => filter;
				set { filter = (value ?? "").Trim(); ScrollY = 0; Invalidate(); }
			}

			public void PetsChanged() => Invalidate();

			public void CloseDetail() { selected = null; detailScroll = 0; Invalidate(); }

			public void SelectForSnapshot(OptionsPet pet) { selected = pet; detailScroll = 0; Invalidate(); }

			protected override void OnMouseWheel(MouseEventArgs e)
			{
				// over the description of the detail panel: scroll the description, not the grid
				if (selected != null && detailMax > 0 && readmeArea.Contains(e.Location))
				{
					detailScroll = Math.Max(0, Math.Min(detailMax, detailScroll - e.Delta / 120 * 40));
					Invalidate();
					return;
				}
				base.OnMouseWheel(e);
			}

			/// <summary>Tabs of the gallery: pets on this computer, and pets to download.</summary>
			public enum Tab { Mine, Download }
			public Tab CurrentTab = Tab.Mine;

			internal List<string> TestShown() => FilteredPets().Select(p => p.folder).ToList();
			internal List<string> TestSourceChips() => sourceChips.Select(c => c.key).ToList();
			internal void TestSource(string key) { sourceFilter = key ?? ""; Invalidate(); }
			internal List<string> TestShownSources() => FilteredPets().Select(SourceKey).Distinct().ToList();
			internal void TestTab(Tab t) { if (DetailOpen) CloseDetail(); CurrentTab = t; show = ShowMode.All; Invalidate(); }

			/// <summary>"Download pets", only the new ones (the pet's "See" button).</summary>
			public void ShowNew()
			{
				if (DetailOpen) CloseDetail();
				CurrentTab = Tab.Download;
				show = ShowMode.New;
				ScrollY = 0;
				Invalidate();
			}
			private readonly List<(Rectangle rect, Tab tab)> tabRects = new List<(Rectangle, Tab)>();
			private int hoverTab = -1;

			/// <summary>On this computer: the built-in eSheep and every downloaded pet (a color group if any color is).</summary>
			private bool IsMine(OptionsPet p) => p.Variants != null
				? p.Variants.Any(v => v != null && form.IsDownloaded(v))
				: form.IsDownloaded(p);

			/// <summary>Still to download (a color group while some color is missing).</summary>
			private bool IsToDownload(OptionsPet p) => p.Variants != null
				? p.Variants.Any(v => v != null && !form.IsDownloaded(v))
				: !form.IsDownloaded(p);

			private IEnumerable<OptionsPet> TabPets(Tab t) => form.WebPets.pets.Where(p => t == Tab.Mine ? IsMine(p) : IsToDownload(p));

			/// <summary>Which pets the gallery shows: all, the ones made for this project, the community's, the ones with sounds.</summary>
			private enum ShowMode { All, Original, Community, Sounds, New }
			private ShowMode show = ShowMode.All;
			private readonly List<(Rectangle rect, ShowMode mode)> showChips = new List<(Rectangle, ShowMode)>();
			private int hoverShow = -1;
			private Rectangle sourcesRect;
			private bool hoverSources;
			/// <summary>"Create a pet": opens the Pet Editor (or offers to download it).</summary>
			private Rectangle editorRect;
			private bool hoverEditor;
			/// <summary>Author of the pets made for this project (the "Originals" filter).</summary>
			internal const string ProjectAuthor = "Luan Felipe";

			private static IEnumerable<OptionsPet> Each(OptionsPet p) => p.Variants != null ? p.Variants.Where(v => v != null) : new[] { p };

			private bool Shown(OptionsPet p)
			{
				switch (show)
				{
					case ShowMode.Original: return Each(p).Any(v => string.Equals(v.author?.Trim(), ProjectAuthor, StringComparison.OrdinalIgnoreCase));
					case ShowMode.Community: return !Each(p).Any(v => string.Equals(v.author?.Trim(), ProjectAuthor, StringComparison.OrdinalIgnoreCase));
					case ShowMode.Sounds: return Each(p).Any(v => v.sounds > 0 || (v.Detail != null && CountOf(v.Detail, "sounds") > 0));
					case ShowMode.New: return form.IsNewPet(p);
					default: return true;
				}
			}

			// ------------------------------------------------------------------ "Source: All | Official gallery | owner/repo…"

			/// <summary>The source chosen ("" = all, "official", "local" = only on this computer, or a source key "owner/repo/path").</summary>
			private string sourceFilter = Program.MyData.GetSetting("GallerySourceFilter") ?? "";
			private readonly List<(Rectangle rect, string key)> sourceChips = new List<(Rectangle, string)>();
			private int hoverSourceChip = -1;

			/// <summary>Where a card comes from: "official" (also the pets that come with the program), "local", or the key of its source.</summary>
			internal static string SourceKey(OptionsPet p)
			{
				var r = Each(p).FirstOrDefault() ?? p;
				if ((r.folder ?? "").StartsWith(PetStore.LocalPrefix, StringComparison.OrdinalIgnoreCase)) return "local";
				var s = r.Source ?? OptionsGit.SourceOf(r.folder);
				return s == null || s.IsOfficial ? "official" : s.Key;
			}

			/// <summary>The sources with pets now, in order: official, the added ones (as in the Sources window), this computer.</summary>
			private List<(string key, string text)> SourceList()
			{
				var keys = new HashSet<string>(form.WebPets.pets.Select(SourceKey));
				var list = new List<(string, string)>();
				if (keys.Contains("official")) list.Add(("official", Lang.T("pets.source.official")));
				foreach (var s in GallerySources.Active().Where(s => !s.IsOfficial && keys.Contains(s.Key)))
					list.Add((s.Key, s.LocalFolder != null ? "📁 " + s.Label : s.Owner + "/" + s.Repo));
				foreach (var k in keys.Where(k => k != "official" && k != "local" && !list.Any(x => x.Item1 == k)))
					list.Add((k, k));
				if (keys.Contains("local")) list.Add(("local", Lang.T("pets.source.local")));
				return list;
			}

			/// <summary>The filter in use, or "" when its source is gone (removed or turned off).</summary>
			private string ActiveSource => sourceFilter.Length > 0 && form.WebPets.pets.Any(p => SourceKey(p) == sourceFilter)
				&& form.WebPets.pets.Any(p => { string k = SourceKey(p); return k != "official" && k != "local"; }) ? sourceFilter : "";

			private IEnumerable<OptionsPet> FilteredPets()
			{
				string src = ActiveSource;
				var pets = TabPets(CurrentTab).Where(Shown).Where(p => src.Length == 0 || SourceKey(p) == src);
				if (filter.Length == 0) return pets;
				bool Has(string s) => (s ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
				return pets.Where(p =>
					Has(p.folder) || Has(p.author) || Has(p.DisplayName) ||
					// group card: any of its colors, by folder ("pink_sheep") or color name ("rosa")
					(p.Variants != null && p.Variants.Where(v => v != null).Any(v => Has(v.folder))) ||
					(p.VariantNames != null && p.VariantNames.Any(k => Has(Lang.T(k)))));
			}

			private Rectangle GridArea => selected == null
				? new Rectangle(0, 0, Width - ScrollBarWidth, Height)
				: new Rectangle(0, 0, Width - DetailW - 16 - ScrollBarWidth, Height);

			/// <summary>Order of the gallery cards.</summary>
			private enum SortMode { Recent, Name, Author }
			private SortMode sort = Enum.TryParse(Program.MyData.GetSetting("GallerySort"), out SortMode saved) ? saved : SortMode.Recent;
			private Rectangle sortRect;
			private static string SortKey(SortMode m) => m == SortMode.Name ? "pets.sort.name" : m == SortMode.Author ? "pets.sort.author" : "pets.sort.recent";

			/// <summary>The order button: a little menu with the three orders (the one in use checked).</summary>
			private void ShowSortMenu()
			{
				var menu = new ContextMenuStrip();
				foreach (SortMode m in new[] { SortMode.Recent, SortMode.Name, SortMode.Author })
				{
					var mode = m;
					var item = new ToolStripMenuItem(Lang.T(SortKey(m))) { Checked = sort == m };
					item.Click += (s, e) => { sort = mode; Program.MyData.SetSetting("GallerySort", mode.ToString()); ScrollY = 0; Invalidate(); };
					menu.Items.Add(item);
				}
				Theme.ApplyTo(menu);
				menu.Closed += (s, e) => BeginInvoke(new Action(menu.Dispose));
				menu.Show(this, new Point(sortRect.Left, sortRect.Bottom + 2));
			}
			private int hoverChip = -1;
			private const int MaxCardW = 190;

			private IEnumerable<OptionsPet> Sorted(IEnumerable<OptionsPet> pets)
			{
				switch (sort)
				{
					case SortMode.Name: return pets.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase);
					case SortMode.Author: return pets.OrderBy(p => p.author ?? "", StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase);
					default: return pets.OrderByDescending(p => p.lastupdate ?? "").ThenBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase);
				}
			}

			protected override int PaintContent(Graphics g, int scrollY)
			{
				cards.Clear();
				var area = GridArea;
				int y = 4 - scrollY;

				// --- tabs: "Meus pets (n)" | "Baixar pets (n)", underlined like the Windows 11 pivots
				tabRects.Clear();
				int tx = 8;
				var tabs = new[] { (Tab.Mine, "pets.tabMine"), (Tab.Download, "pets.tabDownload") };
				for (int i = 0; i < tabs.Length; i++)
				{
					int count = TabPets(tabs[i].Item1).Count();
					string text = Lang.T(tabs[i].Item2) + (Loading && tabs[i].Item1 == Tab.Download ? "" : "  " + count);
					int tw = Theme.MeasureText(g, text, form.fontBold, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					var tr = new Rectangle(tx, y, tw, 34);
					bool on = CurrentTab == tabs[i].Item1;
					if (hoverTab == i && !on) Theme.FillRounded(g, Theme.Hover, tr, 6);
					Theme.DrawText(g, text, form.fontBold, tr, on ? Theme.Text : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					if (on) Theme.FillRounded(g, Theme.Accent, new Rectangle(tr.X + 12, tr.Bottom - 3, tr.Width - 24, 3), 1.5f);
					// new pets to download: a little round count at the corner of the tab
					int fresh = tabs[i].Item1 == Tab.Download && !Loading ? TabPets(Tab.Download).Count(form.IsNewPet) : 0;
					if (fresh > 0)
					{
						string n = fresh > 99 ? "99+" : fresh.ToString();
						int bw = Math.Max(18, Theme.MeasureText(g, n, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 10);
						var badge = new Rectangle(tr.Right - bw + 6, tr.Y - 2, bw, 18);
						Theme.FillRounded(g, Theme.Accent, badge, 9);
						Theme.DrawText(g, n, form.fontSmall, badge, Theme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					}
					tabRects.Add((tr, tabs[i].Item1));
					tx += tw + 6;
				}
				using (var line = new Pen(Theme.Border)) g.DrawLine(line, 8, y + 34, area.Width - 8, y + 34);

				// --- a row of chips: a label, then the choices (the one in use is highlighted)
				Rectangle Chip(int x, int top, string text, bool on, bool hover)
				{
					int w = Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					var chip = new Rectangle(x, top + 2, w, 24);
					Theme.FillRounded(g, on ? Theme.AccentSoft : hover ? Theme.Hover : Theme.Surface, chip, 12);
					Theme.DrawRounded(g, on ? Theme.Accent : Theme.Border, chip, 12);
					Theme.DrawText(g, text, form.fontSmall, chip, on ? Theme.Accent : Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					return chip;
				}
				int Label(int top, string key)
				{
					Theme.DrawText(g, Lang.T(key), form.fontSmall, new Rectangle(8, top, 160, 28), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					return 8 + Theme.MeasureText(g, Lang.T(key), form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 8;
				}
				Rectangle Button(int x, int top, string text, bool hover)
				{
					int w = Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					var r = new Rectangle(x, top, w, 24);
					Theme.FillRounded(g, hover ? Theme.Hover : Theme.Surface, r, 12);
					Theme.DrawRounded(g, Theme.Border, r, 12);
					Theme.DrawText(g, text, form.fontSmall, r, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					return r;
				}
				int TextW(string text) => Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;

				// --- on the right of the tabs: "↕ Recentes ▾" (the order, a menu), "✏ Criar um pet", "📦 Fontes";
				// a narrow window puts them on a row of their own under the tabs
				string sortText = "↕  " + Lang.T(SortKey(sort)) + "  ▾";
				string edText = "✏  " + Lang.T("editor.chip");
				string srcText = "📦  " + Lang.T("sources.button");
				int buttonsW = TextW(sortText) + 6 + TextW(edText) + 6 + TextW(srcText);
				bool beside = tx + 12 + buttonsW <= area.Width - 8;
				int by = beside ? y + 5 : y + 44;
				int bx = area.Width - 8 - buttonsW;
				sortRect = Button(bx, by, sortText, hoverChip == 0);
				editorRect = Button(sortRect.Right + 6, by, edText, hoverEditor);
				sourcesRect = Button(editorRect.Right + 6, by, srcText, hoverSources);
				y += beside ? 44 : 44 + 34;

				// the labels of the rows are as wide as the widest, so the chips line up
				int labelW = Math.Max(Theme.MeasureText(g, Lang.T("pets.show"), form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width,
					Theme.MeasureText(g, Lang.T("pets.source"), form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width) + 16;
				int cx;
				// filter: "Mostrar: Todos | Originais | Comunidade | Com sons"
				showChips.Clear();
				Label(y, "pets.show");
				cx = labelW;
				var shows = new[] { (ShowMode.All, "pets.filter.all"), (ShowMode.Original, "pets.filter.original"), (ShowMode.Community, "pets.filter.community"), (ShowMode.Sounds, "pets.filter.sounds") };
				// "✨ New (3)": only while there are new pets to download (or it is the filter in use)
				int newCount = TabPets(Tab.Download).Count(form.IsNewPet);
				if (newCount > 0 || show == ShowMode.New) shows = shows.Append((ShowMode.New, "pets.filter.new")).ToArray();
				for (int i = 0; i < shows.Length; i++)
				{
					string chipText = shows[i].Item1 == ShowMode.New ? "✨ " + Lang.T(shows[i].Item2) + "  " + newCount : Lang.T(shows[i].Item2);
					if (cx + TextW(chipText) > area.Width - 8 && cx > labelW) { cx = labelW; y += 30; }   // narrow window: the chips wrap
					var chip = Chip(cx, y, chipText, show == shows[i].Item1, hoverShow == i);
					showChips.Add((chip, shows[i].Item1));
					cx += chip.Width + 6;
				}
				y += 34;

				// source: "Fonte: Todas | Galeria oficial 40 | fulano/pets 12 | Neste computador 1", only once a repository
				// was added besides the official gallery (the pets made on this computer alone do not bring the row)
				sourceChips.Clear();
				var sources = SourceList();
				if (sources.Any(s => s.key != "official" && s.key != "local"))
				{
					Label(y, "pets.source");
					cx = labelW;
					string active = ActiveSource;
					var tabPets = TabPets(CurrentTab).ToList();
					var items = new List<(string key, string text)> { ("", Lang.T("pets.filter.all")) };
					// a source with no pet in this tab is left out (unless it is the one chosen)
					foreach (var s in sources)
					{
						int n = tabPets.Count(p => SourceKey(p) == s.key);
						if (n > 0 || s.key == active) items.Add((s.key, s.text + "  " + n));
					}
					for (int i = 0; i < items.Count; i++)
					{
						int w = Theme.MeasureText(g, items[i].text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
						if (cx + w > area.Width - 8 && cx > labelW) { cx = labelW; y += 30; }   // many sources: the chips wrap
						var chip = Chip(cx, y, items[i].text, active == items[i].key, hoverSourceChip == i);
						sourceChips.Add((chip, items[i].key));
						cx += chip.Width + 6;
					}
					y += 34;
				}
				y += 4;

				// --- cards fill the width: as many columns as fit, cards stretched up to MaxCardW, grid centered
				int usable = area.Width - 16;
				int cols = Math.Max(1, (usable + Gap) / (CardW + Gap));
				int cardW = Math.Min(MaxCardW, (usable - (cols - 1) * Gap) / cols);
				int x0 = 8 + (usable - (cols * cardW + (cols - 1) * Gap)) / 2;

				// --- no internet: say why there is nothing to download, with a way to try again
				retryListRect = Rectangle.Empty;
				if (Offline && CurrentTab == Tab.Download)
				{
					var box = new Rectangle(8, y, area.Width - 16, 44);
					Theme.FillRounded(g, Theme.SurfaceAlt, box, 8);
					string btn = Lang.T("pets.offlineRetry");
					int bw = Theme.MeasureText(g, btn, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					retryListRect = new Rectangle(box.Right - bw - 8, box.Y + 8, bw, 28);
					Theme.DrawText(g, Lang.T("pets.offline"), form.fontSmall, new Rectangle(box.X + 12, box.Y, retryListRect.X - box.X - 20, box.Height), Theme.Text,
						TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
					Theme.DrawButton(g, retryListRect, btn, form.fontSmall, false, hoverRetryList);
					y += box.Height + 12;
				}

				var list = FilteredPets().ToList();
				if (list.Count == 0)
				{
					string msg = Loading ? Lang.T("pets.loading")
						: filter.Length > 0 ? Lang.T("pets.noMatch", filter)
						: CurrentTab == Tab.Download && !Offline ? Lang.T("pets.allDownloaded")
						: CurrentTab == Tab.Download ? ""
						: Lang.T("pets.empty");
					Theme.DrawText(g, msg, form.fontText, new Rectangle(8, y + 32, area.Width - 16, 40), Theme.TextMuted, TextFormatFlags.HorizontalCenter);
				}

				// --- one list: the eSheep (comes with the program) first, then every pet of the gallery
				var ordered = list.Where(p => p.Variants == null && BuiltInPets.IsBuiltIn(p.folder))
					.Concat(Sorted(list.Where(p => p.Variants != null || !BuiltInPets.IsBuiltIn(p.folder))))
					.ToList();
				int col = 0;
				foreach (var pet in ordered)
				{
					var r = new Rectangle(x0 + col * (cardW + Gap), y, cardW, CardH);
					cards.Add((r, pet));
					if (r.Bottom >= 0 && r.Top <= Height) DrawCard(g, r, pet);
					col++;
					if (col >= cols) { col = 0; y += CardH + Gap; }
				}
				if (col != 0) y += CardH + Gap;
				y += 10;
				int contentHeight = y + scrollY + 8;

				if (selected != null) DrawDetail(g);
				return contentHeight;
			}

			private void DrawCard(Graphics g, Rectangle r, OptionsPet pet)
			{
				bool isSel = pet == selected, isHover = pet == hoverPet;
				// a group of colors: two cards stacked behind this one, like a small deck
				if (pet.Variants != null)
					for (int layer = 2; layer >= 1; layer--)
					{
						var back = new Rectangle(r.X + layer * 4, r.Y + layer * 4, r.Width, r.Height);
						Theme.FillRounded(g, Theme.SurfaceAlt, back, 10);
						Theme.DrawRounded(g, Theme.Border, back, 10);
					}
				Theme.FillRounded(g, isHover ? Theme.SurfaceAlt : Theme.Surface, r, 10);
				Theme.DrawRounded(g, isSel ? Theme.Accent : Theme.Border, r, 10, isSel ? 2f : 1f);

				var iconRect = new Rectangle(r.X + (r.Width - 64) / 2, r.Y + 20, 64, 64);
				if (pet.Image != null) Theme.DrawPixelImage(g, pet.Image, iconRect);
				else Theme.FillRounded(g, Theme.Hover, iconRect, 8);

				Theme.DrawText(g, pet.DisplayName, form.fontBold, new Rectangle(r.X + 8, r.Y + 94, r.Width - 16, 20), Theme.Text,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				// a color group says how many colors it has, next to the author ("Oliver B. · 7 cores")
				string byline = pet.Variants != null ? Lang.T(pet.VariantsAreVersions ? "pets.authorVersions" : "pets.authorColors", pet.author, pet.Variants.Length) : pet.author;
				Theme.DrawText(g, byline, form.fontSmall, new Rectangle(r.X + 8, r.Y + 116, r.Width - 16, 18), Theme.TextMuted,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

				// badge: update > in use > new ("downloaded" is said by the tab itself; a pet already on this computer
				// is not "new" in "My pets", even before its card was opened)
				string badgeKey = form.IsOutdated(pet) ? "pets.badgeUpdate"
					: pet.Contains(CurrentPet) ? "pets.badgeInUse"
					: CurrentTab == Tab.Download && form.IsNewPet(pet) ? "pets.badgeNew"
					: null;
				if (badgeKey != null)
				{
					string text = Lang.T(badgeKey);
					int bw = Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 18;
					var badge = new Rectangle(r.Right - bw - 8, r.Y + 8, bw, 20);
					bool muted = badgeKey == "pets.badgeDownloaded";
					Theme.FillRounded(g, muted ? Theme.SurfaceAlt : Theme.AccentSoft, badge, 10);
					Theme.DrawText(g, text, form.fontSmall, badge, muted ? Theme.TextMuted : Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				}
			}

			Image animating;

			/// <summary>The pet moving: the GIF advances on its own and repaints the panel; only the open pet animates.</summary>
			private void DrawPreview(Graphics g, Image gif, Rectangle box)
			{
				if (animating != gif)
				{
					if (animating != null) ImageAnimator.StopAnimate(animating, OnFrame);
					animating = gif;
					if (ImageAnimator.CanAnimate(gif)) ImageAnimator.Animate(gif, OnFrame);
				}
				ImageAnimator.UpdateFrames(gif);
				float s = Math.Min(box.Width / (float)gif.Width, box.Height / (float)gif.Height);
				int w = (int)(gif.Width * s), h = (int)(gif.Height * s);
				var r = new Rectangle(box.X + (box.Width - w) / 2, box.Bottom - h, w, h);
				var mode = g.InterpolationMode;
				g.InterpolationMode = s >= 1 ? System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor : System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
				g.DrawImage(gif, r);
				g.InterpolationMode = mode;
			}

			void OnFrame(object sender, EventArgs e)
			{
				if (IsDisposed || !IsHandleCreated) return;
				try { BeginInvoke(new Action(() => { if (selected?.Preview == animating) Invalidate(); else StopPreview(); })); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			}

			void StopPreview()
			{
				if (animating != null) ImageAnimator.StopAnimate(animating, OnFrame);
				animating = null;
			}

			private void DrawDetail(Graphics g)
			{
				links.Clear();
				var panel = new Rectangle(Width - DetailW - 4, 8, DetailW, Height - 16);
				Theme.FillRounded(g, Theme.Surface, panel, 12);
				Theme.DrawRounded(g, Theme.Border, panel, 12);

				var pet = selected;
				closeRect = new Rectangle(panel.Right - 40, panel.Y + 10, 30, 30);
				if (hoverClose) Theme.FillRounded(g, Theme.Hover, closeRect, 6);
				Theme.DrawText(g, "✕", form.fontText, closeRect, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

				int x = panel.X + 20, w = panel.Width - 40, y = panel.Y + 20;
				// big preview
				var iconBack = new Rectangle(x, y, 96, 96);
				Theme.FillRounded(g, Theme.SurfaceAlt, iconBack, 12);
				if (pet.Preview != null) DrawPreview(g, pet.Preview, Rectangle.Inflate(iconBack, -4, -4));
				else if (pet.Image != null) Theme.DrawPixelImage(g, pet.Image, new Rectangle(x + 8, y + 8, 80, 80));
				// title: the pet, the color shown ("gSheep Roxo") and the character's own name from its file ("· Patsu")
				string title = pet.DisplayName;
				if (pet.Variants != null && pet.SelectedVariant >= 0) title += " " + Lang.T(pet.VariantNames[pet.SelectedVariant]);
				string character = pet.Detail?.DocumentElement?.SelectSingleNode("*[local-name()='header']/*[local-name()='petname']")?.InnerText?.Trim();
				if (!string.IsNullOrEmpty(character) && !title.StartsWith(character, StringComparison.OrdinalIgnoreCase)) title += " · " + character;
				Theme.DrawText(g, title, form.fontH2, new Rectangle(x + 110, y + 10, w - 140, 48), Theme.Text, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, Lang.T("pets.by", pet.author), form.fontSmall, new Rectangle(x + 110, y + 60, w - 110, 18), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				// a pet listed without pets.json has no date (its version is the id of its file on GitHub): nothing to say
				if (!string.IsNullOrEmpty(pet.lastupdate) && char.IsDigit(pet.lastupdate[0]) && pet.lastupdate.Contains('-'))
					Theme.DrawText(g, Lang.T("pets.updated", ShortDate(pet.lastupdate)), form.fontSmall, new Rectangle(x + 110, y + 78, w - 110, 18), Theme.TextMuted, TextFormatFlags.SingleLine);
				// a pet of another repository says where it comes from (a link to that gallery)
				if (pet.Source != null && !pet.Source.IsOfficial)
				{
					var from = new Rectangle(x + 110, y + 96, w - 110, 18);
					Theme.DrawText(g, Lang.T("sources.from", pet.Source.LocalFolder ?? pet.Source.Owner + "/" + pet.Source.Repo), form.fontSmall, from, Theme.Link, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					links.Add((from, pet.Source.WebUrl));
					y += 10;
				}
				y += 112;

				// color picker of a group card
				swatches.Clear();
				if (pet.Variants != null)
				{
					int sel = pet.SelectedVariant;
					string colorName = sel >= 0 ? Lang.T(pet.VariantNames[sel]) : "";
					Theme.DrawText(g, Lang.T(pet.VariantsAreVersions ? "pets.version" : "pets.color", colorName), form.fontBold, new Rectangle(x, y, w, 20), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					y += 26;
					int n = pet.Variants.Length, gap = 6;
					int size = Math.Min(40, (w - gap * (n - 1)) / n);
					for (int i = 0; i < n; i++)
					{
						var v = pet.Variants[i];
						var sr = new Rectangle(x + i * (size + gap), y, size, size);
						bool isSel = i == sel, isHover = i == hoverSwatch;
						Theme.FillRounded(g, isSel ? Theme.AccentSoft : isHover ? Theme.Hover : Theme.SurfaceAlt, sr, 8);
						if (v?.Image != null) Theme.DrawPixelImage(g, v.Image, Rectangle.Inflate(sr, -4, -4));
						// color bar under the icon, so similar colors are easy to tell apart
						Theme.FillRounded(g, v == null ? Theme.Hover : pet.VariantColors[i], new Rectangle(sr.X + 8, sr.Bottom - 6, sr.Width - 16, 3), 1.5f);
						Theme.DrawRounded(g, isSel ? Theme.Accent : isHover ? Theme.Border : Theme.SurfaceAlt, sr, 8, isSel ? 2f : 1f);
						if (v != null) swatches.Add((sr, i));
					}
					y += size + 14;
				}

				adoptRect = new Rectangle(x, y, w, 38);
				cancelRect = removeRect = soundRect = Rectangle.Empty;
				bool inUse = pet.folder == CurrentPet;
				if (DownloadingFolder != null && DownloadingFolder == pet.folder)
				{
					// progress bar + ✕ to stop the download
					adoptRect = new Rectangle(x, y, w - 46, 38);
					cancelRect = new Rectangle(adoptRect.Right + 8, y, 38, 38);
					DrawProgressButton(g, adoptRect, DownloadProgress);
					Theme.DrawButton(g, cancelRect, "✕", form.fontBold, false, hoverCancel);
				}
				else
				{
					string weight = FormatSize(pet.size);
					string action = FailedFolder == pet.folder ? Lang.T("pets.retry")
						: form.IsOutdated(pet) ? (weight.Length > 0 ? Lang.T("pets.downloadUpdateSize", weight) : Lang.T("pets.downloadUpdate"))
						: inUse ? Lang.T("pets.adoptAnother")
						: !form.IsDownloaded(pet) ? (weight.Length > 0 ? Lang.T("pets.downloadSize", weight) : Lang.T("pets.download"))
						// a color group says which color will be adopted ("Adotar a gSheep Roxo")
						: pet.Variants != null && pet.SelectedVariant >= 0 ? Lang.T("pets.adoptNamed", pet.DisplayName + " " + Lang.T(pet.VariantNames[pet.SelectedVariant]))
						: Lang.T("pets.adopt");
					Theme.DrawButton(g, adoptRect, action, form.fontBold, true, hoverAdopt);
				}
				y += 46;
				// a downloaded pet (not the one in use) can leave the computer; it can be downloaded again
				if (DownloadingFolder != pet.folder && !inUse && !BuiltInPets.IsBuiltIn(pet.folder) && PetStore.Has(pet.folder))
				{
					string text = Lang.T("pets.removeLink");
					var ts = Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine);
					removeRect = new Rectangle(x + (w - ts.Width - 16) / 2, y - 4, ts.Width + 16, 22);
					if (hoverRemove) Theme.FillRounded(g, Theme.Hover, removeRect, 6);
					Theme.DrawText(g, text, form.fontSmall, removeRect, hoverRemove ? Theme.Danger : Theme.TextMuted,
						TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					y += 24;
				}
				else y += 6;

				// stats: from the file when it is here (scenes and entrances, as the animation picker shows them),
				// otherwise from pets.json
				int soundCount = pet.Detail != null ? CountOf(pet.Detail, "sounds") : pet.sounds;
				var stats = pet.Detail != null ? ReadStats(pet.Detail, pet.folder)
					: pet.animations > 0 ? new List<(string value, string label)>
					{
						(pet.animations.ToString(), Lang.T("pets.statAnimations")),
						(pet.sounds.ToString(), Lang.T("pets.statSounds")),
						("1", Lang.T("pets.statSkins")),
					}
					: null;
				if (stats != null)
				{
					// color group: the third box counts the colors to choose from, not the skins of one file
					if (pet.Variants != null && stats.Count >= 3)
						stats[2] = (pet.Variants.Length.ToString(), Lang.T(pet.VariantsAreVersions ? "pets.statVersions" : "pets.statColors"));
					int sw = (w - 16) / 3;
					for (int i = 0; i < stats.Count && i < 3; i++)
					{
						var sr = new Rectangle(x + i * (sw + 8), y, sw, 54);
						Theme.FillRounded(g, Theme.SurfaceAlt, sr, 8);
						Theme.DrawText(g, stats[i].value, form.fontBold, new Rectangle(sr.X, sr.Y + 8, sr.Width, 20), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
						Theme.DrawText(g, stats[i].label, form.fontSmall, new Rectangle(sr.X, sr.Y + 28, sr.Width, 18), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
					}
					y += 66;
					// sounds of this pet on/off (only when it has sounds and is on this computer)
					soundRect = playRect = Rectangle.Empty;
					if (form.IsDownloaded(pet) && soundCount > 0)
					{
						bool on = !Program.MyData.IsPetMuted(pet.folder);
						// ▶ plays one of the pet's sounds; the rest of the row turns the pet's sounds on or off
						playRect = new Rectangle(x, y, 30, 30);
						if (hoverPlay) Theme.FillRounded(g, Theme.AccentSoft, playRect, 6);
						Theme.DrawText(g, "▶", form.fontText, playRect, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
						var row = new Rectangle(x + 36, y, w - 36, 30);
						soundRect = row;
						if (hoverSound) Theme.FillRounded(g, Theme.Hover, row, 6);
						Theme.DrawText(g, Lang.T("pets.sounds"), form.fontText, new Rectangle(row.X + 8, row.Y, row.Width - 70, row.Height), Theme.Text,
							TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
						Theme.DrawToggle(g, new Rectangle(row.Right - 52, row.Y + 5, 44, 20), on);
						y += 38;
					}
					else y += 2;
				}
				else if (form.IsDownloaded(pet))   // the file is here: its counts are on the way
				{
					Theme.DrawText(g, Lang.T("pets.loadingDetails"), form.fontSmall, new Rectangle(x, y, w, 18), Theme.TextMuted);
					y += 28;
				}

				// readme (simple markdown) in its own scrolling area: it never leaves the panel.
				// TextRenderer ignores the Graphics clip unless PreserveGraphicsClipping is set.
				readmeArea = new Rectangle(panel.X + 1, y, panel.Width - 2, Math.Max(0, panel.Bottom - y - 10));
				const TextFormatFlags Keep = TextFormatFlags.PreserveGraphicsClipping;
				var clip = g.Clip;
				g.SetClip(readmeArea);
				int top = y;
				y -= detailScroll;
				foreach (var block in ReadmeBlocks(pet))
				{
					if (block.heading)
					{
						y += 6;
						if (y + 24 > readmeArea.Top && y < readmeArea.Bottom)
							Theme.DrawText(g, block.text, form.fontBold, new Rectangle(x, y, w, 20), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | Keep);
						y += 24;
						continue;
					}
					int indent = block.bullet ? 14 : 0;
					var size = Theme.MeasureText(g, block.text, form.fontSmall, w - indent);
					var tr = new Rectangle(x + indent, y, w - indent, size.Height);
					if (tr.Bottom > readmeArea.Top && tr.Top < readmeArea.Bottom)
					{
						if (block.bullet) using (var b = new SolidBrush(Theme.TextMuted)) g.FillEllipse(b, x + 2, y + 6, 5, 5);
						bool isLink = block.url.StartsWith("https://");
						Theme.DrawText(g, block.text, form.fontSmall, tr, isLink ? Theme.Link : Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | Keep);
						var visible = Rectangle.Intersect(tr, readmeArea);
						if (isLink && !visible.IsEmpty) links.Add((visible, block.url));
					}
					y += size.Height + 8;
				}
				g.Clip = clip;
				int total = y + detailScroll - top;
				detailMax = Math.Max(0, total - readmeArea.Height);
				if (detailScroll > detailMax) { detailScroll = detailMax; Invalidate(); }
				if (detailMax > 0)   // small scroll thumb on the right edge of the panel
				{
					// soft fade where the text goes on below (instead of a line cut in half)
					if (detailScroll < detailMax)
					{
						var fade = new Rectangle(readmeArea.X, readmeArea.Bottom - 28, readmeArea.Width - 12, 28);
						using (var fb = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(fade.X, fade.Y - 1, fade.Width, fade.Height + 2),
							Color.FromArgb(0, Theme.Surface), Theme.Surface, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
							g.FillRectangle(fb, fade);
					}
					int th = Math.Max(24, readmeArea.Height * readmeArea.Height / Math.Max(1, total));
					int ty = readmeArea.Top + (int)((readmeArea.Height - th) * (detailScroll / (double)detailMax));
					Theme.FillRounded(g, Theme.Border, new Rectangle(panel.Right - 9, ty, 4, th), 2);
				}
			}

			/// <summary>
			/// README as blocks: headings, bullets and paragraphs. Lines of one paragraph are joined
			/// (markdown wraps long sentences over several lines); a blank line ends a paragraph.
			/// </summary>
			private static List<(bool heading, bool bullet, string text, string url)> ReadmeBlocks(OptionsPet pet)
			{
				var blocks = new List<(bool, bool, string, string)>();
				var cur = new System.Text.StringBuilder();
				bool curBullet = false;
				void Flush()
				{
					if (cur.Length == 0) return;
					string text = cur.ToString().Trim();
					// links: keep the label, remember the first url of the block
					var m = Regex.Match(text, @"\[(.*?)\]\((.*?)\)");
					string url = m.Success ? m.Groups[2].Value : Regex.Match(text, @"https?://\S+").Value;
					text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "$1");
					text = Regex.Replace(text, @"\*\*(.*?)\*\*|\*(.*?)\*|__(.*?)__", "$1$2$3").Replace("\\_", "_");
					text = Regex.Replace(text, @"<[^>]+>", "").Replace("`", "");
					blocks.Add((false, curBullet, text, url ?? ""));
					cur.Clear();
					curBullet = false;
				}
				foreach (var raw in pet.Readme ?? new List<string>())
				{
					string line = (raw ?? "").TrimEnd();
					string trimmed = line.Trim();
					if (trimmed.Length == 0 || Regex.IsMatch(trimmed, @"^(-{3,}|\*{3,}|_{3,})$")) { Flush(); continue; }
					if (trimmed.StartsWith("#"))
					{
						Flush();
						string t = trimmed.TrimStart('#').Trim();
						if (t.Equals(pet.DisplayName, StringComparison.OrdinalIgnoreCase) || t.Equals(pet.folder, StringComparison.OrdinalIgnoreCase)) continue;
						blocks.Add((true, false, t, ""));
						continue;
					}
					if (Regex.IsMatch(line, @"^\s*([*-]|\d+\.)\s+"))
					{
						Flush();
						curBullet = true;
						cur.Append(Regex.Replace(line, @"^\s*([*-]|\d+\.)\s+", ""));
						continue;
					}
					if (cur.Length > 0) cur.Append(' ');
					cur.Append(trimmed);
				}
				Flush();
				return blocks;
			}

			/// <summary>File size for the download button: "850 KB", "1,4 MB" (empty when unknown).</summary>
			public static string FormatSize(long bytes)
			{
				if (bytes <= 0) return "";
				if (bytes < 1024 * 1024) return Math.Max(1, (int)Math.Round(bytes / 1024.0)) + " KB";
				return (bytes / (1024.0 * 1024.0)).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " MB";
			}

			/// <summary>The download button while the file arrives: it fills from left to right.</summary>
			private void DrawProgressButton(Graphics g, Rectangle r, double progress)
			{
				progress = Math.Max(0, Math.Min(1, progress));
				string text = Lang.T("pets.downloadingPct", (int)Math.Floor(progress * 100));
				const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
					| TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping;
				var old = g.Clip;
				Theme.FillRounded(g, Theme.AccentSoft, r, 6);
				Theme.DrawRounded(g, Theme.Accent, r, 6);
				var done = new Rectangle(r.X, r.Y, (int)Math.Round(r.Width * progress), r.Height);
				// empty part: accent text on the soft track
				g.SetClip(new Rectangle(done.Right, r.Y, r.Right - done.Right, r.Height), System.Drawing.Drawing2D.CombineMode.Intersect);
				TextRenderer.DrawText(g, text, form.fontBold, r, Theme.Accent, flags);
				g.Clip = old;
				// filled part: the normal button colors
				if (done.Width > 0)
				{
					g.SetClip(done, System.Drawing.Drawing2D.CombineMode.Intersect);
					Theme.FillRounded(g, Theme.Accent, r, 6);
					TextRenderer.DrawText(g, text, form.fontBold, r, Theme.AccentText, flags);
					g.Clip = old;
				}
			}

			static int CountOf(XmlDocument doc, string name) =>
				doc?.DocumentElement?.SelectSingleNode("*[local-name()='" + name + "']")?.ChildNodes.OfType<XmlElement>().Count() ?? 0;

			/// <summary>Scenes and entrances of each pet file (worked out once: the file is big).</summary>
			static readonly Dictionary<string, (int scenes, int entrances)> sceneCounts = new Dictionary<string, (int, int)>();

			/// <summary>
			/// The numbers of the detail: the scenes you can start (what the animation picker offers, not the many
			/// continuations and reactions), the ways the pet comes onto the screen, and its sounds (or skins).
			/// </summary>
			internal static List<(string value, string label)> ReadStats(XmlDocument doc, string folder)
			{
				var res = new List<(string, string)>();
				if (!sceneCounts.TryGetValue(folder ?? "", out var counts))
				{
					counts = (CountOf(doc, "animations"), 0);
					// "new Animations" makes its file the one every value is computed with (Animations.Xml is shared):
					// the pet on the screen must get its own back, or its "imageW", "imageH"… become this file's (0 here)
					var running = Animations.Xml;
					try
					{
						var xml = new Xml(1) { AnimationXML = XmlData.AnimationXML.ParseXML(doc.OuterXml) };
						var anims = new Animations(xml);
						xml.LoadAnimations(anims);
						var hidden = FormAnimPicker.HiddenAnimations(anims);
						var firsts = anims.SheepSpawn.Values.Select(sp => sp.Next).Where(anims.SheepAnimations.ContainsKey).Distinct().ToList();
						// an entrance that only its spawn starts is listed with the entrances, not with the scenes
						var targets = anims.SheepAnimations.Values.SelectMany(a => (a.EndAnimation ?? new List<TNextAnimation>())
							.Concat(a.EndBorder ?? new List<TNextAnimation>()).Concat(a.EndGravity ?? new List<TNextAnimation>())
							.Where(n => n.ID != a.ID).Select(n => n.ID)).ToHashSet();
						int scenes = anims.SheepAnimations.Keys.Count(id => !hidden.Contains(id) && !(firsts.Contains(id) && !targets.Contains(id)));
						counts = (scenes, firsts.Count);
					}
					catch (Exception skipped) { LogFile.Ignored(skipped); }
					finally { Animations.Xml = running; }
					sceneCounts[folder ?? ""] = counts;
				}
				res.Add((counts.scenes.ToString(), Lang.T(counts.entrances > 0 ? "pets.statScenes" : "pets.statAnimations")));
				if (counts.entrances > 0) res.Add((counts.entrances.ToString(), Lang.T("pets.statEntrances")));
				else res.Add((CountOf(doc, "sounds").ToString(), Lang.T("pets.statSounds")));
				var skins = doc.DocumentElement?.SelectSingleNode("*[local-name()='image']/*[local-name()='skins']")?.InnerText;
				res.Add(counts.entrances > 0 ? (CountOf(doc, "sounds").ToString(), Lang.T("pets.statSounds"))
					: (string.IsNullOrEmpty(skins) ? "1" : skins, Lang.T("pets.statSkins")));
				return res;
			}

			/// <summary>"2026-11-18 19:00" → "18/11/2026" in Portuguese (the date as the language in use writes it).</summary>
			static string ShortDate(string date)
			{
				if (string.IsNullOrWhiteSpace(date)) return date;
				var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd" };
				if (!DateTime.TryParseExact(date.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
					return date;
				System.Globalization.CultureInfo culture;
				try { culture = System.Globalization.CultureInfo.GetCultureInfo(Lang.Current); }
				catch (System.Globalization.CultureNotFoundException) { culture = System.Globalization.CultureInfo.CurrentCulture; }
				return d.ToString("d", culture);
			}

			private OptionsPet PetAt(Point p)
			{
				foreach (var c in cards) if (c.rect.Contains(p) && GridArea.Contains(p)) return c.pet;
				return null;
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var hp = PetAt(e.Location);
				bool ha = selected != null && adoptRect.Contains(e.Location);
				bool hc = selected != null && closeRect.Contains(e.Location);
				bool hx = selected != null && cancelRect.Contains(e.Location);
				bool hr = selected != null && removeRect.Contains(e.Location);
				bool hrl = Offline && retryListRect.Contains(e.Location);
				bool hsn = selected != null && soundRect.Contains(e.Location);
				bool hl = selected != null && links.Any(l => l.rect.Contains(e.Location));
				int hs = selected == null ? -1 : swatches.Where(s => s.rect.Contains(e.Location)).Select(s => s.index).DefaultIfEmpty(-1).First();
				int hch = sortRect.Contains(e.Location) ? 0 : -1;
				int htb = tabRects.FindIndex(t => t.rect.Contains(e.Location));
				int hsh = showChips.FindIndex(c => c.rect.Contains(e.Location));
				int hsc = sourceChips.FindIndex(c => c.rect.Contains(e.Location));
				bool hsrc = sourcesRect.Contains(e.Location);
				bool hed = editorRect.Contains(e.Location);
				bool hpl = selected != null && playRect.Contains(e.Location);
				Cursor = (hp != null || ha || hc || hx || hr || hrl || hsn || hl || hs >= 0 || hch >= 0 || htb >= 0 || hsh >= 0 || hsc >= 0 || hsrc || hed || hpl) ? Cursors.Hand : Cursors.Default;
				if (hp != hoverPet || ha != hoverAdopt || hc != hoverClose || hs != hoverSwatch || hch != hoverChip
					|| hx != hoverCancel || hr != hoverRemove || hrl != hoverRetryList || hsn != hoverSound || htb != hoverTab
					|| hsh != hoverShow || hsc != hoverSourceChip || hsrc != hoverSources || hed != hoverEditor || hpl != hoverPlay)
				{
					if (hp != hoverPet) cardTip.SetToolTip(this, hp == null ? null
						: hp.DisplayName + "  ·  " + (hp.Variants != null ? Lang.T(hp.VariantsAreVersions ? "pets.authorVersions" : "pets.authorColors", hp.author, hp.Variants.Length) : hp.author)
						+ (hp.Source != null && !hp.Source.IsOfficial ? "  ·  " + Lang.T("sources.from", hp.Source.LocalFolder ?? hp.Source.Owner + "/" + hp.Source.Repo) : ""));
					hoverPet = hp; hoverAdopt = ha; hoverClose = hc; hoverSwatch = hs; hoverChip = hch;
					hoverCancel = hx; hoverRemove = hr; hoverRetryList = hrl; hoverSound = hsn; hoverTab = htb;
					hoverShow = hsh; hoverSourceChip = hsc; hoverSources = hsrc; hoverEditor = hed; hoverPlay = hpl;
					Invalidate();
				}
				if (hsrc) form.StatusText = Lang.T("opt.sources.desc");
				if (hed) form.StatusText = Lang.T(PetEditorApp.IsInstalled ? "editor.chipHint" : "editor.chipHintDownload");
				if (hpl) form.StatusText = Lang.T("pets.playSound");
				if (hs >= 0 && selected?.VariantNames != null)
					form.StatusText = Lang.T(selected.VariantsAreVersions ? "pets.version" : "pets.color", Lang.T(selected.VariantNames[hs]));
				if (hl) form.StatusText = links.First(l => l.rect.Contains(e.Location)).url;
			}

			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);
				hoverPet = null; hoverAdopt = hoverClose = hoverCancel = hoverRemove = hoverRetryList = hoverSound = hoverSources = hoverEditor = hoverPlay = false;
				hoverSwatch = -1; hoverChip = -1; hoverTab = -1; hoverShow = -1; hoverSourceChip = -1;
				Invalidate();
			}

			protected override async void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				if (sortRect.Contains(e.Location)) { ShowSortMenu(); return; }

				foreach (var c in showChips)
				{
					if (!c.rect.Contains(e.Location)) continue;
					if (show != c.mode) { show = c.mode; ScrollY = 0; }
					Invalidate();
					return;
				}
				foreach (var c in sourceChips)
				{
					if (!c.rect.Contains(e.Location)) continue;
					if (sourceFilter != c.key) { sourceFilter = c.key; Program.MyData.SetSetting("GallerySourceFilter", c.key); ScrollY = 0; }
					Invalidate();
					return;
				}
				if (sourcesRect.Contains(e.Location)) { form.OpenSources(); return; }
				if (editorRect.Contains(e.Location)) { form.OpenPetEditor(); return; }
				if (selected != null && playRect.Contains(e.Location)) { PlayPreview(selected); return; }
				foreach (var t in tabRects)
				{
					if (!t.rect.Contains(e.Location)) continue;
					if (CurrentTab != t.tab) { CurrentTab = t.tab; ScrollY = 0; CloseDetail(); }
					return;
				}
				if (Offline && retryListRect.Contains(e.Location)) { form.ReloadPets(); return; }
				if (selected != null)
				{
					if (closeRect.Contains(e.Location)) { CloseDetail(); return; }
					if (cancelRect.Contains(e.Location)) { form.CancelDownload(); return; }
					if (removeRect.Contains(e.Location)) { form.RemovePet(selected); return; }
					if (soundRect.Contains(e.Location))
					{
						bool muted = !Program.MyData.IsPetMuted(selected.folder);
						Program.MyData.SetPetMuted(selected.folder, muted);
						form.StatusText = Lang.T(muted ? "status.soundOff" : "status.soundOn", selected.DisplayName);
						Invalidate();
						return;
					}
					if (adoptRect.Contains(e.Location))
					{
						if (DownloadingFolder == null) form.PrimaryAction(selected);   // the progress bar is not a button
						return;
					}
					foreach (var l in links) if (l.rect.Contains(e.Location)) { Program.OpenLink(l.url); return; }
					foreach (var s in swatches)
					{
						if (!s.rect.Contains(e.Location)) continue;
						var card = selected;
						card.SelectVariant(card.Variants[s.index]);
						Invalidate();
						await form.LoadDetailsAsync(card);
						if (selected == card) Invalidate();
						return;
					}
				}
				var pet = PetAt(e.Location);
				if (pet == null) return;
				if (selected != pet) detailScroll = 0;
				selected = pet;
				form.MarkSeen(pet);
				Invalidate();
				await form.LoadDetailsAsync(pet);
				if (selected == pet) Invalidate();
			}

			protected override void OnMouseDoubleClick(MouseEventArgs e)
			{
				base.OnMouseDoubleClick(e);
				var pet = PetAt(e.Location);
				if (pet != null) form.PrimaryAction(pet);
			}
		}

	}
}
