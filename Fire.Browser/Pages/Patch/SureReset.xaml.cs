using Microsoft.UI.Xaml.Controls;
using System.IO;

namespace FireBrowserWinUi3.Pages.Patch
{
	public sealed partial class SureReset : ContentDialog
	{
		public SureReset()
		{
			InitializeComponent();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			string resetFilePath = Path.Combine(Path.GetTempPath(), "Reset.set");
			// Write some content to the file (or leave it empty)
			File.WriteAllText(resetFilePath, "True");
			// Restart the app
			_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
		}



		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			Hide();
		}
	}
}
