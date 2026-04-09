using BuildVisualizer.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;

namespace BuildVisualizer.Models
{
	public class ProjectInfo : ViewModelBase
	{
		private string _name;
		private string _uniqueName;
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

		public BuildStatus Status
		{
			get => _status;
			set
			{
				if (SetProperty(ref _status, value))
				{
					OnPropertyChanged(nameof(StatusColor));
				}
			}
		}

		public SolidColorBrush StatusColor
		{
			get
			{
				switch (Status)
				{
					case BuildStatus.NotBuilt:
						return Resources.Colors.NotBuiltBrush;
					case BuildStatus.Building:
						return Resources.Colors.BuildingBrush;
					case BuildStatus.Success:
						return Resources.Colors.SuccessBrush;
					case BuildStatus.Failed:
						return Resources.Colors.FailedBrush;
					case BuildStatus.Skipped:
						return Resources.Colors.SkippedBrush;
					default:
						return Resources.Colors.NotBuiltBrush;
				}
			}
		}

		public ObservableCollection<string> Dependencies { get; set; }

		public ObservableCollection<string> Dependents { get; set; }

		public string DependenciesText
		{
			get
			{
				if (Dependencies == null || Dependencies.Count == 0)
				{
					return "No dependencies";
				}
				return "→ " + string.Join(", ", Dependencies);
			}
		}

		public ProjectInfo(string name, string uniqueName)
		{
			_name = name;
			_uniqueName = uniqueName;
			_status = BuildStatus.NotBuilt;

			Dependencies = new ObservableCollection<string>();
			Dependents = new ObservableCollection<string>();

			// Subscribe to collection changes to update DependenciesText
			Dependencies.CollectionChanged += OnDependenciesChanged;
		}

		private void OnDependenciesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(DependenciesText));
		}
	}
}
