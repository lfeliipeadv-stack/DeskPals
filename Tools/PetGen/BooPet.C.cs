namespace PetGen;

static partial class BooPet
{
	// 150-174: everyday life, food, weather and play — a melting ice cream, hot soup, a rain cloud, a snow cap, a windy day,
	// a kite, an umbrella turned inside out, painting, a toy piano, jump rope, a bubble bath, the laundry line, sunbathing,
	// a paper boat, cocoa, popcorn, a spinning top, a rainbow, a puddle, a hula hoop, a watering can, a drum, a toaster,
	// a hand fan and a sled. Second parts and child windows 400-499, sound seeds 2200-2299.
	static void ScenesC(Kit k)
	{
		var p = k.P;
		int blank = k.Empty;
		int nid = 400;
		int Id() => nid++;

		// ------------------------------------------------------------ small local helpers
		int F(Frame f) => k.F(f);
		// a sprite given as rows ('.' keeps what is there)
		void Put(Frame f, int x, int y, params string[] rows) { for (int j = 0; j < rows.Length; j++) f.Stamp(x, y + j, rows[j].Replace('.', ' ')); }
		void Ln(Frame f, int x0, int y0, int x1, int y1, char c)
		{
			int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
			while (true)
			{
				f.Dot(x0, y0, c);
				if (x0 == x1 && y0 == y1) break;
				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; x0 += sx; }
				if (e2 <= dx) { err += dx; y0 += sy; }
			}
		}
		// wind streaks: only where nothing is drawn yet
		void Streak(Frame f, int x, int y, int len) { for (int i = 0; i < len; i++) if (f[x + i, y] == '.') f[x + i, y] = Zz; }
		void Ring(Frame f, float cx, float cy, float r)       // a soap bubble
		{
			for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
				for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
				{
					float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = MathF.Sqrt(dx * dx + dy * dy);
					if (d <= r && d > r - 1.1f) f[x, y] = Tear;
				}
			f.Dot((int)(cx - r * 0.4f), (int)(cy - r * 0.4f), GlowLight);
		}
		void Recolor(Frame f, char from, char to) { for (int y = f.MinY; y < 16; y++) for (int x = 0; x < 16; x++) if (f[x, y] == from) f[x, y] = to; }
		// looking up (dx -1: up and to the front)
		Frame Up(int lift, int hem, int mouth = 0, int la = 0, int ra = 0, int dx = 0)
		{
			var f = G(lift, hem, 0, mouth, la, ra);
			int y = 5 - lift;
			f.Rect(4, y - 1, 2, 3, Sheet).Rect(10, y - 1, 2, 3, Sheet).Rect(4 + dx, y - 2, 2, 2, Ink).Rect(10 + dx, y - 2, 2, 2, Ink);
			return f;
		}
		// sitting low, like the base "sit" pose (eyes on rows 5-7, mouth on row 8, arms on row 10)
		Frame Sit(int eyes, int mouth = 0, int hem = 0)
		{
			var f = G(0, hem, eyes, mouth); f.Shift(0, 1); f.Rect(3, 15, 10, 1, Shadow);
			return f;
		}
		(int f, int n)[] Cy(int count, params int[] fs) => Enumerable.Range(0, count).Select(i => (fs[i % fs.Length], 1)).ToArray();
		(int f, int n)[] Steps(params (int f, int n)[][] parts) => parts.SelectMany(x => x).ToArray();
		(int f, int n)[] One(int f, int n) => new[] { (f, n) };
		int[] CyF(int count, params int[] fs) => Enumerable.Range(0, count).Select(i => fs[i % fs.Length]).ToArray();
		int[] Rep(int f, int n) => Enumerable.Repeat(f, n).ToArray();
		int[] Cat(params int[][] parts) => parts.SelectMany(x => x).ToArray();
		Anim Kid(int id, string name, int ms, params int[] frames) => p.A(id, name).Move("0", "0").Speed(ms.ToString()).Seq(frames);
		void Snd(int anim, Sfx s) => k.Sound(anim, s);

		// ================================================================ 150 melting ice cream
		// A two-scoop cone melts in his hand; the top scoop slides off. The scoop is a child window that drops
		// to the ground and spreads into a little pink puddle, which stays a moment after Boo moves on.
		void IceCone(Frame f, int x, int y, int melt)        // (x, y) = top-left of the top scoop; melt 3 = the top scoop is gone
		{
			Put(f, x, y + 4, "nnnn", ".nn.", ".nn.", "..n.");
			Put(f, x, y + 2, "iiii", "iiii");
			if (melt <= 1) Put(f, x, y, ".vu.", "uuuu");
			if (melt == 2) Put(f, x, y + 1, "vuu.", "uuuu");
			if (melt >= 1) f.Dot(x, y + 4, Mint);
			if (melt >= 2) f.Dot(x, y + 5, Mint).Dot(x + 3, y + 3, Gum2);
			if (melt == 3) f.Dot(x + 1, y + 2, Gum2);
		}
		Frame Ice(int hem, int eyes, int mouth, int melt, int dx = 0, int dy = 0)
		{
			var f = G(1, hem, eyes, mouth, 1, 0); IceCone(f, dx, -2 + dy, melt); return f;
		}
		int icHold = F(Ice(0, 2, 0, 0)), icHold2 = F(Ice(1, 2, 0, 0));
		var lk = Ice(1, 1, 4, 0, 1, 1); lk.Dot(6, 6, Mouth).Dot(5, 6, Mouth); int icLick = F(lk);
		int icDrip = F(Ice(0, 2, 1, 1)), icDrip2 = F(Ice(1, 2, 0, 1));
		int icWorry = F(Ice(1, 4, 1, 2));
		int icGone = F(Ice(0, 4, 1, 3));
		var icd = Ice(0, 2, 4, 3); icd.Rect(3, 3, 2, 1, Sheet).Rect(9, 3, 2, 1, Sheet); int icDown = F(icd);   // looks down at the floor
		var ics = Ice(1, 6, 4, 3); ics.Dot(5, 6, Tear).Dot(10, 6, Tear); int icSad = F(ics);
		var icm = Ice(0, 1, 4, 3, 1, 1); icm.Dot(6, 6, Mouth).Dot(5, 6, Mouth); int icMint = F(icm);
		int icGiggle = F(Ice(1, 1, 3, 3));
		Frame Scoop(int y) { var f = new Frame(); Put(f, 0, y, ".vu.", "uuuu"); return f; }
		var spl1 = new Frame(); Put(spl1, 0, 13, "u....u", ".vuuu.", "uuuuuu");
		var spl2 = new Frame(); Put(spl2, 0, 14, ".uuu..", "uuvuuu");
		var pud = new Frame(); Put(pud, 0, 15, "uuvuuu");
		int scTop = F(Scoop(-2)), sc1 = F(Scoop(1)), sc2 = F(Scoop(5)), sc3 = F(Scoop(9)), sc4 = F(Scoop(12)), scSplat1 = F(spl1), scSplat2 = F(spl2), scPuddle = F(pud);

		int icPlop = Id(), icFall = Id(), icPuddle = Id();
		k.Lead(150, "melting ice cream", 150, icPlop, (k.Stand, 1), (icHold, 2), (icLick, 2), (icHold2, 1), (icLick, 2), (icDrip, 2), (icDrip2, 2), (icWorry, 3));
		k.Show(icPlop, "ice cream plop", 150, (icGone, 2), (icDown, 5), (icSad, 5), (icMint, 2), (icGiggle, 2), (icMint, 2), (k.Happy, 2));
		k.Child(icPlop, icFall);
		Kid(icFall, "ice cream scoop falls", 80, scTop, sc1, sc2, sc3, sc4, scSplat1, scSplat2).Then(100, icPuddle);
		Kid(icPuddle, "ice cream puddle", 220, Rep(scPuddle, 6)).Fade(1, 0);
		Snd(icPlop, new Sfx(1.0, 2200).Tone(0.0, 0.32, 700, 260, 0.2, Sfx.Wave.Sine, 0.01, 0.1).Thud(0.4, 0.5, 180, 60, 0.15)
			.Noise(0.4, 0.2, 0.35, 2500, 700, 0.002, 0.15, 300));

