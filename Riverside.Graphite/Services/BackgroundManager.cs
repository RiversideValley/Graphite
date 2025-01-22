using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Riverside.Graphite.Runtime.Models;
using Windows.UI;

namespace Riverside.Graphite.Services
{
	public class BackgroundManager
	{
		public async Task<Brush> GetGridBackgroundAsync(Settings.NewTabBackground backgroundType, Riverside.Graphite.Core.Settings userSettings)
		{
			switch (backgroundType)
			{
				case Settings.NewTabBackground.None:
					return new SolidColorBrush(Colors.Transparent);

				case Settings.NewTabBackground.Costum:
					string colorString = userSettings.ColorBackground?.ToString() ?? "#000000";

					Color color = colorString == "#000000" ?
					 Colors.Transparent :
					 (Color)XamlBindingHelper.ConvertValue(typeof(Color), colorString);
					return new SolidColorBrush(color);

				case Settings.NewTabBackground.Featured:
					return await BackgroundImageManager.GetFeaturedBackgroundAsync();

				default:
					return new SolidColorBrush(Colors.Transparent);
			}
		}
	}
}
