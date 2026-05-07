using BuildVisualizer.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BuildVisualizer.Services
{
	public class SolutionService
	{
		private readonly SolutionReferenceSnapshot _snapshot;
		private IReadOnlyList<ProjectReferences> _cachedProjectReferences;

		public SolutionService(SolutionReferenceSnapshot snapshot)
		{
			_snapshot = snapshot;
		}

		public async Task<List<ProjectInfo>> GetProjectsAsync(CancellationToken cancellationToken = default)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			List<ProjectInfo> projects = new List<ProjectInfo>();

			// TODO: Make project and reference loading work,
			// TODO: then continue on the implementation plan

			if (_cachedProjectReferences == null)
			{
				await LoadProjectReferencesAsync(cancellationToken);
			}

			if (_cachedProjectReferences == null || _cachedProjectReferences.Count == 0)
			{
				return projects;
			}

			// Create ProjectInfo objects from cached ProjectReferences
			Dictionary<string, ProjectInfo> projectDict = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);

			foreach (ProjectReferences projectRef in _cachedProjectReferences)
			{
				ProjectInfo projectInfo = new ProjectInfo(projectRef.ProjectName, projectRef.ProjectPath);
				projects.Add(projectInfo);
				projectDict[projectRef.ProjectPath] = projectInfo;
			}

			// Populate dependencies based on project references
			foreach (ProjectReferences projectRef in _cachedProjectReferences)
			{
				if (!projectDict.TryGetValue(projectRef.ProjectPath, out ProjectInfo projectInfo))
					continue;

				string projectPath = Path.GetDirectoryName(projectRef.ProjectPath);

				if (projectPath == null)
					continue;

				foreach (ReferenceInfo reference in projectRef.References)
				{
					// Only process project references
					if (reference.ReferenceKind != ReferenceKind.Project)
						continue;

					FileInfo referencedProjectFile = new FileInfo(Path.Combine(projectPath, reference.OriginalItemSpec));
					string referencedProjectName = referencedProjectFile.FullName.ToLowerInvariant();

					// Add to current project's dependencies
					if (!projectInfo.Dependencies.Contains(referencedProjectName))
					{
						projectInfo.Dependencies.Add(referencedProjectName);
					}

					// Find the referenced project and add current project to its dependents
					if (projectDict.TryGetValue(referencedProjectName, out ProjectInfo referencedProject))
					{
						if (!referencedProject.Dependents.Contains(projectInfo.Name))
						{
							referencedProject.Dependents.Add(projectInfo.Name);
						}
					}
				}
			}

			return projects;
		}

		public async Task LoadProjectReferencesAsync(CancellationToken cancellationToken = default)
		{
			if (_snapshot == null)
				return;

			_cachedProjectReferences = await _snapshot.GetAllProjectReferencesAsync(cancellationToken);
		}

		public void UpdateProjectReferences(IReadOnlyList<ProjectReferences> projectReferences)
		{
			_cachedProjectReferences = projectReferences;
		}
	}
}
