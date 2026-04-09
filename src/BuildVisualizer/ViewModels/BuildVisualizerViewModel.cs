using BuildVisualizer.Commands;
using BuildVisualizer.Models;
using BuildVisualizer.Services;
using Microsoft.VisualStudio.Shell;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuildVisualizer.ViewModels
{
	public class BuildVisualizerViewModel : ViewModelBase
	{
		private readonly SolutionService _solutionService;
		private readonly BuildEventService _buildEventService;

		public ObservableCollection<ProjectInfo> Projects { get; set; }

		public ICommand RefreshCommand { get; }

		public BuildVisualizerViewModel(SolutionService solutionService, BuildEventService buildEventService)
		{
			_solutionService = solutionService;
			_buildEventService = buildEventService;
			Projects = new ObservableCollection<ProjectInfo>();
			RefreshCommand = new RelayCommand(_ => ThreadHelper.JoinableTaskFactory.Run(LoadProjectsAsync));

			// Subscribe to build events
			_buildEventService.ProjectStatusChanged += OnProjectStatusChanged;

			// Fire and forget - load projects asynchronously without blocking constructor
#pragma warning disable VSSDK007 // Avoid fire-and-forget in analyzers (intentional for async initialization)
			var loadTask = ThreadHelper.JoinableTaskFactory.RunAsync(LoadProjectsAsync);
#pragma warning restore VSSDK007
			loadTask.Task.FileAndForget("BuildVisualizer/LoadProjects");
		}

		private void OnProjectStatusChanged(object sender, ProjectStatusChangedEventArgs e)
		{
			// This event might come from a background thread, so marshal to UI thread
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				// Find the project by UniqueName and update its status
				var project = Projects.FirstOrDefault(p => p.UniqueName == e.ProjectUniqueName);
				if (project != null)
				{
					project.Status = e.NewStatus;
				}
			});
		}

		private async Task LoadProjectsAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			Projects.Clear();

			var projects = _solutionService.GetProjects();

			foreach (var project in projects)
			{
				Projects.Add(project);
			}
		}
	}
}
