using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PetEditor
{
	/// <summary>
	/// Where the pets of "Open from the gallery" come from: DeskPals' gallery and any GitHub repository the user pastes,
	/// as in DeskPals and the online editor. Looked for in this order: the pets.json of the folder (Pets, then the root),
	/// then, without a list, every folder with an animations.xml (GitHub's file tree; 60 questions an hour without
	/// signing in). The sources are kept in the editor's settings.
	/// </summary>
	internal static class WebPets
	{
		public sealed class Source
		{
			public string Owner, Repo, Branch, Path;
			public bool Scanned, Official;
			public string Key => (Owner + "/" + Repo).ToLowerInvariant() + (string.IsNullOrEmpty(Path) ? "" : ":" + Path.ToLowerInvariant());
			public string Label => Official ? "DeskPals" : Owner + "/" + Repo + (string.IsNullOrEmpty(Path) || Path == "Pets" ? "" : "/" + Path);
			/// <summary>The address of the folder holding the pets, ending with "/".</summary>
			public string BaseUrl => "https://raw.githubusercontent.com/" + Owner + "/" + Repo + "/" + Enc(Branch) + "/" + (string.IsNullOrEmpty(Path) ? "" : Enc(Path) + "/");
			public string Saved => Owner + "/" + Repo + "|" + Branch + "|" + (Path ?? "") + "|" + (Scanned ? "1" : "0");
		}

		public sealed class Pet
		{
			/// <summary>The name of the pet's folder; <see cref="Dir"/> where its files are (null: the folder itself under the source).</summary>
			public string Folder, Dir, Name, Author;
			public bool Icon = true;
			public string Shown => !string.IsNullOrWhiteSpace(Name) ? Name.Trim() : string.Join(" ", Regex.Split(Folder, "[_-]+").Where(w => w.Length > 0).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
			public string Url(Source s) => s.BaseUrl + (Dir != null ? (Dir.Length == 0 ? "" : Enc(Dir) + "/") : Enc(Folder) + "/");
		}

		/// <summary>A search that did not work, with the text for the user.</summary>
		public sealed class Problem : Exception { public Problem(string message) : base(message) { } }

		/// <summary>Most pets a search lists without a pets.json.</summary>
		public const int MaxScanned = 500;

		public static Source Official => new() { Owner = DesktopPet.AppInfo.RepoOwner, Repo = DesktopPet.AppInfo.RepoName, Branch = DesktopPet.AppInfo.Branch, Path = "Pets", Official = true };

		const string Name = "[A-Za-z0-9](?:[A-Za-z0-9._-]{0,99})";

		static string Enc(string p) => string.Join("/", (p ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

		/// <summary>A folder without "..", without a slash at the start and without strange characters.</summary>
		static bool SafePath(string p) => p.Split('/').All(x => x.Length > 0 && x != "." && x != ".." && x.IndexOfAny(new[] { '\\', ':', '*', '?', '"', '<', '>', '|' }) < 0);

		/// <summary>What a link names: the repository, and the branch and folder when it says them (null: not a GitHub link).</summary>
		public sealed class Link { public string Owner, Repo, Branch, Path; public bool File; }

		/// <summary>
		/// A GitHub link (github.com/owner/repo, …/tree/branch/folder, …/blob/branch/folder/animations.xml, a raw link) or
		/// "owner/repo". Null when it is not one. A link to a file stands for its folder.
		/// </summary>
		public static Link Parse(string text)
		{
			text = (text ?? "").Trim().TrimEnd('/');
			if (text.Length == 0) return null;
			string owner, repo, branch = null, rest = "";
			bool file = false;
			var m = Regex.Match(text, $@"^(?:https?://)?(?:www\.)?github\.com/({Name})/({Name})(?:\.git)?(?:/(tree|blob)/([^/\s?#]+)(/[^\s?#]*)?)?/?(?:[?#].*)?$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				owner = m.Groups[1].Value; repo = m.Groups[2].Value;
				if (m.Groups[4].Success) branch = Uri.UnescapeDataString(m.Groups[4].Value);
				rest = m.Groups[5].Success ? Uri.UnescapeDataString(m.Groups[5].Value).Trim('/') : "";
				file = m.Groups[3].Value.Equals("blob", StringComparison.OrdinalIgnoreCase);
			}
			else if ((m = Regex.Match(text, $@"^(?:https?://)?raw\.githubusercontent\.com/({Name})/({Name})/([^/\s?#]+)(/[^\s?#]*)?$", RegexOptions.IgnoreCase)).Success)
			{
				owner = m.Groups[1].Value; repo = m.Groups[2].Value; branch = Uri.UnescapeDataString(m.Groups[3].Value);
				rest = m.Groups[4].Success ? Uri.UnescapeDataString(m.Groups[4].Value).Trim('/') : "";
				file = Regex.IsMatch(rest, @"\.[a-z0-9]+$", RegexOptions.IgnoreCase);
			}
			else if ((m = Regex.Match(text, $"^({Name})/({Name})$")).Success) { owner = m.Groups[1].Value; repo = m.Groups[2].Value; }
			else return null;
			string path = null;
			if (file) path = rest.Contains('/') ? rest[..rest.LastIndexOf('/')] : "";
			else if (rest.Length > 0) path = rest;
			if (!string.IsNullOrEmpty(path) && !SafePath(path)) return null;
			if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) repo = repo[..^4];
			return new Link { Owner = owner, Repo = repo, Branch = branch, Path = path, File = file };
		}

		static HttpClient http;
		static HttpClient Http
		{
			get
			{
				if (http != null) return http;
				http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
				http.DefaultRequestHeaders.UserAgent.ParseAdd("DeskPals-Editor");   // GitHub asks every program to say its name
				return http;
			}
		}

		/// <summary>A file of the internet as bytes (null: not there).</summary>
		public static async Task<byte[]> Bytes(string url)
		{
			try
			{
				using var r = await Http.GetAsync(url);
				return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
			}
			catch (Exception) { return null; }
		}

		sealed class Api { public HttpStatusCode Status; public JsonDocument Json; public bool Limited; public DateTime Reset; }

		static async Task<Api> Ask(string what)
		{
			try
			{
				using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/" + what);
				req.Headers.Accept.ParseAdd("application/vnd.github+json");
				using var r = await Http.SendAsync(req);
				string left = r.Headers.TryGetValues("x-ratelimit-remaining", out var l) ? l.FirstOrDefault() : null;
				long reset = r.Headers.TryGetValues("x-ratelimit-reset", out var rs) && long.TryParse(rs.FirstOrDefault(), out var v) ? v : 0;
				var api = new Api
				{
					Status = r.StatusCode,
					Limited = (r.StatusCode == HttpStatusCode.Forbidden || (int)r.StatusCode == 429) && (left == null || left == "0"),
					Reset = reset > 0 ? DateTimeOffset.FromUnixTimeSeconds(reset).LocalDateTime : DateTime.Now.AddHours(1),
				};
				if (r.IsSuccessStatusCode) api.Json = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
				return api;
			}
			catch (Exception) { return new Api { Status = 0 }; }
		}

		static string Str(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

		/// <summary>The pets of a pets.json (null: none there, or it is not a list of pets).</summary>
		static async Task<List<Pet>> List(Source s)
		{
			var bytes = await Bytes(s.BaseUrl + "pets.json");
			if (bytes == null || bytes.Length > 4_000_000) return null;
			try
			{
				using var doc = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes).TrimStart('﻿'));
				if (!doc.RootElement.TryGetProperty("pets", out var arr) || arr.ValueKind != JsonValueKind.Array) return null;
				var pets = new List<Pet>();
				foreach (var p in arr.EnumerateArray())
				{
					string folder = Str(p, "folder")?.Trim('/');
					if (string.IsNullOrWhiteSpace(folder)) continue;
					string dir = Str(p, "path") ?? (folder.Contains('/') ? folder : null);
					if (dir != null && dir.Length > 0 && !SafePath(dir)) continue;
					if (dir == null && !SafePath(folder)) continue;
					pets.Add(new Pet { Folder = folder[(folder.LastIndexOf('/') + 1)..], Dir = dir, Name = Str(p, "name"), Author = Str(p, "author") });
				}
				return pets.Count > 0 ? pets : null;
			}
			catch (Exception) { return null; }
		}

		/// <summary>The list made from GitHub's file tree: every folder with an animations.xml inside the source's folder.</summary>
		static async Task<List<Pet>> Scan(Source s)
		{
			var r = await Ask($"repos/{s.Owner}/{s.Repo}/git/trees/{Uri.EscapeDataString(s.Branch)}?recursive=1");
			if (r.Limited) throw new Problem(L.T("web.sourceLimit", r.Reset.ToString("t")));
			if (r.Json == null || !r.Json.RootElement.TryGetProperty("tree", out var tree) || tree.ValueKind != JsonValueKind.Array) return null;
			var blobs = tree.EnumerateArray().Where(e => Str(e, "type") == "blob" && Str(e, "path") != null).Select(e => Str(e, "path")).ToList();
			var all = blobs.ToHashSet();
			var xmls = blobs.Where(p => p == "animations.xml" || p.EndsWith("/animations.xml")).ToList();
			s.Path ??= xmls.Count > 0 && xmls.All(x => x.StartsWith("Pets/")) ? "Pets" : "";
			string prefix = s.Path.Length > 0 ? s.Path + "/" : "";
			var pets = new List<Pet>();
			foreach (var x in xmls)
			{
				if (!x.StartsWith(prefix)) continue;
				string dir = x[prefix.Length..^"animations.xml".Length].TrimEnd('/');
				string folder = dir.Length > 0 ? dir[(dir.LastIndexOf('/') + 1)..] : s.Path.Length > 0 ? s.Path[(s.Path.LastIndexOf('/') + 1)..] : s.Repo;
				pets.Add(new Pet { Folder = folder, Dir = dir, Author = s.Owner, Icon = all.Contains(prefix + (dir.Length > 0 ? dir + "/" : "") + "icon.png") });
				if (pets.Count >= MaxScanned) break;
			}
			return pets;
		}

		/// <summary>The pets of a source already known (DeskPals', or a saved one).</summary>
		public static async Task<List<Pet>> Pets(Source s)
		{
			var found = s.Scanned ? await Scan(new Source { Owner = s.Owner, Repo = s.Repo, Branch = s.Branch, Path = s.Path }) : await List(s);
			if (found == null || found.Count == 0) throw new Problem(L.T("web.sourceNone", s.Label));
			return found;
		}

		/// <summary>
		/// The repository really has pets. Finds what the link did not say: the branch (the repository's default; master
		/// or main when GitHub does not answer), the folder (Pets, then the root) and the list (pets.json, or every folder
		/// with an animations.xml).
		/// </summary>
		public static async Task<(Source source, List<Pet> pets)> Check(Link given)
		{
			string owner = given.Owner, repo = given.Repo;
			List<string> branches = given.Branch != null ? new() { given.Branch } : null;
			DateTime? limited = null;
			if (branches == null)
			{
				var r = await Ask($"repos/{owner}/{repo}");
				if (r.Status == HttpStatusCode.NotFound) throw new Problem(L.T("web.sourceNotFound", owner + "/" + repo));
				if (r.Limited) limited = r.Reset;
				if (r.Json != null)
				{
					repo = Str(r.Json.RootElement, "name") ?? repo;
					if (r.Json.RootElement.TryGetProperty("owner", out var o)) owner = Str(o, "login") ?? owner;
					string def = Str(r.Json.RootElement, "default_branch");
					if (def != null) branches = new() { def };
				}
				branches ??= new() { "master", "main" };
			}
			var folders = given.Path != null ? new List<string> { given.Path } : new List<string> { "Pets", "" };
			foreach (var b in branches)
				foreach (var f in folders)
				{
					var s = new Source { Owner = owner, Repo = repo, Branch = b, Path = f };
					var pets = await List(s);
					if (pets != null) return (s, pets);
				}
			foreach (var b in branches)
			{
				var s = new Source { Owner = owner, Repo = repo, Branch = b, Path = given.Path, Scanned = true };
				List<Pet> pets;
				try { pets = await Scan(s); }
				catch (Problem) { limited ??= DateTime.Now.AddHours(1); continue; }
				if (pets != null && pets.Count > 0) return (s, pets);
			}
			throw new Problem(limited != null ? L.T("web.sourceLimit", limited.Value.ToString("t")) : L.T("web.sourceNone", owner + "/" + repo + (string.IsNullOrEmpty(given.Path) ? "" : "/" + given.Path)));
		}

		// ------------------------------------------------------------------ kept in the settings

		public static List<Source> Saved()
		{
			var list = new List<Source>();
			foreach (var line in EditorSettings.Current.Sources ?? new())
			{
				var p = line.Split('|');
				if (p.Length < 4) continue;
				var or = p[0].Split('/');
				if (or.Length != 2 || p[1].Length == 0) continue;
				list.Add(new Source { Owner = or[0], Repo = or[1], Branch = p[1], Path = p[2], Scanned = p[3] == "1" });
			}
			return list;
		}

		public static void Save(IEnumerable<Source> sources)
		{
			EditorSettings.Current.Sources = sources.Where(s => !s.Official).Select(s => s.Saved).Distinct().ToList();
			EditorSettings.Save();
		}

		/// <summary>
		/// The pet's file brought to this computer (a folder of the editor's own, never the user's pets): the path of its
		/// animations.xml. Its icon comes along when there is one.
		/// </summary>
		public static async Task<string> Download(Source s, Pet p)
		{
			string url = p.Url(s);
			var xml = await Bytes(url + "animations.xml");
			if (xml == null) throw new Problem(L.T("web.loadFail", url + "animations.xml"));
			string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopPet", "PetEditor", "online",
				Regex.Replace((s.Owner + "-" + s.Repo + "-" + (p.Dir ?? p.Folder)).ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-'));
			Directory.CreateDirectory(dir);
			string file = System.IO.Path.Combine(dir, "animations.xml");
			await File.WriteAllBytesAsync(file, xml);
			if (p.Icon) { var icon = await Bytes(url + "icon.png"); if (icon != null) await File.WriteAllBytesAsync(System.IO.Path.Combine(dir, "icon.png"), icon); }
			return file;
		}
	}
}
