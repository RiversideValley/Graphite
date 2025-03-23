using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Riverside.Graphite;
using Riverside.Graphite.Pages;
using Riverside.Graphite.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Riverside.Graphite.Controls
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

		public object TagIconSource
		{
			get => GetValue(TagIconSourceProperty);
			set => SetValue(TagIconSourceProperty, value);
		}

		public static readonly DependencyProperty TagIconSourceProperty = DependencyProperty.Register(
			nameof(TagIconSource),
			typeof(object),
			typeof(GraphiteTabViewItem),
			null);

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
				//UpdatePinnedAppearance();
				tabItem.UpdatePinMenuItemText(tabItem);
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
		
		}

		private void InitializeContextMenu()
		{
			var contextMenu = new MenuFlyout();

			var pinMenuItem = new MenuFlyoutItem
			{
				Text = "Pin",
				Icon = new SymbolIcon(Symbol.Pin)
			};

			var closeAllTabsMenuItem = new MenuFlyoutItem
			{
				Text = "Close all tabs",
				Icon = new SymbolIcon(Symbol.Delete)
			};

			var closeTabsToTheRightMenuItem = new MenuFlyoutItem
			{
				Text = "Close tabs to the right",
				Icon = new SymbolIcon(Symbol.DockRight)
			};	

			closeAllTabsMenuItem.Click += (sender, e) =>
			{
				var tabView = (TabViewListView)Parent as TabViewListView;
				tabView?.Items.Clear();	
			};	

          closeTabsToTheRightMenuItem.Click += (sender, e) =>
            {
                var tabView = (TabViewListView)Parent as TabViewListView;
                var position = tabView?.Items.IndexOf(this);

                if (position.HasValue && position.Value >= 0)
                {
                    for (int i = tabView.Items.Count - 1; i > position.Value; i--)
                    {
                        tabView.Items.RemoveAt(i);
                    }
                }
            };
			pinMenuItem.Click += PinMenuItem_Click;

			contextMenu.Items.Add(pinMenuItem);
			contextMenu.Items.Add(closeAllTabsMenuItem);
			contextMenu.Items.Add(closeTabsToTheRightMenuItem);

			this.ContextFlyout = contextMenu;
		}

		private void SleepMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// Implement sleep logic here or call a method in TabManager
		}

		private void PinMenuItem_Click(object sender, RoutedEventArgs e)
		{
			this.IsPinned = !this.IsPinned;	
			//UpdatePinMenuItemText();
		}

		private void UpdatePinMenuItemText(GraphiteTabViewItem tabItem)
		{
			if (!tabItem.IsPinned)
			{
				if (tabItem.TagIconSource is ImageIconSource source)
					tabItem.IconSource = source;
				else 
					tabItem.TagIconSource = tabItem.IconSource;
			}
			else
			{
				if (tabItem.IconSource is ImageIconSource source)
				{
					tabItem.TagIconSource = source; 
				}
				tabItem.IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource
				{
					Symbol = Symbol.Pin
				};
			}

			MoveTabItemToFirstIfNoPinned(tabItem);

			if (ContextFlyout is MenuFlyout contextMenu)
			{
				var pinMenuItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item => item.Text.StartsWith("Pin") || item.Text.StartsWith("Unpin"));
				if (pinMenuItem != null)
				{
					pinMenuItem.Text = IsPinned ? "Unpin" : "Pin";
					pinMenuItem.Icon = new SymbolIcon(IsPinned ? Symbol.UnPin : Symbol.Pin);
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
			MainWindow win = (Window)(Application.Current as App).m_window as MainWindow;
			var viewTab = (sender as GraphiteTabViewItem);

			//if ((sender as FireBrowserTabViewItem).IsSelected)
			//	if (win?.TabViewContainer.SelectedItem is FireBrowserTabViewItem tab)
			//	{

			if (viewTab.Content is Frame frame)
			{
				
				if (frame.Content is WebContent web )
				{
					await web.WebViewElement.EnsureCoreWebView2Async();

					if (web.WebViewElement.CoreWebView2 is null)
						throw new InvalidCastException("CoreWebView2-CoreWebView2-api-ISNULL"); 

					if (web.PictureWebElement is BitmapImage)
					{
						// get preview from webcontent corewebView2 apis
						if (!viewTab.IsSelected)
							ViewModel.WebPreview = web.PictureWebElement;
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
        private void MoveTabItemToFirstIfNoPinned(GraphiteTabViewItem tab)
        {
            var tabView = (TabViewListView)tab.Parent as TabViewListView;
            if (tabView == null) return;

            if (tab.IsPinned)
            {
                tabView.Items.Remove(this);
                tabView.Items.Insert(0, this);
            }

			// exclude the current tab from the pinned items, set the tab position to the current tab position
			var pinnedItems = tabView.Items.OfType<GraphiteTabViewItem>().Where(item => item.IsPinned && item != this).ToList();
			var tabPosition = default(int);
			
			if (tab.IsPinned)
				tabPosition = tabView.Items.IndexOf(tab); 

			foreach (var pinnedItem in pinnedItems)
            {
                tabView.Items.Remove(pinnedItem);
                tabView.Items.Insert(tabPosition > 0 ? ++tabPosition : 0, pinnedItem);
            }
        }
        
		
	}
}

