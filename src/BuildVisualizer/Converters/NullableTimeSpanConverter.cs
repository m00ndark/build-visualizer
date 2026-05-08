using System;
using System.Globalization;
using System.Windows.Data;

namespace BuildVisualizer.Converters
{
	public class NullableTimeSpanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is TimeSpan ts)
				return $"{ts.TotalSeconds:0.000}s";

			return string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
