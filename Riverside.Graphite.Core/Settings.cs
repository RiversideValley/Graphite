using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Riverside.Graphite.Core;

public class Settings
{
    private void DefaultSettings()
    {
        Settings self = this;
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
        self.IsTrendingVisible = false;
        self.IsLogoVisible = true;
        self.IsAdBlockerEnabled = false;
        self.AdBlockerType = 0;
        self.Gender = "Male";
        self.BackDrop = "Mica";
        self.NewTabHistoryDownloads = false;
        self.NewTabHistoryFavorites = false;
        self.NewTabHistoryHistory = false;
        self.NewTabHistoryQuick = false;
        self.NewTabSelectorBarVisible = true;
        self.ConfirmCloseDlg = false;
        //self.WelcomeMsg = true; //why is this commented out every time code is pushed
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
        {
            DefaultSettings();
        }
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
    public string BackDrop { get; set; }
    public bool NewTabHistoryQuick { get; set; }
    public bool NewTabHistoryDownloads { get; set; }
    public bool NewTabHistoryFavorites { get; set; }
    public bool NewTabHistoryHistory { get; set; }
    public bool NewTabSelectorBarVisible { get; set; } // Use "0" for false, "1" for true

    // public bool WelcomeMsg { get; set; }

    public static implicit operator Settings(DbSettings v)
    {
        return new Settings(v);
    }

	public void FromDictionary(Dictionary<string, object> dict)
	{
		foreach (var kvp in dict)
		{
			switch (kvp.Key)
			{
				case nameof(PackageName):
					PackageName = kvp.Value as string;
					break;
				case nameof(DisableJavaScript):
					DisableJavaScript = (bool)kvp.Value;
					break;
				case nameof(DisablePassSave):
					DisablePassSave = (bool)kvp.Value;
					break;
				case nameof(DisableWebMess):
					DisableWebMess = (bool)kvp.Value;
					break;
				case nameof(DisableGenAutoFill):
					DisableGenAutoFill = (bool)kvp.Value;
					break;
				case nameof(ColorBackground):
					ColorBackground = kvp.Value as string;
					break;
				case nameof(Gender):
					Gender = kvp.Value as string;
					break;
				case nameof(StatusBar):
					StatusBar = (bool)kvp.Value;
					break;
				case nameof(BrowserKeys):
					BrowserKeys = (bool)kvp.Value;
					break;
				case nameof(BrowserScripts):
					BrowserScripts = (bool)kvp.Value;
					break;
				case nameof(Useragent):
					Useragent = kvp.Value as string;
					break;
				case nameof(LightMode):
					LightMode = (bool)kvp.Value;
					break;
				case nameof(OpSw):
					OpSw = (bool)kvp.Value;
					break;
				case nameof(EngineFriendlyName):
					EngineFriendlyName = kvp.Value as string;
					break;
				case nameof(SearchUrl):
					SearchUrl = kvp.Value as string;
					break;
				case nameof(ColorTool):
					ColorTool = kvp.Value as string;
					break;
				case nameof(ColorTV):
					ColorTV = kvp.Value as string;
					break;
				case nameof(AdBlockerType):
					AdBlockerType = (int)kvp.Value;
					break;
				case nameof(Background):
					Background = (int)kvp.Value;
					break;
				case nameof(IsAdBlockerEnabled):
					IsAdBlockerEnabled = (bool)kvp.Value;
					break;
				case nameof(Auto):
					Auto = (bool)kvp.Value;
					break;
				case nameof(Lang):
					Lang = kvp.Value as string;
					break;
				case nameof(ReadButton):
					ReadButton = (bool)kvp.Value;
					break;
				case nameof(AdblockBtn):
					AdblockBtn = (bool)kvp.Value;
					break;
				case nameof(Downloads):
					Downloads = (bool)kvp.Value;
					break;
				case nameof(Translate):
					Translate = (bool)kvp.Value;
					break;
				case nameof(Favorites):
					Favorites = (bool)kvp.Value;
					break;
				case nameof(Historybtn):
					Historybtn = (bool)kvp.Value;
					break;
				case nameof(QrCode):
					QrCode = (bool)kvp.Value;
					break;
				case nameof(FavoritesL):
					FavoritesL = (bool)kvp.Value;
					break;
				case nameof(ToolIcon):
					ToolIcon = (bool)kvp.Value;
					break;
				case nameof(DarkIcon):
					DarkIcon = (bool)kvp.Value;
					break;
				case nameof(OpenTabHandel):
					OpenTabHandel = (bool)kvp.Value;
					break;
				case nameof(BackButton):
					BackButton = (bool)kvp.Value;
					break;
				case nameof(ForwardButton):
					ForwardButton = (bool)kvp.Value;
					break;
				case nameof(RefreshButton):
					RefreshButton = (bool)kvp.Value;
					break;
				case nameof(IsLogoVisible):
					IsLogoVisible = (bool)kvp.Value;
					break;
				case nameof(HomeButton):
					HomeButton = (bool)kvp.Value;
					break;
				case nameof(PipMode):
					PipMode = (bool)kvp.Value;
					break;
				case nameof(NtpDateTime):
					NtpDateTime = (bool)kvp.Value;
					break;
				case nameof(ExitDialog):
					ExitDialog = (bool)kvp.Value;
					break;
				case nameof(NtpTextColor):
					NtpTextColor = kvp.Value as string;
					break;
				case nameof(ExceptionLog):
					ExceptionLog = kvp.Value as string;
					break;
				case nameof(Eq2fa):
					Eq2fa = (bool)kvp.Value;
					break;
				case nameof(Eqfav):
					Eqfav = (bool)kvp.Value;
					break;
				case nameof(EqHis):
					EqHis = (bool)kvp.Value;
					break;
				case nameof(Eqsets):
					Eqsets = (bool)kvp.Value;
					break;
				case nameof(TrackPrevention):
					TrackPrevention = (int)kvp.Value;
					break;
				case nameof(ResourceSave):
					ResourceSave = (bool)kvp.Value;
					break;
				case nameof(ConfirmCloseDlg):
					ConfirmCloseDlg = (bool)kvp.Value;
					break;
				case nameof(IsFavoritesToggled):
					IsFavoritesToggled = (bool)kvp.Value;
					break;
				case nameof(IsSearchBoxToggled):
					IsSearchBoxToggled = (bool)kvp.Value;
					break;
				case nameof(IsHistoryToggled):
					IsHistoryToggled = (bool)kvp.Value;
					break;
				case nameof(IsHistoryVisible):
					IsHistoryVisible = (bool)kvp.Value;
					break;
				case nameof(IsFavoritesVisible):
					IsFavoritesVisible = (bool)kvp.Value;
					break;
				case nameof(IsSearchVisible):
					IsSearchVisible = (bool)kvp.Value;
					break;
				case nameof(IsTrendingVisible):
					IsTrendingVisible = (bool)kvp.Value;
					break;
				case nameof(NtpCoreVisibility):
					NtpCoreVisibility = (bool)kvp.Value;
					break;
				case nameof(BackDrop):
					BackDrop = kvp.Value as string;
					break;
				case nameof(NewTabHistoryQuick):
					NewTabHistoryQuick = (bool)kvp.Value;
					break;
				case nameof(NewTabHistoryDownloads):
					NewTabHistoryDownloads = (bool)kvp.Value;
					break;
				case nameof(NewTabHistoryFavorites):
					NewTabHistoryFavorites = (bool)kvp.Value;
					break;
				case nameof(NewTabHistoryHistory):
					NewTabHistoryHistory = (bool)kvp.Value;
					break;
				case nameof(NewTabSelectorBarVisible):
					NewTabSelectorBarVisible = (bool)kvp.Value;
					break;
			}
		}
	}

	public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { nameof(PackageName), PackageName },
            { nameof(DisableJavaScript), DisableJavaScript },
            { nameof(DisablePassSave), DisablePassSave },
            { nameof(DisableWebMess), DisableWebMess },
            { nameof(DisableGenAutoFill), DisableGenAutoFill },
            { nameof(ColorBackground), ColorBackground },
            { nameof(Gender), Gender },
            { nameof(StatusBar), StatusBar },
            { nameof(BrowserKeys), BrowserKeys },
            { nameof(BrowserScripts), BrowserScripts },
            { nameof(Useragent), Useragent },
            { nameof(LightMode), LightMode },
            { nameof(OpSw), OpSw },
            { nameof(EngineFriendlyName), EngineFriendlyName },
            { nameof(SearchUrl), SearchUrl },
            { nameof(ColorTool), ColorTool },
            { nameof(ColorTV), ColorTV },
            { nameof(AdBlockerType), AdBlockerType },
            { nameof(Background), Background },
            { nameof(IsAdBlockerEnabled), IsAdBlockerEnabled },
            { nameof(Auto), Auto },
            { nameof(Lang), Lang },
            { nameof(ReadButton), ReadButton },
            { nameof(AdblockBtn), AdblockBtn },
            { nameof(Downloads), Downloads },
            { nameof(Translate), Translate },
            { nameof(Favorites), Favorites },
            { nameof(Historybtn), Historybtn },
            { nameof(QrCode), QrCode },
            { nameof(FavoritesL), FavoritesL },
            { nameof(ToolIcon), ToolIcon },
            { nameof(DarkIcon), DarkIcon },
            { nameof(OpenTabHandel), OpenTabHandel },
            { nameof(BackButton), BackButton },
            { nameof(ForwardButton), ForwardButton },
            { nameof(RefreshButton), RefreshButton },
            { nameof(IsLogoVisible), IsLogoVisible },
            { nameof(HomeButton), HomeButton },
            { nameof(PipMode), PipMode },
            { nameof(NtpDateTime), NtpDateTime },
            { nameof(ExitDialog), ExitDialog },
            { nameof(NtpTextColor), NtpTextColor },
            { nameof(ExceptionLog), ExceptionLog },
            { nameof(Eq2fa), Eq2fa },
            { nameof(Eqfav), Eqfav },
            { nameof(EqHis), EqHis },
            { nameof(Eqsets), Eqsets },
            { nameof(TrackPrevention), TrackPrevention },
            { nameof(ResourceSave), ResourceSave },
            { nameof(ConfirmCloseDlg), ConfirmCloseDlg },
            { nameof(IsFavoritesToggled), IsFavoritesToggled },
            { nameof(IsSearchBoxToggled), IsSearchBoxToggled },
            { nameof(IsHistoryToggled), IsHistoryToggled },
            { nameof(IsHistoryVisible), IsHistoryVisible },
            { nameof(IsFavoritesVisible), IsFavoritesVisible },
            { nameof(IsSearchVisible), IsSearchVisible },
            { nameof(IsTrendingVisible), IsTrendingVisible },
            { nameof(NtpCoreVisibility), NtpCoreVisibility },
            { nameof(BackDrop), BackDrop },
            { nameof(NewTabHistoryQuick), NewTabHistoryQuick },
            { nameof(NewTabHistoryDownloads), NewTabHistoryDownloads },
            { nameof(NewTabHistoryFavorites), NewTabHistoryFavorites },
            { nameof(NewTabHistoryHistory), NewTabHistoryHistory },
            { nameof(NewTabSelectorBarVisible), NewTabSelectorBarVisible }
        };
    }
}
