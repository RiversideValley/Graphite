using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Graphite.Controls.SettingsControls.Converters
{
	public class CornerRadiusConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is CornerRadius radius)
			{
				return radius;
			}
			return new CornerRadius();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}

