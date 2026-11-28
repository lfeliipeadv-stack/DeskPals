using System.Drawing;
using System.Text.Json.Nodes;

namespace DeskPalsTools
{
	/// <summary>Tests of the watched folders: the list saved and read back (in the shape DeskPals reads), and its window.</summary>
	internal static class WatchTests
	{
		internal static void Register()
		{
			DevTests.Add("watch list", List);
			DevTests.Add("watch window", Window);
			DevTests.Add("watch texts", Texts);
			DevTests.Add("watch no history", NoHistory);
		}

		/// <summary>"--nohistory" (DeskPals' watched folders): a job done that way leaves the list of last jobs as it was.</summary>
		static async Task NoHistory(DevTests.Ctx t)
		{
			string keep = History.File;   // already the test's own file (DevTests redirects it)
			try
			{
				string pic = Path.Combine(t.Dir, "photo.png");
				using (var bmp = new Bitmap(120, 80)) { using (var g = Graphics.FromImage(bmp)) g.Clear(Color.Orange); bmp.Save(pic, System.Drawing.Imaging.ImageFormat.Png); }
				int before = History.Read().Count;
				WatchFolders.NoHistory(new[] { "--do=topdf", "--quiet" });
				t.Check("watch: without the flag the history file stays", History.File == keep);
				WatchFolders.NoHistory(new[] { "--do=topdf", "--quiet", "--nohistory", pic });
				var r = await Jobs.Run("topdf", new[] { pic }, null);
				t.Check("watch: the job is done with --nohistory", r.ok && r.outputs.Count == 1 && File.Exists(r.outputs[0]), r.message);
				History.File = keep;
				t.Check("watch: --nohistory adds nothing to the last jobs", History.Read().Count == before, History.Read().Count + " / " + before);
			}
			finally { History.File = keep; }
		}

		static Task List(DevTests.Ctx t)
		{
			string a = Directory.CreateDirectory(Path.Combine(t.Dir, "Scans")).FullName;
			string b = Directory.CreateDirectory(Path.Combine(t.Dir, "Photos")).FullName;
			string results = Directory.CreateDirectory(Path.Combine(a, WatchFolders.OutFolderName)).FullName;

			var choices = WatchFolders.JobChoices();
			t.Check("watch: the jobs without questions are offered", choices.Take(1).SequenceEqual(new[] { "topdf" }) && choices.Contains("ocr") && choices.Contains("smaller"), string.Join(",", choices));
			t.Check("watch: \"searchable\" only when it exists", choices.Contains("searchable") == (Jobs.Find("searchable") != null) && choices.Contains("compress") == (Jobs.Find("compress") != null));
			t.Check("watch: no job that asks questions", choices.All(id => Jobs.Find(id)?.Ask == null), string.Join(",", choices));

			// the other settings stay as they were
			ToolSettings.Set("explorerMenu", true);
			WatchFolders.Save(new[] { new WatchFolders.Entry(a, "topdf"), new WatchFolders.Entry(b, "smaller"), new WatchFolders.Entry(a + "\\", "ocr"), new WatchFolders.Entry("", "topdf") });
			var back = WatchFolders.Load();
			t.Check("watch: the list saved is read back", back.Count == 2 && back[0] == new WatchFolders.Entry(a, "topdf") && back[1] == new WatchFolders.Entry(b, "smaller"), string.Join(" | ", back));
			t.Check("watch: the other settings are kept", ToolSettings.Bool("explorerMenu"));
			// the shape DeskPals reads: "watch": [{ "folder": …, "job": … }]
			var raw = ToolSettings.Read()["watch"] as JsonArray;
			t.Check("watch: saved as [{folder, job}]", raw != null && raw.Count == 2 && (string)raw[0]["folder"] == a && (string)raw[0]["job"] == "topdf", raw?.ToJsonString() ?? "none");

			// a damaged file or an unknown job does not break anything
			var o = ToolSettings.Read();
			o["watch"] = JsonNode.Parse("[{\"folder\":\"" + b.Replace("\\", "\\\\") + "\",\"job\":\"nope\"}, 5, null, {\"job\":\"topdf\"}]");
			ToolSettings.Write(o);
			t.Check("watch: bad entries are left out", WatchFolders.Load().Count == 0);
			File.WriteAllText(ToolSettings.File, "{ not json");
			t.Check("watch: a damaged settings file gives an empty list", WatchFolders.Load().Count == 0);
			WatchFolders.Save(Array.Empty<WatchFolders.Entry>());
			t.Check("watch: an empty list is saved", (ToolSettings.Read()["watch"] as JsonArray)?.Count == 0);

			// folders that cannot be added
			var current = new List<WatchFolders.Entry> { new(a, "topdf") };
			t.Check("watch: a folder twice is refused", WatchFolders.Problem(a, current) != null);
			t.Check("watch: a results folder is refused", WatchFolders.Problem(results, new List<WatchFolders.Entry>()) != null);
			t.Check("watch: a folder that does not exist is refused", WatchFolders.Problem(Path.Combine(t.Dir, "nope"), current) != null);
			t.Check("watch: a whole drive is refused", WatchFolders.Problem(Path.GetPathRoot(t.Dir), current) != null);
			t.Check("watch: another folder is fine", WatchFolders.Problem(b, current) == null);
			ToolSettings.Set("watch", new JsonArray());
			return Task.CompletedTask;
		}

