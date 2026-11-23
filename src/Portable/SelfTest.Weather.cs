using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// DeskPals Weather seen from DeskPals: what the pets do with each weather, its state file read (and an old one
		/// ignored).
		/// </summary>
		static Task WeatherExtensionTests()
		{
			Check("weather: a reaction for each weather", new[] { "rain", "snow", "leaves", "blossom" }.All(k => WeatherExtension.Reaction(k, 0, 0)?.Length > 0)
				&& WeatherExtension.Reaction(null, 0.7, 0)?.Contains("look around") == true && WeatherExtension.Reaction(null, 0, 0.7)?.Contains("sunbathing") == true
				&& WeatherExtension.Reaction(null, 0, 0) == null);
			string folder = Path.Combine(InstallPlace.DataDir, "extensions");
			Directory.CreateDirectory(folder);
			string state = Path.Combine(folder, "weather-state.json");
			string keepState = File.Exists(state) ? File.ReadAllText(state) : null;
			try
			{
				File.WriteAllText(state, JsonSerializer.Serialize(new { on = true, kind = "snow", fog = 0.5, sun = 0.0, wind = 0.3 }));
				var s = WeatherExtension.State();
				Check("weather: the state of the extension is read", s.on && s.kind == "snow" && Math.Abs(s.fog - 0.5) < 1e-9, $"{s.on} {s.kind} {s.fog}");
				File.SetLastWriteTime(state, DateTime.Now.AddMinutes(-5));
				Check("weather: an old state (the extension closed) is ignored", !WeatherExtension.State().on);
				Check("weather: a pet line for each weather", new[] { "snow", "rain", "leaves", "blossom", "fog", "sun" }.All(k => Lang.T("weather.say." + k) != "weather.say." + k));

				// the tray submenu, as while the extension runs (its signal exists and it wrote its scenes)
				if (WeatherExtension.IsInstalled)
				{
					string scenes = Path.Combine(folder, "weather-scenes.json"), command = Path.Combine(folder, "weather-command.json");
					bool wasOn = WeatherExtension.Enabled;
					string keepScenes = File.Exists(scenes) ? File.ReadAllText(scenes) : null;
					using var running = WeatherExtension.Running ? null : new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, @"Local\DeskPalsWeatherShow");
					try
					{
						File.WriteAllText(scenes, "{\"on\":true,\"current\":\"b:scene.rainy\",\"scenes\":[{\"id\":\"b:scene.winter\",\"name\":\"Inverno\"},{\"id\":\"b:scene.rainy\",\"name\":\"Dia chuvoso\"}],\"labels\":{\"on\":\"Clima ligado\",\"test\":\"Testar\",\"settings\":\"Configurações…\",\"scenes\":\"Cenas\"}}");
						WeatherExtension.TestQuiet = true;
						Program.MyData.SetSetting("ExtWeather", true);
						File.WriteAllText(state, JsonSerializer.Serialize(new { on = true, kind = "snow", fog = 0.0, sun = 0.0, wind = 0.3 }));   // snow falls: "clear the snow" shows
						var item = new System.Windows.Forms.ToolStripMenuItem();
						WeatherExtension.FillMenu(item);
						var names = item.DropDownItems.OfType<System.Windows.Forms.ToolStripMenuItem>().Select(i => i.Text).ToList();
						var rainy = item.DropDownItems.OfType<System.Windows.Forms.ToolStripMenuItem>().FirstOrDefault(i => i.Text == "Dia chuvoso");
						Check("weather menu: shows the scenes of the extension, the one on checked", item.Available && names.Contains("Inverno") && rainy?.Checked == true && names.Contains("Testar") && names.Contains(WeatherExtension.ClearText("snow")), string.Join(", ", names));
						item.DropDownItems.OfType<System.Windows.Forms.ToolStripMenuItem>().First(i => i.Text == "Inverno").PerformClick();
						Check("weather menu: a scene clicked is asked of the extension", File.Exists(command) && File.ReadAllText(command).Contains("b:scene.winter"));
						item.DropDownItems.OfType<System.Windows.Forms.ToolStripMenuItem>().First(i => i.Text == WeatherExtension.ClearText("snow")).PerformClick();
						Check("weather menu: clear the snow is asked of the extension", File.ReadAllText(command).Contains("\"clear\":true"));
						File.WriteAllText(state, JsonSerializer.Serialize(new { on = true, kind = "rain", fog = 0.0, sun = 0.0, wind = 0.3 }));
						WeatherExtension.FillMenu(item);
						Check("weather menu: nothing to clear in the rain", !item.DropDownItems.OfType<System.Windows.Forms.ToolStripMenuItem>().Any(i => i.Text.Contains(Lang.T("ext.clear.snow"))));
						File.WriteAllText(state, "{\"on\":true,\"kind\":\"leaves\"}");
						Check("weather menu: sweep the leaves when leaves fall", WeatherExtension.Piling == "leaves" && WeatherExtension.ClearText("leaves").Contains(Lang.T("ext.clear.leaves")));
						File.Delete(scenes);
						WeatherExtension.FillMenu(item);
						Check("weather menu: hidden while the extension does not run", !item.Available);
					}
					finally { WeatherExtension.TestQuiet = false; Program.MyData.SetSetting("ExtWeather", wasOn); if (keepScenes != null) File.WriteAllText(scenes, keepScenes); else File.Delete(scenes); try { File.Delete(command); } catch (Exception) { } }
				}
			}
			finally
			{
				if (keepState != null) File.WriteAllText(state, keepState); else File.Delete(state);
			}
			return Task.CompletedTask;
		}
	}
}
