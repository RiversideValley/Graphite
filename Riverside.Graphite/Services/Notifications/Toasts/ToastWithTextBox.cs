using Riverside.Graphite.Core;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers;
using FireBrowserWinUi3.Services.Messages;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.ObjectModel;
using WinRT.Interop;
using static FireBrowserWinUi3.Services.Notifications.NotificationMessenger;



namespace FireBrowserWinUi3.Services.Notifications.Toasts;
public class ToastWithTextBox
{

	public const string Title = "Riverside.Graphite Notifications";
	const string textboxReplyId = "textboxReply";

	public static ObservableCollection<FireNotification> NotificationMessages = new(); 

	public static bool SendToast()
	{
		var appNotification = new AppNotificationBuilder()
			.AddArgument("action", "ToastClick")
			.AddArgument(((int)EnumMessageStatus.Informational).ToString(), "UserStatus")
			.SetAppLogoOverride(new Uri("file://" + App.GetFullPathToAsset("favicon.png")), AppNotificationImageCrop.Circle)
			.AddText(Title)
			.AddText("This is an example message using XML")
			.AddTextBox(textboxReplyId, "Type a reply", "Reply box")
			.AddButton(new AppNotificationButton("Reply")
				.AddArgument("action", "Reply")
				.AddArgument(((int)EnumMessageStatus.Informational).ToString(), "UserStatus")
				.SetInputId(textboxReplyId))
			.BuildNotification();

		AppNotificationManager.Default.Show(appNotification);

		return appNotification.Id != 0; // return true (indicating success) if the toast was sent (if it has an Id)
	}

	public static void NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
	{
		// In a real-life scenario, this type of action would usually be processed in the background. Your App would process the payload in
		// the background (sending the payload back to your App Server) without ever showing the App's UI.
		// This is not something that can easily be demonstrated in a sample such as this one, as we need to show the UI to demonstrate how
		// the payload is routed internally
		var noteMsg = new NotificationMessenger(ref NotificationMessages);
		var notification = new FireNotification();

		notification.Originator = Title;
		notification.Action = notificationActivatedEventArgs.Arguments["action"];
		notification.HasInput = true;
		notification.Input = notificationActivatedEventArgs.UserInput[textboxReplyId];
		noteMsg.NotificationReceived(notification);
		if (Application.Current is App app && app.m_window is MainWindow window)
		{
			nint hWnd = WindowNative.GetWindowHandle(window);
			Windowing.ShowWindow(hWnd, Windowing.WindowShowStyle.SW_RESTORE);
		}
	}
}