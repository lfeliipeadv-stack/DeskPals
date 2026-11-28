using System.Diagnostics;

namespace PetEditor
{
	/// <summary>
	/// "Test on the desktop": the pet as it is now (saved or not) goes to a temporary file and a second DesktopPet
	/// shows only it ("DesktopPet.exe --test-pet=&lt;file&gt;": it does not touch the user's pet or settings). While
	/// it runs, every change in the editor is written again and that DesktopPet reloads the pet.
	/// </summary>
	internal static class DesktopTest
	{
		static Process running;
		static readonly string Folder = Path.Combine(Path.GetTempPath(), "PetEditor", "test");
		static string FilePath => Path.Combine(Folder, "animations.xml");
		static readonly System.Windows.Forms.Timer later = new() { Interval = 900 };
		static PetDoc pending;

		static DesktopTest() => later.Tick += (s, e) => { later.Stop(); if (pending != null && IsRunning) Write(pending); };

		public static bool IsRunning => running != null && !running.HasExited;

		/// <summary>DesktopPet.exe: the one chosen in the settings, next to the editor, installed, or the developer build.</summary>
		public static string FindDesktopPet()
		{
			var s = EditorSettings.Current.DesktopPetPath;
			if (!string.IsNullOrWhiteSpace(s) && File.Exists(s) && SupportsTest(s)) return s;
			string here = AppContext.BaseDirectory;
			var candidates = new List<string>();
			foreach (var name in ExeNames) candidates.Add(Path.Combine(here, name));
			// developer: the build of the repository (editor\bin\...\ next to src\bin\...\) comes before an installed copy
			var dir = new DirectoryInfo(here);
			for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
				foreach (var name in ExeNames) candidates.Add(Path.Combine(dir.FullName, "src", "bin", "Release", "net10.0-windows", "win-x64", name));
			candidates.AddRange(Installed());
			return candidates.FirstOrDefault(c => File.Exists(c) && SupportsTest(c));
		}

		/// <summary>The program's name from 1.1.1 on, and before (an install updated from then keeps DesktopPet.exe).</summary>
		static readonly string[] ExeNames = { "DeskPals.exe", "DesktopPet.exe" };

		/// <summary>Where an installed copy can be: Program Files, or %LocalAppData% of the versions before 2.7.</summary>
		static IEnumerable<string> Installed() =>
			new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) }
				.SelectMany(root => ExeNames.Select(name => Path.Combine(root, "DesktopPet", name)));

		/// <summary>The first version with "--test-pet". An older one would start as a second normal DesktopPet.</summary>
		public static readonly Version TestVersion = new(2, 8, 3);

		/// <summary>
		/// This program can show a test pet (its version, or a developer build of the repository). The builds made while
		/// it was written were numbered 2.x; the public versions started again at 1.0.0 (after 2.8.x), and all of them can.
		/// </summary>
		public static bool SupportsTest(string exe)
		{
			try
			{
				if (exe.Replace('/', '\\').Contains(@"\src\bin\", StringComparison.OrdinalIgnoreCase)) return true;
				var v = System.Diagnostics.FileVersionInfo.GetVersionInfo(exe);
				var version = new Version(v.FileMajorPart, v.FileMinorPart, v.FileBuildPart);
				if (version.Major == 2) return version >= TestVersion;
				// the original eSheep (Adriano's) also had 1.x versions, without the test: its product is "eSheep"
				return version.Major >= 1 && !(v.ProductName ?? "").StartsWith("eSheep", StringComparison.OrdinalIgnoreCase);
			}
			catch { return false; }
		}

		/// <summary>An installed copy too old for the test (to say so instead of "not found").</summary>
		public static string OldInstalled()
		{
			foreach (var c in Installed())
				if (File.Exists(c) && !SupportsTest(c)) return System.Diagnostics.FileVersionInfo.GetVersionInfo(c).FileVersion;
			return null;
		}

		/// <summary><paramref name="startWith"/>: the pet comes in with this animation (every entrance leads to it, in the test only).</summary>
		public static void Run(MainForm owner, PetDoc doc, int startWith = -1)
		{
			if (doc == null) return;
			var (errors, _) = Validator.Count(doc);
			if (errors > 0)
			{
				if (MessageBox.Show(owner, L.T("test.errors", errors), L.T("app.name"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
				{
					owner.Go("check");
					return;
				}
			}
			string exe = FindDesktopPet();
			if (exe == null)
			{
				string old = OldInstalled();
				owner.Message(old != null ? L.T("test.oldApp", old, TestVersion) : L.T("test.noApp"));
				if (old == null) owner.Go("settings");
				return;
			}
			try
			{
				Write(doc, startWith);
				if (IsRunning) return;   // the running test reloads the file by itself
				running = Process.Start(new ProcessStartInfo(exe) { ArgumentList = { "--test-pet=" + FilePath }, UseShellExecute = false });
			}
			catch (Exception ex) { owner.Message(L.T("test.fail", ex.Message)); }
		}

		/// <summary>A change in the editor: the test on the desktop gets it a moment later.</summary>
		public static void Refresh(PetDoc doc)
		{
			if (!IsRunning) return;
			pending = doc;
			later.Stop(); later.Start();
		}

		static void Write(PetDoc doc, int startWith = -1)
		{
			Directory.CreateDirectory(Folder);
			File.WriteAllText(FilePath, StartingWith(PetXmlWriter.Write(doc.Root), startWith), new System.Text.UTF8Encoding(false));
		}

		/// <summary>
		/// The pet's file with every entrance leading to one animation (the pet starts with it, then goes on as usual).
		/// Only the copy of the test changes; -1 or a pet without entrances: the file as it is.
		/// </summary>
		internal static string StartingWith(string xml, int id)
		{
			if (id < 0) return xml;
			try
			{
				var x = System.Xml.Linq.XDocument.Parse(xml, System.Xml.Linq.LoadOptions.PreserveWhitespace);
				var nexts = x.Descendants().Where(e => e.Name.LocalName == "spawn").SelectMany(s => s.Elements().Where(e => e.Name.LocalName == "next")).ToList();
				if (nexts.Count == 0) return xml;
				foreach (var n in nexts) n.Value = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
				return x.Declaration != null ? x.Declaration + Environment.NewLine + x.Root : x.ToString();
			}
			catch (Exception) { return xml; }
		}

		public static void Close()
		{
			try { if (IsRunning) { running.CloseMainWindow(); if (!running.WaitForExit(800)) running.Kill(); } }
			catch (Exception e) { Debug.WriteLine("test close: " + e.Message); }
		}
	}
}
