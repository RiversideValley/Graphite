using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Riverside.Graphite.Data.Migration;

public class Migration
{
	public static MigrationData Migrate(Browser from)
	{
		MigrationData data = new();
		DirectoryInfo file = new(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
		string appData = file.Parent.FullName + @"\";

		// Get the path of the data folder of the browser
		string datapath = appData + Browser.Paths.ElementAt((int)from.BrowserName);

		if (!Directory.Exists(datapath)) return null;

		if (from.BrowserBase == Browser.Base.Chromium)
		{
			Chromium.Cookies.Apply(datapath);
		}

		return data;
	}
}
