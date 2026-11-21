using System.Drawing;

namespace PetGen;

/// <summary>
/// "Pipoca" — a little yellow chick in pixel art: the round body of reference A, with its tuft of feathers.
/// He is put together from parts (body, eye, beak, wing, feet) so every pose is a new mix of the same parts.
/// Many scenes use child windows: a worm that pops out far away on the taskbar (he runs to eat it), a hawk that flies in
/// from behind, circles over him while he hides in his egg and gives up, the mother hen walking by, a corn kernel that
/// pops into popcorn, a frog, a snail, a ladybug, a butterfly he chases...
/// Like every DesktopPet pet he faces left. Frames are 32 px wide with 6 rows of headroom; the feet stand on row 31.
/// A child placed with "imageX-imageW*n" appears in front of him; when he faces right the program mirrors the
/// position and the movement, so the scenes work on both sides.
/// </summary>
static class PintinhoPet
{
	const int W = 32, Head = 6;
	const int AX = 5, AY = 10;   // where the body art sits (its row 21 is the frame's row 31)
	const char Out = 'O', Yel = 'Y', YelLight = 'L', YelDark = 'y', Eye = 'E', White = 'W', Beak = 'B', BeakDark = 'b';
	const char Foot = 'F', Cheek = 'P', Shell = 'S', ShellShade = 's', Brown = 'N', BrownLight = 'n', BrownDark = 'd';
	const char Red = 'R', Worm = 'M', WormDark = 'm', Green = 'G', GreenLight = 'g', Gray = 'k', Cloud = 'c', Water = 'A';
	const char Note = 'q', Heart = 'H', HeartLight = 'h', Zz = 'Z', Blue = 'U', BlueLight = 'u', Line = 'l', Spark = 'X', Leaf = 'V';

	// the round body (reference A) without eye, beak, wing and feet; rows 0-2 are the tuft (hidden by the hat)
	static readonly string[] BodyArt =
	{
		"..........O...........",
		".........OYO.O........",
		"........OYYOYO........",
		"......OOOYYYOOO.......",
		"....OOYYYYYYYYYOO.....",
		"...OYYYLLYYYYYYYYO....",
		"..OYYLLYYYYYYYYYYYO...",
		"..OYYLYYYYYYYYYYYYO...",
		".OYYYYYYYYYYYYYYYYYO..",
		"OYYYYYYYYYYYYYYYYYYO..",
		"OYYYYYYYYYYYYYYYYYYYO.",
		".OYYYYYYYYYYYYYYYYYYO.",
		".OYYYYYYYYYYYYYYYYYYO.",
		".OYYYYYYYYYYYYYYYYYYO.",
		".OYYYYYYYYYYYYYYYYYYO.",
		"..OYYYYYYYYYYYYYYYYO..",
		"..OyYYYYYYYYYYYYYYyO..",
		"...OyyYYYYYYYYYYyyO...",
		"....OOyyyyyyyyyyOO....",
		"......OOOOOOOOOO......",
	};
	// the eggshell hat (reference C, with a stronger zigzag edge)
	static readonly string[] HatArt =
	{
		"...OOOOOOO...",
		"..OSSSSSSSO..",
		".OSSSsSSSSSO.",
		".OSSSSSSSsSO.",
		".OSOSSOSSOSO.",
		"..O.OO.OO.O..",
	};
	// the whole egg he hides in (20 x 20)
	static readonly string[] EggArt =
	{
		".......OOOOOO.......",
		".....OOSSSSSSOO.....",
		"....OSSSSSSSSSSO....",
		"...OSSSsSSSSSSSSO...",
		"..OSSSSSSSSSsSSSSO..",
		"..OSSSSSSSSSSSSSSO..",
		".OSSsSSSSSSSSSSSSSO.",
		".OSSSSSSSSSSSSSsSSO.",
		".OSOSSSOSSSOSSSOSSO.",
		"OSOSOSOSOSOSOSOSOSSO",
		"OSSSSSSSSSSSSSSSSSSO",
		"OSSSSsSSSSSSSSSSSSSO",
		"OSSSSSSSSSSSSsSSSSSO",
		".OSSSSSSSSSSSSSSSSO.",
		".OSSSSSsSSSSSSSSSSO.",
		"..OSSSSSSSSSSSSSSO..",
		"...OSSSSSSSSSSSSO...",
		"....OOSSSSSSSSOO....",
		"......OOOOOOOO......",
	};
	// the hawk (gaviao), wings up and wings down, flying left
	static readonly string[] HawkUp =
	{
		"......................OO......",
		"....................OOdNO.....",
		"..................OOdNNNO.....",
		"................OOdNNNnO......",
		"..............OOdNNNNnO.......",
		"............OOdNNNNnnO........",
		"...OOO.....OdNNNNnnO..........",
		"..OnnnO...OdNNNnnO............",
		".OXnEnnOOONNNNNNNOOOOO........",
		"OXXnnnnNNNNNNNNNNNNNNdOO......",
		".OXOnnnnnnNNNNNNNNNNNNNdOO....",
		"..O.OnnnnnnnnNNNNNNNNNNNNdO...",
		"......OOOnnnnnnNNNNNOOOOOO....",
		"..........OOOOOOOOdNO.........",
		"..................OO..........",
	};
	static readonly string[] HawkDown =
	{
		"..............................",
		"..............................",
		"..............................",
		"...OOO........................",
		"..OnnnO.......................",
		".OXnEnnOOOOOOOOOOOOO..........",
		"OXXnnnnNNNNNNNNNNNNNdOO.......",
		".OXOnnnnnnNNNNNNNNNNNNdOO.....",
		"..O.OnnnnnnnNNNNNNNNNNNNdOO...",
		"......OOnnnnNNNNNNNNNNNNNNdO..",
		"........OOdNNNNNNNNOOOOOOOO...",
		"..........OdNNNNNNNO..........",
		"...........OdNNNNNNNO.........",
		"............OOdNNNNNNO........",
		"..............OOOdNNNNO.......",
		".................OOOOO........",
	};
	// the mother hen (galinha), walking left
	static readonly string[] HenArt =
	{
		"....RR.R......................",
		"...RRRRRR.....................",
		"..OOOOOOOO....................",
		".OnnnnnnnnO.............OOO...",
		"OnnEWnnnnnnO...........OnnNO..",
		"BBnnnnnnnnnO..........OnNNNO..",
		"OBBnnnnnnnnnO........OnNNNO...",
		".ORnnnnnnnnnnO......OnNNNNO...",
		"..RRnnnnnnnnnnOOOOOOnNNNNO....",
		"...OnnnnnnnnnnnnnnnnNNNNNO....",
		"...OnNNnnnnnnnnnnnnnNNNNNO....",
		"...OnNNNNnnnnnnnnnnnNNNNO.....",
		"...OnnNNNNNNnnnnnnnnnnNO......",
		"....OnnnNNNNNNNnnnnnnnO.......",
		".....OnnnnnnnnnnnnnnnO........",
		"......OOnnnnnnnnnnnOO.........",
		"........OOOOOOOOOOO...........",
	};

