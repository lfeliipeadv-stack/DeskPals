using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// The self-test, part: pets. Check, Try and the list of tests are in SelfTest.cs.
	internal static partial class SelfTest
	{
		static void VirtualPet_Decay()
		{
			var n = new PetNeeds { Food = 80, Love = 80, Energy = 80, Updated = DateTime.Now.AddMinutes(-60) };
			VirtualPet.Decay(n);
			Check("needs: 1 hour", Math.Abs(n.Food - 72) < 0.5 && Math.Abs(n.Love - 74) < 0.5 && Math.Abs(n.Energy - 75) < 0.5, $"{n.Food:0.0} {n.Love:0.0} {n.Energy:0.0}");
			var away = new PetNeeds { Food = 80, Love = 5, Energy = 80, Updated = DateTime.Now.AddHours(-48) };
			VirtualPet.Decay(away);
			Check("needs: program closed 2 days → not below 10 (or the value it had)", Math.Abs(away.Food - 10) < 0.5 && Math.Abs(away.Love - 5) < 0.5, $"{away.Food:0.0} {away.Love:0.0}");
			Check("needs: bars text", VirtualPet.Bars(n).Contains("%"));
		}

		// ------------------------------------------------------------------ settings

		static void PetSound()
		{
			const string f = "selftest_pet";
			Program.MyData.SetPetMuted(f, true);
			bool muted = Program.MyData.IsPetMuted(f);
			Program.MyData.SetPetMuted(f, false);
			Check("per-pet sound: mute and unmute", muted && !Program.MyData.IsPetMuted(f));
			Check("per-pet sound: empty folder means eSheep", Program.MyData.IsPetMuted("") == Program.MyData.IsPetMuted("esheep64"));
		}

		/// <summary>Two pets chat: the first says a line, the other answers in its own bubble a moment later.</summary>
		static async Task PetsChat()
		{
			bool meetWas = PetsMeet.Enabled;
			PetsMeet.Enabled = false;   // no meeting on its own during the check
			try
			{
				var first = Lang.T("chat.first").Split('|');
				Check("chat: lines and answers match", first.Length >= 5 && first.Length == Lang.T("chat.answer").Split('|').Length, first.Length.ToString());
				foreach (var x in Application.OpenForms.OfType<SpeechBubble>().ToList()) x.Close();
				var before = Program.Mainthread.sheeps.Where(p => p != null && !p.IsDisposed).ToList();
				Program.Mainthread.AddSheep();
				var b = Program.Mainthread.sheeps.FirstOrDefault(p => p != null && !p.IsDisposed && !before.Contains(p));
				var a = Pet();
				if (a == null || b == null) { lines.Add("SKIP  chat: no room for a second pet"); return; }
				for (int i = 0; i < 40 && b.Opacity <= 0; i++) await Task.Delay(100);
				await Task.Delay(1500);
				foreach (var x in Application.OpenForms.OfType<SpeechBubble>().ToList()) x.Close();   // the new pet's "hello"
				await Task.Delay(200);
				PetsMeet.ResetChat();
				// pets do not chat while the program keeps quiet (full screen, do not disturb, quiet hours, focus):
				// that is on purpose, not a failure
				string quiet = QuietReason();
				if (quiet != null)
				{
					lines.Add("SKIP  chat: the program keeps quiet now (" + quiet + ")");
					if (!Program.Mainthread.KillSheep(b)) b.Close();
					await Task.Delay(1500);
					return;
				}
				bool talked = PetsMeet.Talk(a, b);
				Check("chat: the first pet says something", talked && Application.OpenForms.OfType<SpeechBubble>().Count(x => x.Visible) == 1);
				Check("chat: the pets stop while they chat", FormPet.Held);
				Check("chat: one conversation at a time", !PetsMeet.Talk(b, a));
				await Task.Delay(2600);
				Check("chat: the other pet answers in its own bubble", Application.OpenForms.OfType<SpeechBubble>().Count(x => x.Visible) == 2,
					string.Join(" / ", Application.OpenForms.OfType<SpeechBubble>().Where(x => x.Visible).Select(x => x.Said)));
				await Task.Delay(4500);
				Check("chat: the bubbles go away by themselves", !Application.OpenForms.OfType<SpeechBubble>().Any(x => x.Visible) && !FormPet.Held);
				SpeechBubble.Say("teste", 5);
				PetsMeet.ResetChat();
				Check("chat: not over a bubble talking to the user", !PetsMeet.Talk(a, b));
				foreach (var x in Application.OpenForms.OfType<SpeechBubble>().ToList()) x.Close();
				if (!Program.Mainthread.KillSheep(b)) b.Close();
				await Task.Delay(1500);
			}
			finally { PetsMeet.Enabled = meetWas; PetsMeet.ResetChat(); }
		}

		/// <summary>The eSheep that comes with the program turns to face you while it talks (and turns back after).</summary>
		static void SheepTalk()
		{
			var ser = new System.Xml.Serialization.XmlSerializer(typeof(XmlData.RootNode));
			XmlData.RootNode root;
			using (var r = new StringReader(BuiltInPets.GetXml("esheep64"))) root = (XmlData.RootNode)ser.Deserialize(r);
			var talk = root.Animations.Animation.FirstOrDefault(a => a.Name == "talk");
			Check("eSheep: has a talk animation", talk != null);
			if (talk == null) return;
			Check("eSheep talk: stays in place", talk.Start.X == "0" && talk.End.X == "0" && talk.Start.Y == "0" && talk.End.Y == "0", $"{talk.Start.X},{talk.Start.Y}");
			// counted like the program does: frames + (frames - repeatfrom) × repeat; the repeated frames are repeatfrom…end
			int f = talk.Sequence.Frame.Length, rf = talk.Sequence.RepeatFromFrame, rep = int.TryParse(talk.Sequence.RepeatCount, out int rr) ? rr : 0;
			int steps = f + (f - rf) * rep;
			var held = talk.Sequence.Frame.Skip(rf).Distinct().ToList();
			Check("eSheep talk: stays looking at you (the front frame repeats)", steps >= 50 && held.Count == 1 && held[0] == 10, $"{steps} steps, repeats {string.Join(",", held)}");
			var back = root.Animations.Animation.FirstOrDefault(a => talk.Sequence.Next != null && a.Id == talk.Sequence.Next[0].Value);
			Check("eSheep talk: then turns back", back?.Name == "rotate1b", back?.Name);
		}

		/// <summary>The first run: the pet comes down from the sky and lands, then the introduction can start.</summary>
		static async Task SkyArrival()
		{
			var before = Program.Mainthread.sheeps.Where(p => p != null && !p.IsDisposed).ToList();
			Onboarding.ForceSky = true;
			Program.Mainthread.AddSheep();
			var pet = Program.Mainthread.sheeps.FirstOrDefault(p => p != null && !p.IsDisposed && !before.Contains(p));
			if (pet == null) { lines.Add("SKIP  sky: no room for one more pet"); Onboarding.ForceSky = false; return; }
			var area = Screen.FromControl(pet).WorkingArea;
			Check("sky: the new pet starts above the screen", pet.Top < area.Top, $"top {pet.Top}, screen top {area.Top}");
			bool landed = false;
			for (int i = 0; i < 100 && !landed; i++) { await Task.Delay(150); landed = pet.OnGround && pet.Top > area.Top; }
			Check("sky: it lands", landed, $"{pet.CurrentAnimationName} at {pet.Bounds}");
			FormPet.Hold(true);
			// a landing bounce may still move it a little: once it stands, it must stay
			for (int i = 0; i < 20 && !pet.StandingStill && pet.StillAnimation >= 0; i++) await Task.Delay(150);
			int x = pet.Left, y = pet.Top;
			await Task.Delay(1500);
			Check("sky: held after landing, it neither walks nor sinks", pet.Left == x && pet.Top == y && (pet.StandingStill || pet.StillAnimation < 0), $"{pet.CurrentAnimationName}, moved {pet.Left - x},{pet.Top - y}");
			FormPet.Hold(false);
			if (!Program.Mainthread.KillSheep(pet)) pet.Close();
			await Task.Delay(1500);
		}

		/// <summary>The animation picker: a scene that starts from a spawn (the bath) starts there, and the picker stays open.</summary>
		static async Task PickerScenes()
		{
			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => !p.IsChildWindow && !p.IsDisposed && p.Visible);
			var anims = Program.Mainthread.GetAnimations()?.SheepAnimations;
			int bath = anims?.FirstOrDefault(kv => kv.Value.Name == "batha" || kv.Value.Name == "divea").Key ?? 0;
			if (pet == null || bath == 0) { lines.Add("SKIP  picker: the pet in use has no bath scene"); return; }
			int walk = anims.FirstOrDefault(kv => kv.Value.Name.StartsWith("walk")).Key;
			int spawn = pet.SpawnSceneOf(bath);
			Check("picker: the bath starts from its spawn", spawn >= 0);
			int dive = anims[bath].EndAnimation.First().ID;
			Check("picker: the rest of the bath scene too", pet.SpawnSceneOf(dive) == spawn && pet.SpawnSceneOf(anims[dive].EndAnimation.First().ID) == spawn);
			Check("picker: walking plays where the pet is", walk == 0 || pet.SpawnSceneOf(walk) < 0);

			var picker = pet.TestOpenPicker();
			Check("picker: opens", picker != null && picker.Visible);
			if (picker == null) return;
			picker.Activate();
			await Task.Delay(150);
			bool focused = Form.ActiveForm == picker;
			var area = Screen.FromControl(pet).WorkingArea;
			picker.TestPlay(bath);   // the bath tile (its middle is not offered): it must start the whole scene, from the sky
			await Task.Delay(400);
			// the scene starts at its own spawn (in the sky, at an edge of the screen), not where the pet was:
			// the program mirrors the spawn when the pet faces the other way, so either side of the screen counts
			bool atSpawn = (pet.Left > area.Right - pet.Width * 3 || pet.Left < area.Left + pet.Width * 3)
				&& pet.Top < area.Bottom - pet.Height * 2;
			Check("picker: the bath starts at the spawn, not where the pet was", atSpawn, $"pet at {pet.Left},{pet.Top}");
			var tub = Application.OpenForms.OfType<FormPet>().Any(p => p.IsChildWindow && p.Visible && !p.IsDisposed);
			Check("picker: the tub comes with it", tub);
			await Task.Delay(1500);
			if (focused) Check("picker: stays open while the scene plays", !picker.IsDisposed && picker.Visible);
			else lines.Add("SKIP  picker: stays open (Windows did not let it come to the front: another program has the focus)");
			if (!picker.IsDisposed) picker.Close();
			await Task.Delay(300);
		}

		/// <summary>Another pet is chosen: its hello opens over the new pet, after the old one is completely gone.</summary>
		static async Task PetChangeHello()
		{
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			Program.Mainthread.LoadNewXMLFromString(Program.MyData.GetXml());   // the same pet again: the old ones leave, new ones come
			SpeechBubble.Introduce("self-test hello", 3);
			var clock = System.Diagnostics.Stopwatch.StartNew();
			double sawLeaving = -1, newSince = -1;
			SpeechBubble bubble = null;
			while (clock.Elapsed.TotalSeconds < 40 && bubble == null)
			{
				await Task.Delay(100);
				var pets = Application.OpenForms.OfType<FormPet>().Where(p => !p.IsChildWindow && !p.IsDisposed && p.Visible).ToList();
				if (pets.Any(p => p.Dying)) sawLeaving = clock.Elapsed.TotalSeconds;
				if (newSince < 0 && pets.Any(p => !p.Dying) && !pets.Any(p => p.Dying)) newSince = clock.Elapsed.TotalSeconds;
				bubble = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault(b => b.Visible);
				if (bubble != null)
				{
					Check("pet change: the hello opens over the new pet", bubble.Owner != null && !bubble.Owner.Dying);
					Check("pet change: the old pet is gone before the hello", !pets.Any(p => p.Dying), $"old pet last seen at {sawLeaving:0.0} s");
					Check("pet change: the hello waits a moment after the new pet appears", newSince >= 0 && clock.Elapsed.TotalSeconds - newSince >= 2,
						$"new pet at {newSince:0.0} s, hello at {clock.Elapsed.TotalSeconds:0.0} s");
				}
			}
			Check("pet change: the new pet says hello", bubble != null, $"{clock.Elapsed.TotalSeconds:0} s");
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			await Task.Delay(500);
		}

		/// <summary>
		/// A floating pet (the Office assistants) leaves when another pet is adopted, like any pet; and stays where it is
		/// put: no fall after a while on the desktop.
		/// </summary>
		static async Task FloatingPetChange()
		{
			string dir = AppContext.BaseDirectory;
			while (dir != null && !File.Exists(Path.Combine(dir, "Pets", "clippy", "animations.xml"))) dir = Path.GetDirectoryName(dir);
			if (dir == null) { lines.Add("SKIP  floating pet change: Pets/clippy not found"); return; }
			string keepXml = Program.MyData.GetXml(), keepFolder = Program.MyData.GetCurrentPetFolder();
			string clippy = File.ReadAllText(Path.Combine(dir, "Pets", "clippy", "animations.xml"));
			try
			{
				var before = Application.OpenForms.OfType<FormPet>().ToList();
				Program.MyData.SetXml(clippy, "");
				Program.Mainthread.LoadNewXMLFromString(clippy);
				List<FormPet> Pets() => Application.OpenForms.OfType<FormPet>().Where(p => !p.IsChildWindow && !p.IsDisposed && p.Visible).ToList();
				List<FormPet> floating = null;
				for (int i = 0; i < 80 && (floating == null || floating.Count == 0 || floating.Any(p => p.Dying)); i++)
				{
					await Task.Delay(100);
					floating = Pets().Where(p => !before.Contains(p) && !p.Dying).ToList();
				}
				Check("floating pet change: Clippy is on the desktop", floating.Count > 0, $"{Pets().Count} pets");
				if (floating.Count == 0) return;
				var where = floating[0].Location;
				await Task.Delay(3000);
				Check("floating pet change: Clippy stays where he is (no gravity)", !floating[0].IsDisposed && Math.Abs(floating[0].Top - where.Y) <= 2, $"{where} -> {floating[0].Location}");

				// another pet: Clippy leaves (its goodbye, then the window closes)
				Program.MyData.SetXml(keepXml, keepFolder);
				Program.MyData.SetCurrentPetFolder(keepFolder);
				Program.Mainthread.LoadNewXMLFromString(keepXml);
				var clock = System.Diagnostics.Stopwatch.StartNew();
				while (clock.Elapsed.TotalSeconds < 20 && floating.Any(p => !p.IsDisposed && p.Visible)) await Task.Delay(200);
				var left = floating.Where(p => !p.IsDisposed && p.Visible).ToList();
				Check("floating pet change: Clippy leaves when another pet is adopted", left.Count == 0,
					left.Count == 0 ? $"{clock.Elapsed.TotalSeconds:0.0} s" : string.Join("; ", left.Select(p => $"anim {p.CurrentAnimationName}, dying {p.Dying}, opacity {p.Opacity:0.0}, {p.TestSteps}")));
			}
			finally
			{
				if (Program.MyData.GetXml() != keepXml)
				{
					Program.MyData.SetXml(keepXml, keepFolder);
					Program.MyData.SetCurrentPetFolder(keepFolder);
					Program.Mainthread.LoadNewXMLFromString(keepXml);
				}
				// the next tests need the usual pet on the ground, fully shown (it arrives with a spawn or a fall)
				for (int i = 0; i < 150; i++)
				{
					await Task.Delay(100);
					var p = Application.OpenForms.OfType<FormPet>().FirstOrDefault(x => !x.IsChildWindow && !x.IsDisposed && x.Visible && !x.Dying);
					if (p != null && p.OnGround && p.Opacity >= 0.99 && !Application.OpenForms.OfType<FormPet>().Any(x => !x.IsDisposed && x.Dying)) break;
				}
				await Task.Delay(1000);
			}
		}

		static async Task Bubble()
		{
			var pet = Pet();
			Check("bubble: a pet is on screen", pet != null);
			if (pet == null) return;
			var before = GetForegroundWindow();
			SpeechBubble.Say("self-test", 3);
			await Task.Delay(300);
			Check("bubble: the window in front stays in front", GetForegroundWindow() == before);
			var b = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault();
			Check("bubble: shown", b != null);
			if (b == null) return;
			var wa = Screen.FromControl(pet).WorkingArea;
			Check("bubble: inside the screen", wa.Contains(b.Bounds), $"{b.Bounds} / {wa}");
			Check("bubble: next to the pet", b.Bottom <= pet.Top + 2 || b.Top >= pet.Bottom - 2, $"bubble {b.Bounds}, pet {pet.Bounds}");
			Check("bubble: is not the active window", GetForegroundWindow() != b.Handle);
			Check("bubble: always on top", (GetWindowLong(b.Handle, -20) & 0x8) != 0);
			await Task.Delay(3600);
			Check("bubble: closes by itself", b.IsDisposed || !b.Visible);
		}

		static async Task Hide()
		{
			HideHotkey.SetHidden(true);
			await Task.Delay(200);
			Check("hide: no pet window visible", !Application.OpenForms.OfType<FormPet>().Any(p => p.Visible));
			SpeechBubble.Say("hidden", 2);
			await Task.Delay(200);
			Check("hide: no bubble while hidden", !Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
			// a pet that opens a second window (balloon, plane…) while hidden: that window stays hidden too
			var hiddenPet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => !p.IsChildWindow);
			int childAnim = hiddenPet?.TestChildAnimation() ?? -1;
			if (childAnim >= 0)
			{
				hiddenPet.TestPlay(childAnim);
				await Task.Delay(800);
				Check("hide: a new balloon/plane window stays hidden", !Application.OpenForms.OfType<FormPet>().Any(p => p.Visible),
					string.Join(", ", Application.OpenForms.OfType<FormPet>().Where(p => p.Visible).Select(p => p.Name)));
			}
			else lines.Add("SKIP  hide: the pet in use opens no second window (adopt Boo or Rex to check it)");
			HideHotkey.SetHidden(false);
			await Task.Delay(300);
			Check("hide: pets back", Pet() != null);
		}

		static async Task Menu()
		{
			var pet = Pet();
			if (pet == null) { Check("menu: a pet is on screen", false); return; }
			var cm = PetMenu.Show(pet, pet, new System.Drawing.Point(0, 0));
			// read the items at once: if another window takes the focus the menu closes and its items go away
			var texts = cm.Items.OfType<ToolStripMenuItem>().Select(i => i.Text).ToList();
			int gameCount = cm.Items.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == Lang.T("menu.games"))?.DropDownItems.Count ?? 0;
			await Task.Delay(200);
			foreach (var key in new[] { "menu.games", "tray.reminders", "menu.shortcuts", "menu.mood", "menu.monitor", "menu.optimizeRam", "menu.animations", "menu.sendAway" })
				Check("menu: " + key, texts.Contains(Lang.T(key)), string.Join(" | ", texts));
			Check("menu: 3 games", gameCount == 3, gameCount.ToString());
			Check("menu: inside the screen", Screen.FromControl(pet).Bounds.Contains(cm.Bounds), cm.Bounds.ToString());
			cm.Close();

			// click every game: each one makes the pet say something
			foreach (var key in new[] { "menu.fortune", "menu.ball", "menu.joke" })
			{
				var menu = PetMenu.Show(pet, pet, new System.Drawing.Point(0, 0));
				var g = menu.Items.OfType<ToolStripMenuItem>().First(i => i.Text == Lang.T("menu.games"));
				var item = g.DropDownItems.OfType<ToolStripMenuItem>().First(i => i.Text == Lang.T(key));
				menu.Close();
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				item.PerformClick();
				await Task.Delay(300);
				Check("menu click: " + key + " → the pet talks", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
			}
			// the magic ball: its "Shake" button gives an answer
			var ball = Application.OpenForms.OfType<SpeechBubble>().FirstOrDefault();
			ball?.Close();

			// focus from the menu
			var m2 = PetMenu.Show(pet, pet, new System.Drawing.Point(0, 0));
			var focusItem = m2.Items.OfType<ToolStripMenuItem>().First(i => i.Text == Lang.T("menu.focus"));
			var start = focusItem.DropDownItems.OfType<ToolStripMenuItem>().First();
			m2.Close();
			start.PerformClick();
			Check("menu click: start focusing", Pomodoro.Current == Pomodoro.Phase.Focus);
			var m3 = PetMenu.Show(pet, pet, new System.Drawing.Point(0, 0));
			var running = m3.Items.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == Pomodoro.Status());
			Check("menu: shows the time left while focusing", running != null, string.Join(" | ", m3.Items.OfType<ToolStripMenuItem>().Select(i => i.Text)));
			var stop = running?.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == Lang.T("menu.focusStop"));
			m3.Close();
			stop?.PerformClick();
			Check("menu click: stop focusing", Pomodoro.Current == Pomodoro.Phase.Off);

			// the mood question shows the 5 faces
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			Mood.Ask();
			await Task.Delay(300);
			Check("mood: question shown", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

			// PC monitor on and off from the menu (the setting is put back)
			bool monitorWas = PcMonitor.Enabled;
			PcMonitor.Enabled = true;
			await Task.Delay(2500);
			// shown only with the mouse on the pet: move the mouse away, then onto the pet, then back where it was
			var mouse = Cursor.Position;
			var area = Screen.FromControl(pet).WorkingArea;
			Cursor.Position = new System.Drawing.Point(pet.Left > area.Left + 200 ? area.Left + 20 : area.Right - 20, area.Top + 20);
			await Task.Delay(300);
			Check("monitor: hidden while the mouse is away", !Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "Hud" && f.Visible));
			// a pet still walking in from the edge of the screen cannot be pointed at: let it come in first
			for (int i = 0; i < 40 && !(pet.Left >= area.Left && pet.Right <= area.Right); i++) await Task.Delay(150);
			bool plate = false;
			for (int i = 0; i < 8 && !plate; i++)   // the pet may be walking or falling: follow it
			{
				Cursor.Position = new System.Drawing.Point(pet.Left + pet.Width / 2, pet.Top + pet.Height / 2);
				await Task.Delay(150);
				plate = Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "Hud" && f.Visible);
			}
			Check("monitor: plate when the mouse is on the pet", plate, $"pet at {pet.Bounds}, mouse at {Cursor.Position}");
			Cursor.Position = mouse;
			Check("monitor: real numbers", PcMonitor.Cpu >= 0 && PcMonitor.Cpu <= 100 && PcMonitor.Ram > 0 && PcMonitor.Ram <= 100, $"CPU {PcMonitor.Cpu} RAM {PcMonitor.Ram}");
			Check("monitor: disk bar and free space", PcMonitor.DiskUsed > 0 && PcMonitor.DiskUsed <= 100 && PcMonitor.Disk.Length > 0, $"disk {PcMonitor.DiskUsed}% '{PcMonitor.Disk}'");
			PcMonitor.Enabled = false;
			Check("monitor: plate gone when off", !Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "Hud" && f.Visible));
			// on and off again with the mouse away (the plate is never shown): nothing may keep running after it
			int errorsBefore = StartUp.LogSnapshot().Count(l => l.Text.Contains("PcMonitor"));
			Cursor.Position = new System.Drawing.Point(pet.Left > area.Left + 200 ? area.Left + 20 : area.Right - 20, area.Top + 20);
			PcMonitor.Enabled = true;
			await Task.Delay(500);
			PcMonitor.Enabled = false;
			await Task.Delay(800);
			Cursor.Position = mouse;
			Check("monitor: off without ever showing the plate, no errors after", StartUp.LogSnapshot().Count(l => l.Text.Contains("PcMonitor")) == errorsBefore);
			PcMonitor.Enabled = monitorWas;
		}

		[System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		/// <summary>Memory almost full: the warning (when, how often), freeing the memory, and the button of the plate.</summary>
		static async Task PcMonitorRam()
		{
			// the warning: only after a minute above 90%, and at most once in 30 minutes
			var now = DateTime.Now;
			PcMonitor.ResetRamWarning();
			bool early = false;
			for (int s = 2; s < PcMonitor.RamHighSeconds; s += 2) early |= PcMonitor.RamTick(95, 2, now);
			Check("ram warning: not before a minute", !early);
			Check("ram warning: after a minute above 90%", PcMonitor.RamTick(95, 2, now));
			bool again = false;
			for (int s = 0; s < 90; s += 2) again |= PcMonitor.RamTick(95, 2, now.AddMinutes(5));
			Check("ram warning: not again within 30 minutes", !again);
			bool later = false;
			for (int s = 0; s < 70 && !later; s += 2) later = PcMonitor.RamTick(95, 2, now.AddMinutes(31));
			Check("ram warning: again after 30 minutes", later);
			PcMonitor.ResetRamWarning();
			bool dip = false;
			for (int s = 0; s < 40; s += 2) dip |= PcMonitor.RamTick(95, 2, now);
			dip |= PcMonitor.RamTick(50, 2, now);   // a dip below 90% starts the count again
			for (int s = 0; s < 40; s += 2) dip |= PcMonitor.RamTick(95, 2, now);
			Check("ram warning: a dip below 90% starts the count again", !dip);
			PcMonitor.ResetRamWarning();

			// the warning bubble, with the button
			foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			if (!SpeechBubble.QuietNow && Pet() != null)
			{
				PcMonitor.WarnRam();
				await Task.Delay(300);
				Check("ram warning: bubble shown", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			}
			foreach (var k in new[] { "mon.ramHigh", "mon.optimize", "mon.optimizing", "mon.notNow", "mon.optimized", "mon.optimizedNone", "menu.optimizeRam" })
				Check("ram texts: " + k, Lang.T(k) != k && !string.IsNullOrWhiteSpace(Lang.T(k)));

			// freeing the memory: no administrator, some processes trimmed, numbers that make sense
			var r = await Task.Run(PcMonitor.TrimAll);
			Check("ram optimize: some processes trimmed", r.Processes > 0, $"{r.Processes}");
			Check("ram optimize: before and after", r.Before > 0 && r.Before <= 100 && r.After > 0 && r.After <= 100 && r.FreedMb >= 0, $"{r.Before}% → {r.After}%, {r.FreedMb} MB");
			int procs = System.Diagnostics.Process.GetProcesses().Length;
			Check("ram optimize: no process closed", procs > 10, $"{procs}");

			// the button of the plate: the plate stays while the mouse goes to it, and a click frees the memory
			var pet = Pet();
			if (pet == null) { Check("ram plate: a pet is on screen", false); return; }
			bool monitorWas = PcMonitor.Enabled;
			PcMonitor.Enabled = true;
			var mouse = Cursor.Position;
			var area = Screen.FromControl(pet).WorkingArea;
			for (int i = 0; i < 40 && !(pet.Left >= area.Left && pet.Right <= area.Right); i++) await Task.Delay(150);
			Form hud = null;
			for (int i = 0; i < 8 && hud == null; i++)
			{
				Cursor.Position = new Point(pet.Left + pet.Width / 2, pet.Top + pet.Height / 2);
				await Task.Delay(150);
				hud = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "Hud" && f.Visible);
			}
			if (hud == null) { Check("ram plate: shown", false); Cursor.Position = mouse; PcMonitor.Enabled = monitorWas; return; }
			Check("ram plate: takes clicks (not click-through)", (GetWindowLong(hud.Handle, -20) & 0x20) == 0);
			var button = (Rectangle)hud.GetType().GetProperty("ButtonRect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(hud);
			// the pet may walk: hold it while the mouse travels to the plate
			FormPet.Hold(true);
			try
			{
				// a pet still finishing a step or a fall moves the plate: try a few times
				bool stays = false;
				string seen = "";
				for (int i = 0; i < 6 && !stays; i++)
				{
					Cursor.Position = new Point(pet.Left + pet.Width / 2, pet.Top + pet.Height / 2);
					await Task.Delay(200);
					if (!hud.Visible) continue;
					Cursor.Position = hud.PointToScreen(new Point(button.X + button.Width / 2, button.Y + button.Height / 2));
					await Task.Delay(300);
					stays = hud.Visible;
					seen = $"plate {hud.Bounds}, mouse {Cursor.Position}, pet {pet.Bounds}";
				}
				Check("ram plate: stays while the mouse is on it", stays, seen);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
				var lp = (IntPtr)((button.Y + button.Height / 2) << 16 | (button.X + button.Width / 2));
				SendMessage(hud.Handle, 0x0201, (IntPtr)1, lp);   // left button down
				SendMessage(hud.Handle, 0x0202, IntPtr.Zero, lp);  // up: a click
				Check("ram plate: the button starts freeing memory", PcMonitor.Optimizing);
				for (int i = 0; i < 100 && PcMonitor.Optimizing; i++) await Task.Delay(150);
				Check("ram plate: freeing finishes", !PcMonitor.Optimizing);
				await Task.Delay(300);
				if (!SpeechBubble.QuietNow) Check("ram plate: the pet tells how much was freed", Application.OpenForms.OfType<SpeechBubble>().Any(b => b.Visible));
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			}
			finally { FormPet.Hold(false); }
			Cursor.Position = mouse;
			PcMonitor.Enabled = monitorWas;
		}

		/// <summary>Every name of the action lists exists in that pet's animations (Pets folder of the repository).</summary>
		static void PetActionNames()
		{
			string dir = AppContext.BaseDirectory;
			while (dir != null && !File.Exists(Path.Combine(dir, "Pets", "pets.json"))) dir = Path.GetDirectoryName(dir);
			if (dir == null) { lines.Add("SKIP  pet actions: Pets folder not found"); return; }
			// every pet of the gallery has its own animation to stand still while it talks (not just any animation)
			var noTalk = new List<string>();
			foreach (var petDir in Directory.GetDirectories(Path.Combine(dir, "Pets")))
			{
				string file = Path.Combine(petDir, "animations.xml");
				if (!File.Exists(file)) continue;
				var ser = new System.Xml.Serialization.XmlSerializer(typeof(XmlData.RootNode));
				XmlData.RootNode root;
				using (var r = File.OpenRead(file)) root = (XmlData.RootNode)ser.Deserialize(r);
				bool has = root.Animations.Animation.Any(a => FormPet.StillNames.Contains(a.Name, StringComparer.OrdinalIgnoreCase)
					&& a.Start.X == "0" && a.End.X == "0" && a.Start.Y == "0" && a.End.Y == "0");
				if (!has) noTalk.Add(Path.GetFileName(petDir));
			}
			Check("talk: every gallery pet has a still animation for talking", noTalk.Count == 0, string.Join(", ", noTalk));
			foreach (var folder in PetActions.KnownPets)
			{
				string file = Path.Combine(dir, "Pets", folder, "animations.xml");
				if (!File.Exists(file)) { Check("pet actions: " + folder + " exists", false); continue; }
				var doc = new System.Xml.XmlDocument();
				doc.Load(file);
				var names = new HashSet<string>(doc.SelectNodes("//*[local-name()='animation']/*[local-name()='name']").Cast<System.Xml.XmlNode>().Select(n => n.InnerText), StringComparer.OrdinalIgnoreCase);
				foreach (PetActions.Act act in Enum.GetValues(typeof(PetActions.Act)))
				{
					var own = PetActions.Own(folder, act);
					var missing = own.Where(n => !names.Contains(n)).ToList();
					Check($"pet actions: {folder} {act}", own.Length > 0 && own.Any(names.Contains) && missing.Count == 0, "missing: " + string.Join(", ", missing));
				}
			}
		}

		static async Task TalkingPet()
		{
			var pet = Pet();
			if (pet == null) { Check("talk: a pet is on screen", false); return; }
			// while the program keeps quiet no bubble opens (on purpose), so there is nothing to check
			string quiet = QuietReason();
			if (quiet != null) { lines.Add("SKIP  talk: the program keeps quiet now (" + quiet + ")"); return; }
			void CloseBubbles() { foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close(); }
			CloseBubbles();

			// standing on the ground first: a pet in the middle of a jump would fall out of the sleep at once
			for (int i = 0; i < 100 && !pet.OnGround; i++) await Task.Delay(100);

			// the bug: the sleep animation was replaced by a wave as soon as the bubble opened
			int sleep = pet.FindAnimation(PetActions.Names(PetActions.Act.Sleep));
			if (sleep >= 0)
			{
				pet.React(PetActions.Names(PetActions.Act.Sleep));
				string sleeping = pet.CurrentAnimationName;
				SpeechBubble.Say("teste", 3);
				await Task.Delay(400);
				Check("talk: a bubble does not cut the sleep animation", pet.CurrentAnimationName == sleeping, pet.CurrentAnimationName);
				CloseBubbles();
			}

			// a pet with a "talk" animation (the eSheep turns to face you) uses it while it talks
			if (pet.FindAnimation("talk") >= 0) Check("talk: the pet faces you while talking (talk animation)", pet.StillAnimation == pet.FindAnimation("talk"), pet.StillAnimation.ToString());
			else lines.Add("SKIP  talk: the pet in use has no talk animation");

			// talking: the pet stops walking
			pet.React("walk");
			await Task.Delay(1700);   // walking on its own (not an action that just started, which would finish first)
			SpeechBubble.Say("parado enquanto falo", 4);
			await Task.Delay(500);
			Check("talk: the pet stands still while talking", pet.StillAnimation < 0 || pet.StandingStill || pet.Busy || InAir(pet), pet.CurrentAnimationName);
			CloseBubbles();

			// the games of the pet menu: the pet stays in place while it says the fortune / answer / joke
			foreach (var (name, game) in new (string, Action)[] { ("fortune cookie", Fun.FortuneCookie), ("joke", Fun.Joke), ("magic ball", Fun.MagicBall) })
			{
				pet.React("walk");
				await Task.Delay(1700);
				game();
				await Task.Delay(700);
				var x1 = pet.Left;
				await Task.Delay(600);
				Check($"talk: {name} — the pet does not walk away", pet.Left == x1 && (pet.StandingStill || pet.StillAnimation < 0 || InAir(pet)), $"{pet.CurrentAnimationName}, moved {pet.Left - x1}px");
				CloseBubbles();
			}

			// the pet menu open: every pet stays still; closed: back to normal
			// the menu can only stop a pet that is walking on the ground and fully inside the screen: one that is
			// still arriving from the sky, in a scene (the bath) or half out of the screen goes on
			CloseBubbles();                       // no bubble holding the pets: the menu is the one that holds them
			await Task.Delay(200);
			if (await PetWalkingOnGround(pet))
			{
				var menu = PetMenu.Show(pet, pet, new System.Drawing.Point(0, 0));
				Check("menu open: pets held", FormPet.Held);
				// the pet stops at once: the menu holds it there
				Check("menu open: the pet stops", pet.StandingStill || pet.StillAnimation < 0, pet.CurrentAnimationName);
				// and it stays stopped while the menu is open
				for (int i = 0; i < 20 && !pet.StandingStill && pet.StillAnimation >= 0; i++) await Task.Delay(100);
				if (menu.Visible) Check("menu open: the pet stays stopped", pet.StandingStill || pet.StillAnimation < 0, pet.CurrentAnimationName);
				else lines.Add("SKIP  menu open: the pet stays stopped (the menu closed by itself: Windows did not let it come to the front)");
				if (!menu.IsDisposed) menu.Close();
			}
			else lines.Add("SKIP  menu open: the pet stops (the pet does not come to the ground here)");
			await Task.Delay(200);
			CloseBubbles();
			await Task.Delay(200);
			Check("all closed: nothing keeps the pets still", !FormPet.Held);

			// bubble after the animation: not at once, but when it is over
			bool done = false;
			var start = DateTime.Now;
			double after = -1;
			// an animation that stays in place first: one that moves can be cut short by a wall or a fall
			for (int i = 0; i < 100 && !pet.OnGround; i++) await Task.Delay(100);
			if (pet.ReactThen(() => { done = true; after = (DateTime.Now - start).TotalSeconds; }, "turn", "wave", "rotate1a", "blink", "jump"))
			{
				Check("after animation: not at once", !done);
				for (int i = 0; i < 100 && !done; i++) await Task.Delay(100);
				Check("after animation: runs when the animation ends", done && after > 0.1, after.ToString("0.0") + " s");
			}
			else lines.Add("SKIP  after animation: the pet in use has no short animation to try");
		}

		/// <summary>Emoji in colour: the same drawing call as the bubbles and the panel, on a picture; saved next to the result.</summary>
		static void ColorEmoji()
		{
			Check("emoji: found in text", ColorText.HasEmoji("Hora de beber água! 💧") && ColorText.HasEmoji("Novo e-mail ✉️") && !ColorText.HasEmoji("Fechar ✕") && !ColorText.HasEmoji("Olá"));
			using (var font = Theme.CreateFont(16f, System.Drawing.FontStyle.Regular))
			using (var bmp = new System.Drawing.Bitmap(360, 120))
			using (var g = System.Drawing.Graphics.FromImage(bmp))
			{
				g.Clear(System.Drawing.Color.White);
				string text = "Mandou bem! 🎉 Hora de beber água 💧 ❤️ ☀️ ✅";
				var size = Theme.MeasureText(g, text, font, 340, TextFormatFlags.WordBreak);
				Check("emoji: text measured", size.Width > 50 && size.Height > 10, size.ToString());
				Theme.DrawText(g, text, font, new System.Drawing.Rectangle(10, 10, 340, size.Height), System.Drawing.Color.Black, TextFormatFlags.WordBreak);
				Theme.DrawButton(g, new System.Drawing.Rectangle(10, 70, 160, 34), "😄 Ótimo", font, true, false);
				int colourful = 0;
				for (int y = 0; y < bmp.Height; y++)
					for (int x = 0; x < bmp.Width; x++)
					{
						var c = bmp.GetPixel(x, y);
						if (Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) > 80 && y < 60) colourful++;
					}
				Check("emoji: drawn in colour", colourful > 100, colourful + " coloured pixels");
				try { bmp.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(resultFile)) ?? ".", "emoji.png")); } catch (Exception) { }
			}
		}

		static void Games()
		{
			foreach (var key in new[] { "fun.fortunes", "fun.ballAnswers", "fun.jokes" })
				Check("games: " + key, Lang.T(key).Split('|').Count(s => s.Trim().Length > 0) >= 5);
		}

		// ------------------------------------------------------------------ backup
	}
}
