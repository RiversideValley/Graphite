using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.ViewModels
{
    public class CollectionsGroupData
    {
        private readonly HistoryActions _historyActions;

        public CollectionsGroupData(HistoryActions historyActions)
        {
            _historyActions = historyActions;
        }

		
		public async Task<ObservableCollection<CollectionGroup>> GetGroupedCollectionsAsync()
        {
            var collectionNames = await _historyActions.GetAllCollectionNamesItems();
            var groupedCollections = new ObservableCollection<CollectionGroup>();

            foreach (var collectionName in collectionNames)
            {
                var collections = await _historyActions.GetAllCollectionsItems();
                var filteredCollections = collections.Where(c => c.CollectionNameId == collectionName.Id).ToList();

                foreach (var collection in filteredCollections)
                {
                    var historyItem = _historyActions.HistoryContext.Urls.ToList().FirstOrDefault(h => h.id == collection.HistoryItemId);
                    if (historyItem != null)
                    {
                        collection.ItemsHistory = new List<DbHistoryItem> { historyItem };
                    }
                }

                groupedCollections.Add(new CollectionGroup
                {
                    CollectionName = collectionName,
                    Collections = new ObservableCollection<Collection>(filteredCollections)
                });
            }

            return groupedCollections;
        }
    }

   
}
