using Riverside.Graphite.Core;
using System.Threading.Tasks;

namespace Riverside.Graphite.Data.Core.Actions.Contracts
{
	public interface IUISettings
	{
		Task<bool> UpdateSettingsAsync(Settings settings);
		Task<Settings> GetSettingsAsync();
		SettingsContext SettingsContext { get; }
	}
}
