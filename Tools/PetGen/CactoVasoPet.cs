using System.Drawing;

namespace PetGen;

/// <summary>
/// "Xiquinho" — a little cactus in a clay pot, in pixel art: three arms (a tall middle one and two side arms), a face on
/// the trunk, light spines along his sides, a terracotta pot and two little brown feet under it.
/// The body is put together from parts every time: the cactus is drawn filled (trunk, arms in a pose, face), then it can
/// lean (the top sways), squash or stretch (rows go away or repeat), and only then gets its outline and its spines, so
/// every pose stays clean. The pot and the feet are drawn after it.
/// He never climbs the screen borders: at a border he turns around. He walks with small steps on his little feet (the
/// pot bobs, the cactus sways on top), eases into and out of walking, and squashes and stretches when he jumps and lands.
/// Many scenes use child windows: a watering can that waters him (and a flower blooms on his head), a bee that visits the
/// flower, a balloon that pops on his spines, a heart balloon he hugs, spines that fly off when he sneezes, a tumbleweed
/// that bounces over him, a calango (lizard) that does push-ups with him, a bird that lands on him and gets pricked, a
/// butterfly that rests on his arm, a baby cactus, soap bubbles, flower rain, the sun, a rain cloud that makes him grow...
/// Like every DesktopPet pet he faces left. Frames are 36 px wide with 8 rows of headroom; the feet stand on row 35.
/// A child placed with "imageX-imageW*n" appears in front of him; when he faces right the program mirrors the position
/// and the movement, so the scenes work on both sides. Children that touch him are placed exactly over him
/// ("imageX", "imageY") and drawn in his own coordinates.
/// </summary>
static class CactoVasoPet
{
	const int W = 36, Head = 8;
	const int AX = 7, AY = 14;   // where the reference art (22 x 22) sits: its row 21 (the feet) is the frame's row 35
	const int Ground = AY + 21;

	/// <summary>One pose of Xiquinho.</summary>
	sealed class L
	{
		// eyes: 0 open, 1 closed, 2 happy ^ ^, 3 wide, 4 half (tired), 5 angry, 6 teary, 7/8 dizzy (spinning), 9 hearts,
		//       10 sunglasses, 11 wink, 12 squint > <, -1 none (seen from behind)
		// mouth: 0 small, 1 open, 2 wide (yawn, shout), 3 smile, 4 frown, 5 flat, 6 wavy, 7 tongue, 8 grin, -1 none
		// arms (Af = front/left, Ab = back/right): 0 up, 1 raised, 2 out (waving), 3 straight out (holding), 4 down, 5 short, -1 none
		public int Eyes, Mouth, Af, Ab;
		public int Sway;      // the top leans: - forward (left), + back
		public int Squash;    // + rows go away (shorter), - rows repeat (taller)
		public int Up;        // the whole pet is lifted (jumping)
		public int Bob;       // pot and cactus go up, the legs get longer (walking)
		public int Dx;        // the whole pet moves sideways in the frame
		public int Look, LookY;   // where the eyes look (-1 forward, +1 back; -1 up, +1 down)
		public int Bristle;   // the spines stand up (scared, angry)
		public int Sink;      // the cactus sinks into the pot (hiding)
		public int Crack;     // the pot: 1 small crack, 2 big crack, 3 patched
		public int Sit;       // the feet tuck in, the pot rests on the ground
		public int Feet;      // 0 standing (Fx/Fl, Bx/Bl move and lift them), 1 dangling, 2 splayed, 3 hidden, 4 tucked
		public int Fx, Fl, Bx, Bl;
		public bool NoCheeks, NoSpines, ShortPot, Blush, Mirror, Still;
		public char Tint = 'G';
		public Action<Frame, int, int> Front;   // props, drawn over him, given where the reference art sits
		public L With(Action<L> a) { var c = (L)MemberwiseClone(); a(c); return c; }
	}

	// the trunk (the middle arm and the body), filled, in reference coordinates (the outline comes later)
	static readonly string[] Trunk =
	{
		"......................",
		".........GGGG.........",
		".........GgGG.........",
		".........GGGG.........",
		".........GgGG.........",
		".........GGGG.........",
		"........GGGGGG........",
		".......GGGGGGGG.......",
		"......GGGGGGGGGG......",
		"......GGGGGGGGGG......",
		"......GGGGGGGGGG......",
		"......GGGGGGGGGG......",
		"......GgGGGGGgGG......",
	};
	// arms: x, y of the first row (reference coordinates) and the rows
	static readonly (int x, int y, string[] rows)[] FrontArms =
	{
		(0, 4, new[] { "..GGG.", "..GgG.", "..GGG.", "..GgG.", "...GGG", ".....G" }),                                       // 0 up
		(0, 1, new[] { ".GGG..", ".GgG..", ".GGG..", "..GGG.", "..GgG.", "..GGG.", "..GgG.", "...GGG", ".....G" }),         // 1 raised
		(-1, 2, new[] { "GGG.....", "GgGG....", ".GGGG...", "..GgGG..", "...GGGG.", "....GGGG", ".....GGG" }),             // 2 out
		(-1, 8, new[] { "GGGGGGG", "GgGGgGG", "GGGGGGG" }),                                                                 // 3 straight out
		(-1, 8, new[] { "....GGG", "..GGGG.", ".GGG...", ".GgG...", ".GGG...", ".GgG...", "..G...." }),                               // 4 down
		(0, 6, new[] { "..GGG.", "..GgG.", "...GGG", ".....G" }),                                                           // 5 short
	};
	static readonly (int x, int y, string[] rows)[] BackArms =
	{
		(16, 5, new[] { "GGG", "GgG", "GGG", "GG.", "G.." }),
		(16, 2, new[] { ".GGG", ".GgG", ".GGG", "GGG.", "GgG.", "GGG.", "GG..", "G..." }),
		(15, 3, new[] { "....GGG", "...GGgG", "..GGGG.", ".GGgG..", "GGGG...", "GGG....", "G......" }),
		(15, 8, new[] { "GGGGGGG", "GGgGGgG", "GGGGGGG" }),
		(16, 8, new[] { "GGG...", ".GGGG.", "...GGG", "...GgG", "...GGG", "...GgG", "....G." }),
		(16, 6, new[] { "GGG", "GgG", "GG.", "G.." }),
	};
	// the clay pot (reference rows 13-19)
	static readonly string[] PotArt =
	{
		"....OOOOOOOOOOOOOO....",
		"....OLLCCCCCCCCCcO....",
		"....OOOOOOOOOOOOOO....",
		".....OLCCcCCCCcCO.....",
		".....OLCCCCCCCCcO.....",
		"......OCCcCCCCcO......",
		"......OOOOOOOOOO......",
	};

	static bool Body(char c) => c != '.' && c != 'O' && c != 'S';

	/// <summary>Draws one pose: the cactus (filled, leaned, squashed, then outlined and with spines), the pot, the feet, the props.</summary>
	static Frame Draw(L l)
	{
		const int GW = 44, GH = 32, OX = 10, OY = 8;   // scratch grid: the reference (0, 0) is at (OX, OY)
		var g = new char[GW, GH];
		for (int y = 0; y < GH; y++) for (int x = 0; x < GW; x++) g[x, y] = '.';
		void Put(int rx, int ry, char c) { int x = rx + OX, y = ry + OY; if (x >= 0 && y >= 0 && x < GW && y < GH) g[x, y] = c; }
		void Rows(int rx, int ry, string[] rows) { for (int j = 0; j < rows.Length; j++) for (int i = 0; i < rows[j].Length; i++) if (rows[j][i] != '.') Put(rx + i, ry + j, rows[j][i]); }
		Rows(0, 0, Trunk);
		if (l.Squash >= 2) for (int y = 10; y <= 12; y++) { Put(5, y, 'G'); Put(16, y, 'G'); }   // squashed: the belly bulges
		if (l.Af >= 0) Rows(FrontArms[l.Af].x, FrontArms[l.Af].y, FrontArms[l.Af].rows);
		if (l.Ab >= 0) Rows(BackArms[l.Ab].x, BackArms[l.Ab].y, BackArms[l.Ab].rows);
		Face(Put, l);

		// rows of the grid with the reference row each one came from (squash and stretch move rows around)
		var rows = new List<(int orig, char[] px)>();
		for (int y = 0; y < GH; y++) { var r = new char[GW]; for (int x = 0; x < GW; x++) r[x] = g[x, y]; rows.Add((y - OY, r)); }
		int baseIdx = OY + 12;
		int[] order = { 11, 3, 5, 1 };   // the belly (everything above comes down), the neck of the middle arm, the arms, the top
		for (int i = 0; i < Math.Abs(l.Squash) && i < order.Length; i++)
		{
			int idx = rows.FindIndex(r => r.orig == order[i]);
			if (l.Squash > 0) { rows.RemoveAt(idx); rows.Insert(0, (-99, Enumerable.Repeat('.', GW).ToArray())); }
			else { rows.Insert(idx, (1000 + order[i], (char[])rows[idx].px.Clone())); rows.RemoveAt(0); }
		}
		// lean: the top shifts, the base stays
		if (l.Sway != 0)
			for (int y = 0; y < baseIdx; y++)
			{
				int h = baseIdx - y;
				int s = (int)Math.Round(l.Sway * h / 11.0, MidpointRounding.AwayFromZero);
				s = Math.Clamp(s, -Math.Abs(l.Sway), Math.Abs(l.Sway));
				if (s == 0) continue;
				var src = rows[y].px; var dst = Enumerable.Repeat('.', GW).ToArray();
				for (int x = 0; x < GW; x++) if (x - s >= 0 && x - s < GW) dst[x] = src[x - s];
				rows[y] = (rows[y].orig, dst);
			}
		if (l.Tint != 'G') foreach (var r in rows) for (int x = 0; x < GW; x++) if (r.px[x] == 'G') r.px[x] = l.Tint;
		// outline: every empty pixel next to the body (4 neighbours)
		char At(int x, int y) => x < 0 || y < 0 || x >= GW || y >= rows.Count ? '.' : rows[y].px[x];
		var outline = new List<(int, int)>();
		for (int y = 0; y < rows.Count; y++) for (int x = 0; x < GW; x++)
			if (At(x, y) == '.' && (Body(At(x - 1, y)) || Body(At(x + 1, y)) || Body(At(x, y - 1)) || Body(At(x, y + 1)))) outline.Add((x, y));
		foreach (var (x, y) in outline) rows[y].px[x] = 'O';
		// spines: light dots just outside the outline, on the sides, every other row (bristling: every row, longer)
		if (!l.NoSpines)
		{
			var add = new List<(int, int)>();
			for (int y = 0; y <= baseIdx && y < rows.Count; y++)
			{
				int o = rows[y].orig;
				bool even = o >= 0 && o < 1000 && o % 2 == 0, odd = o >= 0 && o < 1000 && o % 2 == 1;
				if (!even && !(l.Bristle > 0 && odd)) continue;
				for (int x = 0; x < GW; x++)
				{
					if (At(x, y) != 'O') continue;
					if (Body(At(x + 1, y)) && At(x - 1, y) == '.' && At(x - 2, y) == '.' && At(x - 3, y) == '.')
					{ add.Add((x - 1, y)); if (l.Bristle > 0 && even) add.Add((x - 2, y)); }
					if (Body(At(x - 1, y)) && At(x + 1, y) == '.' && At(x + 2, y) == '.' && At(x + 3, y) == '.')
					{ add.Add((x + 1, y)); if (l.Bristle > 0 && even) add.Add((x + 2, y)); }
				}
				// bristling: the tips of the arms get a spine on top too
				if (l.Bristle > 0 && y > 0)
					for (int x = 0; x < GW; x++)
						if (At(x, y) == 'O' && Body(At(x, y + 1)) && At(x, y - 1) == '.' && At(x - 1, y) == 'O' && At(x + 1, y) == 'O' && (x + o) % 2 == 0 && y - 1 >= 0)
							add.Add((x, y - 1));
			}
			foreach (var (x, y) in add) rows[y].px[x] = 'S';
		}

		// ---- put it together
		var f = new Frame();
		int cx = AX + l.Dx, cy = AY - l.Up - l.Bob + l.Sit * 2 + (l.ShortPot ? 1 : 0);
		for (int y = 0; y < rows.Count; y++)
		{
			int ry = y - OY;
			for (int x = 0; x < GW; x++)
			{
				if (rows[y].px[x] == '.') continue;
				int rx = x - OX;
				if (ry + l.Sink > 12 && (l.Sink > 0 || rx >= 4 && rx <= 17)) continue;   // inside the pot
				f[cx + rx, cy + ry + l.Sink] = rows[y].px[x];
			}
		}
		for (int j = 0; j < PotArt.Length; j++)
		{
			if (l.ShortPot && j == 4) continue;
			int ry = 13 + j - (l.ShortPot && j > 4 ? 1 : 0);
			for (int i = 0; i < PotArt[j].Length; i++) if (PotArt[j][i] != '.') f[cx + i, cy + ry] = PotArt[j][i];
		}
		// cracks and the patch
		if (l.Crack >= 1) foreach (var (x, y) in new[] { (11, 14), (11, 16), (10, 17), (11, 18) }) f[cx + x, cy + y] = 'x';
		if (l.Crack >= 2) foreach (var (x, y) in new[] { (12, 17), (9, 17), (8, 18), (12, 14), (13, 14) }) f[cx + x, cy + y] = 'x';
		if (l.Crack == 3)
		{
			foreach (var (x, y) in new[] { (11, 14), (12, 14), (13, 14) }) f[cx + x, cy + y] = 'C';
			for (int x = 8; x <= 13; x++) { f[cx + x, cy + 16] = 'p'; f[cx + x, cy + 17] = 'p'; }
			f[cx + 10, cy + 16] = 'f'; f[cx + 11, cy + 17] = 'f'; f[cx + 11, cy + 16] = 'f'; f[cx + 10, cy + 17] = 'f';
		}
		// feet
		int fx = AX + l.Dx, ground = Ground - l.Up, potBottom = cy + 19 - (l.ShortPot ? 1 : 0);
		void Foot(int x, int lift, int toe, int wide = 1)
		{
			int toeY = Math.Max(ground - lift, potBottom + 1);
			for (int y = potBottom + 1; y < toeY; y++) f[fx + x, y] = 'N';
			f[fx + x, toeY] = 'N';
			for (int k = 1; k <= wide; k++) f[fx + x + toe * k, toeY] = 'N';
		}
		void Leg(int x, int len, int toe) { for (int i = 1; i <= len; i++) f[fx + x, potBottom + i] = 'N'; f[fx + x + toe, potBottom + len] = 'N'; }
		if (l.Sit > 0) { f[fx + 5, Math.Min(potBottom, Ground)] = 'N'; f[fx + 16, Math.Min(potBottom, Ground)] = 'N'; }
		else switch (l.Feet)
		{
			case 0: Foot(7 + l.Fx, l.Fl, -1); Foot(13 + l.Bx, l.Bl, 1); break;
			case 1: Leg(7 + l.Fx, 3 - l.Fl, -1); Leg(13 + l.Bx, 3 - l.Bl, 1); break;
			case 2: Foot(7 + l.Fx, 0, -1, 2); Foot(13 + l.Bx, 0, 1, 2); break;
			case 4: f[fx + 7, potBottom + 1] = 'N'; f[fx + 6, potBottom + 1] = 'N'; f[fx + 13, potBottom + 1] = 'N'; f[fx + 14, potBottom + 1] = 'N'; break;
		}
		l.Front?.Invoke(f, cx, cy);
		if (l.Mirror) f.MirrorX();
		return f;
	}

	static void Face(Action<int, int, char> put, L l)
	{
		int ex = 9 + l.Look, ey = 7 + l.LookY;
		void Open(int x, int y) { put(x, y, 'W'); put(x + 1, y, 'E'); put(x, y + 1, 'E'); put(x + 1, y + 1, 'E'); }
		switch (l.Eyes)
		{
			case 0: Open(ex, ey); Open(ex + 3, ey); break;
			case 1: foreach (int x in new[] { ex, ex + 1, ex + 3, ex + 4 }) put(x, ey + 1, 'E'); break;
			case 2:
				put(ex - 1, ey + 1, 'E'); put(ex, ey, 'E'); put(ex + 1, ey + 1, 'E');
				put(ex + 3, ey + 1, 'E'); put(ex + 4, ey, 'E'); put(ex + 5, ey + 1, 'E');
				break;
			case 3:
				foreach (int x in new[] { ex, ex + 3 }) { put(x, ey - 1, 'W'); put(x + 1, ey - 1, 'E'); put(x, ey, 'E'); put(x + 1, ey, 'E'); put(x, ey + 1, 'E'); put(x + 1, ey + 1, 'E'); }
				break;
			case 4:
				foreach (int x in new[] { ex, ex + 3 }) { put(x, ey, 'O'); put(x + 1, ey, 'O'); put(x, ey + 1, 'E'); put(x + 1, ey + 1, 'E'); }
				break;
			case 5:
				put(ex, ey, 'O'); put(ex + 1, ey, 'E'); put(ex, ey + 1, 'E'); put(ex + 1, ey + 1, 'E'); put(ex - 1, ey - 1, 'O');
				put(ex + 3, ey, 'E'); put(ex + 4, ey, 'O'); put(ex + 3, ey + 1, 'E'); put(ex + 4, ey + 1, 'E'); put(ex + 5, ey - 1, 'O');
				break;
			case 6:
				Open(ex, ey); Open(ex + 3, ey);
				put(ex, ey + 2, 'A'); put(ex, ey + 3, 'A'); put(ex + 4, ey + 2, 'A'); put(ex + 4, ey + 3, 'A');
				break;
			case 7:
			case 8:
				foreach (int x in new[] { ex, ex + 3 })
				{
					bool a = l.Eyes == 7;
					put(x, ey, a ? 'E' : 'W'); put(x + 1, ey, a ? 'W' : 'E'); put(x, ey + 1, a ? 'W' : 'E'); put(x + 1, ey + 1, a ? 'E' : 'W');
				}
				break;
			case 9:
				foreach (int x in new[] { ex - 1, ex + 3 })
				{
					put(x, ey, 'H'); put(x + 2, ey, 'H'); put(x, ey + 1, 'H'); put(x + 1, ey + 1, 'H'); put(x + 2, ey + 1, 'H'); put(x + 1, ey + 2, 'H');
				}
				break;
			case 10:
				for (int x = ex - 1; x <= ex + 5; x++) put(x, ey, 'E');
				for (int x = ex - 1; x <= ex + 1; x++) put(x, ey + 1, 'E');
				for (int x = ex + 3; x <= ex + 5; x++) put(x, ey + 1, 'E');
				put(ex - 1, ey, 'u'); put(ex + 3, ey, 'u');
				break;
			case 11:
				Open(ex, ey);
				put(ex + 3, ey + 1, 'E'); put(ex + 4, ey + 1, 'E');
				break;
			case 12:
				put(ex - 1, ey, 'E'); put(ex, ey + 1, 'E'); put(ex - 1, ey + 2, 'E');
				put(ex + 5, ey, 'E'); put(ex + 4, ey + 1, 'E'); put(ex + 5, ey + 2, 'E');
				break;
		}
		int mx = 10 + l.Look, my = 10 + Math.Max(0, l.LookY);
		switch (l.Mouth)
		{
			case 0: put(mx, my, 'K'); put(mx + 1, my, 'K'); break;
			case 1: put(mx, my, 'K'); put(mx + 1, my, 'K'); put(mx, my + 1, 'K'); put(mx + 1, my + 1, 'K'); break;
			case 2:
				put(mx, my, 'K'); put(mx + 1, my, 'K'); put(mx - 1, my + 1, 'K'); put(mx, my + 1, 'r'); put(mx + 1, my + 1, 'r'); put(mx + 2, my + 1, 'K');
				put(mx, my + 2, 'K'); put(mx + 1, my + 2, 'K');
				break;
			case 3: put(mx - 1, my, 'K'); put(mx + 2, my, 'K'); put(mx, my + 1, 'K'); put(mx + 1, my + 1, 'K'); break;
			case 4: put(mx, my, 'K'); put(mx + 1, my, 'K'); put(mx - 1, my + 1, 'K'); put(mx + 2, my + 1, 'K'); break;
			case 5: for (int x = mx - 1; x <= mx + 2; x++) put(x, my, 'K'); break;
			case 6: put(mx - 1, my, 'K'); put(mx + 1, my, 'K'); put(mx, my + 1, 'K'); put(mx + 2, my + 1, 'K'); break;
			case 7: put(mx, my, 'K'); put(mx + 1, my, 'K'); put(mx + 1, my + 1, 'P'); break;
			case 8: for (int x = mx - 1; x <= mx + 2; x++) put(x, my, 'K'); put(mx, my + 1, 'r'); put(mx + 1, my + 1, 'r'); break;
		}
		if (!l.NoCheeks && l.Eyes >= 0)
		{
			put(8 + l.Look, 10, 'P'); put(13 + l.Look, 10, 'P');
			if (l.Blush) { put(7 + l.Look, 10, 'P'); put(14 + l.Look, 10, 'P'); put(8 + l.Look, 11, 'P'); put(13 + l.Look, 11, 'P'); }
		}
	}

