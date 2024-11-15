using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Services.ViewModels;

namespace Riverside.Graphite.Pages.TimeLinePages;
public sealed partial class DownloadsTimeLine : Page
{
	public DownloadsViewModel ViewModel { get; set; }
	public DownloadsTimeLine()
	{
		// ViewModel is attached to DownloadServices that control the listView.  
		ViewModel = App.GetService<DownloadsViewModel>();
		InitializeComponent();
		_ = ViewModel.GetDownloadItems().GetAwaiter();
	}

	private void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{

	}
}