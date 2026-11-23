using System.Diagnostics;
using System.Drawing;
using DesktopPet;

namespace DeskPalsWeather
{
	/// <summary>
	/// The settings of DeskPals Weather, drawn like DeskPals' own: the sections on the left (Weather, Scenes, Effects,
	/// Sound, Pauses, Shortcuts, About) and their options on the right as cards with switches, sliders,
	/// lists and buttons, in the theme and accent colour of DeskPals and in its language. Every change is saved and shown
	/// at once.
	/// </summary>
	internal sealed class SettingsForm : Form
	{
		internal static readonly (string id, string glyph)[] Pages =
		{
			("weather", ""), ("scenes", ""), ("effects", ""), ("sound", ""),
			("pause", ""), ("keys", ""), ("settings", ""), ("about", ""),
		};

		readonly NavBar nav;
		readonly PageView page;
		readonly StatusBar status;
		internal string Current = "weather";
		Config C => Controller.Config;

		public SettingsForm()
		{
			float z = DeviceDpi / 96f;
			Text = T.S("app");
			Size = new Size((int)(940 * z), (int)(700 * z)); MinimumSize = new Size((int)(760 * z), (int)(540 * z)); StartPosition = FormStartPosition.CenterScreen;
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			nav = new NavBar(this) { Dock = DockStyle.Left, Width = (int)(220 * z) };
			page = new PageView { Dock = DockStyle.Fill };
			status = new StatusBar { Dock = DockStyle.Bottom, Height = (int)(34 * z) };
			Controls.Add(page); Controls.Add(nav); Controls.Add(status);
			ApplyTheme();
			Theme.Changed += OnTheme;
			T.Changed += OnLanguage;
			Controller.Changed += OnState;
			FormClosed += (s, e) => { Theme.Changed -= OnTheme; T.Changed -= OnLanguage; Controller.Changed -= OnState; };
			Build();
			OnState();
		}

		void OnTheme() { if (!IsDisposed) BeginInvoke(() => { ApplyTheme(); Invalidate(true); }); }
		void OnLanguage() { if (!IsDisposed) BeginInvoke(() => { Text = T.S("app"); Build(); nav.Invalidate(); }); }

		void ApplyTheme()
		{
			BackColor = Theme.Background; ForeColor = Theme.Text;
			Theme.ApplyWindowChrome(this);
		}

		public void Show(string pageId) { if (Pages.Any(p => p.id == pageId)) { Current = pageId; Build(); nav.Invalidate(); } }

		void OnState()
		{
			if (IsDisposed) return;
			status.Text = StateText();
			status.Invalidate();
		}

		/// <summary>What shows now, in a line: "Snow 55% · fog 12% · sound", or why it is paused, or that it is starting.</summary>
		internal static string StateText()
		{
			var c = Controller.Config;
			var p = Controller.Now;
			string text;
			if (Controller.Problem == "standalone") return T.S("a.standalone");
			if (Controller.Problem == "noEngine") return T.S("a.noEngine");
			if (Controller.Testing) text = T.S("w.testing");
			else if (!c.On) text = T.S("w.off");
			else if (p.By == "fullscreen") text = T.S("p.paused.fullscreen", Controller.Front);
			else if (p.By == "battery") text = T.S("p.paused", T.S("p.battery"));
			else if (p.By != "") text = T.S(p.Sound && !p.Effects ? "p.paused.sound" : "p.paused.app", p.By);
			else if (Controller.Starting) text = T.S("w.starting");
			else
			{
				var s = Controller.Shown;
				var parts = new List<string>();
				if (s.kind is Kind k) parts.Add(T.S("k." + Controller.Name(k)) + " " + s.intensity + "%");
				if (s.fog > 0.01) parts.Add(T.S("e.fog").ToLower() + " " + Math.Round(s.fog * 100) + "%");
				if (s.sun > 0.01) parts.Add(T.S("c.sun").ToLower() + " " + Math.Round(s.sun * 100) + "%");
				if (s.grass > 0.01) parts.Add(T.S("c.grass").ToLower() + " " + Math.Round(s.grass * 100) + "%");
				if (s.sound) parts.Add(T.S("tab.sound").ToLower());
				if (Overlay.Lights && !p.Effects) parts.Add(T.S("c.lights").ToLower());
				text = parts.Count == 0 ? T.S("k.none") : string.Join(" · ", parts);
			}
			if (Controller.BehindFailed && c.On && p.By == "") text += "  ·  ⚠ " + T.S("w.rain.place.failed.short");
			if (Controller.Weather != null && c.Follow > 0) text += "  ·  " + c.City.Split(',')[0] + " " + Math.Round(Controller.Weather.Temp) + "°";
			return text;
		}

