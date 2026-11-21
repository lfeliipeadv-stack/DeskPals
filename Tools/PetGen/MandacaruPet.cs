using System.Drawing;

namespace PetGen;

/// <summary>
/// "Mandacaru" — the cactus of the Brazilian sertão, with a leather hat (chapéu de couro) and root feet.
/// A tall trunk with a face and two arms raised like a mandacaru. He never climbs the screen borders: when he walks
/// into one he turns around. He is put together from parts (trunk, arms, eyes, mouth, hat, root feet) and every pose
/// can lean (the upper body sways), squash or stretch, so the moves are made of small steps.
/// Many scenes of the sertão use child windows: the forró with his sanfona (notes float up), the triangle and the
/// zabumba, the strong sun with heat waves, the long-awaited rain (a cloud comes and the white mandacaru flowers bloom
/// on his arms), the full moon (flowers open at night and a bat visits them), the asa-branca flying by, the carcará
/// circling, a calango doing push-ups, a jegue walking by, a goat that tries to eat him, a galo-de-campina landing on
/// his arm, a São João bonfire, party flags, a toy balloon that pops on his spines, a dust devil that steals his hat...
/// Like every DesktopPet pet he faces left. Frames are 36 px wide with 8 rows of headroom; the root feet stand on row 35.
/// A child placed with "imageX-imageW*n" appears in front of him; when he faces right the program mirrors the
/// position and the movement, so the scenes work on both sides.
/// </summary>
static class MandacaruPet
{
	const int W = 36, Head = 8;
	const int BX = 6, BY = 11;   // where the body sits: its local (0, 0); local row 24 (the root feet) is the frame's row 35
	const int Ground = 35;
	const char Out = 'O', Grn = 'G', GrnLight = 'g', GrnDark = 'D', Eye = 'E', White = 'W', Cheek = 'P', Mouth = 'K', Inside = 'r', Tongue = 't';
	const char Tan = 'y', Brown = 'N', BrownDark = 'n', Spine = 'S', Gold = 'X', Yel = 'Y', Orange = 'o', Red = 'R', Blue = 'B', BlueLight = 'b';
	const char Water = 'A', Cloud = 'c', Gray = 'C', GrayDark = 'k', Donkey = 'j', Moon = 'M', Zz = 'Z', Note = 'q', Heart = 'H', HeartLight = 'h';
	const char Line = 'l', Paper = 'T', Purple = 'u', Olive = 'v', Sand = 's';

	public static PetDef Build()
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		var p = new PetDef
		{
			Folder = "mandacaru",
			Title = "Mandacaru the Cactus",
			PetName = "Mandacaru",
			Author = "Luan Felipe",
			Version = "1.0",
			Info = "Mandacaru is a cactus of the Brazilian sertão with a leather hat.[br]" +
				   "He plays forró on his sanfona, dances xote, celebrates the rain when his white flowers bloom,[br]" +
				   "waves to the asa-branca and greets the jegue, the calango and the galo-de-campina.",
			Palette = new Dictionary<char, Color>
			{
				[Out] = Color.FromArgb(0x1E, 0x46, 0x28), [Grn] = Color.FromArgb(0x56, 0xA0, 0x50), [GrnLight] = Color.FromArgb(0x86, 0xC8, 0x6E),
				[GrnDark] = Color.FromArgb(0x3E, 0x7A, 0x3C), [Eye] = Color.FromArgb(0x14, 0x14, 0x14), [White] = Color.FromArgb(255, 255, 255),
				[Cheek] = Color.FromArgb(0xFF, 0x9D, 0xB0), [Mouth] = Color.FromArgb(0x3A, 0x1E, 0x1E), [Inside] = Color.FromArgb(0xA8, 0x32, 0x4A),
				[Tongue] = Color.FromArgb(0xEE, 0x72, 0x88),
				[Tan] = Color.FromArgb(0xC8, 0x96, 0x5A), [Brown] = Color.FromArgb(0x8C, 0x5A, 0x30), [BrownDark] = Color.FromArgb(0x5C, 0x38, 0x18),
				[Spine] = Color.FromArgb(0xF4, 0xF0, 0xDC), [Gold] = Color.FromArgb(0xFF, 0xD8, 0x4A), [Yel] = Color.FromArgb(0xF2, 0xC2, 0x30),
				[Orange] = Color.FromArgb(0xF0, 0x8A, 0x24), [Red] = Color.FromArgb(0xD8, 0x3A, 0x3A), [Blue] = Color.FromArgb(0x3C, 0x78, 0xD8),
				[BlueLight] = Color.FromArgb(0x9C, 0xCB, 0xF2), [Water] = Color.FromArgb(0x6E, 0xB4, 0xF0),
				[Cloud] = Color.FromArgb(0xE6, 0xEA, 0xF0), [Gray] = Color.FromArgb(0x9A, 0xA0, 0xAA), [GrayDark] = Color.FromArgb(0x5E, 0x5A, 0x58),
				[Donkey] = Color.FromArgb(0x9A, 0x92, 0x8A), [Moon] = Color.FromArgb(0xFF, 0xF4, 0xC0), [Zz] = Color.FromArgb(0x96, 0xAA, 0xF0),
				[Note] = Color.FromArgb(0x78, 0x54, 0xDC), [Heart] = Color.FromArgb(0xE8, 0x3E, 0x62), [HeartLight] = Color.FromArgb(0xFF, 0x96, 0xAA),
				[Line] = Color.FromArgb(0x78, 0x78, 0x7E), [Paper] = Color.FromArgb(0xEA, 0xDB, 0xB8), [Purple] = Color.FromArgb(0x9B, 0x59, 0xB6),
				[Olive] = Color.FromArgb(0x8A, 0x8A, 0x3A), [Sand] = Color.FromArgb(0xD9, 0xB7, 0x7A),
			},
			Scale = 2,
			TilesX = 8,
		};

