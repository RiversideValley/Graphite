using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBENewtab : Page
	{
		public User User { get; private set; }
		private TaskCompletionSource<bool> _initializationComplete;

		public OOBENewtab()
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
				NewTabPageTypeComboBox.IsEnabled = enabled;
				CustomUrlTextBox.IsEnabled = enabled;
				ShowSearchBarToggle.IsEnabled = enabled;
				ShowQuickLinksToggle.IsEnabled = enabled;
				QuickLinksCountBox.IsEnabled = enabled;
				UseMostVisitedSitesToggle.IsEnabled = enabled;
				BackgroundTypeComboBox.IsEnabled = enabled;
				BackgroundColorTextBox.IsEnabled = enabled;
				CustomImageUrlTextBox.IsEnabled = enabled;
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

				string newTabPageType = await SettingsManager.GetSettingAsync<string>(User.Username, "NewTabPageType") ?? "Blank Page";
				string customUrl = await SettingsManager.GetSettingAsync<string>(User.Username, "CustomUrl") ?? "";
				bool showSearchBar = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowSearchBar");
				bool showQuickLinks = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowQuickLinks");
				int quickLinksCount = await SettingsManager.GetSettingAsync<int>(User.Username, "QuickLinksCount");
				bool useMostVisitedSites = await SettingsManager.GetSettingAsync<bool>(User.Username, "UseMostVisitedSites");
				string backgroundType = await SettingsManager.GetSettingAsync<string>(User.Username, "BackgroundType") ?? "Solid Color";
				string backgroundColor = await SettingsManager.GetSettingAsync<string>(User.Username, "BackgroundColor") ?? "#FFFFFF";
				string customImageUrl = await SettingsManager.GetSettingAsync<string>(User.Username, "CustomImageUrl") ?? "";

				DispatcherQueue.TryEnqueue(() =>
				{
					NewTabPageTypeComboBox.SelectedItem = NewTabPageTypeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == newTabPageType);
					CustomUrlTextBox.Text = customUrl;
					ShowSearchBarToggle.IsOn = showSearchBar;
					ShowQuickLinksToggle.IsOn = showQuickLinks;
					QuickLinksCountBox.Value = quickLinksCount;
					UseMostVisitedSitesToggle.IsOn = useMostVisitedSites;
					BackgroundTypeComboBox.SelectedItem = BackgroundTypeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == backgroundType);
					BackgroundColorTextBox.Text = backgroundColor;
					CustomImageUrlTextBox.Text = customImageUrl;

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

				await SettingsManager.UpdateSettingAsync(User.Username, "NewTabPageType", (NewTabPageTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
				await SettingsManager.UpdateSettingAsync(User.Username, "CustomUrl", CustomUrlTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowSearchBar", ShowSearchBarToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowQuickLinks", ShowQuickLinksToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "QuickLinksCount", (int)QuickLinksCountBox.Value);
				await SettingsManager.UpdateSettingAsync(User.Username, "UseMostVisitedSites", UseMostVisitedSitesToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BackgroundType", (BackgroundTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
				await SettingsManager.UpdateSettingAsync(User.Username, "BackgroundColor", BackgroundColorTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "CustomImageUrl", CustomImageUrlTextBox.Text);

				// Navigate to the main application or a completion page
				Frame.Navigate(typeof(OOBEGeneral), User);
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

		private async void HelpButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ContentDialog helpDialog = new ContentDialog
				{
					Title = "New Tab Settings Help",
					Content = "This page allows you to configure your new tab experience:\n\n" +
							  "• New Tab Page Type: Choose between a blank page, quick links, or a custom URL\n" +
							  "• Custom URL: Set a specific website to open on new tabs\n" +
							  "• Show Search Bar: Display a search bar on new tabs\n" +
							  "• Show Quick Links: Display frequently visited or custom sites\n" +
							  "• Number of Quick Links: Set how many quick links to show\n" +
							  "• Use Most Visited Sites: Automatically populate quick links with your most visited sites\n" +
							  "• Background Type: Choose between a solid color, custom image, or daily changing image\n" +
							  "• Background Color: Set a custom color for the new tab background\n" +
							  "• Custom Image URL: Set a specific image as the new tab background\n\n" +
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