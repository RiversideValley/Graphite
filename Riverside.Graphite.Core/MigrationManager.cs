using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Riverside.Graphite.Core.Helper.Logging;
using Riverside.Graphite.Core.Models;
using Windows.Storage;

namespace Riverside.Graphite.Core
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
			_newBasePath = UserManager.GraphiteDataPath; // Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");
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

				_logger.LogInformation("Full migration process completed");
			}
			catch(Exception ex) 
			{
				ExceptionLogger.LogException(ex);
			}
			finally
			{
				_ = Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
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

			try
			{
				Directory.CreateDirectory(newUserPath);

				string newProfileImagePath = await MigrateProfileImageAsync(oldUserPath, newUserPath, user.Username);
				user.ProfileImagePath = newProfileImagePath;

				await MigrateUserToDatabaseAsync(user);
					// Apply fixes to the migrated settings.db
				await ApplySettingsDbFixesAsync(user.Username);

				_logger.LogInformation($"Migration completed for user: {user.Username}");
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
				throw;
			}
			
		}

		private  async Task<string> MigrateProfileImageAsync(string oldUserPath, string newUserPath, string username)
		{
			string oldProfileImagePath = Path.Combine(oldUserPath, "profile_image.jpg");
			string newProfileImagePath = Path.Combine(newUserPath, "profile_image.jpg");
			try
			{
				try
				{

					if (File.Exists(oldProfileImagePath))
					{
						var imageFile = await StorageFile.GetFileFromPathAsync(oldProfileImagePath);
						var memoryStream = new MemoryStream();	

						using (var imageStream = await imageFile.OpenReadAsync())
						{
							await imageStream.AsStreamForRead().CopyToAsync(memoryStream);
							memoryStream.Seek(0, SeekOrigin.Begin);
						}
						
						File.WriteAllBytes(newProfileImagePath, memoryStream.ToArray());
						memoryStream.Dispose();

						return newProfileImagePath;
						
					}
					else
					{
						_logger.LogWarning($"Profile image not found for user: {username}");
						return null;
					}
				}
				catch (Exception)
				{
					throw; 					
				}
			}
			catch (Exception ex)
			{
				ExceptionLogger.LogException(ex);
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
			connection.Close(); 
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
				// 1. Setup new version2 settings table for user
				await SettingsManager.InitializeUserSettingsAsync(username); 
				// 2. Update or add new default settings
				await UpdateDefaultSettingsAsync(connection, username);

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

        private async Task UpdateDefaultSettingsAsync(SqliteConnection connection, string username = null)
        {
            var defaultSettings = new Settings(true).Self; // SettingsManager.GetDefaultSettings();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM Settings;";
                using (var reader = await command.ExecuteReaderAsync())
                {
					if (reader.HasRows)
					{
						while (await reader.ReadAsync())
						{
							for (var i = 0; i < (reader.FieldCount -1); i++)
							{
								var columnName = reader.GetName(i);
								var columnValue = reader.GetValue(i)?.ToString();

                                var property = defaultSettings.GetType().GetProperty(columnName);
                                if (property != null && property.CanWrite)
                                {
                                    object convertedValue;
                                    if (property.PropertyType == typeof(bool))
                                    {
                                        convertedValue = columnValue == "1";
                                    }
                                    else
                                    {
                                        convertedValue = Convert.ChangeType(columnValue, property.PropertyType);
                                    }
                                    property.SetValue(defaultSettings, convertedValue);
                                }
							}
						}
						if (username is not null)
							await SettingsManager.UpdateSettingsAsync(username, defaultSettings.ToDictionary());

						return; 
					}
                    
                }
            }

            // if user doesn't have existing settings add them.
            foreach (var property in defaultSettings.GetType().GetProperties())
            {
                if (property.CanRead)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT OR REPLACE INTO Settings (Key, Value, Type, LastModified, Version)
                        VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                            COALESCE((SELECT Version + 1 FROM Settings WHERE Key = $key), 1))";

                    command.Parameters.AddWithValue("$key", property.Name);
                    command.Parameters.AddWithValue("$value", property.GetValue(defaultSettings)?.ToString() ?? "");
                    command.Parameters.AddWithValue("$type", property.PropertyType.FullName ?? "");

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

		
	}

}

