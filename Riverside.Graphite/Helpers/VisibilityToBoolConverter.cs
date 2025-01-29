﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Helpers
{
	internal class VisibilityToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is Visibility visual)
			{
				return visual is Visibility.Visible ? true : false;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is bool visibility)
			{
				return visibility == true ? Visibility.Visible : Visibility.Collapsed;	
			}
			return false;
		}
	}
}

