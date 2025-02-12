using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NuGet.Protocol.Plugins;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Actions.Contracts;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Data.Core.Models.Contacts;
using Riverside.Graphite.Data.Favorites;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Pages.TimeLinePages;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.ViewModels.DataGetters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Riverside.Graphite.Services.ViewModels
{
	public partial class HistoryViewModel : ObservableRecipient, INavigationAware
	{
		[ObservableProperty]
		public IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem> _browserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(new BrowserHistoryCollection());

		[ObservableProperty]
		private HistoryItem _SelectedHistoryItem;

		[ObservableProperty]
		private HistoryItem _InternalHistoryItem;

		[ObservableProperty]
		private string _FilterText; 

		public HistoryTimeLine ParentHistoryTimeLine { get; set; }
		public HistoryViewModel(IMessenger messenger): base(messenger) {

			FetchBrowserHistory();
		}

		partial void OnFilterTextChanged(string value)
		{
			if (value is null) return;
			FilterBrowserHistory(value);
		}
		partial void OnSelectedHistoryItemChanged(HistoryItem value)
		{
			if (value is null) return;

			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				window.DispatcherQueue.TryEnqueue(() => {
					window.NavigateToUrl(SelectedHistoryItem.Url);
				});
			}
		}
		private void FilterBrowserHistory(string searchText)
		{
			if (BrowserHistory == null)
			{
				return;
			}

			var graphiteHistory = new BrowserHistoryCollection();
			
			

			// Filter and bind the browser history based on the search text
			var filter = 
				BrowserHistory.Where(item =>
					item.Url.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
					item.Title?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true);

			graphiteHistory.HistoryItems = filter.ToObservableCollection(); 

			BrowserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(graphiteHistory);

			OnPropertyChanged(nameof(BrowserHistory));
		}

		public void Ts_TextChanged(object sender, TextChangedEventArgs e)
		{
			FilterBrowserHistory(FilterText);
		}
		private void FetchBrowserHistory()
		{
			try
			{
				var graphiteHistory = new BrowserHistoryCollection();
				BrowserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(graphiteHistory);
				

			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
		}

		[RelayCommand]
		private async Task CollectionShow(AppBarButton sender) {

			
			HistoryActions historyActions = new(AuthService.CurrentUser.Username);

			MenuFlyout flyout = new();
			flyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;

			var subMenu = new MenuFlyoutSubItem
			{
				Text = "Collections",
				Icon = new FontIcon { Glyph = "\xe71d" }, 
			};

			var list = await historyActions.GetAllCollectionNamesItems();
			foreach (var item in list)
			{
				var menuItem = new MenuFlyoutItem()
				{
					Text = item.Name,
					Icon = new FontIcon { Glyph = $"{item.Name.Substring(0, 1)}", FontSize = 24, FontFamily = new FontFamily("Segoe UI"), FontStyle = Windows.UI.Text.FontStyle.Italic },
					Background = RandomColors.GetRandomSolidColorBrush(),
					Opacity = .9

				};
				menuItem.Click += async (s, args) =>
				{
					var answer = await historyActions.InsertCollectionsItem(InternalHistoryItem, item);

					if (answer)
					{
						Notification note = new()
						{
							Title = $"Added To Collection",
							Message = item.Name,
							Severity = InfoBarSeverity.Informational,
							Duration = TimeSpan.FromSeconds(1.5)
						};
						
						if (App.Current.m_window is MainWindow win) {

							_ = win.NotificationQueue.Show(note);
							win.ViewModelMain.SendMessageOut(new Message_Settings_Actions(EnumMessageStatus.Collections));

						}
						
					}
					else
					{
						Notification note = new()
						{
							Title = $"Already In Collection",
							Message = item.Name,
							Severity = InfoBarSeverity.Informational,
							Duration = TimeSpan.FromSeconds(1.5)
						};
						if (App.Current.m_window is MainWindow win)
						{
							_ = win.NotificationQueue.Show(note);
						}
						
					};
					flyout.Hide(); 
				};
				subMenu.Items.Add(menuItem);
			}
			
			GeneralTransform transform = sender.TransformToVisual(sender);
			Point point = transform.TransformPoint(new Point(0, 0));

			flyout.Items.Add(subMenu);
			flyout.ShowAt((FrameworkElement)sender,  point);

		}

		[RelayCommand]
		private async Task DeleteItem(AppBarButton sender) {

			if (InternalHistoryItem is null) return; 

			
			HistoryActions historyActions = new(AuthService.CurrentUser.Username);
			await historyActions.DeleteHistoryItem(InternalHistoryItem.Url);

			if (sender.Parent is CommandBarFlyout cmdBar)
			{
				cmdBar.Hide();
			}
			InternalHistoryItem = null; 
			FetchBrowserHistory();
		
		}
		public void ShowContextMenu(object sender, RightTappedRoutedEventArgs e)
		{
			ListView listView = sender as ListView;
			FlyoutShowOptions options = new()
			{
				Position = e.GetPosition(listView),
			};
			
			ParentHistoryTimeLine.FlyDeleteItem.ShowAt(listView, options);
			InternalHistoryItem = ((FrameworkElement)e.OriginalSource).DataContext as HistoryItem;
			e.Handled = true; 
		}
				
		[RelayCommand]
		private async Task RemoveAllHistory() {

			HistoryActions historyActions = new(AuthService.CurrentUser.Username);
			await historyActions.DeleteAllHistoryItems();
			FetchBrowserHistory(); 
		}
		public void OnNavigatedFrom()
		{
			;
		}

		public void OnNavigatedTo(object parameter)
		{
			FetchBrowserHistory(); 
		}
	}
}
