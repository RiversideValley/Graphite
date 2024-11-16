using Microsoft.UI.Xaml.Media.Imaging;

namespace Riverside.Graphite.Data.Core.Models;
public class HistoryItem
{
	public int Id { get; set; }
	public string Url { get; set; }
	public string Title { get; set; }
	public int VisitCount { get; set; }
	public int TypedCount { get; set; }
	public int Hidden { get; set; }

	public string LastVisitTime { get; set; }

	public BitmapImage ImageSource { get; set; }
}