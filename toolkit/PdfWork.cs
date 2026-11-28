using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>PDFs with PDFsharp (MIT): pictures to PDF, joining, splitting, turning pages.</summary>
	internal static class PdfWork
	{
		/// <summary>A4 in points.</summary>
		const double A4W = 595.28, A4H = 841.89, Margin = 18;

		/// <summary>One page per picture, the picture as big as fits an A4 page (lying down when the picture is wide).</summary>
		public static async Task FromImages(IEnumerable<string> images, string pdf)
		{
			var temp = new List<string>();
			try
			{
				using var doc = new PdfDocument();
				doc.Info.Creator = "DeskPals Tools";
				foreach (var file in images)
				{
					// JPEG goes as it is (no quality lost); everything else becomes PNG first (WebP, HEIC, GIF, TIFF…)
					string use = file;
					if (!(Jobs.Ext(file) is ".jpg" or ".jpeg" or ".jfif" or ".png"))
					{
						use = Path.Combine(Path.GetTempPath(), "deskpals-tools-" + Guid.NewGuid().ToString("N") + ".png");
						temp.Add(use);
						await ImageWork.Convert(file, use, png: true, maxSide: 0);
					}
					using var img = XImage.FromFile(use);
					bool wide = img.PixelWidth > img.PixelHeight;
					var page = doc.AddPage();
					page.Width = XUnit.FromPoint(wide ? A4H : A4W);
					page.Height = XUnit.FromPoint(wide ? A4W : A4H);
					double pw = page.Width.Point - Margin * 2, ph = page.Height.Point - Margin * 2;
					double k = Math.Min(pw / img.PixelWidth, ph / img.PixelHeight);
					double w = img.PixelWidth * k, h = img.PixelHeight * k;
					using var g = XGraphics.FromPdfPage(page);
					g.DrawImage(img, Margin + (pw - w) / 2, Margin + (ph - h) / 2, w, h);
				}
				if (doc.PageCount == 0) throw new InvalidOperationException(T.S("err.noPages"));
				doc.Save(pdf);
			}
			finally { foreach (var t in temp) try { File.Delete(t); } catch (Exception) { } }
		}

		/// <summary>The pages of every PDF, in order, in one file.</summary>
		public static void Merge(IEnumerable<string> pdfs, string output)
		{
			using var doc = new PdfDocument();
			doc.Info.Creator = "DeskPals Tools";
			foreach (var file in pdfs)
			{
				using var input = PdfReader.Open(file, PdfDocumentOpenMode.Import);
				foreach (var page in input.Pages) doc.AddPage(page);
			}
			if (doc.PageCount == 0) throw new InvalidOperationException(T.S("err.noPages"));
			doc.Save(output);
		}

		/// <summary>One PDF per page, in a new folder ("Name - 1.pdf", "Name - 2.pdf"…).</summary>
		public static List<string> Split(string pdf, string folder)
		{
			var made = new List<string>();
			using var input = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			string name = Path.GetFileNameWithoutExtension(pdf);
			int digits = input.PageCount.ToString().Length;
			for (int i = 0; i < input.PageCount; i++)
			{
				using var one = new PdfDocument();
				one.Info.Creator = "DeskPals Tools";
				one.AddPage(input.Pages[i]);
				string file = Path.Combine(folder, name + " - " + (i + 1).ToString().PadLeft(digits, '0') + ".pdf");
				one.Save(file);
				made.Add(file);
			}
			return made;
		}

		/// <summary>Every page turned by <paramref name="degrees"/> (90 = clockwise), saved as a new file.</summary>
		public static void Rotate(string pdf, string output, int degrees)
		{
			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify);
			foreach (var page in doc.Pages) page.Rotate = ((page.Rotate + degrees) % 360 + 360) % 360;
			doc.Save(output);
		}

		public static int Pages(string pdf)
		{
			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			return doc.PageCount;
		}
	}
}
