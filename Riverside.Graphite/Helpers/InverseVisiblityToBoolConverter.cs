using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Riverside.Graphite.Helpers
{
	internal class InverseVisiblityToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is Visibility visual)
			{
				return visual is Visibility.Visible ? false : true;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is bool visibility)
			{
				return visibility == true ? Visibility.Collapsed : Visibility.Visible;
			}
			return false;
		}
	}
}
