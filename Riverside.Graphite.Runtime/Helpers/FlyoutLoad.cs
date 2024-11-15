using Microsoft.UI.Xaml;
using Riverside.Graphite.Runtime.CoreUi;

namespace Riverside.Graphite.Runtime.Helpers;

public static class FlyoutLoad
{
	public static XamlRoot XamlRoot { get; set; }

	public static void ShowFlyout(FrameworkElement element)
	{
		new SecurityInfo().ShowAt(element);
	}
}