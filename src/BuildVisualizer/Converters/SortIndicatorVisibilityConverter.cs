using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BuildVisualizer.Converters
{
	public class SortIndicatorVisibilityConverter : IMultiValueConverter
	{
		// values[0]: this column's sort property (string, from GridViewColumnHeader.CommandParameter)
		// values[1]: active sort property (string, from ViewModel.SortProperty)
		// values[2]: active sort direction (ListSortDirection, from ViewModel.SortDirection)
		// parameter: "Ascending" or "Descending" — which arrow this instance represents
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is string columnProp &&
				values[1] is string activeProp &&
				values[2] is ListSortDirection direction &&
				parameter is string targetDirection)
			{
				bool isActiveColumn = string.Equals(columnProp, activeProp, StringComparison.Ordinal);
				bool isTargetDirection = direction.ToString() == targetDirection;
				return isActiveColumn && isTargetDirection ? Visibility.Visible : Visibility.Collapsed;
			}

			return Visibility.Collapsed;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
