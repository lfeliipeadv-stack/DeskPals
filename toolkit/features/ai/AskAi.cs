using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using DesktopPet;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Ask about the file": a small conversation with the assistant about one document (PDF, Word, text) or recording
	/// (transcribed first). The file is read once; each question sends the parts of it that match the question best
	/// (all of it when it is short), and the last answers, so a follow-up question works. When the window closes, the
	/// questions and answers are kept in "&lt;name&gt; (questions).txt" next to the file.
	/// </summary>
	internal static class AskAi
	{
		/// <summary>A text up to this long goes whole; a longer one is sent in its best parts, up to this much.</summary>
		internal const int Whole = 14000, Piece = 2500;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "askai", Emoji = "💬",
				// right after "summarize" (26 for documents)
				Rank = f => f.Count == 1 && File.Exists(f[0]) && (AiText.CanRead(f[0]) || IsAudio(f[0]) || IsVideo(f[0])) ? 27 : -1,
				Ask = (owner, files) => AskAiForm.Open(owner, files[0]),
				Run = Save,
			});
		}

		/// <summary>The conversation, written by the window to a temporary file, kept next to the document.</summary>
		static async Task Save(JobContext c)
		{
			string talk = c.Option("conversation");
			if (string.IsNullOrEmpty(talk) || !File.Exists(talk)) { c.Done = true; return; }
			try
			{
				string o = OutName(c.Files[0], ".txt", T.S("askai.name"));
				File.Copy(talk, o, false);
				c.Outputs.Add(o);
				string last = c.Option("last");
				if (last.Length > 0) c.Preview = last.Length > 300 ? last[..300].TrimEnd() + "…" : last;
			}
			finally { try { File.Delete(talk); } catch (Exception) { } }
			await Task.CompletedTask;
		}

		/// <summary>The text to send with a question: all of it when short, else the parts with most of the question's words (in their order).</summary>
		internal static string Parts(string text, string question)
		{
			if (text.Length <= Whole) return text;
			var pieces = Split(text);
			var words = Words(question);
			var scored = pieces.Select((p, i) => (i, p, score: Score(p, words))).ToList();
			var chosen = new List<(int i, string p)>();
			int size = 0;
			foreach (var s in scored.OrderByDescending(x => x.score).ThenBy(x => x.i))
			{
				if (size + s.p.Length > Whole) break;
				chosen.Add((s.i, s.p));
				size += s.p.Length;
			}
			// nothing matched at all: the start of the document (where the parties, the subject… usually are)
			if (scored.All(x => x.score == 0)) chosen = pieces.Select((p, i) => (i, p)).TakeWhile((x, k) => pieces.Take(k + 1).Sum(y => y.Length) <= Whole).ToList();
			return string.Join("\n[…]\n", chosen.OrderBy(x => x.i).Select(x => x.p));
		}

		static List<string> Split(string text)
		{
			var list = new List<string>();
			var sb = new StringBuilder();
			foreach (var para in text.Split('\n'))
			{
				if (sb.Length + para.Length > Piece && sb.Length > 0) { list.Add(sb.ToString().Trim()); sb.Clear(); }
				// one very long paragraph is cut too
				string rest = para;
				while (rest.Length > Piece) { list.Add(rest[..Piece]); rest = rest[Piece..]; }
				sb.Append(rest).Append('\n');
			}
			if (sb.ToString().Trim().Length > 0) list.Add(sb.ToString().Trim());
			return list;
		}

		/// <summary>The words of a text that mean something (3 letters or more), without accents and case.</summary>
		internal static HashSet<string> Words(string s)
		{
			string plain = new string(s.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
			return plain.Split(plain.Where(ch => !char.IsLetterOrDigit(ch)).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length >= 3).ToHashSet();
		}

		static int Score(string piece, HashSet<string> words)
		{
			if (words.Count == 0) return 0;
			var have = Words(piece);
			return words.Count(have.Contains);
		}

		internal const string System = "You answer questions about a document the user gave you. Use only what the document says. "
			+ "If the answer is not in it, say so plainly. Answer briefly, in the language of the question. "
			+ "When it helps, quote the words of the document that give the answer.";

		/// <summary>The message with the document's parts, the last questions and answers, and the question.</summary>
		internal static string Message(string text, string question, IList<(string q, string a)> before)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Document:").AppendLine("<<<").AppendLine(Parts(text, question + " " + string.Join(" ", before.TakeLast(1).Select(x => x.q)))).AppendLine(">>>");
			foreach (var (q, a) in before.TakeLast(3)) sb.AppendLine().Append("Earlier question: ").AppendLine(q).Append("Earlier answer: ").AppendLine(a);
			sb.AppendLine().Append("Question: ").Append(question);
			return sb.ToString();
		}
	}

	/// <summary>The window of "Ask about the file": the conversation, the question box and Ask.</summary>
	internal sealed class AskAiForm : Form
	{
		readonly string file;
		readonly RichTextBox talk;
		readonly TextBox question;
		readonly Button ask;
		readonly Label status;
		readonly Font fText = Theme.CreateFont(13), fBold = Theme.CreateFont(13, FontStyle.Bold), fSmall = Theme.CreateFont(11.5f);
		readonly CancellationTokenSource closing = new();
		readonly List<(string q, string a)> done = new();
		string text;
		AiService.Config cfg;

		public static Dictionary<string, string> Open(IWin32Window owner, string file)
		{
			using var f = new AskAiForm(file) { ShowInTaskbar = owner == null, StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent };
			if (owner == null) f.ShowDialog(); else f.ShowDialog(owner);
			return f.Answers();
		}

		/// <summary>The conversation for the job (a temporary file), or null when nothing was asked.</summary>
		internal Dictionary<string, string> Answers()
		{
			if (done.Count == 0) return null;
			string tmp = Temp(".txt");
			var sb = new StringBuilder();
			sb.AppendLine(Path.GetFileName(file)).AppendLine();
			foreach (var (q, a) in done) sb.Append("❓ ").AppendLine(q).AppendLine().AppendLine(a).AppendLine().AppendLine("—").AppendLine();
			File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(true));
			return new Dictionary<string, string> { ["conversation"] = tmp, ["last"] = done[^1].a };
		}

		internal AskAiForm(string file)
		{
			this.file = file;
			Text = T.S("job.askai") + " — " + Path.GetFileName(file);
			AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
			ClientSize = new Size(640, 560);
			MinimumSize = new Size(480, 400);
			try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch (Exception) { }
			Theme.ApplyWindowChrome(this);
			BackColor = Theme.Background; ForeColor = Theme.Text;
			Padding = new Padding(16, 12, 16, 12);

			status = new Label { Dock = DockStyle.Top, Height = 28, Font = fSmall, ForeColor = Theme.TextMuted, Text = T.S("ai.reading") + "…" };
			talk = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, Font = fText, DetectUrls = false, ScrollBars = RichTextBoxScrollBars.Vertical };
			var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 52, ColumnCount = 2, BackColor = Theme.Background, Padding = new Padding(0, 10, 0, 0) };
			bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			question = new TextBox { Dock = DockStyle.Fill, Font = fText, BackColor = Theme.Surface, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Enabled = false, PlaceholderText = T.S("askai.placeholder") };
			ask = new Button { Text = "💬  " + T.S("askai.ask"), AutoSize = true, Font = fBold, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Theme.AccentText, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(10, 2, 10, 2), Enabled = false, Cursor = Cursors.Hand };
			ask.FlatAppearance.BorderSize = 0;
			ask.Click += async (s, e) => await Ask();
			bottom.Controls.Add(question, 0, 0);
			bottom.Controls.Add(ask, 1, 0);
			AcceptButton = ask;

			Controls.Add(talk);
			Controls.Add(bottom);
			Controls.Add(status);
			ButtonLook.Hook(this);
			Shown += async (s, e) => await Read();
			FormClosed += (s, e) => closing.Cancel();
		}

		/// <summary>The file read (or heard) once, at the start.</summary>
		async Task Read()
		{
			try
			{
				cfg = AiService.Ready(needModel: true);
				if (IsAudio(file) || IsVideo(file))
				{
					AiService.TranscriptionModel(cfg);
					text = await AiJobs.Transcribe(cfg, file, (p, s) => BeginInvoke(() => status.Text = s), closing.Token);
				}
				else text = await AiText.Read(file, (p, s) => BeginInvoke(() => status.Text = s), closing.Token);
				if (text.Count(char.IsLetterOrDigit) < 3) throw new AiException(T.S("ai.noText"));
				status.Text = T.S("askai.ready");
				question.Enabled = ask.Enabled = true;
				question.Focus();
			}
			catch (OperationCanceledException) { }
			catch (Exception ex) { if (!IsDisposed) { status.Text = ""; Say(null, Friendly(ex)); } }
		}

		/// <summary>Tests: the text as if read.</summary>
		internal void UseText(string t, AiService.Config c) { text = t; cfg = c; question.Enabled = ask.Enabled = true; }
		internal TextBox Question => question;
		internal int Count => done.Count;
		internal string Shown_ => talk.Text;

		internal async Task Ask()
		{
			string q = question.Text.Trim();
			if (q.Length == 0 || text == null) return;
			question.Enabled = ask.Enabled = false;
			status.Text = T.S("askai.thinking");
			Say(q, null);
			question.Text = "";
			try
			{
				string a = await AiService.Chat(cfg, AskAi.System, AskAi.Message(text, q, done), closing.Token, w => BeginInvoke(() => status.Text = w), 900);
				a = Plain(a);
				done.Add((q, a));
				Say(null, a);
				status.Text = T.S("askai.ready");
			}
			catch (OperationCanceledException) { return; }
			catch (Exception ex) { Say(null, "⚠️ " + Friendly(ex)); status.Text = ""; }
			if (IsDisposed) return;
			question.Enabled = ask.Enabled = true;
			question.Focus();
		}

		/// <summary>The answer without the marks of formatted text (**bold**, # titles), which the window shows as they are.</summary>
		internal static string Plain(string a)
		{
			a = System.Text.RegularExpressions.Regex.Replace(a ?? "", @"(\*\*|__)(.+?)\1", "$2");
			a = System.Text.RegularExpressions.Regex.Replace(a, @"(?m)^#{1,6}\s+", "");
			return a.Trim();
		}

		/// <summary>A line of the conversation: the question (bold) or the answer.</summary>
		void Say(string q, string a)
		{
			talk.SelectionStart = talk.TextLength;
			if (talk.TextLength > 0) talk.AppendText("\n");
			if (q != null)
			{
				talk.SelectionFont = fBold;
				talk.SelectionColor = Theme.Accent;
				talk.AppendText("❓ " + q + "\n");
			}
			if (a != null)
			{
				talk.SelectionFont = fText;
				talk.SelectionColor = Theme.Text;
				talk.AppendText(a + "\n");
			}
			talk.ScrollToCaret();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) { fText.Dispose(); fBold.Dispose(); fSmall.Dispose(); closing.Dispose(); }
			base.Dispose(disposing);
		}
	}
}
