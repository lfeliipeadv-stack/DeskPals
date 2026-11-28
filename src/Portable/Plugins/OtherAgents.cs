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
	// ====================================================================== 6. other AI agents, 7. the pet levels up, 8. local API

	/// <summary>
	/// Codex and Cursor (and any program that can run a command) tell the pet what they are doing, like Claude Code:
	/// they run "DesktopPet.exe --agent=done", which forwards it to the running pet through the local bridge.
	/// </summary>
	public static class OtherAgents
	{
		const string Marker = "--agent=";
		static string Exe => Environment.ProcessPath ?? Application.ExecutablePath;
		/// <summary>Self-test: write the agents' files in a test folder instead of the user's.</summary>
		internal static string HomeOverride;
		static string Home => HomeOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		public static string CodexConfig => Path.Combine(Home, ".codex", "config.toml");
		public static string CursorHooks => Path.Combine(Home, ".cursor", "hooks.json");

		public static bool AnyInstalled => CodexInstalled || CursorInstalled;

		/// <summary>"--agent=done" in another process: forward it to the running pet. Returns the exit code.</summary>
		public static int Signal(string what)
		{
			string ev;
			switch ((what ?? "").Trim().ToLowerInvariant())
			{
				case "done": case "stop": ev = "Stop"; break;
				case "thinking": case "start": ev = "UserPromptSubmit"; break;
				case "waiting": case "input": ev = "Notification"; break;
				case "error": ev = "PostToolUseFailure"; break;
				case "edit": ev = "PreToolUse/edit"; break;
				case "run": ev = "PreToolUse/run"; break;
				case "read": ev = "PreToolUse/read"; break;
				default: return 2;
			}
			try { McpServer.Send("/hook/" + ev, "{}"); return 0; }
			catch (Exception) { return 1; }   // the pet is not running: the agent must not fail because of it
		}

		public static string Command(string what) => "\"" + Exe + "\" " + Marker + what;

		/// <summary>
		/// Codex or Cursor call a program that is not there any more (installed again as DeskPals.exe, so DesktopPet.exe
		/// went away, or moved): they are pointed to this copy. A program that is still there is left alone.
		/// </summary>
		public static void RefreshPaths()
		{
			try
			{
				if (CodexInstalled)
				{
					var ours = File.ReadAllLines(CodexConfig).FirstOrDefault(l => l.TrimStart().StartsWith("notify") && l.Contains(Marker));
					var parts = ours == null ? null : TomlStrings(ours);
					if (parts != null && parts.Count > 0 && !File.Exists(parts[0])) { SetCodex(false); SetCodex(true); }
				}
				if (CursorInstalled && JsonNode.Parse(File.ReadAllText(CursorHooks))?["hooks"] is JsonObject hooks)
				{
					bool gone = hooks.Select(kv => kv.Value).OfType<JsonArray>().SelectMany(a => a)
						.Select(h => h?["command"]?.GetValue<string>() ?? "")
						.Where(c => c.Contains(Marker) && c.StartsWith("\""))
						.Any(c => { int end = c.IndexOf('"', 1); return end > 1 && !File.Exists(c.Substring(1, end - 1)); });
					if (gone) SetCursor(true);
				}
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
		}

		// ------------------------------------------------------------------ Codex: notify = [...] in config.toml

		public static bool CodexInstalled
		{
			get
			{
				try { return File.Exists(CodexConfig) && File.ReadAllText(CodexConfig).Contains(Marker); }
				catch (Exception) { return false; }
			}
		}

		const string SavedPrefix = "# desktopPet, your notify before: ";

		static string Toml(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

		/// <summary>The strings of a one-line TOML array (notify = ["a", "b"]), or null if the line has another form.</summary>
		public static List<string> TomlStrings(string line)
		{
			int open = line.IndexOf('['), close = line.LastIndexOf(']');
			if (open < 0 || close < open) return null;
			var list = new List<string>();
			string inner = line.Substring(open + 1, close - open - 1);
			int i = 0;
			while (i < inner.Length)
			{
				char c = inner[i];
				if (char.IsWhiteSpace(c) || c == ',') { i++; continue; }
				if (c == '\'') { int e = inner.IndexOf('\'', i + 1); if (e < 0) return null; list.Add(inner.Substring(i + 1, e - i - 1)); i = e + 1; continue; }
				if (c != '"') return null;
				var sb = new StringBuilder();
				i++;
				while (i < inner.Length && inner[i] != '"')
				{
					if (inner[i] == '\\' && i + 1 < inner.Length) { i++; sb.Append(inner[i] == 'n' ? '\n' : inner[i] == 't' ? '\t' : inner[i]); }
					else sb.Append(inner[i]);
					i++;
				}
				if (i >= inner.Length) return null;
				list.Add(sb.ToString());
				i++;
			}
			return list;
		}

		/// <summary>
		/// "DesktopPet.exe --agent=done [--then program args…] [payload]": tell the pet, then run the program that was in
		/// Codex's notify before (with the same arguments and the payload Codex adds at the end).
		/// </summary>
		public static int Run(string[] args)
		{
			int code = Signal(args[0].Substring(Marker.Length));
			int then = Array.IndexOf(args, "--then");
			if (then >= 0 && then + 1 < args.Length)
			{
				try
				{
					var psi = new ProcessStartInfo(args[then + 1]) { UseShellExecute = false, CreateNoWindow = true };
					for (int k = then + 2; k < args.Length; k++) psi.ArgumentList.Add(args[k]);
					using (var p = Process.Start(psi)) p?.WaitForExit(60000);
				}
				catch (Exception ex) { LogFile.Ignored(ex); }
			}
			return code == 2 ? 2 : 0;   // an agent must never fail because the pet is closed
		}

		/// <summary>Add or remove the notify line. Returns a message key when something prevents it.</summary>
		public static string SetCodex(bool on)
		{
			try
			{
				var lines = File.Exists(CodexConfig) ? File.ReadAllLines(CodexConfig).ToList() : new List<string>();
				int ours = lines.FindIndex(l => l.TrimStart().StartsWith("notify") && l.Contains(Marker));
				int saved = lines.FindIndex(l => l.StartsWith(SavedPrefix));
				if (!on)
				{
					if (ours < 0) return null;
					// put back the user's own notify line, exactly as it was
					if (saved >= 0) { lines[ours] = lines[saved].Substring(SavedPrefix.Length); lines.RemoveAt(saved); }
					else lines.RemoveAt(ours);
					File.WriteAllLines(CodexConfig, lines);
					return null;
				}
				if (ours >= 0) return null;
				string exe = Toml(Exe);
				string ourLine = $"notify = [{exe}, {Toml(Marker + "done")}]";
				int theirs = lines.FindIndex(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\s*notify\s*="));
				if (theirs >= 0)
				{
					// Codex runs only one notify program: run theirs after us ("--then"), keep a copy of their line to put it back
					var their = TomlStrings(lines[theirs]);
					if (their == null) return "agents.codexBusy";   // a form we do not read (several lines…): leave it alone
					ourLine = $"notify = [{exe}, {Toml(Marker + "done")}, {Toml("--then")}, {string.Join(", ", their.Select(Toml))}]";
					lines.Insert(theirs, SavedPrefix + lines[theirs]);
					lines[theirs + 1] = ourLine;
				}
				else
				{
					// notify is a top-level key: before the first [table]
					int firstTable = lines.FindIndex(l => l.TrimStart().StartsWith("["));
					if (firstTable < 0) lines.Add(ourLine); else lines.Insert(firstTable, ourLine);
				}
				Directory.CreateDirectory(Path.GetDirectoryName(CodexConfig));
				File.WriteAllLines(CodexConfig, lines);
				return null;
			}
			catch (Exception ex) { LogFile.Ignored(ex); return "agents.writeError"; }
			finally { AgentBridge.Apply(); }
		}

		// ------------------------------------------------------------------ Cursor: hooks.json (only hooks that just watch)

		public static bool CursorInstalled
		{
			get
			{
				try { return File.Exists(CursorHooks) && File.ReadAllText(CursorHooks).Contains(Marker); }
				catch (Exception) { return false; }
			}
		}

		public static string SetCursor(bool on)
		{
			try
			{
				JsonObject root = File.Exists(CursorHooks) ? JsonNode.Parse(File.ReadAllText(CursorHooks)) as JsonObject : null;
				root ??= new JsonObject { ["version"] = 1 };
				if (!(root["hooks"] is JsonObject hooks)) root["hooks"] = hooks = new JsonObject();
				foreach (var (ev, what) in new[] { ("afterFileEdit", "edit"), ("stop", "done") })
				{
					if (!(hooks[ev] is JsonArray list)) hooks[ev] = list = new JsonArray();
					foreach (var old in list.Where(h => (h?["command"]?.GetValue<string>() ?? "").Contains(Marker)).ToList()) list.Remove(old);
					if (on) list.Add(new JsonObject { ["command"] = Command(what) });
					if (list.Count == 0) hooks.Remove(ev);
				}
				Directory.CreateDirectory(Path.GetDirectoryName(CursorHooks));
				File.WriteAllText(CursorHooks, root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
				return null;
			}
			catch (Exception ex) { LogFile.Ignored(ex); return "agents.writeError"; }
			finally { AgentBridge.Apply(); }
		}
	}

	/// <summary>The pet grows while you code: every task an AI agent finishes is worth a point; the level goes up.</summary>
	public static class AgentLevel
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("AgentLevel", true);
			set => Program.MyData.SetSetting("AgentLevel", value);
		}

		public static int Tasks => Program.MyData.GetSettingInt("AgentTasks", 0);

		/// <summary>Level 1 at the start, then 3, 8, 15, 24… tasks (each level needs a little more).</summary>
		public static int LevelFor(int tasks) => 1 + (int)Math.Floor(Math.Sqrt(tasks + 1) - 1);

		public static int Level => LevelFor(Tasks);

		public static void TaskDone()
		{
			if (!Enabled) return;
			int before = Level;
			Program.MyData.SetSetting("AgentTasks", Tasks + 1);
			PetLevel.Add(PetLevel.AgentPoints); Achievements.Count("agent");
			if (Level > before)
			{
				Program.Mainthread?.ReactAll("cheer", "happy", "jump", "dance");
				SpeechBubble.Notify(Lang.T("agents.levelUp", Level), 8);
			}
		}
	}

	/// <summary>
	/// The local address other programs can use (only on this computer, with the secret of agent.json): POST /say with a
	/// short text, POST /react with an activity name, POST /hook/&lt;event&gt;.
	/// </summary>
	public static class LocalApi
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("LocalApi", false);
			set { Program.MyData.SetSetting("LocalApi", value); AgentBridge.Apply(); }
		}

		/// <summary>A PowerShell example that makes the pet say something.</summary>
		public static string Example =>
			"$c = Get-Content \"$env:LOCALAPPDATA\\DesktopPet\\agent.json\" | ConvertFrom-Json\r\n" +
			"Invoke-RestMethod -Method Post -Uri \"http://127.0.0.1:$($c.port)/say\" -Headers @{ 'X-DesktopPet' = $c.token } -Body '" + Lang.T("api.exampleText") + "'";
	}
}
