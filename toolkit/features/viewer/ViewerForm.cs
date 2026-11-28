using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using DesktopPet;
using Windows.Storage.Streams;
using WinPdf = Windows.Data.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// The window of "See and edit": the pages (or pictures) as small pictures on the left, the chosen one big on the
	/// right. Turn, delete, drag to reorder, undo and redo; for a PDF, page numbers and a watermark; for pictures, cleaned
	/// as a document; all seen before saving. Nothing is written until "Save a copy" or "Replace the original" (asked
	/// first); closing with changes asks whether to save them. The answers go to <see cref="ViewerJobs"/>.
	/// </summary>
	internal sealed class ViewerForm : Form
	{
		readonly IList<string> files;
		readonly bool isPdf;
		/// <summary>The PDF shown: the file itself, or (several files) a temporary one with all their pages.</summary>
		readonly string source;
		/// <summary>Several files: where each page of <see cref="source"/> came from.</summary>
		readonly List<(int file, int page)> from;
		readonly PagesForm.PageGrid grid;
		readonly PageView view;
		readonly Label status;
		readonly Button undo, redo, numbersButton, watermarkButton, cleanButton, zoomButton, replace, saveCopy;
		readonly ComboBox saveAs;
		readonly Font fText = Theme.CreateFont(12.5f), fSmall = Theme.CreateFont(11.5f), fBold = Theme.CreateFont(13, FontStyle.Bold);
		readonly CancellationTokenSource closing = new();

		// what is being changed (besides the pages in the grid)
		Dictionary<string, string> numbers, watermark;
		string clean = "none";

		// undo and redo: every state, and where we are
		readonly List<Snap> history = new();
		int at;
		bool restoring;

		// the PDF, read once into memory (the file stays free, so it can be replaced at the end)
		InMemoryRandomAccessStream pdfStream;
		WinPdf.PdfDocument pdfDoc;
		// big pictures already drawn: by page (PDF), by picture and cleaning (pictures), by state (a preview with stamps)
		readonly Dictionary<string, Bitmap> big = new();
		readonly List<string> bigOrder = new();
		int showing;

		public Dictionary<string, string> Answers { get; private set; }

		/// <summary>The answers for the job, or null when the window was closed without saving.</summary>
		public static Dictionary<string, string> Open(IWin32Window owner, IList<string> files)
		{
			using var form = new ViewerForm(files) { ShowInTaskbar = owner == null, StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent };
			var r = owner == null ? form.ShowDialog() : form.ShowDialog(owner);
			return r == DialogResult.OK ? form.Answers : null;
		}

		internal ViewerForm(IList<string> files)
		{
			this.files = files.ToList();
			isPdf = files.Any(Jobs.IsPdf);
			source = files[0];
			if (isPdf && files.Count > 1)
			{
				// several PDFs (and pictures): one temporary PDF with all their pages, read like a single one
				var both = Task.Run(() => ViewerJobs.Combine(files)).GetAwaiter().GetResult();
				source = both.pdf; from = both.from;
			}
			int count = isPdf ? PdfWork.Pages(source) : files.Count;
			if (count == 0) throw new InvalidOperationException(T.S("err.noPages"));

			Text = T.S("job.view") + " — " + (files.Count == 1 ? Path.GetFileName(files[0]) : isPdf ? T.S("viewer.files", files.Count) : T.S("viewer.pictures", files.Count));
			AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
			ClientSize = new Size(Math.Min(1100, screen.Width - 80), Math.Min(760, screen.Height - 80));
			MinimumSize = new Size(640, 460);
			KeyPreview = true;
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);
			BackColor = Theme.Background; ForeColor = Theme.Text;
			Padding = new Padding(12, 8, 12, 10);

			var tools = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Theme.Background, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
			grid = new PagesForm.PageGrid(fBold, fSmall, count) { Dock = DockStyle.Left, Width = 196 };
			if (!isPdf) grid.LabelOf = t => Path.GetFileName(this.files[t.Page - 1]);
			else if (from != null) grid.LabelOf = t => T.S("viewer.pageOf", Short(Path.GetFileNameWithoutExtension(this.files[from[t.Page - 1].file])), from[t.Page - 1].page);
			view = new PageView(fText) { Dock = DockStyle.Fill };
			var gap = new Panel { Dock = DockStyle.Left, Width = 10, BackColor = Theme.Background };
			var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Background, Padding = new Padding(0, 10, 0, 0), WrapContents = false };

			tools.Controls.Add(Tool("🔄  " + T.S("viewer.left"), () => grid.Turn(270), "Ctrl+L"));
			tools.Controls.Add(Tool("🔃  " + T.S("viewer.right"), () => grid.Turn(90), "Ctrl+R"));
			tools.Controls.Add(Tool("🗑️  " + T.S("viewer.delete"), grid.DeleteSelected, "Del"));
			tools.Controls.Add(Space());
			undo = Tool("↩️  " + T.S("viewer.undo"), Undo, "Ctrl+Z");
			redo = Tool("↪️  " + T.S("viewer.redo"), Redo, "Ctrl+Y");
			tools.Controls.Add(undo); tools.Controls.Add(redo);
			tools.Controls.Add(Space());
			if (isPdf)
			{
				numbersButton = Tool("", AskNumbers, null, true);
				watermarkButton = Tool("", AskWatermark, null, true);
				tools.Controls.Add(numbersButton); tools.Controls.Add(watermarkButton);
			}
			else
			{
				cleanButton = Tool("", AskClean, null, true);
				tools.Controls.Add(cleanButton);
			}
			zoomButton = Tool("", view.Fit, "Ctrl+0");
			// the marks: a tool is chosen, then a box is dragged on the page
			FlowLayoutPanel markRow = null;
			if (isPdf)
			{
				markRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Theme.Background, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
				markRow.Controls.Add(new Label { Text = T.S("marks.label"), Font = fText, ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
				foreach (var (kind, emoji, key) in new[] { (MarkKind.Highlight, "🖍️", "marks.highlight"), (MarkKind.Text, "🔤", "marks.text"), (MarkKind.Redact, "⬛", "marks.redact"), (MarkKind.Sign, "✍️", "marks.sign") })
				{
					var b = Tool(emoji + "  " + T.S(key), () => PickTool(kind), T.S(key + "Tip"), true);
					b.Name = "mark-" + kind;
					markTools[kind] = b;
					markRow.Controls.Add(b);
				}
				markHint = new Label { Font = fSmall, ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(6, 9, 0, 0) };
				markRow.Controls.Add(markHint);
				view.BoxDrawn += Drawn;
				view.MarkPicked += i => MarkHint();
				view.MarkMoved += MarkMoved;
			}

			var cancel = Big(T.S("cancel"), false);
			cancel.DialogResult = DialogResult.Cancel;
			saveCopy = Big("💾  " + T.S("viewer.saveCopy"), true);
			replace = Big("♻️  " + T.S("viewer.replace"), false);
			saveCopy.Click += (s, e) => Finish(false);
			replace.Click += (s, e) => Finish(true);
			status = new Label { Font = fSmall, ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(0, 9, 12, 0) };
			bottom.Controls.Add(cancel); bottom.Controls.Add(saveCopy); bottom.Controls.Add(replace);
			if (!isPdf)
			{
				saveAs = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = fText, Width = 190, BackColor = Theme.Surface, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 3, 0, 0) };
				saveAs.Items.Add(T.S("viewer.asPdf"));
				saveAs.Items.Add(files.Count == 1 ? T.S("viewer.asPicture") : T.S("viewer.asPictures"));
				saveAs.SelectedIndex = files.Count > 1 ? 0 : 1;
				saveAs.SelectedIndexChanged += (s, e) => Buttons();
				bottom.Controls.Add(saveAs);
				bottom.Controls.Add(new Label { Text = T.S("viewer.saveAs"), Font = fText, ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(14, 8, 0, 0) });
			}
			bottom.Controls.Add(status);
			// the zoom, at the bottom left (the bottom bar goes from right to left)
			var zoomIn = Tool("➕", view.ZoomIn, "Ctrl +");
			var zoomOut = Tool("➖", view.ZoomOut, "Ctrl −");
			foreach (var z in new[] { zoomIn, zoomButton, zoomOut }) { z.Margin = new Padding(0, 0, 6, 0); bottom.Controls.Add(z); }
			status.Margin = new Padding(0, 9, 24, 0);
			CancelButton = cancel;

			Controls.Add(view);
			Controls.Add(gap);
			Controls.Add(grid);
			Controls.Add(bottom);
			if (markRow != null) Controls.Add(markRow);
			Controls.Add(tools);
			ButtonLook.Hook(this);
			grid.Changed += Changed;
			view.ZoomChanged += () => zoomButton.Text = "🔍  " + (view.Zoom == 1 ? T.S("viewer.fit") : Math.Round(view.Zoom * 100) + "%");
			zoomButton.Text = "🔍  " + T.S("viewer.fit");
			history.Add(Take());
			StampButtons();
			Buttons();
			Shown += async (s, e) => { grid.FocusOn(0); loading = Start(); await loading; };
			FormClosing += AskBeforeClosing;
			FormClosed += (s, e) => { closing.Cancel(); WaitDrawing(); };
		}

		Button Tool(string text, Action act, string tip, bool opensQuestion = false)
		{
			var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(34, 0), Font = fText, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text, Margin = new Padding(0, 2, 6, 2), Padding = new Padding(6, 2, 6, 2), Cursor = Cursors.Hand };
			b.FlatAppearance.BorderColor = Theme.Border;
			b.FlatAppearance.MouseOverBackColor = Theme.Hover;
			b.Click += (s, e) => { act(); if (!b.IsDisposed && !opensQuestion) grid.Focus(); };
			if (!string.IsNullOrEmpty(tip)) new ToolTip().SetToolTip(b, tip);
			return b;
		}

		Control Space() => new Panel { Width = 10, Height = 1, BackColor = Theme.Background, Margin = new Padding(0) };

		Button Big(string text, bool primary)
		{
			var b = new Button { Text = text, AutoSize = true, Font = primary ? fBold : fText, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(10, 3, 10, 3), Cursor = Cursors.Hand,
				BackColor = primary ? Theme.Accent : Theme.Surface, ForeColor = primary ? Theme.AccentText : Theme.Text };
			b.FlatAppearance.BorderSize = primary ? 0 : 1;
			b.FlatAppearance.BorderColor = Theme.Border;
			return b;
		}

		// ------------------------------------------------------------------ the state, undo and redo

		sealed record Snap(List<(int page, int turn, List<Mark> marks)> Pages, Dictionary<string, string> Numbers, Dictionary<string, string> Watermark, string Clean)
		{
			public string Key => string.Join(",", Pages.Select(p => p.page + ":" + p.turn + "[" + string.Join("/", p.marks.Select(m => m.Key)) + "]")) + "|" + Text(Numbers) + "|" + Text(Watermark) + "|" + Clean;
			static string Text(Dictionary<string, string> d) => d == null ? "-" : string.Join(";", d.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + "=" + x.Value));
		}

		Snap Take() => new(grid.Tiles.Select(t => (t.Page, t.Turn, t.Marks.ToList())).ToList(), numbers == null ? null : new(numbers), watermark == null ? null : new(watermark), clean);

		/// <summary>Something changed (or only the page chosen): kept for undo when the pages or the extras are different.</summary>
		void Changed()
		{
			if (!restoring)
			{
				var now = Take();
				if (now.Key != history[at].Key)
				{
					history.RemoveRange(at + 1, history.Count - at - 1);
					history.Add(now);
					at = history.Count - 1;
				}
			}
			Buttons();
			bigTask = ShowCurrent();
		}

		void Put(Snap s)
		{
			restoring = true;
			try
			{
				numbers = s.Numbers == null ? null : new(s.Numbers);
				watermark = s.Watermark == null ? null : new(s.Watermark);
				clean = s.Clean;
				StampButtons();
				grid.SetTiles(s.Pages.Select(p => new PagesForm.Tile { Page = p.page, Turn = p.turn, Marks = p.marks.ToList() }));
			}
			finally { restoring = false; }
			Buttons();
			bigTask = ShowCurrent();
		}

		internal void Undo() { if (at > 0) Put(history[--at]); }
		internal void Redo() { if (at < history.Count - 1) Put(history[++at]); }

		/// <summary>There are changes not saved (the first state is the file as it is).</summary>
		internal bool Dirty => history[at].Key != history[0].Key;
		internal bool CanUndo => at > 0;
		internal bool CanRedo => at < history.Count - 1;
		internal PagesForm.PageGrid Grid => grid;
		internal PageView View => view;
		internal bool AsPdf { get => saveAs == null || saveAs.SelectedIndex == 0; set { if (saveAs != null) saveAs.SelectedIndex = value ? 0 : 1; } }

		void Buttons()
		{
			undo.Enabled = CanUndo;
			redo.Enabled = CanRedo;
			bool any = grid.Tiles.Count > 0;
			// pictures saved as a PDF or "cleaned" become a new file: the originals are not replaced then
			bool canReplace = isPdf ? files.Count == 1 : !AsPdf;
			// several files (or pictures into a PDF) are worth saving as they are: putting them together is the change
			saveCopy.Enabled = any && (Dirty || (!isPdf && AsPdf) || (isPdf && files.Count > 1));
			replace.Enabled = any && Dirty && canReplace;
			replace.Visible = canReplace;
			status.Text = Dirty ? "● " + T.S("viewer.unsaved") : isPdf ? T.S("pdfpages.pages.count", grid.Tiles.Count, grid.Tiles.Count(t => t.Selected)) : "";
			status.ForeColor = Dirty ? Theme.Accent : Theme.TextMuted;
		}

		void StampButtons()
		{
			if (numbersButton != null) numbersButton.Text = "🔢  " + T.S("viewer.numbers") + (numbers != null ? "  ✓" : "");
			if (watermarkButton != null) watermarkButton.Text = "💧  " + T.S("viewer.watermark") + (watermark != null ? "  ✓" : "");
			if (cleanButton != null) cleanButton.Text = "🧾  " + T.S("viewer.clean") + (clean != "none" ? "  ✓" : "");
		}

		// ------------------------------------------------------------------ the extras: numbers, watermark, cleaning

		void AskNumbers() => Extra(numbersButton, numbers != null, () => PdfPagesJobs.AskNumbers(this, new[] { source }, numbers), a => numbers = a);
		void AskWatermark() => Extra(watermarkButton, watermark != null, () => PdfPagesJobs.AskWatermark(this, new[] { source }, watermark), a => watermark = a);

		/// <summary>
		/// An extra not on yet: its questions. One already on: a small menu, "Change…" (the questions again, with the
		/// answers given) or "Remove".
		/// </summary>
		void Extra(Button button, bool on, Func<Dictionary<string, string>> ask, Action<Dictionary<string, string>> set)
		{
			void Change() { var a = ask(); if (a == null) return; set(a); Extras(); }
			if (!on) { Change(); return; }
			var menu = new ContextMenuStrip { Font = fText, ShowImageMargin = false };
			Theme.ApplyTo(menu);
			menu.Items.Add("✏️  " + T.S("viewer.change"), null, (s, e) => BeginInvoke(Change));
			menu.Items.Add("❌  " + T.S("viewer.remove"), null, (s, e) => { set(null); Extras(); });
			menu.Closed += (s, e) => BeginInvoke(menu.Dispose);
			menu.Show(button, new Point(0, button.Height));
		}

		/// <summary>Tests: the extras set without the questions.</summary>
		internal void SetNumbers(Dictionary<string, string> a) { numbers = a; Extras(); }
		internal void SetWatermark(Dictionary<string, string> a) { watermark = a; Extras(); }
		internal void SetClean(string mode) { clean = mode; if (mode != "none") AsPdf = true; Extras(); }

		void AskClean()
		{
			var a = Ask.Show(this, T.S("viewer.clean"), T.S("docs.scan.intro"),
				new Ask.Field
				{
					Key = "mode", Label = T.S("docs.scan.mode"), Kind = Ask.Kind.Choice, Value = clean == "none" ? ToolSettings.Text("docs.scanMode", "color") : clean,
					Choices = new[] { ("none", T.S("viewer.cleanNone")), ("color", T.S("docs.scan.color")), ("gray", T.S("docs.scan.gray")), ("bw", T.S("docs.scan.bw")) },
				});
			if (a == null) return;
			clean = a["mode"];
			if (clean != "none") { try { ToolSettings.Set("docs.scanMode", clean); } catch (Exception) { } AsPdf = true; }
			Extras();
		}

		void Extras()
		{
			StampButtons();
			Changed();
		}

		// ------------------------------------------------------------------ saving

		// ------------------------------------------------------------------ the marks

		readonly Dictionary<MarkKind, Button> markTools = new();
		Label markHint;

		/// <summary>A tool chosen (again: put away). The signature is asked for the first time (or when another is wanted).</summary>
		void PickTool(MarkKind kind)
		{
			if (view.Tool == kind) { SetTool(null); return; }
			var menu = new ContextMenuStrip { Font = fText, ShowImageMargin = false };
			Theme.ApplyTo(menu);
			menu.Closed += (s, e) => BeginInvoke(menu.Dispose);
			if (kind == MarkKind.Highlight)
			{
				// the highlighter's colour
				foreach (var (key, emoji) in new[] { ("yellow", "🟨"), ("green", "🟩"), ("pink", "🟪"), ("blue", "🟦") })
					menu.Items.Add(emoji + "  " + T.S("marks.color." + key), null, (s, e) => { highlight = key; SetTool(MarkKind.Highlight); });
				menu.Show(markTools[kind], new Point(0, markTools[kind].Height));
				return;
			}
			if (kind == MarkKind.Sign)
			{
				if (Marks.Signature == null && !NewSignature()) { menu.Dispose(); return; }
				// where and how: on this page, with today's date, on every page (initials on each page of a contract)
				void Sign(bool date, bool all) { signDate = date; signAll = all; SetTool(MarkKind.Sign); }
				menu.Items.Add("✍️  " + T.S("marks.useSign"), null, (s, e) => Sign(false, false));
				menu.Items.Add("📅  " + T.S("marks.signDate"), null, (s, e) => Sign(true, false));
				menu.Items.Add("📑  " + T.S("marks.signAll"), null, (s, e) => Sign(false, true));
				menu.Items.Add(new ToolStripSeparator());
				menu.Items.Add("🖊️  " + T.S("marks.newSign"), null, (s, e) => BeginInvoke(() => { if (NewSignature()) Sign(false, false); }));
				menu.Show(markTools[kind], new Point(0, markTools[kind].Height));
				return;
			}
			menu.Dispose();
			SetTool(kind);
		}

		/// <summary>The highlighter's colour, and how the next signature goes (with the date, on every page).</summary>
		string highlight = "yellow";
		bool signDate, signAll;

		bool NewSignature()
		{
			string made = SignatureForm.Ask(this);
			if (made == null) return false;
			view.ForgetSignatures();
			return true;
		}

		internal void SetTool(MarkKind? kind)
		{
			view.Tool = kind;
			foreach (var (k, b) in markTools)
			{
				bool on = k == kind;
				b.BackColor = on ? Theme.AccentSoft : Theme.Surface;
				b.FlatAppearance.BorderColor = on ? Theme.Accent : Theme.Border;
				b.Invalidate();
			}
			MarkHint();
			if (kind != null) view.Focus();
		}

		void MarkHint()
		{
			if (markHint == null) return;
			markHint.Text = view.Tool switch
			{
				MarkKind.Redact => T.S("marks.redactHint"),
				MarkKind k => T.S("marks.drag"),
				null when view.SelectedMark >= 0 => T.S("marks.selected"),
				_ => "",
			};
		}

		/// <summary>A box was dragged on the page with a tool: the mark goes on the page (its box as before the page's turn).</summary>
		void Drawn(RectangleF shown)
		{
			if (view.Tool is not MarkKind kind || grid.Tiles.Count == 0) return;
			var tile = grid.Tiles[grid.FocusIndex];
			string text = null, image = null;
			if (kind == MarkKind.Text)
			{
				var a = Ask.Show(this, T.S("marks.text"), null, new Ask.Field { Key = "text", Label = T.S("marks.textAsk"), Kind = Ask.Kind.Lines, Hint = T.S("marks.textLines"), Check = v => string.IsNullOrWhiteSpace(v) ? T.S("pdfpages.err.noText") : null });
				if (a == null) return;
				text = a["text"].Trim().Replace("\r\n", "\n");
			}
			if (kind == MarkKind.Highlight) text = highlight;
			if (kind == MarkKind.Sign) image = Marks.Signature;
			if (kind == MarkKind.Sign && image == null) return;
			// a signature on every page goes to the same place of each (as each page is shown)
			var pages = kind == MarkKind.Sign && signAll ? grid.Tiles.ToList() : new List<PagesForm.Tile> { tile };
			foreach (var p in pages)
			{
				p.Marks.Add(new Mark(kind, Marks.Turn(shown, 360 - p.Turn), text, image));
				if (kind == MarkKind.Sign && signDate)
				{
					// today's date under the signature, a third of its height
					float h = Math.Max(0.02f, shown.Height * 0.32f);
					var under = new RectangleF(shown.X, Math.Min(1 - h, shown.Bottom + 0.004f), shown.Width, h);
					p.Marks.Add(new Mark(MarkKind.Text, Marks.Turn(under, 360 - p.Turn), DateTime.Now.ToString("d", Culture())));
				}
			}
			// a text and a signature are put once; a highlighter and the black box stay in the hand
			if (kind is MarkKind.Text or MarkKind.Sign) SetTool(null);
			Changed();
		}

		/// <summary>Dates in the way of the program's language.</summary>
		static System.Globalization.CultureInfo Culture()
		{
			try { return System.Globalization.CultureInfo.GetCultureInfo(T.Code); }
			catch (Exception) { return System.Globalization.CultureInfo.CurrentCulture; }
		}

		/// <summary>Tests: how the next signature goes, and the highlighter's colour, without the menu.</summary>
		internal void TestSignWay(bool date, bool all) { signDate = date; signAll = all; }
		internal void TestHighlight(string color) => highlight = color;

		/// <summary>The mark picked on the page is taken away (Delete).</summary>
		internal bool RemovePickedMark()
		{
			int i = view.SelectedMark;
			if (i < 0 || grid.Tiles.Count == 0) return false;
			var tile = grid.Tiles[grid.FocusIndex];
			if (i >= tile.Marks.Count) return false;
			tile.Marks.RemoveAt(i);
			view.SelectedMark = -1;
			Changed();
			MarkHint();
			return true;
		}

		/// <summary>A mark moved or resized with the mouse: its new box (as before the page's turn) is kept, for undo too.</summary>
		internal void MarkMoved(int i, RectangleF shown)
		{
			if (grid.Tiles.Count == 0) return;
			var tile = grid.Tiles[grid.FocusIndex];
			if (i < 0 || i >= tile.Marks.Count) return;
			tile.Marks[i] = tile.Marks[i] with { Box = Marks.Turn(shown, 360 - tile.Turn) };
			int keep = view.SelectedMark;
			Changed();
			view.SelectedMark = keep;
		}
		
		/// <summary>Tests: a mark put without the mouse.</summary>
		internal void TestMark(MarkKind kind, RectangleF shown) { SetTool(kind); Drawn(shown); }

		/// <summary>A file name short enough for the line under a page.</summary>
		static string Short(string name) => name.Length > 14 ? name[..13] + "…" : name;
		
		internal Dictionary<string, string> Build(bool replaceOriginal)
		{
			var a = new Dictionary<string, string>
			{
				["kind"] = isPdf ? "pdf" : "images",
				["order"] = PdfPick.Text(grid.Tiles.Select(t => t.Page)),
				["save"] = replaceOriginal ? "replace" : "copy",
			};
			var turned = grid.Tiles.Where(t => t.Turn != 0).Select(t => t.Page + ":" + t.Turn).ToList();
			if (turned.Count > 0) a["rotate"] = string.Join(",", turned);
			// the marks, by the page's place in the new order, their boxes on the page as it will be (turned)
			var placed = grid.Tiles.SelectMany((t, i) => t.Marks.Select(m =>
			{
				var b = Marks.Turn(m.Box, t.Turn);
				return new Marks.Placed { at = i, kind = (int)m.Kind, x = b.X, y = b.Y, w = b.Width, h = b.Height, text = m.Text, image = m.Image };
			})).ToList();
			if (placed.Count > 0) a["marks"] = Marks.Write(placed);
			if (isPdf)
			{
				if (numbers != null) { foreach (var kv in numbers) a[kv.Key] = kv.Value; a["numbers"] = "1"; }
				if (watermark != null) { foreach (var kv in watermark) a[kv.Key] = kv.Value; a["watermark"] = "1"; }
			}
			else
			{
				a["clean"] = clean;
				a["as"] = AsPdf ? "pdf" : "images";
			}
			return a;
		}

		void Finish(bool replaceOriginal)
		{
			if (grid.Tiles.Count == 0) return;
			if (replaceOriginal)
			{
				string what = files.Count == 1 ? Path.GetFileName(files[0]) : isPdf ? T.S("viewer.files", files.Count) : T.S("viewer.pictures", files.Count);
				if (MessageBox.Show(this, T.S("viewer.replaceAsk", what), T.S("viewer.replace"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
			}
			Answers = Build(replaceOriginal);
			DialogResult = DialogResult.OK;
		}

		/// <summary>Closing with changes not saved: save a copy, throw them away, or stay.</summary>
		void AskBeforeClosing(object sender, FormClosingEventArgs e)
		{
			if (DialogResult == DialogResult.OK || !Dirty || e.CloseReason != CloseReason.UserClosing || SkipCloseQuestion) return;
			var r = MessageBox.Show(this, T.S("viewer.askSave"), T.S("job.view"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (r == DialogResult.Cancel) { e.Cancel = true; DialogResult = DialogResult.None; return; }
			if (r == DialogResult.Yes) { Answers = Build(false); DialogResult = DialogResult.OK; }
		}

		/// <summary>Tests close the window without the question.</summary>
		internal bool SkipCloseQuestion;

		protected override bool ProcessCmdKey(ref Message msg, Keys keys)
		{
			switch (keys)
			{
				case Keys.Control | Keys.Z: Undo(); return true;
				case Keys.Control | Keys.Y:
				case Keys.Control | Keys.Shift | Keys.Z: Redo(); return true;
				case Keys.Control | Keys.R: grid.Turn(90); return true;
				case Keys.Control | Keys.L: grid.Turn(270); return true;
				case Keys.Control | Keys.S: if (saveCopy.Enabled) Finish(false); return true;
				case Keys.Control | Keys.Oemplus:
				case Keys.Control | Keys.Add: view.ZoomIn(); return true;
				case Keys.Control | Keys.OemMinus:
				case Keys.Control | Keys.Subtract: view.ZoomOut(); return true;
				case Keys.Control | Keys.D0:
				case Keys.Control | Keys.NumPad0: view.Fit(); return true;
				case Keys.PageDown: grid.FocusOn(grid.FocusIndex + 1); return true;
				case Keys.PageUp: grid.FocusOn(grid.FocusIndex - 1); return true;
				case Keys.Delete when ActiveControl is not TextBox: if (!RemovePickedMark()) grid.DeleteSelected(); return true;
				case Keys.Escape when view.Tool != null: SetTool(null); return true;
				case Keys.Control | Keys.A when ActiveControl is not TextBox: grid.SelectAll(); return true;
			}
			return base.ProcessCmdKey(ref msg, keys);
		}

		// ------------------------------------------------------------------ the pictures

		/// <summary>The PDF into memory, then the small pictures one by one (the window can be used meanwhile).</summary>
		async Task Start()
		{
			try
			{
				if (isPdf)
				{
					var bytes = await File.ReadAllBytesAsync(source, closing.Token);
					pdfStream = new InMemoryRandomAccessStream();
					await pdfStream.WriteAsync(bytes.AsBuffer());
					pdfStream.Seek(0);
					pdfDoc = await WinPdf.PdfDocument.LoadFromStreamAsync(pdfStream);
				}
			}
			catch (Exception) { pdfDoc = null; }
			await ShowCurrent();
			int n = isPdf ? (int)(pdfDoc?.PageCount ?? 0) : files.Count;
			for (int i = 1; i <= n && !closing.IsCancellationRequested; i++)
			{
				try
				{
					var small = isPdf ? await RenderPdf(i, PagesForm.PageGrid.Thumb * 2) : await LoadPicture(files[i - 1], "none", PagesForm.PageGrid.Thumb * 2);
					if (closing.IsCancellationRequested) { small?.Dispose(); break; }
					if (small != null) grid.SetPicture(i, small);
				}
				catch (Exception) { }
			}
			ThumbsDone = true;
		}

		/// <summary>The small pictures are all drawn (tests wait for it).</summary>
		internal bool ThumbsDone { get; private set; }

		/// <summary>The pictures being drawn (small ones, the big page).</summary>
		Task loading, bigTask;

		/// <summary>
		/// The window closed: what Windows is still drawing is waited for (up to 3 s). Drawing left behind when the
		/// program ends can keep it from ending (Windows draws PDFs with the graphics card).
		/// </summary>
		void WaitDrawing()
		{
			var until = DateTime.Now.AddSeconds(3);
			while (DateTime.Now < until && ((loading != null && !loading.IsCompleted) || (bigTask != null && !bigTask.IsCompleted)))
			{
				Application.DoEvents();
				Thread.Sleep(15);
			}
		}
		/// <summary>The big page shown is the one of the current state (tests wait for it).</summary>
		internal bool BigDone { get; private set; }

		async Task<Bitmap> RenderPdf(int page, int longSide)
		{
			if (pdfDoc == null) return null;
			using var p = pdfDoc.GetPage((uint)(page - 1));
			return await Render(p, longSide);
		}

		static async Task<Bitmap> Render(WinPdf.PdfPage p, int longSide)
		{
			using var ms = new InMemoryRandomAccessStream();
			var opts = new WinPdf.PdfPageRenderOptions();
			if (p.Size.Width >= p.Size.Height) opts.DestinationWidth = (uint)longSide; else opts.DestinationHeight = (uint)longSide;
			await p.RenderToStreamAsync(ms, opts);
			using var s = ms.AsStreamForRead();
			using var loaded = new Bitmap(s);
			return new Bitmap(loaded);
		}

		static async Task<Bitmap> LoadPicture(string file, string cleaning, int longSide)
		{
			var px = await ViewerJobs.Picture(file, cleaning, 0, default);
			return await Task.Run(() => ViewerJobs.ToBitmap(ScanImage.Smaller(px, longSide)));
		}

		/// <summary>The size big pages are drawn at: enough for the screen, and for a zoom of 2.</summary>
		const int BigSide = 2200;

		/// <summary>Shows the page with the focus big. A newer change wins over one still being drawn.</summary>
		async Task ShowCurrent()
		{
			int ticket = ++showing;
			BigDone = false;
			if (grid.Tiles.Count == 0) { view.Show(null, 0); BigDone = true; return; }
			int index = grid.FocusIndex;
			var tile = grid.Tiles[index];
			// the marks of this page over it, turned as the page is shown
			view.SetMarks(tile.Marks.Select(m => m with { Box = Marks.Turn(m.Box, tile.Turn) }).ToList());
			bool stamped = isPdf && (numbers != null || watermark != null);
			// a preview with numbers or a watermark depends on the pages and those (not on the marks, drawn over it)
			string stamps = string.Join(",", grid.Tiles.Select(t => t.Page + ":" + t.Turn)) + "|" + string.Join(";", (numbers ?? new()).Concat(watermark ?? new()).Select(x => x.Key + "=" + x.Value));
			string key = isPdf ? stamped ? "s|" + index + "|" + stamps : "p|" + tile.Page : "i|" + tile.Page + "|" + clean;
			int turn = stamped ? 0 : tile.Turn;
			if (big.TryGetValue(key, out var have)) { view.Show(have, turn); BigDone = true; return; }
			view.Wait(T.S("viewer.drawing"));
			Bitmap pic = null;
			try
			{
				if (!stamped && isPdf) pic = await RenderPdf(tile.Page, BigSide);
				else if (stamped)
				{
					// the page exactly as it will be saved: made by the same code into a small temporary PDF
					await Task.Delay(150);
					if (ticket != showing) return;
					var options = Build(false);
					options.Remove("marks");
					string tmp = await Task.Run(() => ViewerJobs.BuildPdf(source, options, index, closing.Token));
					try
					{
						var f = await Windows.Storage.StorageFile.GetFileFromPathAsync(tmp);
						var doc = await WinPdf.PdfDocument.LoadFromFileAsync(f);
						using var p = doc.GetPage(0);
						pic = await Render(p, BigSide);
					}
					finally { _ = Task.Run(async () => { await Task.Delay(500); try { File.Delete(tmp); } catch (Exception) { } }); }
				}
				else
				{
					if (clean != "none") view.Wait(T.S("viewer.cleaning"));
					pic = await LoadPicture(files[tile.Page - 1], clean, BigSide);
				}
			}
			catch (OperationCanceledException) { return; }
			catch (Exception ex)
			{
				if (ticket == showing) { view.Show(null, 0); view.Wait(Jobs.Friendly(ex)); BigDone = true; }
				return;
			}
			if (pic == null || IsDisposed) { pic?.Dispose(); return; }
			Keep(key, pic);
			if (ticket != showing) return;
			view.Show(pic, turn);
			BigDone = true;
		}

		/// <summary>A few big pages are kept (going back and forth is quick); older ones are let go, never the one shown.</summary>
		void Keep(string key, Bitmap pic)
		{
			if (big.TryGetValue(key, out var old) && !ReferenceEquals(old, pic)) { if (!ReferenceEquals(old, view.Picture)) old.Dispose(); }
			big[key] = pic;
			bigOrder.Remove(key);
			bigOrder.Add(key);
			foreach (var k in bigOrder.Take(Math.Max(0, bigOrder.Count - 10)).ToList())
			{
				if (k == key || (big.TryGetValue(k, out var b) && ReferenceEquals(b, view.Picture))) continue;
				bigOrder.Remove(k);
				if (big.Remove(k, out var gone)) gone.Dispose();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				fText.Dispose(); fSmall.Dispose(); fBold.Dispose(); closing.Dispose();
				foreach (var b in big.Values) b.Dispose();
				big.Clear();
				pdfStream?.Dispose();
				if (from != null) try { File.Delete(source); } catch (Exception) { }
			}
			base.Dispose(disposing);
		}
	}
}
