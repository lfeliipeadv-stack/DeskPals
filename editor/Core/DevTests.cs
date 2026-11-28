using System.Drawing;
using System.Drawing.Imaging;

namespace PetEditor
{
	/// <summary>
	/// Developer checks of the tools without windows ("--devtests=&lt;Pets folder&gt;"): the results go to
	/// %TEMP%\PetEditor-devtests.txt; the exit code is the number of failures.
	/// </summary>
	internal static class DevTests
	{
		public static int Run(string pets)
		{
			var log = new List<string>();
			int fails = 0;
			void Check(string name, bool ok, string detail = "") { log.Add($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  →  " + detail : "")}"); if (!ok) fails++; }
			string tmp = Path.Combine(Path.GetTempPath(), "PetEditor-devtests");
			Directory.CreateDirectory(tmp);
			try
			{
				var pingo = PetDoc.Open(Path.Combine(pets, "pingo", "animations.xml"));

				// 1. the animated preview: the GIF reads back with all its frames and the same pixels
				var gif = GalleryDialog.Preview(pingo);
				File.WriteAllBytes(Path.Combine(tmp, "preview.gif"), gif);
				using (var img = Image.FromStream(new MemoryStream(gif)))
				{
					int n = img.GetFrameCount(FrameDimension.Time);
					Check("gif: readable, several frames", n >= 8, $"{n} frames, {img.Width}x{img.Height}");
					var walk = pingo.Root.Animations.Animation.First(a => a.Name == "walk");
					int scale = Math.Max(1, 120 / pingo.FrameH);
					img.SelectActiveFrame(FrameDimension.Time, 0);
					using var b = new Bitmap(img);
					var src = pingo.Frame(walk.Sequence.Frame[0]);
					int diff = 0, opaque = 0;
					for (int y = 0; y < src.Height; y++)
						for (int x = 0; x < src.Width; x++)
						{
							var a = src.GetPixel(x, y); var c = b.GetPixel(x * scale + scale / 2, y * scale + scale / 2);
							if (a.A < 128) { if (c.A >= 128) diff++; continue; }
							opaque++;
							if (Math.Abs(a.R - c.R) + Math.Abs(a.G - c.G) + Math.Abs(a.B - c.B) > 24) diff++;
						}
					Check("gif: the first frame is the pet, pixel by pixel", diff == 0 && opaque > 0, $"{diff} different of {opaque}");
				}

				// 2. the assistant: a pet from the frames of a GIF, with every role it needs
				var frames = SheetBuilder.Load(new[] { Path.Combine(pets, "pingo", "preview.gif") });
				var (png, cols, rows) = SheetBuilder.Build(frames, 8);
				var sheet = new PetDoc(new XmlData.RootNode { Image = new XmlData.ImageNode { Png = png, TilesX = cols, TilesY = rows, Transparency = "Magenta" } }, null);
				var picks = new Dictionary<string, List<int>> { ["idle"] = new() { 0 }, ["walk"] = new() { 0, 1, 2, 3 }, ["turn"] = new(), ["fall"] = new() { 4 }, ["drag"] = new(), ["blink"] = new() { 0, 5 }, ["sleep"] = new(), ["happy"] = new() { 6, 7 }, ["jump"] = new() { 4 } };
				var made = Wizard.Create(sheet, picks, "Teste", "Editor", 1, true);
				var problems = Validator.Run(made);
				Check("assistant: the pet has no errors", problems.All(p => !p.Error), string.Join(" | ", problems.Where(p => p.Error).Select(p => p.Text)));
				Check("assistant: it walks, turns, falls and can be picked up", new[] { "walk", "turn", "fall", "drag", "fall soft", "kill" }.All(n => made.Root.Animations.Animation.Any(a => a.Name == n)));
				var walkA = made.Root.Animations.Animation.First(a => a.Name == "walk");
				Check("assistant: walking turns at the sides and falls without ground", walkA.Border?.Next?.Length > 0 && walkA.Gravity?.Next?.Length > 0);
				Check("assistant: three entrances", made.Root.Spawns.Spawn.Length == 3);
				string xml = PetXmlWriter.Write(made.Root);
				File.WriteAllText(Path.Combine(tmp, "assistant.xml"), xml);
				Check("assistant: the file reads back", XmlData.AnimationXML.ParseXML(xml) != null);
				log.Add("INFO  assistant warnings: " + string.Join(" | ", problems.Select(p => p.Text)));

				// 3. a scene copied from Pingo into the new pet: frames, numbers and children come along
				int before = made.Root.Animations.Animation.Length, framesBefore = made.Frames.Count;
				var kick = pingo.Root.Animations.Animation.First(a => pingo.Root.Childs.Child.Any(ch => ch.Id == a.Id));
				int first = ImportDialog.Copy(made, pingo, new List<int> { kick.Id }, true);
				var copied = made.Anim(first);
				Check("copy: the animation is there with a new number", copied != null && copied.Name == kick.Name, copied?.Name ?? "none");
				Check("copy: more animations (what follows)", made.Root.Animations.Animation.Length > before + 1, $"{made.Root.Animations.Animation.Length - before} new");
				Check("copy: its child window too", made.Root.Childs.Child.Any(ch => ch.Id == first));
				Check("copy: frames added after the old ones", made.Frames.Count > framesBefore && copied.Sequence.Frame.All(f => f >= framesBefore && f < made.Frames.Count), $"{framesBefore} -> {made.Frames.Count}");
				var p2 = Validator.Run(made);
				Check("copy: still no errors", p2.All(p => !p.Error), string.Join(" | ", p2.Where(p => p.Error).Select(p => p.Text)));

				// 4. optimisation: unused frames go, numbers follow; the palette PNG keeps every pixel
				var opt = PetDoc.Open(Path.Combine(pets, "pingo", "animations.xml"));
				var keep = opt.Root.Animations.Animation.ToDictionary(a => a.Id, a => a.Sequence.Frame.Select(f => opt.Frame(f)).Select(b => b.GetPixel(b.Width / 2, b.Height / 2).ToArgb()).ToArray());
				int removed = Optimizer.RemoveUnused(opt);
				bool same = opt.Root.Animations.Animation.All(a => a.Sequence.Frame.Select(f => opt.Frame(f)).Select(b => b.GetPixel(b.Width / 2, b.Height / 2).ToArgb()).SequenceEqual(keep[a.Id]));
				Check("optimise: the animations show the same frames after removing unused ones", same, $"{removed} removed");
				int oldKb = Convert.FromBase64String(opt.Root.Image.Png).Length;
				int saved = Optimizer.Compress(opt);
				log.Add($"INFO  optimise: {removed} unused frames, PNG {oldKb / 1024} KB, palette saved {saved / 1024} KB");
				Check("optimise: the pet still reads back", XmlData.AnimationXML.ParseXML(PetXmlWriter.Write(opt.Root)) != null);

				// 5. the .zip to send: one folder with the five files, and the pet in it opens again the same
				var zdoc = PetDoc.Open(Path.Combine(pets, "pingo", "animations.xml"));
				string zip = Path.Combine(Path.GetTempPath(), "PetEditor-test.zip");
				GalleryDialog.WriteZip(zdoc, zip, "pingo");
				using (var z = System.IO.Compression.ZipFile.OpenRead(zip))
				{
					var names = z.Entries.Select(e => e.FullName.Replace('\\', '/')).OrderBy(n => n).ToList();
					var want = new[] { "pingo/README.md", "pingo/README.pt-BR.md", "pingo/animations.xml", "pingo/icon.png", "pingo/preview.gif" };
					Check("zip: one folder with animations.xml, icon, preview and READMEs", want.All(names.Contains), string.Join(", ", names));
					using var xr = new StreamReader(z.GetEntry("pingo/animations.xml").Open());
					var back = XmlData.AnimationXML.ParseXML(xr.ReadToEnd());
					Check("zip: the pet in it reads back with the same animations", back?.Animations.Animation.Length == zdoc.Root.Animations.Animation.Length, $"{back?.Animations.Animation.Length} animations");
				}
				File.Delete(zip);

				// 7. undo between sessions: saved with the file, back when it opens again, gone if the file changed elsewhere
				{
					string dir = Path.Combine(tmp, "history");
					Directory.CreateDirectory(dir);
					string file = Path.Combine(dir, "animations.xml");
					var h = PetDoc.Open(Path.Combine(pets, "pingo", "animations.xml"));
					string firstName = h.Root.Header.Petname;
					h.Save(file);
					h.Root.Header.Petname = "Passo 1"; h.Changed("header");
					h.Root.Header.Petname = "Passo 2"; h.Changed("header");
					h.Undo();
					h.Save(file);
					var again = PetDoc.Open(file);
					Check("history: the steps come back when the pet opens again", again.UndoCount == 1 && again.RedoCount == 1, $"undo {again.UndoCount}, redo {again.RedoCount}");
					again.Undo();
					Check("history: undo after opening goes back to the step before", again.Root.Header.Petname == firstName, again.Root.Header.Petname);
					again.Redo(); again.Redo();
					Check("history: redo kept too", again.Root.Header.Petname == "Passo 2", again.Root.Header.Petname);
					Check("history: the sheet comes back in the old steps", !string.IsNullOrEmpty(again.Root.Image.Png) && again.Frames.Count > 0);
					File.AppendAllText(file, "\n<!-- changed by hand -->");
					var edited = PetDoc.Open(file);
					Check("history: a file changed elsewhere starts without steps", edited.UndoCount == 0, $"undo {edited.UndoCount}");
					try { File.Delete(PetDoc.HistoryFile(file)); Directory.Delete(dir, true); } catch { }
				}

				// 8. the cards of the Project page: the picture and the name of a pet file
				{
					var (img, name) = ProjectPage.Thumb(Path.Combine(pets, "pingo", "animations.xml"));
					Check("cards: picture and name of the pet", img != null && img.Width > 4 && !string.IsNullOrEmpty(name), $"{img?.Width}x{img?.Height} {name}");
				}

				// 9. publish: the folder name of a pet
				Check("publish: folder name without accents or signs", GalleryDialog.Slug("Gato Preto!") == "gato_preto" && GalleryDialog.Slug("") == "pet", GalleryDialog.Slug("Gato Preto!"));

				// 10. "Create a character": every kind, accessory and size gives a pet that passes the check of the editor,
				// with the animations the program looks for and its sounds; a picture of every kind's poses is saved to look at
				{
					var kinds = Enum.GetValues<CharacterMaker.Kind>();
					var extras = Enum.GetValues<CharacterMaker.Extra>();
					foreach (var kind in kinds)
					{
						var (body, detail, accent) = CharacterMaker.Palette(kind);
						var look = new CharacterMaker.Look { Kind = kind, Body = body, Detail = detail, Accent = accent, Extra = extras[(int)kind % extras.Length], Scale = 1 + (int)kind % 3 };
						var doc = CharacterMaker.Create(look, "Teste " + kind, "self-test");
						var errors = Validator.Run(doc).Where(p => p.Error).Select(p => p.Text).ToList();
						var names = doc.Root.Animations.Animation.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
						var needed = new[] { "walk", "turn", "fall", "drag", "kill", "idle", "blink", "sleep", "happy", "jump", "talk", "dance", "sit", "love" }
							.Concat(CharacterMaker.Extras.Select(x => x.name))
							.Concat(CharacterMaker.Flies(kind) ? new[] { "fly" } : Array.Empty<string>())
							.Concat(kind == CharacterMaker.Kind.Dragon ? new[] { "fire" } : Array.Empty<string>()).ToList();
						Check($"character: {kind} is a pet without errors", errors.Count == 0, string.Join("; ", errors.Take(4)));
						var warnings = Validator.Run(doc).Where(p => !p.Error).Select(p => p.Text).ToList();
						Check($"character: {kind} without warnings", warnings.Count == 0, string.Join("; ", warnings.Take(4)));
						Check($"character: {kind} has its animations", needed.All(names.Contains), string.Join(", ", needed.Where(n => !names.Contains(n))));
						Check($"character: {kind} frames at the chosen size", doc.FrameW == 40 * look.Scale && doc.FrameH == 46 * look.Scale, $"{doc.FrameW}x{doc.FrameH} at {look.Scale}x");
						// every frame an animation uses has the pet drawn on it (the sheet's spare cells are empty by nature)
						int empty = 0;
						var used = doc.Root.Animations.Animation.SelectMany(a => a.Sequence.Frame).Distinct().ToList();
						foreach (int i in used)
						{
							var fr = doc.Frame(i);
							int opaque = 0;
							for (int y = 0; y < fr.Height; y += look.Scale) for (int x = 0; x < fr.Width; x += look.Scale) if (fr.GetPixel(x, y).A > 128) opaque++;
							if (opaque < 200) empty++;
						}
						Check($"character: {kind} draws every frame", empty == 0, $"{empty} of {used.Count} almost empty");
						// the sounds: MP3s that Windows opens, on the animations they belong to, light enough to download
						if (Synth.CanEncode)
						{
							var snd = doc.Root.Sounds.Sound;
							long bytes = snd.Sum(x => (long)(x.Base64?.Length ?? 0) * 3 / 4);
							Check($"character: {kind} has sounds", snd.Length >= 25, $"{snd.Length} sounds, {bytes / 1024} KB");
							Check($"character: {kind} sounds are light", bytes < 400 * 1024, $"{bytes / 1024} KB");
							string bad = null;
							foreach (var x in snd)
								try { DesktopPet.Mp3Player.Load(Convert.FromBase64String(x.Base64)); }
								catch (Exception ex) { bad = $"{doc.Anim(x.Id)?.Name}: {ex.Message}"; break; }
							Check($"character: {kind} sounds open in Windows", bad == null, bad ?? "");
							var silent = new CharacterMaker.Look { Kind = kind, Sounds = false };
							Check($"character: {kind} without sounds when asked", CharacterMaker.Create(silent, "x", "x").Root.Sounds.Sound.Length == 0);
						}
						else log.Add("SKIP  character sounds: this Windows has no MP3 encoder");
					}
					// a picture of each kind to look at: every pose, on a light and a dark row, with the accessories by turn
					int z = 3, picCols = 14, cw = 40 * z + 6, ch = 46 * z + 6;
					foreach (var k in kinds)
					{
						var (b, d, a) = CharacterMaker.Palette(k);
						var kindFrames = CharacterMaker.Frames(new CharacterMaker.Look { Kind = k, Body = b, Detail = d, Accent = a, Extra = extras[((int)k + 1) % extras.Length] }).frames;
						int picRows = (kindFrames.Count + picCols - 1) / picCols;
						using (var pic = new Bitmap(picCols * cw, picRows * ch * 2))
						using (var g = Graphics.FromImage(pic))
						{
							g.Clear(Color.FromArgb(240, 238, 232));
							g.FillRectangle(new SolidBrush(Color.FromArgb(32, 33, 36)), 0, picRows * ch, pic.Width, picRows * ch);
							g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
							g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
							for (int half = 0; half < 2; half++)
								for (int i = 0; i < kindFrames.Count; i++)
									g.DrawImage(kindFrames[i], new Rectangle(i % picCols * cw + 3, (half * picRows + i / picCols) * ch + 3, 40 * z, 46 * z));
							pic.Save(Path.Combine(tmp, $"character-{k}.png"));
						}
						foreach (var f in kindFrames) f.Dispose();
					}
				}

				// 6. scale of the window (Windows zoom): sizes written at 100% grow, fonts too
				Check("zoom: a size at 100% grows with the zoom of Windows", Ui.S(100) == (int)Math.Round(100 * Ui.Scale), $"zoom {Ui.Scale}");

				// 7. the animations playing in place (the list and "All animations"): their frames in order, the repeats, turning
				{
					var walkP = pingo.Root.Animations.Animation.First(a => a.Name == "walk");
					var lv = new LiveAnim(pingo, walkP);
					var seen = new List<int>();
					for (int i = 0; i < walkP.Sequence.Frame.Length; i++) { seen.Add(lv.Frame); lv.Advance(); }
					Check("live: the frames of the animation, in order", seen.SequenceEqual(walkP.Sequence.Frame), string.Join(",", seen) + " ≠ " + string.Join(",", walkP.Sequence.Frame));
					Check("live: a frame stays as long as the animation says", lv.Interval >= 16);
					var fake = new XmlData.AnimationNode { Id = 900, Name = "t", Sequence = new XmlData.SequenceNode { Frame = new[] { 0, 1, 2 }, RepeatFromFrame = 1, RepeatCount = "2", Action = "flip" }, Start = new XmlData.MovingNode { X = "0", Y = "0", Interval = "100", Opacity = 1 }, End = new XmlData.MovingNode { X = "0", Y = "0", Interval = "300", Opacity = 1 } };
					var lf = new LiveAnim(pingo, fake);
					var steps = new List<int>();
					for (int i = 0; i < 7; i++) { steps.Add(lf.Frame); lf.Advance(); }
					Check("live: \"repeat from frame\" and the repeat count, as DeskPals counts them (FormPet)", steps.SequenceEqual(new[] { 0, 1, 2, 2, 1, 2, 1 }), string.Join(",", steps));
					Check("live: an animation ending with \"flip\" turns at the end", lf.Flipped && lf.Step == 0);
					lf.Advance(); lf.Advance();
					Check("live: the time goes from the start interval to the end one", lf.Interval > 100 && lf.Interval < 300, lf.Interval.ToString());
					var empty = new LiveAnim(pingo, new XmlData.AnimationNode { Id = 901, Sequence = new XmlData.SequenceNode(), Start = new XmlData.MovingNode(), End = new XmlData.MovingNode() });
					empty.Advance();
					Check("live: an animation without frames is left alone", !empty.HasFrames && empty.Frame == -1);
				}

				// 8. "Test this one": the copy of the test starts with the animation (every entrance leads to it), the pet stays as it is
				{
					string pxml = PetXmlWriter.Write(pingo.Root);
					int target = pingo.Root.Animations.Animation.Last().Id;
					string started = DesktopTest.StartingWith(pxml, target);
					var root = XmlData.AnimationXML.ParseXML(started);
					Check("test this: every entrance leads to the animation", root != null && root.Spawns.Spawn.Length > 0 && root.Spawns.Spawn.All(s => s.Next == target), root == null ? "not readable" : string.Join(",", root.Spawns.Spawn.Select(s => s.Next)));
					Check("test this: the rest of the pet is the same", root != null && root.Animations.Animation.Length == pingo.Root.Animations.Animation.Length && root.Image.Png == pingo.Root.Image.Png);
					Check("test this: without an animation, the file as it is", DesktopTest.StartingWith(pxml, -1) == pxml);
					Check("test this: the pet in the editor is not changed", PetXmlWriter.Write(pingo.Root) == pxml);
				}

				// 9. links of GitHub for "Open from the gallery"
				{
					string Show(WebPets.Link l) => l == null ? "null" : $"{l.Owner}/{l.Repo}@{l.Branch ?? "?"}:{l.Path ?? "?"}";
					Check("link: a repository", Show(WebPets.Parse("https://github.com/someone/pets")) == "someone/pets@?:?");
					Check("link: owner/repo", Show(WebPets.Parse("someone/pets")) == "someone/pets@?:?");
					Check("link: .git and a slash at the end", Show(WebPets.Parse("https://github.com/someone/pets.git/")) == "someone/pets@?:?");
					Check("link: a folder of a branch", Show(WebPets.Parse("https://github.com/someone/pets/tree/main/Pets/frog")) == "someone/pets@main:Pets/frog");
					Check("link: a pet's animations.xml stands for its folder", Show(WebPets.Parse("https://github.com/someone/pets/blob/main/Pets/frog/animations.xml")) == "someone/pets@main:Pets/frog");
					Check("link: a raw link", Show(WebPets.Parse("https://raw.githubusercontent.com/someone/pets/master/frog/animations.xml")) == "someone/pets@master:frog");
					Check("link: a raw file at the root", Show(WebPets.Parse("https://raw.githubusercontent.com/someone/pets/master/animations.xml")) == "someone/pets@master:");
					Check("link: not GitHub", WebPets.Parse("https://example.com/a/b") == null && WebPets.Parse("") == null && WebPets.Parse("just words") == null);
					Check("link: a folder with \"..\" is refused", WebPets.Parse("https://github.com/someone/pets/tree/main/../secret") == null);
					var src = new WebPets.Source { Owner = "someone", Repo = "pets", Branch = "main", Path = "my pets" };
					Check("link: the address of a folder", src.BaseUrl == "https://raw.githubusercontent.com/someone/pets/main/my%20pets/");
					var pet = new WebPets.Pet { Folder = "gsheep-green" };
					Check("link: a pet's name from its folder", pet.Shown == "Gsheep Green", pet.Shown);
					Check("link: a pet's address", pet.Url(src) == src.BaseUrl + "gsheep-green/" && new WebPets.Pet { Folder = "x", Dir = "" }.Url(src) == src.BaseUrl);
					// kept in the settings, without the official one
					var savedSources = EditorSettings.Current.Sources;
					try
					{
						WebPets.Save(new[] { WebPets.Official, src, new WebPets.Source { Owner = "a", Repo = "b", Branch = "master", Path = "", Scanned = true } });
						var back = WebPets.Saved();
						Check("sources: kept and read back (not the official one)", back.Count == 2 && back[0].Key == src.Key && back[1].Scanned && back[1].Path == "", string.Join(" ; ", EditorSettings.Current.Sources));
					}
					finally { EditorSettings.Current.Sources = savedSources; EditorSettings.Save(); }
					// the real gallery of DeskPals (needs the internet: skipped without it)
					try
					{
						var list = WebPets.Pets(WebPets.Official).GetAwaiter().GetResult();
						Check("gallery: DeskPals' pets are listed", list.Count >= 5, list.Count + " pets");
						var firstPet = list.First();
						string file = WebPets.Download(WebPets.Official, firstPet).GetAwaiter().GetResult();
						var opened = PetDoc.Open(file);
						Check("gallery: a pet comes and opens", opened.Root.Animations.Animation.Length > 0 && opened.Frames.Count > 0, firstPet.Folder);
						Check("gallery: it comes to the editor's own folder", file.Contains(Path.Combine("PetEditor", "online")), file);
					}
					catch (WebPets.Problem ex) { log.Add("SKIP  gallery (internet): " + ex.Message); }
				}
			}
			catch (Exception e) { Check("no exception", false, e.ToString()); }
			File.WriteAllLines(Path.Combine(Path.GetTempPath(), "PetEditor-devtests.txt"), log);
			return fails;
		}
	}
}
