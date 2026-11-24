using Microsoft.Win32;
using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DesktopPet
{
        /// <summary>
        /// Window form for the installer/Repair/Uninstaller. Will add shortcuts to windows.
        /// </summary>
        /// <preliminary/>
    public partial class Install : Form
    {
            /// <summary>
            /// Path where the application is located.
            /// </summary>
        readonly string appPath;
            /// <summary>
            /// Path where the application can be installed.
            /// </summary>
        readonly string installPath;
            /// <summary>
            /// Path to the user desktop location.
            /// </summary>
        readonly string desktopPath;
            /// <summary>
            /// Path to the user start menu location.
            /// </summary>
        readonly string startMenuPath;
            /// <summary>
            /// Path to the start-up location.
            /// </summary>
        readonly string autostartPath;
            /// <summary>
            /// Webpage for this application.
            /// </summary>
            /// <summary>
            /// Application Name.
            /// </summary>
        /// <summary>A shortcut of the program in this folder: DeskPals.url, or DesktopPet.url of a version before the new name.</summary>
        static bool ShortcutIn(string folder) =>
            File.Exists(Path.Combine(folder, InstallPlace.ShortcutName)) || File.Exists(Path.Combine(folder, InstallPlace.OldShortcutName));
            /// <summary>
            /// Name of the uninstall batch file.
            /// </summary>
        readonly string uninstallBatch = "uninstall.cmd";

            /// <summary>
            /// Constructor. Set the path of the different locations and start the update-check process.
            /// </summary>
        public Install()
        {
            InitializeComponent();
            Icon = Properties.Resources.icon;   // program logo instead of the sheep stored in the form's resources

            appPath = Application.StartupPath;
            installPath = InstallPlace.ProgramDir;
            desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            autostartPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                // Check updates only if the application is installed (so downloads from other page will not notify an update after download)
            if(Program.IsApplicationInstalled())
                CheckUpdates();
        }

            /// <summary>
            /// Form was loaded.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Arguments values.</param>
        private void Install_Load(object sender, EventArgs e)
        {
            Text = Lang.T("inst.title", Animations.Xml.AnimationXML.Header.Title);
            label1.Text = Lang.T("inst.header", Animations.Xml.AnimationXML.Header.Petname);
            ApplyTexts();
        }

        /// <summary>Step of the install wizard (the button text is translated, so it can't be used for that).</summary>
        private enum Step { License, Options, Done }
        private Step step = Step.License;

        /// <summary>Write all fixed texts in the current language and apply the modern theme.</summary>
        private void ApplyTexts()
        {
            checkBox1.Text = Lang.T("inst.startMenu");
            checkBox2.Text = Lang.T("inst.desktop");
            checkBox3.Text = Lang.T("inst.autostart");
            button2.Text = Lang.T("inst.update");
            button3.Text = Lang.T("inst.uninstall");
            button1.Text = step == Step.License ? Lang.T("inst.accept") : step == Step.Options ? Lang.T("inst.install") : Lang.T("inst.installed");
            Theme.ApplyTo(this);
            Theme.MakePrimary(button1);
            LayoutModern();
        }

        // ------------------------------------------------------------------ the look of the rest of the program

        const int MW = 540, MPad = 24;
        Font mTitle, mText, mSmall, mIcon;
        Label mDesc;
        Rectangle mCard;

        /// <summary>The same look as the other windows: icon in a circle, title, a line about it, a card, buttons below.</summary>
        void LayoutModern()
        {
            if (mTitle == null)
            {
                mTitle = Theme.CreateFont(20, FontStyle.Bold); mText = Theme.CreateFont(14); mSmall = Theme.CreateFont(12); mIcon = Theme.CreateFont(22);
                mDesc = new Label { AutoSize = false };
                Controls.Add(mDesc);
                pictureBox1.Visible = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = MinimizeBox = false;
                textBox1.VisibleChanged += (s, e) => Invalidate();
                checkBox1.VisibleChanged += (s, e) => Invalidate();
            }
            ClientSize = new Size(MW, 400);
            label1.Text = Lang.T("inst.headerApp");
            label1.Font = mTitle; label1.AutoSize = true;
            label1.Location = new Point(MPad + 64, MPad + 2);
            mDesc.Text = Lang.T("inst.desc");
            mDesc.Font = mSmall; mDesc.ForeColor = Theme.TextMuted;
            mDesc.Location = new Point(MPad + 64, MPad + 34);
            mDesc.Size = new Size(MW - MPad * 2 - 64, 58);
            mCard = new Rectangle(MPad, 122, MW - MPad * 2, 190);
            Text = Lang.T("inst.headerApp");
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Font = mSmall;
            textBox1.BackColor = Theme.Surface; textBox1.ForeColor = Theme.TextMuted;
            textBox1.Location = new Point(mCard.X + 14, mCard.Y + 12);
            textBox1.Size = new Size(mCard.Width - 22, mCard.Height - 24);
            var boxes = new[] { checkBox1, checkBox2, checkBox3 };
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].Font = mText; boxes[i].AutoSize = true;
                boxes[i].BackColor = Theme.Surface; boxes[i].ForeColor = Theme.Text;
                boxes[i].Location = new Point(mCard.X + 18, mCard.Y + 20 + i * 42);
            }
            progressBar1.Location = new Point(mCard.X + 18, mCard.Bottom - 34);
            progressBar1.Size = new Size(mCard.Width - 36, 10);
            foreach (var b in new[] { button1, button2, button3 }) { b.Font = mText; b.AutoSize = true; b.MinimumSize = new Size(120, 38); }
            int by = mCard.Bottom + 28;
            button1.Location = new Point(MW - MPad - button1.Width, by);
            button2.Location = new Point(button1.Left - 10 - button2.Width, by);
            button3.Location = new Point(MPad, by);
            button3.ForeColor = Theme.Danger;
            ClientSize = new Size(MW, by + button1.Height + MPad);
            BackColor = Theme.Background;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (mTitle == null) return;
            var g = e.Graphics;
            Theme.Prepare(g);
            var ic = new Rectangle(MPad, MPad, 48, 48);
            Theme.FillRounded(g, Theme.AccentSoft, ic, 24);
            Theme.DrawText(g, "📦", mIcon, ic, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            Theme.FillRounded(g, Theme.Surface, mCard, 12);
            Theme.DrawRounded(g, Theme.Border, mCard, 12);
            using (var p = new Pen(Theme.Border)) g.DrawLine(p, MPad, button1.Top - 12, MW - MPad, button1.Top - 12);
        }

            /// <summary>
            /// Open the form window. Allows the user to install or uninstall this application.
            /// </summary>
        public void ShowInstallation()
        {
            if (!Program.IsApplicationInstalled())
            {
                // The BSD license disclaimer stays in English: it is the legal text of the project.
                string msg = "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" ";
                msg += "AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE ";
                msg += "IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE ";
                msg += "DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE ";
                msg += "FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL ";
                msg += "DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR ";
                msg += "SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER ";
                msg += "CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, ";
                msg += "OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE ";
                msg += "OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. ";

                textBox1.Text = msg;
                textBox1.Visible = true;
                step = Step.License;
                button1.Text = Lang.T("inst.accept");
                button2.Visible = false;
                button3.Visible = false;
            }
            else
            {
                button1.Visible = false;
                button2.Visible = true;
                button3.Visible = true;
                textBox1.Visible = false;
                checkBox1.Visible = true;
                checkBox2.Visible = true;
                checkBox3.Visible = true;
                button2.Enabled = false;

                checkBox1.Checked = ShortcutIn(startMenuPath);
                checkBox2.Checked = ShortcutIn(desktopPath);
                checkBox3.Checked = ShortcutIn(autostartPath);
            }

            Show();
        }

            /// <summary>
            /// Install was pressed. The application will be installed on the AppData folder.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Mouse event values.</param>
        private void Button1_Click(object sender, EventArgs e)
        {
            if (step == Step.License)
            {
                step = Step.Options;
                button1.Text = Lang.T("inst.install");
                button2.Visible = false;
                textBox1.Visible = false;
                checkBox1.Visible = true;
                checkBox2.Visible = true;
                checkBox3.Visible = true;
                progressBar1.Visible = true;
            }
            else if(step == Step.Options)
            {
                checkBox1.Enabled = false;
                checkBox2.Enabled = false;
                checkBox3.Enabled = false;
                button1.Enabled = false;
                Application.DoEvents();
                progressBar1.Value = 100;
                InstallApplication();
            }
            else
            {
                Application.Exit();
            }
        }

            /// <summary>
            /// Install: the program goes to C:\Program Files\DesktopPet (Windows asks for administrator permission), the
            /// shortcuts point there, and an entry in Windows Settings › Apps uninstalls it. The pet chosen now and the
            /// shortcut icon go to the user's folder (%LocalAppData%\DesktopPet), where the settings and pets also are.
            /// </summary>
        private void InstallApplication()
        {
            string sDestExe = Path.Combine(installPath, InstallPlace.ExeName);
            string dataDir = InstallPlace.DataDir;
            string iconFile = Path.Combine(dataDir, "icon.ico");
            try
            {
                Directory.CreateDirectory(dataDir);
                File.WriteAllBytes(Path.Combine(dataDir, "installpet.xml"), System.Text.Encoding.UTF8.GetBytes(Animations.Xml.AnimationXMLString));
                File.WriteAllBytes(iconFile, Animations.Xml.bitmapIcon.ToArray());
            }
            catch (Exception skipped) { LogFile.Ignored(skipped); iconFile = sDestExe; }

            var shortcuts = new[]
            {
                checkBox1.Checked ? Path.Combine(startMenuPath, InstallPlace.ShortcutName) : "",
                checkBox2.Checked ? Path.Combine(desktopPath, InstallPlace.ShortcutName) : "",
                checkBox3.Checked ? Path.Combine(autostartPath, InstallPlace.ShortcutName) : "",
            };
            string request = string.Join("|", shortcuts) + "|" + iconFile + "|" + (Lang.Current.StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? "pt" : "en");

            string error;
            if (InstallPlace.IsAdmin()) error = InstallToProgramFiles(Environment.ProcessPath ?? Application.ExecutablePath, request);
            else
            {
                // a copy of this program, as administrator, does the copying (see Program: --install-program=)
                try
                {
                    var psi = new ProcessStartInfo(Environment.ProcessPath ?? Application.ExecutablePath, "\"--install-program=" + request + "\"") { UseShellExecute = true, Verb = "runas" };
                    using (var p = Process.Start(psi))
                    {
                        p.WaitForExit();
                        error = p.ExitCode == 0 ? null : Lang.T("inst.adminFailed");
                    }
                }
                catch (System.ComponentModel.Win32Exception) { error = Lang.T("inst.adminCancelled"); }   // "No" in the Windows question
            }
            if (error != null)
            {
                PetMessage.Show(Lang.T("inst.error", error));
                progressBar1.Value = 0;
                checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = button1.Enabled = true;
                return;
            }

            step = Step.Done;
            button1.Text = Lang.T("inst.installed");
            Process.Start(new ProcessStartInfo(sDestExe) { UseShellExecute = true, WorkingDirectory = installPath });
            Hide();
            Application.DoEvents();
            Application.Exit();
        }

        /// <summary>
        /// The part that needs administrator: copy the exe (and the languages) to Program Files, write the shortcuts,
        /// the uninstaller and the entry in Windows Settings; the program of an older install in %LocalAppData% goes
        /// away (the user's things stay). <paramref name="request"/>: "startmenu|desktop|autostart|icon|pt".
        /// Returns null when it worked, or the error.
        /// </summary>
        internal static string InstallToProgramFiles(string sourceExe, string request)
        {
            try
            {
                var parts = request.Split('|');
                string target = InstallPlace.ProgramDir;
                Directory.CreateDirectory(target);
                string exe = Path.Combine(target, InstallPlace.ExeName);
                if (!string.Equals(Path.GetFullPath(sourceExe), Path.GetFullPath(exe), StringComparison.OrdinalIgnoreCase))
                {
                    // the copy installed there, also under the old name (DesktopPet.exe), closes first
                    foreach (string name in InstallPlace.ProcessNames)
                        foreach (var p in Process.GetProcessesByName(name))
                            try { if (p.Id != Environment.ProcessId && string.Equals(Path.GetDirectoryName(p.MainModule?.FileName ?? ""), target, StringComparison.OrdinalIgnoreCase)) { p.Kill(); p.WaitForExit(3000); } } catch (Exception skipped) { LogFile.Ignored(skipped); }
                    File.Copy(sourceExe, exe, true);
                }
                string langFrom = Path.Combine(Path.GetDirectoryName(sourceExe), "lang"), langTo = Path.Combine(target, "lang");
                if (Directory.Exists(langFrom) && !string.Equals(langFrom, langTo, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(langTo);
                    foreach (string f in Directory.GetFiles(langFrom, "*.json")) File.Copy(f, Path.Combine(langTo, Path.GetFileName(f)), true);
                }

                string icon = parts.Length > 3 && parts[3].Length > 0 ? parts[3] : exe;
                string contents = $"[InternetShortcut]\r\nURL=file:///{exe.Replace('\\', '/')}\r\nIconFile={icon}\r\nIconIndex=0\r\n";
                for (int i = 0; i < 3 && i < parts.Length; i++)
                    if (parts[i].Length > 0) { Directory.CreateDirectory(Path.GetDirectoryName(parts[i])); File.WriteAllText(parts[i], contents); }

                // uninstall.cmd: every place a shortcut can be (also the ones not chosen now, made later in "Repair")
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", InstallPlace.ShortcutName);
                string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), InstallPlace.ShortcutName);
                string startup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), InstallPlace.ShortcutName);
                var all = new System.Collections.Generic.List<string> { startMenu, desktop, startup };
                for (int i = 0; i < 3 && i < parts.Length; i++) if (parts[i].Length > 0 && !all.Contains(parts[i])) all.Add(parts[i]);
                bool pt = parts.Length > 4 && parts[4] == "pt";
                string scriptPath = Path.Combine(target, "uninstall.cmd");
                File.WriteAllText(scriptPath, UninstallScript.Build(InstallPlace.AppName, all, InstallPlace.UninstallKey, pt));
                InstallPlace.Register(exe, FileVersionInfo.GetVersionInfo(exe).FileVersion, scriptPath, AppInfo.RepoUrl);
                // an install before the new names: its DesktopPet.exe and DesktopPet.url shortcuts go (the new ones are here)
                var folders = new System.Collections.Generic.List<string>();
                foreach (string sc in all) { string d = Path.GetDirectoryName(sc); if (!folders.Contains(d)) folders.Add(d); }
                InstallPlace.RemoveOldNames(target, folders);
                InstallPlace.RemoveOldProgram();
                return null;
            }
            catch (Exception ex)
            {
                LogFile.Important("install failed: " + ex);
                return ex.Message;
            }
        }

            /// <summary>
            /// Update was pressed. Application icons will be created or removed.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Mouse event values.</param>
        private void Button2_Click(object sender, EventArgs e)
        {
            string sDestExe = InstallPlace.ExeIn(installPath);   // an install updated from before the new name still has DesktopPet.exe

            string contents = $@"
[InternetShortcut] 
URL=file:///{sDestExe.Replace('\\', '/')}
IconFile={sDestExe.Replace('\\', '/')}
IconIndex=0
            ";
            
            if (checkBox1.Checked != ShortcutIn(startMenuPath))
            {
                string file = Path.Combine(startMenuPath, InstallPlace.ShortcutName);
                try { File.Delete(Path.Combine(startMenuPath, InstallPlace.OldShortcutName)); } catch (Exception skipped) { LogFile.Ignored(skipped); }   // the one of an older version
                    
                if (checkBox1.Checked)  // add to start menu
                    File.WriteAllText(file, contents);
                else                    // remove from start menu
                    File.Delete(file);
            }
            if (checkBox2.Checked != ShortcutIn(desktopPath))
            {
                string file = Path.Combine(desktopPath, InstallPlace.ShortcutName);
                try { File.Delete(Path.Combine(desktopPath, InstallPlace.OldShortcutName)); } catch (Exception skipped) { LogFile.Ignored(skipped); }   // the one of an older version
                if (checkBox2.Checked)  // add to desktop
                    File.WriteAllText(file, contents);
                else                    // remove from desktop
                    File.Delete(file);
            }
            if (checkBox3.Checked != ShortcutIn(autostartPath))
            {
                string file = Path.Combine(autostartPath, InstallPlace.ShortcutName);
                try { File.Delete(Path.Combine(autostartPath, InstallPlace.OldShortcutName)); } catch (Exception skipped) { LogFile.Ignored(skipped); }   // the one of an older version
                if (checkBox3.Checked)  // add to auto start
                    File.WriteAllText(file, contents);
                else                    // remove from auto start
                    File.Delete(file);
            }

            button2.Enabled = false;
        }

            /// <summary>
            /// Uninstall was pressed. The uninstall batch file will be executed.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Mouse event values.</param>
        private void Button3_Click(object sender, EventArgs e)
        {
            using (var uninstallProcess = new Process())
            {
                uninstallProcess.StartInfo.FileName = installPath + "\\" + uninstallBatch;
                uninstallProcess.StartInfo.UseShellExecute = true;   // a .cmd; it asks for administrator itself
                uninstallProcess.Start();
            }
        }

            /// <summary>
            /// Closing form. If it is visible, hide the window so that functions are still available.
            /// </summary>
            /// <param name="sender">Caller as sender.</param>
            /// <param name="e">Closing event values.</param>
        private void Install_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Visible)
            {
                Hide();
                e.Cancel = true;
            }
        }

            /// <summary>
            /// If checkbox was changed and it is in update mode, enable the update button.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Check event values.</param>
        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (button2.Visible) button2.Enabled = true;
        }

            /// <summary>
            /// If checkbox was changed and it is in update mode, enable the update button.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Check event values.</param>
        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (button2.Visible) button2.Enabled = true;
        }

            /// <summary>
            /// If checkbox was changed and it is in update mode, enable the update button.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Check event values.</param>
        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (button2.Visible) button2.Enabled = true;
        }

            /// <summary>
            /// Check for updates in the releases of this repository (see <see cref="AppInfo"/>).
            /// Only a newer version is offered, and the download must match its SHA-256.
            /// </summary>
        private void CheckUpdates()
        {
            _ = Updater.CheckAndInstallAsync(this, silent: true);
        }
    }
}
