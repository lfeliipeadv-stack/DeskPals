using DesktopPet;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PetEditor
{
	/// <summary>
	/// "Create a character": a pet drawn from shapes, in the style of the Pato pet. Kind, three colors (with a dice for
	/// colors that go well together), an accessory, a size and a name; the preview walks on a light and a dark desktop and
	/// shows the other poses. "Create pet" builds every frame and animation (see <see cref="CharacterMaker"/>) and opens
	/// it in the editor, to change whatever you like from there.
	/// </summary>
	internal static class CharacterDialog
	{
		static readonly (Color body, Color detail, Color accent)[] Dice =
		{
			(Color.FromArgb(255, 216, 74), Color.FromArgb(255, 154, 42), Color.FromArgb(76, 175, 80)),
			(Color.FromArgb(242, 242, 246), Color.FromArgb(255, 160, 180), Color.FromArgb(214, 60, 60)),
			(Color.FromArgb(132, 150, 190), Color.FromArgb(255, 196, 150), Color.FromArgb(150, 100, 210)),
			(Color.FromArgb(160, 108, 66), Color.FromArgb(232, 190, 140), Color.FromArgb(60, 130, 220)),
			(Color.FromArgb(255, 170, 200), Color.FromArgb(255, 255, 255), Color.FromArgb(70, 160, 230)),
			(Color.FromArgb(120, 210, 170), Color.FromArgb(255, 130, 120), Color.FromArgb(255, 210, 60)),
			(Color.FromArgb(70, 70, 82), Color.FromArgb(255, 200, 80), Color.FromArgb(214, 60, 60)),
			(Color.FromArgb(240, 150, 70), Color.FromArgb(255, 236, 200), Color.FromArgb(90, 176, 76)),
		};

		public static void Show(MainForm main)
		{
			if (!main.ConfirmDiscard()) return;
			var look = new CharacterMaker.Look();
			int dice = 0;

			using var f = new Form { Text = L.T("char.title"), Size = new Size(1000, 760), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, ShowInTaskbar = false, BackColor = Theme.Background, Font = Ui.Font(13) };
			Theme.ApplyWindowChrome(f);
			Ui.AutoFit(f);
			var head = new Label { Dock = DockStyle.Top, Height = 60, Text = L.T("char.title"), Font = Ui.Font(18, true), ForeColor = Theme.Text, BackColor = Theme.Surface, Padding = new Padding(18, 16, 0, 0) };
			var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Surface, Padding = new Padding(14, 10, 14, 0) };
			var create = new FlatButton(L.T("char.create"), primary: true);
			var cancel = new FlatButton(L.T("build.cancel")); cancel.Click += (s, e) => f.Close();
			bar.Controls.AddRange(new Control[] { create, cancel });
			var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Theme.Background, Padding = new Padding(14) };
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			var preview = new CharacterPreview { Dock = DockStyle.Fill };
			void Changed() => preview.SetLook(look);

			// the choices, on the left
			var kind = Ui.Combo(250);
			kind.Items.AddRange(new object[] { L.T("char.duck"), L.T("char.cat"), L.T("char.bear"), L.T("char.bunny"), L.T("char.blob"), L.T("char.dog"), L.T("char.bird"), L.T("char.dragon") });
			kind.SelectedIndex = 0;
			var swBody = new Swatch(L.T("char.body"), look.Body);
			var swDetail = new Swatch(L.T("char.detail"), look.Detail);
			var swAccent = new Swatch(L.T("char.accent"), look.Accent);
			void SetColors((Color body, Color detail, Color accent) c)
			{
				look.Body = swBody.Color = c.body; look.Detail = swDetail.Color = c.detail; look.Accent = swAccent.Color = c.accent;
				Changed();
			}
			kind.SelectedIndexChanged += (s, e) => { look.Kind = (CharacterMaker.Kind)kind.SelectedIndex; SetColors(CharacterMaker.Palette(look.Kind)); };
			swBody.Picked += c => { look.Body = c; Changed(); };
			swDetail.Picked += c => { look.Detail = c; Changed(); };
			swAccent.Picked += c => { look.Accent = c; Changed(); };
			var roll = new FlatButton("🎲  " + L.T("char.dice"));
			roll.Click += (s, e) => { dice = (dice + 1) % Dice.Length; SetColors(Dice[dice]); };
			var extra = Ui.Combo(250);
			extra.Items.AddRange(new object[] { L.T("char.none"), L.T("char.hat"), L.T("char.bow"), L.T("char.scarf"), L.T("char.glasses"), L.T("char.backpack") });
			extra.SelectedIndex = (int)look.Extra;
			extra.SelectedIndexChanged += (s, e) => { look.Extra = (CharacterMaker.Extra)extra.SelectedIndex; Changed(); };
			var size = Ui.Combo(250);
			size.Items.AddRange(new object[] { L.T("char.small"), L.T("char.medium"), L.T("char.large") });
			size.SelectedIndex = 0;
			size.SelectedIndexChanged += (s, e) => look.Scale = size.SelectedIndex + 1;
			var name = Ui.Box(L.T("char.defaultName"), 250);
			var author = Ui.Box(Environment.UserName, 250);
			// sounds made here, with the encoder of Windows (some editions of Windows have none: then the pet has no sounds)
			bool canSound = Synth.CanEncode;
			look.Sounds = canSound;
			var sounds = new Toggle(L.T("char.sounds"), canSound) { Enabled = canSound, MaximumSize = new Size(360, 0) };
			sounds.Changed += (s, e) => look.Sounds = sounds.On;
			var listen = new FlatButton("▶  " + L.T("char.listen")) { Enabled = canSound };
			listen.Click += (s, e) =>
			{
				try { var mp3 = CharacterSounds.Sample(look); if (mp3 != null) Mp3Player.Load(mp3).Play(0.8, 0); }
				catch (Exception ex) { main.Message(ex.Message); }
			};

			var left = Ui.Column(
				Ui.Hint(L.T("char.intro"), 360),
				Ui.Labeled(L.T("char.kind"), kind),
				Ui.Row(swBody, swDetail, swAccent),
				roll,
				Ui.Labeled(L.T("char.extra"), extra),
				Ui.Labeled(L.T("char.size"), size, L.T("char.sizeHint")),
				sounds,
				Ui.Row(listen),
				Ui.Hint(L.T(canSound ? "char.soundsHint" : "char.noEncoder"), 360),
				Ui.Labeled(L.T("head.petname"), name),
				Ui.Labeled(L.T("head.author"), author));
			left.Location = new Point(16, 14);
			var leftCard = new Card { Dock = DockStyle.Fill };
			leftCard.AutoScroll = true;
			leftCard.Controls.Add(left);
			var rightCard = new Card { Dock = DockStyle.Fill };
			rightCard.Controls.Add(preview);
			grid.Controls.Add(leftCard, 0, 0);
			grid.Controls.Add(rightCard, 1, 0);
			f.Controls.Add(grid); f.Controls.Add(bar); f.Controls.Add(head);

			create.Click += (s, e) =>
			{
				try
				{
					var doc = CharacterMaker.Create(look.Clone(), string.IsNullOrWhiteSpace(name.Text) ? L.T("char.defaultName") : name.Text.Trim(), author.Text.Trim());
					f.Close();
					main.SetDoc(doc, "animations");
				}
				catch (Exception ex) { main.Message(ex.Message); }
			};
			Changed();
			f.ShowDialog(main);
			preview.Stop();
		}

		/// <summary>A color to pick: the color and its name; a click opens the color picker.</summary>
		sealed class Swatch : Control
		{
			Color color;
			bool hover;
			public event Action<Color> Picked;
			public Color Color { get => color; set { color = value; Invalidate(); } }

			public Swatch(string text, Color c)
			{
				Text = text; color = c;
				Size = new Size(104, 64);
				Margin = new Padding(0, 4, 8, 4);
				Cursor = Cursors.Hand;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics; Theme.Prepare(g);
				g.Clear(Theme.Surface);
				var box = new Rectangle(Ui.S(4), Ui.S(4), Width - Ui.S(8), Ui.S(30));
				using (var b = new SolidBrush(color)) g.FillRectangle(b, box);
				using (var p = new Pen(hover ? Theme.Accent : Theme.Border, hover ? 2 : 1)) g.DrawRectangle(p, box);
				TextRenderer.DrawText(g, Text, Ui.Font(12), new Rectangle(0, box.Bottom + Ui.S(4), Width, Ui.S(20)), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
			}

			protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
			protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

			protected override void OnClick(EventArgs e)
			{
				base.OnClick(e);
				using var dlg = new ColorDialog { Color = color, FullOpen = true, AnyColor = true };
				if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
				Color = dlg.Color;
				Picked?.Invoke(dlg.Color);
			}
		}

		/// <summary>The character walking on a light and a dark desktop, and its other poses below (each one moving).</summary>
		sealed class CharacterPreview : Control
		{
			readonly System.Windows.Forms.Timer timer = new() { Interval = 110 };
			List<Bitmap> walk = new();
			List<List<Bitmap>> poses = new();
			int step, x;

			/// <summary>The poses shown below the desktops: the program's features first (they are what is new).</summary>
			static readonly string[] Shown = { "wave", "think", "read", "eat", "coffee", "umbrella", "party", "music", "cold", "angry", "cry", "fly", "fire", "sleep", "love", "dance" };

			public CharacterPreview()
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				timer.Tick += (s, e) => { step++; x += Ui.S(4); Invalidate(); };
				timer.Start();
			}

			void Free() { foreach (var b in walk.Concat(poses.SelectMany(p => p))) b.Dispose(); }

			public void SetLook(CharacterMaker.Look look)
			{
				var small = look.Clone();
				small.Scale = 1;   // the preview enlarges by itself
				Free();
				var (frames, roles) = CharacterMaker.Frames(small);
				walk = roles["walk"].Select(i => (Bitmap)frames[i].Clone()).ToList();
				poses = Shown.Where(roles.ContainsKey).Select(r => roles[r].Select(i => (Bitmap)frames[i].Clone()).ToList()).ToList();
				foreach (var b in frames) b.Dispose();
				Invalidate();
			}

			public void Stop() { timer.Stop(); timer.Dispose(); }

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				g.Clear(Theme.Background);
				if (walk.Count == 0) return;
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				int z = Math.Max(2, Math.Min(4, (Width / 2 - Ui.S(20)) / 60));
				int fw = walk[0].Width * z, fh = walk[0].Height * z;
				int stageH = fh + Ui.S(24);
				// two desktops: light and dark; the pet walks to the left and comes back from the right
				var stages = new[] { (new Rectangle(0, 0, Width / 2 - Ui.S(6), stageH), Color.FromArgb(240, 238, 232)), (new Rectangle(Width / 2 + Ui.S(6), 0, Width / 2 - Ui.S(6), stageH), Color.FromArgb(32, 33, 36)) };
				var frame = walk[step % walk.Count];
				foreach (var (r, back) in stages)
				{
					using (var b = new SolidBrush(back)) g.FillRectangle(b, r);
					int span = r.Width + fw;
					int px = r.Right - (x % span);
					var clip = g.Clip;
					g.SetClip(r);
					g.DrawImage(frame, new Rectangle(px, r.Bottom - fh - Ui.S(8), fw, fh));
					g.Clip = clip;
				}
				// the other poses, smaller, in rows, each one playing its frames
				int pz = Math.Max(1, z - 1), pw = walk[0].Width * pz, ph = walk[0].Height * pz;
				int y = stageH + Ui.S(16), px0 = 0;
				foreach (var p in poses)
				{
					if (px0 + pw > Width) { px0 = 0; y += ph + Ui.S(10); }
					using (var b = new SolidBrush(Theme.Surface)) g.FillRectangle(b, px0, y, pw, ph);
					g.DrawImage(p[(step / 3) % p.Count], new Rectangle(px0, y, pw, ph));
					px0 += pw + Ui.S(10);
				}
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing) { timer.Dispose(); Free(); }
				base.Dispose(disposing);
			}
		}
	}
}
