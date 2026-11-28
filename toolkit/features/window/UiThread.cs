namespace DeskPalsTools
{
	/// <summary>
	/// Tests: ONE window thread for all the window tests, alive until the end, as the program has (its windows all run
	/// on the main thread). A thread per test left Windows' drawing objects (thumbnails, PDF pages drawn with the
	/// graphics card) behind on threads that were gone, which could keep the program from ending.
	/// </summary>
	internal static class UiThread
	{
		static WindowsFormsSynchronizationContext context;
		static readonly object gate = new();

		static WindowsFormsSynchronizationContext Context()
		{
			lock (gate)
			{
				if (context != null) return context;
				var ready = new ManualResetEventSlim();
				var thread = new Thread(() =>
				{
					// a control made here gives the thread its message window, then the context posts to it
					using var anchor = new Control();
					_ = anchor.Handle;
					context = new WindowsFormsSynchronizationContext();
					SynchronizationContext.SetSynchronizationContext(context);
					ready.Set();
					Application.Run();
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.IsBackground = true;
				thread.Name = "test windows";
				thread.Start();
				ready.Wait();
				return context;
			}
		}

		/// <summary>Runs <paramref name="work"/> on the window thread (every await comes back to it); its end or its error.</summary>
		public static Task Run(Func<Task> work)
		{
			var done = new TaskCompletionSource();
			Context().Post(async _ =>
			{
				try { await work(); done.TrySetResult(); }
				catch (Exception ex) { done.TrySetException(ex); }
				finally
				{
					// windows a test left open are closed (the next test starts clean)
					foreach (var f in Application.OpenForms.Cast<Form>().ToList()) try { f.Close(); f.Dispose(); } catch (Exception) { }
				}
			}, null);
			return done.Task.WaitAsync(TimeSpan.FromMinutes(3));
		}
	}
}
