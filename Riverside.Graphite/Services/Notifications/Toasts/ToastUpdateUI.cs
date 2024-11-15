using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Riverside.Graphite.Services.Notifications.Interfaces;
using System;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Notifications.Toasts
{
	public class ToastUpdateUI : IToast
	{
		public NotificationMessenger NotificationMessenger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public DateTimeOffset ExpirationTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public AppNotification Notification { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public DispatcherQueue DispatcherQueue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public Task InitializeAsync(DispatcherQueue dispatcher)
		{
			throw new NotImplementedException();
		}

		public Task NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			throw new NotImplementedException();
		}

		public Task<bool> SendToast()
		{
			throw new NotImplementedException();
		}
	}
}
