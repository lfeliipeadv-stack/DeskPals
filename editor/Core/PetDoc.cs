using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Serialization;
using XmlData;

namespace PetEditor
{
	/// <summary>
	/// The pet being edited: the XML model of DesktopPet (<see cref="RootNode"/>), its file, the frames cut from the
	/// sprite sheet, and undo / redo. Pages change <see cref="Root"/> directly and call <see cref="Changed"/>; every
	/// change (typing is grouped) becomes a step that can be undone.
	/// </summary>
	internal sealed class PetDoc
	{
		public RootNode Root;
		/// <summary>animations.xml of the project (null: never saved).</summary>
		public string FilePath;
		public bool Dirty { get; private set; }

		public Bitmap Sheet { get; private set; }
		public List<Bitmap> Frames { get; } = new();
		public int FrameW { get; private set; } = 32;
		public int FrameH { get; private set; } = 32;

		/// <summary>Something changed: the pages refresh what they show (also after undo / redo).</summary>
		public event Action<string> Updated;
		/// <summary>The title (name, dirty mark) changed.</summary>
		public event Action StateChanged;

		readonly List<RootNode> undo = new(), redo = new();
		RootNode stable;   // the state after the last step (what undo goes back to)
		readonly System.Windows.Forms.Timer group = new() { Interval = 700 };

		public PetDoc(RootNode root, string file)
		{
			Root = Normalize(root);
			FilePath = file;
			stable = Clone(Root);
			group.Tick += (s, e) => { group.Stop(); Commit(); };
			RebuildFrames();
		}

		public string Name => string.IsNullOrWhiteSpace(Root.Header?.Petname) ? L.T("doc.untitled") : Root.Header.Petname;

		// ------------------------------------------------------------------ loading and saving

		public static PetDoc Open(string path)
		{
			if (Directory.Exists(path)) path = Path.Combine(path, "animations.xml");
			string text = File.ReadAllText(path);
			var root = AnimationXML.ParseXML(text);
			if (root == null) throw new InvalidDataException(L.T("doc.badXml"));
			var doc = new PetDoc(root, path);
			doc.LoadHistory(text);
			return doc;
		}

		// ------------------------------------------------------------------ undo between sessions

		/// <summary>
		/// The undo and redo steps are kept with the project when it is saved (%AppData%\DesktopPet\PetEditor\history,
		/// one zip per file) and come back when it is opened again, as long as the file was not changed elsewhere.
		/// The big texts (sheet, icon, sounds) are written once and shared by the steps.
		/// </summary>
		static string HistoryDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopPet", "PetEditor", "history");
		const int KeptSteps = 30, KeptProjects = 40;

