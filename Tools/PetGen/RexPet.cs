using System.Drawing;

namespace PetGen;

/// <summary>
/// "Rex" — a little green pixel dinosaur for DesktopPet. The standing pose is a pixel-exact copy
/// of the reference art the user provided (25 x 26 pixels, read cell by cell from the image).
/// Every other pose is built from it by editing body parts in art coordinates (eye, jaw, arms,
/// feet, tail) and then transforming the whole figure (bob, crouch, lean, lie down, roll over);
/// props and costumes from the reference sheets are drawn on top. Several scenes release an
/// object as a separate "child" window that moves on its own.
/// Frames are 32 px wide with 6 rows of headroom; the feet stand on row 31.
/// </summary>
static class RexPet
{
	const int W = 32, Head = 6, AX = 4, AY = 6;

	// The reference dinosaur, exactly as in the user's image ('.' = transparent). Faces left.
	static readonly string[] Art =
	{
		".......GGGGG.............",   //  0
		".....GGGGGGMMOO..........",   //  1
		"..GGGGGGGGGGMMR..........",   //  2
		".GGGGGGGEMGGGMD..........",   //  3 eye
		"GGGMGGGGEMGGGMDOO........",   //  4 eye, nostril
		"GGGGGGGGGGGGGMDRR........",   //  5
		"GGGGGGGGGGGGGMDR.........",   //  6
		"GGGGGGGGDGGGGMD..........",   //  7 mouth corner
		".DDDDDDDMGGGMMDO.........",   //  8 jaw
		"..MMMMGGGGGGMMRR.........",   //  9 chin
		".......MMMGGMMR..........",   // 10
		".......MMGGGGM...........",   // 11
		"......MGGGGGGMMOO........",   // 12
		"...DDDGGGMGGGGMMR........",   // 13 near arm
		"..DDDMGMMMGGGGGMM.OO...MD",   // 14
		"..DD.MMMDDGGGGGGMMRR.GGMD",   // 15
		".....DDDGGGGGGGGGGGGGGMMD",   // 16
		".....MGGGGMGGGGGMGGGGMMD.",   // 17
		".....MGGGGGGGGGGMMMMMMD..",   // 18
		".....MMGGDGGGGGGDMMMMD...",   // 19
		"......DMMDGGGGGMDDDDD....",   // 20
		"......DDMDMGGGMDDDD......",   // 21 legs
		"......DDDDDDGGMD.........",   // 22
		".....DMMDD.MGGMD.........",   // 23
		"....DDDDD.DGGMMD.........",   // 24 front foot
		"..........DDDDDD.........",   // 25 back foot
	};
	const int AR = 26, AC = 25;

	// palette characters
	const char G = 'G', M = 'M', D = 'D', O = 'O', R = 'R', E = 'E';
	const char Wt = 'W', Mouth = 'X', Tongue = 'T', Pink = 'F', Water = 'w', Glass = 'c', Teal = 'C';
	const char Brown = 'B', BrownD = 'b', Yel = 'Y', YelD = 'y', Blk = 'K', GrayD = 'k', Gray = 'g', GrayL = 'l';
	const char Purp = 'P', PurpL = 'p', Navy = 'N', Red = 'H', RedL = 'h', Olive = 'A', Zz = 'Z', Blue = 'U', Mint = 'u';
	// fire, rock and flag colors (meteor, fireworks, fire breath, Brazil flag)
	const char Hot = 'q', Flame = 'j', FlameR = 'J', Ember = 'v', RockD = 's', RockM = 'S', RockL = 'r', Ash = 'a', FlagG = 'L';

	sealed class Look
	{
		public int Eye;        // 0 open, 1 closed, 2 happy ^, 3 wide, 4 angry, 5 look up, 6 squeezed >, 7 half (tired)
		public int Jaw;        // 0 closed, 1 open, 2 roar (wide, teeth), 3 tongue out, 4 "o"
		public int Arm;        // near arm: 0 as drawn, 1 raised, 2 reaching forward, 3 tucked (none)
		public bool FarUp;     // far arm raised too
		public int Feet;       // 0 both down, 1 front foot up, 2 back foot up, 3 both tucked (jump)
		public int Tail;       // 0, 1 up, -1 down
		public bool Blush;
	}

	public static PetDef Build()
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		var p = new PetDef
		{
			Folder = "rex",
			Title = "Rex the Dino",
			PetName = "Rex",
			Author = "Luan Felipe",
			Version = "1.0",
			Info = "Rex is a little green pixel dinosaur with orange spikes.[br]" +
				   "He stomps, roars, plays ball, dresses up as a wizard, a chef, an astronaut...[br]" +
				   "and some of his toys fly off on their own.",
			Palette = new Dictionary<char, Color>
			{
				[G] = Color.FromArgb(155, 188, 96), [M] = Color.FromArgb(66, 130, 68), [D] = Color.FromArgb(32, 84, 48),
				[O] = Color.FromArgb(240, 144, 44), [R] = Color.FromArgb(204, 108, 48), [E] = Color.FromArgb(0, 0, 0),
				[Wt] = Color.FromArgb(255, 255, 255), [Mouth] = Color.FromArgb(110, 30, 40), [Tongue] = Color.FromArgb(240, 110, 130),
				[Pink] = Color.FromArgb(250, 170, 200), [Water] = Color.FromArgb(110, 190, 245), [Glass] = Color.FromArgb(200, 232, 250),
				[Teal] = Color.FromArgb(80, 160, 180), [Brown] = Color.FromArgb(150, 96, 52), [BrownD] = Color.FromArgb(100, 62, 34),
				[Yel] = Color.FromArgb(250, 210, 60), [YelD] = Color.FromArgb(210, 160, 30), [Blk] = Color.FromArgb(30, 30, 34),
				[GrayD] = Color.FromArgb(90, 90, 98), [Gray] = Color.FromArgb(150, 150, 158), [GrayL] = Color.FromArgb(214, 214, 220),
				[Purp] = Color.FromArgb(130, 80, 190), [PurpL] = Color.FromArgb(190, 150, 240), [Navy] = Color.FromArgb(40, 60, 120),
				[Red] = Color.FromArgb(220, 50, 60), [RedL] = Color.FromArgb(255, 130, 130), [Olive] = Color.FromArgb(118, 128, 60),
				[Zz] = Color.FromArgb(40, 96, 56), [Blue] = Color.FromArgb(60, 120, 220), [Mint] = Color.FromArgb(120, 210, 150),
				[Hot] = Color.FromArgb(255, 248, 205), [Flame] = Color.FromArgb(255, 116, 30), [FlameR] = Color.FromArgb(212, 50, 30),
				[Ember] = Color.FromArgb(128, 30, 30), [RockD] = Color.FromArgb(56, 44, 42), [RockM] = Color.FromArgb(100, 78, 66),
				[RockL] = Color.FromArgb(150, 118, 94), [Ash] = Color.FromArgb(70, 68, 76), [FlagG] = Color.FromArgb(20, 150, 70),
			},
			Scale = 2,
			TilesX = 8,
		};

		// ============================================================ body (art coordinates)
		char[,] Body(Look s)
		{
			var a = new char[AR, AC];
			for (int r = 0; r < AR; r++) for (int c = 0; c < AC; c++) a[r, c] = Art[r][c];
			void Set(int r, int c, char ch) { if (r >= 0 && r < AR && c >= 0 && c < AC) a[r, c] = ch; }

			// --- tail (cols 17..24) moves as a block
			if (s.Tail != 0)
			{
				var t = (char[,])a.Clone();
				for (int r = 12; r <= 20; r++) for (int c = 18; c < AC; c++) a[r, c] = '.';
				for (int r = 12; r <= 20; r++) for (int c = 18; c < AC; c++)
				{
					char ch = t[r, c]; if (ch == '.') continue;
					int nr = r - s.Tail * (c >= 21 ? 2 : 1);
					if (nr >= 0 && nr < AR) a[nr, c] = ch;
				}
				for (int r = 12; r <= 20; r++) if (a[r, 18] == '.' && a[r, 17] != '.' && a[r, 17] != 'O' && a[r, 17] != 'R') a[r, 17] = D;
			}
			// --- feet
			void LiftCols(int c0, int c1, int rows)
			{
				var t = (char[,])a.Clone();
				for (int r = 21; r < AR; r++) for (int c = c0; c <= c1; c++) a[r, c] = '.';
				for (int r = 21; r < AR; r++) for (int c = c0; c <= c1; c++) { char ch = t[r, c]; if (ch != '.') Set(r - rows, c, ch); }
			}
			if (s.Feet == 1 || s.Feet == 3) LiftCols(4, 9, s.Feet == 3 ? 2 : 1);
			if (s.Feet == 2 || s.Feet == 3) LiftCols(10, 15, s.Feet == 3 ? 2 : 1);

			// --- near arm
			if (s.Arm != 0)
			{
				foreach (var (r, c) in new[] { (13, 3), (13, 4), (13, 5), (14, 2), (14, 3), (14, 4), (15, 2), (15, 3) }) a[r, c] = '.';
				if (s.Arm == 1) { Set(13, 5, D); Set(13, 4, D); Set(12, 3, D); Set(12, 4, M); Set(11, 2, D); Set(11, 3, M); Set(10, 1, D); Set(10, 2, D); Set(9, 1, D); Set(9, 0, D); Set(11, 1, D); }
				if (s.Arm == 2) { Set(13, 5, D); Set(13, 4, D); Set(13, 3, D); Set(13, 2, D); Set(12, 1, D); Set(14, 1, D); }
			}
			// --- far arm raised
			if (s.FarUp)
			{
				foreach (var (r, c) in new[] { (14, 7), (14, 8), (14, 9), (15, 5), (15, 6), (15, 7), (15, 8), (15, 9) }) a[r, c] = G;
				a[15, 5] = M; a[14, 7] = G;
				Set(12, 9, M); Set(11, 10, D); Set(10, 10, D); Set(9, 10, M); Set(13, 9, M);
			}
			// --- jaw
			if (s.Jaw == 1 || s.Jaw == 2 || s.Jaw == 3 || s.Jaw == 4)
			{
				int n = s.Jaw == 2 ? 2 : 1;
				var jaw = new List<(int r, int c, char ch)>();
				for (int c = 1; c <= 6; c++) jaw.Add((8, c, a[8, c]));
				for (int c = 2; c <= 5; c++) jaw.Add((9, c, a[9, c]));
				foreach (var j in jaw) a[j.r, j.c] = Mouth;
				foreach (var j in jaw) Set(j.r + n, j.c, j.ch);
				if (s.Jaw == 4) { for (int c = 1; c <= 3; c++) a[8, c] = G; a[9, 2] = '.'; a[9, 3] = '.'; Set(10, 2, '.'); }
				if (s.Jaw == 2) { a[8, 2] = Wt; a[8, 4] = Wt; a[8, 6] = Wt; a[9, 3] = Wt; a[9, 5] = Wt; a[9, 1] = '.'; a[9, 6] = Mouth; }
				if (s.Jaw == 3) { Set(9, 1, Tongue); Set(10, 1, Tongue); Set(10, 0, Tongue); Set(11, 1, Tongue); }
			}
			// --- eye (pupil at 3..4,8; socket shading at 3..4,9)
			void EyeClear() { a[3, 8] = G; a[4, 8] = G; a[3, 9] = G; a[4, 9] = G; }
			switch (s.Eye)
			{
				case 1: EyeClear(); a[4, 7] = E; a[4, 8] = E; a[4, 9] = E; break;
				case 2: EyeClear(); a[3, 8] = E; a[4, 7] = E; a[4, 9] = E; break;
				case 3: EyeClear(); for (int r = 2; r <= 4; r++) for (int c = 7; c <= 9; c++) a[r, c] = Wt; a[3, 8] = E; a[1, 7] = D; break;
				case 4: a[2, 9] = E; a[2, 8] = E; a[1, 10] = E; break;
				case 5: EyeClear(); a[2, 8] = E; a[3, 8] = E; a[2, 9] = M; a[3, 9] = M; break;
				case 6: EyeClear(); a[3, 7] = E; a[4, 8] = E; a[5, 7] = E; break;
				case 7: a[3, 8] = M; a[3, 9] = M; a[2, 8] = D; a[2, 9] = D; break;
			}
			if (s.Blush) { Set(6, 9, Pink); Set(6, 10, Pink); }
			return a;
		}
		Frame Place(char[,] a, int dx = 0, int dy = 0)
		{
			var f = new Frame();
			for (int r = 0; r < AR; r++) for (int c = 0; c < AC; c++) if (a[r, c] != '.') f[c + AX + dx, r + AY + dy] = a[r, c];
			return f;
		}
		Frame Pose(int eye = 0, int jaw = 0, int arm = 0, bool farUp = false, int feet = 0, int tail = 0, bool blush = false, int dy = 0)
			=> Place(Body(new Look { Eye = eye, Jaw = jaw, Arm = arm, FarUp = farUp, Feet = feet, Tail = tail, Blush = blush }), 0, dy);

		// ============================================================ whole-figure transforms (frame coords)
		Frame Squash(Frame f, int n)            // crouch / sit: everything above the feet drops n rows
		{
			var s = f.Clone(); int feet = 28;
			for (int y = f.MinY; y < feet; y++) for (int x = 0; x < W; x++) f[x, y] = '.';
			for (int y = f.MinY; y < feet; y++) for (int x = 0; x < W; x++) if (s[x, y] != '.') f[x, Math.Min(feet - 1, y + n)] = s[x, y] == '.' ? f[x, Math.Min(feet - 1, y + n)] : s[x, y];
			for (int y = feet - n; y < feet; y++) for (int x = 0; x < W; x++) if (f[x, y] == '.' && s[x, y] != '.') f[x, y] = s[x, y];
			return f;
		}
		Frame Lean(Frame f, int dx, int belowY)  // shear: rows above belowY shift sideways (more at the top)
		{
			var s = f.Clone();
			for (int y = f.MinY; y < belowY; y++) for (int x = 0; x < W; x++) f[x, y] = '.';
			for (int y = f.MinY; y < belowY; y++)
			{
				int d = (int)Math.Round(dx * (double)(belowY - y) / (belowY - 6));
				for (int x = 0; x < W; x++) if (s[x, y] != '.') f[x + d, y] = s[x, y];
			}
			return f;
		}
		Frame Rotate(Frame f, bool cw)            // 90 degrees around the art (for lying on the back / flying)
		{
			var s = f.Clone(); var o = new Frame();
			for (int y = 6; y < 32; y++) for (int x = 0; x < W; x++)
			{
				char ch = s[x, y]; if (ch == '.') continue;
				int ry = y - 6, rx = x - 3;   // 0..25, 0..28
				int nx = cw ? 31 - ry : ry + 3, ny = cw ? rx + 3 : 28 - rx + 3;
				o[nx, ny] = ch;
			}
			return o;
		}
		// curled up lying pose (sleep): low body hump, head resting on the ground, tail along the floor
		Frame Lying(int eye, int z)
		{
			var f = new Frame();
			// body hump
			for (int y = 22; y <= 31; y++) for (int x = 10; x <= 27; x++)
			{
				double dx = (x - 18.5) / 9.0, dy = (y - 31.5) / 9.5;
				if (dx * dx + dy * dy <= 1) f[x, y] = dy > -0.25 && x > 22 ? M : G;
			}
			// tail
			for (int x = 26; x <= 31; x++) { f[x, 30] = G; f[x, 31] = M; }
			f[31, 29] = M;
			// outline + spikes
			var s = f.Clone();
			for (int y = 20; y <= 31; y++) for (int x = 0; x < W; x++)
				if (s[x, y] != '.' && (s[x - 1, y] == '.' || s[x + 1, y] == '.' || s[x, y - 1] == '.' || (y < 31 && s[x, y + 1] == '.'))) f[x, y] = D;
			foreach (int x in new[] { 13, 17, 21, 25 }) { f[x, 22 + (x == 25 ? 2 : 0) - (x == 17 || x == 21 ? 1 : 0)] = O; f[x + 1, 22 + (x == 25 ? 2 : 0) - (x == 17 || x == 21 ? 1 : 0)] = R; }
			// head resting on the floor (art rows 0..9, cols 0..13), chin on row 31
			var a = Body(new Look { Eye = eye });
			for (int r = 0; r <= 9; r++) for (int c = 0; c <= 13; c++) if (a[r, c] != '.') f[c + 1, r + 22] = a[r, c];
			if (z >= 1) Zed(f, 20, 12, 4);
			if (z >= 2) Zed(f, 25, 5, 5);
			return f;
		}
		// knocked over: on the back, little legs in the air, tongue out
		Frame OnBack(int k)
		{
			var f = Lying(6, 0);
			var a = Body(new Look { Eye = 6, Jaw = 3 });
			for (int r = 0; r <= 11; r++) for (int c = 0; c <= 13; c++) if (a[r, c] != '.') f[c + 1, r + 22 - 2] = a[r, c];
			for (int y = 31; y > 20; y--) for (int x = 0; x < 16; x++) if (y > 29 && f[x, y] == '.' && x > 1) f[x, y] = '.';
			// legs up (two little legs with feet)
			foreach (int lx in new[] { 15, 21 })
			{
				int lift = k == 0 ? (lx == 15 ? 0 : 1) : (lx == 15 ? 1 : 0);
				f.Rect(lx, 16 + lift, 3, 5, M).Rect(lx, 16 + lift, 1, 5, D).Rect(lx + 2, 16 + lift, 1, 5, D).Rect(lx - 1, 15 + lift, 5, 2, D);
			}
			return f;
		}
		Frame Up(Frame f, int n) => f.Shift(0, -n);