		/// <summary>Something changed: save it, show it, and draw the page again.</summary>
		void Changed() { Controller.Apply(); Build(); }

		// ------------------------------------------------------------------ the pages

		internal void Build()
		{
			var rows = new List<Row>();
			void Header(string key) => rows.Add(new Row { Header = true, Title = T.S(key) });
			void Hint(string text) => rows.Add(new Row { Hint = true, Desc = text });
			Row Toggle(string key, Func<bool> get, Action<bool> set) { var r = new Row { Title = T.S(key), Desc = Desc(key), GetOn = get, SetOn = v => { set(v); Changed(); } }; rows.Add(r); return r; }
			void Slider(string key, int min, int max, Func<int> get, Action<int> set) =>
				rows.Add(new Row { Title = T.S(key), Desc = Desc(key), Min = min, Max = max, GetValue = get, SetValue = set, Format = v => v + "%", Done = Changed });
			void Pick(string key, Func<string[]> items, Func<int> get, Action<int> set) =>
				rows.Add(new Row { Title = T.S(key), Desc = Desc(key), Items = items, GetIndex = get, SetIndex = i => { set(i); Changed(); } });
			Row Buttons(string title, string desc, params (string text, Action run)[] buttons) { var r = new Row { Title = title, Desc = desc }; r.Buttons.AddRange(buttons); rows.Add(r); return r; }

			switch (Current)
			{
				case "weather":
					Toggle("w.on", () => C.On, v => C.On = v);
					Pick("w.follow", () => new[] { T.S("w.follow.off"), T.S("w.follow.on"), T.S("w.follow.seasons"), T.S("w.follow.invert") }, () => C.Follow, v => C.Follow = v);
					Buttons(T.S("w.city"), C.City.Length > 0 ? C.City : T.S("w.cityNone"), (T.S("w.cityChoose"), async () => await AskCity()));
					if (Controller.Weather != null && C.Follow > 0) Hint(T.S("w.now", WeatherText(Controller.Weather)));
					Header("w.what");
					Pick("w.kind", () => new[] { T.S("k.none"), T.S("k.snow"), T.S("k.leaves"), T.S("k.rain"), T.S("k.blossom") }, () => (int)C.Kind + 1, v => C.Kind = (Kind)(v - 1));
					Slider("w.intensity", 5, 100, () => C.Intensity, v => C.Intensity = v);
					Toggle("w.windows", () => C.OnWindows, v => C.OnWindows = v);
					Pick("w.size", () => new[] { T.S("w.size.small"), T.S("w.size.medium"), T.S("w.size.large") }, () => C.FlakeSize, v => C.FlakeSize = v);
					Toggle("w.melt", () => C.Melt, v => C.Melt = v);
					Pick("w.rain.place", () => new[] { T.S("w.rain.place.0"), T.S("w.rain.place.1") }, () => Math.Clamp(C.RainPlace, 0, 1), v => C.RainPlace = v);
					if (C.RainPlace == 1 && Controller.BehindFailed) Hint("⚠  " + T.S("w.rain.place.failed"));
					Buttons(T.S("w.test"), T.S("w.test.desc"), (T.S("w.test.button"), Controller.Test));
					Buttons(T.S("w.clear"), T.S("w.clear.desc"), (T.S("w.clear.button"), Engine.Clear));
					break;
				case "scenes":
					Hint(T.S("sc.hint"));
					Header("sc.builtIn");
					foreach (var (key, scene) in Scene.BuiltIn)
					{
						var r = Buttons(T.S(key), Summary(scene), (T.S("sc.use"), () => { scene.ApplyTo(C); Changed(); }));
						r.Thumb = (g, box) => SceneArt.Draw(g, box, scene, C);
						r.Badge = scene.SameAs(C) ? T.S("sc.inUse") : null;
					}
					Header("sc.mine");
					Buttons(T.S("sc.save"), T.S("sc.save.desc"), (T.S("sc.save.button"), SaveScene));
					Buttons(T.S("sc.import"), T.S("sc.import.desc"), (T.S("sc.paste"), PasteScene), (T.S("sc.open"), OpenScene));
					foreach (var scene in C.Scenes.ToList())
					{
						var r = Buttons(scene.Name, Summary(scene),
							(T.S("sc.use"), () => { scene.ApplyTo(C); Changed(); }),
							(T.S("sc.copy"), () => { Clipboard.SetText(scene.ToCode()); status.Flash(T.S("sc.copied")); }),
							(T.S("sc.file"), () => SaveSceneFile(scene)),
							(T.S("sc.delete"), () => { C.Scenes.Remove(scene); Changed(); }));
						r.Thumb = (g, box) => SceneArt.Draw(g, box, scene, C);
						r.Badge = scene.SameAs(C) ? T.S("sc.inUse") : null;
					}
					break;
				case "effects":
					Hint(T.S("e.hint"));
					Slider("e.fog", 0, 100, () => C.Fog, v => C.Fog = v);
					Slider("e.sun", 0, 100, () => C.Sun, v => C.Sun = v);
					Slider("e.grass", 0, 100, () => C.Grass, v => C.Grass = v);
					Slider("e.wind", 0, 100, () => C.Wind, v => C.Wind = v);
					Toggle("e.random", () => C.Random, v => C.Random = v);
					Header("e.where");
					Toggle("e.onTop", () => C.OnTop, v => C.OnTop = v);
					Header("e.lights.header");
					Toggle("e.lights", () => C.Lights, v => C.Lights = v);
					Toggle("e.lights.december", () => C.LightsDecember, v => C.LightsDecember = v);
					Buttons(T.S("e.lights.colors"), T.S("e.lights.colors.desc"), (T.S("e.lights.colors.button"), () => { using var d = new LightsDialog(); d.ShowDialog(this); Build(); }));
					Header("e.colors");
					Hint(T.S("e.colors.hint"));
					foreach (var effect in new[] { "snow", "leaves", "rain", "blossom", "fog", "sun", "grass" })
					{
						var r = Buttons(T.S("c." + effect), C.ColorOf(effect) is Color col ? "#" + col.R.ToString("X2") + col.G.ToString("X2") + col.B.ToString("X2") : T.S("c.own"),
							(T.S("c.choose"), () => ChooseColor(effect)));
						if (C.ColorOf(effect) != null) r.Buttons.Add((T.S("c.reset"), () => { C.SetColor(effect, null); Changed(); }));
						r.Swatch = () => C.ColorOf(effect);
					}
					break;
				case "sound":
					Toggle("s.on", () => C.Sound, v => C.Sound = v);
					Slider("s.volume", 0, 100, () => C.Volume, v => C.Volume = v);
					Toggle("s.calls", () => C.QuietOnCalls, v => C.QuietOnCalls = v);
					break;
				case "pause":
					Toggle("p.fullscreen", () => C.PauseFullscreen, v => C.PauseFullscreen = v);
					Pick("p.battery", () => new[] { T.S("p.battery.same"), T.S("p.battery.less"), T.S("p.battery.pause") }, () => C.Battery, v => C.Battery = v);
					Header("p.apps.header");
					Buttons(T.S("p.apps"), T.S("p.apps.desc"), (T.S("p.add"), AddApp));
					foreach (var app in C.PauseApps.ToList())
					{
						var r = new Row
						{
							Title = app, Desc = T.S("p.mode"),
							Items = () => new[] { T.S("p.mode.all"), T.S("p.mode.effects"), T.S("p.mode.sound") },
							GetIndex = () => C.PauseModeOf(app), SetIndex = i => { C.PauseMode[app] = i; Changed(); },
						};
						r.Buttons.Add((T.S("p.remove"), () => { C.PauseApps.Remove(app); C.PauseMode.Remove(app); Changed(); }));
						rows.Add(r);
					}
					break;
				case "keys":
					string[] Keys() => new[] { T.S("key.none") }.Concat(Hotkeys.Choices.Select(x => x.text)).ToArray();
					Pick("key.toggle", Keys, () => C.KeyToggle + 1, v => C.KeyToggle = v - 1);
					Pick("key.settings", Keys, () => C.KeySettings + 1, v => C.KeySettings = v - 1);
					if (Hotkeys.Busy.Length > 0) Hint(T.S("key.busy") + " " + Hotkeys.Busy);
					break;
				case "settings":
					// the look of this window: its theme and accent colour (or those of DeskPals), its language
					string[] themes = { "", "auto", "light", "dark" };
					Pick("set.theme", () => new[] { T.S("set.theme.deskpals"), T.S("set.theme.auto"), T.S("set.theme.light"), T.S("set.theme.dark") },
						() => Math.Max(0, Array.IndexOf(themes, C.Theme ?? "")), i => { C.Theme = themes[i]; DeskPals.ApplyTheme(C); });
					rows.Add(new Row
					{
						Title = T.S("set.accent"), Desc = T.S("set.accent.desc"),
						Palette = new Color?[] { null }.Concat(Theme.Accents.Select(a => (Color?)a.Color)).ToArray(),
						GetIndex = () => string.IsNullOrEmpty(C.Accent) ? 0 : Array.FindIndex(Theme.Accents, a => a.Id == C.Accent) + 1,
						SetIndex = i => { C.Accent = i <= 0 ? "" : Theme.Accents[i - 1].Id; DeskPals.ApplyTheme(C); Changed(); },
					});
					Pick("a.lang", () => new[] { T.S("a.lang.auto") }.Concat(T.Languages.Select(l => l.name)).ToArray(),
						() => C.Lang == "" ? 0 : Array.FindIndex(T.Languages, l => l.code == C.Lang) + 1,
						i => C.Lang = i <= 0 ? "" : T.Languages[i - 1].code);
					Header("set.privacy");
					Toggle("e.shots", () => C.InScreenshots, v => C.InScreenshots = v);
					break;
				default:
					Hint(T.S("a.text"));
					Buttons(T.S("a.snowhush"), T.S("a.snowhush.desc"),
						(T.S("a.project"), () => Open("https://github.com/mbezruchko/Snowhush")),
						(T.S("a.license"), () => Open(Path.Combine(AppContext.BaseDirectory, "Snowhush-LICENSE.txt"))));
					Hint(T.S("a.version", Application.ProductVersion.Split('+')[0]));
					break;
			}
			page.SetRows(rows);
		}

