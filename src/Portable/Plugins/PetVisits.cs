using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== a friend comes to visit

	/// <summary>
	/// Off unless turned on. Now and then (every 30 minutes, hour or two hours) another pet of the user's collection (the
	/// downloaded ones and the ones that come with the program) comes to the desktop, walks around with the pet in use for
	/// a few minutes, and leaves by itself. Nothing changes in the settings: the visitor has its own animations and sprites.
	/// A user of desktopPet asked for it ("my sheep feels lonely"); Adriano warned that pets added again and again end in
	/// the uninstall button, so the visitor always goes away, only one at a time, and never during quiet time.
	/// </summary>
	public static class PetVisits
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("PetVisits", false);
			set { Program.MyData.SetSetting("PetVisits", value); Apply(); }
		}

		public static readonly int[] EveryChoices = { 30, 60, 120 };

		/// <summary>Minutes between visits (30, 60 or 120).</summary>
		public static int Every
		{
			get { int m = Program.MyData.GetSettingInt("PetVisitsEvery", 60); return EveryChoices.Contains(m) ? m : 60; }
			set { Program.MyData.SetSetting("PetVisitsEvery", value); next = DateTime.Now.AddMinutes(value); }
		}

		/// <summary>How long a visitor stays.</summary>
		public static TimeSpan Stay = TimeSpan.FromMinutes(4);

		static Timer timer, leave;
		static DateTime next = DateTime.MaxValue;
		static FormPet visitor;
		static Xml visitorXml;

		/// <summary>The pet visiting now (or null).</summary>
		public static FormPet Visitor => visitor != null && !visitor.IsDisposed ? visitor : null;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 60000 }; timer.Tick += (s, e) => Tick(); }
			if (Enabled) { if (next == DateTime.MaxValue) next = DateTime.Now.AddMinutes(Math.Min(Every, 20)); timer.Start(); }
			else { timer.Stop(); next = DateTime.MaxValue; GoAway(); }
		}

		static void Tick()
		{
			if (SelfTest.Running && !SelfTest.Live) return;
			if (Visitor != null || DateTime.Now < next) return;
			// quiet time (focus, full screen, pets hidden, do not disturb): later
			if (SpeechBubble.QuietNow || HideHotkey.Hidden) { next = DateTime.Now.AddMinutes(5); return; }
			next = DateTime.Now.AddMinutes(Every);
			Visit();
		}

		/// <summary>The pets that can visit: on this computer, not the one in use.</summary>
		public static List<string> Friends()
		{
			string current = Program.MyData.GetCurrentPetFolder() ?? "";
			var list = new List<string>();
			try
			{
				list.AddRange(BuiltInPets.Folders);
				list.AddRange(PetStore.Folders());
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			return list.Where(f => !string.IsNullOrEmpty(f) && !string.Equals(f, current, StringComparison.OrdinalIgnoreCase)
				&& !(string.IsNullOrEmpty(current) && string.Equals(f, BuiltInPets.Folders.First(), StringComparison.OrdinalIgnoreCase)))
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		static readonly Random rnd = new Random();

		/// <summary>A friend comes now ("Call a visit now" too). False when there is no other pet or it could not be read.</summary>
		public static bool Visit(string folder = null)
		{
			if (Visitor != null) return true;
			var friends = Friends();
			if (folder == null)
			{
				if (friends.Count == 0) { SpeechBubble.Say(Lang.T("visit.noFriends"), 8); return false; }
				folder = friends[rnd.Next(friends.Count)];
			}
			try
			{
				string text = BuiltInPets.IsBuiltIn(folder) ? BuiltInPets.GetXml(folder) : PetStore.Load(folder);
				var xml = new Xml(Program.MyData.GetScale());
				if (!xml.ReadXMLFromText(text)) { xml.Dispose(); return false; }
				var animations = new Animations(xml);
				xml.LoadAnimations(animations);
				var pet = new FormPet(animations, xml);
				foreach (var sprite in xml.sprites) pet.AddImage(sprite);
				pet.Show(xml.spriteWidth, xml.spriteHeight);
				pet.Play(true);
				visitor = pet; visitorXml = xml;
				pet.FormClosed += (s, e) =>
				{
					if (visitor == pet) visitor = null;
					var x = visitorXml; visitorXml = null;
					// the sprites go once the window is gone
					if (x != null) { try { x.Dispose(); } catch (Exception skipped) { LogFile.Ignored(skipped); } }
				};
				string name = string.IsNullOrWhiteSpace(xml.AnimationXML?.Header?.Petname) ? folder : xml.AnimationXML.Header.Petname.Trim();
				LogFile.Important("visit: " + folder);
				Program.Mainthread?.ReactAll("wave", "hello", "happy", "jump");
				SpeechBubble.Notify(Lang.T("visit.arrived", name), 7);
				if (leave == null) { leave = new Timer(); leave.Tick += (s, e) => { leave.Stop(); GoAway(); }; }
				leave.Stop();
				leave.Interval = (int)Math.Max(10000, Stay.TotalMilliseconds);
				leave.Start();
				return true;
			}
			catch (Exception ex)
			{
				LogFile.Ignored(ex);
				return false;
			}
		}

		/// <summary>The visitor leaves (its "kill" animation, as when a pet is closed).</summary>
		public static void GoAway()
		{
			leave?.Stop();
			var v = Visitor;
			if (v == null) return;
			try { v.Kill(); }
			catch (Exception skipped) { LogFile.Ignored(skipped); v.Close(); }
		}
	}
}
