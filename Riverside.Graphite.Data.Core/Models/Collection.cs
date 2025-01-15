using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Riverside.Graphite.Data.Core.Models
{
    public class Collection
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }

        [ForeignKey("HistoryItemId")]
        public int HistoryItemId { get; set; }  // Foreign Key for HistoryItem

        [ForeignKey("CollectionNameId")]
        public int CollectionNameId { get; set; } // Navigation Property for CollectionName
    }
}
