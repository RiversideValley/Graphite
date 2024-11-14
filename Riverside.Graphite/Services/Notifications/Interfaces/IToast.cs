﻿using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Graph.Models;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Notifications.Interfaces
{
	public interface IToast
	{
		public string Title => "Fire Browser Notifications";
		public static ObservableCollection<string> NotificationMessages { get; }
		public Task InitializeAsync(DispatcherQueue dispatcher);
		public Task<bool> SendToast();

		public Task NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs);

		
		public NotificationMessenger NotificationMessenger { get; set;  }

		public DateTimeOffset ExpirationTime { get; set; }	
		public abstract AppNotification Notification { get; set; }
		public abstract DispatcherQueue DispatcherQueue { get; set; }


	}
}
