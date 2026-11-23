using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// Developer: every sound of every pet of a Pets folder (set PETS_DIR) opens in Windows (MCI), and a sound that
		/// cannot open leaves the volume alone. Version 1.0.0 set the volume to 0 for good when one sound failed (the
		/// 0.1 s clicks of Pingus and of the Office assistants), so no pet made a sound anymore.
		/// </summary>
		static void SoundsAllPets()
		{
			float before = Program.MyData.GetVolume(true);
			var broken = new TSound();
			string error = broken.Load(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
			Check("sounds: a sound that cannot open does not mute the pets", error != null && Program.MyData.GetVolume(true) == before,
				$"error: {error ?? "none"}, volume {before} → {Program.MyData.GetVolume(true)}");

			string dir = Environment.GetEnvironmentVariable("PETS_DIR");
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) { lines.Add("SKIP  sounds all pets: PETS_DIR not set"); return; }
			// a computer without any sound device (the GitHub build machine) opens no sound at all: nothing to check there
			var probe = Directory.GetDirectories(dir).Select(d => Path.Combine(d, "animations.xml")).Where(File.Exists)
				.Select(f => Regex.Match(File.ReadAllText(f), @"<sound animationid=""\d+""[\s\S]*?<base64>\s*(?:<!\[CDATA\[)?\s*([^<\]]{2000,})"))
				.FirstOrDefault(m => m.Success);
			if (probe != null)
			{
				string b = probe.Groups[1].Value.Trim();
				int at = b.IndexOf(";base64,");
				if (at > 0) b = b.Substring(at + 8);
				string err = new TSound().Load(Convert.FromBase64String(b));
				if (err != null) { lines.Add("SKIP  sounds all pets: this computer opens no sound (" + err + ")"); return; }
			}
			int total = 0;
			foreach (var folder in Directory.GetDirectories(dir).OrderBy(d => d))
			{
				string file = Path.Combine(folder, "animations.xml");
				if (!File.Exists(file)) continue;
				var failed = new System.Collections.Generic.List<string>();
				int n = 0;
				foreach (Match m in Regex.Matches(File.ReadAllText(file), @"<sound animationid=""(\d+)""[\s\S]*?<base64>\s*(?:<!\[CDATA\[)?\s*([^<\]]+)"))
				{
					n++;
					string b64 = m.Groups[2].Value.Trim();
					int i = b64.IndexOf(";base64,");
					if (i > 0) b64 = b64.Substring(i + 8);
					var sound = new TSound();
					string e = sound.Load(Convert.FromBase64String(b64));
					if (e != null) failed.Add("anim " + m.Groups[1].Value + ": " + e);
				}
				if (n == 0) continue;
				total += n;
				Check($"sounds all pets: {Path.GetFileName(folder)} opens its {n} sounds", failed.Count == 0, string.Join("; ", failed));
			}
			lines.Add($"INFO  sounds all pets: {total} sounds");
		}
	}
}
