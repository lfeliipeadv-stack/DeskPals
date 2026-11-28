using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Transcribe" (a WhatsApp audio, a recording, a video → text) and "Summarize" (a PDF, a Word document, a text, an
	/// audio → a short summary), with the AI service DeskPals already uses. The results go next to the file, as .txt.
	/// </summary>
	internal static class AiJobs
	{
		internal static bool Media(string f) => IsAudio(f) || IsVideo(f);
		/// <summary>Files that exist and are not empty (an empty file has nothing to hear or to summarize).</summary>
		static bool AllFiles(IList<string> f) => f.All(x => File.Exists(x) && new FileInfo(x).Length > 0);

		/// <summary>Characters per request when summarizing (a long text goes in parts); the tests make it tiny.</summary>
		public static int ChunkChars = 12000;
		/// <summary>
		/// At most this many parts (about 240 000 characters, some 80 pages): the free plans allow few words a minute, so
		/// more would mean a very long wait.
		/// </summary>
		const int MaxParts = 20;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "transcribe", Emoji = "📝",
				Rank = f => AllFiles(f) && f.All(Media) ? 15 : -1,
				Run = c => c.Each(async (file, i) =>
				{
					var cfg = AiService.Ready(needModel: false);
					AiService.TranscriptionModel(cfg);   // says so before converting anything, when the service cannot do it
					string text = await Transcribe(cfg, file, (p, s) => c.Report((i + p) / c.Files.Count, s), c.Cancel);
					string o = OutName(file, ".txt", T.S("ai.name.transcription"));
					await File.WriteAllTextAsync(o, text, Encoding.UTF8, c.Cancel);
					c.Outputs.Add(o);
					if (c.Files.Count == 1) c.Preview = Short(text, 200);
				}),
			});
			Register(new JobDef
			{
				Id = "summarize", Emoji = "🧾",
				Rank = f => !AllFiles(f) || !f.All(x => Media(x) || AiText.CanRead(x)) ? -1 : f.All(Media) ? 16 : 26,
				Run = c => c.Each(async (file, i) =>
				{
					var cfg = AiService.Ready(needModel: true);
					Action<double, string> part = (p, s) => c.Report((i + p) / c.Files.Count, s);
					string text;
					if (Media(file))
					{
						AiService.TranscriptionModel(cfg);
						text = await Transcribe(cfg, file, (p, s) => part(p * 0.6, s), c.Cancel);
					}
					else
					{
						part(0, T.S("ai.reading"));
						text = await AiText.Read(file, (p, s) => part(p * 0.4, s), c.Cancel);
						if (text.Count(char.IsLetterOrDigit) < 3) throw new AiException(T.S("ai.noText"));
					}
					string summary = await Summarize(cfg, text, (p, s) => part(0.6 + p * 0.4, s), c.Cancel);
					string o = OutName(file, ".txt", T.S("ai.name.summary"));
					await File.WriteAllTextAsync(o, summary, Encoding.UTF8, c.Cancel);
					c.Outputs.Add(o);
					if (c.Files.Count == 1) c.Preview = Short(summary, 300);
				}),
			});
		}

		static string Short(string s, int max) => s.Length > max ? s[..max].TrimEnd() + "…" : s;

		/// <summary>All the speech of the file, piece after piece.</summary>
		public static async Task<string> Transcribe(AiService.Config cfg, string file, Action<double, string> progress, CancellationToken cancel)
		{
			progress(0, T.S("ai.preparing"));
			List<string> pieces;
			// the pieces are temporary files, deleted at the end; the user's own file never is
			bool temporary = true;
			try { pieces = await AiAudio.Pieces(file, p => progress(p * 0.3, T.S("ai.preparing")), cancel); }
			catch (Exception) when (!cancel.IsCancellationRequested && AiAudio.CanSendAsIs(file))
			{
				// Windows could not convert it (a WhatsApp voice note, often), but the service takes it as it is
				pieces = new List<string> { file };
				temporary = false;
			}
			try
			{
				var texts = new List<string>();
				for (int i = 0; i < pieces.Count; i++)
				{
					double done = 0.3 + 0.7 * i / pieces.Count;
					progress(done, pieces.Count > 1 ? T.S("ai.sending", i + 1, pieces.Count) : T.S("ai.listening"));
					string t = await AiService.Transcribe(cfg, pieces[i], cancel, s => progress(done, s));
					if (t.Length > 0) texts.Add(t);
				}
				string all = string.Join(" ", texts).Trim();
				if (all.Length == 0) throw new AiException(T.S("ai.noSpeech"));
				return all;
			}
			finally { if (temporary) AiAudio.Delete(pieces); }
		}

		/// <summary>The language the summary is written in (the program's), as the model understands it best.</summary>
		static string LanguageName
		{
			get
			{
				try { return new CultureInfo(T.Code).EnglishName; }
				catch (CultureNotFoundException) { return T.Code; }
			}
		}

		static string Instructions(bool part) =>
			"You summarize documents for someone who is not technical. Write in " + LanguageName + ". " +
			(part ? "This is one part of a longer document: list the main points of this part only. "
				: "Write a clear, faithful summary with the main points, in plain language and short paragraphs or simple bullet points. ") +
			"Use only what is in the text: never invent or assume facts, names, numbers or dates. Plain text, no Markdown, no title, no comments about the task.";

		/// <summary>A summary; a long text is summarized in parts, then the summaries of the parts are summarized together.</summary>
		public static async Task<string> Summarize(AiService.Config cfg, string text, Action<double, string> progress, CancellationToken cancel)
		{
			for (int round = 0; ; round++)
			{
				var parts = Split(text, ChunkChars);
				// a whole book (or a giant log) would mean hundreds of requests and the service's limit: said at once
				if (round == 0 && parts.Count > MaxParts) throw new AiException(T.S("ai.tooBig"));
				if (parts.Count == 1)
				{
					progress(0.8, T.S("ai.thinking"));
					string summary = Clean(await AiService.Chat(cfg, Instructions(false), parts[0], cancel, s => progress(0.8, s)));
					return summary.Length > 0 ? summary : throw new AiException(T.S("ai.serviceError", "?"));
				}
				var notes = new StringBuilder();
				for (int i = 0; i < parts.Count; i++)
				{
					double done = 0.8 * i / parts.Count;
					progress(done, T.S("ai.part", i + 1, parts.Count));
					// a part refused for the service's limit per minute is waited for and sent again (in AiService), so
					// the parts already done are never lost
					notes.AppendLine(Clean(await AiService.Chat(cfg, Instructions(true), parts[i], cancel, s => progress(done, s)))).AppendLine();
				}
				string next = notes.ToString().Trim();
				// a summary of the parts that did not get shorter (or a service that repeats) still ends
				if (next.Length == 0) throw new AiException(T.S("ai.serviceError", "?"));
				if (next.Length >= text.Length || round >= 3) text = next[..Math.Min(next.Length, ChunkChars)];
				else text = next;
			}
		}

		/// <summary>Parts of at most <paramref name="max"/> characters, cut between paragraphs, sentences or words when possible.</summary>
		public static List<string> Split(string text, int max)
		{
			var parts = new List<string>();
			text = text.Trim();
			while (text.Length > max)
			{
				int cut = text.LastIndexOf("\n\n", max, StringComparison.Ordinal);
				if (cut < max / 2) cut = text.LastIndexOf(". ", max, StringComparison.Ordinal) + 1;
				if (cut < max / 2) cut = text.LastIndexOf(' ', max);
				if (cut < max / 2) cut = max;
				parts.Add(text[..cut].Trim());
				text = text[cut..].Trim();
			}
			if (text.Length > 0 || parts.Count == 0) parts.Add(text);
			return parts;
		}

		/// <summary>The models sometimes write Markdown anyway: the stars and hashes go, the words stay.</summary>
		static string Clean(string s) => System.Text.RegularExpressions.Regex.Replace(s.Replace("**", ""), @"(?m)^#{1,6}\s*", "").Trim();
	}
}
