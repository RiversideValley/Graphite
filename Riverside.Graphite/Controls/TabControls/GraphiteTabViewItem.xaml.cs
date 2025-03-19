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
using Microsoft.UI.Xaml.Media;
using Riverside.Graphite.Controls.Models;

namespace Riverside.Graphite.Controls
{
	public sealed partial class GraphiteTabViewItem : TabViewItem
	{
		public TabViewItemViewModel ViewModel { get; set; } = new TabViewItemViewModel() { IsTooltipEnabled = default };
		public GraphiteTabGroup Group { get; set; }

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

			// Pin/Unpin option
			var pinMenuItem = new MenuFlyoutItem
			{
				Text = "Pin",
				Icon = new SymbolIcon(Symbol.Pin)
			};
			pinMenuItem.Click += PinMenuItem_Click;
			contextMenu.Items.Add(pinMenuItem);

			// Add separator
			contextMenu.Items.Add(new MenuFlyoutSeparator());

			// Group options
			var createGroupItem = new MenuFlyoutItem
			{
				Text = "Create New Group",
				Icon = new SymbolIcon(Symbol.Add)
			};
			createGroupItem.Click += CreateGroupMenuItem_Click;
			contextMenu.Items.Add(createGroupItem);

			var addToGroupMenu = new MenuFlyoutSubItem
			{
				Text = "Add to Group",
				Icon = new SymbolIcon(Symbol.AddFriend)
			};
			// Groups will be populated dynamically when the menu is opened
			contextMenu.Items.Add(addToGroupMenu);

			var removeFromGroupItem = new MenuFlyoutItem
			{
				Text = "Remove from Group",
				Icon = new SymbolIcon(Symbol.Remove),
				Visibility = Visibility.Collapsed // Initially hidden
			};
			removeFromGroupItem.Click += RemoveFromGroupMenuItem_Click;
			contextMenu.Items.Add(removeFromGroupItem);

			// Add separator
			contextMenu.Items.Add(new MenuFlyoutSeparator());

			// Sleep/Wake option
			var sleepMenuItem = new MenuFlyoutItem
			{
				Text = "Sleep Tab",
				Icon = new SymbolIcon(Symbol.Edit)
			};
			sleepMenuItem.Click += SleepMenuItem_Click;
			contextMenu.Items.Add(sleepMenuItem);

			// Duplicate tab option
			var duplicateMenuItem = new MenuFlyoutItem
			{
				Text = "Duplicate Tab",
				Icon = new SymbolIcon(Symbol.Copy)
			};
			duplicateMenuItem.Click += DuplicateMenuItem_Click;
			contextMenu.Items.Add(duplicateMenuItem);

			// Add separator
			contextMenu.Items.Add(new MenuFlyoutSeparator());

			// Close options
			var closeOtherTabsItem = new MenuFlyoutItem
			{
				Text = "Close Other Tabs",
				Icon = new SymbolIcon(Symbol.Clear)
			};
			closeOtherTabsItem.Click += CloseOtherTabsMenuItem_Click;
			contextMenu.Items.Add(closeOtherTabsItem);

			var closeTabsToRightItem = new MenuFlyoutItem
			{
				Text = "Close Tabs to the Right",
				Icon = new SymbolIcon(Symbol.Delete)
			};
			closeTabsToRightItem.Click += CloseTabsToRightMenuItem_Click;
			contextMenu.Items.Add(closeTabsToRightItem);

			// Set the context menu
			this.ContextFlyout = contextMenu;

			// Register for opening event to update menu items
			contextMenu.Opening += ContextMenu_Opening;
		}

		private void ContextMenu_Opening(object sender, object e)
		{
			if (sender is MenuFlyout contextMenu)
			{
				// Update pin/unpin text
				var pinMenuItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item =>
					item.Text.StartsWith("Pin") || item.Text.StartsWith("Unpin"));
				if (pinMenuItem != null)
				{
					pinMenuItem.Text = IsPinned ? "Unpin" : "Pin";
					pinMenuItem.Icon = new SymbolIcon(IsPinned ? Symbol.UnPin : Symbol.Pin);
				}

				// Update sleep/wake text
				var sleepMenuItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item =>
					item.Text.StartsWith("Sleep") || item.Text.StartsWith("Wake"));
				if (sleepMenuItem != null)
				{
					bool isSleeping = this.Header.ToString().StartsWith("Sleeping - ");
					sleepMenuItem.Text = isSleeping ? "Wake Tab" : "Sleep Tab";
					sleepMenuItem.Icon = new SymbolIcon(isSleeping ? Symbol.Play : Symbol.Edit);
				}

