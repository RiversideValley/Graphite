using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Helpers
{
	public static class WindowHelper
	{
		public static Window GetWindowForElement(UIElement element)
		{
			if (element.XamlRoot != null)
			{
				foreach (var view in WindowManager.Views)
				{
					if (view.Content.XamlRoot == element.XamlRoot)
					{
						return view;
					}
				}
			}
			return null;
		}
	}
}
