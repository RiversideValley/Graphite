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
		Loaded += (s, e) => ViewModel.GetDownloadItems().ConfigureAwait(false);

		InitializeComponent();

	}

}