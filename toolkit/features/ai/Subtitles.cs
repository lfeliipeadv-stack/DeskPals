using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Subtitles": a video's (or a recording's) speech as an .srt file with the same name, next to it, so players
	/// (VLC, the Films app, YouTube when uploaded) find it by themselves. Long stretches are cut in lines of about 42
	/// letters, at most two per subtitle, as subtitles are usually written.
	/// </summary>
	internal static class Subtitles
	{
		const int LineLength = 42;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "subtitles", Emoji = "🎬",
				// right after "transcribe" (15); first among them for a video
				Rank = f => f.All(x => File.Exists(x) && new FileInfo(x).Length > 0) && f.All(AiJobs.Media) ? f.All(IsVideo) ? 14 : 17 : -1,
				Run = c => c.Each(async (file, i) =>
				{
					var cfg = AiService.Ready(needModel: false);
					AiService.TranscriptionModel(cfg);
					var segments = await Timed(cfg, file, (p, s) => c.Report((i + p) / c.Files.Count, s), c.Cancel);
					string o = OutName(file, ".srt");
					await File.WriteAllTextAsync(o, Srt(segments), new UTF8Encoding(false), c.Cancel);
					c.Outputs.Add(o);
					if (c.Files.Count == 1) c.Preview = T.S("subs.made", segments.Count);
				}),
			});
		}

		/// <summary>All the speech with its times in the whole file (each piece's times moved by where it starts).</summary>
		static async Task<List<AiService.Segment>> Timed(AiService.Config cfg, string file, Action<double, string> progress, CancellationToken cancel)
		{
			progress(0, T.S("ai.preparing"));
			var starts = new List<double>();
			List<string> pieces;
			bool temporary = true;
			try { pieces = await AiAudio.Pieces(file, p => progress(p * 0.3, T.S("ai.preparing")), cancel, starts); }
			catch (Exception) when (!cancel.IsCancellationRequested && AiAudio.CanSendAsIs(file))
			{
				pieces = new List<string> { file };
				starts = new List<double> { 0 };
				temporary = false;
			}
			try
			{
				var all = new List<AiService.Segment>();
				for (int i = 0; i < pieces.Count; i++)
				{
					double done = 0.3 + 0.7 * i / pieces.Count;
					progress(done, pieces.Count > 1 ? T.S("ai.sending", i + 1, pieces.Count) : T.S("ai.listening"));
					double at = i < starts.Count ? starts[i] : 0;
					foreach (var s in await AiService.TranscribeTimed(cfg, pieces[i], cancel, w => progress(done, w)))
						all.Add(s with { Start = s.Start + at, End = s.End + at });
				}
				if (all.Count == 0) throw new AiException(T.S("ai.noSpeech"));
				return all;
			}
			finally { if (temporary) AiAudio.Delete(pieces); }
		}

		/// <summary>The subtitles in SubRip's way: number, "00:01:02,500 --> 00:01:05,000", one or two lines, a blank line.</summary>
		internal static string Srt(IEnumerable<AiService.Segment> segments)
		{
			var sb = new StringBuilder();
			int n = 0;
			foreach (var s in Split(segments))
			{
				sb.Append(++n).Append("\r\n");
				sb.Append(Time(s.Start)).Append(" --> ").Append(Time(Math.Max(s.End, s.Start + 0.5))).Append("\r\n");
				sb.Append(Lines(s.Text)).Append("\r\n\r\n");
			}
			return sb.ToString();
		}

		/// <summary>A stretch too long for two lines is shared out in several subtitles (its time in proportion to the letters).</summary>
		static IEnumerable<AiService.Segment> Split(IEnumerable<AiService.Segment> segments)
		{
			foreach (var s in segments)
			{
				string text = string.Join(" ", s.Text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
				if (text.Length <= LineLength * 2) { yield return s with { Text = text }; continue; }
				var parts = new List<string>();
				var words = text.Split(' ');
				var cur = new StringBuilder();
				foreach (var w in words)
				{
					if (cur.Length > 0 && cur.Length + 1 + w.Length > LineLength * 2) { parts.Add(cur.ToString()); cur.Clear(); }
					if (cur.Length > 0) cur.Append(' ');
					cur.Append(w);
				}
				if (cur.Length > 0) parts.Add(cur.ToString());
				double total = parts.Sum(p => p.Length), at = s.Start, length = s.End - s.Start;
				foreach (var p in parts)
				{
					double d = length * p.Length / total;
					yield return new AiService.Segment(at, at + d, p);
					at += d;
				}
			}
		}

		/// <summary>One line, or two of about the same length broken at a space.</summary>
		static string Lines(string text)
		{
			if (text.Length <= LineLength) return text;
			int mid = text.Length / 2, best = -1;
			for (int d = 0; d < mid; d++)
			{
				if (mid + d < text.Length && text[mid + d] == ' ') { best = mid + d; break; }
				if (mid - d > 0 && text[mid - d] == ' ') { best = mid - d; break; }
			}
			return best < 0 ? text : text[..best] + "\r\n" + text[(best + 1)..];
		}

		internal static string Time(double seconds)
		{
			var t = TimeSpan.FromMilliseconds(Math.Round(Math.Max(0, seconds) * 1000));
			return ((int)t.TotalHours).ToString("00", CultureInfo.InvariantCulture) + ":" + t.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":"
				+ t.Seconds.ToString("00", CultureInfo.InvariantCulture) + "," + t.Milliseconds.ToString("000", CultureInfo.InvariantCulture);
		}
	}
}
