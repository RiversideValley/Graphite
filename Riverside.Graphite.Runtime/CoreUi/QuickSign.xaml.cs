using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Riverside.Graphite.Runtime.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace Riverside.Graphite.Runtime.CoreUi;
public sealed partial class QuickSign : Window
{
	private AppWindow appWindow;
	private AppWindowTitleBar titleBar;
	private bool IsLoadSite { get; set; }
	private IMessenger Messenger { get; set; }
	public QuickSign(string site)
	{
		InitializeComponent();

		webView.NavigationCompleted += (sender, e) =>
		{
			IsLoadSite = true;
		};

		_ = InitializeWindow().ConfigureAwait(false);
		NavigateToSite(site);
		LoadWeb();
	}

	private Task InitializeWindow()
	{
		nint hWnd = WindowNative.GetWindowHandle(this);
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
		appWindow = AppWindow.GetFromWindowId(windowId);

		appWindow.MoveAndResize(new RectInt32(500, 500, 720, 960));
		Riverside.Graphite.Runtime.Helpers.Windowing.Center(this);
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
		CommandBarFlyout ctx = Ctx; // Correct the cast and use the appropriate variable name
		CommandBarFlyout flyout = FlyoutBase.GetAttachedFlyout(webView) as Microsoft.UI.Xaml.Controls.CommandBarFlyout; // Cast to the correct type

		FlyoutShowOptions options = new()
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
		webView.Source = site.ToLower() switch
		{
			"microsoft" => new Uri("https://account.microsoft.com"),
			"google" => new Uri("https://accounts.google.com/"),
			_ => new Uri("about:blank"),
		};
		while (!IsLoadSite)
		{
			await Task.Delay(100);
		}

		_ = Windowing.ShowWindow(WindowNative.GetWindowHandle(this), Windowing.WindowShowStyle.SW_SHOWNOACTIVATE | Windowing.WindowShowStyle.SW_RESTORE);
	}

	private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
	{
		if (sender is not AppBarButton { Tag: not null } button)
		{
			return;
		}

		Microsoft.Web.WebView2.Core.CoreWebView2 webview = webView.CoreWebView2;

		switch (button.Tag)
		{
			case "Back" when webview.CanGoBack: webview.GoBack(); break;
			case "Forward" when webview.CanGoForward: webview.GoForward(); break;
			case "Refresh": webview.Reload(); break;
		}

		Ctx.Hide();
	}
}