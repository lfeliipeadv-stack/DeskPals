using System.Runtime.CompilerServices;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// Sound and video: keep a part of an audio ("cutaudio"), several audios in one MP3 ("joinaudio"), a video small
	/// enough for WhatsApp or e-mail ("smallervideo"). Everything with Windows' own converter, nothing downloaded.
	/// </summary>
	internal static class MediaJobs
	{
		static bool AllFiles(IList<string> f) => f.All(File.Exists);

		/// <summary>
		/// The progress window awaits jobs on its own thread: without this, every step after an await (copying the
		/// sound, waiting for Windows to let go of a file) would run there, freezing the window and its Cancel button.
		/// </summary>
		static Func<JobContext, Task> OffWindow(Func<JobContext, Task> run) => c => Task.Run(() => run(c));

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "joinaudio", Emoji = "🎶",
				Rank = f => f.Count > 1 && AllFiles(f) && f.All(IsAudio) ? 41 : -1,
				Run = OffWindow(JoinAudio),
			});
			Register(new JobDef
			{
				Id = "cutaudio", Emoji = "✂️",
				Rank = f => f.Count == 1 && File.Exists(f[0]) && IsAudio(f[0]) ? 42 : -1,
				Ask = AskCut,
				Run = OffWindow(CutAudio),
			});
			Register(new JobDef
			{
				Id = "smallervideo", Emoji = "📼",
				Rank = f => f.Count > 0 && AllFiles(f) && f.All(IsVideo) ? 38 : -1,
				Run = OffWindow(SmallerVideo),
			});
		}

		// ------------------------------------------------------------------ cut

		/// <summary>Asks the part to keep, showing how long the audio is; the end must be after the start and inside the audio.</summary>
		static Dictionary<string, string> AskCut(IWin32Window owner, IList<string> files)
		{
			// the length is read off the window's thread (Windows' async calls must not wait on it)
			TimeSpan? length;
			try { length = Task.Run(() => AudioWork.Length(files[0])).GetAwaiter().GetResult(); }
			catch (Exception ex)
			{
				// open in another program (or gone): say so now, instead of asking for times that cannot be used
				Ask.Show(owner, T.S("media.cut.title"), Friendly(ex));
				return null;
			}
			TimeSpan? start = null;
			string Problem(string v, bool isEnd)
			{
				// an empty end means "to the end" (the only way when Windows cannot tell the length)
				if (isEnd && string.IsNullOrWhiteSpace(v)) return start == null ? T.S("media.err.time") : null;
				var t = AudioWork.ParseTime(v);
				// the start is remembered even when wrong (null), so the end is never compared with an old start
				if (!isEnd) start = t;
				if (t == null) return T.S("media.err.time");
				return CheckTime(t.Value, isEnd ? start : null, length, isEnd);
			}
			return Ask.Show(owner, T.S("media.cut.title"), T.S("media.cut.intro"),
				new Ask.Field { Kind = Ask.Kind.Info, Label = length is TimeSpan l ? T.S("media.cut.length", AudioWork.FormatTime(l)) : T.S("media.cut.noLength") },
				new Ask.Field { Key = "start", Label = T.S("media.cut.start"), Value = "0:00", Check = v => Problem(v, false) },
				new Ask.Field { Key = "end", Label = T.S("media.cut.end"), Value = length is TimeSpan e ? AudioWork.FormatTime(e) : "", Hint = T.S("media.cut.endEmpty"), Check = v => Problem(v, true) });
		}

		/// <summary>Null when the time is fine; else what is wrong, for the user.</summary>
		static string CheckTime(TimeSpan t, TimeSpan? start, TimeSpan? length, bool isEnd)
		{
			// the length shown is rounded to whole seconds: up to one second more is still "the end"
			if (length is TimeSpan l && (isEnd ? t > l + TimeSpan.FromSeconds(1) : t >= l)) return T.S("media.err.afterEnd", AudioWork.FormatTime(l));
			if (isEnd && start is TimeSpan s && t <= s) return T.S("media.err.order");
			return null;
		}

		static async Task CutAudio(JobContext c)
		{
			string file = c.Files[0];
			c.Report(0, Path.GetFileName(file));
			// null when Windows cannot tell: the cut still works, only the times cannot be checked against it
			TimeSpan? length = await AudioWork.Length(file);
			// the answers may come from the command line too ("--opt:start=0:10"): checked the same way as in the window
			var start = AudioWork.ParseTime(c.Option("start", "0")) ?? throw new InvalidOperationException(T.S("media.err.time"));
			string endText = c.Option("end");
			TimeSpan? end = string.IsNullOrWhiteSpace(endText) ? length : AudioWork.ParseTime(endText) ?? throw new InvalidOperationException(T.S("media.err.time"));
			string problem = CheckTime(start, null, length, false) ?? (end is TimeSpan e ? CheckTime(e, start, length, true) : null);
			if (problem != null) throw new InvalidOperationException(problem);
			if (end > length) end = length;
			string o = OutName(file, ".mp3", T.S("media.name.cut"));
			await AudioWork.Cut(file, o, start, end, length, p => c.Report(p, Path.GetFileName(file)), c.Cancel);
			c.Outputs.Add(o);
			// "to the end" of an audio of unknown length: the end shown is where the kept part really stops
			end ??= start + (await AudioWork.Length(o) ?? TimeSpan.Zero);
			c.Preview = T.S("media.preview.cut", AudioWork.FormatExact(start), AudioWork.FormatExact(end.Value));
		}

		// ------------------------------------------------------------------ join

		static async Task JoinAudio(JobContext c)
		{
			string o = OutName(c.Files[0], ".mp3", T.S("name.joined"));
			// the name is kept taken while the parts are decoded (another job could choose it meanwhile)
			File.WriteAllBytes(o, Array.Empty<byte>());
			try { await AudioWork.Join(c.Files, o, c.Report, c.Cancel); }
			catch (Exception) { try { if (File.Exists(o) && new FileInfo(o).Length == 0) AudioWork.Delete(o); } catch (Exception) { } throw; }
			c.Outputs.Add(o);
			var length = await AudioWork.Length(o);
			c.Preview = length is TimeSpan l ? T.S("media.preview.joined", c.Files.Count, AudioWork.FormatTime(l)) : "";
		}

		// ------------------------------------------------------------------ smaller video

		static async Task SmallerVideo(JobContext c)
		{
			long before = 0, after = 0;
			await c.Each(async (file, i) =>
			{
				string o = OutName(file, ".mp4", T.S("name.smaller"));
				var (b, a) = await VideoWork.Smaller(file, o, p => c.Report((i + p) / c.Files.Count, Path.GetFileName(file)), c.Cancel);
				c.Outputs.Add(o);
				before += b; after += a;
			});
			if (after > 0) c.Preview = VideoWork.Size(before) + " → " + VideoWork.Size(after);
		}
	}
}
