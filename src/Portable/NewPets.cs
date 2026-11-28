using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopPet
{
	/// <summary>
	/// Pets that arrived in the gallery (or in the sources added) since the user last looked: the pet tells it once
	/// ("3 new pets arrived in the gallery: …"), with a button that opens the gallery showing only the new ones. Uses the
	/// list <see cref="PetUpdates"/> already fetches every 6 hours, so it costs nothing more. A pet counts as seen as in
	/// the gallery's "New" badge (SeenPets); a pet already told is not told again ("NewPetsTold").
	/// </summary>
	public static class NewPets
	{
		/// <summary>Tell when new pets arrive (on by default).</summary>
		public static bool Notify
		{
			get => Program.MyData.GetSettingBool("NewPetsNotice", true);
			set => Program.MyData.SetSetting("NewPetsNotice", value);
		}

		/// <summary>The gallery list just came (every source): find the pets never seen, never told, and tell them.</summary>
		/// <returns>The names told (empty when nothing was said), for the self-test.</returns>
		public static List<string> Check(IList<OptionsPet> gallery)
		{
			var told = new List<string>();
			try
			{
				if (gallery == null || gallery.Count == 0) return told;
				var seen = Program.MyData.GetSeenPets();
				if (seen == null)
				{
					// first time: everything there now is "already seen" (the gallery does the same when it opens)
					seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					foreach (var p in gallery) if (!string.IsNullOrEmpty(p.folder)) seen[p.folder] = p.Version;
					Program.MyData.SetSeenPets(seen);
					return told;
				}
				var already = new HashSet<string>(Program.MyData.GetSetting("NewPetsTold").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
				var fresh = gallery.Where(p => !string.IsNullOrEmpty(p.folder) && !seen.ContainsKey(p.folder) && !already.Contains(p.folder)).ToList();
				if (fresh.Count == 0) return told;
				foreach (var p in fresh) already.Add(p.folder);
				Program.MyData.SetSetting("NewPetsTold", string.Join("|", already));
				// the colours of one pet (gSheep blue, green…) are one pet
				var names = fresh.Select(p => FormSettings.GroupOf(p.folder) ?? (string.IsNullOrWhiteSpace(p.name) ? p.folder : p.name.Trim()))
					.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
				told.AddRange(names);
				if (!Notify) return told;
				string list = string.Join(", ", names.Take(3)) + (names.Count > 3 ? "…" : "");
				string text = names.Count == 1 ? Lang.T("newPets.one", list) : Lang.T("newPets.many", names.Count, list);
				Program.Mainthread?.ReactAll("surprised", "happy", "wave", "jump");
				SpeechBubble.Notify(text, 0,
					new SpeechBubble.Choice(Lang.T("newPets.see"), FormSettings.ShowNewPets, true),
					new SpeechBubble.Choice(Lang.T("newPets.later"), null));
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			return told;
		}
	}
}
