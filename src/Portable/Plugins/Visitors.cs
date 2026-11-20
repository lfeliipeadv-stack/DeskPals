using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== 10. visits on the screen

	/// <summary>
	/// Now and then a small animal visits the pet: a butterfly flutters around it and lands on its head, a bird perches
	/// and pecks, a bee buzzes around it, a ladybug lands and crawls on it. Then it flies on to the other side of the
	/// screen and disappears there.
	/// </summary>
	public static class Visitors
	{
		static Timer timer;
		static DateTime next = DateTime.Now.AddMinutes(12);
		static VisitorForm current;
		/// <summary>A visitor is on screen now, and where (self-test).</summary>
		internal static Form Flying => current != null && !current.IsDisposed && current.Visible ? current : null;
		/// <summary>Which visitor is on screen now (self-test).</summary>
		internal static Kind? FlyingKind => Flying != null ? current.What : (Kind?)null;
		static readonly Random rnd = new Random();

		public enum Kind { Butterfly, Bird, Bee, Ladybug }

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("Visitors", false);
			set { Program.MyData.SetSetting("Visitors", value); Apply(); }
		}

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 30000 }; timer.Tick += (s, e) => { if (DateTime.Now >= next) Visit(); }; }
			if (Enabled) timer.Start(); else timer.Stop();
		}

		static FormPet Pet() => Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow && p.Opacity > 0);

		/// <summary>A visitor now, chosen at random (also the "Call now" button).</summary>
		public static void Visit() => Visit((Kind)rnd.Next(4));

		/// <summary>This visitor now.</summary>
		internal static void Visit(Kind kind)
		{
			if (SelfTest.Running && !SelfTest.Live) return;
			next = DateTime.Now.AddMinutes(20 + rnd.Next(25));
			var pet = Pet();
			if (pet == null || SpeechBubble.QuietNow || (current != null && !current.IsDisposed)) return;
			current = new VisitorForm(pet, kind);
			current.Show();
		}

		// ------------------------------------------------------------------ the drawings
		// one character per pixel: '.' clear, 'k' outline; the other letters get the colors of each animal

		static readonly string[] ButterflyOpen =
		{
			".....k.......k.....",
			"......k.....k......",
			".kkk...k...k...kkk.",
			"kdwwkk..kbk..kkwwdk",
			"kdwwwwk.kbk.kwwwwdk",
			"kdwyywwkkbkkwwyywdk",
			"kdwyywwwkbkwwwyywdk",
			"kddwwwwwkbkwwwwwddk",
			".kddwwwwkbkwwwwddk.",
			"..kkkdwwkbkwwdkkk..",
			"...kdwwwkbkwwwdk...",
			"..kdwllwkbkwllwdk..",
			"..kdwllwkbkwllwdk..",
			"..kddwwk.k.kwwddk..",
			"...kkkk.....kkkk...",
		};

		static readonly string[] BirdPerch =
		{
			"..................",
			"..........kkkk....",
			".........kbbbbk...",
			"........kbbbbbbk..",
			"........kbbbkebk..",
			"...kk...kbbbbbbkoo",
			"..kbbk.kbbbbbbbk..",
			"..kbbbkbbwwwbblk..",
			"...kbbbbwwwwbllk..",
			"....kbbbwwwwbllk..",
			".....kbbbwwbbllk..",
			"......kkbbbbllk...",
			"........kkkkkk....",
			".........f..f.....",
			".........ff.ff....",
		};
		static readonly string[] BirdPeck =
		{
			"..................",
			"..................",
			"..................",
			"..........kkkk....",
			".........kbbbbk...",
			"...kk...kbbbbbbk..",
			"..kbbk.kbbbbbkebk.",
			"..kbbbkbbwwwbbbkoo",
			"...kbbbbwwwwbbbk..",
			"....kbbbwwwwbllk..",
			".....kbbbwwbbllk..",
			"......kkbbbbllk...",
			"........kkkkkk....",
			".........f..f.....",
			".........ff.ff....",
		};
		static readonly string[] BirdFly1 =
		{
			"....kkkk..........",
			"...kwwwwk.kkkk....",
			"....kwwwwkbbbbk...",
			".....kwwwkbbbbbk..",
			"..kk..kwwkbbkebk..",
			".kbbk.kwwbbbbbbkoo",
			".kbbbkbbbbbbbbbk..",
			"..kbbbbbbbbbbllk..",
			"...kkbbbbbbbbllk..",
			".....kkkbbbbllk...",
			"........kkkkkk....",
			"..................",
		};
		static readonly string[] BirdFly2 =
		{
			"..........kkkk....",
			".........kbbbbk...",
			"........kbbbbbbk..",
			"..kk....kbbbkebk..",
			".kbbk...kbbbbbbkoo",
			".kbbbkkkbbbbbbbk..",
			"..kbbbbwwwwwbllk..",
			"...kkbwwwwwwbllk..",
			"....kwwwwwwkbllk..",
			"...kwwwwwkkbllk...",
			"....kkkkk.kkkk....",
			"..................",
		};

		static readonly string[] Bee1 =
		{
			".....kkk.kkk...",
			"....kwwwkwwwk..",
			"....kwlwkwlwk..",
			".....kwwkwwk...",
			"...kkkkkkkkkk..",
			"..kyykyykyykkk.",
			".kyyykyykyykkek",
			"kkyyykyykyykkkk",
			".kyyykyykyykkk.",
			"..kyykyykyykk..",
			"...kkkkkkkkk...",
			"....k...k......",
		};
		static readonly string[] Bee2 =
		{
			"...............",
			"...............",
			"...............",
			"..kkwwkkwwk....",
			".kwwlwwkwlwwk..",
			"..kkkkkkkkkkk..",
			"..kyykyykyykkk.",
			".kyyykyykyykkek",
			"kkyyykyykyykkkk",
			".kyyykyykyykkk.",
			"..kyykyykyykk..",
			"...kkkkkkkkk...",
		};

		static readonly string[] LadyWalk =
		{
			"...k.....k...",
			"....k...k....",
			".....kkk.....",
			"....kkkkk....",
			"..kkkrkrkkk..",
			".krrrrkrrrrk.",
			"krkkrrkrrkkrk",
			"krkkrrkrrkkrk",
			"krrrrrkrrrrrk",
			"krrkkrkrkkrrk",
			".krkkrkrkkrk.",
			"..krrrkrrrk..",
			"...kkkkkkk...",
			"..k.k...k.k..",
		};
		static readonly string[] LadyFly =
		{
			"...k.....k...",
			"....k...k....",
			".....kkk.....",
			".kk.kkkkk.kk.",
			"krrkk.k.kkrrk",
			"krkkrkwkrkkrk",
			"krkkrkwkrkkrk",
			"krrrkwwwkrrrk",
			"krrkkwwwkkrrk",
			".kkkwwlwwkkk.",
			"...kwwlwwk...",
			"...kwlllwk...",
			"....kwwwk....",
			".....kkk.....",
		};

		/// <summary>The same drawing with the wings folded towards the body (rows 0-2, the antennae, stay).</summary>
		static string[] Fold(string[] open, double f)
		{
			int c = open[0].Length / 2;
			return open.Select((row, j) => j < 3 ? row : new string(Enumerable.Range(0, row.Length).Select(x =>
			{
				int src = (int)Math.Round(c + (x - c) / f);
				return src >= 0 && src < row.Length ? row[src] : '.';
			}).ToArray())).ToArray();
		}

		/// <summary>The drawings of one animal, with their colors (wings and body of a random color).</summary>
		internal static string[][] Drawings(Kind kind) => kind switch
		{
			Kind.Butterfly => new[] { ButterflyOpen, Fold(ButterflyOpen, 0.62), Fold(ButterflyOpen, 0.22), Fold(ButterflyOpen, 0.62) },
			Kind.Bird => new[] { BirdFly1, BirdFly2, BirdPerch, BirdPeck },
			Kind.Bee => new[] { Bee1, Bee2 },
			_ => new[] { LadyFly, LadyWalk },
		};

		internal static Dictionary<char, Color> Colors(Kind kind, Random r)
		{
			var p = new Dictionary<char, Color> { ['k'] = Color.FromArgb(45, 32, 36), ['e'] = Color.FromArgb(15, 15, 18) };
			switch (kind)
			{
				case Kind.Butterfly:
					var wings = new[] { (Color.FromArgb(245, 140, 40), Color.FromArgb(185, 80, 20)), (Color.FromArgb(90, 160, 245), Color.FromArgb(40, 90, 190)),
						(Color.FromArgb(240, 110, 200), Color.FromArgb(170, 60, 140)), (Color.FromArgb(255, 210, 60), Color.FromArgb(200, 140, 20)) }[r.Next(4)];
					p['w'] = wings.Item1; p['d'] = wings.Item2; p['y'] = Color.FromArgb(255, 248, 225); p['l'] = Color.FromArgb(255, 225, 160); p['b'] = Color.FromArgb(70, 48, 36);
					break;
				case Kind.Bird:
					var body = new[] { (Color.FromArgb(80, 140, 225), Color.FromArgb(45, 90, 175)), (Color.FromArgb(215, 75, 60), Color.FromArgb(160, 45, 40)),
						(Color.FromArgb(120, 175, 90), Color.FromArgb(75, 125, 60)) }[r.Next(3)];
					p['b'] = body.Item1; p['w'] = body.Item2; p['l'] = Color.FromArgb(250, 225, 185); p['o'] = Color.FromArgb(250, 170, 40); p['f'] = Color.FromArgb(225, 140, 60);
					break;
				case Kind.Bee:
					p['y'] = Color.FromArgb(255, 205, 40); p['w'] = Color.FromArgb(215, 235, 250); p['l'] = Color.FromArgb(250, 253, 255);
					break;
				default:
					p['r'] = Color.FromArgb(225, 40, 40); p['w'] = Color.FromArgb(205, 225, 240); p['l'] = Color.FromArgb(245, 250, 255);
					break;
			}
			return p;
		}

		sealed class VisitorForm : Form
		{
			public readonly Kind What;
			readonly FormPet pet;
			readonly Timer tick = new Timer { Interval = 40 };
			readonly Bitmap[] frames, flipped;
			readonly Rectangle screen;
			readonly int side;          // +1 came in from the right (leaves to the left), -1 the other way
			double x, y, t, phaseT, lastX;
			int phase;                  // 0 flying in, 1 the visit, 2 flying on to the other side
			int frame;
			bool faceLeft;

			public VisitorForm(FormPet pet, Kind kind)
			{
				this.pet = pet; What = kind;
				// the same size next to every pet (the size of the classic 40 px sheep), bigger only with the size option (2x, 4x)
				int px = Math.Max(1, Program.MyData.GetScale());
				var colors = Colors(kind, rnd);
				frames = Drawings(kind).Select(d => Draw(d, colors, px, false)).ToArray();
				flipped = Drawings(kind).Select(d => Draw(d, colors, px, true)).ToArray();
				FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
				BackColor = Color.Magenta; TransparencyKey = Color.Magenta;
				Size = new Size(frames.Max(b => b.Width), frames.Max(b => b.Height));
				screen = Screen.FromControl(pet).WorkingArea;
				side = pet.Left + pet.Width / 2 > screen.Left + screen.Width / 2 ? 1 : -1;
				x = side > 0 ? screen.Right + 4 : screen.Left - Width - 4;
				y = Math.Max(screen.Top + 10, pet.Top - 70);
				lastX = x;
				Location = new Point((int)x, (int)y);
				tick.Tick += (s, e) => Step();
				tick.Start();
			}

			protected override CreateParams CreateParams
			{
				get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 /* no activate */ | 0x80 /* tool window */ | 0x20 /* click-through */; return cp; }
			}
			protected override bool ShowWithoutActivation => true;

			static Bitmap Draw(string[] rows, Dictionary<char, Color> colors, int px, bool flip)
			{
				int w = rows.Max(r => r.Length);
				var b = new Bitmap(w * px, rows.Length * px);
				using (var g = Graphics.FromImage(b))
				{
					g.Clear(Color.Magenta);
					for (int j = 0; j < rows.Length; j++)
						for (int i = 0; i < rows[j].Length; i++)
						{
							if (!colors.TryGetValue(rows[j][i], out var col)) continue;
							using (var br = new SolidBrush(col)) g.FillRectangle(br, (flip ? w - 1 - i : i) * px, j * px, px, px);
						}
				}
				return b;
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var set = faceLeft ? flipped : frames;
				var img = set[Math.Min(frame, set.Length - 1)];
				e.Graphics.DrawImageUnscaled(img, (Width - img.Width) / 2, Height - img.Height);
			}

			// where the visitor stands on the head, and a point above it
			double HeadX => pet.Left + pet.Width / 2.0 - Width / 2.0;
			double HeadY => pet.Top + pet.Height * 0.35 - Height;

			void Step()
			{
				if (pet.IsDisposed || !pet.Visible) { Close(); return; }
				t += 0.04; phaseT += 0.04;
				double hx = HeadX, hy = HeadY;
				switch (phase)
				{
					case 0:   // fly to the pet, bobbing
						x += Math.Sign(hx - x) * Math.Min(What == Kind.Bee ? 6 : 4.5, Math.Abs(hx - x));
						y += (hy - 30 - y) * 0.06 + Math.Sin(t * (What == Kind.Bee ? 14 : 6)) * 1.5;
						if (Math.Abs(hx - x) < 6) { phase = 1; phaseT = 0; pet.React("look around", "happy", "wave"); }
						break;
					case 1:
						if (Visit(hx, hy)) { phase = 2; phaseT = 0; pet.React("happy", "laugh", "wave"); }
						break;
					default:  // on to the other side of the screen, a little upwards, then gone
						x -= side * (What == Kind.Bee ? 10 : 8);
						y = Math.Max(screen.Top + 4, y - 0.7 + Math.Sin(t * (What == Kind.Bee ? 14 : 5)) * (What == Kind.Butterfly ? 2.2 : 1));
						if (x + Width < screen.Left - 2 || x > screen.Right + 2 || phaseT > 40) { Close(); return; }
						break;
				}
				Pick();
				if (Math.Abs(x - lastX) > 0.5) faceLeft = x < lastX;
				lastX = x;
				Location = new Point((int)Math.Round(x), (int)Math.Round(y));
				Invalidate();
			}

			/// <summary>One step of the visit itself; true when it is over.</summary>
			bool Visit(double hx, double hy)
			{
				switch (What)
				{
					case Kind.Butterfly:   // around the pet, then a rest on its head
						if (phaseT < 6) { x = hx + Math.Cos(t * 2.2) * pet.Width * 0.45; y = hy - 22 + Math.Sin(t * 4.4) * 12; }
						else { x += (hx - x) * 0.2; y += (hy - y) * 0.2; }
						return phaseT > 9;
					case Kind.Bird:        // perches, hops a little and pecks
					{
						x += (hx - x) * 0.25;
						double hop = phaseT % 1.6 < 0.3 ? Math.Sin(phaseT % 1.6 / 0.3 * Math.PI) * 6 : 0;
						y += (hy - hop - y) * 0.35;
						return phaseT > 6;
					}
					case Kind.Bee:         // buzzes around in a figure of eight
						x = hx + Math.Sin(t * 2.6) * pet.Width * 0.6 + Math.Sin(t * 23) * 1.5;
						y = hy - 18 + Math.Sin(t * 5.2) * 14 + Math.Cos(t * 19) * 1.5;
						if (phaseT > 1 && phaseT < 1.05) pet.React("scared", "surprised", "look around");
						return phaseT > 6;
					default:               // lands and crawls a little from side to side
						x += (hx + Math.Sin(phaseT * 1.3) * pet.Width * 0.18 - x) * 0.25;
						y += (hy - y) * 0.3;
						return phaseT > 7;
				}
			}

			/// <summary>The drawing to show now.</summary>
			void Pick()
			{
				switch (What)
				{
					case Kind.Butterfly:
						bool resting = phase == 1 && phaseT >= 6.5;
						frame = resting ? new[] { 0, 1, 2, 1, 0, 0, 0, 0 }[(int)(phaseT * 8) % 8] : (int)(t * 16) % 4;   // resting: a slow flap now and then
						break;
					case Kind.Bird:
						bool perched = phase == 1 && Math.Abs(HeadY - y) < 8;
						frame = perched ? (phaseT % 1.6 >= 0.8 && phaseT % 1.6 < 1.2 ? 3 : 2) : (int)(t * 10) % 2;
						break;
					case Kind.Bee:
						frame = (int)(t * 25) % 2;
						break;
					default:
						frame = phase == 1 && phaseT > 0.6 ? 1 : 0;
						break;
				}
			}

			protected override void OnFormClosed(FormClosedEventArgs e)
			{
				tick.Stop(); tick.Dispose();
				foreach (var b in frames.Concat(flipped)) b.Dispose();
				base.OnFormClosed(e);
			}
		}
	}
}
