using Riverside.Graphite.Helpers;
using System;
using System.Threading.Tasks;

namespace Riverside.Graphite.Navigation;

public class TLD
{
	public static string KnownDomains { get; set; }

	public static async Task LoadKnownDomainsAsync()
	{
		KnownDomains = await LoadFileHelper.LoadFileAsync(new Uri("ms-appx:///Assets/KnownDomains.txt"));
	}

	public static string GetTLDfromURL(string url)
	{
		return url[(url.LastIndexOf(".") + 1)..];
	}
}
