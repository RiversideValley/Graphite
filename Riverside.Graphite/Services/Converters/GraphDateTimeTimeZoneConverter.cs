// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models;
using Microsoft.UI.Xaml.Data;
using System;

namespace Riverside.Graphite.Services.Converters
{
	/// <summary>
	/// Convert a Graph DateTimeTimeZone value to a human-readable string
	/// </summary>
	public class GraphDateTimeTimeZoneConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is DateTimeTimeZone date)
			{
				DateTimeOffset parsedDateAs = DateTimeOffset.Parse(date.DateTime);
				// Return the local date time string
				return $"{parsedDateAs.LocalDateTime.ToShortDateString()} {parsedDateAs.LocalDateTime.ToShortTimeString()}";
			}

			return string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
