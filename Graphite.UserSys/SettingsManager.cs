using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Graphite.UserSys;

namespace Graphite.UserSys
{
	public static class SettingsManager
	{
		private static readonly HashSet<string> EncryptionSettings = new HashSet<string>
		{
			"EncryptDatabase",
			"EncryptPermissions",
			"EncryptSettings"
		};

		private static string GetUserSettingsDbPath(string username)
		{
			return Path.Combine(UserManager.GraphiteDataPath, username, "Settings", "Settings.db");
		}

		public static async Task InitializeUserSettingsAsync(string username)
		{
			string dbPath = GetUserSettingsDbPath(username);
			Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

			Batteries_V2.Init();

			using var connection = new SqliteConnection(GetConnectionString(username));
			await connection.OpenAsync();
			try
			{
				await CreateTablesAsync(connection);
				await InitializeDefaultSettingsAsync(connection);
			}
			finally
			{
				await connection.CloseAsync();
			}
		}

		private static string GetConnectionString(string username)
		{
			string dbPath = GetUserSettingsDbPath(username);
			return new SqliteConnectionStringBuilder
			{
				DataSource = dbPath,
				Mode = SqliteOpenMode.ReadWriteCreate,
				Cache = SqliteCacheMode.Shared,
				Pooling = true,
				DefaultTimeout = 30
			}.ToString();
		}

		private static async Task CreateTablesAsync(SqliteConnection connection)
		{
			var command = connection.CreateCommand();
			command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Version INTEGER DEFAULT 1
                );
                
                CREATE INDEX IF NOT EXISTS idx_settings_key ON Settings(Key);
                CREATE INDEX IF NOT EXISTS idx_settings_modified ON Settings(LastModified);";

			await command.ExecuteNonQueryAsync();
		}

		private static async Task<T> ExecuteWithRetryAsync<T>(string username, Func<SqliteConnection, Task<T>> operation)
		{
			const int maxRetries = 3;
			var retryDelay = TimeSpan.FromMilliseconds(100);

			for (int attempt = 1; attempt <= maxRetries; attempt++)
			{
				using var connection = new SqliteConnection(GetConnectionString(username));
				await connection.OpenAsync();
				try
				{
					return await operation(connection);
				}
				catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && attempt < maxRetries) // SQLite busy
				{
					await Task.Delay(retryDelay * attempt);
				}
				finally
				{
					await connection.CloseAsync();
				}
			}

