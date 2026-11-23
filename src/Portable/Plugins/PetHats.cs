using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// Hats over any pet: a little window of its own, drawn in pixels, that sits on the top of the head drawn in the pet's
	/// picture of the moment (found from its pixels, so it works with every pet, turned to either side, and never cuts the
	/// picture). The hat is the one of a special date (<see cref="Seasons"/>) or else the one won by the pet's level
	/// (<see cref="PetLevel"/>). The window lets the mouse through and follows the pet.
	/// </summary>
	public static class PetHats
	{
		/// <summary>A hat: rows of pixels ('.' empty), the colors of each letter, and the level that wins it (0 = only on a date).</summary>
		public sealed class Hat
		{
			public string Id;
			public int Level;
			public string[] Rows;
			public Dictionary<char, Color> Colors;
			public int Width => Rows.Max(r => r.Length);
			public int Height => Rows.Length;
		}

		static readonly Color Outline = Color.FromArgb(34, 30, 40);

		static Hat H(string id, int level, string palette, params string[] rows)
		{
			// palette: "R=E8485E Y=FFD23F …"
			var colors = new Dictionary<char, Color> { ['K'] = Outline };
			foreach (var p in palette.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				colors[p[0]] = ColorTranslator.FromHtml("#" + p.Substring(2));
			return new Hat { Id = id, Level = level, Rows = rows, Colors = colors };
		}

		/// <summary>Every hat, in the order of the levels; the date hats after them.</summary>
		public static readonly Hat[] All =
		{
			H("party", 2, "R=E8485E B=46A0E8 Y=FFD23F",
				"....Y....",
				"...KYK...",
				"...KRK...",
				"..KRBRK..",
				"..KBRBK..",
				".KRBRBRK.",
				".KBRBRBK.",
				"KRBRBRBRK",
				"KKKKKKKKK"),
			H("cap", 3, "C=3C82DC W=F4F4F8",
				"....KKKKK...",
				"...KCCCCCK..",
				"..KCCWCCCCK.",
				"..KCCCCCCCK.",
				"KKKKKKKKKKK.",
				"KCCCCK......",
				"KKKKK......."),
			H("flowers", 4, "G=4CAF50 R=E8485E Y=FFD23F P=B464DC W=FFFFFF O=FF9A2A",
				"..R...Y...P..",
				".RYR.YOY.PWP.",
				"GGRGGGYGGGPGG",
				".GG.GG.GG.GG."),
			H("tophat", 6, "B=3C3C48 R=D23C3C",
				"..KKKKKK..",
				"..KBBBBK..",
				"..KBBBBK..",
				"..KBBBBK..",
				"..KRRRRK..",
				"KKKKKKKKKK",
				"KBBBBBBBBK",
				"KKKKKKKKKK"),
			H("wizard", 8, "U=465AC8 Y=FFD23F",
				"........KK...",
				".......KUK...",
				"......KUUK...",
				"......KUYK...",
				".....KUUUUK..",
				".....KYUUUK..",
				"....KUUUUYK..",
				"....KUUUUUUK.",
				"KKKKKYYYYYYKKK",
				"KUUUUUUUUUUUUK",
				"KKKKKKKKKKKKKK"),
			H("crown", 10, "Y=FFD23F R=E8485E B=46A0E8",
				"K...K...K",
				"KYKKYKKYK",
				"KYYYYYYYK",
				"KYRYBYRYK",
				"KYYYYYYYK",
				"KKKKKKKKK"),
			// only on special dates
			H("santa", 0, "R=D8323C W=FAFAFA",
				"..........WW",
				"......KKKKWW",
				".....KRRRK..",
				"....KRRRRK..",
				"...KRRRRRK..",
				"..KRRRRRRK..",
				".KWWWWWWWWK.",
				".KWWWWWWWWK.",
				"..KKKKKKKK.."),
			H("witch", 0, "P=6A3C96 O=FF9A2A",
				"......K......",
				".....KPK.....",
				".....KPPK....",
				"....KPPPK....",
				"....KPPPPK...",
				"...KPPPPPK...",
				"...KOOOOOK...",
				"KKKPPPPPPPKKK",
				"KPPPPPPPPPPPK",
				".KKKKKKKKKKK."),
			H("straw", 0, "S=E6C364 R=D8323C",
				".....KKKK.....",
				"....KSSSSK....",
				"....KRRRRK....",
				".KKKSSSSSSKKK.",
				"KSSSSSSSSSSSSK",
				".KKKKKKKKKKKK."),
			H("bunny", 0, "W=FAFAFA P=FFA0B4",
				".KK....KK.",
				"KWWK..KWWK",
				"KWPK..KWPK",
				"KWPK..KWPK",
				"KWPK..KWPK",
				".KWK..KWK.",
				"..KKKKKK..",
				"..KWWWWK..",
				"...KKKK..."),
			H("carnival", 0, "G=4CAF50 P=B464DC O=FF9A2A Y=FFD23F R=E8485E",
				"..G..P..O...",
				".GGG.PPP.OO.",
				".GG..PP..OO.",
				"..G..P...O..",
				"KKKKKKKKKKKK",
				"KYRYYBYYRYYK".Replace('B', 'P'),
				"KKKKKKKKKKKK"),
		};

		public static Hat Find(string id) => string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(h => h.Id == id);

		/// <summary>The hat the pets wear now: the date's hat, else the one won by the level, else none.</summary>
		public static Hat Current => Find(Seasons.HatNow()) ?? (PetLevel.Enabled ? Find(PetLevel.Hat) : null);

		/// <summary>Self-test: a hat put on no matter the settings.</summary>
		internal static string TestHat;

		static Timer follow;
		static readonly Dictionary<FormPet, HatWindow> windows = new Dictionary<FormPet, HatWindow>();

		/// <summary>Put on, change or take off the hats (after a setting changed, a level won, a date began).</summary>
		public static void Apply()
		{
			var hat = Find(TestHat) ?? Current;
			if (hat == null)
			{
				follow?.Stop();
				foreach (var w in windows.Values.ToList()) w.Close();
				windows.Clear();
				return;
			}
			if (follow == null) { follow = new Timer { Interval = 40 }; follow.Tick += (s, e) => Follow(); }
			follow.Start();
			Follow();
		}

		/// <summary>Self-test: the hat windows on the screen now.</summary>
		internal static IReadOnlyCollection<HatWindow> Shown => windows.Values;

		static void Follow()
		{
			var hat = Find(TestHat) ?? Current;
			if (hat == null) { Apply(); return; }
			var pets = Application.OpenForms.OfType<FormPet>().Where(p => !p.IsChildWindow && !p.IsDisposed).ToList();
			foreach (var gone in windows.Keys.Where(p => !pets.Contains(p)).ToList()) { windows[gone].Close(); windows.Remove(gone); }
			foreach (var pet in pets)
			{
				if (!windows.TryGetValue(pet, out var w) || w.IsDisposed) windows[pet] = w = new HatWindow();
				w.Place(pet, hat);
			}
		}

		// ------------------------------------------------------------------ where the head is

		/// <summary>Where the head is in a picture: the middle of the top of what is drawn (0..1 of the width) and its top row (0..1 of the height).</summary>
		internal sealed class Head { public double X, Top; public bool Found; }
		static readonly ConditionalWeakTable<Image, Head> heads = new ConditionalWeakTable<Image, Head>();

		internal static Head HeadOf(Image image)
		{
			if (image == null) return new Head();
			return heads.GetValue(image, Measure);
		}

		static Head Measure(Image image)
		{
			var head = new Head();
			if (!(image is Bitmap bmp) || bmp.Width < 2 || bmp.Height < 2) return head;
			int w = bmp.Width, h = bmp.Height;
			var px = new int[w * h];
			try
			{
				var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				try { System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, Math.Min(px.Length, data.Stride / 4 * h)); }
				finally { bmp.UnlockBits(data); }
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); return head; }
			bool Solid(int c) => ((uint)c >> 24) >= 100 && (c & 0xFFFFFF) != 0xFF00FF;
			// the first row with something drawn (two pixels at least, not a lone speck), and the rows just below it
			int top = -1;
			for (int y = 0; y < h && top < 0; y++)
			{
				int n = 0;
				for (int x = 0; x < w; x++) if (Solid(px[y * w + x])) n++;
				if (n >= 2) top = y;
			}
			if (top < 0) return head;
			int band = Math.Max(2, h / 10);
			var xs = new List<int>();
			for (int y = top; y < Math.Min(h, top + band); y++)
				for (int x = 0; x < w; x++) if (Solid(px[y * w + x])) xs.Add(x);
			xs.Sort();
			head.X = (xs[xs.Count / 2] + 0.5) / w;
			head.Top = (double)top / h;
			head.Found = true;
			return head;
		}

		// ------------------------------------------------------------------ the window of a hat

		internal sealed class HatWindow : Form
		{
			Hat hat;
			bool flipped;
			int scale = 1;

			public HatWindow()
			{
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
				BackColor = TransparencyKey = Color.Magenta;
				DoubleBuffered = true;
				TopMost = true;
				Text = "";
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					// tool window, no activate, topmost, layered, and the mouse goes through it to the pet
					cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008 | 0x00080000 | 0x00000020;
					return cp;
				}
			}

			/// <summary>Self-test: the hat this window shows.</summary>
			internal string HatId => hat?.Id;

			public void Place(FormPet pet, Hat h)
			{
				var image = pet.ShownImage;
				var head = HeadOf(image);
				bool show = head.Found && pet.Visible && !HideHotkey.Hidden && pet.Opacity > 0.5 && !pet.IsDisposed;
				if (!show) { if (Visible) Hide(); return; }
				int s = Math.Max(1, (int)Math.Round(pet.Width / 45.0));
				bool flip = !pet.FacingLeft;
				if (h != hat || s != scale || flip != flipped)
				{
					hat = h; scale = s; flipped = flip;
					Size = new Size(h.Width * s, h.Height * s);
					Invalidate();
				}
				int x = pet.Left + (int)Math.Round(head.X * pet.Width) - Width / 2;
				int y = pet.Top + (int)Math.Round(head.Top * pet.Height) - Height + 2 * s;   // the brim sits on the head
				if (Left != x || Top != y) Location = new Point(x, y);
				if (!Visible)
				{
					if (Owner != pet) Owner = pet;   // an owned window always stays above its pet
					Show();
				}
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				g.Clear(Color.Magenta);
				if (hat == null) return;
				for (int y = 0; y < hat.Rows.Length; y++)
				{
					string row = hat.Rows[y];
					for (int x = 0; x < row.Length; x++)
					{
						if (!hat.Colors.TryGetValue(row[x], out var c)) continue;
						int dx = flipped ? hat.Width - 1 - x : x;
						using (var b = new SolidBrush(c)) g.FillRectangle(b, dx * scale, y * scale, scale, scale);
					}
				}
			}
		}
	}
}
