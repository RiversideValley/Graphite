using Fire.Browser.Core;

namespace Fire.Data.Core.Models;
public class DbSettings : Fire.Browser.Core.Settings
{

    public Fire.Browser.Core.Settings Settings { get; set; }
    public DbSettings() : base(AuthService.CurrentUser?.UserSettings)
    {
        Settings = AuthService.CurrentUser?.UserSettings ?? new Settings();
    }

    public DbSettings(Settings settings) : base(settings)
    {
        Settings = settings;
    }
}
