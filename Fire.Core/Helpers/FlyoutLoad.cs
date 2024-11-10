using FireBrowserWinUi3Core.CoreUi;
using Microsoft.UI.Xaml;

namespace FireBrowserWinUi3Core.Helpers;

public static class FlyoutLoad
{
    public static XamlRoot XamlRoot { get; set; }

    public static void ShowFlyout(FrameworkElement element)
    {
        new SecurityInfo().ShowAt(element);
    }
}