using System.Drawing;
using DesktopPet;

namespace DeskPalsTools
{
	/// <summary>
	/// A small window while a job runs ("Converting to PDF… · Contract.docx", a bar, Cancel), in DeskPals' theme. It closes
	/// by itself at the end; <see cref="Result"/> has what was done.
	/// </summary>
	internal sealed class ProgressForm : Form
	{
		readonly string job;
		readonly List<string> files;
		readonly CancellationTokenSource cancel = new();
		readonly Font fTitle = Theme.CreateFont(15, FontStyle.Bold), fText = Theme.CreateFont(12.5f);
		readonly Button stop;
		double share;
		string doing = "";

		public Jobs.Result Result { get; private set; }

		readonly Dictionary<string, string> given;

		public ProgressForm(string job, List<string> files, Dictionary<string, string> given = null)
		{
			this.job = job; this.files = files; this.given = given;
			Text = T.S("app");
			FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; MinimizeBox = true;
			StartPosition = FormStartPosition.CenterScreen; TopMost = true; ShowInTaskbar = true;
			AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			ClientSize = new Size(440, 150);
			DoubleBuffered = true;
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);
			BackColor = Theme.Background;
			stop = new Button { Text = T.S("cancel"), Size = new Size(110, 32), Location = new Point(ClientSize.Width - 126, ClientSize.Height - 46), FlatStyle = FlatStyle.Flat, Font = fText, BackColor = Theme.Surface, ForeColor = Theme.Text };
			stop.FlatAppearance.BorderColor = Theme.Border;
			stop.Click += (s, e) => { stop.Enabled = false; cancel.Cancel(); };
			Controls.Add(stop);
			ButtonLook.Hook(this);
			Shown += async (s, e) =>
			{
				// a job that needs answers (pages, a password, a text…) asks them first
				var def = Jobs.Find(job);
				Dictionary<string, string> options = given;
				if (def?.Ask != null && options == null)
				{
					// the progress window stays out of sight while the questions (or the viewer) are open
					Opacity = 0;
					try { options = def.Ask(this, files); } finally { Opacity = 1; }
					if (options == null) { Result = new Jobs.Result { ok = false, message = T.S("err.canceled") }; Close(); return; }
				}
				Result = await Jobs.Run(job, files, (p, d) => BeginInvoke(() => { share = p; if (!string.IsNullOrEmpty(d)) doing = d; Invalidate(); }), cancel.Token, options);
				Close();
			};
			FormClosing += (s, e) => { if (Result == null) cancel.Cancel(); };
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			g.Clear(Theme.Background);
			Theme.DrawText(g, Jobs.DoingOf(job), fTitle, new Rectangle(18, 16, ClientSize.Width - 36, 26), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
			Theme.DrawText(g, doing, fText, new Rectangle(18, 44, ClientSize.Width - 36, 22), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.PathEllipsis);
			var bar = new Rectangle(18, 76, ClientSize.Width - 36, 8);
			Theme.FillRounded(g, Theme.SurfaceAlt, bar, 4);
			if (share > 0) Theme.FillRounded(g, Theme.Accent, new Rectangle(bar.X, bar.Y, Math.Max(8, (int)(bar.Width * Math.Min(1, share))), bar.Height), 4);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { fTitle.Dispose(); fText.Dispose(); cancel.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
