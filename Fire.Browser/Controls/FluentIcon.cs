using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace FireBrowserWinUi3.Controls;
public sealed partial class FluentIcon : CommunityToolkit.WinUI.FontIconExtension
{
	private static readonly Dictionary<string, FontFamily> FontFamilyCache = new Dictionary<string, FontFamily>();
	private const string FluentIconsKey = "FluentIcons";
	public FluentIcon()
	{
		// Ensure thread-safe initialization of the FontFamilyCache
		if (!FontFamilyCache.ContainsKey(FluentIconsKey))
		{
			lock (FontFamilyCache)
			{
				if (!FontFamilyCache.ContainsKey(FluentIconsKey))
				{
					if (Application.Current.Resources.TryGetValue(FluentIconsKey, out object fontFamilyObj) && fontFamilyObj is FontFamily fontFamily)
					{
						FontFamilyCache[FluentIconsKey] = fontFamily;
					}
					else
					{
						// Handle the case where the FluentIcons font family is not found in resources
						throw new InvalidOperationException("FluentIcons font family not found in resources.");
					}
				}
			}
		}

		// Assign the cached FontFamily to the control
		FontFamily = FontFamilyCache[FluentIconsKey];
	}
}