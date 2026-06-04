using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace BuildVisualizer.Converters
{
	public class SortedCollectionConverter : IValueConverter
	{
		public string SortProperty { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is IEnumerable source))
				return value;

			ListCollectionView view = new ListCollectionView(new ArrayList(source as IList ?? new ArrayList()));
			view.SortDescriptions.Add(new SortDescription(SortProperty ?? (parameter as string), ListSortDirection.Ascending));
			return view;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
