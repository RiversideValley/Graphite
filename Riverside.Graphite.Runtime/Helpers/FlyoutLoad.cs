using Riverside.Graphite.Runtime.CoreUi;
using Microsoft.UI.Xaml;

namespace Riverside.Graphite.Runtime.Helpers;

public static class FlyoutLoad
{
	public static XamlRoot XamlRoot { get; set; }

	public static void ShowFlyout(FrameworkElement element)
	{
		new SecurityInfo().ShowAt(element);
	}
}