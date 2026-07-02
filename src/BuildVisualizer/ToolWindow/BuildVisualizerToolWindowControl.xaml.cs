using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BuildVisualizer.ToolWindow
{
	/// <summary>
	/// Interaction logic for BuildVisualizerToolWindowControl.
	/// </summary>
	public partial class BuildVisualizerToolWindowControl : UserControl
	{
		private const double MinDependenciesColumnWidth = 60;

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

			DataContext = new BuildVisualizerViewModel(solutionService, buildEventService, solutionEventsService, themeService, dte, solution, buildManager, uiShell, diagnosticsService, projectConfigurationService);

			GridView gridView = (GridView)ProjectListView.View;
			new ListViewStateService(userSettingsService).Attach(gridView);
		}

		private void ProjectListView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			double fixedWidth = ((GridView)ProjectListView.View).Columns
				.Where(column => column != DependenciesColumn)
				.Sum(column => column.ActualWidth);

			double available = ProjectListView.ActualWidth - fixedWidth - (SystemParameters.VerticalScrollBarWidth + 8);
			DependenciesColumn.Width = Math.Max(available, MinDependenciesColumnWidth);
		}
	}
}
