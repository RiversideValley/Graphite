using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Riverside.Graphite.ViewModels.DataGetters ;
public partial class BrowserHistoryCollection : ObservableObject, IIncrementalSource<HistoryItem>
{
    private ObservableCollection<HistoryItem> _historyItems = new();

    public ObservableCollection<HistoryItem> HistoryItems
    {
        get => _historyItems;
        set => SetProperty(ref _historyItems, value);
    }

    public async Task<IEnumerable<HistoryItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        HistoryActions historyActions = new(AuthService.CurrentUser.Username);
        ObservableCollection<HistoryItem> allItems = await historyActions.GetAllHistoryItems();
        _historyItems = new ObservableCollection<HistoryItem>(allItems.OrderByDescending(t=> t.Id).Skip(pageIndex * pageSize).Take(pageSize));
		//ExceptionLogger.LogInformation($"{DateTime.Now.ToLocalTime()} getItems: {pageSize} indexPage: {pageIndex} totalItems: {allItems.Count}"); 
		return HistoryItems;
    }
}
