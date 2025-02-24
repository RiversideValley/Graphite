using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using WinRT.Interop;
using System.Threading.Tasks;
using Graphite.UserSys;
using System.IO;
using Graphite.Controls.FileDialogs;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class DownloadsSettings : Page
	{
		private string currentUsername;

		public DownloadsSettings()
		{
			this.InitializeComponent();
			InitializeSettings();
		}

		private async void InitializeSettings()
		{
			currentUsername = UserManager.GetCurrentUsername();

			// Initialize download path
			string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
			string downloadPath = await SettingsManager.GetSettingAsync<string>(currentUsername, "DefaultDownloadPath", defaultPath);
			DownloadPathTextBox.Text = downloadPath;

			// Initialize ask before downloading toggle
			bool askBeforeDownloading = await SettingsManager.GetSettingAsync<bool>(currentUsername, "AskBeforeDownloading", true);
			AskBeforeDownloadToggle.IsOn = askBeforeDownloading;

			// Initialize open downloads folder toggle
			bool openDownloadsFolder = await SettingsManager.GetSettingAsync<bool>(currentUsername, "OpenDownloadsFolder", true);
			OpenFolderAfterDownloadToggle.IsOn = openDownloadsFolder;
		}

		private async void ChangeDownloadLocation_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new LocationSelectionDialog();
			dialog.Activate();

			var selectedFolder = await dialog.GetSelectedFolderAsync();

			if (selectedFolder != null)
			{
				string newPath = selectedFolder.Path;
				await SettingsManager.UpdateSettingAsync(currentUsername, "DefaultDownloadPath", newPath);
				DownloadPathTextBox.Text = newPath;
				await UpdateWebViewDownloadFolder(newPath);
			}
		}

		private async void AskBeforeDownload_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "AskBeforeDownloading", AskBeforeDownloadToggle.IsOn);
		}

		private async void OpenFolderAfterDownload_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "OpenDownloadsFolder", OpenFolderAfterDownloadToggle.IsOn);
		}

		private async void ClearDownloadHistory_Click(object sender, RoutedEventArgs e)
		{
			ContentDialog confirmDialog = new ContentDialog
			{
				Title = "Confirm",
				Content = "Are you sure you want to clear the download history?",
				PrimaryButtonText = "Yes",
				CloseButtonText = "No",
				XamlRoot = this.XamlRoot
			};

			ContentDialogResult result = await confirmDialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				await ClearDownloadHistoryAsync();
			}
		}

		private async Task UpdateWebViewDownloadFolder(string path)
		{
			// Implement WebView download folder update logic here
			// This will depend on how your WebView is configured
			// For example:
			// await webView.CoreWebView2.Profile.SetDefaultDownloadFolderPathAsync(path);
		}

		private async Task ClearDownloadHistoryAsync()
		{
			try
			{
				// Implement download history clearing logic here
				// This might involve clearing a database or local storage
				// For example:
				// await DownloadManager.ClearHistoryAsync();

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
}