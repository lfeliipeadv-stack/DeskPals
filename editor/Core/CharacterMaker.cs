using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using XmlData;

namespace PetEditor
{
	/// <summary>
	/// A pet drawn from shapes, in the style of the Pato pet: pick a kind (duck, cat, bear, bunny, blob, dog, bird or
	/// dragon), its colors, an accessory and a size, and every frame is drawn here: standing, breathing, blinking, a
	/// six-step walk, falling, held by the mouse, sleeping, happy, jumping, talking, dancing, sitting, in love, and the poses
	/// the program asks for when something happens (waving, thinking, reading, eating, a coffee, an umbrella in the rain,
	/// cold, hot, a party, music, crying, angry…), with their objects shaded like the pet. The bird and the dragon also fly.
	/// The pet comes out through the same path as the new-pet assistant (<see cref="Wizard.Create"/>), so it already walks,
	/// turns, falls and can be picked up, and each animation has the name the program looks for. Sounds made here
	/// (<see cref="CharacterSounds"/>) play with the animations.
	/// Every kind is seen from the side and faces left, like the other pets; the frames are 40 x 46 pixels before scaling.
	/// </summary>
	internal static class CharacterMaker
	{
		public enum Kind { Duck, Cat, Bear, Bunny, Blob, Dog, Bird, Dragon }
		public enum Extra { None, Hat, Bow, Scarf, Glasses, Backpack }
		/// <summary>Things drawn with a pose: held objects (book, cookie, mug, umbrella), things over the head and little effects.</summary>
		public enum Prop { None, Book, Cookie, Mug, Umbrella, Bulb, Thought, Notes, Party, Confetti, Tears, Sweat, Steam, Snow, Exclaim, Stars, Fire }

		/// <summary>What the user chose.</summary>
		public sealed class Look
		{
			public Kind Kind = Kind.Duck;
			public Color Body = Color.FromArgb(255, 216, 74), Detail = Color.FromArgb(255, 154, 42), Accent = Color.FromArgb(76, 175, 80);
			public Extra Extra = Extra.Hat;
			/// <summary>1 = 40 px wide (the size of the eSheep), 2 = 80 px, 3 = 120 px.</summary>
			public int Scale = 1;
			/// <summary>Sounds with the animations (a voice of its kind, steps, snoring, music…).</summary>
			public bool Sounds = true;
			public Look Clone() => (Look)MemberwiseClone();
		}

		/// <summary>The kinds that fly (they flap their wings instead of falling).</summary>
		public static bool Flies(Kind k) => k is Kind.Bird or Kind.Dragon;

		/// <summary>Colors that suit each kind (the defaults when the kind changes).</summary>
		public static (Color body, Color detail, Color accent) Palette(Kind kind) => kind switch
		{
			Kind.Cat => (Color.FromArgb(240, 150, 70), Color.FromArgb(255, 160, 170), Color.FromArgb(70, 130, 220)),
			Kind.Bear => (Color.FromArgb(160, 108, 66), Color.FromArgb(232, 190, 140), Color.FromArgb(214, 60, 60)),
			Kind.Bunny => (Color.FromArgb(242, 242, 246), Color.FromArgb(255, 160, 180), Color.FromArgb(150, 100, 210)),
			Kind.Blob => (Color.FromArgb(120, 210, 170), Color.FromArgb(255, 130, 120), Color.FromArgb(255, 210, 60)),
			Kind.Dog => (Color.FromArgb(214, 166, 112), Color.FromArgb(120, 78, 50), Color.FromArgb(60, 130, 220)),
			Kind.Bird => (Color.FromArgb(96, 172, 236), Color.FromArgb(255, 170, 60), Color.FromArgb(232, 72, 96)),
			Kind.Dragon => (Color.FromArgb(104, 184, 120), Color.FromArgb(70, 130, 110), Color.FromArgb(255, 200, 70)),
			_ => (Color.FromArgb(255, 216, 74), Color.FromArgb(255, 154, 42), Color.FromArgb(76, 175, 80)),
		};

		/// <summary>
		/// One pose. Eye: 0 open, 1 closed, 2 happy, 3 wide, 4 hearts, 5 dizzy (x), 6 tired (half closed), 7 angry, 8 sad
		/// (closed, brows up). Mouth: 0 closed, 1 open, 2 a big "O", 3 a frown. Arm (wings, front legs or little arms): 0 rest,
		/// 1 up, 2 down, 3 forward (holding something), 4 one up (waving). By: the body up (-) or down (+). Hx/Hy: the head
		/// moved on the body. Nf/Ff: near and far feet (X forward -, Y up -). Squash: below 1 flatter, above 1 taller.
		/// Prop and Ph: the object or effect of the pose and its step (0, 1, 2…), for the things that move.
		/// </summary>
		internal sealed record Pose(int Eye = 0, int Mouth = 0, int Arm = 0, double By = 0, double Hx = 0, double Hy = 0,
			double NfX = 0, double NfY = 0, double FfX = 0, double FfY = 0, double Tail = 0, double Squash = 1, bool Sit = false, bool Zz = false,
			Prop Prop = Prop.None, int Ph = 0);

		const int W = 40, H = 46, Y0 = 6;
		static readonly Color Ink = Color.FromArgb(26, 26, 34), Mouth = Color.FromArgb(150, 40, 36), Cheek = Color.FromArgb(255, 162, 180),
			HeartRed = Color.FromArgb(232, 62, 98), Zcolor = Color.FromArgb(150, 170, 240), Paper = Color.FromArgb(248, 246, 238),
			Water = Color.FromArgb(150, 205, 255), WaterDark = Color.FromArgb(60, 120, 210), Gold = Color.FromArgb(255, 214, 64),
			Red = Color.FromArgb(226, 52, 52), Steel = Color.FromArgb(120, 120, 134), Cookie = Color.FromArgb(206, 142, 72),
			Choco = Color.FromArgb(92, 52, 30), Vapor = Color.FromArgb(206, 206, 216);
		static readonly Color[] Party = { Color.FromArgb(232, 72, 96), Color.FromArgb(70, 160, 232), Color.FromArgb(255, 210, 60), Color.FromArgb(90, 200, 120), Color.FromArgb(180, 100, 220) };

		// ================================================================== drawing

