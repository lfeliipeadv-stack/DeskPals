using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Translate": a document (PDF, Word, text) in another language, as a Word document next to it, made by the
	/// assistant part by part (paragraphs kept). The language chosen last time is offered first.
	/// </summary>
	internal static class Translate
	{
		/// <summary>About this many letters per request: the answer fits, and the free plans' limits are kept.</summary>
		internal const int Part = 3500;
		const int MaxParts = 60;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "translate", Emoji = "🌐",
				// after "summarize" (26) and "ask" (27)
				Rank = f => f.Count == 1 && File.Exists(f[0]) && new FileInfo(f[0]).Length > 0 && AiText.CanRead(f[0]) ? 28 : -1,
				Ask = (owner, files) => AskLanguage(owner),
				Run = Run,
			});
		}

		/// <summary>The program's languages, by their own names.</summary>
		internal static List<(string code, string name)> Languages() => T.Codes().Concat(new[] { "en" }).Distinct()
			.Select(c => (c, TessLanguages.NameOf(c))).OrderBy(x => x.Item2, StringComparer.CurrentCulture).ToList();

		static Dictionary<string, string> AskLanguage(IWin32Window owner)
		{
			string last = ToolSettings.Text("translateTo");
			if (string.IsNullOrEmpty(last)) last = T.Code == "en" ? "pt-BR" : "en";
			var a = Ask.Show(owner, T.S("job.translate"), T.S("translate.intro"),
				new Ask.Field { Key = "to", Label = T.S("translate.to"), Kind = Ask.Kind.Choice, Value = last, Choices = Languages().Select(x => (x.code, x.name)).ToArray() });
			if (a != null) try { ToolSettings.Set("translateTo", a["to"]); } catch (Exception) { }
			return a;
		}

		static string EnglishName(string code)
		{
			try { return CultureInfo.GetCultureInfo(code).EnglishName; } catch (Exception) { return code; }
		}

		static async Task Run(JobContext c)
		{
			string file = c.Files[0];
			string to = c.Option("to", T.Code == "en" ? "pt-BR" : "en");
			var cfg = AiService.Ready(needModel: true);
			c.Report(0, T.S("ai.reading"));
			string text = await AiText.Read(file, (p, s) => c.Report(p * 0.1, s), c.Cancel);
			if (text.Count(char.IsLetterOrDigit) < 3) throw new AiException(T.S("ai.noText"));
			var parts = Parts(text);
			if (parts.Count > MaxParts) throw new AiException(T.S("translate.tooBig"));
			string system = "You translate documents. Translate the user's text into " + EnglishName(to) + ". Keep the meaning, the paragraphs and "
				+ "line breaks, names, numbers, dates and lists. Do not add notes or explanations: answer with the translation only.";
			var done = new List<string>();
			for (int i = 0; i < parts.Count; i++)
			{
				double at = 0.1 + 0.88 * i / parts.Count;
				c.Report(at, parts.Count > 1 ? T.S("translate.part", i + 1, parts.Count) : T.S("translate.doing"));
				string t = await AiService.Chat(cfg, system, parts[i], c.Cancel, w => c.Report(at, w), 3000);
				done.Add(AskAiForm.Plain(t));
			}
			string all = string.Join("\n\n", done);
			string o = OutName(file, ".docx", T.S("translate.name", TessLanguages.NameOf(to)));
			OfficeFiles.Docx(o, Paras(all), to);
			c.Outputs.Add(o);
			c.Preview = all.Length > 200 ? all[..200].TrimEnd() + "…" : all;
		}

		/// <summary>The text in parts of about <see cref="Part"/> letters, cut between paragraphs (or, inside a very long one, between sentences).</summary>
		internal static List<string> Parts(string text)
		{
			var list = new List<string>();
			var sb = new StringBuilder();
			foreach (var para in text.Replace("\r\n", "\n").Split("\n\n"))
			{
				string p = para.Trim('\n');
				if (p.Trim().Length == 0) continue;
				if (sb.Length > 0 && sb.Length + p.Length + 2 > Part) { list.Add(sb.ToString()); sb.Clear(); }
				while (p.Length > Part)
				{
					int cut = p.LastIndexOfAny(new[] { '.', '!', '?', '。', '\n' }, Part - 1);
					if (cut < Part / 3) cut = p.LastIndexOf(' ', Part - 1);
					if (cut < Part / 3) cut = Part - 1;
					list.Add(p[..(cut + 1)].Trim());
					p = p[(cut + 1)..].Trim();
				}
				if (sb.Length > 0) sb.Append("\n\n");
				sb.Append(p);
			}
			if (sb.Length > 0) list.Add(sb.ToString());
			return list;
		}

		/// <summary>Paragraphs of the translation (a blank line between them; single line breaks kept inside).</summary>
		static IEnumerable<OfficeFiles.Para> Paras(string text) => text.Replace("\r\n", "\n").Split("\n\n")
			.Select(p => p.Trim('\n')).Where(p => p.Trim().Length > 0).Select(p => new OfficeFiles.Para(p));
	}
}
