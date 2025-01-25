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

namespace Riverside.Graphite.ViewModels
{
    public class CollectionsPageViewModel : ObservableObject
    {
		public  GroupedViewModel GrpViewModel {get;set; }
		private readonly HistoryActions _historyActions;
		private readonly CollectionsGroupData _collectionsGroupData;	
		public ObservableCollection<Brush> RandomBrushes { get; set; }
		public ObservableCollection<CollectionGroup> Collections { get; set; }

		public CollectionsPageViewModel()
        {
			if (AuthService.CurrentUser is null)
				return; 

			_historyActions = new HistoryActions(AuthService.CurrentUser?.Username);
			_collectionsGroupData = new CollectionsGroupData(_historyActions);	
			Initialize();
				
		}


		public async void Initialize() {

			//if (AuthService.CurrentUser is not null)
			//	Collections = new HistoryActions(AuthService.CurrentUser.Username).GetAllCollectionNamesItems().Result;
			//else
			//	Collections = new ObservableCollection<CollectionName>();
			Collections = await _collectionsGroupData.GetGroupedCollectionsAsync(); 	
			GrpViewModel = new GroupedViewModel(this);

		}

        
    }

    public class GroupedViewModel : ObservableObject
    {
        public CollectionViewSource GroupedItems { get; set; }

        public GroupedViewModel(CollectionsPageViewModel viewModel)
        {
            Initialize(viewModel);
        }

        private async void Initialize(CollectionsPageViewModel viewModel)
        {
            GroupedItems = await GetGroupedData(viewModel);
        }

        public async Task<CollectionViewSource> GetGroupedData(CollectionsPageViewModel viewModel)
        {
            DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);

            var collection = new CollectionViewSource
            {
                IsSourceGrouped = true,
                Source = viewModel.Collections.GroupBy(i => i.CollectionName.Name)
                .Select(group => new { Key = group.Key, Items = group.ToList() })
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
