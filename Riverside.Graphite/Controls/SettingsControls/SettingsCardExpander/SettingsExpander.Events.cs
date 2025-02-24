using Microsoft.UI.Xaml;
using System;
using Windows.Foundation;

namespace Graphite.Controls.SettingsControls
{
    public partial class SettingsExpander
    {
        public event TypedEventHandler<SettingsExpander, RoutedEventArgs> Expanding;
        public event TypedEventHandler<SettingsExpander, RoutedEventArgs> Expanded;
        public event TypedEventHandler<SettingsExpander, RoutedEventArgs> Collapsing;
        public event TypedEventHandler<SettingsExpander, RoutedEventArgs> Collapsed;

        protected virtual void OnExpanding(RoutedEventArgs e)
        {
            Expanding?.Invoke(this, e);
        }

        protected virtual void OnExpanded(RoutedEventArgs e)
        {
            Expanded?.Invoke(this, e);
        }

        protected virtual void OnCollapsing(RoutedEventArgs e)
        {
            Collapsing?.Invoke(this, e);
        }

        protected virtual void OnCollapsed(RoutedEventArgs e)
        {
            Collapsed?.Invoke(this, e);
        }
    }
}

