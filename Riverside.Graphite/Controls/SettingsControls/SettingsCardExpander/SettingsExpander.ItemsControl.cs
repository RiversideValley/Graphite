using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Graphite.Controls.SettingsControls
{
    public partial class SettingsExpander
    {
        private static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.Register(nameof(ItemsControl), typeof(ItemsControl), typeof(SettingsExpander), new PropertyMetadata(null));

        private ItemsControl ItemsControl
        {
            get => (ItemsControl)GetValue(ItemsControlProperty);
            set => SetValue(ItemsControlProperty, value);
        }
    }
}

