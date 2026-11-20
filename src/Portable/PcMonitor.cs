using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopPet
{
	/// <summary>
	/// CPU, memory and disk in a small plate next to the pet (Settings → Extras or the pet menu).
	/// When the CPU stays above 90% for a minute, the pet comments on it (at most every 30 minutes).
	/// The same for the memory, with a button to free it (trims the working set of every process it may touch).
	/// </summary>
	public static class PcMonitor
	{
		[StructLayout(LayoutKind.Sequential)] struct FILETIME { public uint Low, High; public ulong Value => ((ulong)High << 32) | Low; }
		[DllImport("kernel32.dll")] static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		class MEMORYSTATUSEX { public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); public uint dwMemoryLoad; public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual; }
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

		static ulong lastIdle, lastTotal;
		static Timer timer;
		static Hud hud;
		static int busySeconds, ramSeconds;
		static DateTime lastComment = DateTime.MinValue, lastRamWarn = DateTime.MinValue;

		[DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
		[DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
		[DllImport("psapi.dll")] static extern bool EmptyWorkingSet(IntPtr h);
		const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, PROCESS_SET_QUOTA = 0x0100;

		/// <summary>Memory use (%) that counts as high, and how long it must last before the pet says so.</summary>
		public const int RamHigh = 90, RamHighSeconds = 60;
		/// <summary>At most one memory warning in this many minutes.</summary>
		public const int RamWarnEveryMinutes = 30;

		public static int Cpu { get; private set; }
		public static int Ram { get; private set; }
		/// <summary>The system drive: how full it is (%), and its free space as text ("C: 120 GB free").</summary>
		public static int DiskUsed { get; private set; }
		public static string Disk { get; private set; } = "";

		public static bool Enabled
		{
			get => Program.MyData.GetSettingBool("PcMonitor", false);
			set { Program.MyData.SetSetting("PcMonitor", value); Apply(); }
		}

		public static void Apply()
		{
			if (Enabled)
			{
				if (timer == null) { timer = new Timer { Interval = 2000 }; timer.Tick += (s, e) => Tick(); }
				timer.Start();
				Tick();
			}
			else
			{
				timer?.Stop();
				hud?.Close();
				hud = null;
			}
		}

		static void Tick()
		{
			try
			{
				if (GetSystemTimes(out var idle, out var kernel, out var user))
				{
					ulong total = kernel.Value + user.Value;
					if (lastTotal > 0 && total > lastTotal)
						Cpu = (int)Math.Round(100.0 * (1 - (idle.Value - lastIdle) / (double)(total - lastTotal)));
					lastIdle = idle.Value; lastTotal = total;
				}
				var mem = new MEMORYSTATUSEX();
				if (GlobalMemoryStatusEx(mem)) Ram = (int)mem.dwMemoryLoad;
				var sys = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
				if (sys.IsReady && sys.TotalSize > 0)
				{
					DiskUsed = (int)Math.Round(100.0 * (sys.TotalSize - sys.TotalFreeSpace) / sys.TotalSize);
					Disk = Lang.T("mon.diskFree", sys.Name.TrimEnd('\\'), sys.AvailableFreeSpace / (1024.0 * 1024 * 1024));
				}
				else { DiskUsed = 0; Disk = ""; }
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); }

			// the pet notices a busy computer
			busySeconds = Cpu >= 90 ? busySeconds + 2 : 0;
			if (busySeconds >= 60 && (DateTime.Now - lastComment).TotalMinutes > 30)
			{
				lastComment = DateTime.Now;
				busySeconds = 0;
				Program.Mainthread?.ReactAll("tired", "dizzy", "hot", "sweat");
				SpeechBubble.Notify(Lang.T("mon.busy", Cpu), 8);
			}

			// …and memory almost full
			if (RamTick(Ram, 2, DateTime.Now)) WarnRam();

			var pet = Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow && p.Opacity > 0);
			if (pet == null || HideHotkey.Hidden) { hud?.Hide(); return; }
			if (hud == null || hud.IsDisposed) hud = new Hud();
			hud.Follow(pet);   // shown only while the mouse is on the pet (see Hud)
			hud.Invalidate();
		}

		/// <summary>
		/// Counts the seconds the memory stays high; true when it is time to warn
		/// (high for <see cref="RamHighSeconds"/> and no warning in the last <see cref="RamWarnEveryMinutes"/> minutes).
		/// </summary>
		internal static bool RamTick(int ram, int seconds, DateTime now)
		{
			ramSeconds = ram >= RamHigh ? ramSeconds + seconds : 0;
			if (ramSeconds < RamHighSeconds || (now - lastRamWarn).TotalMinutes <= RamWarnEveryMinutes) return false;
			lastRamWarn = now;
			ramSeconds = 0;
			return true;
		}

		/// <summary>Self-test: start counting again, as if no warning was ever given.</summary>
		internal static void ResetRamWarning() { ramSeconds = 0; lastRamWarn = DateTime.MinValue; }

		/// <summary>The pet warns that the memory is almost full, with a button to free it.</summary>
		internal static void WarnRam()
		{
			Program.Mainthread?.ReactAll("tired", "dizzy", "sweat", "surprised");
			SpeechBubble.Notify(Lang.T("mon.ramHigh", Ram), 20,
				new SpeechBubble.Choice(Lang.T("mon.optimize"), OptimizeRam, true),
				new SpeechBubble.Choice(Lang.T("mon.notNow"), null));
		}

		/// <summary>The memory is being freed now (the button of the plate waits).</summary>
		public static bool Optimizing { get; private set; }

		/// <summary>What freeing the memory did.</summary>
		public sealed class RamResult
		{
			public int Before, After, Processes;
			public long FreedMb;
		}

		/// <summary>Frees memory in the background, then the pet tells how much (RAM before and after). No process is closed.</summary>
		public static void OptimizeRam()
		{
			if (Optimizing) return;
			Optimizing = true;
			if (hud != null && !hud.IsDisposed) hud.Invalidate();
			Program.Mainthread?.ReactAll("think", "work", "look up");
			var ui = TaskScheduler.FromCurrentSynchronizationContext();
			Task.Run(TrimAll).ContinueWith(t =>
			{
				Optimizing = false;
				if (t.IsFaulted) { LogFile.Ignored(t.Exception); return; }
				var r = t.Result;
				Ram = r.After;
				if (hud != null && !hud.IsDisposed) hud.Invalidate();
				Program.Mainthread?.ReactAll("happy", "cheer", "jump");
				SpeechBubble.Say(r.FreedMb > 0 ? Lang.T("mon.optimized", r.FreedMb, r.Before, r.After) : Lang.T("mon.optimizedNone", r.After), 10);
			}, ui);
		}

		/// <summary>
		/// Asks Windows to move out of RAM the pages each process is not using now (EmptyWorkingSet).
		/// Without administrator rights some processes cannot be opened: those are skipped. Nothing is closed;
		/// the pages come back by themselves when a program needs them.
		/// </summary>
		internal static RamResult TrimAll()
		{
			var r = new RamResult();
			var mem = new MEMORYSTATUSEX();
			ulong availBefore = 0;
			if (GlobalMemoryStatusEx(mem)) { r.Before = (int)mem.dwMemoryLoad; availBefore = mem.ullAvailPhys; }
			foreach (var p in System.Diagnostics.Process.GetProcesses())
			{
				try
				{
					if (p.Id == 0 || p.Id == 4) continue;   // idle and system
					var h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_SET_QUOTA, false, p.Id);
					if (h == IntPtr.Zero) continue;
					try { if (EmptyWorkingSet(h)) r.Processes++; }
					finally { CloseHandle(h); }
				}
				catch (Exception skipped) { LogFile.Ignored(skipped); }
				finally { p.Dispose(); }
			}
			System.Threading.Thread.Sleep(400);   // let the numbers settle
			mem = new MEMORYSTATUSEX();
			if (GlobalMemoryStatusEx(mem))
			{
				r.After = (int)mem.dwMemoryLoad;
				r.FreedMb = mem.ullAvailPhys > availBefore ? (long)((mem.ullAvailPhys - availBefore) / (1024 * 1024)) : 0;
			}
			else r.After = r.Before;
			return r;
		}

		/// <summary>The little plate beside the pet.</summary>
		sealed class Hud : Form
		{
			readonly Font font = Theme.CreateFont(11.5f), bold = Theme.CreateFont(11.5f, FontStyle.Bold);
			FormPet pet;
			bool hoverButton;
			readonly Timer follow = new Timer { Interval = 40 };

			public Hud()
			{
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; DoubleBuffered = true;
				Size = new Size(150, 117);
				Theme.ApplyWindowChrome(this);
				follow.Tick += (s, e) => HoverCheck();
				follow.Start();
				FormClosed += (s, e) => { follow.Stop(); follow.Dispose(); };
			}

			protected override bool ShowWithoutActivation => true;

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					// tool window, no activate, topmost; not click-through: it sits beside the pet (never over it) and its button takes clicks
					cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008;
					return cp;
				}
			}

			public void Follow(FormPet p) { pet = p; Place(); HoverCheck(); }

			/// <summary>The plate shows while the mouse is over the pet, and stays while the mouse is on the plate (to click its button).</summary>
			void HoverCheck()
			{
				// closed before it was ever shown, a form is disposed without FormClosed: the timer must stop here too
				if (IsDisposed || Disposing) { follow.Stop(); return; }
				if (pet == null || pet.IsDisposed || !pet.Visible || HideHotkey.Hidden) { if (Visible) Hide(); return; }
				bool over = Rectangle.Inflate(pet.Bounds, 6, 6).Contains(Cursor.Position)
					|| (Visible && Rectangle.Inflate(Bounds, 4, 4).Contains(Cursor.Position));
				if (over)
				{
					Place();
					if (!Visible) { Show(); Invalidate(); }
				}
				else if (Visible) { hoverButton = false; Hide(); }
			}

			/// <summary>The "Free memory" button at the bottom of the plate.</summary>
			internal Rectangle ButtonRect => new Rectangle(8, 87, Width - 16, 24);

			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				bool h = ButtonRect.Contains(e.Location);
				if (h != hoverButton) { hoverButton = h; Cursor = h ? Cursors.Hand : Cursors.Default; Invalidate(); }
			}

			protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (hoverButton) { hoverButton = false; Invalidate(); } }

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
				if (e.Button == MouseButtons.Left && ButtonRect.Contains(e.Location)) OptimizeRam();
			}

			/// <summary>Self-test: is the plate on screen.</summary>
			internal bool Showing => Visible;

			void Place()
			{
				var wa = Screen.FromControl(pet).WorkingArea;
				int x = pet.Left - Width - 4, y = pet.Bottom - Height - 6;
				if (x < wa.Left) x = pet.Right + 4;
				x = Math.Max(wa.Left, Math.Min(wa.Right - Width, x));
				y = Math.Max(wa.Top, Math.Min(wa.Bottom - Height, y));
				if (Left != x || Top != y) Location = new Point(x, y);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				Theme.Prepare(g);
				g.Clear(Theme.Surface);
				void Row(int y, string label, int pct)
				{
					Theme.DrawText(g, label, bold, new Rectangle(10, y, 46, 16), Theme.Text, TextFormatFlags.SingleLine);
					var track = new Rectangle(56, y + 5, 56, 6);
					Theme.FillRounded(g, Theme.Hover, track, 3);
					var c = pct >= 90 ? Theme.Danger : pct >= 70 ? Color.FromArgb(214, 150, 20) : Theme.Accent;
					Theme.FillRounded(g, c, new Rectangle(track.X, track.Y, Math.Max(6, track.Width * Math.Min(100, pct) / 100), track.Height), 3);
					Theme.DrawText(g, pct + "%", font, new Rectangle(116, y, 32, 16), Theme.TextMuted, TextFormatFlags.SingleLine);
				}
				Row(8, "CPU", Cpu);
				Row(27, "RAM", Ram);
				Row(46, Lang.T("mon.disk"), DiskUsed);
				Theme.DrawText(g, Disk, font, new Rectangle(10, 66, Width - 14, 16), Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				Theme.DrawButton(g, ButtonRect, Optimizing ? Lang.T("mon.optimizing") : Lang.T("mon.optimize"), font, Ram >= RamHigh && !Optimizing, hoverButton);
			}

			protected override void Dispose(bool disposing) { if (disposing) { follow.Stop(); follow.Dispose(); font.Dispose(); bold.Dispose(); } base.Dispose(disposing); }
		}
	}
}
