using Riverside.Graphite.Data.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions.Contracts
{
	public interface IHistoryActions
	{
		Task InsertHistoryItem(string url, string title, int visitCount, int typedCount, int hidden);
		Task DeleteHistoryItem(string url);
		Task DeleteAllHistoryItems();
		Task<ObservableCollection<HistoryItem>> GetAllHistoryItems();
		HistoryContext HistoryContext { get; }
	}
}
