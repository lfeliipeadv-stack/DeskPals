using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace DeskPalsTools
{
	/// <summary>
	/// Audio (or the sound of a video) made ready to send: a small mono MP3 made by Windows' own converter, cut in pieces
	/// of some minutes so each one stays well below the 20 MB the services take. The pieces are temporary files.
	/// </summary>
	internal static class AiAudio
	{
		/// <summary>Speech needs little: 16 kHz mono at 32 kbit/s is about 14 MB an hour.</summary>
		const uint Rate = 16000, Bitrate = 32000;
		/// <summary>Pieces of at most 10 minutes (quicker answers, no wait running out) and never near 20 MB.</summary>
		const int MaxSeconds = 600;
		const long MaxBytes = 19L * 1024 * 1024;

		/// <summary>The tests make the pieces tiny, to see the cutting and joining work on a short sound.</summary>
		public static int? ChunkSecondsForTests;
		/// <summary>The tests make Windows' converter "fail", as it does on Ogg Opus voice notes on some PCs.</summary>
		public static bool FailForTests;

		/// <summary>The MP3 pieces of the file, in order (deleted by who asked for them).</summary>
		/// <param name="starts">When given, gets where each piece starts in the file, in seconds (for subtitles).</param>
		public static async Task<List<string>> Pieces(string src, Action<double> progress, CancellationToken cancel, List<double> starts = null)
		{
			string full = Jobs.Temp(".mp3");
			var pieces = new List<string>();
			try
			{
				await Transcode(src, full, null, null, p => progress?.Invoke(p * 0.6), cancel);
				long size = new FileInfo(full).Length;
				var duration = await Duration(full, size);
				int seconds = ChunkSecondsForTests ?? MaxSeconds;
				// a converter that gave more than expected: shorter pieces, so each stays below the limit
				if (duration.TotalSeconds > 0) seconds = (int)Math.Max(1, Math.Min(seconds, MaxBytes * duration.TotalSeconds / Math.Max(1, size)));
				if (duration.TotalSeconds <= seconds && size < MaxBytes) { pieces.Add(full); starts?.Add(0); full = null; return pieces; }
				// the pieces are cut from the original: a file used as a source stays held by Windows for a while
				Delete(new[] { full });
				full = null;
				int count = (int)Math.Ceiling(duration.TotalSeconds / seconds);
				for (int i = 0; i < count; i++)
				{
					cancel.ThrowIfCancellationRequested();
					string piece = Jobs.Temp(".mp3");
					pieces.Add(piece);
					starts?.Add(i * (double)seconds);
					var start = TimeSpan.FromSeconds(i * (double)seconds);
					var stop = TimeSpan.FromSeconds(Math.Min(duration.TotalSeconds, (i + 1) * (double)seconds));
					await Transcode(src, piece, start, stop, null, cancel);
					progress?.Invoke(0.6 + 0.4 * (i + 1) / count);
				}
				return pieces;
			}
			catch (Exception)
			{
				Delete(pieces);
				throw;
			}
			finally { if (full != null) Delete(new[] { full }); }
		}

		/// <summary>
		/// Removes the temporary pieces. Windows' converter lets go of a file only when its objects are collected, so a
		/// file still held is tried again a few times.
		/// </summary>
		public static void Delete(IEnumerable<string> files)
		{
			var left = files.Where(File.Exists).ToList();
			for (int attempt = 0; left.Count > 0 && attempt < 10; attempt++)
			{
				if (attempt > 0) { GC.Collect(); GC.WaitForPendingFinalizers(); Thread.Sleep(100 * attempt); }
				left.RemoveAll(f => { try { File.Delete(f); return true; } catch (Exception) { return false; } });
			}
		}

		/// <summary>The length of an MP3 (Windows reads it; else from its size, the bitrate being fixed).</summary>
		static async Task<TimeSpan> Duration(string mp3, long size)
		{
			try
			{
				var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(mp3));
				var d = (await file.Properties.GetMusicPropertiesAsync()).Duration;
				if (d > TimeSpan.Zero) return d;
			}
			catch (Exception) { }
			return TimeSpan.FromSeconds(size * 8.0 / Bitrate);
		}

		/// <summary>A small mono MP3 of the file, or of a part of it (from <paramref name="start"/> to <paramref name="stop"/>).</summary>
		static async Task Transcode(string src, string dst, TimeSpan? start, TimeSpan? stop, Action<double> progress, CancellationToken cancel)
		{
			var source = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(src));
			File.WriteAllBytes(dst, Array.Empty<byte>());
			var target = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(dst));
			var transcoder = new MediaTranscoder();
			if (start != null) transcoder.TrimStartTime = start.Value;
			// TrimStopTime is where it stops (from the start of the file, like TrimStartTime)
			if (stop != null) transcoder.TrimStopTime = stop.Value;
			// the converter cannot be tested on a file Windows reads: the tests make it fail on purpose
			if (FailForTests) throw new InvalidOperationException(T.S("ai.cantHear"));
			try
			{
				var prepared = await transcoder.PrepareFileTranscodeAsync(source, target, Profile());
				if (!prepared.CanTranscode)
					throw new InvalidOperationException(prepared.FailureReason == TranscodeFailureReason.CodecNotFound ? T.S("err.codec", Jobs.Ext(src)) : T.S("ai.cantHear"));
				var op = prepared.TranscodeAsync();
				op.Progress = (_, p) => progress?.Invoke(p / 100.0);
				using (cancel.Register(() => op.Cancel()))
					await op;
			}
			catch (OperationCanceledException) { throw; }
			catch (InvalidOperationException) { throw; }
			// Windows' converter fails with bare system errors: said in words
			catch (Exception) when (!cancel.IsCancellationRequested) { throw new InvalidOperationException(T.S("ai.cantHear")); }
		}

		/// <summary>
		/// The formats the speech services take as they are. When Windows cannot convert a file (an Ogg Opus WhatsApp voice
		/// note, often), a small one is sent untouched instead.
		/// </summary>
		static readonly string[] Accepted = { ".ogg", ".opus", ".oga", ".m4a", ".mp3", ".mp4", ".mpeg", ".mpga", ".wav", ".webm", ".flac" };

		public static bool CanSendAsIs(string file) => Accepted.Contains(Jobs.Ext(file)) && File.Exists(file) && new FileInfo(file).Length is > 0 and < MaxBytes;

		static MediaEncodingProfile Profile()
		{
			var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Low);
			profile.Audio = AudioEncodingProperties.CreateMp3(Rate, 1, Bitrate);
			return profile;
		}
	}
}
