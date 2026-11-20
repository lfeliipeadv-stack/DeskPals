namespace PetGen;

static partial class BooPet
{
	// Set A (100-124): spooky but friendly Halloween pranks. Parts and child windows 200-299, sound seeds 2000-2099.
	// 100 rattle chain, 101 haunted teacup, 102 candelabra, 103 jack-o-lantern, 104 costume party, 105 trick or treat,
	// 106 creaky door, 107 spider, 108 cauldron, 109 skeleton friend, 110 moon howl, 111 mouse scare, 112 spooky wail,
	// 113 bat perch, 114 tombstone, 115 haunted broom, 116 flicker lamp, 117 monster mask, 118 wisps, 119 bobbing apples,
	// 120 black cat, 121 cobweb, 122 potion, 123 flashlight story, 124 spooky fog.
	static void ScenesA(Kit k)
	{
		var p = k.P;
		int blank = k.Empty;

		// ------------------------------------------------------------ small local helpers
		void Put(Frame f, int x, int y, params string[] rows) { for (int j = 0; j < rows.Length; j++) f.Stamp(x, y + j, rows[j].Replace('.', ' ')); }
		IEnumerable<(int x, int y)> Pts(int x0, int y0, int x1, int y1)
		{
			int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
			while (true)
			{
				yield return (x0, y0);
				if (x0 == x1 && y0 == y1) yield break;
				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; x0 += sx; }
				if (e2 <= dx) { err += dx; y0 += sy; }
			}
		}
		void Seg(Frame f, int x0, int y0, int x1, int y1, char c) { foreach (var (x, y) in Pts(x0, y0, x1, y1)) f[x, y] = c; }
		Frame Map(Frame f, params (char from, char to)[] m)
		{
			for (int y = f.MinY; y <= 15; y++) for (int x = 0; x < 16; x++)
				foreach (var (a, b) in m) if (f[x, y] == a) { f[x, y] = b; break; }
			return f;
		}
		Frame Cut(Frame f, int fromY) { for (int y = fromY; y <= 15; y++) for (int x = 0; x < 16; x++) f[x, y] = '.'; return f; }
		// looking up: dir -1 up-left, 0 straight up, 1 up-right
		Frame Up(int lift, int hem, int mouth = 0, int la = 0, int ra = 0, int dir = 0)
		{
			var f = G(lift, hem, 7, mouth, la, ra); int y = 5 - lift;
			f.Rect(4 + dir, y - 2, 2, 2, Ink).Rect(10 + dir, y - 2, 2, 2, Ink);
			return f;
		}
		Anim Part(int id, string name, int ms, params int[] frames) => p.A(id, name).Move("0", "0").Speed(ms.ToString()).Seq(frames);
		int[] Rep(int n, params int[] frames) { var l = new List<int>(); for (int i = 0; i < n; i++) l.AddRange(frames); return l.ToArray(); }

		// 299: a moving part that reaches the border of the screen just disappears
		Part(299, "part gone", 60, blank);

		// ================================================================ 100 rattle chain
		// Boo lifts a clanking chain over his head with both hands and rattles it, then drops it in a heap.
		void ChainArc(Frame f, int lift, int h, int wob)
		{
			int y0 = 5 - lift, px = 0, py = y0, idx = 0;
			for (int x = 0; x <= 15; x++)
			{
				double t = x / 15.0;
				int y = (int)Math.Round(y0 - h * Math.Sqrt(Math.Max(0, 1 - (2 * t - 1) * (2 * t - 1))) + wob * Math.Sin(t * 3 * Math.PI));
				foreach (var (cx, cy) in Pts(px, py, x, y))
				{
					if (cx == px && cy == py && idx > 0) continue;
					f[cx, cy] = idx % 2 == 0 ? Ink : Line;
					idx++;
				}
				px = x; py = y;
			}
		}
		Frame Chain(int lift, int hem, int eyes, int mouth, int h, int wob)
		{
			var f = G(lift, hem, eyes, mouth, 1, 1); ChainArc(f, lift, h, wob);
			if (wob != 0) f.Dot(0, 1 - lift, Ink).Dot(15, 2 - lift, Ink).Dot(0, -1 - lift, Ink);   // rattle ticks
			return f;
		}
		Frame Heap(int lift, int hem, int eyes, int mouth)
		{
			var f = G(lift, hem, eyes, mouth);
			f.Dot(0, 15, Ink).Dot(1, 15, Line).Dot(2, 15, Ink).Dot(3, 15, Line).Dot(4, 15, Ink).Dot(1, 14, Ink).Dot(2, 14, Line).Dot(3, 14, Ink);
			return f;
		}
		int chHold = k.F(Chain(1, 0, 2, 0, 8, 0));
		int chA = k.F(Chain(1, 0, 4, 1, 8, 1)), chB = k.F(Chain(2, 1, 5, 1, 5, -1)), chC = k.F(Chain(1, 1, 4, 3, 6, 1));
		int chDrop = k.F(Heap(1, 0, 4, 1)), chG1 = k.F(Heap(1, 0, 1, 3)), chG2 = k.F(Heap(2, 1, 1, 3));
		k.Show(100, "rattle chain", 110, (k.Stand, 1), (chHold, 3), (chA, 2), (chB, 2), (chA, 2), (chC, 2), (chB, 2), (chA, 2), (chC, 2), (chB, 2),
			(chHold, 2), (chDrop, 2), (chG1, 2), (chG2, 2), (chG1, 2), (chG2, 2), (k.Happy, 2));
		{
			var s = new Sfx(2.8, 2000); var r = new Random(2000);
			for (int i = 0; i < 16; i++)
			{
				double at = 0.44 + i * 0.11;
				s.Noise(at, 0.06, 0.16, 7000, 2500, 0.001, 0.05, 1500).Bell(at + 0.01, 1900 + r.Next(900), 0.09, 0.05);
			}
			s.Thud(2.42, 0.35, 300, 120, 0.12).Noise(2.42, 0.22, 0.14, 5000, 1500, 0.001, 0.15, 1200);
			k.Sound(100, s);
		}

