using System.Drawing;
using System.Runtime.CompilerServices;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>Tests of "See and edit": where it is offered, what Save makes (and what it never touches), and the window.</summary>
	internal static class ViewerTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("viewer plan", Plan);
			DevTests.Add("viewer pdf", SavePdf);
			DevTests.Add("viewer pictures", SavePictures);
			DevTests.Add("viewer window", Window);
		}

		static Dictionary<string, string> Opt(params string[] kv)
		{
			var d = new Dictionary<string, string>();
			for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
			return d;
		}

		static List<string> Ids(params string[] files) => Jobs.Plan(files).Select(j => j.Id).ToList();

		static string Text(string pdf, int page)
		{
			using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
			return doc.GetPage(page).Text;
		}

		static async Task Plan(DevTests.Ctx t)
		{
			string pdf = await t.Pdf("plan.pdf", "ONE");
			var ids = Ids(pdf);
			t.Check("viewer plan: one PDF → See and edit first", ids.FirstOrDefault() == "view", string.Join(",", ids));
			t.Check("viewer plan: one PDF no longer gets a turned copy per click", !ids.Contains("rotate") && !ids.Contains("pages"), string.Join(",", ids));
			string pic = t.Picture("plan.png", "P", Color.White, 300, 200);
			t.Check("viewer plan: pictures can be seen and edited", Ids(pic).Contains("view") && Ids(pic, t.Picture("plan2.png", "Q", Color.White, 300, 200)).Contains("view"));
			string pdf2 = await t.Pdf("plan2.pdf", "TWO");
			t.Check("viewer plan: two PDFs → their pages together here, after join (and turn them all at once)", Ids(pdf, pdf2).Contains("view") && Ids(pdf, pdf2).IndexOf("merge") < Ids(pdf, pdf2).IndexOf("view") && Ids(pdf, pdf2).Contains("rotate"), string.Join(",", Ids(pdf, pdf2)));
			string locked = Path.Combine(t.Dir, "plan-locked.pdf");
			PdfLock.Protect(pdf, locked, "abc");
			t.Check("viewer plan: a PDF with a password → not here", !Ids(locked).Contains("view"), string.Join(",", Ids(locked)));
		}

		/// <summary>A PDF whose pages are told apart by their width: page 1 is 301 points wide, page 2 302, page 3 303.</summary>
		static string Widths(string file, int pages = 3)
		{
			using var doc = new PdfSharp.Pdf.PdfDocument();
			for (int i = 1; i <= pages; i++) { var p = doc.AddPage(); p.Width = PdfSharp.Drawing.XUnit.FromPoint(300 + i); p.Height = PdfSharp.Drawing.XUnit.FromPoint(400); }
			doc.Save(file);
			return file;
		}

		/// <summary>Which page of <see cref="Widths"/> this page was (by its width).</summary>
		static int Was(string pdf, int page)
		{
			using var d = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			return (int)Math.Round(d.Pages[page - 1].MediaBox.Width) - 300;
		}

		static async Task SavePdf(DevTests.Ctx t)
		{
			string pdf = Widths(Path.Combine(t.Dir, "three.pdf"));
			long before = new FileInfo(pdf).Length;
			var r = await Jobs.Run("view", new[] { pdf }, null, default, Opt("kind", "pdf", "order", "3,1", "rotate", "1:90", "save", "copy"));
			bool good = r.ok && r.outputs.Count == 1 && r.outputs[0] != pdf && PdfWork.Pages(r.outputs[0]) == 2;
			int rot = -1;
			if (good) using (var d = PdfReader.Open(r.outputs[0], PdfDocumentOpenMode.Import)) rot = d.Pages[1].Rotate;
			t.Check("viewer pdf: a copy with the pages in the new order, turned", good && Was(r.outputs[0], 1) == 3 && Was(r.outputs[0], 2) == 1 && rot == 90, r.message + " rot=" + rot);
			t.Check("viewer pdf: the original is not touched by Save a copy", PdfWork.Pages(pdf) == 3 && new FileInfo(pdf).Length == before);
			t.Check("viewer pdf: the copy is called “… (edited)”", good && Path.GetFileNameWithoutExtension(r.outputs[0]).Contains(T.S("viewer.name.edited")), r.outputs.FirstOrDefault());

			r = await Jobs.Run("view", new[] { pdf }, null, default, Opt("kind", "pdf", "order", "1-3", "watermark", "1", "text", "RASCUNHO", "layout", "horizontal", "opacity", "30", "color", "gray", "numbers", "1", "format", "number", "position", "bottom-center", "first", "1", "size", "10"));
			t.Check("viewer pdf: numbers and a watermark go on every page", r.ok && PdfWork.Pages(r.outputs[0]) == 3 && Enumerable.Range(1, 3).All(p => Text(r.outputs[0], p).Contains("RASCUNHO")) && Text(r.outputs[0], 3).Contains("3"), r.message);

			// the one page the window shows is made by the same code, and keeps its number in the whole document
			string one = ViewerJobs.BuildPdf(pdf, Opt("order", "3,1,2", "numbers", "1", "format", "number", "first", "1", "size", "12"), 1, default);
			try { t.Check("viewer pdf: the preview of one page has its own number (2nd place → 2)", PdfWork.Pages(one) == 1 && Was(one, 1) == 1 && Text(one, 1).Contains("2"), Text(one, 1)); }
			finally { File.Delete(one); }

			string copy = Path.Combine(t.Dir, "replace.pdf");
			File.Copy(pdf, copy);
			r = await Jobs.Run("view", new[] { copy }, null, default, Opt("kind", "pdf", "order", "2,1", "save", "replace"));
			t.Check("viewer pdf: Replace the original writes over the same file", r.ok && r.outputs.SequenceEqual(new[] { copy }) && PdfWork.Pages(copy) == 2 && Was(copy, 1) == 2, r.message);
			t.Check("viewer pdf: no copy of the old file is left behind", !File.Exists(copy + ".deskpals-old") && Directory.GetFiles(t.Dir, "replace*").Length == 1, string.Join(",", Directory.GetFiles(t.Dir, "replace*").Select(Path.GetFileName)));

			using (var held = new FileStream(copy, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				r = await Jobs.Run("view", new[] { copy }, null, default, Opt("kind", "pdf", "order", "1", "save", "replace"));
			}
			t.Check("viewer pdf: a file open in another program → a clear message, the file unchanged", !r.ok && r.message.Contains(T.S("viewer.err.inUse")) && PdfWork.Pages(copy) == 2, r.message);
		}

		static async Task SavePictures(DevTests.Ctx t)
		{
			string a = t.Picture("pa.png", "A", Color.White, 300, 200), b = t.Picture("pb.png", "B", Color.LightYellow, 300, 200);
			var r = await Jobs.Run("view", new[] { a }, null, default, Opt("kind", "images", "rotate", "1:90", "as", "images", "save", "copy"));
			t.Check("viewer pictures: a turned picture is saved as a turned copy (same kind)", r.ok && r.outputs.Count == 1 && r.outputs[0].EndsWith(".png") && t.SizeOf(r.outputs[0]) == new Size(200, 300) && t.SizeOf(a) == new Size(300, 200), r.message + " " + (r.ok ? t.SizeOf(r.outputs[0]).ToString() : ""));

			r = await Jobs.Run("view", new[] { a }, null, default, Opt("kind", "images", "as", "images", "save", "copy"));
			t.Check("viewer pictures: nothing changed → nothing saved, and it says so", !r.ok && r.outputs.Count == 0 && r.message.Contains(T.S("viewer.err.nothing")), r.message);

			r = await Jobs.Run("view", new[] { a, b }, null, default, Opt("kind", "images", "order", "2,1", "rotate", "2:90", "as", "pdf", "save", "copy"));
			t.Check("viewer pictures: several pictures → one PDF in the order chosen", r.ok && r.outputs.Count == 1 && r.outputs[0].EndsWith(".pdf") && PdfWork.Pages(r.outputs[0]) == 2 && Path.GetFileName(r.outputs[0]).StartsWith("pb"), r.message + " " + r.outputs.FirstOrDefault());

			string jpg = Path.Combine(t.Dir, "photo.jpg");
			string png = t.Picture("photo-src.png", "J", Color.White, 400, 240);
			await ImageWork.Convert(png, jpg, false, 0);
			r = await Jobs.Run("view", new[] { jpg }, null, default, Opt("kind", "images", "rotate", "1:270", "as", "images", "save", "replace"));
			t.Check("viewer pictures: Replace the original turns the same photo", r.ok && r.outputs.SequenceEqual(new[] { jpg }) && t.SizeOf(jpg) == new Size(240, 400), r.message + " " + t.SizeOf(jpg));

			r = await Jobs.Run("view", new[] { a }, null, default, Opt("kind", "images", "clean", "bw", "as", "pdf", "save", "copy"));
			t.Check("viewer pictures: cleaned as a document → a PDF page", r.ok && r.outputs.Count == 1 && PdfWork.Pages(r.outputs[0]) == 1, r.message);
		}

		// ------------------------------------------------------------------ the window

		/// <summary>
		/// The Windows objects made on this window thread (thumbnails, PDF drawing) let go while the thread is still there:
		/// let go later, from another thread, they can keep the program from ending.
		/// </summary>
		static void ReleaseHere()
		{
			for (int i = 0; i < 2; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
			Application.DoEvents();
		}

		internal static Task OnWindowThread(Func<Task> work) => UiThread.Run(work);

		internal static async Task<bool> Until(Func<bool> ok, int ms = 15000)
		{
			for (int waited = 0; waited < ms; waited += 50)
			{
				Application.DoEvents();
				if (ok()) return true;
				await Task.Delay(50);
			}
			return ok();
		}

		static Task Window(DevTests.Ctx t) => OnWindowThread(async () =>
		{
			string pdf = await t.Pdf("window.pdf", "ALPHA", "BETA", "GAMMA");
			using (var f = new ViewerForm(new[] { pdf }) { SkipCloseQuestion = true })
			{
				f.Show();
				bool drawn = await Until(() => f.ThumbsDone && f.BigDone && f.View.Picture != null);
				t.Check("viewer window: the pages and the big page are drawn", drawn, $"thumbs={f.ThumbsDone} big={f.BigDone}");
				t.Check("viewer window: nothing to save at the start", !f.Dirty && !f.CanUndo);

				f.Grid.Turn(90);
				t.Check("viewer window: a turn is a change not saved yet, and the big page turns", f.Dirty && f.CanUndo && f.View.Turn == 90 && f.Build(false).GetValueOrDefault("rotate") == "1:90", f.Build(false).GetValueOrDefault("rotate"));
				f.Undo();
				t.Check("viewer window: undo → as at the start", !f.Dirty && f.CanRedo && f.View.Turn == 0 && !f.Build(false).ContainsKey("rotate"));
				f.Redo();
				t.Check("viewer window: redo → turned again", f.Dirty && f.Build(false).GetValueOrDefault("rotate") == "1:90");

				f.Grid.FocusOn(1);
				f.Grid.DeleteSelected();
				t.Check("viewer window: delete a page", f.Grid.Tiles.Count == 2 && f.Build(false)["order"] == "1,3", f.Build(false)["order"]);

				f.SetNumbers(Opt("position", "bottom-center", "format", "number", "first", "1", "size", "12"));
				bool preview = await Until(() => f.BigDone && f.View.Picture != null);
				t.Check("viewer window: page numbers are seen before saving (the preview is already turned)", preview && f.View.Turn == 0 && f.Build(false).GetValueOrDefault("numbers") == "1", $"big={f.BigDone} turn={f.View.Turn}");
				f.Undo();
				t.Check("viewer window: undo takes the numbers away again", !f.Build(false).ContainsKey("numbers"));

				f.View.ZoomIn();
				t.Check("viewer window: zoom", f.View.Zoom > 1);
				f.View.Fit();
				t.Check("viewer window: fit", f.View.Zoom == 1);

				var a = f.Build(true);
				t.Check("viewer window: Replace the original is asked for with “replace”", a["save"] == "replace" && a["kind"] == "pdf");
				f.Close();
			}

			string p1 = t.Picture("w1.png", "ONE", Color.White, 600, 400), p2 = t.Picture("w2.png", "TWO", Color.White, 600, 400);
			using (var f = new ViewerForm(new[] { p1, p2 }) { SkipCloseQuestion = true })
			{
				f.Show();
				bool drawn = await Until(() => f.ThumbsDone && f.BigDone && f.View.Picture != null);
				t.Check("viewer window: pictures are drawn", drawn);
				t.Check("viewer window: several pictures are saved as one PDF by default", f.AsPdf && f.Build(false)["as"] == "pdf");
				f.SetClean("gray");
				bool cleaned = await Until(() => f.BigDone && f.View.Picture != null, 30000);
				t.Check("viewer window: the cleaned page is seen before saving", cleaned && f.Build(false)["clean"] == "gray" && f.Dirty);
				f.AsPdf = false;
				t.Check("viewer window: pictures can be saved as pictures too", f.Build(false)["as"] == "images");
				f.Close();
			}
		});
	}
}
