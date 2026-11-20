namespace PetGen;

static partial class BooPet
{
	// 125-149: friends and family. Most of these scenes open child windows with visitors: other little ghosts
	// (recoloured Boo sheets: pink with a bow, mint with a sprout, lilac with a curly tuft, a grey granny with
	// a bun and glasses), a baby ghost, a ghost cat, a ghost dog, bats, a choir, a conga line and a parade.
	// Parts and child windows 300-368, sound seeds 2100-2199.
	static void ScenesB(Kit k)
	{
		var p = k.P;
		int blank = k.Empty;
		const int PinkG = 0, MintG = 1, LilacG = 2, GranG = 3;

		// ------------------------------------------------------------ small local helpers
		int[] R(int f, int n) => Enumerable.Repeat(f, n).ToArray();
		int[] Alt(int a, int b, int n, int each = 1) { var r = new int[n]; for (int i = 0; i < n; i++) r[i] = (i / each) % 2 == 0 ? a : b; return r; }
		int[] Cat(params int[][] parts) => parts.SelectMany(x => x).ToArray();
		int[] L(params int[] f) => f;
		Anim A(int id, string name, int ms, string mx, string my, params int[] frames) => p.A(id, name).Move(mx, my).Speed(ms.ToString()).Seq(frames);
		Anim Main(int id, string name, int ms, params int[] frames) => A(id, name, ms, "0", "0", frames).Then(100, 1);
		Anim Lead(int id, string name, int ms, int next, params int[] frames) => A(id, name, ms, "0", "0", frames).Then(100, next);
		void Put(Frame f, int x, int y, params string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		// a ghost dithering in / out (the shadow stays)
		int[] In(Frame f) => new[] { k.F(Fade(f.Clone(), 3)), k.F(Fade(f.Clone(), 2)), k.F(Fade(f.Clone(), 1)) };
		int[] Out(Frame f) => In(f).Reverse().ToArray();
		void Snd(int anim, Sfx s) => k.Sound(anim, s);

		// ---- the friends: Boo's sheet recoloured, with a small mark of their own
		char SheetOf(int who) => who switch { PinkG => GumC, MintG => GlowLight, LilacG => PurpleLight, _ => Shade };
		char LineOf(int who) => who switch { PinkG => Gum2, MintG => Mint, LilacG => Purple, _ => Line };
		void Tint(Frame f, int who)
		{
			char s = SheetOf(who), l = LineOf(who);
			for (int y = f.MinY; y <= 15; y++)
				for (int x = 0; x < 16; x++)
				{
					if (f[x, y] == Sheet) f[x, y] = s;
					else if (f[x, y] == Line || f[x, y] == Shade) f[x, y] = l;
				}
		}
		void Accessory(Frame f, int who, int lift)
		{
			int t = -lift;                     // top row of the head
			switch (who)
			{
				case PinkG: f.Dot(10, t - 1, Heart).Dot(12, t - 1, Heart).Rect(10, t, 3, 1, Heart).Dot(11, t, Mouth); break;   // a bow
				case MintG: f.Dot(8, t - 1, Stem).Dot(7, t - 2, Stem).Dot(6, t - 2, Stem).Dot(9, t - 2, Stem); break;           // a sprout
				case LilacG: f.Dot(8, t - 1, Line).Dot(8, t - 2, Line).Dot(9, t - 2, Line); break;                               // a curly tuft
				case GranG: f.Stamp(6, t - 2, " oo ", "oWWo"); f.Rect(6, 5 - lift, 4, 1, Ink); break;                         // bun and glasses
			}
		}
		// right = facing right (towards Boo when the friend stands in front of him)
		Frame Fr(int who, int lift, int hem, int eyes, int mouth = 0, int la = 0, int ra = 0, bool shadow = true, bool right = true)
		{
			var f = G(lift, hem, eyes, mouth, la, ra, shadow);
			Accessory(f, who, lift);
			Tint(f, who);
			if (right) f.MirrorX();
			return f;
		}
		int FrId(int who, int lift, int hem, int eyes, int mouth = 0, int la = 0, int ra = 0, bool shadow = true, bool right = true)
			=> k.F(Fr(who, lift, hem, eyes, mouth, la, ra, shadow, right));
		// Boo looking up
		Frame LookUpF(int hem, int mouth = 0)
		{
			var f = G(1, hem, 0, mouth);
			f.Rect(4, 3, 2, 3, Sheet).Rect(10, 3, 2, 3, Sheet).Rect(4, 2, 2, 2, Ink).Rect(10, 2, 2, 2, Ink);
			return f;
		}
		// squashed down by n rows (the hem stays on the ground)
		Frame Squash(Frame f, int n)
		{
			var src = f.Clone();
			for (int y = 9; y >= f.MinY; y--) for (int x = 0; x < 16; x++) f[x, y] = src[x, y - n];
			return f;
		}
		Frame BigBlush(Frame f, int lift) { int y = 7 - lift; f.Rect(2, y, 2, 1, Blush).Rect(12, y, 2, 1, Blush); return f; }
		int lookUp = k.F(LookUpF(0)), lookUp2 = k.F(LookUpF(1));

		// ---- shared friend poses
		var pkGig1F = Fr(PinkG, 1, 0, 1, 3);
		int pkF1 = FrId(PinkG, 1, 0, 2), pkF2 = FrId(PinkG, 2, 1, 2), pkF3 = FrId(PinkG, 1, 1, 2);
		int pkWave1 = FrId(PinkG, 1, 0, 1, 0, 1, 0), pkWave2 = FrId(PinkG, 2, 1, 1, 0, 1, 0);
		int pkGig1 = k.F(pkGig1F), pkGig2 = FrId(PinkG, 2, 1, 1, 3);
		int pkAwayWave1 = FrId(PinkG, 1, 0, 3, 0, 0, 1, true, false), pkAwayWave2 = FrId(PinkG, 2, 1, 3, 0, 0, 1, true, false);
		int pkAway1 = FrId(PinkG, 1, 0, 2, 0, 0, 0, true, false), pkAway2 = FrId(PinkG, 2, 1, 2, 0, 0, 0, true, false);

		var mtGig1F = Fr(MintG, 1, 0, 1, 3);
		int mtGig1 = k.F(mtGig1F), mtGig2 = FrId(MintG, 2, 1, 1, 3);
		int mtA1 = FrId(MintG, 1, 0, 2), mtA2 = FrId(MintG, 2, 1, 2);
		int mtL1 = FrId(MintG, 1, 0, 2, 0, 0, 0, true, false), mtL2 = FrId(MintG, 2, 1, 2, 0, 0, 0, true, false);

		var liBaseF = Fr(LilacG, 1, 0, 2);
		var liGig1F = Fr(LilacG, 1, 0, 1, 3);
		int liBase = k.F(liBaseF), liGig1 = k.F(liGig1F), liGig2 = FrId(LilacG, 2, 1, 1, 3), liHappy = FrId(LilacG, 1, 0, 1, 0);

		// ================================================================ 125 a pink friend comes to visit
		Main(125, "pink friend visits", 150, Cat(R(k.Stand, 2), R(k.LookL, 4), R(k.Surprise, 2), R(k.Happy, 4),
			Alt(k.Wave, k.Wave2, 8, 2), Alt(k.Giggle1, k.Giggle2, 8, 2), Alt(k.Wave, k.Wave2, 8, 2), R(k.Happy, 4)));
		k.Child(125, 300, "imageX-imageW*2", "imageY");
		A(300, "pink friend arrives", 150, "4", "0", L(pkF1, pkF1, pkF2, pkF2, pkF3, pkF3, pkF1, pkF1, pkF2, pkF2, pkF3, pkF3)).Fade(0, 1).Then(100, 301);
		A(301, "pink friend says hi", 150, "0", "0", Cat(Alt(pkWave1, pkWave2, 8, 2), Alt(pkGig1, pkGig2, 8, 2))).Then(100, 302);
		A(302, "pink friend leaves", 150, "-4", "0", Cat(Alt(pkAwayWave1, pkAwayWave2, 4, 2), Alt(pkAway1, pkAway2, 8, 2))).Fade(1, 0);
		Snd(125, new Sfx(2.6, 2100).Noise(0, 1.6, 0.12, 400, 1400, 0.4, 0.6, 120).Bell(1.85, Sfx.Midi(79), 0.3, 0.35).Bell(2.05, Sfx.Midi(84), 0.3, 0.45));

		// ================================================================ 126 a high five with the mint friend (the hands pass through)
		int mtUp = FrId(MintG, 1, 0, 0, 0, 1, 0);
		int mtLean = k.F(Lean(Fr(MintG, 1, 1, 0, 3, 1, 0), 1, 11));
		int mtPass = k.F(Fade(Lean(Fr(MintG, 1, 0, 4, 1, 1, 0), 2, 11), 1));
		int mtSur = FrId(MintG, 1, 0, 4, 1, 1, 1), mtHappy = FrId(MintG, 1, 0, 1, 0);
		int hfUp = k.F(G(1, 0, 0, 0, 1, 0)), hfLean = k.F(Lean(G(1, 1, 0, 3, 1, 0), -1, 11)), hfPass = k.F(Fade(Lean(G(1, 0, 4, 1, 1, 0), -2, 11), 1));
		Main(126, "high five through", 140, Cat(R(k.LookL, 4), R(k.Happy, 4), R(k.Stand, 4),
			R(hfUp, 3), R(hfLean, 2), R(hfPass, 2), R(k.Surprise, 4), Alt(k.Giggle1, k.Giggle2, 4), R(k.Happy, 3),
			Alt(k.Wave, k.Wave2, 8, 2), R(k.Happy, 4)));
		k.Child(126, 303, "imageX-imageW*2", "imageY");
		A(303, "mint friend arrives", 140, "4", "0", Alt(mtA1, mtA2, 12, 2)).Fade(0, 1).Then(100, 304);
		A(304, "mint high five", 140, "0", "0", Cat(R(mtUp, 3), R(mtLean, 2), R(mtPass, 2), R(mtSur, 4), Alt(mtGig1, mtGig2, 4), R(mtHappy, 3))).Then(100, 305);
		A(305, "mint friend leaves", 140, "-4", "0", Alt(mtL1, mtL2, 12, 2)).Fade(1, 0);
		Snd(126, new Sfx(3.4, 2101).Noise(2.1, 0.4, 0.5, 700, 6000, 0.01, 0.2, 300).Bell(2.38, Sfx.Midi(88), 0.15, 0.3)
			.Notes(2.75, 0.09, 0.15, Sfx.Wave.Triangle, 76, 79, 76, 79));

		// ================================================================ 127 the lilac friend whispers a secret
		Frame Whisper(int h)
		{
			var f = Lean(Fr(LilacG, 1, h, 2, 1), 1, 10);
			if (h == 0) f.Dot(15, 0, Zz).Dot(15, 2, Zz);
			else f.Dot(14, -1, Zz).Dot(15, 1, Zz).Dot(15, 3, Zz);
			return f;
		}
		int wh0 = k.F(Whisper(0)), wh1 = k.F(Whisper(1));
		int listen1 = k.F(Lean(G(1, 0, 2, 4), -1, 10)), listen2 = k.F(Lean(G(1, 1, 2, 4), -1, 10));
		int gasp = k.F(BigBlush(G(1, 0, 4, 1), 1));
		int blushGig1 = k.F(BigBlush(G(1, 0, 1, 3), 1)), blushGig2 = k.F(BigBlush(G(2, 1, 1, 3), 2));
		Main(127, "secret whisper", 160, Cat(R(k.Stand, 3), R(k.LookL, 3), Alt(listen1, listen2, 12, 2), R(gasp, 2), Alt(blushGig1, blushGig2, 6)));
		k.Child(127, 306, "imageX-imageW*0.9", "imageY");
		A(306, "lilac whispers", 160, "0", "0", Cat(In(liBaseF), R(liBase, 3), Alt(wh0, wh1, 12, 2), R(liHappy, 2), L(liGig1, liGig2, liGig1), Out(liGig1F)));
		Snd(127, new Sfx(4.2, 2102).Noise(1.0, 1.8, 0.18, 2500, 4000, 0.05, 0.3, 1500, 7, 0.7).Tone(2.9, 0.25, 600, 900, 0.2, Sfx.Wave.Sine, 0.01, 0.1)
			.Notes(3.3, 0.09, 0.13, Sfx.Wave.Triangle, 79, 76, 79, 76));

		// ================================================================ 128 a baby ghost pops in
		string[] babyRows = { "..oooo..", ".oWWWWo.", "oWKWWKWo", "oPWmmWPo", "oWWWWWWo", "oWWWWWWo", "oWoWWoWo", ".o.oo.o." };
		Frame Baby(int dy, bool happy, bool shadow = true)
		{
			var f = new Frame(); Put(f, 4, 7 + dy, babyRows);
			if (happy) f.Dot(5, 9 + dy, Ink).Dot(6, 9 + dy, Ink).Dot(9, 9 + dy, Ink).Dot(10, 9 + dy, Ink).Dot(6, 8 + dy, Sheet).Dot(9, 8 + dy, Sheet);
			if (shadow) { if (dy < -1) f.Rect(7, 15, 2, 1, Shadow); else f.Rect(6, 15, 4, 1, Shadow); }
			return f;
		}
		var sp1 = new Frame(); Star(sp1, 8, 10); sp1.Rect(7, 15, 2, 1, Shadow);
		var sp2 = new Frame(); Star(sp2, 8, 10); sp2.Dot(5, 8, Glow).Dot(11, 8, Glow).Dot(6, 13, Glow).Dot(10, 12, Glow).Rect(7, 15, 2, 1, Shadow);
		var bb0F = Baby(0, false);
		int bbSp1 = k.F(sp1), bbSp2 = k.F(sp2), bb0 = k.F(bb0F), bbM2 = k.F(Baby(-2, false)), bbM3 = k.F(Baby(-3, true)), bbH = k.F(Baby(0, true));
		int bbFly1 = k.F(Baby(-2, true, false)), bbFly2 = k.F(Baby(-3, false, false));
		Main(128, "baby ghost", 150, Cat(R(k.Surprise, 3), R(k.Happy, 3), R(k.Heart1, 4), R(k.Heart2, 4), Alt(k.Giggle1, k.Giggle2, 8, 2),
			R(lookUp, 2), Alt(k.Wave, k.Wave2, 6, 2), R(k.Happy, 2)));
		k.Child(128, 307, "imageX-imageW*0.9", "imageY");
		A(307, "baby ghost bounces", 150, "0", "0", Cat(L(bbSp1, bbSp2, k.F(Fade(bb0F.Clone(), 2)), k.F(Fade(bb0F.Clone(), 1)), bb0, bb0),
			L(bb0, bbM2, bbM3, bbM2, bbH, bbM2, bbM3, bbM2, bb0, bbM2, bbM3, bbM2, bbH, bbM2, bbM3, bbM2))).Then(100, 308);
		A(308, "baby ghost floats up", 150, "-1", "-3", Alt(bbFly1, bbFly2, 10, 2)).Fade(1, 0);
		Snd(128, new Sfx(3.0, 2103).Pop(0.05, 0.4, 1200).Bell(0.15, Sfx.Midi(91), 0.2, 0.3)
			.Tone(0.95, 0.22, 900, 1300, 0.2, Sfx.Wave.Sine, 0.02, 0.1, 9, 0.03).Tone(1.5, 0.22, 1000, 1400, 0.18, Sfx.Wave.Sine, 0.02, 0.1, 9, 0.03)
			.Tone(2.0, 0.25, 1100, 1600, 0.18, Sfx.Wave.Sine, 0.02, 0.1, 9, 0.03));

		// ================================================================ 129 a ghost cat comes to be petted
		Frame CatF(int tail, int legs, bool closed, bool right)
		{
			var f = new Frame();
			string[] head = tail == 0
				? new[] { ".o..o.......", "oWooWo....o.", "oWWWWo...oWo", "oKWKWo...oWo", "oWmWWoooooo." }
				: new[] { ".o..o........", "oWooWo.....o.", "oWWWWo....oWo", "oKWKWo...oWo.", "oWmWWoooooo.." };
			Put(f, 1, 7, head);
			Put(f, 1, 12, ".ooWWWWWWWo.", "..osWWWWsWo.");
			Put(f, 1, 14, legs == 0 ? "..o.o..o.o.." : legs == 1 ? "...o.o..o.o." : "..oooooooo..");
			if (closed) f.Dot(2, 10, Line).Dot(4, 10, Line);
			f.Rect(3, 15, 9, 1, Shadow);
			if (right) f.MirrorX();
			return f;
		}
		var catIn = new int[12];
		for (int i = 0; i < 12; i++) catIn[i] = k.F(CatF((i / 2) % 2, i % 2, false, true));
		var catOut = new int[12];
		for (int i = 0; i < 12; i++) catOut[i] = k.F(CatF((i / 2) % 2, i % 2, false, false));
		var cp1 = CatF(0, 2, true, true); HeartAt(cp1, 10, 3);
		var cp2 = CatF(1, 2, true, true); HeartAt(cp2, 11, 2);
		var cp3 = CatF(0, 2, true, true); cp3.Dot(8, 10, Zz).Dot(7, 9, Zz);
		int catPurr1 = k.F(cp1), catPurr2 = k.F(cp2), catPurr3 = k.F(cp3), catSit = k.F(CatF(1, 2, false, true));
		int pet1 = k.F(Lean(G(1, 0, 1, 0), -1, 12)), pet2 = k.F(G(1, 1, 1, 3));
		Main(129, "ghost cat", 170, Cat(R(k.LookL, 4), R(k.Surprise, 2), R(k.Happy, 6), Alt(pet1, pet2, 16, 2),
			Alt(k.Wave, k.Wave2, 8, 2), R(k.Happy, 4)));
		k.Child(129, 309, "imageX-imageW*1.7", "imageY");
		A(309, "ghost cat walks in", 170, "3", "0", catIn).Fade(0, 1).Then(100, 310);
		A(310, "ghost cat purrs", 170, "0", "0", L(catSit, catSit, catPurr1, catPurr1, catPurr2, catPurr2, catPurr3, catPurr3, catPurr1, catPurr1, catPurr2, catPurr2, catPurr3, catPurr3, catSit, catSit)).Then(100, 311);
		A(311, "ghost cat walks off", 170, "-3", "0", catOut).Fade(1, 0);
		Snd(129, new Sfx(5.0, 2104).Tone(2.0, 0.45, 620, 880, 0.22, Sfx.Wave.Triangle, 0.05, 0.25, 6, 0.02)
			.Tone(2.4, 0.3, 880, 560, 0.18, Sfx.Wave.Triangle, 0.02, 0.2).Noise(2.6, 2.2, 0.25, 260, 260, 0.2, 0.4, 40, 24, 0.9));

		// ================================================================ 130 a ghost dog jumps on him
		Frame Dog(int tail, int legs, bool tongue, bool right, int up = 0)
		{
			var f = new Frame();
			string[] rows =
			{
				"..oooo......",
				tail == 0 ? ".oWWWWso...o" : ".oWWWWso....",
				tail == 0 ? "oWKWWsso..oo" : "oWKWWsso....",
				tail == 0 ? "KWWWWsso.oo." : "KWWWWsso.ooo",
				tongue ? "mmWWWWoooo.." : ".omWWWoooo..",
				"..oWWWWWWo..",
				"..oWWWWWWo..",
				"..osWWWsWo..",
				legs == 0 ? "..o.o..o.o.." : "...o.o..o.o.",
			};
			Put(f, 1, 6 - up, rows);
			if (up > 0) f.Rect(4, 15, 7, 1, Shadow); else f.Rect(3, 15, 9, 1, Shadow);
			if (right) f.MirrorX();
			return f;
		}
		var dogIn = new int[12]; var dogOut = new int[12];
		for (int i = 0; i < 12; i++) { dogIn[i] = k.F(Dog(i % 2, i % 2, false, true)); dogOut[i] = k.F(Dog(i % 2, i % 2, i % 4 < 2, false)); }
		int dgWag1 = k.F(Dog(0, 0, false, true)), dgWag2 = k.F(Dog(1, 0, false, true));
		int dgJump = k.F(Dog(0, 1, true, true, 3)), dgLand = k.F(Dog(1, 0, true, true)), dgLick = k.F(Dog(0, 0, true, true, 2));
		var lk = BigBlush(G(1, 0, 5, 3), 1); lk.Dot(2, 8, Tear); int lick = k.F(lk);
		Main(130, "ghost dog", 120, Cat(R(k.LookL, 4), R(k.Surprise, 4), R(k.Happy, 4), Alt(k.Giggle1, k.Giggle2, 12, 2), R(lick, 4), R(k.Happy, 4),
			Alt(k.Wave, k.Wave2, 8, 2), R(k.Happy, 4)));
		k.Child(130, 312, "imageX-imageW*2.5", "imageY");
		A(312, "ghost dog runs in", 120, "6", "0", dogIn).Fade(0, 1).Then(100, 313);
		A(313, "ghost dog jumps up", 120, "0", "0", L(dgWag1, dgWag2, dgWag1, dgWag2, dgJump, dgJump, dgLand, dgLand, dgJump, dgJump, dgLand, dgLand,
			dgLick, dgLick, dgWag1, dgWag2, dgWag1, dgWag2, dgWag1, dgWag2)).Then(100, 314);
		A(314, "ghost dog runs off", 120, "-6", "0", dogOut).Fade(1, 0);
		Snd(130, new Sfx(2.6, 2105).Tone(1.45, 0.13, 330, 210, 0.4, Sfx.Wave.Saw, 0.005, 0.08).Noise(1.45, 0.1, 0.2, 1500, 600, 0.002, 0.08)
			.Tone(1.72, 0.13, 340, 200, 0.4, Sfx.Wave.Saw, 0.005, 0.08).Noise(1.72, 0.1, 0.2, 1500, 600, 0.002, 0.08).Soften(2200));

		// ================================================================ 131 bats flutter around his head
		string[] batUp = { "U.......U", "UU.K.K.UU", ".UUyKyUU.", "...KKK...", "....K...." };
		string[] batDn = { "...K.K...", "..UyKyU..", ".UUKKKUU.", "UU..K..UU", "U.......U" };
		Frame Bat(bool up, int dither = 0) { var f = new Frame(); Put(f, 3, 5, up ? batUp : batDn); return dither > 0 ? Fade(f, dither) : f; }
		int batU = k.F(Bat(true)), batD = k.F(Bat(false)), batU2 = k.F(Bat(true, 2)), batD1 = k.F(Bat(false, 1));
		Main(131, "bats", 110, Cat(R(k.Surprise, 4), R(k.Hide, 8), R(k.Peek, 4), Alt(lookUp, lookUp2, 6, 2), Alt(k.Giggle1, k.Giggle2, 4),
			Alt(k.Wave, k.Wave2, 4, 2), R(k.Happy, 2)));
		string[][] batMoves = { new[] { "-2", "-1", "2", "1", "-5", "-4" }, new[] { "2", "1", "-2", "-1", "4", "-5" }, new[] { "1", "-2", "-1", "2", "-3", "-6" } };
		string[][] batAt = { new[] { "imageX-imageW*0.7", "imageY-imageH*0.5" }, new[] { "imageX+imageW*0.5", "imageY-imageH*0.7" }, new[] { "imageX-imageW*0.1", "imageY-imageH*0.95" } };
		for (int i = 0; i < 3; i++)
		{
			var m = batMoves[i];
			k.Child(131, 315 + i * 2, batAt[i][0], batAt[i][1]);
			p.A(315 + i * 2, "bat flutters").Move(m[0], m[1], m[2], m[3]).Speed("110")
				.Seq(Cat(i == 1 ? L(batD1, batU2) : L(batU2, batD1), i == 1 ? Alt(batD, batU, 14) : Alt(batU, batD, 14))).Then(100, 316 + i * 2);
			A(316 + i * 2, "bat flies off", 110, m[4], m[5], Alt(batU, batD, 16)).Fade(1, 0);
		}
		Snd(131, new Sfx(3.6, 2106).Noise(0, 1.8, 0.3, 1500, 1500, 0.01, 0.3, 400, 13, 0.9).Tone(0.2, 0.06, 3200, 3800, 0.15)
			.Tone(0.55, 0.05, 3500, 4100, 0.12).Tone(1.0, 0.06, 3000, 3600, 0.14).Noise(1.8, 1.6, 0.22, 1500, 1500, 0.01, 0.6, 400, 13, 0.9));

		// ================================================================ 132 a ghost choir, Boo conducts
		Frame Singer(int who, int ph)
		{
			var f = Fr(who, ph == 0 ? 1 : 2, ph, 1, ph == 0 ? 1 : 2);
			if (ph == 0) Note(f, 2, -3); else Note(f, 12, -4);
			return f;
		}
		var cu = G(1, 0, 0, 1, 1, 0); cu.Dot(0, 4, Cone).Dot(0, 3, Cone).Dot(0, 2, Glow);
		var cd = G(2, 1, 1, 0, 0, 0); cd.Dot(0, 8, Cone).Dot(0, 9, Cone).Dot(0, 10, Glow);
		int condUp = k.F(cu), condDown = k.F(cd), bow = k.F(Lean(G(1, 0, 1, 0), -1, 10));
		Main(132, "ghost choir", 200, Cat(R(k.Stand, 3), Alt(condUp, condDown, 20, 2), R(bow, 3)));
		int[] choirWho = { PinkG, MintG, LilacG };
		string[] choirAt = { "imageX-imageW", "imageX-imageW*1.9", "imageX-imageW*2.8" };
		for (int i = 0; i < 3; i++)
		{
			var s0F = Singer(choirWho[i], 0);
			int s0 = k.F(s0F), s1 = k.F(Singer(choirWho[i], 1));
			var endF = Fr(choirWho[i], 1, 0, 1, 0);
			int[] sing = i == 0 ? Alt(s0, s1, 20, 2) : i == 1 ? Alt(s1, s0, 20, 2) : Alt(s0, s1, 20, 4);
			k.Child(132, 321 + i, choirAt[i], "imageY");
			A(321 + i, "choir singer", 200, "0", "0", Cat(In(s0F), sing, Out(endF)));
		}
		{
			var ch = new Sfx(5.2, 2107);
			int[][] chords = { new[] { 60, 64, 67 }, new[] { 65, 69, 72 }, new[] { 67, 71, 74 }, new[] { 64, 67, 72 } };
			double[] at = { 0.6, 1.8, 3.0, 4.0 }, len = { 1.2, 1.2, 1.0, 1.0 };
			for (int c = 0; c < 4; c++)
				foreach (int n in chords[c]) ch.Tone(at[c], len[c], Sfx.Midi(n), Sfx.Midi(n), 0.1, Sfx.Wave.Sine, 0.15, 0.4, 5, 0.012);
			Snd(132, ch);
		}

		// ================================================================ 133 tag! the pink friend zooms through him and Boo gives chase
		var zm1 = Fr(PinkG, 1, 0, 1, 3, 0, 0, false, false); Lean(zm1, -1, 8); Trail(zm1, (15, 4), (15, 8), (14, 11));
		var zm2 = Fr(PinkG, 2, 1, 1, 3, 0, 1, false, false); Lean(zm2, -1, 8); Trail(zm2, (15, 2), (15, 6), (15, 10));
		int zA = k.F(zm1), zB = k.F(zm2);
		int tagged = k.F(Fade(G(1, 0, 4, 1, 1, 1), 1));
		var chA = G(1, 0, 2, 3, 1, 1); Lean(chA, -1, 6); Trail(chA, (15, 5), (15, 9));
		var chB = G(2, 1, 2, 3, 0, 0); Lean(chB, -1, 5); Trail(chB, (15, 3), (15, 7), (15, 11));
		int chaseA = k.F(chA), chaseB = k.F(chB);
		Lead(133, "tag", 90, 324, Cat(R(k.Stand, 5), R(k.LookR, 2), R(tagged, 3), R(k.Surprise, 2), L(k.SideR, k.Back, k.SideL)));
		A(324, "tag chase", 90, "-6", "0", Alt(chaseA, chaseB, 20, 2)).Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		k.Child(133, 325, "imageX+imageW*1.3", "imageY");
		A(325, "pink friend zooms in", 90, "-8", "0", Alt(zA, zB, 4, 2)).Fade(0.2, 1).Then(100, 367);
		A(367, "pink friend tags", 90, "-8", "0", Alt(zA, zB, 20, 2)).Then(100, 326);
		A(326, "pink friend runs ahead", 90, "-8", "0", Alt(zA, zB, 11, 2)).Fade(1, 0);
		Snd(133, new Sfx(1.6, 2108).Noise(0.45, 0.4, 0.4, 600, 5000, 0.05, 0.25, 300).Notes(0.85, 0.08, 0.15, Sfx.Wave.Triangle, 84, 79, 84));

		// ================================================================ 134 a ghost conga line
		Frame Conga(int who, int st)          // st: 0 lean forward, 1 lean back, 2 kick
		{
			var f = st switch { 0 => G(1, 0, 1, 3, 1, 0), 1 => G(2, 1, 1, 3, 0, 1), _ => G(2, 0, 1, 0, 1, 1) };
			int lift = st == 0 ? 1 : 2;
			if (st == 2) f.Dot(15, 12 - lift, Line).Dot(15, 11 - lift, Line);
			if (who >= 0) { Accessory(f, who, lift); Tint(f, who); }
			if (st == 0) Lean(f, -1, 10); else if (st == 1) Lean(f, 1, 10);
			return f;
		}
		int[] bar = { 0, 0, 1, 1, 0, 0, 2, 2 };
		int[] CongaSeq(int who)
		{
			var fr = new[] { Conga(who, 0), Conga(who, 1), Conga(who, 2) };
			var ids = fr.Select(f => k.F(f)).ToArray();
			var seq = new int[32];
			for (int i = 0; i < 32; i++) seq[i] = ids[bar[i % 8]];
			if (who >= 0)
			{
				var i0 = In(fr[0]); var o2 = Out(fr[2]);
				for (int i = 0; i < 3; i++) { seq[i] = i0[i]; seq[29 + i] = o2[i]; }
			}
			return seq;
		}
		p.A(134, "ghost conga").Move("-2", "0").Speed("140").Seq(CongaSeq(-1)).Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		k.Child(134, 327, "imageX+imageW*0.9", "imageY");
		k.Child(134, 328, "imageX+imageW*1.8", "imageY");
		A(327, "conga pink", 140, "-2", "0", CongaSeq(PinkG));
		A(328, "conga lilac", 140, "-2", "0", CongaSeq(LilacG));
		{
			var cg = new Sfx(4.6, 2109);
			for (int i = 0; i < 16; i++)
			{
				double t = 0.05 + i * 0.28;
				if (i % 4 == 3) cg.Pop(t, 0.35, 900); else cg.Thud(t, 0.3, i % 2 == 0 ? 240 : 180, 110, 0.12);
			}
			Snd(134, cg);
		}

		// ================================================================ 135 peekaboo with the lilac friend
		int liHide = FrId(LilacG, 1, 0, 0, 0, 2, 2), liPB = FrId(LilacG, 2, 0, 1, 1, 1, 1), liSur = FrId(LilacG, 1, 0, 4, 1, 1, 1);
		Main(135, "peekaboo together", 170, Cat(R(k.Stand, 2), R(k.LookL, 6), R(k.Surprise, 3), L(k.Giggle1, k.Giggle2), R(k.Hide, 4),
			R(k.PeekBoo, 3), Alt(k.Giggle1, k.Giggle2, 6), R(k.Happy, 4)));
		k.Child(135, 329, "imageX-imageW", "imageY");
		A(329, "lilac peekaboo", 170, "0", "0", Cat(In(liBaseF), L(liBase), R(liHide, 4), R(liPB, 3), L(liGig1, liGig2), R(liBase, 4), R(liSur, 3),
			Alt(liGig1, liGig2, 6), L(liGig1), Out(liGig1F)));
		Snd(135, new Sfx(3.4, 2110).Tone(1.36, 0.25, 500, 1000, 0.2, Sfx.Wave.Triangle, 0.01, 0.1).Notes(1.6, 0.08, 0.12, Sfx.Wave.Triangle, 79, 84)
			.Tone(2.89, 0.25, 450, 900, 0.2, Sfx.Wave.Triangle, 0.01, 0.1).Notes(3.1, 0.08, 0.12, Sfx.Wave.Triangle, 76, 81));

		// ================================================================ 136 pillow fight
		void Pillow(Frame f, int x, int y) => Put(f, x, y, ".ooo.", "ozzzo", ".ooo.");
		void Feathers(Frame f, int s) { foreach (var (x, y) in s == 0 ? new[] { (1, 1), (3, -2), (0, -1) } : new[] { (2, 0), (0, -3), (4, -1) }) f.Dot(x, y, Sheet).Dot(x + 1, y, Line); }
		Frame PillowPose(int who, int st)     // st: 0 pillow up, 1 swing, 2 hit, 3 giggle A, 4 giggle B (left-facing, then mirrored for the friend)
		{
			Frame f;
			switch (st)
			{
				case 0: f = who < 0 ? G(1, 0, 2, 0, 1, 0) : Fr(who, 1, 0, 2, 0, 1, 0, true, false); Pillow(f, 0, 2); break;
				case 1: f = who < 0 ? G(1, 1, 5, 3) : Fr(who, 1, 1, 5, 3, 0, 0, true, false); Lean(f, -1, 11); Pillow(f, 0, 7); break;
				case 2: f = who < 0 ? G(1, 0, 5, 1) : Fr(who, 1, 0, 5, 1, 0, 0, true, false); Lean(f, 1, 12); Pillow(f, 1, 8); Feathers(f, 0); break;
				case 3: f = who < 0 ? G(1, 0, 1, 3) : Fr(who, 1, 0, 1, 3, 0, 0, true, false); Pillow(f, 0, 8); break;
				default: f = who < 0 ? G(2, 1, 1, 3) : Fr(who, 2, 1, 1, 3, 0, 0, true, false); Pillow(f, 0, 7); break;
			}
			if (who >= 0) f.MirrorX();
			return f;
		}
		int bPU = k.F(PillowPose(-1, 0)), bSW = k.F(PillowPose(-1, 1)), bHIT = k.F(PillowPose(-1, 2)), bG1 = k.F(PillowPose(-1, 3)), bG2 = k.F(PillowPose(-1, 4));
		var mPUF = PillowPose(MintG, 0); var mG1F = PillowPose(MintG, 3);
		int mPU = k.F(mPUF), mSW = k.F(PillowPose(MintG, 1)), mHIT = k.F(PillowPose(MintG, 2)), mG1 = k.F(mG1F), mG2 = k.F(PillowPose(MintG, 4));
		Main(136, "pillow fight", 130, Cat(R(bPU, 4), R(bSW, 2), R(bPU, 3), R(bHIT, 2), R(bPU, 3), R(bSW, 2), R(bHIT, 2), Alt(bG1, bG2, 8), R(k.Happy, 8)));
		k.Child(136, 330, "imageX-imageW", "imageY");
		A(330, "mint pillow fight", 130, "0", "0", Cat(In(mPUF), L(mPU), R(mHIT, 2), R(mPU, 3), R(mSW, 2), R(mPU, 3), R(mSW, 2), R(mHIT, 2),
			Alt(mG1, mG2, 8), R(mG1, 2), Out(mG1F)));
		Frame FeatherCloud(int s)
		{
			var f = new Frame();
			(int x, int y)[] pts = s == 0 ? new[] { (3, 2), (8, 0), (12, 3), (5, 6), (10, 7), (14, 9) } : new[] { (4, 3), (7, 1), (13, 2), (4, 7), (11, 6), (13, 10) };
			foreach (var (x, y) in pts) f.Dot(x, y, Sheet).Dot(x + 1, y + (s == 0 ? 0 : 1), Line);
			return f;
		}
		var fc0 = FeatherCloud(0);
		int fth0 = k.F(fc0), fth1 = k.F(FeatherCloud(1));
		// the feathers wait unseen while the window drifts into place, then flutter down
		k.Child(136, 331, "imageX-imageW*0.17", "imageY-imageH*0.57");
		A(331, "pillow feathers", 130, "-1", "1", Cat(R(blank, 16), Alt(fth0, fth1, 11, 2), Out(fc0)));
		Snd(136, new Sfx(3.2, 2111).Thud(0.52, 0.4, 160, 60, 0.15).Noise(0.52, 0.15, 0.25, 2000, 500, 0.005, 0.1)
			.Thud(1.17, 0.4, 150, 60, 0.15).Noise(1.17, 0.15, 0.25, 2000, 500, 0.005, 0.1)
			.Thud(2.08, 0.5, 160, 50, 0.18).Noise(2.08, 0.3, 0.35, 3000, 600, 0.005, 0.2)
			.Notes(2.4, 0.09, 0.12, Sfx.Wave.Triangle, 76, 79, 76, 79, 83));

		// ================================================================ 137 hide and seek: she vanishes in front and says BOO from behind
		var pkBooF = Fr(PinkG, 2, 1, 4, 2, 1, 1, true, false);
		var pkGigLF = Fr(PinkG, 1, 0, 1, 3, 0, 0, true, false);
		int pkBoo = k.F(pkBooF), pkGigL1 = k.F(pkGigLF), pkGigL2 = FrId(PinkG, 2, 1, 1, 3, 0, 0, true, false);
		Main(137, "hide and seek", 180, Cat(R(k.Hide, 10), R(k.Peek, 2), R(k.LookL, 3), R(k.LookR, 3), R(k.LookL, 2), R(k.Surprise, 4),
			Alt(k.Giggle1, k.Giggle2, 4), R(k.Happy, 2)));
		k.Child(137, 332, "imageX-imageW", "imageY");
		k.Child(137, 333, "imageX+imageW", "imageY");
		A(332, "pink friend hides", 180, "0", "0", Cat(In(Fr(PinkG, 1, 0, 1, 0, 1, 0)), Alt(pkWave1, pkWave2, 4, 2), L(pkGig1, pkGig2), Out(pkGig1F)));
		A(333, "pink friend boo", 180, "0", "0", Cat(R(blank, 17), In(pkBooF), R(pkBoo, 4), L(pkGigL1, pkGigL2, pkGigL1), Out(pkGigLF)));
		Snd(137, new Sfx(4.4, 2112).Bell(0.2, Sfx.Midi(72), 0.15, 0.2).Bell(0.7, Sfx.Midi(74), 0.15, 0.2).Bell(1.2, Sfx.Midi(76), 0.15, 0.2)
			.Tone(3.6, 0.4, 520, 330, 0.35, Sfx.Wave.Triangle, 0.01, 0.2, 7, 0.04).Notes(3.95, 0.08, 0.13, Sfx.Wave.Triangle, 84, 81, 84, 81));

		// ================================================================ 138 group photo with a self-timer camera
		Frame Camera(int st)            // 0 light off, 1 red light on, 2 flash
		{
			var f = new Frame();
			Put(f, 4, 6, ".KK....", "KKKKKK.", "KKKKKBB", "KKKKKBL", "KKKKKK.", "...o...", "..o.o..", ".o...o.", "o.....o");
			if (st == 1) f.Dot(5, 8, Red);
			if (st == 2) { Star(f, 12, 9); f.Dot(14, 6, Glow).Dot(14, 12, Glow).Dot(10, 5, Glow); }
			return f;
		}
		var cam0F = Camera(0);
		int cam0 = k.F(cam0F), cam1 = k.F(Camera(1)), cam2 = k.F(Camera(2));
		int yay1 = k.F(G(1, 0, 1, 0, 1, 1)), yay2 = k.F(G(2, 1, 1, 0, 1, 1)), squeeze = k.F(G(1, 0, 5, 0, 1, 1));
		Main(138, "group photo", 160, Cat(R(k.Stand, 2), R(k.LookL, 2), R(k.Happy, 2), Alt(yay1, yay2, 10, 2), R(squeeze, 2), Alt(k.Giggle1, k.Giggle2, 6), R(k.Happy, 2)));
		k.Child(138, 334, "imageX-imageW*1.9", "imageY");
		A(334, "photo camera", 160, "0", "0", Cat(In(cam0F), Alt(cam1, cam0, 13), R(cam2, 2), R(cam0, 5), Out(cam0F)));
		int[] PhotoSeq(int who)
		{
			var poseF = Fr(who, 1, 0, 1, 0, 1, 1, true, false);
			var gigF = Fr(who, 1, 0, 1, 3, 0, 0, true, false);
			int pose = k.F(poseF), pose2 = FrId(who, 2, 1, 1, 0, 1, 1, true, false), flash = FrId(who, 1, 0, 5, 0, 1, 1, true, false);
			int g1 = k.F(gigF), g2 = FrId(who, 2, 1, 1, 3, 0, 0, true, false);
			return Cat(In(poseF), Alt(pose, pose2, 13, 2), R(flash, 2), Alt(g1, g2, 5), Out(gigF));
		}
		k.Child(138, 335, "imageX+imageW*0.8", "imageY");
		k.Child(138, 336, "imageX-imageW*0.8", "imageY");
		A(335, "photo pink friend", 160, "0", "0", PhotoSeq(PinkG));
		A(336, "photo mint friend", 160, "0", "0", PhotoSeq(MintG));
		Snd(138, new Sfx(3.2, 2113).Tone(0.5, 0.06, 1800, 1800, 0.15, Sfx.Wave.Square).Tone(0.98, 0.06, 1800, 1800, 0.15, Sfx.Wave.Square)
			.Tone(1.46, 0.06, 1800, 1800, 0.15, Sfx.Wave.Square).Tone(1.94, 0.06, 2400, 2400, 0.15, Sfx.Wave.Square)
			.Click(2.56, 0.6).Noise(2.58, 0.12, 0.3, 5000, 2000, 0.002, 0.08, 500).Click(2.7, 0.5).Soften(5000));

		// ================================================================ 139 a ghost parade marches past
		Frame Parade(int ph)
		{
			var f = new Frame();
			char[] flag = { Red, Glow, Mint };
			for (int i = 0; i < 3; i++)
			{
				int x = i * 5, y = 11 - ((i + ph) % 2);
				Mini(f, x, y);
				f.Rect(x + 3, y - 4, 1, 4, Line).Rect(x + 4, y - 4, 2, 2, flag[i]);
				f.Rect(x, 15, 4, 1, Shadow);
			}
			return f;
		}
		int pa0 = k.F(Parade(0)), pa1 = k.F(Parade(1));
		Main(139, "ghost parade", 130, Cat(R(k.LookR, 8), R(k.Surprise, 2), Alt(k.Wave, k.Wave2, 12, 2), Alt(k.Giggle1, k.Giggle2, 8), R(k.LookL, 6), R(k.Happy, 4)));
		// it marches all the way past him (40 steps x 5 = 4 widths), so it ends well in front of him while he watches it go
		k.Child(139, 337, "imageX+imageW*2.2", "imageY");
		A(337, "parade comes", 130, "-5", "0", Alt(pa0, pa1, 6, 2)).Fade(0, 1).Then(100, 338);
		A(338, "parade marches", 130, "-5", "0", Alt(pa0, pa1, 28, 2)).Then(100, 339);
		A(339, "parade goes", 130, "-5", "0", Alt(pa0, pa1, 6, 2)).Fade(1, 0);
		{
			var pr = new Sfx(5.4, 2114);
			for (int i = 0; i < 10; i++) pr.Thud(0.1 + i * 0.52, 0.3, 150, 60, 0.15);
			pr.Notes(0.5, 0.26, 0.12, Sfx.Wave.Square, 67, 72, 72, 74, 76, 74, 72, 71, 72, 76, 79, 76, 72).Soften(2600);
			Snd(139, pr);
		}

		// ================================================================ 140 a hug from the pink friend
		// the cheeks go on before the lean, so they lean with the face and never poke out over the outline
		var hg1 = Lean(BigBlush(Fr(PinkG, 1, 0, 1, 0), 1), 1, 12); var hg2 = Lean(BigBlush(Fr(PinkG, 1, 1, 1, 0), 1), 1, 12);
		int pkOpen1 = FrId(PinkG, 1, 0, 1, 0, 1, 1), pkOpen2 = FrId(PinkG, 2, 1, 1, 0, 1, 1), pkHug1 = k.F(hg1), pkHug2 = k.F(hg2);
		var bh1 = Lean(BigBlush(G(1, 0, 1, 0), 1), -1, 12); HeartAt(bh1, 1, -4);
		var bh2 = Lean(BigBlush(G(1, 1, 1, 0), 1), -1, 12); HeartAt(bh2, 0, -3); HeartAt(bh2, 12, -4);
		int open1 = k.F(G(1, 0, 0, 0, 1, 1)), open2 = k.F(G(2, 1, 1, 0, 1, 1)), hug1 = k.F(bh1), hug2 = k.F(bh2);
		Main(140, "ghost hug", 150, Cat(Alt(open1, open2, 12, 2), Alt(hug1, hug2, 16, 4), Alt(k.Wave, k.Wave2, 8, 2), R(k.Happy, 2)));
		k.Child(140, 340, "imageX-imageW*1.5", "imageY");
		A(340, "pink friend comes for a hug", 150, "3", "0", Alt(pkOpen1, pkOpen2, 12, 2)).Fade(0, 1).Then(100, 341);
		A(341, "pink friend hugs", 150, "0", "0", Alt(pkHug1, pkHug2, 16, 4)).Then(100, 342);
		A(342, "pink friend waves bye", 150, "-3", "0", Alt(pkAwayWave1, pkAwayWave2, 10, 2)).Fade(1, 0);
		Snd(140, new Sfx(3.4, 2115).Tone(1.8, 0.7, 520, 400, 0.18, Sfx.Wave.Sine, 0.15, 0.35, 5, 0.02).Bell(2.3, Sfx.Midi(84), 0.2, 0.4).Bell(2.6, Sfx.Midi(88), 0.18, 0.5));

		// ================================================================ 141 a little bat lands on his head
		string[] sbUp = { "U.K.K.U", "UUyKyUU", "...K..." };
		string[] sbDn = { "..K.K..", ".UyKyU.", "UU.K.UU" };
		string[] sbSit = { ".K.K.", "UyKyU", "UKKKU" };
		var bt0 = LookUpF(0); Put(bt0, 5, -4, sbUp);
		var bt1 = LookUpF(1); Put(bt1, 5, -3, sbDn);
		var bt2 = LookUpF(0, 1); Put(bt2, 6, -4, sbSit);
		var bt3 = LookUpF(0); Put(bt3, 6, -4, sbSit);
		var bt4 = G(1, 0, 6); Put(bt4, 6, -4, sbSit);
		var bt5 = G(1, 0, 5, 3); Put(bt5, 6, -4, sbSit);
		int batHead0 = k.F(bt0), batHead1 = k.F(bt1), batLand = k.F(bt2), batSit = k.F(bt3), batBlink = k.F(bt4), batTickle = k.F(bt5);
		var sbu = new Frame(); Put(sbu, 5, -4, sbUp);
		var sbd = new Frame(); Put(sbd, 5, -3, sbDn);
		int smallBatU = k.F(sbu), smallBatD = k.F(sbd);
		// first it flutters in from up front (its own window, 4 frames), then it is drawn on his head
		Lead(141, "bat on head", 180, 343, Cat(L(lookUp, lookUp2, lookUp, lookUp2), L(batHead0, batHead1, batHead0, batHead1), R(batLand, 2), R(batSit, 3), L(batBlink), R(batSit, 4), R(batTickle, 2)));
		k.Child(141, 368, "imageX-imageW*0.75", "imageY-imageH*0.45");
		A(368, "little bat flies in", 180, "9", "6", L(smallBatU, smallBatD, smallBatU, smallBatD)).Fade(0.3, 1);
		Main(343, "bat flies off", 180, Cat(Alt(k.Giggle1, k.Giggle2, 6), R(lookUp, 2), Alt(k.Wave, k.Wave2, 4, 2)));
		k.Child(343, 344, "imageX", "imageY");
		A(344, "little bat flies away", 110, "-3", "-4", Alt(smallBatU, smallBatD, 16)).Fade(1, 0);
		Snd(343, new Sfx(1.8, 2116).Noise(0, 1.4, 0.3, 1500, 1500, 0.01, 0.4, 400, 13, 0.9).Tone(0.05, 0.06, 3300, 3900, 0.15).Notes(0.3, 0.08, 0.12, Sfx.Wave.Triangle, 79, 76, 79));

		// ================================================================ 142 a lullaby for the baby ghost (no window: the baby is in his arms)
		Frame Cradle(int ph, int baby, bool zz)      // baby: 0 awake, 1 asleep
		{
			var f = G(1, ph, 1, ph == 0 ? 1 : 0);
			Put(f, 1, 8, ".oooo.", "oWWWWo", baby == 0 ? "oKWWKo" : "ooWWoo", "oPWWPo", ".oooo.");
			if (ph == 0) Note(f, 12, -3); else Note(f, 11, -4);
			if (zz) f.Dot(1, 6, Zz).Dot(2, 5, Zz).Dot(0, 4, Zz);
			return Lean(f, ph == 0 ? -1 : 1, 14);
		}
		int crA0 = k.F(Cradle(0, 0, false)), crB0 = k.F(Cradle(1, 0, false)), crA1 = k.F(Cradle(0, 1, false)), crB1 = k.F(Cradle(1, 1, false));
		int crAz = k.F(Cradle(0, 1, true)), crBz = k.F(Cradle(1, 1, true));
		var poof = G(1, 0, 1, 0); Star(poof, 4, 10); poof.Dot(2, 8, Glow).Dot(6, 12, Glow).Dot(1, 12, Glow);
		int babyPoof = k.F(poof);
		// the baby arrives in a sparkle and leaves in one, so it never just pops in or out
		Main(142, "lullaby", 260, Cat(R(babyPoof, 2), Alt(crA0, crB0, 8, 2), Alt(crA1, crB1, 8, 2), Alt(crAz, crBz, 4, 2), R(babyPoof, 2), R(k.Happy, 2)));
		Snd(142, new Sfx(5.6, 2117).Notes(0.2, 0.5, 0.15, Sfx.Wave.Triangle, 72, 76, 74, 71, 72, 69, 67, 72, 71).Soften(1800));

		// ================================================================ 143 granny ghost knits him a scarf
		Frame Gran(int eyes, int mouth, int needle, int scarf, int la = 0, bool held = false)
		{
			var f = Fr(GranG, 1, needle < 0 ? 0 : needle, eyes, mouth, la, 0, true, false);
			if (needle >= 0)
			{
				int s = needle % 2;
				f.Dot(0, 6 + s, BookBlue).Dot(1, 7, BookBlue).Dot(2, 8, BookBlue).Dot(3, 9 - s, BookBlue);
				f.Dot(4, 6 + s, BookBlue).Dot(3, 7, BookBlue).Dot(1, 9 - s, BookBlue);
				for (int j = 0; j < scarf; j++) f.Rect(1, 9 + j, 2, 1, j % 2 == 0 ? Red : RedLight);
				FillCircle(f, 4.5f, 13.5f, 1.4f, Red, RedLight);
			}
			if (held) f.Rect(0, 8, 4, 2, Red).Dot(1, 8, RedLight).Dot(3, 9, RedLight).Rect(0, 10, 1, 2, Red);
			f.MirrorX();
			return f;
		}
		var granInF = Gran(6, 0, 0, 1);
		var granOutF = Gran(1, 0, -1, 0);
		var knit = new int[14];
		for (int i = 0; i < 14; i++) knit[i] = k.F(Gran(6, 0, i % 2, 1 + i / 3));
		int granHold = k.F(Gran(1, 0, -1, 0, 1, true)), granHappy = k.F(granOutF), granWave1 = k.F(Gran(1, 0, -1, 0, 1)), granWave2 = k.F(Gran(1, 3, -1, 0, 1));
		Frame Scarf(Frame f, int lift)
		{
			for (int y = 8 - lift; y <= 9 - lift; y++)
				for (int x = 0; x < 16; x++) if (f[x, y] != '.') f[x, y] = x % 3 == 0 ? RedLight : Red;
			f.Rect(10, 11 - lift, 2, 3, Red).Dot(10, 13 - lift, RedLight);
			return f;
		}
		var sc1 = Scarf(G(1, 0, 1, 0), 1);
		var sc2 = Scarf(G(1, 1, 1, 3), 1); HeartAt(sc2, 12, -2);
		var sc3 = Scarf(G(2, 0, 1, 0, 0, 1), 2);
		int scarf1 = k.F(sc1), scarf2 = k.F(sc2), scarf3 = k.F(sc3);
		Main(143, "granny knits a scarf", 200, Cat(R(k.LookL, 6), R(k.Stand, 2), R(k.LookL, 6), L(k.Blink), R(k.LookL, 2), R(k.Surprise, 3),
			R(scarf1, 4), R(scarf2, 4), R(scarf3, 2)));
		k.Child(143, 345, "imageX-imageW", "imageY");
		A(345, "granny knits", 200, "0", "0", Cat(In(granInF), knit, R(granHold, 3), R(granHappy, 4), L(granWave1, granWave2, granWave1), Out(granOutF)));
		{
			var kn = new Sfx(4.6, 2118);
			for (int i = 0; i < 14; i++) kn.Click(0.6 + i * 0.2, 0.25);
			kn.Bell(4.0, Sfx.Midi(84), 0.25, 0.4).Bell(4.2, Sfx.Midi(88), 0.2, 0.5);
			Snd(143, kn);
		}

		// ================================================================ 144 leapfrog over the mint friend
		var crF = Squash(Fr(MintG, 0, 0, 1, 0), 3);
		int crouch1 = k.F(crF), crouch2 = k.F(Squash(Fr(MintG, 0, 1, 5, 0), 3)), mtUpWow = FrId(MintG, 2, 1, 4, 1, 1, 1);
		int jUp = k.F(G(2, 0, 4, 3, 1, 1, false)), jTop = k.F(G(2, 1, 1, 3, 1, 1, false));
		Lead(144, "leapfrog", 100, 347, Cat(R(k.Stand, 2), R(k.Squash, 2)));
		// 12 steps x 8 = two widths: he lands clear of the friend (who sits one width in front)
		p.A(347, "leapfrog jump").Move("-8", "-10", "-8", "10").Speed("100").Seq(Cat(R(jUp, 4), R(jTop, 4), R(k.Fall, 4)))
			.Then(100, 348)
			.OnBorder(80, 7, "taskbar").OnBorder(40, 7, "window").OnBorder(20, 5, "horizontal+");
		Main(348, "leapfrog landing", 100, Cat(R(k.Squash, 2), R(k.Happy, 2), R(k.LookR, 4), Alt(k.Giggle1, k.Giggle2, 4)));
		k.Child(144, 346, "imageX-imageW", "imageY");
		A(346, "mint friend crouches", 100, "0", "0", Cat(In(crF), Alt(crouch1, crouch2, 13, 2), R(mtUpWow, 2), Alt(mtGig1, mtGig2, 6), Out(mtGig1F)));
		Snd(347, new Sfx(1.6, 2119).Tone(0, 0.35, 200, 600, 0.3, Sfx.Wave.Sine, 0.01, 0.1, 18, 0.05).Thud(1.15, 0.4));

		// ================================================================ 145 fetch: Boo throws a bone and the ghost dog dashes after it
		void BoneH(Frame f, int x, int y) => Put(f, x, y, "ss..ss", "sWWWWs", "ss..ss");                                   // small, in the dog's mouth
		void BigBone(Frame f, int x, int y) => Put(f, x, y, "ss...ss", "sWsssWs", ".WWWWW.", "sWsssWs", "ss...ss");       // flying, easy to see
		void BoneD(Frame f, int x, int y) => Put(f, x, y, "ss....", "sWs...", ".sWs..", "..sWs.", "...sWs", "....ss");
		var boh = G(1, 0, 2, 0, 1, 0);                    // an upright bone in the raised hand, clear of his face
		boh.Dot(0, 1, Shade).Dot(1, 1, Shade).Dot(0, 2, Sheet).Dot(0, 3, Sheet).Dot(0, 4, Shade).Dot(1, 4, Shade);
		var bot = G(1, 1, 1, 3); bot.Dot(0, 6, Line).Dot(1, 6, '.').Dot(0, 8, Line);
		int boneHold = k.F(boh), boneThrow = k.F(bot);
		// the bone window drops about 9 rows in total (see 351), so rows 1-5 here end on the ground (rows 10-14)
		var b1 = new Frame(); BigBone(b1, 0, 1); var b2 = new Frame(); BoneD(b2, 0, 0);
		var b3F = new Frame(); BigBone(b3F, 0, 1);
		int boneFly1 = k.F(b1), boneFly2 = k.F(b2), boneRest = k.F(b3F);
		var dogRun = new int[30]; for (int i = 0; i < 30; i++) dogRun[i] = k.F(Dog(i % 2, i % 2, i % 4 < 2, false));
		Frame DogBone(int i) { var f = Dog(i % 2, i % 2, false, false); BoneH(f, 0, 10); return f; }
		int dogBone1 = k.F(DogBone(0)), dogBone2 = k.F(DogBone(1));
		Lead(145, "fetch", 110, 349, R(boneHold, 6));
		Main(349, "throw the bone", 110, Cat(R(boneThrow, 2), R(k.LookL, 4), R(k.Surprise, 4), R(k.LookL, 16), Alt(k.Giggle1, k.Giggle2, 6),
			Alt(k.Wave, k.Wave2, 4, 2), R(k.Happy, 2)));
		k.Child(349, 350, "imageX", "imageY");
		A(350, "bone flies", 110, "-6", "-5", Alt(boneFly1, boneFly2, 14)).Move("-6", "-5", "-6", "5").Then(100, 351);
		A(351, "bone falls", 110, "-5", "-2", Alt(boneFly2, boneFly1, 14)).Move("-5", "0", "-5", "4").Then(100, 352);
		A(352, "bone on the ground", 110, "0", "0", L(boneRest, k.F(Fade(b3F.Clone(), 2))));
		k.Child(349, 353, "imageX+imageW", "imageY");
		A(353, "ghost dog dashes", 110, "-7", "0", dogRun).Then(100, 354);
		A(354, "ghost dog has the bone", 110, "-7", "0", Alt(dogBone1, dogBone2, 8)).Fade(1, 0);
		Snd(349, new Sfx(4.0, 2120).Noise(0, 0.25, 0.3, 800, 3000, 0.01, 0.15, 300)
			.Tone(0.75, 0.12, 340, 220, 0.35, Sfx.Wave.Saw, 0.005, 0.08).Tone(1.0, 0.12, 330, 210, 0.35, Sfx.Wave.Saw, 0.005, 0.08)
			.Thud(3.1, 0.25, 300, 120, 0.08).Tone(3.4, 0.12, 360, 230, 0.35, Sfx.Wave.Saw, 0.005, 0.08).Soften(2400));

		// ================================================================ 146 a ghost tower: two friends stack on his head and topple
		int pkS1 = FrId(PinkG, 1, 0, 1, 0, 0, 0, false, false), pkS2 = FrId(PinkG, 2, 1, 1, 0, 0, 0, false, false);
		int pkWobL = k.F(Lean(Fr(PinkG, 1, 0, 5, 1, 1, 1, false, false), -1, 14)), pkWobR = k.F(Lean(Fr(PinkG, 1, 1, 5, 1, 1, 1, false, false), 1, 14));
		int pkFall1 = k.F(Lean(Fr(PinkG, 1, 0, 4, 1, 1, 1, false, false), -2, 14)), pkFall2 = k.F(FlipV(Fr(PinkG, 1, 1, 4, 1, 1, 1, false, false)));
		int liS1 = FrId(LilacG, 1, 0, 1, 0, 0, 0, false, false), liS2 = FrId(LilacG, 2, 1, 1, 0, 0, 0, false, false);
		int liWobL = k.F(Lean(Fr(LilacG, 1, 0, 4, 1, 1, 1, false, false), -2, 14)), liWobR = k.F(Lean(Fr(LilacG, 1, 1, 4, 1, 1, 1, false, false), 2, 14));
		int liFall1 = k.F(Lean(Fr(LilacG, 1, 0, 5, 1, 1, 1, false, false), 2, 14)), liFall2 = k.F(FlipV(Fr(LilacG, 1, 1, 5, 1, 1, 1, false, false)));
		int strain = k.F(Squash(G(1, 0, 5, 1), 1));
		int towL = k.F(Lean(Squash(G(1, 0, 5, 1), 1), -1, 12)), towR = k.F(Lean(Squash(G(1, 1, 5, 1), 1), 1, 12));
		Main(146, "ghost tower", 160, Cat(R(lookUp, 6), R(strain, 6), Alt(towL, towR, 14, 2), R(k.Surprise, 2), Alt(k.Giggle1, k.Giggle2, 4), R(k.Happy, 2)));
		k.Child(146, 355, "imageX", "imageY-imageH*1.05");
		A(355, "pink stacks up", 160, "0", "3", Alt(pkS1, pkS2, 6, 2)).Fade(0, 1).Then(100, 356);
		A(356, "pink wobbles", 160, "0", "0", Cat(Alt(pkS1, pkS2, 6, 2), Alt(pkWobL, pkWobR, 14, 2))).Then(100, 357);
		A(357, "pink topples", 160, "-5", "5", Alt(pkFall1, pkFall2, 8, 2)).Fade(1, 0);
		k.Child(146, 358, "imageX", "imageY-imageH*2.1");
		A(358, "lilac stacks up", 160, "0", "3", Cat(R(blank, 6), Alt(liS1, liS2, 6, 2))).Fade(0, 1).Then(100, 359);
		A(359, "lilac wobbles", 160, "0", "0", Alt(liWobL, liWobR, 14, 2)).Then(100, 360);
		A(360, "lilac topples", 160, "5", "4", Alt(liFall1, liFall2, 8, 2)).Fade(1, 0);
		Snd(146, new Sfx(5.4, 2121).Pop(0.9, 0.3, 900).Pop(1.85, 0.3, 1000).Tone(2.0, 2.1, 300, 300, 0.15, Sfx.Wave.Sine, 0.1, 0.3, 4, 0.15)
			.Thud(4.2, 0.5).Thud(4.4, 0.4, 120, 40).Notes(4.6, 0.09, 0.12, Sfx.Wave.Triangle, 79, 76, 79, 76));

		// ================================================================ 147 jump rope: the friends turn it, Boo jumps
		int[] ropeEdge = { 4, 8, 11, 8 };
		Frame JumpRope(int ph)
		{
			var f = ph == 2 ? G(2, 1, 1, 3, 1, 1, false).Shift(0, -1) : G(ph == 0 ? 0 : 1, ph % 2, 1, 0);
			if (ph == 2) f.Rect(6, 15, 4, 1, Shadow);
			for (int x = 0; x < 16; x++)
			{
				double t = (x - 7.5) / 7.5;
				int y = ph switch { 0 => (int)Math.Round(-3 + 7 * t * t), 2 => (int)Math.Round(15 - 4 * t * t), _ => 8 };
				if (ph == 3 && f[x, y] != '.') continue;     // behind him
				f[x, y] = Red;
			}
			return f;
		}
		Frame Holder(int who, int ph, bool right)
		{
			int ey = ropeEdge[ph];
			var f = Fr(who, 1, ph % 2, 1, 3, 0, 0, true, false);
			f.Dot(1, ey, SheetOf(who)).Dot(2, ey, LineOf(who)).Dot(0, ey, Red);
			if (right) f.MirrorX();
			return f;
		}
		var jr = new int[4]; for (int i = 0; i < 4; i++) jr[i] = k.F(JumpRope(i));
		int[] Cycles(int[] ph) { var r = new int[24]; for (int i = 0; i < 24; i++) r[i] = ph[i % 4]; return r; }
		Main(147, "jump rope", 120, Cat(R(k.Happy, 3), Cycles(jr), L(k.Giggle1, k.Giggle2, k.Happy)));
		int[] HolderSeq(int who, bool right)
		{
			var h = new int[4]; for (int i = 0; i < 4; i++) h[i] = k.F(Holder(who, i, right));
			var endF = Fr(who, 1, 0, 1, 3, 0, 0, true, right);
			return Cat(In(Holder(who, 0, right)), Cycles(h), Out(endF));
		}
		k.Child(147, 361, "imageX-imageW", "imageY");
		k.Child(147, 362, "imageX+imageW", "imageY");
		A(361, "pink turns the rope", 120, "0", "0", HolderSeq(PinkG, true));
		A(362, "lilac turns the rope", 120, "0", "0", HolderSeq(LilacG, false));
		{
			var jrs = new Sfx(3.6, 2122);
			for (int i = 0; i < 6; i++) { double t = (5 + 4 * i) * 0.12; jrs.Click(t, 0.4).Thud(t, 0.2, 200, 90, 0.08); }
			jrs.Notes(3.0, 0.09, 0.12, Sfx.Wave.Triangle, 79, 84);
			Snd(147, jrs);
		}

		// ================================================================ 148 a spooky story with a flashlight
		Frame Torch(int h)
		{
			var f = G(1, h, 0, h == 0 ? 1 : 0);
			for (int y = 6; y <= 9; y++) for (int x = 3; x <= 12; x++) if (f[x, y] == Sheet) f[x, y] = Glow;
			f.Rect(7, 10, 2, 2, Ink).Dot(7, 9, GlowLight).Dot(8, 9, GlowLight);
			return f;
		}
		int torch1 = k.F(Torch(0)), torch2 = k.F(Torch(1));
		var mlF = Fr(MintG, 1, 0, 4, 1);
		var ml2 = Fr(MintG, 1, 1, 4, 1); ml2.Shift(-1, 0);
		var mjF = Fr(MintG, 2, 1, 4, 2, 1, 1, false); mjF.Shift(0, -2);
		int mListen1 = k.F(mlF), mListen2 = k.F(ml2), mJump = k.F(mjF);
		Main(148, "spooky story", 220, Cat(R(torch1, 3), Alt(torch2, torch1, 12, 2), L(k.BooPrep), R(k.Boo, 2), Alt(k.Giggle1, k.Giggle2, 4), R(k.Happy, 2)));
		k.Child(148, 363, "imageX-imageW", "imageY");
		A(363, "mint friend gets scared", 220, "0", "0", Cat(In(mlF), Alt(mListen1, mListen2, 13), R(mJump, 2), Out(mjF)));
		Snd(148, new Sfx(4.2, 2123).Tone(0.6, 2.6, 150, 140, 0.25, Sfx.Wave.Sine, 0.4, 0.6, 4, 0.06).Tone(0.6, 2.6, 226, 210, 0.12, Sfx.Wave.Sine, 0.4, 0.6, 5, 0.05)
			.Tone(3.3, 0.35, 240, 170, 0.4, Sfx.Wave.Saw, 0.01, 0.2).Tone(3.5, 0.25, 1400, 2600, 0.2, Sfx.Wave.Sine, 0.01, 0.1).Soften(2500));

		// ================================================================ 149 a birthday cake from the lilac friend
		void Cake(Frame f, int x, int y, int flame)       // 0/1 lit, 2/3 smoke, 4 nothing
		{
			f.Dot(x + 2, y + 1, RedLight);
			Put(f, x, y + 2, "vvvvv", "nhnhn", "nnnnn");
			switch (flame)
			{
				case 0: f.Dot(x + 2, y, Flame).Dot(x + 2, y - 1, Glow); break;
				case 1: f.Dot(x + 2, y, Glow).Dot(x + 1, y - 1, Flame); break;
				case 2: f.Dot(x + 2, y, Line).Dot(x + 3, y - 1, Line); break;
				case 3: f.Dot(x + 3, y - 1, Line).Dot(x + 2, y - 2, Line); break;
			}
		}
		Frame CakeFriend(int lift, int hem, int eyes, int mouth, int flame, bool right)
		{
			var f = Fr(LilacG, lift, hem, eyes, mouth, 0, 0, true, false);
			Cake(f, 0, 7, flame);
			if (right) f.MirrorX();
			return f;
		}
		int ckA = k.F(CakeFriend(1, 0, 2, 0, 0, true)), ckB = k.F(CakeFriend(2, 1, 2, 0, 1, true)), ckC = k.F(CakeFriend(1, 1, 2, 0, 0, true));
		int ckLit1 = k.F(CakeFriend(1, 0, 1, 0, 0, true)), ckLit2 = k.F(CakeFriend(1, 1, 1, 0, 1, true));
		int ckSm1 = k.F(CakeFriend(1, 0, 4, 1, 2, true)), ckSm2 = k.F(CakeFriend(1, 1, 4, 1, 3, true));
		int ckGig1 = k.F(CakeFriend(1, 0, 1, 3, 4, true)), ckGig2 = k.F(CakeFriend(2, 1, 1, 3, 4, true));
		int ckGo1 = k.F(CakeFriend(1, 0, 2, 0, 4, false)), ckGo2 = k.F(CakeFriend(2, 1, 2, 0, 4, false));
		int wow = k.F(G(1, 0, 4, 1));
		var bl = G(1, 0, 5, 1); bl.Dot(0, 5, Zz).Dot(0, 7, Zz).Dot(1, 6, Zz); int blow = k.F(bl);
		(int x, int y, char c)[] conf = { (1, -3, Heart), (5, -4, Glow), (10, -2, Mint), (14, -4, Tear), (13, 1, PurpleLight), (0, 2, Glow), (3, -1, Tear), (8, -4, Heart) };
		var cf1 = G(1, 0, 1, 3, 1, 1); var cf2 = G(2, 1, 1, 3, 1, 1);
		foreach (var c in conf)
		{
			if (cf1[c.x, c.y] == '.') cf1.Dot(c.x, c.y, c.c);
			int x2 = (c.x + 1) % 16, y2 = c.y + 2;
			if (cf2[x2, y2] == '.') cf2.Dot(x2, y2, c.c);
		}
		int conf1 = k.F(cf1), conf2 = k.F(cf2);
		Main(149, "birthday cake", 180, Cat(R(k.LookL, 4), R(k.Surprise, 2), R(k.Happy, 4), R(wow, 2), R(k.Happy, 4), R(blow, 3), R(k.Happy, 2),
			Alt(conf1, conf2, 8, 2), L(k.Heart1), Alt(k.Wave, k.Wave2, 8, 2), R(k.Happy, 2)));
		k.Child(149, 364, "imageX-imageW*1.85", "imageY");
		A(364, "lilac brings a cake", 180, "4", "0", L(ckA, ckA, ckB, ckB, ckC, ckC, ckA, ckA, ckB, ckB)).Fade(0, 1).Then(100, 365);
		A(365, "lilac holds the cake", 180, "0", "0", Cat(Alt(ckLit1, ckLit2, 9), L(ckSm1, ckSm1, ckSm2), Alt(ckGig1, ckGig2, 8, 2))).Then(100, 366);
		A(366, "lilac goes home", 180, "-4", "0", Alt(ckGo1, ckGo2, 10, 2)).Fade(1, 0);
		Snd(149, new Sfx(5.0, 2124).Notes(1.85, 0.13, 0.18, Sfx.Wave.Square, 72, 76, 79, 84, 79, 84).Noise(2.9, 0.5, 0.3, 3000, 1200, 0.02, 0.3, 500)
			.Pop(3.45, 0.3, 900).Pop(3.55, 0.3, 1200).Pop(3.7, 0.3, 700).Soften(3200));
	}
}
