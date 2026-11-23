using W = PetEditor.Synth.Wave;

namespace PetEditor
{
	/// <summary>
	/// The sounds of a character from <see cref="CharacterMaker"/>: a voice of its kind (a quack, a meow, a bark, a growl, a
	/// squeak, a blip, a tweet, a little roar), higher for a small pet and lower for a big one, and the sounds of what it
	/// does (a hop, a landing, snoring, a melody to dance, a page, a crunch, a sip, the rain…). Each sound is made by
	/// <see cref="Synth"/>, so nothing is recorded or copied, and the same character always gets the same sounds.
	/// </summary>
	internal static class CharacterSounds
	{
		/// <summary>Moods of the voice.</summary>
		public enum Mood { Normal, Happy, Sad, Surprised, Angry }

		/// <summary>How high the voice is: a small pet sounds higher.</summary>
		static double Pitch(CharacterMaker.Look k) => k.Scale switch { 1 => 1.15, 2 => 1.0, _ => 0.86 };

		/// <summary>The voice of the kind, in a mood.</summary>
		public static Synth Voice(CharacterMaker.Kind kind, double p, Mood mood, int seed)
		{
			double up = mood switch { Mood.Happy => 1.14, Mood.Sad => 0.84, Mood.Surprised => 1.3, Mood.Angry => 0.8, _ => 1 };
			double len = mood switch { Mood.Sad => 1.5, Mood.Surprised => 0.75, Mood.Angry => 1.3, _ => 1 };
			p *= up;
			// a happy pet says it twice, a sad one glides down, an angry one growls
			int times = mood == Mood.Happy ? 2 : 1;
			double glide = mood == Mood.Sad ? 0.7 : mood == Mood.Surprised ? 1.35 : 1;
			double growl = mood == Mood.Angry ? 0.55 : 0;
			var s = new Synth(1.6 * len + 0.3, seed);
			for (int i = 0; i < times; i++)
			{
				double at = i * 0.22 * len;
				switch (kind)
				{
					case CharacterMaker.Kind.Duck:
						s.Tone(at, 0.17 * len, 560 * p, 430 * p * glide, 0.42, W.Saw, 0.006, 0.07, amHz: 38, amDepth: 0.3 + growl * 0.5, harmonic: 0.5);
						s.Noise(at, 0.14 * len, 0.06, 2600, 1500, 0.005, 0.08, highPass: 600);
						break;
					case CharacterMaker.Kind.Cat:
						s.Tone(at, 0.2 * len, 520 * p, 820 * p, 0.4, W.Triangle, 0.03, 0.02, harmonic: 0.35);
						s.Tone(at + 0.19 * len, 0.34 * len, 820 * p, 480 * p * glide, 0.4, W.Triangle, 0.01, 0.2, vibHz: 7, vibDepth: 0.02, amHz: growl > 0 ? 30 : 0, amDepth: growl, harmonic: 0.35);
						break;
					case CharacterMaker.Kind.Dog:
						s.Noise(at, 0.1 * len, 0.32, 1900, 700, 0.004, 0.07, highPass: 250);
						s.Tone(at, 0.15 * len, 340 * p, 220 * p * glide, 0.5, W.Saw, 0.004, 0.08, amHz: growl > 0 ? 32 : 0, amDepth: growl, harmonic: 0.4);
						break;
					case CharacterMaker.Kind.Bear:
						s.Tone(at, 0.36 * len, 170 * p, 128 * p * glide, 0.55, W.Saw, 0.04, 0.16, amHz: 30, amDepth: 0.45 + growl * 0.4, harmonic: 0.4);
						s.Noise(at, 0.3 * len, 0.1, 900, 500, 0.04, 0.14, highPass: 120);
						break;
					case CharacterMaker.Kind.Bunny:
						s.Tone(at, 0.09 * len, 1300 * p, 1700 * p * glide, 0.38, W.Sine, 0.004, 0.04, vibHz: 22, vibDepth: 0.03);
						s.Tone(at + 0.12 * len, 0.08 * len, 1500 * p, 1200 * p * glide, 0.3, W.Sine, 0.004, 0.04);
						break;
					case CharacterMaker.Kind.Bird:
						for (int c = 0; c < 3; c++)
							s.Tone(at + c * 0.085 * len, 0.065, 2500 * p * (c == 2 ? glide : 1), 3500 * p * (c == 2 ? glide : 1), 0.32, W.Sine, 0.003, 0.03, amHz: growl > 0 ? 40 : 0, amDepth: growl);
						break;
					case CharacterMaker.Kind.Dragon:
						s.Tone(at, 0.42 * len, 260 * p, 170 * p * glide, 0.5, W.Saw, 0.03, 0.18, vibHz: 7, vibDepth: 0.04, amHz: 30, amDepth: 0.5, harmonic: 0.4);
						s.Noise(at, 0.4 * len, 0.16, 2200, 900, 0.03, 0.18, highPass: 200, amHz: 28, amDepth: 0.5);
						break;
					default:   // the blob: a bubbly blip
						s.Tone(at, 0.14 * len, 480 * p, 920 * p * glide, 0.42, W.Triangle, 0.004, 0.06, vibHz: 12, vibDepth: 0.05, amHz: growl > 0 ? 26 : 0, amDepth: growl);
						break;
				}
			}
			return s.Soften(kind is CharacterMaker.Kind.Bird or CharacterMaker.Kind.Bunny ? 6000 : 3000);
		}

