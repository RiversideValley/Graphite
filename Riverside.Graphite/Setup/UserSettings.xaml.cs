using Riverside.Graphite.Core;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;

namespace FireBrowserWinUi3.Setup
{
	public sealed partial class UserSettings : Window
	{
		public string UserName { get; } = AuthService.NewCreatedUser?.Username
									   ?? AuthService.CurrentUser?.Username
									   ?? "User Settings";

		private int previousSelectedIndex;

		public UserSettings()
		{
			InitializeComponent();
			AppService.AppSettings = new Riverside.Graphite.Core.Settings(true).Self;
			NavView.SelectedItem = NavView.MenuItems[0]; // Select the first item by default
		}

		private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.SelectedItem is NavigationViewItem selectedItem)
			{
				int currentSelectedIndex = NavView.MenuItems.IndexOf(selectedItem);
				Type pageType = GetPageType(selectedItem.Tag as string);

				SlideNavigationTransitionEffect slideEffect = currentSelectedIndex > previousSelectedIndex
					? SlideNavigationTransitionEffect.FromRight
					: SlideNavigationTransitionEffect.FromLeft;

				_ = ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo { Effect = slideEffect });

				previousSelectedIndex = currentSelectedIndex;
			}
		}

		private static Type GetPageType(string tag)
		{
			return tag switch
			{
				"SetupUi" => typeof(SetupUi),
				"SetupAlgemeen" => typeof(SetupAlgemeen),
				"SetupPrivacy" => typeof(SetupPrivacy),
				"SetupAccess" => typeof(SetupAccess),
				"SetupWebView" => typeof(SetupWebView),
				"SetupFinish" => typeof(SetupFinish),
				_ => throw new ArgumentException($"Unknown tag: {tag}", nameof(tag))
			};
		}

		public void NavigateToPage(string pageName)
		{
			NavigationViewItem item = NavView.MenuItems.OfType<NavigationViewItem>()
							 .FirstOrDefault(item => item.Tag.ToString() == pageName);
			if (item != null)
			{
				NavView.SelectedItem = item;
			}
		}
	}
}