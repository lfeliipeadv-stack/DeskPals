using NAudio.MediaFoundation;
using NAudio.Wave;

namespace PetEditor
{
	/// <summary>
	/// A tiny sound-effect synthesizer (the one of Tools/PetGen): sine, square, saw and triangle tones, filtered noise,
	/// bells. Nothing is recorded or copied. The sound comes out as a small mono MP3 made by the MP3 encoder that comes with
	/// Windows (Media Foundation), the format DesktopPet plays; where Windows has no encoder (the "N" editions without the
	/// media pack) <see cref="Mp3"/> gives null and the pet is made without sounds. The random generator has a seed, so the
	/// same character always gets the same sounds.
	/// </summary>
	internal sealed class Synth
	{
		public const int Rate = 22050;
		readonly float[] buf;
		readonly Random rnd;

		public Synth(double seconds, int seed = 1)
		{
			buf = new float[(int)(seconds * Rate)];
			rnd = new Random(seed);
		}

		public enum Wave { Sine, Square, Saw, Triangle }

		static double Osc(Wave w, double phase)
		{
			double p = phase - Math.Floor(phase);
			return w switch
			{
				Wave.Square => p < 0.5 ? 0.6 : -0.6,
				Wave.Saw => (2 * p - 1) * 0.7,
				Wave.Triangle => 1 - 4 * Math.Abs(p - 0.5),
				_ => Math.Sin(2 * Math.PI * p),
			};
		}

		/// <summary>A tone gliding from f0 to f1, with an attack/release envelope, vibrato, amplitude wobble and an octave above.</summary>
		public Synth Tone(double at, double dur, double f0, double f1, double vol, Wave w = Wave.Sine,
			double attack = 0.01, double release = 0.08, double vibHz = 0, double vibDepth = 0,
			double amHz = 0, double amDepth = 0, double harmonic = 0)
		{
			int start = (int)(at * Rate), n = (int)(dur * Rate);
			double phase = 0, phase2 = 0;
			for (int i = 0; i < n && start + i < buf.Length; i++)
			{
				double t = i / (double)Rate, k = i / (double)n;
				double f = f0 * Math.Pow(f1 / f0, k);
				if (vibHz > 0) f *= 1 + vibDepth * Math.Sin(2 * Math.PI * vibHz * t);
				phase += f / Rate;
				phase2 += 2 * f / Rate;
				double s = Osc(w, phase) + harmonic * Osc(w, phase2);
				double env = Math.Min(1, t / Math.Max(1e-4, attack)) * Math.Min(1, (dur - t) / Math.Max(1e-4, release));
				if (amHz > 0) env *= 1 - amDepth * (0.5 + 0.5 * Math.Sin(2 * Math.PI * amHz * t));
				if (start + i >= 0) buf[start + i] += (float)(s * env * vol);
			}
			return this;
		}

		/// <summary>A struck bell: a decaying sine with an inharmonic overtone.</summary>
		public Synth Bell(double at, double freq, double vol, double decay = 0.6)
		{
			int start = (int)(at * Rate), n = (int)(decay * 4 * Rate);
			for (int i = 0; i < n && start + i < buf.Length; i++)
			{
				double t = i / (double)Rate;
				double e = Math.Exp(-t / decay) * Math.Min(1, t / 0.003);
				double s = Math.Sin(2 * Math.PI * freq * t) + 0.35 * Math.Sin(2 * Math.PI * freq * 2.76 * t) * Math.Exp(-t / (decay * 0.4));
				buf[start + i] += (float)(s * e * vol);
			}
			return this;
		}

		/// <summary>Filtered noise: the low-pass cutoff glides from c0 to c1 Hz; highPass takes the rumble away.</summary>
		public Synth Noise(double at, double dur, double vol, double c0 = 4000, double c1 = 4000,
			double attack = 0.005, double release = 0.1, double highPass = 0, double amHz = 0, double amDepth = 0)
		{
			int start = (int)(at * Rate), n = (int)(dur * Rate);
			double lp = 0, hpPrev = 0, hpOut = 0;
			for (int i = 0; i < n && start + i < buf.Length; i++)
			{
				double t = i / (double)Rate, k = i / (double)n;
				double cut = c0 * Math.Pow(c1 / c0, k);
				double a = 1 - Math.Exp(-2 * Math.PI * cut / Rate);
				lp += a * ((rnd.NextDouble() * 2 - 1) - lp);
				double s = lp;
				if (highPass > 0)
				{
					double rc = 1 / (2 * Math.PI * highPass), dt = 1.0 / Rate, ah = rc / (rc + dt);
					hpOut = ah * (hpOut + s - hpPrev);
					hpPrev = s;
					s = hpOut;
				}
				double env = Math.Min(1, t / Math.Max(1e-4, attack)) * Math.Min(1, (dur - t) / Math.Max(1e-4, release));
				if (amHz > 0) env *= 1 - amDepth * (0.5 + 0.5 * Math.Sin(2 * Math.PI * amHz * t));
				buf[start + i] += (float)(s * env * vol * 2.2);
			}
			return this;
		}

