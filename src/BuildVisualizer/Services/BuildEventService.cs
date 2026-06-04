using System;
using BuildVisualizer.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace BuildVisualizer.Services
{
	public class BuildEventService : IDisposable
	{
		private readonly DTE2 _dte;
		private readonly BuildEvents _buildEvents;
		private bool _disposed;
		private vsBuildAction _currentBuildAction;

		public event EventHandler<BuildEventArgs> BuildBegin;
		public event EventHandler<BuildEventArgs> BuildDone;
		public event EventHandler<ProjectStatusChangedEventArgs> ProjectStatusChanged;

		public BuildEventService(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_dte = dte;
			_buildEvents = _dte.Events.BuildEvents;

			// Subscribe to build events
			_buildEvents.OnBuildBegin += OnBuildBegin;
			_buildEvents.OnBuildProjConfigBegin += OnBuildProjConfigBegin;
			_buildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;
			_buildEvents.OnBuildDone += OnBuildDone;
		}

		private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_currentBuildAction = action;
			BuildBegin?.Invoke(this, new BuildEventArgs(scope, action));
		}

		private void OnBuildProjConfigBegin(string project, string projectConfig, string platform, string solutionConfig)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!string.IsNullOrEmpty(project))
			{
				BuildStatus status = _currentBuildAction == vsBuildAction.vsBuildActionClean
					? BuildStatus.Cleaning
					: BuildStatus.Building;
				ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project, status, DateTime.Now));
			}
		}

		private void OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!string.IsNullOrEmpty(project))
			{
				BuildStatus status = success ? BuildStatus.Success : BuildStatus.Failed;
				ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project, status, DateTime.Now));
			}
		}

		private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			BuildDone?.Invoke(this, new BuildEventArgs(scope, action));
		}

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!_disposed)
			{
				if (_buildEvents != null)
				{
					_buildEvents.OnBuildBegin -= OnBuildBegin;
					_buildEvents.OnBuildProjConfigBegin -= OnBuildProjConfigBegin;
					_buildEvents.OnBuildProjConfigDone -= OnBuildProjConfigDone;
					_buildEvents.OnBuildDone -= OnBuildDone;
				}

				_disposed = true;
			}
		}
	}
}
