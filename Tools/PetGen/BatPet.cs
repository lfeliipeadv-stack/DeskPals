using System.Drawing;

namespace PetGen;

/// <summary>
/// "Morceguinho" — a little bat in three looks that the program shows as one card with color dots, like the gSheep:
/// purple (morcego_roxo), the vampire bat with a red cape lining and yellow eyes (morcego_vampiro) and the fruit bat,
/// brown and orange with a little nose (morcego_frutas). Same drawing and scenes; colors, eyes, mouth and favourite food
/// change.
/// He is a floating pet (no gravity, like Clippy): he flies anywhere on the screen, stays where he is dropped, flies up
/// to the top edge to hang upside down and sleep, and never walks. Frames are 34 px wide with 10 rows of headroom; the
/// bat is drawn at the bottom of the frame so that, flipped upside down, his feet touch the top of his window.
/// </summary>
static class BatPet
{
	const int W = 34, Head = 10;
	const int BX = 9, BY = 18;      // where the body art sits (16 x 16, feet on row 33)
	// the bat's own colors (they change with the variant)
	const char Out = 'O', Body = 'B', Belly = 'b', Wing = 'w', Lining = 'r', Pink = 'P', Iris = 'I', Eye = 'E', White = 'W', Mouth = 'K', Fang = 'f', Nose = 'N';
	const char Juice = 'J', Fruit = 'j', FruitDark = 'i';
	// props
	const char Yel = 'Y', YelDark = 'y', Grn = 'G', GrnLight = 'g', Heart = 'H', HeartLight = 'h', Spark = 'X', Water = 'A', BlueLight = 'u', Blue = 'U';
	const char Zz = 'Z', Note = 'q', Gray = 'k', Cloud = 'c', Line = 'l', Moon = 'M', MoonShade = 'm', Pumpkin = 'o', PumpkinDark = 'd', Spider = 'S';
	const char Brown = 'n', Garlic = 'L', Moth = 'T', MothDark = 't';

	static readonly string[] BodyArt =
	{
		"...O........O...",
		"..OPO......OPO..",
		"..OPBO....OBPO..",
		".OBBBBOOOOBBBBO.",
		"OBBBBBBBBBBBBBBO",
		"OBBBBBBBBBBBBBBO",
		"OBBBBBBBBBBBBBBO",
		"OBBBBBBBBBBBBBBO",
		"OBBBBBBBBBBBBBBO",
		"OBBBBBBBBBBBBBBO",
		".OBBbbbbbbbbBBO.",
		"..OBbbbbbbbbBO..",
		"..OBBbbbbbbBBO..",
		".OOBBBBBBBBBBOO.",
		"...OOBBBBBBOO...",
		"....O.O..O.O....",
	};
	static readonly string[] WingMid = { "OO......", "OwOO....", "OwwrOO..", "OwwwrrOO", ".OwwwwrO", ".OwOwwwO", "..O.OwwO" };
	static readonly string[] WingUp = { "O.......", "OO......", "OwO.....", "OwwO....", "OwwrO...", "OwwrrO..", "OwwwrrOO", ".OwwwwrO", "..OwwwwO", "...OOOOO" };
	static readonly string[] WingDown = { "......OO", "....OOrO", "..OOrrwO", ".OwrrwwO", "OwwwwwO.", "OwOwwO..", "O.OOO..." };
	static readonly string[] WingHalf = { "........", "O.......", "OOO.....", "OwrOO...", "OwwrrOOO", ".OwwwwrO", "..OOwwwO" };
	static readonly string[] WingFold = { "OO....", "OwO...", "OwwO..", "OwwrO.", "OwwwrO", ".OwwwO", "..OOO." };

	public static PetDef Purple() => Build(0);
	public static PetDef Vampire() => Build(1);
	public static PetDef FruitBat() => Build(2);

