using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FireBrowserWinUi3.Pages.TimeLinePages;
public sealed partial class MainTimeLine : Page
{
    private readonly Dictionary<string, Type> _pages = new()
    {
        { "history", typeof(TimeLinePages.HistoryTimeLine) },
        { "apps", typeof(TimeLinePages.AppsTimeLine) },
        { "favorites", typeof(TimeLinePages.FavoritesTimeLine) },
        { "downloads", typeof(TimeLinePages.DownloadsTimeLine) }
    };

    public MainTimeLine() => InitializeComponent();

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigated += On_Navigated;

        var window = (Application.Current as App)?.m_window as MainWindow;
        var urlBoxText = window.UrlBox.Text;

        string navigateTo = urlBoxText switch
        {
            var s when s.Contains("firebrowser://apps") => "apps",
            var s when s.Contains("firebrowser://history") => "history",
            var s when s.Contains("firebrowser://favorites") => "favorites",
            var s when s.Contains("firebrowser://downloads") => "downloads",
            _ => "apps"
        };

        NavigationView.SelectedItem = NavigationView.MenuItems[_pages.Keys.ToList().IndexOf(navigateTo)];
        NavigationView_Navigate(navigateTo, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var navItemTag = args.IsSettingsInvoked ? "history" : args.InvokedItemContainer?.Tag.ToString();
        if (navItemTag != null)
            NavigationView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var navItemTag = args.IsSettingsSelected ? "history" : args.SelectedItemContainer?.Tag.ToString();
        if (navItemTag != null)
        {
            NavigationView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            var window = (Application.Current as App)?.m_window as MainWindow;
            window.UrlBox.Text = $"firebrowser://{navItemTag}";
        }
    }

    private void NavigationView_Navigate(string navItemTag, Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
    {
        if (_pages.TryGetValue(navItemTag, out var page) && page != ContentFrame.CurrentSourcePageType)
        {
            ContentFrame.Navigate(page, transitionInfo);
        }
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception($"Failed to load Page {e.SourcePageType.FullName}");
    }

    private bool TryGoBack()
    {
        if (!ContentFrame.CanGoBack || (NavigationView.IsPaneOpen && (NavigationView.DisplayMode is NavigationViewDisplayMode.Compact or NavigationViewDisplayMode.Minimal)))
            return false;

        ContentFrame.GoBack();
        return true;
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        NavigationView.IsBackEnabled = ContentFrame.CanGoBack;
        _ = _pages.ContainsValue(e.SourcePageType); // Optional: store if needed
    }
}
