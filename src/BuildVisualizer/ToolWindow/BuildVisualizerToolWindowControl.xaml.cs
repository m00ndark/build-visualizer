using BuildVisualizer.Converters;
using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BuildVisualizer.ToolWindow
{
	/// <summary>
	/// Interaction logic for BuildVisualizerToolWindowControl.
	/// </summary>
	public partial class BuildVisualizerToolWindowControl : UserControl
	{
		private const double MinDependenciesColumnWidth = 60;

		private ListViewStateService _listViewStateService;

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

			GridView gridView = (GridView)ProjectListView.View;
			_listViewStateService = new ListViewStateService(userSettingsService);
			_listViewStateService.Attach(gridView);
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
			menu.Style = TryFindResource(typeof(ContextMenu)) as Style;
			foreach (MenuItem item in menu.Items.OfType<MenuItem>())
				item.Style = TryFindResource(typeof(MenuItem)) as Style;
			menu.IsOpen = true;
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
	}
}
