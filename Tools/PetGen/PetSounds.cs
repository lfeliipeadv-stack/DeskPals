namespace PetGen;

using W = Sfx.Wave;

/// <summary>
/// Sound effects of the pets made by PetGen. Each one is short and soft (the program also has a
/// volume setting), and is attached to an animation with a chance in percent: frequent animations
/// get a lower chance, so the pet is not noisy.
/// </summary>
public static partial class PetSounds
{
	public static List<PetSound> For(string folder) => folder switch
	{
		"rex" => Rex(),
		"boo" => Boo(),
		"pingo" => Pingo(),
		"mago" => Mago(),
		"nuvi" => Nuvi(),
		"claude" => Claude(),
		"sabio" => Sabio(),
		"arara" or "arara_azul" => Arara(),
		"pipoca" => Pipoca(),
		"sapo" => Sapo(),
		"cacto_vaso" => CactoVaso(),
		"mandacaru" => Mandacaru(),
		"morcego_roxo" or "morcego_vampiro" or "morcego_frutas" => Morcego(),
		_ => new List<PetSound>(),
	};

	/// <summary>Cartoon "meow": a tone that rises and falls, with a nasal overtone.</summary>
	static Sfx Meow(int seed, double pitch = 1)
	{
		var s = new Sfx(0.8, seed);
		s.Tone(0, 0.22, 520 * pitch, 820 * pitch, 0.4, W.Triangle, 0.03, 0.02, harmonic: 0.35);
		s.Tone(0.2, 0.38, 820 * pitch, 480 * pitch, 0.4, W.Triangle, 0.01, 0.2, vibHz: 7, vibDepth: 0.02, harmonic: 0.35);
		return s.Soften(2800);
	}

	static List<PetSound> Sabio()
	{
		var summon = Sparkle(0.5, true, 131);
		summon.Tone(0.55, 0.22, 520, 820, 0.35, W.Triangle, 0.03, 0.02, harmonic: 0.35)
			.Tone(0.75, 0.38, 820, 480, 0.35, W.Triangle, 0.01, 0.2, vibHz: 7, vibDepth: 0.02, harmonic: 0.35);
		var purr = new Sfx(1.4, 132).Noise(0, 1.2, 0.3, 400, 300, 0.2, 0.4, amHz: 24, amDepth: 0.8).Soften(700);
		var hmm = new Sfx(1.0, 133).Tone(0, 0.8, 180, 160, 0.4, W.Triangle, 0.1, 0.3, vibHz: 5, vibDepth: 0.02, harmonic: 0.3).Soften(1500);

		return new List<PetSound>
		{
			new(20, 100, Sparkle(0.6, true, 134)),          // spell
			new(10, 60, Sparkle(0.4, true, 135)),           // sync (spell)
			new(21, 100, Poof(136)),                        // poof out
			new(23, 100, Poof(137)),                        // poof in
			new(28, 50, hmm),                               // think
			new(13, 35, Snore(138)),                        // sleep
			new(31, 100, summon),                           // summon cat
			new(47, 100, Meow(139)),                        // cat appear
			new(41, 30, purr),                              // cat sit
			new(44, 70, Meow(140, 1.15)),                   // cat wake
		};
	}

	// ------------------------------------------------------------------ shared effects

	/// <summary>Cartoon roar: a growling saw wave with breath noise. size 1 = small, 2 = big.</summary>
	static Sfx Roar(double size, int seed)
	{
		double dur = 0.7 + 0.5 * size;
		var s = new Sfx(dur + 0.2, seed);
		double f0 = 300 / size, f1 = 170 / size;
		s.Tone(0, dur, f0, f1, 0.55, W.Saw, 0.06, 0.3, vibHz: 7, vibDepth: 0.04, amHz: 32, amDepth: 0.55, harmonic: 0.4);
		s.Tone(0, dur, f0 * 1.5, f1 * 1.4, 0.18, W.Square, 0.08, 0.3, amHz: 28, amDepth: 0.6);
		s.Noise(0, dur, 0.22, 2400, 900, 0.05, 0.3, highPass: 200, amHz: 30, amDepth: 0.5);
		return s.Soften(2600);
	}

	/// <summary>"Ah… choo!": rising breath, then a noise burst. pitch 1 = normal, 1.5 = small creature.</summary>
	static Sfx Sneeze(double pitch, int seed)
	{
		var s = new Sfx(1.0, seed);
		s.Tone(0, 0.38, 260 * pitch, 420 * pitch, 0.22, W.Triangle, 0.08, 0.06, vibHz: 6, vibDepth: 0.02);
		s.Noise(0, 0.36, 0.08, 1500 * pitch, 2500 * pitch, 0.1, 0.05, highPass: 300);
		s.Noise(0.45, 0.28, 0.6, 7000, 2500, 0.004, 0.2, highPass: 900 * pitch);
		s.Tone(0.45, 0.12, 520 * pitch, 300 * pitch, 0.25, W.Triangle, 0.003, 0.08);
		return s;
	}

	static Sfx Yawn(double pitch, int seed)
	{
		var s = new Sfx(1.3, seed);
		s.Tone(0, 1.1, 330 * pitch, 220 * pitch, 0.4, W.Triangle, 0.25, 0.4, vibHz: 4.5, vibDepth: 0.03);
		s.Noise(0, 1.1, 0.05, 1200, 700, 0.3, 0.4, highPass: 200);
		return s.Soften(2200);
	}

	static Sfx Hiccup(double pitch, int seed)
	{
		var s = new Sfx(0.3, seed);
		s.Tone(0, 0.09, 420 * pitch, 900 * pitch, 0.6, W.Sine, 0.003, 0.04);
		s.Click(0, 0.3);
		return s;
	}

	static Sfx Giggle(double pitch, int seed)
	{
		var s = new Sfx(0.9, seed);
		double[] f = { 880, 820, 900, 780, 840 };
		for (int i = 0; i < f.Length; i++)
			s.Tone(i * 0.13, 0.09, f[i] * pitch, f[i] * 0.8 * pitch, 0.45, W.Triangle, 0.004, 0.05, vibHz: 18, vibDepth: 0.04);
		return s;
	}

