using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite;
using Riverside.Graphite.Pages;
using Riverside.Graphite.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Graphite.Controls
{
	public sealed partial class GraphiteTabViewItem : TabViewItem
	{

		public TabViewItemViewModel ViewModel { get; set; } = new TabViewItemViewModel() { IsTooltipEnabled = default };

		public GraphiteTabViewItem()
		{
			InitializeComponent();
			InitializeContextMenu();
		}

		public BitmapImage BitViewWebContent { get; set; }

		public string Value
		{
			get => (string)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			nameof(Value),
			typeof(string),
			typeof(GraphiteTabViewItem),
			null);

		public bool IsPinned
		{
			get => (bool)GetValue(IsPinnedProperty);
			set => SetValue(IsPinnedProperty, value);
		}

		public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
			nameof(IsPinned),
			typeof(bool),
			typeof(GraphiteTabViewItem),
			new PropertyMetadata(false, OnIsPinnedChanged));

		private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is GraphiteTabViewItem tabItem)
			{
				tabItem.UpdatePinnedAppearance();
			}
		}

		private void UpdatePinnedAppearance()
		{
			if (IsPinned)
			{
				this.Style = (Style)Application.Current.Resources["PinnedTabStyle"];
			}
			else
			{
				this.Style = (Style)Application.Current.Resources["DefaultTabStyle"];
			}
			UpdatePinMenuItemText();
		}

		private void InitializeContextMenu()
		{
			var contextMenu = new MenuFlyout();

			var pinMenuItem = new MenuFlyoutItem
			{
				Text = "Pin",
				Icon = new SymbolIcon(Symbol.Pin)
			};
			pinMenuItem.Click += PinMenuItem_Click;

			contextMenu.Items.Add(pinMenuItem);

			this.ContextFlyout = contextMenu;
		}

		private void SleepMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// Implement sleep logic here or call a method in TabManager
		}

		private void PinMenuItem_Click(object sender, RoutedEventArgs e)
		{
			UpdatePinMenuItemText();
		}

		private void UpdatePinMenuItemText()
		{
			if (ContextFlyout is MenuFlyout contextMenu)
			{
				var pinMenuItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item => item.Text.StartsWith("Pin") || item.Text.StartsWith("Unpin"));
				if (pinMenuItem != null)
				{
					pinMenuItem.Text = IsPinned ? "Unpin" : "Pin";
				}
			}
		}

		public void UpdatePinStatus(bool isPinned)
		{
			IsPinned = isPinned;
			UpdatePinnedAppearance();
		}

		private async void TabViewItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			if (sender == null || !(sender is GraphiteTabViewItem viewTab))
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
}

