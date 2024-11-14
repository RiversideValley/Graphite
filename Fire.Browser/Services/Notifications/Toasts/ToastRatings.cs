using Fire.Browser.Core;
using Fire.Core.Helpers;
using FireBrowserWinUi3.Services.Messages;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WinRT.Interop;
using static FireBrowserWinUi3.Services.Notifications.NotificationMessenger;

namespace FireBrowserWinUi3.Services.Notifications.Toasts;

public sealed class ToastRatings
{

	public const string Title = "Fire Browser Notifications";

	public static ObservableCollection<FireNotification> NotificationMessages = new();

	public static bool SendToast()
	{
		var appNotification = new AppNotificationBuilder()
			.AddArgument("action", "ToastClick")
			.AddArgument(((int)EnumMessageStatus.Informational).ToString(), "UserStatus")
			.SetAppLogoOverride(new Uri("file://" + App.GetFullPathToAsset("fireglobe.png")), AppNotificationImageCrop.Circle)
			.AddText(Title)
			.AddText($"Welcome to your Fire Browser{AuthService.CurrentUser.Username ?? "?"}")
			.AddButton(new AppNotificationButton("Rate Us")
				.AddArgument("action", "OpenApp")
				.SetButtonStyle(AppNotificationButtonStyle.Default)
				.AddArgument(((int)EnumMessageStatus.Informational).ToString(), "UserStatus"))
			.BuildNotification();

		AppNotificationManager.Default.Show(appNotification);

		return appNotification.Id != 0; // return true (indicating success) if the toast was sent (if it has an Id)
	}

	public static async void NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
	{
		var noteMsg = new NotificationMessenger(ref NotificationMessages);
		var notification = new FireNotification();
		notification.Originator = Title;
		notification.Action = notificationActivatedEventArgs.Arguments["action"];
		await noteMsg.PromptUserToRateApp(notification);
		if (Application.Current is App app && app.m_window is MainWindow window)
		{
			nint hWnd = WindowNative.GetWindowHandle(window);
			Windowing.ShowWindow(hWnd, Windowing.WindowShowStyle.SW_RESTORE);
		}

	}
}