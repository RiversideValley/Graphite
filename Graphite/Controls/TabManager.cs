using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text.Json;
using Graphite.Pages;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Concurrent;
using Graphite.Helpers;
using Graphite.UserSys;

namespace Graphite.Controls
{
	public class TabManager
	{
		public GraphiteTabViewContainer _tabViewContainer;
		public const string TabStateKey = "TabState";
		private int _sleepTimeoutMinutes;
		public Dictionary<GraphiteTabViewItem, DateTime> _lastActivityTimes = new Dictionary<GraphiteTabViewItem, DateTime>();
		private DispatcherQueueTimer _sleepTimer;
		private DispatcherQueueTimer _previewTimer;
		private GraphiteTabViewItem _hoveredTab;
		private GraphiteTabViewItem _activeTab;
		private ConcurrentQueue<GraphiteTabViewItem> _preloadedTabs = new ConcurrentQueue<GraphiteTabViewItem>();
		private const int MAX_PRELOADED_TABS = 1;
		private string _defaultUrl = "about:blank";
		private bool _isRestoringTabs = false;
		private bool _tabGroupingEnabled;
		private bool _tabPreloading;
		private int _tabMaxNumber;
		private bool _tabAutoRestore;
		private string _currentUsername;

		public event EventHandler<GraphiteTabViewItem> TabPutToSleep;

		public TabManager(GraphiteTabViewContainer tabViewContainer, string username)
		{
			_tabViewContainer = tabViewContainer;
			_currentUsername = username;
			_tabViewContainer.TabItemsChanged += TabViewContainer_TabItemsChanged;
			_tabViewContainer.SelectionChanged += TabViewContainer_SelectionChanged;
			InitializeSettings(username);
			InitializeSleepTimer();
			InitializePreviewTimer();
		}

		private async void InitializeSettings(string username)
		{
			_sleepTimeoutMinutes = await SettingsManager.GetSettingAsync<int>(username, "TabSleepTime", 30);
			_tabGroupingEnabled = await SettingsManager.GetSettingAsync<bool>(username, "TabGroupingEnabled", false);
			_tabPreloading = await SettingsManager.GetSettingAsync<bool>(username, "TabPreloading", true);
			_tabMaxNumber = await SettingsManager.GetSettingAsync<int>(username, "GenaralTabMaxNumber", 50);
			_tabAutoRestore = await SettingsManager.GetSettingAsync<bool>(username, "GenaralTabAutoRestore", true);
		}

		private void TabViewContainer_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
			{
				var newTab = sender.TabItems[(int)args.Index] as GraphiteTabViewItem;
				if (newTab != null)
				{
					SetupTabPreview(newTab);
				}
			}
		}

