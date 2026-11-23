using System.Diagnostics;
using System.Drawing;
using DesktopPet;

namespace PetEditor
{
	/// <summary>Project: new pet, open (file, folder, a pet of DesktopPet), the recent ones, save as, and what is in this pet.</summary>
	internal sealed class ProjectPage : Page
	{
		FlowLayoutPanel body;

		public override void Bind(MainForm main, PetDoc doc)
		{
			base.Bind(main, doc);
			Controls.Clear();
			var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };
			body = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Theme.Background };
			scroll.Controls.Add(body);
			Controls.Add(scroll);
			Build();
		}

		void Build()
		{
			body.SuspendLayout();
			foreach (Control c in body.Controls) c.Dispose();
			body.Controls.Clear();
			body.Controls.Add(new Label { Text = L.T("proj.title"), AutoSize = true, Font = Ui.Font(22, true), ForeColor = Theme.Text, Margin = new Padding(0, 0, 0, 4) });
			body.Controls.Add(Ui.Hint(L.T("proj.intro"), 900));

			// actions
			var actions = Ui.Wrap(new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Theme.Background, Margin = new Padding(0, 6, 0, 10) }, 1200);
			actions.Controls.Add(Action("proj.wizard", "proj.wizardHint", () => Wizard.Show(Main)));
			actions.Controls.Add(Action("proj.character", "proj.characterHint", () => CharacterDialog.Show(Main)));
			actions.Controls.Add(Action("proj.new", "proj.newHint", Main.NewPet));
			actions.Controls.Add(Action("proj.gallery", "proj.galleryHint", () => GalleryDialog.Show(Main, Doc)));
			actions.Controls.Add(Action("proj.openFile", "proj.openFileHint", () => Main.OpenDialog(false)));
			actions.Controls.Add(Action("proj.openFolder", "proj.openFolderHint", () => Main.OpenDialog(true)));
			actions.Controls.Add(Action("proj.saveAs", "proj.saveAsHint", () => Main.Save(true)));
			actions.Controls.Add(Action("proj.zip", "proj.zipHint", () => GalleryDialog.Zip(Main, Doc)));
			actions.Controls.Add(Action("proj.publish", "proj.publishHint", () => GalleryDialog.Publish(Main, Doc)));
			ActionTile.SameHeight(actions);
			actions.Layout += (s, e) => ActionTile.SameHeight(actions);   // a new zoom or language changes the texts
			body.Controls.Add(actions);

			// this pet
			var info = Ui.Stretch(new Card { Width = 900, Height = 150 }, 320, 1200);
			var r = Doc.Root;
			var col = Ui.Column(
				Ui.Title(L.T("proj.thisPet", Doc.Name), 16),
				Ui.Text(Doc.FilePath ?? L.T("doc.notSaved"), true, 12),
				Ui.Text(L.T("proj.stats", r.Animations.Animation.Length, Doc.Frames.Count, Doc.FrameW, Doc.FrameH, r.Spawns.Spawn.Length, r.Childs.Child.Length, r.Sounds.Sound.Length), false, 13));
			col.Location = new Point(16, 14);
			info.Controls.Add(col);
			if (Doc.FilePath != null)
			{
				var folder = new FlatButton(L.T("proj.showFolder"));
				folder.Margin = new Padding(0, 8, 0, 0);
				folder.Click += (s, e) => Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{Doc.FilePath}\"") { UseShellExecute = true });
				col.Controls.Add(folder);
			}
			FitHeight(info, col);
			body.Controls.Add(info);

			// pets of DesktopPet (downloaded from the gallery) and recent projects
			body.Controls.Add(ListCard(L.T("proj.recent"), EditorSettings.Current.Recent.Where(File.Exists).Select(p => (Path.GetFileName(Path.GetDirectoryName(p)) ?? p, p)).ToList(), L.T("proj.noRecent")));
			string store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "pets");
			var mine = Directory.Exists(store)
				? Directory.GetDirectories(store).Where(d => File.Exists(Path.Combine(d, "animations.xml"))).Select(d => (Path.GetFileName(d), Path.Combine(d, "animations.xml"))).ToList()
				: new List<(string, string)>();
			body.Controls.Add(ListCard(L.T("proj.installed"), mine, L.T("proj.noInstalled"), L.T("proj.installedHint")));
			body.ResumeLayout();
		}

		/// <summary>The card follows the height of its content (texts break their lines when the window is narrow).</summary>
		static void FitHeight(Card card, Control content)
		{
			void Fit() => card.Height = content.Top + content.Height + Ui.S(18);
			content.SizeChanged += (s, e) => Fit();
			Fit();
		}

		Control Action(string title, string hint, Action click)
		{
			var c = new ActionTile(L.T(title), L.T(hint)) { Width = 262 };
			c.Click += (s, e) => click();
			return c;
		}

		/// <summary>A list of pets as cards: the picture of the pet, its name, its folder and when it was changed. A click opens it.</summary>
		Control ListCard(string title, List<(string name, string path)> items, string empty, string hint = null)
		{
			var card = Ui.Stretch(new Card { Width = 900 }, 320, 1200);
			var col = Ui.Column(Ui.Title(title, 16));
			if (hint != null) col.Controls.Add(Ui.Hint(hint, 1000));
			if (items.Count == 0) col.Controls.Add(Ui.Text(empty, true));
			else
			{
				var tiles = Ui.Wrap(new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Theme.Surface, Margin = new Padding(0, 6, 0, 0) }, 1160);
				foreach (var (name, path) in items)
				{
					var t = new PetTile(name, path) { Width = Ui.S(250), Height = Ui.S(84) };
					t.Click += (s, e) => Main.OpenPath(path);
					tiles.Controls.Add(t);
				}
				col.Controls.Add(tiles);
			}
			col.Location = new Point(16, 14);
			card.Controls.Add(col);
			FitHeight(card, col);
			return card;
		}

		/// <summary>The picture and name of a pet file, read once (again only when the file changes).</summary>
		static readonly Dictionary<string, (DateTime when, Bitmap img, string name)> thumbs = new(StringComparer.OrdinalIgnoreCase);

		internal static (Bitmap img, string name) Thumb(string path)
		{
			DateTime when;
			try { when = File.GetLastWriteTimeUtc(path); } catch { return (null, null); }
			if (thumbs.TryGetValue(path, out var t) && t.when == when) return (t.img, t.name);
			Bitmap img = null; string name = null;
			try
			{
				var root = XmlData.AnimationXML.ParseXML(File.ReadAllText(path));
				name = root?.Header?.Petname;
				img = Icons.FromIco(root?.Header?.Icon, 64);
				if (img == null && !string.IsNullOrWhiteSpace(root?.Image?.Png))
				{
					// no icon: the first frame of the sheet
					using var ms = new MemoryStream(Convert.FromBase64String(root.Image.Png.Trim()));
					using var raw = new Bitmap(ms);
					int w = Math.Max(1, raw.Width / Math.Max(1, root.Image.TilesX)), h = Math.Max(1, raw.Height / Math.Max(1, root.Image.TilesY));
					img = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					using (var g = Graphics.FromImage(img)) g.DrawImage(raw, new Rectangle(0, 0, w, h), new Rectangle(0, 0, w, h), GraphicsUnit.Pixel);
					var key = Color.FromName(string.IsNullOrWhiteSpace(root.Image.Transparency) ? "Magenta" : root.Image.Transparency);
					if (key.IsKnownColor && key.A > 0) img.MakeTransparent(key);
				}
			}
			catch (Exception e) { System.Diagnostics.Debug.WriteLine("thumb: " + e.Message); }
			if (thumbs.TryGetValue(path, out var old)) old.img?.Dispose();
			thumbs[path] = (when, img, name);
			return (img, name);
		}

		/// <summary>A pet as a card: picture on the left, name, folder and the day it was changed.</summary>
		sealed class PetTile : Control
		{
			readonly string path, folder; bool hover;
			public PetTile(string folder, string path)
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				this.path = path; this.folder = folder; Cursor = Cursors.Hand; Margin = new Padding(0, 0, 10, 10);
				var tip = new ToolTip(); tip.SetToolTip(this, path);
			}
			protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
			protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics; Theme.Prepare(g); g.Clear(Theme.Surface);
				var r = new Rectangle(0, 0, Width - 1, Height - 1);
				Theme.FillRounded(g, hover ? Theme.AccentSoft : Theme.Background, r, 10);
				Theme.DrawRounded(g, hover ? Theme.Accent : Theme.Border, r, 10);
				var (img, name) = Thumb(path);
				int pic = Height - Ui.S(20);
				var box = new Rectangle(Ui.S(10), Ui.S(10), pic, pic);
				Theme.FillRounded(g, Theme.Surface, box, 8);
				if (img != null)
				{
					float k = Math.Min((pic - Ui.S(8)) / (float)img.Width, (pic - Ui.S(8)) / (float)img.Height);
					if (k >= 1) k = (float)Math.Floor(k);   // pixel art: whole steps when it grows
					int w = (int)(img.Width * k), h = (int)(img.Height * k);
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
					g.DrawImage(img, new Rectangle(box.X + (pic - w) / 2, box.Y + (pic - h) / 2, w, h));
				}
				int tx = box.Right + Ui.S(12), tw = Width - tx - Ui.S(10);
				string title = string.IsNullOrWhiteSpace(name) ? folder : name;
				var flags = TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
				TextRenderer.DrawText(g, title, Ui.Font(14, true), new Rectangle(tx, Ui.S(12), tw, Ui.S(22)), hover ? Theme.Accent : Theme.Text, flags);
				TextRenderer.DrawText(g, folder, Ui.Font(11.5f), new Rectangle(tx, Ui.S(36), tw, Ui.S(18)), Theme.TextMuted, flags | TextFormatFlags.PathEllipsis);
				string when = File.Exists(path) ? L.T("proj.changed", File.GetLastWriteTime(path).ToString("d")) : "";
				TextRenderer.DrawText(g, when, Ui.Font(11.5f), new Rectangle(tx, Ui.S(54), tw, Ui.S(18)), Theme.TextMuted, flags);
			}
		}

		public override void Reload(string what) { if (what is "all" or "header" or "image" or "anims") Build(); }
		public override void Shown() => Build();

		/// <summary>A big clickable tile: title and a line of explanation.</summary>
		sealed class ActionTile : Control
		{
			readonly string hint; bool hover;
			public ActionTile(string title, string hint)
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				Text = title; this.hint = hint; Cursor = Cursors.Hand; Margin = new Padding(0, 0, 12, 12);
				Height = 96;
			}

			/// <summary>The height its explanation needs (all the tiles take the tallest one: <see cref="SameHeight"/>).</summary>
			public int Needed => Math.Max(Ui.S(96), Ui.S(50) + TextRenderer.MeasureText(hint, Ui.Font(12), new Size(Width - Ui.S(28), 1000), TextFormatFlags.WordBreak).Height + Ui.S(14));

			/// <summary>Every tile of <paramref name="row"/> as tall as the tallest one (all the texts seen, the boxes in line).</summary>
			public static void SameHeight(Control row)
			{
				var tiles = row.Controls.OfType<ActionTile>().ToList();
				if (tiles.Count == 0) return;
				int h = tiles.Max(t => t.Needed);
				foreach (var t in tiles) if (t.Height != h) t.Height = h;
			}

			int lastWidth;
			protected override void OnSizeChanged(EventArgs e)
			{
				base.OnSizeChanged(e);
				if (Width != lastWidth && Parent != null) { lastWidth = Width; SameHeight(Parent); }   // the text breaks in other places
				Invalidate();
			}
			protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
			protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics; Theme.Prepare(g); g.Clear(Theme.Background);
				var r = new Rectangle(0, 0, Width - 1, Height - 1);
				Theme.FillRounded(g, hover ? Theme.AccentSoft : Theme.Surface, r, 10);
				Theme.DrawRounded(g, hover ? Theme.Accent : Theme.Border, r, 10);
				TextRenderer.DrawText(g, Text, Ui.Font(15, true), new Rectangle(Ui.S(16), Ui.S(14), Width - Ui.S(28), Ui.S(24)), hover ? Theme.Accent : Theme.Text, TextFormatFlags.Left);
				TextRenderer.DrawText(g, hint, Ui.Font(12), new Rectangle(Ui.S(16), Ui.S(42), Width - Ui.S(28), Height - Ui.S(48)), Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
			}
		}
	}
}
