using System.Drawing;
using System.Drawing.Drawing2D;

namespace DeskPalsWeather
{
	/// <summary>How the Christmas lights light up: steady, blinking here and there, chasing round, changing colour.</summary>
	public enum LightsMode { Steady = 0, Twinkle = 1, Chase = 2, Changing = 3 }

	/// <summary>
	/// Christmas lights round the windows: a dark green wire along the four edges, a bulb on it every few points, each
	/// a little oval of glass on its socket, pointing out of the window and tilted its own way, with a glow round it. A
	/// window in front hides the lights of the one behind. The same window always gets the same bulbs (they come from
	/// where they are, not from chance).
	/// </summary>
	internal static class LightsArt
	{
		public static readonly Color[] Palette =
		{
			Color.FromArgb(236, 56, 56), Color.FromArgb(255, 204, 52), Color.FromArgb(64, 200, 92), Color.FromArgb(52, 196, 222),
			Color.FromArgb(180, 86, 232), Color.FromArgb(255, 112, 178), Color.FromArgb(255, 140, 46), Color.FromArgb(76, 124, 255),
		};
		/// <summary>The shape of the bulbs (the models to choose from).</summary>
		public enum BulbShape { Long = 0, Mini = 1, Classic = 2, Ball = 3, Lying = 4, Video = 5 }
		public static BulbShape Shape = BulbShape.Video;

		/// <summary>The colours along the wire, in their order (they repeat); one colour for all instead (null: the order); colours drawn at random; how bright (0..1).</summary>
		public static Color[] Pattern;   // (none: the colours of the video)
		public static Color? Single;
		/// <summary>Goes up whenever the settings change (the steady lights are drawn again then).</summary>
		public static int Version;
		/// <summary>How far apart the bulbs hang, in points (0: the shape's own).</summary>
		public static float Spacing;
		/// <summary>Every bulb turned its own way (true), or each shape in its own order.</summary>
		public static bool RandomAngles = true;
		public static bool RandomColors;
		/// <summary>How fast they blink, chase and change (1 = normal).</summary>
		public static float Speed = 1;
		public static float Brightness = 0.8f;
		/// <summary>For the tests: only the wire.</summary>
		internal static bool NoBulbs;

		/// <summary>The 24 bulbs of the wreath at the start: the colours of the video in their order (red, purple, green, cyan, yellow).</summary>
		public static Color[] DefaultPattern() => Enumerable.Range(0, 24).Select(i => VideoColors[i % VideoColors.Length]).ToArray();

		/// <summary>Length and width of a bulb, and how far apart they hang, for each shape (at size 1).</summary>
		static (float l, float w, float spacing) Measures(BulbShape s) => s switch
		{
			BulbShape.Mini => (12f, 4.2f, 10.5f),
			BulbShape.Classic => (13.5f, 7.8f, 12.5f),
			BulbShape.Ball => (9.5f, 7.6f, 11f),
			BulbShape.Lying => (13.5f, 7.6f, 12.5f),
			BulbShape.Video => (14f, 7.4f, 15f),
			_ => (13f, 6.6f, 10f),
		};

		/// <summary>The colours of the video, in their order along the wire: red, purple, green, cyan, yellow.</summary>
		internal static readonly Color[] VideoColors = { Color.FromArgb(226, 70, 82), Color.FromArgb(160, 84, 214), Color.FromArgb(86, 196, 92), Color.FromArgb(88, 206, 206), Color.FromArgb(232, 212, 88) };

		static readonly Color Wire = Color.FromArgb(255, 24, 84, 42);   // solid: the glow under it does not show through

