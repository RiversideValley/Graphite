using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions.Contracts
{
	public  interface ICollections
	{
		Task InsertCollectionsItem(DateTime createdDate, HistoryItem historyItem, CollectionName collectionName);
		Task DeleteCollectionsItem(int Id);
		Task DeleteAllCollectionsItems();
		Task<ObservableCollection<Collection>> GetAllCollectionsItems();
		
	}
}
