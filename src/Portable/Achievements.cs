using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== achievements

	/// <summary>
	/// Little trophies for things done with the pets: the first toss, focus sessions, reminders done, pets adopted, days
	/// together… Each is won once and kept forever (with its date). A new one comes down from the top edge of the screen
	/// (a trophy and its name), stays 8 seconds and goes up again; during quiet time it waits. The Achievements page of the
	/// settings shows the ones won and the progress of the others. Counters and dates live in the settings file:
	/// "AchN.&lt;counter&gt;" and "Ach.&lt;id&gt;" (the day it was won).
	/// </summary>
	public static class Achievements
	{
		public enum Group { Start, Pets, Work, Fun, Time, Secret }

		public sealed class Achievement
		{
			public string Id;
			public Group Group;
			/// <summary>The counter that wins it ("" = won directly with <see cref="Unlock"/>).</summary>
			public string Counter = "";
			public int Goal = 1;
			/// <summary>The counter counts minutes, the texts say hours (Goal 600 = "10 hours").</summary>
			public int Scale = 1;
			/// <summary>Hidden until won: name and description are a surprise.</summary>
			public bool Secret;
			public string Title => Lang.T("ach." + Id + ".title");
			public string Description => Lang.T("ach." + Id + ".desc", Goal / Scale);
		}

		static Achievement A(string id, Group g, string counter = "", int goal = 1, int scale = 1, bool secret = false) =>
			new Achievement { Id = id, Group = g, Counter = counter, Goal = goal, Scale = scale, Secret = secret };

		/// <summary>Every achievement, by group, in the order of the page. "legend" (all the others) is the last.</summary>
		public static readonly Achievement[] All =
		{
			// first steps: finding the parts of the program
			A("toss1", Group.Start, "toss"),
			A("explorer", Group.Start),
			A("ai", Group.Start),
			A("voice", Group.Start),
			A("language", Group.Start),
			A("hide", Group.Start),
			A("backup", Group.Start),
			A("install", Group.Start),
			A("extension", Group.Start),
			A("editor", Group.Start),
			A("calendar", Group.Start),
			A("weather", Group.Start),
			// the pets
			A("adopt1", Group.Pets, "adopt"),
			A("adopt5", Group.Pets, "adopt", 5),
			A("adopt10", Group.Pets, "adopt", 10),
			A("adopt25", Group.Pets, "adopt", 25),
			A("adoptAll", Group.Pets),
			A("crowd3", Group.Pets, "crowd", 3),
			A("crowd5", Group.Pets, "crowd", 5),
			A("toss50", Group.Pets, "toss", 50),
			A("toss500", Group.Pets, "toss", 500),
			A("bigToss", Group.Pets),
			A("care10", Group.Pets, "care", 10),
			A("care100", Group.Pets, "care", 100),
			A("hat", Group.Pets),
			A("level5", Group.Pets, "level", 5),
			A("level10", Group.Pets, "level", 10),
			// getting things done
			A("focus1", Group.Work, "focus"),
			A("focus25", Group.Work, "focus", 25),
			A("focus100", Group.Work, "focus", 100),
			A("focusDays", Group.Work, "focusDays", 5),
			A("reminderNew", Group.Work),
			A("reminder1", Group.Work, "reminder"),
			A("reminder50", Group.Work, "reminder", 50),
			A("tasks10", Group.Work, "task", 10),
			A("tasks100", Group.Work, "task", 100),
			A("agent1", Group.Work, "agent"),
			A("agent25", Group.Work, "agent", 25),
			A("timer", Group.Work),
			A("tool1", Group.Work, "tool"),
			A("tool25", Group.Work, "tool", 25),
			// what was done with DeskPals Tools (read from its history: in the pet or in its own window)
			A("toolEdit", Group.Work, "tools.view", 10),
			A("toolAsk", Group.Work, "tools.askai", 5),
			A("toolTranslate", Group.Work, "tools.translate"),
			A("toolCompare", Group.Work, "tools.compare"),
			A("toolSubs", Group.Work, "tools.subtitles"),
			A("toolKinds", Group.Work, "tools.kinds", 10),
			// fun
			A("chat1", Group.Fun, "chat"),
			A("chat100", Group.Fun, "chat", 100),
			A("music1", Group.Fun, "music"),
			A("music100", Group.Fun, "music", 100),
			A("fun1", Group.Fun, "fun"),
			A("visitor", Group.Fun),
			A("snow", Group.Fun),
			A("mood7", Group.Fun, "mood", 7),
			// time together
			A("hours1", Group.Time, "minutes", 60, 60),
			A("hours10", Group.Time, "minutes", 600, 60),
			A("hours100", Group.Time, "minutes", 6000, 60),
			A("hours500", Group.Time, "minutes", 30000, 60),
			A("days7", Group.Time, "days", 7),
			A("days30", Group.Time, "days", 30),
			A("days100", Group.Time, "days", 100),
			A("days365", Group.Time, "days", 365),
			A("streak7", Group.Time, "streak", 7),
			// secret
			A("night", Group.Secret, secret: true),
			A("early", Group.Secret, secret: true),
			A("weekend", Group.Secret, secret: true),
			A("newYear", Group.Secret, secret: true),
			A("christmas", Group.Secret, secret: true),
			A("legend", Group.Secret),
		};

		/// <summary>The settings pages that count for "explorer" (every page of the sidebar).</summary>
		static readonly string[] Pages = { "Home", "Pets", "Reminders", "Ai", "Plugins", "Achievements", "Integrations", "Extensions", "Application", "About" };
		public static Achievement Find(string id) => All.FirstOrDefault(a => a.Id == id);

		/// <summary>Show the trophy at the top of the screen when one is won (on by default).</summary>
		public static bool ShowToast
		{
			get => Program.MyData.GetSettingBool("AchToast", true);
			set => Program.MyData.SetSetting("AchToast", value);
		}

		/// <summary>Raised after an achievement is won (the settings page repaints).</summary>
		public static event Action Changed;

		public static int Value(string counter) => string.IsNullOrEmpty(counter) ? 0 : Math.Max(0, Program.MyData.GetSettingInt("AchN." + counter, 0));

		public static bool IsWon(string id) => WonOn(id) != null;

		/// <summary>The day it was won, or null.</summary>
		public static DateTime? WonOn(string id)
		{
			string s = Program.MyData.GetSetting("Ach." + id);
			return DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;
		}

		public static int WonCount => All.Count(a => IsWon(a.Id));

		/// <summary>Progress of one (0…Goal).</summary>
		public static int Progress(Achievement a)
		{
			if (IsWon(a.Id)) return a.Goal;
			if (a.Id == "legend" || a.Counter.Length == 0) return 0;
			return Math.Min(a.Goal, Value(a.Counter));
		}

		/// <summary>Something happened n more times (a toss, a focus finished…).</summary>
		public static void Count(string counter, int n = 1)
		{
			if (n <= 0 || string.IsNullOrEmpty(counter)) return;
			try { Set(counter, Value(counter) + n); }
			catch (Exception skipped) { LogFile.Ignored(skipped); }
		}

		/// <summary>A counter that is a highest value (pets on screen at once, the level): only goes up.</summary>
		public static void AtLeast(string counter, int value)
		{
			if (value > Value(counter)) Set(counter, value);
		}

		/// <summary>Once a day at most (days with a focus, days together).</summary>
		public static void CountDay(string counter)
		{
			string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			if (Program.MyData.GetSetting("AchD." + counter) == today) return;
			Program.MyData.SetSetting("AchD." + counter, today);
			Count(counter);
		}

		/// <summary>A pet adopted from the gallery: each pet counts once.</summary>
		public static void Adopted(string folder)
		{
			if (string.IsNullOrEmpty(folder)) return;
			var seen = Program.MyData.GetSetting("AchAdopted").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			if (seen.Contains(folder, StringComparer.OrdinalIgnoreCase)) return;
			seen.Add(folder);
			Program.MyData.SetSetting("AchAdopted", string.Join("|", seen));
			Count("adopt");
			CheckAdoptAll();
		}

		/// <summary>Days in a row: yesterday too → one more, otherwise it starts again at 1 (the best run counts).</summary>
		static void Streak()
		{
			string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			string last = Program.MyData.GetSetting("AchStreakDay");
			if (last == today) return;
			int run = last == DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ? Program.MyData.GetSettingInt("AchStreakRun", 0) + 1 : 1;
			Program.MyData.SetSetting("AchStreakDay", today);
			Program.MyData.SetSetting("AchStreakRun", run);
			AtLeast("streak", run);
		}

		/// <summary>A settings page was opened: all of them → "explorer".</summary>
		public static void PageSeen(string page)
		{
			if (IsWon("explorer") || Array.IndexOf(Pages, page) < 0) return;
			var seen = Program.MyData.GetSetting("AchPages").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			if (seen.Contains(page)) return;
			seen.Add(page);
			Program.MyData.SetSetting("AchPages", string.Join("|", seen));
			if (Pages.All(seen.Contains)) Unlock("explorer");
		}

		/// <summary>The pets of the gallery (when its list loads): adopting every one of them → "adoptAll".</summary>
		/// <remarks>Each card is its folders: one, or the colours of a group (any colour adopted counts for the card).</remarks>
		public static void GalleryPets(IEnumerable<string[]> cards)
		{
			var list = cards?.Select(c => c.Where(f => !string.IsNullOrEmpty(f)).ToArray()).Where(c => c.Length > 0).Select(c => string.Join(",", c)).Distinct().ToList();
			if (list == null || list.Count < 5) return;   // a partial list (no internet, still loading) does not count
			Program.MyData.SetSetting("AchGallery", string.Join("|", list));
			CheckAdoptAll();
		}

		static void CheckAdoptAll()
		{
			if (IsWon("adoptAll")) return;
			var gallery = Program.MyData.GetSetting("AchGallery").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (gallery.Length < 5) return;
			var adopted = new HashSet<string>(Program.MyData.GetSetting("AchAdopted").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
			if (gallery.All(card => card.Split(',').Any(adopted.Contains))) Unlock("adoptAll");
		}

		/// <summary>How many cards of the gallery were adopted, and how many there are (for the progress of "adoptAll").</summary>
		public static (int done, int total) GalleryProgress()
		{
			var gallery = Program.MyData.GetSetting("AchGallery").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			var adopted = new HashSet<string>(Program.MyData.GetSetting("AchAdopted").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
			return (gallery.Count(card => card.Split(',').Any(adopted.Contains)), gallery.Length);
		}

		/// <summary>The Pet Editor has a project in its recent list (a pet made or worked on).</summary>
		static bool EditorUsed()
		{
			try
			{
				string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopPet", "PetEditor.json");
				if (!File.Exists(file)) return false;
				var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file));
				return node?["Recent"] is System.Text.Json.Nodes.JsonArray recent && recent.Count > 0;
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); return false; }
		}

		static void Set(string counter, int value)
		{
			Program.MyData.SetSetting("AchN." + counter, value);
			foreach (var a in All.Where(x => x.Counter == counter && value >= x.Goal)) Unlock(a.Id);
		}

		/// <summary>Win it now (once): the trophy comes down from the top of the screen.</summary>
		public static void Unlock(string id)
		{
			var a = Find(id);
			if (a == null || IsWon(id)) return;
			Program.MyData.SetSetting("Ach." + id, DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			LogFile.Important("achievement: " + id);
			Announce(a);
			CommunityPlugins.Event("achievement");
			// all the others won: the last one
			if (id != "legend" && All.Where(x => x.Id != "legend").All(x => IsWon(x.Id))) Unlock("legend");
			Changed?.Invoke();
		}

		// ------------------------------------------------------------------ the clock: days together, night owl

		static Timer timer;

		public static void Start()
		{
			if (timer != null) return;
			timer = new Timer { Interval = 60000 };
			timer.Tick += (s, e) => Tick();
			Lang.Changed += () => { if (!SelfTest.Running) Unlock("language"); };
			timer.Start();
			// a moment after the start (pets on screen)
			var first = new Timer { Interval = 20000 };
			first.Tick += (s, e) => { first.Stop(); first.Dispose(); Tick(); };
			first.Start();
		}

		static DateTime toolsSeen;
		
		/// <summary>
		/// DeskPals Tools' history (its last jobs): how many times each job was done and how many kinds of jobs, for the
		/// achievements of the tools. Read again only when it changed.
		/// </summary>
		internal static void ToolsHistory(string file = null)
		{
			try
			{
				file ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "extensions", "tools-history.json");
				if (!File.Exists(file)) return;
				var when = File.GetLastWriteTimeUtc(file);
				if (when == toolsSeen) return;
				toolsSeen = when;
				var jobs = new List<string>();
				foreach (var e in System.Text.Json.JsonDocument.Parse(File.ReadAllText(file)).RootElement.EnumerateArray())
					if (e.TryGetProperty("job", out var j) && j.GetString() is string id && id.Length > 0) jobs.Add(id.StartsWith("recipe-") ? "recipe" : id);
				foreach (var g in jobs.GroupBy(x => x)) AtLeast("tools." + g.Key, g.Count());
				AtLeast("tools.kinds", jobs.Distinct().Count());
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
		}
		
		internal static void Tick()
		{
			if (SelfTest.Running && !SelfTest.Live) return;
			try
			{
				bool active = Activity.IdleSeconds() < 120;
				int pets = Application.OpenForms.OfType<FormPet>().Count(p => p.Visible && !p.IsChildWindow && !p.Dying);
				if (active) { CountDay("days"); Streak(); }
				// time together: a minute at the computer with a pet on screen
				if (active && pets > 0 && !HideHotkey.Hidden) Count("minutes");
				AtLeast("crowd", pets);
				if (PetLevel.Enabled) AtLeast("level", PetLevel.Level);
				var now = DateTime.Now;
				if (active && pets > 0)
				{
					if (now.Hour < 4) Unlock("night");
					else if (now.Hour < 6) Unlock("early");
					if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) Unlock("weekend");
					if (now.Month == 1 && now.Day == 1) Unlock("newYear");
					if (now.Month == 12 && now.Day == 25) Unlock("christmas");
				}
				ToolsHistory();
				// things set up somewhere else (looked at once a minute, only until won)
				if (!IsWon("ai") && AiAssistant.Configured) Unlock("ai");
				if (!IsWon("calendar") && Agenda.Configured) Unlock("calendar");
				if (!IsWon("weather") && Weather.Enabled && Weather.City != null) Unlock("weather");
				if (!IsWon("install") && Program.IsApplicationInstalled()) Unlock("install");
				if (!IsWon("extension") && (WeatherExtension.Installed() != null || ToolsExtension.Installed() != null)) Unlock("extension");
				if (!IsWon("editor") && EditorUsed()) Unlock("editor");
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
		}

		// ------------------------------------------------------------------ the notice

		static readonly Queue<Achievement> waiting = new Queue<Achievement>();
		static Toast showing;
		static Timer waitTimer;

		/// <summary>Self-test: the trophy on screen now (or null).</summary>
		internal static Form Showing => showing != null && !showing.IsDisposed ? showing : null;

		static void Announce(Achievement a)
		{
			if (!ShowToast) { SpeechBubble.Notify(Lang.T("ach.bubble", a.Title), 6); return; }
			waiting.Enqueue(a);
			Program.Mainthread?.ReactAll("cheer", "happy", "jump", "dance");
			ShowNext();
		}

		static void ShowNext()
		{
			if (showing != null && !showing.IsDisposed) return;
			if (waiting.Count == 0) { waitTimer?.Stop(); return; }
			// quiet time (focus, pets hidden, full screen, do not disturb): it waits, then comes
			if (SpeechBubble.QuietNow && !(SelfTest.Running))
			{
				if (waitTimer == null) { waitTimer = new Timer { Interval = 5000 }; waitTimer.Tick += (s, e) => ShowNext(); }
				waitTimer.Start();
				return;
			}
			waitTimer?.Stop();
			showing = new Toast(waiting.Dequeue());
			showing.FormClosed += (s, e) => { showing = null; ShowNext(); };
			showing.Come();
		}

		/// <summary>The "See how it looks" button: the last one won (or the first one) comes down, nothing is saved.</summary>
		public static void Preview()
		{
			if (showing != null && !showing.IsDisposed) return;
			var a = All.Where(x => IsWon(x.Id)).OrderByDescending(x => WonOn(x.Id)).FirstOrDefault() ?? All[0];
			showing = new Toast(a);
			showing.FormClosed += (s, e) => { showing = null; ShowNext(); };
			showing.Come();
		}

		/// <summary>Self-test: no more notices waiting, and the one on screen goes away.</summary>
		internal static void TestClearQueue() { waiting.Clear(); if (showing != null && !showing.IsDisposed) showing.Close(); }

		/// <summary>Self-test: forget an achievement (to win it again).</summary>
		internal static void TestReset(string id) => Program.MyData.SetSetting("Ach." + id, "");
		internal static void TestSetCounter(string counter, int value) => Program.MyData.SetSetting("AchN." + counter, value);

		// ------------------------------------------------------------------ drawing a trophy

		static readonly Color GoldLight = Color.FromArgb(255, 226, 120), GoldDark = Color.FromArgb(214, 146, 28), GoldEdge = Color.FromArgb(150, 96, 10);

		/// <summary>A golden cup with a star (grey when not won yet).</summary>
		public static void DrawTrophy(Graphics g, RectangleF r, bool won, float shine = -1)
		{
			var old = g.SmoothingMode;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			float s = Math.Min(r.Width, r.Height);
			float x = r.X + (r.Width - s) / 2, y = r.Y + (r.Height - s) / 2;
			PointF P(float px, float py) => new PointF(x + px * s, y + py * s);
			Color light = won ? GoldLight : Color.FromArgb(200, 202, 208), dark = won ? GoldDark : Color.FromArgb(140, 144, 152), edge = won ? GoldEdge : Color.FromArgb(105, 108, 116);
			using (var cup = new GraphicsPath())
			{
				// bowl: flat top, round bottom
				cup.AddLine(P(0.22f, 0.12f), P(0.78f, 0.12f));
				cup.AddBezier(P(0.78f, 0.12f), P(0.78f, 0.46f), P(0.66f, 0.58f), P(0.5f, 0.58f));
				cup.AddBezier(P(0.5f, 0.58f), P(0.34f, 0.58f), P(0.22f, 0.46f), P(0.22f, 0.12f));
				cup.CloseFigure();
				using (var pen = new Pen(edge, Math.Max(1.5f, s * 0.05f)))
				{
					// handles
					g.DrawArc(pen, x + 0.08f * s, y + 0.17f * s, 0.2f * s, 0.22f * s, 90, 180);
					g.DrawArc(pen, x + 0.72f * s, y + 0.17f * s, 0.2f * s, 0.22f * s, 270, 180);
				}
				using (var b = new LinearGradientBrush(new RectangleF(x, y, s, s), light, dark, 35f)) g.FillPath(b, cup);
				using (var pen = new Pen(edge, Math.Max(1f, s * 0.025f))) g.DrawPath(pen, cup);
				// stem and base
				using (var b = new LinearGradientBrush(new RectangleF(x, y + 0.55f * s, s, 0.4f * s), light, dark, 90f))
				{
					g.FillRectangle(b, x + 0.45f * s, y + 0.57f * s, 0.1f * s, 0.16f * s);
					using (var basePath = Rounded(new RectangleF(x + 0.3f * s, y + 0.72f * s, 0.4f * s, 0.08f * s), 0.03f * s)) g.FillPath(b, basePath);
					using (var basePath = Rounded(new RectangleF(x + 0.24f * s, y + 0.8f * s, 0.52f * s, 0.1f * s), 0.03f * s)) { g.FillPath(b, basePath); using (var pen = new Pen(edge, Math.Max(1f, s * 0.02f))) g.DrawPath(pen, basePath); }
				}
				// star on the bowl
				using (var star = Star(P(0.5f, 0.32f), 0.13f * s, 0.055f * s))
				using (var b = new SolidBrush(won ? Color.FromArgb(255, 250, 225) : Color.FromArgb(235, 236, 240))) g.FillPath(b, star);
				// a light sweeping across (the notice, when it arrives)
				if (shine >= 0 && shine <= 1)
				{
					var clip = g.Clip;
					g.SetClip(cup, CombineMode.Intersect);
					float sx = x - 0.4f * s + shine * 1.6f * s;
					using (var band = new GraphicsPath())
					{
						band.AddPolygon(new[] { new PointF(sx, y), new PointF(sx + 0.16f * s, y), new PointF(sx - 0.1f * s, y + s), new PointF(sx - 0.26f * s, y + s) });
						using (var b = new SolidBrush(Color.FromArgb(150, 255, 255, 255))) g.FillPath(b, band);
					}
					g.Clip = clip;
				}
			}
			g.SmoothingMode = old;
		}

		static GraphicsPath Star(PointF c, float outer, float inner)
		{
			var pts = new PointF[10];
			for (int i = 0; i < 10; i++)
			{
				double ang = -Math.PI / 2 + i * Math.PI / 5;
				float rr = i % 2 == 0 ? outer : inner;
				pts[i] = new PointF(c.X + (float)(Math.Cos(ang) * rr), c.Y + (float)(Math.Sin(ang) * rr));
			}
			var p = new GraphicsPath();
			p.AddPolygon(pts);
			return p;
		}

		internal static GraphicsPath Rounded(RectangleF r, float radius)
		{
			var p = new GraphicsPath();
			float d = Math.Max(0.1f, radius * 2);
			p.AddArc(r.X, r.Y, d, d, 180, 90);
			p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
			p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
			p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
			p.CloseFigure();
			return p;
		}

		// ------------------------------------------------------------------ the window that comes down from the top

		/// <summary>A dark card with a golden trophy: slides down from the top edge, stays 8 s (longer under the mouse), slides up.</summary>
		sealed class Toast : Form
		{
			const int W = 320, H = 64;
			const double InTime = 0.45, Stay = 8.0, OutTime = 0.35;
			static readonly Color Back = Color.FromArgb(30, 33, 44), Line = Color.FromArgb(212, 160, 50), Label = Color.FromArgb(240, 196, 90), Desc = Color.FromArgb(190, 196, 210);

			readonly Achievement a;
			readonly Timer clock = new Timer { Interval = 15 };
			readonly Font fLabel = Theme.CreateFont(10, FontStyle.Bold), fTitle = Theme.CreateFont(14, FontStyle.Bold), fDesc = Theme.CreateFont(11);
			DateTime started;
			double held;            // seconds spent under the mouse (the time stops)
			DateTime? hoverSince;
			int topShown, topHidden;
			float shine = -1;

			public Toast(Achievement a)
			{
				this.a = a;
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = true; DoubleBuffered = true;
				BackColor = Back;
				Text = "";
				Size = new Size(W, H);
				Cursor = Cursors.Hand;
				clock.Tick += (s, e) => Step();
				FormClosed += (s, e) => { clock.Stop(); clock.Dispose(); fLabel.Dispose(); fTitle.Dispose(); fDesc.Dispose(); };
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get { var cp = base.CreateParams; cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008; cp.ClassStyle |= 0x00020000; return cp; }   // tool, no activate, topmost, drop shadow
			}

			[DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

			protected override void OnHandleCreated(EventArgs e)
			{
				base.OnHandleCreated(e);
				// Windows 11 rounds the corners itself (smooth, with a shadow); on Windows 10 the window is cut round
				int round = 2;   // DWMWCP_ROUND
				bool win11 = Environment.OSVersion.Version.Build >= 22000;
				if (win11) { try { DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int)); } catch (Exception skipped) { LogFile.Ignored(skipped); } }
				else using (var p = Rounded(new RectangleF(0, 0, W, H), 10)) Region = new Region(p);
			}

			/// <summary>The screen of the main pet (or the primary one), centred at the top.</summary>
			public void Come()
			{
				var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow);
				var area = pet != null ? Screen.FromControl(pet).WorkingArea : Screen.PrimaryScreen.WorkingArea;
				topShown = area.Top + 12;
				topHidden = area.Top - H - 12;
				Location = new Point(area.Left + (area.Width - W) / 2, topHidden);
				started = DateTime.Now;
				Show();
				clock.Start();
			}

			void Step()
			{
				var now = DateTime.Now;
				double pausedNow = hoverSince != null ? (now - hoverSince.Value).TotalSeconds : 0;
				double t = (now - started).TotalSeconds - held - pausedNow;
				double k;
				if (t < InTime) k = 1 - Math.Pow(1 - t / InTime, 3);           // comes down, slowing at the end
				else if (t < InTime + Stay) k = 1;
				else if (t < InTime + Stay + OutTime) { double u = (t - InTime - Stay) / OutTime; k = 1 - u * u; }   // goes up, faster and faster
				else { Close(); return; }
				int top = (int)Math.Round(topHidden + (topShown - topHidden) * k);
				if (top != Top) Top = top;
				// the light crosses the trophy once, just after it arrives
				float sh = (float)((t - InTime) / 0.7);
				float newShine = sh >= 0 && sh <= 1 ? sh : -1;
				if (newShine != shine) { shine = newShine; Invalidate(new Rectangle(8, 4, 56, 56)); }
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Back);
				// a thin golden line along the top and a glow behind the trophy
				using (var b = new LinearGradientBrush(new Rectangle(0, 0, W, 3), Color.FromArgb(0, Line), Line, 0f))
				{
					var blend = new ColorBlend(3) { Colors = new[] { Color.FromArgb(0, Line), Line, Color.FromArgb(0, Line) }, Positions = new[] { 0f, 0.5f, 1f } };
					b.InterpolationColors = blend;
					g.FillRectangle(b, 0, 0, W, 3);
				}
				using (var glow = new GraphicsPath())
				{
					glow.AddEllipse(4, 2, 60, 60);
					using (var b = new PathGradientBrush(glow) { CenterColor = Color.FromArgb(90, 255, 200, 80), SurroundColors = new[] { Color.FromArgb(0, 255, 200, 80) } }) g.FillPath(b, glow);
				}
				DrawTrophy(g, new RectangleF(14, 10, 44, 44), true, shine);
				int tx = 68, tw = W - tx - 12;
				TextRenderer.DrawText(g, Lang.T("ach.unlocked").ToUpperInvariant(), fLabel, new Rectangle(tx, 9, tw, 14), Label, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
				TextRenderer.DrawText(g, a.Title, fTitle, new Rectangle(tx, 22, tw, 20), Color.White, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
				TextRenderer.DrawText(g, a.Description, fDesc, new Rectangle(tx, 42, tw, 16), Desc, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
				if (Environment.OSVersion.Version.Build < 22000)
					using (var p = Rounded(new RectangleF(0.5f, 0.5f, W - 1, H - 1), 10)) using (var pen = new Pen(Color.FromArgb(70, Line))) g.DrawPath(pen, p);
			}

			// under the mouse the time stops (to read it calmly); a click opens the Achievements page
			protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (hoverSince == null && (DateTime.Now - started).TotalSeconds - held > InTime) hoverSince = DateTime.Now; }
			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);
				if (hoverSince != null) { held += (DateTime.Now - hoverSince.Value).TotalSeconds; hoverSince = null; }
			}

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				Close();
				try { FormSettings.ShowPage(FormSettings.Page.Achievements); }
				catch (Exception skipped) { LogFile.Ignored(skipped); }
			}
		}
	}
}
