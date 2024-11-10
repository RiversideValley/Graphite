using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Fire.Browser.Core;

public static class UserDataManager
{
	public static readonly string CoreFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
	public static readonly string UsersFolderPath = "Users";
	public static readonly string CoreFilePath = Path.Combine(CoreFolderPath, "UsrCore.json");

	public static UserDataResult LoadUsers()
	{
		if (!File.Exists(CoreFilePath))
			return new() { Users = [], CurrentUsername = string.Empty };

		var users = JsonSerializer.Deserialize<List<User>>(File.ReadAllText(CoreFilePath)) ?? [];
		return new() { Users = users, CurrentUsername = AuthService.IsUserAuthenticated ? AuthService.CurrentUser.Username : "Guest" };
	}

	public static void SaveUsers(List<User> users)
	{
		Directory.CreateDirectory(CoreFolderPath);
		File.WriteAllText(CoreFilePath, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
	}

	public static void DeleteUser(string username)
	{
		try
		{
			var userData = LoadUsers();
			if (userData.Users.RemoveAll(u => u.Username == username) > 0)
			{
				string userFolderPath = Path.Combine(CoreFolderPath, UsersFolderPath, username);
				if (Directory.Exists(userFolderPath))
					Directory.Delete(userFolderPath, true);

				SaveUsers(userData.Users);
				AuthService.users = AuthService.LoadUsersFromJson();
			}
		}
		catch
		{
			throw;
		}
	}
}