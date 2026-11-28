using System.Globalization;

namespace DeskPalsTools
{
	/// <summary>
	/// The languages Tesseract reads ("tessLanguages" in the extension's settings, as "por+spa+eng"). Not chosen: the
	/// program's language and English. Each new language is downloaded (a few MB) the first time it is used.
	/// </summary>
	internal static class TessLanguages
	{
		public const string Key = "tessLanguages";

		/// <summary>The languages to use now, in Tesseract's way ("por+eng").</summary>
		public static string Current()
		{
			var known = TessData.All().Select(x => x.tess).ToHashSet();
			var chosen = ToolSettings.Text(Key).Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(known.Contains).Distinct().ToList();
			return chosen.Count > 0 ? string.Join("+", chosen) : TessData.LanguagesFor(T.Code);
		}

		public static void Set(IEnumerable<string> tess) => ToolSettings.Set(Key, string.Join("+", tess));

		/// <summary>The language's own name ("português", "日本語"), from Windows.</summary>
		public static string NameOf(string code)
		{
			try
			{
				string n = CultureInfo.GetCultureInfo(code).NativeName;
				// "português (Brasil)" → "Português"; the country does not matter for reading
				int p = n.IndexOf(" (", StringComparison.Ordinal);
				if (p > 0 && code.Contains('-') && code != "zh-CN") n = n[..p];
				return n.Length > 0 ? char.ToUpper(n[0], CultureInfo.GetCultureInfo(code)) + n[1..] : code;
			}
			catch (Exception) { return code; }
		}

		/// <summary>The window to choose them: a box per language, those in use ticked. At least one must stay.</summary>
		public static void Pick(IWin32Window owner)
		{
			var now = Current().Split('+').ToHashSet();
			var all = TessData.All().Select(x => (x.code, x.tess, name: NameOf(x.code))).OrderBy(x => x.name, StringComparer.CurrentCulture).ToList();
			var fields = new List<Ask.Field> { new() { Kind = Ask.Kind.Info, Label = T.S("ocrpdf.langs.intro") } };
			fields.AddRange(all.Select(x => new Ask.Field { Key = x.tess, Label = x.name, Kind = Ask.Kind.Check, Value = now.Contains(x.tess) ? "1" : "0" }));
			while (true)
			{
				var a = Ask.Show(owner, T.S("ocrpdf.pref.languages"), null, fields.ToArray());
				if (a == null) return;
				var chosen = all.Where(x => a.TryGetValue(x.tess, out var v) && v == "1").Select(x => x.tess).ToList();
				if (chosen.Count > 0) { Set(chosen); return; }
				MessageBox.Show(owner, T.S("ocrpdf.langs.none"), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
	}
}
