using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// The tests of the pages of a PDF: smaller, choose pages, numbers, watermark, password. The text written on pages is
	/// read back with PdfPig, and where it landed is checked on the page as Windows draws it (what the reader sees).
	/// </summary>
	internal static class PdfPagesTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("pdfpages plan", Plan);
			DevTests.Add("pdfpages compress", Compress);
			DevTests.Add("pdfpages pages", Pages);
			DevTests.Add("pdfpages numbers", Numbers);
			DevTests.Add("pdfpages watermark", Watermark);
			DevTests.Add("pdfpages password", Password);
			DevTests.Add("pdfpages scripts", Scripts);
		}

		static Dictionary<string, string> Opt(params string[] kv)
		{
			var d = new Dictionary<string, string>();
			for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
			return d;
		}

		// ------------------------------------------------------------------ files made for the tests

		/// <summary>A PDF with real text: one page per word (A4), each word big in the middle.</summary>
		static string TextPdf(string path, params string[] words)
		{
			using var doc = new PdfDocument();
			var font = new XFont("Arial", 36, XFontStyleEx.Bold);
			foreach (var w in words)
			{
				var page = doc.AddPage();
				using var g = XGraphics.FromPdfPage(page);
				g.DrawString(w, font, XBrushes.Black, new XRect(0, 0, page.Width.Point, page.Height.Point), XStringFormats.Center);
			}
			doc.Save(path);
			return path;
		}

		/// <summary>A JPEG that compresses like a photo (colours that change everywhere, with grain).</summary>
		static string Photo(string path, int w, int h, int seed)
		{
			var rnd = new Random(seed);
			using var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
			var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			var row = new byte[data.Stride];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int n = rnd.Next(-40, 40);
					row[x * 3] = (byte)Math.Clamp(x * 255 / w + n, 0, 255);
					row[x * 3 + 1] = (byte)Math.Clamp(y * 255 / h + n, 0, 255);
					row[x * 3 + 2] = (byte)Math.Clamp((x + y) * 128 / (w + h) + 60 + n, 0, 255);
				}
				System.Runtime.InteropServices.Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, row.Length);
			}
			bmp.UnlockBits(data);
			var jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
			using var ps = new EncoderParameters(1);
			ps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
			if (Jobs.Ext(path) == ".png") bmp.Save(path, ImageFormat.Png); else bmp.Save(path, jpeg, ps);
			return path;
		}

		/// <summary>All the text of a PDF (PdfPig), without spaces.</summary>
		static string TextOf(string pdf, int page = -1, string password = null)
		{
			var opts = password == null ? null : new UglyToad.PdfPig.ParsingOptions { Password = password };
			using var d = UglyToad.PdfPig.PdfDocument.Open(pdf, opts);
			var pages = page < 0 ? d.GetPages().ToList() : new List<UglyToad.PdfPig.Content.Page> { d.GetPage(page + 1) };
			return string.Concat(pages.Select(p => p.Text)).Replace(" ", "");
		}

		/// <summary>A page as Windows draws it (what the reader sees), this wide.</summary>
		static async Task<Bitmap> Render(string pdf, int page, int width = 500)
		{
			var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdf));
			var doc = await WinPdf.PdfDocument.LoadFromFileAsync(file);
			using var p = doc.GetPage((uint)page);
			using var ms = new InMemoryRandomAccessStream();
			await p.RenderToStreamAsync(ms, new WinPdf.PdfPageRenderOptions { DestinationWidth = (uint)width });
			using var s = ms.AsStreamForRead();
			using var b = new Bitmap(s);
			return new Bitmap(b);
		}

		/// <summary>Where two drawings of a page differ, as shares of the page (0..1, from the top left); empty when they do not.</summary>
		static RectangleF Changed(Bitmap a, Bitmap b)
		{
			if (a.Size != b.Size) return new RectangleF(0, 0, 1, 1);
			int w = a.Width, h = a.Height, minX = w, minY = h, maxX = -1, maxY = -1;
			var da = a.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var db = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var ra = new byte[da.Stride]; var rb = new byte[db.Stride];
			for (int y = 0; y < h; y++)
			{
				System.Runtime.InteropServices.Marshal.Copy(da.Scan0 + y * da.Stride, ra, 0, ra.Length);
				System.Runtime.InteropServices.Marshal.Copy(db.Scan0 + y * db.Stride, rb, 0, rb.Length);
				for (int x = 0; x < w; x++)
				{
					int d = Math.Abs(ra[x * 4] - rb[x * 4]) + Math.Abs(ra[x * 4 + 1] - rb[x * 4 + 1]) + Math.Abs(ra[x * 4 + 2] - rb[x * 4 + 2]);
					if (d < 60) continue;
					minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
				}
			}
			a.UnlockBits(da); b.UnlockBits(db);
			return maxX < 0 ? RectangleF.Empty : new RectangleF(minX / (float)w, minY / (float)h, (maxX - minX + 1) / (float)w, (maxY - minY + 1) / (float)h);
		}

		static string Show(RectangleF r) => $"x {r.X:0.00} y {r.Y:0.00} w {r.Width:0.00} h {r.Height:0.00}";

		/// <summary>
		/// Pages that are hard to write on the right way: plain A4, turned 90°, turned 270° with a crop box, a media box that
		/// does not start at 0 with a crop box, and turned 180°.
		/// </summary>
		static string TrickyPdf(string path)
		{
			TextPdf(path, "PLAIN", "TURNED", "CROPPED", "SHIFTED", "UPSIDE");
			using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
			{
				doc.Pages[1].Rotate = 90;
				doc.Pages[2].Rotate = 270;
				doc.Pages[2].CropBox = new PdfRectangle(new XPoint(60, 80), new XPoint(520, 760));
				var p3 = doc.Pages[3];
				p3.MediaBox = new PdfRectangle(new XPoint(100, 200), new XPoint(695, 1042));
				p3.CropBox = new PdfRectangle(new XPoint(150, 260), new XPoint(650, 1000));
				doc.Pages[4].Rotate = 180;
				doc.Save(path + ".tmp");
			}
			File.Move(path + ".tmp", path, true);
			return path;
		}

		// ------------------------------------------------------------------ the tests

		static async Task Plan(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "plain.pdf"), "HELLO");
			var ids = Jobs.Plan(new[] { pdf }).Select(j => j.Id).ToList();
			t.Check("plan: a PDF offers smaller early (choosing pages is in See and edit)", ids.IndexOf("compress") is >= 0 and < 5 && !ids.Contains("pages"), string.Join(",", ids));
			t.Check("plan: a PDF offers numbers, watermark, password", ids.Contains("pagenumbers") && ids.Contains("watermark") && ids.Contains("protect") && !ids.Contains("unprotect"), string.Join(",", ids));
			string other = TextPdf(Path.Combine(t.Dir, "other.pdf"), "WORLD");
			ids = Jobs.Plan(new[] { pdf, other }).Select(j => j.Id).ToList();
			t.Check("plan: several PDFs → smaller, not the one-PDF jobs", ids.Contains("compress") && !ids.Contains("pages") && !ids.Contains("protect"), string.Join(",", ids));
			string locked = Path.Combine(t.Dir, "locked.pdf");
			PdfLock.Protect(pdf, locked, "pets");
			ids = Jobs.Plan(new[] { locked }).Select(j => j.Id).ToList();
			t.Check("plan: a locked PDF → remove the password first, nothing that cannot open it", ids.FirstOrDefault() == "unprotect" && !ids.Contains("compress") && !ids.Contains("pages") && !ids.Contains("protect"), string.Join(",", ids));
			string broken = Path.Combine(t.Dir, "broken.pdf");
			File.WriteAllText(broken, "not a pdf");
			ids = Jobs.Plan(new[] { broken }).Select(j => j.Id).ToList();
			t.Check("plan: a damaged PDF gets none of these", !ids.Intersect(new[] { "compress", "pages", "pagenumbers", "watermark", "protect", "unprotect" }).Any(), string.Join(",", ids));
			await Task.CompletedTask;
		}

		static async Task Compress(DevTests.Ctx t)
		{
			// big photos (as a phone or a scanner makes them), one as JPEG and one as PNG (which becomes Flate in the PDF)
			var photos = new[] { Photo(Path.Combine(t.Dir, "p1.jpg"), 4000, 3000, 1), Photo(Path.Combine(t.Dir, "p2.png"), 2400, 3200, 2), Photo(Path.Combine(t.Dir, "p3.jpg"), 3000, 4000, 3) };
			string pdf = Path.Combine(t.Dir, "photos.pdf");
			await PdfWork.FromImages(photos, pdf);
			// a page with real text too, to be sure it survives
			using (var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify))
			{
				using (var g = XGraphics.FromPdfPage(doc.Pages[0], XGraphicsPdfPageOptions.Append))
					g.DrawString("KEEPTHISTEXT", new XFont("Arial", 20), XBrushes.Black, 40, 40);
				doc.Save(pdf + ".tmp");
			}
			File.Move(pdf + ".tmp", pdf, true);
			long before = new FileInfo(pdf).Length;
			// the work file goes in this test's folder, to see that it is removed (the temp folder is shared with others)
			var temps = new List<string>();
			var usual = PdfCompress.TempName;
			PdfCompress.TempName = () => { string f = Path.Combine(t.Dir, "work-" + temps.Count + ".pdf"); temps.Add(f); return f; };
			try { await CompressChecks(t, pdf, before); }
			finally { PdfCompress.TempName = usual; }
			t.Check("compress: no work file left", temps.Count >= 2 && !temps.Any(File.Exists), string.Join(", ", temps.Where(File.Exists)));

			// a big PDF is only looked at in its start and end to know whether it has a password: both answers still right
			string bigLocked = Path.Combine(t.Dir, "photos locked.pdf");
			PdfLock.Protect(pdf, bigLocked, "pets");
			t.Check("plan: a big PDF without password is seen as open", before > 16 << 20 && PdfLock.Of(pdf) == PdfLock.State.Open, before.ToString());
			t.Check("plan: a big PDF with a password is seen as locked", new FileInfo(bigLocked).Length > 16 << 20 && PdfLock.Of(bigLocked) == PdfLock.State.Locked);
		}

		static async Task CompressChecks(DevTests.Ctx t, string pdf, long before)
		{
			var r = await Jobs.Run("compress", new[] { pdf }, null);
			bool ok = r.ok && r.outputs.Count == 1;
			long after = ok ? new FileInfo(r.outputs[0]).Length : 0;
			t.Info($"compress: {PdfCompress.Size(before)} → {PdfCompress.Size(after)}  ({r.preview})");
			t.Check("compress: photos → a much smaller PDF", ok && after < before / 3, r.message);
			t.Check("compress: same number of pages", ok && PdfWork.Pages(r.outputs[0]) == 3);
			t.Check("compress: the text is kept", ok && TextOf(r.outputs[0]).Contains("KEEPTHISTEXT"));
			t.Check("compress: name says smaller", ok && Path.GetFileName(r.outputs[0]) == "photos (" + T.S("pdfpages.name.smaller") + ").pdf", ok ? r.outputs[0] : "");
			t.Check("compress: the bubble says how much", r.preview.Contains("→") && r.preview.Contains(T.S("pdfpages.size.mb", "").Trim()), r.preview);
			t.Check("compress: the original is not changed", new FileInfo(pdf).Length == before);
			if (ok)
			{
				// Windows still draws every page (the new pictures are good JPEGs)
				bool drawn = true;
				for (int i = 0; i < 3; i++) using (var b = await Render(r.outputs[0], i, 200)) drawn &= b.Width == 200;
				t.Check("compress: every page still opens and is drawn", drawn);
			}

			// a PDF of text only cannot get smaller: no useless copy, and the user is told
			string text = TextPdf(Path.Combine(t.Dir, "text.pdf"), "ONLY", "TEXT");
			r = await Jobs.Run("compress", new[] { text }, null);
			string word = T.S("pdfpages.name.smaller");
			if (r.ok) t.Check("compress: a text PDF, when smaller, is smaller", new FileInfo(r.outputs[0]).Length < new FileInfo(text).Length);
			else t.Check("compress: a PDF that cannot get smaller says so and leaves no copy", r.message.Contains(T.S("pdfpages.compress.notSmaller")) && !Directory.GetFiles(t.Dir, "text (" + word + ")*.pdf").Any(), r.message);
		}

		static async Task Pages(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "seven.pdf"), "AAAONE", "BBBTWO", "CCCTHREE", "DDDFOUR", "EEEFIVE", "FFFSIX", "GGGSEVEN");
			string[] words = { "AAAONE", "BBBTWO", "CCCTHREE", "DDDFOUR", "EEEFIVE", "FFFSIX", "GGGSEVEN" };
			string Order(string file) => string.Join(",", Enumerable.Range(0, PdfWork.Pages(file)).Select(i => { string s = TextOf(file, i); return (Array.FindIndex(words, w => s.Contains(w)) + 1).ToString(); }));

			var r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "3,1,2"));
			t.Check("pages: order 3,1,2", r.ok && Order(r.outputs[0]) == "3,1,2", r.ok ? Order(r.outputs[0]) : r.message);
			t.Check("pages: the name says pages", r.ok && Path.GetFileName(r.outputs[0]) == "seven (" + T.S("name.pages") + ").pdf", r.ok ? r.outputs[0] : "");
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "1-3, 7"));
			t.Check("pages: ranges 1-3, 7", r.ok && Order(r.outputs[0]) == "1,2,3,7", r.ok ? Order(r.outputs[0]) : r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "7 - 5"));
			t.Check("pages: backwards 7 - 5", r.ok && Order(r.outputs[0]) == "7,6,5", r.ok ? Order(r.outputs[0]) : r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("delete", "2,4-6"));
			t.Check("pages: delete 2 and 4-6", r.ok && Order(r.outputs[0]) == "1,3,7", r.ok ? Order(r.outputs[0]) : r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "2,1", "rotate", "2:90,1:-90"));
			bool turned = false;
			if (r.ok) using (var d = PdfReader.Open(r.outputs[0], PdfDocumentOpenMode.Import)) turned = d.Pages[0].Rotate == 90 && d.Pages[1].Rotate == 270;
			t.Check("pages: single pages turned", turned && Order(r.outputs[0]) == "2,1", r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "1,1"));
			t.Check("pages: a page twice", r.ok && Order(r.outputs[0]) == "1,1", r.ok ? Order(r.outputs[0]) : r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "9"));
			t.Check("pages: a page that is not there says so", !r.ok && r.message.Contains(T.S("pdfpages.err.pageOut", 9, 7)), r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("order", "two"));
			t.Check("pages: words instead of numbers say so", !r.ok && r.message.Contains(T.S("pdfpages.err.badPages", "two")), r.message);
			r = await Jobs.Run("pages", new[] { pdf }, null, default, Opt("delete", "1-7"));
			t.Check("pages: deleting every page says so", !r.ok && r.message.Contains(T.S("pdfpages.err.noPagesLeft")), r.message);
			t.Check("pages: the window's answer is short", PdfPick.Text(new[] { 1, 2, 3, 4, 9, 7, 6 }) == "1-4,9,7,6", PdfPick.Text(new[] { 1, 2, 3, 4, 9, 7, 6 }));
		}

		/// <summary>The text drawn over each tricky page landed where the reader sees it, upright.</summary>
		static async Task<List<RectangleF>> Landed(string before, string after, int pages)
		{
			var list = new List<RectangleF>();
			for (int i = 0; i < pages; i++)
			{
				using var a = await Render(before, i);
				using var b = await Render(after, i);
				list.Add(Changed(a, b));
			}
			return list;
		}

		static async Task Numbers(DevTests.Ctx t)
		{
			string pdf = TrickyPdf(Path.Combine(t.Dir, "tricky.pdf"));
			var r = await Jobs.Run("pagenumbers", new[] { pdf }, null, default, Opt("position", "bottom-right", "format", "full", "first", "1", "size", "14"));
			t.Check("numbers: done", r.ok, r.message);
			if (!r.ok) return;
			string all = TextOf(r.outputs[0]);
			t.Check("numbers: \"Page 1 of 5\" … \"Page 5 of 5\" written", Enumerable.Range(1, 5).All(n => all.Contains(T.S("pdfpages.num.full", n, 5).Replace(" ", ""))), all.Length > 200 ? all[..200] : all);
			t.Check("numbers: the pages' own text is kept", all.Contains("PLAIN") && all.Contains("UPSIDE"));
			var spots = await Landed(pdf, r.outputs[0], 5);
			for (int i = 0; i < spots.Count; i++)
			{
				var s = spots[i];
				t.Check("numbers: bottom right and upright as seen, page " + (i + 1), s.Width > 0 && s.Left > 0.55 && s.Top > 0.85 && s.Right < 1 && s.Width > s.Height * 2, Show(s));
			}
			r = await Jobs.Run("pagenumbers", new[] { pdf }, null, default, Opt("position", "top-right", "format", "number", "first", "7"));
			all = r.ok ? TextOf(r.outputs[0], 0) : "";
			spots = r.ok ? await Landed(pdf, r.outputs[0], 5) : new();
			t.Check("numbers: \"7\" on the first page, top right", r.ok && all.Contains("7") && !all.Contains(T.S("pdfpages.num.full", 7, 11).Replace(" ", "")) && spots.All(s => s.Width > 0 && s.Left > 0.6 && s.Bottom < 0.15), string.Join(" | ", spots.Select(Show)) + r.message);
			r = await Jobs.Run("pagenumbers", new[] { pdf }, null, default, Opt("position", "bottom-center"));
			spots = r.ok ? await Landed(pdf, r.outputs[0], 5) : new();
			t.Check("numbers: bottom centre", r.ok && spots.All(s => s.Width > 0 && Math.Abs(s.Left + s.Width / 2 - 0.5) < 0.05 && s.Top > 0.85), string.Join(" | ", spots.Select(Show)) + r.message);
		}

		static async Task Watermark(DevTests.Ctx t)
		{
			string pdf = TrickyPdf(Path.Combine(t.Dir, "tricky.pdf"));
			var r = await Jobs.Run("watermark", new[] { pdf }, null, default, Opt("text", "CONFIDENTIAL", "layout", "horizontal", "opacity", "60", "color", "red"));
			t.Check("watermark: done", r.ok, r.message);
			if (!r.ok) return;
			string all = TextOf(r.outputs[0]);
			t.Check("watermark: the text is on every page", Enumerable.Range(0, 5).All(i => TextOf(r.outputs[0], i).Contains("CONFIDENTIAL")), all.Length > 200 ? all[..200] : all);
			t.Check("watermark: the name says watermark", Path.GetFileName(r.outputs[0]) == "tricky (" + T.S("pdfpages.name.watermark") + ").pdf", r.outputs[0]);
			var spots = await Landed(pdf, r.outputs[0], 5);
			for (int i = 0; i < spots.Count; i++)
			{
				var s = spots[i];
				t.Check("watermark: straight across the middle as seen, page " + (i + 1), s.Width > 0.5 && s.Width > s.Height * 3 && Math.Abs(s.Top + s.Height / 2 - 0.5) < 0.08 && Math.Abs(s.Left + s.Width / 2 - 0.5) < 0.08, Show(s));
			}
			r = await Jobs.Run("watermark", new[] { pdf }, null, default, Opt("text", "COPY", "layout", "diagonal", "opacity", "30"));
			spots = r.ok ? await Landed(pdf, r.outputs[0], 5) : new();
			t.Check("watermark: diagonal, in the middle, across most of the page", r.ok && spots.All(s => s.Width > 0.4 && s.Height > 0.4 && Math.Abs(s.Top + s.Height / 2 - 0.5) < 0.08), string.Join(" | ", spots.Select(Show)) + r.message);
			r = await Jobs.Run("watermark", new[] { pdf }, null, default, Opt("text", "  "));
			t.Check("watermark: no text says so", !r.ok && r.message.Contains(T.S("pdfpages.err.noText")), r.message);
		}

		static async Task Password(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "secret.pdf"), "SECRETONE", "SECRETTWO");
			var r = await Jobs.Run("protect", new[] { pdf }, null, default, Opt("password", "Pato123"));
			t.Check("protect: done", r.ok, r.message);
			if (!r.ok) return;
			string locked = r.outputs[0];
			t.Check("protect: the name says protected", Path.GetFileName(locked) == "secret (" + T.S("pdfpages.name.protected") + ").pdf", locked);
			t.Check("protect: it is locked now", PdfLock.Of(locked) == PdfLock.State.Locked);
			bool opened = true;
			try { using var d = PdfReader.Open(locked, PdfDocumentOpenMode.Import); opened = d != null; } catch (Exception) { opened = false; }
			t.Check("protect: cannot be opened without the password", !opened);
			int pages = 0;
			try { using var d = PdfReader.Open(locked, "Pato123", PdfDocumentOpenMode.Import); pages = d.PageCount; } catch (Exception ex) { t.Info("protect: " + ex.Message); }
			t.Check("protect: opens with the password, every page there", pages == 2);
			string text = "";
			try { text = TextOf(locked, -1, "Pato123"); } catch (Exception ex) { text = ex.Message; }
			t.Check("protect: another reader opens it with the password too", text.Contains("SECRETONE"), text.Length > 100 ? text[..100] : text);
			bool pigWithout = true;
			try { TextOf(locked); } catch (Exception) { pigWithout = false; }
			t.Check("protect: another reader cannot open it without the password", !pigWithout);
			r = await Jobs.Run("protect", new[] { pdf }, null, default, Opt("password", ""));
			t.Check("protect: an empty password says so", !r.ok && r.message.Contains(T.S("pdfpages.err.noPassword")), r.message);

			// unprotect: wrong, then right
			r = await Jobs.Run("unprotect", new[] { locked }, null, default, Opt("password", "wrong"));
			t.Check("unprotect: a wrong password says so kindly", !r.ok && r.message.Contains(T.S("pdfpages.err.wrongPassword")), r.message);
			t.Check("unprotect: a wrong password leaves no file", !Directory.GetFiles(t.Dir, "*" + T.S("pdfpages.name.unlocked") + "*").Any());
			r = await Jobs.Run("unprotect", new[] { locked }, null, default, Opt("password", "Pato123"));
			t.Check("unprotect: the right password gives an open copy", r.ok && PdfLock.Of(r.outputs[0]) == PdfLock.State.Open && PdfWork.Pages(r.outputs[0]) == 2 && TextOf(r.outputs[0]).Contains("SECRETTWO"), r.message);
			t.Check("unprotect: the locked one stays locked", PdfLock.Of(locked) == PdfLock.State.Locked);

			// a PDF whose maker kept another password for changes: that password only lets it be read
			string twoKeys = Path.Combine(t.Dir, "two keys.pdf");
			using (var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify))
			{
				doc.SecuritySettings.UserPassword = "reader";
				doc.SecuritySettings.OwnerPassword = "maker";
				doc.SecurityHandler.SetEncryptionToV5();
				doc.Save(twoKeys);
			}
			r = await Jobs.Run("unprotect", new[] { twoKeys }, null, default, Opt("password", "reader"));
			t.Check("unprotect: a password that only reads says so", !r.ok && r.message.Contains(T.S("pdfpages.err.readOnlyPassword")), r.message);
			r = await Jobs.Run("unprotect", new[] { twoKeys }, null, default, Opt("password", "maker"));
			t.Check("unprotect: the maker's password unlocks it", r.ok && PdfLock.Of(r.outputs[0]) == PdfLock.State.Open, r.message);
		}

		/// <summary>
		/// Numbers and watermarks in languages whose letters Arial does not have, or that join and go right to left:
		/// Windows draws them, in a font that has every letter, and they land where they should.
		/// </summary>
		static async Task Scripts(DevTests.Ctx t)
		{
			t.Check("scripts: Latin text stays real text", StampText.Plain("Página 3 de 12 CONFIDENCIAL") && StampText.Plain("Страница 1"));
			foreach (var code in new[] { "ja", "zh-CN", "ko", "hi", "bn", "mr", "ta", "te", "th", "ar", "fa", "ur" })
			{
				var texts = T.Read(code);
				foreach (var s in new[] { string.Format(texts["pdfpages.num.full"], 1, 5), texts["pdfpages.wm.default"] })
				{
					string font = StampText.FontFor(s);
					t.Check("scripts: " + code + " \"" + s + "\" is drawn by Windows in a font with every letter", !StampText.Plain(s) && StampText.Missing(font, s) == 0, font);
				}
			}

			string old = T.Code;
			try
			{
				foreach (var code in new[] { "ja", "hi", "ar" })
				{
					T.Use(code);
					string pdf = TextPdf(Path.Combine(t.Dir, code + ".pdf"), "PLAIN");
					var r = await Jobs.Run("pagenumbers", new[] { pdf }, null, default, Opt("position", "bottom-right", "size", "14"));
					var spot = r.ok ? (await Landed(pdf, r.outputs[0], 1))[0] : RectangleF.Empty;
					t.Check("scripts: " + code + " page numbers at the bottom right", r.ok && Pictures(r.outputs[0]) == 1 && spot.Width > 0.05 && spot.Left > 0.55 && spot.Top > 0.85 && spot.Right < 1, Show(spot) + r.message);
					r = await Jobs.Run("watermark", new[] { pdf }, null, default, Opt("text", T.S("pdfpages.wm.default"), "layout", "horizontal", "opacity", "50", "color", "red"));
					spot = r.ok ? (await Landed(pdf, r.outputs[0], 1))[0] : RectangleF.Empty;
					t.Check("scripts: " + code + " watermark in the middle", r.ok && Pictures(r.outputs[0]) == 1 && spot.Width > 0.2 && Math.Abs(spot.Left + spot.Width / 2 - 0.5) < 0.08 && Math.Abs(spot.Top + spot.Height / 2 - 0.5) < 0.08, Show(spot) + r.message);
				}
			}
			finally { T.Use(old); }

			// a page smaller than the letters (a label): a smaller watermark, not an error
			string tiny = Path.Combine(t.Dir, "tiny.pdf");
			using (var doc = new PdfDocument())
			{
				var page = doc.AddPage();
				page.Width = XUnit.FromPoint(20);
				page.Height = XUnit.FromPoint(20);
				doc.Save(tiny);
			}
			var w = await Jobs.Run("watermark", new[] { tiny }, null, default, Opt("text", "CONFIDENTIAL"));
			t.Check("scripts: a tiny page gets a watermark too", w.ok, w.message);
		}

		/// <summary>How many pictures the first page of a PDF draws.</summary>
		static int Pictures(string pdf)
		{
			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			var xo = doc.Pages[0].Resources.Elements.GetDictionary("/XObject");
			if (xo == null) return 0;
			return xo.Elements.Values.Select(v => v is PdfSharp.Pdf.Advanced.PdfReference r ? r.Value : v).OfType<PdfDictionary>().Count(d => d.Elements.GetName("/Subtype") == "/Image");
		}
	}
}
