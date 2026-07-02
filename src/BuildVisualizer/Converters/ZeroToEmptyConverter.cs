using System;
using System.Globalization;
using System.Windows.Data;

namespace BuildVisualizer.Converters
{
	public class ZeroToEmptyConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is int i && i > 0 ? i.ToString() : string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
