using System.Collections.Generic;

namespace Riverside.Graphite.Data.Migration;

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
	public static List<string> Paths { get; } =
	// in the exact same order than the Name enum
	[
		@"Local\Microsoft\Edge\User Data",
		@"Local\Google\Chrome\User Data",
		@"Roaming\Opera Software\Opera Stable",
		@"Local\Packages\TheBrowserCompany.Arc_ttt1ap7aakyb4\LocalCache\Local\Arc\User Data",
        // Need to add Firefox and OperaGX, them Arc when available
    ];
}
