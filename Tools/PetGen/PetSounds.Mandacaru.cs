namespace PetGen;

using W = Sfx.Wave;

public static partial class PetSounds
{
	/// <summary>
	/// Mandacaru, the cactus of the sertão: a little forró on the sanfona (square and saw tones with vibrato, the
	/// triangle and the zabumba), the asa-branca's coo, the carcará's cry, the jegue's hee-haw, a goat, the bonfire...
	/// Sounds that belong to a moment inside a scene start after a short silence, so they fall on that moment.
	/// </summary>
	static List<PetSound> Mandacaru()
	{
		// the sanfona: two reeds a little apart (square + saw) with a slow vibrato, softened
		Sfx Accordion(Sfx s, double at, double dur, int note, double vol = 0.22)
		{
			double f = Sfx.Midi(note);
			s.Tone(at, dur, f, f, vol, W.Square, 0.02, dur * 0.35, vibHz: 5.5, vibDepth: 0.006);
			s.Tone(at, dur, f * 1.004, f * 1.004, vol * 0.8, W.Saw, 0.02, dur * 0.35, vibHz: 5.5, vibDepth: 0.006);
			return s;
		}
		Sfx Triangle(Sfx s, double at, double vol = 0.3) => s.Bell(at, 2640, vol, 0.25).Bell(at, 3960, vol * 0.4, 0.15);
		Sfx Zabumba(Sfx s, double at, double vol = 0.5) => s.Thud(at, vol, 120, 50, 0.25);
		// a forró phrase in the mixolydian mode (the flat seventh of the baião), with triangle and zabumba
		Sfx Forro(int seed, int bars)
		{
			int[] melody = { 67, 71, 74, 77, 76, 74, 71, 74, 72, 71, 69, 67, 69, 71, 67, 67 };
			double step = 0.16;
			var s = new Sfx(bars * 8 * step + 0.6, seed);
			for (int i = 0; i < bars * 8; i++)
			{
				Accordion(s, i * step, step * (i % 4 == 3 ? 1.6 : 0.95), melody[i % melody.Length]);
				if (i % 2 == 1) Triangle(s, i * step, 0.16);
				if (i % 4 == 0) Zabumba(s, i * step, 0.45);
				if (i % 8 == 6) Zabumba(s, i * step, 0.3);
			}
			for (int i = 0; i < bars * 2; i++) Accordion(s, i * step * 4, step * 3.6, i % 2 == 0 ? 55 : 50, 0.1);   // bass buttons
			return s.Soften(3200);
		}
		var forro = Forro(401, 3);
		var xote = Forro(402, 2);
		var music = Forro(403, 2);
		var tri = new Sfx(3.0, 404);
		for (int i = 0; i < 10; i++) Triangle(tri, 0.39 + i * 0.26, i % 2 == 0 ? 0.35 : 0.22);
		var zab = new Sfx(4.8, 405);
		for (int i = 0; i < 8; i++) { Zabumba(zab, 0.56 + i * 0.56, 0.55); zab.Click(0.56 + i * 0.56 + 0.28, 0.35); }
		var fanSwish = new Sfx(1.3, 406);
		for (int i = 0; i < 2; i++) fanSwish.Noise(i * 0.6, 0.3, 0.35, 1200, 2500, 0.08, 0.15, highPass: 300);
		var sizzle = new Sfx(1.8, 407).Noise(0, 1.6, 0.18, 7000, 5000, 0.4, 0.6, highPass: 2500, amHz: 9, amDepth: 0.5);
		var rain = new Sfx(4.5, 408).Noise(2.0, 2.4, 0.3, 6000, 5000, 0.3, 0.6, highPass: 1500, amHz: 13, amDepth: 0.4);
		rain.Bell(1.9, Sfx.Midi(79), 0.12, 0.3);
		var bloom = Sparkle(0.9, true, 409);
		var moon = Arpeggio(410, W.Sine, 0.28, 64, 67, 71, 76);
		var night = new Sfx(2.0, 411).Bell(0, Sfx.Midi(88), 0.25, 0.4).Bell(0.4, Sfx.Midi(83), 0.2, 0.4).Bell(0.8, Sfx.Midi(91), 0.2, 0.5);
		// the asa-branca: a soft coo, "hoo-hoo"
		var coo = new Sfx(2.6, 412);
		foreach (double t in new[] { 0.9, 1.25, 1.9 }) coo.Tone(t, 0.28, 420, 360, 0.35, W.Sine, 0.04, 0.12, vibHz: 6, vibDepth: 0.02, harmonic: 0.15);
		coo.Soften(1500);
		// the carcará: a harsh cry
		var carcara = new Sfx(1.6, 413);
		carcara.Tone(0.2, 0.45, 2200, 1500, 0.35, W.Saw, 0.02, 0.2, vibHz: 22, vibDepth: 0.05, harmonic: 0.3);
		carcara.Tone(0.75, 0.5, 2100, 1300, 0.35, W.Saw, 0.02, 0.25, vibHz: 22, vibDepth: 0.05, harmonic: 0.3);
		carcara.Soften(3800);
		var patter = new Sfx(1.4, 414);
		for (double t = 0.1; t < 1.2; t += 0.07) patter.Click(t, 0.3);
		var push = new Sfx(2.6, 415);
		for (int i = 0; i < 8; i++) push.Tone(i * 0.3, 0.1, 300, 420, 0.3, W.Sine, 0.005, 0.05);
		// the jegue: hee-haw, hee-haw
		var heehaw = new Sfx(3.6, 416);
		for (int i = 0; i < 2; i++)
		{
			double t = 1.6 + i * 0.8;
			heehaw.Tone(t, 0.32, 900, 1100, 0.35, W.Saw, 0.02, 0.06, amHz: 30, amDepth: 0.4, harmonic: 0.3);
			heehaw.Tone(t + 0.34, 0.4, 330, 260, 0.4, W.Saw, 0.02, 0.15, amHz: 25, amDepth: 0.5, harmonic: 0.3);
		}
		heehaw.Soften(2400);
		var pages = new Sfx(5.6, 417);
		pages.Noise(0.2, 0.25, 0.3, 3000, 1500, 0.02, 0.15, highPass: 600).Noise(3.0, 0.3, 0.3, 3200, 1500, 0.02, 0.2, highPass: 600);
		var munch = new Sfx(3.0, 419);
		foreach (double t in new[] { 0.9, 1.1, 1.3, 2.3, 2.5 }) munch.Noise(t, 0.07, 0.4, 2500, 1200, 0.003, 0.05, highPass: 400);
		var crunch = new Sfx(2.0, 420);
		crunch.Noise(0.9, 0.12, 0.7, 5000, 2500, 0.001, 0.1, highPass: 800).Click(0.9, 0.8).Noise(1.2, 0.1, 0.4, 4000, 2000, 0.002, 0.08, highPass: 800);
		var crackle = new Sfx(4.5, 421);
		var r = new Random(421);
		for (double t = 0.3; t < 4.2; t += 0.05 + r.NextDouble() * 0.2) crackle.Click(t, 0.2 + 0.3 * r.NextDouble());
		crackle.Noise(0.3, 3.9, 0.08, 1200, 900, 0.4, 0.6);
		var festa = Arpeggio(422, W.Square, 0.12, 72, 76, 79, 84, 79, 76, 72, 76, 79, 84);
		var balloon = new Sfx(3.0, 424);
		balloon.Pop(1.92, 0.9, 500).Noise(1.92, 0.15, 0.4, 6000, 2000, 0.001, 0.12, highPass: 600);
		balloon.Tone(2.05, 0.25, 900, 1400, 0.25, W.Sine, 0.005, 0.1);
		var tip = Ding(425, 81);
		var sneeze = Sneeze(1.1, 426);
		var spines = new Sfx(1.6, 427);
		for (int i = 0; i < 5; i++) spines.Tone(1.3 + i * 0.02, 0.08, 3000 - i * 200, 1500, 0.15, W.Sine, 0.002, 0.05);
		var whirl = new Sfx(3.4, 428).Noise(0, 3.2, 0.35, 500, 2500, 1.2, 1.2, highPass: 200, amHz: 3, amDepth: 0.5);
		var hatBack = Chime(429);
		var goat = new Sfx(4.0, 430);
		goat.Tone(1.0, 0.6, 520, 480, 0.3, W.Saw, 0.03, 0.2, vibHz: 9, vibDepth: 0.06, amHz: 11, amDepth: 0.4, harmonic: 0.2);
		goat.Tone(2.9, 0.8, 700, 560, 0.35, W.Saw, 0.02, 0.3, vibHz: 10, vibDepth: 0.08, amHz: 12, amDepth: 0.5, harmonic: 0.2);
		goat.Soften(3000);
		var star = Sparkle(1.0, false, 431);
		var topWhirr = new Sfx(4.0, 432).Tone(0.7, 2.8, 380, 300, 0.25, W.Triangle, 0.1, 0.6, amHz: 18, amDepth: 0.4);
		var birdSong = new Sfx(2.4, 433);
		foreach (double t in new[] { 0.0, 0.18, 0.36, 0.9, 1.05, 1.2, 1.35 }) birdSong.Tone(t, 0.12, 2800, 3600, 0.3, W.Sine, 0.005, 0.05, vibHz: 30, vibDepth: 0.04);
		var hatTrick = Whoosh(0.5, 434);
		var bonk = new Sfx(0.8, 435).Thud(0.1, 0.6, 220, 80, 0.18).Bell(0.12, 1800, 0.12, 0.15);
		var land = new Sfx(0.5, 436).Thud(0, 0.5, 160, 60, 0.2);
		var thud = new Sfx(0.8, 437).Thud(0, 0.8, 120, 40, 0.3);
		var buzz = new Sfx(0.6, 438).Tone(0, 0.18, 220, 210, 0.4, W.Square, 0.005, 0.03).Tone(0.24, 0.26, 180, 170, 0.4, W.Square, 0.005, 0.08).Soften(1800);
		var sob = new Sfx(1.2, 439);
		for (int i = 0; i < 3; i++) sob.Tone(i * 0.32, 0.26, 600, 420, 0.35, W.Triangle, 0.02, 0.1, vibHz: 10, vibDepth: 0.06);
		var grumble = new Sfx(1.0, 440).Tone(0, 0.8, 160, 130, 0.4, W.Saw, 0.05, 0.3, amHz: 30, amDepth: 0.6, harmonic: 0.3).Soften(1200);
		var shock = new Sfx(0.7, 441).Tone(0.1, 0.3, 500, 1300, 0.35, W.Triangle, 0.005, 0.1);
		var shiver = new Sfx(1.2, 442);
		for (int i = 0; i < 8; i++) shiver.Click(i * 0.12, 0.35);
		var gulp = new Sfx(1.2, 443).Tone(0.75, 0.12, 320, 160, 0.5, W.Sine, 0.005, 0.06).Tone(1.0, 0.1, 280, 150, 0.4, W.Sine, 0.005, 0.05);
		var sip = new Sfx(1.6, 444).Noise(0.9, 0.5, 0.3, 2500, 1200, 0.1, 0.2, highPass: 600);
		var horn = new Sfx(1.0, 445).Tone(0, 0.7, 520, 560, 0.4, W.Saw, 0.02, 0.15, vibHz: 6, vibDepth: 0.02, amHz: 30, amDepth: 0.3).Soften(2600);
		var swish = new Sfx(0.9, 446);
		for (int i = 0; i < 3; i++) swish.Noise(i * 0.3, 0.18, 0.4, 3000, 1500, 0.03, 0.1, highPass: 800);
		var hoe = new Sfx(1.5, 447);
		for (int i = 0; i < 2; i++) { hoe.Thud(0.35 + i * 0.68, 0.5, 180, 70, 0.12); hoe.Noise(0.35 + i * 0.68, 0.12, 0.3, 2500, 800, 0.002, 0.1); }
		var scribble = new Sfx(1.0, 448);
		for (int i = 0; i < 4; i++) scribble.Noise(i * 0.2, 0.12, 0.25, 5000, 3000, 0.01, 0.06, highPass: 1500);
		var oi = new Sfx(0.6, 449).Tone(0, 0.12, 600, 750, 0.35, W.Triangle, 0.01, 0.04).Tone(0.14, 0.25, 900, 1100, 0.35, W.Triangle, 0.01, 0.12);
		var pud = new Sfx(2.0, 451).Pop(1.4, 0.4, 400);

		return new List<PetSound>
		{
			new(30, 100, forro),                         // forró on the sanfona
			new(31, 60, xote),                           // xote
			new(113, 100, music),                        // music
			new(33, 100, tri),                           // triangle
			new(34, 100, zab),                           // zabumba
			new(35, 80, sizzle),                         // sertão sun
			new(75, 70, fanSwish),                       // hand fan
			new(36, 100, rain),                          // the rain arrives
			new(37, 80, Giggle(1.0, 452)),               // rain dance
			new(38, 100, bloom),                         // the flowers bloom
			new(39, 100, moon),                          // full moon
			new(40, 100, night),                         // night bloom
			new(41, 100, coo),                           // asa-branca
			new(42, 100, carcara),                       // carcará
			new(43, 80, patter),                         // calango
			new(44, 70, push),                           // push-ups
			new(45, 100, heehaw),                        // jegue
			new(46, 100, pages),                         // cordel
			new(47, 80, munch),                          // cuscuz
			new(48, 100, crunch),                        // rapadura
			new(49, 80, munch),                          // tapioca
			new(50, 100, crackle),                       // fogueira
			new(51, 100, festa),                         // bandeirinhas
			new(52, 100, balloon),                       // balloon pop
			new(53, 100, tip),                           // tip hat
			new(54, 100, sneeze),                        // sneeze
			new(54, 100, spines),                        // the spines fly
			new(55, 100, whirl),                         // dust devil
			new(58, 100, hatBack),                       // hat back
			new(59, 100, goat),                          // goat
			new(23, 100, star),                          // shooting star
			new(24, 100, topWhirr),                      // spinning top
			new(27, 100, birdSong),                      // galo-de-campina on his arm
			new(26, 100, hatTrick),                      // hat trick
			new(9, 100, bonk),                           // bump into the border
			new(7, 60, land),                            // land
			new(8, 100, thud),                           // fall hard
			new(13, 35, Snore(453)),                     // sleep
			new(21, 100, oi),                            // hello ("oi!")
			new(61, 100, Ding(454)),                     // idea
			new(64, 100, Keys(1.4, 455)),                // laptop
			new(65, 80, scribble),                       // write
			new(67, 100, Chime(456)),                    // download
			new(70, 100, Ding(457, 88)),                 // thumbs up
			new(71, 100, new Sfx(0.6, 458).Pop(0.2, 0.8, 600)),   // confetti
			new(73, 100, buzz),                          // error
			new(74, 80, sob),                            // cry
			new(76, 100, grumble),                       // angry
			new(77, 100, shock),                         // shocked
			new(78, 80, shiver),                         // scared
			new(81, 60, pud),                            // puddle splash (the jump)
			new(82, 100, Splat(450)),                    // splash
			new(86, 60, shiver),                         // cold
			new(92, 100, shock),                         // surprised ("oxe!")
			new(95, 100, gulp),                          // drink
			new(96, 100, munch),                         // eat
			new(97, 100, crunch),                        // cookie
			new(114, 100, sip),                          // coffee
			new(100, 80, Boing(459)),                    // dizzy
			new(104, 100, Giggle(1.2, 460)),             // cheer
			new(105, 100, horn),                         // party
			new(106, 100, Boing(461)),                   // jump
			new(109, 100, Chime(462)),                   // hearts
			new(111, 80, swish),                         // sweep
			new(112, 100, hoe),                          // work
			new(116, 100, Giggle(0.9, 463)),             // laugh
			new(118, 60, Yawn(1.1, 464)),                // yawn
			new(117, 50, Yawn(1.3, 465)),                // stretch
		};
	}
}
