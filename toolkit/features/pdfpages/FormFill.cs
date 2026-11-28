using System.Runtime.CompilerServices;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Fill in the form": a PDF with fields (an official form, an application) asked as boxes to type in, ticks and
	/// choices, then saved as a filled copy ("… (filled).pdf"); the original stays empty. Offered first for such a PDF.
	/// </summary>
	internal static class FormFill
	{
		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "fillform", Emoji = "🖊️",
				Rank = f => f.Count == 1 && File.Exists(f[0]) && IsPdf(f[0]) && PdfLock.Of(f[0]) == PdfLock.State.Open && HasFields(f[0]) ? 8 : -1,
				Ask = (owner, files) => AskFields(owner, files[0]),
				Run = c => c.Each((file, i) =>
				{
					string o = OutName(file, ".pdf", T.S("form.name"));
					Fill(file, o, c.Options);
					c.Outputs.Add(o);
					return Task.CompletedTask;
				}),
			});
		}

		static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime when, long size, bool has)> known = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>Has the PDF fields to fill? (Read once per file, again only when it changed: the jobs' list asks often.)</summary>
		internal static bool HasFields(string pdf)
		{
			var info = new FileInfo(pdf);
			if (!info.Exists) return false;
			if (known.TryGetValue(info.FullName, out var k) && k.when == info.LastWriteTimeUtc && k.size == info.Length) return k.has;
			bool has = Fields(pdf).Count > 0;
			known[info.FullName] = (info.LastWriteTimeUtc, info.Length, has);
			return has;
		}

		/// <summary>One field: its full name, the text shown for it, its kind, its value now, its choices.</summary>
		internal sealed record Field(string Name, string Label, string Kind, string Value, string[] Choices);

		/// <summary>The fields a person can fill (text, tick, choice), in the order of the form. None when it has no form or cannot be read.</summary>
		internal static List<Field> Fields(string pdf)
		{
			var list = new List<Field>();
			try
			{
				using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
				var form = doc.AcroForm;
				if (form == null) return list;
				foreach (string name in form.Fields.DescendantNames)
				{
					var f = form.Fields[name];
					if (f == null || f.ReadOnly) continue;
					string label = Label(f, name);
					switch (f)
					{
						case PdfTextField t:
							list.Add(new Field(name, label, "text", t.Text ?? "", null));
							break;
						case PdfCheckBoxField cb:
							list.Add(new Field(name, label, "check", cb.Checked ? "1" : "0", null));
							break;
						case PdfComboBoxField or PdfListBoxField:
							list.Add(new Field(name, label, "choice", ValueOf(f), Options(f)));
							break;
						case PdfRadioButtonField:
							var states = RadioStates(f);
							if (states.Length > 0) list.Add(new Field(name, label, "radio", ValueOf(f).TrimStart('/'), states));
							break;
					}
				}
			}
			catch (Exception) { list.Clear(); }
			return list;
		}

		/// <summary>What the form calls the field for people (its tooltip), else the last part of its name made readable.</summary>
		static string Label(PdfAcroField f, string name)
		{
			string tu = f.Elements.GetString("/TU");
			if (!string.IsNullOrWhiteSpace(tu)) return tu.Trim();
			string last = name.Split('.').Last();
			int b = last.IndexOf('[');
			if (b > 0) last = last[..b];
			return last.Replace('_', ' ').Trim();
		}

		static string ValueOf(PdfAcroField f)
		{
			var v = f.Elements["/V"];
			return v switch { PdfString s => s.Value, PdfName n => n.Value, null => "", _ => v.ToString() };
		}

		/// <summary>The choices of a list ("/Opt": texts, or [export, text] pairs).</summary>
		static string[] Options(PdfAcroField f)
		{
			var opt = f.Elements.GetArray("/Opt");
			if (opt == null) return Array.Empty<string>();
			return opt.Elements.Select(e => e is PdfArray pair && pair.Elements.Count > 0 ? pair.Elements.GetString(0) : e is PdfString s ? s.Value : e?.ToString() ?? "").Where(s => s.Length > 0).ToArray();
		}

		/// <summary>The choices of a group of round buttons: the names of their "on" states.</summary>
		static string[] RadioStates(PdfAcroField f)
		{
			var names = new List<string>();
			var kids = f.Elements.GetArray("/Kids");
			if (kids == null) return Array.Empty<string>();
			foreach (var item in kids.Elements)
			{
				var kid = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
				var ap = kid?.Elements.GetDictionary("/AP")?.Elements.GetDictionary("/N");
				if (ap == null) continue;
				foreach (var key in ap.Elements.Keys) if (key != "/Off" && !names.Contains(key.TrimStart('/'))) names.Add(key.TrimStart('/'));
			}
			return names.ToArray();
		}

		static Dictionary<string, string> AskFields(IWin32Window owner, string pdf)
		{
			var fields = Fields(pdf);
			if (fields.Count == 0) throw new InvalidOperationException(T.S("form.none"));
			var asked = fields.Select((f, i) => new Ask.Field
			{
				Key = "f" + i,
				Label = f.Label,
				Kind = f.Kind switch { "check" => Ask.Kind.Check, "choice" or "radio" => Ask.Kind.Choice, _ => Ask.Kind.Text },
				Value = f.Value,
				Choices = f.Choices?.Select(c => (c, c)).Prepend(("", "—")).ToArray(),
			}).ToArray();
			var a = Ask.Show(owner, T.S("job.fillform"), T.S("form.intro", fields.Count), asked);
			if (a == null) return null;
			// the answers by the fields' own names (so the job can also be given them on the command line)
			var named = new Dictionary<string, string>();
			for (int i = 0; i < fields.Count; i++) if (a.TryGetValue("f" + i, out var v)) named["field:" + fields[i].Name] = v;
			return named;
		}

		/// <summary>The filled copy: each "field:&lt;name&gt;" answer in its field; the viewers draw the values again (NeedAppearances).</summary>
		internal static void Fill(string pdf, string output, Dictionary<string, string> answers)
		{
			using var doc = PdfReader.Open(pdf, PdfDocumentOpenMode.Modify);
			var form = doc.AcroForm ?? throw new InvalidOperationException(T.S("form.none"));
			foreach (var (key, value) in answers)
			{
				if (!key.StartsWith("field:")) continue;
				var f = form.Fields[key[6..]];
				if (f == null || f.ReadOnly) continue;
				switch (f)
				{
					case PdfTextField t: t.Text = value ?? ""; break;
					case PdfCheckBoxField cb: cb.Checked = value == "1" || value == "true"; break;
					case PdfComboBoxField or PdfListBoxField:
						if (!string.IsNullOrEmpty(value)) f.Elements.SetValue("/V", new PdfString(value));
						break;
					case PdfRadioButtonField:
						if (string.IsNullOrEmpty(value)) break;
						f.Elements.SetName("/V", "/" + value);
						// each round button shows "on" only when it is the one chosen
						var kids = f.Elements.GetArray("/Kids");
						if (kids != null)
							foreach (var item in kids.Elements)
							{
								var kid = (item as PdfReference)?.Value as PdfDictionary ?? item as PdfDictionary;
								var states = kid?.Elements.GetDictionary("/AP")?.Elements.GetDictionary("/N");
								if (kid == null || states == null) continue;
								kid.Elements.SetName("/AS", states.Elements.ContainsKey("/" + value) ? "/" + value : "/Off");
							}
						break;
				}
			}
			form.Elements.SetBoolean("/NeedAppearances", true);
			doc.Save(output);
		}
	}
}
