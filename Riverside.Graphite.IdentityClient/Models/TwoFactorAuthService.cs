using Microsoft.UI.Xaml;
using Riverside.Graphite.IdentityClient.Controls;
using Riverside.Graphite.IdentityClient.Helpers;
using Riverside.Graphite.IdentityClient.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.IdentityClient.Services
{
	public class TwoFactorAuthService
	{
		private readonly TwoFactorAuthManager _manager;
		private readonly DispatcherTimer _updateTimer;
		public ObservableCollection<TwoFactorAuthViewModel> AuthItems { get; }

		public TwoFactorAuthService(string userDataPath, string username)
		{
			string filePath = Path.Combine(userDataPath, "Users", username, "Database", "2fa.json");
			_manager = new TwoFactorAuthManager(filePath);
			AuthItems = new ObservableCollection<TwoFactorAuthViewModel>();

			_updateTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			_updateTimer.Tick += UpdateCodes;
		}

		public async Task InitializeAsync()
		{
			await _manager.LoadAsync();
			foreach (var item in _manager.Items)
			{
				AuthItems.Add(new TwoFactorAuthViewModel(item, this));
			}
			_updateTimer.Start();
		}

		public async Task AddItemAsync(string name, string secret, string issuer)
		{
			var newItem = new TwoFactorAuthItem
			{
				Name = name,
				Secret = Base32Encoding.ToBytes(secret),
				Issuer = issuer
			};

			_manager.Add(newItem);
			AuthItems.Add(new TwoFactorAuthViewModel(newItem, this));
			await _manager.SaveAsync();
		}

		public async Task RemoveItemAsync(TwoFactorAuthViewModel item)
		{
			AuthItems.Remove(item);
			_manager.Remove(item.Item);
			await _manager.SaveAsync();
		}

		private void UpdateCodes(object sender, object e)
		{
			foreach (var item in AuthItems)
			{
				item.UpdateCodeAndProgress();
			}
		}

		public void ShowFlyout(FrameworkElement element)
		{
			Two2FAFlyout flyout = new Two2FAFlyout(this);
			flyout.ShowAt(element);
		}
	}
}

