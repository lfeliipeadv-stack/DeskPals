using System.Runtime.CompilerServices;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// The first jobs: to PDF, join, split, turn, read (OCR), pictures, MP3, ZIP. Ranks (lower first): the main job of a
	/// kind of file 10–19, reading 20–29, pictures 30–39, sound 40–49, PDF pages 50–59, the rest 60–89, ZIP 1000.
	/// </summary>
	internal static class CoreJobs
	{
		static bool One(IList<string> f) => f.Count == 1 && File.Exists(f[0]);
		static bool AllFiles(IList<string> f) => f.All(File.Exists);

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "topdf", Emoji = "📄",
				Rank = f => !AllFiles(f) ? -1 : One(f) ? (IsOffice(f[0]) || IsImage(f[0]) ? 10 : -1) : (f.Any(x => IsOffice(x) || IsImage(x)) ? 12 : -1),
				Run = c => c.Each(async (file, i) => c.Outputs.Add(await ToPdf(file, OutName(file, ".pdf")))),
			});
			Register(new JobDef
			{
				Id = "merge", Emoji = "📚",
				Rank = f => f.Count > 1 && AllFiles(f) && f.All(IsPageable) ? 10 : -1,
				Run = async c =>
				{
					var parts = new List<string>();
					var temp = new List<string>();
					try
					{
						for (int i = 0; i < c.Files.Count; i++)
						{
							c.Cancel.ThrowIfCancellationRequested();
							c.Report(i / (double)(c.Files.Count + 1), Path.GetFileName(c.Files[i]));
							if (IsPdf(c.Files[i])) { parts.Add(c.Files[i]); continue; }
							string t = Temp(".pdf");
							temp.Add(t);
							parts.Add(await ToPdf(c.Files[i], t));
						}
						c.Report(c.Files.Count / (double)(c.Files.Count + 1), T.S("doing.joining"));
						string o = OutName(c.Files[0], ".pdf", T.S("name.joined"));
						PdfWork.Merge(parts, o);
						c.Outputs.Add(o);
					}
					finally { foreach (var t in temp) try { File.Delete(t); } catch (Exception) { } }
				},
			});
			Register(new JobDef
			{
				Id = "split", Emoji = "✂️",
				Rank = f => One(f) && IsPdf(f[0]) ? 50 : -1,
				Run = c => { c.Report(0, Path.GetFileName(c.Files[0])); c.Outputs.AddRange(PdfWork.Split(c.Files[0], OutFolder(c.Files[0], T.S("name.pages")))); return Task.CompletedTask; },
			});
			Register(new JobDef
			{
				Id = "rotate", Emoji = "🔄",
				// one PDF is turned in "See and edit" (nothing is saved until Save); several are turned all at once here
				Rank = f => f.Count > 1 && AllFiles(f) && f.All(IsPdf) ? 52 : -1,
				Run = c => c.Each((file, i) => { string o = OutName(file, ".pdf", T.S("name.rotated")); PdfWork.Rotate(file, o, 90); c.Outputs.Add(o); return Task.CompletedTask; }),
			});
			Register(new JobDef
			{
				Id = "ocr", Emoji = "🔎",
				Rank = f => AllFiles(f) && (One(f) ? IsPdf(f[0]) || IsImage(f[0]) : f.All(IsImage)) ? 20 : -1,
				Run = async c =>
				{
					// one engine for the whole run (Windows, or Tesseract when chosen in the settings)
					void Notice(string s) { lock (c.Errors) if (!c.Errors.Contains(s)) c.Errors.Add(s); }
					using var engine = await OcrText.Open((p, s) => c.Report(0, s), c.Cancel, Notice);
					await c.Each(async (file, i) =>
				{
					string text = IsPdf(file)
						? await OcrText.ReadPdf(engine, file, (p, s) => c.Report((i + p) / c.Files.Count, s), c.Cancel)
						: await OcrText.ReadImage(engine, file, c.Cancel);
					string o = OutName(file, ".txt", T.S("name.text"));
					await File.WriteAllTextAsync(o, text, c.Cancel);
					c.Outputs.Add(o);
					if (c.Files.Count == 1) c.Preview = text.Length > 160 ? text[..160] + "…" : text;
				});
				},
			});
			foreach (var (id, png, max, word, rank, emoji) in new[] { ("smaller", false, 1920, "name.smaller", 30, "🗜️"), ("tojpg", false, 0, null, 32, "🖼️"), ("topng", true, 0, null, 32, "🖼️") })
				Register(new JobDef
				{
					Id = id, Emoji = emoji,
					Rank = f => !AllFiles(f) || !f.All(IsImage) ? -1
						: id == "topng" ? (f.All(x => Ext(x) is ".jpg" or ".jpeg" or ".jfif") ? rank : -1)
						: id == "tojpg" ? (f.Any(x => !(Ext(x) is ".jpg" or ".jpeg" or ".jfif")) ? rank : -1)
						: rank,
					Run = c => c.Each(async (file, i) =>
					{
						string o = OutName(file, png ? ".png" : ".jpg", word == null ? null : T.S(word));
						await ImageWork.Convert(file, o, png, max);
						c.Outputs.Add(o);
					}),
				});
			Register(new JobDef
			{
				Id = "tomp3", Emoji = "🎵",
				Rank = f => AllFiles(f) && f.All(x => IsAudio(x) || IsVideo(x)) && f.Any(x => Ext(x) != ".mp3") ? 40 : -1,
				Run = c => c.Each(async (file, i) =>
				{
					if (Ext(file) == ".mp3") return;
					string o = OutName(file, ".mp3");
					await MediaWork.ToMp3(file, o, p => c.Report((i + p) / c.Files.Count, Path.GetFileName(file)), c.Cancel);
					c.Outputs.Add(o);
				}),
			});
			Register(new JobDef
			{
				Id = "zip", Emoji = "🗂️",
				Rank = f => f.Count == 1 && IsZip(f[0]) ? -1 : 1000,
				Run = c =>
				{
					c.Report(0, T.S("doing.zipping"));
					string o = OutName(c.Files[0], ".zip", c.Files.Count > 1 ? T.S("name.files") : null);
					ZipWork.Zip(c.Files, o);
					c.Outputs.Add(o);
					return Task.CompletedTask;
				},
			});
			Register(new JobDef
			{
				Id = "unzip", Emoji = "📂",
				Rank = f => f.Count == 1 && IsZip(f[0]) && File.Exists(f[0]) ? 10 : -1,
				Run = c =>
				{
					c.Report(0, Path.GetFileName(c.Files[0]));
					string folder = OutFolder(c.Files[0], null);
					ZipWork.Unzip(c.Files[0], folder);
					c.Outputs.Add(folder);
					return Task.CompletedTask;
				},
			});
		}
	}
}
