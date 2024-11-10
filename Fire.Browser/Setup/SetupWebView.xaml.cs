using FireBrowserWinUi3.Services;
using FireBrowserWinUi3DataCore.Actions;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;

namespace FireBrowserWinUi3;
public sealed partial class SetupWebView : Page
{
    public SetupWebView()
    {
        this.InitializeComponent();
    }

    private FireBrowserWinUi3MultiCore.User GetUser()
    {
        // Check if the user is authenticated.
        if (AuthService.IsUserAuthenticated)
        {
            // Return the authenticated user.
            return AuthService.CurrentUser;
        }

        // If no user is authenticated, return null or handle as needed.
        return null;
    }
    private void StatusTog_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.StatusBar = autoSettingValue;

        }
    }

    private void BrowserKeys_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.BrowserKeys = autoSettingValue; ;
        }
    }

    private void BrowserScripts_Toggled(object sender, RoutedEventArgs e)
    {

        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.BrowserScripts = autoSettingValue;
        }
    }

    private void userag_TextChanged(object sender, TextChangedEventArgs e)
    {
        string blob = userag.Text.ToString();
        if (!string.IsNullOrEmpty(blob))
        {
            AppService.AppSettings.Useragent = blob;
        }
    }
    private async void CreateNewSettings()
    {

        try
        {
            var settingsActions = new SettingsActions(AuthService.NewCreatedUser?.Username);
            var settingsPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.NewCreatedUser?.Username, "Settings", "Settings.db");

            if (!File.Exists(settingsPath))
            {
                await settingsActions.SettingsContext.Database.MigrateAsync();
            }

            if (File.Exists(settingsPath))
            {
                if (await settingsActions.SettingsContext.Database.CanConnectAsync())
                    await settingsActions.UpdateSettingsAsync(AppService.AppSettings);
            }

        }
        catch (Exception ex)
        {
            ExceptionLogger.LogException(ex);
            Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");
        }
        finally
        {
            AuthService.NewCreatedUser = null;
        }
    }

    private void SetupWebViewBtn_Click(object sender, RoutedEventArgs e)
    {
        // allow user settings to be created from current setup user.
        // synchronous call because I want to make sure database is created first before your time out on the setupfinish page...

        CreateNewSettings();

        Frame.Navigate(typeof(SetupFinish));
    }
}
