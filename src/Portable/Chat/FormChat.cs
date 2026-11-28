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
	/// The chat box above the pet: the conversation in balloons, suggestions to start, a text field, voice (Windows
	/// dictation) and send. Some questions are answered here without the AI (the weather, the next meeting, a reminder
	/// written as a sentence, what the pet can do, how the computer is); the rest goes to the AI when it is set up.
	/// The conversation stays while the program is open ("new conversation" clears it).
	/// </summary>
	internal sealed class FormChat : PetDialog
	{
		public sealed class Message
		{
			public bool User;
			public string Text;
			/// <summary>A button under the message (e.g. "Set up the AI"), optional.</summary>
			public string ActionText; public Action Action;
		}

		static FormChat open;
		static readonly List<Message> messages = new List<Message>();

		/// <summary>Self-test: the conversation.</summary>
		internal static IReadOnlyList<Message> Messages => messages;

		public static bool IsOpen => open != null && !open.IsDisposed && open.Visible;

		/// <summary>The global shortcut: opens the chat, or closes it when it is already in front.</summary>
		internal static void Toggle()
		{
			if (IsOpen && Form.ActiveForm == open) { open.Close(); return; }
			Open();
		}
		/// <summary>Open the chat above the pet (or bring it back), optionally with a text already written.</summary>
		public static void Open(string text = null)
		{
			if (!IsOpen) { open = new FormChat(); open.Show(); }
			open.Activate();
			if (!string.IsNullOrWhiteSpace(text)) { open.input.Text = text; open.input.SelectionStart = text.Length; }
			open.input.Focus();
		}

		/// <summary>
		/// A message sent from the small balloon: the pet thinks and answers in its speech bubble (with "Reply" and
		/// "Open the conversation"); question and answer also go into the conversation.
		/// </summary>
		internal static async void AskInBubble(string text)
		{
			text = (text ?? "").Trim();
			if (text.Length == 0) return;
			messages.Add(new Message { User = true, Text = text }); Achievements.Count("chat");
			Program.Mainthread?.ReactAll("think", "review", "idea", "read");
			SpeechBubble.Say(Lang.T("ask.thinking"), 0);
			Message reply;
			try { reply = await Answer(text); }
			catch (Exception ex) { reply = new Message { Text = Lang.T("ask.error", ex.Message) }; }
			messages.Add(reply);
			Program.Mainthread?.ReactAll("happy", "wave", "idea", "talk");
			var choices = new List<SpeechBubble.Choice>();
			if (reply.ActionText != null && reply.Action != null)
			{
				var r = reply;
				choices.Add(new SpeechBubble.Choice(r.ActionText, () => { r.Action(); r.ActionText = null; }, true));
			}
			choices.Add(new SpeechBubble.Choice(Lang.T("chat.reply"), () => QuickChat.Open(), choices.Count == 0));
			choices.Add(new SpeechBubble.Choice(Lang.T("chat.openFull"), () => Open()));
			SpeechBubble.Say(reply.Text, 30, choices.ToArray());
		}

		readonly TextBox input = new TextBox { BorderStyle = BorderStyle.None, MaxLength = 1000 };
		readonly MessageList list;
		readonly Font fontTitle = Theme.CreateFont(15, FontStyle.Bold), fontSmall = Theme.CreateFont(12), fontText = Theme.CreateFont(14), fontEmoji = Theme.CreateFont(18), fontPill = Theme.CreateFont(12.5f, FontStyle.Bold);
		Rectangle closeRect, clearRect, sendRect, talkRect, inputRect;
		string hover;
		bool busy;
		DateTime keepOpenUntil = DateTime.MinValue;
		bool below;   // placed under the pet (no room above): no tip
		float tipX;   // where the tip is, so it points at the pet also when the window is pushed in from the screen edge
		const int W = 400, H = 500, Header = 56, Footer = 68, Tip = 9;

		FormChat()
		{
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			TopMost = true;
			KeyPreview = true;
			DoubleBuffered = true;
			BackColor = Theme.Surface;
			ClientSize = new Size(W, H + Tip);
			Text = Lang.T("chat.title");

			list = new MessageList(this) { Location = new Point(1, Header), Size = new Size(W - 2, H - Header - Footer) };
			Controls.Add(list);
			inputRect = new Rectangle(14, H - Footer + 13, W - 28 - 52, 42);
			sendRect = new Rectangle(W - 14 - 42, H - Footer + 13, 42, 42);
			input.Font = fontText;
			input.BackColor = Theme.Surface;
			input.ForeColor = Theme.Text;
			input.PlaceholderText = Lang.T("chat.placeholder", PetName);
			input.Location = new Point(inputRect.X + 14, inputRect.Y + (inputRect.Height - input.PreferredHeight) / 2);
			input.Width = inputRect.Width - 28;
			input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Send(input.Text); } };
			Controls.Add(input);
			closeRect = new Rectangle(W - 42, 14, 28, 28);
			using (var g = CreateGraphics())
			{
				int tw = Theme.MeasureText(g, Lang.T("ask.voice"), fontPill, int.MaxValue, TextFormatFlags.SingleLine).Width + 44;
				talkRect = new Rectangle(closeRect.X - 8 - tw, 14, tw, 28);
			}
			clearRect = new Rectangle(talkRect.X - 34, 14, 28, 28);
			Place();
			Region = new Region(below ? Theme.RoundedRect(new RectangleF(0, 0, W, H), 18) : ChatIcons.Balloon(W, H, Tip, 18, tipX));
		}

		static string PetName => Program.Mainthread?.CurrentHeader?.Petname ?? "Pet";

		protected override bool ShowWithoutActivation => false;

		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				cp.ExStyle |= 0x00000080;     // WS_EX_TOOLWINDOW: not in Alt+Tab
				return cp;
			}
		}

		[DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			int round = 2;   // DWMWCP_ROUND: rounded corners on Windows 11
			try { DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int)); } catch (Exception skipped) { LogFile.Ignored(skipped); }
		}

		/// <summary>Above the pet, inside the screen (below it when there is no room above).</summary>
		void Place()
		{
			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow && p.Opacity > 0);
			var wa = pet != null ? Screen.FromControl(pet).WorkingArea : Screen.PrimaryScreen.WorkingArea;
			int x = pet != null ? pet.Left + pet.Width / 2 - Width / 2 : wa.Right - Width - 24;
			int y = pet != null ? pet.Top - Height + 4 : wa.Bottom - Height - 24;   // the tip just over the pet's head
			below = pet != null && y < wa.Top;   // no room above: below the pet, without the tip
			if (below) y = pet.Bottom + 10;
			Location = new Point(Math.Max(wa.Left + 8, Math.Min(wa.Right - Width - 8, x)), Math.Max(wa.Top + 8, Math.Min(wa.Bottom - Height - 8, y)));
			tipX = Math.Max(Tip + 24, Math.Min(Width - Tip - 24, pet != null ? pet.Left + pet.Width / 2f - Left : Width / 2f));
		}

		// ------------------------------------------------------------------ drawing the frame

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			Theme.Prepare(g);
			g.Clear(Theme.Surface);
			// header: ✨ pet name · state pill · 🎤 Talk · (clear) · ✕
			Theme.DrawText(g, "✨", fontEmoji, new Rectangle(12, 13, 30, 30), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
			int right = (messages.Count > 0 ? clearRect.X : talkRect.X) - 8;
			int nameW = Math.Min(Theme.MeasureText(g, PetName, fontTitle, int.MaxValue, TextFormatFlags.SingleLine).Width, right - 46 - 90);
			Theme.DrawText(g, PetName, fontTitle, new Rectangle(46, 14, nameW, 28), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			string state = busy ? Lang.T("chat.thinking") : AiAssistant.Configured ? Lang.T("chat.ready") : Lang.T("chat.readyNoAi");
			var dot = busy ? Color.Orange : AiAssistant.Configured ? Color.FromArgb(46, 160, 90) : Theme.TextMuted;
			int sw = Math.Min(Theme.MeasureText(g, state, fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 30, right - (46 + nameW + 8));
			var pill = new Rectangle(46 + nameW + 8, 16, Math.Max(0, sw), 24);
			if (pill.Width > 30)
			{
				Theme.FillRounded(g, Theme.SurfaceAlt, pill, 12);
				Theme.DrawRounded(g, Theme.Border, pill, 12);
				using (var b = new SolidBrush(dot)) g.FillEllipse(b, pill.X + 10, pill.Y + 8, 8, 8);
				Theme.DrawText(g, state, fontSmall, new Rectangle(pill.X + 22, pill.Y, pill.Width - 26, pill.Height), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			}
			// 🎤 Talk: the AI or the Windows recognizer listens, or the Windows dictation types (green while the microphone is open; a click stops)
			var talkFill = listening != null && micOpen ? ChatIcons.Green : hover == "talk" ? Theme.Hover : Theme.Surface;
			var talkInk = listening != null && micOpen ? Color.White : Theme.Text;
			Theme.FillRounded(g, talkFill, talkRect, 14);
			Theme.DrawRounded(g, listening != null ? ChatIcons.Green : Theme.Border, talkRect, 14);
			ChatIcons.Mic(g, new RectangleF(talkRect.X + 12, talkRect.Y + 6, 16, 16), talkInk);
			Theme.DrawText(g, Lang.T(listening != null ? "voice.short" : "ask.voice"), fontPill, new Rectangle(talkRect.X + 32, talkRect.Y, talkRect.Width - 34, talkRect.Height), talkInk, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			if (messages.Count > 0)
			{
				if (hover == "clear") Theme.FillRounded(g, Theme.Hover, clearRect, 8);
				ChatIcons.NewChat(g, Rectangle.Inflate(clearRect, -7, -7), Theme.TextMuted);
			}
			if (hover == "close") Theme.FillRounded(g, Theme.Hover, closeRect, 8);
			ChatIcons.Close(g, Rectangle.Inflate(closeRect, -8, -8), Theme.TextMuted);
			using (var p = new Pen(Theme.Border)) { g.DrawLine(p, 0, Header - 1, W, Header - 1); g.DrawLine(p, 0, H - Footer, W, H - Footer); }
			// input field and send
			Theme.FillRounded(g, Theme.Surface, inputRect, 14);
			Theme.DrawRounded(g, input.Focused ? Theme.Accent : Theme.Border, inputRect, 14, input.Focused ? 2f : 1f);
			bool canSend = input.Text.Trim().Length > 0 && !busy;
			Theme.FillRounded(g, canSend ? Theme.Accent : (hover == "send" ? Theme.Hover : Theme.SurfaceAlt), sendRect, 12);
			ChatIcons.Send(g, Rectangle.Inflate(sendRect, -12, -12), canSend ? Theme.AccentText : Theme.TextMuted);
			// outline of the balloon, with its tip towards the pet
			ChatIcons.DrawBalloonOutline(g, W, H, below ? 0 : Tip, 18, Theme.Border, Theme.Surface, tipX);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			string h = closeRect.Contains(e.Location) ? "close" : messages.Count > 0 && clearRect.Contains(e.Location) ? "clear" : talkRect.Contains(e.Location) ? "talk" : sendRect.Contains(e.Location) ? "send" : null;
			Cursor = h != null ? Cursors.Hand : e.Y < Header ? Cursors.SizeAll : Cursors.Default;
			if (h != hover) { hover = h; Invalidate(); }
		}

		[DllImport("user32.dll")] static extern bool ReleaseCapture();
		[DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Right && talkRect.Contains(e.Location) && listening == null)
			{
				// the microphone's right button: the AI or the Windows dictation (the chat stays open meanwhile)
				keepOpenUntil = DateTime.Now.AddSeconds(30);
				VoiceInput.ShowModeMenu(PointToScreen(e.Location));
				return;
			}
			if (e.Button != MouseButtons.Left) return;
			if (closeRect.Contains(e.Location)) { Close(); return; }
			if (messages.Count > 0 && clearRect.Contains(e.Location)) { NewConversation(); return; }
			if (talkRect.Contains(e.Location)) { Listen(); return; }
			if (sendRect.Contains(e.Location)) { Send(input.Text); return; }
			if (inputRect.Contains(e.Location)) { input.Focus(); return; }
			if (e.Y < Header) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }   // drag the window by its header
		}

		protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; Invalidate(); }

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
			list.ScrollToEnd();
		}

		/// <summary>A click outside closes it (not while the Windows dictation is typing, nor while the pet thinks).</summary>
		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			if (busy || DateTime.Now < keepOpenUntil) return;
			BeginInvoke(new Action(() => { if (!IsDisposed && Form.ActiveForm != this) Close(); }));
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			listening?.Cancel();   // closed while listening: the microphone stops
			if (open == this) open = null;
			base.OnFormClosed(e);
		}

		System.Threading.CancellationTokenSource listening;
		bool micOpen;

		/// <summary>Listen (the AI or the Windows recognizer, no window opens) and send what it heard; a second click stops. In the dictation mode, Win+H types into the field.</summary>
		async void Listen()
		{
			if (listening != null) { listening.Cancel(); return; }
			if (busy) return;
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
				case VoiceInput.Outcome.Text: Send(text); break;
				case VoiceInput.Outcome.Nothing: input.PlaceholderText = Lang.T("voice.nothing"); break;
				case VoiceInput.Outcome.Privacy:
				case VoiceInput.Outcome.Failed: Close(); VoiceInput.Explain(outcome); break;
			}
		}

		void NewConversation()
		{
			messages.Clear();
			AiAssistant.NewConversation();
			list.Invalidate();
			Invalidate();
			input.Focus();
		}

		// ------------------------------------------------------------------ sending

		internal async void Send(string text)
		{
			text = (text ?? "").Trim();
			if (text.Length == 0 || busy) return;
			input.Text = "";
			messages.Add(new Message { User = true, Text = text }); Achievements.Count("chat");
			busy = true;
			Invalidate(); list.ScrollToEnd();
			Program.Mainthread?.ReactAll("think", "review", "idea", "read");
			Message reply;
			try { reply = await Answer(text); }
			catch (Exception ex) { reply = new Message { Text = Lang.T("ask.error", ex.Message) }; }
			busy = false;
			if (IsDisposed) { SpeechBubble.Say(reply.Text, 12); return; }   // closed meanwhile: the bubble says it
			messages.Add(reply);
			Program.Mainthread?.ReactAll("happy", "wave", "idea", "talk");
			Invalidate(); list.ScrollToEnd();
			input.Focus();
		}

		/// <summary>The reply: answered here when it can be, otherwise by the AI.</summary>
		internal static async Task<Message> Answer(string text)
		{
			var local = await LocalAnswer(text);
			if (local != null) return local;
			if (!AiAssistant.Configured)
				return new Message { Text = Lang.T("chat.needAi"), ActionText = Lang.T("ask.settings"), Action = () => FormSettings.ShowPage(FormSettings.Page.Ai) };
			if (!FormAsk.PrivacyOk()) return new Message { Text = Lang.T("chat.privacyNo") };
			string answer = await AiAssistant.Ask(text);
			return new Message { Text = answer.Length > 0 ? answer : Lang.T("ask.empty") };
		}

		const RegexOptions I = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

		/// <summary>Questions answered on this computer: reminders, weather, meetings, the computer, what the pet can do.</summary>
		internal static async Task<Message> LocalAnswer(string text)
		{
			if (SmartReminder.LooksLikeReminder(text))
			{
				// read here; only a sentence it cannot read goes to the AI (after the privacy notice)
				var r = SmartReminder.Parse(text, DateTime.Now) ?? (AiAssistant.Configured && FormAsk.PrivacyOk() ? await SmartReminder.Understand(text) : null);
				if (r == null) return new Message { Text = Lang.T("rem.smartFail") };
				Reminders.Add(r);
				return new Message { Text = Lang.T("rem.smartOk", r.Text, SmartReminder.When(r, DateTime.Now)), ActionText = Lang.T("rem.smartUndo"), Action = () => { Reminders.Remove(r); messages.Add(new Message { Text = Lang.T("rem.smartUndone") }); open?.list.Invalidate(); } };
			}
			if (Regex.IsMatch(text, @"\b(tempo|clima|chov\w*|chuva|guarda-chuva|weather|rain\w*|umbrella|tiempo|llov\w*|lluvia|paraguas)\b", I) && !Regex.IsMatch(text, @"\b(quanto tempo|how long|cu[aá]nto tiempo)\b", I))
			{
				if (Weather.City == null)
					return new Message { Text = Lang.T("weather.noCity"), ActionText = Lang.T("weather.choose"), Action = () => FormSettings.ShowPage(FormSettings.Page.Plugins) };
				var w = await Weather.Current();
				return new Message { Text = w == null ? Lang.T("weather.offline") : Weather.Summary(w, Weather.City) + " " + Lang.T("weather.nowTemp", Math.Round(w.Now)) };
			}
			if (Regex.IsMatch(text, @"\b(reuni[aãoõ]\w*|agenda|compromisso\w*|meeting\w*|calendar|reuni[oó]n\w*|cita)\b", I))
			{
				if (!Agenda.Configured)
					return new Message { Text = Lang.T("chat.noAgenda"), ActionText = Lang.T("opt.agenda.button"), Action = () => FormSettings.ShowPage(FormSettings.Page.Plugins) };
				var next = Agenda.Next;
				return new Message { Text = next == null ? Lang.T("menu.agendaNone") : Lang.T("chat.nextMeeting", next.Title, next.Start.Date == DateTime.Today ? next.Start.ToString("HH:mm") : next.Start.ToString("g")) };
			}
			// "what is my name?": the name or nickname of the first setup
			if (Regex.IsMatch(text, @"(qual (é |e )?(o )?meu nome|como (eu )?me chamo|meu apelido|what('s| is) my name|c[oó]mo me llamo|cu[aá]l es mi nombre)", I))
				return UserProfile.Name.Length > 0
					? new Message { Text = Lang.T("chat.myName", UserProfile.Name) }
					: new Message { Text = Lang.T("chat.noName"), ActionText = Lang.T("setup.open"), Action = FormSetup.Open };
			if (Regex.IsMatch(text, @"\b(teclas?|digitei|digitad[oa]s?|digitei|keys?|keystrokes?|typed|pulsad[oa]s|tecle[ié])\b", I))
				return TypingPet.Enabled || TypingPet.Today > 0
					? new Message { Text = Lang.T("chat.keys", TypingPet.Today.ToString("N0")) }
					: new Message { Text = Lang.T("chat.keysOff"), ActionText = Lang.T("chat.turnOn"), Action = () => { TypingPet.Enabled = true; } };
			if (Regex.IsMatch(text, @"(como foi (o )?meu dia|resum[oa] do (meu )?dia|how was my day|c[oó]mo (fue|ha ido) mi d[ií]a)", I))
			{
				string facts = DaySummary.Facts();
				return new Message { Text = facts.Length > 0 ? Lang.T("summary.local") + " " + facts : Lang.T("summary.nothing") };
			}
			if (Regex.IsMatch(text, @"(programas?|aplicativos?|apps?|programs?).*(hoje|today|hoy|mais usei|more|m[aá]s)|(tempo|time|tiempo).*(programas?|apps?|programs?)", I))
			{
				var apps = AppTime.Today(5);
				if (!AppTime.Enabled && apps.Count == 0)
					return new Message { Text = Lang.T("apptime.off"), ActionText = Lang.T("chat.turnOn"), Action = () => { AppTime.Enabled = true; } };
				return new Message { Text = apps.Count == 0 ? Lang.T("apptime.none") : Lang.T("apptime.today") + "\n" + string.Join("\n", apps.Select(a => "• " + a.name + ": " + Pomodoro.Minutes(a.minutes))) };
			}
			if (Regex.IsMatch(text, @"\b(cpu|mem[oó]ria|memory|ram|computador|computer|ordenador|sistema|system)\b", I))
				return new Message { Text = Lang.T("chat.system", PcMonitor.Cpu, PcMonitor.Ram, Reminders.All.Count(r => r.Enabled)) };
			if (Regex.IsMatch(text, @"(o que (voc[eê] )?sabe fazer|o que voc[eê] faz|ajuda|what can you do|help|qu[eé] puedes hacer|ayuda)", I))
				return new Message { Text = Lang.T("chat.help") };
			return null;
		}

		/// <summary>The suggestions shown while the conversation is empty.</summary>
		internal static string[] Suggestions => new[] { Lang.T("chat.sugHelp"), Lang.T("chat.sugWeather"), Lang.T("chat.sugRemind"), Lang.T("chat.sugMeeting") };

		// ------------------------------------------------------------------ the conversation

		sealed class MessageList : Control
		{
			readonly FormChat chat;
			int scroll, contentH;
			readonly List<(Rectangle rect, Action run)> hits = new List<(Rectangle, Action)>();
			Rectangle hoverRect;
			readonly Font bigEmoji = Theme.CreateFont(30);

			public MessageList(FormChat c)
			{
				chat = c;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				BackColor = Theme.Surface;
			}

			public void ScrollToEnd() { scroll = int.MaxValue; Invalidate(); }

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Background);
				hits.Clear();
				int maxW = Width * 78 / 100;
				if (messages.Count == 0 && !chat.busy) { PaintEmpty(g); return; }

				// measure everything, then draw from the scroll position
				var items = new List<(Message m, Size size)>();
				int total = 12;
				foreach (var m in messages)
				{
					var s = Theme.MeasureText(g, m.Text, chat.fontText, maxW - 24, TextFormatFlags.WordBreak);
					items.Add((m, s));
					total += s.Height + 20 + (m.ActionText != null ? 38 : 0) + 10;
				}
				if (chat.busy) total += 44;
				contentH = total;
				int max = Math.Max(0, contentH - Height);
				scroll = Math.Max(0, Math.Min(max, scroll));
				int y = 12 - scroll;
				foreach (var (m, s) in items)
				{
					int bw = Math.Min(maxW, s.Width + 24), bh = s.Height + 20;
					var r = new Rectangle(m.User ? Width - bw - 12 : 12, y, bw, bh);
					Theme.FillRounded(g, m.User ? Theme.Accent : Theme.Surface, r, 14);
					if (!m.User) Theme.DrawRounded(g, Theme.Border, r, 14);
					Theme.DrawText(g, m.Text, chat.fontText, new Rectangle(r.X + 12, r.Y + 10, bw - 24, s.Height), m.User ? Theme.AccentText : Theme.Text, TextFormatFlags.WordBreak);
					y += bh + 6;
					if (m.ActionText != null)
					{
						int aw = Theme.MeasureText(g, m.ActionText, chat.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 28;
						var a = new Rectangle(12, y, aw, 30);
						Theme.DrawButton(g, a, m.ActionText, chat.fontSmall, false, hoverRect == a);
						var msg = m;
						hits.Add((a, () => { msg.Action?.Invoke(); msg.ActionText = null; Invalidate(); }));
						y += 38;
					}
					y += 4;
				}
				if (chat.busy)
				{
					var r = new Rectangle(12, y, 70, 34);
					Theme.FillRounded(g, Theme.Surface, r, 14);
					Theme.DrawRounded(g, Theme.Border, r, 14);
					int dots = (int)(DateTime.Now.Millisecond / 333) + 1;
					Theme.DrawText(g, new string('•', dots), chat.fontText, r, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					BeginInvoke(new Action(async () => { await Task.Delay(300); if (!IsDisposed) Invalidate(); }));
				}
			}

			/// <summary>Empty: a hello and suggestions to tap.</summary>
			void PaintEmpty(Graphics g)
			{
				Theme.DrawText(g, "🐾", bigEmoji, new Rectangle(0, 34, Width, 44), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine);
				Theme.DrawText(g, UserProfile.Greet(Lang.T("chat.hello", PetName)), chat.fontTitle, new Rectangle(16, 86, Width - 32, 26), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawText(g, Lang.T(AiAssistant.Configured ? "chat.helloSub" : "chat.helloSubNoAi"), chat.fontSmall, new Rectangle(24, 116, Width - 48, 40), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
				int x = 16, y = 176;
				foreach (string sug in Suggestions)
				{
					int w = Theme.MeasureText(g, sug, chat.fontSmall, int.MaxValue, TextFormatFlags.SingleLine).Width + 26;
					if (x + w > Width - 16) { x = 16; y += 40; }
					var r = new Rectangle(x, y, w, 32);
					Theme.FillRounded(g, hoverRect == r ? Theme.Hover : Theme.Surface, r, 16);
					Theme.DrawRounded(g, Theme.Border, r, 16);
					Theme.DrawText(g, sug, chat.fontSmall, r, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
					string text = sug;
					hits.Add((r, () =>
					{
						// "Remind me to…" fills the field to complete; the others are asked at once
						if (text == Lang.T("chat.sugRemind")) { chat.input.Text = Lang.T("chat.sugRemindFill"); chat.input.Focus(); chat.input.SelectionStart = chat.input.Text.Length; }
						else chat.Send(text);
					}));
					x += w + 8;
				}
			}

			protected override void OnMouseWheel(MouseEventArgs e)
			{
				base.OnMouseWheel(e);
				scroll -= e.Delta / 120 * 50;
				Invalidate();
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				var h = hits.FirstOrDefault(x => x.rect.Contains(e.Location)).rect;
				Cursor = h.IsEmpty ? Cursors.Default : Cursors.Hand;
				if (h != hoverRect) { hoverRect = h; Invalidate(); }
			}

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				hits.FirstOrDefault(x => x.rect.Contains(e.Location)).run?.Invoke();
			}

			/// <summary>Self-test: what can be clicked.</summary>
			internal int Clickable => hits.Count;
		}

		/// <summary>Self-test: the window, its list and sending a text.</summary>
		internal int TestClickable => list.Clickable;
		internal bool TestBusy => busy;
	}
}
