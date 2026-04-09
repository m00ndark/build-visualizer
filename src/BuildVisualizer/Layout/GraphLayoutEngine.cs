using BuildVisualizer.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace BuildVisualizer.Layout
{
	public class GraphLayoutEngine
	{
		private const double NodeHorizontalSpacing = 150;
		private const double NodeVerticalSpacing = 150;
		private const double NodeWidth = 120;
		private const double NodeHeight = 60;

		public (double Width, double Height) CalculateLayout(List<ProjectNodeViewModel> nodes, double canvasWidth = 800)
		{
			if (nodes == null || nodes.Count == 0)
			{
				return (canvasWidth, 200);
			}

			// Assign layers using topological sort
			var layers = AssignLayers(nodes);

			// Calculate positions
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

		private void AssignCoordinates(Dictionary<int, List<ProjectNodeViewModel>> layers)
		{
			int maxLayer = layers.Keys.Max();

			for (int layer = 0; layer <= maxLayer; layer++)
			{
				if (!layers.ContainsKey(layer))
					continue;

				var nodesInLayer = layers[layer];
				double yPosition = layer * NodeVerticalSpacing;

				// Center nodes horizontally within the layer
				for (int i = 0; i < nodesInLayer.Count; i++)
				{
					double xPosition = i * NodeHorizontalSpacing;
					nodesInLayer[i].X = xPosition;
					nodesInLayer[i].Y = yPosition;
				}
			}
		}
	}
}
