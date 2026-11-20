using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopPet
{
	/// <summary>
	/// How the pet calls the person (a name or a nickname, chosen in the first setup or in Settings). Used in the
	/// greetings ("Oi, Luan!"), in the chat and in what the AI is told. Empty = the pet says no name.
	/// </summary>
	public static class UserProfile
	{
		public const int MaxLength = 30;

		public static string Name
		{
			get => Program.MyData.GetSetting("UserName").Trim();
			set
			{
				string v = Regex.Replace(value ?? "", @"\s+", " ").Trim();
				Program.MyData.SetSetting("UserName", v.Length > MaxLength ? v.Substring(0, MaxLength).Trim() : v);
			}
		}

		/// <summary>A greeting at the start of a text ("Oi!", "Bom dia!", "Hi!", "Hola!").</summary>
		static readonly Regex Hello = new Regex(@"^(Oi|Olá|Ola|Bom dia|Boa tarde|Boa noite|Hi|Hello|Hey|Good morning|Good afternoon|Good evening|Hola|Buenos días|Buenas tardes|Buenas noches)([!.,])",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		/// <summary>"Oi! Eu sou Rex." → "Oi, Luan! Eu sou Rex." (unchanged without a name or a greeting).</summary>
		public static string Greet(string text)
		{
			string n = Name;
			if (n.Length == 0 || string.IsNullOrEmpty(text)) return text;
			var m = Hello.Match(text);
			if (!m.Success) return text;
			string mark = m.Groups[2].Value == "," ? "!" : m.Groups[2].Value;
			return m.Groups[1].Value + ", " + n + mark + text.Substring(m.Length);
		}

		/// <summary>A first name to suggest: the first word of the Windows user, when it looks like a name ("Luan Felipe" → "Luan").</summary>
		public static string Suggestion()
		{
			string first = (Environment.UserName ?? "").Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
			if (first.Length < 2 || !first.All(char.IsLetter)) return "";
			return char.ToUpper(first[0]) + first.Substring(1).ToLower();
		}
	}
}
