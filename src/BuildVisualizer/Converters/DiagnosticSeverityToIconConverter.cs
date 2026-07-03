using BuildVisualizer.Models;
using Microsoft.VisualStudio.Imaging;
using System;
using System.Globalization;
using System.Windows.Data;

namespace BuildVisualizer.Converters
{
	public class DiagnosticSeverityToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DiagnosticSeverity severity)
			{
				switch (severity)
				{
					case DiagnosticSeverity.Error:
						return KnownMonikers.StatusError;
					case DiagnosticSeverity.Warning:
						return KnownMonikers.StatusWarning;
					case DiagnosticSeverity.Message:
						return KnownMonikers.StatusInformation;
				}
			}

			return KnownMonikers.StatusInformation;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
