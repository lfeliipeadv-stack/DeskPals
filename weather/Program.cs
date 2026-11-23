using System.Drawing;
using DesktopPet;

namespace DeskPalsWeather
{
	/// <summary>
	/// DeskPals Weather: one copy runs, with an icon in the tray (turn the weather on or off, the scenes, settings, quit).
	/// Opening the program again (DeskPals' Extensions page does that) shows the settings of the copy that runs; "--quit"
	/// closes it; "--config &lt;file&gt;" says where DeskPals keeps its settings (its language and theme are used here too);
	/// "--devtests[=report]" runs the tests of the extension and quits (the exit code is the number of failures).
	/// </summary>
	internal static class Program
	{
		static NotifyIcon tray;
		static SettingsForm settings;

		[STAThread]
		static int Main(string[] args)
		{
			// a picture of the grass (the one of now and the new model), to compare
			string dialogMock = args.FirstOrDefault(a => a.StartsWith("--lights-dialog="));
			if (dialogMock != null) { ApplicationConfiguration.Initialize(); LightsDialog.Mock(dialogMock[16..].Trim('"')); return 0; }
			string spacing = args.FirstOrDefault(a => a.StartsWith("--lights-spacing="));
			if (spacing != null) { LightsArt.SpacingPreview(spacing[17..].Trim('"')); return 0; }
			string corners = args.FirstOrDefault(a => a.StartsWith("--lights-corners="));
			if (corners != null) { LightsArt.Corners(corners[17..].Trim('"')); return 0; }
			string lights = args.FirstOrDefault(a => a.StartsWith("--lights-preview="));
			if (lights != null) { LightsArt.Preview(lights[17..].Trim('"')); return 0; }
			string grass = args.FirstOrDefault(a => a.StartsWith("--grass-preview="));
			if (grass != null) { GrassArt.Preview(grass[16..].Trim('"')); return 0; }
			string devtests = args.FirstOrDefault(a => a.StartsWith("--devtests"));
			if (devtests != null)
			{
				ApplicationConfiguration.Initialize();
				return DevTests.Run(devtests.Contains('=') ? devtests[(devtests.IndexOf('=') + 1)..].Trim('"') : null);
			}
			bool quit = args.Contains("--quit"), background = args.Contains("--background");
			int at = Array.IndexOf(args, "--config");
			string deskPalsConfig = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
			using var single = new Mutex(true, "Local\\DeskPalsWeather", out bool first);
			if (!first)
			{
				// the copy that runs shows its settings, or quits
				string name = quit ? @"Local\DeskPalsWeatherQuit" : @"Local\DeskPalsWeatherShow";
				if (EventWaitHandle.TryOpenExisting(name, out var signal)) { signal.Set(); signal.Dispose(); }
				return 0;
			}
			if (quit) return 0;
			ApplicationConfiguration.Initialize();
			CatchErrors();
			if (deskPalsConfig != null && File.Exists(deskPalsConfig) && deskPalsConfig != Controller.Config.DeskPalsConfig) { Controller.Config.DeskPalsConfig = deskPalsConfig; Controller.Config.Save(); }
			DeskPals.ApplyTheme(Controller.Config);
			var ui = new WindowsFormsSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(ui);
			Listen(@"Local\DeskPalsWeatherShow", () => ui.Post(_ => ShowSettings(), null));
			Listen(@"Local\DeskPalsWeatherQuit", () => ui.Post(_ => Application.ExitThread(), null));
			// DeskPals' tray menu: a scene, on or off, a test (what it asks is in a small file)
			Listen(@"Local\DeskPalsWeatherCommand", () => ui.Post(_ => { try { Controller.RunCommand(File.ReadAllText(Controller.CommandFile)); } catch (Exception) { } }, null));
			iconOn = AppIcon(); iconOff = Gray(iconOn);
			tray = new NotifyIcon { Icon = iconOn, Text = T.S("app"), Visible = true, ContextMenuStrip = Menu() };
			tray.DoubleClick += (s, e) => ShowSettings();
			T.Changed += () => { tray.Text = T.S("app"); tray.ContextMenuStrip = Menu(); };
			Controller.Changed += ShowState;
			Controller.Start();
			if (!background) ShowSettings();
			Application.Run();
			Controller.Quit();
			tray.Visible = false;
			tray.Dispose();
			return 0;
		}

		public static string ErrorLog => Path.Combine(Config.Folder, "weather-errors.log");