	static Sfx Whoosh(double dur, int seed)
	{
		var s = new Sfx(dur + 0.1, seed);
		s.Noise(0, dur, 0.45, 600, 3500, dur * 0.45, dur * 0.5, highPass: 250);
		return s;
	}

	static Sfx Splat(int seed)
	{
		var s = new Sfx(0.5, seed);
		s.Noise(0, 0.32, 0.6, 2200, 400, 0.002, 0.25);
		s.Thud(0, 0.4, 110, 50, 0.18);
		return s;
	}

	static Sfx BubblePop(int seed) => new Sfx(0.2, seed).Pop(0, 0.7, 900);

	static Sfx Ding(int seed, int note = 84) => new Sfx(1.4, seed).Bell(0, Sfx.Midi(note), 0.6, 0.35).Bell(0.12, Sfx.Midi(note + 7), 0.4, 0.3);

	static Sfx Chime(int seed) => new Sfx(1.2, seed).Bell(0, Sfx.Midi(81), 0.45, 0.25).Bell(0.14, Sfx.Midi(85), 0.4, 0.25).Bell(0.28, Sfx.Midi(88), 0.4, 0.35);

	static Sfx Sparkle(double dur, bool up, int seed)
	{
		var s = new Sfx(dur + 0.6, seed);
		var r = new Random(seed);
		int n = (int)(dur * 22);
		for (int i = 0; i < n; i++)
		{
			double k = i / (double)n;
			double f = up ? 1400 + 1800 * k : 3200 - 1800 * k;
			s.Bell(i * dur / n, f * (0.9 + 0.2 * r.NextDouble()), 0.18, 0.12);
		}
		return s;
	}

	static Sfx Poof(int seed)
	{
		var s = new Sfx(0.7, seed);
		s.Noise(0, 0.5, 0.6, 3000, 300, 0.005, 0.4, highPass: 120);
		s.Tone(0, 0.2, 180, 70, 0.3, W.Sine, 0.003, 0.15);
		return s;
	}

	static Sfx Arpeggio(int seed, W wave, double step, params int[] midi) => new Sfx(step * midi.Length + 0.4, seed).Notes(0, step, 0.35, wave, midi).Soften(3500);

	static Sfx Boing(int seed)
	{
		var s = new Sfx(0.7, seed);
		s.Tone(0, 0.55, 180, 520, 0.5, W.Sine, 0.003, 0.25, vibHz: 14, vibDepth: 0.12);
		return s;
	}

	static Sfx Keys(double dur, int seed)
	{
		var s = new Sfx(dur + 0.1, seed);
		var r = new Random(seed);
		for (double t = 0; t < dur; t += 0.06 + r.NextDouble() * 0.08) s.Click(t, 0.35 + 0.25 * r.NextDouble());
		return s;
	}

	static Sfx Snore(int seed)
	{
		var s = new Sfx(1.6, seed);
		s.Noise(0, 1.0, 0.35, 500, 350, 0.35, 0.4, amHz: 22, amDepth: 0.6);
		s.Tone(0, 1.0, 90, 75, 0.15, W.Saw, 0.35, 0.4, amHz: 22, amDepth: 0.6);
		return s.Soften(900);
	}

	// ------------------------------------------------------------------ pets

	static List<PetSound> Rex()
	{
		var drumRoll = new Sfx(1.6, 41);
		for (int i = 0; i < 14; i++) drumRoll.Noise(i * 0.07, 0.06, 0.35 + i * 0.02, 5000, 2500, 0.001, 0.05, highPass: 400).Thud(i * 0.07, 0.15, 180, 120, 0.05);
		drumRoll.Thud(1.0, 0.6, 120, 50, 0.3).Noise(1.0, 0.55, 0.4, 8000, 4000, 0.002, 0.5, highPass: 2000);

		var hero = Whoosh(0.8, 42);
		hero.Notes(0.1, 0.12, 0.25, W.Square, 67, 72, 76, 79).Soften(3000);

		return new List<PetSound>
		{
			new(20, 70, Roar(1, 1)),                        // roar
			new(81, 100, Roar(1.3, 2)),                     // ROAR
			new(90, 100, Roar(1.7, 3)),                     // big roar
			new(85, 100, Sneeze(1, 4)),                     // achoo
			new(19, 60, Yawn(0.9, 5)),                      // yawn
			new(13, 35, Snore(6)),                          // sleep
			new(22, 50, new Sfx(0.5, 7).Thud(0, 0.7, 120, 40, 0.3)),   // stomp
			new(100, 90, new Sfx(0.4, 8).Thud(0, 0.6, 160, 60, 0.15).Click(0, 0.5)),   // kick
			new(79, 80, Whoosh(0.45, 9)),                   // shoot
			new(75, 100, drumRoll),                         // drumming
			new(23, 70, Arpeggio(10, W.Square, 0.09, 72, 76, 79, 84)),   // cheer
			new(53, 100, hero),                             // hero takeoff
			new(30, 60, new Sfx(0.6, 11).Tone(0, 0.12, 300, 160, 0.4, W.Triangle, 0.005, 0.06).Tone(0.2, 0.12, 300, 160, 0.4, W.Triangle, 0.005, 0.06)),   // eat meat (chomp chomp)
			new(91, 100, Chime(12)),                        // in love
			new(97, 100, Ding(13, 79)),                     // flower!
			new(31, 80, Boing(14)),                         // shocked
			// 40 more scenes
			new(250, 100, new Sfx(2.8, 43).Noise(0, 2.6, 0.4, 900, 250, 0.5, 0.7, amHz: 9, amDepth: 0.3)
				.Tone(0, 2.6, 90, 45, 0.25, W.Saw, 0.6, 0.6).Soften(1500)),   // meteor: a rumble that comes closer
			new(253, 100, new Sfx(2.0, 44).Thud(0, 0.9, 110, 28, 0.9).Noise(0, 1.8, 0.6, 2500, 200, 0.003, 1.3).Soften(2600)),   // impact boom
			new(230, 90, new Sfx(0.9, 45).Tone(0, 0.8, 700, 2200, 0.18, W.Sine, 0.02, 0.1).Noise(0, 0.8, 0.12, 6000, 3000, 0.02, 0.2, highPass: 2000)),   // rocket whistle
			new(231, 100, new Sfx(1.3, 46).Thud(0, 0.6, 200, 60, 0.3).Noise(0.02, 1.1, 0.3, 7000, 3000, 0.002, 0.8, highPass: 1500, amHz: 30, amDepth: 0.9)),   // burst and crackle
			new(201, 100, new Sfx(1.0, 47).Noise(0, 0.9, 0.45, 1200, 600, 0.05, 0.4, amHz: 14, amDepth: 0.4).Soften(2500)),   // fire breath
			new(247, 60, new Sfx(0.3, 48).Pop(0, 0.4, 900)),   // hic
			new(235, 80, new Sfx(0.4, 49).Noise(0, 0.25, 0.4, 3000, 800, 0.002, 0.2)),   // snowball splat
			new(133, 70, Chime(50)),                        // shooting star
		};
	}

