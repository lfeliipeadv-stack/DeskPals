namespace PetGen;

static partial class MagoPet
{
	/// <summary>
	/// Scenes 127-139 (their second parts 170-179, their child windows 260-279): runes, cookies, sleepwalk,
	/// magic springs, old man walk, knitting, star fishing, hopscotch, sparkle hiccups, portal, spirit friend,
	/// exercise and the message in a bottle.
	/// </summary>
	static void ScenesC(Kit k)
	{
		var p = k.P;
		int F(Frame f) => k.F(f);
		int Count(params (int f, int n)[] steps) => steps.Sum(s => s.n);
		void EyesDown(Frame f) { f.Dot(5, 5, Skin).Dot(10, 5, Skin).Dot(5, 6, Ink).Dot(10, 6, Ink); }
		// a child that waits, invisible, `steps` steps of `ms` (so it starts in time with the parent), then goes on
		void Delay(int id, string name, int ms, int steps, int next) =>
			p.A(id, name).Move("0", "0").Speed(ms.ToString()).Seq(Enumerable.Repeat(k.Empty, steps).ToArray()).Then(100, next);
		// only where nothing is drawn yet (behind the wizard)
		void Behind(Frame f, int x, int y, char c) { if (f[x, y] == '.') f[x, y] = c; }

		// ================================================================ 127 runes
		// He sits in the air, cross-legged, and three glowing runes circle him like a ring (behind him at the top
		// of the ring, in front at the bottom). When he lands, the runes (a child) rise up and fade.
		char[] runeC = { Cyan, Violet, Magenta };
		string[][] runeG = { new[] { "CC", "C.", "CC" }, new[] { "C.", "CC", ".C" }, new[] { ".C", "CC", "C." } };
		void Runes(Frame f, int ph, bool all)
		{
			for (int i = 0; i < 3; i++)
			{
				double ang = 2 * Math.PI * (ph + i * 4) / 12.0;
				int cx = 8 + (int)Math.Round(6.5 * Math.Cos(ang)) - 1, cy = 7 + (int)Math.Round(3.4 * Math.Sin(ang)) - 1;
				bool front = all || Math.Sin(ang) > 0.1;
				for (int j = 0; j < 3; j++)
					for (int x = 0; x < 2; x++)
						if (runeG[i][j][x] == 'C' && (front || f[cx + x, cy + j] == '.')) f[cx + x, cy + j] = runeC[i];
			}
		}
		Frame Medit(int bob)
		{
			var f = Base(); Eyes(f, 1); Arm(f, -1, 2); Arm(f, 1, 2);
			Squash(f, 3); Shoes(f, ".UUU........UUU.");
			f.Shift(0, -2 + bob);
			f.Dot(5 + bob, 15, Cyan).Dot(8, 15, PaleYellow).Dot(10 - bob, 15, Magenta);   // a faint glow under him
			return f;
		}
		int rnIntro = F(Medit(0));
		var rnOrbit = new int[12];
		for (int ph = 0; ph < 12; ph++) { var f = Medit((ph / 3) % 2); Runes(f, ph, false); rnOrbit[ph] = F(f); }
		var rnFly = new int[4];
		for (int ph = 0; ph < 4; ph++) { var f = new Frame(); Runes(f, ph, true); rnFly[ph] = F(f); }
		var runeSteps = new List<(int, int)> { (k.Stand, 2), (k.Sit, 2), (rnIntro, 2) };
		for (int r = 0; r < 2; r++) foreach (int fr in rnOrbit) runeSteps.Add((fr, 1));
		int runeBefore = Count(runeSteps.ToArray());
		runeSteps.AddRange(new[] { (k.Sit, 2), (k.Happy, 2), (k.Stand, 1) });
		k.Show(127, "runes", 130, runeSteps.ToArray());
		Delay(260, "runes delay", 130, runeBefore, 261);
		p.A(261, "runes fly").Move("0", "-3").Speed("110").Loop("5", 0).Fade(1, 0).Seq(rnFly);
		k.Child(127, 260);

		// ================================================================ 128 cookies
		// He sits down with cookies and a glass of milk, munches (crumbs fall on the floor), drinks, and when he
		// stands up the last cookie falls and rolls away (a child).
		Frame SitBase(int eyes)
		{
			var f = Base(); Eyes(f, eyes); Squash(f, 3); Shoes(f, ".UUU........UUU.");
			return f;
		}
		void Cookie(Frame f, int x, int y, int bite)
		{
			f.Stamp(x, y, "eie", "iBi", "eiB");
			if (bite >= 1) f.Dot(x, y + 1, '.').Dot(x, y, '.');
			if (bite >= 2) f.Dot(x + 1, y + 1, '.').Dot(x, y + 2, '.');
		}
		void Milk(Frame f, int x, int y) { f.Stamp(x, y, "bl", "WW", "Wh", "bb"); }
		void Crumbs(Frame f, params (int x, int y)[] c) { foreach (var (x, y) in c) f.Dot(x, y, (x + y) % 2 == 0 ? Gold : OwlLight); }
		Frame Munch(int eyes, int cookieAt, int bite, bool chew, params (int, int)[] crumbs)
		{
			var f = SitBase(eyes);
			Milk(f, 0, 9); f.Dot(2, 11, Skin);                                       // milk in the left hand
			if (cookieAt == 0) { Cookie(f, 11, 10, bite); f.Dot(13, 13, Skin); }   // cookie held low
			else { Cookie(f, 9, 9, bite); f.Dot(12, 11, Skin).Dot(13, 12, Robe); } // cookie at the mouth
			if (chew) f.Dot(7, 10, Ink);
			Crumbs(f, crumbs);
			return f;
		}
		int ckHold = F(Munch(0, 0, 0, false));
		int ckBite1 = F(Munch(1, 1, 1, false, (8, 12)));
		int ckChew1 = F(Munch(1, 0, 1, true, (7, 13), (9, 12)));
		int ckChew2 = F(Munch(0, 0, 1, false, (7, 14), (9, 13), (6, 15)));
		int ckBite2 = F(Munch(1, 1, 2, false, (6, 15), (9, 15), (8, 12)));
		int ckChew3 = F(Munch(1, 0, 2, true, (6, 15), (9, 15), (8, 14), (5, 13)));
		var dr = SitBase(1); dr.Stamp(5, 7, "bl", "WW", "Wh", "bb").Dot(4, 10, Skin).Dot(3, 11, Robe);
		Cookie(dr, 11, 10, 2); dr.Dot(13, 13, Skin); Crumbs(dr, (6, 15), (9, 15), (8, 15), (5, 15));
		int ckDrink = F(dr);
		var ah = Munch(1, 0, 2, false, (6, 15), (9, 15), (8, 15), (5, 15)); Clear(ah, 9, 9); ah.Stamp(0, 9, "...gWWWPPWWWg...");
		ah.Dot(7, 10, Pink).Dot(8, 10, Pink); Sparkle(ah, 13, 5, Star);
		int ckAhh = F(ah);
		int ckNew = F(Munch(3, 0, 0, false, (6, 15), (9, 15), (8, 15), (5, 15)));
		var ckLook = Stand(3); Crumbs(ckLook, (0, 15), (1, 15), (7, 15));
		int ckLookF = F(ckLook);
		var ckOops = Stand(4); Mouth(ckOops); Crumbs(ckOops, (0, 15), (1, 15), (7, 15));
		int ckOopsF = F(ckOops);
		var ckRoll = new int[4];
		for (int r = 0; r < 4; r++)
		{
			var f = new Frame();
			f.Rect(13, 13, 3, 3, Gold);
			if (r % 2 == 0) f.Dot(13, 13, '.').Dot(15, 15, '.'); else f.Dot(15, 13, '.').Dot(13, 15, '.');
			var (cx, cy) = r switch { 0 => (15, 14), 1 => (14, 15), 2 => (13, 14), _ => (14, 13) };
			f.Dot(14, 14, BrownDark).Dot(cx, cy, BrownDark).Dot(r % 2 == 0 ? 14 : 13, r % 2 == 0 ? 13 : 14, OwlLight);
			ckRoll[r] = F(f);
		}
		var cookieSteps = new (int, int)[]
		{
			(k.Sit, 2), (ckHold, 3), (ckBite1, 2), (ckChew1, 2), (ckChew2, 2), (ckBite2, 2), (ckChew3, 2), (ckChew2, 1), (ckDrink, 4), (ckAhh, 3), (ckNew, 2),
		};
		int cookieBefore = Count(cookieSteps);
		k.Show(128, "cookies", 160, cookieSteps.Concat(new[] { (ckLookF, 3), (ckOopsF, 3), (k.Blink, 1), (k.Stand, 2) }).ToArray());
		Delay(262, "cookie delay", 160, cookieBefore, 263);
		p.A(263, "cookie roll").Move("3", "0").Speed("90").Loop("6", 0).Seq(ckRoll).Then(100, 264);
		p.A(264, "cookie stop").Move("0", "0").Speed("200").Seq(ckRoll[0], ckRoll[0], ckRoll[0], ckRoll[0]);
		k.Child(128, 262);

		// ================================================================ 129 sleepwalk
		// In his striped nightcap, eyes closed, arms out, he walks slowly; a "Z" floats off behind him (a child),
		// he turns around at the edges and wakes up with a start at the end.
		void Nightcap(Frame f)
		{
			NoHat(f);
			f.Stamp(0, -1,
				".......uu.......",
				"......uuWu......",
				".....uuuuWuu....",
				".....uWuuuuWuu..",
				"....hhhhhhhh.uu.",
				".............WW.",
				".............Wh.");		}
		Frame SleepWalk(int ph)
		{
			var f = Base(); Nightcap(f); Eyes(f, 1); Arm(f, -1, 2); Arm(f, 1, 2); f.Dot(8, 7, Ink);
			if (ph == 1) { Shoes(f, "..UUU......UU..."); }
			if (ph == 2) { Shoes(f, "...UU.....UUU..."); Squash(f, 1); Shoes(f, "...UU.....UUU..."); }
			if (ph == 3) { Shoes(f, "....UU....UUU..."); }
			if (ph == 4) { Shoes(f, "..UUU.....UU...."); Squash(f, 1); Shoes(f, "..UUU.....UU...."); }
			return f;
		}
		int sw1 = F(SleepWalk(1)), sw2 = F(SleepWalk(2)), sw3 = F(SleepWalk(3)), sw4 = F(SleepWalk(4));
		var wk1 = Base(); Nightcap(wk1); Eyes(wk1, 4); Mouth(wk1); Arm(wk1, -1, 1); Arm(wk1, 1, 1);
		int swWake = F(wk1);
		var wk2 = Base(); Eyes(wk2, 4); Mouth(wk2); Arm(wk2, -1, 1); Arm(wk2, 1, 1); NoHat(wk2);
		Sparkle(wk2, 4, 0, Cyan); Sparkle(wk2, 11, -1, Star); Sparkle(wk2, 8, -3, Magenta);
		int swPoof = F(wk2);
		Frame Zzz(int k2)
		{
			var f = new Frame();
			f.Rect(12, -4, 3, 1, Zz).Dot(13, -3, Zz).Rect(12, -2, 3, 1, Zz);
			f.Rect(k2 == 0 ? 10 : 11, 0, 2, 1, Zz).Dot(k2 == 0 ? 10 : 11, 1, Zz).Rect(k2 == 0 ? 10 : 11, 1, 2, 1, Zz).Dot(k2 == 0 ? 11 : 12, 0, Zz);
			return f;
		}
		int zz1 = F(Zzz(0)), zz2 = F(Zzz(1));
		p.A(129, "sleepwalk").Move("-1", "0").Speed("230").Loop("random/4+6", 0)
			.Seq(sw1, sw2, sw3, sw4)
			.Then(100, 176)
			.OnBorder(100, 175).OnGravity(100, 5);
		var swTurn = p.A(175, "sleepwalk turn").Move("0", "0").Speed("230").Seq(sw1, sw3);
		swTurn.Action = "flip";
		swTurn.Then(100, 129);
		k.Show(176, "sleepwalk wake", 150, (sw1, 2), (swWake, 3), (swPoof, 2), (k.Surprise, 1), (k.Blink, 2), (k.Stand, 2));
		p.A(265, "zz drift").Move("1", "-1").Speed("200").Loop("5", 0).Fade(1, 0).Seq(zz1, zz2);
		k.Child(129, 265);

		// ================================================================ 130 magic springs
		// A tap of the wand and springs pop under his shoes; he boings along the floor, and at the end the springs
		// spring off on their own (a child) and he lands.
		Frame Spring(int dy, int eyes)
		{
			var src = Base(); Eyes(src, eyes); Arm(src, -1, 1); Arm(src, 1, 1);
			if (eyes == 1) src.Rect(7, 8, 2, 1, Ink);
			var f = new Frame();
			for (int y = -4; y <= 10; y++) for (int x = 0; x < 16; x++) if (src[x, y] != '.') f[x, y + dy] = src[x, y];
			for (int x = 0; x < 16; x++) { if (src[x, 14] != '.') f[x, 11 + dy] = src[x, 14]; if (src[x, 15] != '.') f[x, 12 + dy] = src[x, 15]; }
			for (int y = 13 + dy; y <= 15; y++)
				foreach (int sx in new[] { 3, 10 }) f.Stamp(sx, y, (y - dy) % 2 == 0 ? "ee." : ".ee");
			return f;
		}
		Frame SpringsAlone(int rows)
		{
			var f = new Frame();
			for (int y = 16 - rows; y <= 15; y++) foreach (int sx in new[] { 3, 10 }) f.Stamp(sx, y, y % 2 == 0 ? "ee." : ".ee");
			f.Rect(3, 15 - rows, 3, 1, Shoe).Rect(10, 15 - rows, 3, 1, Shoe);
			return f;
		}
		var spTap = Base(); Eyes(spTap, 2); Arm(spTap, -1, 0); spTap.Rect(14, 8, 1, 3, Robe).Dot(14, 11, Skin); Line(spTap, 14, 12, 13, 14, Wood); spTap.Dot(12, 15, Star);
		Sparkle(spTap, 4, 13, Cyan); Sparkle(spTap, 11, 12, Magenta);
		int spTapF = F(spTap);
		int spLow = F(Spring(2, 4)), spMid = F(Spring(1, 0)), spHigh = F(Spring(0, 1));
		var spHigh2 = Spring(0, 1); spHigh2.Dot(0, 12, LightGray).Dot(15, 11, LightGray).Dot(1, 14, LightGray).Dot(14, 13, LightGray);
		int spHighF = F(spHigh2);
		int spFly1 = F(SpringsAlone(3)), spFly2 = F(SpringsAlone(2));
		k.Lead(130, "magic springs", 120, 177, (k.Cast1, 2), (spTapF, 3), (spLow, 2), (spHigh, 1), (spMid, 1), (spLow, 2));
		p.A(177, "spring bounce").Move("-4", "0").Speed("90").Loop("random/3+5", 0)
			.Seq(spLow, spMid, spHigh, spHighF, spMid, spLow)
			.Then(100, 178)
			.OnBorder(100, 178).OnGravity(100, 5);
		k.Show(178, "springs off", 140, (spLow, 1), (k.Land, 2), (k.Surprise, 3), (k.Blink, 1), (k.Stand, 2));
		p.A(266, "springs fly").Move("2", "-5", "2", "3").Speed("70").Loop("6", 0).Fade(1, 0).Seq(spFly1, spFly2);
		k.Child(178, 266);

		// ================================================================ 131 old man walk
		// Hunched over his staff he shuffles along slowly, then stops, rubs his aching back, stretches and sighs.
		Frame Old(int ph)
		{
			var f = Base(); Eyes(f, 1); Arm(f, -1, 0);
			f.Dot(4, 4, BeardShade).Dot(11, 4, BeardShade);
			Squash(f, 2);
			Shoes(f, ph % 2 == 0 ? "...UUU....UUU..." : (ph == 1 ? "....UUU...UUU..." : "...UUU...UUU...."));
			if (ph == 1 || ph == 3) { Squash(f, 1); Shoes(f, ph == 1 ? "....UUU...UUU..." : "...UUU...UUU...."); }
			int top = ph == 1 || ph == 3 ? 7 : 6;
			f.Rect(15, top, 1, 16 - top, Wood).Dot(15, top - 1, Star);                     // the wand as a walking staff
			f.Rect(14, top + 3, 1, 2, Robe).Dot(15, top + 4, Skin);
			return f;
		}
		int old1 = F(Old(0)), old2 = F(Old(1)), old3 = F(Old(2)), old4 = F(Old(3));
		Frame Rub(int k2)
		{
			var f = Base(); Eyes(f, 1); Mouth(f);
			f.Rect(15, 5, 1, 11, Wood).Dot(15, 4, Star).Rect(14, 8, 1, 2, Robe).Dot(15, 9, Skin);
			f.Dot(1, 9, Robe).Dot(1, 10, Robe).Dot(2, 11 + k2, Skin);                     // hand on the small of the back
			f.Dot(0, 11 + k2, LightGray).Dot(0, 13 - k2, LightGray);                      // ouch
			return f;
		}
		int rub1 = F(Rub(0)), rub2 = F(Rub(1));
		var str = Base(); Eyes(str, 1); Arm(str, -1, 1); Arm(str, 1, 1); str.Rect(7, 7, 2, 2, Ink); str.Shift(0, 0);
		int oldStretch = F(str);
		var sigh = Stand(1); sigh.Dot(1, 6, Smoke).Dot(0, 5, Smoke).Dot(1, 4, LightGray);
		int oldSigh = F(sigh);
		p.A(131, "old man walk").Move("-1", "0").Speed("260").Loop("random/3+3", 0)
			.Seq(old1, old2, old3, old4)
			.Then(100, 179)
			.OnBorder(100, 2).OnGravity(100, 5);
		var br = k.Show(179, "back rub", 200, (old1, 2), (rub1, 2), (rub2, 2), (rub1, 2), (rub2, 2), (oldStretch, 4), (oldSigh, 3), (k.Stand, 2));
		br.Next.Clear(); br.Then(70, 1).Then(30, 131);

		// ================================================================ 132 knitting
		// Two needles clicking, a striped scarf grows down to the floor while the ball of yarn shrinks; then he
		// wraps it round his neck, happy, and it vanishes in a twinkle.
		Frame Knit(int len, int ph)
		{
			var f = Base(); EyesDown(f);
			f.Dot(1, 8, Robe).Dot(2, 9, RobeDark).Dot(3, 9, RobeDark).Dot(4, 9, Skin);          // left forearm
			f.Dot(14, 8, Robe).Dot(13, 9, RobeDark).Dot(12, 9, RobeDark).Dot(11, 9, Skin);      // right forearm
			if (ph == 0) { Line(f, 4, 7, 8, 10, Wood); Line(f, 11, 7, 7, 10, Wood); }
			else { Line(f, 3, 8, 8, 10, Wood); Line(f, 12, 8, 7, 10, Wood); }
			for (int j = 0; j < len; j++)
				for (int x = 6; x <= 9; x++) f[x, 10 + j] = (j / 2) % 2 == 0 ? Red : Star;
			int ball = len < 3 ? 3 : len < 5 ? 2 : 1;
			if (ball == 3) f.Stamp(13, 13, ".XX", "XTX", "XXX");
			else if (ball == 2) f.Stamp(13, 14, "XT", "XX");
			else f.Dot(14, 15, Red);
			f.Dot(12, 12, Red).Dot(11, 11, Red).Dot(11, 10, Red);                                 // the yarn
			return f;
		}
		var knitSteps = new List<(int, int)> { (k.Stand, 1) };
		for (int len = 0; len <= 6; len++)
		{
			int a = F(Knit(len, 0)), b = F(Knit(len, 1));
			knitSteps.Add((a, 1)); knitSteps.Add((b, 1)); knitSteps.Add((a, 1)); knitSteps.Add((b, 1));
		}
		Frame Wrapped(bool twinkle)
		{
			var f = Base(); Eyes(f, 1); Arm(f, -1, 0); Arm(f, 1, 0);
			for (int x = 3; x <= 12; x++) { f[x, 7] = x % 2 == 0 ? Red : Star; f[x, 8] = x % 2 == 0 ? Star : Red; }
			for (int y = 9; y <= 12; y++) { f[11, y] = (y / 2) % 2 == 0 ? Red : Star; f[12, y] = (y / 2) % 2 == 0 ? Red : Star; }
			f.Dot(11, 13, Red).Dot(12, 13, '.').Dot(12, 13, Red);
			if (twinkle) { Sparkle(f, 2, 6, Star); Sparkle(f, 13, 4, Cyan); Sparkle(f, 8, 10, Magenta); }
			return f;
		}
		var showOff = Knit(6, 0); Eyes(showOff, 4); showOff.Dot(5, 5, Ink).Dot(10, 5, Ink).Dot(5, 6, Beard).Dot(10, 6, Beard);
		int knShow = F(showOff), knWrap = F(Wrapped(false)), knTwinkle = F(Wrapped(true));
		knitSteps.AddRange(new[] { (knShow, 3), (knWrap, 6), (knTwinkle, 2), (k.Happy, 2), (k.Stand, 1) });
		k.Show(132, "knitting", 170, knitSteps.ToArray());

		// ================================================================ 133 star fishing
		// A fishing rod comes out, he casts the line up into the sky, waits, something bites, he reels in a star;
		// it dangles on the line, then it wriggles free and flies off (a child) and he waves it goodbye.
		Frame Fish(int eyes, int rod, int starY, bool reel)
		{
			var f = Base(); Eyes(f, eyes); Arm(f, -1, 0); Arm(f, 1, 2);
			if (rod == 0) f.Rect(15, -4, 1, 11, Wood);                                       // upright
			else if (rod == 1) Line(f, 15, 6, 12, -4, Wood);                                 // swung back
			else { f.Rect(15, -1, 1, 8, Wood).Dot(14, -2, Wood).Dot(13, -3, Wood); }        // bent: a bite!
			f.Dot(14, reel ? 5 : 6, LightGray).Dot(14, reel ? 6 : 5, Ash);                   // the reel
			if (rod == 2) f.Dot(12, -4, LightGray);
			if (starY > -99)
			{
				int cx = 13, cy = starY;
				if (cy >= -2) f.Dot(14, -3, LightGray);
				f.Dot(cx, cy - 1, Star).Dot(cx - 1, cy, Star).Dot(cx, cy, HotWhite).Dot(cx + 1, cy, Star).Dot(cx, cy + 1, Star);
			}
			return f;
		}
		const int NoStar = -100;
		int fsBack = F(Fish(3, 1, NoStar, false));
		var fsCast = Fish(3, 0, NoStar, false); Sparkle(fsCast, 13, -3, Cyan);
		int fsCastF = F(fsCast);
		int fsWait1 = F(Fish(3, 0, NoStar, false));
		var fsW2 = Fish(1, 0, NoStar, false); fsW2.Dot(15, -4, '.').Dot(14, -4, Wood);
		int fsWait2 = F(fsW2);
		int fsBite = F(Fish(4, 2, NoStar, true)), fsBite2 = F(Fish(4, 2, NoStar, false));
		int fsReel1 = F(Fish(4, 0, -5, true)), fsReel2 = F(Fish(4, 0, -3, false));
		var fsD1 = Fish(1, 0, -1, true); fsD1.Dot(11, -2, PaleYellow).Dot(15, 1, PaleYellow);
		int fsDangle1 = F(fsD1);
		var fsD2 = Fish(1, 0, -1, false); fsD2.Dot(13, -1, Star).Dot(12, -2, PaleYellow).Dot(14, 0, PaleYellow).Dot(12, 0, PaleYellow).Dot(14, -2, PaleYellow);
		int fsDangle2 = F(fsD2);
		int fsGone = F(Fish(4, 0, NoStar, false)), fsLook = F(Fish(3, 0, NoStar, false));
		Frame FlyStar(int k2)
		{
			var f = new Frame();
			f.Dot(13, -2, Star).Dot(12, -1, Star).Dot(13, -1, HotWhite).Dot(14, -1, Star).Dot(13, 0, Star);
			if (k2 == 1) f.Dot(11, -3, PaleYellow).Dot(15, -3, PaleYellow).Dot(11, 1, PaleYellow).Dot(15, 1, PaleYellow);
			else f.Dot(14, 1, Paper).Dot(15, 2, PaleYellow);
			return f;
		}
		int fsFly1 = F(FlyStar(0)), fsFly2 = F(FlyStar(1));
		var fishSteps = new (int, int)[]
		{
			(k.Stand, 2), (fsBack, 2), (fsCastF, 2), (fsWait1, 3), (fsWait2, 2), (fsWait1, 3), (fsWait2, 2), (fsBite, 2), (fsBite2, 1), (fsBite, 1),
			(fsReel1, 2), (fsReel2, 2), (fsDangle1, 2), (fsDangle2, 2), (fsDangle1, 2), (fsDangle2, 2),
		};
		int fishBefore = Count(fishSteps);
		k.Show(133, "star fishing", 150, fishSteps.Concat(new[] { (fsGone, 3), (fsLook, 2), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Wave2, 2), (k.Stand, 2) }).ToArray());
		Delay(267, "star delay", 150, fishBefore, 268);
		p.A(268, "star flies").Move("-2", "-4").Speed("80").Loop("14", 0).Fade(1, 0).Seq(fsFly1, fsFly2);
		k.Child(133, 267);

