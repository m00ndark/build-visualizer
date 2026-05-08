using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using System.ComponentModel;
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
		private GridViewColumnHeader _lastSortHeader;
		private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildVisualizerToolWindowControl"/> class.
		/// </summary>
		public BuildVisualizerToolWindowControl(SolutionService solutionService, BuildEventService buildEventService, SolutionEventsService solutionEventsService)
		{
			InitializeComponent();

			DataContext = new BuildVisualizerViewModel(solutionService, buildEventService, solutionEventsService);
		}

		private void ListViewHeader_Click(object sender, RoutedEventArgs e)
		{
			if (!(e.OriginalSource is GridViewColumnHeader header) || header.Tag == null)
				return;

			string sortBy = header.Tag.ToString();
			ListSortDirection direction = (header == _lastSortHeader && _lastSortDirection == ListSortDirection.Ascending)
				? ListSortDirection.Descending
				: ListSortDirection.Ascending;

			BuildVisualizerViewModel vm = DataContext as BuildVisualizerViewModel;
			if (vm == null) return;

			vm.SortedProjects.SortDescriptions.Clear();
			vm.SortedProjects.SortDescriptions.Add(new SortDescription(sortBy, direction));

			if (_lastSortHeader != null)
				_lastSortHeader.Column.HeaderTemplate = (DataTemplate)FindResource("SortableHeaderTemplate");

			header.Column.HeaderTemplate = direction == ListSortDirection.Ascending
				? (DataTemplate)FindResource("SortableHeaderAscTemplate")
				: (DataTemplate)FindResource("SortableHeaderDescTemplate");

			_lastSortHeader = header;
			_lastSortDirection = direction;
		}
	}
}