		/// <summary>The lights round one window; the windows in front hide theirs. Size 1 = bulbs of 8 points.</summary>
		public static void Draw(Graphics g, Rectangle window, IEnumerable<Rectangle> inFront, double time, LightsMode mode, float size = 1)
		{
			var keep = g.Save();
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using (var hidden = new Region(new Rectangle(-100000, -100000, 200000, 200000)))
			{
				foreach (var r in inFront) hidden.Exclude(r);
				g.Clip = hidden;
			}
			float spacing = (Spacing > 0 ? Spacing : Measures(Shape).spacing) * size;
			var corners = new[] { new PointF(window.Left, window.Top), new PointF(window.Right, window.Top), new PointF(window.Right, window.Bottom), new PointF(window.Left, window.Bottom) };
			// where the bulbs hang, how they turn and how they shine now
			var bulbs = new List<(PointF at, float angle, Color color, float light)>();
			int index = 0;
			for (int e = 0; e < 4; e++)
			{
				var a = corners[e]; var b = corners[(e + 1) % 4];
				float len = Distance(a, b);
				int n = Math.Max(1, (int)(len / spacing));
				var (nx, ny) = Outward(e);
				for (int i = 0; i < n; i++, index++)
				{
					float t = (i + 0.5f) / n;
					var at = new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
					int seed = Hash(window.Left * 31 + window.Top, index);
					float tilt = (((seed >> 5) & 3) == 0 ? -1 : 1) * (38 + (seed & 31) / 31f * 30);   // lying along the wire, most the same way, a few turned back
					// lying: almost flat along the edge, on top of it (like the lights in the video), each a little turned its own way
					if (Shape == BulbShape.Lying) tilt = 52 + ((seed & 31) / 31f - 0.5f) * 22;
					float angle = (float)(Math.Atan2(ny, nx) * 180 / Math.PI) + tilt;
					float along = (float)(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI);
					if (RandomAngles)
					{
						// random: each bulb hangs its own way, a turn drawn from where it is (up to 45 degrees either way), its socket on the wire
						float wobble = (((seed >> 10) & 63) / 63f - 0.5f) * 90;
						angle = e == 2 ? 90 + wobble : along + wobble;
					}
					else if (Shape == BulbShape.Video)
					{
						// in order: on top and on the sides they follow the wire, one a little to each side; underneath they hang two by two in a V
						float side = i % 2 == 0 ? 1 : -1;
						angle = e == 2 ? 90 + side * 28 : along + side * (e == 0 ? 10 : 20);
					}
					var (color, light) = Shine(mode, index, seed, time * Speed);
					bulbs.Add((at, angle, color, light));
				}
			}
			// first the glows, then the wire over them (it keeps its colour while the bulbs blink), then the bulbs
			if (!NoBulbs) foreach (var x in bulbs) BulbFast(g, x.at, x.angle, x.color, x.light, size, glow: true);
			// the wire: along the edges, sagging a little between the bulbs
			using (var wire = new Pen(Wire, 1.9f * size))
				for (int e = 0; e < 4; e++)
				{
					var a = corners[e]; var b = corners[(e + 1) % 4];
					float len = Distance(a, b);
					int n = Math.Max(1, (int)(len / spacing));
					var (nx, ny) = Outward(e);
					var pts = new List<PointF>();
					for (int i = 0; i <= n * 2; i++)
					{
						float t = i / (float)(n * 2);
						float sag = (i % 2 == 1 ? 1.6f : 0) * size;   // half way between two bulbs the wire hangs a little
						pts.Add(new PointF(a.X + (b.X - a.X) * t - nx * sag * 0.3f, a.Y + (b.Y - a.Y) * t + (e == 0 ? sag : e == 2 ? -sag : 0)));
					}
					g.DrawCurve(wire, pts.ToArray(), 0.5f);
				}
			if (!NoBulbs) foreach (var x in bulbs) BulbFast(g, x.at, x.angle, x.color, x.light, size, glow: false);
			g.Restore(keep);
		}

