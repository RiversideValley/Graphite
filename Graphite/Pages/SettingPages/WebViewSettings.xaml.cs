using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using Graphite.UserSys;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class WebViewSettings : Page
	{
		private string currentUsername;
		private ObservableCollection<EnvVariable> envVariables;

		public WebViewSettings()
		{
			this.InitializeComponent();
			InitializeSettings();
		}

		private async void InitializeSettings()
		{
			currentUsername = UserManager.GetCurrentUsername();

			StatusTog.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnableStatusBar", true);
			BrowserKeys.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnableBrowserKeys", true);
			BrowserScripts.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnableBrowserScripts", true);
			PipModeTg.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnablePictureInPictureMode", true);
			Agent.Text = await SettingsManager.GetSettingAsync<string>(currentUsername, "UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36 Edg/132.0.0.0");
			PreventionLevel.SelectedIndex = await SettingsManager.GetSettingAsync<int>(currentUsername, "TrackingPreventionLevel", 2);
			AdBlocker.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnableAdBlocker", true);
			ResourceSaver.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnableResourceSaving", true);
			EnableOSIntegration.IsOn = await SettingsManager.GetSettingAsync<bool>(currentUsername, "EnableOperatingSystemIntegration", true);

			UpdateTrackingPreventionInfo();
			await LoadEnvironmentVariables();
		}

		private async Task LoadEnvironmentVariables()
		{
			string envVarsJson = await SettingsManager.GetSettingAsync<string>(currentUsername, $"{currentUsername}_envweb", "[]");
			envVariables = JsonSerializer.Deserialize<ObservableCollection<EnvVariable>>(envVarsJson) ?? new ObservableCollection<EnvVariable>();
			EnvVarList.ItemsSource = envVariables;
		}

		private async Task SaveEnvironmentVariables()
		{
			string envVarsJson = JsonSerializer.Serialize(envVariables);
			await SettingsManager.UpdateSettingAsync(currentUsername, $"{currentUsername}_envweb", envVarsJson);
		}

		private async void StatusTog_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnableStatusBar", StatusTog.IsOn);
			// Apply the setting to WebView2
		}

		private async void BrowserKeys_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnableBrowserKeys", BrowserKeys.IsOn);
			// Apply the setting to WebView2
		}

		private async void BrowserScripts_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnableBrowserScripts", BrowserScripts.IsOn);
			// Apply the setting to WebView2
		}

		private async void PipModeTg_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnablePictureInPictureMode", PipModeTg.IsOn);
			// Apply the setting to WebView2
		}

		private async void Agent_TextChanged(object sender, TextChangedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "UserAgent", Agent.Text);
			// Apply the setting to WebView2
		}

		private async void PreventionLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "TrackingPreventionLevel", PreventionLevel.SelectedIndex);
			UpdateTrackingPreventionInfo();
			// Apply the setting to WebView2
		}

		private void UpdateTrackingPreventionInfo()
		{
			string[] infoTexts = new string[]
			{
				"No tracking prevention. All trackers are allowed.",
				"Basic tracking prevention. Some trackers are blocked.",
				"Balanced tracking prevention. Many trackers are blocked.",
				"Strict tracking prevention. Most trackers are blocked."
			};

			Info.Text = infoTexts[PreventionLevel.SelectedIndex];
		}

		private async void ClearCookies_Click(object sender, RoutedEventArgs e)
		{
			// Implement cookie clearing logic here
			// For example:
			// await webView.CoreWebView2.CookieManager.DeleteAllCookiesAsync();

			ContentDialog dialog = new ContentDialog
			{
				Title = "Cookies Cleared",
				Content = "All cookies have been cleared.",
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot
			};

			await dialog.ShowAsync();
		}

		private async void ClearCache_Click(object sender, RoutedEventArgs e)
		{
			// Implement cache clearing logic here
			// For example:
			// await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();

			ContentDialog dialog = new ContentDialog
			{
				Title = "Cache Cleared",
				Content = "Browser cache has been cleared.",
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot
			};

			await dialog.ShowAsync();
		}

		private async void AdBlocker_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnableAdBlocker", AdBlocker.IsOn);
			// Apply the setting to WebView2
		}

		private async void ResourceSaver_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnableResourceSaving", ResourceSaver.IsOn);
			// Apply the setting to WebView2
		}

		private async void EnableOSIntegration_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "EnableOperatingSystemIntegration", EnableOSIntegration.IsOn);
			// Apply the setting to WebView2
		}

		private async void AddEnvVar_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(EnvVarKey.Text) || string.IsNullOrWhiteSpace(EnvVarValue.Text))
			{
				return;
			}

			if (envVariables.Count >= 10)
			{
				ContentDialog dialog = new ContentDialog
				{
					Title = "Maximum Limit Reached",
					Content = "You can only add up to 10 environment variables.",
					CloseButtonText = "OK",
					XamlRoot = this.XamlRoot
				};
				await dialog.ShowAsync();
				return;
			}

			envVariables.Add(new EnvVariable { Key = EnvVarKey.Text, Value = EnvVarValue.Text });
			EnvVarKey.Text = "";
			EnvVarValue.Text = "";

			await SaveEnvironmentVariables();
		}

		private async void EditEnvVar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is EnvVariable envVar)
			{
				var dialog = new ContentDialog()
				{
					Title = "Edit Environment Variable",
					PrimaryButtonText = "Save",
					CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Primary,
					XamlRoot = this.XamlRoot
				};

				var panel = new StackPanel();
				var keyBox = new TextBox() { Header = "Key", Text = envVar.Key };
				var valueBox = new TextBox() { Header = "Value", Text = envVar.Value };
				panel.Children.Add(keyBox);
				panel.Children.Add(valueBox);
				dialog.Content = panel;

				var result = await dialog.ShowAsync();
				if (result == ContentDialogResult.Primary)
				{
					envVar.Key = keyBox.Text;
					envVar.Value = valueBox.Text;
					EnvVarList.ItemsSource = null;
					EnvVarList.ItemsSource = envVariables;
					await SaveEnvironmentVariables();
				}
			}
		}

		private async void RemoveEnvVar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Tag is EnvVariable envVar)
			{
				envVariables.Remove(envVar);
				await SaveEnvironmentVariables();
			}
		}
	}

	public class EnvVariable
	{
		public string Key { get; set; }
		public string Value { get; set; }
	}
}