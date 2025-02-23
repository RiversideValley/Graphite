// OOBEUi.cs
using CommunityToolkit.WinUI;
using Graphite.UserSys;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

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
				await ShowErrorAndGoBackAsync();
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

				DispatcherQueue.TryEnqueue(() =>
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
				await ShowErrorAndGoBackAsync();
			}
		}

		private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				Border previewBorder = null;
				switch (textBox.Name)
				{
					case "BackgroundColorTextBox":
						previewBorder = BackgroundColorPreview;
						break;
					case "ToolbarColorTextBox":
						previewBorder = ToolbarColorPreview;
						break;
					case "TabViewColorTextBox":
						previewBorder = TabViewColorPreview;
						break;
				}

				UpdateColorPreview(textBox, previewBorder);
			}
		}

		private void UpdateColorPreview(TextBox textBox, Border previewBorder)
		{
			if (previewBorder != null)
			{
				if (TryParseColor(textBox.Text, out Color color))
				{
					previewBorder.Background = new SolidColorBrush(color);
				}
				else
				{
					previewBorder.Background = null;
				}
			}
		}

		private bool TryParseColor(string colorCode, out Color color)
		{
			color = Colors.Transparent;

			if (string.IsNullOrWhiteSpace(colorCode) || !colorCode.StartsWith("#"))
			{
				return false;
			}

			colorCode = colorCode.Trim();

			if (colorCode.Length != 7 && colorCode.Length != 9)
			{
				return false;
			}

			try
			{
				if (colorCode.Length == 7) // #RRGGBB
				{
					byte r = Convert.ToByte(colorCode.Substring(1, 2), 16);
					byte g = Convert.ToByte(colorCode.Substring(3, 2), 16);
					byte b = Convert.ToByte(colorCode.Substring(5, 2), 16);

					color = Color.FromArgb(255, r, g, b);
				}
				else // #AARRGGBB
				{
					byte a = Convert.ToByte(colorCode.Substring(1, 2), 16);
					byte r = Convert.ToByte(colorCode.Substring(3, 2), 16);
					byte g = Convert.ToByte(colorCode.Substring(5, 2), 16);
					byte b = Convert.ToByte(colorCode.Substring(7, 2), 16);

					color = Color.FromArgb(a, r, g, b);
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool IsValidColorCode(string colorCode)
		{
			return TryParseColor(colorCode, out _);
		}

		private async void NextStep_Click(object sender, RoutedEventArgs e)
		{
			if (!_initializationComplete.Task.IsCompleted)
			{
				await ShowErrorDialogAsync("Settings not initialized. Please try again.");
				return;
			}

			if (!ValidateColorInputs())
			{
				await ShowErrorDialogAsync("Please enter valid color codes (format: #RRGGBB).");
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
				await ShowErrorDialogAsync($"An error occurred while saving settings: {ex.Message}");
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

		private async void HelpButton_Click(object sender, RoutedEventArgs e)
		{
			var helpDialog = new ContentDialog
			{
				Title = "UI Settings Help",
				Content = "This page allows you to customize the appearance of the application. You can set colors for various UI elements, toggle visibility of certain features, and adjust the font size.",
				CloseButtonText = "OK",
				XamlRoot = this.Content.XamlRoot
			};

			await helpDialog.ShowAsync();
		}

		private async Task ShowErrorAndGoBackAsync(string message = "An error occurred. Please try again.")
		{
			await ShowErrorDialogAsync(message);
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
		}

		private async Task ShowErrorDialogAsync(string message)
		{
			var errorDialog = new ContentDialog
			{
				Title = "Error",
				Content = message,
				CloseButtonText = "OK",
				XamlRoot = this.Content.XamlRoot
			};

			await errorDialog.ShowAsync();
		}
	}
}