		/// <summary>The colour of a bulb now and how bright it is (0..1, times the brightness chosen).</summary>
		internal static (Color color, float light) Shine(LightsMode mode, int index, int seed, double time)
		{
			var pattern = Pattern?.Length > 0 ? Pattern : VideoColors;
			int slot = RandomColors ? (seed >> 3) % pattern.Length : index % pattern.Length;
			Color own = Single ?? pattern[slot];
			float light = 1;
			switch (mode)
			{
				case LightsMode.Twinkle:
					// each bulb breathes at its own pace; now and then one goes almost dark
					double p = Math.Sin(time * (1.4 + (seed >> 6 & 7) * 0.25) + (seed & 255));
					light = (float)(0.3 + 0.7 * Math.Max(0, p));
					break;
				case LightsMode.Chase:
					light = index % 3 == (int)(time * 3) % 3 ? 1f : 0.3f;
					break;
				case LightsMode.Changing:
					// every bulb goes on to the next colour of the order, the wave running round the window (one colour: it breathes)
					if (Single != null) { light = 0.75f + 0.25f * (float)Math.Sin(time * 1.8 + index * 0.4); break; }
					double t = time * 0.7 + (RandomColors ? (seed & 255) / 41.0 : index * 0.2);
					int k = (int)Math.Floor(t);
					float f = (float)(t - k), blend = f < 0.75f ? 0 : (float)Math.Round((f - 0.75f) / 0.25f * 4) / 4;
					own = Mix(pattern[(slot + k) % pattern.Length], pattern[(slot + k + 1) % pattern.Length], blend);
					break;
			}
			return (own, Math.Clamp(light * (0.2f + 0.8f * Brightness), 0.08f, 1f));
		}

		// ------------------------------------------------------------------ bulbs drawn once, then put where they go

		static readonly Dictionary<(BulbShape, int, int, int, int, bool), (Bitmap bmp, float half)> sprites = new();

