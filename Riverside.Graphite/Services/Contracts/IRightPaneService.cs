using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Contracts
{
	public interface IRightPaneService
	{
		void OpenInRightPane(string pageKey, object parameter = null);

		void Initialize(Frame rightPaneFrame, SplitView splitView);

		void CleanUp();
	}
}