		// ------------------------------------------------------------ helpers
		void St(Frame f, int x, int y, params string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		string[] Mir(string[] rows) => rows.Select(r => new string(r.Reverse().ToArray())).ToArray();
		string[] Recolor(string[] rows, char from, char to) => rows.Select(r => r.Replace(from, to)).ToArray();
		Frame Empty() => new Frame();
		// frames that look the same are stored once
		var seen = new Dictionary<string, int>();
		int Add(Frame f) { string k = f.ToString(); if (!seen.TryGetValue(k, out int i)) { i = p.Add(f); seen[k] = i; } return i; }

		// ------------------------------------------------------------ the body parts (local coordinates: the body's (0, 0) is (BX, BY))
		// the leather hat (chapéu de couro): a brown crown with the tan brim turned up in front like a half moon, a gold star
		string[] HatFront =
		{
			"......OOOOOO......",
			"O...OOnNNNNNOO...O",
			"OO.ONNNNNNNNNNO.OO",
			"OyOONNNNNNNNNNOOyO",
			"OyyyOOONNNNOOOyyyO",
			".OyyyyyOOOOyyyyyO.",
			"..OOyyyyXXyyyyOO..",
			"....OOOOOOOOOO....",
		};
		string[] HatBack =
		{
			"......OOOOOO......",
			"....OOnNNNNNOO....",
			"...ONNNNNNNNNNO...",
			"O..ONNNNNNNNNNO..O",
			"OOOONNNNNNNNNNOOOO",
			".OyyOOOOOOOOOOyyO.",
			"..OOyyyyyyyyyyOO..",
			"....OOOOOOOOOO....",
		};
		// the hat held in a hand, seen from the side (10 x 6)
		string[] HatSide =
		{
			"...OOOO...",
			"..OnNNNO..",
			"O.ONNNNO.O",
			"OyOOOOOOyO",
			".OyyXyyyO.",
			"..OOOOOO..",
		};
		// arms: 16 rows (local rows 5..20) x 11 columns (local columns -3..7); the right arm is the mirror image
		string[] E11(int n) => Enumerable.Repeat("...........", n).ToArray();
		string[] Rows(params string[] r) => r;
		var arms = new Dictionary<int, string[]>
		{
			[0] = E11(3).Concat(Rows(".....OO....", "....OGGO...", "....OGgO...", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // up
			[1] = E11(3).Concat(Rows("....OO.....", "...OGGO....", "...OGgO....", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // up, tip out
			[2] = E11(3).Concat(Rows("......OO...", ".....OGGO..", ".....OGgO..", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // up, tip in
			[3] = E11(1).Concat(Rows(".....OO....", "....OGGO...", "....OGgO...", "....OGGO...", "....OGgO...", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // high
			[4] = E11(8).Concat(Rows("......OOOOO", ".....OGGGGG", "....OGGOOOO", "....OGgO...", "....OGGO...", "....OGgO...", "....OGGO...", ".....OO....")).ToArray(),   // down (drooping)
			[5] = E11(8).Concat(Rows("..OOOOOOOOO", ".OGGgGGGgGG", "..OOOOOOOOO")).Concat(E11(5)).ToArray(),   // out (horizontal)
			[6] = E11(3).Concat(Rows(".......OO..", "......OGGO.", ".....OGgO..", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // reach (the tip near the face)
			[7] = E11(1).Concat(Rows("....OO.....", "...OGGO....", "...OGgO....", "....OGGO...", "....OGgO...", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // high, tip out
			[8] = E11(1).Concat(Rows("......OO...", ".....OGGO..", ".....OGgO..", "....OGGO...", "....OGgO...", "....OGGO...", "....OGgO...", "....OGGOOOO", ".....OGGGGG", "......OOOOO")).Concat(E11(5)).ToArray(),   // high, tip in
			[9] = E11(8).Concat(Rows(".....OOOOOO", "....OGGGGGG", "...OGGOOOOO", "...OGgO....", "...OGGO....", "...OGgO....", "...OGGO....", "....OO.....")).ToArray(),   // down, out
			[10] = E11(16),   // hidden
			[11] = E11(8).Concat(Rows(".......OOOO", "......OGGGG", ".....OGGOOO", ".....OGgO..", ".....OGGO..", ".....OGgO..", ".....OGGO..", "......OO...")).ToArray(),   // down, in
		};
		// where the tip of each arm is (local x of its left pixel, local y of its top row), for flowers and things held
		var tips = new Dictionary<int, (int x, int y)> { [0] = (2, 8), [1] = (1, 8), [2] = (3, 8), [3] = (2, 6), [6] = (4, 8), [7] = (1, 6), [8] = (3, 6) };
		// light dots on the trunk (the arms have theirs in their shapes)
		var dots = new[] { (12, 13), (9, 16), (14, 17), (10, 19), (13, 21) };

		// eyes: 0 open, 1 closed, 2 happy ^ ^, 3 wide, 4 up, 5 half (tired), 6 angry, 7 teary, 8 dizzy x, 9 hearts,
		// 10 peaceful (u u), 11 stars, 12 down, 13 laughing > <, 14 wink, -1 none (seen from behind)
		void Eyes(Frame f, int mode, int look)
		{
			int x = BX, y = BY + 9;
			for (int side = 0; side < 2; side++)
			{
				int ex = x + (side == 0 ? 9 : 13) + look, ey = y;
				int c3 = x + (side == 0 ? 9 : 14);   // centre of the 3-wide eyes (symmetric)
				switch (mode)
				{
					case 0: f.Dot(ex, ey, White).Dot(ex + 1, ey, Eye).Rect(ex, ey + 1, 2, 1, Eye); break;
					case 1: f.Rect(ex, ey + 1, 2, 1, Eye); break;
					case 2: f.Dot(c3 - 1, ey + 1, Eye).Dot(c3, ey, Eye).Dot(c3 + 1, ey + 1, Eye); break;
					case 3: f.Rect(ex, ey - 1, 2, 3, Eye).Dot(ex, ey - 1, White); break;
					case 4: f.Dot(ex, ey - 1, White).Dot(ex + 1, ey - 1, Eye).Rect(ex, ey, 2, 1, Eye); break;
					case 5: f.Rect(ex, ey, 2, 1, Out).Rect(ex, ey + 1, 2, 1, Eye); break;
					case 6:
						if (side == 0) f.Dot(ex, ey - 1, Out).Dot(ex + 1, ey, Out).Dot(ex, ey, Eye).Rect(ex, ey + 1, 2, 1, Eye);
						else f.Dot(ex + 1, ey - 1, Out).Dot(ex, ey, Out).Dot(ex + 1, ey, Eye).Rect(ex, ey + 1, 2, 1, Eye);
						break;
					case 7: f.Dot(ex, ey, White).Dot(ex + 1, ey, Eye).Rect(ex, ey + 1, 2, 1, Eye).Dot(side == 0 ? ex : ex + 1, ey + 2, Water).Dot(side == 0 ? ex : ex + 1, ey + 3, Water); break;
					case 8: f.Dot(c3 - 1, ey - 1, Eye).Dot(c3 + 1, ey - 1, Eye).Dot(c3, ey, Eye).Dot(c3 - 1, ey + 1, Eye).Dot(c3 + 1, ey + 1, Eye); break;
					case 9: f.Dot(c3 - 1, ey, Heart).Dot(c3 + 1, ey, Heart).Rect(c3 - 1, ey + 1, 3, 1, Heart).Dot(c3, ey + 2, Heart); break;
					case 10: f.Dot(c3 - 1, ey, Eye).Dot(c3, ey + 1, Eye).Dot(c3 + 1, ey, Eye); break;
					case 11: f.Dot(c3, ey - 1, Gold).Rect(c3 - 1, ey, 3, 1, Gold).Dot(c3, ey, White).Dot(c3, ey + 1, Gold); break;
					case 12: f.Dot(ex, ey + 1, White).Dot(ex + 1, ey + 1, Eye).Rect(ex, ey + 2, 2, 1, Eye); break;
					case 13:
						if (side == 0) f.Dot(c3 - 1, ey - 1, Eye).Dot(c3, ey, Eye).Dot(c3 + 1, ey, Eye).Dot(c3 - 1, ey + 1, Eye);
						else f.Dot(c3 + 1, ey - 1, Eye).Dot(c3, ey, Eye).Dot(c3 - 1, ey, Eye).Dot(c3 + 1, ey + 1, Eye);
						break;
					case 14:
						if (side == 0) f.Dot(ex, ey, White).Dot(ex + 1, ey, Eye).Rect(ex, ey + 1, 2, 1, Eye);
						else f.Dot(c3 - 1, ey + 1, Eye).Dot(c3, ey, Eye).Dot(c3 + 1, ey + 1, Eye);
						break;
				}
			}
		}
		// mouth: 0 smile, 1 open (talking), 2 "O" (shocked, singing high), 3 grin, 4 frown, 5 laughing, 6 tongue out,
		// 7 wavy, 8 chattering teeth, 9 chewing, 10 kiss / whistle, -1 none
		void MouthAt(Frame f, int mode)
		{
			int x = BX, y = BY + 11;
			switch (mode)
			{
				case 0: f.Rect(x + 11, y, 2, 1, Mouth); break;
				case 1: f.Rect(x + 11, y, 2, 1, Mouth).Rect(x + 11, y + 1, 2, 1, Inside); break;
				case 2: St(f, x + 10, y, ".KK.", "KrrK", "KrrK", ".KK."); break;
				case 3: f.Dot(x + 10, y, Mouth).Dot(x + 13, y, Mouth).Rect(x + 11, y + 1, 2, 1, Mouth); break;
				case 4: f.Rect(x + 11, y, 2, 1, Mouth).Dot(x + 10, y + 1, Mouth).Dot(x + 13, y + 1, Mouth); break;
				case 5: St(f, x + 10, y, "KKKK", "KrrK", ".KK."); break;
				case 6: f.Rect(x + 11, y, 2, 1, Mouth).Dot(x + 12, y + 1, Tongue); break;
				case 7: f.Dot(x + 10, y + 1, Mouth).Dot(x + 11, y, Mouth).Dot(x + 12, y + 1, Mouth).Dot(x + 13, y, Mouth); break;
				case 8: St(f, x + 10, y, "KWWK", ".KK."); break;
				case 9: f.Rect(x + 11, y, 2, 2, Mouth); break;
				case 10: f.Dot(x + 12, y, Mouth); break;
			}
		}
		void FeetAt(Frame f, int mode)
		{
			int x = BX, y = BY + 23;
			switch (mode)
			{
				case 0: St(f, x + 5, y, ".nN", "nNN"); St(f, x + 16, y, "Nn.", "NNn"); break;   // standing
				case 1: St(f, x + 4, y, "nNN"); St(f, x + 16, y, "Nn.", "NNn"); break;          // the left (front) root lifted
				case 2: St(f, x + 5, y, ".nN", "nNN"); St(f, x + 17, y, "NNn"); break;          // the right root lifted
				case 3: St(f, x + 5, y, "nNN"); St(f, x + 16, y, "NNn"); break;                 // tucked (in the air)
				case 4: St(f, x + 4, y, ".nN", "nNN"); St(f, x + 17, y, "Nn.", "NNn"); break;   // wide
			}
		}
		void HatAt(Frame f, int kind, int dx, int dy)
		{
			if (kind == 1) St(f, BX + 3 + dx, BY + dy, HatFront);
			else if (kind == 2) St(f, BX + 3 + dx, BY + dy, HatBack);
		}
		// mandacaru flowers on the tips of the arms: 1 bud, 2 opening, 3 open (white, yellow heart)
		// draws something, then a thin outline of another colour around it where the frame is still empty
		void Outlined(Frame f, char oc, Action<Frame> draw)
		{
			var t = new Frame(); draw(t);
			for (int yy = t.MinY; yy < W; yy++) for (int xx = 0; xx < W; xx++)
			{
				if (t[xx, yy] == '.') continue;
				foreach (var (ox, oy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
					if (t[xx + ox, yy + oy] == '.' && f[xx + ox, yy + oy] == '.') f[xx + ox, yy + oy] = oc;
			}
			for (int yy = t.MinY; yy < W; yy++) for (int xx = 0; xx < W; xx++) if (t[xx, yy] != '.') f[xx, yy] = t[xx, yy];
		}
		void FlowerAt(Frame f, int x, int y, int stage)
		{
			// (x, y) = left pixel of the arm tip's top row
			Outlined(f, Gray, g =>
			{
				if (stage == 1) St(g, x, y - 2, "WW", "gg");
				if (stage == 2) St(g, x - 1, y - 3, ".WW.", "WWWW", ".gg.");
				if (stage == 3) St(g, x - 2, y - 4, "W.WW.W", ".WXXW.", "WWXXWW", ".WWWW.", "..gg..");
			});
		}
		void SpinesAt(Frame f, int mode)
		{
			if (mode == 0) return;
			int cx = BX + 11;
			var c = f.Clone();
			// rows 9, 11, 13 (beside the arms) and 16, 20 on the lower trunk: a squash (row 18 goes away) never puts two side by side
			var rowsOn = mode == 2 ? new[] { 8, 10, 12, 14, 16, 20 } : new[] { 9, 11, 13, 16, 20 };
			foreach (int ly in rowsOn)
			{
				int yy = BY + ly;
				for (int xx = 1; xx < W - 1; xx++)
				{
					if (c[xx, yy] != Out) continue;
					bool openL = c[xx - 1, yy] == '.' && c[xx - 2, yy] == '.' && c[xx - 3, yy] == '.';
					bool openR = c[xx + 1, yy] == '.' && c[xx + 2, yy] == '.' && c[xx + 3, yy] == '.';
					if (xx <= cx && openL) { f[xx - 1, yy] = Spine; if (mode == 2) f[xx - 2, yy] = Spine; }
					if (xx > cx && openR) { f[xx + 1, yy] = Spine; if (mode == 2) f[xx + 2, yy] = Spine; }
				}
			}
		}
		void Transform(Frame f, int sway, int squash, int up, int dx)
		{
			if (sway != 0)
			{
				var c = f.Clone();
				for (int yy = f.MinY; yy < W; yy++)
				{
					int local = yy - BY;
					int s = local <= 15 ? sway : local <= 18 ? sway / 2 : 0;
					if (s == 0) continue;
					for (int xx = 0; xx < W; xx++) f[xx, yy] = c[xx - s, yy];
				}
			}
			// squash: a row of the lower trunk goes away (everything above comes down); stretch: it is doubled
			for (int i = 0; i < squash; i++)
			{
				for (int yy = BY + 18; yy > f.MinY; yy--) for (int xx = 0; xx < W; xx++) f[xx, yy] = f[xx, yy - 1];
				for (int xx = 0; xx < W; xx++) f[xx, f.MinY] = '.';
			}
			for (int i = 0; i < -squash; i++)
				for (int yy = f.MinY; yy < BY + 18; yy++) for (int xx = 0; xx < W; xx++) f[xx, yy] = f[xx, yy + 1];
			if (up != 0 || dx != 0) f.Shift(dx, -up);
		}

		// a pose of Mandacaru. look moves the eyes (-1 = to the front/left, 1 = back); sway leans the upper body;
		// squash > 0 makes him shorter, < 0 taller (the root feet stay); up lifts him off the ground.
		// hat: 1 on (front), 2 seen from behind, 0 none; hatDy/hatDx move it (lifted, pulled over the eyes...).
		// held: things he holds (drawn before the lean/squash, so they move with him)
		Frame Pose(int eyes = 0, int look = 0, int mouth = 0, int armL = 0, int armR = 0, int feet = 0, int sway = 0, int squash = 0,
			int up = 0, int dx = 0, int hat = 1, int hatDx = 0, int hatDy = 0, int bloom = 0, bool blush = false, int spines = 1,
			bool cheeks = true, Action<Frame> held = null, Action<Frame> under = null, bool back = false)
		{
			if (bloom > 0 && armL == 0) armL = 1;
			if (bloom > 0 && armR == 0) armR = 1;
			var f = new Frame();
			int x = BX, y = BY;
			under?.Invoke(f);
			// trunk, rounded on top
			f.Rect(x + 9, y + 5, 6, 1, Out);
			f.Dot(x + 8, y + 6, Out).Rect(x + 9, y + 6, 6, 1, Grn).Dot(x + 15, y + 6, Out);
			for (int r = 7; r <= 21; r++) f.Dot(x + 7, y + r, Out).Rect(x + 8, y + r, 8, 1, Grn).Dot(x + 16, y + r, Out);
			f.Rect(x + 6, y + 22, 12, 1, Out);
			foreach (var (gx, gy) in dots) f.Dot(x + (back ? 23 - gx : gx), y + gy, GrnLight);
			FeetAt(f, feet);
			St(f, x - 3, y + 5, arms[armL]);
			St(f, x + 16, y + 5, Mir(arms[armR]));
			SpinesAt(f, spines);
			if (!back)
			{
				Eyes(f, eyes, look);
				if (cheeks)
				{
					f.Dot(x + 8, y + 11, Cheek).Dot(x + 15, y + 11, Cheek);
					if (blush) f.Dot(x + 9, y + 11, Cheek).Dot(x + 14, y + 11, Cheek);
				}
				MouthAt(f, mouth);
			}
			HatAt(f, hat, hatDx, hatDy);
			if (bloom > 0)
			{
				// the flowers lean out, away from the brim of the hat
				if (tips.TryGetValue(armL, out var tl)) FlowerAt(f, x + tl.x - 1, y + tl.y, bloom);
				if (tips.TryGetValue(armR, out var tr)) FlowerAt(f, x + 23 - tr.x - 1 + 1, y + tr.y, bloom);
			}
			held?.Invoke(f);
			Transform(f, sway, squash, up, dx);
			return f;
		}

		// ------------------------------------------------------------ small things
		void HeartAt(Frame f, int x, int y) => St(f, x, y, "HH.HH", "HhHHH", ".HHH.", "..H..");
		void SmallHeart(Frame f, int x, int y) => St(f, x, y, "H.H", "HHH", ".H.");
		void NoteAt(Frame f, int x, int y, int k = 0) => St(f, x, y, k == 0 ? new[] { ".qq", ".q.", "qq." } : new[] { "qqqq", "q..q", "q..q", "qq.qq" });
		void Drop(Frame f, int x, int y) => f.Dot(x, y, Water).Dot(x, y + 1, Water);
		void Zed(Frame f, int x, int y, bool big) => St(f, x, y, big ? new[] { "ZZZZ", "..Z.", ".Z..", "ZZZZ" } : new[] { "ZZZ", ".Z.", "ZZZ" });
		void Spark(Frame f, int x, int y) => St(f, x, y, ".X.", "XWX", ".X.");
		void Twinkle(Frame f, int x, int y) => f.Dot(x, y, Gold);
		void Sweat(Frame f, int x, int y) => St(f, x, y, ".b.", "bAb", "bWA", ".A.");
		void Steam(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { ".C.", "C..", ".C.", "..C" } : new[] { "C..", ".C.", "..C", ".C." });
		void Dust(Frame f, int x, int y) => St(f, x, y, "..ss..", ".sCCs.", "sCssCs", ".ssss.");
		void Stars(Frame f, int k)
		{
			var pts = new[] { (BX + 6, BY - 3), (BX + 11, BY - 5), (BX + 17, BY - 3), (BX + 11, BY - 1) };
			for (int i = 0; i < 4; i++) { var (sx, sy) = pts[(i + k) % 4]; if (i % 2 == 0) Spark(f, sx, sy); else Twinkle(f, sx + 1, sy + 1); }
		}
		// speech bubble with a 5-row text (letters from Glyphs), over his head, in front
		var font = new Dictionary<char, string[]>
		{
			['O'] = new[] { "EEE", "E.E", "E.E", "E.E", "EEE" }, ['I'] = new[] { "E", "E", "E", "E", "E" }, ['!'] = new[] { "E", "E", "E", ".", "E" },
			['X'] = new[] { "E.E", "E.E", ".E.", "E.E", "E.E" }, ['E'] = new[] { "EEE", "E..", "EE.", "E..", "EEE" }, ['K'] = new[] { "E.E", "E.E", "EE.", "E.E", "E.E" },
			['?'] = new[] { "EEE", "..E", ".E.", "...", ".E." }, ['A'] = new[] { ".E.", "E.E", "EEE", "E.E", "E.E" }, ['T'] = new[] { "EEE", ".E.", ".E.", ".E.", ".E." },
			['V'] = new[] { "E.E", "E.E", "E.E", "E.E", ".E." }, ['B'] = new[] { "EE.", "E.E", "EE.", "E.E", "EE." }, ['H'] = new[] { "E.E", "E.E", "EEE", "E.E", "E.E" },
			['M'] = new[] { "E...E", "EE.EE", "E.E.E", "E...E", "E...E" }, ['U'] = new[] { "E.E", "E.E", "E.E", "E.E", "EEE" }, ['P'] = new[] { "EE.", "E.E", "EE.", "E..", "E.." },
			['k'] = new[] { "E..", "E.E", "EE.", "E.E", "E.E" }, ['Ê'] = new[] { "EEE", "E..", "EE.", "E..", "EEE" }, ['R'] = new[] { "EE.", "E.E", "EE.", "E.E", "E.E" },
			['C'] = new[] { "EEE", "E..", "E..", "E..", "EEE" }, ['N'] = new[] { "E..E", "EE.E", "E.EE", "E..E", "E..E" }, ['Ô'] = new[] { "EEE", "E.E", "E.E", "E.E", "EEE" },
			['v'] = new[] { "....GG", "...GG.", "GG.GG.", ".GGG..", "..G..." },   // a check mark
			[' '] = new[] { ".", ".", ".", ".", "." },
		};
		string[] Text(string s)
		{
			var rows = new string[5];
			for (int r = 0; r < 5; r++) rows[r] = string.Join(".", s.Select(ch => font[ch][r]));
			return rows;
		}
		void Bubble(Frame f, string text, int x = 1, int y = -6)
		{
			var t = Text(text);
			int w = t[0].Length + 4;
			var rows = new List<string> { "." + new string('k', w - 2) + "." };
			for (int i = 0; i < 7; i++) rows.Add("k" + new string('W', w - 2) + "k");
			rows.Add("." + new string('k', w - 2) + ".");
			rows.Add(new string('.', w - 4) + "kWk"); rows.Add(new string('.', w - 3) + "k");
			St(f, x, y, rows.ToArray());
			St(f, x + 2, y + 2, t);
		}
		void Thought(Frame f, int k)
		{
			f.Dot(BX + 4, BY - 1, Gray);
			if (k >= 1) St(f, BX + 1, BY - 4, ".C.", "CcC", ".C.");
			if (k >= 2) St(f, BX - 4, BY - 10, "..CCCC..", ".CccccC.", "CccccccC", ".CccccC.", "..CCCC..");
		}

		// ------------------------------------------------------------ frames: the basics
		int fEmpty = Add(Empty());
		int fStand = Add(Pose());
		int fBreath = Add(Pose(squash: 1));
		int fBlink = Add(Pose(eyes: 1));
		int fHappy = Add(Pose(eyes: 2, mouth: 3));
		int fHappyUp = Add(Pose(eyes: 2, mouth: 5, armL: 7, armR: 7, squash: -1));
		int fWide = Add(Pose(eyes: 3, mouth: 1));
		int fUp = Add(Pose(eyes: 4));
		int fUpBack = Add(Pose(eyes: 4, look: 1));
		int fUpFront = Add(Pose(eyes: 4, look: -1));
		int fFront = Add(Pose(look: -1));
		int fBack = Add(Pose(look: 1));
		int fSmile = Add(Pose(mouth: 3));
		int fGrin = Add(Pose(eyes: 14, mouth: 3));
		int fFrontBlink = Add(Pose(eyes: 1, look: -1));

		// the walk (8 frames, 1 px per frame): he waddles from root to root, the tall body sways over the planted root
		// and the arms swing behind the lean
		int[] walk =
		{
			Add(Pose(look: -1, squash: 1)),
			Add(Pose(look: -1, feet: 2, sway: -1, armL: 2, armR: 1)),
			Add(Pose(look: -1, feet: 2, sway: -1, armL: 2, armR: 1, squash: -1)),
			Add(Pose(look: -1)),
			Add(Pose(look: -1, squash: 1)),
			Add(Pose(look: -1, feet: 1, sway: 1, armL: 1, armR: 2)),
			Add(Pose(look: -1, feet: 1, sway: 1, armL: 1, armR: 2, squash: -1)),
			Add(Pose(look: -1)),
		};
		int fStartWalk = Add(Pose(look: -1, squash: 1, armL: 1, armR: 1));
		int fStopLean = Add(Pose(look: -1, sway: 1, armL: 1, armR: 2));
		// the run: a hurried waddle with hops
		int[] run =
		{
			Add(Pose(look: -1, mouth: 1, squash: 1, armL: 1, armR: 2)),
			Add(Pose(look: -1, mouth: 1, feet: 2, sway: -1, armL: 2, armR: 1, up: 1)),
			Add(Pose(look: -1, mouth: 1, feet: 3, sway: -1, armL: 2, armR: 1, up: 2, squash: -1)),
			Add(Pose(look: -1, mouth: 1, squash: 1, armL: 2, armR: 1)),
			Add(Pose(look: -1, mouth: 1, feet: 1, sway: 1, armL: 1, armR: 2, up: 1)),
			Add(Pose(look: -1, mouth: 1, feet: 3, sway: 1, armL: 1, armR: 2, up: 2, squash: -1)),
		};
		// turn: front, middle, back (the program flips him at the end)
		int fTurnMid = Add(Pose(squash: 1, armL: 2, armR: 2));
		// bump into the border
		var bonk = Pose(eyes: 1, mouth: 4, sway: 1, squash: 1, hatDy: 1); Stars(bonk, 0); int fBonk = Add(bonk);
		var bonk2 = Pose(eyes: 8, mouth: 7, sway: -1); Stars(bonk2, 1); int fBonk2 = Add(bonk2);
		var bonk3 = Pose(eyes: 8, mouth: 7, sway: 1); Stars(bonk3, 2); int fBonk3 = Add(bonk3);
		int fBonkLean = Add(Pose(look: -1, sway: -1, armL: 2, armR: 1));
		// dragged / tossed / falling
		int fDrag1 = Add(Pose(eyes: 3, mouth: 2, armL: 7, armR: 7, feet: 3, squash: -1, spines: 2));
		int fDrag2 = Add(Pose(eyes: 3, mouth: 1, armL: 8, armR: 8, feet: 3, squash: -1, spines: 2, sway: 1));
		int fDrag3 = Add(Pose(eyes: 3, mouth: 1, armL: 8, armR: 8, feet: 3, squash: -1, spines: 2, sway: -1));
		int fToss1 = Add(Pose(eyes: 8, mouth: 2, armL: 7, armR: 7, feet: 3, hatDy: -3, spines: 2));
		int fToss2 = Add(Pose(eyes: 8, mouth: 7, armL: 8, armR: 8, feet: 3, hatDy: -2, spines: 2, sway: 1));
		int fFall1 = Add(Pose(eyes: 3, mouth: 2, armL: 7, armR: 7, feet: 3, hatDy: -3));
		int fFall2 = Add(Pose(eyes: 3, mouth: 2, armL: 8, armR: 8, feet: 3, hatDy: -4));
		int fLand1 = Add(Pose(eyes: 1, mouth: 4, armL: 1, armR: 1, feet: 4, squash: 3, hatDy: 1));
		int fLand2 = Add(Pose(eyes: 1, mouth: 0, armL: 0, armR: 0, feet: 4, squash: 2, hatDy: 2));
		int fLand3 = Add(Pose(eyes: 1, mouth: 0, armL: 6, feet: 0, squash: 1, hatDy: 2));
		int fLand4 = Add(Pose(eyes: 3, mouth: 1, armL: 6, squash: -1, hatDy: -1));
		var fh1 = Pose(eyes: 8, mouth: 7, armL: 4, armR: 4, feet: 4, squash: 3, hat: 0); St(fh1, 0, Ground - 5, Recolor(HatSide, 'X', 'y')); Stars(fh1, 0); int fHard1 = Add(fh1);
		var fh2 = Pose(eyes: 8, mouth: 7, armL: 4, armR: 4, feet: 4, squash: 2, hat: 0, sway: 1); St(fh2, 0, Ground - 5, HatSide); Stars(fh2, 1); int fHard2 = Add(fh2);
		var fh3 = Pose(eyes: 8, mouth: 7, armL: 4, armR: 4, feet: 4, squash: 2, hat: 0, sway: -1); St(fh3, 0, Ground - 5, HatSide); Stars(fh3, 2); int fHard3 = Add(fh3);
		var fh4 = Pose(eyes: 1, armL: 4, armR: 0, squash: 2, hat: 0, look: -1, sway: -1); St(fh4, 0, Ground - 5, HatSide); int fHard4 = Add(fh4);
		int fHard5 = Add(Pose(eyes: 1, armL: 8, hat: 1, hatDy: -2));
		// idle: breathing, a gentle sway in the wind
		int fSwayL = Add(Pose(sway: -1, armL: 2, armR: 1));
		int fSwayR = Add(Pose(sway: 1, armL: 1, armR: 2));
		int fSwayL2 = Add(Pose(eyes: 10, sway: -1, armL: 2, armR: 1, mouth: 0));
		int fSwayR2 = Add(Pose(eyes: 10, sway: 1, armL: 1, armR: 2, mouth: 0));
		// sleep: the hat pulled over his eyes, arms down
		Frame SleepF(int z, int sq)
		{
			var f = Pose(eyes: 1, mouth: z == 2 ? 10 : 0, armL: 4, armR: 4, squash: sq, hatDy: 2, feet: 4);
			if (z >= 1) Zed(f, BX + 19, BY - 2, false);
			if (z >= 2) Zed(f, BX + 22, BY - 7, true);
			return f;
		}
		int fSleep0 = Add(SleepF(0, 2)), fSleep1 = Add(SleepF(1, 2)), fSleep2 = Add(SleepF(2, 3)), fSleep3 = Add(SleepF(1, 3));
		int fSit1 = Add(Pose(squash: 1, feet: 4)), fSit2 = Add(Pose(squash: 2, feet: 4, armL: 1, armR: 1)), fSit3 = Add(Pose(squash: 3, feet: 4)), fSitBlink = Add(Pose(eyes: 1, squash: 3, feet: 4));
		int fSitSway = Add(Pose(eyes: 10, squash: 3, feet: 4, sway: 1, armL: 1, armR: 2));
		int fWake1 = Add(Pose(eyes: 1, mouth: 2, armL: 6, armR: 4, squash: 2, hatDy: 1, feet: 4));
		int fWake2 = Add(Pose(eyes: 3, mouth: 1, armL: 6, armR: 0, squash: 1, hatDy: -1));
		// look around
		int fLookUpFront = Add(Pose(eyes: 4, look: -1, sway: -1, armL: 2, armR: 1));
		int fLookUpBack = Add(Pose(eyes: 4, look: 1, sway: 1, armL: 1, armR: 2));
		// wave
		int fWave1 = Add(Pose(eyes: 2, mouth: 3, armL: 7));
		int fWave2 = Add(Pose(eyes: 2, mouth: 5, armL: 8));
		int fWave3 = Add(Pose(eyes: 0, mouth: 3, armL: 7, look: -1));
		var hello1 = Pose(eyes: 2, mouth: 5, armL: 7); Bubble(hello1, "OI!", 0, -7); int fHello1 = Add(hello1);
		var hello2 = Pose(eyes: 2, mouth: 3, armL: 8); Bubble(hello2, "OI!", 0, -7); int fHello2 = Add(hello2);
		// jump (squash and stretch)
		int fCrouch = Add(Pose(eyes: 1, mouth: 0, armL: 4, armR: 4, squash: 2, feet: 4));
		int fTakeoff = Add(Pose(eyes: 2, mouth: 5, armL: 3, armR: 3, squash: -2, feet: 3, up: 2));
		int fAir1 = Add(Pose(eyes: 2, mouth: 5, armL: 3, armR: 3, squash: -1, feet: 3, up: 6, hatDy: 1));
		int fApex = Add(Pose(eyes: 2, mouth: 5, armL: 7, armR: 7, feet: 3, up: 8, hatDy: -1));
		int fAir2 = Add(Pose(eyes: 3, mouth: 1, armL: 7, armR: 7, squash: -1, feet: 3, up: 5, hatDy: -2));
		int fAir3 = Add(Pose(eyes: 3, mouth: 1, armL: 1, armR: 1, squash: -1, feet: 3, up: 2, hatDy: -2));
		int fTouch = Add(Pose(eyes: 1, mouth: 0, armL: 1, armR: 1, squash: 3, feet: 4, hatDy: 1));
		int fRebound = Add(Pose(eyes: 2, mouth: 3, squash: 1, feet: 0));
		// the back (for spinning) and the hat spinning in the air
		int fBackView = Add(Pose(hat: 2, back: true, cheeks: false));
		int fSide1 = Add(Pose(look: -1, sway: -1, armL: 2, armR: 1, squash: 1));
		int fSide2 = Add(Pose(look: 1, sway: 1, armL: 1, armR: 2, squash: 1));

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
		// a child that waits (invisible) n steps of ms, then plays the animation "next"
		int Wait(string name, int ms, int n, int next)
		{
			int id = nextChild++;
			var a = p.A(id, name + " (wait)").Move("0", "0").Speed(ms.ToString()).Seq(Enumerable.Repeat(fEmpty, n).ToArray());
			a.Then(100, next);
			return id;
		}

		p.Spawns.Add((1, 50, "screenW+10", "areaH-imageH", 1));
		p.Spawns.Add((2, 30, "random*(screenW-imageW-50)/100+25", "-imageH-20", 5));
		p.Spawns.Add((3, 20, "-imageW-10", "areaH-imageH", 1));

		var walkA = p.A(1, "walk").Move("-2", "0").Speed("80").Loop("random/10+3").Seq(walk);
		walkA.Then(40, 1).Then(28, 20).Then(10, 2).Then(6, 18).Then(16, 20)
			.OnBorder(85, 2).OnBorder(15, 9).OnGravity(100, 5);
		var turn = p.A(2, "turn").Move("0", "0").Speed("110").Seq(fFront, fTurnMid, fBack); turn.Action = "flip"; turn.Then(70, 19).Then(30, 11);
		p.A(3, "drag").Move("0", "0").Speed("90").Loop("40").Seq(fDrag1, fDrag1, fDrag2, fDrag1, fDrag1, fDrag3).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("50").Loop("30").Seq(fToss1, fToss2).Then(100, 4);
		p.A(5, "fall").Move("0", "2", "0", "8").Speed("90", "60").Loop("20").Seq(fFall1, fFall1, fFall2, fFall2).Then(100, 6).OnBorder(100, 7);
		p.A(6, "fall fast").Move("0", "9").Speed("50").Loop("10").Seq(fFall1, fFall2).Then(100, 6).OnBorder(100, 8);
		p.A(7, "land").Move("0", "0").Speed("90", "180").Seq(fLand1, fLand2, fLand2, fLand3, fLand3, fLand4, fStand, fHappy).Then(100, 11);
		p.A(8, "fall hard").Move("0", "0").Speed("140", "220").Seq(fHard1, fHard2, fHard3, fHard2, fHard3, fHard2, fHard4, fHard4, fHard5, fBlink, fStand).Then(100, 11);
		var bump = p.A(9, "bump").Move("0", "0").Speed("110").Seq(fBonkLean, fBonk, fBonk, fBonk2, fBonk3, fBonk2, fBonk3, fBlink, fStand);
		bump.Then(100, 2);
		p.A(10, "sync").Move("0", "0").Speed("130").Loop("6").Seq(fSwayL, fHappy, fSwayR, fHappy).Then(100, 11);
		var hub = p.A(11, "blink").Move("0", "0").Speed("380", "200").Seq(fStand, fBreath, fStand, fBlink, fStand, fBreath, fStand);
		p.A(12, "sway").Move("0", "0").Speed("260").Loop("random/20+3").Seq(fStand, fSwayL, fSwayL2, fSwayL, fStand, fSwayR, fSwayR2, fSwayR).Then(100, 11);
		p.A(13, "sleep").Move("0", "0").Speed("700", "500").Loop("random/4+6", 2).Seq(fSit2, fSleep0, fSleep0, fSleep1, fSleep2, fSleep3).Then(100, 14);
		p.A(14, "wake up").Move("0", "0").Speed("200", "150").Seq(fSleep0, fWake1, fWake1, fWake2, fWake2, fStand, fHappy, fStand).Then(100, 11);
		p.A(15, "look around").Move("0", "0").Speed("260").Seq(fStand, fFront, fFront, fLookUpFront, fFront, fStand, fBack, fBack, fLookUpBack, fBack, fStand).Then(100, 11);
		p.A(16, "sit").Move("0", "0").Speed("380", "500").Loop("random/10+4", 3).Seq(fSit1, fSit2, fSit3, fSit3, fSitBlink, fSit3, fSitSway).Then(70, 22).Then(30, 13);
		p.A(22, "stand up").Move("0", "0").Speed("160").Seq(fSit3, fSit2, fSit1, fStand).Then(100, 11);
		Loop2(17, "wave", 160, "random/10+4", fWave1, fWave2);
		var runA = p.A(18, "run").Move("-4", "0").Speed("70").Loop("random/10+6").Seq(run);
		runA.Then(100, 20).OnBorder(100, 2).OnGravity(100, 5);
		var startWalk = p.A(19, "start walk").Move("0", "0", "-2", "0").Speed("120", "80").Seq(fFront, fStartWalk, walk[1], walk[2]);
		startWalk.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		var stopWalk = p.A(20, "stop walk").Move("-2", "0", "0", "0").Speed("90", "180").Seq(walk[3], walk[4], fStopLean, fFront, fStand);
		stopWalk.Then(100, 11).OnBorder(100, 11).OnGravity(100, 5);
		Show(21, "hello", 160, (fHello1, 2), (fHello2, 2), (fHello1, 2), (fHello2, 2), (fHello1, 2), (fHappy, 3));
		hub.Then(30, 19).Then(8, 12).Then(5, 15).Then(4, 16).Then(4, 17);

		// 99 kill: tips his hat goodbye and fades away
		p.A(99, "kill").Move("0", "0").Speed("160").Fade(1, 0).Seq(fWave1, fWave2, fWave1, fHard5, fHappy, fHappy).Then(100, 99);

		// ============================================================ more helpers for the scenes
		void LineAt(Frame f, int x0, int y0, int x1, int y1, char c)
		{
			int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
			for (int i = 0; i <= n; i++)
				f.Dot(x0 + (int)Math.Round((x1 - x0) * (double)i / Math.Max(1, n)), y0 + (int)Math.Round((y1 - y0) * (double)i / Math.Max(1, n)), c);
		}
		Frame Draw(Action<Frame> a) { var f = Empty(); a(f); return f; }
		Frame With(Frame f, Action<Frame> a) { a(f); return f; }
		// a thing held up on the tip of the front arm (armL 1), or brought to the mouth (armL 6)
		void Item(Frame f, string[] art, bool atMouth)
		{
			int w = art.Max(r => r.Length), h = art.Length;
			if (atMouth) St(f, Math.Max(BX + 6, BX + 12 - w), BY + 11 - h / 2, art);
			else St(f, BX + 3 - w, BY + 8 - h, art);
		}
		// notes that float up from him (two child windows, the second one a moment later)
		var n1f = Empty(); NoteAt(n1f, BX + 1, BY + 12); int fNote1 = Add(n1f);
		var n2f = Empty(); NoteAt(n2f, BX + 19, BY + 10, 1); int fNote2 = Add(n2f);
		int noteA = nextChild++; Child(noteA, "note floats", "-1", "-3", 120, "14", fNote1, fNote1).Fade(1, 0);
		int noteB = nextChild++; Child(noteB, "note floats 2", "1", "-3", 130, "12", fNote2, fNote2).Fade(1, 0);
		void Notes(int parent, string label, int ms, int delay1, int delay2)
		{
			p.Childs.Add((parent, "imageX", "imageY", delay1 > 0 ? Wait(label + " note", ms, delay1, noteA) : noteA));
			p.Childs.Add((parent, "imageX", "imageY", delay2 > 0 ? Wait(label + " note 2", ms, delay2, noteB) : noteB));
		}

		// ============================================================ forró: the sanfona (accordion), then the xote
		void Sanfona(Frame f, int k)
		{
			int w = 14 + 2 * k, x0 = BX + 5 - k, y0 = BY + 15, bw = w - 8;
			string[] lb = { "OOOO", "OWWO", "OEWO", "OWWO", "OEWO", "OWWO", "OOOO" };
			string[] rb = { "OOOO", "ORRO", "OWRO", "ORRO", "OWRO", "ORRO", "OOOO" };
			var rows = new string[7];
			for (int r = 0; r < 7; r++)
			{
				string mid = r == 0 || r == 6
					? new string(Enumerable.Range(0, bw).Select(i => i % 2 == 0 ? 'O' : 'k').ToArray())
					: new string(Enumerable.Range(0, bw).Select(i => i % 2 == 0 ? 'R' : 'r').ToArray());
				rows[r] = lb[r] + mid + rb[r];
			}
			St(f, x0, y0, rows);
		}
		int[] sanArm = { 11, 4, 9 };
		Frame San(int k, int eyes = 2, int mouth = 3, int feet = 0, int look = 0, int up = 0) =>
			Pose(eyes: eyes, look: look, mouth: mouth, armL: sanArm[k], armR: sanArm[k], feet: feet, up: up, held: f => Sanfona(f, k));
		int[] kseq = { 0, 1, 2, 1 };
		var play = new List<int>();
		for (int i = 0; i < 24; i++) play.Add(Add(San(kseq[i % 4], eyes: (i / 8) % 2 == 0 ? 2 : 10, mouth: i % 4 < 2 ? 1 : 3, feet: i % 8 < 4 ? 1 : 0)));
		int fSanTake = Add(Pose(armL: 4, armR: 4, eyes: 2, mouth: 3));
		int fSan0 = Add(San(0, eyes: 0, mouth: 3));
		var forro = Show(30, "forró", 130, (fStand, 1), (fSanTake, 2), (fSan0, 3));
		forro.Seq(play.ToArray()); forro.Seq(fSan0, fSan0);
		Then(forro, 31);
		Notes(30, "forró", 130, 7, 13);
		var xote = new List<int>();
		int[] xf = { 1, 0, 2, 0 };
		for (int i = 0; i < 8; i++) xote.Add(Add(San(kseq[i % 4], eyes: 2, mouth: i % 2 == 0 ? 3 : 1, feet: xf[i % 4], up: i % 4 == 1 || i % 4 == 3 ? 0 : 1)));
		p.A(31, "xote").Move("-2", "0").Speed("110").Loop("1").Seq(xote.ToArray()).Then(100, 32).OnBorder(100, 32).OnGravity(100, 5);
		p.A(32, "xote back").Move("2", "0").Speed("110").Loop("1").Seq(xote.ToArray()).Seq(fSan0, fSanTake, fHappy).Then(100, 11).OnBorder(100, 11).OnGravity(100, 5);
		Notes(31, "xote", 110, 0, 5);

		// ============================================================ the triangle
		void Triangle(Frame f, bool hit)
		{
			LineAt(f, BX + 5, BY + 9, BX + 9, BY + 13, Line);
			St(f, BX + 7, BY + 14, "...C...", "..C.W..", ".C...C.", "C.....C", "CCCCC.C");
			if (hit) { LineAt(f, BX + 19, BY + 20, BX + 14, BY + 17, BrownDark); Spark(f, BX + 13, BY + 13); }
			else LineAt(f, BX + 20, BY + 20, BX + 18, BY + 15, BrownDark);
		}
		int fTriUp = Add(Pose(eyes: 2, mouth: 1, armL: 6, armR: 4, held: f => Triangle(f, false)));
		int fTriHit = Add(Pose(eyes: 10, mouth: 3, armL: 6, armR: 11, feet: 1, held: f => Triangle(f, true)));
		var tri = Show(33, "triangle", 130, (fStand, 1), (fTriUp, 2));
		for (int i = 0; i < 10; i++) tri.Seq(fTriHit, fTriUp);
		tri.Seq(fHappy, fHappy, fHappy);
		Notes(33, "triangle", 130, 3, 9);

		// ============================================================ the zabumba (drum)
		void Zabumba(Frame f, int hit)
		{
			St(f, BX + 6, BY + 15, ".OOOOOOOOOO.", "OWWWWWWWWWWO", "OcWWWWWWWWcO", "ONNNNNNNNNNO", "OyRyyRyyRyyO", "ORyyRyyRyyRO", "ONNNNNNNNNNO", ".OOOOOOOOOO.");
			if (hit == 1) { LineAt(f, BX + 4, BY + 9, BX + 8, BY + 14, Brown); St(f, BX + 8, BY + 14, "WW", "WW"); }
			else { LineAt(f, BX + 4, BY + 8, BX + 2, BY + 4, Brown); St(f, BX + 1, BY + 2, "WW", "WW"); }
			if (hit == 2) { LineAt(f, BX + 19, BY + 9, BX + 15, BY + 14, Brown); St(f, BX + 14, BY + 14, "WW", "WW"); }
			else { LineAt(f, BX + 19, BY + 8, BX + 21, BY + 4, Brown); St(f, BX + 21, BY + 2, "WW", "WW"); }
		}
		int fZab0 = Add(Pose(eyes: 2, mouth: 3, armL: 6, armR: 6, held: f => Zabumba(f, 0)));
		int fZabL = Add(Pose(eyes: 10, mouth: 1, armL: 6, armR: 6, feet: 1, held: f => Zabumba(f, 1)));
		int fZabR = Add(Pose(eyes: 2, mouth: 3, armL: 6, armR: 6, feet: 2, held: f => Zabumba(f, 2)));
		var zab = Show(34, "zabumba", 140, (fStand, 1), (fZab0, 3));
		for (int i = 0; i < 8; i++) zab.Seq(fZabL, fZab0, fZabR, fZab0);
		zab.Seq(fHappy, fHappy);
		Notes(34, "zabumba", 140, 5, 14);

		// ============================================================ the strong sun of the sertão, then he fans himself with his hat
		void SunAt(Frame f, int cx, int cy, int k, int r = 7)
		{
			St(f, cx - 3, cy - 3, "..ooo..", ".oYYYo.", "oYXXYYo", "oYXYYYo", "oYYYYYo", ".oYYYo.", "..ooo..");
			var ray = k == 0 ? new[] { (0, -1), (0, 1), (-1, 0), (1, 0) } : new[] { (-1, -1), (1, 1), (1, -1), (-1, 1) };
			int d = k == 0 ? r - 2 : r - 3;
			foreach (var (rx, ry) in ray) { f.Dot(cx + rx * d, cy + ry * d, Orange); f.Dot(cx + rx * (d + 1), cy + ry * (d + 1), Orange); }
		}
		int fSun1 = Add(Draw(f => SunAt(f, 18, -1, 0))), fSun2 = Add(Draw(f => SunAt(f, 18, -1, 1)));
		Frame HeatF(int k) => Draw(f => { foreach (int hx in new[] { 8, 17, 26 }) for (int i = 0; i < 9; i++) { int w = (i + k + hx) % 4; f.Dot(hx + (w == 1 ? 1 : w == 3 ? -1 : 0), Ground - 2 - i, i < 3 ? Orange : Yel); } });
		int fHeat1 = Add(HeatF(0)), fHeat2 = Add(HeatF(1)), fHeat3 = Add(HeatF(2));
		int fSquint = Add(With(Pose(eyes: 5, mouth: 4, look: -1), f => Sweat(f, BX + 17, BY + 7)));
		int fSquint2 = Add(With(Pose(eyes: 5, mouth: 4, armL: 4, armR: 4, squash: 1), f => Sweat(f, BX + 17, BY + 9)));
		int fHotTongue = Add(With(Pose(eyes: 5, mouth: 6, armL: 4, armR: 4, squash: 1), f => { Sweat(f, BX + 18, BY + 11); Sweat(f, BX + 2, BY + 13); }));
		int fHotTongue2 = Add(With(Pose(eyes: 1, mouth: 6, armL: 4, armR: 4, squash: 2), f => { Sweat(f, BX + 18, BY + 13); Sweat(f, BX + 2, BY + 10); }));
		var sunS = Show(35, "sertão sun", 150, (fUpFront, 3), (fLookUpFront, 2), (fSquint, 4), (fSquint2, 4), (fHotTongue, 3), (fHotTongue2, 3));
		Then(sunS, 75);
		int sunFade = nextChild++; Child(sunFade, "sun fades", "0", "0", 200, "3", fSun1, fSun2).Fade(1, 0);
		int sunShine = nextChild++; Child(sunShine, "sun shines", "0", "0", 220, "12", fSun1, fSun2); Then(p.Anims.Last(), sunFade);
		int heat = nextChild++; Child(heat, "heat waves", "0", "-1", 150, "8", fHeat1, fHeat2, fHeat3).Fade(1, 0);
		p.Childs.Add((35, "imageX-imageW*0.6", "imageY-imageH*0.1", sunShine));
		p.Childs.Add((35, "imageX-imageW*0.5", "imageY", heat));
		// hand fan: the hat, seen from the side, fans his face
		int fFan1 = Add(Pose(eyes: 10, mouth: 6, armL: 2, hat: 0, held: f => St(f, BX - 4, BY + 2, HatSide)));
		int fFan2 = Add(With(Pose(eyes: 10, mouth: 0, armL: 6, hat: 0, held: f => St(f, BX - 2, BY + 4, HatSide)), f => Sweat(f, BX + 18, BY + 8)));
		var fan = Loop2(75, "hand fan", 150, "random/10+8", fFan1, fFan1, fFan2, fFan2);

		// ============================================================ the rain arrives: a cloud comes, he dances, his flowers bloom
		string[] RainCloud = { "....kkkk..........", "..kkCCCCkk.kkk....", ".kCCCcCCCCkCCCkk..", "kCCcccCCCCCCCCCCk.", "kCCCCCCCCCCCCCCCCk", ".kkkkkkkkkkkkkkkk." };
		Frame CloudF(int k, bool rain) => Draw(f =>
		{
			St(f, 9, -8 + (k == 3 ? 1 : 0), RainCloud);
			if (!rain) return;
			for (int i = 0; i < 6; i++)
				for (int yy = -1 + (i * 7 + k * 4) % 10; yy < Ground; yy += 10) Drop(f, 10 + i * 3, yy);
		});
		int fCloud0 = Add(CloudF(0, false)), fCloud0b = Add(CloudF(3, false));
		int fRain1 = Add(CloudF(0, true)), fRain2 = Add(CloudF(1, true)), fRain3 = Add(CloudF(2, true));
		int cloudOut = nextChild++; Child(cloudOut, "cloud goes away", "-3", "-1", 100, "9", fCloud0, fCloud0b).Fade(1, 0);
		int cloudRain = nextChild++; Child(cloudRain, "cloud rains", "0", "0", 120, "9", fRain1, fRain2, fRain3); Then(p.Anims.Last(), cloudOut);
		int cloudIn = nextChild++; Child(cloudIn, "rain cloud comes", "-4", "0", 100, "9", fCloud0, fCloud0b); Then(p.Anims.Last(), cloudRain);
		int fHope = Add(Pose(eyes: 11, mouth: 0, armL: 2, armR: 2));
		int fHope2 = Add(Pose(eyes: 4, mouth: 10, armL: 2, armR: 2, look: 1));
		var arrive = Show(36, "rain arrives", 100, (fBack, 4), (fUpBack, 4), (fHope2, 4), (fHope, 4), (fWide, 2), (fHappyUp, 2));
		Then(arrive, 37);
		p.Childs.Add((36, "imageX+imageW*1.25", "imageY", cloudIn));
		Frame Wet(Frame f, int k) { foreach (var (wx, wy) in new[] { (BX + 4, BY + 2), (BX + 20, BY + 4), (BX + 1, BY + 14), (BX + 22, BY + 15) }) Drop(f, wx + k, wy + (k * 3) % 5); return f; }
		int fWet1 = Add(Wet(Pose(eyes: 2, mouth: 5, armL: 7, armR: 8, feet: 1, sway: -1), 0));
		int fWet2 = Add(Wet(Pose(eyes: 2, mouth: 3, armL: 7, armR: 7, feet: 0, squash: 1), 1));
		int fWet3 = Add(Wet(Pose(eyes: 2, mouth: 5, armL: 8, armR: 7, feet: 2, sway: 1), 2));
		int fWet4 = Add(Wet(Pose(eyes: 13, mouth: 5, armL: 3, armR: 3, feet: 3, up: 2, squash: -1), 3));
		var rd = Show(37, "rain dance", 120);
		for (int i = 0; i < 7; i++) rd.Seq(fWet1, fWet2, fWet3, i % 2 == 0 ? fWet2 : fWet4);
		rd.Seq(fWet2, fWet2);
		Then(rd, 38);
		Frame Glow(Frame f, int k) { foreach (var (gx, gy) in new[] { (BX - 1, BY + 1), (BX + 4, BY - 1), (BX + 19, BY + 1), (BX + 24, BY - 1) }) f.Dot(gx + k, gy + (k == 0 ? 0 : 1), Gold); return f; }
		int fBloom1 = Add(Pose(eyes: 3, mouth: 1, bloom: 1));
		int fBloom2 = Add(Pose(eyes: 11, mouth: 10, bloom: 2));
		int fBloom3 = Add(Glow(Pose(eyes: 11, mouth: 5, bloom: 3), 0));
		int fBloom3b = Add(Glow(Pose(eyes: 2, mouth: 3, bloom: 3), 1));
		int fBloomSniff = Add(Pose(eyes: 1, look: -1, mouth: 10, bloom: 3, blush: true, armL: 2));
		int fBloomHappy = Add(Pose(eyes: 2, mouth: 3, bloom: 3, blush: true));
		int fBloom2b = Add(Pose(eyes: 0, mouth: 0, bloom: 2));
		int fBloom1b = Add(Pose(eyes: 0, mouth: 0, bloom: 1));
		Show(38, "bloom", 150, (fBloom1, 3), (fBloom2, 3), (fBloom3, 2), (fBloom3b, 2), (fBloom3, 2), (fBloomHappy, 4), (fBloomSniff, 5), (fBloomHappy, 3), (fBloom2b, 2), (fBloom1b, 2), (fStand, 1));

		// ============================================================ the full moon: his flowers open at night and a bat visits
		Frame MoonF(int k) => Draw(f =>
		{
			Outlined(f, Gray, g => St(g, 14, 1, "..MMMM..", ".MMMMcMM", "MMcMMMMM", "MMMMMMcM", "MMMMMMMM", "MMMcMMMM", ".MMMMMM.", "..MMMM.."));
			foreach (var (sx, sy) in k == 0 ? new[] { (6, 3), (28, 0), (25, 9) } : new[] { (9, -2), (31, 5), (4, 9) }) Twinkle(f, sx, sy);
		});
		int fMoon1 = Add(MoonF(0)), fMoon2 = Add(MoonF(1));
		int moonSet = nextChild++; Child(moonSet, "moon sets", "0", "1", 200, "3", fMoon1, fMoon2).Fade(1, 0);
		int moonShine = nextChild++; Child(moonShine, "moon shines", "0", "0", 220, "12", fMoon1, fMoon2); Then(p.Anims.Last(), moonSet);
		int moonRise = nextChild++; Child(moonRise, "moon rises", "0", "-1", 200, "9", fMoon1, fMoon2); Then(p.Anims.Last(), moonShine);
		p.Childs.Add((39, "imageX-imageW*0.6", "imageY", moonRise));
		int fBat1 = Add(Draw(f => St(f, 14, 4, "k.......k", "kk.k.k.kk", ".kkkkkkk.", "...kkk...")));
		int fBat2 = Add(Draw(f => St(f, 14, 5, "...k.k...", "kkkkkkkkk", "k..kkk..k", "k.......k")));
		int bat = nextChild++; Child(bat, "bat flies by", "-3", "0", 90, "40", fBat1, fBat2).Move("-3", "1", "-3", "-1");
		p.Childs.Add((39, "imageX+imageW*1.5", "imageY-imageH*0.1", Wait("bat", 200, 26, bat)));
		int fNight1 = Add(Pose(eyes: 4, look: -1, mouth: 10));
		int fNight2 = Add(Pose(eyes: 11, mouth: 0, bloom: 1));
		int fNight3 = Add(Pose(eyes: 4, mouth: 10, bloom: 2));
		int fNight4 = Add(Glow(Pose(eyes: 11, mouth: 5, bloom: 3), 0));
		int fNight5 = Add(Glow(Pose(eyes: 10, mouth: 3, bloom: 3, blush: true), 1));
		int fNight6 = Add(Glow(Pose(eyes: 10, mouth: 3, bloom: 3, blush: true), 0));
		int fNightBack = Add(Pose(eyes: 4, look: 1, mouth: 10, bloom: 3));
		int fNightFront = Add(Pose(eyes: 4, look: -1, mouth: 5, bloom: 3));
		var moon = Show(39, "full moon", 200, (fUpFront, 3), (fNight1, 3), (fNight2, 4), (fNight3, 4), (fNight4, 4));
		Then(moon, 40);
		Show(40, "night bloom", 200, (fNight5, 2), (fNight6, 2), (fNight5, 2), (fNight6, 2), (fNightBack, 4), (fNightFront, 4), (fNight5, 2), (fNight6, 2), (fBloom2b, 2), (fBloom1b, 2), (fStand, 1));

		// ============================================================ the asa-branca flies by; he waves
		int fDove1 = Add(Draw(f => St(f, 12, 2, "......kk....", ".....kWck...", "....kWWck...", ".kk.kWck....", "kEckccccCCkk", ".kcccccccck.", "..kkkkkkkk..")));
		int fDove2 = Add(Draw(f => St(f, 12, 2, "............", "............", "............", ".kk.........", "kEckccccCCkk", ".kccWWWccck.", "..kkWWkkkk..", "....kk......")));
		int dove = nextChild++; Child(dove, "asa-branca flies", "-4", "0", 100, "40", fDove1, fDove1, fDove2, fDove2).Move("-4", "1", "-4", "-2");
		p.Childs.Add((41, "imageX+imageW*1.5", "imageY", dove));
		int fSigh = Add(Pose(eyes: 10, mouth: 0, blush: true, squash: 1, armL: 2, armR: 2));
		Show(41, "asa-branca", 100, (fBack, 4), (fUpBack, 8), (fUp, 6), (fUpFront, 4), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fSigh, 8), (fStand, 1));

		// ============================================================ the carcará circles over him
		string[] CarUp = { "........nn....", ".......nNNn...", "......nNNn....", ".nnn.nNNNn....", "nTETnNNNNNNNnn", "yyTTNNTNTNNNNn", ".y.nnnnnnnnnn." };
		string[] CarDown = { "..............", "..............", "..............", ".nnn..........", "nTETnNNNNNNNnn", "yyTTNNTNTNNNNn", ".y.nNNNNNNnnn.", "....nNNNNn....", ".....nnnn....." };
		int fCar1 = Add(Draw(f => St(f, 11, 0, CarUp))), fCar2 = Add(Draw(f => St(f, 11, 0, CarDown)));
		int carLeave = nextChild++; Child(carLeave, "carcará leaves", "-6", "-4", 80, "60", fCar1, fCar2);
		int carC3 = nextChild++; Child(carC3, "carcará circles again", "-4", "0", 80, "3", fCar1, fCar2); Then(p.Anims.Last(), carLeave);
		int carT2 = nextChild++; var ct2 = Child(carT2, "carcará turns again", "0", "0", 90, "0", fCar2, fCar1); ct2.Action = "flip"; Then(ct2, carC3);
		int carC2 = nextChild++; Child(carC2, "carcará circles back", "-4", "0", 80, "7", fCar1, fCar2); Then(p.Anims.Last(), carT2);
		int carT1 = nextChild++; var ct1 = Child(carT1, "carcará turns", "0", "0", 90, "0", fCar2, fCar1); ct1.Action = "flip"; Then(ct1, carC2);
		int carC1 = nextChild++; Child(carC1, "carcará circles", "-4", "0", 80, "3", fCar1, fCar2); Then(p.Anims.Last(), carT1);
		int carIn = nextChild++; Child(carIn, "carcará arrives", "-6", "2", 80, "7", fCar1, fCar2); Then(p.Anims.Last(), carC1);
		p.Childs.Add((42, "imageX+imageW*1.5", "imageY-imageH*0.4", carIn));
		int fAlert = Add(Pose(eyes: 3, look: 1, mouth: 4, armL: 1, armR: 1, spines: 2));
		int fAlert2 = Add(Pose(eyes: 3, look: -1, mouth: 4, armL: 2, armR: 2, spines: 2, squash: 1));
		int fAlert3 = Add(Pose(eyes: 4, look: 1, mouth: 8, armL: 2, armR: 2, spines: 2, squash: 1));
		int fAlert4 = Add(Pose(eyes: 4, look: -1, mouth: 8, armL: 2, armR: 2, spines: 2, squash: 1));
		int fPhew = Add(With(Pose(eyes: 1, mouth: 0, armL: 4, armR: 4), f => Sweat(f, BX + 18, BY + 7)));
		Show(42, "carcará", 150, (fUpBack, 3), (fAlert, 4), (fAlert2, 3), (fAlert3, 4), (fAlert4, 4), (fAlert3, 3), (fAlert4, 3), (fPhew, 5), (fHappy, 2));

		// ============================================================ a calango runs by and does push-ups; he does them too
		string[] Cal1 = { ".vv.........", "vvEvvvvvvv..", "oovvkvkvvvvv", ".v...v..v..." };
		string[] Cal2 = { ".vv.........", "vvEvvvvvvv..", "oovvkvkvvvvv", "..v...v...v." };
		int fCal1 = Add(Draw(f => St(f, 12, Ground - 3, Cal1))), fCal2 = Add(Draw(f => St(f, 12, Ground - 3, Cal2)));
		int fCalUp = Add(Draw(f => St(f, 12, Ground - 5, ".vv.........", "vvEvvvvvvv..", "oovvkvkvvvvv", ".v...v......", ".v...v...v..", "........v...")));
		int fCalDown = Add(Draw(f => St(f, 12, Ground - 2, "vvEvvvvvvv..", "oovvkvkvvvvv", "vv..vv..vv..")));
		int calOff = nextChild++; Child(calOff, "calango runs off", "-7", "0", 60, "60", fCal1, fCal2);
		int calPush = nextChild++; Child(calPush, "calango push-ups", "0", "0", 150, "7", fCalUp, fCalDown); Then(p.Anims.Last(), calOff);
		int calIn = nextChild++; Child(calIn, "calango runs in", "-6", "0", 60, "9", fCal1, fCal2); Then(p.Anims.Last(), calPush);
		p.Childs.Add((43, "imageX+imageW*1.0", "imageY", calIn));
		var cal = Show(43, "calango", 60, (fBack, 4), (fStand, 2), (fFront, 14));
		Then(cal, 44);
		int fPushDown = Add(Pose(look: -1, eyes: 2, mouth: 3, squash: 2, feet: 4, armL: 4, armR: 4));
		int fPushUp = Add(Pose(look: -1, eyes: 0, mouth: 5, squash: -1, armL: 3, armR: 3));
		var push = Show(44, "push-ups", 150);
		for (int i = 0; i < 8; i++) push.Seq(fPushUp, fPushDown);
		Then(push, 116);

		// ============================================================ a jegue (little donkey) walks by; he tips his hat
		string[] Jegue1 =
		{
			"..j.j...............", "..jjjj..............", ".jjjjjk.............", "jEjjjjkk............", "cjjjjjjkkkkkkkkk....",
			"ccjjjjjjNNNNNjjjjk..", "....jjjjNyyyNjjjjjk.", "....jjjjNNNNNjjjjj.k", "....jcccccccccccjj..",
			"....jj.jj....jj.jj..", "....jj.jj....jj.jj..", "....kk.kk....kk.kk..",
		};
		string[] Jegue2 = Jegue1.Take(9).Concat(new[] { "...jj...jj..jj...jj.", "...jj...jj..jj...jj.", "...kk...kk..kk...kk." }).ToArray();
		int fJeg1 = Add(Draw(f => St(f, 8, Ground - 11, Jegue1))), fJeg2 = Add(Draw(f => St(f, 8, Ground - 11, Jegue2)));
		int jeg = nextChild++; Child(jeg, "jegue walks by", "-2", "0", 110, "50", fJeg1, fJeg1, fJeg2, fJeg2);
		p.Childs.Add((45, "imageX+imageW*1.3", "imageY", jeg));
		int fTip1 = Add(Pose(armL: 8, eyes: 2, mouth: 3, hatDy: -2));
		int fTip2 = Add(Pose(armL: 8, eyes: 1, mouth: 3, hatDy: -3, sway: -1, squash: 1));
		int fBackSmile = Add(Pose(look: 1, mouth: 3));
		int fFrontSmile = Add(Pose(look: -1, mouth: 3));
		Show(45, "jegue", 110, (fBack, 6), (fBackSmile, 10), (fStand, 4), (fSmile, 8), (fTip1, 3), (fTip2, 6), (fTip1, 3), (fGrin, 4), (fFrontSmile, 12), (fWave3, 2), (fWave2, 2), (fWave3, 2), (fWave2, 2), (fHappy, 3));

		// ============================================================ cordel: he reads a little booklet of string literature
		string[] CordelOpen = { ".kkkkk.kkkkk.", "kTTTTTkTTTTTk", "kTllTTkTlllTk", "kTTTTTkTTTTTk", "kTlllTkTllTTk", "kTTTTTkTTTTTk", ".kkkkkkkkkkk." };
		string[] CordelTurn = { ".kkkkk.kk....", "kTTTTTkTTk...", "kTllTTkTTTk..", "kTTTTTkTlTTk.", "kTlllTkTTTTTk", "kTTTTTkTTTTTk", ".kkkkkkkkkkk." };
		string[] CordelCover = { ".kkkkkkk.", "kTTTTTTTk", "kTTEEETTk", "kTEWEWETk", "kTTEEETTk", "kTTEtETTk", "kTlllllTk", ".kkkkkkk." };
		int fCorTake = Add(Pose(armL: 6, armR: 6, eyes: 2, mouth: 3, held: f => St(f, BX + 7, BY + 12, CordelCover)));
		int fCorRead1 = Add(Pose(armL: 6, armR: 6, eyes: 12, look: -1, held: f => St(f, BX + 5, BY + 12, CordelOpen)));
		int fCorRead2 = Add(Pose(armL: 6, armR: 6, eyes: 12, look: 1, held: f => St(f, BX + 5, BY + 12, CordelOpen)));
		int fCorGiggle = Add(Pose(armL: 6, armR: 6, eyes: 13, held: f => St(f, BX + 5, BY + 12, CordelOpen)));
		int fCorTurn = Add(Pose(armL: 6, armR: 6, eyes: 12, held: f => St(f, BX + 5, BY + 12, CordelTurn)));
		var kkk = Pose(armL: 6, armR: 6, eyes: 13, held: f => St(f, BX + 5, BY + 12, CordelOpen)); Bubble(kkk, "kkk", 0, -7); int fCorLaugh = Add(kkk);
		var kkk2 = Pose(armL: 6, armR: 6, eyes: 13, squash: 1, held: f => St(f, BX + 5, BY + 12, CordelOpen)); Bubble(kkk2, "kkk", 0, -7); int fCorLaugh2 = Add(kkk2);
		Show(46, "cordel", 200, (fStand, 1), (fCorTake, 3), (fCorRead1, 4), (fCorRead2, 4), (fCorGiggle, 3), (fCorRead1, 2), (fCorTurn, 2), (fCorRead1, 3), (fCorRead2, 3),
			(fCorLaugh, 2), (fCorLaugh2, 2), (fCorLaugh, 2), (fCorTake, 2), (fHappy, 2));

		// ============================================================ food: held up on the front arm, then to the mouth
		(int, int)[] Eating(string[][] stages)
		{
			var steps = new List<(int, int)>();
			for (int s = 0; s < stages.Length - 1; s++)
			{
				int s0 = s;
				steps.Add((Add(Pose(armL: 1, eyes: 2, mouth: 3, held: f => Item(f, stages[s0], false))), s == 0 ? 4 : 2));
				steps.Add((Add(Pose(armL: 6, eyes: 1, mouth: 1, held: f => Item(f, stages[s0], true))), 2));
				steps.Add((Add(Pose(armL: 1, eyes: 10, mouth: 9, held: f => Item(f, stages[s0 + 1], false))), 2));
				steps.Add((Add(Pose(armL: 1, eyes: 2, mouth: 0, squash: 1, held: f => Item(f, stages[s0 + 1], false))), 2));
			}
			return steps.ToArray();
		}
		string[][] Corn = { new[] { ".Y.", "YXY", "YYX", "XYY", "YXY", "GYG", "GGG" }, new[] { "...", "...", "Y.X", "XYY", "YXY", "GYG", "GGG" }, new[] { "...", "...", "...", "...", "Y.Y", "GYG", "GGG" }, new[] { "...", "...", "...", "...", "...", "GyG", "GGG" } };
		string[][] Cookie = { new[] { ".yyy.", "ynyyy", "yyyny", ".yyy." }, new[] { ".yy..", "yny..", "yyyn.", ".yyy." }, new[] { ".....", "yn...", "yy...", ".y..." } };
		string[][] Rapadura = { new[] { "yyNNN", "NNNNn", "NNNNn", "Nnnnn" }, new[] { "yyN..", "NNNn.", "NNNNn", "Nnnnn" }, new[] { ".....", "NN...", "NNN..", "Nnn.." } };
		string[][] Tapioca = { new[] { "..cWWc.", ".cWWWWc", "cWRRWWc", "cWWWWc." }, new[] { "..cWW..", ".cWWW..", "cWRRW..", "cWWWW.." }, new[] { ".......", "...W...", "cWR....", "cWW...." } };
		string[][] Mug(char drink) => new[] { new[] { "BBBB..", "W" + drink + drink + "Wkk", "WWWW.k", "WWWWkk", ".kkk.." }, new[] { "BBBB..", "WWWWkk", "W" + drink + drink + "W.k", "WWWWkk", ".kkk.." }, new[] { "BBBB..", "WWWWkk", "WWWW.k", "WWWWkk", ".kkk.." } };
		string[][] IceCream = { new[] { ".hhh.", "hWhhh", "hhhhh", "yNyNy", ".yNy.", "..y.." }, new[] { ".....", ".hhh.", "hhhhh", "yNyNy", ".yNy.", "..y.." }, new[] { ".....", ".....", ".hh..", "yNyNy", ".yNy.", "..y.." } };

		// cuscuz: a plate on the other arm, a spoon to the mouth; steam rises
		void Plate(Frame f, int stage)
		{
			int x = BX + 20, y = BY + 3;
			if (stage == 3) St(f, x + 1, y, "...YXY...", "..YYXYY..", ".YYYYYYY.");
			if (stage == 2) St(f, x + 1, y + 1, "...YYY...", ".YYYYYY..");
			if (stage == 1) St(f, x + 1, y + 2, "..YYY....");
			St(f, x, y + 3, "kWWWWWWWWWk", ".kkkkkkkkk.");
		}
		void Spoon(Frame f, bool atMouth, bool full)
		{
			if (atMouth) { LineAt(f, BX + 5, BY + 9, BX + 9, BY + 11, Gray); if (full) f.Dot(BX + 10, BY + 11, Yel); }
			else { LineAt(f, BX + 2, BY + 7, BX + 1, BY + 3, Gray); if (full) f.Dot(BX + 1, BY + 2, Yel); }
		}
		var cus = new List<(int, int)>();
		cus.Add((Add(Pose(armL: 0, armR: 1, eyes: 11, mouth: 5, held: f => { Plate(f, 3); Spoon(f, false, false); })), 4));
		for (int s = 3; s > 0; s--)
		{
			int s0 = s;
			cus.Add((Add(Pose(armL: 6, armR: 1, eyes: 1, mouth: 1, held: f => { Plate(f, s0); Spoon(f, true, true); })), 2));
			cus.Add((Add(Pose(armL: 0, armR: 1, eyes: 10, mouth: 9, held: f => { Plate(f, s0 - 1); Spoon(f, false, false); })), 3));
			cus.Add((Add(Pose(armL: 0, armR: 1, eyes: 2, mouth: 0, squash: 1, held: f => { Plate(f, s0 - 1); Spoon(f, false, false); })), 2));
		}
		cus.Add((fHappy, 3));
		Show(47, "cuscuz", 180, cus.ToArray());
		int fSteam1 = Add(Draw(f => Steam(f, BX + 24, BY - 2, 0))), fSteam2 = Add(Draw(f => Steam(f, BX + 24, BY - 2, 1)));
		int cusSteam = nextChild++; Child(cusSteam, "cuscuz steam", "0", "-1", 160, "10", fSteam1, fSteam2).Fade(1, 0);
		p.Childs.Add((47, "imageX", "imageY", cusSteam));

		// rapadura: sweet but hard! crumbs fall
		var rap = Eating(Rapadura).ToList();
		int fRapCrack = Add(With(Pose(armL: 6, eyes: 3, mouth: 8, dx: -1, spines: 2, held: f => Item(f, Rapadura[1], true)), f => Spark(f, BX + 7, BY + 7)));
		int fRapCrack2 = Add(Pose(armL: 6, eyes: 3, mouth: 8, dx: 1, spines: 2, held: f => Item(f, Rapadura[1], true)));
		rap.Insert(2, (fRapCrack, 1)); rap.Insert(3, (fRapCrack2, 1)); rap.Insert(4, (fRapCrack, 1)); rap.Insert(5, (fRapCrack2, 1));
		rap.Add((fHappy, 3));
		Show(48, "rapadura", 150, rap.ToArray());
		int fCrumb1 = Add(Draw(f => f.Dot(BX + 9, BY + 14, Brown).Dot(BX + 11, BY + 15, BrownDark).Dot(BX + 8, BY + 16, Tan)));
		int crumbs = nextChild++; Child(crumbs, "crumbs fall", "0", "3", 100, "6", fCrumb1, fCrumb1).Fade(1, 0.2);
		p.Childs.Add((48, "imageX", "imageY", Wait("crumbs", 150, 6, crumbs)));
		var tap = Eating(Tapioca).ToList(); tap.Add((fHappy, 3));
		Show(49, "tapioca", 170, tap.ToArray());

		// ============================================================ São João: the bonfire
		string[] Logs = { ".nN......Nn.", "...NnNNnN...", ".nNNnnnnNNn." };
		string[][] Flames =
		{
			new[] { ".....R......", "....RoR.....", "...RoXoR..R.", "..RoXXXoRRo.", ".RoXXWXXoRo.", ".RoXXXXXXoR." },
			new[] { "......R.....", "..R..RoR....", "..RoRoXoR...", ".RoXoXXXoR..", ".RoXXWXXXoR.", "..RoXXXXXoR." },
			new[] { "....R.......", "....oR...R..", "...RoXR.RoR.", "..RoXXXoRoR.", ".RoXXXWXXoR.", ".RoXXXXXXoRR" },
		};
		Frame FireF(int k, int size) => Draw(f =>
		{
			St(f, 12, Ground - 2, Logs);
			if (size == 2) { St(f, 12, Ground - 8, Flames[k]); foreach (var (sx, sy) in new[] { (14 + k * 3, 20 - k), (21 - k * 2, 18 + k), (17, 15 + k * 2) }) f.Dot(sx, sy, k == 1 ? Orange : Gold); }
			if (size == 1) St(f, 12, Ground - 4, Flames[k].Skip(4).ToArray());
			if (size == 0) { f.Dot(15, Ground - 3, Orange).Dot(19, Ground - 3, Red).Dot(17 + k, Ground - 6 - k, Gray).Dot(16 - k, Ground - 9 - k, Gray); }
		});
		int fFireS1 = Add(FireF(0, 1)), fFireS2 = Add(FireF(1, 1));
		int fFire1 = Add(FireF(0, 2)), fFire2 = Add(FireF(1, 2)), fFire3 = Add(FireF(2, 2));
		int fEmber1 = Add(FireF(0, 0)), fEmber2 = Add(FireF(1, 0));
		int fireOut = nextChild++; Child(fireOut, "fire dies", "0", "0", 200, "3", fEmber1, fEmber2).Fade(1, 0);
		int fireBurn = nextChild++; Child(fireBurn, "fire burns", "0", "0", 120, "13", fFire1, fFire2, fFire3); Then(p.Anims.Last(), fireOut);
		int fireIn = nextChild++; Child(fireIn, "fire lights", "0", "0", 150, "1", fFireS1, fFireS2); Then(p.Anims.Last(), fireBurn);
		p.Childs.Add((50, "imageX-imageW*0.75", "imageY", fireIn));
		int fSpk1 = Add(Draw(f => f.Dot(17, 18, Gold).Dot(20, 21, Orange))), fSpk2 = Add(Draw(f => f.Dot(18, 18, Orange).Dot(19, 21, Gold)));
		int sparks = nextChild++; Child(sparks, "sparks rise", "0", "-2", 110, "9", fSpk1, fSpk2).Fade(1, 0);
		p.Childs.Add((50, "imageX-imageW*0.75", "imageY", Wait("sparks", 120, 10, sparks)));
		p.Childs.Add((50, "imageX-imageW*0.6", "imageY", Wait("sparks 2", 120, 22, sparks)));
		int fWarm1 = Add(Pose(look: -1, eyes: 10, mouth: 3, armL: 5, armR: 5));
		int fWarm2 = Add(Pose(look: -1, eyes: 10, mouth: 0, armL: 5, armR: 5, squash: 1));
		int fArrasta1 = Add(Pose(eyes: 2, mouth: 5, feet: 1, sway: -1, armL: 7, armR: 8));
		int fArrasta2 = Add(Pose(eyes: 2, mouth: 3, feet: 2, sway: 1, armL: 8, armR: 7));
		var fog = Show(50, "fogueira", 120, (fFront, 4), (fFrontSmile, 4));
		for (int i = 0; i < 3; i++) fog.Seq(fWarm1, fWarm1, fWarm2, fWarm2);
		for (int i = 0; i < 4; i++) fog.Seq(fArrasta1, fArrasta1, fArrasta2, fArrasta2);
		for (int i = 0; i < 2; i++) fog.Seq(fWarm1, fWarm1, fWarm2, fWarm2);
		fog.Seq(fHappy, fHappy);

		// ============================================================ São João: party flags; he dances the quadrilha
		char[] flagColors = { Red, Yel, Blue, Purple, Orange };
		Frame FlagsF(int k, int offset) => Draw(f =>
		{
			for (int x = 0; x < W; x++) f.Dot(x, -6 + (int)Math.Round(2 * Math.Sin(Math.PI * x / W)), Line);
			for (int i = 0; i < 9; i++)
			{
				int x = 1 + i * 4, y = -5 + (int)Math.Round(2 * Math.Sin(Math.PI * x / W));
				char c = flagColors[(i + offset) % flagColors.Length];
				f.Rect(x, y, 3, 2, c).Dot(x + 1 + ((i + k) % 2 == 0 ? 0 : (k == 0 ? -1 : 1)), y + 2, c);
			}
		});
		int fFlagsA1 = Add(FlagsF(0, 0)), fFlagsA2 = Add(FlagsF(1, 0)), fFlagsB1 = Add(FlagsF(0, 2)), fFlagsB2 = Add(FlagsF(1, 2));
		int flagsFadeA = nextChild++; Child(flagsFadeA, "flags fade", "0", "0", 200, "2", fFlagsA1, fFlagsA2).Fade(1, 0);
		int flagsA = nextChild++; Child(flagsA, "party flags", "0", "0", 220, "10", fFlagsA1, fFlagsA2); Then(p.Anims.Last(), flagsFadeA);
		int flagsFadeB = nextChild++; Child(flagsFadeB, "flags fade 2", "0", "0", 200, "2", fFlagsB1, fFlagsB2).Fade(1, 0);
		int flagsB = nextChild++; Child(flagsB, "party flags 2", "0", "0", 220, "10", fFlagsB1, fFlagsB2); Then(p.Anims.Last(), flagsFadeB);
		p.Childs.Add((51, "imageX-imageW*0.5", "imageY", flagsA));
		p.Childs.Add((51, "imageX+imageW*0.5", "imageY", flagsB));
		int fQuad1 = Add(Pose(eyes: 2, mouth: 5, armL: 7, armR: 3, feet: 1, sway: -1));
		int fQuad2 = Add(Pose(eyes: 2, mouth: 3, armL: 3, armR: 8, feet: 2, sway: 1));
		var an = Pose(eyes: 2, mouth: 5, armL: 3, armR: 3); Bubble(an, "ANARRIÊ!", 0, -8); int fAnarrie = Add(an);
		var quad = Show(51, "bandeirinhas", 150, (fUp, 4), (fHappyUp, 2));
		for (int i = 0; i < 6; i++) quad.Seq(fQuad1, fQuad1, fQuad2, fQuad2);
		quad.Seq(fAnarrie, fAnarrie, fAnarrie, fAnarrie, fAnarrie, fHappy, fHappy);

		// ============================================================ a toy balloon drifts to him and pops on his spines
		int fBal1 = Add(Draw(f => { St(f, 0, BY + 4, ".RRR.", "RRWRR", "RWRRR", "RRRRR", ".RRR.", "..R.."); LineAt(f, 2, BY + 10, 2, BY + 16, Line); }));
		int fBal2 = Add(Draw(f => { St(f, 0, BY + 4, ".RRR.", "RRWRR", "RWRRR", "RRRRR", ".RRR.", "..R.."); LineAt(f, 2, BY + 10, 3, BY + 16, Line); }));
		int fPop1 = Add(Draw(f => St(f, 0, BY + 4, "R.X.R", ".XWX.", "XWWWX", ".XWX.", "R.X.R")));
		int fPop2 = Add(Draw(f => { f.Dot(0, BY + 2, Red).Dot(5, BY + 3, Red).Dot(1, BY + 9, Red).Dot(4, BY + 8, Red).Dot(2, BY + 5, Gold); LineAt(f, 2, BY + 11, 2, BY + 16, Line); }));
		int fPop3 = Add(Draw(f => { f.Dot(0, BY + 6, Red).Dot(5, BY + 7, Red).Dot(1, BY + 13, Red).Dot(4, BY + 12, Red); LineAt(f, 2, BY + 15, 2, BY + 20, Line); }));
		int balPop = nextChild++; Child(balPop, "balloon pops", "0", "0", 110, "0", fPop1, fPop1, fPop2, fPop2, fPop3, fPop3).Fade(1, 0.2);
		int balIn = nextChild++; Child(balIn, "balloon drifts", "4", "0", 120, "7", fBal1, fBal2); Then(p.Anims.Last(), balPop);
		p.Childs.Add((52, "imageX-imageW*1.0", "imageY", balIn));
		int fBalSee = Add(Pose(look: -1, eyes: 2, mouth: 3));
		int fBalClose = Add(Pose(look: -1, eyes: 3, mouth: 1, sway: -1));
		int fPopShock = Add(Pose(eyes: 3, mouth: 2, armL: 7, armR: 7, spines: 2, up: 2, squash: -1, hatDy: -3, feet: 3));
		int fPopShock2 = Add(Pose(eyes: 3, mouth: 2, armL: 3, armR: 3, spines: 2, hatDy: -1));
		int fSheepish = Add(With(Pose(eyes: 13, mouth: 3, blush: true, armL: 6), f => Sweat(f, BX + 18, BY + 6)));
		Show(52, "balloon pop", 120, (fFront, 6), (fBalSee, 6), (fBalClose, 4), (fPopShock, 3), (fPopShock2, 3), (fBlink, 2), (fSheepish, 6), (fHappy, 2));

		// ============================================================ tip hat, sneeze (his spines fly off!), hat trick
		Show(53, "tip hat", 150, (fStand, 1), (fTip1, 2), (fTip2, 5), (fTip1, 2), (fGrin, 4), (fHappy, 2));
		int fAh1 = Add(Pose(eyes: 5, mouth: 1, squash: -1, look: -1));
		int fAh2 = Add(Pose(eyes: 1, mouth: 2, squash: -2, armL: 2, armR: 2, hatDy: -1));
		int fChoo = Add(Pose(eyes: 1, mouth: 3, squash: 2, spines: 0, armL: 1, armR: 1, sway: -1, hatDy: 1));
		int fBald = Add(Pose(eyes: 3, mouth: 4, spines: 0));
		int fBald2 = Add(Pose(eyes: 12, mouth: 4, spines: 0, look: -1));
		int fRegrow = Add(With(Pose(eyes: 2, mouth: 3, spines: 2), f => { Spark(f, BX - 1, BY + 10); Spark(f, BX + 23, BY + 12); }));
		Show(54, "sneeze", 130, (fStand, 1), (fAh1, 3), (fStand, 1), (fAh1, 2), (fAh2, 3), (fChoo, 3), (fBald, 4), (fBald2, 4), (fRegrow, 3), (fHappy, 3));
		int fSpL = Add(Draw(f => { foreach (var (sx, sy) in new[] { (BX + 2, BY + 10), (BX + 4, BY + 15), (BX + 1, BY + 19) }) St(f, sx, sy, "CS"); }));
		int fSpR = Add(Draw(f => { foreach (var (sx, sy) in new[] { (BX + 20, BY + 11), (BX + 18, BY + 16), (BX + 21, BY + 20) }) St(f, sx, sy, "SC"); }));
		int spL = nextChild++; Child(spL, "spines fly", "-5", "-1", 80, "6", fSpL, fSpL).Fade(1, 0);
		int spR = nextChild++; Child(spR, "spines fly 2", "5", "-1", 80, "6", fSpR, fSpR).Fade(1, 0);
		p.Childs.Add((54, "imageX", "imageY", Wait("spines", 130, 10, spL)));
		p.Childs.Add((54, "imageX", "imageY", Wait("spines 2", 130, 10, spR)));
		var trick = new List<int> { Add(Pose(squash: 2, feet: 4, armL: 4, armR: 4, eyes: 1)), Add(Pose(armL: 3, armR: 3, eyes: 4, mouth: 1, squash: -1, hatDy: -5)) };
		int[] hy = { -9, -13, -16, -16, -13, -9, -5, -2 };
		for (int i = 0; i < hy.Length; i++) trick.Add(Add(Pose(armL: 3, armR: 3, eyes: 4, mouth: i % 2 == 0 ? 5 : 10, hat: i % 2 == 0 ? 2 : 1, hatDy: hy[i], squash: i < 4 ? 0 : -1)));
		trick.Add(Add(Pose(squash: 1, armL: 1, armR: 1, eyes: 1, mouth: 3)));
		var trickA = Show(26, "hat trick", 90, (fStand, 1));
		trickA.Seq(trick.ToArray()); trickA.Seq(fHappy, fHappy, fHappy, fHappyUp, fHappyUp, fHappy);

		// ============================================================ a dust devil steals his hat; he runs after it
		Frame Whirl(int k) => Draw(f =>
		{
			for (int r = 0; r < 16; r++)
			{
				int half = 6 - r * 5 / 15, cx = 18 + (r % 4 == 0 ? k - 1 : 0), yy = Ground - 15 + r;
				for (int dx = -half; dx <= half; dx++)
				{
					int m = ((dx + r + k * 2) % 4 + 4) % 4;
					if (m == 0) f.Dot(cx + dx, yy, Gray); else if (m == 2 && (dx + r) % 3 != 0) f.Dot(cx + dx, yy, Sand);
				}
			}
			f.Dot(14 + k * 4, Ground - 10 + k, Olive).Dot(22 - k * 3, Ground - 5 - k, Olive);
		});
		int fWhirl1 = Add(Whirl(0)), fWhirl2 = Add(Whirl(1)), fWhirl3 = Add(Whirl(2));
		int whirl = nextChild++; Child(whirl, "whirlwind", "-4", "0", 80, "26", fWhirl1, fWhirl2, fWhirl3);
		p.Childs.Add((55, "imageX+imageW*1.5", "imageY", whirl));
		int fHatFly1 = Add(Draw(f => St(f, BX + 3, Ground - 7, HatFront))), fHatFly2 = Add(Draw(f => St(f, BX + 3, Ground - 7, HatBack)));
		int fHatRest2 = Add(Draw(f => { St(f, BX + 3, Ground - 7, HatFront); f.Dot(BX + 1, Ground, Sand).Dot(BX + 22, Ground, Sand); }));
		int hatRest = nextChild++; Child(hatRest, "hat on the ground", "0", "0", 140, "2", fHatFly1, fHatRest2);
		int hatFall = nextChild++; Child(hatFall, "hat falls", "-3", "3", 80, "8", fHatFly1, fHatFly2); Then(p.Anims.Last(), hatRest);
		int hatUp = nextChild++; Child(hatUp, "hat flies", "-4", "-2", 80, "4", fHatFly1, fHatFly2); Then(p.Anims.Last(), hatFall);
		// the hat is drawn on the ground in its window: the window starts 17 rows higher, so at first the hat is on his head
		p.Childs.Add((55, "imageX", "imageY-imageH*17/44", Wait("hat", 80, 24, hatUp)));
		int fHold = Add(Pose(eyes: 3, look: 1, mouth: 2, armL: 8, hatDy: -1));
		int fWorry = Add(Pose(eyes: 3, look: 1, mouth: 1));
		var dd = Show(55, "dust devil", 80, (fBack, 6), (fWorry, 10), (fHold, 8));
		Then(dd, 56);
		int fBlown = Add(Pose(hat: 0, eyes: 3, mouth: 2, look: -1, armL: 7, armR: 7, spines: 2, squash: -1));
		int fBlown2 = Add(Pose(hat: 0, eyes: 4, mouth: 2, look: -1, armL: 8, armR: 8, spines: 2));
		Then(Show(56, "hat blown", 80, (fBlown, 5), (fBlown2, 5)), 57);
		var runNoHat = new[]
		{
			Add(Pose(hat: 0, look: -1, mouth: 2, squash: 1, armL: 7, armR: 8)),
			Add(Pose(hat: 0, look: -1, mouth: 2, feet: 2, sway: -1, armL: 8, armR: 7, up: 1)),
			Add(Pose(hat: 0, look: -1, mouth: 1, feet: 3, sway: -1, armL: 8, armR: 7, up: 2, squash: -1)),
			Add(Pose(hat: 0, look: -1, mouth: 2, squash: 1, armL: 8, armR: 7)),
			Add(Pose(hat: 0, look: -1, mouth: 2, feet: 1, sway: 1, armL: 7, armR: 8, up: 1)),
			Add(Pose(hat: 0, look: -1, mouth: 1, feet: 3, sway: 1, armL: 7, armR: 8, up: 2, squash: -1)),
		};
		var chase = p.A(57, "chase hat").Move("-2", "0").Speed("60");
		for (int i = 0; i < 38; i++) chase.Seq(runNoHat[i % 6]);
		chase.Then(100, 58).OnBorder(100, 58).OnGravity(100, 5);
		int fGrab = Add(Pose(hat: 0, squash: 2, sway: -1, armL: 4, look: -1, eyes: 2, mouth: 3, feet: 4, under: f => St(f, BX - 6, Ground - 7, HatFront)));
		int fGrab2 = Add(Pose(hat: 0, squash: 1, armL: 4, look: -1, eyes: 2, mouth: 3, under: f => St(f, BX - 6, Ground - 8, HatFront)));
		int fHatOn = Add(With(Pose(eyes: 2, mouth: 3, armL: 8, hatDy: -2), f => Spark(f, BX + 1, BY - 2)));
		Show(58, "hat back", 150, (fGrab, 3), (fGrab2, 1), (fHatOn, 2), (fTip1, 2), (fGrin, 3), (fHappy, 2));

		// ============================================================ a goat tries to eat him (bad idea)
		string[] Goat1 =
		{
			"............CC..", "...........CC...", "..........TTTT..", "..........TETTk.", "TT.......TTTTTTk",
			".TTTTTTTTTTTT...", ".TNNTTTTTTTTkT..", ".TNNNTTTTTTT.k..", ".CTTTTTTTTTC....", "..TT.TT..TT.TT..", "..TT.TT..TT.TT..", "..kk.kk..kk.kk..",
		};
		string[] Goat2 = Goat1.Take(9).Concat(new[] { ".TT..TT.TT..TT..", ".TT..TT.TT..TT..", ".kk..kk.kk..kk.." }).ToArray();
		string[] GoatEat = { "................", "................", "................", "............CC..", "TT.........CC...",
			".TTTTTTTTTTTTT..", ".TNNTTTTTTTTTETT", ".TNNNTTTTTTTTTTk", ".CTTTTTTTTTC.Tk.", "..TT.TT..TT.TT..", "..TT.TT..TT.TT..", "..kk.kk..kk.kk.." };
		int fGoat1 = Add(Draw(f => St(f, 10, Ground - 11, Goat1))), fGoat2 = Add(Draw(f => St(f, 10, Ground - 11, Goat2)));
		int fGoatEat1 = Add(Draw(f => St(f, 10, Ground - 11, GoatEat))), fGoatEat2 = Add(Draw(f => St(f, 11, Ground - 11, GoatEat)));
		int fGoatOuch = Add(Draw(f => { St(f, 10, Ground - 15, Goat1.Select(r => r.Replace('E', 'X')).ToArray()); Bubble(f, "!", 18, Ground - 26); }));
		int goatRun = nextChild++; Child(goatRun, "goat runs away", "7", "0", 60, "60", fGoat1, fGoat2);
		int goatJump = nextChild++; var gj = Child(goatJump, "goat jumps", "0", "0", 110, "0", fGoatOuch, fGoatOuch, fGoatOuch); gj.Action = "flip"; Then(gj, goatRun);
		int goatEat = nextChild++; Child(goatEat, "goat nibbles", "0", "0", 150, "3", fGoatEat1, fGoatEat2); Then(p.Anims.Last(), goatJump);
		int goatIn = nextChild++; Child(goatIn, "goat comes", "4", "0", 100, "7", fGoat1, fGoat2); Then(p.Anims.Last(), goatEat);
		p.Childs.Add((59, "imageX-imageW*1.5", "imageY", goatIn));
		var cq = Pose(look: -1, mouth: 0); Bubble(cq, "?", 0, -6); int fGoatQ = Add(cq);
		int fNibbled = Add(With(Pose(look: -1, eyes: 3, mouth: 4, armL: 2, squash: 1), f => Sweat(f, BX + 18, BY + 6)));
		int fNibbled2 = Add(Pose(look: -1, eyes: 5, mouth: 8, armL: 1, squash: 1));
		int fProud = Add(Pose(eyes: 13, mouth: 3, spines: 2, armL: 3, armR: 3));
		var laugh1 = Pose(eyes: 13, mouth: 5, sway: -1); Bubble(laugh1, "kkk", 0, -7); int fLaugh1 = Add(laugh1);
		var laugh2 = Pose(eyes: 13, mouth: 5, sway: 1, squash: 1); Bubble(laugh2, "kkk", 0, -7); int fLaugh2 = Add(laugh2);
		Show(59, "goat", 100, (fFront, 8), (fGoatQ, 8), (fNibbled, 6), (fNibbled2, 6), (fProud, 4), (fLaugh1, 3), (fLaugh2, 3), (fLaugh1, 3), (fHappy, 2));

		// ============================================================ a shooting star: he makes a wish
		int fStar1 = Add(Draw(f => St(f, 14, -6, "........Y", "......YX.", "....XX...", "..XW.....", ".WWX.....", "..X......")));
		int fStar2 = Add(Draw(f => St(f, 14, -6, ".........", "......Y..", "....YX...", "..XW.....", ".WWWX....", "..X......")));
		int star = nextChild++; Child(star, "star streaks by", "-8", "2", 60, "20", fStar1, fStar2).Fade(1, 0.3);
		p.Childs.Add((23, "imageX+imageW*1.5", "imageY", Wait("star", 150, 4, star)));
		int fWish1 = Add(With(Pose(eyes: 1, mouth: 0, armL: 6, armR: 6, blush: true), f => { Twinkle(f, BX + 2, BY + 2); Twinkle(f, BX + 22, BY + 5); }));
		int fWish2 = Add(With(Pose(eyes: 1, mouth: 10, armL: 6, armR: 6, blush: true), f => { Twinkle(f, BX + 3, BY + 5); Twinkle(f, BX + 21, BY + 1); }));
		Show(23, "shooting star", 150, (fUpBack, 4), (fUp, 3), (fUpFront, 3), (fWide, 2), (fWish1, 3), (fWish2, 3), (fWish1, 3), (fWish2, 3), (fHappy, 4));

		// ============================================================ pião (spinning top)
		string[][] Top =
		{
			new[] { ".kkk.", "NRyRN", "NyRyN", ".NRN.", "..N..", "..l.." },
			new[] { ".kkk.", "NyRyN", "NRyRN", ".NyN.", "..N..", "..l.." },
			new[] { ".kkk.", "NRRyN", "NyyRN", ".NRN.", "..N..", "..l.." },
		};
		int[] fTop = Top.Select(t => Add(Draw(f => St(f, 15, Ground - 5, t)))).ToArray();
		int fTopW1 = Add(Draw(f => St(f, 15, Ground - 5, "..kkk", ".NRyR", "NyRyN", "NRN..", "N....", "l....")));
		int fTopW2 = Add(Draw(f => St(f, 15, Ground - 5, "kkk..", "RyRN.", "NyRyN", "..NRN", "....N", "....l")));
		int fTopDown = Add(Draw(f => St(f, 14, Ground - 2, ".kNRN..", "kNyRyNl", ".kNRN..")));
		int topFall = nextChild++; Child(topFall, "top falls", "0", "0", 150, "0", fTopDown, fTopDown, fTopDown, fTopDown, fTopDown, fTopDown).Fade(1, 0);
		int topWob = nextChild++; Child(topWob, "top wobbles", "0", "0", 110, "3", fTopW1, fTopW2); Then(p.Anims.Last(), topFall);
		int topSpin = nextChild++; Child(topSpin, "top spins", "0", "0", 80, "10", fTop); Then(p.Anims.Last(), topWob);
		p.Childs.Add((24, "imageX-imageW*0.7", "imageY", Wait("top", 110, 6, topSpin)));
		int fWind = Add(Pose(armL: 6, armR: 6, look: -1, eyes: 2, mouth: 3, held: f => { St(f, BX + 5, BY + 12, Top[0]); LineAt(f, BX + 18, BY + 9, BX + 10, BY + 14, Line); }));
		int fThrow = Add(Pose(armL: 5, look: -1, eyes: 3, mouth: 1, sway: -1));
		int fWatch1 = Add(Pose(look: -1, eyes: 2, mouth: 3));
		int fWatch2 = Add(Pose(look: -1, eyes: 0, mouth: 3, sway: -1, armL: 2, armR: 1));
		Show(24, "spinning top", 110, (fStand, 1), (fWind, 4), (fThrow, 2), (fWatch1, 6), (fWatch2, 6), (fWatch1, 6), (fWatch2, 6), (fBalClose, 6), (fHappyUp, 4), (fHappy, 2));

		// ============================================================ a galo-de-campina lands on his arm and sings
		string[] Bird = { ".RR....", "RRRR...", "yERRCC.", ".RWCCCk", "..WWCCk", "..WWWkk", ".....kk" };
		string[] BirdUp = { ".RR.CC.", "RRRRCCC", "yERRCCk", ".RWCCCk", "..WWCCk", "..WWWkk", "......." };
		string[] BirdSing = { ".RR....", "RRRR...", "yyRRCC.", "yERCCCk", ".RWWCCk", "..WWWkk", ".....kk" };
		int fBird = Add(Draw(f => St(f, BX - 4, BY + 1, Bird)));
		int fBirdUp = Add(Draw(f => St(f, BX - 4, BY + 1, BirdUp)));
		int fBirdSing = Add(Draw(f => { St(f, BX - 4, BY + 1, BirdSing); NoteAt(f, BX - 6, BY - 4); }));
		int birdOff = nextChild++; Child(birdOff, "galo-de-campina flies off", "-4", "-3", 90, "40", fBirdUp, fBird);
		int birdSit = nextChild++; Child(birdSit, "galo-de-campina sings", "0", "0", 150, "9", fBird, fBirdSing); Then(p.Anims.Last(), birdOff);
		int birdIn = nextChild++; Child(birdIn, "galo-de-campina arrives", "-4", "2", 90, "10", fBirdUp, fBird); Then(p.Anims.Last(), birdSit);
		p.Childs.Add((25, "imageX+imageW*1.375", "imageY-imageH*0.5", birdIn));
		int fPerch = Add(Pose(armL: 1, eyes: 4, look: -1, mouth: 3));
		int fPerch2 = Add(Pose(armL: 1, eyes: 2, mouth: 3, blush: true));
		int fPerch3 = Add(Pose(armL: 1, eyes: 10, mouth: 0, blush: true));
		Then(Show(25, "galo-de-campina", 90, (fBack, 4), (fUpBack, 8), (fUp, 6), (fPerch, 4)), 27);
		Then(Show(27, "bird on arm", 150, (fPerch, 4), (fPerch2, 4), (fPerch3, 4), (fPerch, 4), (fPerch2, 4)), 28);
		Show(28, "bye bird", 150, (fUpFront, 3), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fHappy, 2));

		hub.Then(2, 30).Then(2, 33).Then(1, 34).Then(2, 35).Then(2, 36).Then(1, 39).Then(2, 41).Then(2, 42).Then(2, 43).Then(2, 45).Then(2, 46).Then(1, 47)
			.Then(1, 48).Then(1, 49).Then(1, 50).Then(1, 51).Then(2, 52).Then(2, 53).Then(2, 54).Then(1, 55).Then(2, 59).Then(1, 23).Then(1, 24).Then(2, 25).Then(2, 26);

		// ============================================================ the program's features (60-119): animations it plays by name
		void Bulb(Frame f, int x, int y, bool lit) => St(f, x, y, new[] { ".XXX.", "XXXXX", "XXWXX", ".XXX.", ".lll." }.Select(r => lit ? r : r.Replace('X', 'c').Replace('W', 'C')).ToArray());
		void LoadingRing(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (0, -3), (2, -2), (3, 0), (2, 2), (0, 3), (-2, 2), (-3, 0), (-2, -2) };
			for (int i = 0; i < 8; i++) f.Dot(cx + pts[i].Item1, cy + pts[i].Item2, ((i - k * 2) % 8 + 8) % 8 < 3 ? Blue : Cloud);
		}
		void Confetti(Frame f, int k)
		{
			char[] cs = { Heart, Gold, Blue, Orange, Purple };
			for (int i = 0; i < 20; i++) f.Dot((i * 7 + k * 3) % W, -8 + (i * 5 + k * 4) % 34, cs[(i + k) % cs.Length]);
		}
		void Warning(Frame f, int x, int y) => St(f, x, y, "....k....", "...kYk...", "..kYEYk..", "..kYEYk..", ".kYYEYYk.", ".kYYYYYk.", "kYYYEYYYk", "kkkkkkkkk");
		void Puddle(Frame f, int x, int w) => f.Rect(x, Ground, w, 1, Water).Rect(x + 1, Ground - 1, w - 2, 1, BlueLight);
		Anim FloatUp(string name, int frame, int ms, string loops, string mx = "0") { int id = nextChild++; return Child(id, name, mx, "-2", ms, loops, frame, frame).Fade(1, 0); }

		// think / idea
		Frame ThinkF(int k) => With(Pose(eyes: 4, look: -1, armL: 6, mouth: k == 2 ? 10 : 0), f => Thought(f, k));
		int fTh0 = Add(ThinkF(0)), fTh1 = Add(ThinkF(1)), fTh2 = Add(ThinkF(2));
		Loop2(60, "think", 300, "random/10+3", fTh0, fTh1, fTh2, fTh2);
		int fIdeaOff = Add(With(Pose(eyes: 3, mouth: 1), f => Bulb(f, BX + 2, BY - 7, false)));
		int fIdeaOn = Add(With(Pose(eyes: 2, mouth: 5, armL: 3), f => { Bulb(f, BX + 2, BY - 7, true); Twinkle(f, BX, BY - 5); Twinkle(f, BX + 8, BY - 6); }));
		Show(61, "idea", 160, (fTh1, 4), (fIdeaOff, 3), (fIdeaOn, 1), (fIdeaOff, 1), (fIdeaOn, 6), (fHappy, 3));
		int fBulb = Add(Draw(f => Bulb(f, BX + 2, BY - 7, true)));
		p.Childs.Add((61, "imageX", "imageY", FloatUp("idea bulb", fBulb, 90, "7").Id));
		// read / review (with glasses)
		Loop2(62, "read", 400, "random/10+3", fCorRead1, fCorRead1, fCorRead2, fCorTurn);
		void Glasses(Frame f) { int x = BX, y = BY; f.Rect(x + 8, y + 8, 4, 1, GrayDark).Rect(x + 12, y + 8, 4, 1, GrayDark).Dot(x + 8, y + 9, GrayDark).Dot(x + 8, y + 10, GrayDark).Dot(x + 15, y + 9, GrayDark).Dot(x + 15, y + 10, GrayDark); }
		int fRev1 = Add(Pose(armL: 6, armR: 6, eyes: 12, look: -1, held: f => { St(f, BX + 5, BY + 12, CordelOpen); Glasses(f); }));
		int fRev2 = Add(Pose(armL: 6, armR: 6, eyes: 12, look: 1, held: f => { St(f, BX + 5, BY + 12, CordelOpen); Glasses(f); }));
		var rv3 = Pose(armL: 6, armR: 6, eyes: 2, held: f => { St(f, BX + 5, BY + 12, CordelOpen); Glasses(f); }); Bubble(rv3, "v", 0, -6); int fRev3 = Add(rv3);
		Loop2(63, "review", 320, "random/10+3", fRev1, fRev2, fRev1, fRev2, fRev3, fRev3);
		// laptop on the ground in front of him
		void Laptop(Frame f, int k) { St(f, 0, Ground - 6, "kkkkkkkkk", "kBBBBBBBk", k == 0 ? "kBbbBBBBk" : "kBbbbbBBk", "kBBBBbBBk", "kkkkkkkkk", "lCCCCCCCl", ".lllllll."); }
		int fLap1 = Add(With(Pose(armL: 4, eyes: 12, look: -1), f => Laptop(f, 0)));
		int fLap2 = Add(With(Pose(armL: 9, eyes: 12, look: -1, mouth: 1, squash: 1), f => Laptop(f, 1)));
		int fLap3 = Add(With(Pose(armL: 11, eyes: 12, look: -1, mouth: 0), f => Laptop(f, 1)));
		Loop2(64, "laptop", 140, "random/10+6", fLap1, fLap2, fLap1, fLap3);
		int fCode = Add(Draw(f => St(f, 1, 20, ".g...g.", "g..g..g", ".g.g.g.", "...g...")));
		p.Childs.Add((64, "imageX", "imageY", FloatUp("code floats", fCode, 110, "8", "-1").Id));
		// write in a notebook
		Frame WriteF(int k) => Pose(armL: 6, armR: 6, eyes: 12, mouth: k % 2 == 0 ? 0 : 10, held: f =>
		{
			St(f, BX + 5, BY + 12, ".kkkkkkkkkkk.", "kWWWWWkWWWWWk", "kWWWWWkWWWWWk", "kWWWWWkWWWWWk", "kWWWWWkWWWWWk", ".kkkkkkkkkkk.");
			for (int l = 0; l < 3; l++) f.Rect(BX + 6 + (l > 1 ? 6 : 0), BY + 13 + (l % 2) * 2, l < k ? 4 : 0, 1, Line);
			int px = BX + 9 + k * 2;
			LineAt(f, BX + 18, BY + 9, px + 1, BY + 13, Yel); f.Dot(px, BY + 14, BrownDark);
		});
		Loop2(65, "write", 200, "random/10+3", Add(WriteF(0)), Add(WriteF(1)), Add(WriteF(2)), Add(WriteF(3)));
		// loading: a ring turns over his head
		int[] fLoad = Enumerable.Range(0, 4).Select(k => Add(With(Pose(eyes: k % 2 == 0 ? 4 : 0, look: k == 1 ? -1 : k == 3 ? 1 : 0), f => LoadingRing(f, BX + 11, BY - 5, k)))).ToArray();
		Loop2(66, "loading", 150, "random/10+5", fLoad);
		// download: a box falls from the sky onto his hat
		string[] Box = { "kkkkkkk", "kNNWNNk", "kNNWNNk", "kNWWWNk", "kNNWNNk", "kkkkkkk" };
		int fBoxFall = Add(Draw(f => St(f, BX + 9, 20, Box)));
		int fDlCatch = Add(With(Pose(eyes: 2, mouth: 3, armL: 8, armR: 8, squash: 1), f => St(f, BX + 9, BY - 6, Box)));
		Show(67, "download", 150, (fUp, 7), (fDlCatch, 6), (fHappy, 3));
		int dlBox = nextChild++; Child(dlBox, "download box", "0", "8", 60, "17", fBoxFall);
		p.Childs.Add((67, "imageX", "imageY-imageH*2", dlBox));
		// detective: a magnifying glass over one eye; footprints on the ground
		Frame Detective(int look, bool q)
		{
			var f = Pose(armL: 2, look: look, hatDy: 1, mouth: 4, held: g =>
			{
				LineAt(g, BX + 4, BY + 10, BX + 6, BY + 12, Brown);
				St(g, BX + 6, BY + 6, ".kkkk.", "kbbbbk", "kbbbbk", "kbbbbk", "kbbbbk", ".kkkk.");
				g.Rect(BX + 8 + (look < 0 ? -1 : look > 0 ? 1 : 0), BY + 8, 2, 2, Eye).Dot(BX + 8 + (look < 0 ? -1 : look > 0 ? 1 : 0), BY + 8, White);
			});
			if (q) Bubble(f, "?", 0, -6);
			return f;
		}
		Loop2(68, "detective", 260, "random/10+3", Add(Detective(-1, false)), Add(Detective(-1, false)), Add(Detective(1, false)), Add(Detective(1, false)), Add(Detective(0, true)), Add(Detective(0, true)));
		int fPrints = Add(Draw(f => { St(f, 6, Ground - 1, "n.n", ".n."); St(f, 14, Ground - 1, "n.n", ".n."); St(f, 22, Ground - 1, "n.n", ".n."); }));
		int prints = nextChild++; Child(prints, "footprints", "0", "0", 300, "4", fPrints, fPrints, fPrints).Fade(1, 0);
		p.Childs.Add((68, "imageX-imageW*0.8", "imageY", prints));
		// telescope (luneta): a star twinkles far away
		Frame Telescope(int k) => Pose(armL: 6, eyes: 14, mouth: k == 0 ? 0 : 10, held: f =>
		{
			for (int i = 0; i < 8; i++) { f.Dot(BX + 8 - i, BY + 9 - i, i % 3 == 1 ? Tan : Brown); f.Dot(BX + 9 - i, BY + 9 - i, i % 3 == 1 ? Tan : BrownDark); }
			f.Dot(BX + 1, BY + 1, BlueLight).Dot(BX + 0, BY + 2, BlueLight);
		});
		Loop2(69, "telescope", 300, "random/10+3", Add(Telescope(0)), Add(Telescope(0)), Add(Telescope(1)));
		int fFarStar1 = Add(Draw(f => Spark(f, 8, 4))), fFarStar2 = Add(Draw(f => Twinkle(f, 9, 5)));
		int farStar = nextChild++; Child(farStar, "far star", "0", "0", 250, "8", fFarStar1, fFarStar2).Fade(1, 0);
		p.Childs.Add((69, "imageX-imageW*1.1", "imageY-imageH*0.5", farStar));
		// thumbs up: the arm high (a mandacaru arm is a thumb!)
		var tu = Pose(armL: 3, eyes: 14, mouth: 3); Bubble(tu, "v", 0, -6); Twinkle(tu, BX + 1, BY + 4); int fThumb = Add(tu);
		Show(70, "thumbs up", 200, (fStand, 1), (fThumb, 8), (fHappy, 2));
		// confetti
		int fPopped = Add(With(Pose(eyes: 2, mouth: 5, armL: 3, armR: 3, up: 2, feet: 3, squash: -1), f => Confetti(f, 0)));
		Show(71, "confetti", 140, (fCrouch, 2), (fPopped, 6), (fHappyUp, 2), (fHappy, 3));
		int fConf1 = Add(Draw(f => Confetti(f, 0))), fConf2 = Add(Draw(f => Confetti(f, 1)));
		int conf1 = nextChild++; Child(conf1, "confetti falls", "0", "2", 120, "10", fConf1, fConf2).Fade(1, 0);
		int conf2 = nextChild++; Child(conf2, "confetti falls 2", "1", "2", 140, "9", fConf2, fConf1).Fade(1, 0);
		p.Childs.Add((71, "imageX-imageW*0.4", "imageY-imageH*0.3", conf1));
		p.Childs.Add((71, "imageX+imageW*0.4", "imageY-imageH*0.4", conf2));
		// flag of Brazil waving on a pole
		Frame FlagF(int k) => Pose(armL: 7, eyes: 2, mouth: k == 0 ? 3 : 5, held: f =>
		{
			LineAt(f, BX + 1, BY - 8, BX + 1, BY + 6, Brown);
			string[] fl = { "GGGGGGG", "GGYYYGG", "GYYBYYG", "GGYYYGG", "GGGGGGG" };
			for (int c = 0; c < 7; c++) for (int r = 0; r < 5; r++) f.Dot(BX - 6 + c, BY - 8 + r + ((c + k) % 4 < 2 ? 0 : 1), fl[r][6 - c]);
		});
		Loop2(72, "flag", 200, "random/10+4", Add(FlagF(0)), Add(FlagF(1)), Add(FlagF(2)), Add(FlagF(3)));
		// error
		int fError = Add(With(Pose(eyes: 3, mouth: 2, spines: 2, armL: 3, armR: 3), f => Warning(f, BX + 1, BY - 9)));
		int fScared1 = Add(With(Pose(eyes: 3, mouth: 8, armL: 2, armR: 2, dx: -1, hatDy: 1, spines: 2), f => Sweat(f, BX + 19, BY + 7)));
		int fScared2 = Add(With(Pose(eyes: 3, mouth: 8, armL: 2, armR: 2, dx: 1, hatDy: 1, spines: 2), f => Sweat(f, BX + 20, BY + 8)));
		Show(73, "error", 130, (fWide, 2), (fError, 8), (fScared1, 2), (fScared2, 2), (fBlink, 2), (fStand, 1));
		int fWarn = Add(Draw(f => Warning(f, BX + 1, BY - 9)));
		p.Childs.Add((73, "imageX", "imageY", FloatUp("warning floats", fWarn, 100, "6").Id));
		// cry, angry, shocked, scared
		int fCry1 = Add(With(Pose(eyes: 7, mouth: 4, armL: 4, armR: 4, squash: 1, hatDy: 1), f => { Drop(f, BX + 8, BY + 15); Drop(f, BX + 16, BY + 17); }));
		int fCry2 = Add(With(Pose(eyes: 1, mouth: 2, armL: 4, armR: 4, squash: 2, hatDy: 1), f => { Drop(f, BX + 7, BY + 19); Drop(f, BX + 17, BY + 21); }));
		Loop2(74, "cry", 220, "random/10+4", fCry1, fCry2);
		int fAngry1 = Add(Pose(eyes: 6, mouth: 4, armL: 1, armR: 1, spines: 2, dx: -1));
		int fAngry2 = Add(Pose(eyes: 6, mouth: 8, armL: 2, armR: 2, spines: 2, dx: 1));
		Loop2(76, "angry", 110, "random/10+6", fAngry1, fAngry2);
		int fSteamUp = Add(Draw(f => { Steam(f, BX + 6, BY - 3, 0); Steam(f, BX + 16, BY - 4, 1); }));
		p.Childs.Add((76, "imageX", "imageY", FloatUp("angry steam", fSteamUp, 100, "6").Id));
		int fShock1 = Add(Pose(eyes: 3, mouth: 2, armL: 3, armR: 3, spines: 2, up: 3, feet: 3, squash: -1, hatDy: -4));
		int fShock2 = Add(Pose(eyes: 3, mouth: 2, armL: 7, armR: 7, spines: 2, hatDy: -1));
		Show(77, "shocked", 110, (fStand, 1), (fCrouch, 1), (fShock1, 3), (fShock2, 6), (fBlink, 2), (fStand, 1));
		Loop2(78, "scared", 70, "random/10+10", fScared1, fScared2);
		// talk: he stays in place
		int fTalk1 = Add(Pose(mouth: 1)), fTalk2 = Add(Pose(mouth: 1, armL: 1)), fTalk3 = Add(Pose(mouth: 3, eyes: 2));
		p.A(79, "talk").Move("0", "0").Speed("160").Loop("random/3+8", 0).Seq(fTalk1, fStand, fTalk1, fTalk2, fStand, fTalk3).Then(100, 11);
		// umbrella: a colourful frevo umbrella in the rain
		Frame UmbF(int k) => With(Pose(armL: 7, eyes: k == 0 ? 4 : 2, mouth: 3, held: f =>
		{
			LineAt(f, BX + 1, BY - 3, BX + 1, BY + 6, Brown);
			St(f, BX - 5, BY - 8, "....RRYYB....", "..RRRRYYBBG..", ".RRRRRYYBBGG.", "RRRRRRYYBBGGG", "R.R.R.N.B.G.G");
		}), f => { foreach (var (dx2, dy2) in new[] { (0, 12), (28, 6), (31, 16), (2, 24), (33, 26) }) Drop(f, dx2, dy2 + k * 3); });
		Loop2(80, "umbrella", 220, "random/10+5", Add(UmbF(0)), Add(UmbF(1)));
		// puddle splash
		int fPud0 = Add(With(Pose(eyes: 2, mouth: 3, look: -1), f => Puddle(f, 4, 26)));
		int fPudC = Add(With(Pose(eyes: 1, armL: 4, armR: 4, squash: 2, feet: 4), f => Puddle(f, 4, 26)));
		int fPudJ = Add(With(Pose(eyes: 2, mouth: 5, armL: 3, armR: 3, up: 6, feet: 3, squash: -1), f => Puddle(f, 4, 26)));
		int fPudS = Add(With(Pose(eyes: 13, mouth: 5, armL: 7, armR: 7, squash: 2, feet: 4), f => { Puddle(f, 2, 30); foreach (var (sx, sy) in new[] { (1, 28), (4, 25), (30, 26), (33, 29), (8, 23), (27, 22) }) Drop(f, sx, sy); }));
		Then(Show(81, "puddle splash", 140, (fPud0, 4), (fPudC, 2), (fPudJ, 4)), 82);
		Show(82, "splash", 140, (fPudS, 6), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		int fSplash = Add(Draw(f => { foreach (var (sx, sy) in new[] { (3, 27), (6, 30), (1, 32) }) Drop(f, sx, sy); }));
		int spl1 = nextChild++; Child(spl1, "splash left", "-3", "-1", 80, "4", fSplash, fSplash).Fade(1, 0);
		int spl2 = nextChild++; Child(spl2, "splash right", "3", "-1", 80, "4", fSplash, fSplash).Fade(1, 0);
		p.Childs.Add((82, "imageX", "imageY", spl1));
		p.Childs.Add((82, "imageX+imageW*0.4", "imageY", spl2));
		// hot, sunbathing
		int fHot1 = Add(With(Pose(eyes: 5, mouth: 6, armL: 4, armR: 4), f => { SunAt(f, 6, 3, 0, 6); Sweat(f, BX + 18, BY + 8); }));
		int fHot2 = Add(With(Pose(eyes: 1, mouth: 6, armL: 4, armR: 4, squash: 1), f => { SunAt(f, 6, 3, 1, 6); Sweat(f, BX + 18, BY + 11); }));
		Loop2(83, "hot", 240, "random/10+4", fHot1, fHot2);
		void Shades(Frame f) { f.Rect(BX + 8, BY + 9, 8, 1, Eye).Rect(BX + 9, BY + 10, 2, 1, Eye).Rect(BX + 13, BY + 10, 2, 1, Eye).Dot(BX + 9, BY + 9, White).Dot(BX + 13, BY + 9, White); }
		int fSun1b = Add(With(Pose(eyes: -1, mouth: 3, armL: 2, armR: 2, held: Shades), f => SunAt(f, 6, 3, 0, 6)));
		int fSun2b = Add(With(Pose(eyes: -1, mouth: 0, armL: 2, armR: 2, squash: 1, held: Shades), f => SunAt(f, 6, 3, 1, 6)));
		Loop2(84, "sunbathing", 500, "random/10+3", fSun1b, fSun1b, fSun2b);
		// ice cream
		var ice = Eating(IceCream).ToList(); ice.Add((fHappy, 3));
		Show(85, "ice cream", 200, ice.ToArray());
		// cold, shiver, scarf, snow cap
		int fCold1 = Add(With(Pose(eyes: 5, mouth: 8, armL: 2, armR: 2, dx: -1, squash: 1), f => St(f, BX + 2, BY + 10, "c.", ".c")));
		int fCold2 = Add(With(Pose(eyes: 5, mouth: 8, armL: 2, armR: 2, dx: 1, squash: 1), f => St(f, BX + 1, BY + 9, ".c", "c.")));
		Loop2(86, "cold", 70, "random/10+10", fCold1, fCold2);
		int fShiv1 = Add(Pose(eyes: 1, mouth: 8, armL: 4, armR: 4, dx: -1, spines: 2));
		int fShiv2 = Add(Pose(eyes: 1, mouth: 8, armL: 4, armR: 4, dx: 1, spines: 2));
		Loop2(87, "shiver", 60, "random/10+10", fShiv1, fShiv2);
		void Scarf(Frame f) { St(f, BX + 7, BY + 12, "RWRWRWRWRW"); St(f, BX + 9, BY + 13, "WR", "RW", "WR"); }
		Loop2(88, "scarf", 360, "random/10+3", Add(Pose(eyes: 10, mouth: 3, held: Scarf)), Add(Pose(eyes: 2, mouth: 3, squash: 1, held: Scarf)));
		void Beanie(Frame f) => St(f, BX + 8, BY + 2, "...WW...", ".RRRRRR.", "RWRWRWRW", "RRRRRRRR", "WWWWWWWW");
		int fBean1 = Add(Pose(hat: 0, eyes: 2, mouth: 3, held: Beanie)), fBean2 = Add(Pose(hat: 0, eyes: 4, mouth: 10, held: Beanie));
		Loop2(89, "snow cap", 320, "random/10+4", fBean1, fBean2);
		Frame SnowF(int k) => Draw(f => { foreach (var (sx, sy) in new[] { (2, -6), (9, -1), (19, -4), (29, 0), (5, 8), (31, 12), (14, -8), (24, 6) }) f.Dot(sx, ((sy + k * 3) + 60) % 30 - 8, Zz); });
		int snow = nextChild++; Child(snow, "snowfall", "0", "1", 200, "10", Add(SnowF(0)), Add(SnowF(1))).Fade(1, 0);
		p.Childs.Add((89, "imageX-imageW*0.5", "imageY", snow));
		// rain cloud: the little cloud rains on him, and in the sertão that is a party
		Show(90, "rain cloud", 160, (fUp, 6), (fHope, 2), (fWet1, 2), (fWet2, 2), (fWet3, 2), (fWet2, 2), (fWet1, 2), (fWet3, 2), (fHappy, 3));
		p.Childs.Add((90, "imageX", "imageY", cloudRain));
		// look up, surprised, tired, sleepy
		Show(91, "look up", 280, (fStand, 1), (fUp, 6), (fUpFront, 2), (fUp, 4), (fStand, 1));
		var su = Pose(eyes: 3, mouth: 2, up: 1, armL: 3, armR: 3, spines: 2, feet: 3); Bubble(su, "OXE!", 0, -7); int fSurprised = Add(su);
		Show(92, "surprised", 120, (fStand, 2), (fSurprised, 8), (fWide, 3), (fStand, 1));
		int fTired1 = Add(Pose(eyes: 5, mouth: 0, armL: 4, armR: 4, squash: 1, sway: 1));
		int fTired2 = Add(Pose(eyes: 1, mouth: 1, armL: 4, armR: 4, squash: 2, sway: 1, hatDy: 1));
		Loop2(93, "tired", 420, "random/10+3", fTired1, fTired2);
		int fSleepy = Add(With(Pose(eyes: 1, armL: 4, armR: 4, squash: 2, hatDy: 2), f => Zed(f, BX + 19, BY - 2, false)));
		Show(94, "sleepy", 300, (fTired1, 3), (fTired2, 3), (fSleepy, 4), (fWide, 1), (fTired1, 3), (fSleepy, 5), (fBlink, 2), (fStand, 1));
		// drink (water in an enamel mug), eat (corn), cookie, coffee
		var drink = Eating(Mug(Water)).ToList(); drink.Add((fHappy, 3));
		Show(95, "drink", 200, drink.ToArray());
		var corn = Eating(Corn).ToList(); corn.Add((fHappy, 3));
		Show(96, "eat", 180, corn.ToArray());
		var cookie = Eating(Cookie).ToList(); cookie.Add((fHappy, 3));
		Show(97, "cookie", 180, cookie.ToArray());
		var coffee = Eating(Mug(BrownDark)).ToList(); coffee.Add((fHappy, 3));
		Show(114, "coffee", 220, coffee.ToArray());
		int fCupSteam = Add(Draw(f => Steam(f, BX - 2, BY + 1, 0)));
		p.Childs.Add((114, "imageX", "imageY", FloatUp("coffee steam", fCupSteam, 140, "6").Id));
		// sweat, dizzy, spin, nod, ok
		int fSweat1 = Add(With(Pose(eyes: 5, mouth: 4, armL: 6), f => { Sweat(f, BX + 18, BY + 7); Sweat(f, BX + 3, BY + 4); }));
		int fSweat2 = Add(With(Pose(eyes: 1, mouth: 0, armL: 2), f => Sweat(f, BX + 19, BY + 10)));
		Loop2(98, "sweat", 180, "random/10+4", fSweat1, fSweat2);
		Frame Dizzy(int k) => With(Pose(eyes: 8, mouth: 7, sway: k == 1 ? 1 : -1, armL: k == 1 ? 1 : 2, armR: k == 1 ? 2 : 1), f => Stars(f, k));
		Loop2(100, "dizzy", 130, "random/10+5", Add(Dizzy(0)), Add(Dizzy(1)), Add(Dizzy(2)));
		Loop2(101, "spin", 90, "random/10+3", fStand, fSide1, fBackView, fSide2);
		int fNod = Add(Pose(eyes: 12, mouth: 3, squash: 1, hatDy: 1));
		Show(102, "nod", 180, (fStand, 1), (fNod, 2), (fStand, 2), (fNod, 2), (fHappy, 2));
		var okf = Pose(eyes: 2, mouth: 3, armL: 3); Bubble(okf, "OK", 0, -6); int fOk = Add(okf);
		Show(103, "ok", 200, (fStand, 1), (fOk, 8), (fHappy, 2));
		// cheer, party, jump, happy
		int fCheer2 = Add(Pose(eyes: 2, mouth: 5, armL: 3, armR: 3, up: 3, feet: 3, squash: -1));
		int fCheer3 = Add(Pose(eyes: 13, mouth: 5, armL: 7, armR: 7, up: 4, feet: 3));
		Loop2(104, "cheer", 110, "random/10+4", fCrouch, fCheer2, fCheer3, fCheer2, fTouch);
		int fParty1 = Add(With(Pose(eyes: 2, mouth: 10, armL: 2, held: f => St(f, BX + 8, BY + 11, "YR", "RY")), f => Confetti(f, 0)));
		int fParty2 = Add(With(Pose(eyes: 13, mouth: 10, armL: 1, held: f => { for (int i = 0; i < 9; i++) f.Dot(BX + 11 - i, BY + 11, i % 2 == 0 ? Red : Yel); }), f => Confetti(f, 1)));
		Loop2(105, "party", 160, "random/10+5", fParty1, fParty2);
		p.Childs.Add((105, "imageX-imageW*0.3", "imageY-imageH*0.3", conf1));
		Show(106, "jump", 80, (fCrouch, 2), (fTakeoff, 1), (fAir1, 1), (fApex, 2), (fAir2, 1), (fAir3, 1), (fTouch, 2), (fRebound, 1), (fHappy, 3));
		int fHappy2 = Add(With(Pose(eyes: 2, mouth: 5, up: 1, armL: 7, armR: 7, squash: -1, feet: 3), f => { Twinkle(f, BX - 2, BY + 6); Twinkle(f, BX + 25, BY + 8); }));
		Loop2(107, "happy", 150, "random/10+4", fHappy, fHappy2, fHappy, fRebound);
		// hug, hearts, love
		int fHug1 = Add(With(Pose(eyes: 10, mouth: 3, armL: 6, armR: 6, blush: true), f => SmallHeart(f, BX + 10, BY + 14)));
		int fHug2 = Add(With(Pose(eyes: 2, mouth: 3, armL: 6, armR: 6, blush: true, squash: 1), f => { SmallHeart(f, BX + 10, BY + 15); HeartAt(f, BX + 1, BY - 6); }));
		Loop2(108, "hug", 320, "random/10+3", fHug1, fHug2);
		int fLove1 = Add(Pose(eyes: 9, mouth: 3, blush: true, sway: -1, armL: 2, armR: 1));
		int fLove2 = Add(With(Pose(eyes: 9, mouth: 10, blush: true, sway: 1, armL: 1, armR: 2), f => HeartAt(f, BX + 1, BY - 6)));
		Loop2(109, "hearts", 260, "random/10+3", fLove1, fLove2);
		int fHeartC = Add(Draw(f => HeartAt(f, BX + 1, BY - 4)));
		int fHeartC2 = Add(Draw(f => SmallHeart(f, BX + 20, BY - 2)));
		p.Childs.Add((109, "imageX", "imageY", FloatUp("heart floats", fHeartC, 100, "7", "-1").Id));
		p.Childs.Add((109, "imageX", "imageY", FloatUp("heart floats 2", fHeartC2, 120, "6", "1").Id));
		Loop2(110, "love", 260, "random/10+3", fLove2, fLove1);
		// sweep (straw broom), work (hoe)
		Frame SweepF(int k) => With(Pose(armL: 4, look: -1, eyes: 12, mouth: k == 0 ? 3 : 0), f =>
		{
			LineAt(f, BX + 2, BY + 19, BX - 3 + k, BY + 21, Brown);
			St(f, BX - 6 + k, Ground - 2, ".yyyy", "yYyYy", "YyYyY");
		});
		Loop2(111, "sweep", 160, "random/10+6", Add(SweepF(0)), Add(SweepF(-2)));
		int fDust = Add(Draw(f => Dust(f, 1, Ground - 4)));
		p.Childs.Add((111, "imageX", "imageY", FloatUp("sweep dust", fDust, 120, "6", "-2").Id));
		int fWork1 = Add(Pose(armL: 3, eyes: 2, mouth: 3, look: -1, held: f => { LineAt(f, BX + 2, BY + 7, BX - 3, BY + 1, Brown); St(f, BX - 6, BY - 1, "CCk", "CC.", "C.."); }));
		int fWork2 = Add(With(Pose(armL: 4, eyes: 1, mouth: 9, look: -1, squash: 1), f => { LineAt(f, BX + 2, BY + 19, BX - 3, BY + 22, Brown); St(f, BX - 6, Ground - 2, "kC.", "CC.", "CCC"); Dust(f, 0, Ground - 5); }));
		Loop2(112, "work", 170, "random/10+6", fWork1, fWork1, fWork2, fWork2);
		// music: the sanfona; dance; laugh; stretch; yawn; curious
		Loop2(113, "music", 130, "random/10+3", play.Take(8).ToArray());
		Notes(113, "music", 130, 2, 6);
		Loop2(115, "dance", 140, "random/10+5", fArrasta1, fQuad1, fArrasta2, fQuad2);
		Loop2(116, "laugh", 130, "random/10+4", fLaugh1, fLaugh2);
		int fStretch1 = Add(Pose(armL: 3, armR: 3, eyes: 1, squash: -1));
		int fStretch2 = Add(Pose(armL: 3, armR: 3, eyes: 1, mouth: 2, squash: -2, hatDy: -1));
		Show(117, "stretch", 220, (fStand, 1), (fStretch1, 2), (fStretch2, 5), (fStretch1, 1), (fWave1, 2), (fStand, 1));
		int fYawn = Add(Pose(eyes: 1, mouth: 2, armL: 6, squash: -1));
		Show(118, "yawn", 220, (fStand, 1), (fYawn, 5), (fBlink, 2), (fStand, 1));
		var cu1 = Pose(eyes: 4, look: -1, sway: -1, armL: 2, armR: 1); Bubble(cu1, "?", 0, -6); int fCurious1 = Add(cu1);
		var cu2 = Pose(eyes: 0, look: 1, sway: 1, armL: 1, armR: 2); Bubble(cu2, "?", 0, -6); int fCurious2 = Add(cu2);
		Loop2(119, "curious", 320, "random/10+3", fCurious1, fCurious2);
		foreach (int id in new[] { 60, 62, 64, 69, 72, 75, 80, 84, 96, 101, 105, 107, 109, 111, 112, 113, 115, 117, 118, 119 }) hub.Then(1, id);

		p.Readme = """
			# Mandacaru the Cactus

			## Description

			A cactus of the Brazilian sertão (the dry backlands of the Northeast) in pixel art: a tall green trunk with a
			face, two arms raised like a real mandacaru, little spines, root feet and a northeastern leather hat
			(chapéu de couro) with a gold star.

			He waddles from root to root, turns around when he reaches the edge of the screen (he never climbs it), plays
			forró on his sanfona and dances xote, plays the triangle and the zabumba, suffers under the strong sun and fans
			himself with his hat, celebrates when the long-awaited rain comes and his white flowers bloom, and watches his
			flowers open at night under the full moon while a bat visits them.

			## About pet

			Author: Luan Felipe

			## Animations

			* waddle (8 steps), run, start and stop walking, turn around at the borders, a bump on the border, fall and land
			  (the hat falls over his eyes), sway in the wind, sit, sleep with the hat over his eyes
			* forró on the sanfona (notes float up), xote, triangle, zabumba
			* the strong sun with heat waves, the rain cloud and his white flowers blooming, the full moon and a bat
			* the asa-branca flying by, the carcará circling over him, a calango doing push-ups (he does them too),
			  a jegue walking by (he tips his hat), a goat that tries to eat him, a galo-de-campina landing on his arm
			* a cordel booklet, cuscuz, rapadura (sweet but hard!), tapioca, corn, a coffee in an enamel mug
			* São João: the bonfire and the party flags (quadrilha: "anarriê!")
			* a toy balloon that pops on his spines, a sneeze that sends his spines flying, a hat trick, a dust devil that
			  steals his hat (he runs after it), a shooting star, a spinning top
			* reacts to the app: thinks, reads, types on a laptop, writes, a detective's magnifying glass, a telescope,
			  drinks water on the water reminder, umbrella, scarf, snow cap and sweat with the weather, party at the end of
			  a Pomodoro
			* drag and toss with the mouse
			""";
		p.ReadmePt = """
			# Mandacaru, o Cacto

			## Descrição

			Um cacto do sertão nordestino em pixel art: um tronco alto e verde com carinha, dois braços erguidos como um
			mandacaru de verdade, espinhos, pezinhos de raiz e um chapéu de couro de vaqueiro com uma estrela dourada.

			Ele anda gingando de raiz em raiz, dá meia-volta quando chega na borda da tela (nunca escala), toca forró na
			sanfona e dança xote, toca triângulo e zabumba, sofre com o sol forte e se abana com o chapéu, comemora quando a
			chuva tão esperada chega e as flores brancas desabrocham nos braços dele, e vê as flores abrirem à noite sob a
			lua cheia enquanto um morcego as visita.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* andar gingando (8 passos), correr, começar e parar de andar, virar nas bordas, trombar na borda, cair e pousar
			  (o chapéu cai nos olhos), balançar ao vento, sentar, dormir com o chapéu sobre os olhos
			* forró na sanfona (notas flutuam), xote, triângulo, zabumba
			* o sol forte com ondas de calor, a nuvem de chuva e as flores brancas desabrochando, a lua cheia e um morcego
			* a asa-branca passando, o carcará rodeando, um calango fazendo flexões (ele faz junto), um jegue passando
			  (ele tira o chapéu), um bode que tenta comê-lo, um galo-de-campina que pousa no braço dele
			* folheto de cordel, cuscuz, rapadura (é doce, mas não é mole!), tapioca, milho, café na caneca de ágata
			* São João: a fogueira e as bandeirinhas (quadrilha: "anarriê!")
			* um balão de festa que estoura nos espinhos, um espirro que faz os espinhos voarem, truque com o chapéu, um
			  redemoinho que leva o chapéu (ele corre atrás), uma estrela cadente, um pião
			* reage ao programa: pensa, lê, digita no notebook, escreve, lupa de detetive, luneta, bebe água no lembrete de
			  água, guarda-chuva, cachecol, gorro e suor conforme o clima, festa no fim do Pomodoro
			* arrastar e jogar com o mouse
			""";

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
