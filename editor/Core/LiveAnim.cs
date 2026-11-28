using System.Runtime.InteropServices;
using XmlData;

namespace PetEditor
{
	/// <summary>
	/// One animation playing in place (the thumbnails of the list of animations and the "All animations" page): it follows
	/// its own sequence — the interval from start to end, the repeats, "repeat from frame", turning at the end — without
	/// moving. Many of them share one clock (<see cref="Advance"/> when <see cref="Due"/> has come).
	/// </summary>
	internal sealed class LiveAnim
	{
		readonly PetDoc doc;
		public readonly AnimationNode Anim;
		public int Step;
		public bool Flipped;
		/// <summary>When the next frame is due (milliseconds of <see cref="Environment.TickCount64"/>).</summary>
		public long Due;
		int total, i0, i1;

		public LiveAnim(PetDoc doc, AnimationNode anim) { this.doc = doc; Anim = anim; Read(); }

		/// <summary>The steps and the times, read again at every round (a random repeat count changes each time).</summary>
		public void Read()
		{
			var c = new ExprContext { ImageW = doc.FrameW, ImageH = doc.FrameH };
			int n = Anim.Sequence?.Frame?.Length ?? 0;
			int from = n == 0 ? 0 : Math.Max(0, Math.Min(Anim.Sequence.RepeatFromFrame, n - 1));
			int repeat = (int)Math.Max(0, Math.Min(200, Expr.Value(Anim.Sequence?.RepeatCount, c)));
			total = n == 0 ? 0 : n + (n - from) * repeat;
			i0 = (int)Expr.Value(Anim.Start?.Interval, c, 200);
			i1 = (int)Expr.Value(Anim.End?.Interval, c, 200);
		}

		public bool HasFrames => total > 0;

		/// <summary>The frame of the sheet shown now (-1: none).</summary>
		public int Frame
		{
			get
			{
				var f = Anim.Sequence?.Frame;
				if (f == null || f.Length == 0) return -1;
				int rf = Math.Max(0, Math.Min(Anim.Sequence.RepeatFromFrame, f.Length - 1));
				int step = Math.Min(Step, Math.Max(0, total - 1));
				return step < f.Length ? f[step] : f[(step - f.Length + rf) % Math.Max(1, f.Length - rf) + rf];
			}
		}

		double Part => total <= 1 ? 0 : (double)Step / total;

		/// <summary>How long the frame shown now stays (as DeskPals: from the start interval to the end one).</summary>
		public int Interval => Math.Max(16, (int)Math.Round(i0 + (i1 - i0) * Part));

		public float Opacity => (float)Math.Max(0.15, Math.Min(1, (Anim.Start?.Opacity ?? 1) + ((Anim.End?.Opacity ?? 1) - (Anim.Start?.Opacity ?? 1)) * Part));

		/// <summary>The next frame; at the end it starts again (turned, when the animation ends with "flip").</summary>
		public void Advance()
		{
			if (total == 0) return;
			Step++;
			if (Step >= total)
			{
				if (Anim.Sequence?.Action == "flip") Flipped = !Flipped;
				Step = 0;
				Read();
			}
		}

		/// <summary>Back to the first frame (to see the ones that start together).</summary>
		public void Restart(long now) { Step = 0; Flipped = false; Read(); Due = now; }

		/// <summary>One frame more if its time has come. True when the picture changed.</summary>
		public bool Tick(long now)
		{
			if (total == 0 || now < Due) return false;
			Advance();
			Due = now + Interval;
			return true;
		}

		/// <summary>Windows' "Show animations" is off: the pictures stay on their first frame.</summary>
		public static bool Still
		{
			get
			{
				try { bool on = true; return SystemParametersInfo(0x1042, 0, ref on, 0) && !on; }   // SPI_GETCLIENTAREAANIMATION
				catch { return false; }
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SystemParametersInfo(int action, int param, ref bool value, int ini);
	}
}
