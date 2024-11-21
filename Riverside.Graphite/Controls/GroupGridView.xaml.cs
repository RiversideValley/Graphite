using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Riverside.Graphite.Core;
using Riverside.Graphite.Data.Core.Actions;
using Riverside.Graphite.Data.Core.Models;
using Riverside.Graphite.Data.Core.Models.Contacts;
using Riverside.Graphite.Runtime.Helpers;
using Riverside.Graphite.Runtime.Helpers.Logging;
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
	public sealed partial class GroupGridView : Page
	{
		private string selectedHistoryItem;
		public GroupedViewModel ViewModelGrouped { get; }
		public SourceViewModel ViewModelSourced { get; }

		public ItemGrouped Selected { get; set; }
		public GroupGridView()
		{

			this.InitializeComponent();
			ViewModelSourced = new SourceViewModel();
			ViewModelGrouped = new GroupedViewModel(ViewModelSourced);
			DataContext = ViewModelGrouped;

		}

		
		private void GroupHeader_Click(object sender, RoutedEventArgs e)
		{
			var toggleButton = sender as ToggleButton;
			var stackPanel = toggleButton.Parent as StackPanel;
			var groupItemsPanel = stackPanel?.FindName("GroupItemsPanel") as ItemsControl;

			if (groupItemsPanel != null)
			{
				if (toggleButton.IsChecked == true)
				{
					// Expand group
					ExpandPanel(groupItemsPanel);
				}
				else
				{
					// Collapse group
					CollapsePanel(groupItemsPanel);
				}
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

		private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MainWindow currentWindow = (Application.Current as App)?.m_window as MainWindow;
			if (currentWindow != null && e.AddedItems.Count > 0)
			{
				if(e.AddedItems.FirstOrDefault() is ItemGrouped history) 
					currentWindow.NavigateToUrl(history.Self.Url);
			}
		}

		private void GroupItemsPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{

		}
	}
	public class ItemGrouped : HistoryItem
	{
		public ItemGrouped(HistoryItem historyItem) : base(historyItem) {
			this.Date = DateTime.Parse(historyItem.LastVisitTime);
			this.Description = $"{historyItem.Title} \n {historyItem.Url}";
			this.UrlIcon = new BitmapImage(new Uri($"https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url={historyItem.Url}&size=32"));
		}	
		public DateTime Date { get; set; } 
		public string Description { get; set; }
		public BitmapImage UrlIcon { get; set; } 
	}
	public class SourceViewModel
	{
		private ObservableCollection<HistoryItem> HistoryItems { get; set; }

		public ObservableCollection<ItemGrouped> Items { get; }

		public SourceViewModel()
		{
			ObservableCollection<HistoryItem>	HistoryItems = FetchBrowserHistoryItems().GetAwaiter().GetResult();
			Items = new ObservableCollection<ItemGrouped>();

			
			DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);
			

			foreach (var item in HistoryItems.GroupBy(i => DateTime.Parse(i.LastVisitTime) >= sevenDaysAgo 
			? DateTime.Parse(i.LastVisitTime).ToString("MMMM dd, yyyy") 
			: DateTime.Parse(i.LastVisitTime).ToString("MMMM yyyy")))
			{
				foreach(var data in item)
				{
					Items.Add(new ItemGrouped(data));

				}
			}

		}




		private async Task<ObservableCollection<HistoryItem>> FetchBrowserHistoryItems()
		{
			//Riverside.Graphite.Core.User user = AuthService.CurrentUser;
			try
			{
				HistoryActions historyActions = new(AuthService.CurrentUser.Username);
				var browserHistory = await historyActions.GetAllHistoryItems();
				return browserHistory;
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
			}
			return null;
		}
	}

	public class GroupedViewModel
	{
		public CollectionViewSource GroupedItems { get; }

		public GroupedViewModel(SourceViewModel viewModel)
		{
			DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);

			GroupedItems = new CollectionViewSource
			{
				IsSourceGrouped = true,
				Source = viewModel.Items.GroupBy(i => i.Date >= sevenDaysAgo
						? i.Date.ToString("MMMM dd, yyyy")
						: i.Date.ToString("MMMM yyyy"))
				.Select(group => new { Key = group.Key, Items = group.ToList() })
			};
		}
	}

}
