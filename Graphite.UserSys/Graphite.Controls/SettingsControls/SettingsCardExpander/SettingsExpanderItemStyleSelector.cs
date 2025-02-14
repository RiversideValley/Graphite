using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Graphite.Controls.SettingsControls
{
    public class SettingsExpanderItemStyleSelector : StyleSelector
    {
        public Style DefaultStyle { get; set; }
        public Style ClickableStyle { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            if (item is FrameworkElement element && element.IsTabStop)
            {
                return ClickableStyle ?? DefaultStyle;
            }

            return DefaultStyle;
        }
    }
}

