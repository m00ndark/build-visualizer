using System.Collections.ObjectModel;
using System.Windows.Media;

namespace BuildVisualizer.ViewModels
{
	public class GraphRowGroupViewModel : ViewModelBase
	{
		private const byte GrayscaleLightValue = 100;
		private const byte GrayscaleDarkValue  = 75;
		private const byte BackgroundAlphaLight = 40;
		private const byte BackgroundAlphaDark  = 65;

		private readonly int _maxLayers;

		public int LayerIndex { get; }

		public ObservableCollection<ProjectNodeViewModel> Nodes { get; }

		public SolidColorBrush BackgroundBrush { get; }

		public GraphRowGroupViewModel(int layerIndex, int maxLayers, bool isDarkTheme)
		{
			LayerIndex  = layerIndex;
			_maxLayers  = maxLayers;
			Nodes       = new ObservableCollection<ProjectNodeViewModel>();
			BackgroundBrush = new SolidColorBrush(GetGrayscaleColor(layerIndex, maxLayers, isDarkTheme));
		}

		public void UpdateTheme(bool isDarkTheme)
		{
			BackgroundBrush.Color = GetGrayscaleColor(LayerIndex, _maxLayers, isDarkTheme);
		}

		private static Color GetGrayscaleColor(int layerIndex, int maxLayers, bool isDarkTheme)
		{
			double t    = maxLayers > 1 ? (double) layerIndex / (maxLayers - 1) : 0.0;
			byte gray   = (byte) (GrayscaleLightValue + t * (GrayscaleDarkValue - GrayscaleLightValue));
			byte alpha  = isDarkTheme ? BackgroundAlphaDark : BackgroundAlphaLight;
			return Color.FromArgb(alpha, gray, gray, gray);
		}
	}
}
