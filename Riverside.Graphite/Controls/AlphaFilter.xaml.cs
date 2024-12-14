using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace Riverside.Graphite.Controls;
public sealed partial class AlphaFilter : Flyout
{
	public ObservableCollection<string> Letters { get; set; }
	public string SelectedLetter { get; set; }

	public AlphaFilter()
	{
		this.InitializeComponent();

		Letters = new ObservableCollection<string>(Enumerable.Range('A', 26).Select(i => ((char)i).ToString()).ToList());
		GridMain.ItemsSource = Letters;
	}

	private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		MainWindow currentWindow = (Application.Current as App)?.m_window as MainWindow;
		if (currentWindow != null && e.AddedItems.Count > 0)
		{
			currentWindow.FilterBrowserHistoryTitle(e.AddedItems.FirstOrDefault().ToString());
		}
		this.Hide();
	}

}

