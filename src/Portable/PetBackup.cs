using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace DesktopPet
{
    /// <summary>
    /// "My pets" in one file, to take them to another computer: a zip with
    /// pets/&lt;folder&gt;/animations.xml and version.txt for every downloaded pet,
    /// plus the pet in use and the pets with sound off (settings.txt).
    /// </summary>
    internal static class PetBackup
    {
        public const string Extension = ".desktoppet";

        // ------------------------------------------------------------------ automatic copy in a folder

        /// <summary>
        /// The folder of the automatic copy (empty: off). A folder of OneDrive or Google Drive takes everything to the
        /// other computer, where "Import" with this file brings it back.
        /// </summary>
        public static string AutoFolder
        {
            get => Program.MyData.GetSetting("BackupFolder");
            set { Program.MyData.SetSetting("BackupFolder", value ?? ""); if (!string.IsNullOrEmpty(value)) RunAuto(); }
        }

        /// <summary>The file of the automatic copy (the same name in every language, to be found on the other computer).</summary>
        public static string AutoFile => Path.Combine(AutoFolder, "DesktopPet-backup" + Extension);

        /// <summary>When the last automatic copy was written (MinValue: never).</summary>
        public static DateTime LastAuto =>
            long.TryParse(Program.MyData.GetSetting("BackupLast"), out long t) ? new DateTime(t) : DateTime.MinValue;

        static System.Windows.Forms.Timer autoTimer;

        /// <summary>A copy a couple of minutes after opening and then every few hours, once a day at most.</summary>
        public static void StartAuto()
        {
            if (autoTimer != null || SelfTest.Running) return;
            autoTimer = new System.Windows.Forms.Timer { Interval = 2 * 60 * 1000 };
            autoTimer.Tick += (s, e) =>
            {
                autoTimer.Interval = 3 * 60 * 60 * 1000;
                if (AutoFolder.Length > 0 && DateTime.Now - LastAuto > TimeSpan.FromHours(20)) RunAuto();
            };
            autoTimer.Start();
        }

        /// <summary>Write the automatic copy now. False (and a line in the log) if the folder is missing or can't be written.</summary>
        public static bool RunAuto()
        {
            try
            {
                if (AutoFolder.Length == 0 || !Directory.Exists(AutoFolder)) return false;
                Export(AutoFile);
                Program.MyData.SetSetting("BackupLast", DateTime.Now.Ticks.ToString());
                return true;
            }
            catch (Exception ex) { StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "Automatic backup: " + ex.Message); return false; }
        }

        /// <summary>Write every downloaded pet into <paramref name="file"/>. Returns how many pets were saved.</summary>
        public static int Export(string file)
        {
            AppTime.Flush();   // the minutes counted since the last save go too
            var folders = PetStore.Folders();
            string tmp = file + ".tmp";
            using (var zip = ZipFile.Open(tmp, ZipArchiveMode.Create))
            {
                foreach (var folder in folders)
                {
                    Add(zip, "pets/" + folder + "/animations.xml", PetStore.Load(folder));
                    Add(zip, "pets/" + folder + "/version.txt", PetStore.SavedVersion(folder));
                }
                var settings = new StringBuilder();
                settings.AppendLine("current=" + Program.MyData.GetCurrentPetFolder());
                settings.AppendLine("muted=" + string.Join(";", new[] { "esheep64" }.Concat(folders).Where(f => Program.MyData.IsPetMuted(f))));
                // the rest of "my things": every preference (escaped, one per line), except secrets and this computer's
                foreach (var key in Program.MyData.SettingKeys().Where(Carried).OrderBy(k => k, StringComparer.Ordinal))
                {
                    string v = Program.MyData.GetSetting(key, null);
                    if (v != null) settings.AppendLine("setx." + key + "=" + v.Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n"));
                }
                Add(zip, "settings.txt", settings.ToString());
                // reminders, shortcuts, notes, mood history, time in each program and the needs of the virtual pet
                foreach (var (name, path, _) in DataFiles)
                    if (File.Exists(path)) Add(zip, "data/" + name, File.ReadAllText(path));
            }
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
            return folders.Count;
        }

        /// <summary>
        /// Settings that never go in a backup: keys and tokens (the AI key is also locked to this Windows account),
        /// the pet data kept in the settings (the pets travel as files), and what only means something on this computer.
        /// </summary>
        static readonly HashSet<string> NotCarried = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xml", "Images", "Icon", "CurrentPetFolder", "MutedPets",
            "AiKey", "AiProfiles", "AiProfilesDone", "AiActive", "AgentToken", "AgentPort", "LocalApi",
            "UpdateChecked", "UpdateTold", "UpdateBannerHidden", "LastVersion",
            "BackupFolder", "BackupLast",
        };

        /// <summary>True for a setting the backup carries (and the only ones a restore writes).</summary>
        internal static bool Carried(string key) =>
            !string.IsNullOrEmpty(key) && key.Length <= 80 && !NotCarried.Contains(key)
            && key.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
            && !new[] { "Token", "Secret", "Password", "ApiKey" }.Any(s => key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);

        static string Unescape(string v)
        {
            var sb = new StringBuilder(v.Length);
            for (int i = 0; i < v.Length; i++)
            {
                if (v[i] == '\\' && i + 1 < v.Length) { i++; sb.Append(v[i] == 'n' ? '\n' : v[i]); }
                else sb.Append(v[i]);
            }
            return sb.ToString();
        }

        static bool IsJson(string t) { try { using (System.Text.Json.JsonDocument.Parse(t)) return true; } catch (Exception) { return false; } }

        /// <summary>(name in the zip, file on disk, check that it is valid before writing it)</summary>
        static (string name, string path, Func<string, bool> valid)[] DataFiles => new (string, string, Func<string, bool>)[]
        {
            ("reminders.json", Reminders.FilePath, t => JsonText.Deserialize<List<Reminder>>(t) != null),
            ("launchers.json", Launchers.FilePath, t => JsonText.Deserialize<List<Launcher>>(t) != null),
            ("mood.json", Mood.FilePath, t => JsonText.Deserialize<Dictionary<string, int>>(t) != null),
            ("needs.json", VirtualPet.FilePath, t => JsonText.Deserialize<Dictionary<string, PetNeeds>>(t) != null),
            ("notes.json", Notes.FilePath, IsJson),
            ("apptime.json", AppTime.FilePath, t => JsonText.Deserialize<Dictionary<string, Dictionary<string, int>>>(t) != null),
        };

        /// <summary>True if the file also has reminders, shortcuts or settings (a backup of version 2.5 or newer).</summary>
        public static bool HasMyThings(string file)
        {
            try
            {
                using (var zip = ZipFile.OpenRead(file))
                    return zip.Entries.Any(e => e.FullName.StartsWith("data/")) || (zip.GetEntry("settings.txt") is ZipArchiveEntry s && (Read(s).Contains("set.") || Read(s).Contains("setx.")));
            }
            catch (Exception) { return false; }
        }

        /// <summary>Bring back reminders, shortcuts, mood, needs and settings. Returns how many items were restored.</summary>
        public static int RestoreMyThings(string file, bool dataFiles = true)
        {
            int n = 0;
            using (var zip = ZipFile.OpenRead(file))
            {
                foreach (var (name, path, valid) in dataFiles ? DataFiles : Array.Empty<(string, string, Func<string, bool>)>())
                {
                    var e = zip.GetEntry("data/" + name);
                    if (e == null || e.Length > 10 * 1024 * 1024) continue;
                    string text = Read(e);
                    bool ok;
                    try { ok = valid(text); } catch (Exception) { ok = false; }
                    if (!ok) continue;   // a broken file never replaces the good one
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, text, new UTF8Encoding(false));
                    n++;
                }
                if (zip.GetEntry("settings.txt") is ZipArchiveEntry s)
                    foreach (var line in Read(s).Split('\n').Select(l => l.TrimEnd('\r')))
                    {
                        // "setx.key=escaped" (2.9 and newer: every preference) or "set.key=value" (older backups)
                        bool escaped = line.StartsWith("setx.");
                        if (!escaped && !line.StartsWith("set.")) continue;
                        int eq = line.IndexOf('=');
                        int start = escaped ? 5 : 4;
                        if (eq < start) continue;
                        string key = line.Substring(start, eq - start);
                        if (!Carried(key)) continue;   // never a secret or this computer's
                        string value = line.Substring(eq + 1);
                        Program.MyData.SetSetting(key, escaped ? Unescape(value) : value);
                        n++;
                    }
            }
            if (dataFiles) { Reminders.Reload(); Launchers.Reload(); Mood.Reload(); VirtualPet.Reload(); Notes.Reload(); AppTime.Reload(); }
            return n;
        }

        static void Add(ZipArchive zip, string name, string text)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var w = new StreamWriter(entry.Open(), new UTF8Encoding(false))) w.Write(text ?? "");
        }

        /// <summary>
        /// Read a file made by <see cref="Export"/>: every valid pet goes to this computer (a pet already here is
        /// replaced). Returns the pets imported and the folder of the pet that was in use on the other computer.
        /// </summary>
        public static (List<string> imported, string current) Import(string file)
        {
            var imported = new List<string>();
            string current = "";
            using (var zip = ZipFile.OpenRead(file))
            {
                foreach (var entry in zip.Entries)
                {
                    // only "pets/<plain name>/animations.xml": nothing else is ever written to the disk
                    var parts = entry.FullName.Replace('\\', '/').Split('/');
                    if (parts.Length != 3 || parts[0] != "pets" || parts[2] != "animations.xml") continue;
                    string folder = parts[1];
                    if (folder.Length == 0 || folder.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-'))) continue;
                    if (entry.Length > 50 * 1024 * 1024) continue;
                    string xml = Read(entry);
                    try
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(xml);
                        if (doc.DocumentElement?.SelectSingleNode("*[local-name()='image']") == null) continue;
                    }
                    catch (Exception) { continue; }   // not a pet: skipped
                    var ver = zip.GetEntry("pets/" + folder + "/version.txt");
                    PetStore.Save(folder, xml, ver != null ? Read(ver).Trim() : "");
                    imported.Add(folder);
                }
                var settings = zip.GetEntry("settings.txt");
                if (settings != null)
                {
                    foreach (var line in Read(settings).Split('\n').Select(l => l.Trim()))
                    {
                        if (line.StartsWith("current=")) current = line.Substring(8);
                        if (line.StartsWith("muted="))
                            foreach (var f in line.Substring(6).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                if (f == "esheep64" || imported.Contains(f)) Program.MyData.SetPetMuted(f, true);
                    }
                }
            }
            return (imported, current);
        }

        static string Read(ZipArchiveEntry entry)
        {
            using (var r = new StreamReader(entry.Open(), Encoding.UTF8, true)) return r.ReadToEnd();
        }
    }
}
