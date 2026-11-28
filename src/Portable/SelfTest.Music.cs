using System;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// The media controls of Windows: asked in the background they answer quickly and without an error. With
		/// DESKPALS_MEDIA_EXPECT set (a developer playing a file), the song read must contain that text.
		/// </summary>
		static async Task MediaControls()
		{
			var started = DateTime.Now;
			MediaSession.Info info = null;
			Exception error = null;
			var task = Task.Run(() => { try { info = MediaSession.Current(); } catch (Exception ex) { error = ex; } });
			bool answered = await Task.WhenAny(task, Task.Delay(5000)) == task;
			Check("music: the media controls answer in time", answered, "no answer in 5 s");
			Check("music: the media controls do not fail", error == null, error?.Message ?? "");
			lines.Add($"INFO  music: playing now: {(info == null ? "nothing" : info + " (" + info.App + ")")} in {(DateTime.Now - started).TotalMilliseconds:0} ms");
			lines.Add("INFO  music: sessions: " + await Task.Run(MediaSession.Describe));
			string expect = Environment.GetEnvironmentVariable("DESKPALS_MEDIA_EXPECT");
			if (!string.IsNullOrEmpty(expect))
				Check("music: the song playing is read", info != null && info.ToString().IndexOf(expect, StringComparison.OrdinalIgnoreCase) >= 0, info?.ToString() ?? "nothing");
			// no video and no fake: Playing() falls back to Spotify's title without errors
			MusicWatch.FakeTitle = null;
			Check("music: Playing() answers", MusicWatch.Playing() != null);
		}
	}
}