	static List<PetSound> Boo()
	{
		var boo = new Sfx(1.6, 21);
		boo.Tone(0, 1.3, 330, 230, 0.45, W.Sine, 0.2, 0.5, vibHz: 6, vibDepth: 0.05, harmonic: 0.25);
		boo.Tone(0, 1.3, 495, 350, 0.12, W.Triangle, 0.25, 0.5, vibHz: 6.5, vibDepth: 0.05);
		boo.Soften(2500);

		var drop = new Sfx(0.5, 22).Thud(0, 0.6, 150, 60, 0.25);

		var list = new List<PetSound>
		{
			new(20, 100, boo),                              // boo
			new(21, 80, Giggle(1.15, 23)),                  // peekaboo
			new(22, 100, Giggle(1.25, 24)),                 // giggle
			new(25, 100, Sparkle(0.45, false, 25)),         // vanish
			new(30, 100, Sparkle(0.45, true, 26)),          // appear
			new(24, 70, Chime(27)),                         // hearts
			new(58, 100, Sneeze(1.6, 28)),                  // sneeze
			new(59, 100, Hiccup(1.3, 29)),                  // hiccup
			new(60, 60, Yawn(1.4, 30)),                     // yawn
			new(40, 60, Whoosh(0.5, 31)),                   // take off
			new(78, 100, BubblePop(32)),                    // bubble pop
			new(86, 100, Splat(33)),                        // splat
			new(95, 80, drop),                              // drop pumpkin
			new(91, 90, Ding(34, 88)),                      // kiss heart
			new(92, 90, Sparkle(0.35, true, 35)),           // clone
		};
		list.AddRange(BooPet.ExtraSounds);   // the newer scenes bring their own sounds
		return list;
	}

	static List<PetSound> Pingo()
	{
		var flap = new Sfx(0.7, 51).Noise(0, 0.55, 0.5, 1800, 1500, 0.02, 0.1, highPass: 300, amHz: 14, amDepth: 0.9);

		var squawk = new Sfx(0.5, 52);
		squawk.Tone(0, 0.28, 650, 1050, 0.45, W.Saw, 0.005, 0.1, amHz: 45, amDepth: 0.5).Soften(3000);

		var burp = new Sfx(0.7, 53);
		burp.Tone(0, 0.45, 140, 95, 0.6, W.Saw, 0.02, 0.15, amHz: 26, amDepth: 0.7).Soften(900);

		var clap = new Sfx(0.9, 54);
		for (int i = 0; i < 4; i++) clap.Noise(i * 0.18, 0.05, 0.6, 6000, 3000, 0.001, 0.04, highPass: 900);

		var belly = new Sfx(1.0, 55);
		for (int i = 0; i < 5; i++) belly.Thud(i * 0.15, 0.55, 150, 70, 0.12);

		var trumpet = new Sfx(1.6, 56);
		int[] tune = { 67, 72, 76, 79, 76, 79 };
		double[] len = { 0.14, 0.14, 0.14, 0.3, 0.14, 0.5 };
		double at = 0;
		for (int i = 0; i < tune.Length; i++) { double f = Sfx.Midi(tune[i]); trumpet.Tone(at, len[i], f, f, 0.35, W.Saw, 0.02, 0.06, vibHz: 5.5, vibDepth: 0.01, harmonic: 0.3); at += len[i]; }
		trumpet.Soften(2400);

		var hatch = new Sfx(1.0, 57);
		hatch.Click(0, 0.6).Click(0.18, 0.6).Noise(0.32, 0.08, 0.4, 5000, 2000, 0.001, 0.06, highPass: 700);
		hatch.Tone(0.5, 0.1, 2000, 2400, 0.35, W.Sine, 0.005, 0.05).Tone(0.68, 0.1, 2100, 2500, 0.35, W.Sine, 0.005, 0.05);

		var slide = new Sfx(0.9, 58).Noise(0, 0.8, 0.3, 900, 500, 0.05, 0.3, highPass: 150);
		var sing = Arpeggio(59, W.Triangle, 0.16, 72, 74, 76, 79, 76, 72);

		return new List<PetSound>
		{
			new(22, 80, flap),                              // flap
			new(40, 100, squawk),                           // startled
			new(26, 100, Sneeze(1.4, 60)),                  // sneeze
			new(41, 100, Hiccup(1.1, 61)),                  // hiccup
			new(42, 100, burp),                             // burp
			new(33, 100, Giggle(1.05, 62)),                 // giggle
			new(45, 100, clap),                             // clap
			new(44, 100, belly),                            // belly drum
			new(77, 100, trumpet),                          // trumpet
			new(21, 70, slide),                             // belly slide
			new(31, 60, Yawn(1.2, 63)),                     // yawn
			new(49, 70, sing),                              // sing
			new(85, 100, hatch),                            // chick hatches
			new(112, 100, BubblePop(64)),                   // bubble pop
			new(103, 100, Splat(65)),                       // snow splat
			new(39, 100, Ding(66)),                         // idea
		};
	}

