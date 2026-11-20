namespace PetGen;

static partial class MagoPet
{
	// 101-113: lightning, snowman, baby dragon, phoenix, bats, butterflies, dove, whirlwind, magic garden, geyser,
	// ice block, storm cloud and magic carpet. Second parts 150-158, child windows 220-239.
	static void ScenesA(Kit k)
	{
		var p = k.P;
		int blank = k.Empty;

		// ------------------------------------------------------------ small local helpers
		// the wizard: eyes, left arm pose, wand (0 down, 1 up, 2 up and glowing, 3 pointed forward, 4 empty hand down)
		Frame Wz(int eyes, int left = 0, int wand = 0)
		{
			var f = Base(); Eyes(f, eyes); Arm(f, -1, left);
			switch (wand)
			{
				case 0: WandDown(f); break;
				case 1: WandUp(f, false); break;
				case 2: WandUp(f, true); break;
				case 3: Arm(f, 1, 2); f.Rect(15, 4, 1, 3, Wood).Dot(15, 3, Star); break;
				case 4: Arm(f, 1, 0); break;
			}
			return f;
		}
		// a sprite given as rows ('.' keeps what is there), optionally mirrored
		void Put(Frame f, int x, int y, string[] rows, bool mirror = false)
		{
			for (int j = 0; j < rows.Length; j++)
			{
				var r = mirror ? new string(rows[j].Reverse().ToArray()) : rows[j];
				f.Stamp(x, y + j, r.Replace('.', ' '));
			}
		}
		void Snd(int anim, Sfx s) => k.Sounds.Add(new PetSound(anim, 100, s));
		Frame LookUp(bool wide)
		{
			var f = Wz(wide ? 4 : 0);
			f.Dot(5, 5, Skin).Dot(10, 5, Skin).Dot(5, 4, Ink).Dot(10, 4, Ink);
			if (wide) Mouth(f);
			return f;
		}

		int look = k.F(Wz(2));                       // looking to the front (left)
		int lookR = k.F(Wz(3));
		int sad = k.F(Wz(1));

		// ================================================================ 101 lightning
		int lnPoint = k.F(Wz(0, 0, 2));
		Frame Crackle(int c)
		{
			var f = Wz(0, 0, 2);
			if (c == 0) f.Dot(13, -3, HotWhite).Dot(14, -4, Cyan).Dot(12, -2, Cyan);
			else f.Dot(15, -3, HotWhite).Dot(14, -4, HotWhite).Dot(13, -4, Cyan).Dot(15, -2, Cyan);
			return f;
		}
		int lnCr0 = k.F(Crackle(0)), lnCr1 = k.F(Crackle(1));
		var lfl = Wz(4, 1, 1); Mouth(lfl); Recolor(lfl, Robe, Sky); Recolor(lfl, Beard, HotWhite); Recolor(lfl, RobeDark, Robe);
		int lnFlash = k.F(lfl);
		var lst = Wz(4, 1, 1); Mouth(lst);
		int lnStartle = k.F(lst);

		(int x, int y)[] boltPts = { (9, -4), (7, -1), (10, 2), (6, 6), (9, 9), (6, 12), (8, 15) };
		Frame Bolt(int stage)   // 0 the top of the bolt, 1 the whole bolt, 2 bright with a flash on the ground
		{
			var f = new Frame();
			int n = stage == 0 ? 2 : boltPts.Length - 1;
			if (stage == 2)
				for (int i = 0; i < n; i++)
				{
					Line(f, boltPts[i].x - 1, boltPts[i].y, boltPts[i + 1].x - 1, boltPts[i + 1].y, Cyan);
					Line(f, boltPts[i].x + 2, boltPts[i].y, boltPts[i + 1].x + 2, boltPts[i + 1].y, Cyan);
				}
			for (int i = 0; i < n; i++) Line(f, boltPts[i].x + 1, boltPts[i].y, boltPts[i + 1].x + 1, boltPts[i + 1].y, stage == 2 ? HotWhite : PaleYellow);
			for (int i = 0; i < n; i++) Line(f, boltPts[i].x, boltPts[i].y, boltPts[i + 1].x, boltPts[i + 1].y, stage == 2 ? HotWhite : Star);
			if (stage >= 1) Line(f, 11, 3, 13, 5, stage == 2 ? Star : PaleYellow);   // a small fork
			if (stage == 2)
			{
				f.Rect(4, 15, 9, 1, HotWhite).Rect(6, 14, 5, 1, PaleYellow);
				foreach (var (x, y) in new[] { (2, 13), (14, 13), (3, 11), (13, 11), (1, 15), (15, 15) }) f.Dot(x, y, Star);
			}
			return f;
		}
		Frame Scorch(int s)
		{
			var f = new Frame();
			f.Rect(5, 15, 7, 1, Ink).Dot(4, 15, Ash).Dot(12, 15, Ash).Rect(6, 14, 5, 1, Ash).Dot(7, 14, StoneDark).Dot(9, 14, StoneDark);
			if (s < 2) f.Dot(8, 15, s == 0 ? Orange : Ember).Dot(6, 15, Ember);
			for (int j = 0; j < 3; j++)
			{
				int y = 12 - j * 3 - s, x = 7 + ((j + s) % 2) - (j == 2 ? 1 : 0);
				f.Dot(x, y, Smoke).Dot(x + 1, y, j == 0 ? Smoke : LightGray).Dot(x + 1, y - 1, LightGray);
			}
			return f;
		}
		int bt0 = k.F(Bolt(0)), bt1 = k.F(Bolt(1)), bt2 = k.F(Bolt(2));
		int sc0 = k.F(Scorch(0)), sc1 = k.F(Scorch(1)), sc2 = k.F(Scorch(2)), sc3 = k.F(Scorch(3));

		k.Lead(101, "lightning", 130, 150, (k.Stand, 1), (k.Cast1, 2), (lnPoint, 2), (lnCr0, 1), (lnCr1, 1), (lnCr0, 1), (lnCr1, 1), (lnCr0, 1));
		k.Show(150, "lightning strike", 90, (lnFlash, 3), (lnStartle, 4), (look, 7), (k.Happy, 3), (k.Stand, 2));
		k.Child(150, 220, "imageX-imageW*2", "imageY");
		p.A(220, "lightning bolt").Move("0", "0").Speed("70").Seq(bt0, bt1, bt2, bt2, bt1, bt2, bt1, sc0, sc0, sc1).Then(100, 221);
		p.A(221, "scorch mark").Move("0", "0").Speed("190").Loop("2", 0).Fade(1, 0).Seq(sc1, sc2, sc3);

		// ================================================================ 102 snowman
		void Flakes(Frame f, int t, int x0, int x1, int y0, int y1, int n)
		{
			for (int i = 0; i < n; i++)
			{
				int x = x0 + Hash(i, 1, 7) % (x1 - x0 + 1) + ((t + i) / 2) % 2;
				int y = y0 + (Hash(i, 2, 7) % (y1 - y0 + 1) + t) % (y1 - y0 + 1);
				if (f[x, y] == '.') f[x, y] = i % 3 == 0 ? Zz : Beard;
			}
		}
		string[] snowRows =
		{
			"..WWWh..",   //  2 head
			".WKWKWh.",   //  3 coal eyes
			".WWOOWh.",   //  4 carrot
			"..WWWh..",   //  5
			".XXXXXX.",   //  6 red scarf
			".XWWWWh.",   //  7
			".WWKWWh.",   //  8 buttons
			"..WKWh..",   //  9
			".WWWWWh.",   // 10
			"WWWKWWWh",   // 11
			"WWWWWWhh",   // 12
			"WWWKWWhh",   // 13
			".WWWWhh.",   // 14
			"..gggg..",   // 15
		};
		Frame Snowman(int stage, int arm, int t)   // stage 0 only snow .. 5 complete; arm -1 none, 0 down, 1 waving; t >= 0: falling snow
		{
			var f = new Frame();
			int top = stage switch { 0 => 99, 1 => 13, 2 => 10, 3 => 6, _ => 2 };
			for (int j = 0; j < snowRows.Length; j++)
			{
				int y = 2 + j;
				if (y < top) continue;
				string row = snowRows[j];
				if (stage < 5) row = row.Replace('K', 'W').Replace('O', 'W').Replace('X', 'W');
				f.Stamp(4, y, row.Replace('.', ' '));
			}
			if (stage >= 5 && arm >= 0)
			{
				Line(f, 4, 8, 2, 6, Wood); f.Dot(1, 6, Wood).Dot(2, 5, Wood);
				if (arm == 0) { Line(f, 11, 8, 13, 9, Wood); f.Dot(14, 9, Wood).Dot(13, 10, Wood); }
				else { Line(f, 11, 8, 13, 6, Wood); f.Dot(13, 5, Wood).Dot(14, 6, Wood); }
			}
			if (t >= 0) Flakes(f, t, 0, 14, -4, 14, 9);
			return f;
		}
		Frame Melt(int m)   // 1..6: sinks into a puddle
		{
			var full = Snowman(5, -1, -1);
			var f = new Frame();
			double[] sc = { 1, 1.35, 1.9, 3.0, 5.0 };
			if (m <= 4)
				for (int y = 14; y >= -4; y--)
				{
					int src = 14 - (int)Math.Round((14 - y) * sc[m]);
					if (src < 2) break;
					for (int x = 0; x < 16; x++) if (full[x, src] != '.') f[x, y] = full[x, src];
				}
			int pw = new[] { 0, 6, 8, 10, 12, 12, 8 }[m];
			f.Rect(8 - pw / 2, 15, pw, 1, Glass).Dot(8 - pw / 2 + 1, 15, GlassLight).Dot(8 + pw / 2 - 3, 15, GlassLight);
			if (m == 1) { Line(f, 4, 10, 2, 12, Wood); Line(f, 11, 10, 13, 12, Wood); }
			if (m == 2) { Line(f, 4, 12, 1, 14, Wood); Line(f, 11, 12, 14, 14, Wood); }
			if (m >= 3 && m <= 5) { f.Rect(1, 14, 3, 1, Wood).Rect(12, 14, 3, 1, Wood); }
			if (m >= 4 && m <= 5) f.Dot(7, 14, Orange).Dot(8, 14, Orange).Dot(6, 15, Ink).Dot(10, 15, Ink);
			if (m == 6) f.Dot(7, 15, Orange).Dot(9, 15, Ink);
			return f;
		}
		Frame SnowCast(int t) { var f = Wz(0, 0, 2); Flakes(f, t, 0, 12, -4, 4, 6); return f; }
		var snCast = new int[8];
		for (int i = 0; i < 8; i++) snCast[i] = k.F(SnowCast(i));
		var snW1 = Wz(2); Flakes(snW1, 8, 0, 12, -4, 4, 6);
		var snW2 = Wz(2); Flakes(snW2, 9, 0, 12, -4, 4, 6);
		int snWatch1 = k.F(snW1), snWatch2 = k.F(snW2);
		var snSeq = new List<int>();
		int[] stageAt = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 };
		for (int t = 0; t < 12; t++) snSeq.Add(k.F(Snowman(stageAt[t], 0, t)));
		int snDown = k.F(Snowman(5, 0, -1)), snUp = k.F(Snowman(5, 1, -1));
		snSeq.AddRange(new[] { snUp, snDown, snUp, snDown, snDown });
		for (int m = 1; m <= 6; m++) { int fm = k.F(Melt(m)); snSeq.Add(fm); if (m < 6) snSeq.Add(fm); }

