using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices.WindowsRuntime;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DeskPalsTools
{
	/// <summary>
	/// A PDF made smaller for e-mail (scans, photos): its pictures become JPEG of a good-enough quality, at most about
	/// 150 dpi of the size they are shown on the page; each picture keeps whichever is smaller, the old or the new.
	/// Pages, text and everything else stay as they are (only the pictures change, and loose data is compressed).
	/// </summary>
	internal static class PdfCompress
	{
		/// <summary>Dots per inch kept: enough to read and print a scan, far less than a camera or a scanner makes.</summary>
		const double Dpi = 150;
		const double Quality = 0.6;
		/// <summary>A picture not found on any page (deep inside something) is kept at most this long.</summary>
		const int LongSideCap = 2000;
		/// <summary>Smaller pictures are not worth it (logos, icons).</summary>
		const int MinBytes = 16 * 1024;
		/// <summary>Where the new PDF is made before it is kept (the tests point it at their own folder, to see it removed).</summary>
		internal static Func<string> TempName = () => Jobs.Temp(".pdf");

		/// <summary>
		/// Makes <paramref name="output"/> from <paramref name="pdf"/>. Returns false (and writes nothing) when it would
		/// not be smaller.
		/// </summary>
		public static async Task<bool> Compress(string pdf, string output, Action<double> progress, CancellationToken cancel)
		{
			long before = new FileInfo(pdf).Length;
			string temp = TempName();
			try
			{
				int pages;
				using (var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify))
				{
					pages = doc.PageCount;
					var shown = ShownSizes(doc);
					var images = doc.Internals.GetAllObjects().OfType<PdfDictionary>().Where(d => d.Stream != null && d.Elements.GetName("/Subtype") == "/Image").ToList();
					for (int i = 0; i < images.Count; i++)
					{
						cancel.ThrowIfCancellationRequested();
						progress?.Invoke(i / (double)Math.Max(1, images.Count) * 0.9);
						// one picture that cannot be read stays as it was: the others still get smaller
						try { await Shrink(images[i], shown.TryGetValue(images[i], out double k) ? k : double.NaN); }
						catch (Exception) { }
					}
					ZipLoose(doc);
					doc.Options.CompressContentStreams = true;
					doc.Options.UseFlateDecoderForJpegImages = PdfSharp.Pdf.PdfUseFlateDecoderForJpegImages.Never;
					doc.Options.FlateEncodeMode = PdfSharp.Pdf.PdfFlateEncodeMode.BestCompression;
					progress?.Invoke(0.92);
					doc.Save(temp);
				}
				// never a PDF with pages missing; and a copy that is not smaller is of no use
				if (PdfWork.Pages(temp) != pages) throw new InvalidOperationException(T.S("pdfpages.err.compress"));
				// a scan the pictures' way could not shrink (CMYK, JBIG2…): every page redrawn, when that is smaller still
				if (new FileInfo(temp).Length > before * 0.9 && PdfRaster.ScanOnly(pdf))
				{
					progress?.Invoke(0.94);
					string redrawn = await PdfRaster.Make(pdf, p => progress?.Invoke(0.94 + p * 0.05), cancel);
					if (redrawn != null)
					{
						if (PdfWork.Pages(redrawn) == pages && new FileInfo(redrawn).Length < new FileInfo(temp).Length) File.Move(redrawn, temp, true);
						else try { File.Delete(redrawn); } catch (Exception) { }
					}
				}
				if (new FileInfo(temp).Length >= before) return false;
				File.Move(temp, output, false);
				return true;
			}
			finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception) { } }
		}

		/// <summary>"12.3 MB" in the user's way of writing numbers.</summary>
		public static string Size(long bytes) => bytes >= 1024 * 1024
			? T.S("pdfpages.size.mb", (bytes / 1048576.0).ToString("0.0", CultureInfo.CurrentCulture))
			: T.S("pdfpages.size.kb", Math.Max(1, (int)Math.Round(bytes / 1024.0)).ToString("N0", CultureInfo.CurrentCulture));

		// ------------------------------------------------------------------ how big each picture is shown

		/// <summary>
		/// For each picture, the most it may be scaled (≤ 1) and still have <see cref="Dpi"/> where it is shown largest.
		/// Read from the pages' drawing commands (the "cm" scales before each "Do"), also inside forms.
		/// </summary>
		static Dictionary<PdfDictionary, double> ShownSizes(PdfDocument doc)
		{
			var scale = new Dictionary<PdfDictionary, double>(ReferenceEqualityComparer.Instance);
			foreach (var page in doc.Pages)
			{
				try { Walk(ContentReader.ReadContent(page), page.Elements.GetDictionary("/Resources"), Matrix.Identity, scale, 0); }
				catch (Exception) { }
			}
			return scale;
		}

		readonly record struct Matrix(double A, double B, double C, double D)
		{
			public static readonly Matrix Identity = new(1, 0, 0, 1);
			/// <summary>This one first, then <paramref name="m"/> (PDF order: the new "cm" times the current one).</summary>
			public Matrix Then(Matrix m) => new(A * m.A + B * m.C, A * m.B + B * m.D, C * m.A + D * m.C, C * m.B + D * m.D);
		}

		static void Walk(CSequence ops, PdfDictionary resources, Matrix ctm, Dictionary<PdfDictionary, double> scale, int depth)
		{
			if (depth > 8) return;
			var stack = new Stack<Matrix>();
			var xobjects = resources?.Elements.GetDictionary("/XObject");
			foreach (var item in ops)
			{
				if (item is not COperator op) continue;
				switch (op.OpCode.Name)
				{
					case "q": stack.Push(ctm); break;
					case "Q": if (stack.Count > 0) ctm = stack.Pop(); break;
					case "cm":
						if (op.Operands.Count >= 4) ctm = new Matrix(Num(op.Operands[0]), Num(op.Operands[1]), Num(op.Operands[2]), Num(op.Operands[3])).Then(ctm);
						break;
					case "Do":
						if (op.Operands.Count < 1 || op.Operands[0] is not CName name || xobjects == null) break;
						var x = xobjects.Elements.GetDictionary(name.Name);
						if (x == null) break;
						string sub = x.Elements.GetName("/Subtype");
						if (sub == "/Image") Seen(x, ctm, scale);
						else if (sub == "/Form" && x.Stream != null)
						{
							var m = x.Elements.GetArray("/Matrix");
							var fm = m != null && m.Elements.Count >= 4 ? new Matrix(m.Elements.GetReal(0), m.Elements.GetReal(1), m.Elements.GetReal(2), m.Elements.GetReal(3)) : Matrix.Identity;
							try { Walk(ContentReader.ReadContent(x.Stream.UnfilteredValue), x.Elements.GetDictionary("/Resources") ?? resources, fm.Then(ctm), scale, depth + 1); }
							catch (Exception) { }
						}
						break;
				}
			}
		}

		static double Num(CObject o) => o switch { CInteger i => i.Value, CReal r => r.Value, _ => 0 };

		/// <summary>A picture drawn with this matrix: its 1×1 square becomes |(a,b)| wide and |(c,d)| tall, in points.</summary>
		static void Seen(PdfDictionary image, Matrix ctm, Dictionary<PdfDictionary, double> scale)
		{
			int w = image.Elements.GetInteger("/Width"), h = image.Elements.GetInteger("/Height");
			if (w <= 0 || h <= 0) return;
			double shownW = Math.Sqrt(ctm.A * ctm.A + ctm.B * ctm.B), shownH = Math.Sqrt(ctm.C * ctm.C + ctm.D * ctm.D);
			double k = Math.Max(shownW / 72 * Dpi / w, shownH / 72 * Dpi / h);
			if (double.IsNaN(k) || double.IsInfinity(k)) return;
			scale[image] = Math.Max(scale.GetValueOrDefault(image), k);
		}

		// ------------------------------------------------------------------ one picture

		/// <summary>The picture as a JPEG, scaled by <paramref name="k"/> (NaN: not seen on a page), when that is smaller.</summary>
		static async Task Shrink(PdfDictionary image, double k)
		{
			var e = image.Elements;
			byte[] old = image.Stream.Value;
			if (old == null || old.Length < MinBytes) return;
			if (e.ContainsKey("/SMask") || e.ContainsKey("/Mask") || e.GetBoolean("/ImageMask") || e.ContainsKey("/Decode")) return;
			if (e.GetInteger("/BitsPerComponent") != 8) return;
			int comps = Components(image);
			if (comps == 0) return;
			int w = e.GetInteger("/Width"), h = e.GetInteger("/Height");
			if (w <= 0 || h <= 0) return;
			string filter = Filter(image);
			byte[] pixels = filter switch
			{
				"/DCTDecode" => await FromJpeg(old, w, h, comps),
				"/FlateDecode" => FromFlate(old, image, w, h, comps),
				"" => old.Length == (long)w * h * comps ? (comps == 1 ? old : ToBgra(old, w, h)) : null,
				_ => null,
			};
			if (pixels == null) return;
			if (double.IsNaN(k)) k = LongSideCap / (double)Math.Max(w, h);
			// never smaller than a small picture (a matrix that shows it almost nowhere is not a reason to ruin it)
			k = Math.Min(1, Math.Max(k, 64.0 / Math.Max(w, h)));
			int nw = Math.Max(1, (int)Math.Round(w * k)), nh = Math.Max(1, (int)Math.Round(h * k));
			byte[] jpeg = await ToJpeg(pixels, w, h, comps, nw, nh);
			if (jpeg == null || jpeg.Length >= old.Length) return;
			image.Stream.Value = jpeg;
			e.SetName("/Filter", "/DCTDecode");
			e.Remove("/DecodeParms");
			e.SetInteger("/Width", nw);
			e.SetInteger("/Height", nh);
			e.SetInteger("/Length", jpeg.Length);
		}

		/// <summary>The one filter of a picture ("" when none); "?" when there are several (left alone).</summary>
		static string Filter(PdfDictionary image)
		{
			var f = image.Elements["/Filter"];
			if (f is PdfReference r) f = r.Value;
			return f switch
			{
				null => "",
				PdfName n => n.Value,
				PdfArray a when a.Elements.Count == 1 => a.Elements.GetName(0),
				PdfArray a when a.Elements.Count == 0 => "",
				_ => "?",
			};
		}

		/// <summary>3 for RGB, 1 for grey (also with a colour profile); 0 for anything else (CMYK, palettes…, left alone).</summary>
		static int Components(PdfDictionary image)
		{
			var cs = image.Elements["/ColorSpace"];
			if (cs is PdfReference r) cs = r.Value;
			if (cs is PdfName n) return n.Value switch { "/DeviceRGB" => 3, "/DeviceGray" => 1, _ => 0 };
			if (cs is PdfArray a && a.Elements.Count == 2 && a.Elements.GetName(0) == "/ICCBased")
			{
				var profile = a.Elements.GetDictionary(1);
				int count = profile?.Elements.GetInteger("/N") ?? 0;
				return count is 1 or 3 ? count : 0;
			}
			return 0;
		}

		/// <summary>The pixels of a JPEG (grey: one byte each; colour: BGRA), or null when it is not what the PDF says.</summary>
		static async Task<byte[]> FromJpeg(byte[] jpeg, int w, int h, int comps)
		{
			using var ms = new InMemoryRandomAccessStream();
			await ms.WriteAsync(jpeg.AsBuffer());
			ms.Seek(0);
			var decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.JpegDecoderId, ms);
			if (decoder.PixelWidth != w || decoder.PixelHeight != h) return null;
			var data = await decoder.GetPixelDataAsync(comps == 1 ? BitmapPixelFormat.Gray8 : BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(),
				ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
			return data.DetachPixelData();
		}

		/// <summary>The pixels of a Flate picture (grey: one byte each; colour: BGRA), PNG predictors undone; null when unknown.</summary>
		static byte[] FromFlate(byte[] data, PdfDictionary image, int w, int h, int comps)
		{
			var parms = image.Elements.GetDictionary("/DecodeParms");
			if (parms == null && image.Elements["/DecodeParms"] is PdfArray pa && pa.Elements.Count == 1) parms = pa.Elements.GetDictionary(0);
			int predictor = parms?.Elements.GetInteger("/Predictor") ?? 1;
			if (predictor == 0) predictor = 1;
			if (predictor != 1 && predictor < 10) return null;
			if (predictor >= 10)
			{
				int colors = parms.Elements.ContainsKey("/Colors") ? parms.Elements.GetInteger("/Colors") : 1;
				int bpc = parms.Elements.ContainsKey("/BitsPerComponent") ? parms.Elements.GetInteger("/BitsPerComponent") : 8;
				int columns = parms.Elements.ContainsKey("/Columns") ? parms.Elements.GetInteger("/Columns") : 1;
				if (colors != comps || bpc != 8 || columns != w) return null;
			}
			int row = w * comps;
			long expect = (long)row * h + (predictor >= 10 ? h : 0);
			if (expect > 400_000_000) return null;
			byte[] raw = new byte[expect];
			using (var z = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress))
			{
				int got = 0, n;
				while (got < raw.Length && (n = z.Read(raw, got, raw.Length - got)) > 0) got += n;
				if (got < raw.Length) return null;
			}
			byte[] plain = predictor >= 10 ? Unpredict(raw, row, h, comps) : raw;
			if (plain == null) return null;
			return comps == 1 ? plain : ToBgra(plain, w, h);
		}

		/// <summary>RGB → BGRA, as the encoder takes it.</summary>
		static byte[] ToBgra(byte[] rgb, int w, int h)
		{
			var bgra = new byte[(long)w * h * 4];
			for (long i = 0, j = 0; i + 2 < rgb.Length && j + 3 < bgra.Length; i += 3, j += 4)
			{
				bgra[j] = rgb[i + 2]; bgra[j + 1] = rgb[i + 1]; bgra[j + 2] = rgb[i]; bgra[j + 3] = 255;
			}
			return bgra;
		}

		/// <summary>The PNG filters undone (each row starts with its filter: none, sub, up, average, Paeth).</summary>
		static byte[] Unpredict(byte[] raw, int row, int h, int bpp)
		{
			var outp = new byte[(long)row * h];
			for (int y = 0; y < h; y++)
			{
				int type = raw[(long)y * (row + 1)];
				long src = (long)y * (row + 1) + 1, dst = (long)y * row, up = dst - row;
				for (int x = 0; x < row; x++)
				{
					int a = x >= bpp ? outp[dst + x - bpp] : 0, b = y > 0 ? outp[up + x] : 0, c = x >= bpp && y > 0 ? outp[up + x - bpp] : 0;
					int v = raw[src + x];
					v += type switch
					{
						0 => 0,
						1 => a,
						2 => b,
						3 => (a + b) / 2,
						4 => Paeth(a, b, c),
						_ => throw new InvalidDataException(),
					};
					outp[dst + x] = (byte)v;
				}
			}
			return outp;
		}

		static int Paeth(int a, int b, int c)
		{
			int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
			return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
		}

		/// <summary>The pixels as a JPEG of <see cref="Quality"/>, made <paramref name="nw"/>×<paramref name="nh"/>.</summary>
		static async Task<byte[]> ToJpeg(byte[] pixels, int w, int h, int comps, int nw, int nh)
		{
			using var ms = new InMemoryRandomAccessStream();
			var props = new BitmapPropertySet { { "ImageQuality", new BitmapTypedValue(Quality, Windows.Foundation.PropertyType.Single) } };
			var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms, props);
			encoder.SetPixelData(comps == 1 ? BitmapPixelFormat.Gray8 : BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)w, (uint)h, 96, 96, pixels);
			if (nw != w || nh != h)
			{
				encoder.BitmapTransform.ScaledWidth = (uint)nw;
				encoder.BitmapTransform.ScaledHeight = (uint)nh;
				encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
			}
			await encoder.FlushAsync();
			var bytes = new byte[ms.Size];
			ms.Seek(0);
			await ms.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);
			return bytes;
		}

		/// <summary>Data kept without compression (fonts, drawings…) is compressed, losing nothing.</summary>
		static void ZipLoose(PdfDocument doc)
		{
			foreach (var d in doc.Internals.GetAllObjects().OfType<PdfDictionary>())
			{
				if (d.Stream?.Value == null || d.Stream.Value.Length < 256 || d.Elements.ContainsKey("/Filter")) continue;
				// the description of the file (XMP) stays readable, as the standard advises
				if (d.Elements.GetName("/Type") == "/Metadata" || d.Elements.GetName("/Type") == "/XRef") continue;
				try { d.Stream.Zip(); } catch (Exception) { }
			}
		}
	}
}
