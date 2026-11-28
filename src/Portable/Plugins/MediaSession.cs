using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopPet
{
	/// <summary>
	/// What is playing on the computer, from the media controls of Windows (the same as the volume flyout and the lock
	/// screen show): Spotify, YouTube or Deezer in a browser, Apple Music, VLC, the Media Player… Called straight through
	/// the Windows Runtime (combase.dll), without the 25 MB projection package, so DeskPals stays small. Windows 10 1809
	/// or newer; on older systems (or anything going wrong) it answers null and the caller falls back to Spotify's title.
	/// </summary>
	/// <remarks>
	/// Interfaces of Windows.Media.Control, used by their slots (IUnknown 0–2, IInspectable 3–5, then the members in the
	/// order of the metadata): the manager statics RequestAsync (6); the manager GetSessions (7); a session
	/// SourceAppUserModelId (6), TryGetMediaPropertiesAsync (7), GetPlaybackInfo (9); the playback info PlaybackStatus (7),
	/// PlaybackType (8); the media properties Title (6), Artist (9). Call it away from the window thread: it waits for
	/// the asynchronous answers (a fraction of a second).
	/// </remarks>
	public static unsafe class MediaSession
	{
		public sealed class Info
		{
			public string Title = "", Artist = "", App = "";
			/// <summary>"Artist - Title" (or just the title).</summary>
			public override string ToString() => Artist.Length > 0 && Title.Length > 0 ? Artist + " - " + Title : Title;
		}

		const string ManagerClass = "Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager";
		static Guid IID_ManagerStatics = new Guid("2050c4ee-11a0-57de-aed7-c97c70338245");
		static Guid IID_AsyncInfo = new Guid("00000036-0000-0000-C000-000000000046");
		const int Playing = 4;          // GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
		const int VideoType = 2;        // MediaPlaybackType.Video

		[DllImport("combase.dll")] static extern int RoGetActivationFactory(IntPtr classId, ref Guid iid, out IntPtr factory);
		[DllImport("combase.dll", CharSet = CharSet.Unicode)] static extern int WindowsCreateString(string s, int length, out IntPtr hstring);
		[DllImport("combase.dll")] static extern int WindowsDeleteString(IntPtr hstring);
		[DllImport("combase.dll")] static extern char* WindowsGetStringRawBuffer(IntPtr hstring, out uint length);

		static bool broken;

		/// <summary>The first session playing now (not a video), or null (nothing plays, or the controls are not there).</summary>
		public static Info Current()
		{
			if (broken) return null;
			IntPtr statics = IntPtr.Zero, op = IntPtr.Zero, manager = IntPtr.Zero, sessions = IntPtr.Zero;
			try
			{
				IntPtr name;
				if (WindowsCreateString(ManagerClass, ManagerClass.Length, out name) < 0) return null;
				int hr;
				try { hr = RoGetActivationFactory(name, ref IID_ManagerStatics, out statics); }
				finally { WindowsDeleteString(name); }
				if (hr < 0 || statics == IntPtr.Zero) { broken = true; LogFile.Write("media controls: not available (" + hr.ToString("X8") + ")"); return null; }
				if (Get(statics, 6, out op) < 0) return null;                  // RequestAsync()
				manager = Await(op);
				if (manager == IntPtr.Zero) return null;
				if (Get(manager, 7, out sessions) < 0) return null;            // GetSessions(): IVectorView<Session>
				uint count;
				if (GetUInt(sessions, 7, out count) < 0) return null;         // Size
				for (uint i = 0; i < count && i < 16; i++)
				{
					IntPtr session;
					if (GetAt(sessions, i, out session) < 0 || session == IntPtr.Zero) continue;
					try
					{
						var info = Read(session);
						if (info != null) return info;
					}
					finally { Release(session); }
				}
				return null;
			}
			catch (Exception skipped) { LogFile.Ignored(skipped); return null; }
			finally { Release(sessions); Release(manager); Release(op); Release(statics); }
		}

		/// <summary>Developer: every session with its raw status ("app: status 4 (playing), type …").</summary>
		internal static string Describe()
		{
			var sb = new System.Text.StringBuilder();
			IntPtr statics = IntPtr.Zero, op = IntPtr.Zero, manager = IntPtr.Zero, sessions = IntPtr.Zero;
			try
			{
				IntPtr name;
				WindowsCreateString(ManagerClass, ManagerClass.Length, out name);
				try { RoGetActivationFactory(name, ref IID_ManagerStatics, out statics); } finally { WindowsDeleteString(name); }
				if (statics == IntPtr.Zero || Get(statics, 6, out op) < 0) return "no manager";
				manager = Await(op);
				if (manager == IntPtr.Zero || Get(manager, 7, out sessions) < 0) return "no sessions";
				GetUInt(sessions, 7, out uint count);
				sb.Append(count + " session(s)");
				for (uint i = 0; i < count; i++)
				{
					if (GetAt(sessions, i, out IntPtr s) < 0) continue;
					IntPtr pb;
					int status = -1;
					if (Get(s, 9, out pb) >= 0 && pb != IntPtr.Zero) { GetInt(pb, 7, out status); Release(pb); }
					sb.Append("; " + String(s, 6) + ": status " + status);
					Release(s);
				}
				return sb.ToString();
			}
			catch (Exception ex) { return ex.Message; }
			finally { Release(sessions); Release(manager); Release(op); Release(statics); }
		}

		static Info Read(IntPtr session)
		{
			IntPtr playback = IntPtr.Zero, op = IntPtr.Zero, props = IntPtr.Zero;
			try
			{
				if (Get(session, 9, out playback) < 0 || playback == IntPtr.Zero) return null;   // GetPlaybackInfo()
				int status;
				if (GetInt(playback, 7, out status) < 0 || status != Playing) return null;     // PlaybackStatus
				// a video (a film in the browser) is not a song: only when the player says so
				IntPtr typeRef;
				if (Get(playback, 8, out typeRef) >= 0 && typeRef != IntPtr.Zero)               // PlaybackType: IReference<MediaPlaybackType>
				{
					int type;
					bool video = GetInt(typeRef, 6, out type) >= 0 && type == VideoType;
					Release(typeRef);
					if (video) return null;
				}
				var info = new Info { App = String(session, 6) };                             // SourceAppUserModelId
				if (Get(session, 7, out op) >= 0)                                              // TryGetMediaPropertiesAsync()
				{
					props = Await(op);
					if (props != IntPtr.Zero)
					{
						info.Title = String(props, 6).Trim();
						info.Artist = String(props, 9).Trim();
					}
				}
				return info.Title.Length > 0 ? info : null;
			}
			finally { Release(props); Release(op); Release(playback); }
		}

		/// <summary>Wait (up to 2 s) for an IAsyncOperation and take its result.</summary>
		static IntPtr Await(IntPtr op)
		{
			if (op == IntPtr.Zero) return IntPtr.Zero;
			IntPtr asyncInfo;
			if (QueryInterface(op, IID_AsyncInfo, out asyncInfo) < 0) return IntPtr.Zero;
			try
			{
				for (int i = 0; i < 100; i++)
				{
					int status;
					if (GetInt(asyncInfo, 7, out status) < 0) return IntPtr.Zero;   // Status: 0 started, 1 completed, 2 canceled, 3 error
					if (status == 1) break;
					if (status > 1) return IntPtr.Zero;
					Thread.Sleep(20);
					if (i == 99) return IntPtr.Zero;
				}
			}
			finally { Release(asyncInfo); }
			IntPtr result;
			return Get(op, 8, out result) >= 0 ? result : IntPtr.Zero;              // GetResults()
		}

		// ------------------------------------------------------------------ calls by slot

		static IntPtr Slot(IntPtr obj, int slot) => (*(IntPtr**)obj)[slot];

		static int Get(IntPtr obj, int slot, out IntPtr result)
		{
			IntPtr r = IntPtr.Zero;
			int hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Slot(obj, slot))(obj, &r);
			result = hr >= 0 ? r : IntPtr.Zero;
			return hr;
		}

		static int GetInt(IntPtr obj, int slot, out int result)
		{
			int r = 0;
			int hr = ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)Slot(obj, slot))(obj, &r);
			result = r;
			return hr;
		}

		static int GetUInt(IntPtr obj, int slot, out uint result)
		{
			uint r = 0;
			int hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)Slot(obj, slot))(obj, &r);
			result = r;
			return hr;
		}

		/// <summary>IVectorView.GetAt(index) (slot 6).</summary>
		static int GetAt(IntPtr vector, uint index, out IntPtr result)
		{
			IntPtr r = IntPtr.Zero;
			int hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)Slot(vector, 6))(vector, index, &r);
			result = hr >= 0 ? r : IntPtr.Zero;
			return hr;
		}

		/// <summary>A string property (an HSTRING), "" when it fails.</summary>
		static string String(IntPtr obj, int slot)
		{
			IntPtr h;
			if (Get(obj, slot, out h) < 0 || h == IntPtr.Zero) return "";
			try
			{
				char* p = WindowsGetStringRawBuffer(h, out uint length);
				return p == null ? "" : new string(p, 0, (int)length);
			}
			finally { WindowsDeleteString(h); }
		}

		static int QueryInterface(IntPtr obj, Guid iid, out IntPtr result)
		{
			IntPtr r = IntPtr.Zero;
			int hr = ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)Slot(obj, 0))(obj, &iid, &r);
			result = hr >= 0 ? r : IntPtr.Zero;
			return hr;
		}

		static void Release(IntPtr obj)
		{
			if (obj != IntPtr.Zero) ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(obj, 2))(obj);
		}
	}
}
