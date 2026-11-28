using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;

namespace DeskPalsTools
{
	/// <summary>A picture as BGRA bytes (4 per pixel, no gaps between rows), what the scan cleaning works on.</summary>
	internal sealed class Pixels
	{
		public int W, H;
		public byte[] Data;
		public Pixels(int w, int h) { W = w; H = h; Data = new byte[w * h * 4]; }
		public Pixels(int w, int h, byte[] data) { W = w; H = h; Data = data; }
	}

	/// <summary>
	/// A photo of a paper made to look like a scan, in plain C# on the pixels Windows decodes: find the sheet (on a small
	/// copy, so a 12-megapixel photo stays quick), straighten it (its 4 corners pulled to a rectangle), and divide every
	/// pixel by an estimate of the paper's own light so shadows and uneven light go away (white paper, dark ink).
	/// </summary>
	internal static class ScanImage
	{
		/// <summary>Bigger photos are decoded smaller (memory): 24 megapixels is more than a page needs.</summary>
		const long MaxDecodePixels = 24_000_000;
		/// <summary>The long side of the finished page: A4 at 300 dpi.</summary>
		public const int MaxPageSide = 3508;
		/// <summary>The long side of the small copy the sheet is looked for in.</summary>
		const int FindSide = 800;

		public enum Mode { Color, Gray, BlackWhite }

		// ------------------------------------------------------------------ in and out

		public static async Task<Pixels> Load(string file)
		{
			using var input = File.OpenRead(file);
			var decoder = await BitmapDecoder.CreateAsync(input.AsRandomAccessStream());
			uint w = decoder.OrientedPixelWidth, h = decoder.OrientedPixelHeight;
			var transform = new BitmapTransform();
			if ((long)w * h > MaxDecodePixels)
			{
				double k = Math.Sqrt(MaxDecodePixels / ((double)w * h));
				// the transform's size is before the EXIF turn: scale both sides the same, so the order does not matter
				transform.ScaledWidth = (uint)Math.Max(1, decoder.PixelWidth * k);
				transform.ScaledHeight = (uint)Math.Max(1, decoder.PixelHeight * k);
				transform.InterpolationMode = BitmapInterpolationMode.Fant;
				w = (uint)Math.Max(1, w * k); h = (uint)Math.Max(1, h * k);
			}
			var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform,
				ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
			byte[] px = data.DetachPixelData();
			if (px.Length != (long)w * h * 4)
			{
				// the scaled size can differ by a pixel from the one computed: trust the bytes
				long n = px.Length / 4;
				if ((long)w * h != n) h = (uint)(n / w);
			}
			// see-through parts become white paper
			for (int i = 0; i < px.Length; i += 4)
			{
				int a = px[i + 3];
				if (a == 255) continue;
				px[i] = (byte)Math.Min(255, px[i] + 255 - a);
				px[i + 1] = (byte)Math.Min(255, px[i + 1] + 255 - a);
				px[i + 2] = (byte)Math.Min(255, px[i + 2] + 255 - a);
				px[i + 3] = 255;
			}
			return new Pixels((int)w, (int)h, px);
		}

		/// <summary>JPEG (colour or grey) or PNG (black and white, which PNG keeps small and sharp).</summary>
		public static async Task Save(Pixels p, string file, Mode mode)
		{
			bool png = mode == Mode.BlackWhite;
			if (mode != Mode.Color)
			{
				// grey: one byte per pixel is a third of the size; some Windows encoders refuse it, then the colour way
				var gray = new byte[p.W * p.H];
				for (int i = 0, j = 0; j < gray.Length; i += 4, j++) gray[j] = p.Data[i + 1];
				try { await Encode(file, png, BitmapPixelFormat.Gray8, p.W, p.H, gray); return; }
				catch (Exception) { try { File.Delete(file); } catch (Exception) { } }
			}
			await Encode(file, png, BitmapPixelFormat.Bgra8, p.W, p.H, p.Data);
		}

		static async Task Encode(string file, bool png, BitmapPixelFormat format, int w, int h, byte[] data)
		{
			using var output = File.Create(file);
			var props = new BitmapPropertySet();
			if (!png) props.Add("ImageQuality", new BitmapTypedValue(0.82, Windows.Foundation.PropertyType.Single));
			var encoder = await BitmapEncoder.CreateAsync(png ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId, output.AsRandomAccessStream(), props);
			encoder.SetPixelData(format, BitmapAlphaMode.Ignore, (uint)w, (uint)h, 300, 300, data);
			await encoder.FlushAsync();
		}

		// ------------------------------------------------------------------ the whole cleaning

		/// <summary>The photo of a paper as a clean, straight page.</summary>
		public static Pixels Clean(Pixels photo, Mode mode, CancellationToken cancel = default)
		{
			var page = Straighten(photo, out _);
			cancel.ThrowIfCancellationRequested();
			Whiten(page, mode);
			return page;
		}

