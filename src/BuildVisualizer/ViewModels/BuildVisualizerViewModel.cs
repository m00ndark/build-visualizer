using BuildVisualizer.Commands;
using BuildVisualizer.Layout;
using BuildVisualizer.Models;
using BuildVisualizer.Services;
using EnvDTE;
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
using System.Windows.Threading;

namespace BuildVisualizer.ViewModels
{
#pragma warning disable VSTHRD010 // All COM access is marshaled to the UI thread via ThreadingHelper or DispatcherTimer
	public class BuildVisualizerViewModel : ViewModelBase
	{
		private const string NoBuildInformationAvailableStatusText = "No build information available.";

		private readonly SolutionService _solutionService;
		private readonly BuildEventService _buildEventService;
		private readonly SolutionEventsService _solutionEventsService;
		private readonly ThemeService _themeService;
		private readonly DTE2 _dte;
		private readonly IVsSolution _solution;
		private readonly IVsSolutionBuildManager2 _buildManager;
		private readonly IVsUIShell _uiShell;
		private readonly BuildDiagnosticsService _diagnosticsService;
		private readonly ProjectConfigurationService _projectConfigurationService;
		private readonly UserSettingsService _userSettingsService;
		private readonly GraphLayoutEngine _layoutEngine;
		private readonly DispatcherTimer _buildTimer;
		private bool _isGraphView;
		private ProjectInfo _selectedProject;
		private string _sortProperty;
		private ListSortDirection _sortDirection = ListSortDirection.Ascending;
		private string _buildStatusText;
		private vsBuildScope _buildScope;
		private vsBuildAction _buildAction;
		private DateTime _buildStartTime;
		private int _errorCount;
		private int _warningCount;
		private int _messageCount;
		private bool _focusOnBuildStart;
		private bool _showTransitiveDependencies;
		private bool _isBuilding;

		public ObservableCollection<ProjectInfo> Projects { get; set; }

		public ICollectionView SortedProjects { get; }

		public ObservableCollection<ProjectNodeViewModel> GraphNodes { get; set; }

		public ObservableCollection<GraphRowGroupViewModel> GraphRowGroups { get; set; }

		public ICommand RefreshCommand { get; }

		public ICommand ToggleViewCommand { get; }

		public ICommand SortCommand { get; }

		public ICommand CleanProjectCommand { get; }

		public ICommand BuildProjectCommand { get; }

		public ICommand RebuildProjectCommand { get; }

		public ICommand CancelBuildCommand { get; }

		public ICommand CleanSolutionCommand { get; }

		public ICommand BuildSolutionCommand { get; }

		public ICommand RebuildSolutionCommand { get; }

		public ICommand ContextBuildCommand { get; }

		public ICommand ContextRebuildCommand { get; }

		public ICommand ContextCleanCommand { get; }

		public ICommand RevealInSolutionExplorerCommand { get; }

		public string BuildStatusText
		{
			get => _buildStatusText;
			private set => SetProperty(ref _buildStatusText, value);
		}

		public int ErrorCount
		{
			get => _errorCount;
			private set => SetProperty(ref _errorCount, value);
		}

		public int WarningCount
		{
			get => _warningCount;
			private set => SetProperty(ref _warningCount, value);
		}

		public int MessageCount
		{
			get => _messageCount;
			private set => SetProperty(ref _messageCount, value);
		}

		public bool FocusOnBuildStart
		{
			get => _focusOnBuildStart;
			set
			{
				if (SetProperty(ref _focusOnBuildStart, value))
					_userSettingsService?.SetString(UserSettings.Collections.Settings, UserSettings.Keys.FocusOnBuildStart, value ? "1" : "0");
			}
		}

		public bool ShowTransitiveDependencies
		{
			get => _showTransitiveDependencies;
			set
			{
				if (SetProperty(ref _showTransitiveDependencies, value))
				{
					_userSettingsService?.SetString(UserSettings.Collections.Settings, UserSettings.Keys.ShowTransitiveDependencies, value ? "1" : "0");
					foreach (ProjectNodeViewModel node in GraphNodes)
						node.SetShowTransitiveDependencies(value);
				}
			}
		}

