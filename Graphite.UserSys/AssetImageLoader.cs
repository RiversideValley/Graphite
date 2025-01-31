using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Graphite.UserSys;
public class AssetImageLoader : MarkupExtension
{
    private static readonly Dictionary<string, BitmapImage> ImageCache = new();

    public string ImageName { get; set; }

    protected override object ProvideValue()
    {
        return LoadImage(ImageName);
    }

    public static BitmapImage LoadImage(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return null;

        if (ImageCache.TryGetValue(imageName, out BitmapImage cachedImage))
            return cachedImage;

        var uri = new Uri($"ms-appx:///Graphite.UserSys/Assets/{imageName}.png");
        var image = new BitmapImage(uri);
        ImageCache[imageName] = image;
        return image;
    }

    public static List<string> GetStockImageNames()
    {
        string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Graphite.UserSys", "Assets");
        return Directory.GetFiles(assetsPath, "*.png")
                        .Select(Path.GetFileNameWithoutExtension)
                        .ToList();
    }

    public static string GetStockImageUri(string imageName)
    {
        return $"ms-appx:///Graphite.UserSys/Assets/{imageName}.png";
    }
}