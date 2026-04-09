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
				if (project != null)
				{
					var projectInfo = new ProjectInfo(project.Name, project.UniqueName);
					projects.Add(projectInfo);
				}
			}

			return projects;
		}

		public void ParseProjectDependencies(List<ProjectInfo> projects)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_dte?.Solution == null || projects == null)
			{
				return;
			}

			// Create a dictionary for fast lookup
			var projectDict = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var proj in projects)
			{
				projectDict[proj.UniqueName] = proj;
			}

			// Iterate through all projects in the solution
			foreach (Project project in _dte.Solution.Projects)
			{
				if (project == null)
					continue;

				// Find corresponding ProjectInfo
				if (!projectDict.TryGetValue(project.UniqueName, out var projectInfo))
					continue;

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
									var referencedProject = projectDict.Values
										.FirstOrDefault(p => string.Equals(p.Name, referencedProjectName, StringComparison.OrdinalIgnoreCase));

									if (referencedProject != null)
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
					// Skip projects that can't be cast to VSProject (e.g., C++ projects, solution folders)
				}
			}
		}
	}
}
