using Microsoft.UI.Xaml.Controls;
using Graphite.ViewModels;

namespace Riverside.Graphite.Controls;

public class Passer
{
	public GraphiteTabViewItem Tab { get; set; }
	public TabView TabView { get; set; }
	public ToolbarViewModel ViewModel { get; set; }
	public object Param { get; set; }
}


