using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Riverside.Graphite.Runtime.Helpers;

public partial class ProfileImage : MarkupExtension
{
	private static readonly Dictionary<string, BitmapImage> ImageCache = new();

	public string ImageName { get; set; }

	protected override object ProvideValue()
	{
		if (string.IsNullOrEmpty(ImageName))
		{
			return null;
		}

		if (ImageCache.TryGetValue(ImageName, out BitmapImage cachedImage))
		{
			return cachedImage;
		}

		string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser?.Username);
		string imagePath = Path.Combine(destinationFolderPath, ImageName);

		try
		{
			Uri uri = new(imagePath);
			BitmapImage bitmapImage = new(uri);
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