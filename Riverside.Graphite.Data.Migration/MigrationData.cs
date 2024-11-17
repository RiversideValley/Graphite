using Riverside.Graphite.Data.Core.Models;
using System.Collections.Generic;

namespace Riverside.Graphite.Data.Migration;

public class MigrationData
{
	//make this cookies history and favorites
	public List<DbHistoryItem> History { get; set; }
}