using FireBrowserWinUi3DataCore.Actions;
using FireBrowserWinUi3DataCore.Models;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Pages.TimeLinePages;
public sealed partial class HistoryTimeLine : Page
{
    private readonly User _user = AuthService.CurrentUser;
    private ObservableCollection<FireBrowserDatabase.HistoryItem> _browserHistory;

    public HistoryTimeLine()
    {
        InitializeComponent();
        FetchBrowserHistory();
    }

    private async void FetchBrowserHistory()
    {
        try
        {
            var historyActions = new HistoryActions(_user.Username);
            _browserHistory = await historyActions.GetAllHistoryItems();
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
        Ts.Focus(FocusState.Programmatic);
    }

    private void FilterBrowserHistory(string searchText)
    {
        if (_browserHistory == null) return;

        // Filter and bind the browser history based on the search text
        BigTemp.ItemsSource = new ObservableCollection<FireBrowserDatabase.HistoryItem>(
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
        var historyActions = new HistoryActions(_user.Username);
        await historyActions.DeleteAllHistoryItems();
        BigTemp.ItemsSource = null;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e) => await ClearDb();

    private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is FireBrowserDatabase.HistoryItem historyItem)
        {
            ShowContextMenu(historyItem.Url, (FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
        }
    }

    private void ShowContextMenu(string selectedHistoryItem, FrameworkElement sender, Windows.Foundation.Point position)
    {
        var flyout = new MenuFlyout();
        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "Delete This Record",
            Icon = new FontIcon { Glyph = "\uE74D" }
        };

        deleteMenuItem.Click += async (s, args) =>
        {
            var historyActions = new HistoryActions(_user.Username);
            await historyActions.DeleteHistoryItem(selectedHistoryItem);
            RemoveHistoryItem(selectedHistoryItem);
        };

        flyout.Items.Add(deleteMenuItem);
        flyout.ShowAt(sender, position);
    }

    private void RemoveHistoryItem(string selectedHistoryItem)
    {
        if (BigTemp.ItemsSource is ObservableCollection<FireBrowserDatabase.HistoryItem> historyItems)
        {
            var itemToRemove = historyItems.FirstOrDefault(item => item.Url == selectedHistoryItem);
            if (itemToRemove != null)
            {
                historyItems.Remove(itemToRemove);
            }
        }
    }

    private void BigTemp_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
            if (Application.Current is App app && app.m_window is MainWindow window)
            {
                if (e.AddedItems.FirstOrDefault() is FireBrowserDatabase.HistoryItem historyItem)
                    window.NavigateToUrl(historyItem.Url);
            }
    }
}
