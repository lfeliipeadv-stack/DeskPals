using System;
using System.Linq;

namespace DesktopPet
{
	// ====================================================================== the pet grows with your focus

	/// <summary>
	/// Off unless turned on. What you get done is worth points: a Pomodoro focus finished 3, a task of the notes checked 1,
	/// a reminder answered "Done" 1, a task an AI agent finished 1. The points make levels (each level needs a little more),
	/// and some levels give a hat the pets can wear (<see cref="PetHats"/>): the party hat at level 2, then a cap, a flower
	/// crown, a top hat, a wizard's hat and a crown at level 10. Nothing is ever taken away.
	/// </summary>
	public static class PetLevel
	{
		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("PetLevel", false);
			set { Program.MyData.SetSetting("PetLevel", value); PetHats.Apply(); }
		}

		public static int Points => Math.Max(0, Program.MyData.GetSettingInt("PetPoints", 0));

		/// <summary>Level 1 at the start, 2 at 3 points, 3 at 12, 4 at 27, 5 at 48… (3 × (level − 1)² points).</summary>
		public static int LevelFor(int points) => 1 + (int)Math.Floor(Math.Sqrt(Math.Max(0, points) / 3.0) + 1e-9);

		public static int PointsFor(int level) => 3 * (level - 1) * (level - 1);

		public static int Level => LevelFor(Points);

		/// <summary>Points worth of each thing done.</summary>
		public const int FocusPoints = 3, TaskPoints = 1, ReminderPoints = 1, AgentPoints = 1;

		/// <summary>The hat the user chose to wear ("" = none); only a hat already won counts.</summary>
		public static string Hat
		{
			get
			{
				string id = Program.MyData.GetSetting("PetHat");
				var hat = PetHats.Find(id);
				return hat != null && hat.Level > 0 && hat.Level <= Level ? id : "";
			}
			set { Program.MyData.SetSetting("PetHat", value ?? ""); PetHats.Apply(); }
		}

		/// <summary>Something was done: add its points; a new level is celebrated, and a hat won is put on.</summary>
		public static void Add(int points)
		{
			if (!Enabled || points <= 0) return;
			int before = Level;
			Program.MyData.SetSetting("PetPoints", Points + points);
			int now = Level;
			if (now <= before) return;
			var hat = PetHats.All.Where(h => h.Level > before && h.Level <= now).OrderByDescending(h => h.Level).FirstOrDefault();
			if (hat != null) Hat = hat.Id;   // the new hat goes on at once (it can be changed in the plugin's options)
			Program.Mainthread?.ReactAll("cheer", "party", "happy", "jump", "dance");
			ScreenEffects.Start(ScreenEffects.Kind.Confetti, 8, true);
			SpeechBubble.Notify(hat != null ? Lang.T("level.upHat", now, Lang.T("hat." + hat.Id)) : Lang.T("level.up", now), 10);
			CommunityPlugins.Event("levelUp");
		}
	}
}
