using System.Collections.ObjectModel;

namespace Riverside.Graphite.Data.Core.Models
{
	public class CollectionGroup
	{
		public CollectionName CollectionName { get; set; }
		public ObservableCollection<Collection> Collections { get; set; }
	}

}
