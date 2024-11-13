using FireBrowserWinUi3.Pages;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Notifications
{
	public class NotificationMessage
	{
		private readonly NotificationManager _notificationManager;
		private ObservableCollection<string> messages;

		public ObservableCollection<string> PublicMessages => messages;

		public NotificationMessage() { }
		public NotificationMessage(NotificationManager manager, ref ObservableCollection<string> messagesPage)
		{

			_notificationManager = manager;
			messages = messagesPage;
		}


		public struct FireNotification
		{
			public string Originator;
			public string Action;
			public bool HasInput;
			public string Input;
		};

		public void NotificationReceived(FireNotification notification)
		{
			var text = notification.Originator;

			text += "\t- Action: " + notification.Action;

			if (notification.HasInput)
			{
				if (notification.Input.Length == 0)
				{
					text += "\t- No input received";
				}
				else
				{
					text += "\t- Input received: " + notification.Input;
				}
			}

			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				if (window.DispatcherQueue.HasThreadAccess)
				{
					messages.Insert(0, text);
				}
				else
				{
					_ = window.DispatcherQueue.TryEnqueue(() =>
					{
						messages.Insert(0, text);
					});
				}

			}
		}
	}
}
