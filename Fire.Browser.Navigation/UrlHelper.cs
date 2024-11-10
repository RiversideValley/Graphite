using System;
using System.Linq;

namespace Fire.Browser.Navigation;
public class UrlHelper
{
	public static string GetInputType(string input) => IsURL(input) ? "url" : IsURLWithoutProtocol(input) ? "urlNOProtocol" : "searchquery";

	private static bool IsURL(string input) => input.Contains(":") && input.Contains(".") && Uri.TryCreate(input, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

	private static bool IsURLWithoutProtocol(string input) => input.Contains(".") && TLD.KnownDomains?.Any(domain => TLD.GetTLDfromURL(input).Contains(domain)) == true;
}