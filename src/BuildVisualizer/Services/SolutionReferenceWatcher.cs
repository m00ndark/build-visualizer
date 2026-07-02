using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

				ProjectMetadata projectMetadata = ProjectDataHelper.GetProjectData(hierarchy, _solution);

				UnconfiguredProject unconfigured = GetUnconfiguredProject(hierarchy);

				if (unconfigured != null)
				{
					Debug.WriteLine($"[Solution] SDK-style: {projectMetadata.Name}");

					ResolvedReferenceWatcher watcher = new ResolvedReferenceWatcher(projectMetadata);
					await watcher.SubscribeAsync(unconfigured, cancellationToken);

					watcher.ReferencesChanged += refs => ProjectReferencesChanged?.Invoke(projectMetadata.Name, refs);

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
						Debug.WriteLine($"[Solution] Legacy: {projectMetadata.Name}");

						LegacyReferenceWatcher watcher = new LegacyReferenceWatcher(projectMetadata);
						watcher.Subscribe(project);
						_legacyWatchers.Add(watcher);
						readyTasks.Add(watcher.WaitForReferencesAsync(cancellationToken));
					}
					else
					{
						Debug.WriteLine($"[Solution] Skipping (no ref model): {projectMetadata.Name}");
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

		internal IReadOnlyList<ProjectReferences> CollectAllReferences()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<ProjectReferences> results = new List<ProjectReferences>();
			int i = 0;

			foreach (ResolvedReferenceWatcher watcher in _cpsWatchers)
			{
				results.Add(new ProjectReferences
					{
						ProjectName = watcher.ProjectMetadata.Name,
						ProjectUniqueName = watcher.ProjectMetadata.UniqueName,
						ProjectPath = watcher.ProjectMetadata.Path,
						ProjectStyle = "SDK",
						OutputType = watcher.ProjectMetadata.OutputType,
						Configuration = watcher.ProjectMetadata.Configuration,
						Platform = watcher.ProjectMetadata.Platform,
						References = watcher.GetCurrentReferences()
					});
				i++;
			}

			foreach (LegacyReferenceWatcher watcher in _legacyWatchers)
			{
				results.Add(new ProjectReferences
					{
						ProjectName = watcher.ProjectMetadata.Name,
						ProjectUniqueName = watcher.ProjectMetadata.UniqueName,
						ProjectPath = watcher.ProjectMetadata.Path,
						ProjectStyle = "Legacy",
						OutputType = watcher.ProjectMetadata.OutputType,
						Configuration = watcher.ProjectMetadata.Configuration,
						Platform = watcher.ProjectMetadata.Platform,
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