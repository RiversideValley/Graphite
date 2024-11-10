using System.Threading.Tasks;

namespace FireBrowserWinUi3DataCore.Actions.Contracts
{
    public interface IUISettings
    {
        Task<bool> UpdateSettingsAsync(FireBrowserWinUi3MultiCore.Settings settings);
        Task<FireBrowserWinUi3MultiCore.Settings> GetSettingsAsync();
        SettingsContext SettingsContext { get; }

    }
}
