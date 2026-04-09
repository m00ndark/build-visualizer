using BuildVisualizer.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildVisualizer.Services
{
	public class SolutionService
	{
		private static readonly string[] _solutionFolderKinds =
			{
				"{66A26720-8FB5-11D2-AA7E-00C04F688DDE}", // solution folder for web projects
				"{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}", // generic solution folder
			};

		private readonly DTE2 _dte;

		public SolutionService(DTE2 dte)
		{
			_dte = dte;
		}

		public List<ProjectInfo> GetProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var projects = new List<ProjectInfo>();

			if (_dte?.Solution == null)
			{
				return projects;
			}

			foreach (Project project in _dte.Solution.Projects)
			{
				GetProjectsRecursive(project, projects);
			}

			return projects;
		}

		private void GetProjectsRecursive(Project project, List<ProjectInfo> projects)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (project == null)
			{
				return;
			}

			try
			{
				// Check if this is a solution folder
				if (_solutionFolderKinds.Contains(project.Kind))
				{
					// Recursively process items in the solution folder
					if (project.ProjectItems != null)
					{
						foreach (ProjectItem item in project.ProjectItems)
						{
							if (item.SubProject != null)
							{
								GetProjectsRecursive(item.SubProject, projects);
							}
						}
					}
				}
				else
				{
					// This is a real project, add it
					var projectInfo = new ProjectInfo(project.Name, project.UniqueName);
					projects.Add(projectInfo);
				}
			}
			catch (Exception)
			{
				// Skip projects that can't be accessed
			}
		}

		public void ParseProjectDependencies(List<ProjectInfo> projects)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_dte?.Solution == null || projects == null)
			{
				return;
			}

			// Create dictionaries for fast lookup by both UniqueName and Name
			var projectDictByUniqueName = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
			var projectDictByName = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);

			foreach (var proj in projects)
			{
				projectDictByUniqueName[proj.UniqueName] = proj;
				projectDictByName[proj.Name] = proj;
			}

			// Iterate through all real projects in the solution
			foreach (Project project in _dte.Solution.Projects)
			{
				ParseProjectDependenciesRecursive(project, projectDictByUniqueName, projectDictByName);
			}
		}

		private void ParseProjectDependenciesRecursive(Project project, 
			Dictionary<string, ProjectInfo> projectDictByUniqueName,
			Dictionary<string, ProjectInfo> projectDictByName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (project == null)
			{
				return;
			}

			try
			{
				// Check if this is a solution folder
				if (_solutionFolderKinds.Contains(project.Kind))
				{
					// Recursively process items in the solution folder
					if (project.ProjectItems != null)
					{
						foreach (ProjectItem item in project.ProjectItems)
						{
							if (item.SubProject != null)
							{
								ParseProjectDependenciesRecursive(item.SubProject, projectDictByUniqueName, projectDictByName);
							}
						}
					}
					return;
				}

				// Find corresponding ProjectInfo
				if (!projectDictByUniqueName.TryGetValue(project.UniqueName, out var projectInfo))
					return;

				try
				{
					// Try to get VSProject (for C# and VB.NET projects)
					var vsProject = project.Object as VSLangProj.VSProject;
					if (vsProject != null)
					{
						// Iterate through references
						foreach (VSLangProj.Reference reference in vsProject.References)
						{
							try
							{
								// Check if this is a project reference
								if (reference.SourceProject != null)
								{
									var referencedProjectName = reference.SourceProject.Name;

									// Add to current project's dependencies
									if (!projectInfo.Dependencies.Contains(referencedProjectName))
									{
										projectInfo.Dependencies.Add(referencedProjectName);
									}

									// Find the referenced project and add current project to its dependents
									if (projectDictByName.TryGetValue(referencedProjectName, out var referencedProject))
									{
										if (!referencedProject.Dependents.Contains(projectInfo.Name))
										{
											referencedProject.Dependents.Add(projectInfo.Name);
										}
									}
								}
							}
							catch (Exception)
							{
								// Skip references that can't be accessed
							}
						}
					}
				}
				catch (Exception)
				{
					// Skip projects that can't be cast to VSProject (e.g., C++ projects)
				}
			}
			catch (Exception)
			{
				// Skip projects that can't be accessed
			}
		}
	}
}
