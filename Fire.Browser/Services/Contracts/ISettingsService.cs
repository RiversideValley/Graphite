using Fire.Data.Core.Actions;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts;

public interface ISettingsService
{
    Task SaveChangesToSettings(Fire.Browser.Core.User user, Fire.Browser.Core.Settings settings);
    Fire.Browser.Core.Settings CoreSettings { get; }
    SettingsActions Actions { get; }
}