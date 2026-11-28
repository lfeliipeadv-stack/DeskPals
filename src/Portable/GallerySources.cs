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
	/// One GitHub repository the gallery reads pets from: a folder of it (Pets, the layout of this repository and of the
	/// original desktopPet; or the root, another folder, or the folder of one pet) with one folder per pet
	/// (animations.xml, icon.png, README.md…). The list is its pets.json; a folder without one is listed from the
	/// folders that have an animations.xml (GitHub's file tree).
	/// </summary>
	public sealed class GallerySource
	{
		public string Owner { get; set; }
		public string Repo { get; set; }
		public string Branch { get; set; } = "master";
		/// <summary>The folder holding the pets: "Pets", "" (the root), "classic", or one pet's own folder. Null: not known yet (see <see cref="GallerySources.CheckAsync"/>).</summary>
		public string Path { get; set; }
		public bool Enabled { get; set; } = true;
		/// <summary>A source given by address (tests, "--gallery=…"), not a GitHub repository.</summary>
		internal string FixedUrl;
		/// <summary>
		/// A folder of this computer instead of a repository (the "developer folder"): each subfolder with an animations.xml
		/// is a pet (or the folder's own pets.json). Changes show at the next look at the gallery, without uploading anything.
		/// </summary>
		public string LocalFolder { get; set; }
		/// <summary>What was typed named the branch or the folder (a link to /tree/…): they are not looked for.</summary>
		internal bool BranchGiven, PathGiven;

		/// <summary>The folder holding the pets ("Pets" until it is known).</summary>
		public string PetsPath => Path ?? "Pets";

		/// <summary>This program's own repository (always first, cannot be removed).</summary>
		public bool IsOfficial => LocalFolder == null && FixedUrl != null
			|| (string.Equals(Owner, AppInfo.RepoOwner, StringComparison.OrdinalIgnoreCase)
				&& (string.Equals(Repo, AppInfo.RepoName, StringComparison.OrdinalIgnoreCase) || string.Equals(Repo, AppInfo.OldRepoName, StringComparison.OrdinalIgnoreCase))
				&& PetsPath == "Pets" && LocalFolder == null);

		/// <summary>
		/// "owner/repo", "@branch" when it is not master and ":folder" when the pets are not in Pets (":." = the root),
		/// as the settings save it.
		/// </summary>
		public string Key => LocalFolder != null ? "folder:" + LocalFolder : FixedUrl ?? (Owner + "/" + Repo
			+ (string.Equals(Branch, "master", StringComparison.OrdinalIgnoreCase) ? "" : "@" + Branch)
			+ (PetsPath == "Pets" ? "" : ":" + (PetsPath.Length == 0 ? "." : PetsPath)));

		/// <summary>Short name for the cards: the owner ("Adrianotiger").</summary>
		public string Label => LocalFolder != null ? System.IO.Path.GetFileName(LocalFolder.TrimEnd('\\', '/')) : FixedUrl != null ? "—" : Owner;

		/// <summary>"owner/repo", and the folder when it is not Pets, for the user to read.</summary>
		public string DisplayName => LocalFolder ?? FixedUrl ?? (Owner + "/" + Repo + (PetsPath == "Pets" || PetsPath.Length == 0 ? "" : "/" + PetsPath));

		/// <summary>Address of the folder holding the pets, ending with "/".</summary>
		public string BaseUrl => LocalFolder != null ? new Uri(System.IO.Path.GetFullPath(LocalFolder).TrimEnd('\\') + "\\").AbsoluteUri : FixedUrl ?? "https://raw.githubusercontent.com/" + Owner + "/" + Repo + "/" + Branch + "/" + (PetsPath.Length == 0 ? "" : PetsPath + "/");

		/// <summary>Web page of that folder.</summary>
		public string WebUrl => LocalFolder ?? FixedUrl ?? "https://github.com/" + Owner + "/" + Repo + "/tree/" + Branch + (PetsPath.Length == 0 ? "" : "/" + PetsPath);

		/// <summary>
		/// Where the files of this source are saved (icons, readmes, previews). The official one keeps the folder older
		/// versions used; every other source has a folder of its own, so two sources never mix files.
		/// </summary>
		public string CacheDir => IsOfficial ? OptionsGit.BaseLocalPath
			: System.IO.Path.Combine(OptionsGit.BaseLocalPath, "src-" + Regex.Replace(Key.ToLowerInvariant(), "[^a-z0-9]+", "-"));
	}

	/// <summary>
	/// The repositories the gallery reads, in order of preference: when two have a pet with the same folder, the first one
	/// wins. Saved in the setting "GallerySources" as lines "owner/repo[@branch]|1" (1 = on, 0 = off).
	/// </summary>
	public static class GallerySources
	{
		/// <summary>
		/// The pets of a folder of this computer: its pets.json when it has one, else every subfolder (one level, and the
		/// folder itself) with an animations.xml. The version is the date of the file, so an edited pet is read again.
		/// </summary>
		public static List<OptionsPet> ScanLocal(string folder)
		{
			try
			{
				string list = System.IO.Path.Combine(folder, "pets.json");
				if (System.IO.File.Exists(list))
				{
					var pets = OptionsGit.Normalize(JsonText.Deserialize<OptionsPets>(System.IO.File.ReadAllText(list))?.pets);
					if (pets.Count > 0) return pets;
				}
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			var result = new List<OptionsPet>();
			var dirs = new List<string>();
			if (System.IO.File.Exists(System.IO.Path.Combine(folder, "animations.xml"))) dirs.Add(folder);
			try { dirs.AddRange(System.IO.Directory.GetDirectories(folder).Where(d => System.IO.File.Exists(System.IO.Path.Combine(d, "animations.xml"))).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).Take(300)); }
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			foreach (var dir in dirs)
			{
				string file = System.IO.Path.Combine(dir, "animations.xml");
				string head = "";
				try { using (var r = new System.IO.StreamReader(file)) { var buf = new char[8192]; int n = r.Read(buf, 0, buf.Length); head = new string(buf, 0, n); } }
				catch (Exception skipped) { LogFile.Ignored(skipped); continue; }
				string Tag(string t) { var m = Regex.Match(head, "<" + t + @"[^>]*>\s*(?:<!\[CDATA\[)?(.*?)(?:\]\]>)?\s*</" + t + ">", RegexOptions.Singleline | RegexOptions.IgnoreCase); return m.Success ? System.Net.WebUtility.HtmlDecode(m.Groups[1].Value.Trim()) : null; }
				string rel = dir.Length > folder.Length ? dir.Substring(folder.Length).Trim('\\', '/').Replace('\\', '/') : "";
				string name = rel.Length > 0 ? rel.Substring(rel.LastIndexOf('/') + 1) : System.IO.Path.GetFileName(folder);
				string plain = Regex.Replace(name, "[^A-Za-z0-9_.-]+", "_").Trim('_', '.');
				if (plain.Length == 0) plain = "pet";
				var when = System.IO.File.GetLastWriteTime(file);
				result.Add(new OptionsPet
				{
					folder = plain,
					path = rel.Length == 0 ? "" : (rel == plain ? null : rel),
					name = Tag("petname") ?? Tag("title") ?? name,
					author = Tag("author") ?? "",
					lastupdate = when.ToString("yyyy-MM-dd HH:mm:ss"),
					rev = when.Ticks.ToString(),
				});
			}
			return OptionsGit.Normalize(result, localFolder: true);
		}

		const string Setting = "GallerySources";
		/// <summary>
		/// The original desktopPet of Adriano Petrucci. It is not a default source any more (from 1.1.2): whoever wants its
		/// pets adds it in "Where the pets come from" (it then shows with its own description).
		/// </summary>
		public const string OriginalOwner = "Adrianotiger", OriginalRepo = "desktopPet";

		public static GallerySource Official => new GallerySource { Owner = AppInfo.RepoOwner, Repo = AppInfo.RepoName, Branch = AppInfo.Branch, Path = "Pets" };

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
				if (s != null) s.Path ??= "Pets";   // saved without ":folder": the Pets folder
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
		/// A repository from what the user typed or the settings saved: "owner/repo", "owner/repo@branch",
		/// "owner/repo[@branch]:folder" (":." = the root), or a GitHub link: https://github.com/owner/repo,
		/// …/tree/branch/folder (a folder of pets, or one pet's folder), …/blob/branch/folder/animations.xml (its folder).
		/// Null when it is not one. The branch and the folder not given are found by <see cref="CheckAsync"/>.
		/// </summary>
		public static GallerySource Parse(string text)
		{
			text = (text ?? "").Trim().Trim('"').TrimEnd('/');
			if (text.Length == 0) return null;
			// a folder of this computer ("folder:C:\Pets" as saved, or the path itself)
			string local = text.StartsWith("folder:", StringComparison.OrdinalIgnoreCase) ? text.Substring(7) : text;
			if (local.Length > 2 && (System.IO.Path.IsPathRooted(local) && (local[1] == ':' || local.StartsWith(@"\\"))))
			{
				try { local = System.IO.Path.GetFullPath(local).TrimEnd('\\'); } catch (Exception) { return null; }
				return new GallerySource { LocalFolder = local, Owner = "", Repo = "", Path = "" };
			}
			const string name = @"[A-Za-z0-9](?:[A-Za-z0-9._-]{0,99})";
			string branch = null, path = null;
			var m = Regex.Match(text, @"^(?:https?://)?(?:www\.)?github\.com/(" + name + ")/(" + name + @")(?:\.git)?(?:/(tree|blob)/([^/\s?#]+)(/[^\s?#]*)?)?/?(?:[?#].*)?$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				if (m.Groups[4].Success) branch = Uri.UnescapeDataString(m.Groups[4].Value);
				string rest = m.Groups[5].Success ? Uri.UnescapeDataString(m.Groups[5].Value).Trim('/') : "";
				bool file = m.Groups[3].Value.Equals("blob", StringComparison.OrdinalIgnoreCase);
				// a link to a file (animations.xml, pets.json, a readme…) means its folder
				if (file) path = rest.Contains('/') ? rest.Substring(0, rest.LastIndexOf('/')) : "";
				else if (rest.Length > 0) path = rest;
			}
			else
			{
				m = Regex.Match(text, "^(" + name + ")/(" + name + @")(?:@([^\s:]+))?(?::([^\s]*))?$");
				if (!m.Success) return null;
				if (m.Groups[3].Success && m.Groups[3].Value.Length > 0) branch = m.Groups[3].Value;
				if (m.Groups[4].Success) path = m.Groups[4].Value == "." ? "" : m.Groups[4].Value.Trim('/');
			}
			if (path != null && path.Length > 0 && !OptionsGit.IsSafePath(path)) return null;
			return new GallerySource
			{
				Owner = m.Groups[1].Value, Repo = m.Groups[2].Value,
				Branch = branch ?? "master", BranchGiven = branch != null,
				Path = path, PathGiven = path != null,
			};
		}

		/// <summary>
		/// The repository really has pets. Finds what was not given: the branch (the repository's default one, asked to
		/// GitHub; master or main when GitHub does not answer), the folder (Pets, then the root) and the list (its
		/// pets.json, or else every folder with an animations.xml). <paramref name="source"/> gets the branch and the
		/// folder found. Returns the pets; throws with a message for the user when there are none, the repository does
		/// not exist (or is private), or GitHub is limiting the requests.
		/// </summary>
		public static async Task<List<OptionsPet>> CheckAsync(GallerySource source)
		{
			if (source.LocalFolder != null)
			{
				if (!System.IO.Directory.Exists(source.LocalFolder)) throw new System.IO.InvalidDataException(Lang.T("sources.localMissing", source.LocalFolder));
				var found = await Task.Run(() => ScanLocal(source.LocalFolder));
				if (found.Count == 0) throw new System.IO.InvalidDataException(Lang.T("sources.localNone", source.LocalFolder));
				return found;
			}
			string repo = source.Owner + "/" + source.Repo;
			bool limited = false;
			using (var http = Client())
			{
				var branches = new List<string> { source.Branch };
				if (!source.BranchGiven)
				{
					var (status, json, rateLimited) = await GetApi(http, "repos/" + source.Owner + "/" + source.Repo);
					if (status == System.Net.HttpStatusCode.NotFound) throw new InvalidDataException(Lang.T("sources.notFound", repo));
					limited |= rateLimited;
					string def = null;
					try { if (json != null) def = System.Text.Json.Nodes.JsonNode.Parse(json)?["default_branch"]?.GetValue<string>(); }
					catch (Exception skipped) { LogFile.Ignored(skipped); }
					branches = def != null ? new List<string> { def } : new List<string> { "master", "main" };
				}
				var folders = source.PathGiven ? new[] { source.Path } : new[] { "Pets", "" };
				// the list the author keeps: pets.json
				foreach (string branch in branches)
					foreach (string folder in folders)
					{
						var probe = new GallerySource { Owner = source.Owner, Repo = source.Repo, Branch = branch, Path = folder };
						using (var response = await http.GetAsync(probe.BaseUrl + "pets.json"))
						{
							if (!response.IsSuccessStatusCode) continue;
							var bytes = await response.Content.ReadAsByteArrayAsync();
							if (bytes.Length > OptionsGit.MaxListBytes) throw new InvalidDataException("pets.json: too big");
							List<OptionsPet> listed;
							try { listed = JsonText.Deserialize<OptionsPets>(System.Text.Encoding.UTF8.GetString(bytes).TrimStart('﻿'))?.pets; }
							catch (Exception) { continue; }   // a file with that name that is not a pet list
							if (listed == null) continue;
							var pets = OptionsGit.Normalize(listed);
							if (pets.Count == 0) continue;
							source.Branch = branch; source.Path = folder;
							return pets;
						}
					}
				// no list: every folder with an animations.xml
				foreach (string branch in branches)
				{
					var probe = new GallerySource { Owner = source.Owner, Repo = source.Repo, Branch = branch, Path = source.PathGiven ? source.Path : null };
					List<OptionsPet> found;
					try { found = await ScanAsync(probe, http); }
					catch (RateLimitedException) { limited = true; continue; }
					if (found == null || found.Count == 0) continue;
					source.Branch = branch; source.Path = probe.Path;
					return OptionsGit.Normalize(found);
				}
			}
			throw new InvalidDataException(Lang.T(limited ? "sources.rateLimit" : "sources.notGallery", source.DisplayName));
		}

		/// <summary>GitHub said no more requests for now (60 an hour without signing in).</summary>
		internal sealed class RateLimitedException : Exception { }

		static HttpClient Client()
		{
			var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
			http.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
			return http;
		}

		/// <summary>A GitHub API answer: its status, its text (null when it failed) and whether the limit was reached.</summary>
		static async Task<(System.Net.HttpStatusCode status, string json, bool limited)> GetApi(HttpClient http, string what)
		{
			try
			{
				using (var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/" + what))
				{
					request.Headers.Accept.ParseAdd("application/vnd.github+json");
					using (var response = await http.SendAsync(request))
					{
						bool limited = (response.StatusCode == System.Net.HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
							&& (!response.Headers.TryGetValues("X-RateLimit-Remaining", out var left) || left.FirstOrDefault() == "0");
						string body = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
						return (response.StatusCode, body, limited);
					}
				}
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); return (0, null, false); }
		}

		/// <summary>Most pets one folder may list without a pets.json.</summary>
		const int MaxScanned = 500;

		/// <summary>
		/// A list made from GitHub's file tree, for a repository without pets.json: every folder with an animations.xml
		/// inside <see cref="GallerySource.Path"/> (when it is null, Pets if every pet is there, else the root; it is set).
		/// A pet right in that folder (a link to one pet) is listed too. Each pet's "rev" is its animations.xml's own
		/// id: it changes when the file does, so updates are seen without a date. Null when the branch does not exist.
		/// </summary>
		internal static async Task<List<OptionsPet>> ScanAsync(GallerySource source, HttpClient http = null)
		{
			bool own = http == null;
			http ??= Client();
			try
			{
				var (status, json, limited) = await GetApi(http, "repos/" + source.Owner + "/" + source.Repo + "/git/trees/" + Uri.EscapeDataString(source.Branch) + "?recursive=1");
				if (limited) throw new RateLimitedException();
				if (json == null) return null;
				var tree = System.Text.Json.Nodes.JsonNode.Parse(json)?["tree"] as System.Text.Json.Nodes.JsonArray;
				if (tree == null) return null;
				var xmls = tree.Where(e => e?["type"]?.GetValue<string>() == "blob")
					.Select(e => (path: e["path"]?.GetValue<string>() ?? "", sha: e["sha"]?.GetValue<string>() ?? ""))
					.Where(e => e.path == "animations.xml" || e.path.EndsWith("/animations.xml", StringComparison.Ordinal)).ToList();
				if (source.Path == null)
					source.Path = xmls.Count > 0 && xmls.All(x => x.path.StartsWith("Pets/", StringComparison.Ordinal)) ? "Pets" : "";
				string prefix = source.Path.Length == 0 ? "" : source.Path + "/";
				var pets = new List<OptionsPet>();
				foreach (var x in xmls)
				{
					if (!x.path.StartsWith(prefix, StringComparison.Ordinal)) continue;
					string dir = x.path.Substring(prefix.Length, x.path.Length - prefix.Length - "animations.xml".Length).TrimEnd('/');
					string id = dir.Length > 0 ? dir : (source.Path.Length > 0 ? source.Path.Substring(source.Path.LastIndexOf('/') + 1) : source.Repo);
					pets.Add(new OptionsPet { folder = id, path = dir, author = source.Owner, lastupdate = "", rev = x.sha });
					if (pets.Count >= MaxScanned) break;
				}
				return pets;
			}
			finally { if (own) http.Dispose(); }
		}
	}
}
