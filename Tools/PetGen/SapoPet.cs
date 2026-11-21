using System.Drawing;

namespace PetGen;

/// <summary>
/// "Cururu" — a green frog in pixel art (sapo-cururu, from the Brazilian folk song). He moves by hopping, and every
/// time he eats something his tongue shoots out far: it leaves his own window and crosses one, two or three child
/// windows in front of him, sticks to the insect and pulls it back into his mouth. The child windows are drawn
/// together with his frames, step by step, so the tongue looks like one piece across the windows.
/// "catch visitor" is the animation the program plays when a visitor of the Visitors plugin reaches him: the tongue
/// goes straight up to it and the program pulls the visitor into his mouth.
/// Like every DesktopPet pet he faces left. Frames are 36 px wide with 14 rows of headroom; the feet stand on row 35.
/// </summary>
static class SapoPet
{
	const int W = 36, Head = 14;
	const int FX = 5, FY = 18;          // where the frog art sits
	const int MX = 6, MY = 27;          // the corner of his mouth: where the tongue comes out
	const int UpTipX = 1, UpTipY = -11;  // where the tongue ends when it catches a visitor (Visitors.cs uses the same point)
	const char Out = 'O', Grn = 'G', GrnLight = 'g', GrnDark = 'D', Belly = 'L', White = 'W', Eye = 'E', Mouth = 'K';
	const char Tongue = 'T', TongueTip = 'R', Inside = 'r', Yel = 'Y', Spark = 'X', Water = 'A', BlueLight = 'u', Blue = 'U';
	const char Heart = 'H', HeartLight = 'h', Zz = 'Z', Note = 'q', Gray = 'k', Cloud = 'c', Line = 'l', Brown = 'N', BrownDark = 'n';
	const char Pink = 'P', Orange = 'o', Bug = 'B', Wing = 'w', Gold = 'y', Lily = 'V', LilyLight = 'v';

	// the frog sitting, facing left (26 x 18)
	static readonly string[] FrogArt =
	{
		".....OOOO.................",
		"....OWWWWO................",
		"...OWWWWWWO...............",
		"...OWWWWWWO..OOOO.........",
		"...OGWWWWGOOOGGGGOO.......",
		"..OGGGGGGGGGGGGGGGGOO.....",
		".OGGGGGGGGGGGGGgggGGGO....",
		"OGGGGGGGGGGGGGGGgGGGGGO...",
		"OKKKKKKKKKOGGGGGGGGGGGGO..",
		"OLLLLLLLLLKGGGGGGGGgGGGO..",
		".OLLLLLLLLLOGGGGGGgggGGGO.",
		"..OLLLLLLLLOGGGGGGGGGGGGO.",
		"...OOLLLLLOGGGGGGDGGGGGGO.",
		"....OGOOOOGGGGGGDGGGGGGO..",
		"...OGGGO.OGGGGGGODGGGGGO..",
		"..OGGGO..OGGGGGOOGGGGGO...",
		".OGOGOGO.OOOOOOGGGGGGO....",
		".OOOOOOO......OOOOOOOO....",
	};
	// legs stretched out in a leap (replace rows 13-17)
	static readonly string[] LeapLegs =
	{
		"....OGOOOOGGGGGGGGGGGGGOOO....",
		"...OGGO...OOGGGGGGGGGGGGGGOO..",
		"..OGGO......OOOOOOOOGGGGGGGGO.",
		".OGOGO..............OOOOOGOGOO",
		".OOOO.................OOOOOO..",
	};

	// ---- the legs for each moment of the smooth hop (they replace rows 13-17 of FrogArt; some are longer)
	static readonly string[] SitLegs = FrogArt.Skip(13).ToArray();
	// crouched to jump: the front feet a bit forward, the back foot flat and wide
	static readonly string[] CrouchLegs =
	{
		"....OGOOOOGGGGGGDGGGGGGO..",
		"...OGGGO.OGGGGGGODGGGGGO..",
		"..OGGGO..OGGGGGOOGGGGGGO..",
		".OGOGOGO.OOOOOOGGGGGGGO...",
		".OOOOOOO.....OOOOOOOOOO...",
	};
	// pushing off (body raised 2 px and leaning forward): the back legs stretch down and back, the toes still on the ground
	static readonly string[] PushLegs =
	{
		"....OGOOOOOGGGGGGGGGGGGGO.....",
		"...OGGO....OOOOOOGGGGGGGGO....",
		"..OGGO...........OOGGGGGGO....",
		"..OGO..............OOGGGGGO...",
		"..OO.................OGGGGGO..",
		"......................OGGGGGO.",
		"....................OOOOOOOOOO",
	};
	// pushing off straight up (jumping in place): the back legs stretch down, the foot flat under him
	static readonly string[] PushUpLegs =
	{
		"....OGOOOOOGGGGGGGGGGGGGO.....",
		"...OGGO....OOOOGGGGGGGGGO.....",
		"..OGGO.........OOGGGGGGO......",
		"..OGO...........OGGGGGO.......",
		"..OO............OGGGGGO.......",
		"...............OGGGGGGGO......",
		"............OOOOOOOOOOOO......",
	};
	// just left the ground: the legs almost straight, pointing down and back
	static readonly string[] TakeoffLegs =
	{
		"....OGOOOOOGGGGGGGGGGGGGOO....",
		"...OGGO....OOOOOOGGGGGGGGGO...",
		"..OGGO...........OOGGGGGGGGO..",
		"..OGO..............OOOGGGGGGO.",
		"..OO..................OOGGOGO.",
		"........................OOOO..",
	};
	// the legs folding back in after the top of the jump
	static readonly string[] FoldLegs =
	{
		"....OGOOOOGGGGGGGGGGGGGGO.....",
		"...OGGGO..OGGGGGGGGGGGGGGO....",
		"..OGGGO...OOOOOGGGGGGGGGO.....",
		".OGOGOGO......OOGGGGGOGOO.....",
		".OOOOOOO........OOOOOOOO......",
	};
	// coming down: the front feet reach forward and down for the ground, the back legs tucked under him
	static readonly string[] ReachLegs =
	{
		"....OGOOOOGGGGGGDGGGGGGO..",
		"...OGGO..OGGGGGGODGGGGGO..",
		"..OGGO...OGGGGGOOGGGGGO...",
		".OGGO....OOOOOOGGGGGGO....",
		"OGOGO.........OOOOOOOO....",
		"OOOO......................",
	};
	// the landing: the front feet spread out, the back foot flat
	static readonly string[] LandLegs =
	{
		"...OOGOOOOGGGGGGDGGGGGGO..",
		"..OGGGO..OGGGGGGODGGGGGO..",
		".OGGGO...OGGGGGOOGGGGGGO..",
		"OGOGOGO..OOOOOOGGGGGGGO...",
		"OOOOOOO.....OOOOOOOOOOO...",
	};

