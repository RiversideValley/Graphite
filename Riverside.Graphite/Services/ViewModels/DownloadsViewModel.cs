using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.ViewModels.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.ViewModels;

public class DownloadsViewModel : ObservableRecipient, IDownloadsViewModel, INavigationAware
{
	public ListView DownloadItemsList { get; set; }
	public DownloadService DataCore { get; }
	public ObservableCollection<DownloadItem> ItemsListView { get; set; }

	public DownloadsViewModel()
	{
		DataCore = App.GetService<DownloadService>();
	}

	private async void DataCore_Handler_DownItemsChange(object sender, Riverside.Graphite.Services.Events.DownloadItemStatusEventArgs e)
	{
		ItemsListView = DataCore.DownloadItemControls;
		OnPropertyChanged(nameof(ItemsListView));
		await Task.CompletedTask;
	}

	public async Task GetDownloadItems()
	{
		if (DataCore == null)
		{
			return;
		}

		await DataCore.UpdateAsync();
		ItemsListView = DataCore.DownloadItemControls;
		OnPropertyChanged(nameof(ItemsListView));
	}

	public async void OnNavigatedTo(object parameter)
	{
		await GetDownloadItems();
		DataCore.Handler_DownItemsChange += DataCore_Handler_DownItemsChange;

	}

	public void OnNavigatedFrom()
	{
		DataCore.Handler_DownItemsChange -= DataCore_Handler_DownItemsChange;
	}
}