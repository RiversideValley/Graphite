using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text.Json;
using Graphite.Controls;
using Graphite.Pages;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Graphite.Controls
{
	using Microsoft.UI;
	using Microsoft.UI.Xaml;
	using Microsoft.UI.Xaml.Media;
	using Microsoft.UI.Xaml.Shapes;

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

	public class TabManager
	{
		private GraphiteTabViewContainer _tabViewContainer;
		private const string TabStateKey = "TabState";
		private const int SleepTimeoutMinutes = 1;
		private Dictionary<GraphiteTabViewItem, DateTime> _lastActivityTimes = new Dictionary<GraphiteTabViewItem, DateTime>();
		private DispatcherQueueTimer _sleepTimer;
		private DispatcherQueueTimer _previewTimer;
		private GraphiteTabViewItem _hoveredTab;
		private GraphiteTabViewItem _activeTab;

		public event EventHandler<GraphiteTabViewItem> TabPutToSleep;

		public TabManager(GraphiteTabViewContainer tabViewContainer)
		{
			_tabViewContainer = tabViewContainer;
			_tabViewContainer.TabItemsChanged += TabViewContainer_TabItemsChanged;
			_tabViewContainer.SelectionChanged += TabViewContainer_SelectionChanged;
			InitializeSleepTimer();
			InitializePreviewTimer();
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
			_previewTimer.Interval = TimeSpan.FromSeconds(2);
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
			tab.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
			{
				if (tab.Content is Frame frame && frame.Content is WebContent webContent)
				{
					var previewContent = new Grid
					{
						Width = 300,
						RowDefinitions =
						{
							new RowDefinition { Height = GridLength.Auto },
							new RowDefinition { Height = GridLength.Auto },
							new RowDefinition { Height = GridLength.Auto }
						},
						Padding = new Thickness(16),
						RowSpacing = 8
					};

					if (!string.IsNullOrEmpty(webContent.FaviconUrl))
					{
						try
						{
							var favicon = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0) };
							favicon.Source = new BitmapImage(new Uri(webContent.FaviconUrl));

							var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
							titlePanel.Children.Add(favicon);

							if (!string.IsNullOrEmpty(webContent.Title))
							{
								titlePanel.Children.Add(new TextBlock
								{
									Text = webContent.Title,
									FontWeight = FontWeights.SemiBold,
									TextTrimming = TextTrimming.CharacterEllipsis,
									VerticalAlignment = VerticalAlignment.Center
								});
							}

							Grid.SetRow(titlePanel, 0);
							previewContent.Children.Add(titlePanel);
						}
						catch (UriFormatException)
						{
							// Invalid favicon URL, skip adding the favicon
						}
					}
					else if (!string.IsNullOrEmpty(webContent.Title))
					{
						var titleBlock = new TextBlock
						{
							Text = webContent.Title,
							FontWeight = FontWeights.SemiBold,
							TextTrimming = TextTrimming.CharacterEllipsis,
							Margin = new Thickness(0, 0, 0, 4)
						};
						Grid.SetRow(titleBlock, 0);
						previewContent.Children.Add(titleBlock);
					}

					if (!string.IsNullOrEmpty(webContent.CurrentUrl))
					{
						var urlBlock = new TextBlock
						{
							Text = webContent.CurrentUrl,
							TextWrapping = TextWrapping.Wrap,
							Opacity = 0.7,
							MaxLines = 2,
							TextTrimming = TextTrimming.CharacterEllipsis
						};
						Grid.SetRow(urlBlock, 1);
						previewContent.Children.Add(urlBlock);
					}

					var separator = new Rectangle
					{
						Height = 1,
						Fill = new SolidColorBrush(Colors.Gray),
						Opacity = 0.2,
						Margin = new Thickness(0, 8, 0, 8)
					};
					Grid.SetRow(separator, 2);
					previewContent.Children.Add(separator);

					var flyout = new Flyout
					{
						Content = previewContent,
						Placement = FlyoutPlacementMode.Bottom,
						ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
					};

					flyout.ShowAt(tab);
				}
			});
		}

		private void HideTabPreview(GraphiteTabViewItem tab)
		{
			tab.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
			{
				FlyoutBase.GetAttachedFlyout(tab)?.Hide();
			});
		}

		public GraphiteTabViewItem CreateNewTab(Type pageType = null, object parameter = null, bool isSplitViewActive = false)
		{
			_ = _tabViewContainer.TabItems.Count;


			GraphiteTabViewItem newItem = new()
			{
				Header = "NewTab",
				IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource { Symbol = Symbol.Home },
				Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["FloatingTabViewItemStyle"]
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
				newItem.Content = CreateFrame(pageType, parameter);
			}

			Passer passer = new()
			{
				Tab = newItem,
				TabView = _tabViewContainer,
				ViewModel = new ToolbarViewModel(),
				Param = parameter,
			};

		 

			passer.ViewModel.CurrentAddress = "";


			_tabViewContainer.AddTab(newItem);
			UpdateTabActivity(newItem);
			SetupTabPreview(newItem);
			_tabViewContainer.SelectedItem = newItem;
			return newItem;
		}

		private Frame CreateFrame(Type pageType, object parameter)
		{
			double margin = 37;
			Frame frame = new()
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Margin = new Thickness(0, margin, 0, 0)
			};

			if (pageType != null)
			{
				frame.Navigate(pageType, parameter);
			}
			else
			{
				frame.Navigate(typeof(NewTab), parameter);
			}

			return frame;
		}

		public async Task SaveTabStateAsync()
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
					FaviconUrl = GetTabFaviconUrl(tab)
				};

				tabStates.Add(state);
			}

			var json = JsonSerializer.Serialize(tabStates);
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[TabStateKey] = json;
		}

		public async Task RestoreTabsAsync()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			if (localSettings.Values.TryGetValue(TabStateKey, out object jsonObj))
			{
				var json = jsonObj as string;
				var tabStates = JsonSerializer.Deserialize<List<TabState>>(json);

				_tabViewContainer.TabItems.Clear();
				_lastActivityTimes.Clear();

				foreach (var state in tabStates)
				{
					var newTab = CreateNewTab(typeof(WebContent), state.Url, state.IsSplitView);
					if (state.IsSleeping)
					{
						await PutTabToSleep(newTab);
					}
					else if (newTab.Content is Frame frame && frame.Content is WebContent webContent)
					{
						await webContent.NavigateToUrl(state.Url);
						UpdateTabIcon(newTab, state.FaviconUrl);
					}
				}
			}
		}

		private string GetTabUrl(GraphiteTabViewItem tab)
		{
			if (tab.Content is Frame frame && frame.Content is WebContent webContent)
			{
				return webContent.CurrentUrl;
			}
			else if (tab.Content is SplitViewContainer splitView)
			{
				if (splitView.PrimaryContent is Frame primaryFrame && primaryFrame.Content is WebContent primaryWebContent)
				{
					return primaryWebContent.CurrentUrl;
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

			 tab.DispatcherQueue.TryEnqueue(async () =>
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
			if (tab.Content is Frame frame && frame.Content is WebContent webContent)
			{
				await webContent.ReloadContent();
				tab.Header = tab.Header.ToString().Replace("Sleeping - ", "");
				UpdateTabActivity(tab);
			}
		}

		public void UpdateTabIcon(GraphiteTabViewItem tab, string faviconUrl)
		{
			if (tab == null)
				return;

			try
			{
				if (!string.IsNullOrEmpty(faviconUrl))
				{
					var uri = new Uri(faviconUrl);
					tab.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
					{
						tab.IconSource = new ImageIconSource
						{
							ImageSource = new BitmapImage(uri)
						};
					});
				}
				else
				{
					tab.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
					{
						tab.IconSource = new SymbolIconSource { Symbol = Symbol.Document };
					});
				}
			}
			catch (Exception)
			{
				// If there's any error, fall back to the default icon
				tab.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
				{
					tab.IconSource = new SymbolIconSource { Symbol = Symbol.Document };
				});
			}
		}

		private void OnTabSelected(GraphiteTabViewItem tab)
		{
			if (tab.Header.ToString().StartsWith("Sleeping - "))
			{
				WakeUpTab(tab);
			}
			UpdateTabActivity(tab);
		}
	}

	public class TabState
	{
		public string Header { get; set; }
		public bool IsSleeping { get; set; }
		public string Url { get; set; }
		public bool IsSplitView { get; set; }
		public string FaviconUrl { get; set; }
	}
}

