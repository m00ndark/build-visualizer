using System;
using BuildVisualizer.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BuildVisualizer.Services
{
	public class BuildEventService : IDisposable
	{
		private readonly DTE2 _dte;
		private readonly BuildEvents _buildEvents;
		private bool _disposed;

		public event EventHandler BuildBegin;
		public event EventHandler<ProjectStatusChangedEventArgs> ProjectStatusChanged;
		public event EventHandler AllProjectsStatusReset;
		public event EventHandler<ProjectStatusChangedEventArgs> ProjectStatusReset;

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

			BuildBegin?.Invoke(this, EventArgs.Empty);
			AllProjectsStatusReset?.Invoke(this, EventArgs.Empty);
		}

		private void OnBuildProjConfigBegin(string project, string projectConfig, string platform, string solutionConfig)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!string.IsNullOrEmpty(project))
			{
				DateTime now = DateTime.Now;

				// Reset this specific project's status first
				ProjectStatusReset?.Invoke(this, new ProjectStatusChangedEventArgs(project, BuildStatus.NotBuilt, now));

				// Then set it to Building
				ProjectStatusChanged?.Invoke(this, new ProjectStatusChangedEventArgs(project, BuildStatus.Building, now));
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
			// Build is complete
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
