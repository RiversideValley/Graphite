using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Riverside.Graphite.Controls;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class ResourceString : MarkupExtension
{
	public string Name { get; set; } = string.Empty;
	public string Filename { get; set; } = string.Empty;

	public static string GetString(string name, string filename)
	{
		ResourceLoader resourceLoader = new(filename);
		return resourceLoader.GetString(name);
	}

	protected override object ProvideValue()
	{
		return GetString(Name, Filename);
	}
}