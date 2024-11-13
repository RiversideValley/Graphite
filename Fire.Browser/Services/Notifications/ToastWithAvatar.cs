﻿using Fire.Browser.Core;
using Fire.Core.Helpers;
using FireBrowserWinUi3;
using FireBrowserWinUi3.Services.Messages;
using FireBrowserWinUi3.Services.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.ObjectModel;
using WinRT.Interop;
using static FireBrowserWinUi3.Services.Notifications.NotificationMessage;

namespace FireBrowserWinUi3.Services.Notifications;

class ToastWithAvatar
{

	public const string Title = "Fire Browser Notifications";
	public static ObservableCollection<string> NotificationMessages = new ObservableCollection<string>();
	public static bool SendToast()
	{
		var appNotification = new AppNotificationBuilder()
			.AddArgument("action", "ToastClick")
			.AddArgument(((int)(EnumMessageStatus.Informational)).ToString(), "UserStatus")
			.SetAppLogoOverride(new System.Uri("file://" + App.GetFullPathToAsset("favicon.png")), AppNotificationImageCrop.Circle)
			.AddText(Title)
			.AddText($"Welcome to your Fire Browser '{AuthService.CurrentUser.Username ?? "?"}'")
			.AddButton(new AppNotificationButton("Open App")
				.AddArgument("action", "OpenApp")
				.AddArgument(((int)(EnumMessageStatus.Informational)).ToString(), "UserStatus"))
			.BuildNotification();

		AppNotificationManager.Default.Show(appNotification);

		return appNotification.Id != 0; // return true (indicating success) if the toast was sent (if it has an Id)
	}

	public static void NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
	{
		var noteMsg = new NotificationMessage(ref NotificationMessages);
		var notification = new FireNotification();
		notification.Originator = Title;
		notification.Action = notificationActivatedEventArgs.Arguments["action"];
		noteMsg.NotificationReceived(notification);
		if (Application.Current is App app && app.m_window is MainWindow window)
		{
			IntPtr hWnd = WindowNative.GetWindowHandle(window);
			Windowing.ShowWindow(hWnd, Windowing.WindowShowStyle.SW_RESTORE);
		}

	}
}