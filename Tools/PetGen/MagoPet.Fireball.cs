namespace PetGen;

static partial class MagoPet
{
	/// <summary>
	/// 100 "arcane fireball", the big scene of the Mago. A circle of runes lights up under his shoes and turns, sparks
	/// gather into a ball of fire over his raised hand, he throws it. The fireball is a child window that flies in an arc
	/// (one animation per piece of the arc, each one opening a child with the sparks it leaves behind), and bursts when it
	/// hits the taskbar, a window or the side of the screen: a white flash, a ball of fire, a shockwave of two rings
	/// (cyan and magenta) so big that it needs three more windows around the first one, stars that fly out to both sides,
	/// a puff of violet smoke, and a scorch with glowing embers that fades. The Mago shields his eyes, jumps at the boom,
	/// then blows on the wand like a smoking gun.
	/// Ids: 100 and 140-142 for the Mago, 200-219 for the windows of the fireball.
	/// </summary>
	static void ArcaneFireball(Kit k)
	{
		// ---------------------------------------------------------------- the Mago
		// the rune circle on the floor, around (under) the shoes, turning: k = 0..5
		void RuneCircle(Frame f, int turn, bool bright)
		{
			for (int i = 0; i < 28; i++)
			{
				float an = MathF.PI * 2 * i / 28;
				int x = (int)MathF.Round(7.5f + MathF.Cos(an) * 8f), y = (int)MathF.Round(14.6f + MathF.Sin(an) * 1.4f);
				if (f[x, y] != '.' && y < 15) continue;   // the back half goes behind him
				char c = ((i + turn) % 7) switch { 0 => Star, 1 or 2 => Magenta, 3 => bright ? HotWhite : GlassLight, _ => Cyan };
				if (f[x, y] == '.' || y == 15 && f[x, y] != Shoe) f[x, y] = c;
			}
		}
		// the ball of fire over his raised left hand (the hand is at x 1, y 4), size 0..4
		void HandOrb(Frame f, int size, int flick)
		{
			if (size <= 0) return;
			if (size == 1) { f.Dot(1, 2, Orange).Dot(1, 1, Star); return; }
			float r = size * 0.75f, cx = 1.5f, cy = 2.5f - size * 0.5f;
			for (int y = -4; y <= 4; y++) for (int x = 0; x <= 5; x++)
			{
				float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = MathF.Sqrt(dx * dx + dy * dy);
				if (d > r) continue;
				float q = d / r;
				f[x, y] = q < 0.35f ? HotWhite : q < 0.65f ? Star : q < 0.9f ? Orange : FlameRed;
			}
			// little flames licking up and arcane sparks around it
			f.Dot((int)cx + (flick % 2), (int)(cy - r) - 1, Orange).Dot((int)(cx + r) + 1, (int)cy - flick % 2, Magenta).Dot(0, (int)(cy + r), Cyan);
		}
		// sparks flying in towards the orb (from far to near as the spell builds)
		void Gather(Frame f, int step)
		{
			(int x, int y, char c)[] from = { (7, -3, Cyan), (12, 1, Magenta), (6, 9, Star), (13, 9, Cyan), (9, -4, Magenta) };
			foreach (var (x, y, c) in from)
			{
				float t = Math.Min(1f, step / 5f);
				f.Dot((int)MathF.Round(x + (1 - x) * t * 0.85f), (int)MathF.Round(y + (1 - y) * t * 0.85f), c);
			}
		}
		Frame Charge(int step)
		{
			var f = Base(); Eyes(f, step < 2 ? 0 : 1); Arm(f, -1, 1); WandUp(f, step >= 2);
			RuneCircle(f, step, step >= 4);
			Gather(f, step);
			HandOrb(f, step <= 1 ? step : Math.Min(4, step - 1), step);
			if (step >= 4) { f.Dot(2, 7, Orange).Dot(2, 8, Orange); }   // the glow lights his robe
			return f;
		}
		int[] fCharge = Enumerable.Range(0, 6).Select(s => k.F(Charge(s))).ToArray();
		// throwing: the left hand pushes forward (x 0, y 7), the orb is gone (it is the child window now)
		var th = Base(); Eyes(th, 4); Mouth(th); Arm(th, -1, 2); WandUp(th, true); RuneCircle(th, 6, true);
		th.Dot(0, 6, Orange).Dot(0, 5, Star);   // the last flames leaving the hand
		int fThrow = k.F(th);
		// watching it fly, a hand over the eyes; at the boom he jumps; then the smoking wand
		var w1 = Base(); Eyes(w1, 2); Arm(w1, -1, 1); w1.Dot(2, 4, Skin).Dot(1, 4, Robe); WandDown(w1); int fWatch1 = k.F(w1);
		var w2 = Base(); Eyes(w2, 2); Arm(w2, -1, 1); w2.Dot(2, 4, Skin).Dot(1, 4, Robe); WandDown(w2); w2.Shift(0, -1); Shoes(w2, "...UUU....UUU..."); int fWatch2 = k.F(w2);
		var bm = Base(); Eyes(bm, 4); Mouth(bm); Arm(bm, -1, 1); Arm(bm, 1, 1); bm.Shift(0, -2); Shoes(bm, "..UU........UU.."); int fBoom = k.F(bm);
		Frame Proud(int puff)
		{
			var f = Base(); Eyes(f, 1); Arm(f, -1, 3); WandUp(f, false);
			f.Rect(13, 0, 1, 1, Skin);   // he holds the wand close to his mouth... and blows
			f.Dot(14, -1 - puff, Smoke).Dot(15, -2 - puff, Ash).Dot(13, -3 - puff, LightGray);
			return f;
		}
		int fProud1 = k.F(Proud(0)), fProud2 = k.F(Proud(1));

		// ---------------------------------------------------------------- the fireball (child windows)
		// the ball sits at (8, 10) of its canvas; it flies left, so its tail streams to the right and a bit up
		const float BX = 7.5f, BY = 10f;
		Frame Fireball(int flick)
		{
			var f = new Frame();
			// the tail first
			for (int y = 2; y <= 15; y++) for (int x = 6; x <= 15; x++)
			{
				float px = x + 0.5f - BX, py = y + 0.5f - BY;
				float along = px * 0.93f - py * 0.37f, side = px * 0.37f + py * 0.93f;
				if (along < 0 || along > 10) continue;
				float t = along / 10f;
				float half = 3.2f * (1 - t) + 0.4f + MathF.Sin(along * 1.3f - flick * 1.9f) * 0.7f * t;
				float q = MathF.Abs(side) / half;
				int n = Hash(x, y, flick + 7) % 100;
				if (q > 1) { if (q < 1.8f && n < 6) f[x, y] = n % 2 == 0 ? Cyan : Magenta; continue; }
				float heat = (1 - t) * (1.1f - q * 0.7f) + (n % 7) * 0.02f;
				char c = heat > 0.75f ? Star : heat > 0.5f ? Orange : heat > 0.3f ? FlameRed : heat > 0.15f ? Ember : n < 45 ? Violet : '.';
				if (c != '.') f[x, y] = c;
			}
			// the ball: white-hot core, yellow, orange, a red rim
			for (int y = 5; y <= 15; y++) for (int x = 2; x <= 13; x++)
			{
				float dx = x + 0.5f - BX, dy = y + 0.5f - BY, d = MathF.Sqrt(dx * dx + dy * dy);
				if (d > 3.3f) continue;
				f[x, y] = d < 1.2f ? HotWhite : d < 2.1f ? Star : d < 2.9f ? Orange : FlameRed;
			}
			// an arcane ring spinning around the ball: four bright beads that move each frame
			for (int i = 0; i < 4; i++)
			{
				float an = MathF.PI / 2 * i + flick * 0.55f;
				f[(int)MathF.Round(BX - 0.5f + MathF.Cos(an) * 4.4f), (int)MathF.Round(BY - 0.5f + MathF.Sin(an) * 4.4f)] = i % 2 == 0 ? Cyan : Magenta;
			}
			return f;
		}
		int[] fBall = Enumerable.Range(0, 4).Select(i => k.F(Fireball(i))).ToArray();
		// sparks left in the air behind it: they cool down and drift
		Frame Sparks(int age)
		{
			var f = new Frame();
			for (int i = 0; i < 10; i++)
			{
				int h = Hash(i, 3, 11);
				int x = 9 + h % 7, y = 5 + (h / 7) % 8 + age;
				char c = i % 3 == 0 ? (age == 0 ? Cyan : age == 1 ? Violet : Night)
					: age == 0 ? (i % 2 == 0 ? Star : Orange) : age == 1 ? (i % 2 == 0 ? Orange : FlameRed) : Ember;
				f.Dot(x, y, c);
				if (age == 0 && i % 4 == 0) f.Dot(x, y - 1, Paper);
			}
			return f;
		}
		int[] fSparks = Enumerable.Range(0, 3).Select(i => k.F(Sparks(i))).ToArray();

		// the burst: the center of the blast is (8, 12) of the window that hit; the ground is under row 15
		const float CX = 7.5f, CY = 12f;
		// radius of the shockwave rings at each step of the burst (the same in all four windows)
		float[] ringR = { 2, 3.5f, 5, 7, 9, 11, 13, 15, 17, 18.5f, 20 };
		void Rings(Frame f, float r, float ox, float oy, int step)
		{
			if (r < 3) return;
			for (int i = 0; i < 160; i++)
			{
				float an = MathF.PI * 2 * i / 160;
				foreach (var (rr, c) in new[] { (r, Cyan), (r - 1.4f, Magenta) })
				{
					if (rr < 2) continue;
					int x = (int)MathF.Round(CX - ox + MathF.Cos(an) * rr), y = (int)MathF.Round(CY - oy + MathF.Sin(an) * rr);
					if (step >= 8 && (i + step) % 3 == 0) continue;   // breaking up at the end
					f[x, y] = c;
				}
			}
			// runes riding on the ring
			for (int i = 0; i < 8; i++)
			{
				float an = MathF.PI * 2 * i / 8 + step * 0.2f;
				int x = (int)MathF.Round(CX - ox + MathF.Cos(an) * r), y = (int)MathF.Round(CY - oy + MathF.Sin(an) * r);
				if (x >= 1 && x <= 14 && y >= -3 && y <= 14) Sparkle(f, x, y, i % 2 == 0 ? Star : HotWhite);
			}
		}
		Frame Blast(int step)
		{
			var f = new Frame();
			int stage = step < 2 ? 0 : step < 4 ? 1 : step < 6 ? 2 : step < 8 ? 3 : 4;
			if (stage == 0)   // flash with rays
			{
				for (int i = 0; i < 8; i++)
				{
					float an = MathF.PI * 2 * i / 8;
					for (int r = 3; r <= 8; r++) f[(int)MathF.Round(CX + MathF.Cos(an) * r), (int)MathF.Round(CY + MathF.Sin(an) * r)] = r < 6 ? HotWhite : Star;
				}
				for (int y = 8; y <= 15; y++) for (int x = 3; x <= 12; x++) { float dx = x + 0.5f - CX - 0.5f, dy = y + 0.5f - CY; if (dx * dx + dy * dy <= 7.5f) f[x, y] = HotWhite; }
			}
			else if (stage <= 2)   // the ball of fire, its top turning into violet smoke
			{
				float r = stage == 1 ? 5f : 6.5f;
				for (int y = -4; y <= 15; y++) for (int x = 0; x < 16; x++)
				{
					float dx = x + 0.5f - CX - 0.5f, dy = y + 0.5f - CY, d = MathF.Sqrt(dx * dx + dy * dy);
					if (d > r) continue;
					int n = Hash(x, y, step + 40) % 100;
					float q = d / r + (n % 5) * 0.03f;
					f[x, y] = stage == 2 && y < CY - 3 && q > 0.4f ? (n < 50 ? Violet : Ash)
						: q < 0.3f ? HotWhite : q < 0.5f ? Star : q < 0.72f ? Orange : q < 0.9f ? FlameRed : Ember;
				}
			}
			else if (stage == 3)   // smoke rises, flames stay low
			{
				foreach (var (x, y, r, c) in new[] { (7.5f, 3f, 3.6f, Ash), (4f, 6f, 2.4f, Violet), (11f, 5f, 2.6f, Smoke) }) for (int yy = -4; yy < 12; yy++) for (int xx = 0; xx < 16; xx++)
				{
					float dx = xx + 0.5f - x, dy = yy + 0.5f - y;
					if (dx * dx + dy * dy <= r * r && Hash(xx, yy, 51) % 100 < 80) f[xx, yy] = c;
				}
				for (int x = 3; x <= 12; x++) { int top = 13 - (Hash(x, 1, step) % 3); for (int y = top; y <= 15; y++) f[x, y] = y == top ? FlameRed : y == 15 ? Orange : Star; }
			}
			else   // a glowing rune mark burnt on the floor, small flames, the last smoke
			{
				Scorch(f, step);
				foreach (var (x, y) in new[] { (6, 9), (9, 7), (7, 4), (10, 2) }) f.Dot(x + step % 2, y, Smoke);
			}
			Rings(f, ringR[step], 0, 0, step);
			return f;
		}
		void Scorch(Frame f, int flick)
		{
			f.Rect(2, 15, 12, 1, StoneDark).Rect(4, 14, 8, 1, Ash);
			for (int x = 3; x <= 12; x += 3) f.Dot(x + flick % 2, 15, flick % 2 == 0 ? Orange : Ember);
			f.Dot(5, 14, Cyan).Dot(10, 14, Magenta).Dot(7 + flick % 2, 14, Star);   // the rune still glows
			f.Dot(6, 13, flick % 2 == 0 ? Orange : FlameRed).Dot(9, 13 - flick % 2, Star);   // little flames
		}
		int[] fBlast = Enumerable.Range(0, ringR.Length).Select(s => k.F(Blast(s))).ToArray();
		// the parts of the shockwave in the windows around: left, right, above (offsets in pixels of the canvas)
		Frame RingPart(int step, float ox, float oy) { var f = new Frame(); Rings(f, ringR[step], ox, oy, step); return f; }
		int[] fRingL = Enumerable.Range(0, ringR.Length).Select(s => k.F(RingPart(s, -16, 0))).ToArray();
		int[] fRingR = Enumerable.Range(0, ringR.Length).Select(s => k.F(RingPart(s, 16, 0))).ToArray();
		int[] fRingU = Enumerable.Range(0, ringR.Length).Select(s => k.F(RingPart(s, 0, -20))).ToArray();
		Frame ScorchFrame(int flick)
		{
			var f = new Frame(); Scorch(f, flick);
			for (int i = 0; i < 5; i++) f.Dot((int)(7.5f + MathF.Sin((i + flick * 1.5f) * 0.9f) * 1.5f), 12 - i * 3 - flick, i < 2 ? Smoke : LightGray);
			return f;
		}
		int[] fScorch = { k.F(ScorchFrame(0)), k.F(ScorchFrame(1)) };
		Frame Stars(int spin)
		{
			var f = new Frame();
			Sparkle(f, 7, 8, spin == 0 ? Star : Cyan); Sparkle(f, 11, 11, spin == 0 ? Magenta : Star); f.Dot(4, 12, spin == 0 ? Cyan : Magenta).Dot(9, 5, PaleYellow);
			return f;
		}
		int[] fStars = { k.F(Stars(0)), k.F(Stars(1)) };
		Frame Puff(int g)
		{
			var f = new Frame();
			foreach (var (x, y, r, c) in new[] { (6f, 4f, 3f + g * 0.5f, Violet), (10f, 3f, 2.6f + g * 0.5f, Ash), (8f, 1f, 2.2f + g * 0.4f, Smoke) })
				for (int yy = -4; yy < 10; yy++) for (int xx = 0; xx < 16; xx++) { float dx = xx + 0.5f - x, dy = yy + 0.5f - y; if (dx * dx + dy * dy <= r * r) f[xx, yy] = c; }
			return f;
		}
		int[] fPuff = { k.F(Puff(0)), k.F(Puff(1)) };

		// ---------------------------------------------------------------- animations
		k.Lead(100, "arcane fireball", 120, 140, (k.Stand, 1), (fCharge[0], 2), (fCharge[1], 2), (fCharge[2], 2), (fCharge[3], 2), (fCharge[4], 2), (fCharge[5], 2), (fCharge[4], 1), (fCharge[5], 2));
		k.Lead(140, "fireball throw", 150, 141, (fThrow, 3), (fWatch1, 2), (fWatch2, 1), (fWatch1, 3), (fWatch2, 1), (fWatch1, 4), (fBoom, 3), (k.Surprise, 2));
		k.Show(141, "smoking wand", 220, (fProud1, 2), (fProud2, 2), (fProud1, 2), (k.Happy, 3));
		// the fireball starts just in front of his hand: its ball is 10 canvas pixels left of his hand and 3 lower
		k.Child(140, 200, "imageX-imageW*10/16", "imageY-imageH*3/20");
		// an arc made of pieces: up, flatter, down, then down until it hits something; every piece opens a cloud of sparks
		int[] arcDy = { -5, -3, -2, -1, 1, 2, 3, 4 };
		int[] ids = { 200, 211, 212, 213, 214, 215, 216, 217, 218 };
		for (int i = 0; i < ids.Length; i++)
		{
			string dy = i < arcDy.Length ? arcDy[i].ToString() : "5";
			var a = k.P.A(ids[i], i == 0 ? "fireball" : "fireball " + (i + 1)).Move("-8", dy).Speed("40").Seq(fBall)
				.OnBorder(100, 203, "taskbar").OnBorder(100, 203, "window").OnBorder(100, 203, "vertical");
			a.Then(100, i + 1 < ids.Length ? ids[i + 1] : ids[i]);   // the last piece falls until it hits
			k.Child(ids[i], 202);
		}
		k.P.A(202, "fireball sparks").Move("1", "1").Speed("80").Fade(1, 0).Seq(fSparks[0], fSparks[0], fSparks[1], fSparks[1], fSparks[2], fSparks[2]);
		var blast = k.P.A(203, "arcane blast").Move("0", "0").Speed("60", "110").Seq(fBlast); blast.Then(100, 204);
		k.P.A(204, "scorch").Move("0", "0").Speed("180").Loop("8", 0).Fade(1, 0).Seq(fScorch);
		k.P.A(205, "blast stars").Move("-4", "-6", "-3", "6").Speed("55").Loop("6", 0).Fade(1, 0.2).Seq(fStars);
		k.P.A(206, "blast stars 2").Move("4", "-7", "3", "6").Speed("55").Loop("6", 0).Fade(1, 0.2).Seq(fStars[1], fStars[0]);
		k.P.A(207, "violet smoke").Move("0", "-2").Speed("130").Loop("6", 0).Fade(0.9, 0).Seq(fPuff);
		k.P.A(208, "shockwave left").Move("0", "0").Speed("60", "110").Fade(1, 0.3).Seq(fRingL);
		k.P.A(209, "shockwave right").Move("0", "0").Speed("60", "110").Fade(1, 0.3).Seq(fRingR);
		k.P.A(210, "shockwave top").Move("0", "0").Speed("60", "110").Fade(1, 0.3).Seq(fRingU);
		k.Child(203, 205); k.Child(203, 206); k.Child(203, 207, "imageX", "imageY-imageH/3");
		k.Child(203, 208, "imageX-imageW", "imageY");
		k.Child(203, 209, "imageX+imageW", "imageY");
		k.Child(203, 210, "imageX", "imageY-imageH");

		// ---------------------------------------------------------------- sounds
		k.Sounds.Add(new PetSound(100, 100, new Sfx(1.8, 290).Tone(0, 1.6, 180, 520, 0.22, Sfx.Wave.Triangle, 0.3, 0.2, vibHz: 7, vibDepth: 0.04)
			.Tone(0.4, 1.2, 360, 1040, 0.12, Sfx.Wave.Sine, 0.2, 0.2, vibHz: 9, vibDepth: 0.03).Soften(3500)));   // the spell builds up
		k.Sounds.Add(new PetSound(140, 100, new Sfx(0.9, 291).Noise(0, 0.7, 0.45, 2500, 700, 0.01, 0.4, amHz: 18, amDepth: 0.4).Thud(0, 0.3, 220, 90, 0.2)));   // whoosh
		k.Sounds.Add(new PetSound(203, 100, new Sfx(1.8, 292).Thud(0, 0.85, 120, 30, 0.7).Noise(0, 1.4, 0.55, 3000, 250, 0.003, 1.0)
			.Bell(0.12, Sfx.Midi(88), 0.25, 0.4).Bell(0.24, Sfx.Midi(95), 0.2, 0.4).Soften(4000)));   // boom with a magic shimmer
	}
}