			throw new TimeoutException("Failed to execute database operation after multiple retries");
		}

		public static async Task UpdateSettingAsync(string username, string key, object value)
		{
			if (EncryptionSettings.Contains(key))
			{
				throw new UnauthorizedAccessException("Encryption settings can only be changed through UpdateEncryptionSettingAsync");
			}

			await ExecuteWithRetryAsync(username, async connection =>
			{
				using var transaction = await connection.BeginTransactionAsync();
				try
				{
					var command = connection.CreateCommand();
					command.CommandText = @"
                        INSERT OR REPLACE INTO Settings (Key, Value, Type, LastModified, Version)
                        VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                            COALESCE((SELECT Version + 1 FROM Settings WHERE Key = $key), 1))";

					command.Parameters.AddWithValue("$key", key);
					command.Parameters.AddWithValue("$value", value?.ToString() ?? "");
					command.Parameters.AddWithValue("$type", value?.GetType().FullName ?? "");

					await command.ExecuteNonQueryAsync();
					await transaction.CommitAsync();

					return true;
				}
				catch
				{
					await transaction.RollbackAsync();
					throw;
				}
			});

			await NotifySettingChangedAsync(username, key, value);
		}

		public static async Task UpdateSettingsAsync(string username, Dictionary<string, object> settingsToUpdate)
		{
			foreach (var setting in settingsToUpdate)
			{
				if (EncryptionSettings.Contains(setting.Key))
				{
					throw new UnauthorizedAccessException($"Encryption setting '{setting.Key}' requires special handling");
				}
			}

			await ExecuteWithRetryAsync(username, async connection =>
			{
				using var transaction = await connection.BeginTransactionAsync();
				try
				{
					foreach (var setting in settingsToUpdate)
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

					await transaction.CommitAsync();
					return true;
				}
				catch
				{
					await transaction.RollbackAsync();
					throw;
				}
			});

			foreach (var setting in settingsToUpdate)
			{
				await NotifySettingChangedAsync(username, setting.Key, setting.Value);
			}
		}

		public static async Task<Dictionary<string, object>> GetAllSettingsAsync(string username)
		{
			return await ExecuteWithRetryAsync(username, async connection =>
			{
				var settings = new Dictionary<string, object>();
				var command = connection.CreateCommand();
				command.CommandText = "SELECT Key, Value, Type FROM Settings";

				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					try
					{
						string key = reader.GetString(0);
						string value = reader.GetString(1);
						string typeName = reader.GetString(2);

						var type = Type.GetType(typeName);
						if (type != null)
						{
							settings[key] = Convert.ChangeType(value, type);
						}
						else
						{
							settings[key] = value;
						}
					}
					catch
					{
						continue; // Skip invalid settings
					}
				}

				return settings;
			});
		}

		public static async Task<T> GetSettingAsync<T>(string username, string key, T defaultValue = default)
		{
			return await ExecuteWithRetryAsync(username, async connection =>
			{
				var command = connection.CreateCommand();
				command.CommandText = "SELECT Value, Type FROM Settings WHERE Key = $key";
				command.Parameters.AddWithValue("$key", key);

				using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					var value = reader.GetString(0);
					var type = reader.GetString(1);

					if (string.IsNullOrEmpty(value))
						return defaultValue;

					try
					{
						return (T)Convert.ChangeType(value, typeof(T));
					}
					catch
					{
						return defaultValue;
					}
				}

				return defaultValue;
			});
		}

		public static async Task UpdateEncryptionSettingAsync(string username, string key, bool value)
		{
			if (!EncryptionSettings.Contains(key))
			{
				throw new ArgumentException($"Invalid encryption setting: {key}");
			}

			await ExecuteWithRetryAsync(username, async connection =>
			{
				using var transaction = await connection.BeginTransactionAsync();
				try
				{
					var command = connection.CreateCommand();
					command.CommandText = @"
                        INSERT OR REPLACE INTO Settings (Key, Value, Type, LastModified, Version)
                        VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                            COALESCE((SELECT Version + 1 FROM Settings WHERE Key = $key), 1))";

					command.Parameters.AddWithValue("$key", key);
					command.Parameters.AddWithValue("$value", value.ToString());
					command.Parameters.AddWithValue("$type", typeof(bool).FullName);

					await command.ExecuteNonQueryAsync();
					await transaction.CommitAsync();

					try
					{
						await EncryptionManager.HandleEncryptionSettingChangeAsync(username, key, value);
					}
					catch (Exception ex)
					{
						return false;
					}

					return true;
				}
				catch
				{
					await transaction.RollbackAsync();
					throw;
				}
			});

			await NotifySettingChangedAsync(username, key, value);
		}

		private static async Task InitializeDefaultSettingsAsync(SqliteConnection connection)
		{
			var defaultSettings = GetDefaultSettings();
			using var transaction = await connection.BeginTransactionAsync();

			try
			{
				foreach (var setting in defaultSettings)
				{
					var command = connection.CreateCommand();
					command.CommandText = @"
                        INSERT OR IGNORE INTO Settings (Key, Value, Type)
                        VALUES ($key, $value, $type)";

					command.Parameters.AddWithValue("$key", setting.Key);
					command.Parameters.AddWithValue("$value", setting.Value?.ToString() ?? "");
					command.Parameters.AddWithValue("$type", setting.Value?.GetType().FullName ?? "");

					await command.ExecuteNonQueryAsync();
				}

				await transaction.CommitAsync();
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		public static Dictionary<string, object> GetDefaultSettings()
		{
			return new Dictionary<string, object>
			{
                 // Browser Settings
        { "PackageName", "FireBrowserWinUi3_" },
		{ "DisableJavaScript", false },
		{ "DisablePasswordSaving", false },
		{ "DisableWebMessaging", false },
		{ "DisableGeneralAutoFill", false },
		{ "EnableBrowserKeys", true },
		{ "EnableBrowserScripts", true },
		{ "UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36 Edg/132.0.0.0" },
		{ "EnableOperatingSystemIntegration", true },
		{ "DefaultSearchEngine", "Google" },
		{ "SearchUrl", "https://www.google.com/search?q=" },
		{ "EnablePictureInPictureMode", true },
		{ "TrackingPreventionLevel", 2 },
		{ "EnableResourceSaving", true },
		{ "EnableAutoSave", true },

        // Tab Management Settings
        { "TabGroupingEnabled", false },
		{ "TabPreloading", true },
		{ "TabSleepTime", "30" },  // Minutes before tab goes to sleep
        { "GenaralTabAutoRestore", true },
		{ "GenaralTabMaxNumber", "50" },

        // UI Settings
        { "BackgroundColor", "#000000" },
		{ "ToolbarColor", "#000000" },
		{ "TabViewColor", "#000000" },
		{ "NewTabPageTextColor", "#FFFFFF" },
		{ "BackgroundType", 0 },
		{ "UseLightMode", false },
		{ "BackdropStyle", "Mica" },
		{ "FontSize", 12 },
		{ "DisplayLanguage", "en-US" },

        // UI Visibility Settings
        { "ShowStatusBar", true },
		{ "ShowReadButton", true },
		{ "ShowAdBlockButton", true },
		{ "ShowDownloadsButton", true },
		{ "ShowTranslateButton", true },
		{ "ShowFavoritesButton", true },
		{ "ShowHistoryButton", true },
		{ "ShowQRCodeButton", true },
		{ "ShowFavoritesList", true },
		{ "ShowToolbarIcons", true },
		{ "ShowDarkModeIcon", true },
		{ "ShowDateTimeOnNewTab", true },
		{ "ShowExitDialog", false },
		{ "ShowConfirmCloseDialog", false },
		{ "ShowBackButton", true },
		{ "ShowForwardButton", true },
		{ "ShowRefreshButton", true },
		{ "ShowHomeButton", true },
		{ "ShowBrowserLogo", true },
		{ "ShowWelcomeMessage", true },

        // Panel States
        { "IsHistoryPanelToggled", false },
		{ "IsFavoritesPanelToggled", false },
		{ "IsSearchBoxToggled", false },
		{ "IsFavoritesPanelVisible", true },
		{ "IsHistoryPanelVisible", true },
		{ "IsSearchBarVisible", true },

        // New Tab Settings
        { "ShowNewTabPageCore", true },
		{ "ShowTrendingContent", true },
		{ "ShowDownloadsOnNewTab", false },
		{ "ShowFavoritesOnNewTab", false },
		{ "ShowHistoryOnNewTab", false },
		{ "ShowQuickLinksOnNewTab", false },
		{ "ShowNewTabSelectorBar", true },
		{ "OpenNewTabsInBackground", false },

        // Security Settings
        { "Enable2FA", true },
		{ "EncryptFavorites", false },
		{ "EncryptHistory", false },
		{ "EncryptSettings", false },
		{ "EncryptDatabase", false },
		{ "EncryptPermissions", false },

        // Ad Blocker Settings
        { "EnableAdBlocker", true },
		{ "AdBlockerType", 0 },

        // Sync Settings
        { "SyncBookmarks", true },
		{ "SyncHistory", false },

        // Misc Settings
        { "DefaultDownloadPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") },
		{ "ExceptionLoggingLevel", "Low" },
		{ "EnableAutoMode", false },
		{ "UserGender", "Unspecified" }
			};
		}

		// Event for notifying about setting changes
		public static event EventHandler<SettingChangedEventArgs> SettingChanged;

		private static Task NotifySettingChangedAsync(string username, string key, object value)
		{
			var handler = SettingChanged;
			if (handler != null)
			{
				var args = new SettingChangedEventArgs(username, key, value);
				return Task.Run(() => handler(null, args));
			}
			return Task.CompletedTask;
		}
	}

	public class SettingChangedEventArgs : EventArgs
	{
		public string Username { get; }
		public string Key { get; }
		public object Value { get; }

		public SettingChangedEventArgs(string username, string key, object value)
		{
			Username = username;
			Key = key;
			Value = value;
		}
	}
}