				// Update group-related items
				var removeFromGroupItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item =>
					item.Text == "Remove from Group");
				if (removeFromGroupItem != null)
				{
					removeFromGroupItem.Visibility = Group != null ? Visibility.Visible : Visibility.Collapsed;
				}

				// Update "Add to Group" submenu
				var addToGroupMenu = contextMenu.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault(item =>
					item.Text == "Add to Group");
				if (addToGroupMenu != null)
				{
					addToGroupMenu.Items.Clear();

					// Get the TabManager
					var tabManager = GetTabManager();
					if (tabManager != null && tabManager.TabGroups.Count > 0)
					{
						foreach (var group in tabManager.TabGroups)
						{
							var groupItem = new MenuFlyoutItem { Text = group.Name };
							groupItem.Click += (s, args) =>
							{
								tabManager.AddTabToGroup(this, group.Name);
							};
							addToGroupMenu.Items.Add(groupItem);
						}
					}
					else
					{
						var noGroupsItem = new MenuFlyoutItem
						{
							Text = "No groups available",
							IsEnabled = false
						};
						addToGroupMenu.Items.Add(noGroupsItem);
					}
				}
			}
		}

		private void SleepMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null)
			{
				_ = tabManager.ToggleTabSleep(this);
			}
		}

		private void PinMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null)
			{
				tabManager.ToggleTabPin(this);
			}
		}

		private async void CreateGroupMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null)
			{
				// Create dialog for group name input
				var dialog = new ContentDialog
				{
					Title = "Create New Group",
					PrimaryButtonText = "Create",
					CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Primary,
					XamlRoot = this.XamlRoot
				};

				var textBox = new TextBox
				{
					PlaceholderText = "Enter group name",
					Margin = new Thickness(0, 10, 0, 0)
				};

				dialog.Content = textBox;

				// Show dialog
				var result = await dialog.ShowAsync();

				if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
				{
					tabManager.AddTabToGroup(this, textBox.Text);
				}
			}
		}

		private void RemoveFromGroupMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null && Group != null)
			{
				tabManager.RemoveTabFromGroup(this);
			}
		}

		private void DuplicateMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null)
			{
				// Get the URL or content type
				string url = ""; 

				// Create a new tab with the same content
				GraphiteTabViewItem newTab;

				if (url == "about:newtab" || string.IsNullOrEmpty(url))
				{
					newTab = tabManager.CreateNewTab(typeof(NewTab), null, false);
				}
				else
				{
					newTab = tabManager.CreateNewTab(typeof(WebContent), url, false);
				}

				// Copy group if any
				if (Group != null)
				{
					tabManager.AddTabToGroup(newTab, Group.Name);
				}

				// Select the new tab
				var tabView = FindParent<TabView>();
				if (tabView != null)
				{
					tabView.SelectedItem = newTab;
				}
			}
		}

		private void CloseOtherTabsMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null)
			{
				var tabView = FindParent<TabView>();
				if (tabView != null)
				{
					// Store tabs to close
					var tabsToClose = tabView.TabItems.OfType<GraphiteTabViewItem>()
						.Where(t => t != this && !t.IsPinned)
						.ToList();

					// Close tabs
					foreach (var tab in tabsToClose)
					{
						tabManager.CloseTab(tab);
					}
				}
			}
		}

		private void CloseTabsToRightMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var tabManager = GetTabManager();
			if (tabManager != null)
			{
				var tabView = FindParent<TabView>();
				if (tabView != null)
				{
					// Find index of current tab
					int currentIndex = tabView.TabItems.IndexOf(this);
					if (currentIndex < 0)
						return;

					// Store tabs to close
					var tabsToClose = tabView.TabItems.OfType<GraphiteTabViewItem>()
						.Skip(currentIndex + 1)
						.Where(t => !t.IsPinned)
						.ToList();

					// Close tabs
					foreach (var tab in tabsToClose)
					{
						tabManager.CloseTab(tab);
					}
				}
			}
		}

		private void UpdatePinMenuItemText()
		{
			if (ContextFlyout is MenuFlyout contextMenu)
			{
				var pinMenuItem = contextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(item =>
					item.Text.StartsWith("Pin") || item.Text.StartsWith("Unpin"));
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

		// Apply group style to the tab
		public void ApplyGroupStyle(GraphiteTabGroup group)
		{
			if (group == null)
				return;

			Group = group;

			// Apply visual indicator for group
			this.BorderThickness = new Thickness(0, 0, 0, 3);
			this.BorderBrush = group.GetColorBrush();
		}

		// Reset group styling
		public void ResetGroupStyle()
		{
			Group = null;
			this.BorderThickness = new Thickness(0);
			this.BorderBrush = null;
		}

		private TabManager GetTabManager()
		{
			var tabView = FindParent<GraphiteTabViewContainer>();
			return tabView?.TabManager;
		}

		private T FindParent<T>() where T : DependencyObject
		{
			DependencyObject parent = VisualTreeHelper.GetParent(this);
			while (parent != null && !(parent is T))
			{
				parent = VisualTreeHelper.GetParent(parent);
			}
			return parent as T;
		}

		private async void TabViewItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			MainWindow win = (Window)(Application.Current as App).m_window as MainWindow;
			var viewTab = (sender as GraphiteTabViewItem);

			if (viewTab.Content is Frame frame)
			{
				if (frame.Content is WebContent web)
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
	}
}
