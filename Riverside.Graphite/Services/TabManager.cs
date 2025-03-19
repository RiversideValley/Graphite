using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text.Json;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System.Collections.Concurrent;
using Riverside.Graphite.Pages;
using Graphite.ViewModels;
using Newtonsoft.Json.Linq;
using Riverside.Graphite.Core;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.ObjectModel;
using Riverside.Graphite.Controls.Models;
using System.Xml.Linq;

namespace Riverside.Graphite.Controls;

public class TabManager
{
	public GraphiteTabViewContainer _tabViewContainer;
	public const string TabStateKey = "TabState";
	private const int SleepTimeoutMinutes = 1;
	public Dictionary<GraphiteTabViewItem, DateTime> _lastActivityTimes = new Dictionary<GraphiteTabViewItem, DateTime>();
	public Dictionary<GraphiteTabViewItem, Guid> CurrentTabs { get; set; }
	private DispatcherQueueTimer _sleepTimer;
	private GraphiteTabViewItem _activeTab;
	private ConcurrentQueue<GraphiteTabViewItem> _preloadedTabs = new ConcurrentQueue<GraphiteTabViewItem>();
	private const int MAX_PRELOADED_TABS = 1;
	private bool _isRestoringTabs = false;
	private MenuFlyout _tabContextMenu;
	private GraphiteTabViewItem _contextMenuTargetTab;
	private ContentDialog _groupDialog;
	private TextBox _groupNameTextBox;
	private ColorPicker _colorPicker;

	// Add a collection for tab groups
	public ObservableCollection<GraphiteTabGroup> TabGroups { get; } = new ObservableCollection<GraphiteTabGroup>();

	public event EventHandler<GraphiteTabViewItem> TabPutToSleep;

	public TabManager(GraphiteTabViewContainer tabViewContainer)
	{
		InitializeTabManager(tabViewContainer);
	}

	public TabManager()
	{
		// Empty constructor
	}

	public void InitializeTabManager(GraphiteTabViewContainer tabViewContainer)
	{
		_tabViewContainer = tabViewContainer;
		_tabViewContainer.SelectionChanged += TabViewContainer_SelectionChanged;
		InitializeSleepTimer();
		ApplicationData.Current.LocalSettings.Values.TryGetValue($"{AuthService.CurrentUser?.Username}_{"RestoreTabs"}", out object isRestore);
		_isRestoringTabs = Convert.ToBoolean(isRestore);

		// Initialize the tab context menu
		InitializeTabContextMenu();

		// Register for tab item added event to attach context menu
		_tabViewContainer.TabItemsChanged += TabViewContainer_TabItemsChanged;
	}

	private void InitializeTabContextMenu()
	{
		// Create the context menu
		_tabContextMenu = new MenuFlyout();

		// Create New Group
		var createGroupItem = new MenuFlyoutItem { Text = "Create New Group" };
		createGroupItem.Icon = new SymbolIcon(Symbol.Add);
		createGroupItem.Click += CreateTabGroup_Click;
		_tabContextMenu.Items.Add(createGroupItem);

		// Add to Group submenu
		var addToGroupSubMenu = new MenuFlyoutSubItem { Text = "Add to Group" };
		addToGroupSubMenu.Icon = new SymbolIcon(Symbol.AddFriend);
		var noGroupsPlaceholder = new MenuFlyoutItem { Text = "No groups available", IsEnabled = false };
		addToGroupSubMenu.Items.Add(noGroupsPlaceholder);
		_tabContextMenu.Items.Add(addToGroupSubMenu);

		// Remove from Group
		var removeFromGroupItem = new MenuFlyoutItem { Text = "Remove from Group" };
		removeFromGroupItem.Icon = new SymbolIcon(Symbol.Remove);
		removeFromGroupItem.Click += RemoveFromGroup_Click;
		_tabContextMenu.Items.Add(removeFromGroupItem);

		_tabContextMenu.Items.Add(new MenuFlyoutSeparator());

		// Rename Group
		var renameGroupItem = new MenuFlyoutItem { Text = "Rename Group" };
		renameGroupItem.Icon = new SymbolIcon(Symbol.Rename);
		renameGroupItem.Click += RenameGroup_Click;
		_tabContextMenu.Items.Add(renameGroupItem);

		// Change Group Color
		var changeGroupColorItem = new MenuFlyoutItem { Text = "Change Group Color" };
		changeGroupColorItem.Icon = new SymbolIcon(Symbol.FontColor);
		changeGroupColorItem.Click += ChangeGroupColor_Click;
		_tabContextMenu.Items.Add(changeGroupColorItem);

		_tabContextMenu.Items.Add(new MenuFlyoutSeparator());

		// Pin Tab
		var pinTabItem = new MenuFlyoutItem { Text = "Pin Tab" };
		pinTabItem.Icon = new SymbolIcon(Symbol.Pin);
		pinTabItem.Click += PinTab_Click;
		_tabContextMenu.Items.Add(pinTabItem);

		// Unpin Tab
		var unpinTabItem = new MenuFlyoutItem { Text = "Unpin Tab", Visibility = Visibility.Collapsed };
		unpinTabItem.Icon = new SymbolIcon(Symbol.UnPin);
		unpinTabItem.Click += UnpinTab_Click;
		_tabContextMenu.Items.Add(unpinTabItem);

		// Duplicate Tab
		var duplicateTabItem = new MenuFlyoutItem { Text = "Duplicate Tab" };
		duplicateTabItem.Icon = new SymbolIcon(Symbol.Copy);
		duplicateTabItem.Click += DuplicateTab_Click;
		_tabContextMenu.Items.Add(duplicateTabItem);

		// Sleep/Wake Tab
		var sleepTabItem = new MenuFlyoutItem { Text = "Sleep Tab" };
		sleepTabItem.Icon = new SymbolIcon(Symbol.Edit);
		sleepTabItem.Click += SleepTab_Click;
		_tabContextMenu.Items.Add(sleepTabItem);

		_tabContextMenu.Items.Add(new MenuFlyoutSeparator());

		// Close Other Tabs
		var closeOtherTabsItem = new MenuFlyoutItem { Text = "Close Other Tabs" };
		closeOtherTabsItem.Icon = new SymbolIcon(Symbol.Clear);
		closeOtherTabsItem.Click += CloseOtherTabs_Click;
		_tabContextMenu.Items.Add(closeOtherTabsItem);

		// Close Tabs to the Right
		var closeTabsToRightItem = new MenuFlyoutItem { Text = "Close Tabs to the Right" };
		closeTabsToRightItem.Icon = new SymbolIcon(Symbol.Delete);
		closeTabsToRightItem.Click += CloseTabsToRight_Click;
		_tabContextMenu.Items.Add(closeTabsToRightItem);
	}