		/// <summary>
		/// A bulb quickly: the same bulb (shape, colour, brightness in 10 steps, angle in steps of 6 degrees, size) is drawn
		/// once into a little picture and that picture is put down after; the lights of a whole screen cost little.
		/// </summary>
		static void BulbFast(Graphics g, PointF at, float angle, Color color, float light, float size, bool glow)
		{
			int lightQ = (int)Math.Round(light * 5) * 2, angleQ = (int)Math.Round(((angle % 360) + 360) % 360 / 12) * 2, sizeQ = (int)Math.Round(size * 20);   // brightness in 6 steps, the angle every 12 degrees
			if (glow && lightQ / 10f <= 0.3f) return;   // a dim bulb has no glow
			var key = (Shape, color.ToArgb(), lightQ, angleQ, sizeQ, glow);
			if (!sprites.TryGetValue(key, out var s))
			{
				if (sprites.Count > 3000) { foreach (var x in sprites.Values) x.bmp.Dispose(); sprites.Clear(); }
				var (ml, _, _) = Measures(Shape);
				int box = (int)Math.Ceiling(ml * 1.8f * size + 6) * 2;
				var bmp = new Bitmap(box, box, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
				using (var sg = Graphics.FromImage(bmp)) { sg.SmoothingMode = SmoothingMode.AntiAlias; Bulb(sg, new PointF(box / 2f, box / 2f), angleQ * 6, color, lightQ / 10f, size, glow ? 1 : 2); }
				sprites[key] = s = (bmp, box / 2f);
			}
			// on the screen (no zoom) a plain copy, the quickest way; in the previews (zoomed) drawn to size
			if (g.Transform.IsIdentity) g.DrawImageUnscaled(s.bmp, (int)Math.Round(at.X - s.half), (int)Math.Round(at.Y - s.half));
			else g.DrawImage(s.bmp, at.X - s.half, at.Y - s.half, s.bmp.Width, s.bmp.Height);
		}

		/// <summary>One bulb: its glow, the socket on the wire and the glass, pointing at the angle (degrees, 0 = to the right).</summary>
		internal static void Bulb(Graphics g, PointF at, float angle, Color color, float light, float size, int parts = 3)
		{
			var keep = g.Save();
			g.TranslateTransform(at.X, at.Y);
			g.RotateTransform(angle);
			var (ml, mw, _) = Measures(Shape);
			float l = ml * size, w = mw * size;
			// the glow, round the glass, as strong as the bulb is bright
			if ((parts & 1) != 0 && light > 0.3f)
				using (var glowPath = new GraphicsPath())
				{
					if (Shape == BulbShape.Video) glowPath.AddEllipse(0, -w * 0.85f, l * 1.25f, w * 1.7f);   // the lights of the video hardly glow
					else glowPath.AddEllipse(-0.5f * size, -w * 1.2f, l * 1.6f, w * 2.4f);
					using var glow = new PathGradientBrush(glowPath) { CenterColor = Color.FromArgb((int)((Shape == BulbShape.Video ? 50 : 95) * light), color), SurroundColors = new[] { Color.FromArgb(0, color) } };
					g.FillPath(glow, glowPath);
				}
			if ((parts & 2) == 0) { g.Restore(keep); return; }
			Socket(g, w, size);
			using var glass = Glass(l, w, size);
			Color deep = Mix(color, Color.Black, 0.55f * (1 - light) + 0.12f), core = Mix(color, Color.White, 0.45f * light);
			using (var body = new PathGradientBrush(glass) { CenterPoint = new PointF(l * 0.55f, -w * 0.1f), CenterColor = core, SurroundColors = new[] { deep } })
				g.FillPath(body, glass);
			if (Shape == BulbShape.Video)   // a thin darker edge round the glass, as in the video
				using (var rim = new Pen(Mix(color, Color.Black, 0.45f), 0.8f * size)) g.DrawPath(rim, glass);
			// a glint
			if (light > 0.5f)
				using (var glint = new SolidBrush(Color.FromArgb((int)(170 * light), 255, 255, 255)))
					g.FillEllipse(glint, l * 0.42f, -w * 0.32f, l * 0.22f, w * 0.18f);
			g.Restore(keep);
		}

		/// <summary>The socket on the wire: dark green, with the ribs of the screw on the classic bulb.</summary>
		static void Socket(Graphics g, float w, float size)
		{
			using var socket = new SolidBrush(Color.FromArgb(255, 30, 72, 40));
			switch (Shape)
			{
				case BulbShape.Mini: g.FillRectangle(socket, -0.5f * size, -w * 0.5f, 4f * size, w); break;
				case BulbShape.Classic:
					g.FillRectangle(socket, -0.5f * size, -w * 0.33f, 4.2f * size, w * 0.66f);
					using (var rib = new Pen(Color.FromArgb(255, 18, 46, 26), 0.7f * size))
						for (int i = 1; i <= 2; i++) g.DrawLine(rib, i * 1.3f * size, -w * 0.33f, i * 1.3f * size, w * 0.33f);
					break;
				case BulbShape.Ball: g.FillRectangle(socket, -0.5f * size, -w * 0.22f, 2.6f * size, w * 0.44f); break;
				default: g.FillRectangle(socket, -0.5f * size, -w * 0.36f, 3f * size, w * 0.72f); break;
			}
		}

		/// <summary>The glass of the bulb, from its socket (x = 0) out to its tip (x = l).</summary>
		static GraphicsPath Glass(float l, float w, float size)
		{
			var p = new GraphicsPath();
			float s0 = 2.4f * size;
			switch (Shape)
			{
				case BulbShape.Mini:   // long and thin, a rounded tip (the "rice" bulbs)
					p.AddBezier(s0 + 1, -w * 0.5f, l * 0.6f, -w * 0.55f, l, -w * 0.42f, l, 0);
					p.AddBezier(l, 0, l, w * 0.42f, l * 0.6f, w * 0.55f, s0 + 1, w * 0.5f);
					break;
				case BulbShape.Classic:   // round by the socket, then narrowing to a point (the big old bulbs)
					p.AddBezier(s0 + 1, -w * 0.32f, s0 + 1, -w * 0.62f, l * 0.55f, -w * 0.55f, l, 0);
					p.AddBezier(l, 0, l * 0.55f, w * 0.55f, s0 + 1, w * 0.62f, s0 + 1, w * 0.32f);
					break;
				case BulbShape.Video:   // a Christmas bulb: narrower by the socket, full and round at the tip (an egg)
					p.AddBezier(s0, -w * 0.3f, l * 0.35f, -w * 0.5f, l * 0.6f, -w * 0.56f, l * 0.82f, -w * 0.4f);
					p.AddBezier(l * 0.82f, -w * 0.4f, l * 1.04f, -w * 0.22f, l * 1.04f, w * 0.22f, l * 0.82f, w * 0.4f);
					p.AddBezier(l * 0.82f, w * 0.4f, l * 0.6f, w * 0.56f, l * 0.35f, w * 0.5f, s0, w * 0.3f);
					break;
				case BulbShape.Lying:   // a long, full jelly bean, round at both ends
					p.AddBezier(s0, -w * 0.42f, l * 0.3f, -w * 0.66f, l * 0.95f, -w * 0.6f, l, 0);
					p.AddBezier(l, 0, l * 0.95f, w * 0.6f, l * 0.3f, w * 0.66f, s0, w * 0.42f);
					break;
				case BulbShape.Ball:   // a little globe on a short neck
					p.AddEllipse(s0, -w / 2, l - s0, w);
					break;
				default:   // long and rounded at both ends, a little narrower at the tip (like a jelly bean)
					p.AddBezier(s0, -w * 0.4f, l * 0.35f, -w * 0.62f, l * 0.9f, -w * 0.5f, l, 0);
					p.AddBezier(l, 0, l * 0.9f, w * 0.5f, l * 0.35f, w * 0.62f, s0, w * 0.4f);
					break;
			}
			p.CloseFigure();
			return p;
		}

		static (float nx, float ny) Outward(int edge) => edge switch { 0 => (0, -1), 1 => (1, 0), 2 => (0, 1), _ => (-1, 0) };
		static float Distance(PointF a, PointF b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
		static Color Mix(Color a, Color b, float k) => Color.FromArgb((int)(a.R + (b.R - a.R) * k), (int)(a.G + (b.G - a.G) * k), (int)(a.B + (b.B - a.B) * k));

		static int Hash(int a, int b)
		{
			unchecked
			{
				uint h = (uint)a * 374761393u + (uint)b * 668265263u;
				h = (h ^ (h >> 13)) * 1274126177u;
				return (int)((h ^ (h >> 16)) & 0x7FFFFFFF);
			}
		}

		// ------------------------------------------------------------------ a picture to compare (before deciding)

		/// <summary>
		/// The corner of a window with its lights, as small as in a video of a 1920-point screen shown at 1280 (the same
		/// size as the frame of the video it is compared with): to put both side by side at the same zoom.
		/// </summary>
		public static void Corner(string file)
		{
			using var bmp = new Bitmap(440, 250);
			using var g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 440, 250), Color.FromArgb(22, 40, 62), Color.FromArgb(30, 46, 70), 90f)) g.FillRectangle(sky, 0, 0, 440, 250);
			g.ScaleTransform(1280f / 1920f, 1280f / 1920f);
			var window = new Rectangle(30, 30, 600, 300);
			using (var body = new SolidBrush(Color.FromArgb(32, 33, 38))) g.FillRectangle(body, window);
			using (var bar = new SolidBrush(Color.FromArgb(44, 46, 52))) g.FillRectangle(bar, window.X, window.Y, window.Width, 40);
			Draw(g, window, Array.Empty<Rectangle>(), 0, LightsMode.Steady);
			bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
		}

