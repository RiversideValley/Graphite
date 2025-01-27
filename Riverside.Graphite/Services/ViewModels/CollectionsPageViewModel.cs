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
    public partial class CollectionsPageViewModel : ObservableRecipient
    {
		#region dp_injection_services
			private readonly HistoryActions _historyActions;
			private readonly CollectionsGroupData _collectionGroupData;
		#endregion

		#region ui_props

		public ObservableCollection<Brush> RandomBrushes { get; set; }
		public ObservableCollection<CollectionGroup> Items { get; set; }
		public ObservableCollection<Collection> Children { get; set; }
        public ObservableCollection<DbHistoryItem> SubHistoryItems
        {
            get
            {
                return Children != null
                    ? new ObservableCollection<DbHistoryItem>(Children.SelectMany(t => t.ItemsHistory ?? Enumerable.Empty<DbHistoryItem>()))
                    : new ObservableCollection<DbHistoryItem>();
            }
			
        }

		[ObservableProperty]
		private int _selectedCollection;
		
		[ObservableProperty]
        private Visibility _ChildrenVisible;

		#endregion
		public CollectionsPageViewModel()
        {
			if (AuthService.CurrentUser is null)
				return; 

			_historyActions = new HistoryActions(AuthService.CurrentUser?.Username);
			_collectionGroupData = new CollectionsGroupData(_historyActions); 
			Initialize();
				
		}


		public async void GatherCollections(int Id)
		{
			Children = Items.Where(x => x.CollectionName.Id == Id).SelectMany(x => x.Collections).ToObservableCollection();
			
			await Task.Delay(100);
			OnPropertyChanged(nameof(Children));
			await Task.Delay(100);
			OnPropertyChanged(nameof(SubHistoryItems));
		}	

		public async void Initialize() {

			Items = await _collectionGroupData.GetGroupedCollectionsAsync();	
		}
  
		public void RaisePropertyChanges([CallerMemberName] string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}

	}

    
}
