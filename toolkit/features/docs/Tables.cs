using System.Runtime.CompilerServices;
using UglyToad.PdfPig.Content;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Tables to Excel": the text of a PDF in a spreadsheet, one sheet per page, each line a row and each block of
	/// words a cell; the cells of the lines of one table are put in the same columns (by where they start), so a table
	/// keeps its columns. Numbers become numbers. A scan is read first (its text made searchable in a temporary copy).
	/// </summary>
	internal static class Tables
	{
		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "toexcel", Emoji = "📊",
				Rank = f => f.Count == 1 && File.Exists(f[0]) && IsPdf(f[0]) && PdfLock.Of(f[0]) == PdfLock.State.Open ? 32 : -1,
				Run = Run,
			});
		}

		static async Task Run(JobContext c)
		{
			string file = c.Files[0];
			string source = file, temp = null;
			try
			{
				if (PdfRaster.ScanOnly(file))
				{
					// a scan: its words come from the OCR, written as hidden text in a temporary copy
					temp = Temp(".pdf");
					void Notice(string s) { lock (c.Errors) if (!c.Errors.Contains(s)) c.Errors.Add(s); }
					await Searchable.Make(file, temp, c, 0, 0.8, Notice);
					source = temp;
				}
				c.Report(0.85, Path.GetFileName(file));
				var sheets = await Task.Run(() => Read(source), c.Cancel);
				if (sheets.All(s => s.rows.Count == 0)) throw new InvalidOperationException(T.S("tables.none"));
				string o = OutName(file, ".xlsx");
				OfficeFiles.Xlsx(o, sheets);
				c.Outputs.Add(o);
				int tables = sheets.Sum(s => s.rows.Count(r => r.Count(x => x.Length > 0) >= 2));
				c.Preview = T.S("tables.made", sheets.Count(s => s.rows.Count > 0), tables);
			}
			finally { if (temp != null) try { File.Delete(temp); } catch (Exception) { } }
		}

		/// <summary>The sheets: "Page 1", "Page 2"… with their rows of cells.</summary>
		internal static List<(string name, List<List<string>> rows)> Read(string pdf)
		{
			var sheets = new List<(string, List<List<string>>)>();
			using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
			foreach (var page in doc.GetPages())
				sheets.Add((T.S("name.page", page.Number), Rows(page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList())));
			return sheets;
		}

		sealed record Cell(double Left, double Right, string Text);

		/// <summary>The words of a page as rows of cells, the cells of a table in columns.</summary>
		internal static List<List<string>> Rows(List<Word> words)
		{
			var lines = Lines(words).Select(Cells).ToList();
			var rows = new List<List<string>>();
			int i = 0;
			while (i < lines.Count)
			{
				// a table: lines in a row that have two cells or more
				if (lines[i].Count < 2) { rows.Add(new List<string> { lines[i][0].Text }); i++; continue; }
				int j = i;
				while (j < lines.Count && lines[j].Count >= 2) j++;
				var block = lines.GetRange(i, j - i);
				var starts = Columns(block.SelectMany(l => l.Select(c => c.Left)).ToList());
				foreach (var line in block)
				{
					var row = new List<string>(new string[starts.Count]);
					for (int k = 0; k < row.Count; k++) row[k] = "";
					foreach (var cell in line)
					{
						int col = 0;
						for (int k = 0; k < starts.Count; k++) if (starts[k] <= cell.Left + 4) col = k;
						row[col] = row[col].Length == 0 ? cell.Text : row[col] + " " + cell.Text;
					}
					rows.Add(row);
				}
				i = j;
			}
			return rows;
		}

		/// <summary>Where the columns start: the left edges of the cells, close ones (within 12 points) taken as one.</summary>
		static List<double> Columns(List<double> lefts)
		{
			var sorted = lefts.OrderBy(x => x).ToList();
			var starts = new List<double>();
			foreach (var x in sorted) if (starts.Count == 0 || x - starts[^1] > 12) starts.Add(x);
			return starts;
		}

		/// <summary>The words in lines, from the top: words whose middles are at about the same height.</summary>
		static List<List<Word>> Lines(List<Word> words)
		{
			var lines = new List<List<Word>>();
			foreach (var w in words.OrderByDescending(w => w.BoundingBox.Centroid.Y))
			{
				double h = Math.Max(2, w.BoundingBox.Height);
				var line = lines.LastOrDefault();
				if (line != null && Math.Abs(line.Average(x => x.BoundingBox.Centroid.Y) - w.BoundingBox.Centroid.Y) < h * 0.5) line.Add(w);
				else lines.Add(new List<Word> { w });
			}
			return lines.Select(l => l.OrderBy(w => w.BoundingBox.Left).ToList()).ToList();
		}

		/// <summary>A line cut in cells where the space between two words is wider than about two letters.</summary>
		static List<Cell> Cells(List<Word> line)
		{
			var cells = new List<Cell>();
			Cell cur = null;
			foreach (var w in line)
			{
				double letter = w.BoundingBox.Width / Math.Max(1, w.Text.Length);
				if (cur != null && w.BoundingBox.Left - cur.Right <= Math.Max(letter * 2.2, 4)) cur = cur with { Right = w.BoundingBox.Right, Text = cur.Text + " " + w.Text };
				else
				{
					if (cur != null) cells.Add(cur);
					cur = new Cell(w.BoundingBox.Left, w.BoundingBox.Right, w.Text);
				}
			}
			if (cur != null) cells.Add(cur);
			return cells;
		}
	}
}