		/// <summary>One frame, at 40 x 46 (not scaled).</summary>
		internal static Bitmap Draw(Look k, Pose q)
		{
			var s = new ShadedArt(W, H);
			double hx = 0, hy = 0, hrx = 0, hry = 0;   // the head, for the accessories
			double handX = 10, handY = Y0 + 30;          // where a held object goes
			double mx = 0, my = 0;                       // the front of the mouth (food, a sip, fire)
			var eyes = new List<(int x, int y)>();
			(int x, int y) mouthAt = (0, 0);
			Color light = ShadedArt.Mix(k.Body, Color.White, 0.55);
			double by = q.By + (q.Sit ? 3.5 : 0);
			double ground = Y0 + 39.5;
			int open = q.Mouth is 1 or 2 ? q.Mouth : 0;   // how open a bill or a beak is

			// the umbrella goes behind the pet: its dome over the head, the handle down to the paw
			if (q.Prop == Prop.Umbrella)
			{
				s.Rect(18, 6, 18, Y0 + 30, Steel, 0, "handle");
				s.Ell(18, 7.5, 15.5, 6.5, k.Accent, 0, "umbrella").CutBelow("umbrella", 8);
				s.Ell(18, 0.6, 0.8, 1.2, Steel, 0, "tip");
			}

			// legs of a four-legged pet: the near front and far back legs step together, then the other two
			void Legs(double front, double back, double legRx, double frontPaw, double backPaw, double bodyBottom)
			{
				if (q.Sit) return;
				double lift = q.Arm == 1 ? -3 : 0, kick = q.Arm == 2 ? -2 : 0, wave = q.Arm == 4 ? -6 - q.Ph : lift;
				void Leg(double x, double dy, double paw, int shift, string tag)
				{
					double top = bodyBottom - 2, bottom = ground + Math.Min(0, dy);
					s.Ell(x, (top + bottom) / 2, legRx, Math.Max(1.5, (bottom - top) / 2), k.Body, shift, tag);
					s.Ell(x - 0.8, bottom - 0.8, paw, 1.4, k.Body, shift, tag);
				}
				Leg(front + 3 + q.FfX, q.FfY + lift, frontPaw, 1, "leg2");
				Leg(back + 3 + q.NfX, q.NfY + kick, backPaw, 1, "leg4");
				Leg(front + q.NfX - (q.Arm == 4 ? 2 + q.Ph : 0), q.NfY + wave, frontPaw, 0, "leg");
				Leg(back + q.FfX, q.FfY + kick, backPaw, 0, "leg3");
			}
			void Head(double x, double y, double rx, double ry) { hx = x; hy = y; hrx = rx; hry = ry; s.Ell(x, y, rx, ry, k.Body); }
			// the body, and the backpack on its back right away (so wings and heads still go over it)
			void Body(double cx, double cy, double rx, double ry)
			{
				// the blob carries it behind, on its back side
				if (k.Extra == Extra.Backpack && k.Kind == Kind.Blob)
				{
					s.Ell(cx + rx - 1.2, cy - 1, 4.2, 5, k.Accent, 0, "pack");
					s.Ell(cx + rx - 0.6, cy - 4.2, 3.4, 2, k.Accent, 1, "flap");
				}
				s.Ell(cx, cy, rx, ry, k.Body);
				if (k.Extra != Extra.Backpack || k.Kind == Kind.Blob) return;
				double px = cx + rx * 0.42, py = cy - ry + 3.2;
				s.Ell(px, py, 4.4, 4.6, k.Accent, 0, "pack");
				s.Ell(px - 0.4, py - 2.4, 3.8, 2.2, k.Accent, 1, "flap");
			}
			// thin bird legs with little three-toed feet
			void BirdLegs(double nx, double fx, double top, Color c)
			{
				if (q.Sit) return;
				double fy1 = Y0 + 37.5 + q.FfY, ny1 = Y0 + 37.5 + q.NfY;
				s.Rect(fx + q.FfX, top, fx + q.FfX, fy1, c, 1, "leg2").Ell(fx - 1.5 + q.FfX, fy1 + 0.8, 2.6, 1.2, c, 1, "foot2");
				s.Rect(nx + q.NfX, top, nx + q.NfX, ny1, c, 0, "leg").Ell(nx - 1.5 + q.NfX, ny1 + 0.8, 2.8, 1.3, c, 0, "foot");
			}

			switch (k.Kind)
			{
				case Kind.Duck:
				{
					if (!q.Sit)
					{
						s.Ell(23 + q.FfX, Y0 + 38 + q.FfY, 4.4, 2, k.Detail, 1, "foot2");
						double fy0 = Y0 + 34 + by, fy1 = Y0 + 37 + q.FfY; s.Ell((47 + q.FfX) / 2, (fy0 + fy1) / 2, 1.5, Math.Max(1.4, (fy1 - fy0) / 2 + 0.8), k.Detail, 1, "leg2");
						double ny0 = Y0 + 34 + by, ny1 = Y0 + 37 + q.NfY; s.Ell((36 + q.NfX) / 2, (ny0 + ny1) / 2, 1.6, Math.Max(1.4, (ny1 - ny0) / 2 + 0.8), k.Detail, 0, "leg");
						s.Ell(16 + q.NfX, Y0 + 38.2 + q.NfY, 5, 2.1, k.Detail, 0, "foot");
					}
					double cy = Y0 + 28 + by, ry = 8.6 * q.Squash;
					if (q.Arm == 1 || q.Arm == 2) Wing(s, k.Body, 26, cy - 1, q.Arm, 1, "wing2");
					s.Ell(34.5, Y0 + 22.5 + by + q.Tail, 4, 3, k.Body);
					Body(23, cy + 8.6 - ry, 12.5, ry);
					s.Ell(15.5 + q.Hx * 0.5, Y0 + 21 + by + q.Hy * 0.5, 5.5, 6, k.Body);
					Head(15 + q.Hx, Y0 + 13 + by + q.Hy, 9, 8.6);
					Wing(s, k.Body, 23, cy, q.Arm == 4 ? 1 : q.Arm, 1, "wing", q.Arm == 4 ? q.Ph * 16 - 8 : 0);
					if (open > 0) s.Ell(hx - 7.6, hy + 4, 4.2, 1.7 + (open - 1) * 0.8, Mouth, 2, "mouth");
					s.Ell(hx - 7.4, hy + 4.6 + open * 1.1, 4.6, 1.3, k.Detail, 1, "bill2");
					s.Ell(hx - 8, hy + 2.6 - open * 0.4, 6, 2.1, k.Detail, 0, "bill");
					eyes.Add(((int)Math.Round(hx) - 4, (int)Math.Round(hy) - 3));
					mouthAt = (-100, 0);   // the bill is the mouth
					handX = 9; handY = cy - 0.5; mx = hx - 12; my = hy + 4;
					break;
				}
				case Kind.Cat:
				{
					double cy = Y0 + 29 + by, ry = 7 * q.Squash;
					Legs(17, 29, 1.9, 2.4, 2.4, cy + 7);
					s.Ell(35.5, Y0 + 20 + by + q.Tail * 0.6, 1.8, 7, k.Body, 0, "body", 18 + q.Tail * 6);
					Body(24, cy + 7 - ry, 11, ry);
					double x = 13 + q.Hx, y = Y0 + 20 + by + q.Hy;
					s.Ell(x - 4.2, y - 6.2, 2.6, 4.6, k.Body, 0, "ear", -20).Ell(x - 4.1, y - 5.6, 1.1, 2.6, k.Detail, 0, "ear", -20);
					s.Ell(x + 3.2, y - 6.8, 2.6, 4.6, k.Body, 0, "ear2", 16).Ell(x + 3.2, y - 6.2, 1.1, 2.6, k.Detail, 0, "ear2", 16);
					Head(x, y, 8, 7.2);
					s.Ell(hx - 7.4, hy + 1.2, 1.1, 0.9, k.Detail, 0, "nose");
					eyes.Add(((int)Math.Round(hx) - 4, (int)Math.Round(hy) - 3));
					mouthAt = ((int)Math.Round(hx) - 7, (int)Math.Round(hy) + 3);
					handX = hx - 2; handY = hy + 9; mx = hx - 9; my = hy + 4;
					break;
				}
				case Kind.Bear:
				{
					double cy = Y0 + 28 + by, ry = 8.8 * q.Squash;
					Legs(16.5, 30, 2.8, 3.2, 3.2, cy + 8.8);
					s.Ell(36.5, Y0 + 24 + by + q.Tail * 0.4, 2.3, 2.3, k.Body);
					Body(24, cy + 8.8 - ry, 12, ry);
					double x = 13 + q.Hx, y = Y0 + 18 + by + q.Hy;
					s.Ell(x - 4.5, y - 6.2, 3, 3, k.Body, 0, "ear").Ell(x - 4.5, y - 6, 1.4, 1.4, k.Detail, 0, "ear");
					s.Ell(x + 4.5, y - 7, 3, 3, k.Body, 0, "ear2").Ell(x + 4.5, y - 6.8, 1.4, 1.4, k.Detail, 0, "ear2");
					Head(x, y, 8.8, 8.2);
					s.Ell(hx - 6, hy + 2.6, 3.8, 2.8, light, 0, "snout");
					s.Ell(hx - 9, hy + 1.2, 1.5, 1.1, Ink, 0, "nose");
					eyes.Add(((int)Math.Round(hx) - 4, (int)Math.Round(hy) - 3));
					mouthAt = ((int)Math.Round(hx) - 7, (int)Math.Round(hy) + 4);
					handX = hx - 2; handY = hy + 10; mx = hx - 10; my = hy + 5;
					break;
				}
				case Kind.Bunny:
				{
					double cy = Y0 + 30 + by, ry = 7.4 * q.Squash;
					Legs(17, 29, 1.7, 2.2, 3.8, cy + 7.4);
					s.Ell(34.5, Y0 + 27 + by + q.Tail * 0.4, 3, 3, light, 0, "tail");
					Body(24, cy + 7.4 - ry, 10, ry);
					double x = 14 + q.Hx, y = Y0 + 21 + by + q.Hy, sway = q.Tail * 2;
					s.Ell(x - 3, y - 9, 2.2, 7.5, k.Body, 0, "ear", -14 + sway).Ell(x - 3, y - 8.5, 1, 5.4, k.Detail, 0, "ear", -14 + sway);
					s.Ell(x + 1.5, y - 9.5, 2.2, 7.5, k.Body, 0, "ear2", 8 + sway).Ell(x + 1.5, y - 9, 1, 5.4, k.Detail, 0, "ear2", 8 + sway);
					Head(x, y, 7.4, 6.9);
					s.Ell(hx - 7, hy + 0.8, 1.1, 0.9, k.Detail, 0, "nose");
					eyes.Add(((int)Math.Round(hx) - 4, (int)Math.Round(hy) - 3));
					mouthAt = ((int)Math.Round(hx) - 6, (int)Math.Round(hy) + 3);
					handX = hx - 2; handY = hy + 8.5; mx = hx - 8; my = hy + 4;
					break;
				}
				case Kind.Dog:
				{
					double cy = Y0 + 29 + by, ry = 7.6 * q.Squash;
					Legs(17, 29.5, 2.2, 2.8, 2.8, cy + 7.6);
					s.Ell(36, Y0 + 21 + by + q.Tail * 0.5, 1.7, 5, k.Body, 0, "tail", 28 + q.Tail * 10);
					Body(24, cy + 7.6 - ry, 11.5, ry);
					double x = 13 + q.Hx, y = Y0 + 18 + by + q.Hy;
					s.Ell(x + 4.5, y - 3, 2.4, 4.8, k.Detail, 1, "ear2", 24);
					Head(x, y, 8, 7.4);
					s.Ell(hx - 6.4, hy + 2.6, 4.4, 3, light, 0, "snout");
					s.Ell(hx - 10, hy + 1.2, 1.6, 1.3, Ink, 0, "nose");
					s.Ell(x + 2.6, y - 0.5 + Math.Abs(q.Tail) * 0.2, 2.6, 5.2, k.Detail, 0, "ear", 14 + q.Tail * 3);
					eyes.Add(((int)Math.Round(hx) - 5, (int)Math.Round(hy) - 3));
					mouthAt = ((int)Math.Round(hx) - 8, (int)Math.Round(hy) + 4);
					handX = hx - 2; handY = hy + 10; mx = hx - 11; my = hy + 5;
					break;
				}
				case Kind.Bird:
				{
					double cy = Y0 + 29 + by, ry = 7.8 * q.Squash;
					BirdLegs(20, 24, cy + 5, k.Detail);
					if (q.Arm == 1 || q.Arm == 2) Wing(s, k.Body, 25, cy - 1, q.Arm, 1, "wing2");
					s.Ell(33.5, cy + 2 + q.Tail * 0.5, 5.5, 2, k.Body, 1, "tail", -28 + q.Tail * 4);
					Body(23, cy + 7.8 - ry, 10, ry);
					s.Ell(19, cy + 3.5, 5.5, 4, light, 0, "body");
					Head(16 + q.Hx, Y0 + 17 + by + q.Hy, 7.5, 7);
					s.Ell(hx + 1.2, hy - 7.2, 1.4, 3, k.Accent, 0, "crest", 18).Ell(hx + 3.6, hy - 6.2, 1.2, 2.6, k.Accent, 0, "crest", 40);
					if (open > 0) s.Ell(hx - 7.4, hy + 2.6, 2.6, 1 + open * 0.5, Mouth, 2, "mouth");
					s.Ell(hx - 7.2, hy + 2.8 + open * 0.9, 2.8, 1, k.Detail, 1, "bill2");
					s.Ell(hx - 8, hy + 1.4 - open * 0.3, 3.4, 1.6, k.Detail, 0, "bill");
					Wing(s, k.Body, 21, cy, q.Arm == 4 ? 1 : q.Arm, 1, "wing", q.Arm == 4 ? q.Ph * 16 - 8 : 0);
					eyes.Add(((int)Math.Round(hx) - 4, (int)Math.Round(hy) - 3));
					mouthAt = (-100, 0);
					handX = 9; handY = cy - 0.5; mx = hx - 11; my = hy + 3;
					break;
				}
				case Kind.Dragon:
				{
					double cy = Y0 + 29 + by, ry = 7.4 * q.Squash;
					DragonWing(s, k.Detail, 30, cy - 5, q.Arm, 1, "wing2");
					Legs(17, 29, 2.4, 3, 3, cy + 7.4);
					s.Ell(33.5, cy + 3 + q.Tail * 0.3, 5.5, 2.2, k.Body, 0, "body", -22 + q.Tail * 4);
					s.Ell(38, cy - 0.6 + q.Tail * 0.6, 1.6, 1.6, k.Accent, 0, "spike", 45);
					for (int i = 0; i < 4; i++) s.Ell(18.5 + i * 4.4, cy + 7.4 - 2 * ry + 0.8, 1.3, 2, k.Accent, 0, "spike");
					Body(24, cy + 7.4 - ry, 11, ry);
					s.Ell(21, cy + 3.4, 6, 3.4, light, 0, "body");
					double x = 12 + q.Hx, y = Y0 + 19 + by + q.Hy;
					s.Ell(x + 2.6, y - 6.6, 1.2, 3, k.Accent, 0, "horn", 24).Ell(x + 5.6, y - 5.4, 1.2, 2.8, k.Accent, 0, "horn2", 42);
					Head(x, y, 7.6, 6.8);
					s.Ell(hx - 6, hy + 2 + open * 0.6, 4.6, 3, k.Body);
					DragonWing(s, k.Detail, 27, cy - 4, q.Arm, 0, "wing");
					eyes.Add(((int)Math.Round(hx) - 3, (int)Math.Round(hy) - 3));
					mouthAt = ((int)Math.Round(hx) - 9, (int)Math.Round(hy) + 3);
					handX = hx - 1; handY = hy + 10; mx = hx - 9; my = hy + 2;
					break;
				}
				default:   // the blob: one round body that squashes and stretches, two eyes in front
				{
					double sq = q.Sit ? 0.85 : q.Squash, ry = 9 * sq, rx = 13 / Math.Sqrt(sq);
					double cy = Y0 + 40 - ry + q.By;
					if (q.Arm == 1) { s.Ell(20 - rx + 1, cy - 4, 1.6, 3.2, k.Body, 1, "arm", -25); s.Ell(20 + rx - 1, cy - 4, 1.6, 3.2, k.Body, 1, "arm2", 25); }
					if (q.Arm == 4) { s.Ell(20 - rx + 1 - q.Ph, cy - 5, 1.6, 3.4, k.Body, 1, "arm", -25 - q.Ph * 15); s.Ell(20 + rx, cy + 2, 3, 1.6, k.Body, 1, "arm2", -15); }
					if (q.Arm == 2 || q.Arm == 3) { s.Ell(20 - rx, cy + 2, 3, 1.6, k.Body, 1, "arm", 15); s.Ell(20 + rx, cy + 2, 3, 1.6, k.Body, 1, "arm2", -15); }
					Body(20, cy, rx, ry);
					s.Ell(20 + q.Hx * 0.5, cy + ry * 0.45, rx * 0.55, ry * 0.32, light, 0, "body");
					hx = 20; hy = cy; hrx = rx * 0.7; hry = ry;
					int ey = (int)Math.Round(cy - ry * 0.35 + q.Hy * 0.5);
					eyes.Add(((int)Math.Round(20 - rx * 0.55 + q.Hx), ey));
					eyes.Add(((int)Math.Round(20 - rx * 0.12 + q.Hx), ey));
					mouthAt = ((int)Math.Round(20 - rx * 0.36 + q.Hx), ey + 5);
					handX = 20 - rx + 2; handY = cy + 2; mx = mouthAt.x - 2; my = ey + 6;
					break;
				}
			}

			// the accessory, on the head (a party hat takes the place of a hat or a bow)
			bool party = q.Prop == Prop.Party;
			switch (k.Extra)
			{
				case Extra.Hat when !party:
					s.Ell(hx, hy - hry + 2, hrx * 1.05, 1.7, k.Accent, 0, "hat2");
					s.Ell(hx + 0.5, hy - hry - 0.8, hrx * 0.66, 3.4, k.Accent, 0, "hat");
					s.Ell(hx + 0.5, hy - hry + 1, hrx * 0.66, 0.9, k.Accent, 1, "hatband");
					break;
				case Extra.Bow when !party:
					s.Ell(hx + 2.4, hy - hry + 1.4, 2.4, 1.6, k.Accent, 0, "bow", -20);
					s.Ell(hx + 6.2, hy - hry + 2.6, 2.4, 1.6, k.Accent, 0, "bow", 20);
					s.Ell(hx + 4.3, hy - hry + 2, 1.1, 1.1, k.Accent, 1, "bow");
					break;
				case Extra.Scarf:
					s.Ell(hx + 2.5, hy + hry - 0.6, hrx * 0.75, 1.8, k.Accent, 0, "scarf");
					s.Ell(hx + 2.5 + hrx * 0.6, hy + hry + 2.4, 1.6, 3, k.Accent, 1, "scarf2", -15);
					break;
			}

			// the objects in front of the pet
			switch (q.Prop)
			{
				case Prop.Book:
				{
					double bx = handX, bt = handY - 6;
					s.Rect(bx - 4, bt, bx + 3, bt + 6, k.Accent, 0, "book");
					s.Rect(bx - 3, bt - 1, bx + 2, bt - 1, Paper, 0, "page");
					if (q.Ph == 1) s.Rect(bx - 4, bt - 3, bx - 1, bt - 1, Paper, 0, "page2");
					break;
				}
				case Prop.Cookie when q.Ph < 3:
					s.Ell(mx, my + 1, 3.2 - q.Ph * 0.6, 3.2 - q.Ph * 0.6, Cookie, 0, "cookie");
					break;
				case Prop.Mug:
				{
					double cx = q.Ph == 1 ? mx + 1 : handX, mt = q.Ph == 1 ? my - 1 : handY - 5;
					s.Ell(cx + 2.6, mt + 2.6, 1.6, 1.9, k.Accent, 1, "mughandle");
					s.Rect(cx - 2, mt, cx + 1.5, mt + 5, k.Accent, 0, "mug");
					break;
				}
				case Prop.Bulb:
				{
					var (bx, bt) = Bulb(hx, hy, hrx, hry);
					s.Ell(bx, bt + 3.4, 3.9, 4, Gold, 0, "bulb");
					s.Rect(bx - 1.5, bt + 7, bx + 1.5, bt + 8.5, Steel, 0, "base");
					break;
				}
				case Prop.Party:
				{
					double px = hx + 1, baseY = hy - hry + 1.5;
					for (int i = 0; i < 7; i++) s.Ell(px + i * 0.15, baseY - i * 1.25, Math.Max(0.6, 3.8 - i * 0.55), 1.1, k.Accent, 0, "party");
					s.Ell(px + 1.1, Math.Max(1.4, baseY - 9), 1.4, 1.4, Gold, 0, "pom");
					break;
				}
				case Prop.Fire:
				{
					double grow = 1 + q.Ph * 0.25;
					s.Ell(mx - 1, my - 3, 2.6 * grow, 1.9 * grow, Color.FromArgb(255, 140, 40), 0, "fire", -50);
					s.Ell(mx - 2.4, my - 6.5, 2.2 * grow, 1.7 * grow, Color.FromArgb(255, 190, 50), 0, "fire2", -50);
					if (q.Ph > 0) s.Ell(mx - 3.6, my - 10, 1.6 * grow, 1.3 * grow, Color.FromArgb(255, 232, 120), 0, "fire3");
					break;
				}
			}

			var bmp = s.Render();
			// eyes, cheeks, mouth and the little extras on top (flat pixels, crisp)
			void Px(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < W && y < H) bmp.SetPixel(x, y, c); }
			void Stamp(int x, int y, string[] rows, Color dark, Color lit)
			{
				for (int j = 0; j < rows.Length; j++)
					for (int i = 0; i < rows[j].Length; i++)
						if (rows[j][i] == '0') Px(x + i, y + j, dark); else if (rows[j][i] == '1') Px(x + i, y + j, lit);
			}
			Color lid = ShadedArt.Tone(k.Body, 3);
			foreach (var (ex, ey) in eyes)
			{
				switch (q.Eye)
				{
					case 1: Stamp(ex, ey + 2, new[] { "000" }, lid, lid); break;
					case 2: Stamp(ex, ey + 1, new[] { ".0.", "0.0" }, lid, lid); break;
					case 3: Stamp(ex, ey - 1, new[] { ".0.", "10.", "010", "000", ".0." }, Ink, Color.White); break;
					case 4: Stamp(ex - 1, ey, new[] { "00.00", "00000", ".000.", "..0.." }, HeartRed, HeartRed); break;
					case 5: Stamp(ex, ey, new[] { "0.0", ".0.", "0.0" }, Ink, Ink); break;
					case 6: Stamp(ex, ey + 1, new[] { "000" }, lid, lid); Stamp(ex, ey + 2, new[] { "010", ".0." }, Ink, Color.White); break;
					case 7: Stamp(ex, ey, new[] { "10.", "010", "000", ".0." }, Ink, Color.White); Stamp(ex - 1, ey - 3, new[] { "...00", "000.." }, lid, lid); break;
					case 8: Stamp(ex, ey + 2, new[] { "000" }, lid, lid); Stamp(ex - 1, ey - 2, new[] { "000..", "...00" }, lid, lid); break;
					default: Stamp(ex, ey, new[] { "10.", "010", "000", ".0." }, Ink, Color.White); break;
				}
			}
			if (k.Extra == Extra.Glasses && eyes.Count > 0)
			{
				Color frame = ShadedArt.Tone(k.Accent, 3), glass = ShadedArt.Mix(k.Accent, Color.White, 0.8);
				string[] ring = { ".000.", "0...0", "0...0", "0...0", "0...0", ".000." };
				foreach (var (ex, ey) in eyes) Stamp(ex - 1, ey - 1, ring, frame, glass);
				if (eyes.Count > 1) Stamp(eyes[0].x + 4, eyes[0].y + 1, new[] { "0" }, frame, frame);
				else Stamp(eyes[0].x + 4, eyes[0].y + 1, new[] { "0000" }, frame, frame);
			}
			bool flushed = q.Prop == Prop.Sweat && q.Eye == 6;   // hot: red cheeks
			if (k.Kind != Kind.Blob && q.Eye != 4 && eyes.Count > 0) Stamp(eyes[0].x + 4, eyes[0].y + 3, flushed ? new[] { "000", "000" } : new[] { ".0", "00" }, flushed ? HeartRed : Cheek, Cheek);
			if (k.Kind == Kind.Blob && q.Eye != 4) foreach (var (ex, ey) in eyes) Stamp(ex - 1, ey + 3, new[] { "00" }, flushed ? HeartRed : Cheek, Cheek);
			if (mouthAt.x > -50)
			{
				switch (q.Mouth)
				{
					case 1: Stamp(mouthAt.x, mouthAt.y - 1, new[] { ".00.", "0110", ".00." }, lid, Mouth); break;
					case 2: Stamp(mouthAt.x, mouthAt.y - 1, new[] { ".00.", "0110", "0110", ".00." }, lid, Mouth); break;
					case 3: Stamp(mouthAt.x, mouthAt.y, new[] { ".00.", "0..0" }, lid, lid); break;
					default: Stamp(mouthAt.x, mouthAt.y, new[] { "0..0", ".00." }, lid, lid); break;
				}
			}
			if (k.Kind == Kind.Dragon) Px((int)Math.Round(hx) - 10, (int)Math.Round(hy) + 1, ShadedArt.Tone(k.Body, 3));   // the nostril
			if (q.Zz) Stamp(31, 1, new[] { "0000", "..0.", ".0..", "0000" }, Zcolor, Zcolor);

