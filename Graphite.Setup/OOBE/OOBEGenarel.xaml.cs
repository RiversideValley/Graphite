using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBEGeneral : Page
	{
		public User User { get; private set; }
		private TaskCompletionSource<bool> _initializationComplete;

		public OOBEGeneral()
		{
			this.InitializeComponent();
			_initializationComplete = new TaskCompletionSource<bool>();
			SetControlsEnabled(false);
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.Parameter is User user)
			{
				User = user;
				await InitializeSettingsAsync();
			}
			else
			{
				await ShowErrorAndGoBackAsync();
			}
		}

		private void SetControlsEnabled(bool enabled)
		{
			if (DispatcherQueue.HasThreadAccess)
			{
				StartupBehaviorComboBox.IsEnabled = enabled;
				HomePageTextBox.IsEnabled = enabled;
				ThemeComboBox.IsEnabled = enabled;
				ShowHomeButtonToggle.IsEnabled = enabled;
				ShowBookmarksBarToggle.IsEnabled = enabled;
				SearchEngineComboBox.IsEnabled = enabled;
				CustomSearchEngineTextBox.IsEnabled = enabled;
				DownloadLocationTextBox.IsEnabled = enabled;
				BrowseButton.IsEnabled = enabled;
				AskBeforeDownloadToggle.IsEnabled = enabled;
				NextStep.IsEnabled = enabled;
			}
			else
			{
				DispatcherQueue.TryEnqueue(() => SetControlsEnabled(enabled));
			}
		}

		private async Task InitializeSettingsAsync()
		{
			try
			{
				await SettingsManager.InitializeUserSettingsAsync(User.Username);

				string startupBehavior = await SettingsManager.GetSettingAsync<string>(User.Username, "StartupBehavior") ?? "Open the New Tab page";
				string homePage = await SettingsManager.GetSettingAsync<string>(User.Username, "HomePage") ?? "https://www.example.com";
				string theme = await SettingsManager.GetSettingAsync<string>(User.Username, "Theme") ?? "System default";
				bool showHomeButton = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowHomeButton");
				bool showBookmarksBar = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowBookmarksBar");
				string searchEngine = await SettingsManager.GetSettingAsync<string>(User.Username, "SearchEngine") ?? "Google";
				string customSearchEngineUrl = await SettingsManager.GetSettingAsync<string>(User.Username, "CustomSearchEngineUrl") ?? "";
				string downloadLocation = await SettingsManager.GetSettingAsync<string>(User.Username, "DownloadLocation") ?? "";
				bool askBeforeDownload = await SettingsManager.GetSettingAsync<bool>(User.Username, "AskBeforeDownload");

				DispatcherQueue.TryEnqueue(() =>
				{
					StartupBehaviorComboBox.SelectedItem = StartupBehaviorComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == startupBehavior);
					HomePageTextBox.Text = homePage;
					ThemeComboBox.SelectedItem = ThemeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == theme);
					ShowHomeButtonToggle.IsOn = showHomeButton;
					ShowBookmarksBarToggle.IsOn = showBookmarksBar;
					SearchEngineComboBox.SelectedItem = SearchEngineComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == searchEngine);
					CustomSearchEngineTextBox.Text = customSearchEngineUrl;
					DownloadLocationTextBox.Text = downloadLocation;
					AskBeforeDownloadToggle.IsOn = askBeforeDownload;

					SetControlsEnabled(true);
				});

				_initializationComplete.TrySetResult(true);
			}
			catch (Exception ex)
			{
				_initializationComplete.TrySetException(ex);
				await ShowErrorDialogAsync($"Error initializing settings: {ex.Message}");
				await ShowErrorAndGoBackAsync();
			}
		}

		private async void NextStep_Click(object sender, RoutedEventArgs e)
		{
			if (!_initializationComplete.Task.IsCompleted)
			{
				await ShowErrorDialogAsync("Settings not initialized. Please try again.");
				return;
			}

			try
			{
				SetControlsEnabled(false);

				await SettingsManager.UpdateSettingAsync(User.Username, "StartupBehavior", (StartupBehaviorComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
				await SettingsManager.UpdateSettingAsync(User.Username, "HomePage", HomePageTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "Theme", (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowHomeButton", ShowHomeButtonToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowBookmarksBar", ShowBookmarksBarToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "SearchEngine", (SearchEngineComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
				await SettingsManager.UpdateSettingAsync(User.Username, "CustomSearchEngineUrl", CustomSearchEngineTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "DownloadLocation", DownloadLocationTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "AskBeforeDownload", AskBeforeDownloadToggle.IsOn);

				// Navigate to the next OOBE page or finish the setup
				Frame.Navigate(typeof(OOBEWebview), User);
			}
			catch (Exception ex)
			{
				await ShowErrorDialogAsync($"An error occurred while saving settings: {ex.Message}");
			}
			finally
			{
				SetControlsEnabled(true);
			}
		}

		private async void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			var folderPicker = new FolderPicker();
			folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
			folderPicker.FileTypeFilter.Add("*");

			// Initialize the folder picker with the window handle
			WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WinRT.Interop.WindowNative.GetWindowHandle(this));

			StorageFolder folder = await folderPicker.PickSingleFolderAsync();
			if (folder != null)
			{
				DownloadLocationTextBox.Text = folder.Path;
			}
		}

		private async void HelpButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ContentDialog helpDialog = new ContentDialog
				{
					Title = "General Settings Help",
					Content = "This page allows you to configure general browser settings:\n\n" +
							  "• Startup: Choose what happens when you start the browser\n" +
							  "• Home Page: Set your default home page\n" +
							  "• Theme: Choose between light, dark, or system default\n" +
							  "• Show Home Button: Toggle visibility of the home button\n" +
							  "• Show Bookmarks Bar: Toggle visibility of the bookmarks bar\n" +
							  "• Search Engine: Set your default search engine\n" +
							  "• Custom Search Engine: Set a custom search engine URL\n" +
							  "• Download Location: Choose where files are saved\n" +
							  "• Ask Before Download: Choose whether to confirm each download\n\n" +
							  "If you need further assistance, please contact support.",
					CloseButtonText = "OK",
					XamlRoot = this.XamlRoot,
					DefaultButton = ContentDialogButton.Close
				};

				await helpDialog.ShowAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error showing help dialog: {ex.Message}");
			}
		}

		private async Task ShowErrorDialogAsync(string message)
		{
			try
			{
				var errorDialog = new ContentDialog
				{
					Title = "Error",
					Content = message,
					CloseButtonText = "OK",
					XamlRoot = this.XamlRoot
				};

				await errorDialog.ShowAsync();
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine($"Error showing dialog: {message}");
			}
		}

		private async Task ShowErrorAndGoBackAsync()
		{
			await ShowErrorDialogAsync("An error occurred. Please try again.");
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
		}
	}
}