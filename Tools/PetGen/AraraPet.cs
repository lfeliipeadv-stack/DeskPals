using System.Drawing;

namespace PetGen;

/// <summary>
/// "Pitanga" — a scarlet macaw (arara-vermelha) in pixel art. She is put together from parts drawn as
/// character grids — head, body, folded wing, open wings, tail and feet — so every pose is a new mix of
/// the same parts (head bobbing, wings up or down, one foot raised holding a fruit...).
/// She faces left, like every DesktopPet pet. Frames are 32 px wide with 6 rows of headroom; the feet
/// stand on row 31.
/// </summary>
static class AraraPet
{
	const int W = 32, Head = 6;
	const char Out = 'O', Red = 'R', RedLight = 'L', RedDark = 'D', White = 'W', Face = 'w', Eye = 'E';
	const char Beak = 'P', BeakShade = 'p', BeakBlack = 'K', Tongue = 'T';
	const char Yellow = 'Y', YellowDark = 'y', Green = 'G', Blue = 'B', BlueDark = 'b', Foot = 'F', FootDark = 'f';
	const char Zz = 'Z', Heart = 'H', HeartLight = 'h', Spark = 'S', Nut = 'N', NutDark = 'n', Mango = 'M', MangoRed = 'm';
	const char Leaf = 'V', LeafDark = 'v', Water = 'A', Note = 'q', BlueLight = 'U', Cloud = 'c', CloudShade = 'k', Line = 'l';
	// the bird's own colours (they change with the species; the props keep theirs): wing yellow, wing yellow dark,
	// wing green, wing blue, wing blue dark, beak, beak shade, face, face line; and a fixed scarlet for the red friend
	const char W1 = '1', W2 = '2', W3 = '3', W4 = '4', W5 = '5', BeakC = '6', BeakShadeC = '7', FaceC = '8', FaceLineC = '9';
	const char Scarlet = 'r', ScarletLight = 'i', ScarletDark = 'd';

	// ---------------------------------------------------------------- the parts ('.' = nothing)
	// head with the beak, facing left; the eye is the 2x2 'E' block at columns 6-7, rows 5-6
	static readonly string[] HeadArt =
	{
		".......OOOOO....",   //  0
		".....OORRRRROO..",   //  1
		"....ORRLLLRRRRO.",   //  2
		"...O888LLRRRRRRO",   //  3
		"..O68888RRRRRRRO",   //  4
		".O6689EE8RRRRRRO",   //  5 eye
		"O66689EE8RRRRRRO",   //  6 eye
		"O67668888RRRRRRO",   //  7
		"O67OKK88RRRRRRO.",   //  8 lower beak (black)
		".OO.OKKKRRRRRRO.",   //  9
		"....OKKKORRRRO..",   // 10
		".....OOO.ORRRO..",   // 11
	};
	const int HX = 3, HY = 6;   // where the head sits in a standing pose

	static readonly string[] BodyArt =
	{
		"..ORRRRRRO....",
		".ORRRRRRRRO...",
		"ORLRRRRRRRRO..",
		"ORLRRRRRRRRRO.",
		"ORLRRRRRRRRRO.",
		"ORRRRRRRRRRRO.",
		"ORRRRRRRRRRRO.",
		"ORRRRRRRRRRRO.",
		".ORRRRRRRRRRO.",
		".ORRRRRRRRRO..",
		"..ODRRRRRRDO..",
		"...ODDDDDDO...",
		"....OOOOOO....",
	};
	const int BX = 8, BY = 16;

	// the wing folded along the side: yellow coverts, a green band, blue flight feathers
	static readonly string[] WingArt =
	{
		"..OOOOO.....",
		".O11111OO...",
		"O11211111O..",
		"O111121111O.",
		"O333333333O.",
		"O4444444444O",
		"O4544544444O",
		"O4544544544O",
		".O544544544O",
		".O454454454O",
		"..O45445445O",
		"..O44544544O",
		"...O4454454O",
		"....O44454O.",
		".....OOOOO..",
	};
	const int WX = 14, WY = 15;

	// wing raised (flapping up / taking off); the shoulder is the bottom-left corner
	static readonly string[] WingUpArt =
	{
		"..........OOO.",
		"........OO554O",
		"......OO5444O.",
		".....O544444O.",
		"....O544454O..",
		"...O5445444O..",
		"...O445444O...",
		"..O3333344O...",
		"..O111133O....",
		".O112111O.....",
		"O111111O......",
		"O11211O.......",
		".O111O........",
		"..OOO.........",
	};
	// wing swept down (the down stroke)
	static readonly string[] WingDownArt =
	{
		"OOOO..........",
		"O111OO........",
		"O12111OO......",
		".O111133OO....",
		"..O3334444OO..",
		"...O44454444O.",
		"....O54454444O",
		".....O5445444O",
		"......OO5444O.",
		"........OOOO..",
	};
	// the long tail: red, blue at the tip
	static readonly string[] TailArt =
	{
		"OO........",
		"ORO.......",
		"ORRO......",
		".ORRO.....",
		".ORRRO....",
		"..ORRRO...",
		"..ORRRRO..",
		"...ORRRRO.",
		"...ORRR4O.",
		"....OR444O",
		"....O4444O",
		".....OOOO.",
	};
	const int TX = 22, TY = 20;
	// one foot (zygodactyl: two toes forward, two back)
	static readonly string[] FootArt =
	{
		".OF.",
		"OFFO",
		"FfFf",
	};

	public static PetDef Build() => Build(false);
	/// <summary>"Açaí", the hyacinth macaw (arara-azul-grande): the same pet, cobalt blue with a yellow eye ring.</summary>
	public static PetDef BuildAzul() => Build(true);

