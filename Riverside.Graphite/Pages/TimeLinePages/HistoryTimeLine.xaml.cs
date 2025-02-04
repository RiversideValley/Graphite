using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.ViewModels.DataGetters;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Riverside.Graphite.MainWindow;


namespace Riverside.Graphite.Pages.TimeLinePages;
public sealed partial class HistoryTimeLine : Page
{
	private readonly User _user = AuthService.CurrentUser;
	
	public IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem> _browserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(new BrowserHistoryCollection());

	public HistoryTimeLine()
	{
		InitializeComponent();
		FetchBrowserHistory();
	}
    


	private void FetchBrowserHistory()
	{
		try
		{
			var graphiteHistory = new BrowserHistoryCollection();
			_browserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(graphiteHistory);
			BigTemp.ItemsSource = _browserHistory;

		}
		catch (Exception ex)
		{
			ExceptionLogger.LogException(ex);
		}
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		FetchBrowserHistory();
		_ = Ts.Focus(FocusState.Programmatic);
	}

	private void FilterBrowserHistory(string searchText)
	{
		if (_browserHistory == null)
		{
			return;
		}

		// Filter and bind the browser history based on the search text
		BigTemp.ItemsSource = new ObservableCollection<HistoryItem>(
			_browserHistory.Where(item =>
				item.Url.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
				item.Title?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true));
	}

	private void Ts_TextChanged(object sender, TextChangedEventArgs e)
	{
		FilterBrowserHistory(Ts.Text);
	}

	private async Task ClearDb()
	{
		HistoryActions historyActions = new(_user.Username);
		await historyActions.DeleteAllHistoryItems();
		BigTemp.ItemsSource = null;
	}

	private async void Delete_Click(object sender, RoutedEventArgs e)
	{
		await ClearDb();
	}

	private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		if (((FrameworkElement)sender).DataContext is HistoryItem historyItem)
		{
			ShowContextMenu(historyItem.Url, (FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
		}
	}

	private void ShowContextMenu(string selectedHistoryItem, FrameworkElement sender, Windows.Foundation.Point position)
	{
		MenuFlyout flyout = new();
		MenuFlyoutItem deleteMenuItem = new()
		{
			Text = "Delete This Record",
			Icon = new FontIcon { Glyph = "\uE74D" }
		};

		deleteMenuItem.Click += async (s, args) =>
		{
			HistoryActions historyActions = new(_user.Username);
			await historyActions.DeleteHistoryItem(selectedHistoryItem);
			RemoveHistoryItem(selectedHistoryItem);
		};

		flyout.Items.Add(deleteMenuItem);
		flyout.ShowAt(sender, position);
	}

	private void RemoveHistoryItem(string selectedHistoryItem)
	{
		if (BigTemp.ItemsSource is ObservableCollection<HistoryItem> historyItems)
		{
			HistoryItem itemToRemove = historyItems.FirstOrDefault(item => item.Url == selectedHistoryItem);
			if (itemToRemove != null)
			{
				_ = historyItems.Remove(itemToRemove);
			}
		}
	}

	private void BigTemp_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0)
		{
			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				if (e.AddedItems.FirstOrDefault() is HistoryItem historyItem)
				{
					window.NavigateToUrl(historyItem.Url);
				}
			}
		}
	}
}