		// ================================================================ 151 hot soup
		// Sits down with a bowl of soup, blows on it, sips — too hot! — then it is yummy. Steam rises in a child window.
		void Bowl(Frame f, int y) => Put(f, 0, y, "oOfOOfOo", "BBBBBBBB", ".BBBBBB.", "..BBBB..");
		void Wisp(Frame f, int w)
		{
			var pts = w switch
			{
				0 => new[] { (1, 9), (2, 8), (1, 7), (2, 6) },
				1 => new[] { (2, 9), (1, 8), (2, 7), (1, 6) },
				_ => new[] { (1, 9), (0, 8), (0, 7), (0, 6) },
			};
			foreach (var (x, y) in pts) f.Dot(x, y, Zz);
		}
		Frame Soup(int eyes, int mouth, int wisp, int hem = 0, int bowlY = 10)
		{
			var f = Sit(eyes, mouth, hem); Bowl(f, bowlY); if (wisp >= 0) Wisp(f, wisp); return f;
		}
		int soLookA = F(Soup(2, 0, 0)), soLookB = F(Soup(2, 0, 1, 1));
		var sob = Soup(1, 1, 0); sob.Dot(5, 9, Zz).Dot(4, 9, Zz); int soBlow = F(sob);
		var sobn = Soup(1, 1, 2, 1); sobn.Dot(4, 9, Zz).Dot(3, 9, Zz); int soBlown = F(sobn);
		int soSip = F(Soup(1, 0, -1, 0, 8));
		var soh = Soup(5, 1, 1, 1); soh.Dot(8, 10, Mouth).Dot(13, 3, Tear).Dot(13, 4, Tear); int soHot = F(soh);
		var soy = Soup(1, 3, 0); HeartAt(soy, 12, -2); int soYum = F(soy);
		Frame Steam(int w)
		{
			var f = new Frame();
			var xs = w == 0 ? new[] { 0, 1, 0, 1, 0 } : new[] { 1, 0, 1, 0, 1 };
			for (int i = 0; i < 5; i++) f.Dot(xs[i], 13 - i, Zz);
			for (int i = 0; i < 4; i++) f.Dot(xs[(i + 1) % 5] + 2, 12 - i, Zz);
			return f;
		}
		int stm0 = F(Steam(0)), stm1 = F(Steam(1));
		int soSteam = Id();
		k.Show(151, "hot soup", 150, (k.Stand, 1), (k.Sit, 1), (soLookA, 2), (soLookB, 2), (soBlow, 2), (soBlown, 2), (soBlow, 1), (soBlown, 1),
			(soSip, 3), (soHot, 3), (soYum, 4), (k.Sit, 1), (k.Stand, 1));
		k.Child(151, soSteam, "imageX", "imageY-imageH*0.2");
		Kid(soSteam, "soup steam", 150, Cat(Rep(blank, 4), CyF(16, stm0, stm1))).Move("0", "-2").Fade(1, 0.1);
		Snd(151, new Sfx(3.0, 2201).Noise(0.9, 0.45, 0.35, 1200, 700, 0.05, 0.25, 250).Noise(1.5, 0.3, 0.3, 1200, 700, 0.05, 0.2, 250)
			.Noise(1.8, 0.4, 0.45, 2200, 900, 0.02, 0.1, 400, 28, 0.7).Tone(2.25, 0.3, 500, 900, 0.16, Sfx.Wave.Triangle, 0.01, 0.1, 8, 0.03));

		// ================================================================ 152 rain cloud
		// A little grey cloud (a child window) comes over his head and rains only on him. He gets soaked, sad,
		// shakes the water off when it leaves and cheers up.
		Frame RainCloud(int rain)
		{
			var f = new Frame();
			FillCircle(f, 5.5f, 10.5f, 2.6f, Shade, Sheet); FillCircle(f, 9.5f, 9.5f, 3.2f, Shade, Sheet); FillCircle(f, 12f, 11f, 2.2f, Shade, Shade);
			f.Rect(4, 11, 10, 2, Shade);
			var src = f.Clone();
			for (int y = f.MinY; y < 16; y++) for (int x = 0; x < 16; x++)
				if (src[x, y] != '.' && (src[x - 1, y] == '.' || src[x + 1, y] == '.' || src[x, y - 1] == '.' || src[x, y + 1] == '.')) f[x, y] = Line;
			if (rain >= 0)
				foreach (var (x, y) in rain == 0 ? new[] { (4, 14), (7, 15), (10, 14), (13, 15) } : new[] { (5, 15), (8, 14), (11, 15), (3, 15), (12, 14) })
					f.Dot(x, y, Tear);
			return f;
		}
		Frame Wet(int w)
		{
			var f = G(1, w, 6, 4); f.Dot(5, 6, Tear).Dot(10, 6, Tear);
			foreach (var (x, y) in w == 0 ? new[] { (6, 0), (11, 2), (2, 5), (13, 8), (1, 10) } : new[] { (9, 0), (4, 2), (13, 4), (2, 8), (14, 11) }) f.Dot(x, y, Tear);
			return f;
		}
		Frame WetShake(int dir)
		{
			var f = G(1, dir > 0 ? 1 : 0, 5, 1); f.Shift(dir, 0);
			foreach (var (x, y) in dir < 0 ? new[] { (15, 1), (15, 8), (14, -1) } : new[] { (0, 1), (0, 9), (1, -1) }) f.Dot(x, y, Tear);
			return f;
		}
		int rcCloud = F(RainCloud(-1)), rcRain0 = F(RainCloud(0)), rcRain1 = F(RainCloud(1));
		int rcUp = F(Up(1, 0, 1)), rcUp2 = F(Up(1, 1, 0)), rcWet0 = F(Wet(0)), rcWet1 = F(Wet(1)), rcShakeL = F(WetShake(-1)), rcShakeR = F(WetShake(1));
		int rcIn = Id(), rcRaining = Id(), rcOut = Id();
		k.Show(152, "rain cloud", 150, Steps(One(k.Stand, 1), One(rcUp, 2), Cy(16, rcWet0, rcWet1), Cy(4, rcShakeL, rcShakeR), One(rcUp2, 1), One(k.Happy, 3)));
		k.Child(152, rcIn, "imageX", "imageY-imageH*0.8");
		Kid(rcIn, "rain cloud comes", 150, rcCloud, rcCloud, rcCloud).Fade(0, 1).Then(100, rcRaining);
		Kid(rcRaining, "rain cloud rains", 150, CyF(16, rcRain0, rcRain1)).Then(100, rcOut);
		Kid(rcOut, "rain cloud goes", 150, Rep(rcCloud, 5)).Fade(1, 0);
		Snd(152, new Sfx(3.2, 2202).Noise(0.4, 2.6, 0.3, 6000, 4500, 0.4, 0.6, 1500)
			.Pop(0.8, 0.15, 1400).Pop(1.3, 0.12, 1700).Pop(1.9, 0.15, 1500).Pop(2.4, 0.12, 1800));

		// ================================================================ 153 snow cap
		// Snow keeps falling on his head until he wears a tall white cap, then he shakes it off — brrr — and giggles.
		void Flakes(Frame f, int t)
		{
			foreach (var (x, y) in new[] { (1, -4), (13, -3), (3, 0), (14, 4), (0, 6), (15, -1) })
				if (f[x, y + t] == '.') f[x, y + t] = Zz;
		}
		void Cap(Frame f, int s)
		{
			if (s == 1) Put(f, 4, -2, "..zWWz..");
			if (s == 2) Put(f, 4, -3, "...zz...", ".zWWWWz.");
			if (s >= 3) Put(f, 4, -4, "...zz...", "..zWWz..", ".zWWWWz.");
		}
		Frame Snowy(int s, int t) { var f = Up(1, t, s >= 3 ? 1 : 0); Cap(f, s); Flakes(f, t); return f; }
		Frame SnowShake(int dir)
		{
			var f = G(1, dir > 0 ? 1 : 0, 5, 1); f.Shift(dir, 0);
			foreach (var (x, y) in dir < 0 ? new[] { (2, -3), (1, -2), (12, -4), (14, -2) } : new[] { (0, -1), (3, -4), (15, -3), (13, -4) })
				f.Dot(x, y, (x + y) % 2 == 0 ? Sheet : Zz);
			return f;
		}
		var snw = new int[4, 2];
		for (int s = 0; s < 4; s++) for (int t = 0; t < 2; t++) snw[s, t] = F(Snowy(s, t));
		int snShL = F(SnowShake(-1)), snShR = F(SnowShake(1));
		k.Show(153, "snow cap", 170, Steps(One(k.Stand, 1), One(snw[0, 0], 1), One(snw[0, 1], 1),
			Cy(3, snw[1, 0], snw[1, 1]), Cy(3, snw[2, 0], snw[2, 1]), Cy(3, snw[3, 0], snw[3, 1]),
			Cy(6, snShL, snShR), One(k.Shiver1, 1), One(k.Shiver2, 1), One(k.Giggle1, 2), One(k.Giggle2, 2), One(k.Stand, 1)));
		Snd(153, new Sfx(3.0, 2203).Noise(2.04, 0.7, 0.4, 2500, 1200, 0.02, 0.3, 300, 10, 0.6)
			.Tone(2.04, 0.6, 180, 160, 0.15, Sfx.Wave.Square, 0.02, 0.1, 0, 0, 30, 0.8).Soften(3000));

		// ================================================================ 154 windy day
		// A gust bends his sheet backwards; he squeezes his eyes and holds on while a leaf (child) blows past him.
		Frame Windy(int w)
		{
			var f = G(1, w, 5, 1); Lean(f, 1, 12); if (w == 1) Lean(f, 1, 6);
			f.Dot(15, 12, Line).Dot(15, 13 - w, Line);
			if (w == 0) { Streak(f, 0, -3, 4); Streak(f, 0, 2, 3); Streak(f, 0, 7, 2); Streak(f, 0, 11, 2); }
			else { Streak(f, 1, -2, 4); Streak(f, 0, 4, 3); Streak(f, 0, 9, 2); Streak(f, 0, 13, 1); }
			return f;
		}
		string[][] leafRows =
		{
			new[] { ".Of", "Of.", "n.." },
			new[] { ".n.", "fOf", ".O." },
			new[] { "n..", ".Of", ".fO" },
			new[] { ".O.", "OfO", ".n." },
		};
		var leaf = new int[4];
		for (int i = 0; i < 4; i++) { var f = new Frame(); Put(f, 6, 4 + i % 2, leafRows[i]); leaf[i] = F(f); }
		int wdA = F(Windy(0)), wdB = F(Windy(1));
		int lfBy = Id(), lfAway = Id();
		k.Show(154, "windy day", 120, Steps(One(k.Stand, 1), Cy(20, wdA, wdB), One(k.Shiver1, 1), One(k.Shiver2, 1), One(k.Blink, 1), One(k.Happy, 2)));
		k.Child(154, lfBy, "imageX-imageW*1.2", "imageY");
		Kid(lfBy, "leaf blows by", 70, CyF(16, leaf)).Move("6", "-1", "6", "1").Then(100, lfAway);
		Kid(lfAway, "leaf blows away", 70, CyF(8, leaf)).Move("6", "1").Fade(1, 0);
		Snd(154, new Sfx(3.0, 2204).Noise(0.0, 2.8, 0.55, 400, 1400, 0.5, 0.8, 120, 1.2, 0.5).Noise(0.5, 1.8, 0.2, 2000, 3500, 0.4, 0.6, 800, 2.5, 0.6));

