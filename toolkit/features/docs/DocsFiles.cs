namespace DeskPalsTools
{
	/// <summary>Small file checks shared by the "docs" jobs, so their messages say the real reason.</summary>
	internal static class DocsFiles
	{
		/// <summary>Whether Windows refused the file because another program holds it (sharing or lock violation).</summary>
		public static bool InUse(Exception ex) => ex is IOException && (ex.HResult & 0xFFFF) is 32 or 33;

		/// <summary>
		/// Throws a friendly "the file is open in another program" when another program keeps it to itself. Only a program
		/// that lets nobody read it counts: one that is merely writing it (and lets others read) is fine for us.
		/// </summary>
		public static void CheckFree(string file)
		{
			try { using var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); }
			catch (IOException ex) when (InUse(ex)) { throw new InvalidOperationException(T.S("docs.inUse")); }
		}
	}
}
