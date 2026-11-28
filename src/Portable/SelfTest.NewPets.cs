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
		/// New pets in the gallery: the ones never seen are told once (colours of one pet as one), the bubble has a
		/// "See" button, and that button opens "Download pets" with only the new ones (the chip and the tab count).
		/// </summary>
		/// <summary>
		/// The "Source" row of the gallery: with a second source there is a chip for each (and "All"), and each chip
		/// shows only the pets of its source. Without a source of the user's, the original desktopPet gallery is
		/// added for the test and taken away after.
		/// </summary>
		static async Task SourceFilterCheck()
		{
			var before = GallerySources.All();
			string filterBefore = Program.MyData.GetSetting("GallerySourceFilter");
			try
			{
				if (!GallerySources.Active().Any(s => !s.IsOfficial))
				{
					var extra = GallerySources.Parse("https://github.com/" + GallerySources.OriginalOwner + "/" + GallerySources.OriginalRepo);
					if (extra == null) { Check("source filter: a source to test with", false); return; }
					GallerySources.Save(before.Concat(new[] { extra }));
				}
				foreach (var f in Application.OpenForms.OfType<FormSettings>().ToList()) f.Close();
				FormSettings.ShowPage(FormSettings.Page.Pets);
				var settings = Application.OpenForms.OfType<FormSettings>().FirstOrDefault();
				if (settings == null) { Check("source filter: the gallery opens", false); return; }
				settings.TestDownloadTab();
				await Task.Delay(500);
				for (int i = 0; i < 200 && settings.TestLoading; i++) await Task.Delay(200);
				await Task.Delay(800);
				if (!OptionsGit.Online) { lines.Add("SKIP  source filter: gallery offline"); settings.Close(); return; }
				var chips = settings.TestSourceChips();
				Check("source filter: a chip for all, the official gallery and the other source", chips.Count >= 3 && chips[0] == "" && chips.Contains("official"), string.Join(", ", chips));
				Check("source filter: no chip for a source without pets in this tab", !chips.Contains("local") || settings.TestShownSources("local").Count > 0, string.Join(", ", chips));
				settings.TestShownSources("");
				var all = settings.TestShownSources("");
				Check("source filter: \"All\" shows more than one source", all.Count > 1, string.Join(", ", all));
				foreach (var key in chips.Where(k => k.Length > 0))
				{
					var shown = settings.TestShownSources(key);
					Check("source filter: only " + key, shown.All(k => k == key), string.Join(", ", shown));
				}
				var other = chips.FirstOrDefault(k => k.Length > 0 && k != "official" && k != "local");
				if (other != null && resultFile.Length > 0)
				{
					settings.TestShownSources(other);
					await Task.Delay(300);
					try
					{
						using (var bmp = new System.Drawing.Bitmap(settings.Width, settings.Height))
						{
							settings.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, settings.Width, settings.Height));
							bmp.Save(Path.Combine(Path.GetDirectoryName(resultFile), "source-filter.png"));
						}
					}
					catch (Exception skipped) { LogFile.Ignored(skipped); }
				}
				settings.Close();

				// only the official gallery (and pets made on this computer): no "Source" row
				GallerySources.Save(before.Where(s => s.IsOfficial));
				await Task.Delay(300);
				FormSettings.ShowPage(FormSettings.Page.Pets);
				settings = Application.OpenForms.OfType<FormSettings>().FirstOrDefault();
				if (settings != null)
				{
					await Task.Delay(500);
					for (int i = 0; i < 200 && settings.TestLoading; i++) await Task.Delay(200);
					await Task.Delay(800);
					var none = settings.TestSourceChips();
					Check("source filter: no row with only the official gallery", none.Count == 0, string.Join(", ", none));
					if (resultFile.Length > 0)
						foreach (var (name, w) in new[] { ("gallery-wide", 980), ("gallery-narrow", 760) })
						{
							settings.Width = w;
							await Task.Delay(400);
							using (var bmp = new System.Drawing.Bitmap(settings.Width, settings.Height))
							{
								settings.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, settings.Width, settings.Height));
								bmp.Save(Path.Combine(Path.GetDirectoryName(resultFile), name + ".png"));
							}
						}
					settings.Close();
				}
			}
			finally
			{
				GallerySources.Save(before);
				Program.MyData.SetSetting("GallerySourceFilter", filterBefore ?? "");
			}
		}

		static async Task NewPetsCheck()
		{
			var seenBefore = Program.MyData.GetSeenPets();
			string toldBefore = Program.MyData.GetSetting("NewPetsTold");
			bool notifyBefore = NewPets.Notify;
			try
			{
				OptionsPet P(string folder, string name) => new OptionsPet { folder = folder, name = name };
				Program.MyData.SetSeenPets(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["seen-a"] = "", ["seen-b"] = "" });
				Program.MyData.SetSetting("NewPetsTold", "");
				NewPets.Notify = false;
				var gallery = new List<OptionsPet> { P("seen-a", "A"), P("seen-b", "B"), P("fresh-c", "Cat"), P("blue_sheep", "gSheep Blue"), P("green_sheep", "gSheep Green") };
				var told = NewPets.Check(gallery);
				Check("new pets: the ones never seen are found", told.Contains("Cat"), string.Join(", ", told));
				Check("new pets: the colours of one pet count once", told.Count(n => n == "gSheep") == 1 && told.Count == 2, string.Join(", ", told));
				Check("new pets: the ones seen are not new", !told.Contains("A") && !told.Contains("B"));
				Check("new pets: told only once", NewPets.Check(gallery).Count == 0);

				// with the notice on: the pet says it, with a "See" button
				NewPets.Notify = true;
				SpeechBubble.TestClearHeld();
				gallery.Add(P("fresh-d", "Dog"));
				var told2 = NewPets.Check(gallery);
				await Task.Delay(600);
				Check("new pets: a pet that arrives later is told", told2.SequenceEqual(new[] { "Dog" }), string.Join(", ", told2));
				Check("new pets: the pet says it (or keeps it for after the quiet time)", SpeechBubble.Talking || SpeechBubble.HeldCount > 0);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// the "See" button: the real gallery, all seen but two, opens on the new ones
				var real = await OptionsGit.LoadLists();
				if (!OptionsGit.Online || real.Count < 5) { lines.Add("SKIP  new pets: gallery offline"); return; }
				var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				var keepNew = real.Where(p => FormSettings.GroupOf(p.folder) == null && !PetStore.Has(p.folder) && !BuiltInPets.IsBuiltIn(p.folder)).Take(2).Select(p => p.folder).ToList();
				foreach (var p in real) if (!keepNew.Contains(p.folder)) seen[p.folder] = p.Version;
				Program.MyData.SetSeenPets(seen);
				foreach (var f in Application.OpenForms.OfType<FormSettings>().ToList()) f.Close();
				FormSettings.ShowNewPets();
				var settings = Application.OpenForms.OfType<FormSettings>().FirstOrDefault();
				for (int i = 0; i < 150 && settings != null && settings.TestLoading; i++) await Task.Delay(200);
				await Task.Delay(1500);
				Check("new pets: \"See\" opens the gallery", settings != null && settings.Visible);
				if (settings != null)
				{
					var shown = settings.TestPetsShown();
					Check("new pets: only the new ones are shown", shown.Count > 0 && shown.All(f => keepNew.Contains(f, StringComparer.OrdinalIgnoreCase)), string.Join(", ", shown) + " / new: " + string.Join(", ", keepNew));
					if (resultFile.Length > 0)
						try
						{
							using (var bmp = new System.Drawing.Bitmap(settings.Width, settings.Height))
							{
								settings.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, settings.Width, settings.Height));
								bmp.Save(Path.Combine(Path.GetDirectoryName(resultFile), "new-pets.png"));
							}
						}
						catch (Exception skipped) { LogFile.Ignored(skipped); }
					settings.Close();
				}
			}
			finally
			{
				if (seenBefore != null) Program.MyData.SetSeenPets(seenBefore);
				Program.MyData.SetSetting("NewPetsTold", toldBefore);
				NewPets.Notify = notifyBefore;
			}
		}
	}
}
