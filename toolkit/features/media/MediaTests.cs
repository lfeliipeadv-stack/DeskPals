using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace DeskPalsTools
{
	/// <summary>The tests of the sound and video jobs, on tones and a short video made here.</summary>
	internal static class MediaTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("media times", Times);
			DevTests.Add("media plan", Plan);
			DevTests.Add("media cutaudio", Cut);
			DevTests.Add("media joinaudio", Join);
			DevTests.Add("media smallervideo", Smaller);
			DevTests.Add("media texts", Texts);
		}

		static Task Times(DevTests.Ctx t)
		{
			t.Check("times: 1:30 is 90 s", AudioWork.ParseTime("1:30") == TimeSpan.FromSeconds(90));
			t.Check("times: 1:02:03", AudioWork.ParseTime("1:02:03") == new TimeSpan(1, 2, 3));
			t.Check("times: plain seconds and a decimal comma", AudioWork.ParseTime("75") == TimeSpan.FromSeconds(75) && AudioWork.ParseTime("0:01,5") == TimeSpan.FromSeconds(1.5));
			t.Check("times: nonsense is refused", AudioWork.ParseTime("abc") == null && AudioWork.ParseTime("1:75") == null && AudioWork.ParseTime("") == null && AudioWork.ParseTime("-1") == null && AudioWork.ParseTime("1::2") == null);
			t.Check("times: written back", AudioWork.FormatTime(TimeSpan.FromSeconds(205)) == "3:25" && AudioWork.FormatTime(new TimeSpan(1, 2, 3)) == "1:02:03" && AudioWork.FormatTime(TimeSpan.FromSeconds(5)) == "0:05");
			t.Check("times: half a second is rounded up, not to even", AudioWork.FormatTime(TimeSpan.FromSeconds(0.5)) == "0:01" && AudioWork.FormatTime(TimeSpan.FromSeconds(2.5)) == "0:03");
			string sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			t.Check("times: a part of a second is shown in the bubble", AudioWork.FormatExact(TimeSpan.FromSeconds(0.4)) == "0:00" + sep + "4" && AudioWork.FormatExact(TimeSpan.FromSeconds(61.4)) == "1:01" + sep + "4" && AudioWork.FormatExact(TimeSpan.FromSeconds(3)) == "0:03", AudioWork.FormatExact(TimeSpan.FromSeconds(0.4)));
			t.Check("video size: 1920×1080 → 1280×720", VideoWork.Fit(1920, 1080) == (1280u, 720u));
			t.Check("video size: a standing phone video keeps its shape", VideoWork.Fit(1080, 1920) == (720u, 1280u), VideoWork.Fit(1080, 1920).ToString());
			t.Check("video size: a small video is not made bigger", VideoWork.Fit(640, 360) == (640u, 360u));
			t.Check("video size: odd sizes become even", VideoWork.Fit(1001, 563) is var (w, h) && w % 2 == 0 && h % 2 == 0, VideoWork.Fit(1001, 563).ToString());
			t.Check("video size: written for the bubble", VideoWork.Size(45L * 1024 * 1024) == "45 MB" && VideoWork.Size(800 * 1024) == "800 KB");
			return Task.CompletedTask;
		}

		static Task Plan(DevTests.Ctx t)
		{
			string[] Ids(params string[] names)
			{
				var paths = names.Select(n => Path.Combine(t.Dir, n)).ToList();
				foreach (var f in paths) if (!File.Exists(f)) File.WriteAllText(f, "");
				return Jobs.Plan(paths).Select(j => j.Id).ToArray();
			}
			t.Check("plan: one audio → cut", Ids("a.mp3").Contains("cutaudio") && !Ids("a.mp3").Contains("joinaudio"), string.Join(",", Ids("a.mp3")));
			t.Check("plan: several audios → join", Ids("a.mp3", "b.ogg").Contains("joinaudio") && !Ids("a.mp3", "b.ogg").Contains("cutaudio"), string.Join(",", Ids("a.mp3", "b.ogg")));
			t.Check("plan: videos → smaller", Ids("v.mp4").Contains("smallervideo") && Ids("v.mp4", "w.mov").Contains("smallervideo"));
			t.Check("plan: a video with a PDF → no video job", !Ids("v.mp4", "x.pdf").Contains("smallervideo"));
			t.Check("plan: a picture → no sound job", !Ids("p.png").Any(j => j is "cutaudio" or "joinaudio" or "smallervideo"));
			return Task.CompletedTask;
		}

		static async Task Cut(DevTests.Ctx t)
		{
			string tone = Tone(Path.Combine(t.Dir, "five.wav"), 5, 440, 22050, 1);
			var length = await AudioWork.Length(tone);
			t.Check("cut: the length of the audio is read", length is TimeSpan l && Math.Abs(l.TotalSeconds - 5) < 0.2, length?.ToString());
			var r = await Jobs.Run("cutaudio", new[] { tone }, null, default, new() { ["start"] = "0:01", ["end"] = "0:03" });
			t.Check("cut: 1–3 s of a 5 s tone → an MP3", r.ok && r.outputs.Count == 1 && Jobs.Ext(r.outputs[0]) == ".mp3", r.message);
			if (r.ok)
			{
				double s = await Seconds(r.outputs[0]);
				t.Check("cut: it lasts about 2 s", Math.Abs(s - 2) < 0.3, s.ToString("0.000") + " s");
				t.Info("cut: " + r.preview);
			}
			// only a start given: up to the end of the audio
			r = await Jobs.Run("cutaudio", new[] { tone }, null, default, new() { ["start"] = "0:03,5" });
			double rest = r.ok ? await Seconds(r.outputs[0]) : 0;
			t.Check("cut: from 3.5 s to the end lasts about 1.5 s", r.ok && Math.Abs(rest - 1.5) < 0.3, r.ok ? rest.ToString("0.000") + " s" : r.message);
			var watch = System.Diagnostics.Stopwatch.StartNew();
			t.Check("cut: a file just made can be deleted", r.outputs.All(AudioWork.Delete));
			t.Info("cut: deleted after " + watch.ElapsedMilliseconds + " ms");
			r = await Jobs.Run("cutaudio", new[] { tone }, null, default, new() { ["start"] = "0:03", ["end"] = "0:02" });
			t.Check("cut: an end before the start is refused", !r.ok && r.message == T.S("media.err.order"), r.message);
			r = await Jobs.Run("cutaudio", new[] { tone }, null, default, new() { ["start"] = "0:01", ["end"] = "0:09" });
			t.Check("cut: an end after the audio is refused", !r.ok && r.message.Length > 0, r.message);
			r = await Jobs.Run("cutaudio", new[] { tone }, null, default, new() { ["start"] = "banana" });
			t.Check("cut: a wrong time is refused", !r.ok && r.message == T.S("media.err.time"), r.message);
			t.Check("cut: nothing left behind by the refused ones", Directory.GetFiles(t.Dir, "*.mp3").Length == 1, string.Join(", ", Directory.GetFiles(t.Dir).Select(Path.GetFileName)));
			// canceled before it starts: no half file
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			r = await Jobs.Run("cutaudio", new[] { tone }, null, cts.Token, new() { ["start"] = "0:01", ["end"] = "0:03" });
			t.Check("cut: canceled leaves no file", !r.ok && Directory.GetFiles(t.Dir, "*.mp3").Length == 1, r.message);
			// a file another program keeps to itself: told plainly, not as "no length"
			using (new FileStream(tone, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				r = await Jobs.Run("cutaudio", new[] { tone }, null, default, new() { ["start"] = "0:01", ["end"] = "0:03" });
				t.Check("cut: a file open in another program is told so", !r.ok && r.message.Contains(T.S("media.err.inUse")), r.message);
			}
			t.Check("cut: and leaves no file", Directory.GetFiles(t.Dir, "*.mp3").Length == 1);
		}

		static async Task Join(DevTests.Ctx t)
		{
			// three tones in different formats: they are all decoded to one format before joining
			string a = Tone(Path.Combine(t.Dir, "a.wav"), 1, 330, 22050, 1);
			string b = Tone(Path.Combine(t.Dir, "b.wav"), 2, 523, 44100, 2);
			string c = Tone(Path.Combine(t.Dir, "c.wav"), 1.5, 660, 48000, 1);
			var r = await Jobs.Run("joinaudio", new[] { a, b, c }, null);
			t.Check("join: three tones → one MP3", r.ok && r.outputs.Count == 1 && Jobs.Ext(r.outputs[0]) == ".mp3", r.message);
			if (r.ok)
			{
				double s = await Seconds(r.outputs[0]);
				t.Check("join: it lasts the sum (4.5 s)", Math.Abs(s - 4.5) < 0.35, s.ToString("0.000") + " s");
				t.Info("join: " + r.preview);
			}
			string broken = Path.Combine(t.Dir, "broken.mp3");
			File.WriteAllText(broken, "not a sound");
			r = await Jobs.Run("joinaudio", new[] { a, broken }, null);
			t.Check("join: a broken file says which one, nothing left behind", !r.ok && r.message.Contains("broken.mp3") && Directory.GetFiles(t.Dir, "*.mp3").Length == 2, r.message);
			using (new FileStream(b, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				r = await Jobs.Run("joinaudio", new[] { a, b }, null);
				t.Check("join: a file open in another program is named, in plain words", !r.ok && r.message.Contains("b.wav: " + T.S("media.err.inUse")), r.message);
			}
			t.Check("join: and leaves nothing behind", Directory.GetFiles(t.Dir, "*.mp3").Length == 2);
			await WindowStaysAwake(t);
		}

		/// <summary>
		/// Joined as the progress window does it (awaited on a window's thread): the window keeps answering, so the
		/// user can still click Cancel while long audios are put together.
		/// </summary>
		static async Task WindowStaysAwake(DevTests.Ctx t)
		{
			var tones = Enumerable.Range(0, 3).Select(i => Tone(Path.Combine(t.Dir, "long" + i + ".wav"), 90, 300 + i * 100, 44100, 2)).ToArray();
			var done = new TaskCompletionSource<(Jobs.Result r, long gap)>(TaskCreationOptions.RunContinuationsAsynchronously);
			var thread = new Thread(() =>
			{
				using var form = new Form { ShowInTaskbar = false, Opacity = 0, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000), Size = new Size(10, 10) };
				using var timer = new System.Windows.Forms.Timer { Interval = 20 };
				var watch = System.Diagnostics.Stopwatch.StartNew();
				long last = 0, gap = 0;
				timer.Tick += (_, _) => { long now = watch.ElapsedMilliseconds; gap = Math.Max(gap, now - last); last = now; };
				form.Shown += async (_, _) =>
				{
					last = watch.ElapsedMilliseconds;
					timer.Start();
					Jobs.Result r;
					try { r = await Jobs.Run("joinaudio", tones, null); }
					catch (Exception ex) { r = new Jobs.Result { message = ex.Message }; }
					timer.Stop();
					done.TrySetResult((r, gap));
					form.Close();
				};
				Application.Run(form);
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();
			var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromMinutes(3)));
			if (finished != done.Task) { t.Check("join: from a window, it finishes", false, "still running after 3 minutes"); return; }
			var (res, longest) = done.Task.Result;
			t.Check("join: from a window, 3 × 90 s → one MP3", res.ok, res.message);
			t.Check("join: the window keeps answering meanwhile", longest < 1500, "longest pause " + longest + " ms");
			foreach (var o in res.outputs) AudioWork.Delete(o);
		}

		static async Task Smaller(DevTests.Ctx t)
		{
			string video = Path.Combine(t.Dir, "clip.mp4");
			try { await MakeVideo(t.Dir, video); }
			catch (Exception ex)
			{
				t.Skip("smallervideo: this Windows could not make a test video (" + ex.Message.Trim() + ")");
				return;
			}
			t.Info("smallervideo: test video " + VideoWork.Size(new FileInfo(video).Length));
			var r = await Jobs.Run("smallervideo", new[] { video }, null);
			if (!r.ok && r.message.Contains(T.S("err.codec", ".mp4"))) { t.Skip("smallervideo: no H.264 encoder here"); return; }
			t.Check("smallervideo: a smaller MP4", r.ok && r.outputs.Count == 1 && new FileInfo(r.outputs[0]).Length < new FileInfo(video).Length, r.message);
			if (!r.ok) return;
			var info = await MediaEncodingProfile.CreateFromFileAsync(await StorageFile.GetFileFromPathAsync(r.outputs[0]));
			uint w = info.Video?.Width ?? 0, h = info.Video?.Height ?? 0;
			t.Check("smallervideo: at most 720p, same shape (16:9)", w == 1280 && h == 720, w + "×" + h);
			t.Check("smallervideo: the sound is kept (AAC)", info.Audio != null && info.Audio.Subtype == "AAC", info.Audio?.Subtype ?? "no sound");
			t.Check("smallervideo: the bubble says before → after", r.preview.Contains(" → ") && (r.preview.Contains("MB") || r.preview.Contains("KB")), r.preview);
			t.Info("smallervideo: " + r.preview);

			// the smaller copy again: already small, so the new copy is not kept
			string small = r.outputs[0];
			int before = Directory.GetFiles(t.Dir, "*.mp4").Length;
			r = await Jobs.Run("smallervideo", new[] { small }, null);
			bool kept = r.ok && r.outputs.All(File.Exists);
			t.Check("smallervideo: an already small video is either smaller or told, never a bigger copy", kept ? new FileInfo(r.outputs[0]).Length < new FileInfo(small).Length : r.message.Contains(T.S("media.err.notSmaller")) && Directory.GetFiles(t.Dir, "*.mp4").Length == before, r.message);

			// not a video at all: told so, not "install something from the Store"
			string fake = Path.Combine(t.Dir, "fake.mp4");
			File.WriteAllText(fake, "this is not a video");
			before = Directory.GetFiles(t.Dir, "*.mp4").Length;
			r = await Jobs.Run("smallervideo", new[] { fake }, null);
			t.Check("smallervideo: a damaged video is told as damaged", !r.ok && r.message.Contains(T.S("media.err.badVideo")) && Directory.GetFiles(t.Dir, "*.mp4").Length == before, r.message);
			using (new FileStream(video, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				r = await Jobs.Run("smallervideo", new[] { video }, null);
				t.Check("smallervideo: a video open in another program is told so", !r.ok && r.message.Contains(T.S("media.err.inUse")) && Directory.GetFiles(t.Dir, "*.mp4").Length == before, r.message);
			}
		}

		static Task Texts(DevTests.Ctx t)
		{
			// every language that has this feature's texts has all of them (the core test checks the whole set)
			var en = MediaKeys("en");
			foreach (var code in T.Codes().Where(c => c != "en"))
			{
				var other = MediaKeys(code);
				var missing = en.Where(k => !other.Contains(k)).ToList();
				t.Check("texts: " + code + " has every media text", missing.Count == 0, string.Join(", ", missing.Take(8)));
			}
			return Task.CompletedTask;
		}

		static HashSet<string> MediaKeys(string code)
		{
			var asm = typeof(MediaTests).Assembly;
			var keys = new HashSet<string>();
			using var s = asm.GetManifestResourceStream("DeskPalsTools.lang." + code + ".media.txt");
			if (s == null) return keys;
			using var r = new StreamReader(s);
			string line;
			while ((line = r.ReadLine()) != null)
				if (line.Length > 0 && line[0] != '#' && line.IndexOf('=') > 0) keys.Add(line[..line.IndexOf('=')].Trim());
			return keys;
		}

		// ------------------------------------------------------------------ files made here

		/// <summary>A tone (16-bit PCM WAV) of any length, pitch, rate and channels.</summary>
		internal static string Tone(string path, double seconds, double hz, int rate, int channels)
		{
			int n = (int)(rate * seconds), bytes = n * channels * 2;
			using var w = new BinaryWriter(File.Create(path));
			w.Write("RIFF"u8.ToArray()); w.Write(36 + bytes); w.Write("WAVEfmt "u8.ToArray()); w.Write(16); w.Write((short)1); w.Write((short)channels);
			w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16); w.Write("data"u8.ToArray()); w.Write(bytes);
			for (int i = 0; i < n; i++)
			{
				short v = (short)(Math.Sin(2 * Math.PI * hz * i / rate) * 8000);
				for (int ch = 0; ch < channels; ch++) w.Write(v);
			}
			return path;
		}

		/// <summary>How long an audio really plays: decoded to plain samples and counted.</summary>
		static async Task<double> Seconds(string file)
		{
			string wav = Jobs.Temp(".wav");
			try
			{
				await AudioWork.ToWav(file, wav, null, default);
				return AudioWork.WavLength(wav).TotalSeconds;
			}
			finally { AudioWork.Delete(wav); }
		}

		/// <summary>
		/// A 2-second 1080p MP4 with sound, at a high bitrate: changing coloured noise (so the encoder cannot make it tiny)
		/// and a tone, rendered by Windows' video editor.
		/// </summary>
		static async Task MakeVideo(string dir, string path)
		{
			var comp = new MediaComposition();
			var rnd = new Random(7);
			for (int i = 0; i < 12; i++)
			{
				string png = Path.Combine(dir, "noise" + i + ".png");
				using (var bmp = new Bitmap(480, 270))
				{
					using (var g = Graphics.FromImage(bmp))
						for (int y = 0; y < 270; y += 6)
							for (int x = 0; x < 480; x += 6)
							{
								using var brush = new SolidBrush(Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
								g.FillRectangle(brush, x, y, 6, 6);
							}
					bmp.Save(png, ImageFormat.Png);
				}
				comp.Clips.Add(await MediaClip.CreateFromImageFileAsync(await StorageFile.GetFileFromPathAsync(png), TimeSpan.FromSeconds(1 / 6.0)));
			}
			string tone = Tone(Path.Combine(dir, "sound.wav"), 2, 440, 44100, 2);
			comp.BackgroundAudioTracks.Add(await BackgroundAudioTrack.CreateFromFileAsync(await StorageFile.GetFileFromPathAsync(tone)));
			var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
			profile.Video.Width = 1920; profile.Video.Height = 1080;
			profile.Video.Bitrate = 12_000_000;
			File.WriteAllBytes(path, Array.Empty<byte>());
			var result = await comp.RenderToFileAsync(await StorageFile.GetFileFromPathAsync(path), MediaTrimmingPreference.Precise, profile);
			if (result != Windows.Media.Transcoding.TranscodeFailureReason.None) throw new InvalidOperationException(result.ToString());
			if (new FileInfo(path).Length < 10_000) throw new InvalidOperationException("empty video");
		}
	}
}
