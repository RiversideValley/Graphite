using Riverside.Graphite.Data.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions.Contracts
{
	public interface ICollectionNames
	{
		Task InsertCollectionName(string NameOfCollection);
		Task DeleteCollectionNamesItem(int Id);
		Task DeleteAllCollectionNamesItems();
		Task<ObservableCollection<CollectionName>> GetAllCollectionNamesItems();

	}
}
