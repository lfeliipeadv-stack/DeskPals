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
		/// The plugins of 1.1: community plugins (format, gallery, rules at work), the pet's level and its hats, special
		/// dates, the effects on the screen, the notes the pet brings, the live chat (reading Twitch's messages) and the
		/// programs the pets stay away from. Every setting touched is put back.
		/// </summary>
		static async Task Plugins11()
		{
			var data = Program.MyData;
			string[] keys = { "PetLevel", "PetPoints", "PetHat", "Seasons", "SeasonHats", "Community", "CommunityOff", "IgnoreAppsOn", "IgnoreApps", "LiveChat", "PetNotes", "ScreenFx" };
			var saved = keys.ToDictionary(k => k, k => data.GetSetting(k));
			try
			{
				// ---- the pet's level
				Check("level: 1 at the start, 2 at 3 points, 3 at 12, 10 at 243", PetLevel.LevelFor(0) == 1 && PetLevel.LevelFor(2) == 1 && PetLevel.LevelFor(3) == 2 && PetLevel.LevelFor(12) == 3 && PetLevel.LevelFor(243) == 10);
				Check("level: points of a level and back", Enumerable.Range(1, 15).All(l => PetLevel.LevelFor(PetLevel.PointsFor(l)) == l && PetLevel.LevelFor(PetLevel.PointsFor(l) - 1) == Math.Max(1, l - 1)));
				data.SetSetting("PetLevel", true); data.SetSetting("PetPoints", 0); data.SetSetting("PetHat", "");
				PetLevel.Add(PetLevel.FocusPoints);
				Check("level: a focus finished reaches level 2 and puts on the party hat", PetLevel.Level == 2 && PetLevel.Hat == "party", $"level {PetLevel.Level}, hat {PetLevel.Hat}");
				data.SetSetting("PetHat", "crown");
				Check("level: a hat not won yet is not worn", PetLevel.Hat == "", PetLevel.Hat);
				data.SetSetting("PetLevel", false);
				int before = PetLevel.Points; PetLevel.Add(5);
				Check("level: off, nothing counts", PetLevel.Points == before);
				ScreenEffects.Stop(true);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// ---- the hats
				Check("hats: every hat has rows and its colors", PetHats.All.All(h => h.Height > 0 && h.Width > 0 && h.Rows.All(r => r.All(c => c == '.' || h.Colors.ContainsKey(c)))),
					string.Join(", ", PetHats.All.Where(h => !h.Rows.All(r => r.All(c => c == '.' || h.Colors.ContainsKey(c)))).Select(h => h.Id)));
				Check("hats: one for every level prize and every date", new[] { "party", "cap", "flowers", "tophat", "wizard", "crown", "santa", "witch", "straw", "bunny", "carnival" }.All(id => PetHats.Find(id) != null));
				using (var bmp = new Bitmap(40, 40))
				{
					using (var g = Graphics.FromImage(bmp)) { g.Clear(Color.Magenta); g.FillEllipse(Brushes.Orange, 8, 10, 24, 28); }
					var head = PetHats.HeadOf(bmp);
					Check("hats: the head is found in a picture (its middle and its top)", head.Found && Math.Abs(head.X - 0.5) < 0.08 && Math.Abs(head.Top - 10 / 40.0) < 0.05, $"x {head.X:0.00} top {head.Top:0.00}");
				}
				var pet = Pet();
				if (pet != null)
				{
					PetHats.TestHat = "santa";
					PetHats.Apply();
					await Task.Delay(400);
					var hat = PetHats.Shown.FirstOrDefault(w => w.Visible);
					Check("hats: the hat is on the pet's head", hat != null && hat.HatId == "santa" && hat.Bottom >= pet.Top - 4 && hat.Bottom <= pet.Bottom && hat.Left < pet.Right && hat.Right > pet.Left,
						hat == null ? "no hat window" : $"hat {hat.Bounds}, pet {pet.Bounds}");
					Shot("hat", Rectangle.Union(pet.Bounds, hat?.Bounds ?? pet.Bounds));
					PetHats.TestHat = null;
					PetHats.Apply();
					await Task.Delay(100);
					Check("hats: taken off when none is worn", !PetHats.Shown.Any(w => w.Visible) || PetHats.Current != null);
				}
				else lines.Add("SKIP  hats: no pet on the screen");

				// ---- special dates
				Check("dates: Easter 2026 is April 5, 2027 is March 28", Seasons.Easter(2026) == new DateTime(2026, 4, 5) && Seasons.Easter(2027) == new DateTime(2027, 3, 28));
				Check("dates: Christmas, New Year, Halloween, June, Carnival",
					Seasons.HatDay(new DateTime(2026, 12, 20)) == Seasons.Day.Christmas && Seasons.HatDay(new DateTime(2026, 12, 31)) == Seasons.Day.NewYear
					&& Seasons.HatDay(new DateTime(2026, 10, 31)) == Seasons.Day.Halloween && Seasons.HatDay(new DateTime(2026, 6, 10)) == Seasons.Day.Junina
					&& Seasons.HatDay(new DateTime(2026, 2, 17)) == Seasons.Day.Carnival && Seasons.HatDay(new DateTime(2026, 3, 10)) == Seasons.Day.None);
				Check("dates: the day itself is celebrated, not the days around it",
					Seasons.Celebration(new DateTime(2026, 12, 25)) == Seasons.Day.Christmas && Seasons.Celebration(new DateTime(2026, 12, 20)) == Seasons.Day.None
					&& Seasons.Celebration(new DateTime(2026, 6, 24)) == Seasons.Day.Junina && Seasons.Celebration(new DateTime(2026, 4, 5)) == Seasons.Day.Easter);
				Check("dates: the birthday is read in any common way", Seasons.ParseBirthday("05/03") == (5, 3) && Seasons.ParseBirthday("5-3") == (5, 3) && Seasons.ParseBirthday("31/02") == null && Seasons.ParseBirthday("hello") == null);
				Check("dates: each date has its hat", Enum.GetValues<Seasons.Day>().Where(d => d != Seasons.Day.None).All(d => PetHats.Find(Seasons.HatOf(d)) != null));

				// ---- effects on the screen
				ScreenEffects.Start(ScreenEffects.Kind.Snow, 3, true);
				await Task.Delay(1200);
				var fx = Application.OpenForms.OfType<ScreenEffects.Overlay>().ToList();
				if (FullScreenWatch.Active) lines.Add("SKIP  effects: a full-screen program is open");
				else
				{
					Check("effects: one window over each screen", fx.Count == Screen.AllScreens.Length && fx.All(o => Screen.AllScreens.Any(s => s.Bounds == o.Bounds)), $"{fx.Count} windows, {Screen.AllScreens.Length} screens");
					Check("effects: snow is falling", fx.Any(o => o.Falling > 20), string.Join(", ", fx.Select(o => o.Falling)));
					Shot("snow", Screen.PrimaryScreen.Bounds);
					ScreenEffects.Start(ScreenEffects.Kind.Rain, 3, true);
					await Task.Delay(2500);
					Check("effects: another show replaces the first one", Application.OpenForms.OfType<ScreenEffects.Overlay>().Count() == Screen.AllScreens.Length && ScreenEffects.Showing == ScreenEffects.Kind.Rain);
					await Task.Delay(4500);
					Check("effects: when the time is up it closes by itself", !ScreenEffects.Running, Application.OpenForms.OfType<ScreenEffects.Overlay>().Count() + " still open");
				}
				ScreenEffects.Stop(true);

				// ---- notes the pet brings
				var note = NoteDelivery.Bring("🧪", "Teste", "Um bilhete de teste");
				await Task.Delay(1600);
				pet = Pet();
				Check("notes: the note is on the screen, put down beside the pet", note.Visible && (pet == null || Math.Abs(note.Bottom - pet.Bottom) <= Math.Max(40, note.Height) && (Math.Abs(note.Right - pet.Left) < 40 || Math.Abs(note.Left - pet.Right) < 40)),
					$"note {note.Bounds}, pet {pet?.Bounds}");
				Shot("note", Rectangle.Union(note.Bounds, pet?.Bounds ?? note.Bounds));
				for (int i = 0; i < 6; i++) NoteDelivery.Bring("🧪", "Teste " + i, "…");
				Check("notes: never more than 5 on the screen", NoteDelivery.Open.Count <= 5, NoteDelivery.Open.Count.ToString());
				foreach (var n in NoteDelivery.Open.ToList()) n.Close();
				Check("notes: closed", NoteDelivery.Open.Count == 0);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// ---- live chat
				var c1 = LiveChat.Parse("@badge-info=;display-name=Fulana_99;user-id=1 :fulana_99!fulana_99@fulana_99.tmi.twitch.tv PRIVMSG #canal :!dançar agora");
				Check("live: a command from the chat", c1?.Kind == "command" && c1.Command == "dance" && c1.User == "Fulana_99", c1?.Kind + " " + c1?.Command + " " + c1?.User);
				Check("live: commands in English and Spanish", LiveChat.Parse(":a!a@a.tmi.twitch.tv PRIVMSG #c :!jump")?.Command == "jump" && LiveChat.Parse(":a!a@a.tmi.twitch.tv PRIVMSG #c :!bailar")?.Command == "dance");
				Check("live: ordinary messages are left alone", LiveChat.Parse(":a!a@a.tmi.twitch.tv PRIVMSG #c :hello everyone") == null && LiveChat.Parse(":a!a@a.tmi.twitch.tv PRIVMSG #c :!unknown") == null);
				var sub = LiveChat.Parse("@display-name=Bob;msg-id=resub;login=bob :tmi.twitch.tv USERNOTICE #canal :great stream");
				var raid = LiveChat.Parse("@display-name=Ana;msg-id=raid;msg-param-displayName=Ana;msg-param-viewerCount=42;login=ana :tmi.twitch.tv USERNOTICE #canal");
				var bits = LiveChat.Parse("@bits=150;display-name=Rui :rui!rui@rui.tmi.twitch.tv PRIVMSG #canal :Cheer150 boa");
				Check("live: subscription, raid and bits", sub?.Kind == "sub" && sub.User == "Bob" && raid?.Kind == "raid" && raid.Amount == 42 && bits?.Kind == "bits" && bits.Amount == 150,
					$"{sub?.Kind} {raid?.Kind}/{raid?.Amount} {bits?.Kind}/{bits?.Amount}");
				var odd = LiveChat.Parse("@display-name=<b>x</b>\\s{y} :x!x@x.tmi.twitch.tv PRIVMSG #c :!hug");
				Check("live: the viewer's name is made safe (letters, digits, _)", odd?.User == "bxby", odd?.User);
				Check("live: the channel from a link or a name", LiveChat.CleanChannel("https://www.twitch.tv/SomeOne") == "someone" && LiveChat.CleanChannel("#abc_1") == "abc_1" && LiveChat.CleanChannel("a b") == "");
				data.SetSetting("LiveChat", true);
				LiveChat.LastDone = null;
				LiveChat.Handle(new LiveChat.Message { Kind = "command", User = "selftest" + DateTime.Now.Ticks % 1000, Command = "wave" });
				Check("live: the pets do what the chat asks", LiveChat.LastDone == "wave", LiveChat.LastDone);
				LiveChat.LastDone = null;
				LiveChat.Handle(new LiveChat.Message { Kind = "command", User = "other", Command = "jump" });
				Check("live: one command every few seconds", LiveChat.LastDone == null, LiveChat.LastDone);
				data.SetSetting("LiveChat", false);
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// ---- programs the pets stay away from
				Check("apps: names cleaned", WindowRules.Clean("Code.EXE") == "code" && WindowRules.Clean("  Spotify ") == "spotify");
				data.SetSetting("IgnoreApps", ""); data.SetSetting("IgnoreAppsOn", true);
				pet = Pet();
				if (pet != null)
				{
					Check("apps: an empty list ignores nothing", !WindowRules.Ignored(pet.Handle));
					WindowRules.Apps = new List<string> { Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "desktoppet") };
					Check("apps: a window of a program in the list is ignored", WindowRules.Ignored(pet.Handle), DesktopWindows.ProgramOf(pet.Handle) + " vs " + string.Join(",", WindowRules.Apps));
					data.SetSetting("IgnoreAppsOn", false);
					Check("apps: off, nothing is ignored", !WindowRules.Ignored(pet.Handle));
				}
				Check("apps: the open programs can be listed", WindowRules.OpenPrograms() != null);

				// ---- community plugins
				Check("community: a plugin is read", CommunityPlugins.Parse("{\"id\":\"x-1\",\"name\":\"X\",\"rules\":[{\"when\":{\"every\":30},\"say\":\"oi\"}]}").Rules[0].Every == 30);
				var bad = new[]
				{
					"{\"id\":\"X Y\",\"name\":\"X\",\"rules\":[{\"when\":\"start\",\"say\":\"a\"}]}",          // id
					"{\"id\":\"x\",\"name\":\"X\",\"rules\":[{\"when\":{\"every\":1},\"say\":\"a\"}]}",          // too often
					"{\"id\":\"xy\",\"name\":\"X\",\"rules\":[{\"when\":{\"at\":\"25:00\"},\"say\":\"a\"}]}",    // time
					"{\"id\":\"xy\",\"name\":\"X\",\"rules\":[{\"when\":\"start\"}]}",                           // does nothing
					"{\"id\":\"xy\",\"name\":\"X\",\"rules\":[{\"when\":{\"event\":\"hack\"},\"say\":\"a\"}]}", // unknown event
					"{\"id\":\"xy\",\"name\":\"X\",\"rules\":[{\"when\":\"start\",\"effect\":\"fire\"}]}",       // unknown effect
				};
				int refused = bad.Count(j => { try { CommunityPlugins.Parse(j); return false; } catch (Exception) { return true; } });
				Check("community: broken plugins are refused", refused == bad.Length, $"{refused} of {bad.Length}");
				var multi = CommunityPlugins.Parse("{\"id\":\"xy\",\"name\":{\"en\":\"Hi\",\"pt\":\"Oi\",\"es\":\"Hola\"},\"rules\":[{\"when\":\"start\",\"say\":{\"en\":[\"a\",\"b\"],\"pt\":[\"c\"]}}]}");
				string expectedName = Lang.Current.StartsWith("pt") ? "Oi" : Lang.Current.StartsWith("es") ? "Hola" : "Hi";
				Check("community: texts in the user's language", multi.Name == expectedName, multi.Name + " in " + Lang.Current);
				string repo = AppContext.BaseDirectory;
				while (repo != null && !Directory.Exists(Path.Combine(repo, "PluginGallery"))) repo = Path.GetDirectoryName(repo);
				if (repo != null)
				{
					CommunityPlugins.GalleryDir = Path.Combine(repo, "PluginGallery");
					try
					{
						var gallery = await CommunityPlugins.Gallery();
						var broken = new List<string>();
						foreach (var e in gallery) { try { var p = CommunityPlugins.Parse(File.ReadAllText(Path.Combine(CommunityPlugins.GalleryDir, e.File))); if (p.Id != e.Id) broken.Add(e.Id + " (id)"); } catch (Exception ex) { broken.Add(e.Id + ": " + ex.Message); } }
						Check("community: every plugin of the gallery is valid", gallery.Count >= 5 && broken.Count == 0, $"{gallery.Count} plugins; " + string.Join("; ", broken));
					}
					finally { CommunityPlugins.GalleryDir = null; }
				}
				else lines.Add("SKIP  community: the gallery folder is not here");
				// a plugin installed, at work on an event, removed
				string testJson = "{\"id\":\"selftest-plugin\",\"name\":\"Self-test\",\"rules\":[{\"when\":{\"event\":\"focusDone\"},\"play\":[\"happy\"],\"say\":\"Plugin ok\"}]}";
				data.SetSetting("Community", true);
				CommunityPlugins.Install(testJson);
				Check("community: installed in the user's folder", CommunityPlugins.Installed.Any(p => p.Id == "selftest-plugin"));
				// the user's own plugins wait (one of them could answer the same event first)
				data.SetSetting("CommunityOff", string.Join("|", CommunityPlugins.Installed.Where(p => p.Id != "selftest-plugin").Select(p => p.Id)));
				CommunityPlugins.LastDone = null;
				CommunityPlugins.Event("focusDone");
				Check("community: a rule runs on its event", CommunityPlugins.LastDone == "selftest-plugin:play,say", CommunityPlugins.LastDone);
				CommunityPlugins.SetOn("selftest-plugin", false);
				CommunityPlugins.LastDone = null;
				CommunityPlugins.Event("focusDone");
				Check("community: a plugin turned off does nothing", CommunityPlugins.LastDone == null || !CommunityPlugins.LastDone.StartsWith("selftest-plugin"), CommunityPlugins.LastDone);
				CommunityPlugins.Remove("selftest-plugin");
				Check("community: removed", !CommunityPlugins.Installed.Any(p => p.Id == "selftest-plugin"));
				Check("local api: animation names are kept plain", AgentBridge.PlayNames("dance, happy\nwave;rm -rf").SequenceEqual(new[] { "dance", "happy" }), string.Join("|", AgentBridge.PlayNames("dance, happy\nwave;rm -rf")));
				foreach (var b in Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();

				// ---- the windows of the settings open
				using (var f = new FormCommunityPlugins()) { f.TopMost = true; f.Show(); await Task.Delay(300); Check("community: its window opens", f.Visible); Shot("community", f.Bounds); }
				using (var f = new FormIgnoredApps()) { f.TopMost = true; f.Show(); await Task.Delay(200); Check("apps: its window opens", f.Visible); Shot("apps", f.Bounds); }
			}
			finally
			{
				foreach (var kv in saved) data.SetSetting(kv.Key, kv.Value);
				PetHats.TestHat = null;
				PetHats.Apply();
				ScreenEffects.Stop(true);
				LiveChat.Apply();
				CommunityPlugins.Reload();
				foreach (var n in NoteDelivery.Open.ToList()) n.Close();
			}
		}
	}
}
