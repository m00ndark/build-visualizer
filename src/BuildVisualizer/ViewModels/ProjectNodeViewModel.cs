using BuildVisualizer.Models;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class ProjectNodeViewModel : ViewModelBase
	{
		private bool _isExpanded;
		private bool _isHighlighted;
		private double _x;
		private double _y;
		private double _width = 120;
		private double _height = 60;

		public ProjectInfo ProjectData { get; }

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
		public string Name => ProjectData.Name;

		public BuildStatus Status => ProjectData.Status;

		public SolidColorBrush StatusColor => ProjectData.StatusColor;

		public ProjectNodeViewModel(ProjectInfo projectData)
		{
			ProjectData = projectData;
			Children = new ObservableCollection<ProjectNodeViewModel>();
			DependencyNodes = new ObservableCollection<ProjectNodeViewModel>();
			_isExpanded = false;

			// Subscribe to ProjectData property changes to relay them
			ProjectData.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == nameof(ProjectInfo.Status))
				{
					OnPropertyChanged(nameof(Status));
					OnPropertyChanged(nameof(StatusColor));
				}
			};
		}
	}
}
