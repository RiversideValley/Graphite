using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.ViewModels.Interfaces;

public interface IDownloadsViewModel
{
	ListView DownloadItemsList { get; }
	DownloadService DataCore { get; }
	ObservableCollection<DownloadItem> ItemsListView { get; set; }
	Task GetDownloadItems();
}