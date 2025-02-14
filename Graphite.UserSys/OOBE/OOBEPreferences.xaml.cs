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

        public OOBEPreferences()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is User user)
            {
                User = user;
                _ = LoadCurrentSettingsAsync();
            }
            else
            {
                ShowErrorAndGoBack();
            }
        }

        private async Task LoadCurrentSettingsAsync()
        {
            try
            {
                SettingsManager.InitializeUserSettingsAsync(User.Username);
                // Load display language
                string currentLanguage = await SettingsManager.GetSettingAsync<string>(User.Username, "DisplayLanguage") ?? "en-US";
                DisplayLanguageComboBox.SelectedIndex = currentLanguage == "nl-NL" ? 1 : 0;

                // Load encryption settings
                EncryptPermissionsToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptPermissions");
                EncryptDatabaseToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptDatabase");
                EncryptSettingsToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptSettings");
                EncryptHistoryToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptHistory");
                EncryptFavoritesToggle.IsOn = await SettingsManager.GetSettingAsync<bool>(User.Username, "EncryptFavorites");
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"Error loading settings: {ex.Message}");
            }
        }

        private async void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog helpDialog = new ContentDialog
            {
                Title = "Account Preferences Setup",
                Content = "Here you will set account Preferences. If you need further assistance, please contact support.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await helpDialog.ShowAsync();
        }

        private async void NextStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save display language
                LanguageSetting selectedLanguage = DisplayLanguageComboBox.SelectedItem as LanguageSetting;
                if (selectedLanguage == null)
                {
                    // Fallback to default language if nothing is selected
                    selectedLanguage = new LanguageSetting("en-US", "English (US)");
                    selectedLanguage = new LanguageSetting("nl-NL", "Nederlands (NL)");

                }

                var settingsToUpdate = new Dictionary<string, object>
{
    { "DisplayLanguage", selectedLanguage }
};

                await SettingsManager.UpdateSettingsAsync(User.Username, settingsToUpdate);

                // Save encryption settings
                await SettingsManager.UpdateEncryptionSettingAsync(User.Username, "EncryptPermissions", EncryptPermissionsToggle.IsOn);
                await SettingsManager.UpdateEncryptionSettingAsync(User.Username, "EncryptDatabase", EncryptDatabaseToggle.IsOn);
                await SettingsManager.UpdateEncryptionSettingAsync(User.Username, "EncryptSettings", EncryptSettingsToggle.IsOn);
                await SettingsManager.UpdateEncryptionSettingAsync(User.Username, "EncryptHistory", EncryptHistoryToggle.IsOn);
                await SettingsManager.UpdateEncryptionSettingAsync(User.Username, "EncryptFavorites", EncryptFavoritesToggle.IsOn);

                //Frame.Navigate(typeof(OOBEUi)); 
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync($"An error occurred while saving settings: {ex.Message}");
            }
        }

        private async Task ShowErrorDialogAsync(string message)
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

        public class LanguageSetting
        {
            public string Code { get; set; }
            public string Name { get; set; }

            public LanguageSetting(string code, string name)
            {
                Code = code;
                Name = name;
            }
        }

        private void ShowErrorAndGoBack()
        {
            _ = ShowErrorDialogAsync("An error occurred. Please try again.");
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}