		/// <summary>The window (built, not shown, on a thread of its own as windows need): add, choose the job, remove, save.</summary>
		static Task Window(DevTests.Ctx t)
		{
			string a = Directory.CreateDirectory(Path.Combine(t.Dir, "Inbox")).FullName;
			string b = Directory.CreateDirectory(Path.Combine(t.Dir, "Camera")).FullName;
			Exception error = null;
			var thread = new Thread(() =>
			{
				try
				{
					WatchFolders.Save(new[] { new WatchFolders.Entry(a, "topdf") });
					using var f = new WatchDialog(WatchFolders.Load());
					t.Check("watch window: shows the saved folders", f.Entries().Count == 1 && f.Entries()[0].Folder == a);
					t.Check("watch window: the same folder is not added twice", f.TryAdd(a, "ocr") != null && f.Entries().Count == 1);
					t.Check("watch window: another folder is added", f.TryAdd(b, "topdf") == null && f.Entries().Count == 2);
					f.SetJob(1, "smaller");
					t.Check("watch window: its job can be chosen", f.Entries()[1].Job == "smaller", f.Entries()[1].Job);
					f.RemoveAt(0);
					t.Check("watch window: a folder is removed", f.Entries().Count == 1 && f.Entries()[0].Folder == b);
					WatchFolders.Save(f.Entries());
					var back = WatchFolders.Load();
					t.Check("watch window: what it saves is read back", back.Count == 1 && back[0] == new WatchFolders.Entry(b, "smaller"), string.Join(" | ", back));
				}
				catch (Exception ex) { error = ex; }
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			if (!thread.Join(TimeSpan.FromSeconds(30))) t.Check("watch window: built in time", false);
			if (error != null) t.Check("watch window: no exception", false, error.ToString());
			t.Check("watch window: has a button in the settings", Prefs.All().Any(p => p.Key == "watch" && p.Click != null && p.ButtonKey == "watch.button"));
			ToolSettings.Set("watch", new JsonArray());
			return Task.CompletedTask;
		}

		/// <summary>Every text of the watched folders in the 23 languages.</summary>
		static Task Texts(DevTests.Ctx t)
		{
			string[] codes = { "en", "pt-BR", "es", "de", "fr", "it", "ru", "tr", "id", "vi", "ja", "ko", "zh-CN", "ar", "fa", "ur", "hi", "bn", "mr", "ta", "te", "th", "sw" };
			var mine = T.Read("en").Keys.Where(k => k.StartsWith("watch.")).ToList();
			t.Check("watch texts: there are texts", mine.Count >= 10, mine.Count.ToString());
			foreach (var code in codes)
			{
				var d = T.Read(code);
				var missing = mine.Where(k => !d.ContainsKey(k) || string.IsNullOrWhiteSpace(d[k])).ToList();
				t.Check("watch texts: " + code, missing.Count == 0, string.Join(", ", missing));
			}
			return Task.CompletedTask;
		}
	}
}