	public static PetDef Build()
	{
		Frame.NewHeadroom = Head;
		Frame.NewWidth = W;
		var p = new PetDef
		{
			Folder = "cacto_vaso",
			Title = "Xiquinho the Cactus",
			PetName = "Xiquinho",
			Author = "Luan Felipe",
			Version = "1.0",
			Info = "Xiquinho is a little cactus in a clay pot who walks on two tiny feet.[br]" +
				   "He loves the sun and a little water, blooms a pink flower when you water him,[br]" +
				   "sneezes his spines off, pops balloons by accident and makes friends with bees, birds and lizards.",
			Palette = new Dictionary<char, Color>
			{
				['O'] = Color.FromArgb(30, 70, 40), ['G'] = Color.FromArgb(86, 160, 80), ['g'] = Color.FromArgb(134, 200, 110),
				['1'] = Color.FromArgb(92, 150, 156), ['2'] = Color.FromArgb(196, 100, 70),
				['E'] = Color.FromArgb(20, 20, 20), ['W'] = Color.FromArgb(255, 255, 255), ['P'] = Color.FromArgb(255, 157, 176),
				['K'] = Color.FromArgb(58, 30, 30), ['r'] = Color.FromArgb(168, 42, 52),
				['C'] = Color.FromArgb(210, 105, 60), ['c'] = Color.FromArgb(160, 74, 40), ['L'] = Color.FromArgb(232, 148, 100),
				['N'] = Color.FromArgb(140, 90, 48), ['S'] = Color.FromArgb(244, 240, 220),
				['x'] = Color.FromArgb(90, 42, 20), ['p'] = Color.FromArgb(240, 204, 156),
				['R'] = Color.FromArgb(222, 52, 52), ['H'] = Color.FromArgb(232, 62, 98), ['h'] = Color.FromArgb(255, 150, 170),
				['F'] = Color.FromArgb(255, 111, 168), ['f'] = Color.FromArgb(255, 190, 218),
				['Y'] = Color.FromArgb(255, 217, 59), ['y'] = Color.FromArgb(226, 158, 30), ['X'] = Color.FromArgb(255, 240, 140),
				['V'] = Color.FromArgb(255, 150, 46),
				['A'] = Color.FromArgb(90, 170, 240), ['a'] = Color.FromArgb(170, 218, 250),
				['U'] = Color.FromArgb(50, 102, 220), ['u'] = Color.FromArgb(140, 184, 245),
				['k'] = Color.FromArgb(140, 140, 150), ['z'] = Color.FromArgb(232, 236, 244), ['l'] = Color.FromArgb(104, 104, 116),
				['Z'] = Color.FromArgb(150, 170, 240), ['q'] = Color.FromArgb(120, 84, 220),
				['B'] = Color.FromArgb(92, 60, 32), ['b'] = Color.FromArgb(186, 136, 84), ['n'] = Color.FromArgb(140, 98, 56),
				['T'] = Color.FromArgb(206, 166, 98), ['t'] = Color.FromArgb(140, 106, 56),
				['J'] = Color.FromArgb(150, 138, 66), ['j'] = Color.FromArgb(214, 204, 140), ['i'] = Color.FromArgb(96, 86, 40),
				['M'] = Color.FromArgb(150, 186, 206), ['m'] = Color.FromArgb(84, 120, 146), ['w'] = Color.FromArgb(236, 244, 252),
				['v'] = Color.FromArgb(178, 112, 226), ['e'] = Color.FromArgb(96, 52, 140),
				['Q'] = Color.FromArgb(250, 246, 234), ['I'] = Color.FromArgb(60, 62, 74), ['o'] = Color.FromArgb(120, 74, 40),
				['s'] = Color.FromArgb(226, 200, 140),
			},
			Scale = 2,
			TilesX = 8,
		};

		// ------------------------------------------------------------ helpers
		var seen = new Dictionary<string, int>();
		int Add(Frame f) { string k = f.ToString(); if (!seen.TryGetValue(k, out int i)) { i = p.Add(f); seen[k] = i; } return i; }
		var looks = new Dictionary<int, L>();
		int P(L l) { int id = Add(Draw(l)); looks.TryAdd(id, l); return id; }
		// breathing: a held pose alternates with the same pose one row lower
		var breathOf = new Dictionary<int, int>();
		int Breath(int f)
		{
			if (breathOf.TryGetValue(f, out int b)) return b;
			if (!looks.TryGetValue(f, out var l) || l.Still || l.Squash >= 3 || l.Sink > 0 || l.Sit > 0 && l.Squash >= 2) return breathOf[f] = f;
			return breathOf[f] = P(l.With(x => x.Squash += 1));
		}
		int[] Hold(int f, int n) => n < 3 ? Enumerable.Repeat(f, n).ToArray() : Enumerable.Range(0, n).Select(i => (i / 2) % 2 == 0 ? f : Breath(f)).ToArray();
		void St(Frame f, int x, int y, params string[] rows) => f.Stamp(x, y, rows.Select(r => r.Replace('.', ' ')).ToArray());
		// a prop with a dark outline all around it
		void Prop(Frame f, int x, int y, char oc, params string[] rows)
		{
			var t = new Frame(); St(t, x, y, rows);
			for (int yy = t.MinY; yy < W; yy++) for (int xx = 0; xx < W; xx++)
				if (t[xx, yy] == '.' && (t[xx - 1, yy] != '.' || t[xx + 1, yy] != '.' || t[xx, yy - 1] != '.' || t[xx, yy + 1] != '.')) f[xx, yy] = oc;
			for (int yy = t.MinY; yy < W; yy++) for (int xx = 0; xx < W; xx++) if (t[xx, yy] != '.') f[xx, yy] = t[xx, yy];
		}
		Frame Empty() => new Frame();
		int fNone = Add(Empty());
		// the top of his middle arm in a drawn frame (follows the lean and the squash)
		(int x, int y) TopOf(Frame f, int cx)
		{
			for (int y = f.MinY; y < W; y++)
			{
				int x0 = -1, x1 = -1;
				for (int x = cx + 6; x <= cx + 15; x++) if (f[x, y] == 'O') { if (x0 < 0) x0 = x; x1 = x; }
				if (x0 >= 0) return ((x0 + x1) / 2, y);
			}
			return (cx + 10, AY);
		}

		// ------------------------------------------------------------ small things
		void HeartAt(Frame f, int x, int y) => St(f, x, y, "HH.HH", "HhHHH", ".HHH.", "..H..");
		void SmallHeart(Frame f, int x, int y) => St(f, x, y, "H.H", "HHH", ".H.");
		void NoteAt(Frame f, int x, int y) => St(f, x, y, ".qq", ".q.", "qq.");
		void Drop(Frame f, int x, int y) { f.Dot(x, y, 'a'); f.Dot(x, y + 1, 'A'); }
		void Zed(Frame f, int x, int y) => St(f, x, y, "ZZZZ", "..Z.", ".Z..", "ZZZZ");
		void Spark(Frame f, int x, int y) => St(f, x, y, ".X.", "XYX", ".X.");
		void Sweat(Frame f, int x, int y) => St(f, x, y, ".a.", "aAa", "AAA", ".A.");
		void Bubble(Frame f, int x, int y, string[] glyphs)
		{
			int w = glyphs.Max(gl => gl.Length) + 4;
			var rows = new List<string> { "." + new string('K', w - 2) + "." };
			for (int i = 0; i < glyphs.Length + 2; i++) rows.Add("K" + new string('W', w - 2) + "K");
			rows.Add("." + new string('K', w - 2) + ".");
			St(f, x, y, rows.ToArray());
			St(f, x + w - 5, y + rows.Count - 1, "KWK", ".KK", "..K");
			St(f, x + 2, y + 2, glyphs);
		}
		string[] bang = { "E", "E", "E", ".", "E" };
		string[] question = { "EEE", "..E", ".E.", "...", ".E." };
		string[] okTxt = { "EEE.E.E", "E.E.EE.", "E.E.E..", "E.E.EE.", "EEE.E.E" };
		string[] check = { "......G", ".....GG", "G...GG.", "GG.GG..", ".GGG...", "..G...." };
		string[] kkk = { "E...E...E..", "E.E.E.E.E.E", "EE..EE..EE.", "E.E.E.E.E.E", "E.E.E.E.E.E" };
		string[] oi = { "EEE..E", "E.E...", "E.E..E", "E.E..E", "EEE..E" };
		string[] music = { "..qqq", "..q.q", "..q.q", "qqq.q", "qq.qq" };
		string[] heartTxt = { "HH.HH", "HHHHH", "HHHHH", ".HHH.", "..H.." };
		string[] dots = { ".....", ".....", ".....", ".....", "E.E.E" };

		void Flower(Frame f, int cx, int stage)
		{
			var (tx, ty) = TopOf(f, cx);
			int x = tx - 2;
			switch (stage)
			{
				case 1: Prop(f, x + 1, ty - 2, 'K', ".F", "gg"); f.Dot(x + 2, ty - 2, 'F'); break;
				case 2: Prop(f, x, ty - 4, 'K', "..F..", ".FfF.", ".FFF.", "..g.."); break;
				case 3: Prop(f, x - 1, ty - 5, 'K', ".F.F.F.", "FfFfFfF", "FFYYYFF", ".FFFFF.", "...g..."); break;
				case 4: Prop(f, x - 1, ty - 5, 'K', "F.F.F.F", "FfFfFfF", "FFYXYFF", "FfFFFfF", "...g..."); break;
			}
		}
		void Can(Frame f, int x, int y, bool tilt)
		{
			if (!tilt) Prop(f, x, y, 'K', "...mmm......", "..m...m.....", ".MMMMMMM...M", ".MwMMMMMm.MM", ".MwMMMMMMMm.", ".MMMMMMMm...", ".mmmmmmm....");
			else Prop(f, x, y, 'K', "..mmm.......", ".m...m......", "MMMMMMm.....", "MwMMMMMM....", "MwMMMMMMMm..", ".MMMMMMMMMm.", "..mmmmmmm.MM", "...........M");
		}
		void Balloon(Frame f, int x, int y, int k, bool heart = false)
		{
			if (heart) Prop(f, x, y, 'K', ".HH.HH.", "HhHHHHH", "HhHHHHH", ".HHHHH.", "..HHH..", "...H...");
			else Prop(f, x, y, 'K', "..RRR..", ".RRRRR.", "RRWRRRR", "RWRRRRR", "RRRRRRR", ".RRRRR.", "..RRR..");
			int sy = y + (heart ? 7 : 8);
			if (!heart) f.Dot(x + 3, y + 7, 'r');
			for (int i = 0; i < 7; i++) f.Dot(x + 3 + (i % 2 == 0 ? 0 : k), sy + i, 'l');
		}
		void Burst(Frame f, int x, int y, char c)
		{
			St(f, x, y, "X..X..X", ".X.X.X.", "..XXX..", "XXX.XXX", "..XXX..", ".X.X.X.", "X..X..X");
			f.Dot(x + 1, y + 3, c); f.Dot(x + 5, y + 3, c); f.Dot(x + 3, y + 1, c); f.Dot(x + 3, y + 5, c);
		}
		void Bee(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "..ww..", ".YEYEE", "VEYEYE", "..E.E." } : new[] { ".w..w.", ".YEYEE", "VEYEYE", "......" });
		void BirdAt(Frame f, int x, int y, int k)
		{
			// a little bem-te-vi facing left (yellow belly, white brow): 0 standing, 1 wings up, 2 wings down, 3 startled
			Prop(f, x, y, 'K', "..BBBB....", ".BWWWWB...", "VBEBBBBB..", ".BYYYYbbBB", "..YYYYbbbB", "...YYYbb..");
			switch (k)
			{
				case 0: f.Dot(x + 4, y + 7, 'V'); f.Dot(x + 6, y + 7, 'V'); break;
				case 1: Prop(f, x + 5, y - 3, 'K', "...B", "..bB", ".bbB"); break;
				case 2: Prop(f, x + 5, y + 4, 'K', ".bbB", "..bB", "...B"); break;
				case 3: Prop(f, x + 5, y - 3, 'K', "B..B", ".bbB", ".bbB"); f.Dot(x + 4, y + 7, 'V'); f.Dot(x + 6, y + 7, 'V'); break;
			}
		}
		void Butterfly(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "vv.vv", "vfefv", ".vev.", "v...v" } : k == 1 ? new[] { ".v.v.", ".vev.", "..e..", "....." } : new[] { "..v..", "..e..", "..e..", "....." });
		void Petal(Frame f, int x, int y, int k) => St(f, x, y, k == 0 ? new[] { "Ff", "fF" } : new[] { ".F", "Ff" });
		void Bulb(Frame f, int x, int y, bool lit) => Prop(f, x, y, 'K', (lit ? new[] { ".YYY.", "YXXWY", "YXXXY", ".YYY.", ".kkk.", "..k.." } : new[] { ".zzz.", "zzzWz", "zzzzz", ".zzz.", ".kkk.", "..k.." }));
		void Rays(Frame f, int x, int y) { f.Dot(x + 2, y - 2, 'X'); f.Dot(x - 2, y + 1, 'X'); f.Dot(x + 6, y + 1, 'X'); f.Dot(x - 1, y - 1, 'X'); f.Dot(x + 5, y - 1, 'X'); }
		void Glasses(Frame f, int cx, int cy) { int x = cx + 8, y = cy + 6; St(f, x, y, "llll.llll", "l..l.l..l", "l..lll..l", "llll.llll"); }
		void Book(Frame f, int x, int y) => Prop(f, x, y, 'K', "UUUUUUUUU", "UQQQUQQQU", "UQllUllQU", "UQQQUQQQU", "UUUUUUUUU");
		void Laptop(Frame f, int x, int y, int k)
		{
			Prop(f, x, y, 'K', "IIIIIIIIII", "IUUUUUUUUI", "IUggUUgUUI", "IUUUggUUUI", "IIIIIIIIII", ".kkkkkkkkkk", "kkkkkkkkkkk");
			if (k == 1) { f.Dot(x + 3, y + 3, 'X'); f.Dot(x + 7, y + 2, 'g'); }
		}
		void Paper(Frame f, int x, int y, int lines) { Prop(f, x, y, 'K', "QQQQQQQ", "QQQQQQQ", "QQQQQQQ", "QQQQQQQ", "QQQQQQQ"); for (int i = 0; i < lines; i++) f.Rect(x + 1, y + 1 + i, i == lines - 1 ? 3 : 5, 1, 'l'); }
		void Pencil(Frame f, int x, int y) => Prop(f, x, y, 'K', "E...", ".pY.", "..YY", "...R");
		void Clipboard(Frame f, int x, int y, int ticks)
		{
			Prop(f, x, y, 'K', "..kkk..", "bbbbbbb", "bQQQQQb", "bQQQQQb", "bQQQQQb", "bQQQQQb", "bbbbbbb");
			for (int i = 0; i < 3; i++) { f.Rect(x + 3, y + 2 + i, 3, 1, 'l'); if (i < ticks) f.Dot(x + 2, y + 2 + i, 'G'); }
		}
		void LoadingRing(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (0, -3), (2, -2), (3, 0), (2, 2), (0, 3), (-2, 2), (-3, 0), (-2, -2) };
			for (int i = 0; i < 8; i++) { var (dx, dy) = pts[i]; f.Dot(cx + dx, cy + dy, ((i - k * 2) % 8 + 8) % 8 < 3 ? 'U' : 'u'); }
		}
		void Box(Frame f, int x, int y) => Prop(f, x, y, 'K', "bbbBbbb", "bbbBbbb", "BBBBBBB", "bbbBbbb", "bbbBbbb");
		void Magnifier(Frame f, int x, int y) => Prop(f, x, y, 'K', ".III.", "IaawI", "IaaaI", "IaaaI", ".III.", "....B", ".....");
		void Deerstalker(Frame f, int tx, int ty) => Prop(f, tx - 4, ty - 3, 'K', "..bnbnb..", ".bnbnbnb.", "nbnbnbnbn", "b.......b");
		void Telescope(Frame f, int x, int y) => Prop(f, x, y, 'K', "kk.......", "IIkk.....", "IIIIkk...", ".IIIIIkk.", "...IIIIIk", ".....B...", "....B.B..", "...B...B.");
		void Flag(Frame f, int x, int y, int k)
		{
			for (int i = 0; i < 12; i++) f.Dot(x + 7, y + i, 'B');
			Prop(f, x, y + (k == 0 ? 0 : 1), 'K', k == 0
				? new[] { "GGGGGGG", "GGGYGGG", "GGYUYGG", "GGGYGGG", "GGGGGGG" }
				: new[] { ".GGGGGG", "GGGYGGG", "GGYUYGG", "GGGYGG.", "GGGGG.." });
			f.Dot(x + 7, y - 1, 'Y');
		}
		void Warning(Frame f, int x, int y) => Prop(f, x, y, 'K', "....Y....", "...YYY...", "...YEY...", "..YYEYY..", "..YYEYY..", ".YYYYYYY.", ".YYYEYYY.", "YYYYYYYYY");
		void Steam(Frame f, int x, int y) => St(f, x, y, ".zz.", "zzkz", ".zz.");
		void Umbrella(Frame f, int x, int y)
		{
			Prop(f, x, y, 'K', ".....RRR.....", "...RRWRRWR...", ".RRRWRRRWRRR.", "RRRRWRRRWRRRR", "R.R.R.R.R.R.R");
			for (int i = 5; i < 12; i++) f.Dot(x + 6, y + i, 'B');
		}
		void Fan(Frame f, int x, int y, int k) => Prop(f, x, y, 'K', k == 0 ? new[] { "fFfFf", "FfFfF", ".fFf.", "..B..", "..B.." } : new[] { ".FfF.", "fFfFf", ".FfF.", "..B..", "..B.." });
		void Sun(Frame f, int x, int y, int k)
		{
			if (k == 0) St(f, x, y, "....X....", ".X.....X.", "...YYY...", "..YYYYY..", "XYYXYYYYX", "..YYYYY..", "...YYY...", ".X.....X.", "....X....");
			else St(f, x, y, "..X...X..", ".........", "X..YYY..X", "..YYYYY..", "..YXYYY..", "..YYYYY..", "X..YYY..X", ".........", "..X...X..");
		}
		void IceCream(Frame f, int x, int y, int bites) { Prop(f, x, y, 'K', bites == 0 ? new[] { ".fFf.", "fFfFf", "FfFfF", ".bnb.", ".nbn.", "..b.." } : new[] { ".....", ".Ff..", "FfFfF", ".bnb.", ".nbn.", "..b.." }); }
		void Scarf(Frame f, int cx, int cy) { St(f, cx + 5, cy + 9, "RWRWRWRWRWRW"); St(f, cx + 5, cy + 10, "WRWRWRWRWRWR"); St(f, cx + 6, cy + 11, "RW", "WR", "RW"); }
		void SnowCap(Frame f, int cx, int cy, int n)
		{
			var (tx, ty) = TopOf(f, cx);
			if (n >= 1) Prop(f, tx - 2, ty - 2, 'k', ".zzW.", "zzzzz");
			if (n >= 2) { Prop(f, cx + 2, cy + 2, 'k', ".zW", "zzz"); Prop(f, cx + 16, cy + 3, 'k', "zz.", "zzz"); }
		}
		void Snow(Frame f, int k) { foreach (var (sx, sy) in new[] { (2, -6), (9, -2), (21, -5), (31, 0), (5, 8), (33, 10), (14, -8), (27, 14), (1, 18) }) f.Dot(sx, ((sy + k * 3) + 60) % 30 - 8, 'u'); }
		void CloudAt(Frame f, int x, int y, int k)
		{
			Prop(f, x, y, 'k', "....zzzz.....", "..zzzzzzzz...", ".zzzzzzzzzzz.", "zzzzzzzzzzzzz", ".zzzzzzzzzzz.");
			for (int i = 0; i < 5; i++) Drop(f, x + 2 + i * 2 + (k % 2), y + 7 + ((i + k) % 3) * 3);
		}
		void PartyHat(Frame f, int cx) { var (tx, ty) = TopOf(f, cx); Prop(f, tx - 2, ty - 6, 'K', "..R..", "..Y..", ".YUY.", ".UYU.", "YUYUY"); }
		void Broom(Frame f, int x, int k) { for (int i = 0; i < 9; i++) f.Dot(x + 7 - i / 2 + k, 24 + i, 'B'); Prop(f, x + k, 32, 'K', ".yyyy", "yYyYy", "YyYyY"); }
		void Hammer(Frame f, int x, int y, bool up) => Prop(f, x, y, 'K', up ? new[] { "kkk", "kkk", ".B.", ".B." } : new[] { "Bkk", ".kk" });
		void Headphones(Frame f, int cx, int cy) { St(f, cx + 6, cy + 2, "..qqqqqqqqqq..", ".q..........q.", "q............q"); f.Rect(cx + 5, cy + 5, 2, 4, 'q'); f.Rect(cx + 17, cy + 5, 2, 4, 'q'); }
		void Cup(Frame f, int x, int y) => Prop(f, x, y, 'K', "QQQQ.", "QooQQ", "QQQQ.Q", "QQQQQ.", ".QQQ..");
		void Glass(Frame f, int x, int y, int level)
		{
			Prop(f, x, y, 'K', "a....a", "a....a", "a....a", "a....a", ".aaaa.");
			for (int yy = 3 - level; yy < 4; yy++) f.Rect(x + 1, y + yy, 4, 1, 'A');
		}
		void Melon(Frame f, int x, int y, int bites)
		{
			string[] m = bites switch
			{
				0 => new[] { "RRRRRRR", "RERRERR", ".RRRRR.", "..ggg..", "...G..." },
				1 => new[] { "..RRRRR", "..RERRR", ".RRRRR.", "..ggg..", "...G..." },
				2 => new[] { "....RRR", "....ERR", "...RRR.", "..ggg..", "...G..." },
				_ => new[] { ".......", ".......", "....g..", "..ggg..", "...G..." },
			};
			Prop(f, x, y, 'K', m);
		}
		void Cookie(Frame f, int x, int y, int bites) => Prop(f, x, y, 'K', bites == 0 ? new[] { ".bbb.", "bBbbb", "bbbBb", ".bbb." } : bites == 1 ? new[] { ".bb..", "bBb..", "bbbB.", ".bbb." } : new[] { ".....", "b....", "bb...", ".b..." });
		void Crumbs(Frame f, int x, int y) { f.Dot(x, y, 'b'); f.Dot(x + 2, y + 2, 'b'); f.Dot(x - 1, y + 3, 'n'); }
		void Confetti(Frame f, int k)
		{
			char[] cs = { 'H', 'Y', 'U', 'g', 'v', 'V' };
			for (int i = 0; i < 22; i++) f.Dot((i * 7 + k * 3) % W, -8 + (i * 5 + k * 4) % 36, cs[(i + k) % cs.Length]);
		}
		void Dust(Frame f, int x, int y) => St(f, x, y, "..kk..", ".kzzk.", "kzzkzk", ".kkkk.");
		void Stars(Frame f, int cx, int cy, int k)
		{
			var pts = new[] { (6, 1), (10, -1), (15, 0), (11, 2) };
			for (int i = 0; i < 4; i++) { var (sx, sy) = pts[(i + k) % 4]; f.Dot(cx + sx, cy + sy, 'Y'); f.Dot(cx + sx + 1, cy + sy, 'X'); f.Dot(cx + sx, cy + sy - 1, 'X'); }
		}
		void ThoughtCloud(Frame f, int x, int y, int k)
		{
			f.Dot(x + 10, y + 9, 'k'); f.Dot(x + 8, y + 7, 'k'); f.Dot(x + 9, y + 7, 'k');
			if (k >= 1) Prop(f, x, y, 'k', "..zzzz..", ".zzzzzz.", "zzzzzzzz", ".zzzzzz.");
			if (k >= 2) { f.Dot(x + 2, y + 2, 'k'); f.Dot(x + 4, y + 2, 'k'); f.Dot(x + 6, y + 2, 'k'); }
		}
		void Lizard(Frame f, int x, int y, int k, bool right = true)
		{
			// a calango seen from the side (facing right); k: 0/1 walking legs, 2 push-up (front up)
			string[] art = k switch
			{
				0 => new[] { "...............JJ..", "..............JJWJ.", "........JJJJJJJJEJJ", "JJJJJJJJiJiJiJJJJ..", "........jjjjjjj....", ".......J.J....J.J..", "......J....J.J....J" },
				1 => new[] { "...............JJ..", "..............JJWJ.", "........JJJJJJJJEJJ", "JJJJJJJJiJiJiJJJJ..", "........jjjjjjj....", "........JJ....JJ...", "........J.J...J.J.." },
				_ => new[] { "...............JJ..", "..............JJWJ.", "............JJJJEJJ", "........JJJJJJJJJ..", "JJJJJJJJiJiJijjj...", "........jjjjJ.J.J..", ".......J.J...J...J." },
			};
			if (!right) art = art.Select(r => new string(r.Reverse().ToArray())).ToArray();
			Prop(f, x, y, 'K', art);
		}
		string[] Tumble(int rot)
		{
			// a tumbleweed: a ring of twigs with twigs across it, turned in quarter turns
			var grid = new char[13, 13];
			for (int yy = 0; yy < 13; yy++) for (int xx = 0; xx < 13; xx++) grid[xx, yy] = '.';
			void Set(int x, int y, char c)
			{
				int u = x, v = y;
				for (int r = 0; r < rot; r++) (u, v) = (-v, u);
				if (u >= -6 && u <= 6 && v >= -6 && v <= 6) grid[u + 6, v + 6] = c;
			}
			for (double a = 0; a < 2 * Math.PI; a += 0.12) Set((int)Math.Round(5 * Math.Cos(a)), (int)Math.Round(5 * Math.Sin(a)), 't');
			foreach (var (x0, y0, x1, y1) in new[] { (-4, -2, 3, 3), (-2, 4, 3, -4), (-4, 1, 4, -1), (0, -5, 1, 5), (-3, -4, 4, 2) })
			{
				int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
				for (int i = 0; i <= n; i++) Set(x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n, 'T');
			}
			foreach (var (x, y) in new[] { (-6, -1), (6, 2), (2, -6), (-1, 6), (5, -4), (-5, 4) }) Set(x, y, 'T');
			var rowsOut = new string[13];
			for (int yy = 0; yy < 13; yy++) { var sb = new System.Text.StringBuilder(); for (int xx = 0; xx < 13; xx++) sb.Append(grid[xx, yy]); rowsOut[yy] = sb.ToString(); }
			return rowsOut;
		}
		void BabyCactus(Frame f, int x, int stage, int k)
		{
			// a tiny cactus in a tiny pot, on the ground (x = left of the pot)
			Prop(f, x, 30, 'O', ".CCCCCCC.", ".OOOOOOO.", "..CcCCC..", "..CCCCC..");
			int y = 30;
			if (stage == 1) Prop(f, x + 3, y - 2, 'O', "gG.", ".G.");
			if (stage == 2) Prop(f, x + 3, y - 4, 'O', ".G.", "GgG", "GGG");
			if (stage >= 3)
			{
				var art = k == 0 ? new[] { "...GG...", "G..GG..G", "GG.GgG.G", ".GGEGEG.", "..GGGG.." } : new[] { "...GG..G", "...GG.GG", "GG.GgGG.", ".GGEGEG.", "..GGGG.." };
				Prop(f, x, y - 5, 'O', art);
			}
		}
		Frame Rot(Frame src, int quarter)
		{
			int minX = W, minY = W, maxX = -1, maxY = -100;
			for (int y = src.MinY; y < W; y++) for (int x = 0; x < W; x++) if (src[x, y] != '.') { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
			int w = maxX - minX + 1, h = maxY - minY + 1;
			int nw = quarter % 2 == 1 ? h : w, nh = quarter % 2 == 1 ? w : h;
			int x0 = (minX + maxX + 1) / 2 - nw / 2, y0 = Ground - nh + 1;
			var f = new Frame();
			for (int v = 0; v < h; v++) for (int u = 0; u < w; u++)
			{
				char c = src[minX + u, minY + v];
				if (c == '.') continue;
				var (nu, nv) = quarter switch { 1 => (v, w - 1 - u), 2 => (w - 1 - u, h - 1 - v), 3 => (h - 1 - v, u), _ => (u, v) };
				f[x0 + nu, y0 + nv] = c;
			}
			return f;
		}

		// ------------------------------------------------------------ frames: the basics
		var stand = new L();
		int fStand = P(stand);
		int fBlink = P(new L { Eyes = 1 });
		int fHappy = P(new L { Eyes = 2, Mouth = 3 });
		int fHappyUp = P(new L { Eyes = 2, Mouth = 3, Af = 1, Ab = 1 });
		int fWide = P(new L { Eyes = 3, Mouth = 1 });
		int fUp = P(new L { LookY = -1 });
		int fUpHappy = P(new L { LookY = -1, Mouth = 3, Af = 1, Ab = 1 });
		int fBack = P(new L { Look = 1 });
		int fFwd = P(new L { Look = -1 });
		int fSmile = P(new L { Mouth = 3 });
		int fCrouch = P(new L { Squash = 2, Eyes = 1, ShortPot = true, Bob = -1, Feet = 2 });
		int fCrouch1 = P(new L { Squash = 1, Eyes = 0 });
		int fSitDown = P(new L { Sit = 1, Mouth = 3 });
		int fSit = P(new L { Sit = 1 });
		int fSitBlink = P(new L { Sit = 1, Eyes = 1 });

		// walking: 8 steps; the planted foot moves back 1 px a step while he moves 1 px forward, the other foot swings forward
		int[] ffx = { -2, -1, 0, 1, 1, 0, -1, -2 }, ffl = { 0, 0, 0, 0, 1, 2, 2, 1 };
		int[] bfx = { 1, 0, -1, -2, -2, -1, 0, 1 }, bfl = { 1, 2, 2, 1, 0, 0, 0, 0 };
		int[] bob = { 0, 1, 1, 0, 0, 1, 1, 0 }, sway = { 0, -1, -1, 0, 0, 1, 1, 0 };
		var walkF = new int[8];
		L WalkLook(int i) => new L { Look = -1, Fx = ffx[i], Fl = ffl[i], Bx = bfx[i], Bl = bfl[i], Bob = bob[i], Sway = sway[i], Still = true };
		for (int i = 0; i < 8; i++) walkF[i] = P(WalkLook(i));
		int fLeanBack = P(new L { Look = -1, Sway = 1 });
		int fPlant = P(new L { Look = -1, Sway = -1 });
		int fPlant2 = P(new L { Look = -1, Sway = -2 });
		int fPlant3 = P(new L { Sway = 1 });
		// running: quick little steps, leaning forward
		var runF = new int[4];
		runF[0] = P(new L { Look = -1, Mouth = 5, Sway = -2, Fx = -2, Fl = 1, Bx = 1, Bob = 0, Still = true });
		runF[1] = P(new L { Look = -1, Mouth = 5, Sway = -1, Fx = 0, Fl = 1, Bx = 0, Bl = 1, Bob = 1, Still = true });
		runF[2] = P(new L { Look = -1, Mouth = 5, Sway = -2, Fx = 1, Bx = -2, Bl = 1, Bob = 0, Still = true });
		runF[3] = P(new L { Look = -1, Mouth = 5, Sway = -1, Fx = 0, Fl = 1, Bx = 0, Bl = 1, Bob = 1, Still = true });
		// turning around
		int fTurn1 = P(new L { Af = 5, Ab = 5, Look = 1, Sway = 1 });
		int fTurn2 = P(new L { Af = 5, Ab = 5, Look = 1, Mirror = true });
		// in the air
		int fDrag1 = P(new L { Eyes = 3, Mouth = 1, Af = 2, Ab = 2, Up = 2, Feet = 1, Fl = 1, Bristle = 1, Still = true });
		int fDrag2 = P(new L { Eyes = 3, Mouth = 2, Af = 1, Ab = 1, Up = 2, Feet = 1, Bl = 1, Bristle = 1, Still = true });
		int fDrag3 = P(new L { Eyes = 12, Mouth = 6, Af = 2, Ab = 2, Up = 2, Feet = 1, Fl = 1, Bl = 1, Bristle = 1, Sway = 1, Still = true });
		int fFall1 = P(new L { Eyes = 3, Mouth = 2, Af = 1, Ab = 1, Up = 2, Feet = 1, Bristle = 1, Squash = -1, Still = true });
		int fFall2 = P(new L { Eyes = 3, Mouth = 2, Af = 2, Ab = 2, Up = 2, Feet = 1, Fl = 1, Bl = 1, Bristle = 1, Squash = -1, Still = true });
		int fLand1 = P(new L { Eyes = 12, Mouth = 5, Squash = 3, ShortPot = true, Bob = -1, Feet = 2, Af = 4, Ab = 4, Still = true });
		int fLand2 = P(new L { Eyes = 1, Mouth = 0, Squash = 2, ShortPot = true, Bob = -1, Feet = 2, Still = true });
		int fLand3 = P(new L { Eyes = 3, Mouth = 1, Squash = -1, Af = 1, Ab = 1, Still = true });
		var dz0 = new L { Eyes = 7, Mouth = 6, Squash = 3, ShortPot = true, Bob = -1, Feet = 2, Af = 4, Ab = 4, Still = true };
		int fHard1 = P(dz0.With(l => l.Front = (f, cx, cy) => Stars(f, cx, cy, 0)));
		int fHard2 = P(dz0.With(l => { l.Eyes = 8; l.Squash = 2; l.Front = (f, cx, cy) => Stars(f, cx, cy + 1, 1); }));
		int fHard3 = P(dz0.With(l => { l.Eyes = 7; l.Squash = 2; l.Front = (f, cx, cy) => Stars(f, cx, cy + 1, 2); }));
		// jumping (squash and stretch)
		int fJumpPrep = P(new L { Eyes = 12, Mouth = 5, Squash = 2, ShortPot = true, Bob = -1, Feet = 2, Still = true });
		int fJumpUp = P(new L { Eyes = 2, Mouth = 3, Squash = -2, Af = 1, Ab = 1, Up = 3, Feet = 4, Still = true });
		int fJumpTop = P(new L { Eyes = 2, Mouth = 8, Squash = -1, Af = 2, Ab = 2, Up = 5, Feet = 4, Still = true });
		int fJumpDown = P(new L { Eyes = 3, Mouth = 1, Squash = 0, Af = 1, Ab = 1, Up = 3, Feet = 1, Still = true });
		int fHopAir = P(new L { Eyes = 2, Mouth = 3, Squash = -1, Af = 1, Ab = 1, Up = 1, Feet = 4, Still = true });

		// idle
		int fSleepA = P(new L { Sit = 1, Eyes = 1, Af = 4, Ab = 4, Sway = 1, Still = true });
		int fSleepB = P(new L { Sit = 1, Eyes = 1, Af = 4, Ab = 4, Sway = 1, Squash = 1, Mouth = 1, Still = true, Front = (f, cx, cy) => Zed(f, cx + 21, cy - 2) });
		int fSleepC = P(new L { Sit = 1, Eyes = 1, Af = 4, Ab = 4, Sway = 1, Squash = 1, Mouth = 1, Still = true, Front = (f, cx, cy) => { Zed(f, cx + 21, cy - 2); Zed(f, cx + 25, cy - 8); } });
		int fWake1 = P(new L { Sit = 1, Eyes = 4, Af = 4, Ab = 4 });
		int fYawn = P(new L { Eyes = 1, Mouth = 2, Squash = -2, Af = 1, Ab = 1, Still = true });
		int fStretch1 = P(new L { Eyes = 1, Mouth = 3, Squash = -3, Af = 1, Ab = 1, Still = true });
		int fStretch2 = P(new L { Eyes = 12, Mouth = 2, Squash = -4, Af = 1, Ab = 1, Up = 1, Feet = 0, Still = true });
		int fWave1 = P(new L { Eyes = 2, Mouth = 3, Af = 2, Still = true });
		int fWave2 = P(new L { Eyes = 2, Mouth = 8, Af = 1, Still = true });
		int fLookF1 = P(new L { Look = -1, Sway = -1 });
		int fLookB1 = P(new L { Look = 1, Sway = 1 });
		int fTalk1 = P(new L { Mouth = 1, Still = true });
		int fTalk2 = P(new L { Mouth = 3, Af = 2, Still = true });
		int fTalk3 = P(new L { Mouth = 0, Still = true });

		// ------------------------------------------------------------ the watering can and the flower
		// The can comes from the front and stops right over him (its window is then exactly on his); its water lands on
		// the top of his middle arm.
		int CanFrame(int k, bool tilt, int drops)
		{
			var f = Empty();
			int cx = AX, cy = AY;
			Can(f, cx - 2, cy - 14 + (k % 2), tilt);
			if (tilt) for (int i = 0; i < drops; i++) Drop(f, cx + 9 + (i % 2), cy - 6 + ((i * 4 + k * 3) % 7));
			return Add(f);
		}
		int fCanFly1 = CanFrame(0, false, 0), fCanFly2 = CanFrame(1, false, 0);
		var canPour = Enumerable.Range(0, 4).Select(k => CanFrame(k, true, 3)).ToArray();
		L Wet(int k) => new L { Eyes = 2, Mouth = k == 0 ? 3 : 8, Af = 1, Ab = 1, Sway = k == 0 ? 0 : 1, Front = (f, cx, cy) => { Drop(f, cx + (k == 0 ? 8 : 14), cy + (k == 0 ? 3 : 6)); Drop(f, cx + (k == 0 ? 18 : 6), cy + (k == 0 ? 9 : 11)); Spark(f, cx + 20, cy - 2 + k); } };
		int fWet1 = P(Wet(0)), fWet2 = P(Wet(1));
		L WithFlower(L l, int stage) { var old = l.Front; return l.With(x => x.Front = (f, cx, cy) => { Flower(f, cx, stage); old?.Invoke(f, cx, cy); }); }
		int fStrain = P(new L { Eyes = 12, Mouth = 5, Squash = 2, Af = 4, Ab = 4, ShortPot = true, Bob = -1, Feet = 2, Still = true });
		int fStrain2 = P(new L { Eyes = 12, Mouth = 5, Squash = 1, Af = 4, Ab = 4, Still = true, Bristle = 1 });
		int fBud = P(WithFlower(new L { Eyes = 3, Mouth = 1, Squash = -1, Still = true }, 1));
		int fBudLook = P(WithFlower(new L { LookY = -1, Mouth = 0, Still = true }, 1));
		int fHalf = P(WithFlower(new L { LookY = -1, Mouth = 1, Still = true }, 2));
		int fBloom = P(WithFlower(new L { Eyes = 3, Mouth = 2, Squash = -2, Af = 1, Ab = 1, Still = true, Front = (f, cx, cy) => { Spark(f, cx + 3, cy - 6); Spark(f, cx + 19, cy - 8); } }, 4));
		int fFlowerHappy = P(WithFlower(new L { Eyes = 2, Mouth = 3, Blush = true }, 3));
		int fFlowerSway1 = P(WithFlower(new L { Eyes = 2, Mouth = 3, Blush = true, Sway = -1 }, 3));
		int fFlowerSway2 = P(WithFlower(new L { Eyes = 2, Mouth = 3, Blush = true, Sway = 1 }, 3));
		int fFlowerProud = P(WithFlower(new L { Eyes = 2, Mouth = 8, Af = 1, Ab = 1, Blush = true, Front = (f, cx, cy) => HeartAt(f, cx - 2, cy + 1) }, 3));
		int fFlowerUp = P(WithFlower(new L { LookY = -1, Look = -1, Mouth = 3 }, 3));
		int fFlowerUpB = P(WithFlower(new L { LookY = -1, Look = 1, Mouth = 3 }, 3));
		int fFlowerGiggle1 = P(WithFlower(new L { Eyes = 12, Mouth = 8, Blush = true, Sway = -1, Still = true }, 3));
		int fFlowerGiggle2 = P(WithFlower(new L { Eyes = 12, Mouth = 8, Blush = true, Sway = 1, Still = true }, 3));
		int fFlowerWave1 = P(WithFlower(new L { Eyes = 2, Mouth = 3, Ab = 2, Still = true }, 3));
		int fFlowerWave2 = P(WithFlower(new L { Eyes = 2, Mouth = 3, Ab = 1, Still = true }, 3));
		int fFlowerClose1 = P(WithFlower(new L { Eyes = 4, Mouth = 0 }, 2));
		int fFlowerClose2 = P(WithFlower(new L { Eyes = 1, Mouth = 3 }, 1));
		// the bee: it hovers around the flower on his head, lands on it, and leaves
		(int x, int y)[] beePath = { (4, 4), (7, 2), (10, 1), (13, 2), (16, 1), (19, 2), (22, 3), (24, 5), (23, 7), (20, 7), (17, 6), (14, 5), (12, 6), (13, 7), (15, 7), (16, 8), (16, 8), (16, 8), (16, 8), (16, 8), (16, 8), (16, 8), (16, 8), (15, 7) };
		int BeeFrame(int x, int y, int k) { var f = Empty(); Bee(f, x, y, k); return Add(f); }
		int fBee1 = BeeFrame(4, 4, 0), fBee2 = BeeFrame(4, 4, 1);
		var beeVisit = beePath.Select((pt, i) => BeeFrame(pt.x, pt.y, i % 2)).ToArray();
		int fBeeAway1 = BeeFrame(15, 7, 0), fBeeAway2 = BeeFrame(15, 7, 1);
		// petals falling when the flower closes
		int PetalFrame(int k) { var f = Empty(); Petal(f, 15, 8, k); Petal(f, 20, 10, 1 - k); return Add(f); }
		int fPetal1 = PetalFrame(0), fPetal2 = PetalFrame(1);

		// ------------------------------------------------------------ the balloon
		// it drifts down to him (its window stops exactly on his), wobbles over his raised arm, touches a spine: pop
		int BalloonFrame(int k, int dy, bool heart = false) { var f = Empty(); Balloon(f, AX - 2, AY - 14 + dy, k, heart); return Add(f); }
		int fBal1 = BalloonFrame(0, 0), fBal2 = BalloonFrame(1, 0), fBal3 = BalloonFrame(0, 1), fBal4 = BalloonFrame(1, 2);
		int BurstFrame(int k) { var f = Empty(); if (k == 0) Burst(f, AX - 2, AY - 13, 'R'); else { foreach (var (sx, sy) in new[] { (AX - 3, AY - 12), (AX + 3, AY - 13), (AX, AY - 9), (AX + 5, AY - 8) }) St(f, sx, sy, "Rr", ".R"); } return Add(f); }
		int fPop1 = BurstFrame(0), fPop2 = BurstFrame(1);
		int fBalLook = P(new L { LookY = -1, Look = -1 });
		int fBalReach = P(new L { LookY = -1, Look = -1, Mouth = 3, Af = 2, Ab = 1, Still = true });
		int fBalHold = P(new L { Eyes = 2, Mouth = 8, Af = 2, Ab = 1, Blush = true, Still = true });
		int fPopShock = P(new L { Eyes = 3, Mouth = 2, Af = 1, Ab = 1, Squash = -2, Bristle = 1, Still = true });
		int fOops1 = P(new L { Eyes = 12, Mouth = 6, Af = 4, Ab = 0, Front = (f, cx, cy) => Sweat(f, cx + 19, cy + 1) });
		int fOops2 = P(new L { Eyes = 1, Mouth = 6, Af = 4, Ab = 0, Squash = 1, Front = (f, cx, cy) => Sweat(f, cx + 19, cy + 3) });
		int fLaugh1 = P(new L { Eyes = 12, Mouth = 8, Sway = -1, Still = true, Front = (f, cx, cy) => Bubble(f, 0, -8, kkk) });
		int fLaugh2 = P(new L { Eyes = 12, Mouth = 3, Sway = 1, Squash = 1, Still = true, Front = (f, cx, cy) => Bubble(f, 0, -8, kkk) });

		// ------------------------------------------------------------ sneezing his spines off
		int fAh1 = P(new L { Eyes = 4, Mouth = 1, Squash = -1, Sway = 1, Still = true });
		int fAh2 = P(new L { Eyes = 12, Mouth = 2, Squash = -2, Sway = 2, Still = true });
		int fChoo = P(new L { Eyes = 1, Mouth = 2, Squash = 2, Sway = -2, Bristle = 1, ShortPot = true, Bob = -1, Feet = 2, Still = true, Front = (f, cx, cy) => { f.Dot(cx - 2, cy + 8, 'a'); f.Dot(cx - 3, cy + 10, 'a'); f.Dot(cx - 1, cy + 11, 'a'); } });
		int fBald1 = P(new L { Eyes = 3, Mouth = 1, NoSpines = true, Still = true });
		int fBald2 = P(new L { Eyes = 3, Mouth = 1, NoSpines = true, LookY = 1, Still = true });
		int fBaldSad = P(new L { Eyes = 6, Mouth = 4, NoSpines = true, Af = 4, Ab = 4 });
		int fRegrow = P(new L { Eyes = 2, Mouth = 3, Bristle = 1, Front = (f, cx, cy) => { Spark(f, cx - 1, cy + 4); Spark(f, cx + 20, cy + 6); Spark(f, cx + 9, cy - 3); } });
		int SpineFrame(int k)
		{
			var f = Empty();
			foreach (var (sx, sy) in new[] { (AX - 2, AY + 5), (AX - 4, AY + 9), (AX - 1, AY + 12) }) St(f, sx - k, sy + (k > 0 ? (sx % 2) : 0), "sSSl");
			return Add(f);
		}
		int fSpines1 = SpineFrame(0), fSpines2 = SpineFrame(1);

		// ------------------------------------------------------------ tumbleweed
		int TumbleFrame(int rot, int lift) { var f = Empty(); Prop(f, 11, Ground - 12 - lift, 't', Tumble(rot)); return Add(f); }
		var tumbleRoll = new List<int>();
		int[] hop = { 0, 1, 2, 2, 1, 0 };
		int fDuck = P(new L { Eyes = 1, Mouth = 6, Sink = 4, Af = 5, Ab = 5, Still = true });
		int fDuck2 = P(new L { Eyes = 3, Mouth = 1, Sink = 3, Af = 5, Ab = 5, Look = 1, Still = true, Bristle = 1 });
		int fShrug = P(new L { Eyes = 2, Mouth = 5, Af = 3, Ab = 3, Still = true });
		int fShrug2 = P(new L { Eyes = 1, Mouth = 5, Af = 3, Ab = 3, Squash = 1, Still = true });
		int fScaredBack = P(new L { Eyes = 3, Mouth = 1, Look = 1, Bristle = 1 });
		int fWatchGo = P(new L { Look = -1, Mouth = 1 });

		// ------------------------------------------------------------ calango (lizard)
		int LizFrame(int k, int dx, bool right = true) { var f = Empty(); Lizard(f, 8 + dx, Ground - 6, k, right); return Add(f); }
		int fLiz1 = LizFrame(0, 0), fLiz2 = LizFrame(1, 0), fLizUp = LizFrame(2, 0), fLizL1 = LizFrame(0, 0, false), fLizL2 = LizFrame(1, 0, false);
		int fPush1 = P(new L { Look = -1, Mouth = 5, Squash = 2, ShortPot = true, Bob = -1, Feet = 2, Af = 3, Ab = 3, Still = true });
		int fPush2 = P(new L { Look = -1, Mouth = 3, Squash = -1, Af = 3, Ab = 3, Still = true });
		int fLizSee = P(new L { Look = -1, Eyes = 3, Mouth = 1 });

		// ------------------------------------------------------------ bird
		int BirdFrame(int k, int dy) { var f = Empty(); BirdAt(f, AX + 6, AY - 8 + dy, k); return Add(f); }
		int fBirdF1 = BirdFrame(1, -3), fBirdF2 = BirdFrame(2, -2), fBirdSit = BirdFrame(0, 0), fBirdLand1 = BirdFrame(1, -1), fBirdLand2 = BirdFrame(2, 0);
		int fBirdOuch = BirdFrame(3, -4), fBirdOuch2 = BirdFrame(1, -5);
		int BirdPeck(int dx) { var f = Empty(); BirdAt(f, AX + 6 + dx, AY - 8, 0); return Add(f); }
		int fBirdLookL = BirdPeck(-1), fBirdLookR = BirdPeck(1);
		int FeatherFrame() { var f = Empty(); St(f, AX + 4, AY - 10, "b.", ".B"); St(f, AX + 15, AY - 9, ".b", "B."); BirdAt(f, AX + 6, AY - 13, 1); return Add(f); }
		int fFeathers = FeatherFrame();
		int fBirdWatch = P(new L { LookY = -1, Mouth = 1 });
		int fBirdHappy = P(new L { Eyes = 2, Mouth = 3, Blush = true });
		int fBirdOuchMe = P(new L { Eyes = 12, Mouth = 6, Bristle = 1, Squash = 1, Still = true });
		int fSorry = P(new L { Eyes = 1, Mouth = 6, Af = 4, Ab = 4, LookY = -1, Front = (f, cx, cy) => Sweat(f, cx + 19, cy + 1) });

		// ------------------------------------------------------------ the pot cracks
		int fCrack1 = P(new L { Eyes = 3, Mouth = 1, LookY = 1, Crack = 1 });
		int fCrack2 = P(new L { Eyes = 6, Mouth = 4, LookY = 1, Crack = 2, Af = 4, Ab = 4 });
		int fCrack3 = P(new L { Eyes = 6, Mouth = 4, Crack = 2, Af = 4, Ab = 4, Squash = 1 });
		int fBandAid = P(new L { Eyes = 0, Mouth = 3, Crack = 2, Af = 3, Still = true, Front = (f, cx, cy) => Prop(f, cx - 4, cy + 7, 'K', "pppp", "pffp") });
		int fPatching = P(new L { Eyes = 12, Mouth = 5, Crack = 2, Af = 4, Sway = -2, Squash = 1, Still = true, Front = (f, cx, cy) => Prop(f, cx + 3, cy + 15, 'K', "pppp", "pffp") });
		int fPatched = P(new L { Eyes = 2, Mouth = 3, Crack = 3, Af = 1, Ab = 0 });
		int fPatchedShine = P(new L { Eyes = 2, Mouth = 8, Crack = 3, Af = 1, Ab = 1, Front = (f, cx, cy) => { Spark(f, cx + 3, cy + 15); Spark(f, cx + 16, cy + 18); } });
		int fGoodAsNew = P(new L { Eyes = 2, Mouth = 3, Front = (f, cx, cy) => { Spark(f, cx + 6, cy + 14); Spark(f, cx + 14, cy + 19); Spark(f, cx + 18, cy + 13); } });

		// ------------------------------------------------------------ rolling in his pot
		int fTip = P(new L { Eyes = 3, Mouth = 1, Sway = -2, Dx = -1, Bristle = 1, Still = true });
		var rollFrames = new int[4];
		var rollBase = Draw(new L { Eyes = 12, Mouth = 8, Af = 1, Ab = 1, Still = true });
		for (int q = 0; q < 4; q++) rollFrames[q] = Add(Rot(rollBase, q));
		int fDizzy1 = P(new L { Eyes = 7, Mouth = 6, Sway = -1, Front = (f, cx, cy) => Stars(f, cx, cy, 0) });
		int fDizzy2 = P(new L { Eyes = 8, Mouth = 6, Sway = 1, Front = (f, cx, cy) => Stars(f, cx, cy, 1) });
		int fDizzy3 = P(new L { Eyes = 7, Mouth = 1, Sway = 0, Front = (f, cx, cy) => Stars(f, cx, cy, 2) });

		// ------------------------------------------------------------ flower rain, butterfly, baby cactus, bubbles
		int PetalRain(int k) { var f = Empty(); foreach (var (sx, sy) in new[] { (3, -6), (14, -2), (25, -7), (8, 6), (30, 4), (19, 12), (2, 18), (27, 20) }) Petal(f, sx + (k % 2), ((sy + k * 3) + 50) % 34 - 8, (k + sx) % 2); return Add(f); }
		var petalRain = Enumerable.Range(0, 4).Select(PetalRain).ToArray();
		int fRainDance1 = P(new L { Eyes = 2, Mouth = 3, Af = 1, Ab = 2, Sway = -1, Fl = 1, Still = true });
		int fRainDance2 = P(new L { Eyes = 2, Mouth = 8, Af = 2, Ab = 1, Sway = 1, Bl = 1, Still = true });
		int ButterflyFrame(int k, int x, int y) { var f = Empty(); Butterfly(f, x, y, k); return Add(f); }
		int fBut1 = ButterflyFrame(0, AX + 1, AY), fBut2 = ButterflyFrame(1, AX + 1, AY + 1);
		int fButRest1 = ButterflyFrame(0, AX + 1, AY - 1 + 0), fButRest2 = ButterflyFrame(2, AX + 1, AY - 1);
		int fButWatch = P(new L { Look = -1, LookY = -1, Mouth = 1 });
		int fButCross = P(new L { Look = -1, LookY = -1, Mouth = 0, Blush = true, Still = true });
		int fButHappy = P(new L { Look = -1, Eyes = 2, Mouth = 3, Blush = true, Still = true });
		int fButWave1 = P(new L { Eyes = 2, Mouth = 3, Ab = 2, Still = true });
		int fButWave2 = P(new L { Eyes = 2, Mouth = 3, Ab = 1, Still = true });
		int BabyFrame(int stage, int k) { var f = Empty(); BabyCactus(f, 1, stage, k); if (stage == 3 && k == 2) { SmallHeart(f, 4, 18); } return Add(f); }
		int fBaby0 = BabyFrame(0, 0), fBaby1 = BabyFrame(1, 0), fBaby2 = BabyFrame(2, 0), fBaby3 = BabyFrame(3, 0), fBaby4 = BabyFrame(3, 1), fBaby5 = BabyFrame(3, 2);
		int fPlant1 = P(new L { Look = -1, LookY = 1, Mouth = 0, Sway = -2, Squash = 1, Af = 4, Still = true });
		int fPlant2b = P(new L { Look = -1, LookY = 1, Mouth = 3, Sway = -1, Af = 4, Still = true });
		int fBabyLove = P(new L { Look = -1, Eyes = 9, Mouth = 3, Blush = true });
		int fBabyWave1 = P(new L { Look = -1, Eyes = 2, Mouth = 3, Af = 2, Blush = true, Still = true });
		int fBabyWave2 = P(new L { Look = -1, Eyes = 2, Mouth = 8, Af = 1, Blush = true, Still = true });
		int BubbleFrame(int x, int y, int k) { var f = Empty(); if (k < 2) St(f, x, y, ".aa.", "a.wa", "a..a", ".aa."); else St(f, x - 1, y - 1, "a....a", "..a.a.", "......", ".a..a.", "a....a"); return Add(f); }
		var bubbleA = Enumerable.Range(0, 10).Select(i => BubbleFrame(AX + 1 - (i < 5 ? 5 - i : 0), AY - 4 + (i % 3 == 0 ? 0 : 1), 0)).ToArray();
		int fBubblePop = BubbleFrame(AX + 1, AY - 3, 2);
		int fPoke = P(new L { Look = -1, LookY = -1, Mouth = 3, Af = 2, Still = true });
		int fPokeWatch = P(new L { Look = -1, LookY = -1, Mouth = 1 });

		// ------------------------------------------------------------ dance and other moves
		int fDance1 = P(new L { Eyes = 2, Mouth = 3, Af = 1, Ab = 4, Sway = -2, Fl = 1, Bob = 0, Still = true });
		int fDance2 = P(new L { Eyes = 2, Mouth = 8, Af = 2, Ab = 2, Sway = 0, Bob = 1, Fl = 1, Bl = 1, Still = true });
		int fDance3 = P(new L { Eyes = 2, Mouth = 3, Af = 4, Ab = 1, Sway = 2, Bl = 1, Still = true });
		int fDance4 = P(new L { Eyes = 12, Mouth = 8, Af = 2, Ab = 2, Squash = 1, Still = true });
		int fSpinSide = P(new L { Af = 5, Ab = 5, Look = -1, Mouth = 3, Still = true });
		int fSpinBack = P(new L { Eyes = -1, Mouth = -1, Mirror = true, Still = true });
		int fSpinSide2 = P(new L { Af = 5, Ab = 5, Look = -1, Mouth = 3, Mirror = true, Still = true });
		int fHiccup = P(new L { Eyes = 3, Mouth = 1, Up = 2, Squash = -1, Feet = 4, Still = true });
		int fHiccup2 = P(new L { Eyes = 1, Mouth = 6, Still = true });
		int fComb1 = P(new L { Eyes = 1, Mouth = 3, Af = 1, Still = true, Front = (f, cx, cy) => Prop(f, cx + 4, cy - 2, 'K', "BBBBB", "B.B.B") });
		int fComb2 = P(new L { Eyes = 1, Mouth = 3, Af = 1, Sway = 1, Still = true, Front = (f, cx, cy) => Prop(f, cx + 5, cy + 1, 'K', "BBBBB", "B.B.B") });
		int fCombDone = P(new L { Eyes = 11, Mouth = 3, Bristle = 1, Front = (f, cx, cy) => { Spark(f, cx - 1, cy + 2); Spark(f, cx + 20, cy + 4); } });
		int fWhistle1 = P(new L { Eyes = 1, Mouth = 7, Sway = -1, Still = true, Front = (f, cx, cy) => NoteAt(f, cx + 2, cy + 2) });
		int fWhistle2 = P(new L { Eyes = 1, Mouth = 7, Sway = 1, Still = true, Front = (f, cx, cy) => NoteAt(f, cx + 1, cy - 1) });
		int fKick1 = P(new L { Look = -1, LookY = 1, Mouth = 5, Fx = -1, Fl = 1, Sway = 1, Still = true, Front = (f, cx, cy) => St(f, cx - 1, Ground - 1, ".k", "kk") });
		int fKick2 = P(new L { Look = -1, Mouth = 3, Fx = -3, Fl = 2, Bob = 1, Sway = -1, Still = true });
		int PebbleFrame(int k) { var f = Empty(); St(f, AX - 1, Ground - 1 - (k == 1 ? 1 : 0), k == 0 ? new[] { ".k", "kk" } : new[] { "kk", ".k" }); return Add(f); }
		int fPebble1 = PebbleFrame(0), fPebble2 = PebbleFrame(1);
		int fBoing1 = P(new L { Eyes = 2, Mouth = 8, Squash = -2, Up = 6, Feet = 4, Af = 1, Ab = 1, Still = true });
		int fBoing2 = P(new L { Eyes = 2, Mouth = 3, Squash = -1, Up = 3, Feet = 1, Af = 2, Ab = 2, Still = true });

		// ------------------------------------------------------------ the program's features
		var th1 = new L { LookY = -1, Look = 1, Mouth = 0, Af = 5, Front = (f, cx, cy) => ThoughtCloud(f, cx + 16, cy - 10, 0) };
		int fThink1 = P(th1);
		int fThink2 = P(th1.With(l => l.Front = (f, cx, cy) => ThoughtCloud(f, cx + 16, cy - 12, 1)));
		int fThink3 = P(th1.With(l => { l.Eyes = 1; l.Front = (f, cx, cy) => ThoughtCloud(f, cx + 16, cy - 12, 2); }));
		int fIdea1 = P(new L { Eyes = 3, LookY = -1, Mouth = 1, Front = (f, cx, cy) => Bulb(f, cx + 8, cy - 9, false) });
		int fIdea2 = P(new L { Eyes = 2, Mouth = 8, Af = 1, Ab = 1, Squash = -1, Still = true, Front = (f, cx, cy) => { Bulb(f, cx + 8, cy - 10, true); Rays(f, cx + 8, cy - 10); } });
		int fBulbChild; { var f = Empty(); Bulb(f, AX + 8, AY - 10, true); fBulbChild = Add(f); }
		int fRead1 = P(new L { LookY = 1, Mouth = 0, Af = 3, Ab = 0, Still = true, Front = (f, cx, cy) => { Glasses(f, cx, cy); Book(f, cx - 4, cy + 10); } });
		int fRead2 = P(new L { LookY = 1, Look = -1, Mouth = 0, Af = 3, Ab = 0, Still = true, Front = (f, cx, cy) => { Glasses(f, cx, cy); Book(f, cx - 4, cy + 10); } });
		int fRead3 = P(new L { LookY = 1, Mouth = 3, Af = 3, Ab = 0, Still = true, Front = (f, cx, cy) => { Glasses(f, cx, cy); Book(f, cx - 4, cy + 10); f.Rect(cx + 1, cy + 11, 3, 2, 'Q'); } });
		int fReview1 = P(new L { LookY = 1, Look = -1, Mouth = 5, Af = 3, Still = true, Front = (f, cx, cy) => { Glasses(f, cx, cy); Clipboard(f, cx - 6, cy + 4, 0); } });
		int fReview2 = P(new L { LookY = 1, Look = -1, Mouth = 0, Af = 3, Still = true, Front = (f, cx, cy) => { Glasses(f, cx, cy); Clipboard(f, cx - 6, cy + 4, 1); } });
		int fReview3 = P(new L { LookY = 1, Look = -1, Mouth = 3, Af = 3, Still = true, Front = (f, cx, cy) => { Glasses(f, cx, cy); Clipboard(f, cx - 6, cy + 4, 2); } });
		int fReview4 = P(new L { Eyes = 2, Mouth = 3, Af = 3, Still = true, Front = (f, cx, cy) => { Clipboard(f, cx - 6, cy + 4, 3); } });
		int fLap1 = P(new L { LookY = 1, Look = -1, Mouth = 0, Af = 4, Still = true, Front = (f, cx, cy) => Laptop(f, cx - 6, cy + 13, 0) });
		int fLap2 = P(new L { LookY = 1, Look = -1, Mouth = 5, Af = 5, Still = true, Front = (f, cx, cy) => Laptop(f, cx - 6, cy + 13, 1) });
		int fCode; { var f = Empty(); St(f, 2, 16, ".g...g.", "g..g..g", ".g.g.g.", "...g..."); fCode = Add(f); }
		int fWrite1 = P(new L { LookY = 1, Look = -1, Mouth = 0, Af = 4, Still = true, Front = (f, cx, cy) => { Paper(f, cx - 7, cy + 16, 1); Pencil(f, cx - 3, cy + 12); } });
		int fWrite2 = P(new L { LookY = 1, Look = -1, Mouth = 5, Af = 4, Still = true, Front = (f, cx, cy) => { Paper(f, cx - 7, cy + 16, 2); Pencil(f, cx - 4, cy + 13); } });
		int fWrite3 = P(new L { LookY = 1, Look = -1, Mouth = 3, Af = 4, Still = true, Front = (f, cx, cy) => { Paper(f, cx - 7, cy + 16, 3); Pencil(f, cx - 2, cy + 13); } });
		Frame Loading(int k) { var f = Draw(new L { LookY = -1, Look = k % 2 == 0 ? -1 : 1, Mouth = 0 }); LoadingRing(f, AX + 3, AY - 3, k); return f; }
		var loadF = Enumerable.Range(0, 4).Select(k => Add(Loading(k))).ToArray();
		int fDlLook = P(new L { LookY = -1, Mouth = 1, Af = 1, Ab = 1, Still = true });
		int fDlCatch = P(new L { Eyes = 2, Mouth = 3, Af = 1, Ab = 1, Squash = 1, Still = true, Front = (f, cx, cy) => Box(f, cx + 7, cy - 6) });
		int fDlHug = P(new L { Eyes = 2, Mouth = 8, Af = 3, Ab = 3, Blush = true, Still = true, Front = (f, cx, cy) => Box(f, cx - 6, cy + 5) });
		int fDlBox; { var f = Empty(); Box(f, AX + 7, AY - 8); fDlBox = Add(f); }
		int fDet1 = P(new L { Look = -1, LookY = 1, Mouth = 5, Af = 3, Still = true, Front = (f, cx, cy) => { var (tx, ty) = TopOf(f, cx); Deerstalker(f, tx, ty); Magnifier(f, cx - 6, cy + 4); } });
		int fDet2 = P(new L { Look = -1, LookY = 1, Mouth = 0, Af = 3, Sway = -1, Still = true, Front = (f, cx, cy) => { var (tx, ty) = TopOf(f, cx); Deerstalker(f, tx, ty); Magnifier(f, cx - 7, cy + 6); } });
		int fDet3 = P(new L { Eyes = 3, Mouth = 1, Af = 3, Still = true, Front = (f, cx, cy) => { var (tx, ty) = TopOf(f, cx); Deerstalker(f, tx, ty); Magnifier(f, cx - 5, cy + 3); Bubble(f, 0, -8, bang); } });
		int fTele1 = P(new L { Look = -1, LookY = -1, Mouth = 0, Af = 2, Still = true, Front = (f, cx, cy) => Telescope(f, cx - 6, cy + 5) });
		int fTele2 = P(new L { Look = -1, LookY = -1, Eyes = 11, Mouth = 3, Af = 2, Still = true, Front = (f, cx, cy) => Telescope(f, cx - 6, cy + 5) });
		int fTele3 = P(new L { Look = -1, LookY = -1, Eyes = 3, Mouth = 1, Af = 2, Still = true, Front = (f, cx, cy) => { Telescope(f, cx - 6, cy + 5); Spark(f, 1, -6); } });
		int StarFrame(int k) { var f = Empty(); St(f, 30 - k * 4, -8 + k * 2, "X..", ".XY", ".YY"); if (k > 0) for (int i = 1; i <= Math.Min(k, 3); i++) f.Dot(33 - k * 4 + i * 2, -9 + k * 2 - i, 'X'); return Add(f); }
		var shootStar = Enumerable.Range(0, 7).Select(StarFrame).ToArray();
		int fThumbs = P(new L { Eyes = 11, Mouth = 3, Af = 1, Still = true, Front = (f, cx, cy) => { Bubble(f, 0, -8, check); } });
		int fThumbs0 = P(new L { Eyes = 2, Mouth = 3, Af = 1, Still = true });
		int fConfPop = P(new L { Eyes = 2, Mouth = 8, Af = 1, Ab = 1, Squash = -2, Up = 2, Feet = 4, Still = true, Front = (f, cx, cy) => Confetti(f, 0) });
		int fConf1; { var f = Empty(); Confetti(f, 0); fConf1 = Add(f); }
		int fConf2; { var f = Empty(); Confetti(f, 1); fConf2 = Add(f); }
		int fFlag1 = P(new L { Eyes = 2, Mouth = 3, Af = 2, Still = true, Front = (f, cx, cy) => Flag(f, cx - 8, cy - 9, 0) });
		int fFlag2 = P(new L { Eyes = 2, Mouth = 8, Af = 2, Still = true, Front = (f, cx, cy) => Flag(f, cx - 8, cy - 9, 1) });
		int fError1 = P(new L { Eyes = 3, Mouth = 2, Af = 1, Ab = 1, Bristle = 1, Squash = -1, Still = true, Front = (f, cx, cy) => Warning(f, 2, -8) });
		int fWarnChild; { var f = Empty(); Warning(f, 2, -8); fWarnChild = Add(f); }
		int fErrShake1 = P(new L { Eyes = 3, Mouth = 6, Bristle = 1, Dx = -1, Still = true });
		int fErrShake2 = P(new L { Eyes = 3, Mouth = 6, Bristle = 1, Dx = 1, Still = true });
		int fCry1 = P(new L { Eyes = 6, Mouth = 4, Af = 4, Ab = 4, Still = true });
		int fCry2 = P(new L { Eyes = 1, Mouth = 2, Af = 4, Ab = 4, Squash = 1, Still = true, Front = (f, cx, cy) => { f.Dot(cx + 9, cy + 10, 'A'); f.Dot(cx + 13, cy + 11, 'A'); Drop(f, cx + 3, cy + 12); } });
		int fAngry1 = P(new L { Eyes = 5, Mouth = 4, Tint = '2', Bristle = 1, Dx = -1, Af = 3, Ab = 3, Still = true });
		int fAngry2 = P(new L { Eyes = 5, Mouth = 2, Tint = '2', Bristle = 1, Dx = 1, Af = 1, Ab = 1, Squash = 1, Still = true });
		int fSteam; { var f = Empty(); Steam(f, AX + 16, AY - 5); Steam(f, AX + 3, AY - 3); fSteam = Add(f); }
		int fShock1 = P(new L { Eyes = 3, Mouth = 2, Af = 1, Ab = 1, Squash = -3, Up = 3, Feet = 1, Bristle = 1, Still = true });
		int fShock2 = P(new L { Eyes = 3, Mouth = 1, Bristle = 1, Still = true });
		int fScared1 = P(new L { Eyes = 3, Mouth = 6, Bristle = 1, Dx = -1, Af = 5, Ab = 5, Still = true, Front = (f, cx, cy) => Sweat(f, cx + 19, cy + 1) });
		int fScared2 = P(new L { Eyes = 3, Mouth = 6, Bristle = 1, Dx = 1, Af = 5, Ab = 5, Still = true, Front = (f, cx, cy) => Sweat(f, cx + 20, cy + 2) });
		int fHide1 = P(new L { Eyes = 3, Mouth = -1, Sink = 6, Af = 5, Ab = 5, Still = true });
		int fHide2 = P(new L { Eyes = -1, Mouth = -1, NoCheeks = true, Sink = 9, Af = -1, Ab = -1, Still = true });
		int fPeek1 = P(new L { Eyes = 0, Mouth = -1, Sink = 7, Af = 5, Ab = 5, Look = -1, Still = true });
		int fPeek2 = P(new L { Eyes = 0, Mouth = -1, Sink = 7, Af = 5, Ab = 5, Look = 1, Still = true });
		int fUmb1 = P(new L { Eyes = 2, Mouth = 3, Af = 2, Still = true, Front = (f, cx, cy) => { Umbrella(f, cx - 5, cy - 7); Drop(f, 1, 8); Drop(f, 30, 4); } });
		int fUmb2 = P(new L { Eyes = 0, Mouth = 3, Af = 2, Sway = 1, Still = true, Front = (f, cx, cy) => { Umbrella(f, cx - 5, cy - 7); Drop(f, 2, 12); Drop(f, 31, 9); } });
		L Pud(L l, int w) { var old = l.Front; return l.With(x => x.Front = (f, cx, cy) => { f.Rect(cx + 1, Ground, w, 1, 'A'); f.Rect(cx + 2, Ground - 1, w - 2, 1, 'a'); old?.Invoke(f, cx, cy); }); }
		int fPud0 = P(Pud(new L { Eyes = 2, Mouth = 3, LookY = 1 }, 20));
		int fPudCrouch = P(Pud(new L { Eyes = 12, Mouth = 5, Squash = 2, ShortPot = true, Bob = -1, Feet = 2, Still = true }, 20));
		int fPudJump = P(Pud(new L { Eyes = 2, Mouth = 8, Squash = -2, Up = 6, Feet = 4, Af = 1, Ab = 1, Still = true }, 20));
		int fPudSplash = P(Pud(new L { Eyes = 12, Mouth = 8, Squash = 2, ShortPot = true, Bob = -1, Feet = 2, Af = 2, Ab = 2, Still = true, Front = (f, cx, cy) => { foreach (var (sx, sy) in new[] { (-2, 15), (1, 12), (20, 13), (23, 16) }) Drop(f, cx + sx, cy + sy); } }, 22));
		int fSplashChild; { var f = Empty(); foreach (var (sx, sy) in new[] { (4, 24), (7, 21), (2, 27) }) Drop(f, sx, sy); fSplashChild = Add(f); }
		int fHot1 = P(new L { Eyes = 4, Mouth = 1, Af = 4, Ab = 4, Sway = 1, Front = (f, cx, cy) => { Sun(f, 0, -8, 0); Sweat(f, cx + 19, cy + 1); } });
		int fHot2 = P(new L { Eyes = 4, Mouth = 7, Af = 4, Ab = 4, Sway = 0, Squash = 1, Front = (f, cx, cy) => { Sun(f, 0, -8, 1); Sweat(f, cx + 20, cy + 3); } });
		int fFan1 = P(new L { Eyes = 2, Mouth = 3, Af = 2, Still = true, Front = (f, cx, cy) => Fan(f, cx - 4, cy + 1, 0) });
		int fFan2 = P(new L { Eyes = 1, Mouth = 3, Af = 1, Still = true, Front = (f, cx, cy) => Fan(f, cx - 3, cy - 2, 1) });
		int fSunb1 = P(new L { Eyes = 10, Mouth = 3, Af = 1, Ab = 1, Sway = 1, Still = true });
		int fSunb2 = P(new L { Eyes = 10, Mouth = 8, Af = 1, Ab = 1, Sway = 1, Squash = 1, Still = true, Front = (f, cx, cy) => Spark(f, cx + 2, cy + 1) });
		int fSunPut = P(new L { Eyes = 2, Mouth = 3, LookY = -1 });
		int SunFrame(int k) { var f = Empty(); Sun(f, 1, -8, k); return Add(f); }
		int fSunC1 = SunFrame(0), fSunC2 = SunFrame(1);
		int fIce1 = P(new L { Eyes = 2, Mouth = 3, Af = 2, Still = true, Front = (f, cx, cy) => IceCream(f, cx - 3, cy - 2, 0) });
		int fIce2 = P(new L { Eyes = 1, Mouth = 7, Af = 2, Sway = -1, Still = true, Front = (f, cx, cy) => IceCream(f, cx - 3, cy - 2, 0) });
		int fIce3 = P(new L { Eyes = 1, Mouth = 7, Af = 2, Sway = -1, Still = true, Front = (f, cx, cy) => { IceCream(f, cx - 3, cy - 2, 1); f.Dot(cx - 1, cy + 4, 'f'); } });
		int fIce4 = P(new L { Eyes = 2, Mouth = 3, Af = 2, Blush = true, Still = true, Front = (f, cx, cy) => IceCream(f, cx - 3, cy - 2, 1) });
		int fCold1 = P(new L { Eyes = 1, Mouth = 6, Tint = '1', Af = 5, Ab = 5, Dx = -1, Still = true, Front = (f, cx, cy) => Snow(f, 0) });
		int fCold2 = P(new L { Eyes = 1, Mouth = 6, Tint = '1', Af = 5, Ab = 5, Dx = 1, Squash = 1, Still = true, Front = (f, cx, cy) => Snow(f, 1) });
		int fShiver1 = P(new L { Eyes = 3, Mouth = 6, Tint = '1', Bristle = 1, Dx = -1, Af = 4, Ab = 4, Still = true });
		int fShiver2 = P(new L { Eyes = 3, Mouth = 5, Tint = '1', Bristle = 1, Dx = 1, Af = 4, Ab = 4, Still = true });
		int fScarf1 = P(new L { Eyes = 2, Mouth = 3, Front = (f, cx, cy) => { Scarf(f, cx, cy); Snow(f, 0); } });
		int fScarf2 = P(new L { Eyes = 1, Mouth = 3, Sway = 1, Front = (f, cx, cy) => { Scarf(f, cx, cy); Snow(f, 1); } });
		int fSnowCap0 = P(new L { LookY = -1, Mouth = 1, Tint = '1' });
		int fSnowCap1 = P(new L { LookY = -1, Mouth = 3, Tint = '1', Front = (f, cx, cy) => SnowCap(f, cx, cy, 1) });
		int fSnowCap2 = P(new L { Eyes = 2, Mouth = 3, Tint = '1', Blush = true, Front = (f, cx, cy) => SnowCap(f, cx, cy, 2) });
		int fSnowShake1 = P(new L { Eyes = 12, Mouth = 8, Tint = '1', Dx = -1, Still = true, Front = (f, cx, cy) => { f.Dot(cx + 6, cy - 2, 'z'); f.Dot(cx + 15, cy - 3, 'z'); f.Dot(cx + 21, cy + 1, 'z'); } });
		int fSnowShake2 = P(new L { Eyes = 12, Mouth = 8, Dx = 1, Still = true, Front = (f, cx, cy) => { f.Dot(cx + 4, cy - 4, 'z'); f.Dot(cx + 17, cy - 5, 'z'); f.Dot(cx + 23, cy - 1, 'z'); } });
		int SnowFall(int k) { var f = Empty(); Snow(f, k); return Add(f); }
		int fSnowC1 = SnowFall(0), fSnowC2 = SnowFall(1);
		int CloudFrame(int k) { var f = Empty(); CloudAt(f, AX + 3, AY - 16, k); return Add(f); }
		var cloudF = Enumerable.Range(0, 3).Select(CloudFrame).ToArray();
		int fRainLook = P(new L { LookY = -1, Mouth = 3 });
		int fRainHappy = P(new L { Eyes = 2, Mouth = 8, Af = 1, Ab = 1, Blush = true, Still = true });
		int fGrow1 = P(new L { Eyes = 12, Mouth = 5, Squash = -2, Af = 1, Ab = 1, Still = true });
		int fGrow2 = P(new L { Eyes = 2, Mouth = 8, Squash = -4, Af = 1, Ab = 1, Still = true, Front = (f, cx, cy) => { Spark(f, cx + 3, cy - 5); Spark(f, cx + 18, cy - 7); } });
		int fGrow3 = P(new L { Eyes = 2, Mouth = 3, Squash = -3, Af = 1, Ab = 1, Still = true });
		int fSurprised = P(new L { Eyes = 3, Mouth = 1, Up = 2, Squash = -1, Feet = 4, Still = true, Front = (f, cx, cy) => Bubble(f, 0, -8, bang) });
		int fTired1 = P(new L { Eyes = 4, Mouth = 5, Af = 4, Ab = 4, Squash = 1, Sway = 1 });
		int fTired2 = P(new L { Eyes = 1, Mouth = 0, Af = 4, Ab = 4, Squash = 2, Sway = 2, Still = true });
		int fSleepy1 = P(new L { Eyes = 4, Mouth = 0, Af = 4, Ab = 4, Sway = -1 });
		int fSleepy2 = P(new L { Eyes = 1, Mouth = 1, Af = 4, Ab = 4, Sway = -2, Squash = 1, Still = true, Front = (f, cx, cy) => Zed(f, cx + 22, cy - 2) });
		int fDrink1 = P(new L { Eyes = 0, Mouth = 0, Af = 3, Still = true, Front = (f, cx, cy) => { Glass(f, cx - 6, cy + 4, 3); St(f, cx - 3, cy + 1, "..R", ".R.", "R..", "R.."); } });
		int fDrink2 = P(new L { Eyes = 1, Mouth = 0, Af = 3, Sway = -1, Still = true, Front = (f, cx, cy) => { Glass(f, cx - 6, cy + 4, 2); St(f, cx - 3, cy + 1, "RRRRRRRRRRRR", "R...........", "R..........."); } });
		int fDrink3 = P(new L { Eyes = 1, Mouth = 0, Af = 3, Sway = -1, Still = true, Front = (f, cx, cy) => { Glass(f, cx - 6, cy + 4, 0); St(f, cx - 3, cy + 1, "RRRRRRRRRRRR", "R...........", "R..........."); } });
		int fDrinkAh = P(new L { Eyes = 2, Mouth = 8, Af = 3, Blush = true, Still = true, Front = (f, cx, cy) => { Glass(f, cx - 6, cy + 4, 0); Spark(f, cx + 20, cy + 2); } });
		int fEat1 = P(new L { Eyes = 0, Mouth = 1, Af = 3, Still = true, Front = (f, cx, cy) => Melon(f, cx - 6, cy + 5, 0) });
		int fEat2 = P(new L { Eyes = 1, Mouth = 5, Af = 3, Still = true, Blush = true, Front = (f, cx, cy) => Melon(f, cx - 6, cy + 5, 1) });
		int fEat3 = P(new L { Eyes = 1, Mouth = 0, Af = 3, Squash = 1, Still = true, Blush = true, Front = (f, cx, cy) => Melon(f, cx - 6, cy + 5, 1) });
		int fEat4 = P(new L { Eyes = 0, Mouth = 1, Af = 3, Still = true, Front = (f, cx, cy) => Melon(f, cx - 6, cy + 5, 1) });
		int fEat5 = P(new L { Eyes = 1, Mouth = 5, Af = 3, Still = true, Blush = true, Front = (f, cx, cy) => Melon(f, cx - 6, cy + 5, 2) });
		int fEat6 = P(new L { Eyes = 2, Mouth = 8, Af = 3, Still = true, Blush = true, Front = (f, cx, cy) => Melon(f, cx - 6, cy + 5, 3) });
		int fCookie1 = P(new L { Eyes = 0, Mouth = 1, Af = 3, Still = true, Front = (f, cx, cy) => Cookie(f, cx - 5, cy + 6, 0) });
		int fCookie2 = P(new L { Eyes = 1, Mouth = 5, Af = 3, Still = true, Front = (f, cx, cy) => { Cookie(f, cx - 5, cy + 6, 1); Crumbs(f, cx + 2, cy + 13); } });
		int fCookie3 = P(new L { Eyes = 1, Mouth = 0, Af = 3, Squash = 1, Still = true, Front = (f, cx, cy) => { Cookie(f, cx - 5, cy + 6, 1); Crumbs(f, cx + 1, cy + 15); } });
		int fCookie4 = P(new L { Eyes = 1, Mouth = 5, Af = 3, Still = true, Front = (f, cx, cy) => { Cookie(f, cx - 5, cy + 6, 2); Crumbs(f, cx + 2, cy + 13); } });
		int fCookie5 = P(new L { Eyes = 2, Mouth = 3, Af = 0, Blush = true, Front = (f, cx, cy) => Crumbs(f, cx + 1, Ground - 3) });
		int fSweat1 = P(new L { Eyes = 4, Mouth = 6, Front = (f, cx, cy) => { Sweat(f, cx + 19, cy + 1); Sweat(f, cx - 1, cy + 4); } });
		int fSweat2 = P(new L { Eyes = 4, Mouth = 5, Squash = 1, Front = (f, cx, cy) => { Sweat(f, cx + 20, cy + 4); Sweat(f, cx - 2, cy + 7); } });
		int fNod1 = P(new L { Eyes = 2, Mouth = 3, Sway = -2, Squash = 1, Still = true });
		int fNod2 = P(new L { Eyes = 0, Mouth = 3, Sway = -1, Still = true });
		int fOk = P(new L { Eyes = 11, Mouth = 3, Af = 1, Still = true, Front = (f, cx, cy) => Bubble(f, 0, -8, okTxt) });
		int fCheer1 = P(new L { Eyes = 2, Mouth = 8, Af = 1, Ab = 1, Squash = -3, Up = 5, Feet = 4, Still = true });
		int fCheer2 = P(new L { Eyes = 2, Mouth = 8, Af = 2, Ab = 2, Squash = -1, Up = 2, Feet = 1, Still = true });
		int fParty1 = P(new L { Eyes = 2, Mouth = 3, Af = 1, Ab = 4, Sway = -1, Still = true, Front = (f, cx, cy) => { PartyHat(f, cx); St(f, cx - 6, cy + 5, "YYYRR", "...RR"); } });
		int fParty2 = P(new L { Eyes = 12, Mouth = 8, Af = 4, Ab = 1, Sway = 1, Bob = 1, Still = true, Front = (f, cx, cy) => { PartyHat(f, cx); St(f, cx - 10, cy + 6, "RRYYYYYY"); } });
		int fHappyJump = P(new L { Eyes = 2, Mouth = 8, Af = 2, Ab = 2, Squash = -1, Up = 3, Feet = 4, Still = true });
		int fHug1 = P(new L { Eyes = 2, Mouth = 3, Af = 3, Still = true, Front = (f, cx, cy) => Balloon(f, cx - 7, cy + 3, 0, true) });
		int fHug2 = P(new L { Eyes = 1, Mouth = 3, Af = 3, Squash = 1, Blush = true, Still = true, Front = (f, cx, cy) => Prop(f, cx - 6, cy + 6, 'K', ".HH.HH", "HhHHHH", ".HHHH.", "..HH..") });
		int fHugPop = P(new L { Eyes = 3, Mouth = 2, Af = 3, Bristle = 1, Squash = -1, Still = true, Front = (f, cx, cy) => Burst(f, cx - 6, cy + 4, 'H') });
		int fHugLove = P(new L { Eyes = 9, Mouth = 3, Blush = true, Front = (f, cx, cy) => { SmallHeart(f, cx - 2, cy + 3); SmallHeart(f, cx + 20, cy + 1); } });
		int fHugLove2 = P(new L { Eyes = 9, Mouth = 8, Blush = true, Sway = 1, Af = 1, Ab = 1, Front = (f, cx, cy) => { SmallHeart(f, cx - 1, cy + 1); SmallHeart(f, cx + 21, cy - 1); } });
		int HeartChild(int x, int y) { var f = Empty(); HeartAt(f, x, y); return Add(f); }
		int fHeartC1 = HeartChild(AX + 1, AY + 4), fHeartC2 = HeartChild(AX + 9, AY + 2), fHeartC3 = HeartChild(AX + 16, AY + 4);
		int fLove1 = P(new L { Eyes = 9, Mouth = 3, Blush = true, Sway = -1 });
		int fLove2 = P(new L { Eyes = 9, Mouth = 8, Blush = true, Sway = 1, Front = (f, cx, cy) => HeartAt(f, cx + 19, cy - 2) });
		int fHearts1 = P(new L { Eyes = 2, Mouth = 3, Blush = true, Af = 1, Ab = 1, Front = (f, cx, cy) => SmallHeart(f, cx + 1, cy) });
		int fHearts2 = P(new L { Eyes = 2, Mouth = 8, Blush = true, Af = 1, Ab = 1, Squash = 1, Front = (f, cx, cy) => SmallHeart(f, cx + 19, cy + 2) });
		int fSweep1 = P(new L { Look = -1, LookY = 1, Mouth = 0, Af = 4, Sway = -1, Still = true, Front = (f, cx, cy) => Broom(f, 0, 0) });
		int fSweep2 = P(new L { Look = -1, LookY = 1, Mouth = 3, Af = 4, Sway = 0, Still = true, Front = (f, cx, cy) => { Broom(f, 0, 2); Dust(f, 0, Ground - 3); } });
		int fDustC; { var f = Empty(); Dust(f, 1, Ground - 3); fDustC = Add(f); }
		int fWork1 = P(new L { Look = -1, LookY = 1, Mouth = 5, Af = 1, Still = true, Front = (f, cx, cy) => { Hammer(f, cx + 1, cy - 1, true); f.Rect(0, Ground - 1, 9, 2, 'b'); f.Rect(0, Ground - 1, 9, 1, 'n'); } });
		int fWork2 = P(new L { Look = -1, LookY = 1, Eyes = 12, Mouth = 8, Af = 4, Sway = -1, Still = true, Front = (f, cx, cy) => { Hammer(f, cx - 2, Ground - 3, false); f.Rect(0, Ground - 1, 9, 2, 'b'); f.Rect(0, Ground - 1, 9, 1, 'n'); Spark(f, 1, Ground - 5); } });
		int fMusic1 = P(new L { Eyes = 1, Mouth = 3, Sway = -1, Fl = 1, Still = true, Front = (f, cx, cy) => Headphones(f, cx, cy) });
		int fMusic2 = P(new L { Eyes = 1, Mouth = 8, Sway = 1, Bl = 1, Bob = 1, Still = true, Front = (f, cx, cy) => Headphones(f, cx, cy) });
		int fNoteC; { var f = Empty(); NoteAt(f, AX + 3, AY + 2); fNoteC = Add(f); }
		int fCoffee1 = P(new L { Eyes = 0, Mouth = 0, Af = 3, Still = true, Front = (f, cx, cy) => Cup(f, cx - 5, cy + 5) });
		int fCoffee2 = P(new L { Eyes = 1, Mouth = 7, Af = 3, Sway = -1, Still = true, Front = (f, cx, cy) => Cup(f, cx - 5, cy + 5) });
		int fCoffee3 = P(new L { Eyes = 2, Mouth = 3, Af = 3, Blush = true, Still = true, Front = (f, cx, cy) => { Cup(f, cx - 5, cy + 5); Spark(f, cx + 20, cy + 1); } });
		int fSteamC; { var f = Empty(); St(f, AX - 3, AY + 1, ".k.k", "k.k.", ".k.k"); fSteamC = Add(f); }
		int fHello1 = P(new L { Eyes = 2, Mouth = 8, Af = 2, Still = true, Front = (f, cx, cy) => Bubble(f, 0, -8, oi) });
		int fHello2 = P(new L { Eyes = 2, Mouth = 3, Af = 1, Still = true, Front = (f, cx, cy) => Bubble(f, 0, -8, oi) });
		int fCurious = P(new L { Look = -1, LookY = -1, Sway = 1, Front = (f, cx, cy) => Bubble(f, 0, -8, question) });
		int fCurious2 = P(new L { Look = -1, Sway = -1, Mouth = 1 });
		int fMusicBubble = P(new L { Eyes = 1, Mouth = 3, Front = (f, cx, cy) => { Headphones(f, cx, cy); Bubble(f, 0, -8, music); } });

		p.IconFrame = fHappy;

		// ------------------------------------------------------------ animations
		Anim Show(int id, string name, int ms, params (int f, int n)[] steps)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString());
			foreach (var (f, n) in steps) a.Seq(Hold(f, n));
			a.Then(60, 19).Then(40, 11);
			return a;
		}
		Anim Loop2(int id, string name, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move("0", "0").Speed(ms.ToString()).Loop(loops, 0);
			a.Seq(frames); a.Then(60, 19).Then(40, 11);
			return a;
		}
		Anim Child(int id, string name, string mx, string my, int ms, string loops, params int[] frames)
		{
			var a = p.A(id, name).Move(mx, my).Speed(ms.ToString()).Loop(loops, 0); a.Seq(frames); return a;
		}
		Anim Then(Anim a, int next) { a.Next.Clear(); a.Then(100, next); return a; }
		int[] Rep(int n, params int[] frames) => Enumerable.Range(0, n).SelectMany(_ => frames).ToArray();
		int[] Wait(int n) => Enumerable.Repeat(fNone, n).ToArray();
		// an x/y offset of a child window, in art pixels (one art pixel is imageW/36 or imageH/44 on screen)
		string Ox(int px) => px == 0 ? "imageX" : px < 0 ? $"imageX-imageW*{-px}/{W}" : $"imageX+imageW*{px}/{W}";
		string Oy(int px) => px == 0 ? "imageY" : px < 0 ? $"imageY-imageH*{-px}/{W + Head}" : $"imageY+imageH*{px}/{W + Head}";

		p.Spawns.Add((1, 50, "screenW+10", "areaH-imageH", 1));
		p.Spawns.Add((2, 30, "random*(screenW-imageW-50)/100+25", "-imageH-20", 5));
		p.Spawns.Add((3, 20, "0", "areaH-imageH", 1));

		// ---- moving around (he never climbs: at every border he turns around)
		var walk = p.A(1, "walk").Move("-2", "0").Speed("85").Loop("random/5+3").Seq(walkF);
		walk.Then(40, 1).Then(42, 12).Then(4, 18).Then(3, 9).Then(5, 16, "taskbar")
			.OnBorder(100, 2).OnGravity(100, 5);
		var turn = p.A(2, "turn").Move("0", "0").Speed("110").Seq(fFwd, fStand, fTurn1, fTurn2); turn.Action = "flip"; turn.Then(100, 19);
		p.A(3, "drag").Move("0", "0").Speed("90").Loop("40").Seq(fDrag1, fDrag1, fDrag2, fDrag2, fDrag3, fDrag3).Then(100, 3);
		p.A(4, "toss").Move("0", "0").Speed("50").Loop("30").Seq(fDrag1, fDrag3, fDrag2, fDrag3).Then(100, 4);
		p.A(5, "fall").Move("0", "2", "0", "8").Speed("80", "50").Loop("20").Seq(fFall1, fFall2).Then(100, 6).OnBorder(100, 7);
		p.A(6, "fall fast").Move("0", "9").Speed("45").Loop("10").Seq(fFall1, fFall2).Then(100, 6).OnBorder(100, 8);
		p.A(7, "land").Move("0", "0").Speed("90", "170").Seq(fLand1, fLand2, fLand3, fStand, fHappy, fHappy, fStand).Then(70, 19).Then(30, 11);
		p.A(8, "fall hard").Move("0", "0").Speed("140", "240").Seq(fLand1, fHard1, fHard2, fHard3, fHard1, fHard2, fHard3, fDizzy1, fDizzy2, fBlink, fStand).Then(50, 30).Then(50, 11);
		p.A(9, "hop up").Move("-3", "-8", "-3", "8").Speed("70").Seq(fJumpPrep, fJumpUp, fJumpTop, fJumpTop, fJumpDown, fJumpDown, fLand2, fStand)
			.Then(100, 1).OnBorder(80, 7, "taskbar").OnBorder(40, 7, "window").OnBorder(20, 5, "horizontal+").OnBorder(100, 2, "vertical");
		Loop2(10, "sync", 120, "6", fDance1, fDance2, fDance3, fDance2);
		var hub = p.A(11, "blink").Move("0", "0").Speed("240", "200").Seq(fStand, fStand, Breath(fStand), Breath(fStand), fStand, fBlink, fStand, fStand);
		var stop = p.A(12, "stop walking").Move("-2", "0", "0", "0").Speed("85", "120").Seq(walkF[4], walkF[5], walkF[6], walkF[7], fPlant);
		stop.Then(100, 20).OnBorder(100, 2).OnGravity(100, 5);
		p.A(13, "sleep").Move("0", "0").Speed("600", "500").Loop("random/4+5", 3).Seq(fCrouch1, fSitDown, fSitBlink, fSleepA, fSleepA, fSleepB, fSleepC, fSleepB).Then(100, 14);
		p.Childs.Add((13, Ox(0), Oy(0), 290));
		Child(290, "z floats", "1", "-2", 260, "12", fNone, fNone, fNone).Then(100, 291);
		var zf = Empty(); Zed(zf, AX + 22, AY - 4); int fZc = Add(zf);
		Child(291, "z floats up", "1", "-2", 200, "14", fZc).Fade(1, 0);
		Show(14, "wake up", 200, (fSleepA, 2), (fWake1, 3), (fSitBlink, 1), (fWake1, 2), (fStretch1, 3), (fStretch2, 4), (fYawn, 3), (fBlink, 1), (fHappy, 3), (fStand, 1));
		Show(15, "look around", 230, (fStand, 1), (fFwd, 1), (fLookF1, 5), (fFwd, 1), (fStand, 1), (fBack, 1), (fLookB1, 5), (fBack, 1), (fStand, 2), (fBlink, 1), (fStand, 1));
		var sit = p.A(16, "sit").Move("0", "0").Speed("400", "300").Loop("random/3+4", 3).Seq(fCrouch1, fSitDown, fSit, fSit, fSit, fSitBlink, fSit, Breath(fSit));
		sit.Then(65, 21).Then(35, 13);
		Show(21, "stand up", 140, (fSit, 1), (fCrouch1, 1), (fLand3, 1), (fStand, 2), (fHappy, 2));
		Show(17, "wave", 160, (fStand, 1), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fWave2, 2), (fHappy, 3));
		var run = p.A(18, "run").Move("-4", "0").Speed("60").Loop("random/4+8").Seq(runF);
		run.Then(100, 12).OnBorder(100, 2).OnGravity(100, 5);
		var start = p.A(19, "start walking").Move("0", "0", "-2", "0").Speed("140", "85").Seq(fLeanBack, walkF[0], walkF[1], walkF[2], walkF[3]);
		start.Then(100, 1).OnBorder(100, 2).OnGravity(100, 5);
		var settle = p.A(20, "settle").Move("0", "0").Speed("100", "140").Seq(fPlant2, fPlant, fStand, fPlant3, fStand);
		settle.Then(100, 11);

		// ================================================================ cactus scenes (most with a child window)
		// 22 watering can → 23 bloom → 24 flower (→ 25 bee) → 26 flower closes
		Show(22, "watering can", 150, (fStand, 2), (fUp, 4), (fUpHappy, 2), (fWet1, 3), (fWet2, 3), (fWet1, 3), (fWet2, 3), (fWet1, 3), (fWet2, 3));
		Then(p.Anims.Last(), 23);
		Child(200, "can arrives", "4", "3", 150, "0", Rep(4, fCanFly1, fCanFly2)).Then(100, 201);
		Child(201, "can pours", "0", "0", 150, "0", Rep(4, canPour[0], canPour[1], canPour[2], canPour[3]).Concat(new[] { canPour[0], canPour[1] }).ToArray()).Then(100, 202);
		Child(202, "can leaves", "-4", "-3", 120, "0", Rep(6, fCanFly1, fCanFly2)).Fade(1, 0);
		p.Childs.Add((22, Ox(-16), Oy(-12), 200));
		Show(23, "bloom", 140, (fStrain, 3), (fStrain2, 2), (fStrain, 3), (fBud, 3), (fBudLook, 4), (fHalf, 4), (fStrain2, 1), (fBloom, 5), (fFlowerHappy, 4));
		Then(p.Anims.Last(), 24);
		var flower = Show(24, "flower", 200, (fFlowerHappy, 2), (fFlowerSway1, 2), (fFlowerHappy, 1), (fFlowerSway2, 2), (fFlowerHappy, 1), (fFlowerProud, 5), (fFlowerSway1, 2), (fFlowerHappy, 1), (fFlowerSway2, 2), (fFlowerHappy, 2));
		flower.Next.Clear(); flower.Then(55, 25).Then(45, 26);
		// the bee: comes from the front, circles the flower, lands on it (he giggles), and flies away
		Show(25, "bee", 120, (fFlowerUp, 12), (fFlowerUp, 4), (fFlowerUpB, 6), (fFlowerUp, 4), (fFlowerGiggle1, 2), (fFlowerGiggle2, 2), (fFlowerGiggle1, 2), (fFlowerGiggle2, 2), (fFlowerGiggle1, 2), (fFlowerHappy, 2), (fFlowerWave1, 2), (fFlowerWave2, 2), (fFlowerWave1, 2), (fFlowerWave2, 2), (fFlowerHappy, 2));
		Then(p.Anims.Last(), 26);
		Child(203, "bee arrives", "4", "2", 120, "0", Rep(6, fBee1, fBee2)).Then(100, 204);
		Child(204, "bee visits", "0", "0", 120, "0", beeVisit).Then(100, 205);
		Child(205, "bee leaves", "-5", "-3", 110, "0", Rep(10, fBeeAway1, fBeeAway2)).Fade(1, 0.2);
		p.Childs.Add((25, Ox(-24), Oy(-12), 203));
		Show(26, "flower closes", 200, (fFlowerHappy, 2), (fFlowerClose1, 3), (fFlowerClose2, 3), (fBlink, 1), (fHappy, 3));
		Child(206, "petals fall", "-1", "2", 160, "0", Rep(6, fPetal1, fPetal2)).Fade(1, 0);
		p.Childs.Add((26, Ox(0), Oy(0), 206));

		// 27 balloon: it drifts down to him, he reaches for it, it touches a spine: POP
		Show(27, "balloon", 130, (fStand, 2), (fBalLook, 10), (fBalReach, 8), (fBalHold, 10), (fPopShock, 4), (fOops1, 4), (fOops2, 4), (fLaugh1, 2), (fLaugh2, 2), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		Child(207, "balloon drifts in", "1", "2", 130, "0", Rep(10, fBal1, fBal2)).Then(100, 208);
		Child(208, "balloon wobbles", "0", "0", 130, "0", fBal1, fBal2, fBal1, fBal2, fBal3, fBal3, fBal4, fBal4, fBal4, fBal4).Then(100, 209);
		Child(209, "balloon pops", "0", "0", 90, "0", fPop1, fPop1, fPop2, fPop2).Then(100, 210);
		Child(210, "balloon pieces fall", "0", "3", 110, "0", Rep(5, fPop2)).Fade(1, 0);
		p.Childs.Add((27, Ox(-10), Oy(-20), 207));

		// 28 sneeze: his spines fly off... then grow back
		Show(28, "sneeze", 120, (fStand, 2), (fAh1, 4), (fStand, 1), (fAh1, 2), (fAh2, 5), (fChoo, 3), (fBald1, 6), (fBald2, 4), (fBaldSad, 6), (fRegrow, 5), (fHappy, 2));
		Child(211, "spines wait", "0", "0", 120, "0", Wait(14)).Then(100, 212);
		Child(212, "spines fly", "-8", "-1", 60, "0", Rep(8, fSpines1, fSpines2)).Then(100, 213);
		Child(213, "spines fall", "-3", "4", 70, "0", Rep(5, fSpines2, fSpines1)).Fade(1, 0);
		p.Childs.Add((28, Ox(0), Oy(0), 211));

		// 29 tumbleweed: it rolls in from behind and bounces right over him (he ducks into his pot)
		{
			int i = 0;
			// 36 steps to reach him (6 px a step = 3 art px; it starts 108 art px behind him), bouncing; the big bounce over him; then away
			for (; i < 26; i++) tumbleRoll.Add(TumbleFrame(3 - i % 4, hop[i % 6] * 2));
			int[] big = { 4, 9, 13, 16, 18, 19, 19, 18, 16, 13, 9, 4 };
			foreach (int b in big) tumbleRoll.Add(TumbleFrame(3 - (i++) % 4, b));
			for (int k = 0; k < 30; k++, i++) tumbleRoll.Add(TumbleFrame(3 - i % 4, hop[i % 6] * 2));
		}
		Show(29, "tumbleweed", 140, (fStand, 2), (fBack, 5), (fScaredBack, 6), (fDuck2, 2), (fDuck, 5), (fDuck2, 2), (fWatchGo, 6), (fShrug, 3), (fShrug2, 2), (fShrug, 3), (fStand, 1));
		Child(214, "tumbleweed rolls", "-6", "0", 80, "0", tumbleRoll.ToArray()).Fade(1, 0.6);
		p.Childs.Add((29, Ox(95), Oy(0), 214));

		// 30 the pot cracks → 31 he patches it with a band-aid
		Show(30, "crack pot", 170, (fStand, 1), (fCrack1, 5), (fCrack2, 5), (fCrack3, 4));
		Then(p.Anims.Last(), 31);
		Show(31, "patch pot", 160, (fCrack2, 2), (fBandAid, 5), (fPatching, 4), (fCrack2, 0), (fPatched, 4), (fPatchedShine, 4), (fGoodAsNew, 4), (fHappy, 2));

		// 32 calango: a lizard runs up to him and does push-ups; he does push-ups too; it runs away
		Show(32, "calango", 150, (fStand, 2), (fFwd, 4), (fLizSee, 6), (fPush1, 2), (fPush2, 2), (fPush1, 2), (fPush2, 2), (fPush1, 2), (fPush2, 2), (fPush1, 2), (fPush2, 2), (fFwd, 4), (fWave1, 2), (fWave2, 2), (fWave1, 2), (fHappy, 2));
		Child(215, "calango arrives", "6", "0", 75, "0", Rep(12, fLiz1, fLiz2)).Then(100, 216);
		Child(216, "calango push-ups", "0", "0", 150, "0", Rep(4, fLiz1, fLiz1, fLizUp, fLizUp)).Then(100, 217);
		Child(217, "calango runs away", "-7", "0", 60, "0", Rep(20, fLizL1, fLizL2));
		p.Childs.Add((32, Ox(-36 - 72), Oy(0), 215));

		// 33 bird: it lands on his head, gets pricked by a spine, and flies off in a hurry
		Show(33, "bird", 130, (fStand, 2), (fBirdWatch, 10), (fBirdHappy, 8), (fBirdHappy, 4), (fBirdOuchMe, 3), (fSorry, 6), (fWatchGo, 4), (fOops2, 2), (fStand, 1));
		Child(218, "bird flies in", "5", "3", 130, "0", Rep(6, fBirdF1, fBirdF2)).Then(100, 219);
		Child(219, "bird lands", "0", "0", 130, "0", fBirdLand1, fBirdLand2, fBirdSit, fBirdSit, fBirdLookL, fBirdLookL, fBirdSit, fBirdLookR, fBirdLookR, fBirdSit, fBirdSit, fBirdSit).Then(100, 220);
		Child(220, "bird pricked", "0", "0", 100, "0", fBirdOuch, fFeathers, fBirdOuch2, fFeathers).Then(100, 221);
		Child(221, "bird flies off", "-6", "-5", 90, "0", Rep(12, fBirdF1, fBirdF2)).Fade(1, 0.4);
		p.Childs.Add((33, Ox(-30), Oy(-18), 218));

		// 34 roll: he tips over and rolls in his pot, then stands up dizzy
		var roll = p.A(34, "roll").Move("-5", "0").Speed("75").Loop("3", 1).Seq(fTip, rollFrames[1], rollFrames[2], rollFrames[3], rollFrames[0]);
		roll.Then(100, 35).OnBorder(100, 35).OnGravity(100, 5);
		Show(35, "dizzy roll", 150, (fLand2, 1), (fDizzy1, 2), (fDizzy2, 2), (fDizzy3, 2), (fDizzy1, 2), (fDizzy2, 2), (fBlink, 1), (fHappy, 2));

		// 36 flower rain: petals fall all around him and he dances
		Show(36, "flower rain", 150, (fUp, 3), (fHappyUp, 3), (fRainDance1, 2), (fRainDance2, 2), (fRainDance1, 2), (fRainDance2, 2), (fRainDance1, 2), (fRainDance2, 2), (fRainDance1, 2), (fRainDance2, 2), (fHappy, 3));
		Child(222, "petals rain", "0", "1", 150, "6", petalRain).Fade(1, 0.3);
		Child(223, "petals rain 2", "1", "1", 170, "5", petalRain[2], petalRain[3], petalRain[0], petalRain[1]).Fade(1, 0.3);
		p.Childs.Add((36, Ox(-18), Oy(-10), 222));
		p.Childs.Add((36, Ox(16), Oy(-14), 223));

		// 37 butterfly: it flutters in and rests on his front arm; he holds very still
		Show(37, "butterfly", 130, (fStand, 2), (fButWatch, 12), (fButCross, 18), (fButHappy, 6), (fButWave1, 2), (fButWave2, 2), (fButWave1, 2), (fButWave2, 2), (fHappy, 2));
		Child(224, "butterfly arrives", "4", "1", 130, "0", Rep(7, fBut1, fBut2)).Then(100, 225);
		Child(225, "butterfly rests", "0", "0", 260, "0", fButRest1, fButRest1, fButRest2, fButRest2, fButRest1, fButRest1, fButRest2, fButRest1, fButRest2, fButRest2).Then(100, 226);
		Child(226, "butterfly leaves", "-3", "-4", 120, "0", Rep(10, fBut1, fBut2)).Fade(1, 0.3);
		p.Childs.Add((37, Ox(-28), Oy(-7), 224));

		// 38 baby cactus: he plants a seed and a tiny cactus grows in front of him
		Show(38, "baby cactus", 160, (fStand, 1), (fPlant1, 4), (fPlant2b, 3), (fPlant1, 3), (fFwd, 6), (fBabyLove, 8), (fBabyWave1, 2), (fBabyWave2, 2), (fBabyWave1, 2), (fBabyWave2, 2), (fBabyLove, 4), (fHappy, 2));
		Child(227, "baby cactus grows", "0", "0", 160, "0", Wait(4).Concat(new[] { fBaby0, fBaby0, fBaby0, fBaby1, fBaby1, fBaby1, fBaby2, fBaby2, fBaby2, fBaby3, fBaby3, fBaby3, fBaby3 }).ToArray()).Then(100, 228);
		Child(228, "baby cactus waves", "0", "0", 160, "0", Rep(5, fBaby4, fBaby3).Concat(new[] { fBaby5, fBaby5, fBaby5, fBaby5 }).ToArray()).Then(100, 229);
		Child(229, "baby cactus fades", "0", "0", 160, "0", fBaby3, fBaby3, fBaby3, fBaby3, fBaby3, fBaby3).Fade(1, 0);
		p.Childs.Add((38, Ox(-12), Oy(0), 227));

		// 39 bubbles: a soap bubble floats to him and he pops it with a spine
		Show(39, "bubbles", 130, (fStand, 1), (fPokeWatch, 9), (fPoke, 2), (fWide, 2), (fLaugh1, 2), (fLaugh2, 2), (fPokeWatch, 6), (fPoke, 2), (fHappy, 4));
		Child(230, "bubble floats", "1", "0", 130, "0", bubbleA.Concat(new[] { bubbleA[9], bubbleA[9] }).ToArray()).Then(100, 231);
		Child(231, "bubble pops", "0", "0", 130, "0", fBubblePop, fBubblePop).Fade(1, 0.2);
		Child(232, "bubble wait", "0", "0", 130, "0", Wait(13)).Then(100, 233);
		Child(233, "bubble floats 2", "1", "0", 130, "0", bubbleA.Concat(new[] { bubbleA[9] }).ToArray()).Then(100, 231);
		p.Childs.Add((39, Ox(-6), Oy(0), 230));
		p.Childs.Add((39, Ox(-6), Oy(1), 232));

		// 40-49 more cactus moments
		Loop2(40, "dance", 130, "random/3+4", fDance1, fDance2, fDance3, fDance2, fDance4, fDance2);
		Loop2(41, "laugh", 140, "random/3+3", fLaugh1, fLaugh1, fLaugh2, fLaugh2);
		Show(42, "yawn", 200, (fStand, 1), (fStretch1, 2), (fYawn, 6), (fBlink, 2), (fStand, 1));
		Show(43, "stretch", 180, (fStand, 1), (fCrouch1, 2), (fStretch1, 3), (fStretch2, 5), (fStretch1, 2), (fStand, 1), (fHappy, 2));
		Show(44, "hiccup", 140, (fStand, 3), (fHiccup, 2), (fHiccup2, 4), (fStand, 3), (fHiccup, 2), (fHiccup2, 4), (fStand, 2), (fHiccup, 2), (fHiccup2, 3), (fOops2, 3));
		Show(45, "peek", 180, (fHide2, 6), (fPeek1, 4), (fHide2, 2), (fPeek2, 4), (fPeek1, 3), (fHide1, 2), (fLand3, 2), (fStand, 1));
		Show(46, "comb spines", 160, (fStand, 1), (fComb1, 2), (fComb2, 2), (fComb1, 2), (fComb2, 2), (fComb1, 2), (fComb2, 2), (fCombDone, 6));
		Show(47, "whistle", 220, (fStand, 1), (fWhistle1, 2), (fWhistle2, 2), (fWhistle1, 2), (fWhistle2, 2), (fWhistle1, 2), (fWhistle2, 2), (fSmile, 2));
		Show(48, "kick pebble", 140, (fFwd, 4), (fKick1, 3), (fKick2, 3), (fWatchGo, 8), (fHappy, 2));
		Child(234, "pebble rolls", "-5", "0", 70, "0", Wait(7).Concat(Rep(10, fPebble1, fPebble2)).ToArray()).Fade(1, 0.4);
		p.Childs.Add((48, Ox(0), Oy(0), 234));
		Loop2(49, "boing", 110, "random/3+3", fJumpPrep, fBoing2, fBoing1, fBoing1, fBoing2, fLand2);
		Loop2(50, "spin", 90, "random/3+3", fStand, fSpinSide, fSpinBack, fSpinSide2);

		// ================================================================ the program's features (60-130)
		Loop2(60, "think", 300, "random/3+3", fThink1, fThink1, fThink2, fThink2, fThink3, fThink3);
		Loop2(61, "review", 280, "random/3+2", fReview1, fReview1, fReview2, fReview2, fReview3, fReview3, fReview4, fReview4);
		Show(62, "idea", 150, (fThink1, 3), (fThink2, 3), (fIdea1, 3), (fIdea2, 1), (fIdea1, 1), (fIdea2, 6), (fHappy, 3));
		Child(235, "idea bulb", "0", "-2", 90, "0", Wait(17).Concat(Rep(14, fBulbChild)).ToArray()).Fade(1, 0);
		p.Childs.Add((62, Ox(0), Oy(0), 235));
		Loop2(63, "read", 380, "random/3+3", fRead1, fRead1, fRead2, fRead2, fRead1, fRead3);
		Loop2(64, "laptop", 140, "random/3+6", fLap1, fLap2);
		Child(236, "code floats", "-1", "-2", 110, "16", fCode).Fade(1, 0);
		p.Childs.Add((64, Ox(0), Oy(0), 236));
		Loop2(65, "write", 160, "random/3+5", fWrite1, fWrite2, fWrite3, fWrite2);
		Loop2(66, "loading", 140, "random/3+5", loadF);
		Show(67, "download", 150, (fDlLook, 7), (fDlCatch, 4), (fDlHug, 6), (fHappy, 3));
		Child(237, "download box", "0", "6", 60, "17", fDlBox);
		p.Childs.Add((67, Ox(0), Oy(-51), 237));
		Loop2(68, "detective", 200, "random/3+3", fDet1, fDet1, fDet2, fDet2, fDet1, fDet2, fDet3, fDet3);
		Show(69, "telescope", 180, (fTele1, 8), (fTele2, 4), (fTele3, 6), (fTele2, 4), (fHappy, 2));
		Child(238, "shooting star", "-2", "1", 90, "0", Wait(24).Concat(shootStar).Concat(shootStar.Reverse().Take(1)).ToArray()).Fade(1, 0);
		p.Childs.Add((69, Ox(0), Oy(0), 238));
		Show(70, "thumbs up", 180, (fThumbs0, 2), (fThumbs, 8), (fHappy, 2));
		Show(71, "confetti", 130, (fJumpPrep, 2), (fConfPop, 6), (fCheer2, 2), (fHappy, 3));
		Child(239, "confetti falls", "0", "2", 120, "20", fConf1, fConf2).Fade(1, 0);
		Child(240, "confetti falls 2", "1", "2", 140, "18", fConf2, fConf1).Fade(1, 0);
		p.Childs.Add((71, Ox(-14), Oy(-26), 239));
		p.Childs.Add((71, Ox(14), Oy(-30), 240));
		Loop2(72, "flag", 200, "random/3+4", fFlag1, fFlag2);
		Show(73, "error", 120, (fWide, 2), (fError1, 8), (fErrShake1, 1), (fErrShake2, 1), (fErrShake1, 1), (fErrShake2, 1), (fBlink, 2), (fStand, 1));
		Child(241, "warning up", "0", "-2", 100, "12", fWarnChild).Fade(1, 0);
		p.Childs.Add((73, Ox(0), Oy(0), 241));
		Loop2(74, "cry", 220, "random/3+4", fCry1, fCry1, fCry2, fCry2);
		Loop2(75, "angry", 110, "random/3+6", fAngry1, fAngry2);
		Child(242, "angry steam", "1", "-2", 100, "12", fSteam).Fade(1, 0);
		p.Childs.Add((75, Ox(0), Oy(0), 242));
		Show(76, "shocked", 110, (fStand, 2), (fShock1, 4), (fShock2, 5), (fBlink, 2), (fStand, 1));
		// scared: he trembles and hides in his pot, then peeks out
		Show(77, "scared", 80, (fScared1, 2), (fScared2, 2), (fScared1, 2), (fScared2, 2), (fHide1, 2));
		Then(p.Anims.Last(), 45);
		var talk = p.A(78, "talk").Move("0", "0").Speed("160").Loop("random/3+8", 0);
		talk.Seq(fTalk1, fTalk3, fTalk1, fStand, fTalk2, fTalk1, fTalk3, fStand); talk.Then(60, 19).Then(40, 11);
		Loop2(79, "umbrella", 220, "random/3+5", fUmb1, fUmb1, fUmb2, fUmb2);
		Show(80, "puddle splash", 130, (fPud0, 4), (fPudCrouch, 2), (fPudJump, 4));
		Then(p.Anims.Last(), 81);
		Show(81, "splash", 140, (fPudSplash, 6), (fLaugh1, 2), (fLaugh2, 2), (fHappy, 2));
		Child(243, "splash left", "-3", "-1", 80, "8", fSplashChild).Fade(1, 0);
		Child(244, "splash right", "3", "-1", 80, "8", fSplashChild).Fade(1, 0);
		p.Childs.Add((81, Ox(0), Oy(0), 243));
		p.Childs.Add((81, Ox(16), Oy(0), 244));
		Loop2(82, "hot", 240, "random/3+4", fHot1, fHot1, fHot2, fHot2);
		Loop2(83, "hand fan", 120, "random/3+8", fFan1, fFan2);
		Show(84, "sunbathing", 260, (fUp, 2), (fSunPut, 3), (fSunb1, 8), (fSunb2, 4), (fSunb1, 6), (fSunb2, 4), (fSunb1, 4));
		Child(245, "sun shines", "0", "0", 260, "0", Rep(15, fSunC1, fSunC2)).Fade(1, 0);
		p.Childs.Add((84, Ox(-2), Oy(-4), 245));
		Show(85, "ice cream", 200, (fIce1, 3), (fIce2, 3), (fIce1, 2), (fIce2, 3), (fIce3, 3), (fIce4, 4));
		Loop2(86, "cold", 70, "random/3+10", fCold1, fCold2);
		Loop2(87, "shiver", 60, "random/3+10", fShiver1, fShiver2);
		Loop2(88, "scarf", 360, "random/3+3", fScarf1, fScarf2);
		Show(89, "snow cap", 220, (fSnowCap0, 6), (fSnowCap1, 6), (fSnowCap2, 6), (fSnowShake1, 1), (fSnowShake2, 1), (fSnowShake1, 1), (fSnowShake2, 1), (fHappy, 3));
		Child(246, "snowfall", "0", "1", 200, "20", fSnowC1, fSnowC2).Fade(1, 0);
		p.Childs.Add((89, Ox(-10), Oy(-14), 246));
		// rain cloud: it rains on him and he grows a little (then goes back to his size)
		Show(90, "rain cloud", 150, (fRainLook, 6), (fRainHappy, 8), (fGrow1, 3), (fGrow2, 6), (fGrow3, 3), (fGrow1, 2), (fHappy, 3));
		Child(247, "cloud rains", "0", "0", 150, "0", Rep(9, cloudF)).Then(100, 248);
		Child(248, "cloud leaves", "-3", "-1", 100, "0", Rep(5, cloudF)).Fade(1, 0);
		p.Childs.Add((90, Ox(0), Oy(0), 247));
		Show(91, "look up", 260, (fStand, 1), (fUp, 6), (fBlink, 1), (fUp, 4), (fStand, 1));
		Show(92, "surprised", 120, (fStand, 2), (fSurprised, 6), (fWide, 3), (fStand, 1));
		Loop2(93, "tired", 400, "random/3+3", fTired1, fTired1, fTired2, fTired2);
		Show(94, "sleepy", 300, (fTired1, 3), (fSleepy1, 3), (fSleepy2, 4), (fWide, 1), (fTired1, 3), (fSleepy2, 5), (fBlink, 2), (fStand, 1));
		Show(95, "drink", 200, (fDrink1, 3), (fDrink2, 4), (fDrink3, 4), (fDrinkAh, 4), (fHappy, 2));
		Show(96, "eat", 180, (fEat1, 3), (fEat2, 2), (fEat3, 2), (fEat2, 2), (fEat4, 3), (fEat5, 2), (fEat3, 2), (fEat5, 2), (fEat6, 4), (fHappy, 2));
		Show(97, "cookie", 180, (fCookie1, 3), (fCookie2, 2), (fCookie3, 2), (fCookie2, 2), (fCookie4, 2), (fCookie3, 2), (fCookie5, 4));
		Loop2(98, "sweat", 160, "random/3+5", fSweat1, fSweat1, fSweat2, fSweat2);
		Loop2(100, "dizzy", 140, "random/3+4", fDizzy1, fDizzy2, fDizzy3);
		Show(101, "nod", 160, (fStand, 1), (fNod2, 1), (fNod1, 2), (fNod2, 1), (fStand, 1), (fNod2, 1), (fNod1, 2), (fNod2, 1), (fHappy, 2));
		Show(102, "ok", 200, (fStand, 1), (fOk, 8), (fHappy, 2));
		Loop2(103, "cheer", 110, "random/3+4", fJumpPrep, fCheer2, fCheer1, fCheer1, fCheer2, fLand2);
		Loop2(104, "party", 160, "random/3+5", fParty1, fParty1, fParty2, fParty2);
		Child(249, "party confetti", "0", "2", 140, "16", fConf1, fConf2).Fade(1, 0);
		p.Childs.Add((104, Ox(0), Oy(-20), 249));
		Show(105, "jump", 80, (fCrouch1, 1), (fJumpPrep, 3), (fJumpUp, 2), (fJumpTop, 4), (fJumpDown, 2), (fLand1, 2), (fLand2, 1), (fLand3, 2), (fHappy, 3));
		Loop2(106, "happy", 140, "random/3+3", fHappy, fHappy, fHappyJump, fHappyJump, fHappy, fLand2);
		// hug: he hugs a heart balloon... too hard: pop! hearts fly out
		Show(107, "hug", 150, (fHug1, 6), (fHug2, 6), (fHugPop, 2), (fHugLove, 4), (fHugLove2, 4), (fHugLove, 4));
		Child(250, "hug heart wait", "0", "0", 150, "0", Wait(13)).Then(100, 251);
		Child(251, "hug heart", "-2", "-3", 110, "12", fHeartC1).Fade(1, 0);
		Child(252, "hug heart wait 2", "0", "0", 150, "0", Wait(13)).Then(100, 253);
		Child(253, "hug heart 2", "0", "-3", 120, "12", fHeartC2).Fade(1, 0);
		Child(254, "hug heart wait 3", "0", "0", 150, "0", Wait(13)).Then(100, 255);
		Child(255, "hug heart 3", "2", "-3", 130, "11", fHeartC3).Fade(1, 0);
		p.Childs.Add((107, Ox(0), Oy(0), 250));
		p.Childs.Add((107, Ox(0), Oy(0), 252));
		p.Childs.Add((107, Ox(0), Oy(0), 254));
		Loop2(108, "hearts", 240, "random/3+3", fHearts1, fHearts1, fHearts2, fHearts2);
		Child(256, "heart up", "-1", "-2", 110, "14", fHeartC1).Fade(1, 0);
		Child(257, "heart up 2", "1", "-2", 130, "12", fHeartC3).Fade(1, 0);
		p.Childs.Add((108, Ox(0), Oy(0), 256));
		p.Childs.Add((108, Ox(0), Oy(-4), 257));
		Loop2(109, "love", 260, "random/3+3", fLove1, fLove1, fLove2, fLove2);
		Loop2(110, "sweep", 160, "random/3+6", fSweep1, fSweep2);
		Child(258, "sweep dust", "-2", "-1", 120, "12", fDustC).Fade(1, 0);
		p.Childs.Add((110, Ox(0), Oy(0), 258));
		Loop2(111, "work", 150, "random/3+6", fWork1, fWork1, fWork2, fWork2);
		Loop2(112, "music", 200, "random/3+6", fMusic1, fMusic2);
		Child(259, "note up", "-1", "-2", 110, "20", fNoteC, fNoteC).Fade(1, 0);
		Child(260, "note up 2", "1", "-2", 130, "18", fNoteC, fNoteC).Fade(1, 0);
		p.Childs.Add((112, Ox(0), Oy(0), 259));
		p.Childs.Add((112, Ox(10), Oy(-6), 260));
		Show(113, "coffee", 220, (fCoffee1, 3), (fCoffee2, 3), (fCoffee1, 2), (fCoffee2, 3), (fCoffee3, 4));
		Child(261, "coffee steam", "0", "-1", 140, "12", fSteamC).Fade(1, 0);
		p.Childs.Add((113, Ox(0), Oy(0), 261));
		Show(114, "hello", 160, (fStand, 1), (fHello1, 2), (fHello2, 2), (fHello1, 2), (fHello2, 2), (fHello1, 2), (fHappy, 3));
		Loop2(115, "curious", 300, "random/3+3", fCurious, fCurious, fCurious2, fCurious2);
		Show(116, "sing", 220, (fMusicBubble, 4), (fMusic1, 2), (fMusic2, 2), (fMusic1, 2), (fMusic2, 2), (fHappy, 2));
		p.Childs.Add((116, Ox(0), Oy(0), 259));

		// the idle hub: what he does next
		hub.Then(30, 19).Then(8, 15).Then(6, 17).Then(5, 16).Then(4, 22).Then(3, 27).Then(4, 28).Then(3, 29).Then(2, 30).Then(4, 32).Then(3, 33)
			.Then(3, 34).Then(3, 36).Then(3, 37).Then(3, 38).Then(3, 39).Then(3, 40).Then(2, 41).Then(3, 42).Then(3, 43).Then(2, 44).Then(2, 46)
			.Then(2, 47).Then(3, 48).Then(2, 49).Then(2, 50).Then(2, 84).Then(2, 90).Then(2, 60).Then(2, 63).Then(2, 101).Then(2, 109).Then(2, 116);

		// 99 kill: he waves goodbye and sinks into his pot, which fades away
		p.A(99, "kill").Move("0", "0").Speed("150").Fade(1, 0).Seq(fWave1, fWave2, fWave1, fWave2, fHide1, fHide2, fHide2, fHide2).Then(100, 99);

		p.Readme = """
			# Xiquinho the Cactus

			## Description

			A little cactus in pixel art, living in a terracotta pot that walks on two tiny brown feet.
			He has three arms (a tall one in the middle and two on the sides), a happy face with pink
			cheeks, and light spines all along his sides.

			He waddles around with small steps, swaying on top of his pot, and never climbs the screen
			borders: when he reaches one he just turns around. Water him and a pink flower blooms on his
			head (and a bee comes to visit it); a balloon pops on his spines, a sneeze sends his spines
			flying, a tumbleweed bounces right over him, a calango does push-ups with him and a bird
			that lands on his head gets pricked.

			## About pet

			Author: Luan Felipe

			## Animations

			* waddle, scurry, turn around, ease into and out of walking, hop, fall and land (squash and stretch), sit, sleep
			* watering can and a blooming flower, a bee, a balloon, sneezing his spines off, a tumbleweed, a calango (lizard),
			  a bird, a butterfly, a baby cactus, soap bubbles, flower rain, a cracked pot (and a band-aid), rolling in his pot
			* dance, laugh, yawn, stretch, hiccup, peek out of the pot, comb his spines, whistle, kick a pebble, spin
			* reacts to the app: thinks, reviews, reads, writes, types on a laptop, plays detective, looks through a telescope
			  and celebrates with Claude Code; drinks water on the water reminder, eats watermelon at lunch, has a coffee on
			  breaks; umbrella, hand fan, sunbathing, scarf, snow cap and a rain cloud (that makes him grow) with the weather;
			  party at the end of a Pomodoro
			* drag and toss with the mouse
			""";
		p.ReadmePt = """
			# Xiquinho, o Cacto

			## Descrição

			Um cactinho em pixel art que mora num vasinho de barro com dois pezinhos marrons.
			Ele tem três braços (um alto no meio e dois dos lados), uma carinha feliz com bochechas
			rosadas e espinhos clarinhos dos dois lados.

			Ele anda gingando com passinhos curtos, balançando em cima do vaso, e nunca escala as bordas
			da tela: quando chega numa, só dá meia-volta. Regue o Xiquinho e uma flor cor-de-rosa
			desabrocha na cabeça dele (e uma abelha vem visitar); um balão estoura nos espinhos, um
			espirro faz os espinhos voarem, uma bola de feno quica por cima dele, um calango faz flexões
			com ele e um passarinho que pousa na cabeça dele leva uma espetada.

			## Sobre o pet

			Autor: Luan Felipe

			## Animações

			* andar gingando, correr, dar meia-volta, começar e parar de andar com suavidade, pular, cair e pousar
			  (amassando e esticando), sentar, dormir
			* regador e flor desabrochando, abelha, balão, espirro que faz os espinhos voarem, bola de feno, calango,
			  passarinho, borboleta, cactinho bebê, bolhas de sabão, chuva de flores, vaso rachado (e um curativo),
			  rolar dentro do vaso
			* dançar, rir, bocejar, se espreguiçar, soluçar, espiar de dentro do vaso, pentear os espinhos, assobiar,
			  chutar uma pedrinha, girar
			* reage ao programa: pensa, revisa, lê, escreve, digita no notebook, vira detetive, olha pela luneta e
			  comemora com o Claude Code; bebe água no lembrete de água, come melancia no almoço, toma um cafezinho
			  nas pausas; guarda-chuva, leque, banho de sol, cachecol, neve na cabeça e nuvem de chuva (que faz ele
			  crescer) conforme o clima; festa no fim do Pomodoro
			* arrastar e jogar com o mouse
			""";

		Frame.NewHeadroom = 0;
		Frame.NewWidth = Frame.Size;
		return p;
	}
}
