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

namespace Riverside.Graphite.ViewModels
{
    public class CollectionsPageViewModel : ObservableObject
    {
        public ObservableCollection<Brush> RandomBrushes { get; set; }
		public ObservableCollection<CollectionName> Collections { get; set; }

		public CollectionsPageViewModel()
        {
			Initialize(); 
        }


		public void Initialize() {

			if (AuthService.CurrentUser is not null)
				Collections = new HistoryActions(AuthService.CurrentUser.Username).GetAllCollectionNamesItems().Result;
			else
				Collections = new ObservableCollection<CollectionName>();

		
		}

        
    }
}