	static List<PetSound> Mago()
	{
		var fireworks = new Sfx(1.8, 71);
		fireworks.Tone(0, 0.55, 500, 1700, 0.25, W.Sine, 0.02, 0.05);
		var r = new Random(71);
		for (int i = 0; i < 8; i++) fireworks.Noise(0.6 + i * 0.09 + r.NextDouble() * 0.05, 0.12, 0.35, 6000, 2500, 0.001, 0.1, highPass: 600);
		fireworks.Thud(0.58, 0.5, 120, 50, 0.3);

		var boom = new Sfx(1.0, 72);
		boom.Noise(0, 0.8, 0.8, 1500, 200, 0.003, 0.6).Thud(0, 0.8, 90, 35, 0.5);

		var gulp = new Sfx(0.6, 73).Tone(0, 0.14, 320, 150, 0.5, W.Sine, 0.005, 0.08).Tone(0.22, 0.14, 300, 130, 0.5, W.Sine, 0.005, 0.08);
		var shrink = new Sfx(0.8, 74).Tone(0, 0.65, 1000, 160, 0.3, W.Square, 0.01, 0.15, vibHz: 12, vibDepth: 0.05).Soften(3000);
		var rainbow = Arpeggio(75, W.Sine, 0.08, 72, 76, 79, 84, 88, 91, 96);

		var list = new List<PetSound>
		{
			new(20, 100, Sparkle(0.6, true, 76)),           // spell
			new(21, 100, Poof(77)),                         // poof out
			new(23, 100, Poof(78)),                         // poof in
			new(30, 100, Ding(79)),                         // aha
			new(36, 100, Sneeze(1.0, 80)),                  // sneeze
			new(45, 60, Yawn(0.85, 81)),                    // yawn
			new(40, 100, fireworks),                        // fireworks
			new(47, 100, boom),                             // backfire
			new(31, 100, gulp),                             // hat swallow
			new(50, 100, shrink),                           // shrink
			new(51, 100, rainbow),                          // rainbow
			new(32, 80, Sparkle(0.35, true, 82)),           // rabbit
			new(35, 80, new Sfx(0.9, 83).Tone(0, 0.12, 400, 700, 0.4, W.Sine, 0.004, 0.06).Tone(0.15, 0.12, 450, 750, 0.4, W.Sine, 0.004, 0.06).Tone(0.3, 0.12, 500, 820, 0.4, W.Sine, 0.004, 0.06)),   // potion (bubbling)
		};
		list.AddRange(MagoPet.ExtraSounds);   // the newer scenes bring their own sounds
		return list;
	}

	static List<PetSound> Nuvi()
	{
		var error = new Sfx(0.7, 91).Tone(0, 0.2, 150, 150, 0.4, W.Square, 0.005, 0.03).Tone(0.27, 0.3, 120, 120, 0.4, W.Square, 0.005, 0.05).Soften(2200);
		var hello = new Sfx(0.6, 92).Tone(0, 0.14, 660, 880, 0.35, W.Square, 0.005, 0.04).Tone(0.18, 0.2, 880, 520, 0.35, W.Square, 0.005, 0.06).Soften(3000);
		var thunder = new Sfx(2.0, 93).Noise(0, 1.7, 0.9, 500, 120, 0.02, 1.0, amHz: 3, amDepth: 0.5).Thud(0, 0.6, 70, 30, 0.6);
		var loading = new Sfx(0.9, 94).Tone(0, 0.08, 880, 880, 0.3, W.Square, 0.003, 0.03).Tone(0.25, 0.08, 880, 880, 0.3, W.Square, 0.003, 0.03).Tone(0.5, 0.08, 1175, 1175, 0.3, W.Square, 0.003, 0.03).Soften(3200);
		var battery = new Sfx(0.9, 95).Tone(0, 0.18, 600, 600, 0.35, W.Square, 0.005, 0.04).Tone(0.3, 0.3, 400, 380, 0.35, W.Square, 0.005, 0.08).Soften(2600);
		var staticNoise = new Sfx(0.8, 96).Noise(0, 0.7, 0.35, 8000, 6000, 0.01, 0.1, highPass: 1500, amHz: 17, amDepth: 0.5);
		var dizzy = new Sfx(1.1, 97).Tone(0, 1.0, 520, 520, 0.35, W.Triangle, 0.05, 0.2, vibHz: 5, vibDepth: 0.25);

		return new List<PetSound>
		{
			new(18, 45, Keys(0.9, 98)),                     // code
			new(21, 60, loading),                           // loading
			new(22, 100, error),                            // error
			new(23, 100, Arpeggio(99, W.Square, 0.08, 72, 76, 79, 84)),   // happy
			new(26, 100, thunder),                          // thunder
			new(30, 80, Arpeggio(100, W.Square, 0.07, 60, 64, 67, 72, 76)),   // download
			new(31, 80, Arpeggio(101, W.Square, 0.07, 76, 72, 67, 64, 60)),   // upload
			new(34, 100, hello),                            // hello
			new(35, 100, staticNoise),                      // static
			new(37, 100, dizzy),                            // dizzy
			new(38, 100, battery),                          // battery low
			new(40, 100, Ding(102)),                        // idea
			new(41, 60, Keys(0.6, 103)),                    // compile
			new(43, 100, new Sfx(1.2, 104).Bell(0, Sfx.Midi(88), 0.5, 0.3).Bell(0.15, Sfx.Midi(93), 0.5, 0.4)),   // commit
		};
	}

