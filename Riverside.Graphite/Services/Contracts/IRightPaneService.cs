using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Services.Contracts
{
	public interface IRightPaneService
	{
		void OpenInRightPane(string pageKey, object parameter = null);

		void Initialize(Frame rightPaneFrame, SplitView splitView);

		void CleanUp();
	}
}
