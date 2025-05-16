using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static Riverside.Graphite.Core.AuthService;

namespace Riverside.Graphite;

public sealed partial class ChangeUsernameCore : Window
{
	private AppWindow appWindow;
	private AppWindowTitleBar titleBar;
	private DispatcherTimer restartTimer;

	public ChangeUsernameCore()
	{
		AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(500, 500, 850, 500));
		Riverside.Graphite.Runtime.Helpers.Windowing.Center(this);
		AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
		AppWindow.MoveInZOrderAtTop();
		AppWindow.ShowOnceWithRequestedStartupState();

		InitializeComponent();

		title();
		UpdateUser();
	}


	public void title()
	{
		nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

		appWindow = AppWindow.GetFromWindowId(windowId);

		if (!AppWindowTitleBar.IsCustomizationSupported())
		{
			// Why? Because I don't care
			throw new Exception("Unsupported OS version.");
		}
		else
		{
			titleBar = appWindow.TitleBar;
			titleBar.ExtendsContentIntoTitleBar = true;
			Windows.UI.Color btnColor = Colors.Transparent;
			titleBar.BackgroundColor = btnColor;
			titleBar.ButtonBackgroundColor = btnColor;
			titleBar.InactiveBackgroundColor = btnColor;
			titleBar.ButtonInactiveBackgroundColor = btnColor;
		}
	}

	public async Task ChangeUsername()
	{
		try
		{
			// Read the JSON file
			string tempFolderPath = Path.GetTempPath();
			string jsonFilePath = Path.Combine(tempFolderPath, "changeusername.json");

			if (!File.Exists(jsonFilePath))
			{
				Console.WriteLine("Change username JSON file not found.");
				return;
			}

			// Deserialize the JSON content
			string jsonContent = File.ReadAllText(jsonFilePath);
			ChangeUsernameData changeUsernameData = JsonSerializer.Deserialize<ChangeUsernameData>(jsonContent);

			// Update the UI with old and new usernames
			Username.Text = $"{changeUsernameData.OldUsername} -> {changeUsernameData.NewUsername}";

			// Rename the folder
			string usersFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore", "Users");
			string oldUserFolderPath = Path.Combine(usersFolderPath, changeUsernameData.OldUsername);
			string newUserFolderPath = Path.Combine(usersFolderPath, changeUsernameData.NewUsername);

			if (Directory.Exists(oldUserFolderPath))
			{
				Directory.Move(oldUserFolderPath, newUserFolderPath);
				Console.WriteLine($"Folder renamed from '{changeUsernameData.OldUsername}' to '{changeUsernameData.NewUsername}'.");
			}
			else
			{
				Console.WriteLine($"Folder '{changeUsernameData.OldUsername}' not found.");
			}

			string usersFolderPathV2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore", "Users", UserManager.GraphiteDataPath);
			string oldUserFolderPathV2 = Path.Combine(usersFolderPathV2, changeUsernameData.OldUsername);
			string newUserFolderPathV2 = Path.Combine(usersFolderPathV2, changeUsernameData.NewUsername);

			if (Directory.Exists(oldUserFolderPathV2))
			{
				Directory.Move(oldUserFolderPathV2, newUserFolderPathV2);
				Console.WriteLine($"Folder renamed from '{changeUsernameData.OldUsername}' to '{changeUsernameData.NewUsername}'.");
			}
			else
			{
				Console.WriteLine($"Folder '{changeUsernameData.OldUsername}' not found.");
			}

			// Remove the JSON file
			var existingUser = await UserManager.GetUserAsync(changeUsernameData.OldUsername);

			if (existingUser != null)
			{
				existingUser.Username = changeUsernameData.NewUsername;
				await UserManager.UpdateUserPropertiesAsync(existingUser, changeUsernameData.OldUsername);
				await UserManager.UpdateSecurityInfoAsync(existingUser, changeUsernameData.OldUsername);
			}

			File.Delete(jsonFilePath);
			Console.WriteLine("Change username JSON file deleted.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
		}


	}

	private async void UpdateUser()
	{
		try
		{
			await Task.Delay(2000).ContinueWith(async t =>
			{
				try
				{
					await ChangeUsername();
				}
				catch (Exception)
				{

					throw;
				}
			});

			string tempFolderPath = Path.GetTempPath();
			string jsonFilePath = Path.Combine(tempFolderPath, "changeusername.json");
			File.Delete(jsonFilePath);
		}
		catch (Exception)
		{
			;
		}
		finally
		{
			_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
		}

	}

	private void ManaulRestart_Click(object sender, RoutedEventArgs e)
	{
		// Read the JSON file
		try
		{
			string tempFolderPath = Path.GetTempPath();
			string jsonFilePath = Path.Combine(tempFolderPath, "changeusername.json");
			File.Delete(jsonFilePath);
		}
		catch (Exception)
		{
			;
		}
		_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
	}
}