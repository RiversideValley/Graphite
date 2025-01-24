using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Helpers
{
	public class TextSubStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (parameter is not null)
				if (value is string str)
				{
					string[] parameters = ((string)parameter).Split(',');
					int startIndex = int.Parse(parameters[0]);
					int length = int.Parse(parameters[1]);
					return str.Substring(startIndex, length);
				}
			return value;
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
	
}
