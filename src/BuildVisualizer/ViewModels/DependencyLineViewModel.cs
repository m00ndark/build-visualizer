using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class DependencyLineViewModel : ViewModelBase
	{
		private Geometry _pathData;
		private bool _isHighlighted;

		private readonly ProjectNodeViewModel _source;
		private readonly ProjectNodeViewModel _target;
		private readonly List<ProjectNodeViewModel> _allNodes;

		public Geometry PathData
		{
			get => _pathData;
			set => SetProperty(ref _pathData, value);
		}

		public bool IsHighlighted
		{
			get => _isHighlighted;
			set
			{
				if (SetProperty(ref _isHighlighted, value))
				{
					// When this line is highlighted, also highlight the connected nodes
					if (_source != null)
						_source.IsHighlighted = value;
					if (_target != null)
						_target.IsHighlighted = value;
				}
			}
		}

		public ProjectNodeViewModel Source => _source;
		public ProjectNodeViewModel Target => _target;

		public DependencyLineViewModel(ProjectNodeViewModel source, ProjectNodeViewModel target, List<ProjectNodeViewModel> allNodes)
		{
			_source = source;
			_target = target;
			_allNodes = allNodes;

			// Subscribe to position changes
			_source.PropertyChanged += OnNodePositionChanged;
			_target.PropertyChanged += OnNodePositionChanged;

			// Initial calculation
			UpdatePath();
		}

		private void OnNodePositionChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ProjectNodeViewModel.X) ||
				e.PropertyName == nameof(ProjectNodeViewModel.Y) ||
				e.PropertyName == nameof(ProjectNodeViewModel.Width) ||
				e.PropertyName == nameof(ProjectNodeViewModel.Height))
			{
				UpdatePath();
			}
		}

		private void UpdatePath()
		{
			var geometry = CalculateRoutedPath();
			PathData = geometry;
		}

		private Geometry CalculateRoutedPath()
		{
			// Calculate connection points (bottom of source, top of target)
			double sourceX = _source.X + _source.Width / 2;
			double sourceY = _source.Y + _source.Height;
			double targetX = _target.X + _target.Width / 2;
			double targetY = _target.Y;

			// Simple orthogonal routing with smooth corners
			var points = new List<Point>();
			points.Add(new Point(sourceX, sourceY));

			// Check if we can draw a straight vertical line
			double verticalGap = targetY - sourceY;
			bool needsRouting = false;

			// Check if any nodes are in the path
			foreach (var node in _allNodes)
			{
				if (node == _source || node == _target)
					continue;

				// Check if this node intersects with our path
				if (IsNodeInPath(sourceX, sourceY, targetX, targetY, node))
				{
					needsRouting = true;
					break;
				}
			}

			if (!needsRouting && Math.Abs(sourceX - targetX) < 10)
			{
				// Straight line down
				points.Add(new Point(targetX, targetY));
			}
			else
			{
				// Orthogonal routing
				double midY = sourceY + verticalGap / 2;

				// Go down from source
				points.Add(new Point(sourceX, midY));
				// Go across to target X
				points.Add(new Point(targetX, midY));
				// Go to target
				points.Add(new Point(targetX, targetY));
			}

			// Convert to PathGeometry with smooth curves
			return CreateSmoothPathGeometry(points);
		}

		private bool IsNodeInPath(double x1, double y1, double x2, double y2, ProjectNodeViewModel node)
		{
			double minX = Math.Min(x1, x2) - 10;
			double maxX = Math.Max(x1, x2) + 10;
			double minY = Math.Min(y1, y2);
			double maxY = Math.Max(y1, y2);

			double nodeRight = node.X + node.Width;
			double nodeBottom = node.Y + node.Height;

			// Check if node overlaps with bounding box of line
			return !(node.X > maxX || nodeRight < minX || node.Y > maxY || nodeBottom < minY);
		}

		private Geometry CreateSmoothPathGeometry(List<Point> points)
		{
			if (points.Count < 2)
				return Geometry.Empty;

			var pathFigure = new PathFigure();
			pathFigure.StartPoint = points[0];

			if (points.Count == 2)
			{
				// Simple straight line
				pathFigure.Segments.Add(new LineSegment(points[1], true));
			}
			else
			{
				// Smooth corners with quadratic bezier curves
				double cornerRadius = 10;

				for (int i = 1; i < points.Count; i++)
				{
					var prev = points[i - 1];
					var curr = points[i];

					if (i == points.Count - 1)
					{
						// Last segment - just draw to the point
						pathFigure.Segments.Add(new LineSegment(curr, true));
					}
					else
					{
						var next = points[i + 1];

						// Calculate direction changes
						Vector v1 = curr - prev;
						Vector v2 = next - curr;

						// Calculate the distance to the corner
						double dist1 = v1.Length;
						double dist2 = v2.Length;

						if (dist1 < 0.001 || dist2 < 0.001)
						{
							// Degenerate case - just draw a line
							pathFigure.Segments.Add(new LineSegment(curr, true));
							continue;
						}

						// Limit corner radius to half the segment length
						double actualRadius = Math.Min(cornerRadius, Math.Min(dist1 / 2, dist2 / 2));

						// Calculate the point before the corner
						v1.Normalize();
						Point beforeCorner = curr - v1 * actualRadius;

						// Draw line to point before corner
						pathFigure.Segments.Add(new LineSegment(beforeCorner, true));

						// Calculate the point after the corner
						v2.Normalize();
						Point afterCorner = curr + v2 * actualRadius;

						// Draw quadratic curve around the corner
						pathFigure.Segments.Add(new QuadraticBezierSegment(curr, afterCorner, true));
					}
				}
			}

			var pathGeometry = new PathGeometry();
			pathGeometry.Figures.Add(pathFigure);
			pathGeometry.Freeze(); // Freeze for performance

			return pathGeometry;
		}
	}
}
