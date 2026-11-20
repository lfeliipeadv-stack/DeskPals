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
	// The self-test, part: integrations. Check, Try and the list of tests are in SelfTest.cs.
	internal static partial class SelfTest
	{
		static void Mcp()
		{
			System.Text.Json.Nodes.JsonNode Ask(string json) => McpServer.Handle(System.Text.Json.Nodes.JsonNode.Parse(json) as System.Text.Json.Nodes.JsonObject);
			var init = Ask("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\"}}");
			Check("mcp: initialize", init?["result"]?["serverInfo"]?["name"]?.GetValue<string>() == "desktoppet");
			Check("mcp: notification gets no answer", Ask("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}") == null);
			var tools = Ask("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}")?["result"]?["tools"]?.AsArray();
			Check("mcp: 3 tools", tools != null && tools.Count == 3 && tools.Any(t => t["name"]?.GetValue<string>() == "pet_say"));
			var bad = Ask("{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"pet_say\",\"arguments\":{\"text\":\"veja C:\\\\Users\\\\x\\\\senha.txt\"}}}");
			Check("mcp: a path is never said", bad?["result"]?["isError"]?.GetValue<bool>() == true);
			Check("mcp: unknown method", Ask("{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"nada\"}")?["error"] != null);
			Check("say filter: normal sentence ok", AgentBridge.IsSafeToSay("Terminei os testes, 2 falharam."));
			Check("say filter: link and key refused", !AgentBridge.IsSafeToSay("veja https://x.com") && !AgentBridge.IsSafeToSay("token sk_" + "live_" + "abcdefghijklmnopqrstuvwxyz123456"));
		}

		static void ClaudeHooksFile()
		{
			string dir = Path.Combine(Path.GetTempPath(), "selftest-claude");
			Directory.CreateDirectory(dir);
			string path = Path.Combine(dir, "settings.json");
			File.WriteAllText(path, "{\n  \"model\": \"opus\",\n  \"hooks\": { \"PreToolUse\": [ { \"matcher\": \"Bash\", \"hooks\": [ { \"type\": \"command\", \"command\": \"echo mine\" } ] } ] }\n}");
			ClaudeHooks.Install(path);
			string text = File.ReadAllText(path);
			Check("hooks: installed", ClaudeHooks.IsInstalled(path) && ClaudeHooks.IsCurrent(path));
			Check("hooks: other settings kept", text.Contains("\"model\": \"opus\"") && text.Contains("echo mine"));
			Check("hooks: backup saved", File.Exists(ClaudeHooks.BackupPath(path)) && File.ReadAllText(ClaudeHooks.BackupPath(path)).Contains("echo mine"));
			ClaudeHooks.Install(path);
			int count = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path), "X-DesktopPet:").Count;
			Check("hooks: installing twice does not duplicate", count == 10, count.ToString());
			ClaudeHooks.Uninstall(path);
			text = File.ReadAllText(path);
			Check("hooks: removed, the user's hook stays", !ClaudeHooks.IsInstalled(path) && text.Contains("echo mine") && text.Contains("opus"), text);
			File.Delete(path);
			ClaudeHooks.Install(path);
			Check("hooks: a missing file is created", ClaudeHooks.IsInstalled(path));
			ClaudeHooks.Uninstall(path);
			Check("hooks: empty hooks section removed", !File.ReadAllText(path).Contains("\"hooks\""));
			File.WriteAllText(path, "{ not json");
			bool refused = false;
			try { ClaudeHooks.Install(path); } catch (Exception) { refused = true; }
			Check("hooks: a broken settings file is never overwritten", refused && File.ReadAllText(path) == "{ not json");
			Directory.Delete(dir, true);
		}

		static void ClaudeEvents()
		{
			var cases = new (string path, string json, AgentBridge.Activity expected)[]
			{
				("UserPromptSubmit", "{}", AgentBridge.Activity.Thinking),
				("PreToolUse/edit", "", AgentBridge.Activity.Editing),
				("PreToolUse/run", "", AgentBridge.Activity.Running),
				("PreToolUse/read", "", AgentBridge.Activity.Reading),
				("PreToolUse/web", "", AgentBridge.Activity.Searching),
				("PreToolUse", "{\"tool_name\":\"Write\"}", AgentBridge.Activity.Editing),
				("PostToolUseFailure", "", AgentBridge.Activity.Error),
				("PostToolUse", "{\"tool_name\":\"Bash\"}", AgentBridge.Activity.None),
				("Notification", "", AgentBridge.Activity.Waiting),
				("Stop", "", AgentBridge.Activity.Done),
				("SessionStart", "", AgentBridge.Activity.Hello),
			};
			foreach (var c in cases)
				Check("claude event: " + c.path, AgentBridge.ClassifyPath(c.path, c.json) == c.expected, AgentBridge.ClassifyPath(c.path, c.json).ToString());
		}

		static async Task ClaudeEndpoint()
		{
			bool was = AgentBridge.Enabled;
			AgentBridge.Enabled = true;
			try
			{
				Check("endpoint: listening on 127.0.0.1", AgentBridge.Listening);
				if (!AgentBridge.Listening) return;
				int ok = await AgentBridge.SendTest("PreToolUse/edit", "{}");
				await Task.Delay(300);
				Check("endpoint: event accepted", ok == 204 && AgentBridge.Last == AgentBridge.Activity.Editing, ok + " " + AgentBridge.Last);
				int bad = await AgentBridge.SendTest("Stop", "{}", "wrong-token");
				await Task.Delay(200);
				Check("endpoint: wrong secret refused", bad == 403 && AgentBridge.Last == AgentBridge.Activity.Editing, bad + " " + AgentBridge.Last);

				// the real hook command, as Claude Code runs it (Git Bash if installed, otherwise PowerShell)
				string cmd = $"curl.exe -s -m 2 -X POST -d x -H \"X-DesktopPet: {AgentBridge.Token}\" http://127.0.0.1:{AgentBridge.Port}/hook/Stop; exit 0";
				foreach (var (shell, args) in new[] { (GitBash(), "-c"), ("powershell", "-NoProfile -Command"), ("pwsh", "-NoProfile -Command") })
				{
					var (code, output) = await RunShell(shell, args, cmd);
					if (code == int.MinValue) { lines.Add($"SKIP  hook command in {shell}: not installed"); continue; }
					await Task.Delay(300);
					Check($"hook command in {shell}: exit 0, no output, pet reacts", code == 0 && output.Trim().Length == 0 && AgentBridge.Last == AgentBridge.Activity.Done, $"exit {code}, output '{output.Trim()}', last {AgentBridge.Last}");
					AgentBridge.React(AgentBridge.Activity.Thinking);
				}
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// pet closed: the hook must still end quietly (Claude Code must never show an error)
				AgentBridge.Enabled = false;
				var (code2, out2) = await RunShell("powershell", "-NoProfile -Command", cmd);
				Check("hook command with the pet closed: exit 0, no output", code2 == 0 && out2.Trim().Length == 0, $"exit {code2}, output '{out2.Trim()}'");
			}
			finally { AgentBridge.Enabled = was; }
		}

		static string GitBash()
		{
			foreach (var p in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\Programs" })
			{
				if (string.IsNullOrEmpty(p)) continue;
				string b = Path.Combine(p, "Git", "bin", "bash.exe");
				if (File.Exists(b)) return b;
			}
			return "bash";
		}

		static async Task<(int code, string output)> RunShell(string shell, string args, string command)
		{
			try
			{
				var psi = new System.Diagnostics.ProcessStartInfo(shell)
				{
					UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true, CreateNoWindow = true,
				};
				psi.Arguments = args + " \"" + command.Replace("\"", "\\\"") + "\"";
				using (var p = System.Diagnostics.Process.Start(psi))
				{
					p.StandardInput.Write("{\"hook_event_name\":\"Stop\"}");
					p.StandardInput.Close();
					string o = await p.StandardOutput.ReadToEndAsync();
					await Task.Run(() => p.WaitForExit(15000));
					return (p.ExitCode, o);
				}
			}
			catch (System.ComponentModel.Win32Exception) { return (int.MinValue, ""); }
		}
	}
}
