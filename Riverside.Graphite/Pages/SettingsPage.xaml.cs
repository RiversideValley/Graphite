using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Riverside.Graphite.Controls;
using Riverside.Graphite.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Riverside.Graphite.Pages;
public sealed partial class SettingsPage : Page
{
	public string UserName => AuthService.CurrentUser?.Username ?? "Graphite";
	public SettingsPage()
	{
		InitializeComponent();
	}

	private readonly List<(string Tag, Type Page)> _pages = new()
	{
		("SettingsHome", typeof(Pages.SettingsPages.SettingsHome)),
		("Privacy",  typeof(Pages.SettingsPages.SettingsPrivacy)),
		("WebView", typeof(Pages.SettingsPages.SettingsWebView)),
		("NewTab",  typeof(Pages.SettingsPages.SettingsNewTab)),
		("Design",  typeof(Pages.SettingsPages.SettingsDesign)),
		("Enqryption",  typeof(Pages.SettingsPages.SettingsEnqryption)),
		("Accessibility",  typeof(Pages.SettingsPages.SettingsAccess)),
		("About",  typeof(Pages.SettingsPages.SettingsAbout))
	};

	private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
	{
		throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
	}

	private void NavView_Loaded(object sender, RoutedEventArgs e)
	{
		ContentFrame.Navigated += On_Navigated;

		MainWindow window = (Application.Current as App)?.m_window as MainWindow;
		string urlBoxText = window?.UrlBox.Text ?? string.Empty;

		(string, object) navigateTo = urlBoxText switch
		{
			string s when s.Contains("firebrowser://SettingsHome") => ("SettingsHome", NavView.MenuItems[0]),
			string s when s.Contains("firebrowser://Privacy") => ("Privacy", NavView.MenuItems[2]),
			string s when s.Contains("firebrowser://WebView") => ("WebView", NavView.MenuItems[3]),
			string s when s.Contains("firebrowser://NewTab") => ("NewTab", NavView.MenuItems[4]),
			string s when s.Contains("firebrowser://Design") => ("Design", NavView.MenuItems[1]),
			string s when s.Contains("firebrowser://Enqryption") => ("Enqryption", NavView.MenuItems[5]),
			string s when s.Contains("firebrowser://Accessibility") => ("Accessibility", NavView.MenuItems[6]),
			string s when s.Contains("firebrowser://About") => ("About", NavView.MenuItems[7]),
			_ => (null, null), // default case
		};

		(string selectedTag, object selectedItem) = navigateTo;

		if (selectedTag != null && selectedItem != null)
		{
			NavView.SelectedItem = selectedItem;
			NavView_Navigate(selectedTag, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
		}
		else
		{
			NavView.SelectedItem = NavView.MenuItems[0];
			NavView_Navigate("SettingsHome", new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
		} // Default behavior
	}

	private Passer passer;
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		passer = e.Parameter as Passer;

		passer.Tab.IconSource = new FontIconSource
		{
			Glyph = "\uE713"
		};
	}

	private void NavView_Navigate(
	   string navItemTag,
	   Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
	{
		Type _page = null;
		if (navItemTag == "settings")
		{
			_page = typeof(SettingsPage);
		}
		else
		{
			(string Tag, Type Page) item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
			_page = item.Page;
		}
		// Get the page type before navigation so you can prevent duplicate
		// entries in the backstack.
		Type preNavPageType = ContentFrame.CurrentSourcePageType;

		// Only navigate if the selected page isn't currently loaded.
		if (_page is not null && !Type.Equals(preNavPageType, _page))
		{
			_ = ContentFrame.Navigate(_page, passer, transitionInfo);
		}
	}

	private void On_Navigated(object sender, NavigationEventArgs e)
	{
		NavView.IsBackEnabled = ContentFrame.CanGoBack;

		if (ContentFrame.SourcePageType != null)
		{
			(string Tag, Type Page) item = _pages.FirstOrDefault(p => p.Page == e.SourcePageType);


			NavView.Header =
				((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();

			if (passer is not null)
			{
				passer.Tab.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
			}

		}
	}

	private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		// Check if the clicked item is in the FooterMenuItems
		if (sender.FooterMenuItems.Contains(args.InvokedItemContainer))
		{
			// Do nothing if it's a footer item
			return;
		}
		if (args.IsSettingsInvoked == true)
		{
			NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
		}
		else if (args.InvokedItemContainer != null)
		{
			string navItemTag = args.InvokedItemContainer.Tag.ToString();
			NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
		}
	}

	private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		if (args.SelectedItemContainer is NavigationViewItemHeader)
		{
			// Do nothing if it's a header item
			return;
		}

		if (args.IsSettingsSelected == true)
		{
			NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
		}
		else if (args.SelectedItemContainer != null)
		{
			// Check if Tag is null before trying to use it
			if (args.SelectedItemContainer.Tag != null)
			{
				string navItemTag = args.SelectedItemContainer.Tag.ToString();
				NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
				MainWindow window = (Application.Current as App)?.m_window as MainWindow;
				window.UrlBox.Text = "firebrowser://" + navItemTag;
			}
			// If Tag is null, do nothing (this handles your footer item)
		}
	}
}