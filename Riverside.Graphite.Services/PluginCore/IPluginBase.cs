using System;
using System.Collections.ObjectModel;
using static Riverside.Graphite.Services.PluginCore.IPluginCore;

namespace Riverside.Graphite.Services.PluginCore;
public interface IPluginBase
{
	public String Name { get; set; }
	public String Description { get; set; }

	public ObservableCollection<object> DynamicValues { get; set; }

	public PluginResponse Initialize(PluginParameters args);

	public PluginResponse Execute(PluginParameters args);
}
