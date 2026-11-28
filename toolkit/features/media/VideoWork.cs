using Windows.Media.MediaProperties;
using Windows.Storage;

namespace DeskPalsTools
{
	/// <summary>
	/// A video made small enough for WhatsApp or e-mail with Windows' own encoder: H.264 at 720p at most (the shape of
	/// the picture kept), a lower bitrate, and AAC sound at 96 kbps.
	/// </summary>
	internal static class VideoWork
	{
		public const int LongSide = 1280, ShortSide = 720;

		/// <summary>The size of the smaller copy: inside 1280×720 (either way round), never bigger than the source, even numbers (the encoder needs them).</summary>
		public static (uint w, uint h) Fit(uint w, uint h)
		{
			double k = Math.Min(1, Math.Min(LongSide / (double)Math.Max(w, h), ShortSide / (double)Math.Min(w, h)));
			static uint Even(double v) => (uint)Math.Max(2, (int)Math.Round(v / 2) * 2);
			return (Even(w * k), Even(h * k));
		}

		/// <summary>About 1.2 Mbit/s for 720p at 30 frames a second, less for smaller pictures, and never more than 60% of the source.</summary>
		public static uint Bitrate(uint w, uint h, double fps, uint source)
		{
			double b = Math.Clamp(w * (double)h * fps * 0.045, 250_000, 1_500_000);
			if (source > 0) b = Math.Min(b, Math.Max(200_000, source * 0.6));
			return (uint)b;
		}

		/// <summary>
		/// The smaller copy. Returns the sizes before and after; when the copy is not smaller it is deleted and an error
		/// says the video was already small.
		/// </summary>
		public static async Task<(long before, long after)> Smaller(string src, string dst, Action<double> progress, CancellationToken cancel)
		{
			// a file another program keeps to itself is told as such, not as a damaged video
			AudioWork.EnsureReadable(src);
			var source = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(src));
			MediaEncodingProfile info;
			try { info = await MediaEncodingProfile.CreateFromFileAsync(source); }
			catch (Exception)
			{
				// Windows reads MP4, MOV, M4V, AVI and WMV by itself: only the other kinds may just need the Store's extensions
				throw new InvalidOperationException(NeedsExtensions(src) ? T.S("err.codec", Jobs.Ext(src)) : T.S("media.err.badVideo"));
			}
			if (info?.Video == null || info.Video.Width == 0 || info.Video.Height == 0) throw new InvalidOperationException(T.S("media.err.noVideo"));

			var (w, h) = Fit(info.Video.Width, info.Video.Height);
			uint num = info.Video.FrameRate.Numerator, den = info.Video.FrameRate.Denominator;
			if (num == 0 || den == 0) { num = 30; den = 1; }
			// more than 30 frames a second only makes the file bigger for a phone screen
			if (num / (double)den > 30.5) { num = 30; den = 1; }
			var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
			profile.Video.Width = w;
			profile.Video.Height = h;
			profile.Video.FrameRate.Numerator = num;
			profile.Video.FrameRate.Denominator = den;
			profile.Video.PixelAspectRatio.Numerator = 1;
			profile.Video.PixelAspectRatio.Denominator = 1;
			profile.Video.Bitrate = Bitrate(w, h, num / (double)den, info.Video.Bitrate);
			// a video without sound stays without sound (asking for sound that is not there makes the converter fail)
			profile.Audio = info.Audio == null ? null : AudioEncodingProperties.CreateAac(44100, 2, 96_000);

			await AudioWork.Transcode(src, dst, profile, null, progress, cancel);
			long before = new FileInfo(src).Length, after = new FileInfo(dst).Length;
			if (after >= before)
			{
				AudioWork.Delete(dst);
				throw new InvalidOperationException(T.S("media.err.notSmaller"));
			}
			return (before, after);
		}

		/// <summary>Kinds of video Windows opens only with the Store's "Web Media Extensions" (the others it reads by itself).</summary>
		public static bool NeedsExtensions(string file) => Jobs.Ext(file) is ".mkv" or ".webm";

		/// <summary>"45 MB", "800 KB".</summary>
		public static string Size(long bytes) =>
			bytes >= 1024L * 1024 * 1024 ? (bytes / (1024.0 * 1024 * 1024)).ToString("0.#") + " GB"
			: bytes >= 1024 * 1024 ? (bytes / (1024.0 * 1024)).ToString(bytes >= 10 * 1024 * 1024 ? "0" : "0.#") + " MB"
			: Math.Max(1, bytes / 1024) + " KB";
	}
}
