using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Riverside.Graphite.Core;
public class AuthService
{
	private static readonly string UserDataFileName = "UsrCore.json";
	private static readonly string UserDataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore", UserDataFileName);
	public static List<User> users { get; set; } = LoadUsersFromJson();

	public static List<User> LoadUsersFromJson()
	{
		try
		{
			if (File.Exists(UserDataFilePath))
			{
				string json = File.ReadAllText(UserDataFilePath);
				if (!string.IsNullOrWhiteSpace(json))
				{
					return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
				}
			}
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Error deserializing user data: {ex.Message}");
		}
		catch (IOException ex)
		{
			Console.WriteLine($"Error reading user data file: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine($"Unauthorized access to user data file: {ex.Message}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Unexpected error loading user data: {ex.Message}");
		}

		return new List<User>();
	}

	public static User CurrentUser { get; private set; }

	public static bool IsUserAuthenticated => CurrentUser != null;

	public static bool SwitchUser(string username) => (CurrentUser = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) != null;

	public static bool Authenticate(string username) => SwitchUser(username);

	public static void AddUser(User newUser)
	{
		if (!users.Any(u => u.Username.Equals(newUser.Username, StringComparison.OrdinalIgnoreCase)))
		{
			users.Add(newUser);
			NewCreatedUser = newUser;
			SaveUsers();
		}
	}

	private static void SaveUsers()
	{
		try
		{
			File.WriteAllText(UserDataFilePath, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error saving user data: {ex.Message}");
		}
	}

	public static List<string> GetAllUsernames() => users.Select(u => u.Username).ToList();
	public static bool IsUserNameChanging { get; set; }
	public static void Logout() => CurrentUser = null;
	public static ChangeUsernameData UserWhomIsChanging { get; set; }

	public static User NewCreatedUser { get; set; }
	public static bool ChangeUsername(string oldUsername, string newUsername)
	{
		User userToChange = users.FirstOrDefault(u => u.Username.Equals(oldUsername, StringComparison.OrdinalIgnoreCase));
		if (userToChange == null || users.Any(u => u.Username.Equals(newUsername, StringComparison.OrdinalIgnoreCase)))
			return IsUserNameChanging = false;

		userToChange.Username = newUsername;

		SaveUsers();

		if (CurrentUser != null && CurrentUser.Username.Equals(oldUsername, StringComparison.OrdinalIgnoreCase))
			CurrentUser = userToChange;

		UserWhomIsChanging = new(oldUsername, userToChange.Username);
		return IsUserNameChanging = true;
	}

	public record ChangeUsernameData(string OldUsername, string NewUsername)
	{
		public FileInfo FileInfo { get; set; }
		public DirectoryInfo? DirectoryInfo { get; set; }

	}
}