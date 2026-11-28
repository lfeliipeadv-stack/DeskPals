using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// Text drawn over every page of a PDF (page numbers, a watermark), upright as the reader sees the page: the visible
	/// part (the crop box) and a page turned by its /Rotate are taken into account, so "bottom right" is the bottom right
	/// on the screen. What was on the page stays as it was; the text goes on top.
	/// </summary>
	internal static class PdfStamp
	{
		/// <summary>Draws on a page: the graphics (points, top left = the top left the reader sees), its width and height, the page index and the count.</summary>
		public delegate void Draw(XGraphics g, double width, double height, int index, int count);

		public static void Apply(string pdf, string output, Draw draw, Action<double> progress = null, CancellationToken cancel = default)
		{
			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify);
			int count = doc.PageCount;
			if (count == 0) throw new InvalidOperationException(T.S("err.noPages"));
			for (int i = 0; i < count; i++)
			{
				cancel.ThrowIfCancellationRequested();
				progress?.Invoke(i / (double)count);
				OnPage(doc.Pages[i], (g, w, h) => draw(g, w, h, i, count));
			}
			doc.Options.CompressContentStreams = true;
			doc.Save(output);
		}

		/// <summary>The visible box of a page in its own units: the crop box inside the media box (PDF y goes up).</summary>
		static (double x0, double y0, double x1, double y1) Visible(PdfPage page)
		{
			var mb = page.MediaBox;
			double x0 = Math.Min(mb.X1, mb.X2), y0 = Math.Min(mb.Y1, mb.Y2), x1 = Math.Max(mb.X1, mb.X2), y1 = Math.Max(mb.Y1, mb.Y2);
			if (page.Elements.ContainsKey("/CropBox"))
			{
				var cb = page.CropBox;
				double cx0 = Math.Max(x0, Math.Min(cb.X1, cb.X2)), cy0 = Math.Max(y0, Math.Min(cb.Y1, cb.Y2));
				double cx1 = Math.Min(x1, Math.Max(cb.X1, cb.X2)), cy1 = Math.Min(y1, Math.Max(cb.Y1, cb.Y2));
				// a crop box outside the page is ignored (as readers do)
				if (cx1 - cx0 > 1 && cy1 - cy0 > 1) (x0, y0, x1, y1) = (cx0, cy0, cx1, cy1);
			}
			return (x0, y0, x1, y1);
		}

		static void OnPage(PdfPage page, Action<XGraphics, double, double> draw)
		{
			int rotate = ((page.Rotate % 360) + 360) % 360;
			var (x0, y0, x1, y1) = Visible(page);
			double w = x1 - x0, h = y1 - y0;
			bool sideways = rotate is 90 or 270;
			double vw = sideways ? h : w, vh = sideways ? w : h;

			// PDFsharp turns its drawing for a turned page by itself, in its own way: the page is drawn as if not turned,
			// with our own turn (below), and its /Rotate is put back after
			// (its orientation is not put back: that swaps the media box's width and height; the box is kept as it was)
			var mb = page.MediaBox;
			var box = new PdfRectangle(new XPoint(mb.X1, mb.Y1), new XPoint(mb.X2, mb.Y2));
			page.Elements.Remove("/Rotate");
			page.Orientation = PdfSharp.PageOrientation.Portrait;
			try
			{
				using var g = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
				// what the reader sees (u right, v down, from its top left) → the page's own units (x right, y up)
				//   0°: x = x0 + u, y = y1 - v      90°: x = x0 + v, y = y0 + u
				// 180°: x = x1 - u, y = y0 + v     270°: x = x1 - v, y = y1 - u
				var (a, b, c, d, e, f) = rotate switch
				{
					90 => (0.0, 1.0, 1.0, 0.0, x0, y0),
					180 => (-1.0, 0.0, 0.0, 1.0, x1, y0),
					270 => (0.0, -1.0, -1.0, 0.0, x1, y1),
					_ => (1.0, 0.0, 0.0, -1.0, x0, y1),
				};
				// the page's units → PDFsharp's: it writes X as it is and y = height - Y (the height of the page not turned),
				// whatever the media box starts at; so Y = height - y
				// (x, y) = (a·u + c·v + e, b·u + d·v + f) in XMatrix order (m11 = a, m12 = b, m21 = c, m22 = d)
				var toPage = new XMatrix(a, b, c, d, e, f);
				var toPdfSharp = new XMatrix(1, 0, 0, -1, 0, page.Height.Point);
				var m = toPage * toPdfSharp;
				g.MultiplyTransform(m);
				draw(g, vw, vh);
			}
			finally
			{
				page.MediaBox = box;
				if (rotate != 0) page.Elements.SetInteger("/Rotate", rotate);
			}
		}
	}
}
