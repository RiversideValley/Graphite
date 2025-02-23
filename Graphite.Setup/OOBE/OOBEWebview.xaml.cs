using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.UI;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBEWebview : Page
	{
		public User User { get; private set; }
		private TaskCompletionSource<bool> _initializationComplete;

		public OOBEWebview()
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
				DefaultBackgroundColorTextBox.IsEnabled = enabled;
				DefaultScriptEnabledToggle.IsEnabled = enabled;
				DefaultWebExternalEnabledToggle.IsEnabled = enabled;
				UserAgentTextBox.IsEnabled = enabled;
				AreHostObjectsAllowedToggle.IsEnabled = enabled;
				IsBuiltInErrorPageEnabledToggle.IsEnabled = enabled;
				IsScriptEnabledToggle.IsEnabled = enabled;
				IsStatusBarEnabledToggle.IsEnabled = enabled;
				IsWebMessageEnabledToggle.IsEnabled = enabled;
				IsZoomControlEnabledToggle.IsEnabled = enabled;
				DefaultEncodingTextBox.IsEnabled = enabled;
				IsGenericFontFamiliesEnabledToggle.IsEnabled = enabled;
				IsPinchZoomEnabledToggle.IsEnabled = enabled;
				IsSwipeNavigationEnabledToggle.IsEnabled = enabled;
				AdditionalBrowserArgumentsTextBox.IsEnabled = enabled;
				LanguageTextBox.IsEnabled = enabled;
				TargetCompatibleBrowserVersionTextBox.IsEnabled = enabled;
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

				string defaultBackgroundColor = await SettingsManager.GetSettingAsync<string>(User.Username, "DefaultBackgroundColor") ?? "#FFFFFF";
				bool defaultScriptEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "DefaultScriptEnabled");
				bool defaultWebExternalEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "DefaultWebExternalEnabled");
				string userAgent = await SettingsManager.GetSettingAsync<string>(User.Username, "UserAgent") ?? "";
				bool areHostObjectsAllowed = await SettingsManager.GetSettingAsync<bool>(User.Username, "AreHostObjectsAllowed");
				bool isBuiltInErrorPageEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsBuiltInErrorPageEnabled");
				bool isScriptEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsScriptEnabled");
				bool isStatusBarEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsStatusBarEnabled");
				bool isWebMessageEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsWebMessageEnabled");
				bool isZoomControlEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsZoomControlEnabled");
				string defaultEncoding = await SettingsManager.GetSettingAsync<string>(User.Username, "DefaultEncoding") ?? "UTF-8";
				bool isGenericFontFamiliesEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsGenericFontFamiliesEnabled");
				bool isPinchZoomEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsPinchZoomEnabled");
				bool isSwipeNavigationEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "IsSwipeNavigationEnabled");
				string additionalBrowserArguments = await SettingsManager.GetSettingAsync<string>(User.Username, "AdditionalBrowserArguments") ?? "";
				string language = await SettingsManager.GetSettingAsync<string>(User.Username, "Language") ?? "en-US";
				string targetCompatibleBrowserVersion = await SettingsManager.GetSettingAsync<string>(User.Username, "TargetCompatibleBrowserVersion") ?? "";

				DispatcherQueue.TryEnqueue(() =>
				{
					DefaultBackgroundColorTextBox.Text = defaultBackgroundColor;
					DefaultScriptEnabledToggle.IsOn = defaultScriptEnabled;
					DefaultWebExternalEnabledToggle.IsOn = defaultWebExternalEnabled;
					UserAgentTextBox.Text = userAgent;
					AreHostObjectsAllowedToggle.IsOn = areHostObjectsAllowed;
					IsBuiltInErrorPageEnabledToggle.IsOn = isBuiltInErrorPageEnabled;
					IsScriptEnabledToggle.IsOn = isScriptEnabled;
					IsStatusBarEnabledToggle.IsOn = isStatusBarEnabled;
					IsWebMessageEnabledToggle.IsOn = isWebMessageEnabled;
					IsZoomControlEnabledToggle.IsOn = isZoomControlEnabled;
					DefaultEncodingTextBox.Text = defaultEncoding;
					IsGenericFontFamiliesEnabledToggle.IsOn = isGenericFontFamiliesEnabled;
					IsPinchZoomEnabledToggle.IsOn = isPinchZoomEnabled;
					IsSwipeNavigationEnabledToggle.IsOn = isSwipeNavigationEnabled;
					AdditionalBrowserArgumentsTextBox.Text = additionalBrowserArguments;
					LanguageTextBox.Text = language;
					TargetCompatibleBrowserVersionTextBox.Text = targetCompatibleBrowserVersion;

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

				await SettingsManager.UpdateSettingAsync(User.Username, "DefaultBackgroundColor", DefaultBackgroundColorTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "DefaultScriptEnabled", DefaultScriptEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "DefaultWebExternalEnabled", DefaultWebExternalEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "UserAgent", UserAgentTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "AreHostObjectsAllowed", AreHostObjectsAllowedToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsBuiltInErrorPageEnabled", IsBuiltInErrorPageEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsScriptEnabled", IsScriptEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsStatusBarEnabled", IsStatusBarEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsWebMessageEnabled", IsWebMessageEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsZoomControlEnabled", IsZoomControlEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "DefaultEncoding", DefaultEncodingTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsGenericFontFamiliesEnabled", IsGenericFontFamiliesEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsPinchZoomEnabled", IsPinchZoomEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "IsSwipeNavigationEnabled", IsSwipeNavigationEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "AdditionalBrowserArguments", AdditionalBrowserArgumentsTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "Language", LanguageTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "TargetCompatibleBrowserVersion", TargetCompatibleBrowserVersionTextBox.Text);

				// Navigate to the main application or a completion page
				Frame.Navigate(typeof(OOBEFinish), User);
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
					Title = "WebView2 Settings Help",
					Content = "This page allows you to configure WebView2 settings:\n\n" +
							  "• Default Background Color: Set the background color for WebView2\n" +
							  "• Default Script Enabled: Enable or disable JavaScript by default\n" +
							  "• Default Web External Enabled: Allow or block external web content\n" +
							  "• User Agent: Set a custom user agent string\n" +
							  "• Are Host Objects Allowed: Allow or block host objects in web content\n" +
							  "• Is Built In Error Page Enabled: Show or hide built-in error pages\n" +
							  "• Is Script Enabled: Enable or disable JavaScript\n" +
							  "• Is Status Bar Enabled: Show or hide the status bar\n" +
							  "• Is Web Message Enabled: Enable or disable web messaging\n" +
							  "• Is Zoom Control Enabled: Allow or block zoom controls\n" +
							  "• Default Encoding: Set the default text encoding\n" +
							  "• Is Generic Font Families Enabled: Use generic font families\n" +
							  "• Is Pinch Zoom Enabled: Enable or disable pinch-to-zoom\n" +
							  "• Is Swipe Navigation Enabled: Enable or disable swipe navigation\n" +
							  "• Additional Browser Arguments: Set additional command-line arguments\n" +
							  "• Language: Set the preferred language for web content\n" +
							  "• Target Compatible Browser Version: Set the target browser version\n\n" +
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