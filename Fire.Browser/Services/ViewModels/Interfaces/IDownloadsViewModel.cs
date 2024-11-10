using FireBrowserWinUi3.Controls;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.ViewModels.Interfaces;

public interface IDownloadsViewModel
{
    ListView DownloadItemsList { get; }
    DownloadService DataCore { get; }
    ObservableCollection<DownloadItem> ItemsListView { get; set; }
    Task GetDownloadItems();
}