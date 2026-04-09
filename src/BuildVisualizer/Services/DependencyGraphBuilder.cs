using BuildVisualizer.Models;
using BuildVisualizer.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
			var nodeMap = new Dictionary<string, ProjectNodeViewModel>();
			foreach (var project in projects)
			{
				nodeMap[project.Name] = new ProjectNodeViewModel(project);
			}

			// Track which nodes are children of other nodes
			var childNodes = new HashSet<string>();

			// Build the hierarchy by processing dependencies
			foreach (var project in projects)
			{
				var currentNode = nodeMap[project.Name];

				// For each dependency, add the current project to that dependency's children
				// This creates a "who depends on me" hierarchy
				foreach (var dependencyName in project.Dependencies)
				{
					if (nodeMap.TryGetValue(dependencyName, out var dependencyNode))
					{
						// Add current node as a child of its dependency
						dependencyNode.Children.Add(currentNode);
						childNodes.Add(project.Name);
					}
				}
			}

			// Root nodes are those that are NOT children of any other node
			var rootNodes = new ObservableCollection<ProjectNodeViewModel>();
			foreach (var kvp in nodeMap)
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
				foreach (var node in nodeMap.Values)
				{
					rootNodes.Add(node);
				}
			}

			return rootNodes;
		}
	}
}
