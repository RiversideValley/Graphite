using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Riverside.Graphite.Controls;

public partial class TabStates
{
	public record TabAll(Guid Tag, string Header, string Url, IconSource FaviconUrl)
	{
		public BitmapImage FaviconUrlString => GetIconSourceUrl(FaviconUrl);

		private BitmapImage GetIconSourceUrl(IconSource iconSource)
		{
			if (iconSource is ImageIconSource bitmapIconSource)
			{
				return (BitmapImage)bitmapIconSource.ImageSource; 
			}
			// Add more conditions if you have other types of IconSource
			return null;
		}
	}
	
}