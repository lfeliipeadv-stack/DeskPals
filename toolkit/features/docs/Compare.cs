using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using static DeskPalsTools.Jobs;

namespace DeskPalsTools
{
	/// <summary>
	/// "Compare": two versions of a document (PDF, Word, text) side by side in one page that opens in the browser: what
	/// was taken out struck through in red, what was put in underlined in green, the rest as it is. Paragraphs are
	/// matched first, then the words inside the ones that changed, so a long document is quick.
	/// </summary>
	internal static class Compare
	{
		[ModuleInitializer]
		internal static void Init()
		{
			Register(new JobDef
			{
				Id = "compare", Emoji = "⚖️",
				Rank = f => f.Count == 2 && f.All(x => File.Exists(x) && AiText.CanRead(x)) ? 13 : -1,
				Run = Run,
			});
		}

		static async Task Run(JobContext c)
		{
			string a = c.Files[0], b = c.Files[1];
			c.Report(0, Path.GetFileName(a));
			string ta = await AiText.Read(a, (p, s) => c.Report(p * 0.4, s), c.Cancel);
			c.Report(0.4, Path.GetFileName(b));
			string tb = await AiText.Read(b, (p, s) => c.Report(0.4 + p * 0.4, s), c.Cancel);
			c.Report(0.85, T.S("compare.doing"));
			var parts = await Task.Run(() => Diff(ta, tb), c.Cancel);
			int removed = parts.Where(p => p.kind < 0).Sum(p => Words(p.text)), added = parts.Where(p => p.kind > 0).Sum(p => Words(p.text));
			string o = OutName(b, ".html", T.S("compare.name"));
			await File.WriteAllTextAsync(o, Html(Path.GetFileName(a), Path.GetFileName(b), parts, removed, added), new UTF8Encoding(false), c.Cancel);
			c.Outputs.Add(o);
			c.Preview = removed + added == 0 ? T.S("compare.same") : T.S("compare.summary", removed, added);
		}

		static int Words(string s) => Regex.Matches(s, @"\w+").Count;

		/// <summary>The two texts as parts: (0 the same, -1 only in the first, +1 only in the second, text).</summary>
		internal static List<(int kind, string text)> Diff(string first, string second)
		{
			var pa = Paragraphs(first);
			var pb = Paragraphs(second);
			var result = new List<(int, string)>();
			var ops = Myers(pa.Select(Norm).ToList(), pb.Select(Norm).ToList());
			// paragraphs taken out and put in next to each other are compared word by word
			var gone = new List<string>();
			var come = new List<string>();
			void Flush()
			{
				if (gone.Count > 0 && come.Count > 0) result.AddRange(Words(string.Join("\n\n", gone), string.Join("\n\n", come)));
				else
				{
					foreach (var g in gone) result.Add((-1, g + "\n\n"));
					foreach (var n in come) result.Add((1, n + "\n\n"));
				}
				if (gone.Count + come.Count > 0 && gone.Count > 0 && come.Count > 0) result.Add((0, "\n\n"));
				gone.Clear(); come.Clear();
			}
			foreach (var (kind, ia, ib) in ops)
			{
				if (kind == 0) { Flush(); result.Add((0, pb[ib] + "\n\n")); }
				else if (kind < 0) gone.Add(pa[ia]);
				else come.Add(pb[ib]);
			}
			Flush();
			return Merge(result);
		}

		static List<(int, string)> Words(string a, string b)
		{
			var ta = Tokens(a);
			var tb = Tokens(b);
			var list = new List<(int, string)>();
			foreach (var (kind, ia, ib) in Myers(ta.Select(Norm).ToList(), tb.Select(Norm).ToList()))
				list.Add((kind, kind < 0 ? ta[ia] : tb[ib]));
			return list;
		}

		/// <summary>Neighbouring parts of the same kind joined (fewer marks in the page).</summary>
		static List<(int kind, string text)> Merge(List<(int kind, string text)> parts)
		{
			var list = new List<(int kind, string text)>();
			foreach (var p in parts)
			{
				if (list.Count > 0 && list[^1].kind == p.kind) list[^1] = (p.kind, list[^1].text + p.text);
				else list.Add(p);
			}
			return list;
		}

		static List<string> Paragraphs(string s) => Regex.Split((s ?? "").Replace("\r\n", "\n"), @"\n\s*\n").Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

		/// <summary>Words, punctuation and spaces, each its own piece (spaces are kept to rebuild the text).</summary>
		static List<string> Tokens(string s) => Regex.Matches(s, @"\w+|\s+|[^\w\s]").Select(m => m.Value).ToList();

		/// <summary>What is compared: spaces of any kind are the same; a line break inside a paragraph is a space.</summary>
		static string Norm(string s) => string.IsNullOrWhiteSpace(s) ? " " : Regex.Replace(s.Trim(), @"\s+", " ");

