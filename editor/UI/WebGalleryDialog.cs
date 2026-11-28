using System.Drawing;
using DesktopPet;

namespace PetEditor
{
	/// <summary>
	/// "Open from the gallery": the pets of DeskPals' gallery and of any GitHub repository the user adds, as cards with
	/// their icon; a click brings the pet to this computer and opens it (as a pet not saved yet: "Save" asks where).
	/// "Open from a link": a pet's link opens it at once; a repository's link opens this window searching it.
	/// </summary>
	internal static class WebGalleryDialog
	{
		/// <summary>The icons already brought (by address), for this run of the editor.</summary>
		static readonly Dictionary<string, Image> icons = new();

		public static void Show(MainForm main, WebPets.Source start = null, List<WebPets.Pet> found = null)
		{
			var sources = new List<WebPets.Source> { WebPets.Official };
			sources.AddRange(WebPets.Saved());
			if (start != null && !sources.Any(s => s.Key == start.Key)) { sources.Add(start); WebPets.Save(sources); }

			using var f = new Form { Text = L.T("web.gallery"), Size = new Size(940, 680), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, ShowInTaskbar = false, BackColor = Theme.Surface, Font = Ui.Font(13) };
			Theme.ApplyWindowChrome(f);
			Ui.AutoFit(f);
			var which = new Choice(320);
			var add = new FlatButton("+  " + L.T("web.sourceAdd")).Tip(L.T("web.sourceHint"));
			var again = new FlatButton(L.T("web.sourceRefresh"));
			var remove = new FlatButton(L.T("web.sourceRemove")) { Danger = true };
			var status = Ui.Hint(L.T("web.galleryHint"), 860);
			var search = Ui.Box("", 300); search.PlaceholderText = L.T("anim.search");
			var top = Ui.Column(Ui.Title(L.T("web.gallery"), 16), Ui.Row(which, add, again, remove), status, search);
			top.Dock = DockStyle.Top; top.Padding = new Padding(14, 12, 14, 0);
			var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, BackColor = Theme.Background, Padding = new Padding(10) };
			f.Controls.Add(grid); f.Controls.Add(top);

			List<WebPets.Pet> pets = new();
			int asked = 0;   // the latest search: an older one that answers late is left aside

			void FillSources(WebPets.Source select)
			{
				which.Items.Clear();
				foreach (var s in sources) which.Items.Add(s.Label);
				which.SelectedIndex = Math.Max(0, sources.FindIndex(s => s.Key == (select ?? sources[0]).Key));
			}

			WebPets.Source Current() => which.SelectedIndex >= 0 && which.SelectedIndex < sources.Count ? sources[which.SelectedIndex] : sources[0];

			void Draw()
			{
				grid.SuspendLayout();
				foreach (Control c in grid.Controls.Cast<Control>().ToList()) c.Dispose();
				grid.Controls.Clear();
				string q = search.Text.Trim();
				var src = Current();
				foreach (var p in pets.Where(p => q.Length == 0 || p.Shown.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Folder.Contains(q, StringComparison.OrdinalIgnoreCase) || (p.Author ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).Take(400))
				{
					var card = new WebPetCard(p.Shown, p.Author ?? src.Owner) { Width = Ui.S(200), Height = Ui.S(70), Margin = new Padding(Ui.S(5)) };
					card.Click += async (s, e) => await Open(main, f, src, p);
					grid.Controls.Add(card);
					if (p.Icon) _ = Icon(card, p.Url(src) + "icon.png");
				}
				grid.ResumeLayout();
			}

			async Task Load(WebPets.Source s, List<WebPets.Pet> ready = null)
			{
				int me = ++asked;
				remove.Visible = !s.Official;
				pets = new();
				Draw();
				status.Text = L.T("web.sourceSearching", s.Label);
				try
				{
					var list = ready ?? await WebPets.Pets(s);
					if (me != asked || f.IsDisposed) return;
					pets = list;
					status.Text = s.Official ? L.T("web.galleryHint") : L.T("web.sourceFound", list.Count, s.Label) + (list.Count >= WebPets.MaxScanned ? " " + L.T("web.sourceMore", WebPets.MaxScanned) : "");
					Draw();
				}
				catch (WebPets.Problem ex) { if (me == asked && !f.IsDisposed) status.Text = ex.Message; }
				catch (Exception ex) { if (me == asked && !f.IsDisposed) status.Text = L.T("web.loadFail", ex.Message); }
			}

			which.SelectedIndexChanged += async (s, e) => await Load(Current());
			search.TextChanged += (s, e) => Draw();
			again.Click += async (s, e) => await Load(Current());
			remove.Click += async (s, e) =>
			{
				var cur = Current();
				if (cur.Official) return;
				sources.RemoveAll(x => x.Key == cur.Key);
				WebPets.Save(sources);
				FillSources(null);
				await Load(Current());
			};
			add.Click += async (s, e) =>
			{
				string text = AskText(f, L.T("web.sourceAsk"), "");
				if (string.IsNullOrWhiteSpace(text)) return;
				var link = WebPets.Parse(text);
				if (link == null) { status.Text = L.T("web.sourceBad"); return; }
				status.Text = L.T("web.sourceSearching", link.Owner + "/" + link.Repo);
				try
				{
					var (src, list) = await WebPets.Check(link);
					if (f.IsDisposed) return;
					sources.RemoveAll(x => x.Key == src.Key);
					sources.Add(src);
					WebPets.Save(sources);
					asked++;
					FillSources(src);   // starts a search of its own: the list just found is used instead
					await Load(src, list);
				}
				catch (WebPets.Problem ex) { status.Text = ex.Message; }
			};

			f.Shown += async (s, e) =>
			{
				FillSources(start);
				if (found != null) await Load(Current(), found);
			};
			f.ShowDialog(main);
		}

