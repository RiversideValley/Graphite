using CommunityToolkit.Mvvm.ComponentModel;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Services.ViewModels.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.ViewModels;

public class DownloadsViewModel : ObservableObject, IDownloadsViewModel
{
	public ListView DownloadItemsList { get; set; }
	public DownloadService DataCore { get; }
	public ObservableCollection<DownloadItem> ItemsListView { get; set; }

	public DownloadsViewModel()
	{
		DataCore = App.GetService<DownloadService>();
		DataCore.Handler_DownItemsChange += DataCore_Handler_DownItemsChange;
	}

	private async void DataCore_Handler_DownItemsChange(object sender, FireBrowserWinUi3.Services.Events.DownloadItemStatusEventArgs e)
	{
		ItemsListView = DataCore.DownloadItemControls;
		OnPropertyChanged(nameof(ItemsListView));
		await Task.Delay(200);
	}

	public async Task GetDownloadItems()
	{
		if (DataCore == null) return;

		await DataCore.UpdateAsync();
		ItemsListView = DataCore.DownloadItemControls;
		OnPropertyChanged(nameof(ItemsListView));
	}
}