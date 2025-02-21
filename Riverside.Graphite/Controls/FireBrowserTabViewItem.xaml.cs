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
		if (sender == null || !(sender is FireBrowserTabViewItem viewTab))
		{
			return; // Do nothing if sender is null or not a FireBrowserTabViewItem
		}

		MainWindow win = (Window)(Application.Current as App)?.m_window as MainWindow;
		if (win == null)
		{
			return; // Do nothing if MainWindow is null
		}

		if (!(viewTab.Content is Frame frame) || !(frame.Content is WebContent web))
		{
			return; // Do nothing if Content is not Frame or Frame.Content is not WebContent
		}

		if (web.PictureWebElement is BitmapImage && !viewTab.IsSelected)
		{
			ViewModel.WebPreview = web.PictureWebElement;
		}

		if (web.WebView?.CoreWebView2 != null)
		{
			ViewModel.WebTitle = web.WebView.CoreWebView2.DocumentTitle;

			try
			{
				BitmapImage bitmapImage = new();
				using (IRandomAccessStream stream = await web.WebView.CoreWebView2.GetFaviconAsync(Microsoft.Web.WebView2.Core.CoreWebView2FaviconImageFormat.Png))
				{
					if (stream != null)
					{
						await bitmapImage.SetSourceAsync(stream);
						ViewModel.IconImage = bitmapImage;
					}
				}

				ViewModel.IsTooltipEnabled = true;
				ViewModel.WebAddress = web.WebView.CoreWebView2.Source?.ToLower();

				// Raise enable prop hence page is two-way bindings. 
				ViewModel.RaisePropertyChange(nameof(ViewModel.IsTooltipEnabled));
				await Task.Delay(100);
			}
			catch (Exception)
			{
				// Handle or log any exceptions that might occur during favicon retrieval
			}
		}

		e.Handled = true;
	}

}