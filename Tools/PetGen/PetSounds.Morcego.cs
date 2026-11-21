namespace PetGen;

using W = Sfx.Wave;

public static partial class PetSounds
{
	/// <summary>The Morceguinho bats: high squeaks, soft wing flaps, sonar pings, a BOO, "eca" at the garlic.</summary>
	static List<PetSound> Morcego()
	{
		Sfx Squeak(int seed, int n = 2, double pitch = 1)
		{
			var s = new Sfx(0.18 * n + 0.2, seed);
			for (int i = 0; i < n; i++) s.Tone(i * 0.16, 0.08, 3600 * pitch, 4600 * pitch, 0.35, W.Triangle, 0.004, 0.04, vibHz: 40, vibDepth: 0.04);
			return s;
		}
		var flap = new Sfx(0.6, 501).Noise(0, 0.45, 0.35, 1400, 1000, 0.02, 0.1, highPass: 250, amHz: 11, amDepth: 0.9);
		var sonar = new Sfx(1.2, 502);
		for (int i = 0; i < 3; i++) sonar.Tone(i * 0.28, 0.07, 5200, 4200, 0.3, W.Sine, 0.002, 0.05);
		var boo = new Sfx(1.0, 503).Tone(0, 0.6, 220, 160, 0.5, W.Saw, 0.03, 0.25, vibHz: 6, vibDepth: 0.04, amHz: 20, amDepth: 0.3).Soften(1800);
		var eca = new Sfx(0.8, 504).Tone(0, 0.45, 700, 420, 0.4, W.Saw, 0.01, 0.2, vibHz: 14, vibDepth: 0.06).Soften(2400);
		var gulp = new Sfx(0.6, 505).Tone(0, 0.1, 500, 260, 0.5, W.Sine, 0.005, 0.05).Tone(0.2, 0.1, 460, 240, 0.4, W.Sine, 0.005, 0.05);
		var slurp = new Sfx(1.0, 506).Noise(0, 0.6, 0.3, 2800, 1400, 0.05, 0.2, highPass: 600);
		var keys = Keys(1.4, 507);
		var buzz = new Sfx(0.6, 508).Tone(0, 0.18, 220, 210, 0.4, W.Square, 0.005, 0.03).Tone(0.24, 0.26, 180, 170, 0.4, W.Square, 0.005, 0.08).Soften(1800);
		var horn = new Sfx(1.0, 509).Tone(0, 0.7, 640, 680, 0.35, W.Saw, 0.02, 0.15, vibHz: 6, vibDepth: 0.02, amHz: 30, amDepth: 0.3).Soften(3000);
		var swish = new Sfx(0.9, 510);
		for (int i = 0; i < 3; i++) swish.Noise(i * 0.25, 0.18, 0.4, 3000, 1500, 0.03, 0.1, highPass: 800);
		var hammer = new Sfx(1.2, 511);
		for (int i = 0; i < 4; i++) { hammer.Click(i * 0.3, 0.7); hammer.Bell(i * 0.3, 2400, 0.12, 0.05); }
		var music = Arpeggio(512, W.Triangle, 0.15, 69, 72, 76, 81, 76, 72, 74, 69);
		var spooky = Arpeggio(513, W.Square, 0.18, 57, 60, 63, 66, 63, 60);
		var sob = new Sfx(1.2, 514);
		for (int i = 0; i < 3; i++) sob.Tone(i * 0.32, 0.26, 3000, 2200, 0.25, W.Sine, 0.02, 0.1, vibHz: 12, vibDepth: 0.06);
		var crunch = new Sfx(0.6, 515);
		for (int i = 0; i < 3; i++) crunch.Noise(i * 0.14, 0.07, 0.5, 5000, 1500, 0.001, 0.05, highPass: 500);

		return new List<PetSound>
		{
			new(1, 8, flap),                                // flying
			new(13, 30, flap),                              // fly up
			new(20, 100, sonar),                            // sonar
			new(21, 100, sonar),                            // moth: he hears it
			new(46, 100, gulp),                             // gulp moth
			new(23, 100, Squeak(516, 3, 1.1)),              // bat friends
			new(24, 70, Sparkle(0.6, true, 517)),           // moon
			new(25, 100, Squeak(518, 1, 1.3)),              // spider: scared
			new(26, 100, eca),                              // garlic
			new(27, 100, spooky),                           // pumpkin
			new(28, 100, Chime(519)),                       // candy rain
			new(29, 100, slurp),                            // juice
			new(36, 100, Squeak(520, 3)),                   // squeak
			new(37, 100, boo),                              // boo
			new(38, 100, crunch),                           // favourite food
			new(39, 100, Sneeze(2.2, 521)),                 // sneeze
			new(40, 100, Giggle(2.0, 522)),                 // laugh
			new(43, 100, Hiccup(1.8, 523)),                 // hiccup
			new(35, 100, Whoosh(0.5, 524)),                 // loop the loop
			new(32, 35, Snore(525)),                        // sleep hanging
			new(113, 35, Snore(526)),                       // sleep
			new(61, 100, Ding(527)),                        // idea
			new(64, 100, keys),                             // laptop
			new(66, 100, Chime(528)),                       // download
			new(67, 100, Ding(529, 88)),                    // thumbs up
			new(68, 100, new Sfx(0.6, 530).Pop(0, 0.8, 600)),   // confetti
			new(70, 100, buzz),                             // error
			new(71, 80, sob),                               // cry
			new(72, 100, Squeak(531, 3, 0.8)),              // angry
			new(73, 100, Squeak(532, 1, 1.4)),              // shocked
			new(90, 100, slurp),                            // drink
			new(91, 100, crunch),                           // eat
			new(92, 100, new Sfx(0.6, 533).Click(0, 0.6).Click(0.15, 0.6)),   // cookie
			new(95, 80, Boing(534)),                        // dizzy
			new(100, 100, Squeak(535, 2, 1.2)),             // cheer
			new(101, 100, horn),                            // party
			new(105, 100, Chime(536)),                      // hearts
			new(107, 80, swish),                            // sweep
			new(108, 100, hammer),                          // work
			new(109, 100, music),                           // music
			new(110, 100, slurp),                           // coffee
			new(112, 60, Yawn(2.0, 537)),                   // yawn
		};
	}
}
