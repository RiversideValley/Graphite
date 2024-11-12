using Fire.Authentication.Private;
using Fire.Authentication.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fire.Authentication.Dialogs
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class AddDialog : ContentDialog
	{
		public AddDialog()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			secretBox.Text = secretBox.Text.Replace(" ", "");
			secretBox.Text = secretBox.Text.Replace("-", "");

			if (secretBox.Text.Length > 0) // 0 for testing purposes
			{
				TwoFactorsAuthentification.Add(nameBox.Text, secretBox.Text);
			}

			Hide();
		}

		private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
		{
			secretBox.Text = secretBox.Text.Replace(" ", "");
			secretBox.Text = secretBox.Text.Replace("-", "");

			if (secretBox.Text.Length > 0) // For testing purpose (the normal min value should be 16)
			{
				Totp totp = new(Base32Encoding.ToBytes(secretBox.Text));
				string code = totp.ComputeTotp();

				DataPackage package = new();
				package.SetText(code);
				Clipboard.SetContent(package);
			}

		}
	}
}