		k.Show(102, "snowman", 160, (k.Cast1, 2), (snCast[0], 1), (snCast[1], 1), (snCast[2], 1), (snCast[3], 1), (snCast[4], 1), (snCast[5], 1),
			(snCast[6], 1), (snCast[7], 1), (snWatch1, 1), (snWatch2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Wave2, 1), (look, 7), (sad, 2), (k.Stand, 2));
		k.Child(102, 222, "imageX-imageW", "imageY");
		p.A(222, "snowman builds").Move("0", "0").Speed("160").Seq(snSeq.ToArray());

		// ================================================================ 103 baby dragon
		string[] drUp = { ".d....d..", "GGG..dLd.", "GKGG.dLLd", "GGGGGGGG.", "..yyyGG.G", "..G..G.G." };
		string[] drDn = { ".d.......", "GGG......", "GKGG.....", "GGGGGGGG.", "..yydLLdG", "..G..ddG." };
		Frame Dragon(bool up, bool right) { var f = new Frame(); Put(f, 4, 8, up ? drUp : drDn, right); return f; }
		Frame Egg(int tilt, bool crack)
		{
			var f = new Frame();
			f.Stamp(6 + tilt, 8, " yyy ", "yyGye");
			f.Stamp(6, 10, "yyyye", "Gyyye", "yyyGe", " eee ");
			if (crack) f.Dot(6, 11, BrownDark).Dot(7, 10, BrownDark).Dot(8, 11, BrownDark).Dot(9, 10, BrownDark).Dot(10, 11, BrownDark);
			return f;
		}
		var dsp0 = new Frame(); Sparkle(dsp0, 8, 10, Star);
		var dsp1 = new Frame(); Sparkle(dsp1, 8, 10, Star); dsp1.Dot(5, 8, Cyan).Dot(11, 9, Magenta).Dot(6, 14, Magenta).Dot(11, 13, Cyan);
		var dpop = Dragon(true, false); dpop.Stamp(6, 14, "y  ey", " eee ").Dot(12, 6, PaleYellow).Dot(13, 6, Gold).Dot(3, 7, PaleYellow);
		int drSp0 = k.F(dsp0), drSp1 = k.F(dsp1), drEgg = k.F(Egg(0, false)), drEggL = k.F(Egg(-1, false)), drEggR = k.F(Egg(1, false)), drCrack = k.F(Egg(0, true));
		int drPop = k.F(dpop), drUpL = k.F(Dragon(true, false)), drDnL = k.F(Dragon(false, false)), drUpR = k.F(Dragon(true, true)), drDnR = k.F(Dragon(false, true));
		Frame Puff(int s)
		{
			var f = new Frame();
			switch (s)
			{
				case 0: f.Dot(3, 10, Star).Dot(2, 10, Orange).Dot(2, 9, Orange); break;
				case 1: f.Dot(3, 10, HotWhite).Dot(2, 10, Star).Dot(1, 10, Orange).Dot(2, 9, Orange).Dot(2, 11, FlameRed).Dot(0, 10, FlameRed).Dot(1, 9, FlameRed); break;
				case 2: f.Dot(1, 10, Orange).Dot(0, 10, Smoke).Dot(1, 9, Smoke).Dot(2, 10, Smoke).Dot(1, 11, LightGray); break;
				default: f.Dot(1, 9, LightGray).Dot(0, 10, LightGray); break;
			}
			return f.Shift(0, 1);
		}
		int pf0 = k.F(Puff(0)), pf1 = k.F(Puff(1)), pf2 = k.F(Puff(2)), pf3 = k.F(Puff(3));
		int drGest = k.F(Wz(2, 2, 0));
		var dw = Wz(4, 2, 0); Mouth(dw);
		int drWow = k.F(dw);
		int drUpEyes = k.F(LookUp(false));

