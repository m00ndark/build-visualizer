using BuildVisualizer.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

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
	}
}