		// ================================================================ 134 hopscotch
		// With the wand pointed at the floor he draws three glowing squares, then hops through them on one foot;
		// the squares (a child) stay glowing where he drew them and every hop leaves sparkly footprints (children).
		void Grid(Frame f, int from, bool all)
		{
			for (int b = 0; b < 3; b++)
			{
				int x0 = 10 - b * 5;
				if (x0 < from) continue;
				for (int x = x0; x <= x0 + 5 && x <= 15; x++) { if (all) f[x, 14] = Cyan; else Behind(f, x, 14, Cyan); }
				foreach (int x in new[] { x0, Math.Min(15, x0 + 5) }) { if (all) f[x, 15] = Cyan; else Behind(f, x, 15, Cyan); }
				int sx = x0 + 2 + b % 2;
				if (all) f[sx, 15] = Star; else Behind(f, sx, 15, Star);
			}
		}
		Frame Draw(int from)
		{
			var f = Base(); EyesDown(f); Arm(f, -1, 0);
			f.Rect(14, 8, 1, 3, Robe).Dot(14, 11, Skin).Dot(15, 12, Wood).Dot(15, 13, Wood).Dot(15, 14, Star);
			Grid(f, from, false);
			if (from < 99) Sparkle(f, Math.Max(1, from), 13, Star);
			return f;
		}
		int hsAim = F(Draw(99)), hsDraw1 = F(Draw(10)), hsDraw2 = F(Draw(5)), hsDraw3 = F(Draw(0));
		var hsAdm = Base(); Eyes(hsAdm, 1); Arm(hsAdm, -1, 1); WandUp(hsAdm, false); Grid(hsAdm, 0, false);
		int hsAdmire = F(hsAdm);
		var gridA = new Frame(); Grid(gridA, 0, true);
		var gridB = gridA.Clone(); gridB.Dot(2, 15, Cyan).Dot(8, 15, Cyan).Dot(13, 15, Cyan).Dot(3, 15, Star).Dot(7, 15, Star).Dot(12, 15, Star);
		int hsGridA = F(gridA), hsGridB = F(gridB);
		Frame Hop(bool up)
		{
			var f = Base(); Eyes(f, 2); Arm(f, -1, 2); Arm(f, 1, 2);
			if (up) { Shoes(f, "..........UUU..."); f.Rect(3, 14, 3, 1, Shoe); }
			else { Squash(f, 1); Shoes(f, "...UUU....UUU..."); }
			return f;
		}
		int hopUp = F(Hop(true)), hopDown = F(Hop(false));
		var fp1 = new Frame(); fp1.Rect(3, 15, 3, 1, Cyan).Rect(10, 15, 3, 1, Cyan).Dot(4, 14, Star).Dot(11, 14, Star);
		var fp2 = new Frame(); fp2.Dot(3, 15, Cyan).Dot(5, 15, Cyan).Dot(4, 15, Star).Dot(10, 15, Cyan).Dot(12, 15, Cyan).Dot(11, 15, Star).Dot(4, 13, PaleYellow).Dot(11, 13, PaleYellow);
		int hsFoot1 = F(fp1), hsFoot2 = F(fp2);
		var hopSteps = new (int, int)[] { (k.Stand, 1), (hsAim, 2), (hsDraw1, 2), (hsDraw2, 2), (hsDraw3, 2) };
		int hopBefore = Count(hopSteps);
		k.Lead(134, "hopscotch", 140, 174, hopSteps.Concat(new[] { (hsAdmire, 3) }).ToArray());
		p.A(174, "hopscotch hop").Move("-5", "0").Speed("110").Seq(hopDown, hopUp, hopUp, hopDown)
			.Then(80, 174).Then(20, 1)
			.OnBorder(100, 2).OnGravity(100, 5);
		Delay(269, "grid delay", 140, hopBefore, 270);
		p.A(270, "hopscotch grid").Move("0", "0").Speed("160").Loop("10", 0).Fade(1, 0).Seq(hsGridA, hsGridB);
		p.A(271, "sparkle footprint").Move("0", "0").Speed("120").Loop("4", 0).Fade(1, 0).Seq(hsFoot1, hsFoot2);
		k.Child(134, 269);
		k.Child(174, 271);

