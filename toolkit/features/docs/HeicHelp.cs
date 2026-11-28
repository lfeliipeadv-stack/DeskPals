using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Graphics.Imaging;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// iPhone photos (HEIC/HEIF). Windows opens them only with the free "HEIF Image Extensions" from the Microsoft Store.
	/// When they are there, the usual picture jobs ("Convert to JPG"…) just work; when they are not, "heichelp" comes first
	/// in the list: one friendly sentence and the Store page, opened.
	/// </summary>
	internal static class HeicHelp
	{
		public const string StoreLink = "ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG";

		/// <summary>How the Store page is opened (the tests put something harmless here).</summary>
		internal static Func<string, bool> Open = link =>
		{
			try { using var p = Process.Start(new ProcessStartInfo(link) { UseShellExecute = true }); return true; }
			catch (Exception) { return false; }
		};

		public static bool IsHeic(string f) => Ext(f) is ".heic" or ".heif";

		static bool? canDecode;

		/// <summary>Whether Windows has a decoder for .heic (asked once: the program lives for one job).</summary>
		public static bool CanDecode()
		{
			if (canDecode is bool known) return known;
			bool found = false;
			try
			{
				foreach (var info in BitmapDecoder.GetDecoderInformationEnumerator())
					if (info.FileExtensions.Any(e => e.Equals(".heic", StringComparison.OrdinalIgnoreCase) || e.Equals(".heif", StringComparison.OrdinalIgnoreCase))) { found = true; break; }
			}
			catch (Exception) { }
			canDecode = found;
			return found;
		}

		/// <summary>The friendly text for a picture Windows could not open (the HEIC one when that is the reason).</summary>
		public static string Explain(string file) => IsHeic(file) && !CanDecode() ? T.S("docs.heic.missing") : null;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "heichelp", Emoji = "📱",
				Rank = f => f.Any(x => File.Exists(x) && IsHeic(x)) && !CanDecode() ? 5 : -1,
				Run = c =>
				{
					c.Report(0, T.S("doing.heichelp"));
					// nothing is made: the sentence is the answer (in the bubble and in the window's message)
					string say = Open(StoreLink) ? T.S("docs.heic.opened") : T.S("docs.heic.cantOpen");
					c.Preview = say;
					if (say == T.S("docs.heic.opened")) c.Done = true; else c.Errors.Add(say);
					return Task.CompletedTask;
				},
			});
		}
	}
}
