using FireBrowserWinUi3.Pages.Patch;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FireBrowserWinUi3;

public sealed partial class SetupInit : Page
{
    private const string IntroMessage = @"
• Seamless browsing experience.

• One-click access to favorite websites and a built-in favorites organizer.

• Immersive full-screen mode.

• Prioritizes user convenience.

• Caters to users seeking a user-friendly web browser with advanced features. ";

    public SetupInit()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string IntroMessageProperty => IntroMessage;

    private void Setup_Click(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(SetupUser));

    private async void RestoreNow_Click(object sender, RoutedEventArgs e) =>
        await ShowRestoreBackupDialogAsync();

    private async Task ShowRestoreBackupDialogAsync()
    {
        var dlg = new RestoreBackupDialog { XamlRoot = XamlRoot };
        await dlg.ShowAsync();
        
    }
}