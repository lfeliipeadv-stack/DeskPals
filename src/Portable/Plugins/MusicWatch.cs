using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== 9. music and games

	/// <summary>The pet dances when a new song starts in any player, and asks how the game went when a long one ends.</summary>
	public static class MusicWatch
	{
		static Timer timer;
		static string song = "";

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("MusicGames", false);
			set { Program.MyData.SetSetting("MusicGames", value); Apply(); }
		}

		/// <summary>Also say the name of each new song.</summary>
		public static bool SayTitle
		{
			get => Program.MyData.GetSettingBool("MusicTitle", false);
			set => Program.MyData.SetSetting("MusicTitle", value);
		}

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 3000 }; timer.Tick += (s, e) => Tick(); }
			if (Enabled) timer.Start(); else timer.Stop();
		}

		/// <summary>"Artist - Song" from Spotify's window, or "" when nothing plays (the title is just "Spotify").</summary>
		/// <summary>Spotify shows "Artist - Song" while playing and just "Spotify…" when paused.</summary>
		public static bool IsSong(string title) => !string.IsNullOrEmpty(title) && title.Contains(" - ") && !title.StartsWith("Spotify", StringComparison.OrdinalIgnoreCase);

		/// <summary>Self-test: a window title to use instead of Spotify's.</summary>
		internal static string FakeTitle;

		/// <summary>
		/// The song playing now: from the media controls of Windows (any player: Spotify, YouTube or Deezer in a
		/// browser, Apple Music, VLC…), else from Spotify's window title; "" when nothing plays.
		/// </summary>
		public static string Playing()
		{
			if (FakeTitle != null) return IsSong(FakeTitle) ? FakeTitle : "";
			if (media != null) return media.ToString();
			return SpotifyTitle();
		}

		/// <summary>The app playing the song (its id in Windows, e.g. "Spotify.exe", "chrome"), or "".</summary>
		public static string PlayingApp => media?.App ?? "";

		static string SpotifyTitle()
		{
			try
			{
				foreach (var p in Process.GetProcessesByName("Spotify"))
				{
					using (p)
					{
						string t = p.MainWindowTitle ?? "";
						if (IsSong(t)) return t;
					}
				}
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }
			return "";
		}

		// the media controls answer asynchronously: asked in the background, the last answer kept here
		static volatile MediaSession.Info media;
		static int asking;

		static void AskMedia()
		{
			if (System.Threading.Interlocked.Exchange(ref asking, 1) == 1) return;
			Task.Run(() =>
			{
				try { media = MediaSession.Current(); }
				finally { System.Threading.Interlocked.Exchange(ref asking, 0); }
			});
		}

		internal static void Tick()
		{
			if (SelfTest.Running && !SelfTest.Live) return;
			AskMedia();   // the answer is used on the next tick (3 s later)
			string now = Playing();
			if (now == song) return;
			song = now;
			if (now.Length == 0 || SpeechBubble.QuietNow) return;
			Program.Mainthread?.ReactAll("dance", "party", "happy", "cheer", "jump"); Achievements.Count("music");
			if (SayTitle) SpeechBubble.Say("🎵 " + now, 5);
		}
		public static void GameOver(TimeSpan played)
		{
			if (!Enabled) return;
			var t = new Timer { Interval = 2500 };
			t.Tick += (s, e) =>
			{
				t.Stop(); t.Dispose();
				Program.Mainthread?.ReactAll("cheer", "happy", "wave");
				SpeechBubble.Say(Lang.T("game.over", (int)played.TotalMinutes), 10);
			};
			t.Start();
		}
	}
}
