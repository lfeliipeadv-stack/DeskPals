using System.Globalization;
using System.Text;

namespace DeskPalsTools
{
	/// <summary>
	/// The jobs in groups for the window: the most useful few first ("Suggested"), the others under a title each
	/// (convert, edit, read and compare, the assistant, protect, other), behind "More options" while folded. A search
	/// finds a job by its name (without accents or case).
	/// </summary>
	internal static class JobGroups
	{
		/// <summary>More jobs than this: the window shows the first ones and folds the rest.</summary>
		public const int Fold = 8, Top = 5;

		public static readonly string[] Order = { "convert", "edit", "read", "ai", "protect", "other" };

		static readonly Dictionary<string, string> Of = new()
		{
			["topdf"] = "convert", ["merge"] = "convert", ["tojpg"] = "convert", ["topng"] = "convert", ["tomp3"] = "convert", ["toword"] = "convert",
			["toexcel"] = "convert", ["topictures"] = "convert", ["split"] = "convert", ["zip"] = "convert", ["unzip"] = "convert", ["cleanscan"] = "convert",
			["heichelp"] = "convert", ["smallervideo"] = "convert",
			["view"] = "edit", ["rotate"] = "edit", ["pages"] = "edit", ["pagenumbers"] = "edit", ["watermark"] = "edit", ["compress"] = "edit",
			["smaller"] = "edit", ["resize"] = "edit", ["nobackground"] = "edit", ["photos"] = "edit", ["cutaudio"] = "edit", ["joinaudio"] = "edit", ["fillform"] = "edit",
			["ocr"] = "read", ["searchable"] = "read", ["compare"] = "read",
			["transcribe"] = "ai", ["subtitles"] = "ai", ["summarize"] = "ai", ["askai"] = "ai", ["translate"] = "ai",
			["protect"] = "protect", ["unprotect"] = "protect",
		};

		public static string GroupOf(string id) => Of.TryGetValue(id, out var g) ? g : id.StartsWith("recipe-") ? "convert" : "other";

		public static string Emoji(string group) => group switch
		{
			"convert" => "📄", "edit" => "✏️", "read" => "🔎", "ai" => "🤖", "protect" => "🔒", _ => "🧩",
		};

		/// <summary>Does the job's text have every word of the search (accents and case left aside)?</summary>
		public static bool Matches(string text, string search)
		{
			string t = Plain(text);
			return Plain(search).Split(' ', StringSplitOptions.RemoveEmptyEntries).All(t.Contains);
		}

		static string Plain(string s) => new string((s ?? "").Normalize(NormalizationForm.FormD)
			.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
	}
}
