using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// DeskPals Tools from DeskPals: a pet file still changes the pet, anything else goes to the tools; the jobs come from
		/// the extension; a job really runs (a picture becomes a PDF); a file dropped on a pet makes it offer the jobs.
		/// </summary>
		static async Task ToolsCheck()
		{
			string dir = Path.Combine(Path.GetTempPath(), "deskpals-tools-from-pet-" + Environment.ProcessId);
			Directory.CreateDirectory(dir);
			try
			{
				string pet = Path.Combine(dir, "animations.xml");
				File.WriteAllText(pet, BuiltInPets.GetXml(BuiltInPets.Folders.First()));
				string other = Path.Combine(dir, "notes.xml");
				File.WriteAllText(other, "<notes><a/></notes>");
				Check("tools: a pet file is still a pet", ToolsExtension.IsPetFile(pet));
				Check("tools: another XML is not a pet", !ToolsExtension.IsPetFile(other));
				if (!ToolsExtension.IsInstalled) { lines.Add("SKIP  tools: the extension is not here (DESKPALS_TOOLS_EXE)"); return; }
				string png = Path.Combine(dir, "picture.png");
				using (var bmp = new System.Drawing.Bitmap(300, 200)) { using (var g = System.Drawing.Graphics.FromImage(bmp)) g.Clear(System.Drawing.Color.SkyBlue); bmp.Save(png); }
				var jobs = await ToolsExtension.ListAsync(new[] { png });
				Check("tools: the jobs come from the extension", jobs.Any(j => j.Id == "topdf") && jobs.Any(j => j.Id == "ocr"), string.Join(",", jobs.Select(j => j.Id)));
				Check("tools: the jobs come in the language of DeskPals", jobs.All(j => !string.IsNullOrWhiteSpace(j.Text) && !j.Text.StartsWith("job.")), string.Join(" | ", jobs.Select(j => j.Text)));
				var r = await ToolsExtension.RunAsync("topdf", new[] { png });
				Check("tools: a job runs (picture → PDF)", r.Ok && r.Outputs.Count == 1 && File.Exists(r.Outputs[0]) && r.Outputs[0].EndsWith(".pdf"), r.Message);
				SpeechBubble.TestClearHeld();
				ToolsExtension.Offer(new[] { png });
				for (int i = 0; i < 50 && !SpeechBubble.Talking; i++) await Task.Delay(100);
				Check("tools: a file dropped on the pet → it offers the jobs", SpeechBubble.Talking);
				foreach (var b in System.Windows.Forms.Application.OpenForms.OfType<SpeechBubble>().ToList()) b.Close();
			}
			finally { try { Directory.Delete(dir, true); } catch (Exception skipped) { LogFile.Ignored(skipped); } }
		}
	}
}
