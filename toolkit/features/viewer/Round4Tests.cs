using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of the fourth round: the jobs in groups and the search, where files are saved, marks moved and resized,
	/// a signature on every page and with the date, the highlighter's colours and texts of several lines, filling in a
	/// form, recipes, and the notification.
	/// </summary>
	internal static class Round4Tests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("r4 groups", Groups);
			DevTests.Add("r4 output", OutputTest);
			DevTests.Add("r4 marks", MarksMore);
			DevTests.Add("r4 form", Form);
			DevTests.Add("r4 recipes", RecipesTest);
			DevTests.Add("r4 notice", NoticeTest);
		}

		static Dictionary<string, string> Opt(params string[] kv)
		{
			var d = new Dictionary<string, string>();
			for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
			return d;
		}

		// ------------------------------------------------------------------ groups and search

		static Task Groups(DevTests.Ctx t) => ViewerTests.OnWindowThread(async () =>
		{
			bool before = ToolSettings.Bool("jobsExpanded");
			try
			{
				ToolSettings.Set("jobsExpanded", false);
				string pdf = await t.Pdf("g.pdf", "G");
				int all = Jobs.Plan(new[] { pdf }).Count;
				using (var f = new MainForm(new[] { pdf }))
				{
					f.Show();
					await ViewerTests.Until(() => f.JobButtons.Any());
					t.Check("groups: many jobs → the first five, and “More options”", all > JobGroups.Fold && f.JobButtons.Count() == JobGroups.Top && f.MoreButton != null, $"{f.JobButtons.Count()} of {all}");
					f.MoreButton.PerformClick();
					await ViewerTests.Until(() => f.JobButtons.Count() == all);
					t.Check("groups: “More options” shows them all, under their titles", f.JobButtons.Count() == all && f.JobsArea.Controls.OfType<Label>().Count() >= 2, f.JobButtons.Count().ToString());
					var area = f.JobsArea;
					var cut = f.JobButtons.Where(b => b.Bottom > area.ClientSize.Height + area.VerticalScroll.Maximum).ToList();
					t.Check("groups: the search box is there for many jobs", f.SearchBox.Visible);
					f.SearchBox.Text = "senha".Length > 0 ? T.S("job.protect").Split(' ')[0] : "";
					await ViewerTests.Until(() => f.JobButtons.Count() < all);
					t.Check("groups: a search shows only the jobs with those words", f.JobButtons.Any() && f.JobButtons.All(b => JobGroups.Matches(b.Text, f.SearchBox.Text)) && f.JobButtons.Any(b => b.Name == "protect"), string.Join(",", f.JobButtons.Select(b => b.Name)));
					f.SearchBox.Text = "zzzz nothing";
					await ViewerTests.Until(() => !f.JobButtons.Any());
					t.Check("groups: nothing found → says so", !f.JobButtons.Any() && f.JobsArea.Controls.OfType<Label>().Any());
					f.SearchBox.Text = "";
					f.Close();
				}
				t.Check("groups: the search ignores accents and case", JobGroups.Matches("Deixar o texto pesquisável", "PESQUISAVEL texto") && !JobGroups.Matches("Juntar", "senha"));
			}
			finally { ToolSettings.Set("jobsExpanded", before); }
		});

		// ------------------------------------------------------------------ where files go

		static async Task OutputTest(DevTests.Ctx t)
		{
			string mode = ToolSettings.Text(Output.Key, "next"), folder = ToolSettings.Text(Output.FolderKey);
			try
			{
				string pic = t.Picture("o.png", "O", Color.White, 300, 200);
				string target = Path.Combine(t.Dir, "out-here");
				Directory.CreateDirectory(target);
				ToolSettings.Set(Output.Key, "folder"); ToolSettings.Set(Output.FolderKey, target);
				var r = await Jobs.Run("topdf", new[] { pic }, null);
				t.Check("output: a chosen folder gets the files made", r.ok && Path.GetDirectoryName(r.outputs[0]) == target, r.outputs.FirstOrDefault());
				Directory.Delete(target, true);
				r = await Jobs.Run("topdf", new[] { pic }, null);
				t.Check("output: a folder that is gone → next to the original (nothing lost)", r.ok && Path.GetDirectoryName(r.outputs[0]) == t.Dir, r.outputs.FirstOrDefault());
				ToolSettings.Set(Output.Key, "downloads");
				t.Check("output: Downloads is the user's Downloads folder", Output.Folder(pic) == Output.Downloads() && Directory.Exists(Output.Downloads()));
				ToolSettings.Set(Output.Key, "next");
				t.Check("output: by default next to the original", Output.Folder(pic) == t.Dir);
			}
			finally { ToolSettings.Set(Output.Key, mode); ToolSettings.Set(Output.FolderKey, folder); }
		}

		// ------------------------------------------------------------------ the marks, more

		static Task MarksMore(DevTests.Ctx t) => ViewerTests.OnWindowThread(async () =>
		{
			string pdf = await t.Pdf("m.pdf", "ONE", "TWO", "THREE");
			string sign = Path.Combine(t.Dir, "s.png");
			using (var bmp = new Bitmap(200, 80)) { using (var g = Graphics.FromImage(bmp)) g.DrawLine(new Pen(Color.Navy, 4), 10, 60, 190, 20); bmp.Save(sign); }
			using var f = new ViewerForm(new[] { pdf }) { SkipCloseQuestion = true };
			f.Show();
			await ViewerTests.Until(() => f.BigDone && f.View.Picture != null);

			f.TestHighlight("green");
			f.TestMark(MarkKind.Highlight, new RectangleF(0.1f, 0.1f, 0.3f, 0.05f));
			var m = Marks.Read(f.Build(false)["marks"]);
			t.Check("marks: the highlighter's colour is kept", m.Count == 1 && m[0].text == "green" && Marks.Highlight("green") != Marks.Highlight("yellow"));

			// moved and resized with the mouse (as the page view reports it)
			f.MarkMoved(0, new RectangleF(0.5f, 0.5f, 0.2f, 0.1f));
			m = Marks.Read(f.Build(false)["marks"]);
			t.Check("marks: a mark moved and resized keeps its new place", m.Count == 1 && Math.Abs(m[0].x - 0.5f) < 0.001 && Math.Abs(m[0].w - 0.2f) < 0.001, m.Count == 1 ? m[0].x + "," + m[0].w : "");
			f.Undo();
			m = Marks.Read(f.Build(false)["marks"]);
			t.Check("marks: and undo puts it back", m.Count == 1 && Math.Abs(m[0].x - 0.1f) < 0.001);

			// a signature on every page, with the date
			string keep = Marks.Signature;
			File.Copy(sign, Path.Combine(Places.Folder, "signature.png"), true);
			try
			{
				f.TestSignWay(date: true, all: true);
				f.TestMark(MarkKind.Sign, new RectangleF(0.6f, 0.8f, 0.3f, 0.08f));
				m = Marks.Read(f.Build(false)["marks"]);
				t.Check("marks: a signature on every page (initials)", Enumerable.Range(0, 3).All(i => m.Any(x => x.at == i && x.Kind == MarkKind.Sign)), string.Join(" ", m.Select(x => x.at + ":" + x.Kind)));
				string today = DateTime.Now.ToString("d", System.Globalization.CultureInfo.GetCultureInfo(T.Code));
				t.Check("marks: with today's date under it", m.Count(x => x.Kind == MarkKind.Text && x.text == today) == 3 && m.Where(x => x.Kind == MarkKind.Text).All(x => x.y > 0.88f - 0.001f), today);
			}
			finally { if (keep == null) File.Delete(Path.Combine(Places.Folder, "signature.png")); }
			f.Close();

			// a text of several lines on the saved page
			var marks = new List<Marks.Placed> { new() { at = 0, kind = (int)MarkKind.Text, x = 0.1f, y = 0.1f, w = 0.6f, h = 0.12f, text = "First line\nSecond line" } };
			var r = await Jobs.Run("view", new[] { pdf }, null, default, Opt("kind", "pdf", "order", "1-3", "marks", Marks.Write(marks)));
			string text = "";
			if (r.ok) using (var d = UglyToad.PdfPig.PdfDocument.Open(r.outputs[0])) text = d.GetPage(1).Text;
			t.Check("marks: a text of several lines keeps its lines", text.Contains("First line") && text.Contains("Second line"), text);
		});

		// ------------------------------------------------------------------ forms

		/// <summary>A small form written by hand: a text field "name", a tick "agree", a list "city".</summary>
		static string FormPdf(string file)
		{
			var objs = new List<string>
			{
				"<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R 5 0 R 6 0 R] /DA (/Helv 0 Tf 0 g) /DR << /Font << /Helv 7 0 R >> >> >> >>",
				"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
				"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 300] /Annots [4 0 R 5 0 R 6 0 R] >>",
				"<< /Type /Annot /Subtype /Widget /FT /Tx /T (name) /TU (Your name) /Rect [50 220 300 240] /DA (/Helv 12 Tf 0 g) /F 4 /P 3 0 R >>",
				"<< /Type /Annot /Subtype /Widget /FT /Btn /T (agree) /TU (I agree) /Rect [50 180 65 195] /V /Off /AS /Off /F 4 /P 3 0 R /AP << /N << /Yes 8 0 R /Off 8 0 R >> >> >>",
				"<< /Type /Annot /Subtype /Widget /FT /Ch /Ff 131072 /T (city) /TU (City) /Opt [(Lisbon) (Porto) (Recife)] /Rect [50 140 200 160] /DA (/Helv 12 Tf 0 g) /F 4 /P 3 0 R >>",
				"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
				"<< /Length 0 /Subtype /Form /BBox [0 0 15 15] >>\nstream\n\nendstream",
			};
			var sb = new StringBuilder("%PDF-1.4\n");
			var offsets = new List<int>();
			for (int i = 0; i < objs.Count; i++) { offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append(i + 1).Append(" 0 obj\n").Append(objs[i]).Append("\nendobj\n"); }
			int xref = Encoding.ASCII.GetByteCount(sb.ToString());
			sb.Append("xref\n0 ").Append(objs.Count + 1).Append("\n0000000000 65535 f \n");
			foreach (var o in offsets) sb.Append(o.ToString("D10")).Append(" 00000 n \n");
			sb.Append("trailer\n<< /Size ").Append(objs.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
			File.WriteAllText(file, sb.ToString(), Encoding.ASCII);
			return file;
		}

		static async Task Form(DevTests.Ctx t)
		{
			string pdf = FormPdf(Path.Combine(t.Dir, "form.pdf"));
			var fields = FormFill.Fields(pdf);
			t.Check("form: its fields are found, with the names people see", fields.Count == 3 && fields.Any(x => x.Label == "Your name" && x.Kind == "text") && fields.Any(x => x.Kind == "check") && fields.Any(x => x.Kind == "choice" && x.Choices.Contains("Porto")),
				string.Join(" | ", fields.Select(x => x.Label + ":" + x.Kind)));
			var ids = Jobs.Plan(new[] { pdf }).Select(j => j.Id).ToList();
			t.Check("form: “Fill in the form” is offered first", ids.FirstOrDefault() == "fillform", string.Join(",", ids));
			var r = await Jobs.Run("fillform", new[] { pdf }, null, default, Opt("field:name", "Maria Silva", "field:agree", "1", "field:city", "Porto"));
			t.Check("form: a filled copy is saved", r.ok && r.outputs.Count == 1, r.message);
			if (!r.ok) return;
			using var doc = PdfReader.Open(r.outputs[0], PdfDocumentOpenMode.Import);
			var form = doc.AcroForm;
			t.Check("form: the values are in their fields", (form.Fields["name"] as PdfTextField)?.Text == "Maria Silva" && (form.Fields["agree"] as PdfCheckBoxField)?.Checked == true && form.Fields["city"].Elements.GetString("/V") == "Porto",
				(form.Fields["name"] as PdfTextField)?.Text);
			t.Check("form: the original stays empty", string.IsNullOrEmpty(FormFill.Fields(pdf).First(x => x.Kind == "text").Value));
			string plain = await t.Pdf("noform.pdf", "X");
			t.Check("form: a PDF without fields is not offered it", !Jobs.Plan(new[] { plain }).Any(j => j.Id == "fillform"));
		}

		// ------------------------------------------------------------------ recipes

		static async Task RecipesTest(DevTests.Ctx t)
		{
			var before = Recipes.Read();
			try
			{
				Recipes.Save(new List<Recipes.Recipe> { new() { name = "Join and number", steps = new() { "merge", "pagenumbers" } } });
				string a = await t.Pdf("ra.pdf", "A"), b = await t.Pdf("rb.pdf", "B");
				var plan = Jobs.Plan(new[] { a, b });
				t.Check("recipes: offered first, with its own name", plan.FirstOrDefault()?.Id == "recipe-0" && plan[0].Text == "Join and number", string.Join(",", plan.Select(j => j.Id)));
				// the history keeps a limited number of entries: from empty, what the recipe wrote is counted
				History.Clear();
				var r = await Jobs.Run("recipe-0", new[] { a, b }, null, default, Opt("s1.position", "bottom-right", "s1.format", "number", "s1.first", "1", "s1.size", "12"));
				t.Check("recipes: the steps one after the other → the last one's file", r.ok && r.outputs.Count == 1 && PdfWork.Pages(r.outputs[0]) == 2 && r.outputs[0].Contains(T.S("pdfpages.name.numbered")), r.message + " " + r.outputs.FirstOrDefault());
				t.Check("recipes: the file between the steps is deleted", !Directory.GetFiles(t.Dir, "*" + T.S("name.joined") + ").pdf").Any(f => !f.Contains(T.S("pdfpages.name.numbered"))), string.Join(",", Directory.GetFiles(t.Dir).Select(Path.GetFileName)));
				t.Check("recipes: the history has the recipe, not each step", History.Read().Count == 1 && History.Read()[0].job == "recipe-0",
					string.Join(",", History.Read().Take(3).Select(e => e.job)));
				Recipes.Save(new List<Recipes.Recipe> { new() { name = "Impossible", steps = new() { "merge", "no-such-job" } } });
				var bad = await Jobs.Run("recipe-0", new[] { a, b }, null);
				t.Check("recipes: a step that cannot be done stops it, and says which", !bad.ok && bad.message.Contains("no-such-job"), bad.message);
				Recipes.Save(new List<Recipes.Recipe>());
				t.Check("recipes: deleted → no longer offered", !Jobs.Plan(new[] { a, b }).Any(j => j.Id.StartsWith("recipe-")));
				t.Check("recipes: the steps to choose are the real jobs", Recipes.Steps().Any(s => s.id == "merge") && !Recipes.Steps().Any(s => s.id.StartsWith("recipe-")));
			}
			finally { Recipes.Save(before); }
		}

		static Task NoticeTest(DevTests.Ctx t) => ViewerTests.OnWindowThread(async () =>
		{
			using var f = new Form { ShowInTaskbar = false, Size = new Size(200, 100) };
			f.Show();
			Notice.Show(f, "Make smaller", "1 file done", true);
			await Task.Delay(200);
			t.Check("notice: a long job done while away is told by Windows", Notice.Last == "Make smaller · 1 file done");
			f.Close();
		});
	}
}
