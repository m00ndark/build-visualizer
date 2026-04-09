using BuildVisualizer.ViewModels;

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
			set => SetProperty(ref _status, value);
		}

		public ProjectInfo(string name, string uniqueName)
		{
			_name = name;
			_uniqueName = uniqueName;
			_status = BuildStatus.NotBuilt;
		}
	}
}
