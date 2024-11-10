using Microsoft.UI.Xaml.Controls;
using System;

namespace FireBrowserWinUi3Services.PluginCore;
public class PluginEntry
{
    public String Name { get; set; }
    public String Description { get; set; }

    IPluginBase Plugin = null;

    public UserControl form { get; set; } = null;

    public PluginEntry(IPluginBase p)
    {
        Plugin = p;

        this.Name = p.Name;
        this.Description = p.Description;

        var response = p.Initialize(null);

        // Check if the response contains a form
        if (response is IPluginCore.RpResponse formResponse)
        {
            form = formResponse.Form;
        }
    }
}
