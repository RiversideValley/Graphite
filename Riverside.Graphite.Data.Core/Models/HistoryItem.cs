using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace Riverside.Graphite.Data.Core.Models;
public class HistoryItem
{
	public HistoryItem(HistoryItem historyItem)
	{
		Self = historyItem;
	}
	public HistoryItem() { }

	public int Id { get; set; }
	public string Url { get; set; }
	public string Title { get; set; }
	public int VisitCount { get; set; }
	public int TypedCount { get; set; }
	public int Hidden { get; set; }

	public string LastVisitTime { get; set; }

	[JsonIgnore]
	[NotMapped]
	public BitmapImage ImageSource { get; set; }

	[JsonIgnore]
	[NotMapped]
	public HistoryItem Self { get; }
	
}
