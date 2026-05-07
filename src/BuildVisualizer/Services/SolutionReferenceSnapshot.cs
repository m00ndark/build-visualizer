using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using VSLangProj;

namespace BuildVisualizer.Services
{
	public sealed class SolutionReferenceSnapshot
	{
		private static readonly Guid SolutionFolderGuid = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");

		private readonly IVsSolution _solution;

		public SolutionReferenceSnapshot(IVsSolution solution)
		{
			_solution = solution ?? throw new ArgumentNullException(nameof(solution));
		}

		public async Task<IReadOnlyList<ProjectReferences>> GetAllProjectReferencesAsync(
			CancellationToken cancellationToken)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			List<IVsHierarchy> hierarchies = EnumerateLoadedProjects();
			List<ProjectReferences> results = new List<ProjectReferences>();

			foreach (IVsHierarchy hierarchy in hierarchies)
			{
				cancellationToken.ThrowIfCancellationRequested();

				(string projectName, string projectUniqueName, string projectPath) = GetProjectData(hierarchy);

				UnconfiguredProject unconfigured =
					GetUnconfiguredProject(projectPath);

				if (unconfigured != null)
				{
					IReadOnlyList<ReferenceInfo> refs =
						await GetSdkDirectReferencesAsync(
							unconfigured, cancellationToken);

					results.Add(new ProjectReferences
						{
							ProjectName = projectName,
							ProjectUniqueName = projectUniqueName,
							ProjectPath = projectPath,
							ProjectStyle = "SDK",
							References = refs
						});

					Debug.WriteLine($"[Snapshot] SDK '{projectName}': {refs.Count} direct ref(s)");
				}
				else
				{
					IReadOnlyList<ReferenceInfo> refs =
						GetLegacyDirectReferences(hierarchy);

					if (refs != null)
					{
						results.Add(new ProjectReferences
							{
								ProjectName = projectName,
								ProjectUniqueName = projectUniqueName,
								ProjectPath = projectPath,
								ProjectStyle = "Legacy",
								References = refs
							});

						Debug.WriteLine($"[Snapshot] Legacy '{projectName}': {refs.Count} direct ref(s)");
					}
					else
					{
						Debug.WriteLine($"[Snapshot] Skipping '{projectName}' (no ref model)");
					}
				}
			}

			return results;
		}

		private async Task<IReadOnlyList<ReferenceInfo>> GetSdkDirectReferencesAsync(
			UnconfiguredProject unconfiguredProject,
			CancellationToken cancellationToken)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			ConfiguredProject configuredProject = await unconfiguredProject
				.GetSuggestedConfiguredProjectAsync();

			IProjectSubscriptionService subscriptionService = configuredProject
				.Services
				.ExportProvider
				.GetExportedValue<IProjectSubscriptionService>();

			// Get evaluation snapshot — declared ProjectReference items
			IProjectRuleSnapshot evaluationSnapshot =
				await GetLatestSnapshotAsync(
					subscriptionService.ProjectRuleSource,
					"ProjectReference",
					cancellationToken);

			// Get resolved snapshot — all resolved project references
			IProjectRuleSnapshot resolvedSnapshot =
				await GetLatestSnapshotAsync(
					subscriptionService.JointRuleSource,
					"ResolvedProjectReference",
					cancellationToken);

			if (evaluationSnapshot == null || resolvedSnapshot == null)
			{
				return Array.Empty<ReferenceInfo>();
			}

			string projectDirectory = Path.GetDirectoryName(unconfiguredProject.FullPath);

			// Cross-reference to filter to direct only
			HashSet<string> directPaths = new HashSet<string>(
				evaluationSnapshot.Items.Keys,
				StringComparer.OrdinalIgnoreCase);

			List<ReferenceInfo> results = new List<ReferenceInfo>();

			foreach (KeyValuePair<string, IImmutableDictionary<string, string>> kvp in resolvedSnapshot.Items)
			{
				string itemName = kvp.Key;
				IImmutableDictionary<string, string> metadata = kvp.Value;

				if (!metadata.TryGetValue("OriginalItemSpec", out string originalSpec))
				{
					originalSpec = itemName;
				}

				if (!directPaths.Contains(originalSpec) && !directPaths.Contains(itemName))
				{
					continue;
				}

				if (!metadata.TryGetValue("ResolvedPath", out string resolvedPath))
				{
					resolvedPath = string.Empty;
				}

				metadata.TryGetValue("Version", out string version);

				string projectPath = ResolveReferencedProjectPath(metadata, originalSpec, projectDirectory);

				results.Add(new ReferenceInfo
					{
						Name = itemName,
						Path = resolvedPath,
						IsResolved = metadata.ContainsKey("ResolvedPath"),
						Version = version,
						ReferenceKind = ReferenceKind.Project,
						OriginalItemSpec = originalSpec
					});
			}