	public static PetDef Build()
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		var p = new PetDef
		{
			Folder = "pipoca",
			Title = "Pipoca the Chick",
			PetName = "Pipoca",
			Author = "Luan Felipe",
			Version = "1.0",
			Info = "Pipoca is a little round yellow chick.[br]" +
				   "He pecks the ground, chases worms and butterflies, follows his mom,[br]" +
				   "hides in his egg when the hawk flies over and pops corn into popcorn.",
			Palette = new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(110, 66, 20),
				[Yel] = Color.FromArgb(255, 217, 59), [YelLight] = Color.FromArgb(255, 242, 166), [YelDark] = Color.FromArgb(232, 178, 30),
				[Eye] = Color.FromArgb(30, 26, 22), [White] = Color.FromArgb(255, 255, 255),
				[Beak] = Color.FromArgb(255, 154, 31), [BeakDark] = Color.FromArgb(217, 106, 14), [Foot] = Color.FromArgb(255, 154, 31),
				[Cheek] = Color.FromArgb(255, 157, 176), [Shell] = Color.FromArgb(251, 247, 238), [ShellShade] = Color.FromArgb(217, 210, 194),
				[Brown] = Color.FromArgb(134, 88, 48), [BrownLight] = Color.FromArgb(196, 150, 100), [BrownDark] = Color.FromArgb(84, 52, 26),
				[Red] = Color.FromArgb(222, 44, 44), [Worm] = Color.FromArgb(236, 140, 150), [WormDark] = Color.FromArgb(196, 96, 110),
				[Green] = Color.FromArgb(64, 160, 70), [GreenLight] = Color.FromArgb(130, 210, 110), [Gray] = Color.FromArgb(150, 150, 156),
				[Cloud] = Color.FromArgb(226, 230, 238), [Water] = Color.FromArgb(110, 180, 240), [Note] = Color.FromArgb(120, 84, 220),
				[Heart] = Color.FromArgb(232, 62, 98), [HeartLight] = Color.FromArgb(255, 150, 170), [Zz] = Color.FromArgb(150, 170, 240),
				[Blue] = Color.FromArgb(50, 110, 220), [BlueLight] = Color.FromArgb(130, 180, 245), [Line] = Color.FromArgb(120, 120, 126),
				[Spark] = Color.FromArgb(255, 232, 90), [Leaf] = Color.FromArgb(76, 150, 60),
			},
			Scale = 2,
			TilesX = 8,
		};

		// ------------------------------------------------------------ helpers
		void St(Frame f, int x, int y, string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		string[] Mirror(string[] rows) => rows.Select(r => new string(r.Reverse().ToArray())).ToArray();
		Frame Empty() => new Frame();

		// eyes: 0 open, 1 closed, 2 happy ^, 3 wide, 4 up, 5 half (tired), 6 angry, 7 teary, 8 back (looking behind)
		void Eyes(Frame f, int x, int y, int mode)
		{
			// (x, y) = top-left of the 2x2 eye
			switch (mode)
			{
				case 0: f.Dot(x, y, Eye).Dot(x + 1, y, Eye).Dot(x, y + 1, Eye).Dot(x + 1, y + 1, White); break;
				case 1: f.Rect(x - 1, y + 1, 4, 1, Eye); break;
				case 2: f.Dot(x - 1, y + 1, Eye).Dot(x, y, Eye).Dot(x + 1, y, Eye).Dot(x + 2, y + 1, Eye); break;
				case 3: f.Rect(x, y - 1, 2, 3, Eye).Dot(x + 1, y - 1, White); break;
				case 4: f.Dot(x, y - 1, Eye).Dot(x + 1, y - 1, Eye).Dot(x, y, Eye).Dot(x + 1, y, White); break;
				case 5: f.Rect(x - 1, y, 4, 1, Out).Dot(x, y + 1, Eye).Dot(x + 1, y + 1, Eye); break;
				case 6: f.Dot(x, y, Eye).Dot(x + 1, y + 1, Eye).Dot(x, y + 1, Eye).Dot(x - 1, y - 1, Out).Dot(x, y - 1, Out).Dot(x + 1, y, Out); break;
				case 7: f.Dot(x, y, Eye).Dot(x + 1, y, Water).Dot(x, y + 1, Water).Dot(x + 1, y + 1, Eye).Dot(x, y + 2, Water).Dot(x, y + 3, Water); break;
				case 8: f.Dot(x + 1, y, Eye).Dot(x + 2, y, Eye).Dot(x + 1, y + 1, Eye).Dot(x + 2, y + 1, White); break;
			}
		}
		// beak: 0 closed, 1 open, 2 wide
		void BeakAt(Frame f, int x, int y, int mode)
		{
			if (mode == 0) St(f, x, y, new[] { "OBB.", "ObBB", ".OBB" });
			else if (mode == 1) St(f, x, y - 1, new[] { ".OBB", "OBBB", "OEEY", "ObbB", ".OB." });
			else St(f, x, y - 1, new[] { ".OBB", "OBBB", "OEEY", "OEEY", "ObbB", ".OB." });
		}
		// wing on his back side: 0 folded (an oval line), 1 up, 2 out/down, 3 waving (high), 4 forward (holding something)
		void WingAt(Frame f, int x, int y, int mode)
		{
			switch (mode)
			{
				case 0: St(f, x + 13, y + 10, new[] { "..yy.", ".y..y", "y...y", ".yyy." }); break;
				case 1: St(f, x + 16, y + 5, new[] { "..OO.", ".OYYO", "OYYYO", "OYYO.", ".OO.." }); St(f, x + 13, y + 10, new[] { "..yy.", ".y..y" }); break;
				case 2: St(f, x + 18, y + 11, new[] { ".OO..", "OYYO.", "OYYYO", ".OYYO", "..OO." }); break;
				case 3: St(f, x + 17, y + 1, new[] { "...OO", "..OYO", ".OYYO", "OYYO.", "OYO..", ".O..." }); break;
				case 4: St(f, x - 1, y + 13, new[] { ".OO.", "OYYO", "OYYO", ".OO." }); break;
			}
		}
		// feet: 0 stand, 1 left up, 2 right up, 3 tucked (jumping), 4 apart (running)
		void Feet(Frame f, int mode, int dy = 0)
		{
			int lx = AX + 6, rx = AX + 12, y = AY + 20 + dy;
			(int l, int r) lift = mode switch { 1 => (1, 0), 2 => (0, 1), _ => (0, 0) };
			if (mode == 3) { f.Rect(lx + 1, y, 1, 1, Foot).Rect(rx + 1, y, 1, 1, Foot); return; }
			if (mode == 4) { lx -= 2; rx += 2; }
			St(f, lx, y - lift.l, new[] { ".F.", "FFF" });
			St(f, rx, y - lift.r, new[] { ".F.", "FFF" });
		}
		void Hat(Frame f, int x, int y, int tilt = 0) => St(f, x + 4 + tilt, y - 2, HatArt);
		void Cheeks(Frame f, int x, int y) => f.Rect(x + 5, y + 10, 2, 2, Cheek);

		// a pose: dx/dy move the body (lean, squash), up lifts everything (jump)
		Frame Pose(int eyes = 0, int beak = 0, int wing = 0, int feet = 0, int dx = 0, int dy = 0, int up = 0, bool hat = false, bool cheeks = true, int hatTilt = 0)
		{
			var f = new Frame();
			int x = AX + dx, y = AY - up;   // dy squashes the body below (the feet stay on the floor)
			var body = hat ? BodyArt.Select((r, i) => i < 3 ? new string('.', r.Length) : r).ToArray() : BodyArt;
			St(f, x, y, body);
			if (wing != 4) WingAt(f, x, y, wing);
			if (cheeks) Cheeks(f, x, y);
			Eyes(f, x + 7, y + 7, eyes);
			BeakAt(f, x, y + 9, beak);
			if (wing == 4) WingAt(f, x, y, wing);
			Feet(f, feet, -up);
			if (hat) Hat(f, x, y, hatTilt);
			// squash: a row of the belly (below the beak and the wing) goes away dy times; everything above comes down
			for (int i = 0; i < dy; i++)
			{
				for (int yy = AY + 16; yy > f.MinY; yy--) for (int xx = 0; xx < W; xx++) f[xx, yy] = f[xx, yy - 1];
				for (int xx = 0; xx < W; xx++) f[xx, f.MinY] = '.';
			}
			return f;
		}
		Frame Low(int n, int eyes = 0, int beak = 0, int wing = 0) => Pose(eyes, beak, wing, 0, 0, n);

		// the egg he hides in: 0 still, 1 wobble left, 2 wobble right, 3 peeking (the top lifts, two eyes in the gap)
		Frame Egg(int k)
		{
			var f = new Frame();
			int x = 6 + (k == 1 ? -1 : k == 2 ? 1 : 0), y = 13;
			if (k == 3)
			{
				St(f, x, y - 3, EggArt.Take(9).ToArray());
				St(f, x + 1, y + 6, new[] { "OYYYYYYYYYYYYYYYYO", "OYYEEYYYYEEYYYYYYO", "OYYYYYYYYYYYYYYYYO" });
				St(f, x, y + 9, EggArt.Skip(9).ToArray());
			}
			else St(f, x, y, EggArt);
			f.Rect(x + 6, 31, 2, 1, Foot).Rect(x + 12, 31, 2, 1, Foot);
			return f;
		}

		// small things
		void HeartAt(Frame f, int x, int y) => St(f, x, y, new[] { "HH.HH", "HhHHH", ".HHH.", "..H.." });
		void NoteAt(Frame f, int x, int y) => St(f, x, y, new[] { ".qq", ".q.", "qq." });
		void Drop(Frame f, int x, int y) => f.Dot(x, y, Water).Dot(x, y + 1, Water);
		void Zed(Frame f, int x, int y) => St(f, x, y, new[] { "ZZZZ", "..Z.", ".Z..", "ZZZZ" });
		void Sparks(Frame f, int k) { foreach (var (sx, sy) in new[] { (3, 6), (27, 8), (1, 20), (29, 18), (14, 2) }) f.Dot(sx + k, sy - k, Spark); }
		void Bubble(Frame f, string[] glyphs)
		{
			St(f, 0, -6, new[] { ".OOOOOOOOOOO.", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", ".OOOOOOOOOOO.", "....OWO......", ".....O......." });
			St(f, 2, -5, glyphs);
		}
		string[] piu = { "EEE.E.E.E.E", "E.E...E.E.E", "EEE.E.E.E.E", "E...E.E.E..", "E...E..EE.E" };
		string[] ok = { "..EEE.E.E..", "..E.E.EE...", "..E.E.E....", "..E.E.EE...", "..EEE.E.E.." };
		string[] check = { ".........GG", "........GG.", "GG.....GG..", ".GG..GG....", "..GGGG....." };
		string[] kkk = { "E...E...E..", "E.E.E.E.E.E", "EE..EE..EE.", "E.E.E.E.E.E", "E.E.E.E.E.E" };
		string[] bang = { ".....E.....", ".....E.....", ".....E.....", "...........", ".....E....." };
		string[] question = { "....EEE....", "......E....", ".....E.....", "...........", ".....E....." };
		void WormAt(Frame f, int x, int stage, int k)
		{
			f.Rect(x - 1, 31, 7, 1, BrownDark).Rect(x, 30, 5, 1, Brown);
			if (stage == 0) return;
			if (stage == 1) { St(f, x + 1, 27, new[] { ".MM", "MEM", ".Mm" }); return; }
			St(f, x + (k == 0 ? 0 : 1), 23, k == 0
				? new[] { ".MM.", "MEMm", ".Mm.", "Mm..", ".Mm.", "..Mm", ".Mm." }
				: new[] { ".MM.", "mMEM", ".mM.", "..mM", ".mM.", "mM..", ".Mm." });
		}
		void Kernel(Frame f, int x, int y) => St(f, x, y, new[] { ".YO", "YyY", "Oy." });
		void Popcorn(Frame f, int x, int y) => St(f, x, y, new[] { ".kkk.", "kWWWk", "WWYWW", "kWWWk", ".kWk." });
		void Butterfly(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "UU.UU", "uUEUu", ".UEU.", "U...U" } : new[] { ".....", ".UEU.", ".uEu.", "....." });
		void Ladybug(Frame f, int x, int k) => St(f, x, 27, new[] { ".EERR.", "EEREER", "ERRRRE", k == 0 ? ".E..E." : "E....E" });
		void Snail(Frame f, int x, int k) => St(f, x, 24, new[] { "..E.E...", "..l.l.OO", "...lOnNO", "..lOnNnNO", ".lllNnNNO", k == 0 ? "llllllllO" : ".lllllllO" }.Select(r => r.Replace('l', 'g')).ToArray());
		void Frog(Frame f, int x, int y, int k) => St(f, x, y, k == 0
			? new[] { ".WE.WE..", "GGGGGGG.", "GgggggGG", "GGGGGGGG", ".G.G..G." }
			: new[] { ".WE.WE..", "GGGGGGG.", "GgggggGG", "GG.GG.GG", "G.....G." });
		void Dandelion(Frame f, int x, int y, bool full)
		{
			f.Rect(x + 2, y + 4, 1, 7, Green);
			if (full) St(f, x, y, new[] { ".cWc.", "cWWWc", "WWWWW", "cWWWc", ".cWc." });
			else f.Dot(x + 2, y + 3, Brown);
		}
		void Seed(Frame f, int x, int y) => St(f, x, y, new[] { "cWc", ".k.", ".k." });
		void Balloon(Frame f, int x, int y, int k) { St(f, x, y, new[] { ".RRR.", "RRWRR", "RRRRR", ".RRR.", "..R.." }); for (int i = 0; i < 6; i++) f.Dot(x + 2 + (i % 2 == 0 ? 0 : k), y + 5 + i, Line); }
		void Feather(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { ".Y.", "YLY", "YYy", ".y." } : new[] { "..Y", ".YL", "YYy", "y.." });
		void Footprint(Frame f, int x) => St(f, x, 30, new[] { "k.k.k", ".kkk.", "..k.." });
		void Flower(Frame f, int x, int stage)
		{
			f.Rect(x, 31, 7, 1, BrownDark).Rect(x + 1, 30, 5, 1, Brown);
			if (stage >= 1) f.Rect(x + 3, 28, 1, 2, Leaf).Dot(x + 2, 28, Leaf).Dot(x + 4, 27, Leaf);
			if (stage >= 2) { f.Rect(x + 3, 24, 1, 4, Leaf); St(f, x, 24, new[] { "VV.....", ".VV...." }); St(f, x + 4, 25, new[] { ".VV", "VV." }); }
			if (stage >= 3) St(f, x + 1, 19, new[] { ".HXH.", "HHXHH", ".HXH.", "..V.." });
		}
		void Dust(Frame f, int x, int y) => St(f, x, y, new[] { "..kk..", ".kcck.", "kcckck", ".kkkk." });
		void CloudAt(Frame f, int x, int y, int k)
		{
			St(f, x, y, new[] { "...kkkk.....", ".kkcccckk...", "kccccccccckk", "kcccccccccck", ".kkkkkkkkkk." });
			for (int i = 0; i < 5; i++) Drop(f, x + 1 + i * 2 + k, y + 6 + ((i + k) % 2) * 3);
		}
		void LeafUmbrella(Frame f, int x, int y) { St(f, x, y, new[] { "....VVVVV....", "..VVgVVVgVV..", ".VVVVgVgVVVV.", "VVVVVVgVVVVVV", "V.V.V.N.V.V.V" }); f.Rect(x + 6, y + 5, 1, 9, Brown); }
		void Glasses(Frame f, int x, int y) { f.Rect(x, y, 4, 1, Eye).Rect(x, y + 3, 4, 1, Eye).Rect(x, y, 1, 4, Eye).Rect(x + 3, y, 1, 4, Eye).Rect(x + 4, y + 1, 4, 1, Eye); }
		void Book(Frame f, int x, int y) => St(f, x, y, new[] { "OUUUUOUUUUO", "OWWWWOWWWWO", "OWllWOWllWO", "OWWWWOWWWWO", ".OOOOOOOOO." });
		void Laptop(Frame f, int x, int y, int k) { St(f, x, y, new[] { "OOOOOOOOO", "OUUUUUUUO", "OUGGUUUUO", "OUUXXUUUO", "OOOOOOOOO", "OlllllllO", ".OOOOOOO." }); if (k == 1) f.Dot(x + 5, y + 2, Green).Dot(x + 6, y + 2, Green); }
		void Bulb(Frame f, int x, int y, bool lit) => St(f, x, y, new[] { ".XXX.", "XXXXX", "XXWXX", ".XXX.", ".lll." }.Select(r => lit ? r : r.Replace('X', 'c').Replace('W', 'k')).ToArray());
		void LoadingRing(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (0, -3), (2, -2), (3, 0), (2, 2), (0, 3), (-2, 2), (-3, 0), (-2, -2) };
			for (int i = 0; i < 8; i++) f.Dot(cx + pts[i].Item1, cy + pts[i].Item2, ((i - k * 2) % 8 + 8) % 8 < 3 ? Blue : Cloud);
		}
		void DownloadBox(Frame f, int x, int y) => St(f, x, y, new[] { "OOOOOOO", "OGGWGGO", "OGGWGGO", "OGWWWGO", "OGGWGGO", "OOOOOOO" });
		void Warning(Frame f, int x, int y) => St(f, x, y, new[] { "....O....", "...OYO...", "..OYEYO..", "..OYEYO..", ".OYYEYYO.", ".OYYYYYO.", "OYYYEYYYO", "OOOOOOOOO" });
		void Steam(Frame f, int x, int y) => St(f, x, y, new[] { ".cc.", "cckc", ".cc." });
		void Sweat(Frame f, int x, int y) => St(f, x, y, new[] { ".A.", "AAA", "AWA", ".A." });
		void Sun(Frame f, int x, int y) => St(f, x, y, new[] { "X.X.X", ".XXX.", "XXXXX", ".XXX.", "X.X.X" });
		void Snow(Frame f, int k) { foreach (var (sx, sy) in new[] { (2, -4), (9, 0), (19, -2), (27, 3), (5, 10), (29, 14), (14, -5) }) f.Dot(sx, ((sy + k * 3) + 40) % 24 - 5, Zz); }
		void Scarf(Frame f) => St(f, AX + 3, AY + 15, new[] { "RWRWRWRWRWRWRW", "....RW........", "....WR........" });
		void Confetti(Frame f, int k)
		{
			char[] cs = { Heart, Spark, Blue, Green, Yel };
			for (int i = 0; i < 18; i++) f.Dot((i * 7 + k * 3) % W, -6 + (i * 5 + k * 4) % 30, cs[(i + k) % cs.Length]);
		}
		void Cup(Frame f, int x, int y) => St(f, x, y, new[] { "OWWWO.", "OdddOO", "OWWWO.O", ".OOO.." });
		void Broom(Frame f, int x, int k) { for (int i = 0; i < 8; i++) f.Dot(x + 6 - i / 2 + k, 20 + i, BrownDark); St(f, x + k, 27, new[] { ".yyyy", "yyyyy", "yYyYy", "YyYyY" }); }
		void Hammer(Frame f, int x, int y, bool up) => St(f, x, y, up ? new[] { "lll.", "lll.", ".N..", ".N.." } : new[] { "NNNll", "...ll" });
		void Headphones(Frame f) { St(f, AX + 6, AY + 1, new[] { "..qqqqqq..", ".q......q.", "q........q" }); f.Rect(AX + 14, AY + 4, 3, 5, Note).Dot(AX + 15, AY + 5, BlueLight); }
		void PartyHat(Frame f) => St(f, AX + 8, AY - 5, new[] { "...X...", "..HXH..", "..XHX..", ".HXHXH.", ".XHXHX." });
		void Fortune(Frame f, int x, int y, bool broken) => St(f, x, y, broken ? new[] { "YY...YY", "Yy.W.yY", ".Y...Y." } : new[] { "..YYY..", ".YYyYY.", "YYyYyYY", ".YYYYY." });
		void Slip(Frame f, int x, int y) => St(f, x, y, new[] { "WWWWW", "WlllW", "WWWWW" });
		void Puddle(Frame f, int x, int w) => f.Rect(x, 31, w, 1, Water).Rect(x + 1, 30, w - 2, 1, BlueLight);
		void Glass(Frame f, int x, int y) => St(f, x, y, new[] { "O...O", "OAAAO", "OAAAO", ".OOO." });

		// ------------------------------------------------------------ frames: basics
		int fStand = p.Add(Pose());
		int fBlink = p.Add(Pose(1));
		int fHappy = p.Add(Pose(2));
		int fWide = p.Add(Pose(3, 1));
		int fUp = p.Add(Pose(4));
		int fBack = p.Add(Pose(8));
		int fWalk1 = p.Add(Pose(0, 0, 0, 1, -1));
		int fWalk2 = p.Add(Pose(0, 0, 0, 0, 0, 1));
		int fWalk3 = p.Add(Pose(0, 0, 0, 2, 1));
		int fRun1 = p.Add(Pose(3, 0, 2, 1, -2, 0, 1, false, true, -1));
		int fRun2 = p.Add(Pose(3, 0, 1, 4, -2, 0, 0, false, true, -1));
		int fWave1 = p.Add(Pose(2, 0, 3));
		int fWave2 = p.Add(Pose(2, 1, 1));
		int fFlap1 = p.Add(Pose(3, 1, 1, 3, 0, 0, 2));
		int fFlap2 = p.Add(Pose(3, 1, 2, 3, 0, 0, 1));
		int fCrouch = p.Add(Low(2));
		int fLand = p.Add(Low(3, 1, 1, 2));
		int fJump = p.Add(Pose(2, 1, 1, 3, 0, 0, 6));
		var dz = Low(4, 1, 0, 0); dz.Dot(6, 6, Spark).Dot(18, 4, Spark).Dot(12, 2, Spark); int fDizzy0 = p.Add(dz);
		int fSit = p.Add(Low(2, 0));
		Frame Sleep(int z) { var f = Low(3, 1); if (z >= 1) Zed(f, 22, 6); if (z >= 2) Zed(f, 26, 1); return f; }
		int fSleep1 = p.Add(Sleep(0)), fSleep2 = p.Add(Sleep(1)), fSleep3 = p.Add(Sleep(2));
		int fClimb1 = p.Add(Pose(4, 0, 1, 1, 0, -1)), fClimb2 = p.Add(Pose(4, 0, 2, 2, 0, -2));
		int fPeck1 = p.Add(Pose(0, 0, 0, 0, -2, 5)), fPeck2 = p.Add(Pose(1, 1, 0, 0, -3, 7));
		var pp = Pose(2, 1, 1, 0, 0, -1); int fPeep = p.Add(pp);
		var piuB = Empty(); Bubble(piuB, piu); int fPiuBubble = p.Add(piuB);
		int fTalk1 = p.Add(Pose(0, 1)), fTalk2 = p.Add(Pose(2, 0));

		// egg hiding
		int fEgg0 = p.Add(Egg(0)), fEgg1 = p.Add(Egg(1)), fEgg2 = p.Add(Egg(2)), fEggPeek = p.Add(Egg(3));
		var into = Low(3, 3, 2, 1); int fDuck = p.Add(into);

		// ------------------------------------------------------------ frames: child-window things
		// worm far away on the taskbar
		var spotW = Pose(3, 1, 0, 0, -1); Bubble(spotW, bang); int fSpot = p.Add(spotW);
		int[] worm = new int[5];
		for (int i = 0; i < 5; i++) { var f = Empty(); WormAt(f, 13, i < 2 ? i : 2, i % 2); worm[i] = p.Add(f); }
		var wormHole = Empty(); WormAt(wormHole, 13, 0, 0); int fWormHole = p.Add(wormHole);
		var eatW1 = Pose(3, 1, 0, 0, -2, 5); int fEatW1 = p.Add(eatW1);
		var eatW2 = Pose(1, 0, 0, 0, -1, 2); eatW2.Rect(AX - 1, AY + 12, 3, 1, Worm); int fEatW2 = p.Add(eatW2);
		int fYum = p.Add(Pose(2, 1, 1));
		// hawk
		var hawk1 = Empty(); St(hawk1, 1, 0, HawkUp); int fHawk1 = p.Add(hawk1);
		var hawk2 = Empty(); St(hawk2, 1, 2, HawkDown); int fHawk2 = p.Add(hawk2);
		var worried = Pose(4, 0, 0, 0, 1, 1); worried.Dot(AX + 19, AY + 2, Water); int fWorried = p.Add(worried);
		var gasp = Pose(3, 2, 1, 0, 0, 0, 1); Bubble(gasp, bang); int fGasp = p.Add(gasp);
		var phew = Pose(1, 1, 2); Sweat(phew, AX + 18, AY); int fPhew = p.Add(phew);
		// mother hen
		var hen1 = Empty(); St(hen1, 1, 12, HenArt); St(hen1, 9, 29, new[] { ".F...F", "FFF.FFF" }); int fHen1 = p.Add(hen1);
		var hen2 = Empty(); St(hen2, 1, 13, HenArt); St(hen2, 9, 30, new[] { "F...F.", "FF.FFF" }); int fHen2 = p.Add(hen2);
		var love = Pose(2); HeartAt(love, 3, 2); int fLoveMom = p.Add(love);
		// popcorn
		var k0 = Pose(4, 0, 0, 0, -1, 2); Kernel(k0, 1, 28); int fKernel = p.Add(k0);
		var k1 = Pose(3, 0, 0, 0, 0, 0); Kernel(k1, 1, 28); k1.Dot(0, 26, Spark).Dot(4, 25, Spark); Sun(k1, 0, -6); int fKernelHot = p.Add(k1);
		var kpop = Pose(3, 2, 1, 3, 0, 0, 5); St(kpop, 0, 25, new[] { "X.X.X", ".XXX.", "X.X.X" }); int fPop = p.Add(kpop);
		var pc = Empty(); Popcorn(pc, 0, 25); int fPopcorn = p.Add(pc);
		var pcr = Empty(); Popcorn(pcr, 0, 27); int fPopcornRest = p.Add(pcr);
		// butterfly
		var bb1 = Empty(); Butterfly(bb1, 2, 6, 0); int fBut1 = p.Add(bb1);
		var bb2 = Empty(); Butterfly(bb2, 2, 6, 1); int fBut2 = p.Add(bb2);
		var sad = Pose(7, 0, 0, 0, 0, 1); int fSad = p.Add(sad);
		// ladybug, snail, frog
		var lb1 = Empty(); Ladybug(lb1, 4, 0); int fBug1 = p.Add(lb1);
		var lb2 = Empty(); Ladybug(lb2, 4, 1); int fBug2 = p.Add(lb2);
		var sn1 = Empty(); Snail(sn1, 6, 0); int fSnail1 = p.Add(sn1);
		var sn2 = Empty(); Snail(sn2, 6, 1); int fSnail2 = p.Add(sn2);
		int fTap1 = p.Add(Pose(5, 0, 0, 1)), fTap2 = p.Add(Pose(5, 0, 0, 0));
		var fr1 = Empty(); Frog(fr1, 4, 27, 0); int fFrog1 = p.Add(fr1);
		var fr2 = Empty(); Frog(fr2, 4, 22, 1); int fFrog2 = p.Add(fr2);
		// dandelion
		var dd1 = Pose(0, 0, 4); Dandelion(dd1, 0, 12, true); int fDand1 = p.Add(dd1);
		var dd2 = Pose(1, 1, 4); Dandelion(dd2, 0, 12, false); int fDand2 = p.Add(dd2);
		var sd = Empty(); Seed(sd, 1, 12); int fSeed = p.Add(sd);
		// balloon
		var bl1 = Pose(2, 0, 4); Balloon(bl1, 0, -2, 0); int fBalloon = p.Add(bl1);
		var bf1 = Empty(); Balloon(bf1, 0, -2, 0); int fBalFly1 = p.Add(bf1);
		var bf2 = Empty(); Balloon(bf2, 0, -2, 1); int fBalFly2 = p.Add(bf2);
		// feather, footprints, flower, egg rolling, dust, rain, nap Zs, ant
		var sh1 = Pose(1, 0, 1); sh1.Shift(-1, 0); Sparks(sh1, 0); int fShake1 = p.Add(sh1);
		var sh2 = Pose(1, 0, 2); sh2.Shift(1, 0); Sparks(sh2, 1); int fShake2 = p.Add(sh2);
		var fe1 = Empty(); Feather(fe1, 12, 14, 0); int fFeather1 = p.Add(fe1);
		var fe2 = Empty(); Feather(fe2, 12, 14, 1); int fFeather2 = p.Add(fe2);
		int fStomp1 = p.Add(Pose(6, 0, 1, 1, 0, 0, 1)), fStomp2 = p.Add(Pose(6, 1, 0, 2, 0, 1));
		var fp = Empty(); Footprint(fp, 11); Footprint(fp, 17); int fPrints = p.Add(fp);
		var dig = Pose(0, 1, 0, 0, -2, 6); int fDig = p.Add(dig);
		var fl = new int[5]; for (int i = 0; i < 4; i++) { var f = Empty(); Flower(f, 0, i); fl[i] = p.Add(f); }
		var wilt = Empty(); Flower(wilt, 0, 2); fl[4] = p.Add(wilt);
		var eggR1 = Empty(); St(eggR1, 4, 22, new[] { "..OOO..", ".OSSSO.", "OSSsSSO", "OSSSSSO", "OSSSsSO", ".OSSSO.", "..OOO.." }); int fRoll1 = p.Add(eggR1);
		var eggR2 = Empty(); St(eggR2, 4, 22, new[] { "..OOO..", ".OSSSO.", "OSSSSSO", "OsSSSSO", "OSSSSSO", ".OSsSO.", "..OOO.." }); int fRoll2 = p.Add(eggR2);
		var bath1 = Low(4, 1, 0, 1); Dust(bath1, 0, 26); int fBath1 = p.Add(bath1);
		var bath2 = Low(4, 2, 0, 2); Dust(bath2, 22, 26); int fBath2 = p.Add(bath2);
		var du = Empty(); Dust(du, 2, 26); int fDust = p.Add(du);
		var cl1 = Empty(); CloudAt(cl1, 8, -6, 0); int fCloud1 = p.Add(cl1);
		var cl2 = Empty(); CloudAt(cl2, 8, -6, 1); int fCloud2 = p.Add(cl2);
		var wet = Pose(7, 0, 0, 0, 0, 1); Drop(wet, 10, 6); Drop(wet, 20, 8); Drop(wet, 6, 14); int fWet = p.Add(wet);
		var zc = Empty(); Zed(zc, 18, 8); int fZ = p.Add(zc);
		var ant1 = Empty(); St(ant1, 4, 26, new[] { "..WW.", ".WWWW", "EE.EE", "E.E.E" }); int fAnt1 = p.Add(ant1);
		var ant2 = Empty(); St(ant2, 4, 26, new[] { "..WW.", ".WWWW", "EE.EE", ".E.E." }); int fAnt2 = p.Add(ant2);
		// rooster crow attempt
		var crow1 = Pose(5, 0, 1, 0, 0, 0, 1); int fCrowIn = p.Add(crow1);
		var crow2 = Pose(1, 2, 3, 0, -1, -1, 2); int fCrow = p.Add(crow2);

		// ------------------------------------------------------------ frames: the program's features
		var th1 = Pose(4); for (int i = 0; i < 3; i++) th1.Dot(3 + i * 2, 6 - i * 2, Note); int fThink1 = p.Add(th1);
		var th2 = Pose(0, 0, 0, 0, 1); St(th2, 1, -5, new[] { ".kkk.", "kccck", "kccck", ".kkk." }); int fThink2 = p.Add(th2);
		var id1 = Pose(3); Bulb(id1, 2, -5, false); int fIdea1 = p.Add(id1);
		var id2 = Pose(2, 1, 1); Bulb(id2, 2, -5, true); id2.Dot(0, -3, Spark).Dot(8, -4, Spark); int fIdea2 = p.Add(id2);
		var ib = Empty(); Bulb(ib, 2, -5, true); int fBulb = p.Add(ib);
		var rd1 = Pose(0, 0, 4, 0, 0, 2); Glasses(rd1, AX + 6, AY + 8); Book(rd1, 0, 23); int fRead1 = p.Add(rd1);
		var rd2 = Pose(1, 0, 4, 0, 0, 2); Glasses(rd2, AX + 6, AY + 8); Book(rd2, 0, 23); rd2.Rect(6, 22, 2, 2, White); int fRead2 = p.Add(rd2);
		var lp1 = Pose(0, 0, 0, 0, 0, 3); Laptop(lp1, 0, 25, 0); int fLap1 = p.Add(lp1);
		var lp2 = Pose(3, 1, 0, 0, -1, 4); Laptop(lp2, 0, 25, 1); int fLap2 = p.Add(lp2);
		var code = Empty(); St(code, 0, 18, new[] { ".G...G.", "G..G..G", ".G.G.G.", "...G..." }); int fCode = p.Add(code);
		Frame Loading(int k) { var f = Pose(k % 2 == 0 ? 4 : 0); LoadingRing(f, 5, -2, k); return f; }
		int fLoad1 = p.Add(Loading(0)), fLoad2 = p.Add(Loading(1)), fLoad3 = p.Add(Loading(2)), fLoad4 = p.Add(Loading(3));
		int fDlLook = p.Add(Pose(4, 1, 1));
		var dlc = Pose(2, 0, 4, 0, 0, 1); DownloadBox(dlc, 2, 17); int fDlCatch = p.Add(dlc);
		var dlb = Empty(); DownloadBox(dlb, 2, 0); int fDlBox = p.Add(dlb);
		var tu = Pose(2, 0, 3); Bubble(tu, check); int fThumbs = p.Add(tu);
		var okf = Pose(2, 0, 3); Bubble(okf, ok); int fOk = p.Add(okf);
		var cp = Pose(2, 1, 1, 3, 0, 0, 3); Confetti(cp, 0); int fPopped = p.Add(cp);
		var cfa = Empty(); Confetti(cfa, 0); int fConf1 = p.Add(cfa);
		var cfb = Empty(); Confetti(cfb, 1); int fConf2 = p.Add(cfb);
		var er = Pose(3, 2, 1, 0, 0, 0, 1); Warning(er, 1, -6); int fError = p.Add(er);
		var wn = Empty(); Warning(wn, 1, -6); int fWarn = p.Add(wn);
		int fCry1 = p.Add(Pose(7, 1, 0, 0, 0, 1)), fCry2 = p.Add(Pose(1, 2, 0, 0, 0, 2));
		var an1 = Pose(6, 2, 2); an1.Shift(-1, 0); int fAngry1 = p.Add(an1);
		var an2 = Pose(6, 1, 0); an2.Shift(1, 0); int fAngry2 = p.Add(an2);
		var stc = Empty(); Steam(stc, 15, 6); int fSteam = p.Add(stc);
		var sk = Pose(3, 2, 1, 3, 0, 0, 3, false, true, 2); sk.Dot(AX + 10, AY - 4, Yel).Dot(AX + 12, AY - 5, Yel); int fShocked = p.Add(sk);
		var sc1 = Pose(3, 1, 1); sc1.Shift(-1, 0); Sweat(sc1, AX + 18, AY); int fScared1 = p.Add(sc1);
		var sc2 = Pose(3, 0, 2); sc2.Shift(1, 0); Sweat(sc2, AX + 19, AY + 2); int fScared2 = p.Add(sc2);
		var um1 = Pose(4, 0, 4); LeafUmbrella(um1, 1, -4); Drop(um1, 0, 10); Drop(um1, 27, 6); int fUmb1 = p.Add(um1);
		var um2 = Pose(0, 0, 4); LeafUmbrella(um2, 1, -4); Drop(um2, 1, 14); Drop(um2, 28, 10); int fUmb2 = p.Add(um2);
		var pd0 = Pose(2); Puddle(pd0, 0, 11); int fPud0 = p.Add(pd0);
		var pdj = Pose(2, 1, 1, 3, 0, 0, 5); Puddle(pdj, 0, 11); int fPudJump = p.Add(pdj);
		var pds = Low(2, 2, 1, 2); Puddle(pds, 0, 13); foreach (var (sx, sy) in new[] { (0, 25), (3, 23), (9, 24), (12, 27) }) Drop(pds, sx, sy); int fPudSplash = p.Add(pds);
		var spl = Empty(); foreach (var (sx, sy) in new[] { (2, 23), (4, 26), (0, 28) }) Drop(spl, sx, sy); int fSplash = p.Add(spl);
		var ht1 = Pose(5, 1); Sweat(ht1, AX + 18, AY); Sun(ht1, 0, -6); int fHot1 = p.Add(ht1);
		var ht2 = Pose(5, 2); Sweat(ht2, AX + 19, AY + 3); Sun(ht2, 0, -6); int fHot2 = p.Add(ht2);
		var cd1 = Pose(1, 0, 0, 0, 0, 1); cd1.Shift(-1, 0); Snow(cd1, 0); int fCold1 = p.Add(cd1);
		var cd2 = Pose(1, 0, 0, 0, 0, 1); cd2.Shift(1, 0); Snow(cd2, 1); int fCold2 = p.Add(cd2);
		var sf1 = Pose(2); Scarf(sf1); Snow(sf1, 0); int fScarf1 = p.Add(sf1);
		var sf2 = Pose(1, 0, 0, 0, 0, 1); Scarf(sf2); Snow(sf2, 1); int fScarf2 = p.Add(sf2);
		var su = Pose(3, 1, 1, 0, 0, 0, 2); Bubble(su, bang); int fSurprised = p.Add(su);
		int fTired1 = p.Add(Pose(5, 0, 0, 0, 1, 2)), fTired2 = p.Add(Pose(1, 1, 0, 0, 1, 3));
		var sl2 = Pose(1, 0, 0, 0, 2, 4); Zed(sl2, 22, 6); int fSleepy2 = p.Add(sl2);
		var dk1 = Pose(0, 0, 4); Glass(dk1, 1, 20); int fDrink1 = p.Add(dk1);
		var dk2 = Pose(1, 1, 4, 0, -1, 2); Glass(dk2, 0, 21); int fDrink2 = p.Add(dk2);
		var ck1 = Pose(0, 0, 4); Fortune(ck1, 0, 21, false); int fCookie1 = p.Add(ck1);
		var ck2 = Pose(3, 1, 4); Fortune(ck2, 0, 21, true); int fCookie2 = p.Add(ck2);
		var slp = Empty(); Slip(slp, 2, 18); int fSlip = p.Add(slp);
		var sw1 = Pose(5, 1, 1); Sweat(sw1, AX + 18, AY); Sweat(sw1, 2, 4); int fSweat1 = p.Add(sw1);
		var sw2 = Pose(5, 0, 2); Sweat(sw2, AX + 19, AY + 2); int fSweat2 = p.Add(sw2);
		Frame Dizzy(int k)
		{
			var f = Pose(1, 1, 0, 0, k == 0 ? -1 : 1);
			var stars = new[] { (6, 5), (12, 3), (18, 5), (12, 7) };
			for (int i = 0; i < 4; i++) { var (sx, sy) = stars[(i + k) % 4]; f.Dot(sx, sy, Spark).Dot(sx + 1, sy, Spark).Dot(sx, sy + 1, Spark); }
			return f;
		}
		int fDizzyA = p.Add(Dizzy(0)), fDizzyB = p.Add(Dizzy(1)), fDizzyC = p.Add(Dizzy(2));
		var mir = Pose(2); mir.MirrorX(); int fMirror = p.Add(mir);
		int fNod = p.Add(Pose(2, 0, 0, 0, 0, 2));
		int fCheer1 = p.Add(Pose(2, 1, 3, 3, 0, 0, 5)), fCheer2 = p.Add(Pose(2, 2, 1));
		var pt1 = Pose(2, 0, 0, 0, 0, 0, 0, false); PartyHat(pt1); St(pt1, 0, 18, new[] { ".HH", "H.H", ".H." }); int fParty1 = p.Add(pt1);
		var pt2 = Pose(3, 1, 1, 0, -1, 0, 0, false); PartyHat(pt2); pt2.Rect(0, 19, 5, 1, Heart).Rect(0, 20, 5, 1, Yel); int fParty2 = p.Add(pt2);
		var hp = Pose(2, 0, 0, 0, 0, 0, 2); hp.Dot(2, 6, Spark).Dot(27, 8, Spark); int fHappy2 = p.Add(hp);
		var hg1 = Pose(1, 0, 4, 0, 0, 1); HeartAt(hg1, 2, 18); int fHug1 = p.Add(hg1);
		var hg2 = Pose(2, 0, 4, 0, 0, 1); HeartAt(hg2, 2, 18); HeartAt(hg2, 0, 2); int fHug2 = p.Add(hg2);
		var hc = Empty(); HeartAt(hc, 4, 6); int fHeartChild = p.Add(hc);
		var lv1 = Pose(2); HeartAt(lv1, 3, 2); int fLove1 = p.Add(lv1);
		var lv2 = Pose(2, 0, 0, 0, 0, 1); HeartAt(lv2, 4, -1); HeartAt(lv2, 24, 4); int fLove2 = p.Add(lv2);
		var swp1 = Pose(0, 0, 4, 0, 0, 2); Broom(swp1, 0, 0); int fSweep1 = p.Add(swp1);
		var swp2 = Pose(0, 0, 4, 0, -1, 2); Broom(swp2, 0, -1); Dust(swp2, 0, 26); int fSweep2 = p.Add(swp2);
		// work: hammering a board
		var wk1 = Pose(0, 0, 4, 0, 0, 3); Hammer(wk1, 1, 20, true); wk1.Rect(0, 30, 8, 2, Brown); int fWork1 = p.Add(wk1);
		var wk2 = Pose(3, 1, 4, 0, -1, 4); Hammer(wk2, 0, 27, false); wk2.Rect(0, 30, 8, 2, Brown); wk2.Dot(1, 26, Spark).Dot(6, 25, Spark); int fWork2 = p.Add(wk2);
		var ms1 = Pose(1, 0, 0, 1, -1); Headphones(ms1); int fMusic1 = p.Add(ms1);
		var ms2 = Pose(2, 1, 0, 2, 1, 1); Headphones(ms2); int fMusic2 = p.Add(ms2);
		var nc = Empty(); NoteAt(nc, 3, 4); int fNote = p.Add(nc);
		var cf1 = Pose(0, 0, 4); Cup(cf1, 0, 21); int fCoffee1 = p.Add(cf1);
		var cf2 = Pose(1, 1, 4, 0, -1, 1); Cup(cf2, 0, 20); int fCoffee2 = p.Add(cf2);
		var stm = Empty(); St(stm, 1, 15, new[] { ".k.k", "k.k.", ".k.k" }); int fCoffeeSteam = p.Add(stm);
		var lg1 = Pose(2, 2); Bubble(lg1, kkk); int fLaugh1 = p.Add(lg1);
		var lg2 = Pose(2, 1, 0, 0, 0, 1); Bubble(lg2, kkk); int fLaugh2 = p.Add(lg2);
		int fDance1 = p.Add(Pose(2, 1, 3, 1, -2)), fDance2 = p.Add(Pose(2, 0, 2, 2, 2, 1));
		int fYawn = p.Add(Pose(1, 2, 0, 0, 0, -1)), fStretch = p.Add(Pose(1, 0, 3, 2, 0, -1));
		var cu = Pose(4, 0, 0, 0, 1); Bubble(cu, question); int fCurious = p.Add(cu);
		var eat1 = Pose(0, 0, 0, 0, -1, 2); Kernel(eat1, 1, 28); Kernel(eat1, 6, 29); int fEat1 = p.Add(eat1);
		var eat2 = Pose(1, 1, 0, 0, -3, 7); Kernel(eat2, 6, 29); int fEat2 = p.Add(eat2);
		var sneezeA = Pose(4, 1, 0, 0, 1, -1); int fAh = p.Add(sneezeA);
		var sneezeB = Pose(1, 2, 2, 0, -2, 1, 0, false, true, -3); sneezeB.Dot(0, 18, Water).Dot(1, 21, Water).Dot(2, 16, Water); int fChoo = p.Add(sneezeB);

		p.IconFrame = fHappy;

		// ------------------------------------------------------------ animations
		Anim Show(int id, string name, int ms, params (int f, int n)[] steps)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString());
			foreach (var (f, n) in steps) a.Seq(Enumerable.Repeat(f, n).ToArray());
			a.Then(100, 1);
			return a;
		}
		Anim Loop2(int id, string name, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString()).Loop(loops, 0);
			a.Seq(frames); a.Then(100, 1);
			return a;
		}
		Anim Child(int id, string name, string mx, string my, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move(mx, my).Speed(ms.ToString()).Loop(loops, 0); a.Seq(frames); return a;
		}
		Anim Then(Anim a, int next) { a.Next.Clear(); a.Then(100, next); return a; }

		p.Spawns.Add((1, 50, "screenW+10", "areaH-imageH", 1));
		p.Spawns.Add((2, 30, "random*(screenW-imageW-50)/100+25", "-imageH-20", 5));
		p.Spawns.Add((3, 20, "0", "areaH-imageH", 1));

		var walk = p.A(1, "walk").Move("-2", "0").Speed("150").Loop("random/4+8").Seq(fWalk1, fWalk2, fWalk3, fWalk2);
		walk.Then(45, 1).Then(10, 2).Then(15, 11).Then(5, 18).Then(5, 20).Then(4, 21).Then(3, 22).Then(2, 25).Then(3, 29).Then(3, 30).Then(3, 33)
			.Then(6, 16, "taskbar").Then(4, 13, "taskbar")
			.OnBorder(100, 2).OnBorder(20, 12, "vertical").OnBorder(30, 9, "window").OnGravity(100, 5);
		var turn = p.A(2, "turn").Move("0", "0").Speed("130").Seq(fBack, fStand); turn.Action = "flip"; turn.Then(100, 1);
		p.A(3, "drag").Move("0", "0").Speed("70").Loop("40").Seq(fFlap1, fFlap1, fFlap2, fFlap2).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("45").Loop("30").Seq(fFlap1, fWide, fFlap2, fWide).Then(100, 4);
		p.A(5, "fall").Move("0", "2", "0", "8").Speed("80", "50").Loop("20").Seq(fFlap1, fFlap2).Then(100, 6).OnBorder(100, 7);
		p.A(6, "fall fast").Move("0", "9").Speed("45").Loop("10").Seq(fFlap1, fWide).Then(100, 6).OnBorder(100, 8);
		p.A(7, "land").Move("0", "0").Speed("120", "200").Seq(fLand, fCrouch, fStand, fHappy).Then(100, 1);
		p.A(8, "fall hard").Move("0", "0").Speed("150", "260").Seq(fLand, fDizzy0, fDizzy0, fDizzy0, fDizzy0, fBlink, fStand).Then(100, 1);
		p.A(9, "hop up").Move("-3", "-8", "-3", "8").Speed("70").Seq(fCrouch, fJump, fFlap1, fFlap2, fFlap1, fFlap2, fLand, fStand)
			.Then(100, 1).OnBorder(80, 7, "taskbar").OnBorder(40, 7, "window").OnBorder(20, 5, "horizontal+");
		p.A(10, "sync").Move("0", "0").Speed("120").Loop("6").Seq(fDance1, fDance2).Then(100, 1);
		var hub = p.A(11, "blink").Move("0", "0").Speed("450", "120").Seq(fStand, fBlink, fStand, fBlink, fStand);
		hub.Then(30, 1).Then(8, 15).Then(6, 17).Then(8, 20).Then(6, 21).Then(5, 22).Then(4, 25).Then(4, 29).Then(4, 30).Then(4, 33)
			.Then(3, 36).Then(3, 37).Then(3, 38).Then(3, 39).Then(3, 40).Then(3, 41).Then(3, 42).Then(3, 43).Then(3, 44).Then(3, 45)
			.Then(3, 46).Then(3, 47).Then(3, 48).Then(3, 49).Then(3, 50).Then(2, 51).Then(2, 52);
		p.A(12, "climb").Move("0", "-3").Speed("140").Loop("5000", 2).Seq(fClimb1, fClimb2, fClimb1, fClimb2).Then(100, 12).OnBorder(100, 2);
		p.A(13, "sleep").Move("0", "0").Speed("800", "450").Loop("random/4+5", 2).Seq(fSit, fSleep1, fSleep1, fSleep2, fSleep3, fSleep2).Then(100, 14);
		p.A(14, "wake up").Move("0", "0").Speed("220", "160").Seq(fSleep1, fSit, fWide, fStand, fHappy, fStand).Then(100, 1);
		p.A(15, "look around").Move("0", "0").Speed("280").Seq(fStand, fUp, fUp, fStand, fBack, fBack, fStand).Then(100, 1);
		p.A(16, "sit").Move("0", "0").Speed("600", "400").Loop("random/3+4", 1).Seq(fSit, fSit, fSit, fSit).Then(60, 1).Then(40, 13);
		p.A(17, "wave").Move("0", "0").Speed("170").Loop("4", 1).Seq(fStand, fWave1, fWave2, fWave1, fWave2).Then(100, 1);
		var run = p.A(18, "run").Move("-6", "0").Speed("70").Loop("random/4+10").Seq(fRun1, fRun2);
		run.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);

		// ================================================================ chick scenes (most with a child window)
		Loop2(20, "peck", 150, "random/3+4", fStand, fPeck1, fPeck2, fPeck1);
		// 21 peep: "piu!" floats up
		Show(21, "peep", 200, (fStand, 1), (fPeep, 4), (fHappy, 3));
		Child(200, "piu bubble", "-1", "-2", 100, "14", fPiuBubble).Fade(1, 0);
		p.Childs.Add((21, "imageX", "imageY", 200));

		// 22 worm: it pops out far away on the taskbar; he spots it, runs there and eats it.
		// The worm is 4 sprites in front of him; he runs 4 sprites (64 px each) in 0.07 s steps of 6 px: about 43 steps.
		Show(22, "spot worm", 120, (fStand, 3), (fUp, 2), (fSpot, 6));
		Then(p.Anims.Last(), 23);
		var toWorm = p.A(23, "run to worm").Move("-6", "0").Speed("70").Loop("20", 0).Seq(fRun1, fRun2);
		toWorm.Then(100, 24).OnBorder(100, 24).OnGravity(100, 5);
		Show(24, "eat worm", 160, (fEatW1, 2), (fEatW2, 3), (fEatW1, 2), (fEatW2, 3), (fYum, 4), (fHappy, 3));
		Child(201, "worm pops out", "0", "0", 140, "0", fWormHole, worm[0], worm[1], worm[1]);
		Then(p.Anims.Last(), 202);
		Child(202, "worm wiggles", "0", "0", 140, "13", worm[2], worm[3]);
		Then(p.Anims.Last(), 203);
		Child(203, "worm eaten", "0", "0", 140, "0", worm[1], worm[0], fWormHole).Fade(1, 0);
		p.Childs.Add((22, "imageX-imageW*4", "imageY", 201));

		// 25 hawk: it flies in from behind, high up, circles over him while he hides in his egg, then gives up and leaves.
		Show(25, "hawk", 150, (fStand, 2), (fBack, 6), (fWorried, 6), (fGasp, 4));
		Then(p.Anims.Last(), 26);
		p.A(26, "hide in shell").Move("0", "0").Speed("160").Seq(fDuck, fDuck, fEgg0, fEgg1, fEgg0, fEgg2, fEgg0, fEgg0, fEgg1, fEgg0, fEgg2, fEgg0,
			fEgg0, fEgg0, fEgg1, fEgg0, fEgg2, fEgg0, fEgg0, fEgg0, fEgg0, fEgg1, fEgg0, fEgg2, fEgg0, fEgg0, fEgg0, fEgg0, fEgg0, fEgg0, fEgg0, fEgg0).Then(100, 27);
		Show(27, "peek out", 220, (fEggPeek, 6), (fEgg0, 2), (fEggPeek, 6));
		Then(p.Anims.Last(), 28);
		Show(28, "phew", 180, (fDuck, 2), (fPhew, 6), (fHappy, 3));
		// the hawk: it starts 6.5 sprites behind him and 5 sprites high, glides down to 2 sprites above him (~46 steps)
		Child(204, "hawk arrives", "-9", "5", 70, "22", fHawk1, fHawk2);
		Then(p.Anims.Last(), 205);
		Child(205, "hawk circles", "-7", "0", 70, "8", fHawk1, fHawk2);
		Then(p.Anims.Last(), 206);
		var hawkTurn = Child(206, "hawk turns", "0", "0", 90, "0", fHawk2, fHawk1); hawkTurn.Action = "flip"; Then(hawkTurn, 207);
		Child(207, "hawk circles back", "-7", "0", 70, "16", fHawk1, fHawk2);
		Then(p.Anims.Last(), 208);
		var hawkTurn2 = Child(208, "hawk turns again", "0", "0", 90, "0", fHawk2, fHawk1); hawkTurn2.Action = "flip"; Then(hawkTurn2, 209);
		Child(209, "hawk gives up", "-7", "-1", 70, "6", fHawk1, fHawk2);
		Then(p.Anims.Last(), 210);
		Child(210, "hawk leaves", "-9", "-5", 70, "200", fHawk1, fHawk2);
		p.Childs.Add((25, "imageX+imageW*6.5", "imageY-imageH*5", 204));

		// 29 corn kernel: the sun heats it and it pops into popcorn
		Show(29, "corn kernel", 220, (fKernel, 4), (fCurious, 4), (fKernelHot, 6));
		Then(p.Anims.Last(), 31);
		Show(31, "popcorn", 120, (fPop, 4), (fWide, 3), (fUp, 6), (fLaugh1, 2), (fLaugh2, 2), (fLaugh1, 2), (fHappy, 2));
		Child(211, "popcorn up", "-3", "-7", 60, "0", Enumerable.Repeat(fPopcorn, 10).ToArray());
		Then(p.Anims.Last(), 212);
		Child(212, "popcorn falls", "-2", "7", 60, "200", fPopcorn).OnBorder(100, 213, "taskbar").OnBorder(100, 213, "window");
		Child(213, "popcorn rests", "0", "0", 300, "6", fPopcornRest).Fade(1, 0);
		p.Childs.Add((31, "imageX", "imageY", 211));

		// 30 the mother hen walks by; he follows her for a while, then waves goodbye
		Show(30, "mother hen", 200, (fBack, 6), (fWide, 2), (fLoveMom, 6));
		Then(p.Anims.Last(), 32);
		var follow = p.A(32, "follow mom").Move("-3", "0").Speed("120").Loop("12", 0).Seq(fWalk1, fWalk2, fWalk3, fWalk2);
		follow.Then(100, 34).OnBorder(100, 34).OnGravity(100, 5);
		Show(34, "bye mom", 200, (fStand, 2), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fLoveMom, 4), (fStand, 1));
		Child(214, "hen walks", "-5", "0", 120, "300", fHen1, fHen2);
		p.Childs.Add((30, "imageX+imageW*1.5", "imageY", 214));

		// 33 butterfly: he watches it, chases it, loses it
		Show(33, "butterfly", 220, (fUp, 4), (fHappy, 3));
		Then(p.Anims.Last(), 35);
		var chase = p.A(35, "chase butterfly").Move("-5", "0").Speed("80").Loop("6", 0).Seq(fRun1, fJump, fRun2, fJump);
		chase.Then(100, 53).OnBorder(100, 53).OnGravity(100, 5);
		Show(53, "butterfly got away", 220, (fUp, 4), (fSad, 6), (fBlink, 2), (fStand, 1));
		Child(215, "butterfly flutters", "-4", "-1", 110, "16", fBut1, fBut2);
		Then(p.Anims.Last(), 216);
		Child(216, "butterfly away", "-3", "-4", 110, "60", fBut1, fBut2);
		p.Childs.Add((33, "imageX-imageW*0.5", "imageY", 215));

		// 36 ladybug crawls past him
		Show(36, "ladybug", 260, (fBack, 4), (fStand, 2), (fUp, 1), (fStand, 6), (fWave1, 2), (fWave2, 2), (fHappy, 2));
		Child(217, "ladybug walk", "-2", "0", 120, "120", fBug1, fBug2);
		p.Childs.Add((36, "imageX+imageW*1", "imageY", 217));
		// 37 snail: so slow... he taps his foot and sits
		Show(37, "snail", 300, (fStand, 2), (fCurious, 4), (fTap1, 1), (fTap2, 1), (fTap1, 1), (fTap2, 1), (fTap1, 1), (fTap2, 1), (fTired1, 4), (fSit, 6), (fStand, 1));
		Child(218, "snail crawls", "-1", "0", 200, "160", fSnail1, fSnail2);
		p.Childs.Add((37, "imageX-imageW*0.8", "imageY", 218));
		// 38 a frog hops past and scares him
		Show(38, "frog", 120, (fStand, 14), (fWide, 2), (fJump, 3), (fLand, 2), (fScared1, 2), (fScared2, 2), (fScared1, 2), (fStand, 2));
		Child(219, "frog hops", "-5", "0", 110, "40", fFrog1, fFrog2, fFrog2, fFrog1);
		p.Childs.Add((38, "imageX+imageW*1.5", "imageY", 219));
		// 39 dandelion: he blows, two seeds float away
		Show(39, "dandelion", 220, (fDand1, 6), (fDand2, 6), (fUp, 4), (fHappy, 2));
		Child(220, "seed floats", "-2", "-1", 140, "30", fSeed).Fade(1, 0);
		Child(221, "seed floats 2", "-1", "-2", 160, "26", fSeed).Fade(1, 0);
		p.Childs.Add((39, "imageX-imageW*0.2", "imageY", 220));
		p.Childs.Add((39, "imageX", "imageY-10", 221));
		// 40 balloon: he holds it, lets go, it floats away
		Show(40, "balloon", 240, (fBalloon, 8));
		Then(p.Anims.Last(), 54);
		Show(54, "let the balloon go", 200, (fWide, 3), (fUp, 6), (fWave1, 2), (fWave2, 2), (fSad, 3), (fStand, 1));
		Child(222, "balloon away", "-1", "-3", 90, "120", fBalFly1, fBalFly1, fBalFly2, fBalFly2);
		p.Childs.Add((54, "imageX", "imageY", 222));
		// 41 he shakes and loses a feather
		Show(41, "shake", 70, (fStand, 2), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fWide, 4), (fUp, 6), (fHappy, 2));
		Child(223, "feather", "-1", "2", 110, "40", fFeather1, fFeather1, fFeather2, fFeather2).Fade(1, 0.3)
			.OnBorder(100, 224, "taskbar").OnBorder(100, 224, "window");
		Child(224, "feather rests", "0", "0", 300, "4", fFeather1).Fade(1, 0);
		p.Childs.Add((41, "imageX", "imageY", 223));
		// 42 stomp: angry little footprints that stay on the floor and fade
		Loop2(42, "stomp", 140, "random/3+4", fStomp1, fStomp2);
		Child(225, "footprints", "0", "0", 400, "8", fPrints).Fade(1, 0);
		p.Childs.Add((42, "imageX", "imageY", 225));
		// 43 plants a seed: a flower grows beside him
		Show(43, "plant a flower", 220, (fDig, 3), (fStand, 1), (fDig, 3), (fStand, 2), (fWide, 2), (fLove1, 4), (fHappy, 2));
		Child(226, "flower grows", "0", "0", 500, "0", fl[0], fl[0], fl[1], fl[1], fl[2], fl[2], fl[3]);
		Then(p.Anims.Last(), 227);
		Child(227, "flower", "0", "0", 500, "30", fl[3]);
		Then(p.Anims.Last(), 228);
		Child(228, "flower wilts", "0", "0", 400, "3", fl[4]).Fade(1, 0);
		p.Childs.Add((43, "imageX-imageW*0.6", "imageY", 226));
		// 44 an egg rolls past
		Show(44, "rolling egg", 160, (fBack, 4), (fWide, 3), (fCurious, 6), (fStand, 1));
		Child(229, "egg rolls", "-4", "0", 90, "60", fRoll1, fRoll2);
		p.Childs.Add((44, "imageX+imageW*1.5", "imageY", 229));
		// 45 dust bath: dust clouds float off
		Loop2(45, "dust bath", 160, "random/3+5", fBath1, fBath2);
		Child(230, "dust", "-2", "-1", 120, "12", fDust).Fade(1, 0);
		Child(231, "dust 2", "2", "-1", 140, "10", fDust).Fade(1, 0);
		p.Childs.Add((45, "imageX", "imageY", 230));
		p.Childs.Add((45, "imageX+imageW*0.5", "imageY", 231));
		// 46 tries to crow like a rooster: "piu!"
		Show(46, "crow", 200, (fStand, 1), (fCrowIn, 4), (fCrow, 6), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		p.Childs.Add((46, "imageX", "imageY", 200));
		// 47 a little rain cloud just for him
		Show(47, "rain cloud", 160, (fUp, 6), (fWide, 2), (fWet, 10), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fBlink, 2), (fHappy, 2));
		Child(232, "cloud rains", "0", "0", 150, "12", fCloud1, fCloud2);
		Then(p.Anims.Last(), 233);
		Child(233, "cloud leaves", "-3", "-1", 100, "12", fCloud1, fCloud2).Fade(1, 0);
		p.Childs.Add((47, "imageX", "imageY", 232));
		// 48 nap in the egg: Zs float up
		Show(48, "nap in shell", 400, (fDuck, 2), (fEgg0, 20), (fEggPeek, 4), (fStand, 1));
		Child(234, "z floats", "1", "-2", 160, "16", fZ).Fade(1, 0);
		p.Childs.Add((48, "imageX", "imageY", 234));
		// 49 an ant carries a crumb past
		Show(49, "ant", 240, (fCurious, 4), (fBack, 3), (fStand, 6), (fHappy, 2));
		Child(235, "ant walks", "-2", "0", 110, "120", fAnt1, fAnt2);
		p.Childs.Add((49, "imageX+imageW*0.8", "imageY", 235));
		// 50-52 more chick moments
		Loop2(50, "dance", 130, "random/3+5", fDance1, fDance2);
		Show(51, "sneeze", 140, (fStand, 1), (fAh, 3), (fStand, 1), (fAh, 4), (fChoo, 3), (fBlink, 2), (fStand, 1));
		Loop2(52, "laugh", 130, "random/3+4", fLaugh1, fLaugh2);

		// ================================================================ the program's features (60-104)
		Loop2(60, "think", 320, "random/3+3", fThink1, fThink1, fThink2, fThink2);
		Show(61, "idea", 160, (fThink1, 4), (fIdea1, 3), (fIdea2, 1), (fIdea1, 1), (fIdea2, 6), (fHappy, 3));
		Child(236, "idea bulb", "0", "-2", 90, "14", fBulb).Fade(1, 0);
		p.Childs.Add((61, "imageX", "imageY", 236));
		Loop2(62, "read", 420, "random/3+3", fRead1, fRead1, fRead2, fRead1);
		Loop2(63, "review", 320, "random/3+3", fRead1, fRead2);
		Loop2(64, "laptop", 140, "random/3+6", fLap1, fLap2);
		Child(237, "code floats", "-1", "-2", 110, "16", fCode).Fade(1, 0);
		p.Childs.Add((64, "imageX", "imageY", 237));
		Loop2(65, "loading", 140, "random/3+5", fLoad1, fLoad2, fLoad3, fLoad4);
		Show(66, "download", 150, (fDlLook, 7), (fDlCatch, 6), (fHappy, 3));
		Child(238, "download box", "0", "8", 60, "17", fDlBox);
		p.Childs.Add((66, "imageX", "imageY-imageH*2", 238));
		Show(67, "thumbs up", 200, (fStand, 1), (fThumbs, 8), (fHappy, 2));
		Show(68, "confetti", 140, (fCrouch, 2), (fPopped, 6), (fCheer2, 2), (fHappy, 3));
		Child(239, "confetti falls", "0", "2", 120, "20", fConf1, fConf2).Fade(1, 0);
		Child(240, "confetti falls 2", "1", "2", 140, "18", fConf2, fConf1).Fade(1, 0);
		p.Childs.Add((68, "imageX-imageW*0.4", "imageY-imageH*0.6", 239));
		p.Childs.Add((68, "imageX+imageW*0.4", "imageY-imageH*0.7", 240));
		Show(69, "error", 130, (fWide, 2), (fError, 8), (fScared1, 2), (fScared2, 2), (fBlink, 2), (fStand, 1));
		Child(241, "warning up", "0", "-2", 100, "12", fWarn).Fade(1, 0);
		p.Childs.Add((69, "imageX", "imageY", 241));
		Loop2(70, "cry", 220, "random/3+4", fCry1, fCry2);
		Loop2(71, "angry", 110, "random/3+6", fAngry1, fAngry2);
		Child(242, "angry steam", "1", "-2", 100, "12", fSteam).Fade(1, 0);
		p.Childs.Add((71, "imageX", "imageY", 242));
		Show(72, "shocked", 110, (fStand, 2), (fShocked, 8), (fBlink, 2), (fStand, 1));
		// scared: he hides in his egg (the same as with the hawk)
		Show(73, "scared", 80, (fScared1, 2), (fScared2, 2), (fScared1, 2), (fScared2, 2));
		Then(p.Anims.Last(), 26);
		var talk = p.A(74, "talk").Move("0", "0").Speed("160").Loop("random/3+8", 0);
		talk.Seq(fTalk1, fStand, fTalk1, fTalk2, fStand, fStand); talk.Then(100, 1);
		Loop2(75, "umbrella", 220, "random/3+5", fUmb1, fUmb2);
		Show(76, "puddle splash", 140, (fPud0, 4), (fCrouch, 2), (fPudJump, 4));
		Then(p.Anims.Last(), 77);
		Show(77, "splash", 140, (fPudSplash, 6), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		Child(243, "splash left", "-3", "-1", 80, "8", fSplash).Fade(1, 0);
		Child(244, "splash right", "3", "-1", 80, "8", fSplash).Fade(1, 0);
		p.Childs.Add((77, "imageX", "imageY", 243));
		p.Childs.Add((77, "imageX+imageW*0.3", "imageY", 244));
		Loop2(78, "hot", 240, "random/3+4", fHot1, fHot2);
		Loop2(79, "cold", 70, "random/3+10", fCold1, fCold2);
		Loop2(80, "scarf", 360, "random/3+3", fScarf1, fScarf2);
		Show(81, "look up", 280, (fStand, 1), (fUp, 6), (fBlink, 1), (fUp, 4), (fStand, 1));
		Show(82, "surprised", 120, (fStand, 2), (fSurprised, 6), (fWide, 3), (fStand, 1));
		Loop2(83, "tired", 400, "random/3+3", fTired1, fTired2);
		Show(84, "sleepy", 300, (fTired1, 3), (fTired2, 3), (fSleepy2, 4), (fWide, 1), (fTired1, 3), (fSleepy2, 5), (fBlink, 2), (fStand, 1));
		Show(85, "drink", 220, (fDrink1, 3), (fDrink2, 3), (fDrink1, 1), (fDrink2, 3), (fHappy, 3));
		Loop2(86, "eat", 180, "random/3+5", fEat1, fEat2);
		Show(87, "cookie", 200, (fCookie1, 4), (fCookie2, 3));
		Then(p.Anims.Last(), 88);
		Show(88, "fortune", 200, (fWide, 3), (fUp, 5), (fThink1, 3), (fHappy, 3));
		Child(245, "fortune slip", "-1", "-2", 110, "14", fSlip).Fade(1, 0);
		p.Childs.Add((88, "imageX", "imageY", 245));
		Loop2(89, "sweat", 160, "random/3+5", fSweat1, fSweat2);
		Loop2(90, "dizzy", 130, "random/3+5", fDizzyA, fDizzyB, fDizzyC);
		Loop2(91, "spin", 90, "random/3+3", fStand, fBack, fMirror, fBack);
		Show(92, "nod", 180, (fStand, 1), (fNod, 2), (fStand, 2), (fNod, 2), (fHappy, 2));
		Show(93, "ok", 200, (fStand, 1), (fOk, 8), (fHappy, 2));
		Loop2(94, "cheer", 120, "random/3+4", fCrouch, fCheer1, fCheer1, fCheer2);
		Loop2(95, "party", 160, "random/3+5", fParty1, fParty2);
		Show(96, "jump", 90, (fCrouch, 2), (fJump, 4), (fFlap2, 1), (fLand, 2), (fHappy, 3));
		Loop2(97, "happy", 140, "random/3+4", fHappy, fHappy2);
		Loop2(98, "hug", 320, "random/3+3", fHug1, fHug2);
		Loop2(100, "hearts", 260, "random/3+3", fLove1, fLove2);
		Child(246, "heart up", "-1", "-2", 100, "14", fHeartChild).Fade(1, 0);
		Child(247, "heart up 2", "1", "-2", 120, "12", fHeartChild).Fade(1, 0);
		p.Childs.Add((100, "imageX", "imageY", 246));
		p.Childs.Add((100, "imageX+imageW*0.4", "imageY", 247));
		Loop2(101, "love", 260, "random/3+3", fLove1, fLove2);
		Loop2(102, "sweep", 160, "random/3+6", fSweep1, fSweep2);
		Child(248, "sweep dust", "-2", "-1", 120, "12", fDust).Fade(1, 0);
		p.Childs.Add((102, "imageX", "imageY", 248));
		Loop2(103, "work", 150, "random/3+6", fWork1, fWork2);
		Loop2(104, "music", 200, "random/3+6", fMusic1, fMusic2);
		Child(249, "note up", "-1", "-2", 110, "20", fNote, fNote).Fade(1, 0);
		Child(250, "note up 2", "1", "-2", 130, "18", fNote, fNote).Fade(1, 0);
		p.Childs.Add((104, "imageX", "imageY", 249));
		p.Childs.Add((104, "imageX+imageW*0.3", "imageY-10", 250));
		Show(105, "coffee", 240, (fCoffee1, 3), (fCoffee2, 3), (fCoffee1, 2), (fCoffee2, 3), (fHappy, 4));
		Child(251, "coffee steam", "0", "-1", 140, "12", fCoffeeSteam).Fade(1, 0);
		p.Childs.Add((105, "imageX", "imageY", 251));
		Show(106, "yawn", 220, (fStand, 1), (fYawn, 5), (fBlink, 2), (fStand, 1));
		Show(107, "stretch", 220, (fStand, 1), (fStretch, 5), (fWave1, 2), (fStand, 1));
		Loop2(108, "curious", 320, "random/3+3", fCurious, fUp);
		foreach (int id in new[] { 60, 62, 64, 66, 68, 75, 85, 86, 87, 95, 98, 100, 102, 104, 105, 106, 107, 108 }) hub.Then(2, id);

		// 99 kill: hides in his egg and fades away
		p.A(99, "kill").Move("0", "0").Speed("160").Fade(1, 0).Seq(fWave1, fWave2, fDuck, fEgg0, fEgg1, fEgg0, fEgg2, fEgg0).Then(100, 99);

		p.Readme = """
			# Pipoca the Chick

			## Description

			A little yellow chick in pixel art, round like a ball, with a little tuft of feathers,
			pink cheeks, an orange beak and orange feet.

			He pecks the ground, peeps, runs to eat a worm that pops out far away on the
			taskbar, hides in his egg when the hawk circles over him, follows his mom,
			chases a butterfly, pops a corn kernel into popcorn and naps in his shell.

			## About pet

			Author: Luan Felipe

			## Animations

			* waddle, run, turn, hop onto windows, fall (flapping) and land
			* peck, peep, crow (well, "piu!"), dance, laugh, sneeze, shake (and lose a feather), stomp (footprints)
			* a worm far away, the hawk, the mother hen, a butterfly, a ladybug, a snail, a frog, an ant, a rolling egg
			* corn kernel to popcorn, dandelion, balloon, a flower, dust bath, a rain cloud, nap in the egg
			* reacts to the app: thinks, reads, types on a laptop and celebrates with Claude Code; drinks water on the
			  water reminder; leaf umbrella, scarf and sweat with the weather; party at the end of a Pomodoro
			* climb the screen border, sit and sleep on the taskbar
			* drag and toss with the mouse
			""";
		p.ReadmePt = """
			# Pipoca, o Pintinho

			## Descrição

			Um pintinho amarelinho em pixel art, redondo como uma bolinha, com um topetinho
			de penas, bochechas rosadas, bico e pezinhos laranja.

			Ele bica o chão, faz "piu", corre para comer uma minhoca que aparece longe na
			barra de tarefas, se esconde no ovo quando o gavião sobrevoa, segue a mamãe
			galinha, persegue uma borboleta, estoura milho em pipoca e cochila na casca.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* andar, correr, virar, subir nas janelas, cair (batendo as asinhas) e pousar
			* bicar, piar, tentar cantar como galo ("piu!"), dançar, rir, espirrar, chacoalhar (e perder uma pena), bater o pé (pegadas)
			* minhoca longe, gavião, mamãe galinha, borboleta, joaninha, caracol, sapo, formiga, ovo rolando
			* milho que vira pipoca, dente-de-leão, balão, florzinha, banho de terra, nuvem de chuva, cochilo no ovo
			* reage ao programa: pensa, lê, digita no notebook e comemora com o Claude Code; bebe água no lembrete
			  de água; guarda-chuva de folha, cachecol e suor conforme o clima; festa no fim do Pomodoro
			* escalar as bordas da tela, sentar e dormir na barra de tarefas
			* arrastar e jogar com o mouse
			""";

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
