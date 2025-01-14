using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite.Pages;
using Riverside.Graphite.ViewModels;
using Riverside.Graphite;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;
using System;
using Riverside.Graphite.Data.Favorites;
using CommunityToolkit.WinUI.Behaviors;

namespace Graphite.Controls;
public sealed partial class FireBrowserTabViewItem : TabViewItem
{
	public FireBrowserTabViewItem() => InitializeComponent();

	public TabViewItemViewModel ViewModel { get; set; } = new TabViewItemViewModel() { IsTooltipEnabled = default };

	public BitmapImage BitViewWebContent { get; set; }
	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public static DependencyProperty ValueProperty = DependencyProperty.Register(
	nameof(Value),
	typeof(string),
	typeof(FireBrowserTabViewItem),
	null);


	private async void TabViewItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		MainWindow win = (Window)(Application.Current as App).m_window as MainWindow;
		var viewTab = (sender as FireBrowserTabViewItem);

		//if ((sender as FireBrowserTabViewItem).IsSelected)
		//	if (win?.TabViewContainer.SelectedItem is FireBrowserTabViewItem tab)
		//	{

		if (viewTab.Content is Frame frame)
		{
			if (frame.Content is WebContent web) {

				if (web.PictureWebElement is BitmapImage)
				{
					// get preview from webcontent corewebView2 apis
					if (!viewTab.IsSelected)
						ViewModel.WebPreview =  web.PictureWebElement;
				}

				ViewModel.WebTitle = web.WebView.CoreWebView2?.DocumentTitle;

				BitmapImage bitmapImage = new();
				IRandomAccessStream stream = await web.WebView.CoreWebView2?.GetFaviconAsync(Microsoft.Web.WebView2.Core.CoreWebView2FaviconImageFormat.Png); ;
				ImageIconSource iconSource = new() { ImageSource = bitmapImage };
				await bitmapImage.SetSourceAsync(stream ?? await web.WebView.CoreWebView2?.GetFaviconAsync(Microsoft.Web.WebView2.Core.CoreWebView2FaviconImageFormat.Png));

				ViewModel.IconImage = bitmapImage;
				ViewModel.IsTooltipEnabled = true;
				ViewModel.WebAddress = web.WebView.CoreWebView2?.Source.ToLower();
				// raise enable prop hence page is two-way bindings. 
				ViewModel.RaisePropertyChange(nameof(ViewModel.IsTooltipEnabled));
				await Task.Delay(100);
				
			}			
		}

		e.Handled = true; 
	}

}