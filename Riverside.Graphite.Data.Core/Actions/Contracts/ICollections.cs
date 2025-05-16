using Riverside.Graphite.Data.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions.Contracts
{
	public interface ICollections
	{
		Task<bool> InsertCollectionsItem(HistoryItem historyItem, CollectionName collectionName);
		Task DeleteCollectionsItem(int Id);
		Task DeleteAllCollectionsItems();
		Task<ObservableCollection<Collection>> GetAllCollectionsItems();

	}
}
