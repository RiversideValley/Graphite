using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Graph.Groups.Item.Team.Channels.Item.DoesUserHaveAccessuserIdUserIdTenantIdTenantIdUserPrincipalNameUserPrincipalName;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Core;
using Riverside.Graphite.Pages;
using Riverside.Graphite.Pages.Patch;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.Services.Notifications.Toasts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Riverside.Graphite.Services.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient
{
	internal MainWindow MainView { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsMsLoginVisibility), nameof(IsMsButtonVisibility))]
	private bool isMsLogin = AppService.IsAppUserAuthenicated;

	[ObservableProperty]
	private BitmapImage msProfilePicture;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(MsOptionsWebCommand))]
	private ListViewItem msOptionSelected;

	public Visibility IsMsLoginVisibility => IsMsLogin ? Visibility.Visible : Visibility.Collapsed;
	public Visibility IsMsButtonVisibility => !IsMsLogin ? Visibility.Visible : Visibility.Collapsed;

	[ObservableProperty]
	private BitmapImage profileImage;

	[ObservableProperty]
	private BitmapImage webViewContentPicture;
	public MainWindowViewModel(IMessenger messenger) : base(messenger)
	{
		Messenger.Register<Message_Settings_Actions>(this, (r, m) => ReceivedStatus(m));
	}

	[RelayCommand]
	public void CloseMoreFlyout()
	{
		_ = MainView.DispatcherQueue.TryEnqueue(() =>
		{
			MainView?.MoreFlyout.Hide();
		});
	}
	[RelayCommand]
	public Task GetActiveWebView()
	{
		MainWindow currentWindow = (Application.Current as App)?.m_window as MainWindow;
		if (currentWindow != null && currentWindow.TabViewContainer.SelectedItem is FireBrowserTabViewItem && currentWindow.TabContent.Content is WebContent web)
		{
			WebViewContentPicture = web.PictureWebElement;
		}
		OnPropertyChanged(nameof(WebViewContentPicture));
		return Task.CompletedTask;
	}
	private async Task ValidateMicrosoft()
	{
		IsMsLogin = true; // AppService.MsalService.IsSignedIn;
		if (IsMsLogin && AppService.GraphService.ProfileMicrosoft is null)
		{
			using Stream stream = await AppService.MsalService.GraphClient?.Me.Photo.Content.GetAsync();
			if (stream == null)
			{
				MsProfilePicture = new BitmapImage(new Uri("ms-appx:///Assets/Icons/Products/MicrosoftOffice.png"));
				return;
			}

			using MemoryStream memoryStream = new();
			await stream.CopyToAsync(memoryStream);
			memoryStream.Position = 0;

			BitmapImage bitmapImage = new();
			await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
			MsProfilePicture = bitmapImage;
		}
		else if (IsMsLogin)
		{
			MsProfilePicture = AppService.GraphService.ProfileMicrosoft;
		}
	}

	private const string nameof = "Copilot By Graphite";
	public Task<bool> CopilotExists() {

		
		if (Windowing.FindWindowsByName(nameof) is List<nint> collection)
		{

			if (collection.Count > 0)
			{
				foreach (nint winId in collection)
				{
					if (Windowing.IsWindow(winId))
					{
						if (Windowing.IsWindowVisible(winId))
						{
							Windowing.ShowWindow(winId, Windowing.WindowShowStyle.SW_HIDE);
						}
						else {
							Windowing.ShowWindow(winId, Windowing.WindowShowStyle.SW_SHOW);
						}
						
						
					}
					else
						continue;
				}
				return Task.FromResult(true);
			}
		}

		return Task.FromResult(false);

	}

	[RelayCommand]
	private async Task GoCopilotOpen() {


		if (await CopilotExists())
			return; 
				
			SizeInt32? desktop = await Windowing.SizeWindow();

			Window wndCopilot = new Window();
		
			
			WebView2 web = new WebView2();
			web.Margin = new Thickness(0, 32, 0, 0); 
			web.Source =  new Uri("https://copilot.microsoft.com/?showconv=1&?auth=1");
			await web.EnsureCoreWebView2Async();
			web.CoreWebView2.NewWindowRequested += (s, e) =>
			{

				MainView.NavigateToUrl(e.Uri);
				e.Handled = true;

			};

			wndCopilot.Content = web; 
			
			// window procs
			IntPtr hWnd = WindowNative.GetWindowHandle(wndCopilot);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			AppWindow appWindow = AppWindow.GetFromWindowId(wndId) ;
			appWindow.SetIcon("ms-appx:///Assets/Icons/copilot.png");

			if (appWindow != null)
			{
				appWindow.MoveAndResize(new RectInt32(0, 0, 600, (int)(desktop.Value.Height * .75)));
				appWindow.MoveInZOrderAtTop();
				appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
				appWindow.Title = nameof;
				AppWindowTitleBar titleBar = appWindow.TitleBar;
				Windows.UI.Color btnColor = Colors.Transparent;
				titleBar.BackgroundColor = btnColor;
				titleBar.ForegroundColor = Colors.WhiteSmoke;
				titleBar.ButtonBackgroundColor = btnColor;
				titleBar.ButtonInactiveBackgroundColor = btnColor;
				appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

			}

		wndCopilot.Activate(); 
		Windowing.AnimateWindow(hWnd, 2000, Windowing.AW_HOR_NEGATIVE | Windowing.AW_SLIDE | Windowing.AW_ACTIVATE);

	}
	[RelayCommand]
	private async Task LogOut()
	{
		if (MainView.TabWebView is not null)
		{
			MainView.NavigateToUrl("https://login.microsoftonline.com/common/oauth2/v2.0/logout?client_id=edfc73e2-cac9-4c47-a84c-dedd3561e8b5&post_logout_redirect_uri=https://www.bing.com");
		}

		using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
		try
		{
			while (AppService.IsAppUserAuthenicated)
			{
				if (!AppService.IsAppUserAuthenicated)
				{
					IsMsLogin = false;
					MainView.MsLoggedInOptions.Hide();
					break;
				}

				await Task.Delay(400, cts.Token);
			}
		}
		catch (OperationCanceledException)
		{
			AppService.IsAppUserAuthenicated = IsMsLogin = false;
			MainView.NavigateToUrl("https://fireapp.msal/main.html");
			_ = MainView.NotificationQueue.Show("You've been logged out of Microsoft", 15000, "Authorization");
			Console.WriteLine("The task was canceled due to timeout.");
		}
	}

	[RelayCommand]
	private async Task AdminCenter()
	{
		if (!AppService.MsalService.IsSignedIn)
		{
			Microsoft.Identity.Client.AuthenticationResult answer = await AppService.MsalService.SignInAsync();
			if (answer is null)
			{
				_ = MainView.NotificationQueue.Show("You must sign into the FireBrowser Application for cloudbackups!", 1000, "Backups");
				return;
			}
		}

		UpLoadBackup win = new()
		{
			ExtendsContentIntoTitleBar = true
		};
		win.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay);
		Windows.Graphics.SizeInt32? desktop = await Windowing.SizeWindow();
		win.AppWindow.MoveAndResize(new(MainView.AppWindow.Position.X, 0, desktop.Value.Width / 2, desktop.Value.Height / 2));

		nint handle = WindowNative.GetWindowHandle(win);
		_ = Windowing.ShowWindow(handle, Windowing.WindowShowStyle.SW_SHOWDEFAULT);
		_ = Windowing.AnimateWindow(handle, 2000, Windowing.AW_BLEND | Windowing.AW_VER_POSITIVE | Windowing.AW_ACTIVATE);
		win.AppWindow?.ShowOnceWithRequestedStartupState();
	}

	[RelayCommand]
	private void MsOptionsWeb(FrameworkElement sender)
	{
		try
		{
			MainView?.NavigateToUrl(sender.Tag?.ToString());
			MainView.MsLoggedInOptions.Hide();
		}
		catch (Exception e)
		{
			ExceptionLogger.LogException(e);
			_ = Messenger.Send(new Message_Settings_Actions("Can't navigate to the requested website", EnumMessageStatus.Informational));
		}
	}

	[RelayCommand]
	private void LoginToMicrosoft(Button sender)
	{
		if (!AppService.IsAppUserAuthenicated)
		{
			MainView.NavigateToUrl("https://fireapp.msal/main.html");
		}
		else
		{
			IsMsLogin = true;
			FlyoutBase.SetAttachedFlyout(sender, MainView.MsLoggedInOptions);
			FlyoutBase.ShowAttachedFlyout(sender);
		}
	}

	private void ReceivedStatus(Message_Settings_Actions message)
	{
		if (message is null)
		{
			return;
		}

		switch (message.Status)
		{
			case EnumMessageStatus.Login:
				ShowLoginNotification();
				break;
			case EnumMessageStatus.Settings:
				_ = MainView.LoadUserSettings();
				break;
			case EnumMessageStatus.Removed:
				ShowRemovedNotification();
				break;
			case EnumMessageStatus.XorError:
				ShowErrorNotification(message.Payload!);
				break;
			default:
				ShowNotifyNotification(message.Payload!);
				break;
		}
	}

	private void ShowErrorNotification(string payload)
	{
		ShowNotification("Riverside.Graphite Error", payload, InfoBarSeverity.Error, TimeSpan.FromSeconds(5));
	}

	private void ShowNotifyNotification(string payload)
	{
		ShowNotification("Riverside.Graphite Information", payload, InfoBarSeverity.Informational, TimeSpan.FromSeconds(5));
	}

	private void ShowRemovedNotification()
	{
		ShowNotification("Riverside.Graphite", "User has been removed from FireBrowser!", InfoBarSeverity.Warning, TimeSpan.FromSeconds(3));
	}

	private void ShowLoginNotification()
	{
		_ = ToastRatings.SendToast();
		ShowNotification("Riverside.Graphite", $"Welcome, {AuthService.CurrentUser.Username.ToUpperInvariant()}!", InfoBarSeverity.Informational, TimeSpan.FromSeconds(3));
	}

	private void ShowNotification(string title, string message, InfoBarSeverity severity, TimeSpan duration)
	{
		Notification note = new()
		{
			Title = $"{title}\n",
			Message = message,
			Severity = severity,
			IsIconVisible = true,
			Duration = duration
		};
		_ = MainView.NotificationQueue.Show(note);
	}
}