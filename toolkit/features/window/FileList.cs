using System.Drawing;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// The files in the window, top to bottom in the order the jobs get them (a joined PDF has its pages in this order).
	/// Each line: its number, a thumbnail, the name and, smaller, its size and folder. A file is dragged to another place
	/// (a line shows where it will go), and Alt+↑/Alt+↓ and Delete do the same from the keyboard. The list only asks:
	/// the window changes its files and gives them back with <see cref="ShowFiles"/>.
	/// </summary>
	internal sealed class FileList : ListBox
	{
		/// <summary>Move the file at (from) so that it ends at (to).</summary>
		public event Action<int, int> MoveAsked;
		/// <summary>Files dropped from Explorer, to be put at this place (0 = first; Count = last).</summary>
		public event Action<string[], int> FilesDropped;
		public event Action<int> RemoveAsked;

		const string OrderFormat = "DeskPalsTools.FileOrder";
		readonly UiFonts fonts;
		readonly Dictionary<string, string> details = new(StringComparer.OrdinalIgnoreCase);
		Point downAt;
		int downIndex = -1;
		int dropLine = -1;
		// scrolling while a file is dragged near an edge (a ListBox does not do it by itself during a drag)
		readonly System.Windows.Forms.Timer scrollTimer = new() { Interval = 150 };
		int scrollWay;

		public FileList(UiFonts fonts)
		{
			this.fonts = fonts;
			scrollTimer.Tick += ScrollTick;
			DrawMode = DrawMode.OwnerDrawFixed;
			IntegralHeight = false;
			BorderStyle = BorderStyle.None;
			SelectionMode = SelectionMode.One;
			AllowDrop = true;
			ItemHeight = Math.Min(255, Look.Px(this, 46));
			Thumbnails.Ready += ThumbnailReady;
		}

		public IReadOnlyList<string> Files => Items.Cast<string>().ToList();

		/// <summary>Show these files (in this order) and select one.</summary>
		public void ShowFiles(IList<string> files, int selected)
		{
			BeginUpdate();
			Items.Clear();
			foreach (var f in files) Items.Add(f);
			if (Items.Count > 0) SelectedIndex = Math.Clamp(selected, 0, Items.Count - 1);
			EndUpdate();
		}

		/// <summary>Where a file ends when it is put before the line at (insertAt): the place it leaves no longer counts.</summary>
		public static int Final(int from, int insertAt) => insertAt > from ? insertAt - 1 : insertAt;

		/// <summary>The line (0..Count) a drop at this point goes to: before a file's upper half, after its lower half.</summary>
		public int LineAt(Point client)
		{
			if (Items.Count == 0) return 0;
			int i = IndexFromPoint(client);
			if (i < 0) return client.Y < 0 ? 0 : Items.Count;
			var r = GetItemRectangle(i);
			return client.Y > r.Top + r.Height / 2 ? i + 1 : i;
		}

		/// <summary>A file dragged inside the list and let go before the line at (insertAt). The mouse and the tests use it.</summary>
		public void DropMove(int from, int insertAt)
		{
			if (from < 0 || from >= Items.Count) return;
			int to = Final(from, Math.Clamp(insertAt, 0, Items.Count));
			if (to != from) MoveAsked?.Invoke(from, to);
		}

		void ThumbnailReady(string path)
		{
			if (!IsHandleCreated || IsDisposed) return;
			try { BeginInvoke(() => { if (!IsDisposed) Invalidate(); }); } catch (Exception) { }
		}

		protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Look.ScrollBars(this); }

		protected override void RescaleConstantsForDpi(int oldDpi, int newDpi)
		{
			base.RescaleConstantsForDpi(oldDpi, newDpi);
			ItemHeight = Math.Min(255, ItemHeight * newDpi / Math.Max(1, oldDpi));
		}

		// ------------------------------------------------------------------ drawing

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			if (e.Index < 0 || e.Index >= Items.Count) return;
			var g = e.Graphics;
			Theme.Prepare(g);
			string path = (string)Items[e.Index];
			var r = e.Bounds;
			bool selected = (e.State & DrawItemState.Selected) != 0;
			using (var back = new SolidBrush(Theme.Surface)) g.FillRectangle(back, r);
			if (selected) Theme.FillRounded(g, Theme.AccentSoft, new RectangleF(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), Look.Px(this, 6));

			int pad = Look.Px(this, 8), num = Look.Px(this, 26), thumb = r.Height - Look.Px(this, 10);
			Theme.DrawText(g, (e.Index + 1).ToString(), fonts.Small, new Rectangle(r.X + pad / 2, r.Y, num, r.Height), Theme.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			var box = new Rectangle(r.X + pad / 2 + num + pad, r.Y + (r.Height - thumb) / 2, thumb, thumb);
			DrawThumb(g, path, box);

			int x = box.Right + pad, w = Math.Max(10, r.Right - x - pad), half = r.Height / 2;
			var flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.Bottom;
			Theme.DrawText(g, Path.GetFileName(path.TrimEnd('\\')), fonts.Text, new Rectangle(x, r.Y + Look.Px(this, 2), w, half), Theme.Text, flags);
			Theme.DrawText(g, Detail(path), fonts.Small, new Rectangle(x, r.Y + half, w, half - Look.Px(this, 2)), Theme.TextMuted, (flags & ~TextFormatFlags.Bottom) | TextFormatFlags.Top);

			// where a dragged file will land: a line above this one (or below the last one)
			int line = dropLine == e.Index ? r.Top + 1 : dropLine == Items.Count && e.Index == Items.Count - 1 ? r.Bottom - 2 : -1;
			if (line >= 0) using (var pen = new Pen(Theme.Accent, Look.Px(this, 3))) g.DrawLine(pen, r.Left + pad, line, r.Right - pad, line);
			if ((e.State & DrawItemState.Focus) != 0 && Focused && ShowFocusCues) Theme.DrawRounded(g, Theme.Accent, new RectangleF(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), Look.Px(this, 6), 1.5f);
		}

		void DrawThumb(Graphics g, string path, Rectangle box)
		{
			var bmp = Thumbnails.Get(path, Math.Max(32, box.Width * 2));
			if (bmp != null)
			{
				// fit inside the square, keeping its shape
				float k = Math.Min(box.Width / (float)bmp.Width, box.Height / (float)bmp.Height);
				int w = Math.Max(1, (int)(bmp.Width * k)), h = Math.Max(1, (int)(bmp.Height * k));
				g.DrawImage(bmp, new Rectangle(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h));
				return;
			}
			Theme.DrawText(g, Emoji(path), fonts.Section, box, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
		}

		static string Emoji(string path) =>
			Directory.Exists(path) ? "📁" : Jobs.IsImage(path) ? "🖼️" : Jobs.IsAudio(path) ? "🎵" : Jobs.IsVideo(path) ? "🎬" : Jobs.IsZip(path) ? "🗂️" : "📄";

		/// <summary>"1.2 MB · Documents" (or "Folder · Desktop"), kept so the disk is not asked at every paint.</summary>
		string Detail(string path)
		{
			if (details.TryGetValue(path, out var d)) return d;
			string folder = Path.GetFileName(Path.GetDirectoryName(path.TrimEnd('\\')) ?? "");
			string what;
			try { what = Directory.Exists(path) ? T.S("window.folder") : Look.Size(new FileInfo(path).Length); }
			catch (Exception) { what = T.S("err.missing"); }
			d = string.IsNullOrEmpty(folder) ? what : what + "  ·  " + folder;
			details[path] = d;
			return d;
		}

		// ------------------------------------------------------------------ the mouse: drag to reorder, drop from Explorer

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			downIndex = e.Button == MouseButtons.Left ? IndexFromPoint(e.Location) : -1;
			downAt = e.Location;
		}

		protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); downIndex = -1; }

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (downIndex < 0 || e.Button != MouseButtons.Left) return;
			var drag = SystemInformation.DragSize;
			if (Math.Abs(e.X - downAt.X) < drag.Width && Math.Abs(e.Y - downAt.Y) < drag.Height) return;
			int from = downIndex;
			downIndex = -1;
			DoDragDrop(new DataObject(OrderFormat, from), DragDropEffects.Move);
			SetLine(-1);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			int i = IndexFromPoint(e.Location);
			if (i >= 0) MainForm.Open((string)Items[i]);
		}

		protected override void OnDragEnter(DragEventArgs e) { base.OnDragEnter(e); DragOverHere(e); }
		protected override void OnDragOver(DragEventArgs e) { base.OnDragOver(e); DragOverHere(e); }
		protected override void OnDragLeave(EventArgs e) { base.OnDragLeave(e); scrollTimer.Stop(); scrollWay = 0; SetLine(-1); }

		void DragOverHere(DragEventArgs e)
		{
			bool mine = e.Data.GetDataPresent(OrderFormat), files = e.Data.GetDataPresent(DataFormats.FileDrop);
			e.Effect = mine ? DragDropEffects.Move : files ? DragDropEffects.Copy : DragDropEffects.None;
			var at = PointToClient(new Point(e.X, e.Y));
			// near the top or the bottom the list scrolls, so a file can be taken to a place not in view
			scrollWay = mine || files ? EdgeAt(at) : 0;
			if (scrollWay != 0 && !scrollTimer.Enabled) { DragScroll(scrollWay); scrollTimer.Start(); }
			if (scrollWay == 0) scrollTimer.Stop();
			SetLine(mine || files ? LineAt(at) : -1);
		}

		/// <summary>-1 when this point is in the top edge of the list (it scrolls up), 1 in the bottom edge, else 0.</summary>
		public int EdgeAt(Point client)
		{
			int edge = Math.Max(Look.Px(this, 12), ItemHeight / 2);
			if (client.X < 0 || client.X > ClientSize.Width) return 0;
			if (client.Y < edge && TopIndex > 0) return -1;
			if (client.Y > ClientSize.Height - edge && !LastInView) return 1;
			return 0;
		}

		/// <summary>The last file is seen whole (Windows gives no rectangle for a line out of view, so it is counted).</summary>
		public bool LastInView => Items.Count == 0 || TopIndex + ClientSize.Height / Math.Max(1, ItemHeight) >= Items.Count;

		/// <summary>Scroll one file up (-1) or down (1) while something is dragged; the timer repeats it while the mouse rests there.</summary>
		public void DragScroll(int way)
		{
			if (way == 0 || Items.Count == 0) return;
			int top = Math.Clamp(TopIndex + way, 0, Items.Count - 1);
			if (top != TopIndex) { TopIndex = top; Invalidate(); }
		}

		void ScrollTick(object sender, EventArgs e)
		{
			// the mouse button was let go somewhere else, or the edge was left: stop
			if (scrollWay == 0 || (MouseButtons & (MouseButtons.Left | MouseButtons.Right)) == 0) { scrollTimer.Stop(); return; }
			scrollWay = EdgeAt(PointToClient(MousePosition));
			if (scrollWay == 0) { scrollTimer.Stop(); return; }
			DragScroll(scrollWay);
			SetLine(LineAt(PointToClient(MousePosition)), true);
		}

		protected override void OnDragDrop(DragEventArgs e)
		{
			base.OnDragDrop(e);
			scrollTimer.Stop();
			scrollWay = 0;
			int line = LineAt(PointToClient(new Point(e.X, e.Y)));
			SetLine(-1);
			if (e.Data.GetDataPresent(OrderFormat)) DropMove((int)e.Data.GetData(OrderFormat), line);
			else if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) FilesDropped?.Invoke(paths, line);
		}

		void SetLine(int line, bool always = false)
		{
			if (line == dropLine && !always) return;
			dropLine = line;
			Invalidate();
		}

		// ------------------------------------------------------------------ the keyboard

		protected override void OnKeyDown(KeyEventArgs e)
		{
			int i = SelectedIndex;
			if (i >= 0 && e.Alt && e.KeyCode == Keys.Up) { if (i > 0) MoveAsked?.Invoke(i, i - 1); e.Handled = e.SuppressKeyPress = true; return; }
			if (i >= 0 && e.Alt && e.KeyCode == Keys.Down) { if (i < Items.Count - 1) MoveAsked?.Invoke(i, i + 1); e.Handled = e.SuppressKeyPress = true; return; }
			if (i >= 0 && e.KeyCode == Keys.Delete) { RemoveAsked?.Invoke(i); e.Handled = e.SuppressKeyPress = true; return; }
			if (i >= 0 && e.KeyCode == Keys.Enter) { MainForm.Open((string)Items[i]); e.Handled = e.SuppressKeyPress = true; return; }
			base.OnKeyDown(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { Thumbnails.Ready -= ThumbnailReady; scrollTimer.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
