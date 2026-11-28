using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DeskPalsTools
{
	/// <summary>
	/// Some pages of a PDF, in a new order, some of them turned, saved as a new PDF. The pages are written as people
	/// write them: "3, 1, 2", "1-3, 7", "5-1" (backwards); the turns as "2:90, 4:180" (page: degrees clockwise).
	/// </summary>
	internal static class PdfPick
	{
		/// <summary>The page numbers (from 1) of a text like "1-3, 7". Throws a friendly message for a wrong one.</summary>
		public static List<int> Parse(string text, int count)
		{
			var list = new List<int>();
			// "1 - 3" is "1-3"
			string clean = System.Text.RegularExpressions.Regex.Replace(text ?? "", @"\s*[-–]\s*", "-");
			foreach (var raw in clean.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string part = raw.Trim();
				int dash = part.IndexOf('-');
				if (dash > 0)
				{
					int from = Number(part[..dash], text, count), to = Number(part[(dash + 1)..], text, count);
					for (int n = from; ; n += from <= to ? 1 : -1) { list.Add(n); if (n == to) break; }
				}
				else list.Add(Number(part, text, count));
			}
			return list;
		}

		static int Number(string s, string whole, int count)
		{
			if (!int.TryParse(s.Trim(), out int n) || n < 1) throw new InvalidOperationException(T.S("pdfpages.err.badPages", whole));
			if (n > count) throw new InvalidOperationException(T.S("pdfpages.err.pageOut", n, count));
			return n;
		}

		/// <summary>The turns of "2:90, 4:-90" (page → degrees clockwise, 0/90/180/270).</summary>
		public static Dictionary<int, int> ParseTurns(string text, int count)
		{
			var turns = new Dictionary<int, int>();
			foreach (var raw in (text ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var bits = raw.Split(':', '=');
				if (bits.Length != 2 || !int.TryParse(bits[1].Trim(), out int deg) || deg % 90 != 0) throw new InvalidOperationException(T.S("pdfpages.err.badPages", text));
				foreach (int n in Parse(bits[0], count)) turns[n] = (((turns.GetValueOrDefault(n) + deg) % 360) + 360) % 360;
			}
			return turns;
		}

		/// <summary>
		/// The pages to keep, from the options: "order" (the pages, in their new order; all when empty), less "delete"
		/// (pages taken out).
		/// </summary>
		public static List<int> Order(Dictionary<string, string> options, int count)
		{
			string order = options.GetValueOrDefault("order") ?? "";
			var pages = string.IsNullOrWhiteSpace(order) ? Enumerable.Range(1, count).ToList() : Parse(order, count);
			string delete = options.GetValueOrDefault("delete") ?? "";
			if (!string.IsNullOrWhiteSpace(delete)) { var gone = Parse(delete, count).ToHashSet(); pages.RemoveAll(gone.Contains); }
			if (pages.Count == 0) throw new InvalidOperationException(T.S("pdfpages.err.noPagesLeft"));
			return pages;
		}

		/// <summary>A new PDF with these pages of <paramref name="pdf"/>, in this order, turned as asked.</summary>
		public static void Save(string pdf, string output, Dictionary<string, string> options)
		{
			using var input = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
			var pages = Order(options, input.PageCount);
			var turns = ParseTurns(options.GetValueOrDefault("rotate"), input.PageCount);
			using var doc = new PdfDocument();
			doc.Info.Creator = "DeskPals Tools";
			if (!string.IsNullOrEmpty(input.Info.Title)) doc.Info.Title = input.Info.Title;
			foreach (int n in pages)
			{
				var page = doc.AddPage(input.Pages[n - 1]);
				if (turns.TryGetValue(n, out int deg) && deg != 0) page.Rotate = (((page.Rotate + deg) % 360) + 360) % 360;
			}
			doc.Save(output);
		}

		/// <summary>"3,1,2" from page numbers; "1-4" for a run of pages in order (shorter for the history and the command line).</summary>
		public static string Text(IEnumerable<int> pages)
		{
			var list = pages.ToList();
			var parts = new List<string>();
			for (int i = 0; i < list.Count;)
			{
				int j = i;
				while (j + 1 < list.Count && list[j + 1] == list[j] + 1) j++;
				parts.Add(j - i >= 2 ? list[i] + "-" + list[j] : string.Join(",", list.Skip(i).Take(j - i + 1)));
				i = j + 1;
			}
			return string.Join(",", parts);
		}
	}
}
