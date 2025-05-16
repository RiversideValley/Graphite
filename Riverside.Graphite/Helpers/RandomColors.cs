using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Riverside.Graphite.Helpers
{
	public class RandomColors
	{
		public static SolidColorBrush GetRandomSolidColorBrush()
		{
			Random random = new Random();
			byte r = (byte)random.Next(128);
			byte g = (byte)random.Next(128);
			byte b = (byte)random.Next(128);
			return new SolidColorBrush(Color.FromArgb(128, r, g, b));
		}
	}
}
