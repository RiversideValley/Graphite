using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts;

public interface IServiceDownloads
{
    ObservableCollection<FireBrowserWinUi3.Controls.DownloadItem> DownloadItemControls { get; }
    Task SaveAsync(string fileName);
    Task<bool> DeleteAsync(string filePath);
    Task UpdateAsync();
    Task InsertAsync(string currentPath, string endTime, long startTime);
}