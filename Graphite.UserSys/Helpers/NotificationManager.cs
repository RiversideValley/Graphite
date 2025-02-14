using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;

namespace Graphite.Helpers;

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
		try
		{
			IDictionary<string, string> arguments = notificationActivatedEventArgs.Arguments;

			if (arguments.ContainsKey("action"))
			{
				switch (arguments["action"])
				{
					case "UpdateApp":
						c_notificationHandlers[(int)EnumMessageStatus.Updated](notificationActivatedEventArgs);
						return true;
					case "RateApp":
						c_notificationHandlers[(int)EnumMessageStatus.Informational](notificationActivatedEventArgs);
						return true;
				}
			}

			// If we reach here, no matching action was found
			System.Diagnostics.Debug.WriteLine("No matching action found for the notification.");
			return false;
		}
		catch (Exception ex)
		{
			// Log the exception or handle it appropriately
			System.Diagnostics.Debug.WriteLine($"Error dispatching notification: {ex.Message}");
			return false;
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
