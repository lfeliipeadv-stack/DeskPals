using System.Drawing;
using DesktopPet;
using XmlData;

namespace PetEditor
{
	/// <summary>
	/// "All animations": every animation of the pet playing at the same time, each at its own pace, in place. Pause,
	/// start all together, the size of the pictures and a search; a click opens the animation. Only what is on screen
	/// plays (and nothing while Windows' animations are off).
	/// </summary>
	internal sealed class BoardPage : Page
	{
		Board board;
		Panel scroll;
		TextBox search;
		FlatButton pauseBtn;
		readonly System.Windows.Forms.Timer timer = new() { Interval = 33 };

		public override void Bind(MainForm main, PetDoc doc)
		{
			base.Bind(main, doc);
			Controls.Clear();
			var top = Ui.Column(Ui.Title(L.T("nav.board"), 22), Ui.Hint(L.T("board.intro"), 900));
			top.Dock = DockStyle.Top;
			pauseBtn = new FlatButton(L.T("board.pause"));
			pauseBtn.Click += (s, e) => { board.Playing = !board.Playing; ShowPaused(); };
			var restart = new FlatButton(L.T("board.restart")).Tip(L.T("board.restartHint"));
			restart.Click += (s, e) => { board.Restart(); if (!board.Playing) { board.Playing = true; ShowPaused(); } };
			var size = new Choice(150);
			foreach (var k in new[] { "board.sizeS", "board.sizeM", "board.sizeL" }) size.Items.Add(L.T(k));
			size.SelectedIndex = Math.Max(0, Math.Min(2, EditorSettings.Current.BoardSize));
			size.SelectedIndexChanged += (s, e) => { EditorSettings.Current.BoardSize = size.SelectedIndex; EditorSettings.Save(); board.Tile = TileOf(size.SelectedIndex); Fit(); };
			var find = new SearchBox(L.T("anim.searchShort"), L.T("anim.search")) { Width = 240 };
			search = find.Box;
			search.TextChanged += (s, e) => { board.Filter(search.Text.Trim()); Fit(); };
			var sizeLbl = Ui.Text(L.T("board.size"), true); sizeLbl.Margin = new Padding(12, 6, 4, 0);
			var bar = Ui.Row(pauseBtn, restart, sizeLbl, size, find);
			bar.Margin = new Padding(0, 0, 0, 8);
			top.Controls.Add(bar);
			scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };
			board = new Board(main, doc) { Location = new Point(0, 0), Tile = TileOf(size.SelectedIndex) };
			board.Opened += id => Main.Go("animations", id);
			scroll.Controls.Add(board);
			scroll.Resize += (s, e) => Fit();
			Controls.Add(scroll);
			Controls.Add(top);
			board.Fill();
			Fit();
			timer.Tick += (s, e) => board.Tick(VisibleArea());
			Disposed += (s, e) => timer.Dispose();
		}

		static int TileOf(int size) => Ui.S(size switch { 0 => 72, 2 => 160, _ => 110 });

		void Fit()
		{
			if (board == null || scroll == null) return;
			board.Width = Math.Max(Ui.S(120), scroll.ClientSize.Width - Ui.S(4));
			board.Height = board.NeededHeight();
			board.Invalidate();
		}

		/// <summary>The part of the board seen in the page (in the board's own coordinates).</summary>
		Rectangle VisibleArea() => new(0, -board.Top, scroll.ClientSize.Width, scroll.ClientSize.Height);

		void ShowPaused() => pauseBtn.Text = board.Playing ? L.T("board.pause") : "▶  " + L.T("board.play");

		public override void Reload(string what)
		{
			if (board == null) return;
			board.Doc = Doc;
			board.Fill();
			board.Filter(search.Text.Trim());
			Fit();
		}

		public override void Shown() { timer.Start(); }
		public override void Hidden() { timer.Stop(); }

		/// <summary>The tiles: the animation playing, its name and number under it.</summary>
		sealed class Board : Control
		{
			public PetDoc Doc;
			readonly MainForm main;
			readonly List<LiveAnim> all = new();
			List<LiveAnim> shown = new();
			public int Tile = 110;
			public bool Playing = !LiveAnim.Still;
			public event Action<int> Opened;
			int hover = -1;

			public Board(MainForm main, PetDoc doc)
			{
				this.main = main; Doc = doc;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				BackColor = Theme.Background;
				Cursor = Cursors.Hand;
			}

