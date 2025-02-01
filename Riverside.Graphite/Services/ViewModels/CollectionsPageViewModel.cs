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
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graph.Models;
using Riverside.Graphite.Services.Messages;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Windowing;
using Riverside.Graphite.Pages;
using WinRT.Interop;
using static Riverside.Graphite.MainWindow;
using Riverside.Graphite.Services;
using System.Threading;
using Riverside.Graphite.Runtime.Helpers.Logging;

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
                    ? [.. Children.SelectMany(t => t.ItemsHistory ?? Enumerable.Empty<DbHistoryItem>())]
					: [];
            }
			
        }

		[ObservableProperty]
		
		private bool isWebViewLoaded ;

		[ObservableProperty]
		private string _selectedCollectionName; 

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(OpenViewNewWindowCommand))]
		private bool _IsHistoryViewing;
		
		[ObservableProperty]
		private Uri _selectedUrl; 

		[ObservableProperty]
		private int _selectedCollection;
		
		[ObservableProperty]
        private Visibility _ChildrenVisible;

		[ObservableProperty]
		
		private Visibility _WebViewVisible;

		partial void OnSelectedCollectionChanged(int value)
		{
			SelectedCollectionName = Items?.Where(t=> t.CollectionName.Id == value).Select(t => t.CollectionName.Name).FirstOrDefault();
		}
		partial void OnWebViewVisibleChanged(Visibility value)
		{
			if (value == Visibility.Visible) {
				IsHistoryViewing = true;
			}
			else {
				IsHistoryViewing = false; 
			}

			RaisePropertyChanges(nameof(IsHistoryViewing));

		}

		[RelayCommand(CanExecute = nameof(IsHistoryViewing))]
		private async Task OpenViewNewWindow() {

			if (SelectedUrl is null) return;

			SemaphoreSlim semaphoreSlim = new(1);

			try
			{
				await semaphoreSlim.WaitAsync();

				var win = new Window();
				var frame = new Frame();
				frame.Margin = new Thickness(0, 32, 0, 0);
				var web = new WebView2() { Source = SelectedUrl };
				frame.Content = web;
				win.Content = frame;
				await web.EnsureCoreWebView2Async();
				web.CoreWebView2.NewWindowRequested += (sender, args) =>
				{
					sender.Navigate(args.Uri);
				};

				await AppService.ConfigureSettingsWindow(win);

			}
			catch (Exception e)
			{
				ExceptionLogger.LogException(e);	
				
			}
			finally { 

				semaphoreSlim?.Release();	
			}


		}

		#endregion
		public CollectionsPageViewModel(IMessenger messenger):base(messenger)
		{
			if (AuthService.CurrentUser is null)
				return;
			Messenger.Register<Message_Settings_Actions>(this, (r, m) => ReceivedStatus(m));
			_historyActions = new HistoryActions(AuthService.CurrentUser?.Username);
			_collectionGroupData = new CollectionsGroupData(_historyActions); 
			Initialize();
				
		}

		private async void ReceivedStatus(Message_Settings_Actions m)
		{
			switch(m.Status)
			{
				case EnumMessageStatus.Collections:
					if (SelectedCollection is not 0) {
						Items = await _collectionGroupData.GetGroupedCollectionsAsync();
						RaisePropertyChanges(nameof(Items));	
						GatherCollections(SelectedCollection);

						if (SelectedUrl is not null && m.DataItemPassed is not null && SelectedUrl?.AbsoluteUri == (m.DataItemPassed! as HistoryItem).Url)
						{
							SelectedUrl = null;
							RaisePropertyChanges(nameof(SelectedUrl));

							await Task.Delay(200);

							WebViewVisible = Visibility.Collapsed;
							RaisePropertyChanges(nameof(WebViewVisible));
						}
					}
					break;
				case EnumMessageStatus.Updated:
					break;
				case EnumMessageStatus.Added:
					break;
				case EnumMessageStatus.Removed:
					break;
				case EnumMessageStatus.Informational:
					break;
				case EnumMessageStatus.Login:
					break;
				case EnumMessageStatus.Logout:
					break;
				case EnumMessageStatus.Settings:
					break;
				case EnumMessageStatus.XorError:
					break;
			}
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
			RaisePropertyChanges(nameof(Items));
			Children = new(); 
			ChildrenVisible = Visibility.Collapsed;
			WebViewVisible = Visibility.Collapsed;
		}
  
		public void RaisePropertyChanges([CallerMemberName] string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}

	}

    
}
