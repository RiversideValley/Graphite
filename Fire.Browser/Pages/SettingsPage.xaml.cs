using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using static FireBrowserWinUi3.MainWindow;

namespace FireBrowserWinUi3.Pages;
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }

    private readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
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

        var window = (Application.Current as App)?.m_window as MainWindow;
        var urlBoxText = window?.UrlBox.Text ?? string.Empty;

        var navigateTo = urlBoxText switch
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

        var (selectedTag, selectedItem) = navigateTo;

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
            var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
            _page = item.Page;
        }
        // Get the page type before navigation so you can prevent duplicate
        // entries in the backstack.
        var preNavPageType = ContentFrame.CurrentSourcePageType;

        // Only navigate if the selected page isn't currently loaded.
        if (!(_page is null) && !Type.Equals(preNavPageType, _page))
        {
            ContentFrame.Navigate(_page, passer, transitionInfo);
        }
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        NavView.IsBackEnabled = ContentFrame.CanGoBack;

        if (ContentFrame.SourcePageType != null)
        {
            var item = _pages.FirstOrDefault(p => p.Page == e.SourcePageType);


            NavView.Header =
                ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
            passer.Tab.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked == true)
        {
            NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
        }
        else if (args.InvokedItemContainer != null)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString();
            NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected == true)
        {
            NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
        }
        else if (args.SelectedItemContainer != null)
        {
            var navItemTag = args.SelectedItemContainer.Tag.ToString();
            NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            var window = (Application.Current as App)?.m_window as MainWindow;
            window.UrlBox.Text = "firebrowser://" + navItemTag.ToString();
        }
    }
}