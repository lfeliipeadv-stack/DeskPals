using System.Runtime.CompilerServices;
using Windows.Graphics.Imaging;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "See and edit": one PDF, or pictures, in a window where the pages are seen big, turned, deleted and put in order,
	/// with page numbers, a watermark or a cleaned document seen before anything is written. Nothing is saved until
	/// Save: then one file is made ("Save a copy"), or the original is replaced (after a question). The window is the
	/// job's question (<see cref="ViewerForm"/>); the work here follows its answers:
	/// "kind" pdf|images, "order" (pages or pictures, from 1, in their new order), "rotate" ("2:90,3:270"),
	/// "save" copy|replace; for a PDF "numbers"=1 (+ position, format, first, size) and "watermark"=1 (+ text, layout,
	/// opacity, color); for pictures "clean" none|color|gray|bw and "as" pdf|images.
	/// </summary>
	internal static class ViewerJobs
	{
		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "view", Emoji = "👁️",
				// first for one PDF (it replaces "turn" and "choose pages"); for pictures right after "to PDF" and "clean scan"
				// several PDFs (and pictures): their pages together, to make one PDF (after "join", which does it at once)
				Rank = f => OnePdf(f) ? 9 : f.Count > 0 && f.All(x => File.Exists(x) && IsImage(x)) ? 12 : Several(f) ? 11 : -1,
				Ask = (owner, files) => ViewerForm.Open(owner, files),
				Run = Save,
			});
		}

		static bool OnePdf(IList<string> f) => f.Count == 1 && File.Exists(f[0]) && IsPdf(f[0]) && PdfLock.Of(f[0]) == PdfLock.State.Open;
		
		/// <summary>Several files, PDFs (that open without a password) and pictures, at least one PDF.</summary>
		static bool Several(IList<string> f) => f.Count > 1 && f.Any(IsPdf)
			&& f.All(x => File.Exists(x) && (IsImage(x) || (IsPdf(x) && PdfLock.Of(x) == PdfLock.State.Open)));
		
		/// <summary>
		/// The pages of several files in one temporary PDF (a picture is one page), and where each page came from (the
		/// file's place in the list, from 0, and its page there, from 1). The caller deletes the PDF.
		/// </summary>
		internal static async Task<(string pdf, List<(int file, int page)> from)> Combine(IList<string> files, CancellationToken cancel = default)
		{
			var parts = new List<string>();
			var temps = new List<string>();
			var from = new List<(int, int)>();
			try
			{
				for (int i = 0; i < files.Count; i++)
				{
					cancel.ThrowIfCancellationRequested();
					string part = files[i];
					if (IsImage(part))
					{
						string tmp = Temp(".pdf");
						temps.Add(tmp);
						await PdfWork.FromImages(new[] { part }, tmp);
						part = tmp;
					}
					int n = PdfWork.Pages(part);
					for (int p = 1; p <= n; p++) from.Add((i, p));
					parts.Add(part);
				}
				string pdf = Temp(".pdf");
				PdfWork.Merge(parts, pdf);
				return (pdf, from);
			}
			finally { foreach (var t in temps) try { File.Delete(t); } catch (Exception) { } }
		}

		static async Task Save(JobContext c)
		{
			bool replace = c.Option("save", "copy") == "replace";
			if (c.Option("kind", c.Files.Any(IsPdf) ? "pdf" : "images") == "pdf") await SavePdf(c, replace && c.Files.Count == 1);
			else await SavePictures(c, replace);
		}

		// ------------------------------------------------------------------ a PDF

		/// <summary>The pages in their order and turns, then the numbers and the watermark, into a copy or over the original.</summary>
		static async Task SavePdf(JobContext c, bool replace)
		{
			string file = c.Files[0];
			c.Report(0.05, Path.GetFileName(file));
			// several files: their pages together first, then the same work as for one
			string combined = c.Files.Count > 1 ? (await Combine(c.Files, c.Cancel)).pdf : null;
			string made;
			try
			{
				made = await Task.Run(() => BuildPdf(combined ?? file, c.Options, null, c.Cancel), c.Cancel);
				// black boxes: those pages drawn again as pictures, so what is under a box is gone
				made = await Marks.Burn(made, Marks.Read(c.Option("marks")), c.Cancel);
			}
			finally { if (combined != null) try { File.Delete(combined); } catch (Exception) { } }
			try
			{
				string target = replace ? file : OutName(file, ".pdf", T.S("viewer.name.edited"));
				Put(made, target, replace);
				c.Outputs.Add(target);
			}
			finally { try { File.Delete(made); } catch (Exception) { } }
		}

		/// <summary>
		/// A temporary PDF with these answers. <paramref name="onlyAt"/>: only that place of the new order (0-based), for
		/// the viewer's big page; its number is still the one it will have.
		/// </summary>
		internal static string BuildPdf(string file, Dictionary<string, string> options, int? onlyAt, CancellationToken cancel)
		{
			var o = new Dictionary<string, string>(options);
			int total = 0;
			if (onlyAt != null)
			{
				int count = PdfWork.Pages(file);
				var order = PdfPick.Order(o, count);
				total = order.Count;
				o["order"] = order[Math.Clamp(onlyAt.Value, 0, order.Count - 1)].ToString();
				o.Remove("delete");
			}
			string step = Temp(".pdf");
			PdfPick.Save(file, step, o);
			var ctx = new JobContext { Files = new[] { file }, Options = o, Cancel = cancel };
			var draws = new List<PdfStamp.Draw>();
			if (o.GetValueOrDefault("numbers") == "1") draws.Add(PdfPagesJobs.Numbers(ctx));
			string text = (o.GetValueOrDefault("text") ?? "").Trim();
			if (o.GetValueOrDefault("watermark") == "1" && text.Length > 0) draws.Add(PdfPagesJobs.Watermark(ctx, text));
			// the marks (highlights, texts, signatures, black boxes) on their pages, last, over everything
			var marks = Marks.Read(o.GetValueOrDefault("marks"));
			if (marks.Count > 0)
			{
				var writer = new StampText();
				draws.Add((g, w, h, i, n) => Marks.Draw(g, w, h, marks.Where(m => m.at == i), writer));
			}
			foreach (var d in draws)
			{
				cancel.ThrowIfCancellationRequested();
				string next = Temp(".pdf");
				var draw = d;
				// the one page shown is numbered as in the whole document
				if (onlyAt != null) { int at = onlyAt.Value, all = total; draw = (g, w, h, i, n) => d(g, w, h, at, all); }
				try { PdfStamp.Apply(step, next, draw, null, cancel); }
				finally { try { File.Delete(step); } catch (Exception) { } }
				step = next;
			}
			return step;
		}

		/// <summary>The finished file to its place: a new copy, or over the original (which is kept until the new one is whole).</summary>
		static void Put(string made, string target, bool replace)
		{
			if (!replace) { File.Copy(made, target, false); return; }
			string backup = target + ".deskpals-old";
			try
			{
				File.Copy(target, backup, true);
				File.Copy(made, target, true);
			}
			catch (IOException ex) when ((ex.HResult & 0xFFFF) is 32 or 33) { throw new InvalidOperationException(T.S("viewer.err.inUse")); }
			finally { try { File.Delete(backup); } catch (Exception) { } }
		}

		// ------------------------------------------------------------------ pictures

		static async Task SavePictures(JobContext c, bool replace)
		{
			var order = Order(c.Option("order"), c.Files.Count);
			var turns = PdfPick.ParseTurns(c.Option("rotate"), c.Files.Count);
			string clean = c.Option("clean", "none");
			bool asPdf = c.Option("as", "images") == "pdf";
			var temps = new List<string>();
			try
			{
				for (int k = 0; k < order.Count; k++)
				{
					c.Cancel.ThrowIfCancellationRequested();
					int n = order[k];
					string file = c.Files[n - 1];
					int turn = turns.GetValueOrDefault(n);
					c.Report(k / (double)order.Count, order.Count > 1 ? T.S("doing.page", k + 1, order.Count) : Path.GetFileName(file));
					// a picture saved as a picture without any change is left alone
					if (!asPdf && turn == 0 && clean == "none") continue;
					try
					{
						var px = await Picture(file, clean, turn, c.Cancel);
						bool png = clean == "bw" || (!asPdf && Ext(file) == ".png");
						string tmp = Temp(png ? ".png" : ".jpg");
						await Encode(px, tmp, png);
						temps.Add(tmp);
						if (asPdf) continue;
						bool sameKind = Ext(file) is ".jpg" or ".jpeg" ? !png : png && Ext(file) == ".png";
						string target = replace && sameKind ? file : OutName(file, png ? ".png" : ".jpg", T.S("viewer.name.edited"));
						Put(tmp, target, replace && sameKind);
						c.Outputs.Add(target);
					}
					catch (OperationCanceledException) { throw; }
					catch (Exception ex) { c.Fail(file, ex); }
				}
				if (!asPdf)
				{
					if (c.Outputs.Count == 0 && c.Errors.Count == 0) throw new InvalidOperationException(T.S("viewer.err.nothing"));
					return;
				}
				if (temps.Count == 0) return;
				c.Report(0.97, T.S("doing.joining"));
				string pdf = OutName(c.Files[order[0] - 1], ".pdf", T.S("viewer.name.edited"));
				string made = Temp(".pdf");
				try
				{
					if (clean != "none") CleanScan.WritePdf(temps, made);
					else await PdfWork.FromImages(temps, made);
					File.Move(made, pdf, false);
				}
				finally { try { File.Delete(made); } catch (Exception) { } }
				c.Outputs.Add(pdf);
			}
			finally { foreach (var t in temps) try { File.Delete(t); } catch (Exception) { } }
		}

		static List<int> Order(string text, int count)
		{
			var list = string.IsNullOrWhiteSpace(text) ? Enumerable.Range(1, count).ToList() : PdfPick.Parse(text, count);
			if (list.Count == 0) throw new InvalidOperationException(T.S("pdfpages.err.noPagesLeft"));
			return list;
		}

		/// <summary>A picture as it will be saved: cleaned as a document (when asked), then turned as the user turned it.</summary>
		internal static async Task<Pixels> Picture(string file, string clean, int turn, CancellationToken cancel)
		{
			Pixels px;
			if (clean is "color" or "gray" or "bw")
			{
				string tmp = await CleanScan.CleanOne(file, CleanScan.ModeOf(clean), cancel);
				try { px = await ScanImage.Load(tmp); }
				finally { try { File.Delete(tmp); } catch (Exception) { } }
			}
			else
			{
				try { px = await ScanImage.Load(file); }
				catch (Exception ex) when (ex is not (IOException or UnauthorizedAccessException))
				{
					throw new InvalidOperationException(HeicHelp.Explain(file) ?? T.S("docs.scan.noPicture"));
				}
			}
			return Turn(px, turn);
		}

		internal static Pixels Turn(Pixels px, int degrees)
		{
			switch (((degrees % 360) + 360) % 360)
			{
				case 90: return ScanImage.Turn(px, true);
				case 180: return ScanImage.Turn(ScanImage.Turn(px, true), true);
				case 270: return ScanImage.Turn(px, false);
				default: return px;
			}
		}

		/// <summary>JPEG at a good quality (a photo that is only turned should not get worse) or PNG.</summary>
		internal static async Task Encode(Pixels p, string file, bool png)
		{
			using var output = File.Create(file);
			var props = new BitmapPropertySet();
			if (!png) props.Add("ImageQuality", new BitmapTypedValue(0.92, Windows.Foundation.PropertyType.Single));
			var encoder = await BitmapEncoder.CreateAsync(png ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId, output.AsRandomAccessStream(), props);
			encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)p.W, (uint)p.H, 96, 96, p.Data);
			await encoder.FlushAsync();
		}

		/// <summary>Pixels (BGRA) as a picture for the screen.</summary>
		internal static System.Drawing.Bitmap ToBitmap(Pixels p)
		{
			var bmp = new System.Drawing.Bitmap(p.W, p.H, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, p.W, p.H), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
			try
			{
				for (int y = 0; y < p.H; y++)
					System.Runtime.InteropServices.Marshal.Copy(p.Data, y * p.W * 4, data.Scan0 + y * data.Stride, p.W * 4);
			}
			finally { bmp.UnlockBits(data); }
			return bmp;
		}
	}
}
