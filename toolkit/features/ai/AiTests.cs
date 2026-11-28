using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DeskPalsTools
{
	/// <summary>
	/// Tests of "transcribe" and "summarize" against a pretend AI service on this computer (never the real one, never the
	/// user's key), and one against the real service only with DESKPALS_AI_LIVE=1.
	/// </summary>
	internal static class AiTests
	{
		[ModuleInitializer]
		internal static void Init()
		{
			DevTests.Add("ai plan", Plan);
			DevTests.Add("ai key", Key);
			DevTests.Add("ai text", Text);
			DevTests.Add("ai transcribe", Transcribe);
			DevTests.Add("ai summarize", Summarize);
			DevTests.Add("ai retry", Retry);
			DevTests.Add("ai errors", Errors);
			DevTests.Add("ai live", Live);
		}

		// ------------------------------------------------------------------ a pretend OpenAI-compatible service

		/// <summary>Answers /audio/transcriptions and /chat/completions on a free localhost port, and keeps what it got.</summary>
		sealed class FakeService : IDisposable
		{
			readonly HttpListener listener = new();
			public readonly string BaseUrl;
			/// <summary>When not 200, every request gets this status (to see the messages for 401, 429…).</summary>
			public int Status = 200;
			/// <summary>This many requests get "429 too many" before the service answers (the limit per minute of free plans).</summary>
			public int Busy;
			/// <summary>The Retry-After of those 429s (seconds), when set.</summary>
			public int? RetryAfter;
			/// <summary>This many chat answers are "cut off": all the room spent thinking, no words (finish_reason "length").</summary>
			public int CutOff;
			/// <summary>The name the file was sent under.</summary>
			public string FileName;
			public readonly List<(string path, string auth, Dictionary<string, string> fields, int fileBytes, string body)> Got = new();

			public FakeService()
			{
				var probe = new TcpListener(IPAddress.Loopback, 0);
				probe.Start();
				int port = ((IPEndPoint)probe.LocalEndpoint).Port;
				probe.Stop();
				BaseUrl = "http://localhost:" + port + "/v1";
				listener.Prefixes.Add("http://localhost:" + port + "/");
				listener.Start();
				_ = Task.Run(Loop);
			}

			async Task Loop()
			{
				while (listener.IsListening)
				{
					HttpListenerContext ctx;
					try { ctx = await listener.GetContextAsync(); } catch (Exception) { return; }
					try { Answer(ctx); } catch (Exception) { }
				}
			}

			void Answer(HttpListenerContext ctx)
			{
				using var ms = new MemoryStream();
				ctx.Request.InputStream.CopyTo(ms);
				byte[] raw = ms.ToArray();
				string latin = Encoding.Latin1.GetString(raw);
				var fields = new Dictionary<string, string>();
				foreach (Match m in Regex.Matches(latin, "name=\"?(\\w+)\"?(?:; filename=[^\\r]*)?\\r\\n(?:Content-Type:[^\\r]*\\r\\n)?\\r\\n([^\\r]*)"))
					if (m.Groups[1].Value != "file") fields[m.Groups[1].Value] = m.Groups[2].Value;
				int fileBytes = 0;
				var fileMatch = Regex.Match(latin, "name=\"?file\"?; filename=[^\\r]*\\r\\n(?:Content-Type:[^\\r]*\\r\\n)?\\r\\n");
				if (fileMatch.Success)
				{
					int start = fileMatch.Index + fileMatch.Length;
					int end = latin.IndexOf("\r\n--", start, StringComparison.Ordinal);
					fileBytes = end > start ? end - start : 0;
					FileName = Regex.Match(fileMatch.Value, "filename=\"?([^\";\\r]+)").Groups[1].Value;
					// a real MP3: "ID3" or a frame start
					if (fileBytes > 2 && !(latin.Substring(start, 3) == "ID3" || (raw[start] == 0xFF && (raw[start + 1] & 0xE0) == 0xE0))) fileBytes = -1;
				}
				string path = ctx.Request.Url.AbsolutePath;
				string body = Encoding.UTF8.GetString(raw);
				int n;
				lock (Got) { Got.Add((path, ctx.Request.Headers["Authorization"], fields, fileBytes, body)); n = Got.Count(g => g.path == path); }
				string reply;
				bool busy = false, cut = false;
				lock (Got)
				{
					if (Busy > 0) { Busy--; busy = true; }
					else if (CutOff > 0 && path.EndsWith("/chat/completions")) { CutOff--; cut = true; }
				}
				if (Status != 200 || busy)
				{
					ctx.Response.StatusCode = busy ? 429 : Status;
					if (RetryAfter != null) ctx.Response.Headers["Retry-After"] = RetryAfter.Value.ToString();
					reply = "{\"error\":{\"message\":\"no\"}}";
				}
				else if (cut) reply = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"finish_reason\":\"length\"}]}";
				else if (path.EndsWith("/audio/transcriptions")) reply = "{\"text\":\"piece" + n + "\"}";
				else if (path.EndsWith("/chat/completions"))
				{
					// the summary says how long the text was and its first word, so the tests can follow the parts
					var doc = System.Text.Json.Nodes.JsonNode.Parse(body);
					string user = (string)doc["messages"][1]["content"];
					string first = user.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
					reply = System.Text.Json.JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content = "**SUMMARY" + n + "** of " + first } } } });
				}
				else { ctx.Response.StatusCode = 404; reply = "{}"; }
				byte[] bytes = Encoding.UTF8.GetBytes(reply);
				ctx.Response.ContentType = "application/json";
				ctx.Response.OutputStream.Write(bytes);
				ctx.Response.Close();
			}

			public AiService.Config Config(string provider = "groq", string key = "test-key-123") => new(provider, BaseUrl, "fake-model", key);

			public void Dispose() { try { listener.Stop(); listener.Close(); } catch (Exception) { } }
		}

		/// <summary>Runs with the pretend service set, and always puts things back.</summary>
		static async Task With(AiService.Config cfg, Func<Task> work)
		{
			var before = AiService.TestOverride;
			var maxWait = AiService.MaxWait;
			AiService.TestOverride = cfg ?? new AiService.Config("", "", "", "");
			// the waits after "too many" are made tiny: the tests check that they happen, not how long they last
			AiService.MaxWait = TimeSpan.FromMilliseconds(50);
			try { await work(); }
			finally { AiService.TestOverride = before; AiService.MaxWait = maxWait; AiAudio.ChunkSecondsForTests = null; AiAudio.FailForTests = false; AiJobs.ChunkChars = 12000; }
		}

		// ------------------------------------------------------------------ the tests

		static Task Plan(DevTests.Ctx t)
		{
			string[] Ids(string name)
			{
				string f = Path.Combine(t.Dir, name);
				File.WriteAllText(f, "content");
				return Jobs.Plan(new[] { f }).Select(j => j.Id).ToArray();
			}
			string empty = Path.Combine(t.Dir, "empty.pdf");
			File.WriteAllText(empty, "");
			t.Check("ai plan: an empty file is not offered", !Jobs.Plan(new[] { empty }).Any(j => j.Id is "summarize" or "transcribe"));
			var ogg = Ids("voice.ogg");
			t.Check("ai plan: an audio → transcribe first, then summarize, before MP3", ogg.Take(2).SequenceEqual(new[] { "transcribe", "summarize" }) && Array.IndexOf(ogg, "tomp3") > 1, string.Join(",", ogg));
			t.Check("ai plan: a video → subtitles and transcribe first", Ids("clip.mp4").Take(2).SequenceEqual(new[] { "subtitles", "transcribe" }), string.Join(",", Ids("clip.mp4")));
			var pdf = Ids("x.pdf");
			t.Check("ai plan: a PDF → read, then summarize", Array.IndexOf(pdf, "summarize") == Array.IndexOf(pdf, "ocr") + 1 && Array.IndexOf(pdf, "ocr") >= 0, string.Join(",", pdf));
			t.Check("ai plan: a Word document and a text can be summarized", Ids("x.docx").Contains("summarize") && Ids("notes.md").Contains("summarize"));
			t.Check("ai plan: a picture is not summarized nor transcribed", !Ids("a.png").Any(j => j is "summarize" or "transcribe"));
			return Task.CompletedTask;
		}

		static Task Key(DevTests.Ctx t)
		{
			// the same encryption as DeskPals: only this Windows user can read it back
			string blob = AiService.Protect("minha-chave-secreta");
			t.Check("ai key: encrypted, then read back", AiService.Unprotect(blob) == "minha-chave-secreta" && !blob.Contains("minha"));
			t.Check("ai key: a broken or empty value is no key", AiService.Unprotect("bm90LWRwYXBp") == "" && AiService.Unprotect("") == "" && AiService.Unprotect("%%%") == "");
			// read from a DeskPals settings file of the test's own (never the user's)
			string config = Path.Combine(t.Dir, "DesktopPet.config");
			File.WriteAllText(config, "<?xml version=\"1.0\"?><configuration><appSettings>" +
				"<add key=\"AiProvider\" value=\"groq\" /><add key=\"AiBaseUrl\" value=\"https://api.groq.com/openai/v1\" />" +
				"<add key=\"AiModel\" value=\"llama-3.3-70b-versatile\" /><add key=\"AiKey\" value=\"" + blob + "\" /></appSettings></configuration>");
			string beforeConfig = Settings.DeskPalsConfig;
			var beforeOverride = AiService.TestOverride;
			try
			{
				Settings.DeskPalsConfig = config;
				AiService.TestOverride = null;
				var c = AiService.Current();
				t.Check("ai key: DeskPals' settings read (service, address, model, key)", c.Provider == "groq" && c.BaseUrl.Contains("groq.com") && c.Model.StartsWith("llama") && c.Key == "minha-chave-secreta");
				t.Check("ai key: Groq transcribes with Whisper", AiService.TranscriptionModel(c) == "whisper-large-v3-turbo");
				t.Check("ai key: OpenAI transcribes with whisper-1", AiService.TranscriptionModel(new("custom", "https://api.openai.com/v1", "gpt", "k")) == "whisper-1");
				bool says = false;
				try { AiService.TranscriptionModel(new("gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "g", "k")); }
				catch (AiException ex) { says = ex.Message == T.S("ai.noTranscribe"); }
				t.Check("ai key: another service → \"Groq (free) can\"", says);
			}
			finally { Settings.DeskPalsConfig = beforeConfig; AiService.TestOverride = beforeOverride; }
			return Task.CompletedTask;
		}

		/// <summary>A PDF with real text (not pictures), written with PDFsharp.</summary>
		static string TextPdf(string path, params string[] pages)
		{
			using var doc = new PdfDocument();
			foreach (var words in pages)
			{
				var page = doc.AddPage();
				using var g = XGraphics.FromPdfPage(page);
				g.DrawString(words, new XFont("Arial", 14), XBrushes.Black, new XPoint(40, 80));
			}
			doc.Save(path);
			return path;
		}

		/// <summary>The smallest .docx Word opens: two paragraphs, one with a tab.</summary>
		static string Docx(string path, string first, string second) =>
			RawDocx(path, "<w:p><w:r><w:t>" + first + "</w:t></w:r></w:p><w:p><w:r><w:t xml:space=\"preserve\">Item </w:t></w:r><w:r><w:tab/><w:t>" + second + "</w:t></w:r></w:p>");

		/// <summary>A .docx with this body (Word's XML, with the w: and mc: prefixes).</summary>
		static string RawDocx(string path, string body)
		{
			using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
			void Put(string name, string xml) { using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); w.Write(xml); }
			Put("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
			Put("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
			Put("word/document.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"><w:body>" +
				body + "</w:body></w:document>");
			return path;
		}

		static async Task Text(DevTests.Ctx t)
		{
			string pdf = TextPdf(Path.Combine(t.Dir, "contract.pdf"), "The rent is 1500 reais per month, paid on day 5.", "The contract lasts twelve months.");
			string text = await AiText.Read(pdf, null, default);
			t.Check("ai text: the words of a PDF", text.Contains("1500 reais") && text.Contains("twelve months"), text.Length > 120 ? text[..120] : text);
			string docx = Docx(Path.Combine(t.Dir, "letter.docx"), "Dear neighbours, the meeting is on Friday.", "Bring a chair");
			text = await AiText.Read(docx, null, default);
			t.Check("ai text: the paragraphs of a Word document", text.Contains("meeting is on Friday") && (text.Contains("Item \tBring a chair") || text.Contains("Item Bring a chair")), text);
			string md = Path.Combine(t.Dir, "notes.md");
			File.WriteAllText(md, "# Shopping\n\n\n\n\n- rice\n- beans", new UTF8Encoding(true));
			text = await AiText.Read(md, null, default);
			t.Check("ai text: a text file, empty lines tidied", text == "# Shopping\n\n- rice\n- beans", text);
			string rtf = Path.Combine(t.Dir, "memo.rtf");
			File.WriteAllText(rtf, @"{\rtf1\ansi{\fonttbl{\f0 Arial;}}\f0\fs24 Pets need water every day.\par}");
			text = await AiText.Read(rtf, null, default);
			t.Check("ai text: an RTF (without Word)", text.Contains("Pets need water every day"), text);
			// a scan (pictures only): the OCR of Windows reads it
			if (OcrWork.Languages().Length == 0) t.Skip("ai text: no OCR language in this Windows");
			else
			{
				string scan = await t.Pdf("scan.pdf", "HELLO PETS");
				text = await AiText.Read(scan, null, default);
				t.Check("ai text: a scanned PDF is read by OCR", text.Replace(" ", "").Contains("HELLOPETS", StringComparison.OrdinalIgnoreCase), text);
			}
			string broken = Path.Combine(t.Dir, "broken.docx");
			File.WriteAllText(broken, "not a zip");
			bool friendly = false;
			try { await AiText.Read(broken, null, default); } catch (InvalidOperationException ex) { friendly = ex.Message == T.S("ai.badDocument"); }
			t.Check("ai text: a broken document says so", friendly);
			// a text box: Word writes it twice (mc:Choice and mc:Fallback), each with its own paragraph inside
			string box = "<w:txbxContent><w:p><w:r><w:t>BOXTEXT</w:t></w:r></w:p></w:txbxContent>";
			string boxed = RawDocx(Path.Combine(t.Dir, "boxed.docx"), "<w:p><w:r><mc:AlternateContent><mc:Choice Requires=\"wps\">" + box + "</mc:Choice><mc:Fallback>" + box +
				"</mc:Fallback></mc:AlternateContent></w:r><w:r><w:t>Body</w:t></w:r></w:p>");
			text = await AiText.Read(boxed, null, default);
			t.Check("ai text: a text box is read once", Regex.Matches(text, "BOXTEXT").Count == 1 && text.Contains("Body"), text.Replace('\n', '|'));
			// an old Notepad file (Windows-1252), and a UTF-8 one: the accents stay
			string ansi = Path.Combine(t.Dir, "ansi.txt");
			File.WriteAllBytes(ansi, Encoding.Latin1.GetBytes("Reunião de condomínio: ação aprovada."));
			text = await AiText.Read(ansi, null, default);
			t.Check("ai text: an old Windows text keeps its accents", text == "Reunião de condomínio: ação aprovada.", text);
			string utf8 = Path.Combine(t.Dir, "utf8.txt");
			File.WriteAllText(utf8, "Coração e pão", new UTF8Encoding(false));
			text = await AiText.Read(utf8, null, default);
			t.Check("ai text: a UTF-8 text without a mark", text == "Coração e pão", text);
			// open in Word (Word lets others read): read anyway; held by a program that lets no one read: said in words, no path
			using (new FileStream(docx, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
			{
				string held = null;
				try { held = await AiText.Read(docx, null, default); } catch (Exception ex) { held = "error: " + ex.Message; }
				t.Check("ai text: a Word document open in Word is still read", held.Contains("meeting is on Friday"), held);
			}
			using (new FileStream(ansi, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				string msg = "";
				try { await AiText.Read(ansi, null, default); } catch (InvalidOperationException ex) { msg = ex.Message; } catch (Exception ex) { msg = "raw: " + ex.Message; }
				t.Check("ai text: a file locked by another program → a friendly message", msg == T.S("err.noAccess"), msg);
			}
			var parts = AiJobs.Split("One two three. Four five six. Seven eight nine.", 20);
			t.Check("ai text: long texts cut between sentences", parts.Count == 3 && parts[0] == "One two three." && parts.All(p => p.Length <= 20), string.Join(" | ", parts));
		}

		static async Task Transcribe(DevTests.Ctx t)
		{
			using var fake = new FakeService();
			string tone = t.Tone("voice message.wav", 150);
			await With(fake.Config(), async () =>
			{
				AiAudio.ChunkSecondsForTests = 40;
				var r = await Jobs.Run("transcribe", new[] { tone }, null);
				var sent = fake.Got.Where(g => g.path == "/v1/audio/transcriptions").ToList();
				t.Check("ai transcribe: done", r.ok, r.message);
				t.Check("ai transcribe: a long audio goes in pieces (150 s / 40 s = 4)", sent.Count == 4, sent.Count.ToString());
				t.Info("ai transcribe: piece sizes " + string.Join(", ", sent.Select(s => s.fileBytes)));
				t.Check("ai transcribe: every piece is a small MP3", sent.All(s => s.fileBytes > 1000 && s.fileBytes < 20 * 1024 * 1024), string.Join(",", sent.Select(s => s.fileBytes)));
				// 40 s at 32 kbit/s is about 160 KB: much more would mean the pieces were not cut
				t.Check("ai transcribe: the pieces are cut, not whole", sent.All(s => s.fileBytes < 260_000), string.Join(",", sent.Select(s => s.fileBytes)));
				t.Check("ai transcribe: Whisper model, the program's language, plain answer", sent.All(s => s.fields.GetValueOrDefault("model") == "whisper-large-v3-turbo"
					&& s.fields.GetValueOrDefault("language") == AiService.Language2 && s.fields.GetValueOrDefault("response_format") == "json" && s.fields.GetValueOrDefault("temperature") == "0"),
					string.Join(";", sent.FirstOrDefault().fields?.Select(kv => kv.Key + "=" + kv.Value) ?? Array.Empty<string>()));
				t.Check("ai transcribe: the key goes in the header", sent.All(s => s.auth == "Bearer test-key-123"));
				string o = r.outputs.FirstOrDefault();
				t.Check("ai transcribe: \"<name> (transcription).txt\" next to the file", o != null && Path.GetFileName(o) == "voice message (" + T.S("ai.name.transcription") + ").txt" && Path.GetDirectoryName(o) == t.Dir, o);
				t.Check("ai transcribe: the pieces joined in order", o != null && File.ReadAllText(o) == "piece1 piece2 piece3 piece4", o == null ? "" : File.ReadAllText(o));
				t.Check("ai transcribe: the start in the pet's bubble", r.preview.StartsWith("piece1"), r.preview);
				// a short audio goes whole, and nothing is overwritten
				fake.Got.Clear();
				AiAudio.ChunkSecondsForTests = null;
				string shortTone = t.Tone("short.wav", 3);
				r = await Jobs.Run("transcribe", new[] { shortTone }, null);
				var again = await Jobs.Run("transcribe", new[] { shortTone }, null);
				t.Check("ai transcribe: a short audio in one piece", r.ok && fake.Got.Count == 2, r.message + " " + fake.Got.Count);
				t.Check("ai transcribe: a second time does not overwrite", again.ok && again.outputs[0] != r.outputs[0] && File.Exists(r.outputs[0]), again.outputs.FirstOrDefault());
			});
			t.Check("ai transcribe: no temporary audio left", !Directory.EnumerateFiles(Path.GetTempPath(), "deskpals-tools-*.mp3").Any(f => File.GetLastWriteTime(f) > DateTime.Now.AddMinutes(-5)));
		}

		static async Task Summarize(DevTests.Ctx t)
		{
			using var fake = new FakeService();
			await With(fake.Config(), async () =>
			{
				string pdf = TextPdf(Path.Combine(t.Dir, "report.pdf"), "Sales grew in March because of the new shop.");
				var r = await Jobs.Run("summarize", new[] { pdf }, null);
				var chat = fake.Got.Where(g => g.path == "/v1/chat/completions").ToList();
				t.Check("ai summarize: a PDF summarized", r.ok && chat.Count == 1, r.message);
				string o = r.outputs.FirstOrDefault();
				t.Check("ai summarize: \"<name> (summary).txt\", Markdown stars removed", o != null && Path.GetFileName(o) == "report (" + T.S("ai.name.summary") + ").txt" && File.ReadAllText(o) == "SUMMARY1 of Sales", o == null ? "" : File.ReadAllText(o));
				var req = chat.Count > 0 ? System.Text.Json.Nodes.JsonNode.Parse(chat[0].body) : null;
				t.Check("ai summarize: the saved model, temperature 0.3, a limit of words", req != null && (string)req["model"] == "fake-model" && (double)req["temperature"] == 0.3 && (int)req["max_tokens"] >= 1000);
				string system = req == null ? "" : (string)req["messages"][0]["content"];
				t.Check("ai summarize: asks for a faithful summary in the program's language", system.Contains("never invent") && system.Contains(new System.Globalization.CultureInfo(T.Code).EnglishName), system);
				t.Check("ai summarize: the summary in the pet's bubble", r.preview == "SUMMARY1 of Sales", r.preview);

				// a long text: parts, then the summary of the summaries
				fake.Got.Clear();
				AiJobs.ChunkChars = 400;
				string txt = Path.Combine(t.Dir, "long.txt");
				File.WriteAllText(txt, string.Join("\n\n", Enumerable.Range(1, 6).Select(i => "Paragraph" + i + " " + string.Concat(Enumerable.Repeat("words and more words. ", 9)))));
				r = await Jobs.Run("summarize", new[] { txt }, null);
				chat = fake.Got.Where(g => g.path == "/v1/chat/completions").ToList();
				t.Check("ai summarize: a long text in parts, then all together", r.ok && chat.Count >= 3 && File.ReadAllText(r.outputs[0]).StartsWith("SUMMARY" + chat.Count), r.message + " requests=" + chat.Count);
				t.Check("ai summarize: each part within the size", chat.All(c => ((string)System.Text.Json.Nodes.JsonNode.Parse(c.body)["messages"][1]["content"]).Length <= 400));
				// far too long: said at once, nothing sent
				fake.Got.Clear();
				string huge = Path.Combine(t.Dir, "huge.txt");
				File.WriteAllText(huge, string.Concat(Enumerable.Repeat("A sentence of a very long book. ", 700)));
				r = await Jobs.Run("summarize", new[] { huge }, null);
				t.Check("ai summarize: a giant text → too big, no requests", !r.ok && r.message.EndsWith(T.S("ai.tooBig")) && fake.Got.Count == 0, r.message + " requests=" + fake.Got.Count);
				AiJobs.ChunkChars = 12000;

				// a Word document, and an audio (transcribed first)
				fake.Got.Clear();
				string docx = Docx(Path.Combine(t.Dir, "minutes.docx"), "Decided: paint the hall blue.", "Budget 300");
				r = await Jobs.Run("summarize", new[] { docx }, null);
				t.Check("ai summarize: a Word document", r.ok && File.ReadAllText(r.outputs[0]) == "SUMMARY1 of Decided:", r.message);
				fake.Got.Clear();
				string tone = t.Tone("meeting.wav", 3);
				r = await Jobs.Run("summarize", new[] { tone }, null);
				t.Check("ai summarize: an audio is transcribed, then summarized", r.ok && fake.Got.Select(g => g.path).SequenceEqual(new[] { "/v1/audio/transcriptions", "/v1/chat/completions" }) && File.ReadAllText(r.outputs[0]) == "SUMMARY1 of piece1", r.message);
				string empty = Path.Combine(t.Dir, "empty.txt");
				File.WriteAllText(empty, "   \n ");
				r = await Jobs.Run("summarize", new[] { empty }, null);
				t.Check("ai summarize: an empty file says there is no text", !r.ok && r.message.Contains(T.S("ai.noText")), r.message);
			});
		}

		/// <summary>The free plans: "too many" waited for, answers cut off while thinking, voice notes Windows cannot convert.</summary>
		static async Task Retry(DevTests.Ctx t)
		{
			using var fake = new FakeService();
			await With(fake.Config(), async () =>
			{
				// a long text whose 2nd part is refused twice for the limit: waited for, nothing lost
				AiJobs.ChunkChars = 400;
				string txt = Path.Combine(t.Dir, "long.txt");
				File.WriteAllText(txt, string.Join("\n\n", Enumerable.Range(1, 3).Select(i => "Paragraph" + i + " " + string.Concat(Enumerable.Repeat("words and more words. ", 15)))));
				fake.Got.Clear();
				var r = await Jobs.Run("summarize", new[] { txt }, null);
				int plain = fake.Got.Count;
				fake.Got.Clear();
				fake.Busy = 2;
				r = await Jobs.Run("summarize", new[] { txt }, null);
				t.Check("ai retry: \"too many\" → waits and sends again, the job ends well", r.ok && fake.Got.Count == plain + 2, r.message + " requests=" + fake.Got.Count + " (without waits " + plain + ")");
				// a limit of the day: not waited for
				fake.Got.Clear();
				fake.Busy = 100;
				fake.RetryAfter = 3600;
				r = await Jobs.Run("summarize", new[] { txt }, null);
				t.Check("ai retry: \"come back in an hour\" → the limit at once", !r.ok && r.message.EndsWith(T.S("ai.limit")) && fake.Got.Count == 1, r.message + " requests=" + fake.Got.Count);
				fake.Busy = 0;
				fake.RetryAfter = null;
				AiJobs.ChunkChars = 12000;

				// a thinking model that spent all the room: asked again with more room
				string note = Path.Combine(t.Dir, "note.txt");
				File.WriteAllText(note, "The pet eats at noon.");
				fake.Got.Clear();
				fake.CutOff = 1;
				r = await Jobs.Run("summarize", new[] { note }, null);
				var chat = fake.Got.Where(g => g.path == "/v1/chat/completions").ToList();
				int Room(int i) => (int)System.Text.Json.Nodes.JsonNode.Parse(chat[i].body)["max_tokens"];
				t.Check("ai retry: an answer cut off is asked again with more room", r.ok && chat.Count == 2 && Room(1) > Room(0) && File.ReadAllText(r.outputs[0]).StartsWith("SUMMARY2"), r.message + " requests=" + chat.Count);
				fake.CutOff = 2;
				r = await Jobs.Run("summarize", new[] { note }, null);
				t.Check("ai retry: still empty → an error, no empty summary saved", !r.ok && r.outputs.Count == 0, r.message);
				fake.CutOff = 0;

				// Windows cannot convert it (Ogg Opus on many PCs): the voice note goes as it is
				AiAudio.FailForTests = true;
				fake.Got.Clear();
				string opus = Path.Combine(t.Dir, "PTT-20261127-WA0001.opus");
				File.WriteAllBytes(opus, Encoding.ASCII.GetBytes("OggS" + new string('x', 5000)));
				r = await Jobs.Run("transcribe", new[] { opus }, null);
				t.Check("ai retry: a voice note Windows cannot convert is sent as it is", r.ok && fake.Got.Count == 1 && fake.Got[0].fileBytes == -1 && fake.FileName == "audio.ogg", r.message + " name=" + fake.FileName);
				t.Check("ai retry: the voice note itself is kept", File.Exists(opus) && new FileInfo(opus).Length == 5004);
				string wma = Path.Combine(t.Dir, "old.wma");
				File.WriteAllBytes(wma, new byte[3000]);
				r = await Jobs.Run("transcribe", new[] { wma }, null);
				t.Check("ai retry: a format the service does not take → said in words", !r.ok && r.message.EndsWith(T.S("ai.cantHear")), r.message);
			});
		}

		static async Task Errors(DevTests.Ctx t)
		{
			using var fake = new FakeService();
			string txt = Path.Combine(t.Dir, "note.txt");
			File.WriteAllText(txt, "The pet eats at noon.");
			string tone = t.Tone("audio.wav", 2);
			async Task<string> Message(AiService.Config cfg, string job, string file)
			{
				string msg = null;
				await With(cfg, async () => { var r = await Jobs.Run(job, new[] { file }, null); msg = r.ok ? "(worked)" : r.message; });
				return msg;
			}
			bool Says(string message, string key) => message.EndsWith(T.S(key));
			string m = await Message(null, "summarize", txt);
			t.Check("ai errors: not set up → set it up in DeskPals", Says(m, "ai.notSet"), m);
			m = await Message(fake.Config(key: ""), "summarize", txt);
			t.Check("ai errors: Groq without a key → set it up", Says(m, "ai.notSet"), m);
			fake.Status = 401;
			m = await Message(fake.Config(), "summarize", txt);
			t.Check("ai errors: 401 → the key", Says(m, "ai.badKey"), m);
			fake.Status = 403;
			m = await Message(fake.Config(), "transcribe", tone);
			t.Check("ai errors: 403 → the key (transcribe)", Says(m, "ai.badKey"), m);
			fake.Status = 429;
			m = await Message(fake.Config(), "summarize", txt);
			t.Check("ai errors: 429 → the limit", Says(m, "ai.limit"), m);
			fake.Status = 500;
			m = await Message(fake.Config(), "summarize", txt);
			t.Check("ai errors: another error → try later", Says(m, "ai.serviceError") || m.Contains("500"), m);
			fake.Status = 200;
			m = await Message(fake.Config("ollama", ""), "transcribe", tone);
			t.Check("ai errors: a service without speech-to-text → Groq can", Says(m, "ai.noTranscribe"), m);
			// a port where nothing answers: the internet
			var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			int closed = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();
			m = await Message(new AiService.Config("groq", "http://127.0.0.1:" + closed + "/v1", "x", "k"), "summarize", txt);
			t.Check("ai errors: no connection → check the internet", Says(m, "ai.network"), m);
			m = await Message(new AiService.Config("custom", "http://example.com/v1", "x", "k"), "summarize", txt);
			t.Check("ai errors: a key never goes without https", Says(m, "ai.httpsOnly"), m);
			t.Check("ai errors: the key never appears in a message", !m.Contains("test-key"));
			t.Check("ai errors: nothing written when it failed", !Directory.EnumerateFiles(t.Dir, "*(" + T.S("ai.name.summary") + ")*").Any());
			// canceled while sending
			using var cancel = new CancellationTokenSource();
			cancel.Cancel();
			Jobs.Result rc = null;
			await With(fake.Config(), async () => rc = await Jobs.Run("summarize", new[] { txt }, null, cancel.Token));
			t.Check("ai errors: canceled → stops", !rc.ok && rc.message.Contains(T.S("err.canceled")), rc.message);
		}

		/// <summary>The real service of this computer's DeskPals, only when asked (it uses the internet and the user's key).</summary>
		static async Task Live(DevTests.Ctx t)
		{
			if (Environment.GetEnvironmentVariable("DESKPALS_AI_LIVE") != "1") { t.Skip("ai live: set DESKPALS_AI_LIVE=1 to try the real AI service"); return; }
			var before = AiService.TestOverride;
			AiService.TestOverride = null;
			try
			{
				string txt = Path.Combine(t.Dir, "story.txt");
				File.WriteAllText(txt, "Maria adopted a small orange cat named Tico in May. Tico likes to sleep on the keyboard and eats twice a day. In June, Maria bought him a new bed, but he still prefers the keyboard.");
				var r = await Jobs.Run("summarize", new[] { txt }, null);
				t.Check("ai live: a real summary", r.ok && File.ReadAllText(r.outputs[0]).Length > 20, r.message);
				if (r.ok) t.Info("ai live summary: " + r.preview.Replace('\n', ' '));
				string tone = t.Tone("tone.wav", 3);
				r = await Jobs.Run("transcribe", new[] { tone }, null);
				// a tone has no words: a text or "no speech" are both fine; a broken request is not
				t.Check("ai live: a real transcription request", r.ok || r.message.Contains(T.S("ai.noSpeech")) || r.message.Contains(T.S("ai.noTranscribe")), r.message);
			}
			finally { AiService.TestOverride = before; }
		}
	}
}