	static PetDef Build(int v)
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		string[] folders = { "morcego_roxo", "morcego_vampiro", "morcego_frutas" };
		string[] names = { "Morceguinho Roxo", "Morceguinho Vampiro", "Morceguinho das Frutas" };
		var p = new PetDef
		{
			Folder = folders[v],
			Title = names[v],
			PetName = names[v],
			Author = "Luan Felipe",
			Version = "1.0",
			Floating = true,
			Info = new[]
			{
				"A little purple bat. He flies all over the screen, hangs upside down[br]from the top to sleep, squeaks, uses his sonar and chases moths.",
				"A little vampire bat with a red cape lining and yellow eyes. He flies[br]all over the screen, hangs upside down to sleep, says BOO and hates garlic.",
				"A little fruit bat, brown and orange. He flies all over the screen,[br]hangs upside down to sleep and loves bananas.",
			}[v],
			Palette = new Dictionary<char, Color>
			{
				[Eye] = Color.FromArgb(20, 20, 20), [White] = Color.FromArgb(255, 255, 255), [Fang] = Color.FromArgb(255, 255, 255),
				[Yel] = Color.FromArgb(255, 214, 60), [YelDark] = Color.FromArgb(214, 168, 30), [Grn] = Color.FromArgb(90, 176, 76), [GrnLight] = Color.FromArgb(140, 214, 110),
				[Heart] = Color.FromArgb(232, 62, 98), [HeartLight] = Color.FromArgb(255, 150, 170), [Spark] = Color.FromArgb(255, 236, 110),
				[Water] = Color.FromArgb(110, 180, 240), [BlueLight] = Color.FromArgb(150, 196, 245), [Blue] = Color.FromArgb(56, 110, 220),
				[Zz] = Color.FromArgb(150, 170, 240), [Note] = Color.FromArgb(120, 84, 220), [Gray] = Color.FromArgb(150, 150, 156),
				[Cloud] = Color.FromArgb(226, 230, 238), [Line] = Color.FromArgb(160, 160, 166), [Moon] = Color.FromArgb(255, 242, 160), [MoonShade] = Color.FromArgb(226, 200, 110),
				[Pumpkin] = Color.FromArgb(255, 140, 40), [PumpkinDark] = Color.FromArgb(200, 90, 16), [Spider] = Color.FromArgb(40, 40, 44),
				[Brown] = Color.FromArgb(120, 80, 44), [Garlic] = Color.FromArgb(244, 238, 220), [Moth] = Color.FromArgb(214, 196, 160), [MothDark] = Color.FromArgb(150, 128, 96),
			},
			Scale = 2,
			TilesX = 8,
		};
		// the bat's own colors
		var bat = v switch
		{
			1 => new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(20, 20, 24), [Body] = Color.FromArgb(62, 62, 76), [Belly] = Color.FromArgb(98, 98, 122), [Wing] = Color.FromArgb(42, 42, 54),
				[Lining] = Color.FromArgb(180, 32, 44), [Pink] = Color.FromArgb(224, 96, 112), [Iris] = Color.FromArgb(255, 210, 60), [Mouth] = Color.FromArgb(90, 16, 24),
				[Nose] = Color.FromArgb(20, 20, 24), [Juice] = Color.FromArgb(214, 50, 60), [Fruit] = Color.FromArgb(222, 52, 52), [FruitDark] = Color.FromArgb(150, 30, 30),
			},
			2 => new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(46, 26, 14), [Body] = Color.FromArgb(138, 84, 48), [Belly] = Color.FromArgb(224, 148, 68), [Wing] = Color.FromArgb(78, 48, 32),
				[Lining] = Color.FromArgb(106, 64, 40), [Pink] = Color.FromArgb(240, 160, 160), [Iris] = Color.FromArgb(20, 20, 20), [Mouth] = Color.FromArgb(58, 30, 20),
				[Nose] = Color.FromArgb(46, 26, 14), [Juice] = Color.FromArgb(255, 154, 40), [Fruit] = Color.FromArgb(255, 216, 74), [FruitDark] = Color.FromArgb(216, 168, 32),
			},
			_ => new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(42, 30, 60), [Body] = Color.FromArgb(122, 100, 168), [Belly] = Color.FromArgb(168, 148, 208), [Wing] = Color.FromArgb(78, 60, 120),
				[Lining] = Color.FromArgb(96, 76, 142), [Pink] = Color.FromArgb(255, 157, 176), [Iris] = Color.FromArgb(20, 20, 20), [Mouth] = Color.FromArgb(58, 30, 42),
				[Nose] = Color.FromArgb(42, 30, 60), [Juice] = Color.FromArgb(122, 58, 168), [Fruit] = Color.FromArgb(130, 70, 180), [FruitDark] = Color.FromArgb(84, 40, 128),
			},
		};
		foreach (var kv in bat) p.Palette[kv.Key] = kv.Value;
		bool fruitBat = v == 2;

		// ------------------------------------------------------------ helpers
		void St(Frame f, int x, int y, string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		string[] Mirror(string[] rows) => rows.Select(r => new string(r.Reverse().ToArray())).ToArray();
		Frame Empty() => new Frame();
		var seen = new Dictionary<string, int>();
		int Add(Frame f) { string k = f.ToString(); if (!seen.TryGetValue(k, out int i)) { i = p.Add(f); seen[k] = i; } return i; }

		// eyes (3 x 2 each): 0 open, 1 closed, 2 happy, 3 wide, 4 up, 5 half, 6 angry, 7 teary, 8 look ahead (left), 9 dizzy
		void Eyes(Frame f, int x, int y, int mode)
		{
			foreach (int ex in new[] { x + 3, x + 10 })
			{
				switch (mode)
				{
					case 0: St(f, ex, y + 5, new[] { "WII", "III" }); break;
					case 1: St(f, ex, y + 6, new[] { "EEE" }); break;
					case 2: St(f, ex, y + 5, new[] { ".E.", "E.E" }); break;
					case 3: St(f, ex, y + 4, new[] { "WII", "III", "III" }); break;
					case 4: St(f, ex, y + 4, new[] { "WII", "III" }); break;
					case 5: St(f, ex, y + 5, new[] { "OOO", "III" }); break;
					case 6: St(f, ex, y + 4, new[] { "O..", ".OO", "III" }); break;
					case 7: St(f, ex, y + 5, new[] { "WII", "III", "A.." }); break;
					case 8: St(f, ex, y + 5, new[] { "IIW", "III" }); break;
					case 9: St(f, ex, y + 5, new[] { "E.E", ".E." }); break;
				}
				if (mode == 6 && ex == x + 10) St(f, ex, y + 4, new[] { "..O", "OO." });
			}
		}
		// mouth: 0 smile, 1 open, 2 wide, 3 small closed, 4 "o"
		void MouthAt(Frame f, int x, int y, int mode)
		{
			f.Dot(x + 2, y + 7, Pink).Dot(x + 13, y + 7, Pink);
			if (fruitBat)
			{
				f.Rect(x + 7, y + 7, 2, 1, Nose);
				switch (mode)
				{
					case 0: f.Dot(x + 6, y + 8, Mouth).Dot(x + 9, y + 8, Mouth).Rect(x + 7, y + 9, 2, 1, Mouth); break;
					case 1: f.Rect(x + 6, y + 8, 4, 2, Mouth); break;
					case 2: f.Rect(x + 6, y + 8, 4, 3, Mouth); break;
					case 3: f.Rect(x + 7, y + 9, 2, 1, Mouth); break;
					case 4: f.Rect(x + 7, y + 8, 2, 2, Mouth); break;
				}
				return;
			}
			switch (mode)
			{
				case 0: f.Rect(x + 6, y + 7, 4, 1, Mouth).Dot(x + 6, y + 8, Fang).Rect(x + 7, y + 8, 2, 1, Mouth).Dot(x + 9, y + 8, Fang); break;
				case 1: f.Rect(x + 6, y + 7, 4, 3, Mouth).Dot(x + 6, y + 7, Fang).Dot(x + 9, y + 7, Fang); break;
				case 2: f.Rect(x + 5, y + 7, 6, 4, Mouth).Dot(x + 5, y + 7, Fang).Dot(x + 10, y + 7, Fang); break;
				case 3: f.Rect(x + 7, y + 8, 2, 1, Mouth); break;
				case 4: f.Rect(x + 7, y + 7, 2, 2, Mouth); break;
			}
		}
		// wings: 0 mid, 1 up, 2 down, 3 half, 4 folded (wrapped on the belly), 5 left up / right down, 6 left down / right up
		void Wings(Frame f, int x, int y, int mode)
		{
			string[] Pick(int m) => m switch { 1 => WingUp, 2 => WingDown, 3 => WingHalf, _ => WingMid };
			int Dy(int m) => m switch { 1 => -1, 2 => 8, 3 => 4, _ => 6 };
			if (mode == 4)
			{
				St(f, x - 1, y + 7, WingFold);
				St(f, x + 11, y + 7, Mirror(WingFold));
				return;
			}
			int l = mode == 5 ? 1 : mode == 6 ? 2 : mode, r = mode == 5 ? 2 : mode == 6 ? 1 : mode;
			St(f, x - 8, y + Dy(l), Pick(l));
			St(f, x + 16, y + Dy(r), Mirror(Pick(r)));
		}
		// a pose: up = height above the floor of the frame (flying ~3, bobbing 2-4), dx = lean
		Frame Pose(int eyes = 0, int mouth = 0, int wing = 0, int up = 3, int dx = 0)
		{
			var f = new Frame();
			int x = BX + dx, y = BY - up;
			if (wing != 4) Wings(f, x, y, wing);
			St(f, x, y, BodyArt);
			Eyes(f, x, y, eyes);
			MouthAt(f, x, y, mouth);
			if (wing == 4) Wings(f, x, y, wing);
			if (up > 0) f.Rect(x + 4, y + 15, 8, 1, '.');   // flying: the feet are tucked
			if (up > 0) f.Dot(x + 6, y + 14, Out).Dot(x + 9, y + 14, Out);
			return f;
		}
		// upside down: the pose drawn on the floor of the frame, flipped so the feet touch the top of the window
		Frame Hang(Frame src)
		{
			var g = new Frame();
			for (int yy = g.MinY; yy < W; yy++) for (int xx = 0; xx < W; xx++) g[xx, (g.MinY + W - 1) - yy] = src[xx, yy];
			return g;
		}
		Frame HangPose(int eyes = 1, int mouth = 3, int wing = 4) => Hang(Pose(eyes, mouth, wing, 0));

		void HeartAt(Frame f, int x, int y) => St(f, x, y, new[] { "HH.HH", "HhHHH", ".HHH.", "..H.." });
		void NoteAt(Frame f, int x, int y) => St(f, x, y, new[] { ".qq", ".q.", "qq." });
		void Drop(Frame f, int x, int y) => f.Dot(x, y, Water).Dot(x, y + 1, Water);
		void Zed(Frame f, int x, int y) => St(f, x, y, new[] { "ZZZZ", "..Z.", ".Z..", "ZZZZ" });
		void Bubble(Frame f, int x, int y, string[] glyphs)
		{
			int w = glyphs.Max(g => g.Length) + 4;
			var rows = new List<string> { "." + new string('O', w - 2) + "." };
			for (int i = 0; i < glyphs.Length + 2; i++) rows.Add("O" + new string('W', w - 2) + "O");
			rows.Add("." + new string('O', w - 2) + ".");
			rows.Add("....OWO"); rows.Add(".....O");
			St(f, x, y, rows.ToArray());
			St(f, x + 2, y + 2, glyphs);
		}
		string[] iii = { "E.E.E.E", "E.E.E.E", "E.E.E.E", ".......", "E.E.E.E" };
		string[] buu = { "EEE..E.E.E.E", "E..E.E.E.E.E", "EEE..E.E.E.E", "E..E.E.E.E.E", "EEE...E...E." };
		string[] eca = { "EEE..EE..E.", "E...E...E.E", "EE..E...EEE", "E...E...E.E", "EEE..EE.E.E" };
		string[] ok = { "EEE.E.E", "E.E.EE.", "E.E.E..", "E.E.EE.", "EEE.E.E" };
		string[] check = { "......GG", ".....GG.", "GG..GG..", ".GGGG...", "..GG...." };
		string[] kkk = { "E...E...E..", "E.E.E.E.E.E", "EE..EE..EE.", "E.E.E.E.E.E", "E.E.E.E.E.E" };
		string[] bang = { "E", "E", "E", ".", "E" };
		string[] question = { "EEE", "..E", ".E.", "...", ".E." };
		void Sparks(Frame f, int k) { foreach (var (sx, sy) in new[] { (2, 10), (30, 12), (1, 24), (32, 20), (16, 4) }) f.Dot(sx + k, sy - k, Spark); }
		void Bulb(Frame f, int x, int y, bool lit) => St(f, x, y, new[] { ".XXX.", "XXXXX", "XXWXX", ".XXX.", ".lll." }.Select(r => lit ? r : r.Replace('X', 'c').Replace('W', 'k')).ToArray());
		void LoadingRing(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (0, -3), (2, -2), (3, 0), (2, 2), (0, 3), (-2, 2), (-3, 0), (-2, -2) };
			for (int i = 0; i < 8; i++) f.Dot(cx + pts[i].Item1, cy + pts[i].Item2, ((i - k * 2) % 8 + 8) % 8 < 3 ? Blue : Cloud);
		}
		void Book(Frame f, int x, int y) => St(f, x, y, new[] { "OUUUUOUUUUO", "OWWWWOWWWWO", "OWllWOWllWO", "OWWWWOWWWWO", ".OOOOOOOOO." });
		void Glasses(Frame f, int x, int y) { foreach (int ex in new[] { x + 2, x + 9 }) { f.Rect(ex, y + 4, 5, 1, Eye).Rect(ex, y + 7, 5, 1, Eye).Rect(ex, y + 4, 1, 4, Eye).Rect(ex + 4, y + 4, 1, 4, Eye); } f.Rect(x + 7, y + 5, 2, 1, Eye); }
		void Laptop(Frame f, int x, int y, int k) { St(f, x, y, new[] { "OOOOOOOOO", "OUUUUUUUO", "OUggUUUUO", "OUUXXUUUO", "OOOOOOOOO", "OlllllllO", ".OOOOOOO." }); if (k == 1) f.Dot(x + 5, y + 2, GrnLight).Dot(x + 6, y + 2, GrnLight); }
		void DownloadBox(Frame f, int x, int y) => St(f, x, y, new[] { "OOOOOOO", "OGGWGGO", "OGGWGGO", "OGWWWGO", "OGGWGGO", "OOOOOOO" });
		void Warning(Frame f, int x, int y) => St(f, x, y, new[] { "....O....", "...OYO...", "..OYEYO..", "..OYEYO..", ".OYYEYYO.", ".OYYYYYO.", "OYYYEYYYO", "OOOOOOOOO" });
		void Steam(Frame f, int x, int y) => St(f, x, y, new[] { ".cc.", "cckc", ".cc." });
		void Sweat(Frame f, int x, int y) => St(f, x, y, new[] { ".A.", "AAA", "AWA", ".A." });
		void Sun(Frame f, int x, int y) => St(f, x, y, new[] { "X.X.X", ".XXX.", "XXXXX", ".XXX.", "X.X.X" });
		void Snow(Frame f, int k) { foreach (var (sx, sy) in new[] { (2, -8), (9, -2), (19, -6), (29, 0), (5, 8), (31, 12), (14, -10) }) f.Dot(sx, ((sy + k * 3) + 60) % 40 - 10, Zz); }
		void Confetti(Frame f, int k)
		{
			char[] cs = { Heart, Spark, Blue, Grn, Yel };
			for (int i = 0; i < 20; i++) f.Dot((i * 7 + k * 3) % W, -10 + (i * 5 + k * 4) % 42, cs[(i + k) % cs.Length]);
		}
		void CloudAt(Frame f, int x, int y, int k)
		{
			St(f, x, y, new[] { "...kkkk.....", ".kkcccckk...", "kccccccccckk", "kcccccccccck", ".kkkkkkkkkk." });
			for (int i = 0; i < 5; i++) Drop(f, x + 1 + i * 2 + k, y + 6 + ((i + k) % 2) * 3);
		}
		void Umbrella(Frame f, int x, int y) { St(f, x, y, new[] { "....OOOOO....", "..OOUUUUUOO..", ".OUUUuUUUUUO.", "OUUUUUUUUuUUO", "O.O.O.n.O.O.O" }); f.Rect(x + 6, y + 5, 1, 10, Brown); }
		void Shades(Frame f, int x, int y) { f.Rect(x + 2, y + 5, 5, 2, Eye).Rect(x + 9, y + 5, 5, 2, Eye).Rect(x + 7, y + 5, 2, 1, Eye).Dot(x + 3, y + 5, White).Dot(x + 10, y + 5, White); }
		void Scarf(Frame f, int x, int y) => St(f, x + 1, y + 10, new[] { "HWHWHWHWHWHWHW", "..HW..........", "..WH.........." });
		void Beanie(Frame f, int x, int y) => St(f, x + 2, y - 2, new[] { "....W.......", "..UWUWUWUW..", ".UWUWUWUWUW.", "WUWUWUWUWUWU" });
		void Headphones(Frame f, int x, int y) { St(f, x + 1, y + 1, new[] { "..qqqqqqqqqq..", ".q..........q.", "q............q" }); f.Rect(x - 1, y + 4, 2, 4, Note).Rect(x + 15, y + 4, 2, 4, Note); }
		void PartyHat(Frame f, int x, int y) => St(f, x + 5, y - 5, new[] { "...X...", "..HXH..", "..XHX..", ".HXHXH.", ".XHXHX." });
		void Cup(Frame f, int x, int y) => St(f, x, y, new[] { "OWWWO.", "OnnnOO", "OWWWO.O", ".OOO.." });
		void Broom(Frame f, int x, int y, int k) { for (int i = 0; i < 8; i++) f.Dot(x + 6 - i / 2 + k, y + i, Brown); St(f, x + k, y + 8, new[] { ".yyyy", "yyyyy", "yYyYy" }); }
		void Hammer(Frame f, int x, int y, bool up) => St(f, x, y, up ? new[] { "lll.", "lll.", ".n..", ".n..", ".n.." } : new[] { "nnnll", "...ll" });
		void Dust(Frame f, int x, int y) => St(f, x, y, new[] { "..kk..", ".kcck.", "kcckck", ".kkkk." });
		void Fan(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "HYHYH", "HYHYH", ".HYH.", "..n..", "..n.." } : new[] { ".YHY.", "YHYHY", ".YHY.", "..n..", "..n.." });
		void IceCream(Frame f, int x, int y, bool scoop) { if (scoop) St(f, x, y, new[] { ".hhh.", "hHhhh", ".hhh." }); St(f, x, y + 3, new[] { ".nyn.", "..n..", "..y.." }); }
		void Slip(Frame f, int x, int y) => St(f, x, y, new[] { "WWWWW", "WlllW", "WWWWW" });
		void FortuneCookie(Frame f, int x, int y, bool broken) => St(f, x, y, broken ? new[] { "YY...YY", "Yy.W.yY", ".Y...Y." } : new[] { "..YYY..", ".YYyYY.", "YYyYyYY", ".YYYYY." });
		void Flag(Frame f, int x, int y, int k)
		{
			f.Rect(x, y, 1, 14, Brown);
			var flag = new[] { "GGGGGGGGG", "GGGYYYGGG", "GYYUUUYYG", "GYYUUUYYG", "GGGYYYGGG", "GGGGGGGGG" };
			for (int r = 0; r < flag.Length; r++) St(f, x + 1, y + r + (k == 1 && r % 2 == 0 ? 1 : 0), new[] { flag[r] });
		}
		// the variant's favourite food: grapes (purple), a tomato (vampire), a banana (fruit bat)
		void Food(Frame f, int x, int y, int bites)
		{
			string[] food = v switch
			{
				1 => new[] { "..G..", ".jjj.", "jjjjj", "jjjji", ".iii." },
				2 => new[] { "....n", "...jj", "..jji", ".jji.", "jji..", ".i..." },
				_ => new[] { "..G..", ".jj..", "jjjj.", ".jjj.", "..j.." },
			};
			St(f, x, y, food);
			for (int b = 0; b < bites; b++) f.Rect(x + b * 2, y + 1, 2, 2, '.');
		}
		void JuiceBox(Frame f, int x, int y) { St(f, x, y, new[] { "...l", "...l", "OOOl", "OJJO", "OJWO", "OJJO", "OOOO" }); }
		void Rings(Frame f, int k)
		{
			// sonar: arcs in front of him (to the left), growing with k
			int cx = 8 - k * 3, cy = 26;
			for (int a = -4; a <= 4; a++) f.Dot(cx - (int)Math.Round(Math.Sqrt(Math.Max(0, 16 - a * a)) / 2.0), cy + a, k % 2 == 0 ? BlueLight : Blue);
		}
		void MiniBat(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "O..O.O..O", "wO.OBO.Ow", "wwOBBBOww", ".O.O.O.O." } : new[] { "...O.O...", "..OBBBO..", "OwwBBBwwO", "O.O...O.O" });
		void MothAt(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "tTT.TTt", ".TTtTT.", "..tEt.." } : new[] { "...t...", ".tTtTt.", "..tEt.." });
		void SpiderAt(Frame f, int x, int top, int len)
		{
			for (int i = 0; i < len; i++) f.Dot(x + 2, top + i, Line);
			St(f, x, top + len, new[] { "S.S.S", ".SSS.", "SSWSS", ".SSS.", "S.S.S" });
		}
		void GarlicAt(Frame f, int x, int y) => St(f, x, y, new[] { "..G..", ".LkL.", "LLkLL", "LLkLL", ".LLL." });
		void PumpkinAt(Frame f, int x, int y, int k) => St(f, x, y, new[] { "...G...", ".ooooo.", "oXoooXo".Replace('X', k == 0 ? 'X' : 'Y'), "ooooooo", "odXXXdo".Replace('X', k == 0 ? 'X' : 'Y'), ".ddddd." });
		void MoonAt(Frame f, int x, int y) => St(f, x, y, new[] { "..MMM..", ".MMm...", "MMm....", "MMm....", "MMm....", ".MMm...", "..MMM.." });
		void Candy(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "H.H", ".W.", "H.H" } : new[] { "Y.Y", ".G.", "Y.Y" });
		void Firefly(Frame f, int x, int y, bool on) => St(f, x, y, on ? new[] { ".X.", "XYX", ".X." } : new[] { "...", ".y.", "..." });

		// ------------------------------------------------------------ frames: flying and basics
		// the flap: wings up, half, mid, down, mid, half — the body bobs a little against the beat
		int f1 = Add(Pose(0, 0, 1, 4)), f2 = Add(Pose(0, 0, 3, 4)), f3 = Add(Pose(0, 0, 0, 3)), f4 = Add(Pose(0, 0, 2, 2)), f5 = Add(Pose(0, 0, 0, 3)), f6 = Add(Pose(0, 0, 3, 4));
		int[] flap = { f1, f2, f3, f4, f5, f6 };
		int fBlink = Add(Pose(1, 0, 0, 3));
		int fHappy = Add(Pose(2, 0, 1, 4));
		int fWide = Add(Pose(3, 1, 1, 4));
		int fUp = Add(Pose(4, 0, 0, 3));
		int fAhead = Add(Pose(8, 0, 0, 3));
		int fTalk = Add(Pose(0, 1, 0, 3));
		int[] flapHappy = { Add(Pose(2, 0, 1, 4)), Add(Pose(2, 0, 3, 4)), Add(Pose(2, 0, 0, 3)), Add(Pose(2, 0, 2, 2)), Add(Pose(2, 0, 0, 3)), Add(Pose(2, 0, 3, 4)) };
		int[] flapWide = { Add(Pose(3, 1, 1, 4)), Add(Pose(3, 1, 3, 4)), Add(Pose(3, 1, 0, 3)), Add(Pose(3, 1, 2, 2)), Add(Pose(3, 1, 0, 3)), Add(Pose(3, 1, 3, 4)) };
		int[] flapAhead = { Add(Pose(8, 0, 1, 4, -1)), Add(Pose(8, 0, 3, 4, -1)), Add(Pose(8, 0, 0, 3, -1)), Add(Pose(8, 0, 2, 2, -1)), Add(Pose(8, 0, 0, 3, -1)), Add(Pose(8, 0, 3, 4, -1)) };
		int fFold = Add(Pose(0, 3, 4, 3));
		int fFoldBlink = Add(Pose(1, 3, 4, 3));
		int fWave1 = Add(Pose(2, 0, 5, 3)), fWave2 = Add(Pose(2, 0, 6, 3));
		int fDrag1 = Add(Pose(3, 2, 1, 3)), fDrag2 = Add(Pose(3, 2, 2, 3));
		int fDizzy1 = Add(Pose(9, 3, 3, 3, -1)), fDizzy2 = Add(Pose(9, 3, 3, 3, 1));
		// upside down (hanging from the top of the screen)
		int fHang = Add(HangPose(0, 3, 4)), fHangBlink = Add(HangPose(1, 3, 4)), fHangHappy = Add(HangPose(2, 0, 4));
		int fHangSleep = Add(HangPose(1, 3, 4));
		var hz1 = HangPose(1, 3, 4); Zed(hz1, 24, 20); int fHangZ = Add(hz1);
		int fHangWings = Add(HangPose(3, 1, 0));
		int fHangStretch = Add(HangPose(1, 2, 1));

		// ------------------------------------------------------------ frames: scenes
		var sonar = new int[4]; for (int k = 0; k < 4; k++) { var f = Pose(0, 1, 0, 3); sonar[k] = Add(f); }
		var rings = new int[4]; for (int k = 0; k < 4; k++) { var f = Empty(); Rings(f, k); Rings(f, k + 2); rings[k] = Add(f); }
		var mothF = new int[2]; for (int k = 0; k < 2; k++) { var f = Empty(); MothAt(f, 4, 22, k); mothF[k] = Add(f); }
		var gulp = Pose(2, 4, 0, 3); int fGulp = Add(gulp);
		var chew = Pose(2, 3, 3, 3); int fChew = Add(chew);
		var mb = new int[2]; for (int k = 0; k < 2; k++) { var f = Empty(); MiniBat(f, 4, 14, k); MiniBat(f, 16, 8 + k, 1 - k); MiniBat(f, 24, 18 - k, k); mb[k] = Add(f); }
		var moonF = Empty(); MoonAt(moonF, 4, -8); moonF.Dot(16, -6, Spark).Dot(24, -2, Spark).Dot(1, 4, Spark); int fMoon = Add(moonF);
		var loveMoon = Pose(2, 0, 4, 3); HeartAt(loveMoon, 4, 4); int fLoveMoon = Add(loveMoon);
		var spiders = new int[6]; for (int k = 0; k < 6; k++) { var f = Empty(); SpiderAt(f, 6, -10, 4 + Math.Min(k, 3) * 6 - (k > 3 ? (k - 3) * 6 : 0)); spiders[k] = Add(f); }
		var shocked = Pose(3, 2, 1, 5); Bubble(shocked, 0, -8, bang); int fShocked = Add(shocked);
		var garlicF = Empty(); GarlicAt(garlicF, 3, 22); int fGarlic = Add(garlicF);
		var ecaF = Pose(6, 2, 1, 4, 2); Bubble(ecaF, 0, -2, eca); int fEca = Add(ecaF);
		var pumpkin = new int[2]; for (int k = 0; k < 2; k++) { var f = Empty(); PumpkinAt(f, 2, 27, k); pumpkin[k] = Add(f); }
		var candy = new int[2]; for (int k = 0; k < 2; k++) { var f = Empty(); Candy(f, 4, 4, k); Candy(f, 20, 12, 1 - k); Candy(f, 12, -6, k); candy[k] = Add(f); }
		var fireflies = new int[4]; for (int k = 0; k < 4; k++) { var f = Empty(); Firefly(f, 4 + (k % 2) * 3, 12 - k, k % 2 == 0); Firefly(f, 20 - k, 4 + (k % 2) * 2, k % 2 == 1); fireflies[k] = Add(f); }
		var squeak = Pose(2, 2, 1, 4); int fSqueak = Add(squeak);
		var iiiB = Empty(); Bubble(iiiB, 0, -8, iii); int fIii = Add(iiiB);
		var hide = Pose(1, 3, 4, 3); int fHide = Add(hide);
		var boo = Pose(6, 2, 1, 5); Bubble(boo, 0, -8, buu); int fBoo = Add(boo);
		var juice1 = Pose(0, 3, 4, 3); JuiceBox(juice1, 3, 22); int fJuice1 = Add(juice1);
		var juice2 = Pose(1, 4, 4, 3); JuiceBox(juice2, 5, 21); juice2.Rect(10, 21, 4, 1, Line); int fJuice2 = Add(juice2);
		var juice3 = Pose(2, 0, 4, 3); JuiceBox(juice3, 3, 22); juice3.Dot(1, 18, Spark).Dot(30, 16, Spark); int fJuice3 = Add(juice3);
		var food0 = Pose(3, 0, 4, 3); Food(food0, 3, 24, 0); int fFood0 = Add(food0);
		var food1 = Pose(1, 1, 4, 3); Food(food1, 3, 24, 1); int fFood1 = Add(food1);
		var food2 = Pose(2, 3, 4, 3); Food(food2, 3, 24, 2); int fFood2 = Add(food2);
		int fUpside = Add(Hang(Pose(2, 0, 0, 0)));
		int fUpside2 = Add(Hang(Pose(2, 0, 1, 0)));
		var laugh1 = Pose(2, 2, 1, 4); Bubble(laugh1, 0, -8, kkk); int fLaugh1 = Add(laugh1);
		var laugh2 = Pose(2, 1, 0, 3); Bubble(laugh2, 0, -8, kkk); int fLaugh2 = Add(laugh2);
		var cu = Pose(4, 3, 3, 3, 1); Bubble(cu, 2, -6, question); int fCurious = Add(cu);
		var sz1 = Pose(4, 1, 3, 4); int fAh = Add(sz1);
		var sz2 = Pose(1, 2, 2, 2, -1); sz2.Dot(2, 26, Water).Dot(4, 28, Water).Dot(1, 22, Water); int fChoo = Add(sz2);
		int fDance1 = Add(Pose(2, 0, 5, 4, -1)), fDance2 = Add(Pose(2, 1, 6, 2, 1));
		int fHic = Add(Pose(3, 4, 1, 6));

		// program features
		var th1 = Pose(4, 3, 3, 3); for (int i = 0; i < 3; i++) th1.Dot(4 + i * 2, 12 - i * 2, Note); int fThink1 = Add(th1);
		var th2 = Pose(0, 3, 4, 3); St(th2, 1, -2, new[] { ".kkk.", "kccck", "kccck", ".kkk." }); int fThink2 = Add(th2);
		var id1 = Pose(3, 0, 0, 3); Bulb(id1, 3, -6, false); int fIdea1 = Add(id1);
		var id2 = Pose(2, 1, 1, 4); Bulb(id2, 3, -7, true); id2.Dot(1, -4, Spark).Dot(10, -5, Spark); int fIdea2 = Add(id2);
		var ib = Empty(); Bulb(ib, 3, -7, true); int fBulb = Add(ib);
		var rd1 = Pose(0, 3, 4, 3); Glasses(rd1, BX, BY - 3); Book(rd1, BX - 3, BY + 9); int fRead1 = Add(rd1);
		var rd2 = Pose(1, 3, 4, 3); Glasses(rd2, BX, BY - 3); Book(rd2, BX - 3, BY + 9); rd2.Rect(BX + 2, BY + 8, 2, 2, White); int fRead2 = Add(rd2);
		var lp1 = Pose(8, 3, 2, 3); Laptop(lp1, BX - 4, BY + 9, 0); int fLap1 = Add(lp1);
		var lp2 = Pose(8, 1, 3, 3); Laptop(lp2, BX - 4, BY + 9, 1); int fLap2 = Add(lp2);
		var code = Empty(); St(code, 1, 16, new[] { ".g...g.", "g..g..g", ".g.g.g.", "...g..." }); int fCode = Add(code);
		Frame Loading(int k) { var f = Pose(k % 2 == 0 ? 4 : 0, 3, 3, 3); LoadingRing(f, 6, 4, k); return f; }
		int fLoad1 = Add(Loading(0)), fLoad2 = Add(Loading(1)), fLoad3 = Add(Loading(2)), fLoad4 = Add(Loading(3));
		var dlb = Empty(); DownloadBox(dlb, 13, -8); int fDlBox = Add(dlb);
		var dlc = Pose(2, 0, 4, 3); DownloadBox(dlc, 13, 24); int fDlCatch = Add(dlc);
		var tu = Pose(2, 0, 5, 3); Bubble(tu, 0, -8, check); int fThumbs = Add(tu);
		var okf = Pose(2, 0, 6, 3); Bubble(okf, 0, -8, ok); int fOk = Add(okf);
		var cp = Pose(2, 2, 1, 5); Confetti(cp, 0); int fPopped = Add(cp);
		var cfa = Empty(); Confetti(cfa, 0); int fConf1 = Add(cfa);
		var cfb = Empty(); Confetti(cfb, 1); int fConf2 = Add(cfb);
		var fl1 = Pose(2, 0, 5, 3); Flag(fl1, 3, 6, 0); int fFlag1 = Add(fl1);
		var fl2 = Pose(2, 1, 5, 4); Flag(fl2, 3, 5, 1); int fFlag2 = Add(fl2);
		var er = Pose(3, 2, 1, 4); Warning(er, 1, -4); int fError = Add(er);
		var wn = Empty(); Warning(wn, 1, -4); int fWarn = Add(wn);
		int fCry1 = Add(Pose(7, 1, 2, 2)), fCry2 = Add(Pose(1, 2, 2, 2));
		var an1 = Pose(6, 2, 1, 3); an1.Shift(-1, 0); int fAngry1 = Add(an1);
		var an2 = Pose(6, 0, 2, 3); an2.Shift(1, 0); int fAngry2 = Add(an2);
		var stc = Empty(); Steam(stc, 16, 8); int fSteam = Add(stc);
		var sc1 = Pose(3, 1, 4, 3); sc1.Shift(-1, 0); Sweat(sc1, 26, 12); int fScared1 = Add(sc1);
		var sc2 = Pose(3, 3, 4, 3); sc2.Shift(1, 0); Sweat(sc2, 27, 14); int fScared2 = Add(sc2);
		var um1 = Pose(4, 3, 4, 3); Umbrella(um1, 10, 0); Drop(um1, 2, 4); Drop(um1, 30, 8); int fUmb1 = Add(um1);
		var um2 = Pose(2, 0, 4, 3); Umbrella(um2, 10, 0); Drop(um2, 3, 8); Drop(um2, 31, 2); int fUmb2 = Add(um2);
		var ht1 = Pose(5, 1, 3, 3); Sweat(ht1, 27, 12); Sun(ht1, 1, -8); int fHot1 = Add(ht1);
		var ht2 = Pose(5, 2, 2, 3); Sweat(ht2, 28, 15); Sun(ht2, 1, -8); int fHot2 = Add(ht2);
		var hf1 = Pose(2, 0, 4, 3); Fan(hf1, 3, 20, 0); int fFan1 = Add(hf1);
		var hf2 = Pose(1, 3, 4, 3); Fan(hf2, 3, 20, 1); int fFan2 = Add(hf2);
		// sunbathing, bat style: sunglasses AND an umbrella (bats don't like the sun)
		var sb1 = Pose(2, 0, 4, 3); Shades(sb1, BX, BY - 3); Umbrella(sb1, 10, 0); Sun(sb1, 1, -8); int fSun1 = Add(sb1);
		var sb2 = Pose(2, 3, 4, 3); Shades(sb2, BX, BY - 3); Umbrella(sb2, 10, 0); Sun(sb2, 2, -7); int fSun2 = Add(sb2);
		var ic1 = Pose(2, 0, 4, 3); IceCream(ic1, 3, 22, true); int fIce1 = Add(ic1);
		var ic2 = Pose(1, 4, 4, 3); IceCream(ic2, 3, 22, true); int fIce2 = Add(ic2);
		var cd1 = Pose(1, 3, 4, 3); cd1.Shift(-1, 0); Snow(cd1, 0); int fCold1 = Add(cd1);
		var cd2 = Pose(1, 3, 4, 3); cd2.Shift(1, 0); Snow(cd2, 1); int fCold2 = Add(cd2);
		var sf1 = Pose(2, 0, 3, 3); Scarf(sf1, BX, BY - 3); Snow(sf1, 0); int fScarf1 = Add(sf1);
		var sf2 = Pose(1, 3, 0, 3); Scarf(sf2, BX, BY - 3); Snow(sf2, 1); int fScarf2 = Add(sf2);
		var bn1 = Pose(2, 0, 3, 3); Beanie(bn1, BX, BY - 3); int fBeanie1 = Add(bn1);
		var bn2 = Pose(4, 3, 0, 3); Beanie(bn2, BX, BY - 3); int fBeanie2 = Add(bn2);
		var snowC = Empty(); Snow(snowC, 0); int fSnowC = Add(snowC);
		var snowC2 = Empty(); Snow(snowC2, 1); int fSnowC2 = Add(snowC2);
		var cl1 = Empty(); CloudAt(cl1, 11, -10, 0); int fCloud1 = Add(cl1);
		var cl2 = Empty(); CloudAt(cl2, 11, -10, 1); int fCloud2 = Add(cl2);
		var wet = Pose(7, 3, 2, 2); Drop(wet, 10, 10); Drop(wet, 22, 12); Drop(wet, 6, 18); int fWet = Add(wet);
		var su = Pose(3, 1, 1, 5); Bubble(su, 2, -8, bang); int fSurprised = Add(su);
		int fTired1 = Add(Pose(5, 3, 2, 2)), fTired2 = Add(Pose(1, 1, 2, 1));
		var sl2 = Pose(1, 3, 4, 2); Zed(sl2, 26, 10); int fSleepy2 = Add(sl2);
		var ck1 = Pose(0, 0, 4, 3); FortuneCookie(ck1, 3, 23, false); int fCookie1 = Add(ck1);
		var ck2 = Pose(3, 1, 4, 3); FortuneCookie(ck2, 3, 23, true); int fCookie2 = Add(ck2);
		var slip = Empty(); Slip(slip, 3, 18); int fSlip = Add(slip);
		var sw1 = Pose(5, 1, 3, 3); Sweat(sw1, 27, 12); Sweat(sw1, 3, 10); int fSweat1 = Add(sw1);
		var sw2 = Pose(5, 0, 2, 3); Sweat(sw2, 28, 15); int fSweat2 = Add(sw2);
		var mir = Pose(2, 0, 0, 3); mir.MirrorX(); int fMirror = Add(mir);
		int fNod = Add(Pose(2, 0, 3, 1));
		var pt1 = Pose(2, 0, 5, 3); PartyHat(pt1, BX, BY - 3); int fParty1 = Add(pt1);
		var pt2 = Pose(3, 1, 6, 4); PartyHat(pt2, BX, BY - 4); int fParty2 = Add(pt2);
		var hg1 = Pose(1, 0, 4, 3); HeartAt(hg1, 14, 26); int fHug1 = Add(hg1);
		var hg2 = Pose(2, 0, 4, 3); HeartAt(hg2, 14, 26); HeartAt(hg2, 4, 4); int fHug2 = Add(hg2);
		var hc = Empty(); HeartAt(hc, 6, 8); int fHeartChild = Add(hc);
		var lv1 = Pose(2, 0, 3, 3); HeartAt(lv1, 4, 6); int fLove1 = Add(lv1);
		var lv2 = Pose(2, 0, 0, 4); HeartAt(lv2, 5, 2); HeartAt(lv2, 26, 6); int fLove2 = Add(lv2);
		var swp1 = Pose(8, 3, 4, 3); Broom(swp1, 2, 20, 0); int fSweep1 = Add(swp1);
		var swp2 = Pose(8, 3, 4, 3); Broom(swp2, 2, 20, -1); Dust(swp2, 0, 28); int fSweep2 = Add(swp2);
		var du = Empty(); Dust(du, 2, 28); int fDust = Add(du);
		var wk1 = Pose(8, 3, 4, 3); Hammer(wk1, 2, 18, true); wk1.Rect(0, 30, 9, 2, Brown); int fWork1 = Add(wk1);
		var wk2 = Pose(3, 1, 4, 3); Hammer(wk2, 0, 27, false); wk2.Rect(0, 30, 9, 2, Brown); wk2.Dot(1, 26, Spark).Dot(6, 25, Spark); int fWork2 = Add(wk2);
		var ms1 = Pose(1, 0, 5, 3); Headphones(ms1, BX, BY - 3); int fMusic1 = Add(ms1);
		var ms2 = Pose(2, 1, 6, 4); Headphones(ms2, BX, BY - 4); int fMusic2 = Add(ms2);
		var nc = Empty(); NoteAt(nc, 4, 8); int fNote = Add(nc);
		var cof1 = Pose(0, 3, 4, 3); Cup(cof1, 3, 24); int fCoffee1 = Add(cof1);
		var cof2 = Pose(1, 4, 4, 3); Cup(cof2, 5, 22); int fCoffee2 = Add(cof2);
		var stm = Empty(); St(stm, 4, 16, new[] { ".k.k", "k.k.", ".k.k" }); int fCoffeeSteam = Add(stm);
		int fStretch = Add(Pose(1, 2, 1, 4));
		int fYawn = Add(Pose(1, 2, 3, 3));
		var rv = Pose(0, 3, 4, 3); Glasses(rv, BX, BY - 3); St(rv, BX - 4, BY + 9, new[] { "OOOOOOO", "OWWWWWO", "OWlllWO", "OWWWWWO", "OOOOOOO" }); int fReview = Add(rv);
		var wr = Pose(8, 3, 4, 3); St(wr, BX - 5, BY + 10, new[] { "OOOOOOOO", "OWqqqWWO", "OWqqWWWO", "OOOOOOOO" }); St(wr, BX - 2, BY + 6, new[] { "Y...", ".Y..", "..y." }); int fWrite = Add(wr);
		var dt = Pose(8, 3, 4, 3); St(dt, BX - 6, BY + 4, new[] { ".OOO..", "OAWAO.", "OAAAO.", "OAAAO.", ".OOOn.", "....nn" }); int fDetective = Add(dt);
		var ts = Pose(1, 3, 4, 3); for (int i = 0; i < 9; i++) ts.Rect(BX - 6 + i, BY - 4 + i, 3, 2, i < 3 ? Spark : Brown); int fTelescope = Add(ts);

		p.IconFrame = fHappy;

		// ------------------------------------------------------------ animations
		Anim Show(int id, string name, int ms, params (int f, int n)[] steps)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString());
			foreach (var (f, n) in steps) a.Seq(Enumerable.Repeat(f, n).ToArray());
			a.Then(100, 11);
			return a;
		}
		Anim Loop2(int id, string name, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString()).Loop(loops, 0);
			a.Seq(frames); a.Then(100, 11);
			return a;
		}
		Anim Child(int id, string name, string mx, string my, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move(mx, my).Speed(ms.ToString()).Loop(loops, 0); a.Seq(frames); return a;
		}
		Anim Then(Anim a, int next) { a.Next.Clear(); a.Then(100, next); return a; }
		int nextChild = 200;
		int NewChild(string name, string mx, string my, int ms, string loops, params int[] frames) { int id = nextChild++; Child(id, name, mx, my, ms, loops, frames); return id; }

		// he appears flying, somewhere on the screen
		p.Spawns.Add((1, 60, "random*(screenW-imageW-80)/100+40", "random*(areaH-imageH-160)/100+60", 1));
		p.Spawns.Add((2, 40, "screenW+10", "random*(areaH/2)/100+40", 1));

		// 1 fly: the six-beat flap, small steps; at the borders he turns
		var fly = p.A(1, "walk").Move("-3", "0").Speed("70").Loop("random/4+4").Seq(flap);
		fly.Then(30, 1).Then(16, 11).Then(9, 13).Then(9, 14).Then(5, 2).Then(4, 20).Then(3, 21).Then(3, 23).Then(3, 34).Then(3, 35).Then(3, 12)
			.OnBorder(100, 2);
		var turn = p.A(2, "turn").Move("0", "0").Speed("80").Seq(f3, fMirror, f3); turn.Action = "flip"; turn.Then(100, 1);
		p.A(3, "drag").Move("0", "0").Speed("60").Loop("40").Seq(fDrag1, fDrag2).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("60").Loop("30").Seq(fDrag1, fDrag2).Then(100, 4);
		// a floating pet: "fall" is where he stays after being dropped (no gravity)
		p.A(5, "fall").Move("0", "0").Speed("80").Loop("3").Seq(flapWide).Then(100, 11);
		// 11 hover in place (the idle hub)
		var hub = p.A(11, "blink").Move("0", "0").Speed("80").Loop("random/4+2").Seq(f1, f2, f3, f4, f5, f6, f1, f2, fBlink, f4, f5, f6);
		hub.Then(28, 1).Then(6, 15).Then(6, 17).Then(5, 13).Then(5, 14).Then(4, 20).Then(3, 21).Then(3, 23).Then(3, 24).Then(3, 25)
			.Then(3, 26).Then(3, 27).Then(3, 28).Then(3, 34).Then(3, 35).Then(3, 36).Then(3, 37).Then(3, 38).Then(3, 39).Then(3, 40)
			.Then(3, 41).Then(3, 42).Then(3, 43).Then(3, 44).Then(2, 45).Then(4, 12);
		// 13 fly up, 14 fly down: diagonal flights that bounce off the top and the taskbar
		p.A(13, "fly up").Move("-2", "-3").Speed("70").Loop("random/5+3").Seq(flap).Then(70, 1).Then(30, 11).OnBorder(100, 2, "vertical").OnBorder(100, 14, "horizontal");
		p.A(14, "fly down").Move("-2", "3").Speed("70").Loop("random/5+3").Seq(flap).Then(70, 1).Then(30, 11).OnBorder(100, 2, "vertical").OnBorder(100, 13, "horizontal");
		p.A(15, "look around").Move("0", "0").Speed("120").Seq(f1, f2, f3, fUp, fUp, fUp, f4, f5, fAhead, fAhead, fAhead, f6, f1).Then(100, 11);
		p.A(16, "sit").Move("0", "0").Speed("400").Loop("random/3+4", 0).Seq(fFold, fFold, fFoldBlink, fFold).Then(100, 11);
		p.A(17, "wave").Move("0", "0").Speed("150").Loop("4", 0).Seq(fWave1, fWave2).Then(100, 11);
		Show(18, "hello", 150, (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fHappy, 3));

		// 12 the night: flies up to the top of the screen and hangs there, sleeps, wakes up, lets go
		p.A(12, "fly to the top").Move("-1", "-5").Speed("60").Loop("200", 0).Seq(flap).Then(100, 11)
			.OnBorder(100, 30, "horizontal").OnBorder(100, 2, "vertical");
		p.A(30, "hang").Move("0", "0").Speed("160").Seq(fHangWings, fHangStretch, fHangWings, fHang, fHang).Then(100, 31);
		p.A(31, "hang upside down").Move("0", "0").Speed("400").Loop("random/3+6", 0).Seq(fHang, fHang, fHangBlink, fHang, fHangHappy, fHang).Then(60, 32).Then(40, 33);
		p.A(32, "sleep hanging").Move("0", "0").Speed("600").Loop("random/3+8", 0).Seq(fHangSleep, fHangZ, fHangSleep, fHangZ).Then(100, 33);
		int zUp = NewChild("z floats", "1", "1", 200, "14", Add(((Func<Frame>)(() => { var f = Empty(); Zed(f, 22, 20); return f; }))()));
		p.Anims.First(a => a.Id == zUp).Fade(1, 0);
		p.Childs.Add((32, "imageX", "imageY", zUp));
		p.A(33, "let go").Move("0", "0").Speed("120").Seq(fHangBlink, fHangStretch, fHangWings, fUpside2, fWide, f1, f2).Then(100, 14);

		// ================================================================ bat scenes
		// 20 sonar: rings go out in front of him
		Show(20, "sonar", 120, (sonar[0], 2), (sonar[1], 2), (sonar[2], 2), (fAhead, 4), (sonar[0], 2), (fAhead, 4), (fHappy, 2));
		int ringId = NewChild("sonar rings", "-3", "0", 90, "0", rings[0], rings[1], rings[2], rings[3], rings[0], rings[1], rings[2], rings[3]); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((20, "imageX-imageW*0.5", "imageY", ringId));
		// 21 a moth: he hears it, chases it (it is 2 sprites ahead and slower) and catches it
		Show(21, "moth", 100, (sonar[0], 2), (fAhead, 4), (flapWide[0], 1), (flapWide[1], 1));
		Then(p.Anims.Last(), 22);
		var chase = p.A(22, "chase moth").Move("-4", "0").Speed("60").Loop("21", 0).Seq(flapAhead[0], flapAhead[2], flapAhead[3], flapAhead[5]);
		chase.Then(100, 46).OnBorder(100, 46);
		Show(46, "gulp moth", 140, (fGulp, 3), (fChew, 2), (fGulp, 2), (fChew, 2), (fHappy, 4));
		int mothId = NewChild("moth flies", "-1", "0", 60, "43", mothF[0], mothF[1]);
		p.Childs.Add((21, "imageX-imageW*2", "imageY", mothId));
		// 23 bat friends fly by
		Show(23, "bat friends", 140, (fAhead, 4), (fWide, 2), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fHappy, 4));
		int friends = NewChild("friends fly by", "-6", "-1", 70, "70", mb[0], mb[1]);
		p.Childs.Add((23, "imageX+imageW*2", "imageY", friends));
		// 24 the moon rises behind him
		Show(24, "moon", 200, (fUp, 4), (fLoveMoon, 8), (fFold, 4), (fFoldBlink, 2), (fFold, 4), (fHappy, 2));
		int moonId = NewChild("moon rises", "0", "-1", 220, "16", fMoon); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((24, "imageX+imageW*0.4", "imageY+20", moonId));
		// 25 a spider drops down on its thread right in front of him
		Show(25, "spider", 120, (f3, 6), (fShocked, 6), (flapWide[0], 2), (flapWide[2], 2), (fScared1, 2), (fScared2, 2), (fBlink, 2));
		int spiderId = NewChild("spider dangles", "0", "0", 160, "0", spiders[0], spiders[1], spiders[2], spiders[3], spiders[3], spiders[3], spiders[4], spiders[5]);
		p.Childs.Add((25, "imageX-imageW*0.6", "imageY", spiderId));
		// 26 garlic! he backs away fanning his nose
		Show(26, "garlic", 120, (f3, 4), (fEca, 8));
		Then(p.Anims.Last(), 47);
		var away = p.A(47, "back away").Move("3", "-1").Speed("70").Loop("4", 0).Seq(flapWide); away.Then(100, 11).OnBorder(100, 11);
		int garlicId = NewChild("garlic", "0", "4", 60, "6", fGarlic); Then(p.Anims.Last(), nextChild);
		NewChild("garlic rests", "0", "0", 300, "6", fGarlic); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((26, "imageX-imageW*0.6", "imageY-30", garlicId));
		// 27 a jack-o'-lantern flickers in front of him
		Show(27, "pumpkin", 180, (fAhead, 4), (fHappy, 4), (fDance1, 2), (fDance2, 2), (fDance1, 2), (fDance2, 2), (fHappy, 2));
		int pumpkinId = NewChild("pumpkin glows", "0", "0", 180, "8", pumpkin[0], pumpkin[1]); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((27, "imageX-imageW*0.6", "imageY", pumpkinId));
		// 28 candy rain
		Show(28, "candy rain", 140, (fUp, 4), (flapHappy[0], 1), (flapHappy[1], 1), (flapHappy[2], 1), (flapHappy[3], 1), (flapHappy[4], 1), (flapHappy[5], 1), (fHappy, 4), (fLaugh1, 2), (fLaugh2, 2));
		int candyA = NewChild("candies fall", "0", "3", 110, "16", candy[0], candy[1]); p.Anims.Last().Fade(1, 0.3);
		int candyB = NewChild("candies fall 2", "1", "3", 130, "14", candy[1], candy[0]); p.Anims.Last().Fade(1, 0.3);
		p.Childs.Add((28, "imageX-imageW*0.4", "imageY-imageH*0.4", candyA));
		p.Childs.Add((28, "imageX+imageW*0.4", "imageY-imageH*0.5", candyB));
		// 29 a juice box (grape / tomato / orange)
		Show(29, "juice", 200, (fJuice1, 3), (fJuice2, 3), (fJuice1, 1), (fJuice2, 3), (fJuice3, 4), (fHappy, 2));
		// 34 fireflies
		Loop2(34, "fireflies", 160, "random/3+4", flapHappy[0], flapHappy[2], flapHappy[3], flapHappy[5]);
		int ffly = NewChild("fireflies blink", "-1", "-1", 160, "10", fireflies[0], fireflies[1], fireflies[2], fireflies[3]); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((34, "imageX-imageW*0.5", "imageY", ffly));
		// 35 loop the loop: up, upside down at the top, down
		var loopUp = p.A(35, "loop the loop").Move("-2", "-4").Speed("70").Seq(f1, f2, f3, fHappy, fUpside2, fUpside, fUpside2);
		loopUp.Then(100, 48).OnBorder(100, 48);
		var loopDown = p.A(48, "loop down").Move("-2", "4").Speed("70").Seq(fUpside, fUpside2, fWide, f4, f5, f6, fHappy); loopDown.Then(100, 11).OnBorder(100, 11);
		// 36 squeak: "iii!" floats up
		Show(36, "squeak", 140, (f3, 1), (fSqueak, 4), (fHappy, 3));
		int iiiId = NewChild("squeak bubble", "-1", "-2", 100, "14", fIii); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((36, "imageX", "imageY", iiiId));
		// 37 boo: hides in his wings, then opens them wide
		Show(37, "boo", 140, (fHide, 8), (fBoo, 8), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		// 38 his favourite food: grapes / a tomato / a banana
		Show(38, "favourite food", 220, (fFood0, 3), (fFood1, 3), (fChew, 2), (fFood1, 2), (fFood2, 3), (fChew, 2), (fHappy, 3));
		Show(39, "sneeze", 140, (f3, 1), (fAh, 3), (f3, 1), (fAh, 4), (fChoo, 3), (fBlink, 2));
		Loop2(40, "laugh", 130, "random/3+4", fLaugh1, fLaugh2);
		Loop2(41, "dance", 130, "random/3+5", fDance1, fDance2);
		Loop2(42, "curious", 300, "random/3+3", fCurious, fUp);
		Show(43, "hiccup", 120, (f3, 3), (fHic, 2), (f3, 4), (fHic, 2), (f3, 3), (fHic, 2), (fWide, 2));
		Show(44, "upside down", 200, (fUpside2, 2), (fUpside, 6), (fUpside2, 2), (fHappy, 2));
		Show(45, "dizzy flight", 120, (fDizzy1, 2), (fDizzy2, 2), (fDizzy1, 2), (fDizzy2, 2), (fBlink, 2));

		// ================================================================ the program's features (60-111)
		Loop2(60, "think", 320, "random/3+3", fThink1, fThink1, fThink2, fThink2);
		Show(61, "idea", 160, (fThink1, 4), (fIdea1, 3), (fIdea2, 1), (fIdea1, 1), (fIdea2, 6), (fHappy, 3));
		int bulbId = NewChild("idea bulb", "0", "-2", 90, "14", fBulb); p.Anims.Last().Fade(1, 0); p.Childs.Add((61, "imageX", "imageY", bulbId));
		Loop2(62, "read", 420, "random/3+3", fRead1, fRead1, fRead2, fRead1);
		Loop2(63, "review", 320, "random/3+3", fReview, fRead2);
		Loop2(64, "laptop", 140, "random/3+6", fLap1, fLap2);
		int codeId = NewChild("code floats", "-1", "-2", 110, "16", fCode); p.Anims.Last().Fade(1, 0); p.Childs.Add((64, "imageX", "imageY", codeId));
		Loop2(65, "loading", 140, "random/3+5", fLoad1, fLoad2, fLoad3, fLoad4);
		Show(66, "download", 150, (fUp, 7), (fDlCatch, 6), (fHappy, 3));
		int dlId = NewChild("download box", "0", "8", 60, "8", fDlBox); p.Childs.Add((66, "imageX", "imageY-imageH*0.8", dlId));
		Show(67, "thumbs up", 200, (f3, 1), (fThumbs, 8), (fHappy, 2));
		Show(68, "confetti", 140, (fWide, 2), (fPopped, 6), (fHappy, 3));
		int cA = NewChild("confetti falls", "0", "2", 120, "20", fConf1, fConf2); p.Anims.Last().Fade(1, 0); p.Childs.Add((68, "imageX-imageW*0.4", "imageY-imageH*0.3", cA));
		int cB = NewChild("confetti falls 2", "1", "2", 140, "18", fConf2, fConf1); p.Anims.Last().Fade(1, 0); p.Childs.Add((68, "imageX+imageW*0.4", "imageY-imageH*0.4", cB));
		Loop2(69, "flag", 200, "random/3+5", fFlag1, fFlag2);
		Show(70, "error", 130, (fWide, 2), (fError, 8), (fScared1, 2), (fScared2, 2), (fBlink, 2));
		int warnId = NewChild("warning up", "0", "-2", 100, "12", fWarn); p.Anims.Last().Fade(1, 0); p.Childs.Add((70, "imageX", "imageY", warnId));
		Loop2(71, "cry", 220, "random/3+4", fCry1, fCry2);
		Loop2(72, "angry", 110, "random/3+6", fAngry1, fAngry2);
		int steamId = NewChild("angry steam", "1", "-2", 100, "12", fSteam); p.Anims.Last().Fade(1, 0); p.Childs.Add((72, "imageX", "imageY", steamId));
		Show(73, "shocked", 110, (f3, 2), (fShocked, 8), (fBlink, 2));
		Loop2(74, "scared", 70, "random/3+10", fScared1, fScared2);
		var talk = p.A(75, "talk").Move("0", "0").Speed("160").Loop("random/3+8", 0);
		talk.Seq(fTalk, f3, fTalk, fHappy, f3, f3); talk.Then(100, 11);
		Loop2(76, "umbrella", 220, "random/3+5", fUmb1, fUmb2);
		Loop2(77, "hot", 240, "random/3+4", fHot1, fHot2);
		Loop2(78, "hand fan", 120, "random/3+8", fFan1, fFan2);
		Loop2(79, "sunbathing", 500, "random/3+3", fSun1, fSun1, fSun2);
		Show(80, "ice cream", 200, (fIce1, 3), (fIce2, 3), (fIce1, 2), (fIce2, 3), (fHappy, 2));
		Loop2(81, "cold", 70, "random/3+10", fCold1, fCold2);
		Loop2(82, "shiver", 70, "random/3+8", fCold1, fCold2);
		Loop2(83, "scarf", 360, "random/3+3", fScarf1, fScarf2);
		Loop2(84, "snow cap", 320, "random/3+4", fBeanie1, fBeanie2);
		int snowId = NewChild("snowfall", "0", "1", 200, "16", fSnowC, fSnowC2); p.Anims.Last().Fade(1, 0); p.Childs.Add((84, "imageX-imageW*0.5", "imageY-imageH*0.2", snowId));
		Show(85, "rain cloud", 160, (fUp, 6), (fWide, 2), (fWet, 10), (fDizzy1, 1), (fDizzy2, 1), (fBlink, 2), (fHappy, 2));
		int rainId = NewChild("cloud rains", "0", "0", 150, "12", fCloud1, fCloud2); int rainAway = nextChild; Then(p.Anims.Last(), rainAway);
		NewChild("cloud leaves", "-3", "-1", 100, "12", fCloud1, fCloud2); p.Anims.Last().Fade(1, 0);
		p.Childs.Add((85, "imageX", "imageY", rainId));
		Show(86, "look up", 280, (f3, 1), (fUp, 6), (fBlink, 1), (fUp, 4));
		Show(87, "surprised", 120, (f3, 2), (fSurprised, 6), (fWide, 3));
		Loop2(88, "tired", 400, "random/3+3", fTired1, fTired2);
		Show(89, "sleepy", 300, (fTired1, 3), (fTired2, 3), (fSleepy2, 4), (fWide, 1), (fTired1, 3), (fSleepy2, 5), (fBlink, 2));
		Show(90, "drink", 200, (fJuice1, 3), (fJuice2, 3), (fJuice1, 1), (fJuice2, 3), (fJuice3, 4));
		Show(91, "eat", 220, (fFood0, 3), (fFood1, 3), (fChew, 2), (fFood2, 3), (fChew, 2), (fHappy, 3));
		Show(92, "cookie", 200, (fCookie1, 4), (fCookie2, 3));
		Then(p.Anims.Last(), 93);
		Show(93, "fortune", 200, (fWide, 3), (fUp, 5), (fThink1, 3), (fHappy, 3));
		int slipId = NewChild("fortune slip", "-1", "-2", 110, "14", fSlip); p.Anims.Last().Fade(1, 0); p.Childs.Add((93, "imageX", "imageY", slipId));
		Loop2(94, "sweat", 160, "random/3+5", fSweat1, fSweat2);
		Loop2(95, "dizzy", 130, "random/3+5", fDizzy1, fDizzy2);
		Loop2(96, "spin", 90, "random/3+3", f3, fUpside2, fMirror, fUpside);
		Show(97, "nod", 180, (f3, 1), (fNod, 2), (f3, 2), (fNod, 2), (fHappy, 2));
		Show(98, "ok", 200, (f3, 1), (fOk, 8), (fHappy, 2));
		Loop2(99, "kill", 80, "6", f1, f2, f3, f4, f5, f6);
		p.Anims.Last().Move("-3", "-6").Fade(1, 0); p.Anims.Last().Next.Clear(); p.Anims.Last().Then(100, 99);
		Loop2(100, "cheer", 100, "random/3+4", flapHappy[0], flapHappy[1], flapHappy[2], flapHappy[3], flapHappy[4], flapHappy[5]);
		Loop2(101, "party", 160, "random/3+5", fParty1, fParty2);
		Show(102, "jump", 80, (f4, 2), (fWide, 2), (fUpside2, 1), (fUpside, 1), (fUpside2, 1), (fHappy, 3));
		Loop2(103, "happy", 90, "random/3+3", flapHappy);
		Loop2(104, "hug", 320, "random/3+3", fHug1, fHug2);
		Loop2(105, "hearts", 260, "random/3+3", fLove1, fLove2);
		int hA = NewChild("heart up", "-1", "-2", 100, "14", fHeartChild); p.Anims.Last().Fade(1, 0); p.Childs.Add((105, "imageX", "imageY", hA));
		int hB = NewChild("heart up 2", "1", "-2", 120, "12", fHeartChild); p.Anims.Last().Fade(1, 0); p.Childs.Add((105, "imageX+imageW*0.4", "imageY", hB));
		Loop2(106, "love", 260, "random/3+3", fLove1, fLove2);
		Loop2(107, "sweep", 160, "random/3+6", fSweep1, fSweep2);
		int dustId = NewChild("sweep dust", "-2", "-1", 120, "12", fDust); p.Anims.Last().Fade(1, 0); p.Childs.Add((107, "imageX", "imageY", dustId));
		Loop2(108, "work", 150, "random/3+6", fWork1, fWork2);
		Loop2(109, "music", 200, "random/3+6", fMusic1, fMusic2);
		int nA = NewChild("note up", "-1", "-2", 110, "20", fNote, fNote); p.Anims.Last().Fade(1, 0); p.Childs.Add((109, "imageX", "imageY", nA));
		int nB = NewChild("note up 2", "1", "-2", 130, "18", fNote, fNote); p.Anims.Last().Fade(1, 0); p.Childs.Add((109, "imageX+imageW*0.3", "imageY-10", nB));
		Show(110, "coffee", 240, (fCoffee1, 3), (fCoffee2, 3), (fCoffee1, 2), (fCoffee2, 3), (fHappy, 4));
		int stmId = NewChild("coffee steam", "0", "-1", 140, "12", fCoffeeSteam); p.Anims.Last().Fade(1, 0); p.Childs.Add((110, "imageX", "imageY", stmId));
		Show(111, "stretch", 220, (f3, 1), (fStretch, 5), (fWave1, 2));
		Show(112, "yawn", 220, (f3, 1), (fYawn, 5), (fBlink, 2));
		Loop2(113, "sleep", 500, "random/3+6", fFold, fFoldBlink, fFoldBlink, fFoldBlink);
		p.Anims.Last().Seq(Enumerable.Repeat(Add(((Func<Frame>)(() => { var f = Pose(1, 3, 4, 2); Zed(f, 26, 10); return f; }))()), 2).ToArray());
		p.Childs.Add((113, "imageX", "imageY", zUp));
		Show(114, "write", 260, (fWrite, 4), (fAhead, 1), (fWrite, 4));
		Loop2(115, "detective", 300, "random/3+3", fDetective, fDetective, fAhead, fDetective);
		Loop2(116, "telescope", 320, "random/3+3", fTelescope, fTelescope, fUp);
		Loop2(117, "dance party", 110, "random/3+6", fDance1, fDance2, fParty1, fParty2);
		foreach (int id in new[] { 60, 62, 64, 69, 76, 78, 90, 91, 101, 104, 105, 107, 109, 110, 111, 116 }) hub.Then(2, id);

		p.Readme = $$"""
			# {{names[v]}}

			## Description

			{{new[] { "A little purple bat in pixel art, with big pink ears and two tiny fangs.", "A little vampire bat in pixel art: dark gray, yellow eyes and wings lined in red like a cape.", "A little fruit bat in pixel art: brown with an orange belly and a little nose." }[v]}}

			He doesn't walk: he flies all over the screen with a soft six-beat flap and stays wherever
			you drop him. Now and then he flies up to the top of the screen, hangs upside down and
			sleeps. He uses his sonar, chases moths, meets his bat friends, admires the moon, gets
			scared by a spider, can't stand garlic, says BOO and eats his favourite food.

			Part of the "Morceguinho" family: the program shows the three bats on one card with color dots.

			## About pet

			Author: Luan Felipe

			## Animations

			* fly (six-beat flap), turn, fly up and down, hover, look around, wave, hello
			* fly to the top of the screen, hang upside down, sleep hanging, let go
			* sonar, chase a moth, bat friends, the moon, a spider, garlic, a jack-o'-lantern, candy rain, fireflies
			* loop the loop, squeak, boo, juice box, favourite food, sneeze, laugh, dance, hiccup, upside down
			* reacts to the app: thinks, reads, types on a laptop and celebrates with Claude Code; drinks on the water
			  reminder; umbrella, fan, scarf and snow cap with the weather; party at the end of a Pomodoro
			* drag with the mouse (he has no gravity: he stays where you drop him)
			""";
		p.ReadmePt = $$"""
			# {{new[] { "Morceguinho Roxo", "Morceguinho Vampiro", "Morceguinho das Frutas" }[v]}}

			## Descrição

			{{new[] { "Um morceguinho roxo em pixel art, com orelhonas cor-de-rosa e dois dentinhos.", "Um morceguinho vampiro em pixel art: cinza-escuro, olhos amarelos e asas forradas de vermelho como uma capa.", "Um morceguinho das frutas em pixel art: marrom, com a barriga laranja e um narizinho." }[v]}}

			Ele não anda: voa pela tela toda batendo as asas bem suave e fica onde você soltar.
			De vez em quando sobe até o alto da tela, fica de cabeça para baixo e dorme. Usa o
			sonar, persegue mariposas, encontra os amigos morcegos, admira a lua, se assusta com
			uma aranha, não suporta alho, faz BUU e come a comida preferida.

			Faz parte da família "Morceguinho": o programa mostra os três morcegos num cartão só, com bolinhas de cor.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* voar (batida de asas em seis tempos), virar, subir e descer voando, pairar, olhar ao redor, dar tchau, oi
			* subir até o alto da tela, ficar de cabeça para baixo, dormir pendurado, soltar
			* sonar, perseguir mariposa, amigos morcegos, a lua, uma aranha, alho, abóbora iluminada, chuva de doces, vaga-lumes
			* looping, guinchar, buu, caixinha de suco, comida preferida, espirro, risada, dança, soluço, de cabeça para baixo
			* reage ao programa: pensa, lê, digita no notebook e comemora com o Claude Code; bebe no lembrete de água;
			  guarda-chuva, leque, cachecol e gorro conforme o clima; festa no fim do Pomodoro
			* arrastar com o mouse (ele não tem gravidade: fica onde você soltar)
			""";

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
