using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DeskPalsTools
{
	/// <summary>
	/// Small pictures of the files in the window's list: the same thumbnails (or icons) Explorer shows, asked to Windows on
	/// a thread of their own so a big photo or a slow PDF never freezes the window. <see cref="Get"/> answers at once
	/// (null while it is being made) and <see cref="Ready"/> says when one arrived.
	/// </summary>
	internal static class Thumbnails
	{
		static readonly Dictionary<string, Bitmap> made = new(StringComparer.OrdinalIgnoreCase);
		static int pending;

		/// <summary>A thumbnail arrived (its file's path): the list draws itself again.</summary>
		public static event Action<string> Ready;

		static string Key(string path, int size) => size + "|" + path;

		/// <summary>The thumbnail at this size, or null (not made yet, or Windows has none: then an emoji is drawn).</summary>
		public static Bitmap Get(string path, int size)
		{
			string key = Key(path, size);
			lock (made)
			{
				if (made.TryGetValue(key, out var bmp)) return bmp;
				made[key] = null;
				if (stopped) return null;
				Interlocked.Increment(ref pending);
			}
			Task.Run(() => Make(key));
			return null;
		}

		/// <summary>Wait (at most this long) until every thumbnail asked for is made. For the tests' pictures.</summary>
		public static bool WaitIdle(TimeSpan most)
		{
			var until = DateTime.UtcNow + most;
			while (Volatile.Read(ref pending) > 0 && DateTime.UtcNow < until) Thread.Sleep(20);
			return Volatile.Read(ref pending) == 0;
		}

		static readonly SemaphoreSlim two = new(2);
		static bool stopped;

		/// <summary>The program ends: no new thumbnail is started, the ones being made are waited for (a moment).</summary>
		public static void Stop(int waitMs = 3000)
		{
			lock (made) stopped = true;
			WaitIdle(TimeSpan.FromMilliseconds(waitMs));
		}

		/// <summary>
		/// One thumbnail, on a worker of the thread pool (at most two at once, so a folder of photos does not take
		/// the whole computer). No thread of its own is kept: one living to the end of the program, with the PDF drawing
		/// of "See and edit", could keep the program from ending.
		/// </summary>
		static async Task Make(string key)
		{
			await two.WaitAsync();
			try
			{
				int bar = key.IndexOf('|');
				int size = int.Parse(key[..bar]);
				string path = key[(bar + 1)..];
				Bitmap bmp = null;
				try { bmp = Load(path, size); } catch (Exception) { }
				lock (made) made[key] = bmp;
				if (bmp != null) try { Ready?.Invoke(path); } catch (Exception) { }
			}
			finally { two.Release(); Interlocked.Decrement(ref pending); }
		}

		// ------------------------------------------------------------------ Windows' shell

		[ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IShellItemImageFactory
		{
			[PreserveSig] int GetImage(NativeSize size, int flags, out IntPtr bitmap);
		}

		[StructLayout(LayoutKind.Sequential)] struct NativeSize { public int cx, cy; }

		[StructLayout(LayoutKind.Sequential)]
		struct BitmapHeader
		{
			public int biSize, biWidth, biHeight;
			public short biPlanes, biBitCount;
			public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct NativeBitmap { public int bmType, bmWidth, bmHeight, bmWidthBytes; public short bmPlanes, bmBitsPixel; public IntPtr bmBits; }

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory item);
		[DllImport("gdi32.dll")] static extern int GetObject(IntPtr h, int size, out NativeBitmap bm);
		[DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapHeader info, uint usage);
		[DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr h);
		[DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr window);
		[DllImport("user32.dll")] static extern int ReleaseDC(IntPtr window, IntPtr dc);

		/// <summary>The thumbnail (a photo, a PDF's first page) or else the file's icon, as Explorer has it.</summary>
		/// <summary>
		/// The small picture of a file: a photo made smaller here, the first page of a PDF drawn by Windows' PDF reader,
		/// any other file its icon. (The shell's own thumbnail makers are not used: together with the PDF drawing of
		/// "See and edit" they could keep the program from ending.)
		/// </summary>
		internal static Bitmap Load(string path, int size)
		{
			try
			{
				if (Plain.Contains(Jobs.Ext(path))) return Photo(path, size);
				if (Jobs.IsPdf(path)) { var page = PdfPage(path, size); if (page != null) return page; }
			}
			catch (Exception) { }
			// any other file: its type's icon
			using var icon = Icon.ExtractAssociatedIcon(path);
			return icon?.ToBitmap();
		}

		/// <summary>Photos GDI+ reads by itself.</summary>
		static readonly HashSet<string> Plain = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".jfif", ".png", ".bmp", ".gif", ".tif", ".tiff" };

		/// <summary>A photo made small, turned as the camera said (EXIF), without keeping the file open.</summary>
		static Bitmap Photo(string path, int size)
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var img = Image.FromStream(stream, false, false);
			// the camera's way up (EXIF orientation 0x0112)
			try
			{
				if (img.PropertyIdList.Contains(0x0112))
				{
					int o = img.GetPropertyItem(0x0112).Value[0];
					var turn = o switch { 3 => RotateFlipType.Rotate180FlipNone, 6 => RotateFlipType.Rotate90FlipNone, 8 => RotateFlipType.Rotate270FlipNone, _ => RotateFlipType.RotateNoneFlipNone };
					if (turn != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(turn);
				}
			}
			catch (Exception) { }
			double k = Math.Min(1.0, size / (double)Math.Max(img.Width, img.Height));
			int w = Math.Max(1, (int)Math.Round(img.Width * k)), h = Math.Max(1, (int)Math.Round(img.Height * k));
			var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
			using var g = Graphics.FromImage(bmp);
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			g.DrawImage(img, 0, 0, w, h);
			return bmp;
		}

		/// <summary>The first page of a PDF, drawn by Windows' PDF reader (on this thread, waiting for it).</summary>
		static Bitmap PdfPage(string path, int size)
		{
			var file = Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(path)).AsTask().GetAwaiter().GetResult();
			var doc = Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file).AsTask().GetAwaiter().GetResult();
			if (doc.PageCount == 0) return null;
			using var page = doc.GetPage(0);
			using var ms = new Windows.Storage.Streams.InMemoryRandomAccessStream();
			var opts = new Windows.Data.Pdf.PdfPageRenderOptions { BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255) };
			if (page.Size.Width >= page.Size.Height) opts.DestinationWidth = (uint)size; else opts.DestinationHeight = (uint)size;
			page.RenderToStreamAsync(ms, opts).AsTask().GetAwaiter().GetResult();
			using var s = System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead(ms);
			using var loaded = new Bitmap(s);
			return new Bitmap(loaded);
		}

		/// <summary>
		/// A GDI bitmap with its transparency kept (Image.FromHbitmap loses it): its pixels read top-down, 32 bits each,
		/// already multiplied by their alpha as the shell gives them.
		/// </summary>
		static Bitmap FromHandle(IntPtr h)
		{
			if (GetObject(h, Marshal.SizeOf<NativeBitmap>(), out var info) == 0 || info.bmWidth <= 0) return null;
			int w = info.bmWidth, ht = Math.Abs(info.bmHeight);
			var header = new BitmapHeader { biSize = Marshal.SizeOf<BitmapHeader>(), biWidth = w, biHeight = -ht, biPlanes = 1, biBitCount = 32 };
			var bits = new byte[w * ht * 4];
			IntPtr dc = GetDC(IntPtr.Zero);
			try { if (GetDIBits(dc, h, 0, (uint)ht, bits, ref header, 0) == 0) return null; }
			finally { ReleaseDC(IntPtr.Zero, dc); }
			// a bitmap without transparency has every alpha at 0: then it is all opaque
			bool alpha = false;
			for (int i = 3; i < bits.Length && !alpha; i += 4) alpha = bits[i] != 0;
			if (!alpha) for (int i = 3; i < bits.Length; i += 4) bits[i] = 255;
			var bmp = new Bitmap(w, ht, PixelFormat.Format32bppPArgb);
			var data = bmp.LockBits(new Rectangle(0, 0, w, ht), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
			try { for (int y = 0; y < ht; y++) Marshal.Copy(bits, y * w * 4, data.Scan0 + y * data.Stride, w * 4); }
			finally { bmp.UnlockBits(data); }
			return bmp;
		}
	}
}
