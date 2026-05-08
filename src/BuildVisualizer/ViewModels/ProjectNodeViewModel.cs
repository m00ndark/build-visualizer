using BuildVisualizer.Models;
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
			new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

		private double _x;
		private double _y;
		private double _width;
		private double _height = NodeHeight;
		private Thickness _normalBorderThickness = new Thickness(NormalBorderThicknessValue);
		private Thickness _highlightedBorderThickness = new Thickness(HighlightedBorderThicknessValue);
		private bool _isDependencyHighlighted;

		public ProjectInfo ProjectInfo { get; }

		public ObservableCollection<ProjectNodeViewModel> Children { get; set; }

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

		public bool IsDependencyHighlighted
		{
			get => _isDependencyHighlighted;
			set => SetProperty(ref _isDependencyHighlighted, value);
		}

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
			Children = new ObservableCollection<ProjectNodeViewModel>();
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
			foreach (ProjectNodeViewModel dependency in DependencyNodes)
				dependency.IsDependencyHighlighted = isHovered;
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
