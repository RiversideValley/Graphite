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

		public Two2FAFlyout(Riverside.Graphite.IdentityClient.Services.TwoFactorAuthService authService)
		{
			InitializeComponent();
			_authService = authService;
			list.ItemsSource = _authService.AuthItems;
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

		private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is ListViewItem item && item.DataContext is TwoFactorAuthViewModel twoFactAuth)
			{
				DataPackage package = new DataPackage();
				package.SetText(twoFactAuth.Code);
				Clipboard.SetContent(package);
			}
		}
	}
}

