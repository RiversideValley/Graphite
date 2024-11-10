using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Data.Common;

namespace FireBrowserWinUi3Services.PluginCore;
public class IPluginCore
{
    public class PluginParameters
    {
        public DbConnection con { get; set; } = null;
        public HashSet<object> Context { get; set; } = new HashSet<object>();
    }

    public class PluginResponse
    {
        public string Message { get; set; } = "";
        public bool HasError { get; set; } = false;
        public string MessageID { get; set; } = "";
    }

    public class RpResponse : PluginResponse
    {
        public UserControl Form { get; set; }

        public RpResponse(PluginEntry entry)
        {
            // Check if the entry is a XamlPluginEntry
            if (entry is XamlPluginEntry xamlEntry)
            {
                // If it's a XamlPluginEntry, extract the UserControl
                Form = xamlEntry.form;
            }
            else
            {
                // If it's a regular PluginEntry, extract the form directly
                Form = entry.form;
            }
        }
    }
}
