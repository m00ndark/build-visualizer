using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BuildVisualizer.Services
{
	public class SolutionEventsService : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
	{
		private static readonly Guid _solutionFolderGuid = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");

		private readonly IVsSolution _solution;
		private uint _solutionEventsCookie;
		private bool _disposed;
		private SolutionReferenceWatcher _watcher;

		public event EventHandler SolutionOpened;
		public event Action<IReadOnlyList<ProjectReferences>> SolutionFullyLoaded;
		public event EventHandler SolutionClosed;
		public event EventHandler<ProjectEventArgs> ProjectAdded;
		public event EventHandler<ProjectEventArgs> ProjectRemoved;

		public SolutionEventsService(IVsSolution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_solution = solution ?? throw new ArgumentNullException(nameof(solution));

			// Subscribe to solution events (IVsSolutionEvents)
			// Since we also implement IVsSolutionLoadEvents, VS should detect this
			// and call those methods as well when appropriate
			_solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
		}

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (fAdded != 0)
			{
				// Project was added to the solution
				if (pHierarchy != null)
				{
					if (ErrorHandler.Succeeded(pHierarchy.GetProperty(
						(uint)VSConstants.VSITEMID.Root,
						(int)__VSHPROPID.VSHPROPID_Name,
						out object nameObj)) && nameObj is string projectName)
					{
						ProjectAdded?.Invoke(this, new ProjectEventArgs(projectName, pHierarchy));
					}
				}
			}

			return VSConstants.S_OK;
		}

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (fRemoved != 0)
			{
				// Project is being removed from the solution
				if (pHierarchy != null)
				{
					if (ErrorHandler.Succeeded(pHierarchy.GetProperty(
						(uint)VSConstants.VSITEMID.Root,
						(int)__VSHPROPID.VSHPROPID_Name,
						out object nameObj)) && nameObj is string projectName)
					{
						ProjectRemoved?.Invoke(this, new ProjectEventArgs(projectName, pHierarchy));
					}
				}
			}

			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Project was loaded (e.g., after being unloaded)
			if (pRealHierarchy != null)
			{
				if (ErrorHandler.Succeeded(pRealHierarchy.GetProperty(
					(uint)VSConstants.VSITEMID.Root,
					(int)__VSHPROPID.VSHPROPID_Name,
					out object nameObj)) && nameObj is string projectName)
				{
					ProjectAdded?.Invoke(this, new ProjectEventArgs(projectName, pRealHierarchy));
				}
			}

			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Project is being unloaded
			if (pRealHierarchy != null)
			{
				if (ErrorHandler.Succeeded(pRealHierarchy.GetProperty(
					(uint)VSConstants.VSITEMID.Root,
					(int)__VSHPROPID.VSHPROPID_Name,
					out object nameObj)) && nameObj is string projectName)
				{
					ProjectRemoved?.Invoke(this, new ProjectEventArgs(projectName, pRealHierarchy));
				}
			}

			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			// Solution was opened - but dependencies may not be ready yet
			// We'll wait for OnAfterBackgroundSolutionLoadComplete instead
			SolutionOpened?.Invoke(this, EventArgs.Empty);
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_watcher?.Dispose();
			SolutionClosed?.Invoke(this, EventArgs.Empty);
			return VSConstants.S_OK;
		}

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		// IVsSolutionLoadEvents implementation
		public int OnBeforeOpenSolution(string pszSolutionFilename)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeBackgroundSolutionLoadBegins()
		{
			return VSConstants.S_OK;
		}

		public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
		{
			pfShouldDelayLoadToNextIdle = false;
			return VSConstants.S_OK;
		}

		public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterBackgroundSolutionLoadComplete()
		{
#pragma warning disable VSSDK007 // Intentional fire-and-forget
			_ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					List<IVsHierarchy> hierarchies = EnumerateLoadedProjects().ToList();
					IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
					IProjectServiceAccessor accessor = componentModel.GetService<IProjectServiceAccessor>();

					_watcher?.Dispose();
					_watcher = new SolutionReferenceWatcher(accessor);

					_watcher.AllReferencesReady += OnAllReady;
					_watcher.ProjectReferencesChanged += OnProjectChanged;

					await _watcher.WatchAllProjectsAsync(hierarchies);
				});
#pragma warning restore VSTHRD110

			return VSConstants.S_OK;
		}

		private void OnAllReady(IReadOnlyList<ProjectReferences> projectReferences)
		{
			foreach (ProjectReferences proj in projectReferences)
			{
				Debug.WriteLine(proj);
				foreach (ReferenceInfo r in proj.References)
					Debug.WriteLine($"  {r}");
			}

			SolutionFullyLoaded?.Invoke(projectReferences);
		}

		private void OnProjectChanged(
			string projectName,
			IReadOnlyList<ReferenceInfo> refs)
		{
			Debug.WriteLine($"References changed in {projectName}:");
			foreach (ReferenceInfo r in refs)
				Debug.WriteLine($"  {r}");
		}

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				_watcher?.Dispose();

				ThreadHelper.JoinableTaskFactory.Run(async delegate
					{
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

						// Unsubscribe from solution events
						if (_solutionEventsCookie != 0 && _solution != null)
						{
							_solution.UnadviseSolutionEvents(_solutionEventsCookie);
							_solutionEventsCookie = 0;
						}
					});
			}

			_disposed = true;
		}

		private IEnumerable<IVsHierarchy> EnumerateLoadedProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Guid guid = Guid.Empty;
			_solution.GetProjectEnum(
				(uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
				ref guid,
				out IEnumHierarchies enumerator);

			IVsHierarchy[] hierarchy = new IVsHierarchy[1];
			while (enumerator.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
			{
				// Skip solution folders — they show up as hierarchies
				// but aren't real projects
				if (IsSolutionFolder(hierarchy[0]))
					continue;

				yield return hierarchy[0];
			}
		}

		private static bool IsSolutionFolder(IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return hierarchy.GetGuidProperty(
					VSConstants.VSITEMID_ROOT,
					(int)__VSHPROPID.VSHPROPID_TypeGuid,
					out Guid typeGuid) == VSConstants.S_OK
				&& typeGuid == _solutionFolderGuid;
		}
	}


	public class ProjectEventArgs : EventArgs
	{
		public string ProjectName { get; }
		public IVsHierarchy Hierarchy { get; }

		public ProjectEventArgs(string projectName, IVsHierarchy hierarchy)
		{
			ProjectName = projectName;
			Hierarchy = hierarchy;
		}
	}
}
