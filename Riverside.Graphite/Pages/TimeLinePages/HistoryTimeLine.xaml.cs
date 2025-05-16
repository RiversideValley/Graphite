using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Services.ViewModels;


namespace Riverside.Graphite.Pages.TimeLinePages;
public sealed partial class HistoryTimeLine : Page
{

	//public IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem> _browserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(new BrowserHistoryCollection());

	public HistoryViewModel ViewModel { get; set; }
	public HistoryTimeLine()
	{
		ViewModel = App.GetService<HistoryViewModel>();
		Loaded += (s, e) => ViewModel.FetchBrowserHistory();
		InitializeComponent();
		ViewModel.ParentHistoryTimeLine = this;
	}

}
