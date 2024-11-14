using FireBrowserWinUi3.Pages;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;
using WinRT.Interop;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FireBrowserWinUi3.Services.Notifications
{
	public sealed class NotificationMessenger
	{
		private ObservableCollection<FireNotification> messages;
		private StoreContext _storeContext;
		public ObservableCollection<FireNotification> PublicMessages => messages;

		public NotificationMessenger(ref ObservableCollection<FireNotification> messagesPage) 
		{
			messages = messagesPage;
			_ = InitializeStoreContext();
			
		}
		private async Task InitializeStoreContext()
		{
			IReadOnlyList<Windows.System.User> users = await Windows.System.User.FindAllAsync();

			if (users != null && users.Count > 0)
			{
				Windows.System.User firstUser = users[0];
				_storeContext = StoreContext.GetForUser(firstUser);

			}
			else { _storeContext = StoreContext.GetDefault(); }


		}

		public async Task InstallUpdatesAsync(FireNotification notification)
		{

			await InitializeStoreContext();

			if (_storeContext is null) return;

			IReadOnlyList<StorePackageUpdate> updates = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();

			if (updates.Count > 0)
			{
				StorePackageUpdateResult updateResult = await _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);

				if (updateResult.OverallState == StorePackageUpdateState.Completed)
				{
					// Inform the user that the update was successful
					var toastContentBuilder = new AppNotificationBuilder()
						.AddText("Update Complete!")
						.AddText("The app has been updated successfully.");

					var toast = toastContentBuilder.BuildNotification();
					AppNotificationManager.Default.Show(toast);
				}
				else
				{
					// Inform the user that the update failed
					var toastContentBuilder = new AppNotificationBuilder()
						.AddText("Update Failed")
						.AddText("The app could not be updated. Please try again later.");

					var toast = toastContentBuilder.BuildNotification();
					AppNotificationManager.Default.Show(toast);
				}
			}
			else
			{
				// Inform the user that no updates are available
				var toastContentBuilder = new AppNotificationBuilder()
					.AddText("No Updates Available")
					.AddText("You already have the latest version of the app.");

				var toast = toastContentBuilder.BuildNotification();
				AppNotificationManager.Default.Show(toast);
			}

			NotificationReceived(notification);
		}
		

		public async Task PromptUserToRateApp(FireNotification notification)
		{
			await InitializeStoreContext();

			if (_storeContext is null) return;

			WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.m_window as MainWindow));
			StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();
			if (Application.Current is App app && app.m_window is MainWindow window)
			{

				if (result.Status == StoreRateAndReviewStatus.Succeeded)
				{
					if (result.WasUpdated)
					{
						// User has already rated the app
						window.DispatcherQueue.TryEnqueue(() =>
						window.NotificationQueue.Show("Thanks for raing our Application", 2000, "Fire Browser"));
					}
					else
					{
						// User has rated the app for the first time
						window.DispatcherQueue.TryEnqueue(() =>
						window.NotificationQueue.Show("Login to Microsoft and have ability to backup data to the cloud", 2000, "Fire Browser"));
					}


				}
				else if (result.Status == StoreRateAndReviewStatus.CanceledByUser)
				{
					// User canceled the rating request
					window.DispatcherQueue.TryEnqueue(() =>
							window.NotificationQueue.Show("Operation was cancelled", 2000, "Fire Browser"));

				}
			}

		}
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
					messages.Insert(0, notification);
				}
				else
				{
					_ = window.DispatcherQueue.TryEnqueue(() =>
					{
						messages.Insert(0, notification);
					});
				}

			}
		}
	}
}


