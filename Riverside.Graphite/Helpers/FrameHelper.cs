using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Helpers
{
	public static class FrameHelper
	{

		public static object GetPageViewModel(this Frame frame)
			=> frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);

	}
}
