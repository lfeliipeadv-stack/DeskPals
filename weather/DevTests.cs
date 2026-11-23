using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DeskPalsWeather
{
	/// <summary>
	/// The tests of DeskPals Weather ("--devtests[=report]"; the release runs them): what falls with each weather, the
	/// pauses, the scenes and their codes, the settings file, the texts of the 23 languages, the drawings of fog, sun and grass, the colour for Snowhush, the state file DeskPals reads, and every
	/// page of the settings in every language (with "--snap=&lt;folder&gt;" their pictures are kept).
	/// </summary>
	internal static class DevTests
	{
		static int fails, passes;
		static readonly StringBuilder log = new();
		static string snapDir;

		static void Check(string name, bool ok, string detail = "")
		{
			if (ok) passes++; else fails++;
			log.AppendLine((ok ? "ok    " : "FALHA ") + name + (ok || detail == "" ? "" : "  → " + detail));
		}

		public static int Run(string report)
		{
			report ??= Path.Combine(Path.GetTempPath(), "DeskPalsWeather-devtests.txt");
			snapDir = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--snap="))?[7..].Trim('"');
			var keep = Controller.Config;
			Controller.Config = new Config();
			foreach (var (name, test) in new (string, Action)[]
			{
				("decide", Decide), ("pauses", Pauses), ("scenes", Scenes), ("config", ConfigFile), ("texts", Texts), ("deskpals", DeskPalsSettings),
				("drawings", Drawings), ("engine", EngineParts), ("menu", MenuAndStatus), ("lights", LightsTests), ("grass", GrassClip), ("state", State), ("windows", Windows),
			})
				try { test(); } catch (Exception ex) { Check(name + ": sem erro", false, ex.ToString()); }
			Controller.Config = keep;
			log.Insert(0, $"DeskPals Weather — testes: {passes} ok, {fails} falha(s)\r\n\r\n");
			try { File.WriteAllText(report, log.ToString()); } catch (Exception) { }
			return fails;
		}

		static Forecast.Now W(int code, double temp = 20, bool day = true, int clouds = 20, double wind = 10) => new() { Code = code, Temp = temp, Day = day, Clouds = clouds, Wind = wind };

		static void Decide()
		{
			var c = new Config { Kind = Kind.Rain, Fog = 30, Sun = 10, Wind = 50 };
			var d = Controller.Decide(c, null, DateTime.Now);
			Check("escolhido: o que cai e os efeitos", d.kind == Kind.Rain && Math.Abs(d.fog - 0.3) < 1e-9 && Math.Abs(d.sun - 0.1) < 1e-9 && Math.Abs(d.wind - 0.5) < 1e-9);
			c.Kind = Kind.None;
			Check("escolhido: nada caindo", Controller.Decide(c, null, DateTime.Now).kind == null);
			c = new Config { Follow = 1, Lat = 40 };
			Check("cidade: chuva (61)", Controller.Decide(c, W(61), DateTime.Now).kind == Kind.Rain);
			Check("cidade: neve (73)", Controller.Decide(c, W(73), DateTime.Now).kind == Kind.Snow);
			Check("cidade: trovoada (95) é chuva", Controller.Decide(c, W(95), DateTime.Now).kind == Kind.Rain);
			var fog = Controller.Decide(c, W(45), DateTime.Now);
			Check("cidade: neblina (45)", fog.kind == null && fog.fog > 0.7);
			Check("cidade: sol num dia limpo", Controller.Decide(c, W(0, clouds: 10), DateTime.Now).sun > 0.5);
			Check("cidade: sem sol à noite", Controller.Decide(c, W(0, day: false), DateTime.Now).sun == 0);
			Check("cidade: vento de 40 km/h = máximo", Controller.Decide(c, W(0, wind: 40), DateTime.Now).wind == 1);
			Check("cidade: dia seco sem estações não cai nada", Controller.Decide(c, W(1), new DateTime(2026, 10, 10)).kind == null);
			c.Follow = 2;
			Check("estações: outono no norte (outubro) = folhas", Controller.Decide(c, W(1), new DateTime(2026, 10, 10)).kind == Kind.Leaves);
			Check("estações: primavera no norte (abril) = flores", Controller.Decide(c, W(1), new DateTime(2026, 4, 10)).kind == Kind.Blossom);
			Check("estações: no verão nada", Controller.Decide(c, W(1), new DateTime(2026, 7, 10)).kind == null);
			Check("estações: chuva vence a estação", Controller.Decide(c, W(63), new DateTime(2026, 10, 10)).kind == Kind.Rain);
			c.Lat = -23.5;
			Check("estações: outono no sul (abril) = folhas", Controller.Decide(c, W(1), new DateTime(2026, 4, 10)).kind == Kind.Leaves);
			Check("estações: primavera no sul (outubro) = flores", Controller.Decide(c, W(1), new DateTime(2026, 10, 10)).kind == Kind.Blossom);
			c.Follow = 3;
			var inv = Controller.Decide(c, W(61, wind: 40), DateTime.Now);
			Check("invertido: chuva lá fora = sol aqui, vento ao contrário", inv.kind == null && inv.sun > 0.8 && inv.wind == 0);
			Check("invertido: dia seco e quente = neve", Controller.Decide(c, W(0, temp: 31), DateTime.Now).kind == Kind.Snow);
			Check("invertido: dia seco ameno = chuva", Controller.Decide(c, W(0, temp: 18), DateTime.Now).kind == Kind.Rain);
			Check("invertido: noite seca = nada caindo", Controller.Decide(c, W(0, day: false), DateTime.Now).kind == null);
			Check("invertido: neblina some", Controller.Decide(c, W(45), DateTime.Now).fog == 0);
		}

		static void Pauses()
		{
			var c = new Config { PauseApps = { "chrome", "obs64", "spotify" }, PauseMode = { ["obs64"] = 1, ["spotify"] = 2 } };
			Check("pausa: nada", Controller.Pauses(c, "notepad", false, false) == Paused.None);
			var all = Controller.Pauses(c, "chrome", false, false);
			Check("pausa: programa da lista pausa tudo", all.Effects && all.Sound && all.By == "chrome");
			var fx = Controller.Pauses(c, "obs64", false, false);
			Check("pausa: só os efeitos (o som continua)", fx.Effects && !fx.Sound);
			var snd = Controller.Pauses(c, "spotify", false, false);
			Check("pausa: só o som", !snd.Effects && snd.Sound);
			Check("pausa: tela cheia", Controller.Pauses(c, "notepad", true, false).By == "fullscreen");
			c.Battery = 2;
			Check("pausa: bateria (pausar)", Controller.Pauses(c, "notepad", false, true).By == "battery");
			c.Battery = 1;
			Check("pausa: bateria (menos) não pausa", Controller.Pauses(c, "notepad", false, true) == Paused.None);
			c.On = false;
			Check("pausa: desligado", Controller.Pauses(c, "notepad", false, false).By == "off");
		}

		static void Scenes()
		{
			var c = new Config { Kind = Kind.Leaves, Intensity = 77, Fog = 20, Sun = 35, Grass = 60, Wind = 80, Random = false, Sound = true };
			c.SetColor("leaves", Color.FromArgb(200, 40, 30));
			var s = Scene.From(c, "Outono vermelho ção");
			string code = s.ToCode();
			Check("cena: código começa com DPW1:", code.StartsWith(Scene.CodePrefix));
			var back = Scene.Parse(code);
			var c2 = new Config { Follow = 1 };
			back?.ApplyTo(c2);
			Check("cena: código vai e volta igual", back != null && back.Name == s.Name && back.SameAs(c) && s.SameAs(c2));
			Check("cena: usar deixa de acompanhar a cidade", c2.Follow == 0);
			Check("cena: código dentro de uma mensagem", Scene.Parse("olha que linda: " + code + " :)")?.Name == s.Name);
			Check("cena: arquivo (JSON) vai e volta", Scene.Parse(System.Text.Json.JsonSerializer.Serialize(s))?.SameAs(c) == true);
			Check("cena: texto qualquer não é cena", Scene.Parse("bom dia") == null && Scene.Parse("DPW1:!!!") == null && Scene.Parse("") == null);
			Check("cena: tipo desconhecido recusado", Scene.Parse("{\"Kind\": 9}") == null);
			var big = Scene.Parse("{\"Name\":\"x\",\"Intensity\":500,\"Fog\":-3}");
			var c3 = new Config();
			big?.ApplyTo(c3);
			Check("cena: valores fora do lugar ajustados", c3.Intensity == 100 && c3.Fog == 0);
			Check("cena: todas as cenas prontas têm nome", Scene.BuiltIn.All(b => T.Has(b.key)));
			Check("cena: as cenas prontas são diferentes", Scene.BuiltIn.Select(b => b.scene.ToCode()).Distinct().Count() == Scene.BuiltIn.Length);
		}

		static void ConfigFile()
		{
			string file = Path.Combine(Path.GetTempPath(), "DeskPalsWeather-test-config.json");
			var c = new Config { Kind = Kind.Blossom, PauseApps = { "game" }, PauseMode = { ["game"] = 2 }, Lang = "ja" };
			c.SetColor("snow", Color.FromArgb(10, 20, 30));
			c.Scenes.Add(Scene.From(c, "minha"));
			c.Save(file);
			var back = Config.Load(file);
			File.Delete(file);
			Check("configuração: salva e lida igual", back.Kind == Kind.Blossom && back.PauseModeOf("game") == 2 && back.Lang == "ja"
				&& back.ColorOf("snow") == Color.FromArgb(10, 20, 30) && back.Scenes.Count == 1);
			Check("configuração: cor própria = nenhuma", back.ColorOf("fog") == null);
			Check("configuração: arquivo estragado = padrão", Config.Load(Path.Combine(Path.GetTempPath(), "nao-existe-" + Guid.NewGuid() + ".json")).On);
		}

		static void Texts()
		{
			var en = T.Read("en");
			Check("textos: inglês tem textos", en.Count > 120, en.Count.ToString());
			var holes = new Regex(@"\{\d\}");
			foreach (var (code, _) in T.Languages)
			{
				var d = T.Read(code);
				var missing = en.Keys.Where(k => !d.ContainsKey(k)).ToList();
				var empty = d.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
				var extra = d.Keys.Where(k => !en.ContainsKey(k)).ToList();
				var wrong = en.Where(kv => d.TryGetValue(kv.Key, out var v) && !holes.Matches(kv.Value).Select(m => m.Value).OrderBy(x => x).SequenceEqual(holes.Matches(v).Select(m => m.Value).OrderBy(x => x))).Select(kv => kv.Key).ToList();
				Check($"textos [{code}]: completos", missing.Count == 0, string.Join(", ", missing.Take(12)));
				Check($"textos [{code}]: sem vazios nem sobras", empty.Count == 0 && extra.Count == 0, string.Join(", ", empty.Concat(extra).Take(12)));
				Check($"textos [{code}]: mesmos {{0}} do inglês", wrong.Count == 0, string.Join(", ", wrong.Take(12)));
			}
			// the keys made in the code from names
			var made = SettingsForm.Pages.Select(p => "tab." + p.id)
				.Concat(new[] { "snow", "leaves", "rain", "blossom" }.Select(k => "k." + k))
				.Concat(new[] { "snow", "leaves", "rain", "blossom", "fog", "sun", "grass" }.Select(k => "c." + k))
				.Concat(Enumerable.Range(0, 4).Select(i => "ld.mode." + i)).Concat(Enumerable.Range(0, 6).Select(i => "ld.shape." + i))
				.Concat(Scene.BuiltIn.Select(b => b.key)).ToList();
			var absent = made.Where(k => !en.ContainsKey(k)).ToList();
			Check("textos: os nomes feitos no código existem", absent.Count == 0, string.Join(", ", absent));
			Check("idioma: pt → pt-BR, zh-TW → zh-CN, xx → en", T.Closest("pt") == "pt-BR" && T.Closest("zh-TW") == "zh-CN" && T.Closest("xx") == "en" && T.Closest("es-MX") == "es");
		}

		static void DeskPalsSettings()
		{
			string file = Path.Combine(Path.GetTempPath(), "DeskPalsWeather-test-DesktopPet.config");
			File.WriteAllText(file, "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings><add key=\"Language\" value=\"fr\" /><add key=\"Theme\" value=\"dark\" /><add key=\"Accent\" value=\"pink\" /></appSettings></configuration>");
			var c = new Config { DeskPalsConfig = file };
			Check("DeskPals: lê idioma, tema e cor", DeskPals.Setting(c, "Language") == "fr" && DeskPals.Setting(c, "Theme") == "dark" && DeskPals.Setting(c, "Accent") == "pink");
			Check("DeskPals: usa o idioma do DeskPals", T.Pick(c) == "fr");
			c.Lang = "es";
			Check("DeskPals: o idioma escolhido aqui vence", T.Pick(c) == "es");
			File.Delete(file);
		}

		static void Drawings()
		{
			using var b = new Bitmap(400, 300, PixelFormat.Format32bppPArgb);
			using (var g = Graphics.FromImage(b)) Overlay.DrawSun(g, b.Size, 1, 1, Color.FromArgb(255, 0, 0));
			Check("desenho: raios de sol aparecem", Painted(b) > 1000);
			Check("desenho: raios de sol na cor escolhida", Average(b).R > Average(b).G * 3);
			using var b2 = new Bitmap(400, 300, PixelFormat.Format32bppPArgb);
			using (var g = Graphics.FromImage(b2)) Overlay.DrawGrass(g, new() { (10, 390, 200) }, Point.Empty, 1, 1, 0.5, Color.FromArgb(40, 60, 230));
			bool above = false, below = false;
			for (int x = 10; x < 390; x++) { if (b2.GetPixel(x, 192).A > 0) above = true; if (b2.GetPixel(x, 230).A > 0) below = true; }
			Check("desenho: grama em cima da borda, nada embaixo", above && !below);
			Check("desenho: grama na cor escolhida", Average(b2).B > Average(b2).G);
			using var blob = Overlay.MakeBlob(64, Color.White);
			using var b3 = new Bitmap(400, 300, PixelFormat.Format32bppPArgb);
			using (var g = Graphics.FromImage(b3)) Overlay.DrawFog(g, blob, new[] { (200f, 150f, 100f, 0.3f) }, 0, 1);
			Check("desenho: neblina aparece", Painted(b3) > 5000);
			Timing();
		}

		/// <summary>How long a frame of the sky takes (written in the report, not a test).</summary>
		static void Timing()
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var small = new Bitmap(640, 360, PixelFormat.Format32bppPArgb);
			using var full = new Bitmap(1920, 1080, PixelFormat.Format32bppPArgb);
			using var blob = Overlay.MakeBlob(96, Color.White, 0.4);
			var clouds = Enumerable.Range(0, 9).Select(i => (i * 200f, 300f + i * 50, 400f, 0.3f)).ToArray();
			double draw = 0, stretch = 0;
			for (int n = 0; n < 10; n++)
			{
				sw.Restart();
				using (var g = Graphics.FromImage(small)) { g.Clear(Color.Transparent); g.ScaleTransform(1 / 3f, 1 / 3f); Overlay.DrawSun(g, full.Size, n, 0.4, Color.Gold); Overlay.DrawFog(g, blob, clouds, n, 1); }
				draw += sw.Elapsed.TotalMilliseconds; sw.Restart();
				using (var g = Graphics.FromImage(full)) { g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy; g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear; g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; g.DrawImage(small, new Rectangle(0, 0, 1920, 1080), 0, 0, 640, 360, GraphicsUnit.Pixel); }
				stretch += sw.Elapsed.TotalMilliseconds;
			}
			double ours = 0;
			var fd = full.LockBits(new Rectangle(0, 0, 1920, 1080), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
			for (int n = 0; n < 10; n++) { sw.Restart(); Overlay.Stretch(small, fd.Scan0, 1920, 1080, fd.Stride); ours += sw.Elapsed.TotalMilliseconds; }
			full.UnlockBits(fd);
			log.AppendLine($"info  céu: desenho pequeno {draw / 10:N1} ms, ampliar com o GDI+ {stretch / 10:N1} ms, com a nossa {ours / 10:N1} ms por quadro");
			// the stretch keeps a flat colour flat and a step between two colours in between
			using var two = new Bitmap(2, 1, PixelFormat.Format32bppPArgb);
			two.SetPixel(0, 0, Color.FromArgb(255, 0, 0, 0)); two.SetPixel(1, 0, Color.FromArgb(255, 200, 200, 200));
			using var wide = new Bitmap(6, 3, PixelFormat.Format32bppPArgb);
			var wd = wide.LockBits(new Rectangle(0, 0, 6, 3), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
			Overlay.Stretch(two, wd.Scan0, 6, 3, wd.Stride);
			wide.UnlockBits(wd);
			var line = Enumerable.Range(0, 6).Select(x => (int)wide.GetPixel(x, 1).R).ToList();
			Check("céu: ampliar fica suave (do preto ao cinza)", line[0] == 0 && line[5] == 200 && line.Zip(line.Skip(1)).All(p => p.Second >= p.First) && line[2] > 0 && line[3] < 200, string.Join(",", line));
		}

		/// <summary>The Christmas lights: every shape drawn, hidden behind a window in front, December, the colours, the scene.</summary>
		static void LightsTests()
		{
			var keepShape = LightsArt.Shape;
			try
			{
				foreach (var shape in Enum.GetValues<LightsArt.BulbShape>())
				{
					LightsArt.Shape = shape;
					using var b = new Bitmap(300, 200, PixelFormat.Format32bppPArgb);
					using (var g = Graphics.FromImage(b)) LightsArt.Draw(g, new Rectangle(40, 40, 220, 120), Array.Empty<Rectangle>(), 0, LightsMode.Steady);
					Check($"luzes {shape}: desenhadas em volta da janela", Painted(b) > 300 && b.GetPixel(150, 100).A == 0);
				}
				LightsArt.Shape = LightsArt.BulbShape.Video;
				using (var b = new Bitmap(300, 200, PixelFormat.Format32bppPArgb))
				{
					using (var g = Graphics.FromImage(b)) LightsArt.Draw(g, new Rectangle(40, 40, 220, 120), new[] { new Rectangle(0, 0, 300, 200) }, 0, LightsMode.Steady);
					Check("luzes: escondidas atrás de uma janela na frente", Painted(b) == 0);
				}
			}
			finally { LightsArt.Shape = keepShape; }
			// the wire keeps its colour while the bulbs blink: where the wire shows (not under a bulb), every frame is the same there
			foreach (var mode in new[] { LightsMode.Twinkle, LightsMode.Chase, LightsMode.Changing })
			{
				var keepRandom = LightsArt.RandomColors; LightsArt.RandomColors = true;
				Bitmap Frame(double t, bool bulbs)
				{
					var b = new Bitmap(300, 200, PixelFormat.Format32bppPArgb);
					LightsArt.NoBulbs = !bulbs;
					using (var g = Graphics.FromImage(b)) LightsArt.Draw(g, new Rectangle(40, 40, 220, 120), Array.Empty<Rectangle>(), t, mode);
					LightsArt.NoBulbs = false;
					return b;
				}
				using var wireOnly = Frame(0, false);
				var frames = Enumerable.Range(0, 8).Select(n => Frame(n * 0.37, true)).ToList();
				int wire = 0, changes = 0;
				for (int y = 0; y < 200; y++)
					for (int x = 0; x < 300; x++)
					{
						var w = wireOnly.GetPixel(x, y);
						if (w.A < 255) continue;   // the wire itself (its soft edge lets what is under it show, as any edge does)
						// a wire pixel that some frame shows as the bare wire: it must be the bare wire in every frame without a bulb on it
						var seen = frames.Select(f => f.GetPixel(x, y)).ToList();
						if (!seen.Any(p => p.ToArgb() == w.ToArgb())) continue;
						wire++;
						if (seen.Any(p => p.ToArgb() != w.ToArgb() && Math.Abs(p.R - w.R) + Math.Abs(p.G - w.G) + Math.Abs(p.B - w.B) < 90)) changes++;
					}
				frames.ForEach(f => f.Dispose());
				LightsArt.RandomColors = keepRandom;
				Check($"luzes {mode}: o fio não pisca nem muda de cor", wire > 200 && changes == 0, $"{changes} de {wire} pontos do fio mudam");
			}
			// how long a frame of twinkling lights takes on a screen (written in the report, not a test)
			using (var screenPic = new Bitmap(1920, 1080, PixelFormat.Format32bppPArgb))
			using (var g = Graphics.FromImage(screenPic))
			{
				var win = new Rectangle(420, 220, 900, 520);
				var sw = System.Diagnostics.Stopwatch.StartNew();
				for (int n = 0; n < 12; n++) LightsArt.Draw(g, win, Array.Empty<Rectangle>(), n * 0.165, LightsMode.Twinkle);
				double open = sw.Elapsed.TotalMilliseconds / 12; sw.Restart();
				for (int n = 0; n < 12; n++) LightsArt.Draw(g, win, new[] { new Rectangle(0, 0, 300, 300), new Rectangle(1500, 700, 300, 300) }, n * 0.165, LightsMode.Twinkle);
				double hidden = sw.Elapsed.TotalMilliseconds / 12; sw.Restart();
				for (int n = 0; n < 12; n++) g.Clear(Color.Transparent);
				double clear = sw.Elapsed.TotalMilliseconds / 12;
				log.AppendLine($"info  luzes piscando: janela de 900x520 {open:N1} ms, com janelas na frente {hidden:N1} ms, limpar a tela {clear:N1} ms por quadro");
			}
			var c = new Config();
			{
				int Count(float gap, bool random) { LightsArt.Spacing = gap; LightsArt.RandomAngles = random; using var b = new Bitmap(300, 200, PixelFormat.Format32bppPArgb); using (var g = Graphics.FromImage(b)) LightsArt.Draw(g, new Rectangle(40, 40, 220, 120), Array.Empty<Rectangle>(), 0, LightsMode.Steady); return Painted(b); }
				int n20 = Count(20, true), n30 = Count(30, true), ordered = Count(25, false), random = Count(25, true);
				Check("luzes: 20 pontos tem mais lâmpadas que 30", n20 > n30, $"{n20} / {n30}");
				Check("luzes: ângulos organizados e aleatórios são diferentes", ordered != random);
				LightsArt.Spacing = 0; LightsArt.RandomAngles = true;
			}
			Check("luzes: em dezembro acendem sozinhas", c.LightsToday(new DateTime(2026, 12, 3)) && !c.LightsToday(new DateTime(2026, 11, 3)));
			c.LightsDecember = false;
			Check("luzes: sem a opção, dezembro não acende", !c.LightsToday(new DateTime(2026, 12, 3)));
			c.Lights = true;
			Check("luzes: ligadas, acendem em qualquer mês", c.LightsToday(new DateTime(2026, 6, 3)));
			c.LightsMulti = false; c.LightsColor = 0x123456; c.LightsBrightness = 100;
			Controller.ApplyLights(c);
			Check("luzes: uma cor pinta todas", Enumerable.Range(0, 10).All(i => LightsArt.Shine(LightsMode.Steady, i, i * 7, 0).color.ToArgb() == Color.FromArgb(0x12, 0x34, 0x56).ToArgb()));
			c.LightsMulti = true; c.LightsColors = LightsArt.DefaultPattern().Select(Controller.ToRgb).ToList(); c.LightsColors[0] = 0xFFFFFF;
			Controller.ApplyLights(c);
			Check("luzes: a primeira lâmpada da guirlanda manda na primeira do fio", LightsArt.Shine(LightsMode.Steady, 0, 0, 0).color.ToArgb() == Color.White.ToArgb() && LightsArt.Shine(LightsMode.Steady, 24, 0, 0).color.ToArgb() == Color.White.ToArgb());
			var at0 = LightsArt.Shine(LightsMode.Changing, 3, 0, 0).color; var at5 = LightsArt.Shine(LightsMode.Changing, 3, 0, 5).color;
			Check("luzes: cores que mudam, mudam", at0.ToArgb() != at5.ToArgb());
			c.LightsSpeed = 2; Controller.ApplyLights(c);
			bool fast = LightsArt.Speed == 2f;
			c.LightsSpeed = 0; Controller.ApplyLights(c);
			Check("luzes: velocidade (devagar, normal, rápido)", fast && LightsArt.Speed == 0.5f && new Config().LightsSpeed == 1);
			c.LightsSpeed = 1;
			{
				// a window in full screen gets no lights round it (they would shine over it, along the edges of the screen)
				var screenBox = new Rectangle(0, 0, 1920, 1080);
				Check("luzes: janela em tela cheia fica sem luzes", DesktopWindows.CoversScreen(new Rectangle(0, 0, 1920, 1080), screenBox) && !DesktopWindows.CoversScreen(new Rectangle(0, 0, 1920, 1040), screenBox));
			}
			using (var d = new LightsDialog())
				Check("luzes: a caixa das lâmpadas cabe na tela", d.Height <= Screen.PrimaryScreen.WorkingArea.Height, $"{d.Height} / {Screen.PrimaryScreen.WorkingArea.Height}");
			Check("chuva atrás das janelas: sem o Snowhush rodando, nenhum aviso", Engine.Running || !Engine.BehindFailed());
			var defaults = new Config();
			Check("padrões: som 0%, luzes 90% correndo, F, 30 pontos, ângulos e cores aleatórios",
				defaults.Volume == 0 && defaults.LightsBrightness == 90 && defaults.LightsMode == (int)LightsMode.Chase && defaults.LightsShape == 5 && defaults.LightsSpacing == 30 && defaults.LightsRandomAngles && defaults.LightsRandom);
			c.LightsBrightness = 0;
			Controller.ApplyLights(c);
			Check("luzes: brilho baixo apaga quase tudo", LightsArt.Shine(LightsMode.Steady, 0, 0, 0).light < 0.25f);
			Controller.ApplyLights(new Config());
			var xmas = Scene.BuiltIn.First(b => b.key == "scene.christmas").scene;
			Check("cena Natal: neve e luzes, e o código guarda as luzes", xmas.Lights && xmas.Kind == Kind.Snow && Scene.Parse(xmas.ToCode())?.Lights == true);
		}

		/// <summary>The grass draws only inside the part being drawn again.</summary>
		static void GrassClip()
		{
			using (var b = new Bitmap(400, 120, PixelFormat.Format32bppPArgb))
			{
				using (var g = Graphics.FromImage(b))
					for (double t = 0; t < 4; t += 0.5) GrassArt.DrawFast(g, 100, 301, 100, 1, 1, t, GrassArt.Own);
				bool past = false;
				for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) if ((x < 100 || x > 301) && b.GetPixel(x, y).A > 10) past = true;
				Check("grama: nada sobra para fora das pontas da janela (nem com vento forte)", !past);
			}
			// (what is outside was not cleared: drawing there would pile the grass up)
			using (var b = new Bitmap(400, 120, PixelFormat.Format32bppPArgb))
			using (var g = Graphics.FromImage(b))
			{
				g.SetClip(new Rectangle(0, 0, 100, 120));
				GrassArt.DrawFast(g, 0, 400, 100, 0.5, 0.3, 0, GrassArt.Own);
				g.ResetClip();
				bool outsideClip = false;
				for (int y = 0; y < b.Height; y++) for (int x = 130; x < b.Width; x++) if (b.GetPixel(x, y).A > 10) outsideClip = true;
				Check("grama: desenha só dentro da parte pedida", !outsideClip);
			}
		}

		/// <summary>DeskPals' menu (the scenes file it reads, the commands it sends), the status line, the test, the pictures of the scenes.</summary>
		static void MenuAndStatus()
		{
			var c = Controller.Config;
			string file = Path.Combine(Path.GetTempPath(), "DeskPalsWeather-test-scenes.json");
			Scene.BuiltIn[0].scene.ApplyTo(c);
			Controller.WriteScenes(file);
			var j = JsonNode.Parse(File.ReadAllText(file));
			File.Delete(file);
			Check("menu: as cenas vão para o DeskPals, com a que está em uso", j["scenes"]?.AsArray().Count == Scene.BuiltIn.Length && (string)j["current"] == "b:" + Scene.BuiltIn[0].key && (string)j["labels"]?["test"] == T.S("w.test.button"));
			string Cmd(object o) { var n = System.Text.Json.JsonSerializer.SerializeToNode(o).AsObject(); n["time"] = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture); return n.ToJsonString(); }
			c.SavedAt = Path.Combine(Path.GetTempPath(), "DeskPalsWeather-test-cmd.json");
			bool ok = Controller.RunCommand(Cmd(new { scene = "b:" + Scene.BuiltIn[3].key }));
			Check("menu: escolher uma cena pelo DeskPals", ok && Scene.BuiltIn[3].scene.SameAs(c));
			Check("menu: limpar a neve pelo DeskPals", Controller.RunCommand(Cmd(new { clear = true })));
			Controller.RunCommand(Cmd(new { on = false }));
			Check("menu: desligar pelo DeskPals", !c.On);
			Check("menu: pedido antigo é ignorado", !Controller.RunCommand("{\"scene\":\"b:scene.winter\",\"time\":\"2020-01-01T00:00:00\"}") && !Controller.RunCommand(Cmd(new { scene = "b:nada" })));
			Check("estado: desligado", SettingsForm.StateText() == T.S("w.off"));
			c.On = true;
			Controller.Now = new Paused(true, true, "fullscreen"); Controller.Front = "vlc";
			Check("estado: diz qual programa está em tela cheia", SettingsForm.StateText().Contains("vlc"));
			Controller.Now = Paused.None;
			Controller.Shown = (Kind.Snow, 55, 0.12, 0, 0, true);
			string line = SettingsForm.StateText();
			Check("estado: mostra o que está na tela", line.Contains("55%") && line.Contains("12%") && line.Contains(T.S("tab.sound").ToLower()), line);
			Controller.TestUntil = DateTime.Now.AddSeconds(15);
			Check("testar: avisa que está testando", Controller.Testing && SettingsForm.StateText() == T.S("w.testing"));
			Controller.TestUntil = DateTime.MinValue;
			Check("tamanho dos flocos: pequeno, médio, grande", Engine.Scale(0) == 100 && Engine.Scale(1) == 160 && Engine.Scale(2) == 230);
			foreach (var (key, scene) in Scene.BuiltIn)
			{
				using var b = new Bitmap(76, 48, PixelFormat.Format32bppArgb);
				using (var g = Graphics.FromImage(b)) SceneArt.Draw(g, new Rectangle(0, 0, 75, 47), scene);
				Check($"cena {key}: desenho", Painted(b) > 600);
			}
			using var snowy = new Bitmap(76, 48); using var foggy = new Bitmap(76, 48);
			using (var g = Graphics.FromImage(snowy)) SceneArt.Draw(g, new Rectangle(0, 0, 75, 47), Scene.BuiltIn[0].scene);
			using (var g = Graphics.FromImage(foggy)) SceneArt.Draw(g, new Rectangle(0, 0, 75, 47), Scene.BuiltIn.First(b => b.key == "scene.sunny").scene);
			Check("cenas: desenhos diferentes para climas diferentes", Average(snowy).B < Average(foggy).B || Average(snowy).R != Average(foggy).R);
		}

		static void EngineParts()
		{
			Check("Snowhush: cor própria = 0", Engine.Tint(null) == 0);
			Check("Snowhush: cor = 0x01RRGGBB", Engine.Tint(Color.FromArgb(255, 0x12, 0x34, 0x56)) == 0x01123456);
			Check("Snowhush: o motor está junto", Engine.Exe() != null, "DeskPals-Snowhush.exe não encontrado");
			Check("atalhos: sem repetidos", Hotkeys.Choices.Select(h => h.text).Distinct().Count() == Hotkeys.Choices.Length);
		}

		static void State()
		{
			string file = Path.Combine(Path.GetTempPath(), "DeskPalsWeather-test-state.json");
			Controller.WriteState(Kind.Rain, 0.4, 0.1, 0.5, false, file);
			var j = JsonNode.Parse(File.ReadAllText(file));
			Check("estado: o que cai, para o DeskPals", (bool?)j["on"] == true && (string)j["kind"] == "rain" && Math.Abs((double)j["fog"] - 0.4) < 1e-9);
			Controller.WriteState(Kind.Rain, 0.4, 0.1, 0.5, true, file);
			j = JsonNode.Parse(File.ReadAllText(file));
			Check("estado: pausado não mostra nada", (bool?)j["on"] == false && j["kind"] == null && (double)j["fog"] == 0);
			File.Delete(file);
			Check("tela cheia: janela do tamanho da tela", DesktopWindows.CoversScreen(new Rectangle(0, 0, 1920, 1080), new Rectangle(0, 0, 1920, 1080)) && !DesktopWindows.CoversScreen(new Rectangle(0, 0, 1920, 1040), new Rectangle(0, 0, 1920, 1080)));
		}

		/// <summary>Every page of the settings in every language: built and drawn without an error (pictures kept with --snap).</summary>
		static void Windows()
		{
			string keepLang = Controller.Config.Lang;
			foreach (var (code, _) in T.Languages)
			{
				Controller.Config.Lang = code;
				T.Use(code);
				using var f = new SettingsForm { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ShowInTaskbar = false };
				f.Show();
				Application.DoEvents();
				var errors = new List<string>();
				foreach (var (id, _) in SettingsForm.Pages)
					try
					{
						f.Show(id);
						Application.DoEvents();
						using var b = new Bitmap(f.Width, f.Height);
						f.DrawToBitmap(b, new Rectangle(Point.Empty, f.Size));
						if (snapDir != null) { Directory.CreateDirectory(snapDir); b.Save(Path.Combine(snapDir, $"{code}-{id}.png"), ImageFormat.Png); }
					}
					catch (Exception ex) { errors.Add(id + ": " + ex.Message); }
				try
				{
					using var pic = LightsDialog.Picture(true);
					if (snapDir != null) pic.Save(Path.Combine(snapDir, $"{code}-luzes.png"), ImageFormat.Png);
				}
				catch (Exception ex) { errors.Add("luzes: " + ex.Message); }
				Check($"janela [{code}]: todas as páginas", errors.Count == 0, string.Join("; ", errors));
			}
			Controller.Config.Lang = keepLang;
			T.Use(T.Pick(Controller.Config));
		}

		static int Painted(Bitmap b)
		{
			int n = 0;
			for (int y = 0; y < b.Height; y += 2) for (int x = 0; x < b.Width; x += 2) if (b.GetPixel(x, y).A > 10) n++;
			return n;
		}

		static Color Average(Bitmap b)
		{
			long r = 0, g = 0, bl = 0, n = 0;
			for (int y = 0; y < b.Height; y += 3) for (int x = 0; x < b.Width; x += 3) { var p = b.GetPixel(x, y); if (p.A > 10) { r += p.R; g += p.G; bl += p.B; n++; } }
			return n == 0 ? Color.Empty : Color.FromArgb((int)(r / n), (int)(g / n), (int)(bl / n));
		}
	}
}