		/// <summary>The same window with its lights at several distances between the bulbs, at real size and zoomed, side by side.</summary>
		public static void SpacingPreview(string file)
		{
			float[] gaps = { 20, 25, 30 };
			string[] names = { "A · Gomo", "B · Mini", "C · Clássica", "D · Bolinha", "E · Deitada", "F · Pisca-pisca de Natal" };
			int cw = 300, ch = 160, left = 190, top = 70;
			using var bmp = new Bitmap(left + 6 * (cw + 16), top + 6 * (ch + 16));
			using var g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.Clear(Color.FromArgb(24, 26, 32));
			using var font = new Font("Segoe UI", 13, FontStyle.Bold);
			var keepShape = Shape; var keepGap = Spacing; var keepAngles = RandomAngles;
			int col = 0;
			foreach (bool random in new[] { false, true })
				foreach (float gap in gaps)
				{
					int x = left + col++ * (cw + 16);
					g.DrawString($"{gap:0} pontos", font, Brushes.White, x, 8);
					g.DrawString(random ? "ângulos aleatórios" : "ângulos organizados", font, random ? Brushes.Gold : Brushes.LightSkyBlue, x, 32);
					for (int s = 0; s < 6; s++)
					{
						Shape = (BulbShape)s; Spacing = gap; RandomAngles = random;
						int y = top + s * (ch + 16);
						if (col == 1) g.DrawString(names[s], font, Brushes.White, 12, y + ch / 2 - 10);
						using (var sky = new SolidBrush(Color.FromArgb(22, 40, 62))) g.FillRectangle(sky, x, y, cw, ch);
						var win = new Rectangle(x + 24, y + 22, cw - 48, ch - 44);
						using (var body = new SolidBrush(Color.FromArgb(32, 33, 38))) g.FillRectangle(body, win);
						Draw(g, win, Array.Empty<Rectangle>(), 0, LightsMode.Steady);
					}
				}
			Shape = keepShape; Spacing = keepGap; RandomAngles = keepAngles;
			bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
		}

