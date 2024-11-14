using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.System;

namespace Riverside.Graphite.Runtime.Licensing
{
	public class AddonManager
	{
		private StoreContext _storeContext;
		private const string AddOnStoreId = "9NL6FM9S0DHV";
		private bool _isInitialized = false;

		public AddonManager()
		{
		}

		public void Initialize(Window window)
		{
			if (_isInitialized)
			{
				return;
			}

			_storeContext = StoreContext.GetDefault();
			_isInitialized = true;
		}

		private void EnsureInitialized()
		{
			if (!_isInitialized)
			{
				throw new InvalidOperationException("AddonManager is not initialized. Call Initialize first.");
			}
		}

		public async Task<bool> PurchaseAddonAsync()
		{
			EnsureInitialized();

			try
			{
				// First check if we already own the add-on
				var appLicense = await _storeContext.GetAppLicenseAsync();
				if (appLicense.AddOnLicenses.TryGetValue(AddOnStoreId, out var addOnLicense) && addOnLicense.IsActive)
				{
					return true;
				}

				// Open the Store page for the add-on
				await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?ProductId={AddOnStoreId}"));

				// Wait for a short period to allow the user to complete the purchase
				await Task.Delay(TimeSpan.FromSeconds(30));

				// Check again if the add-on is now owned
				appLicense = await _storeContext.GetAppLicenseAsync();
				return appLicense.AddOnLicenses.TryGetValue(AddOnStoreId, out addOnLicense) && addOnLicense.IsActive;
			}
			catch (Exception ex)
			{
				throw new Exception($"Purchase failed: {ex.Message}", ex);
			}
		}

		public async Task<bool> IsSubscriptionActive()
		{
			EnsureInitialized();

			try
			{
				var appLicense = await _storeContext.GetAppLicenseAsync();
				return appLicense.AddOnLicenses.TryGetValue(AddOnStoreId, out var addOnLicense) && addOnLicense.IsActive;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}