		static Synth Giggle(double p, int seed)
		{
			var s = new Synth(0.9, seed);
			double[] f = { 880, 820, 900, 780, 840 };
			for (int i = 0; i < f.Length; i++)
				s.Tone(i * 0.13, 0.09, f[i] * p, f[i] * 0.8 * p, 0.45, W.Triangle, 0.004, 0.05, vibHz: 18, vibDepth: 0.04);
			return s;
		}

		static Synth Yawn(double p, int seed) =>
			new Synth(1.3, seed).Tone(0, 1.1, 330 * p, 220 * p, 0.4, W.Triangle, 0.25, 0.4, vibHz: 4.5, vibDepth: 0.03)
				.Noise(0, 1.1, 0.05, 1200, 700, 0.3, 0.4, highPass: 200).Soften(2200);

		static Synth Snore(int seed) =>
			new Synth(1.6, seed).Noise(0, 1.0, 0.35, 500, 350, 0.35, 0.4, amHz: 22, amDepth: 0.6)
				.Tone(0, 1.0, 90, 75, 0.15, W.Saw, 0.35, 0.4, amHz: 22, amDepth: 0.6).Soften(900);

		static Synth Sparkle(double dur, bool up, int seed)
		{
			var s = new Synth(dur + 0.6, seed);
			var r = new Random(seed);
			int n = (int)(dur * 22);
			for (int i = 0; i < n; i++)
			{
				double k = i / (double)n, f = up ? 1400 + 1800 * k : 3200 - 1800 * k;
				s.Bell(i * dur / n, f * (0.9 + 0.2 * r.NextDouble()), 0.18, 0.12);
			}
			return s;
		}

		static Synth Melody(int seed, W wave, double step, params int[] midi) => new Synth(step * midi.Length + 0.4, seed).Notes(0, step, 0.35, wave, midi).Soften(3500);

