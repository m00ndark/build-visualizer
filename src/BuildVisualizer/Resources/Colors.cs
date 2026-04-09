using System.Windows.Media;

namespace BuildVisualizer.Resources
{
	public static class Colors
	{
		public static readonly SolidColorBrush NotBuiltBrush;
		public static readonly SolidColorBrush BuildingBrush;
		public static readonly SolidColorBrush SuccessBrush;
		public static readonly SolidColorBrush FailedBrush;
		public static readonly SolidColorBrush SkippedBrush;

		static Colors()
		{
			NotBuiltBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
			NotBuiltBrush.Freeze();

			BuildingBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
			BuildingBrush.Freeze();

			SuccessBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
			SuccessBrush.Freeze();

			FailedBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
			FailedBrush.Freeze();

			SkippedBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
			SkippedBrush.Freeze();
		}
	}
}
