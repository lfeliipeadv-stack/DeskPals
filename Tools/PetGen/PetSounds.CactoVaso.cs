namespace PetGen;

using W = Sfx.Wave;

public static partial class PetSounds
{
	/// <summary>
	/// Xiquinho, the cactus in a pot: little clay "tok" steps, water pouring, a flower blooming, a bee, a balloon that pops
	/// on his spines, a sneeze, a tumbleweed in the wind, a bird that gets pricked, soap bubbles... Sounds of scenes where
	/// something happens in the middle (the pop, the sneeze, the prick) are timed: the effect starts with the animation
	/// and the event comes after a pause, at the step where it happens.
	/// </summary>
	static List<PetSound> CactoVaso()
	{
		// a small clay "tok" (the pot on the ground)
		Sfx Tok(int seed, double vol = 0.5) => new Sfx(0.3, seed).Thud(0, vol, 260, 140, 0.07).Click(0, vol * 0.4);
		// a tiny high voice (he is small): a short glide
		Sfx Voice(int seed, double f0, double f1, double dur = 0.18) => new Sfx(dur + 0.25, seed).Tone(0, dur, f0, f1, 0.45, W.Triangle, 0.01, dur * 0.5, vibHz: 8, vibDepth: 0.03).Soften(3200);

		var water = new Sfx(3.0, 301);
		water.Noise(1.2, 1.7, 0.35, 5000, 4000, 0.15, 0.3, highPass: 1500, amHz: 11, amDepth: 0.5);
		for (int i = 0; i < 6; i++) water.Pop(1.3 + i * 0.27, 0.2, 900 + i * 60);

		var bloom = new Sfx(2.6, 302);
		bloom.Tone(0, 1.2, 200, 260, 0.25, W.Triangle, 0.1, 0.2, amHz: 18, amDepth: 0.4);   // straining "nnngh"
		bloom.Pop(1.75, 0.5, 700);
		bloom.Notes(1.9, 0.09, 0.25, W.Sine, 79, 83, 86, 91);
		var sparkleUp = Sparkle(0.5, true, 303);

		var buzz = new Sfx(3.0, 304).Tone(0, 2.8, 220, 240, 0.25, W.Saw, 0.3, 0.6, vibHz: 7, vibDepth: 0.04, amHz: 90, amDepth: 0.3).Soften(1800);

		// balloon: a squeaky rub, then POP when it touches a spine (step 30 at 0.13 s)
		var balloon = new Sfx(4.6, 305);
		balloon.Tone(2.7, 0.5, 900, 1300, 0.12, W.Triangle, 0.05, 0.1, vibHz: 20, vibDepth: 0.05);
		balloon.Pop(3.9, 1.0, 420).Noise(3.9, 0.2, 0.6, 7000, 2000, 0.001, 0.15, highPass: 500);

		// sneeze: "ah... ah... CHOO" with the choo at step 14 (0.12 s a step), then the spines whizz off
		var sneeze = new Sfx(2.4, 306);
		sneeze.Tone(0.25, 0.35, 700, 900, 0.2, W.Triangle, 0.08, 0.06, vibHz: 6, vibDepth: 0.02);
		sneeze.Tone(0.85, 0.7, 760, 1100, 0.22, W.Triangle, 0.1, 0.06, vibHz: 6, vibDepth: 0.02);
		sneeze.Noise(1.68, 0.28, 0.6, 7000, 2500, 0.004, 0.2, highPass: 1300);
		sneeze.Tone(1.68, 0.12, 1000, 600, 0.25, W.Triangle, 0.003, 0.08);
		for (int i = 0; i < 3; i++) sneeze.Tone(1.75 + i * 0.05, 0.25, 2600 - i * 300, 1800 - i * 200, 0.08, W.Sine, 0.003, 0.15);

		var wind = new Sfx(4.0, 307).Noise(0, 3.8, 0.35, 700, 1400, 0.8, 1.2, highPass: 200, amHz: 0.6, amDepth: 0.5);
		for (int i = 0; i < 12; i++) wind.Noise(1.2 + i * 0.24, 0.08, 0.12, 4000, 2500, 0.005, 0.06, highPass: 1200);   // the tumbleweed rustles

		var crack = new Sfx(1.2, 308).Click(0.17, 0.9).Noise(0.18, 0.12, 0.5, 6000, 1500, 0.001, 0.1, highPass: 900).Click(0.95, 0.7).Noise(0.96, 0.15, 0.4, 5000, 1200, 0.001, 0.12, highPass: 900);
		var patched = Ding(309, 88);

		var patter = new Sfx(2.0, 310);
		for (int i = 0; i < 16; i++) patter.Click(i * 0.11, 0.25 + 0.1 * (i % 2));

		// bird: chirps, then the prick (step 24 at 0.13 s): a squawk, and flapping
		var bird = new Sfx(4.6, 311);
		for (int i = 0; i < 3; i++) bird.Tone(0.3 + i * 0.3, 0.08, 2600, 3400, 0.3, W.Sine, 0.005, 0.03);
		bird.Tone(1.8, 0.1, 2700, 3500, 0.25, W.Sine, 0.005, 0.03).Tone(1.95, 0.1, 2700, 3600, 0.25, W.Sine, 0.005, 0.03);
		bird.Tone(3.12, 0.3, 3200, 2000, 0.5, W.Saw, 0.003, 0.15, vibHz: 25, vibDepth: 0.06);
		bird.Noise(3.3, 0.7, 0.3, 1800, 1500, 0.02, 0.2, highPass: 300, amHz: 14, amDepth: 0.9);
		bird.Soften(4500);

		var roll = new Sfx(1.4, 312);
		for (int i = 0; i < 5; i++) roll.Thud(i * 0.3, 0.4, 200, 100, 0.1);
		roll.Noise(0, 1.3, 0.15, 900, 600, 0.1, 0.3, highPass: 150);
		var dizzy = new Sfx(1.2, 313).Tone(0, 1.0, 600, 600, 0.3, W.Triangle, 0.05, 0.2, vibHz: 5, vibDepth: 0.25);

		var petals = Arpeggio(314, W.Sine, 0.12, 84, 88, 91, 96, 91, 88, 84, 88);
		var butterfly = Sparkle(0.6, false, 315);
		var baby = new Sfx(2.0, 316).Pop(0.65, 0.5, 900).Pop(1.13, 0.5, 1000);
		baby.Tone(1.6, 0.12, 1800, 2400, 0.3, W.Triangle, 0.005, 0.06).Tone(1.75, 0.14, 2000, 2800, 0.3, W.Triangle, 0.005, 0.08);   // the baby's "hi!"

		var bubbles = new Sfx(3.4, 317).Pop(1.56, 0.7, 1100).Pop(3.12, 0.7, 1250);
		var dance = Arpeggio(318, W.Square, 0.13, 72, 76, 79, 76, 74, 77, 81, 77, 72, 76, 79, 84);
		var hiccup = new Sfx(3.2, 319);
		foreach (double t in new[] { 0.42, 1.68, 2.94 }) { hiccup.Tone(t, 0.09, 700, 1400, 0.6, W.Sine, 0.003, 0.04); hiccup.Click(t, 0.3); }
		var comb = new Sfx(2.4, 320);
		for (int i = 0; i < 6; i++) comb.Noise(0.16 + i * 0.32, 0.22, 0.3, 6000, 3000, 0.02, 0.08, highPass: 2500, amHz: 60, amDepth: 0.8);
		comb.Bell(1.95, 2600, 0.3, 0.2);
		var whistle = new Sfx(3.0, 321);
		int[] tune = { 79, 84, 83, 79, 76, 79, 81, 79 };
		for (int i = 0; i < tune.Length; i++) { double f = Sfx.Midi(tune[i]); whistle.Tone(0.2 + i * 0.32, 0.28, f, f, 0.3, W.Sine, 0.03, 0.08, vibHz: 6, vibDepth: 0.015); }
		var kick = new Sfx(2.0, 322).Thud(0.98, 0.5, 220, 90, 0.1);
		for (int i = 0; i < 5; i++) kick.Click(1.1 + i * 0.14, 0.3 - i * 0.04);
		var spin = Whoosh(0.4, 323);

		var keys = Keys(1.6, 324);
		var errorBuzz = new Sfx(0.6, 325).Tone(0, 0.18, 230, 220, 0.4, W.Square, 0.005, 0.03).Tone(0.24, 0.26, 180, 170, 0.4, W.Square, 0.005, 0.08).Soften(1800);
		var sob = new Sfx(1.3, 326);
		for (int i = 0; i < 3; i++) sob.Tone(i * 0.34, 0.26, 1300, 900, 0.32, W.Triangle, 0.02, 0.1, vibHz: 12, vibDepth: 0.06);
		var grr = new Sfx(0.8, 327).Tone(0, 0.6, 300, 260, 0.4, W.Saw, 0.03, 0.2, amHz: 30, amDepth: 0.6).Soften(1600);
		var shiver = new Sfx(1.2, 328);
		for (int i = 0; i < 8; i++) shiver.Click(i * 0.07, 0.35);
		shiver.Tone(0, 1.0, 900, 800, 0.12, W.Triangle, 0.05, 0.2, vibHz: 18, vibDepth: 0.05);
		var fanfare = Arpeggio(329, W.Saw, 0.14, 67, 72, 76, 79, 76, 79);
		var confetti = new Sfx(1.2, 330).Pop(0.2, 0.8, 600);
		confetti.Noise(0.22, 0.6, 0.25, 8000, 4000, 0.005, 0.5, highPass: 3000);
		var splash = Splat(331);
		var rain = new Sfx(3.0, 332).Noise(0, 2.8, 0.3, 6000, 5000, 0.3, 0.5, highPass: 1500, amHz: 13, amDepth: 0.4);
		rain.Tone(2.2, 0.9, 300, 900, 0.3, W.Triangle, 0.05, 0.2, vibHz: 10, vibDepth: 0.03);   // he grows
		var gulp = new Sfx(2.2, 334);
		for (int i = 0; i < 3; i++) gulp.Tone(0.7 + i * 0.3, 0.09, 320, 180, 0.5, W.Sine, 0.005, 0.05);
		gulp.Noise(1.65, 0.3, 0.25, 2000, 900, 0.03, 0.2, highPass: 300);   // "ahh"
		var crunch = new Sfx(3.0, 335);
		foreach (double t in new[] { 0.55, 0.95, 1.35, 1.9, 2.3 }) crunch.Noise(t, 0.08, 0.5, 5000, 1500, 0.001, 0.06, highPass: 500);
		var horn = new Sfx(1.0, 336).Tone(0, 0.7, 520, 560, 0.4, W.Saw, 0.02, 0.15, vibHz: 6, vibDepth: 0.02, amHz: 30, amDepth: 0.3).Soften(2600);
		var hug = new Sfx(3.0, 337).Pop(1.8, 0.9, 500);
		hug.Bell(2.0, Sfx.Midi(84), 0.35, 0.3).Bell(2.15, Sfx.Midi(88), 0.35, 0.3).Bell(2.3, Sfx.Midi(91), 0.35, 0.4);
		var swish = new Sfx(0.9, 338);
		for (int i = 0; i < 3; i++) swish.Noise(i * 0.25, 0.18, 0.4, 3000, 1500, 0.03, 0.1, highPass: 800);
		var hammer = new Sfx(1.3, 339);
		for (int i = 0; i < 3; i++) { hammer.Click(0.3 + i * 0.3, 0.7); hammer.Bell(0.3 + i * 0.3, 2400, 0.12, 0.05); }
		var music = Arpeggio(340, W.Triangle, 0.16, 72, 76, 79, 81, 79, 76, 74, 72);
		var sip = new Sfx(1.6, 341).Noise(0.5, 0.5, 0.3, 2500, 1200, 0.1, 0.2, highPass: 600);
		sip.Tone(1.2, 0.3, 700, 560, 0.3, W.Triangle, 0.03, 0.15);
		var oi = new Sfx(0.7, 342);
		oi.Tone(0.15, 0.1, 900, 1200, 0.45, W.Triangle, 0.01, 0.03).Tone(0.25, 0.22, 1300, 950, 0.45, W.Triangle, 0.01, 0.12, vibHz: 8, vibDepth: 0.03);
		var star = new Sfx(3.4, 343);
		for (int i = 0; i < 8; i++) star.Bell(2.16 + i * 0.07, 2400 + i * 180, 0.15, 0.15);
		var snow = Sparkle(0.8, false, 344);
		var lick = new Sfx(2.0, 345).Noise(0.6, 0.2, 0.3, 3000, 1500, 0.02, 0.1, highPass: 700).Noise(1.4, 0.2, 0.3, 3000, 1500, 0.02, 0.1, highPass: 700);
		var land = new Sfx(0.6, 346).Thud(0, 0.6, 180, 70, 0.18);
		land.Click(0, 0.4);
		var hard = new Sfx(1.6, 347).Thud(0, 0.8, 150, 50, 0.3);
		hard.Click(0, 0.6);
		hard.Tone(0.2, 1.1, 700, 700, 0.2, W.Triangle, 0.05, 0.3, vibHz: 5, vibDepth: 0.2);   // birdies around his head
		var peek = Voice(348, 900, 1400, 0.15);

		return new List<PetSound>
		{
			new(1, 12, Tok(349, 0.35)),                     // walk: a tiny clay tok now and then
			new(19, 20, Tok(350, 0.35)),                    // start walking
			new(7, 80, land),                               // land
			new(8, 100, hard),                              // fall hard
			new(9, 80, Boing(351)),                         // hop up
			new(13, 35, Snore(352)),                        // sleep
			new(14, 60, Yawn(1.6, 353)),                    // wake up
			new(17, 50, Voice(354, 1000, 1400)),            // wave
			new(18, 40, patter),                            // run
			new(22, 100, water),                            // watering can
			new(23, 100, bloom),                            // bloom
			new(24, 60, sparkleUp),                         // flower
			new(25, 100, buzz),                             // bee
			new(26, 60, Sparkle(0.5, false, 355)),          // flower closes
			new(27, 100, balloon),                          // balloon (pops on a spine)
			new(28, 100, sneeze),                           // sneeze (the spines fly off)
			new(29, 100, wind),                             // tumbleweed
			new(30, 100, crack),                            // crack pot
			new(31, 100, patched),                          // patch pot
			new(32, 80, patter),                            // calango
			new(33, 100, bird),                             // bird (chirps, gets pricked)
			new(34, 100, roll),                             // roll
			new(35, 80, dizzy),                             // dizzy roll
			new(36, 100, petals),                           // flower rain
			new(37, 70, butterfly),                         // butterfly
			new(38, 100, baby),                             // baby cactus
			new(39, 100, bubbles),                          // bubbles
			new(40, 80, dance),                             // dance
			new(10, 50, dance),                             // sync
			new(41, 100, Giggle(1.6, 356)),                 // laugh
			new(42, 70, Yawn(1.6, 357)),                    // yawn
			new(43, 60, Yawn(1.9, 358)),                    // stretch
			new(44, 100, hiccup),                           // hiccup
			new(45, 70, peek),                              // peek
			new(46, 90, comb),                              // comb spines
			new(47, 100, whistle),                          // whistle
			new(48, 100, kick),                             // kick pebble
			new(49, 80, Boing(359)),                        // boing
			new(50, 80, spin),                              // spin
			new(62, 100, Ding(360)),                        // idea
			new(64, 100, keys),                             // laptop
			new(65, 60, Keys(1.0, 361)),                    // write (pencil taps)
			new(67, 100, Chime(362)),                       // download
			new(69, 100, star),                             // telescope (shooting star)
			new(70, 100, Ding(363, 88)),                    // thumbs up
			new(71, 100, confetti),                         // confetti
			new(72, 80, fanfare),                           // flag
			new(73, 100, errorBuzz),                        // error
			new(74, 80, sob),                               // cry
			new(75, 100, grr),                              // angry
			new(76, 100, Boing(364)),                       // shocked
			new(77, 100, shiver),                           // scared
			new(81, 100, splash),                           // puddle splash
			new(83, 70, swish),                             // hand fan
			new(84, 80, Chime(365)),                        // sunbathing
			new(85, 100, lick),                             // ice cream
			new(86, 80, shiver),                            // cold
			new(87, 80, shiver),                            // shiver
			new(89, 80, snow),                              // snow cap
			new(90, 100, rain),                             // rain cloud (and he grows)
			new(92, 100, Voice(366, 900, 1600, 0.14)),      // surprised
			new(94, 60, Yawn(1.5, 367)),                    // sleepy
			new(95, 100, gulp),                             // drink
			new(96, 100, crunch),                           // eat (watermelon)
			new(97, 100, crunch),                           // cookie
			new(100, 80, dizzy),                            // dizzy
			new(102, 100, Ding(368, 81)),                   // ok
			new(103, 100, Giggle(1.8, 369)),                // cheer
			new(104, 100, horn),                            // party
			new(105, 80, Boing(370)),                       // jump
			new(106, 60, Giggle(1.7, 371)),                 // happy
			new(107, 100, hug),                             // hug (the heart balloon pops)
			new(108, 100, Chime(372)),                      // hearts
			new(109, 80, Chime(373)),                       // love
			new(110, 80, swish),                            // sweep
			new(111, 100, hammer),                          // work
			new(112, 100, music),                           // music
			new(113, 100, sip),                             // coffee
			new(114, 100, oi),                              // hello
			new(116, 100, music),                           // sing
		};
	}
}