			return results;
		}

		private static string ResolveReferencedProjectPath(
			IImmutableDictionary<string, string> metadata,
			string originalItemSpec,
			string projectDirectory)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Strategy 1: Use MSBuildSourceProjectFile if available
			if (metadata.TryGetValue("MSBuildSourceProjectFile", out string projectFilePath)
				&& !string.IsNullOrEmpty(projectFilePath))
			{
				return projectFilePath;
			}

			// Strategy 2: Resolve OriginalItemSpec against project directory
			if (!string.IsNullOrEmpty(originalItemSpec) && !string.IsNullOrEmpty(projectDirectory))
			{
				try
				{
					return Path.IsPathRooted(originalItemSpec)
						? originalItemSpec
						: Path.GetFullPath(Path.Combine(projectDirectory, originalItemSpec));
				}
				catch (ArgumentException ex)
				{
					Debug.WriteLine($"[Snapshot] Could not resolve path '{originalItemSpec}': {ex.Message}");
				}
			}

			return null;
		}

		private static async Task<IProjectRuleSnapshot> GetLatestSnapshotAsync(
			IProjectValueDataSource<IProjectSubscriptionUpdate> dataSource,
			string ruleName,
			CancellationToken cancellationToken)
		{
			TaskCompletionSource<IProjectRuleSnapshot> tcs =
				new TaskCompletionSource<IProjectRuleSnapshot>();

			ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> actionBlock =
				new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
						update =>
							{
								bool gotValue = update.Value.ProjectChanges
									.TryGetValue(ruleName, out IProjectChangeDescription change);

								tcs.TrySetResult(gotValue ? change.After : null);
							});

			IDisposable link = dataSource.SourceBlock.LinkTo(
				actionBlock,
				new DataflowLinkOptions { PropagateCompletion = true },
				initialDataAsNew: true,
				suppressVersionOnlyUpdates: true,
				ruleNames: ImmutableHashSet<string>.Empty.Add(ruleName));

			try
			{
				using (cancellationToken.Register(() => tcs.TrySetCanceled()))
				{
					return await tcs.Task;
				}
			}
			finally
			{
				link.Dispose();
			}
		}

		private IReadOnlyList<ReferenceInfo> GetLegacyDirectReferences(
			IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			int hr = hierarchy.GetProperty(
				VSConstants.VSITEMID_ROOT,
				(int)__VSHPROPID.VSHPROPID_ExtObject,
				out object extObject);

			if (hr != VSConstants.S_OK)
			{
				return null;
			}

			if (!(extObject is Project project))
			{
				return null;
			}

			if (!(project.Object is VSProject vsProject))
			{
				return null;
			}

			List<ReferenceInfo> results = new List<ReferenceInfo>();

			try
			{
				foreach (Reference r in vsProject.References)
				{
					// In legacy projects, only project references
					// have a SourceProject property
					if (r.SourceProject == null)
					{
						continue;
					}

					string version;
					try
					{
						version = r.Version;
					}
					catch
					{
						version = null;
					}

					results.Add(new ReferenceInfo
						{
							Name = r.Name,
							Path = r.Path,
							IsResolved = !string.IsNullOrEmpty(r.Path),
							Version = version,
							ReferenceKind = ReferenceKind.Project,
							OriginalItemSpec = r.Name
						});
				}
			}
			catch (COMException ex)
			{
				Debug.WriteLine($"[Snapshot] COM error reading legacy references: {ex.Message}");
				return null;
			}

			return results;
		}

		private List<IVsHierarchy> EnumerateLoadedProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Guid guid = Guid.Empty;
			_solution.GetProjectEnum(
				(uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
				ref guid,
				out IEnumHierarchies enumerator);

			List<IVsHierarchy> results = new List<IVsHierarchy>();
			IVsHierarchy[] hierarchy = new IVsHierarchy[1];

			while (enumerator.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK
				&& fetched == 1)
			{
				if (!IsSolutionFolder(hierarchy[0]))
				{
					results.Add(hierarchy[0]);
				}
			}

			return results;
		}

		private static bool IsSolutionFolder(IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return hierarchy.GetGuidProperty(
					VSConstants.VSITEMID_ROOT,
					(int)__VSHPROPID.VSHPROPID_TypeGuid,
					out Guid typeGuid) == VSConstants.S_OK
				&& typeGuid == SolutionFolderGuid;
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

		private static UnconfiguredProject GetUnconfiguredProject(string projectPath)
		{
			if (string.IsNullOrEmpty(projectPath))
			{
				return null;
			}

			IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
			IProjectServiceAccessor projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();

			IProjectService projectService = projectServiceAccessor.GetProjectService();

			return projectService
				.LoadedUnconfiguredProjects
				.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals((string)p.FullPath, projectPath));
		}
	}
}
