using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DeskPalsTools
{
	/// <summary>An error of the assistant's service, already in words for the user (the HTTP status kept for the tests).</summary>
	internal sealed class AiException : InvalidOperationException
	{
		public int Status { get; }
		public AiException(string message, int status = 0) : base(message) { Status = status; }
	}

	/// <summary>
	/// The AI service DeskPals already uses (Settings › Assistant): an OpenAI-compatible service, its address, model and
	/// key. The key is kept by DeskPals encrypted for this Windows user (DPAPI), so only this user on this computer can
	/// read it; it is never written, logged or shown here.
	/// </summary>
	internal static class AiService
	{
		/// <summary>What DeskPals saved.</summary>
		public sealed record Config(string Provider, string BaseUrl, string Model, string Key);

		/// <summary>The tests set this, so they never read the user's real settings nor their key.</summary>
		public static Config TestOverride;

		/// <summary>The same extra secret DeskPals mixes into the encryption of the key.</summary>
		static readonly byte[] Entropy = Encoding.UTF8.GetBytes("desktopPet.ai");

		public static string Protect(string key) => string.IsNullOrEmpty(key) ? "" : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser));

		/// <summary>The key from DeskPals' "AiKey" ("" when there is none or it cannot be read: another user, another PC).</summary>
		public static string Unprotect(string blob)
		{
			if (string.IsNullOrEmpty(blob)) return "";
			try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(blob), Entropy, DataProtectionScope.CurrentUser)); }
			catch (Exception) { return ""; }
		}

		public static Config Current() => TestOverride ?? new Config(
			Settings.DeskPals("AiProvider") ?? "", Settings.DeskPals("AiBaseUrl") ?? "", Settings.DeskPals("AiModel") ?? "", Unprotect(Settings.DeskPals("AiKey")));

		static bool IsLocal(Uri u) => u.IsLoopback || u.Host == "localhost";

		/// <summary>The services that cannot work without a key (the others: on this computer, or "another service").</summary>
		static bool NeedsKey(Config c) => c.Provider is "gemini" or "groq" or "openrouter" || HostIs(c, "api.openai.com");

		static bool HostIs(Config c, string host) => Uri.TryCreate(c.BaseUrl, UriKind.Absolute, out var u) && (u.Host.Equals(host, StringComparison.OrdinalIgnoreCase) || u.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase));

		/// <summary>The settings, checked: set up, with a key when the service needs one, and a safe address.</summary>
		public static Config Ready(bool needModel)
		{
			var c = Current();
			if (c == null || string.IsNullOrWhiteSpace(c.BaseUrl) || !Uri.TryCreate(c.BaseUrl.Trim(), UriKind.Absolute, out var u)
				|| (needModel && string.IsNullOrWhiteSpace(c.Model)) || (NeedsKey(c) && string.IsNullOrEmpty(c.Key)))
				throw new AiException(T.S("ai.notSet"));
			// the key never travels unencrypted over the internet
			if (!IsLocal(u) && u.Scheme != Uri.UriSchemeHttps) throw new AiException(T.S("ai.httpsOnly"));
			return c with { BaseUrl = c.BaseUrl.Trim().TrimEnd('/') };
		}

		/// <summary>The speech-to-text model of the service, or a message saying Groq (free) can do it.</summary>
		public static string TranscriptionModel(Config c)
		{
			if (c.Provider == "groq" || HostIs(c, "groq.com")) return "whisper-large-v3-turbo";
			if (HostIs(c, "api.openai.com")) return "whisper-1";
			throw new AiException(T.S("ai.noTranscribe"));
		}

		/// <summary>"pt-BR" → "pt": the language the speech is in (the program's).</summary>
		public static string Language2 => T.Code.Split('-')[0].ToLowerInvariant();

		/// <summary>The services are slow with long audio and long documents: a generous wait, but never forever.</summary>
		public static TimeSpan Timeout = TimeSpan.FromMinutes(5);

		static HttpClient Client(Config c)
		{
			var http = new HttpClient { Timeout = Timeout };
			http.DefaultRequestHeaders.UserAgent.ParseAdd("DeskPalsTools/1.0");
			if (!string.IsNullOrEmpty(c.Key)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", c.Key);
			return http;
		}

		/// <summary>The types the speech services take, by extension (an Opus voice note is an Ogg file).</summary>
		static string AudioType(string ext) => ext switch
		{
			".mp3" or ".mpga" or ".mpeg" => "audio/mpeg",
			".ogg" or ".opus" or ".oga" => "audio/ogg",
			".wav" => "audio/wav",
			".m4a" => "audio/mp4",
			".mp4" => "video/mp4",
			".webm" => "audio/webm",
			".flac" => "audio/flac",
			_ => "application/octet-stream",
		};

		/// <summary>
		/// The text spoken in one audio file below 20 MB: a piece made by Windows, or the user's own file sent as it is.
		/// Only the extension of the name is sent, never the name of the user's file.
		/// </summary>
		public static async Task<string> Transcribe(Config c, string audio, CancellationToken cancel, Action<string> waiting = null)
			=> Str(Parse(await TranscribeRaw(c, audio, "json", cancel, waiting))?["text"]).Trim();
		
		/// <summary>One stretch of speech: from, to (seconds in the piece) and what was said.</summary>
		public sealed record Segment(double Start, double End, string Text);
		
		/// <summary>The speech with its times (for subtitles); a service that gives no times gives one stretch for all.</summary>
		public static async Task<List<Segment>> TranscribeTimed(Config c, string audio, CancellationToken cancel, Action<string> waiting = null)
		{
			var json = Parse(await TranscribeRaw(c, audio, "verbose_json", cancel, waiting));
			var list = new List<Segment>();
			if (json?["segments"] is JsonArray segs)
				foreach (var seg in segs)
				{
					string text = Str(seg?["text"]).Trim();
					if (text.Length == 0) continue;
					double from = (double?)seg?["start"] ?? 0, to = (double?)seg?["end"] ?? from;
					list.Add(new Segment(from, Math.Max(from, to), text));
				}
			string all = Str(json?["text"]).Trim();
			if (list.Count == 0 && all.Length > 0) list.Add(new Segment(0, (double?)json?["duration"] ?? 5, all));
			return list;
		}
		
		static async Task<string> TranscribeRaw(Config c, string audio, string format, CancellationToken cancel, Action<string> waiting)
		{
			string model = TranscriptionModel(c);
			string ext = Jobs.Ext(audio);
			// ".opus" is not in every service's list, but it is an Ogg file, which they all take
			string sendExt = ext is ".opus" or ".oga" ? ".ogg" : ext.Length > 0 ? ext : ".mp3";
			byte[] bytes = await File.ReadAllBytesAsync(audio, cancel);
			using var http = Client(c);
			HttpContent Form()
			{
				var form = new MultipartFormDataContent();
				var file = new ByteArrayContent(bytes);
				file.Headers.ContentType = new MediaTypeHeaderValue(AudioType(ext));
				form.Add(file, "file", "audio" + sendExt);
				form.Add(new StringContent(model), "model");
				form.Add(new StringContent(format), "response_format");
				form.Add(new StringContent("0"), "temperature");
				form.Add(new StringContent(Language2), "language");
				return form;
			}
			return await Send(http, c.BaseUrl + "/audio/transcriptions", Form, cancel, audio: true, waiting);
		}

		/// <summary>Models that think before answering, and spend the answer's room on it unless told to think little.</summary>
		static bool Thinks(string model) => model?.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase) == true;

		/// <summary>
		/// The answer of the chat model to these instructions and this text. A "thinking" model can spend all the room
		/// before writing: an answer cut off is asked again with more room, and one still cut ends with "…" so it is never
		/// taken for a complete one.
		/// </summary>
		public static async Task<string> Chat(Config c, string system, string user, CancellationToken cancel, Action<string> waiting = null, int maxTokens = 1200)
		{
			using var http = Client(c);
			for (int attempt = 0; ; attempt++)
			{
				var request = new JsonObject
				{
					["model"] = c.Model,
					["temperature"] = 0.3,
					["max_tokens"] = maxTokens,
					["messages"] = new JsonArray(
						new JsonObject { ["role"] = "system", ["content"] = system },
						new JsonObject { ["role"] = "user", ["content"] = user }),
				};
				if (Thinks(c.Model)) request["reasoning_effort"] = "low";
				string json = request.ToJsonString();
				string body = await Send(http, c.BaseUrl + "/chat/completions", () => new StringContent(json, Encoding.UTF8, "application/json"), cancel, audio: false, waiting);
				var choice = Parse(body)?["choices"]?[0];
				string text = NoThinking(Str(choice?["message"]?["content"]));
				if (Str(choice?["finish_reason"]) != "length") return text;
				if (attempt == 0) { maxTokens = Math.Max(maxTokens * 3, 4000); continue; }
				return text.Length > 0 ? text + " …" : "";
			}
		}

		static string Str(JsonNode n) => n?.GetValueKind() == JsonValueKind.String ? n.GetValue<string>() : "";

		/// <summary>Some models write their thinking into the answer between &lt;think&gt; marks (DeskPals removes it too).</summary>
		static string NoThinking(string s)
		{
			s = Regex.Replace(s ?? "", @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase);
			int open = s.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);   // cut off while still thinking
			return (open >= 0 ? s[..open] : s).Trim();
		}

		static JsonNode Parse(string body)
		{
			try { return JsonNode.Parse(body); }
			catch (JsonException) { throw new AiException(T.S("ai.serviceError", "?")); }
		}

		/// <summary>How many times a request refused for the limit is tried again, and the longest wait in between (the tests shorten it).</summary>
		const int Retries = 6;
		public static TimeSpan MaxWait = TimeSpan.FromSeconds(65);

		/// <summary>
		/// Sends, and turns every problem into a message for the user (the key, the limit, the internet…). The free plans
		/// allow few words a minute: when the service says "too many", it is waited for (as long as it says) and tried again.
		/// </summary>
		static async Task<string> Send(HttpClient http, string url, Func<HttpContent> content, CancellationToken cancel, bool audio, Action<string> waiting)
		{
			for (int attempt = 0; ; attempt++)
			{
				HttpResponseMessage resp;
				using var body = content();
				try { resp = await http.PostAsync(url, body, cancel); }
				catch (OperationCanceledException) when (cancel.IsCancellationRequested) { throw; }
				catch (TaskCanceledException) { throw new AiException(T.S("ai.network")); }   // the wait ran out
				catch (HttpRequestException) { throw new AiException(T.S("ai.network")); }
				using (resp)
				{
					string text = await resp.Content.ReadAsStringAsync(cancel);
					if (resp.IsSuccessStatusCode) return text;
					int status = (int)resp.StatusCode;
					// a limit of the day ("come back in an hour") is not waited for: said at once
					if (status == 429 && attempt < Retries && RetryWait(resp, attempt) is TimeSpan wait)
					{
						waiting?.Invoke(T.S("ai.waiting"));
						await Task.Delay(wait, cancel);
						continue;
					}
					throw new AiException(status switch
					{
						401 or 403 => T.S("ai.badKey"),
						429 => T.S("ai.limit"),
						413 => T.S("ai.tooBig"),
						404 when audio => T.S("ai.noTranscribe"),
						_ => T.S("ai.serviceError", status),
					}, status);
				}
			}
		}

		/// <summary>
		/// How long to wait after "too many": what the service says (Retry-After, or Groq's reset times), else longer each
		/// time; null when the service says to come back much later (a limit per day), not worth waiting for.
		/// </summary>
		internal static TimeSpan? RetryWait(HttpResponseMessage resp, int attempt)
		{
			TimeSpan? said = resp.Headers.RetryAfter?.Delta;
			if (said == null && resp.Headers.RetryAfter?.Date is DateTimeOffset when) said = when - DateTimeOffset.UtcNow;
			foreach (var name in new[] { "x-ratelimit-reset-tokens", "x-ratelimit-reset-requests" })
				if (said == null && resp.Headers.TryGetValues(name, out var values)) said = Duration(values.FirstOrDefault());
			if (said > TimeSpan.FromMinutes(5)) return null;
			var wait = (said ?? TimeSpan.FromSeconds(5 * Math.Pow(2, attempt))) + TimeSpan.FromMilliseconds(500);
			return wait > MaxWait ? MaxWait : wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
		}

		/// <summary>Groq's "2m59.56s", "7.66s" or "120ms" as a time (null when it is not one).</summary>
		internal static TimeSpan? Duration(string s)
		{
			var parts = Regex.Matches(s ?? "", @"([\d.]+)(ms|h|m|s)");
			if (parts.Count == 0) return null;
			double seconds = 0;
			foreach (Match m in parts)
			{
				if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return null;
				seconds += m.Groups[2].Value switch { "h" => v * 3600, "m" => v * 60, "ms" => v / 1000, _ => v };
			}
			return TimeSpan.FromSeconds(seconds);
		}
	}
}
