namespace DeskPalsTools
{
	/// <summary>
	/// The end of the program: the thumbnails being made are finished, and the graphics of the colour emoji
	/// (Direct2D, DirectWrite, WIC) are given back while the program still runs. Left to Windows' own clean-up at the
	/// very end, together with the graphics of the PDF reader ("See and edit"), they kept the program from ending.
	/// </summary>
	internal static class Ending
	{
		static int done;

		public static void Now()
		{
			if (Interlocked.Exchange(ref done, 1) == 1) return;
			Thumbnails.Stop();
			DesktopPet.ColorText.Shutdown();
		}
	}
}
