using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Contracts;

public interface IServiceDownloads
{
	ObservableCollection<Riverside.Graphite.Controls.DownloadItem> DownloadItemControls { get; }
	Task SaveAsync(string fileName);
	Task<bool> DeleteAsync(string filePath);
	Task UpdateAsync();
	Task InsertAsync(string currentPath, string endTime, long startTime);
}