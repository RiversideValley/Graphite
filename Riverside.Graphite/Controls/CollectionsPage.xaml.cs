using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
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
		public CollectionsPage()
		{
			ViewModel = App.GetService<CollectionsPageViewModel>();
			DataContext = ViewModel;
 
			this.InitializeComponent();
		}
        private void GroupHeader_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var id = (int?)btn.Tag;

			
			ViewModel.GatherCollections(id.Value);
			ViewModel.SelectedCollection = id.Value;	

			if (ViewModel.Children.Count > 0) {
				ViewModel.ChildrenVisible = Visibility.Visible;
				ViewModel.RaisePropertyChanges(nameof(ViewModel.ChildrenVisible));
			}
		}
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
				Text = "Delete This Record",
				Icon = new FontIcon { Glyph = "\uE74D" }
			};

			deleteMenuItem.Click += async (s, args) =>
			{
				HistoryActions historyActions = new(AuthService.CurrentUser?.Username);
				await historyActions.DeleteHistoryItem(selectedHistoryItem);
				RemoveHistoryItem(selectedHistoryItem);
			};

			flyout.Items.Add(deleteMenuItem);
			flyout.ShowAt(sender, position);
		}

		private async void RemoveHistoryItem(string selectedHistoryItem)
		{
			ViewModel.Initialize();
			await Task.Delay(100);	
			ViewModel.GatherCollections(ViewModel.SelectedCollection);
		}

		
	}
}
