using FireBrowserWinUi3.Services;
using FireBrowserWinUi3MultiCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetupAccess : Page
    {
        public SetupAccess()
        {
            this.InitializeComponent();
            Langue.SelectedItem = "en-US";
            Langue.Text = "en-US";
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

        private void LiteMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {

                // Assuming 'url' and 'selection' have been defined earlier
                var autoSettingValue = toggleSwitch.IsOn;

                AppService.AppSettings.LightMode = autoSettingValue;

            }
        }

        private void Langue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selection = e.AddedItems[0].ToString();
            string type;

            switch (selection)
            {
                case "nl-NL":
                    type = "nl-NL";

                    break;
                case "en-US":
                    type = "en-US";

                    break;

                // Add other cases for different search engines.
                default:
                    // Handle the case when selection doesn't match any of the predefined options.
                    type = "en-US";

                    break;
            }

            if (!string.IsNullOrEmpty(type))
            {
                AppService.AppSettings.Lang = type;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SetupWebView));
        }
    }
}
