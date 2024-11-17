using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Riverside.Graphite.Extensions;
public class PluginManager
{
	public HashSet<string> DirectoryPaths = new();
	public HashSet<IPluginBase> CurrentPlugins = new();

	public PluginManager(string DirectoryPath)
	{
		_ = DirectoryPaths.Add(DirectoryPath);
		LoadPlugins();
	}

	public PluginManager(List<string> DirectoryPaths)
	{
		this.DirectoryPaths = new HashSet<string>(DirectoryPaths);
		LoadPlugins();
	}

	public void LoadPlugins()
	{
		CurrentPlugins = new HashSet<IPluginBase>();

		foreach (string ele in DirectoryPaths)
		{
			DirectoryInfo dir = new(ele);

			foreach (FileInfo file in dir.GetFiles("*.dll"))
			{
				Assembly ass = Assembly.LoadFrom(file.FullName);
				foreach (Type t in ass.GetTypes())
				{
					if ((t.IsSubclassOf(typeof(IPluginBase)) || t.GetInterfaces().Contains(typeof(IPluginBase))) && t.IsAbstract == false)
					{
						IPluginBase b = t.InvokeMember(null,
											BindingFlags.CreateInstance, null, null, null) as IPluginBase;

						_ = CurrentPlugins.Add(b);
					}
				}
			}
		}
	}
}