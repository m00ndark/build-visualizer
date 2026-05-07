using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuildVisualizer.ToolWindow
{
	/// <summary>
	/// Interaction logic for BuildVisualizerToolWindowControl.
	/// </summary>
	public partial class BuildVisualizerToolWindowControl : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BuildVisualizerToolWindowControl"/> class.
		/// </summary>
		public BuildVisualizerToolWindowControl(SolutionService solutionService, BuildEventService buildEventService, SolutionEventsService solutionEventsService)
		{
			InitializeComponent();

			DataContext = new BuildVisualizerViewModel(solutionService, buildEventService, solutionEventsService);
		}

		private void DependencyLine_MouseEnter(object sender, MouseEventArgs e)
		{
			if (sender is System.Windows.Shapes.Path path && path.DataContext is DependencyLineViewModel lineViewModel)
			{
				lineViewModel.IsHighlighted = true;
			}
		}

		private void DependencyLine_MouseLeave(object sender, MouseEventArgs e)
		{
			if (sender is System.Windows.Shapes.Path path && path.DataContext is DependencyLineViewModel lineViewModel)
			{
				lineViewModel.IsHighlighted = false;
			}
		}
	}
}