		/// <summary>A soft low thump (feet, landing).</summary>
		public Synth Thud(double at, double vol, double f0 = 140, double f1 = 45, double dur = 0.22)
		{
			Tone(at, dur, f0, f1, vol, Wave.Sine, 0.002, dur * 0.8);
			return Noise(at, 0.03, vol * 0.25, 1800, 600, 0.001, 0.025);
		}

		public Synth Click(double at, double vol) => Noise(at, 0.012, vol, 7000, 3000, 0.0005, 0.01, 800);

		public Synth Pop(double at, double vol, double f = 700)
		{
			Tone(at, 0.06, f, f * 0.35, vol, Wave.Sine, 0.001, 0.04);
			return Click(at, vol * 0.5);
		}

		/// <summary>Short notes one after the other (MIDI note numbers).</summary>
		public Synth Notes(double at, double step, double vol, Wave w, params int[] midi)
		{
			for (int i = 0; i < midi.Length; i++)
			{
				double f = Midi(midi[i]);
				Tone(at + i * step, step * 0.95, f, f, vol, w, 0.005, step * 0.4);
			}
			return this;
		}

		public static double Midi(int note) => 440 * Math.Pow(2, (note - 69) / 12.0);

		/// <summary>A gentle low-pass over the whole sound (softens square and saw waves).</summary>
		public Synth Soften(double cutoff)
		{
			double a = 1 - Math.Exp(-2 * Math.PI * cutoff / Rate), y = 0;
			for (int i = 0; i < buf.Length; i++) { y += a * (buf[i] - y); buf[i] = (float)y; }
			return this;
		}

		/// <summary>The samples as 16-bit PCM, scaled to a fixed peak, with the silent tail cut and a short fade at both ends.</summary>
		byte[] Pcm(double peak)
		{
			float max = 1e-6f;
			foreach (var s in buf) max = Math.Max(max, Math.Abs(s));
			int last = buf.Length - 1;
			while (last > 0 && Math.Abs(buf[last]) < max * 0.002) last--;
			int n = Math.Min(buf.Length, last + Rate / 50);
			var pcm = new byte[n * 2];
			for (int i = 0; i < n; i++)
			{
				double fade = Math.Min(1, Math.Min(i, n - i) / (Rate * 0.004));
				short v = (short)Math.Round(Math.Max(-1, Math.Min(1, buf[i] / max * peak * fade)) * 32767);
				pcm[2 * i] = (byte)v; pcm[2 * i + 1] = (byte)(v >> 8);
			}
			return pcm;
		}

		static bool? canEncode;

		/// <summary>True when Windows can make MP3 files here (checked once).</summary>
		public static bool CanEncode
		{
			get
			{
				if (canEncode == null)
					try { MediaFoundationApi.Startup(); canEncode = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, new WaveFormat(Rate, 16, 1), 48000) != null; }
					catch { canEncode = false; }
				return canEncode.Value;
			}
		}

		/// <summary>The sound as a small mono MP3 (48 kbit/s), or null when Windows cannot make one.</summary>
		public byte[] Mp3(double peak = 0.7)
		{
			if (!CanEncode) return null;
			string file = Path.Combine(Path.GetTempPath(), "PetEditor-" + Guid.NewGuid().ToString("N") + ".mp3");
			try
			{
				var fmt = new WaveFormat(Rate, 16, 1);
				var type = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, fmt, 48000);
				using (var src = new RawSourceWaveStream(new MemoryStream(Pcm(peak)), fmt))
				using (var enc = new MediaFoundationEncoder(type))
					enc.Encode(file, src);
				return File.ReadAllBytes(file);
			}
			catch { return null; }
			finally { try { File.Delete(file); } catch { } }
		}
	}
}
