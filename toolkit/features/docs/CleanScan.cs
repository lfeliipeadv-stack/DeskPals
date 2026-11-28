using System.Runtime.CompilerServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "cleanscan": photos of paper documents → one PDF that looks scanned (the sheet cut out and straightened, white
	/// paper, dark ink), one A4 page per photo in the order given. Colour, grey or black and white (the last choice is
	/// remembered). The work is in <see cref="ScanImage"/>.
	/// </summary>
	internal static class CleanScan
	{
		/// <summary>A4 in points.</summary>
		const double A4W = 595.28, A4H = 841.89;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "cleanscan", Emoji = "🧾",
				// right after "Convert to PDF" (10) for pictures
				Rank = f => f.Count > 0 && f.All(x => File.Exists(x) && IsImage(x)) ? 11 : -1,
				Ask = AskMode,
				Run = Run,
			});
		}

		static Dictionary<string, string> AskMode(IWin32Window owner, IList<string> files)
		{
			var a = Ask.Show(owner, T.S("job.cleanscan"), T.S("docs.scan.intro"),
				new Ask.Field
				{
					Key = "mode", Label = T.S("docs.scan.mode"), Kind = Ask.Kind.Choice, Value = ToolSettings.Text("docs.scanMode", "color"),
					Choices = new[] { ("color", T.S("docs.scan.color")), ("gray", T.S("docs.scan.gray")), ("bw", T.S("docs.scan.bw")) },
				});
			if (a != null) try { ToolSettings.Set("docs.scanMode", a["mode"]); } catch (Exception) { }
			return a;
		}

		internal static ScanImage.Mode ModeOf(string s) => s switch { "gray" => ScanImage.Mode.Gray, "bw" => ScanImage.Mode.BlackWhite, _ => ScanImage.Mode.Color };

		static async Task Run(JobContext c)
		{
			var mode = ModeOf(c.Option("mode", "color"));
			var pages = new List<string>();
			try
			{
				await c.Each(async (file, i) =>
				{
					c.Report(i / (double)c.Files.Count, c.Files.Count > 1 ? T.S("doing.page", i + 1, c.Files.Count) : Path.GetFileName(file));
					pages.Add(await CleanOne(file, mode, c.Cancel));
				});
				c.Cancel.ThrowIfCancellationRequested();
				if (pages.Count == 0) return;   // every photo failed: their errors say why
				c.Report(0.97, T.S("doing.joining"));
				string o = OutName(c.Files[0], ".pdf", T.S("docs.name.scan"));
				string tmp = Temp(".pdf");
				try
				{
					WritePdf(pages, tmp);
					File.Move(tmp, o, false);
				}
				finally { try { File.Delete(tmp); } catch (Exception) { } }
				c.Outputs.Add(o);
			}
			finally { foreach (var p in pages) try { File.Delete(p); } catch (Exception) { } }
		}

		/// <summary>One photo cleaned, as a temporary picture (JPEG, or PNG in black and white).</summary>
		internal static async Task<string> CleanOne(string file, ScanImage.Mode mode, CancellationToken cancel)
		{
			Pixels photo;
			try { photo = await ScanImage.Load(file); }
			catch (IOException ex) when (DocsFiles.InUse(ex)) { throw new InvalidOperationException(T.S("docs.inUse")); }
			catch (Exception ex) when (ex is not (FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException or IOException))
			{
				throw new InvalidOperationException(HeicHelp.IsHeic(file) && !HeicHelp.CanDecode() ? T.S("docs.heic.missing") : T.S("docs.scan.noPicture"));
			}
			cancel.ThrowIfCancellationRequested();
			var page = await Task.Run(() => ScanImage.Clean(photo, mode, cancel), cancel);
			photo = null;
			cancel.ThrowIfCancellationRequested();
			if (await Task.Run(() => ScanImage.Sideways(page), cancel)) page = await Upright(page, true);
			// a sheet photographed upside down: the text recognition reads it much better turned round
			cancel.ThrowIfCancellationRequested();
			if (await UpsideDown(page)) page = await Task.Run(() => ScanImage.Turn(ScanImage.Turn(page, true), true), cancel);
			string out_ = Temp(mode == ScanImage.Mode.BlackWhite ? ".png" : ".jpg");
			await ScanImage.Save(page, out_, mode);
			return out_;
		}

		/// <summary>
		/// A page lying on its side, turned upright: the quarter turn (clockwise or not) after which Windows' text
		/// recognition reads more words; without it (or when it reads nothing either way), the turn after which the lines
		/// start at a common left margin.
		/// </summary>
		internal static async Task<Pixels> Upright(Pixels page, bool useOcr)
		{
			var look = ScanImage.Smaller(page, 2000);
			Pixels cw = ScanImage.Turn(look, true), ccw = ScanImage.Turn(look, false);
			int a = 0, b = 0;
			if (useOcr) { a = await Words(cw); b = await Words(ccw); }
			bool clockwise = a != b ? a > b : ScanImage.LeftAligned(cw) >= ScanImage.LeftAligned(ccw);
			return await Task.Run(() => ScanImage.Turn(page, clockwise));
		}

		/// <summary>
		/// Is the page upside down? Only when the text recognition reads clearly more words turned round (and enough of them
		/// to be sure); without text recognition, or with few words, the page is left as it is.
		/// </summary>
		internal static async Task<bool> UpsideDown(Pixels page)
		{
			var look = ScanImage.Smaller(page, 1600);
			int now = await Words(look);
			int turned = await Words(ScanImage.Turn(ScanImage.Turn(look, true), true));
			return turned >= 40 && turned > now * 1.4;
		}
		
		/// <summary>How many letters Windows' text recognition reads as real words (3 letters or more); 0 without OCR.</summary>
		internal static async Task<int> Words(Pixels p)
		{
			try
			{
				var engine = Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
				if (engine == null)
					foreach (var lang in Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages)
						if ((engine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(lang)) != null) break;
				if (engine == null) return 0;
				using var bitmap = Windows.Graphics.Imaging.SoftwareBitmap.CreateCopyFromBuffer(
					System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(p.Data),
					Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, p.W, p.H, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
				var result = await engine.RecognizeAsync(bitmap);
				return result.Lines.SelectMany(l => l.Words).Select(w => w.Text).Where(w => w.Length >= 3 && w.All(char.IsLetter)).Sum(w => w.Length);
			}
			catch (Exception) { return 0; }
		}

		/// <summary>
		/// One A4 page per picture. A page shaped about like A4 fills the whole sheet (as a scanner gives it); any other
		/// shape is fitted inside, centred on white. Wide pictures get a lying page.
		/// </summary>
		internal static void WritePdf(IEnumerable<string> pictures, string pdf)
		{
			using var doc = new PdfDocument();
			doc.Info.Creator = "DeskPals Tools";
			foreach (var file in pictures)
			{
				using var img = XImage.FromFile(file);
				bool wide = img.PixelWidth > img.PixelHeight;
				var page = doc.AddPage();
				double pw = wide ? A4H : A4W, ph = wide ? A4W : A4H;
				page.Width = XUnit.FromPoint(pw);
				page.Height = XUnit.FromPoint(ph);
				double ratio = img.PixelWidth / (double)img.PixelHeight, a4 = pw / ph;
				using var g = XGraphics.FromPdfPage(page);
				if (Math.Abs(ratio / a4 - 1) < 0.12) g.DrawImage(img, 0, 0, pw, ph);
				else
				{
					double k = Math.Min(pw / img.PixelWidth, ph / img.PixelHeight);
					double w = img.PixelWidth * k, h = img.PixelHeight * k;
					g.DrawImage(img, (pw - w) / 2, (ph - h) / 2, w, h);
				}
			}
			if (doc.PageCount == 0) throw new InvalidOperationException(T.S("err.noPages"));
			doc.Save(pdf);
		}
	}
}
