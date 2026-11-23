using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DeskPalsWeather
{
	/// <summary>
	/// Fog that drifts across the screen, warm sun rays from a corner, and grass growing along the top of the windows and
	/// the taskbar, bending in the wind and in gusts. Windows over each screen with real transparency (layered windows
	/// drawn with their own alpha) that the mouse goes through: one for the sky (fog and sun) and one for the grass. They
	/// sit just behind the window in use (over the others and the desktop), or over everything; they stay out of
	/// screenshots unless asked. To spare the processor the sky is drawn three times smaller and stretched (fog and light
	/// are soft anyway) five times a second; the grass, which moves more, a dozen times, and only its strips are sent to
	/// Windows; on battery both go slower.
	/// </summary>
	internal sealed class Overlay : Form
	{
		/// <summary>How much of each, 0..1, and their colours (null: their own), set by the controller.</summary>
		public static double Fog, Sun, Grass, Wind;
		public static Color? FogColor, SunColor, GrassColor;
		public static bool OnTop, InScreenshots, Slow, Lights;
		public static LightsMode LightsMode;

		static readonly Color FogOwn = Color.FromArgb(236, 238, 242), SunOwn = Color.FromArgb(255, 214, 120), GrassOwn = GrassArt.Own;
		const int Down = 3;

		readonly Rectangle screen;
		readonly bool sky, topmost, lights;
		string framesBefore = "";
		readonly System.Windows.Forms.Timer tick = new() { Interval = 80 };
		readonly Random rnd = new(7);
		readonly (float x, float y, float r, float speed)[] clouds;
		Bitmap fogBlob, small;
		Graphics smallGfx;
		Color blobColor;
		double blobFog;
		List<(int x0, int x1, int y)> tops = new();
		DateTime topsAt = DateTime.MinValue;
		double time;
		Rectangle drawnBefore;
		bool presented;

		public Overlay(Screen s, bool sky, bool lights = false)
		{
			screen = s.Bounds;
			this.sky = sky;
			this.lights = lights;
			topmost = OnTop;
			FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Bounds = screen; Text = "";
			clouds = Enumerable.Range(0, 9).Select(i => ((float)rnd.NextDouble() * screen.Width, (float)(0.15 + rnd.NextDouble() * 0.75) * screen.Height, (float)(260 + rnd.NextDouble() * 360), (float)(0.15 + rnd.NextDouble() * 0.35))).ToArray();
			tick.Tick += (o, e) => Frame();
			FormClosed += (o, e) => { tick.Stop(); tick.Dispose(); fogBlob?.Dispose(); smallGfx?.Dispose(); small?.Dispose(); FreeCanvas(); };
		}

		protected override bool ShowWithoutActivation => true;
		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				cp.ExStyle |= 0x80 | 0x08000000 | 0x80000 | 0x20;   // tool, no activate, layered, click-through
				if (topmost) cp.ExStyle |= 0x08;                   // over every window
				return cp;
			}
		}

		protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Shots.Apply(Handle); }
		protected override void OnShown(EventArgs e) { base.OnShown(e); tick.Start(); Frame(); }

		// ------------------------------------------------------------------ one picture, kept for good

		[StructLayout(LayoutKind.Sequential)] struct BITMAPINFOHEADER { public int biSize, biWidth, biHeight; public short biPlanes, biBitCount; public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant; }
		[DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
		[DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
		[DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
		[DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
		[DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
		[DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
		[DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);

		IntPtr memDc, dib, oldObject, bits;
		Bitmap canvas;
		Graphics gfx;

		void MakeCanvas()
		{
			IntPtr screenDc = GetDC(IntPtr.Zero);
			memDc = CreateCompatibleDC(screenDc);
			ReleaseDC(IntPtr.Zero, screenDc);
			var info = new BITMAPINFOHEADER { biSize = 40, biWidth = screen.Width, biHeight = -screen.Height, biPlanes = 1, biBitCount = 32 };
			dib = CreateDIBSection(memDc, ref info, 0, out bits, IntPtr.Zero, 0);
			oldObject = SelectObject(memDc, dib);
			canvas = new Bitmap(screen.Width, screen.Height, screen.Width * 4, PixelFormat.Format32bppPArgb, bits);
			gfx = Graphics.FromImage(canvas);
			if (sky)
			{
				small = new Bitmap(Math.Max(1, screen.Width / Down), Math.Max(1, screen.Height / Down), PixelFormat.Format32bppPArgb);
				smallGfx = Graphics.FromImage(small);
			}
		}

		void FreeCanvas()
		{
			gfx?.Dispose(); canvas?.Dispose(); gfx = null; canvas = null;
			if (memDc != IntPtr.Zero) { SelectObject(memDc, oldObject); DeleteObject(dib); DeleteDC(memDc); memDc = IntPtr.Zero; }
		}

		void Frame()
		{
			int interval = (lights ? (LightsMode == LightsMode.Steady ? 300 : 165) : sky ? 200 : 80) * (Slow ? 2 : 1);
			if (tick.Interval != interval) tick.Interval = interval;
			time += interval / 1000.0;
			if (!topmost) KeepBehindFront();
			if (canvas == null) MakeCanvas();
			if (lights) FrameLights(); else if (sky) FrameSky(); else FrameGrass();
		}

		/// <summary>
		/// The Christmas lights round the windows, the ones in front hiding the lights of those behind. Steady, they are drawn
		/// again only when a window moves; blinking, about nine times a second. Only round the windows is sent to Windows.
		/// </summary>
		void FrameLights()
		{
			var frames = DesktopWindows.Frames(screen);
			string key = LightsArt.Version + "|" + string.Join(";", frames);
			if (LightsMode == LightsMode.Steady && key == framesBefore && presented) return;
			framesBefore = key;
			// a window in full screen (a game, a film) gets no lights round it: they would shine along the edges of the
			// screen, over it; it still hides the lights of the windows behind it
			var local = frames.Select(f => (r: new Rectangle(f.bounds.X - screen.X, f.bounds.Y - screen.Y, f.bounds.Width, f.bounds.Height), lit: f.lit && !DesktopWindows.CoversScreen(f.bounds, screen))).ToList();
			Rectangle now = Rectangle.Empty;
			foreach (var (r, lit) in local) { if (!lit) continue; var near = Rectangle.Inflate(r, 30, 30); now = now.IsEmpty ? near : Rectangle.Union(now, near); }
			now = Rectangle.Intersect(now, new Rectangle(Point.Empty, screen.Size));
			Rectangle dirty = Rectangle.Union(now, drawnBefore);
			if (dirty.IsEmpty && presented) return;
			gfx.SetClip(dirty);
			gfx.Clear(Color.Transparent);
			gfx.ResetClip();
			gfx.CompositingMode = CompositingMode.SourceOver;
			// from the back to the front: each window's lights hidden by the windows in front of it
			for (int i = local.Count - 1; i >= 0; i--)
				if (local[i].lit) LightsArt.Draw(gfx, local[i].r, local.Take(i).Select(f => f.r), time, LightsMode);
			Present(presented ? dirty : new Rectangle(Point.Empty, screen.Size));
			presented = true;
			drawnBefore = now;
		}

		/// <summary>Fog and sun: drawn small, stretched over the whole screen.</summary>
		void FrameSky()
		{
			var g = smallGfx;
			g.Clear(Color.Transparent);
			var keep = g.Save();
			g.ScaleTransform(1f / Down, 1f / Down);
			if (Sun > 0.01) DrawSun(g, screen.Size, time, Sun, SunColor ?? SunOwn);
			if (Fog > 0.01)
			{
				var color = FogColor ?? FogOwn;
				if (fogBlob == null || blobColor != color || Math.Abs(blobFog - Fog) > 0.02) { fogBlob?.Dispose(); fogBlob = MakeBlob(96, color, Fog); blobColor = color; blobFog = Fog; }
				for (int i = 0; i < clouds.Length; i++)
				{
					var c = clouds[i];
					c.x += (float)(Wind * 30 + 6) * c.speed * tick.Interval / 1000f;
					if (c.x - c.r * 2 > screen.Width) c.x = -c.r * 2;
					clouds[i] = c;
				}
				DrawFog(g, fogBlob, clouds, time, 1);
			}
			g.Restore(keep);
			Stretch(small, bits, screen.Width, screen.Height, screen.Width * 4);
			Present(new Rectangle(Point.Empty, screen.Size));
		}

		/// <summary>The grass: only its strips are cleared, drawn and sent.</summary>
		void FrameGrass()
		{
			if ((DateTime.Now - topsAt).TotalMilliseconds > 600) { tops = DesktopWindows.Tops(screen); topsAt = DateTime.Now; AddTaskbar(); }
			Rectangle now = GrassArea(tops, screen, Grass);
			Rectangle dirty = Rectangle.Union(now, drawnBefore);
			if (dirty.IsEmpty && presented) return;
			gfx.SetClip(dirty);
			gfx.Clear(Color.Transparent);
			DrawGrass(gfx, tops, screen.Location, time, Grass, Wind, GrassColor ?? GrassOwn);
			gfx.ResetClip();
			Present(presented ? dirty : new Rectangle(Point.Empty, screen.Size));
			presented = true;
			drawnBefore = now;
		}

		void AddTaskbar()
		{
			var work = Screen.FromRectangle(screen).WorkingArea;
			if (work.Bottom < screen.Bottom) tops.Add((work.Left, work.Right, work.Bottom));
		}

		// ------------------------------------------------------------------ behind the window in use

		[DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
		[DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int index);
		IntPtr lastFront;
		DateTime placedAt;

		/// <summary>Right under the window in use: over the other windows and the desktop, never over what one works in.</summary>
		void KeepBehindFront()
		{
			IntPtr front = DesktopWindows.GetForegroundWindow();
			if (front == lastFront && (DateTime.Now - placedAt).TotalSeconds < 1.5) return;
			lastFront = front; placedAt = DateTime.Now;
			const uint flags = 0x0001 | 0x0002 | 0x0010 | 0x0200;   // no size, no move, no activate, no owner order
			bool frontOnTop = front != IntPtr.Zero && (GetWindowLong(front, -20) & 0x08) != 0;
			if (front == Handle || DesktopWindows.IsShell(front) || frontOnTop) SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, flags);
			else SetWindowPos(Handle, front, 0, 0, 0, 0, flags);
		}

		// ------------------------------------------------------------------ the drawings (also used by the tests)

		static int[] mapX, weightX;

		/// <summary>
		/// A small picture stretched over a big one (both with premultiplied alpha), smooth (bilinear), quickly: whole numbers,
		/// two colour channels at a time, each source row blended once.
		/// </summary>
		public static unsafe void Stretch(Bitmap src, IntPtr dst, int width, int height, int stride)
		{
			int sw = src.Width, sh = src.Height;
			if (mapX == null || mapX.Length != width)
			{
				mapX = new int[width]; weightX = new int[width];
				for (int x = 0; x < width; x++) (mapX[x], weightX[x]) = Place(x, width, sw);
			}
			var data = src.LockBits(new Rectangle(0, 0, sw, sh), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
			try
			{
				uint* row = stackalloc uint[sw + 1];
				for (int y = 0; y < height; y++)
				{
					var (sy, wy) = Place(y, height, sh);
					uint* a = (uint*)((byte*)data.Scan0 + sy * data.Stride), b = (uint*)((byte*)data.Scan0 + Math.Min(sh - 1, sy + 1) * data.Stride);
					for (int i = 0; i < sw; i++) row[i] = Lerp(a[i], b[i], wy);
					row[sw] = row[sw - 1];
					uint* d = (uint*)((byte*)dst + y * stride);
					for (int x = 0; x < width; x++) { int sx = mapX[x]; d[x] = Lerp(row[sx], row[sx + 1], weightX[x]); }
				}
			}
			finally { src.UnlockBits(data); }
		}

		/// <summary>Where a pixel of the big picture falls in the small one: the pixel before it and how far towards the next (0..256).</summary>
		static (int at, int weight) Place(int i, int big, int small)
		{
			float f = Math.Clamp((i + 0.5f) * small / big - 0.5f, 0, small - 1);
			int at = Math.Min((int)f, small - 1);
			return (at, (int)((f - at) * 256));
		}

		static uint Lerp(uint p, uint q, int w)
		{
			uint k = (uint)w, j = 256 - k;
			uint rb = (((p & 0x00FF00FF) * j + (q & 0x00FF00FF) * k) >> 8) & 0x00FF00FF;
			uint ag = (((p >> 8) & 0x00FF00FF) * j + ((q >> 8) & 0x00FF00FF) * k) & 0xFF00FF00;
			return rb | ag;
		}


		public static void DrawSun(Graphics g, Size size, double time, double amount, Color color)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			var origin = new PointF(size.Width * 0.08f, -size.Height * 0.05f);
			for (int i = 0; i < 6; i++)
			{
				double a = 0.35 + i * 0.16 + Math.Sin(time * 0.15 + i) * 0.03;
				float len = size.Height * 1.6f, w = 0.035f + 0.02f * (i % 3);
				var p1 = new PointF(origin.X + (float)Math.Cos(a - w) * len, origin.Y + (float)Math.Sin(a - w) * len);
				var p2 = new PointF(origin.X + (float)Math.Cos(a + w) * len, origin.Y + (float)Math.Sin(a + w) * len);
				int alpha = Math.Clamp((int)(amount * (40 + 25 * Math.Sin(time * 0.4 + i * 1.7))), 0, 255);
				using var brush = new LinearGradientBrush(origin, p1, Color.FromArgb(alpha, color), Color.FromArgb(0, color));
				g.FillPolygon(brush, new[] { origin, p1, p2 });
			}
		}

		/// <summary>The puffs of fog (amount below 1 fades them; the blob may already carry its strength).</summary>
		public static void DrawFog(Graphics g, Bitmap blob, (float x, float y, float r, float speed)[] clouds, double time, double amount)
		{
			g.InterpolationMode = InterpolationMode.Bilinear;
			using var attrs = amount < 0.999 ? new ImageAttributes() : null;
			attrs?.SetColorMatrix(new ColorMatrix { Matrix33 = (float)Math.Max(0, amount) });
			for (int i = 0; i < clouds.Length; i++)
			{
				var c = clouds[i];
				float r = c.r * (1 + 0.08f * (float)Math.Sin(time * 0.2 + i));
				g.DrawImage(blob, new Rectangle((int)(c.x - r * 1.5f), (int)(c.y - r * 0.6f), (int)(r * 3), (int)(r * 1.2f)), 0, 0, blob.Width, blob.Height, GraphicsUnit.Pixel, attrs);
			}
		}

		/// <summary>A soft disc fading to nothing at its edge (one puff of fog), as strong as the fog.</summary>
		public static Bitmap MakeBlob(int size, Color color, double strength = 1)
		{
			var b = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
			using var g = Graphics.FromImage(b);
			using var path = new GraphicsPath();
			path.AddEllipse(0, 0, size, size);
			int alpha = Math.Clamp((int)(150 * Math.Min(1, strength)), 0, 255);
			using var brush = new PathGradientBrush(path) { CenterColor = Color.FromArgb(alpha, color), SurroundColors = new[] { Color.FromArgb(0, color) } };
			g.FillEllipse(brush, 0, 0, size, size);
			return b;
		}

		/// <summary>How high the grass reaches over the edge, with its earth (the tall blades included).</summary>
		static float GrassHeight(double grass) => (float)((12 + 26 * grass) * 1.6 + 5 + 3 * grass);

		/// <summary>Where the grass is drawn (to send Windows only that part).</summary>
		static Rectangle GrassArea(List<(int x0, int x1, int y)> tops, Rectangle screen, double grass)
		{
			var r = Rectangle.Empty;
			int h = (int)GrassHeight(grass) + 6;
			foreach (var (x0, x1, y) in tops)
			{
				var band = new Rectangle(x0 - screen.X - 14, y - screen.Y - h, x1 - x0 + 44, h + 3);
				r = r.IsEmpty ? band : Rectangle.Union(r, band);
			}
			return Rectangle.Intersect(r, new Rectangle(Point.Empty, screen.Size));
		}

		public static void DrawGrass(Graphics g, List<(int x0, int x1, int y)> tops, Point origin, double time, double grass, double wind, Color color)
		{
			// the lawn of GrassArt on each edge: earth, tufts of blades, the wind
			foreach (var (x0, x1, y) in tops)
				GrassArt.DrawFast(g, x0 - origin.X, x1 - origin.X, y - origin.Y, grass, wind, time, color);
		}

		// ------------------------------------------------------------------ to the screen: only the part that changed

		[StructLayout(LayoutKind.Sequential)] struct SIZE { public int cx, cy; }
		[StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
		[StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }
		[StructLayout(LayoutKind.Sequential, Pack = 1)] struct BLEND { public byte op, flags, alpha, format; }
		[StructLayout(LayoutKind.Sequential)] struct INFO { public int cbSize; public IntPtr hdcDst, pptDst, psize, hdcSrc, pptSrc; public int crKey; public IntPtr pblend; public int dwFlags; public IntPtr prcDirty; }
		[DllImport("user32.dll")] static extern bool UpdateLayeredWindowIndirect(IntPtr hwnd, ref INFO info);

		unsafe void Present(Rectangle dirty)
		{
			var size = new SIZE { cx = screen.Width, cy = screen.Height };
			var pos = new POINT { x = screen.X, y = screen.Y };
			var zero = new POINT();
			var blend = new BLEND { alpha = 255, format = 1 };
			var d = new RECT { left = dirty.Left, top = dirty.Top, right = dirty.Right, bottom = dirty.Bottom };
			var info = new INFO
			{
				cbSize = sizeof(INFO), pptDst = (IntPtr)(&pos), psize = (IntPtr)(&size), hdcSrc = memDc, pptSrc = (IntPtr)(&zero),
				pblend = (IntPtr)(&blend), dwFlags = 2, prcDirty = (IntPtr)(&d),
			};
			UpdateLayeredWindowIndirect(Handle, ref info);
		}

		// ------------------------------------------------------------------ per screen, only while something is on

		static readonly List<Overlay> skies = new(), grasses = new(), strings = new();

		public static void Apply(bool paused)
		{
			Keep(skies, !paused && (Fog > 0.01 || Sun > 0.01), true);
			Keep(grasses, !paused && Grass > 0.01, false);
			Keep(strings, !paused && Lights, false, true);
		}

		static void Keep(List<Overlay> list, bool want, bool sky, bool lights = false)
		{
			if (!want || list.Any(o => o.topmost != OnTop) || (list.Count > 0 && list.Count != Screen.AllScreens.Length))
			{
				foreach (var o in list.ToList()) o.Close();
				list.Clear();
			}
			if (!want) return;
			foreach (var o in list) Shots.Apply(o.Handle);
			if (list.Count > 0) return;
			foreach (var s in Screen.AllScreens) { var o = new Overlay(s, sky, lights); list.Add(o); o.Show(); }
		}

		public static bool Showing => skies.Count + grasses.Count + strings.Count > 0;
	}

	/// <summary>Our windows left out of screenshots and screen recordings (they still show on the screen), unless asked.</summary>
	internal static class Shots
	{
		[DllImport("user32.dll")] static extern bool SetWindowDisplayAffinity(IntPtr h, uint affinity);
		[DllImport("user32.dll")] static extern bool GetWindowDisplayAffinity(IntPtr h, out uint affinity);

		public static void Apply(IntPtr h)
		{
			if (h == IntPtr.Zero) return;
			uint want = Overlay.InScreenshots ? 0u : 0x11u;   // WDA_NONE or WDA_EXCLUDEFROMCAPTURE
			if (GetWindowDisplayAffinity(h, out uint now) && now == want) return;
			SetWindowDisplayAffinity(h, want);
		}

		public static bool Hidden(IntPtr h) => GetWindowDisplayAffinity(h, out uint a) && a == 0x11;
	}
}
