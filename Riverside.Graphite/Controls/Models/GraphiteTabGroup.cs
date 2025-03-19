using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Controls.Models
{
	public class GraphiteTabGroup
	{
		public string Name { get; set; }
		public string Color { get; set; } = "#FF0078D7"; // Default blue color
		public ObservableCollection<GraphiteTabViewItem> Tabs { get; } = new ObservableCollection<GraphiteTabViewItem>();

		// Generate a color brush from the color string
		public SolidColorBrush GetColorBrush()
		{
			try
			{
				if (string.IsNullOrEmpty(Color))
					return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)); // Default blue

				Color = Color.Replace("#", string.Empty);
				byte a = 255;
				byte r = 0;
				byte g = 0;
				byte b = 0;

				if (Color.Length == 8)
				{
					a = Convert.ToByte(Color.Substring(0, 2), 16);
					Color = Color.Substring(2);
				}

				if (Color.Length == 6)
				{
					r = Convert.ToByte(Color.Substring(0, 2), 16);
					g = Convert.ToByte(Color.Substring(2, 2), 16);
					b = Convert.ToByte(Color.Substring(4, 2), 16);
				}
				else if (Color.Length == 3)
				{
					r = Convert.ToByte(Color[0] + Color[0].ToString(), 16);
					g = Convert.ToByte(Color[1] + Color[1].ToString(), 16);
					b = Convert.ToByte(Color[2] + Color[2].ToString(), 16);
				}

				return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
			}
			catch
			{
				return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)); // Default blue as fallback
			}
		}
	}
}
