using Riverside.Graphite.Core;

namespace Riverside.Graphite.Data.Core.Models;
public class DbSettings : Riverside.Graphite.Core.Settings
{
	public Riverside.Graphite.Core.Settings Settings { get; set; }
	public DbSettings() : base(AuthService.CurrentUser?.UserSettings)
	{
		Settings = AuthService.CurrentUser?.UserSettings ?? new Settings();
	}

	public DbSettings(Settings settings) : base(settings)
	{
		Settings = settings;
	}
}
