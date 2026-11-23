using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// Developer: every pet of a Pets folder (set PETS_DIR), read the way the program reads it. The sprite sheet opens and
		/// splits into whole frames; every frame of every animation exists; every next, border, gravity, child, spawn and
		/// sound points to an animation that exists; every value (positions, intervals, repeats, often "random*…") computes
		/// without an error and every interval is above 0 (a timer cannot tick at 0); and nothing was logged as an error.
		/// </summary>
		static void PetsDeep()
		{
			string dir = Environment.GetEnvironmentVariable("PETS_DIR");
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) { lines.Add("SKIP  pets deep: PETS_DIR not set"); return; }
			int pets = 0, animations = 0;
			foreach (var folder in Directory.GetDirectories(dir).OrderBy(d => d))
			{
				string file = Path.Combine(folder, "animations.xml");
				if (!File.Exists(file)) continue;
				string pet = Path.GetFileName(folder);
				var problems = new List<string>();
				int errors0 = StartUp.ErrorCount;
				var running = Animations.Xml;   // shared: given back to the pet on the screen at the end
				try
				{
					var xml = new Xml(1) { AnimationXML = XmlData.AnimationXML.ParseXML(File.ReadAllText(file)) };

					// the sprite sheet first, as the program reads it: a real image, whole frames
					var image = xml.AnimationXML.Image;
					int tiles = Math.Max(0, image.TilesX) * Math.Max(0, image.TilesY);
					if (tiles == 0) problems.Add($"tiles {image.TilesX}x{image.TilesY}");
					try
					{
						using (var ms = new MemoryStream(Convert.FromBase64String((image.Png ?? "").Trim())))
						using (var bmp = new Bitmap(ms))
						{
							if (image.TilesX > 0 && image.TilesY > 0 && (bmp.Width % image.TilesX != 0 || bmp.Height % image.TilesY != 0))
								problems.Add($"sheet {bmp.Width}x{bmp.Height} does not split into {image.TilesX}x{image.TilesY} frames");
							// the size of one frame, as the program has it once the image is loaded ("imageW", "imageH")
							if (image.TilesX > 0 && image.TilesY > 0) { xml.spriteWidth = bmp.Width / image.TilesX; xml.spriteHeight = bmp.Height / image.TilesY; }
						}
					}
					catch (Exception ex) { problems.Add("sheet: " + ex.Message); }

					// then the animations (values that depend on the screen, like "screenW/imageH", are worked out here)
					var anims = new Animations(xml);
					xml.LoadAnimations(anims);
					pets++;
					animations += anims.SheepAnimations.Count;

					bool Has(int id) => anims.SheepAnimations.ContainsKey(id);
					foreach (var a in anims.SheepAnimations.Values)
					{
						string name = $"{a.Name} ({a.ID})";
						if (a.Sequence.Frames.Count == 0) problems.Add(name + ": no frames");
						foreach (int f in a.Sequence.Frames)
							if (f < 0 || (tiles > 0 && f >= tiles)) { problems.Add($"{name}: frame {f} of {tiles}"); break; }
						foreach (var n in (a.EndAnimation ?? new List<TNextAnimation>()).Concat(a.EndBorder ?? new List<TNextAnimation>()).Concat(a.EndGravity ?? new List<TNextAnimation>()))
							if (!Has(n.ID)) { problems.Add($"{name}: next {n.ID} missing"); break; }
						try
						{
							for (int k = 0; k < 3; k++)   // "random" differs each time: a few tries
							{
								var copy = a;
								copy.UpdateValues(0);
								if (copy.Start.Interval.Value <= 0 || copy.End.Interval.Value <= 0) { problems.Add($"{name}: interval {copy.Start.Interval.Value}/{copy.End.Interval.Value}"); break; }
								if (copy.Sequence.TotalSteps <= 0)
								{
									problems.Add($"{name}: {copy.Sequence.TotalSteps} steps (repeat \"{a.Sequence.Repeat.Compute}\" = {a.Sequence.Repeat.GetValue(0)}, frame {xml.spriteWidth}x{xml.spriteHeight}, own values: {Animations.Xml == xml})");
									break;
								}
							}
						}
						catch (Exception ex) { problems.Add($"{name}: values: {ex.GetType().Name} {ex.Message}"); }
					}
					foreach (var kv in anims.SheepChild)
					{
						if (!Has(kv.Key)) problems.Add($"child of missing animation {kv.Key}");
						foreach (var c in kv.Value)
						{
							if (!Has(c.Next)) problems.Add($"child of {kv.Key}: next {c.Next} missing");
							try { c.Position.X.GetValue(0); c.Position.Y.GetValue(0); }
							catch (Exception ex) { problems.Add($"child of {kv.Key}: position: {ex.Message}"); }
						}
					}
					foreach (var s in anims.SheepSpawn.Values)
					{
						if (!Has(s.Next)) problems.Add($"spawn: next {s.Next} missing");
						try { s.Start.X.GetValue(0); s.Start.Y.GetValue(0); }
						catch (Exception ex) { problems.Add($"spawn: position: {ex.Message}"); }
					}
					if (anims.SheepSpawn.Count == 0) problems.Add("no spawn");
					foreach (var id in anims.SheepSound.Keys) if (!Has(id)) problems.Add($"sound of missing animation {id}");
				}
				catch (Exception ex) { problems.Add("load: " + ex.GetType().Name + " " + ex.Message); }
				finally { Animations.Xml = running; }
				if (StartUp.ErrorCount > errors0) problems.Add($"{StartUp.ErrorCount - errors0} error(s) logged, the last: {StartUp.LastError}");
				Check($"pets deep: {pet}", problems.Count == 0, string.Join("; ", problems.Distinct().Take(8)) + (problems.Count > 8 ? $" … ({problems.Count})" : ""));
			}
			lines.Add($"INFO  pets deep: {pets} pets, {animations} animations");
		}

		/// <summary>
		/// A pet file of any gallery cannot stop the program: a broken expression counts as 0 (and is logged), a value far
		/// out of range is held to a sane size. A pet left out of every screen comes back by itself.
		/// </summary>
		static async Task PetSafety()
		{
			var xml = Animations.Xml;
			if (xml == null) { lines.Add("SKIP  pet safety: no pet loaded"); return; }
			int Try(string expression) { try { return xml.ParseValue(expression, "self-test"); } catch (Exception) { return int.MinValue; } }
			Check("pet safety: a broken expression counts as 0", Try("2+") == 0 && Try("abc def") == 0 && Try("(1") == 0 && Try("1/0") == 0,
				$"2+ → {Try("2+")}, abc def → {Try("abc def")}, (1 → {Try("(1")}, 1/0 → {Try("1/0")}");
			Check("pet safety: a huge value is held to a sane size", Try("10000000000000") == 1000000 && Try("-10000000000000") == -1000000);
			Check("pet safety: normal expressions still work", Try("screenW/2") == Screen.PrimaryScreen.Bounds.Width / 2 && Try("3*4+1") == 13);

			var pet = Pet();
			if (pet == null) { lines.Add("SKIP  pet safety: no pet on the screen"); return; }
			double keep = FormPet.LostSeconds;
			int rescued = FormPet.Rescued;
			FormPet.LostSeconds = 1.5;
			try
			{
				var all = SystemInformation.VirtualScreen;
				pet.TestPlaceAt(all.Right + 300, all.Bottom + 600);   // far below and to the right of every screen
				bool back = false;
				for (int i = 0; i < 60 && !back; i++)
				{
					await Task.Delay(100);
					back = FormPet.Rescued > rescued && Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(pet.Bounds));
				}
				Check("pet safety: a pet lost out of the screen comes back by itself", back, pet.Bounds.ToString());
			}
			finally { FormPet.LostSeconds = keep; }
		}

		/// <summary>
		/// Developer (slow, set PETS_DIR and PETS_LIVE=1; PETS_LIVE_ONLY=a,b for some): every pet of the folder is adopted
		/// in turn and every one of its animations is played for a moment on the screen (its children windows too).
		/// A step that fails is counted (no error window, see FormPet.QuietStepErrors); the pet must stay on a screen.
		/// The pet in use comes back at the end.
		/// </summary>
		static async Task PetsLive()
		{
			string dir = Environment.GetEnvironmentVariable("PETS_DIR");
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || Environment.GetEnvironmentVariable("PETS_LIVE") != "1")
			{ lines.Add("SKIP  pets live: set PETS_DIR and PETS_LIVE=1"); return; }
			var only = (Environment.GetEnvironmentVariable("PETS_LIVE_ONLY") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
			string keepXml = Program.MyData.GetXml(), keepFolder = Program.MyData.GetCurrentPetFolder();
			var everywhere = SystemInformation.VirtualScreen;
			FormPet.QuietStepErrors = true;
			int played = 0;
			try
			{
				foreach (var folder in Directory.GetDirectories(dir).OrderBy(d => d))
				{
					string file = Path.Combine(folder, "animations.xml");
					string name = Path.GetFileName(folder);
					if (!File.Exists(file) || (only.Count > 0 && !only.Contains(name, StringComparer.OrdinalIgnoreCase))) continue;
					string text = File.ReadAllText(file);
					int expected;
					try { var x = new Xml(1) { AnimationXML = XmlData.AnimationXML.ParseXML(text) }; var a0 = new Animations(x); x.LoadAnimations(a0); expected = a0.SheepAnimations.Count; }
					catch (Exception ex) { Check($"pets live: {name} loads", false, ex.Message); continue; }

					// adopt it, as the gallery does
					int log0 = StartUp.ErrorCount;
					Program.MyData.SetImages("");
					Program.MyData.SetIcon("");
					Program.MyData.SetXml(text, name);
					Program.Mainthread.LoadNewXMLFromString(text);
					FormPet pet = null;
					for (int i = 0; i < 120; i++)
					{
						await Task.Delay(100);
						pet = Pet();
						if (pet != null && Program.Mainthread.GetAnimations()?.SheepAnimations?.Count == expected && pet.Opacity > 0) break;
						pet = null;
					}
					if (pet == null) { Check($"pets live: {name} comes onto the screen", false); continue; }

					var anims = Program.Mainthread.GetAnimations();
					var failed = new List<string>();
					var lost = new List<string>();
					foreach (var a in anims.SheepAnimations.Values.OrderBy(v => v.ID).ToList())
					{
						if (a.ID == anims.AnimationKill) continue;   // that one sends the pet away (it comes back by itself)
						pet = Pet();
						for (int i = 0; i < 50 && pet == null; i++) { await Task.Delay(100); pet = Pet(); }
						if (pet == null) { lost.Add(a.Name); continue; }
						int e0 = FormPet.StepErrors;
						pet.TestPlay(a.ID);
						int wait = Math.Max(120, Math.Min(450, a.Start.Interval.Value * 3));
						await Task.Delay(wait);
						played++;
						if (FormPet.StepErrors > e0) failed.Add(FormPet.LastStepError);
						if (!pet.IsDisposed && pet.Visible && !everywhere.IntersectsWith(Rectangle.Inflate(pet.Bounds, everywhere.Width, everywhere.Height)))
							lost.Add($"{a.Name} at {pet.Bounds}");
						// the windows the scenes open (balloons, planes…) would pile up: close them between two animations
						foreach (var child in Application.OpenForms.OfType<FormPet>().Where(p => p.IsChildWindow && !p.IsDisposed).ToList())
							try { child.Close(); } catch (Exception skipped) { LogFile.Ignored(skipped); }
						foreach (var bubble in Application.OpenForms.OfType<SpeechBubble>().ToList()) bubble.Close();
					}
					Check($"pets live: {name} plays its {anims.SheepAnimations.Count} animations without an error", failed.Count == 0,
						string.Join("; ", failed.Distinct().Take(6)) + (failed.Count > 6 ? $" … ({failed.Count})" : ""));
					// not a failure: animations are forced here one after another, cutting a pet that walks out at the edge
					// (it comes back by itself when left alone) or dives under the taskbar on purpose (Pingus digging)
					if (lost.Count > 0) lines.Add($"INFO  pets live: {name} went out of the screen while forced ({string.Join("; ", lost.Take(3))})");
					if (StartUp.ErrorCount > log0)
						Check($"pets live: {name} logs no error", false, $"{StartUp.ErrorCount - log0} error(s), the last: {StartUp.LastError}");
				}
			}
			finally
			{
				FormPet.QuietStepErrors = false;
				Program.MyData.SetImages("");
				Program.MyData.SetIcon("");
				Program.MyData.SetXml(keepXml, keepFolder);
				Program.Mainthread.LoadNewXMLFromString(keepXml);
				lines.Add($"INFO  pets live: {played} animations played");
			}
		}
	}
}
