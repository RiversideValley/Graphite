using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;

namespace Fire.Browser.Assets;
public class ImageLoader : MarkupExtension
{
	private static readonly ConcurrentDictionary<string, BitmapImage> ImageCache = new ConcurrentDictionary<string, BitmapImage>();

	private string _imageName;

	public string ImageName
	{
		get => _imageName;
		set
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentException("ImageName cannot be null or empty.");
			_imageName = value;
		}
	}

	protected override object ProvideValue() => LoadImage(ImageName);

	public BitmapImage LoadImage(string imageName)
	{
		if (string.IsNullOrEmpty(imageName))
			return null;

		if (ImageCache.TryGetValue(imageName, out var cachedImage))
			return cachedImage;

		try
		{
			var uri = new Uri($"ms-appx:///Fire.Browser.Assets/Assets/{imageName}");

			var _ = uri; // Dispose Uri object after use

			cachedImage = new BitmapImage(uri);
			ImageCache.TryAdd(imageName, cachedImage);
		}
		catch (Exception ex)
		{
			// Handle URI creation errors gracefully
			Console.WriteLine($"Failed to load image '{imageName}': {ex.Message}");
		}

		return cachedImage;
	}
}