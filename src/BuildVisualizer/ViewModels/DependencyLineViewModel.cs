using System.ComponentModel;

namespace BuildVisualizer.ViewModels
{
	public class DependencyLineViewModel : ViewModelBase
	{
		private double _x1;
		private double _y1;
		private double _x2;
		private double _y2;

		private readonly ProjectNodeViewModel _source;
		private readonly ProjectNodeViewModel _target;

		public double X1
		{
			get => _x1;
			set => SetProperty(ref _x1, value);
		}

		public double Y1
		{
			get => _y1;
			set => SetProperty(ref _y1, value);
		}

		public double X2
		{
			get => _x2;
			set => SetProperty(ref _x2, value);
		}

		public double Y2
		{
			get => _y2;
			set => SetProperty(ref _y2, value);
		}

		public DependencyLineViewModel(ProjectNodeViewModel source, ProjectNodeViewModel target)
		{
			_source = source;
			_target = target;

			// Subscribe to position changes
			_source.PropertyChanged += OnNodePositionChanged;
			_target.PropertyChanged += OnNodePositionChanged;

			// Initial calculation
			UpdateLinePositions();
		}

		private void OnNodePositionChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ProjectNodeViewModel.X) ||
				e.PropertyName == nameof(ProjectNodeViewModel.Y) ||
				e.PropertyName == nameof(ProjectNodeViewModel.Width) ||
				e.PropertyName == nameof(ProjectNodeViewModel.Height))
			{
				UpdateLinePositions();
			}
		}

		private void UpdateLinePositions()
		{
			// Calculate line from center of source to center of target
			X1 = _source.X + _source.Width / 2;
			Y1 = _source.Y + _source.Height / 2;
			X2 = _target.X + _target.Width / 2;
			Y2 = _target.Y + _target.Height / 2;
		}
	}
}