		// ================================================================ 135 sparkle hiccups
		// Hic! — every hiccup spits out a little sparkle (a child) that floats up; three hiccups, then relief.
		var hic = Base(); Eyes(hic, 4); Mouth(hic); Arm(hic, -1, 3); WandDown(hic);
		hic.Dot(1, 2, LightGray).Dot(0, 3, LightGray).Dot(14, 2, LightGray).Dot(15, 3, LightGray);
		int hcHic = F(hic);
		var aft = Base(); Eyes(aft, 1); Arm(aft, -1, 3); WandDown(aft); aft.Dot(4, 5, Pink).Dot(11, 5, Pink);
		int hcAfter = F(aft);
		var hcSp1F = new Frame(); Sparkle(hcSp1F, 8, 7, Star);
		var hcSp2F = new Frame(); hcSp2F.Dot(8, 7, HotWhite).Dot(7, 6, Cyan).Dot(9, 6, Magenta).Dot(7, 8, Magenta).Dot(9, 8, Cyan);
		int hcSp1 = F(hcSp1F), hcSp2 = F(hcSp2F);
		k.Show(135, "sparkle hiccups", 120,
			(hcHic, 2), (hcAfter, 2), (k.Stand, 6), (hcHic, 2), (hcAfter, 2), (k.Stand, 6), (hcHic, 2), (hcAfter, 3), (k.Blink, 2), (k.Happy, 2), (k.Stand, 1));
		p.A(272, "hiccup sparkle").Move("1", "-2", "-1", "-2").Speed("90").Loop("8", 0).Fade(1, 0).Seq(hcSp1, hcSp2);
		Delay(273, "hiccup delay 1", 120, 10, 272);
		Delay(274, "hiccup delay 2", 120, 20, 272);
		k.Child(135, 272); k.Child(135, 273); k.Child(135, 274);

