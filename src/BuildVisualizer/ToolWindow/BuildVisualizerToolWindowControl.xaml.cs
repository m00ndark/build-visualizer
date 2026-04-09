using BuildVisualizer.Services;
using BuildVisualizer.ViewModels;
using EnvDTE80;
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
		public BuildVisualizerToolWindowControl(DTE2 dte)
		{
			this.InitializeComponent();
			DataContext = new BuildVisualizerViewModel(new SolutionService(dte));
		}
	}
}