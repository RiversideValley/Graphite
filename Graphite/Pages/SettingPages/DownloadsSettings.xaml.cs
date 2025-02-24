using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using WinRT.Interop;
using System.Threading.Tasks;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class DownloadsSettings : Page
	{
		private ApplicationDataContainer localSettings;

		public DownloadsSettings()
		{
			this.InitializeComponent();
			
		}

		private void InitializeSettings()
		{
			
		}

		private async void ChangeDownloadLocation_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void AskBeforeDownload_Toggled(object sender, RoutedEventArgs e)
		{
		}

		private void OpenFolderAfterDownload_Toggled(object sender, RoutedEventArgs e)
		{
		}

		private async void ClearDownloadHistory_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private async Task UpdateWebViewDownloadFolder(string path)
		{
			// Implement WebView download folder update logic here
			// This will depend on how your WebView is configured
		}

		private async Task ClearDownloadHistoryAsync()
		{
			try
			{
				// Implement download history clearing logic here
				// This might involve clearing a database or local storage

				ContentDialog successDialog = new ContentDialog
				{
					Title = "Success",
					Content = "Download history has been cleared.",
					CloseButtonText = "OK",
					XamlRoot = this.XamlRoot
				};

				await successDialog.ShowAsync();
			}
			catch (Exception ex)
			{
				ContentDialog errorDialog = new ContentDialog
				{
					Title = "Error",
					Content = $"Failed to clear download history: {ex.Message}",
					CloseButtonText = "OK",
					XamlRoot = this.XamlRoot
				};

				await errorDialog.ShowAsync();
			}
		}
	}

	// Helper class to get the window handle

}