		/// <summary>An error nobody caught does not stop the program nor show a box: it is written down (the last ones) and the program goes on.</summary>
		static void CatchErrors()
		{
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			Application.ThreadException += (s, e) => Log(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => Log(e.ExceptionObject as Exception);
		}

		public static void Log(Exception ex)
		{
			try
			{
				Directory.CreateDirectory(Config.Folder);
				if (File.Exists(ErrorLog) && new FileInfo(ErrorLog).Length > 200_000) File.Delete(ErrorLog);
				File.AppendAllText(ErrorLog, DateTime.Now.ToString("s") + " " + ex + Environment.NewLine + Environment.NewLine);
			}
			catch (Exception) { }
		}

		static Icon iconOn, iconOff;

		/// <summary>The tray icon tells what goes on: in colour while the weather shows, grey while it is off or paused; its tip says what shows.</summary>
		static void ShowState()
		{
			if (tray == null) return;
			bool showing = Controller.Config.On && Controller.Now.By == "" || Controller.Testing;
			var icon = showing ? iconOn : iconOff;
			if (tray.Icon != icon) tray.Icon = icon;
			string tip = T.S("app") + Environment.NewLine + SettingsForm.StateText();
			tray.Text = tip.Length > 127 ? tip[..126] + "…" : tip;
		}

		/// <summary>The icon without its colours (off or paused).</summary>
		static Icon Gray(Icon icon)
		{
			try
			{
				using var colour = icon.ToBitmap();
				var gray = new Bitmap(colour.Width, colour.Height);
				using (var g = Graphics.FromImage(gray))
				using (var a = new System.Drawing.Imaging.ImageAttributes())
				{
					float r = 0.3f, gr = 0.59f, b = 0.11f;
					a.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new[] { new[] { r, r, r, 0, 0 }, new[] { gr, gr, gr, 0, 0 }, new[] { b, b, b, 0, 0 }, new[] { 0f, 0, 0, 0.75f, 0 }, new[] { 0f, 0, 0, 0, 1 } }));
					g.DrawImage(colour, new Rectangle(0, 0, colour.Width, colour.Height), 0, 0, colour.Width, colour.Height, GraphicsUnit.Pixel, a);
				}
				return Icon.FromHandle(gray.GetHicon());
			}
			catch (Exception) { return icon; }
		}

		static Icon AppIcon()
		{
			try { return Icon.ExtractAssociatedIcon(Environment.ProcessPath) ?? SystemIcons.Application; } catch (Exception) { return SystemIcons.Application; }
		}

		static ContextMenuStrip Menu()
		{
			var m = new ContextMenuStrip();
			Theme.ApplyTo(m);
			var on = new ToolStripMenuItem(T.S("tray.on")) { Checked = Controller.Config.On };
			on.Click += (s, e) => { Controller.Config.On = !Controller.Config.On; Controller.Apply(); };
			// the scenes: the built-in ones and one's own, put on with a click
			var scenes = new ToolStripMenuItem(T.S("tab.scenes"));
			// clear what piles up (the snow, the leaves, the blossom), only while something does
			var clear = new ToolStripMenuItem();
			clear.Click += (s, e) => Engine.Clear();
			m.Opening += (s, e) =>
			{
				Theme.ApplyTo(m);
				on.Checked = Controller.Config.On;
				bool piles = Controller.Falling is Kind.Snow or Kind.Leaves or Kind.Blossom;
				clear.Visible = piles;
				if (piles) clear.Text = (Controller.Falling switch { Kind.Leaves => "🍂 ", Kind.Blossom => "🌸 ", _ => "❄ " }) + T.S("clear." + Controller.Name(Controller.Falling.Value));
				scenes.DropDownItems.Clear();
				foreach (var (_, name, scene) in Controller.AllScenes())
				{
					var item = new ToolStripMenuItem(name) { Checked = scene.SameAs(Controller.Config) };
					item.Click += (o, a) => { scene.ApplyTo(Controller.Config); Controller.Config.On = true; Controller.Apply(); };
					scenes.DropDownItems.Add(item);
				}
			};
			m.Items.Add(on);
			m.Items.Add(scenes);
			m.Items.Add(T.S("w.test.button"), null, (s, e) => Controller.Test());
			m.Items.Add(clear);
			m.Items.Add(T.S("tray.settings"), null, (s, e) => ShowSettings());
			m.Items.Add(new ToolStripSeparator());
			m.Items.Add(T.S("tray.quit"), null, (s, e) => Application.ExitThread());
			return m;
		}

		public static void ShowSettings()
		{
			if (settings == null || settings.IsDisposed) settings = new SettingsForm();
			settings.Show();
			if (settings.WindowState == FormWindowState.Minimized) settings.WindowState = FormWindowState.Normal;
			settings.Activate();
		}

		/// <summary>A named signal the other copies (and DeskPals) set: show the settings, or quit.</summary>
		static void Listen(string name, Action run)
		{
			var signal = new EventWaitHandle(false, EventResetMode.AutoReset, name);
			new Thread(() => { while (signal.WaitOne()) run(); }) { IsBackground = true }.Start();
		}
	}
}
