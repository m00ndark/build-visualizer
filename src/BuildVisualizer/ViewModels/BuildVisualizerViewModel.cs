using BuildVisualizer.Commands;
using BuildVisualizer.Layout;
using BuildVisualizer.Models;
using BuildVisualizer.Services;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace BuildVisualizer.ViewModels
{
	public class BuildVisualizerViewModel : ViewModelBase
	{
		private readonly SolutionService _solutionService;
		private readonly BuildEventService _buildEventService;
		private readonly SolutionEventsService _solutionEventsService;
		private readonly ThemeService _themeService;
		private readonly DTE2 _dte;
		private readonly IVsSolution _solution;
		private readonly IVsSolutionBuildManager2 _buildManager;
		private readonly GraphLayoutEngine _layoutEngine;
		private bool _isGraphView;
		private ProjectInfo _selectedProject;
		private string _sortProperty;
		private ListSortDirection _sortDirection = ListSortDirection.Ascending;

		public ObservableCollection<ProjectInfo> Projects { get; set; }

		public ICollectionView SortedProjects { get; private set; }

		public ObservableCollection<ProjectNodeViewModel> GraphNodes { get; set; }

		public ObservableCollection<GraphRowGroupViewModel> GraphRowGroups { get; set; }

		public ICommand RefreshCommand { get; }

		public ICommand ToggleViewCommand { get; }

		public ICommand SortCommand { get; }

		public ICommand CleanProjectCommand { get; }

		public ICommand BuildProjectCommand { get; }

		public ICommand RebuildProjectCommand { get; }

		public ICommand CleanSolutionCommand { get; }

		public ICommand BuildSolutionCommand { get; }

		public ICommand RebuildSolutionCommand { get; }

		public string SortProperty
		{
			get => _sortProperty;
			private set => SetProperty(ref _sortProperty, value);
		}

		public ListSortDirection SortDirection
		{
			get => _sortDirection;
			private set => SetProperty(ref _sortDirection, value);
		}

		public ProjectInfo SelectedProject
		{
			get => _selectedProject;
			set
			{
				if (SetProperty(ref _selectedProject, value))
					CommandManager.InvalidateRequerySuggested();
			}
		}

		public bool IsGraphView
		{
			get => _isGraphView;
			set
			{
				if (SetProperty(ref _isGraphView, value))
					CommandManager.InvalidateRequerySuggested();
			}
		}

		public BuildVisualizerViewModel(
			SolutionService solutionService,
			BuildEventService buildEventService,
			SolutionEventsService solutionEventsService,
			ThemeService themeService,
			DTE2 dte,
			IVsSolution solution,
			IVsSolutionBuildManager2 buildManager)
		{
			_solutionService = solutionService;
			_buildEventService = buildEventService;
			_solutionEventsService = solutionEventsService;
			_themeService = themeService;
			_dte = dte;
			_solution = solution;
			_buildManager = buildManager;
			Resources.Colors.IsDarkTheme = themeService.IsDarkTheme;
			_layoutEngine = new GraphLayoutEngine();
			Projects = new ObservableCollection<ProjectInfo>();
			SortedProjects = CollectionViewSource.GetDefaultView(Projects);
			if (SortedProjects is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveSorting)
				liveShaping.IsLiveSorting = true;
			GraphNodes = new ObservableCollection<ProjectNodeViewModel>();
			GraphRowGroups = new ObservableCollection<GraphRowGroupViewModel>();
			RefreshCommand = new RelayCommand(_ => ThreadHelper.JoinableTaskFactory.Run(LoadProjectsAsync));
			ToggleViewCommand = new RelayCommand(_ => IsGraphView = !IsGraphView);
			SortCommand = new RelayCommand(param =>
			{
				if (!(param is string property) || string.IsNullOrEmpty(property))
					return;

				ListSortDirection direction = (property == SortProperty && SortDirection == ListSortDirection.Ascending)
					? ListSortDirection.Descending
					: ListSortDirection.Ascending;

				SortedProjects.SortDescriptions.Clear();
				SortedProjects.SortDescriptions.Add(new SortDescription(property, direction));

				if (SortedProjects is ICollectionViewLiveShaping liveSorting)
				{
					liveSorting.LiveSortingProperties.Clear();
					liveSorting.LiveSortingProperties.Add(property);
				}

				SortProperty = property;
				SortDirection = direction;
			});

#pragma warning disable VSTHRD010 // RunOnMainThread ensures UI thread
			Func<object, bool> canBuildProject = _ => !IsGraphView && SelectedProject != null;
			CleanProjectCommand = new RelayCommand(_ => RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN)), canBuildProject);
			BuildProjectCommand = new RelayCommand(_ => RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD)), canBuildProject);
			RebuildProjectCommand = new RelayCommand(_ => RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE)), canBuildProject);

			CleanSolutionCommand = new RelayCommand(_ => RunOnMainThread(() => ExecuteCommand("Build.CleanSolution")));
			BuildSolutionCommand = new RelayCommand(_ => RunOnMainThread(() => ExecuteCommand("Build.BuildSolution")));
			RebuildSolutionCommand = new RelayCommand(_ => RunOnMainThread(() => ExecuteCommand("Build.RebuildSolution")));