			int hxi = (int)Math.Round(hx), hyi = (int)Math.Round(hy), top = (int)Math.Round(hy - hry);
			switch (q.Prop)
			{
				case Prop.Thought:
				{
					// two little bubbles going up from the head, the cloud with dots appearing one by one
					// the cloud near the head, up and to the back (the whole cloud inside the frame)
					double cx = Math.Min(W - 7.5, hx + hrx + 8), cy = Math.Max(4.5, top - 7);
					Stamp(hxi + (int)hrx - 1, top - 1, new[] { "00", "00" }, ShadedArt.Tone(Paper, 3), Paper);
					Stamp((int)Math.Round((hxi + hrx + cx) / 2) - 1, (int)Math.Round((top + cy) / 2), new[] { ".0.", "010", ".0." }, ShadedArt.Tone(Paper, 3), Paper);
					Disc(bmp, cx, cy, 6.5, 3.8, Paper, ShadedArt.Tone(Paper, 3));
					for (int i = 0; i <= Math.Min(2, q.Ph); i++) Px((int)Math.Round(cx) - 3 + i * 2, (int)Math.Round(cy), Ink);
					break;
				}
				case Prop.Notes:
				{
					// a note in the accent color with a dark rim: seen on a light and on a dark desktop
					string[] note = { "..11", "..1.", "..1.", "111.", "11.." };
					void Note(int x, int y) { Outlined(bmp, x, y, note, k.Accent, ShadedArt.Tone(k.Accent, 3)); }
					if (q.Ph == 0) { Note(28, 8); Note(34, 2); }
					else { Note(31, 5); Note(24, 1); }
					break;
				}
				case Prop.Tears:
					foreach (var (ex, ey) in eyes.Take(1))
					{
						Stamp(ex + 1, ey + 4, q.Ph == 0 ? new[] { "1", "1" } : new[] { "1", ".", "1", "1" }, WaterDark, Water);
						Stamp(ex - 2 - q.Ph, ey + 1 + q.Ph, new[] { ".1.", "101", ".0." }, WaterDark, Water);
					}
					break;
				case Prop.Sweat:
					Stamp(hxi + (int)(hrx * 0.6), top + 1 + q.Ph, new[] { ".0.", "010", "010", ".0." }, WaterDark, Water);
					if (q.Ph == 1) Stamp(hxi + (int)(hrx * 0.6) + 4, top + 5, new[] { "0", "1" }, WaterDark, Water);
					break;
				case Prop.Steam:
					Stamp(hxi + (int)(hrx * 0.5) - q.Ph, top - 1, new[] { ".0.0.", "00.00", ".....", "00.00", ".0.0." }, Red, Red);
					Disc(bmp, hxi + hrx + 2, Math.Max(2, top - 3 - q.Ph), 2 + q.Ph * 0.5, 1.6, Vapor, ShadedArt.Tone(Vapor, 2));
					break;
				case Prop.Snow:
					foreach (var (fx, fy) in new[] { (3, 4), (12, 0), (27, 5), (36, 2), (33, 15), (4, 17), (21, 1) })
						Stamp(fx, (fy + q.Ph * 3) % 22, new[] { ".1.", "101", ".1." }, Color.FromArgb(160, 196, 240), Color.White);
					break;
				case Prop.Exclaim:
					Stamp(Math.Min(W - 3, hxi + (int)hrx + 1), Math.Max(0, top - 5 + q.Ph), new[] { "00", "00", "00", "..", "00" }, Red, Red);
					break;
				case Prop.Stars:
					for (int i = 0; i < 3; i++)
					{
						double a = q.Ph * 0.9 + i * 2.1;
						Stamp((int)Math.Round(hx + Math.Cos(a) * (hrx + 1)) - 1, (int)Math.Round(top - 2 + Math.Sin(a) * 2.2) - 1, new[] { ".0.", "010", ".0." }, Gold, Color.White);
					}
					break;
				case Prop.Cookie:
				{
					int cx = (int)Math.Round(mx), cy = (int)Math.Round(my + 1);
					if (q.Ph < 3) { Px(cx - 1, cy - 1, Choco); if (q.Ph < 2) Px(cx + 1, cy + 1, Choco); if (q.Ph == 0) Px(cx - 1, cy + 2, Choco); }
					if (q.Ph > 0) { Px(cx - 1, cy + 5 + q.Ph, Cookie); Px(cx + 2, cy + 7, Cookie); }
					break;
				}
				case Prop.Mug when q.Ph != 1:
				{
					int cx = (int)Math.Round(handX), t = (int)Math.Round(handY - 5);
					Stamp(cx - 1, t - 5, q.Ph == 0 ? new[] { ".0.", "0..", ".0.", "..0" } : new[] { "0..", ".0.", "..0", ".0." }, Vapor, Vapor);
					break;
				}
				case Prop.Bulb when q.Ph == 1:
				{
					var (bx, bt) = Bulb(hx, hy, hrx, hry);
					int cx = (int)Math.Round(bx), cy = (int)Math.Round(bt + 3);
					foreach (var (dx, dy) in new[] { (-5, 0), (5, 0), (0, -5), (-4, -4), (4, -4) }) Px(cx + dx, cy + dy, Gold);
					break;
				}
				case Prop.Umbrella:
					foreach (var (rx, ry) in new[] { (1, 10), (37, 12), (2, 22), (35, 24), (38, 4) })
						Stamp(rx, (ry + q.Ph * 4) % 30 + 6, new[] { "0", "0" }, Color.FromArgb(110, 160, 240), Color.Empty);
					break;
				case Prop.Party:
				case Prop.Confetti:
				{
					var r = new Random(7 + q.Ph);
					for (int i = 0; i < 12; i++) Px(r.Next(0, W), r.Next(0, 16), Party[i % Party.Length]);
					break;
				}
			}
			return bmp;
		}

