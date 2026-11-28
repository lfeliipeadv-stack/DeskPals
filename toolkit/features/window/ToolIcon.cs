using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace DeskPalsTools
{
	/// <summary>
	/// The extension's own icon (not DeskPals' pet): a white wrench on a rounded tile of DeskPals' green, drawn here in
	/// code so it can be made again at any size. "DeskPalsTools.exe --make-icon=&lt;file.ico&gt;" writes the .ico the
	/// project uses (toolkit\icon.ico: 16, 20, 24, 32, 40, 48, 64 and 256 pixels); the window draws its own copy too, so
	/// its title bar and taskbar get the sharp small sizes.
	/// </summary>
	internal static class ToolIcon
	{
		public static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
		static readonly Color Light = Color.FromArgb(58, 186, 110), Dark = Color.FromArgb(22, 116, 62), Edge = Color.FromArgb(16, 92, 48);
		static Icon window;

		[ModuleInitializer]
		internal static void Init()
		{
			// "--make-icon=<file>" is handled here, before anything else starts (see the report's hook for Program.cs)
			string arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--make-icon", StringComparison.Ordinal));
			if (arg != null) Environment.Exit(MakeIcon(arg.Contains('=') ? arg[(arg.IndexOf('=') + 1)..].Trim('"') : "icon.ico"));
		}

		/// <summary>Write the icon to this file (exit code 0) or 1 when it could not be written.</summary>
		public static int MakeIcon(string path)
		{
			try { Save(Path.GetFullPath(path)); return 0; }
			catch (Exception) { return 1; }
		}

		/// <summary>The icon at this size. Under 32 pixels the wrench is drawn bolder so it still reads at 16.</summary>
		public static Bitmap Draw(int size)
		{
			var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
			using var g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.Clear(Color.Transparent);
			// drawn on a 256 grid
			float k = size / 256f;
			g.ScaleTransform(k, k);
			bool small = size < 32;

			// the tile: a rounded square, lighter at the top
			float m = small ? 4 : 10;
			var tile = new RectangleF(m, m, 256 - m * 2, 256 - m * 2);
			using var tileBrush = new LinearGradientBrush(new PointF(0, m), new PointF(0, 256 - m), Light, Dark);
			using var tilePath = Rounded(tile, small ? 48 : 56);
			g.FillPath(tileBrush, tilePath);
			// nothing is drawn outside the tile (the jaw's cut would stick out of its corner)
			g.SetClip(tilePath);
			if (!small) using (var pen = new Pen(Edge, 6f)) using (var path = Rounded(new RectangleF(m + 3, m + 3, tile.Width - 6, tile.Height - 6), 53)) g.DrawPath(pen, path);

			// the wrench, from the lower left to the upper right
			float handle = small ? 50 : 40, head = small ? 62 : 54, slot = small ? 46 : 38;
			var start = new PointF(small ? 70 : 74, small ? 186 : 182);
			var center = new PointF(small ? 160 : 162, small ? 96 : 94);
			if (!small)
			{
				// a soft shadow under it
				using var shadow = new SolidBrush(Color.FromArgb(70, 0, 40, 10));
				using var shadowPen = new Pen(shadow, handle) { StartCap = LineCap.Round, EndCap = LineCap.Round };
				g.DrawLine(shadowPen, start.X + 6, start.Y + 8, center.X + 6, center.Y + 8);
				g.FillEllipse(shadow, center.X - head + 6, center.Y - head + 8, head * 2, head * 2);
			}
			using (var pen = new Pen(Color.White, handle) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(pen, start, center);
			g.FillEllipse(Brushes.White, center.X - head, center.Y - head, head * 2, head * 2);
			// the open jaw: a slot cut towards the upper right, in the tile's own colours so it looks cut out
			using (var cut = new Pen(tileBrush, slot) { StartCap = LineCap.Round, EndCap = LineCap.Flat })
				g.DrawLine(cut, center.X + 4, center.Y - 4, center.X + (head + 12) * 0.72f, center.Y - (head + 12) * 0.72f);
			// the hole at the end of the handle
			if (size >= 40) g.FillEllipse(tileBrush, start.X - 9, start.Y - 9, 18, 18);
			return bmp;
		}

		static GraphicsPath Rounded(RectangleF r, float radius)
		{
			var p = new GraphicsPath();
			float d = radius * 2;
			p.AddArc(r.X, r.Y, d, d, 180, 90);
			p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
			p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
			p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
			p.CloseFigure();
			return p;
		}

		/// <summary>
		/// An .ico with these sizes: 256 as PNG (what Windows expects for the big one), the others as 32-bit bitmaps with
		/// transparency (what every part of Windows reads, the old ones too).
		/// </summary>
		public static byte[] Ico(IList<int> sizes)
		{
			var images = sizes.Select(s => (s, data: Entry(s))).ToList();
			using var ms = new MemoryStream();
			using var w = new BinaryWriter(ms);
			w.Write((short)0); w.Write((short)1); w.Write((short)images.Count);
			int offset = 6 + 16 * images.Count;
			foreach (var (s, data) in images)
			{
				w.Write((byte)(s >= 256 ? 0 : s)); w.Write((byte)(s >= 256 ? 0 : s));
				w.Write((byte)0); w.Write((byte)0);
				w.Write((short)1); w.Write((short)32);
				w.Write(data.Length); w.Write(offset);
				offset += data.Length;
			}
			foreach (var (_, data) in images) w.Write(data);
			w.Flush();
			return ms.ToArray();
		}

		static byte[] Entry(int size)
		{
			using var bmp = Draw(size);
			if (size >= 256)
			{
				using var png = new MemoryStream();
				bmp.Save(png, ImageFormat.Png);
				return png.ToArray();
			}
			int maskRow = (size + 31) / 32 * 4;
			using var ms = new MemoryStream();
			using var w = new BinaryWriter(ms);
			// BITMAPINFOHEADER: the height counts the colour rows and the mask's
			w.Write(40); w.Write(size); w.Write(size * 2); w.Write((short)1); w.Write((short)32);
			w.Write(0); w.Write(size * size * 4 + maskRow * size); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
			var data = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				var row = new byte[size * 4];
				// bottom row first, as bitmaps are stored
				for (int y = size - 1; y >= 0; y--)
				{
					System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
					w.Write(row);
				}
			}
			finally { bmp.UnlockBits(data); }
			// the mask is all "use the alpha"
			w.Write(new byte[maskRow * size]);
			w.Flush();
			return ms.ToArray();
		}

		/// <summary>Write the .ico (to a temporary file first, so a failure never leaves half an icon).</summary>
		public static void Save(string path)
		{
			string dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
			string tmp = path + ".tmp";
			File.WriteAllBytes(tmp, Ico(Sizes));
			File.Move(tmp, path, true);
		}

		/// <summary>The window's icon with the sizes a title bar and a taskbar use (made once).</summary>
		public static Icon ForWindow()
		{
			if (window != null) return window;
			using var ms = new MemoryStream(Ico(new[] { 16, 20, 24, 32, 40, 48, 64 }));
			window = new Icon(ms);
			return window;
		}
	}
}
