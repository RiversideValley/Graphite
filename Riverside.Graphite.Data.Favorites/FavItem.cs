using System.ComponentModel;

namespace Riverside.Graphite.Data.Favorites;
public class FavItem : INotifyPropertyChanged
{
	public string Title { get; set; }
	public string Url { get; set; }
	public string IconUrlPath { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;
}