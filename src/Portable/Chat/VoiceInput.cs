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
	/// The microphone of the chat. By default the pet records one sentence and the AI in use writes it (Whisper, on Groq
	/// or OpenAI). Without such a service, the speech recognizer of Windows runs through a hidden Windows PowerShell
	/// (in every Windows 10 and 11), which keeps the program small. That one needs "Online speech recognition" on in the
	/// Windows privacy settings, and on some computers it hears nothing. The other way is the Windows dictation (Win+H),
	/// which types into the box: the right button on a microphone chooses.
	/// </summary>
	internal static class VoiceInput
	{
		public enum Outcome { Text, Nothing, Privacy, Failed, Canceled }

		// S:listening when ready, R:<text> at the end, E:<reason> on error (ASCII only: Windows PowerShell reads it as is)
		const string Script = @"$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
try {
  Add-Type -AssemblyName System.Runtime.WindowsRuntime
  $null = [Windows.Media.SpeechRecognition.SpeechRecognizer, Windows.Media.SpeechRecognition, ContentType = WindowsRuntime]
  $asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
  function Wait($op, [Type]$t) { $task = $asTask.MakeGenericMethod($t).Invoke($null, @($op)); $task.Wait(); $task.Result }
  $r = New-Object Windows.Media.SpeechRecognition.SpeechRecognizer
  $r.Timeouts.InitialSilenceTimeout = [TimeSpan]::FromSeconds(7)
  $r.Timeouts.EndSilenceTimeout = [TimeSpan]::FromSeconds(1.2)
  $compiled = Wait ($r.CompileConstraintsAsync()) ([Windows.Media.SpeechRecognition.SpeechRecognitionCompilationResult])
  if ($compiled.Status -ne 'Success') { 'E:compile:' + $compiled.Status; exit 1 }
  'S:listening'; [Console]::Out.Flush(); $sw = [Diagnostics.Stopwatch]::StartNew()
  $res = Wait ($r.RecognizeAsync()) ([Windows.Media.SpeechRecognition.SpeechRecognitionResult])
  'I:' + $res.Status + ' confidence=' + $res.Confidence + ' raw=' + [Math]::Round($res.RawConfidence, 2) + ' ms=' + $sw.ElapsedMilliseconds + ' duration=' + [Math]::Round($res.PhraseDuration.TotalSeconds, 1) + ' alternates=' + (($res.GetAlternates(3) | % { $_.Text }) -join ' | ')
  if ($res.Status -ne 'Success') { 'E:status:' + $res.Status; exit 1 }
  'R:' + $res.Text
}
catch {
  $h = $_.Exception.HResult; $inner = $_.Exception.InnerException; while ($inner) { $h = $inner.HResult; $inner = $inner.InnerException }
  if ($h -eq -2147199735) { 'E:privacy' } else { 'E:' + ('{0:X8}' -f $h) + ':' + $_.Exception.Message }
  exit 1
}
";

		static string ScriptPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "ouvir.ps1");

		/// <summary>The last reason it could not listen (for the log and the self-test).</summary>
		internal static string LastError { get; private set; }

		// ------------------------------------------------------------------ what the microphone does

		/// <summary>
		/// What the microphone button does: the pet records and the AI writes it, the Windows online speech recognizer
		/// listens (works on some computers, not on others), or the Windows dictation (Win+H) types into the box.
		/// </summary>
		public enum Mode { Ai, Dictation, Windows }

		/// <summary>The modes in the order they are offered, with their names.</summary>
		internal static readonly (Mode mode, string key)[] Modes =
			{ (Mode.Ai, "voice.modeAi"), (Mode.Windows, "voice.modeWindows"), (Mode.Dictation, "voice.modeDictation") };

		public static Mode Current
		{
			get
			{
				switch (Program.MyData.GetSetting("VoiceMode"))
				{
					case "dictation": return Mode.Dictation;
					case "windows": return Mode.Windows;
					default: return Mode.Ai;
				}
			}
			set => Program.MyData.SetSetting("VoiceMode", value == Mode.Dictation ? "dictation" : value == Mode.Windows ? "windows" : "ai");
		}

		/// <summary>The microphone's tip: what a click does, and that the right button changes it.</summary>
		public static string Tip => Lang.T(Current == Mode.Dictation ? "voice.tipDictation" : Current == Mode.Windows ? "voice.tipWindows" : "voice.tipAi") + "\n" + Lang.T("voice.tipChange");

		/// <summary>The right button on a microphone: choose the AI, the Windows online recognizer or the Windows dictation.</summary>
		public static void ShowModeMenu(Point screen)
		{
			var menu = new ContextMenuStrip();
			var head = new ToolStripMenuItem(Lang.T("voice.menuTitle")) { Enabled = false };
			head.Font = new Font(head.Font, FontStyle.Bold);
			menu.Items.Add(head);
			foreach (var (mode, key) in Modes)
			{
				var m = mode;
				var item = new ToolStripMenuItem(Lang.T(key)) { Checked = Current == m };
				item.Click += (s, e) => Current = m;
				menu.Items.Add(item);
			}
			if (AiAssistant.TranscriptionModel == null)
				menu.Items.Add(new ToolStripMenuItem(Lang.T("voice.noAiHint")) { Enabled = false });
			Theme.ApplyTo(menu);
			menu.Closed += (s, e) => menu.BeginInvoke(new Action(menu.Dispose));
			menu.Show(screen);
		}

		/// <summary>Where the last sentence went: true when the AI wrote it (for the explanation when it fails).</summary>
		static bool lastByAi;

		/// <summary>
		/// Listen to one sentence: recorded and written by the AI when the service in use can (Groq, OpenAI),
		/// otherwise (or in the Windows mode) the Windows recognizer. <paramref name="ready"/> runs when the microphone
		/// is open and <paramref name="working"/> when the AI is writing what it heard (both on the calling thread).
		/// Canceling stops the listening at once.
		/// </summary>
		public static async Task<(Outcome outcome, string text)> Listen(Action ready, Action working, System.Threading.CancellationToken cancel)
		{
			lastByAi = Current != Mode.Windows && AiAssistant.TranscriptionModel != null;
			if (!lastByAi) return await ListenWindows(ready, cancel);
			LastError = null;
			var (result, wav) = await VoiceRecorder.Record(ready, cancel);
			LogFile.Important($"voice recorded: {result}, {(wav?.Length ?? 0) / 32000.0:0.0} s, peak {VoiceRecorder.LastPeak} {VoiceRecorder.LastError}");
			switch (result)
			{
				case VoiceRecorder.Result.Canceled: return (Outcome.Canceled, null);
				case VoiceRecorder.Result.Silence: return (Outcome.Nothing, null);
				case VoiceRecorder.Result.NoMicrophone: LastError = VoiceRecorder.LastError; lastByAi = false; return (Outcome.Failed, null);
			}
			working?.Invoke();
			try
			{
				string text = await AiAssistant.Transcribe(wav, cancel);
				LogFile.Important($"voice: the AI wrote {text.Length} letters");
				return text.Length > 0 ? (Outcome.Text, text) : (Outcome.Nothing, null);
			}
			catch (Exception) when (cancel.IsCancellationRequested) { return (Outcome.Canceled, null); }
			catch (Exception ex)
			{
				LastError = ex.Message;
				StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "voice (AI): " + ex.Message);
				return (Outcome.Failed, null);
			}
		}

		/// <summary>One sentence with the Windows speech recognizer (needs "Online speech recognition" on).</summary>
		internal static async Task<(Outcome outcome, string text)> ListenWindows(Action ready, System.Threading.CancellationToken cancel)
		{
			LastError = null;
			try
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScriptPath));
				System.IO.File.WriteAllText(ScriptPath, Script.Replace("\r\n", "\n").Replace("\n", "\r\n"), new System.Text.UTF8Encoding(true));
				var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + ScriptPath + "\"")
				{
					UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, StandardOutputEncoding = System.Text.Encoding.UTF8,
				};
				using (var p = System.Diagnostics.Process.Start(psi))
				using (cancel.Register(() => { try { if (!p.HasExited) p.Kill(); } catch (Exception) { } }))
				{
					string line, result = null, error = null;
					while ((line = await p.StandardOutput.ReadLineAsync()) != null)
					{
						if (line == "S:listening") ready?.Invoke();
						else if (line.StartsWith("R:")) result = line.Substring(2).Trim();
						else if (line.StartsWith("E:")) error = line.Substring(2);
						else if (line.StartsWith("I:")) LogFile.Important("voice heard: " + line.Substring(2));
					}
					if (cancel.IsCancellationRequested) return (Outcome.Canceled, null);
					LastError = error;
					LogFile.Important($"voice: {(result == null ? "no text" : result.Length == 0 ? "empty text" : result.Length + " letters")}, {error ?? "no error"}, exit {p.ExitCode}");
					if (!string.IsNullOrEmpty(result)) return (Outcome.Text, result);
					if (error == "privacy") return (Outcome.Privacy, null);
					// nothing understood (silence, a pause, noise, Windows stopping by itself): "I did not hear"; the microphone, the network or the language: a real failure
					if (result != null || (error != null && System.Text.RegularExpressions.Regex.IsMatch(error, "^status:(Timeout|PauseLimit|UserCanceled|Unknown|AudioQuality)"))) return (Outcome.Nothing, null);
					if (error != null) StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "voice: " + error);
					return (Outcome.Failed, null);
				}
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
				StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "voice: " + ex.Message);
				return cancel.IsCancellationRequested ? (Outcome.Canceled, null) : (Outcome.Failed, null);
			}
		}

		/// <summary>Could not listen: the pet says why and offers the way out (the setting, or the Windows dictation).</summary>
		public static void Explain(Outcome outcome)
		{
			var dictation = new SpeechBubble.Choice(Lang.T("voice.useDictation"), () => { QuickChat.Open(); FormAsk.VoiceTyping(); });
			if (outcome == Outcome.Privacy)
				SpeechBubble.Say(Lang.T("voice.privacy"), 0,
					new SpeechBubble.Choice(Lang.T("voice.openSettings"), () =>
					{
						try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:privacy-speech") { UseShellExecute = true }); }
						catch (Exception skipped) { LogFile.Ignored(skipped); }
					}, true),
					dictation);
			else if (outcome == Outcome.Failed && lastByAi)
				SpeechBubble.Say(Lang.T("voice.aiFailed", LastError ?? ""), 0, dictation,
					new SpeechBubble.Choice(Lang.T("voice.alwaysDictation"), () => { Current = Mode.Dictation; QuickChat.Open(); FormAsk.VoiceTyping(); }));
			else if (outcome == Outcome.Failed)
				SpeechBubble.Say(Lang.T("voice.failed"), 0, dictation);
		}
	}
}
