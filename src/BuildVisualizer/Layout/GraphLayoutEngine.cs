using BuildVisualizer.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace BuildVisualizer.Layout
{
	public class GraphLayoutEngine
	{
		public Dictionary<int, List<ProjectNodeViewModel>> GetOrderedLayers(List<ProjectNodeViewModel> nodes)
		{
			if (nodes == null || nodes.Count == 0)
				return new Dictionary<int, List<ProjectNodeViewModel>>();

			var layers = AssignLayers(nodes);
			MinimizeCrossings(layers, nodes);
			return layers;
		}

		private Dictionary<int, List<ProjectNodeViewModel>> AssignLayers(List<ProjectNodeViewModel> nodes)
		{
			var layers = new Dictionary<int, List<ProjectNodeViewModel>>();
			var nodeToLayer = new Dictionary<ProjectNodeViewModel, int>();

			// Find nodes with no dependencies (layer 0)
			var rootNodes = nodes.Where(n => n.ProjectInfo.Dependencies.Count == 0).ToList();

			foreach (var root in rootNodes)
			{
				nodeToLayer[root] = 0;
			}

			// Assign layers to remaining nodes using longest path
			bool changed = true;
			while (changed)
			{
				changed = false;
				foreach (var node in nodes)
				{
					if (nodeToLayer.ContainsKey(node))
						continue;

					// Check if all dependencies have been assigned layers
					var depNodes = node.DependencyNodes.Where(n => nodes.Contains(n)).ToList();
					if (depNodes.Count == 0)
					{
						// No dependencies in the graph, assign to layer 0
						nodeToLayer[node] = 0;
						changed = true;
					}
					else if (depNodes.All(d => nodeToLayer.ContainsKey(d)))
					{
						// Assign to max(dependency layers) + 1
						int maxDepLayer = depNodes.Max(d => nodeToLayer[d]);
						nodeToLayer[node] = maxDepLayer + 1;
						changed = true;
					}
				}
			}

			// Handle any remaining unassigned nodes (circular dependencies or disconnected)
			foreach (var node in nodes)
			{
				if (!nodeToLayer.ContainsKey(node))
				{
					nodeToLayer[node] = 0;
				}
			}

			// Group nodes by layer
			foreach (var kvp in nodeToLayer)
			{
				int layer = kvp.Value;
				if (!layers.ContainsKey(layer))
				{
					layers[layer] = new List<ProjectNodeViewModel>();
				}
				layers[layer].Add(kvp.Key);
			}

			return layers;
		}

		private void MinimizeCrossings(Dictionary<int, List<ProjectNodeViewModel>> layers, List<ProjectNodeViewModel> allNodes)
		{
			if (layers.Count <= 1)
				return;

			int maxLayer = layers.Keys.Max();

			// Multiple passes to reduce crossings
			for (int pass = 0; pass < 3; pass++)
			{
				// Forward pass: order each layer based on barycenter of dependencies
				for (int layer = 1; layer <= maxLayer; layer++)
				{
					if (!layers.TryGetValue(layer, out List<ProjectNodeViewModel> nodesInLayer))
						continue;

					var orderedNodes = new List<(ProjectNodeViewModel node, double barycenter)>();

					foreach (var node in nodesInLayer)
					{
						double barycenter = CalculateBarycenter(node, layers, layer - 1, allNodes);
						orderedNodes.Add((node, barycenter));
					}

					// Sort by barycenter value
					layers[layer] = orderedNodes.OrderBy(x => x.barycenter).Select(x => x.node).ToList();
				}

				// Backward pass: order each layer based on barycenter of dependents
				for (int layer = maxLayer - 1; layer >= 0; layer--)
				{
					if (!layers.TryGetValue(layer, out List<ProjectNodeViewModel> nodesInLayer))
						continue;

					var orderedNodes = new List<(ProjectNodeViewModel node, double barycenter)>();

					foreach (var node in nodesInLayer)
					{
						double barycenter = CalculateBarycenterDependents(node, layers, layer + 1);
						orderedNodes.Add((node, barycenter));
					}

					// Sort by barycenter value
					layers[layer] = orderedNodes.OrderBy(x => x.barycenter).Select(x => x.node).ToList();
				}
			}
		}

		private static double CalculateBarycenter(ProjectNodeViewModel node, Dictionary<int, List<ProjectNodeViewModel>> layers, int previousLayer, List<ProjectNodeViewModel> allNodes)
		{
			if (!layers.TryGetValue(previousLayer, out List<ProjectNodeViewModel> previousLayerNodes))
				return 0;

			var dependencies = node.DependencyNodes.Where(d => allNodes.Contains(d) && previousLayerNodes.Contains(d)).ToList();

			if (dependencies.Count == 0)
				return 0;

			// Calculate average position of dependencies in previous layer
			double sum = 0;
			foreach (var dep in dependencies)
			{
				int index = previousLayerNodes.IndexOf(dep);
				sum += index;
			}

			return sum / dependencies.Count;
		}

		private static double CalculateBarycenterDependents(ProjectNodeViewModel node, Dictionary<int, List<ProjectNodeViewModel>> layers, int nextLayer)
		{
			if (!layers.TryGetValue(nextLayer, out List<ProjectNodeViewModel> nextLayerNodes))
				return 0;

			// Find nodes in next layer that depend on this node
			var dependents = nextLayerNodes.Where(n => n.DependencyNodes.Contains(node)).ToList();

			if (dependents.Count == 0)
				return 0;

			// Calculate average position of dependents in next layer
			double sum = 0;
			foreach (var dep in dependents)
			{
				int index = nextLayerNodes.IndexOf(dep);
				sum += index;
			}

			return sum / dependents.Count;
		}
	}
}
