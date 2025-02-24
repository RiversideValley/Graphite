using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using Graphite.UserSys;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class AccessibilitySettings : Page
	{
		private string currentUsername;

		public AccessibilitySettings()
		{
			this.InitializeComponent();
			InitializeControls();
		}

		private async void InitializeControls()
		{
			currentUsername = UserManager.GetCurrentUsername();

			// Initialize Lite Mode
			bool liteModeEnabled = await SettingsManager.GetSettingAsync<bool>(currentUsername, "LiteModeEnabled", false);
			LiteMode.IsOn = liteModeEnabled;

			// Initialize Welcome Message
			bool showWelcomeMessage = await SettingsManager.GetSettingAsync<bool>(currentUsername, "ShowWelcomeMessage", true);
			WelcomeMesg.IsOn = showWelcomeMessage;

			// Initialize Confirm Dialog
			bool showConfirmCloseDialog = await SettingsManager.GetSettingAsync<bool>(currentUsername, "ShowConfirmCloseDialog", false);
			ConfirmDialog.IsOn = showConfirmCloseDialog;

			// Initialize Language ComboBox
			List<string> languages = new List<string> { "English", "Spanish", "French", "German", "Chinese" };
			Langue.ItemsSource = languages;
			string currentLanguage = await SettingsManager.GetSettingAsync<string>(currentUsername, "SpeechEngineLanguage", "English");
			Langue.SelectedItem = currentLanguage;

			// Initialize Gender ComboBox
			List<string> genders = new List<string> { "Male", "Female" };
			Gender.ItemsSource = genders;
			string currentGender = await SettingsManager.GetSettingAsync<string>(currentUsername, "SpeechEngineGender", "Male");
			Gender.SelectedItem = currentGender;
		}

		private async void LiteMode_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "LiteModeEnabled", LiteMode.IsOn);
			// Additional logic for enabling/disabling Lite Mode features
		}

		private async void WelcomeMesg_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "ShowWelcomeMessage", WelcomeMesg.IsOn);
		}

		private async void ConfirmDialog_Toggled(object sender, RoutedEventArgs e)
		{
			await SettingsManager.UpdateSettingAsync(currentUsername, "ShowConfirmCloseDialog", ConfirmDialog.IsOn);
		}

		private async void Langue_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Langue.SelectedItem is string selectedLanguage)
			{
				await SettingsManager.UpdateSettingAsync(currentUsername, "SpeechEngineLanguage", selectedLanguage);
				// Additional logic for changing speech engine language
			}
		}

		private async void Gender_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Gender.SelectedItem is string selectedGender)
			{
				await SettingsManager.UpdateSettingAsync(currentUsername, "SpeechEngineGender", selectedGender);
				// Additional logic for changing speech engine gender
			}
		}
	}
}