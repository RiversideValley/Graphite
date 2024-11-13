﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FireBrowserWinUi3.Services.Messages;
using FireBrowserWinUi3.Services.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace FireBrowserWinUi3.Services
{
	public partial class NotificationManager : ObservableRecipient
	{
		private bool m_isRegistered;

		private Dictionary<int, Action<AppNotificationActivatedEventArgs>> c_notificationHandlers;

		public NotificationManager() { }
		public NotificationManager(IMessenger messenger) : base(messenger)
		{
			m_isRegistered = false;

			// When adding new a scenario, be sure to add its notification handler here.
			c_notificationHandlers = new Dictionary<int, Action<AppNotificationActivatedEventArgs>>
			{
				{ (int)EnumMessageStatus.Added, ToastWithAvatar.NotificationReceived },
				{ (int)EnumMessageStatus.Login, ToastWithTextBox.NotificationReceived }
			};
		}

		~NotificationManager()
		{
			Unregister();
		}

		public void Init()
		{
			var notificationManager = AppNotificationManager.Default;

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
			DispatchNotification(notificationActivatedEventArgs);
			Messenger.Send(new Message_Settings_Actions("Application launched by notification", EnumMessageStatus.Informational));
		}

		public bool DispatchNotification(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			//var scenarioId = notificationActivatedEventArgs.Arguments[Common.scenarioTag];
			//if (scenarioId.Length != 0)
			//{
			//	try
			//	{
			//		c_notificationHandlers[int.Parse(scenarioId)](notificationActivatedEventArgs);
			//		return true;
			//	}
			//	catch
			//	{
			//		return false; // Couldn't find a NotificationHandler for scenarioId.
			//	}
			//}
			//else
			//{
			//	return false; // No scenario specified in the notification
			//}
			return false;
		}

		void OnNotificationInvoked(object sender, AppNotificationActivatedEventArgs notificationActivatedEventArgs)
		{
			//NotifyUser.NotificationReceived();

			//if (!DispatchNotification(notificationActivatedEventArgs))
			//{
			//	NotifyUser.UnrecognizedToastOriginator();
			//}
		}
	}
}