		/// <summary>Where the light bulb of an idea goes: next to the head, up.</summary>
		static (double x, double top) Bulb(double hx, double hy, double hrx, double hry) =>
			(Math.Min(W - 4.5, hx + hrx + 3), Math.Max(1, hy - hry - 6));

		/// <summary>Flat pixels ('1') with a rim of another color around them (where nothing of the shape is).</summary>
		static void Outlined(Bitmap bmp, int x, int y, string[] rows, Color fill, Color rim)
		{
			bool In(int i, int j) => j >= 0 && j < rows.Length && i >= 0 && i < rows[j].Length && rows[j][i] == '1';
			for (int j = -1; j <= rows.Length; j++)
				for (int i = -1; i <= 4 + 1; i++)
				{
					int px = x + i, py = y + j;
					if (px < 0 || py < 0 || px >= bmp.Width || py >= bmp.Height) continue;
					if (In(i, j)) bmp.SetPixel(px, py, fill);
					else if (In(i - 1, j) || In(i + 1, j) || In(i, j - 1) || In(i, j + 1)) bmp.SetPixel(px, py, rim);
				}
		}

		/// <summary>A flat disc with an outline (the cloud of a thought, a puff of steam).</summary>
		static void Disc(Bitmap bmp, double cx, double cy, double rx, double ry, Color fill, Color line)
		{
			for (int y = (int)(cy - ry) - 1; y <= (int)(cy + ry) + 1; y++)
				for (int x = (int)(cx - rx) - 1; x <= (int)(cx + rx) + 1; x++)
				{
					if (x < 0 || y < 0 || x >= bmp.Width || y >= bmp.Height) continue;
					static double D(double dx, double dy, double rx, double ry) => dx * dx / (rx * rx) + dy * dy / (ry * ry);
					double d = D(x + 0.5 - cx, y + 0.5 - cy, rx, ry);
					if (d > 1) continue;
					bool edge = D(x + 1.5 - cx, y + 0.5 - cy, rx, ry) > 1 || D(x - 0.5 - cx, y + 0.5 - cy, rx, ry) > 1 || D(x + 0.5 - cx, y + 1.5 - cy, rx, ry) > 1 || D(x + 0.5 - cx, y - 0.5 - cy, rx, ry) > 1;
					bmp.SetPixel(x, y, edge ? line : fill);
				}
		}