		/// <summary>The icon of a card, brought once.</summary>
		static async Task Icon(WebPetCard card, string url)
		{
			if (!icons.TryGetValue(url, out var img))
			{
				var bytes = await WebPets.Bytes(url);
				if (bytes != null) { try { img = Image.FromStream(new MemoryStream(bytes)); } catch (Exception) { img = null; } }
				icons[url] = img;
			}
			if (!card.IsDisposed && img != null) card.Picture = img;
		}

		/// <summary>The pet brought to this computer and opened (a pet not saved yet: "Save" asks where).</summary>
		static async Task Open(MainForm main, Form dialog, WebPets.Source s, WebPets.Pet p)
		{
			if (!main.ConfirmDiscard()) return;
			dialog.UseWaitCursor = true;
			try
			{
				string file = await WebPets.Download(s, p);
				OpenDownloaded(main, file);
				dialog.Close();
			}
			catch (Exception ex) { main.Message(L.T("web.loadFail", ex.Message)); }
			finally { if (!dialog.IsDisposed) dialog.UseWaitCursor = false; }
		}

		static void OpenDownloaded(MainForm main, string file)
		{
			var d = PetDoc.Open(file);
			d.FilePath = null;   // from the internet: saved where the user says
			main.SetDoc(d);
		}

		/// <summary>"Open from a link": a pet opens at once; a repository (or a folder of pets) opens the gallery searching it.</summary>
		public static async void OpenLink(MainForm main)
		{
			string text = AskText(main, L.T("web.openUrlAsk"), Clipboard.ContainsText() && WebPets.Parse(Clipboard.GetText()) != null ? Clipboard.GetText().Trim() : "");
			if (string.IsNullOrWhiteSpace(text)) return;
			var link = WebPets.Parse(text);
			if (link == null) { main.Message(L.T("web.sourceBad")); return; }
			main.UseWaitCursor = true;
			try
			{
				// a pet's own folder (or its animations.xml): opened at once
				if (link.Path != null && link.Branch != null)
				{
					var one = new WebPets.Source { Owner = link.Owner, Repo = link.Repo, Branch = link.Branch, Path = link.Path };
					var pet = new WebPets.Pet { Folder = link.Path.Length > 0 ? link.Path[(link.Path.LastIndexOf('/') + 1)..] : link.Repo, Dir = "" };
					if (await WebPets.Bytes(one.BaseUrl + "animations.xml") != null)
					{
						if (!main.ConfirmDiscard()) return;
						OpenDownloaded(main, await WebPets.Download(one, pet));
						return;
					}
				}
				var (src, list) = await WebPets.Check(link);
				main.UseWaitCursor = false;
				Show(main, src, list);
			}
			catch (WebPets.Problem ex) { main.Message(ex.Message); }
			catch (Exception ex) { main.Message(L.T("web.loadFail", ex.Message)); }
			finally { main.UseWaitCursor = false; }
		}

		/// <summary>A small window that asks for one line of text (null: cancelled).</summary>
		internal static string AskText(IWin32Window owner, string question, string text)
		{
			using var f = new Form { Text = L.T("app.name"), Size = new Size(620, 200), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog, ShowInTaskbar = false, BackColor = Theme.Surface, Font = Ui.Font(13) };
			Theme.ApplyWindowChrome(f);
			Ui.AutoFit(f);
			var box = Ui.Box(text, 560);
			var col = Ui.Column(Ui.Text(question), box);
			col.Location = new Point(16, 14);
			var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Surface, Padding = new Padding(10, 8, 10, 0) };
			var ok = new FlatButton("OK", primary: true); ok.Click += (s, e) => { f.DialogResult = DialogResult.OK; f.Close(); };
			var cancel = new FlatButton(L.T("build.cancel")); cancel.Click += (s, e) => f.Close();
			bar.Controls.Add(ok); bar.Controls.Add(cancel);
			f.Controls.Add(col); f.Controls.Add(bar);
			f.AcceptButton = null;
			box.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ok.PerformClick(); } };
			return f.ShowDialog(owner) == DialogResult.OK ? box.Text.Trim() : null;
		}

		/// <summary>A pet of the gallery: its icon, its name and who made it.</summary>
		sealed class WebPetCard : Control
		{
			readonly string name, author;
			bool hover;
			Image picture;
			public Image Picture { set { picture = value; Invalidate(); } }

			public WebPetCard(string name, string author)
			{
				this.name = name; this.author = author;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				Cursor = Cursors.Hand;
				BackColor = Theme.Background;
			}

			protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
			protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics; Theme.Prepare(g);
				var r = new Rectangle(0, 0, Width - 1, Height - 1);
				Theme.FillRounded(g, Theme.Surface, r, 8);
				Theme.DrawRounded(g, hover ? Theme.Accent : Theme.Border, r, 8);
				int s = Height - Ui.S(16);
				var box = new Rectangle(Ui.S(8), Ui.S(8), s, s);
				if (picture != null) Ui.DrawFrame(g, picture, box);
				else TextRenderer.DrawText(g, "🐾", Ui.Font(20), box, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				int x = box.Right + Ui.S(8), w = Width - x - Ui.S(6);
				TextRenderer.DrawText(g, name, Ui.Font(13, true), new Rectangle(x, Ui.S(12), w, Ui.S(22)), Theme.Text, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
				TextRenderer.DrawText(g, author ?? "", Ui.Font(11.5f), new Rectangle(x, Ui.S(36), w, Ui.S(18)), Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
			}
		}
	}
}