		public bool IsBuilding
		{
			get => _isBuilding;
			private set => SetProperty(ref _isBuilding, value);
		}

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
			IVsSolutionBuildManager2 buildManager,
			IVsUIShell uiShell,
			BuildDiagnosticsService diagnosticsService,
			ProjectConfigurationService projectConfigurationService,
			UserSettingsService userSettingsService)
		{
			_solutionService = solutionService;
			_buildEventService = buildEventService;
			_solutionEventsService = solutionEventsService;
			_themeService = themeService;
			_dte = dte;
			_solution = solution;
			_buildManager = buildManager;
			_uiShell = uiShell;
			_diagnosticsService = diagnosticsService;
			_projectConfigurationService = projectConfigurationService;
			_userSettingsService = userSettingsService;
			_focusOnBuildStart = userSettingsService?.GetString(UserSettings.Collections.Settings, UserSettings.Keys.FocusOnBuildStart) != "0";
			_showTransitiveDependencies = userSettingsService?.GetString(UserSettings.Collections.Settings, UserSettings.Keys.ShowTransitiveDependencies) != "0";
			Resources.Colors.IsDarkTheme = themeService.IsDarkTheme;
			_layoutEngine = new GraphLayoutEngine();
			_buildTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			_buildTimer.Tick += (s, _) => OnBuildTimerTick();
			Projects = new ObservableCollection<ProjectInfo>();
			SortedProjects = CollectionViewSource.GetDefaultView(Projects);
			if (SortedProjects is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveSorting)
				liveShaping.IsLiveSorting = true;
			GraphNodes = new ObservableCollection<ProjectNodeViewModel>();
			GraphRowGroups = new ObservableCollection<GraphRowGroupViewModel>();
			RefreshCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(LoadProjectsAsync));
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

