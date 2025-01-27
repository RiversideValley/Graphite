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
			if (ViewModel.Children.Count > 0) {
				ViewModel.ChildrenVisible = Visibility.Visible;
				ViewModel.RaisePropertyChanges(nameof(ViewModel.ChildrenVisible));
			}
		}

		private void ExpandPanel(ItemsControl panel)
		{
			// Ensure the panel is visible
			panel.Visibility = Visibility.Visible;

			// Animate the expansion
			var storyboard = new Storyboard();
			var animation = new DoubleAnimation
			{
				From = 0,
				To = panel.ActualHeight,
				Duration = new Duration(TimeSpan.FromMilliseconds(500))
			};
			Storyboard.SetTarget(animation, panel);
			Storyboard.SetTargetProperty(animation, "Height");
			storyboard.Children.Add(animation);
			storyboard.Begin();
		}

		private void CollapsePanel(ItemsControl panel)
		{
			// Animate the collapse
			var storyboard = new Storyboard();
			var animation = new DoubleAnimation
			{
				From = panel.ActualHeight,
				To = 0,
				Duration = new Duration(TimeSpan.FromMilliseconds(500))
			};
			Storyboard.SetTarget(animation, panel);
			Storyboard.SetTargetProperty(animation, "Height");
			storyboard.Children.Add(animation);
			storyboard.Begin();

			// Hide the panel after animation completes
			storyboard.Completed += (s, e) =>
			{
				panel.Visibility = Visibility.Collapsed;
			};
		}

		private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (((FrameworkElement)sender).DataContext is HistoryItem historyItem)
			{
				ShowContextMenu(historyItem.Url, (FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
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

		private void RemoveHistoryItem(string selectedHistoryItem)
		{
			ViewModel.Initialize();	
		}

		
		private void ZoomControl_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
		{
			if (e.IsSourceZoomedInView == false)
			{
				e.DestinationItem.Item = e.SourceItem.Item;
			}
		}
	}
}
