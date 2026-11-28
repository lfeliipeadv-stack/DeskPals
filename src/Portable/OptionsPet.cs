using System;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DesktopPet
{
	// The pets of the gallery as the settings window lists them (read by FormSettings).

	/// <summary>
	/// Represents a pet with its folder, author, and last update date.
	/// Base-Info to list in the option page.
	/// </summary>
	public class OptionsPet
	{
		/// <summary>
		/// Folder on GitHub
		/// </summary>
		public string folder { get; set; }
		/// <summary>The repository this pet comes from (see <see cref="GallerySources"/>); null for pets only on this computer.</summary>
		[JsonIgnore]
		public GallerySource Source { get; set; }
		/// <summary>
		/// Display name from pets.json ("eSheep", "Ano Nym XL" …). Optional: the folder name is used when missing.
		/// </summary>
		public string name { get; set; }
		/// <summary>
		/// Human friendly name: group name, then <see cref="name"/>, then the folder ("gsheep-green" -> "Gsheep Green")
		/// </summary>
		[JsonIgnore]
		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrEmpty(GroupName)) return GroupName;
				if (!string.IsNullOrWhiteSpace(name)) return name;
				var parts = (folder ?? "").Replace('_', ' ').Replace('-', ' ').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < parts.Length; i++)
					parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
				return string.Join(" ", parts);
			}
		}
		/// <summary>
		/// Author name
		/// </summary>
		public string author { get; set; }
		/// <summary>
		/// Last update of the animation file
		/// </summary>
		public string lastupdate { get; set; }
		/// <summary>
		/// Size in bytes of animations.xml (pets.json, optional; asked to the server when missing).
		/// Shown on the download button before downloading.
		/// </summary>
		public long size { get; set; }
		/// <summary>Number of animations (pets.json, optional), shown before the pet is downloaded.</summary>
		public int animations { get; set; }
		/// <summary>Number of sounds (pets.json, optional).</summary>
		public int sounds { get; set; }
		/// <summary>
		/// Where the pet's files are inside its source's folder, when that is not <see cref="folder"/> (pets.json,
		/// optional): "classic/bowser_jr", or "" for a pet right in that folder. <see cref="OptionsGit.Normalize"/> fills it
		/// for a list that writes the path in "folder", and keeps "folder" a plain name for this computer.
		/// </summary>
		public string path { get; set; }
		/// <summary>The id of the pet's animations.xml on GitHub, for a list made without pets.json (it has no date).</summary>
		public string rev { get; set; }
		/// <summary>What tells a new version of the pet: its date, or the id of its file when it has no date.</summary>
		[JsonIgnore]
		public string Version => string.IsNullOrEmpty(lastupdate) ? (rev ?? "") : lastupdate;

		/// <summary>
		/// Image to show in the windows95-explorer on the left side
		/// </summary>
		[JsonIgnore]
		public Image Image { get; set; } = null;
		/// <summary>The pet moving (Pets/&lt;folder&gt;/preview.gif), shown in the detail before downloading. Null until loaded.</summary>
		[JsonIgnore]
		public Image Preview { get; set; }
		[JsonIgnore]
		public bool PreviewTried { get; set; }
		/// <summary>
		/// Readme to parse and show on the page
		/// </summary>
		public List<String> Readme { get; set; } = new List<string>();
		/// <summary>Language the <see cref="Readme"/> was loaded in (it is loaded again after a language change).</summary>
		[JsonIgnore]
		public string ReadmeLang { get; set; }
		/// <summary>
		/// Detail XML, loaded directly from XML animation file
		/// </summary>
		[JsonIgnore]
		public XmlDocument Detail { get; set; } = null;

		/// <summary>
		/// Name of a group of pets that only differ in color (e.g. "gSheep"), or null.
		/// A group card shows the selected variant: <see cref="folder"/>, <see cref="Image"/> … are copied from it.
		/// </summary>
		[JsonIgnore]
		public string GroupName { get; set; }
		/// <summary>Color variants of a group card, in display order (null entries are not loaded yet).</summary>
		[JsonIgnore]
		public OptionsPet[] Variants { get; set; }
		/// <summary>Display names of the variants (e.g. color names), same order as <see cref="Variants"/>.</summary>
		[JsonIgnore]
		public string[] VariantNames { get; set; }
		/// <summary>Swatch colors of the variants, same order as <see cref="Variants"/>.</summary>
		[JsonIgnore]
		public Color[] VariantColors { get; set; }

		/// <summary>The variants are versions of one pet (Ano Nym, Jerma985), not colors: the card says "versions".</summary>
		[JsonIgnore]
		public bool VariantsAreVersions => VariantNames != null && VariantNames.Length > 0 && VariantNames[0].StartsWith("variant.", StringComparison.Ordinal);

		/// <summary>Index of the selected variant, or -1.</summary>
		[JsonIgnore]
		public int SelectedVariant => Variants == null ? -1 : Array.FindIndex(Variants, v => v != null && v.folder == folder);

		/// <summary>True if this card (or one of its variants) is the given folder.</summary>
		public bool Contains(string petFolder)
		{
			if (string.IsNullOrEmpty(petFolder)) return false;
			if (folder == petFolder) return true;
			return Variants != null && Array.Exists(Variants, v => v != null && v.folder == petFolder);
		}

		/// <summary>Show a variant on this group card (copies its data).</summary>
		public void SelectVariant(OptionsPet v)
		{
			if (v == null) return;
			folder = v.folder; name = v.name; author = v.author; lastupdate = v.lastupdate; Source = v.Source; path = v.path; rev = v.rev;
			size = v.size; animations = v.animations; sounds = v.sounds;
			Image = v.Image; Preview = v.Preview; PreviewTried = v.PreviewTried; Readme = v.Readme; ReadmeLang = v.ReadmeLang; Detail = v.Detail;
		}
	}
	/// <summary>
	/// Class conaining a list of pets and a method to reorder them by last update date.
	/// </summary>
	public class OptionsPets
	{
		/// <summary>
		/// List of pets
		/// </summary>
		public List<OptionsPet> pets { get; set; }
		/// <summary>The newest pets first (the order of the gallery).</summary>
		public void Reorder() => pets.Sort((x, y) => y.lastupdate.CompareTo(x.lastupdate));

	}

	/// <summary>
	/// All GitHub functionality to the retrieve pet information and files is in this class. 
	/// It downloads the pets.json file from GitHub, checks if it is updated, and downloads the pet icon and readme files as needed.
	/// </summary>
	public class OptionsGit
	{
		/// <summary>
		/// Address of the official gallery ("--gallery=address" replaces it, and every other source, for tests).
		/// The sources the gallery reads are in <see cref="GallerySources"/>.
		/// </summary>
		public static string BaseGitUrl = AppInfo.PetsRawUrl;
		/// <summary>
		/// Local temporary path, to store the github files.
		/// </summary>
		public static string BaseLocalPath = Path.Combine(Path.GetTempPath(), "esheep64");

		/// <summary>A list bigger than this is not a pet list.</summary>
		internal const long MaxListBytes = 2 * 1024 * 1024;
		/// <summary>Files of a pet (icon, readme, preview, animations) are refused above this size.</summary>
		internal const long MaxFileBytes = 40 * 1024 * 1024;

		/// <summary>Which source each pet of the last list came from (folder → source; the first source wins).</summary>
		static Dictionary<string, GallerySource> sourceOf = new Dictionary<string, GallerySource>(StringComparer.OrdinalIgnoreCase);

		/// <summary>The source a pet of the gallery comes from (the official one when it is not in the list).</summary>
		public static GallerySource SourceOf(string folder)
		{
			var map = sourceOf;
			return map.TryGetValue(folder ?? "", out var s) ? s : GallerySources.Active().FirstOrDefault(x => x.IsOfficial) ?? GallerySources.Official;
		}

		/// <summary>A folder name of a list is a plain name (never a path that leaves the cache or the store).</summary>
		internal static bool IsPlainFolder(string folder) =>
			!string.IsNullOrWhiteSpace(folder) && folder.Length <= 80 && folder.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.') && !folder.Contains("..");

		/// <summary>A folder inside a repository ("classic/bowser_jr"): plain names joined by "/", never ".." nor a full path.</summary>
		internal static bool IsSafePath(string path) =>
			!string.IsNullOrEmpty(path) && path.Length <= 200 && path.Split('/').All(IsPlainFolder);

		/// <summary>Where each pet of the last list is inside its source's folder (folder → path; "" = the folder itself).</summary>
		static Dictionary<string, string> pathOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>Where a pet's files are inside its source's folder (its folder name, unless the list said otherwise).</summary>
		internal static string PathOf(string folder) => pathOf.TryGetValue(folder ?? "", out var p) ? p : folder;

		/// <summary>The address of one file of a pet of the gallery.</summary>
		internal static string PetUrl(string folder, string file)
		{
			string p = PathOf(folder);
			return SourceOf(folder).BaseUrl + (string.IsNullOrEmpty(p) ? "" : string.Join("/", p.Split('/').Select(Uri.EscapeDataString)) + "/") + file;
		}

		/// <summary>
		/// The pets of a list as this program keeps them: "folder" a plain name (the name on this computer) and "path"
		/// where the files are. A list may write a path in "folder" ("classic/bowser_jr", the layout of some galleries):
		/// the pet keeps the last name ("bowser_jr", or the whole path with "-" when that name is taken). Entries that
		/// would point outside their folder are left out.
		/// </summary>
		internal static List<OptionsPet> Normalize(IEnumerable<OptionsPet> pets, bool localFolder = false)
		{
			// a folder of this computer: its folders may have spaces and accents ("my pets\Rex 2"), never ".."
			bool Safe(string w) => localFolder
				? w.Length <= 240 && !w.Split('/').Any(s => s.Length == 0 || s == ".." || s == "." || s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
				: IsSafePath(w);
			var all = (pets ?? Enumerable.Empty<OptionsPet>()).Where(p => p != null).ToList();
			var id = new Dictionary<OptionsPet, string>();
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			// a pet in a plain folder keeps its name; one in a subfolder takes the last name if it is free
			foreach (var p in all.Where(p => p.path == null && IsPlainFolder(p.folder)))
				if (used.Add(p.folder)) id[p] = p.folder;
			foreach (var p in all.Where(p => !id.ContainsKey(p)))
			{
				if (p.path == null && IsPlainFolder(p.folder)) continue;   // a second pet with the same plain name
				string where = (p.path ?? p.folder ?? "").Replace('\\', '/').Trim('/');
				if (where.Length > 0 && !Safe(where)) continue;
				if (where.Length == 0 && p.path == null) continue;   // no folder at all
				string name = IsPlainFolder(p.folder) ? p.folder : where.Substring(where.LastIndexOf('/') + 1);
				if (!IsPlainFolder(name) || used.Contains(name)) name = where.Replace('/', '-');
				if (!IsPlainFolder(name) || !used.Add(name)) continue;
				id[p] = name;
				p.path = where == name ? null : where;
			}
			var result = new List<OptionsPet>();
			foreach (var p in all)
				if (id.TryGetValue(p, out string n)) { p.folder = n; result.Add(p); }
			return result;
		}

		/// <summary>
		/// Get the list of pets, from Github or locally.
		/// </summary>
		public static async Task GetPetList(IProgress<OptionsPet> progress)
		{
			var pets = await LoadLists();
			foreach (var p in pets)
			{
				// one missing icon must not hide the rest of the gallery
				try
				{
					using (Stream stream = GetOrDownload(p.folder, "icon.png"))
					using (var img = Image.FromStream(stream))
						p.Image = new Bitmap(img);
				}
				catch (Exception) { p.Image = null; }
				progress.Report(p);
			}
		}

		/// <summary>
		/// The lists of every source turned on, merged: the first source wins a folder, the newest pets first.
		/// Each source's list is asked to GitHub every time (it is small and the source of new pets and updates);
		/// without internet the copy saved last time is used (the caller hides what cannot be downloaded: see Online).
		/// </summary>
		internal static async Task<List<OptionsPet>> LoadLists()
		{
			var result = new List<OptionsPet>();
			var map = new Dictionary<string, GallerySource>(StringComparer.OrdinalIgnoreCase);
			var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			bool any = false, all = true;
			var fetched = new List<string>();
			var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var source in GallerySources.Active())
			{
				var (json, online) = await FetchList(source);
				any |= online; all &= online;
				if (online) fetched.Add(source.Key);
				if (json == null) continue;
				List<OptionsPet> pets;
				try { pets = Normalize(JsonText.Deserialize<OptionsPets>(json)?.pets, source.LocalFolder != null); }
				catch (Exception ex) { LogFile.Ignored(ex); continue; }   // a broken list hides only that source
				totals[source.Key] = pets.Count;
				foreach (var p in pets)
				{
					if (map.ContainsKey(p.folder)) continue;
					p.Source = source;
					map[p.folder] = source;
					paths[p.folder] = p.path ?? p.folder;
					result.Add(p);
				}
			}
			sourceOf = map;
			pathOf = paths;
			Online = any;
			AllOnline = all;
			FetchedSources = fetched;
			SourceTotals = totals;
			// Order by date, as this is the default selection in the option window
			result.Sort((x, y) => string.CompareOrdinal(y.lastupdate ?? "", x.lastupdate ?? ""));
			return result;
		}

		/// <summary>One source's pets.json: from GitHub (saved for next time), or the saved copy. Null when neither.</summary>
		static async Task<(string json, bool online)> FetchList(GallerySource source)
		{
			string dir = source.CacheDir;
			string jsonFile = Path.Combine(dir, "pets.json");
			string cached = null, jsonString = null;
			try { Directory.CreateDirectory(dir); cached = File.Exists(jsonFile) ? File.ReadAllText(jsonFile) : null; }
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			// a folder of this computer: the list is made from its folders every time (an edited pet shows at once)
			if (source.LocalFolder != null)
			{
				if (!Directory.Exists(source.LocalFolder)) return (cached, false);
				jsonString = JsonText.SerializeIndented(new OptionsPets { pets = GallerySources.ScanLocal(source.LocalFolder) });
			}
			else try
			{
				using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
				{
					client.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
					using (var response = await client.GetAsync(source.BaseUrl + "pets.json", HttpCompletionOption.ResponseHeadersRead))
					{
						if (response.StatusCode == HttpStatusCode.NotFound && source.FixedUrl == null)
						{
							// no pets.json: the list is made from GitHub's file tree. It is asked again after 15 minutes at
							// most (GitHub answers 60 times an hour without signing in); meanwhile the one made last time
							string madeFile = Path.Combine(dir, "scanned.txt");
							if (cached != null && File.Exists(madeFile) && DateTime.Now - File.GetLastWriteTime(madeFile) < TimeSpan.FromMinutes(15))
								return (cached, true);
							var made = await GallerySources.ScanAsync(new GallerySource { Owner = source.Owner, Repo = source.Repo, Branch = source.Branch, Path = source.PetsPath });
							if (made == null) return (cached, false);
							jsonString = JsonText.SerializeIndented(new OptionsPets { pets = made });
							try { File.WriteAllText(madeFile, DateTime.Now.ToString("o")); } catch (Exception skipped) { LogFile.Ignored(skipped); }
						}
						else
						{
						response.EnsureSuccessStatusCode();
						if (response.Content.Headers.ContentLength > MaxListBytes) throw new InvalidDataException("pets.json: too big");
						var bytes = await response.Content.ReadAsByteArrayAsync();
						if (bytes.Length > MaxListBytes) throw new InvalidDataException("pets.json: too big");
						jsonString = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('﻿');
						try { File.Delete(Path.Combine(dir, "scanned.txt")); } catch (Exception skipped) { LogFile.Ignored(skipped); }
						}
					}
				}
			}
			catch (Exception)
			{
				return (cached, false);   // offline (or the source is gone): the copy saved last time, if any
			}

			if (jsonString != cached)
			{
				// a pet whose date changed is downloaded again (icon, animations, readme). So is a pet that is new in
				// the list: its folder name may have belonged to an older pet of the gallery, whose files are still
				// saved here (the old "tico" folder held Rex the Dino, and the new Tico showed the dino's icon).
				try
				{
					var online = Normalize(JsonText.Deserialize<OptionsPets>(jsonString)?.pets, source.LocalFolder != null);
					List<OptionsPet> local = null;
					try { if (cached != null) local = Normalize(JsonText.Deserialize<OptionsPets>(cached)?.pets, source.LocalFolder != null); }
					catch (Exception skipped) { LogFile.Ignored(skipped); }   // a broken saved copy is simply replaced
					local = local ?? new List<OptionsPet>();
					foreach (var op in online)
					{
						if (local.Any(lp => lp.folder == op.folder && lp.Version == op.Version)) continue;
						foreach (var ext in new[] { ".png", ".xml", ".md", ".gif", ".pt-BR.md", ".pt-BR.none" })
							File.Delete(Path.Combine(dir, op.folder + ext));
					}
				}
				catch (Exception skipped) { LogFile.Ignored(skipped); }
				try { File.WriteAllText(jsonFile, jsonString); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			}
			return (jsonString, true);
		}

		/// <summary>True when the last <see cref="GetPetList"/> got at least one list from the internet (not only saved copies).</summary>
		public static volatile bool Online;
		/// <summary>True when every source answered: only then a pet missing from the lists really left the gallery.</summary>
		public static volatile bool AllOnline;
		/// <summary>The sources that answered in the last <see cref="LoadLists"/>.</summary>
		public static List<string> FetchedSources = new List<string>();
		/// <summary>How many pets each source listed in the last <see cref="LoadLists"/> (a pet another source wins counts too).</summary>
		public static Dictionary<string, int> SourceTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		/// <summary>The animated preview of a pet (preview.gif next to its animations), or null when it has none.</summary>
		public static Image GetPreview(string folder)
		{
			try
			{
				using (var s = GetOrDownload(folder, "preview.gif"))
				{
					var ms = new MemoryStream();
					s.CopyTo(ms);
					ms.Position = 0;
					return Image.FromStream(ms);   // the stream stays open: an animated GIF reads its frames from it
				}
			}
			catch (Exception) { return null; }
		}

		/// <summary>
		/// Get (if neccessary, download) the README.md file for a pet, and return it as a list of strings, one string per line.
		/// </summary>
		public static List<string> GetReadMe(string folder)
		{
			List<string> readme = new List<string>();
			// Portuguese description when the program is in Portuguese and the pet has one (README.pt-BR.md)
			string file = "README.md";
			if (Lang.Current.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
			{
				string none = Path.Combine(SourceOf(folder).CacheDir, folder + ".pt-BR.none");   // remembers "this pet has none"
				if (!File.Exists(none))
				{
					try { using (GetOrDownload(folder, "README.pt-BR.md")) { } file = "README.pt-BR.md"; }
					catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
					{
						try { File.WriteAllText(none, ""); } catch (Exception skipped) { LogFile.Ignored(skipped); }
					}
					catch (Exception skipped) { LogFile.Ignored(skipped); }   // offline: the English one may still be saved
				}
			}
			using (var stream = GetOrDownload(folder, file))
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					var str = reader.ReadToEnd();
					readme = str.Split(new string[] { "<br>", "<br />", "<br/>", "\r\n", "\n" }, StringSplitOptions.None).ToList();   // keep blank lines: they separate paragraphs
				}
			}
			return readme;
		}

		/// <summary>
		/// Get (if neccessary, download) the animations.xml file for a pet, and return it as a string.
		/// </summary>
		public static async Task<string> GetPetXml(string folder)
		{
			var xmlStr = "";
			using (var stream = GetOrDownload(folder, "animations.xml"))
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					xmlStr = reader.ReadToEnd();
				}
			}
			return xmlStr;
		}

		/// <summary>
		/// Size of a pet's animations.xml on the server, without downloading it (HEAD request). 0 if unknown.
		/// </summary>
		public static async Task<long> GetPetSize(string folder)
		{
			try
			{
				using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
				using (var request = new HttpRequestMessage(HttpMethod.Head, PetUrl(folder, "animations.xml")))
				{
					client.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
					using (var response = await client.SendAsync(request))
						return response.IsSuccessStatusCode ? response.Content.Headers.ContentLength ?? 0 : 0;
				}
			}
			catch (Exception) { return 0; }
		}

		/// <summary>
		/// Download a pet's animations.xml, reporting the progress (0..1). Returns the text of the file.
		/// </summary>
		public static async Task<string> DownloadPetXml(string folder, long expectedSize, IProgress<double> progress, System.Threading.CancellationToken cancel = default)
		{
			// a pet of a folder of this computer: read as it is
			var url = new Uri(PetUrl(folder, "animations.xml"));
			if (url.IsFile)
			{
				progress?.Report(1.0);
				return await File.ReadAllTextAsync(url.LocalPath, System.Text.Encoding.UTF8, cancel);
			}
			using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
			{
				client.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
				using (var response = await client.GetAsync(PetUrl(folder, "animations.xml"), HttpCompletionOption.ResponseHeadersRead, cancel))
				{
					response.EnsureSuccessStatusCode();
					long? announced = response.Content.Headers.ContentLength;
					if (announced > MaxFileBytes) throw new IOException(Lang.T("sources.tooBig", MaxFileBytes / (1024 * 1024)));
					long total = announced ?? expectedSize;
					using (var stream = await response.Content.ReadAsStreamAsync(cancel))
					using (var memory = new MemoryStream())
					{
						var buffer = new byte[32 * 1024];
						long received = 0;
						int read;
						while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancel)) > 0)
						{
							memory.Write(buffer, 0, read);
							received += read;
							if (received > MaxFileBytes) throw new IOException(Lang.T("sources.tooBig", MaxFileBytes / (1024 * 1024)));
							if (total > 0) progress?.Report(Math.Min(1.0, received / (double)total));
						}
						// a cut connection or a file that does not match the gallery is never saved
						if (announced.HasValue && received != announced.Value)
							throw new IOException(Lang.T("pets.errIncomplete"));
						if (expectedSize > 0 && received != expectedSize)
							throw new IOException(Lang.T("pets.errMismatch"));
						progress?.Report(1.0);
						memory.Position = 0;
						using (var reader = new StreamReader(memory, System.Text.Encoding.UTF8, true))   // also strips a BOM
							return reader.ReadToEnd();
					}
				}
			}
		}

		/// <summary>
		/// Get the file, if not present locally, download it from the pet's source and keep it in that source's cache
		/// folder. Return the file as a stream.
		/// </summary>
		static private Stream GetOrDownload(string folder, string filename)
		{
			if (!IsPlainFolder(folder)) throw new ArgumentException("Invalid pet folder");
			var source = SourceOf(folder);
			Directory.CreateDirectory(source.CacheDir);
			string localFile = Path.Combine(source.CacheDir, folder + "." + filename.Substring(filename.IndexOf(".") + 1));
			if (!File.Exists(localFile))
			{
				using (WebResponse wrFileResponse = WebRequest.Create(PetUrl(folder, filename)).GetResponse())
				{
					if (wrFileResponse.ContentLength > MaxFileBytes) throw new IOException(Lang.T("sources.tooBig", MaxFileBytes / (1024 * 1024)));
					string temp = localFile + ".part";
					using (Stream stream = wrFileResponse.GetResponseStream())
					using (FileStream file = File.Create(temp))
					{
						var buffer = new byte[64 * 1024];
						long total = 0;
						int read;
						while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							total += read;
							if (total > MaxFileBytes) throw new IOException(Lang.T("sources.tooBig", MaxFileBytes / (1024 * 1024)));
							file.Write(buffer, 0, read);
						}
					}
					File.Copy(temp, localFile, true);
					File.Delete(temp);
				}
			}

			return File.Open(localFile, FileMode.Open, FileAccess.Read);
		}
	}
}
