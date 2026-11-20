using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DesktopPet
{
    /// <summary>
    /// Main for the application. Once the application is started, this class will create all objects.
    /// </summary>
    class Program
    {
        /// <summary>
        /// StartUp is the main program.
        /// </summary>
        public static StartUp Mainthread;


        /// <summary>
        /// Mutual Exclusion, to allow only 1 instance of this application.
        /// </summary>
        /// <remarks>
        /// Mutex can be made static so that GC doesn't recycle same effect with GC.KeepAlive(mutex) at the end of main
        /// </remarks>
        static readonly Mutex mutex = new Mutex(false, "eSheep_Running");

        /// <summary>
        /// Second Mutual Exclusion, to allow 2 instances of this application.
        /// </summary>
        static readonly Mutex mutex2 = new Mutex(false, "eSheep_Running2");

		public static LocalData MyData = new LocalData();


        /// <summary>
        /// Argument: load local animation XML.
        /// </summary>
        public static string ArgumentLocalXML = "";

        /// <summary>
        /// Argument: load animation XML from web.
        /// </summary>
        public static string ArgumentWebXML = "";

        /// <summary>
        /// Argument: open the installer when application starts.
        /// </summary>
        public static string ArgumentInstall = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // "DesktopPet.exe --mcp": MCP server on stdin/stdout for Claude Code (no window, no pet; talks to the running one)
            if (args.Length > 0 && args[0] == "--mcp")
            {
                var stdin = new StreamReader(Console.OpenStandardInput(), new System.Text.UTF8Encoding(false));
                var stdout = new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false));
                Environment.Exit(McpServer.Run(stdin, stdout));
                return;
            }
            // "DesktopPet.exe --agent=done": another AI agent (Codex, Cursor, a script) tells the running pet what it is doing
            if (args.Length > 0 && args[0].StartsWith("--agent=", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(OtherAgents.Run(args));
                return;
            }
            int iMutexIndex = 0;

            // Modern look: visual styles, per-monitor DPI and GDI+ text rendering (set in the csproj).
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.error, "Unhandled: " + e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogFile.Important("CRASH " + e.ExceptionObject);
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.error, "Background task: " + e.Exception?.GetBaseException());
                e.SetObserved();
            };
            LogFile.Important($"start {Application.ProductVersion} on {Environment.OSVersion}, args: {string.Join(" ", args)}");

            // Interface language: saved preference, or the Windows language ("auto").
            Lang.Use(MyData.GetLanguage());
            // Color theme: saved preference, or the Windows setting ("auto").
            Theme.Mode = MyData.GetTheme();
            Theme.AccentId = MyData.GetSetting("Accent", "green");

            // Developer helpers: write the English template / check that a translation has every key.
            foreach (string s in args)
            {
                if (s.StartsWith("--export-lang=", StringComparison.OrdinalIgnoreCase))
                {
                    Lang.ExportEnglish(s.Substring("--export-lang=".Length));
                    return;
                }
                // installer: this copy was started as administrator only to copy the program to Program Files (see Install)
                if (s.StartsWith("--install-program=", StringComparison.OrdinalIgnoreCase))
                {
                    string error = Install.InstallToProgramFiles(Environment.ProcessPath ?? Application.ExecutablePath, s.Substring("--install-program=".Length));
                    Environment.Exit(error == null ? 0 : 1);
                    return;
                }
                // memory plugin: this copy was started as administrator only to do the full tidy (see MemoryCare)
                if (s.StartsWith("--clean-memory=", StringComparison.OrdinalIgnoreCase))
                {
                    MemoryCare.FullCleanToFile(s.Substring("--clean-memory=".Length));
                    return;
                }
                // developer: listen for Claude Code without changing the saved setting (events noted in %TEMP%\desktoppet-agent.log)
                if (s.Equals("--listen", StringComparison.OrdinalIgnoreCase)) AgentBridge.DevListen = true;
                // developer: write the hooks into another settings file (claude --settings <file>), not the user's one
                if (s.StartsWith("--write-hooks=", StringComparison.OrdinalIgnoreCase))
                {
                    ClaudeHooks.Install(s.Substring("--write-hooks=".Length));
                    return;
                }
            }

            // Pet Editor: "--test-pet=<animations.xml>" shows only that pet, in a copy of the program that saves nothing
            // (it does not take the single-instance lock: the user's own DesktopPet keeps running)
            foreach (string s in args)
            {
                if (s.StartsWith("--test-pet=", StringComparison.OrdinalIgnoreCase))
                {
                    TestPet.Run(s.Substring("--test-pet=".Length).Trim('"'));
                    return;
                }
            }

            // if you like to wait a few seconds in case that the instance is just
            // shutting down
            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                {
                    iMutexIndex = 1;
                    try
                    {
                        if (!mutex2.WaitOne(TimeSpan.FromSeconds(1), false))
                        {
                            PetMessage.Show(Lang.T("msg.alreadyRunning"), Lang.T("msg.information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                PetMessage.Show(Lang.T("msg.cantExecute", ex.Message));
                return;
            }

            // Check and parse the arguments
            string SearchStringLocalXml = "localxml=";
            string SearchStringWebXml = "webxml=";
            string SearchStringInstall = "install=";
            bool loadExternalXml = false;
			foreach (string s in args)
            {
                if (s.IndexOf(SearchStringLocalXml) >= 0)
                {
                    ArgumentLocalXML = s.Substring(s.IndexOf(SearchStringLocalXml) + SearchStringLocalXml.Length);
                    if (ArgumentLocalXML.IndexOf(" ") >= 0)
                    {
						ArgumentLocalXML = ArgumentLocalXML.Substring(0, ArgumentLocalXML.IndexOf(" "));
                    }
                    loadExternalXml = true;
				}
                else if (s.IndexOf(SearchStringWebXml) >= 0)
                {
                    ArgumentWebXML = s.Substring(s.IndexOf(SearchStringWebXml) + SearchStringWebXml.Length);
                    if (ArgumentWebXML.IndexOf(" ") >= 0)
                    {
                        ArgumentWebXML = ArgumentWebXML.Substring(0, ArgumentWebXML.IndexOf(" "));
                    }
					loadExternalXml = true;
				}
                else if (s.IndexOf(SearchStringInstall) >= 0)
                {
                    ArgumentInstall = s.Substring(s.IndexOf(SearchStringInstall) + SearchStringInstall.Length);
                    if (ArgumentInstall.IndexOf(" ") >= 0)
                    {
                        ArgumentInstall = ArgumentInstall.Substring(0, ArgumentInstall.IndexOf(" "));
                    }
					loadExternalXml = true;
				}
            }

            if (loadExternalXml)
            {
				MyData.SetXml(MyData.LoadXML(), "");
			}
            else
            {
                RefreshBuiltInPet();
            }
            WindowsStartup.Refresh();   // "start with Windows" follows the exe after an install

            // Show the system tray icon.
            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();

                Mainthread = new StartUp(pi);

                // "DesktopPet.exe settings[=pets|behaviour|application|about]" opens the settings window at startup.
                foreach (string s in args)
                {
                    // developer: another gallery address ("--gallery=https://nowhere.invalid/" tests the no-internet panel)
                    if (s.StartsWith("--gallery=", StringComparison.OrdinalIgnoreCase))
                    {
                        OptionsGit.BaseGitUrl = s.Substring("--gallery=".Length);
                        continue;
                    }
                    // developer: another place for the languages to download (an address or a folder with languages.json)
                    if (s.StartsWith("--languages=", StringComparison.OrdinalIgnoreCase))
                    {
                        LanguageStore.BaseUrl = s.Substring("--languages=".Length);
                        continue;
                    }
                    // developer: check the features and write PASS/FAIL lines ("--selftest=C:\folder\result.txt")
                    if (s.StartsWith("--ai-check=", StringComparison.OrdinalIgnoreCase))
                    {
                        SelfTest.AiCheck(s.Substring("--ai-check=".Length));
                        continue;
                    }
                    if (s.StartsWith("--only=", StringComparison.OrdinalIgnoreCase))
                    {
                        SelfTest.Only = s.Substring("--only=".Length);
                        continue;
                    }
                    if (s.StartsWith("--selftest=", StringComparison.OrdinalIgnoreCase))
                    {
                        Onboarding.Disabled = true;
                        SelfTest.Run(s.Substring("--selftest=".Length));
                        continue;
                    }
                    if (s.StartsWith("--snapshot=", StringComparison.OrdinalIgnoreCase))
                    {
                        Onboarding.Disabled = true;
                        FormSettings.SaveSnapshots(s.Substring("--snapshot=".Length));
                        continue;
                    }
                    if (!s.StartsWith("settings", StringComparison.OrdinalIgnoreCase)) continue;
                    var page = FormSettings.Page.Pets;
                    int eq = s.IndexOf('=');
                    if (eq > 0) Enum.TryParse(s.Substring(eq + 1), true, out page);
                    FormSettings.ShowPage(page);
                }

                // Make sure the application runs!
                Application.Run();
            }

            if (iMutexIndex == 0)
                mutex.ReleaseMutex();
            else if (iMutexIndex == 1)
                mutex2.ReleaseMutex();
        }


        /// <summary>
        /// A built-in pet (Claude, Capi …) is saved in the settings when it is adopted, so a newer exe would keep
        /// showing the old copy. Replace it with the version embedded in this exe when they differ.
        /// </summary>
        /// <summary>Same pet: the same title, name and author in the header (the version may differ).</summary>
        static bool SameHeader(string a, string b)
        {
            try
            {
                string Head(string xml)
                {
                    var d = new System.Xml.XmlDocument();
                    d.LoadXml(xml);
                    var h = d.DocumentElement?.SelectSingleNode("*[local-name()='header']");
                    return h == null ? null : string.Join("|", new[] { "title", "petname", "author" }.Select(n => h.SelectSingleNode($"*[local-name()='{n}']")?.InnerText ?? ""));
                }
                string ha = Head(a);
                return ha != null && ha == Head(b);
            }
            catch (Exception) { return false; }
        }

        static void RefreshBuiltInPet()
        {
            try
            {
                string folder = MyData.GetCurrentPetFolder();
                // the eSheep saved on the first run has no folder name: recognise it by its header
                if (string.IsNullOrEmpty(folder))
                    folder = BuiltInPets.Folders.FirstOrDefault(f => SameHeader(MyData.GetXml(), BuiltInPets.GetXml(f))) ?? "";
                // the pet in use comes from the exe (eSheep) or from its downloaded copy: when that copy is newer than
                // the one kept in the settings (an update of the gallery, another color downloaded…), use the new one
                string source = BuiltInPets.IsBuiltIn(folder) ? BuiltInPets.GetXml(folder)
                    : !string.IsNullOrEmpty(folder) && PetStore.Has(folder) ? PetStore.Load(folder) : null;
                if (source == null) return;
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(source);
                string fresh = doc.OuterXml;               // same form the settings window saves
                if (MyData.GetXml() == fresh) return;
                MyData.SetImages("");                      // cached sprites/icon belong to the old copy
                MyData.SetIcon("");
                MyData.SetXml(fresh, "");
                MyData.SetCurrentPetFolder(folder);        // SetXml forgets it, keep the pet adopted
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.info, "pet in use '" + folder + "' updated from its saved copy");
            }
            catch (Exception ex)
            {
                StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.warning, "built-in pet refresh failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Check if application is started from the installation path.
        /// </summary>
        /// <returns>true if the executed application is installed.</returns>
        public static bool IsApplicationInstalled()
        {
            // C:\Program Files\DesktopPet, or %LocalAppData%\DesktopPet of the versions before 2.7
            string appPath = Application.StartupPath.TrimEnd('\\');
            return string.Equals(appPath, InstallPlace.ProgramDir, StringComparison.OrdinalIgnoreCase)
                || string.Equals(appPath, InstallPlace.DataDir, StringComparison.OrdinalIgnoreCase);
        }

        public static void RestartApp()
        {
			Application.Restart();
			Application.Exit();
		}

        public static bool OpenLink(string httpsUrl)
        {
			if (Uri.TryCreate(httpsUrl, UriKind.Absolute, out Uri validatedUri) && validatedUri.Scheme == Uri.UriSchemeHttps)
			{
				try
				{
					// 2. Sicher ausführen
					Process.Start(new ProcessStartInfo
					{
						FileName = validatedUri.AbsoluteUri,
						UseShellExecute = true
					});
                    return true;
				}
				catch (Exception ex)
				{
					// Abfangen, falls z.B. kein Browser im System registriert ist
					Console.WriteLine($"Error opening browser: {ex.Message}");
                    return false;
				}
			}
			else
			{
				Console.WriteLine("Invalid URL or not a https.");
                return false;
			}
        }
    }
}
