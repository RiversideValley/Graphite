using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.AppNotifications;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.Services.Notifications.Toasts;
using System;
using System.Collections.Generic;

namespace Riverside.Graphite.Services
{
	public sealed class NotificationManager : ObservableRecipient
	{
		private bool m_isRegistered;

		private readonly Dictionary<int, Action<AppNotificationActivatedEventArgs>> c_notificationHandlers;

		public NotificationManager()
		{
			m_isRegistered = false;

			// When adding new a scenario, be sure to add its notification handler here.
			c_notificationHandlers = new Dictionary<int, Action<AppNotificationActivatedEventArgs>>
			{
				{ (int)EnumMessageStatus.Informational, ToastRatings.NotificationReceived },
				{ (int)EnumMessageStatus.Login, ToastWithTextBox.NotificationReceived },
				{ (int)EnumMessageStatus.Updated, ToastUpdate.NotificationReceived },
			};
		}

		~NotificationManager()
		{
			Unregister();
		}

		public void Init()
		{
			AppNotificationManager notificationManager = AppNotificationManager.Default;

			// To ensure all Notification handling happens in this process instance, register for
			// NotificationInvoked before calling Register(). Without this a new process will
			// be launched to handle the notification.
			notificationManager.NotificationInvoked += OnNotificationInvoked;

			notificationManager.Register();
			m_isRegistered = true;
		}

		public void Unregister()
		{
			if (m_isRegistered)
			{
				AppNotificationManager.Default.Unregister();
				m_isRegistered = false;
			}
		}

		public void ProcessLaunchActivationArgs(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			_ = DispatchNotification(notificationActivatedEventArgs);
			_ = Messenger.Send(new Message_Settings_Actions("Application launched by notification", EnumMessageStatus.Informational));
		}

		public bool DispatchNotification(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			{
				try
				{
					IDictionary<string, string> arguments = notificationActivatedEventArgs.Arguments;

					if (arguments.ContainsKey("action") && arguments["action"] == "UpdateApp")
					{
						c_notificationHandlers[(int)EnumMessageStatus.Updated](notificationActivatedEventArgs);
					}
					else if (arguments.ContainsKey("action") && arguments["action"] == "RateApp")
					{
						c_notificationHandlers[(int)EnumMessageStatus.Informational](notificationActivatedEventArgs);
					}

					return true;
				}
				catch (Exception ex)
				{
					ExceptionLogger.LogException(ex);
					return false; // Couldn't find a NotificationHandler for scenarioId.
				}
			}
		}

		private void OnNotificationInvoked(object sender, AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			if (!DispatchNotification(notificationActivatedEventArgs))
			{
				Console.WriteLine("Unregisterd author of notifications");
			}
		}
	}
}