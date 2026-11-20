using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopPet
{
	/// <summary>
	/// Records one sentence from the default microphone with the classic Windows audio (waveIn: no extra library,
	/// and it works where the Windows speech recognizer hears nothing). It starts saving when the voice starts and
	/// stops after a short silence, then gives back a WAV file (16 kHz, mono, 16 bits) for the transcription.
	/// </summary>
	internal static class VoiceRecorder
	{
		public enum Result { Speech, Silence, NoMicrophone, Canceled }

		const int Rate = 16000, ChunkMs = 100, ChunkBytes = Rate * 2 * ChunkMs / 1000, Buffers = 6;
		const int InitialSilenceMs = 8000, EndSilenceMs = 1400, MaxMs = 30000, PreRollChunks = 4, MinVoiceChunks = 3;

		/// <summary>The last reason it could not record (for the log and the self-test).</summary>
		internal static string LastError { get; private set; }

		/// <summary>The loudest level heard in the last recording (for the log).</summary>
		internal static int LastPeak { get; private set; }

		/// <summary>
		/// Listen until the sentence ends. <paramref name="ready"/> runs when the microphone is open, on the calling thread
		/// (the loop awaits on it, so a window can be updated there).
		/// </summary>
		public static async Task<(Result result, byte[] wav)> Record(Action ready, CancellationToken cancel)
		{
			LastError = null; LastPeak = 0;
			var format = new WaveFormat { wFormatTag = 1, nChannels = 1, nSamplesPerSec = Rate, wBitsPerSample = 16, nBlockAlign = 2, nAvgBytesPerSec = Rate * 2 };
			int err = waveInOpen(out IntPtr device, WAVE_MAPPER, ref format, IntPtr.Zero, IntPtr.Zero, 0);
			if (err != 0) { LastError = "waveInOpen " + err; return (Result.NoMicrophone, null); }
			var headers = new IntPtr[Buffers];
			int headerSize = Marshal.SizeOf<WaveHdr>();
			try
			{
				for (int i = 0; i < Buffers; i++)
				{
					var h = new WaveHdr { lpData = Marshal.AllocHGlobal(ChunkBytes), dwBufferLength = ChunkBytes };
					headers[i] = Marshal.AllocHGlobal(headerSize);
					Marshal.StructureToPtr(h, headers[i], false);
					waveInPrepareHeader(device, headers[i], headerSize);
					waveInAddBuffer(device, headers[i], headerSize);
				}
				err = waveInStart(device);
				if (err != 0) { LastError = "waveInStart " + err; return (Result.NoMicrophone, null); }
				ready?.Invoke();

				var kept = new MemoryStream();
				var preRoll = new Queue<byte[]>();
				var clock = System.Diagnostics.Stopwatch.StartNew();
				double floor = -1;   // the room's noise, from the first chunks
				int voiced = 0, quietMs = 0, next = 0;
				bool talking = false;
				while (true)
				{
					if (cancel.IsCancellationRequested) return (Result.Canceled, null);
					var h = Marshal.PtrToStructure<WaveHdr>(headers[next]);
					if ((h.dwFlags & WHDR_DONE) == 0) { await Task.Delay(25); continue; }
					var chunk = new byte[h.dwBytesRecorded];
					Marshal.Copy(h.lpData, chunk, 0, chunk.Length);
					h.dwFlags &= ~WHDR_DONE; h.dwBytesRecorded = 0;
					Marshal.StructureToPtr(h, headers[next], false);
					waveInAddBuffer(device, headers[next], headerSize);
					next = (next + 1) % Buffers;

					double level = Rms(chunk, out int peak);
					LastPeak = Math.Max(LastPeak, peak);
					double voice = Math.Max(floor * 2.5, 40);
					if (floor < 0) floor = level;
					else if (!talking && level < voice) floor = floor * 0.9 + level * 0.1;   // follows the room's noise, not the voice
					if (!talking)
					{
						preRoll.Enqueue(chunk);
						if (preRoll.Count > PreRollChunks) preRoll.Dequeue();
						voiced = level > voice ? voiced + 1 : 0;
						if (voiced >= 2)
						{
							talking = true;
							foreach (var c in preRoll) kept.Write(c, 0, c.Length);
						}
						else if (clock.ElapsedMilliseconds > InitialSilenceMs) return (Result.Silence, null);
						continue;
					}
					kept.Write(chunk, 0, chunk.Length);
					if (level > voice * 0.8) { voiced++; quietMs = 0; } else quietMs += ChunkMs;
					if (quietMs >= EndSilenceMs || clock.ElapsedMilliseconds > MaxMs) break;
				}
				if (voiced < MinVoiceChunks) return (Result.Silence, null);   // a click or a bump, not a sentence
				return (Result.Speech, Wav(kept.ToArray()));
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
				return (Result.NoMicrophone, null);
			}
			finally
			{
				waveInReset(device);
				foreach (var p in headers)
				{
					if (p == IntPtr.Zero) continue;
					waveInUnprepareHeader(device, p, headerSize);
					var h = Marshal.PtrToStructure<WaveHdr>(p);
					Marshal.FreeHGlobal(h.lpData);
					Marshal.FreeHGlobal(p);
				}
				waveInClose(device);
			}
		}

		/// <summary>How loud a chunk is (root mean square of the 16-bit samples).</summary>
		internal static double Rms(byte[] pcm, out int peak)
		{
			double sum = 0; peak = 0;
			int n = pcm.Length / 2;
			for (int i = 0; i < n; i++)
			{
				int v = BitConverter.ToInt16(pcm, i * 2);
				sum += (double)v * v;
				if (Math.Abs(v) > peak) peak = Math.Abs(v);
			}
			return n == 0 ? 0 : Math.Sqrt(sum / n);
		}

		/// <summary>The samples inside a WAV file.</summary>
		internal static byte[] Wav(byte[] pcm)
		{
			using (var ms = new MemoryStream())
			using (var w = new BinaryWriter(ms))
			{
				w.Write("RIFF".ToCharArray()); w.Write(36 + pcm.Length); w.Write("WAVE".ToCharArray());
				w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
				w.Write("data".ToCharArray()); w.Write(pcm.Length); w.Write(pcm);
				w.Flush();
				return ms.ToArray();
			}
		}

		// ------------------------------------------------------------------ waveIn

		const int WAVE_MAPPER = -1, WHDR_DONE = 1;

		[StructLayout(LayoutKind.Sequential)]
		struct WaveFormat { public short wFormatTag, nChannels; public int nSamplesPerSec, nAvgBytesPerSec; public short nBlockAlign, wBitsPerSample, cbSize; }

		[StructLayout(LayoutKind.Sequential)]
		struct WaveHdr { public IntPtr lpData; public int dwBufferLength, dwBytesRecorded; public IntPtr dwUser; public int dwFlags, dwLoops; public IntPtr lpNext, reserved; }

		[DllImport("winmm.dll")] static extern int waveInOpen(out IntPtr h, int device, ref WaveFormat format, IntPtr callback, IntPtr instance, int flags);
		[DllImport("winmm.dll")] static extern int waveInPrepareHeader(IntPtr h, IntPtr hdr, int size);
		[DllImport("winmm.dll")] static extern int waveInUnprepareHeader(IntPtr h, IntPtr hdr, int size);
		[DllImport("winmm.dll")] static extern int waveInAddBuffer(IntPtr h, IntPtr hdr, int size);
		[DllImport("winmm.dll")] static extern int waveInStart(IntPtr h);
		[DllImport("winmm.dll")] static extern int waveInReset(IntPtr h);
		[DllImport("winmm.dll")] static extern int waveInClose(IntPtr h);
	}
}
