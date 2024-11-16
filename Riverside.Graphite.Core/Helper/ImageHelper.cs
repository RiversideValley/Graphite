using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;

namespace Riverside.Graphite.Core.Helper;
public partial class ImageHelper : MarkupExtension
{
	private static readonly Dictionary<string, BitmapImage> ImageCache = new();

	public string ImageName { get; set; }

	protected override object ProvideValue()
	{
		return LoadImage(ImageName);
	}

	public BitmapImage LoadImage(string imageName)
	{
		return string.IsNullOrEmpty(imageName) ? null : ImageCache.TryGetValue(imageName, out BitmapImage cachedImage)
			? cachedImage
			: ImageCache[imageName] = new BitmapImage(new Uri($"ms-appx:///Riverside.Graphite.Core//Assets/{imageName}"));
	}
}