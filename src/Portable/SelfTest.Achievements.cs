using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// Achievements: counters win them at their goal (once), highest values never go down, a day counts once, a pet
		/// adopted again does not count twice, every one has its texts; the trophy comes down to the top of the screen,
		/// stays and goes away by itself; with the notice off it is a bubble; the page draws.
		/// </summary>
		static async Task AchievementsCheck()
		{
			string[] counters = { "toss", "chat", "adopt", "crowd", "focusDays", "tools.translate", "tools.compare", "tools.subtitles", "tools.askai", "tools.kinds", "tools.view", "tools.recipe", "tools.topdf" };
			foreach (var a in Achievements.All) Achievements.TestReset(a.Id);
			foreach (var c in counters) Achievements.TestSetCounter(c, 0);
			Program.MyData.SetSetting("AchAdopted", "");
			Program.MyData.SetSetting("AchD.focusDays", "");
			bool toastBefore = Achievements.ShowToast;
			Achievements.ShowToast = true;
			try
			{
				// texts
				var noText = Achievements.All.Where(a => a.Title.StartsWith("ach.") || a.Description.StartsWith("ach.")).Select(a => a.Id).ToList();
				Check("achievements: every one has a name and a description", noText.Count == 0, string.Join(", ", noText));
				Check("achievements: goals above one say the number", Achievements.All.Where(a => a.Goal / a.Scale > 1).All(a => a.Description.Contains((a.Goal / a.Scale).ToString())),
					string.Join(", ", Achievements.All.Where(a => a.Goal / a.Scale > 1 && !a.Description.Contains((a.Goal / a.Scale).ToString())).Select(a => a.Id)));

				// the tools: read from DeskPals Tools' history (what was done in the pet or in the tools' own window)
				string history = Path.Combine(Path.GetTempPath(), "deskpals-selftest-history-" + Guid.NewGuid().ToString("N") + ".json");
				try
				{
					// no trophy on screen for these (the toss below checks its own trophy and the focus)
					Achievements.ShowToast = false;
					File.WriteAllText(history, "[" + string.Join(",", new[] { "translate", "compare", "askai", "askai", "view", "subtitles", "recipe-0", "topdf" }.Select(j => "{\"job\":\"" + j + "\"}")) + "]");
					Achievements.ToolsHistory(history);
					Check("achievements: a translation in the tools' history wins Polyglot", Achievements.IsWon("toolTranslate") && Achievements.IsWon("toolCompare") && Achievements.IsWon("toolSubs"));
					Check("achievements: counted jobs are not won before their goal", !Achievements.IsWon("toolAsk") && Achievements.Progress(Achievements.Find("toolAsk")) == 2 && Achievements.Progress(Achievements.Find("toolKinds")) == 7,
						Achievements.Progress(Achievements.Find("toolAsk")) + " / " + Achievements.Progress(Achievements.Find("toolKinds")));
					// the trophies just shown are taken away again (the toss test below looks for its own)
					Achievements.TestClearQueue();
					}
					finally { try { File.Delete(history); } catch (Exception) { } Achievements.ShowToast = true; }

				// the first toss: won, and the trophy comes down
				Achievements.Count("toss");
				Check("achievements: first toss won", Achievements.IsWon("toss1"));
				Check("achievements: won today", Achievements.WonOn("toss1") == DateTime.Today);
				Check("achievements: 50 tosses not yet", !Achievements.IsWon("toss50") && Achievements.Progress(Achievements.Find("toss50")) == 1);
				await Task.Delay(800);
				var toast = Achievements.Showing;
				Check("achievements: the trophy is on screen", toast != null && toast.Visible);
				if (toast != null)
				{
					var area = Screen.FromControl(toast).WorkingArea;
					Check("achievements: at the top, in the middle", Math.Abs(toast.Top - (area.Top + 12)) <= 2 && Math.Abs(toast.Left + toast.Width / 2 - (area.Left + area.Width / 2)) <= 2, toast.Bounds + " in " + area);
					Check("achievements: does not take the focus", Form.ActiveForm != toast);
					if (resultFile.Length > 0)
						try
						{
							using (var bmp = new System.Drawing.Bitmap(toast.Width, toast.Height))
							{
								toast.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, toast.Width, toast.Height));
								bmp.Save(Path.Combine(Path.GetDirectoryName(resultFile), "achievement-toast.png"));
							}
						}
						catch (Exception skipped) { LogFile.Ignored(skipped); }
					await Task.Delay(5000);
					Check("achievements: still there after 5 s", Achievements.Showing == toast && toast.Top >= area.Top);
					for (int i = 0; i < 60 && Achievements.Showing != null; i++) await Task.Delay(100);
					Check("achievements: gone by itself after about 8 s", Achievements.Showing == null);
				}

				// a goal of many: once, at the goal
				Achievements.TestSetCounter("toss", 48);
				Achievements.Count("toss");
				Check("achievements: 49 of 50 is not won", !Achievements.IsWon("toss50"));
				Achievements.Count("toss");
				Check("achievements: 50 of 50 is won", Achievements.IsWon("toss50"));
				// two at once wait for each other (one notice at a time)
				await Task.Delay(300);
				Check("achievements: one notice at a time", Application.OpenForms.Cast<Form>().Count(f => f.GetType().Name == "Toast") <= 1);

				// highest value: never down
				Achievements.AtLeast("crowd", 2);
				Achievements.AtLeast("crowd", 1);
				Check("achievements: a highest value does not go down", Achievements.Value("crowd") == 2);
				Achievements.AtLeast("crowd", 3);
				Check("achievements: three pets at once", Achievements.IsWon("crowd3"));

				// once a day
				Achievements.CountDay("focusDays");
				Achievements.CountDay("focusDays");
				Check("achievements: a day counts once", Achievements.Value("focusDays") == 1);

				// adopting the same pet again does not count
				Achievements.Adopted("sheep");
				Achievements.Adopted("Sheep");
				Achievements.Adopted("pingus");
				Check("achievements: each pet adopted counts once", Achievements.Value("adopt") == 2 && Achievements.IsWon("adopt1"));

				// notice off: a bubble instead of the trophy
				Achievements.TestClearQueue();
				await Task.Delay(200);
				Achievements.ShowToast = false;
				Achievements.Unlock("chat1");
				await Task.Delay(300);
				Check("achievements: with the notice off, no trophy window", Achievements.Showing == null && Achievements.IsWon("chat1"));

				// the page
				FormSettings.ShowPage(FormSettings.Page.Achievements);
				await Task.Delay(500);
				var settings = Application.OpenForms.OfType<FormSettings>().FirstOrDefault();
				Check("achievements: the page opens", settings != null);
				if (settings != null && resultFile.Length > 0)
				{
					try
					{
						using (var bmp = new System.Drawing.Bitmap(settings.Width, settings.Height))
						{
							settings.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, settings.Width, settings.Height));
							bmp.Save(Path.Combine(Path.GetDirectoryName(resultFile), "achievements-page.png"));
						}
						settings.Size = new System.Drawing.Size(760, 520);
						await Task.Delay(300);
						using (var bmp = new System.Drawing.Bitmap(settings.Width, settings.Height))
						{
							settings.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, settings.Width, settings.Height));
							bmp.Save(Path.Combine(Path.GetDirectoryName(resultFile), "achievements-page-small.png"));
						}
					}
					catch (Exception skipped) { LogFile.Ignored(skipped); }
					settings.Close();
				}
			}
			finally
			{
				Achievements.ShowToast = toastBefore;
				foreach (var a in Achievements.All) Achievements.TestReset(a.Id);
				foreach (var c in counters) Achievements.TestSetCounter(c, 0);
				Program.MyData.SetSetting("AchAdopted", "");
				Program.MyData.SetSetting("AchD.focusDays", "");
			}
		}
	}
}
