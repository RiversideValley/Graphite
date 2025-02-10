using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Windows;
using Graphite.UserSys.Windows;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;

namespace Graphite.UserSys
{
	public class MigrationManager
	{
		private readonly ILogger<MigrationManager> _logger;
		private readonly string _oldBasePath;
		private readonly string _newBasePath;
		private readonly string _newDbPath;

		public MigrationManager(ILogger<MigrationManager> logger)
		{
			_logger = logger;
			_oldBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireBrowserUserCore");
			_newBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");
			_newDbPath = Path.Combine(_newBasePath, "UserCore.db");
		}

		public async Task PerformFullMigrationAsync()
		{
			_logger.LogInformation("Starting full migration process");


			try
			{
				string usrCorePath = Path.Combine(_oldBasePath, "UsrCore.json");
				if (!File.Exists(usrCorePath))
				{
					throw new FileNotFoundException("UsrCore.json not found", usrCorePath);
				}

				string usrCoreJson = await File.ReadAllTextAsync(usrCorePath);
				var users = JsonSerializer.Deserialize<List<UserMigrationData>>(usrCoreJson);

				Directory.CreateDirectory(_newBasePath);
				await InitializeNewDatabaseAsync();

				for (int i = 0; i < users.Count; i++)
				{
					await MigrateUserAsync(users[i]);
				}

				// Zip the old folder, delete it, and keep the zip file
				await ZipAndDeleteOldFolderAsync();

				_logger.LogInformation("Full migration process completed");
			}
			finally
			{
				
				_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
			}
		}


		private async Task ZipAndDeleteOldFolderAsync()
		{
			try
			{
				_logger.LogInformation("Starting to zip the old folder");

				string oldFolderName = "FireBrowserUserCore";
				string newFolderName = "GraphiteOld";
				string parentDir = Path.GetDirectoryName(_oldBasePath);
				string newPath = Path.Combine(parentDir, newFolderName);
				string zipPath = Path.Combine(parentDir, $"{newFolderName}.zip");

				// Rename the folder
				if (Directory.Exists(newPath))
				{
					Directory.Delete(newPath, true);
				}
				Directory.Move(_oldBasePath, newPath);
				_logger.LogInformation($"Renamed '{oldFolderName}' to '{newFolderName}'");

				// Create a zip file of the renamed folder
				ZipFile.CreateFromDirectory(newPath, zipPath);
				_logger.LogInformation($"Old folder zipped successfully to: {zipPath}");

				// Delete the renamed folder
				Directory.Delete(newPath, true);
				_logger.LogInformation($"'{newFolderName}' folder deleted successfully");

				await Task.CompletedTask; // Since ZipFile.CreateFromDirectory is synchronous
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error during zipping and deleting old folder: {ex.Message}");
				throw;
			}
		}

		private async Task InitializeNewDatabaseAsync()
		{
			using var connection = new SqliteConnection($"Data Source={_newDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Username TEXT PRIMARY KEY,
                    PasswordHash TEXT,
                    Email TEXT,
                    WindowsUserName TEXT,
                    IsFirstLaunch INTEGER,
                    ProfileImagePath TEXT,
                    HasPassword INTEGER
                )";

			await command.ExecuteNonQueryAsync();
		}

		private async Task MigrateUserAsync(UserMigrationData user)
		{
			_logger.LogInformation($"Migrating user: {user.Username}");

			string oldUserPath = Path.Combine(_oldBasePath, "Users", user.Username);
			string newUserPath = Path.Combine(_newBasePath, user.Username);

			Directory.CreateDirectory(newUserPath);

			string newProfileImagePath = await MigrateProfileImageAsync(oldUserPath, newUserPath, user.Username);
			user.ProfileImagePath = newProfileImagePath;

			await MigrateUserToDatabaseAsync(user);
			await MigrateUserFilesAsync(oldUserPath, newUserPath);

			// Apply fixes to the migrated settings.db
			await ApplySettingsDbFixesAsync(user.Username);

			_logger.LogInformation($"Migration completed for user: {user.Username}");
		}