	static PetDef Build(bool azul)
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		var p = new PetDef
		{
			Folder = azul ? "arara_azul" : "arara",
			Title = azul ? "Açaí the Hyacinth Macaw" : "Pitanga the Macaw",
			PetName = azul ? "Açaí" : "Pitanga",
			Author = "Luan Felipe",
			Version = "1.0",
			Info = azul
				? "Açaí is a hyacinth macaw (arara-azul) from Brazil.[br]" +
				  "He waddles along your windows, flies across the screen, squawks,[br]" +
				  "says hi, dances, cracks Brazil nuts and eats mango."
				: "Pitanga is a scarlet macaw from Brazil.[br]" +
				  "She waddles along your windows, flies across the screen, squawks,[br]" +
				  "says hi, dances, cracks Brazil nuts and eats mango.",
			Palette = new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(58, 18, 24),
				[Red] = Color.FromArgb(222, 38, 44),
				[RedLight] = Color.FromArgb(246, 98, 86),
				[RedDark] = Color.FromArgb(160, 24, 36),
				[White] = Color.FromArgb(252, 248, 242),
				[Face] = Color.FromArgb(226, 206, 206),
				[Eye] = Color.FromArgb(20, 18, 20),
				[Beak] = Color.FromArgb(240, 230, 206),
				[BeakShade] = Color.FromArgb(188, 176, 152),
				[BeakBlack] = Color.FromArgb(34, 32, 36),
				[Tongue] = Color.FromArgb(70, 56, 60),
				[Yellow] = Color.FromArgb(252, 208, 48),
				[YellowDark] = Color.FromArgb(214, 160, 26),
				[Green] = Color.FromArgb(64, 168, 84),
				[Blue] = Color.FromArgb(38, 112, 214),
				[BlueDark] = Color.FromArgb(24, 66, 152),
				[Foot] = Color.FromArgb(120, 116, 120),
				[FootDark] = Color.FromArgb(76, 72, 78),
				[Zz] = Color.FromArgb(150, 170, 240),
				[Heart] = Color.FromArgb(228, 62, 98), [HeartLight] = Color.FromArgb(255, 150, 170),
				[Spark] = Color.FromArgb(255, 222, 80),
				[Nut] = Color.FromArgb(150, 96, 52), [NutDark] = Color.FromArgb(96, 58, 30),
				[Mango] = Color.FromArgb(252, 168, 40), [MangoRed] = Color.FromArgb(232, 86, 50),
				[Leaf] = Color.FromArgb(70, 150, 60),
				[Water] = Color.FromArgb(120, 190, 240),
				[Note] = Color.FromArgb(120, 84, 220),
				[LeafDark] = Color.FromArgb(40, 104, 44),
				[BlueLight] = Color.FromArgb(110, 170, 240),
				[Cloud] = Color.FromArgb(236, 240, 246), [CloudShade] = Color.FromArgb(170, 180, 196),
				[Line] = Color.FromArgb(150, 150, 150),
				[Scarlet] = Color.FromArgb(222, 38, 44), [ScarletLight] = Color.FromArgb(246, 98, 86), [ScarletDark] = Color.FromArgb(160, 24, 36),
			},
			Scale = 2,
			TilesX = 8,
		};
		// the bird's own colours: scarlet macaw (red, yellow-green-blue wings, white face, ivory beak)
		// or hyacinth macaw (all cobalt blue, yellow eye ring and beak base, dark beak)
		var bird = azul
			? new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(16, 22, 58),
				[Red] = Color.FromArgb(40, 86, 196), [RedLight] = Color.FromArgb(92, 144, 232), [RedDark] = Color.FromArgb(24, 52, 138),
				[W1] = Color.FromArgb(52, 104, 212), [W2] = Color.FromArgb(34, 74, 170), [W3] = Color.FromArgb(44, 92, 196),
				[W4] = Color.FromArgb(32, 68, 168), [W5] = Color.FromArgb(20, 42, 116),
				[BeakC] = Color.FromArgb(56, 58, 66), [BeakShadeC] = Color.FromArgb(34, 34, 40),
				[FaceC] = Color.FromArgb(40, 86, 196), [FaceLineC] = Color.FromArgb(40, 86, 196),
			}
			: new Dictionary<char, Color>
			{
				[W1] = Color.FromArgb(252, 208, 48), [W2] = Color.FromArgb(214, 160, 26), [W3] = Color.FromArgb(64, 168, 84),
				[W4] = Color.FromArgb(38, 112, 214), [W5] = Color.FromArgb(24, 66, 152),
				[BeakC] = Color.FromArgb(240, 230, 206), [BeakShadeC] = Color.FromArgb(188, 176, 152),
				[FaceC] = Color.FromArgb(252, 248, 242), [FaceLineC] = Color.FromArgb(226, 206, 206),
			};
		foreach (var kv in bird) p.Palette[kv.Key] = kv.Value;

		// ------------------------------------------------------------ helpers
		void St(Frame f, int x, int y, string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		string[] Mirror(string[] rows) => rows.Select(r => new string(r.Reverse().ToArray())).ToArray();
		string[] FlipV(string[] rows) => rows.Reverse().ToArray();

		// head: eyes 0 open, 1 closed, 2 happy ^ ^, 3 wide, 4 looking up-left; beak 0 closed, 1 open, 2 wide open
		void DrawHead(Frame f, int x, int y, int eyes, int beak, bool mirrored = false)
		{
			var h = HeadArt.Select(r => r.ToCharArray()).ToArray();
			void S(int c, int r, char ch) { h[r][c] = ch; }
			// eye
			S(6, 5, FaceC); S(7, 5, FaceC); S(6, 6, FaceC); S(7, 6, FaceC);
			if (azul) foreach (var (c, r) in new[] { (5, 5), (5, 6), (8, 5), (8, 6), (6, 4), (7, 4), (6, 7), (7, 7), (4, 8) }) S(c, r, Yellow);   // the yellow eye ring and the yellow at the beak
			switch (eyes)
			{
				case 0: S(6, 5, azul ? Yellow : FaceC); S(7, 5, Eye); S(6, 6, Eye); S(7, 6, Eye); break;
				case 1: S(5, 6, Eye); S(6, 6, Eye); S(7, 6, Eye); S(8, 6, Eye); break;
				case 2: S(5, 6, Eye); S(6, 5, Eye); S(7, 5, Eye); S(8, 6, Eye); break;
				case 3: S(6, 4, Eye); S(7, 4, Eye); S(6, 5, Eye); S(7, 5, azul ? Yellow : FaceC); S(6, 6, Eye); S(7, 6, Eye); break;
				case 4: S(5, 5, Eye); S(6, 5, Eye); S(5, 6, azul ? Yellow : FaceC); S(6, 6, Eye); S(7, 6, azul ? Yellow : FaceC); break;
				case 5: S(5, 5, azul ? Yellow : FaceLineC); S(6, 5, Out); S(7, 5, Out); S(6, 6, Eye); S(7, 6, Eye); break;            // half closed (tired)
				case 6: S(6, 5, azul ? Yellow : FaceC); S(7, 5, Eye); S(6, 6, Eye); S(7, 6, Eye); S(5, 4, Out); S(6, 4, Out); S(4, 3, Out); break;   // angry brow
				case 7: S(6, 5, Water); S(7, 5, Eye); S(6, 6, Eye); S(7, 6, Water); break;                       // teary
			}
			// beak open: the black lower half drops and the dark mouth shows
			if (beak >= 1)
			{
				S(4, 8, Tongue); S(5, 8, Tongue); S(3, 8, BeakShadeC);
				S(4, 9, Out); S(5, 9, Tongue); S(6, 9, BeakBlack); S(7, 9, BeakBlack);
				S(5, 10, BeakBlack); S(6, 10, BeakBlack); S(7, 10, BeakBlack); S(4, 10, Out);
				S(5, 11, Out); S(6, 11, Out); S(7, 11, Out);
				if (beak == 2) { S(5, 10, Tongue); S(4, 11, Out); S(3, 9, '.'); S(3, 10, '.'); }
			}
			var rows = h.Select(r => new string(r)).ToArray();
			St(f, x, y, mirrored ? Mirror(rows) : rows);
		}
		// feet: 0 both down, 1 left foot up, 2 right foot up, 3 tucked (flying), 4 left foot raised high (holding)
		void DrawFeet(Frame f, int feet, int dy = 0)
		{
			if (feet == 3) { f.Rect(12, 29 + dy, 2, 1, Foot).Rect(16, 29 + dy, 2, 1, Foot); return; }
			int ly = feet == 1 ? 28 : feet == 4 ? 23 : 29, ry = feet == 2 ? 28 : 29;
			if (feet == 4) { St(f, 6, ly + dy, new[] { "OFFO", "FfFf", ".OF." }); }
			else St(f, 11, ly + dy, FootArt);
			St(f, 16, ry + dy, FootArt);
			if (feet != 4) f.Rect(12, ly + dy - 1, 1, 1, Foot);
			f.Rect(17, ry + dy - 1, 1, 1, Foot);
		}
		// wing: 0 folded, 1 up, 2 down, 3 half (between), 4 none (preening/holding: folded but lower)
		void DrawWing(Frame f, int wing, int dy)
		{
			switch (wing)
			{
				case 0: St(f, WX, WY + dy, WingArt); break;
				case 1: St(f, 14, 3 + dy, WingUpArt); St(f, WX, WY + 2 + dy, WingArt.Take(6).ToArray()); break;
				case 2: St(f, 14, 16 + dy, WingDownArt); break;
				case 3: St(f, 14, 10 + dy, WingUpArt.Skip(5).ToArray()); St(f, 15, 18 + dy, WingDownArt.Take(4).ToArray()); break;
			}
		}
		// the far wing (behind the body), seen above the back when both wings open
		void FarWing(Frame f, int wing, int dy)
		{
			if (wing == 1) St(f, 18, 1 + dy, Mirror(WingUpArt.Take(9).ToArray()).Select(r => r.Replace(W1, W5).Replace(W2, W5).Replace(W3, W5).Replace(W4, W5)).ToArray());
		}

		Frame Pose(int eyes = 0, int beak = 0, int wing = 0, int feet = 0, int hdx = 0, int hdy = 0, int bodyDy = 0, bool headBack = false)
		{
			var f = new Frame();
			if (wing == 1) FarWing(f, wing, bodyDy);
			St(f, TX, TY + bodyDy, TailArt);
			St(f, BX, BY + bodyDy, BodyArt);
			DrawFeet(f, feet, feet == 3 ? bodyDy : 0);
			if (wing != 1) DrawWing(f, wing, bodyDy);
			if (headBack) DrawHead(f, 13 + hdx, HY + 3 + hdy + bodyDy, eyes, beak, true);
			else DrawHead(f, HX + hdx, HY + hdy + bodyDy, eyes, beak);
			if (wing == 1) DrawWing(f, wing, bodyDy);
			return f;
		}
		// crouch: the body sinks n rows, the feet stay
		Frame Low(int n, int eyes = 0, int beak = 0, int wing = 0) => Pose(eyes, beak, wing, 0, 0, n, n);
		void Zed(Frame f, int x, int y) => St(f, x, y, new[] { "ZZZZ", "..Z.", ".Z..", "ZZZZ" });
		void HeartAt(Frame f, int x, int y) => St(f, x, y, new[] { "HH.HH", "HhHHH", ".HHH.", "..H.." });
		void NoteAt(Frame f, int x, int y) => St(f, x, y, new[] { ".qq", ".q.", "qq." });
		void Drop(Frame f, int x, int y) => f.Dot(x, y, Water).Dot(x, y + 1, Water);
		void NutAt(Frame f, int x, int y, int k)
		{
			if (k == 0) St(f, x, y, new[] { ".NN.", "NNNn", "NnNn", ".nn." });
			else St(f, x, y, new[] { "N..N", "Nn.n", ".n.." });
		}
		void MangoAt(Frame f, int x, int y, int bites)
		{
			string[] m = { "..V..", ".MMM.", "MMMMm", "MMMmm", ".mmm." };
			St(f, x, y, m);
			if (bites >= 1) { f.Dot(x, y + 1, '.').Dot(x + 1, y + 1, '.').Dot(x, y + 2, Spark); }
			if (bites >= 2) { f.Dot(x + 1, y + 2, '.').Dot(x, y + 2, '.').Dot(x + 1, y + 3, Spark); f.Dot(x + 2, y + 1, '.'); }
		}
		// speech bubble "OI!" above the head
		void SayHi(Frame f)
		{
			St(f, 0, -6, new[]
			{
				".OOOOOOOOOOO.",
				"OWWWWWWWWWWWO",
				"OWEEEWEWEWWWO",
				"OWEWEWEWEWWWO",
				"OWEWEWEWEWWWO",
				"OWEEEWEWWWWWO",
				"OWWWWWWWEWWWO",
				".OOOOOOOOOOO.",
				"....OWO......",
				".....O.......",
			});
		}
		void Spin(Frame f, int k)
		{
			foreach (var (x, y) in new[] { (2, 2), (27, 4), (1, 20), (29, 15) }) f.Dot(x + (k == 1 ? 1 : 0), y - k, Spark);
		}

		// ------------------------------------------------------------ frames
		int fStand = p.Add(Pose());
		int fBlink = p.Add(Pose(1));
		int fHappy = p.Add(Pose(2));
		int fWide = p.Add(Pose(3, 1));
		int fLookUp = p.Add(Pose(4, 0, 0, 0, 0, -1));
		int fLookBack = p.Add(Pose(0, 0, 0, 0, 0, 0, 0, true));

		// waddle: the body rocks and one foot lifts at a time
		int fWalk1 = p.Add(Pose(0, 0, 0, 1, -1, 0));
		int fWalk2 = p.Add(Pose(0, 0, 0, 0, 0, 1, 1));
		int fWalk3 = p.Add(Pose(0, 0, 0, 2, 1, 0));

		// squawk: head forward, beak wide open
		int fSquawk1 = p.Add(Pose(3, 2, 3, 0, -1, -1));
		int fSquawk2 = p.Add(Pose(3, 1, 0, 0, -1, 0));

		// flight
		int fFlyUp = p.Add(Pose(0, 0, 1, 3, -1, 0, -2));
		int fFlyMid = p.Add(Pose(0, 0, 3, 3, -1, 0, -2));
		int fFlyDown = p.Add(Pose(0, 0, 2, 3, -1, 0, -2));
		int fGlide = p.Add(Pose(2, 0, 2, 3, -1, 0, -3));
		int fTakeoff = p.Add(Low(2, 3, 0, 3));

		// falling, held, landing
		int fFall = p.Add(Pose(3, 1, 1, 3));
		int fDrag1 = p.Add(Pose(3, 2, 1, 3));
		int fDrag2 = p.Add(Pose(3, 1, 3, 3));
		int fCrouch = p.Add(Low(2));
		int fLand = p.Add(Low(3, 1, 1, 3));
		var flat = Low(5, 1, 0, 2); flat.Dot(4, 5, Spark).Dot(18, 3, Spark).Dot(11, 1, Spark); int fDizzy = p.Add(flat);

		// sleeping: head lowered, eyes closed, Zs
		Frame Sleep(int z)
		{
			var f = Pose(1, 0, 0, 0, 2, 3, 1);
			if (z >= 1) Zed(f, 22, 6);
			if (z >= 2) Zed(f, 26, 1);
			return f;
		}
		int fSit = p.Add(Low(2));
		int fSleep1 = p.Add(Sleep(0)), fSleep2 = p.Add(Sleep(1)), fSleep3 = p.Add(Sleep(2));

		// head-bob dance
		int fBob1 = p.Add(Pose(2, 0, 0, 1, -2, -1));
		int fBob2 = p.Add(Pose(2, 1, 0, 0, 0, 2, 1));
		int fBob3 = p.Add(Pose(2, 0, 0, 2, 2, -1));

		// wave with the wing
		int fWave1 = p.Add(Pose(2, 0, 1));
		int fWave2 = p.Add(Pose(2, 0, 3));

		// "OI!"
		var hi1 = Pose(2, 1); SayHi(hi1); int fHi1 = p.Add(hi1);
		var hi2 = Pose(2, 0); SayHi(hi2); int fHi2 = p.Add(hi2);

		// preening: the head turns back into the wing
		int fPreen1 = p.Add(Pose(1, 0, 0, 0, 0, 1, 0, true));
		int fPreen2 = p.Add(Pose(1, 0, 0, 0, 1, 2, 0, true));

		// Brazil nut: held in the foot, cracked with the beak
		var n1 = Pose(0, 0, 0, 4, 0, 1); NutAt(n1, 5, 18, 0); int fNutHold = p.Add(n1);
		var n2 = Pose(3, 1, 0, 4, 0, 3); NutAt(n2, 5, 18, 0); int fNutBite = p.Add(n2);
		var n3 = Pose(2, 0, 0, 0, 0, 1); NutAt(n3, 3, 28, 1); n3.Dot(9, 22, Spark).Dot(2, 20, Spark); int fNutCrack = p.Add(n3);
		var n4 = Pose(2, 1, 0, 0, 0, 1); NutAt(n4, 3, 28, 1); int fNutChew = p.Add(n4);

		// mango: held in the foot, bitten twice
		var m0 = Pose(0, 0, 0, 4, 0, 1); MangoAt(m0, 4, 16, 0); int fMango0 = p.Add(m0);
		var m1 = Pose(3, 1, 0, 4, 0, 3); MangoAt(m1, 4, 16, 0); int fMangoBite = p.Add(m1);
		var m2 = Pose(2, 0, 0, 4, 0, 1); MangoAt(m2, 4, 16, 1); int fMango1 = p.Add(m2);
		var m3 = Pose(2, 0, 0, 4, 0, 1); MangoAt(m3, 4, 16, 2); int fMango2 = p.Add(m3);
		var m4 = Pose(2, 1); m4.Dot(2, 18, Mango).Dot(4, 17, Mango); int fYum = p.Add(m4);

		// feather fluff / shake
		var sh1 = Pose(1, 0, 3); sh1.Shift(-1, 0); Spin(sh1, 0); int fShake1 = p.Add(sh1);
		var sh2 = Pose(1, 0, 0); sh2.Shift(1, 0); Spin(sh2, 1); int fShake2 = p.Add(sh2);

		// love
		var lv1 = Pose(2); HeartAt(lv1, 1, -2); int fLove1 = p.Add(lv1);
		var lv2 = Pose(2, 0, 0, 0, 0, 1, 1); HeartAt(lv2, 2, -5); HeartAt(lv2, 24, 2); int fLove2 = p.Add(lv2);

		// whistle / sing
		var sg1 = Pose(1, 1); NoteAt(sg1, 1, 0); int fSing1 = p.Add(sg1);
		var sg2 = Pose(2, 1, 0, 0, -1, -1); NoteAt(sg2, 0, -4); NoteAt(sg2, 6, -2); int fSing2 = p.Add(sg2);

		// yawn and stretch (one wing out)
		int fYawn = p.Add(Pose(1, 2, 0, 0, 0, -1));
		int fStretch = p.Add(Pose(1, 0, 2, 2));

		// bath: shakes in the rain of drops
		Frame Bath(int k)
		{
			var f = Pose(2, 1, k == 0 ? 1 : 2);
			foreach (var (x, y) in new[] { (3, 3), (10, -2), (25, 1), (6, 14), (29, 10), (1, 24) }) Drop(f, x, y + k * 2);
			return f;
		}
		int fBath1 = p.Add(Bath(0)), fBath2 = p.Add(Bath(1));

		// climb: wings out, beak up
		int fClimb1 = p.Add(Pose(4, 1, 1, 1, 0, -1));
		int fClimb2 = p.Add(Pose(4, 0, 2, 2, 0, -2));

		// upside down: hangs by the feet from the window (the pose flipped)
		Frame Upside(int k)
		{
			var f = Pose(2, k, 0, 0);
			var g = new Frame();
			for (int y = 0; y < W; y++) for (int x = 0; x < W; x++) g[x, W - 1 - y] = f[x, y];
			return g;
		}
		int fUpside1 = p.Add(Upside(0)), fUpside2 = p.Add(Upside(1));

		// child: a feather that floats down
		var feather = new Frame(); St(feather, 4, 4, new[] { ".R..", "RLR.", "RRB.", ".RB.", "..O." }); int fFeather1 = p.Add(feather);
		var feather2 = new Frame(); St(feather2, 3, 4, new[] { "..R.", ".RLR", ".BRR", ".BR.", ".O.." }); int fFeather2 = p.Add(feather2);

		// ================================================================ 20 MORE (40-59, most with a child window)
		Frame Empty() => new Frame();
		Frame Recolor(Frame src, Dictionary<char, char> map)
		{
			var f = src.Clone();
			for (int y = f.MinY; y < W; y++) for (int x = 0; x < W; x++) if (map.TryGetValue(f[x, y], out char c)) f[x, y] = c;
			return f;
		}
		void Ball(Frame f, int x, int y, int k) => St(f, x, y, k == 0
			? new[] { "..OOO..", ".OWWWO.", "OWWEWWO", "OWEEEWO", "OEWEWEO", ".OWWWO.", "..OOO.." }
			: new[] { "..OOO..", ".OWWWO.", "OEWEWEO", "OWEEEWO", "OWWEWWO", ".OWWWO.", "..OOO.." });
		void Seed(Frame f, int x, int y) => St(f, x, y, new[] { ".PP.", "PPPp", ".pp." });
		void Kite(Frame f, int x, int y, int k)
		{
			St(f, x, y, new[] { "...O...", "..OGO..", ".OGYGO.", "OGYBYGO", ".OGYGO.", "..OGO..", "...O..." });
			// the tail with two bows
			f.Dot(x + 3, y + 7, Line).Dot(x + 3 + k, y + 8, Line).Dot(x + 3, y + 9, Line).Dot(x + 2 + k, y + 9, Heart).Dot(x + 4 + k, y + 9, Heart).Dot(x + 3 - k, y + 10, Line).Dot(x + 3, y + 11, Line);
		}
		void Coconut(Frame f, int x, int y) => St(f, x, y, new[] { ".OOOO.", "ONnNNO", "ONNnNO", "OnNNNO", "ONNNnO", ".OOOO." });
		void Butterfly(Frame f, int x, int y, int k) => St(f, x, y, k == 0
			? new[] { "BB.BB", "UBEBU", ".BEB.", "B...B" }
			: new[] { ".....", ".BEB.", ".UEU.", "....." });
		void Sprout(Frame f, int x, int stage)
		{
			f.Rect(x, 31, 7, 1, NutDark).Rect(x + 1, 30, 5, 1, Nut);
			if (stage >= 1) f.Rect(x + 3, 28, 1, 2, Leaf).Dot(x + 2, 28, Leaf).Dot(x + 4, 27, Leaf);
			if (stage >= 2) { f.Rect(x + 3, 24, 1, 4, Leaf); St(f, x, 24, new[] { "VV.....", ".VV....", "......." }); St(f, x + 4, 25, new[] { ".VV", "VV." }); }
			if (stage >= 3) St(f, x + 1, 19, new[] { ".HSH.", "HHSHH", ".HSH.", "..V.." });
			if (stage == 4) { for (int y = 19; y < 30; y++) for (int i = 0; i < 7; i++) if (f[x + i, y] == Heart || f[x + i, y] == Leaf) f[x + i, y] = y > 23 ? LeafDark : '.'; }
		}
		void Egg(Frame f, int x, int y, int crack)
		{
			St(f, x, y, new[] { ".OOO.", "OWWWO", "OWWWO", "OWWWO", ".OOO." });
			if (crack >= 1) f.Dot(x + 1, y + 2, Out).Dot(x + 2, y + 1, Out).Dot(x + 3, y + 2, Out);
			if (crack >= 2) f.Rect(x + 1, y, 3, 2, '.').Dot(x + 2, y, Red).Dot(x + 1, y + 1, Red).Dot(x + 3, y + 1, Red);
		}
		void Chick(Frame f, int x, int y, int k) => St(f, x, y, new[] { ".RR..", "PWER.", ".RRR.", ".RYB.", k == 0 ? ".F.F." : "F...F" });
		void CloudAt(Frame f, int x, int y, int k)
		{
			St(f, x, y, new[] { "...kkkk.....", ".kkccccck...", "kccccccccckk", "kcccccccccck", ".kkkkkkkkkk." });
			for (int i = 0; i < 5; i++) Drop(f, x + 1 + i * 2 + (k == 1 ? 1 : 0), y + 6 + ((i + k) % 2) * 3);
		}
		void Confetti(Frame f, int k)
		{
			char[] cs = { Heart, Spark, Blue, Green, Yellow };
			for (int i = 0; i < 18; i++) f.Dot((i * 7 + k * 3) % W, -6 + (i * 5 + k * 4) % 30, cs[(i + k) % cs.Length]);
		}
		void Bubble(Frame f, string[] glyphs)
		{
			St(f, 0, -6, new[] { ".OOOOOOOOOOO.", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", "OWWWWWWWWWWWO", ".OOOOOOOOOOO.", "....OWO......", ".....O......." });
			St(f, 2, -5, glyphs);
		}
		void Pandeiro(Frame f, int x, int y) => St(f, x, y, new[] { "..OSO..", ".Occco.", "OScccSO", ".Occco.", "..OSO.." }.Select(r => r.Replace('o', 'O')).ToArray());

		// 40 soccer ball: kicked, it bounces away
		var bk0 = Pose(0, 0, 0, 0); Ball(bk0, 2, 26, 0); int fBallHold = p.Add(bk0);
		var bk1 = Pose(3, 1, 3, 1, -1); int fKick = p.Add(bk1);
		var bb0 = Empty(); Ball(bb0, 2, 26, 0); int fBall1 = p.Add(bb0);
		var bb1 = Empty(); Ball(bb1, 2, 26, 1); int fBall2 = p.Add(bb1);
		// 41 mango seed: spat out, it flies in an arc and rolls
		var sd0 = Pose(3, 1, 0, 0, -1); Seed(sd0, 1, 15); int fSeedMouth = p.Add(sd0);
		int fSpit = p.Add(Pose(6 - 4, 2, 0, 0, -2, -1));
		var sdf = Empty(); Seed(sdf, 1, 15); int fSeedFly = p.Add(sdf);
		var sdr = Empty(); Seed(sdr, 1, 29); int fSeedRest = p.Add(sdr);
		// 42 shout "OI!": the bubble floats up on its own
		var echo = Empty(); SayHi(echo); int fEcho = p.Add(echo);
		// 43 a blue friend (arara-azul) flies past
		// the friend is the other species: a blue friend for Pitanga, a scarlet one for Açaí (fixed prop colours)
		var friendMap = azul
			? new Dictionary<char, char> { [Red] = Scarlet, [RedLight] = ScarletLight, [RedDark] = ScarletDark, [W1] = Yellow, [W2] = YellowDark, [W3] = Green, [W4] = Blue, [W5] = BlueDark, [BeakC] = Beak, [BeakShadeC] = BeakShade, [FaceC] = White, [FaceLineC] = Face }
			: new Dictionary<char, char> { [Red] = Blue, [RedLight] = BlueLight, [RedDark] = BlueDark, [W1] = BlueLight, [W2] = Blue, [W3] = Blue, [W4] = Blue, [W5] = BlueDark, [BeakC] = Eye, [BeakShadeC] = Eye, [FaceC] = Yellow, [FaceLineC] = Yellow };
		int fFriend1 = p.Add(Recolor(Pose(2, 0, 1, 3, -1, 0, -2), friendMap));
		int fFriend2 = p.Add(Recolor(Pose(2, 0, 2, 3, -1, 0, -2), friendMap));
		// 44 kite (pipa): held by the string, then let go
		var kh = Pose(4, 0, 0, 0, 0, -1); Kite(kh, 0, -6, 0); for (int i = 0; i < 8; i++) kh.Dot(3 + i / 4, 6 - i, Line); int fKiteHold = p.Add(kh);
		var kf0 = Empty(); Kite(kf0, 0, -6, 0); int fKite1 = p.Add(kf0);
		var kf1 = Empty(); Kite(kf1, 0, -6, 1); int fKite2 = p.Add(kf1);
		// 45 a coconut falls on her head
		var bonk = Low(3, 1, 2, 3); bonk.Dot(3, 0, Spark).Dot(20, -2, Spark).Dot(23, 4, Spark); int fBonk = p.Add(bonk);
		var cf = Empty(); Coconut(cf, 9, -6); int fCoconut = p.Add(cf);
		var cr = Empty(); Coconut(cr, 9, 26); int fCoconutRest = p.Add(cr);
		// 46 a blue morpho butterfly lands on her beak and flies off
		var bf0 = Pose(4, 0, 0, 0, 0, -1); Butterfly(bf0, 0, 2, 0); int fButterflyBeak = p.Add(bf0);
		var bf1 = Pose(3, 0, 0, 0, 0, -1); Butterfly(bf1, 0, 2, 1); int fButterflyBeak2 = p.Add(bf1);
		var bw0 = Empty(); Butterfly(bw0, 0, 2, 0); int fButterfly1 = p.Add(bw0);
		var bw1 = Empty(); Butterfly(bw1, 0, 2, 1); int fButterfly2 = p.Add(bw1);
		// 47 samba song: notes float away
		var nf = Empty(); NoteAt(nf, 2, 0); int fNote = p.Add(nf);
		// 48 blown kiss
		var kis = Pose(1, 1, 0, 0, -1); kis.Dot(1, 14, Heart); int fKiss = p.Add(kis);
		var kh2 = Empty(); HeartAt(kh2, 0, 10); int fKissHeart = p.Add(kh2);
		// 49 a little rain cloud just for her
		var cl0 = Empty(); CloudAt(cl0, 6, -6, 0); int fCloud1 = p.Add(cl0);
		var cl1 = Empty(); CloudAt(cl1, 6, -6, 1); int fCloud2 = p.Add(cl1);
		var wet = Pose(1, 0, 0, 0, 0, 1, 1); Drop(wet, 7, 3); Drop(wet, 16, 1); Drop(wet, 24, 10); Drop(wet, 2, 17); int fWet = p.Add(wet);
		// 50 plants a seed: a flower grows, then wilts
		var dig1 = Pose(0, 1, 0, 0, -1, 5, 2); int fDig = p.Add(dig1);
		var sp = new int[5]; for (int i = 0; i < 5; i++) { var s = Empty(); Sprout(s, 0, i); sp[i] = p.Add(s); }
		// 51 an egg hatches, the chick walks off
		var eg0 = Pose(4, 0, 0, 0, 0, 2, 1); Egg(eg0, 1, 27, 0); int fEgg0 = p.Add(eg0);
		var eg1 = Pose(3, 0, 0, 0, 0, 2, 1); Egg(eg1, 1, 27, 1); int fEgg1 = p.Add(eg1);
		var eg2 = Pose(3, 1, 0, 0, 0, 2, 1); Egg(eg2, 1, 27, 2); int fEgg2 = p.Add(eg2);
		var ck0 = Empty(); Chick(ck0, 1, 27, 0); int fChick1 = p.Add(ck0);
		var ck1 = Empty(); Chick(ck1, 1, 26, 1); int fChick2 = p.Add(ck1);
		// 52 carnival: confetti rains down
		var cf0 = Empty(); Confetti(cf0, 0); int fConfetti1 = p.Add(cf0);
		var cf1 = Empty(); Confetti(cf1, 1); int fConfetti2 = p.Add(cf1);
		// 53 throws a Brazil nut
		var tn = Pose(0, 1, 0, 0, -1); NutAt(tn, 0, 13, 0); int fNutMouth = p.Add(tn);
		var tnf = Empty(); NutAt(tnf, 0, 13, 0); int fNutFly = p.Add(tnf);
		var tnr = Empty(); NutAt(tnr, 0, 28, 0); int fNutRest = p.Add(tnr);
		// 54 samba
		int fSamba1 = p.Add(Pose(2, 1, 3, 1, -2, 0));
		int fSamba2 = p.Add(Pose(2, 0, 2, 0, 0, 1, 1));
		int fSamba3 = p.Add(Pose(2, 1, 1, 2, 2, 0));
		// 55 sneeze
		int fAh = p.Add(Pose(4, 1, 0, 0, 1, -2));
		var choo = Pose(1, 2, 3, 0, -2, 1); choo.Dot(0, 12, Water).Dot(1, 15, Water).Dot(0, 17, Water).Dot(2, 10, Water); int fChoo = p.Add(choo);
		// 56 laugh "kkk"
		string[] kkk = { "E...E...E..", "E.E.E.E.E.E", "EE..EE..EE.", "E.E.E.E.E.E", "E.E.E.E.E.E" };
		var lg1 = Pose(2, 2); Bubble(lg1, kkk); int fLaugh1 = p.Add(lg1);
		var lg2 = Pose(2, 1, 0, 0, 0, 1, 1); Bubble(lg2, kkk); int fLaugh2 = p.Add(lg2);
		// 57 pandeiro (tambourine)
		var pd1 = Pose(2, 1, 3, 0); Pandeiro(pd1, 0, 14); int fPand1 = p.Add(pd1);
		var pd2 = Pose(1, 0, 1, 0, 0, 1); Pandeiro(pd2, 0, 9); pd2.Dot(1, 7, Spark).Dot(7, 8, Spark); int fPand2 = p.Add(pd2);
		// 58 hiccup
		var hc = Pose(3, 1, 0, 3, 0, -1, -2); int fHic = p.Add(hc);
		// 59 curious: head tilted, a question mark
		string[] q = { "...", "qqq", "..q", ".q.", "...", ".q." };
		var cu1 = Pose(4, 0, 0, 0, 1, -1); St(cu1, 2, -4, q); int fCurious1 = p.Add(cu1);
		var cu2 = Pose(0, 0, 0, 0, -1, 1); St(cu2, 3, -5, q); int fCurious2 = p.Add(cu2);

		// ================================================================ 50 SCENES FOR THE PROGRAM'S FEATURES
		// The program picks animations by name (Claude Code, the AI chat, weather, reminders, Pomodoro, mood,
		// fortune cookie, music, memory care, the virtual pet...): each scene below uses one of those names.
		// head origin in a pose: (3 + hdx, 6 + hdy + bodyDy); the eye is at head (6..7, 5..6)
		void Glasses(Frame f, int hdx = 0, int hdy = 0)
		{
			int x = 3 + hdx + 5, y = 6 + hdy + 4;
			char rim = azul ? Eye : BlueDark;   // dark blue rims vanish on the blue macaw
			for (int i = 0; i < 4; i++) { f.Dot(x + i, y, rim).Dot(x + i, y + 3, rim); }
			f.Dot(x, y + 1, rim).Dot(x, y + 2, rim).Dot(x + 3, y + 1, rim).Dot(x + 3, y + 2, rim).Rect(x + 4, y + 1, 5, 1, rim);
		}
		void Shades(Frame f, int hdx = 0, int hdy = 0)
		{
			int x = 3 + hdx, y = 6 + hdy;
			f.Rect(x + 4, y + 4, 5, 3, Eye).Rect(x + 9, y + 5, 5, 1, Eye).Dot(x + 5, y + 4, White);
		}
		void HatOn(Frame f, string[] hat, int hdx = 0, int hdy = 0) => St(f, 3 + hdx + 4, 6 + hdy - hat.Length + 2, hat);
		string[] beanie = { "....W....", "..GYGYG..", ".GYGYGYG.", "YYYYYYYYY" };
		string[] partyHat = { "...S...", "..HSH..", "..SHS..", ".HSHSH.", ".SHSHS." };
		string[] hardHat = { "..OOOOO...", ".OYYYYYO..", "OYYyYYYYO.", "OOOOOOOOOO" };
		void Headphones(Frame f, int hdx = 0, int hdy = 0)
		{
			int x = 3 + hdx, y = 6 + hdy;
			foreach (var (dx, dy) in new[] { (6, 0), (7, -1), (8, -1), (9, -1), (10, -1), (11, 0), (12, 1) }) f.Dot(x + dx, y + dy, Note);
			f.Rect(x + 11, y + 2, 3, 5, Note).Dot(x + 12, y + 3, BlueLight);
		}
		void Scarf(Frame f) => St(f, 9, 16, new[] { "GYGYGYGYGY", ".GYGYGYGY.", "..YG......", "..GY......", "..YG......" });
		void Sweat(Frame f, int x, int y) => St(f, x, y, new[] { ".A.", "AAA", "AWA", ".A." });
		void Book(Frame f, int x, int y) => St(f, x, y, new[] { "OBBBBOBBBBO", "OWWWWOWWWWO", "OWllWOWllWO", "OWWWWOWWWWO", "OWllWOWllWO", ".OOOOOOOOO." });
		void Papers(Frame f, int x, int y, bool check) { St(f, x, y, new[] { "OOOOOOO", "OWWWWWO", "OWlllWO", "OWWWWWO", "OWlllWO", "OWWWWWO", "OOOOOOO" }); if (check) St(f, x + 3, y + 2, new[] { "...G", "G.G.", ".G.." }); }
		void Bulb(Frame f, int x, int y, bool lit)
		{
			St(f, x, y, new[] { ".SSS.", "SSSSS", "SSWSS", ".SSS.", ".lll.", ".lll." }.Select(r => lit ? r : r.Replace('S', 'c').Replace('W', 'k')).ToArray());
			if (lit) f.Dot(x - 2, y + 1, Spark).Dot(x + 6, y + 1, Spark).Dot(x + 2, y - 2, Spark);
		}
		void Laptop(Frame f, int x, int y, int k)
		{
			St(f, x, y, new[] { "OOOOOOOOOO", "OBBBBBBBBO", "OBGGGBBBBO", "OBBBSSSBBO", "OBGGBBBBBO", "OOOOOOOOOO", "OllllllllO", ".OOOOOOOO." });
			if (k == 1) { f.Dot(x + 6, y + 4, Green).Dot(x + 7, y + 4, Green); }
		}
		void Quill(Frame f, int x, int y) => St(f, x, y, new[] { "S....", ".Y...", "..Y..", "...y.", "....E" });
		void Paper(Frame f, int x, int y, int lines) { St(f, x, y, new[] { "OOOOOOOO", "OWWWWWWO", "OWWWWWWO", "OWWWWWWO", "OOOOOOOO" }); for (int i = 0; i < lines && i < 3; i++) f.Rect(x + 1, y + 1 + i, 2 + i * 2, 1, Note); }
		void LoadingRing(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (0, -3), (2, -2), (3, 0), (2, 2), (0, 3), (-2, 2), (-3, 0), (-2, -2) };
			for (int i = 0; i < 8; i++) f.Dot(cx + pts[i].Item1, cy + pts[i].Item2, ((i - k * 2) % 8 + 8) % 8 < 3 ? Blue : CloudShade);
		}
		void DownloadBox(Frame f, int x, int y) => St(f, x, y, new[] { "OOOOOOO", "OGGWGGO", "OGGWGGO", "OGWWWGO", "OGGWGGO", "OOOOOOO" });
		void Magnifier(Frame f, int x, int y) => St(f, x, y, new[] { ".OOO..", "OAWAO.", "OAAAO.", "OAAAO.", ".OOON.", "....NN" });
		void Telescope(Frame f) { for (int i = 0; i < 9; i++) f.Rect(1 + i, 3 + i, 3, 2, i < 3 ? Spark : Nut); }
		void Check(Frame f) => Bubble(f, new[] { ".........GG", "........GG.", "GG.....GG..", ".GG..GG....", "..GGGG....." });
		void OkSign(Frame f) => Bubble(f, new[] { "..EEE.E.E..", "..E.E.EE...", "..E.E.E....", "..E.E.EE...", "..EEE.E.E.." });
		void Flag(Frame f, int k)
		{
			f.Rect(2, -6, 1, 21, NutDark);
			var flag = new[] { "GGGGGGGGG", "GGGYYYGGG", "GYYBBBYYG", "GYYBBBYYG", "GGGYYYGGG", "GGGGGGGGG" };
			for (int r = 0; r < flag.Length; r++) St(f, 3, -6 + r + (k == 1 && r % 2 == 0 ? 1 : 0), new[] { flag[r] });
		}
		void Warning(Frame f, int x, int y) => St(f, x, y, new[] { "....O....", "...OYO...", "..OYEYO..", "..OYEYO..", ".OYYEYYO.", ".OYYYYYO.", "OYYYEYYYO", "OOOOOOOOO" });
		void Tears(Frame f, int k, int hdy = 0) { Drop(f, 8, 13 + hdy + k * 2); Drop(f, 11, 14 + hdy + (1 - k) * 2); Drop(f, 7, 17 + hdy + k); }
		void Steam(Frame f, int x, int y) => St(f, x, y, new[] { ".cc.", "cckc", ".cc." });
		void Crest(Frame f) { foreach (var (x, y) in new[] { (10, 4), (12, 3), (14, 4), (16, 5), (11, 5), (13, 5) }) f.Dot(x, y, Red); f.Dot(10, 3, Out).Dot(12, 2, Out).Dot(14, 3, Out).Dot(16, 4, Out); }
		void UmbrellaOn(Frame f, int k)
		{
			St(f, 0, -6, new[] { "......OOOOOO......", "...OOGGYYBBGGOO...", ".OGGGYYYBBBYYGGGO.", "OGGYYYBBBBBBYYYGGO", "O..O....O....O...O" });
			f.Rect(9, -2, 1, 14, NutDark).Dot(8, 11, NutDark);
			foreach (var (x, y) in k == 0 ? new[] { (0, 2), (21, 4), (27, 0), (1, 14) } : new[] { (1, 6), (22, 0), (28, 5), (0, 18) }) Drop(f, x, y);
		}
		void Puddle(Frame f, int x, int w) => f.Rect(x, 31, w, 1, Water).Rect(x + 1, 30, w - 2, 1, BlueLight);
		void Sun(Frame f, int x, int y) { St(f, x, y, new[] { "S.S.S", ".SSS.", "SSSSS", ".SSS.", "S.S.S" }); }
		void HandFan(Frame f, int x, int y, int k) => St(f, x, y, k == 0
			? new[] { "RYRYR", "RYRYR", ".RYR.", "..N..", "..N.." }
			: new[] { ".YRY.", "YRYRY", ".YRY.", "..N..", "..N.." });
		void IceCream(Frame f, int x, int y, bool scoop) { if (scoop) St(f, x, y, new[] { ".hhh.", "hHhhh", ".hhh." }); St(f, x, y + 3, new[] { ".NyN.", "..N..", "..y.." }); }
		void Snowflakes(Frame f, int k) { foreach (var (x, y) in new[] { (2, -4), (9, 0), (19, -2), (27, 3), (5, 10), (29, 14), (14, -5) }) f.Dot(x, ((y + k * 3) + 40) % 24 - 5, Zz); }
		void Glass(Frame f, int x, int y, bool tilt) => St(f, x, y, tilt
			? new[] { "OAA...", "OAAAO.", ".OAAAO", "..OOO." }
			: new[] { "O...O", "OAAAO", "OAAAO", "OAWAO", ".OOO." });
		void SeedBowl(Frame f, int x, int y) => St(f, x, y, new[] { ".YyYyY.", "NNNNNNN", ".NnNnN.", "..nnn.." });
		void FortuneCookie(Frame f, int x, int y, bool broken) => St(f, x, y, broken
			? new[] { "YY...YY", "Yy.W.yY", ".Y...Y." }
			: new[] { "..YYY..", ".YYyYY.", "YYyYyYY", ".YYYYY." });
		void Slip(Frame f, int x, int y) => St(f, x, y, new[] { "WWWWW", "WlllW", "WWWWW" });
		void Broom(Frame f, int x, int k) { for (int i = 0; i < 9; i++) f.Dot(x + 7 - i / 2 + k, 18 + i, NutDark); St(f, x + k, 27, new[] { ".yyyy", "yyyyy", "yYyYy", "YyYyY" }); }
		void Dust(Frame f, int x, int y) => St(f, x, y, new[] { "..kk..", ".kcck.", "kcckck", ".kkkk." });
		void Hammer(Frame f, int x, int y, bool up) => St(f, x, y, up ? new[] { "lll.", "lll.", ".N..", ".N..", ".N.." } : new[] { "....", "NNNll", "...ll" });
		void Board(Frame f) => f.Rect(0, 30, 9, 2, Nut).Rect(0, 31, 9, 1, NutDark);
		void CoffeeCup(Frame f, int x, int y) => St(f, x, y, new[] { "OWWWO.", "OnnnOO", "OWWWO.O", ".OOO..", "OOOOO." });
		void PartyHorn(Frame f, int k) { if (k == 0) St(f, 0, 12, new[] { ".HH", "H.H", ".H." }); else { f.Rect(0, 13, 5, 1, Heart).Rect(0, 14, 5, 1, Yellow).Dot(0, 12, Heart); } }

		// --- Claude Code and the AI chat
		var th1 = Pose(4, 0, 0, 0, 0, -1); for (int i = 0; i < 3; i++) th1.Dot(0 + i * 2, 3 - i * 2, Note); int fThink1 = p.Add(th1);
		var th2 = Pose(0, 0, 0, 0, 1, 0); St(th2, 0, -5, new[] { ".kkk.", "kccck", "kccck", ".kkk." }); th2.Dot(3, 0, CloudShade).Dot(5, 2, CloudShade); int fThink2 = p.Add(th2);
		var rv1 = Pose(0, 0, 0, 4, 0, 2); Glasses(rv1, 0, 2); Papers(rv1, 2, 16, false); int fReview1 = p.Add(rv1);
		var rv2 = Pose(2, 0, 0, 4, 0, 2); Glasses(rv2, 0, 2); Papers(rv2, 2, 16, true); int fReview2 = p.Add(rv2);
		var id1 = Pose(3, 0, 0, 0, 0, -1); Bulb(id1, 1, -5, false); int fIdea1 = p.Add(id1);
		var id2 = Pose(2, 1, 1, 0, 0, -1); Bulb(id2, 1, -5, true); int fIdea2 = p.Add(id2);
		var ib = Empty(); Bulb(ib, 1, -5, true); int fBulbChild = p.Add(ib);
		var rd1 = Pose(0, 0, 0, 0, 0, 3, 1); Glasses(rd1, 0, 4); Book(rd1, 0, 20); int fRead1 = p.Add(rd1);
		var rd2 = Pose(1, 0, 0, 0, 1, 3, 1); Glasses(rd2, 1, 4); Book(rd2, 0, 20); rd2.Rect(6, 19, 2, 2, White); int fRead2 = p.Add(rd2);
		var lp1 = Pose(0, 0, 0, 0, 0, 4, 1); Laptop(lp1, 0, 24, 0); int fLaptop1 = p.Add(lp1);
		var lp2 = Pose(3, 1, 0, 0, -1, 5, 1); Laptop(lp2, 0, 24, 1); int fLaptop2 = p.Add(lp2);
		var codeChild = Empty(); St(codeChild, 0, 18, new[] { ".E...E.", "E..E..E", ".E.E.E.", "...E..." }.Select(r => r.Replace('E', 'G')).ToArray()); int fCode = p.Add(codeChild);
		var wr1 = Pose(0, 0, 0, 0, 0, 4, 1); Quill(wr1, 0, 18); Paper(wr1, 0, 27, 1); int fWrite1 = p.Add(wr1);
		var wr2 = Pose(0, 1, 0, 0, -1, 5, 1); Quill(wr2, 1, 19); Paper(wr2, 0, 27, 3); int fWrite2 = p.Add(wr2);
		Frame Loading(int k) { var f = Pose(k % 2 == 0 ? 4 : 0, 0, 0, 0, 0, -1); LoadingRing(f, 5, -2, k); return f; }
		int fLoad1 = p.Add(Loading(0)), fLoad2 = p.Add(Loading(1)), fLoad3 = p.Add(Loading(2)), fLoad4 = p.Add(Loading(3));
		var dl1 = Pose(4, 1, 1, 0, 0, -1); int fDlLook = p.Add(dl1);
		var dl2 = Pose(2, 0, 2, 4, 0, 1); DownloadBox(dl2, 3, 15); int fDlCatch = p.Add(dl2);
		var dlc = Empty(); DownloadBox(dlc, 3, 0); int fDlChild = p.Add(dlc);
		var dt1 = Pose(0, 0, 0, 0, -1, 3, 1); Magnifier(dt1, 0, 15); int fDetective1 = p.Add(dt1);
		var dt2 = Pose(4, 0, 0, 0, 0, 2, 1); Magnifier(dt2, 0, 13); int fDetective2 = p.Add(dt2);
		var ts1 = Pose(1, 0, 0, 0, 0, -1); Telescope(ts1); int fTelescope1 = p.Add(ts1);
		var ts2 = Pose(1, 0, 0, 0, 0, -1); Telescope(ts2); ts2.Dot(0, 1, Spark).Dot(2, 0, Spark); int fTelescope2 = p.Add(ts2);
		var tu1 = Pose(2, 0, 1); Check(tu1); int fThumbs1 = p.Add(tu1);
		var tu2 = Pose(2, 1, 3); Check(tu2); int fThumbs2 = p.Add(tu2);
		var cp1 = Pose(3, 0, 1); St(cp1, 0, 11, new[] { "..M", ".MM", "MMm" }); cp1.Dot(0, 9, Spark); int fPopper = p.Add(cp1);
		var cp2 = Pose(2, 1, 1, 0, 0, -1, -1); Confetti(cp2, 0); int fPopped = p.Add(cp2);
		var fl1 = Pose(2, 1, 0, 4); Flag(fl1, 0); int fFlag1 = p.Add(fl1);
		var fl2 = Pose(2, 0, 0, 4, 0, 1); Flag(fl2, 1); int fFlag2 = p.Add(fl2);
		var er1 = Pose(3, 2, 1, 0, 0, -1); Warning(er1, 1, -6); int fError1 = p.Add(er1);
		var er2 = Pose(3, 1, 0, 0, 1, 0); Warning(er2, 1, -5); int fError2 = p.Add(er2);
		var wn = Empty(); Warning(wn, 1, -6); int fWarnChild = p.Add(wn);
		var cy1 = Pose(7, 1, 0, 0, 0, 1, 1); Tears(cy1, 0, 1); int fCry1 = p.Add(cy1);
		var cy2 = Pose(1, 2, 0, 0, 0, 2, 1); Tears(cy2, 1, 1); int fCry2 = p.Add(cy2);
		var an1 = Pose(6, 2, 3, 0, -1); an1.Dot(4, 9, Heart); int fAngry1 = p.Add(an1);
		var an2 = Pose(6, 1, 0, 0, 1, 1); an2.Shift(1, 0); int fAngry2 = p.Add(an2);
		var stc = Empty(); Steam(stc, 14, 2); int fSteam = p.Add(stc);
		var sk1 = Pose(3, 2, 1, 3, 0, -2, -2); Crest(sk1); int fShocked1 = p.Add(sk1);
		var sk2 = Pose(3, 2, 3, 0, 0, -1); Crest(sk2); int fShocked2 = p.Add(sk2);
		var sc1 = Pose(3, 1, 1); sc1.Shift(-1, 0); Sweat(sc1, 20, 4); int fScared1 = p.Add(sc1);
		var sc2 = Pose(3, 0, 3); sc2.Shift(1, 0); Sweat(sc2, 21, 6); int fScared2 = p.Add(sc2);
		// talk: stands still, beak opening and closing (used while the speech bubble is open)
		int fTalk1 = p.Add(Pose(0, 1)), fTalk2 = p.Add(Pose(0, 0)), fTalk3 = p.Add(Pose(2, 1));

		// --- weather
		var um1 = Pose(4, 0, 0, 0, 0, 0); UmbrellaOn(um1, 0); int fUmb1 = p.Add(um1);
		var um2 = Pose(0, 0, 0, 0, 0, 0); UmbrellaOn(um2, 1); int fUmb2 = p.Add(um2);
		var pd0 = Pose(2); Puddle(pd0, 0, 10); int fPuddle0 = p.Add(pd0);
		var pdj = Pose(2, 1, 1, 3, 0, 0, -4); Puddle(pdj, 0, 10); int fPuddleJump = p.Add(pdj);
		var pds = Low(2, 2, 1, 2); Puddle(pds, 0, 12); foreach (var (x, y) in new[] { (0, 24), (3, 22), (8, 23), (11, 26) }) Drop(pds, x, y); int fPuddleSplash = p.Add(pds);
		var splashL = Empty(); foreach (var (x, y) in new[] { (2, 22), (4, 25), (0, 27) }) Drop(splashL, x, y); int fSplashChild = p.Add(splashL);
		var ht1 = Pose(5, 2); Sweat(ht1, 20, 4); Sun(ht1, 0, -6); int fHot1 = p.Add(ht1);
		var ht2 = Pose(5, 1); Sweat(ht2, 21, 7); Sun(ht2, 0, -6); int fHot2 = p.Add(ht2);
		var hf1 = Pose(2, 0, 0, 4); HandFan(hf1, 3, 15, 0); int fFan1 = p.Add(hf1);
		var hf2 = Pose(1, 0, 0, 4); HandFan(hf2, 4, 16, 1); int fFan2 = p.Add(hf2);
		var sb1 = Low(2, 2); Shades(sb1, 0, 2); Sun(sb1, 0, -6); int fSunbathe1 = p.Add(sb1);
		var sb2 = Low(3, 2, 0, 0); Shades(sb2, 0, 3); Sun(sb2, 1, -5); int fSunbathe2 = p.Add(sb2);
		var ic1 = Pose(2, 0, 0, 4); IceCream(ic1, 4, 14, true); int fIce1 = p.Add(ic1);
		var ic2 = Pose(1, 1, 0, 4, 0, 1); IceCream(ic2, 4, 14, true); int fIce2 = p.Add(ic2);
		var ic3 = Pose(3, 2, 0, 4); IceCream(ic3, 4, 14, false); int fIceGone = p.Add(ic3);
		var scoop = Empty(); St(scoop, 4, 14, new[] { ".hhh.", "hHhhh", ".hhh." }); int fScoop = p.Add(scoop);
		var splatI = Empty(); St(splatI, 3, 30, new[] { ".hHh..", "hhhhhh" }); int fScoopSplat = p.Add(splatI);
		var cd1 = Pose(1, 0, 3, 0, 0, 1, 1); cd1.Shift(-1, 0); Snowflakes(cd1, 0); cd1.Dot(1, 15, BlueLight); int fCold1 = p.Add(cd1);
		var cd2 = Pose(1, 0, 0, 0, 0, 1, 1); cd2.Shift(1, 0); Snowflakes(cd2, 1); cd2.Dot(2, 15, BlueLight); int fCold2 = p.Add(cd2);
		var sf1 = Pose(2); Scarf(sf1); Snowflakes(sf1, 0); int fScarf1 = p.Add(sf1);
		var sf2 = Pose(1, 0, 0, 0, 0, 1, 1); Scarf(sf2); Snowflakes(sf2, 1); int fScarf2 = p.Add(sf2);
		var bn1 = Pose(2); HatOn(bn1, beanie); int fBeanie1 = p.Add(bn1);
		var bn2 = Pose(4, 0, 0, 0, 0, -1); HatOn(bn2, beanie, 0, -1); int fBeanie2 = p.Add(bn2);
		var snowC = Empty(); Snowflakes(snowC, 0); int fSnowChild1 = p.Add(snowC);
		var snowC2 = Empty(); Snowflakes(snowC2, 1); int fSnowChild2 = p.Add(snowC2);
		var su = Pose(3, 1, 1, 0, 0, -1, -2); St(su, 2, -4, new[] { "q", "q", "q", ".", "q" }); int fSurprised = p.Add(su);

		// --- breaks, reminders and the virtual pet
		int fTired1 = p.Add(Pose(5, 0, 0, 0, 1, 2, 1));
		var tr2 = Pose(1, 1, 0, 0, 1, 3, 2); Sweat(tr2, 20, 6); int fTired2 = p.Add(tr2);
		var sl1 = Pose(5, 0, 0, 0, 1, 2, 1); int fSleepy1 = p.Add(sl1);
		var sl2 = Pose(1, 0, 0, 0, 2, 4, 2); Zed(sl2, 22, 4); int fSleepy2 = p.Add(sl2);
		var dk1 = Pose(0, 0, 0, 4); Glass(dk1, 4, 16, false); int fDrink1 = p.Add(dk1);
		var dk2 = Pose(1, 1, 0, 4, 0, -1); Glass(dk2, 2, 14, true); int fDrink2 = p.Add(dk2);
		var dk3 = Pose(2, 0, 0, 0); Drop(dk3, 4, 15); int fDrinkAh = p.Add(dk3);
		var et1 = Pose(0, 0, 0, 0, 0, 1); SeedBowl(et1, 0, 28); int fEat1 = p.Add(et1);
		var et2 = Pose(1, 1, 0, 0, -1, 7, 2); SeedBowl(et2, 0, 28); int fEat2 = p.Add(et2);
		var fc1 = Pose(0, 0, 0, 4); FortuneCookie(fc1, 3, 17, false); int fCookie1 = p.Add(fc1);
		var fc2 = Pose(3, 1, 0, 4, 0, 1); FortuneCookie(fc2, 3, 17, true); int fCookie2 = p.Add(fc2);
		var slipC = Empty(); Slip(slipC, 4, 15); int fSlip = p.Add(slipC);
		var sw1 = Pose(5, 1, 3); Sweat(sw1, 21, 4); Sweat(sw1, 3, 2); int fSweat1 = p.Add(sw1);
		var sw2 = Pose(5, 0, 0); Sweat(sw2, 22, 6); int fSweat2 = p.Add(sw2);
		Frame Dizzy(int k)
		{
			var f = Pose(1, 1, 0, 0, k == 0 ? -1 : 1, 0);
			var stars = new[] { (4, 3), (10, 1), (16, 3), (10, 5) };
			for (int i = 0; i < 4; i++) { var (x, y) = stars[(i + k) % 4]; f.Dot(x, y, Spark).Dot(x + 1, y, Spark).Dot(x, y + 1, Spark); }
			return f;
		}
		int fDizzyA = p.Add(Dizzy(0)), fDizzyB = p.Add(Dizzy(1)), fDizzyC = p.Add(Dizzy(2));
		int fSpinBack = p.Add(Pose(0, 0, 0, 0, 0, 0, 0, true));
		var spinM = Pose(2); spinM.MirrorX(); int fSpinMirror = p.Add(spinM);
		int fNodDown = p.Add(Pose(2, 0, 0, 0, 0, 2));
		var ok1 = Pose(2, 0, 3); OkSign(ok1); int fOk = p.Add(ok1);
		var ch1 = Pose(2, 1, 1, 3, 0, -1, -4); ch1.Dot(1, 3, Spark).Dot(28, 2, Spark).Dot(6, -4, Spark); int fCheer1 = p.Add(ch1);
		var ch2 = Pose(2, 2, 2, 0); int fCheer2 = p.Add(ch2);
		var pt1 = Pose(2, 0, 0); HatOn(pt1, partyHat); PartyHorn(pt1, 0); int fParty1 = p.Add(pt1);
		var pt2 = Pose(3, 1, 1, 0, -1); HatOn(pt2, partyHat, -1); PartyHorn(pt2, 1); int fParty2 = p.Add(pt2);
		int fJumpUp = p.Add(Pose(2, 1, 1, 3, 0, 0, -6));
		var hp1 = Pose(2, 0, 0, 0, 0, 0, -2); hp1.Dot(2, 4, Spark).Dot(26, 6, Spark); int fHappy1 = p.Add(hp1);
		var hg1 = Pose(1, 0, 2, 0, 0, 1, 1); HeartAt(hg1, 4, 17); int fHug1 = p.Add(hg1);
		var hg2 = Pose(2, 0, 2, 0, 0, 1, 1); HeartAt(hg2, 4, 17); HeartAt(hg2, 0, 0); int fHug2 = p.Add(hg2);
		var hc1 = Empty(); HeartAt(hc1, 2, 4); int fHeartChild = p.Add(hc1);
		var swp1 = Pose(0, 0, 0, 4, 0, 2, 1); Broom(swp1, 0, 0); int fSweep1 = p.Add(swp1);
		var swp2 = Pose(0, 0, 0, 4, -1, 2, 1); Broom(swp2, 0, -1); Dust(swp2, 0, 26); int fSweep2 = p.Add(swp2);
		var dustC = Empty(); Dust(dustC, 0, 26); int fDust = p.Add(dustC);
		var wk1 = Pose(0, 0, 0, 0, 0, 3, 1); HatOn(wk1, hardHat, 0, 3); Hammer(wk1, 2, 18, true); Board(wk1); int fWork1 = p.Add(wk1);
		var wk2 = Pose(3, 1, 0, 0, -1, 5, 1); HatOn(wk2, hardHat, -1, 5); Hammer(wk2, 0, 27, false); Board(wk2); wk2.Dot(1, 26, Spark).Dot(7, 25, Spark); int fWork2 = p.Add(wk2);
		var ms1 = Pose(1, 0, 0, 1, -1, 0); Headphones(ms1, -1, 0); int fMusic1 = p.Add(ms1);
		var ms2 = Pose(2, 1, 0, 2, 1, 1, 1); Headphones(ms2, 1, 2); int fMusic2 = p.Add(ms2);
		var cof1 = Pose(0, 0, 0, 4); CoffeeCup(cof1, 3, 17); int fCoffee1 = p.Add(cof1);
		var cof2 = Pose(1, 1, 0, 4, 0, 1); CoffeeCup(cof2, 2, 15); int fCoffee2 = p.Add(cof2);
		var cof3 = Pose(2, 0, 0, 4); CoffeeCup(cof3, 3, 17); cof3.Dot(4, 10, Spark).Dot(26, 6, Spark); int fCoffeeAh = p.Add(cof3);
		var steamC = Empty(); St(steamC, 4, 12, new[] { ".k.k", "k.k.", ".k.k" }); int fCoffeeSteam = p.Add(steamC);

		p.IconFrame = fHappy;

		// ------------------------------------------------------------ animations
		p.Spawns.Add((1, 50, "screenW+10", "random*(areaH/2)/100+20", 21));   // flies in from the side
		p.Spawns.Add((2, 30, "screenW+10", "areaH-imageH", 1));
		p.Spawns.Add((3, 20, "random*(screenW-imageW-50)/100+25", "-imageH-20", 5));

		p.A(1, "walk").Move("-2", "0").Speed("160").Loop("random/4+8")
			.Seq(fWalk1, fWalk2, fWalk3, fWalk2)
			.Then(50, 1).Then(10, 2).Then(12, 11).Then(5, 20).Then(4, 23).Then(4, 24).Then(3, 25).Then(3, 40).Then(3, 54).Then(2, 43)
			.Then(6, 16, "taskbar").Then(4, 13, "taskbar")
			.OnBorder(100, 2).OnBorder(20, 12, "vertical").OnBorder(30, 9, "window")
			.OnGravity(100, 5);

		var turn = p.A(2, "turn").Move("0", "0").Speed("130");
		turn.Seq(fLookBack, fStand);
		turn.Action = "flip";
		turn.Then(100, 1);

		p.A(3, "drag").Move("0", "0").Speed("70").Loop("40").Seq(fDrag1, fDrag1, fDrag2, fDrag2).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("45").Loop("30").Seq(fDrag1, fFall, fDrag2, fFall).Then(100, 4);
		// falling: she flaps, so the fall is slower than the other pets'
		p.A(5, "fall").Move("0", "2", "0", "6").Speed("80", "60").Loop("20").Seq(fFlyUp, fFlyMid, fFlyDown, fFlyMid).Then(100, 6).OnBorder(100, 7);
		p.A(6, "fall fast").Move("0", "7").Speed("50").Loop("10").Seq(fFall, fDrag2).Then(100, 6).OnBorder(100, 8);
		p.A(7, "land").Move("0", "0").Speed("120", "200").Seq(fFlyDown, fLand, fCrouch, fStand, fHappy).Then(100, 1);
		p.A(8, "fall hard").Move("0", "0").Speed("150", "260").Seq(fLand, fDizzy, fDizzy, fDizzy, fDizzy, fBlink, fStand).Then(100, 1);

		p.A(9, "hop up").Move("-3", "-8", "-3", "8").Speed("70")
			.Seq(fCrouch, fTakeoff, fFlyUp, fFlyMid, fFlyDown, fFlyUp, fFlyMid, fFlyDown, fLand, fStand)
			.Then(100, 1)
			.OnBorder(80, 7, "taskbar").OnBorder(40, 7, "window").OnBorder(20, 5, "horizontal+");

		p.A(10, "sync").Move("0", "0").Speed("120").Loop("6").Seq(fBob1, fBob2, fBob3, fBob2).Then(100, 1);

		var hub = p.A(11, "blink").Move("0", "0").Speed("450", "120").Seq(fStand, fBlink, fStand, fBlink, fStand);
		hub.Then(30, 1).Then(8, 15).Then(7, 17).Then(6, 20).Then(7, 23).Then(7, 24).Then(5, 25).Then(5, 26).Then(5, 27).Then(5, 28)
			.Then(4, 29).Then(4, 30).Then(4, 31).Then(4, 32).Then(4, 33).Then(4, 34).Then(3, 35).Then(4, 36).Then(4, 37).Then(3, 38)
			.Then(4, 40).Then(4, 41).Then(4, 42).Then(3, 43).Then(3, 44).Then(3, 45).Then(4, 46).Then(4, 47).Then(4, 48).Then(3, 49)
			.Then(3, 50).Then(3, 51).Then(3, 52).Then(4, 53).Then(4, 54).Then(4, 55).Then(4, 56).Then(4, 57).Then(3, 58).Then(4, 59);

		p.A(12, "climb").Move("0", "-3").Speed("140").Loop("5000", 2)
			.Seq(fClimb1, fClimb2, fClimb1, fClimb2)
			.Then(100, 12)
			.OnBorder(100, 2);

		p.A(13, "sleep").Move("0", "0").Speed("800", "450").Loop("random/4+5", 2)
			.Seq(fSit, fSleep1, fSleep1, fSleep2, fSleep3, fSleep2)
			.Then(100, 14);
		p.A(14, "wake up").Move("0", "0").Speed("220", "160").Seq(fSleep1, fSit, fWide, fStand, fHappy, fStand).Then(100, 1);
		p.A(15, "look around").Move("0", "0").Speed("280").Seq(fStand, fLookUp, fLookUp, fStand, fLookBack, fLookBack, fStand).Then(100, 1);
		p.A(16, "sit").Move("0", "0").Speed("600", "400").Loop("random/3+4", 1).Seq(fSit, fSit, fSit, fSit).Then(60, 1).Then(40, 13);
		p.A(17, "wave").Move("0", "0").Speed("170").Loop("4", 1).Seq(fStand, fWave1, fWave2, fWave1, fWave2).Then(100, 1);

		// --- macaw specials ----------------------------------------------------
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

		// 20-22 flight: takes off, flies across the screen, glides down and lands
		p.A(20, "take off").Move("0", "0", "-2", "-5").Speed("90").Seq(fCrouch, fTakeoff, fFlyUp, fFlyMid, fFlyDown, fFlyUp).Then(100, 21);
		p.A(21, "fly").Move("-6", "-2", "-6", "1").Speed("80").Loop("random/3+10", 0)
			.Seq(fFlyUp, fFlyMid, fFlyDown, fFlyMid)
			.Then(70, 22).Then(30, 21)
			.OnBorder(100, 19, "vertical").OnBorder(100, 7, "taskbar").OnBorder(100, 7, "window");
		p.A(22, "glide").Move("-5", "3").Speed("90").Loop("200", 0)
			.Seq(fGlide, fGlide, fFlyMid, fGlide)
			.Then(100, 22)
			.OnBorder(100, 7, "taskbar").OnBorder(100, 7, "window").OnBorder(100, 19, "vertical");
		var flyTurn = p.A(19, "fly turn").Move("0", "0").Speed("90").Seq(fFlyMid, fFlyUp);
		flyTurn.Action = "flip";
		flyTurn.Then(100, 21);

		Show(23, "squawk", 140, (fStand, 1), (fWide, 2), (fSquawk1, 4), (fSquawk2, 2), (fSquawk1, 4), (fStand, 2));
		Loop2(24, "dance", 130, "random/3+5", fBob1, fBob2, fBob3, fBob2);
		Show(25, "say hi", 220, (fStand, 1), (fHi1, 2), (fHi2, 1), (fHi1, 2), (fHi2, 4), (fHappy, 2));
		Loop2(26, "preen", 260, "random/3+3", fPreen1, fPreen2, fPreen1, fLookBack);
		Show(27, "brazil nut", 200, (fNutHold, 4), (fNutBite, 2), (fNutHold, 2), (fNutBite, 3), (fNutCrack, 3), (fNutChew, 2), (fNutCrack, 1), (fNutChew, 2), (fHappy, 2));
		Show(28, "mango", 220, (fMango0, 3), (fMangoBite, 2), (fMango1, 3), (fMangoBite, 2), (fMango2, 3), (fYum, 3), (fHappy, 2));
		Show(29, "shake", 70, (fStand, 2), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fBlink, 3), (fHappy, 3));
		Loop2(30, "love", 260, "random/3+3", fLove1, fLove2);
		Loop2(31, "whistle", 240, "random/3+3", fSing1, fSing2);
		Show(32, "yawn", 220, (fStand, 1), (fYawn, 5), (fBlink, 2), (fStand, 1));
		Show(33, "stretch", 220, (fStand, 1), (fStretch, 5), (fWave1, 2), (fStand, 1));
		Loop2(34, "bath", 160, "random/3+5", fBath1, fBath2);
		Loop2(35, "upside down", 300, "random/3+3", fUpside1, fUpside2);
		var hop = p.A(36, "hop hop").Move("-3", "0").Speed("120").Loop("random/3+4", 0);
		hop.Seq(fCrouch, fFlyMid, fFlyMid, fCrouch); hop.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		Show(37, "startled", 90, (fStand, 3), (fWide, 1), (fFlyUp, 2), (fFlyDown, 2), (fFlyUp, 2), (fLand, 2), (fBlink, 2), (fStand, 1));
		// 38 loses a feather while shaking: it floats down in its own window
		Show(38, "lose a feather", 80, (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fWide, 4), (fLookUp, 6), (fHappy, 2));
		p.A(101, "feather").Move("-1", "2", "1", "2").Speed("110").Loop("40", 0).Fade(1, 0.2)
			.Seq(fFeather1, fFeather1, fFeather2, fFeather2)
			.OnBorder(100, 102, "taskbar").OnBorder(100, 102, "window");
		p.A(102, "feather rests").Move("0", "0").Speed("300").Fade(1, 0).Seq(fFeather1, fFeather1, fFeather1, fFeather1);
		p.Childs.Add((38, "imageX+imageW*0.3", "imageY+imageH*0.2", 101));

		// ================================================================ 20 MORE, most with a child window
		// A scene starts in Pitanga's own frames; the "release" animation opens the child window (the object),
		// which then moves on its own. A child animation with no next closes its window when it ends.
		Anim Hold(int id, string name, int ms, int frame, int n, int next)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString());
			a.Seq(Enumerable.Repeat(frame, n).ToArray()); a.Then(100, next);
			return a;
		}
		// 40 soccer ball
		Hold(40, "soccer ball", 200, fBallHold, 4, 60);
		Show(60, "kick ball", 140, (fKick, 3), (fLookUp, 1), (fStand, 4), (fHappy, 2), (fWave1, 2), (fWave2, 2));
		p.A(103, "ball bounce up").Move("-6", "-7", "-6", "6").Speed("60").Seq(fBall1, fBall2, fBall1, fBall2, fBall1, fBall2, fBall1, fBall2)
			.Then(100, 104).OnBorder(100, 104, "taskbar").OnBorder(100, 104, "window");
		p.A(104, "ball bounce").Move("-5", "-4", "-5", "4").Speed("60").Seq(fBall1, fBall2, fBall1, fBall2, fBall1, fBall2)
			.Then(100, 105);
		p.A(105, "ball roll").Move("-4", "0").Speed("70").Loop("60", 0).Seq(fBall1, fBall2);
		p.A(106, "ball drop").Move("-3", "8").Speed("60").Loop("100", 0).Seq(fBall1, fBall2).OnBorder(100, 105, "taskbar").OnBorder(100, 105, "window");
		p.Childs.Add((60, "imageX", "imageY", 103));
		// 41 mango seed
		Hold(41, "mango seed", 220, fSeedMouth, 4, 61);
		Show(61, "spit seed", 140, (fSpit, 3), (fLookUp, 2), (fStand, 3), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		p.A(107, "seed arc").Move("-7", "-5", "-6", "6").Speed("60").Seq(Enumerable.Repeat(fSeedFly, 12).ToArray())
			.Then(100, 108).OnBorder(100, 109, "taskbar").OnBorder(100, 109, "window");
		p.A(108, "seed fall").Move("-4", "7").Speed("60").Loop("200", 0).Seq(fSeedFly).OnBorder(100, 109, "taskbar").OnBorder(100, 109, "window");
		p.A(109, "seed rests").Move("0", "0").Speed("300").Fade(1, 0).Seq(fSeedRest, fSeedRest, fSeedRest, fSeedRest, fSeedRest, fSeedRest);
		p.Childs.Add((61, "imageX", "imageY", 107));
		// 42 shout "OI!" and the bubble floats up
		Show(42, "shout hi", 180, (fSquawk2, 2), (fSquawk1, 3), (fHappy, 6), (fLookUp, 4), (fHappy, 2));
		p.A(110, "hi bubble").Move("-1", "-3").Speed("90").Loop("12", 0).Fade(1, 0).Seq(fEcho, fEcho);
		p.Childs.Add((42, "imageX", "imageY", 110));
		// 43 the blue friend flies past
		Show(43, "friend flies by", 160, (fLookBack, 6), (fWide, 2), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fLookUp, 4), (fHappy, 3));
		p.A(111, "friend flight").Move("-7", "-1").Speed("80").Loop("80", 0).Seq(fFriend1, fFriend2);
		p.Childs.Add((43, "imageX+imageW*1.2", "imageY-imageH*0.5", 111));
		// 44 kite
		Hold(44, "kite", 240, fKiteHold, 8, 62);
		Show(62, "let the kite go", 200, (fLookUp, 4), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fHappy, 3));
		p.A(112, "kite away").Move("-2", "-3", "-1", "-4").Speed("110").Loop("90", 0).Seq(fKite1, fKite1, fKite2, fKite2);
		p.Childs.Add((62, "imageX", "imageY", 112));
		// 45 coconut on the head
		Show(45, "coconut", 120, (fStand, 3), (fLookUp, 5), (fWide, 2), (fBonk, 4), (fDizzy, 8), (fBlink, 2), (fStand, 1));
		p.A(113, "coconut falls").Move("0", "9").Speed("60").Loop("200", 0).Seq(fCoconut).OnBorder(100, 114, "taskbar").OnBorder(100, 114, "window");
		p.A(114, "coconut rests").Move("0", "0").Speed("300").Fade(1, 0).Seq(fCoconutRest, fCoconutRest, fCoconutRest, fCoconutRest, fCoconutRest);
		p.Childs.Add((45, "imageX", "imageY-imageH*2.4", 113));
		// 46 butterfly on the beak
		var bfa = p.A(46, "butterfly").Move("0", "0").Speed("220").Seq(fButterflyBeak, fButterflyBeak2, fButterflyBeak, fButterflyBeak2, fButterflyBeak, fButterflyBeak2);
		bfa.Then(100, 63);
		Show(63, "butterfly flies off", 200, (fWide, 2), (fLookUp, 6), (fLove1, 2), (fLove2, 2), (fHappy, 2));
		p.A(115, "butterfly away").Move("-2", "-3", "1", "-3").Speed("110").Loop("60", 0).Seq(fButterfly1, fButterfly2);
		p.Childs.Add((63, "imageX", "imageY", 115));
		// 47 samba song: two notes float away
		Loop2(47, "sing samba", 220, "random/3+4", fSing1, fSing2, fSamba1, fSamba3);
		p.A(116, "note up").Move("-1", "-2").Speed("110").Loop("20", 0).Fade(1, 0).Seq(fNote, fNote);
		p.A(117, "note up 2").Move("1", "-2").Speed("130").Loop("18", 0).Fade(1, 0).Seq(fNote, fNote);
		p.Childs.Add((47, "imageX", "imageY", 116));
		p.Childs.Add((47, "imageX+imageW*0.3", "imageY-10", 117));
		// 48 blown kiss
		Show(48, "blow kiss", 180, (fStand, 1), (fKiss, 4), (fLove1, 3), (fHappy, 2));
		p.A(118, "kiss heart").Move("-3", "-2", "-2", "-3").Speed("80").Loop("10", 0).Fade(1, 0).Seq(fKissHeart, fKissHeart);
		p.Childs.Add((48, "imageX", "imageY", 118));
		// 49 rain cloud
		Show(49, "rain cloud", 160, (fLookUp, 6), (fWide, 2), (fWet, 10), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fShake1, 1), (fShake2, 1), (fBlink, 2), (fHappy, 2));
		p.A(119, "cloud rains").Move("0", "0").Speed("150").Loop("12", 0).Seq(fCloud1, fCloud2).Then(100, 120);
		p.A(120, "cloud leaves").Move("-3", "-1").Speed("100").Loop("12", 0).Fade(1, 0).Seq(fCloud1, fCloud2);
		p.Childs.Add((49, "imageX", "imageY", 119));
		// 50 plants a seed: a flower grows beside her and wilts later
		Show(50, "plant a flower", 200, (fDig, 3), (fStand, 1), (fDig, 3), (fStand, 2), (fWide, 2), (fLove1, 3), (fHappy, 2));
		p.A(121, "flower grows").Move("0", "0").Speed("500").Seq(sp[0], sp[0], sp[1], sp[1], sp[2], sp[2], sp[3]).Then(100, 122);
		p.A(122, "flower").Move("0", "0").Speed("500").Loop("30", 0).Seq(sp[3]).Then(100, 123);
		p.A(123, "flower wilts").Move("0", "0").Speed("400").Fade(1, 0).Seq(sp[4], sp[4], sp[4], sp[4]);
		p.Childs.Add((50, "imageX-imageW*0.45", "imageY", 121));
		// 51 egg: the chick walks off
		p.A(51, "egg").Move("0", "0").Speed("220").Seq(fEgg0, fEgg0, fEgg1, fEgg0, fEgg1, fEgg1, fEgg2, fEgg2).Then(100, 64);
		Show(64, "chick hatches", 180, (fWide, 2), (fHappy, 2), (fLookUp, 2), (fWave1, 2), (fWave2, 2), (fLove1, 2), (fHappy, 2));
		p.A(124, "chick walk").Move("-2", "0").Speed("140").Loop("120", 0).Seq(fChick1, fChick2);
		p.A(125, "chick flutter down").Move("-1", "5").Speed("80").Loop("100", 0).Seq(fChick2, fChick1).OnBorder(100, 124, "taskbar").OnBorder(100, 124, "window");
		p.Childs.Add((64, "imageX", "imageY", 124));
		// 52 carnival: confetti
		Loop2(52, "carnival", 120, "random/3+6", fSamba1, fSamba2, fSamba3, fSamba2);
		p.A(126, "confetti").Move("0", "2").Speed("120").Loop("20", 0).Fade(1, 0).Seq(fConfetti1, fConfetti2);
		p.A(127, "confetti 2").Move("1", "2").Speed("140").Loop("18", 0).Fade(1, 0).Seq(fConfetti2, fConfetti1);
		p.Childs.Add((52, "imageX-imageW*0.4", "imageY-imageH*0.6", 126));
		p.Childs.Add((52, "imageX+imageW*0.4", "imageY-imageH*0.7", 127));
		// 53 throws a Brazil nut
		Hold(53, "nut in the beak", 220, fNutMouth, 4, 65);
		Show(65, "throw nut", 140, (fSquawk1, 2), (fLookUp, 2), (fStand, 4), (fHappy, 2));
		p.A(128, "nut arc").Move("-7", "-6", "-6", "6").Speed("60").Seq(Enumerable.Repeat(fNutFly, 12).ToArray())
			.Then(100, 129).OnBorder(100, 130, "taskbar").OnBorder(100, 130, "window");
		p.A(129, "nut fall").Move("-4", "7").Speed("60").Loop("200", 0).Seq(fNutFly).OnBorder(100, 130, "taskbar").OnBorder(100, 130, "window");
		p.A(130, "nut rests").Move("0", "0").Speed("300").Fade(1, 0).Seq(fNutRest, fNutRest, fNutRest, fNutRest, fNutRest);
		p.Childs.Add((65, "imageX", "imageY", 128));
		// 54-59 without a child
		Loop2(54, "samba", 110, "random/3+6", fSamba1, fSamba2, fSamba3, fSamba2);
		Show(55, "sneeze", 140, (fStand, 1), (fAh, 3), (fStand, 1), (fAh, 4), (fChoo, 3), (fBlink, 2), (fStand, 1));
		Loop2(56, "laugh", 130, "random/3+4", fLaugh1, fLaugh2);
		Loop2(57, "pandeiro", 130, "random/3+6", fPand1, fPand2);
		Show(58, "hiccup", 120, (fStand, 3), (fHic, 2), (fStand, 4), (fHic, 2), (fStand, 3), (fHic, 2), (fWide, 2), (fStand, 1));
		Loop2(59, "curious", 320, "random/3+3", fCurious1, fCurious2);

		// ================================================================ 50 SCENES FOR THE PROGRAM'S FEATURES (140-189)
		// releases 190-199, child windows 200-219. The names are the ones the program asks for (see ReactAll in src).
		Anim Child(int id, string name, string mx, string my, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move(mx, my).Speed(ms.ToString()).Loop(loops, 0); a.Seq(frames); return a;
		}
		// Claude Code / AI: thinking, reading, editing, running, searching, done, error
		Loop2(140, "think", 320, "random/3+3", fThink1, fThink1, fThink2, fThink2);
		Loop2(141, "review", 300, "random/3+3", fReview1, fReview1, fReview2);
		Show(142, "idea", 160, (fThink1, 4), (fIdea1, 3), (fIdea2, 1), (fIdea1, 1), (fIdea2, 6), (fHappy, 3));
		Child(200, "idea bulb", "0", "-2", 90, "14", fBulbChild).Fade(1, 0);
		p.Childs.Add((142, "imageX", "imageY", 200));
		Loop2(143, "read", 420, "random/3+3", fRead1, fRead1, fRead2, fRead1);
		Loop2(144, "laptop", 140, "random/3+6", fLaptop1, fLaptop2);
		Child(201, "code floats", "-1", "-2", 110, "16", fCode, fCode).Fade(1, 0);
		p.Childs.Add((144, "imageX", "imageY", 201));
		Loop2(145, "write", 260, "random/3+4", fWrite1, fWrite2);
		Loop2(146, "loading", 140, "random/3+5", fLoad1, fLoad2, fLoad3, fLoad4);
		Show(147, "download", 150, (fDlLook, 7), (fDlCatch, 6), (fHappy, 3));
		Child(202, "download box", "0", "8", 60, "17", fDlChild);
		p.Childs.Add((147, "imageX", "imageY-imageH*2", 202));
		Loop2(148, "detective", 300, "random/3+3", fDetective1, fDetective2, fDetective1, fLookBack);
		Loop2(149, "telescope", 320, "random/3+3", fTelescope1, fTelescope2);
		Show(150, "thumbs up", 200, (fStand, 1), (fThumbs1, 4), (fThumbs2, 2), (fThumbs1, 3), (fHappy, 2));
		Hold(151, "confetti", 180, fPopper, 4, 193);
		Show(193, "confetti pop", 140, (fPopped, 6), (fCheer2, 2), (fHappy, 3));
		p.Childs.Add((193, "imageX-imageW*0.4", "imageY-imageH*0.6", 126));
		p.Childs.Add((193, "imageX+imageW*0.4", "imageY-imageH*0.7", 127));
		Loop2(152, "flag", 200, "random/3+5", fFlag1, fFlag2);
		Show(153, "error", 130, (fWide, 2), (fError1, 6), (fError2, 4), (fError1, 4), (fBlink, 2), (fStand, 1));
		Child(203, "warning up", "0", "-2", 100, "12", fWarnChild).Fade(1, 0);
		p.Childs.Add((153, "imageX", "imageY", 203));
		Loop2(154, "cry", 220, "random/3+4", fCry1, fCry2);
		Loop2(155, "angry", 110, "random/3+6", fAngry1, fAngry2);
		Child(204, "angry steam", "1", "-2", 100, "12", fSteam).Fade(1, 0);
		p.Childs.Add((155, "imageX", "imageY", 204));
		Show(156, "shocked", 110, (fStand, 2), (fShocked1, 4), (fShocked2, 6), (fBlink, 2), (fStand, 1));
		Loop2(157, "scared", 70, "random/3+10", fScared1, fScared2);
		// talk: never moves (the program plays it while the speech bubble is open)
		var talk = p.A(158, "talk").Move("0", "0").Speed("160").Loop("random/3+8", 0);
		talk.Seq(fTalk1, fTalk2, fTalk1, fTalk3, fTalk2, fTalk2); talk.Then(100, 1);
		// weather
		Loop2(159, "umbrella", 220, "random/3+5", fUmb1, fUmb2);
		Show(160, "puddle splash", 140, (fPuddle0, 4), (fCrouch, 2), (fPuddleJump, 4));
		p.Anims.Last().Next.Clear(); p.Anims.Last().Then(100, 192);
		Show(192, "splash", 140, (fPuddleSplash, 6), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		Child(205, "splash left", "-3", "-1", 80, "8", fSplashChild).Fade(1, 0);
		Child(206, "splash right", "3", "-1", 80, "8", fSplashChild).Fade(1, 0);
		p.Childs.Add((192, "imageX", "imageY", 205));
		p.Childs.Add((192, "imageX+imageW*0.3", "imageY", 206));
		Loop2(161, "hot", 240, "random/3+4", fHot1, fHot2);
		Loop2(162, "hand fan", 120, "random/3+8", fFan1, fFan2);
		Loop2(163, "sunbathing", 500, "random/3+3", fSunbathe1, fSunbathe1, fSunbathe2);
		Show(164, "ice cream", 220, (fIce1, 3), (fIce2, 2), (fIce1, 2), (fIce2, 2));
		p.Anims.Last().Next.Clear(); p.Anims.Last().Then(100, 190);
		Show(190, "scoop falls", 150, (fIceGone, 6), (fCry1, 3), (fCry2, 3), (fCry1, 2), (fStand, 1));
		Child(207, "scoop", "0", "7", 60, "200", fScoop).OnBorder(100, 208, "taskbar").OnBorder(100, 208, "window");
		Child(208, "scoop splat", "0", "0", 300, "6", fScoopSplat).Fade(1, 0);
		p.Childs.Add((190, "imageX", "imageY", 207));
		Loop2(165, "cold", 70, "random/3+10", fCold1, fCold2);
		Loop2(166, "scarf", 360, "random/3+3", fScarf1, fScarf2);
		Loop2(167, "snow cap", 320, "random/3+4", fBeanie1, fBeanie2);
		Child(209, "snowfall", "0", "1", 200, "16", fSnowChild1, fSnowChild2).Fade(1, 0);
		Child(210, "snowfall 2", "1", "1", 240, "14", fSnowChild2, fSnowChild1).Fade(1, 0);
		p.Childs.Add((167, "imageX-imageW*0.5", "imageY-imageH*0.4", 209));
		p.Childs.Add((167, "imageX+imageW*0.5", "imageY-imageH*0.5", 210));
		Show(168, "look up", 280, (fStand, 1), (fLookUp, 6), (fBlink, 1), (fLookUp, 4), (fStand, 1));
		Show(169, "surprised", 120, (fStand, 2), (fSurprised, 6), (fWide, 3), (fStand, 1));
		// breaks, reminders, mood, fun
		Loop2(170, "tired", 400, "random/3+3", fTired1, fTired2);
		Show(171, "sleepy", 300, (fTired1, 3), (fSleepy1, 3), (fSleepy2, 4), (fWide, 1), (fTired1, 3), (fSleepy2, 5), (fBlink, 2), (fStand, 1));
		Show(172, "drink", 220, (fDrink1, 3), (fDrink2, 3), (fDrink1, 1), (fDrink2, 3), (fDrinkAh, 3), (fHappy, 2));
		Loop2(173, "eat", 180, "random/3+5", fEat1, fEat2);
		Show(174, "cookie", 200, (fCookie1, 4), (fCookie2, 3));
		p.Anims.Last().Next.Clear(); p.Anims.Last().Then(100, 191);
		Show(191, "fortune", 200, (fWide, 3), (fLookUp, 5), (fThink1, 3), (fHappy, 3));
		Child(211, "fortune slip", "-1", "-2", 110, "14", fSlip).Fade(1, 0);
		p.Childs.Add((191, "imageX", "imageY", 211));
		Loop2(175, "sweat", 160, "random/3+5", fSweat1, fSweat2);
		Loop2(176, "dizzy", 130, "random/3+5", fDizzyA, fDizzyB, fDizzyC);
		Loop2(177, "spin", 90, "random/3+3", fStand, fSpinBack, fSpinMirror, fLookBack);
		Show(178, "nod", 180, (fStand, 1), (fNodDown, 2), (fStand, 2), (fNodDown, 2), (fHappy, 2));
		Show(179, "ok", 200, (fStand, 1), (fOk, 8), (fHappy, 2));
		Loop2(180, "cheer", 120, "random/3+4", fCrouch, fCheer1, fCheer1, fCheer2);
		Loop2(181, "party", 160, "random/3+5", fParty1, fParty2);
		Show(182, "jump", 90, (fCrouch, 2), (fJumpUp, 4), (fFlyMid, 1), (fLand, 2), (fHappy, 3));
		Loop2(183, "happy", 140, "random/3+4", fHappy, fHappy1);
		Loop2(184, "hug", 320, "random/3+3", fHug1, fHug2);
		Loop2(185, "hearts", 260, "random/3+3", fLove1, fLove2);
		Child(212, "heart up", "-1", "-2", 100, "14", fHeartChild).Fade(1, 0);
		Child(213, "heart up 2", "1", "-2", 120, "12", fHeartChild).Fade(1, 0);
		p.Childs.Add((185, "imageX", "imageY", 212));
		p.Childs.Add((185, "imageX+imageW*0.4", "imageY", 213));
		Loop2(186, "sweep", 160, "random/3+6", fSweep1, fSweep2);
		Child(214, "dust", "-2", "-1", 120, "12", fDust).Fade(1, 0);
		p.Childs.Add((186, "imageX", "imageY", 214));
		Loop2(187, "work", 150, "random/3+6", fWork1, fWork2);
		Loop2(188, "music", 200, "random/3+6", fMusic1, fMusic2);
		p.Childs.Add((188, "imageX", "imageY", 116));
		p.Childs.Add((188, "imageX+imageW*0.3", "imageY-10", 117));
		Show(189, "coffee", 240, (fCoffee1, 3), (fCoffee2, 3), (fCoffee1, 2), (fCoffee2, 3), (fCoffeeAh, 4), (fHappy, 2));
		Child(215, "coffee steam", "0", "-1", 140, "12", fCoffeeSteam).Fade(1, 0);
		p.Childs.Add((189, "imageX", "imageY", 215));
		// some of them also happen on their own, now and then
		foreach (int id in new[] { 140, 142, 143, 144, 145, 148, 149, 151, 152, 159, 162, 166, 167, 172, 173, 174, 177, 181, 184, 185, 186, 187, 188, 189 })
			hub.Then(2, id);

		// 99 kill: flies away up and out of sight
		p.A(99, "kill").Move("-4", "-6").Speed("80").Fade(1, 0)
			.Seq(fHappy, fWave1, fTakeoff, fFlyUp, fFlyMid, fFlyDown, fFlyUp, fFlyMid, fFlyDown, fFlyUp)
			.Then(100, 99);

		p.Readme = """
			# Pitanga the Macaw

			## Description

			A scarlet macaw from Brazil in pixel art: red feathers, yellow and blue wings,
			a white face and a big hooked beak.

			She waddles along your windows, takes off and flies across the screen, glides
			down to land, squawks, says hi, dances to the beat, preens her feathers, cracks
			Brazil nuts, eats mango, whistles, takes a bath, hangs upside down and naps.

			## About pet

			Author: Luan Felipe

			## Animations

			* waddle, turn, hop onto windows, fall (flapping) and land
			* take off, fly across the screen, turn in the air, glide down
			* squawk, say hi, dance, wave, whistle, love, shake, lose a feather
			* preen, crack a Brazil nut, eat a mango, bath, stretch, yawn, upside down
			* soccer ball, kite, coconut, butterfly, rain cloud, a flower, an egg, carnival confetti, a blue friend flying by
			* reacts to the app: thinks, reads, types on a laptop and celebrates with Claude Code; drinks water on the
			  water reminder; umbrella, scarf and fan with the weather; party at the end of a Pomodoro; fortune cookie
			* climb the screen border, sit and sleep on the taskbar
			* drag and toss with the mouse
			""";
		p.ReadmePt = """
			# Pitanga, a Arara

			## Descrição

			Uma arara-vermelha brasileira em pixel art: penas vermelhas, asas amarelas e
			azuis, o rosto branco e um bico grande e curvo.

			Ela anda gingando pelas janelas, levanta voo e atravessa a tela, plana até
			pousar, grita, dá "oi", dança no ritmo, arruma as penas, quebra castanha-do-pará,
			come manga, assobia, toma banho, fica de cabeça para baixo e tira um cochilo.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* andar gingando, virar, subir nas janelas, cair (batendo as asas) e pousar
			* levantar voo, atravessar a tela voando, virar no ar, planar até o chão
			* gritar, dar oi, dançar, dar tchau, assobiar, apaixonar-se, chacoalhar, perder uma pena
			* arrumar as penas, quebrar castanha-do-pará, comer manga, banho, espreguiçar, bocejar, de cabeça para baixo
			* bola de futebol, pipa, coco, borboleta, nuvem de chuva, flor, ovo, confete de carnaval, amiga arara-azul passando
			* reage ao programa: pensa, lê, digita no notebook e comemora com o Claude Code; bebe água no lembrete
			  de água; guarda-chuva, cachecol e leque conforme o clima; festa no fim do Pomodoro; biscoito da sorte
			* escalar as bordas da tela, sentar e dormir na barra de tarefas
			* arrastar e jogar com o mouse
			""";
		if (azul)
		{
			p.Readme = p.Readme
				.Replace("# Pitanga the Macaw", "# Açaí the Hyacinth Macaw")
				.Replace("A scarlet macaw from Brazil in pixel art: red feathers, yellow and blue wings,\na white face and a big hooked beak.",
					"A hyacinth macaw (arara-azul-grande) from Brazil in pixel art: cobalt blue feathers,\na yellow ring around the eye, yellow at the base of the beak and a big dark hooked beak.")
				.Replace("She waddles", "He waddles").Replace("preens her feathers", "preens his feathers")
				.Replace("a blue friend flying by", "a scarlet friend flying by");
			p.ReadmePt = p.ReadmePt
				.Replace("# Pitanga, a Arara", "# Açaí, a Arara-Azul")
				.Replace("Uma arara-vermelha brasileira em pixel art: penas vermelhas, asas amarelas e\nazuis, o rosto branco e um bico grande e curvo.",
					"Uma arara-azul-grande brasileira em pixel art: penas azul-cobalto, um anel\namarelo em volta do olho, amarelo na base do bico e um bico grande, escuro e curvo.")
				.Replace("Ela anda gingando", "Ele anda gingando")
				.Replace("amiga arara-azul passando", "amiga arara-vermelha passando");
		}

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
