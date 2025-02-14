using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Plugins;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Favorites;
using Riverside.Graphite.Helpers;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.Messages;
using Riverside.Graphite.Services.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.System;

namespace Riverside.Graphite.Services.ViewModels
{
	public partial class FavoritesViewModel : ObservableRecipient, INavigationAware
	{
		[ObservableProperty]
		private string _SearchFilterText; 
		
		[ObservableProperty]
		private ObservableCollection<FavItem> _Favorites;

		[ObservableProperty]
		private FavItem _SelectedFavoriteItem;

		[ObservableProperty]
		private FavItem _InternalFavoriteItem;

		public CommandBarFlyout FavoritesContextMenu { get; set; }
		internal FavManager FavManager { get; set; }	
		public FavoritesViewModel(IMessenger messenger):base(messenger) {
			FavManager = new();
		}

		static string oldValue { get; set; }
		async partial void OnSearchFilterTextChanged(string value)
		{
			if (oldValue == value) return;	

			if (string.IsNullOrEmpty(value))
			{
				await LoadFavorites();
				return;
			}

			var temp = FavManager.LoadFav().Where(t => t.Title.Contains(value, StringComparison.OrdinalIgnoreCase)).ToObservableCollection();
			Favorites = temp;
			RaisePropertyChanges(nameof(Favorites));
		
			await Task.Delay(50);
			oldValue = value; 
		}

		partial void OnSelectedFavoriteItemChanged(FavItem value)
		{
			if (value is null) return;

			InternalFavoriteItem = value; 

			if (Application.Current is App app && app.m_window is MainWindow window)
			{
				window.DispatcherQueue?.TryEnqueue(async() => {
					window.NavigateToUrl(value.Url);
					await Task.Delay(200); 
				});	
				
			}
		}

		[RelayCommand]
		private async Task CollectionShow(AppBarButton sender)
		{


			HistoryActions historyActions = new(AuthService.CurrentUser.Username);

			MenuFlyout flyout = new();

			flyout.Placement = FlyoutPlacementMode.LeftEdgeAlignedTop;

			var subMenu = new MenuFlyoutSubItem
			{
				Margin = new Thickness(2),
				Padding = new Thickness(1),
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
					var items = await historyActions.GetAllHistoryItems();

					var historyItem = items.Where(i => i.Url == InternalFavoriteItem.Url).FirstOrDefault();

					var answer = default(bool);

					if (historyItem != null)
					{
						answer = await historyActions.InsertCollectionsItem(historyItem, item);
					}
					else {

						await historyActions.InsertHistoryItem(InternalFavoriteItem.Url, InternalFavoriteItem.Title, 0, 0, 0);
						var items2 = await historyActions.GetAllHistoryItems();
						var historyItem2 = items2.Where(i => i.Url == InternalFavoriteItem.Url).FirstOrDefault();

						if (historyItem2 != null)
						{
							answer = await historyActions.InsertCollectionsItem(historyItem2, item);
						}

					}

					if (answer)
					{
						Notification note = new()
						{
							Title = $"Added To Collection",
							Message = item.Name,
							Severity = InfoBarSeverity.Informational,
							Duration = TimeSpan.FromSeconds(1.5)
						};

						if (App.Current.m_window is MainWindow win)
						{

							_ = win.NotificationQueue.Show(note);
							Messenger.Send(new Message_Settings_Actions(EnumMessageStatus.Collections));

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

			GeneralTransform transform =  sender.TransformToVisual(sender);

			Point point = transform.TransformPoint(new Point(0, 0));

			flyout.Items.Add(subMenu);
			flyout.ShowAt((FrameworkElement)sender, point);

		}

		[RelayCommand]
		private async Task DeleteAllFavorites() {
			
			FavManager.ClearFavs();
			await LoadFavorites();
		}

		[RelayCommand]
		private async Task FavoriteItemContextClick(AppBarButton sender) {

			if (InternalFavoriteItem is null) return; 

			switch ((sender as AppBarButton).Tag)
			{
				case "OpenLnkInNewWindow":
					_ = await Launcher.LaunchUriAsync(new Uri($"{InternalFavoriteItem.Url}"));
					break;
				case "Copy":
					ClipBoard.WriteStringToClipboard(InternalFavoriteItem.Url);
					break;
				case "CopyText":
					ClipBoard.WriteStringToClipboard(InternalFavoriteItem.Title);
					break;
				case "DeleteSingleRecord":
					FavManager fs = new();
					FavItem selectedItem = new() { Url = InternalFavoriteItem.Url, Title = InternalFavoriteItem.Title };
					fs.RemoveFavorite(selectedItem);
					await LoadFavorites();
					break;
					// Add other cases as needed
			}
			
			if(sender.Parent is CommandBarFlyout cmdBar){
				cmdBar.Hide();
			}
			
		}

		public Task LoadFavorites() {

			Favorites = FavManager.LoadFav().ToObservableCollection(); 
			OnPropertyChanged(nameof(Favorites));
			return Task.CompletedTask;  
		}

		public void RightTappedFavoriteItem(object sender, RightTappedRoutedEventArgs e)
		{
			ListView listView = sender as ListView;
			FlyoutShowOptions options = new()
			{
				Position = e.GetPosition(listView),
			};

			FavoritesContextMenu.ShowAt(listView, options);
			InternalFavoriteItem = ((FrameworkElement)e.OriginalSource).DataContext as FavItem;
				
		}
		public async void OnNavigatedTo(object parameter)
		{
			await LoadFavorites();
		}

		public void OnNavigatedFrom()
		{
			;
		}

		public void RaisePropertyChanges([CallerMemberName] string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}
	}
}