		// ============================================================ props and effects (frame coords)
		void St(Frame f, int x, int y, params string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		void Behind(Frame f, int x, int y, params string[] rows)   // only on empty pixels (cape, backpack, tail-side props)
		{
			for (int j = 0; j < rows.Length; j++) for (int i = 0; i < rows[j].Length; i++)
				if (rows[j][i] != '.' && f[x + i, y + j] == '.') f[x + i, y + j] = rows[j][i];
		}
		void Zed(Frame f, int x, int y, int n)
		{
			f.Rect(x, y, n, 1, Zz).Rect(x, y + n - 1, n, 1, Zz);
			for (int i = 1; i < n - 1; i++) f.Dot(x + n - 1 - i, y + i, Zz);
		}
		void Note(Frame f, int x, int y) => St(f, x, y, ".DD", ".D.", ".D.", "DD.");
		void Star(Frame f, int x, int y, char c) { f.Dot(x, y, c).Dot(x - 1, y, c).Dot(x + 1, y, c).Dot(x, y - 1, c).Dot(x, y + 1, c); }
		void Heart(Frame f, int x, int y) => St(f, x, y, "HH.HH", "HhHHH", ".HHH.", "..H..");
		void Drop(Frame f, int x, int y) => f.Dot(x, y, Water).Dot(x, y + 1, Water);
		void Lines(Frame f, params (int x, int y, int len, bool vert)[] ls) { foreach (var (x, y, len, v) in ls) f.Rect(x, y, v ? 1 : len, v ? len : 1, D); }
		void Circle(Frame f, float cx, float cy, float r, char fill, char edge)
		{
			for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
				for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
				{
					float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = MathF.Sqrt(dx * dx + dy * dy);
					if (d <= r) f[x, y] = d > r - 1 ? edge : fill;
				}
		}
		void Ring(Frame f, float cx, float cy, float r, char c)
		{
			for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
				for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
				{
					float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = MathF.Sqrt(dx * dx + dy * dy);
					if (d <= r && d > r - 1) f[x, y] = c;
				}
		}
		// hats sit on the head (head top is row 6, head spans x 4..17)
		void Hat(Frame f, string kind, int dx = 0, int dy = 0)
		{
			switch (kind)
			{
				case "detective": St(f, 6 + dx, 2 + dy, "....bBBBb....", "..bBBBBBBBb..", ".bBBbBBbBBBb.", "bbbbbbbbbbbbb"); break;
				case "wizard": St(f, 7 + dx, -4 + dy, ".......PP..", "......PPP..", ".....PYPPP.", "....PPPPPp.", "...PPPYPPP.", "..PPYPPPPPP", "PPPPPPPPPPPPPP", ".pppppppppppp."); break;
				case "fedora": St(f, 6 + dx, 2 + dy, "...KKKKKKK...", "..KKKKKKKKK..", "..kkkkkkkkk..", "KKKKKKKKKKKKK"); break;
				case "police": St(f, 7 + dx, 2 + dy, "..NNNNNNNNN.", ".NNNNNYNNNNN", "NNNNNNNNNNNN", "...KKKKKKKKK"); break;
				case "explorer": St(f, 6 + dx, 2 + dy, "...AAAAAAA...", "..AAAAAyAAA..", ".AAAAAAAAAAA.", "AAAAAAAAAAAAAAA"); break;
				case "builder": St(f, 6 + dx, 2 + dy, "....YYYYY....", "..YYYYlYYYY..", ".YYYYYYYYYYY.", "yyyyyyyyyyyyy"); break;
				case "chef": St(f, 7 + dx, -3 + dy, "..lWWl.lWWl", ".WWWWWWWWWl", "lWWWWlWWWWl", ".WWWWWWWWW.", ".WWWWWWWWW.", ".lllllllll."); break;
				case "party": St(f, 11 + dx, -2 + dy, "...Y...", "..HPH..", "..PHP..", ".HPHPH.", ".PHPHP.", "HPHPHPH", "YYYYYYY"); break;
				case "beret": St(f, 7 + dx, 3 + dy, ".....K.....", "..HHHHHHH..", ".HHHHHHHHH.", "HHHHHHHHHHH"); break;
			}
		}
		void Sunglasses(Frame f) { f.Rect(8, 9, 8, 2, Blk).Rect(16, 9, 2, 1, Blk).Dot(10, 9, GrayD).Dot(14, 9, GrayD); }
		void Glasses(Frame f) { St(f, 9, 8, "KKKK.KKKK", "KccK.KccK", "KKKKKKKKK"); }
		void Goggles(Frame f) { St(f, 9, 8, "CCCC", "CccC", "CCCC"); f.Rect(13, 9, 5, 1, Teal); }
		void Headphones(Frame f, char cup)
		{
			foreach (var (x, y) in new[] { (9, 5), (10, 4), (11, 4), (12, 4), (13, 4), (14, 4), (15, 4), (16, 5), (17, 6), (17, 7) }) f.Dot(x, y, Blk);
			St(f, 15, 8, "KKK", "K" + cup + "K", "K" + cup + "K", "KKK");
		}
		void Cape(Frame f, int k)
		{
			Behind(f, 14, 15, k == 0
				? new[] { "HHHHH...........", "HHHHHHH.........", ".HHHHHHHH.......", ".HHHHHHHHHH.....", "..HHHHHHHHHHH...", "..HHHHHHhHHHHHH.", "...HHHHHHHHHHHh.", "...HHHHHHHHHH...", "....HHHHHHhH....", ".....HHHHHH.....", "......HHHH......" }
				: new[] { "HHHHH...........", "HHHHHHHH........", ".HHHHHHHHHH.....", ".HHHHHHHHHHHHH..", "..HHHHHHHHHHHHHH", "..HHHHHHhHHHHHh.", "...HHHHHHHHHH...", "....HHHHHHHh....", ".....HHHHHH.....", ".......HHH......", "................" });
			f.Rect(13, 16, 6, 1, Red).Rect(12, 17, 2, 1, Red).Dot(11, 17, RedL);
		}
		// held items: the near hand is at about (5..7, 19..21)
		void Hold(Frame f, string item, int k = 0)
		{
			switch (item)
			{
				case "meat": St(f, 0, 11, "..HHHH..", ".HHhhHH.", "HHhHHHHH", "HHHHHHHh", ".HHHHHH.", "..HHHH..", ".....WW.", "......WW", ".....W.W"); break;
				case "magnifier": St(f, 0, 14, ".gggg.", "gccccg", "gcWccg", "gccccg", "gccccg", ".gggg.", "....bb", ".....b"); break;
				case "book": St(f, 0, 16, "BBBBB.BBBBB", "BWWWWbWWWWB", "BWllWbWllWB", "BWWWWbWWWWB", "BWllWbWllWB", "BWWWWbWWWWB", "BBBBBBBBBBB"); break;
				case "glass": St(f, 0, 10, "...HH", "...H.", "c.H.c", "cwwwc", "cwwwc", "cwwwc", "cwwwc", "ccccc"); break;
				case "pizza": St(f, 0, 11, "bbbbbbbb", "bYYHYYYb", ".YHHYYHY", "..YYYHY.", "...YYY..", "....Y..."); break;
				case "balloon": Circle(f, 3f, 0f, 3.2f, Red, Red); f.Dot(2, -2, RedL).Dot(3, 3, Red); for (int y = 4; y <= 18; y++) f.Dot(3 + (y > 11 ? 1 : 0) + (y > 16 ? 1 : 0), y, GrayL); break;
				case "gloves": St(f, 0, 16, ".HHHH.", "HHhHHH", "HHHHHH", "HHHHHH", ".HHHH.", "..WW.."); St(f, 8, 19, ".HHHH", "HHhHH", "HHHHH", ".HHH."); break;
				case "popcorn": St(f, 0, 15, ".W.Y.W.", "YWYWYWY", "WYWYWYW", "HWHWHWH", "HWHWHWH", "HWHWHWH", ".HWHWH."); break;
				case "camera": St(f, 7, 8, "..KK...", "KKKKKKK", "KccKKKK", "KccKKgK", "KKKKKKK"); break;
				case "tube": St(f, 1, 15, "cc", "cc", "Fc", "FF", "FF", "cc"); break;
				case "wand": for (int i = 0; i < 6; i++) f.Dot(4 - i / 2, 19 - i, BrownD); Star(f, 1, 12, Yel); f.Dot(0, 9, PurpL).Dot(3, 10, Yel); break;
				case "watering": St(f, 0, 16, "C..CCCCC.", ".C.CCCCCC", "..CCCCCC.", "...CCCCC.", "...CCCCC."); if (k == 1) { f.Dot(0, 21, Water).Dot(1, 23, Water).Dot(0, 25, Water).Dot(1, 27, Water); } break;
				case "basketball": Circle(f, 3.5f, 20.5f, 3.6f, O, R); f.Rect(0, 20, 7, 1, Blk).Rect(3, 17, 1, 7, Blk); break;
				case "cupcake": St(f, 1, 14, "..Y..", "..H..", ".FFF.", "FFFFF", "BbBbB", ".BbB."); break;
				case "pan": St(f, 0, 21, "KKKKKKKgg", ".KkkkkK..", "..KKKK..."); St(f, 1, 12, "gggg", "gkgk", "gggg", ".bb.", ".b..", ".b..", ".b.."); break;
				case "walkie": St(f, 2, 15, ".K..", ".K..", "KKKK", "KgKK", "KKKK", "KYKK", "KKKK"); break;
				case "notes": St(f, 0, 16, "lllllll", "lWWWWWl", "lWKKKWl", "lWWWWWl", "lWKKWWl", "lWWWWWl", "lllllll"); for (int i = 0; i < 5; i++) f.Dot(7 + i / 2, 18 - i, Yel); f.Dot(7, 19, Blk); break;
				case "map": St(f, 0, 16, "lllllllll", "lwwWWuuul", "lwwwuuuwl", "luuwwHwwl", "luuuwwwwl", "lllllllll"); break;
				case "hammer": St(f, 0, 11, "kkkkk", "kgkkk", "kkkkk", "..b..", "..b..", "..b..", "..b..", "..b.."); break;
				case "phone": St(f, 2, 16, "KKK", "KcK", "KcK", "KKK"); break;
				case "mic": for (int y = 14; y <= 31; y++) f.Dot(2, y, GrayD); f.Rect(0, 31, 5, 1, GrayD); St(f, 1, 12, "KK", "KK"); f.Dot(3, 13, GrayD); break;
				case "brush": for (int i = 0; i < 5; i++) f.Dot(4 - i / 2, 19 - i, BrownD); f.Dot(2, 14, Red); St(f, 7, 21, ".BBB", "BHYB", "BUGB", ".BB."); break;
				case "drumsticks": f.Dot(4, 18, BrownD).Dot(3, 17, BrownD).Dot(2, 16, BrownD).Dot(9, 18, BrownD).Dot(8, 17, BrownD); break;
				case "fish": St(f, 0, 17, ".UUU.U", "UuUUUU", ".UUU.U"); break;
				case "orb": break;
			}
		}
		Frame Fx(Frame f, string kind, int k = 0)
		{
			switch (kind)
			{
				case "stars": foreach (var (x, y, c) in new[] { (12, 6, Yel), (20, 4, O), (16, 2, Mint) }) Star(f, x + (k == 0 ? 0 : 1), y - k, c); break;
				case "shock": Lines(f, (3, 3, 2, false), (2, 7, 2, false), (20, 4, 2, false), (19, 1, 2, false), (12, 1, 1, true)); break;
				case "tears": Drop(f, 11, 11 + k); Drop(f, 12, 14 - k); Drop(f, 13, 17 + k); break;
				case "sweat": Drop(f, 15, 7 + k); break;
				case "sun": Circle(f, 3f, -2f, 2.6f, Yel, YelD); foreach (var (x, y) in new[] { (3, -6), (3, 2), (-1, -2), (7, -2), (0, -5), (6, -5), (0, 1), (6, 1) }) if (k == 1 || (x + y) % 2 == 0) f.Dot(x, y, Yel); break;
				case "snow": foreach (var (x, y) in new[] { (2, 2), (8, 0), (20, 2), (24, 6), (5, 9), (18, -2) }) f.Dot(x, (y + k * 2 + 20) % 14 - 3, Glass); break;
				case "confetti":
					char[] cs = { Red, Yel, Blue, Purp, Mint };
					var pts = new[] { (2, -4), (7, -2), (13, -5), (19, -3), (25, -1), (4, 2), (22, 3), (10, -6), (28, 6), (0, 6) };
					for (int i = 0; i < pts.Length; i++) f.Dot(pts[i].Item1, pts[i].Item2 + (k + i) % 3, cs[(i + k) % 5]);
					break;
				case "notes": Note(f, 1 + k * 2, 2 - k); Note(f, 22, -2 + k); break;
				case "zz": Zed(f, 20, 0, 4); if (k == 1) Zed(f, 25, -6, 5); break;
				case "shiver": Lines(f, (0, 12, 2, true), (24, 14, 2, true), (2, 20, 2, true), (26, 20, 2, true)); break;
				case "teeth": f.Rect(4, 14, 5, 1, Wt).Dot(5, 14, GrayL).Dot(7, 14, GrayL); break;
				case "smoke": Circle(f, 21f, 0f, 2.6f, Gray, Gray); Circle(f, 24f, -1f, 2f, GrayL, Gray); break;
			}
			return f;
		}
		void Costume(Frame f, string kind)
		{
			char Recolor(char c, char light, char dark) => c == G ? light : (c == M || c == D) ? dark : c;
			int y0 = kind == "trench" ? 16 : 17, y1 = kind == "trench" ? 30 : 26;
			for (int y = y0; y <= y1; y++) for (int x = 8; x <= 22; x++)
			{
				char c = f[x, y]; if (c == '.' || c == O || c == R) continue;
				switch (kind)
				{
					case "labcoat": f[x, y] = Recolor(c, Wt, GrayL); break;
					case "trench": f[x, y] = Recolor(c, GrayD, Blk); break;
					case "astronaut": f[x, y] = Recolor(c, GrayL, Gray); break;
				}
			}
			if (kind == "labcoat") f.Rect(14, 18, 1, 8, GrayL);
			if (kind == "trench") f.Rect(14, 17, 1, 13, Blk).Dot(13, 20, Gray).Dot(13, 24, Gray);
			if (kind == "astronaut") { f.Dot(12, 21, Red).Dot(13, 21, Blue).Rect(20, 17, 4, 7, Gray); }
		}

		// ============================================================ frames
		int F(Frame f) => p.Add(f);
		int fStand = F(Pose());
		int fBlink = F(Pose(eye: 1));
		int fHappy = F(Pose(eye: 2, blush: true));
		int fLookUp = F(Pose(eye: 5));
		int fWide = F(Pose(eye: 3, jaw: 4));
		int fTailUp = F(Pose(eye: 2, tail: 1));
		int fTailDown = F(Pose(eye: 2, tail: -1));
		// walk: alternate feet with a 1 px bob
		int fWalk1 = F(Pose(feet: 1, tail: 1, dy: -1));
		int fWalk2 = F(Pose());
		int fWalk3 = F(Pose(feet: 2, tail: -1, dy: -1));
		// run: leaning forward, arms tucked
		int fRun1 = F(Lean(Pose(arm: 3, feet: 1, tail: 1, jaw: 1), -2, 26));
		int fRun2 = F(Lean(Pose(arm: 3, feet: 2, tail: -1, jaw: 1), -2, 26));
		int fCrouch = F(Squash(Pose(eye: 6), 2));
		int fJump = F(Up(Pose(eye: 2, arm: 1, feet: 3, tail: 1), 3));
		int fFall = F(Pose(eye: 3, jaw: 4, arm: 1, farUp: true, feet: 3, tail: 1));
		int fLand = F(Squash(Pose(eye: 6, jaw: 1), 3));
		int fSit = F(Squash(Pose(tail: -1), 3));
		int fSitBlink = F(Squash(Pose(eye: 1, tail: -1), 3));
		int fSleep1 = F(Lying(1, 0)), fSleep2 = F(Lying(1, 1)), fSleep3 = F(Lying(1, 2));
		var dz1 = OnBack(0); Star(dz1, 3, 16, Yel); Star(dz1, 9, 13, O); Star(dz1, 14, 17, Mint); int fDizzy1 = F(dz1);
		var dz2 = OnBack(1); Star(dz2, 5, 14, Mint); Star(dz2, 11, 12, Yel); Star(dz2, 2, 18, O); int fDizzy2 = F(dz2);
		int fWave1 = F(Pose(eye: 2, arm: 1, blush: true));
		int fWave2 = F(Pose(eye: 2, arm: 1, tail: 1, blush: true, dy: -1));
		int fCheer = F(Up(Pose(eye: 2, jaw: 1, arm: 1, farUp: true, tail: 1), 2));
		int fDrag1 = F(Pose(eye: 3, jaw: 4, arm: 1, farUp: true, feet: 3, tail: 1));
		int fDrag2 = F(Lean(Pose(eye: 3, jaw: 4, arm: 1, farUp: true, feet: 3, tail: -1), 1, 30));
		int fClimb1 = F(Pose(eye: 5, arm: 1, feet: 1));
		int fClimb2 = F(Pose(eye: 5, arm: 2, farUp: true, feet: 2));
		// roar
		int fRoarPrep = F(Squash(Pose(eye: 6, jaw: 1, tail: -1), 1));
		int fRoar1 = F(Lean(Pose(eye: 4, jaw: 2, arm: 1, tail: 1), -1, 26));
		int fRoar2 = F(Lean(Pose(eye: 4, jaw: 2, arm: 2, farUp: true, tail: 1), -2, 26));

		// --- reference sheet 4: feelings
		var eat1 = Pose(eye: 2, jaw: 2); Hold(eat1, "meat"); int fEat1 = F(eat1);
		var eat2 = Pose(eye: 1, jaw: 0, blush: true); Hold(eat2, "meat"); eat2.Rect(0, 14, 3, 3, '.'); int fEat2 = F(eat2);
		int fShock1 = F(Fx(Pose(eye: 3, jaw: 2, arm: 1, farUp: true), "shock"));
		int fShock2 = F(Fx(Up(Pose(eye: 3, jaw: 2, arm: 1, farUp: true), 1), "shock"));
		int fCry1 = F(Fx(Squash(Pose(eye: 1, jaw: 1, tail: -1), 3), "tears", 0));
		int fCry2 = F(Fx(Squash(Pose(eye: 1, jaw: 4, tail: -1), 3), "tears", 1));
		int fThumb = F(Pose(eye: 2, arm: 1, blush: true));
		int fAngry1 = F(Pose(eye: 4, jaw: 0, arm: 2, farUp: false, tail: -1));
		int fAngry2 = F(Squash(Pose(eye: 4, jaw: 1, arm: 2, tail: -1), 1));
		var cool1 = Pose(arm: 2); Sunglasses(cool1); int fCool1 = F(cool1);
		var cool2 = Pose(arm: 1, tail: 1, eye: 0); Sunglasses(cool2); cool2.Dot(20, 6, Yel).Dot(21, 5, Yel); int fCool2 = F(cool2);
		int fAh = F(Up(Pose(eye: 6, jaw: 1, tail: 1), 1));
		var choo = Lean(Pose(eye: 6, jaw: 2, arm: 2, tail: 1), -2, 26); Lines(choo, (0, 11, 2, false), (0, 15, 2, false), (1, 13, 2, false)); int fChoo = F(choo);
		int fDance1 = F(Fx(Lean(Pose(eye: 2, jaw: 1, arm: 1, feet: 1, tail: 1, blush: true), -1, 26), "notes", 0));
		int fDance2 = F(Fx(Lean(Pose(eye: 2, jaw: 0, arm: 2, farUp: true, feet: 2, tail: -1, blush: true), 1, 26), "notes", 1));
		var sniff1 = Lean(Pose(eye: 0, arm: 2, tail: 1), -2, 28); Hold(sniff1, "magnifier"); int fSniff1 = F(sniff1);
		var sniff2 = Lean(Pose(eye: 6, jaw: 4, arm: 2, tail: 1), -3, 28); Hold(sniff2, "magnifier"); int fSniff2 = F(sniff2);
		int fParty1 = F(Fx(Pose(eye: 2, jaw: 1, arm: 1, farUp: true, blush: true), "confetti", 0));
		int fParty2 = F(Fx(Up(Pose(eye: 2, jaw: 1, arm: 1, farUp: true, feet: 3, tail: 1, blush: true), 3), "confetti", 1));
		int fLove = F(Pose(eye: 2, blush: true, tail: 1));
		int fTired1 = F(Fx(Squash(Lean(Pose(eye: 7, jaw: 3, tail: -1), -1, 26), 1), "sweat", 0));
		int fTired2 = F(Fx(Squash(Lean(Pose(eye: 7, jaw: 3, tail: -1), -1, 26), 2), "sweat", 1));

		// --- reference sheet 5: everyday things
		var rd1 = Squash(Pose(eye: 1, arm: 2, tail: -1), 2); Hold(rd1, "book"); int fRead1 = F(rd1);
		var rd2 = Squash(Pose(eye: 0, arm: 2, tail: -1), 2); Hold(rd2, "book"); rd2.Rect(5, 16, 3, 1, Wt); int fRead2 = F(rd2);
		var dr1 = Pose(eye: 0, arm: 2); Hold(dr1, "glass"); int fDrink1 = F(dr1);
		var dr2 = Pose(eye: 1, arm: 2, blush: true); Hold(dr2, "glass"); dr2.Rect(2, 16, 2, 2, Glass); int fDrink2 = F(dr2);
		var pz1 = Pose(eye: 2, jaw: 2, arm: 2); Hold(pz1, "pizza"); int fPizza1 = F(pz1);
		var pz2 = Pose(eye: 1, jaw: 0, arm: 2, blush: true); Hold(pz2, "pizza"); pz2.Rect(0, 13, 3, 3, '.'); int fPizza2 = F(pz2);
		Frame Cold(int k) { var f = Pose(eye: 6, jaw: 1, arm: 2, farUp: false); Fx(f, "teeth"); Fx(f, "snow", k); Fx(f, "shiver"); f.Shift(k == 0 ? -1 : 1, 0); return f; }
		int fCold1 = F(Cold(0)), fCold2 = F(Cold(1));
		int fHot1 = F(Fx(Fx(Pose(eye: 7, jaw: 3, arm: 1), "sun", 0), "sweat", 0));
		int fHot2 = F(Fx(Fx(Pose(eye: 7, jaw: 3, arm: 2), "sun", 1), "sweat", 1));
		Frame Computer(int k)
		{
			var f = Squash(Pose(eye: k == 2 ? 2 : 0, arm: 2, tail: -1), 1);
			St(f, 0, 15, "kkkkkkkkk", "kCCCCCCCk", "kCcCCCCCk", "kCCCCcCCk", "kCCCCCCCk", "kkkkkkkkk", "....k....", "..kkkkk..");
			f.Dot(2 + k * 2, 17 + k, Wt);
			return f;
		}
		int fPc1 = F(Computer(0)), fPc2 = F(Computer(1)), fPc3 = F(Computer(2));
		var bx1 = Pose(eye: 4, arm: 2); Hold(bx1, "gloves"); int fBox1 = F(bx1);
		var bx2 = Lean(Pose(eye: 4, jaw: 1, arm: 2, feet: 1), -2, 28); Hold(bx2, "gloves"); bx2.Shift(-2, 0); Lines(bx2, (0, 14, 2, true)); int fBox2 = F(bx2);
		var bl1 = Pose(eye: 5, arm: 2, tail: 1); Hold(bl1, "balloon"); int fBalloon1 = F(bl1);
		var bl2 = Pose(eye: 2, arm: 2, tail: -1, blush: true); Hold(bl2, "balloon"); bl2.Shift(0, -1); int fBalloon2 = F(bl2);
		var dt1 = Pose(eye: 0, arm: 2); Hat(dt1, "detective"); Hold(dt1, "magnifier"); int fDetective1 = F(dt1);
		var dt2 = Lean(Pose(eye: 6, arm: 2, tail: 1), -2, 28); Hat(dt2, "detective", -1); Hold(dt2, "magnifier"); int fDetective2 = F(dt2);
		var pc1 = Squash(Pose(eye: 0, jaw: 1, arm: 2, tail: -1), 2); Hold(pc1, "popcorn"); int fPop1 = F(pc1);
		var pc2 = Squash(Pose(eye: 3, jaw: 2, arm: 2, tail: -1), 2); Hold(pc2, "popcorn"); pc2.Dot(4, 12, Yel).Dot(6, 10, Wt); int fPop2 = F(pc2);
		var hd1 = Pose(eye: 1, jaw: 0, arm: 1, blush: true); Headphones(hd1, Purp); Fx(hd1, "notes", 0); int fMusic1 = F(hd1);
		var hd2 = Lean(Pose(eye: 2, jaw: 1, arm: 2, feet: 1, tail: 1, blush: true), 1, 26); Headphones(hd2, Blue); Fx(hd2, "notes", 1); int fMusic2 = F(hd2);

		// --- reference sheet 6: hobbies and costumes
		Frame Fishing(int k)
		{
			var f = Squash(Pose(eye: k == 2 ? 3 : 0, arm: 2, tail: -1), 3);
			St(f, 18, 27, "bBBBBb", ".b..b.", ".b..b.", ".b..b.");   // stool behind
			for (int i = 0; i < 10; i++) f.Dot(5 - i / 2, 21 - i, BrownD);
			for (int y = 12; y <= 30 - k * 4; y++) f.Dot(0, y, GrayL);
			if (k == 0) f.Rect(0, 31, 3, 1, Water);
			if (k == 2) St(f, 0, 24, "UUU", "uUU", "UUU");
			return f;
		}
		int fFish1 = F(Fishing(0)), fFish2 = F(Fishing(1)), fFish3 = F(Fishing(2));
		var cm1 = Pose(eye: 0, arm: 2); Hold(cm1, "camera"); int fCamera1 = F(cm1);
		var cm2 = Pose(eye: 0, arm: 2); Hold(cm2, "camera"); Star(cm2, 6, 7, Yel); cm2.Dot(3, 6, Yel).Dot(9, 5, Yel); int fCamera2 = F(cm2);
		Frame Paint(int k)
		{
			var f = Pose(eye: k == 1 ? 2 : 0, arm: 2, tail: k == 1 ? 1 : 0);
			f.Shift(4, 0);
			St(f, 0, 12, "WWWWWW", "WwwwwW", "WwYwwW", "WGGwGW", "WGGGGW", "WWWWWW");
			for (int y = 18; y <= 31; y++) { f.Dot(1, y, BrownD); f.Dot(4, y, BrownD); }
			f.Dot(2, 18, BrownD).Dot(3, 18, BrownD);
			for (int i = 0; i < 4; i++) f.Dot(9 - i, 19 - i, BrownD);
			f.Dot(6, 15, k == 0 ? Red : Blue);
			St(f, 11, 21, ".BBB", "BHYB", "BUGB", ".BB.");
			return f;
		}
		int fPaint1 = F(Paint(0)), fPaint2 = F(Paint(1));
		var sc1 = Pose(eye: 0, arm: 2); Costume(sc1, "labcoat"); Hold(sc1, "tube"); int fScience1 = F(sc1);
		var sc2 = Pose(eye: 3, jaw: 4, arm: 2); Costume(sc2, "labcoat"); Hold(sc2, "tube"); sc2.Dot(1, 13, Mint).Dot(2, 11, Mint).Dot(0, 10, Pink); int fScience2 = F(sc2);
		var wz1 = Pose(eye: 0, arm: 2); Hat(wz1, "wizard"); Hold(wz1, "wand"); int fWizard1 = F(wz1);
		var wz2 = Pose(eye: 2, jaw: 1, arm: 1, tail: 1); Hat(wz2, "wizard"); for (int i = 0; i < 5; i++) wz2.Dot(3 - i / 2, 13 - i, BrownD); Star(wz2, 1, 6, Yel); Star(wz2, 6, 3, PurpL); wz2.Dot(9, 6, Yel); int fWizard2 = F(wz2);
		var spy1 = Pose(eye: 4, arm: 3); Costume(spy1, "trench"); Hat(spy1, "fedora"); int fSpy1 = F(spy1);
		var spy2 = Pose(eye: 6, arm: 3, feet: 1); Costume(spy2, "trench"); Hat(spy2, "fedora"); spy2.Shift(-1, 0); int fSpy2 = F(spy2);
		Frame Guitar(int k)
		{
			var f = Pose(eye: k == 1 ? 1 : 2, jaw: k, arm: 2, tail: k == 0 ? 1 : -1, blush: true);
			Circle(f, 13f, 24f, 3.6f, Brown, BrownD); f.Dot(12, 24, Blk).Dot(13, 24, Blk).Dot(13, 23, Blk);
			for (int i = 0; i < 10; i++) f.Dot(10 - i, 22 - i / 2, BrownD);
			f.Rect(0, 16, 2, 2, BrownD);
			f.Dot(14 + k, 21, D);
			Note(f, 22 + k, 4 - k * 3);
			return f;
		}
		int fGuitar1 = F(Guitar(0)), fGuitar2 = F(Guitar(1));
		var wt1 = Pose(eye: 0, arm: 2); Hold(wt1, "watering", 0); int fWater1 = F(wt1);
		var wt2 = Lean(Pose(eye: 2, arm: 2, blush: true), -1, 28); Hold(wt2, "watering", 1); int fWater2 = F(wt2);
		Frame Swim(int k)
		{
			var f = Pose(eye: 0, jaw: 1, arm: k == 0 ? 1 : 2, farUp: k == 1, feet: k == 0 ? 1 : 2, tail: k == 0 ? 1 : -1);
			Goggles(f);
			for (int x = 8; x <= 22; x++) { f[x, 23] = O; f[x, 24] = R; }
			f.Rect(10, 23, 2, 2, Wt).Rect(17, 23, 2, 2, Wt);
			f.Dot(2 + k, 7 - k, Glass).Dot(4, 4 + k, Glass);
			return f;
		}
		int fSwim1 = F(Swim(0)), fSwim2 = F(Swim(1));
		var bb1 = Pose(eye: 0, arm: 2); Hold(bb1, "basketball"); int fBall1 = F(bb1);
		var bb2 = Up(Pose(eye: 5, arm: 1, farUp: true, feet: 3, tail: 1), 3); Circle(bb2, 5f, 2f, 3f, O, R); bb2.Rect(3, 2, 5, 1, Blk); int fBall2 = F(bb2);
		var cc1 = Pose(eye: 2, jaw: 0, arm: 2, blush: true); Hat(cc1, "party"); Hold(cc1, "cupcake"); int fCake1 = F(cc1);
		var cc2 = Pose(eye: 2, jaw: 4, arm: 2, blush: true); Hat(cc2, "party"); Hold(cc2, "cupcake"); cc2.Dot(3, 13, O); int fCake2 = F(cc2);

		// --- reference sheet 7: jobs
		Frame Astronaut(int k)
		{
			var f = Up(Pose(eye: 2, jaw: 1, arm: 2, feet: 3, tail: k == 0 ? 1 : -1), 2 + k);
			Costume(f, "astronaut");
			Ring(f, 11f, 9.5f - (2 + k), 9f, Glass);
			f.Dot(5, 3 - k, Wt).Dot(6, 2 - k, Wt);
			return f;
		}
		int fAstro1 = F(Astronaut(0)), fAstro2 = F(Astronaut(1));
		var ch1 = Pose(eye: 0, arm: 2); Hat(ch1, "chef"); Hold(ch1, "pan"); int fChef1 = F(ch1);
		var ch2 = Pose(eye: 2, arm: 2, tail: 1); Hat(ch2, "chef"); Hold(ch2, "pan"); ch2.Shift(0, -1); St(ch2, 0, 15, ".Y.", "YWY"); int fChef2 = F(ch2);
		var ft1 = Pose(eye: 0, feet: 0); Circle(ft1, 2.5f, 28.5f, 2.6f, Wt, Blk); ft1.Dot(2, 28, Blk).Dot(1, 29, Blk); int fSoccer1 = F(ft1);
		var ft2 = Lean(Pose(eye: 6, jaw: 1, feet: 1, arm: 1), 1, 30); int fSoccer2 = F(ft2);
		var pl1 = Pose(eye: 0, arm: 2); Hat(pl1, "police"); Hold(pl1, "walkie"); pl1.Dot(12, 19, Yel); int fPolice1 = F(pl1);
		var pl2 = Pose(eye: 0, jaw: 1, arm: 2); Hat(pl2, "police"); Hold(pl2, "walkie"); pl2.Dot(12, 19, Yel); Lines(pl2, (0, 12, 1, true), (1, 10, 1, true)); int fPolice2 = F(pl2);
		var wr1 = Squash(Pose(eye: 1, arm: 2, tail: -1), 2); Hold(wr1, "notes"); int fWrite1 = F(wr1);
		var wr2 = Squash(Pose(eye: 0, arm: 2, tail: -1), 2); Hold(wr2, "notes"); wr2.Dot(3, 19, Blk).Dot(2, 21, Blk); int fWrite2 = F(wr2);
		Frame Drums(int k)
		{
			var f = Pose(eye: k == 1 ? 6 : 2, jaw: k, arm: 2, farUp: k == 1, tail: k == 0 ? 1 : -1);
			St(f, 0, 23, ".HHHHHHH.", "HWWWWWWWH", "HWWWWWWWH", "HHHHHHHHH", "HlHlHlHlH", "HHHHHHHHH", ".g.....g.", "g.......g", ".........");
			St(f, 0, 18, "YYYY");
			f.Dot(1, 19, GrayD);
			Hold(f, "drumsticks");
			if (k == 1) { f.Dot(5, 21, Yel).Dot(1, 16, Yel); }
			return f;
		}
		int fDrums1 = F(Drums(0)), fDrums2 = F(Drums(1));
		var ex1 = Pose(eye: 0, arm: 2); Hat(ex1, "explorer"); Behind(ex1, 19, 15, ".bbbb", "bBBBBb", "bBUBBb", "bBBBBb", "bBBBBb", ".bbbb."); Hold(ex1, "map"); int fExplore1 = F(ex1);
		var ex2 = Pose(eye: 5, arm: 2, feet: 1); Hat(ex2, "explorer"); Behind(ex2, 19, 15, ".bbbb", "bBBBBb", "bBUBBb", "bBBBBb", "bBBBBb", ".bbbb."); Hold(ex2, "map"); int fExplore2 = F(ex2);
		Frame Hero(int k, bool flying)
		{
			var f = flying ? Lean(Up(Pose(eye: 0, jaw: 0, arm: 2, feet: 3, tail: 1), 2), -4, 30) : Pose(eye: 2, arm: 1, tail: 1);
			Cape(f, k);
			if (flying) { f.Dot(28, 14, D).Dot(29, 18, D).Dot(28, 22, D); }
			return f;
		}
		int fHeroStand = F(Hero(0, false)), fHeroFly1 = F(Hero(0, true)), fHeroFly2 = F(Hero(1, true));
		var bd1 = Pose(eye: 0, arm: 1); Hat(bd1, "builder"); Hold(bd1, "hammer"); bd1.Shift(0, 0); int fBuild1 = F(bd1);
		var bd2 = Pose(eye: 6, jaw: 1, arm: 2); Hat(bd2, "builder"); St(bd2, 0, 19, "kkk", "kkk", ".bbbb"); bd2.Dot(1, 23, Yel).Dot(3, 24, Yel); int fBuild2 = F(bd2);
		// Juggling: three balls travel around a loop in front of his face (thrown up from one hand, over the top,
		// caught by the other), a third of the loop apart, so every ball really moves from frame to frame.
		char[] orbColors = { Blue, Red, Mint };
		// 24 frames: each ball moves about 2 px per frame, so the loop looks smooth (the body keeps an 8-step rhythm)
		const int JuggleFrames = 24;
		Frame Juggle(int k)
		{
			// the hand that is about to throw goes up, the other one waits low
			int beat = k * 8 / JuggleFrames;
			bool throwNear = beat % 4 < 2;
			var f = Pose(eye: 5, jaw: beat % 4 == 1 ? 4 : 0, arm: throwNear ? 1 : 2, farUp: !throwNear, tail: beat % 2 == 0 ? 1 : -1);
			for (int i = 0; i < 3; i++)
			{
				// position on the loop (0..1): up on the outside, down on the inside, faster at the bottom
				double t = (k / (double)JuggleFrames + i / 3.0) % 1.0;
				double a = Math.PI * 2 * t;
				int x = (int)Math.Round(9 + Math.Cos(a) * 7);   // 2..16: the whole ball stays inside the frame
				int y = (int)Math.Round(3 - Math.Sin(a) * 7 * (Math.Sin(a) > 0 ? 1.0 : 0.45));
				Circle(f, x, y, 1.6f, orbColors[i], orbColors[i]);
				f.Dot(x - 1, y - 1, Wt);   // a little shine
			}
			return f;
		}
		int[] fJuggle = Enumerable.Range(0, JuggleFrames).Select(k => F(Juggle(k))).ToArray();
		Frame Sing(int k)
		{
			var f = Pose(eye: k == 0 ? 2 : 1, jaw: k == 0 ? 1 : 2, arm: 2, tail: k == 0 ? 1 : -1, blush: true);
			Hold(f, "mic");
			Note(f, 1 + k * 3, 1 - k * 2); Note(f, 24, -3 + k * 2);
			return f;
		}
		int fSing1 = F(Sing(0)), fSing2 = F(Sing(1));
		int fYawn = F(Up(Pose(eye: 1, jaw: 2, arm: 1, farUp: true, tail: 1), 1));
		int fStomp1 = F(Squash(Pose(eye: 4, jaw: 1, feet: 1, tail: 1), 1));
		var stomp2 = Pose(eye: 4, jaw: 2, feet: 0, tail: -1); Lines(stomp2, (0, 31, 3, false), (6, 30, 1, true), (26, 31, 3, false)); int fStomp2 = F(stomp2);
		int fPhone1 = F(Pose(eye: 0, jaw: 0, arm: 2)); Hold(p.Frames[fPhone1], "phone");
		var ph2 = Pose(eye: 2, jaw: 1, arm: 2, blush: true); Hold(ph2, "phone"); ph2.Dot(4, 13, Yel); int fPhone2 = F(ph2);

		// ============================================================ child objects (separate windows)
		// roar sound waves
		Frame Waves(int k)
		{
			var f = new Frame();
			for (int i = 0; i < 3; i++) for (int y = -2 - i; y <= 2 + i; y++) f.Dot(3 - i * 2 - Math.Abs(y) / 2 + k, 12 + y, i % 2 == 0 ? M : G);
			return f;
		}
		int fWaves1 = F(Waves(0)), fWaves2 = F(Waves(1));
		// soccer ball on the floor, rolling
		Frame Ball(int k)
		{
			var f = new Frame(); Circle(f, 2.5f, 28.5f, 2.6f, Wt, Blk);
			var spots = new[] { (2, 28), (1, 29), (3, 27), (2, 29) }; var (sx, sy) = spots[k % 4]; f.Dot(sx, sy, Blk).Dot(sx + 1, sy, Blk);
			return f;
		}
		var rollBall = new int[4]; for (int i = 0; i < 4; i++) rollBall[i] = F(Ball(i));
		// balloon on its own
		Frame BalloonAlone(int k) { var f = new Frame(); Hold(f, "balloon"); if (k == 1) f.Shift(1, 0); return f; }
		int fBalAway1 = F(BalloonAlone(0)), fBalAway2 = F(BalloonAlone(1));
		// basketball in the air
		Frame Hoop(int k) { var f = new Frame(); Circle(f, 5f, 2f, 3f, O, R); if (k == 0) f.Rect(3, 2, 5, 1, Blk); else f.Rect(5, 0, 1, 5, Blk); return f; }
		int fHoop1 = F(Hoop(0)), fHoop2 = F(Hoop(1));
		// an ant runs away from the magnifier
		Frame Ant(int k) { var f = new Frame(); St(f, 0, 29, k == 0 ? "K.K.K." : ".K.K.K", "KKKKKK", k == 0 ? ".K.K.K" : "K.K.K."); f.Dot(0, 28, Blk); return f; }
		int fAnt1 = F(Ant(0)), fAnt2 = F(Ant(1));
		// angry smoke puff
		Frame Smoke(int k) { var f = new Frame(); Circle(f, 21f, 0f, 2.6f + k * 0.4f, Gray, Gray); Circle(f, 24f, -1f, 2f + k * 0.3f, GrayL, Gray); return f; }
		int fSmoke1 = F(Smoke(0)), fSmoke2 = F(Smoke(1));
		// love heart in a speech bubble
		Frame Bubble() { var f = new Frame(); St(f, 15, -4, ".DDDDDDD.", "D.......D", "D..H.H..D", "D..HHH..D", "D...H...D", ".DDDDDDD.", "...DD....", "...D....."); return f; }
		int fLoveBubble = F(Bubble());
		// a music note floating up
		var nt = new Frame(); Note(nt, 22, 0); int fNoteUp = F(nt);
		// sneeze puff
		var pf = new Frame(); Circle(pf, 2f, 12f, 2.2f, GrayL, GrayL); pf.Dot(4, 10, GrayL).Dot(0, 14, GrayL); int fPuff = F(pf);
		// paper airplane
		Frame Plane(int k) { var f = new Frame(); St(f, 0, 15, k == 0 ? new[] { "....lll", "..llWWl", "llWWWgl", "..llggl" } : new[] { "..lllll", "llWWWWl", "..lWggl", "...ll.." }); return f; }
		int fPlane1 = F(Plane(0)), fPlane2 = F(Plane(1));
		var plh = Pose(eye: 0, arm: 1); St(plh, 1, 10, "....lll", "..llWWl", "llWWWgl"); int fPlaneHold = F(plh);
		int fPlaneThrow = F(Lean(Pose(eye: 2, arm: 2, feet: 1), -1, 28));
		// a flower that grows where Rex watered, then wilts
		Frame Flower(int g)
		{
			var f = new Frame();
			f.Rect(0, 31, 6, 1, BrownD);
			if (g >= 1) f.Rect(2, 30 - g, 1, g, M).Dot(1, 30 - g / 2, M);
			if (g >= 3) { St(f, 0, 30 - g - 3, ".H.", "HYH", ".H."); }
			if (g == 5) { f.Rect(0, 26, 3, 3, '.'); St(f, 2, 26, "H.", ".H", "HY"); }
			return f;
		}
		int fFlower1 = F(Flower(1)), fFlower2 = F(Flower(2)), fFlower3 = F(Flower(3)), fFlower4 = F(Flower(4)), fWilt = F(Flower(5));
		// bubbles while swimming
		Frame Bubbles(int k) { var f = new Frame(); Ring(f, 3f, 5f, 1.6f, Glass); Ring(f, 6f, 1f - k, 1.1f, Glass); return f; }
		int fBub1 = F(Bubbles(0)), fBub2 = F(Bubbles(1));

		// ============================================================ 40 more scenes: frames
		int Hash(int x, int y, int k) { unchecked { int h = x * 374761393 + y * 668265263 + k * 1274126177; h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) & 0x7fffffff; } }
		void Line(Frame f, int x0, int y0, int x1, int y1, char c)
		{
			int n = Math.Max(1, Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)));
			for (int i = 0; i <= n; i++) f[x0 + (int)MathF.Round((x1 - x0) * i / (float)n), y0 + (int)MathF.Round((y1 - y0) * i / (float)n)] = c;
		}
		// outline of an ellipse; part chooses by the sine of the angle (< 0 = upper half), behind = only on empty pixels
		void Oval(Frame f, float cx, float cy, float rx, float ry, char c, Func<float, bool> part = null, bool behind = false)
		{
			for (int i = 0; i < 120; i++)
			{
				float an = MathF.PI * 2 * i / 120, s = MathF.Sin(an);
				if (part != null && !part(s)) continue;
				int x = (int)MathF.Round(cx + MathF.Cos(an) * rx), y = (int)MathF.Round(cy + s * ry);
				if (!behind || f[x, y] == '.') f[x, y] = c;
			}
		}
		Frame Copy(int frame) => p.Frames[frame].Clone();
		int fBlank = F(new Frame());

		// --- the meteor: a child window crosses the screen from behind Rex, leaves embers (children of the child)
		// and crashes on the taskbar or on a window: flash, fireball, mushroom cloud, flying rocks, a smoking crater.
		// The rock sits at the bottom left of the frame, so it touches the ground when the window lands.
		const float MtX = 8.5f, MtY = 25.5f, MtR = 5.3f, TailX = 0.753f, TailY = -0.659f;   // the tail points up-right: it flies down-left
		Frame Meteor(int k)
		{
			var f = new Frame();
			const float len = 31f;
			// the fiery tail: white-hot at the rock, then yellow, orange, red, dark embers and ash; flickering ragged edges
			for (int y = f.MinY; y < W; y++) for (int x = 0; x < W; x++)
			{
				float px = x + 0.5f - MtX, py = y + 0.5f - MtY;
				float along = px * TailX + py * TailY, side = -px * TailY + py * TailX;
				if (along > len) continue;
				if (along < 0 && px * px + py * py > (MtR + 1.3f) * (MtR + 1.3f)) continue;
				float t = Math.Max(0, along) / len;
				int n = Hash(x, y, k) % 100;
				float half = MtR * MathF.Pow(1 - t, 0.8f) + 0.6f
					+ MathF.Sin(along * 0.85f - k * 1.7f) * 1.1f * t + MathF.Sin(along * 0.37f + k * 2.3f) * 0.6f * t;
				float q = MathF.Abs(side) / Math.Max(half, 0.3f);
				if (q > 1)
				{
					if (along > 3 && q < 2f && n < 3 + (int)(t * 7)) f[x, y] = n % 2 == 0 ? Yel : Flame;   // sparks thrown off the tail
					continue;
				}
				float heat = (1 - t) * (1.15f - q * 0.75f) + (n % 9) * 0.015f;
				char c = heat > 0.82f ? Hot : heat > 0.64f ? Yel : heat > 0.47f ? O : heat > 0.32f ? Flame : heat > 0.2f ? FlameR : heat > 0.11f ? Ember : n < 40 ? Ash : '.';
				if (c != '.') f[x, y] = c;
			}
			// the rock: lit by its own fire at the back, dark just behind the front, the front edge burning white-hot
			for (int y = 18; y < W; y++) for (int x = 0; x < 18; x++)
			{
				float px = x + 0.5f - MtX, py = y + 0.5f - MtY, d = MathF.Sqrt(px * px + py * py);
				if (d > MtR) continue;
				float lead = -(px * TailX + py * TailY) / MtR;   // 1 = the front
				char c = lead < -0.45f ? RockL : lead > 0.1f && lead < 0.5f ? RockD : RockM;
				if (d > MtR - 1f) c = lead > 0.3f ? (lead > 0.75f ? Hot : Yel) : lead < -0.3f ? Flame : RockD;
				else if (d > MtR - 2f && lead > 0.55f) c = O;
				f[x, y] = c;
			}
			// craters that turn with the rock
			for (int i = 0; i < 3; i++)
			{
				float an = i * 2.1f + k * 0.5f, rr = i == 2 ? 1.2f : 2.5f;
				int x = (int)MathF.Floor(MtX + MathF.Cos(an) * rr), y = (int)MathF.Floor(MtY + MathF.Sin(an) * rr);
				f.Dot(x, y, RockD).Dot(x + 1, y, RockD).Dot(x + 1, y + 1, RockL);
			}
			return f;
		}
		int[] fMeteor = Enumerable.Range(0, 4).Select(k => F(Meteor(k))).ToArray();
		// embers left in the air where the meteor passed: they cool down (white, orange, red, dark) and sink
		Frame Embers(int k)
		{
			var f = new Frame();
			for (int i = 0; i < 16; i++)
			{
				int h = Hash(i, 7, 3);
				float along = 5 + h % 24, side = (h / 32 % 9) - 4;
				int x = (int)(MtX + along * TailX - side * TailY), y = (int)(MtY + along * TailY + side * TailX + k * 1.5f);
				f[x, y] = k == 0 ? (i % 3 == 0 ? Hot : i % 3 == 1 ? Yel : O) : k == 1 ? (i % 2 == 0 ? O : Flame) : (i % 2 == 0 ? FlameR : Ember);
				if (k == 0 && i % 4 == 0) f[x + 1, y] = Flame;
			}
			for (int i = 0; i < 7; i++) f[15 + i * 2 + k, 13 - i + (i % 2) - k] = k == 2 ? Gray : Ash;   // a wisp of smoke
			return f;
		}
		int[] fEmbers = Enumerable.Range(0, 3).Select(k => F(Embers(k))).ToArray();
		// the crash, on the ground line of the frame
		const float BoomX = 11.5f, BoomY = 30.5f;
		void CraterOn(Frame f, int k)
		{
			St(f, 0, 28, "..bBBb...........bBBb..", ".bBBBBb.........bBBBBb.", "bBBBBBBbsssssssbBBBBBBb",
				k == 0 ? "bBBbsssjsssOsssjsssbBBb" : "bBBbsssOsssjsssOsssbBBb");
		}
		Frame Boom(int k)
		{
			var f = new Frame();
			if (k == 0)   // white flash with rays
			{
				for (int i = 0; i < 12; i++)
				{
					float an = MathF.PI + MathF.PI * i / 11;   // the upper half (the ground is below)
					for (int r = 7; r <= 15; r++) if ((r + i) % 3 != 0) f[(int)MathF.Round(BoomX + MathF.Cos(an) * r), (int)MathF.Round(BoomY + MathF.Sin(an) * r)] = r < 11 ? Hot : Yel;
				}
				Circle(f, BoomX, BoomY, 6.5f, Hot, Yel);
				f.Rect(0, 31, 24, 1, Yel);
			}
			else if (k <= 2)   // the fireball grows, its top turns to smoke, rocks fly out
			{
				float r = k == 1 ? 9.5f : 12f, cy = BoomY - (k == 2 ? 2 : 0);
				for (int y = (int)(cy - r - 1); y < W; y++) for (int x = 0; x < W; x++)
				{
					float dx = x + 0.5f - BoomX, dy = y + 0.5f - cy, d = MathF.Sqrt(dx * dx + dy * dy);
					if (d > r) continue;
					int n = Hash(x, y, k + 30) % 100;
					float q = d / r + (n % 7) * 0.02f;
					f[x, y] = k == 2 && y < cy - 7 && q > 0.45f ? (n < 50 ? Gray : GrayD)
						: q < 0.3f ? Hot : q < 0.5f ? Yel : q < 0.7f ? O : q < 0.86f ? Flame : FlameR;
				}
				for (int i = 0; i < 10; i++)
				{
					int h = Hash(i, k, 9);
					float an = MathF.PI + MathF.PI * (h % 100) / 100f, dist = r + 1 + h / 100 % 5;
					f[(int)(BoomX + MathF.Cos(an) * dist), (int)(cy + MathF.Sin(an) * dist)] = i % 3 == 0 ? Flame : RockD;
				}
			}
			else if (k == 3)   // mushroom cloud
			{
				for (int y = 0; y < W; y++) for (int x = 0; x < W; x++)
				{
					int n = Hash(x, y, 41) % 100;
					float dx = x + 0.5f - BoomX;
					float cap = (dx / 9.5f) * (dx / 9.5f) + ((y - 7f) / 6.5f) * ((y - 7f) / 6.5f);
					float wid = 2.2f + (31 - y) / 9f;
					if (cap <= 1) f[x, y] = y > 9 && n < 55 ? (n < 25 ? Flame : FlameR) : n < 30 ? GrayL : n < 75 ? Gray : GrayD;
					else if (y >= 12 && MathF.Abs(dx) <= wid) f[x, y] = y > 26 ? (n < 50 ? Yel : O) : y > 20 ? (n < 50 ? Flame : FlameR) : n < 50 ? Ash : GrayD;
					else if (y >= 28 && (dx / 12.5f) * (dx / 12.5f) + ((y - 31.5f) / 3.5f) * ((y - 31.5f) / 3.5f) <= 1) f[x, y] = n < 50 ? Brown : BrownD;   // dust ring
				}
			}
			else   // the smoke clears and leaves a crater
			{
				for (int y = -6; y < 26; y++) for (int x = 0; x < W; x++)
				{
					int n = Hash(x, y, 51) % 100;
					float dx = x + 0.5f - BoomX, cap = (dx / 11f) * (dx / 11f) + ((y - 5f) / 7.5f) * ((y - 5f) / 7.5f);
					if (cap <= 1 && n < 55) f[x, y] = n < 25 ? GrayL : Gray;
				}
				for (int y = 13; y < 28; y++) f[(int)(BoomX + MathF.Sin(y * 0.5f) * 1.5f), y] = Gray;
				CraterOn(f, 0);
			}
			return f;
		}
		int[] fBoom = Enumerable.Range(0, 5).Select(k => F(Boom(k))).ToArray();
		Frame Crater(int k)
		{
			var f = new Frame();
			CraterOn(f, k);
			for (int i = 0; i < 6; i++)
			{
				int x = (int)(BoomX + MathF.Sin((i + k * 1.5f) * 0.9f) * 2), y = 26 - i * 3 - k;
				f.Dot(x, y, i < 3 ? Gray : GrayL); if (i % 2 == 0) f.Dot(x + 1, y, GrayL);
			}
			return f;
		}
		int[] fCrater = { F(Crater(0)), F(Crater(1)) };
		Frame Debris(int k)
		{
			var f = new Frame();
			foreach (var (x, y, big) in new[] { (9, 22, true), (14, 18, false), (5, 16, false), (17, 24, true) })
			{
				if (big)
				{
					f.Rect(x, y, 3, 2, RockM).Dot(x + (k == 0 ? 0 : 2), y, RockL).Dot(x + (k == 0 ? 2 : 0), y + 1, Flame).Dot(x + 1, y + 2, RockD);
				}
				else f.Rect(x, y, 2, 2, RockD).Dot(x + k, y, Flame);
				f.Dot(x + 3, y - 1, Ash).Dot(x + 4, y - 2, k == 0 ? Ash : Gray);   // smoke trail
			}
			return f;
		}
		int[] fDebris = { F(Debris(0)), F(Debris(1)) };
		Frame SmokeCloud(int k)
		{
			var f = new Frame();
			foreach (var (x, y, r) in new[] { (8f, 3f, 4.2f), (15f, 1f, 5f), (21f, 4f, 3.8f), (12f, 7f, 3.5f) })
				Circle(f, x + k * 0.6f, y - k * 0.5f, r + k * 0.3f, k == 0 ? Gray : GrayL, GrayD);
			return f;
		}
		int[] fSmokeCloud = { F(SmokeCloud(0)), F(SmokeCloud(1)) };
		// Rex sees it: points up, the glow lights his head and back, he ducks, then wipes his forehead
		var mPoint = Pose(eye: 5, jaw: 4, arm: 1); Fx(mPoint, "shock"); int fPointUp = F(mPoint);
		Frame Lit(Frame src)
		{
			var o = src.Clone();
			for (int y = src.MinY; y < W; y++) for (int x = 0; x < W; x++)
				if ((src[x, y] == G || src[x, y] == M) && (src[x, y - 1] == '.' || src[x + 1, y] == '.')) o[x, y] = src[x, y] == G ? Yel : O;
			return o;
		}
		int fPointLit = F(Lit(Copy(fPointUp))), fShockLit1 = F(Lit(Copy(fShock1))), fShockLit2 = F(Lit(Copy(fShock2)));
		var mDuck = Squash(Pose(eye: 6, jaw: 1, arm: 1, farUp: true, tail: -1), 3); Fx(mDuck, "shiver"); int fDuck = F(mDuck);
		var mPhew1 = Pose(eye: 1, jaw: 1, arm: 1); Fx(mPhew1, "sweat", 0); int fPhew1 = F(mPhew1);
		var mPhew2 = Pose(eye: 2, arm: 1, blush: true); Fx(mPhew2, "sweat", 1); int fPhew2 = F(mPhew2);

		// --- shooting star: he makes a wish
		Frame Streak(int k)
		{
			var f = new Frame();
			for (int i = 0; i < 16; i++)
			{
				char c = i < 3 ? Wt : i < 6 ? Glass : i < 11 ? (i % 2 == 0 ? Glass : '.') : (i % 3 == 0 ? GrayL : '.');
				if (c != '.') f[6 + i, 14 - i / 2] = c;
			}
			Star(f, 5, 14, k == 0 ? Wt : Yel);
			if (k == 1) f.Dot(3, 14, Glass).Dot(7, 12, Glass).Dot(5, 11, Glass).Dot(5, 17, Glass);
			return f;
		}
		int[] fStreak = { F(Streak(0)), F(Streak(1)) };
		var ws1 = Pose(eye: 1, arm: 2, blush: true); Star(ws1, 20, 2, Yel); Star(ws1, 26, 7, PurpL); int fWish1 = F(ws1);
		var ws2 = Pose(eye: 1, arm: 2, tail: 1, blush: true); Star(ws2, 23, 0, Mint); Star(ws2, 18, 5, Yel); int fWish2 = F(ws2);

		// --- fireworks: the rocket goes up (child) and bursts twice (a second, later burst is its child)
		void Rocket(Frame f, int x, int y, bool lit)
		{
			St(f, x, y, ".Y.", "HWH", "HHH", "WHW", "HHH");
			f.Rect(x + 1, y + 5, 1, 6, BrownD);
			if (lit) f.Dot(x + 1, y + 11, Yel).Dot(x, y + 12, O).Dot(x + 2, y + 12, Flame).Dot(x + 1, y + 13, FlameR);
		}
		var rk1 = Pose(eye: 2, arm: 2, blush: true); Rocket(rk1, 2, 9, false); int fRocket1 = F(rk1);
		var rk2 = Squash(Pose(eye: 6, arm: 2, tail: -1), 2); Rocket(rk2, 1, 17, true); int fRocket2 = F(rk2);
		Frame RocketFly(int k)
		{
			var f = new Frame(); Rocket(f, 11, 10, true);
			if (k == 1) f.Dot(11, 22, Yel).Dot(13, 23, Flame);
			for (int i = 0; i < 4; i++) f.Dot(12 + (i + k) % 2, 25 + i * 2, i < 2 ? Gray : GrayL);
			return f;
		}
		int[] fRocketFly = { F(RocketFly(0)), F(RocketFly(1)) };
		char[] burstCol = { Red, Yel, Mint, PurpL, Blue, Pink };
		Frame Burst(int k, int shift)
		{
			var f = new Frame();
			float[] rad = { 3, 6.5f, 10, 11.5f };
			for (int i = 0; i < 12; i++)
			{
				float an = MathF.PI * 2 * i / 12 + (i % 2) * 0.12f, r = rad[k], droop = k >= 2 ? (k - 1) * 1.3f : 0;
				char c = burstCol[(i + shift) % burstCol.Length];
				for (int s = 0; s < (k == 0 ? 1 : 3); s++)   // the head of each spark and its trail
				{
					float rr = r - s * 1.6f; if (rr < 1) continue;
					if (k == 3 && s == 0 && i % 3 == 0) continue;   // fading out
					f[(int)MathF.Round(12 + MathF.Cos(an) * rr), (int)MathF.Round(6 + MathF.Sin(an) * rr + droop * (rr / r))] = s == 0 ? (k == 0 ? Wt : c) : s == 1 ? c : (k >= 2 ? Ember : c);
				}
			}
			if (k <= 1) Star(f, 12, 6, k == 0 ? Wt : Yel);
			return f;
		}
		int[] fBurst = Enumerable.Range(0, 4).Select(k => F(Burst(k, 0))).ToArray();
		int[] fBurst2 = Enumerable.Range(0, 4).Select(k => F(Burst(k, 3))).ToArray();
		Frame Twinkle(int k)
		{
			var f = new Frame();
			for (int i = 0; i < 14; i++) { int h = Hash(i, 2, 5); if ((i + k) % 2 == 0) f.Dot(1 + h % 23, h / 32 % 17, burstCol[i % burstCol.Length]); }
			return f;
		}
		int[] fTwinkle = { F(Twinkle(0)), F(Twinkle(1)) };

		// --- fire breath (child: a burst of flame flies forward)
		var fb1 = Lean(Pose(eye: 4, jaw: 2, arm: 1, tail: 1), -1, 26); St(fb1, 0, 13, ".jYq", "jYqq", ".JjY"); int fFire1 = F(fb1);
		var fb2 = Lean(Pose(eye: 4, jaw: 2, arm: 2, farUp: true, tail: 1), -2, 26); St(fb2, 0, 13, "jYqY", "Yqqq", "jJjY"); int fFire2 = F(fb2);
		Frame FlameBurst(int k)
		{
			var f = new Frame();
			for (int x = 6; x <= 28; x++)
			{
				float t = (28 - x) / 22f;   // 0 at the mouth, 1 at the tip
				float half = 1.2f + t * 5.5f + MathF.Sin(x * 0.9f + k * 2f) * 0.8f * t;
				for (int y = 4; y <= 26; y++)
				{
					float dy = MathF.Abs(y + 0.5f - (14.5f - t * 1.5f));
					if (dy > half) continue;
					int n = Hash(x, y, k + 20) % 100;
					float q = dy / half, heat = (1 - t) * (1.1f - q * 0.6f) + (1 - q) * 0.25f + (n % 7) * 0.02f;
					char c = heat > 0.85f ? Hot : heat > 0.65f ? Yel : heat > 0.48f ? O : heat > 0.32f ? Flame : heat > 0.2f ? FlameR : n < 50 ? Ember : n < 70 ? Ash : '.';
					if (c != '.') f[x, y] = c;
				}
			}
			return f;
		}
		int[] fFlameB = { F(FlameBurst(0)), F(FlameBurst(1)) };

		// --- snowball fight (child: the snowball flies in an arc and splats)
		var sn1 = Squash(Pose(eye: 0, arm: 2, tail: -1), 3); St(sn1, 0, 28, "..WWW..", ".WWlWW.", "WWWWWlW", "lWWWWWW"); Circle(sn1, 4.5f, 23.5f, 1.8f, Wt, GrayL); int fSnow1 = F(sn1);
		var sn2 = Pose(eye: 4, jaw: 1, arm: 1, tail: 1); Circle(sn2, 4f, 13f, 2f, Wt, GrayL); int fSnow2 = F(sn2);
		int fSnow3 = F(Lean(Pose(eye: 2, jaw: 1, arm: 2, feet: 1), -1, 28));
		Frame Snowball(int k) { var f = new Frame(); Circle(f, 4f, 29f, 2f, Wt, GrayL); f.Dot(k == 0 ? 3 : 4, k == 0 ? 28 : 29, GrayL); return f; }
		int[] fSnowball = { F(Snowball(0)), F(Snowball(1)) };
		Frame Splat(int k)
		{
			var f = new Frame();
			if (k == 0) { St(f, 0, 29, "..W.W..", ".WWlWW.", "WWWWWWW"); f.Dot(1, 26, Wt).Dot(6, 25, Wt).Dot(8, 27, GrayL); }
			else { St(f, 0, 30, "W.WlW.WW", "WWWWWWWWW"); f.Dot(0, 27, GrayL).Dot(9, 28, Wt); }
			return f;
		}
		int[] fSplat = { F(Splat(0)), F(Splat(1)) };

		// --- ninja (child: a spinning throwing star)
		void NinjaMask(Frame f, int dx)
		{
			f.Rect(5 + dx, 8, 13, 3, Blk);
			f.Dot(11 + dx, 9, Wt).Dot(12 + dx, 9, E).Dot(13 + dx, 9, Wt);
			f.Dot(18 + dx, 8, Red).Dot(19 + dx, 7, Red).Dot(20 + dx, 8, Red).Dot(21 + dx, 9, Red).Dot(19 + dx, 9, Red);
		}
		void Shuriken(Frame f, int x, int y, int k) => St(f, x, y, k % 2 == 0
			? new[] { "..g..", ".gkg.", "gkKkg", ".gkg.", "..g.." }
			: new[] { "g...g", ".gkg.", "..K..", ".gkg.", "g...g" });
		var nj1 = Lean(Pose(eye: 4, arm: 2, tail: 1), -1, 28); NinjaMask(nj1, -1); Shuriken(nj1, 1, 16, 0); int fNinja1 = F(nj1);
		var nj2 = Lean(Pose(eye: 4, jaw: 1, arm: 2, feet: 1), -2, 28); NinjaMask(nj2, -2); Lines(nj2, (0, 17, 2, false), (0, 21, 2, false)); int fNinja2 = F(nj2);
		int[] fShuriken = Enumerable.Range(0, 2).Select(k => { var f = new Frame(); Shuriken(f, 1, 16, k); return F(f); }).ToArray();

		// --- graduation (child: the cap flies up spinning)
		void GradCap(Frame f, bool tilt)
		{
			if (!tilt) { St(f, 5, 3, "...KKKKKKK...", "KKKKKKKKKKKKK", "..kKKKKKKKk.."); f.Dot(16, 5, Yel).Dot(16, 6, Yel).Dot(16, 7, YelD); }
			else { St(f, 7, 2, "......KKKK", "...KKKKKKK", "KKKKKKKKk.", "KKKKk....."); f.Dot(9, 6, Yel).Dot(9, 7, YelD); }
		}
		var gr1 = Pose(eye: 2, arm: 2, blush: true); GradCap(gr1, false); St(gr1, 0, 18, "lWWHWWl", "lWWHWWl"); int fGrad1 = F(gr1);
		int fGrad2 = F(Up(Pose(eye: 2, jaw: 1, arm: 1, farUp: true, tail: 1), 2));
		int[] fCap = Enumerable.Range(0, 2).Select(k => { var f = new Frame(); GradCap(f, k == 1); return F(f); }).ToArray();

		// --- boomerang (child: it flies out and comes back)
		string[][] boomShapes = { new[] { "BBBb", "B...", "b..." }, new[] { "bBBB", "...B", "...b" }, new[] { "...b", "...B", "bBBB" }, new[] { "b...", "B...", "BBBb" } };
		var bm1 = Pose(eye: 0, arm: 1); St(bm1, 1, 11, boomShapes[0]); int fBoomHold = F(bm1);
		var bm2 = Pose(eye: 2, arm: 2, blush: true); St(bm2, 1, 17, boomShapes[1]); int fBoomCatch = F(bm2);
		int[] fBoomer = boomShapes.Select(s => { var f = new Frame(); St(f, 1, 11, s); return F(f); }).ToArray();

		// --- kite (child: the string breaks and it flies away)
		void KiteOn(Frame f, int x, int y, int k, bool tied)
		{
			St(f, x, y, "..H..", ".HYH.", "HYYYH", ".HYH.", "..H..");
			f.Dot(x + 2 + k, y + 5, Yel).Dot(x + 3 - k, y + 6, Blue).Dot(x + 2 + k, y + 7, Yel);
			if (tied) Line(f, x + 2, y + 4, 4, 15, GrayL); else Line(f, x + 2, y + 4, x + 4, y + 11, GrayL);
		}
		var kt1 = Pose(eye: 5, arm: 1, blush: true); KiteOn(kt1, 1, -6, 0, true); int fKite1 = F(kt1);
		var kt2 = Pose(eye: 2, arm: 1, tail: 1); KiteOn(kt2, 2, -5, 1, true); int fKite2 = F(kt2);
		int[] fKiteAway = Enumerable.Range(0, 2).Select(k => { var f = new Frame(); KiteOn(f, 1 + k, -6 + k, k, false); return F(f); }).ToArray();

		// --- soap bubbles (child, three times)
		void Wand(Frame f) { Ring(f, 2.5f, 12.5f, 2.3f, GrayL); Line(f, 3, 15, 5, 19, GrayL); }
		var bw1 = Pose(eye: 1, jaw: 4, arm: 2); Wand(bw1); int fBlow1 = F(bw1);
		var bw2 = Pose(eye: 1, jaw: 4, arm: 2, blush: true); Wand(bw2); Ring(bw2, 1.5f, 11f, 2.8f, Glass); int fBlow2 = F(bw2);
		Frame SoapBubbles(int k)
		{
			var f = new Frame();
			foreach (var (x, y, r) in new[] { (3f, 8f, 2.4f), (8f, 3f, 1.7f), (1.5f, 1f, 1.3f), (12f, 7f, 1.3f) })
			{
				Ring(f, x + k * 0.5f, y - k * 0.5f, r, Glass);
				f.Dot((int)(x + k * 0.5f - r * 0.4f), (int)(y - k * 0.5f - r * 0.4f), Wt);
			}
			return f;
		}
		int[] fSoap = { F(SoapBubbles(0)), F(SoapBubbles(1)) };

		// --- an egg hatches (child: the baby dino, a half-size Rex with a bit of shell on its head, walks off)
		void EggOn(Frame f, int dx, int stage)   // 0 whole, 1 cracked, 2 hatched
		{
			if (stage < 2) St(f, dx, 23, "..lll..", ".lWWWl.", "lWWuWWl", "lWWWWWl", "lWuWWWl", "lWWWWul", "lWWWWWl", ".lWWWl.", "..lll..");
			if (stage == 1) f.Dot(dx + 1, 27, Blk).Dot(dx + 2, 26, Blk).Dot(dx + 3, 27, Blk).Dot(dx + 4, 26, Blk).Dot(dx + 5, 27, Blk);
			if (stage == 2) St(f, dx, 27, "l.l.l.l", "lWlWlWl", "lWWWWul", ".lWWWl.", "..lll..");
		}
		var eg1 = Lean(Pose(eye: 3, jaw: 4, arm: 2), -1, 28); EggOn(eg1, 0, 0); int fEgg1 = F(eg1);
		var eg2 = Pose(eye: 0, arm: 2); EggOn(eg2, 1, 0); int fEgg2 = F(eg2);
		var eg2b = Pose(eye: 0, arm: 2); EggOn(eg2b, 0, 0); eg2b.Dot(0, 22, D); int fEgg2b = F(eg2b);
		var eg3 = Pose(eye: 3, jaw: 2, arm: 1, farUp: true); EggOn(eg3, 0, 1); Fx(eg3, "shock"); int fEgg3 = F(eg3);
		var eg4 = Pose(eye: 2, jaw: 1, arm: 2, blush: true); EggOn(eg4, 0, 2); Star(eg4, 2, 22, Mint); int fEgg4 = F(eg4);
		var eg5 = Pose(eye: 2, blush: true, tail: 1); EggOn(eg5, 0, 2); int fEggLove = F(eg5);
		Frame Baby(int k)
		{
			var a = Body(new Look { Feet = k == 0 ? 1 : 2, Tail = k == 0 ? 1 : -1 });
			var f = new Frame();
			for (int r = 0; r < 13; r++) for (int c = 0; c < 13; c++)
			{
				char best = '.';
				for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++)
				{
					int ar = r * 2 + j, ac = c * 2 + i;
					if (ar >= AR || ac >= AC || a[ar, ac] == '.') continue;
					char ch = a[ar, ac];
					if (ch == E || best == '.' || ((ch == O || ch == R) && best != E)) best = ch;
				}
				if (best != '.') f[c + 1, r + 19] = best;
			}
			St(f, 2, 17, ".W.W.W", "WWWWWW");   // a piece of shell on the head
			if (k == 1) f.Shift(0, -1);
			return f;
		}
		int[] fBaby = { F(Baby(0)), F(Baby(1)) };

		// --- a rain cloud only over him (child), he gets wet, shakes the water off
		void CloudOn(Frame f, int k, bool rain)
		{
			foreach (var (x, y, r) in new[] { (9f, -2f, 3.2f), (14f, -2.5f, 3.6f), (19.5f, -2f, 3f) }) Circle(f, x, y, r, Gray, Gray);
			f.Rect(6, -1, 17, 2, Gray).Rect(7, 1, 15, 1, GrayD).Dot(12, -5, GrayL).Dot(13, -5, GrayL).Dot(8, -4, GrayL);
			if (rain) for (int i = 0; i < 6; i++) { int x = 8 + i * 3, y = 2 + (i * 5 + k * 2) % 5; f.Dot(x, y, Water).Dot(x, y + 1, Water); }
		}
		int[] fCloud = Enumerable.Range(0, 4).Select(k => { var f = new Frame(); CloudOn(f, k, k < 3); return F(f); }).ToArray();
		Frame Wet(int k)
		{
			var f = Squash(Pose(eye: k == 0 ? 7 : 1, tail: -1), 1);
			foreach (var (x, y) in k == 0 ? new[] { (10, 7), (15, 9), (6, 13), (20, 17) } : new[] { (12, 8), (17, 7), (8, 11), (22, 19) }) Drop(f, x, y);
			Behind(f, 2, 31, "wwwwwwwwwwwwwwwwwwwwwwwww");
			return f;
		}
		int fWet1 = F(Wet(0)), fWet2 = F(Wet(1));
		var sh1 = Pose(eye: 6, arm: 2).Shift(-1, 0); foreach (var (x, y) in new[] { (1, 8), (24, 5), (27, 14), (0, 19) }) Drop(sh1, x, y); int fShake1 = F(sh1);
		var sh2 = Pose(eye: 6, arm: 1).Shift(1, 0); foreach (var (x, y) in new[] { (2, 4), (28, 9), (25, 20), (1, 15) }) Drop(sh2, x, y); int fShake2 = F(sh2);

		// --- a bird sits on his head, sings, then flies away (child)
		void BirdSit(Frame f) => St(f, 11, 1, ".UU..", "YEUU.", ".UUUu", "..UUU", "..y.y");
		var br1 = Pose(eye: 5); BirdSit(br1); int fBird1 = F(br1);
		var br2 = Pose(eye: 5, jaw: 4); BirdSit(br2); Note(br2, 6, -5); int fBird2 = F(br2);
		var br3 = Pose(eye: 2, tail: 1, blush: true); BirdSit(br3); int fBird3 = F(br3);
		int[] fBirdFly =
		{
			F(new Frame().Stamp(11, 0, "U   U ", " U U  ", "YEUUU ", " UUUu ")),
			F(new Frame().Stamp(11, 1, "YEUUU ", " UUUu ", " U U  ", "U   U ")),
		};

		// --- butterfly net (child: the butterfly escapes)
		void Bfly(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "PP.PP", "PFKFP", ".PKP.", "..K.." } : new[] { ".P.P.", ".PKP.", ".FKF.", "..K.." });
		Frame Net(bool up, int k)
		{
			var f = up ? Pose(eye: 5, arm: 1) : Lean(Pose(eye: 6, jaw: 1, arm: 2), -1, 28);
			if (up) { Line(f, 5, 15, 9, 0, BrownD); Ring(f, 10f, -3f, 2.8f, GrayL); f.Dot(10, -3, Glass).Dot(9, -2, Glass); Bfly(f, 0, 5 + k, k); }
			else { Line(f, 4, 20, 2, 25, BrownD); Ring(f, 2.5f, 28.5f, 2.8f, GrayL); }
			return f;
		}
		int fNet1 = F(Net(true, 0)), fNet1b = F(Net(true, 1)), fNet2 = F(Net(false, 0));
		int[] fBfly = Enumerable.Range(0, 2).Select(k => { var f = new Frame(); Bfly(f, 0, 5 + k, k); return F(f); }).ToArray();

		// --- a gift: a balloon comes out and floats away (child: the balloon of the "balloon" scene)
		void GiftOn(Frame f, int dx, bool open)
		{
			St(f, dx + 1, 25, "HHHYHHH", "HhHYHHH", "YYYYYYY", "HHHYHHH", "HHHYHhH", "HHHYHHH", "HHHYHHH");
			if (!open) { St(f, dx, 23, "hhhhYhhhh", "HHHHYHHHH"); St(f, dx + 2, 21, "Y...Y", ".YYY."); }
		}
		var gf1 = Pose(eye: 2, arm: 2, blush: true); GiftOn(gf1, 0, false); int fGift1 = F(gf1);
		var gf2 = Squash(Pose(eye: 3, jaw: 4, arm: 2), 1); GiftOn(gf2, 1, false); int fGift2 = F(gf2);
		var gf2b = Squash(Pose(eye: 3, jaw: 4, arm: 2), 1); GiftOn(gf2b, 0, false); int fGift2b = F(gf2b);
		var gf3 = Pose(eye: 3, jaw: 2, arm: 1, farUp: true); GiftOn(gf3, 0, true); Hold(gf3, "balloon"); Line(gf3, 5, 19, 4, 24, GrayL); int fGift3 = F(gf3);
		var gf4 = Pose(eye: 2, jaw: 1, arm: 1, farUp: true, tail: 1); GiftOn(gf4, 0, true); Fx(gf4, "confetti", 0); int fGift4 = F(gf4);

		// --- bubble gum: chews, the bubble grows and pops on his face
		Frame Gum(int stage)
		{
			if (stage <= 1) { var g0 = Pose(eye: 0, jaw: stage); g0.Dot(5, 15, Pink); return g0; }
			if (stage == 6)
			{
				var g6 = Pose(eye: 3, jaw: 4);
				foreach (var (x, y) in new[] { (4, 11), (5, 13), (6, 9), (8, 12), (3, 15), (7, 16), (10, 10), (5, 17), (9, 14) }) g6.Dot(x, y, Pink);
				return Fx(g6, "shock");
			}
			var g = Pose(eye: stage >= 4 ? 3 : 1, jaw: 0, blush: stage == 5);
			float r = new[] { 1.4f, 2.4f, 3.5f, 4.7f }[stage - 2];
			Circle(g, 5f - r * 0.7f, 15f, r, Pink, Tongue);
			g.Dot((int)(5f - r * 1.1f), (int)(15f - r * 0.5f), Wt);
			return g;
		}
		int[] fGum = Enumerable.Range(0, 7).Select(k => F(Gum(k))).ToArray();

		// --- ice cream, coffee (then he gets hyper), watermelon
		void Cone(Frame f, int bite)
		{
			St(f, 1, 12, bite == 0 ? new[] { "..H..", ".FWF.", "FFWFF", "FFFFF" } : new[] { ".....", ".....", "F.FFF", "FFFFF" });
			St(f, 1, 16, "BbBbB", ".BbB.", ".bBb.", "..B..");
			if (bite == 1) f.Dot(1, 16, Pink).Dot(1, 17, Pink);   // a drip
		}
		var ic1 = Pose(eye: 0, arm: 2); Cone(ic1, 0); int fIce1 = F(ic1);
		var ic2 = Pose(eye: 1, jaw: 3, arm: 2, blush: true); Cone(ic2, 0); int fIce2 = F(ic2);
		var ic3 = Pose(eye: 2, arm: 2, blush: true); Cone(ic3, 1); int fIce3 = F(ic3);
		void Mug(Frame f, int y, int steam)
		{
			St(f, 0, y, "lWWWl..", "WbbbWl.", "WWWWW.l", "WWWWWl.", ".WWW...");
			if (steam >= 0) for (int i = 0; i < 5; i++) f.Dot(1 + (i + steam) % 2 + (i == 4 ? 1 : 0), y - 2 - i, GrayL);
		}
		var cf1 = Pose(eye: 0, arm: 2); Mug(cf1, 16, 0); int fCoffee1 = F(cf1);
		var cf1b = Pose(eye: 0, arm: 2); Mug(cf1b, 16, 1); int fCoffee1b = F(cf1b);
		var cf2 = Pose(eye: 1, arm: 2, blush: true); Mug(cf2, 12, -1); int fCoffee2 = F(cf2);
		var hy1 = Pose(eye: 3, jaw: 2, arm: 1, farUp: true, tail: 1); Fx(hy1, "shock"); hy1.Shift(-1, 0); int fHyper1 = F(hy1);
		var hy2 = Pose(eye: 3, jaw: 1, arm: 2, tail: -1); Fx(hy2, "shock"); hy2.Shift(1, 0); int fHyper2 = F(hy2);
		void Melon(Frame f, int bite)
		{
			St(f, 0, 13, bite == 0 ? new[] { "...H...", "..HKH..", ".HHHHH." } : bite == 1 ? new[] { ".......", ".......", ".HH.HH." } : new[] { ".......", ".......", "......." });
			St(f, 0, 16, bite < 2 ? "HKHHHKH" : "H.H.H.H", "HHHHHHH", "WWWWWWW", "LLLLLLL", ".LLLLL.");
		}
		var ml1 = Pose(eye: 2, jaw: 2, arm: 2); Melon(ml1, 0); int fMelon1 = F(ml1);
		var ml2 = Pose(eye: 1, jaw: 0, arm: 2, blush: true); Melon(ml2, 1); int fMelon2 = F(ml2);
		var ml3 = Pose(eye: 2, jaw: 1, arm: 2, blush: true); Melon(ml3, 2); ml3.Dot(2, 14, Blk).Dot(0, 11, Blk); int fMelon3 = F(ml3);

		// --- toys and sports
		Frame YoYo(int y, int eye)
		{
			var f = Pose(eye: eye, arm: 2);
			for (int yy = 21; yy < y - 1; yy++) f.Dot(5, yy, GrayL);
			Circle(f, 5.5f, y + 0.5f, 1.8f, RedL, Red);
			f.Dot(5, y, Wt);
			return f;
		}
		int[] fYo = { F(YoYo(22, 2)), F(YoYo(26, 0)), F(YoYo(29, 0)) };
		Frame Skate(int k)
		{
			var f = Lean(Up(k == 0 ? Pose(eye: 2, arm: 2, tail: 1, blush: true) : Pose(eye: 0, jaw: 1, arm: 1, farUp: true, tail: -1), 3), k == 0 ? -1 : -2, 26);
			f.Rect(5, 29, 18, 1, BrownD).Dot(4, 28, BrownD).Dot(23, 28, BrownD);
			f.Rect(7, 30, 2, 2, GrayD).Rect(19, 30, 2, 2, GrayD).Dot(7, 30, Gray).Dot(19, 30, Gray);
			if (k == 1) Lines(f, (25, 18, 4, false), (27, 22, 4, false), (24, 26, 3, false));
			return f;
		}
		int fSkate1 = F(Skate(0)), fSkate2 = F(Skate(1));
		Frame Rope(int k)
		{
			var f = k == 2 ? Up(Pose(eye: 2, feet: 3, arm: 2, tail: 1), 4) : k == 1 ? Squash(Pose(eye: 0, arm: 2), 1) : Pose(eye: 2, arm: 2, tail: -1);
			if (k == 0) Oval(f, 15f, 18f, 13f, 22f, Red, s => s <= 0, behind: true);   // over his head, behind him
			if (k == 1) Oval(f, 15f, 21f, 13f, 6f, Red, s => s >= 0);                    // swinging down in front
			if (k == 2) Oval(f, 15f, 15f, 13f, 16f, Red, s => s >= 0);                   // under his feet
			int hy = k == 1 ? 20 : k == 2 ? 14 : 17;
			f.Rect(1, hy, 2, 3, BrownD).Rect(28, hy, 2, 3, BrownD);
			return f;
		}
		int[] fRope = Enumerable.Range(0, 3).Select(k => F(Rope(k))).ToArray();
		Frame Lift(int k)
		{
			var f = k == 0 ? Pose(eye: 0, arm: 2) : Squash(Pose(eye: 6, jaw: 1, arm: 1, farUp: true, tail: 1), 1);
			St(f, 0, k == 0 ? 19 : 13, "K...K", "KgggK", "K...K");
			if (k == 1) Fx(f, "sweat", 0);
			return f;
		}
		int fLift0 = F(Lift(0)), fLift1 = F(Lift(1));
		Frame Hula(int k)   // k = -1, 0, 1: the tilt of the hoop and of his hips
		{
			var f = Lean(Pose(eye: 2, jaw: 1, arm: 1, farUp: true, tail: k, blush: true), -k, 28);
			for (int i = 0; i < 90; i++)
			{
				float an = MathF.PI * 2 * i / 90, s = MathF.Sin(an), c = MathF.Cos(an);
				int x = (int)MathF.Round(15 + c * 12), y = (int)MathF.Round(23 + s * 2.5f + c * k * 2.5f);
				if (s >= 0 || f[x, y] == '.') f[x, y] = (i / 5) % 2 == 0 ? PurpL : Pink;   // the front half over him, the back behind
			}
			return f;
		}
		int[] fHula = { F(Hula(-1)), F(Hula(0)), F(Hula(1)) };
		Frame Zen(int k)
		{
			int lift = new[] { 0, 1, 2, 1 }[k];
			var f = Up(Squash(Pose(eye: 1, arm: 2, tail: -1, blush: k == 2), 3), lift);
			foreach (var (x, y, c) in new[] { (3, 4, PurpL), (23, 2, Mint), (27, 12, Yel), (1, 14, Mint) }) if ((x + k) % 2 == 0 || k == 2) Star(f, x, y - k % 2, c);
			if (lift > 0) Behind(f, 9 + lift, 31, new string('k', 14 - lift * 2));   // his shadow on the floor
			return f;
		}
		int[] fZen = Enumerable.Range(0, 4).Select(k => F(Zen(k))).ToArray();
		Frame Umbrella(int k)
		{
			var f = Pose(eye: 2, arm: 2, tail: k == 0 ? 1 : -1, blush: true);
			Line(f, 9, 1, 5, 19, BrownD); f.Dot(5, 21, BrownD).Dot(6, 22, BrownD).Dot(7, 21, BrownD);
			for (int x = 0; x <= 20; x++)
			{
				float u = (x - 10) / 10f;
				int top = (int)MathF.Round(-5 + u * u * 4), bottom = x % 4 == 0 ? 0 : 1;   // scalloped edge
				for (int y = top; y <= bottom; y++) f[x, y] = (x / 4) % 2 == 0 ? Red : Wt;
			}
			f.Dot(10, -6, BrownD);
			int[] rx = { 22, 25, 28, 24, 30, 27, 23 };
			for (int i = 0; i < rx.Length; i++) Drop(f, rx[i], (i * 7 + k * 4) % 30 - 4);
			return f;
		}
		int[] fUmb = { F(Umbrella(0)), F(Umbrella(1)) };
		void Headband(Frame f) { f.Rect(9, 7, 10, 1, Wt); f.Dot(19, 7, Wt).Dot(20, 8, Wt).Dot(19, 9, Wt).Dot(21, 9, Wt); }
		var kr1 = Pose(eye: 4, arm: 2, tail: 1); Headband(kr1); int fKarate1 = F(Squash(kr1, 1));
		var kr2 = Pose(eye: 4, jaw: 1, arm: 2); Headband(kr2); Lean(kr2, -2, 28); Lines(kr2, (0, 17, 2, false), (0, 21, 2, false)); int fKarate2 = F(kr2);
		var kr3 = Pose(eye: 4, jaw: 2, feet: 1, tail: 1); Headband(kr3); Lean(kr3, 2, 30); Lines(kr3, (1, 27, 3, false), (0, 29, 3, false)); int fKarate3 = F(kr3);

		// --- small gags
		var hc = Up(Pose(eye: 3, jaw: 4, arm: 3), 1); Fx(hc, "shock"); int fHic = F(hc);
		Frame HicBubble(int k) { var f = new Frame(); Ring(f, 3f, 12f - k, 1.4f, Glass); f.Dot(2, 11 - k, Wt); return f; }
		int[] fHicBub = { F(HicBubble(0)), F(HicBubble(1)) };
		var lg1 = Lean(Pose(eye: 6, jaw: 2, arm: 2, tail: 1, blush: true), 2, 26); Drop(lg1, 15, 11); int fLaugh1 = F(lg1);
		var lg2 = Up(Lean(Pose(eye: 6, jaw: 1, arm: 2, tail: -1, blush: true), 1, 26), 1); Drop(lg2, 14, 12); int fLaugh2 = F(lg2);
		int fOnBack1 = F(OnBack(0)), fOnBack2 = F(OnBack(1));
		Frame Selfie(int k)
		{
			var f = Pose(eye: k == 1 ? 1 : 2, jaw: k == 1 ? 0 : 1, arm: 1, tail: 1, blush: true);
			St(f, 1, 10, "KKK", "KcK", "KcK", "KKK");
			if (k == 2) { Star(f, 2, 8, Wt); f.Dot(2, 5, Wt).Dot(0, 8, Yel).Dot(5, 7, Yel).Dot(2, 4, Yel); }
			return f;
		}
		int[] fSelfie = Enumerable.Range(0, 3).Select(k => F(Selfie(k))).ToArray();
		void Teddy(Frame f) => St(f, 0, 16, "bb...bb", "bBBBBBb", ".BKBKB.", ".BBbBB.", "..BBB..", ".BBBBB.", "BBBBBBB", ".BBBBB.", ".bb.bb.");
		var td1 = Pose(eye: 1, arm: 2, blush: true); Teddy(td1); int fTeddy1 = F(td1);
		var td2 = Pose(eye: 2, arm: 2, tail: 1, blush: true); Teddy(td2); Heart(td2, 16, -4); int fTeddy2 = F(td2);
		Frame Idea(int k)   // 0 thinking, 1 bulb off, 2 and 3 bulb on
		{
			var f = k == 0 ? Pose(eye: 5, arm: 2) : k == 1 ? Pose(eye: 5) : k == 2 ? Up(Pose(eye: 3, jaw: 1, arm: 1, farUp: true), 1) : Pose(eye: 2, jaw: 1, arm: 1, blush: true);
			if (k >= 1)
			{
				char b = k == 1 ? GrayL : Yel;
				St(f, 11, -5, ".bbb.".Replace('b', b), (b + "" + b + "W" + b + b), new string(b, 5), "." + new string(b, 3) + ".", ".ggg.", ".kkk.");
				if (k >= 2) foreach (var (x, y) in k == 2 ? new[] { (9, -5), (17, -5), (8, -2), (18, -2), (13, -6) } : new[] { (8, -4), (18, -4), (9, -1), (17, -1) }) f.Dot(x, y, YelD);
			}
			return f;
		}
		int[] fIdea = Enumerable.Range(0, 4).Select(k => F(Idea(k))).ToArray();
		void BoxOn(Frame f)
		{
			f.Rect(4, 19, 22, 13, Brown);
			f.Rect(4, 19, 22, 1, BrownD).Rect(4, 19, 1, 13, BrownD).Rect(25, 19, 1, 13, BrownD).Rect(4, 31, 22, 1, BrownD);
			f.Rect(13, 20, 4, 4, YelD);
			St(f, 0, 15, "bB....", ".bB...", "..bB..", "...bB.");
			St(f, 26, 15, "....Bb", "...Bb.", "..Bb..", ".Bb...");
			f.Dot(10, 24, BrownD).Dot(9, 25, BrownD).Dot(11, 25, BrownD).Dot(10, 25, BrownD).Dot(10, 26, BrownD).Dot(10, 27, BrownD);   // "this side up"
		}
		Frame Box(int k)   // 0 hidden, 1 the eye over the edge, 2 the head out, 3 pops out
		{
			var f = k == 0 ? new Frame() : k == 1 ? Pose(eye: 0).Shift(0, 9) : k == 2 ? Pose(eye: 3).Shift(0, 6) : Pose(eye: 2, jaw: 1, arm: 1, farUp: true, blush: true).Shift(0, 2);
			BoxOn(f);
			if (k == 3) Fx(f, "stars", 0);
			return f;
		}
		int[] fBox = Enumerable.Range(0, 4).Select(k => F(Box(k))).ToArray();

		// --- costumes
		void PirateHat(Frame f) => St(f, 6, 3,"....KKKKK....", "..KKKKWKKKK..", "KKKKKKKKKKKKK", ".KK.......KK.");
		var pr1 = Pose(eye: 4, jaw: 1, arm: 1, tail: 1); PirateHat(pr1); pr1.Rect(12, 9, 2, 2, Blk).Dot(11, 8, Blk).Dot(14, 8, Blk).Dot(15, 7, Blk).Dot(16, 7, Blk);
		pr1.Dot(4, 14, Gray).Dot(3, 13, Gray).Dot(3, 12, Gray).Dot(4, 11, Gray); int fPirate1 = F(pr1);
		var pr2 = Pose(eye: 0, arm: 2); PirateHat(pr2); Lean(pr2, -1, 28); pr2.Rect(3, 9, 8, 2, Brown).Rect(2, 8, 2, 4, YelD).Rect(9, 9, 1, 2, YelD); int fPirate2 = F(pr2);
		void CowboyHat(Frame f) => St(f, 5, 3,"....BBBBB....", "....BbBBB....", "B...BBBBB...B", ".BBBBBBBBBBB.", "..bbbbbbbbb..");
		var cb1 = Pose(eye: 2, jaw: 1, arm: 1, tail: 1); CowboyHat(cb1); Oval(cb1, 8f, -3f, 6f, 1.8f, YelD); Line(cb1, 4, 15, 2, -3, YelD); int fCowboy1 = F(cb1);
		var cb2 = Pose(eye: 2, arm: 1, tail: -1, blush: true); CowboyHat(cb2); Oval(cb2, 9f, -2.5f, 6.5f, 1.3f, YelD); Line(cb2, 4, 15, 3, -2, YelD); int fCowboy2 = F(cb2);
		Frame King(int k)
		{
			var f = Pose(eye: k == 0 ? 1 : 2, arm: 2, tail: k == 0 ? 1 : 0, blush: k == 0);
			Cape(f, k);
			for (int y = f.MinY; y < W; y++) for (int x = 0; x < W; x++) { if (f[x, y] == Red) f[x, y] = Purp; else if (f[x, y] == RedL) f[x, y] = PurpL; }
			f.Rect(13, 16, 6, 1, Wt).Dot(14, 16, Blk).Dot(17, 16, Blk);   // ermine collar
			St(f, 9, 3, "Y.Y.Y.Y", "YYHYUYY", "YYYYYYY");
			Line(f, 5, 19, 2, 11, YelD); Circle(f, 2f, 10f, 1.6f, Yel, YelD); f.Dot(2, 9, Red);
			if (k == 1) Star(f, 12, 1, Wt);
			return f;
		}
		int fKing1 = F(King(0)), fKing2 = F(King(1));
		var mw1 = Pose(eye: 1, feet: 1, arm: 2, tail: 1); Hat(mw1, "fedora"); Lean(mw1, 1, 28); int fMoon1 = F(mw1);
		var mw2 = Pose(eye: 1, feet: 2, arm: 3, tail: -1); Hat(mw2, "fedora"); Lean(mw2, 1, 28); mw2.Dot(24, 31, GrayL).Dot(27, 31, GrayL); Star(mw2, 26, 27, Wt); int fMoon2 = F(mw2);
		Frame Brazil(int k)
		{
			var f = Pose(eye: 2, jaw: k == 0 ? 1 : 0, arm: 1, tail: k == 0 ? 1 : -1, blush: true);
			for (int y = -6; y <= 17; y++) f[3, y] = y < 15 ? GrayL : BrownD;
			for (int x = 4; x <= 16; x++)
			{
				int wave = (int)MathF.Round(MathF.Sin((x - 4) * 0.55f - k * 2.1f) * 1.2f * (x - 4) / 12f);
				for (int y = -6; y <= 2; y++)
				{
					float u = (x - 10f) / 6.5f, v = (y + 2f) / 4.6f, cu = x - 10f, cv = y + 2f;
					char c = FlagG;
					if (MathF.Abs(u) + MathF.Abs(v) <= 0.95f) c = Yel;
					if (cu * cu + cv * cv <= 5.3f) c = MathF.Abs(cv + 0.35f * cu) < 0.6f ? Wt : Navy;
					f[x, y + wave] = c;
				}
			}
			return f;
		}
		int[] fFlag = Enumerable.Range(0, 3).Select(k => F(Brazil(k))).ToArray();

		p.IconFrame = fHappy;

		// ============================================================ animations
		p.Spawns.Add((1, 45, "screenW+10", "areaH-imageH", 1));
		p.Spawns.Add((2, 35, "random*(screenW-imageW-50)/100+25", "-imageH-20", 5));
		p.Spawns.Add((3, 20, "0", "areaH-imageH", 2));

		Anim Show(int id, string name, int ms, params (int f, int n)[] steps)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString());
			foreach (var (f, n) in steps) a.Seq(Enumerable.Repeat(f, n).ToArray());
			a.Then(100, 1);
			return a;
		}
		Anim Loop(int id, string name, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString()).Loop(loops, 0);
			a.Seq(frames); a.Then(100, 1);
			return a;
		}
		Anim Lead(int id, string name, int ms, int next, params (int f, int n)[] steps)   // leads into another animation
		{
			var a = Show(id, name, ms, steps); a.Next.Clear(); a.Then(100, next); return a;
		}

		// --- core behaviour (ids the app knows by name)
		p.A(1, "walk").Move("-3", "0").Speed("150").Loop("random/4+8")
			.Seq(fWalk1, fWalk2, fWalk3, fWalk2)
			.Then(50, 1).Then(8, 2).Then(10, 11).Then(5, 20).Then(4, 21).Then(3, 9).Then(3, 22).Then(2, 23)
			.Then(3, 72).Then(3, 74).Then(3, 61).Then(2, 63)
			.Then(8, 16, "taskbar").Then(5, 13, "taskbar")
			.OnBorder(100, 2).OnBorder(20, 12, "vertical").OnBorder(30, 9, "window")
			.OnGravity(100, 5);
		var turn = p.A(2, "turn").Move("0", "0").Speed("130");
		turn.Seq(fBlink, fStand, fStand); turn.Action = "flip"; turn.Then(100, 1);
		p.A(3, "drag").Move("0", "0").Speed("70").Loop("40").Seq(fDrag1, fDrag1, fDrag2, fDrag2).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("45").Loop("30").Seq(fDrag1, fFall, fDrag2, fFall).Then(100, 4);
		p.A(5, "fall").Move("0", "2", "0", "9").Speed("90", "40").Loop("20").Seq(fFall, fDrag2).Then(100, 6).OnBorder(100, 7);
		p.A(6, "fall fast").Move("0", "9").Speed("40").Loop("10").Seq(fFall, fFall, fDrag2).Then(100, 6).OnBorder(100, 8);
		p.A(7, "fall soft").Move("0", "0").Speed("120", "200").Seq(fLand, fLand, fCrouch, fStand, fHappy).Then(100, 1);
		p.A(8, "fall hard").Move("0", "0").Speed("150", "260").Seq(fLand, fDizzy1, fDizzy2, fDizzy1, fDizzy2, fDizzy1, fCrouch, fBlink, fStand).Then(100, 1);
		p.A(9, "jump").Move("-5", "-9", "-5", "9").Speed("70")
			.Seq(fCrouch, fCrouch, fJump, fJump, fJump, fJump, fFall, fFall, fLand, fStand)
			.Then(100, 1)
			.OnBorder(80, 7, "taskbar").OnBorder(40, 7, "window").OnBorder(20, 5, "horizontal+");
		p.A(10, "sync").Move("0", "0").Speed("100", "150").Loop("6").Seq(fHappy, fCheer, fWave1, fCheer).Then(100, 1);
		p.A(11, "blink").Move("0", "0").Speed("450", "120")
			.Seq(fStand, fBlink, fStand, fBlink, fStand)
			.Then(30, 1).Then(5, 15).Then(5, 17).Then(4, 18).Then(4, 19).Then(4, 24)
			.Then(2, 30).Then(2, 31).Then(2, 32).Then(2, 33).Then(2, 34).Then(2, 35).Then(2, 36).Then(2, 37).Then(2, 38).Then(2, 39).Then(2, 40).Then(2, 41)
			.Then(2, 42).Then(2, 43).Then(2, 44).Then(2, 45).Then(2, 46).Then(2, 47).Then(2, 48).Then(2, 49).Then(2, 50).Then(2, 51).Then(2, 52).Then(2, 53)
			.Then(2, 54).Then(2, 55).Then(2, 56).Then(2, 57).Then(2, 58).Then(2, 59).Then(2, 60).Then(2, 61).Then(2, 62).Then(2, 63).Then(2, 64).Then(2, 65)
			.Then(2, 66).Then(2, 67).Then(2, 68).Then(2, 69).Then(2, 70).Then(2, 71).Then(3, 72).Then(2, 73).Then(3, 74).Then(2, 75).Then(2, 76).Then(2, 77)
			.Then(3, 80).Then(3, 82).Then(3, 84).Then(3, 86).Then(3, 88).Then(3, 90).Then(3, 92).Then(3, 94).Then(3, 96);
		p.A(12, "climb").Move("0", "-3").Speed("140").Loop("5000", 2).Seq(fClimb1, fClimb2, fClimb1, fClimb2).Then(100, 12).OnBorder(100, 2);
		p.A(13, "sleep").Move("0", "0").Speed("800", "450").Loop("random/4+5", 2).Seq(fSitBlink, fSleep1, fSleep1, fSleep2, fSleep3, fSleep2).Then(100, 14);
		p.A(14, "wake up").Move("0", "0").Speed("220", "160").Seq(fSleep1, fSitBlink, fSit, fYawn, fYawn, fStand, fHappy).Then(100, 1);
		Show(15, "look around", 260, (fStand, 1), (fLookUp, 2), (fStand, 1), (fTailUp, 1), (fTailDown, 1), (fStand, 1));
		p.A(16, "sit").Move("0", "0").Speed("600", "400").Loop("random/3+4", 1).Seq(fSit, fSit, fSitBlink, fSit).Then(60, 1).Then(40, 13);
		Loop(17, "wave", 170, "3", fStand, fWave1, fWave2, fWave1, fWave2);
		Loop(18, "tail wag", 140, "random/3+4", fTailUp, fHappy, fTailDown, fHappy);
		Show(19, "yawn", 220, (fStand, 1), (fYawn, 5), (fBlink, 2), (fStand, 1));
		// 20 roar (child: sound waves fly forward)
		Lead(20, "roar", 140, 81, (fStand, 1), (fRoarPrep, 3));
		Show(81, "ROAR", 120, (fRoar1, 2), (fRoar2, 6), (fRoar1, 2), (fHappy, 3));
		p.A(101, "roar waves").Move("-6", "0").Speed("80").Loop("8", 0).Fade(1, 0).Seq(fWaves1, fWaves2);
		p.Childs.Add((81, "imageX", "imageY", 101));
		p.A(21, "run").Move("-7", "0").Speed("90").Loop("random/3+10").Seq(fRun1, fRun2).Then(70, 1).Then(30, 21).OnBorder(100, 2).OnBorder(40, 9, "window").OnGravity(100, 5);
		Loop(22, "stomp", 150, "random/3+3", fStomp1, fStomp2);
		Loop(23, "cheer", 140, "random/3+3", fCheer, fHappy);
		Loop(24, "think", 300, "2", fLookUp, fStand, fLookUp, fBlink);

		// --- feelings (sheet 4)
		Show(30, "eat meat", 200, (fEat1, 2), (fEat2, 2), (fEat1, 2), (fEat2, 3), (fHappy, 2));
		Loop(31, "shocked", 100, "random/3+4", fShock1, fShock2);
		Loop(32, "cry", 220, "random/3+4", fCry1, fCry2);
		Show(33, "thumbs up", 200, (fStand, 1), (fThumb, 6), (fHappy, 2));
		// 34 angry (child: smoke puff rises)
		Lead(34, "angry", 140, 83, (fAngry1, 3), (fAngry2, 2));
		Show(83, "fuming", 140, (fAngry2, 2), (fAngry1, 3), (fAngry2, 2), (fAngry1, 3), (fBlink, 1), (fStand, 1));
		p.A(103, "smoke").Move("1", "-2").Speed("110").Loop("8", 0).Fade(1, 0).Seq(fSmoke1, fSmoke2);
		p.Childs.Add((83, "imageX", "imageY", 103));
		Loop(35, "cool", 300, "random/3+3", fCool1, fCool2);
		// 36 sneeze (child: puff drifts forward)
		Lead(36, "sneeze", 150, 85, (fStand, 1), (fAh, 3), (fStand, 1), (fAh, 4));
		Show(85, "achoo", 140, (fChoo, 4), (fBlink, 2), (fHappy, 2));
		p.A(105, "sneeze puff").Move("-3", "0", "-1", "-1").Speed("100").Loop("8", 0).Fade(0.9, 0).Seq(fPuff);
		p.Childs.Add((85, "imageX", "imageY", 105));
		// 37 dance (child: notes)
		Lead(37, "dance", 150, 87, (fDance1, 1), (fDance2, 1));
		var dance = p.A(87, "dancing").Move("0", "0").Speed("150").Loop("random/3+5", 0); dance.Seq(fDance1, fDance2); dance.Then(100, 1);
		p.A(107, "note").Move("-1", "-2", "1", "-2").Speed("100").Loop("10", 0).Fade(1, 0).Seq(fNoteUp);
		p.Childs.Add((87, "imageX", "imageY", 107));
		// 38 sniff with a magnifier (child: an ant runs away)
		Lead(38, "sniff", 220, 89, (fSniff1, 4), (fSniff2, 3), (fSniff1, 2), (fSniff2, 3));
		Show(89, "ant!", 140, (fShock1, 3), (fLookUp, 2), (fHappy, 3));
		p.A(109, "ant runs").Move("-4", "0").Speed("70").Loop("80", 0).Seq(fAnt1, fAnt2).Then(100, 109);
		p.Childs.Add((89, "imageX", "imageY", 109));
		Loop(39, "party", 130, "random/3+5", fParty1, fParty2);
		// 40 love (child: heart bubble floats up)
		Lead(40, "love", 200, 91, (fStand, 1));
		Show(91, "in love", 220, (fLove, 6), (fHappy, 3));
		p.A(111, "love bubble").Move("0", "-2").Speed("110").Loop("10", 0).Fade(1, 0).Seq(fLoveBubble);
		p.Childs.Add((91, "imageX", "imageY", 111));
		Loop(41, "tired", 260, "random/3+3", fTired1, fTired2);

		// --- everyday things (sheet 5)
		Loop(42, "read", 420, "random/3+3", fRead1, fRead1, fRead2, fRead1);
		Show(43, "drink", 260, (fDrink1, 3), (fDrink2, 4), (fDrink1, 2), (fDrink2, 3), (fHappy, 2));
		Show(44, "pizza", 200, (fPizza1, 2), (fPizza2, 2), (fPizza1, 2), (fPizza2, 3), (fHappy, 2));
		Loop(45, "cold", 70, "random/3+12", fCold1, fCold2);
		Loop(46, "hot", 240, "random/3+4", fHot1, fHot2);
		Loop(47, "computer", 140, "random/3+6", fPc1, fPc2, fPc1, fPc2, fPc3);
		Loop(48, "boxing", 140, "random/3+5", fBox1, fBox2);
		// 49 balloon (child: let go, it floats away)
		Lead(49, "balloon", 260, 93, (fBalloon1, 3), (fBalloon2, 3), (fBalloon1, 3), (fBalloon2, 3));
		Show(93, "let go", 170, (fWide, 3), (fLookUp, 6), (fWave1, 2), (fWave2, 2), (fHappy, 2));
		p.A(113, "balloon away").Move("-1", "-3", "1", "-4").Speed("70").Loop("100", 0).Seq(fBalAway1, fBalAway1, fBalAway2, fBalAway2).Then(100, 113);
		p.Childs.Add((93, "imageX", "imageY", 113));
		Loop(50, "detective", 300, "random/3+3", fDetective1, fDetective2);
		Loop(51, "popcorn", 260, "random/3+4", fPop1, fPop1, fPop2);
		// 52 superhero: stands, takes off, flies across the screen, lands
		Lead(52, "superhero", 150, 53, (fHeroStand, 6), (fCrouch, 2));
		p.A(53, "hero takeoff").Move("-3", "-6").Speed("80").Loop("random/4+5", 0).Seq(fHeroFly1, fHeroFly2)
			.Then(100, 54).OnBorder(100, 54, "horizontal").OnBorder(100, 2, "vertical");
		p.A(54, "hero flight").Move("-8", "0").Speed("70").Loop("random/4+10", 0).Seq(fHeroFly1, fHeroFly2)
			.Then(100, 55).OnBorder(100, 55, "vertical");
		p.A(55, "hero landing").Move("-3", "5").Speed("80").Loop("300", 0).Seq(fHeroFly1, fHeroFly2)
			.Then(100, 55).OnBorder(100, 7, "taskbar").OnBorder(100, 7, "window").OnBorder(100, 2, "vertical");
		Loop(56, "headphones", 200, "random/3+5", fMusic1, fMusic2);

		// --- hobbies and costumes (sheet 6)
		Show(57, "fishing", 280, (fFish1, 6), (fFish2, 3), (fFish1, 3), (fFish2, 4), (fFish3, 5), (fHappy, 2));
		Show(58, "photo", 200, (fCamera1, 4), (fCamera2, 2), (fCamera1, 3), (fCamera2, 2), (fHappy, 2));
		Loop(59, "paint", 320, "random/3+3", fPaint1, fPaint1, fPaint2);
		Show(60, "science", 240, (fScience1, 4), (fScience2, 3), (fScience1, 2), (fScience2, 3), (fHappy, 2));
		Show(61, "wizard", 200, (fWizard1, 4), (fWizard2, 3), (fWizard1, 2), (fWizard2, 3), (fHappy, 1));
		var spy = p.A(62, "spy").Move("-1", "0").Speed("240").Loop("random/3+4", 0); spy.Seq(fSpy1, fSpy2); spy.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		Lead(63, "guitar", 180, 95, (fGuitar1, 1));
		var gt = p.A(95, "play guitar").Move("0", "0").Speed("180").Loop("random/3+5", 0); gt.Seq(fGuitar1, fGuitar2); gt.Then(100, 1);
		p.Childs.Add((95, "imageX", "imageY", 107));
		// 64 watering (child: a flower grows there, stays a while, then wilts)
		Lead(64, "water plant", 240, 97, (fWater1, 3), (fWater2, 5));
		Show(97, "flower!", 200, (fHappy, 4), (fWave1, 2), (fWave2, 2), (fHappy, 2));
		var fl = p.A(117, "flower").Move("0", "0").Speed("300").Seq(fFlower1, fFlower2, fFlower3, fFlower4);
		fl.Seq(Enumerable.Repeat(fFlower4, 60).ToArray()); fl.Seq(fWilt, fWilt, fWilt, fFlower2);
		p.Childs.Add((97, "imageX", "imageY", 117));
		// 65 swim (child: bubbles)
		Lead(65, "swim", 200, 98, (fSwim1, 1));
		var sw = p.A(98, "swimming").Move("0", "0").Speed("200").Loop("random/3+4", 0); sw.Seq(fSwim1, fSwim2); sw.Then(100, 1);
		p.A(118, "bubbles").Move("0", "-2").Speed("110").Loop("10", 0).Fade(1, 0).Seq(fBub1, fBub2);
		p.Childs.Add((98, "imageX", "imageY", 118));
		// 66 basketball (child: the shot flies in an arc)
		Lead(66, "basketball", 160, 79, (fBall1, 3), (fCrouch, 2));
		Show(79, "shoot", 130, (fBall2, 1), (fCheer, 3), (fLookUp, 6), (fCheer, 3), (fHappy, 2));
		p.A(119, "ball arc").Move("-4", "-9", "-4", "9").Speed("60").Loop("1", 0).Seq(Enumerable.Repeat(fHoop1, 8).Concat(Enumerable.Repeat(fHoop2, 8)).ToArray())
			.Then(100, 120).OnBorder(100, 120, "taskbar").OnBorder(100, 120, "window");
		p.A(120, "ball bounce").Move("-3", "-4", "-3", "4").Speed("70").Loop("6", 0).Seq(fHoop1, fHoop2, fHoop1, fHoop2).Then(100, 120);
		p.Childs.Add((79, "imageX", "imageY", 119));
		Show(67, "birthday", 220, (fCake1, 3), (fCake2, 2), (fCake1, 2), (fCake2, 3), (fParty1, 2), (fParty2, 2));

		// --- jobs (sheet 7)
		var astro = p.A(68, "astronaut").Move("0", "0").Speed("300").Loop("random/3+4", 0); astro.Seq(fAstro1, fAstro2); astro.Then(100, 1);
		Show(69, "chef", 200, (fChef1, 3), (fChef2, 2), (fChef1, 3), (fChef2, 2), (fHappy, 2));
		// 70 soccer (child: the ball rolls away)
		Lead(70, "soccer", 180, 100, (fSoccer1, 4));
		Show(100, "kick", 130, (fSoccer2, 3), (fLookUp, 2), (fCheer, 3), (fHappy, 2));
		p.A(121, "ball roll").Move("-6", "0", "-3", "0").Speed("70").Loop("40", 0).Seq(rollBall).Then(100, 121);
		p.Childs.Add((100, "imageX", "imageY", 121));
		Loop(71, "police", 260, "random/3+3", fPolice1, fPolice2);
		// 72 notes, then a paper airplane (child)
		Lead(72, "write", 300, 73, (fWrite1, 3), (fWrite2, 2), (fWrite1, 3), (fWrite2, 2), (fPlaneHold, 4));
		Show(73, "paper plane", 140, (fPlaneThrow, 3), (fLookUp, 4), (fHappy, 2));
		p.A(122, "plane flight").Move("-6", "-2", "-6", "1").Speed("60").Loop("30", 0).Seq(fPlane1, fPlane1, fPlane2, fPlane2).Then(100, 122);
		p.Childs.Add((73, "imageX", "imageY", 122));
		Lead(74, "drums", 120, 75, (fDrums1, 1));
		var drums = p.A(75, "drumming").Move("0", "0").Speed("120").Loop("random/3+6", 0); drums.Seq(fDrums1, fDrums2); drums.Then(100, 1);
		p.Childs.Add((75, "imageX", "imageY", 107));
		var explore = p.A(76, "explorer").Move("-1", "0").Speed("260").Loop("random/3+4", 0); explore.Seq(fExplore1, fExplore2); explore.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		Show(77, "builder", 150, (fBuild1, 2), (fBuild2, 2), (fBuild1, 2), (fBuild2, 2), (fBuild1, 2), (fBuild2, 2), (fHappy, 2));
		Loop(80, "juggle", 45, "random/3+3", fJuggle);
		Lead(82, "sing", 220, 84, (fSing1, 1));
		var sing = p.A(84, "singing").Move("0", "0").Speed("220").Loop("random/3+4", 0); sing.Seq(fSing1, fSing2); sing.Then(100, 1);
		p.Childs.Add((84, "imageX", "imageY", 107));
		Show(86, "phone", 240, (fPhone1, 4), (fPhone2, 3), (fPhone1, 3), (fPhone2, 3), (fHappy, 1));
		Loop(88, "tail chase", 120, "random/3+3", fTailUp, fTailDown, fCheer, fTailDown);
		Show(90, "big roar", 120, (fStomp1, 2), (fStomp2, 2), (fStomp1, 2), (fRoar2, 6), (fHappy, 2));
		Loop(92, "hop", 120, "random/3+3", fCrouch, fJump, fJump, fCrouch);
		Show(94, "stretch", 220, (fStand, 1), (fCheer, 5), (fYawn, 3), (fBlink, 1), (fStand, 1));
		Show(96, "wink", 220, (fStand, 2), (fBlink, 1), (fHappy, 4));

		// ============================================================ 40 more scenes: animations
		// ids: 130..169 the scenes (picked at random), 200..219 their second parts, 230..259 the child windows
		// 130 meteor: Rex points at the sky; a meteor (child) comes from behind him, high up, flies down across the screen
		// leaving embers (a child of the child every few steps) and crashes on the taskbar or a window: the impact opens
		// flying rocks to both sides and a smoke cloud (more children), and leaves a smoking crater that fades.
		Lead(130, "meteor!", 160, 216, (fStand, 1), (fLookUp, 3), (fPointUp, 3));
		Lead(216, "meteor watch", 150, 217, (fPointLit, 4), (fShockLit1, 1), (fShockLit2, 1), (fShockLit1, 1), (fShockLit2, 1), (fShockLit1, 1), (fShockLit2, 1),
			(fShockLit1, 1), (fShockLit2, 1), (fShock1, 1), (fShock2, 1), (fDuck, 8));
		Show(217, "phew", 200, (fPhew1, 3), (fPhew2, 3), (fBlink, 1), (fHappy, 2));
		p.Childs.Add((216, "imageX+imageW*3", "imageY/4", 250));
		p.A(250, "meteor").Move("-8", "7").Speed("45").Loop("1", 0).Seq(fMeteor).Then(100, 251)
			.OnBorder(100, 253, "taskbar").OnBorder(100, 253, "window");
		p.A(251, "meteor trail").Move("-8", "7").Speed("45").Seq(fMeteor).Then(100, 251)
			.OnBorder(100, 253, "taskbar").OnBorder(100, 253, "window");
		p.Childs.Add((251, "imageX", "imageY", 252));
		p.A(252, "embers").Move("1", "1").Speed("70").Fade(1, 0).Seq(fEmbers[0], fEmbers[0], fEmbers[1], fEmbers[1], fEmbers[2], fEmbers[2], fEmbers[2]);
		p.A(253, "impact").Move("0", "0").Speed("60", "120")
			.Seq(fBoom[0], fBoom[0], fBoom[1], fBoom[1], fBoom[2], fBoom[2], fBoom[3], fBoom[3], fBoom[3], fBoom[4], fBoom[4], fBoom[4]).Then(100, 254);
		p.A(254, "crater").Move("0", "0").Speed("170").Fade(1, 0).Loop("9", 0).Seq(fCrater);
		p.A(255, "debris").Move("-4", "-7", "-3", "7").Speed("50").Loop("6", 0).Fade(1, 0.3).Seq(fDebris[0], fDebris[1]);
		p.A(256, "debris 2").Move("4", "-8", "3", "6").Speed("50").Loop("6", 0).Fade(1, 0.3).Seq(fDebris[1], fDebris[0]);
		p.A(257, "smoke cloud").Move("1", "-2").Speed("130").Loop("7", 0).Fade(0.9, 0).Seq(fSmokeCloud);
		foreach (int c in new[] { 255, 256, 257 }) p.Childs.Add((253, "imageX", "imageY", c));

		// 133 shooting star (child) and a wish
		Show(133, "shooting star", 200, (fStand, 1), (fLookUp, 4), (fWide, 2), (fWish1, 3), (fWish2, 3), (fWish1, 2), (fHappy, 2));
		p.A(248, "star streak").Move("-12", "4").Speed("40").Loop("40", 0).Seq(fStreak);
		p.Childs.Add((133, "imageX+imageW*2", "imageY/3", 248));
		// 134 fireworks: the rocket (child) goes up and bursts; the burst opens a second one (child of the child)
		Lead(134, "fireworks", 220, 200, (fRocket1, 4), (fRocket2, 3));
		Show(200, "launch", 160, (fStand, 1), (fLookUp, 4), (fCheer, 3), (fParty1, 2), (fParty2, 2), (fLookUp, 3), (fHappy, 2));
		p.Childs.Add((200, "imageX-imageW*10/32", "imageY+imageH*7/38", 230));
		p.A(230, "rocket").Move("0", "-10").Speed("50").Loop("10", 0).Seq(fRocketFly).Then(100, 231).OnBorder(100, 231, "horizontal");
		p.A(231, "burst").Move("0", "0").Speed("90").Seq(fBurst[0], fBurst[1], fBurst[2], fBurst[2], fBurst[3], fBurst[3]).Then(100, 232);
		p.A(232, "sparkles").Move("0", "2").Speed("110").Loop("5", 0).Fade(0.9, 0).Seq(fTwinkle);
		p.A(258, "burst 2").Move("0", "0").Speed("90").Seq(fBlank, fBlank, fBlank, fBlank, fBurst2[0], fBurst2[1], fBurst2[2], fBurst2[2], fBurst2[3], fBurst2[3]).Then(100, 232);
		p.Childs.Add((231, "imageX-imageW/3", "imageY-imageH/4", 258));
		// 135 fire breath (child: the flame)
		Lead(135, "fire breath", 140, 201, (fStand, 1), (fRoarPrep, 4));
		Show(201, "flame", 110, (fFire1, 2), (fFire2, 2), (fFire1, 2), (fFire2, 2), (fFire1, 2), (fRoar1, 2), (fHappy, 3));
		p.A(233, "flame burst").Move("-5", "0").Speed("70").Loop("5", 0).Fade(1, 0).Seq(fFlameB);
		p.Childs.Add((201, "imageX-imageW*25/32", "imageY", 233));
		// 136 snowball (child: it flies in an arc, falls and splats on the floor)
		Lead(136, "snowball", 220, 202, (fSnow1, 4), (fSnow2, 3));
		Show(202, "throw snowball", 140, (fSnow3, 3), (fLookUp, 3), (fCheer, 3), (fHappy, 2));
		p.Childs.Add((202, "imageX", "imageY-imageH*16/38", 234));
		p.A(234, "snowball").Move("-6", "-6", "-6", "8").Speed("50").Loop("20", 0).Seq(fSnowball).Then(100, 259)
			.OnBorder(100, 235, "taskbar").OnBorder(100, 235, "window");
		p.A(259, "snowball falls").Move("-4", "8").Speed("50").Loop("40", 0).Seq(fSnowball)
			.OnBorder(100, 235, "taskbar").OnBorder(100, 235, "window");
		p.A(235, "splat").Move("0", "0").Speed("140").Fade(1, 0).Seq(fSplat[0], fSplat[1], fSplat[1], fSplat[1], fSplat[1]);
		// 137 ninja (child: a throwing star)
		Lead(137, "ninja", 200, 203, (fNinja1, 4), (fBlink, 1), (fNinja1, 3));
		Show(203, "throw star", 120, (fNinja2, 3), (fNinja1, 4), (fHappy, 2));
		p.A(236, "shuriken").Move("-9", "0").Speed("40").Loop("50", 0).Seq(fShuriken);
		p.Childs.Add((203, "imageX", "imageY", 236));
		// 138 graduation (child: the cap)
		Lead(138, "graduation", 220, 204, (fGrad1, 6), (fCrouch, 2));
		Show(204, "cap toss", 120, (fGrad2, 6), (fLookUp, 4), (fGrad1, 4), (fHappy, 2));
		p.A(237, "cap").Move("-1", "-8", "-1", "8").Speed("60").Loop("11", 0).Seq(fCap);
		p.Childs.Add((204, "imageX", "imageY", 237));
		// 139 boomerang (child: out and back)
		Lead(139, "boomerang", 200, 205, (fBoomHold, 4));
		Show(205, "boomerang throw", 150, (fPlaneThrow, 2), (fStand, 2), (fLookUp, 9), (fWide, 2), (fBoomCatch, 3), (fHappy, 2));
		p.A(238, "boomerang out").Move("-12", "-2", "-2", "1").Speed("50").Loop("5", 0).Seq(fBoomer).Then(100, 239);
		p.A(239, "boomerang back").Move("2", "-1", "12", "2").Speed("50").Loop("5", 0).Seq(fBoomer);
		p.Childs.Add((205, "imageX", "imageY", 238));
		// 140 kite (child: it gets loose)
		Lead(140, "kite", 230, 206, (fKite1, 2), (fKite2, 2), (fKite1, 2), (fKite2, 2), (fKite1, 2), (fKite2, 2));
		Show(206, "kite away", 170, (fWide, 3), (fShock1, 2), (fLookUp, 4), (fWave1, 2), (fWave2, 2), (fBlink, 1), (fStand, 1));
		p.A(240, "kite flies").Move("-2", "-5").Speed("90").Loop("40", 0).Seq(fKiteAway);
		p.Childs.Add((206, "imageX", "imageY", 240));
		// 141 soap bubbles (child, three times)
		Lead(141, "soap bubbles", 200, 207, (fBlow1, 3), (fBlow2, 2));
		Lead(207, "more bubbles", 200, 208, (fBlow1, 3), (fBlow2, 2));
		Show(208, "last bubbles", 200, (fBlow1, 3), (fBlow2, 2), (fHappy, 3));
		p.A(241, "bubbles float").Move("-2", "-2", "-1", "-2").Speed("100").Loop("12", 0).Fade(1, 0).Seq(fSoap);
		foreach (int b in new[] { 141, 207, 208 }) p.Childs.Add((b, "imageX", "imageY", 241));
		// 142 egg (child: the baby dino walks off)
		Lead(142, "egg", 220, 209, (fEgg1, 4), (fEgg2, 1), (fEgg2b, 1), (fEgg2, 1), (fEgg2b, 1), (fEgg3, 3), (fEgg4, 3));
		Show(209, "baby dino", 220, (fEggLove, 4), (fHappy, 2), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fHappy, 2));
		p.A(242, "baby walks").Move("-2", "0").Speed("150").Loop("30", 0).Seq(fBaby);
		p.Childs.Add((209, "imageX", "imageY", 242));
		// 143 rain cloud (child) over his head
		Lead(143, "rain cloud", 200, 210, (fLookUp, 3), (fWide, 2));
		Lead(210, "wet", 200, 219, (fWet1, 2), (fWet2, 2), (fWet1, 2), (fWet2, 2), (fWet1, 2), (fWet2, 2));
		Loop(219, "shake off", 70, "5", fShake1, fShake2);
		p.A(243, "rain cloud").Move("0", "0").Speed("120").Loop("11", 0).Seq(fCloud[0], fCloud[1], fCloud[2]).Then(100, 244);
		p.A(244, "cloud leaves").Move("-3", "-1").Speed("90").Loop("20", 0).Fade(1, 0).Seq(fCloud[3]);
		p.Childs.Add((143, "imageX", "imageY", 243));
		// 144 bird friend (child: it flies away)
		Lead(144, "bird friend", 220, 211, (fBird1, 4), (fBird2, 3), (fBird1, 2), (fBird3, 4), (fBird2, 2), (fBird3, 2));
		Show(211, "bird flies", 170, (fLookUp, 3), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fHappy, 2));
		p.A(245, "bird away").Move("-3", "-3", "-4", "-1").Speed("80").Loop("30", 0).Seq(fBirdFly);
		p.Childs.Add((211, "imageX", "imageY", 245));
		// 145 butterfly net (child: it escapes)
		Lead(145, "butterfly", 220, 212, (fNet1, 2), (fNet1b, 2), (fNet1, 2), (fNet1b, 2));
		Show(212, "missed", 160, (fNet2, 3), (fWide, 3), (fLookUp, 3), (fHappy, 3));
		p.A(246, "butterfly away").Move("-2", "-3", "-3", "1").Speed("110").Loop("30", 0).Fade(1, 0.3).Seq(fBfly);
		p.Childs.Add((212, "imageX", "imageY", 246));
		// 146 gift (child: the balloon floats away)
		Lead(146, "gift", 220, 213, (fGift1, 4), (fGift2, 1), (fGift2b, 1), (fGift2, 1), (fGift2b, 1), (fGift3, 2));
		Show(213, "surprise", 180, (fGift4, 4), (fLookUp, 4), (fCheer, 3), (fHappy, 2));
		p.Childs.Add((213, "imageX", "imageY", 113));
		// 147.. scenes on the spot
		Show(147, "bubble gum", 180, (fGum[0], 2), (fGum[1], 2), (fGum[0], 2), (fGum[1], 2), (fGum[2], 2), (fGum[3], 2), (fGum[4], 2), (fGum[5], 3), (fGum[6], 3), (fBlink, 2), (fHappy, 2));
		Show(148, "ice cream", 220, (fIce1, 3), (fIce2, 2), (fIce1, 2), (fIce2, 2), (fIce3, 3), (fHappy, 2));
		Lead(149, "coffee", 220, 218, (fCoffee1, 2), (fCoffee1b, 2), (fCoffee2, 3), (fCoffee1, 2), (fCoffee2, 3), (fWide, 2));
		Loop(218, "hyper", 70, "8", fHyper1, fHyper2);
		Loop(150, "yo-yo", 140, "random/3+4", fYo[0], fYo[1], fYo[2], fYo[1]);
		p.A(151, "skateboard").Move("-6", "0").Speed("100").Loop("random/3+10", 0).Seq(fSkate1, fSkate1, fSkate2, fSkate2)
			.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		Loop(152, "jump rope", 110, "random/3+5", fRope[0], fRope[1], fRope[2], fRope[1]);
		Loop(153, "weights", 260, "random/3+3", fLift0, fLift0, fLift1, fLift1);
		Loop(154, "meditate", 320, "random/3+3", fZen);
		Loop(155, "umbrella", 160, "random/3+5", fUmb);
		Show(156, "karate", 150, (fKarate1, 3), (fKarate2, 2), (fKarate1, 2), (fKarate3, 3), (fKarate1, 2), (fKarate2, 2), (fHappy, 2));
		// 157 hiccups (child: a little bubble each time)
		Lead(157, "hiccup", 160, 214, (fHic, 2), (fStand, 3), (fBlink, 1));
		Lead(214, "hiccup 2", 160, 215, (fHic, 2), (fStand, 3), (fBlink, 1));
		Show(215, "hiccup 3", 160, (fHic, 2), (fStand, 2), (fWide, 2), (fHappy, 3));
		p.A(247, "hic bubble").Move("-1", "-2").Speed("100").Loop("4", 0).Fade(1, 0).Seq(fHicBub);
		foreach (int h in new[] { 157, 214, 215 }) p.Childs.Add((h, "imageX", "imageY", 247));
		Show(158, "laugh", 140, (fLaugh1, 2), (fLaugh2, 2), (fLaugh1, 2), (fLaugh2, 2), (fLaugh1, 2), (fOnBack1, 2), (fOnBack2, 2), (fOnBack1, 2), (fOnBack2, 2), (fCrouch, 1), (fHappy, 2));
		Show(159, "selfie", 220, (fSelfie[0], 3), (fSelfie[1], 2), (fSelfie[2], 1), (fSelfie[0], 2), (fSelfie[2], 1), (fHappy, 2));
		Loop(160, "teddy hug", 280, "random/3+3", fTeddy1, fTeddy1, fTeddy2, fTeddy2);
		Show(161, "watermelon", 200, (fMelon1, 2), (fMelon2, 2), (fMelon1, 2), (fMelon3, 3), (fHappy, 2));
		Loop(162, "hula hoop", 110, "random/3+6", fHula[0], fHula[1], fHula[2], fHula[1]);
		Loop(163, "pirate", 300, "random/3+3", fPirate1, fPirate1, fPirate2, fPirate2);
		Loop(164, "cowboy", 160, "random/3+5", fCowboy1, fCowboy2);
		Loop(165, "king", 320, "random/3+3", fKing1, fKing2);
		p.A(166, "moonwalk").Move("3", "0").Speed("160").Loop("random/3+6", 0).Seq(fMoon1, fMoon2).Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		Show(167, "idea", 200, (fLookUp, 3), (fIdea[0], 2), (fLookUp, 2), (fIdea[1], 2), (fIdea[2], 3), (fIdea[3], 2), (fIdea[2], 2), (fHappy, 2));
		Show(168, "hide in box", 220, (fBox[0], 4), (fBox[1], 3), (fBox[0], 3), (fBox[1], 2), (fBox[2], 2), (fBox[3], 3), (fHappy, 2));
		Loop(169, "brazil flag", 180, "random/3+5", fFlag);

		// picked at random like the others (from "blink"), the meteor a bit more often; the skateboard and the moonwalk also from "walk"
		var blinkAnim = p.Anims.First(a => a.Id == 11);
		for (int id = 130; id <= 169; id++) blinkAnim.Then(id == 130 ? 3 : 2, id);
		var walkAnim = p.Anims.First(a => a.Id == 1);
		walkAnim.Next.Insert(walkAnim.Next.FindIndex(n => n.only == "taskbar"), (2, "none", 151));
		walkAnim.Next.Insert(walkAnim.Next.FindIndex(n => n.only == "taskbar"), (2, "none", 166));

		// 99 kill: waves goodbye and fades
		p.A(99, "kill").Move("0", "0").Speed("130", "220").Fade(1, 0).Seq(fWave1, fWave2, fWave1, fBlink, fBlink).Then(100, 99);

		p.Readme = """
			# Rex the Dino

			## Description

			A little green pixel dinosaur with orange spikes, a big head and tiny arms.

			He stomps along your taskbar, roars (the sound waves fly off), plays soccer and
			basketball, dances, sings, plays guitar and drums, reads, paints, fishes, swims,
			dresses up as a detective, a wizard, a spy, a scientist, a chef, a police officer,
			an explorer, a builder, an astronaut and a superhero who flies across your screen.

			## About pet

			Author: Luan Felipe

			## Animations

			* walk, run, turn, jump onto windows, fall (soft landing, hard landing knocked over)
			* climb the screen border, sit, sleep curled up, wake up, yawn, stretch
			* blink, look around, wave, tail wag, stomp, roar, cheer, hop, wink
			* eat meat, pizza, popcorn, drink, birthday cupcake
			* shocked, crying, thumbs up, angry (smoke), cool sunglasses, sneeze, dance, love, tired
			* read, computer, phone, boxing, balloon, headphones, fishing, photo, painting, guitar,
			  drums, singing, juggling, watering a flower, swimming, basketball, soccer, writing
			* costumes: detective, wizard, spy, scientist, chef, police, explorer, builder,
			  astronaut, superhero
			* separate little windows: roar waves, smoke, sneeze puff, music notes, an ant,
			  a heart bubble, a balloon, a basketball, a soccer ball, a paper plane, a flower,
			  bubbles
			* a meteor that crosses the screen behind him, leaves glowing embers and crashes on
			  the taskbar or a window (flash, fireball, mushroom cloud, flying rocks, a smoking
			  crater) while Rex points, ducks and wipes his forehead
			* more scenes: shooting star and a wish, fireworks (two bursts), fire breath,
			  snowball, ninja star, graduation cap toss, boomerang, kite, soap bubbles, an egg
			  that hatches a baby dino, a rain cloud only over him, a bird on his head, butterfly
			  net, a gift with a balloon, bubble gum, ice cream, coffee, watermelon, yo-yo,
			  skateboard, jump rope, weights, meditation, umbrella, karate, hiccups, laughing,
			  selfie, teddy bear, hula hoop, pirate, cowboy, king, moonwalk, a bright idea,
			  hiding in a box and the Brazilian flag
			""";
		p.ReadmePt = """
			# Rex, o Dino

			## Descrição

			Um dinossaurinho verde de pixel art com espinhos laranja, cabeção e bracinhos.

			Ele anda pisando forte pela barra de tarefas, ruge (as ondas do rugido saem
			voando), joga futebol e basquete, dança, canta, toca violão e bateria, lê, pinta,
			pesca, nada e se fantasia de detetive, mago, espião, cientista, chef, policial,
			explorador, pedreiro, astronauta e de super-herói que atravessa a tela voando.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* andar, correr, virar, pular sobre janelas, cair (pouso suave / cai de costas tonto)
			* escalar as bordas da tela, sentar, dormir enrolado, acordar, bocejar, espreguiçar
			* piscar, olhar ao redor, dar tchau, abanar o rabo, pisar forte, rugir, comemorar
			* comer carne, pizza, pipoca, beber, cupcake de aniversário
			* susto, choro, joinha, bravo (fumaça), óculos escuros, espirro, dança, apaixonado, cansado
			* ler, computador, celular, boxe, balão, fones, pesca, foto, pintura, violão,
			  bateria, cantar, malabares, regar uma flor, nadar, basquete, futebol, escrever
			* fantasias: detetive, mago, espião, cientista, chef, policial, explorador,
			  pedreiro, astronauta, super-herói
			* janelinhas independentes: ondas do rugido, fumaça, espirro, notas musicais,
			  formiga, coração, balão, bola de basquete, bola de futebol, avião de papel,
			  flor, bolhas
			* um meteoro que atravessa a tela atrás dele, deixa brasas no caminho e cai na
			  barra de tarefas ou numa janela (clarão, bola de fogo, cogumelo de fumaça, pedras
			  voando, cratera fumegante) enquanto o Rex aponta, se abaixa e enxuga a testa
			* mais cenas: estrela cadente e um pedido, fogos de artifício (duas explosões),
			  bafo de fogo, bola de neve, estrela ninja, chapéu de formatura jogado para cima,
			  bumerangue, pipa, bolhas de sabão, um ovo que choca um filhote, uma nuvem de
			  chuva só em cima dele, um passarinho na cabeça, rede de borboleta, presente com
			  balão, chiclete, sorvete, café, melancia, ioiô, skate, pular corda, halteres,
			  meditação, guarda-chuva, caratê, soluço, gargalhada, selfie, ursinho, bambolê,
			  pirata, caubói, rei, moonwalk, uma ideia brilhante, esconder na caixa e a
			  bandeira do Brasil
			""";

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
