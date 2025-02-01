using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.ViewModels;

public partial class ToolbarViewModel : ObservableObject
{
	[ObservableProperty] public bool canRefresh;
	[ObservableProperty] public bool canGoBack;
	[ObservableProperty] public bool canGoForward;
	[ObservableProperty] public string currentAddress;
	[ObservableProperty] public string securityIcon;
	[ObservableProperty] public string securityIcontext;
	[ObservableProperty] public string securitytext;
	[ObservableProperty] public string securitytype;
	[ObservableProperty] public Visibility homeButtonVisibility;

	public void UpdateNavigationState(bool canGoBack, bool canGoForward)
	{
	}
}
