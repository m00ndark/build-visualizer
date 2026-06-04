using BuildVisualizer.Services;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using IVsSolutionBuildManager = Microsoft.VisualStudio.Shell.Interop.IVsSolutionBuildManager2;
using Task = System.Threading.Tasks.Task;

namespace BuildVisualizer
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Guid(PackageGuidString)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideToolWindow(typeof(ToolWindow.BuildVisualizerToolWindow))]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class BuildVisualizerPackage : AsyncPackage
	{
		/// <summary>
		/// BuildVisualizerPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "6cb9de7d-b7e0-4471-8b66-df6dd4bda1a4";

		/// <summary>
		/// Gets the BuildEventService instance for this package.
		/// </summary>
		public static BuildEventService BuildEventService { get; private set; }

		/// <summary>
		/// Gets the SolutionService instance for this package.
		/// </summary>
		public static SolutionService SolutionService { get; private set; }

		/// <summary>
		/// Gets the SolutionEventsService instance for this package.
		/// </summary>
		public static SolutionEventsService SolutionEventsService { get; private set; }

		/// <summary>
		/// Gets the ThemeService instance for this package.
		/// </summary>
		public static ThemeService ThemeService { get; private set; }

		/// <summary>
		/// Gets the IVsSolution service for resolving project hierarchies.
		/// </summary>
		public static IVsSolution Solution { get; private set; }

		/// <summary>
		/// Gets the IVsSolutionBuildManager2 service for triggering project builds.
		/// </summary>
		public static IVsSolutionBuildManager SolutionBuildManager { get; private set; }

		/// <summary>
		/// Gets the DTE2 automation object for executing VS commands.
		/// </summary>
		public static DTE2 Dte { get; private set; }

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			// Get DTE2 and IVsSolution services
			if (!(await GetServiceAsync(typeof(EnvDTE.DTE)) is DTE2 dte)
				|| !(await GetServiceAsync(typeof(SVsSolution)) is IVsSolution solution)
				|| !(await GetServiceAsync(typeof(SVsSolutionBuildManager)) is IVsSolutionBuildManager buildManager))
			{
				throw new InvalidOperationException("Failed to get required services.");
			}

			Dte = dte;
			Solution = solution;
			SolutionBuildManager = buildManager;
			BuildEventService = new BuildEventService(dte);
			SolutionService = new SolutionService(new SolutionReferenceSnapshot(solution));
			SolutionEventsService = new SolutionEventsService(solution);
			ThemeService = new ThemeService();

			BuildEventService.BuildBegin += OnBuildBegin;

			await ToolWindow.BuildVisualizerToolWindowCommand.InitializeAsync(this);
		}

		private void OnBuildBegin(object sender, Models.BuildEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ToolWindowPane window = FindToolWindow(typeof(ToolWindow.BuildVisualizerToolWindow), 0, true);
			if (window?.Frame is IVsWindowFrame frame)
			{
				frame.Show();
			}
		}

		protected override void Dispose(bool disposing)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (disposing)
			{
				if (BuildEventService != null)
				{
					BuildEventService.BuildBegin -= OnBuildBegin;
					BuildEventService.Dispose();
				}
				SolutionEventsService?.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