	public static PetDef Build()
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		var p = new PetDef
		{
			Folder = "sapo",
			Title = "Cururu the Frog",
			PetName = "Cururu",
			Author = "Luan Felipe",
			Version = "1.0",
			Info = "Cururu is a green frog who moves by hopping.[br]" +
				   "His tongue shoots out really far to catch flies, mosquitoes and fireflies,[br]" +
				   "and he even catches the visitors that fly by.",
			Palette = new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(24, 62, 30),
				[Grn] = Color.FromArgb(86, 178, 70), [GrnLight] = Color.FromArgb(140, 214, 104), [GrnDark] = Color.FromArgb(52, 128, 50),
				[Belly] = Color.FromArgb(220, 236, 160), [White] = Color.FromArgb(255, 255, 255), [Eye] = Color.FromArgb(20, 20, 20),
				[Mouth] = Color.FromArgb(28, 60, 30),
				[Tongue] = Color.FromArgb(240, 104, 128), [TongueTip] = Color.FromArgb(204, 60, 90), [Inside] = Color.FromArgb(150, 40, 60),
				[Yel] = Color.FromArgb(255, 214, 60), [Spark] = Color.FromArgb(255, 236, 110), [Water] = Color.FromArgb(110, 180, 240),
				[BlueLight] = Color.FromArgb(150, 196, 245), [Blue] = Color.FromArgb(56, 110, 220),
				[Heart] = Color.FromArgb(232, 62, 98), [HeartLight] = Color.FromArgb(255, 150, 170), [Zz] = Color.FromArgb(150, 170, 240),
				[Note] = Color.FromArgb(120, 84, 220), [Gray] = Color.FromArgb(150, 150, 156), [Cloud] = Color.FromArgb(226, 230, 238),
				[Line] = Color.FromArgb(120, 120, 126), [Brown] = Color.FromArgb(150, 100, 56), [BrownDark] = Color.FromArgb(96, 60, 30),
				[Pink] = Color.FromArgb(255, 160, 180), [Orange] = Color.FromArgb(255, 150, 40), [Bug] = Color.FromArgb(40, 40, 50),
				[Wing] = Color.FromArgb(200, 225, 245), [Gold] = Color.FromArgb(240, 190, 40),
				[Lily] = Color.FromArgb(60, 150, 70), [LilyLight] = Color.FromArgb(110, 190, 100),
			},
			Scale = 2,
			TilesX = 8,
		};

		// ------------------------------------------------------------ helpers
		void St(Frame f, int x, int y, string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		Frame Empty() => new Frame();
		// frames that look the same are stored once (the tongue scenes repeat many steps)
		var seen = new Dictionary<string, int>();
		int Add(Frame f) { string k = f.ToString(); if (!seen.TryGetValue(k, out int i)) { i = p.Add(f); seen[k] = i; } return i; }

		// the eyelid comes down over the white of the eye (rows from the top)
		void Lid(Frame f, int x, int y, int rows) { for (int yy = y; yy < y + rows; yy++) for (int xx = x; xx < x + 6; xx++) if (f[xx, yy] == White) f[xx, yy] = Grn; }
		// eyes: 0 open, 1 closed, 2 happy, 3 wide, 4 up, 5 half, 6 angry, 7 teary, 8 back, 9 forward (stalking), 10 forward (hopping)
		void Eyes(Frame f, int x, int y, int mode)
		{
			// (x, y) = the top-left of the white of the eye (6 x 3 inside the bump)
			switch (mode)
			{
				case 0: f.Rect(x + 2, y, 2, 2, Eye).Dot(x + 2, y, White); break;
				case 1: Lid(f, x, y, 4); f.Rect(x + 1, y + 2, 4, 1, Out); break;
				case 2: f.Dot(x + 1, y + 1, Eye).Dot(x + 2, y, Eye).Dot(x + 3, y, Eye).Dot(x + 4, y + 1, Eye); break;
				case 3: f.Rect(x + 2, y - 1, 2, 3, Eye).Dot(x + 3, y - 1, White); break;
				case 4: f.Rect(x + 2, y - 1, 2, 2, Eye).Dot(x + 2, y - 1, White); break;
				case 5: Lid(f, x, y, 2); f.Rect(x + 1, y + 1, 4, 1, Out).Rect(x + 2, y + 2, 2, 1, Eye); break;
				case 6: f.Rect(x + 2, y, 2, 2, Eye).Rect(x, y - 1, 3, 1, Out).Dot(x + 3, y - 1, Out); break;
				case 7: f.Rect(x + 2, y, 2, 2, Eye).Dot(x + 1, y + 1, Water).Dot(x + 1, y + 2, Water); break;
				case 8: f.Rect(x + 4, y, 2, 2, Eye).Dot(x + 4, y, White); break;
				case 9: f.Rect(x, y, 2, 2, Eye).Dot(x, y, White); break;
			case 10: f.Rect(x + 1, y, 2, 2, Eye).Dot(x + 1, y, White); break;
			}
		}
		// mouth: 0 closed, 1 open, 2 wide, 3 smile
		void MouthAt(Frame f, int x, int y, int mode)
		{
			if (mode == 1) { f.Rect(x + 1, y + 9, 8, 1, Inside).Rect(x + 1, y + 10, 8, 1, Mouth); }
			if (mode == 2) { f.Rect(x + 1, y + 9, 9, 2, Inside).Rect(x + 1, y + 11, 8, 1, Mouth); }
			if (mode == 3) { f.Dot(x + 9, y + 7, Mouth).Dot(x + 8, y + 8, GrnDark); }
		}
		// the throat sac: 0 none, 1 half, 2 full (croaking), 3 bulge (gulping)
		void Sac(Frame f, int x, int y, int mode)
		{
			if (mode == 1) St(f, x + 1, y + 11, new[] { ".LLLL.", "LWWWWL", ".LLLL." });
			if (mode == 2) St(f, x, y + 10, new[] { "..OOOO..", ".OWWWWO.", "OWWWWWWO", "OWWWWWWO", ".OWWWWO.", "..OOOO.." });
			if (mode == 3) St(f, x + 2, y + 11, new[] { ".OO.", "OggO", ".OO." });
		}
		Frame Pose(int eyes = 0, int mouth = 0, int sac = 0, int up = 0, int dx = 0, bool leap = false, int squash = 0)
		{
			var f = new Frame();
			int x = FX + dx, y = FY - up;
			var art = FrogArt.Select(r => r).ToArray();
			if (leap) art = art.Take(13).ToArray();
			St(f, x, y, art);
			if (leap) St(f, x, y + 13, LeapLegs);
			Eyes(f, x + 4, y + 1, eyes);
			MouthAt(f, x, y, mouth);
			Sac(f, x, y, sac);
			// squash: the body gets lower (a row above the mouth goes away), the feet stay
			for (int i = 0; i < squash; i++)
			{
				for (int yy = FY + 7; yy > f.MinY; yy--) for (int xx = 0; xx < W; xx++) f[xx, yy] = f[xx, yy - 1];
				for (int xx = 0; xx < W; xx++) f[xx, f.MinY] = '.';
			}
			return f;
		}
		// The frog of the smooth hop, put together from the body (rows 0-12 of FrogArt) and a set of legs.
		// squash > 0: lower (rows above the mouth go away), squash < 0: one row taller (stretching up);
		// tilt > 0: the front of the body goes up (rising), tilt < 0: the back goes up (coming down) — the columns of the
		// art are shifted a pixel, like a small rotation.
		Frame Hop(int eyes, int up, string[] legs = null, int tilt = 0, int squash = 0, int dx = 0, int mouth = 0)
		{
			var f = new Frame();
			int x = FX + dx, y = FY - up;
			St(f, x, y, FrogArt.Take(13).ToArray());
			St(f, x, y + 13, legs ?? SitLegs);
			Eyes(f, x + 4, y + 1, eyes);
			MouthAt(f, x, y, mouth);
			int top = f.MinY;
			for (int i = 0; i < squash; i++)
			{
				for (int yy = y + 7; yy > top; yy--) for (int xx = 0; xx < W; xx++) f[xx, yy] = f[xx, yy - 1];
				for (int xx = 0; xx < W; xx++) f[xx, top] = '.';
			}
			for (int i = 0; i < -squash; i++)
				for (int yy = top; yy < y + 7; yy++) for (int xx = 0; xx < W; xx++) f[xx, yy] = f[xx, yy + 1];
			if (tilt != 0)
			{
				var c = f.Clone();
				f.Clear();
				for (int yy = top; yy < W; yy++) for (int xx = 0; xx < W; xx++)
				{
					if (c[xx, yy] == '.') continue;
					int ax = Math.Clamp(xx - x, 0, 25);
					int s = (int)Math.Round(Math.Abs(tilt) * (tilt > 0 ? 25 - ax : ax) / 25.0, MidpointRounding.AwayFromZero);
					f[xx, yy - s] = c[xx, yy];
				}
			}
			return f;
		}
		// a straight tongue from (x0, y0) to (x1, y1), two pixels thick, with a round tip
		void TongueLine(Frame f, int x0, int y0, int x1, int y1)
		{
			int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
			for (int i = 0; i <= n; i++)
			{
				int tx = x0 + (int)Math.Round((x1 - x0) * (double)i / Math.Max(1, n)), ty = y0 + (int)Math.Round((y1 - y0) * (double)i / Math.Max(1, n));
				f.Dot(tx, ty, Tongue).Dot(tx, ty + 1, Tongue);
			}
			St(f, x1 - 1, y1 - 1, new[] { ".R.", "RRR", "RRR", ".R." });
		}

		// small things
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
		string[] croac = { ".EE.EEE..EE...E...EE", "E...E.E.E..E.E.E.E..", "E...EE..E..E.EEE.E..", "E...E.E.E..E.E.E.E..", ".EE.E.E..EE..E.E..EE" };
		string[] ok = { "EEE.E.E", "E.E.EE.", "E.E.E..", "E.E.EE.", "EEE.E.E" };
		string[] check = { "......GG", ".....GG.", "GG..GG..", ".GGGG...", "..GG...." };
		string[] kkk = { "E...E...E..", "E.E.E.E.E.E", "EE..EE..EE.", "E.E.E.E.E.E", "E.E.E.E.E.E" };
		string[] bang = { "E", "E", "E", ".", "E" };
		string[] question = { "EEE", "..E", ".E.", "...", ".E." };
		void Sparks(Frame f, int k) { foreach (var (sx, sy) in new[] { (3, 10), (30, 12), (1, 24), (33, 20), (17, 8) }) f.Dot(sx + k, sy - k, Spark); }
		void Bulb(Frame f, int x, int y, bool lit) => St(f, x, y, new[] { ".XXX.", "XXXXX", "XXWXX", ".XXX.", ".lll." }.Select(r => lit ? r : r.Replace('X', 'c').Replace('W', 'k')).ToArray());
		void LoadingRing(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (0, -3), (2, -2), (3, 0), (2, 2), (0, 3), (-2, 2), (-3, 0), (-2, -2) };
			for (int i = 0; i < 8; i++) f.Dot(cx + pts[i].Item1, cy + pts[i].Item2, ((i - k * 2) % 8 + 8) % 8 < 3 ? Blue : Cloud);
		}
		void Book(Frame f, int x, int y) => St(f, x, y, new[] { "OUUUUOUUUUO", "OWWWWOWWWWO", "OWllWOWllWO", "OWWWWOWWWWO", ".OOOOOOOOO." });
		void Glasses(Frame f) { int x = FX + 5, y = FY + 1; f.Rect(x, y, 6, 1, Eye).Rect(x, y + 3, 6, 1, Eye).Rect(x, y, 1, 4, Eye).Rect(x + 5, y, 1, 4, Eye); }
		void Laptop(Frame f, int x, int y, int k) { St(f, x, y, new[] { "OOOOOOOOO", "OUUUUUUUO", "OUggUUUUO", "OUUXXUUUO", "OOOOOOOOO", "OlllllllO", ".OOOOOOO." }); if (k == 1) f.Dot(x + 5, y + 2, GrnLight).Dot(x + 6, y + 2, GrnLight); }
		void DownloadBox(Frame f, int x, int y) => St(f, x, y, new[] { "OOOOOOO", "OGGWGGO", "OGGWGGO", "OGWWWGO", "OGGWGGO", "OOOOOOO" });
		void Warning(Frame f, int x, int y) => St(f, x, y, new[] { "....O....", "...OYO...", "..OYEYO..", "..OYEYO..", ".OYYEYYO.", ".OYYYYYO.", "OYYYEYYYO", "OOOOOOOOO" });
		void Steam(Frame f, int x, int y) => St(f, x, y, new[] { ".cc.", "cckc", ".cc." });
		void Sweat(Frame f, int x, int y) => St(f, x, y, new[] { ".A.", "AAA", "AWA", ".A." });
		void Sun(Frame f, int x, int y) => St(f, x, y, new[] { "X.X.X", ".XXX.", "XXXXX", ".XXX.", "X.X.X" });
		void Snow(Frame f, int k) { foreach (var (sx, sy) in new[] { (2, -10), (9, -4), (19, -8), (29, -2), (5, 6), (33, 10), (14, -12) }) f.Dot(sx, ((sy + k * 3) + 60) % 36 - 12, Zz); }
		void Confetti(Frame f, int k)
		{
			char[] cs = { Heart, Spark, Blue, Grn, Yel };
			for (int i = 0; i < 20; i++) f.Dot((i * 7 + k * 3) % W, -12 + (i * 5 + k * 4) % 40, cs[(i + k) % cs.Length]);
		}
		void CloudAt(Frame f, int x, int y, int k)
		{
			St(f, x, y, new[] { "...kkkk.....", ".kkcccckk...", "kccccccccckk", "kcccccccccck", ".kkkkkkkkkk." });
			for (int i = 0; i < 5; i++) Drop(f, x + 1 + i * 2 + k, y + 6 + ((i + k) % 2) * 3);
		}
		void LeafUmbrella(Frame f) { St(f, 6, 4, new[] { "....VVVVV....", "..VVvVVVvVV..", ".VVVVvVvVVVV.", "VVVVVVvVVVVVV", "V.V.V.N.V.V.V" }); f.Rect(12, 9, 1, 12, Brown); }
		void LilyPad(Frame f) => St(f, 1, 33, new[] { "..VVVVVVVV.VVVVVVVVVV....", ".VvVVVVVVVVVVVVVvVVVVVV..", "VVVVVVVvVVVVVVVVVVVVVVVV." });
		void Shades(Frame f) => f.Rect(FX + 4, FY + 1, 7, 3, Eye).Dot(FX + 5, FY + 1, White).Rect(FX + 11, FY + 2, 3, 1, Eye);
		void Crown(Frame f, int x, int y) => St(f, x, y, new[] { "y.y.y", "yyyyy", "yXyXy", "yyyyy" });
		void Scarf(Frame f) => St(f, FX + 9, FY + 11, new[] { "HWHWHWHWHWHWH", "HW...........", "WH..........." });
		void Beanie(Frame f) => St(f, FX + 2, FY - 3, new[] { "....W....", "..UWUWU..", ".UWUWUWU.", "WUWUWUWUW" });
		void Headphones(Frame f) { St(f, FX + 4, FY - 2, new[] { "..qqqqq..", ".q.....q.", "q.......q" }); f.Rect(FX + 11, FY + 1, 3, 4, Note); }
		void PartyHat(Frame f) => St(f, FX + 5, FY - 6, new[] { "...X...", "..HXH..", "..XHX..", ".HXHXH.", ".XHXHX." });
		void Cup(Frame f, int x, int y) => St(f, x, y, new[] { "OWWWO.", "OnnnOO", "OWWWO.O", ".OOO.." });
		void Broom(Frame f, int k) { for (int i = 0; i < 9; i++) f.Dot(9 - i / 2 + k, 24 + i, BrownDark); St(f, 1 + k, 32, new[] { ".yyyy", "yyyyy", "yYyYy", "YyYyY" }); }
		void Hammer(Frame f, bool up) => St(f, 1, up ? 22 : 30, up ? new[] { "lll.", "lll.", ".N..", ".N..", ".N.." } : new[] { "NNNll", "...ll" });
		void Dust(Frame f, int x, int y) => St(f, x, y, new[] { "..kk..", ".kcck.", "kcckck", ".kkkk." });
		void Puddle(Frame f, int x, int w) => f.Rect(x, 35, w, 1, Water).Rect(x + 1, 34, w - 2, 1, BlueLight);
		void HandFan(Frame f, int k) => St(f, 2, 20, k == 0 ? new[] { "VvVvV", "VvVvV", ".VvV.", "..N..", "..N.." } : new[] { ".vVv.", "vVvVv", ".vVv.", "..N..", "..N.." });
		void IceCream(Frame f, int x, int y, bool scoop) { if (scoop) St(f, x, y, new[] { ".PPP.", "PhPPP", ".PPP." }); St(f, x, y + 3, new[] { ".NyN.", "..N..", "..y.." }); }
		void FortuneSlip(Frame f, int x, int y) => St(f, x, y, new[] { "WWWWW", "WlllW", "WWWWW" });

		// the prey (centered on (cx, cy)); k = wing beat
		void Prey(Frame f, string kind, int cx, int cy, int k)
		{
			switch (kind)
			{
				case "fly": St(f, cx - 2, cy - 2, k == 0 ? new[] { ".ww.", "wBBw", ".BB." } : new[] { "w..w", ".BB.", ".BB." }); break;
				case "mosquito": St(f, cx - 3, cy - 2, k == 0 ? new[] { "..w.w.", "BBBBBo", ".B.B.." } : new[] { ".w...w", "BBBBBo", "B...B." }); break;
				case "moth": St(f, cx - 3, cy - 2, k == 0 ? new[] { "nNN.NNn", ".NNBNN.", "..nBn.." } : new[] { "...B...", ".nNBNn.", "..nBn.." }); break;
				case "dragonfly": St(f, cx - 4, cy - 2, k == 0 ? new[] { ".ww.ww...", "UUUUUUUUU", ".ww.ww..." } : new[] { "w..w.....", "UUUUUUUUU", "w..w....." }); break;
				case "firefly": St(f, cx - 2, cy - 2, k == 0 ? new[] { "XwwX", "XBBX", ".YY." } : new[] { ".ww.", ".BB.", "XYYX" }); break;
				case "bee": St(f, cx - 3, cy - 2, k == 0 ? new[] { "..ww..", "BYBYBo", ".YBY.." } : new[] { ".w..w.", "BYBYBo", ".YBY.." }); break;
				case "cricket": St(f, cx - 3, cy - 2, k == 0 ? new[] { "g.....", ".gGGGo", "g.g.g." } : new[] { "g..g..", ".gGGGo", ".g...g" }); break;
				case "drop": St(f, cx - 1, cy - 2, new[] { ".A.", "AAA", "AuA", ".A." }); break;
				case "cookie": St(f, cx - 3, cy - 2, new[] { "..YYY..", ".YYyYY.", "YYyYyYY", ".YYYYY." }); break;
			}
		}

		// ------------------------------------------------------------ frames: the frog
		int fSit = Add(Pose());
		int fBlink = Add(Pose(1));
		int fHappy = Add(Pose(2, 3));
		int fWide = Add(Pose(3, 1));
		int fUp = Add(Pose(4));
		int fBack = Add(Pose(8));
		int fStalk = Add(Pose(9, 0, 0, 0, 0, false, 1));
		int fAir = Add(Pose(2, 0, 0, 5, 0, true));
		int fBigLeap = Add(Pose(3, 1, 0, 12, -1, true));
		int fSac1 = Add(Pose(1, 0, 1));
		int fSac2 = Add(Pose(1, 0, 2));
		int fGulp = Add(Pose(1, 0, 3));
		var lick = Pose(2, 1); St(lick, FX + 1, FY + 7, new[] { "TT", "TR" }); int fLick = Add(lick);
		int fTalk = Add(Pose(0, 1));
		var dz = Pose(1, 1, 0, 0, 0, false, 2); dz.Dot(8, 14, Spark).Dot(18, 12, Spark).Dot(13, 10, Spark); int fDizzy0 = Add(dz);
		Frame Sleep(int z) { var f = Pose(1, 0, z % 2 == 0 ? 1 : 0, 0, 0, false, 2); if (z >= 1) Zed(f, 24, 12); if (z >= 2) Zed(f, 28, 6); return f; }
		int fSleep1 = Add(Sleep(0)), fSleep2 = Add(Sleep(1)), fSleep3 = Add(Sleep(2));

		// ------------------------------------------------------------ frames: the smooth hop
		// getting ready: lower and lower, the eyes on where he is going
		int fCrouch1 = Add(Hop(10, 0, CrouchLegs, 0, 1));
		int fCrouch2 = Add(Hop(10, 0, CrouchLegs, 0, 2));
		int fCrouch3 = Add(Hop(10, 0, CrouchLegs, 0, 3));
		// the push: the body goes up and forward, the back legs stretch out behind, the toes still on the ground
		const int Lean = -3;
		int fPush = Add(Hop(10, 2, PushLegs, 1, 0, Lean));
		// straight up (jumping in place): no leaning
		int fPushUp = Add(Hop(10, 2, PushUpLegs, 0, -1));
		// One jump through the air: n frames along a parabola h px high. The legs stretch back while he rises, fold in
		// at the top and reach for the ground on the way down; his front goes up while he rises and down as he falls.
		// lean: the body starts ahead of the window (as in the push) and comes back to its place by the landing.
		List<int> Arc(int n, int h, int eyes, int mouth = 0, bool lean = true, Action<Frame> extra = null)
		{
			int Up(int i)
			{
				double k = (i + 0.5) / n * 2 - 1;
				return (int)Math.Round(h * (1 - k * k), MidpointRounding.AwayFromZero);
			}
			var list = new List<int>();
			for (int i = 0; i < n; i++)
			{
				double t = (i + 0.5) / n;
				int up = Up(i), next = i + 1 < n ? Up(i + 1) : 0;
				string[] legs = i == 0 ? TakeoffLegs : t < 0.5 ? LeapLegs : t < 0.72 ? FoldLegs : ReachLegs;
				// the front up while he still goes up, level at the top, the front down as soon as he comes down
				int tilt = !lean ? 0 : next > up ? 1 : next == up ? 0 : -1;
				int dx = lean ? (int)Math.Round(Lean * (1 - (double)i / (n - 1)), MidpointRounding.AwayFromZero) : 0;
				var f = Hop(eyes, up, legs, tilt, t < 0.15 ? -1 : 0, dx, mouth);
				extra?.Invoke(f);
				list.Add(Add(f));
			}
			return list;
		}
		// touching down: the front feet first, then the squash, a little bounce back up and sitting again
		int fTouch = Add(Hop(0, 1, ReachLegs, -1));
		int fSquash3 = Add(Hop(1, 0, LandLegs, 0, 3));
		int fSquash2 = Add(Hop(1, 0, LandLegs, 0, 2));
		int fSquash1 = Add(Hop(5, 0, CrouchLegs, 0, 1));
		int fRebound = Add(Hop(0, 0, SitLegs, 0, -1));
		int fTouchWide = Add(Hop(3, 1, ReachLegs, -1, 0, 0, 1));
		// coming down at once (he hit a border in the middle of a hop)
		int fComeDown1 = Add(Hop(10, 6, FoldLegs, -1)), fComeDown2 = Add(Hop(10, 3, ReachLegs, -1));
		// jumping in place: touching down straight
		int fTouchUp = Add(Hop(0, 1, ReachLegs));
		// falling (after being dropped): flapping his legs, ready for the ground
		int fFall1 = Add(Hop(3, 2, ReachLegs, 0, 0, 0, 1));
		int fFall2 = Add(Hop(3, 2, FoldLegs, 0, 0, 0, 1));

		// ------------------------------------------------------------ the tongue scenes
		// u = how far from his mouth (in frame pixels, towards the front); window k (k >= 1) is the child at imageX-imageW*k.
		int XIn(int k, int u) => MX - u + W * k;
		Frame Window(int k, int g, string prey, int preyU, int preyDy, int wing, bool attached, bool parent, int eyes)
		{
			var f = parent ? Pose(eyes, g > 0 || attached ? 1 : 0) : Empty();
			for (int u = 0; u <= g; u++) { int x = XIn(k, u); if (x >= 0 && x < W) f.Dot(x, MY, Tongue).Dot(x, MY + 1, Tongue); }
			if (g > 0) { int tx = XIn(k, g); St(f, tx - 1, MY - 1, new[] { ".R.", "RRR", "RRR", ".R." }); }
			if (prey != null)
			{
				int px = XIn(k, attached ? g : preyU);
				if (px > -6 && px < W + 6) Prey(f, prey, px, MY + (attached ? 0 : preyDy), wing);
			}
			return f;
		}
		// A whole catch: the prey arrives and hovers in front of him (watch steps), the tongue shoots out across n windows,
		// sticks, and comes back with the prey. Returns the frames of his window and of each child window, step by step.
		(List<int> parent, List<int>[] childs) Catch(string prey, int n, int watch, bool miss = false)
		{
			int L = W * n - 4 - MX + 6;   // the tongue tip ends 10 px inside the last window
			L = XIn(n, 0) - 10;
			var steps = new List<(int g, int pu, int pdy, bool att, int eyes)>();
			for (int i = 0; i < watch; i++) steps.Add((0, L + Math.Max(0, 8 - i * 2), i % 4 < 2 ? 0 : -1, false, i < watch - 3 ? 9 : 3));
			steps.Add((0, L, 0, false, 3));
			for (int i = 1; i <= 4; i++) steps.Add((L * i / 4, L, 0, false, 3));
			if (miss)
			{
				for (int i = 1; i <= 6; i++) steps.Add((L, L + i, -4 * i, false, 3));
				for (int i = 3; i >= 0; i--) steps.Add((L * i / 4, L + 20, -40, false, 5));
			}
			else
			{
				steps.Add((L, L, 0, true, 2)); steps.Add((L, L, 0, true, 2));
				for (int i = 3; i >= 0; i--) steps.Add((L * i / 4, L, 0, true, 1));
			}
			var par = new List<int>();
			var ch = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
			for (int s = 0; s < steps.Count; s++)
			{
				var (g, pu, pdy, att, eyes) = steps[s];
				bool showPrey = !(att && g == 0);
				par.Add(Add(Window(0, g, showPrey ? prey : null, pu, pdy, s % 2, att, true, eyes)));
				for (int k = 1; k <= n; k++) ch[k - 1].Add(Add(Window(k, g, showPrey ? prey : null, pu, pdy, s % 2, att, false, eyes)));
			}
			return (par, ch);
		}

		// the tongue straight up to a visitor above him (the program pulls the visitor into his mouth)
		Frame Up(int stage)
		{
			var f = Pose(4, stage > 0 ? 1 : 0);
			if (stage > 0)
			{
				// forward and up, to the front top corner (1, -11): the program waits there with the visitor (Visitors.cs TipX/TipY)
				int ty = MY + (UpTipY - MY) * Math.Min(stage, 3) / 3;
				int tx = MX + (UpTipX - MX) * Math.Min(stage, 3) / 3;
				TongueLine(f, MX, MY, tx, ty);
			}
			return f;
		}
		int fUp1 = Add(Up(1)), fUp2 = Add(Up(2)), fUp3 = Add(Up(3));

		// ------------------------------------------------------------ other frames
		var croakB = Empty(); Bubble(croakB, 0, -14, croac); int fCroakBubble = Add(croakB);
		var fr1 = Empty(); St(fr1, 6, FY, FrogArt.Select(r => r.Replace('G', 'D').Replace('g', 'G')).Take(18).ToArray()); int fFriend = Add(fr1);
		var fr2 = Empty(); St(fr2, 6, FY, FrogArt.Select(r => r.Replace('G', 'D').Replace('g', 'G')).Take(18).ToArray()); Sac(fr2, 6, FY, 2); int fFriendCroak = Add(fr2);
		var bub1 = Empty(); St(bub1, 2, 20, new[] { ".uu.", "u..u", "uW.u", ".uu." }); int fBubble = Add(bub1);
		var bubPop = Empty(); foreach (var (sx, sy) in new[] { (1, 20), (5, 20), (3, 18), (6, 23), (0, 23) }) bubPop.Dot(sx, sy, BlueLight); int fBubblePop = Add(bubPop);
		int fBlow = Add(Pose(1, 1, 1));
		var crownF = Empty(); Crown(crownF, FX + 5, -10); int fCrownFalls = Add(crownF);
		var prince = Pose(2, 3); Crown(prince, FX + 5, FY - 3); int fPrince = Add(prince);
		var kiss = Pose(1, 1); kiss.Dot(4, 26, Heart); int fKiss = Add(kiss);
		var sting = Pose(7, 2); St(sting, FX - 1, FY + 7, new[] { "HHH", "HhH", "HHH" }); int fStung = Add(sting);
		var glow1 = Pose(2, 3); for (int yy = glow1.MinY; yy < W; yy++) for (int xx = 0; xx < W; xx++) if (glow1[xx, yy] == Belly) glow1[xx, yy] = Yel;
		foreach (var (sx, sy) in new[] { (FX - 2, FY + 9), (FX + 11, FY + 13), (FX + 1, FY + 15) }) glow1.Dot(sx, sy, Spark);
		int fGlow1 = Add(glow1);
		int fGlow2 = Add(Pose(2, 3));
		var lickEye = Pose(1, 1); TongueLine(lickEye, MX, MY - 1, FX + 7, FY + 2); int fLickEye = Add(lickEye);
		var yawnT = Pose(1, 2); St(yawnT, 0, MY, new[] { "RTTTT" }); int fYawnTongue = Add(yawnT);
		var tadpoles = Empty(); Puddle(tadpoles, 0, 14); St(tadpoles, 3, 33, new[] { "EE.l...EE.l" }); int fTad1 = Add(tadpoles);
		var tadpoles2 = Empty(); Puddle(tadpoles2, 0, 14); St(tadpoles2, 4, 33, new[] { "lEE...l.EE" }); int fTad2 = Add(tadpoles2);
		var proud = Pose(2, 3); HeartAt(proud, 4, 10); int fProud = Add(proud);

		// program features
		var th1 = Pose(4); for (int i = 0; i < 3; i++) th1.Dot(6 + i * 2, 14 - i * 2, Note); int fThink1 = Add(th1);
		var th2 = Pose(0); St(th2, 2, 2, new[] { ".kkk.", "kccck", "kccck", ".kkk." }); int fThink2 = Add(th2);
		var id1 = Pose(3); Bulb(id1, 4, 2, false); int fIdea1 = Add(id1);
		var id2 = Pose(2, 3); Bulb(id2, 4, 2, true); id2.Dot(2, 4, Spark).Dot(11, 3, Spark); int fIdea2 = Add(id2);
		var ib = Empty(); Bulb(ib, 4, 2, true); int fBulb = Add(ib);
		var rd1 = Pose(0); Glasses(rd1); Book(rd1, 0, 30); int fRead1 = Add(rd1);
		var rd2 = Pose(1); Glasses(rd2); Book(rd2, 0, 30); int fRead2 = Add(rd2);
		var lp1 = Pose(9); Laptop(lp1, 0, 29, 0); int fLap1 = Add(lp1);
		var lp2 = Pose(9, 1); Laptop(lp2, 0, 29, 1); int fLap2 = Add(lp2);
		var code = Empty(); St(code, 0, 22, new[] { ".g...g.", "g..g..g", ".g.g.g.", "...g..." }); int fCode = Add(code);
		Frame Loading(int k) { var f = Pose(k % 2 == 0 ? 4 : 0); LoadingRing(f, 8, 8, k); return f; }
		int fLoad1 = Add(Loading(0)), fLoad2 = Add(Loading(1)), fLoad3 = Add(Loading(2)), fLoad4 = Add(Loading(3));
		var dlb = Empty(); DownloadBox(dlb, 4, 0); int fDlBox = Add(dlb);
		var dlc = Pose(2, 3); DownloadBox(dlc, 0, 26); int fDlCatch = Add(dlc);
		var tu = Pose(2, 3); Bubble(tu, 0, 2, check); int fThumbs = Add(tu);
		var okf = Pose(2, 3); Bubble(okf, 0, 2, ok); int fOk = Add(okf);
		var cp = Pose(2, 2, 0, 4, 0, true); Confetti(cp, 0); int fPopped = Add(cp);
		int fPopUp = Add(Hop(2, 2, TakeoffLegs, 0, -1, 0, 2)), fPopDown = Add(Hop(2, 2, ReachLegs, 0, 0, 0, 2));
		var cfa = Empty(); Confetti(cfa, 0); int fConf1 = Add(cfa);
		var cfb = Empty(); Confetti(cfb, 1); int fConf2 = Add(cfb);
		var er = Pose(3, 2); Warning(er, 3, 4); int fError = Add(er);
		var wn = Empty(); Warning(wn, 3, 4); int fWarn = Add(wn);
		int fCry1 = Add(Pose(7, 1)), fCry2 = Add(Pose(1, 2, 0, 0, 0, false, 1));
		var an1 = Pose(6, 2, 2); an1.Shift(-1, 0); int fAngry1 = Add(an1);
		var an2 = Pose(6, 0); an2.Shift(1, 0); int fAngry2 = Add(an2);
		var stc = Empty(); Steam(stc, 16, 12); int fSteam = Add(stc);
		var sk = Pose(3, 2, 0, 5); sk.Dot(FX + 3, FY - 8, Spark).Dot(FX + 12, FY - 9, Spark); int fShocked = Add(sk);
		var sc1 = Pose(3, 1); sc1.Shift(-1, 0); Sweat(sc1, FX + 18, FY - 2); int fScared1 = Add(sc1);
		var sc2 = Pose(3, 0); sc2.Shift(1, 0); Sweat(sc2, FX + 19, FY); int fScared2 = Add(sc2);
		var um1 = Pose(4); LeafUmbrella(um1); Drop(um1, 2, 8); Drop(um1, 30, 6); int fUmb1 = Add(um1);
		var um2 = Pose(2, 3); LeafUmbrella(um2); Drop(um2, 3, 12); Drop(um2, 31, 10); int fUmb2 = Add(um2);
		var pd0 = Pose(2, 3); Puddle(pd0, 0, 14); int fPud0 = Add(pd0);
		var pdj = Pose(2, 2, 0, 8, 0, true); Puddle(pdj, 0, 14); int fPudJump = Add(pdj);
		var pds = Pose(3, 2, 0, 0, 0, false, 2); Puddle(pds, 0, 16); foreach (var (sx, sy) in new[] { (0, 29), (3, 27), (11, 28), (15, 31) }) Drop(pds, sx, sy); int fPudSplash = Add(pds);
		var spl = Empty(); foreach (var (sx, sy) in new[] { (2, 27), (4, 30), (0, 32) }) Drop(spl, sx, sy); int fSplash = Add(spl);
		var ht1 = Pose(5, 1); Sweat(ht1, FX + 18, FY - 2); Sun(ht1, 1, -6); int fHot1 = Add(ht1);
		var ht2 = Pose(5, 2); Sweat(ht2, FX + 19, FY); Sun(ht2, 1, -6); int fHot2 = Add(ht2);
		var hf1 = Pose(2); HandFan(hf1, 0); int fFan1 = Add(hf1);
		var hf2 = Pose(1); HandFan(hf2, 1); int fFan2 = Add(hf2);
		var sb1 = Pose(2, 3, 0, 0, 0, false, 2); Shades(sb1); LilyPad(sb1); Sun(sb1, 1, -6); int fSunbathe1 = Add(sb1);
		var sb2 = Pose(2, 3, 1, 0, 0, false, 2); Shades(sb2); LilyPad(sb2); Sun(sb2, 2, -5); int fSunbathe2 = Add(sb2);
		var ic1 = Pose(2); IceCream(ic1, 0, 22, true); int fIce1 = Add(ic1);
		var ic2 = Pose(1, 1); IceCream(ic2, 0, 22, true); TongueLine(ic2, MX, MY, 3, 23); int fIce2 = Add(ic2);
		var cd1 = Pose(1, 0, 0, 0, 0, false, 1); cd1.Shift(-1, 0); Snow(cd1, 0); int fCold1 = Add(cd1);
		var cd2 = Pose(1, 0, 0, 0, 0, false, 1); cd2.Shift(1, 0); Snow(cd2, 1); int fCold2 = Add(cd2);
		var sf1 = Pose(2); Scarf(sf1); Snow(sf1, 0); int fScarf1 = Add(sf1);
		var sf2 = Pose(1); Scarf(sf2); Snow(sf2, 1); int fScarf2 = Add(sf2);
		var bn1 = Pose(2); Beanie(bn1); int fBeanie1 = Add(bn1);
		var bn2 = Pose(4); Beanie(bn2); int fBeanie2 = Add(bn2);
		var snowC = Empty(); Snow(snowC, 0); int fSnowC1 = Add(snowC);
		var snowC2 = Empty(); Snow(snowC2, 1); int fSnowC2 = Add(snowC2);
		var cl1 = Empty(); CloudAt(cl1, 10, -12, 0); int fCloud1 = Add(cl1);
		var cl2 = Empty(); CloudAt(cl2, 10, -12, 1); int fCloud2 = Add(cl2);
		var su = Pose(3, 1, 0, 3); Bubble(su, 2, -2, bang); int fSurprised = Add(su);
		int fTired1 = Add(Pose(5, 0, 0, 0, 0, false, 1)), fTired2 = Add(Pose(1, 1, 0, 0, 0, false, 2));
		var sl2 = Pose(1, 0, 1, 0, 0, false, 3); Zed(sl2, 24, 10); int fSleepy2 = Add(sl2);
		var sw1 = Pose(5, 1); Sweat(sw1, FX + 18, FY - 2); Sweat(sw1, 3, 10); int fSweat1 = Add(sw1);
		var sw2 = Pose(5, 0); Sweat(sw2, FX + 19, FY); int fSweat2 = Add(sw2);
		Frame Dizzy(int k)
		{
			var f = Pose(1, 1, 0, 0, k == 0 ? -1 : 1);
			var stars = new[] { (8, 12), (14, 10), (20, 12), (14, 14) };
			for (int i = 0; i < 4; i++) { var (sx, sy) = stars[(i + k) % 4]; f.Dot(sx, sy, Spark).Dot(sx + 1, sy, Spark).Dot(sx, sy + 1, Spark); }
			return f;
		}
		int fDizzyA = Add(Dizzy(0)), fDizzyB = Add(Dizzy(1)), fDizzyC = Add(Dizzy(2));
		var mir = Pose(2, 3); mir.MirrorX(); int fMirror = Add(mir);
		int fNod = Add(Pose(2, 3, 0, 0, 0, false, 2));
		var pt1 = Pose(2, 3); PartyHat(pt1); St(pt1, 0, 26, new[] { ".HH", "H.H", ".H." }); int fParty1 = Add(pt1);
		var pt2 = Pose(3, 1); PartyHat(pt2); pt2.Rect(0, 27, 6, 1, Heart).Rect(0, 28, 6, 1, Yel); int fParty2 = Add(pt2);
		var hp = Pose(2, 3, 0, 2); hp.Dot(2, 12, Spark).Dot(31, 14, Spark); int fHappy2 = Add(hp);
		var hg1 = Pose(1, 3); HeartAt(hg1, 1, 26); int fHug1 = Add(hg1);
		var hg2 = Pose(2, 3); HeartAt(hg2, 1, 26); HeartAt(hg2, 4, 8); int fHug2 = Add(hg2);
		var hc = Empty(); HeartAt(hc, 6, 12); int fHeartChild = Add(hc);
		var lv1 = Pose(2, 3); HeartAt(lv1, 6, 10); int fLove1 = Add(lv1);
		var lv2 = Pose(2, 3, 1); HeartAt(lv2, 7, 6); HeartAt(lv2, 26, 12); int fLove2 = Add(lv2);
		var swp1 = Pose(9); Broom(swp1, 0); int fSweep1 = Add(swp1);
		var swp2 = Pose(9, 0, 0, 0, -1); Broom(swp2, -1); Dust(swp2, 0, 31); int fSweep2 = Add(swp2);
		var du = Empty(); Dust(du, 2, 31); int fDust = Add(du);
		var wk1 = Pose(9); Hammer(wk1, true); wk1.Rect(0, 34, 9, 2, Brown); int fWork1 = Add(wk1);
		var wk2 = Pose(3, 1, 0, 0, -1); Hammer(wk2, false); wk2.Rect(0, 34, 9, 2, Brown); wk2.Dot(1, 30, Spark).Dot(7, 29, Spark); int fWork2 = Add(wk2);
		var ms1 = Pose(1, 3, 0, 0, -1); Headphones(ms1); int fMusic1 = Add(ms1);
		var ms2 = Pose(2, 1, 0, 1, 1); Headphones(ms2); int fMusic2 = Add(ms2);
		var nc = Empty(); NoteAt(nc, 4, 10); int fNote = Add(nc);
		var cf1 = Pose(0); Cup(cf1, 0, 26); int fCoffee1 = Add(cf1);
		var cf2 = Pose(1, 1, 0, 0, -1); Cup(cf2, 0, 25); TongueLine(cf2, MX, MY, 3, 26); int fCoffee2 = Add(cf2);
		var stm = Empty(); St(stm, 1, 20, new[] { ".k.k", "k.k.", ".k.k" }); int fCoffeeSteam = Add(stm);
		var lg1 = Pose(2, 2); Bubble(lg1, 0, 0, kkk); int fLaugh1 = Add(lg1);
		var lg2 = Pose(2, 1, 0, 1); Bubble(lg2, 0, 0, kkk); int fLaugh2 = Add(lg2);
		int fDance1 = Add(Pose(2, 3, 0, 2, -2)), fDance2 = Add(Pose(2, 1, 1, 0, 2, false, 1));
		int fStretch = Add(Pose(1, 1, 0, 3, 0, true));
		var cu = Pose(4, 0, 0, 0, 1); Bubble(cu, 2, 0, question); int fCurious = Add(cu);
		var slip = Empty(); FortuneSlip(slip, 2, 22); int fSlip = Add(slip);
		var ah = Pose(4, 1, 0, 1); int fAh = Add(ah);
		var choo = Pose(1, 2, 0, 0, -2); choo.Dot(0, 26, Water).Dot(1, 29, Water).Dot(2, 24, Water); int fChoo = Add(choo);
		int fWave1 = Add(Pose(2, 3, 0, 2)), fWave2 = Add(Pose(2, 1));

		p.IconFrame = fHappy;

		// ------------------------------------------------------------ animations
		// the parts of a hop besides "walk" (1); after a scene he crouches first ("hop prepare"), then hops
		const int HopPrep = 50, HopLand = 51, HopBump = 52, LongPrep = 53;
		Anim Show(int id, string name, int ms, params (int f, int n)[] steps)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString());
			foreach (var (f, n) in steps) a.Seq(Enumerable.Repeat(f, n).ToArray());
			a.Then(100, HopPrep);
			return a;
		}
		Anim Loop2(int id, string name, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString()).Loop(loops, 0);
			a.Seq(frames); a.Then(100, HopPrep);
			return a;
		}
		Anim Child(int id, string name, string mx, string my, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move(mx, my).Speed(ms.ToString()).Loop(loops, 0); a.Seq(frames); return a;
		}
		Anim Then(Anim a, int next) { a.Next.Clear(); a.Then(100, next); return a; }
		int nextChild = 300;
		// a tongue scene: his animation id, its name, the prey, how many windows the tongue crosses, then what he does after
		void TongueScene(int id, string name, string prey, int n, int after, int watch = 14, bool miss = false)
		{
			var (par, ch) = Catch(prey, n, watch, miss);
			var a = p.A(id, name).Move("0", "0").Speed("80"); a.Seq(par.ToArray()); a.Then(100, after);
			for (int k = 1; k <= n; k++)
			{
				int cid = nextChild++;
				p.A(cid, name + " " + (k == n ? "prey" : "tongue " + k)).Move("0", "0").Speed("80").Seq(ch[k - 1].ToArray());
				p.Childs.Add((id, "imageX-imageW*" + k, "imageY", cid));
			}
		}

		p.Spawns.Add((1, 50, "screenW+10", "areaH-imageH", 1));
		p.Spawns.Add((2, 30, "random*(screenW-imageW-50)/100+25", "-imageH-20", 5));
		p.Spawns.Add((3, 20, "0", "areaH-imageH", 1));

		// He moves by hopping. One hop is three animations, so he only moves while he is in the air:
		// "hop prepare" (crouching, still) -> "walk" (in the air, moving) -> "hop land" (landing, still) -> the next hop or a scene.
		var prep = p.A(HopPrep, "hop prepare").Move("0", "0").Speed("60").Seq(fCrouch1, fCrouch2, fCrouch3, fCrouch3, fPush);
		prep.Then(100, 1).OnGravity(100, 5);
		var walk = p.A(1, "walk").Move("-4", "0").Speed("50").Seq(Arc(12, 11, 9).ToArray());
		walk.Then(100, HopLand).OnBorder(100, HopBump).OnBorder(30, 9, "window").OnGravity(100, 5);
		var land = p.A(HopLand, "hop land").Move("0", "0").Speed("50", "90").Seq(fTouch, fSquash2, fSquash1, fRebound, fSit, fSit);
		land.Then(400, HopPrep).Then(10, 2).Then(15, 11).Then(5, LongPrep).Then(6, 20).Then(4, 21).Then(4, 22).Then(3, 23).Then(3, 25).Then(3, 30)
			.Then(6, 16, "taskbar").Then(4, 13, "taskbar")
			.OnGravity(100, 5);
		// at a border he comes down where he is, lands and turns around
		p.A(HopBump, "hop bump").Move("0", "0").Speed("50", "90").Seq(fComeDown1, fComeDown2, fTouch, fSquash2, fSquash1, fSit).Then(100, 2).OnGravity(100, 5);
		var turn = p.A(2, "turn").Move("0", "0").Speed("130").Seq(fBack, fSit); turn.Action = "flip"; turn.Then(100, HopPrep);
		p.A(3, "drag").Move("0", "0").Speed("70").Loop("40").Seq(fAir, fAir, fWide, fWide).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("45").Loop("30").Seq(fBigLeap, fWide, fAir, fWide).Then(100, 4);
		p.A(5, "fall").Move("0", "2", "0", "8").Speed("80", "50").Loop("20").Seq(fFall1, fFall2).Then(100, 6).OnBorder(100, 7);
		p.A(6, "fall fast").Move("0", "9").Speed("45").Loop("10").Seq(fFall1, fFall2).Then(100, 6).OnBorder(100, 8);
		p.A(7, "land").Move("0", "0").Speed("55", "200").Seq(fTouch, fSquash3, fSquash2, fSquash1, fRebound, fSit, fHappy).Then(100, HopPrep);
		p.A(8, "fall hard").Move("0", "0").Speed("60", "260").Seq(fTouchWide, fSquash3, fSquash3, fDizzy0, fDizzy0, fDizzy0, fDizzy0, fBlink, fSit).Then(100, HopPrep);
		// onto a window: it starts in the air (from a hop) and the window itself goes up and down along the arc
		var hopUpArt = new[] { (6, LeapLegs, 1), (7, LeapLegs, 1), (8, LeapLegs, 1), (8, LeapLegs, 0), (8, FoldLegs, 0), (7, FoldLegs, -1),
			(6, FoldLegs, -1), (5, ReachLegs, -1), (4, ReachLegs, -1), (3, ReachLegs, -1), (2, ReachLegs, -1), (2, ReachLegs, -1) };
		p.A(9, "hop up").Move("-3", "-7", "-3", "7").Speed("50").Seq(hopUpArt.Select(s => Add(Hop(3, s.Item1, s.Item2, s.Item3, 0, 0, 1))).ToArray())
			.Then(100, HopLand).OnBorder(80, 7, "taskbar").OnBorder(40, 7, "window").OnBorder(20, 5, "horizontal+");
		p.A(10, "sync").Move("0", "0").Speed("120").Loop("6").Seq(fDance1, fDance2).Then(100, HopPrep);
		var hub = p.A(11, "blink").Move("0", "0").Speed("450", "120").Seq(fSit, fBlink, fSit, fBlink, fSit);
		hub.Then(30, HopPrep).Then(8, 15).Then(8, 20).Then(6, 21).Then(6, 22).Then(5, 23).Then(4, 24).Then(4, 25).Then(4, 26).Then(4, 27)
			.Then(4, 28).Then(3, 29).Then(3, 30).Then(3, 31).Then(3, 32).Then(3, 33).Then(3, 34).Then(3, 35).Then(3, 36).Then(3, 37).Then(3, 38).Then(3, 39);
		p.A(13, "sleep").Move("0", "0").Speed("800", "450").Loop("random/4+5", 2).Seq(fSit, fSleep1, fSleep1, fSleep2, fSleep3, fSleep2).Then(100, 14);
		p.A(14, "wake up").Move("0", "0").Speed("220", "160").Seq(fSleep1, fSit, fWide, fSit, fHappy, fSit).Then(100, HopPrep);
		p.A(15, "look around").Move("0", "0").Speed("280").Seq(fSit, fUp, fUp, fSit, fBack, fBack, fSit).Then(100, HopPrep);
		p.A(16, "sit").Move("0", "0").Speed("600", "400").Loop("random/3+4", 1).Seq(fSit, fSit, fSit, fSit).Then(60, HopPrep).Then(40, 13);
		p.A(17, "wave").Move("0", "0").Speed("170").Loop("4", 1).Seq(fSit, fWave1, fWave2, fWave1, fWave2).Then(100, HopPrep);
		// the long jump: a deeper crouch, then a long, high arc
		p.A(LongPrep, "long jump prepare").Move("0", "0").Speed("70").Seq(fCrouch1, fCrouch2, fCrouch3, fCrouch3, fCrouch3, fPush).Then(100, 18).OnGravity(100, 5);
		var longJump = p.A(18, "long jump").Move("-5", "0").Speed("45").Seq(Arc(16, 15, 3, 1).ToArray());
		longJump.Then(100, HopLand).OnBorder(100, HopBump).OnGravity(100, 5);
		// ================================================================ the tongue: every time he eats, it goes far
		Show(40, "gulp", 160, (fGulp, 3), (fSit, 1), (fGulp, 2), (fLick, 3), (fHappy, 3));
		TongueScene(20, "catch fly", "fly", 2, 40);
		TongueScene(21, "catch mosquito", "mosquito", 3, 40);
		TongueScene(22, "catch moth", "moth", 1, 40);
		TongueScene(23, "catch dragonfly", "dragonfly", 2, 40);
		Show(41, "firefly glow", 200, (fGulp, 3), (fGlow1, 1), (fGlow2, 1), (fGlow1, 1), (fGlow2, 1), (fGlow1, 1), (fGlow2, 1), (fGlow1, 2), (fHappy, 3));
		TongueScene(24, "catch firefly", "firefly", 2, 41);
		Show(42, "bee sting", 140, (fGulp, 2), (fWide, 2), (fStung, 10), (fCry1, 3), (fCry2, 3), (fDizzyA, 2), (fDizzyB, 2), (fSit, 1));
		TongueScene(25, "catch bee", "bee", 1, 42);
		TongueScene(26, "catch cricket", "cricket", 1, 40);
		Show(43, "missed", 160, (fWide, 3), (fUp, 6), (fAngry1, 2), (fAngry2, 2), (fAngry1, 2), (fBlink, 2), (fSit, 1));
		TongueScene(27, "miss a fly", "fly", 2, 43, 14, true);
		// the visitors of the program: the tongue goes straight up, the program pulls the visitor in
		var cv = p.A(19, "catch visitor").Move("0", "0").Speed("80").Seq(fUp, fWide, fUp1, fUp2, fUp3, fUp3, fUp3, fUp2, fUp1, fWide);
		cv.Then(100, 40);

		// ================================================================ frog scenes (with child windows)
		// croak: the throat swells, "CROAC" floats up
		Show(28, "croak", 160, (fSit, 1), (fSac1, 2), (fSac2, 6), (fSac1, 2), (fSac2, 6), (fSit, 2));
		Child(nextChild, "croak bubble", "-1", "-2", 100, "14", fCroakBubble).Fade(1, 0);
		p.Childs.Add((28, "imageX", "imageY", nextChild++));
		// another frog answers him from in front, then hops away
		Show(29, "frog chorus", 180, (fSac2, 6), (fSit, 4), (fUp, 2), (fSit, 4), (fSac2, 6), (fHappy, 6), (fSac2, 4), (fSit, 2));
		int chorus = nextChild++;
		Child(chorus, "friend croaks", "0", "0", 180, "0", fFriend, fFriend, fFriend, fFriend, fFriend, fFriend, fFriend, fFriend, fFriend, fFriend,
			fFriendCroak, fFriendCroak, fFriendCroak, fFriendCroak, fFriendCroak, fFriendCroak, fFriend, fFriend, fFriend, fFriend, fFriend, fFriend, fFriendCroak, fFriendCroak, fFriendCroak, fFriendCroak);
		int chorusAway = nextChild++;
		Then(p.Anims.Last(), chorusAway);
		Child(chorusAway, "friend hops away", "-5", "-2", 90, "40", fFriend, fFriend);
		p.Childs.Add((29, "imageX-imageW*1.5", "imageY", chorus));
		// soap bubbles from his mouth
		Show(30, "bubbles", 220, (fBlow, 3), (fSit, 2), (fBlow, 3), (fHappy, 3), (fUp, 4), (fSit, 1));
		int b1 = nextChild++, b2 = nextChild++, bPop = nextChild++;
		Child(b1, "bubble up", "-1", "-2", 110, "14", fBubble); Then(p.Anims.Last(), bPop);
		Child(b2, "bubble up 2", "-2", "-3", 120, "12", fBubble); Then(p.Anims.Last(), bPop);
		Child(bPop, "bubble pop", "0", "0", 150, "0", fBubblePop);
		p.Childs.Add((30, "imageX", "imageY", b1));
		p.Childs.Add((30, "imageX-imageW*0.2", "imageY-10", b2));
		// frog prince: a kiss, and a crown falls on his head
		Show(31, "frog prince", 160, (fSit, 2), (fKiss, 4), (fLove1, 3), (fUp, 4), (fWide, 1), (fPrince, 10), (fHappy, 2));
		int crown = nextChild++;
		Child(crown, "crown falls", "0", "6", 60, "12", fCrownFalls);
		p.Childs.Add((31, "imageX", "imageY-imageH*0.6", crown));
		// a rain cloud: he loves it and dances
		Show(32, "rain dance", 140, (fUp, 4), (fHappy, 3), (fDance1, 2), (fDance2, 2), (fDance1, 2), (fDance2, 2), (fDance1, 2), (fDance2, 2), (fHappy, 3));
		int rain = nextChild++, rainAway = nextChild++;
		Child(rain, "cloud rains", "0", "0", 150, "12", fCloud1, fCloud2); Then(p.Anims.Last(), rainAway);
		Child(rainAway, "cloud leaves", "-3", "-1", 100, "12", fCloud1, fCloud2).Fade(1, 0);
		p.Childs.Add((32, "imageX", "imageY", rain));
		// his tadpoles in a puddle in front of him
		Show(33, "tadpoles", 260, (fSit, 2), (fBack, 2), (fProud, 6), (fHappy, 4), (fProud, 4));
		int tad = nextChild++;
		Child(tad, "tadpoles swim", "0", "0", 260, "8", fTad1, fTad2).Fade(1, 0);
		p.Childs.Add((33, "imageX-imageW*0.8", "imageY", tad));
		Show(34, "lick eye", 150, (fSit, 1), (fLickEye, 3), (fBlink, 1), (fLickEye, 3), (fHappy, 2));
		Show(35, "yawn", 220, (fSit, 1), (fYawnTongue, 6), (fBlink, 2), (fSit, 1));
		Loop2(36, "dance", 130, "random/3+5", fDance1, fDance2);
		Show(37, "sneeze", 140, (fSit, 1), (fAh, 3), (fSit, 1), (fAh, 4), (fChoo, 3), (fBlink, 2), (fSit, 1));
		Loop2(38, "laugh", 130, "random/3+4", fLaugh1, fLaugh2);
		Loop2(39, "curious", 320, "random/3+3", fCurious, fUp);

		// ================================================================ the program's features (60-110)
		Loop2(60, "think", 320, "random/3+3", fThink1, fThink1, fThink2, fThink2);
		Show(61, "idea", 160, (fThink1, 4), (fIdea1, 3), (fIdea2, 1), (fIdea1, 1), (fIdea2, 6), (fHappy, 3));
		Child(nextChild, "idea bulb", "0", "-2", 90, "14", fBulb).Fade(1, 0); p.Childs.Add((61, "imageX", "imageY", nextChild++));
		Loop2(62, "read", 420, "random/3+3", fRead1, fRead1, fRead2, fRead1);
		Loop2(63, "review", 320, "random/3+3", fRead1, fRead2);
		Loop2(64, "laptop", 140, "random/3+6", fLap1, fLap2);
		Child(nextChild, "code floats", "-1", "-2", 110, "16", fCode).Fade(1, 0); p.Childs.Add((64, "imageX", "imageY", nextChild++));
		Loop2(65, "loading", 140, "random/3+5", fLoad1, fLoad2, fLoad3, fLoad4);
		Show(66, "download", 150, (fUp, 7), (fDlCatch, 6), (fHappy, 3));
		Child(nextChild, "download box", "0", "8", 60, "17", fDlBox); p.Childs.Add((66, "imageX", "imageY-imageH*2", nextChild++));
		Show(67, "thumbs up", 200, (fSit, 1), (fThumbs, 8), (fHappy, 2));
		Show(68, "confetti", 70, (fCrouch1, 1), (fCrouch2, 1), (fCrouch3, 2), (fPushUp, 1), (fPopUp, 1), (fPopped, 10), (fPopDown, 1), (fTouchUp, 1),
			(fSquash2, 1), (fSquash1, 1), (fHappy, 5));
		Child(nextChild, "confetti falls", "0", "2", 120, "20", fConf1, fConf2).Fade(1, 0); p.Childs.Add((68, "imageX-imageW*0.4", "imageY-imageH*0.3", nextChild++));
		Child(nextChild, "confetti falls 2", "1", "2", 140, "18", fConf2, fConf1).Fade(1, 0); p.Childs.Add((68, "imageX+imageW*0.4", "imageY-imageH*0.4", nextChild++));
		Show(69, "error", 130, (fWide, 2), (fError, 8), (fScared1, 2), (fScared2, 2), (fBlink, 2), (fSit, 1));
		Child(nextChild, "warning up", "0", "-2", 100, "12", fWarn).Fade(1, 0); p.Childs.Add((69, "imageX", "imageY", nextChild++));
		Loop2(70, "cry", 220, "random/3+4", fCry1, fCry2);
		Loop2(71, "angry", 110, "random/3+6", fAngry1, fAngry2);
		Child(nextChild, "angry steam", "1", "-2", 100, "12", fSteam).Fade(1, 0); p.Childs.Add((71, "imageX", "imageY", nextChild++));
		Show(72, "shocked", 110, (fSit, 2), (fShocked, 8), (fBlink, 2), (fSit, 1));
		Loop2(73, "scared", 70, "random/3+10", fScared1, fScared2);
		var talk = p.A(74, "talk").Move("0", "0").Speed("160").Loop("random/3+8", 0);
		talk.Seq(fTalk, fSit, fTalk, fHappy, fSit, fSit); talk.Then(100, HopPrep);
		Loop2(75, "umbrella", 220, "random/3+5", fUmb1, fUmb2);
		var pudJump = new List<int> { fPud0, fPud0, fPud0, fPud0, fPud0, fPud0, fPud0, fPud0 };
		foreach (int sq in new[] { 1, 2, 3, 3 }) { var c = Hop(10, 0, CrouchLegs, 0, sq); Puddle(c, 0, 14); pudJump.Add(Add(c)); }
		var pp = Hop(10, 2, PushUpLegs, 0, -1); Puddle(pp, 0, 14); pudJump.Add(Add(pp));
		pudJump.AddRange(Arc(10, 9, 2, 2, false, f => Puddle(f, 0, 14)));
		p.A(76, "puddle splash").Move("0", "0").Speed("70").Seq(pudJump.ToArray());
		Then(p.Anims.Last(), 77);
		Show(77, "splash", 140, (fPudSplash, 6), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		Child(nextChild, "splash left", "-3", "-1", 80, "8", fSplash).Fade(1, 0); p.Childs.Add((77, "imageX", "imageY", nextChild++));
		Child(nextChild, "splash right", "3", "-1", 80, "8", fSplash).Fade(1, 0); p.Childs.Add((77, "imageX+imageW*0.3", "imageY", nextChild++));
		Loop2(78, "hot", 240, "random/3+4", fHot1, fHot2);
		Loop2(79, "hand fan", 120, "random/3+8", fFan1, fFan2);
		Loop2(80, "sunbathing", 500, "random/3+3", fSunbathe1, fSunbathe1, fSunbathe2);
		Show(81, "ice cream", 200, (fIce1, 3), (fIce2, 3), (fIce1, 2), (fIce2, 3), (fLick, 3), (fHappy, 2));
		Loop2(82, "cold", 70, "random/3+10", fCold1, fCold2);
		Loop2(83, "shiver", 70, "random/3+8", fCold1, fCold2);
		Loop2(84, "scarf", 360, "random/3+3", fScarf1, fScarf2);
		Loop2(85, "snow cap", 320, "random/3+4", fBeanie1, fBeanie2);
		Child(nextChild, "snowfall", "0", "1", 200, "16", fSnowC1, fSnowC2).Fade(1, 0); p.Childs.Add((85, "imageX-imageW*0.5", "imageY-imageH*0.2", nextChild++));
		Show(86, "rain cloud", 160, (fUp, 6), (fHappy, 4), (fDance1, 2), (fDance2, 2), (fDance1, 2), (fDance2, 2), (fHappy, 2));
		p.Childs.Add((86, "imageX", "imageY", rain));
		Show(87, "look up", 280, (fSit, 1), (fUp, 6), (fBlink, 1), (fUp, 4), (fSit, 1));
		Show(88, "surprised", 120, (fSit, 2), (fSurprised, 6), (fWide, 3), (fSit, 1));
		Loop2(89, "tired", 400, "random/3+3", fTired1, fTired2);
		Show(90, "sleepy", 300, (fTired1, 3), (fTired2, 3), (fSleepy2, 4), (fWide, 1), (fTired1, 3), (fSleepy2, 5), (fBlink, 2), (fSit, 1));
		// eating and drinking: always with the long tongue
		TongueScene(91, "drink", "drop", 1, 40);
		TongueScene(92, "eat", "fly", 2, 40);
		Show(94, "fortune", 200, (fGulp, 2), (fWide, 3), (fUp, 5), (fHappy, 3));
		Child(nextChild, "fortune slip", "-1", "-2", 110, "14", fSlip).Fade(1, 0); p.Childs.Add((94, "imageX", "imageY", nextChild++));
		TongueScene(93, "cookie", "cookie", 1, 94);
		Loop2(95, "sweat", 160, "random/3+5", fSweat1, fSweat2);
		Loop2(96, "dizzy", 130, "random/3+5", fDizzyA, fDizzyB, fDizzyC);
		Loop2(97, "spin", 90, "random/3+3", fSit, fBack, fMirror, fBack);
		Show(98, "nod", 180, (fSit, 1), (fNod, 2), (fSit, 2), (fNod, 2), (fHappy, 2));
		Show(99, "ok", 200, (fSit, 1), (fOk, 8), (fHappy, 2));
		var cheer = new List<int> { Add(Hop(2, 0, CrouchLegs, 0, 1, 0, 3)), Add(Hop(2, 0, CrouchLegs, 0, 2, 0, 3)), Add(Hop(2, 2, PushUpLegs, 0, -1, 0, 2)) };
		cheer.AddRange(Arc(8, 8, 2, 2, false));
		cheer.AddRange(new[] { Add(Hop(2, 1, ReachLegs, 0, 0, 0, 2)), Add(Hop(2, 0, LandLegs, 0, 2, 0, 3)), fHappy });
		Loop2(100, "cheer", 60, "random/3+4", cheer.ToArray());
		Loop2(101, "party", 160, "random/3+5", fParty1, fParty2);
		var jump = p.A(102, "jump").Move("0", "0").Speed("55").Seq(fCrouch1, fCrouch2, fCrouch3, fCrouch3, fPushUp).Seq(Arc(14, 14, 3, 1, false).ToArray());
		jump.Seq(fTouchUp, fSquash3, fSquash2, fSquash1, fRebound, fHappy, fHappy, fHappy, fHappy, fHappy).Then(100, HopPrep);
		Loop2(103, "happy", 140, "random/3+4", fHappy, fHappy2);
		Loop2(104, "hug", 320, "random/3+3", fHug1, fHug2);
		Loop2(105, "hearts", 260, "random/3+3", fLove1, fLove2);
		Child(nextChild, "heart up", "-1", "-2", 100, "14", fHeartChild).Fade(1, 0); p.Childs.Add((105, "imageX", "imageY", nextChild++));
		Child(nextChild, "heart up 2", "1", "-2", 120, "12", fHeartChild).Fade(1, 0); p.Childs.Add((105, "imageX+imageW*0.4", "imageY", nextChild++));
		Loop2(106, "love", 260, "random/3+3", fLove1, fLove2);
		Loop2(107, "sweep", 160, "random/3+6", fSweep1, fSweep2);
		Child(nextChild, "sweep dust", "-2", "-1", 120, "12", fDust).Fade(1, 0); p.Childs.Add((107, "imageX", "imageY", nextChild++));
		Loop2(108, "work", 150, "random/3+6", fWork1, fWork2);
		Loop2(109, "music", 200, "random/3+6", fMusic1, fMusic2);
		Child(nextChild, "note up", "-1", "-2", 110, "20", fNote, fNote).Fade(1, 0); p.Childs.Add((109, "imageX", "imageY", nextChild++));
		Child(nextChild, "note up 2", "1", "-2", 130, "18", fNote, fNote).Fade(1, 0); p.Childs.Add((109, "imageX+imageW*0.3", "imageY-10", nextChild++));
		Show(110, "coffee", 240, (fCoffee1, 3), (fCoffee2, 3), (fCoffee1, 2), (fCoffee2, 3), (fHappy, 4));
		Child(nextChild, "coffee steam", "0", "-1", 140, "12", fCoffeeSteam).Fade(1, 0); p.Childs.Add((110, "imageX", "imageY", nextChild++));
		Show(111, "stretch", 220, (fSit, 1), (fStretch, 5), (fWave1, 2), (fSit, 1));
		foreach (int id in new[] { 60, 62, 64, 75, 79, 80, 92, 93, 101, 104, 105, 107, 109, 110, 111 }) hub.Then(2, id);

		// 49 kill: one big leap up and away
		// (it starts slowly and goes faster and faster; after the last frame the program keeps moving it and shows the last one)
		var kill = p.A(49, "kill").Move("0", "0", "-9", "-16").Speed("70").Fade(1, 0).Seq(fWave1, fWave2, fCrouch2, fCrouch3, fPush);
		kill.Seq(Add(Hop(2, 2, TakeoffLegs, 1, -1)), Add(Hop(2, 4, LeapLegs, 1, -1)), Add(Hop(2, 6, LeapLegs, 1)), Add(Hop(2, 7, LeapLegs, 1)), Add(Hop(2, 8, LeapLegs, 1)));
		kill.Loop("0", kill.Frames.Count - 1).Then(100, 49);

		p.Readme = """
			# Cururu the Frog

			## Description

			A green frog in pixel art with big eyes, a light belly and a long, long tongue.

			He moves by hopping. Every time he eats, his tongue shoots out really far, across
			the screen in front of him, sticks to the insect and pulls it back: flies, mosquitoes,
			moths, dragonflies, fireflies (and then his belly glows), a cricket, a bee (ouch!).
			With the Visitors plugin, the butterflies, birds, bees and ladybugs that come to
			visit him end up the same way.

			## About pet

			Author: Luan Felipe

			## Animations

			* hop, long jump, turn, hop onto windows, fall and land, sit and sleep
			* a smooth hop: he crouches, pushes off, flies along an arc with his legs stretched out, reaches for the
			  ground with his front feet and lands with a squash
			* catch a fly, a mosquito, a moth, a dragonfly, a firefly, a cricket, a bee; miss a fly
			* catch the visitors of the Visitors plugin with the tongue
			* croak, frog chorus, bubbles, frog prince, rain dance, tadpoles, lick his eye, yawn
			* reacts to the app: thinks, reads, types on a laptop and celebrates with Claude Code; drinks water on the
			  water reminder; leaf umbrella, fan, lily pad and scarf with the weather; party at the end of a Pomodoro
			* drag and toss with the mouse
			""";
		p.ReadmePt = """
			# Cururu, o Sapo

			## Descrição

			Um sapo verde em pixel art, com olhos grandes, barriga clara e uma língua comprida, comprida.

			Ele anda pulando. Sempre que come, a língua espicha bem longe, atravessa a tela
			na frente dele, gruda no inseto e volta: mosca, pernilongo, mariposa, libélula,
			vaga-lume (e a barriga dele acende), grilo, abelha (ai!). Com o plugin de Visitas,
			as borboletas, pássaros, abelhas e joaninhas que vêm visitá-lo acabam do mesmo jeito.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* pular, salto longo, virar, subir nas janelas, cair e pousar, sentar e dormir
			* um pulo suave: ele se agacha, toma impulso, voa num arco com as pernas esticadas, estica as patas da
			  frente para o chão e pousa amassadinho
			* pegar mosca, pernilongo, mariposa, libélula, vaga-lume, grilo, abelha; errar uma mosca
			* pegar com a língua as visitas do plugin de Visitas
			* coaxar, coral de sapos, bolhas, sapo príncipe, dança da chuva, girinos, lamber o olho, bocejar
			* reage ao programa: pensa, lê, digita no notebook e comemora com o Claude Code; bebe água no lembrete
			  de água; guarda-chuva de folha, leque, vitória-régia e cachecol conforme o clima; festa no fim do Pomodoro
			* arrastar e jogar com o mouse
			""";

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
