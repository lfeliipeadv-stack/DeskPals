namespace PetGen;

static partial class BooPet
{
	// 175-199: ghostly powers, magic and the night sky. Portal, puddle, stretchy ghost, haunted broom, levitating book,
	// crystal ball, shooting star wish, moon song, firefly, paper lantern, will-o-wisp, ectoplasm bubbles, three mini Boos,
	// teleport, aurora, bat, constellation, sinking through the floor, moon hammock, magic hat, ghost tornado, runaway
	// shadow, star in a jar, potion cauldron and orbiting orbs. Second parts 500-519, child windows 520-559.
	static void ScenesD(Kit k)
	{
		var p = k.P;
		int blank = k.Empty;

		// ------------------------------------------------------------ small local helpers
		void Ln(Frame f, int x0, int y0, int x1, int y1, char c)
		{
			int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
			while (true)
			{
				f.Dot(x0, y0, c);
				if (x0 == x1 && y0 == y1) break;
				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; x0 += sx; }
				if (e2 <= dx) { err += dx; y0 += sy; }
			}
		}
		void Put(Frame f, int x, int y, params string[] rows) { for (int j = 0; j < rows.Length; j++) f.Stamp(x, y + j, rows[j].Replace('.', ' ')); }
		void Ellipse(Frame f, float cx, float cy, float rx, float ry, char c)
		{
			for (int y = -4; y <= 15; y++) for (int x = 0; x < 16; x++)
			{
				float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
				if (dx * dx + dy * dy <= 1) f[x, y] = c;
			}
		}
		Frame Recolor(Frame f, char from, char to)
		{
			for (int y = -4; y <= 15; y++) for (int x = 0; x < 16; x++) if (f[x, y] == from) f[x, y] = to;
			return f;
		}
		void Behind(Frame f, int x, int y, char c) { if (f[x, y] == '.') f[x, y] = c; }
		// a sparkle: a single dot on even t, a little cross on odd t
		void Twinkle(Frame f, int x, int y, int t)
		{
			if (t % 2 == 0) f.Dot(x, y, GlowLight);
			else f.Dot(x, y, Glow).Dot(x - 1, y, GlowLight).Dot(x + 1, y, GlowLight).Dot(x, y - 1, GlowLight).Dot(x, y + 1, GlowLight);
		}
		// Boo looking up (dx -1 up and ahead, 0 up, 1 up and back) or down at the floor
		Frame Up(int lift, int hem, int mouth = 0, int la = 0, int ra = 0, int dx = 0, bool shadow = true)
		{
			var f = G(lift, hem, 7, mouth, la, ra, shadow); int y = 5 - lift;
			f.Rect(4 + dx, y - 2, 2, 2, Ink).Rect(10 + dx, y - 2, 2, 2, Ink);
			return f;
		}
		Frame Down(int lift, int hem, int mouth = 4, bool shadow = true)
		{
			var f = G(lift, hem, 7, mouth, 0, 0, shadow); int y = 5 - lift;
			f.Rect(4, y, 2, 2, Ink).Rect(10, y, 2, 2, Ink);
			return f;
		}
		int[] Repeat(int times, params int[] frames) { var l = new List<int>(); for (int i = 0; i < times; i++) l.AddRange(frames); return l.ToArray(); }

		// ================================================================ 175 portal
		// Boo draws a swirling portal in front of him, dives into it and pops back out.
		Frame Portal(int size, int t)
		{
			var f = new Frame();
			float[] rx = { 1.2f, 2.4f, 3.4f, 4.4f }, ry = { 2f, 3.8f, 5.6f, 7.2f };
			float cx = 8, cy = 7.5f;
			for (int y = -4; y < 15; y++) for (int x = 0; x < 16; x++)
			{
				float dx = (x + 0.5f - cx) / rx[size], dy = (y + 0.5f - cy) / ry[size], d = dx * dx + dy * dy;
				if (d > 1) continue;
				if (d > 0.62f) f[x, y] = Purple;
				else if (d > 0.4f) f[x, y] = PurpleLight;
				else
				{
					double s = Math.Atan2(dy, dx) / (2 * Math.PI) * 2 + Math.Sqrt(d) * 2.2 - t * 0.25;
					f[x, y] = s - Math.Floor(s) < 0.3 ? Purple : Ink;
				}
			}
			if (size == 3)
			{
				(int x, int y)[] sp = { (2, 1), (14, 3), (1, 10), (14, 12), (8, -1) };
				for (int i = 0; i < sp.Length; i++) if ((i + t) % 2 == 0) f.Dot(sp[i].x, sp[i].y, i % 2 == 0 ? GlowLight : PurpleLight);
				f.Rect(5, 15, 6, 1, Shadow);
			}
			return f;
		}
		Frame Cast(int t)
		{
			var f = G(1, t % 2, 2, 3, 1, 0);
			f.Dot(0, 4 - t % 2, GlowCyan).Dot(1, 3 + t % 2, GlowLight);
			return f;
		}
		int pc0 = k.F(Cast(0)), pc1 = k.F(Cast(1));
		var dive = new int[8];
		for (int i = 0; i < 8; i++) { var f = G(1, i % 2, 1, 3, 1, 1); Lean(f, -1, 10); dive[i] = k.F(Fade(f, i / 2)); }
		var po = new int[4];
		for (int i = 0; i < 4; i++) po[i] = k.F(Portal(3, i));
		int ps0 = k.F(Portal(0, 0)), ps1 = k.F(Portal(1, 1)), ps2 = k.F(Portal(2, 2));

		k.Lead(175, "portal", 120, 500, (k.Stand, 1), (pc0, 1), (pc1, 1), (pc0, 1), (pc1, 1), (pc0, 1), (pc1, 1), (k.Happy, 1));
		p.A(500, "portal dive").Move("-5", "0").Speed("100").Fade(1, 0.2).Seq(dive)
			.Then(100, 501).OnBorder(100, 501).OnGravity(100, 501);
		k.Show(501, "portal pop out", 120, (k.Gone, 1), (k.Fade3, 1), (k.Fade2, 1), (k.Fade1, 1), (k.Surprise, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 1)).Fade(0.4, 1);
		k.Child(175, 520, "imageX-imageW*0.9", "imageY");
		p.A(520, "portal opens").Speed("120").Fade(0.5, 1).Seq(ps0, ps1, ps2, po[3], po[0], po[1], po[2], po[3]).Then(100, 521);
		p.A(521, "portal swirls").Speed("100").Seq(Repeat(3, po)).Then(100, 522);
		p.A(522, "portal closes").Speed("100").Fade(1, 0).Seq(po[0], ps2, ps1, ps0);
		k.Sound(175, new Sfx(1.6, 2300).Tone(0, 1.2, 220, 880, 0.25, Sfx.Wave.Sine, 0.3, 0.4, vibHz: 7, vibDepth: 0.04)
			.Noise(0.1, 1.3, 0.15, 600, 2500, 0.4, 0.5, highPass: 300, amHz: 5, amDepth: 0.5).Soften(3000));

		// ================================================================ 176 puddle
		// Boo melts into a little puddle, slides along the floor and pulls himself back together.
		Frame Melt(int m, int look)
		{
			var f = new Frame();
			float[] cy = { 0, 16f, 16f, 16f }, rx = { 0, 7f, 7.6f, 7.9f }, ry = { 0, 9.3f, 5.9f, 3.9f };
			Ellipse(f, 8 - (m == 3 ? look : 0) * 0.5f, cy[m], rx[m], ry[m], Sheet);
			OutlineAll(f);
			switch (m)
			{
				case 1:
					f.Rect(4, 9, 2, 2, Ink).Rect(10, 9, 2, 2, Ink).Dot(3, 11, Blush).Dot(12, 11, Blush).Rect(7, 11, 2, 1, Mouth);
					f.Dot(3, 14, Shade).Dot(12, 14, Shade);
					break;
				case 2:
					f.Rect(4, 11, 2, 2, Ink).Rect(10, 11, 2, 2, Ink).Dot(3, 13, Blush).Dot(12, 13, Blush).Rect(7, 13, 2, 1, Mouth);
					break;
				default:
					f.Rect(4 - look, 13, 2, 2, Ink).Rect(10 - look, 13, 2, 2, Ink).Dot(12 + look, 14, Shade);
					if (look == 1) f.Dot(15, 15, Shade); else f.Dot(13, 13, Shade);
					break;
			}
			return f;
		}
		int ml1 = k.F(Melt(1, 0)), ml2 = k.F(Melt(2, 0)), ml3 = k.F(Melt(3, 0)), mlA = k.F(Melt(3, 1)), mlB = k.F(Melt(3, 0));
		k.Lead(176, "puddle", 120, 502, (k.Happy, 1), (k.Squash, 1), (ml1, 2), (ml2, 2), (ml3, 2));
		p.A(502, "puddle slide").Move("-4", "0").Speed("90").Loop("6", 0).Seq(mlA, mlA, mlB, mlB)
			.Then(100, 503).OnBorder(100, 503).OnGravity(100, 5);
		k.Show(503, "puddle reform", 120, (ml3, 1), (ml2, 1), (ml1, 1), (k.Squash, 1), (k.Stand2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 2));
		k.Sound(176, new Sfx(1.0, 2301).Tone(0.12, 0.35, 700, 160, 0.35, Sfx.Wave.Sine, 0.01, 0.15).Tone(0.5, 0.35, 420, 110, 0.3, Sfx.Wave.Sine, 0.01, 0.2));
		k.Sound(503, new Sfx(0.7, 2302).Tone(0, 0.45, 140, 620, 0.35, Sfx.Wave.Sine, 0.02, 0.1).Pop(0.45, 0.3, 900));

