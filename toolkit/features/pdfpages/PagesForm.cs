using System.Drawing;
using System.Drawing.Drawing2D;
using DesktopPet;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// The window of "Choose pages": the pages of a PDF as small pictures (drawn by Windows itself), to click (Ctrl and
	/// Shift for more), drag to a new place (or move with the arrows), turn and delete. It ends with "Save only the
	/// selected pages" or "Save all in this order", and gives the job its options ("order", "rotate").
	/// </summary>
	internal sealed class PagesForm : Form
	{
		readonly string pdf;
		readonly PageGrid grid;
		readonly Label count;
		readonly Button saveSelected, saveAll;
		readonly Font fText = Theme.CreateFont(13), fSmall = Theme.CreateFont(11.5f), fBold = Theme.CreateFont(13, FontStyle.Bold);
		readonly CancellationTokenSource closing = new();

		public Dictionary<string, string> Answers { get; private set; }

		/// <summary>The options chosen in the window, or null when the user canceled (or the PDF has no pages).</summary>
		public static Dictionary<string, string> Choose(IWin32Window owner, string pdf)
		{
			int pages = PdfWork.Pages(pdf);
			if (pages == 0) throw new InvalidOperationException(T.S("err.noPages"));
			using var form = new PagesForm(pdf, pages) { ShowInTaskbar = owner == null, TopMost = owner == null, StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent };
			var r = owner == null ? form.ShowDialog() : form.ShowDialog(owner);
			return r == DialogResult.OK ? form.Answers : null;
		}

		PagesForm(string pdf, int pages)
		{
			this.pdf = pdf;
			Text = T.S("pdfpages.pages.title") + " — " + Path.GetFileName(pdf);
			AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			ClientSize = new Size(820, 600);
			MinimumSize = new Size(560, 420);
			MinimizeBox = false;
			KeyPreview = true;
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);
			BackColor = Theme.Background; ForeColor = Theme.Text;
			Padding = new Padding(16, 12, 16, 12);

			var intro = new Label { Text = T.S("pdfpages.pages.intro"), Font = fText, ForeColor = Theme.TextMuted, Dock = DockStyle.Top, Height = 44, AutoSize = false };
			var tools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.Background, WrapContents = false };
			grid = new PageGrid(fBold, fSmall, pages) { Dock = DockStyle.Fill };
			var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Background, Padding = new Padding(0, 10, 0, 0), WrapContents = false };
			count = new Label { Font = fSmall, ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(0, 10, 12, 0) };

			tools.Controls.Add(Tool(T.S("pdfpages.pages.left"), () => grid.MoveSelected(-1)));
			tools.Controls.Add(Tool(T.S("pdfpages.pages.right"), () => grid.MoveSelected(+1)));
			tools.Controls.Add(Tool("🔃  " + T.S("pdfpages.pages.turn"), grid.TurnSelected));
			tools.Controls.Add(Tool("🗑️  " + T.S("pdfpages.pages.delete"), grid.DeleteSelected));
			tools.Controls.Add(Tool(T.S("pdfpages.pages.all"), grid.SelectAll));

			var cancel = Big(T.S("cancel"), false);
			cancel.DialogResult = DialogResult.Cancel;
			saveSelected = Big(T.S("pdfpages.pages.saveSelected"), true);
			saveAll = Big(T.S("pdfpages.pages.saveAll"), false);
			saveSelected.Click += (s, e) => Finish(true);
			saveAll.Click += (s, e) => Finish(false);
			bottom.Controls.Add(cancel); bottom.Controls.Add(saveSelected); bottom.Controls.Add(saveAll); bottom.Controls.Add(count);
			CancelButton = cancel;

			Controls.Add(grid);
			Controls.Add(bottom);
			Controls.Add(tools);
			Controls.Add(intro);
			ButtonLook.Hook(this);
			grid.Changed += Refresh2;
			Refresh2();
			Shown += async (s, e) => { grid.Focus(); await LoadPictures(); };
			FormClosed += (s, e) => closing.Cancel();
		}

		Button Tool(string text, Action act)
		{
			var b = new Button { Text = text, AutoSize = true, Font = fText, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text, Margin = new Padding(0, 4, 8, 4), Padding = new Padding(6, 2, 6, 2), Cursor = Cursors.Hand };
			b.FlatAppearance.BorderColor = Theme.Border;
			b.FlatAppearance.MouseOverBackColor = Theme.Hover;
			b.Click += (s, e) => { act(); grid.Focus(); };
			return b;
		}

		Button Big(string text, bool primary)
		{
			var b = new Button { Text = text, AutoSize = true, Font = primary ? fBold : fText, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(10, 3, 10, 3), Cursor = Cursors.Hand,
				BackColor = primary ? Theme.Accent : Theme.Surface, ForeColor = primary ? Theme.AccentText : Theme.Text };
			b.FlatAppearance.BorderSize = primary ? 0 : 1;
			b.FlatAppearance.BorderColor = Theme.Border;
			return b;
		}

		/// <summary>The buttons follow the choice: "Save only the selected pages" needs a page selected.</summary>
		void Refresh2()
		{
			int selected = grid.Tiles.Count(t => t.Selected);
			count.Text = T.S("pdfpages.pages.count", grid.Tiles.Count, selected);
			saveSelected.Enabled = selected > 0;
			saveAll.Enabled = grid.Tiles.Count > 0;
		}

		void Finish(bool onlySelected)
		{
			var tiles = grid.Tiles.Where(t => !onlySelected || t.Selected).ToList();
			if (tiles.Count == 0) return;
			Answers = new Dictionary<string, string> { ["order"] = PdfPick.Text(tiles.Select(t => t.Page)) };
			var turned = tiles.Where(t => t.Turn != 0).GroupBy(t => t.Page).Select(g => g.Key + ":" + g.First().Turn).ToList();
			if (turned.Count > 0) Answers["rotate"] = string.Join(",", turned);
			DialogResult = DialogResult.OK;
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keys)
		{
			switch (keys)
			{
				case Keys.Control | Keys.A: grid.SelectAll(); return true;
				case Keys.Control | Keys.Left: grid.MoveSelected(-1); return true;
				case Keys.Control | Keys.Right: grid.MoveSelected(+1); return true;
				case Keys.Delete: grid.DeleteSelected(); return true;
				case Keys.Control | Keys.R: grid.TurnSelected(); return true;
			}
			return base.ProcessCmdKey(ref msg, keys);
		}

		/// <summary>The small pictures, one by one (the window can be used meanwhile). A PDF Windows cannot draw shows numbers only.</summary>
		async Task LoadPictures()
		{
			try
			{
				var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.GetFullPath(pdf));
				var doc = await WinPdf.PdfDocument.LoadFromFileAsync(file);
				for (uint i = 0; i < doc.PageCount && !closing.IsCancellationRequested; i++)
				{
					using var page = doc.GetPage(i);
					using var ms = new InMemoryRandomAccessStream();
					var opts = new WinPdf.PdfPageRenderOptions();
					// about twice the size shown, so it stays sharp on screens with zoom
					if (page.Size.Width >= page.Size.Height) opts.DestinationWidth = (uint)(PageGrid.Thumb * 2);
					else opts.DestinationHeight = (uint)(PageGrid.Thumb * 2);
					await page.RenderToStreamAsync(ms, opts);
					if (closing.IsCancellationRequested) break;
					using var s = ms.AsStreamForRead();
					using var loaded = new Bitmap(s);
					grid.SetPicture((int)i + 1, new Bitmap(loaded));
				}
			}
			catch (Exception) { }
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { fText.Dispose(); fSmall.Dispose(); fBold.Dispose(); closing.Dispose(); }
			base.Dispose(disposing);
		}

		/// <summary>A page in the window: which page of the PDF (from 1), how much more it is turned, chosen or not.</summary>
		internal sealed class Tile
		{
			public int Page, Turn;
			/// <summary>The marks the viewer put on this page (their boxes as the page is before its turn).</summary>
			public List<Mark> Marks = new();
			public bool Selected;
		}

		/// <summary>The pages in rows, drawn here (in the theme), with selection, dragging and the keyboard.</summary>
		internal sealed class PageGrid : ScrollableControl
		{
			public const int Thumb = 130;
			const int CellW = Thumb + 28, CellH = Thumb + 52, Gap = 10;
			readonly Font bold, small;
			readonly Dictionary<int, Bitmap> pictures = new();
			public readonly List<Tile> Tiles = new();
			public event Action Changed;
			int focus, anchor;
			Point downAt;
			bool dragging, pressed;
			int dropAt = -1;

			/// <summary>The text under a page (by default "Page 3"); the viewer of pictures puts the file name.</summary>
			public Func<Tile, string> LabelOf;
			/// <summary>The page with the keyboard focus (the one the viewer shows big).</summary>
			public int FocusIndex => Math.Clamp(focus, 0, Math.Max(0, Tiles.Count - 1));

			public PageGrid(Font bold, Font small, int pages)
			{
				this.bold = bold; this.small = small;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
				BackColor = Theme.Surface;
				AutoScroll = true;
				TabStop = true;
				for (int i = 1; i <= pages; i++) Tiles.Add(new Tile { Page = i });
			}

			public void SetPicture(int page, Bitmap bmp)
			{
				if (pictures.TryGetValue(page, out var old)) old.Dispose();
				pictures[page] = bmp;
				Invalidate();
			}

			int Columns => Math.Max(1, (ClientSize.Width - Gap) / (CellW + Gap));

			Rectangle Cell(int index)
			{
				int cols = Columns, used = cols * (CellW + Gap) - Gap, left = Math.Max(Gap, (ClientSize.Width - used) / 2);
				return new Rectangle(left + index % cols * (CellW + Gap), Gap + index / cols * (CellH + Gap) + AutoScrollPosition.Y, CellW, CellH);
			}

			void Relayout()
			{
				int rows = (Tiles.Count + Columns - 1) / Columns;
				AutoScrollMinSize = new Size(0, Gap + rows * (CellH + Gap));
				Invalidate();
			}

			protected override void OnResize(EventArgs e) { base.OnResize(e); Relayout(); }

			int HitTile(Point p)
			{
				for (int i = 0; i < Tiles.Count; i++) if (Cell(i).Contains(p)) return i;
				return -1;
			}

			/// <summary>Where a page dropped at this point goes (0..count): before the tile under the mouse, or after it on its right half.</summary>
			int InsertAt(Point p)
			{
				if (Tiles.Count == 0) return 0;
				int best = Tiles.Count, bestDist = int.MaxValue;
				for (int i = 0; i < Tiles.Count; i++)
				{
					var r = Cell(i);
					if (p.Y < r.Top - Gap || p.Y > r.Bottom + Gap) continue;
					int d = Math.Abs(p.X - (r.Left + r.Width / 2));
					if (d < bestDist) { bestDist = d; best = p.X > r.Left + r.Width / 2 ? i + 1 : i; }
				}
				if (bestDist == int.MaxValue) best = p.Y < Cell(0).Top ? 0 : Tiles.Count;
				return best;
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Surface);
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				for (int i = 0; i < Tiles.Count; i++)
				{
					var r = Cell(i);
					if (!r.IntersectsWith(e.ClipRectangle)) continue;
					DrawTile(g, Tiles[i], r, i == focus && Focused);
				}
				if (dragging && dropAt >= 0)
				{
					var r = dropAt < Tiles.Count ? Cell(dropAt) : Cell(Tiles.Count - 1);
					int x = dropAt < Tiles.Count ? r.Left - Gap / 2 : r.Right + Gap / 2;
					using var pen = new Pen(Theme.Accent, 4);
					g.DrawLine(pen, x, r.Top, x, r.Bottom);
				}
			}

			void DrawTile(Graphics g, Tile t, Rectangle r, bool focused)
			{
				Theme.FillRounded(g, t.Selected ? Theme.AccentSoft : Theme.Background, r, 10);
				if (t.Selected || focused) Theme.DrawRounded(g, t.Selected ? Theme.Accent : Theme.Border, new RectangleF(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), 10, t.Selected ? 2.5f : 1f);
				var box = new Rectangle(r.X + 14, r.Y + 12, Thumb, Thumb);
				if (pictures.TryGetValue(t.Page, out var pic))
				{
					// fitted in the box, turned as the page will be
					bool side = t.Turn is 90 or 270;
					double pw = side ? pic.Height : pic.Width, ph = side ? pic.Width : pic.Height, k = Math.Min(box.Width / pw, box.Height / ph);
					float w = (float)(pic.Width * k), h = (float)(pic.Height * k);
					var state = g.Save();
					g.TranslateTransform(box.X + box.Width / 2f, box.Y + box.Height / 2f);
					g.RotateTransform(t.Turn);
					g.FillRectangle(Brushes.White, -w / 2, -h / 2, w, h);
					g.DrawImage(pic, -w / 2, -h / 2, w, h);
					using (var pen = new Pen(Theme.Border)) g.DrawRectangle(pen, -w / 2, -h / 2, w, h);
					g.Restore(state);
				}
				else
				{
					var paper = new Rectangle(box.X + box.Width / 2 - 46, box.Y + 4, 92, box.Height - 8);
					g.FillRectangle(Brushes.White, paper);
					using var pen = new Pen(Theme.Border);
					g.DrawRectangle(pen, paper);
					Theme.DrawText(g, t.Page.ToString(), bold, paper, Color.DimGray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				}
				string label = (LabelOf?.Invoke(t) ?? T.S("name.page", t.Page)) + (t.Turn != 0 ? "  ↻" : "");
				Theme.DrawText(g, label, small, new Rectangle(r.X + 4, r.Bottom - 34, r.Width - 8, 24), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				if (t.Selected)
				{
					var dot = new Rectangle(r.Right - 30, r.Y + 8, 22, 22);
					using var b = new SolidBrush(Theme.Accent);
					g.FillEllipse(b, dot);
					Theme.DrawText(g, "✓", small, dot, Theme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				}
			}

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				Focus();
				if (e.Button != MouseButtons.Left) return;
				int i = HitTile(e.Location);
				downAt = e.Location; pressed = true; dragging = false;
				if (i < 0) { if ((ModifierKeys & (Keys.Control | Keys.Shift)) == 0) { foreach (var t in Tiles) t.Selected = false; Done(); } return; }
				if ((ModifierKeys & Keys.Shift) != 0)
				{
					int a = Math.Min(anchor, i), b = Math.Max(anchor, i);
					for (int k = 0; k < Tiles.Count; k++) Tiles[k].Selected = k >= a && k <= b;
				}
				else if ((ModifierKeys & Keys.Control) != 0) { Tiles[i].Selected = !Tiles[i].Selected; anchor = i; }
				// a click on a chosen page keeps the others (they may be dragged together)
				else if (!Tiles[i].Selected) { foreach (var t in Tiles) t.Selected = false; Tiles[i].Selected = true; anchor = i; }
				focus = i;
				Done();
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				if (!pressed || e.Button != MouseButtons.Left || !Tiles.Any(t => t.Selected)) return;
				if (!dragging && (Math.Abs(e.X - downAt.X) > 6 || Math.Abs(e.Y - downAt.Y) > 6)) dragging = true;
				if (!dragging) return;
				// near the top or the bottom it scrolls
				if (e.Y < 20) AutoScrollPosition = new Point(0, -AutoScrollPosition.Y - 20);
				else if (e.Y > ClientSize.Height - 20) AutoScrollPosition = new Point(0, -AutoScrollPosition.Y + 20);
				dropAt = InsertAt(e.Location);
				Invalidate();
			}

			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);
				if (dragging && dropAt >= 0) MoveTo(dropAt);
				else if (pressed && !dragging && (ModifierKeys & (Keys.Control | Keys.Shift)) == 0)
				{
					// a plain click (not a drag) on a page among several chosen chooses only it
					int i = HitTile(e.Location);
					if (i >= 0) { foreach (var t in Tiles) t.Selected = false; Tiles[i].Selected = true; anchor = focus = i; Done(); }
				}
				pressed = dragging = false; dropAt = -1;
				Invalidate();
			}

			/// <summary>The chosen pages moved together to this place (in their order).</summary>
			void MoveTo(int at)
			{
				var moving = Tiles.Where(t => t.Selected).ToList();
				at -= Tiles.Take(at).Count(t => t.Selected);
				Tiles.RemoveAll(t => t.Selected);
				Tiles.InsertRange(Math.Clamp(at, 0, Tiles.Count), moving);
				focus = Tiles.IndexOf(moving[0]);
				Done();
			}

			public void MoveSelected(int step)
			{
				var idx = Enumerable.Range(0, Tiles.Count).Where(i => Tiles[i].Selected).ToList();
				if (idx.Count == 0) return;
				if (step < 0 && idx[0] == 0 || step > 0 && idx[^1] == Tiles.Count - 1) return;
				if (step < 0) foreach (int i in idx) (Tiles[i - 1], Tiles[i]) = (Tiles[i], Tiles[i - 1]);
				else foreach (int i in Enumerable.Reverse(idx)) (Tiles[i + 1], Tiles[i]) = (Tiles[i], Tiles[i + 1]);
				focus = Math.Clamp(focus + step, 0, Tiles.Count - 1);
				Done();
			}

			public void TurnSelected() => Turn(90);

			/// <summary>The chosen pages (or, when none is chosen, the one with the focus) turned by these degrees.</summary>
			public void Turn(int degrees)
			{
				var which = Tiles.Where(t => t.Selected).ToList();
				if (which.Count == 0 && Tiles.Count > 0) which.Add(Tiles[FocusIndex]);
				foreach (var t in which) t.Turn = ((t.Turn + degrees) % 360 + 360) % 360;
				Done();
			}

			/// <summary>Puts back pages as they were (undo, redo); the focus stays on the same place.</summary>
			public void SetTiles(IEnumerable<Tile> tiles)
			{
				Tiles.Clear();
				Tiles.AddRange(tiles);
				focus = Math.Clamp(focus, 0, Math.Max(0, Tiles.Count - 1));
				Relayout();
				Done();
			}

			/// <summary>Shows this page with the focus (and chosen alone).</summary>
			public void FocusOn(int index)
			{
				if (Tiles.Count == 0) return;
				focus = anchor = Math.Clamp(index, 0, Tiles.Count - 1);
				foreach (var t in Tiles) t.Selected = false;
				Tiles[focus].Selected = true;
				ScrollTo(focus);
				Done();
			}

			public void DeleteSelected()
			{
				int first = Tiles.FindIndex(t => t.Selected);
				if (first < 0) return;
				Tiles.RemoveAll(t => t.Selected);
				focus = Math.Clamp(first, 0, Math.Max(0, Tiles.Count - 1));
				Relayout();
				Done();
			}

			public void SelectAll()
			{
				bool all = Tiles.All(t => t.Selected);
				foreach (var t in Tiles) t.Selected = !all;
				Done();
			}

			void Done() { Invalidate(); Changed?.Invoke(); }

			protected override bool IsInputKey(Keys k) => (k & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Space || base.IsInputKey(k);

			protected override void OnKeyDown(KeyEventArgs e)
			{
				base.OnKeyDown(e);
				if (Tiles.Count == 0) return;
				int step = e.KeyCode switch { Keys.Left => -1, Keys.Right => 1, Keys.Up => -Columns, Keys.Down => Columns, _ => 0 };
				if (step != 0 && !e.Control)
				{
					focus = Math.Clamp(focus + step, 0, Tiles.Count - 1);
					if (e.Shift) { int a = Math.Min(anchor, focus), b = Math.Max(anchor, focus); for (int k = 0; k < Tiles.Count; k++) Tiles[k].Selected = k >= a && k <= b; }
					else { foreach (var t in Tiles) t.Selected = false; Tiles[focus].Selected = true; anchor = focus; }
					ScrollTo(focus);
					Done();
				}
				else if (e.KeyCode == Keys.Space) { Tiles[focus].Selected = !Tiles[focus].Selected; Done(); }
			}

			void ScrollTo(int i)
			{
				var r = Cell(i);
				if (r.Top < 0) AutoScrollPosition = new Point(0, -AutoScrollPosition.Y + r.Top - Gap);
				else if (r.Bottom > ClientSize.Height) AutoScrollPosition = new Point(0, -AutoScrollPosition.Y + r.Bottom - ClientSize.Height + Gap);
			}

			protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
			protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

			protected override void Dispose(bool disposing)
			{
				if (disposing) foreach (var b in pictures.Values) b.Dispose();
				base.Dispose(disposing);
			}
		}
	}
}
