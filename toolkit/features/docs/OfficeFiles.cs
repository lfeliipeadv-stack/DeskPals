using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace DeskPalsTools
{
	/// <summary>
	/// Word (.docx) and Excel (.xlsx) files written by hand: the few parts every Office (and LibreOffice, Google Docs)
	/// opens, nothing else. A document is paragraphs (with page breaks between pages); a workbook is sheets of cells,
	/// numbers kept as numbers.
	/// </summary>
	internal static class OfficeFiles
	{
		/// <summary>One paragraph of a document: its text, whether it is a title, whether a new page starts before it.</summary>
		public sealed record Para(string Text, bool Title = false, bool NewPage = false);

		public static void Docx(string file, IEnumerable<Para> paragraphs, string language = null)
		{
			var body = new StringBuilder();
			foreach (var p in paragraphs)
			{
				body.Append("<w:p>");
				if (p.Title || p.NewPage || language != null)
				{
					body.Append("<w:pPr>");
					if (p.Title) body.Append("<w:pStyle w:val=\"Heading1\"/>");
					if (p.NewPage) body.Append("<w:pageBreakBefore/>");
					body.Append("</w:pPr>");
				}
				// a line break inside a paragraph stays a line break
				var lines = (p.Text ?? "").Replace("\r\n", "\n").Split('\n');
				for (int i = 0; i < lines.Length; i++)
				{
					body.Append("<w:r>");
					if (language != null) body.Append("<w:rPr><w:lang w:val=\"").Append(Esc(language)).Append("\"/></w:rPr>");
					if (i > 0) body.Append("<w:br/>");
					body.Append("<w:t xml:space=\"preserve\">").Append(Esc(Clean(lines[i]))).Append("</w:t></w:r>");
				}
				body.Append("</w:p>");
			}
			Zip(file, new()
			{
				["[Content_Types].xml"] = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/><Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/></Types>",
				["_rels/.rels"] = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>",
				["word/_rels/document.xml.rels"] = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>",
				["word/styles.xml"] = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii=\"Calibri\" w:hAnsi=\"Calibri\" w:eastAsia=\"Calibri\" w:cs=\"Calibri\"/><w:sz w:val=\"22\"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after=\"160\" w:line=\"259\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault></w:docDefaults><w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style><w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/><w:next w:val=\"Normal\"/><w:pPr><w:keepNext/><w:spacing w:before=\"240\" w:after=\"120\"/><w:outlineLvl w:val=\"0\"/></w:pPr><w:rPr><w:b/><w:sz w:val=\"32\"/></w:rPr></w:style></w:styles>",
				["word/document.xml"] = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + body + "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1417\" w:right=\"1417\" w:bottom=\"1417\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr></w:body></w:document>",
			});
		}

		/// <summary>A workbook: sheets by name, each rows of cells. A cell that is a number (in the user's way or the invariant way) is kept as a number.</summary>
		public static void Xlsx(string file, IList<(string name, List<List<string>> rows)> sheets)
		{
			if (sheets.Count == 0) sheets = new List<(string, List<List<string>>)> { ("1", new List<List<string>>()) };
			var parts = new Dictionary<string, string>();
			var types = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
			var book = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
			var rels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < sheets.Count; i++)
			{
				int n = i + 1;
				string name = SheetName(sheets[i].name, used);
				types.Append("<Override PartName=\"/xl/worksheets/sheet").Append(n).Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
				book.Append("<sheet name=\"").Append(Esc(name)).Append("\" sheetId=\"").Append(n).Append("\" r:id=\"rId").Append(n).Append("\"/>");
				rels.Append("<Relationship Id=\"rId").Append(n).Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet").Append(n).Append(".xml\"/>");
				parts["xl/worksheets/sheet" + n + ".xml"] = Sheet(sheets[i].rows);
			}
			types.Append("</Types>");
			book.Append("</sheets></workbook>");
			rels.Append("</Relationships>");
			parts["[Content_Types].xml"] = types.ToString();
			parts["_rels/.rels"] = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
			parts["xl/workbook.xml"] = book.ToString();
			parts["xl/_rels/workbook.xml.rels"] = rels.ToString();
			Zip(file, parts);
		}

		static string Sheet(List<List<string>> rows)
		{
			var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
			for (int r = 0; r < rows.Count; r++)
			{
				sb.Append("<row r=\"").Append(r + 1).Append("\">");
				for (int c = 0; c < rows[r].Count; c++)
				{
					string v = Clean(rows[r][c] ?? "").Trim();
					if (v.Length == 0) continue;
					string at = Column(c) + (r + 1);
					if (Number(v, out double d)) sb.Append("<c r=\"").Append(at).Append("\"><v>").Append(d.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
					else sb.Append("<c r=\"").Append(at).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Esc(v)).Append("</t></is></c>");
				}
				sb.Append("</row>");
			}
			return sb.Append("</sheetData></worksheet>").ToString();
		}

		/// <summary>"1.234,56", "1,234.56", "12", "-3,5" are numbers; "001", "12/05", a CPF or a phone stay text.</summary>
		internal static bool Number(string v, out double d)
		{
			d = 0;
			v = v.Replace(" ", "").Replace(" ", "");
			if (v.Length == 0 || v.Length > 18 || (v.Length > 1 && v[0] == '0' && char.IsDigit(v[1]))) return false;
			if (!v.All(ch => char.IsDigit(ch) || ch is '.' or ',' or '-' or '+')) return false;
			int dot = v.LastIndexOf('.'), comma = v.LastIndexOf(',');
			string norm;
			if (dot >= 0 && comma >= 0) norm = dot > comma ? v.Replace(",", "") : v.Replace(".", "").Replace(',', '.');
			else if (comma >= 0) norm = v.Count(ch => ch == ',') == 1 && !(v.Length - comma - 1 == 3 && !CommaDecimal) ? v.Replace(',', '.') : v.Replace(",", "");
			// "1.234": a thousand in Portuguese, Spanish, German…; one point something in English
			else norm = v.Count(ch => ch == '.') > 1 || (dot >= 0 && v.Length - dot - 1 == 3 && CommaDecimal) ? v.Replace(".", "") : v;
			return double.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
		}

		/// <summary>Does the program's language write "3,5" for three and a half?</summary>
		static bool CommaDecimal
		{
			get
			{
				try { return CultureInfo.GetCultureInfo(T.Code).NumberFormat.NumberDecimalSeparator == ","; }
				catch (Exception) { return false; }
			}
		}

		static string Column(int c)
		{
			string s = "";
			for (c++; c > 0; c = (c - 1) / 26) s = (char)('A' + (c - 1) % 26) + s;
			return s;
		}

		static string SheetName(string name, HashSet<string> used)
		{
			string n = new string((name ?? "").Where(ch => !"[]:*?/\\".Contains(ch)).ToArray()).Trim();
			if (n.Length == 0) n = (used.Count + 1).ToString(CultureInfo.InvariantCulture);
			if (n.Length > 31) n = n[..31];
			string b = n;
			for (int k = 2; !used.Add(n); k++) n = (b.Length > 27 ? b[..27] : b) + " (" + k + ")";
			return n;
		}

		/// <summary>Characters XML cannot hold (control characters a PDF or OCR may give) are left out.</summary>
		static string Clean(string s) => new string((s ?? "").Where(ch => ch == '\t' || ch >= 0x20 && ch != 0xFFFE && ch != 0xFFFF).ToArray());

		static string Esc(string s) => SecurityElement.Escape(s) ?? "";

		static void Zip(string file, Dictionary<string, string> parts)
		{
			string tmp = file + ".tmp";
			using (var fs = File.Create(tmp))
			using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
				foreach (var (name, text) in parts)
				{
					var e = zip.CreateEntry(name, CompressionLevel.Optimal);
					using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
					w.Write(text);
				}
			File.Move(tmp, file, true);
		}
	}
}