		// ================================================================ 177 stretchy ghost
		// He stretches up, tall as a noodle, to peek around, wobbles and snaps back: boing!
		Frame Stretch(int s, int eyes, int mouth, int lean = 0)
		{
			var src = G(0, 0, eyes, mouth); var plain = Ghost(0, 0);
			var f = new Frame();
			for (int y = -4; y <= 15; y++) for (int x = 0; x < 16; x++)
			{
				if (y > 8) f[x, y] = src[x, y];
				else if (y <= 8 - s) f[x, y] = src[x, y + s];
				else f[x, y] = plain[x, 8];
			}
			if (lean != 0) Lean(f, lean, 6 - s);
			return f;
		}
		int st1 = k.F(Stretch(1, 0, 0)), st2 = k.F(Stretch(2, 0, 1)), stL = k.F(Stretch(4, 2, 0)), stR = k.F(Stretch(4, 3, 0)), stW = k.F(Stretch(4, 4, 1));
		int stWa = k.F(Stretch(4, 1, 3, -1)), stWb = k.F(Stretch(4, 1, 3, 1));
		k.Show(177, "stretchy", 110, (k.Stand, 1), (k.Squash, 1), (st1, 1), (st2, 1), (stL, 3), (stR, 3), (stW, 2), (stWa, 1), (stWb, 1), (stWa, 1), (stWb, 1),
			(st2, 1), (k.Squash, 1), (k.Stand2, 1), (k.Happy, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Stand, 1));
		k.Sound(177, new Sfx(2.6, 2303).Tone(0.2, 0.5, 300, 900, 0.25, Sfx.Wave.Sine, 0.02, 0.05, vibHz: 12, vibDepth: 0.02)
			.Tone(1.5, 0.35, 900, 250, 0.25, Sfx.Wave.Sine, 0.01, 0.05).Tone(1.85, 0.4, 180, 420, 0.3, Sfx.Wave.Sine, 0.01, 0.3, vibHz: 18, vibDepth: 0.1));

		// ================================================================ 178 haunted broom
		// Boo possesses a broom: it sweeps on its own in front of him while he conducts it with his little hand.
		Frame Broom(int tilt, int t)
		{
			var f = new Frame();
			int bx = 8 - tilt, tx = 8 + tilt * 3;
			Ln(f, tx, -2, bx, 9, Cone);
			f.Rect(bx - 1, 10, 3, 1, Red);
			f.Rect(bx - 1, 11, 3, 1, Glow);
			f.Rect(bx - 2 - tilt, 12, 5, 1, Glow).Rect(bx - 2 - tilt, 13, 5, 1, Glow);
			for (int x = bx - 3 - tilt * 2; x <= bx + 3 - tilt * 2; x++) f.Dot(x, 14, x % 2 == 0 ? Flame : Glow);
			for (int x = bx - 2 - tilt; x <= bx + 2 - tilt; x += 2) f.Dot(x, 12, Flame);
			if (tilt != 0) { int dx = bx - 6 - tilt * 2; f.Dot(dx, 14, Shade).Dot(dx - 1, 13, Shade).Dot(dx + t % 2, 12, Shadow); }   // dust
			f.Dot(tx + 1, -1 + t % 2, GlowCyan).Dot(bx + 2, 4 + t % 3, GlowCyan).Dot(tx - 2, 2 - t % 2, GlowLight);              // ghostly glow
			return f;
		}
		Frame Conduct(int t)
		{
			var f = G(1, t % 2, 2, t % 4 == 0 ? 3 : 0, t % 2 == 0 ? 1 : 0, 0);
			if (t % 2 == 0) f.Dot(0, 3, GlowCyan).Dot(1, 4, GlowLight);
			else f.Dot(0, 7, GlowCyan).Dot(1, 6, GlowLight);
			return f;
		}
		var cond = new int[4]; for (int i = 0; i < 4; i++) cond[i] = k.F(Conduct(i));
		int brUp0 = k.F(Broom(0, 0)), brUp1 = k.F(Broom(0, 1)), brL = k.F(Broom(-1, 0)), brR = k.F(Broom(1, 1)), brM = k.F(Broom(0, 2));
		k.Show(178, "haunted broom", 150, (k.Stand, 1), (cond[0], 1), (cond[1], 1), (cond[0], 1),
			(cond[1], 1), (cond[2], 1), (cond[3], 1), (cond[0], 1), (cond[1], 1), (cond[2], 1), (cond[3], 1), (cond[0], 1),
			(cond[1], 1), (cond[2], 1), (cond[3], 1), (cond[0], 1), (cond[1], 1), (cond[2], 1), (cond[3], 1), (cond[0], 1),
			(k.Happy, 2), (k.Wave, 1), (k.Wave2, 1));
		k.Child(178, 523, "imageX-imageW*0.9", "imageY");
		p.A(523, "broom appears").Speed("150").Fade(0, 1).Seq(brUp0, brUp1, brUp0, brUp1).Then(100, 524);
		p.A(524, "broom sweeps").Move("-1", "0").Speed("150").Seq(Repeat(4, brL, brM, brR, brM)).Then(100, 525).OnBorder(100, 525);
		p.A(525, "broom rests").Speed("150").Fade(1, 0).Seq(brUp0, brUp1, brUp0, brUp1);
		{
			var s = new Sfx(3.4, 2304);
			for (int i = 0; i < 8; i++) s.Noise(0.6 + i * 0.3, 0.22, 0.3, 5000, 1500, 0.03, 0.15, highPass: 1200);
			s.Tone(0, 0.6, 500, 1000, 0.12, Sfx.Wave.Sine, 0.1, 0.3, vibHz: 8, vibDepth: 0.05);
			k.Sound(178, s);
		}

		// ================================================================ 179 levitating book
		// He concentrates, a book rises from behind his head, flaps its pages, then drops on his head. Bonk!
		string[] bookShut = { "BBBBB", "BnnnB", "BBBBB" }, bookOpen = { "Bn.nB", ".BnB.", "..B.." };
		void Book(Frame f, int y, bool open, bool front)
		{
			var rows = open ? bookOpen : bookShut;
			for (int j = 0; j < 3; j++) for (int i = 0; i < 5; i++)
			{
				char c = rows[j][i]; if (c == '.') continue;
				if (front) f.Dot(6 + i, y + j, c); else Behind(f, 6 + i, y + j, c);
			}
		}
		Frame Sparkly(Frame f, int t) { Twinkle(f, 2, 1 + t % 2, t); Twinkle(f, 13, 2 - t % 2, t + 1); return f; }
		Frame Think(int bookY, int t) { var f = G(0, t % 2, 5, 4, 1, 1); Book(f, bookY, false, false); return Sparkly(f, t); }
		Frame UpBook(int y, bool open, int t) { var f = Up(0, t % 2, open ? 3 : 1); Book(f, y, open, false); return Sparkly(f, t); }
		Frame Bonk(int eyes, int mouth, int t, bool stars) { var f = G(0, t % 2, eyes, mouth); Book(f, stars ? -1 : -2, false, true); if (stars) f.Dot(2, -2, Glow).Dot(13, -3, Glow).Dot(1, 0, GlowLight).Dot(14, -1, GlowLight); return f; }
		int lvC0 = k.F(Think(9, 0)), lvC1 = k.F(Think(9, 1)), lv1 = k.F(Think(-1, 0)), lv2 = k.F(Think(-2, 1));
		int lv3 = k.F(UpBook(-3, false, 0)), lv4 = k.F(UpBook(-4, false, 1)), lvO1 = k.F(UpBook(-4, true, 0)), lvO2 = k.F(UpBook(-4, false, 1));
		var fall = G(0, 0, 4, 1); Book(fall, -3, false, true); int lvFall = k.F(fall);
		int lvBonk = k.F(Bonk(5, 1, 1, true)), lvGig = k.F(Bonk(1, 3, 0, false)), lvGig2 = k.F(Bonk(1, 0, 1, false));
		var poofBook = G(0, 0, 1, 0); Twinkle(poofBook, 6, -2, 1); Twinkle(poofBook, 10, -3, 0); Twinkle(poofBook, 9, -1, 1); int lvPoof = k.F(poofBook);
		k.Show(179, "levitate book", 140, (k.Stand, 1), (lvC0, 1), (lvC1, 1), (lv1, 1), (lv2, 1), (lv3, 1), (lv4, 1), (lv3, 1), (lv4, 1),
			(lvO1, 1), (lvO2, 1), (lvO1, 1), (lvO2, 1), (lvFall, 1), (lvBonk, 3), (lvGig, 1), (lvGig2, 1), (lvGig, 1), (lvPoof, 1), (k.Happy, 2));
		k.Sound(179, new Sfx(2.8, 2305).Tone(0.1, 1.5, 500, 1100, 0.15, Sfx.Wave.Sine, 0.3, 0.3, vibHz: 9, vibDepth: 0.06)
			.Noise(1.4, 0.15, 0.2, 3000, 1500, 0.005, 0.1).Thud(1.82, 0.7, 260, 90, 0.18).Bell(1.9, 1568, 0.12, 0.25));

		// ================================================================ 180 crystal ball
		// Boo gazes into a crystal ball; the mist swirls and a heart appears in it.
		(int x, int y)[] swirl = { (9, 9), (9, 11), (7, 11), (7, 9) };
		Frame Crystal(int t, int mode)   // mode 0 swirling mist, 1 the heart appears, 2 happy with a heart floating up
		{
			var f = mode == 0 ? Down(1, t % 2) : G(1, t % 2, mode == 1 ? 4 : 1, mode == 1 ? 1 : 0);
			FillCircle(f, 8, 10, 3.2f, Purple, Purple); FillCircle(f, 8, 10, 2.4f, PurpleLight, GlowLight);
			f.Rect(5, 13, 6, 1, Cone).Rect(6, 14, 4, 1, Cone);
			if (mode == 0)
			{
				f.Dot(swirl[t % 4].x, swirl[t % 4].y, Purple).Dot(swirl[(t + 2) % 4].x, swirl[(t + 2) % 4].y, GlowCyan);
				f.Dot(8, 10, t % 2 == 0 ? Purple : GlowCyan);
			}
			else
			{
				HeartAt(f, 7, 9);
				f.Dot(4, 9 + t % 2, GlowLight).Dot(12, 10 - t % 2, GlowLight);
				if (mode == 2) HeartAt(f, 12, -3 + t % 2);
			}
			return f;
		}
		var cb = new int[4]; for (int i = 0; i < 4; i++) cb[i] = k.F(Crystal(i, 0));
		int cbR0 = k.F(Crystal(0, 1)), cbR1 = k.F(Crystal(1, 1)), cbH0 = k.F(Crystal(0, 2)), cbH1 = k.F(Crystal(1, 2));
		k.Show(180, "crystal ball", 160, (k.Stand, 1), (cb[0], 1), (cb[1], 1), (cb[2], 1), (cb[3], 1), (cb[0], 1), (cb[1], 1), (cb[2], 1), (cb[3], 1),
			(cbR0, 1), (cbR1, 1), (cbR0, 1), (cbH0, 1), (cbH1, 1), (cbH0, 1), (k.Happy, 1));
		k.Sound(180, new Sfx(2.6, 2306).Bell(0.15, 660, 0.15, 0.5).Bell(0.5, 990, 0.12, 0.5).Bell(0.85, 830, 0.12, 0.5)
			.Bell(1.45, 1320, 0.2, 0.6).Bell(1.5, 1660, 0.15, 0.6).Tone(0.1, 1.3, 180, 200, 0.1, Sfx.Wave.Sine, 0.4, 0.4, amHz: 3, amDepth: 0.5));

