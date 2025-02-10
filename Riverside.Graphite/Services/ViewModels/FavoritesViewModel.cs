using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Plugins;
using Riverside.Graphite.Data.Favorites;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Services.Contracts;
using Riverside.Graphite.Services.ViewModels.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
		public FavoritesViewModel() {
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
		public  void OnNavigatedTo(object parameter)
		{
			;
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
