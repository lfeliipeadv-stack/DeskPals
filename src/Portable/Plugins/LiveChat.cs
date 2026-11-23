using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopPet
{
	// ====================================================================== live mode (Twitch chat)

	/// <summary>
	/// Off unless turned on. For streamers: the pets read the chat of a Twitch channel (only reading, as an anonymous
	/// viewer: no login, no password, nothing is ever written in the chat) and do what viewers ask with commands
	/// (!dance, !jump, !wave, !hug, !eat, !sleep, !snow, !confetti, in English, Portuguese or Spanish), and celebrate the
	/// subscriptions, gifted subs, raids and bits. Each viewer can ask again after 20 s, the pets take one command every
	/// 4 s, and the snow or the confetti at most every 3 minutes. Only the viewer's name is ever shown, never what they wrote.
	/// </summary>
	public static class LiveChat
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("LiveChat", false);
			set { Program.MyData.SetSetting("LiveChat", value); Apply(); }
		}

		/// <summary>The channel ("" = not chosen), as Twitch writes it in the address (lower case).</summary>
		public static string Channel
		{
			get => CleanChannel(Program.MyData.GetSetting("LiveChannel"));
			set { Program.MyData.SetSetting("LiveChannel", CleanChannel(value)); if (Enabled) { Stop(); Apply(); } }
		}

		/// <summary>"https://www.twitch.tv/SomeOne" or "#someone" → "someone"; "" when it cannot be a channel name.</summary>
		public static string CleanChannel(string text)
		{
			string t = (text ?? "").Trim();
			int slash = t.LastIndexOf('/');
			if (slash >= 0) t = t.Substring(slash + 1);
			t = t.TrimStart('#', '@').Split('?')[0].ToLowerInvariant();
			return Regex.IsMatch(t, "^[a-z0-9_]{3,25}$") ? t : "";
		}

		public static bool Commands { get => Program.MyData.GetSettingBool("LiveCommands", true); set => Program.MyData.SetSetting("LiveCommands", value); }
		public static bool Alerts { get => Program.MyData.GetSettingBool("LiveAlerts", true); set => Program.MyData.SetSetting("LiveAlerts", value); }
		public static bool ShowNames { get => Program.MyData.GetSettingBool("LiveNames", true); set => Program.MyData.SetSetting("LiveNames", value); }

		public enum State { Off, Connecting, Connected }
		public static State Now { get; private set; } = State.Off;

		static CancellationTokenSource cancel;
		static SynchronizationContext ui;

		public static void Apply()
		{
			ui ??= SynchronizationContext.Current;
			if (Enabled && Channel.Length > 0) { if (cancel == null) { cancel = new CancellationTokenSource(); _ = Task.Run(() => Loop(Channel, cancel.Token)); } }
			else Stop();
		}

		static void Stop()
		{
			cancel?.Cancel();
			cancel = null;
			Now = State.Off;
		}

		/// <summary>Connected, and again after any drop (waiting a little longer each time, at most a minute).</summary>
		static async Task Loop(string channel, CancellationToken ct)
		{
			int wait = 5;
			while (!ct.IsCancellationRequested)
			{
				Now = State.Connecting;
				try
				{
					using (var tcp = new TcpClient())
					{
						await tcp.ConnectAsync("irc.chat.twitch.tv", 6697, ct);
						using (var ssl = new SslStream(tcp.GetStream()))
						{
							await ssl.AuthenticateAsClientAsync("irc.chat.twitch.tv");
							var reader = new StreamReader(ssl, Encoding.UTF8);
							var writer = new StreamWriter(ssl, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
							// an anonymous viewer: Twitch lets "justinfan" + digits read any public chat without logging in
							await writer.WriteLineAsync("CAP REQ :twitch.tv/tags twitch.tv/commands");
							await writer.WriteLineAsync("PASS SCHMOOPIIE");
							await writer.WriteLineAsync("NICK justinfan" + new Random().Next(10000, 99999));
							await writer.WriteLineAsync("JOIN #" + channel);
							while (!ct.IsCancellationRequested)
							{
								string line = await reader.ReadLineAsync(ct);
								if (line == null) break;
								if (line.StartsWith("PING", StringComparison.Ordinal)) { await writer.WriteLineAsync("PONG" + line.Substring(4)); continue; }
								if (line.Contains(" JOIN #" + channel)) { Now = State.Connected; wait = 5; }
								var m = Parse(line);
								if (m != null) ui?.Post(_ => Handle(m), null);
							}
						}
					}
				}
				catch (Exception ex) when (!ct.IsCancellationRequested) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "live chat: " + ex.Message); }
				catch (Exception) { return; }
				Now = State.Connecting;
				try { await Task.Delay(TimeSpan.FromSeconds(wait), ct); } catch (Exception) { return; }
				wait = Math.Min(60, wait * 2);
			}
		}

		// ------------------------------------------------------------------ what comes from the chat

		/// <summary>A message of the chat that matters: a command, a subscription, a raid or bits.</summary>
		public sealed class Message
		{
			public string Kind;     // "command", "sub", "gift", "raid", "bits"
			public string User;     // the viewer's name (letters, digits and _ only)
			public string Command;  // the command, in English ("dance", "jump"…)
			public int Amount;      // bits, raiders, gifted subs
		}

		/// <summary>The commands, as viewers write them (English, Portuguese, Spanish) → the command.</summary>
		static readonly Dictionary<string, string> Words = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["dance"] = "dance", ["dancar"] = "dance", ["dançar"] = "dance", ["danca"] = "dance", ["bailar"] = "dance", ["baila"] = "dance",
			["jump"] = "jump", ["pular"] = "jump", ["pula"] = "jump", ["saltar"] = "jump", ["salta"] = "jump",
			["wave"] = "wave", ["hi"] = "wave", ["hello"] = "wave", ["oi"] = "wave", ["ola"] = "wave", ["olá"] = "wave", ["hola"] = "wave", ["tchau"] = "wave", ["bye"] = "wave",
			["hug"] = "hug", ["abraco"] = "hug", ["abraço"] = "hug", ["abrazo"] = "hug", ["pet"] = "hug", ["carinho"] = "hug",
			["eat"] = "eat", ["food"] = "eat", ["comer"] = "eat", ["comida"] = "eat", ["feed"] = "eat",
			["sleep"] = "sleep", ["dormir"] = "sleep", ["nap"] = "sleep",
			["snow"] = "snow", ["neve"] = "snow", ["nieve"] = "snow",
			["confetti"] = "confetti", ["confete"] = "confetti", ["confeti"] = "confetti", ["party"] = "confetti", ["festa"] = "confetti", ["fiesta"] = "confetti",
		};

		/// <summary>Read one line of the chat (IRC with Twitch's tags). Null when it is nothing for the pets.</summary>
		public static Message Parse(string line)
		{
			if (string.IsNullOrEmpty(line)) return null;
			var tags = new Dictionary<string, string>();
			string rest = line;
			if (rest.StartsWith("@", StringComparison.Ordinal))
			{
				int sp = rest.IndexOf(' ');
				if (sp < 0) return null;
				foreach (var kv in rest.Substring(1, sp - 1).Split(';'))
				{
					int eq = kv.IndexOf('=');
					if (eq > 0) tags[kv.Substring(0, eq)] = kv.Substring(eq + 1);
				}
				rest = rest.Substring(sp + 1);
			}
			// ":login!login@login.tmi.twitch.tv PRIVMSG #channel :text"
			var m = Regex.Match(rest, @"^:(?<login>[A-Za-z0-9_]{1,25})!\S+ (?<cmd>PRIVMSG|USERNOTICE) #\S+(?: :(?<text>.*))?$");
			if (!m.Success)
			{
				// USERNOTICE comes from the server itself (":tmi.twitch.tv USERNOTICE #channel")
				m = Regex.Match(rest, @"^:tmi\.twitch\.tv (?<cmd>USERNOTICE) #\S+(?: :(?<text>.*))?$");
				if (!m.Success) return null;
			}
			string user = Name(tags.TryGetValue("display-name", out var dn) && dn.Length > 0 ? dn : tags.TryGetValue("login", out var lg) ? lg : m.Groups["login"].Value);
			if (user.Length == 0) return null;
			int Num(string key) => tags.TryGetValue(key, out var v) && int.TryParse(v, out int n) ? Math.Max(0, n) : 0;
			if (m.Groups["cmd"].Value == "USERNOTICE")
			{
				string id = tags.TryGetValue("msg-id", out var x) ? x : "";
				switch (id)
				{
					case "sub": case "resub": return new Message { Kind = "sub", User = user };
					case "subgift": return new Message { Kind = "gift", User = user, Amount = 1 };
					case "submysterygift": return new Message { Kind = "gift", User = user, Amount = Math.Max(1, Num("msg-param-mass-gift-count")) };
					case "raid":
						string raider = tags.TryGetValue("msg-param-displayName", out var rn) ? Name(rn) : user;
						return new Message { Kind = "raid", User = raider.Length > 0 ? raider : user, Amount = Num("msg-param-viewerCount") };
					default: return null;
				}
			}
			int bits = Num("bits");
			if (bits > 0) return new Message { Kind = "bits", User = user, Amount = bits };
			string text = m.Groups["text"].Value.Trim();
			if (!text.StartsWith("!", StringComparison.Ordinal)) return null;
			string word = text.Substring(1).Split(' ')[0];
			return Words.TryGetValue(word, out var command) ? new Message { Kind = "command", User = user, Command = command } : null;
		}

		/// <summary>A viewer's name made safe to show: letters, digits and _, at most 25 (Twitch's own escapes removed).</summary>
		static string Name(string raw)
		{
			string s = (raw ?? "").Replace("\\s", " ");
			s = new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').Take(25).ToArray());
			return s;
		}

		// ------------------------------------------------------------------ what the pets do

		static DateTime lastAny = DateTime.MinValue, lastEffect = DateTime.MinValue;
		static readonly Dictionary<string, DateTime> lastByUser = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
		/// <summary>Self-test: what the pets did last ("dance", "sub"…).</summary>
		internal static string LastDone;

		internal static void Handle(Message m)
		{
			if (!Enabled) return;
			var now = DateTime.Now;
			if (m.Kind == "command")
			{
				if (!Commands || (now - lastAny).TotalSeconds < 4) return;
				if (lastByUser.TryGetValue(m.User, out var t) && (now - t).TotalSeconds < 20) return;
				bool effect = m.Command == "snow" || m.Command == "confetti";
				if (effect && (now - lastEffect).TotalMinutes < 3) return;
				lastAny = now; lastByUser[m.User] = now;
				if (lastByUser.Count > 500) lastByUser.Clear();
				switch (m.Command)
				{
					case "dance": Program.Mainthread?.ReactAll("dance", "party", "happy"); break;
					case "jump": Program.Mainthread?.ReactAll("jump", "happy", "cheer"); break;
					case "wave": Program.Mainthread?.ReactAll("wave", "hello", "happy"); break;
					case "hug": Program.Mainthread?.ReactAll("hug", "love", "hearts", "happy"); break;
					case "eat": Program.Mainthread?.ReactAll("eat", "cookie", "drink", "happy"); break;
					case "sleep": Program.Mainthread?.ReactAll("sleep", "nap", "yawn"); break;
					case "snow": lastEffect = now; ScreenEffects.Start(ScreenEffects.Kind.Snow, 30, true); break;
					case "confetti": lastEffect = now; ScreenEffects.Start(ScreenEffects.Kind.Confetti, 15, true); break;
				}
				LastDone = m.Command;
				if (ShowNames) SpeechBubble.Notify(Lang.T("live.asked", m.User), 3);
				return;
			}
			if (!Alerts) return;
			LastDone = m.Kind;
			Program.Mainthread?.ReactAll("party", "cheer", "dance", "happy", "jump");
			switch (m.Kind)
			{
				case "sub": SpeechBubble.Notify(Lang.T("live.sub", m.User), 8); break;
				case "gift": SpeechBubble.Notify(Lang.T("live.gift", m.User, m.Amount), 8); break;
				case "raid": SpeechBubble.Notify(Lang.T("live.raid", m.User, m.Amount), 10); ScreenEffects.Start(ScreenEffects.Kind.Confetti, 15, true); return;
				case "bits": SpeechBubble.Notify(Lang.T("live.bits", m.User, m.Amount), 8); break;
			}
			if (m.Kind == "gift" || (m.Kind == "bits" && m.Amount >= 100)) ScreenEffects.Start(ScreenEffects.Kind.Confetti, 10, true);
		}
	}
}
