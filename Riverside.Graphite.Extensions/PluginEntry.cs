using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Extensions;
public class PluginEntry
{
	public string Name { get; set; }
	public string Description { get; set; }

	private readonly IPluginBase Plugin = null;

	public UserControl form { get; set; } = null;

	public PluginEntry(IPluginBase p)
	{
		Plugin = p;

		Name = p.Name;
		Description = p.Description;

		IPluginCore.PluginResponse response = p.Initialize(null);

		// Check if the response contains a form
		if (response is IPluginCore.RpResponse formResponse)
		{
			form = formResponse.Form;
		}
	}
}
