namespace DeskPalsTools
{
	/// <summary>
	/// Developer ("--scenario file.pdf picture.png"): the program used as a person uses it, all on the main thread — the
	/// window open with the picture, "See and edit" opened on the PDF and closed, a PDF made from the picture, the
	/// window closed — so the end of the program can be watched (it must end by itself). DP_SC leaves parts out:
	/// "main" (a plain window instead), "viewer", "pdf".
	/// </summary>
	internal static class Scenario
	{
		public static int Run(List<string> files)
		{
			string pdf = files.FirstOrDefault(Jobs.IsPdf), pic = files.FirstOrDefault(Jobs.IsImage);
			if (pdf == null || pic == null) return 2;
			string skip = Environment.GetEnvironmentVariable("DP_SC") ?? "";
			Form main = skip.Contains("main") ? new Form { Text = "plain" } : new MainForm(new[] { pic });
			main.Shown += async (s, e) =>
			{
				await Task.Delay(1500);
				if (!skip.Contains("viewer"))
				{
					var viewer = new ViewerForm(new[] { pdf }) { SkipCloseQuestion = true };
					viewer.Show();
					for (int i = 0; i < 100 && !(viewer.ThumbsDone && viewer.BigDone); i++) await Task.Delay(100);
					viewer.Close();
					viewer.Dispose();
				}
				if (!skip.Contains("pdf"))
				{
					string made = Path.Combine(Path.GetTempPath(), "deskpals-scenario-" + Environment.ProcessId + ".pdf");
					await Task.Run(() => PdfWork.FromImages(new[] { pic }, made));
					try { File.Delete(made); } catch (Exception) { }
				}
				await Task.Delay(500);
				main.Close();
			};
			Application.Run(main);
			Ending.Now();
			Console.Error.WriteLine("scenario: the end of Main");
			return 0;
		}
	}
}
