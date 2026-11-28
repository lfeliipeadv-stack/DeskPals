using PdfSharp.Fonts;

namespace DeskPalsTools
{
	/// <summary>
	/// The fonts of Windows for the text PDFsharp writes (page numbers, watermarks, the hidden text of the OCR): Arial,
	/// Segoe UI, Times New Roman, Courier New, Calibri and Verdana in their four styles, anything else as Arial.
	/// </summary>
	internal sealed class WindowsFonts : IFontResolver
	{
		static readonly Dictionary<string, string[]> Files = new(StringComparer.OrdinalIgnoreCase)
		{
			// regular, bold, italic, bold italic
			["Arial"] = new[] { "arial.ttf", "arialbd.ttf", "ariali.ttf", "arialbi.ttf" },
			["Segoe UI"] = new[] { "segoeui.ttf", "segoeuib.ttf", "segoeuii.ttf", "segoeuiz.ttf" },
			["Times New Roman"] = new[] { "times.ttf", "timesbd.ttf", "timesi.ttf", "timesbi.ttf" },
			["Courier New"] = new[] { "cour.ttf", "courbd.ttf", "couri.ttf", "courbi.ttf" },
			["Calibri"] = new[] { "calibri.ttf", "calibrib.ttf", "calibrii.ttf", "calibriz.ttf" },
			["Verdana"] = new[] { "verdana.ttf", "verdanab.ttf", "verdanai.ttf", "verdanaz.ttf" },
		};

		static string Dir => Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

		public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
		{
			if (!Files.TryGetValue(familyName ?? "", out var set) || !File.Exists(Path.Combine(Dir, set[0]))) set = Files["Arial"];
			int i = (bold ? 1 : 0) + (italic ? 2 : 0);
			string file = set[i];
			// a style that is not there: the regular one, made bold or slanted by PDFsharp
			if (!File.Exists(Path.Combine(Dir, file))) return new FontResolverInfo(set[0], bold, italic);
			return new FontResolverInfo(file);
		}

		public byte[] GetFont(string faceName) => File.ReadAllBytes(Path.Combine(Dir, faceName));
	}
}
