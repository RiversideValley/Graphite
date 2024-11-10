using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetupAlgemeen : Page
    {
        public SetupAlgemeen()
        {
            this.InitializeComponent();
        }

        private void SearchengineSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                string selection = e.AddedItems[0].ToString();
                string url;

                switch (selection)
                {
                    case "Ask":
                        url = "https://www.ask.com/web?q=";
                        break;
                    case "Baidu":
                        url = "https://www.baidu.com/s?ie=utf-8&f=8&rsv_bp=1&rsv_idx=1&tn=baidu&wd=";
                        break;
                    case "Bing":
                        url = "https://www.bing.com?q=";
                        break;
                    case "DuckDuckGo":
                        url = "https://www.duckduckgo.com?q=";
                        break;
                    case "Ecosia":
                        url = "https://www.ecosia.org/search?q=";
                        break;
                    case "Google":
                        url = "https://www.google.com/search?q=";
                        break;
                    case "Startpage":
                        url = "https://www.startpage.com/search?q=";
                        break;
                    case "Qwant":
                        url = "https://www.qwant.com/?q=";
                        break;
                    case "Qwant Lite":
                        url = "https://lite.qwant.com/?q=";
                        break;
                    case "Yahoo!":
                        url = "https://search.yahoo.com/search?p=";
                        break;
                    case "Presearch":
                        url = "https://presearch.com/search?q=";
                        break;
                    case "Swisscows":
                        url = "https://swisscows.com/web?query=";
                        break;
                    case "Dogpile":
                        url = "https://www.dogpile.com/serp?q=";
                        break;
                    case "Webcrawler":
                        url = "https://www.webcrawler.com/serp?q=";
                        break;
                    case "You":
                        url = "https://you.com/search?q=";
                        break;
                    case "Excite":
                        url = "https://results.excite.com/serp?q=";
                        break;
                    case "Lycos":
                        url = "https://search20.lycos.com/web/?q=";
                        break;
                    case "Metacrawler":
                        url = "https://www.metacrawler.com/serp?q=";
                        break;
                    case "Mojeek":
                        url = "https://www.mojeek.com/search?q=";
                        break;
                    case "BraveSearch":
                        url = "https://search.brave.com/search?q=";
                        break;
                    // Add other cases for different search engines.
                    default:
                        // Handle the case when selection doesn't match any of the predefined options.
                        url = "https://www.google.com/search?q=";
                        break;
                }

                if (!string.IsNullOrEmpty(url))
                {
                    AppService.AppSettings.EngineFriendlyName = selection;
                    AppService.AppSettings.SearchUrl = url;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

        }

        private void ToggleSetting(string settingName, bool value)
        {
            // Set the specified setting
            switch (settingName)
            {
                case "Downloads":
                    AppService.AppSettings.Downloads = value;
                    break;
                case "FavoritesL":
                    AppService.AppSettings.FavoritesL = value;
                    break;
                case "Favorites":
                    AppService.AppSettings.Favorites = value;
                    break;
                case "Historybtn":
                    AppService.AppSettings.Historybtn = value;
                    break;
                case "QrCode":
                    AppService.AppSettings.QrCode = value;
                    break;
                case "ToolIcon":
                    AppService.AppSettings.ToolIcon = value;
                    break;
                case "DarkIcon":
                    AppService.AppSettings.DarkIcon = value;
                    break;
                case "Translate":
                    AppService.AppSettings.Translate = value;
                    break;
                case "ReadButton":
                    AppService.AppSettings.ReadButton = value;
                    break;
                case "AdblockBtn":
                    AppService.AppSettings.AdblockBtn = value;
                    break;
                case "OpenTabHandel":
                    AppService.AppSettings.OpenTabHandel = value;
                    break;
                // Add other cases for different settings.
                default:
                    throw new ArgumentException("Invalid setting name");
            }
        }

        private void Dwbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("Downloads", (sender as ToggleSwitch).IsOn);

        private void Frbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("FavoritesL", (sender as ToggleSwitch).IsOn);

        private void FlAd_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("Favorites", (sender as ToggleSwitch).IsOn);

        private void Hsbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("Historybtn", (sender as ToggleSwitch).IsOn);

        private void Qrbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("QrCode", (sender as ToggleSwitch).IsOn);

        private void Tlbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("ToolIcon", (sender as ToggleSwitch).IsOn);

        private void Drbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("DarkIcon", (sender as ToggleSwitch).IsOn);

        private void Trbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("Translate", (sender as ToggleSwitch).IsOn);

        private void Read_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("ReadButton", (sender as ToggleSwitch).IsOn);

        private void Adbl_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("AdblockBtn", (sender as ToggleSwitch).IsOn);

        private void OpenNew_Toggled(object sender, RoutedEventArgs e) => ToggleSetting("OpenTabHandel", (sender as ToggleSwitch).IsOn);


        private void SetupAlgemeenBtn_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SetupPrivacy));
        }
    }
}
