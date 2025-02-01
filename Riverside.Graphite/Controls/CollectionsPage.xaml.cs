using Microsoft.Bing.WebSearch;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Pages;
using Riverside.Graphite.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WebUI;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Riverside.Graphite.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class CollectionsPage : Page
	{
		public CollectionsPageViewModel ViewModel { get; set; }
		public WebView2 WebView { get; private set; }

		public CollectionsPage()
		{
			ViewModel = App.GetService<CollectionsPageViewModel>();
			DataContext = ViewModel;
			this.InitializeComponent();
			WebView = this.WebViewHistoryItem;

			string browserFolderPath = Path.Combine(UserDataManager.CoreFolderPath, "Users", AuthService.CurrentUser?.Username, "Browser");
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", browserFolderPath);
			Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msSingleSignOnOSForPrimaryAccountIsShared");
		}

		#region CollectionName_RightTap



		private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (((FrameworkElement)sender).DataContext is DbHistoryItem historyItem)
			{
				ShowContextMenu(historyItem.url, (FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
			}
		}
		private void ShowContextMenu(string selectedHistoryItem, FrameworkElement sender, Windows.Foundation.Point position)
		{
			MenuFlyout flyout = new();
			MenuFlyoutItem deleteMenuItem = new()
			{
				Text = "Remove From Collection",
				Icon = new FontIcon { Glyph = "\uE74D" }
			};
			MenuFlyoutItem newTabMenuItem = new()
			{
				Text = "Open in New Tab",
				Icon = new FontIcon { Glyph = "\uE8AD" }
			};

			newTabMenuItem.Click += (s, args) =>
			{
				if (App.Current.m_window is MainWindow window)
				{
					window.TabViewContainer.TabItems.Add(window.CreateNewTab(typeof(WebContent), selectedHistoryItem));
				}
			};

			deleteMenuItem.Click += async (s, args) =>
			{
				var item = ViewModel.SubHistoryItems.Where(t => t.url == selectedHistoryItem).FirstOrDefault();
				var collectItem = ViewModel.Children.Where(t => t.HistoryItemId == item.id).Where(s => s.CollectionNameId == ViewModel.SelectedCollection).FirstOrDefault();

				if (collectItem is Collection itemToDelete)
				{
					HistoryActions historyActions = new(AuthService.CurrentUser?.Username);
					await historyActions.DeleteCollectionsItem(itemToDelete.Id);
					RemoveHistoryItem(selectedHistoryItem);
				}

				await WebViewHistoryItem.EnsureCoreWebView2Async(); ;

				_ = await WebViewHistoryItem.CoreWebView2?.ExecuteScriptAsync(
							@"(function() { 
							try
							{
								const videos = document.querySelectorAll('video');
								videos.forEach((video) => { video.pause();});
								console.log('WINUI3_CoreWebView2: YES_VIDEOS_CLOSED');
								return true; 
							}
							catch(error) {
								console.log('WINUI3_CoreWebView2: NO_VIDEOS_CLOSED');
								return error.message; 
							}
						})();");


			};
			flyout.Items.Add(newTabMenuItem);
			flyout.Items.Add(deleteMenuItem);
			flyout.ShowAt(sender, position);
		}

		private async void RemoveHistoryItem(string selectedHistoryItem)
		{
			ViewModel.Initialize();
			await Task.Delay(200);
			ViewModel.GatherCollections(ViewModel.SelectedCollection);
		}
		#endregion
		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				ViewModel.SelectedUrl = new((e.AddedItems[0]! as DbHistoryItem).url);
				ViewModel.RaisePropertyChanges(nameof(ViewModel.SelectedUrl));
			}
		}

		private async void WebViewHistoryItem_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{

			if (sender is WebView2 view)
			{

				await view.EnsureCoreWebView2Async();
				await view.CoreWebView2.ExecuteScriptWithResultAsync(@"
					document.body.style.zoom='.75';
				");

				ViewModel.IsWebViewLoaded = false;
				ViewModel.RaisePropertyChanges(nameof(ViewModel.IsWebViewLoaded));

				ViewModel.WebViewVisible = Visibility.Visible;

			}


		}

		private async void WebViewHistoryItem_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			if (sender is WebView2 view)
			{
				await view.EnsureCoreWebView2Async();
				view.CoreWebView2.NewWindowRequested += (s, e) =>
				{
					e.Handled = true;
					if (App.Current.m_window is MainWindow window)
					{
						window.TabViewContainer.TabItems.Add(window.CreateNewTab(typeof(WebContent), e.Uri));
					}
				};
			}
		}

		private void WebViewHistoryItem_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{

			ViewModel.IsWebViewLoaded = true;
			ViewModel.RaisePropertyChanges(nameof(ViewModel.IsWebViewLoaded));

		}

		private void GridCollections_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				if (e.AddedItems[0] is CollectionGroup collectionName)
				{
					ViewModel.SelectedCollection = collectionName.CollectionName.Id;
					ViewModel.RaisePropertyChanges(nameof(ViewModel.SelectedCollection));
					ViewModel.GatherCollections(collectionName.CollectionName.Id);
					if (ViewModel.Children.Count > 0)
					{
						ViewModel.ChildrenVisible = Visibility.Visible;
					}
					else
					{
						ViewModel.ChildrenVisible = Visibility.Collapsed;
						if (App.Current.m_window is MainWindow window)
						{
							window.DispatcherQueue.TryEnqueue(() => window.NotificationQueue.Show("No items in this collection", 2000, "Collections"));
						}
					}

					ViewModel.WebViewVisible = Visibility.Collapsed;
				}
			}
			else { return; }



		}
		private void AddCollectionButton_Click(object sender, RoutedEventArgs e)
		{
			AddCollectionTeachingTip.IsOpen = true;
		}

		private async void AddCollection_Click(object sender, RoutedEventArgs e)
		{
			string collectionName = CollectionNameTextBox.Text;
			if (!string.IsNullOrEmpty(collectionName))
			{
				// Add your logic to handle the new collection name
				try
				{
					if (ViewModel.Items.Any(t => t.CollectionName.Name.ToLower() == collectionName.ToLower()))
					{
						(App.Current.m_window as MainWindow).NotificationQueue.Show("Collection already exists", 2000, "Collections");
						CollectionNameTextBox.Text = string.Empty;
						return;
					}

					HistoryActions historyActions = new(AuthService.CurrentUser?.Username);
					historyActions.HistoryContext.CollectionNames.Add(new CollectionName { Name = collectionName });
					await historyActions.HistoryContext.SaveChangesAsync();
					ViewModel.Initialize();	
					(App.Current.m_window as MainWindow).NotificationQueue.Show("Your collection has been added successfully", 2000, "Collections");
				}
				catch (Exception)
				{
					(App.Current.m_window as MainWindow).NotificationQueue.Show("Error adding collection", 2000, "Collections");
					throw;
				}

				AddCollectionTeachingTip.IsOpen = false;
			}
			else {
				(App.Current.m_window as MainWindow).NotificationQueue.Show("Please provide a valid name for a new collection", 2000, "Collections");
			}
		}

		private void HelpCollection_Click(object sender, RoutedEventArgs e)
		{
			HelpTeachingTip.IsOpen	= true;
		}
	}
}
