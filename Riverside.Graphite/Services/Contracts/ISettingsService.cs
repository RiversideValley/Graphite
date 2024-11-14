using Riverside.Graphite.Data.Core.Actions;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts;

public interface ISettingsService
{
	Task SaveChangesToSettings(Riverside.Graphite.Core.User user, Riverside.Graphite.Core.Settings settings);
	Riverside.Graphite.Core.Settings CoreSettings { get; }
	SettingsActions Actions { get; }
}