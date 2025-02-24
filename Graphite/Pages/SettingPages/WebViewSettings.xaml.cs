using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class WebViewSettings : Page
	{
		public WebViewSettings()
		{
			this.InitializeComponent();
		}

		
		private void StatusTog_Toggled(object sender, RoutedEventArgs e)
		{
			// Apply the setting to WebView2
		}

		private void BrowserKeys_Toggled(object sender, RoutedEventArgs e)
		{
			// Apply the setting to WebView2
		}

		private void BrowserScripts_Toggled(object sender, RoutedEventArgs e)
		{
			// Apply the setting to WebView2
		}

		private void PipModeTg_Toggled(object sender, RoutedEventArgs e)
		{
			// Apply the setting to WebView2
		}

		private void Agent_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Apply the setting to WebView2
		}

		private void PreventionLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			
		}

		private void UpdateTrackingPreventionInfo()
		{
			
		}

		private async void ClearCookies_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private async void ClearCache_Click(object sender, RoutedEventArgs e)
		{
		}

		private void AdBlocker_Toggled(object sender, RoutedEventArgs e)
		{
			// Apply the setting to WebView2
		}

		private void ResourceSaver_Toggled(object sender, RoutedEventArgs e)
		{
			// Apply the setting to WebView2
		}
	}
}