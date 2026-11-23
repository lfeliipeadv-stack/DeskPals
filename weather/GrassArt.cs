using System.Drawing;
using System.Drawing.Drawing2D;

namespace DeskPalsWeather
{
	/// <summary>
	/// Grass like a small lawn on top of the windows: tufts of thin blades fanning out from a point, wide at the foot and
	/// pointed at the tip, most of them short and a few much taller; a darker row behind and a bright one in front; each
	/// blade dark olive at the foot, bright yellow-green in the middle and light at the tip; a thin line of soil under it.
	/// Every blade leans its own way and bends with the wind and the gusts that run along the screen. The same place
	/// always grows the same grass (it comes from where it is, not from chance).
	/// </summary>
	internal static class GrassArt
	{
		public static readonly Color Own = Color.FromArgb(104, 170, 52);

		/// <summary>The grass on one edge, from x0 to x1, its foot on baseY; height 0..1, wind 0..1, time in seconds.</summary>
		public static void Draw(Graphics g, float x0, float x1, float baseY, double height, double wind, double time, Color color)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			float tall = (float)(12 + 26 * height);
			// the soil: a band of brown earth with an uneven top, lighter clods in it and a dark foot
			float depth = (float)(5 + 3 * height);
			using (var path = new GraphicsPath())
			{
				var top = new List<PointF> { new(x0, baseY) };
				for (float x = x0; x <= x1; x += 4)
					top.Add(new PointF(x, baseY - depth - ((Hash((int)x, 7) & 7) / 7f) * 1.6f));
				top.Add(new PointF(x1, baseY - depth)); top.Add(new PointF(x1, baseY));
				path.AddPolygon(top.ToArray());
				using var soil = new LinearGradientBrush(new PointF(0, baseY - depth - 2), new PointF(0, baseY + 0.5f), Color.FromArgb(245, 128, 84, 46), Color.FromArgb(250, 62, 38, 22));
				g.FillPath(soil, path);
			}
			using (var clod = new SolidBrush(Color.FromArgb(200, 156, 108, 64)))
			using (var dark = new SolidBrush(Color.FromArgb(190, 48, 30, 18)))
				for (float x = x0 + 2; x < x1 - 2; x += 5)
				{
					int s = Hash((int)x, 11);
					float cy = baseY - 1 - (s & 15) / 15f * (depth - 1.5f), r = 0.8f + ((s >> 4) & 3) * 0.35f;
					g.FillEllipse((s >> 6 & 1) == 0 ? clod : dark, x + ((s >> 7) & 3), cy - r, r * 2, r * 1.5f);
				}
			baseY -= depth - 1.5f;   // the grass grows out of the top of the earth, which shows whole under it
			// two rows: behind (darker, a little shorter), in front (bright)
			for (int row = 0; row < 2; row++)
			{
				bool back = row == 0;
				float step = back ? 6 : 5;
				for (float tx = Snap(x0, step, row); tx < x1; tx += step)
				{
					int seed = Hash((int)tx, row);
					float x = tx + (seed & 7) - 3.5f;
					x = Math.Clamp(x, x0 + 0.5f, x1 - 0.5f);   // a tuft by the edge stays on it (no gap where two pieces meet)
					int blades = 5 + (seed >> 3) % 6;
					for (int b = 0; b < blades; b++)
					{
						int s = Hash(seed, b);
						float spread = ((s & 31) / 31f - 0.5f) * 2;                    // -1..1, the way it leans
						float h = tall * (0.45f + ((s >> 5) & 31) / 31f * 0.55f);
						if (((s >> 10) & 15) == 0) h *= 1.55f;                           // now and then a tall one
						if (back) h *= 0.8f;
						float w = 1.0f + ((s >> 14) & 7) / 7f * 0.9f;
						float foot = x + spread * 2.5f;
						// the wind: every blade the same way, more in the gusts that run along the screen, and a little flutter of its own
						double gust = Math.Sin(time * 2.0 - foot * 0.011) * 0.5 + 0.5;
						// (its rhythm a whole multiple of the gusts': the pictures kept for the screen loop without a jump)
						double flutter = Math.Sin(time * 2.0 * (1 + (s & 1)) + (s & 63)) * 0.06;
						float lean = spread * 0.2f + (float)((wind * 0.6 + 0.03) * (0.35 + 0.65 * gust) + flutter);
						Blade(g, foot, baseY - 1, h, w, lean, color, back ? 0.62f : 1f, s);
					}
				}
			}
		}

		// ------------------------------------------------------------------ on the screen: pictures kept, not every blade every time

