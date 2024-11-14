using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Contracts;
public interface IDatabaseService
{
	Task<Task> DatabaseCreationValidation();
}