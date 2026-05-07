using BuildVisualizer.Models;
using BuildVisualizer.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BuildVisualizer.Services
{
	public class DependencyGraphBuilder
	{
		public ObservableCollection<ProjectNodeViewModel> BuildHierarchy(List<ProjectInfo> projects)
		{
			if (projects == null || projects.Count == 0)
			{
				return new ObservableCollection<ProjectNodeViewModel>();
			}

			// Create a dictionary mapping project names to ProjectNodeViewModel instances
			Dictionary<string, ProjectNodeViewModel> nodeMap = new Dictionary<string, ProjectNodeViewModel>();
			foreach (ProjectInfo project in projects)
			{
				nodeMap[project.ProjectPath] = new ProjectNodeViewModel(project);
			}

			// Track which nodes are children of other nodes
			HashSet<string> childNodes = new HashSet<string>();

			// Build the hierarchy by processing dependencies
			foreach (ProjectInfo project in projects)
			{
				ProjectNodeViewModel currentNode = nodeMap[project.ProjectPath];

				// For each dependency, add the current project to that dependency's children
				// This creates a "who depends on me" hierarchy
				foreach (ProjectInfo dependencyProject in project.Dependencies)
				{
					if (nodeMap.TryGetValue(dependencyProject.ProjectPath, out ProjectNodeViewModel dependencyNode))
					{
						// Add current node as a child of its dependency
						dependencyNode.Children.Add(currentNode);
						childNodes.Add(project.ProjectPath);
					}
				}
			}

			// Root nodes are those that are NOT children of any other node
			ObservableCollection<ProjectNodeViewModel> rootNodes = new ObservableCollection<ProjectNodeViewModel>();
			foreach (KeyValuePair<string, ProjectNodeViewModel> kvp in nodeMap)
			{
				if (!childNodes.Contains(kvp.Key))
				{
					rootNodes.Add(kvp.Value);
				}
			}

			// If no root nodes found (circular dependencies or all are dependencies),
			// include all nodes as roots
			if (rootNodes.Count == 0 && nodeMap.Count > 0)
			{
				foreach (ProjectNodeViewModel node in nodeMap.Values)
				{
					rootNodes.Add(node);
				}
			}

			return rootNodes;
		}
	}
}
