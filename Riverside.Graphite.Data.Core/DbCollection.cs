// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Riverside.Graphite.Data.Core
{
	public class Collection
	{
		[Key]
		public int Id { get; set; }

		public int CollectionNameId { get; set; }  // Foreign Key for CollectionName

		public DateTime CreatedDate { get; set; }

		public int HistoryItemId { get; set; }  // Foreign Key for HistoryItem

		[ForeignKey("HistoryItemId")]
		public HistoryItem HistoryItem { get; set; }

		[ForeignKey("CollectionNameId")]
		public CollectionName CollectionName { get; set; }  // Navigation Property for CollectionName
	}

	public class CollectionName
	{
		[Key]
		public int Id { get; set; }

		public string Name { get; set; }

		public ICollection<Collection> Collections { get; set; }
	}

}