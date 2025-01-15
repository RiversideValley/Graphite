// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.ComponentModel.DataAnnotations;

namespace Riverside.Graphite.Data.Core.Models
{
    public class DbCollection
    {
        public int HistoryItemId { get; set; }

		// Graphite.HistoryItem
		[Key]
        public int Id { get; set; }
        public string Collection_Name { get; set; }
        public DateTimeOffset DateTime_Created { get; set; }

        public DbCollection() { }

        public DbCollection(int historyItemId, int id, string collectionName, DateTimeOffset dateTimeCreated)
        {
            HistoryItemId = historyItemId;
            Id = id;
            Collection_Name = collectionName;
            DateTime_Created = dateTimeCreated;
        }
    }
}