		private async Task<string> MigrateProfileImageAsync(string oldUserPath, string newUserPath, string username)
		{
			string oldProfileImagePath = Path.Combine(oldUserPath, "profile_image.jpg");
			string newProfileImagePath = Path.Combine(newUserPath, "profile_image.jpg");

			if (File.Exists(oldProfileImagePath))
			{
				_logger.LogInformation($"Migrating profile image for user: {username}");
				File.Copy(oldProfileImagePath, newProfileImagePath, true);
				return newProfileImagePath;
			}
			else
			{
				_logger.LogWarning($"Profile image not found for user: {username}");
				return null;
			}
		}

		private async Task MigrateUserToDatabaseAsync(UserMigrationData user)
		{
			using var connection = new SqliteConnection($"Data Source={_newDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = @"
                INSERT OR REPLACE INTO Users (
                    Username, 
                    PasswordHash, 
                    Email, 
                    WindowsUserName, 
                    IsFirstLaunch, 
                    ProfileImagePath, 
                    HasPassword
                ) VALUES (
                    $username, 
                    $passwordHash, 
                    $email, 
                    $windowsUserName, 
                    $isFirstLaunch, 
                    $profileImagePath, 
                    $hasPassword
                )";

			command.Parameters.AddWithValue("$username", user.Username);
			command.Parameters.AddWithValue("$passwordHash", user.PasswordHash ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$email", user.Email ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$windowsUserName", user.WindowsUserName ?? Environment.UserName);
			command.Parameters.AddWithValue("$isFirstLaunch", user.IsFirstLaunch ? 1 : 0);
			command.Parameters.AddWithValue("$profileImagePath", user.ProfileImagePath ?? (object)DBNull.Value);
			command.Parameters.AddWithValue("$hasPassword", user.HasPassword ? 1 : 0);

			await command.ExecuteNonQueryAsync();
		}

		private async Task MigrateUserFilesAsync(string oldUserPath, string newUserPath)
		{
			// Migrate specific folders and databases
			string[] foldersToMigrate = { "Browser", "Database", "Permissions", "Settings" };
			string[] databasesToMigrate = { "History.db", "Favorites.db", "Downloads.db" };

			foreach (var folder in foldersToMigrate)
			{
				string oldFolderPath = Path.Combine(oldUserPath, folder);
				string newFolderPath = Path.Combine(newUserPath, folder);

				if (Directory.Exists(oldFolderPath))
				{
					_logger.LogInformation($"Migrating folder: {folder}");
					Directory.CreateDirectory(newFolderPath);
					await CopyDirectoryAsync(oldFolderPath, newFolderPath);
				}
			}

			foreach (var dbName in databasesToMigrate)
			{
				string oldDbPath = Path.Combine(oldUserPath, "Database", dbName);
				string newDbPath = Path.Combine(newUserPath, "Database", dbName);

				if (File.Exists(oldDbPath))
				{
					_logger.LogInformation($"Migrating database: {dbName}");
					File.Copy(oldDbPath, newDbPath, true);
				}
			}
		}

		private async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
		{
			foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
			{
				Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
			}

			foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
			{
				string newFilePath = filePath.Replace(sourceDir, destinationDir);
				File.Copy(filePath, newFilePath, true);
			}

			await Task.CompletedTask; // This method doesn't have async operations, but we keep it async for consistency
		}

		private async Task ApplySettingsDbFixesAsync(string username)
		{
			_logger.LogInformation($"Applying fixes to settings.db for user: {username}");

			string settingsDbPath = Path.Combine(_newBasePath, username, "Settings", "Settings.db");

			if (!File.Exists(settingsDbPath))
			{
				_logger.LogWarning($"settings.db not found for user: {username}. Creating a new one.");
				Directory.CreateDirectory(Path.GetDirectoryName(settingsDbPath));
			}

			using var connection = new SqliteConnection($"Data Source={settingsDbPath}");
			await connection.OpenAsync();

			try
			{
				// 1. Ensure the Settings table has the correct structure
				await EnsureSettingsTableStructureAsync(connection);

				// 2. Update or add new default settings
				await UpdateDefaultSettingsAsync(connection);

				// 3. Remove any obsolete settings
				await RemoveObsoleteSettingsAsync(connection);

				// 4. Perform any necessary data type conversions
				await PerformDataTypeConversionsAsync(connection);

				_logger.LogInformation($"Successfully applied fixes to settings.db for user: {username}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error applying fixes to settings.db for user {username}: {ex.Message}");
			}
			finally
			{
				await connection.CloseAsync();
			}
		}

		private async Task EnsureSettingsTableStructureAsync(SqliteConnection connection)
		{
			var command = connection.CreateCommand();

			// First, check if the old table exists and drop it
			command.CommandText = "DROP TABLE IF EXISTS OldSettingsTable";
			await command.ExecuteNonQueryAsync();

			// Now, create or recreate the Settings table
			command.CommandText = @"
                DROP TABLE IF EXISTS Settings;
                CREATE TABLE Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Version INTEGER DEFAULT 1
                );
                
                CREATE INDEX IF NOT EXISTS idx_settings_key ON Settings(Key);
                CREATE INDEX IF NOT EXISTS idx_settings_modified ON Settings(LastModified);";

			await command.ExecuteNonQueryAsync();

			// Verify if the table structure is correct
			command.CommandText = "PRAGMA table_info(Settings)";
			using var reader = await command.ExecuteReaderAsync();
			var columns = new List<string>();
			while (await reader.ReadAsync())
			{
				columns.Add(reader.GetString(1)); // Column name is at index 1
			}

			if (!columns.Contains("Key") || !columns.Contains("Value") || !columns.Contains("Type") ||
				!columns.Contains("LastModified") || !columns.Contains("Version"))
			{
				throw new Exception("Failed to create the Settings table with the correct structure.");
			}
		}

