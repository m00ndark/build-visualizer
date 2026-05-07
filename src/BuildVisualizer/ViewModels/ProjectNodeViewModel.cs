using BuildVisualizer.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class ProjectNodeViewModel : ViewModelBase
	{
		private const double NodePadding = 24.0; // 8px text margin + 2px border, per side
		private const double NodeHeight = 28.0;
		private const double FontSize = 12.0;
		private static readonly Typeface NodeTypeface = new Typeface(
			new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

		private bool _isExpanded;
		private bool _isHighlighted;
		private double _x;
		private double _y;
		private double _width;
		private double _height = NodeHeight;

		public ProjectInfo ProjectInfo { get; }

		public ObservableCollection<ProjectNodeViewModel> Children { get; set; }

		public ObservableCollection<ProjectNodeViewModel> DependencyNodes { get; set; }

		public bool IsExpanded
		{
			get => _isExpanded;
			set => SetProperty(ref _isExpanded, value);
		}

		public bool IsHighlighted
		{
			get => _isHighlighted;
			set => SetProperty(ref _isHighlighted, value);
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

		public ProjectNodeViewModel(ProjectInfo projectInfo)
		{
			ProjectInfo = projectInfo;
			Children = new ObservableCollection<ProjectNodeViewModel>();
			DependencyNodes = new ObservableCollection<ProjectNodeViewModel>();
			_isExpanded = false;
			_width = MeasureTextWidth(projectInfo.Name) + NodePadding;

			// Subscribe to ProjectInfo property changes to relay them
			ProjectInfo.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(Models.ProjectInfo.Status))
				{
					OnPropertyChanged(nameof(Status));
					OnPropertyChanged(nameof(StatusColor));
				}
			};
		}

		private static double MeasureTextWidth(string text)
		{
			var formattedText = new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				NodeTypeface,
				FontSize,
				Brushes.Black,
				1.0);
			return formattedText.Width;
		}
	}
}
