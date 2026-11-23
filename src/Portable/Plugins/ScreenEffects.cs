using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== snow, rain, confetti and leaves on the screen

	/// <summary>
	/// Off unless turned on (the weather part; a show can always be started by hand). For a few minutes the screen gets
	/// snow that piles up on the top of the windows and on the taskbar, rain that splashes on them, confetti or falling
	/// leaves. With the weather plugin on, it snows or rains on the screen when it snows or rains outside (at most once an
	/// hour, between 7 and 22 h, never over a full-screen program or during quiet time). One window over each screen, that
	/// the mouse goes through; when the time is up the piles melt and it closes.
	/// </summary>
	public static class ScreenEffects
	{
		public enum Kind { Snow, Rain, Confetti, Leaves }

		/// <summary>Follow the weather (it snows or rains on the screen when it does outside).</summary>
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("ScreenFx", false);
			set { Program.MyData.SetSetting("ScreenFx", value); Apply(); }
		}

		/// <summary>How long a show lasts, in minutes.</summary>
		public static int Minutes
		{
			get { int v = Program.MyData.GetSettingInt("ScreenFxMinutes", 2); return MinuteChoices.Contains(v) ? v : 2; }
			set => Program.MyData.SetSetting("ScreenFxMinutes", value);
		}
		public static readonly int[] MinuteChoices = { 1, 2, 5 };

		static readonly List<Overlay> overlays = new List<Overlay>();
		public static bool Running => overlays.Any(o => !o.IsDisposed);
		public static Kind? Showing { get; private set; }

		/// <summary>
		/// Start a show of this kind for this many seconds. <paramref name="asked"/>: the user (or a date, a level, the chat)
		/// asked for it, so it shows even during quiet time; a full-screen program is never covered.
		/// </summary>
		public static void Start(Kind kind, int seconds, bool asked = false)
		{
			if (FullScreenWatch.Active || HideHotkey.Hidden) return;
			if (!asked && SpeechBubble.QuietNow) return;
			Stop(true);
			Showing = kind;
			foreach (var screen in Screen.AllScreens)
			{
				var o = new Overlay(screen, kind, TimeSpan.FromSeconds(Math.Max(3, seconds)));
				o.FormClosed += (s, e) => { overlays.Remove(o); if (overlays.Count == 0) Showing = null; };
				overlays.Add(o);
				o.Show();
			}
			if (kind == Kind.Snow) Program.Mainthread?.ReactAll("cold", "shiver", "look up", "happy");
			else if (kind == Kind.Rain) Program.Mainthread?.ReactAll("umbrella", "rain cloud", "look up");
			else Program.Mainthread?.ReactAll("happy", "dance", "cheer");
		}

		/// <summary>End the show (the piles melt first, unless <paramref name="now"/>).</summary>
		public static void Stop(bool now = false)
		{
			foreach (var o in overlays.ToList()) { if (now) o.Close(); else o.Melt(); }
		}

		static Timer timer;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 5 * 60 * 1000 }; timer.Tick += (s, e) => FollowWeather(); }
			if (Enabled) timer.Start(); else timer.Stop();
		}

		/// <summary>The weather says snow or rain now: the same on the screen (at most once an hour).</summary>
		static void FollowWeather()
		{
			if (!Enabled || !Weather.Enabled || Running) return;
			var f = Weather.ForecastCached;
			if (f == null) return;
			var now = DateTime.Now;
			if (now.Hour < 7 || now.Hour >= 22) return;
			Kind? kind = Weather.IsSnow(f.Code) ? Kind.Snow : Weather.IsRain(f.Code) ? Kind.Rain : (Kind?)null;
			if (kind == null) return;
			if (DateTime.TryParse(Program.MyData.GetSetting("ScreenFxLast"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var last) && (now - last).TotalMinutes < 60) return;
			Program.MyData.SetSetting("ScreenFxLast", now.ToString("s", CultureInfo.InvariantCulture));
			Start(kind.Value, Minutes * 60);
			CommunityPlugins.Event(kind == Kind.Snow ? "snow" : "rain");
		}

		// ------------------------------------------------------------------ the window over a screen

		internal sealed class Overlay : Form
		{
			sealed class Bit { public float X, Y, Vx, Vy, Phase; public int W, H; public Color C; public bool Landed; public DateTime At; }

			readonly Kind kind;
			readonly Rectangle screen;
			readonly DateTime ends;
			readonly Random rnd = new Random();
			readonly List<Bit> bits = new List<Bit>();
			readonly Timer tick = new Timer { Interval = 40 };
			const int Bucket = 4;
			/// <summary>For each strip of the screen: where things land (screen y, relative to this screen) and what is piled there.</summary>
			readonly int[] floor;
			readonly IntPtr[] floorOf;
			readonly float[] pile;
			DateTime lastFloors = DateTime.MinValue;
			bool melting;
			float melt = 1;

			static readonly Color[] ConfettiColors = { Color.FromArgb(232, 72, 96), Color.FromArgb(70, 160, 232), Color.FromArgb(255, 210, 60), Color.FromArgb(90, 200, 120), Color.FromArgb(180, 100, 220), Color.FromArgb(255, 154, 42) };
			static readonly Color[] LeafColors = { Color.FromArgb(214, 108, 34), Color.FromArgb(190, 64, 40), Color.FromArgb(232, 170, 48), Color.FromArgb(150, 96, 48) };

			public Overlay(Screen s, Kind k, TimeSpan length)
			{
				kind = k; screen = s.Bounds; ends = DateTime.Now + length;
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
				Bounds = screen;
				BackColor = TransparencyKey = Color.Magenta;
				DoubleBuffered = true;
				TopMost = true;
				Text = "";
				int n = (screen.Width + Bucket - 1) / Bucket;
				floor = new int[n]; floorOf = new IntPtr[n]; pile = new float[n];
				tick.Tick += (o, e) => Step();
				FormClosed += (o, e) => { tick.Stop(); tick.Dispose(); };
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008 | 0x00080000 | 0x00000020;   // tool, no activate, topmost, layered, click-through
					return cp;
				}
			}

			protected override void OnShown(EventArgs e) { base.OnShown(e); Floors(); tick.Start(); }

			public void Melt() { if (!melting) { melting = true; } }

			/// <summary>Self-test: what is falling and what has landed.</summary>
			internal int Falling => bits.Count(b => !b.Landed);
			internal float Piled => pile.Sum();

			/// <summary>
			/// Where something falling at each strip lands: the top edge of the front-most window that is seen there (an edge
			/// hidden behind another window does not count), or the taskbar, or the bottom of the screen.
			/// </summary>
			void Floors()
			{
				lastFloors = DateTime.Now;
				var wins = DesktopWindows.List();
				var work = Screen.FromRectangle(screen).WorkingArea;
				for (int i = 0; i < floor.Length; i++)
				{
					int x = screen.X + i * Bucket + Bucket / 2;
					int best = (x >= work.Left && x < work.Right ? work.Bottom : screen.Bottom) - screen.Y;
					IntPtr bestOf = IntPtr.Zero;
					for (int w = 0; w < wins.Count; w++)
					{
						var r = wins[w].Bounds;
						if (x < r.Left || x >= r.Right || r.Top <= screen.Top + 2 || r.Top >= screen.Bottom) continue;
						int y = r.Top - screen.Y;
						if (y >= best) continue;
						bool hidden = false;
						for (int a = 0; a < w && !hidden; a++) if (wins[a].Bounds.Contains(x, r.Top)) hidden = true;
						if (!hidden) { best = y; bestOf = wins[w].Handle; }
					}
					// the window under the pile moved or closed: the pile goes with it
					if (floor[i] != best || floorOf[i] != bestOf) { pile[i] = 0; floor[i] = best; floorOf[i] = bestOf; }
				}
			}

			int Max => kind == Kind.Rain ? 220 : kind == Kind.Snow ? 260 : 160;

			void Spawn()
			{
				int want = DateTime.Now > ends || melting ? 0 : Max;
				int falling = bits.Count(b => !b.Landed);
				for (int k = 0; k < 6 && falling < want; k++, falling++)
				{
					var b = new Bit { X = rnd.Next(0, screen.Width), Y = -rnd.Next(4, 40), Phase = (float)(rnd.NextDouble() * Math.PI * 2) };
					switch (kind)
					{
						case Kind.Snow: b.Vy = 0.8f + (float)rnd.NextDouble() * 1.4f; b.W = b.H = rnd.Next(2, 5); b.C = Color.White; break;
						case Kind.Rain: b.Vy = 14 + (float)rnd.NextDouble() * 6; b.Vx = -1.5f; b.W = 1; b.H = rnd.Next(8, 14); b.C = Color.FromArgb(120, 160, 230); break;
						case Kind.Confetti: b.Vy = 1.6f + (float)rnd.NextDouble() * 1.6f; b.W = rnd.Next(3, 6); b.H = rnd.Next(2, 4); b.C = ConfettiColors[rnd.Next(ConfettiColors.Length)]; break;
						default: b.Vy = 1.2f + (float)rnd.NextDouble() * 1.2f; b.W = rnd.Next(5, 8); b.H = rnd.Next(3, 5); b.C = LeafColors[rnd.Next(LeafColors.Length)]; break;
					}
					bits.Add(b);
				}
			}

			void Step()
			{
				if (FullScreenWatch.Active || HideHotkey.Hidden) { Close(); return; }
				if ((DateTime.Now - lastFloors).TotalMilliseconds > 700) Floors();
				if (DateTime.Now > ends) melting = true;
				Spawn();
				var now = DateTime.Now;
				foreach (var b in bits)
				{
					if (b.Landed) continue;
					b.Phase += 0.08f;
					float sway = kind == Kind.Snow ? (float)Math.Sin(b.Phase) * 0.6f : kind == Kind.Leaves ? (float)Math.Sin(b.Phase) * 1.6f : kind == Kind.Confetti ? (float)Math.Sin(b.Phase * 1.7) * 1.1f : 0;
					b.X += b.Vx + sway; b.Y += b.Vy;
					int i = (int)(b.X / Bucket);
					if (i < 0 || i >= floor.Length) { b.Landed = true; b.At = DateTime.MinValue; continue; }
					float top = floor[i] - (kind == Kind.Snow ? pile[i] : 0);
					if (b.Y + b.H >= top)
					{
						switch (kind)
						{
							case Kind.Snow:
								// the flake joins the pile (piles stay soft: a strip never grows much above its neighbours)
								float neighbours = Math.Min(i > 0 ? pile[i - 1] : pile[i], i < pile.Length - 1 ? pile[i + 1] : pile[i]);
								int j = pile[i] > neighbours + 3 ? (i > 0 && pile[i - 1] < (i < pile.Length - 1 ? pile[i + 1] : float.MaxValue) ? i - 1 : Math.Min(pile.Length - 1, i + 1)) : i;
								if (pile[j] < 16) pile[j] += b.W * b.H / 9f;
								b.Landed = true; b.At = DateTime.MinValue;
								break;
							case Kind.Rain:
								b.Landed = true; b.At = now; b.Y = top - 2; b.H = 2; b.W = 3;   // a splash for a moment
								break;
							default:
								b.Landed = true; b.At = now; b.Y = top - b.H;   // lies there for a while
								break;
						}
					}
				}
				bits.RemoveAll(b => b.Landed && (b.At == DateTime.MinValue || (now - b.At).TotalSeconds > (kind == Kind.Rain ? 0.15 : 12)));
				if (melting)
				{
					melt -= 0.02f;
					for (int i = 0; i < pile.Length; i++) pile[i] = Math.Max(0, pile[i] - 0.25f);
					if (melt <= 0 && bits.Count(b => !b.Landed) == 0) { Close(); return; }
				}
				Invalidate();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				g.Clear(Color.Magenta);
				if (kind == Kind.Snow)
				{
					using (var white = new SolidBrush(Color.White))
					using (var shade = new SolidBrush(Color.FromArgb(196, 214, 236)))
						for (int i = 0; i < pile.Length; i++)
						{
							int h = (int)Math.Round(pile[i]);
							if (h <= 0) continue;
							g.FillRectangle(shade, i * Bucket, floor[i] - h, Bucket, h);
							g.FillRectangle(white, i * Bucket, floor[i] - h, Bucket, Math.Max(1, h - 1));
						}
				}
				foreach (var b in bits)
				{
					using (var brush = new SolidBrush(b.C))
					{
						if (kind == Kind.Snow)
						{
							using (var rim = new SolidBrush(Color.FromArgb(170, 190, 220))) g.FillRectangle(rim, b.X - 1, b.Y - 1, b.W + 2, b.H + 2);
							g.FillRectangle(brush, b.X, b.Y, b.W, b.H);
						}
						else if (kind == Kind.Confetti && !b.Landed && Math.Sin(b.Phase * 2) < 0) g.FillRectangle(brush, b.X, b.Y, b.H, b.W);   // turning over
						else g.FillRectangle(brush, b.X, b.Y, b.W, b.H);
					}
				}
			}
		}
	}
}
