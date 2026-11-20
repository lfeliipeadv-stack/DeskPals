using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// The small chat balloon above the head of the pet (the message button beside the pet): a field to write and send,
	/// a button that opens the whole conversation, and a close button. The pet answers in its speech bubble.
	/// </summary>
	internal sealed class QuickChat : PetDialog
	{
		static QuickChat open;
		public static bool IsOpen => open != null && !open.IsDisposed && open.Visible;
		/// <summary>Self-test: the balloon on screen.</summary>
		internal static QuickChat Current => IsOpen ? open : null;
		/// <summary>Self-test: where the tip is, from the left of the balloon.</summary>
		internal int TestTipX => (int)Math.Round(tipX);

		/// <summary>Open the balloon above the pet (with voice, it listens right away and sends what it hears).</summary>
		public static void Open(bool voice = false)
		{
			if (!IsOpen) { open = new QuickChat(); open.Show(); }
			open.Activate();
			open.input.Focus();
			if (voice) open.Listen();
		}

		System.Threading.CancellationTokenSource listening;
		bool micOpen;   // the recognizer is ready: talk now
		/// <summary>Self-test: listening now.</summary>
		internal bool TestListening => listening != null;

		/// <summary>Listen (the AI or the Windows recognizer, no window opens), put what it heard in the field and send it. In the dictation mode, Win+H types into the field.</summary>
		internal async void Listen()
		{
			if (listening != null) { listening.Cancel(); return; }   // a second click stops
			if (VoiceInput.Current == VoiceInput.Mode.Dictation)
			{
				// the Windows dictation types into the field; Enter sends
				keepOpenUntil = DateTime.Now.AddSeconds(3);
				input.PlaceholderText = Lang.T("voice.dictationHint");
				input.Focus();
				FormAsk.VoiceTyping();
				return;
			}
			listening = new System.Threading.CancellationTokenSource();
			micOpen = false;
			keepOpenUntil = DateTime.Now.AddMinutes(1);
			string placeholder = input.PlaceholderText;
			input.PlaceholderText = Lang.T("voice.starting");
			input.Text = "";
			Invalidate();
			var (outcome, text) = await VoiceInput.Listen(
				() => { micOpen = true; if (!IsDisposed) { input.PlaceholderText = Lang.T("voice.listening"); Invalidate(); } },
				() => { micOpen = false; if (!IsDisposed) { input.PlaceholderText = Lang.T("voice.writing"); Invalidate(); } },
				listening.Token);
			listening.Dispose(); listening = null; micOpen = false;
			keepOpenUntil = DateTime.MinValue;
			if (IsDisposed) return;
			input.PlaceholderText = placeholder;
			Invalidate();
			switch (outcome)
			{
				case VoiceInput.Outcome.Text: input.Text = text; Send(); break;
				case VoiceInput.Outcome.Nothing: input.PlaceholderText = Lang.T("voice.nothing"); break;
				case VoiceInput.Outcome.Privacy:
				case VoiceInput.Outcome.Failed: Close(); VoiceInput.Explain(outcome); break;
			}
		}

		const int W = 300, CardH = 92, Tip = 9;
		readonly TextBox input = new TextBox { BorderStyle = BorderStyle.None, MaxLength = 1000 };
		readonly Font fontTitle = Theme.CreateFont(14, FontStyle.Bold), fontText = Theme.CreateFont(13.5f), fontEmoji = Theme.CreateFont(15);
		readonly Rectangle expandRect, closeRect, inputRect, sendRect;
		readonly ToolTip tips = new ToolTip();
		readonly Timer follow = new Timer { Interval = 100 };   // stays over the pet if it moves
		string hover;
		DateTime keepOpenUntil = DateTime.MinValue;
		bool below;   // placed under the pet (no room above): no tip
		float tipX;   // where the tip is, so it points at the pet also when the window is pushed in from the screen edge

		QuickChat()
		{
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			TopMost = true;
			KeyPreview = true;
			DoubleBuffered = true;
			BackColor = Theme.Surface;
			ClientSize = new Size(W, CardH + Tip);
			Text = Lang.T("chat.quick");
			closeRect = new Rectangle(W - 38, 10, 26, 26);
			expandRect = new Rectangle(W - 70, 10, 26, 26);
			inputRect = new Rectangle(12, 44, W - 12 - 12 - 46, 38);
			sendRect = new Rectangle(W - 12 - 38, 44, 38, 38);
			input.Font = fontText;
			input.BackColor = Theme.Surface;
			input.ForeColor = Theme.Text;
			input.PlaceholderText = Lang.T("chat.placeholder", Program.Mainthread?.CurrentHeader?.Petname ?? "Pet");
			input.Location = new Point(inputRect.X + 12, inputRect.Y + (inputRect.Height - input.PreferredHeight) / 2);
			input.Width = inputRect.Width - 24;
			input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Send(); } };
			Controls.Add(input);
			Place();
			Shape();
			follow.Tick += (s, e) => { var was = (Location, below, tipX); Place(); if ((Location, below, tipX) != was) { Shape(); Invalidate(); } };
			follow.Start();
		}

		protected override CreateParams CreateParams
		{
			get { var cp = base.CreateParams; cp.ExStyle |= 0x00000080; return cp; }   // not in Alt+Tab
		}

		/// <summary>The window's shape: the card, with the tip at the pet (no tip when the balloon is under the pet).</summary>
		void Shape() => Region = new Region(below ? Theme.RoundedRect(new RectangleF(0, 0, W, CardH), 16) : ChatIcons.Balloon(W, CardH, Tip, 16, tipX));

		/// <summary>Over the head of the pet, its tip pointing at the pet.</summary>
		void Place()
		{
			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow && p.Opacity > 0);
			var wa = pet != null ? Screen.FromControl(pet).WorkingArea : Screen.PrimaryScreen.WorkingArea;
			int x = pet != null ? pet.Left + pet.Width / 2 - Width / 2 : wa.Right - Width - 24;
			int y = pet != null ? pet.Top - Height + 6 : wa.Bottom - Height - 24;
			below = pet != null && y < wa.Top;   // no room above: below the pet, without the tip
			if (below) y = pet.Bottom + 10;
			Location = new Point(Math.Max(wa.Left + 8, Math.Min(wa.Right - Width - 8, x)), Math.Max(wa.Top + 8, Math.Min(wa.Bottom - Height - 8, y)));
			tipX = Math.Max(Tip + 24, Math.Min(Width - Tip - 24, pet != null ? pet.Left + pet.Width / 2f - Left : Width / 2f));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			g.Clear(Theme.Surface);
			Theme.DrawText(g, "✨", fontEmoji, new Rectangle(12, 10, 24, 26), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			Theme.DrawText(g, Lang.T("chat.quick"), fontTitle, new Rectangle(40, 10, expandRect.X - 46, 26), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			foreach (var (r, id) in new[] { (expandRect, "expand"), (closeRect, "close") })
			{
				Theme.FillRounded(g, hover == id ? Theme.Hover : Theme.Surface, r, 7);
				Theme.DrawRounded(g, Theme.Border, r, 7);
				var ico = RectangleF.Inflate(r, -7, -7);
				if (id == "expand") ChatIcons.Message(g, ico, Theme.Text); else ChatIcons.Close(g, RectangleF.Inflate(ico, -1, -1), Theme.Text);
			}
			Theme.FillRounded(g, Theme.Surface, inputRect, 12);
			Theme.DrawRounded(g, input.Focused ? Theme.Accent : Theme.Border, inputRect, 12, input.Focused ? 2f : 1f);
			// the button: send when there is text; otherwise the microphone (green while it listens)
			bool canSend = input.Text.Trim().Length > 0 && listening == null;
			var icon = RectangleF.Inflate(sendRect, -11, -11);
			if (canSend)
			{
				Theme.FillRounded(g, Theme.Accent, sendRect, 11);
				ChatIcons.Send(g, icon, Theme.AccentText);
			}
			else if (listening != null)
			{
				Theme.FillRounded(g, micOpen ? ChatIcons.Green : Theme.SurfaceAlt, sendRect, 11);
				ChatIcons.Mic(g, icon, micOpen ? Color.White : ChatIcons.Green);
			}
			else
			{
				Theme.FillRounded(g, hover == "send" ? Theme.Hover : Theme.SurfaceAlt, sendRect, 11);
				ChatIcons.Mic(g, icon, ChatIcons.Green);
			}
			ChatIcons.DrawBalloonOutline(g, W, CardH, below ? 0 : Tip, 16, Theme.Border, Theme.Surface, tipX);
		}

		/// <summary>The button beside the field: send with text, otherwise listen (or stop listening).</summary>
		void SendOrListen()
		{
			if (listening == null && input.Text.Trim().Length > 0) Send();
			else Listen();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			string h = expandRect.Contains(e.Location) ? "expand" : closeRect.Contains(e.Location) ? "close" : sendRect.Contains(e.Location) ? "send" : null;
			Cursor = h != null ? Cursors.Hand : Cursors.Default;
			if (h == hover) return;
			hover = h;
			string sendTip = listening != null ? Lang.T("voice.stop") : input.Text.Trim().Length > 0 ? Lang.T("chat.send") : VoiceInput.Tip;
			tips.SetToolTip(this, h == "expand" ? Lang.T("chat.openFull") : h == "close" ? Lang.T("chat.close") : h == "send" ? sendTip : null);
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; Invalidate(); }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Right && sendRect.Contains(e.Location) && listening == null && input.Text.Trim().Length == 0)
			{
				// the microphone's right button: the AI or the Windows dictation (the balloon stays open meanwhile)
				keepOpenUntil = DateTime.Now.AddSeconds(30);
				VoiceInput.ShowModeMenu(PointToScreen(e.Location));
				return;
			}
			if (e.Button != MouseButtons.Left) return;
			if (closeRect.Contains(e.Location)) Close();
			else if (expandRect.Contains(e.Location)) Expand();
			else if (sendRect.Contains(e.Location)) SendOrListen();
			else if (inputRect.Contains(e.Location)) input.Focus();
		}

		/// <summary>The whole conversation, with what was written here.</summary>
		internal void Expand()
		{
			string text = input.Text;
			keepOpenUntil = DateTime.Now.AddSeconds(2);   // not closed by the deactivation while the conversation opens
			FormChat.Open(text);
			Close();
		}

		internal void Send(string text = null)
		{
			text = (text ?? input.Text).Trim();
			if (text.Length == 0) return;
			Close();
			FormChat.AskInBubble(text);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Escape) { e.Handled = true; Close(); }
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			input.TextChanged += (s, a) => Invalidate(sendRect);
			input.GotFocus += (s, a) => Invalidate(inputRect);
			input.LostFocus += (s, a) => Invalidate(inputRect);
		}

		/// <summary>A click elsewhere closes it (not while the Windows dictation is typing into it).</summary>
		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			if (DateTime.Now < keepOpenUntil) return;
			BeginInvoke(new Action(() => { if (!IsDisposed && Form.ActiveForm != this) Close(); }));
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			listening?.Cancel();   // closed while listening: the microphone stops
			follow.Stop();
			if (open == this) open = null;
			base.OnFormClosed(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { follow.Dispose(); fontTitle.Dispose(); fontText.Dispose(); fontEmoji.Dispose(); tips.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
