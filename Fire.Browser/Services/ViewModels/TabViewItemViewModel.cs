using CommunityToolkit.Mvvm.ComponentModel;

namespace FireBrowserWinUi3.ViewModels;
public partial class TabViewItemViewModel : ObservableObject
{
    [ObservableProperty] public bool _IsTooltipEnabled;
}