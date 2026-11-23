using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// The self-test, part: chat. Check, Try and the list of tests are in SelfTest.cs.
	internal static partial class SelfTest
	{
		/// <summary>True once the mouse has not moved for <paramref name="quiet"/>; false if that does not happen within <paramref name="limit"/>.</summary>
		static async Task<bool> MouseStill(TimeSpan quiet, TimeSpan limit)
		{
			var clock = System.Diagnostics.Stopwatch.StartNew();
			var still = System.Diagnostics.Stopwatch.StartNew();
			var at = Cursor.Position;
			while (clock.Elapsed < limit)
			{
				await Task.Delay(100);
				var now = Cursor.Position;
				if (now != at) { at = now; still.Restart(); }
				else if (still.Elapsed >= quiet) return true;
			}
			return false;
		}

		/// <summary>
		/// Mouse on the pet: the message and voice buttons; the message button opens the small balloon over the head,
		/// its balloon button opens the whole conversation, and a message sent from it is answered in the pet's bubble.
		/// </summary>
		static async Task HoverChat()
		{
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			var pet = Pet();
			if (pet == null || !HoverAsk.Enabled) { lines.Add("SKIP  hover chat: no pet, or the buttons are turned off"); return; }
			// the test points at the pet with the mouse: someone using the mouse now would move it away. It waits until
			// the mouse has been still for a moment (up to 20 s), and leaves the test for later if it never is.
			if (!await MouseStill(TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(20))) { lines.Add("SKIP  hover chat: the mouse is being used (it never stood still for 1.5 s)"); return; }
			var mouse = Cursor.Position;
			for (int i = 0; i < 60 && !(pet.OnGround && Screen.FromControl(pet).WorkingArea.Contains(pet.Bounds)); i++) await Task.Delay(150);   // arriving, or in a scene below the taskbar
			FormPet.Hold(true);   // a pet walking away cannot be pointed at
			try
			{
				await Task.Delay(600);
				for (int i = 0; i < 20 && !HoverAsk.Showing; i++) { Cursor.Position = new Point(pet.Left + pet.Width / 2, pet.Top + pet.Height / 2); await Task.Delay(150); }
				Check("hover chat: message and voice buttons beside the pet", HoverAsk.Showing);
				Shot("1-botoes", new Rectangle(pet.Left - 20, pet.Top - 30, pet.Width + 90, pet.Height + 60));
				HoverAsk.TestClick(0);
				await Task.Delay(500);
				var q = QuickChat.Current;
				Check("hover chat: the message button opens the small balloon", q != null);
				if (q != null)
				{
					Check("hover chat: the balloon is over the pet's head, its tip at the pet", ((q.Bottom >= pet.Top - 2 && q.Bottom <= pet.Top + 12) || pet.Top > Screen.FromControl(pet).WorkingArea.Bottom - pet.Height / 2) && (Math.Abs(q.Left + q.TestTipX - (pet.Left + pet.Width / 2)) <= 6 || q.TestTipX <= 40 || q.TestTipX >= q.Width - 40), $"balloon {q.Bounds}, tip at {q.Left + q.TestTipX}, pet {pet.Bounds}");
					Check("hover chat: the pets wait while it is open", FormPet.Held);
					Shot("2-balao", Rectangle.Union(q.Bounds, pet.Bounds));
					q.Expand();
					await Task.Delay(600);
					Check("hover chat: its balloon button opens the whole conversation", FormChat.IsOpen && !QuickChat.IsOpen);
					var chat = Application.OpenForms.OfType<FormChat>().FirstOrDefault();
					if (chat != null) { chat.Refresh(); Shot("3-conversa", Rectangle.Union(chat.Bounds, pet.Bounds)); chat.Close(); }
					await Task.Delay(300);
				}
				// a message sent from the small balloon: the pet answers in its bubble, and it goes into the conversation
				int count = FormChat.Messages.Count;
				QuickChat.Open();
				await Task.Delay(300);
				QuickChat.Current?.Send("o que você sabe fazer?");
				bool said = false;
				for (int i = 0; i < 40 && !said; i++) { await Task.Delay(100); said = FormChat.Messages.Count >= count + 2 && Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible); }
				Check("hover chat: a message from the balloon is answered in the pet's bubble", said && !QuickChat.IsOpen && FormChat.Messages.Last().Text == Lang.T("chat.help"));
				var bubble = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible);
				if (bubble != null) Shot("4-resposta", Rectangle.Union(bubble.Bounds, pet.Bounds));
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// voice: the Windows recognizer, through a hidden Windows PowerShell (no window of its own)
				var clock = System.Diagnostics.Stopwatch.StartNew();
				var (heard, _) = await VoiceInput.ListenWindows(null, System.Threading.CancellationToken.None);
				Check("hover chat: the Windows recognizer answers", heard != VoiceInput.Outcome.Failed, $"{heard} in {clock.Elapsed.TotalSeconds:0.0} s {VoiceInput.LastError}");
				// voice for the AI: the pet's own recorder opens the microphone (stopped after a moment)
				bool noMicrophone = false;
				using (var stop = new System.Threading.CancellationTokenSource(1500))
				{
					bool opened = false;
					var (rec, _) = await VoiceRecorder.Record(() => opened = true, stop.Token);
					noMicrophone = rec == VoiceRecorder.Result.NoMicrophone;
					// a computer without a microphone (the GitHub build machine) has nothing to open
					if (rec == VoiceRecorder.Result.NoMicrophone) lines.Add("SKIP  hover chat: the pet's recorder opens the microphone: no microphone on this computer");
					else Check("hover chat: the pet's recorder opens the microphone", opened && rec == VoiceRecorder.Result.Canceled, $"{rec} {VoiceRecorder.LastError} peak {VoiceRecorder.LastPeak}");
				}
				// the microphone choice: the AI or the Windows dictation (the test listens with the AI's way)
				var mode = VoiceInput.Current;
				VoiceInput.Current = VoiceInput.Mode.Dictation;
				Check("hover chat: the microphone can be set to the Windows dictation", VoiceInput.Current == VoiceInput.Mode.Dictation && VoiceInput.Tip.Contains(Lang.T("voice.tipDictation")));
				VoiceInput.Current = VoiceInput.Mode.Windows;
				Check("hover chat: the microphone can be set to the Windows online recognizer", VoiceInput.Current == VoiceInput.Mode.Windows && VoiceInput.Tip.Contains(Lang.T("voice.tipWindows")));
				VoiceInput.Current = VoiceInput.Mode.Ai;
				QuickChat.Open(voice: true);
				VoiceInput.Current = mode;
				// without a microphone the listening ends at once (nothing to hear): nothing to check then
				bool listening = false;
				for (int i = 0; i < 15 && !listening; i++) { await Task.Delay(100); listening = QuickChat.Current?.TestListening == true; }
				if (noMicrophone) lines.Add("SKIP  hover chat: the microphone button listens in the balloon: no microphone on this computer");
				else Check("hover chat: the microphone button listens in the balloon", listening);
				await Task.Delay(1500);
				if (QuickChat.Current != null) Shot("5-ouvindo", Rectangle.Union(QuickChat.Current.Bounds, pet.Bounds));
				for (int i = 0; i < 150 && QuickChat.Current?.TestListening == true; i++) await Task.Delay(100);
				if (heard == VoiceInput.Outcome.Privacy && AiAssistant.TranscriptionModel == null)
				{
					bubble = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible);
					Check("hover chat: without the speech setting, the pet explains and offers it", bubble != null && !QuickChat.IsOpen);
					if (bubble != null) Shot("6-sem-permissao", Rectangle.Union(bubble.Bounds, pet.Bounds));
				}
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			}
			finally { FormPet.Hold(false); Cursor.Position = mouse; QuickChat.Current?.Close(); }
		}

		/// <summary>The chat above the pet: opens, holds the pets, answers here (reminder, help, meeting) and closes.</summary>
		static async Task ChatBox()
		{
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			// a pet still arriving from the sky moves away from where the chat opened: let it land first
			for (int i = 0; i < 40 && Pet() is FormPet landing && !landing.OnGround; i++) await Task.Delay(150);
			FormChat.Open();
			await Task.Delay(400);
			var chat = Application.OpenForms.OfType<FormChat>().FirstOrDefault();
			Check("chat: opens", chat != null && chat.Visible);
			if (chat == null) return;
			Check("chat: the pets wait while it is open", FormPet.Held);
			var pet = Pet();
			if (pet != null) Check("chat: next to the pet", Math.Abs((chat.Left + chat.Width / 2) - (pet.Left + pet.Width / 2)) < chat.Width && (chat.Bottom <= pet.Top + 10 || chat.Top >= pet.Bottom - 2), $"chat {chat.Bounds}, pet {pet.Bounds}");
			chat.Refresh();
			Check("chat: suggestions to tap", FormChat.Messages.Count > 0 || chat.TestClickable >= 4, chat.TestClickable.ToString());

			int before = Reminders.All.Count;
			var r1 = await FormChat.Answer("me lembra de regar as plantas às 23h");
			var added = Reminders.All.Skip(before).ToList();
			Check("chat: a reminder written as a sentence is created", added.Count == 1 && added[0].Text == "Regar as plantas" && r1.ActionText != null, r1.Text);
			foreach (var r in added) Reminders.Remove(r);
			var help = await FormChat.Answer("o que você sabe fazer?");
			Check("chat: says what it can do (no AI)", help.Text == Lang.T("chat.help"));
			var sys = await FormChat.Answer("como está a memória do computador?");
			Check("chat: the computer, answered here", sys.Text.Contains("%"), sys.Text);
			bool typingWas = TypingPet.Enabled;
			Program.MyData.SetSetting("TypingPet", true);   // on, without the keyboard hook (self-test)
			var keys = await FormChat.Answer("quantas teclas eu digitei hoje?");
			Check("chat: how many keys were typed, answered here", keys.Text == Lang.T("chat.keys", TypingPet.Today.ToString("N0")), keys.Text);
			Program.MyData.SetSetting("TypingPet", typingWas);
			var day = await FormChat.Answer("como foi meu dia?");
			Check("chat: how was my day, answered here", day.Text.StartsWith(Lang.T("summary.local")) || day.Text == Lang.T("summary.nothing"), day.Text);
			if (!AiAssistant.Configured)
			{
				var free = await FormChat.Answer("qual a capital da França?");
				Check("chat: free question without AI offers to set it up", free.Text == Lang.T("chat.needAi") && free.Action != null);
			}
			else lines.Add("SKIP  chat: free question without AI (an AI is set up here)");

			chat.Send("o que você sabe fazer?");
			for (int i = 0; i < 30 && chat.TestBusy; i++) await Task.Delay(100);
			Check("chat: the conversation shows question and answer", FormChat.Messages.Count >= 2 && FormChat.Messages[FormChat.Messages.Count - 2].User && !FormChat.Messages.Last().User);
			chat.Close();
			await Task.Delay(200);
			Check("chat: closed, pets free again", !FormChat.IsOpen && !FormPet.Held);
		}

		/// <summary>Configured AI providers: saved, switched and removed; the user's own are put back afterwards.</summary>
		static void AiProviders()
		{
			string savedList = Program.MyData.GetSetting("AiProfiles"), savedActive = AiProfiles.ActiveId;
			bool savedDone = Program.MyData.GetSettingBool("AiProfilesDone", false);
			var savedNow = AiProfiles.Snapshot();
			try
			{
				Program.MyData.SetSetting("AiProfiles", ""); AiProfiles.ActiveId = "";
				Program.MyData.SetSetting("AiProfilesDone", true);
				AiProfiles.Restore(("", "", "", ""));
				Check("ai providers: none at first", AiProfiles.All.Count == 0 && !AiAssistant.Configured);
				AiAssistant.ProviderId = "ollama"; AiAssistant.BaseUrl = "http://localhost:11434/v1"; AiAssistant.Model = "llama3.2"; AiAssistant.Key = "";
				AiProfiles.SaveCurrent(asNew: true);
				AiAssistant.ProviderId = "groq"; AiAssistant.BaseUrl = "https://api.groq.com/openai/v1"; AiAssistant.Model = "llama-3.3-70b-versatile"; AiAssistant.Key = "chave-de-teste";
				AiProfiles.SaveCurrent(asNew: true);
				var all = AiProfiles.All;
				Check("ai providers: two saved, the last in use", all.Count == 2 && AiProfiles.ActiveId == all[1].Id && AiAssistant.ProviderId == "groq");
				Check("ai providers: the key stays encrypted in the list", !Program.MyData.GetSetting("AiProfiles").Contains("chave-de-teste") && all[1].KeyBlob.Length > 0);
				AiProfiles.Use(all[0]);
				Check("ai providers: switching uses the other one", AiAssistant.ProviderId == "ollama" && AiAssistant.Model == "llama3.2" && AiAssistant.Key == "");
				AiProfiles.Use(all[1]);
				Check("ai providers: and back, with its key", AiAssistant.Key == "chave-de-teste");
				AiProfiles.Remove(all[1]);
				Check("ai providers: removing the one in use falls back to the other", AiProfiles.All.Count == 1 && AiAssistant.ProviderId == "ollama");
				AiProfiles.Remove(AiProfiles.All[0]);
				Check("ai providers: none left, nothing in use", AiProfiles.All.Count == 0 && !AiAssistant.Configured);
			}
			finally
			{
				Program.MyData.SetSetting("AiProfiles", savedList); AiProfiles.ActiveId = savedActive;
				Program.MyData.SetSetting("AiProfilesDone", savedDone);
				AiProfiles.Restore(savedNow);
			}
		}

		/// <summary>
		/// "Ask the pet" against a fake AI service on this computer that speaks the OpenAI format (like Ollama,
		/// Gemini, Groq and OpenRouter do): models, the right and the wrong key, the answer in the bubble.
		/// The real AI settings are put back afterwards.
		/// </summary>
		/// <summary>
		/// The real AI service, when its key is in the environment (DESKTOPPET_AI_KEY, DESKTOPPET_AI_PROVIDER; never in a
		/// file): a free question through the chat and through the small balloon, answered in the pet's bubble.
		/// The settings of this computer are put back afterwards.
		/// </summary>
		static async Task AiLive()
		{
			string key = Environment.GetEnvironmentVariable("DESKTOPPET_AI_KEY") ?? "";
			if (key.Length == 0) { lines.Add("SKIP  ai live: no key in DESKTOPPET_AI_KEY"); return; }
			var p = AiAssistant.Preset(Environment.GetEnvironmentVariable("DESKTOPPET_AI_PROVIDER") ?? "groq");
			string pId = AiAssistant.ProviderId, pUrl = AiAssistant.BaseUrl, pModel = AiAssistant.Model, pKey = AiAssistant.Key, privacy = Program.MyData.GetSetting("AiPrivacyOk");
			try
			{
				var models = await AiAssistant.ListModels(p.BaseUrl, key);
				string model = await AiAssistant.FindWorkingModel(p, p.BaseUrl, key, p.Model, models);
				Check("ai live: the service answers with a model", !string.IsNullOrEmpty(model), $"{p.Name}: {model}");
				AiAssistant.ProviderId = p.Id; AiAssistant.BaseUrl = p.BaseUrl; AiAssistant.Model = model; AiAssistant.Key = key;
				Program.MyData.SetSetting("AiPrivacyOk", "1");
				AiAssistant.NewConversation();
				var clock = System.Diagnostics.Stopwatch.StartNew();
				var reply = await FormChat.Answer("Qual é a capital da França? Responda só o nome.");
				Check("ai live: a free question in the chat", reply.Text.IndexOf("Paris", StringComparison.OrdinalIgnoreCase) >= 0, $"{clock.ElapsedMilliseconds} ms: {reply.Text}");
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				int count = FormChat.Messages.Count;
				FormChat.AskInBubble("Quanto é 7 vezes 8? Responda só o número.");
				bool said = false;
				for (int i = 0; i < 150 && !said; i++) { await Task.Delay(100); said = FormChat.Messages.Count >= count + 2; }
				string last = FormChat.Messages.LastOrDefault()?.Text ?? "";
				Check("ai live: from the balloon, answered in the pet's bubble", said && last.Contains("56") && Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible), last);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// the summary of the day, written by the AI from today's numbers
				AppTime.TestAdd("Programa de teste", 40 * 60);
				DaySummary.Tell();
				SpeechBubble summary = null;
				for (int i = 0; i < 150 && summary == null; i++)
				{
					await Task.Delay(100);
					summary = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible && b.TestText != Lang.T("ask.thinking"));
				}
				Check("ai live: the AI writes the summary of the day", summary != null && !summary.TestText.StartsWith(Lang.T("summary.local")), summary?.TestText);
				AppTime.TestRemove("Programa de teste");
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			}
			finally
			{
				AiAssistant.ProviderId = pId; AiAssistant.BaseUrl = pUrl; AiAssistant.Model = pModel; AiAssistant.Key = pKey;
				Program.MyData.SetSetting("AiPrivacyOk", privacy);
				AiAssistant.NewConversation();
			}
		}

		static async Task AskThePet()
		{
			var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
			listener.Start();
			int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
			string lastBody = "";
			var cts = new System.Threading.CancellationTokenSource();
			_ = Task.Run(async () =>
			{
				while (!cts.IsCancellationRequested)
				{
					System.Net.Sockets.TcpClient c;
					try { c = await listener.AcceptTcpClientAsync(cts.Token); } catch (Exception) { return; }
					using (c)
					{
						var s = c.GetStream();
						var buf = new byte[65536];
						int n = await s.ReadAsync(buf, 0, buf.Length);
						string req = Encoding.UTF8.GetString(buf, 0, n);
						int headEnd = req.IndexOf("\r\n\r\n");
						var len = System.Text.RegularExpressions.Regex.Match(req, @"Content-Length: (\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
						int want = len.Success ? int.Parse(len.Groups[1].Value) : 0;
						while (headEnd >= 0 && Encoding.UTF8.GetByteCount(req) - (headEnd + 4) < want)
						{
							int m = await s.ReadAsync(buf, 0, buf.Length);
							if (m == 0) break;
							req += Encoding.UTF8.GetString(buf, 0, m);
						}
						bool keyOk = req.Contains("Authorization: Bearer chave-certa");
						string status = "200 OK", body;
						if (!keyOk) { status = "401 Unauthorized"; body = "{\"error\":{\"message\":\"invalid key\"}}"; }
						else if (req.StartsWith("GET /v1/models")) body = "{\"data\":[{\"id\":\"modelo-a\"},{\"id\":\"llama3.2\"}]}";
						else { lastBody = req.Substring(headEnd + 4); body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"<think>hmm</think>**Oi!** Eu sou o pet de teste.\\n# Fim\"}}]}"; }
						var bytes = Encoding.UTF8.GetBytes(body);
						var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
						await s.WriteAsync(head, 0, head.Length);
						await s.WriteAsync(bytes, 0, bytes.Length);
					}
				}
			});

			string url = $"http://127.0.0.1:{port}/v1";
			// keep the real settings
			string pId = AiAssistant.ProviderId, pUrl = AiAssistant.BaseUrl, pModel = AiAssistant.Model, pKey = AiAssistant.Key;
			try
			{
				var models = await AiAssistant.ListModels(url, "chave-certa");
				Check("ai: models listed", models.Count == 2, string.Join(",", models));
				Check("ai: preferred model chosen (llama3.2 for Ollama)", AiAssistant.PickModel(AiAssistant.Preset("ollama"), "nao-existe", models) == "llama3.2");
				Check("ai: saved model kept when it exists", AiAssistant.PickModel(AiAssistant.Preset("ollama"), "modelo-a", models) == "modelo-a");

				string answer = await AiAssistant.Ask(url, "modelo-a", "chave-certa", "quem é você?", "Rex");
				Check("ai: answer cleaned for the bubble", answer == "Oi! Eu sou o pet de teste.\nFim", answer);
				var sent = System.Text.Json.Nodes.JsonNode.Parse(lastBody);
				Check("ai: the pet's name and language go in the instructions",
					sent?["messages"]?[0]?["content"]?.GetValue<string>().Contains("Rex") == true && sent?["model"]?.GetValue<string>() == "modelo-a", lastBody.Length.ToString());

				bool refused = false;
				try { await AiAssistant.Ask(url, "modelo-a", "chave-errada", "oi", "Rex"); }
				catch (InvalidOperationException ex) { refused = ex.Message == Lang.T("ai.errKey"); }
				Check("ai: a wrong key gives a clear message", refused);

				bool https = false;
				try { await AiAssistant.ListModels("http://example.com/v1", "x"); } catch (ArgumentException) { https = true; }
				Check("ai: internet services only over https", https);

				// the key is stored encrypted, never in plain text
				AiAssistant.Key = "minha-chave-secreta";
				Check("ai: key stored encrypted", AiAssistant.Key == "minha-chave-secreta" && !Program.MyData.GetSetting("AiKey").Contains("minha-chave"));

				// the whole flow: menu item, thinking, answer in the bubble
				AiAssistant.ProviderId = "custom"; AiAssistant.BaseUrl = url; AiAssistant.Model = "modelo-a"; AiAssistant.Key = "chave-certa";
				Check("ai: configured", AiAssistant.Configured);
				var pet = Pet();
				if (pet != null)
				{
					var cm = PetMenu.Show(pet, pet, new System.Drawing.Point(0, 0));
					Check("ai: 'Ask the pet' in the pet menu once set up", cm.Items.OfType<ToolStripMenuItem>().Any(i => i.Text == Lang.T("menu.ask")));
					cm.Close();
				}
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				await FormAsk.AskNow("quem é você?");
				await Task.Delay(300);
				Check("ai: the answer appears in the pet's bubble", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// the conversation: the second question carries the first one and its answer
				AiAssistant.NewConversation();
				await AiAssistant.Ask("primeira pergunta");
				await AiAssistant.Ask("e depois?");
				var conv = System.Text.Json.Nodes.JsonNode.Parse(lastBody)?["messages"]?.AsArray();
				Check("ai: remembers the conversation", conv != null && conv.Count == 4 && conv[1]?["content"]?.GetValue<string>() == "primeira pergunta", conv?.Count.ToString());
				AiAssistant.NewConversation();
				await AiAssistant.Ask("outra conversa");
				conv = System.Text.Json.Nodes.JsonNode.Parse(lastBody)?["messages"]?.AsArray();
				Check("ai: 'new conversation' forgets it", conv != null && conv.Count == 2, conv?.Count.ToString());
				AiAssistant.NewConversation();

				// hovering the pet shows the "Ask" button (like the CPU plate); leaving hides it
				if (pet != null && HoverAsk.Enabled)
				{
					var mouse = Cursor.Position;
					pet.React("walk");
					await Task.Delay(300);
					bool shown = false;
					for (int i = 0; i < 8 && !shown; i++)
					{
						Cursor.Position = new System.Drawing.Point(pet.Left + pet.Width / 2, pet.Top + pet.Height / 2);
						await Task.Delay(150);
						shown = HoverAsk.Showing;
					}
					Check("hover ask: the button appears over the pet", shown, $"pet at {pet.Bounds}, mouse at {Cursor.Position}, busy {pet.Busy}");
					Cursor.Position = new System.Drawing.Point(pet.Left - 400 < 0 ? pet.Right + 400 : pet.Left - 400, Math.Max(0, pet.Top - 300));
					await Task.Delay(1200);
					Check("hover ask: it goes away when the mouse leaves", !HoverAsk.Showing);
					Cursor.Position = mouse;
				}
			}
			finally
			{
				AiAssistant.ProviderId = pId; AiAssistant.BaseUrl = pUrl; AiAssistant.Model = pModel; AiAssistant.Key = pKey;
				cts.Cancel();
				listener.Stop();
			}
		}
	}
}