	static List<PetSound> Claude()
	{
		var confetti = new Sfx(1.2, 111);
		var r = new Random(111);
		for (int i = 0; i < 6; i++) confetti.Pop(i * 0.07 + r.NextDouble() * 0.03, 0.4, 600 + r.Next(600));
		confetti.Notes(0.45, 0.09, 0.25, W.Square, 72, 76, 79, 84).Soften(3200);

		var fanfare = new Sfx(1.6, 112);
		int[] tune = { 67, 72, 76, 79 };
		double[] len = { 0.15, 0.15, 0.15, 0.6 };
		double at = 0;
		for (int i = 0; i < tune.Length; i++) { double f = Sfx.Midi(tune[i]); fanfare.Tone(at, len[i], f, f, 0.35, W.Saw, 0.02, 0.08, vibHz: 5, vibDepth: 0.01, harmonic: 0.3); at += len[i]; }
		fanfare.Soften(2400);

		var tada = new Sfx(1.4, 113);
		foreach (int n in new[] { 72, 76, 79 }) tada.Tone(0, 0.15, Sfx.Midi(n), Sfx.Midi(n), 0.2, W.Square, 0.005, 0.05);
		foreach (int n in new[] { 72, 76, 79, 84 }) tada.Tone(0.22, 0.8, Sfx.Midi(n), Sfx.Midi(n), 0.2, W.Square, 0.005, 0.4);
		tada.Soften(3000);

		var yelp = new Sfx(0.4, 114).Tone(0, 0.22, 950, 620, 0.45, W.Triangle, 0.004, 0.1);
		var music = Arpeggio(115, W.Triangle, 0.15, 67, 71, 74, 79, 74, 71, 67);
		var gym = new Sfx(0.6, 116).Tone(0, 0.35, 200, 260, 0.35, W.Saw, 0.05, 0.12, amHz: 20, amDepth: 0.4).Soften(1200);

		return new List<PetSound>
		{
			new(17, 100, Boing(117)),                       // surprise
			new(23, 80, yelp),                              // scared
			new(33, 100, fanfare),                          // flag
			new(34, 100, confetti),                         // confetti
			new(36, 50, Keys(1.0, 118)),                    // laptop
			new(37, 100, tada),                             // top hat
			new(39, 80, music),                             // music
			new(44, 100, Ding(119)),                        // idea
			new(35, 60, gym),                               // gym (effort)
			new(18, 60, Chime(120)),                        // flower
		};
	}