		/// <summary>The sheet cut out and pulled straight; when no sheet is found, the whole photo turned level by its lines of text.</summary>
		public static Pixels Straighten(Pixels photo, out bool found)
		{
			double k = Math.Min(1.0, FindSide / (double)Math.Max(photo.W, photo.H));
			var small = SmallGray(photo, k, out int sw, out int sh);
			var corners = FindSheet(small, sw, sh);
			found = corners != null;
			if (corners != null)
			{
				var full = corners.Select(c => (c.x / k, c.y / k)).ToArray();
				return Warp(photo, full);
			}
			double angle = TextAngle(small, sw, sh);
			return Rotate(photo, angle);
		}

		// ------------------------------------------------------------------ the small grey copy

		/// <summary>A grey copy <paramref name="k"/> times the size (each small pixel the average of its block).</summary>
		static float[] SmallGray(Pixels p, double k, out int w, out int h)
		{
			w = Math.Max(1, (int)Math.Round(p.W * k)); h = Math.Max(1, (int)Math.Round(p.H * k));
			var sum = new float[w * h];
			var count = new int[w * h];
			int ww = w, hh = h;
			var xs = new int[p.W];
			for (int x = 0; x < p.W; x++) xs[x] = Math.Min(ww - 1, (int)(x * k));
			for (int y = 0; y < p.H; y++)
			{
				int row = Math.Min(hh - 1, (int)(y * k)) * ww, i = y * p.W * 4;
				for (int x = 0; x < p.W; x++, i += 4)
				{
					int j = row + xs[x];
					sum[j] += 0.114f * p.Data[i] + 0.587f * p.Data[i + 1] + 0.299f * p.Data[i + 2];
					count[j]++;
				}
			}
			for (int j = 0; j < sum.Length; j++) sum[j] = count[j] > 0 ? sum[j] / count[j] : 0;
			return sum;
		}

		/// <summary>Box blur of radius r, rows then columns (a running sum: the cost does not grow with r).</summary>
		static float[] Blur(float[] src, int w, int h, int r)
		{
			var tmp = new float[src.Length];
			var dst = new float[src.Length];
			for (int y = 0; y < h; y++)
			{
				float acc = 0; int n = 0, row = y * w;
				for (int x = -r; x < w; x++)
				{
					if (x + r < w) { acc += src[row + x + r]; n++; }
					if (x - r - 1 >= 0) { acc -= src[row + x - r - 1]; n--; }
					if (x >= 0) tmp[row + x] = acc / n;
				}
			}
			for (int x = 0; x < w; x++)
			{
				float acc = 0; int n = 0;
				for (int y = -r; y < h; y++)
				{
					if (y + r < h) { acc += tmp[(y + r) * w + x]; n++; }
					if (y - r - 1 >= 0) { acc -= tmp[(y - r - 1) * w + x]; n--; }
					if (y >= 0) dst[y * w + x] = acc / n;
				}
			}
			return dst;
		}