		/// <summary>For each shape of bulb, the corner of a window as small as in the video (corner-&lt;shape&gt;.png), to compare.</summary>
		public static void Corners(string folder)
		{
			Directory.CreateDirectory(folder);
			var keep = Shape;
			foreach (var s in Enum.GetValues<BulbShape>()) { Shape = s; Corner(Path.Combine(folder, "corner-" + s + ".png")); }
			Shape = keep;
		}

		/// <summary>Two windows, one in front of the other, in each way of shining; and up close.</summary>
		public static void Preview(string file)
		{
			Corner(Path.ChangeExtension(file, null) + "-canto.png");
			int w = 1100, h = 1020;
			using var bmp = new Bitmap(w, h);
			using var g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using (var sky = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(28, 34, 58), Color.FromArgb(60, 46, 84), 90f)) g.FillRectangle(sky, 0, 0, w, h);
			using var font = new Font("Segoe UI", 14, FontStyle.Bold);
			void Window(Rectangle r)
			{
				using var body = new SolidBrush(Color.FromArgb(32, 33, 38));
				using var bar = new SolidBrush(Color.FromArgb(46, 48, 54));
				g.FillRectangle(body, r); g.FillRectangle(bar, r.X, r.Y, r.Width, 26);
				foreach (var (i, c) in new[] { (0, Color.FromArgb(255, 95, 87)), (1, Color.FromArgb(254, 188, 46)), (2, Color.FromArgb(40, 200, 64)) })
					using (var dot = new SolidBrush(c)) g.FillEllipse(dot, r.X + 10 + i * 18, r.Y + 8, 10, 10);
			}
			var modes = new (string name, LightsMode mode, double time, Color? single)[]
			{
				("Fixas", LightsMode.Steady, 0, null), ("Pisca-pisca", LightsMode.Twinkle, 1.3, null), ("Cores que mudam", LightsMode.Changing, 2.9, null),
				("Correndo", LightsMode.Chase, 0.1, null), ("Uma cor (vermelho)", LightsMode.Steady, 0, Color.FromArgb(226, 56, 66)), ("Uma cor (branco quente)", LightsMode.Steady, 0, Color.FromArgb(255, 214, 140)),
			};
			for (int m = 0; m < modes.Length; m++)
			{
				int x = 40 + (m % 2) * 540, y = 40 + (m / 2) * 230;
				g.DrawString(modes[m].name, font, Brushes.White, x, y);
				Single = modes[m].single;
				var back = new Rectangle(x + 10, y + 50, 300, 140);
				var front = new Rectangle(x + 230, y + 95, 240, 110);
				Window(back);
				Draw(g, back, new[] { front }, modes[m].time, modes[m].mode);
				Window(front);
				Draw(g, front, Array.Empty<Rectangle>(), modes[m].time, modes[m].mode);
				Single = null;
			}
			// up close: a corner, three times bigger
			g.DrawString("De perto (3×)", font, Brushes.White, 40, 730);
			using (var close = new Bitmap(340, 90))
			{
				using (var cg = Graphics.FromImage(close))
				{
					cg.Clear(Color.FromArgb(28, 34, 58));
					cg.SmoothingMode = SmoothingMode.AntiAlias;
					var r = new Rectangle(20, 30, 400, 200);
					using (var body = new SolidBrush(Color.FromArgb(32, 33, 38))) cg.FillRectangle(body, r);
					Draw(cg, r, Array.Empty<Rectangle>(), 0, LightsMode.Steady);
				}
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.DrawImage(close, new Rectangle(40, 770, 1020, 240));
			}
			bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
		}
	}
}
