using BuildVisualizer.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // MouseEventArgs

namespace BuildVisualizer.Views
{
	public partial class ProjectNodeControl : UserControl
	{
		public ProjectNodeControl()
		{
			InitializeComponent();
			MouseEnter += OnMouseEnter;
			MouseLeave += OnMouseLeave;

			if (ContextMenu != null)
				ContextMenu.Closed += OnContextMenuClosed;
		}

		private void OnMouseEnter(object sender, MouseEventArgs e)
		{
			if (DataContext is ProjectNodeViewModel vm)
				vm.SetHovered(true);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			// Don't unhighlight while context menu is open
			if (ContextMenu != null && ContextMenu.IsOpen)
				return;

			if (DataContext is ProjectNodeViewModel vm)
				vm.SetHovered(false);
		}

		private void OnContextMenuClosed(object sender, RoutedEventArgs e)
		{
			// Clear highlight when context menu closes (unless mouse is still over the node)
			if (!IsMouseOver && DataContext is ProjectNodeViewModel vm)
				vm.SetHovered(false);
		}
	}
}
