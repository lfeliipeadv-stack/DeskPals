using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopPet
{
    /// <summary>
    /// "Start with Windows": an entry in HKCU\...\Run pointing to this exe (no admin rights needed).
    /// The installer used to put a DesktopPet.url (DeskPals.url from 1.2.0) in the Startup folder instead; it counts as "on" too,
    /// and is removed when the option is turned off, so the program never starts twice.
    /// </summary>
    internal static class WindowsStartup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "DesktopPet";

        static string[] StartupShortcuts => new[] { InstallPlace.ShortcutName, InstallPlace.OldShortcutName }.Select(n => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), n)).ToArray();

        public static bool Enabled
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                        if (key?.GetValue(ValueName) is string s && s.Length > 0) return true;
                    return StartupShortcuts.Any(File.Exists);
                }
                catch (Exception) { return false; }
            }
        }

        /// <summary>Turn it on or off. Returns false (and leaves things as they were) if Windows refused.</summary>
        public static bool Set(bool on)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (on) key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
                    else key.DeleteValue(ValueName, false);
                }
                // one way only: the old shortcut would start a second copy
                foreach (string sc in StartupShortcuts) if (File.Exists(sc)) File.Delete(sc);
                return true;
            }
            catch (Exception ex)
            {
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.error, "start with Windows: " + ex.Message);
                return false;
            }
        }

        /// <summary>The exe moved (e.g. installed after the option was turned on): point the entry to this copy.</summary>
        public static void Refresh()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (!(key?.GetValue(ValueName) is string s) || s.Length == 0) return;
                    string want = "\"" + Application.ExecutablePath + "\"";
                    if (s != want && Program.IsApplicationInstalled()) key.SetValue(ValueName, want);
                }
            }
            catch (Exception skipped) { LogFile.Ignored(skipped); }
        }
    }
}
