using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts;
public interface IDatabaseService
{
	Task<Task> DatabaseCreationValidation();
}