		// ================================================================ 155 kite
		// Holds the string of a kite flying high in front of him (a child window, the string continues across both
		// windows). Then the string snaps: the kite drifts away and he waves goodbye.
		void KiteAt(Frame f, int kx, int ky, int tail, bool str)
		{
			if (str) Ln(f, kx + 3, ky + 3, 12, 15, Ink);
			var tl = tail == 0 ? new[] { (2, 5), (1, 6), (1, 7), (0, 8), (0, 9) } : new[] { (2, 5), (2, 6), (1, 7), (1, 8), (2, 9) };
			for (int i = 0; i < tl.Length; i++) f.Dot(kx + tl[i].Item1, ky + tl[i].Item2, i % 2 == 1 ? Heart : Ink);
			Put(f, kx, ky, "..R..", ".RRy.", "RRRyy", ".Ryy.", "..y..");
		}
		Frame Kite(int kx, int ky, int tail, bool str) { var f = new Frame(); KiteAt(f, kx, ky, tail, str); if (!str) Ln(f, kx + 3, ky + 4, kx + 5, ky + 8, Ink); return f; }
		Frame KiteHold(int h) { var f = Up(1, h, h == 0 ? 0 : 3, 1, 0, -1); Ln(f, 0, -4, 0, 4, Ink); return f; }
		int[] kRise = { F(Kite(7, 9, 0, true)), F(Kite(6, 7, 1, true)), F(Kite(5, 5, 0, true)), F(Kite(4, 3, 1, true)), F(Kite(4, 2, 0, true)) };
		int[] kSway = { F(Kite(4, 1, 0, true)), F(Kite(5, 2, 1, true)), F(Kite(4, 2, 0, true)), F(Kite(3, 1, 1, true)) };
		int kFree0 = F(Kite(4, 2, 0, false)), kFree1 = F(Kite(5, 2, 1, false));
		int khA = F(KiteHold(0)), khB = F(KiteHold(1));
		int kSnap = F(G(1, 0, 4, 1, 1, 0));
		int kWatch0 = F(Up(1, 0, 1, 0, 0, -1)), kWatch1 = F(Up(1, 1, 1, 0, 0, -1));
		var ksd = G(1, 0, 6, 4); ksd.Dot(5, 6, Tear); int kSad = F(ksd);
		int ktRise = Id(), ktSway = Id(), ktSnap = Id(), ktAway = Id();
		k.Lead(155, "kite", 130, ktSnap, Steps(One(k.Stand, 1), Cy(19, khA, khA, khB, khB)));
		k.Child(155, ktRise, "imageX-imageW*0.75", "imageY-imageH*0.9");
		Kid(ktRise, "kite rises", 130, kRise).Fade(0, 1).Then(100, ktSway);
		Kid(ktSway, "kite flies", 130, CyF(15, kSway));
		k.Show(ktSnap, "kite string snaps", 130, (kSnap, 3), (kWatch0, 3), (kWatch1, 3), (kSad, 4), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Happy, 2));
		k.Child(ktSnap, ktAway, "imageX-imageW*0.75", "imageY-imageH*0.9");
		Kid(ktAway, "kite drifts away", 130, CyF(14, kFree0, kFree1)).Move("-3", "-4", "-2", "-5").Fade(1, 0);
		Snd(155, new Sfx(2.6, 2206).Noise(0, 2.5, 0.25, 600, 900, 0.4, 0.8, 150, 0.8, 0.4));
		Snd(ktSnap, new Sfx(1.2, 2205).Tone(0, 0.12, 1400, 500, 0.3, Sfx.Wave.Triangle, 0.001, 0.08).Click(0, 0.6).Noise(0.1, 0.8, 0.25, 1500, 3000, 0.1, 0.5, 300));

		// ================================================================ 156 umbrella inside out
		// A gust turns his umbrella inside out; he tugs at it until it pops back, and giggles.
		void UmbN(Frame f) { f.Rect(3, -1, 10, 1, Purple).Rect(5, -2, 6, 1, Purple).Rect(7, -3, 2, 1, Purple).Dot(4, 0, Purple).Dot(8, 0, Purple).Dot(12, 0, Purple).Dot(7, -2, PurpleLight).Rect(13, -1, 1, 7, Line); }
		void UmbI(Frame f) { f.Dot(3, -4, Purple).Dot(4, -3, Purple).Dot(12, -4, Purple).Dot(11, -3, Purple).Dot(6, -3, Line).Dot(9, -3, Line).Rect(5, -2, 6, 1, Purple).Rect(6, -1, 4, 1, PurpleLight).Dot(8, -2, PurpleLight).Rect(13, -1, 1, 7, Line).Dot(12, -1, Line); }
		Frame Umb(int hem, int eyes, int mouth, bool inside, int lean, bool wind)
		{
			var f = G(1, hem, eyes, mouth, 0, 1);
			if (inside) UmbI(f); else UmbN(f);
			if (lean != 0) Lean(f, lean, 12);
			if (wind) { Streak(f, 0, -3, 3); Streak(f, 0, 3, 2); Streak(f, 0, 10, 2); }
			return f;
		}
		int umA = F(Umb(0, 0, 0, false, 0, false)), umB = F(Umb(1, 0, 0, false, 0, false));
		int umGust = F(Umb(0, 5, 1, false, 1, true)), umInside = F(Umb(1, 4, 1, true, 1, true));
		int umTugA = F(Umb(0, 5, 1, true, -1, false)), umTugB = F(Umb(1, 5, 1, true, 0, false));
		var upb = G(2, 0, 1, 3, 0, 1); UmbN(upb); upb.Dot(1, -3, Glow).Dot(15, -3, Glow); int umPop = F(upb);
		int umGiggle = F(Umb(1, 1, 3, false, 0, false));
		k.Show(156, "umbrella inside out", 150, (k.Stand, 1), (umA, 2), (umB, 2), (umGust, 2), (umInside, 3), (umTugA, 1), (umTugB, 1), (umTugA, 1), (umTugB, 1),
			(umPop, 2), (umGiggle, 3), (k.Happy, 1), (k.Stand, 1));
		Snd(156, new Sfx(2.6, 2207).Noise(0.75, 0.6, 0.45, 800, 2800, 0.15, 0.3, 200).Pop(1.05, 0.6, 260).Pop(2.1, 0.6, 380));

		// ================================================================ 157 painting
		// An easel (child window) stands in front of him; with each brush stroke the picture grows: a sun, grass,
		// a heart and a little flower. He shows it off proudly and the easel goes away.
		Frame Easel(int stage, bool sparkle)
		{
			var f = new Frame();
			Ln(f, 3, 9, 1, 15, Cone); Ln(f, 8, 9, 10, 15, Cone); Ln(f, 6, 9, 6, 14, Cone); f.Rect(1, 9, 10, 1, Cone);
			f.Rect(2, 1, 8, 8, Line).Rect(3, 2, 6, 6, Sheet).Rect(5, 0, 2, 1, Cone);
			if (stage >= 1) f.Rect(7, 2, 2, 2, Glow);
			if (stage >= 2) f.Rect(3, 7, 6, 1, Stem).Dot(4, 6, Stem);
			if (stage >= 3) HeartAt(f, 3, 3);
			if (stage >= 4) f.Dot(7, 6, Stem).Dot(7, 5, Heart);
			if (sparkle) f.Dot(1, 0, Glow).Dot(11, 3, Glow).Dot(0, 5, Glow).Dot(10, 0, GlowLight);
			return f;
		}
		char[] tips = { Glow, Glow, Stem, Heart, Heart };
		Frame Paint(int stage, bool up, int h)
		{
			var f = G(1, h, 2, 0, up ? 1 : 0, 0);
			if (up) f.Dot(0, 3, tips[stage]).Dot(0, 4, Ink); else f.Dot(0, 6, tips[stage]).Dot(0, 7, Ink);
			return f;
		}
		var easel = new int[5];
		for (int s = 0; s < 5; s++) easel[s] = F(Easel(s, false));
		int easelShine = F(Easel(4, true));
		var paintSteps = new List<(int f, int n)>();
		var easelSeq = new List<int>();
		Frame EaselDab(int stage, int i)       // the canvas while he paints: a dab of paint flicks off the brush
		{
			var f = Easel(stage, false);
			char c = tips[Math.Max(1, stage)];
			if (i % 2 == 0) f.Dot(10, 2, c).Dot(11, 1, c); else f.Dot(10, 6, c).Dot(11, 7, c);
			return f;
		}
		for (int i = 0; i < 16; i++) { paintSteps.Add((F(Paint(1 + i / 4, i % 2 == 0, (i / 2) % 2)), 1)); easelSeq.Add(F(EaselDab((i + 2) / 4, i))); }
		int esIn = Id(), esPaint = Id(), esDone = Id(), esOut = Id();
		k.Show(157, "painting", 160, Steps(One(k.Stand, 1), One(k.LookL, 1), paintSteps.ToArray(), One(k.Happy, 2), Cy(4, k.Wave, k.Wave2), One(k.Heart1, 2)));
		k.Child(157, esIn, "imageX-imageW*0.625", "imageY");
		Kid(esIn, "easel appears", 160, easel[0], easel[0]).Fade(0, 1).Then(100, esPaint);
		Kid(esPaint, "easel painting", 160, easelSeq.ToArray()).Then(100, esDone);
		Kid(esDone, "easel finished", 160, easelShine, easel[4], easelShine, easel[4]).Then(100, esOut);
		Kid(esOut, "easel goes away", 160, Rep(easel[4], 4)).Fade(1, 0);
		var paintSfx = new Sfx(3.4, 2208);
		for (int i = 1; i < 16; i += 2) paintSfx.Noise((2 + i) * 0.16, 0.12, 0.18, 3000, 1500, 0.02, 0.08, 600);
		paintSfx.Bell(2.9, Sfx.Midi(84), 0.15, 0.3).Bell(3.0, Sfx.Midi(88), 0.12, 0.3);
		Snd(157, paintSfx);

		// ================================================================ 158 toy piano
		// A toy piano (child window) appears in front of him; he plays a little tune, notes float up, he bows.
		void NoteC(Frame f, int x, int y, char c) { f.Rect(x + 1, y, 1, 3, c).Dot(x + 2, y, c).Rect(x, y + 2, 2, 1, c); }
		Frame Piano(int ph)
		{
			var f = new Frame();
			Put(f, 1, 7, "RRRRRRRRRRR", "RWKWKWWKWKR", "RWWWWWWWWWR", "RRRRRRRRRRR");
			f.Rect(2, 11, 1, 4, Ink).Rect(10, 11, 1, 4, Ink).Rect(2, 15, 9, 1, Shadow);
			if (ph >= 0)
			{
				(int x, int y)[] a = { (3, 3), (4, 1), (3, -1), (4, -3) }, b = { (8, -1), (9, -3), (8, 3), (9, 1) };
				NoteC(f, a[ph].x, a[ph].y, Purple); NoteC(f, b[ph].x, b[ph].y, BookBlue);
			}
			return f;
		}
		Frame Play(int h, bool down)
		{
			var f = G(1, h, 1, down ? 3 : 0);
			if (down) { Lean(f, -1, 8); f.Dot(0, 8, '.').Dot(0, 9, Line); }
			return f;
		}
		int piQuiet = F(Piano(-1));
		var piNotes = new int[4];
		for (int i = 0; i < 4; i++) piNotes[i] = F(Piano(i));
		int plA0 = F(Play(0, false)), plB0 = F(Play(0, true)), plA1 = F(Play(1, false)), plB1 = F(Play(1, true));
		var bow = G(1, 0, 1, 0); Lean(bow, -1, 12); int plBow = F(bow);
		int piIn = Id(), piPlay = Id(), piOut = Id();
		k.Show(158, "toy piano", 180, Steps(One(k.Stand, 1), One(k.LookL, 1), Cy(16, plA0, plB0, plA1, plB1), One(k.Happy, 1), One(plBow, 2), One(k.Happy, 1)));
		k.Child(158, piIn, "imageX-imageW*0.75", "imageY");
		Kid(piIn, "piano appears", 180, piQuiet, piQuiet).Fade(0, 1).Then(100, piPlay);
		Kid(piPlay, "piano plays", 180, CyF(16, piNotes)).Then(100, piOut);
		Kid(piOut, "piano goes away", 180, Rep(piQuiet, 4)).Fade(1, 0);
		var tune = new Sfx(3.8, 2209);
		int[] melody = { 72, 76, 79, 76, 77, 74, 71, 72 };
		for (int i = 0; i < melody.Length; i++) tune.Bell(0.36 + i * 0.36, Sfx.Midi(melody[i]), 0.25, 0.35);
		tune.Bell(3.24, Sfx.Midi(72), 0.18, 0.5).Bell(3.24, Sfx.Midi(76), 0.15, 0.5).Bell(3.24, Sfx.Midi(79), 0.15, 0.5);
		Snd(158, tune);

		// ================================================================ 159 jump rope
		Frame Rope(int ph)
		{
			Frame f;
			switch (ph)
			{
				case 0:     // rope over the head
					f = G(1, 0, 1, 3); Ln(f, 0, 7, 0, -1, Red); Ln(f, 15, 7, 15, -1, Red);
					f.Dot(1, -2, Red).Dot(14, -2, Red).Dot(2, -3, Red).Dot(13, -3, Red).Rect(3, -4, 10, 1, Red);
					break;
				case 1:     // rope low in front, he hops
					f = G(2, 1, 1, 3); Ln(f, 0, 8, 0, 13, Red); Ln(f, 15, 8, 15, 13, Red); f.Rect(1, 14, 14, 1, Red);
					break;
				case 2:     // up high, the rope passes under him
					f = G(2, 0, 4, 3, 0, 0, false); f.Shift(0, -1); f.Rect(6, 15, 4, 1, Shadow);
					Ln(f, 0, 7, 0, 13, Red); Ln(f, 15, 7, 15, 13, Red); f.Rect(1, 14, 14, 1, Red);
					break;
				default:    // landing, the rope goes up behind him
					f = G(1, 1, 1, 3); Ln(f, 0, 7, 0, 3, Red); Ln(f, 15, 7, 15, 3, Red); f.Dot(1, 3, Red).Dot(14, 3, Red);
					break;
			}
			return f;
		}
		int rp0 = F(Rope(0)), rp1 = F(Rope(1)), rp2 = F(Rope(2)), rp3 = F(Rope(3));
		p.A(159, "jump rope").Move("0", "0").Speed("110").Seq(k.Stand, rp0, rp1, rp2, rp3).Loop("6", 1).Then(100, 1);
		var ropeSfx = new Sfx(3.4, 2210);
		for (int i = 0; i < 7; i++) { double t = 0.33 + i * 0.44; ropeSfx.Click(t, 0.35).Thud(t, 0.2, 320, 140, 0.05).Thud(t + 0.11, 0.15, 160, 70, 0.1); }
		Snd(159, ropeSfx);

		// ================================================================ 160 bubble bath
		// Soaks in a little tub full of foam with a rubber duck; bubbles (child windows) float up and pop.
		Frame Bath(int duck, int eyes, int mouth, bool splash, int hem)
		{
			var f = G(0, hem, eyes, mouth); f.Shift(0, 2); f.Rect(0, 15, 16, 1, '.');
			Put(f, 0, 9, ".zz........zz...", "zWWzzWWzzWWzzWWz");
			Put(f, 0, 11, "oooooooooooooooo", ".BBBBBBBBBBBBBB.", ".BBBBBBBBBBBBBB.", "..BBBBBBBBBBBB..", "..n.qqqqqqqq.n..");
			Put(f, 0, 6 + duck, ".yy.", ".Ky.", "fyy.", ".yyy");
			if (splash) f.Dot(0, 3, Tear).Dot(1, 1, Tear).Dot(15, 2, Tear).Dot(14, 4, Tear).Dot(13, 0, Tear);
			return f;
		}
		Frame Suds(int w) { var f = new Frame(); Ring(f, 3 + w, 8, 1.6f); Ring(f, 8 - w, 4, 1.3f); Ring(f, 12 + w, 9, 1.5f); return f; }
		int baA = F(Bath(0, 1, 3, false, 0)), baB = F(Bath(1, 1, 0, false, 1)), baSplash = F(Bath(0, 4, 1, true, 0));
		int sud0 = F(Suds(0)), sud1 = F(Suds(1));
		var sp = new Frame(); foreach (var (x, y) in new[] { (2, 6), (5, 9), (8, 2), (11, 7), (13, 10), (7, 5) }) sp.Dot(x, y, Tear); int sudPop = F(sp);
		var sp2 = new Frame(); foreach (var (x, y) in new[] { (1, 7), (5, 11), (9, 1), (12, 8), (14, 12), (6, 4) }) sp2.Dot(x, y, Tear); int sudPop2 = F(sp2);
		int bbRise = Id(), bbPop = Id();
		k.Show(160, "bubble bath", 160, Steps(One(k.Stand, 1), One(k.Squash, 1), Cy(12, baA, baA, baB, baB), One(baSplash, 2), Cy(4, baA, baB), One(k.Squash, 1), One(k.Happy, 2)));
		k.Child(160, bbRise, "imageX", "imageY-imageH*0.2");
		k.Child(160, bbRise, "imageX+imageW*0.3", "imageY-imageH*0.05");
		Kid(bbRise, "bath bubbles rise", 120, Cat(Rep(blank, 2), CyF(14, sud0, sud1))).Move("0", "-2", "1", "-3").Then(100, bbPop);
		Kid(bbPop, "bath bubbles pop", 120, sudPop, sudPop2).Fade(1, 0.2);
		var bathSfx = new Sfx(3.6, 2211);
		double[] popAt = { 0.4, 0.62, 0.95, 1.2, 1.38, 1.7, 1.95, 2.5, 2.8, 3.1 };
		for (int i = 0; i < popAt.Length; i++) bathSfx.Pop(popAt[i], 0.22, 900 + (i * 137) % 600);
		bathSfx.Noise(2.24, 0.4, 0.6, 5000, 1500, 0.002, 0.3, 400);
		Snd(160, bathSfx);

		// ================================================================ 161 laundry line
		// A white sheet hangs on a laundry line (child window) — it looks just like a ghost! Boo waves at it, hides,
		// peeks, then realises it is only laundry flapping in the wind and giggles.
		Frame Laundry(int flap)
		{
			var f = new Frame();
			f.Rect(1, 0, 1, 15, Cone).Rect(14, 0, 1, 15, Cone).Rect(0, 15, 3, 1, Shadow).Rect(13, 15, 3, 1, Shadow);
			Ln(f, 2, 1, 7, 2, Ink); Ln(f, 8, 2, 13, 1, Ink);
			string[] sheet = { "oooooooo", "oWWWWWWo", "oWWWWWso", "oWWWWWso", "oWWWWWso", "oWWWWWso", "oWWWWWso", "oWWWWsso", "oWsoWWso", ".oo..oo." };
			for (int j = 0; j < sheet.Length; j++) Put(f, 4 + flap * (j >= 4 ? (j - 2) / 3 : 0), 3 + j, sheet[j]);
			f.Dot(5, 2, Red).Dot(5, 3, Red).Dot(10, 2, Red).Dot(10, 3, Red);
			return f;
		}
		int ldStill = F(Laundry(0)), ldLeft = F(Laundry(-1)), ldRight = F(Laundry(1));
		int ldIn = Id(), ldFlap = Id(), ldOut = Id();
		k.Show(161, "laundry line", 170, (k.Stand, 1), (k.LookL, 2), (k.Surprise, 3), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Hide, 4), (k.Peek, 3),
			(k.LookL, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 3), (k.Stand, 2));
		k.Child(161, ldIn, "imageX-imageW", "imageY");
		Kid(ldIn, "laundry appears", 170, ldStill, ldStill, ldStill).Fade(0, 1).Then(100, ldFlap);
		Kid(ldFlap, "laundry flaps", 170, CyF(20, ldRight, ldStill, ldLeft, ldStill)).Then(100, ldOut);
		Kid(ldOut, "laundry goes away", 170, Rep(ldStill, 5)).Fade(1, 0);
		// the sheet flapping in the breeze, a tiny "eek" when he sees it, then giggles
		Snd(161, new Sfx(4.6, 2225).Noise(0.5, 3.6, 0.18, 700, 1100, 0.5, 0.8, 200, 2.9, 0.5)
			.Tone(0.5, 0.18, 900, 1500, 0.12, Sfx.Wave.Triangle, 0.01, 0.08)
			.Tone(3.4, 0.1, 1100, 1300, 0.1, Sfx.Wave.Triangle, 0.01, 0.05).Tone(3.6, 0.1, 1100, 1300, 0.1, Sfx.Wave.Triangle, 0.01, 0.05));

		// ================================================================ 162 sunbathing
		// Sits on a striped towel with sunglasses under a smiling sun (child window), turns pink, jumps — hot! —
		// and shakes himself back to white.
		Frame Sunbathe(int burn, int hem)
		{
			var f = Sit(0, burn >= 2 ? 3 : 0, hem);
			f.Rect(4, 5, 2, 3, Sheet).Rect(10, 5, 2, 3, Sheet);
			f.Rect(3, 5, 4, 2, Ink).Rect(9, 5, 4, 2, Ink).Rect(7, 5, 2, 1, Ink).Dot(4, 5, GlowLight).Dot(10, 5, GlowLight);
			if (burn >= 1) f.Dot(2, 8, Blush).Dot(13, 8, Blush).Dot(4, 8, Blush).Dot(11, 8, Blush);
			if (burn >= 2) Recolor(f, Sheet, GumC);
			for (int x = 0; x < 16; x++) f[x, 15] = (x / 2) % 2 == 0 ? Red : Glow;
			return f;
		}
		Frame Sun(int w)
		{
			var f = new Frame();
			if (w == 0) f.Rect(7, 2, 2, 2, Flame).Rect(7, 12, 2, 2, Flame).Rect(2, 7, 2, 2, Flame).Rect(12, 7, 2, 2, Flame);
			else foreach (var (x, y) in new[] { (3, 3), (4, 4), (12, 3), (11, 4), (3, 12), (4, 11), (12, 12), (11, 11) }) f.Dot(x, y, Flame);
			FillCircle(f, 8, 8, 2.8f, Glow, GlowLight);
			f.Dot(6, 7, Ink).Dot(9, 7, Ink).Dot(7, 9, Flame).Dot(8, 9, Flame);
			return f;
		}
		int sbA = F(Sunbathe(0, 0)), sbB = F(Sunbathe(0, 1)), sbPinkA = F(Sunbathe(1, 0)), sbPinkB = F(Sunbathe(1, 1)), sbRed = F(Sunbathe(2, 0));
		var hot = G(2, 1, 4, 1, 1, 1); Recolor(hot, Sheet, GumC); hot.Dot(2, -3, Flame).Dot(1, -4, Flame).Dot(13, -3, Flame).Dot(14, -4, Flame); int sbHot = F(hot);
		int sun0 = F(Sun(0)), sun1 = F(Sun(1));
		int suIn = Id(), suShine = Id(), suOut = Id();
		k.Show(162, "sunbathing", 200, Steps(One(k.Stand, 1), One(k.Sit, 1), One(sbA, 2), Cy(8, sbA, sbA, sbB, sbB), One(sbPinkA, 2), One(sbPinkB, 1), One(sbRed, 3),
			One(sbHot, 3), One(k.Shiver1, 1), One(k.Shiver2, 1), One(k.Stand, 1), One(k.Happy, 2)));
		k.Child(162, suIn, "imageX-imageW*0.7", "imageY-imageH*0.9");
		Kid(suIn, "sun rises", 200, sun0, sun1, sun0).Fade(0, 1).Then(100, suShine);
		Kid(suShine, "sun shines", 200, CyF(18, sun1, sun0)).Then(100, suOut);
		Kid(suOut, "sun sets", 200, sun1, sun0, sun1, sun0, sun1).Fade(1, 0);
		Snd(162, new Sfx(4.3, 2212).Noise(3.6, 0.5, 0.3, 7000, 4000, 0.01, 0.3, 2500).Tone(3.6, 0.25, 600, 1200, 0.2, Sfx.Wave.Square, 0.01, 0.1).Soften(4000));

		// ================================================================ 163 paper boat
		// Folds a sheet of paper into a boat and puts it down; the boat (child window) sails away along the floor.
		string[] boatRows = { "..o...", ".oWo..", "oWWWWo", ".oooo." };
		string[] boatRows2 = { "...o..", "..oWo.", "oWWWWo", ".oooo." };
		Frame Fold(int s, int h, int eyes = 2, int mouth = 0)
		{
			var f = G(1, h, eyes, mouth);
			if (s == 0) Put(f, 0, 6, "ooooo", "oWWWo", "oWWWo", "ooooo");
			else if (s == 1) Put(f, 0, 6, "..o..", ".oWo.", "oWWWo", "ooooo");
			else Put(f, 0, 6, boatRows);
			return f;
		}
		Frame Crouch(bool boat)
		{
			var f = G(0, 0, 2, 3); f.Shift(0, 1); f.Rect(3, 15, 10, 1, Shadow);
			if (boat) { Put(f, 0, 11, boatRows); f.Dot(1, 15, Tear).Dot(4, 15, Tear); }
			return f;
		}
		Frame BoatC(int w)
		{
			var f = new Frame(); Put(f, 0, 11, w == 0 ? boatRows : boatRows2);
			if (w == 0) f.Dot(1, 15, Tear).Dot(4, 15, Tear).Dot(7, 14, Tear); else f.Dot(0, 15, Tear).Dot(3, 15, Tear).Dot(6, 15, Tear).Dot(8, 14, Tear);
			return f;
		}
		int fo0a = F(Fold(0, 0)), fo0b = F(Fold(0, 1)), fo1a = F(Fold(1, 0)), fo1b = F(Fold(1, 1)), fo2 = F(Fold(2, 0)), foShow = F(Fold(2, 1, 1, 3));
		int crBoat = F(Crouch(true)), crEmpty = F(Crouch(false));
		int bt0 = F(BoatC(0)), bt1 = F(BoatC(1));
		int boSail = Id(), boGo = Id(), boFade = Id();
		k.Lead(163, "paper boat", 170, boSail, (k.Stand, 1), (fo0a, 2), (fo0b, 1), (fo1a, 2), (fo1b, 1), (fo2, 2), (foShow, 2), (crBoat, 2));
		k.Show(boSail, "paper boat sails", 150, (crEmpty, 1), (k.LookL, 6), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Happy, 2));
		k.Child(boSail, boGo);
		Kid(boGo, "paper boat sailing", 110, CyF(40, bt0, bt1)).Move("-2", "0").Then(100, boFade).OnBorder(100, boFade).OnGravity(100, boFade);
		Kid(boFade, "paper boat fades", 150, bt0, bt1, bt0, bt1).Fade(1, 0);
		var crinkle = new Sfx(1.6, 2213);
		foreach (var t in new[] { 0.17, 0.51, 0.85, 1.2 }) crinkle.Noise(t, 0.15, 0.4, 6000, 3000, 0.002, 0.1, 1500, 40, 0.8);
		Snd(163, crinkle);

		// ================================================================ 164 cocoa
		// Warms up with a mug of cocoa: sips, gets a cocoa moustache, licks it off and sends hearts.
		void Mug(Frame f, int x, int y, int steam)
		{
			Put(f, x, y, "oWno", "RRRR", "RrRR", "RRRR", ".RR.");
			f.Dot(x + 4, y + 1, Red).Dot(x + 5, y + 2, Red).Dot(x + 4, y + 3, Red);
			if (steam == 0) f.Dot(x + 1, y - 1, Zz).Dot(x + 2, y - 2, Zz).Dot(x + 1, y - 3, Zz);
			if (steam == 1) f.Dot(x + 2, y - 1, Zz).Dot(x + 1, y - 2, Zz).Dot(x + 2, y - 3, Zz);
		}
		Frame Cocoa(int hem, int eyes, int mouth, int mx, int steam, bool moustache)
		{
			var f = G(1, hem, eyes, mouth); Mug(f, mx, 6, steam);
			if (moustache) f.Rect(5, 6, 2, 1, Cone).Rect(9, 6, 2, 1, Cone).Rect(6, 7, 4, 1, Cone);   // cocoa all around the mouth (row 6)
			return f;
		}
		int ccA = F(Cocoa(0, 0, 0, 0, 0, false)), ccB = F(Cocoa(1, 0, 0, 0, 1, false));
		int ccSip = F(Cocoa(0, 1, 0, 3, -1, false));
		int ccAhh = F(Cocoa(1, 0, 0, 0, -1, true));
		// licks the right side clean first, then the left side
		var cl1 = Cocoa(0, 0, 0, 0, -1, false); cl1.Rect(5, 6, 2, 1, Cone).Rect(6, 7, 2, 1, Cone).Dot(9, 6, Mouth).Dot(9, 7, Mouth).Dot(10, 7, Mouth); int ccLick1 = F(cl1);
		var cl2 = Cocoa(1, 0, 0, 0, 0, false); cl2.Dot(5, 7, Cone).Dot(6, 6, Mouth).Dot(6, 7, Mouth).Dot(5, 6, Mouth); int ccLick2 = F(cl2);
		var chh = Cocoa(0, 1, 0, 0, 1, false); HeartAt(chh, 12, -3); int ccHearts = F(chh);
		k.Show(164, "cocoa", 180, (k.Stand, 1), (ccA, 2), (ccB, 2), (ccSip, 4), (ccAhh, 3), (ccLick1, 2), (ccLick2, 2), (ccHearts, 3), (k.Happy, 1), (k.Stand, 1));
		Snd(164, new Sfx(2.4, 2214).Noise(0.9, 0.5, 0.4, 2200, 900, 0.02, 0.1, 400, 22, 0.7).Tone(1.62, 0.4, 500, 350, 0.12, Sfx.Wave.Sine, 0.05, 0.2, 5, 0.02));

		// ================================================================ 165 popcorn
		// Munches popcorn: kernels pop out of the bag and he catches them; a big one (child window) flies off.
		void Bag(Frame f, int pop)
		{
			Put(f, 0, 6, ".WyW.", "WsWyW", "RrRrR", "RrRrR", "RrRrR", "RrRrR", ".RrR.");
			if (pop == 1) f.Dot(1, 3, Sheet).Dot(2, 3, Glow).Dot(0, 1, Sheet);
			if (pop == 2) f.Dot(1, 0, Sheet).Dot(2, 0, Glow).Dot(0, 2, Glow).Dot(1, -2, Sheet);
		}
		Frame Pop(int hem, int eyes, int mouth, int pop) { var f = G(1, hem, eyes, mouth); Bag(f, pop); return f; }
		int pcBag = F(Pop(0, 2, 0, 0)), pcPop1 = F(Pop(1, 4, 1, 1)), pcPop2 = F(Pop(0, 4, 1, 2));
		var pcc = Pop(1, 1, 1, 0); pcc.Dot(7, 7, Glow).Dot(8, 7, Sheet); int pcCatch = F(pcc);
		int pcMunch = F(Pop(0, 1, 3, 0)), pcMunch2 = F(Pop(1, 1, 0, 0));
		var pcb = Pop(1, 4, 1, 0); pcb.Dot(0, 4, Glow).Dot(4, 4, Glow).Dot(2, 3, Glow); int pcBig = F(pcb);
		var pu0 = Up(1, 0, 1, 0, 0, -1); Bag(pu0, 0); int pcUp0 = F(pu0);
		var pu1 = Up(1, 1, 1, 0, 0, -1); Bag(pu1, 0); int pcUp1 = F(pu1);
		int pcLook = F(Pop(0, 2, 4, 0)), pcGiggle = F(Pop(1, 1, 3, 0));
		Frame Kernel(int w, int dy = 0) { var f = new Frame(); Put(f, 1, 3 + dy, w == 0 ? "Wy" : "yW", w == 0 ? "sW" : "Ws"); return f; }
		int kr0 = F(Kernel(0)), kr1 = F(Kernel(1)), krHop = F(Kernel(1, -2)), krHop2 = F(Kernel(0, -1));
		int pcBigPop = Id(), krFly = Id(), krLand = Id();
		k.Lead(165, "popcorn", 140, pcBigPop, (k.Stand, 1), (pcBag, 2), (pcPop1, 1), (pcPop2, 1), (pcCatch, 2), (pcMunch, 1), (pcMunch2, 1), (pcPop1, 1), (pcPop2, 1),
			(pcCatch, 2), (pcMunch, 1), (pcMunch2, 1));
		k.Show(pcBigPop, "popcorn big pop", 140, (pcBig, 2), (pcUp0, 2), (pcUp1, 2), (pcLook, 3), (pcMunch, 1), (pcMunch2, 1), (pcGiggle, 2), (k.Happy, 2));
		k.Child(pcBigPop, krFly);
		Kid(krFly, "popcorn flies", 70, CyF(12, kr0, kr1)).Move("-3", "-6", "-3", "11").Then(100, krLand).OnBorder(100, krLand);   // ends ~10 rows lower: lands on the floor, not in mid-air
		Kid(krLand, "popcorn lands", 120, kr0, krHop, kr1, krHop2, kr0, kr0).Fade(1, 0);
		var pops = new Sfx(2.2, 2215);
		foreach (var (t, fr) in new[] { (0.42, 700.0), (0.56, 950.0), (1.26, 800.0), (1.4, 1050.0) }) pops.Pop(t, 0.45, fr);
		foreach (var t in new[] { 0.98, 1.82 }) pops.Noise(t, 0.12, 0.35, 3500, 1500, 0.003, 0.08, 500);
		Snd(165, pops);
		Snd(pcBigPop, new Sfx(0.8, 2216).Pop(0, 0.8, 500).Tone(0.05, 0.4, 800, 1600, 0.12, Sfx.Wave.Triangle, 0.01, 0.15));

		// ================================================================ 166 spinning top
		// Throws a spinning top; it whirls along the floor on its own (child window), wobbles and tips over.
		string[][] topRows =
		{
			new[] { "..K..", ".RyR.", "RyRyR", ".RyR.", "..R.." },
			new[] { "..K..", ".yRy.", "yRyRy", ".yRy.", "..R.." },
			new[] { ".K...", "RyR..", "yRyR.", ".RyR.", "..R.." },
			new[] { "...K.", "..RyR", ".RyRy", ".RyR.", "..R.." },
			new[] { ".....", ".....", ".RyR.", "KRyRR", ".RyR." },
		};
		Frame TopC(int i) { var f = new Frame(); Put(f, 1, 10, topRows[i]); f.Rect(2, 15, 3, 1, Shadow); return f; }
		Frame TopHold(int h)
		{
			var f = G(1, h, 2, 0, 1, 0); Put(f, 0, -3, topRows[h]); f.Dot(1, 2, Ink).Dot(1, 3, Ink).Dot(0, 4, Ink);
			return f;
		}
		var tw = G(1, 0, 5, 0, 1, 0); Lean(tw, 1, 12); Put(tw, 1, -3, topRows[0]); tw.Dot(2, 2, Ink).Dot(2, 3, Ink).Dot(1, 4, Ink); int tpWind = F(tw);
		int tpHold0 = F(TopHold(0)), tpHold1 = F(TopHold(1)), tpThrow = F(G(1, 1, 4, 1, 0, 0));
		int ts0 = F(TopC(0)), ts1 = F(TopC(1)), tsL = F(TopC(2)), tsR = F(TopC(3)), tsDown = F(TopC(4));
		int tpWatch = Id(), tpSpin = Id(), tpWobble = Id(), tpFall = Id();
		k.Lead(166, "spinning top", 150, tpWatch, (k.Stand, 1), (tpHold0, 2), (tpHold1, 2), (tpHold0, 2), (tpWind, 3));
		k.Show(tpWatch, "watching the top", 150, (tpThrow, 2), (k.LookL, 4), (k.Happy, 2), (k.Wave, 1), (k.Wave2, 1), (k.LookL, 6), (k.Surprise, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 1));
		k.Child(tpWatch, tpSpin, "imageX-imageW*0.25", "imageY");     // lands just in front of him, not over his sheet
		Kid(tpSpin, "top spins", 70, CyF(24, ts0, ts1)).Move("-2", "0").Then(100, tpWobble).OnBorder(100, tpFall).OnGravity(100, tpFall);
		Kid(tpWobble, "top wobbles", 110, tsL, ts0, tsR, ts1, tsL, ts0, tsR, ts0).Move("-1", "0").Then(100, tpFall).OnBorder(100, tpFall).OnGravity(100, tpFall);
		Kid(tpFall, "top tips over", 200, tsDown, tsDown, tsDown, tsDown).Fade(1, 0);
		Snd(tpWatch, new Sfx(3.0, 2217).Tone(0.0, 1.7, 520, 380, 0.12, Sfx.Wave.Triangle, 0.01, 0.2, 0, 0, 24, 0.6)
			.Tone(1.7, 0.9, 380, 300, 0.1, Sfx.Wave.Triangle, 0.01, 0.2, 0, 0, 12, 0.6).Thud(2.6, 0.3, 250, 100, 0.08).Click(2.6, 0.4));

		// ================================================================ 167 rainbow
		// A rainbow (child window) draws itself in front of him, colour by colour; he is amazed and sends hearts.
		char[] bands = { Red, Flame, Glow, Stem, Tear, Purple };
		Frame Rainbow(int stage, bool sparkle)
		{
			var f = new Frame();
			for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
			{
				float dx = x + 0.5f - 8f, dy = 15f - y, d = MathF.Sqrt(dx * dx + dy * dy);
				if (d > 8.5f || d < 2.5f) continue;
				if (MathF.Atan2(dy, dx) < MathF.PI * (1 - stage / 4f) - 0.01f) continue;
				f[x, y] = bands[Math.Min(5, (int)(8.5f - d))];
			}
			if (sparkle) f.Dot(1, 6, GlowLight).Dot(14, 7, GlowLight).Dot(8, 5, Glow).Dot(4, 12, GlowLight);
			return f;
		}
		int[] rb = { F(Rainbow(1, false)), F(Rainbow(2, false)), F(Rainbow(3, false)), F(Rainbow(4, false)) };
		int rbShine = F(Rainbow(4, true));
		int rbWow = F(G(1, 0, 2, 1));
		int rbIn = Id(), rbShow = Id(), rbOut = Id();
		k.Show(167, "rainbow", 160, (k.Stand, 1), (k.LookL, 1), (rbWow, 3), (k.Heart1, 2), (k.Heart2, 2), (k.Heart1, 2), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1),
			(k.Happy, 3), (k.Giggle1, 1), (k.Giggle2, 1), (k.Stand, 2));
		k.Child(167, rbIn, "imageX-imageW", "imageY-imageH*0.2");
		Kid(rbIn, "rainbow appears", 160, rb).Fade(0.5, 1).Then(100, rbShow);
		Kid(rbShow, "rainbow shines", 160, CyF(12, rb[3], rbShine)).Then(100, rbOut);
		Kid(rbOut, "rainbow fades", 160, Rep(rb[3], 6)).Fade(1, 0);
		var chime = new Sfx(2.4, 2218);
		int[] arp = { 72, 76, 79, 84, 88 };
		for (int i = 0; i < arp.Length; i++) chime.Bell(0.16 * (i + 1), Sfx.Midi(arp[i]), 0.2, 0.5);
		Snd(167, chime);

		// ================================================================ 168 puddle splash
		// Finds a puddle and jumps in it with a splash — twice.
		void Puddle(Frame f) { f.Rect(1, 15, 14, 1, Tear).Dot(3, 15, GlowLight).Dot(10, 15, GlowLight); }
		var pd0 = G(1, 0, 2, 3, 0, 0, false); pd0.Rect(3, 3, 2, 1, Sheet).Rect(9, 3, 2, 1, Sheet); Puddle(pd0); int pdLook = F(pd0);
		var pd1 = G(0, 0, 5, 0, 0, 0, false); pd1.Shift(0, 1); Puddle(pd1); int pdCrouch = F(pd1);
		var pd2 = G(2, 1, 4, 3, 1, 1, false); pd2.Shift(0, -1); Puddle(pd2); int pdHigh = F(pd2);
		var pd3 = G(0, 1, 1, 1, 1, 1, false); pd3.Shift(0, 1); Puddle(pd3);
		foreach (var (x, y) in new[] { (0, 9), (1, 11), (0, 13), (15, 10), (14, 12), (15, 13), (2, 7), (13, 7) }) pd3.Dot(x, y, Tear);
		int pdSplash = F(pd3);
		var pd4 = G(1, 0, 1, 3, 0, 0, false); Puddle(pd4); foreach (var (x, y) in new[] { (0, 12), (15, 12), (1, 14), (14, 14) }) pd4.Dot(x, y, Tear); int pdDrops = F(pd4);
		var pd5 = G(1, 1, 1, 3, 0, 0, false); Puddle(pd5); int pdGiggle = F(pd5);
		k.Show(168, "puddle splash", 120, (k.Stand, 1), (pdLook, 2), (pdCrouch, 2), (pdHigh, 3), (pdSplash, 2), (pdDrops, 2), (pdGiggle, 2), (pdCrouch, 1), (pdHigh, 3),
			(pdSplash, 2), (pdDrops, 2), (pdGiggle, 2), (pdLook, 2), (k.Stand, 1));
		var splash = new Sfx(2.8, 2219);
		foreach (var t in new[] { 0.96, 2.16 }) splash.Noise(t, 0.35, 0.6, 5000, 1500, 0.002, 0.3, 400).Pop(t + 0.12, 0.2, 1300).Pop(t + 0.22, 0.15, 1600);
		Snd(168, splash);

		// ================================================================ 169 hula hoop
		void Hoop(Frame f, int dx, int hi)
		{
			for (int x = 0; x < 16; x++) if (f[x, 9] == '.') f[x, 9] = Purple;      // the back of the hoop, behind him
			for (int x = 1; x <= 14; x++) f[x + dx, 11] = Purple;
			f.Dot(dx, 10, Purple).Dot(15 + dx, 10, Purple).Dot(hi + dx, 11, PurpleLight).Dot(hi + dx + 1, 11, PurpleLight);
		}
		var hu0 = G(1, 0, 1, 3); Lean(hu0, 1, 10); Hoop(hu0, -1, 3);
		var hu1 = G(1, 1, 1, 0); Hoop(hu1, 0, 7);
		var hu2 = G(1, 0, 1, 3); Lean(hu2, -1, 10); Hoop(hu2, 1, 11);
		var hu3 = G(1, 1, 1, 0); Hoop(hu3, 0, 5);
		k.Loop(169, "hula hoop", 100, "8", F(hu0), F(hu1), F(hu2), F(hu3));

		// ================================================================ 170 watering can
		// Waters a flower pot (child window): a sprout grows, gets leaves and a bud, and blooms. Hearts!
		Frame Plant(int stage, int drops)
		{
			var f = new Frame();
			Put(f, 5, 12, "nKKKn", ".OOO.", ".OOO.");
			f.Rect(6, 15, 3, 1, Shadow);
			if (stage == 1) f.Dot(7, 11, Stem).Dot(6, 10, Stem).Dot(8, 10, Stem);
			if (stage == 2) f.Rect(7, 8, 1, 4, Stem).Dot(6, 9, Stem).Dot(5, 8, Stem).Dot(8, 10, Stem);
			if (stage >= 3) { f.Rect(7, 6, 1, 6, Stem).Dot(6, 9, Stem).Dot(5, 8, Stem).Dot(8, 10, Stem).Dot(9, 9, Stem); }
			if (stage == 3) f.Dot(7, 5, Heart).Dot(7, 4, Heart);
			if (stage == 4) Flower(f, 7, 3);
			if (drops == 0) f.Dot(8, 8, Tear).Dot(9, 10, Tear);
			if (drops == 1) f.Dot(8, 9, Tear).Dot(8, 11, Tear);
			return f;
		}
		Frame Water(int h)
		{
			var f = G(1, h, 2, 0);
			Put(f, 0, 7, "i.iiii", ".iiiii", "..iiii", "..iiii"); f.Dot(6, 8, Mint).Dot(6, 9, Mint);
			f.Dot(0, 9 + h, Tear);
			return f;
		}
		int plPot = F(Plant(0, -1)), plBloom = F(Plant(4, -1));
		var pbs = Plant(4, -1); pbs.Dot(4, 2, Glow).Dot(10, 4, GlowLight).Dot(9, 1, Glow).Dot(4, 6, GlowLight); int plSparkle = F(pbs);
		var plWater = new int[10];
		for (int i = 0; i < 10; i++) plWater[i] = F(Plant(i < 4 ? i / 2 : 1 + i / 4, i % 2));
		int waA = F(Water(0)), waB = F(Water(1));
		int pnIn = Id(), pnGrow = Id(), pnBloom = Id(), pnOut = Id();
		k.Show(170, "watering can", 170, Steps(One(k.Stand, 1), One(k.LookL, 1), Cy(10, waA, waB), One(k.Surprise, 2), Cy(4, k.Heart1, k.Heart2), One(k.Happy, 2)));
		k.Child(170, pnIn, "imageX-imageW*0.6", "imageY");
		Kid(pnIn, "flower pot appears", 170, plPot, plPot).Fade(0, 1).Then(100, pnGrow);
		Kid(pnGrow, "flower grows", 170, plWater).Then(100, pnBloom);
		Kid(pnBloom, "flower blooms", 170, plSparkle, plBloom, plSparkle, plBloom).Then(100, pnOut);
		Kid(pnOut, "flower pot goes away", 170, Rep(plBloom, 4)).Fade(1, 0);
		Snd(170, new Sfx(2.8, 2220).Noise(0.34, 1.7, 0.3, 3000, 2500, 0.1, 0.2, 800, 18, 0.5).Bell(2.04, Sfx.Midi(84), 0.2, 0.4).Bell(2.14, Sfx.Midi(91), 0.18, 0.5));

		// ================================================================ 171 drum
		Frame Drum(int hem, int eyes, int mouth, int stick)     // stick 0 up, 1 hitting, 2 high
		{
			var f = G(stick == 2 ? 2 : 1, hem, eyes, mouth, stick == 1 ? 0 : 1, 0);
			Put(f, 0, 10, "oLLLLLLo", "RRRRRRRR", "yRyRyRyR", "RRRRRRRR", "oooooooo");
			if (stick == 0) f.Dot(1, 4, Ink).Dot(2, 3, Ink).Dot(2, 2, Ink);
			if (stick == 1) f.Dot(1, 9, Ink).Dot(2, 9, Ink).Dot(3, 9, Ink).Dot(4, 8, Glow).Dot(2, 8, Glow);
			if (stick == 2) f.Dot(1, 3, Ink).Dot(1, 2, Ink).Dot(2, 1, Ink);
			return f;
		}
		int drA0 = F(Drum(0, 1, 0, 0)), drB0 = F(Drum(1, 1, 3, 1)), drA1 = F(Drum(1, 1, 3, 0)), drB1 = F(Drum(0, 1, 0, 1));
		int drHigh = F(Drum(0, 4, 1, 2));
		var dbg = Drum(1, 5, 3, 1); Star(dbg, 4, 7); dbg.Dot(13, 0, Glow).Dot(1, 1, Glow); int drBig = F(dbg);
		k.Show(171, "drum", 140, Steps(One(k.Stand, 1), Cy(12, drA0, drB0, drA1, drB1), One(drHigh, 2), One(drBig, 2), One(k.Happy, 2), One(k.Stand, 1)));
		var drumSfx = new Sfx(3.0, 2221);
		for (int i = 0; i < 6; i++) drumSfx.Thud((2 + 2 * i) * 0.14, 0.45, 160, 60, 0.15);
		drumSfx.Thud(2.1, 0.7, 120, 45, 0.3).Noise(2.1, 0.5, 0.25, 8000, 5000, 0.002, 0.45, 3000);
		Snd(171, drumSfx);

		// ================================================================ 172 toaster
		// A toaster (child window) pops a slice of toast into the air; Boo catches it and eats it in three bites.
		void Toast(Frame f, int x, int y) => Put(f, x, y, "OOOO", "OnnO", "OnnO");
		Frame Toaster(int toast, int dx = 0)       // toast: 0 in the slot, 1 popping, 2 high, 3 flying, 4 near Boo, -1 none
		{
			var f = new Frame();
			Put(f, 2 + dx, 9, "ooKKKKoo", "osssssso", "osWsssso", "osssssKo", "osssssso", ".o....o.");
			f.Rect(3, 15, 6, 1, Shadow);
			switch (toast)
			{
				case 0: Put(f, 4 + dx, 8, "OOOO"); break;
				case 1: Toast(f, 4, 5); break;
				case 2: Toast(f, 5, 1); break;
				case 3: Toast(f, 9, 0); break;
				case 4: Toast(f, 12, 4); break;
			}
			return f;
		}
		int toIdle = F(Toaster(0)), toShake = F(Toaster(0, 1)), toPop = F(Toaster(1)), toHigh = F(Toaster(2)), toFly = F(Toaster(3)), toNear = F(Toaster(4)), toEmpty = F(Toaster(-1));
		var cat = G(1, 0, 4, 1, 1, 0); Toast(cat, 0, 4); int toCatch = F(cat);
		Frame Bite(int b, int hem)
		{
			var f = G(1, hem, 1, b % 2 == 0 ? 1 : 3);
			string[][] rows = { new[] { "OOO.", "OnnO", "OnnO" }, new[] { "OO..", "OnO.", "OnnO" }, new[] { "O...", "On..", "OnO." } };
			if (b < 3) Put(f, 3, 6, rows[b]);
			return f;
		}
		int bt1a = F(Bite(0, 0)), bt2a = F(Bite(1, 1)), bt3a = F(Bite(2, 0));
		int toUp = F(Up(1, 0, 1, 0, 0, -1)), toUp2 = F(Up(1, 1, 4, 0, 0, 0));
		int tsIn = Id(), tsToast = Id(), tsOut = Id();
		k.Show(172, "toaster", 140, (k.Stand, 1), (k.LookL, 3), (k.Blink, 1), (k.LookL, 1), (k.Surprise, 1), (toUp, 1), (toUp2, 1), (k.Surprise, 1), (toCatch, 2),
			(bt1a, 2), (bt2a, 2), (bt3a, 2), (k.Happy, 2));
		k.Child(172, tsIn, "imageX-imageW*0.75", "imageY");
		Kid(tsIn, "toaster appears", 140, toIdle, toIdle).Fade(0, 1).Then(100, tsToast);
		Kid(tsToast, "toaster pops", 140, toIdle, toShake, toIdle, toShake, toPop, toHigh, toFly, toNear).Then(100, tsOut);
		Kid(tsOut, "toaster goes away", 140, Rep(toEmpty, 8)).Fade(1, 0);
		var toastSfx = new Sfx(2.6, 2222).Bell(0.84, 1568, 0.3, 0.4).Pop(0.84, 0.4, 400);
		foreach (var t in new[] { 1.68, 1.96, 2.24 }) toastSfx.Noise(t, 0.1, 0.45, 4000, 1500, 0.003, 0.07, 600);
		Snd(172, toastSfx);

		// ================================================================ 173 hand fan
		// A hot day: sweating, he fans himself with a little folding fan until he feels better.
		Frame Fan(int hem, int eyes, int mouth, int fan, bool sweat)   // fan 0 up, 1 down
		{
			var f = G(1, hem, eyes, mouth, fan == 0 ? 1 : 0, 0);
			if (fan == 0) { Put(f, 0, 1, "hyhy", ".hyh", "..K."); f.Dot(1, 4, Ink); }
			else { Put(f, 0, 5, ".hy.", "hyhy", "hyh.", ".K.."); f.Dot(4, 6, Zz).Dot(5, 7, Zz); }
			if (sweat) f.Dot(13, 1, Tear).Dot(13, 2, Tear).Dot(5, -3, Flame).Dot(6, -4, Flame).Dot(10, -3, Flame).Dot(11, -4, Flame);
			return f;
		}
		var fh0 = G(1, 0, 6, 1); fh0.Dot(13, 1, Tear).Dot(13, 2, Tear).Dot(5, -3, Flame).Dot(6, -4, Flame).Dot(10, -3, Flame).Dot(11, -4, Flame); int fnHot0 = F(fh0);
		var fh1 = G(1, 1, 6, 1); fh1.Dot(13, 2, Tear).Dot(13, 3, Tear).Dot(5, -4, Flame).Dot(6, -3, Flame).Dot(10, -4, Flame).Dot(11, -3, Flame); int fnHot1 = F(fh1);
		int fnU0 = F(Fan(0, 6, 1, 0, true)), fnD0 = F(Fan(1, 6, 1, 1, true)), fnU1 = F(Fan(1, 1, 0, 0, false)), fnD1 = F(Fan(0, 1, 0, 1, false));
		int fnRelief = F(Fan(0, 1, 3, 1, false));
		k.Show(173, "hand fan", 130, Steps(One(k.Stand, 1), One(fnHot0, 2), One(fnHot1, 2), Cy(6, fnU0, fnD0), Cy(6, fnU1, fnD1), One(fnRelief, 3), One(k.Happy, 2), One(k.Stand, 1)));
		var fanSfx = new Sfx(2.6, 2223);
		for (int i = 0; i < 6; i++) fanSfx.Noise(0.65 + i * 0.26, 0.15, 0.2, 1500, 800, 0.03, 0.1, 300);
		Snd(173, fanSfx);

		// ================================================================ 174 sled
		// Hops on a little red sled and slides along the floor, arms up; when he stops he hops off and the sled
		// (a child window) is left behind and fades away.
		string[] sledRows = { ".RRRRRRRRRRRRR..", "nRrRRrRRrRRrRR..", "n..n.......n....", ".nnnnnnnnnnnnnn." };
		Frame Sled(int lift, int eyes, int mouth, int spray, int hem, int arms)
		{
			var f = G(lift, hem, eyes, mouth, arms, arms, false);
			Put(f, 0, 12, sledRows);
			if (spray == 0) f.Dot(15, 13, Zz).Dot(14, 11, Zz).Dot(15, 9, Zz);
			if (spray == 1) f.Dot(15, 12, Zz).Dot(15, 14, Zz).Dot(14, 10, Zz);
			return f;
		}
		var sh = G(2, 0, 4, 3, 0, 0, false); sh.Shift(0, -1); Put(sh, 0, 12, sledRows); int slHover = F(sh);
		int slSit = F(Sled(2, 2, 0, -1, 0, 0)), slReady = F(Sled(2, 4, 3, -1, 1, 0));
		int slRideA = F(Sled(2, 2, 3, 0, 0, 1)), slRideB = F(Sled(2, 2, 3, 1, 1, 1));
		var sst = Sled(2, 1, 0, -1, 0, 0); sst.Dot(0, 10, Zz).Dot(1, 9, Zz).Dot(0, 12, Zz).Dot(2, 11, Zz); int slStop = F(sst);   // a puff of snow flies forward
		int slStop2 = F(Sled(2, 1, 3, -1, 1, 0));
		var sledOnly = new Frame(); Put(sledOnly, 0, 12, sledRows); int slAlone = F(sledOnly);
		var hop = G(2, 1, 1, 3, 1, 1, false); hop.Shift(0, -1); hop.Rect(6, 15, 4, 1, Shadow); int slHop = F(hop);
		int slRide = Id(), slHalt = Id(), slOff = Id(), slLeft = Id();
		k.Lead(174, "sled", 140, slRide, (k.Stand, 1), (k.LookL, 1), (slHover, 2), (slSit, 2), (slReady, 2));
		p.A(slRide, "sled ride").Move("-6", "0").Speed("70").Loop("random/10+6", 0).Seq(slRideA, slRideA, slRideB, slRideB)
			.Then(100, slHalt).OnBorder(100, slHalt).OnGravity(100, 5);
		k.Lead(slHalt, "sled stops", 150, slOff, (slStop, 2), (slStop2, 2));
		p.A(slOff, "hop off the sled").Move("-4", "0").Speed("120").Seq(slHop, slHop, k.Squash, k.Happy, k.Happy, k.Stand)
			.Then(100, 1).OnBorder(100, 1).OnGravity(100, 5);
		k.Child(slOff, slLeft);
		Kid(slLeft, "sled left behind", 150, Rep(slAlone, 6)).Fade(1, 0);
		Snd(slRide, new Sfx(2.2, 2224).Noise(0, 2.0, 0.4, 1500, 3000, 0.1, 0.6, 700).Tone(0.05, 0.5, 600, 1100, 0.1, Sfx.Wave.Triangle, 0.02, 0.2));
	}
}
