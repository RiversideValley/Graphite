using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.Storage;


namespace FireBrowserWinUi3Core.Helpers
{

    public class UrlValidater
    {
        public static async Task<bool> IsUrlReachable(Uri url)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = url;
                try
                {
                    var response = await httpClient.GetAsync("");
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
        }
        public static Uri? GetValidateUrl(string queryText)
        {

            Uri uriOut = null;
            Regex regex = new Regex(@"^(http|https|ms-appx|ms-appx-web|ftp|firebrowseruser|firebrowserwinui|firebrowserincog|firebrowser)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$", RegexOptions.IgnoreCase);

            //Regex($@"^(http|https|ms-appx|ms-appx-web|ftp|firebrowseruser|firebrowserwinui|firebrowserincog)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$", RegexOptions.IgnoreCase);

            if (IsUrlValid(queryText, regex, out Uri sendUri))
            {
                uriOut = sendUri != null ? new UriBuilder(sendUri.ToString()).Uri : new Uri(queryText);
            }
            else
            {
                return null;
            }

            return uriOut;
        }

        private static bool IsUrlValid(string url, Regex regex, out Uri uri)
        {
            uri = null;
            if (regex.IsMatch(url))
            {
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
                {
                    return true;
                }
            }
            return false;
        }

        static async Task<(string Name, string DisplayName)[]> GetProtocolsFromManifest()
        {
            var packageFolder = Package.Current.InstalledLocation;
            var manifestFile = await packageFolder.GetFileAsync("AppxManifest.xml");
            var manifestContent = await FileIO.ReadTextAsync(manifestFile);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(manifestContent);

            var protocolNodes = xmlDoc.SelectNodesNS("//uap:Protocol", "xmlns:uap=\"http://schemas.microsoft.com/appx/manifest/uap/windows10\"");
            var protocols = protocolNodes.Select(node => (
                Name: node.Attributes.GetNamedItem("Name").InnerText,
                DisplayName: node.SelectSingleNodeNS("uap:DisplayName", "xmlns:uap=\"http://schemas.microsoft.com/appx/manifest/uap/windows10\"").InnerText
            )).ToArray();

            return protocols;
        }
    }
}


