using CommunityToolkit.WinUI;
using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBEUi : Page
	{
		public User User { get; private set; }
		private TaskCompletionSource<bool> _initializationComplete;

		public OOBEUi()
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
				ShowErrorAndGoBack();
			}
		}

		private void SetControlsEnabled(bool enabled)
		{
			if (DispatcherQueue.HasThreadAccess)
			{
				BackgroundColorTextBox.IsEnabled = enabled;
				ToolbarColorTextBox.IsEnabled = enabled;
				TabViewColorTextBox.IsEnabled = enabled;
				ShowStatusBarToggle.IsEnabled = enabled;
				ShowToolbarIconsToggle.IsEnabled = enabled;
				ShowDarkModeIconToggle.IsEnabled = enabled;
				FontSizeBox.IsEnabled = enabled;
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

				string backgroundColor = await SettingsManager.GetSettingAsync<string>(User.Username, "BackgroundColor") ?? "#000000";
				string toolbarColor = await SettingsManager.GetSettingAsync<string>(User.Username, "ToolbarColor") ?? "#000000";
				string tabViewColor = await SettingsManager.GetSettingAsync<string>(User.Username, "TabViewColor") ?? "#000000";
				bool showStatusBar = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowStatusBar");
				bool showToolbarIcons = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowToolbarIcons");
				bool showDarkModeIcon = await SettingsManager.GetSettingAsync<bool>(User.Username, "ShowDarkModeIcon");
				int fontSize = await SettingsManager.GetSettingAsync<int>(User.Username, "FontSize");

				await DispatcherQueue.EnqueueAsync(() =>
				{
					BackgroundColorTextBox.Text = backgroundColor;
					ToolbarColorTextBox.Text = toolbarColor;
					TabViewColorTextBox.Text = tabViewColor;
					ShowStatusBarToggle.IsOn = showStatusBar;
					ShowToolbarIconsToggle.IsOn = showToolbarIcons;
					ShowDarkModeIconToggle.IsOn = showDarkModeIcon;
					FontSizeBox.Value = fontSize;

					SetControlsEnabled(true);
				});

				_initializationComplete.TrySetResult(true);
			}
			catch (Exception ex)
			{
				_initializationComplete.TrySetException(ex);
				System.Diagnostics.Debug.WriteLine($"Error initializing settings: {ex.Message}");
				ShowErrorAndGoBack();
			}
		}

		private bool IsValidColorCode(string colorCode)
		{
			return Regex.IsMatch(colorCode, @"^#[0-9A-Fa-f]{6}$");
		}

		private async void NextStep_Click(object sender, RoutedEventArgs e)
		{
			if (!_initializationComplete.Task.IsCompleted)
			{
				System.Diagnostics.Debug.WriteLine("Settings not initialized. Please try again.");
				return;
			}

			if (!ValidateColorInputs())
			{
				System.Diagnostics.Debug.WriteLine("Please enter valid color codes (format: #RRGGBB).");
				return;
			}

			try
			{
				SetControlsEnabled(false);

				await SettingsManager.UpdateSettingAsync(User.Username, "BackgroundColor", BackgroundColorTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "ToolbarColor", ToolbarColorTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "TabViewColor", TabViewColorTextBox.Text);
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowStatusBar", ShowStatusBarToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowToolbarIcons", ShowToolbarIconsToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "ShowDarkModeIcon", ShowDarkModeIconToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "FontSize", (int)FontSizeBox.Value);

				Frame.Navigate(typeof(OOBEPrivacy), User);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"An error occurred while saving settings: {ex.Message}");
			}
			finally
			{
				SetControlsEnabled(true);
			}
		}

		private bool ValidateColorInputs()
		{
			return IsValidColorCode(BackgroundColorTextBox.Text) &&
				   IsValidColorCode(ToolbarColorTextBox.Text) &&
				   IsValidColorCode(TabViewColorTextBox.Text);
		}

		private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Color preview functionality removed
		}

		private void HelpButton_Click(object sender, RoutedEventArgs e)
		{
			// Help dialog removed
			System.Diagnostics.Debug.WriteLine("Help button clicked. Functionality removed.");
		}

		private void ShowErrorAndGoBack()
		{
			System.Diagnostics.Debug.WriteLine("An error occurred. Please try again.");
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
		}
	}
}