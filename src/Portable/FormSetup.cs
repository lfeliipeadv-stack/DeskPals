using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// The first setup, in a few short steps: how the pet calls you, what it does (plugins), your city (only with the
	/// weather ticked), the AI (optional) and "all set". Everything has a good default, "Skip" is always there, and
	/// nothing is saved before "Start" (except the AI, which its own window saves). It opens after the pet's
	/// introduction the first time, and again from Settings › "Redo the first setup".
	/// </summary>
	internal sealed class FormSetup : ThemedDialog
	{
		internal enum Step { Name, Plugins, City, Ai, Done }

		internal sealed class Choice
		{
			public string Id, Emoji, Key;
			public Func<bool> Get;
			public Action<bool> Set;
			public bool On;
			public Rectangle Rect;
		}

		/// <summary>The plugins offered (they need nothing else, except the weather: its city is the next step).</summary>
		internal static List<Choice> Choices() => new List<Choice>
		{
			new Choice { Id = "focus", Emoji = "🍅", Key = "plug.focus", Get = () => PluginSwitch.On("focus"), Set = on => PluginSwitch.Set("focus", on), On = true },
			new Choice { Id = "timer", Emoji = "⏱️", Key = "plug.timer", Get = () => PluginSwitch.On("timer"), Set = on => PluginSwitch.Set("timer", on), On = true },
			new Choice { Id = "games", Emoji = "🥠", Key = "plug.games", Get = () => PluginSwitch.On("games"), Set = on => PluginSwitch.Set("games", on), On = true },
			new Choice { Id = "notes", Emoji = "📝", Key = "plug.notes", Get = () => PluginSwitch.On("notes"), Set = on => PluginSwitch.Set("notes", on), On = true },
			new Choice { Id = "weather", Emoji = "🌦️", Key = "plug.weather", Get = () => Weather.Enabled, Set = on => Weather.Enabled = on },
			new Choice { Id = "dnd", Emoji = "🔕", Key = "plug.dnd", Get = () => DoNotDisturb.Calls, Set = on => DoNotDisturb.Calls = on, On = true },
			new Choice { Id = "mood", Emoji = "😊", Key = "opt.mood", Get = () => Mood.Daily, Set = on => Mood.Daily = on },
			new Choice { Id = "vpet", Emoji = "🍖", Key = "opt.vpet", Get = () => VirtualPet.Enabled, Set = on => VirtualPet.Enabled = on },
			new Choice { Id = "summary", Emoji = "🌙", Key = "plug.summary", Get = () => DaySummary.Enabled, Set = on => DaySummary.Enabled = on },
			new Choice { Id = "typing", Emoji = "⌨️", Key = "plug.typing", Get = () => TypingPet.Enabled, Set = on => TypingPet.Enabled = on },
		};

		/// <summary>The setup was done (or skipped) once: it does not open by itself again.</summary>
		public static bool Done => Program.MyData.GetSettingBool("SetupDone", false) || Program.MyData.GetSettingBool("WelcomeDone", false);

		readonly List<Choice> choices = Choices();
		readonly TextBox name = new TextBox { BorderStyle = BorderStyle.None, MaxLength = UserProfile.MaxLength };
		readonly TextBox city = new TextBox { BorderStyle = BorderStyle.None, MaxLength = 80 };
		readonly Button next, back, skip;
		readonly Font fCard = Theme.CreateFont(13, FontStyle.Bold), fEmoji = Theme.CreateFont(20), fBig = Theme.CreateFont(15, FontStyle.Bold);
		readonly Timer searchTimer = new Timer { Interval = 450 };
		Step step = Step.Name;
		Weather.Place place = Weather.City;
		List<Weather.Place> found = new List<Weather.Place>();
		string cityStatus;
		int searchId;
		bool settingCity, startup = true, finished;
		object hover;
		Rectangle field, aiButton, startupToggle;
		readonly List<Rectangle> foundRects = new List<Rectangle>();
		const int W = 640, H = 654, CardH = 74, Gap = 10, Top = 150;

		public FormSetup()
		{
			Text = Lang.T("setup.title");
			iconEmoji = "✨";
			ClientSize = new Size(W, H);
			// what is on already stays ticked (opened again from Settings)
			foreach (var c in choices) if (c.Get()) c.On = true;
			try { startup = WindowsStartup.Enabled || !Done; } catch (Exception skipped) { LogFile.Ignored(skipped); }
			name.Text = UserProfile.Name;
			name.PlaceholderText = UserProfile.Suggestion().Length > 0 ? Lang.T("setup.nameExample", UserProfile.Suggestion()) : Lang.T("setup.namePlaceholder");
			city.Text = place?.Name ?? "";
			city.PlaceholderText = Lang.T("setup.cityPlaceholder");
			foreach (var t in new[] { name, city }) { t.Font = fText; t.BackColor = Theme.Surface; t.ForeColor = Theme.Text; Controls.Add(t); }

			next = AddButton(Lang.T("setup.next"), DialogResult.None, true);
			back = AddButton(Lang.T("setup.back"), DialogResult.None, false);
			skip = AddButton(Lang.T("setup.skip"), DialogResult.None, false);
			foreach (var b in buttons) b.Font = fText;
			ApplyTheme(next);
			// the theme gives text boxes a border: here the rounded field around them is the border
			foreach (var t in new[] { name, city }) { t.BorderStyle = BorderStyle.None; t.BackColor = Theme.Surface; t.ForeColor = Theme.Text; }
			// "Skip" is a quiet link-like button, not a box
			skip.FlatStyle = FlatStyle.Flat;
			skip.FlatAppearance.BorderSize = 0;
			skip.BackColor = Theme.Background;
			skip.FlatAppearance.MouseOverBackColor = Theme.Hover;
			skip.FlatAppearance.MouseDownBackColor = Theme.Hover;
			skip.ForeColor = Theme.TextMuted;
			next.Click += (s, e) => Next();
			back.Click += (s, e) => Go(-1);
			skip.Click += (s, e) => Close();

			city.TextChanged += (s, e) =>
			{
				if (settingCity) return;
				place = null;
				searchTimer.Stop();
				if (city.Text.Trim().Length >= 2) searchTimer.Start(); else { found.Clear(); cityStatus = null; Invalidate(); }
			};
			searchTimer.Tick += async (s, e) => { searchTimer.Stop(); await Search(); };
			name.GotFocus += (s, e) => Invalidate(); name.LostFocus += (s, e) => Invalidate();
			city.GotFocus += (s, e) => Invalidate(); city.LostFocus += (s, e) => Invalidate();
			Show(Step.Name);
		}

		/// <summary>The steps of this run: the city only with the weather ticked.</summary>
		List<Step> Steps()
		{
			var list = new List<Step> { Step.Name, Step.Plugins };
			if (choices.First(c => c.Id == "weather").On) list.Add(Step.City);
			list.Add(Step.Ai);
			list.Add(Step.Done);
			return list;
		}

		void Show(Step s)
		{
			step = s;
			var steps = Steps();
			int i = steps.IndexOf(s);
			name.Visible = s == Step.Name;
			city.Visible = s == Step.City;
			back.Visible = i > 0;
			next.Text = s == Step.Done ? Lang.T("setup.start") : Lang.T("setup.next");
			skip.Visible = s != Step.Done;
			// the buttons: Next on the right, Back next to it, Skip on the left
			int by = H - Pad - 38;
			next.Location = new Point(W - Pad - next.Width, by);
			back.Location = new Point(next.Left - 10 - back.Width, by);
			skip.Location = new Point(Pad - 8, by);
			field = new Rectangle(Pad, Top + 10, W - Pad * 2, 46);
			var box = s == Step.Name ? name : city;
			box.Width = field.Width - 28;
			box.Location = new Point(field.X + 14, field.Y + (field.Height - box.Height) / 2);
			Invalidate();
			if (s == Step.Name) name.Focus();
			else if (s == Step.City) city.Focus();
			else next.Focus();
		}

		void Go(int by)
		{
			var steps = Steps();
			int i = Math.Max(0, Math.Min(steps.Count - 1, steps.IndexOf(step) + by));
			Show(steps[i]);
		}

		void Next()
		{
			if (step == Step.City && place == null)
			{
				// the first suggestion, or no weather without a city
				if (found.Count > 0) place = found[0];
				else { choices.First(c => c.Id == "weather").On = false; }
			}
			if (step == Step.Done) { Apply(); finished = true; DialogResult = DialogResult.OK; Close(); return; }
			Go(1);
		}

		async Task Search()
		{
			int id = ++searchId;
			string text = city.Text.Trim();
			cityStatus = Lang.T("weather.searching");
			Invalidate();
			try
			{
				var list = await Weather.FindCities(text, 5);
				if (id != searchId || IsDisposed) return;
				found = list;
				cityStatus = list.Count == 0 ? Lang.T("weather.notFound", text) : null;
			}
			catch (Exception ex)
			{
				if (id != searchId || IsDisposed) return;
				found.Clear();
				cityStatus = Lang.T("weather.error", ex.Message);
			}
			Invalidate();
		}

		void Pick(Weather.Place p)
		{
			place = p;
			settingCity = true;
			city.Text = p.Name;
			city.SelectionStart = city.Text.Length;
			settingCity = false;
			found.Clear();
			cityStatus = null;
			Invalidate();
		}

		/// <summary>Save everything: the name, the city, the plugins, starting with Windows.</summary>
		internal void Apply()
		{
			UserProfile.Name = name.Text;
			var weather = choices.First(c => c.Id == "weather");
			if (weather.On && place != null) Weather.City = place;
			if (weather.On && Weather.City == null) weather.On = false;
			foreach (var c in choices)
				if (c.On != c.Get())
					try { c.Set(c.On); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			try { if (startup != WindowsStartup.Enabled) WindowsStartup.Set(startup); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			Program.MyData.SetSetting("SetupDone", true);
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			// closed or skipped: it does not open by itself again (Settings › "Redo the first setup")
			if (!finished) Program.MyData.SetSetting("SetupDone", true);
			searchTimer.Dispose();
			base.OnFormClosed(e);
		}

		// ------------------------------------------------------------------ drawing

		(string emoji, string title, string text) Header()
		{
			switch (step)
			{
				case Step.Name: return ("👋", Lang.T("setup.nameTitle"), Lang.T("setup.nameText"));
				case Step.Plugins: return ("✨", Lang.T("welcome.title"), Lang.T("welcome.text"));
				case Step.City: return ("🌦️", Lang.T("setup.cityTitle"), Lang.T("setup.cityText"));
				case Step.Ai: return ("💬", Lang.T("setup.aiTitle"), Lang.T("setup.aiText"));
				default: return ("🎉", name.Text.Trim().Length > 0 ? Lang.T("setup.doneTitleName", name.Text.Trim()) : Lang.T("setup.doneTitle"), Lang.T("setup.doneText"));
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			var (emoji, title, text) = Header();
			iconEmoji = emoji;
			PaintIcon(g);
			Theme.DrawText(g, title, fTitle, new Rectangle(Pad + 64, Pad + 8, W - Pad * 2 - 64 - 90, 32), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			// progress: one dot per step of this run
			var steps = Steps();
			int cur = steps.IndexOf(step);
			for (int i = 0; i < steps.Count; i++)
			{
				var d = new Rectangle(W - Pad - (steps.Count - i) * 16 + 4, Pad + 18, 10, 10);
				Theme.FillRounded(g, i <= cur ? Theme.Accent : Theme.Border, d, 5);
			}
			Theme.DrawText(g, text, fText, new Rectangle(Pad, Pad + 62, W - Pad * 2, 60), Theme.TextMuted, TextFormatFlags.WordBreak);
			switch (step)
			{
				case Step.Name: PaintField(g, name); Theme.DrawText(g, Lang.T("setup.nameHint"), fSmall, new Rectangle(Pad, field.Bottom + 10, W - Pad * 2, 40), Theme.TextMuted, TextFormatFlags.WordBreak); break;
				case Step.Plugins: PaintPlugins(g); break;
				case Step.City: PaintCity(g); break;
				case Step.Ai: PaintAi(g); break;
				case Step.Done: PaintDone(g); break;
			}
		}

		void PaintField(Graphics g, TextBox box)
		{
			Theme.FillRounded(g, Theme.Surface, field, 10);
			Theme.DrawRounded(g, box.Focused ? Theme.Accent : Theme.Border, field, 10, box.Focused ? 2f : 1f);
		}

		void PaintPlugins(Graphics g)
		{
			int cw = (W - Pad * 2 - Gap) / 2;
			for (int i = 0; i < choices.Count; i++)
			{
				var c = choices[i];
				var r = c.Rect = new Rectangle(Pad + (i % 2) * (cw + Gap), Top - 14 + (i / 2) * (CardH + Gap), cw, CardH);
				Theme.FillRounded(g, c.On ? Theme.AccentSoft : hover == c ? Theme.Hover : Theme.Surface, r, 12);
				Theme.DrawRounded(g, c.On ? Theme.Accent : Theme.Border, r, 12, c.On ? 2f : 1f);
				Theme.DrawText(g, c.Emoji, fEmoji, new Rectangle(r.X + 10, r.Y, 40, r.Height), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				int tx = r.X + 56, tw = r.Width - 56 - 40;
				Theme.DrawText(g, Lang.T(c.Key + ".title"), fCard, new Rectangle(tx, r.Y + 12, tw, 22), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, Lang.T("welcome." + c.Id), fSmall, new Rectangle(tx, r.Y + 34, tw, r.Height - 40), Theme.TextMuted, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
				var box = new Rectangle(r.Right - 32, r.Y + (r.Height - 20) / 2, 20, 20);
				Theme.FillRounded(g, c.On ? Theme.Accent : Theme.Surface, box, 6);
				Theme.DrawRounded(g, c.On ? Theme.Accent : Theme.Border, box, 6);
				if (c.On) Theme.DrawText(g, "✓", fCard, box, Theme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}
		}

		void PaintCity(Graphics g)
		{
			PaintField(g, city);
			int y = field.Bottom + 12;
			foundRects.Clear();
			if (place != null)
				Theme.DrawText(g, "✓ " + Lang.T("setup.cityChosen", place.Name), fCard, new Rectangle(Pad, y, W - Pad * 2, 24), Theme.Accent, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			else if (cityStatus != null)
				Theme.DrawText(g, cityStatus, fSmall, new Rectangle(Pad, y, W - Pad * 2, 40), Theme.TextMuted, TextFormatFlags.WordBreak);
			else
				for (int i = 0; i < found.Count; i++)
				{
					var r = new Rectangle(Pad, y + i * 44, W - Pad * 2, 38);
					foundRects.Add(r);
					bool over = hover is int h && h == i;
					Theme.FillRounded(g, over ? Theme.Hover : Theme.Surface, r, 8);
					Theme.DrawRounded(g, over ? Theme.Accent : Theme.Border, r, 8);
					Theme.DrawText(g, "📍  " + found[i].Name, fText, new Rectangle(r.X + 12, r.Y, r.Width - 24, r.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				}
			Theme.DrawText(g, Lang.T("setup.cityPrivacy"), fSmall, new Rectangle(Pad, H - Pad - 38 - 52, W - Pad * 2, 40), Theme.TextMuted, TextFormatFlags.WordBreak);
		}

		void PaintAi(Graphics g)
		{
			var card = new Rectangle(Pad, Top, W - Pad * 2, 132);
			Theme.FillRounded(g, Theme.Surface, card, 12);
			Theme.DrawRounded(g, AiAssistant.Configured ? Theme.Accent : Theme.Border, card, 12, AiAssistant.Configured ? 2f : 1f);
			string state = AiAssistant.Configured
				? "✓ " + Lang.T("setup.aiReady", AiAssistant.Preset(AiAssistant.ProviderId).Name)
				: Lang.T("setup.aiNone");
			Theme.DrawText(g, state, fBig, new Rectangle(card.X + 20, card.Y + 18, card.Width - 40, 26), AiAssistant.Configured ? Theme.Accent : Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			Theme.DrawText(g, Lang.T("setup.aiFree"), fSmall, new Rectangle(card.X + 20, card.Y + 46, card.Width - 40, 36), Theme.TextMuted, TextFormatFlags.WordBreak);
			aiButton = new Rectangle(card.X + 20, card.Bottom - 46, 220, 34);
			Theme.DrawButton(g, aiButton, Lang.T(AiAssistant.Configured ? "setup.aiChange" : "setup.aiSetUp"), fText, !AiAssistant.Configured, hover as string == "ai");
			Theme.DrawText(g, Lang.T("setup.aiWithout"), fSmall, new Rectangle(Pad, card.Bottom + 16, W - Pad * 2, 60), Theme.TextMuted, TextFormatFlags.WordBreak);
		}

		void PaintDone(Graphics g)
		{
			var on = choices.Where(c => c.On).Select(c => Lang.T(c.Key + ".title")).ToList();
			var lines = new List<(string emoji, string text)>
			{
				("👋", name.Text.Trim().Length > 0 ? Lang.T("setup.sumName", name.Text.Trim()) : Lang.T("setup.sumNoName")),
				("✨", on.Count == 0 ? Lang.T("setup.sumNoPlugins") : Lang.T("setup.sumPlugins", on.Count, string.Join(", ", on))),
			};
			if (choices.First(c => c.Id == "weather").On && (place ?? Weather.City) != null) lines.Add(("🌦️", Lang.T("setup.sumCity", (place ?? Weather.City).Name)));
			lines.Add(("💬", AiAssistant.Configured ? Lang.T("setup.aiReady", AiAssistant.Preset(AiAssistant.ProviderId).Name) : Lang.T("setup.sumNoAi")));
			int y = Top - 6;
			foreach (var (emoji, text) in lines)
			{
				int h = Math.Max(30, Theme.MeasureText(g, text, fText, W - Pad * 2 - 44).Height + 8);
				Theme.DrawText(g, emoji, fText, new Rectangle(Pad, y, 34, 26), Theme.Text, TextFormatFlags.SingleLine);
				Theme.DrawText(g, text, fText, new Rectangle(Pad + 38, y + 2, W - Pad * 2 - 44, h), Theme.Text, TextFormatFlags.WordBreak);
				y += h + 6;
			}
			// start with Windows (on by default the first time)
			var row = new Rectangle(Pad, y + 14, W - Pad * 2, 56);
			Theme.FillRounded(g, Theme.Surface, row, 12);
			Theme.DrawRounded(g, Theme.Border, row, 12);
			Theme.DrawText(g, Lang.T("opt.startup.title"), fCard, new Rectangle(row.X + 18, row.Y, row.Width - 100, row.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			startupToggle = new Rectangle(row.Right - 18 - 44, row.Y + (row.Height - 22) / 2, 44, 22);
			Theme.DrawToggle(g, startupToggle, startup);
			Theme.DrawText(g, Lang.T("setup.doneHint"), fSmall, new Rectangle(Pad, row.Bottom + 16, W - Pad * 2, 44), Theme.TextMuted, TextFormatFlags.WordBreak);
		}

		// ------------------------------------------------------------------ mouse

		object HitAt(Point p)
		{
			if (step == Step.Plugins) return choices.FirstOrDefault(c => c.Rect.Contains(p));
			if (step == Step.City) { int i = foundRects.FindIndex(r => r.Contains(p)); return i >= 0 ? (object)i : null; }
			if (step == Step.Ai && aiButton.Contains(p)) return "ai";
			if (step == Step.Done && Rectangle.Inflate(startupToggle, 8, 8).Contains(p)) return "startup";
			return null;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var h = HitAt(e.Location);
			Cursor = h != null ? Cursors.Hand : Cursors.Default;
			if (!Equals(h, hover)) { hover = h; Invalidate(); }
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			switch (HitAt(e.Location))
			{
				case Choice c: c.On = !c.On; Invalidate(); break;
				case int i when i < found.Count: Pick(found[i]); break;
				case "ai":
					using (var f = new FormAiSettings()) f.ShowDialog(this);
					Invalidate();
					break;
				case "startup": startup = !startup; Invalidate(); break;
			}
		}

		// ------------------------------------------------------------------ open, self-test

		/// <summary>Show it (the pets wait while it is open). Once open, it comes to the front instead of a second one.</summary>
		public static void Open()
		{
			var open = Application.OpenForms.OfType<FormSetup>().FirstOrDefault();
			if (open != null) { open.Activate(); return; }
			using (var f = new FormSetup { StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = true })
				f.ShowDialog();
		}

		internal void TestGo(Step s) => Show(s);
		internal void TestTick(string id, bool on) { choices.First(x => x.Id == id).On = on; }
		internal void TestName(string n) => name.Text = n;
		internal void TestPlace(Weather.Place p) => Pick(p);
		internal void TestStartup(bool on) => startup = on;
		internal IReadOnlyList<Step> TestSteps => Steps();
	}
}