			Func<object, bool> canBuildProject = _ => !IsBuilding && !IsGraphView && SelectedProject != null;
			CleanProjectCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN, SelectedProject)), canBuildProject);
			BuildProjectCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD, SelectedProject)), canBuildProject);
			RebuildProjectCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE, SelectedProject)), canBuildProject);

			Func<object, bool> canBuildSolution = _ => !IsBuilding;
			CleanSolutionCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(() => ExecuteCommand("Build.CleanSolution")), canBuildSolution);
			BuildSolutionCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(() => ExecuteCommand("Build.BuildSolution")), canBuildSolution);
			RebuildSolutionCommand = new RelayCommand(_ => ThreadingHelper.RunOnMainThread(() => ExecuteCommand("Build.RebuildSolution")), canBuildSolution);

			CancelBuildCommand = new RelayCommand(
				_ => ThreadingHelper.RunOnMainThread(() => { ThreadHelper.ThrowIfNotOnUIThread(); _buildManager.CancelUpdateSolutionConfiguration(); }),
				_ => { ThreadHelper.ThrowIfNotOnUIThread(); return _buildManager.CanCancelUpdateSolutionConfiguration(out int canCancel) == VSConstants.S_OK && canCancel != 0; });

			Func<object, bool> canContextBuildProject = p => !IsBuilding && p is ProjectInfo;
			ContextBuildCommand = new RelayCommand(p => ThreadingHelper.RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD, (ProjectInfo)p)), canContextBuildProject);
			ContextRebuildCommand = new RelayCommand(p => ThreadingHelper.RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE, (ProjectInfo)p)), canContextBuildProject);
			ContextCleanCommand = new RelayCommand(p => ThreadingHelper.RunOnMainThread(() => BuildProject(VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN, (ProjectInfo)p)), canContextBuildProject);
			RevealInSolutionExplorerCommand = new RelayCommand(p => ThreadingHelper.RunOnMainThread(() => RevealInSolutionExplorer((ProjectInfo)p)), canContextBuildProject);

			// Subscribe to build events
			_buildEventService.BuildBegin += OnBuildBegin;
			_buildEventService.BuildDone += OnBuildDone;
			_buildEventService.ProjectStatusChanged += OnProjectStatusChanged;
			_diagnosticsService.DiagnosticsChanged += OnDiagnosticsChanged;
			_projectConfigurationService.ActiveConfigurationChanged += OnActiveConfigurationChanged;

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

			BuildStatusText = NoBuildInformationAvailableStatusText;
		}

		private void OnBuildBegin(object sender, BuildEventArgs e)
		{
			_diagnosticsService.Clear();

			ThreadingHelper.RunOnMainThread(() =>
			{
				IsBuilding = true;
				CommandManager.InvalidateRequerySuggested();

				// Reset all projects
				foreach (ProjectInfo project in Projects)
				{
					project.Status = BuildStatus.NotBuilt;
					project.BuildStart = null;
					project.BuildFinish = null;
				}

				// Start tracking overall build status
				_buildScope = e.Scope;
				_buildAction = e.Action;
				_buildStartTime = DateTime.Now;
				UpdateBuildStatusText();
				_buildTimer.Start();
			});
		}

		private void OnBuildDone(object sender, BuildEventArgs e)
		{
			ThreadingHelper.RunOnMainThread(() =>
			{
				_buildTimer.Stop();
				IsBuilding = false;
				CommandManager.InvalidateRequerySuggested();

				TimeSpan elapsed = DateTime.Now - _buildStartTime;
				string scope = _buildScope == vsBuildScope.vsBuildScopeProject ? "project in solution" : "solution";
				string duration = FormatDuration(elapsed);
				int failedCount = GetLastBuildFailedCount();

				switch (_buildAction)
				{
					case vsBuildAction.vsBuildActionClean:
						BuildStatusText = $"Cleaned {scope} successfully. Started at {_buildStartTime:HH:mm:ss} and lasted {duration}.";
						break;

					case vsBuildAction.vsBuildActionRebuildAll:
						BuildStatusText = failedCount > 0
							? $"Rebuild of {scope} failed. Started at {_buildStartTime:HH:mm:ss} and lasted {duration}."
							: $"Rebuilt {scope} successfully. Started at {_buildStartTime:HH:mm:ss} and lasted {duration}.";
						break;

					default:
						BuildStatusText = failedCount > 0
							? $"Build of {scope} failed. Started at {_buildStartTime:HH:mm:ss} and lasted {duration}."
							: $"Built {scope} successfully. Started at {_buildStartTime:HH:mm:ss} and lasted {duration}.";
						break;
				}
			});
		}

		private void OnProjectStatusChanged(object sender, ProjectStatusChangedEventArgs e)
		{
			ThreadingHelper.RunOnMainThread(() =>
			{
				ProjectInfo project = Projects.FirstOrDefault(p => string.Equals(p.UniqueName, e.ProjectUniqueName, StringComparison.OrdinalIgnoreCase));
				if (project != null)
				{
					project.Status = e.NewStatus;

					if (e.NewStatus == BuildStatus.Building || e.NewStatus == BuildStatus.Cleaning)
					{
						project.BuildStart = e.Timestamp;
						project.BuildFinish = null;
						if (e.Configuration != null) project.Configuration = e.Configuration;
						if (e.Platform != null) project.Platform = e.Platform;
					}
					else if (e.NewStatus == BuildStatus.Success || e.NewStatus == BuildStatus.Failed || e.NewStatus == BuildStatus.Skipped)
					{
						project.BuildFinish = e.Timestamp;
					}
				}
			});
		}

		private void OnHighlightChangedHandler(HashSet<ProjectNodeViewModel> involved)
		{
			foreach (ProjectNodeViewModel node in GraphNodes)
				node.IsDimmed = involved != null && !involved.Contains(node);
		}

		private void OnActiveConfigurationChanged(string projectPath, string configuration, string platform)
		{
			ThreadingHelper.RunOnMainThread(() =>
			{
				ProjectInfo project = Projects.FirstOrDefault(p => string.Equals(p.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));
				if (project != null)
				{
					project.Configuration = configuration;
					project.Platform = platform;
				}
			});
		}

		private async Task LoadProjectsAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			await _solutionService.LoadProjectReferencesAsync();
			await UpdateProjectsAsync();
			BuildStatusText = NoBuildInformationAvailableStatusText;
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
			foreach (ProjectNodeViewModel node in GraphNodes)
				node.HighlightChanged -= OnHighlightChangedHandler;

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
				node.HighlightChanged += OnHighlightChangedHandler;
				node.SetShowTransitiveDependencies(_showTransitiveDependencies);
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

		private void BuildProject(VSSOLNBUILDUPDATEFLAGS flags, ProjectInfo project)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

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

		private static readonly Guid SolutionExplorerGuid = new Guid("3AE79031-E1BC-11D0-8F78-00A0C9110057");

		private void RevealInSolutionExplorer(ProjectInfo project)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (project == null)
				return;

			int hr = _solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy hierarchy);
			if (hr != VSConstants.S_OK || hierarchy == null)
				return;

			Guid guid = SolutionExplorerGuid;
			_uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guid, out IVsWindowFrame frame);
			if (frame == null)
				return;

			frame.Show();

			if (frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView) == VSConstants.S_OK
				&& docView is IVsUIHierarchyWindow hierarchyWindow)
			{
				hierarchyWindow.ExpandItem(
					hierarchy as IVsUIHierarchy,
					VSConstants.VSITEMID_ROOT,
					EXPANDFLAGS.EXPF_SelectItem);
			}
		}

		private void OnBuildTimerTick()
		{
			UpdateBuildStatusText();

			foreach (ProjectInfo project in Projects)
			{
				if (project.Status == BuildStatus.Building || project.Status == BuildStatus.Cleaning)
					project.NotifyBuildDurationChanged();
			}
		}

		private void OnDiagnosticsChanged(string projectFile)
		{
			ThreadingHelper.RunOnMainThread(() =>
			{
				// Update global counts
				ErrorCount = _diagnosticsService.ErrorCount;
				WarningCount = _diagnosticsService.WarningCount;
				MessageCount = _diagnosticsService.MessageCount;

				if (projectFile != null)
				{
					// Update only the affected project
					ProjectInfo project = Projects.FirstOrDefault(
						p => string.Equals(p.ProjectPath, projectFile, StringComparison.OrdinalIgnoreCase));

					if (project != null)
					{
						(int errors, int warnings, int messages) = _diagnosticsService.GetDiagnosticCountsForProject(projectFile);
						project.ErrorCount = errors;
						project.WarningCount = warnings;
						project.MessageCount = messages;
					}
				}
				else
				{
					// Clear was called — reset all projects
					foreach (ProjectInfo project in Projects)
					{
						project.ErrorCount = 0;
						project.WarningCount = 0;
						project.MessageCount = 0;
					}
				}
			});
		}

		private void UpdateBuildStatusText()
		{
			TimeSpan elapsed = DateTime.Now - _buildStartTime;
			string scope = _buildScope == vsBuildScope.vsBuildScopeProject ? "project in solution" : "solution";
			string duration = FormatDuration(elapsed);
			string task;

			switch (_buildAction)
			{
				case vsBuildAction.vsBuildActionClean:
					task = "Cleaning";
					break;
				case vsBuildAction.vsBuildActionRebuildAll:
					task = "Rebuilding";
					break;
				default:
					task = "Building";
					break;
			}

			BuildStatusText = $"{task} {scope}. Started at {_buildStartTime:HH:mm:ss} and has been ongoing for {duration}.";
		}

		private int GetLastBuildFailedCount()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return _dte.Solution?.SolutionBuild?.LastBuildInfo ?? 0;
		}

		private static string FormatDuration(TimeSpan elapsed)
		{
			int totalSeconds = (int)elapsed.TotalSeconds;
			int minutes = totalSeconds / 60;
			int seconds = totalSeconds % 60;

			if (minutes == 0)
				return seconds == 1 ? "1 second" : $"{seconds} seconds";

			string minutePart = minutes == 1 ? "1 minute" : $"{minutes} minutes";
			string secondPart = seconds == 1 ? "1 second" : $"{seconds} seconds";
			return $"{minutePart} and {secondPart}";
		}

		private void OnProjectsChanged(IReadOnlyList<ProjectReferences> projectReferences)
		{
			_solutionService.UpdateProjectReferences(projectReferences);
			ThreadingHelper.RunOnMainThread(async () =>
			{
				await UpdateProjectsAsync();
				if (!_buildTimer.IsEnabled)
					BuildStatusText = NoBuildInformationAvailableStatusText;
			});
		}

		private void OnSolutionClosed(object sender, EventArgs e)
		{
			ThreadingHelper.RunOnMainThread(() =>
			{
				_buildTimer.Stop();
				Projects.Clear();
				GraphNodes.Clear();
				GraphRowGroups.Clear();
				BuildStatusText = NoBuildInformationAvailableStatusText;
			});
		}

		private void OnThemeChanged(object sender, EventArgs e)
		{
			bool isDarkTheme = _themeService.IsDarkTheme;
			Resources.Colors.IsDarkTheme = isDarkTheme;
			ThreadingHelper.RunOnMainThread(() =>
			{
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
