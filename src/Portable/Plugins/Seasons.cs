using System;
using System.Globalization;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== special dates

	/// <summary>
	/// Off unless turned on. On special dates every pet wears the date's hat (a Santa hat in December, a witch's hat at
	/// Halloween, a straw hat in June for the Festa Junina, bunny ears at Easter, feathers at Carnival, a party hat on New
	/// Year's Eve and on your birthday), and on the day itself the pet says something once and the screen celebrates
	/// (snow at Christmas, confetti at New Year, Carnival and the birthday, leaves at Halloween).
	/// </summary>
	public static class Seasons
	{
		public enum Day { None, Christmas, NewYear, Halloween, Carnival, Junina, Easter, Birthday }

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("Seasons", false);
			set { Program.MyData.SetSetting("Seasons", value); Apply(); }
		}

		/// <summary>The pets wear the date's hat.</summary>
		public static bool Hats
		{
			get => Program.MyData.GetSettingBool("SeasonHats", true);
			set { Program.MyData.SetSetting("SeasonHats", value); PetHats.Apply(); }
		}

		/// <summary>Snow, confetti or leaves on the screen on the day itself.</summary>
		public static bool Effects
		{
			get => Program.MyData.GetSettingBool("SeasonEffects", true);
			set => Program.MyData.SetSetting("SeasonEffects", value);
		}

		/// <summary>The user's birthday, "dd/MM" ("" = not told).</summary>
		public static string Birthday
		{
			get => Program.MyData.GetSetting("Birthday");
			set => Program.MyData.SetSetting("Birthday", value ?? "");
		}

		/// <summary>"25/12", "25-12", "25.12" → (25, 12); null when it is not a day of the year.</summary>
		public static (int day, int month)? ParseBirthday(string text)
		{
			var parts = (text ?? "").Trim().Split('/', '-', '.', ' ');
			if (parts.Length < 2 || !int.TryParse(parts[0], out int d) || !int.TryParse(parts[1], out int m)) return null;
			if (m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(2024, m)) return null;
			return (d, m);
		}

		/// <summary>Easter Sunday (the Gregorian computus).</summary>
		public static DateTime Easter(int year)
		{
			int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
			int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451;
			int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
			return new DateTime(year, month, day);
		}

		/// <summary>The date whose hat is worn on this day (the period around it), the most special first.</summary>
		public static Day HatDay(DateTime t)
		{
			t = t.Date;
			var b = ParseBirthday(Birthday);
			if (b != null && t.Day == b.Value.day && t.Month == b.Value.month) return Day.Birthday;
			if ((t.Month == 12 && t.Day == 31) || (t.Month == 1 && t.Day == 1)) return Day.NewYear;
			if (t.Month == 12 && t.Day <= 26) return Day.Christmas;
			var easter = Easter(t.Year);
			if (t >= easter.AddDays(-2) && t <= easter) return Day.Easter;
			if (t >= easter.AddDays(-50) && t <= easter.AddDays(-46)) return Day.Carnival;   // Saturday to Ash Wednesday
			if (t.Month == 10 && t.Day >= 25) return Day.Halloween;
			if (t.Month == 6) return Day.Junina;
			return Day.None;
		}

		/// <summary>The day itself, when the pet says something and the screen celebrates.</summary>
		public static Day Celebration(DateTime t)
		{
			t = t.Date;
			var day = HatDay(t);
			switch (day)
			{
				case Day.Christmas: return t.Day == 24 || t.Day == 25 ? day : Day.None;
				case Day.Halloween: return t.Day == 31 ? day : Day.None;
				case Day.Junina: return t.Day == 24 ? day : Day.None;
				case Day.Easter: return t == Easter(t.Year) ? day : Day.None;
				case Day.Carnival: return t == Easter(t.Year).AddDays(-47) ? day : Day.None;   // Carnival Tuesday
				default: return day;   // New Year, birthday
			}
		}

		public static string HatOf(Day d) => d switch
		{
			Day.Christmas => "santa", Day.Halloween => "witch", Day.Junina => "straw", Day.Easter => "bunny",
			Day.Carnival => "carnival", Day.NewYear => "party", Day.Birthday => "party", _ => null,
		};

		public static ScreenEffects.Kind? EffectOf(Day d) => d switch
		{
			Day.Christmas => ScreenEffects.Kind.Snow, Day.Halloween => ScreenEffects.Kind.Leaves,
			Day.NewYear or Day.Carnival or Day.Birthday or Day.Junina => ScreenEffects.Kind.Confetti, _ => (ScreenEffects.Kind?)null,
		};

		static Day preview = Day.None;
		static DateTime previewUntil;
		/// <summary>Self-test: the day it is, instead of today.</summary>
		internal static DateTime? TestToday;
		static DateTime Today => TestToday ?? DateTime.Now;

		/// <summary>The hat of now ("" none): a preview, or the date's hat when the plugin and its hats are on.</summary>
		public static string HatNow()
		{
			if (preview != Day.None && DateTime.Now < previewUntil) return HatOf(preview);
			return Enabled && Hats ? HatOf(HatDay(Today)) : null;
		}

		static Timer timer;

		public static void Apply()
		{
			if (timer == null) { timer = new Timer { Interval = 5 * 60 * 1000 }; timer.Tick += (s, e) => Check(); }
			if (Enabled) { timer.Start(); Check(); } else timer.Stop();
			PetHats.Apply();
		}

		/// <summary>Every few minutes: the hat of the date, and once on the day itself the greeting and the celebration.</summary>
		static void Check()
		{
			PetHats.Apply();
			if (!Enabled) return;
			var now = DateTime.Now;
			var day = Celebration(Today);
			if (day == Day.None || now.Hour < 8 || now.Hour >= 23 || SpeechBubble.QuietNow) return;
			string mark = Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " " + day;
			if (Program.MyData.GetSetting("SeasonSaid") == mark) return;
			Program.MyData.SetSetting("SeasonSaid", mark);
			Celebrate(day);
		}

		/// <summary>The greeting, the dance and the effect of a date.</summary>
		static void Celebrate(Day day)
		{
			Program.Mainthread?.ReactAll("party", "cheer", "dance", "happy", "wave");
			SpeechBubble.Notify(UserProfile.Greet(Lang.T("season." + day.ToString().ToLowerInvariant())), 12);
			var fx = EffectOf(day);
			if (Effects && fx != null) ScreenEffects.Start(fx.Value, 60, true);
		}

		/// <summary>"Try it": a date shown now for a minute (its hat, greeting and effect).</summary>
		public static void Preview(Day day)
		{
			preview = day;
			previewUntil = DateTime.Now.AddMinutes(1);
			PetHats.Apply();
			Celebrate(day);
			var t = new Timer { Interval = 61000 };
			t.Tick += (s, e) => { t.Stop(); t.Dispose(); PetHats.Apply(); };
			t.Start();
		}
	}
}
