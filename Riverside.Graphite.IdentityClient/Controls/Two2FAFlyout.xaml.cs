using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Riverside.Graphite.IdentityClient.Dialogs;
using Riverside.Graphite.IdentityClient.Models;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace Riverside.Graphite.IdentityClient.Controls
{
	public sealed partial class Two2FAFlyout : Flyout
	{
		private readonly Riverside.Graphite.IdentityClient.Services.TwoFactorAuthService _authService;
		private DispatcherTimer _timer;

		public Two2FAFlyout(Riverside.Graphite.IdentityClient.Services.TwoFactorAuthService authService)
		{
			InitializeComponent();
			_authService = authService;
			list.ItemsSource = _authService.AuthItems;

			// Initialize and start the timer
			_timer = new DispatcherTimer();
			_timer.Interval = TimeSpan.FromSeconds(1);
			_timer.Tick += Timer_Tick;
			_timer.Start();
		}

		private void Timer_Tick(object sender, object e)
		{
			// Update all items in the list
			foreach (var item in _authService.AuthItems)
			{
				item.UpdateCodeAndProgress();
			}
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			AddDialog addDialog = new AddDialog(_authService);
			addDialog.XamlRoot = XamlRoot;
			ContentDialogResult result = await addDialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				string name = addDialog.ItemName;
				string secret = addDialog.ItemSecret;
				string issuer = addDialog.ItemIssuer;

				await _authService.AddItemAsync(name, secret, issuer);
			}
		}
	}
}

