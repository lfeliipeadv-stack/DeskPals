using System.Drawing;
using System.Drawing.Drawing2D;
using DesktopPet;

namespace DeskPalsWeather
{
	/// <summary>
	/// A small picture of a scene, for its card: the sky of its weather, what falls (flakes, drops, leaves or petals, in
	/// the scene's colours), the sun in a corner, fog over it and grass at the bottom, each as strong as in the scene.
	/// </summary>
	internal static class SceneArt
	{
		public static void Draw(Graphics g, Rectangle box, Scene s, Config unused = null)
		{
			var keep = g.Save();
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using var frame = Theme.RoundedRect(box, 8);
			g.SetClip(frame);
			var (top, bottom) = s.Kind switch
			{
				Kind.Snow => (Color.FromArgb(58, 74, 112), Color.FromArgb(140, 160, 196)),
				Kind.Rain => (Color.FromArgb(70, 80, 96), Color.FromArgb(128, 138, 154)),
				Kind.Leaves => (Color.FromArgb(232, 160, 84), Color.FromArgb(250, 222, 166)),
				Kind.Blossom => (Color.FromArgb(244, 196, 214), Color.FromArgb(255, 240, 246)),
				_ => s.Fog > s.Sun ? (Color.FromArgb(172, 180, 192), Color.FromArgb(214, 218, 226)) : (Color.FromArgb(104, 180, 246), Color.FromArgb(196, 228, 255)),
			};
			using (var sky = new LinearGradientBrush(box, top, bottom, 90f)) g.FillRectangle(sky, box);
			float z = box.Height / 48f;
			if (s.Sun > 0)
			{
				float r = 9 * z;
				using var sun = new SolidBrush(Color.FromArgb(Math.Clamp(110 + s.Sun * 14 / 10, 0, 255), 255, 214, 90));
				g.FillEllipse(sun, box.Right - r * 2.6f, box.Top + r * 0.5f, r * 2, r * 2);
			}
			// what falls, scattered the same way each time
			var rnd = new Random(s.Kind.GetHashCode() * 31 + s.Intensity);
			int count = s.Kind == Kind.None ? 0 : 6 + s.Intensity / 6;
			Color own = s.Kind switch { Kind.Snow => Color.White, Kind.Rain => Color.FromArgb(200, 226, 255), Kind.Leaves => Color.FromArgb(200, 70, 30), Kind.Blossom => Color.FromArgb(236, 120, 160), _ => Color.White };
			if (s.Colors != null && s.Colors.TryGetValue(Controller.Name(s.Kind == Kind.None ? Kind.Snow : s.Kind), out int c)) own = Color.FromArgb((c >> 16) & 255, (c >> 8) & 255, c & 255);
			using var fill = new SolidBrush(own);
			using var line = new Pen(own, 1.3f * z);
			for (int i = 0; i < count; i++)
			{
				float x = box.Left + (float)rnd.NextDouble() * box.Width, y = box.Top + (float)rnd.NextDouble() * box.Height;
				switch (s.Kind)
				{
					case Kind.Snow: float d = (1.6f + (float)rnd.NextDouble() * 2.2f) * z; g.FillEllipse(fill, x, y, d, d); break;
					case Kind.Rain: g.DrawLine(line, x, y, x - 2 * z, y + 6 * z); break;
					default:
						var st = g.Save();
						g.TranslateTransform(x, y); g.RotateTransform(rnd.Next(360));
						g.FillEllipse(fill, -3 * z, -1.6f * z, 6 * z, 3.2f * z);
						g.Restore(st);
						break;
				}
			}
			if (s.Grass > 0)
			{
				float h = (3 + s.Grass / 14f) * z;
				using var grass = new SolidBrush(s.Colors != null && s.Colors.TryGetValue("grass", out int gc) ? Color.FromArgb((gc >> 16) & 255, (gc >> 8) & 255, gc & 255) : Color.FromArgb(78, 150, 60));
				g.FillRectangle(grass, box.Left, box.Bottom - h, box.Width, h);
			}
			if (s.Fog > 0)
			{
				using var fog = new SolidBrush(Color.FromArgb(Math.Clamp((int)(s.Fog * 2.2), 0, 220), 236, 238, 242));
				g.FillRectangle(fog, box);
			}
			g.Restore(keep);
			Theme.DrawRounded(g, Theme.Border, box, 8);
		}
	}
}
