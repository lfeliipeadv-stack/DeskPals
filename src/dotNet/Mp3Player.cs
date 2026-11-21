using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DesktopPet
{
    /// <summary>
    /// Plays an MP3 with the Windows MCI API (winmm.dll), so no audio library has to ship with the exe.
    /// <para>
    /// MCI needs a file, so the sound bytes are written once to %TEMP%\DesktopPet\sounds (named by content hash,
    /// so reloading the same pet reuses the same file and the same MCI device). Every call must happen on the
    /// UI thread, which is where the pets play their sounds.
    /// </para>
    /// </summary>
    internal sealed class Mp3Player
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr callback);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern bool mciGetErrorString(int error, StringBuilder text, int length);

        private const int MM_MCINOTIFY = 0x03B9;
        private const int MCI_NOTIFY_SUCCESSFUL = 0x0001;

        /// <summary>One MCI device per sound content (hash → player).</summary>
        private static readonly Dictionary<string, Mp3Player> cache = new Dictionary<string, Mp3Player>();
        private static NotifyWindow notifyWindow;
        private static int nextId;

        private readonly string alias;
        private double volume;
        private int repeatsLeft;

        private Mp3Player(string alias) { this.alias = alias; }

        /// <summary>Open an MP3 from memory. Throws with the MCI error text if Windows can't open it.</summary>
        public static Mp3Player Load(byte[] mp3)
        {
            string hash;
            using (var sha = SHA1.Create()) hash = Convert.ToHexString(sha.ComputeHash(mp3));
            if (cache.TryGetValue(hash, out var existing)) return existing;

            string folder = Path.Combine(Path.GetTempPath(), "DesktopPet", "sounds");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, hash + ".mp3");
            if (!File.Exists(file) || new FileInfo(file).Length != mp3.Length) File.WriteAllBytes(file, mp3);

            if (notifyWindow == null)
            {
                notifyWindow = new NotifyWindow();
                Application.ApplicationExit += (s, e) => Send("close all");
            }

            var player = new Mp3Player("dpsnd" + (++nextId));
            int error = Send("open \"" + file + "\" type mpegvideo alias " + player.alias);
            if (error != 0)
            {
                // Windows refuses an MP3 of only a few frames (the clicks of 0.1 s of Pingus and of the Office
                // assistants): the same sound followed by a moment of silence opens and plays the same.
                byte[] padded = PadWithSilence(mp3);
                if (padded != null)
                {
                    string paddedFile = Path.Combine(folder, hash + "-padded.mp3");
                    if (!File.Exists(paddedFile) || new FileInfo(paddedFile).Length != padded.Length) File.WriteAllBytes(paddedFile, padded);
                    error = Send("open \"" + paddedFile + "\" type mpegvideo alias " + player.alias);
                }
            }
            Check(error);
            cache[hash] = player;
            return player;
        }

        /// <summary>
        /// The MP3 followed by silent frames of the same format (a frame whose side information is all zero decodes
        /// to silence), about <paramref name="seconds"/> long. Null when no MPEG Layer III frame is found.
        /// </summary>
        internal static byte[] PadWithSilence(byte[] mp3, double seconds = 0.5)
        {
            int i = 0;
            if (mp3.Length > 10 && mp3[0] == 'I' && mp3[1] == 'D' && mp3[2] == '3')
                i = 10 + ((mp3[6] & 0x7F) << 21 | (mp3[7] & 0x7F) << 14 | (mp3[8] & 0x7F) << 7 | (mp3[9] & 0x7F)) + ((mp3[5] & 0x10) != 0 ? 10 : 0);
            int[] kbps1 = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 };
            int[] kbps2 = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 };
            int[] rates = { 44100, 48000, 32000 };
            for (; i + 4 <= mp3.Length; i++)
            {
                if (mp3[i] != 0xFF || (mp3[i + 1] & 0xE0) != 0xE0) continue;
                int version = (mp3[i + 1] >> 3) & 3;     // 3 MPEG 1, 2 MPEG 2, 0 MPEG 2.5 (1 is reserved)
                int layer = (mp3[i + 1] >> 1) & 3;       // 1 = Layer III
                int bitIndex = (mp3[i + 2] >> 4) & 15, rateIndex = (mp3[i + 2] >> 2) & 3;
                if (version == 1 || layer != 1 || bitIndex == 0 || bitIndex == 15 || rateIndex == 3) continue;
                int rate = rates[rateIndex] / (version == 3 ? 1 : version == 2 ? 2 : 4);
                int kbps = (version == 3 ? kbps1 : kbps2)[bitIndex];
                int samples = version == 3 ? 1152 : 576;
                int length = samples / 8 * kbps * 1000 / rate;
                var frame = new byte[length];
                frame[0] = 0xFF;
                frame[1] = (byte)(mp3[i + 1] | 1);       // no CRC
                frame[2] = (byte)(mp3[i + 2] & 0xFD);    // no padding byte
                frame[3] = mp3[i + 3];
                int count = (int)Math.Ceiling(seconds * rate / samples);
                using (var ms = new MemoryStream())
                {
                    ms.Write(mp3, 0, mp3.Length);
                    for (int k = 0; k < count; k++) ms.Write(frame, 0, frame.Length);
                    return ms.ToArray();
                }
            }
            return null;
        }

        /// <summary>
        /// Play from the beginning. <paramref name="volume"/> goes from 0.0 to 1.0,
        /// <paramref name="repeats"/> is how many extra times the sound is played after the first one.
        /// </summary>
        public void Play(double volume, int repeats)
        {
            this.volume = volume;
            repeatsLeft = repeats;
            PlayOnce();
        }

        private void PlayOnce()
        {
            int v = (int)Math.Round(Math.Max(0.0, Math.Min(1.0, volume)) * 1000);
            Check(Send("setaudio " + alias + " volume to " + v));
            Check(Send("play " + alias + " from 0 notify", notifyWindow.Handle));
        }

        private static int Send(string command, IntPtr callback = default)
        {
            return mciSendString(command, null, 0, callback);
        }

        private static void Check(int error)
        {
            if (error == 0) return;
            var text = new StringBuilder(256);
            throw new InvalidOperationException(mciGetErrorString(error, text, text.Capacity) ? text.ToString() : "MCI error " + error);
        }

        /// <summary>Hidden window that receives the "playback finished" notification from MCI.</summary>
        private sealed class NotifyWindow : NativeWindow
        {
            public NotifyWindow() { CreateHandle(new CreateParams()); }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == MM_MCINOTIFY && (int)m.WParam == MCI_NOTIFY_SUCCESSFUL)
                {
                    // lParam is the MCI device id: find the player that owns it.
                    foreach (var p in cache.Values)
                    {
                        if (mciGetDeviceId(p.alias) != (int)m.LParam) continue;
                        if (p.repeatsLeft-- > 0)
                        {
                            try { p.PlayOnce(); } catch (InvalidOperationException) { p.repeatsLeft = 0; }
                        }
                        break;
                    }
                    return;
                }
                base.WndProc(ref m);
            }
        }

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciGetDeviceIDW")]
        private static extern int mciGetDeviceId(string device);
    }
}
