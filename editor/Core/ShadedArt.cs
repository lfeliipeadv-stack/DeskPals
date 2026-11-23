using System.Drawing;
using System.Drawing.Imaging;

namespace PetEditor
{
	/// <summary>
	/// Shaded pixel art in the eSheep style (the drawing of the Pato pet): parts are ellipses of a color; then each part
	/// gets one light from the top left (a bright rim on top, a shadow band at the bottom right) and an outline in the
	/// darkest tone of its own color, never black. The "body" parts (head, neck, body, tail) melt into one shape; any other
	/// tag (wing, ear, leg, a hat) keeps its own rim and outline. Any color works: the four tones are worked out from it.
	/// </summary>
	internal sealed class ShadedArt
	{
		public readonly int W, H;
		readonly Cell[,] cells;
		sealed record Cell(Color Base, int Shift, string Tag);

		public ShadedArt(int w, int h) { W = w; H = h; cells = new Cell[w, h]; }

		Cell Get(int x, int y) => x < 0 || y < 0 || x >= W || y >= H ? null : cells[x, y];
		void Set(int x, int y, Cell c) { if (x >= 0 && y >= 0 && x < W && y < H) cells[x, y] = c; }

		/// <summary>An ellipse of a color; <paramref name="shift"/> 1 = a tone darker (a wing over the body); ang turns it clockwise.</summary>
		public ShadedArt Ell(double cx, double cy, double rx, double ry, Color color, int shift = 0, string tag = "body", double ang = 0)
		{
			double a = ang * Math.PI / 180, cos = Math.Cos(a), sin = Math.Sin(a);
			int r = (int)Math.Ceiling(Math.Max(rx, ry)) + 1;
			for (int y = (int)cy - r; y <= (int)cy + r; y++)
				for (int x = (int)cx - r; x <= (int)cx + r; x++)
				{
					double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
					double u = (dx * cos + dy * sin) / rx, v = (-dx * sin + dy * cos) / ry;
					if (u * u + v * v <= 1) Set(x, y, new Cell(color, shift, tag));
				}
			return this;
		}

		/// <summary>A rectangle of a color (books, mugs, handles), corners included.</summary>
		public ShadedArt Rect(double x0, double y0, double x1, double y1, Color color, int shift = 0, string tag = "thing")
		{
			for (int y = (int)Math.Round(y0); y <= (int)Math.Round(y1); y++)
				for (int x = (int)Math.Round(x0); x <= (int)Math.Round(x1); x++)
					Set(x, y, new Cell(color, shift, tag));
			return this;
		}

		/// <summary>Takes away the cells of one part from this row down (an ellipse cut in half: the dome of an umbrella).</summary>
		public ShadedArt CutBelow(string tag, int y)
		{
			for (int yy = Math.Max(0, y); yy < H; yy++)
				for (int x = 0; x < W; x++)
					if (cells[x, yy]?.Tag == tag) cells[x, yy] = null;
			return this;
		}

		static readonly HashSet<string> Soft = new() { "wing", "wing2", "bill", "bill2", "foot", "foot2", "leg", "leg2", "leg3", "leg4", "ear", "ear2" };

		/// <summary>The four tones of a color: 0 light, 1 the color, 2 shadow, 3 outline.</summary>
		public static Color Tone(Color c, int t) => t switch
		{
			0 => Mix(c, Color.White, 0.42),
			1 => c,
			2 => Mix(c, Color.Black, 0.22),
			_ => Mix(c, Color.Black, 0.62),
		};

		public static Color Mix(Color a, Color b, double k) =>
			Color.FromArgb(255, (int)Math.Round(a.R + (b.R - a.R) * k), (int)Math.Round(a.G + (b.G - a.G) * k), (int)Math.Round(a.B + (b.B - a.B) * k));

		/// <summary>The drawing, shaded and outlined (transparent background).</summary>
		public Bitmap Render()
		{
			var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
			for (int y = 0; y < H; y++)
				for (int x = 0; x < W; x++)
				{
					var c = cells[x, y];
					if (c == null) continue;
					string tag = c.Tag;
					bool Same(int dx, int dy)
					{
						var q = Get(x + dx, y + dy);
						return q != null && (q.Tag == tag || (tag == "body" && !q.Tag.StartsWith("bill") && q.Tag != "mouth"));
					}
					int b = Math.Min(2, 1 + c.Shift), t = b;
					if (!Same(-1, -1) || !Same(0, -1) || !Same(0, -2)) t = Math.Max(0, b - 1);
					else if (!Same(1, 2) || !Same(2, 2) || !Same(0, 3) || !Same(3, 1)) t = 2;
					bool edge = false, inner = false;
					foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
					{
						var q = Get(x + dx, y + dy);
						if (q == null) edge = true;
						else if (q.Tag != tag && tag != "body" && tag != "mouth" && q.Tag != "mouth" && (!Soft.Contains(tag) || dx == 1 || dy == 1)) inner = true;
					}
					bmp.SetPixel(x, y, Tone(c.Base, edge || inner ? 3 : t));
				}
			return bmp;
		}
	}
}
