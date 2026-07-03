using BuildVisualizer.Converters;
using BuildVisualizer.Models;
using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using BuildVisualizer.Views;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static BuildVisualizer.Services.UserSettings;

namespace BuildVisualizer.ToolWindow
{
	/// <summary>
	/// Interaction logic for BuildVisualizerToolWindowControl.
	/// </summary>
	public partial class BuildVisualizerToolWindowControl : UserControl
	{
		private const double MinDependenciesColumnWidth = 60;
		private const double DefaultDetailsPanelWidth = 300;

		private ListViewStateService _listViewStateService;
		private UserSettingsService _userSettingsService;
		private GridLength _detailsPanelWidth = new GridLength(DefaultDetailsPanelWidth);

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildVisualizerToolWindowControl"/> class.
		/// </summary>
		public BuildVisualizerToolWindowControl(
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
			InitializeComponent();

			BuildVisualizerViewModel viewModel = new BuildVisualizerViewModel(
				solutionService, buildEventService, solutionEventsService, themeService,
				dte, solution, buildManager, uiShell, diagnosticsService, projectConfigurationService,
				userSettingsService);

			DataContext = viewModel;
			_userSettingsService = userSettingsService;

			GridView gridView = (GridView)ProjectListView.View;
			_listViewStateService = new ListViewStateService(userSettingsService);
			_listViewStateService.Attach(gridView);

			// Restore persisted details panel width
			string savedWidth = userSettingsService?.GetString(Collections.Settings, Keys.DetailsPanelWidth);
			if (double.TryParse(savedWidth, out double width) && width > 0)
				_detailsPanelWidth = new GridLength(width);

			// Sync column width with ShowProjectDetails visibility
			viewModel.PropertyChanged += OnViewModelPropertyChanged;
			UpdateDetailsPanelColumnWidth(viewModel.ShowProjectDetails);

			// Set theme-aware selection brush and update on theme change
			UpdateSelectionBrush();
			themeService.ThemeChanged += (s, _) => UpdateSelectionBrush();
		}

		private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BuildVisualizerViewModel.ShowProjectDetails)
				&& sender is BuildVisualizerViewModel vm)
			{
				UpdateDetailsPanelColumnWidth(vm.ShowProjectDetails);
			}
		}

		internal void UpdateSelectionBrush()
		{
			Resources["ListSelectionBrush"] = BuildVisualizer.Resources.Colors.SelectionBackground;
		}

		private void UpdateDetailsPanelColumnWidth(bool show)
		{
			DetailsPanelColumn.Width = show ? _detailsPanelWidth : new GridLength(0);
		}

		private void ProjectListView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			double fixedWidth = ((GridView)ProjectListView.View).Columns
				.Where(column => column != DependenciesColumn)
				.Sum(column => column.ActualWidth);

			double available = ProjectListView.ActualWidth - fixedWidth - (SystemParameters.VerticalScrollBarWidth + 8);
			DependenciesColumn.Width = Math.Max(available, MinDependenciesColumnWidth);
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			ContextMenu menu = SettingsButton.ContextMenu;
			menu.DataContext = DataContext;
			ApplyMenuStyles(menu);
			menu.IsOpen = true;
		}

		private void ApplyMenuStyles(ItemsControl menu)
		{
			if (menu is ContextMenu contextMenu)
				contextMenu.Style = TryFindResource(typeof(ContextMenu)) as Style;

			Style menuItemStyle = TryFindResource(typeof(MenuItem)) as Style;
			foreach (MenuItem item in menu.Items.OfType<MenuItem>())
			{
				item.Style = menuItemStyle;
				if (item.Items.Count > 0)
					ApplyMenuStyles(item);
			}
		}

		private void ProjectListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (DataContext is BuildVisualizerViewModel vm && vm.SelectedProject != null)
				vm.ShowProjectDetails = true;
		}

		private void ProjectListView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Only handle right-clicks on column headers
			DependencyObject hit = e.OriginalSource as DependencyObject;
			while (hit != null && !(hit is GridViewColumnHeader))
				hit = VisualTreeHelper.GetParent(hit);

			if (!(hit is GridViewColumnHeader))
				return;

			e.Handled = true;

			GridView gridView = (GridView)ProjectListView.View;
			ContextMenu menu = BuildColumnContextMenu(gridView);
			menu.IsOpen = true;
		}

		private ContextMenu BuildColumnContextMenu(GridView gridView)
		{
			ContextMenu menu = new ContextMenu
			{
				Style = TryFindResource(typeof(ContextMenu)) as Style
			};

			Style menuItemStyle = TryFindResource(typeof(MenuItem)) as Style;

			foreach (ListViewColumnDefinition def in ListViewStateService.AllColumns)
			{
				bool visible = gridView.Columns.Any(c => GridViewColumnTag.GetTag(c) == def.Key);
				MenuItem item = new MenuItem
				{
					Header = def.Header,
					IsCheckable = true,
					IsChecked = visible,
					Tag = def.Key,
					Style = menuItemStyle
				};
				item.Click += ColumnMenuItem_Click;
				menu.Items.Add(item);
			}

			return menu;
		}

		private void ColumnMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)sender;
			string key = (string)item.Tag;
			GridView gridView = (GridView)ProjectListView.View;
			_listViewStateService.SetColumnVisible(gridView, key, item.IsChecked);
		}

		private void DetailsSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			_detailsPanelWidth = DetailsPanelColumn.Width;
			_userSettingsService?.SetString(
				Collections.Settings,
				Keys.DetailsPanelWidth,
				DetailsPanelColumn.ActualWidth.ToString("F0"));
		}

		private void GraphScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Deselect when clicking empty space in the graph view (not on a node)
			DependencyObject hit = e.OriginalSource as DependencyObject;
			while (hit != null)
			{
				if (hit is ProjectNodeControl)
					return; // Click is on a node — let it handle selection
				hit = VisualTreeHelper.GetParent(hit);
			}

			if (DataContext is BuildVisualizerViewModel vm)
				vm.SelectGraphNode(null);
		}

		private void DiagnosticRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2
				&& sender is FrameworkElement element
				&& element.DataContext is BuildDiagnostic diagnostic
				&& DataContext is BuildVisualizerViewModel vm
				&& vm.NavigateToDiagnosticCommand.CanExecute(diagnostic))
			{
				vm.NavigateToDiagnosticCommand.Execute(diagnostic);
				e.Handled = true;
			}
		}
	}
}
