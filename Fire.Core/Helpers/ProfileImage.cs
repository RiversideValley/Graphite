using Riverside.Graphite.Core;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Fire.Core.Helpers;

public class ProfileImage : MarkupExtension
{
	private static readonly Dictionary<string, BitmapImage> ImageCache = new();

	public string ImageName { get; set; }

	protected override object ProvideValue()
	{
		if (string.IsNullOrEmpty(ImageName))
			return null;

		if (ImageCache.TryGetValue(ImageName, out var cachedImage))
			return cachedImage;

		string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser?.Username);
		string imagePath = Path.Combine(destinationFolderPath, ImageName);

		try
		{
			var uri = new Uri(imagePath);
			var bitmapImage = new BitmapImage(uri);
			ImageCache[ImageName] = bitmapImage;

			return bitmapImage;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading image: {ex.Message}");
			return null; // Return a placeholder image or null as needed.
		}
	}
}