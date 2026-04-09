using BuildVisualizer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildVisualizer.Layout
{
	public class GraphLayoutEngine
	{
		private const double NodeHorizontalSpacing = 150;
		private const double NodeVerticalSpacing = 120;
		private const double NodeWidth = 120;
		private const double NodeHeight = 60;

		public (double Width, double Height) CalculateLayout(List<ProjectNodeViewModel> nodes, double canvasWidth = 800)
		{
			if (nodes == null || nodes.Count == 0)
			{
				return (canvasWidth, 200);
			}

			// Phase 1: Assign layers using topological sort with longest path
			var layers = AssignLayers(nodes);

			// Phase 2: Minimize crossings using barycenter heuristic
			MinimizeCrossings(layers, nodes);

			// Phase 3: Assign X coordinates based on dependencies
			AssignCoordinates(layers);

			// Calculate required canvas size
			double maxX = nodes.Max(n => n.X + NodeWidth);
			double maxY = nodes.Max(n => n.Y + NodeHeight);

			return (maxX + 50, maxY + 50); // Add padding
		}

		private Dictionary<int, List<ProjectNodeViewModel>> AssignLayers(List<ProjectNodeViewModel> nodes)
		{
			var layers = new Dictionary<int, List<ProjectNodeViewModel>>();
			var nodeToLayer = new Dictionary<ProjectNodeViewModel, int>();

			// Find nodes with no dependencies (layer 0)
			var rootNodes = nodes.Where(n => n.ProjectData.Dependencies.Count == 0).ToList();

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
					if (!layers.ContainsKey(layer))
						continue;

					var nodesInLayer = layers[layer];
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
					if (!layers.ContainsKey(layer))
						continue;

					var nodesInLayer = layers[layer];
					var orderedNodes = new List<(ProjectNodeViewModel node, double barycenter)>();

					foreach (var node in nodesInLayer)
					{
						double barycenter = CalculateBarycenterDependents(node, layers, layer + 1, allNodes);
						orderedNodes.Add((node, barycenter));
					}

					// Sort by barycenter value
					layers[layer] = orderedNodes.OrderBy(x => x.barycenter).Select(x => x.node).ToList();
				}
			}
		}

		private double CalculateBarycenter(ProjectNodeViewModel node, Dictionary<int, List<ProjectNodeViewModel>> layers, int previousLayer, List<ProjectNodeViewModel> allNodes)
		{
			if (!layers.ContainsKey(previousLayer))
				return 0;

			var previousLayerNodes = layers[previousLayer];
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

		private double CalculateBarycenterDependents(ProjectNodeViewModel node, Dictionary<int, List<ProjectNodeViewModel>> layers, int nextLayer, List<ProjectNodeViewModel> allNodes)
		{
			if (!layers.ContainsKey(nextLayer))
				return 0;

			var nextLayerNodes = layers[nextLayer];

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

		private void AssignCoordinates(Dictionary<int, List<ProjectNodeViewModel>> layers)
		{
			int maxLayer = layers.Keys.Max();

			for (int layer = 0; layer <= maxLayer; layer++)
			{
				if (!layers.ContainsKey(layer))
					continue;

				var nodesInLayer = layers[layer];
				double yPosition = layer * NodeVerticalSpacing;

				// Calculate X positions centered on dependencies
				for (int i = 0; i < nodesInLayer.Count; i++)
				{
					var node = nodesInLayer[i];
					double xPosition;

					if (layer == 0)
					{
						// Root nodes: evenly spaced
						xPosition = i * NodeHorizontalSpacing;
					}
					else
					{
						// Calculate preferred X position based on dependencies
						var dependencies = node.DependencyNodes.Where(d => d.Y < yPosition).ToList();

						if (dependencies.Count > 0)
						{
							// Center on average X position of dependencies
							double avgDepX = dependencies.Average(d => d.X);
							xPosition = avgDepX;

							// Adjust to avoid overlapping with neighbors
							if (i > 0)
							{
								double minX = nodesInLayer[i - 1].X + NodeHorizontalSpacing;
								xPosition = Math.Max(xPosition, minX);
							}
						}
						else
						{
							// No dependencies in previous layers, use spacing
							xPosition = i * NodeHorizontalSpacing;
						}
					}

					node.X = xPosition;
					node.Y = yPosition;
				}

				// Second pass: spread out nodes if they're too clustered
				if (nodesInLayer.Count > 1)
				{
					BalanceLayerSpacing(nodesInLayer);
				}
			}
		}

		private void BalanceLayerSpacing(List<ProjectNodeViewModel> nodesInLayer)
		{
			// Check for overlapping nodes and adjust spacing
			for (int i = 1; i < nodesInLayer.Count; i++)
			{
				var prevNode = nodesInLayer[i - 1];
				var currNode = nodesInLayer[i];

				double minDistance = NodeHorizontalSpacing * 0.8; // Allow some compression
				double currentDistance = currNode.X - prevNode.X;

				if (currentDistance < minDistance)
				{
					// Shift current node and all following nodes to the right
					double shift = minDistance - currentDistance;
					for (int j = i; j < nodesInLayer.Count; j++)
					{
						nodesInLayer[j].X += shift;
					}
				}
			}
		}
	}
}
