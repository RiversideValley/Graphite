using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

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