		/// <summary>The brightest value in a square of radius r (rows then columns): it rubs out the ink, leaving the paper.</summary>
		static float[] MaxFilter(float[] src, int w, int h, int r)
		{
			var tmp = new float[src.Length];
			var dst = new float[src.Length];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					float m = 0;
					for (int d = Math.Max(0, x - r), e = Math.Min(w - 1, x + r); d <= e; d++) m = Math.Max(m, src[y * w + d]);
					tmp[y * w + x] = m;
				}
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					float m = 0;
					for (int d = Math.Max(0, y - r), e = Math.Min(h - 1, y + r); d <= e; d++) m = Math.Max(m, tmp[d * w + x]);
					dst[y * w + x] = m;
				}
			return dst;
		}

		// ------------------------------------------------------------------ finding the sheet

		/// <summary>
		/// The 4 corners of the sheet in the small copy (top-left, top-right, bottom-right, bottom-left), or null when there
		/// is no clear sheet (then the photo is used whole). The paper is found two ways, the first that gives a believable
		/// sheet wins: (1) the smooth area around the bright part: light changes slowly over a sheet (shadows, a lamp) while
		/// its edge is a sharp step, so a fill that stops at sharp steps of the brightness ratio finds the whole sheet even
		/// where it lies in a shadow darker than the table elsewhere; (2) simply the biggest bright patch. The outline
		/// (convex hull) of the patch gives the 4-cornered shape with the largest area.
		/// </summary>
		internal static (double x, double y)[] FindSheet(float[] gray, int w, int h)
		{
			var g = Blur(Blur(gray, w, h, 2), w, h, 2);
			float t = Otsu(g);
			var bright = new bool[w * h];
			for (int i = 0; i < bright.Length; i++) bright[i] = g[i] > t;
			var (labels, areas) = Label(bright, w, h);
			int sheet = 0;
			for (int l = 1; l < areas.Length; l++) if (areas[l] > areas[sheet]) sheet = l;
			if (sheet == 0) return null;

			var (smooth, smoothAreas) = Label(Smooth(g, w, h), w, h);
			var overlap = new int[smoothAreas.Length];
			for (int i = 0; i < labels.Length; i++) if (smooth[i] != 0 && labels[i] == sheet) overlap[smooth[i]]++;
			int best = 0;
			for (int l = 1; l < overlap.Length; l++) if (overlap[l] > overlap[best]) best = l;
			// the smooth area must be mostly the bright patch, not the bright patch plus half the table
			if (best > 0 && overlap[best] > areas[sheet] * 0.5 && smoothAreas[best] < areas[sheet] * 2.5)
			{
				// the fill stops where the edge's blur begins: the sheet reaches a few pixels further
				var q = FromPatch(smooth, best, smoothAreas[best], g, w, h, 3);
				if (q != null) return q;
			}
			return FromPatch(labels, sheet, areas[sheet], g, w, h, 0);
		}

		/// <summary>
		/// Where the brightness changes slowly: the ratio between neighbours (a step of the logarithm) is small. A lamp's
		/// fall-off or a soft shadow changes it by far less than a sheet's edge or a letter.
		/// </summary>
		static bool[] Smooth(float[] g, int w, int h)
		{
			var log = new float[g.Length];
			for (int i = 0; i < g.Length; i++) log[i] = MathF.Log(g[i] + 8);
			var m = new bool[g.Length];
			for (int y = 1; y < h - 1; y++)
				for (int x = 1; x < w - 1; x++)
				{
					int i = y * w + x;
					float gx = Math.Abs(log[i + 1] - log[i - 1]), gy = Math.Abs(log[i + w] - log[i - w]);
					m[i] = Math.Max(gx, gy) < 0.07f;
				}
			return m;
		}

		/// <summary>The sheet's corners from one patch of a labelled mask, or null when it does not look like a sheet.</summary>
		static (double x, double y)[] FromPatch(int[] labels, int label, int area, float[] g, int w, int h, double grow)
		{
			if (area < w * h * 0.12 || area > w * h * 0.97) return null;
			var points = new List<(double x, double y)>();
			for (int y = 0; y < h; y++)
			{
				int min = -1, max = -1;
				for (int x = 0; x < w; x++) if (labels[y * w + x] == label) { if (min < 0) min = x; max = x; }
				if (min >= 0) { points.Add((min, y)); points.Add((max + 1, y)); }
			}
			var hull = Hull(points);
			if (hull.Count < 4) return null;
			double hullArea = Math.Abs(Area(hull));
			// a patch with too much missing inside its outline is not a sheet (a light floor, a window…)
			if (area < hullArea * 0.55) return null;
			var quad = BiggestQuad(hull);
			if (quad == null || Math.Abs(Area(quad)) < hullArea * 0.88) return null;
			// "a sheet" as big as the whole photo is the photo itself (taken close up): nothing to cut
			if (Math.Abs(Area(quad)) > w * h * 0.93) return null;
			var ordered = Order(quad);
			if (!Reasonable(ordered)) return null;
			// paper and table must differ enough for the edge to be trusted
			if (!Contrast(g, w, h, ordered)) return null;
			return Inset(ordered, 0.006 * Math.Max(w, h) - grow);
		}

		/// <summary>The grey level that best splits the picture in two (Otsu).</summary>
		static float Otsu(float[] g)
		{
			var hist = new double[256];
			foreach (var v in g) hist[Math.Clamp((int)v, 0, 255)]++;
			double total = g.Length, sumAll = 0;
			for (int i = 0; i < 256; i++) sumAll += i * hist[i];
			double wB = 0, sumB = 0, best = -1; int bestT = 128;
			for (int i = 0; i < 256; i++)
			{
				wB += hist[i];
				if (wB == 0) continue;
				double wF = total - wB;
				if (wF == 0) break;
				sumB += i * hist[i];
				double mB = sumB / wB, mF = (sumAll - sumB) / wF;
				double between = wB * wF * (mB - mF) * (mB - mF);
				if (between > best) { best = between; bestT = i; }
			}
			return bestT + 0.5f;
		}

		/// <summary>The connected patches of a mask (4 neighbours): a label per pixel (0 = none) and each label's pixel count.</summary>
		static (int[] labels, int[] areas) Label(bool[] mask, int w, int h)
		{
			var label = new int[mask.Length];
			var areas = new List<int> { 0 };
			var stack = new Stack<int>();
			for (int s = 0; s < mask.Length; s++)
			{
				if (!mask[s] || label[s] != 0) continue;
				int next = areas.Count, area = 0;
				label[s] = next; stack.Push(s);
				while (stack.Count > 0)
				{
					int i = stack.Pop(); area++;
					int x = i % w, y = i / w;
					if (x > 0 && mask[i - 1] && label[i - 1] == 0) { label[i - 1] = next; stack.Push(i - 1); }
					if (x < w - 1 && mask[i + 1] && label[i + 1] == 0) { label[i + 1] = next; stack.Push(i + 1); }
					if (y > 0 && mask[i - w] && label[i - w] == 0) { label[i - w] = next; stack.Push(i - w); }
					if (y < h - 1 && mask[i + w] && label[i + w] == 0) { label[i + w] = next; stack.Push(i + w); }
				}
				areas.Add(area);
			}
			return (label, areas.ToArray());
		}

		/// <summary>Convex hull (monotone chain), counter-clockwise in screen terms.</summary>
		static List<(double x, double y)> Hull(List<(double x, double y)> pts)
		{
			var p = pts.Distinct().OrderBy(a => a.x).ThenBy(a => a.y).ToList();
			if (p.Count < 3) return p;
			static double Cross((double x, double y) o, (double x, double y) a, (double x, double y) b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
			var hull = new List<(double x, double y)>();
			foreach (var pt in p) { while (hull.Count >= 2 && Cross(hull[^2], hull[^1], pt) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(pt); }
			int lower = hull.Count + 1;
			for (int i = p.Count - 2; i >= 0; i--) { while (hull.Count >= lower && Cross(hull[^2], hull[^1], p[i]) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p[i]); }
			hull.RemoveAt(hull.Count - 1);
			return hull;
		}

		static double Area(IList<(double x, double y)> poly)
		{
			double a = 0;
			for (int i = 0; i < poly.Count; i++) { var p = poly[i]; var q = poly[(i + 1) % poly.Count]; a += p.x * q.y - q.x * p.y; }
			return a / 2;
		}

		/// <summary>
		/// The 4 hull points that make the largest 4-sided shape. For each pair (a diagonal), the farthest point on each side
		/// of it; the hull is thinned to at most 120 points first so this stays quick.
		/// </summary>
		static (double x, double y)[] BiggestQuad(List<(double x, double y)> hull)
		{
			var h = hull;
			if (h.Count > 120) { int step = (int)Math.Ceiling(h.Count / 120.0); h = h.Where((_, i) => i % step == 0).ToList(); }
			int n = h.Count;
			double best = 0; (double x, double y)[] quad = null;
			for (int i = 0; i < n; i++)
				for (int j = i + 2; j < n; j++)
				{
					var a = h[i]; var b = h[j];
					double bestL = 0, bestR = 0; int l = -1, r = -1;
					for (int m = 0; m < n; m++)
					{
						if (m == i || m == j) continue;
						double cross = (b.x - a.x) * (h[m].y - a.y) - (b.y - a.y) * (h[m].x - a.x);
						if (cross > bestL) { bestL = cross; l = m; }
						if (-cross > bestR) { bestR = -cross; r = m; }
					}
					if (l < 0 || r < 0) continue;
					double area = (bestL + bestR) / 2;
					if (area > best) { best = area; quad = new[] { a, h[l], b, h[r] }; }
				}
			return quad;
		}

		/// <summary>Top-left, top-right, bottom-right, bottom-left (clockwise on screen, the first nearest the top-left).</summary>
		static (double x, double y)[] Order((double x, double y)[] q)
		{
			double cx = q.Average(p => p.x), cy = q.Average(p => p.y);
			var s = q.OrderBy(p => Math.Atan2(p.y - cy, p.x - cx)).ToList();
			int first = 0;
			for (int i = 1; i < 4; i++) if (s[i].x + s[i].y < s[first].x + s[first].y) first = i;
			return Enumerable.Range(0, 4).Select(i => s[(first + i) % 4]).ToArray();
		}

		/// <summary>Corners that make a believable sheet: angles near square, no side tiny next to its opposite.</summary>
		static bool Reasonable((double x, double y)[] q)
		{
			for (int i = 0; i < 4; i++)
			{
				var p = q[(i + 3) % 4]; var c = q[i]; var n = q[(i + 1) % 4];
				double ax = p.x - c.x, ay = p.y - c.y, bx = n.x - c.x, by = n.y - c.y;
				double cos = (ax * bx + ay * by) / (Math.Sqrt(ax * ax + ay * ay) * Math.Sqrt(bx * bx + by * by) + 1e-9);
				if (Math.Abs(cos) > 0.5) return false;   // outside 60°–120°
			}
			double Len(int a, int b) => Math.Sqrt(Math.Pow(q[a].x - q[b].x, 2) + Math.Pow(q[a].y - q[b].y, 2));
			double top = Len(0, 1), right = Len(1, 2), bottom = Len(2, 3), left = Len(3, 0);
			return Math.Min(top, bottom) / Math.Max(top, bottom) > 0.5 && Math.Min(left, right) / Math.Max(left, right) > 0.5;
		}

		/// <summary>
		/// All along the edges, the paper just inside is clearly lighter than the table just outside (compared as a ratio,
		/// so a shadow over both does not matter). Parts of the edge at the photo's border have no outside: they count as fine.
		/// </summary>
		static bool Contrast(float[] g, int w, int h, (double x, double y)[] q)
		{
			double cx = q.Average(p => p.x), cy = q.Average(p => p.y);
			int pairs = 0, good = 0;
			for (int i = 0; i < 4; i++)
			{
				var a = q[i]; var b = q[(i + 1) % 4];
				for (double s = 0.1; s <= 0.9; s += 0.05)
				{
					double x = a.x + (b.x - a.x) * s, y = a.y + (b.y - a.y) * s;
					double dx = x - cx, dy = y - cy, len = Math.Sqrt(dx * dx + dy * dy) + 1e-9;
					int xi = (int)(x - dx / len * 7), yi = (int)(y - dy / len * 7), xo = (int)(x + dx / len * 7), yo = (int)(y + dy / len * 7);
					if (xi < 0 || yi < 0 || xi >= w || yi >= h) continue;
					if (xo < 0 || yo < 0 || xo >= w || yo >= h) continue;
					pairs++;
					if ((g[yi * w + xi] + 8) / (g[yo * w + xo] + 8) > 1.25) good++;
				}
			}
			return pairs < 8 || good >= pairs * 0.7;
		}

		/// <summary>Each corner moved a little towards the middle: a sliver of the table at the edge looks worse than a sliver less of paper.</summary>
		static (double x, double y)[] Inset((double x, double y)[] q, double by)
		{
			double cx = q.Average(p => p.x), cy = q.Average(p => p.y);
			return q.Select(p =>
			{
				double dx = cx - p.x, dy = cy - p.y, len = Math.Sqrt(dx * dx + dy * dy) + 1e-9;
				return (p.x + dx / len * by * 1.4142, p.y + dy / len * by * 1.4142);
			}).ToArray();
		}

		// ------------------------------------------------------------------ straightening

		/// <summary>The 4 corners pulled to a rectangle (a projective "warp"), the size of the sheet's longest sides (at most A4 at 300 dpi).</summary>
		static Pixels Warp(Pixels src, (double x, double y)[] q)
		{
			double D(int a, int b) => Math.Sqrt(Math.Pow(q[a].x - q[b].x, 2) + Math.Pow(q[a].y - q[b].y, 2));
			double w = Math.Max(D(0, 1), D(3, 2)), h = Math.Max(D(0, 3), D(1, 2));
			double k = Math.Min(1.0, MaxPageSide / Math.Max(w, h));
			int W = Math.Max(1, (int)Math.Round(w * k)), H = Math.Max(1, (int)Math.Round(h * k));
			var m = SquareToQuad(q);
			var dst = new Pixels(W, H);
			Parallel.For(0, H, v =>
			{
				double t = (v + 0.5) / H;
				int o = v * W * 4;
				for (int u = 0; u < W; u++, o += 4)
				{
					double s = (u + 0.5) / W;
					double z = m[6] * s + m[7] * t + 1;
					double x = (m[0] * s + m[1] * t + m[2]) / z, y = (m[3] * s + m[4] * t + m[5]) / z;
					Sample(src, x - 0.5, y - 0.5, dst.Data, o);
				}
			});
			return dst;
		}

		/// <summary>
		/// The projective map from the unit square to the 4 corners (Heckbert): x = (a·s + b·t + c) / (g·s + h·t + 1),
		/// y = (d·s + e·t + f) / (g·s + h·t + 1). Returns a, b, c, d, e, f, g, h.
		/// </summary>
		internal static double[] SquareToQuad((double x, double y)[] q)
		{
			double x0 = q[0].x, y0 = q[0].y, x1 = q[1].x, y1 = q[1].y, x2 = q[2].x, y2 = q[2].y, x3 = q[3].x, y3 = q[3].y;
			double dx1 = x1 - x2, dx2 = x3 - x2, dx3 = x0 - x1 + x2 - x3;
			double dy1 = y1 - y2, dy2 = y3 - y2, dy3 = y0 - y1 + y2 - y3;
			if (Math.Abs(dx3) < 1e-9 && Math.Abs(dy3) < 1e-9)
				return new[] { x1 - x0, x2 - x1, x0, y1 - y0, y2 - y1, y0, 0, 0 };
			double den = dx1 * dy2 - dx2 * dy1;
			double g = (dx3 * dy2 - dx2 * dy3) / den, h = (dx1 * dy3 - dx3 * dy1) / den;
			return new[] { x1 - x0 + g * x1, x3 - x0 + h * x3, x0, y1 - y0 + g * y1, y3 - y0 + h * y3, y0, g, h };
		}

		/// <summary>The colour between 4 pixels (bilinear); outside the photo it is white paper.</summary>
		static void Sample(Pixels p, double x, double y, byte[] dst, int o)
		{
			if (x < -0.5 || y < -0.5 || x > p.W - 0.5 || y > p.H - 0.5) { dst[o] = dst[o + 1] = dst[o + 2] = dst[o + 3] = 255; return; }
			x = Math.Clamp(x, 0, p.W - 1); y = Math.Clamp(y, 0, p.H - 1);
			int x0 = (int)x, y0 = (int)y, x1 = Math.Min(p.W - 1, x0 + 1), y1 = Math.Min(p.H - 1, y0 + 1);
			double fx = x - x0, fy = y - y0;
			int a = (y0 * p.W + x0) * 4, b = (y0 * p.W + x1) * 4, c = (y1 * p.W + x0) * 4, d = (y1 * p.W + x1) * 4;
			var s = p.Data;
			for (int ch = 0; ch < 3; ch++)
			{
				double top = s[a + ch] + (s[b + ch] - s[a + ch]) * fx, bottom = s[c + ch] + (s[d + ch] - s[c + ch]) * fx;
				dst[o + ch] = (byte)(top + (bottom - top) * fy + 0.5);
			}
			dst[o + 3] = 255;
		}

		/// <summary>
		/// The tilt of the lines of text, in degrees (−12…12): the angle at which the dark pixels pile up in the fewest,
		/// fullest rows. Used only when no sheet edge was found.
		/// </summary>
		internal static double TextAngle(float[] gray, int w, int h)
		{
			var bg = Blur(gray, w, h, Math.Max(4, Math.Max(w, h) / 40));
			var dark = new List<(int x, int y)>();
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					if (gray[y * w + x] < bg[y * w + x] - 25) dark.Add((x, y));
			if (dark.Count < 50) return 0;
			if (dark.Count > 40000) { int step = dark.Count / 40000 + 1; dark = dark.Where((_, i) => i % step == 0).ToList(); }
			double Score(double deg)
			{
				double r = deg * Math.PI / 180, sin = Math.Sin(r), cos = Math.Cos(r);
				int size = w + h;
				var hist = new double[size * 2 + 1];
				foreach (var (x, y) in dark) hist[(int)(y * cos - x * sin) + size]++;
				return hist.Sum(v => v * v);
			}
			double best = 0, bestScore = Score(0);
			for (double a = -12; a <= 12; a += 0.5) { double sc = Score(a); if (sc > bestScore * 1.0001) { bestScore = sc; best = a; } }
			for (double a = best - 0.4; a <= best + 0.4; a += 0.1) { double sc = Score(a); if (sc > bestScore) { bestScore = sc; best = a; } }
			return best;
		}

		/// <summary>The photo turned by <paramref name="deg"/> about its middle (what comes in at the corners is white).</summary>
		static Pixels Rotate(Pixels src, double deg)
		{
			double k = Math.Min(1.0, MaxPageSide / (double)Math.Max(src.W, src.H));
			int W = Math.Max(1, (int)(src.W * k)), H = Math.Max(1, (int)(src.H * k));
			if (Math.Abs(deg) < 0.05 && k == 1.0) return src;
			double r = deg * Math.PI / 180, sin = Math.Sin(r), cos = Math.Cos(r);
			double cx = src.W / 2.0, cy = src.H / 2.0;
			var dst = new Pixels(W, H);
			Parallel.For(0, H, v =>
			{
				int o = v * W * 4;
				for (int u = 0; u < W; u++, o += 4)
				{
					double x = (u + 0.5) / k - cx, y = (v + 0.5) / k - cy;
					// the text tilts by deg: undo it by looking back at the point turned the other way
					double sx = x * cos - y * sin + cx, sy = x * sin + y * cos + cy;
					Sample(src, sx - 0.5, sy - 0.5, dst.Data, o);
				}
			});
			return dst;
		}

		// ------------------------------------------------------------------ white paper, dark ink

		/// <summary>
		/// Each colour divided by the light the paper gets there (a small, blurred copy where the ink was rubbed out with a
		/// "brightest nearby" filter), then stretched: the paper becomes white, shadows and uneven light go, the ink gets
		/// dark. Grey and black-and-white work on the brightness only.
		/// </summary>
		public static void Whiten(Pixels p, Mode mode)
		{
			const int Side = 160;
			double k = Side / (double)Math.Max(p.W, p.H);
			int bw = Math.Max(2, (int)Math.Round(p.W * k)), bh = Math.Max(2, (int)Math.Round(p.H * k));
			var estimate = new float[3][];
			for (int ch = 0; ch < 3; ch++) estimate[ch] = MaxFilter(SmallChannel(p, ch, bw, bh), bw, bh, 4);
			// a photo, a logo or a filled box bigger than the filter is not paper: its light comes from the paper around it
			var light = PaperOnly(estimate, bw, bh);
			var bg = new float[3][];
			for (int ch = 0; ch < 3; ch++) bg[ch] = Blur(Blur(Blur(light[ch], bw, bh, 4), bw, bh, 4), bw, bh, 4);
			const double White = 0.86, Black = 0.22;
			var curve = new byte[1025];
			for (int i = 0; i <= 1024; i++)
			{
				double n = i / 1024.0;
				double v = n >= White ? 1 : n <= Black ? 0 : Math.Pow((n - Black) / (White - Black), 1.25);
				curve[i] = (byte)Math.Round(v * 255);
			}
			Parallel.For(0, p.H, y =>
			{
				double fy = Math.Clamp((y + 0.5) * bh / (double)p.H - 0.5, 0, bh - 1);
				int y0 = (int)fy, y1 = Math.Min(bh - 1, y0 + 1);
				double ty = fy - y0;
				int o = y * p.W * 4;
				Span<double> n = stackalloc double[3];
				for (int x = 0; x < p.W; x++, o += 4)
				{
					double fx = Math.Clamp((x + 0.5) * bw / (double)p.W - 0.5, 0, bw - 1);
					int x0 = (int)fx, x1 = Math.Min(bw - 1, x0 + 1);
					double tx = fx - x0;
					for (int ch = 0; ch < 3; ch++)
					{
						var b = bg[ch];
						double top = b[y0 * bw + x0] + (b[y0 * bw + x1] - b[y0 * bw + x0]) * tx;
						double bottom = b[y1 * bw + x0] + (b[y1 * bw + x1] - b[y1 * bw + x0]) * tx;
						double light = Math.Max(24, top + (bottom - top) * ty);
						n[ch] = Math.Min(1, p.Data[o + ch] / light);
					}
					if (mode == Mode.Color)
						for (int ch = 0; ch < 3; ch++) p.Data[o + ch] = curve[(int)(n[ch] * 1024)];
					else
					{
						double l = 0.114 * n[0] + 0.587 * n[1] + 0.299 * n[2];
						byte v = mode == Mode.BlackWhite ? (byte)(l > 0.62 ? 255 : 0) : curve[(int)(l * 1024)];
						p.Data[o] = p.Data[o + 1] = p.Data[o + 2] = v;
					}
					p.Data[o + 3] = 255;
				}
			});
		}

		/// <summary>The biggest change of brightness (as a ratio) between neighbours that is still the same paper under changing light.</summary>
		const float PaperStep = 0.09f;

		/// <summary>
		/// The paper's light, from the paper only. The "brightest nearby" estimate is the paper wherever ink is thin, but a
		/// photo, a logo or a filled box wider than the filter is its own "brightest", and dividing by it would turn it white.
		/// Paper is what can be reached from the brightest part through small steps (light and shadows change gently; the
		/// edge of a picture is a sharp step); everything else gets the light of the paper around it, filled in from the
		/// outside, ring by ring, so it keeps its own tone and colour.
		/// </summary>
		static float[][] PaperOnly(float[][] est, int w, int h)
		{
			int n = w * h;
			var lum = new float[n];
			var logs = new float[3][];
			for (int ch = 0; ch < 3; ch++) logs[ch] = new float[n];
			for (int i = 0; i < n; i++)
			{
				lum[i] = 0.114f * est[0][i] + 0.587f * est[1][i] + 0.299f * est[2][i];
				for (int ch = 0; ch < 3; ch++) logs[ch][i] = MathF.Log(est[ch][i] + 8);
			}
			var sorted = (float[])lum.Clone();
			Array.Sort(sorted);
			float seed = sorted[(int)(n * 0.95)] * 0.85f;
			var paper = new bool[n];
			var queue = new Queue<int>();
			for (int i = 0; i < n; i++) if (lum[i] >= seed) { paper[i] = true; queue.Enqueue(i); }
			void Try(int from, int to)
			{
				if (paper[to]) return;
				for (int ch = 0; ch < 3; ch++) if (Math.Abs(logs[ch][from] - logs[ch][to]) > PaperStep) return;
				paper[to] = true; queue.Enqueue(to);
			}
			while (queue.Count > 0)
			{
				int i = queue.Dequeue(), x = i % w, y = i / w;
				if (x > 0) Try(i, i - 1);
				if (x < w - 1) Try(i, i + 1);
				if (y > 0) Try(i, i - w);
				if (y < h - 1) Try(i, i + w);
			}
			// hardly any paper found (a photo of a picture, all dark…): the plain estimate, as before
			if (paper.Count(b => b) < n * 0.15) return est;

			var light = est.Select(a => (float[])a.Clone()).ToArray();
			var known = (bool[])paper.Clone();
			var ring = new List<int>();
			var values = new List<(int i, float b, float g, float r)>();
			while (true)
			{
				ring.Clear();
				for (int i = 0; i < n; i++)
				{
					if (known[i]) continue;
					int x = i % w, y = i / w;
					if ((x > 0 && known[i - 1]) || (x < w - 1 && known[i + 1]) || (y > 0 && known[i - w]) || (y < h - 1 && known[i + w])) ring.Add(i);
				}
				if (ring.Count == 0) break;
				values.Clear();
				foreach (int i in ring)
				{
					int x = i % w, y = i / w, count = 0;
					float b = 0, g = 0, r = 0;
					for (int dy = -1; dy <= 1; dy++)
						for (int dx = -1; dx <= 1; dx++)
						{
							int xx = x + dx, yy = y + dy;
							if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
							int j = yy * w + xx;
							if (!known[j]) continue;
							b += light[0][j]; g += light[1][j]; r += light[2][j]; count++;
						}
					values.Add((i, b / count, g / count, r / count));
				}
				foreach (var v in values) { light[0][v.i] = v.b; light[1][v.i] = v.g; light[2][v.i] = v.r; known[v.i] = true; }
			}
			return light;
		}

		// ------------------------------------------------------------------ a page lying on its side

		/// <summary>
		/// Whether the lines of text run up and down (the sheet was photographed sideways, or turned more than 45°): the dark
		/// pixels pile up in full columns with gaps between, rather than in rows.
		/// </summary>
		internal static bool Sideways(Pixels page)
		{
			var dark = DarkMask(page, out int w, out int h, out int count);
			if (count < 200) return false;
			var rows = new double[h];
			var cols = new double[w];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					if (dark[y * w + x]) { rows[y]++; cols[x]++; }
			// how much the dark pixels gather in some lines and leave others empty, the same for any length
			static double Energy(double[] hist, int total) => hist.Sum(v => v * v) * hist.Length / ((double)total * total);
			return Energy(cols, count) > Energy(rows, count) * 1.2;
		}

		/// <summary>The ink of a page (darker than the paper around it) on a copy of at most 800 pixels.</summary>
		static bool[] DarkMask(Pixels page, out int w, out int h, out int count)
		{
			double k = Math.Min(1.0, FindSide / (double)Math.Max(page.W, page.H));
			var gray = SmallGray(page, k, out w, out h);
			var bg = Blur(gray, w, h, Math.Max(4, Math.Max(w, h) / 40));
			var dark = new bool[w * h];
			count = 0;
			for (int i = 0; i < dark.Length; i++) if (gray[i] < bg[i] - 25) { dark[i] = true; count++; }
			return dark;
		}

		/// <summary>
		/// How much the text looks lined up on the left (as most text is) rather than on the right: for each line of text,
		/// whether it starts at the common left margin, minus whether it ends at a common right one. Used to tell a page
		/// turned a quarter one way from the other when there is no OCR.
		/// </summary>
		internal static double LeftAligned(Pixels page)
		{
			var dark = DarkMask(page, out int w, out int h, out int count);
			if (count < 200) return 0;
			var lefts = new List<int>();
			var rights = new List<int>();
			// a sliver of table or a shadow at the very edge would look like a perfect margin: leave the edges out
			int bx = Math.Max(2, w * 4 / 100), by = Math.Max(2, h * 4 / 100);
			for (int y = by; y < h - by; y++)
			{
				int first = -1, last = -1, n = 0;
				for (int x = bx; x < w - bx; x++) if (dark[y * w + x]) { if (first < 0) first = x; last = x; n++; }
				if (n >= 3) { lefts.Add(first); rights.Add(w - 1 - last); }
			}
			if (lefts.Count < 5) return 0;
			double Margin(List<int> v)
			{
				var s = v.OrderBy(a => a).ToList();
				int edge = s[s.Count / 10], near = Math.Max(2, w / 100);
				return v.Count(a => Math.Abs(a - edge) <= near) / (double)v.Count;
			}
			return Margin(lefts) - Margin(rights);
		}

		/// <summary>The picture turned a quarter, clockwise or the other way.</summary>
		internal static Pixels Turn(Pixels src, bool clockwise)
		{
			var dst = new Pixels(src.H, src.W);
			int W = dst.W, H = dst.H;
			Parallel.For(0, H, y =>
			{
				for (int x = 0; x < W; x++)
				{
					int sx = clockwise ? y : src.W - 1 - y, sy = clockwise ? src.H - 1 - x : x;
					Buffer.BlockCopy(src.Data, (sy * src.W + sx) * 4, dst.Data, (y * W + x) * 4, 4);
				}
			});
			return dst;
		}

		/// <summary>A copy at most <paramref name="side"/> pixels long (each pixel taken from its place; for looking, not for keeping).</summary>
		internal static Pixels Smaller(Pixels p, int side)
		{
			double k = Math.Min(1.0, side / (double)Math.Max(p.W, p.H));
			if (k >= 1.0) return p;
			int w = Math.Max(1, (int)(p.W * k)), h = Math.Max(1, (int)(p.H * k));
			var dst = new Pixels(w, h);
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					Buffer.BlockCopy(p.Data, ((int)(y / k) * p.W + (int)(x / k)) * 4, dst.Data, (y * w + x) * 4, 4);
			return dst;
		}

		/// <summary>One colour channel, made small by averaging blocks.</summary>
		static float[] SmallChannel(Pixels p, int ch, int w, int h)
		{
			var sum = new float[w * h];
			var count = new int[w * h];
			for (int y = 0; y < p.H; y++)
			{
				int row = Math.Min(h - 1, y * h / p.H) * w, i = y * p.W * 4 + ch;
				for (int x = 0; x < p.W; x++, i += 4)
				{
					int j = row + Math.Min(w - 1, x * w / p.W);
					sum[j] += p.Data[i]; count[j]++;
				}
			}
			for (int j = 0; j < sum.Length; j++) sum[j] = count[j] > 0 ? sum[j] / count[j] : 255;
			return sum;
		}
	}
}
