using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Contracts
{
	public interface INavigationService
	{
		event NavigatedEventHandler Navigated;

		bool CanGoBack { get; }

		Frame Frame { get; set; }

		bool NavigateTo(string pageKey, object parameter = null, bool clearNavigation = false);

		bool GoBack();
	}
}
