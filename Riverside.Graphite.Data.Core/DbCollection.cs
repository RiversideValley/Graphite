// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;

namespace Riverside.Graphite.Data.Core
{
	public class DbCollection
	{

		public int HistoryItemId { get; set; }	

		// Graphite.HistoryItem
		public int Id { get; set; }
		public string Collection_Name { get; set; }
		public DateTimeOffset DateTime_Created { get; set; }

		public DbCollection() { }	


	}
}