		/// <summary>The description of an option ("key.desc"), when it has one.</summary>
		static string Desc(string key) => T.Has(key + ".desc") ? T.S(key + ".desc") : null;

		static string WeatherText(Forecast.Now w)
		{
			string sky = Forecast.Snow(w.Code) ? T.S("k.snow") : Forecast.Rain(w.Code) ? T.S("k.rain") : Forecast.Fog(w.Code) ? T.S("e.fog") : w.Clouds > 60 ? T.S("w.cloudy") : T.S("w.clear.sky");
			return sky + ", " + Math.Round(w.Temp) + "°C, " + T.S("e.wind").ToLowerInvariant() + " " + Math.Round(w.Wind) + " km/h";
		}

		static string Summary(Scene s)
		{
			var parts = new List<string> { s.Kind == Kind.None ? T.S("k.none") : T.S("k." + Controller.Name(s.Kind)) + " " + s.Intensity + "%" };
			if (s.Fog > 0) parts.Add(T.S("e.fog") + " " + s.Fog + "%");
			if (s.Sun > 0) parts.Add(T.S("e.sun") + " " + s.Sun + "%");
			if (s.Grass > 0) parts.Add(T.S("c.grass") + " " + s.Grass + "%");
			parts.Add(T.S("e.wind") + " " + s.Wind + "%");
			if (s.Sound) parts.Add(T.S("tab.sound"));
			if (s.Lights) parts.Add(T.S("c.lights"));
			if (s.Colors?.Count > 0) parts.Add(T.S("e.colors"));
			return string.Join(" · ", parts);
		}

