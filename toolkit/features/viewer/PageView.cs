using System.Drawing;
using System.Drawing.Drawing2D;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// The big page of the viewer: a picture fitted in the space (or zoomed, with scroll bars), turned as the page will
	/// be, on the theme's background. Ctrl + wheel zooms. While a page is being drawn a short message is shown.
	/// The pictures belong to whoever gives them (they are not disposed here).
	/// </summary>
	internal sealed class PageView : ScrollableControl
	{
		static readonly double[] Steps = { 1, 1.25, 1.5, 2, 3, 4 };
		Bitmap pic;
		int turn, step;
		string message = "";
		readonly Font font;

		/// <summary>The zoom changed (the window writes it on its button).</summary>
		public event Action ZoomChanged;

		public PageView(Font font)
		{
			this.font = font;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
			BackColor = Theme.SurfaceAlt;
			AutoScroll = true;
			TabStop = true;
		}

		public Bitmap Picture => pic;
		public int Turn => turn;
		/// <summary>1 = the page fitted in the space; more is bigger.</summary>
		public double Zoom => Steps[step];
		public string Message => message;

		public void Show(Bitmap picture, int degrees)
		{
			bool same = ReferenceEquals(picture, pic) && degrees == turn;
			pic = picture; turn = ((degrees % 360) + 360) % 360; message = "";
			if (!same) Relayout();
			Invalidate();
		}

		public void Wait(string text) { message = text ?? ""; Invalidate(); }

		public void ZoomIn() => SetStep(step + 1);
		public void ZoomOut() => SetStep(step - 1);
		public void Fit() => SetStep(0);

		void SetStep(int s)
		{
			s = Math.Clamp(s, 0, Steps.Length - 1);
			if (s == step) return;
			// the middle of what is seen stays in the middle
			var size = AutoScrollMinSize;
			double cx = size.Width > 0 ? (-AutoScrollPosition.X + ClientSize.Width / 2.0) / Math.Max(1, size.Width) : 0.5;
			double cy = size.Height > 0 ? (-AutoScrollPosition.Y + ClientSize.Height / 2.0) / Math.Max(1, size.Height) : 0.5;
			step = s;
			Relayout();
			var now = AutoScrollMinSize;
			AutoScrollPosition = new Point((int)(cx * now.Width - ClientSize.Width / 2.0), (int)(cy * now.Height - ClientSize.Height / 2.0));
			Invalidate();
			ZoomChanged?.Invoke();
		}

		const int Margin_ = 16;

		/// <summary>The size the page is drawn at (turned), for this zoom.</summary>
		SizeF Drawn()
		{
			if (pic == null) return SizeF.Empty;
			bool side = turn is 90 or 270;
			double pw = side ? pic.Height : pic.Width, ph = side ? pic.Width : pic.Height;
			// the client size without scroll bars, so fitting does not depend on whether they show
			var room = new Size(Width - Margin_ * 2, Height - Margin_ * 2);
			double fit = Math.Max(0.01, Math.Min(room.Width / pw, room.Height / ph));
			double k = fit * Zoom;
			return new SizeF((float)(pw * k), (float)(ph * k));
		}

		void Relayout()
		{
			var d = Drawn();
			AutoScrollMinSize = step == 0 ? Size.Empty : new Size((int)d.Width + Margin_ * 2, (int)d.Height + Margin_ * 2);
		}

		protected override void OnResize(EventArgs e) { base.OnResize(e); Relayout(); Invalidate(); }

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			g.Clear(Theme.SurfaceAlt);
			if (pic != null)
			{
				var d = Drawn();
				float areaW = Math.Max(ClientSize.Width, AutoScrollMinSize.Width), areaH = Math.Max(ClientSize.Height, AutoScrollMinSize.Height);
				float cx = AutoScrollPosition.X + areaW / 2f, cy = AutoScrollPosition.Y + areaH / 2f;
				bool side = turn is 90 or 270;
				float w = side ? d.Height : d.Width, h = side ? d.Width : d.Height;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				var state = g.Save();
				g.TranslateTransform(cx, cy);
				// a soft shadow under the sheet
				using (var shade = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
					g.FillRectangle(shade, -d.Width / 2 + 3, -d.Height / 2 + 4, d.Width, d.Height);
				g.RotateTransform(turn);
				g.FillRectangle(Brushes.White, -w / 2, -h / 2, w, h);
				g.DrawImage(pic, -w / 2, -h / 2, w, h);
				g.Restore(state);
				DrawMarks(g, Area());
			}
			if (message.Length > 0)
			{
				var box = new Rectangle(ClientSize.Width / 2 - 140, ClientSize.Height - 54, 280, 34);
				Theme.FillRounded(g, Theme.Surface, box, 17);
				Theme.DrawText(g, message, font, box, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if ((ModifierKeys & Keys.Control) != 0) { if (e.Delta > 0) ZoomIn(); else ZoomOut(); return; }
			base.OnMouseWheel(e);
		}

		// ------------------------------------------------------------------ the marks (highlights, texts, black boxes, signatures)

		List<Mark> marks = new();
		readonly Dictionary<string, Image> signs = new();
		Point dragFrom;
		Rectangle band;
		bool banding;
		MarkKind? tool;

		/// <summary>The tool chosen: a drag on the page makes a box for it (null: none, a click picks a mark).</summary>
		public MarkKind? Tool { get => tool; set { tool = value; Cursor = value == null ? Cursors.Default : Cursors.Cross; Invalidate(); } }
		/// <summary>The mark picked (for Delete), or -1.</summary>
		public int SelectedMark { get; set; } = -1;
		/// <summary>A box was dragged with a tool: 0..1 of the page as it is shown.</summary>
		public event Action<RectangleF> BoxDrawn;
		public event Action<int> MarkPicked;

		/// <summary>The marks of the page shown, their boxes as the page is shown (turned).</summary>
		public void SetMarks(List<Mark> list) { marks = list ?? new(); if (SelectedMark >= marks.Count) SelectedMark = -1; Invalidate(); }
		internal List<Mark> MarksShown => marks;

		/// <summary>Where the page is drawn now, in the control.</summary>
		public RectangleF Area()
		{
			if (pic == null) return RectangleF.Empty;
			var d = Drawn();
			float areaW = Math.Max(ClientSize.Width, AutoScrollMinSize.Width), areaH = Math.Max(ClientSize.Height, AutoScrollMinSize.Height);
			float cx = AutoScrollPosition.X + areaW / 2f, cy = AutoScrollPosition.Y + areaH / 2f;
			return new RectangleF(cx - d.Width / 2, cy - d.Height / 2, d.Width, d.Height);
		}

		static RectangleF On(RectangleF area, RectangleF box) => new(area.X + box.X * area.Width, area.Y + box.Y * area.Height, box.Width * area.Width, box.Height * area.Height);

		void DrawMarks(Graphics g, RectangleF area)
		{
			for (int i = 0; i < marks.Count; i++)
			{
				var m = marks[i];
				var r = On(area, m.Box);
				switch (m.Kind)
				{
					case MarkKind.Highlight:
						var (hr, hg, hb) = Marks.Highlight(m.Text);
						using (var b = new SolidBrush(Color.FromArgb(96, hr, hg, hb))) g.FillRectangle(b, r);
						break;
					case MarkKind.Redact:
						g.FillRectangle(Brushes.Black, r);
						break;
					case MarkKind.Text:
						// one or more lines, each as tall as its share of the box (as on the saved page)
						var lines = (m.Text ?? "").Replace("\r", "").Split('\n');
						float lineH = r.Height / lines.Length;
						float px = Math.Clamp(lineH * 0.72f, 3, 400);
						using (var f = new Font(Theme.FontFamilyName, px, GraphicsUnit.Pixel))
						{
							float widest = lines.Max(l => g.MeasureString(l.Length == 0 ? " " : l, f).Width);
							float k = widest > r.Width && widest > 0 ? r.Width / widest : 1;
							using var fit = k < 1 ? new Font(Theme.FontFamilyName, Math.Max(3, px * k), GraphicsUnit.Pixel) : null;
							using var ink = new SolidBrush(Color.FromArgb(20, 20, 20));
							using var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
							for (int n = 0; n < lines.Length; n++)
								g.DrawString(lines[n], fit ?? f, ink, new RectangleF(r.X, r.Y + lineH * n, r.Width + 40, lineH), format);
						}
						break;
					case MarkKind.Sign:
						var img = Sign(m.Image);
						if (img == null) break;
						float s = Math.Min(r.Width / img.Width, r.Height / img.Height);
						g.DrawImage(img, r.X + (r.Width - img.Width * s) / 2, r.Y + (r.Height - img.Height * s) / 2, img.Width * s, img.Height * s);
						break;
				}
				if (i == SelectedMark)
					{
						using (var pen = new Pen(Theme.Accent, 2) { DashStyle = DashStyle.Dash }) g.DrawRectangle(pen, r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4);
						// the corner that changes the size
						using var handle = new SolidBrush(Theme.Accent);
						g.FillRectangle(handle, Handle(r));
					}
			}
			if (banding && band.Width > 0)
				using (var pen = new Pen(Theme.Accent, 1.5f) { DashStyle = DashStyle.Dash }) g.DrawRectangle(pen, band);
		}

		Image Sign(string file)
		{
			if (string.IsNullOrEmpty(file) || !File.Exists(file)) return null;
			if (!signs.TryGetValue(file, out var img))
				try { using var tmp = Image.FromFile(file); img = new Bitmap(tmp); signs[file] = img; } catch (Exception) { return null; }
			return img;
		}

		/// <summary>A new signature was saved over the old file: its picture is read again.</summary>
		public void ForgetSignatures() { foreach (var i in signs.Values) i.Dispose(); signs.Clear(); Invalidate(); }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			if (e.Button != MouseButtons.Left || pic == null) return;
			if (tool != null) { banding = true; dragFrom = e.Location; band = Rectangle.Empty; return; }
			var area = Area();
			// the picked mark: its corner resizes it, the rest of it moves it
			if (SelectedMark >= 0 && SelectedMark < marks.Count)
			{
				var r = On(area, marks[SelectedMark].Box);
				if (Handle(r).Contains(e.Location)) { moving = 2; dragFrom = e.Location; startBox = r; return; }
				if (r.Contains(e.Location)) { moving = 1; dragFrom = e.Location; startBox = r; return; }
			}
			int hit = -1;
			for (int i = marks.Count - 1; i >= 0 && hit < 0; i--) if (RectangleF.Inflate(On(area, marks[i].Box), 3, 3).Contains(e.Location)) hit = i;
			SelectedMark = hit;
			if (hit >= 0) { moving = 1; dragFrom = e.Location; startBox = On(area, marks[hit].Box); }
			Invalidate();
			MarkPicked?.Invoke(hit);
		}

		/// <summary>0: nothing; 1: the picked mark moves with the mouse; 2: its corner is pulled (its size changes).</summary>
		int moving;
		RectangleF startBox, nowBox;

		/// <summary>The little square at a mark's bottom right corner that changes its size.</summary>
		RectangleF Handle(RectangleF r) => new(r.Right - 6, r.Bottom - 6, 12, 12);

		/// <summary>A mark moved or resized: its place, and its new box (0..1 of the page as shown).</summary>
		public event Action<int, RectangleF> MarkMoved;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (moving > 0 && SelectedMark >= 0 && e.Button == MouseButtons.Left)
			{
				float dx = e.X - dragFrom.X, dy = e.Y - dragFrom.Y;
				var area = Area();
				nowBox = moving == 1 ? new RectangleF(startBox.X + dx, startBox.Y + dy, startBox.Width, startBox.Height)
					: new RectangleF(startBox.X, startBox.Y, Math.Max(8, startBox.Width + dx), Math.Max(6, startBox.Height + dy));
				// kept inside the page
				nowBox.X = Math.Clamp(nowBox.X, area.X, Math.Max(area.X, area.Right - nowBox.Width));
				nowBox.Y = Math.Clamp(nowBox.Y, area.Y, Math.Max(area.Y, area.Bottom - nowBox.Height));
				nowBox.Width = Math.Min(nowBox.Width, area.Right - nowBox.X);
				nowBox.Height = Math.Min(nowBox.Height, area.Bottom - nowBox.Y);
				marks[SelectedMark] = marks[SelectedMark] with { Box = new RectangleF((nowBox.X - area.X) / area.Width, (nowBox.Y - area.Y) / area.Height, nowBox.Width / area.Width, nowBox.Height / area.Height) };
				Invalidate();
				return;
			}
			if (tool == null && SelectedMark >= 0 && SelectedMark < marks.Count && pic != null)
			{
				var r = On(Area(), marks[SelectedMark].Box);
				Cursor = Handle(r).Contains(e.Location) ? Cursors.SizeNWSE : r.Contains(e.Location) ? Cursors.SizeAll : Cursors.Default;
			}
			if (!banding) return;
			band = Rectangle.FromLTRB(Math.Min(dragFrom.X, e.X), Math.Min(dragFrom.Y, e.Y), Math.Max(dragFrom.X, e.X), Math.Max(dragFrom.Y, e.Y));
			Invalidate();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (moving > 0)
			{
				int how = moving;
				moving = 0;
				// a click without moving is only a pick
				if (SelectedMark >= 0 && SelectedMark < marks.Count && (Math.Abs(e.X - dragFrom.X) > 2 || Math.Abs(e.Y - dragFrom.Y) > 2))
					MarkMoved?.Invoke(SelectedMark, marks[SelectedMark].Box);
				return;
			}
			if (!banding) return;
			banding = false;
			var area = Area();
			var r = RectangleF.Intersect(band, area);
			band = Rectangle.Empty;
			Invalidate();
			if (r.Width < 5 || r.Height < 5 || area.Width <= 0) return;
			BoxDrawn?.Invoke(new RectangleF((r.X - area.X) / area.Width, (r.Y - area.Y) / area.Height, r.Width / area.Width, r.Height / area.Height));
		}

		/// <summary>Tests: a box dragged with the tool, without the mouse (0..1 of the page shown).</summary>
		internal void TestDraw(RectangleF box) => BoxDrawn?.Invoke(box);

		protected override void Dispose(bool disposing)
		{
			if (disposing) foreach (var i in signs.Values) i.Dispose();
			base.Dispose(disposing);
		}
	}
}
