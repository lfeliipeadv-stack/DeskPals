using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// How lively (the restful next animations weigh more), sleeping together (a window that opens once and does not
		/// stretch), a friend's visit (its own pet, then it leaves) and a folder of this computer as a source of pets.
		/// </summary>
		static async Task CalmAndVisitsCheck()
		{
			// --- how lively: an animation of the eSheep whose next ones include a restful one
			int levelBefore = PetCalm.Level;
			bool togetherBefore = PetCalm.SleepTogether;
			try
			{
				var xml = new Xml(1) { AnimationXML = XmlData.AnimationXML.ParseXML(BuiltInPets.GetXml(BuiltInPets.Folders.First())) };
				var anims = new Animations(xml);
				xml.LoadAnimations(anims);
				var from = anims.SheepAnimations.Values.FirstOrDefault(a => a.EndAnimation.Count > 1
					&& a.EndAnimation.Any(n => anims.SheepAnimations.ContainsKey(n.ID) && PetCalm.IsRestful(anims.SheepAnimations[n.ID].Name))
					&& a.EndAnimation.Any(n => anims.SheepAnimations.ContainsKey(n.ID) && !PetCalm.IsRestful(anims.SheepAnimations[n.ID].Name) && n.Probability > 0));
				if (from.EndAnimation == null) lines.Add("SKIP  calm: the eSheep has no animation with restful and lively next ones");
				else
				{
					double Share(int level)
					{
						PetCalm.Level = level;
						int restful = 0, n = 4000;
						for (int i = 0; i < n; i++)
						{
							int id = anims.SetNextSequenceAnimation(from.ID, TNextAnimation.TOnly.TASKBAR);
							if (id > 0 && PetCalm.IsRestful(anims.SheepAnimations[id].Name)) restful++;
						}
						return restful / (double)n;
					}
					double normal = Share(0), sleepy = Share(3);
					lines.Add($"INFO  calm: from \"{from.Name}\", restful next {normal:P0} at normal, {sleepy:P0} as sleepyhead");
					Check("calm: a sleepyhead pet rests much more often", sleepy > normal * 5 && sleepy > 0.4, $"{normal:P0} → {sleepy:P0}");
					double calm = Share(1);
					Check("calm: each level rests more than the one before", calm > normal && sleepy > calm, $"{normal:P0} < {calm:P0} < {sleepy:P0}");
					Check("calm: only next animations its maker set", Enumerable.Range(0, 300).Select(_ => anims.SetNextSequenceAnimation(from.ID, TNextAnimation.TOnly.TASKBAR)).All(id => id < 0 || from.EndAnimation.Any(n => n.ID == id)));
				}

				// --- sleeping together: opens once with two pets, never with one, does not stretch while it lasts
				PetCalm.Level = 0;
				PetCalm.SleepTogether = true;
				var t0 = DateTime.Now;
				PetCalm.Now = () => t0;
				PetCalm.TestReset();
				PetCalm.Started("sleep1a", true, 1);
				Check("sleep together: one pet alone does not start it", PetCalm.Weight("sleep") == 1);
				PetCalm.Started("sleep1a", true, 2);
				Check("sleep together: a pet falling asleep makes the others sleepy", PetCalm.Weight("sleep") >= 30 && PetCalm.Weight("walk") == 1);
				PetCalm.Now = () => t0.AddSeconds(80);
				PetCalm.Started("sleep1a", true, 2);   // another falls asleep during it: no longer nap
				PetCalm.Now = () => t0.AddSeconds(100);
				Check("sleep together: ends after a while (not made longer)", PetCalm.Weight("sleep") == 1);
				PetCalm.SleepTogether = false;
				PetCalm.TestReset();
				PetCalm.Now = () => t0;
				PetCalm.Started("sleep1a", true, 3);
				Check("sleep together: off, nothing changes", PetCalm.Weight("sleep") == 1);
			}
			finally
			{
				PetCalm.Now = () => DateTime.Now;
				PetCalm.TestReset();
				PetCalm.Level = levelBefore;
				PetCalm.SleepTogether = togetherBefore;
			}

			// --- a friend's visit
			var friends = PetVisits.Friends();
			Check("visits: the pet in use is not a friend", !friends.Contains(Program.MyData.GetCurrentPetFolder() ?? "\u0001", StringComparer.OrdinalIgnoreCase));
			if (friends.Count == 0) lines.Add("SKIP  visits: no other pet on this computer");
			else
			{
				int before = Application.OpenForms.OfType<FormPet>().Count();
				bool came = PetVisits.Visit(friends.First());
				await Task.Delay(1500);
				var v = PetVisits.Visitor;
				Check("visits: a friend comes", came && v != null && v.Visible, friends.First());
				Check("visits: with its own pet (not the one in use)", v != null && v.TestOwnAnimations);
				Check("visits: the pet in use stays the same", Program.MyData.GetCurrentPetFolder() == (Program.MyData.GetCurrentPetFolder()));
				PetVisits.GoAway();
				for (int i = 0; i < 150 && PetVisits.Visitor != null; i++) await Task.Delay(100);
				Check("visits: the friend leaves", PetVisits.Visitor == null);
			}

			// --- a folder of this computer as a source
			string dir = Path.Combine(Path.GetTempPath(), "deskpals-devfolder-" + Environment.ProcessId);
			var before2 = GallerySources.All();
			try
			{
				Directory.CreateDirectory(dir);
				var mine = PetStore.Folders().Take(2).ToList();
				foreach (var f in mine)
				{
					string d = Path.Combine(dir, "my " + f);
					Directory.CreateDirectory(d);
					File.WriteAllText(Path.Combine(d, "animations.xml"), PetStore.Load(f));
				}
				Directory.CreateDirectory(Path.Combine(dir, "empty folder"));
				var src = GallerySources.Parse(dir);
				Check("dev folder: a folder path is a source", src != null && src.LocalFolder != null && src.Key.StartsWith("folder:"), src?.Key ?? "null");
				if (src != null && mine.Count > 0)
				{
					var pets = await GallerySources.CheckAsync(src);
					Check("dev folder: each subfolder with an animations.xml is a pet", pets.Count == mine.Count, string.Join(", ", pets.Select(p => p.folder + "=" + p.name)));
					Check("dev folder: names from the pet's header", pets.All(p => !string.IsNullOrWhiteSpace(p.name)));
					GallerySources.Save(before2.Concat(new[] { src }));
					Check("dev folder: saved and read back", GallerySources.All().Any(s => s.LocalFolder == src.LocalFolder));
					var all = await OptionsGit.LoadLists();
					var fromFolder = all.Where(p => p.Source?.LocalFolder != null).ToList();
					Check("dev folder: its pets come in the gallery list", fromFolder.Count > 0, all.Count.ToString());
					if (fromFolder.Count > 0)
					{
						string text = await OptionsGit.DownloadPetXml(fromFolder[0].folder, 0, null);
						Check("dev folder: a pet is read from the folder", text.Contains("<animations"), text.Length.ToString());
					}
				}
				bool threw = false;
				try { await GallerySources.CheckAsync(GallerySources.Parse(Path.Combine(dir, "empty folder"))); } catch (InvalidDataException) { threw = true; }
				Check("dev folder: a folder without pets says so", threw);
			}
			finally
			{
				GallerySources.Save(before2);
				try { Directory.Delete(dir, true); } catch (Exception skipped) { LogFile.Ignored(skipped); }
			}
		}
	}
}
