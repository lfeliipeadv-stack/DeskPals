using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== the pet brings a note

	/// <summary>
	/// Off unless turned on. Now and then the pet comes in carrying a little paper note that stays on the screen until it
	/// is closed (or for an hour): the reminders when they ring, and once a day the photo of the day of Wikipedia (Wikimedia
	/// Commons, with its description and a link to it). The note slides in from the side of the screen to the pet, goes with
	/// it for a few steps and is put down; it can be dragged anywhere.
	/// </summary>
	public static class NoteDelivery
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("PetNotes", false);
			set { Program.MyData.SetSetting("PetNotes", value); Apply(); }
		}

		/// <summary>Reminders also come as a note.</summary>
		public static bool Reminders
		{
			get => Program.MyData.GetSettingBool("PetNotesReminders", true);
			set => Program.MyData.SetSetting("PetNotesReminders", value);
		}

		/// <summary>The photo of the day, once a day.</summary>
		public static bool Photo
		{
			get => Program.MyData.GetSettingBool("PetNotesPhoto", true);
			set => Program.MyData.SetSetting("PetNotesPhoto", value);
		}

		public static readonly int[] HourChoices = { 9, 10, 12, 14, 16 };
		public static int PhotoHour
		{
			get { int v = Program.MyData.GetSettingInt("PetNotesHour", 10); return HourChoices.Contains(v) ? v : 10; }
			set => Program.MyData.SetSetting("PetNotesHour", value);
		}

		static Timer timer;
		static readonly List<NoteWindow> notes = new List<NoteWindow>();
		internal static IReadOnlyList<NoteWindow> Open => notes;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 10 * 60 * 1000 }; timer.Tick += async (s, e) => await Check(); }
			if (Enabled) timer.Start(); else timer.Stop();
		}

		/// <summary>Once a day, after the chosen hour: the photo of the day.</summary>
		static async Task Check()
		{
			if (!Enabled || !Photo || SpeechBubble.QuietNow || DateTime.Now.Hour < PhotoHour || DateTime.Now.Hour >= 22) return;
			string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			if (Program.MyData.GetSetting("PetNotesPhotoDay") == today) return;
			Program.MyData.SetSetting("PetNotesPhotoDay", today);
			await BringPhoto();
		}

		/// <summary>A reminder rang: it also comes as a note (when the plugin and that option are on).</summary>
		public static void Reminder(string text)
		{
			if (Enabled && Reminders && !string.IsNullOrWhiteSpace(text)) Bring("⏰", Lang.T("note.reminder"), text);
		}

		/// <summary>"Bring one now": the photo of the day; without internet, today's tasks.</summary>
		public static async Task BringNow()
		{
			if (await BringPhoto()) return;
			var open = Notes.Tasks.Where(t => !t.Done).Select(t => "• " + t.Text).Take(6).ToList();
			Bring("📝", Lang.T("note.tasks"), open.Count > 0 ? string.Join("\n", open) : Lang.T("note.noTasks"));
		}

		// ------------------------------------------------------------------ the photo of the day

		static readonly HttpClient http = CreateClient();
		static HttpClient CreateClient()
		{
			var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
			c.DefaultRequestHeaders.UserAgent.ParseAdd("DeskPals/1.1 (desktop pet)");
			return c;
		}

		/// <summary>The photo of the day: its picture, its description and its page; null when it could not be read.</summary>
		internal static async Task<(Image image, string text, string link)?> PhotoOfTheDay(DateTime day)
		{
			try
			{
				string lang = Lang.Current.Split('-')[0].ToLowerInvariant();
				string url = $"https://api.wikimedia.org/feed/v1/wikipedia/{lang}/featured/{day:yyyy}/{day:MM}/{day:dd}";
				var j = JsonNode.Parse(await http.GetStringAsync(url));
				var img = j?["image"];
				string thumb = (string)img?["thumbnail"]?["source"];
				if (string.IsNullOrEmpty(thumb) || !thumb.StartsWith("https://upload.wikimedia.org/", StringComparison.Ordinal)) return null;
				string text = ((string)img?["description"]?["text"] ?? "").Trim();
				if (text.Length > 220) text = text.Substring(0, 217).TrimEnd() + "…";
				string link = (string)img?["file_page"];
				if (link != null && !link.StartsWith("https://commons.wikimedia.org/", StringComparison.Ordinal)) link = null;
				var bytes = await http.GetByteArrayAsync(thumb);
				if (bytes.Length > 6 * 1024 * 1024) return null;
				Image picture;
				using (var ms = new MemoryStream(bytes)) using (var raw = Image.FromStream(ms)) picture = Fit(raw, 256, 180);
				return (picture, text, link);
			}
			catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "photo of the day: " + ex.Message); return null; }
		}

		static Image Fit(Image src, int maxW, int maxH)
		{
			double k = Math.Min(1.0, Math.Min(maxW / (double)src.Width, maxH / (double)src.Height));
			var bmp = new Bitmap(Math.Max(1, (int)(src.Width * k)), Math.Max(1, (int)(src.Height * k)));
			using (var g = Graphics.FromImage(bmp)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(src, 0, 0, bmp.Width, bmp.Height); }
			return bmp;
		}

		static async Task<bool> BringPhoto()
		{
			var p = await PhotoOfTheDay(DateTime.Now);
			if (p == null) return false;
			Bring("🖼️", Lang.T("note.photo"), p.Value.text, p.Value.image, p.Value.link);
			return true;
		}

		// ------------------------------------------------------------------ the note

		/// <summary>The pet brings a note: it comes in from the side of the screen to the pet and is put down beside it.</summary>
		public static NoteWindow Bring(string emoji, string title, string text, Image image = null, string link = null)
		{
			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => !p.IsChildWindow && p.Visible && !p.IsDisposed);
			while (notes.Count >= 5) notes[0].Close();   // never a pile of papers
			var note = new NoteWindow(emoji, title, text, image, link);
			notes.Add(note);
			note.FormClosed += (s, e) => notes.Remove(note);
			note.Carry(pet);
			Program.Mainthread?.ReactAll("wave", "happy", "hello");
			return note;
		}

		public sealed class NoteWindow : Form
		{
			readonly string emoji, title, text, link;
			readonly Image image;
			readonly Font fTitle = Theme.CreateFont(12.5f, FontStyle.Bold), fText = Theme.CreateFont(11.5f), fEmoji = Theme.CreateFont(15);
			readonly Timer move = new Timer { Interval = 20 }, life = new Timer { Interval = 60 * 60 * 1000 };
			Rectangle closeRect, openRect;
			bool hoverClose, hoverOpen;
			static readonly Color Paper = Color.FromArgb(255, 244, 186), Fold = Color.FromArgb(236, 214, 130), Ink = Color.FromArgb(60, 50, 30);
			const int W = 280, Pad = 14;

			public NoteWindow(string emoji, string title, string text, Image image, string link)
			{
				this.emoji = emoji; this.title = title; this.text = text ?? ""; this.image = image; this.link = link;
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; DoubleBuffered = true; TopMost = true;
				BackColor = Paper;
				Text = "";
				int textH = TextRenderer.MeasureText(this.text, fText, new Size(W - Pad * 2, 2000), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
				int h = Pad + 26 + (image != null ? image.Height + 10 : 0) + (this.text.Length > 0 ? textH + 8 : 0) + (link != null ? 34 : 0) + Pad;
				Size = new Size(W, h);
				closeRect = new Rectangle(W - 30, 8, 22, 22);
				openRect = link != null ? new Rectangle(Pad, h - Pad - 28, 110, 26) : Rectangle.Empty;
				life.Tick += (s, e) => Close();
				life.Start();
				FormClosed += (s, e) => { move.Stop(); move.Dispose(); life.Stop(); life.Dispose(); image?.Dispose(); };
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get { var cp = base.CreateParams; cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008; cp.ClassStyle |= 0x00020000; return cp; }   // tool, no activate, topmost, drop shadow
			}

			/// <summary>From the side of the screen to beside the pet (in about a second), then along with it for a few steps.</summary>
			public void Carry(FormPet pet)
			{
				var area = pet != null ? Screen.FromControl(pet).WorkingArea : Screen.PrimaryScreen.WorkingArea;
				Point Target()
				{
					if (pet == null || pet.IsDisposed) return new Point(area.Right - Width - 20, area.Bottom - Height - 20);
					bool left = pet.Left - Width - 4 >= area.Left;
					int x = left ? pet.Left - Width + 6 : pet.Right - 6;
					int y = Math.Max(area.Top, Math.Min(area.Bottom - Height, pet.Bottom - Height));
					return new Point(Math.Max(area.Left, Math.Min(area.Right - Width, x)), y);
				}
				var to = Target();
				bool fromLeft = to.X + Width / 2 < area.Left + area.Width / 2;
				Location = new Point(fromLeft ? area.Left - Width : area.Right, to.Y);
				Show();
				var start = DateTime.Now;
				var from = Location;
				move.Tick += (s, e) =>
				{
					double t = (DateTime.Now - start).TotalSeconds;
					var aim = Target();
					if (t < 1.1)
					{
						double k = 1 - Math.Pow(1 - t / 1.1, 3);   // slows down at the end
						Location = new Point((int)(from.X + (aim.X - from.X) * k), (int)(from.Y + (aim.Y - from.Y) * k));
					}
					else if (t < 4.5) Location = aim;   // carried along
					else move.Stop();                    // put down
				};
				move.Start();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Paper);
				// a folded corner and a border, like a paper note
				using (var b = new SolidBrush(Fold)) g.FillPolygon(b, new[] { new Point(Width - 18, Height), new Point(Width, Height - 18), new Point(Width, Height) });
				using (var p = new Pen(Fold)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
				TextRenderer.DrawText(g, emoji, fEmoji, new Rectangle(Pad - 2, Pad - 6, 30, 28), Ink, TextFormatFlags.NoPrefix);
				TextRenderer.DrawText(g, title, fTitle, new Rectangle(Pad + 28, Pad - 2, W - Pad * 2 - 56, 22), Ink, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
				TextRenderer.DrawText(g, "✕", fText, closeRect, hoverClose ? Color.FromArgb(200, 40, 40) : Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				int y = Pad + 26;
				if (image != null) { g.DrawImage(image, (Width - image.Width) / 2, y, image.Width, image.Height); y += image.Height + 10; }
				if (text.Length > 0)
				{
					var r = new Rectangle(Pad, y, W - Pad * 2, Height - y);
					TextRenderer.DrawText(g, text, fText, r, Ink, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
				}
				if (link != null)
				{
					using (var b = new SolidBrush(hoverOpen ? Fold : Color.FromArgb(246, 228, 150))) g.FillRectangle(b, openRect);
					TextRenderer.DrawText(g, Lang.T("note.open"), fText, openRect, Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
				}
			}

			[DllImport("user32.dll")] static extern bool ReleaseCapture();
			[DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				bool c = closeRect.Contains(e.Location), o = openRect.Contains(e.Location);
				Cursor = c || o ? Cursors.Hand : Cursors.SizeAll;
				if (c != hoverClose || o != hoverOpen) { hoverClose = c; hoverOpen = o; Invalidate(); }
			}

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				if (e.Button != MouseButtons.Left) return;
				if (closeRect.Contains(e.Location)) { Close(); return; }
				if (openRect.Contains(e.Location))
				{
					try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true }); }
					catch (Exception skipped) { LogFile.Ignored(skipped); }
					return;
				}
				move.Stop();   // taken by the mouse: no longer carried
				ReleaseCapture();
				SendMessage(Handle, 0xA1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
			}
		}
	}
}
