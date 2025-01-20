using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Core;

public static class UserFolderManager
{
	private static readonly string[] SubFolderNames = ["Settings", "Database", "Browser", "Modules", "Permissions"];

	public static void CreateUserFolders(User user)
	{
		string userFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, user.Username);

		_ = Parallel.ForEach(SubFolderNames, folderName =>
			Directory.CreateDirectory(Path.Combine(userFolderPath, folderName)));
	}
}