		/// <summary>Every sound of the character: the animation it plays with, the chance (percent) and the MP3 (null when Windows cannot make MP3s).</summary>
		public static IEnumerable<(string anim, int probability, byte[] mp3)> For(CharacterMaker.Look k)
		{
			if (!Synth.CanEncode) yield break;
			double p = Pitch(k);
			var kind = k.Kind;
			int seed = 1000 + (int)kind * 100;
			(string, int, byte[]) S(string anim, int prob, Synth s) => (anim, prob, s.Mp3());

			yield return S("talk", 35, Voice(kind, p, Mood.Normal, seed + 1));
			yield return S("happy", 60, Voice(kind, p, Mood.Happy, seed + 2));
			yield return S("wave", 70, Voice(kind, p, Mood.Happy, seed + 3));
			yield return S("drag", 50, Voice(kind, p, Mood.Surprised, seed + 4));
			yield return S("surprised", 100, Voice(kind, p, Mood.Surprised, seed + 5).Pop(0, 0.4, 900));
			yield return S("scared", 80, Voice(kind, p * 1.1, Mood.Surprised, seed + 6));
			yield return S("cry", 80, Voice(kind, p, Mood.Sad, seed + 7));
			yield return S("angry", 80, Voice(kind, p, Mood.Angry, seed + 8));
			yield return S("love", 60, Giggle(p, seed + 9));
			yield return S("laugh", 100, Giggle(p * 1.05, seed + 10));
			yield return S("jump", 60, new Synth(0.4, seed + 11).Tone(0, 0.2, 300 * p, 760 * p, 0.42, W.Sine, 0.004, 0.08));
			yield return S("fall soft", 50, new Synth(0.4, seed + 12).Thud(0, 0.6, 150 / p, 50 / p, 0.2));
			yield return S("sleep", 50, Snore(seed + 13));
			yield return S("yawn", 100, Yawn(p, seed + 14));
			yield return S("dance", 100, Melody(seed + 15, W.Square, 0.14, 72, 76, 79, 76, 74, 77, 81, 77));
			yield return S("music", 100, Melody(seed + 16, W.Triangle, 0.2, 67, 71, 74, 72, 71, 69, 67));
			yield return S("cheer", 100, Melody(seed + 17, W.Square, 0.09, 72, 76, 79, 84).Pop(0.42, 0.35, 1100));
			yield return S("party", 100, Sparkle(0.5, true, seed + 18).Pop(0, 0.5, 800).Pop(0.12, 0.4, 1000));
			yield return S("think", 50, new Synth(1.0, seed + 19).Tone(0, 0.8, 180 * p, 160 * p, 0.4, W.Triangle, 0.1, 0.3, vibHz: 5, vibDepth: 0.02, harmonic: 0.3).Soften(1500));
			yield return S("idea", 100, new Synth(1.4, seed + 20).Bell(0, Synth.Midi(84), 0.6, 0.35).Bell(0.12, Synth.Midi(91), 0.4, 0.3));
			yield return S("dizzy", 80, Sparkle(0.6, false, seed + 21));
			yield return S("read", 60, new Synth(0.4, seed + 22).Noise(0, 0.2, 0.4, 5000, 2000, 0.03, 0.12, highPass: 1200));
			// a crunch: bites of noise with clicks
			var crunch = new Synth(0.8, seed + 23);
			for (int i = 0; i < 3; i++) crunch.Noise(i * 0.2, 0.09, 0.5, 4200, 1600, 0.002, 0.06, highPass: 700).Click(i * 0.2, 0.4);
			yield return S("eat", 100, crunch);
			yield return S("coffee", 80, new Synth(1.0, seed + 24).Noise(0, 0.35, 0.25, 900, 2200, 0.05, 0.15, highPass: 400, amHz: 9, amDepth: 0.6)
				.Tone(0.45, 0.4, 300 * p, 240 * p, 0.25, W.Triangle, 0.05, 0.25));
			// rain: soft noise and drops
			var rain = new Synth(1.4, seed + 25).Noise(0, 1.3, 0.22, 3000, 3000, 0.2, 0.4, highPass: 900);
			var rr = new Random(seed);
			for (int i = 0; i < 9; i++) rain.Pop(0.1 + rr.NextDouble() * 1.1, 0.18, 1400 + rr.Next(900));
			yield return S("umbrella", 100, rain);
			// chattering teeth and a "brrr"
			var cold = new Synth(0.9, seed + 26).Tone(0, 0.7, 210 * p, 190 * p, 0.3, W.Triangle, 0.02, 0.2, amHz: 26, amDepth: 0.8);
			for (double t = 0; t < 0.7; t += 0.06) cold.Click(t, 0.3);
			yield return S("cold", 80, cold);
			yield return S("hot", 60, new Synth(0.8, seed + 27).Noise(0, 0.6, 0.3, 700, 1600, 0.1, 0.3, highPass: 250).Tone(0, 0.5, 260 * p, 200 * p, 0.18, W.Triangle, 0.1, 0.3));
			if (CharacterMaker.Flies(kind))
			{
				var flap = new Synth(0.7, seed + 28);
				for (int i = 0; i < 3; i++) flap.Noise(i * 0.18, 0.12, 0.4, 700, 2500, 0.04, 0.07, highPass: 200);
				yield return S("fly", 60, flap);
			}
			if (kind == CharacterMaker.Kind.Dragon)
				yield return S("fire", 100, Voice(kind, p * 0.9, Mood.Angry, seed + 29).Noise(0.1, 0.7, 0.35, 900, 3000, 0.1, 0.3, highPass: 300));
		}

		/// <summary>The voice alone, to listen to in the character window.</summary>
		public static byte[] Sample(CharacterMaker.Look k) => Voice(k.Kind, Pitch(k), Mood.Happy, 7).Mp3();
	}
}
