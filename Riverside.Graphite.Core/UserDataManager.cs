using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Riverside.Graphite.Core;

public static class UserDataManager
{
	public static string GetFullPathToExe()
	{
		string path = AppDomain.CurrentDomain.BaseDirectory;
		int pos = path.LastIndexOf("\\");
		return path[..pos];
	}

	public static readonly string CoreFolderPath =  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
	public static readonly string UsersFolderPath = "Users";
	public async static void DeleteUser(string username)
	{
		try
		{
			await UserManager.DeleteUserAsync(username); 

			string userFolderPath = Path.Combine(CoreFolderPath, UsersFolderPath, username);

			if (Directory.Exists(userFolderPath))
			{
				Directory.Delete(userFolderPath, true);
			}
	
		}
		catch(Exception ex)
		{
			Helper.Logging.ExceptionLogger.LogException(ex);	
		}
	}
}