using System.Drawing;
using DesktopPet;

namespace DeskPalsWeather
{
	/// <summary>One line of a page: a header, a hint, or a card with a title, a description and its controls.</summary>
	internal sealed class Row
	{
		public bool Header, Hint;
		public string Title, Desc, Badge;
		// a switch
		public Func<bool> GetOn;
		public Action<bool> SetOn;
		// a slider
		public int Min, Max;
		public Func<int> GetValue;
		public Action<int> SetValue;
		public Func<int, string> Format;
		public Action Done;
		// a list to pick from
		public Func<string[]> Items;
		public Func<int> GetIndex;
		public Action<int> SetIndex;
		// buttons, a colour
		public readonly List<(string text, Action run)> Buttons = new();
		public Func<Color?> Swatch;
		// colours to pick one of (GetIndex / SetIndex); null in it: "as DeskPals"
		public Color?[] Palette;
		// a small picture on the left of the card (the scenes)
		public Action<Graphics, Rectangle> Thumb;

		// where it was drawn (for the mouse)
		internal Rectangle Card, Toggle, Slider, List, SwatchBox;
		internal readonly List<Rectangle> ButtonBoxes = new();
		internal readonly List<Rectangle> PaletteBoxes = new();
	}

	/// <summary>A page of rows, drawn like the options of DeskPals, that scrolls.</summary>
	internal sealed class PageView : Control
	{
		List<Row> rows = new();
		int scrollY, contentHeight;
		Row sliding;
		(Row row, int part) hover = (null, -1);   // part: 0 card, 1 switch, 2 slider, 3 list, 10+ a button
		public Font Text14, Bold, Small, Header;

		public PageView()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
			MakeFonts();
		}

		float Z => DeviceDpi / 96f;
		int S(float v) => (int)(v * Z);

		void MakeFonts()
		{
			Text14?.Dispose(); Bold?.Dispose(); Small?.Dispose(); Header?.Dispose();
			Text14 = Theme.CreateFont(14 * Z); Bold = Theme.CreateFont(14.5f * Z, FontStyle.Bold); Small = Theme.CreateFont(12.5f * Z); Header = Theme.CreateFont(12 * Z, FontStyle.Bold);
		}

		protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); MakeFonts(); Invalidate(); }

		public void SetRows(List<Row> list)
		{
			bool same = rows.Count == list.Count && rows.Zip(list).All(p => p.First.Title == p.Second.Title);
			rows = list;
			if (!same) scrollY = 0;
			hover = (null, -1);
			Invalidate();
		}

		// ------------------------------------------------------------------ drawing

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			g.Clear(Theme.Background);
			int pad = S(24), y = S(20) - scrollY, w = Width - pad * 2 - S(8);
			foreach (var r in rows)
			{
				r.Toggle = r.Slider = r.List = r.SwatchBox = Rectangle.Empty;
				r.ButtonBoxes.Clear();
				r.PaletteBoxes.Clear();
				if (r.Header)
				{
					y += S(10);
					Theme.DrawText(g, r.Title.ToUpperInvariant(), Header, new Rectangle(pad + S(4), y, w, S(22)), Theme.Accent, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
					y += S(30);
					continue;
				}
				if (r.Hint)
				{
					var size = Theme.MeasureText(g, r.Desc, Small, w - S(8));
					Theme.DrawText(g, r.Desc, Small, new Rectangle(pad + S(4), y, w - S(8), size.Height + S(4)), Theme.TextMuted);
					y += size.Height + S(14);
					continue;
				}
				y += Card(g, r, pad, y, w) + S(8);
			}
			contentHeight = y + scrollY + S(16);
			if (scrollY > Math.Max(0, contentHeight - Height)) { scrollY = Math.Max(0, contentHeight - Height); Invalidate(); }
			var thumb = Thumb();
			if (!thumb.IsEmpty) Theme.FillRounded(g, Theme.Border, thumb, 3);
		}

		/// <summary>One card: the text on the left, the controls on the right (below the text when they do not fit beside it).</summary>
		int Card(Graphics g, Row r, int x, int y, int w)
		{
			int inner = S(18), gap = S(10), ch = S(34);
			// the controls, from the right
			var parts = new List<(string kind, int width)>();
			foreach (var b in r.Buttons) parts.Add(("button", TextRenderer.MeasureText(b.text, Text14).Width + S(30)));
			if (r.Items != null) parts.Add(("list", Math.Min(S(400), Math.Max(S(180), (r.Items().Select(i => TextRenderer.MeasureText(i, Text14).Width).DefaultIfEmpty(0).Max()) + S(48)))));
			if (r.GetValue != null) parts.Add(("slider", S(260)));
			if (r.GetOn != null) parts.Add(("toggle", S(46)));
			if (r.Swatch != null) parts.Add(("swatch", S(30)));
			if (r.Palette != null) parts.Add(("palette", r.Palette.Length * S(32) - S(4)));
			int controls = parts.Sum(p => p.width) + gap * Math.Max(0, parts.Count - 1);
			bool below = controls > (w - inner * 2) * 0.55;
			int textW = below || controls == 0 ? w - inner * 2 : w - inner * 2 - controls - S(16);
			int thumbW = r.Thumb != null ? S(76) : 0, left = x + inner + (thumbW > 0 ? thumbW + S(14) : 0);
			textW -= thumbW > 0 ? thumbW + S(14) : 0;
			if (r.Badge != null) textW -= TextRenderer.MeasureText(r.Badge, Small).Width + S(24);
			var titleSize = Theme.MeasureText(g, r.Title ?? "", Bold, textW);
			var descSize = string.IsNullOrEmpty(r.Desc) ? Size.Empty : Theme.MeasureText(g, r.Desc, Small, textW);
			int textH = titleSize.Height + (descSize.IsEmpty ? 0 : descSize.Height + S(4));
			int h = below ? S(16) + textH + S(12) + ch + S(16) : Math.Max(S(64), textH + S(32));
			if (thumbW > 0 && !below) h = Math.Max(h, S(48) + S(24));
			r.Card = new Rectangle(x, y, w, h);
			bool hot = hover.row == r;
			Theme.FillRounded(g, hot ? Theme.SurfaceAlt : Theme.Surface, r.Card, 10);
			Theme.DrawRounded(g, Theme.Border, r.Card, 10);
			int ty = below ? y + S(16) : y + (h - textH) / 2;
			Color ink = Theme.Text;
			Theme.DrawText(g, r.Title ?? "", Bold, new Rectangle(left, ty, textW, titleSize.Height + S(2)), ink);
			if (r.Badge != null)
			{
				var bs = TextRenderer.MeasureText(r.Badge, Small);
				var badge = new Rectangle(left + Math.Min(textW, titleSize.Width) + S(10), ty, bs.Width + S(16), S(22));
				Theme.FillRounded(g, Theme.AccentSoft, badge, S(11));
				Theme.DrawText(g, r.Badge, Small, badge, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}
			if (r.Thumb != null) r.Thumb(g, new Rectangle(x + inner, (below ? y + S(16) : y + (h - S(48)) / 2), thumbW, S(48)));
			if (!descSize.IsEmpty) Theme.DrawText(g, r.Desc, Small, new Rectangle(left, ty + titleSize.Height + S(4), textW, descSize.Height + S(2)), Theme.TextMuted);

			int cy = below ? y + h - S(16) - ch / 2 : y + h / 2;
			int right = x + w - inner;
			int bi = 0;
			foreach (var (kind, width) in parts)
			{
				var box = new Rectangle(right - width, cy - ch / 2, width, ch);
				switch (kind)
				{
					case "button":
						var bt = r.Buttons[bi];
						r.ButtonBoxes.Add(box);
						Theme.DrawButton(g, box, bt.text, Text14, false, hover.row == r && hover.part == 10 + bi);
						bi++;
						break;
					case "list":
						r.List = box;
						var items = r.Items();
						int idx = Math.Clamp(r.GetIndex(), 0, Math.Max(0, items.Length - 1));
						Theme.FillRounded(g, hover.row == r && hover.part == 3 ? Theme.Hover : Theme.SurfaceAlt, box, 6);
						Theme.DrawRounded(g, Theme.Border, box, 6);
						Theme.DrawText(g, items.Length > 0 ? items[idx] : "", Text14, new Rectangle(box.X + S(12), box.Y, box.Width - S(40), box.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
						Theme.DrawText(g, "▾", Text14, new Rectangle(box.Right - S(28), box.Y, S(20), box.Height), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
						break;
					case "slider":
						int valueW = S(52);
						r.Slider = new Rectangle(box.X + S(10), box.Y, box.Width - valueW - S(20), box.Height);
						int v = Math.Clamp(r.GetValue(), r.Min, r.Max);
						Theme.DrawSlider(g, r.Slider, v - r.Min, r.Max - r.Min);
						Theme.DrawText(g, r.Format(v), Bold, new Rectangle(box.Right - valueW, box.Y, valueW, box.Height), Theme.Text, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
						break;
					case "toggle":
						r.Toggle = new Rectangle(box.X, cy - S(12), width, S(24));
						Theme.DrawToggle(g, r.Toggle, r.GetOn());
						break;
					case "palette":
						int now = r.GetIndex();
						for (int k = 0; k < r.Palette.Length; k++)
						{
							var dot = new Rectangle(box.X + k * S(32) + S(2), cy - S(12), S(24), S(24));
							r.PaletteBoxes.Add(dot);
							if (r.Palette[k] is Color pc) using (var fill = new SolidBrush(pc)) g.FillEllipse(fill, dot);
							else DrawOwnColor(g, dot);   // "as DeskPals"
							if (k == now) using (var ring = new Pen(Theme.Text, 2f * Z)) g.DrawEllipse(ring, dot.X - S(3), dot.Y - S(3), dot.Width + S(6), dot.Height + S(6));
						}
						break;
					case "swatch":
						r.SwatchBox = new Rectangle(box.X, cy - S(14), S(28), S(28));
						var c = r.Swatch();
						if (c is Color col) Theme.FillRounded(g, col, r.SwatchBox, 6);
						else DrawOwnColor(g, r.SwatchBox);
						Theme.DrawRounded(g, Theme.Border, r.SwatchBox, 6);
						break;
				}
				right = box.X - gap;
			}
			return h;
		}

		/// <summary>The swatch of "its own colour": a diagonal of two halves.</summary>
		void DrawOwnColor(Graphics g, Rectangle r)
		{
			using var path = Theme.RoundedRect(r, 6);
			var old = g.Clip;
			g.SetClip(path);
			using var a = new SolidBrush(Theme.SurfaceAlt);
			g.FillRectangle(a, r);
			using var p = new Pen(Theme.TextMuted, 1.5f * Z);
			g.DrawLine(p, r.Left + 4, r.Bottom - 4, r.Right - 4, r.Top + 4);
			g.Clip = old;
		}

		Rectangle Thumb()
		{
			if (contentHeight <= Height || Height <= 0) return Rectangle.Empty;
			int th = Math.Max(S(40), Height * Height / contentHeight);
			int ty = (int)((Height - th) * (scrollY / (double)Math.Max(1, contentHeight - Height)));
			return new Rectangle(Width - S(10), ty, S(6), th);
		}

		// ------------------------------------------------------------------ the mouse and the keys

		(Row row, int part) HitTest(Point p)
		{
			foreach (var r in rows)
			{
				if (r.Header || r.Hint || !r.Card.Contains(p)) continue;
				if (r.Toggle.Contains(p)) return (r, 1);
				if (Rectangle.Inflate(r.Slider, 0, 4).Contains(p)) return (r, 2);
				if (r.List.Contains(p)) return (r, 3);
				for (int i = 0; i < r.ButtonBoxes.Count; i++) if (r.ButtonBoxes[i].Contains(p)) return (r, 10 + i);
				for (int i = 0; i < r.PaletteBoxes.Count; i++) if (Rectangle.Inflate(r.PaletteBoxes[i], S(3), S(3)).Contains(p)) return (r, 100 + i);
				return (r, r.GetOn != null && r.Items == null && r.Buttons.Count == 0 && r.GetValue == null ? 1 : 0);
			}
			return (null, -1);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (sliding != null) { Slide(sliding, e.X); return; }
			var h = HitTest(e.Location);
			Cursor = h.part >= 1 ? Cursors.Hand : Cursors.Default;
			if (h != hover) { hover = h; Invalidate(); }
		}

		protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (sliding == null) { hover = (null, -1); Invalidate(); } }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			if (e.Button != MouseButtons.Left) return;
			var (row, part) = HitTest(e.Location);
			if (row == null) return;
			if (part == 2) { sliding = row; Capture = true; Slide(row, e.X); }
		}

		void Slide(Row r, int x)
		{
			double k = Math.Clamp((x - r.Slider.X) / (double)Math.Max(1, r.Slider.Width), 0, 1);
			int v = r.Min + (int)Math.Round(k * (r.Max - r.Min));
			if (v != r.GetValue()) { r.SetValue(v); Invalidate(); }
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (sliding != null) { var r = sliding; sliding = null; Capture = false; r.Done?.Invoke(); return; }
			if (e.Button != MouseButtons.Left) return;
			var (row, part) = HitTest(e.Location);
			if (row == null) return;
			if (part == 1 && row.SetOn != null) row.SetOn(!row.GetOn());
			else if (part == 3) ShowList(row);
			else if (part >= 100) row.SetIndex(part - 100);
			else if (part >= 10) row.Buttons[part - 10].run();
		}

		void ShowList(Row r)
		{
			var menu = new ContextMenuStrip();
			Theme.ApplyTo(menu);
			var items = r.Items();
			int now = r.GetIndex();
			for (int i = 0; i < items.Length; i++)
			{
				int at = i;
				var item = new ToolStripMenuItem(items[i]) { Checked = i == now };
				item.Click += (s, e) => r.SetIndex(at);
				menu.Items.Add(item);
			}
			menu.Closed += (s, e) => BeginInvoke(() => menu.Dispose());
			menu.Show(this, new Point(r.List.X, r.List.Bottom + S(2)));
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			ScrollBy(-e.Delta / 120 * S(60));
		}

		void ScrollBy(int by)
		{
			int v = Math.Clamp(scrollY + by, 0, Math.Max(0, contentHeight - Height));
			if (v != scrollY) { scrollY = v; Invalidate(); }
		}

		protected override bool IsInputKey(Keys k) => k is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown || base.IsInputKey(k);

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			switch (e.KeyCode)
			{
				case Keys.Down: ScrollBy(S(40)); break;
				case Keys.Up: ScrollBy(-S(40)); break;
				case Keys.PageDown: ScrollBy(Height - S(40)); break;
				case Keys.PageUp: ScrollBy(-(Height - S(40))); break;
				case Keys.Home: ScrollBy(-contentHeight); break;
				case Keys.End: ScrollBy(contentHeight); break;
			}
		}

		/// <summary>For the pictures of the tests: scroll to a place.</summary>
		internal void ScrollTo(int y) { scrollY = 0; ScrollBy(y); }
		internal int ContentHeight => contentHeight;
	}
}
