using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Services;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Riverside.Graphite
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SetupAlgemeen : Page
	{
		public SetupAlgemeen()
		{
			InitializeComponent();
		}

		private void SearchengineSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

			try
			{
				string selection = e.AddedItems[0].ToString();
				string url = selection switch
				{
					"Ask" => "https://www.ask.com/web?q=",
					"Baidu" => "https://www.baidu.com/s?ie=utf-8&f=8&rsv_bp=1&rsv_idx=1&tn=baidu&wd=",
					"Bing" => "https://www.bing.com?q=",
					"DuckDuckGo" => "https://www.duckduckgo.com?q=",
					"Ecosia" => "https://www.ecosia.org/search?q=",
					"Google" => "https://www.google.com/search?q=",
					"Startpage" => "https://www.startpage.com/search?q=",
					"Qwant" => "https://www.qwant.com/?q=",
					"Qwant Lite" => "https://lite.qwant.com/?q=",
					"Yahoo!" => "https://search.yahoo.com/search?p=",
					"Presearch" => "https://presearch.com/search?q=",
					"Swisscows" => "https://swisscows.com/web?query=",
					"Dogpile" => "https://www.dogpile.com/serp?q=",
					"Webcrawler" => "https://www.webcrawler.com/serp?q=",
					"You" => "https://you.com/search?q=",
					"Excite" => "https://results.excite.com/serp?q=",
					"Lycos" => "https://search20.lycos.com/web/?q=",
					"Metacrawler" => "https://www.metacrawler.com/serp?q=",
					"Mojeek" => "https://www.mojeek.com/search?q=",
					"BraveSearch" => "https://search.brave.com/search?q=",
					// Add other cases for different search engines.
					_ => "https://www.google.com/search?q=",// Handle the case when selection doesn't match any of the predefined options.
				};
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

		private void Dwbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("Downloads", (sender as ToggleSwitch).IsOn);
		}

		private void Frbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("FavoritesL", (sender as ToggleSwitch).IsOn);
		}

		private void FlAd_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("Favorites", (sender as ToggleSwitch).IsOn);
		}

		private void Hsbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("Historybtn", (sender as ToggleSwitch).IsOn);
		}

		private void Qrbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("QrCode", (sender as ToggleSwitch).IsOn);
		}

		private void Tlbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("ToolIcon", (sender as ToggleSwitch).IsOn);
		}

		private void Drbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("DarkIcon", (sender as ToggleSwitch).IsOn);
		}

		private void Trbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("Translate", (sender as ToggleSwitch).IsOn);
		}

		private void Read_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("ReadButton", (sender as ToggleSwitch).IsOn);
		}

		private void Adbl_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("AdblockBtn", (sender as ToggleSwitch).IsOn);
		}

		private void OpenNew_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSetting("OpenTabHandel", (sender as ToggleSwitch).IsOn);
		}

		private void SetupAlgemeenBtn_Click(object sender, RoutedEventArgs e)
		{
			_ = Frame.Navigate(typeof(SetupPrivacy));
		}
	}
}