	/// <summary>Pitanga the macaw: harsh squawks, a cheerful "oi", a wolf whistle, flaps and samba.</summary>
	static List<PetSound> Arara()
	{
		Sfx Squawk(int seed, double pitch = 1, double dur = 0.35)
		{
			var s = new Sfx(dur + 0.2, seed);
			s.Tone(0, dur, 900 * pitch, 1300 * pitch, 0.5, W.Saw, 0.005, dur * 0.4, vibHz: 9, vibDepth: 0.05, amHz: 60, amDepth: 0.5, harmonic: 0.5);
			s.Noise(0, dur, 0.18, 4000, 2500, 0.005, dur * 0.4, highPass: 900, amHz: 60, amDepth: 0.5);
			return s.Soften(3600);
		}
		// "oi!": a bright voice-like glide up and down, with a little rasp
		var oi = new Sfx(0.6, 151);
		oi.Tone(0, 0.12, 700, 950, 0.45, W.Saw, 0.01, 0.03, harmonic: 0.4).Tone(0.11, 0.24, 1050, 760, 0.45, W.Saw, 0.01, 0.12, vibHz: 8, vibDepth: 0.03, harmonic: 0.4);
		oi.Soften(2600);
		// wolf whistle
		var whistle = new Sfx(1.1, 152);
		whistle.Tone(0, 0.3, 1200, 2400, 0.4, W.Sine, 0.02, 0.05).Tone(0.42, 0.5, 1300, 2600, 0.4, W.Sine, 0.02, 0.05);
		whistle.Tone(0.62, 0.3, 2600, 1100, 0.4, W.Sine, 0.01, 0.12);
		var flap = new Sfx(0.7, 153).Noise(0, 0.55, 0.5, 1800, 1400, 0.02, 0.1, highPass: 300, amHz: 9, amDepth: 0.9);
		var crunch = new Sfx(0.6, 154);
		for (int i = 0; i < 3; i++) crunch.Noise(i * 0.14, 0.07, 0.6, 5000, 1500, 0.001, 0.05, highPass: 500);
		var kick = new Sfx(0.5, 155).Thud(0, 0.7, 180, 60, 0.18);
		kick.Pop(0.02, 0.4, 500);
		var bonk = new Sfx(0.8, 156).Thud(0, 0.7, 240, 80, 0.2);
		bonk.Bell(0.05, 1600, 0.25, 0.15).Bell(0.18, 1900, 0.2, 0.15).Bell(0.3, 2200, 0.15, 0.15);
		var samba = new Sfx(1.6, 157);
		for (int i = 0; i < 8; i++) { samba.Thud(i * 0.18, i % 2 == 0 ? 0.5 : 0.3, 130, 60, 0.12); samba.Noise(i * 0.18 + 0.09, 0.05, 0.25, 7000, 5000, 0.001, 0.04, highPass: 3000); }
		samba.Notes(0, 0.18, 0.25, W.Triangle, 72, 76, 79, 76, 74, 77, 81, 79);
		var pandeiro = new Sfx(1.2, 158);
		for (int i = 0; i < 6; i++) { pandeiro.Noise(i * 0.17, 0.12, 0.4, 9000, 6000, 0.001, 0.1, highPass: 4000); pandeiro.Thud(i * 0.17, i % 2 == 0 ? 0.35 : 0.15, 200, 90, 0.08); }
		var chirp = new Sfx(0.7, 159);
		chirp.Tone(0, 0.08, 2600, 3400, 0.35, W.Sine, 0.005, 0.03).Tone(0.15, 0.08, 2700, 3500, 0.35, W.Sine, 0.005, 0.03);
		var hatch = new Sfx(1.0, 160);
		hatch.Click(0, 0.6).Click(0.18, 0.6).Noise(0.32, 0.08, 0.4, 5000, 2000, 0.001, 0.06, highPass: 700);
		var rain = new Sfx(1.6, 161).Noise(0, 1.4, 0.3, 6000, 5000, 0.2, 0.4, highPass: 1500, amHz: 13, amDepth: 0.4);
		var shake = new Sfx(0.6, 162).Noise(0, 0.45, 0.45, 2500, 2000, 0.01, 0.1, highPass: 400, amHz: 18, amDepth: 0.9);
		var kiss = new Sfx(0.4, 163).Pop(0, 0.5, 1400);
		var laugh = new Sfx(1.0, 164);
		for (int i = 0; i < 5; i++) laugh.Tone(i * 0.15, 0.1, 1100 - i * 40, 800, 0.4, W.Saw, 0.005, 0.05, harmonic: 0.4);
		laugh.Soften(2800);
		var confettiPop = new Sfx(0.9, 191).Pop(0, 0.8, 600);
		confettiPop.Noise(0.03, 0.5, 0.25, 8000, 4000, 0.005, 0.4, highPass: 3000);
		var fanfare = Arpeggio(192, W.Saw, 0.14, 67, 72, 76, 79, 76, 79);
		var buzz = new Sfx(0.6, 193).Tone(0, 0.18, 220, 210, 0.4, W.Square, 0.005, 0.03).Tone(0.24, 0.26, 180, 170, 0.4, W.Square, 0.005, 0.08).Soften(1800);
		var sob = new Sfx(1.2, 194);
		for (int i = 0; i < 3; i++) sob.Tone(i * 0.32, 0.26, 900, 600, 0.35, W.Triangle, 0.02, 0.1, vibHz: 12, vibDepth: 0.06);
		var gulp = new Sfx(0.9, 195);
		for (int i = 0; i < 3; i++) gulp.Tone(i * 0.22, 0.09, 300, 180, 0.5, W.Sine, 0.005, 0.05);
		var horn = new Sfx(1.0, 196).Tone(0, 0.7, 330, 360, 0.4, W.Saw, 0.02, 0.15, vibHz: 6, vibDepth: 0.02, amHz: 30, amDepth: 0.3).Soften(2200);
		var swish = new Sfx(0.9, 197);
		for (int i = 0; i < 3; i++) swish.Noise(i * 0.25, 0.18, 0.4, 3000, 1500, 0.03, 0.1, highPass: 800);
		var hammer = new Sfx(1.2, 198);
		for (int i = 0; i < 4; i++) { hammer.Click(i * 0.3, 0.7); hammer.Bell(i * 0.3, 2400, 0.12, 0.05); }
		var sip = new Sfx(1.0, 199).Noise(0, 0.5, 0.3, 2500, 1200, 0.1, 0.2, highPass: 600);
		sip.Tone(0.6, 0.3, 500, 420, 0.3, W.Triangle, 0.03, 0.15);

		return new List<PetSound>
		{
			new(23, 100, Squawk(165)),                      // squawk
			new(42, 100, Squawk(166, 1.1, 0.45)),           // shout hi
			new(37, 100, Squawk(167, 1.2, 0.2)),            // startled
			new(25, 100, oi),                               // say hi
			new(31, 80, whistle),                           // whistle
			new(20, 70, flap),                              // take off
			new(17, 40, flap),                              // wave
			new(27, 100, crunch),                           // brazil nut
			new(60, 100, kick),                             // kick ball
			new(45, 100, bonk),                             // coconut
			new(47, 100, samba),                            // sing samba
			new(52, 100, samba),                            // carnival
			new(57, 100, pandeiro),                         // pandeiro
			new(64, 100, chirp),                            // chick hatches
			new(51, 100, hatch),                            // egg
			new(49, 100, rain),                             // rain cloud
			new(29, 100, shake),                            // shake
			new(38, 100, shake),                            // lose a feather
			new(48, 100, kiss),                             // blow kiss
			new(56, 100, laugh),                            // laugh
			new(55, 100, Sneeze(1.5, 168)),                 // sneeze
			new(58, 100, Hiccup(1.3, 169)),                 // hiccup
			new(32, 60, Yawn(1.3, 170)),                    // yawn
			new(13, 35, Snore(171)),                        // sleep
			new(61, 100, new Sfx(0.3, 172).Pop(0, 0.6, 800)),   // spit seed
			new(65, 100, Whoosh(0.35, 173)),                // throw nut
			new(62, 80, Whoosh(0.6, 174)),                  // let the kite go
			// the 50 scenes for the program's features
			new(142, 100, Ding(175)),                       // idea
			new(144, 100, Keys(1.6, 176)),                  // laptop
			new(147, 100, Chime(177)),                      // download
			new(150, 100, Ding(178, 88)),                   // thumbs up
			new(193, 100, confettiPop),                     // confetti pop
			new(152, 80, fanfare),                          // flag
			new(153, 100, buzz),                            // error
			new(154, 80, sob),                              // cry
			new(155, 100, Squawk(179, 0.85, 0.4)),          // angry
			new(156, 100, Squawk(180, 1.3, 0.25)),          // shocked
			new(192, 100, Splat(181)),                      // puddle splash
			new(190, 100, Splat(182)),                      // ice cream scoop falls
			new(167, 70, Sparkle(0.6, false, 183)),         // snow cap
			new(169, 100, Squawk(184, 1.25, 0.18)),         // surprised
			new(170, 60, Yawn(1.2, 185)),                   // tired
			new(172, 100, gulp),                            // drink
			new(173, 70, crunch),                           // eat
			new(174, 100, crunch),                          // cookie
			new(176, 80, Boing(186)),                       // dizzy
			new(177, 80, Whoosh(0.5, 187)),                 // spin
			new(179, 100, Ding(188, 81)),                   // ok
			new(180, 100, Giggle(1.4, 189)),                // cheer
			new(181, 100, horn),                            // party (lingua de sogra)
			new(182, 70, flap),                             // jump
			new(185, 100, Chime(190)),                      // hearts
			new(186, 80, swish),                            // sweep
			new(187, 100, hammer),                          // work
			new(188, 100, samba),                           // music
			new(189, 100, sip),                             // coffee
		};
	}

