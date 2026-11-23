using System.Drawing;
using System.Drawing.Drawing2D;
using DesktopPet;

namespace DeskPalsWeather
{
	/// <summary>
	/// The colours of the Christmas lights (like Weatherling's "Light color", in the look of DeskPals): many colours or
	/// one, a wreath of 24 bulbs (click one, then a colour, to change it; the order of the wreath is the order along the
	/// wire), brightness, how they light up, the shape of the bulbs, random colours, and Done. Every change shows at once
	/// on the windows.
	/// </summary>
	internal sealed class LightsDialog : Form
	{
		/// <summary>The colours to pick from: red, orange, yellow, green, cyan, blue, purple, pink, warm white.</summary>
		internal static readonly Color[] Swatches =
		{
			Color.FromArgb(226, 56, 66), Color.FromArgb(255, 140, 46), Color.FromArgb(232, 212, 88), Color.FromArgb(86, 196, 92), Color.FromArgb(88, 206, 206),
			Color.FromArgb(76, 124, 255), Color.FromArgb(160, 84, 214), Color.FromArgb(255, 112, 178), Color.FromArgb(255, 214, 140),
		};

		Config C => Controller.Config;
		int picked;
		bool sliding;
		Font title, text, small;

		// where things are (at 96 dots per inch; times Z, made smaller when the screen is not tall enough)
		const int Width0 = 440, Height0 = 866;
		float fit = 1;
		float Z => DeviceDpi / 96f * fit;
		int S(float v) => (int)Math.Round(v * Z);
		Rectangle Seg => new(S(60), S(58), S(320), S(34));
		Rectangle SegLeft => new(Seg.X + 3, Seg.Y + 3, Seg.Width / 2 - 3, Seg.Height - 6);
		Rectangle SegRight => new(Seg.X + Seg.Width / 2, Seg.Y + 3, Seg.Width / 2 - 3, Seg.Height - 6);
		PointF Centre => new(S(220), S(228));
		float Radius => S(104);
		const int HintY = 364;
		Rectangle Swatch(int i) => new(S(44 + i * 36), S(390), S(28), S(28));
		/// <summary>The rows under the colours: brightness, how they light up, speed, shape, distance, angles, random colours, December.</summary>
		static int RowY(int row) => 432 + row * 46;
		Rectangle Slider => new(S(200), S(RowY(0) + 8), S(200), S(20));
		Rectangle Box(int row) => new(S(200), S(RowY(row)), S(200), S(32));
		Rectangle ModeBox => Box(1);
		Rectangle SpeedBox => Box(2);
		Rectangle ShapeBox => Box(3);
		Rectangle SpacingBox => Box(4);
		Rectangle AnglesBox => Box(5);
		Rectangle ToggleAt(int row) => new(S(354), S(RowY(row) + 6), S(46), S(24));
		Rectangle ToggleRow(int row) => new(S(40), S(RowY(row)), S(360), S(36));
		Rectangle Done => new(S(160), S(RowY(8) + 10), S(120), S(36));
		static readonly int[] Gaps = { 20, 25, 30 };

		/// <summary>The window as big as the dots per inch say, or smaller so that it fits on the screen.</summary>
		void Fit()
		{
			fit = 1;
			int room = Screen.FromPoint(Cursor.Position).WorkingArea.Height - 60;
			float tall = Height0 * DeviceDpi / 96f;
			if (tall > room) fit = Math.Max(0.6f, room / tall);
			MakeFonts();
			ClientSize = new Size(S(Width0), S(Height0));
		}

		public LightsDialog()
		{
			Text = T.S("ld.title");
			FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
			Fit();
			BackColor = Theme.Surface;
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);
			FormClosed += (s, e) => { title?.Dispose(); text?.Dispose(); small?.Dispose(); Controller.Apply(); };
		}

		void MakeFonts()
		{
			title?.Dispose(); text?.Dispose(); small?.Dispose();
			title = Theme.CreateFont(17 * Z, FontStyle.Bold); text = Theme.CreateFont(14 * Z); small = Theme.CreateFont(12.5f * Z);
		}

		protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); Fit(); Invalidate(); }

		/// <summary>The 24 colours of the wreath (the order along the wire).</summary>
		List<int> Pattern()
		{
			if (C.LightsColors.Count != 24) C.LightsColors = LightsArt.DefaultPattern().Select(Controller.ToRgb).ToList();
			return C.LightsColors;
		}

		PointF BulbAt(int i, float out_ = 0)
		{
			double a = i * Math.PI * 2 / 24 - Math.PI / 2;
			return new PointF(Centre.X + (float)Math.Cos(a) * (Radius + out_), Centre.Y + (float)Math.Sin(a) * (Radius + out_));
		}

		/// <summary>Something changed: kept, and shown at once (here and on the windows).</summary>
		void Changed(bool save = true)
		{
			Controller.ApplyLights(C);
			if (save) C.Save();
			Invalidate();
		}

		// ------------------------------------------------------------------ drawing

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			g.Clear(Theme.Surface);
			var centre = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
			Theme.DrawText(g, T.S("ld.title"), title, new Rectangle(0, S(16), ClientSize.Width, S(26)), Theme.Text, centre);

			// many colours / one colour
			Theme.FillRounded(g, Theme.SurfaceAlt, Seg, S(8));
			Theme.FillRounded(g, Theme.Accent, C.LightsMulti ? SegLeft : SegRight, S(6));
			Theme.DrawText(g, T.S("ld.multi"), text, SegLeft, C.LightsMulti ? Theme.AccentText : Theme.Text, centre | TextFormatFlags.EndEllipsis);
			Theme.DrawText(g, T.S("ld.single"), text, SegRight, C.LightsMulti ? Theme.Text : Theme.AccentText, centre | TextFormatFlags.EndEllipsis);

			// the wreath: a round wire with 24 bulbs pointing out
			var c = Centre; float radius = Radius;
			using (var wire = new Pen(Color.FromArgb(245, 24, 84, 42), 2.6f * Z)) g.DrawEllipse(wire, c.X - radius, c.Y - radius, radius * 2, radius * 2);
			var pattern = Pattern();
			for (int i = 0; i < 24; i++)
			{
				var color = C.LightsMulti ? Controller.FromRgb(pattern[i]) : Controller.FromRgb(C.LightsColor);
				double a = i * Math.PI * 2 / 24 - Math.PI / 2;
				LightsArt.Bulb(g, BulbAt(i), (float)(a * 180 / Math.PI), color, 0.2f + 0.8f * C.LightsBrightness / 100f, 1.7f * Z);
			}
			if (C.LightsMulti)
			{
				var at = BulbAt(picked, S(14));
				using var mark = new Pen(Theme.Accent, 2f * Z);
				g.DrawEllipse(mark, at.X - S(17), at.Y - S(17), S(34), S(34));
			}

			// the colours
			Theme.DrawText(g, T.S(C.LightsMulti ? "ld.pick" : "ld.all"), small, new Rectangle(S(10), S(HintY), ClientSize.Width - S(20), S(18)), Theme.TextMuted, centre | TextFormatFlags.EndEllipsis);
			int chosen = C.LightsMulti ? pattern[picked] : C.LightsColor;
			for (int i = 0; i < Swatches.Length; i++)
			{
				var b = Swatch(i);
				using (var fill = new SolidBrush(Swatches[i])) g.FillEllipse(fill, b);
				if (Controller.ToRgb(Swatches[i]) == chosen) using (var ring = new Pen(Theme.Text, 2f * Z)) g.DrawEllipse(ring, b.X - S(4), b.Y - S(4), b.Width + S(8), b.Height + S(8));
			}
			var more = Swatch(Swatches.Length);
			using (var ring = new Pen(Theme.TextMuted, 1.5f * Z)) g.DrawEllipse(ring, more);
			using (var plus = Theme.CreateFont(16 * Z, FontStyle.Bold)) Theme.DrawText(g, "+", plus, more, Theme.TextMuted, centre);

			// brightness, how they light up and how fast, the shape, the distance, the angles, random colours, December
			Label(g, S(RowY(0)), "☀  " + T.S("ld.brightness"));
			Theme.DrawSlider(g, Slider, C.LightsBrightness, 100);
			Label(g, S(RowY(1)), T.S("ld.mode"));
			Box(g, ModeBox, T.S("ld.mode." + Math.Clamp(C.LightsMode, 0, 3)));
			Label(g, S(RowY(2)), T.S("ld.speed"));
			Box(g, SpeedBox, T.S("ld.speed." + Math.Clamp(C.LightsSpeed, 0, 2)));
			Label(g, S(RowY(3)), T.S("ld.shape"));
			Box(g, ShapeBox, T.S("ld.shape." + Math.Clamp(C.LightsShape, 0, 5)));
			Label(g, S(RowY(4)), T.S("ld.spacing"));
			Box(g, SpacingBox, T.S("ld.spacing.value", C.LightsSpacing is 20 or 25 or 30 ? C.LightsSpacing : 25));
			Label(g, S(RowY(5)), T.S("ld.angles"));
			Box(g, AnglesBox, T.S(C.LightsRandomAngles ? "ld.angles.1" : "ld.angles.0"));
			Label(g, S(RowY(6)), "⤮  " + T.S("ld.random"), S(300));
			Theme.DrawToggle(g, ToggleAt(6), C.LightsRandom);
			Label(g, S(RowY(7)), "🎄  " + T.S("e.lights.december"), S(300));
			Theme.DrawToggle(g, ToggleAt(7), C.LightsDecember);
			Theme.DrawButton(g, Done, T.S("ld.done"), text, true, Done.Contains(PointToClient(MousePosition)));
		}

		void Label(Graphics g, int y, string label, int width = 0) =>
			Theme.DrawText(g, label, text, new Rectangle(S(40), y, width > 0 ? width : S(156), S(36)), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

		void Box(Graphics g, Rectangle b, string value)
		{
			Theme.FillRounded(g, Theme.SurfaceAlt, b, S(6));
			Theme.DrawRounded(g, Theme.Border, b, S(6));
			Theme.DrawText(g, value, text, new Rectangle(b.X + S(12), b.Y, b.Width - S(36), b.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			Theme.DrawText(g, "▾", text, new Rectangle(b.Right - S(26), b.Y, S(18), b.Height), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
		}

		// ------------------------------------------------------------------ the mouse

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button != MouseButtons.Left) return;
			var p = e.Location;
			if (Rectangle.Inflate(Slider, 0, S(8)).Contains(p)) { sliding = true; Capture = true; Slide(p.X); return; }
			if (SegLeft.Contains(p)) { C.LightsMulti = true; Changed(); return; }
			if (SegRight.Contains(p)) { C.LightsMulti = false; Changed(); return; }
			for (int i = 0; i < 24; i++)
			{
				var at = BulbAt(i, S(12));
				if (Math.Abs(at.X - p.X) < S(16) && Math.Abs(at.Y - p.Y) < S(16)) { picked = i; if (!C.LightsMulti) { C.LightsMulti = true; Changed(); } Invalidate(); return; }
			}
			for (int i = 0; i < Swatches.Length; i++) if (Rectangle.Inflate(Swatch(i), S(3), S(3)).Contains(p)) { SetColor(Swatches[i]); return; }
			if (Rectangle.Inflate(Swatch(Swatches.Length), S(3), S(3)).Contains(p))
			{
				using var d = new ColorDialog { FullOpen = true, Color = Controller.FromRgb(C.LightsMulti ? Pattern()[picked] : C.LightsColor) };
				if (d.ShowDialog(this) == DialogResult.OK) SetColor(d.Color);
				return;
			}
			if (ModeBox.Contains(p)) { Choose(ModeBox, Enumerable.Range(0, 4).Select(i => T.S("ld.mode." + i)).ToArray(), C.LightsMode, i => C.LightsMode = i); return; }
			if (SpeedBox.Contains(p)) { Choose(SpeedBox, Enumerable.Range(0, 3).Select(i => T.S("ld.speed." + i)).ToArray(), Math.Clamp(C.LightsSpeed, 0, 2), i => C.LightsSpeed = i); return; }
			if (ShapeBox.Contains(p)) { Choose(ShapeBox, Enumerable.Range(0, 6).Select(i => T.S("ld.shape." + i)).ToArray(), C.LightsShape, i => C.LightsShape = i); return; }
			if (SpacingBox.Contains(p)) { Choose(SpacingBox, Gaps.Select(v => T.S("ld.spacing.value", v)).ToArray(), Array.IndexOf(Gaps, C.LightsSpacing), i => C.LightsSpacing = Gaps[i]); return; }
			if (AnglesBox.Contains(p)) { Choose(AnglesBox, new[] { T.S("ld.angles.0"), T.S("ld.angles.1") }, C.LightsRandomAngles ? 1 : 0, i => C.LightsRandomAngles = i == 1); return; }
			if (ToggleRow(6).Contains(p)) { C.LightsRandom = !C.LightsRandom; Changed(); return; }
			if (ToggleRow(7).Contains(p)) { C.LightsDecember = !C.LightsDecember; Controller.Apply(); Invalidate(); return; }
			if (Done.Contains(p)) Close();
		}

		void SetColor(Color color)
		{
			if (C.LightsMulti) Pattern()[picked] = Controller.ToRgb(color);
			else C.LightsColor = Controller.ToRgb(color);
			Changed();
		}

		void Choose(Rectangle box, string[] items, int now, Action<int> set)
		{
			var menu = new ContextMenuStrip();
			Theme.ApplyTo(menu);
			for (int i = 0; i < items.Length; i++)
			{
				int at = i;
				var item = new ToolStripMenuItem(items[i]) { Checked = i == now };
				item.Click += (s, e) => { set(at); Changed(); };
				menu.Items.Add(item);
			}
			menu.Closed += (s, e) => BeginInvoke(() => menu.Dispose());
			menu.Show(this, new Point(box.X, box.Bottom + S(2)));
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (sliding) { Slide(e.X); return; }
			Invalidate(Rectangle.Inflate(Done, S(4), S(4)));
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (sliding) { sliding = false; Capture = false; Changed(); }
		}

		void Slide(int x)
		{
			int v = (int)Math.Round(Math.Clamp((x - Slider.X) / (double)Slider.Width, 0, 1) * 100);
			if (v != C.LightsBrightness) { C.LightsBrightness = v; Changed(save: false); }
		}

		// ------------------------------------------------------------------ for the tests and for approval

		/// <summary>A picture of the window as it is (with many colours, then with one), side by side.</summary>
		public static void Mock(string file)
		{
			Theme.Mode = "dark";
			var keep = Controller.Config;
			Controller.Config = new Config { SavedAt = Path.Combine(Path.GetTempPath(), "DeskPalsWeather-mock.json") };
			try
			{
				using var a = Picture(true);
				Controller.Config.LightsMulti = false; Controller.Config.LightsMode = 0;
				using var b = Picture(false);
				using var both = new Bitmap(a.Width * 2 + 20, a.Height);
				using (var g = Graphics.FromImage(both)) { g.Clear(Color.FromArgb(28, 34, 58)); g.DrawImage(a, 0, 0); g.DrawImage(b, a.Width + 20, 0); }
				both.Save(file, System.Drawing.Imaging.ImageFormat.Png);
			}
			finally { Controller.Config = keep; Controller.ApplyLights(keep); }
		}

		internal static Bitmap Picture(bool multi)
		{
			Controller.ApplyLights(Controller.Config);
			using var d = new LightsDialog { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) };
			d.Show();
			Application.DoEvents();
			var bmp = new Bitmap(d.ClientSize.Width, d.ClientSize.Height);
			d.DrawToBitmap(bmp, new Rectangle(Point.Empty, d.ClientSize));
			d.Close();
			return bmp;
		}
	}
}
