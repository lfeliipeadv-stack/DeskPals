using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// The gallery's sources (this repository, the original desktopPet, others the user adds): links read right,
		/// settings kept, the official one always first and the only default, each source in its own cache folder, and
		/// online two sources answer with the first one winning a pet both have. Also the readable names of untranslated
		/// animations, the version groups and the privacy page.
		/// </summary>
		static async Task GalleryFeatures()
		{
			var a = GallerySources.Parse("https://github.com/Adrianotiger/desktopPet");
			Check("sources: a GitHub link", a != null && a.Owner == "Adrianotiger" && a.Repo == "desktopPet" && a.Branch == "master", a?.Key);
			var b = GallerySources.Parse("https://github.com/someone/pets/tree/main/Pets");
			Check("sources: a link to a branch", b != null && b.Repo == "pets" && b.Branch == "main", b?.Key);
			Check("sources: owner/repo@branch", GallerySources.Parse("owner/repo@dev")?.Branch == "dev");
			Check("sources: what is not a repository is refused",
				GallerySources.Parse("hello") == null && GallerySources.Parse("https://example.com/a/b") == null && GallerySources.Parse("") == null);
			Check("sources: each source has its own cache folder", a != null && a.CacheDir != GallerySources.Official.CacheDir && a.CacheDir.StartsWith(OptionsGit.BaseLocalPath));
			Check("sources: folder names of a list never leave the cache",
				!OptionsGit.IsPlainFolder("../x") && !OptionsGit.IsPlainFolder("a/b") && !OptionsGit.IsPlainFolder("a\\b") && !OptionsGit.IsPlainFolder("") && OptionsGit.IsPlainFolder("ssj-goku"));

			// links that name a branch or a folder (1.1.3): a folder of pets, one pet's folder, a file of it
			Check("sources: a plain link leaves the branch and the folder to be found", a != null && !a.BranchGiven && !a.PathGiven && a.Path == null);
			var c = GallerySources.Parse("https://github.com/someone/pets/tree/main/classic/bowser_jr");
			Check("sources: a link to a folder keeps the branch and the folder", c != null && c.Branch == "main" && c.BranchGiven && c.Path == "classic/bowser_jr" && c.PathGiven, c?.Key);
			var d = GallerySources.Parse("https://github.com/someone/pets/blob/dev/rex/animations.xml");
			Check("sources: a link to animations.xml means its folder", d != null && d.Branch == "dev" && d.Path == "rex", d?.Key);
			var root = new GallerySource { Owner = "o", Repo = "r", Branch = "main", Path = "" };
			var back = GallerySources.Parse(root.Key);
			Check("sources: the root folder is saved and read back", root.Key == "o/r@main:." && back != null && back.Path == "" && back.Branch == "main", root.Key);
			Check("sources: the Pets folder is saved as before", new GallerySource { Owner = "o", Repo = "r", Path = "Pets" }.Key == "o/r");
			Check("sources: a folder that leaves the repository is refused",
				GallerySources.Parse("https://github.com/o/r/tree/main/../x") == null && GallerySources.Parse("o/r:../x") == null);
			var listed = OptionsGit.Normalize(new[]
			{
				new OptionsPet { folder = "classic/bowser_jr" }, new OptionsPet { folder = "bowser_jr" }, new OptionsPet { folder = "neko" },
				new OptionsPet { folder = "../evil" }, new OptionsPet { folder = "a\\b" }, new OptionsPet { folder = "solo", path = "" },
			});
			Check("sources: a list with a path in \"folder\" keeps a plain name and the path (a plain folder keeps its own name)",
				listed.Any(p => p.folder == "classic-bowser_jr" && p.path == "classic/bowser_jr") && listed.Any(p => p.folder == "bowser_jr" && p.path == null)
				&& listed.Any(p => p.folder == "neko" && p.path == null) && listed.Any(p => p.folder == "solo" && p.path == ""),
				string.Join(", ", listed.Select(p => p.folder + "=" + (p.path ?? "·"))));
			// "a\b" is read as the subfolder a/b (still inside the repository); only "../evil" leaves it
			Check("sources: entries that leave their folder are left out", !listed.Any(p => (p.path ?? p.folder).Contains("..")) && listed.Count == 5 && listed.Any(p => p.path == "a/b"),
				string.Join(", ", listed.Select(p => p.folder)));
			var alone = OptionsGit.Normalize(new[] { new OptionsPet { folder = "classic/bowser_jr" } });
			Check("sources: a pet in a subfolder takes its last name when it is free", alone.Count == 1 && alone[0].folder == "bowser_jr" && alone[0].path == "classic/bowser_jr");
			// the preview before adding a repository: its first pets, how many more, the warning; drawn to a picture
			var fakes = Enumerable.Range(1, 12).Select(i => new OptionsPet { folder = "pet" + i, name = i == 2 ? "A pet with a really long name to cut" : "Pet " + i }).ToList();
			var fakeIcons = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase) { ["pet1"] = new Bitmap(Properties.Resources.icon.ToBitmap()) };
			using (var preview = new GalleryPreview(new GallerySource { Owner = "someone", Repo = "pets", Path = "" }, fakes, fakeIcons))
			{
				// opened out of sight, so the buttons are made too (a window never shown draws without them)
				preview.StartPosition = FormStartPosition.Manual;
				preview.Location = new Point(-5000, -5000);
				preview.ShowInTaskbar = false;
				preview.Show();
				Application.DoEvents();
				var area = Screen.PrimaryScreen.WorkingArea;
				var addButton = preview.Controls.OfType<Button>().FirstOrDefault(b => b.DialogResult == DialogResult.Yes);
				Check("sources: the preview has its buttons inside the window", addButton != null && addButton.Visible && preview.ClientRectangle.Contains(addButton.Bounds),
					addButton == null ? "no button" : addButton.Bounds + " / " + preview.ClientRectangle);
				Check("sources: the preview window fits on the screen", preview.Height < area.Height && preview.Width < area.Width, preview.Size.ToString());
				// ‹ › turn the pages: 12 pets are 8 and then 4, in a window of the same size
				var sizeBefore = preview.Size;
				Check("sources: the preview starts on the first page, with ‹ › to turn it",
					preview.Page == 0 && preview.Controls.OfType<Button>().Count(b => b.Text == "‹" || b.Text == "›") == 2);
				await preview.TestGo(1);
				Check("sources: › shows the next pets", preview.Page == 1 && preview.Size == sizeBefore, preview.Page + " · " + preview.Size);
				await preview.TestGo(2);
				Check("sources: there is no page after the last", preview.Page == 1);
				using (var shot = new Bitmap(preview.Width, preview.Height))
				{
					preview.DrawToBitmap(shot, new Rectangle(Point.Empty, preview.Size));
					try { shot.Save(Path.Combine(Path.GetTempPath(), "deskpals-gallery-preview.png")); } catch (Exception skipped) { LogFile.Ignored(skipped); }
				}
			}
			Check("sources: a pet without a date has its file's id as version",
				new OptionsPet { lastupdate = "", rev = "abc" }.Version == "abc" && new OptionsPet { lastupdate = "2026-11-24", rev = "abc" }.Version == "2026-11-24");

			string saved = Program.MyData.GetSetting("GallerySources", "");
			bool removedBefore = Program.MyData.GetSettingBool("GalleryOriginalRemoved", false);
			try
			{
				// the original desktopPet is no default any more (1.1.2): a list saved before loses it once; added
				// again by hand afterwards, it stays
				Program.MyData.SetSetting("GalleryOriginalRemoved", false);
				Program.MyData.SetSetting("GallerySources", GallerySources.Official.Key + "|1\nAdrianotiger/desktopPet|1\nx/y|0");
				Check("sources: a list saved before 1.1.2 loses the original desktopPet once",
					string.Join(",", GallerySources.All().Select(s => s.Key)) == GallerySources.Official.Key + ",x/y", string.Join(",", GallerySources.All().Select(s => s.Key)));
				GallerySources.Save(new[] { GallerySources.Official, new GallerySource { Owner = "Adrianotiger", Repo = "desktopPet" } });
				Check("sources: added again by hand, the original desktopPet stays", GallerySources.All().Any(s => s.Key == "Adrianotiger/desktopPet"));

				GallerySources.Save(new[] { new GallerySource { Owner = "x", Repo = "y", Enabled = false }, GallerySources.Official });
				var all = GallerySources.All();
				Check("sources: the official one comes first and is always on", all.Count == 2 && all[0].IsOfficial && all[0].Enabled && !all[1].Enabled,
					string.Join(", ", all.Select(s => s.Key + (s.Enabled ? "" : " (off)"))));
				Check("sources: a source turned off is not read", !GallerySources.Active().Any(s => s.Owner == "x"));

				Program.MyData.SetSetting("GallerySources", "");   // the defaults
				Check("sources: by default only this repository",
					GallerySources.All().Select(s => s.Key).SequenceEqual(new[] { GallerySources.Official.Key }), string.Join(",", GallerySources.All().Select(s => s.Key)));
				// two sources read together: the original desktopPet added by hand
				GallerySources.Save(new[] { GallerySources.Official, new GallerySource { Owner = "Adrianotiger", Repo = "desktopPet" } });
				var pets = await OptionsGit.LoadLists();
				if (!OptionsGit.Online) lines.Add("SKIP  sources online: no internet");
				else
				{
					Check("sources online: both sources answer", OptionsGit.FetchedSources.Count == 2, string.Join(", ", OptionsGit.FetchedSources));
					var neko = pets.FirstOrDefault(p => p.folder == "neko");
					Check("sources online: the first source wins a pet both have", neko?.Source?.IsOfficial == true, neko?.Source?.Key);
					Check("sources online: no pet twice", pets.Select(p => p.folder).Distinct(StringComparer.OrdinalIgnoreCase).Count() == pets.Count);
					Check("sources online: the original gallery is read", OptionsGit.SourceTotals.TryGetValue("Adrianotiger/desktopPet", out int n) && n > 20, n.ToString());
					string why = null;
					try { await GallerySources.CheckAsync(GallerySources.Parse(AppInfo.RepoOwner + "/no-such-gallery-for-the-self-test")); }
					catch (InvalidDataException ex) { why = ex.Message; }
					Check("sources online: a repository that does not exist is said so",
						why != null && (why == Lang.T("sources.notFound", AppInfo.RepoOwner + "/no-such-gallery-for-the-self-test") || why.StartsWith(Lang.T("sources.rateLimit").Substring(0, 10))), why);
					// the link of one pet: its folder, no pets.json there, the pet listed from GitHub's file tree
					try
					{
						var one = GallerySources.Parse(AppInfo.RepoUrl + "/tree/" + AppInfo.Branch + "/Pets/rex");
						var onePets = await GallerySources.CheckAsync(one);
						Check("sources online: the link of one pet brings that pet", onePets.Count == 1 && onePets[0].folder == "rex" && onePets[0].path == "" && !string.IsNullOrEmpty(onePets[0].rev),
							string.Join(", ", onePets.Select(p => p.folder + "=" + p.path)));
						Check("sources online: …from its own folder", one.Path == "Pets/rex" && one.BaseUrl.EndsWith("/Pets/rex/"), one.BaseUrl);
						// a repository found by a plain link: its default branch and where its pets.json is
						var plain = GallerySources.Parse(AppInfo.RepoUrl);
						var plainPets = await GallerySources.CheckAsync(plain);
						Check("sources online: a plain link finds the branch and the Pets folder", plain.Branch == AppInfo.Branch && plain.Path == "Pets" && plainPets.Count > 20,
							plain.Key + " · " + plainPets.Count);
						// a folder without pets.json, with the pets in subfolders: this repository read from its root
						var rootSource = GallerySources.Parse(AppInfo.RepoOwner + "/" + AppInfo.RepoName + "@" + AppInfo.Branch + ":.");
						var rootPets = await GallerySources.CheckAsync(rootSource);
						var rex = rootPets.FirstOrDefault(p => p.folder == "rex");
						Check("sources online: pets in subfolders are listed with their path", rex != null && rex.path == "Pets/rex" && rootSource.Path == "",
							rex == null ? string.Join(", ", rootPets.Take(5).Select(p => p.folder + "=" + p.path)) : rex.path);
						var icons = await GalleryPreview.LoadIcons(rootSource, rootPets.Where(p => p.folder == "rex").ToList());
						Check("sources online: …and their files are read from that path", icons.ContainsKey("rex"));
						foreach (var i in icons.Values) i.Dispose();
					}
					catch (InvalidDataException ex) when (ex.Message == Lang.T("sources.rateLimit")) { lines.Add("SKIP  sources online: GitHub is limiting the requests now"); }
				}
			}
			finally { Program.MyData.SetSetting("GallerySources", saved); Program.MyData.SetSetting("GalleryOriginalRemoved", removedBefore); }

			Check("names: technical names become readable",
				AnimNames.Readable("run_dl") == "Run dl" && AnimNames.Readable("Flying_Up") == "Flying up" && AnimNames.Readable("jumpUp") == "Jump up"
				&& AnimNames.Readable("walk2") == "Walk 2" && AnimNames.Readable("look around") == "look around",
				string.Join(" | ", new[] { "run_dl", "Flying_Up", "jumpUp", "walk2", "look around" }.Select(AnimNames.Readable)));

			var versions = new OptionsPet { VariantNames = new[] { "variant.active", "variant.calm" } };
			var colors = new OptionsPet { VariantNames = new[] { "color.white", "color.black" } };
			Check("groups: versions are told from colors", versions.VariantsAreVersions && !colors.VariantsAreVersions);

			// the bug: opening a pet's details in the gallery made that file the one the pet on the screen computes its
			// values with ("imageW", "screenW/imageH"…): its children and scenes went wrong until the next restart
			var running = Animations.Xml;
			var doc = new System.Xml.XmlDocument();
			doc.LoadXml(Program.MyData.GetXml());
			FormSettings.PetStats(doc, "selftest-details-" + Guid.NewGuid().ToString("N"));
			Check("gallery: a pet's details leave the pet on the screen as it is", Animations.Xml == running);

			var (title, intro, parts) = FormSettings.PrivacyText();
			var texts = new[] { title, intro }.Concat(parts.SelectMany(p => new[] { p.heading }.Concat(p.items))).ToList();
			Check("privacy: every line of the page has its text", texts.Count >= 12 && texts.All(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith("privacy.")),
				string.Join(", ", texts.Where(t => string.IsNullOrWhiteSpace(t) || t.StartsWith("privacy."))));
		}
	}
}