		// ================================================================ 181 shooting star wish
		// A shooting star crosses the sky above him (a child window); he closes his eyes and makes a wish.
		Frame Wish(int t)
		{
			var f = G(1, t % 2, 1, 0, 1, 1);   // eyes closed, both little hands raised to the sky
			(int x, int y)[] sp = { (2, -2), (13, -1), (14, 3), (1, 2) };
			Twinkle(f, sp[t % 4].x, sp[t % 4].y, 1); Twinkle(f, sp[(t + 2) % 4].x, sp[(t + 2) % 4].y, 0);
			return f;
		}
		Frame Shooting(int t)
		{
			var f = new Frame();
			Star(f, 3, 7);
			f.Dot(3, 7, GlowLight);
			(int x, int y)[] trail = { (5, 6), (6, 6), (7, 5), (8, 5), (9, 5), (10, 4), (11, 4), (13, 3), (15, 2) };
			for (int i = 0; i < trail.Length; i++)
				if (i < 3) f.Dot(trail[i].x, trail[i].y, GlowLight);
				else if ((i + t) % 3 != 0) f.Dot(trail[i].x, trail[i].y, i % 2 == 0 ? Zz : GlowLight);
			return f;
		}
		var wish = new int[4]; for (int i = 0; i < 4; i++) wish[i] = k.F(Wish(i));
		int upR = k.F(Up(1, 0, 0, 0, 0, 1)), upC = k.F(Up(1, 1, 1)), upL = k.F(Up(1, 0, 1, 0, 0, -1));
		int shoot0 = k.F(Shooting(0)), shoot1 = k.F(Shooting(1));
		k.Show(181, "shooting star", 130, (k.Stand, 1), (upR, 2), (upC, 2), (upL, 2), (k.Surprise, 2),
			(wish[0], 1), (wish[1], 1), (wish[2], 1), (wish[3], 1), (wish[0], 1), (wish[1], 1), (wish[2], 1), (wish[3], 1),
			(k.Heart1, 2), (k.Heart2, 2), (k.Happy, 1));
		k.Child(181, 526, "imageX+imageW*1.2", "imageY-imageH*1.3");
		p.A(526, "shooting star").Move("-9", "2").Speed("60").Fade(1, 0.3).Seq(Repeat(6, shoot0, shoot1));
		k.Sound(181, new Sfx(2.8, 2307).Tone(0.15, 0.7, 2400, 900, 0.12, Sfx.Wave.Sine, 0.02, 0.4)
			.Notes(1.3, 0.1, 0.12, Sfx.Wave.Triangle, 84, 88, 91, 96).Bell(1.75, 1568, 0.12, 0.6));

		// ================================================================ 182 moon song
		// The moon rises behind him (a child window) and Boo sings a soft ghostly "ooOOoo" to it.
		Frame MoonF(int t)
		{
			var f = new Frame();
			FillCircle(f, 8, 7, 4.6f, Glow, Glow);
			for (int y = -4; y <= 15; y++) for (int x = 0; x < 16; x++)
			{
				float dx = x + 0.5f - 10.4f, dy = y + 0.5f - 5.4f;
				if (dx * dx + dy * dy < 4.1f * 4.1f) f[x, y] = '.';
			}
			var src = f.Clone();
			for (int y = -4; y <= 15; y++) for (int x = 0; x < 16; x++) if (src[x, y] == Glow && src[x - 1, y] == '.') f[x, y] = GlowLight;
			f.Dot(5, 9, Cone).Dot(6, 11, Cone);
			Twinkle(f, 2, 0, t); Twinkle(f, 14, 12, t + 1); f.Dot(13, 0, t % 2 == 0 ? Zz : GlowLight);
			return f;
		}
		Frame Sing(int t)
		{
			var f = G(1 + t % 2, t % 2, 1, t % 4 < 2 ? 1 : 2);
			Lean(f, t % 4 < 2 ? -1 : 1, 10);
			if (t % 4 < 2) Note(f, 12, -4 + t % 2); else Note(f, 0, -3 + t % 2);
			return f;
		}
		var sing = new int[4]; for (int i = 0; i < 4; i++) sing[i] = k.F(Sing(i));
		int moon0 = k.F(MoonF(0)), moon1 = k.F(MoonF(1));
		int upBack = k.F(Up(1, 1, 1, 0, 0, 1));
		k.Show(182, "moon song", 170, (k.Stand, 1), (upBack, 3), (sing[0], 1), (sing[1], 1), (sing[2], 1), (sing[3], 1), (sing[0], 1), (sing[1], 1), (sing[2], 1), (sing[3], 1),
			(sing[0], 1), (sing[1], 1), (sing[2], 1), (sing[3], 1), (sing[0], 1), (sing[1], 1), (sing[2], 1), (sing[3], 1), (k.Happy, 2), (k.Stand, 2));
		k.Child(182, 527, "imageX+imageW*0.35", "imageY-imageH*0.6");
		p.A(527, "moon rises").Move("0", "-2").Speed("170").Fade(0, 1).Seq(Repeat(5, moon0, moon1)).Then(100, 528);
		p.A(528, "moon shines").Speed("170").Seq(Repeat(4, moon0, moon1)).Then(100, 529);
		p.A(529, "moon sets").Move("0", "1").Speed("170").Fade(1, 0).Seq(Repeat(3, moon0, moon1));
		k.Sound(182, new Sfx(3.6, 2308).Tone(0.6, 1.1, 300, 450, 0.3, Sfx.Wave.Sine, 0.3, 0.3, vibHz: 5, vibDepth: 0.04, harmonic: 0.2)
			.Tone(1.6, 1.4, 450, 280, 0.3, Sfx.Wave.Sine, 0.2, 0.6, vibHz: 5.5, vibDepth: 0.05, harmonic: 0.2).Soften(2200));

