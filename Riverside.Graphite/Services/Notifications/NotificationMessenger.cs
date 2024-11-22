using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Riverside.Graphite.Services.Notifications
{
	public sealed class NotificationMessenger
	{
		private readonly ObservableCollection<FireNotification> messages;
		private StoreContext _storeContext;
		public ObservableCollection<FireNotification> PublicMessages => messages;

		public NotificationMessenger() { messages = new();  }
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

			if (_storeContext is null)
			{
				return;
			}

			IReadOnlyList<StorePackageUpdate> updates = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				if (updates.Count > 0)
				{
					StorePackageUpdateResult updateResult = await _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);

					if (updateResult.OverallState == StorePackageUpdateState.Completed)
					{
						// Inform the user that the update was successful
						window.NotificationQueue.Show("Update is Complete", 2000, "Fire Browser");
					}
					else
					{
						// Inform the user that the update failed
						window.NotificationQueue.Show("Update Failed\nThe app could not be updated. Please visit Microsoft Store Updates", 2000, "Fire Browser");
					}
				}
				else
				{
					// Inform the user that no updates are available
					window.NotificationQueue.Show("No Updates Available\nYou already have the lastest version of the App", 2000, "Fire Browser");

				}
			}

			NotificationReceived(notification);
		}

		public async Task<List<StoreProduct>> GetAssociatedStoreProductsAsync()
		{
			List<string> productKinds = new() { "Durable", "Consumable", "UnmanagedConsumable", "Subscription" };

			StoreProductQueryResult result = await _storeContext.GetAssociatedStoreProductsAsync(productKinds);

			if (result.ExtendedError != null)
			{
				// Handle error
				throw new Exception(result.ExtendedError.Message);
			}

			List<StoreProduct> products = new();

			foreach (StoreProduct product in result.Products.Values)
			{
				products.Add(product);
				Console.WriteLine($"Product ID: {product.StoreId}, Title: {product.Title}, Price: {product.Price.FormattedPrice}");
			}

			return products;
		}
		public async Task PromptUserToRateApp()
		{
			await InitializeStoreContext();

			if (_storeContext is null)
			{
				return;
			}

			WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.m_window as MainWindow));
			StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();

			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				if (result.Status == StoreRateAndReviewStatus.Succeeded)
				{
					if (result.WasUpdated)
					{
						// User has already rated the app
						_ = window.DispatcherQueue.TryEnqueue(() =>
						window.NotificationQueue.Show("Thanks for raing our Application", 2000, "Fire Browser"));
					}
					else
					{
						// User has rated the app for the first time
						_ = window.DispatcherQueue.TryEnqueue(() =>
						window.NotificationQueue.Show("Login to Microsoft and have ability to backup data to the cloud", 2000, "Fire Browser"));
					}
				}
				else if (result.Status == StoreRateAndReviewStatus.CanceledByUser)
				{
					// User canceled the rating request
					_ = window.DispatcherQueue.TryEnqueue(() =>
							window.NotificationQueue.Show("Operation was cancelled", 2000, "Fire Browser"));
				}
			}
		}
		public void NotificationReceived(FireNotification notification)
		{
			string text = notification.Originator;

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