	/// <summary>Pipoca the chick: peeps, a squeaky crow, the hawk's cry, the hen's cluck, popcorn popping, a frog.</summary>
	static List<PetSound> Pipoca()
	{
		Sfx Peep(int seed, int n = 2, double pitch = 1)
		{
			var s = new Sfx(0.25 * n + 0.2, seed);
			for (int i = 0; i < n; i++) s.Tone(i * 0.22, 0.12, 2600 * pitch, 3600 * pitch, 0.4, W.Sine, 0.005, 0.05, vibHz: 30, vibDepth: 0.03);
			return s;
		}
		var crow = new Sfx(1.2, 201);
		crow.Tone(0, 0.15, 1800, 2400, 0.35, W.Triangle, 0.01, 0.03).Tone(0.18, 0.15, 2000, 2600, 0.35, W.Triangle, 0.01, 0.03)
			.Tone(0.36, 0.55, 2400, 3200, 0.4, W.Triangle, 0.02, 0.2, vibHz: 9, vibDepth: 0.05);
		var hawk = new Sfx(1.4, 202);
		hawk.Tone(0, 1.1, 3000, 1900, 0.4, W.Saw, 0.05, 0.4, vibHz: 14, vibDepth: 0.03, harmonic: 0.3);
		hawk.Noise(0, 1.0, 0.12, 6000, 3000, 0.05, 0.4, highPass: 1500);
		hawk.Soften(4000);
		var cluck = new Sfx(1.0, 203);
		for (int i = 0; i < 3; i++) cluck.Tone(i * 0.24, 0.12, 520, 380, 0.45, W.Saw, 0.005, 0.06, harmonic: 0.4);
		cluck.Soften(2200);
		var pop = new Sfx(0.6, 204).Pop(0, 0.9, 500);
		pop.Noise(0, 0.15, 0.4, 6000, 2000, 0.001, 0.12, highPass: 600);
		var croak = new Sfx(0.9, 205);
		for (int i = 0; i < 2; i++) croak.Tone(i * 0.35, 0.22, 160, 120, 0.5, W.Saw, 0.01, 0.06, amHz: 40, amDepth: 0.7);
		croak.Soften(1400);
		var gulp = new Sfx(0.6, 206).Tone(0, 0.1, 300, 180, 0.5, W.Sine, 0.005, 0.05).Tone(0.18, 0.1, 280, 170, 0.45, W.Sine, 0.005, 0.05);
		var peck = new Sfx(0.9, 207);
		for (int i = 0; i < 4; i++) peck.Click(i * 0.2, 0.6);
		var stomp = new Sfx(0.9, 208);
		for (int i = 0; i < 4; i++) stomp.Thud(i * 0.2, 0.5, 200, 90, 0.08);
		var shiver = new Sfx(1.2, 209);
		for (int i = 0; i < 6; i++) shiver.Tone(i * 0.16, 0.1, 2400, 2100, 0.25, W.Sine, 0.005, 0.04, vibHz: 40, vibDepth: 0.05);
		var blow = Whoosh(0.6, 210);
		var phew = new Sfx(0.9, 211).Noise(0, 0.6, 0.35, 1500, 700, 0.05, 0.3, highPass: 200);
		phew.Tone(0.05, 0.5, 1400, 900, 0.2, W.Sine, 0.05, 0.2);
		var keys = Keys(1.4, 212);
		var buzz = new Sfx(0.6, 213).Tone(0, 0.18, 220, 210, 0.4, W.Square, 0.005, 0.03).Tone(0.24, 0.26, 180, 170, 0.4, W.Square, 0.005, 0.08).Soften(1800);
		var horn = new Sfx(1.0, 214).Tone(0, 0.7, 520, 560, 0.4, W.Saw, 0.02, 0.15, vibHz: 6, vibDepth: 0.02, amHz: 30, amDepth: 0.3).Soften(2600);
		var swish = new Sfx(0.9, 215);
		for (int i = 0; i < 3; i++) swish.Noise(i * 0.25, 0.18, 0.4, 3000, 1500, 0.03, 0.1, highPass: 800);
		var hammer = new Sfx(1.2, 216);
		for (int i = 0; i < 4; i++) { hammer.Click(i * 0.3, 0.7); hammer.Bell(i * 0.3, 2400, 0.12, 0.05); }
		var music = Arpeggio(217, W.Triangle, 0.16, 76, 79, 84, 79, 76, 72, 74, 76);
		var sip = new Sfx(1.0, 218).Noise(0, 0.5, 0.3, 2500, 1200, 0.1, 0.2, highPass: 600);
		var splash = Splat(219);
		var sob = new Sfx(1.2, 220);
		for (int i = 0; i < 3; i++) sob.Tone(i * 0.32, 0.26, 2200, 1600, 0.3, W.Sine, 0.02, 0.1, vibHz: 12, vibDepth: 0.06);

		return new List<PetSound>
		{
			new(21, 100, Peep(221)),                        // peep
			new(46, 100, crow),                             // crow ("piu!")
			new(22, 100, Peep(222, 3, 1.15)),               // spot worm
			new(24, 100, gulp),                             // eat worm
			new(204, 100, hawk),                            // hawk arrives
			new(209, 100, hawk),                            // hawk gives up
			new(25, 80, Peep(223, 1, 0.9)),                 // hawk: worried
			new(26, 60, shiver),                            // hide in shell
			new(28, 100, phew),                             // phew
			new(31, 100, pop),                              // popcorn
			new(214, 100, cluck),                           // the mother hen
			new(34, 100, Peep(224, 2, 1.1)),                // bye mom
			new(35, 70, Peep(225, 3, 1.2)),                 // chase butterfly
			new(219, 100, croak),                           // the frog
			new(38, 100, Peep(226, 1, 1.3)),                // frog scare
			new(39, 100, blow),                             // dandelion
			new(41, 100, Whoosh(0.4, 227)),                 // shake
			new(42, 100, stomp),                            // stomp
			new(20, 60, peck),                              // peck
			new(86, 60, peck),                              // eat
			new(51, 100, Sneeze(2.0, 228)),                 // sneeze
			new(52, 100, Giggle(1.8, 229)),                 // laugh
			new(13, 35, Snore(230)),                        // sleep
			new(61, 100, Ding(231)),                        // idea
			new(64, 100, keys),                             // laptop
			new(66, 100, Chime(232)),                       // download
			new(67, 100, Ding(233, 88)),                    // thumbs up
			new(68, 100, pop),                              // confetti
			new(69, 100, buzz),                             // error
			new(70, 80, sob),                               // cry
			new(71, 100, Peep(234, 3, 0.8)),                // angry
			new(72, 100, Peep(235, 1, 1.4)),                // shocked
			new(73, 100, Peep(236, 3, 1.3)),                // scared
			new(77, 100, splash),                           // splash
			new(85, 100, gulp),                             // drink
			new(87, 100, new Sfx(0.6, 237).Click(0, 0.6).Click(0.15, 0.6)),   // cookie
			new(90, 80, Boing(238)),                        // dizzy
			new(94, 100, Giggle(1.8, 239)),                 // cheer
			new(95, 100, horn),                             // party
			new(100, 100, Chime(240)),                      // hearts
			new(102, 80, swish),                            // sweep
			new(103, 100, hammer),                          // work
			new(104, 100, music),                           // music
			new(105, 100, sip),                             // coffee
			new(106, 60, Yawn(1.8, 241)),                   // yawn
			new(30, 100, Peep(242, 2, 1.2)),                // mother hen: he sees her
		};
	}