		// ================================================================ 183 firefly
		// A firefly flies into his cupped hands (child window); he holds its light, then lets it fly away (another child).
		void Fly(Frame f, int x, int y, int t)
		{
			f.Dot(x, y, Ink).Dot(x + 1, y - 1 + t % 2, Zz);
			f.Dot(x + 1, y + 1, t % 2 == 0 ? Glow : GlowLight);
			if (t % 2 == 0) f.Dot(x + 2, y + 1, GlowLight).Dot(x + 1, y + 2, GlowLight);
		}
		Frame Cup(int t, int eyes, int mode)   // mode 0 closed with light inside, 1 open with the firefly, 2 open and empty
		{
			var f = G(1, t % 2, eyes, eyes == 1 ? 0 : 1);
			f.Dot(5, 8, Line).Dot(5, 9, Line).Dot(6, 10, Line).Rect(7, 10, 2, 1, Line).Dot(9, 10, Line).Dot(10, 9, Line).Dot(10, 8, Line);
			if (mode == 0)
			{
				f.Dot(6, 7, Line).Dot(9, 7, Line).Dot(7, 7, Line);
				f.Dot(8, 7, t % 2 == 0 ? Glow : GlowLight).Dot(7, 9, t % 2 == 0 ? Glow : GlowLight).Dot(8, 9, Glow);
				if (t % 2 == 0) f.Dot(8, 6, GlowLight).Dot(4, 9, GlowLight).Dot(11, 8, GlowLight);
			}
			else if (mode == 1) Fly(f, 7, 8, t);
			return f;
		}
		var cupF = new int[4]; for (int i = 0; i < 4; i++) cupF[i] = k.F(Cup(i, 1, 0));
		int cupOpen0 = k.F(Cup(0, 4, 1)), cupOpen1 = k.F(Cup(1, 4, 1)), cupEmpty = k.F(Cup(0, 4, 2));
		Frame FlyAlone(int x, int y, int t) { var f = new Frame(); Fly(f, x, y, t); return f; }
		int ffIn0 = k.F(FlyAlone(12, 12, 0)), ffIn1 = k.F(FlyAlone(12, 12, 1)), ffOut0 = k.F(FlyAlone(7, 8, 0)), ffOut1 = k.F(FlyAlone(7, 8, 1));
		int upF0 = k.F(Up(1, 0, 1)), upF1 = k.F(Up(1, 1, 0));
		k.Lead(183, "firefly", 100, 504, (k.Stand, 2), (k.LookL, 6), (cupOpen0, 1), (cupOpen1, 1),
			(cupF[0], 2), (cupF[1], 2), (cupF[2], 2), (cupF[3], 2), (cupF[0], 2), (cupF[1], 2), (cupF[2], 2));
		k.Show(504, "firefly release", 120, (cupEmpty, 2), (upF0, 2), (upF1, 2), (upF0, 2), (upF1, 2), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Happy, 2));
		k.Child(183, 530, "imageX-imageW*1.2", "imageY-imageH*0.5");
		p.A(530, "firefly comes").Move("4", "2").Speed("100").Seq(Repeat(5, ffIn0, ffIn1));
		k.Child(504, 531, "imageX", "imageY");
		p.A(531, "firefly away").Move("-1", "-3").Speed("120").Fade(1, 0).Seq(Repeat(8, ffOut0, ffOut1));
		k.Sound(183, new Sfx(2.4, 2309).Notes(0.1, 0.18, 0.08, Sfx.Wave.Sine, 96, 98, 96, 100, 96).Bell(0.95, 2093, 0.1, 0.4).Bell(1.4, 2637, 0.06, 0.4));
		k.Sound(504, new Sfx(1.6, 2310).Notes(0.1, 0.12, 0.1, Sfx.Wave.Triangle, 88, 91, 95, 100).Tone(0.1, 1.0, 1500, 3000, 0.04, Sfx.Wave.Sine, 0.2, 0.5));

		// ================================================================ 184 paper lantern
		// A paper lantern floats over his head; he blows on it, it lights up, and he lets it rise into the night sky.
		void Lantern(Frame f, int x, int y, bool lit, int t)
		{
			char c = lit ? (t % 2 == 0 ? Glow : GlowLight) : RedLight;
			Put(f, x, y, ".KK.", "R" + c + c + "R", "R" + c + c + "R", ".KK.");
			Behind(f, x + 2, y + 4, lit ? Flame : Red);
			if (lit && t % 2 == 0) { Behind(f, x - 1, y + 1, GlowLight); Behind(f, x + 4, y + 2, GlowLight); }
		}
		Frame Lant(int mode, int t)   // 0 appears with sparkles, 1 unlit, 2 blowing, 3 lit
		{
			var f = mode == 2 ? Up(0, t % 2, 1) : mode == 3 ? G(0, t % 2, 1, 0, 1, 1) : Up(0, t % 2, 0, 1, 1);
			Lantern(f, 6, -4, mode == 3, t);
			if (mode == 0) { Twinkle(f, 4, -3, 1); Twinkle(f, 11, -2, 0); }
			if (mode == 2) f.Dot(7, -1 + t % 2, Zz).Dot(8, 0, Zz);
			return f;
		}
		Frame LanternAlone(int t) { var f = new Frame(); Lantern(f, 6 + (t / 2) % 2, -4, true, t); return f; }
		int ln0 = k.F(Lant(0, 1)), ln1a = k.F(Lant(1, 0)), ln1b = k.F(Lant(1, 1)), ln2a = k.F(Lant(2, 0)), ln2b = k.F(Lant(2, 1)), ln3a = k.F(Lant(3, 0)), ln3b = k.F(Lant(3, 1));
		var lnc = new int[4]; for (int i = 0; i < 4; i++) lnc[i] = k.F(LanternAlone(i));
		int lnUp0 = k.F(Up(0, 0, 0)), lnUp1 = k.F(Up(0, 1, 1));
		k.Lead(184, "paper lantern", 150, 505, (k.Stand, 1), (ln0, 1), (ln1a, 1), (ln1b, 1), (ln1a, 1), (ln2a, 1), (ln2b, 1),
			(ln3a, 1), (ln3b, 1), (ln3a, 1), (ln3b, 1), (ln3a, 1), (ln3b, 1));
		k.Show(505, "lantern release", 150, (lnUp0, 2), (lnUp1, 2), (lnUp0, 2), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Happy, 2), (k.Stand, 1));
		k.Child(505, 532, "imageX", "imageY");
		p.A(532, "lantern rises").Move("0", "-2").Speed("150").Fade(1, 0).Seq(Repeat(5, lnc));
		k.Sound(184, new Sfx(2.2, 2311).Noise(0.75, 0.3, 0.25, 1500, 600, 0.05, 0.2).Tone(1.0, 0.6, 180, 300, 0.2, Sfx.Wave.Sine, 0.02, 0.4)
			.Bell(1.05, 1047, 0.12, 0.6).Bell(1.2, 1319, 0.08, 0.6));

		// ================================================================ 185 will-o'-the-wisp
		// A little blue flame with eyes appears ahead; Boo follows it, mesmerized, until it blinks out.
		Frame Wisp(int t)
		{
			var f = new Frame();
			int b = t % 2, sx = (t / 2) % 2, y0 = 6 + b;
			f.Dot(8 - sx, y0, GlowLight);
			f.Rect(7, y0 + 1, 2, 1, GlowCyan);
			f.Rect(6, y0 + 2, 4, 1, GlowCyan).Rect(7, y0 + 2, 2, 1, GlowLight);
			f.Rect(5, y0 + 3, 6, 2, GlowCyan).Rect(7, y0 + 4, 2, 1, GlowLight);
			f.Dot(6, y0 + 3, Ink).Dot(9, y0 + 3, Ink);
			f.Rect(6, y0 + 5, 4, 1, GlowDeep).Rect(7, y0 + 6, 2, 1, GlowDeep);
			f.Dot(11 + sx, y0 + 5 - b, GlowCyan).Dot(12, y0 + 7, b == 0 ? GlowDeep : GlowCyan);
			return f;
		}
		var wisp = new int[4]; for (int i = 0; i < 4; i++) wisp[i] = k.F(Wisp(i));
		Frame Mesmer(int t)
		{
			int[] lifts = { 1, 2, 1, 0 };
			var f = G(lifts[t], t % 2, 4, 1, 1, 0);
			Lean(f, -1, 10);
			return f;
		}
		var mes = new int[4]; for (int i = 0; i < 4; i++) mes[i] = k.F(Mesmer(i));
		int puzzled = k.F(G(1, 1, 3, 1));
		k.Lead(185, "will-o-wisp", 120, 506, (k.Stand, 1), (k.LookL, 2), (k.Surprise, 2), (mes[0], 1), (mes[1], 1), (mes[2], 1));
		p.A(506, "follow wisp").Move("-3", "0").Speed("120").Seq(Repeat(5, mes)).Then(100, 507).OnBorder(100, 507).OnGravity(100, 5);
		k.Show(507, "wisp gone", 150, (k.LookL, 2), (k.LookR, 2), (puzzled, 2), (k.Blink, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Stand, 1));
		k.Child(185, 533, "imageX-imageW*1.0", "imageY-imageH*0.15");
		p.A(533, "wisp appears").Speed("120").Fade(0, 1).Seq(Repeat(2, wisp)).Then(100, 534);
		p.A(534, "wisp leads").Move("-3", "0").Speed("120").Seq(Repeat(5, wisp)).Then(100, 535);
		p.A(535, "wisp fades").Speed("120").Fade(1, 0).Seq(wisp[0], wisp[1], wisp[2], wisp[3], wisp[0], wisp[1]);
		k.Sound(185, new Sfx(3.2, 2312).Tone(0.2, 2.6, 520, 560, 0.15, Sfx.Wave.Sine, 0.6, 0.8, vibHz: 4, vibDepth: 0.06)
			.Tone(0.4, 2.4, 780, 740, 0.08, Sfx.Wave.Triangle, 0.6, 0.8, vibHz: 3, vibDepth: 0.05, amHz: 2, amDepth: 0.6));

		// ================================================================ 186 ectoplasm bubbles
		// Boo gets bubbly: little green ectoplasm bubbles float out of his head and pop.
		(int x, int y, int s)[][] bub =        // s: 0 small, 1 ring, 2 pop
		{
			new[] { (6, -1, 0) },
			new[] { (6, -2, 0), (10, -1, 0) },
			new[] { (5, -4, 1), (10, -2, 0) },
			new[] { (6, -3, 2), (10, -3, 0), (3, -1, 0) },
			new[] { (9, -4, 1), (3, -2, 0) },
			new[] { (10, -3, 2), (2, -4, 1), (7, -1, 0) },
			new[] { (7, -2, 0), (12, -1, 0) },
			new[] { (3, -3, 2), (7, -3, 0), (12, -2, 0) },
		};
		Frame Ecto(int t)
		{
			int[] eyes = { 0, 4, 1, 1, 0, 4, 1, 1 }, mouth = { 1, 1, 3, 3, 1, 1, 3, 0 };
			var f = G(1, t % 2, eyes[t], mouth[t]);
			Recolor(f, Shade, Mint);
			Behind(f, 4, 14 - t % 2, Mint);
			foreach (var (x, y, s) in bub[t])
			{
				if (s == 0) f.Dot(x, y, Mint).Dot(x + 1, y, GlowCyan);
				else if (s == 1) f.Dot(x + 1, y, Mint).Dot(x, y + 1, Mint).Dot(x + 2, y + 1, Mint).Dot(x + 1, y + 2, Mint).Dot(x + 1, y + 1, GlowLight);
				else f.Dot(x - 1, y - 1, GlowLight).Dot(x + 1, y - 1, Mint).Dot(x - 1, y + 1, Mint).Dot(x + 1, y + 1, GlowLight);
			}
			return f;
		}
		var ecto = new int[8]; for (int i = 0; i < 8; i++) ecto[i] = k.F(Ecto(i));
		k.Show(186, "ectoplasm bubbles", 130, (k.Stand, 1),
			(ecto[0], 1), (ecto[1], 1), (ecto[2], 1), (ecto[3], 1), (ecto[4], 1), (ecto[5], 1), (ecto[6], 1), (ecto[7], 1),
			(ecto[0], 1), (ecto[1], 1), (ecto[2], 1), (ecto[3], 1), (ecto[4], 1), (ecto[5], 1), (ecto[6], 1), (ecto[7], 1),
			(k.Happy, 1), (k.Giggle1, 1), (k.Giggle2, 1));
		{
			var s = new Sfx(2.6, 2313);
			foreach (int fi in new[] { 4, 6, 8, 12, 14, 16 }) s.Pop(fi * 0.13, 0.35, 380 + fi * 25);
			s.Tone(0.1, 2.0, 120, 140, 0.1, Sfx.Wave.Sine, 0.2, 0.3, amHz: 7, amDepth: 0.8);
			k.Sound(186, s);
		}

		// ================================================================ 187 three mini Boos
		// He splits into three mini Boos that dance in a row, then they merge back together.
		Frame Three(int x0, int x1, int x2, int y0, int y1, int y2, bool sparkle)
		{
			var f = new Frame();
			int[] xs = { x0, x1, x2 }, ys = { y0, y1, y2 };
			for (int i = 0; i < 3; i++) { f.Rect(xs[i] + 1, 15, 2, 1, Shadow); Mini(f, xs[i], 11 + ys[i]); }
			if (sparkle) { Twinkle(f, 3, 6, 1); Twinkle(f, 12, 5, 1); Twinkle(f, 8, 3, 0); }
			return f;
		}
		var burst = Fade(G(1, 0, 1, 0), 2); Twinkle(burst, 2, 2, 1); Twinkle(burst, 13, 1, 1); Twinkle(burst, 8, -2, 0);
		int thBurst = k.F(burst);
		int th0 = k.F(Three(1, 6, 11, 0, 0, 0, true)), thA = k.F(Three(1, 6, 11, -2, 0, -2, false)), thN = k.F(Three(1, 6, 11, 0, 0, 0, false));
		int thB = k.F(Three(1, 6, 11, 0, -2, 0, false)), thC = k.F(Three(1, 6, 11, -1, -1, -1, false));
		int thM1 = k.F(Three(3, 6, 9, 0, -1, 0, false)), thM2 = k.F(Three(5, 6, 7, 0, 0, 0, true));
		var flash = G(1, 0, 1, 0); Star(flash, 2, 2); Star(flash, 13, 3); int thFlash = k.F(Fade(flash, 1));
		k.Show(187, "three mini boos", 120, (k.Stand, 1), (k.Fade1, 1), (thBurst, 2), (th0, 1),
			(thA, 1), (thN, 1), (thB, 1), (thN, 1), (thA, 1), (thN, 1), (thB, 1), (thN, 1), (thC, 1), (thN, 1), (thC, 1), (thN, 1),
			(thM1, 1), (thM2, 1), (thFlash, 1), (k.Fade1, 1), (k.Happy, 2), (k.Giggle1, 1), (k.Giggle2, 1));
		k.Sound(187, new Sfx(2.8, 2314).Pop(0.3, 0.3, 600).Pop(0.45, 0.3, 800).Pop(0.6, 0.3, 1000)
			.Notes(0.7, 0.24, 0.08, Sfx.Wave.Square, 72, 76, 79, 76, 72, 76, 79).Bell(2.2, 1319, 0.15, 0.5).Soften(3500));

		// ================================================================ 188 teleport
		// A ring of sparkles, he dissolves, jumps a long way ahead in a blink and sparkles back into view.
		(int x, int y)[] tpPts = { (1, 0), (14, 1), (0, 9), (15, 7), (7, -3), (3, 13), (12, 13), (10, -2) };
		Frame Tele(int stage, int t)
		{
			Frame f;
			if (stage < 4) f = Fade(G(1, t % 2, 5, 4, 1, 1), stage);
			else { f = new Frame(); f.Rect(6, 15, 4, 1, Shadow); }
			for (int i = 0; i < tpPts.Length; i++) if ((i + t) % 3 != 0) Twinkle(f, tpPts[i].x, tpPts[i].y, i + t);
			return f;
		}
		int tp0 = k.F(Tele(0, 0)), tp1 = k.F(Tele(1, 1)), tp2 = k.F(Tele(2, 2)), tp3 = k.F(Tele(3, 3)), tp4 = k.F(Tele(4, 4)), tp5 = k.F(Tele(4, 5));
		Frame Glitter(int t)
		{
			var f = new Frame();
			for (int i = 0; i < tpPts.Length; i++)
			{
				int y = tpPts[i].y + 2 + t * 2;
				if (y > 15) continue;
				f.Dot(tpPts[i].x, y, (i + t) % 2 == 0 ? (i % 3 == 0 ? PurpleLight : GlowLight) : Glow);
			}
			return f;
		}
		var gl = new int[6]; for (int i = 0; i < 6; i++) gl[i] = k.F(Glitter(i));
		k.Lead(188, "teleport", 100, 508, (k.Stand, 1), (tp0, 2), (tp1, 1), (tp2, 1), (tp3, 1), (tp4, 1), (tp5, 1));
		p.A(508, "teleport jump").Move("-30", "0").Speed("60").Fade(0, 0).Seq(blank, blank, blank).Then(100, 509).OnBorder(100, 509).OnGravity(100, 509);
		k.Show(509, "teleport arrive", 100, (tp5, 1), (tp4, 1), (tp3, 1), (tp2, 1), (tp1, 1), (k.Surprise, 2), (k.LookR, 3), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 1)).Fade(0.5, 1);
		k.Child(508, 536, "imageX", "imageY");
		p.A(536, "teleport sparkles").Speed("110").Fade(1, 0).Seq(gl);
		k.Sound(188, new Sfx(1.0, 2315).Tone(0.05, 0.6, 300, 2400, 0.15, Sfx.Wave.Square, 0.02, 0.1).Notes(0.1, 0.07, 0.08, Sfx.Wave.Sine, 84, 88, 91, 96, 100).Soften(4000));
		k.Sound(509, new Sfx(0.9, 2316).Tone(0, 0.5, 2400, 500, 0.12, Sfx.Wave.Square, 0.01, 0.2).Bell(0.45, 1047, 0.12, 0.4).Soften(4000));

		// ================================================================ 189 aurora
		// An aurora ripples across the sky above him (two child windows side by side); he looks up in awe.
		Frame Aurora(int shift, int t)
		{
			var f = new Frame();
			for (int x = 0; x < 16; x++)
			{
				int X = x + shift;
				if ((X + t + 40) % 7 == 0) continue;                                     // a darker gap between the curtains
				int top = 1 + (int)Math.Round(1.6 * Math.Sin(X * 0.55 + t * 1.05) + Math.Sin(X * 0.23 - t * 0.5));
				int len = 7 + (int)Math.Round(1.5 * Math.Sin(X * 0.4 + t * 1.05));
				for (int j = 0; j < len; j++)
				{
					int y = top + j;
					if (j == 0 && (X + t + 40) % 2 == 0) continue;
					if (j >= len - 2 && (X + y + 40) % 2 != 0) continue;
					char c = j < 2 ? PurpleLight : j < len - 2 ? ((X + y + t + 40) % 5 == 0 ? Mint : GlowCyan) : Mint;
					f[x, y] = c;
				}
			}
			return f;
		}
		var auL = new int[6]; var auR = new int[6];
		for (int i = 0; i < 6; i++) { auL[i] = k.F(Aurora(-8, i)); auR[i] = k.F(Aurora(8, i)); }
		Frame Awe(int t)
		{
			var f = Up(1, t % 2, t % 4 < 2 ? 1 : 0);
			Recolor(f, Shade, t % 2 == 0 ? GlowCyan : Mint);
			return f;
		}
		var awe = new int[4]; for (int i = 0; i < 4; i++) awe[i] = k.F(Awe(i));
		k.Show(189, "aurora", 150, (k.Stand, 1), (upC, 3), (awe[0], 1), (awe[1], 1), (awe[2], 1), (awe[3], 1), (awe[0], 1), (awe[1], 1), (awe[2], 1), (awe[3], 1),
			(awe[0], 1), (awe[1], 1), (awe[2], 1), (awe[3], 1), (awe[0], 1), (awe[1], 1), (awe[2], 1), (awe[3], 1),
			(k.Heart1, 2), (k.Heart2, 2), (k.Heart1, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(189, 537, "imageX-imageW*0.5", "imageY-imageH*0.95");
		k.Child(189, 540, "imageX+imageW*0.5", "imageY-imageH*0.95");
		p.A(537, "aurora left in").Speed("150").Fade(0, 1).Seq(auL).Then(100, 538);
		p.A(538, "aurora left").Speed("150").Seq(Repeat(3, auL)).Then(100, 539);
		p.A(539, "aurora left out").Speed("150").Fade(1, 0).Seq(auL);
		p.A(540, "aurora right in").Speed("150").Fade(0, 1).Seq(auR).Then(100, 541);
		p.A(541, "aurora right").Speed("150").Seq(Repeat(3, auR)).Then(100, 542);
		p.A(542, "aurora right out").Speed("150").Fade(1, 0).Seq(auR);
		k.Sound(189, new Sfx(4.6, 2317).Tone(0, 4.3, Sfx.Midi(57), Sfx.Midi(57), 0.12, Sfx.Wave.Sine, 1.2, 1.5, amHz: 0.5, amDepth: 0.4)
			.Tone(0.3, 4.0, Sfx.Midi(64), Sfx.Midi(64), 0.1, Sfx.Wave.Sine, 1.2, 1.5, amHz: 0.7, amDepth: 0.4)
			.Tone(0.6, 3.7, Sfx.Midi(69), Sfx.Midi(71), 0.08, Sfx.Wave.Triangle, 1.2, 1.5, vibHz: 4, vibDepth: 0.01, amHz: 0.9, amDepth: 0.5).Soften(2500));

		// ================================================================ 190 bat
		// Poof! Boo turns into a little white ghost bat, flutters up in an arc and poofs back.
		Frame Bat(bool up)
		{
			var f = new Frame();
			FillCircle(f, 8, 8, 3.3f, Sheet, Sheet);
			f.Dot(6, 3, Sheet).Dot(6, 4, Sheet).Dot(9, 3, Sheet).Dot(9, 4, Sheet);   // ears
			OutlineAll(f);
			int[][] spans = up
				? new[] { new[] { 2, 0, 1 }, new[] { 3, 0, 2 }, new[] { 4, 1, 3 }, new[] { 5, 1, 4 }, new[] { 6, 2, 4 }, new[] { 7, 3, 4 } }
				: new[] { new[] { 8, 3, 4 }, new[] { 9, 2, 4 }, new[] { 10, 1, 4 }, new[] { 11, 0, 3 }, new[] { 12, 0, 2 }, new[] { 13, 0, 1 } };
			foreach (var s in spans)
				for (int x = s[1]; x <= s[2]; x++)
				{
					char c = x == s[1] || s[0] == (up ? 2 : 13) ? Line : Shade;
					f[x, s[0]] = c; f[15 - x, s[0]] = c;
				}
			f.Rect(6, 7, 1, 2, Ink).Rect(9, 7, 1, 2, Ink).Rect(7, 10, 2, 1, Mouth).Dot(5, 9, Blush).Dot(10, 9, Blush);
			return f;
		}
		Frame Puff(int s)
		{
			var f = s == 0 ? Fade(G(1, 0, 5, 1, 1, 1), 2) : new Frame();
			(float x, float y, float r)[] blobs = { (5, 6, 2.6f), (10.5f, 5, 3f), (8, 9, 3.4f), (4, 10.5f, 2.2f), (12, 10.5f, 2.4f) };
			if (s == 1)
			{
				foreach (var b in blobs) FillCircle(f, b.x, b.y, b.r, Sheet, Sheet);
				OutlineAll(f);
				f.Dot(6, 8, Shade).Dot(9, 6, Shade).Dot(10, 10, Shade).Dot(5, 11, Shade);
			}
			else
			{
				var c = new Frame();
				foreach (var b in blobs) FillCircle(c, b.x, b.y, b.r + 0.6f, Shade, Shade);
				for (int y = -4; y < 15; y++) for (int x = 0; x < 16; x++) if (c[x, y] != '.' && (x + y) % 2 == 0) Behind(f, x, y, Shade);
			}
			f.Rect(5, 15, 6, 1, Shadow);
			return f;
		}
		int batUp = k.F(Bat(true)), batDn = k.F(Bat(false)), puff0 = k.F(Puff(0)), puff1 = k.F(Puff(1));
		int squeeze = k.F(G(1, 0, 5, 4));
		k.Lead(190, "bat", 100, 510, (k.Stand, 1), (squeeze, 2), (puff0, 1), (puff1, 2));
		p.A(510, "bat flutter").Move("-4", "-4", "-4", "4").Speed("80").Seq(Repeat(8, batUp, batDn))
			.Then(100, 511).OnBorder(100, 511);
		k.Show(511, "bat poof back", 100, (puff1, 2), (puff0, 1), (k.Surprise, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Happy, 2));
		k.Sound(190, new Sfx(0.7, 2318).Noise(0.3, 0.35, 0.4, 2500, 400, 0.005, 0.3).Pop(0.3, 0.3, 500));
		{
			var s = new Sfx(1.4, 2319);
			for (int i = 0; i < 8; i++) s.Noise(i * 0.16, 0.08, 0.25, 1800, 700, 0.005, 0.06);
			s.Tone(0, 0.2, 2200, 2600, 0.05, Sfx.Wave.Sine, 0.01, 0.1);
			k.Sound(510, s);
			k.Sound(511, new Sfx(0.7, 2320).Noise(0, 0.35, 0.4, 2500, 400, 0.005, 0.3).Pop(0.05, 0.3, 700));
		}

		// ================================================================ 191 constellation
		// Boo points at the sky and joins the stars into a heart-shaped constellation (child window above him).
		(int x, int y)[] cst = { (4, 3), (6, 1), (8, 3), (10, 1), (12, 3), (11, 6), (8, 9), (5, 6) };
		Frame Constellation(int stars, int links, int t)
		{
			var f = new Frame();
			for (int i = 0; i < links; i++) { var a = cst[i]; var b = cst[(i + 1) % cst.Length]; Ln(f, a.x, a.y, b.x, b.y, Zz); }
			for (int i = 0; i < stars; i++)
				if (t >= 0 && (i + t) % 2 == 0) Star(f, cst[i].x, cst[i].y);
				else f.Dot(cst[i].x, cst[i].y, i == stars - 1 && t < 0 ? GlowLight : Glow);
			return f;
		}
		var csA = new List<int>();
		for (int n = 1; n <= 8; n++) csA.Add(k.F(Constellation(n, 0, -1)));
		csA.Add(k.F(Constellation(8, 3, -1))); csA.Add(k.F(Constellation(8, 6, -1))); csA.Add(k.F(Constellation(8, 8, -1)));
		int csT0 = k.F(Constellation(8, 8, 0)), csT1 = k.F(Constellation(8, 8, 1));
		csA.AddRange(Repeat(3, csT0, csT1));
		Frame Point(int t) { var f = Up(1, t % 2, 0, 1, 0, -1); Twinkle(f, 0, 3 - t % 2, t); return f; }
		int pt0 = k.F(Point(0)), pt1 = k.F(Point(1));
		k.Show(191, "constellation", 160, (k.Stand, 1), (pt0, 1), (pt1, 1), (pt0, 1), (pt1, 1), (pt0, 1), (pt1, 1), (pt0, 1), (pt1, 1),
			(upC, 3), (k.Heart1, 2), (k.Heart2, 2), (k.Heart1, 2), (k.Happy, 2), (k.Stand, 1));
		k.Child(191, 543, "imageX-imageW*0.25", "imageY-imageH*0.85");
		p.A(543, "constellation").Speed("160").Seq(csA.ToArray()).Then(100, 544);
		p.A(544, "constellation fades").Speed("160").Fade(1, 0).Seq(csT0, csT1, csT0, csT1);
		k.Sound(191, new Sfx(3.4, 2331).Notes(0.16, 0.16, 0.12, Sfx.Wave.Sine, 84, 86, 88, 91, 93, 96, 98, 100)
			.Bell(1.9, 1047, 0.12, 0.6).Bell(1.95, 1319, 0.1, 0.6).Bell(2.0, 1568, 0.1, 0.6));

		// ================================================================ 192 through the floor
		// Boo sinks through the floor, peeks out with just his eyes, then pops back up.
		Frame Sink(int d, int eyes, int mouth, int t)
		{
			var src = G(1, t % 2, eyes, mouth, 0, 0, false);
			var f = new Frame();
			for (int y = -4; y < 15; y++) for (int x = 0; x < 16; x++) if (src[x, y] != '.' && y + d < 14) f[x, y + d] = src[x, y];
			f.Rect(3, 15, 10, 1, Shadow).Dot(2 - t % 2, 14, Shade).Dot(13 + t % 2, 14, Shade);
			return f;
		}
		int sk2 = k.F(Sink(2, 4, 1, 0)), sk4 = k.F(Sink(4, 4, 1, 1)), sk7 = k.F(Sink(7, 1, 3, 0)), sk10 = k.F(Sink(10, 1, 3, 1)), sk13 = k.F(Sink(13, 1, 3, 0));
		int skG0 = k.F(Sink(20, 0, 0, 0)), skG1 = k.F(Sink(20, 0, 0, 1));
		int skPL = k.F(Sink(9, 2, 4, 0)), skPR = k.F(Sink(9, 3, 4, 1));
		int sk8 = k.F(Sink(8, 4, 1, 0)), skU4 = k.F(Sink(4, 4, 1, 1)), sk1 = k.F(Sink(1, 1, 3, 0));
		k.Show(192, "through the floor", 110, (k.Stand, 1), (k.Giggle1, 1), (sk2, 1), (sk4, 1), (sk7, 1), (sk10, 1), (sk13, 1),
			(skG0, 1), (skG1, 1), (skG0, 1), (skG1, 1), (skPL, 3), (skPR, 3), (sk13, 1), (skG0, 1), (skG1, 1),
			(sk8, 1), (skU4, 1), (sk1, 1), (k.Stand2, 1), (k.Happy, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Stand, 1));
		k.Sound(192, new Sfx(3.0, 2321).Tone(0.2, 0.6, 900, 200, 0.2, Sfx.Wave.Sine, 0.02, 0.1)
			.Tone(1.75, 0.4, 200, 1000, 0.2, Sfx.Wave.Sine, 0.02, 0.05).Pop(2.15, 0.3, 800));

		// ================================================================ 193 moon hammock
		// A crescent moon appears under him like a hammock; he rocks in it and naps for a while.
		Frame MoonBed(int tilt, int eyes, int mouth, int z, int t)
		{
			var f = G(1, t % 2, eyes, mouth, 0, 0, false);
			for (int x = 0; x < 16; x++)
			{
				int yb = 9 + (int)Math.Round(3 * Math.Sin(Math.PI * (x + 0.5) / 16)) + (int)Math.Round(tilt * (x - 7.5) / 7.5);
				for (int y = yb + 2; y < 15; y++) f[x, y] = '.';
				f[x, yb] = x < 2 || x > 13 ? GlowLight : Glow;
				f[x, yb + 1] = Glow;
				if (x >= 4 && x <= 11) f[x, yb + 2] = Cone;
			}
			f.Rect(5, 15, 6, 1, Shadow);
			if (z >= 1) f.Dot(13, -1, Zz).Dot(14, -2, Zz);
			if (z >= 2) Put(f, 11, -4, "zzz", ".z.", "zzz");
			return f;
		}
		var mApp = G(1, 0, 4, 1); Twinkle(mApp, 2, 12, 1); Twinkle(mApp, 13, 12, 1); Twinkle(mApp, 8, 15, 0); int mbApp = k.F(mApp);
		int mbIn = k.F(MoonBed(0, 1, 0, 0, 0));
		int mbA = k.F(MoonBed(-1, 6, 4, 1, 0)), mbB = k.F(MoonBed(0, 6, 4, 2, 1)), mbC = k.F(MoonBed(1, 6, 4, 1, 0)), mbD = k.F(MoonBed(0, 6, 4, 0, 1));
		int mbWake = k.F(MoonBed(0, 0, 0, 0, 0));
		var mGone = G(1, 0, 1, 0); Twinkle(mGone, 3, 12, 0); Twinkle(mGone, 12, 13, 1); int mbGone = k.F(mGone);
		k.Show(193, "moon hammock", 200, (k.Stand, 1), (mbApp, 1), (mbIn, 2), (mbA, 1), (mbB, 1), (mbC, 1), (mbD, 1), (mbA, 1), (mbB, 1), (mbC, 1), (mbD, 1),
			(mbA, 1), (mbB, 1), (mbC, 1), (mbD, 1), (mbWake, 2), (mbGone, 1), (k.Happy, 1), (k.Stand, 1));
		k.Sound(193, new Sfx(4.2, 2322).Notes(0.4, 0.4, 0.12, Sfx.Wave.Triangle, 76, 79, 84, 79, 76, 79, 83, 79).Soften(2000));

		// ================================================================ 194 magic hat
		// A top hat appears in front of him (child window); he waves his hand, a star pops out, then a tiny Boo waves hello.
		Frame Hat(int w, int miniY, int wave, bool star)
		{
			var f = new Frame();
			if (miniY < 99)
			{
				Mini(f, 6, miniY);
				if (wave == 1) f.Dot(10, miniY, Line); else if (wave == 2) f.Dot(10, miniY + 1, Line);
			}
			for (int y = 9; y <= 13; y++) for (int x = 5; x <= 10; x++) f[x + (y < 12 ? w : 0), y] = y == 12 ? Purple : Ink;
			f.Rect(6 + w, 9, 1, 3, Purple);
			f.Rect(3, 14, 10, 1, Ink).Dot(3, 14, Line).Dot(12, 14, Line);
			f.Rect(5, 15, 6, 1, Shadow);
			if (star) { Star(f, 8, 5); Twinkle(f, 4, 4, 1); Twinkle(f, 12, 3, 0); }
			return f;
		}
		int ht0 = k.F(Hat(0, 99, 0, false)), htL = k.F(Hat(-1, 99, 0, false)), htR = k.F(Hat(1, 99, 0, false)), htS = k.F(Hat(0, 99, 0, true));
		int htM8 = k.F(Hat(0, 8, 0, false)), htM6 = k.F(Hat(0, 6, 0, false)), htM5 = k.F(Hat(0, 5, 0, false)), htW1 = k.F(Hat(0, 5, 1, false)), htW2 = k.F(Hat(0, 5, 2, false));
		var hp = Hat(0, 99, 0, false); for (int y = 6; y < 15; y++) for (int x = 2; x < 14; x++) if ((x + y) % 3 == 0) hp.Dot(x, y, Shade);
		int htPoof = k.F(Fade(hp, 1));
		Frame Magic(int t) { var f = G(1, t % 2, 2, 0, t % 2 == 0 ? 1 : 0, 0); Twinkle(f, 0, t % 2 == 0 ? 3 : 7, t); return f; }
		int mg0 = k.F(Magic(0)), mg1 = k.F(Magic(1));
		k.Show(194, "magic hat", 150, (k.Stand, 1), (mg0, 1), (mg1, 1), (mg0, 1), (mg1, 1), (mg0, 1), (mg1, 1), (mg0, 1), (k.Surprise, 2),
			(k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1),
			(k.LookL, 2), (k.Happy, 3), (k.Stand, 2));
		k.Child(194, 545, "imageX-imageW*0.8", "imageY");
		p.A(545, "hat appears").Speed("150").Fade(0, 1).Seq(ht0, htL).Then(100, 546);
		p.A(546, "hat trick").Speed("150").Seq(htR, htL, htR, htL, htR, ht0, htS, htS, htM8, htM6, htM5, htW1, htW2, htW1, htW2, htM6, htM8, htL, htR).Then(100, 547);
		p.A(547, "hat poof").Speed("150").Fade(1, 0).Seq(ht0, htPoof, htPoof);
		{
			var s = new Sfx(3.0, 2323);
			s.Noise(0.1, 1.05, 0.25, 900, 1400, 0.1, 0.05, highPass: 150, amHz: 22, amDepth: 0.9);
			s.Notes(1.2, 0.14, 0.18, Sfx.Wave.Square, 79, 84).Bell(1.48, 1568, 0.1, 0.5);
			s.Pop(1.65, 0.2, 1200).Soften(3500);
			k.Sound(194, s);
		}

		// ================================================================ 195 ghost tornado
		// He spins faster and faster into a little white whirlwind, zooms ahead and stops, dizzy.
		Frame Twister(int t)
		{
			var f = new Frame();
			for (int y = 0; y < 15; y++)
			{
				int hw = 1 + (14 - y) * 6 / 14;
				int cx = 8 + (int)Math.Round(Math.Sin(y * 0.5 + t * 1.5));
				for (int x = cx - hw; x < cx + hw; x++) f[x, y] = (x - cx + y + t * 2 + 40) % 4 == 0 ? Shade : Sheet;
			}
			OutlineAll(f);
			f.Rect(4, 2, 2, 2, Ink).Rect(10, 2, 2, 2, Ink).Rect(7, 4, 2, 1, Mouth);
			f.Dot(3 + t % 2 * 9, 15, Shadow).Dot(5 + t % 2 * 5, 15, Shadow).Dot(1 + t % 2 * 13, 14, Shade);
			return f;
		}
		var tw = new int[4]; for (int i = 0; i < 4; i++) tw[i] = k.F(Twister(i));
		Frame Dizzy(int t)
		{
			var f = G(1, t % 2, 5, 1); Lean(f, t % 2 == 0 ? 1 : -1, 10);
			(int x, int y)[] ring = { (3, -3), (8, -4), (13, -3), (8, -2) };
			Twinkle(f, ring[t % 4].x, ring[t % 4].y, 1); f.Dot(ring[(t + 2) % 4].x, ring[(t + 2) % 4].y, Glow);
			return f;
		}
		var dz = new int[4]; for (int i = 0; i < 4; i++) dz[i] = k.F(Dizzy(i));
		k.Lead(195, "ghost tornado", 80, 512, (k.Stand, 1), (k.SideL, 1), (k.Back, 1), (k.SideR, 1), (k.SideL, 1), (k.Back, 1), (tw[0], 1), (tw[1], 1));
		p.A(512, "tornado").Move("-5", "0").Speed("70").Seq(Repeat(5, tw)).Then(100, 513).OnBorder(100, 513).OnGravity(100, 5);
		k.Show(513, "dizzy", 130, (dz[0], 1), (dz[1], 1), (dz[2], 1), (dz[3], 1), (dz[0], 1), (dz[1], 1), (dz[2], 1), (dz[3], 1), (k.Blink, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Stand, 1));
		k.Sound(195, new Sfx(2.2, 2324).Noise(0.2, 1.8, 0.4, 600, 2500, 0.4, 0.5, highPass: 200, amHz: 9, amDepth: 0.5)
			.Tone(0.2, 1.6, 300, 700, 0.08, Sfx.Wave.Sine, 0.4, 0.4));
		k.Sound(513, new Sfx(1.4, 2325).Tone(0, 1.1, 900, 700, 0.08, Sfx.Wave.Sine, 0.05, 0.3, vibHz: 6, vibDepth: 0.15));

		// ================================================================ 196 runaway shadow
		// His shadow sneaks off on its own (child window), makes a little bunny on the floor, and comes back.
		Frame Shad(int s)
		{
			var f = new Frame();
			switch (s)
			{
				case 0: f.Rect(5, 15, 6, 1, Shadow); break;
				case 1: f.Rect(4, 15, 6, 1, Shadow).Dot(4, 14, Shadow); break;
				// a little shadow bunny facing left: ears up, then a small hop with one ear folded
				case 2: Put(f, 4, 10, "..q.q....", "..q.q....", ".qqqq....", "qqqqqqqq.", ".qqqqqqqq", "..qqqqqq."); break;
				default: Put(f, 4, 9, "..qq.....", "....q....", ".qqqq....", "qqqqqqqq.", ".qqqqqqqq", "..qqqqqq."); f.Rect(6, 15, 5, 1, Shadow); break;
			}
			return f;
		}
		int sh0 = k.F(Shad(0)), sh1 = k.F(Shad(1)), sh2 = k.F(Shad(2)), sh3 = k.F(Shad(3));
		int nsDown0 = k.F(Down(1, 0, 4, false)), nsDown1 = k.F(Down(1, 1, 1, false)), nsSur = k.F(G(1, 0, 4, 1, 1, 1, false));
		int nsLook0 = k.F(G(1, 0, 2, 1, 0, 0, false)), nsLook1 = k.F(G(1, 1, 2, 0, 0, 0, false));
		int nsGig0 = k.F(G(1, 0, 1, 3, 0, 0, false)), nsGig1 = k.F(G(2, 1, 1, 3, 0, 0, false));
		int nsCall0 = k.F(G(1, 0, 2, 1, 1, 0, false)), nsCall1 = k.F(G(1, 1, 2, 0, 0, 0, false));
		k.Show(196, "runaway shadow", 100, (nsDown0, 2), (nsDown1, 1), (nsSur, 3), (nsLook0, 3), (nsLook1, 3),
			(nsGig0, 1), (nsGig1, 1), (nsGig0, 1), (nsGig1, 1), (nsGig0, 1), (nsGig1, 1),
			(nsCall0, 2), (nsCall1, 2), (nsCall0, 2), (nsCall1, 2), (nsCall0, 2), (nsCall1, 2), (k.Happy, 3), (k.Stand, 1));
		k.Child(196, 548, "imageX", "imageY");
		p.A(548, "shadow sneaks").Move("-3", "0").Speed("100").Seq(Repeat(6, sh0, sh1)).Then(100, 549).OnBorder(100, 549);
		p.A(549, "shadow bunny").Speed("100").Seq(sh2, sh3, sh2, sh3, sh2, sh3).Then(100, 550);
		p.A(550, "shadow returns").Move("3", "0").Speed("100").Seq(Repeat(6, sh0, sh1));
		k.Sound(196, new Sfx(1.2, 2326).Tone(0.25, 0.25, 500, 900, 0.15, Sfx.Wave.Sine, 0.02, 0.1).Tone(0.5, 0.3, 900, 400, 0.15, Sfx.Wave.Sine, 0.02, 0.2));

		// ================================================================ 197 star in a jar
		// A little star falls from the sky (child window) straight into the glass jar he holds up; he closes the lid.
		Frame JarF(int mode, int t)   // 0 open and empty, looking up; 1 just caught; 2 lid on, glowing
		{
			var f = mode == 0 ? Up(1, t % 2, 1) : G(1, t % 2, mode == 1 ? 4 : 1, mode == 1 ? 1 : 0);
			f.Rect(5, 9, 6, 4, GlowLight).Rect(5, 9, 1, 5, Tear).Rect(10, 9, 1, 5, Tear).Rect(5, 13, 6, 1, Tear);
			f.Dot(4, 8, Tear).Dot(11, 8, Tear);
			if (mode == 2) f.Rect(5, 8, 6, 1, Cone);
			if (mode >= 1)
			{
				Star(f, 8, 11);
				if (t % 2 == 0) f.Dot(8, 11, GlowLight);
				if (mode == 2 && t % 2 == 0) f.Dot(3, 10, Glow).Dot(12, 11, Glow).Dot(3, 13, GlowLight).Dot(12, 8, GlowLight);
			}
			f.Dot(4, 11, Line).Dot(11, 11, Line);
			return f;
		}
		int jr0 = k.F(JarF(0, 0)), jr1 = k.F(JarF(0, 1)), jrC = k.F(JarF(1, 0)), jrG0 = k.F(JarF(2, 0)), jrG1 = k.F(JarF(2, 1));
		Frame Falling(int t)
		{
			var f = new Frame(); Star(f, 8, 0); f.Dot(8, 0, GlowLight);
			f.Dot(8, -2, GlowLight).Dot(8 + t % 2, -3, Zz).Dot(9 - t % 2, -4, GlowLight);
			return f;
		}
		int fs0 = k.F(Falling(0)), fs1 = k.F(Falling(1));
		k.Show(197, "star in a jar", 120, (jr0, 2), (jr1, 2), (jr0, 2), (jr1, 2), (jr0, 2), (jr1, 2), (jrC, 2),
			(jrG0, 1), (jrG1, 1), (jrG0, 1), (jrG1, 1), (jrG0, 1), (jrG1, 1), (jrG0, 1), (jrG1, 1), (jrG0, 1), (jrG1, 1), (k.Happy, 2));
		k.Child(197, 551, "imageX", "imageY-imageH*1.2");
		p.A(551, "falling star").Move("0", "4").Speed("60").Seq(Repeat(12, fs0, fs1));
		k.Sound(197, new Sfx(2.4, 2327).Tone(0.05, 1.35, 2600, 900, 0.08, Sfx.Wave.Sine, 0.05, 0.1, vibHz: 14, vibDepth: 0.03)
			.Bell(1.45, 2093, 0.2, 0.3).Click(1.6, 0.2).Bell(1.75, 1319, 0.1, 0.6));

		// ================================================================ 198 potion cauldron
		// A bubbling cauldron appears in front of him (child window); he stirs, drinks a potion, turns purple and hiccups stars.
		Frame Caul(int t)
		{
			var f = new Frame();
			f.Rect(3, 9, 10, 1, Line).Rect(2, 10, 12, 3, Ink).Rect(3, 13, 10, 1, Ink).Dot(4, 14, Ink).Dot(11, 14, Ink);
			f.Rect(4, 8, 8, 1, Purple).Dot(2, 10, Line).Dot(2, 11, Line);
			int b = t % 4;
			f.Dot(5 + b * 2, 8, PurpleLight).Dot(6 + (b * 3) % 5, 7 - b % 2, PurpleLight).Dot(9 - b % 3, 6 - b % 2, Purple);
			f.Dot(6 + t % 2, 4 - b % 2, Shade).Dot(9 - t % 2, 3, Shade).Dot(8, 1 + t % 2, Shade);
			f.Dot(6, 15, t % 2 == 0 ? Flame : Glow).Dot(8, 15, Flame).Dot(10, 15, t % 2 == 0 ? Glow : Flame).Dot(7, 15, Red).Dot(9, 15, Red);
			return f;
		}
		var caul = new int[4]; for (int i = 0; i < 4; i++) caul[i] = k.F(Caul(i));
		int caulIn = k.F(Fade(Caul(0), 2));
		Frame Stir(int t) { var f = G(1, t % 2, 2, 0, t % 2 == 0 ? 1 : 0, 0); Lean(f, -1, 10); f.Dot(0, 6 + (t % 2) * 2, Cone); return f; }
		Frame Drink(int t) { var f = G(1, t % 2, 1, 4); f.Rect(6, 6, 2, 3, Purple).Rect(6, 5, 2, 1, Tear).Dot(7, 4, Cone); if (t % 2 == 1) f.Dot(6, 6, PurpleLight); return f; }
		Frame Purp(int t, int eyes, int mouth, int lift)
		{
			var f = G(lift, t % 2, eyes, mouth, t % 2, t % 2);
			Recolor(f, Sheet, PurpleLight); Recolor(f, Line, Purple); Recolor(f, Shade, Purple);
			if (lift == 2) { Star(f, 3, -3); Twinkle(f, 12, -3, 0); }
			return f;
		}
		int sr0 = k.F(Stir(0)), sr1 = k.F(Stir(1)), dr0 = k.F(Drink(0)), dr1 = k.F(Drink(1));
		int pu0 = k.F(Purp(0, 4, 1, 1)), puH = k.F(Purp(1, 5, 1, 2)), puL = k.F(Purp(0, 4, 1, 0));
		k.Show(198, "potion", 140, (k.Stand, 1), (sr0, 1), (sr1, 1), (sr0, 1), (sr1, 1), (sr0, 1), (sr1, 1), (sr0, 1), (sr1, 1), (k.Happy, 2),
			(dr0, 1), (dr1, 1), (dr0, 1), (dr1, 1), (pu0, 2), (puH, 1), (puL, 1), (puH, 1), (puL, 1), (k.Fade1, 1), (k.Happy, 2), (k.Giggle1, 1), (k.Giggle2, 1));
		k.Child(198, 552, "imageX-imageW*0.75", "imageY");
		p.A(552, "cauldron appears").Speed("140").Fade(0, 1).Seq(caulIn, caul[0], caul[1]).Then(100, 553);
		p.A(553, "cauldron bubbles").Speed("140").Seq(Repeat(5, caul)).Then(100, 554);
		p.A(554, "cauldron vanishes").Speed("140").Fade(1, 0).Seq(caul[2], caul[3], caulIn);
		{
			var s = new Sfx(3.8, 2328);
			s.Noise(0.1, 2.0, 0.15, 400, 500, 0.2, 0.3, amHz: 6, amDepth: 0.7);
			for (int i = 0; i < 7; i++) s.Pop(0.2 + i * 0.27, 0.15, 250 + (i * 70) % 200);
			s.Tone(1.55, 0.5, 300, 200, 0.12, Sfx.Wave.Sine, 0.05, 0.2, amHz: 10, amDepth: 0.8);   // glug glug
			s.Pop(2.38, 0.35, 1100).Pop(2.66, 0.35, 1200);                                           // hic! hic!
			k.Sound(198, s);
		}

		// ================================================================ 199 orbiting orbs
		// Three glowing spirit orbs circle around him while he conducts them, then they rise away (child window).
		Frame Orbs(int t)
		{
			var f = G(1, t % 2, 1, t % 4 == 0 ? 3 : 0, t % 2 == 0 ? 1 : 0, t % 2 == 0 ? 0 : 1);
			for (int i = 0; i < 3; i++)
			{
				double a = (t * 45 + i * 120) * Math.PI / 180;
				int x = (int)Math.Round(7.5 + 6.5 * Math.Cos(a)), y = (int)Math.Round(4.5 + 6 * Math.Sin(a));
				x = Math.Clamp(x, 1, 14);
				bool front = Math.Sin(a) > 0;
				foreach (var (dx, dy, c) in new[] { (0, 0, GlowLight), (1, 0, GlowCyan), (-1, 0, GlowCyan), (0, -1, GlowCyan), (0, 1, GlowDeep) })
					if (front) f.Dot(x + dx, y + dy, c); else Behind(f, x + dx, y + dy, c);
			}
			return f;
		}
		var orb = new int[8]; for (int i = 0; i < 8; i++) orb[i] = k.F(Orbs(i));
		Frame OrbsRise(int t)
		{
			var f = new Frame();
			(int x, int y)[] pos = { (3, 1), (7, -2), (12, 0) };
			for (int i = 0; i < 3; i++)
			{
				int x = pos[i].x + ((t + i) % 2), y = pos[i].y;
				f.Dot(x, y, GlowLight).Dot(x + 1, y, GlowCyan).Dot(x, y + 1, GlowCyan).Dot(x + 1, y + 1, GlowDeep);
				if ((t + i) % 2 == 0) f.Dot(x, y + 3, GlowCyan);
			}
			return f;
		}
		int or0 = k.F(OrbsRise(0)), or1 = k.F(OrbsRise(1));
		k.Lead(199, "orbiting orbs", 110, 514, (k.Stand, 1), (orb[0], 1), (orb[1], 1), (orb[2], 1), (orb[3], 1), (orb[4], 1), (orb[5], 1), (orb[6], 1), (orb[7], 1),
			(orb[0], 1), (orb[1], 1), (orb[2], 1), (orb[3], 1), (orb[4], 1), (orb[5], 1), (orb[6], 1), (orb[7], 1));
		k.Show(514, "orbs fly away", 110, (upC, 2), (upF1, 2), (upC, 2), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Happy, 2), (k.Stand, 1));
		k.Child(514, 555, "imageX", "imageY");
		p.A(555, "orbs rise").Move("-1", "-3").Speed("110").Fade(1, 0).Seq(Repeat(7, or0, or1));
		k.Sound(199, new Sfx(2.2, 2329).Bell(0.1, 784, 0.1, 0.5).Bell(0.35, 988, 0.1, 0.5).Bell(0.6, 1175, 0.1, 0.5).Bell(0.85, 1568, 0.1, 0.5)
			.Bell(1.1, 784, 0.1, 0.5).Bell(1.35, 988, 0.1, 0.5).Bell(1.6, 1175, 0.1, 0.5));
		k.Sound(514, new Sfx(1.2, 2330).Tone(0, 0.9, 600, 1800, 0.1, Sfx.Wave.Sine, 0.05, 0.4, vibHz: 10, vibDepth: 0.03));
	}
}