		// ================================================================ 136 portal
		// He opens a swirling portal on the floor and drops into it; it closes behind him (a child), he travels
		// unseen, and pops out of a new portal somewhere else.
		void Portal(Frame f, int size, int ph, bool front)
		{
			int hw = size == 1 ? 2 : size == 2 ? 4 : 6;
			for (int y = 13; y <= 15; y++)
			{
				int w = y == 14 ? hw : hw - 2;
				if (w <= 0) continue;
				for (int x = 8 - w; x < 8 + w; x++)
				{
					bool edge = x == 8 - w || x == 8 + w - 1 || y != 14;
					char c = edge ? (y == 14 ? Violet : ((x + ph) % 3 == 0 ? Magenta : Violet)) : Night;
					if (!edge && (x + ph * 2) % 4 == 0) c = Magenta;
					if (!edge && (x + ph * 2) % 4 == 2) c = Cyan;
					if (front) f[x, y] = c; else Behind(f, x, y, c);
				}
			}
		}
		Frame Sink(int n, int ph)
		{
			var f = Base(); Eyes(f, 4); Mouth(f); Arm(f, -1, 1); Arm(f, 1, 1);
			f.Shift(0, n); Clear(f, 13, 15);
			Portal(f, 3, ph, true);
			return f;
		}
		Frame OnPortal(int size, int ph, int eyes, bool armsUp)
		{
			var f = Base(); Eyes(f, eyes);
			if (armsUp) { Arm(f, -1, 1); Arm(f, 1, 1); if (eyes == 4) Mouth(f); }
			else { Arm(f, -1, 0); f.Rect(14, 8, 1, 3, Robe).Dot(14, 11, Skin).Dot(15, 12, Wood).Dot(15, 13, Star); }
			Portal(f, size, ph, true);
			return f;
		}
		Frame PortalAlone(int size, int ph) { var f = new Frame(); Portal(f, size, ph, true); return f; }
		int ptOpen1 = F(OnPortal(1, 0, 3, false)), ptOpen2 = F(OnPortal(2, 1, 3, false)), ptOpen3 = F(OnPortal(3, 2, 4, false));
		int ptSink1 = F(Sink(3, 0)), ptSink2 = F(Sink(6, 1)), ptSink3 = F(Sink(10, 2)), ptSink4 = F(Sink(14, 3));
		int ptAlone0 = F(PortalAlone(3, 0)), ptAlone1 = F(PortalAlone(3, 1)), ptAlone2 = F(PortalAlone(3, 2)), ptAlone3 = F(PortalAlone(3, 3));
		int ptMid = F(PortalAlone(2, 0)), ptSmall = F(PortalAlone(1, 1));
		var ptBye = new Frame(); ptBye.Dot(8, 14, HotWhite).Dot(7, 14, Magenta).Dot(9, 14, Cyan).Dot(8, 13, Violet);
		int ptByeF = F(ptBye);
		int ptUp1 = F(OnPortal(3, 1, 4, true)), ptUp2 = F(OnPortal(2, 2, 1, true)), ptUp3 = F(OnPortal(1, 3, 1, true));
		k.Lead(136, "portal", 110, 170, (k.Cast1, 2), (ptOpen1, 2), (ptOpen2, 2), (ptOpen3, 3), (ptSink1, 1), (ptSink2, 1), (ptSink3, 1), (ptSink4, 1), (ptAlone0, 1), (ptAlone1, 1));
		p.A(170, "portal travel").Move("-24", "0").Speed("40").Loop("random/4+3", 0)
			.Seq(k.Empty, k.Empty)
			.Then(100, 171)
			.OnBorder(100, 171).OnGravity(100, 171);
		k.Show(171, "portal exit", 110, (ptSmall, 1), (ptMid, 1), (ptAlone2, 1), (ptAlone3, 1), (ptSink4, 1), (ptSink3, 1), (ptSink2, 1), (ptSink1, 1),
			(ptUp1, 2), (ptUp2, 2), (ptUp3, 1), (k.Happy, 2), (k.Stand, 2));
		p.A(275, "portal closing").Move("0", "0").Speed("110").Seq(ptAlone2, ptAlone3, ptAlone0, ptAlone1, ptMid, ptSmall, ptByeF);
		k.Child(170, 275);

