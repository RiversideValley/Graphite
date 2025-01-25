using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Models
{
	public class CollectionGroup
	{
		public CollectionName CollectionName { get; set; }
		public ObservableCollection<Collection> Collections { get; set; }
	}
	
}