		/// <summary>
		/// The shortest list of changes between two sequences (Myers' algorithm): (0 kept, -1 removed, +1 added) with the
		/// index in the first and in the second. Very different long texts are compared in a simpler way (all removed,
		/// all added) rather than taking minutes.
		/// </summary>
		internal static List<(int kind, int a, int b)> Myers(List<string> a, List<string> b)
		{
			int n = a.Count, m = b.Count, max = n + m;
			var trace = new List<int[]>();
			var v = new int[2 * max + 2];
			int offset = max + 1;
			int limit = Math.Min(max, 6000);
			for (int d = 0; d <= limit; d++)
			{
				trace.Add((int[])v.Clone());
				for (int k = -d; k <= d; k += 2)
				{
					int x = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? v[offset + k + 1] : v[offset + k - 1] + 1;
					int y = x - k;
					while (x < n && y < m && a[x] == b[y]) { x++; y++; }
					v[offset + k] = x;
					if (x >= n && y >= m) return Back(trace, a, b, offset, d);
				}
				// the copies of the table grow with every step: a huge difference stops here
				if ((long)trace.Count * v.Length > 60_000_000) break;
			}
			var all = new List<(int, int, int)>();
			for (int i = 0; i < n; i++) all.Add((-1, i, -1));
			for (int j = 0; j < m; j++) all.Add((1, -1, j));
			return all;
		}

		static List<(int, int, int)> Back(List<int[]> trace, List<string> a, List<string> b, int offset, int dEnd)
		{
			var ops = new List<(int, int, int)>();
			int x = a.Count, y = b.Count;
			for (int d = dEnd; d >= 0; d--)
			{
				var v = trace[d];
				int k = x - y;
				int prevK = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]) ? k + 1 : k - 1;
				int prevX = d == 0 ? 0 : v[offset + prevK], prevY = prevX - prevK;
				while (x > prevX && y > prevY) { ops.Add((0, x - 1, y - 1)); x--; y--; }
				if (d > 0)
				{
					if (x == prevX) ops.Add((1, -1, y - 1));
					else ops.Add((-1, x - 1, -1));
				}
				x = prevX; y = prevY;
			}
			ops.Reverse();
			return ops;
		}

		static string Html(string nameA, string nameB, List<(int kind, string text)> parts, int removed, int added)
		{
			var sb = new StringBuilder();
			sb.Append("<!doctype html><html lang=\"").Append(T.Code).Append("\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>")
				.Append(WebUtility.HtmlEncode(T.S("compare.title"))).Append("</title><style>")
				.Append(":root{color-scheme:light dark;--bg:#fafaf7;--fg:#1f2328;--mut:#6b7280;--del:#fde2e2;--delfg:#a40e26;--ins:#dcf5e3;--insfg:#116329;--card:#fff;--line:#e5e7eb}")
				.Append("@media(prefers-color-scheme:dark){:root{--bg:#16181d;--fg:#e6e6e6;--mut:#9aa0a6;--del:#4a1d22;--delfg:#ffb3b8;--ins:#163a24;--insfg:#9ff0b5;--card:#1e2128;--line:#2c3038}}")
				.Append("body{margin:0;background:var(--bg);color:var(--fg);font:16px/1.6 'Segoe UI',system-ui,sans-serif}main{max-width:860px;margin:0 auto;padding:24px 16px 64px}")
				.Append("h1{font-size:22px;margin:0 0 4px}.files{color:var(--mut);margin:0 0 16px}.sum{display:flex;gap:12px;flex-wrap:wrap;margin:0 0 20px}")
				.Append(".chip{padding:4px 12px;border-radius:999px;font-weight:600}.chip.d{background:var(--del);color:var(--delfg)}.chip.i{background:var(--ins);color:var(--insfg)}")
				.Append(".doc{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:20px 24px;white-space:pre-wrap;word-wrap:break-word}")
				.Append("del{background:var(--del);color:var(--delfg);text-decoration:line-through;border-radius:3px}ins{background:var(--ins);color:var(--insfg);text-decoration:underline;border-radius:3px}")
				.Append("</style></head><body><main><h1>⚖️ ").Append(WebUtility.HtmlEncode(T.S("compare.title"))).Append("</h1><p class=\"files\">")
				.Append(WebUtility.HtmlEncode(nameA)).Append(" → ").Append(WebUtility.HtmlEncode(nameB)).Append("</p><div class=\"sum\">")
				.Append("<span class=\"chip d\">− ").Append(WebUtility.HtmlEncode(T.S("compare.removed", removed))).Append("</span>")
				.Append("<span class=\"chip i\">+ ").Append(WebUtility.HtmlEncode(T.S("compare.added", added))).Append("</span></div><div class=\"doc\">");
			foreach (var (kind, text) in parts)
			{
				string t = WebUtility.HtmlEncode(text);
				sb.Append(kind < 0 ? "<del>" + t + "</del>" : kind > 0 ? "<ins>" + t + "</ins>" : t);
			}
			return sb.Append("</div></main></body></html>").ToString();
		}
	}
}
