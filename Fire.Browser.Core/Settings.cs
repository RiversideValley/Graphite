using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Fire.Browser.Core;
public class DbSettings
{

	[Key]
	public string PackageName { get; set; }
	public bool DisableJavaScript { get; set; } // Use "0" for false, "1" for true
	public bool DisablePassSave { get; set; } // Use "0" for false, "1" for true
	public bool DisableWebMess { get; set; } // Use "0" for false, "1" for true
	public bool DisableGenAutoFill { get; set; } // Use "0" for false, "1" for true
	public string ColorBackground { get; set; }
	public bool StatusBar { get; set; } // Use "0" for false, "1" for true
	public bool BrowserKeys { get; set; } // Use "0" for false, "1" for true
	public bool BrowserScripts { get; set; } // Use "0" for false, "1" for true
	public string Useragent { get; set; }
	public bool LightMode { get; set; } // Use "0" for false, "1" for true
	public bool OpSw { get; set; } // Use "0" for false, "1" for true
	public string EngineFriendlyName { get; set; }
	public string SearchUrl { get; set; }
	public string ColorTool { get; set; }
	public string ColorTV { get; set; }

	public string Gender { get; set; }
	public int AdBlockerType { get; set; }
	public int Background { get; set; } // Use "0" for false, "1" for true
	public bool IsAdBlockerEnabled { get; set; } // Use "0" for false, "1" for true
	public bool Auto { get; set; } // Use "0" for false, "1" for true
	public string Lang { get; set; }
	public bool ReadButton { get; set; }
	public bool AdblockBtn { get; set; }
	public bool Downloads { get; set; }
	public bool Translate { get; set; }
	public bool Favorites { get; set; }
	public bool Historybtn { get; set; }
	public bool QrCode { get; set; }
	public bool FavoritesL { get; set; }
	public bool ToolIcon { get; set; }
	public bool DarkIcon { get; set; }
	public bool OpenTabHandel { get; set; }
	public bool BackButton { get; set; }
	public bool ForwardButton { get; set; }
	public bool RefreshButton { get; set; }
	public bool IsLogoVisible { get; set; }
	public bool HomeButton { get; set; }
	public bool PipMode { get; set; }
	public bool NtpDateTime { get; set; }
	public bool ExitDialog { get; set; }
	public string NtpTextColor { get; set; }
	public string ExceptionLog { get; set; }

	public bool Eq2fa { get; set; }
	public bool Eqfav { get; set; }
	public bool EqHis { get; set; }
	public bool Eqsets { get; set; }
	public int TrackPrevention { get; set; }
	public bool ResourceSave { get; set; }
	public bool ConfirmCloseDlg { get; set; }
	public bool IsFavoritesToggled { get; set; }
	public bool IsSearchBoxToggled { get; set; }
	public bool IsHistoryToggled { get; set; }
	public bool IsHistoryVisible { get; set; }
	public bool IsFavoritesVisible { get; set; }
	public bool IsSearchVisible { get; set; }
	public bool IsTrendingVisible { get; set; }
	public bool NtpCoreVisibility { get; set; }

	public bool NewTabHistoryQuick { get; set; }

	public bool NewTabHistoryDownloads { get; set; }

	public bool NewTabHistoryFavorites { get; set; }

	public bool NewTabHistoryHistory { get; set; }


	// public bool WelcomeMsg { get; set; }

}
public class Settings
{
	void DefaultSettings()
	{
		var self = this;
		self.PackageName = "FireBrowswerWinUi3_"; //+ Guid.NewGuid().ToString();
		self.DisableJavaScript = false;
		self.DisablePassSave = false;
		self.DisableWebMess = false;
		self.DisableGenAutoFill = false;
		self.ColorBackground = "#000000";
		self.StatusBar = true;
		self.BrowserKeys = true;
		self.BrowserScripts = true;
		self.Useragent = "WebView";
		self.LightMode = false;
		self.OpSw = true;
		self.EngineFriendlyName = "Google";
		self.SearchUrl = "https://www.google.com/search?q=";
		self.ColorTool = "#000000";
		self.ColorTV = "#000000";
		self.Background = 0;
		self.Auto = false;
		self.Lang = "nl-NL";
		self.ReadButton = true;
		self.AdblockBtn = true;
		self.Downloads = true;
		self.Translate = true;
		self.Favorites = true;
		self.Historybtn = true;
		self.QrCode = true;
		self.FavoritesL = true;
		self.ToolIcon = true;
		self.DarkIcon = true;
		self.OpenTabHandel = false;
		self.NtpDateTime = true;
		self.ExitDialog = false;
		self.NtpTextColor = "#FFFFFF";
		self.ExceptionLog = "Low";
		self.Eq2fa = true;
		self.Eqfav = false;
		self.EqHis = false;
		self.Eqsets = false;
		self.TrackPrevention = 2;
		self.ResourceSave = false;
		self.ConfirmCloseDlg = true;
		self.IsHistoryToggled = false;
		self.IsFavoritesToggled = false;
		self.IsSearchBoxToggled = false;
		self.IsFavoritesVisible = true;
		self.IsHistoryVisible = true;
		self.IsSearchVisible = true;
		self.NtpCoreVisibility = true;
		self.BackButton = true;
		self.ForwardButton = true;
		self.RefreshButton = true;
		self.HomeButton = true;
		self.PipMode = false;
		self.IsTrendingVisible = true;
		self.IsLogoVisible = true;
		self.IsAdBlockerEnabled = false;
		self.AdBlockerType = 0;
		self.Gender = "Male";
		self.NewTabHistoryDownloads = false;
		self.NewTabHistoryFavorites = false;
		self.NewTabHistoryHistory = false;
		self.NewTabHistoryQuick = false;
		//self.WelcomeMsg = true;
	}

