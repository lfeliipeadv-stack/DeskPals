using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPet
{
	public sealed partial class FormSettings
	{
		// ================================================================== Home page

		/// <summary>
		/// The start page: the pet in use, the numbers of today (focus, breaks, reminders, mood), what comes next
		/// (reminders, meeting, weather) and the state of the program. Everything is read on this computer.
		/// </summary>
		private sealed class HomeView : ScrollSurface
		{
			private readonly FormSettings form;
			private readonly List<(Rectangle rect, Action run, string id)> buttons = new List<(Rectangle, Action, string)>();
			private string hover;
			/// <summary>What the week chart shows: focus, breaks, keys or time at the computer.</summary>
			private string weekMetric = "focus";
			private Font bigFont, emojiFont, heroFont;

			private readonly Timer refresh = new Timer { Interval = 2000 };

			public HomeView(FormSettings f)
			{
				form = f;
				// the numbers change while the page is open (keys, CPU, the timer)
				refresh.Tick += (s, e) => { if (Visible && !IsDisposed) Invalidate(); };
				refresh.Start();
			}

			protected override void Dispose(bool disposing) { if (disposing) { refresh.Stop(); refresh.Dispose(); } base.Dispose(disposing); }

			/// <summary>One tile of the numbers of today.</summary>
			private sealed class Stat
			{
				public string Emoji, Title, Value, Foot;
				public Page? Opens;
				/// <summary>What a click does when it is not a page (the mood question).</summary>
				public Action Run;
			}

			/// <summary>The numbers of today: the four of every day, then the ones of the plugins that are on.</summary>
			private List<Stat> Stats()
			{
				var list = new List<Stat>
				{
					new Stat { Emoji = "⏱️", Title = Lang.T("home.focus"), Value = Pomodoro.Minutes(DayStats.Get("focusMin")), Foot = Lang.T("home.focusFoot"), Opens = Page.Reminders },
					new Stat { Emoji = "🚶", Title = Lang.T("home.breaks"), Value = DayStats.Get("breaks").ToString(), Foot = Lang.T("home.breaksFoot"), Opens = Page.Reminders },
					new Stat { Emoji = "⏰", Title = Lang.T("home.reminders"), Value = TodayReminders().Count.ToString(), Foot = Lang.T("home.remindersFoot"), Opens = Page.Reminders },
					new Stat { Emoji = "😊", Title = Lang.T("home.mood"), Value = MoodToday(), Foot = Lang.T("home.moodFoot"), Run = DesktopPet.Mood.Ask },
				};
				if (TypingPet.Enabled)
					list.Add(new Stat { Emoji = "⌨️", Title = Lang.T("home.keys"), Value = TypingPet.Today.ToString("N0"), Foot = Lang.T("home.keysFoot"), Opens = Page.Plugins });
				if (AppTime.Enabled)
				{
					var top = AppTime.Today(1);
					list.Add(new Stat { Emoji = "🕒", Title = Lang.T("home.topApp"), Value = top.Count == 0 ? "—" : top[0].name, Foot = top.Count == 0 ? Lang.T("apptime.none") : Pomodoro.Minutes(top[0].minutes), Opens = Page.Plugins });
				}
				if (PcMonitor.Enabled || MemoryCare.Enabled)
					list.Add(new Stat { Emoji = "💻", Title = Lang.T("home.pc"), Value = $"{PcMonitor.Cpu}% · {MemoryCare.Status().load}%", Foot = Lang.T("home.pcFoot"), Opens = Page.Plugins });
				if (SimpleTimer.Active)
					list.Add(new Stat { Emoji = "⏳", Title = Lang.T("timer.title"), Value = SimpleTimer.Finished ? Lang.T("timer.doneShort") : SimpleTimer.LeftText, Foot = SimpleTimer.Paused ? Lang.T("pomo.paused") : Lang.T("home.timerFoot") });
				return list;
			}

			/// <summary>Quick actions: focus, timer, the summary of the day, the chat.</summary>
			private List<(string text, string id, Action run)> Actions()
			{
				var list = new List<(string, string, Action)>();
				if (Pomodoro.Current == Pomodoro.Phase.Off) list.Add(("🍅 " + Lang.T("home.startFocus", Pomodoro.Minutes(Pomodoro.FocusMinutes)), "focus", Pomodoro.StartFocus));
				else list.Add(("🍅 " + Lang.T("home.stopFocus"), "focus", Pomodoro.Stop));
				if (PluginSwitch.On("timer")) list.Add(("⏳ " + Lang.T("home.timer"), "timer", SimpleTimer.AskAndStart));
				if (DoNotDisturb.Manual) list.Add(("🔔 " + Lang.T("dnd.stop"), "dnd", DoNotDisturb.Stop));
				else list.Add(("🔕 " + Lang.T("home.dndHour"), "dnd", () => DoNotDisturb.Start(60)));
				list.Add(("🌙 " + Lang.T("menu.summary"), "summary", () => DaySummary.Tell()));
				list.Add(("💬 " + Lang.T("home.chat"), "chat", FormAsk.Open));
				return list;
			}

			protected override int PaintContent(Graphics g, int scrollY)
			{
				bigFont = bigFont ?? Theme.CreateFont(26, FontStyle.Bold);
				heroFont = heroFont ?? Theme.CreateFont(30, FontStyle.Bold);
				emojiFont = emojiFont ?? Theme.CreateFont(20);
				buttons.Clear();
				chartHours = null;   // set again when the forecast chart is drawn
				int y = 6 - scrollY;
				int w = Width - ScrollBarWidth - 16;
				const int Gap = 14;

				// --- hero: the pet in use
				var hero = new Rectangle(8, y, w, 170);
				using (var b = new LinearGradientBrush(hero, Theme.Accent, ControlPaint.Dark(Theme.Accent, 0.15f), LinearGradientMode.Horizontal))
				using (var path = Theme.RoundedRect(hero, 16))
					g.FillPath(b, path);
				Theme.DrawText(g, Lang.T("home.yourPet").ToUpperInvariant(), form.fontSmall, new Rectangle(hero.X + 26, hero.Y + 20, hero.Width - 220, 18), Color.FromArgb(210, Theme.AccentText), TextFormatFlags.SingleLine);
				string name = Program.Mainthread?.CurrentHeader?.Petname ?? "Pet";
				Theme.DrawText(g, name, heroFont, new Rectangle(hero.X + 24, hero.Y + 40, hero.Width - 220, 44), Theme.AccentText, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, PetLine(), form.fontText, new Rectangle(hero.X + 26, hero.Y + 86, hero.Width - 240, 22), Color.FromArgb(230, Theme.AccentText), TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				int bx = hero.X + 26;
				bx = HeroButton(g, bx, hero.Y + 120, Lang.T("home.changePet"), "change", () => form.SelectPage(Page.Pets));
				bx = HeroButton(g, bx + 8, hero.Y + 120, "💬 " + Lang.T("home.chat"), "ask", FormAsk.Open);
				bx = HeroButton(g, bx + 8, hero.Y + 120, "＋ " + Lang.T("home.addPet"), "add", () => Program.Mainthread.AddSheep());
				var img = form.CurrentPetImage();
				if (img != null)
				{
					var dest = Fit(img.Size, new Rectangle(hero.Right - 170, hero.Y + 20, 140, 130));
					Theme.DrawPixelImage(g, img, dest);
				}
				y += hero.Height + Gap;

				// --- a new version of the program (found by the automatic check): install it from here
				if (Updater.Staged is Updater.Release ready)
					y = PaintUpdateBanner(g, y, w, ready) + Gap;
				else if (Updater.Available is Updater.Release news && Program.MyData.GetSetting("UpdateBannerHidden") != news.Version.ToString())
					y = PaintUpdateBanner(g, y, w, news) + Gap;

				// --- the numbers of today (more tiles with the plugins that are on)
				var stats = Stats();
				int cols = w >= 760 ? 4 : 2;
				int sw = (w - (cols - 1) * Gap) / cols;
				for (int i = 0; i < stats.Count; i++)
				{
					var st = stats[i];
					var r = new Rectangle(8 + (i % cols) * (sw + Gap), y + (i / cols) * (112 + Gap), sw, 112);
					string id = "stat" + i;
					// a click opens its page (focus and breaks: the focus timer is set up with the reminders) or asks the mood
					Action open = st.Run ?? (st.Opens is Page opens ? () => form.SelectPage(opens) : (Action)null);
					bool over = open != null && hover == id;
					Theme.FillRounded(g, over ? Theme.Hover : Theme.Surface, r, 12);
					Theme.DrawRounded(g, over ? Theme.Accent : Theme.Border, r, 12);
					var ic = new Rectangle(r.X + 16, r.Y + 14, 34, 34);
					Theme.FillRounded(g, Theme.AccentSoft, ic, 9);
					Theme.DrawText(g, st.Emoji, emojiFont, ic, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					Theme.DrawText(g, st.Title.ToUpperInvariant(), form.fontSmall, new Rectangle(ic.Right + 10, r.Y + 14, r.Width - 70, 34), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					Theme.DrawText(g, st.Value, bigFont, new Rectangle(r.X + 16, r.Y + 50, r.Width - 32, 34), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					Theme.DrawText(g, st.Foot, form.fontSmall, new Rectangle(r.X + 16, r.Y + 84, r.Width - 32, 18), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					if (open != null) buttons.Add((r, open, id));
				}
				y += ((stats.Count + cols - 1) / cols) * (112 + Gap);

				// --- quick actions
				var actions = Actions();
				int ax = 8;
				int ay = y;
				foreach (var (text, aid, run) in actions)
				{
					int aw = Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 32;
					if (ax + aw > 8 + w) { ax = 8; ay += 42; }
					var r = new Rectangle(ax, ay, aw, 34);
					Theme.DrawButton(g, r, text, form.fontSmall, aid == "focus" && Pomodoro.Current == Pomodoro.Phase.Off, hover == aid);
					buttons.Add((r, run, aid));
					ax = r.Right + 10;
				}
				y = ay + 34 + Gap + 4;

				// --- the weather forecast: now, the hours (temperature, rain, wind) and the next days
				if (Weather.Enabled) y = PaintWeather(g, y, w) + Gap;

				// --- the week: one bar per day (focus, breaks, keys, time at the computer)
				y = PaintWeek(g, y, w) + Gap;

				// --- the programs of today, as bars (with "Time in each program" on)
				if (AppTime.Enabled)
				{
					var apps = AppTime.Today(5);
					int ph = 60 + Math.Max(1, apps.Count) * 34;
					var panel = new Rectangle(8, y, w, ph);
					Panel(g, panel, "🕒  " + Lang.T("home.apps"));
					if (apps.Count == 0)
						Theme.DrawText(g, Lang.T("apptime.none"), form.fontText, new Rectangle(panel.X + 20, panel.Y + 52, panel.Width - 40, 24), Theme.TextMuted, TextFormatFlags.SingleLine);
					int most = Math.Max(1, apps.Count == 0 ? 1 : apps.Max(a => a.minutes));
					int nameW = Math.Min(220, panel.Width / 3), timeW = 90, barX = panel.X + 20 + nameW + 10, barMax = panel.Right - 20 - timeW - barX;
					for (int i = 0; i < apps.Count; i++)
					{
						int ry = panel.Y + 50 + i * 34;
						Theme.DrawText(g, apps[i].name, form.fontText, new Rectangle(panel.X + 20, ry, nameW, 26), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
						var track = new Rectangle(barX, ry + 8, Math.Max(10, barMax), 10);
						Theme.FillRounded(g, Theme.SurfaceAlt, track, 5);
						int bw = Math.Max(10, (int)(track.Width * apps[i].minutes / (double)most));
						Theme.FillRounded(g, i == 0 ? Theme.Accent : Theme.AccentSoft, new Rectangle(track.X, track.Y, bw, track.Height), 5);
						Theme.DrawText(g, Pomodoro.Minutes(apps[i].minutes), form.fontSmall, new Rectangle(panel.Right - 20 - timeW, ry, timeW, 26), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine);
					}
					y += ph + Gap;
				}

				// --- next (reminders, meeting, weather) | the program
				bool two = w >= 760;
				int lw = two ? (w - Gap) * 3 / 5 : w, rw = two ? w - lw - Gap : w;
				var next = NextItems();
				int lh = 64 + Math.Max(1, next.Count) * 40;
				var left = new Rectangle(8, y, lw, lh);
				Panel(g, left, "🗓️  " + Lang.T("home.next"));
				if (next.Count == 0)
					Theme.DrawText(g, Lang.T("home.nothing"), form.fontText, new Rectangle(left.X + 20, left.Y + 56, left.Width - 40, 24), Theme.TextMuted, TextFormatFlags.SingleLine);
				for (int i = 0; i < next.Count; i++)
				{
					var row = new Rectangle(left.X + 16, left.Y + 52 + i * 40, left.Width - 32, 34);
					Theme.FillRounded(g, Theme.SurfaceAlt, row, 8);
					Theme.DrawText(g, next[i].when, form.fontBold, new Rectangle(row.X + 12, row.Y, 110, row.Height), Theme.Accent, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					Theme.DrawText(g, next[i].what, form.fontText, new Rectangle(row.X + 126, row.Y, row.Width - 136, row.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				}

				var right = two ? new Rectangle(left.Right + Gap, y, rw, Math.Max(lh, 236)) : new Rectangle(8, left.Bottom + Gap, rw, 236);
				Panel(g, right, "🩺  " + Lang.T("home.system"));
				int on = form.pluginsView?.TestCards.Count(c => c.Get == null || c.Get()) ?? 0, total = form.pluginsView?.TestCards.Count ?? 0;
				var lines = new[]
				{
					(Lang.T("home.pets"), Lang.T("home.petsValue", PetStore.Folders().Count + BuiltInPets.Folders.Length)),
					(Lang.T("home.plugins"), Lang.T("home.pluginsValue", on, total)),
					(Lang.T("home.updates"), PetUpdates.Outdated.Count > 0 ? Lang.T("home.updatesPets", PetUpdates.Outdated.Count) : Lang.T("home.updatesNone")),
					(Lang.T("home.version"), Updater.Available != null ? Lang.T("home.versionNew", Updater.CurrentVersion.ToString(3), Updater.Available.Version.ToString(3)) : Updater.CurrentVersion.ToString(3)),
				};
				for (int i = 0; i < lines.Length; i++)
				{
					int ly = right.Y + 52 + i * 34;
					Theme.DrawText(g, lines[i].Item1, form.fontText, new Rectangle(right.X + 20, ly, right.Width / 2, 28), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					Theme.DrawText(g, lines[i].Item2, form.fontSmall, new Rectangle(right.X + right.Width / 2, ly, right.Width / 2 - 20, 28), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				}
				var manage = new Rectangle(right.X + 20, right.Bottom - 48, right.Width - 40, 32);
				Theme.DrawButton(g, manage, Lang.T("home.managePlugins"), form.fontSmall, false, hover == "plugins");
				buttons.Add((manage, () => form.SelectPage(Page.Plugins), "plugins"));

				int bottom = Math.Max(left.Bottom, right.Bottom);
				return bottom + scrollY + 12;
			}

			/// <summary>
			/// "Version X is available": a balloon under the pet with Install now, What's new and Later (Later hides it
			/// until the next version; the version line of "The program" and Settings still offer it). Returns the bottom.
			/// </summary>
			private int PaintUpdateBanner(Graphics g, int y, int w, Updater.Release news)
			{
				string v = news.Version.ToString(3);
				// downloaded in the background: it waits for the program to close (or a restart now)
				bool ready = Updater.Staged == news;
				var buttonsList = new List<(string text, string id, bool primary, Action run)>
				{
					ready ? ("🔄 " + Lang.T("home.updRestart"), "upd:install", true, (Action)Updater.RestartToInstall) : ("⬇️ " + Lang.T("home.updInstall"), "upd:install", true, (Action)(() => _ = Updater.InstallAvailableAsync())),
				};
				if (!string.IsNullOrWhiteSpace(news.Notes))
					buttonsList.Add((Lang.T("home.updNotes"), "upd:notes", false, () =>
						TextViewer.Show(form, Lang.T("whatsnew.notesTitle", v), news.Notes, "🎉")));
				if (!ready) buttonsList.Add((Lang.T("home.updLater"), "upd:later", false, () => { Program.MyData.SetSetting("UpdateBannerHidden", news.Version.ToString()); Invalidate(); }));
				var widths = buttonsList.Select(b => Theme.MeasureText(g, b.text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 32).ToList();
				int allW = widths.Sum() + (widths.Count - 1) * 8;
				// the buttons on the right of the text, or under it when the window is narrow
				string sub = ready ? Lang.T("home.updReadyText") : Lang.T("home.updText", Updater.CurrentVersion.ToString(3));
				int subW = Theme.MeasureText(g, sub, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width;
				bool under = w - allW - 110 < Math.Max(300, subW);
				var box = new Rectangle(8, y, w, under ? 128 : 84);
				Theme.FillRounded(g, Theme.AccentSoft, box, 14);
				Theme.DrawRounded(g, Theme.Accent, box, 14);
				var ic = new Rectangle(box.X + 18, box.Y + 18, 44, 44);
				Theme.FillRounded(g, Theme.Accent, ic, 12);
				Theme.DrawText(g, "🎉", emojiFont, ic, Theme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				int textW = (under ? box.Right - 18 : box.Right - 24 - allW - 12) - (ic.Right + 14);
				Theme.DrawText(g, Lang.T(ready ? "home.updReadyTitle" : "home.updTitle", v), form.fontBold, new Rectangle(ic.Right + 14, box.Y + 18, textW, 22), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, sub, form.fontSmall, new Rectangle(ic.Right + 14, box.Y + 42, textW, 20), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				int bx = under ? ic.Right + 14 : box.Right - 24 - allW, by = under ? box.Y + 78 : box.Y + 25;
				for (int i = 0; i < buttonsList.Count; i++)
				{
					var b = buttonsList[i];
					var r = new Rectangle(bx, by, widths[i], 34);
					Theme.DrawButton(g, r, b.text, form.fontSmall, b.primary, hover == b.id);
					buttons.Add((r, b.run, b.id));
					bx = r.Right + 8;
				}
				return box.Bottom;
			}

			/// <summary>The metrics the week chart can show (keys and computer time only with their plugins on).</summary>
			private List<(string id, string label, Func<int[]> values, Func<int, string> format)> WeekMetrics()
			{
				var list = new List<(string, string, Func<int[]>, Func<int, string>)>
				{
					("focus", Lang.T("home.week.focus"), () => DayStats.Week("focusMin"), m => Pomodoro.Minutes(m)),
					("breaks", Lang.T("home.week.breaks"), () => DayStats.Week("breaks"), n => n.ToString()),
				};
				if (TypingPet.Enabled) list.Add(("keys", Lang.T("home.week.keys"), () => DayStats.Week("keys"), n => n >= 10000 ? Lang.T("home.week.thousand", n / 1000) : n.ToString("N0")));
				if (AppTime.Enabled) list.Add(("pc", Lang.T("home.week.pc"), AppTime.Week, m => Pomodoro.Minutes(m)));
				return list;
			}

			/// <summary>"Your week": tabs for the metric and seven bars, today highlighted. Returns the bottom.</summary>
			private int PaintWeek(Graphics g, int y, int w)
			{
				var metrics = WeekMetrics();
				var metric = metrics.FirstOrDefault(m => m.id == weekMetric);
				if (metric.id == null) { metric = metrics[0]; weekMetric = metric.id; }
				int[] values = metric.values();
				var panel = new Rectangle(8, y, w, 250);

				// tabs on the right of the title (under it when the window is narrow)
				int tabsW = metrics.Sum(m => Theme.MeasureText(g, m.label, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24 + 4);
				bool under = tabsW > panel.Width - 220;
				if (under) panel.Height += 36;
				Panel(g, panel, "📊  " + Lang.T("home.week"));
				int tx = under ? panel.X + 20 : panel.Right - 16 - tabsW, ty = under ? panel.Y + 46 : panel.Y + 12;
				foreach (var m in metrics)
				{
					int tw = Theme.MeasureText(g, m.label, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					var r = new Rectangle(tx, ty, tw, 28);
					bool sel = m.id == weekMetric;
					string id = "week:" + m.id;
					if (sel) Theme.FillRounded(g, Theme.Accent, r, 14);
					else if (hover == id) Theme.FillRounded(g, Theme.Hover, r, 14);
					Theme.DrawText(g, m.label, form.fontSmall, r, sel ? Theme.AccentText : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					string mid = m.id;
					buttons.Add((r, () => { weekMetric = mid; Invalidate(); }, id));
					tx = r.Right + 4;
				}

				// total and average of the days with something
				int top = panel.Y + (under ? 84 : 50);
				int total = values.Sum(), used = values.Count(v => v > 0);
				string sum = Lang.T("home.week.total", metric.format(total), metric.format(used == 0 ? 0 : total / used));
				Theme.DrawText(g, sum, form.fontSmall, new Rectangle(panel.X + 20, top, panel.Width - 40, 18), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

				// the bars
				int chartTop = top + 30, chartBottom = panel.Bottom - 34;
				if (total == 0)
					Theme.DrawText(g, Lang.T("home.week.empty"), form.fontText, new Rectangle(panel.X + 20, chartTop + 30, panel.Width - 40, 40), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
				int most = Math.Max(1, values.Max());
				int colW = (panel.Width - 40) / 7;
				int barW = Math.Min(46, colW - 16);
				System.Globalization.CultureInfo culture;
				try { culture = System.Globalization.CultureInfo.GetCultureInfo(Lang.Current); } catch (System.Globalization.CultureNotFoundException) { culture = System.Globalization.CultureInfo.CurrentUICulture; }
				for (int i = 0; i < 7; i++)
				{
					var day = DateTime.Today.AddDays(i - 6);
					int cx = panel.X + 20 + i * colW + colW / 2;
					int maxH = chartBottom - chartTop - 20;
					int bh = values[i] == 0 ? 4 : Math.Max(8, (int)(maxH * values[i] / (double)most));
					var bar = new Rectangle(cx - barW / 2, chartBottom - bh, barW, bh);
					Theme.FillRounded(g, i == 6 ? Theme.Accent : values[i] == 0 ? Theme.SurfaceAlt : Theme.AccentSoft, bar, Math.Min(8, barW / 3));
					if (values[i] > 0)
						Theme.DrawText(g, metric.format(values[i]), form.fontSmall, new Rectangle(cx - colW / 2, bar.Y - 20, colW, 18), i == 6 ? Theme.Text : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
					string label = i == 6 ? Lang.T("home.week.today") : culture.DateTimeFormat.GetAbbreviatedDayName(day.DayOfWeek).TrimEnd('.');
					Theme.DrawText(g, label, i == 6 ? form.fontBold : form.fontSmall, new Rectangle(cx - colW / 2, chartBottom + 8, colW, 20), i == 6 ? Theme.Text : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
				}
				return panel.Bottom;
			}

			/// <summary>What the forecast chart shows (temperature, rain or wind) and which day (0 = the next 24 hours).</summary>
			private string weatherMetric = "temp";
			private int weatherDay;
			/// <summary>Where the hours chart was drawn, its hours and the one under the mouse (-1: none).</summary>
			private Rectangle chartArea;
			private List<(DateTime time, double temp, int rain, double wind, int code)> chartHours;
			private int hoverHour = -1;

			private static void OpenUrl(string url)
			{
				try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
				catch (Exception skipped) { LogFile.Ignored(skipped); }
			}
			private Font weatherBig, weatherTemp;

			/// <summary>
			/// "Weather forecast": the weather now (big), the hours as a chart (tabs: temperature, rain, wind) and the
			/// next days (click one to see its hours). Like a weather site, with the city chosen for the weather plugin.
			/// </summary>
			private int PaintWeather(Graphics g, int y, int w)
			{
				weatherBig = weatherBig ?? new Font("Segoe UI Emoji", 34, FontStyle.Regular, GraphicsUnit.Pixel);
				weatherTemp = weatherTemp ?? Theme.CreateFont(34, FontStyle.Bold);
				if (Weather.ForecastStale) Weather.RefreshForecast(() => { if (!IsDisposed) Invalidate(); });
				var f = Weather.ForecastCached;
				string city = Weather.City?.Name.Split(',')[0] ?? "";
				var metrics = new[] { ("temp", Lang.T("home.weatherTemp")), ("rain", Lang.T("home.weatherRain")), ("wind", Lang.T("home.weatherWind")) };

				string outlook = Weather.Outlook(f);
				var panel = new Rectangle(8, y, w, f == null ? 96 : 372 + (outlook.Length > 0 ? 30 : 0) + 24);
				int tabsW = metrics.Sum(m => Theme.MeasureText(g, m.Item2, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 28);
				bool under = f != null && tabsW > panel.Width - 320;
				if (under) panel.Height += 36;
				Panel(g, panel, "🌤️  " + Lang.T("home.weatherTitle") + (city.Length > 0 ? " · " + city : ""));
				if (f == null)
				{
					Theme.DrawText(g, Lang.T("home.weatherLoading"), form.fontText, new Rectangle(panel.X + 20, panel.Y + 52, panel.Width - 40, 24), Theme.TextMuted, TextFormatFlags.SingleLine);
					return panel.Bottom;
				}
				var culture = HomeCulture();

				// tabs, like the week chart
				int tx = under ? panel.X + 20 : panel.Right - 16 - tabsW, ty = under ? panel.Y + 46 : panel.Y + 12;
				foreach (var (id, label) in metrics)
				{
					int tw = Theme.MeasureText(g, label, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					var r = new Rectangle(tx, ty, tw, 28);
					bool sel = id == weatherMetric;
					string hid = "wmetric:" + id;
					if (sel) Theme.FillRounded(g, Theme.Accent, r, 14);
					else if (hover == hid) Theme.FillRounded(g, Theme.Hover, r, 14);
					Theme.DrawText(g, label, form.fontSmall, r, sel ? Theme.AccentText : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					string mid = id;
					buttons.Add((r, () => { weatherMetric = mid; Invalidate(); }, hid));
					tx = r.Right + 4;
				}

				// --- now: emoji, temperature, rain / humidity / wind; on the right the day, the time and the sky
				int top = panel.Y + (under ? 84 : 50);
				Theme.DrawText(g, Weather.Emoji(f.Code, f.IsDay), weatherBig, new Rectangle(panel.X + 16, top, 60, 56), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
				string temp = Math.Round(f.Temp) + "°C";
				int tempW = Theme.MeasureText(g, temp, weatherTemp, int.MaxValue, TextFormatFlags.SingleLine).Width;
				Theme.DrawText(g, temp, weatherTemp, new Rectangle(panel.X + 82, top, tempW + 4, 56), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				int dx = panel.X + 92 + tempW;
				Theme.DrawText(g, Lang.T("home.weatherRainNow", f.RainNow), form.fontSmall, new Rectangle(dx, top + 2, 200, 17), Theme.TextMuted, TextFormatFlags.SingleLine);
				Theme.DrawText(g, Lang.T("home.weatherHumidity", f.Humidity), form.fontSmall, new Rectangle(dx, top + 19, 200, 17), Theme.TextMuted, TextFormatFlags.SingleLine);
				Theme.DrawText(g, Lang.T("home.weatherWindNow", Math.Round(f.Wind)), form.fontSmall, new Rectangle(dx, top + 36, 200, 17), Theme.TextMuted, TextFormatFlags.SingleLine);
				var right = new Rectangle(panel.Right - 320, top, 300, 20);
				Theme.DrawText(g, Capital(f.Now.ToString("dddd, HH:mm", culture)), form.fontText, right, Theme.Text, TextFormatFlags.Right | TextFormatFlags.SingleLine);
				Theme.DrawText(g, Capital(Lang.T(Weather.Describe(f.Code).key)), form.fontText, new Rectangle(right.X, right.Y + 20, right.Width, 20), Theme.TextMuted, TextFormatFlags.Right | TextFormatFlags.SingleLine);
				Theme.DrawText(g, Lang.T("home.weatherFeels", Math.Round(f.Feels)), form.fontSmall, new Rectangle(right.X, right.Y + 40, right.Width, 18), Theme.TextMuted, TextFormatFlags.Right | TextFormatFlags.SingleLine);
				// a click on "now" opens the full forecast of the city in the browser
				var nowRow = new Rectangle(panel.X + 12, top - 4, panel.Width - 24, 64);
				string place = Weather.City?.Name ?? city;
				buttons.Add((nowRow, () => OpenUrl("https://www.google.com/search?q=" + Uri.EscapeDataString(Lang.T("home.weatherSearch") + " " + place)), "wsite"));

				// --- the hours of the chosen day (the next 24 hours for today)
				if (weatherDay >= f.Days.Count) weatherDay = 0;
				var start = new DateTime(f.Now.Year, f.Now.Month, f.Now.Day, f.Now.Hour, 0, 0);
				var hours = weatherDay == 0
					? f.Hours.Where(h => h.time >= start).Take(25).ToList()
					: f.Hours.Where(h => h.time.Date == f.Days[weatherDay].date.Date).ToList();
				var chart = new Rectangle(panel.X + 34, top + 92, panel.Width - 68, 104);
				chartArea = chart; chartHours = hours;
				if (hours.Count > 1) PaintHours(g, chart, hours, culture);

				// --- the days: weekday, sky, high and low; click one to see its hours
				int days = Math.Min(8, f.Days.Count);
				int strip = chart.Bottom + 34, colW = (panel.Width - 40) / Math.Max(1, days);
				for (int i = 0; i < days; i++)
				{
					var d = f.Days[i];
					var cell = new Rectangle(panel.X + 20 + i * colW + 3, strip, colW - 6, 92);
					string hid = "wday:" + i;
					if (i == weatherDay) Theme.FillRounded(g, Theme.SurfaceAlt, cell, 10);
					else if (hover == hid) Theme.FillRounded(g, Theme.Hover, cell, 10);
					string name = i == 0 ? Lang.T("home.week.today") : culture.DateTimeFormat.GetAbbreviatedDayName(d.date.DayOfWeek).TrimEnd('.');
					Theme.DrawText(g, name, i == weatherDay ? form.fontBold : form.fontText, new Rectangle(cell.X, cell.Y + 6, cell.Width, 20), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
					Theme.DrawText(g, Weather.Emoji(d.code), emojiFont, new Rectangle(cell.X, cell.Y + 26, cell.Width, 34), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					string hiLo = $"{d.max}°  {d.min}°";
					Theme.DrawText(g, hiLo, form.fontSmall, new Rectangle(cell.X, cell.Y + 64, cell.Width, 20), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
					int di = i;
					buttons.Add((cell, () => { weatherDay = di; Invalidate(); }, hid));
				}
				// what is coming, in words: rain at what time, a storm, tomorrow cooler or warmer
				if (outlook.Length > 0)
					Theme.DrawText(g, "💬 " + outlook, form.fontText, new Rectangle(panel.X + 20, strip + 98, panel.Width - 40, 22), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				// the source of the data (Open-Meteo, CC BY 4.0: the license asks for it to be named)
				string source = Lang.T("home.weatherSource");
				int sw2 = Theme.MeasureText(g, source, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 4;
				var src = new Rectangle(panel.Right - 20 - sw2, panel.Bottom - 26, sw2, 18);
				Theme.DrawText(g, source, form.fontSmall, src, hover == "wsource" ? Theme.Link : Theme.TextMuted, TextFormatFlags.SingleLine);
				buttons.Add((src, () => OpenUrl("https://open-meteo.com/"), "wsource"));
				return panel.Bottom;
			}

			/// <summary>The hours as a chart: temperature (line with area), rain (bars, %) or wind (line, km/h); a value and the time every 3 hours.</summary>
			private void PaintHours(Graphics g, Rectangle chart, List<(DateTime time, double temp, int rain, double wind, int code)> hours, System.Globalization.CultureInfo culture)
			{
				var warm = Color.FromArgb(244, 180, 0);
				var water = Color.FromArgb(66, 133, 244);
				var air = Color.FromArgb(52, 168, 140);
				double[] v = hours.Select(h => weatherMetric == "rain" ? h.rain : weatherMetric == "wind" ? h.wind : h.temp).ToArray();
				string Fmt(double x) => weatherMetric == "rain" ? Math.Round(x) + "%" : weatherMetric == "wind" ? Math.Round(x) + " km/h" : Math.Round(x).ToString();
				double lo = weatherMetric == "rain" ? 0 : v.Min(), hi = weatherMetric == "rain" ? 100 : v.Max();
				if (hi - lo < 4) { double mid = (hi + lo) / 2; lo = mid - 2; hi = mid + 2; }
				if (weatherMetric == "wind") lo = Math.Min(lo, 0);
				int n = v.Length;
				float step = chart.Width / (float)(n - 1);
				float X(int i) => chart.X + i * step;
				float Y(double x) => chart.Bottom - (float)((x - lo) / (hi - lo)) * (chart.Height - 22);

				var oldMode = g.SmoothingMode;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				if (weatherMetric == "rain")
				{
					float bw = Math.Max(3, step * 0.6f);
					using (var b = new SolidBrush(Color.FromArgb(150, water)))
						for (int i = 0; i < n; i++)
						{
							float bh = Math.Max(2, chart.Bottom - Y(v[i]));
							g.FillRectangle(b, X(i) - bw / 2, chart.Bottom - bh, bw, bh);
						}
				}
				else
				{
					var color = weatherMetric == "wind" ? air : warm;
					var pts = Enumerable.Range(0, n).Select(i => new PointF(X(i), Y(v[i]))).ToArray();
					using (var area = new GraphicsPath())
					{
						area.AddLines(pts);
						area.AddLine(pts[n - 1], new PointF(pts[n - 1].X, chart.Bottom));
						area.AddLine(new PointF(pts[n - 1].X, chart.Bottom), new PointF(pts[0].X, chart.Bottom));
						area.CloseFigure();
						using (var b = new SolidBrush(Color.FromArgb(60, color))) g.FillPath(b, area);
					}
					using (var pen = new Pen(color, 2.5f)) g.DrawLines(pen, pts);
				}
				// the hour under the mouse: a line and its values (time, temperature, rain, wind)
				if (hoverHour >= 0 && hoverHour < n)
				{
					float hx = X(hoverHour);
					using (var pen = new Pen(Theme.Border, 1f) { DashStyle = DashStyle.Dash }) g.DrawLine(pen, hx, chart.Y - 4, hx, chart.Bottom);
					using (var dot = new SolidBrush(weatherMetric == "rain" ? water : weatherMetric == "wind" ? air : warm)) g.FillEllipse(dot, hx - 4, Y(v[hoverHour]) - 4, 8, 8);
					var hh = hours[hoverHour];
					string tip = hh.time.ToString("HH:mm", culture) + "  ·  " + Math.Round(hh.temp) + "°  ·  ☔ " + hh.rain + "%  ·  " + Math.Round(hh.wind) + " km/h";
					var ts = Theme.MeasureText(g, tip, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine);
					var box = new Rectangle((int)hx - ts.Width / 2 - 10, chart.Y - 34, ts.Width + 20, 24);
					if (box.X < chart.X - 20) box.X = chart.X - 20;
					if (box.Right > chart.Right + 20) box.X = chart.Right + 20 - box.Width;
					Theme.FillRounded(g, Theme.SurfaceAlt, box, 8);
					Theme.DrawRounded(g, Theme.Border, box, 8);
					Theme.DrawText(g, tip, form.fontSmall, box, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				}
				g.SmoothingMode = oldMode;
				// a value above and the time below, every 3 hours (the first one is "now" for today)
				for (int i = 0; i < n; i += 3)
				{
					var lbl = new Rectangle((int)X(i) - 30, (int)Y(v[i]) - 20, 60, 16);
					Theme.DrawText(g, Fmt(v[i]), form.fontSmall, lbl, i == 0 ? Theme.Text : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
					Theme.DrawText(g, hours[i].time.ToString("HH:mm", culture), form.fontSmall, new Rectangle((int)X(i) - 30, chart.Bottom + 6, 60, 18), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
				}
			}

			/// <summary>"quarta-feira, 20:45" → "Quarta-feira, 20:45" (only the first letter).</summary>
			private static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

			private static System.Globalization.CultureInfo HomeCulture()
			{
				try { return System.Globalization.CultureInfo.GetCultureInfo(Lang.Current); }
				catch (System.Globalization.CultureNotFoundException) { return System.Globalization.CultureInfo.CurrentUICulture; }
			}

			private void Panel(Graphics g, Rectangle r, string title)
			{
				Theme.FillRounded(g, Theme.Surface, r, 12);
				Theme.DrawRounded(g, Theme.Border, r, 12);
				Theme.DrawText(g, title, form.fontBold, new Rectangle(r.X + 20, r.Y + 14, r.Width - 40, 26), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
			}

			private int HeroButton(Graphics g, int x, int y, string text, string id, Action run)
			{
				int bw = Theme.MeasureText(g, text, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 30;
				var r = new Rectangle(x, y, bw, 32);
				Theme.FillRounded(g, hover == id ? Color.FromArgb(70, Color.White) : Color.FromArgb(40, Color.White), r, 16);
				Theme.DrawText(g, text, form.fontSmall, r, Theme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				buttons.Add((r, run, id));
				return r.Right;
			}

			private static Rectangle Fit(Size img, Rectangle box)
			{
				double k = Math.Min(box.Width / (double)img.Width, box.Height / (double)img.Height);
				if (k >= 1) k = Math.Floor(k);   // pixel art: whole steps
				int w = (int)(img.Width * k), h = (int)(img.Height * k);
				return new Rectangle(box.X + (box.Width - w) / 2, box.Bottom - h, w, h);
			}

			/// <summary>One line about the pet now: focusing, its needs, or ready.</summary>
			private static string PetLine()
			{
				if (Pomodoro.Current != Pomodoro.Phase.Off) return "🍅 " + Pomodoro.Status();
				if (HideHotkey.Hidden) return Lang.T("home.hidden");
				if (DoNotDisturb.Active) return "🔕 " + Lang.T("home.dnd", DoNotDisturb.StatusText);
				return Lang.T("home.ready");
			}

			private static string MoodToday()
			{
				return DesktopPet.Mood.History.TryGetValue(DateTime.Today.ToString("yyyy-MM-dd"), out int v) && v >= 0 && v < DesktopPet.Mood.Faces.Length ? DesktopPet.Mood.Faces[v] : "—";
			}

			/// <summary>Reminders still due today, in order.</summary>
			private static List<(DateTime at, string text)> TodayReminders()
			{
				var now = DateTime.Now; var end = DateTime.Today.AddDays(1);
				var list = new List<(DateTime, string)>();
				foreach (var r in Reminders.All.Where(r => r.Enabled))
				{
					var t = r.Next(now);
					if (t.HasValue && t.Value < end) list.Add((t.Value, r.Text));
				}
				return list.OrderBy(x => x.Item1).ToList();
			}

			private static List<(string when, string what)> NextItems()
			{
				var items = new List<(DateTime at, string when, string what)>();
				foreach (var (at, text) in TodayReminders().Take(4)) items.Add((at, at.ToString("HH:mm"), "⏰ " + text));
				var meeting = Agenda.Configured ? Agenda.Next : null;
				if (meeting != null && meeting.Start < DateTime.Now.AddDays(1))
					items.Add((meeting.Start, (meeting.Start.Date == DateTime.Today ? "" : Lang.T("home.tomorrow") + " ") + meeting.Start.ToString("HH:mm"), "📅 " + meeting.Title));
				// (the weather has its own card, the forecast)
				return items.OrderBy(i => i.at).Take(5).Select(i => (i.when, i.what)).ToList();
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				string h = buttons.FirstOrDefault(b => b.rect.Contains(e.Location)).id;
				Cursor = h != null ? Cursors.Hand : Cursors.Default;
				// the hour of the forecast chart under the mouse
				int hh = -1;
				var zone = Rectangle.Inflate(chartArea, 12, 20);
				if (chartHours != null && chartHours.Count > 1 && zone.Contains(e.Location))
					hh = Math.Max(0, Math.Min(chartHours.Count - 1, (int)Math.Round((e.X - chartArea.X) / (chartArea.Width / (double)(chartHours.Count - 1)))));
				if (h != hover || hh != hoverHour) { hover = h; hoverHour = hh; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; hoverHour = -1; Invalidate(); }

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				if (e.Button != MouseButtons.Left) return;
				var b = buttons.FirstOrDefault(x => x.rect.Contains(e.Location));
				b.run?.Invoke();
				Invalidate();
			}
		}

		/// <summary>The picture of the pet in use (gallery card, or the built-in eSheep).</summary>
		private Image CurrentPetImage()
		{
			string folder = petsView?.CurrentPet ?? "";
			var pet = WebPets.pets.FirstOrDefault(p => string.Equals(p.folder, folder, StringComparison.OrdinalIgnoreCase));
			if (pet?.Image != null) return pet.Image;
			try { return BuiltInPets.GetIcon(BuiltInPets.IsBuiltIn(folder) ? folder : "esheep64"); } catch (Exception) { return null; }
		}
	}
}
