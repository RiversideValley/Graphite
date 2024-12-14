using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.IdentityClient.Helpers;
using Riverside.Graphite.IdentityClient.Services;
using Riverside.Graphite.IdentityClient.Utils;
using Windows.ApplicationModel.DataTransfer;

namespace Riverside.Graphite.IdentityClient.Dialogs
{
	public sealed partial class AddDialog : ContentDialog
	{
		private readonly TwoFactorAuthService _authService;

		public string ItemName { get; private set; }
		public string ItemSecret { get; private set; }
		public string ItemIssuer { get; private set; }

		public AddDialog(TwoFactorAuthService authService)
		{
			InitializeComponent();
			_authService = authService;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		private async void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			ItemSecret = secretBox.Text.Replace(" ", "").Replace("-", "");
			ItemName = nameBox.Text;
			ItemIssuer = issuerBox.Text;

			if (!string.IsNullOrEmpty(ItemSecret))
			{
				await _authService.AddItemAsync(ItemName, ItemSecret, ItemIssuer);
			}

			Hide();
		}

		private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
		{
			string secret = secretBox.Text.Replace(" ", "").Replace("-", "");

			if (!string.IsNullOrEmpty(secret))
			{
				Totp totp = new(Base32Encoding.ToBytes(secret));
				string code = totp.ComputeTotp();

				DataPackage package = new();
				package.SetText(code);
				Clipboard.SetContent(package);
			}
		}
	}
}

