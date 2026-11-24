using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace DesktopPet
{
	/// <summary>
	/// Where the program is installed, shared by both installers (the program's own and DesktopPet-Instalador.exe;
	/// this file is compiled into both). The program goes to C:\Program Files\DesktopPet (installing, updating and
	/// uninstalling ask for administrator permission); the user's pets, settings, reminders and logs stay in
	/// %LocalAppData%\DesktopPet, where older versions also kept the program itself.
	/// </summary>
	internal static class InstallPlace
	{
		/// <summary>
		/// The folders (Program Files, %LocalAppData%), the uninstall entry and the "start with Windows" value keep this
		/// name: the user's pets and settings live there and every installed version finds them.
		/// </summary>
		public const string AppName = "DesktopPet";
		public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + AppName;

		/// <summary>The program and its shortcuts are called DeskPals from 1.1.1 on; before, DesktopPet.</summary>
		public const string ExeName = "DeskPals.exe", OldExeName = "DesktopPet.exe";
		public const string EditorExeName = "DeskPals-Editor.exe", OldEditorExeName = "PetEditor.exe";
		public const string ShortcutName = "DeskPals.url", OldShortcutName = "DesktopPet.url";

		/// <summary>The program in this folder: DeskPals.exe, or DesktopPet.exe of an install updated from before the new name.</summary>
		public static string ExeIn(string dir) =>
			!File.Exists(Path.Combine(dir, ExeName)) && File.Exists(Path.Combine(dir, OldExeName)) ? Path.Combine(dir, OldExeName) : Path.Combine(dir, ExeName);

		/// <summary>Process names (without ".exe") of the program and the editor, new and old, to close them before installing.</summary>
		public static readonly string[] ProcessNames = { "DeskPals", "DesktopPet", "DeskPals-Editor", "PetEditor" };

		/// <summary>
		/// An install made before the new names: its DesktopPet.exe, PetEditor.exe and DesktopPet.url shortcuts go away
		/// (the new ones were just written next to them). Shortcuts in <paramref name="shortcutFolders"/>.
		/// </summary>
		public static void RemoveOldNames(string programDir, IEnumerable<string> shortcutFolders)
		{
			foreach (string name in new[] { OldExeName, "DesktopPet_old.exe", OldEditorExeName })
				try { File.Delete(Path.Combine(programDir, name)); } catch (Exception) { }
			foreach (string folder in shortcutFolders)
				try { File.Delete(Path.Combine(folder, OldShortcutName)); } catch (Exception) { }
		}

		/// <summary>C:\Program Files\DesktopPet (the 64-bit folder, also when asked from a 32-bit process).</summary>
		public static string ProgramDir => Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppName);

		/// <summary>%LocalAppData%\DesktopPet: the user's things (and the program of versions before 2.7).</summary>
		public static string DataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

		public static bool IsAdmin()
		{
			try { using (var id = WindowsIdentity.GetCurrent()) return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator); }
			catch (Exception) { return false; }
		}

		/// <summary>True if <paramref name="folder"/> is inside Program Files (there the settings can't be written without administrator).</summary>
		public static bool InProgramFiles(string folder)
		{
			foreach (var pf in new[] { Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
			{
				if (string.IsNullOrEmpty(pf)) continue;
				string root = pf.TrimEnd('\\') + "\\";
				if (((folder ?? "").TrimEnd('\\') + "\\").StartsWith(root, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		/// <summary>
		/// The program of an older install in %LocalAppData%\DesktopPet goes away (exe, dll, languages, its uninstall
		/// entry); the pets, settings and everything else of the user stay there.
		/// </summary>
		public static void RemoveOldProgram()
		{
			string dir = DataDir;
			foreach (string name in new[] { OldExeName, "DesktopPet_old.exe", ExeName, "DeskPals_old.exe", "uninstall.cmd", "icon.ico" })
				try { File.Delete(Path.Combine(dir, name)); } catch (Exception) { }
			try { foreach (string f in Directory.GetFiles(dir, "*.dll")) File.Delete(f); } catch (Exception) { }
			try { if (Directory.Exists(Path.Combine(dir, "lang"))) Directory.Delete(Path.Combine(dir, "lang"), true); } catch (Exception) { }
			try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch (Exception) { }
		}

		/// <summary>The entry in Windows Settings › Apps (for every user of the computer).</summary>
		public static void Register(string exe, string version, string uninstallCmd, string webpage)
		{
			using (var hk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
			using (var key = hk.CreateSubKey(UninstallKey))
			{
				key.SetValue("DisplayIcon", exe);
				key.SetValue("DisplayName", "DeskPals");
				key.SetValue("DisplayVersion", version ?? "");
				key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
				key.SetValue("InstallLocation", Path.GetDirectoryName(exe));
				key.SetValue("Publisher", "DeskPals");
				key.SetValue("QuietUninstallString", uninstallCmd);
				key.SetValue("UninstallString", uninstallCmd);
				key.SetValue("URLInfoAbout", webpage);
				key.SetValue("URLUpdateInfo", webpage);
				key.SetValue("EstimatedSize", 2500, RegistryValueKind.DWord);
				key.SetValue("NoModify", 1, RegistryValueKind.DWord);
				key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
			}
		}

		/// <summary>
		/// Open the program as the normal user, also from an installer running as administrator (Explorer starts it,
		/// so the pet never runs with administrator rights).
		/// </summary>
		public static void StartAsUser(string exe)
		{
			try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + exe + "\"") { UseShellExecute = false }); }
			catch (Exception) { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exe) }); }
		}
	}

	/// <summary>
	/// The uninstall.cmd written by both installers. It asks for administrator permission (the program is in
	/// Program Files), removes the program, the shortcuts and the entry in Windows Settings, and asks before deleting
	/// the user's pets and settings in %LocalAppData%\DesktopPet (30 seconds without an answer, or a quiet uninstall: kept).
	/// </summary>
	internal static class UninstallScript
	{
		public static string Build(string appName, IEnumerable<string> shortcuts, string uninstallKey, bool pt)
		{
			string ask = pt
				? "Apagar tambem seus pets, lembretes, humor, atalhos e configuracoes? [S]im / [N]ao (em 30 s: Nao) "
				: "Also delete your pets, reminders, mood, shortcuts and settings? [Y]es / [N]o (in 30 s: No) ";
			string bye = pt ? "O DeskPals foi desinstalado." : "DeskPals was uninstalled.";
			string kept = pt ? "Seus pets e configuracoes ficaram guardados: instale de novo e eles voltam." : "Your pets and settings were kept: install again and they come back.";
			var s = new StringBuilder();
			s.AppendLine("@echo off");
			s.AppendLine("setlocal");
			// the program is in Program Files: start again as administrator (Windows asks)
			s.AppendLine("net session >nul 2>&1");
			s.AppendLine("if errorlevel 1 (");
			s.AppendLine("  powershell -NoProfile -WindowStyle Hidden -Command \"Start-Process -FilePath '%~f0' -Verb RunAs\"");
			s.AppendLine("  exit /b");
			s.AppendLine(")");
			s.AppendLine("set \"APPDIR=%~dp0\"");
			s.AppendLine("set \"APPDIR=%APPDIR:~0,-1%\"");
			s.AppendLine("cd /d \"%APPDIR%\\..\"");
			foreach (string p in InstallPlace.ProcessNames) s.AppendLine($"taskkill /IM {p}.exe /f >nul 2>&1");
			// the shortcuts, and the ones an install before the new names left (DesktopPet.url next to DeskPals.url)
			var all = new List<string>();
			foreach (string sc in shortcuts)
			{
				if (!all.Contains(sc)) all.Add(sc);
				if (string.Equals(Path.GetFileName(sc), InstallPlace.ShortcutName, StringComparison.OrdinalIgnoreCase))
				{
					string old = Path.Combine(Path.GetDirectoryName(sc), InstallPlace.OldShortcutName);
					if (!all.Contains(old)) all.Add(old);
				}
			}
			foreach (string sc in all) s.AppendLine($"del \"{sc}\" >nul 2>&1");
			s.AppendLine($"reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v {appName} /f >nul 2>&1");
			s.AppendLine($"reg delete \"HKLM\\{uninstallKey}\" /f /reg:64 >nul 2>&1");
			s.AppendLine($"reg delete \"HKCU\\{uninstallKey}\" /f >nul 2>&1");
			s.AppendLine("echo.");
			// S/Y = yes, N = no; default No after 30 seconds
			s.AppendLine($"choice /C SYN /N /T 30 /D N /M \"{ask}\"");
			s.AppendLine("if errorlevel 3 goto keep");
			s.AppendLine($"rd /s /q \"%LOCALAPPDATA%\\{appName}\" >nul 2>&1");
			s.AppendLine("rd /s /q \"%LOCALAPPDATA%\\eSheep64\" >nul 2>&1");
			s.AppendLine($"echo {bye}");
			s.AppendLine("goto done");
			s.AppendLine(":keep");
			s.AppendLine($"echo {bye}");
			s.AppendLine($"echo {kept}");
			s.AppendLine(":done");
			s.AppendLine("timeout /t 4 >nul");
			// (goto) leaves the script before its own folder (the program's folder) goes away
			s.AppendLine("(goto) 2>nul & rd /s /q \"%APPDIR%\"");
			return s.ToString();
		}
	}
}
