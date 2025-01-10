using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.Storage;

namespace Riverside.Graphite.Runtime.Helpers
{
	public class UrlValidater
	{
		public static async Task<bool> IsUrlReachable(Uri url)
		{
			using HttpClient httpClient = new();
			httpClient.BaseAddress = url;
			try
			{
				HttpResponseMessage response = await httpClient.GetAsync("");
				if (response.IsSuccessStatusCode)
				{
					Console.WriteLine($"URL is reachable: {url}");
					return true;
				}
				else
				{
					Console.WriteLine($"URL returned status code {response.StatusCode}: {url}");
					return false;
				}
			}
			catch (HttpRequestException e)
			{
				Console.WriteLine($"Request error: {e.Message}");
				return false;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Unexpected error: {e.Message}");
				return false;
			}
		}

		public static Uri GetValidateUrl(string queryText)
		{
			if (string.IsNullOrWhiteSpace(queryText))
				return null;

			// If it already starts with a protocol, try to create a URI directly
			if (queryText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				queryText.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				if (Uri.TryCreate(queryText, UriKind.Absolute, out Uri uri))
				{
					return uri;
				}
			}

			// Check if it's an IP address with port
			if (IsValidIpWithPort(queryText))
			{
				// Try HTTPS first for IP addresses with ports
				try
				{
					var httpsUri = new Uri($"https://{queryText}");
					return httpsUri;
				}
				catch
				{
					try
					{
						// Fallback to HTTP if HTTPS fails
						return new Uri($"http://{queryText}");
					}
					catch
					{
						return null;
					}
				}
			}

			// Then check if it's a valid domain without protocol
			if (IsValidDomain(queryText))
			{
				return new Uri($"http://{queryText}");
			}

			// Finally check if it's a full URL with a different protocol
			if (IsValidFullUrl(queryText))
			{
				return new Uri(queryText);
			}

			return null;
		}

		private static bool IsValidIpWithPort(string input)
		{
			// Updated regex to better handle IPv4 addresses with ports
			var ipPortRegex = new Regex(@"^(\d{1,3}\.){3}\d{1,3}(:\d+)?$");
			if (ipPortRegex.IsMatch(input))
			{
				string[] parts = input.Split(':');
				string ipPart = parts[0];

				// Validate each octet of the IP address
				string[] octets = ipPart.Split('.');
				if (octets.Length == 4 && octets.All(octet =>
					byte.TryParse(octet, out byte value)))
				{
					// If there's a port, validate it
					if (parts.Length == 2)
					{
						return int.TryParse(parts[1], out int port) &&
							   port >= 1 &&
							   port <= 65535;
					}
					return true; // Valid IP without port
				}
			}
			return false;
		}

		private static bool IsValidDomain(string input)
		{
			// Match domain names like youtube.com, www.youtube.com, etc.
			var domainRegex = new Regex(@"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$");
			return domainRegex.IsMatch(input);
		}

		private static bool IsValidFullUrl(string input)
		{
			// Match URLs with various protocols
			var urlRegex = new Regex(@"^(http|https|ms-appx|ms-appx-web|ftp|firebrowseruser|firebrowserwinui|firebrowserincog|firebrowser)://", RegexOptions.IgnoreCase);
			return urlRegex.IsMatch(input) && Uri.TryCreate(input, UriKind.Absolute, out _);
		}

		private static async Task<(string Name, string DisplayName)[]> GetProtocolsFromManifest()
		{
			StorageFolder packageFolder = Package.Current.InstalledLocation;
			StorageFile manifestFile = await packageFolder.GetFileAsync("AppxManifest.xml");
			string manifestContent = await FileIO.ReadTextAsync(manifestFile);
			XmlDocument xmlDoc = new();
			xmlDoc.LoadXml(manifestContent);

			XmlNodeList protocolNodes = xmlDoc.SelectNodesNS("//uap:Protocol", "xmlns:uap=\"http://schemas.microsoft.com/appx/manifest/uap/windows10\"");
			(string Name, string DisplayName)[] protocols = protocolNodes.Select(node => (
				Name: node.Attributes.GetNamedItem("Name").InnerText,
				DisplayName: node.SelectSingleNodeNS("uap:DisplayName", "xmlns:uap=\"http://schemas.microsoft.com/appx/manifest/uap/windows10\"").InnerText
			)).ToArray();

			return protocols;
		}
	}
}

