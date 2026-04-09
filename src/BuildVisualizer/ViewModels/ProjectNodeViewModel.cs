using BuildVisualizer.Models;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class ProjectNodeViewModel : ViewModelBase
	{
		private bool _isExpanded;

		public ProjectInfo ProjectData { get; }

		public ObservableCollection<ProjectNodeViewModel> Children { get; set; }

		public bool IsExpanded
		{
			get => _isExpanded;
			set => SetProperty(ref _isExpanded, value);
		}

		// Delegated properties from ProjectInfo
		public string Name => ProjectData.Name;

		public BuildStatus Status => ProjectData.Status;

		public SolidColorBrush StatusColor => ProjectData.StatusColor;

		public ProjectNodeViewModel(ProjectInfo projectData)
		{
			ProjectData = projectData;
			Children = new ObservableCollection<ProjectNodeViewModel>();
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
