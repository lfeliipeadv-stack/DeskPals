namespace PetGen;

static partial class MagoPet
{
	// Scenes 114-126 (second parts 160-169, child windows 240-259).
	static void ScenesB(Kit k)
	{
		var p = k.P;
		int F(Frame f) => k.F(f);
		void Eyes0Down(Frame f) { f.Dot(5, 5, Skin).Dot(10, 5, Skin).Dot(5, 6, Ink).Dot(10, 6, Ink); }      // looking down
		void Glow(Frame f, char c, params (int x, int y)[] dots) { foreach (var (x, y) in dots) if (f[x, y] == '.') f.Dot(x, y, c); }

		// ================================================================ 114 magic mirror
		// a hand mirror held up beside the head; the reflection pulls faces
		void Mirror(Frame f, int face)
		{
			f.Rect(1, 5, 1, 3, Robe).Dot(1, 4, Skin).Dot(2, 4, Skin);
			f.Rect(2, 2, 1, 2, Wood);
			f.Stamp(0, -4, ".eee.", "e...e", "e...e", "e...e", "e...e", ".eee.");
			string[] rows = face switch
			{
				1 => new[] { "rRr", "kFK", "WPW", "WTW" },    // winks and sticks the tongue out
				2 => new[] { "rRr", "kFk", "WKW", "WWW" },    // laughs at him
				_ => new[] { "rRr", "KFK", "WPW", "WWW" },    // just like him
			};
			f.Stamp(1, -3, rows);
		}
		void Question(Frame f) => f.Stamp(13, -4, ".KK", "..K", ".K.", "...", ".K.");
		var mc = Base(); Eyes(mc, 2); Arm(mc, -1, 1); WandDown(mc); Sparkle(mc, 2, -2, Star); mc.Dot(0, 1, Cyan).Dot(4, -4, Magenta);
		int mrConjure = F(mc);
		var m0 = Base(); Eyes(m0, 2); Mirror(m0, 0); WandDown(m0);
		int mrLook = F(m0);
		var m1 = Base(); Eyes(m1, 2); Mirror(m1, 1); WandDown(m1);
		int mrCheeky = F(m1);
		var m2 = Base(); Eyes(m2, 4); Mouth(m2); Mirror(m2, 0); WandDown(m2);
		int mrStartle = F(m2);
		var m3 = Base(); Eyes(m3, 2); Mirror(m3, 2); Arm(m3, 1, 3); Question(m3);
		int mrLaugh = F(m3);
		var m4 = Base(); Eyes(m4, 3); Mirror(m4, 0); Arm(m4, 1, 3); Question(m4); m4.Dot(12, 10, Skin).Dot(12, 9, BeardShade);
		int mrPuzzled = F(m4);
		k.Show(114, "magic mirror", 170, (k.Stand, 1), (mrConjure, 2), (mrLook, 4), (mrCheeky, 3), (mrLook, 1), (mrStartle, 3), (mrLook, 3),
			(mrCheeky, 2), (mrLook, 1), (mrLaugh, 3), (mrPuzzled, 3), (mrLaugh, 2), (mrPuzzled, 3), (k.Blink, 1), (k.Stand, 2));
		k.Sounds.Add(new PetSound(114, 100, new Sfx(1.6, 350).Bell(0.2, 1568, 0.25, 0.4).Bell(0.32, 2093, 0.2, 0.4)
			.Tone(0.9, 0.12, 700, 900, 0.18, Sfx.Wave.Triangle).Tone(1.05, 0.2, 900, 600, 0.18, Sfx.Wave.Triangle)));

		// ================================================================ 115 hourglass
		// a 6 x 7 hourglass held in front of the chest; sand: top and bottom amounts (0..2 rows), a falling stream
		char[,] HgGrid(int top, int bottom, bool stream)
		{
			string[] rows = { "wwwwww", ".b..b.", ".b..b.", "..bb..", ".b..b.", ".b..b.", "wwwwww" };
			var g = new char[7, 6];
			for (int r = 0; r < 7; r++) for (int c = 0; c < 6; c++) g[r, c] = rows[r][c];
			foreach (int r in new[] { 1, 2, 4, 5 }) { g[r, 2] = GlassLight; g[r, 3] = r == 1 || r == 4 ? GlassLight : Glass; }
			if (top >= 1) g[2, 2] = g[2, 3] = Star;
			if (top >= 2) g[1, 2] = g[1, 3] = Star;
			if (bottom >= 1) g[5, 2] = g[5, 3] = Star;
			if (bottom >= 2) g[4, 2] = g[4, 3] = Star;
			if (stream) { g[3, 2] = Star; if (bottom < 2) g[4, 2] = Star; }
			return g;
		}
		void Hourglass(Frame f, char[,] g, bool sideways)
		{
			for (int r = 0; r < 7; r++) for (int c = 0; c < 6; c++)
			{
				if (g[r, c] == '.') continue;
				if (sideways) f.Dot(5 + r, 8 + c, g[r, c]); else f.Dot(5 + c, 7 + r, g[r, c]);
			}
			if (sideways) f.Dot(4, 11, Skin).Dot(12, 11, Skin); else f.Dot(4, 10, Skin).Dot(11, 10, Skin);
		}
		Frame Hg(int top, int bottom, bool stream, bool sideways = false, int eyes = 0)
		{
			var f = Base(); if (eyes == -1) Eyes0Down(f); else Eyes(f, eyes);
			Hourglass(f, HgGrid(top, bottom, stream), sideways);
			return f;
		}
		int hgFull = F(Hg(0, 2, false, false, -1));
		int hgSide = F(Hg(0, 2, false, true, 4));
		int hgFlip = F(Hg(2, 0, false, false, 0));
		int hgRun1 = F(Hg(2, 0, true, false, -1));
		int hgRun2 = F(Hg(1, 1, true, false, -1));
		// mid-step: the back foot lifted
		Frame Step(bool gray, int sp)
		{
			var f = Base(); Eyes(f, 2);
			Shoes(f, "..UUU..........."); f.Rect(10, 14, 3, 1, Shoe);
			if (gray)
			{
				foreach (var (a, b) in new[] { (Robe, Stone), (RobeDark, StoneDark), (Star, Smoke), (Beard, LightGray), (BeardShade, Smoke),
					(Skin, Smoke), (SkinDark, Ash), (Nose, Ash), (Shoe, StoneDark), (Wood, Ash) })
					Recolor(f, a, b);
			}
			Hourglass(f, HgGrid(1, 1, !gray), false);
			if (gray)
			{
				f.Dot(4, 10, Smoke).Dot(11, 10, Smoke);
				var spots = sp switch
				{
					0 => new[] { (1, -2, Cyan), (14, 2, Violet), (13, 12, Cyan) },
					1 => new[] { (2, 1, Violet), (14, -2, Cyan), (1, 11, Cyan) },
					_ => new[] { (3, -3, Cyan), (13, 6, Cyan), (1, 7, Violet) },
				};
				foreach (var (x, y, c) in spots) Sparkle(f, x, y, c);
			}
			return f;
		}
		int hgStep = F(Step(false, 0));
		int hgStop1 = F(Step(true, 0)), hgStop2 = F(Step(true, 1)), hgStop3 = F(Step(true, 2));
		int hgDone = F(Hg(0, 2, false, false, 1));
		k.Lead(115, "hourglass", 160, 160, (k.Stand, 1), (hgFull, 4), (hgSide, 2), (hgFlip, 3), (hgRun1, 4), (hgRun2, 3), (hgStep, 2));
		k.Lead(160, "time stop", 140, 161, (hgStop1, 2), (hgStop2, 2), (hgStop3, 2), (hgStop1, 2), (hgStop2, 2), (hgStop3, 2), (hgStop1, 2));
		k.Show(161, "time resumes", 160, (hgStep, 2), (k.Surprise, 3), (hgRun2, 3), (hgDone, 3), (k.Blink, 1), (k.Happy, 2), (k.Stand, 2));
		{
			var tick = new Sfx(2.0, 351);
			for (int i = 0; i < 6; i++) tick.Click(0.2 + i * 0.3, 0.6).Tone(0.2 + i * 0.3, 0.03, 2400, 2000, 0.2);
			tick.Noise(0.65, 0.3, 0.25, 2500, 900, 0.05, 0.2);
			k.Sounds.Add(new PetSound(115, 100, tick));
			k.Sounds.Add(new PetSound(160, 100, new Sfx(1.2, 352).Tone(0, 1.0, 1200, 180, 0.3, Sfx.Wave.Sine, 0.01, 0.4, vibHz: 6, vibDepth: 0.04)));
			k.Sounds.Add(new PetSound(161, 100, new Sfx(0.6, 353).Tone(0, 0.45, 180, 1200, 0.3, Sfx.Wave.Sine, 0.01, 0.1)));
		}

		// ================================================================ 116 magic letter
		Frame Write(int prog, int tip)
		{
			var f = Base(); Eyes0Down(f);
			f.Rect(4, 8, 7, 5, LightGray).Rect(5, 9, 5, 3, Paper).Dot(3, 11, Skin);
			for (int i = 0; i < prog; i++) if (i % 5 != 2) f.Dot(5 + i % 5, 9 + 2 * (i / 5), Ash);
			int ty = 9 + (prog >= 5 ? 2 : 0);
			f.Dot(tip, ty, Ink).Dot(tip + 1, ty - 1, Magenta).Dot(tip + 2, ty - 2, Magenta).Dot(tip + 3, ty - 3, Magenta).Dot(tip + 2, ty - 1, Violet).Dot(tip + 3, ty - 2, Violet);
			f.Dot(tip + 1, ty + 1, Skin); Line(f, tip + 2, ty + 2, 13, ty + 2, Robe);
			return f;
		}
		int lt0 = F(Write(0, 5)), lt1 = F(Write(2, 7)), lt2 = F(Write(4, 9)), lt3 = F(Write(7, 7)), lt4 = F(Write(10, 9));
		Frame Envelope(bool seal, bool sparkle)
		{
			var f = Base(); Eyes(f, seal ? 1 : 0);
			f.Rect(5, 9, 6, 4, LightGray).Rect(6, 10, 4, 2, Paper).Dot(6, 10, LightGray).Dot(9, 10, LightGray).Rect(7, 11, 2, 1, LightGray);
			f.Dot(4, 11, Skin).Dot(11, 11, Skin);
			if (seal) f.Rect(7, 11, 2, 1, Red);
			if (sparkle) { Sparkle(f, 3, 7, Star); Sparkle(f, 12, 8, Cyan); f.Dot(8, 13, Magenta); }
			return f;
		}
		int ltFold = F(Envelope(false, false)), ltSeal = F(Envelope(true, false)), ltMagic = F(Envelope(true, true));
		// the origami bird (with the red seal), facing forward (left), in front of the hat: x 3..10, y 0..4
		void Bird(Frame f, int wing)
		{
			if (wing == 0) f.Stamp(3, 0, ".....h..", "....hp..");
			if (wing == 1) f.Stamp(3, 1, "...hpph.");
			f.Stamp(3, 2, "hppppXph", "..hhhh..");
			if (wing == 2) f.Stamp(3, 3, "..hpph..", "...hh...");
		}
		var bh = Base(); Eyes(bh, 4); Arm(bh, -1, 1); Arm(bh, 1, 1); Bird(bh, 1); Sparkle(bh, 13, -2, Star); bh.Dot(1, 0, Cyan).Dot(12, 6, Magenta);
		int ltBird = F(bh);
		var bg = Base(); Eyes(bg, 2); Arm(bg, -1, 1); Arm(bg, 1, 1);
		int ltGo = F(bg);
		var bw = Stand(2); bw.Dot(3, 3, '.');
		int ltWatch = F(bw);
		var birdF = new int[3];
		for (int i = 0; i < 3; i++) { var f = new Frame(); Bird(f, i); birdF[i] = F(f); }
		k.Lead(116, "magic letter", 170, 162, (k.Stand, 1), (lt0, 2), (lt1, 2), (lt2, 2), (lt3, 2), (lt4, 3), (k.Blink, 1), (ltFold, 3), (ltSeal, 3), (ltMagic, 2), (ltBird, 3));
		k.Show(162, "letter flies", 170, (ltGo, 3), (ltWatch, 4), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Wave2, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(162, 240);
		p.A(240, "paper bird").Move("-4", "-3").Speed("90").Loop("30", 0).Seq(birdF[1], birdF[0], birdF[1], birdF[2]);
		k.Sounds.Add(new PetSound(116, 100, new Sfx(2.2, 354).Noise(0.2, 1.3, 0.25, 5000, 3000, 0.02, 0.1, highPass: 1500, amHz: 9, amDepth: 0.9)
			.Pop(1.9, 0.4, 500)));
		{
			var flap = new Sfx(1.2, 355);
			for (int i = 0; i < 5; i++) flap.Noise(i * 0.2, 0.08, 0.3, 2000, 700, 0.01, 0.06);
			flap.Notes(0.05, 0.08, 0.15, Sfx.Wave.Sine, 91, 94, 91);
			k.Sounds.Add(new PetSound(162, 100, flap));
		}

		// ================================================================ 117 enchanted broom (child: the broom sweeps away)
		Frame Broom(int pose)
		{
			var f = new Frame();
			switch (pose)
			{
				case 0:   // lying on the floor
					f.Rect(1, 15, 9, 1, Wood).Dot(10, 15, Red).Stamp(11, 13, "Y...", "YYe.", "YYYY");
					break;
				case 1: case 2:   // standing up, eyes open (2: hop)
					f.Rect(8, 2, 1, 9, Wood).Rect(7, 11, 3, 1, Red);
					f.Stamp(6, 12, "YKYKY", "YYeYY", "YeYYe").Stamp(5, 15, "YeYYeYY");
					if (pose == 2) { f.Shift(0, -2); f.Dot(5, 15, Smoke).Dot(11, 15, Smoke); }
					break;
				case 3:   // sweeping: bristles forward, dust pushed ahead
					Line(f, 11, 3, 8, 10, Wood); f.Rect(6, 11, 3, 1, Red);
					f.Stamp(5, 12, "YKYK.", "YYeYY", "eYYeYY").Stamp(3, 15, "YYeYYe");
					f.Stamp(0, 12, ".hh.", "hqqh", "qhqq.", "hqqh");
					break;
				case 4:   // sweeping: bristles back
					Line(f, 6, 3, 8, 10, Wood); f.Rect(8, 11, 3, 1, Red);
					f.Stamp(8, 12, "KYKY", "YYeY", "YeYYe").Stamp(8, 15, "eYYeYY");
					f.Stamp(1, 11, "..h.", ".hqh.", "hqqqh", "qhqqh.", "hqh").Dot(5, 12, LightGray);
					break;
			}
			return f;
		}
		int brLie = F(Broom(0));
		var bls = Broom(0); Sparkle(bls, 6, 12, Star); Sparkle(bls, 12, 10, Cyan); bls.Dot(3, 11, Magenta);
		int brSpark = F(bls);
		int brStand = F(Broom(1)), brHop = F(Broom(2)), brSweepA = F(Broom(3)), brSweepB = F(Broom(4));
		var bsb = Broom(1); bsb.Dot(7, 12, Star).Dot(9, 12, Star);
		int brBlink = F(bsb);
		var dp1 = new Frame(); dp1.Stamp(5, 13, ".hq.", "qhhq", "hqqh");
		var dp2 = new Frame(); dp2.Stamp(4, 11, "h..q", ".q.h", "h.q..", "..h.q");
		int dust1 = F(dp1), dust2 = F(dp2);
		var bwt = Stand(2); bwt.Rect(1, 8, 1, 2, '.').Dot(1, 10, '.'); bwt.Dot(2, 10, Skin);   // hand on the hip
		int brWatch = F(bwt);
		var bwl = Base(); Eyes(bwl, 1); Arm(bwl, -1, 1); WandDown(bwl); Mouth(bwl);
		int brLaugh = F(bwl);
		k.Lead(117, "enchanted broom", 150, 163, (k.Stand, 1), (k.Cast1, 3), (k.Cast2, 2));
		k.Show(163, "broom wakes", 150, (k.Burst1, 2), (k.Burst2, 2), (brWatch, 6), (brLaugh, 2), (brWatch, 4), (brLaugh, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(163, 241, "imageX-imageW*5/8");
		p.A(241, "broom wakes up").Move("0", "0").Speed("150").Seq(brLie, brSpark, brSpark, brLie, brStand, brHop, brStand, brBlink, brStand).Then(100, 242);
		p.A(242, "broom sweeps").Move("-3", "0").Speed("100").Loop("2", 0).Seq(brSweepA, brSweepA, brSweepB, brSweepB).Then(100, 242);
		k.Child(242, 243, "imageX+imageW/8");
		p.A(243, "dust puff").Move("0", "-1").Speed("120").Fade(0.9, 0).Seq(dust1, dust1, dust2, dust2, dust2);
		k.Sounds.Add(new PetSound(117, 100, new Sfx(1.0, 356).Notes(0.3, 0.07, 0.2, Sfx.Wave.Triangle, 79, 83, 86, 91)));
		{
			var sw = new Sfx(1.1, 357);
			for (int i = 0; i < 3; i++) sw.Noise(0.05 + i * 0.35, 0.25, 0.25, 3500, 1200, 0.08, 0.12, highPass: 500);
			k.Sounds.Add(new PetSound(242, 60, sw));
		}

		// ================================================================ 118 floating candles (children)
		Frame Candle(int ph)
		{
			var f = new Frame();
			f.Rect(7, 12, 2, 4, Paper).Dot(8, 13, LightGray).Dot(8, 14, LightGray).Dot(7, 12, HotWhite).Dot(8, 11, Ink);
			switch (ph)
			{
				case 0: f.Dot(8, 10, Orange).Dot(8, 9, Star); Glow(f, PaleYellow, (6, 9), (10, 10)); break;
				case 1: f.Dot(8, 10, Orange).Dot(8, 9, Star).Dot(8, 8, PaleYellow); Glow(f, PaleYellow, (10, 8), (6, 11)); break;
				default: f.Dot(8, 10, Orange).Dot(7, 9, Star).Dot(7, 10, Orange); Glow(f, PaleYellow, (5, 9), (10, 9)); break;
			}
			return f;
		}
		int cd0 = F(Candle(0)), cd1 = F(Candle(1)), cd2 = F(Candle(2));
		var cu = Base(); Eyes(cu, 1); Arm(cu, -1, 1); Arm(cu, 1, 1); cu.Dot(0, 3, PaleYellow).Dot(15, 2, PaleYellow);
		int cdUp = F(cu);
		var cu2 = Base(); Eyes(cu2, 4); Arm(cu2, -1, 1); Arm(cu2, 1, 1); cu2.Dot(0, 1, PaleYellow).Dot(15, 4, PaleYellow);
		int cdUp2 = F(cu2);
		k.Lead(118, "floating candles", 160, 164, (k.Stand, 1), (k.Cast2, 2), (cdUp, 4), (cdUp2, 3));
		k.Show(164, "more candles", 160, (cdUp, 4), (cdUp2, 3), (k.Happy, 4), (k.Blink, 1), (k.Happy, 3), (k.Stand, 2));
		k.Child(118, 244, "imageX-imageW*7/16");
		k.Child(118, 244, "imageX+imageW*7/16", "imageY-imageH/10");
		k.Child(164, 244, "imageX-imageW*12/16", "imageY-imageH/6");
		k.Child(164, 244, "imageX+imageW*11/16", "imageY-imageH/4");
		p.A(244, "candle floats").Move("0", "-2").Speed("110").Loop("6", 0).Fade(1, 0).Seq(cd0, cd1, cd2, cd1);
		k.Sounds.Add(new PetSound(118, 100, new Sfx(1.6, 358).Bell(0.3, 1319, 0.2, 0.5).Bell(0.5, 1760, 0.18, 0.5)));
		k.Sounds.Add(new PetSound(164, 100, new Sfx(1.6, 359).Bell(0, 1568, 0.2, 0.5).Bell(0.2, 2093, 0.18, 0.5)));

		// ================================================================ 119 tea time (he sits, the teapot pours by itself)
		// sitting: everything 3 rows lower; teapot top-left, cup in the left hand
		Frame Tea(int pot, int cup, int eyes)
		{
			var f = Base(); Eyes(f, eyes); WandDown(f); Squash(f, 3); Shoes(f, ".UUU........UUU.");
			// cup (0 none, 1 empty, 2 full, 3 full with steam, 4 at the mouth): blue porcelain on a saucer
			if (cup >= 1 && cup <= 3)
			{
				f.Stamp(0, 10, cup == 1 ? "upu" : "uwu", "uuu", "pppp").Dot(3, 10, Sky).Dot(1, 13, Skin).Dot(2, 13, Skin);
				if (cup == 3) f.Dot(1, 8, LightGray).Dot(0, 7, LightGray).Dot(1, 5, LightGray);
			}
			if (cup == 4) { f.Stamp(5, 9, "uwu", "uuu").Dot(8, 9, Sky).Dot(4, 10, Skin).Dot(3, 11, Skin); f.Dot(5, 7, LightGray).Dot(4, 6, LightGray); }
			// teapot (0 none, 1 sparkles, 2 floating, 3 pouring, 4 pouring and the stream): pink, floating top-left
			if (pot == 1) { Sparkle(f, 2, 2, Star); f.Dot(0, 0, Cyan).Dot(4, 4, Magenta); }
			if (pot == 2) f.Stamp(0, 0, "..e..", "TTTT.", ".MMMT", ".TTTT", "..TT.").Dot(1, 1, Pink).Dot(2, 3, Paper);
			if (pot >= 3)
			{
				f.Stamp(0, 0, "...e.", "..TTT", ".MMTT", "TTTT.", ".T...").Dot(2, 3, Paper);
				if (pot == 4) f.Rect(1, 5, 1, 5, Wood);
			}
			return f;
		}
		int tSit = F(Tea(0, 1, 2)), tSpark = F(Tea(1, 1, 4)), tPot = F(Tea(2, 1, 2)), tTilt = F(Tea(3, 1, 2)), tPour = F(Tea(4, 1, 2));
		int tPour2 = F(Tea(4, 2, 2)), tFull = F(Tea(2, 3, 1)), tGone = F(Tea(1, 3, 0)), tSip = F(Tea(0, 4, 1));
		var ta = Tea(0, 3, 1); ta.Dot(4, 9, Pink).Dot(11, 9, Pink);
		int tAhh = F(ta);
		k.Show(119, "tea time", 200, (k.Stand, 1), (k.Crouch, 1), (tSit, 3), (tSpark, 2), (tPot, 3), (tTilt, 2), (tPour, 3), (tPour2, 3), (tFull, 3), (tGone, 2),
			(tAhh, 2), (tSip, 4), (tAhh, 4), (tSip, 3), (tAhh, 3), (k.Crouch, 1), (k.Happy, 2), (k.Stand, 1));
		k.Sounds.Add(new PetSound(119, 100, new Sfx(3.2, 360).Bell(0.6, 1760, 0.15, 0.3)
			.Noise(1.4, 1.1, 0.25, 1400, 900, 0.1, 0.3, highPass: 300, amHz: 22, amDepth: 0.5)
			.Tone(3.0, 0.15, 300, 250, 0.2, Sfx.Wave.Triangle)));

		// ================================================================ 120 card trick (children: the cards fly out and fall)
		Frame Fan(int open, int eyes)
		{
			var f = Base(); Eyes(f, eyes); Arm(f, -1, 0);
			f.Dot(7, 12, Skin).Dot(8, 12, Skin).Dot(12, 11, Skin).Rect(13, 10, 1, 2, Robe);
			if (open == 0) f.Stamp(7, 9, "XX", "XX", "pp");
			if (open == 1) f.Stamp(5, 8, ".pXXp.", "pXpXXp", "pXXpXp", ".pXXp.");
			if (open == 2) f.Stamp(4, 7, "..pXXp..", ".pXXpXp.", "pXpXXpXp", "pXXpXpXp", ".pXpXXp.");
			return f;
		}
		int cdDeck = F(Fan(0, -1 + 1)), cdHalf = F(Fan(1, 0)), cdFan = F(Fan(2, 0)), cdFanBl = F(Fan(2, 1));
		var fl = Base(); Eyes(fl, 4); Arm(fl, -1, 1); Arm(fl, 1, 1); Mouth(fl);
		Sparkle(fl, 7, 0, Star); fl.Stamp(3, -2, "pp", "Xp").Stamp(11, -3, "pX", "pp");
		int cdFlick = F(fl);
		var cw = Base(); Eyes(cw, 2); Arm(cw, -1, 1); WandDown(cw);
		int cdWatch = F(cw);
		Frame Ace(bool spark)
		{
			var f = Base(); Eyes(f, spark ? 1 : 3); Arm(f, -1, 1); WandDown(f);
			f.Stamp(0, 0, "ppp", "pXp", "XXX", "ppp").Dot(1, 4, Skin);
			if (spark) { Sparkle(f, 3, -3, Star); f.Dot(0, -2, Cyan).Dot(4, 1, Magenta); }
			return f;
		}
		int cdAce = F(Ace(false)), cdAceSp = F(Ace(true));
		// a card in flight, drawn on rows 12..15 (it lands on its own window's floor)
		Frame Card(int spin)
		{
			var f = new Frame();
			switch (spin)
			{
				case 0: f.Stamp(6, 12, "ppp", "pXp", "ppp", "pXp"); break;
				case 1: f.Stamp(6, 12, ".pp", "pX.", "ph.", "p.."); f.Dot(7, 13, Red); break;
				case 2: f.Rect(7, 12, 1, 4, LightGray); break;
				default: f.Stamp(6, 12, "XJX", "JpJ", "XJX", "JXJ"); break;
			}
			return f;
		}
		var cardF = new int[4];
		for (int i = 0; i < 4; i++) cardF[i] = F(Card(i));
		var crest = new Frame(); crest.Stamp(5, 14, ".pppp", "pXppX");
		int cardRest = F(crest);
		k.Lead(120, "card trick", 160, 165, (k.Stand, 1), (cdDeck, 3), (cdHalf, 2), (cdFan, 4), (cdFanBl, 1), (cdFan, 3), (cdHalf, 1), (cdFan, 2));
		k.Show(165, "cards fly", 150, (cdFlick, 3), (cdWatch, 3), (k.Surprise, 2), (cdWatch, 2), (cdAce, 4), (cdAceSp, 3), (cdAce, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(165, 245, "imageX", "imageY-imageH*4/20");
		k.Child(165, 245, "imageX-imageW/8", "imageY-imageH*5/20");
		k.Child(165, 246, "imageX+imageW/8", "imageY-imageH*4/20");
		k.Child(165, 246, "imageX", "imageY-imageH*6/20");
		p.A(245, "card flies").Move("-4", "-7", "-3", "10").Speed("50").Loop("5", 0).Seq(cardF).Then(100, 247)
			.OnBorder(100, 247, "taskbar").OnBorder(100, 247, "window");
		p.A(246, "card arcs back").Move("3", "-8", "2", "11").Speed("55").Loop("5", 0).Seq(cardF[0], cardF[3], cardF[2], cardF[1]).Then(100, 247)
			.OnBorder(100, 247, "taskbar").OnBorder(100, 247, "window");
		p.A(247, "card lands").Move("0", "0").Speed("150").Fade(1, 0).Seq(cardRest, cardRest, cardRest, cardRest, cardRest, cardRest);
		{
			var sh = new Sfx(1.6, 361);
			for (int i = 0; i < 10; i++) sh.Click(0.15 + i * 0.05, 0.5);
			for (int i = 0; i < 8; i++) sh.Click(1.0 + i * 0.04, 0.45);
			k.Sounds.Add(new PetSound(120, 100, sh));
			k.Sounds.Add(new PetSound(165, 100, new Sfx(1.6, 362).Noise(0, 0.35, 0.35, 5000, 1500, 0.01, 0.2, highPass: 800)
				.Notes(0.9, 0.09, 0.2, Sfx.Wave.Triangle, 72, 76, 79, 84)));
		}

		// ================================================================ 121 gold coins (children: a rain of coins)
		var ce = Base(); Eyes(ce, 3); Arm(ce, -1, 0); ce.Rect(14, 6, 1, 3, Robe).Dot(13, 5, Skin);
		int gcEar = F(ce);
		var cp = Base(); Eyes(cp, 4); Arm(cp, -1, 0); cp.Rect(14, 5, 1, 3, Robe).Dot(14, 4, Skin).Rect(14, 2, 2, 2, Gold).Dot(14, 2, Star);
		int gcPull = F(cp);
		var cs = Base(); Eyes(cs, 1); Arm(cs, -1, 0); WandUp(cs, false); cs.Rect(14, 0, 1, 4, '.');
		cs.Rect(14, 5, 1, 3, Robe).Dot(14, 4, Skin).Rect(14, 2, 2, 2, Gold).Dot(14, 2, Star); Sparkle(cs, 13, 0, Star);
		int gcShow = F(cs);
		var cl = Base(); Eyes(cl, 4); Mouth(cl); Arm(cl, -1, 1); Arm(cl, 1, 1);
		int gcLook = F(cl);
		var cc = Base(); Eyes(cc, 1); Arm(cc, -1, 1); Arm(cc, 1, 1); Mouth(cc); cc.Dot(1, 2, Star).Dot(14, 1, Gold);
		int gcCheer = F(cc);
		Frame Coin(int spin)
		{
			var f = new Frame();
			switch (spin)
			{
				case 0: f.Stamp(6, 12, ".YY.", "YeYe", "YeYe", ".ee."); break;
				case 1: f.Stamp(7, 12, "Y.", "Ye", "Ye", "e."); f.Dot(8, 12, Gold).Dot(8, 15, Gold); break;
				default: f.Rect(7, 12, 1, 4, Gold).Dot(7, 12, Star); break;
			}
			return f;
		}
		int coin0 = F(Coin(0)), coin1 = F(Coin(1)), coin2 = F(Coin(2));
		k.Lead(121, "gold coins", 160, 168, (k.Stand, 1), (gcEar, 4), (gcPull, 3), (gcShow, 4), (k.Stand, 1));
		k.Show(168, "coin rain", 150, (gcLook, 4), (gcCheer, 2), (gcLook, 2), (gcCheer, 2), (gcLook, 2), (gcCheer, 3), (k.Happy, 2), (k.Stand, 2));
		foreach (var (x, y) in new[] { ("imageX-imageW*5/8", "imageY-imageH*3/2"), ("imageX-imageW/4", "imageY-imageH*2"), ("imageX+imageW/4", "imageY-imageH*7/4"),
			("imageX+imageW*5/8", "imageY-imageH*5/4"), ("imageX-imageW", "imageY-imageH*9/4"), ("imageX+imageW", "imageY-imageH*11/4") })
			k.Child(168, 248, x, y);
		p.A(248, "coin falls").Move("0", "7").Speed("45").Loop("12", 0).Seq(coin0, coin1, coin2, coin1).Then(100, 249)
			.OnBorder(100, 249, "taskbar").OnBorder(100, 249, "window");
		p.A(249, "coin bounce").Move("0", "-4", "0", "4").Speed("60").Fade(1, 0.15).Seq(coin0, coin1, coin2, coin1, coin0, coin1, coin2, coin1, coin0);
		k.Sounds.Add(new PetSound(121, 100, new Sfx(1.4, 363).Bell(0.9, 2637, 0.3, 0.35).Bell(0.95, 3520, 0.15, 0.25)));
		{
			var rain = new Sfx(2.4, 364);
			var r = new Random(364);
			for (int i = 0; i < 9; i++) rain.Bell(0.4 + i * 0.17 + r.NextDouble() * 0.08, 2300 + r.Next(1300), 0.18, 0.18);
			k.Sounds.Add(new PetSound(168, 100, rain));
		}

		// ================================================================ 122 flying books (child: the shelf, which opens the books)
		// shelf of spell books x 2..13, plank y 9; books stand on it
		char[] bookCol = { Red, Red, Violet, Violet, Green, Red, Red, Sky, Sky, OwlBrown };   // columns 3..12
		Frame Shelf(int stage)          // 0 sparkles, 1 half, 2 full, 3 three books gone
		{
			var f = new Frame();
			f.Rect(2, 9, 12, 1, Wood).Rect(2, 10, 12, 1, BrownDark).Stamp(3, 11, "B", "").Stamp(12, 11, "B");
			for (int i = 0; i < bookCol.Length; i++)
			{
				int x = 3 + i;
				if (stage == 3 && (x == 3 || x == 4 || x == 5 || x == 6 || x == 8 || x == 9)) continue;
				int top = i == 4 ? 6 : (i == 9 ? 6 : 5);
				f.Rect(x, top, 1, 9 - top, bookCol[i]);
				if (i % 2 == 0 && i != 4) f.Dot(x, top + 1, Gold);
			}
			if (stage == 3) f.Dot(5, 8, Paper);
			if (stage <= 1)
			{
				for (int y = 0; y <= 15; y++) for (int x = 0; x < 16; x++)
					if (f[x, y] != '.' && (x * 5 + y * 3) % 4 < (stage == 0 ? 3 : 1)) f[x, y] = '.';
				Sparkle(f, 4, 6, Star); Sparkle(f, 11, 4, Cyan);
				if (stage == 0) Sparkle(f, 8, 11, Magenta);
			}
			return f;
		}
		int shS = F(Shelf(0)), shH = F(Shelf(1)), shF = F(Shelf(2)), shE = F(Shelf(3));
		// a flying book: x = left column of the book on the shelf (2 wide), pose 0 standing, 1 wings up, 2 flat, 3 wings down
		Frame Book(int x, char c, int pose)
		{
			var f = new Frame();
			switch (pose)
			{
				case 0: f.Rect(x, 5, 2, 4, c).Dot(x, 6, Gold); break;
				case 1: f.Stamp(x - 2, 5, "c....c".Replace('c', c), "cc..cc".Replace('c', c), ".cppc.".Replace('c', c)); break;
				case 2: f.Stamp(x - 2, 6, ".pppp.", "cccccc".Replace('c', c)); break;
				default: f.Stamp(x - 2, 6, ".cppc.".Replace('c', c), "cc..cc".Replace('c', c), "c....c".Replace('c', c)); break;
			}
			return f;
		}
		int bkR0 = F(Book(3, Red, 0)), bkR1 = F(Book(3, Red, 1)), bkR2 = F(Book(3, Red, 2)), bkR3 = F(Book(3, Red, 3));
		int bkV0 = F(Book(5, Violet, 0)), bkV1 = F(Book(5, Violet, 1)), bkV2 = F(Book(5, Violet, 2)), bkV3 = F(Book(5, Violet, 3));
		var bc = Base(); Eyes(bc, 2); Arm(bc, -1, 1); WandUp(bc, true);
		int fbConduct = F(bc);
		var bc2 = Base(); Eyes(bc2, 2); Arm(bc2, -1, 2); WandUp(bc2, false); Sparkle(bc2, 0, 5, Star);
		int fbConduct2 = F(bc2);
		var bu = Base(); Eyes(bu, 4); Arm(bu, -1, 0); WandDown(bu);
		int fbWatch = F(bu);
		k.Lead(122, "flying books", 150, 1, (k.Stand, 1), (fbConduct, 3), (fbConduct2, 3), (fbConduct, 3), (fbConduct2, 3), (fbWatch, 5), (k.Surprise, 3),
			(fbWatch, 2), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(122, 250, "imageX-imageW*3/4");
		p.A(250, "spell shelf").Move("0", "0").Speed("150").Seq(shS, shS, shH, shF, shF, shF, shF, shF).Then(100, 251);
		p.A(251, "shelf empties").Move("0", "0").Speed("150").Fade(1, 0).Seq(shE, shE, shE, shE, shE, shE, shE, shE, shE, shE, shE, shE);
		k.Child(251, 252);
		k.Child(251, 252, "imageX+imageW*5/16");
		k.Child(251, 253);
		p.A(252, "book swoops").Move("5", "-5", "5", "4").Speed("70").Loop("5", 0).Fade(1, 0.2).Seq(bkR0, bkR1, bkR2, bkR3);
		p.A(253, "book flaps away").Move("-3", "-4").Speed("80").Loop("12", 0).Seq(bkV0, bkV1, bkV2, bkV3, bkV2);
		{
			var fl2 = new Sfx(2.0, 365);
			for (int i = 0; i < 9; i++) fl2.Noise(1.2 + i * 0.13, 0.07, 0.25, 1800, 600, 0.01, 0.05);
			fl2.Bell(0.2, 1175, 0.2, 0.4).Bell(0.35, 1568, 0.18, 0.4);
			k.Sounds.Add(new PetSound(122, 100, fl2));
		}

		// ================================================================ 123 smoke rings (children)
		Frame Pipe(int puff)            // 0 holding, 1 drawing (ember bright), 2 blowing
		{
			var f = Base(); Eyes(f, puff == 1 ? 1 : (puff == 2 ? 2 : 0)); Arm(f, -1, 0);
			Line(f, 8, 7, 11, 8, Wood);
			f.Stamp(12, 6, "w.w", "www", ".w.").Dot(13, 6, puff == 1 ? Star : Ember);
			if (puff == 1) f.Dot(13, 5, Orange);
			f.Dot(14, 8, Skin).Rect(14, 9, 1, 2, Robe);
			if (puff == 0) f.Dot(13, 4, Smoke).Dot(14, 3, LightGray);
			if (puff == 2) { f.Dot(6, 7, Ink).Dot(5, 7, LightGray).Dot(4, 6, LightGray); }
			return f;
		}
		int pp0 = F(Pipe(0)), pp1 = F(Pipe(1)), pp2 = F(Pipe(2));
		Frame Ring(int size)
		{
			var f = new Frame();
			switch (size)
			{
				case 0: f.Stamp(2, 4, ".q.", "q.q", ".q."); break;
				case 1: f.Stamp(1, 3, ".qq.", "q..q", "q..q", ".qq."); break;
				case 2: f.Stamp(1, 2, ".hhh.", "h...h", "h...h", "h...h", ".hhh."); break;
				default: f.Stamp(0, 1, "..h.h.", ".....h", "h.....", ".....h", "h.....", ".h.h.."); break;
			}
			return f;
		}
		int rg0 = F(Ring(0)), rg1 = F(Ring(1)), rg2 = F(Ring(2)), rg3 = F(Ring(3));
		k.Lead(123, "smoke rings", 170, 166, (k.Stand, 1), (pp0, 4), (pp1, 3));
		// each puff part starts as he blows: the ring (child) leaves the mouth then; the puff repeats a few times
		var puff = p.A(166, "smoke ring puff").Move("0", "0").Speed("170").Seq(pp2, pp2, pp2, pp0, pp0, pp0, pp1, pp1, pp1);
		puff.Then(55, 166).Then(45, 167);
		k.Show(167, "last smoke ring", 170, (pp2, 3), (pp0, 4), (k.Blink, 1), (k.Happy, 2), (k.Stand, 2));
		p.A(254, "smoke ring").Move("-1", "-3").Speed("130").Fade(1, 0).Seq(rg0, rg0, rg1, rg1, rg2, rg2, rg2, rg3, rg3, rg3);
		k.Child(166, 254);
		k.Child(167, 254);
		foreach (var (a, seed) in new[] { (166, 367), (167, 368) })
			k.Sounds.Add(new PetSound(a, 100, new Sfx(1.0, seed).Noise(0, 0.45, 0.3, 900, 400, 0.05, 0.25)));
		k.Sounds.Add(new PetSound(123, 100, new Sfx(1.4, 366).Noise(0.75, 0.45, 0.2, 600, 300, 0.1, 0.2).Tone(0.75, 0.4, 120, 110, 0.1)));

		// ================================================================ 124 pumpkin (child: it grows beside him)
		Frame Pumpkin(int stage)
		{
			var f = new Frame();
			switch (stage)
			{
				case 0: break;
				case 1: Sparkle(f, 3, 9, Star); f.Dot(4, 12, Wood); break;
				case 2: f.Dot(5, 15, Wood).Dot(3, 13, Cyan).Dot(7, 12, Star); break;
				case 3: f.Dot(5, 15, DarkGreen).Dot(5, 14, Green).Dot(6, 13, Leaf); break;
				case 4: f.Rect(5, 12, 1, 4, Green).Dot(6, 12, Leaf).Dot(4, 13, Leaf).Dot(6, 11, Leaf); break;
				case 5: f.Rect(3, 15, 6, 1, Green).Dot(4, 14, Leaf).Stamp(6, 13, ".d.", "OOO", "OJO").Rect(6, 15, 3, 1, Orange); break;
				case 6: f.Rect(2, 15, 3, 1, Green).Dot(3, 14, Leaf).Stamp(5, 11, "..d..", ".OdO.", "OJOJO", "OJOJO", ".OOO."); break;
				default:
					f.Stamp(3, 9, "....dL...", ".OOJdJOO.", "OOJOOOJOO", "OJOOOOOJO", "OJOOOOOJO", "OOJOOOJOO", ".OOJJJOO.");
					f.Dot(5, 11, Star).Dot(4, 12, Star).Dot(2, 15, Green).Dot(1, 15, Leaf);
					if (stage == 8) { Squash(f, 1); f.Dot(12, 9, Ink).Dot(13, 8, Ink); }
					break;
			}
			return f;
		}
		var pk = new int[9];
		for (int i = 0; i < 9; i++) pk[i] = F(Pumpkin(i));
		var pc = Base(); Eyes(pc, 3); Arm(pc, -1, 0); Arm(pc, 1, 2); pc.Rect(15, 9, 1, 3, Wood).Dot(15, 12, Star);
		int pkPoint = F(pc);
		var pcs = pc.Clone(); Sparkle(pcs, 14, 13, Star); pcs.Dot(15, 14, Cyan);
		int pkSpark = F(pcs);
		int pkWatch = F(Stand(3));
		var pw = Base(); Eyes(pw, 4); Mouth(pw); Arm(pw, -1, 1); WandDown(pw);
		int pkWow = F(pw);
		var ka = Base(); Eyes(ka, 3); Arm(ka, -1, 0); ka.Rect(14, 9, 1, 2, Robe).Dot(15, 11, Skin).Dot(15, 10, Robe);
		int pkKnock = F(ka);
		var kb = Base(); Eyes(kb, 3); Arm(kb, -1, 0); kb.Rect(14, 8, 1, 2, Robe).Dot(14, 7, Skin);
		int pkKnockUp = F(kb);
		k.Show(124, "pumpkin", 150, (k.Stand, 1), (pkPoint, 2), (pkSpark, 4), (pkWatch, 8), (pkWow, 3),
			(pkKnockUp, 1), (pkKnock, 1), (pkKnockUp, 1), (pkKnock, 1), (k.Happy, 3), (k.Stand, 2));
		k.Child(124, 255, "imageX+imageW*3/4");
		p.A(255, "pumpkin grows").Move("0", "0").Speed("150").Seq(pk[0], pk[0], pk[0], pk[1], pk[2], pk[2], pk[3], pk[3], pk[4], pk[4], pk[5], pk[5], pk[6], pk[6], pk[7], pk[7]).Then(100, 256);
		p.A(256, "pumpkin knocked").Move("0", "0").Speed("150").Seq(pk[7], pk[7], pk[7], pk[8], pk[7], pk[8], pk[7], pk[7]).Then(100, 257);
		p.A(257, "pumpkin fades").Move("0", "0").Speed("150").Fade(1, 0).Seq(pk[7], pk[7], pk[7], pk[7], pk[7], pk[7]);
		k.Sounds.Add(new PetSound(124, 100, new Sfx(3.4, 369).Notes(0.3, 0.07, 0.18, Sfx.Wave.Triangle, 76, 79, 83)
			.Tone(1.0, 1.2, 200, 500, 0.15, Sfx.Wave.Sine, 0.2, 0.3).Pop(2.2, 0.3, 400)
			.Thud(2.85, 0.5, 260, 120, 0.12).Thud(3.15, 0.5, 260, 120, 0.12)));

		// ================================================================ 125 long beard (grows to the floor, trimmed with big scissors)
		Frame LongBeard(int len, int scissors, int eyes)     // len 0 normal .. 3 to the floor, 4/5 curled; scissors 0 none, 1 open, 2 closed
		{
			var f = Base(); if (eyes == -1) Eyes0Down(f); else Eyes(f, eyes); Arm(f, -1, 0);
			if (len >= 1) { f.Rect(6, 12, 4, 1, Beard).Dot(6, 12, BeardShade).Dot(9, 12, BeardShade).Rect(7, 13, 2, 1, Beard).Dot(7, 14, BeardShade); }
			if (len >= 2) { f.Rect(6, 13, 4, 1, Beard).Dot(9, 13, BeardShade).Rect(6, 14, 4, 1, Beard).Dot(6, 14, BeardShade); }
			if (len >= 3) { f.Rect(6, 15, 4, 1, Beard).Dot(9, 15, BeardShade); }
			if (len >= 4) { f.Rect(3, 15, 3, 1, Beard).Dot(2, 15, BeardShade).Dot(1, 14, Beard).Dot(1, 15, BeardShade); }
			if (len >= 5) { f.Rect(0, 15, 3, 1, Beard).Dot(0, 14, Beard).Dot(0, 13, Beard).Dot(1, 12, Beard).Dot(2, 13, BeardShade); }
			if (scissors > 0)
			{
				f.Rect(14, 9, 1, 3, Robe).Dot(14, 12, Skin);
				f.Stamp(12, 12, "XX", "..", "XX").Dot(13, 13, Red).Dot(11, 13, Ink);
				if (scissors == 1) f.Stamp(7, 11, "Ss...", ".SSs.", "....K", ".SSs.", "Ss...");
				else f.Rect(7, 13, 4, 1, Stone).Dot(6, 13, StoneDark).Dot(11, 13, Ink);
			}
			else WandDown(f);
			return f;
		}
		int lbThink = k.Think1;
		int lb1 = F(LongBeard(1, 0, -1)), lb2 = F(LongBeard(2, 0, -1)), lb3 = F(LongBeard(3, 0, 4)), lb4 = F(LongBeard(4, 0, -1)), lb5 = F(LongBeard(5, 0, 4));
		var lbm = LongBeard(5, 0, 4); Mouth(lbm);
		int lbShock = F(lbm);
		int lbOpen = F(LongBeard(5, 1, 3)), lbSnip = F(LongBeard(5, 2, 1));
		// trimmed: normal beard again, the cut part lies on the floor
		Frame Trimmed(int stage)
		{
			var f = LongBeard(0, 0, stage == 0 ? 4 : 1);
			if (stage < 2) f.Rect(0, 15, 3, 1, Beard).Rect(6, 15, 4, 1, Beard).Rect(3, 15, 3, 1, '.').Dot(0, 14, Beard).Dot(1, 15, BeardShade).Dot(8, 14, Beard).Dot(9, 15, BeardShade);
			if (stage < 2) Shoes(f, "...UUU....UUU...");
			if (stage < 2) f.Rect(0, 15, 3, 1, Beard).Dot(1, 15, BeardShade).Dot(0, 14, Beard).Rect(6, 15, 4, 1, Beard).Dot(9, 15, BeardShade).Dot(7, 14, Beard);
			if (stage == 1) { Sparkle(f, 1, 13, Star); Sparkle(f, 8, 13, Cyan); }
			return f;
		}
		int lbCut = F(Trimmed(0)), lbPoof = F(Trimmed(1));
		k.Show(125, "long beard", 170, (k.Stand, 1), (lbThink, 2), (k.Think2, 2), (lb1, 3), (lb2, 3), (lb3, 3), (lb4, 3), (lb5, 3), (lbShock, 3), (k.Blink, 1),
			(lbOpen, 3), (lbSnip, 2), (lbOpen, 2), (lbSnip, 2), (lbCut, 3), (lbPoof, 2), (k.Happy, 2), (k.Think1, 2), (k.Stand, 2));
		{
			var sn = new Sfx(3.6, 370).Tone(0.4, 1.4, 300, 700, 0.12, Sfx.Wave.Triangle, 0.2, 0.3, vibHz: 8, vibDepth: 0.03);
			foreach (double t in new[] { 2.55, 2.9, 3.25 }) sn.Click(t, 0.7).Tone(t, 0.04, 3000, 2500, 0.15);
			k.Sounds.Add(new PetSound(125, 100, sn));
		}

		// ================================================================ 126 star catcher (children: the star falls into the jar; later it goes up again)
		Frame Jar(int star, int eyes)     // star 0 empty, 1 glowing, 2 glowing bright, 3 open (star leaves)
		{
			var f = Base(); Eyes(f, eyes); Arm(f, -1, 1); WandDown(f);
			f.Stamp(0, 0, ".hh.", "b..b", "b..b", "bbbb").Dot(1, 4, Skin);
			f.Dot(1, 1, GlassLight).Dot(2, 2, GlassLight).Dot(1, 2, GlassLight).Dot(2, 1, GlassLight);
			if (star == 3) f.Dot(1, 0, '.').Dot(2, 0, '.');
			if (star == 1 || star == 2)
			{
				f.Rect(1, 1, 2, 2, Star).Dot(1, 1, HotWhite);
				Glow(f, PaleYellow, star == 1 ? new[] { (0, -1), (4, 0), (3, -2) } : new[] { (3, -1), (0, -2), (4, 2), (1, -3) });
				f.Dot(0, 0, PaleYellow).Dot(3, 0, PaleYellow);
			}
			return f;
		}
		int jEmpty = F(Jar(0, 4)), jGlow1 = F(Jar(1, 0)), jGlow2 = F(Jar(2, 0)), jLove = F(Jar(2, 1)), jOpen = F(Jar(3, 4));
		Frame StarSp(int tw)
		{
			var f = new Frame();
			f.Stamp(0, 0, ".Y.", "YHY", ".Y.").Dot(1, 1, HotWhite);
			if (tw == 0) f.Dot(3, -2, PaleYellow).Dot(4, -3, PaleYellow);
			else f.Dot(4, -2, PaleYellow).Dot(3, -1, PaleYellow).Dot(5, -4, PaleYellow);
			return f;
		}
		Frame StarUp(int tw)
		{
			var f = new Frame();
			f.Stamp(0, 0, ".Y.", "YHY", ".Y.").Dot(1, 1, HotWhite);
			f.Dot(1, 3 + tw, PaleYellow).Dot(tw == 0 ? 0 : 2, 5, PaleYellow);
			return f;
		}
		int st0 = F(StarSp(0)), st1 = F(StarSp(1)), su0 = F(StarUp(0)), su1 = F(StarUp(1));
		var jw = Base(); Eyes(jw, 4); Arm(jw, -1, 1); Arm(jw, 1, 1); Mouth(jw);
		int jWave = F(jw);
		k.Lead(126, "star catcher", 120, 169, (k.Stand, 1), (jEmpty, 5), (jGlow1, 2), (jGlow2, 2), (jGlow1, 2), (jLove, 3), (jGlow2, 2), (jLove, 3), (jGlow1, 2));
		k.Show(169, "star set free", 140, (jOpen, 3), (k.Happy, 1), (jWave, 3), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Wave2, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(126, 258, "imageX+imageW/2", "imageY-imageH*6/5");
		p.A(258, "wish star falls").Move("-2", "6").Speed("60").Loop("5", 0).Seq(st0, st1);
		k.Child(169, 259);
		p.A(259, "jar star rises").Move("1", "-5").Speed("70").Loop("40", 0).Seq(su0, su1);
		k.Sounds.Add(new PetSound(126, 100, new Sfx(1.8, 371).Tone(0, 0.7, 2200, 700, 0.15, Sfx.Wave.Sine, 0.05, 0.1).Bell(0.72, 1568, 0.3, 0.6).Bell(0.8, 2349, 0.2, 0.5)));
		k.Sounds.Add(new PetSound(169, 100, new Sfx(1.2, 372).Notes(0, 0.08, 0.2, Sfx.Wave.Sine, 84, 88, 91, 96, 100)));
	}
}
