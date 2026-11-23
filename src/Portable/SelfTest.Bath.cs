using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// gSheep baths, played for real: the dive from the sky (both sides) and the jump from the ground.
		/// The sheep must land exactly on its tub and leave it walking, without re-spawning in the middle.
		/// </summary>
		/// <summary>The pet in use when it is a gSheep (or the Sheep Mate), with a way to find its animations by name.</summary>
		static (FormPet pet, Dictionary<int, TAnimation> anims, Func<string, int> id) GSheep(string test)
		{
			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => !p.IsChildWindow && !p.IsDisposed && p.Visible);
			var anims = Program.Mainthread.GetAnimations()?.SheepAnimations;
			int Id(string name) => anims?.FirstOrDefault(kv => kv.Value.Name == name).Key ?? 0;
			if (pet == null || Id("divea") == 0 || Id("bath_start") == 0) { lines.Add($"SKIP  {test}: the pet in use is not a gSheep"); return (null, null, null); }
			return (pet, anims, Id);
		}

		static async Task GSheepDives()
		{
			var (pet, _, Id) = GSheep("gsheep dive");
			if (pet == null) return;
			foreach (bool left in new[] { true, false }) await Dive(pet, Id("divea"), left);
		}

		static async Task GSheepGroundBath()
		{
			var (pet, _, Id) = GSheep("gsheep ground bath");
			if (pet != null) await GroundBath(pet, Id("bath_start"));
		}

		static async Task GSheepAbduction()
		{
			var (pet, _, Id) = GSheep("gsheep abduction");
			if (pet != null) await Abduction(pet, Id("scream"));
		}

		static async Task GSheepPicker()
		{
			var (pet, anims, Id) = GSheep("gsheep picker");
			if (pet == null) return;
			int dive = Id("divea");

			// the picker offers whole scenes only: the start of the bath, not its middle, nor the tub or the king alone
			var hidden = FormAnimPicker.HiddenAnimations(Program.Mainthread.GetAnimations());
			string Shown(params string[] names) => string.Join(", ", names.Where(n => Id(n) != 0 && !hidden.Contains(Id(n))));
			string Hidden(params string[] names) => string.Join(", ", names.Where(n => Id(n) != 0 && hidden.Contains(Id(n))));
			string mustShow = Hidden("divea", "bath_start", "walk_task", "eat", "sit", "blastoff", "jump", "rotate1a");
			Check("picker: the starts of the scenes are offered", mustShow.Length == 0, "hidden: " + mustShow);
			string mustHide = Shown("diveb", "bathsplash", "bathc", "bathb2", "bath_startB", "bath_startC", "bath2a", "bath2c", "bath2d",
				"bath1a", "bath_wait", "bath_eyes", "king_walk", "king_spawn", "beam1", "blastoffc",
				"pissb", "pissb_2", "sit_stare", "sit_end", "sleep1b", "alien_kill", "spin_kill", "abducted_wait", "eat 2", "roll_end");
			Check("picker: the middle of a scene and the child windows are not", mustHide.Length == 0, "shown: " + mustHide);
			var picker = pet.TestOpenPicker();
			if (picker != null)
			{
				int spawns = Program.Mainthread.GetAnimations().SheepSpawn.Values.Select(sp => sp.Next).Distinct().Count();   // one tile per first animation
				Check("picker: the entrances have their own section, one tile each", picker.TestSpawnCount == spawns, $"{picker.TestSpawnCount} of {spawns}");
				Check("picker: an entrance is not repeated among the animations", !picker.TestAnimationIds.Contains(dive), "divea listed twice");
				picker.Close();
				await Task.Delay(200);
			}
			lines.Add($"INFO  picker: {anims.Count - hidden.Count} of {anims.Count} animations offered: "
				+ string.Join(", ", anims.Where(kv => !hidden.Contains(kv.Key)).OrderBy(kv => kv.Key).Select(kv => kv.Value.Name)));
		}

		/// <summary>The alien ship takes the sheep: it comes back (re-spawns) only after the ship has flown away.</summary>
		static async Task Abduction(FormPet pet, int scream)
		{
			if (scream == 0) return;
			var area = Screen.FromControl(pet).WorkingArea;
			for (int i = 0; i < 200 && !(pet.OnGround && Math.Abs(pet.Bottom - area.Bottom) <= 3); i++) await Task.Delay(100);
			int spawns = pet.SpawnCount;
			pet.TestPlay(scream);
			var clock = Stopwatch.StartNew();
			double shipGone = -1, back = -1;
			bool sawShip = false;
			int ground = pet.Top, rose = 0;
			double beamOpacity = 2;
			while (clock.Elapsed.TotalSeconds < 40 && back < 0)
			{
				await Task.Delay(50);
				if (pet.SpawnCount == spawns && pet.Opacity > 0.5) rose = Math.Max(rose, ground - pet.Top);
				foreach (var beam in Application.OpenForms.OfType<FormPet>().Where(f => f.Visible && (f.CurrentAnimationName ?? "").StartsWith("beam")))
					beamOpacity = Math.Min(beamOpacity, beam.Opacity);   // its first frame is drawn before the animation sets it
				bool ship = pet.LiveChildren.Any(c => (c.CurrentAnimationName ?? "").StartsWith("ship") && c.Bottom > area.Top);
				if (ship) { sawShip = true; shipGone = -1; }
				else if (sawShip && shipGone < 0) shipGone = clock.Elapsed.TotalSeconds;
				if (pet.SpawnCount != spawns) back = clock.Elapsed.TotalSeconds;
			}
			Check("abduction: the ship comes", sawShip);
			Check("abduction: the sheep rises up the beam to the ship", rose >= pet.Height * 3, $"rose {rose} px (ship {pet.Height * 4} px above)");
			Check("abduction: the yellow light is see-through", beamOpacity < 0.9, $"opacity {beamOpacity:0.00}");
			Check("abduction: the sheep comes back", back > 0, $"{clock.Elapsed.TotalSeconds:0.0} s");
			Check("abduction: it comes back only after the ship has gone", shipGone > 0 && back >= shipGone,
				$"ship gone at {shipGone:0.0} s, sheep back at {back:0.0} s");
			Check("abduction: without a long empty wait", back < 0 || shipGone < 0 || back - shipGone < 6, $"{back - shipGone:0.0} s after the ship");
		}

		/// <summary>Developer: what the animation picker offers for every pet of a Pets folder (set PETS_DIR).</summary>
		static void PickerAllPets()
		{
			string dir = Environment.GetEnvironmentVariable("PETS_DIR");
			if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) { lines.Add("SKIP  picker all pets: PETS_DIR not set"); return; }
			var running = Animations.Xml;   // "new Animations" below takes it: the pet on the screen gets it back
			try { PickerAllPetsIn(dir); }
			finally { Animations.Xml = running; }
		}

		static void PickerAllPetsIn(string dir)
		{
			foreach (var folder in System.IO.Directory.GetDirectories(dir).OrderBy(d => d))
			{
				string file = System.IO.Path.Combine(folder, "animations.xml");
				if (!System.IO.File.Exists(file)) continue;
				var xml = new Xml(1) { AnimationXML = XmlData.AnimationXML.ParseXML(System.IO.File.ReadAllText(file)) };
				var anims = new Animations(xml);
				try { xml.LoadAnimations(anims); }
				catch (Exception e) { Check($"picker all pets: {System.IO.Path.GetFileName(folder)} loads", false, e.GetType().Name + " " + e.StackTrace?.Split('\n').FirstOrDefault()?.Trim()); continue; }
				// a floating pet (no gravity): the pose after a drop does not move, and nothing falls
				if (anims.Floating)
					Check($"picker all pets: {System.IO.Path.GetFileName(folder)} floats (no fall, no gravity)",
						anims.SheepAnimations.Values.All(a => !a.Gravity) && anims.AnimationFall >= 0
						&& anims.SheepAnimations[anims.AnimationFall].Start.Y.Value == 0 && anims.SheepAnimations[anims.AnimationFall].End.Y.Value == 0);
				var hidden = FormAnimPicker.HiddenAnimations(anims);
				var shown = anims.SheepAnimations.Where(kv => !hidden.Contains(kv.Key)).OrderBy(kv => kv.Key).Select(kv => kv.Key + ":" + kv.Value.Name);
				string pet = System.IO.Path.GetFileName(folder);
				lines.Add($"INFO  {pet} ({anims.SheepAnimations.Count - hidden.Count}/{anims.SheepAnimations.Count}): {string.Join(", ", shown)}");
				// every name the picker shows (animations and entrances) needs a Portuguese name in AnimNames.cs
				var names = anims.SheepAnimations.Where(kv => !hidden.Contains(kv.Key)).Select(kv => kv.Value.Name)
					.Concat(anims.SheepSpawn.Values.Where(sp => anims.SheepAnimations.ContainsKey(sp.Next)).Select(sp => anims.SheepAnimations[sp.Next].Name))
					.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase);
				var missing = names.Where(n => !AnimNames.Has(n)).OrderBy(n => n).ToList();
				Check($"picker all pets: {pet} has every name in Portuguese (AnimNames.cs)", missing.Count == 0, string.Join(", ", missing));
			}
		}

		static readonly string[] Tubs ={ "bath1a", "bath1b", "bath1c", "bath_wait", "bath_waitB", "bath_tub_stays", "bath_tub_fades" };

		// the newest tub: the one of an earlier bath may still be fading away
		static FormPet TubOf(FormPet pet) => pet.LiveChildren.LastOrDefault(c => Tubs.Contains(c.CurrentAnimationName));

		/// <summary>The peeking eyes of the bath (and the tub under them) the first time they show; whether they faded away.</summary>
		static (Rectangle eyes, Rectangle? tub)? Eyes;
		static bool EyesFaded;

		/// <summary>Follow the pet until it walks again; log the animations it went through.</summary>
		static async Task<(List<string> names, Rectangle? landed, Rectangle? tub, Rectangle? exit, bool respawned)> Follow(FormPet pet, string landing, int seconds)
		{
			var names = new List<string>();
			Rectangle? landed = null, tub = null, exit = null;
			int spawns = pet.SpawnCount;
			var clock = Stopwatch.StartNew();
			while (clock.Elapsed.TotalSeconds < seconds && !pet.IsDisposed)
			{
				await Task.Delay(15);
				string now = pet.CurrentAnimationName ?? "";
				if (names.Count == 0 || names[names.Count - 1] != now) names.Add(now);
				if (landed == null && now == landing)
				{
					await Task.Delay(40);   // the window is placed on the next frame
					landed = pet.Bounds;
					tub = TubOf(pet)?.Bounds;
				}
				if (Eyes == null && pet.LiveChildren.FirstOrDefault(c => c.CurrentAnimationName == "bath_eyes") is FormPet eyes)
					Eyes = (eyes.Bounds, TubOf(pet)?.Bounds);
				if (pet.LiveChildren.Any(c => c.CurrentAnimationName == "bath_eyes_fade")) EyesFaded = true;
				if (landed != null && (now == "rotate1b" || now.StartsWith("walk")))
				{
					exit = pet.Bounds;
					break;
				}
				if (pet.SpawnCount != spawns) break;
			}
			return (names, landed, tub, exit, pet.SpawnCount != spawns);
		}

		static async Task Dive(FormPet pet, int dive, bool left)
		{
			string side = left ? "from the right" : "from the left";
			int scene = pet.SpawnSceneOf(dive);
			if (scene < 0) { Check($"dive {side}: starts from its spawn", false); return; }
			pet.TestFace(left);
			pet.Play(false, scene);
			var (names, landed, tub, exit, respawned) = await Follow(pet, "bathsplash", 40);
			string path = string.Join(" > ", names);
			Check($"dive {side}: reaches the tub", landed != null, path);
			Check($"dive {side}: the tub is there when it lands", tub != null, path);
			if (landed is Rectangle l && tub is Rectangle t)
				Check($"dive {side}: lands exactly on the tub", Math.Abs(l.X - t.X) <= 1 && Math.Abs(l.Y - t.Y) <= 1, $"sheep {l.Location}, tub {t.Location}");
			Check($"dive {side}: leaves the tub without re-spawning", !respawned && exit != null, path);
			if (landed is Rectangle l2 && exit is Rectangle e)
				Check($"dive {side}: walks off from the tub", Math.Abs(e.X - l2.X) <= 4 && Math.Abs(e.Bottom - l2.Bottom) <= 4, $"landed {l2.Location}, walks at {e.Location}");
			lines.Add($"INFO  dive {side}: {path}");
			await Task.Delay(500);
		}

		static async Task GroundBath(FormPet pet, int jump)
		{
			var area = Screen.FromControl(pet).WorkingArea;
			// on the taskbar, standing
			for (int i = 0; i < 200 && !(pet.OnGround && Math.Abs(pet.Bottom - area.Bottom) <= 3); i++) await Task.Delay(100);
			if (!(pet.OnGround && Math.Abs(pet.Bottom - area.Bottom) <= 3)) { lines.Add("SKIP  ground bath: the sheep did not reach the taskbar"); return; }
			Eyes = null; EyesFaded = false;
			pet.TestPlay(jump);
			var (names, landed, tub, exit, respawned) = await Follow(pet, "bath_startC", 40);
			string path = string.Join(" > ", names);
			// the eyes peek over the rim: same window place as the tub (the frames draw them on the rim)
			Check("ground bath: the eyes peek over the rim of the tub", Eyes is var (eb, et) && et is Rectangle tb && eb.Location == tb.Location,
				Eyes is var (e2, t2) ? $"eyes {e2.Location}, tub {t2?.Location}" : "no eyes");
			Check("ground bath: then the eyes fade away", EyesFaded);
			Check("ground bath: lands in the tub", landed != null && tub != null, path);
			if (landed is Rectangle l && tub is Rectangle t)
				Check("ground bath: lands exactly on the tub", Math.Abs(l.X - t.X) <= 1 && Math.Abs(l.Y - t.Y) <= 1, $"sheep {l.Location}, tub {t.Location}");
			Check("ground bath: leaves the tub without re-spawning", !respawned && exit != null, path);
			lines.Add($"INFO  ground bath: {path}");
		}
	}
}
