using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FireBrowserWinUi3DataCore.Actions.Contracts
{
    public interface IHistoryActions
    {
        Task InsertHistoryItem(string url, string title, int visitCount, int typedCount, int hidden);
        Task DeleteHistoryItem(string url);
        Task DeleteAllHistoryItems();
        Task<ObservableCollection<FireBrowserDatabase.HistoryItem>> GetAllHistoryItems();
        HistoryContext HistoryContext { get; }

    }
}
