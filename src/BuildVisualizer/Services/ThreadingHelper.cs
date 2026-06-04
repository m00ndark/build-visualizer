using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;

namespace BuildVisualizer.Services
{
	internal static class ThreadingHelper
	{
		/// <summary>
		/// Runs an action on the UI thread, blocking until it completes.
		/// </summary>
		public static void RunOnMainThread(Action action)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				action();
			});
		}

		/// <summary>
		/// Runs an async task on the UI thread, blocking until it completes.
		/// </summary>
		public static void RunOnMainThread(Func<Task> asyncAction)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				await asyncAction();
			});
		}
	}
}
