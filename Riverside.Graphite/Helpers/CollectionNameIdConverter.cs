using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Helpers
{
	internal class CollectionNameIdConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is CollectionName collection)
			{
				return collection.Name ;
			}
			return string.Empty	;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}

	
}
