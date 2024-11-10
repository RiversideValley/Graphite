using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FireBrowserWinUi3.Controls;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class ResourceString : MarkupExtension
{
    public string Name { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;

    public static string GetString(string name, string filename)
    {
        var resourceLoader = new ResourceLoader(filename);
        return resourceLoader.GetString(name);
    }

    protected override object ProvideValue() => GetString(Name, Filename);
}