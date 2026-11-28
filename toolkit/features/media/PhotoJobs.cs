using System.Runtime.CompilerServices;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// Photos: "Resize" (several at once, to a size for e-mail or a chat; never made bigger) and "Remove the
	/// background" (a plain background — a wall, a sheet, a studio backdrop — taken out from the edges in, for a
	/// see-through PNG or a white background, as for a document photo).
	/// </summary>
	internal static class PhotoJobs
	{
		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "resize", Emoji = "📐",
				Rank = f => f.Count > 0 && f.All(x => File.Exists(x) && IsImage(x)) ? 33 : -1,
				Ask = (owner, files) => AskSize(owner),
				Run = Resize,
			});
			Register(new JobDef
			{
				Id = "nobackground", Emoji = "✂️",
				Rank = f => f.Count > 0 && f.All(x => File.Exists(x) && IsImage(x)) ? 36 : -1,
				Ask = (owner, files) => AskBackground(owner),
				Run = NoBackground,
			});
		}

		// ------------------------------------------------------------------ resize

		static Dictionary<string, string> AskSize(IWin32Window owner)
		{
			string last = ToolSettings.Text("resizeTo", "1920");
			var a = Ask.Show(owner, T.S("job.resize"), T.S("resize.intro"),
				new Ask.Field
				{
					Key = "side", Label = T.S("resize.size"), Kind = Ask.Kind.Choice, Value = last is "1280" or "1920" or "2560" or "800" ? last : "custom",
					Choices = new[] { ("800", T.S("resize.tiny")), ("1280", T.S("resize.small")), ("1920", T.S("resize.medium")), ("2560", T.S("resize.large")), ("custom", T.S("resize.custom")) },
				},
				new Ask.Field { Key = "custom", Label = T.S("resize.customSide"), Kind = Ask.Kind.Number, Value = last is "1280" or "1920" or "2560" or "800" ? "1600" : last, Min = 100, Max = 10000 });
			if (a == null) return null;
			string side = a["side"] == "custom" ? a["custom"] : a["side"];
			try { ToolSettings.Set("resizeTo", side); } catch (Exception) { }
			return new Dictionary<string, string> { ["side"] = side };
		}

		static async Task Resize(JobContext c)
		{
			int side = int.TryParse(c.Option("side", "1920"), out int s) ? Math.Clamp(s, 100, 10000) : 1920;
			// one photo: next to it; several: in a folder, with their own names
			string folder = c.Files.Count > 1 ? OutFolder(c.Files[0], T.S("resize.folder")) : null;
			long before = 0, after = 0;
			await c.Each(async (file, i) =>
			{
				c.Report(i / (double)c.Files.Count, Path.GetFileName(file));
				bool png = Ext(file) == ".png";
				string ext = png ? ".png" : ".jpg";
				string o = folder == null ? OutName(file, ext, T.S("resize.name")) : Free(Path.Combine(folder, Path.GetFileNameWithoutExtension(file) + ext));
				await ImageWork.Convert(file, o, png, side);
				before += new FileInfo(file).Length;
				after += new FileInfo(o).Length;
				if (folder == null) c.Outputs.Add(o);
			});
			if (folder != null) c.Outputs.Add(folder);
			c.Preview = PdfCompress.Size(before) + " → " + PdfCompress.Size(after);
		}

		/// <summary>A name not taken yet in a folder ("photo.jpg", "photo (2).jpg"…).</summary>
		static string Free(string path)
		{
			string dir = Path.GetDirectoryName(path), name = Path.GetFileNameWithoutExtension(path), ext = Path.GetExtension(path);
			for (int n = 2; File.Exists(path); n++) path = Path.Combine(dir, name + " (" + n + ")" + ext);
			return path;
		}

		// ------------------------------------------------------------------ remove the background

		static Dictionary<string, string> AskBackground(IWin32Window owner) => Ask.Show(owner, T.S("job.nobackground"), T.S("nobg.intro"),
			new Ask.Field
			{
				Key = "to", Label = T.S("nobg.to"), Kind = Ask.Kind.Choice, Value = ToolSettings.Text("nobgTo", "clear"),
				Choices = new[] { ("clear", T.S("nobg.clear")), ("white", T.S("nobg.white")) },
			});

		static async Task NoBackground(JobContext c)
		{
			bool white = c.Option("to", "clear") == "white";
			try { ToolSettings.Set("nobgTo", white ? "white" : "clear"); } catch (Exception) { }
			await c.Each(async (file, i) =>
			{
				c.Report(i / (double)c.Files.Count, Path.GetFileName(file));
				Pixels px;
				try { px = await ScanImage.Load(file); }
				catch (Exception ex) when (ex is not (IOException or UnauthorizedAccessException))
				{
					throw new InvalidOperationException(HeicHelp.Explain(file) ?? T.S("docs.scan.noPicture"));
				}
				// big photos are made a size where the work is quick (and enough for a document photo or a product)
				if (Math.Max(px.W, px.H) > 2400) px = ScanImage.Smaller(px, 2400);
				var result = await Task.Run(() => Background.Remove(px, white, c.Cancel), c.Cancel);
				if (result == null) throw new InvalidOperationException(T.S("nobg.notPlain"));
				string o = OutName(file, white ? ".jpg" : ".png", T.S("nobg.name"));
				if (white) await ViewerJobs.Encode(result, o, false);
				else await Background.SavePng(result, o);
				c.Outputs.Add(o);
			});
		}
	}

	/// <summary>
	/// A plain background taken out of a photo: its colour is learnt from the photo's edges, then everything joined to
	/// the edges and close to that colour (in light and hue) is made see-through, with a soft edge. A background of many
	/// colours (a room, a street) is not plain: then nothing is done.
	/// </summary>
	internal static class Background
	{
		/// <summary>The photo with its background taken out (see-through, or white), or null when the background is not plain.</summary>
		public static Pixels Remove(Pixels p, bool white, CancellationToken cancel = default)
		{
			int w = p.W, h = p.H;
			var d = p.Data;
			// the edges: their colour, and how much it changes
			var edge = new List<(int b, int g, int r)>();
			int step = Math.Max(1, (w + h) / 400);
			for (int x = 0; x < w; x += step) { edge.Add(Px(d, w, x, 0)); edge.Add(Px(d, w, x, h - 1)); }
			for (int y = 0; y < h; y += step) { edge.Add(Px(d, w, 0, y)); edge.Add(Px(d, w, w - 1, y)); }
			var (mb, mg, mr) = Median(edge);
			double spread = edge.Select(e => Dist(e, (mb, mg, mr))).OrderBy(x => x).ElementAt((int)(edge.Count * 0.8));
			// a background whose edges are of many colours is not plain
			if (spread > 60) return null;
			double tol = Math.Clamp(spread * 2.2 + 18, 24, 70);
			// from the edges in: pixels close to the background's colour, and joined to it, are background
			var bg = new bool[w * h];
			var queue = new Queue<int>();
			void Try(int x, int y)
			{
				int i = y * w + x;
				if (bg[i]) return;
				if (Dist(Px(d, w, x, y), (mb, mg, mr)) > tol) return;
				bg[i] = true;
				queue.Enqueue(i);
			}
			for (int x = 0; x < w; x++) { Try(x, 0); Try(x, h - 1); }
			for (int y = 0; y < h; y++) { Try(0, y); Try(w - 1, y); }
			int n = 0;
			while (queue.Count > 0)
			{
				if ((++n & 0xFFFF) == 0) cancel.ThrowIfCancellationRequested();
				int i = queue.Dequeue(), x = i % w, y = i / w;
				if (x > 0) Try(x - 1, y);
				if (x < w - 1) Try(x + 1, y);
				if (y > 0) Try(x, y - 1);
				if (y < h - 1) Try(x, y + 1);
			}
			int count = bg.Count(b => b);
			// almost nothing (no plain background) or almost everything (no subject): nothing to do
			if (count < w * h * 0.05 || count > w * h * 0.97) return null;
			// a soft edge: the see-through grows over two pixels around the subject
			var alpha = new byte[w * h];
			for (int i = 0; i < alpha.Length; i++) alpha[i] = bg[i] ? (byte)0 : (byte)255;
			alpha = Soften(alpha, w, h);
			var o = new Pixels(w, h);
			for (int i = 0, j = 0; i < alpha.Length; i++, j += 4)
			{
				int a = alpha[i];
				if (white)
				{
					// on white: the colour mixed with white as much as it is see-through
					o.Data[j] = (byte)((d[j] * a + 255 * (255 - a)) / 255);
					o.Data[j + 1] = (byte)((d[j + 1] * a + 255 * (255 - a)) / 255);
					o.Data[j + 2] = (byte)((d[j + 2] * a + 255 * (255 - a)) / 255);
					o.Data[j + 3] = 255;
				}
				else
				{
					// premultiplied, as Windows' encoder takes it
					o.Data[j] = (byte)(d[j] * a / 255);
					o.Data[j + 1] = (byte)(d[j + 1] * a / 255);
					o.Data[j + 2] = (byte)(d[j + 2] * a / 255);
					o.Data[j + 3] = (byte)a;
				}
			}
			return o;
		}

		static (int b, int g, int r) Px(byte[] d, int w, int x, int y) { int i = (y * w + x) * 4; return (d[i], d[i + 1], d[i + 2]); }

		static (int, int, int) Median(List<(int b, int g, int r)> list)
		{
			int M(IEnumerable<int> v) { var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
			return (M(list.Select(x => x.b)), M(list.Select(x => x.g)), M(list.Select(x => x.r)));
		}

		/// <summary>How far two colours are, as the eye sees it (red and green count more than blue).</summary>
		static double Dist((int b, int g, int r) a, (int b, int g, int r) c)
		{
			double db = a.b - c.b, dg = a.g - c.g, dr = a.r - c.r;
			return Math.Sqrt(0.3 * dr * dr + 0.59 * dg * dg + 0.11 * db * db) * 1.4;
		}

		static byte[] Soften(byte[] a, int w, int h)
		{
			var o = new byte[a.Length];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					int sum = 0, k = 0;
					for (int dy = -1; dy <= 1; dy++)
						for (int dx = -1; dx <= 1; dx++)
						{
							int xx = x + dx, yy = y + dy;
							if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
							sum += a[yy * w + xx]; k++;
						}
					o[y * w + x] = (byte)(sum / k);
				}
			return o;
		}

		/// <summary>A see-through PNG.</summary>
		public static async Task SavePng(Pixels p, string file)
		{
			using var output = File.Create(file);
			var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, output.AsRandomAccessStream());
			encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, (uint)p.W, (uint)p.H, 96, 96, p.Data);
			await encoder.FlushAsync();
		}
	}
}