		private void TabViewContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is TabView tabView && tabView.SelectedItem is GraphiteTabViewItem selectedTab)
			{
				OnTabSelected(selectedTab);
			}
		}

		private void SetupTabPreview(GraphiteTabViewItem tab)
		{
			tab.PointerEntered += Tab_PointerEntered;
			tab.PointerExited += Tab_PointerExited;
			tab.PointerPressed += Tab_PointerPressed;
		}

		private void InitializePreviewTimer()
		{
			_previewTimer = _tabViewContainer.DispatcherQueue.CreateTimer();
			_previewTimer.Interval = TimeSpan.FromSeconds(0.5);
			_previewTimer.Tick += PreviewTimer_Tick;
		}

		private void Tab_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_hoveredTab = sender as GraphiteTabViewItem;
			_previewTimer.Start();
		}

		private void Tab_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_hoveredTab = null;
			_previewTimer.Stop();
			HideTabPreview(sender as GraphiteTabViewItem);
		}

		private void Tab_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_previewTimer.Stop();
			HideTabPreview(sender as GraphiteTabViewItem);
		}

		private void PreviewTimer_Tick(DispatcherQueueTimer sender, object args)
		{
			_previewTimer.Stop();
			if (_hoveredTab != null)
			{
				ShowTabPreview(_hoveredTab);
			}
		}

		private void ShowTabPreview(GraphiteTabViewItem tab)
		{
			// Implementation of ShowTabPreview
		}

		private void HideTabPreview(GraphiteTabViewItem tab)
		{
			tab.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
			{
				FlyoutBase.GetAttachedFlyout(tab)?.Hide();
			});
		}

		private async Task PreloadTabAsync()
		{
			if (_tabPreloading && _preloadedTabs.Count < MAX_PRELOADED_TABS && !_isRestoringTabs)
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
						newTab.Content = new Frame { Content = newTabContent };
						_preloadedTabs.Enqueue(newTab);
					});
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error preloading tab: {ex.Message}");
				}
			}
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

				try
				{
					WebContent webContent;
					if (newTab.Content is WebContent existingContent)
					{
						webContent = existingContent;
						await webContent.NavigateToUrl(url);
					}
					else
					{
						webContent = new WebContent();
						await webContent.NavigateToUrl(url);
					}

					if (!(newTab.Content is WebContent))
					{
						newTab.Content = webContent;
					}
					newTab.Header = webContent.Title;
					UpdateTabIcon(newTab, webContent.FaviconUrl);
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
			if (_tabPreloading && !_isRestoringTabs && _preloadedTabs.Count == 0)
			{
				await PreloadTabAsync();
			}
		}

		public GraphiteTabViewItem CreateNewTab(Type pageType = null, object parameter = null, bool isSplitViewActive = false, string username = null)
		{
			GraphiteTabViewItem newItem;

			if (_preloadedTabs.TryDequeue(out var preloadedTab))
			{
				newItem = preloadedTab;
				newItem.Header = $"Home Page - {_currentUsername}";
				newItem.IconSource = new SymbolIconSource { Symbol = Symbol.Home };
				newItem.Style = (Style)Application.Current.Resources["FloatingTabViewItemStyle"];
			}
			else
			{
				newItem = new GraphiteTabViewItem()
				{
					Header = $"Home Page - {_currentUsername}",
					IconSource = new SymbolIconSource { Symbol = Symbol.Home },
					Style = (Style)Application.Current.Resources["FloatingTabViewItemStyle"]
				};
			}

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
				if (!(newItem.Content is Frame))
				{
					newItem.Content = CreateFrame(pageType, parameter);
				}
				else
				{
					((Frame)newItem.Content).Navigate(pageType ?? typeof(WebContent), passer);
				}
			}


			_tabViewContainer.AddTab(newItem);
			UpdateTabActivity(newItem);
			//SetupTabPreview(newItem);
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
				Margin = new Thickness(0, 37, 0, 0)
			};

			frame.Navigate(pageType, parameter);

			return frame;
		}

		public async Task SaveTabStateAsync(string username)
		{
			var tabStates = new List<TabState>();

			foreach (GraphiteTabViewItem tab in _tabViewContainer.TabItems)
			{
				var state = new TabState
				{
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
				};

				tabStates.Add(state);
			}

			var json = JsonSerializer.Serialize(tabStates);
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[$"{username}_{TabStateKey}"] = json;
		}

		public async Task RestoreTabsAsync(string username)
		{
			_isRestoringTabs = true;
			var localSettings = ApplicationData.Current.LocalSettings;
			if (localSettings.Values.TryGetValue($"{username}_{TabStateKey}", out object jsonObj))
			{
				var json = jsonObj as string;
				var tabStates = JsonSerializer.Deserialize<List<TabState>>(json);

				_tabViewContainer.DispatcherQueue.TryEnqueue(async () =>
				{
					_tabViewContainer.TabItems.Clear();
					_lastActivityTimes.Clear();

					foreach (var state in tabStates)
					{
						GraphiteTabViewItem newTab;
						if (state.Url == "about:newtab" || string.IsNullOrEmpty(state.Url))
						{
							newTab = CreateNewTab(typeof(NewTab), null, state.IsSplitView, username);
						}
						else
						{
							newTab = CreateNewTab(typeof(WebContent), state.Url, state.IsSplitView, username);
						}

						if (state.IsSleeping)
						{
							await PutTabToSleep(newTab);
						}
						else if (newTab.Content is Frame frame)
						{
							if (frame.Content is WebContent webContent)
							{
								await webContent.NavigateToUrl(state.Url);
								UpdateTabIcon(newTab, state.FaviconUrl);
							}
							else if (frame.Content is NewTab)
							{
								// Update NewTab content if necessary
							}
						}
						newTab.Header = state.Header;
						newTab.IsPinned = state.IsPinned;
						SetTabColor(newTab, state.CustomColor);
						_lastActivityTimes[newTab] = state.LastAccessTime;
						SetTabScrollPosition(newTab, state.ScrollPosition);
					}
				});
			}
			_isRestoringTabs = false;
		}


		private string GetTabUrl(GraphiteTabViewItem tab)
		{
			if (tab.Content is Frame frame)
			{
				if (frame.Content is WebContent webContent)
				{
					return webContent.CurrentUrl;
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
						return primaryWebContent.CurrentUrl;
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
				return webContent.FaviconUrl;
			}
			else if (tab.Content is SplitViewContainer splitView)
			{
				if (splitView.PrimaryContent is Frame primaryFrame && primaryFrame.Content is WebContent primaryWebContent)
				{
					return primaryWebContent.FaviconUrl;
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
				return webContent.Title;
			}
			return tab.Header.ToString();
		}

		private string GetTabColor(GraphiteTabViewItem tab)
		{
			// Implement logic to get tab color
			return "";
		}

		private void SetTabColor(GraphiteTabViewItem tab, string color)
		{
			// Implement logic to set tab color
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
				.Where(kvp => (now - kvp.Value).TotalMinutes >= _sleepTimeoutMinutes &&
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
				_tabViewContainer.TabItems.Remove(tab);
				_lastActivityTimes.Remove(tab);

				if (tab.Content is Frame frame)
				{
					if (frame.Content is WebContent webContent)
					{
						if (webContent._webView != null)
						{
							webContent._webView.Close();
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

		private void OnTabSelected(GraphiteTabViewItem tab)
		{
			if (tab.Header.ToString().StartsWith("Sleeping - "))
			{
				WakeUpTab(tab);
			}
			UpdateTabActivity(tab);
		}

		public void CreateTabGroup(IEnumerable<GraphiteTabViewItem> tabs, string groupName)
		{
			if (!_tabGroupingEnabled) return;

			var tabGroup = new TabViewItem
			{
				Header = groupName,
				IsSelected = true
			};

			var subTabView = new TabView();
			foreach (var tab in tabs)
			{
				_tabViewContainer.TabItems.Remove(tab);
				subTabView.TabItems.Add(tab);
			}

			tabGroup.Content = subTabView;
			_tabViewContainer.TabItems.Add(tabGroup);
		}

		public void UnGroupTabs(TabViewItem groupTab)
		{
			if (groupTab.Content is TabView subTabView)
			{
				int insertIndex = _tabViewContainer.TabItems.IndexOf(groupTab);
				foreach (GraphiteTabViewItem tab in subTabView.TabItems)
				{
					_tabViewContainer.TabItems.Insert(insertIndex, tab);
					insertIndex++;
				}
				_tabViewContainer.TabItems.Remove(groupTab);
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

	public class Passer
	{
		public GraphiteTabViewItem Tab { get; set; }
		public TabView TabView { get; set; }
		public ToolbarViewModel ViewModel { get; set; }
		public object Param { get; set; }
	}

	public class ToolbarViewModel
	{
		public string CurrentAddress { get; set; }
	}


}