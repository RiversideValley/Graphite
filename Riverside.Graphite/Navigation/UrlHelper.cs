using System;
using System.Linq;

namespace Riverside.Graphite.Navigation;

public class UrlHelper
{
	public static string GetInputType(string input)
	{
		return IsURL(input) ? "url" : IsURLWithoutProtocol(input) ? "urlNOProtocol" : "searchquery";
	}

	private static bool IsURL(string input)
	{
		return input.Contains(':') && input.Contains('.') && Uri.TryCreate(input, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}

	private static bool IsURLWithoutProtocol(string input)
	{
		return input.Contains('.') && TLD.KnownDomains?.Any(domain => TLD.GetTLDfromURL(input).Contains(domain)) == true;
	}
}