﻿using System.ComponentModel.DataAnnotations;

namespace Riverside.Graphite.Core;

public class DbSettings
{
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
	public string BackDrop { get; set; }
	public bool NewTabHistoryQuick { get; set; }
	public bool NewTabHistoryDownloads { get; set; }
	public bool NewTabHistoryFavorites { get; set; }
	public bool NewTabHistoryHistory { get; set; }
	public bool NewTabSelectorBarVisible { get; set; }
	// public bool WelcomeMsg { get; set; }
}