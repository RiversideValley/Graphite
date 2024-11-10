using FireBrowserWinUi3MultiCore;

namespace FireBrowserWinUi3DataCore.Models;
public class DbSettings : FireBrowserWinUi3MultiCore.Settings
{

    public FireBrowserWinUi3MultiCore.Settings Settings { get; set; }
    public DbSettings() : base(AuthService.CurrentUser?.UserSettings)
    {
        Settings = AuthService.CurrentUser?.UserSettings ?? new Settings();
    }

    public DbSettings(Settings settings) : base(settings)
    {
        Settings = settings;
    }
}
