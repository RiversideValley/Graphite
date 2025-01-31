using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Graphite.Controls;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GraphiteTabViewContainer : TabView
{
	public GraphiteTabViewContainer()
	{
		InitializeComponent();
		
	}

	public FireBrowserTabViewViewModel ViewModel { get; set; }

	public partial class FireBrowserTabViewViewModel : ObservableObject
	{
		[ObservableProperty]
		private Style style;
	}



	//private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	//{
	//	// Optional: Handle selection changes between tabs.
	//}

	//public void AddTab(FireBrowserTabViewItem tabViewItem)
	//{
	//	if (tabViewItem != null)
	//	{
	//		tabViewItem.PointerEntered += TabItem_PointerEntered;
	//		tabViewItem.PointerExited += TabItem_PointerExited;
	//	}
	//}

	//private void TabItem_PointerEntered(object sender, RoutedEventArgs e)
	//{
	//	if (IsPreviewEnabled && sender is FireBrowserTabViewItem tabViewItem)
	//	{
	//		tabViewItem.ShowPreview();
	//	}
	//}

	//private void TabItem_PointerExited(object sender, RoutedEventArgs e)
	//{
	//	if (sender is FireBrowserTabViewItem tabViewItem)
	//	{
	//		tabViewItem.HidePreview();
	//	}
	//}
}
