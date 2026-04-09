using BuildVisualizer.Commands;
using BuildVisualizer.Layout;
using BuildVisualizer.Models;
using BuildVisualizer.Services;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
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
		private readonly DependencyGraphBuilder _graphBuilder;
		private readonly GraphLayoutEngine _layoutEngine;
		private double _canvasWidth;
		private double _canvasHeight;

		public ObservableCollection<ProjectInfo> Projects { get; set; }

		public ObservableCollection<ProjectNodeViewModel> ProjectTree { get; set; }

		public ObservableCollection<ProjectNodeViewModel> GraphNodes { get; set; }

		public ObservableCollection<DependencyLineViewModel> DependencyLines { get; set; }

		public double CanvasWidth
		{
			get => _canvasWidth;
			set => SetProperty(ref _canvasWidth, value);
		}

		public double CanvasHeight
		{
			get => _canvasHeight;
			set => SetProperty(ref _canvasHeight, value);
		}

		public ICommand RefreshCommand { get; }

		public BuildVisualizerViewModel(SolutionService solutionService, BuildEventService buildEventService)
		{
			_solutionService = solutionService;
			_buildEventService = buildEventService;
			_graphBuilder = new DependencyGraphBuilder();
			_layoutEngine = new GraphLayoutEngine();
			Projects = new ObservableCollection<ProjectInfo>();
			ProjectTree = new ObservableCollection<ProjectNodeViewModel>();
			GraphNodes = new ObservableCollection<ProjectNodeViewModel>();
			DependencyLines = new ObservableCollection<DependencyLineViewModel>();
			RefreshCommand = new RelayCommand(_ => ThreadHelper.JoinableTaskFactory.Run(LoadProjectsAsync));

			// Subscribe to build events
			_buildEventService.ProjectStatusChanged += OnProjectStatusChanged;
			_buildEventService.AllProjectsStatusReset += OnAllProjectsStatusReset;
			_buildEventService.ProjectStatusReset += OnProjectStatusReset;

			// Fire and forget - load projects asynchronously without blocking constructor
#pragma warning disable VSSDK007 // Avoid fire-and-forget in analyzers (intentional for async initialization)
			var loadTask = ThreadHelper.JoinableTaskFactory.RunAsync(LoadProjectsAsync);
#pragma warning restore VSSDK007
			loadTask.Task.FileAndForget("BuildVisualizer/LoadProjects");
		}

		private void OnAllProjectsStatusReset(object sender, System.EventArgs e)
		{
			// Reset all projects to NotBuilt status when solution build starts
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				foreach (var project in Projects)
				{
					project.Status = BuildStatus.NotBuilt;
				}
			});
		}

		private void OnProjectStatusReset(object sender, ProjectStatusChangedEventArgs e)
		{
			// Reset specific project status when individual project build starts
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				var project = Projects.FirstOrDefault(p => p.UniqueName == e.ProjectUniqueName);
				if (project != null)
				{
					project.Status = BuildStatus.NotBuilt;
				}
			});
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
			ProjectTree.Clear();

			var projects = _solutionService.GetProjects();

			// Parse dependencies
			_solutionService.ParseProjectDependencies(projects);

			foreach (var project in projects)
			{
				Projects.Add(project);
			}

			// Build the project hierarchy tree
			var treeNodes = _graphBuilder.BuildHierarchy(projects);
			foreach (var node in treeNodes)
			{
				ProjectTree.Add(node);
			}

			// Build graph layout
			BuildGraphLayout();
		}

		private void BuildGraphLayout()
		{
			GraphNodes.Clear();
			DependencyLines.Clear();

			if (ProjectTree.Count == 0)
			{
				CanvasWidth = 800;
				CanvasHeight = 200;
				return;
			}

			// Create a mapping from project name to node
			var nodeMap = new Dictionary<string, ProjectNodeViewModel>();

			// Flatten the tree into a list of all nodes
			var allNodes = new List<ProjectNodeViewModel>();
			FlattenTree(ProjectTree, allNodes);

			foreach (var node in allNodes)
			{
				nodeMap[node.Name] = node;
			}

			// Populate DependencyNodes for each node (resolve string names to node references)
			foreach (var node in allNodes)
			{
				node.DependencyNodes.Clear();
				foreach (var depName in node.ProjectData.Dependencies)
				{
					if (nodeMap.ContainsKey(depName))
					{
						node.DependencyNodes.Add(nodeMap[depName]);
					}
				}
			}

			// Add nodes to GraphNodes collection
			foreach (var node in allNodes)
			{
				GraphNodes.Add(node);
			}

			// Calculate layout
			var (width, height) = _layoutEngine.CalculateLayout(allNodes);
			CanvasWidth = width;
			CanvasHeight = height;

			// Build dependency lines
			foreach (var node in allNodes)
			{
				foreach (var depNode in node.DependencyNodes)
				{
					var line = new DependencyLineViewModel(node, depNode, allNodes);
					DependencyLines.Add(line);
				}
			}
		}

		private void FlattenTree(ObservableCollection<ProjectNodeViewModel> nodes, List<ProjectNodeViewModel> result)
		{
			foreach (var node in nodes)
			{
				result.Add(node);
				if (node.Children.Count > 0)
				{
					FlattenTree(node.Children, result);
				}
			}
		}
	}
}
