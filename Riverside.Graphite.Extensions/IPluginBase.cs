using System;
using System.Collections.ObjectModel;
using static Riverside.Graphite.Extensions.IPluginCore;

namespace Riverside.Graphite.Extensions;
public interface IPluginBase
{
	public String Name { get; set; }
	public String Description { get; set; }

	public ObservableCollection<object> DynamicValues { get; set; }

	public PluginResponse Initialize(PluginParameters args);

	public PluginResponse Execute(PluginParameters args);
}
