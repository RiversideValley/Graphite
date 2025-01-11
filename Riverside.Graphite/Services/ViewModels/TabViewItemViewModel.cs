using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.CompilerServices;

namespace Riverside.Graphite.ViewModels;
public partial class TabViewItemViewModel : ObservableObject
{
	[ObservableProperty] public bool _IsTooltipEnabled;
	[ObservableProperty] public BitmapImage _webPreview;
	[ObservableProperty] public string _webTitle;
	[ObservableProperty] public string _webAddress; 
	[ObservableProperty] public BitmapImage _IconImage;

	public void RaisePropertyChange([CallerMemberName] string callerMemberName = null)
	{
		OnPropertyChanged(callerMemberName);
	}
}