using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace FireBrowserWinUi3;
public sealed partial class SetupUi : Page
{
    public SetupUi()
    {
        this.InitializeComponent();
        ColorTV.Text = "#000000";
        ColorTB.Text = "#000000";
    }

    private void SetupUiBtn_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SetupAlgemeen));
    }

    private void AutoTog_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.Auto = autoSettingValue;

        }
    }

    private void ColorTB_TextChanged(object sender, TextChangedEventArgs e)
    {
        string color = ColorTB.Text.ToString();
        if (!string.IsNullOrEmpty(color))
        {
            AppService.AppSettings.ColorBackground = color;
        }
    }

    private void ColorTV_TextChanged(object sender, TextChangedEventArgs e)
    {
        string color = ColorTV.Text.ToString();
        if (!string.IsNullOrEmpty(color))
        {
            AppService.AppSettings.ColorTool = color;
        }
    }

    private void DateTime_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.NtpDateTime = autoSettingValue; ;

        }
    }

    private void NtpColorText_TextChanged(object sender, TextChangedEventArgs e)
    {
        string color = NtpColorText.Text.ToString();
        if (!string.IsNullOrEmpty(color))
        {
            AppService.AppSettings.NtpTextColor = color;
        }
    }

    private void Color_TextChanged(object sender, TextChangedEventArgs e)
    {

        string color = Color.Text.ToString();
        if (!string.IsNullOrEmpty(color))
        {
            AppService.AppSettings.ColorTV = color;
        }
    }

    private void Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string selection = e.AddedItems[0].ToString();
        int type = default;


        switch (selection)
        {
            case "Default":
                type = 0;
                Color.IsEnabled = false;
                Image.Visibility = Visibility.Visible;
                Image2.Visibility = Visibility.Collapsed;
                Image3.Visibility = Visibility.Collapsed;
                break;
            case "Featured":
                type = 1;
                Color.IsEnabled = false;
                Image2.Visibility = Visibility.Visible;
                Image.Visibility = Visibility.Collapsed;
                Image3.Visibility = Visibility.Collapsed;
                break;
            case "Custom":
                type = 2;
                Color.IsEnabled = true;
                Image3.Visibility = Visibility.Visible;
                Image.Visibility = Visibility.Collapsed;
                Image2.Visibility = Visibility.Collapsed;
                break;

            // Add other cases for different search engines.
            default:
                // Handle the case when selection doesn't match any of the predefined options.
                type = 0;
                Color.IsEnabled = false;
                break;
        }

        AppService.AppSettings.Background = type;

    }

    private void SearchHome_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.IsSearchVisible = autoSettingValue; ;

        }
    }

    private void HistoryHome_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.IsHistoryVisible = autoSettingValue; ;

        }
    }

    private void FavoritesHome_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.IsFavoritesVisible = autoSettingValue; ;

        }
    }

    private void TrendingHome_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            // Assuming 'url' and 'selection' have been defined earlier
            var autoSettingValue = toggleSwitch.IsOn;
            AppService.AppSettings.IsTrendingVisible = autoSettingValue; ;

        }
    }
}
