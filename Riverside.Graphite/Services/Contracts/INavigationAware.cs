using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Contracts
{
	public interface INavigationAware
	{
		void OnNavigatedTo(object parameter);

		void OnNavigatedFrom();
	}
}
