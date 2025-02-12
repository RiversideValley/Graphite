using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Runtime.Helpers.Logging;
using Riverside.Graphite.Services.ViewModels;
using Riverside.Graphite.ViewModels.DataGetters;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Riverside.Graphite.MainWindow;


namespace Riverside.Graphite.Pages.TimeLinePages;
public sealed partial class HistoryTimeLine : Page
{
	private readonly User _user = AuthService.CurrentUser;
	
	public IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem> _browserHistory = new IncrementalLoadingCollection<BrowserHistoryCollection, HistoryItem>(new BrowserHistoryCollection());

	public HistoryViewModel ViewModel { get; set; }
	public HistoryTimeLine()
	{
		ViewModel = App.GetService<HistoryViewModel>();	
		InitializeComponent();
		ViewModel.ParentHistoryTimeLine = this;	
	}
    
}
