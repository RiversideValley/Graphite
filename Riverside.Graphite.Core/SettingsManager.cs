using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Riverside.Graphite.Core.Helper.Logging;

namespace Riverside.Graphite.Core
{
    public static class SettingsManager
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static readonly HashSet<string> EncryptionSettings = new HashSet<string>
        {
            "EncryptDatabase",
            "EncryptPermissions",
            "EncryptSettings"
        };

        public static string GetUserSettingsDbPath(string username)
        {
            return Path.Combine(UserManager.GraphiteDataPath, username, "Settings", "Settings.db");
        }

		public static async Task InitializeUserSettingsAsync(string username)
		{
			await _semaphore.WaitAsync();
			try
			{
				string dbPath = GetUserSettingsDbPath(username);
				Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

				// Initialize batteries with v2 configuration
				Batteries_V2.Init();

				using var connection = await GetConnectionAsync(username);
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
			finally
			{
				_semaphore.Release();
			}

		}


		private static async Task<SqliteConnection> GetConnectionAsync(string username)
        {
            string dbPath = GetUserSettingsDbPath(username);
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = true,
                DefaultTimeout = 30
            }.ToString();

            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static async Task CreateTablesAsync(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings_V2 (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Version INTEGER DEFAULT 1
                );
                
                CREATE INDEX IF NOT EXISTS idx_settings_key ON Settings_V2(Key);
                CREATE INDEX IF NOT EXISTS idx_settings_modified ON Settings_V2(LastModified);";

            await command.ExecuteNonQueryAsync();
        }

        private static async Task<T> ExecuteWithRetryAsync<T>(string username, Func<SqliteConnection, Task<T>> operation)
        {
            const int maxRetries = 3;
            var retryDelay = TimeSpan.FromMilliseconds(100);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var connection = await GetConnectionAsync(username);
                    return await operation(connection);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && attempt < maxRetries) // SQLite busy
                {
                    await Task.Delay(retryDelay * attempt);
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
                        INSERT OR REPLACE INTO Settings_V2 (Key, Value, Type, LastModified, Version)
                        VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                            COALESCE((SELECT Version + 1 FROM Settings_V2 WHERE Key = $key), 1))";

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
				finally { 
					
					connection.Close();	
				}
            });
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
                            INSERT OR REPLACE INTO Settings_V2 (Key, Value, Type, LastModified, Version)
                            VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                                COALESCE((SELECT Version + 1 FROM Settings_V2 WHERE Key = $key), 1))";

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
				finally
				{

					connection.Close();
				}
			});
        }

        public static async Task<T> GetSettingAsync<T>(string username, string key, T defaultValue = default)
        {
            return await ExecuteWithRetryAsync(username, async connection =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Value, Type FROM Settings_V2 WHERE Key = $key";
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
					finally
					{
						reader.Close();
						connection.Close();
					}
				}

                return defaultValue;
            });
        }

        public static async Task<Dictionary<string, object>> GetAllSettingsAsync(string username)
        {
            return await ExecuteWithRetryAsync(username, async connection =>
            {
				try
				{
					var settings = new Dictionary<string, object>();
					var command = connection.CreateCommand();
					command.CommandText = "SELECT Key, Value, Type FROM Settings_V2";

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
				}
				catch (Exception)
				{

					throw;
				}
				finally { connection.Close(); }	
			});
        }

        public static async Task UpdateEncryptionSettingAsync(string username, string key, bool value)
        {
            if (!EncryptionSettings.Contains(key))
            {
                throw new ArgumentException($"Invalid encryption setting: {key}");
            }

            await _semaphore.WaitAsync();
            try
            {
                await ExecuteWithRetryAsync(username, async connection =>
                {
                    using var transaction = await connection.BeginTransactionAsync();
					try
					{
						var command = connection.CreateCommand();
						command.CommandText = @"
                            INSERT OR REPLACE INTO Settings_V2 (Key, Value, Type, LastModified, Version)
                            VALUES ($key, $value, $type, CURRENT_TIMESTAMP, 
                                COALESCE((SELECT Version + 1 FROM Settings_V2 WHERE Key = $key), 1))";

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
							ExceptionLogger.LogException(ex);
							return false;
						}
						
						return true;
					}
					catch
					{
						await transaction.RollbackAsync();
						throw;
					}
					finally {
						connection.Close(); 
					}
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static async Task InitializeDefaultSettingsAsync(SqliteConnection connection)
        {
			var defaultSettings = new Settings(true).Self.ToDictionary(); // GetDefaultSettings();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                foreach (var setting in defaultSettings)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT OR IGNORE INTO Settings_V2 (Key, Value, Type)
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

    }
}