		const int TileWidth = 192, Margin = 24, Frames = 24, Variants = 3;
		/// <summary>One sway of the gusts (they turn at 2 a second): the frames kept cover it, then it starts again.</summary>
		const double Period = Math.PI;
		/// <summary>The pieces kept for one lawn (each lawn of another kind keeps its own).</summary>
		internal sealed class Tiles
		{
			public readonly Dictionary<(int variant, int frame), Bitmap> Kept = new();
			public (int wind, int height, int color) For;
			public int Height, Foot;
		}
		static readonly Tiles Shared = new();

		/// <summary>
		/// The grass on one edge, quickly: drawn once into pieces (192 points wide, three different ones, 24 moments of the
		/// sway of the wind) and those pieces put side by side. The wind is kept in five steps; a piece is made the first
		/// time it is needed. Neighbouring pieces sway a little apart, so the gusts still run along the edge.
		/// </summary>
		public static void DrawFast(Graphics g, float x0, float x1, float baseY, double height, double wind, double time, Color color, Tiles kept = null)
		{
			kept ??= Shared;
			var key = ((int)Math.Round(Math.Clamp(wind, 0, 1) * 4), (int)Math.Round(Math.Clamp(height, 0, 1) * 10), color.ToArgb());
			if (key != kept.For || kept.Height == 0)
			{
				foreach (var b in kept.Kept.Values) b.Dispose();
				kept.Kept.Clear();
				kept.For = key;
				kept.Height = (int)((12 + 26 * key.Item2 / 10.0) * 1.7 + 5 + 3 * key.Item2 / 10.0 + 10);
				kept.Foot = 3;
			}
			int frame = (int)(time / Period * Frames);
			var keep = g.Save();
			// (inside the part being drawn again, never over it: what is outside was not cleared)
			// and only over the edge itself: no grass hanging in the air past the ends of the window
			g.SetClip(new RectangleF(x0, baseY - kept.Height, x1 - x0, kept.Height + kept.Foot + 1), CombineMode.Intersect);
			g.InterpolationMode = InterpolationMode.NearestNeighbor;
			g.CompositingMode = CompositingMode.SourceOver;
			int n = 0;
			for (float x = x0; x < x1; x += TileWidth, n++)
			{
				int variant = Hash(n, (int)x1) % Variants, f = ((frame + n * 3) % Frames + Frames) % Frames;
				var tile = Tile(kept, variant, f, key.Item1 / 4.0, key.Item2 / 10.0, color);
				float w = Math.Min(TileWidth, x1 - x) + Margin * 2;
				g.DrawImage(tile, new RectangleF(x - Margin, baseY - kept.Height + kept.Foot, w, kept.Height), new RectangleF(0, 0, w, kept.Height), GraphicsUnit.Pixel);
			}
			g.Restore(keep);
		}

