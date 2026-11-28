using System.Drawing;
using System.Drawing.Drawing2D;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// The signature: drawn with the mouse (or a pen, or a finger) on a white strip, or a picture of it (a photo or a
	/// scan of the signature on paper). It is kept for next time, see-through around the ink.
	/// </summary>
	internal sealed class SignatureForm : Form
	{
		readonly Panel pad;
		readonly List<List<PointF>> strokes = new();
		readonly Font fText = Theme.CreateFont(13), fSmall = Theme.CreateFont(11.5f), fBold = Theme.CreateFont(13, FontStyle.Bold);
		readonly Button save;
		string picture;

		/// <summary>The kept signature's file, or null when the window was closed.</summary>
		public static string Ask(IWin32Window owner)
		{
			using var f = new SignatureForm();
			return f.ShowDialog(owner) == DialogResult.OK ? Marks.Signature : null;
		}

		SignatureForm()
		{
			Text = T.S("marks.signTitle");
			AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			ClientSize = new Size(600, 330);
			FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);
			BackColor = Theme.Background; ForeColor = Theme.Text;
			Padding = new Padding(16, 12, 16, 12);

			var intro = new Label { Text = T.S("marks.signIntro"), Font = fText, ForeColor = Theme.TextMuted, Dock = DockStyle.Top, Height = 44 };
			pad = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.White, Cursor = Cursors.Cross };
			pad.Paint += PaintPad;
			pad.MouseDown += (s, e) => { if (e.Button != MouseButtons.Left) return; picture = null; strokes.Add(new List<PointF> { e.Location }); pad.Invalidate(); Buttons(); };
			pad.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left && strokes.Count > 0) { strokes[^1].Add(e.Location); pad.Invalidate(); } };
			var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Background, Padding = new Padding(0, 10, 0, 0), WrapContents = false };
			var cancel = Button(T.S("cancel"), false);
			cancel.DialogResult = DialogResult.Cancel;
			save = Button("💾  " + T.S("marks.signSave"), true);
			save.Click += (s, e) => Save();
			var clear = Button("🧽  " + T.S("marks.signClear"), false);
			clear.Click += (s, e) => { strokes.Clear(); picture = null; pad.Invalidate(); Buttons(); };
			var fromFile = Button("🖼️  " + T.S("marks.signPicture"), false);
			fromFile.Click += (s, e) => FromFile();
			bottom.Controls.Add(cancel); bottom.Controls.Add(save); bottom.Controls.Add(clear); bottom.Controls.Add(fromFile);
			CancelButton = cancel;
			Controls.Add(pad);
			Controls.Add(bottom);
			Controls.Add(intro);
			ButtonLook.Hook(this);
			Buttons();
		}

		sealed class DoubleBufferedPanel : Panel { public DoubleBufferedPanel() { DoubleBuffered = true; } }

		Button Button(string text, bool primary)
		{
			var b = new Button { Text = text, AutoSize = true, Font = primary ? fBold : fText, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(8, 3, 8, 3), Cursor = Cursors.Hand,
				BackColor = primary ? Theme.Accent : Theme.Surface, ForeColor = primary ? Theme.AccentText : Theme.Text };
			b.FlatAppearance.BorderSize = primary ? 0 : 1;
			b.FlatAppearance.BorderColor = Theme.Border;
			return b;
		}

		void Buttons() => save.Enabled = strokes.Any(s => s.Count > 1) || picture != null;

		void PaintPad(object sender, PaintEventArgs e)
		{
			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			// the line to sign on
			using (var line = new Pen(Color.FromArgb(200, 200, 200), 1) { DashStyle = DashStyle.Dash })
				g.DrawLine(line, 24, pad.Height * 0.72f, pad.Width - 24, pad.Height * 0.72f);
			if (picture != null)
			{
				using var img = Image.FromFile(picture);
				float k = Math.Min((pad.Width - 20f) / img.Width, (pad.Height - 20f) / img.Height);
				g.DrawImage(img, (pad.Width - img.Width * k) / 2, (pad.Height - img.Height * k) / 2, img.Width * k, img.Height * k);
				return;
			}
			if (strokes.Count == 0)
				TextRenderer.DrawText(g, "✍️  " + T.S("marks.signHere"), fSmall, pad.ClientRectangle, Color.Gray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			using var pen = new Pen(Color.FromArgb(20, 30, 80), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
			foreach (var s in strokes)
			{
				if (s.Count == 1) g.FillEllipse(Brushes.Black, s[0].X - 1.5f, s[0].Y - 1.5f, 3, 3);
				else g.DrawCurve(pen, s.ToArray(), 0.4f);
			}
		}

		void FromFile()
		{
			using var open = new OpenFileDialog { Filter = T.S("marks.signFilter") + "|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.heic", Title = T.S("marks.signPicture") };
			if (open.ShowDialog(this) != DialogResult.OK) return;
			picture = open.FileName;
			strokes.Clear();
			pad.Invalidate();
			Buttons();
		}

		void Save()
		{
			try
			{
				if (picture != null)
				{
					using var img = Image.FromFile(picture);
					using var bmp = new Bitmap(img);
					Marks.KeepSignature(bmp);
				}
				else
				{
					// drawn twice as big as on the screen, so it stays sharp on paper
					int k = 2;
					using var bmp = new Bitmap(pad.Width * k, pad.Height * k);
					using (var g = Graphics.FromImage(bmp))
					{
						g.Clear(Color.White);
						g.SmoothingMode = SmoothingMode.AntiAlias;
						g.ScaleTransform(k, k);
						using var pen = new Pen(Color.FromArgb(20, 30, 80), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
						foreach (var s in strokes) if (s.Count > 1) g.DrawCurve(pen, s.ToArray(), 0.4f);
					}
					Marks.KeepSignature(bmp);
				}
				DialogResult = DialogResult.OK;
			}
			catch (Exception ex) { MessageBox.Show(this, Jobs.Friendly(ex), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { fText.Dispose(); fSmall.Dispose(); fBold.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
