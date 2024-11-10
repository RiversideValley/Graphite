using System;
using System.Collections.ObjectModel;
using static Fire.Browser.Services.PluginCore.IPluginCore;

namespace Fire.Browser.Services.PluginCore;
public interface IPluginBase
{
	public String Name { get; set; }
	public String Description { get; set; }

	public ObservableCollection<object> DynamicValues { get; set; }

	public PluginResponse Initialize(PluginParameters args);

	public PluginResponse Execute(PluginParameters args);
}
