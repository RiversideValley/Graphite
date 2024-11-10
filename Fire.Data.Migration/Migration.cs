using FireBrowserWinUi3DataCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace FireBrowserWinUi3Migration;

public class MigrationData
{
    //make this cookies history and favorites
    public List<HistoryItem> History { get; set; }
}

public class Browser
{
    //need to make it work
    public enum Name
    {
        Edge,
        Chrome,
        Opera,
        ArcBrowser,

    }

    public enum Base
    {
        Chromium,
        Gecko
    }
    public Name BrowserName { get; set; }
    public Base BrowserBase { get; set; }


    public static Browser Edge { get; } = new() { BrowserName = Name.Edge, BrowserBase = Base.Chromium };
    public static Browser Chrome { get; } = new() { BrowserName = Name.Chrome, BrowserBase = Base.Chromium };
    public static Browser Opera { get; } = new() { BrowserName = Name.Opera, BrowserBase = Base.Chromium };
    public static Browser Arc { get; } = new() { BrowserName = Name.ArcBrowser, BrowserBase = Base.Chromium };
    /// <summary>
    /// Use this list to get the path of a browser by its Name (enum) as an int
    /// </summary>
    public static List<string> Paths { get; } = new() // in the exact same order than the Name enum
    {
        @"Local\Microsoft\Edge\User Data",
        @"Local\Google\Chrome\User Data",
        @"Roaming\Opera Software\Opera Stable",
        @"Local\Packages\TheBrowserCompany.Arc_ttt1ap7aakyb4\LocalCache\Local\Arc\User Data",
        // Need to add Firefox and OperaGX, them Arc when available
    };
}



public class Migration
{
    public static MigrationData Migrate(Browser from)
    {
        MigrationData data = new();
        DirectoryInfo file = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
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
