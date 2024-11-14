using CommunityToolkit.Mvvm.ComponentModel;

namespace Riverside.Graphite.ViewModels;
public partial class TabViewItemViewModel : ObservableObject
{
	[ObservableProperty] public bool _IsTooltipEnabled;
}