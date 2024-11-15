using Riverside.Graphite.Runtime.Helpers.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Navigation;

public class TLD
{
	public static string KnownDomains { get; set; }

	public static async Task LoadKnownDomainsAsync()
	{
		try
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///public_domains.txt"));
			KnownDomains = await new FireTxtReader().ReadTextFile(file);
		}
		catch (FileNotFoundException ex) when (HandleFileNotFoundException(ex))
		{
			Debug.WriteLine("File not found: " + ex.Message);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("An error occurred: " + ex.Message);
		}
	}

	private static bool HandleFileNotFoundException(FileNotFoundException ex)
	{
		ExceptionLogger.LogException(ex);
		return true; // Return true to suppress the exception
	}

	public static string GetTLDfromURL(string url) => url[(url.LastIndexOf(".") + 1)..];
}