			public void Fill()
			{
				all.Clear();
				long now = Environment.TickCount64;
				foreach (var a in Doc.Root.Animations.Animation) all.Add(new LiveAnim(Doc, a) { Due = now });
				shown = all.ToList();
			}

			public void Filter(string q)
			{
				shown = all.Where(l => q.Length == 0 || (l.Anim.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) || l.Anim.Id.ToString() == q).ToList();
				hover = -1;
			}

			public void Restart() { long now = Environment.TickCount64; foreach (var l in all) l.Restart(now); Invalidate(); }

			int Gap => Ui.S(12);
			int LabelH => Ui.S(38);
			int Columns => Math.Max(1, (Width - Gap) / (Tile + Gap));

			Rectangle Cell(int i)
			{
				int c = i % Columns, r = i / Columns;
				return new Rectangle(Gap + c * (Tile + Gap), Gap + r * (Tile + LabelH + Gap), Tile, Tile + LabelH);
			}

			public int NeededHeight() => shown.Count == 0 ? Ui.S(80) : Gap + (shown.Count + Columns - 1) / Columns * (Tile + LabelH + Gap);

			/// <summary>One step of the clock: the tiles on screen whose frame is due change.</summary>
			public void Tick(Rectangle seen)
			{
				if (!Playing || !Visible || LiveAnim.Still) return;
				long now = Environment.TickCount64;
				for (int i = 0; i < shown.Count; i++)
				{
					var cell = Cell(i);
					if (!cell.IntersectsWith(seen)) continue;
					if (shown[i].Tick(now)) Invalidate(new Rectangle(cell.X, cell.Y, cell.Width, Tile));
				}
			}

			int At(Point p) { for (int i = 0; i < shown.Count; i++) if (Cell(i).Contains(p)) return i; return -1; }

			protected override void OnMouseMove(MouseEventArgs e)
			{
				int h = At(e.Location);
				if (h != hover) { hover = h; Invalidate(); }
				base.OnMouseMove(e);
			}

			protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }

			protected override void OnMouseClick(MouseEventArgs e)
			{
				int i = At(e.Location);
				if (i >= 0 && e.Button == MouseButtons.Left) Opened?.Invoke(shown[i].Anim.Id);
				base.OnMouseClick(e);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics; Theme.Prepare(g);
				if (shown.Count == 0)
				{
					TextRenderer.DrawText(g, L.T(all.Count == 0 ? "board.empty" : "board.none"), Ui.Font(13), new Rectangle(Gap, Gap, Width - 2 * Gap, Ui.S(40)), Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
					return;
				}
				for (int i = 0; i < shown.Count; i++)
				{
					var cell = Cell(i);
					if (!cell.IntersectsWith(e.ClipRectangle)) continue;
					var l = shown[i];
					var box = new Rectangle(cell.X, cell.Y, Tile, Tile);
					Theme.FillRounded(g, Theme.Surface, box, 8);
					Theme.DrawRounded(g, i == hover ? Theme.Accent : Theme.Border, box, 8);
					// the frame as big as it fits (whole times when it can, so pixel art stays sharp)
					int fw = Math.Max(1, Doc.FrameW), fh = Math.Max(1, Doc.FrameH), room = Tile - Ui.S(10);
					double z = Math.Min((double)room / fw, (double)room / fh);
					if (z >= 1) z = Math.Floor(z);
					int w = (int)(fw * z), h = (int)(fh * z);
					var f = l.Anim.Sequence?.Frame;
					var img = Doc.Frame(l.HasFrames ? l.Frame : f?.Length > 0 ? f[0] : -1);
					if (img != null) Ui.DrawFrame(g, img, new Rectangle(box.X + (Tile - w) / 2, box.Y + (Tile - h) / 2, w, h), mirror: l.Flipped, opacity: l.Opacity);
					TextRenderer.DrawText(g, l.Anim.Name, Ui.Font(12.5f, i == hover), new Rectangle(cell.X, box.Bottom + Ui.S(3), Tile, Ui.S(18)), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
					TextRenderer.DrawText(g, "#" + l.Anim.Id, Ui.Font(11), new Rectangle(cell.X, box.Bottom + Ui.S(20), Tile, Ui.S(16)), Theme.TextMuted, TextFormatFlags.HorizontalCenter);
				}
			}
		}
	}
}
