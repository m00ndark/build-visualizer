using BuildVisualizer.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace BuildVisualizer.Models
{
	public class ProjectInfo : ViewModelBase
	{
		private string _name;
		private string _uniqueName;
		private string _projectPath;
		private string _projectDirectory;
		private string _projectType;
		private BuildStatus _status;
		private DateTime? _buildStart;
		private DateTime? _buildStop;

		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		public string UniqueName
		{
			get => _uniqueName;
			set => SetProperty(ref _uniqueName, value);
		}

		public string ProjectPath
		{
			get => _projectPath;
			set => SetProperty(ref _projectPath, value);
		}

		public string ProjectDirectory
		{
			get => _projectDirectory;
			set => SetProperty(ref _projectDirectory, value);
		}

		public string ProjectType
		{
			get => _projectType;
			set => SetProperty(ref _projectType, value);
		}

		public BuildStatus Status
		{
			get => _status;
			set
			{
				if (SetProperty(ref _status, value))
				{
					OnPropertyChanged(nameof(StatusColor));
					OnPropertyChanged(nameof(StatusHighlightColor));
					OnPropertyChanged(nameof(StatusDependencyBorderColor));
					OnPropertyChanged(nameof(StatusDependencyBackgroundColor));
					OnPropertyChanged(nameof(StatusTextColor));
				}
			}
		}

		public SolidColorBrush StatusColor                     => Resources.Colors.GetStatusBrush(Status);
		public SolidColorBrush StatusHighlightColor            => Resources.Colors.GetStatusHighlightBrush(Status);
		public SolidColorBrush StatusDependencyBorderColor     => Resources.Colors.GetStatusDependencyBorderBrush(Status);
		public SolidColorBrush StatusDependencyBackgroundColor => Resources.Colors.GetStatusDependencyBackgroundBrush(Status);
		public SolidColorBrush StatusTextColor                 => Resources.Colors.GetStatusTextBrush(Status);

		public void NotifyColorPropertiesChanged()
		{
			OnPropertyChanged(nameof(StatusColor));
			OnPropertyChanged(nameof(StatusHighlightColor));
			OnPropertyChanged(nameof(StatusDependencyBorderColor));
			OnPropertyChanged(nameof(StatusDependencyBackgroundColor));
			OnPropertyChanged(nameof(StatusTextColor));
		}

		public DateTime? BuildStart
		{
			get => _buildStart;
			set
			{
				if (SetProperty(ref _buildStart, value))
					OnPropertyChanged(nameof(BuildDuration));
			}
		}

		public DateTime? BuildStop
		{
			get => _buildStop;
			set
			{
				if (SetProperty(ref _buildStop, value))
					OnPropertyChanged(nameof(BuildDuration));
			}
		}

		public TimeSpan? BuildDuration =>
			_buildStart.HasValue && _buildStop.HasValue
				? _buildStop.Value - _buildStart.Value
				: (TimeSpan?)null;

		public ObservableCollection<ProjectInfo> Dependencies { get; set; }

		public ObservableCollection<ProjectInfo> Dependents { get; set; }

		public string DependenciesText
		{
			get
			{
				return Dependencies == null || Dependencies.Count == 0
					? string.Empty
					: string.Join(", ", Dependencies.Select(x => x.Name).OrderBy(n => n));
			}
		}

		public ProjectInfo(string name, string uniqueName, string projectPath, string projectType = null)
		{
			_name = name;
			_uniqueName = uniqueName;
			_projectPath = projectPath;
			_projectDirectory = Path.GetDirectoryName(projectPath);
			_projectType = projectType;
			_status = BuildStatus.NotBuilt;

			Dependencies = new ObservableCollection<ProjectInfo>();
			Dependents = new ObservableCollection<ProjectInfo>();

			// Subscribe to collection changes to update DependenciesText
			Dependencies.CollectionChanged += OnDependenciesChanged;
		}

		private void OnDependenciesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(DependenciesText));
		}
	}
}
