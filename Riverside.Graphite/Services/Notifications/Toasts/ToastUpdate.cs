using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services.Messages;
using System;
using System.Collections.ObjectModel;
using WinRT.Interop;

namespace Riverside.Graphite.Services.Notifications.Toasts
{
	public class ToastUpdate
	{
		public const string Title = "Graphite Browser Notifications";

		public static ObservableCollection<FireNotification> NotificationMessages = new();

		public static bool SendToast()
		{
			AppNotification appNotification = new AppNotificationBuilder()
				.AddArgument("action", "ToastClick")
				.AddArgument(((int)EnumMessageStatus.Updated).ToString(), "UserStatus")
				.SetAppLogoOverride(new Uri("file://" + App.GetFullPathToAsset("fire_globe1.png")), AppNotificationImageCrop.Circle)
				.AddText(Title)
				.AddText("Please update the app to enjoy the latest features and improvements.")
				.AddButton(new AppNotificationButton("Check for Update")
							.AddArgument("action", "UpdateApp")
							.SetButtonStyle(AppNotificationButtonStyle.Default)
							.AddArgument(((int)EnumMessageStatus.Updated).ToString(), "UserStatus"))
				.BuildNotification();

			AppNotificationManager.Default.Show(appNotification);

			return appNotification.Id != 0; // return true (indicating success) if the toast was sent (if it has an Id)
		}

		public static async void NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			NotificationMessenger noteMsg = new(ref NotificationMessages);
			FireNotification notification = new();
			notification.Originator = Title;
			notification.Action = notificationActivatedEventArgs.Arguments["action"];
			await noteMsg.InstallUpdatesAsync(notification);
			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				nint hWnd = WindowNative.GetWindowHandle(window);
				_ = Windowing.ShowWindow(hWnd, Windowing.WindowShowStyle.SW_RESTORE);
			}
		}
	}
}