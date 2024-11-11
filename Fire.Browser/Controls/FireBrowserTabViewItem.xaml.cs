using FireBrowserWinUi3.Pages;
using FireBrowserWinUi3.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace FireBrowserWinUi3.Controls;
public sealed partial class FireBrowserTabViewItem : TabViewItem
{
	public FireBrowserTabViewItem()
	{
		InitializeComponent();
	}

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

	public WebView2 MainWebView
	{
		get;
		set;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void TabViewItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		MainWindow win = (Application.Current as App).m_window as MainWindow;

		if ((sender as FireBrowserWinUi3.Controls.FireBrowserTabViewItem).IsSelected)
		{
			if (win?.TabViewContainer.SelectedItem is FireBrowserTabViewItem)
			{
				if (win?.TabContent.Content is WebContent web)
				{
					if (web.PictureWebElement is not null)
					{
						ImgTabViewItem.Source = web.PictureWebElement;
						ImgTabViewHeader.Header = new TextBlock() { Text = web.WebView.CoreWebView2?.DocumentTitle, IsColorFontEnabled = true, FontSize = 12, MaxLines = 2, TextWrapping = TextWrapping.WrapWholeWords };
						ViewModel.IsTooltipEnabled = true;
					}

				}
			}
		}
	}
}