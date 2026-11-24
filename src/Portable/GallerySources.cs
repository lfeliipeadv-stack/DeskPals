using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DesktopPet
{
	/// <summary>
	/// One GitHub repository the gallery reads pets from: it has a Pets folder with pets.json (the list) and one folder
	/// per pet (animations.xml, icon.png, README.md…), the layout of this repository and of the original desktopPet.
	/// </summary>
	public sealed class GallerySource
	{
		public string Owner { get; set; }
		public string Repo { get; set; }
		public string Branch { get; set; } = "master";
		public bool Enabled { get; set; } = true;
		/// <summary>A source given by address (tests, "--gallery=…"), not a GitHub repository.</summary>
		internal string FixedUrl;

		/// <summary>This program's own repository (always first, cannot be removed).</summary>
		public bool IsOfficial => FixedUrl != null
			|| (string.Equals(Owner, AppInfo.RepoOwner, StringComparison.OrdinalIgnoreCase)
				&& (string.Equals(Repo, AppInfo.RepoName, StringComparison.OrdinalIgnoreCase) || string.Equals(Repo, AppInfo.OldRepoName, StringComparison.OrdinalIgnoreCase)));

		/// <summary>"owner/repo" (and "@branch" when it is not master), as the settings save it.</summary>
		public string Key => FixedUrl ?? (Owner + "/" + Repo + (string.Equals(Branch, "master", StringComparison.OrdinalIgnoreCase) ? "" : "@" + Branch));

		/// <summary>Short name for the cards: the owner ("Adrianotiger").</summary>
		public string Label => FixedUrl != null ? "—" : Owner;

		/// <summary>Address of the Pets folder, ending with "/".</summary>
		public string BaseUrl => FixedUrl ?? "https://raw.githubusercontent.com/" + Owner + "/" + Repo + "/" + Branch + "/Pets/";

		/// <summary>Web page of the repository's Pets folder.</summary>
		public string WebUrl => FixedUrl ?? "https://github.com/" + Owner + "/" + Repo + "/tree/" + Branch + "/Pets";

		/// <summary>
		/// Where the files of this source are saved (icons, readmes, previews). The official one keeps the folder older
		/// versions used; every other source has a folder of its own, so two sources never mix files.
		/// </summary>
		public string CacheDir => IsOfficial ? OptionsGit.BaseLocalPath
			: Path.Combine(OptionsGit.BaseLocalPath, "src-" + Regex.Replace(Key.ToLowerInvariant(), "[^a-z0-9]+", "-"));
	}

	/// <summary>
	/// The repositories the gallery reads, in order of preference: when two have a pet with the same folder, the first one
	/// wins. Saved in the setting "GallerySources" as lines "owner/repo[@branch]|1" (1 = on, 0 = off).
	/// </summary>
	public static class GallerySources
	{
		const string Setting = "GallerySources";
		/// <summary>
		/// The original desktopPet of Adriano Petrucci. It is not a default source any more (from 1.1.2): whoever wants its
		/// pets adds it in "Where the pets come from" (it then shows with its own description).
		/// </summary>
		public const string OriginalOwner = "Adrianotiger", OriginalRepo = "desktopPet";

		public static GallerySource Official => new GallerySource { Owner = AppInfo.RepoOwner, Repo = AppInfo.RepoName, Branch = AppInfo.Branch };

		static List<GallerySource> Defaults() => new List<GallerySource> { Official };

		/// <summary>Set once the original desktopPet, a default source until 1.1.1, was taken out of a saved list.</summary>
		const string OriginalRemovedSetting = "GalleryOriginalRemoved";

		/// <summary>Every source of the settings (the official one first, even when the setting lost it).</summary>
		public static List<GallerySource> All()
		{
			string saved = Program.MyData?.GetSetting(Setting, "") ?? "";
			if (string.IsNullOrWhiteSpace(saved)) return Defaults();
			// a list saved until 1.1.1 has the original desktopPet, which was a default then: it goes once. Added again
			// by hand afterwards, it stays.
			if (Program.MyData != null && !Program.MyData.GetSettingBool(OriginalRemovedSetting, false))
			{
				Program.MyData.SetSetting(OriginalRemovedSetting, true);
				string original = OriginalOwner + "/" + OriginalRepo;
				var kept = saved.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(l => !l.Trim().Split('|')[0].Trim().Equals(original, StringComparison.OrdinalIgnoreCase)).ToList();
				saved = string.Join("\n", kept);
				Program.MyData.SetSetting(Setting, saved);
				if (string.IsNullOrWhiteSpace(saved)) return Defaults();
			}
			var list = new List<GallerySource>();
			foreach (var line in saved.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var parts = line.Trim().Split('|');
				var s = Parse(parts[0]);
				if (s != null && s.FixedUrl == null && s.IsOfficial) s.Repo = AppInfo.RepoName;   // saved before the repository was renamed
				if (s == null || list.Any(x => x.Key.Equals(s.Key, StringComparison.OrdinalIgnoreCase))) continue;
				s.Enabled = parts.Length < 2 || parts[1].Trim() != "0";
				list.Add(s);
			}
			var official = list.FirstOrDefault(s => s.IsOfficial);
			if (official == null) list.Insert(0, Official);
			else { list.Remove(official); official.Enabled = true; list.Insert(0, official); }
			return list;
		}

		public static void Save(IEnumerable<GallerySource> sources) =>
			Program.MyData?.SetSetting(Setting, string.Join("\n", sources.Select(s => s.Key + "|" + (s.Enabled ? "1" : "0"))));

		/// <summary>
		/// The sources the gallery reads now: the ones turned on. "--gallery=address" (tests) replaces them all.
		/// </summary>
		public static List<GallerySource> Active()
		{
			if (!string.Equals(OptionsGit.BaseGitUrl, AppInfo.PetsRawUrl, StringComparison.OrdinalIgnoreCase))
				return new List<GallerySource> { new GallerySource { FixedUrl = OptionsGit.BaseGitUrl } };
			return All().Where(s => s.Enabled).ToList();
		}

		/// <summary>
		/// A repository from what the user typed: "owner/repo", "owner/repo@branch" or a GitHub link
		/// (https://github.com/owner/repo, …/tree/branch/Pets). Null when it is not one.
		/// </summary>
		public static GallerySource Parse(string text)
		{
			text = (text ?? "").Trim().TrimEnd('/');
			if (text.Length == 0) return null;
			const string name = @"[A-Za-z0-9](?:[A-Za-z0-9._-]{0,99})";
			var m = Regex.Match(text, @"^(?:https?://)?(?:www\.)?github\.com/(" + name + ")/(" + name + @")(?:\.git)?(?:/tree/([^/\s]+)(?:/.*)?)?$", RegexOptions.IgnoreCase);
			if (!m.Success) m = Regex.Match(text, "^(" + name + ")/(" + name + @")(?:@([^\s/]+))?$");
			if (!m.Success) return null;
			string branch = m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? m.Groups[3].Value : "master";
			return new GallerySource { Owner = m.Groups[1].Value, Repo = m.Groups[2].Value, Branch = branch };
		}

		/// <summary>
		/// The repository really is a pet gallery: its Pets/pets.json lists pets. Returns how many; throws with the
		/// reason when it is not one. A repository on "main" instead of "master" is found too (Branch is fixed).
		/// </summary>
		public static async Task<int> CheckAsync(GallerySource source)
		{
			async Task<int?> Try(string branch)
			{
				var probe = new GallerySource { Owner = source.Owner, Repo = source.Repo, Branch = branch };
				using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
				{
					http.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
					using (var response = await http.GetAsync(probe.BaseUrl + "pets.json"))
					{
						if (!response.IsSuccessStatusCode) return null;
						var bytes = await response.Content.ReadAsByteArrayAsync();
						if (bytes.Length > OptionsGit.MaxListBytes) throw new InvalidDataException("pets.json: too big");
						var pets = JsonText.Deserialize<OptionsPets>(System.Text.Encoding.UTF8.GetString(bytes))?.pets;
						if (pets == null) throw new InvalidDataException("pets.json");
						return pets.Count(p => OptionsGit.IsPlainFolder(p?.folder));
					}
				}
			}
			int? count = await Try(source.Branch);
			if (count == null && source.Branch == "master" && (count = await Try("main")) != null) source.Branch = "main";
			if (count == null) throw new InvalidDataException(Lang.T("sources.notGallery", source.Owner + "/" + source.Repo));
			return count.Value;
		}
	}
}
