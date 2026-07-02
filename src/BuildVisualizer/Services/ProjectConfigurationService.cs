using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace BuildVisualizer.Services
{
	public class ProjectConfigurationService : IVsUpdateSolutionEvents2, IDisposable
	{
		private readonly IVsSolution _solution;
		private readonly IVsSolutionBuildManager2 _buildManager;
		private uint _cookie;
		private bool _disposed;

		public event Action<string, string, string> ActiveConfigurationChanged;

		public ProjectConfigurationService(IVsSolution solution, IVsSolutionBuildManager2 buildManager)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_solution = solution ?? throw new ArgumentNullException(nameof(solution));
			_buildManager = buildManager ?? throw new ArgumentNullException(nameof(buildManager));

			_buildManager.AdviseUpdateSolutionEvents(this, out _cookie);
		}

		public int OnActiveProjectCfgChange(IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (hierarchy == null)
				return VSConstants.S_OK;

			hierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out string projectPath);

			if (string.IsNullOrEmpty(projectPath))
				return VSConstants.S_OK;

			hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object extObj);

			if (!(extObj is EnvDTE.Project dteProject))
				return VSConstants.S_OK;

			string configuration = null;
			string platform = null;

			try
			{
				EnvDTE.Configuration activeConfig = dteProject.ConfigurationManager?.ActiveConfiguration;
				configuration = activeConfig?.ConfigurationName;
				platform = activeConfig?.PlatformName;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[ProjectConfig] Could not read active configuration for '{projectPath}': {ex.Message}");
				return VSConstants.S_OK;
			}

			ActiveConfigurationChanged?.Invoke(projectPath, configuration, platform);

			return VSConstants.S_OK;
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			ThreadHelper.ThrowIfNotOnUIThread();

			if (_cookie != 0)
			{
				_buildManager.UnadviseUpdateSolutionEvents(_cookie);
				_cookie = 0;
			}
		}

		// Unused IVsUpdateSolutionEvents2 members
		public int UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.S_OK;
		public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => VSConstants.S_OK;
		public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;
		public int UpdateSolution_Cancel() => VSConstants.S_OK;
		public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy, IVsProjectCfg pIVsProjectCfg) => VSConstants.S_OK;
		public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel) => VSConstants.S_OK;
		public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel) => VSConstants.S_OK;
	}
}
