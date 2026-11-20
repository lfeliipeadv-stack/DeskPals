using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPet
{
	public sealed partial class FormSettings
	{

		// ================================================================== Reminders page

		/// <summary>List of reminders: on/off, what and when, test, edit, delete; plus "remind me in N min".</summary>
		private sealed class RemindersView : ScrollSurface
		{
			private readonly FormSettings form;
			private readonly List<(Rectangle rect, object id)> hits = new List<(Rectangle, object)>();
			private object hover;
			static readonly int[] Quick = { 5, 10, 15, 30, 60 };

			public RemindersView(FormSettings f)
			{
				form = f;
				Reminders.Changed += OnChanged;
				Disposed += (s, e) => Reminders.Changed -= OnChanged;
			}

			void OnChanged() { if (!IsDisposed) { if (InvokeRequired) BeginInvoke(new Action(Invalidate)); else Invalidate(); } }

			protected override int PaintContent(Graphics g, int scrollY)
			{
				hits.Clear();
				int w = Width - ScrollBarWidth - 16;
				int y = 8 - scrollY;

				// --- actions: new reminder + quick timers
				var add = new Rectangle(8, y, Theme.MeasureText(g, Lang.T("rem.new"), form.fontBold, int.MaxValue, TextFormatFlags.SingleLine).Width + 40, 36);
				Theme.DrawButton(g, add, Lang.T("rem.new"), form.fontBold, true, Equals(hover, "new"));
				hits.Add((add, "new"));
				int x = add.Right + 24;
				string quick = Lang.T("rem.quick");
				int qw = Theme.MeasureText(g, quick, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width;
				Theme.DrawText(g, quick, form.fontSmall, new Rectangle(x, y, qw + 4, 36), Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				x += qw + 10;
				foreach (int m in Quick)
				{
					string t = Lang.T("rem.minutesShort", m);
					int cw = Theme.MeasureText(g, t, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 24;
					if (x + cw > w) break;
					var chip = new Rectangle(x, y + 5, cw, 26);
					Theme.FillRounded(g, Equals(hover, m) ? Theme.Hover : Theme.Surface, chip, 13);
					Theme.DrawRounded(g, Theme.Border, chip, 13);
					Theme.DrawText(g, t, form.fontSmall, chip, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					hits.Add((chip, m));
					x += cw + 6;
				}
				y += 52;

				var list = Reminders.All;
				if (list.Count == 0)
				{
					Theme.DrawText(g, Lang.T("rem.empty"), form.fontText, new Rectangle(8, y + 20, w, 40), Theme.TextMuted, TextFormatFlags.HorizontalCenter);
					return y + 80 + scrollY;
				}

				var now = DateTime.Now;
				foreach (var r in list)
				{
					var card = new Rectangle(8, y, w, 76);
					Theme.FillRounded(g, Equals(hover, r) ? Theme.SurfaceAlt : Theme.Surface, card, 10);
					Theme.DrawRounded(g, Theme.Border, card, 10);

					var toggle = new Rectangle(card.X + 18, card.Y + 27, 44, 22);
					Theme.DrawToggle(g, toggle, r.Enabled);
					hits.Add((Rectangle.Inflate(toggle, 6, 8), ("toggle", r)));

					// buttons on the right
					int bx = card.Right - 14;
					var buttons = new[] { ("delete", "rem.delete"), ("edit", "rem.edit"), ("test", "rem.test") };
					foreach (var b in buttons)
					{
						string t = Lang.T(b.Item2);
						int bw = Theme.MeasureText(g, t, form.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 22;
						bx -= bw;
						var br = new Rectangle(bx, card.Y + 23, bw, 30);
						Theme.DrawButton(g, br, t, form.fontSmall, false, Equals(hover, (b.Item1, r)));
						hits.Add((br, (b.Item1, r)));
						bx -= 6;
					}

					int tx = toggle.Right + 18, tw = bx - tx - 10;
					Theme.DrawText(g, r.Text, form.fontBold, new Rectangle(tx, card.Y + 14, tw, 22), r.Enabled ? Theme.Text : Theme.TextMuted,
						TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
					string sub = r.Summary();
					var next = r.Enabled ? r.Next(r.LastFired > now.AddDays(-1) ? r.LastFired : now) : null;
					if (next != null) sub += "  ·  " + Lang.T("rem.next", NextText(next.Value, now));
					Theme.DrawText(g, sub, form.fontSmall, new Rectangle(tx, card.Y + 40, tw, 20), Theme.TextMuted,
						TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
					hits.Add((card, r));
					y += card.Height + 10;
				}
				return y + 10 + scrollY;
			}

			static string NextText(DateTime t, DateTime now)
			{
				string hm = t.ToString("t", CultureInfo.CurrentCulture);
				if (t.Date == now.Date) return Lang.T("rem.today", hm);
				if (t.Date == now.Date.AddDays(1)) return Lang.T("rem.tomorrow", hm);
				return t.ToString("g", CultureInfo.CurrentCulture);
			}

			object HitAt(Point p)
			{
				// the most specific first: buttons and toggles were added before their card
				foreach (var h in hits) if (h.rect.Contains(p) && !(h.id is Reminder)) return h.id;
				foreach (var h in hits) if (h.rect.Contains(p)) return h.id;
				return null;
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var h = HitAt(e.Location);
				Cursor = h != null && !(h is Reminder) ? Cursors.Hand : Cursors.Default;
				if (!Equals(h, hover)) { hover = h; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; Invalidate(); }

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				switch (HitAt(e.Location))
				{
					case "new":
						using (var dlg = new FormReminder(null))
							if (dlg.ShowDialog(form) == DialogResult.OK) { Reminders.Add(dlg.Result); form.StatusText = Lang.T("rem.saved"); }
						break;
					case int minutes:
						Reminders.In(minutes);
						form.StatusText = Lang.T("rem.timerSet", minutes);
						break;
					case ValueTuple<string, Reminder> act:
						var r = act.Item2;
						switch (act.Item1)
						{
							case "toggle":
								r.Enabled = !r.Enabled;
								if (r.Enabled) { r.LastFired = DateTime.Now; if (r.Kind == Reminder.Kinds.Once && r.At < DateTime.Now) r.At = DateTime.Now.AddMinutes(10); }
								Reminders.Save();
								break;
							case "test": Reminders.Fire(r); break;
							case "edit":
								using (var dlg = new FormReminder(r))
									if (dlg.ShowDialog(form) == DialogResult.OK) { Reminders.Save(); form.StatusText = Lang.T("rem.saved"); }
								break;
							case "delete":
								if (PetMessage.Show(form, Lang.T("rem.deleteConfirm", r.Text), Lang.T("rem.delete"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
									Reminders.Remove(r);
								break;
						}
						break;
				}
				Invalidate();
			}

			protected override void OnMouseDoubleClick(MouseEventArgs e)
			{
				base.OnMouseDoubleClick(e);
				if (HitAt(e.Location) is Reminder r)
					using (var dlg = new FormReminder(r))
						if (dlg.ShowDialog(form) == DialogResult.OK) Reminders.Save();
			}
		}
	}

	/// <summary>Create or edit a reminder: what the pet says, when (repeat · every day · once) and on which days.</summary>
	internal sealed class FormReminder : ThemedDialog
	{
		public Reminder Result { get; private set; }
		readonly Reminder editing;
		readonly TextBox text = new TextBox { MaxLength = 200, BorderStyle = BorderStyle.None };
		// the choices of the program's look (no classic spin boxes): every N minutes, from/to hour, the time of day
		readonly PetStepper minutes = new PetStepper { Width = 150, Values = new[] { 1, 2, 3, 5, 10, 15, 20, 25, 30, 40, 45, 50, 60, 75, 90, 120, 150, 180, 240, 300, 360, 480, 720, 1440 }, Format = Pomodoro.Minutes };
		readonly PetStepper from = new PetStepper { Width = 100, Values = Enumerable.Range(0, 24).ToArray(), Format = h => h + "h" };
		readonly PetStepper to = new PetStepper { Width = 100, Values = Enumerable.Range(1, 24).ToArray(), Format = h => h + "h" };
		/// <summary>Minutes since midnight, every 5 minutes (and the exact time of a reminder made before).</summary>
		readonly PetStepper time = new PetStepper { Width = 120, Values = Enumerable.Range(0, 288).Select(i => i * 5).ToArray(), Format = m => $"{m / 60:00}:{m % 60:00}" };
		readonly DateTimePicker at = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy  HH:mm", Width = 180 };
		readonly FlowLayoutPanel rowInterval, rowDaily, rowOnce;
		readonly bool[] dayOn = new bool[7];
		static readonly int[] DayOrder = { 1, 2, 3, 4, 5, 6, 0 };   // Monday first
		Reminder.Kinds kind;
		Rectangle textField, card;
		Rectangle[] segs = new Rectangle[3], dayRects = new Rectangle[7];
		int whenTop, daysTop;
		const int W = 560;

		public FormReminder(Reminder r)
		{
			editing = r;
			var model = r ?? new Reminder { Text = "", Days = 127 };
			Text = Lang.T(r == null ? "rem.new" : "rem.editTitle");
			iconEmoji = "⏰";
			var ok = AddButton(Lang.T("rem.save"), DialogResult.None, true);
			AddButton(Lang.T("rem.cancel"), DialogResult.Cancel, false);

			FlowLayoutPanel Row(params Control[] cs)
			{
				var p = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
				foreach (var c in cs) { c.Margin = new Padding(0, 4, 8, 0); c.Font = fText; p.Controls.Add(c); }
				return p;
			}
			Label T(string key) => new Label { Text = Lang.T(key), AutoSize = true, Padding = new Padding(0, 9, 0, 0) };
			text.Text = model.Text;
			text.Font = fText;
			Controls.Add(text);
			rowInterval = Row(T("rem.every"), minutes, T("rem.from"), from, T("rem.to"), to);
			// two lines: "every [1 h]" / "from [9h] to [18h]" (the choices already show their unit)
			rowInterval.WrapContents = true;
			rowInterval.SetFlowBreak(minutes, true);
			rowDaily = Row(T("rem.at"), time);
			rowOnce = Row(T("rem.on"), at);
			Controls.AddRange(new Control[] { rowInterval, rowDaily, rowOnce });

			minutes.Value = Math.Max(1, Math.Min(1440, model.Minutes));
			from.Value = Math.Max(0, Math.Min(23, model.FromHour));
			to.Value = model.ToHour <= 0 ? 24 : Math.Min(24, model.ToHour);
			int minuteOfDay = TimeSpan.TryParse(model.Time, out var ts) ? (int)ts.TotalMinutes : 9 * 60;
			if (!time.Values.Contains(minuteOfDay)) time.Values = time.Values.Concat(new[] { minuteOfDay }).OrderBy(m => m).ToArray();
			time.Value = minuteOfDay;
			at.Value = model.At < DateTime.Now ? DateTime.Now.AddMinutes(10) : model.At;
			kind = model.Kind;
			for (int i = 0; i < 7; i++) dayOn[i] = (model.Days & (1 << i)) != 0;

			ok.Click += (s, e) =>
			{
				if (string.IsNullOrWhiteSpace(text.Text)) { text.Focus(); System.Media.SystemSounds.Beep.Play(); return; }
				int mask = 0;
				for (int i = 0; i < 7; i++) if (dayOn[i]) mask |= 1 << i;
				if (mask == 0 && kind != Reminder.Kinds.Once) { System.Media.SystemSounds.Beep.Play(); return; }
				var target = editing ?? new Reminder();
				target.Text = text.Text.Trim();
				target.Kind = kind;
				target.Minutes = (int)minutes.Value;
				target.FromHour = (int)from.Value;
				target.ToHour = (int)to.Value % 24;
				target.Time = $"{time.Value / 60:00}:{time.Value % 60:00}";
				target.At = at.Value;
				target.Days = mask == 0 ? 127 : mask;
				target.Enabled = true;
				target.SnoozedUntil = DateTime.MinValue;
				// counting starts now (an interval reminder does not fire at once)
				target.LastFired = target.Kind == Reminder.Kinds.Once ? DateTime.MinValue : DateTime.Now;
				Result = target;
				DialogResult = DialogResult.OK;
			};

			foreach (var b in buttons) b.Font = fText;
			ClientSize = new Size(W, 100);
			ApplyTheme(ok);
			text.BorderStyle = BorderStyle.None;
			text.BackColor = Theme.Surface; text.ForeColor = Theme.Text;
			foreach (var n in new Control[] { minutes, from, to }) { n.BackColor = Theme.Surface; n.ForeColor = Theme.Text; }
			foreach (var p in new[] { rowInterval, rowDaily, rowOnce }) p.BackColor = Theme.Surface;
			LayoutAll();
			ActiveControl = text;
		}

		void LayoutAll()
		{
			int y = Pad + 64;
			y += 24;   // "What should the pet say?"
			textField = new Rectangle(Pad, y, W - Pad * 2, 44);
			text.Width = textField.Width - 28;
			text.Location = new Point(textField.X + 14, textField.Y + (44 - text.Height) / 2);
			y = textField.Bottom + 22;
			whenTop = y;
			y += 24;   // "When"
			int sw = (W - Pad * 2 - 8) / 3;
			for (int i = 0; i < 3; i++) segs[i] = new Rectangle(Pad + i * (sw + 4), y, sw, 36);
			y += 46;
			var row = kind == Reminder.Kinds.Interval ? rowInterval : kind == Reminder.Kinds.Daily ? rowDaily : rowOnce;
			foreach (var p in new[] { rowInterval, rowDaily, rowOnce }) p.Visible = p == row;
			row.PerformLayout();
			var pref = row.PreferredSize;
			card = new Rectangle(Pad, y, W - Pad * 2, Math.Max(56, pref.Height + 24));
			row.AutoSize = false;   // exactly its content: an auto-sized panel grew over the days below
			row.Size = new Size(Math.Min(pref.Width, card.Width - 28), pref.Height);
			row.Location = new Point(card.X + 14, card.Y + (card.Height - pref.Height) / 2);
			y = card.Bottom + 22;
			daysTop = y;
			y += 24;   // "Days of the week"
			int dw = (W - Pad * 2 - 6 * 6) / 7;
			for (int i = 0; i < 7; i++) dayRects[i] = new Rectangle(Pad + i * (dw + 6), y, dw, 34);
			y += 34 + 28;
			int bottom = LayoutButtons(y, W);
			ClientSize = new Size(W, bottom + Pad);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			Theme.Prepare(g);
			PaintIcon(g);
			Theme.DrawText(g, Text, fTitle, new Rectangle(Pad + 64, Pad + 8, W - Pad * 2 - 64, 32), Theme.Text, TextFormatFlags.SingleLine);
			Theme.DrawText(g, Lang.T("rem.whatSays"), fText, new Rectangle(Pad, textField.Y - 24, W - Pad * 2, 22), Theme.Text, TextFormatFlags.SingleLine);
			Theme.FillRounded(g, Theme.Surface, textField, 10);
			Theme.DrawRounded(g, text.Focused ? Theme.Accent : Theme.Border, textField, 10);

			Theme.DrawText(g, Lang.T("rem.when"), fText, new Rectangle(Pad, whenTop, 300, 22), Theme.Text, TextFormatFlags.SingleLine);
			string[] names = { Lang.T("rem.kindInterval"), Lang.T("rem.kindDaily"), Lang.T("rem.kindOnce") };
			var kinds = new[] { Reminder.Kinds.Interval, Reminder.Kinds.Daily, Reminder.Kinds.Once };
			for (int i = 0; i < 3; i++)
			{
				bool on = kind == kinds[i];
				Theme.FillRounded(g, on ? Theme.Accent : Theme.Surface, segs[i], 10);
				if (!on) Theme.DrawRounded(g, Theme.Border, segs[i], 10);
				Theme.DrawText(g, names[i], fSmall, segs[i], on ? Theme.AccentText : Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			}
			Theme.FillRounded(g, Theme.Surface, card, 10);
			Theme.DrawRounded(g, Theme.Border, card, 10);

			bool daysUsed = kind != Reminder.Kinds.Once;
			Theme.DrawText(g, Lang.T("rem.days"), fText, new Rectangle(Pad, daysTop, 300, 22), daysUsed ? Theme.Text : Theme.TextMuted, TextFormatFlags.SingleLine);
			var dayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
			for (int i = 0; i < 7; i++)
			{
				int d = DayOrder[i];
				bool on = dayOn[d] && daysUsed;
				Theme.FillRounded(g, on ? Theme.AccentSoft : Theme.Surface, dayRects[i], 17);
				Theme.DrawRounded(g, on ? Theme.Accent : Theme.Border, dayRects[i], 17);
				Theme.DrawText(g, dayNames[d].TrimEnd('.'), fSmall, dayRects[i], on ? Theme.Accent : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}
			using (var p = new Pen(Theme.Border)) g.DrawLine(p, Pad, buttons[0].Top - 12, W - Pad, buttons[0].Top - 12);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			Cursor = segs.Any(r => r.Contains(e.Location)) || (kind != Reminder.Kinds.Once && dayRects.Any(r => r.Contains(e.Location))) ? Cursors.Hand : Cursors.Default;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			var kinds = new[] { Reminder.Kinds.Interval, Reminder.Kinds.Daily, Reminder.Kinds.Once };
			for (int i = 0; i < 3; i++) if (segs[i].Contains(e.Location)) { kind = kinds[i]; LayoutAll(); return; }
			if (kind == Reminder.Kinds.Once) return;
			for (int i = 0; i < 7; i++) if (dayRects[i].Contains(e.Location)) { int d = DayOrder[i]; dayOn[d] = !dayOn[d]; Invalidate(); return; }
		}
	}

	/// <summary>
	/// A number to choose in the look of the program: "‹ 30 min ›" in a rounded box. Arrows (or a click on the sides),
	/// the mouse wheel and the arrow keys move through <see cref="Values"/>. Replaces the classic spin boxes.
	/// </summary>
	internal sealed class PetStepper : Control
	{
		int[] values = { 0 };
		int index;
		int hoverSide;   // -1 left, 1 right
		static readonly Font ArrowFont = Theme.CreateFont(20, FontStyle.Bold);
		public Func<int, string> Format = v => v.ToString();
		public event EventHandler ValueChanged;

		public PetStepper()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
			Size = new Size(140, 32);
			Cursor = Cursors.Hand;
			TabStop = true;
		}

		/// <summary>The choices, in order.</summary>
		public int[] Values
		{
			get => values;
			set { int keep = Value; values = value != null && value.Length > 0 ? value : new[] { 0 }; Value = keep; }
		}

		/// <summary>The chosen value (the nearest choice when set to another number).</summary>
		public int Value
		{
			get => values[index];
			set
			{
				int best = 0;
				for (int i = 1; i < values.Length; i++) if (Math.Abs(values[i] - value) < Math.Abs(values[best] - value)) best = i;
				if (best != index) { index = best; Invalidate(); ValueChanged?.Invoke(this, EventArgs.Empty); }
			}
		}

		public void Step(int by)
		{
			int i = Math.Max(0, Math.Min(values.Length - 1, index + by));
			if (i == index) return;
			index = i; Invalidate(); ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		Rectangle LeftRect => new Rectangle(0, 0, Height, Height);
		Rectangle RightRect => new Rectangle(Width - Height, 0, Height, Height);

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			var r = new Rectangle(0, 0, Width - 1, Height - 1);
			Theme.FillRounded(g, Theme.Surface, r, 8);
			Theme.DrawRounded(g, Focused ? Theme.Accent : Theme.Border, r, 8, Focused ? 2f : 1f);
			for (int side = -1; side <= 1; side += 2)
			{
				var b = Rectangle.Inflate(side < 0 ? LeftRect : RightRect, -4, -4);
				bool can = side < 0 ? index > 0 : index < values.Length - 1;
				if (can && hoverSide == side) Theme.FillRounded(g, Theme.Hover, b, 6);
				Theme.DrawText(g, side < 0 ? "‹" : "›", ArrowFont, b, can ? Theme.Accent : Theme.Border, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}
			Theme.DrawText(g, Format(Value), Font, new Rectangle(Height, 0, Width - Height * 2, Height), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			int side = LeftRect.Contains(e.Location) ? -1 : RightRect.Contains(e.Location) ? 1 : 0;
			if (side != hoverSide) { hoverSide = side; Invalidate(); }
		}

		protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hoverSide = 0; Invalidate(); }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			if (e.Button != MouseButtons.Left) return;
			// a click on the left half goes back, on the right half forward
			Step(e.X < Width / 2 ? -1 : 1);
		}

		protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Step(e.Delta > 0 ? 1 : -1); }

		protected override bool IsInputKey(Keys k) => k == Keys.Up || k == Keys.Down || k == Keys.Left || k == Keys.Right || base.IsInputKey(k);

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Right) { Step(1); e.Handled = true; }
			else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Left) { Step(-1); e.Handled = true; }
		}

		protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
		protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
	}
}
