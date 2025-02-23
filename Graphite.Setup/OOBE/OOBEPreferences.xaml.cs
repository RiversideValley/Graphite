using Graphite.UserSys;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBEPreferences : Page
	{
		public User User { get; private set; }
		private readonly List<LanguageSetting> Languages = new List<LanguageSetting>
		{
			new LanguageSetting("en-US", "English (US)"),
			new LanguageSetting("nl-NL", "Nederlands (NL)")
		};
		private readonly List<string> BackdropStyles = new List<string> { "Mica", "MicaAlt", "Acrylic", "None" };
		private TaskCompletionSource<bool> _initializationComplete;

		public OOBEPreferences()
		{
			this.InitializeComponent();
			DisplayLanguageComboBox.ItemsSource = Languages;
			BackdropStyleComboBox.ItemsSource = BackdropStyles;
			_initializationComplete = new TaskCompletionSource<bool>();

			SetControlsEnabled(false);
		}

		private void SetControlsEnabled(bool enabled)
		{
			if (DispatcherQueue.HasThreadAccess)
			{
				DisplayLanguageComboBox.IsEnabled = enabled;
				UseLightModeToggle.IsEnabled = enabled;
				BackdropStyleComboBox.IsEnabled = enabled;
				TabGroupingEnabledToggle.IsEnabled = enabled;
				TabPreloadingToggle.IsEnabled = enabled;
				TabSleepTimeBox.IsEnabled = enabled;
				GeneralTabAutoRestoreToggle.IsEnabled = enabled;
				GeneralTabMaxNumberBox.IsEnabled = enabled;
				NextStep.IsEnabled = enabled;
			}
			else
			{
				DispatcherQueue.TryEnqueue(() => SetControlsEnabled(enabled));
			}
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

		private async Task InitializeSettingsAsync()
		{
			try
			{
				await SettingsManager.InitializeUserSettingsAsync(User.Username);

				string currentLanguage = await SettingsManager.GetSettingAsync<string>(User.Username, "DisplayLanguage") ?? "en-US";
				bool useLightMode = await SettingsManager.GetSettingAsync<bool>(User.Username, "UseLightMode");
				string backdropStyle = await SettingsManager.GetSettingAsync<string>(User.Username, "BackdropStyle") ?? "Mica";
				bool tabGroupingEnabled = await SettingsManager.GetSettingAsync<bool>(User.Username, "TabGroupingEnabled");
				bool tabPreloading = await SettingsManager.GetSettingAsync<bool>(User.Username, "TabPreloading");
				int tabSleepTime = await SettingsManager.GetSettingAsync<int>(User.Username, "TabSleepTime");
				bool generalTabAutoRestore = await SettingsManager.GetSettingAsync<bool>(User.Username, "GenaralTabAutoRestore");
				int generalTabMaxNumber = await SettingsManager.GetSettingAsync<int>(User.Username, "GenaralTabMaxNumber");

				DispatcherQueue.TryEnqueue(() =>
				{
					DisplayLanguageComboBox.SelectedItem = Languages.Find(l => l.Code == currentLanguage) ?? Languages[0];
					UseLightModeToggle.IsOn = useLightMode;
					BackdropStyleComboBox.SelectedItem = backdropStyle;
					TabGroupingEnabledToggle.IsOn = tabGroupingEnabled;
					TabPreloadingToggle.IsOn = tabPreloading;
					TabSleepTimeBox.Value = tabSleepTime;
					GeneralTabAutoRestoreToggle.IsOn = generalTabAutoRestore;
					GeneralTabMaxNumberBox.Value = generalTabMaxNumber;

					SetControlsEnabled(true);
				});

				_initializationComplete.TrySetResult(true);
			}
			catch (Exception ex)
			{
				_initializationComplete.TrySetException(ex);
				await ShowErrorDialogAsync($"Error initializing settings: {ex.Message}", this.Content.XamlRoot);
				ShowErrorAndGoBack();
			}
		}

		private async void NextStep_Click(object sender, RoutedEventArgs e)
		{
			if (!_initializationComplete.Task.IsCompleted)
			{
				await ShowErrorDialogAsync("Settings not initialized. Please try again.", this.Content.XamlRoot);
				return;
			}

			try
			{
				SetControlsEnabled(false);

				var selectedLanguage = DisplayLanguageComboBox.SelectedItem as LanguageSetting;
				if (selectedLanguage == null)
				{
					throw new InvalidOperationException("No language selected");
				}

				await SettingsManager.UpdateSettingAsync(User.Username, "DisplayLanguage", selectedLanguage.Code);
				await SettingsManager.UpdateSettingAsync(User.Username, "UseLightMode", UseLightModeToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "BackdropStyle", BackdropStyleComboBox.SelectedItem as string);
				await SettingsManager.UpdateSettingAsync(User.Username, "TabGroupingEnabled", TabGroupingEnabledToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "TabPreloading", TabPreloadingToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "TabSleepTime", (int)TabSleepTimeBox.Value);
				await SettingsManager.UpdateSettingAsync(User.Username, "GenaralTabAutoRestore", GeneralTabAutoRestoreToggle.IsOn);
				await SettingsManager.UpdateSettingAsync(User.Username, "GenaralTabMaxNumber", (int)GeneralTabMaxNumberBox.Value);

				Frame.Navigate(typeof(OOBEUi));
			}
			catch (Exception ex)
			{
				await ShowErrorDialogAsync($"An error occurred while saving settings: {ex.Message}", this.Content.XamlRoot);
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
					Title = "User Preferences Setup",
					Content = "Here you can configure your user preferences:\n\n" +
							 "• Display Language: Choose your preferred interface language\n" +
							 "• Light Mode: Toggle between light and dark mode\n" +
							 "• Backdrop Style: Choose the app's backdrop style (Mica, Acrylic, or None)\n" +
							 "• Tab Management:\n" +
							 "  - Tab Grouping: Enable or disable tab grouping\n" +
							 "  - Tab Preloading: Enable or disable tab preloading\n" +
							 "  - Tab Sleep Time: Set the time before tabs go to sleep\n" +
							 "  - Auto Restore Tabs: Enable or disable automatic tab restoration\n" +
							 "  - Maximum Number of Tabs: Set the maximum number of allowed tabs\n\n" +
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

		private async Task ShowErrorDialogAsync(string message, XamlRoot xaml)
		{
			try
			{
				var errorDialog = new ContentDialog
				{
					Title = "Error",
					Content = message,
					CloseButtonText = "OK",
					XamlRoot = xaml
				};

				await errorDialog.ShowAsync();
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine($"Error showing dialog: {message}");
			}
		}

		public class LanguageSetting
		{
			public string Code { get; set; }
			public string Name { get; set; }

			public LanguageSetting(string code, string name)
			{
				Code = code;
				Name = name;
			}

			public override string ToString()
			{
				return Name;
			}
		}

		private void ShowErrorAndGoBack()
		{
			_ = ShowErrorDialogAsync("An error occurred. Please try again.", this.Content.XamlRoot);
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
			}
		}
	}
}