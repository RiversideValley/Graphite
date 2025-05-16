using System;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.Graphite.Core;

public static class UserDataManager
{
	public static string GetFullPathToExe()
	{
		string path = AppDomain.CurrentDomain.BaseDirectory;
		int pos = path.LastIndexOf("\\");
		return path[..pos];
	}

	public static readonly string CoreFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
	public static readonly string UsersFolderPath = "Users";
	public async static Task DeleteUser(string username)
	{
		try
		{
			await UserManager.DeleteUserAsync(username);

			string userFolderPath = Path.Combine(CoreFolderPath, UsersFolderPath, username);

			if (Directory.Exists(userFolderPath))
			{
				Directory.Delete(userFolderPath, true);
			}

			if (Directory.Exists(Path.Combine(UserManager.GraphiteDataPath, username)))
			{
				Directory.Delete(Path.Combine(UserManager.GraphiteDataPath, username), true);
			}

		}
		catch (Exception ex)
		{
			Helper.Logging.ExceptionLogger.LogException(ex);
		}
	}
}