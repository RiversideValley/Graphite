using System.Threading.Tasks;

namespace Fire.Data.Core.Actions.Contracts
{
	public interface IUISettings
	{
		Task<bool> UpdateSettingsAsync(Riverside.Graphite.Core.Settings settings);
		Task<Riverside.Graphite.Core.Settings> GetSettingsAsync();
		SettingsContext SettingsContext { get; }

	}
}
