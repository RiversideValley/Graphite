using Microsoft.UI.Xaml;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services.ViewModels;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Riverside.Graphite.Pages.Patch
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class UpLoadBackup : Window
	{
		public UploadBackupViewModel ViewModel { get; }
		public static UpLoadBackup Instance { get; set; }

		private readonly WindowBounce windowBounce;
		public UpLoadBackup()
		{
			Instance = this;
			ViewModel = new();
			InitializeComponent();
			windowBounce = new WindowBounce(this);
			_ = windowBounce.ShowWindowBounce().ConfigureAwait(false);
		}

		private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			DataPackage dataPackage = new();
			dataPackage.SetText(ViewModel.FileSelected.BlobUrl);
			Clipboard.SetContent(dataPackage);
		}
	}
}
