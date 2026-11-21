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
			folder = v.folder; name = v.name; author = v.author; lastupdate = v.lastupdate;
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
		/// Base path to the pets
		/// </summary>
		public static string BaseGitUrl = AppInfo.PetsRawUrl; // gallery of this repository only (see AppInfo)
		/// <summary>
		/// Local temporary path, to store the github files.
		/// </summary>
		public static string BaseLocalPath = Path.Combine(Path.GetTempPath(), "esheep64");

		/// <summary>
		/// Get the list of pets, from Github or locally.
		/// </summary>
		/// <returns>List of OptionsPets pets</returns>
		public static async Task GetPetList(IProgress<OptionsPet> progress)
		{
			string jsonString = null;
			string jsonFile = Path.Combine(BaseLocalPath, "pets.json");
			Directory.CreateDirectory(BaseLocalPath);
			string cached = File.Exists(jsonFile) ? File.ReadAllText(jsonFile) : null;

			// The list is small (a few KB) and is the source of new pets, names and updates:
			// always ask the repository. Without internet the copy saved last time is used only for
			// names and icons of pets already downloaded (the caller hides the rest: see Online).
			Online = false;
			try
			{
				using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
				{
					client.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
					jsonString = await client.GetStringAsync(BaseGitUrl + "pets.json");
					Online = true;
				}
			}
			catch (Exception)
			{
				if (cached == null) return;   // first run without internet: nothing to show
			}

			if (jsonString == null)
			{
				jsonString = cached;
			}
			else if (jsonString != cached)
			{
				// a pet whose date changed is downloaded again (icon, animations, readme). So is a pet that is new in
				// the list: its folder name may have belonged to an older pet of the gallery, whose files are still
				// saved here (the old "tico" folder held Rex the Dino, and the new Tico showed the dino's icon).
				try
				{
					var online = JsonText.Deserialize<OptionsPets>(jsonString).pets;
					List<OptionsPet> local = null;
					try { if (cached != null) local = JsonText.Deserialize<OptionsPets>(cached).pets; }
					catch (Exception skipped) { LogFile.Ignored(skipped); }   // a broken saved copy is simply replaced
					local = local ?? new List<OptionsPet>();
					foreach (var op in online)
					{
						if (local.Any(lp => lp.folder == op.folder && lp.lastupdate == op.lastupdate)) continue;
						foreach (var ext in new[] { ".png", ".xml", ".md", ".gif", ".pt-BR.md", ".pt-BR.none" })
							File.Delete(Path.Combine(BaseLocalPath, op.folder + ext));
					}
				}
				catch (Exception skipped) { LogFile.Ignored(skipped); }
				File.WriteAllText(jsonFile, jsonString);
			}

			var pets = JsonText.Deserialize<OptionsPets>(jsonString);
			// Order by date, as this is the default selection in the option window
			pets.Reorder();

			foreach (var p in pets.pets)
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

		/// <summary>True when the last <see cref="GetPetList"/> got the list from the repository (not the saved copy).</summary>
		public static volatile bool Online;

		/// <summary>
		/// Get (if neccessary, download) the README.md file for a pet, and return it as a list of strings, one string per line.
		/// </summary>
		/// <param name="folder">folder on Github or name of the pet</param>
		/// <returns>list of lines, markdown formatted</returns>
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

		public static List<string> GetReadMe(string folder)	
		{
			List<string> readme = new List<string>();
			// Portuguese description when the program is in Portuguese and the pet has one (README.pt-BR.md)
			string file = "README.md";
			if (Lang.Current.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
			{
				string none = Path.Combine(BaseLocalPath, folder + ".pt-BR.none");   // remembers "this pet has none"
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
		/// <param name="folder">folder on Github or name of the pet</param>
		/// <returns>the xml as string</returns>
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
				using (var request = new HttpRequestMessage(HttpMethod.Head, BaseGitUrl + folder + "/animations.xml"))
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
			using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
			{
				client.DefaultRequestHeaders.Add("User-Agent", "DesktopPet");
				using (var response = await client.GetAsync(BaseGitUrl + folder + "/animations.xml", HttpCompletionOption.ResponseHeadersRead, cancel))
				{
					response.EnsureSuccessStatusCode();
					long? announced = response.Content.Headers.ContentLength;
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
		/// Get the file, if not present locally, download it from Github and store it in the local temporary folder. Return the file as a stream.
		/// </summary>
		/// <param name="folder">folder on Github or name of the pet</param>
		/// <param name="filename">name of the file to get</param>
		/// <returns>the file as a stream</returns>
		static private Stream GetOrDownload(string folder, string filename)
		{
			string localFile = Path.Combine(BaseLocalPath, folder + "." + filename.Substring(filename.IndexOf(".") + 1));
			if (!File.Exists(localFile))//BaseGitUrl + folder + "/" + filename
			{
				//using (WebResponse wrFileResponse = WebRequest.Create(url + WebPets.pets[j].folder + "/icon.png").GetResponse())
				using (WebResponse wrFileResponse = WebRequest.Create(BaseGitUrl + folder + "/" + filename).GetResponse())
				{
					using (Stream stream = wrFileResponse.GetResponseStream())
					{
						using (FileStream file = File.Create(localFile))
						{
							stream.CopyTo(file);
						}
					}
				}
			}

			return File.Open(localFile, FileMode.Open, FileAccess.Read);
		}
	}
}
