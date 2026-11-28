using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using static DeskPalsTools.Jobs;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// A PDF as pictures: "PDF to pictures" (each page drawn by Windows as a PNG, 150 dpi, in a folder) and "Save the
	/// photos in it" (the pictures the PDF carries, as they are: JPEG stays JPEG, the rest becomes PNG).
	/// </summary>
	internal static class PdfPictures
	{
		const double Dpi = 150;
		/// <summary>Smaller pictures are lines, logos and decorations, not photos.</summary>
		const int MinSide = 64;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "topictures", Emoji = "🖼️",
				Rank = f => f.Count > 0 && f.All(x => File.Exists(x) && IsPdf(x) && PdfLock.Of(x) == PdfLock.State.Open) ? 54 : -1,
				Run = c => c.Each(async (file, i) =>
				{
					string folder = OutFolder(file, T.S("pictures.name"));
					c.Outputs.Add(folder);
					await Pages(file, folder, p => c.Report((i + p) / c.Files.Count, Path.GetFileName(file)), c.Cancel);
				}),
			});
			Register(new JobDef
			{
				Id = "photos", Emoji = "📷",
				Rank = f => f.Count == 1 && File.Exists(f[0]) && IsPdf(f[0]) && PdfLock.Of(f[0]) == PdfLock.State.Open ? 59 : -1,
				Run = c =>
				{
					string file = c.Files[0];
					c.Report(0, Path.GetFileName(file));
					string folder = OutName(file, "", T.S("photos.name"));
					int n = Photos(file, folder, c.Cancel);
					if (n == 0) throw new InvalidOperationException(T.S("photos.none"));
					c.Outputs.Add(folder);
					c.Preview = T.S("photos.made", n);
					return Task.CompletedTask;
				},
			});
		}

		/// <summary>Every page as "page 01.png"… (the number as long as the last one).</summary>
		internal static async Task Pages(string pdf, string folder, Action<double> progress, CancellationToken cancel)
		{
			var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdf));
			var doc = await WinPdf.PdfDocument.LoadFromFileAsync(file);
			int digits = doc.PageCount.ToString().Length;
			for (uint i = 0; i < doc.PageCount; i++)
			{
				cancel.ThrowIfCancellationRequested();
				progress?.Invoke(i / (double)doc.PageCount);
				using var page = doc.GetPage(i);
				string name = Path.Combine(folder, T.S("name.page", (i + 1).ToString().PadLeft(digits, '0')) + ".png");
				using var output = File.Create(name);
				using var stream = output.AsRandomAccessStream();
				await page.RenderToStreamAsync(stream, new WinPdf.PdfPageRenderOptions
				{
					DestinationWidth = (uint)Math.Max(1, Math.Round(page.Size.Width * Dpi / 96)),
					BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255),
					BitmapEncoderId = Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,
				});
			}
		}

		/// <summary>The pictures inside the PDF, saved in <paramref name="folder"/> (made only when there is one). Gives how many.</summary>
		internal static int Photos(string pdf, string folder, CancellationToken cancel)
		{
			int n = 0;
			var seen = new HashSet<string>();
			using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
			foreach (var page in doc.GetPages())
			{
				int k = 0;
				foreach (var img in page.GetImages())
				{
					cancel.ThrowIfCancellationRequested();
					k++;
					if (img.WidthInSamples < MinSide || img.HeightInSamples < MinSide) continue;
					byte[] bytes;
					string ext;
					// a JPEG is kept as it is (nothing lost); anything else as PNG when it can be read
					if (img.ImageDictionary.ContainsKey(UglyToad.PdfPig.Tokens.NameToken.Filter) && img.ImageDictionary.Data[UglyToad.PdfPig.Tokens.NameToken.Filter.Data] is UglyToad.PdfPig.Tokens.NameToken f && f.Data == "DCTDecode")
					{ bytes = img.RawBytes.ToArray(); ext = ".jpg"; }
					else if (img.TryGetPng(out var png)) { bytes = png; ext = ".png"; }
					else continue;
					// the same picture used on many pages (a logo) is saved once
					string key = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes));
					if (!seen.Add(key)) continue;
					Directory.CreateDirectory(folder);
					n++;
					File.WriteAllBytes(Path.Combine(folder, T.S("photos.file", page.Number, k) + ext), bytes);
				}
			}
			return n;
		}
	}
}
