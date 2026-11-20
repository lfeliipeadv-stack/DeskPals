using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
    /// <summary>
    /// Updates the application from the releases of this repository (<see cref="AppInfo.LatestReleaseApi"/>).
    /// <para>
    /// A release must contain <c>DesktopPet.exe</c>, optionally <c>lang.zip</c>, and <c>SHA256SUMS</c>
    /// ("hash  file name" per line). The tag is the version ("v2.1.0"). Only a <b>higher</b> version is offered,
    /// and every file is checked against its SHA-256 before anything is replaced.
    /// Tools\release.ps1 builds such a release.
    /// </para>
    /// </summary>
    internal static class Updater
    {
#if SELFCONTAINED
        // the "full" exe (no .NET needed) updates to the full exe, never to the one that needs .NET
        private const string ExeAsset = "DesktopPet-completo.exe";
#else
        private const string ExeAsset = "DesktopPet.exe";
#endif
        private const string LangAsset = "lang.zip";
        private const string SumsAsset = "SHA256SUMS";

        private static readonly HttpClient http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DesktopPet-Updater");
            return client;
        }

        /// <summary>Information about the latest published release.</summary>
        public sealed class Release
        {
            public Version Version;
            public string Notes = "";
            public Dictionary<string, string> Assets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Version of the running application (major.minor.build).</summary>
        public static Version CurrentVersion
        {
            get
            {
                var v = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
                return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
            }
        }

        /// <summary>"v2.1.0", "2.1", "release-2.1.0" → 2.1.0. Returns null if there is no version in the text.</summary>
        public static Version ParseTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            var digits = new string(tag.SkipWhile(c => !char.IsDigit(c)).TakeWhile(c => char.IsDigit(c) || c == '.').ToArray()).Trim('.');
            if (!Version.TryParse(digits.Contains('.') ? digits : digits + ".0", out var v)) return null;
            return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
        }

        /// <summary>Read a release as returned by the GitHub API.</summary>
        public static Release ParseRelease(string json)
        {
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                var release = new Release
                {
                    Version = ParseTag(root.GetProperty("tag_name").GetString()),
                    Notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
                };
                if (root.TryGetProperty("assets", out var assets))
                    foreach (var asset in assets.EnumerateArray())
                        release.Assets[asset.GetProperty("name").GetString()] = asset.GetProperty("browser_download_url").GetString();
                return release.Version == null ? null : release;
            }
        }

        /// <summary>Latest release of this repository, or null if nothing was published yet.</summary>
        public static async Task<Release> GetLatestAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, AppInfo.LatestReleaseApi))
            {
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using (var response = await http.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                    response.EnsureSuccessStatusCode();
                    return ParseRelease(await response.Content.ReadAsStringAsync());
                }
            }
        }

        /// <summary>Parse a SHA256SUMS file ("hash  name" or "hash *name" per line).</summary>
        public static Dictionary<string, string> ParseSums(string text)
        {
            var sums = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length < 66) continue;
                var hash = line.Substring(0, 64);
                var name = line.Substring(64).Trim().TrimStart('*');
                if (hash.All(Uri.IsHexDigit) && name.Length > 0) sums[name] = hash.ToLowerInvariant();
            }
            return sums;
        }

        /// <summary>SHA-256 of a byte array as lowercase hex.</summary>
        public static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

        /// <summary>
        /// Check for an update and, if the user agrees, install it and restart.
        /// Errors are shown only when <paramref name="silent"/> is false.
        /// </summary>
        public static async Task CheckAndInstallAsync(IWin32Window owner, bool silent)
        {
            try
            {
                var release = await GetLatestAsync();
                if (release == null || release.Version <= CurrentVersion)
                {
                    if (!silent) PetMessage.Show(owner, Lang.T("upd.upToDate", CurrentVersion.ToString(3)));
                    return;
                }
                if (!release.Assets.ContainsKey(ExeAsset) || !release.Assets.ContainsKey(SumsAsset))
                    throw new InvalidOperationException(Lang.T("inst.incompleteRelease", release.Version));

                string notes = release.Notes.Length > 1500 ? release.Notes.Substring(0, 1500) + "…" : release.Notes;
                if (PetMessage.Show(owner, Lang.T("inst.newVersion", release.Version, notes),
                        Lang.T("inst.versionTitle", "DesktopPet", CurrentVersion),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                await InstallAsync(release, owner);
            }
            catch (Exception ex)
            {
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "Update failed: " + ex.Message);
                if (!silent) PetMessage.Show(owner, Lang.T("inst.updateFailed", ex.Message));
            }
        }

        // ------------------------------------------------------------------ automatic updates

        /// <summary>Look for a new version by itself (when the program opens, then once a day). On by default.</summary>
        public static bool Auto
        {
            get => Program.MyData.GetSettingBool("AutoUpdate", true);
            set { Program.MyData.SetSetting("AutoUpdate", value); StartAuto(); }
        }

        /// <summary>A newer version found by the automatic check (null = none, or not checked yet).</summary>
        public static Release Available { get; internal set; }

        /// <summary>When the last check happened (the start page shows it).</summary>
        public static DateTime LastCheck
        {
            get => long.TryParse(Program.MyData.GetSetting("UpdateChecked"), out long t) ? new DateTime(t) : DateTime.MinValue;
            private set => Program.MyData.SetSetting("UpdateChecked", value.Ticks.ToString());
        }

        static Timer autoTimer;

        /// <summary>Start (or stop) the automatic check: soon after opening (always, so the start page can show a new version), then every few hours (once a day at most).</summary>
        public static void StartAuto()
        {
            if (autoTimer == null)
            {
                autoTimer = new Timer { Interval = 15 * 1000 };
                autoTimer.Tick += async (s, e) => { autoTimer.Interval = 3 * 60 * 60 * 1000; await AutoCheckAsync(); };
                if (!SelfTest.Running)
                {
                    LoadStaged();
                    // a version downloaded in the background is installed when the program closes
                    Application.ApplicationExit += (s, e) => { if (Silent && Staged != null && !installing && !SelfTest.Running) ApplyStaged(restart: false); };
                }
            }
            if (Auto && !SelfTest.Running) autoTimer.Start(); else autoTimer.Stop();
        }

        // ------------------------------------------------------------------ in the background, installed on close

        /// <summary>Download a new version by itself and install it when the program closes (on by default).</summary>
        public static bool Silent
        {
            get => Program.MyData.GetSettingBool("UpdateSilent", true);
            set => Program.MyData.SetSetting("UpdateSilent", value);
        }

        /// <summary>A version already downloaded and checked, waiting for the program to close (null: none).</summary>
        public static Release Staged { get; internal set; }

        /// <summary>Set while the program closes to install (the close must not install a second time).</summary>
        static bool installing;

        static string StageDir => Path.Combine(InstallPlace.DataDir, "update");

        /// <summary>The notes of the version being installed, shown once by the new version ("What's new").</summary>
        public static string WhatsNewFile => Path.Combine(InstallPlace.DataDir, "whatsnew.txt");

        static void SaveWhatsNew(Release release)
        {
            try { Directory.CreateDirectory(InstallPlace.DataDir); File.WriteAllText(WhatsNewFile, release.Version.ToString(3) + "\n" + (release.Notes ?? "")); }
            catch (Exception skipped) { LogFile.Ignored(skipped); }
        }

        /// <summary>The notes saved for <paramref name="version"/> by the update that installed it (then forgotten), or null.</summary>
        public static string TakeWhatsNew(string version)
        {
            try
            {
                if (!File.Exists(WhatsNewFile)) return null;
                string text = File.ReadAllText(WhatsNewFile);
                File.Delete(WhatsNewFile);
                int nl = text.IndexOf('\n');
                if (nl < 0 || text.Substring(0, nl).Trim() != version) return null;
                string notes = text.Substring(nl + 1).Trim();
                return notes.Length == 0 ? null : notes;
            }
            catch (Exception skipped) { LogFile.Ignored(skipped); return null; }
        }

        /// <summary>A version downloaded before the program closed last time (still newer than this one).</summary>
        static void LoadStaged()
        {
            try
            {
                string versionFile = Path.Combine(StageDir, "version.txt");
                if (!File.Exists(versionFile)) return;
                var v = ParseTag(File.ReadAllText(versionFile));
                if (v == null || v <= CurrentVersion || !File.Exists(Path.Combine(StageDir, "DesktopPet.exe"))) { Directory.Delete(StageDir, true); return; }
                string notes = Path.Combine(StageDir, "notes.txt");
                Staged = new Release { Version = v, Notes = File.Exists(notes) ? File.ReadAllText(notes) : "" };
            }
            catch (Exception skipped) { LogFile.Ignored(skipped); }
        }

        /// <summary>Download the release and check every hash, into the user's folder (nothing of the program is touched).</summary>
        static async Task StageAsync(Release release)
        {
            if (Staged != null && Staged.Version == release.Version) return;
            var sums = ParseSums(await http.GetStringAsync(release.Assets[SumsAsset]));
            byte[] exe = await DownloadVerifiedAsync(release, ExeAsset, sums);
            byte[] lang = release.Assets.ContainsKey(LangAsset) ? await DownloadVerifiedAsync(release, LangAsset, sums) : null;
            string tmp = StageDir + ".tmp";
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            Directory.CreateDirectory(tmp);
            File.WriteAllBytes(Path.Combine(tmp, "DesktopPet.exe"), exe);
            if (lang != null) File.WriteAllBytes(Path.Combine(tmp, "lang.zip"), lang);
            File.WriteAllText(Path.Combine(tmp, "hashes.txt"), "exe=" + Sha256(exe) + "\nlang=" + (lang == null ? "" : Sha256(lang)) + "\n");
            File.WriteAllText(Path.Combine(tmp, "notes.txt"), release.Notes ?? "");
            File.WriteAllText(Path.Combine(tmp, "version.txt"), release.Version.ToString(3));
            if (Directory.Exists(StageDir)) Directory.Delete(StageDir, true);
            Directory.Move(tmp, StageDir);
            Staged = release;
        }

        /// <summary>
        /// Install the downloaded version: its files are checked again, then they replace the program (Program Files:
        /// Windows asks for permission). With <paramref name="restart"/> the new version opens. False if nothing changed.
        /// </summary>
        public static bool ApplyStaged(bool restart)
        {
            var staged = Staged;
            if (staged == null) return false;
            try
            {
                var hashes = File.ReadAllLines(Path.Combine(StageDir, "hashes.txt")).Select(l => l.Split('=')).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
                byte[] exe = File.ReadAllBytes(Path.Combine(StageDir, "DesktopPet.exe"));
                if (Sha256(exe) != hashes["exe"]) throw new InvalidOperationException(Lang.T("inst.hashMismatch", ExeAsset));
                string langFile = Path.Combine(StageDir, "lang.zip");
                byte[] lang = File.Exists(langFile) ? File.ReadAllBytes(langFile) : null;
                if (lang != null && Sha256(lang) != hashes["lang"]) lang = null;   // the new exe still has its built-in English
                SaveWhatsNew(staged);
                string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                if (InstallPlace.InProgramFiles(Path.GetDirectoryName(exePath)) && !InstallPlace.IsAdmin())
                    ReplaceAsAdmin(exePath, exe, lang, restart);
                else
                {
                    ReplaceFiles(exePath, exe, lang);
                    if (restart) Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                }
                Staged = null;
                try { Directory.Delete(StageDir, true); } catch (Exception skipped) { LogFile.Ignored(skipped); }
                return true;
            }
            catch (Exception ex)
            {
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "Update on close failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>"Restart and install" (the start page and the pet's notice).</summary>
        public static void RestartToInstall()
        {
            if (!ApplyStaged(restart: true)) { PetMessage.Show((IWin32Window)null, Lang.T("inst.updateFailed", "")); return; }
            installing = true;
            Application.Exit();
        }

        /// <summary>The first check after opening ignores the once-a-day limit (a version found yesterday is remembered only in memory).</summary>
        static bool checkedThisRun;

        static async Task AutoCheckAsync()
        {
            if (!Auto || (checkedThisRun && DateTime.Now - LastCheck < TimeSpan.FromHours(20))) return;
            checkedThisRun = true;
            try
            {
                var release = await GetLatestAsync();
                LastCheck = DateTime.Now;
                if (release == null || release.Version <= CurrentVersion || !release.Assets.ContainsKey(ExeAsset) || !release.Assets.ContainsKey(SumsAsset)) return;
                Available = release;
                // in the background: downloaded now, installed when the program closes
                if (Silent)
                {
                    try { await StageAsync(release); }
                    catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.info, "Update download: " + ex.Message); }
                }
                // the pet tells it once per version (notices wait during focus or "do not disturb")
                if (Program.MyData.GetSetting("UpdateTold") == release.Version.ToString()) return;
                Program.MyData.SetSetting("UpdateTold", release.Version.ToString());
                if (Staged != null)
                {
                    SpeechBubble.Notify(Lang.T("upd.staged", release.Version.ToString(3)), 0,
                        new SpeechBubble.Choice(Lang.T("upd.restartNow"), RestartToInstall, true),
                        new SpeechBubble.Choice("OK", null));
                    return;
                }
                SpeechBubble.Notify(Lang.T("upd.available", release.Version.ToString(3)), 0,
                    new SpeechBubble.Choice(Lang.T("upd.install"), () => _ = InstallAvailableAsync(), true),
                    new SpeechBubble.Choice(Lang.T("vpet.later"), null));
            }
            catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.info, "Update check: " + ex.Message); }
        }

        /// <summary>Install the version found by the automatic check (the start page and the pet's notice).</summary>
        public static async Task InstallAvailableAsync()
        {
            if (Available == null) return;
            try
            {
                SpeechBubble.Say(Lang.T("upd.downloading"), 0);
                await InstallAsync(Available, null);
            }
            catch (Exception ex)
            {
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "Update failed: " + ex.Message);
                PetMessage.Show((IWin32Window)null, Lang.T("inst.updateFailed", ex.Message));
            }
        }

        /// <summary>Download, check every hash, replace the exe (and the languages) and restart.</summary>
        private static async Task InstallAsync(Release release, IWin32Window owner)
        {
            string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            string appDir = Path.GetDirectoryName(exePath);
            string oldExe = Path.Combine(appDir, "DesktopPet_old.exe");
            try { if (File.Exists(oldExe)) File.Delete(oldExe); } catch (Exception skipped) { LogFile.Ignored(skipped); }   // leftover of the previous update
            {
                // download everything and check every hash first: nothing is touched if one file is wrong
                var sums = ParseSums(await http.GetStringAsync(release.Assets[SumsAsset]));
                byte[] exe = await DownloadVerifiedAsync(release, ExeAsset, sums);
                byte[] lang = release.Assets.ContainsKey(LangAsset) ? await DownloadVerifiedAsync(release, LangAsset, sums) : null;

                SaveWhatsNew(release);   // the new version shows its notes once
                installing = true;
                Staged = null;
                try { if (Directory.Exists(StageDir)) Directory.Delete(StageDir, true); } catch (Exception skipped) { LogFile.Ignored(skipped); }

                // installed in Program Files: Windows asks for permission, then an administrator PowerShell replaces the files
                if (InstallPlace.InProgramFiles(appDir) && !InstallPlace.IsAdmin())
                {
                    try { ReplaceAsAdmin(exePath, exe, lang, restart: true); }
                    catch { installing = false; throw; }
                    Application.Exit();
                    return;
                }

                try { ReplaceFiles(exePath, exe, lang); }
                catch { installing = false; throw; }
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                PetMessage.Show(owner, Lang.T("inst.updated"));
                Application.Exit();
            }
        }

        /// <summary>Replace the exe of a program not in Program Files (a running exe can't be overwritten, but it can be renamed) and the languages.</summary>
        private static void ReplaceFiles(string exePath, byte[] exe, byte[] lang)
        {
            string appDir = Path.GetDirectoryName(exePath);
            string oldExe = Path.Combine(appDir, "DesktopPet_old.exe");
            try { if (File.Exists(oldExe)) File.Delete(oldExe); } catch (Exception skipped) { LogFile.Ignored(skipped); }
            File.Move(exePath, oldExe, true);
            try
            {
                File.WriteAllBytes(exePath, exe);
            }
            catch
            {
                File.Move(oldExe, exePath, true);
                throw;
            }

            if (lang != null)
            {
                try
                {
                    using (var zip = new ZipArchive(new MemoryStream(lang)))
                        zip.ExtractToDirectory(Path.Combine(appDir, "lang"), true);
                }
                catch (Exception skipped) { LogFile.Ignored(skipped); }   // the new exe still works with its built-in English
            }
        }

        /// <summary>
        /// Replace the program in Program Files as administrator, after this copy closes. The new files wait in the
        /// user's temp folder; the administrator step checks their SHA-256 again (taken from the files already checked
        /// here) before writing them, and its commands go on the command line, not in a file anyone could change.
        /// Then Explorer opens the new version as the normal user.
        /// </summary>
        private static void ReplaceAsAdmin(string exePath, byte[] exe, byte[] lang, bool restart)
        {
            string dir = Path.Combine(Path.GetTempPath(), "DesktopPet-update");
            Directory.CreateDirectory(dir);
            string newExe = Path.Combine(dir, "DesktopPet.exe"), newLang = Path.Combine(dir, "lang.zip");
            File.WriteAllBytes(newExe, exe);
            if (lang != null) File.WriteAllBytes(newLang, lang);
            string Q(string s) => "'" + s.Replace("'", "''") + "'";
            string appDir = Path.GetDirectoryName(exePath);
            string script = string.Join("\n", new[]
            {
                "$ErrorActionPreference = 'Stop'",
                $"try {{ Wait-Process -Id {Environment.ProcessId} -Timeout 30 -ErrorAction SilentlyContinue }} catch {{}}",
                "function Check($file, $hash) { $b = [IO.File]::ReadAllBytes($file); $h = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($b)).Replace('-', ''); if ($h -ne $hash) { exit 2 }; return ,$b }",
                $"$exe = {Q(exePath)}; $old = {Q(Path.Combine(appDir, "DesktopPet_old.exe"))}",
                $"$bytes = Check {Q(newExe)} '{Sha256(exe).ToUpperInvariant()}'",
                "if (Test-Path $old) { Remove-Item $old -Force }",
                "Move-Item $exe $old -Force",
                "try { [IO.File]::WriteAllBytes($exe, $bytes) } catch { Move-Item $old $exe -Force; exit 3 }",
                lang == null ? "" :
                $"try {{ $z = Check {Q(newLang)} '{Sha256(lang).ToUpperInvariant()}'; $zip = {Q(Path.Combine(appDir, "lang.zip"))}; [IO.File]::WriteAllBytes($zip, $z); Expand-Archive $zip -DestinationPath {Q(Path.Combine(appDir, "lang"))} -Force; Remove-Item $zip -Force }} catch {{}}",
                $"Remove-Item {Q(dir)} -Recurse -Force -ErrorAction SilentlyContinue",
                restart ? "Start-Process explorer.exe -ArgumentList ('\"' + $exe + '\"')" : "",
            });
            string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            try
            {
                Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encoded) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // "No" in the Windows question: nothing changed
                throw new InvalidOperationException(Lang.T("inst.adminCancelled"));
            }
        }

        /// <summary>Asset of the Pet Editor in the releases (a separate program, downloaded the first time it is opened).</summary>
        public const string EditorAsset = "PetEditor.exe";

        /// <summary>Where a downloaded Pet Editor is kept (the user's folder: Program Files is read-only).</summary>
        public static string EditorPath => Path.Combine(InstallPlace.DataDir, EditorAsset);

        /// <summary>
        /// The Pet Editor of the latest release, downloaded (and checked against SHA256SUMS) into the user's folder.
        /// It is downloaded again when this DesktopPet is newer than the release it came with. Returns its path.
        /// </summary>
        public static async Task<string> GetEditorAsync()
        {
            string versionFile = EditorPath + ".version";
            string have = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "";
            if (File.Exists(EditorPath) && ParseTag(have) is Version v && v >= CurrentVersion) return EditorPath;
            try
            {
                var release = await GetLatestAsync();
                if (release == null || !release.Assets.ContainsKey(EditorAsset) || !release.Assets.ContainsKey(SumsAsset))
                    return File.Exists(EditorPath) ? EditorPath : null;
                var sums = ParseSums(await http.GetStringAsync(release.Assets[SumsAsset]));
                byte[] exe = await DownloadVerifiedAsync(release, EditorAsset, sums);
                Directory.CreateDirectory(InstallPlace.DataDir);
                File.WriteAllBytes(EditorPath, exe);
                File.WriteAllText(versionFile, release.Version.ToString());
                return EditorPath;
            }
            catch (Exception) when (File.Exists(EditorPath))
            {
                return EditorPath;   // offline: the copy already here still works
            }
        }

        private static async Task<byte[]> DownloadVerifiedAsync(Release release, string name, Dictionary<string, string> sums)
        {
            if (!sums.TryGetValue(name, out var expected))
                throw new InvalidOperationException(Lang.T("inst.hashMissing", name));
            byte[] data = await http.GetByteArrayAsync(release.Assets[name]);
            if (Sha256(data) != expected)
                throw new InvalidOperationException(Lang.T("inst.hashMismatch", name));
            return data;
        }
    }
}