	private void TabViewContainer_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
	{
		// When a new tab is added, attach the context menu
		if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
		{
			var index = (int)args.Index;
			if (index < _tabViewContainer.TabItems.Count)
			{
				var tabItem = _tabViewContainer.TabItems[index] as GraphiteTabViewItem;
				if (tabItem != null)
				{
					AttachContextMenuToTab(tabItem);
				}
			}
		}
	}

	// Add this method to attach the context menu to a tab
	private void AttachContextMenuToTab(GraphiteTabViewItem tabItem)
	{
		tabItem.RightTapped -= TabItem_RightTapped; // Remove any existing handler
		tabItem.RightTapped += TabItem_RightTapped;
	}

	// Add this method to handle right-click on a tab
	private void TabItem_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
	{
		var tabItem = sender as GraphiteTabViewItem;
		if (tabItem != null)
		{
			_contextMenuTargetTab = tabItem;

			// Update menu items based on tab state
			UpdateContextMenuState();

			// Show the context menu
			_tabContextMenu.ShowAt(tabItem, e.GetPosition(tabItem));

			e.Handled = true;
		}
	}

	// Add this method to update the context menu state based on the target tab
	private void UpdateContextMenuState()
	{
		if (_contextMenuTargetTab == null)
			return;

		// Get menu items by name
		var pinTabItem = _tabContextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Pin Tab");
		var unpinTabItem = _tabContextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Unpin Tab");
		var removeFromGroupItem = _tabContextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Remove from Group");
		var renameGroupItem = _tabContextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Rename Group");
		var changeGroupColorItem = _tabContextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Change Group Color");
		var sleepTabItem = _tabContextMenu.Items.OfType<MenuFlyoutItem>().FirstOrDefault(i => i.Text == "Sleep Tab");

		// Update sleep/wake menu item
		if (sleepTabItem != null)
		{
			bool isSleeping = _contextMenuTargetTab.Header.ToString().StartsWith("Sleeping - ");
			sleepTabItem.Text = isSleeping ? "Wake Tab" : "Sleep Tab";
			sleepTabItem.Icon = new SymbolIcon(isSleeping ? Symbol.Play : Symbol.Edit);
		}

		// Update Add to Group submenu
		var addToGroupSubMenu = _tabContextMenu.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault(i => i.Text == "Add to Group");
		if (addToGroupSubMenu != null)
		{
			addToGroupSubMenu.Items.Clear();

			if (TabGroups.Count > 0)
			{
				foreach (var group in TabGroups)
				{
					var groupItem = new MenuFlyoutItem { Text = group.Name };
					groupItem.Click += (s, e) => AddTabToGroup(_contextMenuTargetTab, group.Name);
					addToGroupSubMenu.Items.Add(groupItem);
				}
			}
			else
			{
				addToGroupSubMenu.Items.Add(new MenuFlyoutItem { Text = "No groups available", IsEnabled = false });
			}
		}

		// Update pin/unpin visibility
		if (pinTabItem != null && unpinTabItem != null)
		{
			if (_contextMenuTargetTab.IsPinned)
			{
				pinTabItem.Visibility = Visibility.Collapsed;
				unpinTabItem.Visibility = Visibility.Visible;
			}
			else
			{
				pinTabItem.Visibility = Visibility.Visible;
				unpinTabItem.Visibility = Visibility.Collapsed;
			}
		}

		// Update group-related items visibility
		bool hasGroup = _contextMenuTargetTab.Group != null;
		if (removeFromGroupItem != null)
			removeFromGroupItem.Visibility = hasGroup ? Visibility.Visible : Visibility.Collapsed;

		if (renameGroupItem != null)
			renameGroupItem.Visibility = hasGroup ? Visibility.Visible : Visibility.Collapsed;

		if (changeGroupColorItem != null)
			changeGroupColorItem.Visibility = hasGroup ? Visibility.Visible : Visibility.Collapsed;

		// Update sleep/wake menu item
		if (sleepTabItem != null)
		{
			bool isSleeping = _contextMenuTargetTab.Header.ToString().StartsWith("Sleeping - ");
			sleepTabItem.Text = isSleeping ? "Wake Tab" : "Sleep Tab";
			sleepTabItem.Icon = new SymbolIcon(isSleeping ? Symbol.Play : Symbol.Edit);
		}
	}

