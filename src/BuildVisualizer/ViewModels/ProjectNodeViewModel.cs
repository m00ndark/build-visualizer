using BuildVisualizer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class ProjectNodeViewModel : ViewModelBase
	{
		private const double NormalBorderThicknessValue = 1.0;
		private const double HighlightedBorderThicknessValue = 2.0;
		private const double NodePadding = (8.0 + HighlightedBorderThicknessValue) * 2; // 8px text margin + border thickness, per side
		private const double NodeHeight = 28.0;
		private const double FontSize = 12.0;
		private static readonly Typeface NodeTypeface = new Typeface(
			new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);

		private double _x;
		private double _y;
		private double _width;
		private double _height = NodeHeight;
		private Thickness _normalBorderThickness = new Thickness(NormalBorderThicknessValue);
		private Thickness _highlightedBorderThickness = new Thickness(HighlightedBorderThicknessValue);
		private bool _isHighlighted;
		private bool _isDependencyHighlighted;
		private bool _isTransitiveDependencyHighlighted;
		private bool _isDimmed;
		private bool _isHovered;
		private bool _isSelected;
		private bool _showTransitiveDependencies;

		public ProjectInfo ProjectInfo { get; }

		public ObservableCollection<ProjectNodeViewModel> DependencyNodes { get; set; }

		public Thickness NormalBorderThickness
		{
			get => _normalBorderThickness;
			set => SetProperty(ref _normalBorderThickness, value);
		}

		public Thickness HighlightedBorderThickness
		{
			get => _highlightedBorderThickness;
			set => SetProperty(ref _highlightedBorderThickness, value);
		}

		public bool IsHighlighted
		{
			get => _isHighlighted;
			set => SetProperty(ref _isHighlighted, value);
		}

		public bool IsDependencyHighlighted
		{
			get => _isDependencyHighlighted;
			set => SetProperty(ref _isDependencyHighlighted, value);
		}

		public bool IsTransitiveDependencyHighlighted
		{
			get => _isTransitiveDependencyHighlighted;
			set => SetProperty(ref _isTransitiveDependencyHighlighted, value);
		}

		public bool IsSelected
		{
			get => _isSelected;
			set => SetProperty(ref _isSelected, value);
		}

		public bool IsDimmed
		{
			get => _isDimmed;
			set => SetProperty(ref _isDimmed, value);
		}

		/// <summary>
		/// Fired by ApplyHighlight so the owning ViewModel can dim/undim unrelated nodes.
		/// Parameter is the set of nodes involved in the current highlight (null when unhovered).
		/// </summary>
		public event Action<HashSet<ProjectNodeViewModel>> HighlightChanged;

		// Layout properties
		public double X
		{
			get => _x;
			set => SetProperty(ref _x, value);
		}

		public double Y
		{
			get => _y;
			set => SetProperty(ref _y, value);
		}

		public double Width
		{
			get => _width;
			set => SetProperty(ref _width, value);
		}

		public double Height
		{
			get => _height;
			set => SetProperty(ref _height, value);
		}

		// Delegated properties from ProjectInfo
		public string Name => ProjectInfo.Name;

		public string ProjectPath => ProjectInfo.ProjectPath;

		public BuildStatus Status => ProjectInfo.Status;

		public SolidColorBrush StatusColor => ProjectInfo.StatusColor;

		public SolidColorBrush HighlightBorderColor => ProjectInfo.StatusHighlightColor;

		public SolidColorBrush DependencyBorderColor => ProjectInfo.StatusDependencyBorderColor;

		public SolidColorBrush DependencyBackgroundColor => ProjectInfo.StatusDependencyBackgroundColor;

		public SolidColorBrush TextColor => ProjectInfo.StatusTextColor;

		public ProjectNodeViewModel(ProjectInfo projectInfo)
		{
			ProjectInfo = projectInfo;
			DependencyNodes = new ObservableCollection<ProjectNodeViewModel>();
			_width = MeasureTextWidth(projectInfo.Name) + NodePadding;

			// Subscribe to ProjectInfo property changes to relay them
			ProjectInfo.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(Models.ProjectInfo.Status))
				{
					OnPropertyChanged(nameof(Status));
					OnPropertyChanged(nameof(StatusColor));
					OnPropertyChanged(nameof(HighlightBorderColor));
					OnPropertyChanged(nameof(DependencyBorderColor));
					OnPropertyChanged(nameof(DependencyBackgroundColor));
					OnPropertyChanged(nameof(TextColor));
				}
			};
		}

		public void SetHovered(bool isHovered)
		{
			_isHovered = isHovered;
			ApplyHighlight();
		}

		public void SetShowTransitiveDependencies(bool show)
		{
			_showTransitiveDependencies = show;
			if (_isHovered)
				ApplyHighlight();
		}

		private void ApplyHighlight()
		{
			IsHighlighted = _isHovered;

			// Clear all dependency states first
			foreach (ProjectNodeViewModel dep in DependencyNodes)
				dep.IsDependencyHighlighted = false;

			foreach (ProjectNodeViewModel dep in GetAllTransitiveDependencies())
				dep.IsTransitiveDependencyHighlighted = false;

			if (!_isHovered)
			{
				HighlightChanged?.Invoke(null);
				return;
			}

			HashSet<ProjectNodeViewModel> involved = new HashSet<ProjectNodeViewModel> { this };

			if (_showTransitiveDependencies)
			{
				HashSet<ProjectNodeViewModel> direct = new HashSet<ProjectNodeViewModel>(DependencyNodes);
				foreach (ProjectNodeViewModel dep in direct)
				{
					dep.IsDependencyHighlighted = true;
					involved.Add(dep);
				}

				foreach (ProjectNodeViewModel dep in GetAllTransitiveDependencies())
				{
					if (!direct.Contains(dep))
						dep.IsTransitiveDependencyHighlighted = true;
					involved.Add(dep);
				}
			}
			else
			{
				foreach (ProjectNodeViewModel dep in DependencyNodes)
				{
					dep.IsDependencyHighlighted = true;
					involved.Add(dep);
				}
			}

			HighlightChanged?.Invoke(involved);
		}

		private HashSet<ProjectNodeViewModel> GetAllTransitiveDependencies()
		{
			HashSet<ProjectNodeViewModel> visited = new HashSet<ProjectNodeViewModel>();
			Queue<ProjectNodeViewModel> queue = new Queue<ProjectNodeViewModel>();

			foreach (ProjectNodeViewModel dep in DependencyNodes)
				queue.Enqueue(dep);

			while (queue.Count > 0)
			{
				ProjectNodeViewModel current = queue.Dequeue();
				if (!visited.Add(current))
					continue;

				foreach (ProjectNodeViewModel dep in current.DependencyNodes)
					queue.Enqueue(dep);
			}

			return visited;
		}

		private static double MeasureTextWidth(string text)
		{
			return new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				NodeTypeface,
				FontSize,
				Brushes.Black,
				1.0).Width;
		}
	}
}