		// ================================================================ 101 haunted teacup
		// A teacup appears in front of Boo, rattles and hops on its own saucer; Boo jumps, then laughs.
		Frame Cup(int lift, int tilt, int steam, bool spill, int mark)
		{
			var f = new Frame(); int x = 8, y = 10 - lift;
			f.Rect(x - 1, 15, 8, 1, Line);                                            // saucer
			string[] rows = { "onnnno..", "oWWWWooo", "oBWBWo.o", "oWWWWooo", ".oWWo..." };
			for (int j = 0; j < rows.Length; j++) Put(f, x + (j < 2 ? tilt : 0), y + j, rows[j]);
			if (steam == 1) f.Dot(x + 1, y - 2, Zz).Dot(x + 2, y - 3, Zz).Dot(x + 1, y - 4, Zz).Dot(x + 4, y - 2, Zz);
			if (steam == 2) f.Dot(x + 2, y - 2, Zz).Dot(x + 1, y - 3, Zz).Dot(x + 2, y - 4, Zz).Dot(x + 3, y - 3, Zz);
			if (spill) f.Dot(x - 1, y + 1, Cone).Dot(x - 2, y + 3, Cone);
			if (mark == -1) f.Dot(x - 3, y + 1, Line).Dot(x - 3, y + 3, Line);       // rattle marks
			if (mark == 1) f.Dot(x - 2, y, Line).Dot(x - 3, y + 2, Line);
			if (mark == 2) f.Dot(x - 3, 14, Line).Dot(x + 8, 14, Line).Dot(x - 2, 13, Line);   // landing puff
			return f;
		}
		int cupR0 = k.F(Cup(0, 0, 1, false, 0)), cupR1 = k.F(Cup(0, 0, 2, false, 0));
		int cupJL = k.F(Cup(0, -1, 0, false, -1)), cupJR = k.F(Cup(0, 1, 0, false, 1));
		int cupH1 = k.F(Cup(2, 0, 0, false, 0)), cupH2 = k.F(Cup(3, -1, 0, true, 0)), cupLand = k.F(Cup(0, 0, 0, false, 2));
		k.Show(101, "haunted teacup", 140, (k.Stand, 2), (k.LookL, 2), (k.Surprise, 4), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1),
			(k.LookL, 3), (k.Surprise, 2), (k.Giggle1, 2), (k.Giggle2, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Wave, 2), (k.Wave2, 2), (k.Happy, 2));
		k.Child(101, 200, "imageX-imageW*0.9", "imageY");
		Part(200, "teacup appears", 140, cupR0, cupR1, cupR0, cupR1).Fade(0, 1).Then(100, 201);
		Part(201, "teacup hops", 140, cupJL, cupJR, cupJL, cupJR, cupH1, cupH2, cupLand, cupR0, cupJR, cupJL, cupH1, cupH2, cupLand, cupR1, cupR0, cupR1).Then(100, 202);
		Part(202, "teacup fades", 140, cupR0, cupR1, cupR0, cupR1).Fade(1, 0);
		{
			var s = new Sfx(2.8, 2001);
			foreach (double at in new[] { 0.56, 0.70, 0.84, 0.98, 1.68, 1.82 }) s.Bell(at, 2600 + (int)(at * 300) % 400, 0.16, 0.05);
			foreach (double at in new[] { 1.40, 2.24 }) s.Bell(at, 2200, 0.3, 0.12).Click(at, 0.3);
			k.Sound(101, s);
		}

		// ================================================================ 102 candelabra
		// Three candles on a candelabra; a draft puts them out one by one, Boo shivers in the dark, then relights them.
		Frame Candelabra(int lit, int fl, int eyes, int mouth, int lift, int sL, int sM, int sR, int dx = 0, bool spark = false)
		{
			var f = G(lift, fl % 2, eyes, mouth); if (dx != 0) f.Shift(dx, 0);
			f.Rect(5, 14, 6, 1, Cone).Rect(7, 13, 2, 1, Cone).Rect(3, 12, 10, 1, Cone);       // base, stem, arms
			f.Rect(3, 10, 1, 2, Red).Rect(7, 10, 2, 2, Red).Rect(12, 10, 1, 2, Red).Dot(3, 10, RedLight).Dot(7, 10, RedLight).Dot(12, 10, RedLight);   // red candles
			(int x, int bit, int s)[] cs = { (3, 1, sL), (7, 2, sM), (12, 4, sR) };
			foreach (var (x, bit, s) in cs)
			{
				if ((lit & bit) != 0)
				{
					int fx = x + (bit == 2 ? fl % 2 : 0), tip = (fl + bit) % 2 == 0 ? 0 : (x < 8 ? -1 : 1);
					f.Dot(fx, 9, Flame).Dot(fx + tip, 8, Glow);
				}
				else if (s > 0) f.Dot(x + (s % 2), 9 - s, Line).Dot(x + 1 - (s % 2), 8 - s, Line);  // a wisp of smoke
				else if (s == 0) f.Dot(x, 9, Ink);                                                   // wick
			}
			if (spark) { Star(f, 3, 7); Star(f, 12, 7); f.Dot(7, 7, Glow).Dot(8, 6, Glow); }
			return f;
		}
		int cdL0 = k.F(Candelabra(7, 0, 1, 0, 1, 0, 0, 0)), cdL1 = k.F(Candelabra(7, 1, 1, 0, 1, 0, 0, 0));
		int cdO1 = k.F(Candelabra(6, 0, 4, 1, 1, 1, 0, 0)), cdO2 = k.F(Candelabra(6, 1, 4, 1, 1, 2, 0, 0)), cdO3 = k.F(Candelabra(6, 0, 4, 1, 1, 3, 0, 0));
		int cdP1 = k.F(Candelabra(2, 1, 4, 1, 1, -1, 0, 1)), cdP2 = k.F(Candelabra(2, 0, 4, 1, 1, -1, 0, 2)), cdP3 = k.F(Candelabra(2, 1, 4, 1, 1, -1, 0, 3));
		int cdQ1 = k.F(Candelabra(0, 0, 5, 1, 1, -1, 1, -1)), cdQ2 = k.F(Candelabra(0, 1, 5, 1, 1, -1, 2, -1)), cdQ3 = k.F(Candelabra(0, 0, 5, 1, 1, -1, 3, -1));
		int cdDk1 = k.F(Candelabra(0, 0, 5, 1, 1, -1, -1, -1, -1)), cdDk2 = k.F(Candelabra(0, 1, 5, 1, 1, -1, -1, -1, 1));
		int cdPuff = k.F(Candelabra(0, 1, 5, 1, 2, -1, -1, -1));
		int cdSp = k.F(Candelabra(7, 0, 4, 0, 1, 0, 0, 0, 0, true));
		int cdH0 = k.F(Candelabra(7, 0, 1, 3, 1, 0, 0, 0)), cdH1 = k.F(Candelabra(7, 1, 1, 3, 2, 0, 0, 0));
		k.Show(102, "candelabra", 130, (cdL0, 1), (cdL1, 1), (cdL0, 1), (cdL1, 1), (cdL0, 1), (cdL1, 1), (cdO1, 1), (cdO2, 1), (cdO3, 1), (cdP1, 1), (cdP2, 1), (cdP3, 1),
			(cdQ1, 1), (cdQ2, 1), (cdQ3, 1), (cdDk1, 1), (cdDk2, 1), (cdDk1, 1), (cdDk2, 1), (cdPuff, 1), (cdSp, 2), (cdH0, 1), (cdH1, 1), (cdH0, 1), (cdH1, 1), (k.Happy, 2));
		{
			var s = new Sfx(3.2, 2002);
			foreach (double at in new[] { 0.78, 1.17, 1.56 }) s.Noise(at, 0.18, 0.3, 1800, 600, 0.01, 0.12, 200);
			s.Noise(2.47, 0.15, 0.25, 2500, 4000, 0.02, 0.1, 400).Bell(2.6, 1568, 0.25, 0.3).Bell(2.7, 2093, 0.2, 0.35);
			k.Sound(102, s);
		}

		// ================================================================ 103 jack-o-lantern
		// A pumpkin sits in front of Boo; he pulls a face at it, it lights up and grins back, winks, and Boo laughs.
		Frame Jack(int face, bool aura, int bob)
		{
			var f = new Frame(); int x = 7, y = 9 + bob;
			string[] rows = { "...GG...", ".OOOOOO.", "OOOOOOOO", "OKKOOKKO", "OOOOOOOO", "OKOKKOKO", ".OOOOOO." };
			if (face >= 1) rows = rows.Select(r => r.Replace('K', 'y')).ToArray();
			if (face == 2) { rows[2] = "OOyOOyOO"; rows[3] = "OyOyyOyO"; rows[4] = "OyOOOOyO"; rows[5] = "OOyyyyOO"; }
			if (face == 3) { rows[3] = "OyyOOKKO"; rows[4] = "OyOOOOyO"; rows[5] = "OOyyyyOO"; }
			Put(f, x, y, rows);
			for (int j = 1; j <= 5; j++) foreach (int rx in new[] { 2, 5 }) if (f[x + rx, y + j] == Pumpkin) f[x + rx, y + j] = Flame;   // ribs
			if (aura) f.Dot(5, 11 + bob, Glow).Dot(15, 12 + bob, Glow).Dot(9, 7 + bob, Glow).Dot(14, 8 + bob, Glow).Dot(6, 14, Glow);
			return f;
		}
		int jkD = k.F(Jack(0, false, 0)), jkDu = k.F(Jack(0, false, -1)), jkL = k.F(Jack(1, false, 0)), jkLa = k.F(Jack(1, true, 0));
		int jkG = k.F(Jack(2, false, 0)), jkGa = k.F(Jack(2, true, 0)), jkGu = k.F(Jack(2, true, -1)), jkW = k.F(Jack(3, true, 0));
		int jkFace1 = k.F(G(1, 0, 2, 2)), jkFace2 = k.F(G(2, 1, 2, 2));
		int jkJump = k.F(G(2, 1, 4, 1, 1, 1));
		k.Show(103, "jack-o-lantern", 140, (k.LookL, 3), (jkFace1, 1), (jkFace2, 1), (jkFace1, 1), (k.LookL, 2), (jkJump, 1), (k.Surprise, 1), (k.Shiver1, 1), (k.Shiver2, 1),
			(k.Blink, 1), (k.LookL, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Wave, 1), (k.Wave2, 1), (k.Happy, 2));
		k.Child(103, 203, "imageX-imageW*0.75", "imageY");
		Part(203, "jack-o-lantern appears", 140, jkD, jkDu, jkD).Fade(0, 1).Then(100, 204);
		Part(204, "jack-o-lantern grins", 140, jkD, jkD, jkD, jkL, jkLa, jkGu, jkGa, jkG, jkGu, jkW, jkW, jkGa, jkLa, jkGu, jkLa).Then(100, 205);
		Part(205, "jack-o-lantern fades", 140, jkL, jkDu, jkL, jkD).Fade(1, 0);
		{
			var s = new Sfx(2.2, 2003);
			s.Noise(0.42, 0.25, 0.15, 800, 2500, 0.05, 0.15, 200);
			foreach (double at in new[] { 1.12, 1.32, 1.52 }) s.Tone(at, 0.15, 200, 165, 0.35, Sfx.Wave.Square, 0.01, 0.08, amHz: 30, amDepth: 0.5);
			k.Sound(103, s.Soften(1400));
		}

		// ================================================================ 104 costume party
		// Quick spins between costumes: a witch hat, cat ears, a vampire collar, then confetti.
		Frame Costume(int c, int b)
		{
			int L = b == 0 ? 1 : 0;
			var f = G(L, b, c == 2 ? 0 : 1, c == 1 ? 3 : 0, c == 0 && b == 0 ? 1 : 0, c == 2 && b == 1 ? 1 : 0);
			if (c == 0)
			{
				f.Rect(3, -L, 10, 1, Purple).Rect(5, -L - 1, 6, 1, PurpleLight).Dot(8, -L - 1, Glow).Rect(6, -L - 2, 4, 1, Purple).Rect(7, -L - 3, 2, 1, Purple);
				if (L == 0) f.Dot(6, -4, Purple);
			}
			if (c == 1)
			{
				f.Dot(4, -L, Ink).Dot(5, -L, Blush).Dot(4, -L - 1, Ink).Dot(11, -L, Ink).Dot(10, -L, Blush).Dot(11, -L - 1, Ink).Dot(5, -L - 1, Ink).Dot(10, -L - 1, Ink);
				f.Dot(2, 6 - L, Ink).Dot(2, 8 - L, Ink).Dot(13, 6 - L, Ink).Dot(13, 8 - L, Ink).Dot(8, 6 - L, Heart);
			}
			if (c == 2)
			{
				Put(f, 0, -L - 2, "R..", "RR.", "RRR");
				Put(f, 13, -L - 2, "..R", ".RR", "RRR");
				for (int y = 2 - L; y <= 12 - L; y++) for (int x = 0; x < 16; x++) if ((x <= 2 || x >= 13) && f[x, y] == Line) f[x, y] = Red;
				f.Dot(6, 1 - L, Ink).Dot(7, 1 - L, Ink).Dot(8, 1 - L, Ink).Dot(9, 1 - L, Ink).Dot(7, 2 - L, Ink).Dot(8, 2 - L, Ink);
			}
			return f;
		}
		Frame Confetti(int b)
		{
			var f = G(b == 0 ? 1 : 2, b, 1, 3, 1, 1);
			char[] cols = { Heart, Glow, Mint, Tear, PurpleLight };
			(int x, int y)[] pts = b == 0 ? new[] { (2, -3), (6, -4), (11, -2), (14, -4), (1, 2), (13, 3), (4, 0) } : new[] { (3, -1), (8, -3), (12, 0), (0, -2), (15, 1), (10, -4), (5, -3) };
			for (int i = 0; i < pts.Length; i++) if (f[pts[i].x, pts[i].y] == '.') f[pts[i].x, pts[i].y] = cols[i % cols.Length];
			return f;
		}
		var cos = new int[3, 2];
		for (int c = 0; c < 3; c++) for (int b = 0; b < 2; b++) cos[c, b] = k.F(Costume(c, b));
		int cf0 = k.F(Confetti(0)), cf1 = k.F(Confetti(1));
		k.Show(104, "costume party", 120, (k.Stand, 1),
			(k.SideL, 1), (k.Back, 1), (k.SideR, 1), (cos[0, 0], 2), (cos[0, 1], 2), (cos[0, 0], 2),
			(k.SideL, 1), (k.Back, 1), (k.SideR, 1), (cos[1, 0], 2), (cos[1, 1], 2), (cos[1, 0], 2),
			(k.SideL, 1), (k.Back, 1), (k.SideR, 1), (cos[2, 0], 2), (cos[2, 1], 2), (cos[2, 0], 2),
			(k.SideL, 1), (k.Back, 1), (k.SideR, 1), (cf0, 2), (cf1, 2), (cf0, 2), (k.Happy, 2));
		{
			var s = new Sfx(4.4, 2004);
			foreach (double at in new[] { 0.12, 1.2, 2.28, 3.36 }) s.Noise(at, 0.3, 0.2, 1500, 5000, 0.05, 0.2, 500);
			s.Tone(3.72, 0.4, 500, 560, 0.25, Sfx.Wave.Saw, 0.02, 0.1, vibHz: 12, vibDepth: 0.02).Pop(3.75, 0.3, 900).Pop(3.86, 0.3, 1200).Pop(3.98, 0.3, 1000);
			k.Sound(104, s.Soften(3500));
		}

		// ================================================================ 105 trick or treat
		// Boo holds a treat bag open; three wrapped candies drop from above into it. He peeks in, delighted.
		void Bag(Frame f, int fill)
		{
			Put(f, 0, 9, "UVVVU", "UUUUU", "UUyUU", "UUUUU", fill >= 2 ? "UUUUU" : ".UUU.");
			if (fill >= 1) f.Dot(1, 8, Red);
			if (fill >= 2) f.Dot(3, 8, Mint);
			if (fill >= 3) f.Dot(2, 7, Gum2).Dot(2, 8, GumC);
		}
		Frame TT(int fill, int mode, int b)
		{
			Frame f = mode switch
			{
				0 => Up(1 + b, b, 1, 0, 0, -1),
				1 => G(2, 1, 1, 0),
				2 => G(1, b, 2, 0),
				_ => G(1 + b, b, 1, 0),
			};
			if (mode == 3) HeartAt(f, 11, -3 + b);
			Bag(f, fill);
			return f;
		}
		Frame Candy(int type, int y, bool vert)
		{
			var f = new Frame();
			char mid = type == 0 ? Red : type == 1 ? Mint : Gum2, end = type == 0 ? RedLight : type == 1 ? GlowLight : GumC;
			if (vert) f.Dot(2, y, end).Dot(2, y + 1, mid).Dot(2, y + 2, end);
			else f.Dot(1, y + 1, end).Dot(2, y + 1, mid).Dot(3, y + 1, end);
			return f;
		}
		var tt = new int[4, 4, 2];
		for (int fl = 0; fl < 4; fl++) for (int m = 0; m < 4; m++) for (int b = 0; b < 2; b++) tt[fl, m, b] = k.F(TT(fl, m, b));
		var cnd = new int[3, 4];
		int[] candyY = { -4, -1, 2, 5 };
		for (int t = 0; t < 3; t++) for (int i = 0; i < 4; i++) cnd[t, i] = k.F(Candy(t, candyY[i], (i + t) % 2 == 1));
		k.Show(105, "trick or treat", 130, (tt[0, 0, 0], 1), (tt[0, 0, 1], 1), (tt[0, 0, 0], 1), (tt[0, 0, 1], 1), (tt[0, 0, 0], 1), (tt[0, 0, 1], 1), (tt[1, 1, 0], 1),
			(tt[1, 0, 0], 1), (tt[1, 0, 1], 1), (tt[1, 0, 0], 1), (tt[1, 0, 1], 1), (tt[2, 1, 0], 1),
			(tt[2, 0, 0], 1), (tt[2, 0, 1], 1), (tt[2, 0, 0], 1), (tt[2, 0, 1], 1), (tt[3, 1, 0], 1),
			(tt[3, 2, 0], 2), (tt[3, 2, 1], 2), (tt[3, 3, 0], 1), (tt[3, 3, 1], 1), (tt[3, 3, 0], 1), (tt[3, 3, 1], 1), (k.Happy, 2));
		k.Child(105, 206);
		Part(206, "candies fall", 130, blank, blank, cnd[0, 0], cnd[0, 1], cnd[0, 2], cnd[0, 3], blank, cnd[1, 0], cnd[1, 1], cnd[1, 2], cnd[1, 3], blank,
			cnd[2, 0], cnd[2, 1], cnd[2, 2], cnd[2, 3]);
		{
			var s = new Sfx(2.5, 2005);
			s.Pop(0.78, 0.5, 900).Pop(1.43, 0.5, 1100).Pop(2.08, 0.5, 1300).Bell(2.1, 2093, 0.12, 0.2);
			k.Sound(105, s);
		}

		// ================================================================ 106 creaky door
		// A door appears and creaks open by itself; two glowing eyes blink in the dark. Boo says BOO and the door slams.
		Frame Door(int w, int eyes, int shake)
		{
			var f = new Frame();
			f.Rect(6, 1, 8, 15, Line).Rect(7, 2, 6, 14, Ink);              // frame and the dark doorway
			if (eyes == 1) f.Dot(8, 7, Glow).Dot(10, 7, Glow);
			if (eyes == 2) f.Dot(8, 8, Line).Dot(10, 8, Line);
			if (eyes == 3) f.Rect(8, 6, 1, 2, Glow).Rect(10, 6, 1, 2, Glow);
			for (int x = 13 - w; x <= 12; x++) for (int y = 2; y <= 15; y++) f[x, y] = Cone;
			if (w >= 4) for (int y = 2; y <= 15; y++) f[13 - w + w / 2, y] = Flame;
			if (w >= 3) f.Dot(13 - w + 1, 9, Glow);
			if (w <= 2) for (int y = 2; y <= 15; y++) f[12, y] = Flame;
			if (shake != 0) f.Shift(shake, 0);
			return f;
		}
		int dr6 = k.F(Door(6, 0, 0)), dr6r = k.F(Door(6, 0, 1)), dr6l = k.F(Door(6, 0, -1)), dr5 = k.F(Door(5, 0, 0)), dr4 = k.F(Door(4, 0, 0)), dr3 = k.F(Door(3, 0, 0));
		int dr2 = k.F(Door(2, 0, 0)), dr2e = k.F(Door(2, 1, 0)), dr2b = k.F(Door(2, 2, 0)), dr2w = k.F(Door(2, 3, 0));
		k.Show(106, "creaky door", 120, (k.LookL, 3), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1),
			(k.Surprise, 5), (k.BooPrep, 3), (k.Boo, 4), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 2));
		k.Child(106, 207, "imageX-imageW*0.95", "imageY");
		Part(207, "door appears", 120, dr6, dr6r, dr6).Fade(0, 1).Then(100, 208);
		Part(208, "door creaks open", 120, dr5, dr5, dr4, dr4, dr3, dr2, dr2, dr2e, dr2e, dr2b, dr2e, dr2w, dr2w, dr2w, dr2, dr6, dr6r, dr6l).Then(100, 209);
		Part(209, "door fades", 120, dr6, dr6r, dr6, dr6l, dr6, dr6).Fade(1, 0);
		{
			var s = new Sfx(2.6, 2006);
			s.Tone(0.36, 1.1, 260, 390, 0.35, Sfx.Wave.Saw, 0.1, 0.3, amHz: 28, amDepth: 0.75, harmonic: 0.4);
			s.Thud(2.16, 0.6, 160, 50, 0.25).Click(2.16, 0.4);
			k.Sound(106, s.Soften(2500));
		}

		// ================================================================ 107 spider on a thread
		// A little spider drops in front of Boo's face, dangles, waves a leg, and climbs back up.
		Frame Spider(int sy, int pose)
		{
			var f = new Frame(); int sx = 5;
			for (int y = -4; y < sy; y++) f.Dot(sx + 3, y, Line);
			Put(f, sx, sy, "K.KKK.K", ".KKKKK.", "K.WKW.K", ".K...K.");
			if (pose == 1) f.Dot(sx, sy, '.').Dot(sx, sy - 1, Ink).Dot(sx + 1, sy - 2, Ink);
			if (pose == 2) f.Dot(sx, sy, '.').Dot(sx - 1, sy - 1, Ink).Dot(sx, sy - 2, Ink);
			return f;
		}
		int Sp(int y, int pose = 0) => k.F(Spider(y, pose));
		int spM4 = Sp(-4), spM2 = Sp(-2), sp1 = Sp(1), sp3 = Sp(3), sp4 = Sp(4), sp5 = Sp(5), spW1 = Sp(4, 1), spW2 = Sp(4, 2), spM1 = Sp(-1), spM3 = Sp(-3);
		var cross = G(1, 0, 7, 1); cross.Rect(5, 3, 2, 3, Ink).Rect(9, 3, 2, 3, Ink);   // cross-eyed at the spider
		int spCross = k.F(cross);
		k.Show(107, "spider", 130, (k.Stand, 1), (k.Blink, 1), (k.Stand, 2), (spCross, 2), (k.Surprise, 2), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1),
			(k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.F(Up(1, 0, 0)), 2), (k.F(Up(2, 1, 0)), 2), (k.Happy, 2));
		k.Child(107, 210, "imageX-imageW*0.35", "imageY");
		Part(210, "spider drops", 130, spM4, spM2, sp1, sp3).Fade(0, 1).Then(100, 211);
		Part(211, "spider dangles", 130, sp4, sp5, sp4, sp5, spW1, spW2, spW1, spW2, sp5, sp4).Then(100, 212);
		Part(212, "spider climbs", 130, sp3, sp1, spM1, spM3, spM4, spM4).Fade(1, 0);

		// ================================================================ 108 cauldron
		// Boo stirs a bubbling cauldron over a little fire; green bubbles float up and one pops right on his nose.
		void Cauldron(Frame f, int t)
		{
			f.Rect(3, 9, 10, 1, Mint);
			int bx = 4 + (t * 3) % 8; f.Dot(bx, 8, GlowCyan).Dot(bx, 9, GlowLight).Dot(12 - (t * 5) % 8, 9, GlowCyan);
			f.Rect(2, 10, 12, 3, Ink).Rect(3, 10, 3, 1, Line).Rect(3, 13, 10, 1, Ink).Rect(4, 14, 8, 1, Ink);
			for (int x = 2; x <= 13; x++) f[x, 15] = '.';
			f.Dot(3, 15, Ink).Dot(12, 15, Ink).Dot(5 + t % 2, 15, Flame).Dot(7, 15, Glow).Dot(8, 15, Flame).Dot(10 - t % 2, 15, Glow);
		}
		Frame Stir(int t, int eyes, int mouth, int lift = 1)
		{
			var f = G(lift, t % 2, eyes, mouth); int[] top = { 3, 5, 7, 5 };
			Cauldron(f, t);
			int tx = top[t % 4];
			Seg(f, tx, 6, tx + 2, 9, Cone);
			return f;
		}
		var stir = new int[8]; for (int t = 0; t < 8; t++) stir[t] = k.F(Stir(t, 1, 3));
		int stSur = k.F(Stir(1, 4, 1, 2)), stSur2 = k.F(Stir(2, 4, 1)), stG1 = k.F(Stir(3, 1, 3, 2)), stG2 = k.F(Stir(0, 1, 3));
		void Ring(Frame f, int x, int y, int r)
		{
			if (r == 0) f.Dot(x, y, Mint);
			else if (r == 1) { f.Dot(x, y - 1, Mint).Dot(x - 1, y, Mint).Dot(x + 1, y, Mint).Dot(x, y + 1, Mint).Dot(x, y, '.'); f.Dot(x - 1, y - 1, GlowLight); }
			else { Put(f, x - 2, y - 2, ".iii.", "i...i", "i...i", "i...i", ".iii."); f.Dot(x - 1, y - 1, GlowLight); }
		}
		int Bub(int x, int y, int r) { var f = new Frame(); Ring(f, x, y, r); return k.F(f); }
		int BubPop(int x, int y) { var f = new Frame(); f.Dot(x - 2, y, Mint).Dot(x + 2, y, Mint).Dot(x, y - 2, Mint).Dot(x - 1, y + 2, GlowCyan).Dot(x + 2, y - 2, GlowCyan); return k.F(f); }
		k.Show(108, "cauldron", 120, (stir[0], 1), (stir[1], 1), (stir[2], 1), (stir[3], 1), (stir[4], 1), (stir[5], 1), (stir[6], 1), (stir[7], 1),
			(stir[0], 1), (stir[1], 1), (stir[2], 1), (stir[3], 1), (stir[4], 1), (stir[5], 1), (stir[6], 1),
			(stSur, 2), (stSur2, 1), (stG1, 1), (stG2, 1), (stG1, 1), (stG2, 1), (stir[0], 1), (stir[1], 1), (stir[2], 1), (stir[3], 1), (k.Happy, 2));
		k.Child(108, 213);
		k.Child(108, 214);
		Part(213, "brew bubble", 120, blank, blank, Bub(9, 8, 0), Bub(8, 6, 1), Bub(8, 4, 1), Bub(9, 2, 1), Bub(8, 0, 1), Bub(8, -2, 2), BubPop(8, -2));
		Part(214, "brew bubble pops", 120, blank, blank, blank, blank, blank, blank, blank, blank, blank,
			Bub(11, 8, 0), Bub(10, 6, 1), Bub(9, 5, 1), Bub(8, 4, 1), Bub(6, 3, 2), Bub(5, 3, 2), BubPop(5, 3));
		{
			var s = new Sfx(3.4, 2008); var r = new Random(2008);
			for (int i = 0; i < 14; i++) s.Pop(0.1 + i * 0.22 + r.NextDouble() * 0.08, 0.18 + r.NextDouble() * 0.1, 250 + r.Next(300));
			s.Pop(1.92, 0.55, 900);
			k.Sound(108, s.Soften(3000));
		}

		// ================================================================ 109 skeleton friend
		// A cheerful little skeleton rises from the ground next to Boo and they dance together.
		void Skel(Frame f, int cx, int top, int armL, int armR, int legL, int legR)
		{
			Put(f, cx - 2, top, ".ooo.", "oWWWo", "oKWKo", "oWWWo", ".WsW.");
			f.Dot(cx, top + 5, Shade).Rect(cx - 2, top + 6, 5, 1, Sheet).Dot(cx, top + 7, Shade).Rect(cx - 1, top + 8, 3, 1, Sheet).Dot(cx, top + 9, Shade).Rect(cx - 1, top + 10, 3, 1, Sheet);
			foreach (var (side, a) in new[] { (-1, armL), (1, armR) })
			{
				int sx = cx + 3 * side, sy = top + 6;
				var hand = a switch { 0 => (sx, sy + 3), 1 => (sx + side, sy - 1), _ => (sx, sy - 3) };
				Seg(f, sx, sy, hand.Item1, hand.Item2, Shade); f.Dot(hand.Item1, hand.Item2, Sheet);
			}
			foreach (var (side, l) in new[] { (-1, legL), (1, legR) })
			{
				int hx = cx + side, hy = top + 11;
				var foot = l == 0 ? (cx + 2 * side, 15) : (cx + 4 * side, 13);
				Seg(f, hx, hy, foot.Item1, foot.Item2, Shade); f.Dot(foot.Item1, foot.Item2, Sheet).Dot(foot.Item1 + side, foot.Item2, Sheet);
			}
		}
		Frame SkelF(int top, int aL, int aR, int lL, int lR, int up = 0)
		{
			var f = new Frame(); Skel(f, 8, top + up, aL, aR, lL, lR);
			if (up > 0) { Cut(f, 15); f.Dot(4, 15, Cone).Dot(6, 15, Cone).Dot(10, 15, Cone).Dot(12, 15, Cone).Dot(5, 14, Cone).Dot(11, 14, Cone); }
			return f;
		}
		int skR1 = k.F(SkelF(1, 2, 2, 0, 0, 10)), skR2 = k.F(SkelF(1, 2, 2, 0, 0, 6)), skR3 = k.F(SkelF(1, 1, 1, 0, 0, 2));
		int skP0 = k.F(SkelF(1, 2, 2, 0, 0)), skP1 = k.F(SkelF(1, 1, 1, 1, 0)), skP2 = k.F(SkelF(2, 2, 0, 0, 0)), skP3 = k.F(SkelF(1, 1, 1, 0, 1));
		int skBow = k.F(SkelF(3, 0, 0, 0, 0)), skBow2 = k.F(SkelF(2, 0, 0, 0, 0)), skWv1 = k.F(SkelF(1, 0, 2, 0, 0)), skWv2 = k.F(SkelF(1, 0, 1, 0, 0));
		var bd0 = G(1, 0, 1, 1, 1, 1); Lean(bd0, -1, 6); int skB0 = k.F(bd0);
		var bd1 = G(2, 1, 1, 3, 1, 1); Lean(bd1, 1, 6); int skB1 = k.F(bd1);
		k.Show(109, "skeleton friend", 140, (k.LookL, 3), (k.Surprise, 2), (skB0, 2), (skB1, 2), (skB0, 2), (skB1, 2), (skB0, 2), (skB1, 2), (skB0, 1), (skB1, 1),
			(k.Happy, 2), (k.Wave, 1), (k.Wave2, 1), (k.Wave, 1), (k.Happy, 1));
		k.Child(109, 215, "imageX-imageW", "imageY");
		Part(215, "skeleton rises", 140, skR1, skR2, skR3).Fade(0, 1).Then(100, 216);
		Part(216, "skeleton dances", 140, Rep(4, skP0, skP1, skP2, skP3)).Then(100, 217);
		Part(217, "skeleton bows", 140, skBow, skBow2, skWv1, skWv2, skWv1, skWv2).Fade(1, 0);
		{
			var s = new Sfx(3.2, 2009);
			s.Noise(0.0, 0.4, 0.2, 500, 250, 0.02, 0.2, 60);
			int[] mel = { 69, 72, 76, 72, 74, 77, 81, 77, 76, 72, 69, 72, 74, 71, 68, 69 };
			for (int i = 0; i < mel.Length; i++) { double at = 0.42 + i * 0.14; s.Bell(at, Sfx.Midi(mel[i] + 12), 0.25, 0.09).Click(at, 0.15); }
			k.Sound(109, s);
		}

		// ================================================================ 110 moon howl
		// A full moon rises behind Boo; he takes a deep breath and howls at it like a little wolf.
		Frame Moon(int cloudX, bool halo)
		{
			var f = new Frame(); FillCircle(f, 8, 6, 5.2f, Glow, GlowLight);
			f.Dot(6, 7, Cone).Dot(10, 4, Cone).Dot(9, 9, Cone).Dot(11, 7, Cone);
			if (halo) foreach (var (x, y) in new[] { (1, 6), (15, 6), (8, -1), (8, 13), (3, 1), (13, 1), (3, 11), (13, 11) }) f.Dot(x, y, GlowLight);
			if (cloudX > -10) Put(f, cloudX, 8, ".sss..", "ssssss", ".oooo.");
			return f;
		}
		int mn0 = k.F(Moon(-99, false)), mn1 = k.F(Moon(-99, true));
		var mnC = new int[12]; for (int i = 0; i < 12; i++) mnC[i] = k.F(Moon(15 - 2 * i, i % 2 == 0));
		int howlUp1 = k.F(Up(1, 0, 0, 0, 0, -1)), howlUp2 = k.F(Up(2, 1, 0, 0, 0, -1));
		int howlIn = k.F(G(2, 1, 5, 1));
		Frame Howl(int b)
		{
			var f = G(1 + b, b, 1, 1); Lean(f, -1, 6);
			Seg(f, 1, -2 + b, 0, 0 + b, Line); Seg(f, 3, -4 + b, 2, -2 + b, Line);
			if (b == 1) f.Dot(0, -3, Line);
			return f;
		}
		int hw0 = k.F(Howl(0)), hw1 = k.F(Howl(1));
		k.Show(110, "moon howl", 130, (howlUp1, 1), (howlUp2, 1), (howlUp1, 1), (howlUp2, 1), (howlUp1, 1), (howlUp2, 1), (howlIn, 3),
			(hw0, 2), (hw1, 2), (hw0, 2), (hw1, 2), (hw0, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Happy, 2));
		k.Child(110, 218, "imageX-imageW*0.6", "imageY-imageH*1.05");
		Part(218, "moon rises", 130, mn0, mn1, mn0, mn1, mn0, mn1).Move("0", "-1").Fade(0, 1).Then(100, 219).OnBorder(100, 299);
		Part(219, "moon glows", 130, mnC).Then(100, 220);
		Part(220, "moon sets", 130, mn1, mn0, mn1, mn0, mn1, mn0).Move("0", "1").Fade(1, 0).OnBorder(100, 299);
		{
			var s = new Sfx(3.0, 2010);
			s.Tone(1.17, 0.5, 420, 760, 0.4, Sfx.Wave.Sine, 0.08, 0.05, vibHz: 6, vibDepth: 0.03, harmonic: 0.3);
			s.Tone(1.67, 0.9, 760, 500, 0.4, Sfx.Wave.Sine, 0.02, 0.4, vibHz: 6, vibDepth: 0.04, harmonic: 0.3);
			k.Sound(110, s);
		}

		// ================================================================ 111 mouse scare
		// A mouse scurries up; Boo says BOO; the mouse rears up and squeaks so loud that Boo is the one who jumps.
		string[] mouseA = { ".......P..", "....ooooo.", "...oooooKo", "PPoooooooh", "....o..o.." };
		string[] mouseB = { ".......P..", "....ooooo.", "PP.oooooKo", "..oooooooh", ".....oo..." };
		string[] mouseSn = { ".......P..", "....ooooo.", "...oooooKh", "PPoooooooo", "....o..o.." };
		string[] mouseRear = { ".......P..", "......oo..", ".....oKoh.", ".....ooo..", "....oooo..", "PP..oooo..", "..PPo..o.." };
		Frame Mouse(string[] rows, int y, int dx = 0, int alarm = 0)
		{
			var f = new Frame(); Put(f, 6 + dx, y, rows);
			if (alarm >= 1) f.Dot(3, 8, Ink).Dot(3, 9, Ink).Dot(3, 11, Ink).Dot(14, 11, Heart);
			if (alarm >= 2) f.Dot(1, 7, Ink).Dot(1, 8, Ink).Dot(1, 10, Ink).Dot(4, 6, Line).Dot(13, 7, Line);
			return f;
		}
		int msA = k.F(Mouse(mouseA, 11)), msB = k.F(Mouse(mouseB, 11)), msSn = k.F(Mouse(mouseSn, 11));
		int msTr = k.F(Mouse(mouseA, 12)), msTrR = k.F(Mouse(mouseA, 12, 1)), msRear = k.F(Mouse(mouseRear, 9));
		int msR1 = k.F(Mouse(mouseRear, 9, 0, 1)), msR2 = k.F(Mouse(mouseRear, 9, 0, 2));
		int msMA = k.F(Mouse(mouseA, 11).MirrorX()), msMB = k.F(Mouse(mouseB, 11).MirrorX());
		int mGrin = k.F(G(1, 0, 2, 3));
		var jumpy = G(2, 1, 4, 1, 1, 1); jumpy.Dot(14, -3, Ink).Dot(14, -2, Ink).Dot(14, 0, Ink); int mJump = k.F(jumpy);
		k.Show(111, "mouse scare", 100, (k.LookL, 8), (mGrin, 3), (k.BooPrep, 3), (k.Boo, 5), (mJump, 2), (k.Surprise, 2),
			(k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1),
			(k.LookL, 4), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 2));
		k.Child(111, 221, "imageX-imageW*1.6", "imageY");
		Part(221, "mouse runs in", 100, Rep(4, msA, msB)).Move("4", "0").Then(100, 222).OnBorder(100, 299);
		Part(222, "mouse sniffs", 100, msA, msSn, msA, msSn, msA, msSn, msA, msTr, msTrR, msTr, msRear).Then(100, 223);
		Part(223, "mouse squeaks", 100, msR1, msR2, msR1, msR2, msR1, msR2).Then(100, 224);
		Part(224, "mouse runs off", 100, Rep(6, msMA, msMB)).Move("-7", "0").Fade(1, 0.2).OnBorder(100, 299);
		{
			var s = new Sfx(2.6, 2011);
			s.Tone(1.4, 0.4, 300, 200, 0.35, Sfx.Wave.Sine, 0.03, 0.15, vibHz: 7, vibDepth: 0.05, harmonic: 0.3);
			s.Tone(1.9, 0.12, 2200, 2800, 0.3, Sfx.Wave.Square, 0.005, 0.04).Tone(2.08, 0.16, 2400, 3100, 0.3, Sfx.Wave.Square, 0.005, 0.05);
			k.Sound(111, s.Soften(4500));
		}

		// ================================================================ 112 spooky wail
		// Arms up, hollow eyes, the sheet ripples like a flag: "wooOOooo"... then he breaks into giggles.
		Frame Wail(int ph)
		{
			int lift = ph % 2 == 0 ? 1 : 2;
			var src = G(lift, ph % 2, 7, 4, 1, 1); int y0 = 5 - lift;
			src.Rect(4, y0 - 2, 2, 4, Ink).Rect(10, y0 - 2, 2, 4, Ink).Rect(7, y0 + 2, 2, 3, Ink);
			var f = new Frame();
			for (int y = f.MinY; y <= 15; y++)
			{
				int dx = y >= 15 || y < 10 - lift ? 0 : (int)Math.Round(Math.Sin(y * 0.6 + ph * 1.6));
				for (int x = 0; x < 16; x++) if (src[x, y] != '.') f[x + dx, y] = src[x, y];
			}
			foreach (var (x, y) in ph % 2 == 0 ? new[] { (1, -3), (14, -1), (0, 12) } : new[] { (2, -4), (15, -3), (15, 12) }) if (f[x, y] == '.') f[x, y] = Zz;
			return f;
		}
		var wl = new int[4]; for (int i = 0; i < 4; i++) wl[i] = k.F(Wail(i));
		k.Show(112, "spooky wail", 120, (k.Stand, 1), (k.Surprise, 2), (wl[0], 1), (wl[1], 1), (wl[2], 1), (wl[3], 1), (wl[0], 1), (wl[1], 1), (wl[2], 1), (wl[3], 1),
			(wl[0], 1), (wl[1], 1), (wl[2], 1), (wl[3], 1), (wl[0], 1), (wl[1], 1), (wl[2], 1), (wl[3], 1),
			(k.Giggle1, 2), (k.Giggle2, 2), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 2));
		{
			var s = new Sfx(2.6, 2012);
			s.Tone(0.36, 0.9, 300, 480, 0.4, Sfx.Wave.Sine, 0.15, 0.1, vibHz: 5, vibDepth: 0.04, harmonic: 0.25);
			s.Tone(1.26, 1.0, 480, 280, 0.4, Sfx.Wave.Sine, 0.05, 0.4, vibHz: 5, vibDepth: 0.05, harmonic: 0.25);
			s.Tone(0.36, 1.9, 150, 140, 0.12, Sfx.Wave.Triangle, 0.3, 0.5);
			k.Sound(112, s);
		}

		// ================================================================ 113 bat perch
		// A little bat flutters in and lands on Boo's head, flaps a bit, then flies off while Boo waves.
		string[] batUp = { "U.K.K.U", "UUKKKUU", ".UyKyU.", "...K..." };
		string[] batDn = { "..K.K..", ".UKKKU.", "UUyKyUU", "U..K..U" };
		Frame Bat(int x, int y, bool up) { var f = new Frame(); Put(f, x, y, up ? batUp : batDn); return f; }
		(int x, int y)[] batPath = { (0, -4), (2, -3), (4, -1), (6, 1), (7, 1), (8, 0) };
		var batIn = new int[6]; for (int i = 0; i < 6; i++) batIn[i] = k.F(Bat(batPath[i].x, batPath[i].y, i % 2 == 0));
		Frame Perch(bool flap, int mouth)
		{
			var f = Up(1, flap ? 1 : 0, mouth);
			if (flap) Put(f, 4, -4, "..K.K..", "UUKKKUU", "U.yKy.U");
			else Put(f, 5, -4, "K...K", "UKKKU", "UyKyU");
			return f;
		}
		int pc0 = k.F(Perch(false, 0)), pc1 = k.F(Perch(true, 3)), pc2 = k.F(Perch(false, 3));
		int batU = k.F(Bat(4, -4, true)), batD = k.F(Bat(4, -4, false));
		int upL = k.F(Up(1, 0, 0, 0, 0, -1));
		k.Lead(113, "bat perch", 120, 225, (k.Stand, 1), (upL, 5), (pc0, 2), (pc1, 2), (pc2, 2), (pc1, 2), (pc0, 2));
		k.Show(225, "bat takes off", 120, (k.F(Up(2, 1, 1)), 2), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (upL, 2), (k.Happy, 2));
		k.Child(113, 226, "imageX-imageW*0.25", "imageY-imageH*0.2");
		Part(226, "bat flies in", 120, batIn).Fade(0, 1);
		k.Child(225, 227);
		Part(227, "bat flies off", 120, Rep(8, batU, batD)).Move("-3", "-4").Fade(1, 0).OnBorder(100, 299);

		// ================================================================ 114 tombstone
		// A little tombstone pops out of the ground; Boo sinks behind it, peeks over the top, then jumps out: BOO!
		void Stone(Frame f, int down)
		{
			var s = new Frame();
			Put(s, 3, 8, "..oooooo..", ".oqqqqqqo.", "oqqqqoqqqo", "oqqqoooqqo", "oqqqqoqqqo", "oqqqqqqqqo", "oqoqqqqqqo", "oooooooooo");
			s.Dot(2, 15, Stem).Dot(13, 15, Stem).Dot(2, 14, Stem).Dot(14, 15, Stem);
			if (down > 0) { s.Shift(0, down); Cut(s, 16); }
			for (int y = s.MinY; y <= 15; y++) for (int x = 0; x < 16; x++) if (s[x, y] != '.') f[x, y] = s[x, y];
		}
		Frame Behind(int d, int eyes, int mouth, int lift, int stoneDown, int la = 0, int ra = 0, int hem = 0)
		{
			var f = G(lift, hem, eyes, mouth, la, ra, d == 0);
			if (d > 0) { f.Shift(0, d); Cut(f, 15); }
			Stone(f, stoneDown);
			return f;
		}
		int ts1 = k.F(Behind(0, 4, 1, 1, 6, 1, 1)), ts2 = k.F(Behind(0, 4, 1, 1, 3, 1, 1)), ts3 = k.F(Behind(0, 0, 0, 1, 0));
		int tsS3 = k.F(Behind(3, 0, 0, 1, 0, 0, 0, 1)), tsS6 = k.F(Behind(6, 0, 0, 1, 0)), tsS9 = k.F(Behind(9, 1, 0, 1, 0, 0, 0, 1));
		int tsH1 = k.F(Behind(11, 0, 0, 1, 0)), tsH2 = k.F(Behind(12, 0, 0, 1, 0, 0, 0, 1));
		int tsPL = k.F(Behind(2, 2, 0, 1, 0)), tsPR = k.F(Behind(2, 3, 0, 1, 0, 0, 0, 1));
		var tsb1 = Behind(0, 5, 1, 2, 0, 1, 1); int tsBp = k.F(tsb1);
		var tsb2 = Behind(0, 4, 2, 2, 0, 1, 1, 1); tsb2.Dot(0, -2, Ink).Dot(15, -2, Ink).Dot(1, -1, Ink).Dot(14, -1, Ink); int tsBoo = k.F(tsb2);
		int tsG1 = k.F(Behind(0, 1, 3, 1, 0)), tsG2 = k.F(Behind(0, 1, 3, 2, 0, 0, 0, 1));
		int tsD1 = k.F(Behind(0, 1, 0, 1, 3)), tsD2 = k.F(Behind(0, 1, 0, 1, 6)), tsD3 = k.F(Behind(0, 1, 0, 1, 9));
		k.Show(114, "tombstone", 120, (k.Stand, 1), (ts1, 1), (ts2, 1), (ts3, 1), (tsS3, 1), (tsS6, 1), (tsS9, 1), (tsH1, 1), (tsH2, 1), (tsH1, 1), (tsH2, 1),
			(tsPL, 2), (tsPR, 2), (tsS6, 1), (tsH1, 1), (tsH2, 1), (tsBp, 1), (tsBoo, 4), (tsG1, 1), (tsG2, 1), (tsG1, 1), (tsG2, 1), (tsD1, 1), (tsD2, 1), (tsD3, 1), (k.Happy, 2));
		{
			var s = new Sfx(3.4, 2014);
			s.Noise(0.12, 0.35, 0.3, 400, 200, 0.02, 0.2, 40).Thud(0.36, 0.4, 120, 40);
			s.Tone(1.92, 0.4, 280, 190, 0.4, Sfx.Wave.Sine, 0.02, 0.2, vibHz: 7, vibDepth: 0.05, harmonic: 0.35);
			s.Noise(2.76, 0.35, 0.25, 400, 200, 0.02, 0.2, 40);
			k.Sound(114, s);
		}

		// ================================================================ 115 haunted broom
		// A broom sweeps past all by itself, raising little puffs of dust. Boo watches it go and waves.
		Frame Broom(int t)
		{
			var f = new Frame(); int sw = t - 1;
			Seg(f, 11 + sw, -3, 8, 9, Cone);
			f.Rect(6, 10, 4, 1, Flame);
			int[] x0 = { 6, 5, 5, 4, 4 }, w = { 4, 6, 6, 8, 8 };
			for (int j = 0; j < 5; j++)
			{
				int dx = j >= 3 ? -sw : 0;
				f.Rect(x0[j] + dx, 11 + j, w[j], 1, Glow);
				if (j >= 1) f.Dot(x0[j] + dx + 2, 11 + j, Cone).Dot(x0[j] + dx + w[j] - 3, 11 + j, Cone);
			}
			foreach (var (x, y) in t == 0 ? new[] { (2, 14), (1, 12) } : t == 1 ? new[] { (1, 15), (0, 13), (2, 12) } : new[] { (0, 14), (2, 13) }) f.Dot(x, y, Shade);
			return f;
		}
		int bm0 = k.F(Broom(0)), bm1 = k.F(Broom(1)), bm2 = k.F(Broom(2));
		k.Show(115, "haunted broom", 110, (k.Stand, 1), (k.Surprise, 3), (k.LookL, 4), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1), (k.LookL, 3),
			(k.Giggle1, 2), (k.Giggle2, 2), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Happy, 2));
		k.Child(115, 228, "imageX-imageW*0.7", "imageY");
		Part(228, "broom sweeps", 110, bm0, bm1, bm2, bm1).Loop("5", 0).Move("-3", "0").Then(100, 229).OnBorder(100, 299);
		Part(229, "broom leaves", 110, bm0, bm1, bm2, bm1, bm0, bm1).Move("-3", "0").Fade(1, 0).OnBorder(100, 299);
		{
			var s = new Sfx(3.0, 2015);
			for (int i = 0; i < 12; i++) s.Noise(0.05 + i * 0.22, 0.14, 0.22, 6000, 2500, 0.03, 0.09, 1800);
			k.Sound(115, s);
		}

		// ================================================================ 116 flicker lamp
		// A hanging lamp above Boo flickers on and off; every time it goes dark, Boo glows in the dark.
		Frame Lamp(bool on, int sw)
		{
			var f = new Frame();
			for (int y = -4; y <= 4; y++) f.Dot(8 + (y >= 3 ? sw : 0), y, Ink);
			var l = new Frame();
			l.Rect(7, 5, 3, 1, Purple).Rect(6, 6, 5, 1, Purple).Rect(5, 7, 7, 1, PurpleLight).Rect(7, 8, 3, 1, on ? Glow : Line);
			if (on) { l.Dot(8, 8, GlowLight); foreach (var (x, y) in new[] { (4, 9), (12, 9), (8, 11), (5, 11), (11, 11) }) l.Dot(x, y, Glow); }
			l.Shift(sw, 0);
			for (int y = 0; y <= 15; y++) for (int x = 0; x < 16; x++) if (l[x, y] != '.') f[x, y] = l[x, y];
			return f;
		}
		int lpOn0 = k.F(Lamp(true, 0)), lpOn1 = k.F(Lamp(true, 1)), lpOnL = k.F(Lamp(true, -1)), lpOff0 = k.F(Lamp(false, 0)), lpOff1 = k.F(Lamp(false, 1));
		Frame DarkGlow(int b)
		{
			var f = G(1 + b, b, 4, b == 0 ? 1 : 3);
			return Map(f, (Sheet, GlowLight), (Line, GlowDeep), (Shade, GlowDeep), (Shadow, '.'));
		}
		int dg0 = k.F(DarkGlow(0)), dg1 = k.F(DarkGlow(1));
		int lu0 = k.F(Up(1, 0, 0, 0, 0, -1)), lu1 = k.F(Up(2, 1, 3, 0, 0, -1));
		int[] pat = { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 1, 1, 1 };
		var lampSeq = new List<int>(); var booSeq = new List<int> { lu0, lu1, lu0 };
		for (int i = 0; i < pat.Length; i++)
		{
			lampSeq.Add(pat[i] == 1 ? (i % 3 == 0 ? lpOn0 : i % 3 == 1 ? lpOn1 : lpOnL) : (i % 2 == 0 ? lpOff0 : lpOff1));
			booSeq.Add(pat[i] == 1 ? (i % 2 == 0 ? lu0 : lu1) : (i % 2 == 0 ? dg0 : dg1));
		}
		booSeq.AddRange(new[] { k.Giggle1, k.Giggle2, k.Giggle1, k.Happy });
		p.A(116, "flicker lamp").Move("0", "0").Speed("110").Seq(booSeq.ToArray()).Then(100, 1);
		k.Child(116, 230, "imageX-imageW*0.15", "imageY-imageH*0.85");
		Part(230, "lamp lowers", 110, lpOn0, lpOn1, lpOnL).Move("0", "1").Fade(0, 1).Then(100, 231).OnBorder(100, 299);
		Part(231, "lamp flickers", 110, lampSeq.ToArray()).Then(100, 232);
		Part(232, "lamp goes up", 110, lpOn0, lpOn1, lpOn0, lpOnL).Move("0", "-2").Fade(1, 0).OnBorder(100, 299);
		{
			var s = new Sfx(2.8, 2016);
			s.Tone(0.0, 0.3, 100, 100, 0.08, Sfx.Wave.Square, 0.01, 0.1);
			for (int i = 1; i < pat.Length; i++) if (pat[i] != pat[i - 1]) s.Click((3 + i) * 0.11, 0.5).Tone((3 + i) * 0.11, 0.05, 120, 120, 0.06, Sfx.Wave.Square, 0.002, 0.03);
			k.Sound(116, s.Soften(5000));
		}

		// ================================================================ 117 monster mask
		// Boo holds a green monster mask on a stick in front of his face and ROARS, peeks out, roars again and giggles.
		void Mask(Frame f, int x, int y, bool roar)
		{
			Put(f, x, y, "U........U", ".UGGGGGGU.", "GGGGGGGGGG", "GKKGGGGKKG", "GGyKGGKyGG", "GGGGGGGGGG",
				roar ? "GKWKWWKWKG" : "GGKKKKKKGG", roar ? ".GGKKKKGG." : ".GGGGGGGG.");
			Seg(f, x + 4, y + 8, x + 3, y + 10, Cone);
		}
		Frame Masked(int lift, int hem, int eyes, int mouth, int mx, int my, bool roar)
		{
			var f = G(lift, hem, eyes, mouth); Mask(f, mx, my, roar);
			if (roar && lift == 2) f.Dot(0, -3, Ink).Dot(1, -4, Ink).Dot(15, -3, Ink).Dot(14, -4, Ink);
			return f;
		}
		int mkRaise = k.F(Masked(1, 0, 2, 0, 3, 6, false)), mkOn = k.F(Masked(1, 0, 0, 0, 3, 0, false));
		int mkR1 = k.F(Masked(2, 1, 0, 0, 3, -1, true)), mkR2 = k.F(Masked(1, 0, 0, 0, 3, 0, true));
		int mkPeek = k.F(Masked(1, 1, 1, 0, 3, 6, false)), mkPeek2 = k.F(Masked(1, 0, 1, 0, 3, 5, false));
		int mkAside1 = k.F(Masked(1, 0, 1, 3, 0, 7, false)), mkAside2 = k.F(Masked(2, 1, 1, 3, 0, 6, false));
		k.Show(117, "monster mask", 120, (k.Stand, 1), (mkRaise, 2), (mkOn, 2), (mkR1, 1), (mkR2, 1), (mkR1, 1), (mkR2, 1), (mkR1, 1), (mkR2, 1),
			(mkPeek, 1), (mkPeek2, 1), (mkPeek, 1), (mkOn, 1), (mkR1, 1), (mkR2, 1), (mkR1, 1), (mkAside1, 1), (mkAside2, 1), (mkAside1, 1), (mkAside2, 1), (k.Happy, 2));
		{
			var s = new Sfx(2.3, 2017);
			foreach (double at in new[] { 0.6, 1.68 }) s.Tone(at, 0.45, 120, 90, 0.5, Sfx.Wave.Saw, 0.03, 0.15, amHz: 32, amDepth: 0.8).Noise(at, 0.4, 0.15, 900, 500, 0.03, 0.15, 100);
			k.Sound(117, s.Soften(1300));
		}

		// ================================================================ 118 wisps
		// Three will-o'-the-wisps circle around Boo; he follows them with his eyes and waves as they fly off.
		Frame Wisps(int t)
		{
			var f = new Frame();
			for (int i = 0; i < 3; i++)
			{
				double a = (t * 30 + i * 120) * Math.PI / 180, ta = a - 0.45;
				int x = Math.Clamp((int)Math.Round(7 + 7 * Math.Cos(a)), 0, 14), y = (int)Math.Round(4 + 7 * Math.Sin(a));
				int tx = Math.Clamp((int)Math.Round(7.5 + 7 * Math.Cos(ta)), 0, 15), ty = (int)Math.Round(4.5 + 7 * Math.Sin(ta));
				f.Dot(tx, ty, GlowDeep);
				f.Rect(x, y, 2, 2, GlowCyan).Dot(x, y, GlowLight);
			}
			return f;
		}
		var wsp = new int[16]; for (int t = 0; t < 16; t++) wsp[t] = k.F(Wisps(t));
		int wsL0 = k.F(G(1, 0, 2, 1)), wsL1 = k.F(G(2, 1, 2, 1)), wsR0 = k.F(G(1, 0, 3, 1)), wsR1 = k.F(G(2, 1, 3, 1));
		int wsU0 = k.F(Up(1, 0, 1, 1, 0)), wsU1 = k.F(Up(2, 1, 3, 0, 1));
		var wSeq = new List<int> { k.Surprise, k.Surprise, k.LookL, k.LookL };
		for (int t = 4; t < 20; t++)
		{
			double a = (t % 12) * 30 * Math.PI / 180;
			wSeq.Add(Math.Sin(a) < -0.5 ? (t % 2 == 0 ? wsU0 : wsU1) : Math.Cos(a) < 0 ? (t % 2 == 0 ? wsL0 : wsL1) : (t % 2 == 0 ? wsR0 : wsR1));
		}
		wSeq.AddRange(new[] { k.Wave, k.Wave2, k.Wave, k.Wave2, k.Wave, k.Wave2, k.Happy, k.Happy });
		p.A(118, "wisps").Move("0", "0").Speed("110").Seq(wSeq.ToArray()).Then(100, 1);
		k.Child(118, 233);
		Part(233, "wisps appear", 110, wsp[0], wsp[1], wsp[2], wsp[3]).Fade(0, 1).Then(100, 234);
		Part(234, "wisps circle", 110, wsp[4], wsp[5], wsp[6], wsp[7], wsp[8], wsp[9], wsp[10], wsp[11], wsp[12], wsp[13], wsp[14], wsp[15], wsp[0], wsp[1], wsp[2], wsp[3]).Then(100, 235);
		Part(235, "wisps fly off", 110, wsp[4], wsp[5], wsp[6], wsp[7], wsp[8], wsp[9], wsp[10], wsp[11]).Move("-4", "-5").Fade(1, 0).OnBorder(100, 299);
		{
			var s = new Sfx(3.2, 2018); var r = new Random(2018); int[] f0 = { 1568, 1760, 2093, 2349, 2637 };
			for (int i = 0; i < 10; i++) s.Bell(0.05 + i * 0.3 + r.NextDouble() * 0.05, f0[r.Next(f0.Length)], 0.12, 0.25);
			k.Sound(118, s);
		}

		// ================================================================ 119 bobbing for apples
		// A tub of water with floating apples: Boo dunks his head and comes up with an apple in his mouth.
		void Tub(Frame f, int t, bool both, bool splash)
		{
			f.Rect(1, 10, 1, 1, Cone).Rect(14, 10, 1, 1, Cone).Rect(2, 10, 12, 1, BookBlue).Dot(3 + (t * 3) % 9, 10, Tear).Dot(12 - (t * 2) % 7, 10, Tear);
			f.Rect(1, 11, 14, 1, Cone).Rect(1, 12, 14, 1, Line).Rect(1, 13, 14, 1, Cone).Rect(2, 14, 12, 1, Cone).Rect(3, 15, 10, 1, Line);
			int b = t % 2;
			if (both) { f.Rect(4, 8 + b, 2, 2, Red).Dot(4, 8 + b, RedLight).Dot(5, 7 + b, Stem); }
			f.Rect(10, 9 - b, 2, 2, Red).Dot(10, 9 - b, RedLight).Dot(11, 8 - b, Stem);
			if (splash) foreach (var (x, y) in new[] { (2, 7), (4, 5), (12, 6), (14, 8), (7, 3), (9, 4) }) f.Dot(x, y, Tear);
		}
		Frame Bob(int d, int t, int eyes, int mouth, bool both = true, bool splash = false, int lift = 1)
		{
			var f = G(lift, t % 2, eyes, mouth, 0, 0, false);
			if (eyes == 7) f.Rect(4, 5 - lift, 2, 2, Ink).Rect(10, 5 - lift, 2, 2, Ink);   // looking down
			if (d > 0) { f.Shift(0, d); Cut(f, 10); }
			Tub(f, t, both, splash);
			if (d >= 8) f.Dot(7, 8 - t % 2, Tear).Dot(9, 6 + t % 2, Tear);                  // bubbles from under the water
			return f;
		}
		Frame AppleMouth(int t, bool bite, int lift)
		{
			var f = Bob(0, t, 1, 4, false, false, lift); int y = 7 - lift;
			f.Rect(7, y, 2, 2, Red).Dot(7, y, RedLight).Dot(8, y - 1, Stem);
			if (bite) f.Dot(8, y + 1, Sheet).Dot(6, y + 3, Red).Dot(10, y + 2, RedLight);
			f.Dot(5, -lift, Tear).Dot(11, 1 - lift + t % 2, Tear);
			return f;
		}
		int ab0 = k.F(Bob(0, 0, 7, 0)), ab1 = k.F(Bob(0, 1, 7, 0)), abLean = k.F(Bob(2, 0, 7, 1)), abDunk = k.F(Bob(7, 1, 7, 1, true, true));
		int abU0 = k.F(Bob(8, 0, 7, 1, true)), abU1 = k.F(Bob(8, 1, 7, 1, true));
		var abr = Bob(3, 0, 1, 4, false, true); abr.Rect(7, 9, 2, 1, Red); int abRise = k.F(abr);
		int am0 = k.F(AppleMouth(0, false, 2)), am1 = k.F(AppleMouth(1, false, 1)), amBite = k.F(AppleMouth(0, true, 1)), amBite2 = k.F(AppleMouth(1, true, 2));
		k.Show(119, "bobbing apples", 120, (k.Stand, 1), (ab0, 1), (ab1, 1), (ab0, 1), (ab1, 1), (abLean, 2), (abDunk, 2), (abU0, 1), (abU1, 1), (abU0, 1), (abU1, 1),
			(abRise, 2), (am0, 1), (am1, 1), (am0, 1), (am1, 1), (amBite, 2), (amBite2, 1), (amBite, 1), (amBite2, 1), (amBite, 1), (k.Happy, 2));
		{
			var s = new Sfx(2.8, 2019);
			s.Noise(0.84, 0.35, 0.45, 3000, 700, 0.005, 0.25, 150);
			foreach (double at in new[] { 1.08, 1.22, 1.35, 1.46 }) s.Pop(at, 0.2, 350);
			s.Noise(1.56, 0.2, 0.25, 2500, 900, 0.005, 0.15, 200);
			s.Click(2.16, 0.6).Pop(2.17, 0.4, 500);
			k.Sound(119, s);
		}

		// ================================================================ 120 black cat
		// A black cat walks up, arches its back and hisses at Boo; he shivers, then they make friends and it purrs.
		void Cat(Frame f, int x, int pose, int v)
		{
			if (pose == 0)
			{
				f.Rect(x + 2, 12, 5, 2, Ink).Rect(x + 6, 10, 3, 3, Ink).Dot(x + 6, 9, Ink).Dot(x + 8, 9, Ink);
				f.Dot(x + 6, 11, Glow).Dot(x + 8, 11, Glow).Dot(x + 7, 12, Blush);
				if (v == 0) f.Rect(x + 2, 14, 1, 2, Ink).Rect(x + 6, 14, 1, 2, Ink);
				else f.Rect(x + 3, 14, 1, 2, Ink).Rect(x + 5, 14, 1, 2, Ink);
				Seg(f, x + 1, 12, x, 10 - v, Ink); f.Dot(x + 1, 9 - v, Ink);
			}
			else if (pose == 1)
			{
				f.Rect(x + 2, 11, 5, 1, Ink).Rect(x + 3, 10, 3, 1, Ink).Rect(x + 6, 11, 3, 3, Ink).Dot(x + 6, 10, Ink).Dot(x + 8, 10, Ink);
				f.Dot(x + 6, 12, Glow).Dot(x + 8, 12, Glow).Dot(x + 7, 13, Heart);
				f.Dot(x + 3 + v, 9, Ink).Dot(x + 5 - v, 9, Ink);                                       // fur up
				f.Rect(x + 2, 12, 1, 4, Ink).Rect(x + 4, 12, 1, 4, Ink).Rect(x + 6, 14, 1, 2, Ink).Rect(x + 8, 14, 1, 2, Ink);
				f.Rect(x, 6 + v, 2, 5 - v, Ink).Dot(x + 1, 11, Ink);                                   // puffed tail
			}
			else
			{
				f.Rect(x + 4, 11, 4, 4, Ink).Rect(x + 5, 8, 4, 3, Ink).Dot(x + 5, 7, Ink).Dot(x + 8, 7, Ink);
				f.Dot(x + 6, 9, Line).Dot(x + 8, 9, Line).Dot(x + 7, 10, Blush);
				f.Dot(x + 5, 15, Ink).Dot(x + 7, 15, Ink).Rect(x + 1, 15, 3, 1, Ink).Dot(x + 1, 14 - v, Ink);
				HeartAt(f, x + 6, 3 - v);
			}
		}
		int Kitty(int pose, int v, bool mirror = false) { var f = new Frame(); Cat(f, mirror ? 0 : 5, pose, v); if (mirror) f.MirrorX(); return k.F(f); }
		int ctW0 = Kitty(0, 0), ctW1 = Kitty(0, 1), ctA0 = Kitty(1, 0), ctA1 = Kitty(1, 1), ctS0 = Kitty(2, 0), ctS1 = Kitty(2, 1), ctM0 = Kitty(0, 0, true), ctM1 = Kitty(0, 1, true);
		k.Show(120, "black cat", 120, (k.LookL, 4), (k.Blink, 1), (k.LookL, 5), (k.Surprise, 2), (k.Shiver1, 1), (k.Shiver2, 1), (k.Shiver1, 1), (k.Shiver2, 1),
			(k.Heart1, 2), (k.Heart2, 2), (k.Heart1, 2), (k.Heart2, 2), (k.Wave, 2), (k.Wave2, 2), (k.Wave, 2), (k.Happy, 4));
		k.Child(120, 236, "imageX-imageW*1.6", "imageY");
		Part(236, "cat walks in", 120, Rep(5, ctW0, ctW1)).Move("3", "0").Then(100, 237).OnBorder(100, 299);
		Part(237, "cat hisses", 120, Rep(3, ctA0, ctA1)).Then(100, 238);
		Part(238, "cat purrs", 120, Rep(4, ctS0, ctS1)).Then(100, 239);
		Part(239, "cat walks off", 120, Rep(5, ctM0, ctM1)).Move("-4", "0").Fade(1, 0).OnBorder(100, 299);
		{
			var s = new Sfx(3.6, 2020);
			s.Noise(1.2, 0.45, 0.3, 7000, 5000, 0.02, 0.2, 2500);
			s.Noise(2.0, 0.9, 0.35, 250, 250, 0.1, 0.3, 30, amHz: 22, amDepth: 0.9);
			s.Tone(2.95, 0.18, 520, 800, 0.3, Sfx.Wave.Triangle, 0.02, 0.05, vibHz: 6, vibDepth: 0.02).Tone(3.13, 0.32, 800, 480, 0.3, Sfx.Wave.Triangle, 0.01, 0.15, vibHz: 6, vibDepth: 0.03);
			k.Sound(120, s);
		}

		// ================================================================ 121 cobweb
		// Boo floats up straight into a cobweb, wriggles in it, spins free and the web falls off.
		void Web(Frame f, int cx, int cy)
		{
			(int dx, int dy)[] dirs = { (-5, 0), (-4, -4), (0, -6), (4, -4), (5, 0), (4, 4), (0, 5), (-4, 4) };
			foreach (var (dx, dy) in dirs) Seg(f, cx, cy, cx + dx, cy + dy, Line);
			for (int i = 0; i < dirs.Length; i++)
			{
				var a = dirs[i]; var b = dirs[(i + 1) % dirs.Length];
				int n = 0; foreach (var (x, y) in Pts(cx + a.dx * 3 / 5, cy + a.dy * 3 / 5, cx + b.dx * 3 / 5, cy + b.dy * 3 / 5)) if (n++ % 2 == 0) f[x, y] = Line;
			}
			Seg(f, cx, cy - 6, cx, -4, Line);
		}
		Frame Webbed(int dx, int eyes, int mouth, int lift, int cx)
		{
			var f = G(lift, dx < 0 ? 0 : 1, eyes, mouth, 1, 1); if (dx != 0) f.Shift(dx, 0);
			Web(f, cx, 3 - lift); return f;
		}
		int wbHit = k.F(Webbed(0, 4, 1, 2, 6)), wbA = k.F(Webbed(-1, 5, 1, 2, 5)), wbB = k.F(Webbed(1, 5, 1, 1, 7));
		var cl1 = new Frame(); Put(cl1, 3, 0, "o.o.o", ".ooo.", "o.o.o"); int wbC0 = k.F(cl1);
		var cl2 = new Frame(); Put(cl2, 3, 0, ".o.o.", "ooooo", ".o.o."); int wbC1 = k.F(cl2);
		var wdn = G(1, 0, 7, 3); wdn.Rect(4, 4, 2, 2, Ink).Rect(10, 4, 2, 2, Ink); int wbDown = k.F(wdn);
		k.Lead(121, "cobweb", 120, 240, (k.Stand, 1), (k.F(G(2, 1, 0, 0)), 2), (wbHit, 2), (wbA, 1), (wbB, 1), (wbA, 1), (wbB, 1), (wbA, 1), (wbB, 1), (wbHit, 1));
		k.Show(240, "shake off web", 120, (k.SideL, 1), (k.Back, 1), (k.SideR, 1), (k.SideL, 1), (wbDown, 3), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 2));
		k.Child(240, 241);
		Part(241, "web falls", 120, Rep(5, wbC0, wbC1)).Move("-1", "3").Fade(1, 0).OnBorder(100, 299);

		// ================================================================ 122 potion
		// Boo drinks a bubbling purple potion, turns purple and hiccups bubbles, then sparkles back to white.
		Frame Potion(int mode, int b)
		{
			Frame f;
			if (mode == 0) { f = G(1, b, 2, 0); Put(f, 0, 3, ".n.", ".o.", "UVU", "UVU", "UUU", "UUU"); }
			else if (mode == 1) { f = G(1, b, 1, 4, 1, 0); Put(f, 1, 5, ".UUU..", b == 0 ? "UVVUoo" : "UUVUoo", ".UUU.."); }
			else if (mode == 2) { f = G(1, b, 4, 1); Put(f, 0, 3, ".o.", ".o.", "o.o", "o.o", "o.o", "ooo"); }
			else
			{
				f = G(1 + b, b, b == 0 ? 4 : 5, 1);
				var m = new[] { (Sheet, PurpleLight), (Shade, Purple), (Line, Purple) };
				if (mode == 3) { for (int y = f.MinY; y < 15; y++) for (int x = 0; x < 16; x++) if ((x + y) % 2 == 0) foreach (var (a, c) in m) if (f[x, y] == a) { f[x, y] = c; break; } }
				else Map(f, m);
				if (mode == 4 && b == 1) f.Dot(13, -3, PurpleLight).Dot(14, -4, PurpleLight).Dot(12, -4, PurpleLight).Dot(1, -2, PurpleLight);
			}
			return f;
		}
		int poHold = k.F(Potion(0, 0)), poDr0 = k.F(Potion(1, 0)), poDr1 = k.F(Potion(1, 1)), poEmpty = k.F(Potion(2, 0));
		int poHalf = k.F(Potion(3, 0)), poP0 = k.F(Potion(4, 0)), poP1 = k.F(Potion(4, 1));
		var spk = G(1, 0, 1, 3); Star(spk, 2, 0); Star(spk, 13, 1); spk.Dot(8, -3, Glow); int poSpark = k.F(spk);
		k.Show(122, "potion", 120, (k.Stand, 1), (poHold, 3), (poDr0, 2), (poDr1, 2), (poDr0, 2), (poEmpty, 2), (poHalf, 1), (poP0, 2), (poP1, 2), (poP0, 2), (poP1, 2),
			(poHalf, 1), (poSpark, 2), (k.Happy, 2));
		{
			var s = new Sfx(3.0, 2022);
			foreach (double at in new[] { 0.48, 0.72, 0.96 }) s.Tone(at, 0.13, 260, 150, 0.4, Sfx.Wave.Sine, 0.005, 0.06).Click(at, 0.2);
			s.Notes(1.44, 0.08, 0.2, Sfx.Wave.Triangle, 72, 75, 79, 84);
			s.Pop(1.8, 0.4, 1300).Pop(2.28, 0.4, 1400);
			s.Bell(2.64, 2093, 0.2, 0.3).Bell(2.72, 2637, 0.15, 0.3);
			k.Sound(122, s);
		}

		// ================================================================ 123 flashlight story
		// Boo holds a flashlight under his chin and tells a spooky story, ending with a BOO! and a giggle.
		Frame Story(bool on, int eyes, int mouth, int lift, int hem, int lean, int la = 1, int ra = 1)
		{
			var f = G(lift, hem, eyes, mouth, la, ra);
			if (lean != 0) Lean(f, lean, 8);
			if (on)
				for (int y = f.MinY; y < 13; y++) for (int x = 0; x < 16; x++)
				{
					if (f[x, y] != Sheet) continue;
					bool inCone = Math.Abs(x - 7.5) <= (9 - y) * 0.7 + 1;
					f[x, y] = inCone && y >= 3 - lift ? Glow : Shade;
				}
			f.Rect(7, 9, 2, 3, Ink).Dot(8, 10, Red).Rect(7, 8, 2, 1, on ? Glow : Line);
			return f;
		}
		int stOff = k.F(Story(false, 2, 0, 1, 0, 0, 0, 0));
		int stS0 = k.F(Story(true, 0, 1, 1, 0, 0)), stS1 = k.F(Story(true, 2, 4, 1, 1, -1)), stS2 = k.F(Story(true, 4, 1, 2, 1, 0));
		int stPrep = k.F(Story(true, 5, 1, 2, 0, 0));
		var stb = Story(true, 4, 2, 2, 1, 0); stb.Dot(0, -2, Ink).Dot(15, -2, Ink).Dot(1, -1, Ink).Dot(14, -1, Ink); int stBoo = k.F(stb);
		int flG1 = k.F(Story(false, 1, 3, 1, 0, 0, 0, 0)), flG2 = k.F(Story(false, 1, 3, 2, 1, 0, 0, 0));
		k.Show(123, "flashlight story", 130, (k.Stand, 1), (stOff, 2), (stS0, 1), (stS1, 1), (stS0, 1), (stS2, 1), (stS1, 1), (stS0, 1), (stS2, 1), (stS1, 1), (stS0, 1), (stS1, 1),
			(stPrep, 1), (stBoo, 3), (flG1, 1), (flG2, 1), (flG1, 1), (flG2, 1), (k.Happy, 2));
		{
			var s = new Sfx(2.8, 2023);
			s.Click(0.39, 0.6).Tone(0.4, 1.3, 98, 98, 0.22, Sfx.Wave.Triangle, 0.2, 0.3, amHz: 3, amDepth: 0.5).Tone(0.4, 1.3, 147, 147, 0.12, Sfx.Wave.Triangle, 0.2, 0.3);
			s.Tone(1.82, 0.4, 220, 110, 0.35, Sfx.Wave.Saw, 0.005, 0.25).Noise(1.82, 0.2, 0.2, 3000, 800, 0.002, 0.15, 300);
			s.Click(2.21, 0.5);
			k.Sound(123, s.Soften(3000));
		}

		// ================================================================ 124 spooky fog
		// Boo takes a deep breath and blows a bank of spooky fog that rolls away along the floor.
		Frame Fog(int t, int y0, int seed)
		{
			var f = new Frame(); var r = new Random(seed + t / 3);
			int n = 3 + Math.Min(t, 6) / 2;
			for (int i = 0; i < n; i++) FillCircle(f, 2.5f + i * 2.2f + (float)r.NextDouble(), y0 + 2.5f - (i % 2) * 0.8f + ((t + i) % 3 == 0 ? 0.5f : 0f), 1.6f + (float)r.NextDouble() * 0.9f, Shade, Shade);
			var src = f.Clone();
			for (int y = f.MinY; y <= 15; y++) for (int x = 0; x < 16; x++)
				if (src[x, y] == Shade && (src[x - 1, y] == '.' || src[x + 1, y] == '.' || src[x, y - 1] == '.' || src[x, y + 1] == '.')) f[x, y] = Zz;
			return f;
		}
		var fogLo = new int[12]; for (int t = 0; t < 12; t++) fogLo[t] = k.F(Fog(t, 11, 2024));
		var fogHi = new int[10]; for (int t = 0; t < 10; t++) fogHi[t] = k.F(Fog(t, 4, 2124));
		int fgIn = k.F(G(2, 0, 5, 1));
		Frame Blow(int t)
		{
			var f = G(1, t % 2, 6, 1);
			foreach (var (x, y) in t % 2 == 0 ? new[] { (5, 7), (3, 8), (1, 7), (2, 9) } : new[] { (5, 8), (3, 7), (1, 8), (0, 10) }) f.Dot(x, y, Zz);
			return f;
		}
		int fgB0 = k.F(Blow(0)), fgB1 = k.F(Blow(1));
		var fogA = G(1, 0, 4, 1, 1, 1); fogA.Dot(0, 1, Zz).Dot(15, 0, Zz).Dot(2, -3, Zz); int fgSp0 = k.F(fogA);
		var fogB = G(2, 1, 4, 1, 1, 1); fogB.Dot(1, -1, Zz).Dot(14, -3, Zz).Dot(13, 2, Zz); int fgSp1 = k.F(fogB);
		k.Show(124, "spooky fog", 120, (k.Stand, 1), (fgIn, 3), (fgB0, 1), (fgB1, 1), (fgB0, 1), (fgB1, 1), (fgB0, 1), (fgB1, 1), (fgB0, 1), (fgB1, 1),
			(k.LookL, 3), (fgSp0, 1), (fgSp1, 1), (fgSp0, 1), (fgSp1, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Giggle1, 1), (k.Giggle2, 1), (k.Happy, 2));
		k.Child(124, 242, "imageX-imageW*0.6", "imageY");
		k.Child(124, 243, "imageX-imageW*0.4", "imageY-imageH*0.3");
		var lo = new List<int> { blank, blank, blank, blank }; lo.AddRange(fogLo);
		Part(242, "fog rolls", 120, lo.ToArray()).Move("-2", "0").Fade(0.9, 0).OnBorder(100, 299);
		var hi = new List<int> { blank, blank, blank, blank, blank, blank }; hi.AddRange(fogHi);
		Part(243, "fog drifts", 120, hi.ToArray()).Move("-3", "-1").Fade(0.8, 0).OnBorder(100, 299);
		{
			var s = new Sfx(2.4, 2024);
			s.Noise(0.48, 1.3, 0.35, 600, 1500, 0.2, 0.5, 150, amHz: 3, amDepth: 0.4).Tone(0.5, 1.2, 90, 70, 0.15, Sfx.Wave.Sine, 0.2, 0.5);
			k.Sound(124, s);
		}
	}
}
