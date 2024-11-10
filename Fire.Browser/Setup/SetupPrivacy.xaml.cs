using FireBrowserWinUi3.Services;
using FireBrowserWinUi3MultiCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FireBrowserWinUi3;
public sealed partial class SetupPrivacy : Page
{
    public SetupPrivacy()
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
    private void DisableJavaScriptToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.DisableJavaScript = autoSettingValue; ;

        }
    }

    private void DisableGenaralAutoFillToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.DisableGenAutoFill = autoSettingValue; ; ;

        }
    }

    private void DisablWebMessFillToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.DisableWebMess = autoSettingValue;

        }
    }

    private void PasswordWebMessFillToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.DisablePassSave = autoSettingValue;

        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SetupAccess));
    }
}
