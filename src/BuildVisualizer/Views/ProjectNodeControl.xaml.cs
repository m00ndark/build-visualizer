using BuildVisualizer.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuildVisualizer.Views
{
	public partial class ProjectNodeControl : UserControl
	{
		public ProjectNodeControl()
		{
			InitializeComponent();
			MouseEnter += OnMouseEnter;
			MouseLeave += OnMouseLeave;
		}

		private void OnMouseEnter(object sender, MouseEventArgs e)
		{
			if (DataContext is ProjectNodeViewModel vm)
				vm.SetHovered(true);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (DataContext is ProjectNodeViewModel vm)
				vm.SetHovered(false);
		}
	}
}