	[JsonIgnore]
	[NotMapped]
	public Settings Self { get; set; }

	public Settings(Settings settings)
	{
		Self = settings;
	}
	public Settings(bool LoadDefaults)
	{
		Self = this;

		if (LoadDefaults)
			DefaultSettings();
	}
	public Settings() { }

	[Key]
	public string PackageName { get; set; }
	public bool DisableJavaScript { get; set; } // Use "0" for false, "1" for true
	public bool DisablePassSave { get; set; } // Use "0" for false, "1" for true
	public bool DisableWebMess { get; set; } // Use "0" for false, "1" for true
	public bool DisableGenAutoFill { get; set; } // Use "0" for false, "1" for true
	public string ColorBackground { get; set; }

	public string Gender { get; set; }


	public bool StatusBar { get; set; } // Use "0" for false, "1" for true
	public bool BrowserKeys { get; set; } // Use "0" for false, "1" for true
	public bool BrowserScripts { get; set; } // Use "0" for false, "1" for true
	public string Useragent { get; set; }
	public bool LightMode { get; set; } // Use "0" for false, "1" for true
	public bool OpSw { get; set; } // Use "0" for false, "1" for true
	public string EngineFriendlyName { get; set; }
	public string SearchUrl { get; set; }
	public string ColorTool { get; set; }
	public string ColorTV { get; set; }
	public int AdBlockerType { get; set; }
	public int Background { get; set; } // Use "0" for false, "1" for true
	public bool IsAdBlockerEnabled { get; set; } // Use "0" for false, "1" for true
	public bool Auto { get; set; } // Use "0" for false, "1" for true
	public string Lang { get; set; }
	public bool ReadButton { get; set; }
	public bool AdblockBtn { get; set; }
	public bool Downloads { get; set; }
	public bool Translate { get; set; }
	public bool Favorites { get; set; }
	public bool Historybtn { get; set; }
	public bool QrCode { get; set; }
	public bool FavoritesL { get; set; }
	public bool ToolIcon { get; set; }
	public bool DarkIcon { get; set; }
	public bool OpenTabHandel { get; set; }
	public bool BackButton { get; set; }
	public bool ForwardButton { get; set; }
	public bool RefreshButton { get; set; }
	public bool IsLogoVisible { get; set; }
	public bool HomeButton { get; set; }
	public bool PipMode { get; set; }
	public bool NtpDateTime { get; set; }
	public bool ExitDialog { get; set; }
	public string NtpTextColor { get; set; }
	public string ExceptionLog { get; set; }
	public bool Eq2fa { get; set; }
	public bool Eqfav { get; set; }
	public bool EqHis { get; set; }
	public bool Eqsets { get; set; }

	public int TrackPrevention { get; set; }
	public bool ResourceSave { get; set; }
	public bool ConfirmCloseDlg { get; set; }
	public bool IsFavoritesToggled { get; set; }
	public bool IsSearchBoxToggled { get; set; }
	public bool IsHistoryToggled { get; set; }
	public bool IsHistoryVisible { get; set; }
	public bool IsFavoritesVisible { get; set; }
	public bool IsSearchVisible { get; set; }
	public bool IsTrendingVisible { get; set; }
	public bool NtpCoreVisibility { get; set; }

	public bool NewTabHistoryQuick { get; set; }

	public bool NewTabHistoryDownloads { get; set; }

	public bool NewTabHistoryFavorites { get; set; }

	public bool NewTabHistoryHistory { get; set; }

	// public bool WelcomeMsg { get; set; }


	public static implicit operator Settings(DbSettings v)
	{
		return new Settings(v);
	}
}