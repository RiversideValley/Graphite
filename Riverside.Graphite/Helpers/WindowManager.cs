using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Riverside.Graphite.Helpers
{
	public static class WindowManager
	{
		public static List<Window> Views { get; } = new List<Window>();

		public static void RegisterWindow(Window window)
		{
			Views.Add(window);
		}

		public static void UnregisterWindow(Window window)
		{
			Views.Remove(window);
		}
	}
}