		// ================================================================ 137 spirit friend
		// A soft light glows between his cupped hands and a little friendly ghost (a child) rises out of it, does
		// a playful loop around him and floats away; he follows it with his eyes and waves.
		Frame Cup(int glow)
		{
			var f = Base(); EyesDown(f);
			f.Dot(1, 8, Robe).Dot(2, 9, RobeDark).Dot(3, 10, RobeDark).Dot(4, 10, RobeDark).Dot(5, 10, Skin);
			f.Dot(14, 8, Robe).Dot(13, 9, RobeDark).Dot(12, 10, RobeDark).Dot(11, 10, RobeDark).Dot(10, 10, Skin);
			f.Rect(6, 10, 4, 1, PaleYellow).Dot(7, 10, HotWhite).Dot(8, 10, HotWhite);
			if (glow >= 1) f.Rect(7, 9, 2, 1, PaleYellow);
			if (glow >= 2) { f.Dot(6, 8, PaleYellow).Dot(9, 8, PaleYellow).Dot(8, 7, HotWhite); Sparkle(f, glow == 2 ? 3 : 12, 6, PaleYellow); }
			return f;
		}
		int sfCup0 = F(Cup(0)), sfCup1 = F(Cup(1)), sfCup2 = F(Cup(2)), sfCup3 = F(Cup(3));
		Frame Ghost(int k2)
		{
			var f = new Frame();
			f.Stamp(6, 6,
				".WWW.",
				"WWWWh",
				"WKWKh",
				"WWWWh",
				"WWWWh");
			f.Stamp(6, 11, k2 == 0 ? "W.W.h" : ".W.h.");
			if (k2 == 1) f.Dot(8, 9, Pink);
			return f;
		}
		int ghost1 = F(Ghost(0)), ghost2 = F(Ghost(1));
		var sfLook = Stand(4); int sfLookF = F(sfLook);
		k.Lead(137, "spirit friend", 160, 172, (k.Stand, 1), (sfCup0, 3), (sfCup1, 2), (sfCup2, 2), (sfCup3, 2));
		k.Show(172, "spirit watch", 160, (sfLookF, 3), (k.Think1, 3), (k.Stand, 1), (sfLookF, 2), (k.Happy, 3), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Wave2, 2), (k.Stand, 2));
		p.A(276, "ghost rise").Move("0", "-3").Speed("110").Loop("3", 0).Seq(ghost1, ghost2).Then(100, 277);
		p.A(277, "ghost circle").Move("4", "-2", "-4", "2").Speed("110").Loop("6", 0).Seq(ghost1, ghost2).Then(100, 278);
		p.A(278, "ghost away").Move("-2", "-3").Speed("110").Loop("12", 0).Fade(1, 0).Seq(ghost1, ghost2);
		k.Child(172, 276);

		// ================================================================ 138 exercise
		// Morning exercises: arms up, squats, side stretches... he gets tired, pants and wipes the sweat off.
		var exUp = Base(); Eyes(exUp, 0); Arm(exUp, -1, 1); Arm(exUp, 1, 1);
		int exUpF = F(exUp);
		var exSq = Base(); Eyes(exSq, 1); Arm(exSq, -1, 2); Arm(exSq, 1, 2); Squash(exSq, 3); Shoes(exSq, ".UUU........UUU.");
		int exSqF = F(exSq);
		Frame Side(int dir)
		{
			var src = Base(); Eyes(src, dir < 0 ? 2 : 3); Arm(src, -1, 1); Arm(src, 1, 1);
			var f = new Frame();
			for (int y = src.MinY; y <= 15; y++)
				for (int x = 0; x < 16; x++)
					if (src[x, y] != '.') f[x + dir * (y < 0 ? 2 : y < 5 ? 1 : 0), y] = src[x, y];
			return f;
		}
		int exLeft = F(Side(-1)), exRight = F(Side(1));
		var exT = Base(); Eyes(exT, 1); Arm(exT, -1, 0); WandDown(exT); exT.Rect(7, 7, 2, 2, Ink); Squash(exT, 1); exT.Dot(3, 3, Cyan).Dot(12, 4, Cyan);
		int exTired = F(exT);
		var exT2 = Base(); Eyes(exT2, 1); Arm(exT2, -1, 0); WandDown(exT2); exT2.Rect(7, 7, 2, 1, Ink); exT2.Dot(3, 4, Cyan).Dot(12, 5, Cyan).Dot(13, 2, Cyan);
		int exTired2 = F(exT2);
		var exW = Base(); Eyes(exW, 1); WandDown(exW); exW.Rect(1, 6, 1, 3, Robe).Dot(2, 5, Robe).Dot(3, 4, Skin).Dot(4, 4, Skin).Dot(1, 3, Cyan).Dot(0, 2, Cyan);
		int exWipe = F(exW);
		var exW2 = Base(); Eyes(exW2, 1); WandDown(exW2); exW2.Rect(1, 6, 1, 3, Robe).Dot(2, 5, Robe).Dot(3, 4, Robe).Dot(5, 4, Skin).Dot(6, 4, Skin).Dot(0, 1, Cyan);
		int exWipe2 = F(exW2);
		var exerciseSteps = new List<(int, int)> { (k.Stand, 1) };
		for (int r = 0; r < 3; r++) { exerciseSteps.Add((exUpF, 2)); exerciseSteps.Add((exSqF, 2)); }
		for (int r = 0; r < 2; r++) { exerciseSteps.Add((exLeft, 2)); exerciseSteps.Add((exUpF, 1)); exerciseSteps.Add((exRight, 2)); exerciseSteps.Add((exUpF, 1)); }
		exerciseSteps.AddRange(new[] { (exSqF, 2), (exTired, 3), (exTired2, 2), (exTired, 2), (exWipe, 2), (exWipe2, 2), (exWipe, 1), (exTired2, 2), (k.Blink, 1), (k.Stand, 2) });
		k.Show(138, "exercise", 170, exerciseSteps.ToArray());

		// ================================================================ 139 message bottle
		// A glass bottle (a child) rolls in along the floor and stops at his feet; he picks it up, pops the cork,
		// reads the little letter and smiles, with a heart.
		void BottleLying(Frame f, int roll)
		{
			if (roll == 0) f.Stamp(0, 13, "..LGd.", "wGpppd", "..ddd.");
			else f.Stamp(0, 13, "..dGd.", "wGppGd", "..dLd.");
			 
		}
		var bt1 = new Frame(); BottleLying(bt1, 0); int btRoll1 = F(bt1);
		var bt2 = new Frame(); BottleLying(bt2, 1); int btRoll2 = F(bt2);
		var btSee = Stand(2); int btSeeF = F(btSee);
		var btSee2 = Stand(2); Eyes(btSee2, 4); btSee2.Dot(5, 5, Skin).Dot(4, 5, Ink).Dot(10, 5, Skin).Dot(9, 5, Ink); int btSee2F = F(btSee2);
		var btOn = Stand(2); BottleLying(btOn, 0); int btOnF = F(btOn);
		var btPick = Base(); Eyes(btPick, 2); WandDown(btPick); Squash(btPick, 2); btPick.Dot(1, 11, Robe).Dot(1, 12, Robe).Dot(2, 13, Skin); BottleLying(btPick, 0);
		int btPickF = F(btPick);
		Frame Holding(int stage)
		{
			var f = Base(); Eyes(f, stage == 2 ? 4 : 0);
			if (stage < 3)
			{
				EyesDown(f); if (stage == 2) { f.Dot(5, 6, Beard).Dot(10, 6, Beard); Eyes(f, 4); }
				f.Dot(1, 8, Robe).Dot(2, 9, RobeDark).Dot(3, 10, RobeDark).Dot(4, 10, Skin);
				f.Dot(14, 8, Robe).Dot(13, 9, RobeDark).Dot(12, 10, RobeDark).Dot(11, 10, Skin);
				f.Rect(6, 9, 4, 3, Green).Rect(9, 9, 1, 3, DarkGreen).Rect(6, 11, 4, 1, DarkGreen).Dot(6, 9, Leaf).Dot(6, 10, Leaf).Rect(7, 10, 2, 1, Paper).Dot(7, 8, Green).Dot(8, 8, DarkGreen);
				f.Rect(5, 10, 1, 1, Skin).Rect(10, 10, 1, 1, Skin);
				if (stage == 0) f.Dot(7, 7, Wood).Dot(8, 7, Wood);
				if (stage >= 1) { f.Dot(12, 2, Wood).Dot(13, 1, Wood); Sparkle(f, 10, 3, Star); }
				if (stage == 2) { f.Rect(7, 6, 2, 3, Paper); f.Dot(12, 2, '.').Dot(13, 1, '.'); }
			}
			return f;
		}
		int btHold = F(Holding(0)), btPop = F(Holding(1)), btPull = F(Holding(2));
		Frame Letter(int line, bool smile)
		{
			var f = Base(); if (smile) Eyes(f, 1); else EyesDown(f);
			f.Dot(1, 8, Robe).Dot(2, 9, RobeDark).Dot(3, 10, Skin);
			f.Dot(14, 8, Robe).Dot(13, 9, RobeDark).Dot(12, 10, Skin);
			f.Rect(4, 8, 8, 5, Paper).Dot(4, 8, PaleYellow).Dot(11, 12, PaleYellow);
			for (int x = 5; x <= 10; x++) if (x != 7) f[x, 9] = line >= 0 ? Ash : LightGray;
			for (int x = 5; x <= 7; x++) if (x != 6 || line < 2) f[x, 11] = line >= 1 ? Ash : LightGray;
			f.Dot(5, 10, Paper);
			f.Dot(9, 11, Red).Dot(10, 11, Red);                                                // a little red heart drawn on it
			if (smile) { f.Stamp(11, -2, "T.T", "TTT", ".T."); f.Dot(4, 5, Pink).Dot(11, 5, Pink); }
			return f;
		}
		int btRead1 = F(Letter(0, false)), btRead2 = F(Letter(1, false)), btRead3 = F(Letter(2, false)), btSmile = F(Letter(2, true));
		k.Lead(139, "message bottle", 80, 173, (btSeeF, 8), (btSee2F, 8));
		k.Show(173, "read message", 180, (btOnF, 2), (btPickF, 3), (btHold, 3), (btPop, 3), (btPull, 3), (btRead1, 3), (btRead2, 3), (btRead3, 4), (btSmile, 6), (k.Happy, 2), (k.Stand, 2));
		p.A(279, "bottle roll").Move("6", "0").Speed("80").Loop("7", 0).Seq(btRoll1, btRoll2);
		k.Child(139, 279, "imageX-imageW*2", "imageY");

		// ================================================================ sounds (seeds 400-449)
		k.Sounds.Add(new PetSound(127, 70, new Sfx(2.6, 400).Tone(0, 2.4, 220, 220, 0.18, Sfx.Wave.Sine, 0.4, 0.8, 5, 0.01)
			.Bell(0.5, 880, 0.25, 0.5).Bell(1.0, 1175, 0.2, 0.5).Bell(1.5, 1320, 0.2, 0.6)));
		var crunch = new Sfx(2.6, 401);
		foreach (double t in new[] { 0.8, 0.95, 1.45, 1.6, 2.0 }) crunch.Noise(t, 0.08, 0.35, 3500, 1200, 0.002, 0.05, 300);
		k.Sounds.Add(new PetSound(128, 80, crunch));
		k.Sounds.Add(new PetSound(129, 60, new Sfx(1.8, 402).Tone(0, 0.9, 85, 70, 0.35, Sfx.Wave.Saw, 0.2, 0.3, 0, 0, 28, 0.6).Soften(900)
			.Noise(0.95, 0.6, 0.15, 1200, 500, 0.2, 0.3)));
		k.Sounds.Add(new PetSound(177, 50, new Sfx(0.5, 403).Tone(0, 0.4, 160, 520, 0.4, Sfx.Wave.Sine, 0.005, 0.2, 22, 0.15)));
		k.Sounds.Add(new PetSound(130, 80, new Sfx(0.9, 404).Bell(0, 1320, 0.25, 0.3).Tone(0.4, 0.4, 140, 600, 0.4, Sfx.Wave.Sine, 0.005, 0.2, 20, 0.2)));
		k.Sounds.Add(new PetSound(131, 40, new Sfx(0.8, 405).Tone(0, 0.6, 140, 110, 0.25, Sfx.Wave.Saw, 0.1, 0.3).Soften(700)));
		k.Sounds.Add(new PetSound(179, 70, new Sfx(1.2, 406).Tone(0, 0.25, 320, 260, 0.2, Sfx.Wave.Saw, 0.01, 0.1, 0, 0, 40, 0.8).Soften(1500)
			.Tone(0.4, 0.7, 260, 180, 0.25, Sfx.Wave.Sine, 0.05, 0.4)));
		var clicks = new Sfx(4.6, 407);
		for (int i = 0; i < 14; i++) clicks.Click(0.25 + i * 0.3, 0.3).Click(0.32 + i * 0.3, 0.2);
		k.Sounds.Add(new PetSound(132, 80, clicks));
		k.Sounds.Add(new PetSound(133, 80, new Sfx(4.8, 408).Noise(0.3, 0.35, 0.3, 2500, 6000, 0.05, 0.2, 400)
			.Click(2.6, 0.3).Click(2.7, 0.3).Click(2.8, 0.3).Click(2.9, 0.3).Click(3.0, 0.3)
			.Bell(3.0, 1568, 0.25, 0.4).Bell(4.6, 2093, 0.2, 0.4)));
		k.Sounds.Add(new PetSound(134, 70, new Sfx(1.4, 409).Notes(0.3, 0.12, 0.2, Sfx.Wave.Triangle, 76, 79, 84, 88)));
		k.Sounds.Add(new PetSound(174, 60, new Sfx(0.2, 410).Pop(0, 0.3, 500)));
		var hics = new Sfx(2.8, 411);
		foreach (double t in new[] { 0.0, 1.2, 2.4 }) hics.Tone(t, 0.09, 500, 900, 0.4, Sfx.Wave.Sine, 0.002, 0.05).Bell(t + 0.05, 1760, 0.15, 0.2);
		k.Sounds.Add(new PetSound(135, 90, hics));
		k.Sounds.Add(new PetSound(136, 80, new Sfx(2.0, 412).Tone(0.3, 0.8, 300, 600, 0.2, Sfx.Wave.Sine, 0.1, 0.3, 6, 0.1)
			.Noise(0.9, 0.6, 0.35, 3000, 400, 0.05, 0.3, 200).Tone(1.0, 0.5, 700, 150, 0.3, Sfx.Wave.Sine, 0.01, 0.3)));
		k.Sounds.Add(new PetSound(171, 80, new Sfx(1.2, 413).Noise(0, 0.5, 0.3, 400, 3000, 0.05, 0.2, 200).Tone(0.3, 0.4, 150, 700, 0.3, Sfx.Wave.Sine, 0.01, 0.2)
			.Pop(0.75, 0.3, 800)));
		k.Sounds.Add(new PetSound(172, 80, new Sfx(2.4, 414).Tone(0, 1.2, 520, 660, 0.18, Sfx.Wave.Triangle, 0.3, 0.6, 5, 0.03)
			.Tone(1.1, 1.0, 660, 520, 0.15, Sfx.Wave.Triangle, 0.2, 0.6, 5, 0.03)));
		k.Sounds.Add(new PetSound(138, 50, new Sfx(3.5, 415).Noise(2.9, 0.3, 0.2, 1500, 600, 0.05, 0.2).Noise(3.2, 0.3, 0.18, 1500, 600, 0.05, 0.2)));
		var rolling = new Sfx(1.4, 416);
		for (int i = 0; i < 8; i++) rolling.Click(i * 0.16, 0.25).Bell(i * 0.16, 2400 + i * 40, 0.04, 0.05);
		k.Sounds.Add(new PetSound(139, 80, rolling));
		k.Sounds.Add(new PetSound(173, 80, new Sfx(6.0, 417).Pop(1.6, 0.4, 900).Notes(5.0, 0.15, 0.18, Sfx.Wave.Triangle, 72, 76, 79)));
	}
}
