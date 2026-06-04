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
				ProjectInfo projectInfo = new ProjectInfo(projectRef.ProjectName, projectRef.ProjectUniqueName, projectRef.ProjectPath, projectRef.OutputType);
				projects.Add(projectInfo);
				projectDict[projectRef.ProjectPath] = projectInfo;
			}

			// Populate dependencies based on project references
			foreach (ProjectReferences projectRef in _cachedProjectReferences)
			{
				if (!projectDict.TryGetValue(projectRef.ProjectPath, out ProjectInfo project))
					continue;

				foreach (ReferenceInfo reference in projectRef.References)
				{
					// Only process project references
					if (reference.ReferenceKind != ReferenceKind.Project)
						continue;

					FileInfo referencedProjectFile = new FileInfo(Path.Combine(project.ProjectDirectory, reference.OriginalItemSpec));
					string referencedProjectPath = referencedProjectFile.FullName.ToLowerInvariant();

					if (!projectDict.TryGetValue(referencedProjectPath, out ProjectInfo referencedProject)) 
						continue;

					// Add to current project's dependencies
					if (!project.Dependencies.Contains(referencedProject))
					{
						project.Dependencies.Add(referencedProject);
					}

					// Find the referenced project and add current project to its dependents
					if (!referencedProject.Dependents.Contains(project))
					{
						referencedProject.Dependents.Add(project);
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