	// Add these event handlers for the context menu items
	private async void CreateTabGroup_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		// Create dialog for group name input
		if (_groupDialog == null)
		{
			_groupDialog = new ContentDialog
			{
				Title = "Create New Group",
				PrimaryButtonText = "Create",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary
			};

			_groupNameTextBox = new TextBox
			{
				PlaceholderText = "Enter group name",
				Margin = new Thickness(0, 10, 0, 0)
			};

			_groupDialog.Content = _groupNameTextBox;
		}

		// Reset text box
		_groupNameTextBox.Text = string.Empty;

		// Set XamlRoot for the dialog
		_groupDialog.XamlRoot = _tabViewContainer.XamlRoot;

		// Show dialog
		var result = await _groupDialog.ShowAsync();

		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(_groupNameTextBox.Text))
		{
			AddTabToGroup(_contextMenuTargetTab, _groupNameTextBox.Text);
		}
	}

	private void RemoveFromGroup_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null || _contextMenuTargetTab.Group == null)
			return;

		RemoveTabFromGroup(_contextMenuTargetTab);
	}

	private async void RenameGroup_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null || _contextMenuTargetTab.Group == null)
			return;

		var group = _contextMenuTargetTab.Group;

		// Create dialog for group name input
		if (_groupDialog == null)
		{
			_groupDialog = new ContentDialog
			{
				Title = "Rename Group",
				PrimaryButtonText = "Rename",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary
			};

			_groupNameTextBox = new TextBox
			{
				PlaceholderText = "Enter new group name",
				Margin = new Thickness(0, 10, 0, 0)
			};

			_groupDialog.Content = _groupNameTextBox;
		}

		// Set current group name
		_groupNameTextBox.Text = group.Name;

		// Set XamlRoot for the dialog
		_groupDialog.XamlRoot = _tabViewContainer.XamlRoot;

		// Show dialog
		var result = await _groupDialog.ShowAsync();

		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(_groupNameTextBox.Text))
		{
			// Rename the group
			string oldName = group.Name;
			string newName = _groupNameTextBox.Text;

			if (oldName != newName)
			{
				group.Name = newName;

				// Update all tabs in this group
				foreach (var tab in group.Tabs)
				{
					// Apply updated group style if needed
					tab.ApplyGroupStyle(group);
				}
			}
		}
	}

	private async void ChangeGroupColor_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null || _contextMenuTargetTab.Group == null)
			return;

		var group = _contextMenuTargetTab.Group;

		// Create color picker dialog
		var colorDialog = new ContentDialog
		{
			Title = "Choose Group Color",
			PrimaryButtonText = "Apply",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = _tabViewContainer.XamlRoot
		};

		// Create color picker
		if (_colorPicker == null)
		{
			_colorPicker = new ColorPicker
			{
				ColorSpectrumShape = ColorSpectrumShape.Box,
				IsAlphaEnabled = false,
				IsColorPreviewVisible = true,
				IsColorSliderVisible = true,
				IsHexInputVisible = true
			};
		}

		// Try to parse current color
		try
		{
			string colorHex = "#FF0078D7"; // Default color
			if (!string.IsNullOrEmpty(group.Color))
			{
				colorHex = group.Color;
			}

			_colorPicker.Color = HexToColor(colorHex);
		}
		catch
		{
			// Use default color if parsing fails
			_colorPicker.Color = Windows.UI.Color.FromArgb(255, 0, 120, 215);
		}

		colorDialog.Content = _colorPicker;

		// Show dialog
		var result = await colorDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			// Get selected color
			var color = _colorPicker.Color;
			string colorHex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

			// Update group color
			group.Color = colorHex;

			// Update all tabs in this group
			foreach (var tab in group.Tabs)
			{
				// Apply updated group style
				tab.ApplyGroupStyle(group);
			}
		}
	}

	private void PinTab_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		ToggleTabPin(_contextMenuTargetTab);
	}

	private void UnpinTab_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		ToggleTabPin(_contextMenuTargetTab);
	}

	private void DuplicateTab_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		// Get the URL or content type
		string url = GetTabUrl(_contextMenuTargetTab);

		// Create a new tab with the same content
		GraphiteTabViewItem newTab;

		if (url == "about:newtab" || string.IsNullOrEmpty(url))
		{
			newTab = CreateNewTab(typeof(NewTab), null, false);
		}
		else
		{
			newTab = CreateNewTab(typeof(WebContent), url, false);
		}

		// Copy group if any
		if (_contextMenuTargetTab.Group != null)
		{
			AddTabToGroup(newTab, _contextMenuTargetTab.Group.Name);
		}

		// Select the new tab
		_tabViewContainer.SelectedItem = newTab;
	}

	private async void SleepTab_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		await ToggleTabSleep(_contextMenuTargetTab);
	}

	private void CloseOtherTabs_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		// Store tabs to close
		var tabsToClose = _tabViewContainer.TabItems.OfType<GraphiteTabViewItem>()
			.Where(t => t != _contextMenuTargetTab && !t.IsPinned)
			.ToList();

		// Close tabs
		foreach (var tab in tabsToClose)
		{
			CloseTab(tab);
		}
	}

	private void CloseTabsToRight_Click(object sender, RoutedEventArgs e)
	{
		if (_contextMenuTargetTab == null)
			return;

		// Find index of current tab
		int currentIndex = _tabViewContainer.TabItems.IndexOf(_contextMenuTargetTab);
		if (currentIndex < 0)
			return;

		// Store tabs to close
		var tabsToClose = _tabViewContainer.TabItems.OfType<GraphiteTabViewItem>()
			.Skip(currentIndex + 1)
			.Where(t => !t.IsPinned)
			.ToList();

		// Close tabs
		foreach (var tab in tabsToClose)
		{
			CloseTab(tab);
		}
	}

	public Task<bool> GetCurrentTabs()
	{
		CurrentTabs = new Dictionary<GraphiteTabViewItem, Guid>();

		foreach (var tab in _tabViewContainer.TabItems)
		{
			if (tab is GraphiteTabViewItem graphiteTab)
			{
				CurrentTabs.Add(graphiteTab, (Guid)graphiteTab.Tag);
			}
		}

		if (CurrentTabs.Count > 0)
			return Task.FromResult(true);
		else
			return Task.FromResult(false);
	}

	private void TabViewContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (sender is TabView tabView && tabView.SelectedItem is GraphiteTabViewItem selectedTab)
		{
			OnTabSelected(selectedTab);
		}
	}

	private Task PreloadTabAsync()
	{
		if (_preloadedTabs.Count < MAX_PRELOADED_TABS && !_isRestoringTabs)
		{
			try
			{
				_tabViewContainer.DispatcherQueue.TryEnqueue(async () =>
				{
					var newTab = new GraphiteTabViewItem()
					{
						Header = "New Tab",
						IconSource = new SymbolIconSource { Symbol = Symbol.Home }
					};

					var newTabContent = new NewTab();
					newTab.Content = CreateFrame(typeof(NewTab), null);
					_preloadedTabs.Enqueue(newTab);
				});
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error preloading tab: {ex.Message}");
			}
		}
		return Task.CompletedTask;
	}

	public async Task<GraphiteTabViewItem> CreateLazyLoadingTab(string url)
	{
		GraphiteTabViewItem newTab = null;

		_tabViewContainer.DispatcherQueue.TryEnqueue(async () =>
		{
			if (_preloadedTabs.TryDequeue(out var preloadedTab))
			{
				newTab = preloadedTab;
			}
			else
			{
				newTab = new GraphiteTabViewItem()
				{
					Header = "Loading...",
					IconSource = new SymbolIconSource { Symbol = Symbol.Refresh }
				};
				newTab.Content = new ProgressRing { IsActive = true };
			}

			_tabViewContainer.TabItems.Add(newTab);

			// Attach context menu to the new tab
			AttachContextMenuToTab(newTab);

			try
			{
				WebContent webContent;
				if (newTab.Content is WebContent existingContent)
				{
					webContent = existingContent;
					webContent.WebView.NavigateToString(url);
				}
				else
				{
					webContent = new WebContent();
					webContent.WebView.NavigateToString(url);
				}

				if (!(newTab.Content is WebContent))
				{
					newTab.Content = webContent;
				}
				await webContent?.WebView.EnsureCoreWebView2Async();

				newTab.Header = webContent?.WebView.CoreWebView2.DocumentTitle ?? "New Tab";
				UpdateTabIcon(newTab, webContent?.WebView.CoreWebView2.FaviconUri);

			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error creating lazy loading tab: {ex.Message}");
				newTab.Header = "Error";
				newTab.IconSource = new SymbolIconSource { Symbol = Symbol.GoToToday };
			}
		});

		// Preload a new tab to replace the one we just used
		await PreloadTabAsync();

		return newTab;
	}

	public async Task StartPreloadingTabs()
	{
		if (!_isRestoringTabs && _preloadedTabs.Count == 0)
		{
			await PreloadTabAsync();
		}
	}

	public GraphiteTabViewItem CreateNewTab(Type pageType = null, object parameter = null, bool isSplitViewActive = false, string username = null, Guid? idTag = null)
	{
		GraphiteTabViewItem newItem;

		if (_preloadedTabs.TryDequeue(out var preloadedTab))
		{
			newItem = preloadedTab;
			newItem.Header = $"Home Page - {username}";
			newItem.IconSource = new SymbolIconSource { Symbol = Symbol.Home };
			newItem.Style = (Style)Application.Current.Resources["FloatingTabViewItemStyle"];
		}
		else
		{
			newItem = new GraphiteTabViewItem()
			{
				Header = $"Home Page - {username}",
				IconSource = new SymbolIconSource { Symbol = Symbol.Home },
				Style = (Style)Application.Current.Resources["FloatingTabViewItemStyle"]
			};
		}

		if (idTag is null)
		{
			newItem.Tag = Guid.NewGuid();
		}
		else { newItem.Tag = idTag; }

		Passer passer = new()
		{
			Tab = newItem,
			TabView = _tabViewContainer,
			ViewModel = new ToolbarViewModel { CurrentAddress = "" },
			Param = parameter,
		};

		if (isSplitViewActive)
		{
			var splitViewContainer = new SplitViewContainer
			{
				IsSplitViewActive = true
			};

			splitViewContainer.PrimaryContent = CreateFrame(pageType, parameter);
			splitViewContainer.SecondaryContent = CreateFrame(pageType, parameter);

			newItem.Content = splitViewContainer;
		}
		else
		{

			newItem.Content = CreateFrame(pageType, passer);

		}

		if (newItem.Content is Frame frame)
		{
			frame.Tag = passer;
		}
		else if (newItem.Content is SplitViewContainer splitView)
		{
			if (splitView.PrimaryContent is Frame primaryFrame)
			{
				primaryFrame.Tag = passer;
			}
		}

		_tabViewContainer.AddTab(newItem);
		UpdateTabActivity(newItem);

		// Attach context menu to the new tab
		AttachContextMenuToTab(newItem);

		_tabViewContainer.SelectedItem = newItem;

		// Preload a new tab to replace the one we just used
		if (_preloadedTabs.Count == 0)
		{
			Task.Run(async () => await PreloadTabAsync());
		}

		return newItem;
	}

	private Frame CreateFrame(Type pageType, object parameter)
	{
		Frame frame = new Frame
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Margin = new Thickness(2, 48, 2, 2)
		};

		frame.Navigate(pageType, parameter);

		return frame;
	}

	public Task<string> SaveTabStateAsync(string username)
	{
		var tabStates = new List<TabState>();

		foreach (GraphiteTabViewItem tab in _tabViewContainer.TabItems)
		{
			var state = new TabState
			{
				Id = tab.Tag is not null ? (Guid)tab.Tag : Guid.NewGuid(),
				Header = tab.Header.ToString(),
				IsSleeping = tab.Header.ToString().StartsWith("Sleeping - "),
				Url = GetTabUrl(tab),
				IsSplitView = tab.Content is SplitViewContainer,
				FaviconUrl = GetTabFaviconUrl(tab),
				LastAccessTime = _lastActivityTimes.ContainsKey(tab) ? _lastActivityTimes[tab] : DateTime.Now,
				ScrollPosition = GetTabScrollPosition(tab),
				PageTitle = GetTabTitle(tab),
				IsPinned = tab.IsPinned,
				CustomColor = GetTabColor(tab),
				IsNewTab = tab.Content is Frame frame && frame.Content is NewTab,
				// Add group information
				GroupName = tab.Group?.Name,
				GroupColor = tab.Group?.Color
			};

			tabStates.Add(state);
		}

		var json = JsonSerializer.Serialize(tabStates);
		var localSettings = ApplicationData.Current.LocalSettings;
		localSettings.Values[$"{username}_{TabStateKey}"] = json;

		return Task.FromResult(json ?? null);
	}

	public Task RestoreTabsAsync(string username)
	{
		_isRestoringTabs = true;
		var localSettings = ApplicationData.Current.LocalSettings;
		if (localSettings.Values.TryGetValue($"{username}_{TabStateKey}", out object jsonObj))
		{
			var json = jsonObj as string;
			var tabStates = JsonSerializer.Deserialize<List<TabState>>(json);

			// First, clear any existing tab groups
			TabGroups.Clear();

			// Create all the groups first
			var groupNames = tabStates.Where(s => !string.IsNullOrEmpty(s.GroupName))
									 .Select(s => s.GroupName)
									 .Distinct();

			foreach (var groupName in groupNames)
			{
				var groupColor = tabStates.FirstOrDefault(s => s.GroupName == groupName)?.GroupColor;
				TabGroups.Add(new GraphiteTabGroup
				{
					Name = groupName,
					Color = !string.IsNullOrEmpty(groupColor) ? groupColor : "#FF0078D7"
				});
			}

			_tabViewContainer.DispatcherQueue.TryEnqueue(async () =>
			{
				_tabViewContainer.TabItems.Clear();
				_lastActivityTimes.Clear();

				foreach (var state in tabStates)
				{
					SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

					try
					{
						await semaphore.WaitAsync();
						GraphiteTabViewItem newTab;
						if (state.Url == "about:newtab" || string.IsNullOrEmpty(state.Url))
						{
							newTab = CreateNewTab(typeof(NewTab), null, state.IsSplitView, username, state.Id);

						}
						else
						{
							newTab = CreateNewTab(typeof(WebContent), state.Url, state.IsSplitView, username, state.Id);

						}

						if (state.IsSleeping)
						{
							await PutTabToSleep(newTab);
						}
						else if (newTab.Content is Frame frame)
						{
							if (frame.Content is WebContent webContent)
							{
								webContent.WebView.Source = new(state.Url);
								await webContent.WebView.EnsureCoreWebView2Async();
								UpdateTabIcon(newTab, state.FaviconUrl);
							}
							else if (frame.Content is NewTab nT)
							{
								nT.ViewModel.SettingsService.Initialize();
								// Update NewTab content if necessary
							}
						}
						newTab.Header = state.Header;
						newTab.IsPinned = state.IsPinned;

						// Restore group if applicable
						if (!string.IsNullOrEmpty(state.GroupName))
						{
							var group = TabGroups.FirstOrDefault(g => g.Name == state.GroupName);
							if (group != null)
							{
								AddTabToGroup(newTab, group.Name);
							}
						}

						SetTabColor(newTab, state.CustomColor);
						_lastActivityTimes[newTab] = state.LastAccessTime;
						SetTabScrollPosition(newTab, state.ScrollPosition);
					}
					catch (Exception ex)
					{
						semaphore.Release();
						System.Diagnostics.Debug.WriteLine($"Error restoring tab: {ex.Message}");
						throw;
					}
				}
			});
		}
		_isRestoringTabs = false;
		return Task.CompletedTask;
	}

	private string GetTabUrl(GraphiteTabViewItem tab)
	{
		if (tab.Content is Frame frame)
		{
			if (frame.Content is WebContent webContent)
			{
				return webContent.WebView.Source?.AbsoluteUri ?? "about:blank";
			}
			else if (frame.Content is NewTab)
			{
				return "about:newtab";
			}
		}
		else if (tab.Content is SplitViewContainer splitView)
		{
			if (splitView.PrimaryContent is Frame primaryFrame)
			{
				if (primaryFrame.Content is WebContent primaryWebContent)
				{
					return primaryWebContent.WebView.Source.AbsoluteUri;
				}
				else if (primaryFrame.Content is NewTab)
				{
					return "about:newtab";
				}
			}
		}
		return string.Empty;
	}

	private string GetTabFaviconUrl(GraphiteTabViewItem tab)
	{
		if (tab.Content is Frame frame && frame.Content is WebContent webContent)
		{
			return webContent.WebView.CoreWebView2?.FaviconUri;
		}
		else if (tab.Content is SplitViewContainer splitView)
		{
			if (splitView.PrimaryContent is Frame primaryFrame && primaryFrame.Content is WebContent primaryWebContent)
			{
				return primaryWebContent.WebView.CoreWebView2.FaviconUri;
			}
		}
		return string.Empty;
	}

	private int GetTabScrollPosition(GraphiteTabViewItem tab)
	{
		// Implement logic to get scroll position
		return 0;
	}

	private void SetTabScrollPosition(GraphiteTabViewItem tab, int position)
	{
		// Implement logic to set scroll position
	}

	private string GetTabTitle(GraphiteTabViewItem tab)
	{
		if (tab.Content is Frame frame && frame.Content is WebContent webContent)
		{
			return webContent.WebView.CoreWebView2?.DocumentTitle;
		}
		return tab.Header.ToString();
	}

	private string GetTabColor(GraphiteTabViewItem tab)
	{
		// Return the group color if the tab is in a group
		if (tab.Group != null)
		{
			return tab.Group.Color;
		}
		return "";
	}

	private void SetTabColor(GraphiteTabViewItem tab, string color)
	{
		// If the tab is not in a group, we can set a custom color
		if (tab.Group == null && !string.IsNullOrEmpty(color))
		{
			tab.BorderThickness = new Thickness(0, 0, 0, 3);
			tab.BorderBrush = new SolidColorBrush(HexToColor(color));
		}
	}

	private Windows.UI.Color HexToColor(string hex)
	{
		// Handle null or empty hex values
		if (string.IsNullOrEmpty(hex))
			return Windows.UI.Color.FromArgb(0, 0, 0, 0); // Return transparent color

		hex = hex.Replace("#", string.Empty);
		byte a = 255;
		byte r = 0;
		byte g = 0;
		byte b = 0;

		try
		{
			if (hex.Length == 8)
			{
				a = Convert.ToByte(hex.Substring(0, 2), 16);
				hex = hex.Substring(2);
			}

			if (hex.Length == 6)
			{
				r = Convert.ToByte(hex.Substring(0, 2), 16);
				g = Convert.ToByte(hex.Substring(2, 2), 16);
				b = Convert.ToByte(hex.Substring(4, 2), 16);
			}
			else if (hex.Length == 3)
			{
				r = Convert.ToByte(hex[0] + hex[0].ToString(), 16);
				g = Convert.ToByte(hex[1] + hex[1].ToString(), 16);
				b = Convert.ToByte(hex[2] + hex[2].ToString(), 16);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error parsing color hex: {ex.Message}");
			return Windows.UI.Color.FromArgb(255, 0, 0, 0); // Return black as fallback
		}

		return Windows.UI.Color.FromArgb(a, r, g, b);
	}

	private void InitializeSleepTimer()
	{
		_sleepTimer = _tabViewContainer.DispatcherQueue.CreateTimer();
		_sleepTimer.Interval = TimeSpan.FromMinutes(1);
		_sleepTimer.Tick += async (s, e) => await CheckAndSleepInactiveTabs();
		_sleepTimer.Start();
	}

	private async Task CheckAndSleepInactiveTabs()
	{
		var now = DateTime.Now;
		var tabsToSleep = _lastActivityTimes
			.Where(kvp => (now - kvp.Value).TotalMinutes >= SleepTimeoutMinutes &&
						  IsWebContentTab(kvp.Key) &&
						  kvp.Key != _activeTab)
			.Select(kvp => kvp.Key)
			.ToList();

		foreach (var tab in tabsToSleep)
		{
			await PutTabToSleep(tab);
		}
	}

	private bool IsWebContentTab(GraphiteTabViewItem tab)
	{
		if (tab.Content is Frame frame && frame.Content is WebContent)
		{
			return true;
		}
		return false;
	}

	public async Task PutTabToSleep(GraphiteTabViewItem tab)
	{
		if (tab == _activeTab)
		{
			return; // Don't put the active tab to sleep
		}

		_tabViewContainer.DispatcherQueue.TryEnqueue(async () =>
		{
			if (tab.Content is Frame frame && frame.Content is WebContent webContent)
			{
				await webContent.UnloadContent();
				if (!tab.Header.ToString().StartsWith("Sleeping - "))
				{
					tab.Header = "Sleeping - " + tab.Header.ToString();
				}

				TabPutToSleep?.Invoke(this, tab);
			}
			_lastActivityTimes.Remove(tab);
		});

		await Task.Delay(200);
	}

	public void UpdateTabActivity(GraphiteTabViewItem tab)
	{
		_lastActivityTimes[tab] = DateTime.Now;
		_activeTab = tab;
	}

	public async Task WakeUpTab(GraphiteTabViewItem tab)
	{
		_tabViewContainer.DispatcherQueue.TryEnqueue(async () =>
		{
			if (tab.Content is Frame frame && frame.Content is WebContent webContent)
			{
				await webContent.ReloadContent();
				tab.Header = tab.Header.ToString().Replace("Sleeping - ", "");
				UpdateTabActivity(tab);
			}
		});
	}

	public void CloseTab(GraphiteTabViewItem tab)
	{
		if (tab != null)
		{
			// Remove from group if it belongs to one
			if (tab.Group != null)
			{
				RemoveTabFromGroup(tab);
			}

			_tabViewContainer.TabItems.Remove(tab);
			_lastActivityTimes.Remove(tab);

			if (tab.Content is Frame frame)
			{
				if (frame.Content is WebContent webContent)
				{
					if (webContent.WebView != null)
					{
						webContent.WebView.Close();
					}
				}
				else if (frame.Content is NewTab newTab)
				{
					// Perform any necessary cleanup for NewTab
					newTab.Cleanup(); // Assuming NewTab has a Cleanup method
				}

				frame.Content = null;
			}

			tab.Content = null;
		}
	}

	public void UpdateTabIcon(GraphiteTabViewItem tab, string faviconUrl)
	{
		if (tab == null)
			return;

		_tabViewContainer.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
		{
			try
			{
				if (!string.IsNullOrEmpty(faviconUrl))
				{
					var uri = new Uri(faviconUrl);
					tab.IconSource = new ImageIconSource
					{
						ImageSource = new BitmapImage(uri)
					};
				}
				else
				{
					tab.IconSource = new SymbolIconSource { Symbol = Symbol.Document };
				}
			}
			catch (Exception)
			{
				// If there's any error, fall back to the default icon
				tab.IconSource = new SymbolIconSource { Symbol = Symbol.Document };
			}
		});
	}

	private async void OnTabSelected(GraphiteTabViewItem tab)
	{
		if (tab.Header.ToString().StartsWith("Sleeping - "))
		{
			await WakeUpTab(tab);
		}
		UpdateTabActivity(tab);
	}

	// Tab Group Management Methods

	public void AddTabToGroup(GraphiteTabViewItem tabItem, string groupName)
	{
		// Find or create the group
		var group = TabGroups.FirstOrDefault(g => g.Name == groupName);
		if (group == null)
		{
			group = new GraphiteTabGroup { Name = groupName };
			TabGroups.Add(group);
		}

		// Remove from previous group if any
		if (tabItem.Group != null && tabItem.Group != group)
		{
			tabItem.Group.Tabs.Remove(tabItem);
			if (tabItem.Group.Tabs.Count == 0)
			{
				TabGroups.Remove(tabItem.Group);
			}
		}

		// Add to new group
		tabItem.Group = group;
		if (!group.Tabs.Contains(tabItem))
		{
			group.Tabs.Add(tabItem);
		}

		// Apply group style
		tabItem.ApplyGroupStyle(group);
	}

	public void RemoveTabFromGroup(GraphiteTabViewItem tabItem)
	{
		if (tabItem.Group == null)
			return;

		var group = tabItem.Group;
		group.Tabs.Remove(tabItem);
		tabItem.Group = null;

		// Remove empty group
		if (group.Tabs.Count == 0)
		{
			TabGroups.Remove(group);
		}

		// Reset group style
		tabItem.ResetGroupStyle();
	}

	public void CreateTabGroup(IEnumerable<GraphiteTabViewItem> tabs, string groupName)
	{
		var group = new GraphiteTabGroup { Name = groupName };
		TabGroups.Add(group);

		foreach (var tab in tabs)
		{
			AddTabToGroup(tab, groupName);
		}
	}

	public void UnGroupTabs(string groupName)
	{
		var group = TabGroups.FirstOrDefault(g => g.Name == groupName);
		if (group != null)
		{
			foreach (var tab in group.Tabs.ToList())
			{
				RemoveTabFromGroup(tab);
			}
		}
	}

	public IEnumerable<GraphiteTabViewItem> SearchTabs(string searchTerm)
	{
		return _tabViewContainer.TabItems.OfType<GraphiteTabViewItem>()
			.Where(tab =>
				(tab.Header?.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
				GetTabUrl(tab).Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
	}

	public async Task PutTabToSleepByItem(GraphiteTabViewItem tab)
	{
		if (tab != _activeTab && !tab.IsPinned)
		{
			await PutTabToSleep(tab);
		}
	}

	public async Task WakeUpTabByItem(GraphiteTabViewItem tab)
	{
		if (tab.Header.ToString().StartsWith("Sleeping - "))
		{
			await WakeUpTab(tab);
		}
	}

	public async Task ToggleTabSleep(GraphiteTabViewItem tab)
	{
		if (tab.Header.ToString().StartsWith("Sleeping - "))
		{
			await WakeUpTabByItem(tab);
		}
		else
		{
			await PutTabToSleepByItem(tab);
		}
	}

	public void ToggleTabPin(GraphiteTabViewItem tab)
	{
		if (tab != null)
		{
			tab.IsPinned = !tab.IsPinned;
			tab.UpdatePinStatus(tab.IsPinned);
			ReorderPinnedTabs();
		}
	}

	public void ReorderPinnedTabs()
	{
		var pinnedTabs = _tabViewContainer.TabItems.OfType<GraphiteTabViewItem>().Where(t => t.IsPinned).ToList();
		var unpinnedTabs = _tabViewContainer.TabItems.OfType<GraphiteTabViewItem>().Where(t => !t.IsPinned).ToList();

		_tabViewContainer.TabItems.Clear();

		foreach (var tab in pinnedTabs)
		{
			_tabViewContainer.TabItems.Add(tab);
		}

		foreach (var tab in unpinnedTabs)
		{
			_tabViewContainer.TabItems.Add(tab);
		}
	}

	public static List<TabState> GetStoredTabStates(string username)
	{
		var localSettings = ApplicationData.Current.LocalSettings;
		string cacheKey = $"{username}_{TabStateKey}";

		if (localSettings.Values.TryGetValue(cacheKey, out object jsonObj))
		{
			var json = jsonObj as string;
			return JsonSerializer.Deserialize<List<TabState>>(json) ?? new List<TabState>();
		}

		return new List<TabState>();
	}
}

