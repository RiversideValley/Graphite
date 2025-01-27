using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI;
using Riverside.Graphite.Data.Core;
using Microsoft.UI.Xaml.Data;
using Riverside.Graphite.Controls;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;
using Riverside.Graphite.Services.ViewModels;
using Riverside.Graphite.Runtime.Helpers;

namespace Riverside.Graphite.ViewModels
{
    public class CollectionsPageViewModel : ObservableObject
    {
		private readonly HistoryActions _historyActions;
		private readonly CollectionsGroupData _collectionGroupData;	
		public ObservableCollection<Brush> RandomBrushes { get; set; }
		public ObservableCollection<CollectionGroup> Items { get; set; }
		public ObservableCollection<Collection> Children { get; set; }
		public CollectionViewSource GroupedItems { get; set; }
		public Visibility ChildrenVisible { get; set; } = Visibility.Collapsed;
		public CollectionsPageViewModel()
        {
			if (AuthService.CurrentUser is null)
				return; 

			_historyActions = new HistoryActions(AuthService.CurrentUser?.Username);
			_collectionGroupData = new CollectionsGroupData(_historyActions); 
			Initialize();
				
		}


		public void GatherCollections(int Id)
		{
			Children = Items.Where(x => x.CollectionName.Id == Id).SelectMany(x => x.Collections).ToObservableCollection();
			OnPropertyChanged(nameof(Children));
		}	

		public async void Initialize() {

			Items = await _collectionGroupData.GetGroupedCollectionsAsync();	
			//GroupedItems = await GetGroupedData(Items);
			OnPropertyChanged(nameof(GroupedItems));

		}
        public async Task<CollectionViewSource> GetGroupedData(ObservableCollection<Collection> items)
        {
            var collection = new CollectionViewSource
            {
                IsSourceGrouped = true,
                Source = items.GroupBy(i => _historyActions.GetAllCollectionNamesItems().Result.Where(x => x.Id == i.Id).Select(g=> g.Name))
                              .Select(group => new { group.Key, Items = group.SelectMany(x=> x.ItemsHistory).ToList() })
            };
			
			OnPropertyChanged(nameof(GroupedItems));
			return await Task.FromResult(collection);
        }

		public void RaisePropertyChanges([CallerMemberName] string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}

	}

    
}