		k.Show(103, "baby dragon", 140, (k.Stand, 1), (drGest, 7), (drWow, 2), (look, 3), (drUpEyes, 4), (lookR, 5), (drUpEyes, 2), (look, 2),
			(k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(103, 223, "imageX-imageW/2", "imageY");
		p.A(223, "dragon hatches").Move("0", "0").Speed("140").Seq(blank, blank, drSp0, drSp1, drEgg, drEggL, drEggR, drCrack, drPop, drDnL, drUpL, drDnL, drUpL).Then(100, 224);
		p.A(224, "dragon flaps around").Move("5", "-10", "5", "10").Speed("90").Seq(drUpR, drDnR, drUpR, drDnR, drUpR, drDnR, drUpR, drDnR, drUpR, drDnR, drUpR, drDnR, drUpR, drDnR).Then(100, 225);
		p.A(225, "dragon flies off").Move("-6", "-2", "-5", "-4").Speed("80").Seq(drUpL, drDnL, drUpL, drDnL, drUpL, drDnL, drUpL, drDnL, drUpL, drDnL).Then(100, 225);
		k.Child(225, 226);
		p.A(226, "dragon flame puff").Move("-3", "-1").Speed("70").Fade(1, 0.2).Seq(pf0, pf1, pf1, pf2, pf3, pf3);

		// ================================================================ 104 phoenix
		string[] phUp = { "J.......J", "OJ..Y..JO", ".OJYHYJO.", "..OJYJO..", "...JOJ...", "...O.O...", "..J...J.." };
		string[] phDn = { "....Y....", "...YHY...", "JOOJYJOOJ", ".JOOYOOJ.", "..J.O.J..", "...O.O...", "...J.J..." };
		Frame Phoenix(bool up) { var f = new Frame(); Put(f, 7, -4, up ? phUp : phDn); return f; }
		Frame PhBall(int s)
		{
			var f = new Frame();
			int cx = 12, cy = -1;
			if (s == 0) f.Dot(cx, cy, HotWhite).Dot(cx - 1, cy, Star).Dot(cx + 1, cy, Star).Dot(cx, cy - 1, Star).Dot(cx, cy + 1, Orange);
			if (s >= 1)
			{
				f.Rect(cx - 1, cy - 1, 3, 3, Star).Dot(cx, cy, HotWhite);
				f.Dot(cx - 2, cy, Orange).Dot(cx + 2, cy, Orange).Dot(cx, cy - 2, Orange).Dot(cx, cy + 2, Orange);
			}
			if (s == 2)
				foreach (var (x, y) in new[] { (cx - 3, cy), (cx + 3, cy), (cx, cy - 3), (cx, cy + 3), (cx - 2, cy - 2), (cx + 2, cy - 2), (cx - 2, cy + 2), (cx + 2, cy + 2) })
					f.Dot(x, y, FlameRed);
			return f;
		}
		Frame FireTip(int s)
		{
			var f = Wz(0, 0, 2);
			f.Dot(14, -1, Orange).Dot(14, -2, s == 0 ? Star : FlameRed).Dot(s == 0 ? 13 : 15, -2, FlameRed).Dot(14, -3, s == 0 ? Orange : Star);
			return f;
		}
		var fe0 = new Frame(); fe0.Dot(11, 3, Orange).Dot(12, 2, Star).Dot(10, 4, Ember);
		var fe1 = new Frame(); fe1.Dot(11, 2, Star).Dot(12, 3, Orange).Dot(13, 4, Ember);
		int phU = k.F(Phoenix(true)), phD = k.F(Phoenix(false)), phB0 = k.F(PhBall(0)), phB1 = k.F(PhBall(1)), phB2 = k.F(PhBall(2));
		int ft0 = k.F(FireTip(0)), ft1 = k.F(FireTip(1)), phFe0 = k.F(fe0), phFe1 = k.F(fe1);

		k.Show(104, "phoenix", 140, (k.Stand, 1), (k.Cast1, 2), (lnPoint, 2), (ft0, 1), (ft1, 1), (ft0, 1), (k.Surprise, 3), (k.Happy, 4), (drUpEyes, 2), (look, 3),
			(k.Wave, 2), (k.Wave2, 2), (k.Stand, 2));
		k.Child(104, 227);
		p.A(227, "phoenix burst").Move("0", "0").Speed("140").Seq(blank, blank, blank, blank, blank, blank, blank, phB0, phB1, phB2, phD, phU, phD, phU).Then(100, 228);
		p.A(228, "phoenix rises").Move("-3", "-8").Speed("90").Seq(phU, phD, phU, phD, phU, phD).Then(100, 228);
		k.Child(228, 229);
		p.A(229, "glowing feather").Move("1", "2", "-1", "2").Speed("120").Loop("5", 0).Fade(1, 0).Seq(phFe0, phFe1);

		// ================================================================ 105 bats
		Frame HatUp(bool wide)
		{
			var f = Base(); Eyes(f, wide ? 4 : 0); WandDown(f); NoHat(f);
			HatAt(f, -1, -2);
			Clear(f, -4, -3);
			f.Stamp(6, -4, "rR").Stamp(5, -3, "rRYR");           // keep the hat pointed
			f.Rect(1, 3, 1, 5, Robe).Dot(1, 2, Skin);              // the hand holding the brim up
			if (wide) Mouth(f);
			return f;
		}
		int btGrab = k.F(Wz(0, 1, 0)), btUp = k.F(HatUp(false)), btWide = k.F(HatUp(true));
		var bsh = HatUp(true); bsh.Shift(1, 0);
		int btShake = k.F(bsh);
		var bbk = Wz(4, 1, 0); Mouth(bbk);
		int btBack = k.F(bbk);
		string[] batUp = { "a.....a", "aK.V.Ka", ".KXVXK.", "...V..." };  // a = Ash wing tips, V = Violet body
		string[] batDn = { "..V.V..", ".KXVXK.", "aKKVKKa", "a..V..a" };
		Frame Bat(bool up) { var f = new Frame(); Put(f, 5, -1, up ? batUp : batDn); return f; }
		int batU = k.F(Bat(true)), batD = k.F(Bat(false));

		k.Lead(105, "bats", 140, 157, (k.Stand, 1), (btGrab, 2), (btUp, 2));
		k.Show(157, "bats out", 110, (btWide, 3), (btShake, 1), (btWide, 1), (btShake, 1), (btWide, 2), (btBack, 2), (k.Surprise, 2), (k.Blink, 2), (k.Stand, 2));
		k.Child(157, 230);
		k.Child(157, 231);
		k.Child(157, 230, "imageX+imageW/8", "imageY+imageH/10");
		p.A(230, "bat flies left").Move("-5", "-4", "-6", "-1").Speed("70").Loop("30", 0).Fade(1, 0).Seq(batU, batD);
		p.A(231, "bat flies right").Move("5", "-5", "4", "-2").Speed("70").Loop("30", 0).Fade(1, 0).Seq(batD, batU);

		// ================================================================ 106 butterflies
		void Bfly(Frame f, int x, int y, char c1, char c2, bool open)
		{
			if (open) f.Dot(x - 1, y, c1).Dot(x + 1, y, c1).Dot(x - 1, y + 1, c2).Dot(x + 1, y + 1, c2).Dot(x, y, Ink).Dot(x, y + 1, Ink);
			else f.Dot(x - 1, y - 1, c1).Dot(x + 1, y - 1, c1).Dot(x, y, Ink).Dot(x, y + 1, c2);
		}
		(int x, int y, char a, char b)[] bfs = { (12, -2, Pink, Magenta), (8, 0, Star, Orange), (13, 2, Cyan, Sky) };
		var bspark = new Frame();
		foreach (var b in bfs) Sparkle(bspark, b.x, b.y, b.a);
		var bturn = new Frame();
		foreach (var b in bfs) { Bfly(bturn, b.x, b.y, b.a, b.b, true); bturn.Dot(b.x - 2, b.y - 1, Paper).Dot(b.x + 2, b.y + 2, Paper); }
		Frame BflyFly(int t)
		{
			var f = new Frame();
			for (int i = 0; i < bfs.Length; i++)
			{
				var b = bfs[i];
				Bfly(f, b.x + (t / 2 + i) % 2, b.y - ((t + i) % 4 < 2 ? 0 : 1), b.a, b.b, (t + i) % 2 == 0);
			}
			return f;
		}
		int bfSpark = k.F(bspark), bfTurn = k.F(bturn);
		var bfFly = new int[4];
		for (int t = 0; t < 4; t++) bfFly[t] = k.F(BflyFly(t));
		var bw0 = Wz(0, 0, 2); bw0.Dot(12, -2, Pink).Dot(10, 0, Star).Dot(12, 1, Cyan);
		var bw1 = Wz(0, 0, 2); Sparkle(bw1, 11, -2, Magenta); bw1.Dot(9, 0, Star).Dot(13, 2, Cyan);
		int bfW0 = k.F(bw0), bfW1 = k.F(bw1);

		k.Show(106, "butterflies", 130, (k.Stand, 1), (k.Cast1, 2), (lnPoint, 2), (bfW0, 1), (bfW1, 1), (bfW0, 1), (bfW1, 1), (k.Happy, 3), (drUpEyes, 3), (look, 3),
			(k.Wave, 2), (k.Wave2, 2), (k.Stand, 2));
		k.Child(106, 232);
		k.Child(106, 232, "imageX-imageW/3", "imageY+imageH/6");
		p.A(232, "butterflies fly").Move("-1", "-2", "-3", "-3").Speed("130").Loop("18", 9).Fade(1, 0)
			.Seq(blank, blank, blank, blank, blank, blank, blank, bfSpark, bfTurn, bfFly[0], bfFly[1], bfFly[2], bfFly[3]);

		// ================================================================ 107 dove
		Frame Sleeve(int eyes, int s)   // left sleeve held out: 0 out, 1 shaken up, 2 shaken down
		{
			var f = Base(); Eyes(f, eyes); WandDown(f);
			if (s == 0) Arm(f, -1, 2);
			if (s == 1) f.Dot(1, 8, Robe).Dot(1, 7, Robe).Dot(0, 7, Robe).Dot(0, 6, Skin);
			if (s == 2) f.Dot(1, 8, Robe).Dot(0, 9, Robe).Dot(1, 9, Robe).Dot(0, 10, Skin).Dot(0, 12, Beard);
			return f;
		}
		string[] dvUp = { ".....W.", "....Whg", ".WW.Wg.", "OKWWWWh", "..gWWhW", "....g.." };
		string[] dvDn = { ".......", ".......", ".WW....", "OKWWWWh", "..gWhgW", "...Whg.", "....g.." };
		Frame Dove(bool up) { var f = new Frame(); Put(f, 1, 3, up ? dvUp : dvDn); return f; }
		var dpk0 = new Frame(); dpk0.Dot(3, 7, Beard).Dot(4, 7, BeardShade).Dot(2, 7, Orange).Dot(3, 6, LightGray);
		var dpk1 = new Frame(); dpk1.Dot(2, 6, Beard).Dot(3, 6, Beard).Dot(1, 7, Orange).Dot(2, 7, Ink).Dot(3, 7, Beard).Dot(4, 7, LightGray).Dot(4, 6, BeardShade);
		int dvPeek0 = k.F(dpk0), dvPeek1 = k.F(dpk1), dvU = k.F(Dove(true)), dvD = k.F(Dove(false));
		int slOut = k.F(Sleeve(2, 0)), slUpF = k.F(Sleeve(2, 1)), slDnF = k.F(Sleeve(2, 2));
		var sw = Sleeve(4, 0); Mouth(sw);
		int slWow = k.F(sw);

		k.Show(107, "dove", 140, (k.Stand, 1), (slOut, 1), (slUpF, 1), (slDnF, 1), (slUpF, 1), (slOut, 2), (slWow, 3), (look, 4),
			(k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Happy, 2), (k.Stand, 2));
		k.Child(107, 233, "imageX-imageW/4", "imageY");
		p.A(233, "dove flies").Move("0", "0", "-9", "-6").Speed("150", "70").Loop("20", 7).Fade(1, 0)
			.Seq(blank, blank, blank, blank, dvPeek0, dvPeek1, dvU, dvD, dvU);

		// ================================================================ 108 whirlwind
		Frame Tornado(int h, int t, int leaf)
		{
			var f = new Frame();
			int cxBase = 8;
			for (int i = 0; i < h; i++)
			{
				int y = 15 - i;
				int w = 2 + i * 9 / 10;
				int cx = cxBase + (i < 2 ? 0 : (int)Math.Round(Math.Sin(i * 0.5 + t * 1.3) * 1.2));
				int x0 = cx - w / 2;
				for (int x = x0; x < x0 + w; x++) f[x, y] = i % 2 == 0 ? LightGray : Smoke;
				f[x0, y] = Ash; f[x0 + w - 1, y] = i % 2 == 0 ? Smoke : Ash;
				if (w > 3) f[x0 + 1 + (i + t * 2) % (w - 2), y] = Beard;
			}
			if (h > 0) f.Dot(t % 2 == 0 ? 4 : 5, 15, Stone).Dot(t % 2 == 0 ? 12 : 11, 14, Stone).Dot(3 + t % 3, 13, Stone);
			if (leaf >= 0)
			{
				var lp = new[] { (2, 10), (8, 4), (13, 8), (1, 15) }[leaf];
				f.Dot(lp.Item1, lp.Item2, Leaf).Dot(lp.Item1 + 1, lp.Item2, DarkGreen);
			}
			return f;
		}
		int twS0 = k.F(Tornado(3, 0, 3)), twS1 = k.F(Tornado(5, 1, 3)), twM0 = k.F(Tornado(7, 2, 3)), twM1 = k.F(Tornado(9, 0, 0));
		int twF0 = k.F(Tornado(11, 0, 0)), twF1 = k.F(Tornado(11, 1, 1)), twF2 = k.F(Tornado(11, 2, 2));
		Frame Windy(int s)
		{
			var f = Wz(s == 0 ? 2 : 1, 2, 0);
			for (int y = 7; y <= 12; y++)
				for (int x = 2; x <= 14; x++)
					if ((f[x, y] == Beard || f[x, y] == BeardShade) && f[x - 1, y] == Robe) { f[x - 1, y] = BeardShade; break; }
			f.Dot(8, -4, '.').Dot(9, -4, '.').Dot(7, -4, Robe).Dot(6, -4, s == 0 ? '.' : RobeDark);   // the hat tip bends in the wind
			f.Dot(0, 3 + s, Paper).Dot(1, 11 - s, Paper);
			return f;
		}
		int twCast = k.F(Wz(2, 2, 2)), wnd0 = k.F(Windy(0)), wnd1 = k.F(Windy(1));

		k.Show(108, "whirlwind", 130, (k.Stand, 1), (twCast, 3), (wnd0, 2), (wnd1, 2), (wnd0, 2), (wnd1, 2), (wnd0, 2), (look, 4), (k.Happy, 2), (k.Stand, 2));
		k.Child(108, 234, "imageX-imageW", "imageY");
		p.A(234, "tornado travels").Move("0", "0", "-7", "0").Speed("90").Loop("14", 7)
			.Seq(blank, blank, blank, twS0, twS1, twM0, twM1, twF0, twF1, twF2).Then(100, 235);
		p.A(235, "whirlwind fades").Move("-7", "0").Speed("90").Loop("4", 0).Fade(1, 0).Seq(twF0, twF1, twF2);

		// ================================================================ 109 magic garden
		(int x, int h, char pc, char cc)[] flw = { (3, 4, Red, Star), (6, 7, Violet, Star), (9, 5, Star, Orange), (12, 3, Pink, Star) };
		char Wilt(char c) => c == Red ? Ember : c == Violet ? StoneDark : c == Star ? Gold : c == Pink ? Nose : c == Orange ? Wood : c;
		Frame Garden(int stage, int sway)   // 0 sprouts, 1 shoots, 2 stems, 3 buds, 4 bloom, 5 wilting, 6 wilted, 7-8 fading
		{
			var f = new Frame();
			foreach (var (x, h, pc, cc) in flw)
			{
				int top = 15 - h;
				if (stage == 0) { f.Dot(x, 15, Green); continue; }
				if (stage == 1) { f.Dot(x, 15, Green).Dot(x, 14, Green).Dot(x - 1, 14, Leaf); continue; }
				int stemTop = stage >= 6 ? top + 3 : top + 1;
				for (int y = 15; y >= stemTop; y--)
				{
					int sx = x + (stage == 4 && y <= top + 2 ? sway : 0) + (stage >= 5 && y <= top + 2 ? 1 : 0);
					f[sx, y] = stage >= 5 ? DarkGreen : Green;
				}
				if (h >= 4) f.Dot(x - 1, 13, Leaf).Dot(x + 1, 12, stage >= 5 ? DarkGreen : Leaf);
				if (stage == 3) f.Dot(x, top, pc).Dot(x, top + 1, Green);
				if (stage == 4)
				{
					int hx = x + sway;
					f.Dot(hx, top, cc).Dot(hx - 1, top, pc).Dot(hx + 1, top, pc).Dot(hx, top - 1, pc).Dot(hx, top + 1, pc);
				}
				if (stage == 5)
				{
					int hx = x + 2, hy = top + 2;
					f.Dot(hx, hy, Wilt(cc)).Dot(hx - 1, hy, Wilt(pc)).Dot(hx + 1, hy, Wilt(pc)).Dot(hx, hy + 1, Wilt(pc));
				}
				if (stage >= 6) f.Dot(x + 1, 15, Wilt(pc)).Dot(x - 1, 15, Wilt(pc)).Dot(x + 1, top + 2, DarkGreen);
			}
			if (stage >= 1) foreach (int gx in new[] { 2, 5, 8, 11, 13 }) f.Dot(gx, 15, stage >= 5 ? DarkGreen : Leaf);
			if (stage >= 7)
				for (int y = -4; y <= 15; y++)
					for (int x = 0; x < 16; x++)
						if (f[x, y] != '.' && Hash(x, y, 9) % 10 < (stage == 7 ? 5 : 8)) f[x, y] = '.';
			return f;
		}
		var gd = new int[9];
		for (int s = 0; s < 9; s++) gd[s] = k.F(Garden(s, 0));
		int gdL = k.F(Garden(4, -1)), gdR = k.F(Garden(4, 1));
		Frame Tap(bool glow)
		{
			var f = Base(); Eyes(f, 2); Arm(f, -1, 0);
			f.Rect(14, 8, 1, 2, Robe).Dot(14, 10, Skin).Rect(15, 11, 1, 4, Wood);
			if (glow) f.Dot(15, 15, Star).Dot(14, 15, Cyan).Dot(13, 14, Green).Dot(1, 15, Green).Dot(0, 14, Star).Dot(2, 13, Leaf);
			return f;
		}
		int gdTap = k.F(Tap(false)), gdTapG = k.F(Tap(true));
		var smell = Wz(1, 3, 0);
		int gdSmell = k.F(smell);

		k.Show(109, "magic garden", 150, (k.Stand, 1), (k.Cast1, 1), (gdTap, 2), (k.Cast1, 1), (gdTapG, 2), (look, 4), (k.Happy, 3), (gdSmell, 3), (look, 3), (sad, 3), (k.Stand, 2));
		k.Child(109, 236, "imageX-imageW", "imageY");
		p.A(236, "magic flowers").Move("0", "0").Speed("150")
			.Seq(blank, blank, blank, blank, blank, blank, gd[0], gd[1], gd[2], gd[3], gd[4], gdL, gd[4], gdR, gd[4], gdL, gd[4], gdR, gd[5], gd[5], gd[6], gd[6], gd[7], gd[8]);

		// ================================================================ 110 geyser
		Frame Spout(int h, int t)
		{
			var f = new Frame();
			f.Dot(6, 15, StoneDark).Dot(10, 15, StoneDark).Dot(7, 15, Stone).Dot(9, 15, Stone);
			if (h <= 0) { if (t > 0) f.Dot(8, 14, Cyan).Dot(8, 15, Sky).Dot(7, 13, GlassLight); else f.Dot(8, 15, Ink); return f; }
			int top = 15 - h;
			for (int y = top; y <= 15; y++)
			{
				f.Dot(7, y, Sky).Dot(8, y, Cyan).Dot(9, y, Sky);
				if ((y + t) % 4 == 0) f.Dot(8, y, GlassLight);
				if ((y + t) % 5 == 2) f.Dot(7, y, Cyan);
			}
			f.Rect(7, top, 3, 1, GlassLight);
			if (h >= 6)
				for (int side = -1; side <= 1; side += 2)
					for (int i = 1; i <= 5; i++)
					{
						int x = 8 + side * (1 + i), y = top + (i * i) / 3 + (t + i) % 2;
						f.Dot(x, y, (i + t) % 2 == 0 ? Cyan : GlassLight);
					}
			f.Dot(5, 15, Cyan).Dot(11, 15, Cyan).Dot(6, 14, GlassLight).Dot(10, 14, GlassLight);
			return f;
		}
		Frame Puddle(int w) { var f = new Frame(); f.Rect(8 - w / 2, 15, w, 1, Glass).Dot(8 - w / 2 + 1, 15, GlassLight); return f; }
		var gySeq = new List<int> { k.F(Spout(0, 0)), k.F(Spout(0, 1)), k.F(Spout(3, 0)), k.F(Spout(9, 1)) };
		for (int t = 0; t < 6; t++) gySeq.Add(k.F(Spout(19, t)));
		gySeq.AddRange(new[] { k.F(Spout(12, 2)), k.F(Spout(6, 3)), k.F(Spout(2, 0)), k.F(Puddle(8)), k.F(Puddle(8)), k.F(Puddle(4)) });
		Frame Wet(int t)
		{
			var f = Stand(1);
			int[] xs = { 1, 4, 7, 11, 13, 15 };
			for (int i = 0; i < xs.Length; i++)
			{
				int y = -4 + (i * 5 + t * 3) % 15;
				f.Dot(xs[i], y, Cyan).Dot(xs[i], y - 1, GlassLight);
			}
			f.Dot(8, 7 + t % 2, Cyan);
			return f;
		}
		int wet0 = k.F(Wet(0)), wet1 = k.F(Wet(1)), wet2 = k.F(Wet(2));
		var shL = Stand(1); shL.Shift(-1, 0); shL.Dot(15, 2, Cyan).Dot(15, 9, Cyan).Dot(14, -2, Cyan);
		var shR = Stand(1); shR.Shift(1, 0); shR.Dot(0, 1, Cyan).Dot(0, 8, Cyan).Dot(1, -3, Cyan);
		int gyShL = k.F(shL), gyShR = k.F(shR);
		var gw = Wz(4, 1, 0); Mouth(gw);
		int gyWow = k.F(gw);

		k.Show(110, "geyser", 110, (k.Stand, 1), (look, 3), (gyWow, 3), (wet0, 1), (wet1, 1), (wet2, 1), (wet0, 1), (wet1, 1), (wet2, 1), (wet0, 1),
			(gyShL, 1), (gyShR, 1), (gyShL, 1), (gyShR, 1), (gyShL, 1), (gyShR, 1), (k.Blink, 2), (k.Stand, 2));
		k.Child(110, 237, "imageX-imageW/2", "imageY");
		p.A(237, "water spout").Move("0", "0").Speed("110").Seq(gySeq.ToArray());

		// ================================================================ 111 ice block
		Frame Ice(int stage, int crack, int dx)
		{
			var f = Stand(4); Mouth(f);
			if (stage >= 1) { Recolor(f, Robe, Sky); Recolor(f, Skin, GlassLight); Recolor(f, RobeDark, Robe); }
			int y0 = stage == 0 ? 8 : -4;
			for (int y = y0; y <= 15; y++)
				for (int x = 1; x <= 14; x++)
				{
					bool edge = x == 1 || x == 14 || y == y0 || y == 15;
					if (f[x, y] == '.') f[x, y] = edge ? Sky : Glass;
				}
			foreach (var (x, y) in new[] { (2, y0 + 3), (3, y0 + 2), (4, y0 + 1), (2, y0 + 6), (3, y0 + 5), (12, 13), (13, 12) })
				if (f[x, y] == Glass) f[x, y] = GlassLight;
			if (crack >= 1) { Line(f, 4, -1, 6, 2, HotWhite); Line(f, 6, 2, 5, 5, HotWhite); }
			if (crack >= 2) { Line(f, 10, 8, 12, 11, HotWhite); Line(f, 9, -4, 11, 0, HotWhite); Line(f, 2, 10, 4, 12, HotWhite); }
			if (dx != 0) f.Shift(dx, 0);
			return f;
		}
		var mis = Wz(3, 0, 3); mis.Dot(14, 2, Cyan).Dot(12, 1, GlassLight).Dot(13, 3, Cyan).Dot(11, 3, GlassLight).Dot(15, 1, HotWhite);
		int icMis = k.F(mis), ice0 = k.F(Ice(0, 0, 0)), ice1 = k.F(Ice(1, 0, 0)), iceL = k.F(Ice(1, 0, -1)), iceR = k.F(Ice(1, 0, 1));
		int iceC1 = k.F(Ice(1, 1, 0)), iceC2 = k.F(Ice(1, 2, 0));
		var free = Wz(4, 1, 1); Mouth(free); free.Dot(5, 0, GlassLight).Dot(10, -1, GlassLight).Dot(3, 3, Glass).Dot(12, 3, GlassLight).Dot(4, 12, Glass);
		int icFree = k.F(free);
		Frame Chill(int s)
		{
			var f = Stand(1); f.Dot(8, 7, Cyan).Dot(s == 0 ? 0 : 15, 4, Zz).Dot(s == 0 ? 15 : 0, 9, Zz);
			f.Rect(7, 7, 2, 1, s == 0 ? Ink : Beard).Dot(7, 7, Ink);
			f.Shift(s == 0 ? -1 : 1, 0);
			return f;
		}
		int ch0 = k.F(Chill(0)), ch1 = k.F(Chill(1));
		var shardSrc = new List<(int x, int y)>();
		for (int y = -4; y <= 14; y += 3) { shardSrc.Add((1, y)); shardSrc.Add((13, y + 1)); }
		for (int x = 3; x <= 12; x += 3) { shardSrc.Add((x, -4)); shardSrc.Add((x + 1, 14)); }
		shardSrc.AddRange(new[] { (5, 2), (11, 5), (4, 9), (10, 11) });
		Frame Shards(int s)
		{
			var f = new Frame();
			foreach (var (x, y) in shardSrc)
			{
				double dx = x - 7.5, dy = y - 6, len = Math.Sqrt(dx * dx + dy * dy);
				dx /= len; dy /= len;
				int px = x + (int)Math.Round(dx * s * 1.3), py = y + (int)Math.Round(dy * s * 1.0 + s * s * 0.3);
				f.Dot(px, py, (x + y) % 2 == 0 ? GlassLight : HotWhite).Dot(px + (dx < 0 ? -1 : 1), py, Glass);
				if (s < 2) f.Dot(px, py + 1, Sky);
			}
			return f;
		}
		var shd = new int[6];
		for (int s = 0; s < 6; s++) shd[s] = k.F(Shards(s));

		k.Lead(111, "ice block", 120, 158, (k.Stand, 1), (k.Cast1, 2), (icMis, 2), (k.Surprise, 1), (ice0, 1), (ice1, 3), (iceL, 1), (ice1, 1), (iceR, 1), (ice1, 2),
			(iceL, 1), (iceR, 1), (iceC1, 2), (iceC2, 2));
		k.Show(158, "ice break", 110, (icFree, 3), (ch0, 1), (ch1, 1), (ch0, 1), (ch1, 1), (ch0, 1), (ch1, 1), (ch0, 1), (ch1, 1), (k.Blink, 2), (k.Stand, 2));
		k.Child(158, 238);
		p.A(238, "ice shards").Move("0", "1").Speed("70").Fade(1, 0.1).Seq(shd);

		// ================================================================ 112 storm cloud
		string[] cloudRows =
		{
			"....qqq..qq...",
			"..qqaaaqqaaqq.",
			".qaaaaaaaaaaaq",
			"qaaasaaaaasaaa",
			".ssssssssssss.",
		};
		Frame Cloud(int stage)   // 0 two puffs, 1 closer, 2 whole, 3 rumbling, 4/5 zap, 6/7 drizzle, 8-10 thinning out
		{
			var f = new Frame();
			if (stage == 0) { f.Stamp(0, 3, ".qq.", "qaaq", ".ss."); f.Stamp(11, 2, ".qq..", "qaaaq", ".sss."); return f; }
			if (stage == 1) { f.Stamp(1, 2, "..qq.", ".qaaq", "qaaaa", ".sss."); f.Stamp(9, 1, "..qq..", ".qaaqq", "qaaaaa", ".ssss."); return f; }
			f.Stamp(1, 1, cloudRows);
			if (stage == 3) f.Dot(5, 3, Star).Dot(10, 4, PaleYellow);
			if (stage == 4 || stage == 5)
			{
				f.Dot(6, 3, Star).Dot(9, 2, Star);
				var pts = stage == 4 ? new[] { (8, 6), (7, 8), (9, 9), (8, 11) } : new[] { (8, 6), (9, 7), (7, 9), (8, 11) };
				for (int i = 0; i < 3; i++) { Line(f, pts[i].Item1 + 1, pts[i].Item2, pts[i + 1].Item1 + 1, pts[i + 1].Item2, Star); }
				for (int i = 0; i < 3; i++) Line(f, pts[i].Item1, pts[i].Item2, pts[i + 1].Item1, pts[i + 1].Item2, HotWhite);
			}
			if (stage == 6 || stage == 7) for (int i = 0; i < 4; i++) f.Dot(3 + i * 3, 7 + (i + stage) % 3, Cyan);
			if (stage >= 8)
			{
				Recolor(f, Ash, Smoke); Recolor(f, StoneDark, Ash);
				for (int y = -4; y <= 15; y++)
					for (int x = 0; x < 16; x++)
						if (f[x, y] != '.' && Hash(x, y, 5) % 10 < (stage - 7) * 3) f[x, y] = '.';
			}
			return f;
		}
		var cl = new int[11];
		for (int s = 0; s < 11; s++) cl[s] = k.F(Cloud(s));
		Frame ZapF(int s)
		{
			var f = Wz(4, 1, 0); Mouth(f);
			if (s == 0)
				for (int y = -4; y <= 15; y++)
					for (int x = 0; x < 16; x++)
						if (f[x, y] != '.' && f[x, y] != Ink) f[x, y] = HotWhite;
			var sparks = s == 0 ? new[] { (0, 2), (15, 4), (1, 12), (14, 13), (3, -3) } : new[] { (0, 5), (15, 1), (2, 13), (15, 11), (12, -3) };
			foreach (var (x, y) in sparks) f.Dot(x, y, Star);
			return f;
		}
		Frame Frizz(int t, bool cough)
		{
			var f = Wz(cough ? 1 : 4);
			Recolor(f, BeardShade, Ash); Recolor(f, Robe, RobeDark);
			foreach (var (x, y) in new[] { (2, 6), (13, 6), (1, 8), (14, 8), (2, 10), (13, 10), (4, 12), (11, 12), (6, 13), (9, 13), (3, 5), (12, 5) })
				f[x, y] = (x + y) % 2 == 0 ? Beard : LightGray;
			f.Dot(8, -4, Ash).Dot(9, -4, Ink).Dot(8, -3, Ash);                // singed hat tip
			f.Dot(10 + t % 2, -3 - t % 2, Smoke).Dot(11 - t % 2, -4 + t % 2, LightGray).Dot(3, 2 - t % 2, Smoke).Dot(2, 1 - t % 2, LightGray);
			if (cough)
			{
				Mouth(f);
				f.Dot(5, 7, Smoke).Dot(4, 6 - t % 2, Smoke).Dot(3, 7 - t % 2, LightGray);
				if (t % 2 == 1) Squash(f, 1);
			}
			return f;
		}
		int stUp = k.F(LookUp(false)), stUpW = k.F(LookUp(true)), zp0 = k.F(ZapF(0)), zp1 = k.F(ZapF(1));
		int fz0 = k.F(Frizz(0, false)), fz1 = k.F(Frizz(1, false)), cg0 = k.F(Frizz(0, true)), cg1 = k.F(Frizz(1, true));

		k.Show(112, "storm cloud", 140, (k.Stand, 1), (stUp, 2), (stUpW, 3), (zp0, 1), (zp1, 1), (zp0, 1), (fz0, 2), (fz1, 2), (cg0, 1), (cg1, 2), (cg0, 1), (cg1, 2),
			(fz1, 2), (k.Blink, 2), (k.Stand, 2));
		k.Child(112, 239, "imageX", "imageY-imageH*3/4");
		p.A(239, "dark cloud").Move("0", "0").Speed("140").Seq(cl[0], cl[1], cl[2], cl[3], cl[2], cl[3], cl[4], cl[5], cl[4], cl[6], cl[7], cl[8], cl[9], cl[10]);

		// ================================================================ 113 magic carpet
		void Carpet(Frame f, int y, int wave)
		{
			for (int x = 0; x < 16; x++)
			{
				int yy = y + (wave == 1 && x < 3 ? -1 : 0) + (wave == 2 && x > 12 ? -1 : 0);
				f[x, yy] = x % 3 == 1 ? Star : Red;
				f[x, yy + 1] = Ember;
				if (x == 0 || x == 15) f[x, yy + 2] = Star;
			}
		}
		Frame Ride(int t, bool back)
		{
			var f = Base(); Eyes(f, back ? 3 : 2); Arm(f, -1, 2); WandDown(f);
			Squash(f, 3); Clear(f, 15, 15);
			f.Shift(0, -2);
			Carpet(f, 13, t % 2 == 0 ? 1 : 2);
			f.Dot(3, 12, Shoe).Dot(4, 12, Shoe).Dot(11, 12, Shoe).Dot(12, 12, Shoe);
			f.Dot(15, 9 + t % 2, Paper).Dot(14, 11 - t % 2, Paper);
			return f;
		}
		string[] roll = { ".XX.", "XeXX", ".vv." };
		var cu0 = Stand(2); Put(cu0, 0, 13, roll);
		var cu1 = Stand(2); cu1.Rect(0, 15, 7, 1, Red).Dot(1, 15, Star).Dot(4, 15, Star); Put(cu1, 6, 13, roll);
		var cu2 = Stand(2); cu2.Shift(0, -1); for (int x = 0; x < 16; x++) cu2[x, 15] = x % 3 == 1 ? Star : Red;
		var chop = Wz(4, 1, 1); Mouth(chop); chop.Shift(0, -2); for (int x = 0; x < 16; x++) chop[x, 15] = x % 3 == 1 ? Star : Red;
		var cgone = Stand(2); Sparkle(cgone, 2, 13, Star); cgone.Dot(5, 12, Magenta).Dot(0, 11, Cyan);
		int cUn0 = k.F(cu0), cUn1 = k.F(cu1), cOn = k.F(cu2), cHop = k.F(chop), cGone = k.F(cgone);
		int rd0 = k.F(Ride(0, false)), rd1 = k.F(Ride(1, false)), rdBack = k.F(Ride(0, true));

		k.Lead(113, "magic carpet", 150, 151, (k.Stand, 1), (cUn0, 3), (cUn1, 2), (cOn, 2), (cHop, 2), (rd0, 2));
		p.A(151, "carpet takeoff").Move("-4", "-3").Speed("100").Seq(rd0, rd1, rd0, rd1, rd0, rd1).Then(100, 152);
		p.A(152, "carpet ride").Move("-8", "0").Speed("80").Loop("random/4+18", 0).Seq(rd0, rd0, rd1, rd1)
			.Then(100, 155).OnBorder(100, 153, "vertical");
		var turnA = p.A(153, "carpet turn").Move("0", "0").Speed("140").Seq(rdBack, rdBack, rd0);
		turnA.Action = "flip";
		turnA.Then(100, 154);
		p.A(154, "carpet ride back").Move("-8", "0").Speed("80").Loop("random/4+10", 0).Seq(rd0, rd0, rd1, rd1)
			.Then(100, 155).OnBorder(100, 155, "vertical");
		p.A(155, "carpet landing").Move("0", "3").Speed("100").Seq(rd0, rd1, rd0, rd1, rd0, rd1).Then(100, 156);
		k.Show(156, "carpet roll up", 150, (cOn, 2), (cUn1, 2), (cUn0, 2), (cGone, 2), (k.Happy, 2), (k.Stand, 2));

		// ------------------------------------------------------------ sounds (seeds 300-313)
		Snd(150, new Sfx(1.8, 300).Noise(0, 0.12, 0.9, 9000, 3000, 0.001, 0.08).Noise(0.05, 1.6, 0.7, 900, 120, 0.02, 1.0, 0, 7, 0.4));
		Snd(102, new Sfx(1.6, 301).Bell(0.3, Sfx.Midi(84), 0.3, 0.4).Bell(0.55, Sfx.Midi(88), 0.25, 0.4).Bell(0.8, Sfx.Midi(91), 0.25, 0.5)
			.Noise(0.3, 1.2, 0.06, 6000, 6000, 0.3, 0.6, 2000));
		Snd(103, new Sfx(1.6, 302).Bell(0.28, Sfx.Midi(86), 0.3, 0.3).Bell(0.42, Sfx.Midi(93), 0.2, 0.3).Pop(1.12, 0.5, 900)
			.Tone(1.22, 0.12, 1400, 2100, 0.22, Sfx.Wave.Triangle).Tone(1.38, 0.1, 1500, 2300, 0.18, Sfx.Wave.Triangle));
		Snd(104, new Sfx(2.0, 303).Noise(0.9, 0.9, 0.6, 600, 3000, 0.05, 0.5, 100).Tone(1.35, 0.45, 1200, 1800, 0.2, Sfx.Wave.Triangle, 0.02, 0.2, 9, 0.05));
		Snd(157, new Sfx(1.0, 304).Tone(0.0, 0.06, 3000, 3600, 0.2).Tone(0.15, 0.05, 3300, 3900, 0.18).Tone(0.32, 0.06, 2900, 3500, 0.18)
			.Noise(0, 0.8, 0.22, 2500, 2500, 0.01, 0.3, 300, 18, 0.8));
		Snd(106, new Sfx(1.6, 305).Notes(0.7, 0.09, 0.2, Sfx.Wave.Triangle, 79, 83, 86, 91).Bell(1.1, Sfx.Midi(96), 0.15, 0.4));
		Snd(107, new Sfx(1.6, 306).Noise(0.55, 0.6, 0.3, 2000, 2000, 0.01, 0.2, 300, 14, 0.8).Tone(1.0, 0.3, 420, 380, 0.25, Sfx.Wave.Sine, 0.03, 0.1, 0, 0, 10, 0.5));
		Snd(108, new Sfx(2.2, 307).Noise(0, 2.1, 0.5, 400, 1200, 0.4, 0.8, 150, 3, 0.4));
		Snd(109, new Sfx(2.0, 308).Notes(0.6, 0.12, 0.2, Sfx.Wave.Triangle, 72, 76, 79, 84).Bell(1.2, Sfx.Midi(88), 0.2, 0.5));
		Snd(110, new Sfx(1.8, 309).Noise(0, 0.45, 0.4, 200, 300, 0.05, 0.2).Noise(0.4, 1.1, 0.7, 3000, 1500, 0.01, 0.6, 300));
		Snd(111, new Sfx(1.4, 310).Tone(0.3, 0.15, 1800, 600, 0.3, Sfx.Wave.Square).Bell(0.6, 2600, 0.2, 0.2).Bell(0.68, 3100, 0.15, 0.2).Bell(0.8, 2200, 0.15, 0.3));
		Snd(158, new Sfx(1.0, 311).Noise(0, 0.25, 0.7, 9000, 4000, 0.001, 0.2, 1500).Bell(0.02, 2800, 0.2, 0.2).Bell(0.06, 3500, 0.15, 0.2).Bell(0.1, 2300, 0.15, 0.2));
		Snd(112, new Sfx(2.6, 312).Noise(0.1, 0.8, 0.45, 300, 150, 0.1, 0.4).Noise(0.84, 0.15, 0.9, 9000, 3000, 0.001, 0.1).Tone(0.84, 0.2, 2000, 400, 0.25, Sfx.Wave.Square)
			.Noise(1.85, 0.12, 0.45, 1500, 800, 0.005, 0.08, 200).Noise(2.15, 0.12, 0.4, 1400, 700, 0.005, 0.08, 200));
		Snd(113, new Sfx(1.2, 313).Notes(0.0, 0.08, 0.18, Sfx.Wave.Sine, 76, 79, 83, 88));
		Snd(152, new Sfx(1.0, 314).Noise(0, 0.9, 0.35, 800, 2500, 0.1, 0.5, 200));
	}
}
