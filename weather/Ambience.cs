using Microsoft.Win32;
using NAudio.Wave;

namespace DeskPalsWeather
{
	/// <summary>
	/// The sound of the weather, made here (nothing recorded): rain (filtered noise), wind for the
	/// snow and the leaves, a light breeze for the blossom. It loops, fades in and out, follows the volume chosen, and
	/// fades away while a program uses the microphone (a call), without opening it.
	/// </summary>
	internal static class Ambience
	{
		const int Rate = 22050;
		static WaveOutEvent output;
		static Loop loop;
		static Kind? playing;
		static float target, now;
		static System.Windows.Forms.Timer fader;

		/// <summary>Play the sound of this kind (null = silence) at this volume (0..1).</summary>
		public static void Set(Kind? kind, double volume)
		{
			target = kind == null ? 0 : (float)Math.Clamp(volume, 0, 1);
			if (kind != null && kind != playing)
			{
				Stop();
				loop = new Loop(Make(kind.Value));
				output = new WaveOutEvent { DesiredLatency = 200 };
				try { output.Init(loop); output.Volume = 0; output.Play(); playing = kind; now = 0; }
				catch (Exception) { Stop(); return; }
			}
			if (fader == null) { fader = new System.Windows.Forms.Timer { Interval = 50 }; fader.Tick += (s, e) => Fade(); }
			fader.Start();
		}

		static void Fade()
		{
			float goal = target * (CallNow ? 0 : 1);
			now += Math.Clamp(goal - now, -0.02f, 0.02f);
			if (output != null) try { output.Volume = Math.Clamp(now, 0, 1); } catch (Exception) { }
			if (goal == 0 && now <= 0.001f) { Stop(); fader.Stop(); }
		}

		static void Stop()
		{
			try { output?.Stop(); output?.Dispose(); } catch (Exception) { }
			output = null; loop = null; playing = null;
		}

		public static bool QuietOnCalls = true;
		static DateTime callChecked;
		static bool call;

		/// <summary>A program is using the microphone now (Windows keeps it in the privacy settings; the microphone is not opened).</summary>
		static bool CallNow
		{
			get
			{
				if (!QuietOnCalls) return false;
				if ((DateTime.Now - callChecked).TotalSeconds < 2) return call;
				callChecked = DateTime.Now;
				call = false;
				try
				{
					using var root = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone");
					if (root != null) call = InUse(root);
				}
				catch (Exception) { }
				return call;
			}
		}

		static bool InUse(RegistryKey key)
		{
			foreach (var name in key.GetSubKeyNames())
			{
				using var sub = key.OpenSubKey(name);
				if (sub == null) continue;
				if (sub.GetValue("LastUsedTimeStart") is long start && start > 0 && sub.GetValue("LastUsedTimeStop") is long stop && stop == 0) return true;
				if (name == "NonPackaged" && InUse(sub)) return true;
			}
			return false;
		}

		// ------------------------------------------------------------------ the sounds

		/// <summary>Eight seconds that loop without a seam (the end fades into the start).</summary>
		static float[] Make(Kind kind)
		{
			int n = Rate * 8;
			var s = new float[n];
			var r = new Random(11);
			double lp = 0, lp2 = 0, hp = 0, prev = 0;
			for (int i = 0; i < n; i++)
			{
				double t = i / (double)Rate, white = r.NextDouble() * 2 - 1;
				switch (kind)
				{
					case Kind.Rain:
						// the steady hiss of rain (only the rain: no drops dripping, which sounds like water running)
						lp += 0.35 * (white - lp);
						hp = 0.97 * (hp + lp - prev); prev = lp;
						s[i] = (float)(hp * 0.55);
						break;
					default:
						// wind: low noise rising and falling slowly
						double cut = kind == Kind.Blossom ? 0.02 : 0.035;
						lp += cut * (white - lp);
						lp2 += cut * (lp - lp2);
						double swell = 0.55 + 0.45 * Math.Sin(2 * Math.PI * t / 8 * (kind == Kind.Snow ? 2 : 1)) * Math.Sin(2 * Math.PI * t / 4 + 1);
						s[i] = (float)(lp2 * 6 * swell * (kind == Kind.Blossom ? 0.5 : 1));
						break;
				}
			}
			// no seam: the last half second fades into the first
			int x = Rate / 2;
			for (int i = 0; i < x; i++) { float k = i / (float)x; s[i] = s[i] * k + s[n - x + i] * (1 - k); }
			Array.Resize(ref s, n - x);
			float max = s.Max(v => Math.Abs(v));
			for (int i = 0; i < s.Length; i++) s[i] = s[i] / Math.Max(1e-6f, max) * 0.6f;
			return s;
		}

		sealed class Loop : IWaveProvider
		{
			readonly float[] data;
			int pos;
			public WaveFormat WaveFormat { get; } = new WaveFormat(Rate, 16, 1);
			public Loop(float[] d) { data = d; }
			public int Read(byte[] buffer, int offset, int count)
			{
				for (int i = 0; i + 1 < count; i += 2)
				{
					short v = (short)(data[pos] * 32767);
					buffer[offset + i] = (byte)v; buffer[offset + i + 1] = (byte)(v >> 8);
					pos = (pos + 1) % data.Length;
				}
				return count;
			}
		}
	}
}
