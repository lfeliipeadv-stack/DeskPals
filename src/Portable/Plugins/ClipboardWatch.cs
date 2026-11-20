using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== 4. clipboard

	/// <summary>
	/// Off unless turned on. When a link or a long text is copied, the pet offers to open it, summarize it or translate it
	/// (with the AI, only when you click). The last copies are kept in memory (never on disk) to copy them again.
	/// Copies marked private by password managers are ignored.
	/// </summary>
	public static class ClipboardWatch
	{
		[DllImport("user32.dll")] static extern bool AddClipboardFormatListener(IntPtr hwnd);
		[DllImport("user32.dll")] static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
		[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterClipboardFormat(string name);
		[DllImport("user32.dll")] static extern bool IsClipboardFormatAvailable(uint format);

		static Receiver window;
		static string last = "";
		static DateTime lastBubble = DateTime.MinValue;
		public static readonly List<string> History = new List<string>();
		/// <summary>Set while the program itself writes to the clipboard.</summary>
		public static bool Writing;

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("ClipWatch", false);
			set { Program.MyData.SetSetting("ClipWatch", value); Apply(); }
		}

		/// <summary>Offer actions in a bubble (otherwise only the history is kept).</summary>
		public static bool Offers
		{
			get => Program.MyData.GetSettingBool("ClipOffers", true);
			set => Program.MyData.SetSetting("ClipOffers", value);
		}

		public static void Apply()
		{
			if (Enabled && window == null)
			{
				window = new Receiver();
				AddClipboardFormatListener(window.Handle);
			}
			else if (!Enabled && window != null)
			{
				RemoveClipboardFormatListener(window.Handle);
				window.DestroyHandle();
				window = null;
				History.Clear();
			}
		}

		/// <summary>Copy again (from the history), without offering it back.</summary>
		public static void Copy(string text)
		{
			Writing = true;
			try { Clipboard.SetText(text); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			finally { Writing = false; }
		}

		static bool Private()
		{
			// password managers mark what they copy as "not for history / monitoring"
			try
			{
				return IsClipboardFormatAvailable(RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing"))
					|| IsClipboardFormatAvailable(RegisterClipboardFormat("CanIncludeInClipboardHistory"));
			}
			catch (Exception) { return true; }
		}

		static void Changed()
		{
			if (Writing || Private()) return;
			string text;
			try { if (!Clipboard.ContainsText()) return; text = Clipboard.GetText(); }
			catch (Exception skipped) { LogFile.Ignored(skipped); return; }
			text = text?.Trim() ?? "";
			if (text.Length == 0 || text == last) return;
			last = text;
			History.Remove(text);
			History.Insert(0, text);
			if (History.Count > 20) History.RemoveAt(History.Count - 1);

			if (!Offers || SpeechBubble.QuietNow || (DateTime.Now - lastBubble).TotalSeconds < 45) return;
			if (IsLink(text, out var uri))
			{
				lastBubble = DateTime.Now;
				var choices = new List<SpeechBubble.Choice> { new SpeechBubble.Choice(Lang.T("clip.open"), () => Program.OpenLink(uri.AbsoluteUri), true) };
				SpeechBubble.Say(Lang.T("clip.link", uri.Host), 8, choices.ToArray());
			}
			else if (text.Length >= 60 && AiAssistant.Configured)
			{
				lastBubble = DateTime.Now;
				OfferAi(text, 10);
			}
		}

		/// <summary>An error message (a program's error, a stack trace…): the pet offers to explain it first.</summary>
		internal static bool LooksLikeError(string text) =>
			System.Text.RegularExpressions.Regex.IsMatch(text, @"(\w*exception\b|\b(error|erro|exceção|failed|falhou|falha|fatal|traceback|stack trace|denied|negado|not found|não encontrad[oa]|HRESULT|0x[0-9A-F]{8})\b)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

		/// <summary>The help the AI gives with a copied text: explain, summarize, translate.</summary>
		public static void OfferAi(string text, int seconds = 0)
		{
			if (string.IsNullOrWhiteSpace(text)) return;
			string copied = text.Length > 6000 ? text.Substring(0, 6000) : text;
			bool error = LooksLikeError(copied);
			var explain = new SpeechBubble.Choice(Lang.T(error ? "clip.explainError" : "clip.explain"), () => AskAi(Lang.T(error ? "clip.askError" : "clip.askExplain") + "\n\n" + copied), error);
			var summary = new SpeechBubble.Choice(Lang.T("clip.summary"), () => AskAi(Lang.T("clip.askSummary") + "\n\n" + copied), !error);
			var translate = new SpeechBubble.Choice(Lang.T("clip.translate"), () => AskAi(Lang.T("clip.askTranslate") + "\n\n" + copied));
			SpeechBubble.Say(Lang.T(error ? "clip.error" : "clip.long"), seconds, error ? new[] { explain, translate } : new[] { summary, explain, translate });
		}

		/// <summary>The pet menu: help with the last copied text.</summary>
		public static void OfferLast() { if (History.Count > 0) OfferAi(History[0]); }

		public static bool IsLink(string text, out Uri uri)
		{
			uri = null;
			return !text.Contains('\n') && !text.Contains(' ') && Uri.TryCreate(text, UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https");
		}

		static async void AskAi(string question)
		{
			if (!FormAsk.PrivacyOk()) return;   // the copied text leaves the computer only after the notice
			SpeechBubble.Say(Lang.T("clip.thinking"), 30);
			try
			{
				string answer = await AiAssistant.Ask(question);
				SpeechBubble.Say(answer, 0, new SpeechBubble.Choice(Lang.T("clip.copyAnswer"), () => Copy(answer), true), new SpeechBubble.Choice("OK", null));
			}
			catch (Exception ex) { SpeechBubble.Say(Lang.T("clip.aiError", ex.Message), 8); }
		}

		sealed class Receiver : NativeWindow
		{
			public Receiver() { CreateHandle(new CreateParams()); }
			protected override void WndProc(ref Message m)
			{
				const int WM_CLIPBOARDUPDATE = 0x031D;
				if (m.Msg == WM_CLIPBOARDUPDATE) { try { Changed(); } catch (Exception skipped) { LogFile.Ignored(skipped); } }
				base.WndProc(ref m);
			}
		}

		/// <summary>The last copies: click one to copy it again.</summary>
		public static void ShowHistory(IWin32Window owner)
		{
			using (var f = new FormClipHistory()) f.ShowDialog(owner);
		}

		sealed class FormClipHistory : ThemedDialog
		{
			public FormClipHistory()
			{
				Text = Lang.T("clip.historyTitle");
				iconEmoji = "📋";
				const int W = 520;
				var list = new ListBox { Location = new Point(Pad, 88), Size = new Size(W - Pad * 2, 260), Font = fSmall, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
				foreach (var h in History) list.Items.Add(h.Replace("\r", " ").Replace("\n", " ⏎ "));
				if (History.Count == 0) list.Items.Add(Lang.T("clip.historyEmpty"));
				list.DoubleClick += (s, e) =>
				{
					if (list.SelectedIndex < 0 || list.SelectedIndex >= History.Count) return;
					Copy(History[list.SelectedIndex]);
					DialogResult = DialogResult.OK;
				};
				Controls.Add(list);
				var close = AddButton(Lang.T("launch.close"), DialogResult.OK, true);
				ApplyTheme(close);
				ClientSize = new Size(W, LayoutButtons(list.Bottom + 18, W) + Pad);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);
				Theme.Prepare(e.Graphics);
				PaintIcon(e.Graphics);
				Theme.DrawText(e.Graphics, Lang.T("clip.historyHelp"), fSmall, new Rectangle(Pad + 62, Pad + 4, ClientSize.Width - Pad * 2 - 62, 50), Theme.TextMuted);
			}
		}
	}
}
