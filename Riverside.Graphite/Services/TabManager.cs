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
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services;

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
	public CancellationToken _CancellationToken { get; set; }	

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
	}	

	public Task<bool> GetCurrentTabs()
	{
		CurrentTabs =  new Dictionary<GraphiteTabViewItem, Guid>();

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
				UpdateTabIcon(newTab,webContent?.WebView.CoreWebView2.FaviconUri);

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

	public GraphiteTabViewItem CreateNewTab(Type pageType = null, object parameter = null, bool isSplitViewActive = false, string username = null, Guid? idTag = null )
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
			Margin = new Thickness(2,48,2,2)
		};

		frame.Navigate(pageType, parameter);

		return frame;
	}

	public Task<string> SaveTabStateAsync(string username)
	{
		// need this to be thread safe
		var obj = new SemaphoreSlim(1, 1);

		obj.Wait();
		try
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
				};

				tabStates.Add(state);
			}

			var json = JsonSerializer.Serialize(tabStates);
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[$"{username}_{TabStateKey}"] = json;
			return Task.FromResult(json ?? null);
		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);	
		}
		finally
		{
			obj.Release();
		}

		return null;
		
	}

	public Task RestoreTabsAsync(string username)
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
		_sleepTimer.Tick += async (s, e) =>
		{
			if (_CancellationToken.IsCancellationRequested)
			{
				_sleepTimer.Stop();
				return;
			}

			await CheckAndSleepInactiveTabs();
		};
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

	public void CreateTabGroup(IEnumerable<GraphiteTabViewItem> tabs, string groupName)
	{
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


