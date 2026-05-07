using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using System.Windows.Controls;

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
	}
}