using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BuildVisualizer.Services
{
	public sealed class SolutionReferenceWatcher : IDisposable
	{
		private readonly IVsSolution _solution;
		private readonly IProjectServiceAccessor _projectServiceAccessor;
		private readonly List<ResolvedReferenceWatcher> _cpsWatchers = new List<ResolvedReferenceWatcher>();
		private readonly List<LegacyReferenceWatcher> _legacyWatchers = new List<LegacyReferenceWatcher>();
		private bool _disposed;

		/// <summary>
		/// Fired once when all projects' references are fully resolved.
		/// </summary>
		public event Action<IReadOnlyList<ProjectReferences>> AllReferencesReady;

		/// <summary>
		/// Fired whenever any project's references change after the
		/// initial resolution.
		/// </summary>
		public event Action<string, IReadOnlyList<ReferenceInfo>> ProjectReferencesChanged;

		public SolutionReferenceWatcher(IVsSolution solution, IProjectServiceAccessor projectServiceAccessor)
		{
			_solution = solution ?? throw new ArgumentNullException(nameof(solution));

			_projectServiceAccessor = projectServiceAccessor
				?? throw new ArgumentNullException(nameof(projectServiceAccessor));
		}

		/// <summary>
		/// Starts watching all provided hierarchies. Completes when
		/// every project's references are resolved.
		/// </summary>
		public async Task WatchAllProjectsAsync(
			IEnumerable<IVsHierarchy> hierarchies,
			CancellationToken cancellationToken = default)
		{
			List<Task> readyTasks = new List<Task>();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			foreach (IVsHierarchy hierarchy in hierarchies)
			{
				cancellationToken.ThrowIfCancellationRequested();

				(string projectName, string projectUniqueName, string projectPath) = GetProjectData(hierarchy);

				UnconfiguredProject unconfigured = GetUnconfiguredProject(hierarchy);

				if (unconfigured != null)
				{
					Debug.WriteLine($"[Solution] SDK-style: {projectName}");

					ResolvedReferenceWatcher watcher = new ResolvedReferenceWatcher(projectName, projectUniqueName, projectPath);
					await watcher.SubscribeAsync(unconfigured, cancellationToken);

					watcher.ReferencesChanged += refs => ProjectReferencesChanged?.Invoke(projectName, refs);

					_cpsWatchers.Add(watcher);
					readyTasks.Add(watcher.ReferencesReady);
				}
				else
				{
					hierarchy.GetProperty(
						VSConstants.VSITEMID_ROOT,
						(int)__VSHPROPID.VSHPROPID_ExtObject,
						out object extObject);

					if (extObject is Project project && project.Object is VSLangProj.VSProject)
					{
						Debug.WriteLine($"[Solution] Legacy: {projectName}");

						LegacyReferenceWatcher watcher = new LegacyReferenceWatcher(projectName, projectUniqueName, projectPath);
						watcher.Subscribe(project);
						_legacyWatchers.Add(watcher);
						readyTasks.Add(watcher.WaitForReferencesAsync(cancellationToken));
					}
					else
					{
						Debug.WriteLine($"[Solution] Skipping (no ref model): {projectName}");
					}
				}
			}

			Debug.WriteLine($"[Solution] Waiting for {readyTasks.Count} project(s)...");

			await Task.WhenAll(readyTasks);

			Debug.WriteLine("[Solution] All references resolved.");

			// Collect final state
			IReadOnlyList<ProjectReferences> allRefs = CollectAllReferences();
			AllReferencesReady?.Invoke(allRefs);
		}

		private IReadOnlyList<ProjectReferences> CollectAllReferences()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<ProjectReferences> results = new List<ProjectReferences>();
			int i = 0;

			foreach (ResolvedReferenceWatcher watcher in _cpsWatchers)
			{
				results.Add(new ProjectReferences
					{
						ProjectName = watcher.ProjectName,
						ProjectUniqueName = watcher.ProjectUniqueName,
						ProjectPath = watcher.ProjectPath,
						ProjectStyle = "SDK",
						References = watcher.GetCurrentReferences()
					});
				i++;
			}

			foreach (LegacyReferenceWatcher watcher in _legacyWatchers)
			{
				results.Add(new ProjectReferences
					{
						ProjectName = watcher.ProjectName,
						ProjectUniqueName = watcher.ProjectUniqueName,
						ProjectPath = watcher.ProjectPath,
						ProjectStyle = "Legacy",
						References = watcher.GetCurrentReferences()
					});
				i++;
			}

			return results;
		}

		private UnconfiguredProject GetUnconfiguredProject(
			IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			hierarchy.GetCanonicalName(
				VSConstants.VSITEMID_ROOT, out string projectPath);

			if (string.IsNullOrEmpty(projectPath))
				return null;

			IProjectService projectService = _projectServiceAccessor.GetProjectService();

			return projectService?
				.LoadedUnconfiguredProjects
				.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals((string)p.FullPath, projectPath));
		}

		private (string Name, string UniqueName, string Path) GetProjectData(IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			hierarchy.GetProperty(
				VSConstants.VSITEMID_ROOT,
				(int)__VSHPROPID.VSHPROPID_Name,
				out object nameObj);

			_solution.GetUniqueNameOfProject(hierarchy, out string uniqueName);

			hierarchy.GetCanonicalName(
				VSConstants.VSITEMID_ROOT, out string path);

			return (nameObj as string ?? "(unknown)",
				uniqueName ?? "(unknown)",
				path ?? string.Empty);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ResolvedReferenceWatcher w in _cpsWatchers) w.Dispose();
			foreach (LegacyReferenceWatcher w in _legacyWatchers) w.Dispose();
			_cpsWatchers.Clear();
			_legacyWatchers.Clear();
		}
	}
}