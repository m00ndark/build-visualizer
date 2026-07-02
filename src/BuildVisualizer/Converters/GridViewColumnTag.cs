using System.Windows;
using System.Windows.Controls;

namespace BuildVisualizer.Converters
{
	public static class GridViewColumnTag
	{
		public static readonly DependencyProperty TagProperty =
			DependencyProperty.RegisterAttached(
				"Tag",
				typeof(string),
				typeof(GridViewColumnTag),
				new PropertyMetadata(null));

		public static void SetTag(GridViewColumn element, string value) =>
			element.SetValue(TagProperty, value);

		public static string GetTag(GridViewColumn element) =>
			(string)element.GetValue(TagProperty);
	}
}
