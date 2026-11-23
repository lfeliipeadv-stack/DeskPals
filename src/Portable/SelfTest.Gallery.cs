using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// The gallery's sources (this repository, the original desktopPet, others the user adds): links read right,
		/// settings kept, the official one always first, each source in its own cache folder, and online both default
		/// sources answer with the first one winning a pet both have. Also the readable names of untranslated
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

			string saved = Program.MyData.GetSetting("GallerySources", "");
			try
			{
				GallerySources.Save(new[] { new GallerySource { Owner = "x", Repo = "y", Enabled = false }, GallerySources.Official });
				var all = GallerySources.All();
				Check("sources: the official one comes first and is always on", all.Count == 2 && all[0].IsOfficial && all[0].Enabled && !all[1].Enabled,
					string.Join(", ", all.Select(s => s.Key + (s.Enabled ? "" : " (off)"))));
				Check("sources: a source turned off is not read", !GallerySources.Active().Any(s => s.Owner == "x"));

				Program.MyData.SetSetting("GallerySources", "");   // the defaults
				Check("sources: by default this repository and the original desktopPet",
					GallerySources.All().Select(s => s.Key).SequenceEqual(new[] { GallerySources.Official.Key, "Adrianotiger/desktopPet" }));
				var pets = await OptionsGit.LoadLists();
				if (!OptionsGit.Online) lines.Add("SKIP  sources online: no internet");
				else
				{
					Check("sources online: both default sources answer", OptionsGit.FetchedSources.Count == 2, string.Join(", ", OptionsGit.FetchedSources));
					var neko = pets.FirstOrDefault(p => p.folder == "neko");
					Check("sources online: the first source wins a pet both have", neko?.Source?.IsOfficial == true, neko?.Source?.Key);
					Check("sources online: no pet twice", pets.Select(p => p.folder).Distinct(StringComparer.OrdinalIgnoreCase).Count() == pets.Count);
					Check("sources online: the original gallery is read", OptionsGit.SourceTotals.TryGetValue("Adrianotiger/desktopPet", out int n) && n > 20, n.ToString());
					bool refused = false;
					try { await GallerySources.CheckAsync(new GallerySource { Owner = AppInfo.RepoOwner, Repo = "no-such-gallery-for-the-self-test" }); }
					catch (InvalidDataException) { refused = true; }
					Check("sources online: a repository without Pets/pets.json is refused", refused);
				}
			}
			finally { Program.MyData.SetSetting("GallerySources", saved); }

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
