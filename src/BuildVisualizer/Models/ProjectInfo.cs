using BuildVisualizer.ViewModels;
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
		private BuildStatus _status;

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

		public BuildStatus Status
		{
			get => _status;
			set
			{
				if (SetProperty(ref _status, value))
				{
					OnPropertyChanged(nameof(StatusColor));
					OnPropertyChanged(nameof(StatusHighlightColor));
					OnPropertyChanged(nameof(StatusTextColor));
				}
			}
		}

		public SolidColorBrush StatusColor
		{
			get
			{
				switch (Status)
				{
					case BuildStatus.NotBuilt:  return Resources.Colors.NotBuiltBrush;
					case BuildStatus.Building:  return Resources.Colors.BuildingBrush;
					case BuildStatus.Success:   return Resources.Colors.SuccessBrush;
					case BuildStatus.Failed:    return Resources.Colors.FailedBrush;
					case BuildStatus.Skipped:   return Resources.Colors.SkippedBrush;
					default:                    return Resources.Colors.NotBuiltBrush;
				}
			}
		}

		public SolidColorBrush StatusHighlightColor
		{
			get
			{
				switch (Status)
				{
					case BuildStatus.NotBuilt:  return Resources.Colors.NotBuiltHighlightBrush;
					case BuildStatus.Building:  return Resources.Colors.BuildingHighlightBrush;
					case BuildStatus.Success:   return Resources.Colors.SuccessHighlightBrush;
					case BuildStatus.Failed:    return Resources.Colors.FailedHighlightBrush;
					case BuildStatus.Skipped:   return Resources.Colors.SkippedHighlightBrush;
					default:                    return Resources.Colors.NotBuiltHighlightBrush;
				}
			}
		}

		public SolidColorBrush StatusTextColor
		{
			get
			{
				switch (Status)
				{
					case BuildStatus.NotBuilt:  return Resources.Colors.NotBuiltTextBrush;
					case BuildStatus.Building:  return Resources.Colors.BuildingTextBrush;
					case BuildStatus.Success:   return Resources.Colors.SuccessTextBrush;
					case BuildStatus.Failed:    return Resources.Colors.FailedTextBrush;
					case BuildStatus.Skipped:   return Resources.Colors.SkippedTextBrush;
					default:                    return Resources.Colors.NotBuiltTextBrush;
				}
			}
		}

		public ObservableCollection<ProjectInfo> Dependencies { get; set; }

		public ObservableCollection<ProjectInfo> Dependents { get; set; }

		public string DependenciesText
		{
			get
			{
				return Dependencies == null || Dependencies.Count == 0
					? "No dependencies" 
					: "→ " + string.Join(", ", Dependencies.Select(x => x.Name));
			}
		}

		public ProjectInfo(string name, string uniqueName, string projectPath)
		{
			_name = name;
			_uniqueName = uniqueName;
			_projectPath = projectPath;
			_projectDirectory = Path.GetDirectoryName(projectPath);
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
