using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	// ====================================================================== typing along

	/// <summary>
	/// Off unless turned on. While you type, the pet "types" too (its laptop or typing animation), and it counts the
	/// keys pressed today. Only the number is kept: which keys were pressed is never read or saved.
	/// </summary>
	public static class TypingPet
	{
		const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x0100, WM_SYSKEYDOWN = 0x0104;
		delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
		[DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
		[DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);

		static IntPtr hook = IntPtr.Zero;
		static HookProc proc;   // kept alive: Windows calls it
		static Timer timer;
		static int pending;     // keys not yet added to today's count
		static DateTime lastKey = DateTime.MinValue, lastReaction = DateTime.MinValue;

		/// <summary>Animations that look like typing, in order of preference.</summary>
		internal static readonly string[] TypingNames = { "type", "typing", "laptop", "code", "coding", "computer", "work", "write", "read", "think" };

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("TypingPet", false);
			set { Program.MyData.SetSetting("TypingPet", value); Apply(); }
		}

		/// <summary>Keys pressed today.</summary>
		public static int Today => DayStats.Get("keys") + pending;

		public static void Apply()
		{
			if (Enabled && !(SelfTest.Running && !SelfTest.Live))
			{
				if (hook == IntPtr.Zero)
				{
					proc = OnKey;
					hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(null), 0);
				}
				if (timer == null) { timer = new Timer { Interval = 500 }; timer.Tick += (s, e) => Step(); }
				timer.Start();
			}
			else
			{
				if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
				timer?.Stop();
				Flush();
			}
		}

		static IntPtr OnKey(int code, IntPtr wParam, IntPtr lParam)
		{
			// only "a key went down": which key is never looked at
			if (code >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)) { pending++; lastKey = DateTime.Now; }
			return CallNextHookEx(hook, code, wParam, lParam);
		}

		/// <summary>Self-test: as if a key was pressed now.</summary>
		internal static void TestKey() { pending++; lastKey = DateTime.Now; Step(); }

		static void Flush() { if (pending > 0) { DayStats.Add("keys", pending); pending = 0; } }

		static void Step()
		{
			if (pending >= 50 || (pending > 0 && (DateTime.Now - lastKey).TotalSeconds > 5)) Flush();
			bool typing = (DateTime.Now - lastKey).TotalSeconds < 1.5;
			if (!typing || (DateTime.Now - lastReaction).TotalSeconds < 6 || HideHotkey.Hidden) return;
			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow && p.Opacity > 0 && !p.Dying);
			if (pet == null || pet.Busy || !pet.OnGround) return;
			if (pet.React(TypingNames)) lastReaction = DateTime.Now;
		}
	}
}
