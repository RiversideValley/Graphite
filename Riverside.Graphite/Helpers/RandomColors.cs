using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Riverside.Graphite.Helpers
{
	public class RandomColors
	{
		public static SolidColorBrush GetRandomSolidColorBrush()
		{
			Random random = new Random();
			byte r = (byte)random.Next(256);
			byte g = (byte)random.Next(256);
			byte b = (byte)random.Next(256);
			return new SolidColorBrush(Color.FromArgb(255, r, g, b));
		}
	}
}
