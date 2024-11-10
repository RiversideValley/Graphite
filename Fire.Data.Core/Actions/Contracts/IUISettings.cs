using System.Threading.Tasks;

namespace Fire.Data.Core.Actions.Contracts
{
	public interface IUISettings
	{
		Task<bool> UpdateSettingsAsync(Fire.Browser.Core.Settings settings);
		Task<Fire.Browser.Core.Settings> GetSettingsAsync();
		SettingsContext SettingsContext { get; }

	}
}
