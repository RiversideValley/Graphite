using Microsoft.UI.Xaml;

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