		/// <summary>The wing of the duck and the bird: 0 rest, 1 up, 2 down (flapping), 3 forward like a hand; tilt turns a raised wing (waving).</summary>
		static void Wing(ShadedArt s, Color body, double cx, double cy, int mode, int shift, string tag, double tilt = 0)
		{
			switch (mode)
			{
				case 1: s.Ell(cx + 3, cy - 8, 3.6, 7.5, body, shift, tag, 18 + tilt).Ell(cx + 5.5 + tilt * 0.12, cy - 15, 2.4, 3, body, shift, tag, 18 + tilt); break;
				case 2: s.Ell(cx + 2, cy + 3.5, 8, 3.2, body, shift, tag, -15).Ell(cx + 9, cy + 6, 3, 1.8, body, shift, tag, -15); break;
				case 3: s.Ell(cx - 6.5, cy - 0.5, 8, 3.2, body, shift, tag, -8); break;
				default: s.Ell(cx + 2.5, cy - 1.5, 7.5, 5, body, shift, tag).Ell(cx + 8, cy - 3.5, 3.2, 2, body, shift, tag); break;
			}
		}

		/// <summary>The dragon's wing, a bat wing: folded on the back, up or down (flapping).</summary>
		static void DragonWing(ShadedArt s, Color c, double cx, double cy, int mode, int shift, string tag)
		{
			switch (mode)
			{
				case 1: s.Ell(cx - 1, cy - 6, 2.8, 6.5, c, shift, tag, 20).Ell(cx + 2.5, cy - 9, 2.4, 4.2, c, shift, tag, 40); break;
				case 2: s.Ell(cx + 2, cy + 3, 7, 2.6, c, shift, tag, -18).Ell(cx + 7.5, cy + 5.5, 2.6, 1.6, c, shift, tag, -18); break;
				default: s.Ell(cx + 1, cy - 1.5, 6.5, 2.8, c, shift, tag, -32).Ell(cx + 5.5, cy - 5, 2.4, 1.6, c, shift, tag, -32); break;
			}
		}