#pragma warning restore VSTHRD010

			// Subscribe to build events
			_buildEventService.ProjectStatusChanged += OnProjectStatusChanged;
			_buildEventService.AllProjectsStatusReset += OnAllProjectsStatusReset;
			_buildEventService.ProjectStatusReset += OnProjectStatusReset;

			// Subscribe to solution events
			_solutionEventsService.ProjectsChanged += OnProjectsChanged;
			_solutionEventsService.SolutionClosed += OnSolutionClosed;

			// Subscribe to theme change events
			_themeService.ThemeChanged += OnThemeChanged;

			// Catch up if the solution was already loaded before the tool window opened
#pragma warning disable VSTHRD110, VSSDK007 // Intentional fire-and-forget for async initialization in constructor
			if (_solutionEventsService.LastProjectReferences != null)
			{
				// Solution fully loaded with dependencies resolved — use the cached result
				_solutionService.UpdateProjectReferences(_solutionEventsService.LastProjectReferences);
				ThreadHelper.JoinableTaskFactory.RunAsync(UpdateProjectsAsync);
			}
			else if (_solutionEventsService.IsSolutionOpen)
			{
				// Solution is open but dependencies are still loading — show projects without dependency info;
				// the ProjectsChanged event will refresh with full dependency data when ready
				ThreadHelper.JoinableTaskFactory.RunAsync(LoadProjectsAsync);
			}
