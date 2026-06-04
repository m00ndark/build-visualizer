using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BuildVisualizer.Services
{
	public class SolutionEventsService : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
	{
		private static readonly Guid _solutionFolderGuid = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");

		private readonly IVsSolution _solution;
		private uint _solutionEventsCookie;
		private bool _disposed;
		private SolutionReferenceWatcher _watcher;

		/// <summary>
		/// True when a solution is open (set on open, cleared on close).
		/// </summary>
		public bool IsSolutionOpen { get; private set; }

		/// <summary>
		/// The most recently resolved project references, or null if no solution
		/// has been fully loaded yet (or the solution was closed).
		/// </summary>
		public IReadOnlyList<ProjectReferences> LastResolvedReferences { get; private set; }

		public event EventHandler SolutionOpened;
		public event Action<IReadOnlyList<ProjectReferences>> SolutionFullyLoaded;
		public event EventHandler SolutionClosed;

		public SolutionEventsService(IVsSolution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_solution = solution ?? throw new ArgumentNullException(nameof(solution));

			// Subscribe to solution events (IVsSolutionEvents)
			// Since we also implement IVsSolutionLoadEvents, VS should detect this
			// and call those methods as well when appropriate
			_solution.AdviseSolutionEvents(this, out _solutionEventsCookie);

			// If a solution is already open (e.g. package loaded asynchronously after
			// solution events already fired), catch up by triggering the same logic
			// that OnAfterBackgroundSolutionLoadComplete would have run.
			if (IsSolutionAlreadyOpen())
			{
				IsSolutionOpen = true;
				OnAfterBackgroundSolutionLoadComplete();
			}
		}

		private bool IsSolutionAlreadyOpen()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value);
			return value is bool isOpen && isOpen;
		}

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (fAdded != 0)
			{
				// Project was added — rebuild the watcher to include it
				RebuildWatcher();
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
				// Project is being removed — rebuild the watcher, excluding
				// this hierarchy since it's still enumerable at this point
				RebuildWatcher(excludeHierarchy: pHierarchy);
			}

			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Project was loaded (e.g., after being unloaded) — rebuild the watcher
			RebuildWatcher();
			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Project is being unloaded — rebuild the watcher, excluding
			// this hierarchy since it's still enumerable at this point
			RebuildWatcher(excludeHierarchy: pRealHierarchy);
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			// Solution was opened - but dependencies may not be ready yet
			// We'll wait for OnAfterBackgroundSolutionLoadComplete instead
			IsSolutionOpen = true;
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

			IsSolutionOpen = false;
			LastResolvedReferences = null;
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
			ThreadHelper.ThrowIfNotOnUIThread();

			RebuildWatcher();
			return VSConstants.S_OK;
		}

		private void RebuildWatcher(IVsHierarchy excludeHierarchy = null)
		{
#pragma warning disable VSSDK007 // Intentional fire-and-forget
			_ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					List<IVsHierarchy> hierarchies = EnumerateLoadedProjects()
						.Where(h => h != excludeHierarchy)
						.ToList();
					IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
					IProjectServiceAccessor accessor = componentModel.GetService<IProjectServiceAccessor>();

					_watcher?.Dispose();
					_watcher = new SolutionReferenceWatcher(_solution, accessor);

					_watcher.AllReferencesReady += OnAllReady;
					_watcher.ProjectReferencesChanged += OnProjectChanged;

					await _watcher.WatchAllProjectsAsync(hierarchies);
				});
#pragma warning restore VSTHRD110
		}

		private void OnAllReady(IReadOnlyList<ProjectReferences> projectReferences)
		{
			foreach (ProjectReferences proj in projectReferences)
			{
				Debug.WriteLine(proj);
				foreach (ReferenceInfo r in proj.References)
					Debug.WriteLine($"  {r}");
			}

			LastResolvedReferences = projectReferences;
			SolutionFullyLoaded?.Invoke(projectReferences);
		}

		private void OnProjectChanged(
			string projectName,
			IReadOnlyList<ReferenceInfo> refs)
		{
			Debug.WriteLine($"References changed in {projectName}:");
			foreach (ReferenceInfo r in refs)
				Debug.WriteLine($"  {r}");

			// Re-collect all references and notify listeners so the UI updates
			if (_watcher != null)
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					IReadOnlyList<ProjectReferences> allRefs = _watcher.CollectAllReferences();
					LastResolvedReferences = allRefs;
					SolutionFullyLoaded?.Invoke(allRefs);
				});
			}
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
}
