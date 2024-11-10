using Fire.Authentication.Dialogs;
using Fire.Authentication.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fire.Authentication.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Two2FAFlyout : Flyout
	{
		public Two2FAFlyout()
		{
			this.InitializeComponent();
			list.ItemsSource = null;
			list.ItemsSource = TwoFactorsAuthentification.Items;
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			AddDialog addDialog = new();
			addDialog.XamlRoot = this.XamlRoot;
			await addDialog.ShowAsync();
		}

		private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			ListViewItem item = sender as ListViewItem;
			TwoFactAuth twoFactAuth = item.DataContext as TwoFactAuth;

			var package = new DataPackage();
			package.SetText(twoFactAuth.Code);
			Clipboard.SetContent(package);
		}

		private async void Repair_Click(object sender, RoutedEventArgs e)
		{
			await Fire.Core.Helpers.TwoFactorsAuthentification.Repair();
		}
	}
}
