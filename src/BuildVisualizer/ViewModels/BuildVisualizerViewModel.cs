using BuildVisualizer.Commands;
using BuildVisualizer.Models;
using BuildVisualizer.Services;
using Microsoft.VisualStudio.Shell;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuildVisualizer.ViewModels
{
	public class BuildVisualizerViewModel : ViewModelBase
	{
		private readonly SolutionService _solutionService;

		public ObservableCollection<ProjectInfo> Projects { get; set; }

		public ICommand RefreshCommand { get; }

		public BuildVisualizerViewModel(SolutionService solutionService)
		{
			_solutionService = solutionService;
			Projects = new ObservableCollection<ProjectInfo>();
			RefreshCommand = new RelayCommand(_ => ThreadHelper.JoinableTaskFactory.Run(LoadProjectsAsync));

			// Fire and forget - load projects asynchronously without blocking constructor
#pragma warning disable VSSDK007 // Avoid fire-and-forget in analyzers (intentional for async initialization)
			var loadTask = ThreadHelper.JoinableTaskFactory.RunAsync(LoadProjectsAsync);
#pragma warning restore VSSDK007
			loadTask.Task.FileAndForget("BuildVisualizer/LoadProjects");
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
