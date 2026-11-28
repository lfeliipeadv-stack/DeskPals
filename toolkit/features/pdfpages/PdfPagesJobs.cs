using System.Globalization;
using System.Runtime.CompilerServices;
using PdfSharp.Drawing;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// What can be done to the pages of a PDF: make it smaller (for e-mail), choose and reorder its pages, number them,
	/// put a watermark, protect it with a password and take the password off. Every job saves a new PDF next to the old
	/// one, which is never changed. Ranks: smaller 22, choose pages 24, numbers 56, watermark 57, password 58; a PDF that
	/// asks a password gets "remove the password" at 12 (no other job can open it).
	/// </summary>
	internal static class PdfPagesJobs
	{
		static bool OnePdf(IList<string> f) => f.Count == 1 && File.Exists(f[0]) && IsPdf(f[0]);
		/// <summary>One PDF that opens without a password (and can be changed).</summary>
		static bool OneOpenPdf(IList<string> f) => OnePdf(f) && PdfLock.Of(f[0]) == PdfLock.State.Open;

		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "compress", Emoji = "🗜️",
				Rank = f => f.Count > 0 && f.All(x => File.Exists(x) && IsPdf(x) && PdfLock.Of(x) == PdfLock.State.Open) ? 22 : -1,
				Run = Compress,
			});
			Register(new JobDef
			{
				Id = "pages", Emoji = "📑",
				// "See and edit" does the same (and more) in one window: this job stays for the command line and old answers
				Rank = f => -1,
				Ask = (owner, files) => PagesForm.Choose(owner, files[0]),
				Run = c => c.Each((file, i) =>
				{
					string o = OutName(file, ".pdf", T.S("name.pages"));
					PdfPick.Save(file, o, c.Options);
					c.Outputs.Add(o);
					return Task.CompletedTask;
				}),
			});
			Register(new JobDef
			{
				Id = "pagenumbers", Emoji = "🔢",
				Rank = f => OneOpenPdf(f) ? 56 : -1,
				Ask = AskNumbers,
				Run = c => c.Each((file, i) =>
				{
					string o = OutName(file, ".pdf", T.S("pdfpages.name.numbered"));
					PdfStamp.Apply(file, o, Numbers(c), p => c.Report(p, Path.GetFileName(file)), c.Cancel);
					c.Outputs.Add(o);
					return Task.CompletedTask;
				}),
			});
			Register(new JobDef
			{
				Id = "watermark", Emoji = "💧",
				Rank = f => OneOpenPdf(f) ? 57 : -1,
				Ask = AskWatermark,
				Run = c => c.Each((file, i) =>
				{
					string text = c.Option("text").Trim();
					if (text.Length == 0) throw new InvalidOperationException(T.S("pdfpages.err.noText"));
					string o = OutName(file, ".pdf", T.S("pdfpages.name.watermark"));
					PdfStamp.Apply(file, o, Watermark(c, text), p => c.Report(p, Path.GetFileName(file)), c.Cancel);
					c.Outputs.Add(o);
					return Task.CompletedTask;
				}),
			});
			Register(new JobDef
			{
				Id = "protect", Emoji = "🔒",
				Rank = f => OneOpenPdf(f) ? 58 : -1,
				Ask = AskPassword,
				Run = c => c.Each((file, i) =>
				{
					string o = OutName(file, ".pdf", T.S("pdfpages.name.protected"));
					try { PdfLock.Protect(file, o, c.Option("password"), c.Option("owner"), c.Option("print", "1") == "1", c.Option("copy", "1") == "1", c.Option("edit", "1") == "1"); }
					catch (Exception) { try { File.Delete(o); } catch (Exception) { } throw; }
					c.Outputs.Add(o);
					return Task.CompletedTask;
				}),
			});
			Register(new JobDef
			{
				Id = "unprotect", Emoji = "🔓",
				Rank = f => OnePdf(f) && PdfLock.Of(f[0]) == PdfLock.State.Locked ? 12 : -1,
				Ask = (owner, files) => Ask.Show(owner, T.S("pdfpages.unlock.title"), T.S("pdfpages.unlock.intro"),
					new Ask.Field { Key = "password", Label = T.S("pdfpages.lock.password"), Kind = Ask.Kind.Password, Check = v => v.Length == 0 ? T.S("pdfpages.err.noPassword") : null }),
				Run = c => c.Each((file, i) =>
				{
					string o = OutName(file, ".pdf", T.S("pdfpages.name.unlocked"));
					try { PdfLock.Unprotect(file, o, c.Option("password")); }
					catch (Exception) { try { File.Delete(o); } catch (Exception) { } throw; }
					c.Outputs.Add(o);
					return Task.CompletedTask;
				}),
			});
		}

		// ------------------------------------------------------------------ smaller

		static async Task Compress(JobContext c)
		{
			long before = 0, after = 0;
			await c.Each(async (file, i) =>
			{
				string o = OutName(file, ".pdf", T.S("pdfpages.name.smaller"));
				bool smaller = await PdfCompress.Compress(file, o, p => c.Report((i + p) / c.Files.Count, Path.GetFileName(file)), c.Cancel);
				// not smaller: no useless copy, and the user is told
				if (!smaller) { c.Errors.Add(Path.GetFileName(file) + ": " + T.S("pdfpages.compress.notSmaller")); return; }
				before += new FileInfo(file).Length;
				after += new FileInfo(o).Length;
				c.Outputs.Add(o);
			});
			if (c.Outputs.Count > 0) c.Preview = PdfCompress.Size(before) + " → " + PdfCompress.Size(after);
		}

		// ------------------------------------------------------------------ page numbers

		static Dictionary<string, string> AskNumbers(IWin32Window owner, IList<string> files) => AskNumbers(owner, files, null);

		/// <summary>The questions of page numbers, filled with earlier answers (the viewer asks again to change them).</summary>
		internal static Dictionary<string, string> AskNumbers(IWin32Window owner, IList<string> files, Dictionary<string, string> now)
		{
			string V(string key, string fallback) => now != null && now.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
			int total = 12;
			try { total = PdfWork.Pages(files[0]); } catch (Exception) { }
			return Ask.Show(owner, T.S("pdfpages.num.title"), null,
				new Ask.Field
				{
					Key = "position", Label = T.S("pdfpages.num.position"), Kind = Ask.Kind.Choice, Value = V("position", "bottom-center"),
					Choices = new[] { ("bottom-center", T.S("pdfpages.num.bottomCenter")), ("bottom-right", T.S("pdfpages.num.bottomRight")), ("top-right", T.S("pdfpages.num.topRight")) },
				},
				new Ask.Field
				{
					Key = "format", Label = T.S("pdfpages.num.format"), Kind = Ask.Kind.Choice, Value = V("format", "full"),
					Choices = new[] { ("full", T.S("pdfpages.num.full", 1, total)), ("number", "1, 2, 3…") },
				},
				new Ask.Field { Key = "first", Label = T.S("pdfpages.num.first"), Kind = Ask.Kind.Number, Value = V("first", "1"), Min = 0, Max = 99999 },
				new Ask.Field { Key = "size", Label = T.S("pdfpages.num.size"), Kind = Ask.Kind.Number, Value = V("size", "10"), Min = 6, Max = 48 });
		}

		static int Int(JobContext c, string key, int fallback, int min, int max) =>
			int.TryParse(c.Option(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? Math.Clamp(v, min, max) : fallback;

		/// <summary>"Page 3 of 12" (or "3") at the chosen corner, a bit in from the edge.</summary>
		internal static PdfStamp.Draw Numbers(JobContext c)
		{
			string position = c.Option("position", "bottom-center");
			bool full = c.Option("format", "full") != "number";
			int first = Int(c, "first", 1, 0, 99999);
			double size = Int(c, "size", 10, 6, 48);
			var color = XColor.FromArgb(255, 64, 64, 64);
			var writer = new StampText();
			return (g, w, h, i, count) =>
			{
				string text = full ? T.S("pdfpages.num.full", first + i, first + count - 1) : (first + i).ToString(CultureInfo.CurrentCulture);
				double margin = Math.Min(28, Math.Min(w, h) * 0.05);
				var (point, format) = position switch
				{
					"bottom-right" => (new XPoint(w - margin, h - margin), XStringFormats.BottomRight),
					"top-right" => (new XPoint(w - margin, margin), XStringFormats.TopRight),
					_ => (new XPoint(w / 2, h - margin), XStringFormats.BottomCenter),
				};
				writer.Draw(g, text, size, false, color, point, format);
			};
		}

		// ------------------------------------------------------------------ watermark

		static Dictionary<string, string> AskWatermark(IWin32Window owner, IList<string> files) => AskWatermark(owner, files, null);

		/// <summary>The questions of the watermark, filled with earlier answers.</summary>
		internal static Dictionary<string, string> AskWatermark(IWin32Window owner, IList<string> files, Dictionary<string, string> now)
		{
			string V(string key, string fallback) => now != null && now.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
			return Ask.Show(owner, T.S("pdfpages.wm.title"), null,
			new Ask.Field { Key = "text", Label = T.S("pdfpages.wm.text"), Value = V("text", T.S("pdfpages.wm.default")), Hint = T.S("pdfpages.wm.hint"), Check = v => string.IsNullOrWhiteSpace(v) ? T.S("pdfpages.err.noText") : null },
			new Ask.Field
			{
				Key = "layout", Label = T.S("pdfpages.wm.layout"), Kind = Ask.Kind.Choice, Value = V("layout", "diagonal"),
				Choices = new[] { ("diagonal", T.S("pdfpages.wm.diagonal")), ("horizontal", T.S("pdfpages.wm.horizontal")) },
			},
			new Ask.Field { Key = "opacity", Label = T.S("pdfpages.wm.opacity"), Kind = Ask.Kind.Number, Value = V("opacity", "25"), Min = 5, Max = 100 },
			new Ask.Field
			{
				Key = "color", Label = T.S("pdfpages.wm.color"), Kind = Ask.Kind.Choice, Value = V("color", "gray"),
				Choices = new[] { ("gray", T.S("pdfpages.wm.gray")), ("red", T.S("pdfpages.wm.red")), ("blue", T.S("pdfpages.wm.blue")) },
			});
		}

		/// <summary>Big see-through letters across the middle of the page, diagonal (bottom left to top right) or straight.</summary>
		internal static PdfStamp.Draw Watermark(JobContext c, string text)
		{
			bool diagonal = c.Option("layout", "diagonal") != "horizontal";
			int alpha = (int)Math.Round(Int(c, "opacity", 25, 5, 100) * 2.55);
			var (r, gr, b) = c.Option("color", "gray") switch { "red" => (200, 30, 30), "blue" => (30, 80, 200), _ => (128, 128, 128) };
			var color = XColor.FromArgb(alpha, r, gr, b);
			var writer = new StampText();
			return (g, w, h, i, count) =>
			{
				// as big as fits: three quarters of the diagonal (or four fifths of the width), not taller than a share of the page
				double room = diagonal ? Math.Sqrt(w * w + h * h) * 0.75 : w * 0.8;
				double width = Math.Max(1, writer.Width(g, text, 100, true));
				// a tiny page (a label, a strip) gets smaller letters than 8 points rather than an error
				double max = Math.Max(1, Math.Min(w, h) * (diagonal ? 0.3 : 0.25));
				double size = Math.Clamp(100 * room / width, Math.Min(8, max), max);
				var state = g.Save();
				g.TranslateTransform(w / 2, h / 2);
				if (diagonal) g.RotateTransform(-Math.Atan2(h, w) * 180 / Math.PI);
				writer.Draw(g, text, size, true, color, new XPoint(0, 0), XStringFormats.Center);
				g.Restore(state);
			};
		}

		// ------------------------------------------------------------------ password

		/// <summary>The password, typed twice (asked again while the two differ).</summary>
		static Dictionary<string, string> AskPassword(IWin32Window owner, IList<string> files)
		{
			while (true)
			{
				var a = Ask.Show(owner, T.S("pdfpages.lock.title"), T.S("pdfpages.lock.intro"),
					new Ask.Field { Key = "password", Label = T.S("pdfpages.lock.password"), Kind = Ask.Kind.Password, Hint = T.S("pdfpages.lock.openHint") },
					new Ask.Field { Key = "confirm", Label = T.S("pdfpages.lock.confirm"), Kind = Ask.Kind.Password },
					new Ask.Field { Kind = Ask.Kind.Info, Label = T.S("pdfpages.lock.allow") },
					new Ask.Field { Key = "print", Label = T.S("pdfpages.lock.print"), Kind = Ask.Kind.Check, Value = "1" },
					new Ask.Field { Key = "copy", Label = T.S("pdfpages.lock.copy"), Kind = Ask.Kind.Check, Value = "1" },
					new Ask.Field { Key = "edit", Label = T.S("pdfpages.lock.edit"), Kind = Ask.Kind.Check, Value = "1" },
					new Ask.Field { Key = "owner", Label = T.S("pdfpages.lock.owner"), Kind = Ask.Kind.Password, Hint = T.S("pdfpages.lock.ownerHint") });
				if (a == null) return null;
				bool limited = a["print"] != "1" || a["copy"] != "1" || a["edit"] != "1";
				if (a["password"].Length == 0 && !limited) { MessageBox.Show(owner, T.S("pdfpages.err.noPasswordNorLimit"), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Information); continue; }
				if (a["password"] == a["confirm"]) { a.Remove("confirm"); return a; }
				MessageBox.Show(owner, T.S("pdfpages.err.differ"), T.S("app"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}
	}
}
