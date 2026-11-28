using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopPet
{
	/// <summary>
	/// How lively the pets are, for any pet, without touching its XML (a user of desktopPet asked: "I want the sheep to
	/// sleep 95% of the time, so it does not catch my eye"). At the end of each animation the pet draws its next one by the
	/// chances its maker gave; here the restful ones (sleep, sit, rest, idle…) weigh more: 1× (normal), 4×, 12× or 40×
	/// (sleepyhead). Only animations the maker put as next are drawn, so the pet still lies down before sleeping and wakes up
	/// properly. "Sleep together": when a pet falls asleep while other pets are on screen, for a minute and a half their
	/// sleep animations weigh a lot more, so they tend to doze off too (as the sheep of the original eSheep did).
	/// </summary>
	public static class PetCalm
	{
		/// <summary>0 normal, 1 calm, 2 very calm, 3 sleepyhead.</summary>
		public static int Level
		{
			get => Math.Max(0, Math.Min(3, Program.MyData?.GetSettingInt("PetCalm", 0) ?? 0));
			set => Program.MyData.SetSetting("PetCalm", Math.Max(0, Math.Min(3, value)));
		}

		/// <summary>When one pet falls asleep, the others tend to sleep too (on by default).</summary>
		public static bool SleepTogether
		{
			get => Program.MyData?.GetSettingBool("SleepTogether", true) ?? true;
			set => Program.MyData.SetSetting("SleepTogether", value);
		}

		static readonly int[] Factor = { 1, 4, 12, 40 };
		static readonly Regex Restful = new Regex(@"sleep|sit|rest|idle|nap|lay|lie|yawn|relax|dream|zzz|bored|wait|tired|snooze|doze", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		static readonly Regex Sleeping = new Regex(@"sleep|nap|zzz|snooze|doze|dream", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public static bool IsRestful(string name) => !string.IsNullOrEmpty(name) && Restful.IsMatch(name);
		public static bool IsSleep(string name) => !string.IsNullOrEmpty(name) && Sleeping.IsMatch(name);

		/// <summary>Until when the pets tend to fall asleep together.</summary>
		static DateTime togetherUntil = DateTime.MinValue;
		/// <summary>Self-test: the clock.</summary>
		internal static Func<DateTime> Now = () => DateTime.Now;

		/// <summary>How much more this next animation weighs now (1 = as its maker set).</summary>
		public static int Weight(string name)
		{
			int w = 1;
			if (IsRestful(name)) w = Factor[Level];
			if (IsSleep(name) && Now() < togetherUntil) w = Math.Max(w, 30);
			return w;
		}

		/// <summary>A pet started an animation: when it is sleep and there are other pets, the others get sleepy for a while.</summary>
		public static void Started(string name, bool mainPet, int petsOnScreen)
		{
			if (!SleepTogether || !mainPet || petsOnScreen < 2 || !IsSleep(name)) return;
			// only a new nap starts it (pets falling asleep during it do not make it longer: they all wake up again)
			if (Now() < togetherUntil) return;
			togetherUntil = Now().AddSeconds(90);
			StartUp.AddDebugInfo(StartUp.DEBUG_TYPE.info, "a pet fell asleep: the others get sleepy");
		}

		/// <summary>Self-test: no nap going on.</summary>
		internal static void TestReset() => togetherUntil = DateTime.MinValue;
	}
}
