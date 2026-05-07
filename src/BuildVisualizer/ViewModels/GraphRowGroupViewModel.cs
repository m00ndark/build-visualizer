using System.Collections.ObjectModel;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class GraphRowGroupViewModel : ViewModelBase
	{
		private const byte GrayscaleLightValue = 200;
		private const byte GrayscaleDarkValue = 100;
		private const byte BackgroundAlpha = 40;

		public int LayerIndex { get; }

		public ObservableCollection<ProjectNodeViewModel> Nodes { get; }

		public SolidColorBrush BackgroundBrush { get; }

		public GraphRowGroupViewModel(int layerIndex, int maxLayers)
		{
			LayerIndex = layerIndex;
			Nodes = new ObservableCollection<ProjectNodeViewModel>();
			BackgroundBrush = CreateGrayscaleBrush(layerIndex, maxLayers);
		}

		private static SolidColorBrush CreateGrayscaleBrush(int layerIndex, int maxLayers)
		{
			double t = maxLayers > 1 ? (double) layerIndex / (maxLayers - 1) : 0.0;
			byte gray = (byte) (GrayscaleLightValue + t * (GrayscaleDarkValue - GrayscaleLightValue));
			return new SolidColorBrush(Color.FromArgb(BackgroundAlpha, gray, gray, gray));
		}
	}
}
