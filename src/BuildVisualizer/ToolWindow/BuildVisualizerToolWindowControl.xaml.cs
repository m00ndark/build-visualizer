using BuildVisualizer.Services;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace BuildVisualizer.ToolWindow
{
	/// <summary>
	/// Interaction logic for BuildVisualizerToolWindowControl.
	/// </summary>
	public partial class BuildVisualizerToolWindowControl : UserControl
	{
		private readonly DTE2 _dte;

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildVisualizerToolWindowControl"/> class.
		/// </summary>
		public BuildVisualizerToolWindowControl(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_dte = dte;
			this.InitializeComponent();
			LoadProjects();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			LoadProjects();
		}

		private void LoadProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_dte == null)
			{
				return;
			}

			var solutionService = new SolutionService(_dte);
			var projects = solutionService.GetProjects();

			ProjectListBox.ItemsSource = projects;
		}
	}
}