#pragma warning restore VSTHRD110, VSSDK007
		}

		private void OnAllProjectsStatusReset(object sender, System.EventArgs e)
		{
			// Reset all projects to NotBuilt status when solution build starts
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				foreach (ProjectInfo project in Projects)
				{
					project.Status = BuildStatus.NotBuilt;
					project.BuildStart = null;
					project.BuildFinish = null;
				}
			});
		}

		private void OnProjectStatusReset(object sender, ProjectStatusChangedEventArgs e)
		{
			// Reset specific project status when individual project build starts
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				ProjectInfo project = Projects.FirstOrDefault(p => p.ProjectPath == e.ProjectUniqueName);
				if (project != null)
				{
					project.Status = BuildStatus.NotBuilt;
					project.BuildStart = null;
					project.BuildFinish = null;
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
				ProjectInfo project = Projects.FirstOrDefault(p => string.Equals(p.UniqueName, e.ProjectUniqueName, StringComparison.OrdinalIgnoreCase));
				if (project != null)
				{
					project.Status = e.NewStatus;

					if (e.NewStatus == BuildStatus.Building)
						project.BuildStart = e.Timestamp;
					else if (e.NewStatus == BuildStatus.Success || e.NewStatus == BuildStatus.Failed || e.NewStatus == BuildStatus.Skipped)
						project.BuildFinish = e.Timestamp;
				}
			});
		}

		private async Task LoadProjectsAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			await _solutionService.LoadProjectReferencesAsync();
			await UpdateProjectsAsync();
		}

		private async Task UpdateProjectsAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			Projects.Clear();

			// Get projects (which will now use the cached references)
			List<ProjectInfo> projects = await _solutionService.GetProjectsAsync();

			foreach (ProjectInfo project in projects)
			{
				Projects.Add(project);
			}

			// Build graph layout
			BuildGraphLayout();
		}

		private void BuildGraphLayout()
		{
			GraphNodes.Clear();
			GraphRowGroups.Clear();

			if (Projects.Count == 0)
				return;

			// Create a mapping from project path to node
			Dictionary<string, ProjectNodeViewModel> nodeMap = new Dictionary<string, ProjectNodeViewModel>();

			// Create nodes directly from Projects
			List<ProjectNodeViewModel> allNodes = new List<ProjectNodeViewModel>();
			foreach (ProjectInfo project in Projects)
			{
				ProjectNodeViewModel node = new ProjectNodeViewModel(project);
				allNodes.Add(node);
				nodeMap[node.ProjectPath] = node;
			}

			// Populate DependencyNodes for each node (resolve string names to node references)
			foreach (ProjectNodeViewModel node in allNodes)
			{
				node.DependencyNodes.Clear();
				foreach (ProjectInfo dependencyProject in node.ProjectInfo.Dependencies)
				{
					if (nodeMap.TryGetValue(dependencyProject.ProjectPath, out ProjectNodeViewModel projectNode))
					{
						node.DependencyNodes.Add(projectNode);
					}
				}
			}

			// Add nodes to GraphNodes collection
			foreach (ProjectNodeViewModel node in allNodes)
			{
				GraphNodes.Add(node);
			}

			// Build ordered layer groups for the responsive row layout
			Dictionary<int, List<ProjectNodeViewModel>> layers = _layoutEngine.GetOrderedLayers(allNodes);

			int maxLayer = layers.Count > 0 ? layers.Keys.Max() : 0;
			for (int layer = 0; layer <= maxLayer; layer++)
			{
				if (!layers.ContainsKey(layer))
					continue;

				GraphRowGroupViewModel group = new GraphRowGroupViewModel(layer, maxLayer + 1, _themeService.IsDarkTheme);
				foreach (ProjectNodeViewModel node in layers[layer])
				{
					group.Nodes.Add(node);
				}
				GraphRowGroups.Add(group);
			}
		}

		private static void RunOnMainThread(Action action)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				action();
			});
		}

		private void BuildProject(VSSOLNBUILDUPDATEFLAGS flags)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectInfo project = SelectedProject;
			if (project == null)
				return;

			int hr = _solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy hierarchy);
			if (hr != VSConstants.S_OK || hierarchy == null)
			{
				Debug.WriteLine($"[Build] Could not resolve hierarchy for '{project.UniqueName}'");
				return;
			}

			_buildManager.StartSimpleUpdateProjectConfiguration(
				hierarchy, null, null, (uint)flags, 0, 0);
		}

		private void ExecuteCommand(string commandName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_dte.ExecuteCommand(commandName);
		}

		private void OnProjectsChanged(IReadOnlyList<ProjectReferences> projectReferences)
		{
			_solutionService.UpdateProjectReferences(projectReferences);
			ThreadHelper.JoinableTaskFactory.Run(UpdateProjectsAsync);
		}

		private void OnSolutionClosed(object sender, EventArgs e)
		{
			// Clear all visualizations when solution closes
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				Projects.Clear();
				GraphNodes.Clear();
				GraphRowGroups.Clear();
			});
		}

		private void OnThemeChanged(object sender, EventArgs e)
		{
			bool isDarkTheme = _themeService.IsDarkTheme;
			Resources.Colors.IsDarkTheme = isDarkTheme;
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				foreach (GraphRowGroupViewModel group in GraphRowGroups)
					group.UpdateTheme(isDarkTheme);
				foreach (ProjectInfo project in Projects)
				{
					project.NotifyColorPropertiesChanged();
				}
			});
		}
	}
}
