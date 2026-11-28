using System.Drawing;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>A column of cards that scrolls, each card as wide as the column (so long texts wrap instead of hiding).</summary>
	internal sealed class CardList : FlowLayoutPanel
	{
		public CardList()
		{
			FlowDirection = FlowDirection.TopDown;
			WrapContents = false;
			AutoScroll = true;
			Dock = DockStyle.Fill;
		}

		/// <summary>The width the cards have now (the whole column, less the scroll bar only when one is needed).</summary>
		public int CardWidth { get; private set; }

		/// <summary>The column's width when no scroll bar is shown.</summary>
		int Full => ClientSize.Width + (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - Padding.Horizontal - Look.Px(this, 4);
		bool fitting;

		protected override void OnClientSizeChanged(EventArgs e) { base.OnClientSizeChanged(e); Fit(); }
		protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Look.ScrollBars(this); }

		public void Fit()
		{
			if (fitting) return;
			fitting = true;
			try
			{
				// first as wide as the column; only when the cards are then too tall for it, less the scroll bar
				int full = Math.Max(Look.Px(this, 200), Full);
				Lay(full);
				if (Bottom() + Padding.Bottom > ClientSize.Height) Lay(Math.Max(Look.Px(this, 200), full - SystemInformation.VerticalScrollBarWidth));
				// laid out first while the page was still tiny (hidden), the cards were tall: a scroll bar that is no longer
				// needed stays unless the scrolling is set up again
				if (VerticalScroll.Visible && Bottom() + Padding.Bottom <= ClientSize.Height)
				{
					AutoScroll = false;
					AutoScroll = true;
				}
			}
			finally { fitting = false; }
		}

		int Bottom() => Controls.Count == 0 ? 0 : Controls.Cast<Control>().Max(c => c.Bottom + c.Margin.Bottom) - AutoScrollPosition.Y;

		void Lay(int w)
		{
			CardWidth = w;
			SuspendLayout();
			foreach (Control c in Controls) if (c is Card card) card.Fit(w);
			ResumeLayout(true);
		}
	}

	/// <summary>One card of a <see cref="CardList"/>: a box on the surface colour whose texts wrap at its width.</summary>
	internal class Card : TableLayoutPanel
	{
		/// <summary>Labels that wrap: they get the width that is left for them.</summary>
		public readonly List<(Label label, int less)> Wrapping = new();

		public Card(Control owner)
		{
			Tag = "card";
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			Padding = new Padding(Look.Px(owner, 14), Look.Px(owner, 10), Look.Px(owner, 14), Look.Px(owner, 10));
			Margin = new Padding(0, 0, 0, Look.Px(owner, 8));
		}

		public virtual void Fit(int width)
		{
			MinimumSize = new Size(width, 0);
			MaximumSize = new Size(width, 0);
			foreach (var (label, less) in Wrapping) label.MaximumSize = new Size(Math.Max(40, width - Padding.Horizontal - less), 0);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// rounded corners on the page's colour
			e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
			Theme.Prepare(e.Graphics);
			Theme.FillRounded(e.Graphics, Theme.Surface, new RectangleF(0, 0, Width - 1, Height - 1), Look.Px(this, 10));
			Theme.DrawRounded(e.Graphics, Theme.Border, new RectangleF(0, 0, Width, Height), Look.Px(this, 10));
		}
	}

	/// <summary>The top of a page: its title on the left and, optionally, a button on the right.</summary>
	internal sealed class PageHeader : TableLayoutPanel
	{
		public PageHeader(string title, UiFonts fonts, Button button = null)
		{
			Dock = DockStyle.Top;
			AutoSize = true;
			ColumnCount = 2;
			ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			Padding = new Padding(0, 0, 0, Look.Px(this, 10));
			var label = Look.Label(title, fonts.Section);
			label.Anchor = AnchorStyles.Left;
			label.Margin = new Padding(0, Look.Px(this, 4), 0, 0);
			Controls.Add(label, 0, 0);
			if (button != null) { button.Anchor = AnchorStyles.Right; button.Margin = new Padding(0); Controls.Add(button, 1, 0); }
		}
	}

	/// <summary>
	/// "Recent jobs": what was done lately (the newest first), when, and the files made, each with Open and Show in
	/// folder. Follows <see cref="History.Changed"/>, so a job just done appears; "Clear history" empties the list (the
	/// files themselves are never touched).
	/// </summary>
	internal sealed class RecentPage : Panel
	{
		/// <summary>How many files of one job are listed (the rest is "… and 3 more").</summary>
		public const int MostFiles = 4;
		readonly UiFonts fonts;
		readonly CardList list;
		readonly Label empty;
		readonly Button clear;
		bool dirty = true;

		public RecentPage(UiFonts fonts)
		{
			this.fonts = fonts;
			Tag = "page";
			Dock = DockStyle.Fill;
			clear = Look.Button(T.S("window.recent.clear"), fonts.Text);
			clear.Click += (s, e) => ClearHistory(true);
			list = new CardList();
			empty = Look.Label(T.S("window.recent.empty"), fonts.Text, "muted");
			empty.Margin = new Padding(0, Look.Px(this, 8), 0, 0);
			Controls.Add(list);
			Controls.Add(new PageHeader(T.S("window.recent.title"), fonts, clear));
			History.Changed += HistoryChanged;
		}

		/// <summary>The jobs listed (their cards), for the tests.</summary>
		public List<Card> Cards => list.Controls.OfType<Card>().ToList();
		public bool ShowsEmpty => list.Controls.Contains(empty);
		public Button ClearButton => clear;

		void HistoryChanged()
		{
			dirty = true;
			if (!IsHandleCreated || IsDisposed) return;
			// it may come from the thread of a job
			try { BeginInvoke(() => { if (Visible) Reload(); }); } catch (Exception) { }
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible && dirty) Reload();
		}

		/// <summary>Ask first (unless told not to), then empty the history. The files made stay where they are.</summary>
		public void ClearHistory(bool ask)
		{
			if (ask && MessageBox.Show(FindForm(), T.S("window.recent.ask"), T.S("app"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
			History.Clear();
			Reload();
		}

		public void Reload()
		{
			dirty = false;
			var entries = History.Read();
			list.SuspendLayout();
			foreach (Control c in list.Controls.Cast<Control>().ToList()) if (c != empty) c.Dispose();
			list.Controls.Clear();
			clear.Enabled = entries.Count > 0;
			if (entries.Count == 0) list.Controls.Add(empty);
			var now = DateTime.Now;
			foreach (var entry in entries) list.Controls.Add(Entry(entry, now));
			Look.Paint(list);
			list.Fit();
			list.ResumeLayout();
		}

		Card Entry(History.Entry entry, DateTime now)
		{
			var card = new Card(this) { ColumnCount = 1 };
			var def = Jobs.Find(entry.job ?? "");
			var top = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0), Padding = new Padding(0) };
			top.Controls.Add(new Label { Text = ((def?.Emoji ?? "✔") + "  " + Jobs.TextOf(entry.job)), Font = fonts.Bold, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 0, Look.Px(this, 12), 0) });
			top.Controls.Add(new Label { Text = Look.When(entry.at, now), Font = fonts.Small, AutoSize = true, Tag = "muted", UseMnemonic = false, Margin = new Padding(0, Look.Px(this, 1), 0, 0) });
			card.Controls.Add(top);
			foreach (var path in entry.outputs.Take(MostFiles)) card.Controls.Add(Made(path, card));
			if (entry.outputs.Count > MostFiles) card.Controls.Add(new Label { Text = T.S("main.more", entry.outputs.Count - MostFiles), Font = fonts.Small, AutoSize = true, Tag = "muted", UseMnemonic = false });
			return card;
		}

		/// <summary>A file made: its name and Open / Show in folder, or "(no longer there)" when it was moved or deleted.</summary>
		Control Made(string path, Card card)
		{
			var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, Look.Px(this, 4), 0, 0), Padding = new Padding(0) };
			bool there = File.Exists(path) || Directory.Exists(path);
			var name = new Label { Text = Path.GetFileName(path.TrimEnd('\\')), Font = fonts.Text, AutoSize = true, UseMnemonic = false, Tag = there ? null : "muted", Margin = new Padding(0, 0, Look.Px(this, 12), 0) };
			card.Wrapping.Add((name, Look.Px(this, 240)));
			if (there) Look.DragOut(name, path);
			row.Controls.Add(name);
			if (there)
			{
				row.Controls.Add(Look.Link(T.S("open"), fonts.Small, () => MainForm.Open(path)));
				row.Controls.Add(Look.Link(T.S("showFolder"), fonts.Small, () => MainForm.Reveal(path)));
			}
			else row.Controls.Add(new Label { Text = T.S("window.recent.gone"), Font = fonts.Small, AutoSize = true, Tag = "muted", UseMnemonic = false });
			return row;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { History.Changed -= HistoryChanged; empty.Dispose(); }
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// "Settings": every <see cref="Prefs.Pref"/> the features registered, each with its text and hint, a switch and/or a
	/// button. With none it says there is nothing to set up yet.
	/// </summary>
	internal sealed class SettingsPage : Panel
	{
		readonly UiFonts fonts;
		readonly CardList list;
		readonly Label empty;

		public SettingsPage(UiFonts fonts)
		{
			this.fonts = fonts;
			Tag = "page";
			Dock = DockStyle.Fill;
			list = new CardList();
			empty = Look.Label(T.S("window.settings.empty"), fonts.Text, "muted");
			empty.Margin = new Padding(0, Look.Px(this, 8), 0, 0);
			Controls.Add(list);
			Controls.Add(new PageHeader(T.S("window.settings.title"), fonts));
		}

		public List<Card> Cards => list.Controls.OfType<Card>().ToList();
		public bool ShowsEmpty => list.Controls.Contains(empty);

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible) Reload();
		}

		/// <summary>Built again each time the page is shown: a switch may have been changed somewhere else meanwhile.</summary>
		public void Reload()
		{
			var prefs = Prefs.All();
			list.SuspendLayout();
			foreach (Control c in list.Controls.Cast<Control>().ToList()) if (c != empty) c.Dispose();
			list.Controls.Clear();
			if (prefs.Count == 0) list.Controls.Add(empty);
			foreach (var p in prefs) list.Controls.Add(Row(p));
			Look.Paint(list);
			list.Fit();
			list.ResumeLayout();
		}

		Card Row(Prefs.Pref p)
		{
			var card = new Card(this) { ColumnCount = 2, Name = p.Key };
			card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			string text = T.S(p.TextKey ?? p.Key);
			var texts = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0), Padding = new Padding(0), Anchor = AnchorStyles.Left };
			var title = new Label { Text = text, Font = fonts.Bold, AutoSize = true, UseMnemonic = false, Margin = new Padding(0) };
			texts.Controls.Add(title);
			int side = (p.Get != null ? Look.Px(this, 70) : 0) + (p.ButtonKey != null ? Look.Px(this, 200) : 0) + Look.Px(this, 8);
			card.Wrapping.Add((title, side));
			if (!string.IsNullOrEmpty(p.HintKey))
			{
				var hint = new Label { Text = T.S(p.HintKey), Font = fonts.Small, AutoSize = true, Tag = "muted", UseMnemonic = false, Margin = new Padding(0, Look.Px(this, 3), 0, 0) };
				texts.Controls.Add(hint);
				card.Wrapping.Add((hint, side));
			}
			card.Controls.Add(texts, 0, 0);

			var side2 = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(Look.Px(this, 8), 0, 0, 0), Padding = new Padding(0), Anchor = AnchorStyles.Right };
			if (p.ButtonKey != null && p.Click != null)
			{
				var b = Look.Button(T.S(p.ButtonKey), fonts.Text);
				b.Margin = new Padding(0, 0, p.Get != null ? Look.Px(this, 12) : 0, 0);
				b.Anchor = AnchorStyles.None;
				b.Click += (s, e) => Safely(() => p.Click(FindForm()));
				side2.Controls.Add(b);
			}
			if (p.Get != null)
			{
				var sw = new Switch(this) { AccessibleName = text, Anchor = AnchorStyles.None };
				try { sw.Checked = p.Get(); } catch (Exception) { }
				sw.CheckedChanged += (s, e) =>
				{
					if (sw.Quiet || p.Set == null) return;
					if (!Safely(() => p.Set(sw.Checked))) sw.SetQuietly(!sw.Checked);
				};
				side2.Controls.Add(sw);
			}
			card.Controls.Add(side2, 1, 0);
			return card;
		}

		/// <summary>A setting that fails says so in plain words, and nothing else breaks.</summary>
		bool Safely(Action act)
		{
			try { act(); return true; }
			catch (Exception ex)
			{
				MessageBox.Show(FindForm(), T.S("window.settings.failed", Jobs.Friendly(ex)), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) empty.Dispose();
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// An on/off switch like the ones in DeskPals' settings. It is a check box underneath, so Tab reaches it, Space
	/// turns it and screen readers say "on/off".
	/// </summary>
	internal sealed class Switch : CheckBox
	{
		public bool Quiet { get; private set; }

		public Switch(Control owner)
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			AutoSize = false;
			Size = new Size(Look.Px(owner, 46), Look.Px(owner, 26));
			Cursor = Cursors.Hand;
			Margin = new Padding(0);
		}

		/// <summary>Change it without telling (to put it back when a setting could not be changed).</summary>
		public void SetQuietly(bool on) { Quiet = true; try { Checked = on; } finally { Quiet = false; } }

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.Clear(Parent?.BackColor ?? Theme.Surface);
			Theme.Prepare(g);
			int inset = Math.Max(1, Height / 10);
			var r = new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);
			Theme.DrawToggle(g, r, Checked);
			if (Focused && ShowFocusCues) Theme.DrawRounded(g, Theme.Accent, new RectangleF(0, 0, Width, Height), Height / 2f, 1.5f);
		}

		protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
		protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
	}
}
