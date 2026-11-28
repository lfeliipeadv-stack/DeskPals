using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace DeskPalsTools
{
	/// <summary>
	/// Windows shortcuts (.lnk) through Windows' own ShellLink object: made for "Send to", and read back by the tests.
	/// Plain COM interop (no WScript), so nothing depends on scripting being allowed on the PC.
	/// </summary>
	internal static class ShellLink
	{
		/// <summary>Writes a shortcut to <paramref name="target"/> (replacing one with the same name: ours).</summary>
		public static void Save(string lnk, string target, string arguments, string description) => Save(lnk, target, arguments, description, 0);
		
		/// <param name="hotkey">A key that starts it from anywhere (low byte: the key; high byte: 1 Shift, 2 Ctrl, 4 Alt), or 0.</param>
		public static void Save(string lnk, string target, string arguments, string description, short hotkey)
		{
			var link = (IShellLinkW)new CShellLink();
			try
			{
				link.SetPath(target);
				link.SetArguments(arguments ?? "");
				link.SetWorkingDirectory(Path.GetDirectoryName(target) ?? "");
				link.SetIconLocation(target, 0);
				if (!string.IsNullOrEmpty(description)) link.SetDescription(description.Length > 250 ? description[..250] : description);
				if (hotkey != 0) link.SetHotkey(hotkey);
				((IPersistFile)link).Save(lnk, true);
			}
			finally { Marshal.FinalReleaseComObject(link); }
		}

		/// <summary>The key a shortcut has (0 when none, or when it cannot be read).</summary>
		public static short Hotkey(string lnk)
		{
			if (!File.Exists(lnk)) return 0;
			var link = (IShellLinkW)new CShellLink();
			try { ((IPersistFile)link).Load(lnk, 0); link.GetHotkey(out short k); return k; }
			catch (Exception) { return 0; }
			finally { Marshal.FinalReleaseComObject(link); }
		}
		
		/// <summary>Where a shortcut points (null when it cannot be read).</summary>
		public static string Target(string lnk)
		{
			if (!File.Exists(lnk)) return null;
			var link = (IShellLinkW)new CShellLink();
			try
			{
				((IPersistFile)link).Load(lnk, 0);
				var sb = new StringBuilder(1024);
				link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0x4 /* SLGP_RAWPATH */);
				return sb.ToString();
			}
			catch (Exception) { return null; }
			finally { Marshal.FinalReleaseComObject(link); }
		}

		[ComImport, Guid("00021401-0000-0000-C000-000000000046")]
		class CShellLink { }

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
		interface IShellLinkW
		{
			void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, int fFlags);
			void GetIDList(out IntPtr ppidl);
			void SetIDList(IntPtr pidl);
			void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
			void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
			void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
			void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
			void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
			void GetHotkey(out short pwHotkey);
			void SetHotkey(short wHotkey);
			void GetShowCmd(out int piShowCmd);
			void SetShowCmd(int iShowCmd);
			void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
			void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
			void Resolve(IntPtr hwnd, int fFlags);
			void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}
	}
}
