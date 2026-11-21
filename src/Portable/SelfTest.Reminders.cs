using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// The self-test, part: reminders. Check, Try and the list of tests are in SelfTest.cs.
	internal static partial class SelfTest
	{
		static void Reminders_Next()
		{
			// a fixed Wednesday, so the days of the week are known
			var wed = new DateTime(2026, 11, 11);
			var fri = new DateTime(2026, 11, 13);
			var r = new Reminder { Kind = Reminder.Kinds.Interval, Minutes = 60, FromHour = 9, ToHour = 18, Days = 127 };
			Check("interval: +60 min inside hours", r.Next(wed.AddHours(10)) == wed.AddHours(11), r.Next(wed.AddHours(10)).ToString());
			Check("interval: after hours → next day 9h", r.Next(wed.AddHours(17.5)) == wed.AddDays(1).AddHours(9), r.Next(wed.AddHours(17.5)).ToString());
			r.Days = 62;   // Mon–Fri
			Check("interval: Friday evening → Monday 9h", r.Next(fri.AddHours(17.5)) == fri.AddDays(3).AddHours(9), r.Next(fri.AddHours(17.5)).ToString());
			var night = new Reminder { Kind = Reminder.Kinds.Interval, Minutes = 60, FromHour = 22, ToHour = 6, Days = 127 };
			Check("interval: over midnight", night.Next(wed.AddHours(23)) == wed.AddDays(1), night.Next(wed.AddHours(23)).ToString());
			var allDay = new Reminder { Kind = Reminder.Kinds.Interval, Minutes = 30, FromHour = 0, ToHour = 0, Days = 127 };
			Check("interval: 0h–0h is all day", allDay.Next(wed.AddHours(3)) == wed.AddHours(3.5), allDay.Next(wed.AddHours(3)).ToString());

			var daily = new Reminder { Kind = Reminder.Kinds.Daily, Time = "12:00", Days = 62 };
			Check("daily: later today", daily.Next(wed.AddHours(11)) == wed.AddHours(12));
			daily.LastFired = wed.AddHours(12);
			Check("daily: fired today → tomorrow", daily.Next(wed.AddHours(12)) == wed.AddDays(1).AddHours(12), daily.Next(wed.AddHours(12)).ToString());
			daily.LastFired = DateTime.MinValue;
			Check("daily: Saturday → Monday", daily.Next(fri.AddDays(1).AddHours(8)) == fri.AddDays(3).AddHours(12), daily.Next(fri.AddDays(1).AddHours(8)).ToString());

			var once = new Reminder { Kind = Reminder.Kinds.Once, At = wed.AddHours(15) };
			Check("once: due at its time", once.Next(DateTime.MinValue) == wed.AddHours(15));
			once.LastFired = wed.AddHours(15);
			Check("once: never again after firing", once.Next(DateTime.MinValue) == null);

			var snoozed = new Reminder { Kind = Reminder.Kinds.Interval, Minutes = 60, Days = 127, FromHour = 0, ToHour = 0, SnoozedUntil = wed.AddHours(10.25) };
			Check("snooze wins", snoozed.Next(wed.AddHours(10)) == wed.AddHours(10.25));
			Check("disabled → never", new Reminder { Enabled = false }.Next(wed) == null);
			Check("summary text", !string.IsNullOrEmpty(r.Summary()) && !r.Summary().StartsWith("rem."), r.Summary());
			// the pet acts out the reminder, read from its words
			Check("animation: water → drink", Reminders.AnimationsFor("Hora de beber água 💧")?[0] == "drink");
			Check("animation: stretch → stretch", Reminders.AnimationsFor("Time to stretch")?[0] == "stretch");
			Check("animation: eyes → look around", Reminders.AnimationsFor("Descanse os olhos")?[0] == "look around");
			Check("animation: other text → none", Reminders.AnimationsFor("Ligar para a Ana") == null);
			Check("animation: only whole words", Reminders.AnimationsFor("Aguardar o relatório e create the report") == null);
		}

		static void Reminders_Json()
		{
			var list = new List<Reminder> { new Reminder { Text = "água", Kind = Reminder.Kinds.Daily, Time = "08:30", Days = 65, At = new DateTime(2026, 12, 1, 10, 0, 0) } };
			var back = JsonText.Deserialize<List<Reminder>>(JsonText.SerializeIndented(list));
			Check("reminder survives save/load", back.Count == 1 && back[0].Text == "água" && back[0].Kind == Reminder.Kinds.Daily
				&& back[0].Time == "08:30" && back[0].Days == 65 && back[0].At == list[0].At);
		}

		// ------------------------------------------------------------------ virtual pet

		/// <summary>Reminders written as a sentence, read on this computer (fixed "now": Wednesday 14:10).</summary>
		static void SmartReminders()
		{
			var now = new DateTime(2026, 11, 11, 14, 10, 0);
			void T(string sentence, Reminder.Kinds kind, string when, string text)
			{
				var r = SmartReminder.Parse(sentence, now);
				string got = r == null ? "(not understood)"
					: r.Kind + " " + (r.Kind == Reminder.Kinds.Once ? r.At.ToString("dd HH:mm") : r.Kind == Reminder.Kinds.Daily ? r.Time : r.Minutes + "min") + " \"" + r.Text + "\"";
				string want = kind + " " + when + " \"" + text + "\"";
				Check("smart reminder: " + sentence, got == want, got + "  (wanted " + want + ")");
			}
			T("me lembra de ligar para a Ana às 15h", Reminder.Kinds.Once, "11 15:00", "Ligar para a Ana");
			T("Me lembre daqui a 20 minutos de tirar o bolo do forno", Reminder.Kinds.Once, "11 14:30", "Tirar o bolo do forno");
			T("lembrete amanhã às 9:30 reunião com o time", Reminder.Kinds.Once, "12 09:30", "Reunião com o time");
			T("me lembra às 3 de buscar as crianças", Reminder.Kinds.Once, "11 15:00", "Buscar as crianças");
			T("me lembra às 10 de pagar a conta", Reminder.Kinds.Once, "11 22:00", "Pagar a conta");
			T("todo dia às 8 tomar o remédio", Reminder.Kinds.Daily, "08:00", "Tomar o remédio");
			T("a cada 30 minutos me lembra de alongar", Reminder.Kinds.Interval, "30min", "Alongar");
			T("me lembra em meia hora de beber água", Reminder.Kinds.Once, "11 14:40", "Beber água");
			T("remind me to call mom at 6pm", Reminder.Kinds.Once, "11 18:00", "Call mom");
			T("remind me in 2 hours to stretch", Reminder.Kinds.Once, "11 16:10", "Stretch");
			T("recuérdame mañana a las 8 llamar al médico", Reminder.Kinds.Once, "12 08:00", "Llamar al médico");
			Check("smart reminder: no time, not understood", SmartReminder.Parse("me lembra de comprar pão", now) == null);
			Check("smart reminder: a question is not a reminder", !SmartReminder.LooksLikeReminder("por que o céu é azul?") && SmartReminder.LooksLikeReminder("me lembra de comprar pão às 18h"));
		}

		static void AgendaParse()
		{
			string ics = string.Join("\r\n", new[]
			{
				"BEGIN:VCALENDAR", "VERSION:2.0",
				"BEGIN:VEVENT", "UID:utc", "DTSTART:20261116T130000Z", "SUMMARY:Café\\, conversa", "END:VEVENT",
				"BEGIN:VEVENT", "UID:tz", "DTSTART;TZID=America/Sao_Paulo:20261117T100000", "SUMMARY:Reunião com", "  o time", "END:VEVENT",
				"BEGIN:VEVENT", "UID:allday", "DTSTART;VALUE=DATE:20261118", "SUMMARY:Feriado", "END:VEVENT",
				"BEGIN:VEVENT", "UID:weekly", "DTSTART:20261102T090000", "RRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR", "EXDATE:20261118T090000", "SUMMARY:Daily", "END:VEVENT",
				"BEGIN:VEVENT", "UID:weekly", "RECURRENCE-ID:20261120T090000", "DTSTART:20261120T110000", "SUMMARY:Daily (movida)", "END:VEVENT",
				"BEGIN:VEVENT", "UID:cancel", "DTSTART:20261119T150000", "STATUS:CANCELLED", "SUMMARY:Cancelada", "END:VEVENT",
				"BEGIN:VEVENT", "UID:count", "DTSTART:20261115T080000", "RRULE:FREQ=DAILY;COUNT=2", "SUMMARY:Duas vezes", "END:VEVENT",
				"END:VCALENDAR",
			});
			var list = Agenda.Parse(ics, new DateTime(2026, 11, 16), new DateTime(2026, 11, 23));
			string all = string.Join(" | ", list.Select(e => e.Start.ToString("ddd dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) + " " + e.Title));
			Check("agenda: 5 events (all-day, cancelled, removed and moved ones handled)", list.Count == 5, all);
			Check("agenda: UTC time converted", list.Any(e => e.Uid == "utc" && e.Start == new DateTime(2026, 11, 16, 13, 0, 0, DateTimeKind.Utc).ToLocalTime()), all);
			var sp = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
			Check("agenda: time zone converted", list.Any(e => e.Uid == "tz" && e.Start == TimeZoneInfo.ConvertTime(new DateTime(2026, 11, 17, 10, 0, 0), sp, TimeZoneInfo.Local)), all);
			Check("agenda: long line joined, escapes removed", list.Any(e => e.Title == "Reunião com o time") && list.Any(e => e.Title == "Café, conversa"), all);
			Check("agenda: weekly repeat, removed day skipped", list.Count(e => e.Uid == "weekly") == 2 && list.Any(e => e.Uid == "weekly" && e.Start == new DateTime(2026, 11, 16, 9, 0, 0)), all);
			Check("agenda: moved occurrence at its new time", list.Any(e => e.Title == "Daily (movida)" && e.Start.Hour == 11) && !list.Any(e => e.Uid == "weekly" && e.Start == new DateTime(2026, 11, 20, 9, 0, 0)), all);
			Check("agenda: COUNT respected", list.Count(e => e.Uid == "count") == 1, all);
			Check("agenda: no all-day or cancelled events", !list.Any(e => e.Uid == "allday" || e.Uid == "cancel"), all);
		}

		static async Task AgendaOnline()
		{
			// a real public Google calendar (Brazilian holidays): only all-day events, so nothing to warn about
			const string url = "https://calendar.google.com/calendar/ical/pt.brazilian%23holiday%40group.v.calendar.google.com/public/basic.ics";
			try
			{
				var list = await Agenda.Load(url);
				Check("agenda online: a real Google calendar is read", list != null && list.All(e => e.Title.Length > 0), list?.Count.ToString());
			}
			catch (System.Net.Http.HttpRequestException ex) { lines.Add("SKIP  agenda online: no internet (" + ex.Message + ")"); }
			bool refused = false;
			try { await Agenda.Load("http://example.com/cal.ics"); } catch (ArgumentException) { refused = true; }
			Check("agenda: plain http refused", refused);
		}
	}
}