		static void Open(string target)
		{
			try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch (Exception) { }
		}

		async Task AskCity()
		{
			string name = Ask.Text(this, T.S("w.city"), T.S("w.cityAsk"), C.City.Split(',')[0]);
			if (string.IsNullOrWhiteSpace(name)) return;
			try
			{
				var place = await Forecast.Find(name);
				if (place == null) { Ask.Say(this, T.S("w.cityNotFound")); return; }
				C.City = place.Value.name; C.Lat = place.Value.lat; C.Lon = place.Value.lon;
				if (C.Follow == 0) C.Follow = 1;
				Changed();
			}
			catch (Exception ex) { Ask.Say(this, ex.Message); }
		}

		void ChooseColor(string effect)
		{
			using var d = new ColorDialog { FullOpen = true, Color = C.ColorOf(effect) ?? Color.White };
			if (d.ShowDialog(this) != DialogResult.OK) return;
			C.SetColor(effect, d.Color);
			Changed();
		}

		void AddApp()
		{
			var menu = new ContextMenuStrip();
			Theme.ApplyTo(menu);
			foreach (var app in OpenPrograms().Where(a => !C.PauseApps.Contains(a)))
				menu.Items.Add(app, null, (o, a) => { C.PauseApps.Add(app); Changed(); });
			if (menu.Items.Count == 0) menu.Items.Add(T.S("p.noneOpen")).Enabled = false;
			menu.Show(Cursor.Position);
		}