		private async Task UpdateDefaultSettingsAsync(SqliteConnection connection)
		{
			var defaultSettings = SettingsManager.GetDefaultSettings();

			foreach (var setting in defaultSettings)
			{
				var command = connection.CreateCommand();
				command.CommandText = @"
                    INSERT OR REPLACE INTO Settings (Key, Value, Type, LastModified, Version)
                    VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                        COALESCE((SELECT Version + 1 FROM Settings WHERE Key = $key), 1))";

				command.Parameters.AddWithValue("$key", setting.Key);
				command.Parameters.AddWithValue("$value", setting.Value?.ToString() ?? "");
				command.Parameters.AddWithValue("$type", setting.Value?.GetType().FullName ?? "");

				await command.ExecuteNonQueryAsync();
			}
		}

		private async Task RemoveObsoleteSettingsAsync(SqliteConnection connection)
		{
			var obsoleteSettings = new[] { "OldSetting1", "OldSetting2" }; // Add any obsolete setting keys here

			foreach (var setting in obsoleteSettings)
			{
				var command = connection.CreateCommand();
				command.CommandText = "DELETE FROM Settings WHERE Key = $key";
				command.Parameters.AddWithValue("$key", setting);
				await command.ExecuteNonQueryAsync();
			}
		}

		private async Task PerformDataTypeConversionsAsync(SqliteConnection connection)
		{
			// Example: Convert a setting from string to int
			var command = connection.CreateCommand();
			command.CommandText = @"
                UPDATE Settings 
                SET Value = CAST(Value AS INTEGER), 
                    Type = 'System.Int32'
                WHERE Key = 'SomeIntSetting' AND Type = 'System.String'";
			await command.ExecuteNonQueryAsync();

			// Add more conversions as needed
		}
	}

	public class UserMigrationData
	{
		public string Username { get; set; }
		public string PasswordHash { get; set; }
		public string Email { get; set; }
		public string WindowsUserName { get; set; }
		public bool IsFirstLaunch { get; set; }
		public string ProfileImagePath { get; set; }
		public bool HasPassword { get; set; }
	}

	public class FileLogger : ILogger<MigrationManager>
	{
		private readonly string _filePath;

		public FileLogger()
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string logDirectory = Path.Combine(localAppData);
			Directory.CreateDirectory(logDirectory);
			_filePath = Path.Combine(logDirectory, $"MigrationLog_{DateTime.Now:yyyyMMddHHmmss}.txt");
		}

		public IDisposable BeginScope<TState>(TState state) => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
			File.AppendAllText(_filePath, logMessage + Environment.NewLine);
		}
	}
}

