using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using static FireBrowserWinUi3.MainWindow;

namespace FireBrowserWinUi3.Pages.SettingsPages;

public sealed partial class SettingsAbout : Page
{
    private Passer? param;

    protected override void OnNavigatedTo(NavigationEventArgs e) =>
        param = e.Parameter as Passer;

    public SettingsAbout() => InitializeComponent();

    private void AboutCardClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not SettingsCard card) return;

        var url = card.Tag switch
        {
            "Discord" => "https://discord.gg/kYStRKBHwy",
            "GitHub" => "https://github.com/FirebrowserDevs/FireBrowserWinUi3",
            "License" => "https://github.com/FirebrowserDevs/FireBrowserWinUi3/blob/main/License.lic",
            _ => "https://example.com"
        };

        var window = (Application.Current as App)?.m_window as MainWindow;
        window.NavigateToUrl(url);
    }
}