	/// <summary>Cururu the frog: croaks, the "thwip" of the tongue, gulps, a bee sting, bubbles.</summary>
	static List<PetSound> Sapo()
	{
		Sfx Croak(int seed, double pitch = 1, int n = 2)
		{
			var s = new Sfx(0.4 * n + 0.3, seed);
			for (int i = 0; i < n; i++) s.Tone(i * 0.38, 0.26, 150 * pitch, 115 * pitch, 0.55, W.Saw, 0.01, 0.07, amHz: 42, amDepth: 0.75, harmonic: 0.3);
			return s.Soften(1500);
		}
		var thwip = new Sfx(0.5, 251).Tone(0, 0.12, 400, 1600, 0.4, W.Sine, 0.003, 0.04);
		thwip.Noise(0, 0.1, 0.2, 5000, 2000, 0.002, 0.06, highPass: 1000);
		var gulp = new Sfx(1.0, 252).Tone(0, 0.12, 320, 160, 0.55, W.Sine, 0.005, 0.06).Tone(0.3, 0.1, 280, 150, 0.4, W.Sine, 0.005, 0.05);
		gulp.Noise(0.55, 0.25, 0.2, 2500, 1200, 0.02, 0.15, highPass: 500);   // the lick
		var ouch = new Sfx(1.0, 253).Tone(0, 0.5, 900, 300, 0.45, W.Saw, 0.01, 0.2, vibHz: 18, vibDepth: 0.08).Soften(2600);
		var hop = new Sfx(0.4, 254).Thud(0, 0.35, 160, 70, 0.12);
		var bubbles = new Sfx(1.2, 255);
		for (int i = 0; i < 4; i++) bubbles.Pop(i * 0.25, 0.4, 700 + i * 120);
		var rain = new Sfx(1.6, 256).Noise(0, 1.4, 0.3, 6000, 5000, 0.2, 0.4, highPass: 1500, amHz: 13, amDepth: 0.4);
		var keys = Keys(1.4, 257);
		var buzz = new Sfx(0.6, 258).Tone(0, 0.18, 220, 210, 0.4, W.Square, 0.005, 0.03).Tone(0.24, 0.26, 180, 170, 0.4, W.Square, 0.005, 0.08).Soften(1800);
		var horn = new Sfx(1.0, 259).Tone(0, 0.7, 330, 360, 0.4, W.Saw, 0.02, 0.15, vibHz: 6, vibDepth: 0.02, amHz: 30, amDepth: 0.3).Soften(2200);
		var swish = new Sfx(0.9, 260);
		for (int i = 0; i < 3; i++) swish.Noise(i * 0.25, 0.18, 0.4, 3000, 1500, 0.03, 0.1, highPass: 800);
		var hammer = new Sfx(1.2, 261);
		for (int i = 0; i < 4; i++) { hammer.Click(i * 0.3, 0.7); hammer.Bell(i * 0.3, 2400, 0.12, 0.05); }
		var music = Arpeggio(262, W.Triangle, 0.16, 60, 64, 67, 72, 67, 64, 62, 60);
		var sip = new Sfx(1.0, 263).Noise(0, 0.5, 0.3, 2500, 1200, 0.1, 0.2, highPass: 600);
		var sob = new Sfx(1.2, 264);
		for (int i = 0; i < 3; i++) sob.Tone(i * 0.32, 0.26, 500, 350, 0.35, W.Triangle, 0.02, 0.1, vibHz: 10, vibDepth: 0.06);

		return new List<PetSound>
		{
			new(28, 100, Croak(265)),                       // croak
			new(29, 100, Croak(266, 1, 3)),                 // frog chorus
			new(43, 100, Croak(267, 1.3, 1)),               // missed
			new(40, 100, gulp),                             // gulp (after every catch)
			new(19, 100, thwip),                            // catch visitor
			new(42, 100, ouch),                             // bee sting
			new(41, 100, Sparkle(0.6, true, 268)),          // firefly glow
			new(1, 5, hop),                                 // hopping (every hop: a low chance)
			new(18, 100, Boing(269)),                       // long jump
			new(102, 100, Boing(270)),                      // jump
			new(30, 100, bubbles),                          // bubbles
			new(31, 100, Chime(271)),                       // frog prince
			new(32, 100, rain),                             // rain dance
			new(86, 100, rain),                             // rain cloud
			new(33, 80, Croak(272, 1.6, 2)),                // tadpoles
			new(34, 100, sip),                              // lick eye
			new(35, 60, Yawn(0.8, 273)),                    // yawn
			new(37, 100, Sneeze(0.9, 274)),                 // sneeze
			new(38, 100, Giggle(0.9, 275)),                 // laugh
			new(13, 35, Snore(276)),                        // sleep
			new(61, 100, Ding(277)),                        // idea
			new(64, 100, keys),                             // laptop
			new(66, 100, Chime(278)),                       // download
			new(67, 100, Ding(279, 88)),                    // thumbs up
			new(68, 100, new Sfx(0.6, 280).Pop(0, 0.8, 600)),   // confetti
			new(69, 100, buzz),                             // error
			new(70, 80, sob),                               // cry
			new(71, 100, Croak(281, 0.8, 3)),               // angry
			new(72, 100, Croak(282, 1.5, 1)),               // shocked
			new(77, 100, Splat(283)),                       // splash
			new(81, 100, sip),                              // ice cream
			new(96, 80, Boing(284)),                        // dizzy
			new(100, 100, Croak(285, 1.2, 2)),              // cheer
			new(101, 100, horn),                            // party
			new(105, 100, Chime(286)),                      // hearts
			new(107, 80, swish),                            // sweep
			new(108, 100, hammer),                          // work
			new(109, 100, music),                           // music
			new(110, 100, sip),                             // coffee
		};
	}
}
