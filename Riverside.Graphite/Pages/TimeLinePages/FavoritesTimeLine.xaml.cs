using Riverside.Graphite.Core;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Data.Favorites;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace FireBrowserWinUi3.Pages.TimeLinePages;
public sealed partial class FavoritesTimeLine : Page
{
	public FavoritesTimeLine()
	{
		InitializeComponent();
		LoadFavs();
	}

	public Riverside.Graphite.Core.User user = AuthService.CurrentUser;
	public FavManager fs = new();
	private string ctmtext;
	private string ctmurl;

	public void LoadFavs()
	{
		List<FavItem> favorites = fs.LoadFav();
		FavoritesListView.ItemsSource = favorites;
	}

	private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		TextBox textbox = sender as TextBox;

		List<FavItem> favorites = fs.LoadFav();
		// Get all ListView items with the submitted search query
		IEnumerable<FavItem> SearchResults = from s in favorites where s.Title.Contains(textbox.Text, StringComparison.OrdinalIgnoreCase) select s;
		// Set SearchResults as ItemSource for HistoryListView
		FavoritesListView.ItemsSource = SearchResults;
	}

	private void FavoritesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		ListView listView = sender as ListView;
		FlyoutShowOptions options = new()
		{
			Position = e.GetPosition(listView),
		};
		FavoritesContextMenu.ShowAt(listView, options);
		FavItem item = ((FrameworkElement)e.OriginalSource).DataContext as FavItem;
		ctmtext = item.Title;
		ctmurl = item.Url;
	}


	private void Button_Click(object sender, RoutedEventArgs e)
	{
		FavManager fs = new();
		fs.ClearFavs();
		LoadFavs();
	}
	private async void FavContextItem_Click(object sender, RoutedEventArgs e)
	{
		switch ((sender as AppBarButton).Tag)
		{
			case "OpenLnkInNewWindow":
				_ = await Launcher.LaunchUriAsync(new Uri($"{ctmurl}"));
				break;
			case "Copy":
				ClipBoard.WriteStringToClipboard(ctmurl);
				break;
			case "CopyText":
				ClipBoard.WriteStringToClipboard(ctmtext);
				break;
			case "DeleteSingleRecord":
				FavManager fs = new();
				FavItem selectedItem = new() { Url = ctmurl, Title = ctmtext };
				fs.RemoveFavorite(selectedItem);
				LoadFavs();
				break;
				// Add other cases as needed
		}
		FavoritesContextMenu.Hide();
	}

	private void FavoritesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0)
		{
			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				if (e.AddedItems.FirstOrDefault() is FavItem favItem)
				{
					window.NavigateToUrl(favItem.Url);
				}
			}
		}
	}
}