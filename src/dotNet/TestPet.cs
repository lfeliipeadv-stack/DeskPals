using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DesktopPet
{
    /// <summary>
    /// "DesktopPet.exe --test-pet=&lt;animations.xml&gt;": the Pet Editor's "Test on the desktop". A second copy of the
    /// program shows only that pet: it saves nothing (the user's pet and settings stay as they are), starts none of
    /// the extras (reminders, plugins, assistant…), and loads the pet again every time the editor writes the file.
    /// Its tray icon reloads, adds another pet or ends the test.
    /// </summary>
    internal static class TestPet
    {
        /// <summary>This copy is a test of the Pet Editor.</summary>
        public static bool Active { get; private set; }
        static string file;
        static FileSystemWatcher watcher;
        static System.Windows.Forms.Timer later;
        static SynchronizationContext ui;

        public static void Run(string path)
        {
            Active = true;
            LocalData.ReadOnly = true;
            Onboarding.Disabled = true;
            file = Path.GetFullPath(path);
            string xml = Read();
            if (xml == null)
            {
                PetMessage.Show(Lang.T("test.cantRead", file), Lang.T("msg.information"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Use(xml);
            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.DisplayTest(Reload, () => Program.Mainthread?.AddSheep(), () => Application.Exit());
                Program.Mainthread = new StartUp(pi);
                ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
                later = new System.Windows.Forms.Timer { Interval = 350 };
                later.Tick += (s, e) => { later.Stop(); Reload(); };
                watcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true,
                };
                FileSystemEventHandler changed = (s, e) => ui.Post(_ => { later.Stop(); later.Start(); }, null);
                watcher.Changed += changed;
                watcher.Created += changed;
                watcher.Renamed += (s, e) => changed(s, null);
                Application.Run();
                watcher.Dispose();
            }
        }

        /// <summary>The file as the editor left it (it may still be writing it: a few tries).</summary>
        static string Read()
        {
            for (int i = 0; i < 6; i++)
            {
                try { return File.ReadAllText(file); }
                catch (IOException) { Thread.Sleep(120); }
                catch (Exception) { return null; }
            }
            return null;
        }

        static void Use(string xml)
        {
            Program.MyData.SetXml(xml, "");
            Program.MyData.SetImages("");
            Program.MyData.SetIcon("");
        }

        /// <summary>The editor saved a change: the pets of the test come back with it.</summary>
        static void Reload()
        {
            string xml = Read();
            if (xml == null || XmlData.AnimationXML.ParseXML(xml) == null) return;   // half written: the next change brings it
            Use(xml);
            Program.Mainthread?.LoadNewXMLFromString(xml);
        }
    }
}
