using CommunityToolkit.Mvvm.Messaging;
using FireBrowserWinUi3Core.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace FireBrowserWinUi3Core.CoreUi;
public sealed partial class QuickSign : Window
{
    private AppWindow appWindow;
    private AppWindowTitleBar titleBar;
    private bool IsLoadSite { get; set; }
    private IMessenger Messenger { get; set; }
    public QuickSign(string site)
    {

        this.InitializeComponent();

        webView.NavigationCompleted += (sender, e) =>
        {
            IsLoadSite = true;
        };

        InitializeWindow().ConfigureAwait(false);
        NavigateToSite(site);
        LoadWeb();


    }

    private Task InitializeWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.MoveAndResize(new RectInt32(500, 500, 720, 960));
        FireBrowserWinUi3Core.Helpers.Windowing.Center(this);
        appWindow.SetPresenter(AppWindowPresenterKind.Default);
        appWindow.MoveInZOrderAtTop();
        appWindow.SetIcon("accounts.ico");

        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return Task.FromException(new Exception("Unsupported OS version."));
        }
        else
        {
            titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = false;
        }

        return Task.CompletedTask;
    }

    public async void LoadWeb()
    {
        await webView.EnsureCoreWebView2Async(null);
        webView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
    }

    private void CoreWebView2_ContextMenuRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuRequestedEventArgs args)
    {
        var ctx = (Microsoft.UI.Xaml.Controls.CommandBarFlyout)Ctx; // Correct the cast and use the appropriate variable name
        var flyout = FlyoutBase.GetAttachedFlyout(webView) as Microsoft.UI.Xaml.Controls.CommandBarFlyout; // Cast to the correct type

        var options = new FlyoutShowOptions()
        {
            Position = args.Location,
            ShowMode = FlyoutShowMode.Standard
        };

        flyout = flyout ?? ctx; // Use the previously defined ctx variable if flyout is null
        flyout.ShowAt(webView, options);
        args.Handled = true;
    }

    private async void NavigateToSite(string site)
    {
        switch (site.ToLower())
        {
            case "microsoft":
                webView.Source = new Uri("https://account.microsoft.com");
                break;
            case "google":
                webView.Source = new Uri("https://accounts.google.com/");
                break;
            default:
                webView.Source = new Uri("about:blank");
                break;
        }

        while (!IsLoadSite)
        {
            await Task.Delay(100);
        }

        Windowing.ShowWindow(WindowNative.GetWindowHandle(this), Windowing.WindowShowStyle.SW_SHOWNOACTIVATE | Windowing.WindowShowStyle.SW_RESTORE);

    }

    private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not AppBarButton { Tag: not null } button) return;

        var webview = webView.CoreWebView2;

        switch (button.Tag)
        {
            case "Back" when webview.CanGoBack: webview.GoBack(); break;
            case "Forward" when webview.CanGoForward: webview.GoForward(); break;
            case "Refresh": webview.Reload(); break;
        }

        Ctx.Hide();
    }

}