		static Bitmap Scaled(Bitmap src, int scale)
		{
			if (scale <= 1) return src;
			var big = new Bitmap(src.Width * scale, src.Height * scale, PixelFormat.Format32bppArgb);
			using (var g = Graphics.FromImage(big))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				g.DrawImage(src, new Rectangle(0, 0, big.Width, big.Height));
			}
			src.Dispose();
			return big;
		}

		// ================================================================== the frames and the pet

		/// <summary>
		/// The poses the program asks for by name when something happens (the name, its interval in ms, how many times it
		/// repeats, and how often the walk goes into it by itself: 0 = only when the program asks).
		/// </summary>
		internal static readonly (string name, int ms, string repeat, int lottery)[] Extras =
		{
			("wave", 180, "3", 2), ("cheer", 160, "3", 0), ("laugh", 130, "4", 1), ("think", 380, "2", 0), ("idea", 260, "3", 0),
			("yawn", 320, "0", 1), ("surprised", 200, "2", 0), ("scared", 90, "6", 0), ("dizzy", 180, "3", 0), ("read", 650, "random/4+4", 1),
			("eat", 230, "1", 0), ("coffee", 420, "1", 0), ("umbrella", 220, "random/6+8", 0), ("cold", 100, "10", 0), ("hot", 320, "3", 0),
			("party", 170, "4", 0), ("music", 260, "4", 1), ("cry", 220, "4", 0), ("angry", 150, "4", 0), ("look up", 500, "2", 0),
		};

		/// <summary>Every frame of the pet, and which frames each part of it uses (the roles of the assistant and the poses above).</summary>
		internal static (List<Bitmap> frames, Dictionary<string, List<int>> roles) Frames(Look k)
		{
			var poses = new List<Pose>();
			List<int> Add(params Pose[] p) { var ids = new List<int>(); foreach (var x in p) { poses.Add(x); ids.Add(poses.Count - 1); } return ids; }
			var p0 = new Pose();
			bool blob = k.Kind == Kind.Blob, flies = Flies(k.Kind);
			var roles = new Dictionary<string, List<int>>();
			var stand = Add(p0, p0 with { By = 0.6, Hy = 0.3 });
			roles["idle"] = new List<int> { stand[0], stand[0], stand[1], stand[1] };
			roles["blink"] = new List<int> { stand[0], Add(p0 with { Eye = 1 })[0], stand[0] };
			// the walk: six steps, the head bobbing forward as each foot lands; the blob hops, squashing and stretching
			roles["walk"] = blob
				? Add(p0 with { Squash = 0.86, By = 0 }, p0 with { Squash = 1.08, By = -1.5 }, p0 with { Squash = 1.12, By = -3 }, p0 with { Squash = 1.04, By = -1.5 }, p0 with { Squash = 0.9, By = 0 }, p0)
				: Add(p0 with { NfX = -2, FfX = 2, Hx = -1.2 }, p0 with { NfX = -1, FfX = 1, FfY = -1.6, By = -0.5, Hx = -0.2, Tail = -0.5 },
					p0 with { FfX = -1, FfY = -1, By = -0.9, Hx = 0.6, Tail = -1 }, p0 with { NfX = 2, FfX = -2, Hx = -1.2 },
					p0 with { NfX = 1, NfY = -1.6, FfX = -1, By = -0.5, Hx = -0.2, Tail = 0.5 }, p0 with { NfX = -1, NfY = -1, By = -0.9, Hx = 0.6, Tail = 1 });
			roles["turn"] = new List<int>();
			// a pet with wings flaps them in the air instead of being scared
			roles["fall"] = flies
				? Add(p0 with { Arm = 1, NfY = -2, FfY = -2, By = -1 }, p0 with { Arm = 2, NfY = -2, FfY = -2, By = -0.5 })
				: Add(p0 with { Eye = 3, Mouth = 1, Arm = 1, NfY = -1, By = -1, Squash = blob ? 1.1 : 1 }, p0 with { Eye = 3, Mouth = 1, Arm = 2, FfY = -1, By = -1, Squash = blob ? 0.95 : 1 });
			roles["drag"] = Add(p0 with { Eye = 3, Mouth = 1, Arm = 1, NfX = -1, NfY = -2, FfX = 1, By = -2, Squash = blob ? 1.15 : 1 }, p0 with { Eye = 3, Mouth = 1, Arm = 2, NfX = 1, FfX = -1, FfY = -2, By = -2, Squash = blob ? 1.05 : 1 });
			roles["sleep"] = Add(p0 with { Sit = true, Eye = 1, Hy = 2, Hx = 1 }, p0 with { Sit = true, Eye = 1, Hy = 2.6, Hx = 1, By = 0.4, Zz = true });
			roles["happy"] = Add(p0 with { Eye = 2, Mouth = 1, Tail = -2, By = -1, Squash = blob ? 1.08 : 1 }, p0 with { Eye = 2, Tail = 1.5, Hy = 0.8, Squash = blob ? 0.94 : 1 });
			roles["jump"] = Add(p0 with { Eye = 2, By = 1.5, Squash = 0.88 }, p0 with { Eye = 2, Mouth = 1, Arm = 1, NfY = -1, FfY = -1, By = -2, Squash = blob ? 1.12 : 1 }, p0 with { Eye = 2, Mouth = 1, Arm = 2, NfY = -2, FfY = -2, By = -1.5 });
			roles["talk"] = new List<int> { Add(p0 with { Mouth = 1 })[0], stand[0], Add(p0 with { Mouth = 1, Eye = 2, Hx = -0.5 })[0], stand[0] };
			roles["dance"] = Add(p0 with { Eye = 2, Mouth = 1, Arm = 1, NfY = -1.5, Tail = -2, Hx = -1, Squash = blob ? 1.1 : 1 }, p0 with { Eye = 2, Arm = 2, FfY = -1.5, Tail = 2, Hx = 1, Squash = blob ? 0.9 : 1 });
			roles["sit"] = Add(p0 with { Sit = true }, p0 with { Sit = true, Eye = 1 });
			roles["love"] = Add(p0 with { Eye = 4, Arm = 3 }, p0 with { Eye = 4, Arm = 3, Hy = 0.8 });

			// the poses of the program's features, with their objects
			roles["wave"] = Add(p0 with { Arm = 4, Eye = 2, Mouth = 1 }, p0 with { Arm = 4, Eye = 2, Ph = 1, Hx = -0.4 });
			roles["cheer"] = Add(p0 with { Arm = 1, Eye = 2, Mouth = 2, By = -2.5, NfY = -2, FfY = -2, Prop = Prop.Confetti, Squash = blob ? 1.1 : 1 }, p0 with { Arm = 2, Eye = 2, Mouth = 1, Prop = Prop.Confetti, Ph = 1, Squash = blob ? 0.92 : 1 });
			roles["laugh"] = Add(p0 with { Eye = 2, Mouth = 2, By = -0.8, Hy = -0.6, Tail = -1, Squash = blob ? 1.05 : 1 }, p0 with { Eye = 2, Mouth = 1, By = 0.4, Hy = 0.4, Tail = 1, Squash = blob ? 0.96 : 1 });
			roles["think"] = Add(p0 with { Prop = Prop.Thought, Hx = 0.6, Hy = -0.6, Arm = 3 }, p0 with { Prop = Prop.Thought, Ph = 1, Hx = 0.6, Hy = -0.6, Arm = 3 }, p0 with { Prop = Prop.Thought, Ph = 2, Hx = 0.6, Hy = -0.6, Arm = 3, Eye = 1 });
			roles["idea"] = Add(p0 with { Prop = Prop.Bulb, Eye = 3, Hy = -0.5 }, p0 with { Prop = Prop.Bulb, Ph = 1, Eye = 2, Mouth = 1, Arm = 1, By = -1 });
			roles["yawn"] = Add(p0 with { Eye = 6 }, p0 with { Eye = 1, Mouth = 2, Hy = -1, Arm = 1 }, p0 with { Eye = 1, Mouth = 2, Hy = -1.4, Arm = 1, By = -0.4 }, p0 with { Eye = 6, Hy = 0.4 });
			roles["surprised"] = Add(p0 with { Eye = 3, Mouth = 2, By = -1.5, Arm = 1, Prop = Prop.Exclaim, Squash = blob ? 1.1 : 1 }, p0 with { Eye = 3, Mouth = 2, Prop = Prop.Exclaim, Ph = 1 });
			roles["scared"] = Add(p0 with { Eye = 3, Mouth = 3, Hx = -0.8, By = 0.5, Prop = Prop.Sweat }, p0 with { Eye = 3, Mouth = 3, Hx = 0.8, By = 0.5, Prop = Prop.Sweat, Ph = 1, Squash = blob ? 0.94 : 1 });
			roles["dizzy"] = Add(p0 with { Eye = 5, Mouth = 3, Hx = -1, Prop = Prop.Stars }, p0 with { Eye = 5, Mouth = 1, Hx = 1, Hy = 0.5, Prop = Prop.Stars, Ph = 1 }, p0 with { Eye = 5, Mouth = 3, Hy = -0.3, Prop = Prop.Stars, Ph = 2 });
			roles["read"] = Add(p0 with { Prop = Prop.Book, Arm = 3, Hy = 1, Hx = -0.3 }, p0 with { Prop = Prop.Book, Arm = 3, Hy = 1, Hx = 0.3 }, p0 with { Prop = Prop.Book, Ph = 1, Arm = 3, Hy = 1, Eye = 2 });
			roles["eat"] = Add(p0 with { Prop = Prop.Cookie, Mouth = 1, Arm = 3 }, p0 with { Prop = Prop.Cookie, Ph = 1, Eye = 2, Arm = 3 }, p0 with { Prop = Prop.Cookie, Ph = 2, Mouth = 1, Arm = 3 }, p0 with { Prop = Prop.Cookie, Ph = 3, Eye = 2, Tail = -1 });
			roles["coffee"] = Add(p0 with { Prop = Prop.Mug, Arm = 3 }, p0 with { Prop = Prop.Mug, Ph = 1, Arm = 3, Eye = 1, Hy = -0.6 }, p0 with { Prop = Prop.Mug, Ph = 2, Arm = 3, Eye = 2 });
			roles["umbrella"] = Add(p0 with { Prop = Prop.Umbrella, Arm = 3, By = 1 }, p0 with { Prop = Prop.Umbrella, Ph = 1, Arm = 3, By = 1, Hy = 0.4 });
			roles["cold"] = Add(p0 with { Prop = Prop.Snow, Eye = 6, Mouth = 3, Hx = -0.7, By = 0.5 }, p0 with { Prop = Prop.Snow, Ph = 1, Eye = 6, Mouth = 3, Hx = 0.7, By = 0.5 });
			roles["hot"] = Add(p0 with { Prop = Prop.Sweat, Eye = 6, Mouth = 1, By = 0.6 }, p0 with { Prop = Prop.Sweat, Ph = 1, Eye = 6, Mouth = 1, By = 0.8, Hy = 0.4 });
			roles["party"] = Add(p0 with { Prop = Prop.Party, Eye = 2, Mouth = 1, Arm = 1, By = -1.5, Squash = blob ? 1.08 : 1 }, p0 with { Prop = Prop.Party, Ph = 1, Eye = 2, Arm = 2, Tail = 1.5, Squash = blob ? 0.94 : 1 });
			roles["music"] = Add(p0 with { Prop = Prop.Notes, Eye = 1, Mouth = 1, Hx = -0.8, Tail = -1.5 }, p0 with { Prop = Prop.Notes, Ph = 1, Eye = 1, Hx = 0.8, Tail = 1.5, By = 0.5 });
			roles["cry"] = Add(p0 with { Prop = Prop.Tears, Eye = 8, Mouth = 3, Hy = 0.6 }, p0 with { Prop = Prop.Tears, Ph = 1, Eye = 8, Mouth = 2, Hy = 0.9, By = 0.3 });
			roles["angry"] = Add(p0 with { Prop = Prop.Steam, Eye = 7, Mouth = 3, By = 0.4 }, p0 with { Prop = Prop.Steam, Ph = 1, Eye = 7, Mouth = 3, By = -0.4, Hx = -0.5 });
			roles["look up"] = Add(p0 with { Hy = -1.6, Hx = 1 }, p0 with { Hy = -1.6, Hx = 1, Eye = 1 });
			if (flies)
				roles["fly"] = Add(p0 with { Arm = 1, NfY = -2, FfY = -2, By = -1 }, p0 with { Arm = 0, NfY = -2, FfY = -2, By = -1.5 }, p0 with { Arm = 2, NfY = -2, FfY = -2, By = -1 }, p0 with { Arm = 0, NfY = -2, FfY = -2, By = -0.5 });
			if (k.Kind == Kind.Dragon)
				roles["fire"] = Add(p0 with { Mouth = 2, Hy = -1, Hx = 0.5, Prop = Prop.Fire }, p0 with { Mouth = 2, Hy = -1.2, Hx = 0.6, Prop = Prop.Fire, Ph = 1 }, p0 with { Mouth = 2, Hy = -1.2, Hx = 0.6, Prop = Prop.Fire, Ph = 2, Eye = 7 });

			var frames = poses.Select(p => Scaled(Draw(k, p), Math.Max(1, k.Scale))).ToList();
			return (frames, roles);
		}

		/// <summary>
		/// The new pet: the assistant's pet (walk, turn, fall, land, drag, idle, blink, sleep, happy, jump, kill), talk, dance,
		/// sit and love, the poses of the program's features (<see cref="Extras"/>), flying for the bird and the dragon, and
		/// the sounds when the look asks for them and Windows can make MP3s.
		/// </summary>
		public static PetDoc Create(Look k, string name, string author)
		{
			var (frames, roles) = Frames(k);
			var (png, cols, rows) = SheetBuilder.Build(frames, Math.Min(8, frames.Count));
			foreach (var b in frames) b.Dispose();
			var sheet = new PetDoc(new RootNode { Image = new ImageNode { Png = png, TilesX = cols, TilesY = rows, Transparency = "Magenta" } }, null);
			var picks = new[] { "idle", "walk", "turn", "fall", "drag", "blink", "sleep", "happy", "jump" }.ToDictionary(r => r, r => roles[r]);
			var doc = Wizard.Create(sheet, picks, name, author, 1.0, false);
			var root = doc.Root;
			Behaviors.Create(root, "talk", roles["talk"].ToArray());
			Behaviors.Create(root, "dance", roles["dance"].ToArray(), lottery: 4);
			Behaviors.Create(root, "sit", roles["sit"].ToArray(), lottery: 4);
			Behaviors.Create(root, "happy", roles["love"].ToArray(), name: "love", lottery: 3);
			foreach (var (n, ms, repeat, lottery) in Extras)
			{
				var a = Behaviors.Create(root, "idle", roles[n].ToArray(), name: n, lottery: lottery);
				a.Start.Interval = a.End.Interval = ms.ToString(System.Globalization.CultureInfo.InvariantCulture);
				a.Sequence.RepeatCount = repeat;
			}
			if (roles.TryGetValue("fire", out var fire))
			{
				var a = Behaviors.Create(root, "idle", fire.ToArray(), name: "fire", lottery: 1);
				a.Start.Interval = a.End.Interval = "160";
				a.Sequence.RepeatCount = "2";
			}
			if (roles.TryGetValue("fly", out var fly)) AddFlight(root, fly.ToArray());
			if (k.Sounds) foreach (var (anim, prob, mp3) in CharacterSounds.For(k))
			{
				var a = root.Animations.Animation.FirstOrDefault(x => x.Name == anim);
				if (a == null || mp3 == null) continue;
				root.Sounds.Sound = root.Sounds.Sound.Append(new SoundNode { Id = a.Id, Probability = prob, Base64 = Convert.ToBase64String(mp3) }).ToArray();
			}
			doc.Changed("all");
			return doc;
		}

		/// <summary>
		/// Flying: now and then the walk takes off; the pet goes forward flapping, a little up and then down, and comes
		/// back with the fall (for a pet with wings that is flapping slowly down to the ground). At a side of the screen it
		/// lets go too.
		/// </summary>
		static void AddFlight(RootNode root, int[] frames)
		{
			var walk = root.Animations.Animation.FirstOrDefault(a => a.Name == "walk");
			var fall = root.Animations.Animation.FirstOrDefault(a => a.Name == "fall");
			if (walk == null || fall == null) return;
			var fly = Behaviors.Create(root, "idle", frames, name: "fly", lottery: 4);
			fly.Start.X = "-4"; fly.End.X = "-4";
			fly.Start.Y = "-3"; fly.End.Y = "1";
			fly.Start.Interval = fly.End.Interval = "90";
			fly.Sequence.RepeatCount = "random/4+5";
			fly.Sequence.Next = new[] { new NextNode { Value = fall.Id, Probability = 100, OnlyFlag = "none" } };
			fly.Border = new HitNode { Next = new[] { new NextNode { Value = fall.Id, Probability = 100, OnlyFlag = "none" } } };
			// gliding down, not dropping
			fall.Start.Y = "2"; fall.End.Y = "5";
		}
	}
}
