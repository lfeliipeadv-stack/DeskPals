using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace DeskPalsTools
{
	/// <summary>
	/// Sound with Windows' own converter (MediaTranscoder): a part of an audio, several audios in one, the length of an
	/// audio, and times written by people ("1:30", "1:02:03").
	/// </summary>
	internal static class AudioWork
	{
		/// <summary>The format every part is decoded to before joining (CD quality), so the samples can simply follow each other.</summary>
		public const int Rate = 44100, Channels = 2, Bits = 16;
		const int BlockAlign = Channels * Bits / 8;

		// ------------------------------------------------------------------ converting

		/// <summary>
		/// One conversion by Windows. The target is made at once (so its name stays taken) and removed when anything fails
		/// or the user cancels: a half-written file is never left behind.
		/// </summary>
		public static async Task Transcode(string src, string dst, MediaEncodingProfile profile, Action<MediaTranscoder> setup, Action<double> progress, CancellationToken cancel)
		{
			cancel.ThrowIfCancellationRequested();
			File.WriteAllBytes(dst, Array.Empty<byte>());
			try { await TranscodeNow(src, dst, profile, setup, progress, cancel); }
			catch (Exception)
			{
				Release();
				Delete(dst);
				throw;
			}
			Release();
		}

		/// <summary>The converter's own objects are let go at once (they hold memory and threads until collected).</summary>
		static void Release()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		/// <summary>
		/// Deletes a file made here, waiting a little while Windows still holds it (its converter closes files a moment
		/// after it says it is done). True when it is gone.
		/// </summary>
		public static bool Delete(string path)
		{
			for (int i = 0; ; i++)
			{
				try { File.Delete(path); return true; }
				catch (IOException) when (i < 25) { Thread.Sleep(200); }
				catch (UnauthorizedAccessException) when (i < 25) { Thread.Sleep(200); }
				catch (Exception) { return false; }
			}
		}

		/// <summary>
		/// The conversion itself, on streams this method opens and closes: given files, the converter keeps them open long
		/// after it is done (they could not be deleted or renamed). The source stream carries the file's type, so Windows
		/// still knows how to read it.
		/// </summary>
		static async Task TranscodeNow(string src, string dst, MediaEncodingProfile profile, Action<MediaTranscoder> setup, Action<double> progress, CancellationToken cancel)
		{
			using var input = OpenSource(src);
			using var output = new FileStream(dst, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			using var source = input.AsRandomAccessStream();
			using var target = output.AsRandomAccessStream();
			{
				var transcoder = new MediaTranscoder();
				setup?.Invoke(transcoder);
				var prepared = await transcoder.PrepareStreamTranscodeAsync(source, target, profile);
				if (!prepared.CanTranscode)
					throw new InvalidOperationException(prepared.FailureReason == TranscodeFailureReason.CodecNotFound ? T.S("err.codec", Jobs.Ext(src)) : T.S("err.cantConvert", prepared.FailureReason));
				var op = prepared.TranscodeAsync();
				op.Progress = (_, p) => progress?.Invoke(Math.Clamp(p / 100.0, 0, 1));
				using (cancel.Register(() => { try { op.Cancel(); } catch (Exception) { } }))
					await op;
				cancel.ThrowIfCancellationRequested();
			}
		}

		/// <summary>
		/// The user's file, opened to be read. Shared for writing, deleting and renaming too: the user can still move or
		/// delete the original while (and after) it is read. A file another program keeps to itself gets a plain message.
		/// </summary>
		public static FileStream OpenSource(string src)
		{
			try { return new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); }
			catch (IOException ex) when (ex is not FileNotFoundException && ex is not DirectoryNotFoundException && ex is not PathTooLongException)
			{
				throw new InvalidOperationException(T.S("media.err.inUse"), ex);
			}
		}

		/// <summary>Throws the plain "open in another program" message (or the usual ones) when the file cannot be read now.</summary>
		public static void EnsureReadable(string src)
		{
			using (OpenSource(src)) { }
		}

		/// <summary>Only the part from <paramref name="start"/> to <paramref name="end"/> (null: to the end), as an MP3.</summary>
		public static Task Cut(string src, string dst, TimeSpan start, TimeSpan? end, TimeSpan? length, Action<double> progress, CancellationToken cancel)
		{
			// despite its documentation, TrimStopTime works as the point where the output stops (measured on the source),
			// not as how much is left out at the end: the test "1–3 s of a 5 s tone → 2 s" checks it
			return Transcode(src, dst, MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High), t =>
			{
				t.TrimStartTime = start;
				if (end is TimeSpan e && (length == null || e < length)) t.TrimStopTime = e;
			}, progress, cancel);
		}

		/// <summary>Any audio as a plain WAV in the one format of <see cref="Rate"/>/<see cref="Channels"/>/<see cref="Bits"/>.</summary>
		public static Task ToWav(string src, string dst, Action<double> progress, CancellationToken cancel)
		{
			var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
			profile.Audio = AudioEncodingProperties.CreatePcm(Rate, Channels, Bits);
			return Transcode(src, dst, profile, null, progress, cancel);
		}

		/// <summary>Several audios, in this order, as one MP3: each is decoded to the same WAV format, the samples are put one after the other, then made MP3.</summary>
		public static async Task Join(IList<string> files, string dst, Jobs.Progress report, CancellationToken cancel)
		{
			var parts = new List<string>();
			string joined = Jobs.Temp(".wav");
			try
			{
				for (int i = 0; i < files.Count; i++)
				{
					string f = files[i], name = Path.GetFileName(f);
					report(i * 0.7 / files.Count, T.S("media.decoding", name));
					string wav = Jobs.Temp(".wav");
					parts.Add(wav);
					try { await ToWav(f, wav, p => report((i + p) * 0.7 / files.Count, T.S("media.decoding", name)), cancel); }
					catch (OperationCanceledException) { throw; }
					catch (Exception ex) { throw new InvalidOperationException(name + ": " + Jobs.Friendly(ex), ex); }
				}
				// hundreds of MB can be copied here: never on the window's thread, whoever awaits this
				await Task.Run(() => Concat(parts, joined, cancel), cancel);
				report(0.7, T.S("media.encoding"));
				await Transcode(joined, dst, MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High), null, p => report(0.7 + p * 0.3, T.S("media.encoding")), cancel);
			}
			finally
			{
				foreach (var p in parts) Delete(p);
				Delete(joined);
			}
		}

		// ------------------------------------------------------------------ WAV files

		/// <summary>Where the samples of a WAV are (offset, bytes), after checking it has the expected format.</summary>
		public static (long offset, long length, int rate, int channels, int bits) Samples(Stream s)
		{
			var r = new BinaryReader(s);
			s.Position = 0;
			if (s.Length < 12 || new string(r.ReadChars(4)) != "RIFF") throw new InvalidDataException(T.S("media.err.wav"));
			r.ReadUInt32();
			if (new string(r.ReadChars(4)) != "WAVE") throw new InvalidDataException(T.S("media.err.wav"));
			int rate = 0, channels = 0, bits = 0;
			while (s.Position + 8 <= s.Length)
			{
				string id = new string(r.ReadChars(4));
				long size = r.ReadUInt32();
				long body = s.Position;
				if (id == "fmt ")
				{
					r.ReadInt16(); channels = r.ReadInt16(); rate = r.ReadInt32(); r.ReadInt32(); r.ReadInt16(); bits = r.ReadInt16();
				}
				else if (id == "data")
				{
					// a size of 0 or too big means the writer did not fill it in: the samples go to the end of the file
					long left = s.Length - body;
					long length = size == 0 || size > left ? left : size;
					return (body, length - length % Math.Max(1, channels * bits / 8), rate, channels, bits);
				}
				s.Position = body + size + (size & 1);
			}
			throw new InvalidDataException(T.S("media.err.wav"));
		}

		/// <summary>Plain WAVs of the same format, one after the other, in a new WAV.</summary>
		public static void Concat(IList<string> wavs, string dst, CancellationToken cancel)
		{
			var buffer = new byte[1 << 16];
			using var output = File.Create(dst);
			var w = new BinaryWriter(output);
			WriteHeader(w, 0);
			long total = 0;
			foreach (var wav in wavs)
			{
				using var input = File.OpenRead(wav);
				var (offset, length, rate, channels, bits) = Samples(input);
				if (rate != Rate || channels != Channels || bits != Bits) throw new InvalidDataException(T.S("media.err.wav"));
				if (total + length > uint.MaxValue - 36) throw new InvalidOperationException(T.S("media.err.tooLong"));
				input.Position = offset;
				for (long left = length; left > 0;)
				{
					cancel.ThrowIfCancellationRequested();
					int n = input.Read(buffer, 0, (int)Math.Min(buffer.Length, left));
					if (n <= 0) break;
					output.Write(buffer, 0, n);
					left -= n;
					total += n;
				}
			}
			output.Position = 0;
			WriteHeader(w, total);
		}

		static void WriteHeader(BinaryWriter w, long dataBytes)
		{
			w.Write("RIFF"u8.ToArray()); w.Write((uint)(36 + dataBytes)); w.Write("WAVEfmt "u8.ToArray());
			w.Write(16); w.Write((short)1); w.Write((short)Channels); w.Write(Rate); w.Write(Rate * BlockAlign); w.Write((short)BlockAlign); w.Write((short)Bits);
			w.Write("data"u8.ToArray()); w.Write((uint)dataBytes);
			w.Flush();
		}

		/// <summary>How long a WAV made by <see cref="ToWav"/> plays.</summary>
		public static TimeSpan WavLength(string wav)
		{
			using var s = File.OpenRead(wav);
			var (_, length, rate, channels, bits) = Samples(s);
			return TimeSpan.FromSeconds(length / (double)(rate * channels * bits / 8));
		}

		/// <summary>How long an audio (or video) plays, or null when Windows cannot tell. Throws when the file cannot be read now.</summary>
		public static async Task<TimeSpan?> Length(string file)
		{
			// a file another program keeps to itself is told as such, not as "no length"
			EnsureReadable(file);
			StorageFile sf;
			try { sf = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(file)); }
			catch (Exception) { return null; }
			try
			{
				var music = await sf.Properties.GetMusicPropertiesAsync();
				if (music.Duration > TimeSpan.Zero) return music.Duration;
				var video = await sf.Properties.GetVideoPropertiesAsync();
				if (video.Duration > TimeSpan.Zero) return video.Duration;
			}
			catch (Exception) { }
			// some formats have no length in their properties: the editing part of Windows opens the file itself
			try
			{
				var track = await BackgroundAudioTrack.CreateFromFileAsync(sf);
				if (track.OriginalDuration > TimeSpan.Zero) return track.OriginalDuration;
			}
			catch (Exception) { }
			return null;
		}

		// ------------------------------------------------------------------ times written by people

		/// <summary>"90", "1:30", "01:30.5", "1:02:03" → a time; null when it is not one.</summary>
		public static TimeSpan? ParseTime(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return null;
			var parts = text.Trim().Replace(',', '.').Split(':');
			if (parts.Length > 3) return null;
			double seconds = 0;
			for (int i = 0; i < parts.Length; i++)
			{
				bool last = i == parts.Length - 1;
				string p = parts[i].Trim();
				if (p.Length == 0) return null;
				if (!last && !p.All(char.IsAsciiDigit)) return null;
				if (!double.TryParse(p, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double v) || v < 0) return null;
				// minutes and seconds after the first part stay under 60 ("1:75" is a typo, not 2:15)
				if (i > 0 && v >= 60) return null;
				seconds = seconds * 60 + v;
			}
			return seconds > 864000 ? null : TimeSpan.FromSeconds(seconds);
		}

		/// <summary>A time as people write it: "0:05", "3:25", "1:02:03" (whole seconds, half a second rounded up).</summary>
		public static string FormatTime(TimeSpan t)
		{
			int s = (int)Math.Round(t.TotalSeconds, MidpointRounding.AwayFromZero);
			return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60}:{s % 60:00}";
		}

		/// <summary>Like <see cref="FormatTime"/>, with tenths when the time has them ("0:00,4"), so a short part is told as it was cut.</summary>
		public static string FormatExact(TimeSpan t)
		{
			int tenths = (int)Math.Round(t.TotalSeconds * 10, MidpointRounding.AwayFromZero);
			string whole = FormatTime(TimeSpan.FromSeconds(tenths / 10));
			return tenths % 10 == 0 ? whole : whole + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + (tenths % 10);
		}
	}
}
