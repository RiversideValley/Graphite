using FireBrowserWinUi3DataCore.Actions;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts;

public interface ISettingsService
{
    Task SaveChangesToSettings(FireBrowserWinUi3MultiCore.User user, FireBrowserWinUi3MultiCore.Settings settings);
    FireBrowserWinUi3MultiCore.Settings CoreSettings { get; }
    SettingsActions Actions { get; }
}