		static Bitmap Tile(Tiles kept, int variant, int frame, double wind, double height, Color color)
		{
			if (kept.Kept.TryGetValue((variant, frame), out var b)) return b;
			b = new Bitmap(TileWidth + Margin * 2, kept.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
			using (var g = Graphics.FromImage(b))
			{
				g.TranslateTransform(Margin - variant * 1000, 0);   // each variant grows from another stretch of the same lawn
				Draw(g, variant * 1000, variant * 1000 + TileWidth, kept.Height - kept.Foot, height, wind, frame * Period / Frames, color);
			}
			kept.Kept[(variant, frame)] = b;
			return b;
		}

		/// <summary>One blade: a curved, pointed leaf from its foot; lean 0 is straight up, 1 bends it far to the right.</summary>
		static void Blade(Graphics g, float fx, float fy, float h, float w, float lean, Color color, float light, int seed)
		{
			float tipX = fx + lean * h * 0.95f, tipY = fy - h * (1 - Math.Min(0.45f, Math.Abs(lean) * 0.38f));
			float midX = fx + lean * h * 0.28f, midY = fy - h * 0.55f;
			using var path = new GraphicsPath();
			path.AddBezier(fx - w / 2, fy, midX - w * 0.35f, midY, tipX - w * 0.1f, tipY + h * 0.12f, tipX, tipY);
			path.AddBezier(tipX, tipY, tipX + w * 0.1f, tipY + h * 0.12f, midX + w * 0.35f, midY, fx + w / 2, fy);
			path.CloseFigure();
			// dark olive at the foot → bright in the middle → light, a little yellow, at the tip
			float k = light * (0.9f + ((seed >> 18) & 7) / 7f * 0.2f);
			Color footC = Tone(color, -0.55f, k), midC = Tone(color, 0.05f, k), tipC = Tone(Yellower(color), 0.35f, k);
			// (GDI+ refuses a gradient a hair off the vertical: a blade almost straight up gets one exactly vertical)
			float gradX = Math.Abs(tipX - fx) < 0.05f ? fx : tipX;
			using var brush = new LinearGradientBrush(new PointF(fx, fy + 0.5f), new PointF(gradX, tipY - 0.5f), footC, tipC)
			{
				InterpolationColors = new ColorBlend { Colors = new[] { footC, midC, tipC }, Positions = new[] { 0f, 0.45f, 1f } },
			};
			g.FillPath(brush, path);
		}

		static Color Tone(Color c, float amount, float light)
		{
			float r = c.R, g = c.G, b = c.B;
			if (amount < 0) { r *= 1 + amount; g *= 1 + amount; b *= 1 + amount; }
			else { r += (255 - r) * amount; g += (255 - g) * amount; b += (255 - b) * amount; }
			return Color.FromArgb(240, Math.Clamp((int)(r * light), 0, 255), Math.Clamp((int)(g * light), 0, 255), Math.Clamp((int)(b * light), 0, 255));
		}

		static Color Yellower(Color c) => Color.FromArgb(Math.Min(255, c.R + 40), Math.Min(255, c.G + 15), Math.Max(0, c.B - 20));

		static float Snap(float x, float step, int row) => (float)Math.Floor(x / step) * step + row * 3;

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

		/// <summary>The grass of now and the new one, on dark and light windows, at three moments of the wind.</summary>
		public static void Preview(string file)
		{
			int w = 1100, h = 620;
			using var bmp = new Bitmap(w, h);
			using var g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using (var sky = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(38, 44, 62), Color.FromArgb(70, 60, 90), 90f)) g.FillRectangle(sky, 0, 0, w, h);
			using var font = new Font("Segoe UI", 15, FontStyle.Bold);
			using var small = new Font("Segoe UI", 11);
			void Window(int x, int y, int ww, int hh, bool dark)
			{
				using var body = new SolidBrush(dark ? Color.FromArgb(32, 33, 38) : Color.FromArgb(246, 246, 248));
				using var bar = new SolidBrush(dark ? Color.FromArgb(44, 46, 52) : Color.FromArgb(228, 230, 234));
				g.FillRectangle(body, x, y, ww, hh); g.FillRectangle(bar, x, y, ww, 30);
				foreach (var (i, c) in new[] { (0, Color.FromArgb(255, 95, 87)), (1, Color.FromArgb(254, 188, 46)), (2, Color.FromArgb(40, 200, 64)) })
					using (var dot = new SolidBrush(c)) g.FillEllipse(dot, x + 12 + i * 20, y + 9, 12, 12);
			}
			g.DrawString("Hoje", font, Brushes.White, 30, 18);
			g.DrawString("Novo modelo (como no vídeo)", font, Brushes.White, 30, 218);
			g.DrawString("vento fraco · vento médio · rajada forte", small, Brushes.Gainsboro, 30, 420);
			// now: the blades of today
			Window(30, 120, 1040, 70, true);
			Overlay.DrawGrass(g, new() { (30, 1070, 120) }, Point.Empty, 1, 0.6, 0.3, Color.FromArgb(78, 150, 60));
			// the new grass, on a dark and a light window
			Window(30, 300, 500, 90, true);
			Window(570, 300, 500, 90, false);
			Draw(g, 30, 530, 300, 0.6, 0.3, 1, Own);
			Draw(g, 570, 1070, 300, 0.6, 0.3, 1, Own);
			// three moments of the wind, side by side
			for (int i = 0; i < 3; i++)
			{
				int x = 30 + i * 350;
				Window(x, 500, 320, 90, true);
				Draw(g, x, x + 320, 500, 0.75, new[] { 0.1, 0.45, 0.95 }[i], 2.3, Own);
			}
			bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
			// and up close, three times bigger, like the crop of the video
			using var close = new Bitmap(1040, 200);
			using (var cg = Graphics.FromImage(close))
			{
				cg.InterpolationMode = InterpolationMode.NearestNeighbor;
				cg.DrawImage(bmp, new Rectangle(0, 0, 1040, 200), new Rectangle(40, 255, 347, 67), GraphicsUnit.Pixel);
			}
			close.Save(Path.ChangeExtension(file, null) + "-perto.png", System.Drawing.Imaging.ImageFormat.Png);
		}
	}
}
