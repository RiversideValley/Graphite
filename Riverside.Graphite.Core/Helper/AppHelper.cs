using CommunityToolkit.WinUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Riverside.Graphite.Core.Helper
{
	public class AppHelper
	{
		private static Tuple<int, int, int, int> GetVersionDescription()
		{
			string appName = "AppDisplayName".GetLocalized();
			Package package = Package.Current;
			PackageId packageId = package.Id;
			PackageVersion version = packageId.Version;
			return new Tuple<int, int, int, int>(version.Major, version.Minor, version.Build, version.Revision);
		}
	}
}
