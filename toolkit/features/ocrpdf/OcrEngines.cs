using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>One word the OCR found: its text and its box, in pixels of the picture that was read. Words of one line share <see cref="Line"/>.</summary>
	internal sealed record OcrWord(string Text, double X, double Y, double W, double H, int Line);

	/// <summary>What the OCR found on one picture: the words with their boxes and the plain text (lines with new lines).</summary>
	internal sealed class OcrPage
	{
		public int Width, Height;
		public List<OcrWord> Words { get; } = new();
		public string Text = "";
	}

	/// <summary>A picture to read: BGRA pixels (white behind what was see-through) and its resolution.</summary>
	internal sealed class OcrImage
	{
		public byte[] Bgra;
		public int Width, Height;
		public double Dpi = 96;
	}

	/// <summary>
	/// An OCR engine, whichever it is (Windows' own or Tesseract): the same words-with-boxes for "searchable" and the same
	/// plain text for "ocr". <see cref="MaxSide"/> is the biggest picture it takes (bigger ones are made smaller first).
	/// </summary>
	internal interface IOcrEngine : IDisposable
	{
		string Name { get; }
		int MaxSide { get; }
		Task<OcrPage> Read(OcrImage image, CancellationToken cancel);
	}

	/// <summary>The OCR of Windows: no download, offline, in the languages this Windows has.</summary>
	internal sealed class WindowsOcr : IOcrEngine
	{
		readonly OcrEngine engine;

		public WindowsOcr()
		{
			engine = OcrEngine.TryCreateFromUserProfileLanguages();
			if (engine == null)
				foreach (var lang in OcrEngine.AvailableRecognizerLanguages)
					if ((engine = OcrEngine.TryCreateFromLanguage(lang)) != null) break;
			if (engine == null) throw new InvalidOperationException(T.S("err.noOcr"));
		}

		public string Name => "windows";
		public int MaxSide => (int)OcrEngine.MaxImageDimension;

		public async Task<OcrPage> Read(OcrImage image, CancellationToken cancel)
		{
			cancel.ThrowIfCancellationRequested();
			using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(image.Bgra.AsBuffer(), BitmapPixelFormat.Bgra8, image.Width, image.Height, BitmapAlphaMode.Premultiplied);
			var result = await engine.RecognizeAsync(bitmap);
			var page = new OcrPage { Width = image.Width, Height = image.Height };
			int line = 0;
			foreach (var l in result.Lines)
			{
				foreach (var w in l.Words)
					if (!string.IsNullOrWhiteSpace(w.Text)) page.Words.Add(new OcrWord(w.Text, w.BoundingRect.X, w.BoundingRect.Y, w.BoundingRect.Width, w.BoundingRect.Height, line));
				line++;
			}
			page.Text = string.Join(Environment.NewLine, result.Lines.Select(l => l.Text));
			return page;
		}

		public void Dispose() { }
	}

	/// <summary>
	/// Tesseract (Apache-2.0), for bad scans and languages Windows lacks. Its two DLLs travel inside the program and are put
	/// in <see cref="NativeFolder"/> the first time; its language data comes from <see cref="TessData"/>.
	/// </summary>
	internal sealed class TesseractOcr : IOcrEngine
	{
		/// <summary>Where the DLLs are put (a folder per version, "x64" inside it as Tesseract wants). Tests point it elsewhere.</summary>
		public static string NativeFolder;
		const string Version = "5.2.0";

		readonly Tesseract.TesseractEngine engine;

		public TesseractOcr(string tessdata, string languages)
		{
			PrepareNatives();
			engine = new Tesseract.TesseractEngine(tessdata, languages, Tesseract.EngineMode.LstmOnly);
		}

		public string Name => "tesseract";
		public int MaxSide => 6000;

		/// <summary>The DLLs inside the program, written once to a folder of their own (a copy that is complete is never rewritten: it may be in use).</summary>
		static void PrepareNatives()
		{
			string root = Path.Combine(NativeFolder ?? Path.Combine(Places.Folder, "tesseract"), Version);
			string dir = Path.Combine(root, "x64");
			Directory.CreateDirectory(dir);
			var asm = Assembly.GetExecutingAssembly();
			const string prefix = "DeskPalsTools.tesseract.";
			var names = asm.GetManifestResourceNames().Where(n => n.StartsWith(prefix) && n.EndsWith(".dll")).ToList();
			if (names.Count == 0) throw new InvalidOperationException(T.S("ocrpdf.err.tesseract"));
			foreach (var name in names)
			{
				using var s = asm.GetManifestResourceStream(name);
				string file = Path.Combine(dir, name[prefix.Length..]);
				if (File.Exists(file) && new FileInfo(file).Length == s.Length) continue;
				string tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
				using (var f = File.Create(tmp)) s.CopyTo(f);
				File.Move(tmp, file, true);
			}
			Tesseract.TesseractEnviornment.CustomSearchPath = root;
		}

		public Task<OcrPage> Read(OcrImage image, CancellationToken cancel) => Task.Run(() =>
		{
			cancel.ThrowIfCancellationRequested();
			using var pix = ToPix(image);
			using var result = engine.Process(pix, Tesseract.PageSegMode.Auto);
			var page = new OcrPage { Width = image.Width, Height = image.Height, Text = (result.GetText() ?? "").Trim() };
			using var it = result.GetIterator();
			it.Begin();
			int line = -1;
			do
			{
				if (it.IsAtBeginningOf(Tesseract.PageIteratorLevel.TextLine)) line++;
				string text = it.GetText(Tesseract.PageIteratorLevel.Word)?.Trim();
				if (string.IsNullOrEmpty(text) || !it.TryGetBoundingBox(Tesseract.PageIteratorLevel.Word, out var r)) continue;
				// very unsure words are noise (specks, lines of a table)
				if (it.GetConfidence(Tesseract.PageIteratorLevel.Word) < 30) continue;
				page.Words.Add(new OcrWord(text, r.X1, r.Y1, r.Width, r.Height, Math.Max(0, line)));
			}
			while (it.Next(Tesseract.PageIteratorLevel.Word));
			return page;
		}, cancel);

		/// <summary>BGRA pixels → a 32-bit picture of Leptonica (one uint per pixel, red in the highest byte).</summary>
		static unsafe Tesseract.Pix ToPix(OcrImage image)
		{
			var pix = Tesseract.Pix.Create(image.Width, image.Height, 32);
			var data = pix.GetData();
			uint* start = (uint*)data.Data;
			fixed (byte* src = image.Bgra)
				for (int y = 0; y < image.Height; y++)
				{
					uint* row = start + y * data.WordsPerLine;
					byte* p = src + y * image.Width * 4;
					for (int x = 0; x < image.Width; x++, p += 4)
						row[x] = (uint)(p[2] << 24 | p[1] << 16 | p[0] << 8 | 0xFF);
				}
			pix.XRes = pix.YRes = (int)Math.Round(image.Dpi);
			return pix;
		}

		public void Dispose() => engine.Dispose();
	}

	/// <summary>
	/// The language data of Tesseract (tessdata_fast, from the official tesseract-ocr repository on GitHub): the program's
	/// language and English, downloaded the first time into %LocalAppData%\DesktopPet\extensions\tessdata.
	/// </summary>
	internal static class TessData
	{
		/// <summary>Where the files go and where they come from; tests point them elsewhere.</summary>
		public static string Folder;
		public static string BaseUrl = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/";
		/// <summary>A real language file is a few MB: smaller or much bigger is an error page or a broken download.</summary>
		public const long MinSize = 100_000, MaxSize = 150_000_000;

		/// <summary>How long the server may take to answer, and how long the data may stop coming, before a download is given up.</summary>
		public static TimeSpan AnswerWait = TimeSpan.FromSeconds(20), StallWait = TimeSpan.FromSeconds(30);

		public static string Dir
		{
			get
			{
				string d = Folder ?? Path.Combine(Places.Folder, "tessdata");
				Directory.CreateDirectory(d);
				return d;
			}
		}

		static readonly Dictionary<string, string> Codes = new(StringComparer.OrdinalIgnoreCase)
		{
			["en"] = "eng", ["pt-BR"] = "por", ["pt"] = "por", ["es"] = "spa", ["de"] = "deu", ["fr"] = "fra", ["it"] = "ita", ["ru"] = "rus",
			["tr"] = "tur", ["id"] = "ind", ["vi"] = "vie", ["ja"] = "jpn", ["ko"] = "kor", ["zh-CN"] = "chi_sim", ["zh"] = "chi_sim",
			["ar"] = "ara", ["fa"] = "fas", ["ur"] = "urd", ["hi"] = "hin", ["bn"] = "ben", ["mr"] = "mar", ["ta"] = "tam", ["te"] = "tel",
			["th"] = "tha", ["sw"] = "swa",
		};

		/// <summary>Every language Tesseract can read here: (a language of the program, Tesseract's name), one per Tesseract name.</summary>
		public static List<(string code, string tess)> All() => Codes.GroupBy(kv => kv.Value).Select(g => (g.OrderByDescending(kv => kv.Key.Length).First().Key, g.Key)).ToList();
		
		/// <summary>The Tesseract languages for a language of the program: "pt-BR" → "por+eng", "en" → "eng".</summary>
		public static string LanguagesFor(string code)
		{
			string main = Codes.TryGetValue(code ?? "", out var c) ? c : Codes.TryGetValue((code ?? "").Split('-')[0], out c) ? c : "eng";
			return main == "eng" ? "eng" : main + "+eng";
		}

		/// <summary>Downloads the files of these languages that are not here yet, with their progress (0..1, what).</summary>
		public static async Task Ensure(string languages, Action<double, string> progress, CancellationToken cancel)
		{
			var missing = languages.Split('+').Where(l => !File.Exists(Path.Combine(Dir, l + ".traineddata"))).ToList();
			if (missing.Count == 0) return;
			// no overall limit: a slow but working connection may take its time; a stalled one is stopped in Download
			using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
			http.DefaultRequestHeaders.UserAgent.ParseAdd("DeskPalsTools/1.0");
			for (int i = 0; i < missing.Count; i++)
			{
				int at = i;
				await Download(http, missing[i], p => progress?.Invoke((at + p) / missing.Count, T.S("ocrpdf.downloading", (int)Math.Round((at + p) / missing.Count * 100))), cancel);
			}
		}

		static async Task Download(HttpClient http, string lang, Action<double> progress, CancellationToken cancel)
		{
			string file = Path.Combine(Dir, lang + ".traineddata");
			string part = file + ".part";
			// a connection that hangs (a hotel's login page, a very bad network) is given up quickly, so Windows OCR takes
			// over in seconds instead of minutes: the answer must start in AnswerWait and data must keep coming within StallWait
			using var stall = CancellationTokenSource.CreateLinkedTokenSource(cancel);
			try
			{
				stall.CancelAfter(AnswerWait);
				using var response = await http.GetAsync(BaseUrl + lang + ".traineddata", HttpCompletionOption.ResponseHeadersRead, stall.Token);
				response.EnsureSuccessStatusCode();
				long? expected = response.Content.Headers.ContentLength;
				if (expected is < MinSize or > MaxSize) throw new InvalidDataException();
				long got = 0;
				stall.CancelAfter(StallWait);
				using (var input = await response.Content.ReadAsStreamAsync(stall.Token))
				using (var output = File.Create(part))
				{
					var buffer = new byte[81920];
					int n;
					while ((n = await input.ReadAsync(buffer, stall.Token)) > 0)
					{
						stall.CancelAfter(StallWait);
						await output.WriteAsync(buffer.AsMemory(0, n), cancel);
						got += n;
						if (got > MaxSize) throw new InvalidDataException();
						if (expected > 0) progress(got / (double)expected.Value);
					}
				}
				// the size check: all of it arrived, and it is the size of a language file
				if ((expected != null && got != expected) || got < MinSize) throw new InvalidDataException();
				File.Move(part, file, true);
			}
			catch (OperationCanceledException) when (cancel.IsCancellationRequested) { throw; }
			catch (Exception) { throw new InvalidOperationException(T.S("ocrpdf.err.download")); }
			finally { try { File.Delete(part); } catch (Exception) { } }
		}
	}

	/// <summary>
	/// Which OCR is used, and reading with it. The choice is "ocrEngine" in the extension's settings ("windows", the
	/// default, or "tesseract"). When Tesseract cannot be used (no internet for its first download, a DLL that does not
	/// load…) the OCR of Windows does the work and <c>notice</c> tells the user once.
	/// </summary>
	internal static class OcrText
	{
		public const string Key = "ocrEngine";

		/// <summary>"tesseract" or "windows" (an older true/false "tesseract" setting is honoured too).</summary>
		public static string Choice()
		{
			JsonObject o = ToolSettings.Read();
			try
			{
				string v = (string)o[Key];
				if (!string.IsNullOrEmpty(v)) return v.Equals("tesseract", StringComparison.OrdinalIgnoreCase) ? "tesseract" : "windows";
			}
			catch (Exception) { }
			return ToolSettings.Bool("tesseract") ? "tesseract" : "windows";
		}

		public static void Choose(bool tesseract) => ToolSettings.Set(Key, tesseract ? "tesseract" : "windows");

		/// <summary>The chosen engine, ready (Tesseract's files downloaded the first time). Dispose it at the end.</summary>
		public static async Task<IOcrEngine> Open(Action<double, string> progress, CancellationToken cancel, Action<string> notice)
		{
			if (Choice() != "tesseract") return new WindowsOcr();
			try
			{
				string langs = TessLanguages.Current();
				await TessData.Ensure(langs, progress, cancel);
				return new Fallback(new TesseractOcr(TessData.Dir, langs), notice);
			}
			catch (OperationCanceledException) when (cancel.IsCancellationRequested) { throw; }
			catch (Exception ex)
			{
				notice?.Invoke(T.S("ocrpdf.fallback", Reason(ex)));
				return new WindowsOcr();
			}
		}

		static string Reason(Exception ex) => ex is InvalidOperationException && ex.Message.Length > 0 && ex.Message.Length < 200 ? ex.Message : T.S("ocrpdf.err.tesseract");

		/// <summary>Tesseract that, when it fails on a page, gives the work to the OCR of Windows (once and for the rest).</summary>
		sealed class Fallback : IOcrEngine
		{
			IOcrEngine current;
			readonly Action<string> notice;
			public Fallback(IOcrEngine first, Action<string> notice) { current = first; this.notice = notice; }
			public string Name => current.Name;
			public int MaxSide => current.MaxSide;

			public async Task<OcrPage> Read(OcrImage image, CancellationToken cancel)
			{
				try { return await current.Read(image, cancel); }
				catch (OperationCanceledException) when (cancel.IsCancellationRequested) { throw; }
				catch (Exception) when (current is TesseractOcr)
				{
					current.Dispose();
					current = new WindowsOcr();
					notice?.Invoke(T.S("ocrpdf.fallback", T.S("ocrpdf.err.tesseract")));
					return await current.Read(Fit(image, current.MaxSide), cancel);
				}
			}

			public void Dispose() => current.Dispose();
		}

		/// <summary>A picture made smaller when it is bigger than an engine takes.</summary>
		static OcrImage Fit(OcrImage image, int maxSide)
		{
			if (Math.Max(image.Width, image.Height) <= maxSide) return image;
			double k = maxSide / (double)Math.Max(image.Width, image.Height);
			int w = Math.Max(1, (int)(image.Width * k)), h = Math.Max(1, (int)(image.Height * k));
			var px = new byte[w * h * 4];
			for (int y = 0; y < h; y++)
			{
				int sy = Math.Min(image.Height - 1, (int)(y / k));
				for (int x = 0; x < w; x++)
					System.Buffer.BlockCopy(image.Bgra, (sy * image.Width + Math.Min(image.Width - 1, (int)(x / k))) * 4, px, (y * w + x) * 4, 4);
			}
			return new OcrImage { Bgra = px, Width = w, Height = h, Dpi = image.Dpi * k };
		}

		// ------------------------------------------------------------------ pictures to read

		/// <summary>The pixels of a decoded picture, at most <paramref name="maxSide"/>, white behind what was see-through.</summary>
		public static async Task<OcrImage> FromDecoder(BitmapDecoder decoder, int maxSide, double dpi)
		{
			uint w = decoder.OrientedPixelWidth, h = decoder.OrientedPixelHeight;
			var transform = new BitmapTransform();
			if (Math.Max(w, h) > maxSide)
			{
				double k = maxSide / (double)Math.Max(w, h);
				// the transform scales before turning (EXIF): sizes of the stored picture
				transform.ScaledWidth = (uint)Math.Max(1, decoder.PixelWidth * k);
				transform.ScaledHeight = (uint)Math.Max(1, decoder.PixelHeight * k);
				transform.InterpolationMode = BitmapInterpolationMode.Fant;
				dpi *= k;
			}
			var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform,
				ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
			byte[] px = data.DetachPixelData();
			for (int i = 0; i < px.Length; i += 4)
			{
				int a = px[i + 3];
				if (a == 255) continue;
				px[i] = (byte)Math.Min(255, px[i] + 255 - a);
				px[i + 1] = (byte)Math.Min(255, px[i + 1] + 255 - a);
				px[i + 2] = (byte)Math.Min(255, px[i + 2] + 255 - a);
				px[i + 3] = 255;
			}
			// the picture's real size after turning comes from the pixels
			int width = (int)(transform.ScaledWidth > 0 ? (IsTurned(decoder) ? transform.ScaledHeight : transform.ScaledWidth) : w);
			int height = px.Length / 4 / Math.Max(1, width);
			return new OcrImage { Bgra = px, Width = width, Height = height, Dpi = dpi };
		}

		static bool IsTurned(BitmapDecoder d) => d.OrientedPixelWidth != d.PixelWidth;

		public static async Task<OcrImage> FromFile(string file, int maxSide)
		{
			using var input = File.OpenRead(file);
			var decoder = await ImageWork.Decoder(input, file);
			return await FromDecoder(decoder, maxSide, 96);
		}

		/// <summary>A page of a PDF drawn by Windows (as it is shown: cut to its CropBox and turned) at about <paramref name="dpi"/>.</summary>
		public static async Task<OcrImage> FromPdfPage(WinPdf.PdfPage page, double dpi, int maxSide, double minWidth = 0)
		{
			double k = dpi / 96;
			double w = page.Size.Width * k, h = page.Size.Height * k;
			// a very small page is drawn bigger, or its letters are too small to read (the "ocr" job's 800 pixels)
			if (w > 0 && w < minWidth) { double s = minWidth / w; w *= s; h *= s; dpi *= s; }
			if (Math.Max(w, h) > maxSide) { double s = maxSide / Math.Max(w, h); w *= s; h *= s; dpi *= s; }
			using var ms = new InMemoryRandomAccessStream();
			await page.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions { DestinationWidth = (uint)Math.Max(1, Math.Round(w)), DestinationHeight = (uint)Math.Max(1, Math.Round(h)) });
			var decoder = await BitmapDecoder.CreateAsync(ms);
			return await FromDecoder(decoder, int.MaxValue, dpi);
		}

		// ------------------------------------------------------------------ plain text (for the "ocr" job)

		/// <summary>The text of a picture with the chosen engine.</summary>
		public static async Task<string> ReadImage(string file, CancellationToken cancel, Action<string> notice, Action<double, string> progress = null)
		{
			using var engine = await Open(progress, cancel, notice);
			return await ReadImage(engine, file, cancel);
		}

		/// <summary>The text of a picture with an engine already open (one engine for all the files of a job).</summary>
		public static async Task<string> ReadImage(IOcrEngine engine, string file, CancellationToken cancel)
		{
			var page = await engine.Read(await FromFile(file, engine.MaxSide), cancel);
			return page.Text;
		}

		/// <summary>The text of every page of a PDF with the chosen engine ("— Page 2 —" between pages, like the OCR of Windows).</summary>
		public static async Task<string> ReadPdf(string file, Action<double, string> progress, CancellationToken cancel, Action<string> notice)
		{
			using var engine = await Open(progress, cancel, notice);
			return await ReadPdf(engine, file, progress, cancel);
		}

		/// <summary>The text of each page of a PDF, page by page (for a Word document made from a scan).</summary>
		public static async Task<List<string>> ReadPdfPages(IOcrEngine engine, string file, Action<double, string> progress, CancellationToken cancel)
		{
			using var input = File.OpenRead(file);
			var pdf = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			var pages = new List<string>();
			for (uint i = 0; i < pdf.PageCount; i++)
			{
				cancel.ThrowIfCancellationRequested();
				progress?.Invoke(i / (double)pdf.PageCount, T.S("doing.page", i + 1, pdf.PageCount));
				using var page = pdf.GetPage(i);
				var read = await engine.Read(await FromPdfPage(page, engine is WindowsOcr ? 200 : 300, engine.MaxSide, 800), cancel);
				pages.Add(read.Text ?? "");
			}
			return pages;
		}
		
		/// <summary>The text of every page of a PDF with an engine already open (one engine for all the files of a job).</summary>
		public static async Task<string> ReadPdf(IOcrEngine engine, string file, Action<double, string> progress, CancellationToken cancel)
		{
			using var input = File.OpenRead(file);
			var pdf = await WinPdf.PdfDocument.LoadFromStreamAsync(input.AsRandomAccessStream());
			var sb = new System.Text.StringBuilder();
			for (uint i = 0; i < pdf.PageCount; i++)
			{
				cancel.ThrowIfCancellationRequested();
				progress?.Invoke(i / (double)pdf.PageCount, T.S("doing.page", i + 1, pdf.PageCount));
				using var page = pdf.GetPage(i);
				var read = await engine.Read(await FromPdfPage(page, engine is WindowsOcr ? 200 : 300, engine.MaxSide, 800), cancel);
				if (pdf.PageCount > 1) sb.AppendLine("— " + T.S("name.page", i + 1) + " —");
				sb.AppendLine(read.Text);
				sb.AppendLine();
			}
			return sb.ToString().TrimEnd();
		}
	}
}
