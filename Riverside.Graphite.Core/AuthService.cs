using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Riverside.Graphite.Core;
public class AuthService 
{
	//private static readonly string UserDataFileName = "UsrCore.json";
	//private static readonly string UserDataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore", UserDataFileName);
	private static List<User> users; 
	public static List<User> Users
	{
		get
		{
			return LoadUserFromDatabase().Result	; 
			
		}
		set { 
			
			users = value;	
		}
	}

	public  event PropertyChangedEventHandler PropertyChanged;

	public async static Task<List<User>> LoadUserFromDatabase()
    {
        try
        {
            var tmp = await UserManager.GetAllUsersAsync();
            var converted = tmp.Select(u => new User
            {
                Id = Guid.NewGuid(),
                Username = u.Username,
                Email = u.Email,
                WindowsUserName = u.WindowsUserName,
                Password = string.Empty, // Assuming password is not available in UserV2
                IsFirstLaunch = u.IsFirstLaunch,
                UserSettings = new Settings() // Assuming default settings
            }).ToList();

            return Users = converted;
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

	public static bool SwitchUser(string username)
	{
		return (CurrentUser = Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) != null;
	}

	public static bool Authenticate(string username)
	{
		return SwitchUser(username);
	}

	//public static void AddUser(User newUser)
	//{
	//	if (!Users.Any(u => u.Username.Equals(newUser.Username, StringComparison.OrdinalIgnoreCase)))
	//	{
	//		Users.Add(newUser);
	//		NewCreatedUser = newUser;
	//	}
	//}  not used because Users is bassed on databaes of UserCore.   Add-> UserManager.CreateUserAsync

#nullable enable
	public static User? UserExists(string userName)
	{

		if (Users.Any(t => t.Username == userName))
		{
			return Users.Where(t => t.Username == userName).FirstOrDefault();
		}

		return null;

	}

	public static List<string> GetAllUsernames()
	{
		return LoadUserFromDatabase().Result.Select(t=> t.Username).ToList();

	}

	public static bool IsUserNameChanging { get; set; }
	public static void Logout()
	{
		CurrentUser = null;
	}

	public static ChangeUsernameData UserWhomIsChanging { get; set; }

	public static User NewCreatedUser { get; set; }

	public static bool ChangeUsername(string oldUsername, string newUsername)
	{
		User userToChange = Users.FirstOrDefault(u => u.Username.Equals(oldUsername, StringComparison.OrdinalIgnoreCase));
		if (userToChange == null || Users.Any(u => u.Username.Equals(newUsername, StringComparison.OrdinalIgnoreCase)))
		{
			return IsUserNameChanging = false;
		}

		userToChange.Username = newUsername;

		//SaveUsers();

		if (CurrentUser != null && CurrentUser.Username.Equals(oldUsername, StringComparison.OrdinalIgnoreCase))
		{
			CurrentUser = userToChange;
		}

		UserWhomIsChanging = new(oldUsername, userToChange.Username);
		return IsUserNameChanging = true;
	}

	public record ChangeUsernameData(string OldUsername, string NewUsername)
	{
		public FileInfo FileInfo { get; set; }
		public DirectoryInfo? DirectoryInfo { get; set; }
	}
}