		static List<string> OpenPrograms()
		{
			var names = new HashSet<string>();
			foreach (var (h, _) in DesktopWindows.List())
				try
				{
					[System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hw, out uint pid);
					GetWindowThreadProcessId(h, out uint pid);
					using var p = Process.GetProcessById((int)pid);
					names.Add(p.ProcessName.ToLowerInvariant());
				}
				catch (Exception) { }
			return names.OrderBy(n => n).ToList();
		}

		// ------------------------------------------------------------------ scenes: save, send, bring

		void SaveScene()
		{
			string name = Ask.Text(this, T.S("sc.save"), T.S("sc.name"), T.S("sc.name.default", C.Scenes.Count + 1));
			if (string.IsNullOrWhiteSpace(name)) return;
			C.Scenes.RemoveAll(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
			C.Scenes.Add(Scene.From(C, name.Trim()));
			Changed();
		}

		void PasteScene()
		{
			string text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
			if (!text.Contains(Scene.CodePrefix)) text = Ask.Text(this, T.S("sc.import"), T.S("sc.paste.ask"), "");
			AddScene(Scene.Parse(text));
		}

		void OpenScene()
		{
			using var d = new OpenFileDialog { Filter = T.S("sc.filter") + "|*" + Scene.FileExtension + ";*.json" };
			if (d.ShowDialog(this) != DialogResult.OK) return;
			try { AddScene(Scene.Parse(File.ReadAllText(d.FileName))); } catch (Exception ex) { Ask.Say(this, ex.Message); }
		}

		void AddScene(Scene s)
		{
			if (s == null) { Ask.Say(this, T.S("sc.bad")); return; }
			if (s.Name.Length == 0) s.Name = T.S("sc.name.default", C.Scenes.Count + 1);
			C.Scenes.RemoveAll(x => x.Name.Equals(s.Name, StringComparison.OrdinalIgnoreCase));
			C.Scenes.Add(s);
			Changed();
			status.Flash(T.S("sc.added", s.Name));
		}

		void SaveSceneFile(Scene s)
		{
			string safe = string.Concat(s.Name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
			using var d = new SaveFileDialog { FileName = safe + Scene.FileExtension, Filter = T.S("sc.filter") + "|*" + Scene.FileExtension };
			if (d.ShowDialog(this) != DialogResult.OK) return;
			try { File.WriteAllText(d.FileName, System.Text.Json.JsonSerializer.Serialize(s, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); }
			catch (Exception ex) { Ask.Say(this, ex.Message); }
		}

		// ------------------------------------------------------------------ the list of sections

		internal sealed class NavBar : Control
		{
			readonly SettingsForm form;
			int hover = -1;
			public NavBar(SettingsForm f) { form = f; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); }

			float Z => DeviceDpi / 96f;
			Rectangle Item(int i) => new((int)(10 * Z), (int)((86 + i * 44) * Z), Width - (int)(20 * Z), (int)(38 * Z));

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Surface);
				using var title = Theme.CreateFont(17 * Z, FontStyle.Bold);
				using var text = Theme.CreateFont(14 * Z);
				using var icons = new Font(IconFont, 12 * Z, GraphicsUnit.Pixel);
				try { using var ico = new Icon(form.Icon, (int)(32 * Z), (int)(32 * Z)); g.DrawIcon(ico, new Rectangle((int)(16 * Z), (int)(22 * Z), (int)(32 * Z), (int)(32 * Z))); } catch (Exception) { }
				Theme.DrawText(g, "DeskPals", text, new Rectangle((int)(56 * Z), (int)(18 * Z), Width, (int)(20 * Z)), Theme.TextMuted, TextFormatFlags.SingleLine);
				Theme.DrawText(g, T.S("app.short"), title, new Rectangle((int)(56 * Z), (int)(36 * Z), Width - (int)(60 * Z), (int)(24 * Z)), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				for (int i = 0; i < Pages.Length; i++)
				{
					var r = Item(i);
					bool on = Pages[i].id == form.Current;
					if (on) Theme.FillRounded(g, Theme.AccentSoft, r, 8);
					else if (i == hover) Theme.FillRounded(g, Theme.Hover, r, 8);
					if (on) Theme.FillRounded(g, Theme.Accent, new RectangleF(r.X, r.Y + r.Height * 0.25f, 3 * Z, r.Height * 0.5f), 1.5f * Z);
					TextRenderer.DrawText(g, Pages[i].glyph, icons, new Rectangle(r.X + (int)(12 * Z), r.Y, (int)(24 * Z), r.Height), on ? Theme.Accent : Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
					Theme.DrawText(g, T.S("tab." + Pages[i].id), text, new Rectangle(r.X + (int)(44 * Z), r.Y, r.Width - (int)(48 * Z), r.Height), on ? Theme.Accent : Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				}
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				int h = Enumerable.Range(0, Pages.Length).FirstOrDefault(i => Item(i).Contains(e.Location), -1);
				Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
				if (h != hover) { hover = h; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

			protected override void OnMouseClick(MouseEventArgs e)
			{
				int i = Enumerable.Range(0, Pages.Length).FirstOrDefault(i => Item(i).Contains(e.Location), -1);
				if (i >= 0) { form.Current = Pages[i].id; form.Build(); Invalidate(); }
			}
		}

		internal static readonly string IconFont = new System.Drawing.Text.InstalledFontCollection().Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

		internal sealed class StatusBar : Control
		{
			string flash;
			readonly System.Windows.Forms.Timer clear = new() { Interval = 3000 };
			public StatusBar()
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				clear.Tick += (s, e) => { clear.Stop(); flash = null; Invalidate(); };
			}

			public void Flash(string text) { flash = text; clear.Stop(); clear.Start(); Invalidate(); }

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Surface);
				using var p = new Pen(Theme.Border);
				g.DrawLine(p, 0, 0, Width, 0);
				float z = DeviceDpi / 96f;
				using var f = Theme.CreateFont(13 * z);
				Theme.DrawText(g, flash ?? Text, f, new Rectangle((int)(16 * z), 0, Width - (int)(32 * z), Height), flash != null ? Theme.Accent : Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			}
		}
	}

	/// <summary>A small question in the theme of DeskPals: a line of text to type, or a message.</summary>
	internal static class Ask
	{
		public static string Text(IWin32Window owner, string title, string prompt, string value)
		{
			using var f = Dialog(title, prompt, out var panel, out var ok);
			var box = new TextBox { Text = value ?? "", Dock = DockStyle.Top, BackColor = Theme.SurfaceAlt, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = Theme.CreateFont(15) };
			panel.Controls.Add(box);
			box.BringToFront();
			f.Shown += (s, e) => { box.Focus(); box.SelectAll(); };
			return f.ShowDialog(owner) == DialogResult.OK ? box.Text : null;
		}

		public static void Say(IWin32Window owner, string text)
		{
			using var f = Dialog(T.S("app"), text, out _, out var ok);
			foreach (var b in ok.Parent.Controls.OfType<Button>().Where(b => b != ok).ToList()) b.Visible = false;
			f.ShowDialog(owner);
		}

		static Form Dialog(string title, string prompt, out Panel panel, out Button ok)
		{
			var f = new Form
			{
				Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false,
				StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(440, 170), BackColor = Theme.Background, ForeColor = Theme.Text, Font = Theme.CreateFont(14),
				AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new SizeF(96, 96),
			};
			Theme.ApplyWindowChrome(f);
			panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 16, 18, 8) };
			var label = new Label { Text = prompt, Dock = DockStyle.Top, AutoSize = false, Height = 52 };
			panel.Controls.Add(label);
			var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 52, Padding = new Padding(12, 8, 12, 8), BackColor = Theme.Surface };
			ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 100, Height = 32, FlatStyle = FlatStyle.Flat };
			Theme.MakePrimary(ok);
			var cancel = new Button { Text = T.S("ask.cancel"), DialogResult = DialogResult.Cancel, Width = 100, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text };
			cancel.FlatAppearance.BorderColor = Theme.Border;
			buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
			f.Controls.Add(panel); f.Controls.Add(buttons);
			f.AcceptButton = ok; f.CancelButton = cancel;
			return f;
		}
	}
}
