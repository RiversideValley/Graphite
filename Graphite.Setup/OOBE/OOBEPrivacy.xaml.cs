using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBEPrivacy : Page
	{
		public User User { get; private set; }
		private TaskCompletionSource<bool> _initializationComplete;

		public OOBEPrivacy()
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
				DoNotTrackToggle.IsEnabled = enabled;
				BlockThirdPartyCookiesToggle.IsEnabled = enabled;
				ClearBrowsingDataOnExitToggle.IsEnabled = enabled;
				BlockAdsToggle.IsEnabled = enabled;
				BlockTrackersToggle.IsEnabled = enabled;
				BlockMaliciousContentToggle.IsEnabled = enabled;
				EncryptPasswordsToggle.IsEnabled = enabled;
				EncryptHistoryToggle.IsEnabled = enabled;
				EncryptBookmarksToggle.IsEnabled = enabled;
				BlockLocationAccessToggle.IsEnabled = enabled;
				BlockCameraAccessToggle.IsEnabled = enabled;
				BlockMicrophoneAccessToggle.IsEnabled = enabled;
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

				bool doNotTrack = await SettingsManager.GetSettingAsync<bool>(User.Username, "DoNotTrack");
				bool blockThirdPartyCookies = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockThirdPartyCookies");
				bool clearBrowsingDataOnExit = await SettingsManager.GetSettingAsync<bool>(User.Username, "ClearBrowsingDataOnExit");
				bool blockAds = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockAds");
				bool blockTrackers = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockTrackers");
				bool blockMaliciousContent = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockMaliciousContent");
				bool encryptPasswords = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptPasswords");
				bool encryptHistory = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptHistory");
				bool encryptBookmarks = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptBookmarks");
				bool blockLocationAccess = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockLocationAccess");
				bool blockCameraAccess = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockCameraAccess");
				bool blockMicrophoneAccess = await SettingsManager.GetSettingAsync<bool>(User.Username, "BlockMicrophoneAccess");

				DispatcherQueue.TryEnqueue(() =>
				{
					DoNotTrackToggle.IsOn = doNotTrack;
					BlockThirdPartyCookiesToggle.IsOn = blockThirdPartyCookies;
					ClearBrowsingDataOnExitToggle.IsOn = clearBrowsingDataOnExit;
					BlockAdsToggle.IsOn = blockAds;
					BlockTrackersToggle.IsOn = blockTrackers;
					BlockMaliciousContentToggle.IsOn = blockMaliciousContent;
					EncryptPasswordsToggle.IsOn = encryptPasswords;
					EncryptHistoryToggle.IsOn = encryptHistory;
					EncryptBookmarksToggle.IsOn = encryptBookmarks;
					BlockLocationAccessToggle.IsOn = blockLocationAccess;
					BlockCameraAccessToggle.IsOn = blockCameraAccess;
					BlockMicrophoneAccessToggle.IsOn = blockMicrophoneAccess;

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

				await SettingsManager.UpdateSettingAsync(User.Username, "DoNotTrack", DoNotTrackToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockThirdPartyCookies", BlockThirdPartyCookiesToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "ClearBrowsingDataOnExit", ClearBrowsingDataOnExitToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockAds", BlockAdsToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockTrackers", BlockTrackersToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockMaliciousContent", BlockMaliciousContentToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "EncryptPasswords", EncryptPasswordsToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "EncryptHistory", EncryptHistoryToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "EncryptBookmarks", EncryptBookmarksToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockLocationAccess", BlockLocationAccessToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockCameraAccess", BlockCameraAccessToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BlockMicrophoneAccess", BlockMicrophoneAccessToggle.IsOn);

				// Navigate to the main application or a completion page
				Frame.Navigate(typeof(OOBENewtab), User);
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
					Title = "Privacy Settings Help",
					Content = "This page allows you to configure various privacy settings for your browsing experience:\n\n" +
							  "• Do Not Track: Sends a request to websites not to track your browsing\n" +
							  "• Block Third-Party Cookies: Prevents sites from setting cookies from other domains\n" +
							  "• Clear Browsing Data on Exit: Automatically clears your browsing history when you close the browser\n" +
							  "• Block Ads: Attempts to block advertisements on websites\n" +
							  "• Block Trackers: Prevents tracking scripts from running\n" +
							  "• Block Malicious Content: Helps protect against known malicious websites\n" +
							  "• Encrypt Passwords/History/Bookmarks: Adds an extra layer of security to your saved data\n" +
							  "• Block Location/Camera/Microphone Access: Prevents websites from accessing these features by default\n\n" +
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