		internal static string HistoryFile(string path) => Path.Combine(HistoryDir,
			Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToLowerInvariant()))).Substring(0, 16) + ".zip");

		static string Sha(string text) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

		/// <summary>The steps that can be undone and redone (tests).</summary>
		public int UndoCount => undo.Count;
		public int RedoCount => redo.Count;

		const string BlobMark = "@blob:";

		/// <summary>A step as XML with its big texts replaced by a reference into <paramref name="blobs"/>.</summary>
		static string Pack(RootNode s, Dictionary<string, string> blobs)
		{
			string Ref(string text)
			{
				if (string.IsNullOrEmpty(text)) return text;
				string h = Sha(text).Substring(0, 24);
				blobs[h] = text;
				return BlobMark + h;
			}
			string png = s.Image?.Png, icon = s.Header?.Icon;
			var sounds = s.Sounds?.Sound?.Select(x => x.Base64).ToArray();
			try
			{
				if (s.Image != null) s.Image.Png = Ref(png);
				if (s.Header != null) s.Header.Icon = Ref(icon);
				if (sounds != null) for (int i = 0; i < sounds.Length; i++) s.Sounds.Sound[i].Base64 = Ref(sounds[i]);
				using var sw = new StringWriter();
				serializer.Serialize(sw, s);
				return sw.ToString();
			}
			finally
			{
				if (s.Image != null) s.Image.Png = png;
				if (s.Header != null) s.Header.Icon = icon;
				if (sounds != null) for (int i = 0; i < sounds.Length; i++) s.Sounds.Sound[i].Base64 = sounds[i];
			}
		}

		static RootNode Unpack(string xml, Func<string, string> blob)
		{
			using var sr = new StringReader(xml);
			var r = Normalize((RootNode)serializer.Deserialize(sr));
			string Back(string t) => t != null && t.StartsWith(BlobMark) ? blob(t.Substring(BlobMark.Length)) : t;
			r.Image.Png = Back(r.Image.Png);
			r.Header.Icon = Back(r.Header.Icon);
			foreach (var s in r.Sounds.Sound) s.Base64 = Back(s.Base64);
			return r;
		}

		/// <summary>Keep the steps of this project with the text just written to its file.</summary>
		void SaveHistory(string written)
		{
			try
			{
				if (group.Enabled) { group.Stop(); Commit(); }
				Directory.CreateDirectory(HistoryDir);
				string file = HistoryFile(FilePath), tmp = file + ".tmp";
				if (undo.Count == 0 && redo.Count == 0) { if (File.Exists(file)) File.Delete(file); return; }
				var blobs = new Dictionary<string, string>();
				using (var zip = System.IO.Compression.ZipFile.Open(tmp, System.IO.Compression.ZipArchiveMode.Create))
				{
					void Add(string name, string text)
					{
						using var w = new StreamWriter(zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Fastest).Open(), new System.Text.UTF8Encoding(false));
						w.Write(text);
					}
					Add("file.sha", Sha(written));
					int i = 0;
					foreach (var s in undo.Skip(Math.Max(0, undo.Count - KeptSteps))) Add($"undo/{i++:D3}.xml", Pack(s, blobs));
					i = 0;
					foreach (var s in redo.Skip(Math.Max(0, redo.Count - KeptSteps))) Add($"redo/{i++:D3}.xml", Pack(s, blobs));
					foreach (var kv in blobs) Add("blobs/" + kv.Key, kv.Value);
				}
				File.Move(tmp, file, true);
				// the oldest projects are forgotten
				foreach (var old in new DirectoryInfo(HistoryDir).GetFiles("*.zip").OrderByDescending(f => f.LastWriteTimeUtc).Skip(KeptProjects))
					old.Delete();
			}
			catch (Exception e) { System.Diagnostics.Debug.WriteLine("history: " + e.Message); }
		}

		/// <summary>The steps kept when this file was saved (none if the file changed since, e.g. edited by hand).</summary>
		void LoadHistory(string fileText)
		{
			try
			{
				string file = HistoryFile(FilePath);
				if (!File.Exists(file)) return;
				using var zip = System.IO.Compression.ZipFile.OpenRead(file);
				string Read(System.IO.Compression.ZipArchiveEntry e) { using var r = new StreamReader(e.Open()); return r.ReadToEnd(); }
				if (zip.GetEntry("file.sha") is not { } sha || Read(sha) != Sha(fileText)) return;
				var blobs = new Dictionary<string, string>();
				string Blob(string h)
				{
					if (blobs.TryGetValue(h, out var t)) return t;
					var e = zip.GetEntry("blobs/" + h);
					return blobs[h] = e == null ? null : Read(e);
				}
				var u = zip.Entries.Where(e => e.FullName.StartsWith("undo/")).OrderBy(e => e.FullName).Select(e => Unpack(Read(e), Blob)).ToList();
				var r = zip.Entries.Where(e => e.FullName.StartsWith("redo/")).OrderBy(e => e.FullName).Select(e => Unpack(Read(e), Blob)).ToList();
				undo.Clear(); undo.AddRange(u);
				redo.Clear(); redo.AddRange(r);
			}
			catch (Exception e) { System.Diagnostics.Debug.WriteLine("history: " + e.Message); undo.Clear(); redo.Clear(); }
		}

		/// <summary>Write the pet (with a .bak of the file it replaces) and, if wanted, README.md and icon.png when missing.</summary>
		public void Save(string path)
		{
			string xml = PetXmlWriter.Write(Root);
			if (File.Exists(path) && EditorSettings.Current.Backup)
				File.Copy(path, path + ".bak", true);
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
			File.WriteAllText(path, xml, new System.Text.UTF8Encoding(false));
			if (EditorSettings.Current.WriteExtras) WriteExtras(Path.GetDirectoryName(Path.GetFullPath(path)));
			FilePath = path;
			Dirty = false;
			SaveHistory(xml);
			EditorSettings.Current.AddRecent(path);
			StateChanged?.Invoke();
		}

		void WriteExtras(string dir)
		{
			try
			{
				string readme = Path.Combine(dir, "README.md");
				if (!File.Exists(readme))
				{
					var h = Root.Header;
					File.WriteAllText(readme, $"# {h.Petname}\n\n{(h.Info ?? "").Replace("[br]", "\n")}\n\n{L.T("doc.readmeBy")} {h.Author}.\n", new System.Text.UTF8Encoding(false));
				}
				string icon = Path.Combine(dir, "icon.png");
				if (!File.Exists(icon) && Icons.FromIco(Root.Header.Icon, 64) is Bitmap b) using (b) b.Save(icon, ImageFormat.Png);
			}
			catch (Exception e) { System.Diagnostics.Debug.WriteLine("extras: " + e.Message); }
		}

		/// <summary>Every list the app expects exists (it needs &lt;childs&gt; and &lt;sounds&gt; even when empty).</summary>
		static RootNode Normalize(RootNode r)
		{
			r.Header ??= new HeaderNode();
			r.Image ??= new ImageNode { TilesX = 1, TilesY = 1, Transparency = "Magenta" };
			r.Spawns ??= new SpawnsNode();
			r.Spawns.Spawn ??= Array.Empty<SpawnNode>();
			r.Animations ??= new AnimationsNode();
			r.Animations.Animation ??= Array.Empty<AnimationNode>();
			r.Childs ??= new ChildsNode();
			r.Childs.Child ??= Array.Empty<ChildNode>();
			r.Sounds ??= new SoundsNode();
			r.Sounds.Sound ??= Array.Empty<SoundNode>();
			foreach (var a in r.Animations.Animation)
			{
				a.Start ??= new MovingNode { X = "0", Y = "0", Interval = "200" };
				a.End ??= new MovingNode { X = "0", Y = "0", Interval = "200" };
				a.Sequence ??= new SequenceNode { RepeatCount = "0" };
				a.Sequence.Frame ??= Array.Empty<int>();
				a.Sequence.Next ??= Array.Empty<NextNode>();
				a.Sequence.RepeatCount ??= "0";
			}
			return r;
		}

		// ------------------------------------------------------------------ frames

		/// <summary>Cut the frames from the sprite sheet (after a new image or another grid).</summary>
		public void RebuildFrames()
		{
			foreach (var f in Frames) f.Dispose();
			Frames.Clear();
			Sheet?.Dispose();
			Sheet = null;
			var img = Root.Image;
			if (string.IsNullOrWhiteSpace(img.Png)) return;
			try
			{
				using var ms = new MemoryStream(Convert.FromBase64String(img.Png.Trim()));
				using var raw = new Bitmap(ms);
				Sheet = new Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppArgb);
				using (var g = Graphics.FromImage(Sheet)) g.DrawImage(raw, 0, 0, raw.Width, raw.Height);
				var key = Color.FromName(string.IsNullOrWhiteSpace(img.Transparency) ? "Magenta" : img.Transparency);
				if (key.IsKnownColor && key.A > 0) Sheet.MakeTransparent(key);
				int tx = Math.Max(1, img.TilesX), ty = Math.Max(1, img.TilesY);
				FrameW = Math.Max(1, Sheet.Width / tx);
				FrameH = Math.Max(1, Sheet.Height / ty);
				for (int y = 0; y < ty; y++)
					for (int x = 0; x < tx; x++)
						Frames.Add(Sheet.Clone(new Rectangle(x * FrameW, y * FrameH, FrameW, FrameH), PixelFormat.Format32bppArgb));
			}
			catch (Exception e) { System.Diagnostics.Debug.WriteLine("sheet: " + e.Message); }
		}

		public Bitmap Frame(int i) => i >= 0 && i < Frames.Count ? Frames[i] : null;

		/// <summary>
		/// A new sprite sheet from these frames (all on cells of the same size, feet at the bottom), in
		/// <paramref name="cols"/> columns. The caller announces the change (<see cref="Changed"/> "image").
		/// </summary>
		public void SetFrames(List<Bitmap> frames, int cols)
		{
			if (frames.Count == 0) return;
			var (png, c, r) = SheetBuilder.Build(frames, Math.Max(1, cols));
			Root.Image.Png = png;
			Root.Image.TilesX = c;
			Root.Image.TilesY = r;
		}

		/// <summary>Copies of the frames (to change them and write a new sheet).</summary>
		public List<Bitmap> CopyFrames() => Frames.Select(f => new Bitmap(f)).ToList();

		/// <summary>Every frame number in the animations goes through <paramref name="map"/> (null: the frame is removed).</summary>
		public void RemapFrames(Func<int, int?> map)
		{
			foreach (var a in Root.Animations.Animation)
				a.Sequence.Frame = (a.Sequence.Frame ?? Array.Empty<int>()).Select(map).Where(x => x != null).Select(x => x.Value).ToArray();
		}

		// ------------------------------------------------------------------ lookups

		public AnimationNode Anim(int id) => Root.Animations.Animation.FirstOrDefault(a => a.Id == id);
		public string AnimName(int id) => Anim(id) is AnimationNode a ? $"{a.Name}  (#{id})" : $"#{id} ?";
		public int NextAnimId() => Root.Animations.Animation.Length == 0 ? 1 : Root.Animations.Animation.Max(a => a.Id) + 1;

		/// <summary>Where an animation is used (next lists, spawns, children, sounds): shown before deleting it.</summary>
		public List<string> UsesOf(int id)
		{
			var uses = new List<string>();
			foreach (var a in Root.Animations.Animation)
			{
				if (a.Id == id) continue;
				var lists = (a.Sequence.Next ?? Array.Empty<NextNode>()).Concat(a.Border?.Next ?? Array.Empty<NextNode>()).Concat(a.Gravity?.Next ?? Array.Empty<NextNode>()).Concat(a.Interact?.Next ?? Array.Empty<NextNode>());
				if (lists.Any(n => n.Value == id)) uses.Add(L.T("uses.anim", a.Name, a.Id));
			}
			foreach (var s in Root.Spawns.Spawn) if (s.Next == id) uses.Add(L.T("uses.spawn", s.Id));
			foreach (var c in Root.Childs.Child) if (c.Next == id || c.Id == id) uses.Add(L.T("uses.child", c.Id));
			foreach (var s in Root.Sounds.Sound) if (s.Id == id) uses.Add(L.T("uses.sound"));
			return uses;
		}

		// ------------------------------------------------------------------ changes, undo and redo

		/// <summary>The pages changed <see cref="Root"/>. <paramref name="typing"/>: grouped with the next changes (one undo step).</summary>
		public void Changed(string what, bool typing = false)
		{
			Dirty = true;
			if (what == "image") RebuildFrames();
			if (typing) { group.Stop(); group.Start(); }
			else { group.Stop(); Commit(); }
			Updated?.Invoke(what);
			StateChanged?.Invoke();
		}

		/// <summary>The whole pet replaced (the XML page applied an edited text): one step that can be undone.</summary>
		public void Replace(RootNode r)
		{
			Root = Normalize(r);
			RebuildFrames();
			Changed("all");
		}

		void Commit()
		{
			undo.Add(stable);
			if (undo.Count > 80) undo.RemoveAt(0);
			redo.Clear();
			stable = Clone(Root);
			StateChanged?.Invoke();
		}

		public bool CanUndo => undo.Count > 0 || group.Enabled;
		public bool CanRedo => redo.Count > 0;

		public void Undo()
		{
			if (group.Enabled) { group.Stop(); Commit(); }
			if (undo.Count == 0) return;
			redo.Add(stable);
			stable = undo[^1];
			undo.RemoveAt(undo.Count - 1);
			Restore();
		}

		public void Redo()
		{
			if (redo.Count == 0) return;
			undo.Add(stable);
			stable = redo[^1];
			redo.RemoveAt(redo.Count - 1);
			Restore();
		}

		void Restore()
		{
			string oldPng = Root.Image?.Png;
			Root = Clone(stable);
			Dirty = true;
			if (Root.Image?.Png != oldPng || Frames.Count != Math.Max(1, Root.Image.TilesX) * Math.Max(1, Root.Image.TilesY)) RebuildFrames();
			Updated?.Invoke("all");
			StateChanged?.Invoke();
		}

		static readonly XmlSerializer serializer = new(typeof(RootNode));

		/// <summary>
		/// A deep copy. The big texts (sprite sheet, icon, sounds) are not copied: the copy shares them (texts never
		/// change in place), so a hundred undo steps of a big pet cost little memory.
		/// </summary>
		public static RootNode Clone(RootNode r)
		{
			string png = r.Image?.Png, icon = r.Header?.Icon;
			var sounds = r.Sounds?.Sound?.Select(s => s.Base64).ToArray();
			if (r.Image != null) r.Image.Png = null;
			if (r.Header != null) r.Header.Icon = null;
			if (sounds != null) foreach (var s in r.Sounds.Sound) s.Base64 = null;
			RootNode c;
			try
			{
				using var ms = new MemoryStream();
				serializer.Serialize(ms, r);
				ms.Position = 0;
				c = Normalize((RootNode)serializer.Deserialize(ms));
			}
			finally
			{
				if (r.Image != null) r.Image.Png = png;
				if (r.Header != null) r.Header.Icon = icon;
				if (sounds != null) for (int i = 0; i < sounds.Length; i++) r.Sounds.Sound[i].Base64 = sounds[i];
			}
			c.Image.Png = png;
			c.Header.Icon = icon;
			if (sounds != null) for (int i = 0; i < sounds.Length && i < c.Sounds.Sound.Length; i++) c.Sounds.Sound[i].Base64 = sounds[i];
			return c;
		}
	}
}
