using System.Collections.ObjectModel;
using static Riverside.Graphite.Extensions.IPluginCore;

namespace Riverside.Graphite.Extensions;
public interface IPluginBase
{
	public string Name { get; set; }
	public string Description { get; set; }

	public ObservableCollection<object> DynamicValues { get; set; }

	public PluginResponse Initialize(PluginParameters args);

	public PluginResponse Execute(PluginParameters args);
}
