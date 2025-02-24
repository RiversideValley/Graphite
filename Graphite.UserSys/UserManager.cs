using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Text;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Security;
using System.Text.Json;
using System.Linq;

namespace Graphite.UserSys
{
	public static class UserManager
	{
		private static readonly string KeyFileName = "graphite.key";
		private static readonly string IVFileName = "graphite.iv";

		private static string KeyFilePath => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"Graphite",
			"Security",
			KeyFileName);
		private static string IVFilePath => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"Graphite",
			"Security",
			IVFileName);

		private static byte[] _cachedKey;
		private static byte[] _cachedIV;

		public static readonly string GraphiteDataPath = Path.Combine(
	   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
	   @"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");

		private const string MainDbName = "UserCore.db";
		private static readonly string MainDbPath = Path.Combine(GraphiteDataPath, MainDbName);

		private static readonly string SecurityDbPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"Graphite",
			"Security",
			"UserSecurity.db");

		private static readonly ConcurrentDictionary<string, (int Attempts, DateTime LastAttempt)> _loginAttempts =
			new ConcurrentDictionary<string, (int Attempts, DateTime LastAttempt)>();
		private const int MaxLoginAttempts = 5;
		private const int LockoutMinutes = 15;

		private static readonly ConcurrentDictionary<string, (string Username, DateTime ExpirationTime)> _activeSessions =
			new ConcurrentDictionary<string, (string Username, DateTime ExpirationTime)>();
		private const int SessionExpirationMinutes = 30;

		public static async Task InitializeAsync()
		{
			try
			{
				await EnsureEncryptionKeysAsync();
				Directory.CreateDirectory(GraphiteDataPath);

				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
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
                    );
                    CREATE TABLE IF NOT EXISTS Profiles (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT,
                        ProfileName TEXT,
                        IsActive INTEGER DEFAULT 0,
                        UNIQUE(Username, ProfileName)
                    );
                    CREATE TABLE IF NOT EXISTS blobs (
                        Username TEXT PRIMARY KEY,
                        Metadata TEXT,
                        FOREIGN KEY(Username) REFERENCES Users(Username)
                    );";
				await command.ExecuteNonQueryAsync();

				await EnsureUserSecurityTableAsync();
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to initialize application.", ex);
			}
		}

		private static async Task EnsureUserSecurityTableAsync()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(SecurityDbPath));

			await using var connection = new SqliteConnection($"Data Source={SecurityDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = @"
                CREATE TABLE IF NOT EXISTS UserSecurity (
                    Username TEXT PRIMARY KEY,
                    Hash TEXT,
                    Salt TEXT
                )";
			await command.ExecuteNonQueryAsync();
		}

		private static async Task EnsureEncryptionKeysAsync()
		{
			if (_cachedKey != null && _cachedIV != null)
			{
				return;
			}

			try
			{
				string securityFolder = Path.GetDirectoryName(KeyFilePath);

				if (!Directory.Exists(securityFolder))
				{
					Directory.CreateDirectory(securityFolder);
				}

				if (File.Exists(KeyFilePath) && File.Exists(IVFilePath))
				{
					try
					{
						_cachedKey = File.ReadAllBytes(KeyFilePath);
						_cachedIV = File.ReadAllBytes(IVFilePath);

						if (_cachedKey.Length != 32 || _cachedIV.Length != 16)
						{
							throw new Exception("Invalid key or IV length detected.");
						}
					}
					catch
					{
						_cachedKey = null;
						_cachedIV = null;

						if (File.Exists(KeyFilePath)) File.Delete(KeyFilePath);
						if (File.Exists(IVFilePath)) File.Delete(IVFilePath);
					}
				}

				if (_cachedKey == null || _cachedIV == null)
				{
					using (var aes = Aes.Create())
					{
						aes.KeySize = 256;
						aes.GenerateKey();
						aes.GenerateIV();

						_cachedKey = aes.Key;
						_cachedIV = aes.IV;

						File.WriteAllBytes(KeyFilePath, _cachedKey);
						File.WriteAllBytes(IVFilePath, _cachedIV);

						try
						{
							StorageFile keyFile = await StorageFile.GetFileFromPathAsync(KeyFilePath);
							StorageFile ivFile = await StorageFile.GetFileFromPathAsync(IVFilePath);

							await keyFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });
							await ivFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });

							var keyProps = await keyFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });
							var ivProps = await ivFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });

							uint keyAttributes = (uint)keyProps["System.FileAttributes"];
							uint ivAttributes = (uint)ivProps["System.FileAttributes"];

							keyAttributes |= 0x2;
							ivAttributes |= 0x2;

							await keyFile.Properties.SavePropertiesAsync(new Dictionary<string, object> { { "System.FileAttributes", keyAttributes } });
							await ivFile.Properties.SavePropertiesAsync(new Dictionary<string, object> { { "System.FileAttributes", ivAttributes } });
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine($"Failed to set file attributes: {ex.Message}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to initialize encryption keys.", ex);
			}
		}

		private static string EncryptPassword(string password)
		{
			if (string.IsNullOrEmpty(password))
				return null;

			try
			{
				EnsureEncryptionKeysAsync().GetAwaiter().GetResult();

				using (Aes aes = Aes.Create())
				{
					aes.Key = _cachedKey;
					aes.IV = _cachedIV;
					aes.Padding = PaddingMode.PKCS7;

					ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

					using (MemoryStream msEncrypt = new MemoryStream())
					{
						using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
						{
							swEncrypt.Write(password);
						}

						return Convert.ToBase64String(msEncrypt.ToArray());
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to encrypt password.", ex);
			}
		}

		private static string DecryptPassword(string encryptedPassword)
		{
			if (string.IsNullOrEmpty(encryptedPassword))
				return null;

			try
			{
				EnsureEncryptionKeysAsync().GetAwaiter().GetResult();

				using (Aes aes = Aes.Create())
				{
					aes.Key = _cachedKey;
					aes.IV = _cachedIV;
					aes.Padding = PaddingMode.PKCS7;

					ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

					byte[] cipherBytes = Convert.FromBase64String(encryptedPassword);

					using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					using (StreamReader srDecrypt = new StreamReader(csDecrypt))
					{
						return srDecrypt.ReadToEnd();
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to decrypt password. The stored password may be corrupted or the encryption keys have changed.", ex);
			}
		}

		public static async Task<List<User>> GetAllUsersAsync()
		{
			var users = new List<User>();

			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = "SELECT Username, Email, ProfileImagePath, HasPassword FROM Users";

				await using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					users.Add(new User
					{
						Username = reader.GetString(0),
						Email = reader.IsDBNull(1) ? null : reader.GetString(1),
						ProfileImagePath = reader.GetString(2),
						HasPassword = reader.GetInt32(3) == 1,
						WindowsUserName = Environment.UserName,
						IsFirstLaunch = false
					});
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to retrieve users.", ex);
			}

			return users;
		}

		public static async Task<User> GetUserAsync(string username)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = "SELECT Username, Email, ProfileImagePath, HasPassword, WindowsUserName, IsFirstLaunch FROM Users WHERE Username = $username";
				command.Parameters.AddWithValue("$username", username);

				await using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					return new User
					{
						Username = reader.GetString(0),
						Email = reader.IsDBNull(1) ? null : reader.GetString(1),
						ProfileImagePath = reader.GetString(2),
						HasPassword = reader.GetInt32(3) == 1,
						WindowsUserName = reader.GetString(4),
						IsFirstLaunch = reader.GetInt32(5) == 1
					};
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to retrieve user: {username}", ex);
			}

			return null;
		}

		public static async Task<User> CreateUserAsync(string username, string password = null, string email = null, Stream profileImageStream = null)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(username))
				{
					throw new ArgumentException("Username is required.", nameof(username));
				}

				string sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));

				var user = new User
				{
					Username = username,
					Email = email,
					WindowsUserName = Environment.UserName,
					IsFirstLaunch = true,
					HasPassword = !string.IsNullOrEmpty(password)
				};

				string encryptedPassword = null;
				string passwordHash = null;
				string passwordSalt = null;
				if (!string.IsNullOrEmpty(password))
				{
					(passwordHash, passwordSalt) = HashPassword(password);
					encryptedPassword = EncryptPassword(password);

					await StoreSecurityInfoAsync(username, passwordHash, passwordSalt);
				}

				string userFolderPath = Path.Combine(GraphiteDataPath, sanitizedUsername);
				if (Directory.Exists(userFolderPath))
				{
					throw new Exception($"User folder already exists for username: {username}");
				}

				Directory.CreateDirectory(userFolderPath);
				foreach (var folder in new[] { "Browser", "Database", "Permissions", "Settings" })
				{
					Directory.CreateDirectory(Path.Combine(userFolderPath, folder));
				}

				string profileImagePath = Path.Combine(userFolderPath, "profile_image.jpg");
				user.ProfileImagePath = profileImagePath;
				if (profileImageStream != null)
				{
					try
					{
						using (var fileStream = File.Create(profileImagePath))
						{
							await profileImageStream.CopyToAsync(fileStream);
						}
					}
					catch (Exception ex)
					{
						throw new Exception($"Failed to process profile image: {ex.Message}", ex);
					}
				}
				else
				{
					try
					{
						string defaultImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "default_profile.jpg");
						if (File.Exists(defaultImagePath))
						{
							File.Copy(defaultImagePath, profileImagePath, true);
						}
						else
						{
							File.WriteAllBytes(profileImagePath, new byte[0]);
						}
					}
					catch (Exception ex)
					{
						throw new Exception($"Failed to create default profile image: {ex.Message}", ex);
					}
				}

				try
				{
					await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
					await connection.OpenAsync();

					using var transaction = await connection.BeginTransactionAsync();

					try
					{
						var command = connection.CreateCommand();
						command.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, Email, WindowsUserName, IsFirstLaunch, ProfileImagePath, HasPassword)
                VALUES ($username, $passwordHash, $email, $windowsUserName, $isFirstLaunch, $profileImagePath, $hasPassword)";

						command.Parameters.AddWithValue("$username", user.Username);
						command.Parameters.AddWithValue("$passwordHash", (object)encryptedPassword ?? DBNull.Value);
						command.Parameters.AddWithValue("$email", (object)user.Email ?? DBNull.Value);
						command.Parameters.AddWithValue("$windowsUserName", user.WindowsUserName);
						command.Parameters.AddWithValue("$isFirstLaunch", user.IsFirstLaunch ? 1 : 0);
						command.Parameters.AddWithValue("$profileImagePath", user.ProfileImagePath);
						command.Parameters.AddWithValue("$hasPassword", user.HasPassword ? 1 : 0);

						await command.ExecuteNonQueryAsync();

						var metadataCommand = connection.CreateCommand();
						metadataCommand.CommandText = @"
                INSERT INTO blobs (Username, Metadata)
                VALUES ($username, $metadata)";
						metadataCommand.Parameters.AddWithValue("$username", user.Username);
						metadataCommand.Parameters.AddWithValue("$metadata", JsonSerializer.Serialize(new
						{
							CreationDate = DateTime.UtcNow,
							LastLogin = DateTime.UtcNow,
							HasPassword = user.HasPassword
						}));
						await metadataCommand.ExecuteNonQueryAsync();

						await transaction.CommitAsync();

						await SettingsManager.InitializeUserSettingsAsync(username);

						return user;
					}
					catch
					{
						await transaction.RollbackAsync();
						throw;
					}
				}
				catch (Exception ex)
				{
					try
					{
						if (Directory.Exists(userFolderPath))
						{
							Directory.Delete(userFolderPath, true);
						}
					}
					catch
					{
						// Ignore cleanup errors
					}
					throw new Exception($"Failed to create user in database: {ex.Message}", ex);
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to create user: {ex.Message}", ex);
			}
		}

		private static (string Hash, string Salt) HashPassword(string password)
		{
			byte[] salt = new byte[16];
			using (var rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(salt);
			}

			using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
			{
				byte[] hash = pbkdf2.GetBytes(20);
				return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
			}
		}

		public static async Task<User> AuthenticateAsync(string username, string password = null)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				throw new ArgumentException("Username cannot be empty", nameof(username));
			}

			username = username.Trim();

			if (IsAccountLocked(username))
			{
				throw new SecurityException($"Account is locked. Please try again after {LockoutMinutes} minutes.");
			}

			try
			{
				await SettingsManager.InitializeUserSettingsAsync(username);
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = "SELECT * FROM Users WHERE Username = $username";
				command.Parameters.AddWithValue("$username", username);

				await using var reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					bool hasPassword = reader.GetInt32(reader.GetOrdinal("HasPassword")) == 1;

					if (hasPassword)
					{
						if (string.IsNullOrEmpty(password))
						{
							IncrementLoginAttempts(username);
							return null;
						}

						var securityInfo = await GetSecurityInfoAsync(username);
						if (!securityInfo.HasValue)
						{
							throw new SecurityException("Security information is missing. Please contact support.");
						}

						var (storedHash, storedSalt) = securityInfo.Value;
						if (!VerifyPassword(password, storedHash, storedSalt))
						{
							IncrementLoginAttempts(username);
							return null;
						}
					}
					else if (password != null)
					{
						IncrementLoginAttempts(username);
						return null;
					}

					ResetLoginAttempts(username);

					var user = new User
					{
						Username = username,
						Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
						WindowsUserName = reader.GetString(reader.GetOrdinal("WindowsUserName")),
						IsFirstLaunch = reader.GetInt32(reader.GetOrdinal("IsFirstLaunch")) == 1,
						ProfileImagePath = reader.GetString(reader.GetOrdinal("ProfileImagePath")),
						HasPassword = hasPassword
					};

					user.Profiles = await GetUserProfilesAsync(username);
					user.SessionId = GenerateSessionId(username);

					return user;
				}

				IncrementLoginAttempts(username);
				return null;
			}
			catch (SecurityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new Exception($"Authentication failed: {ex.Message}", ex);
			}
		}

		private static bool IsAccountLocked(string username)
		{
			if (_loginAttempts.TryGetValue(username, out var attempts))
			{
				if (attempts.Attempts >= MaxLoginAttempts)
				{
					if (DateTime.UtcNow - attempts.LastAttempt < TimeSpan.FromMinutes(LockoutMinutes))
					{
						return true;
					}
					else
					{
						ResetLoginAttempts(username);
					}
				}
			}
			return false;
		}

		private static void IncrementLoginAttempts(string username)
		{
			_loginAttempts.AddOrUpdate(
				username,
				(key) => (1, DateTime.UtcNow),
				(key, existing) => (existing.Attempts + 1, DateTime.UtcNow)
			);
		}

		private static void ResetLoginAttempts(string username)
		{
			_loginAttempts.TryRemove(username, out _);
		}

		private static string GenerateSessionId(string username)
		{
			string sessionId = Guid.NewGuid().ToString();
			_activeSessions[sessionId] = (username, DateTime.UtcNow.AddMinutes(SessionExpirationMinutes));
			return sessionId;
		}

		public static bool ValidateSession(string sessionId)
		{
			if (_activeSessions.TryGetValue(sessionId, out var sessionInfo))
			{
				if (DateTime.UtcNow < sessionInfo.ExpirationTime)
				{
					_activeSessions[sessionId] = (sessionInfo.Username, DateTime.UtcNow.AddMinutes(SessionExpirationMinutes));
					return true;
				}
				else
				{
					_activeSessions.TryRemove(sessionId, out _);
				}
			}
			return false;
		}

		public static void EndSession(string sessionId)
		{
			_activeSessions.TryRemove(sessionId, out _);
		}

		public static async Task<bool> DeleteUserAsync(string username, string password = null)
		{
			try
			{
				using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();

				try
				{
					var checkCommand = connection.CreateCommand();
					checkCommand.CommandText = @"
                SELECT HasPassword, PasswordHash 
                FROM Users 
                WHERE Username = $username";
					checkCommand.Parameters.AddWithValue("$username", username);

					using var reader = await checkCommand.ExecuteReaderAsync();
					if (!await reader.ReadAsync())
					{
						return false;
					}

					bool hasPassword = reader.GetBoolean(0);
					string storedEncryptedPassword = reader.IsDBNull(1) ? null : reader.GetString(1);

					if (hasPassword)
					{
						if (string.IsNullOrEmpty(password))
						{
							throw new UnauthorizedAccessException("Password required for user deletion.");
						}

						var securityInfo = await GetSecurityInfoAsync(username);
						if (securityInfo.HasValue)
						{
							var (storedHash, storedSalt) = securityInfo.Value;
							if (!VerifyPassword(password, storedHash, storedSalt))
							{
								throw new UnauthorizedAccessException("Incorrect password provided for user deletion.");
							}
						}
						else
						{
							throw new UnauthorizedAccessException("Incorrect password provided for user deletion.");
						}
					}

					var deleteCommand = connection.CreateCommand();
					deleteCommand.CommandText = "DELETE FROM Users WHERE Username = $username";
					deleteCommand.Parameters.AddWithValue("$username", username);

					int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

					if (rowsAffected > 0)
					{
						await transaction.CommitAsync();

						string userFolderPath = Path.Combine(GraphiteDataPath, username);
						if (Directory.Exists(userFolderPath))
						{
							try
							{
								Directory.Delete(userFolderPath, true);
							}
							catch (IOException ex)
							{
								Console.WriteLine($"Warning: Failed to delete user folder: {ex.Message}");
							}
						}

						return true;
					}

					return false;
				}
				catch
				{
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to delete user: {ex.Message}", ex);
			}
		}

		public static async Task SetPasswordAsync(string username, string newPassword)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				string encryptedPassword = EncryptPassword(newPassword);

				var command = connection.CreateCommand();
				command.CommandText = @"
                    UPDATE Users 
                    SET PasswordHash = $passwordHash, HasPassword = 1
                    WHERE Username = $username";
				command.Parameters.AddWithValue("$passwordHash", encryptedPassword);
				command.Parameters.AddWithValue("$username", username);

				await command.ExecuteNonQueryAsync();

				var (hash, salt) = HashPassword(newPassword);
				await StoreSecurityInfoAsync(username, hash, salt);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to set password: {ex.Message}", ex);
			}
		}

		public static async Task RemovePasswordAsync(string username)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = @"
                    UPDATE Users 
                    SET PasswordHash = NULL, HasPassword = 0
                    WHERE Username = $username";
				command.Parameters.AddWithValue("$username", username);

				await command.ExecuteNonQueryAsync();

				await using var securityConnection = new SqliteConnection($"Data Source={SecurityDbPath}");
				await securityConnection.OpenAsync();

				var securityCommand = securityConnection.CreateCommand();
				securityCommand.CommandText = "DELETE FROM UserSecurity WHERE Username = $username";
				securityCommand.Parameters.AddWithValue("$username", username);

				await securityCommand.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to remove password: {ex.Message}", ex);
			}
		}

		public static async Task<User> LoginAsGuestAsync()
		{
			const string guestUsername = "Guest";
			const string guestEmail = "guest@app.com";

			try
			{
				var existingGuest = await GetUserAsync(guestUsername);
				if (existingGuest != null)
				{
					await ResetGuestDataAsync(guestUsername);
				}
				else
				{
					await CreateUserAsync(guestUsername, null, guestEmail, null);
				}

				await using (var connection = new SqliteConnection($"Data Source={MainDbPath}"))
				{
					await connection.OpenAsync();

					var command = connection.CreateCommand();
					command.CommandText = "UPDATE Users SET IsFirstLaunch = 0 WHERE Username = $username";
					command.Parameters.AddWithValue("$username", guestUsername);
					await command.ExecuteNonQueryAsync();
				}

				await CreateGuestFolderStructureAsync(guestUsername);

				return await AuthenticateAsync(guestUsername, null);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to login as Guest: {ex.Message}", ex);
			}
		}

		private static async Task ResetGuestDataAsync(string guestUsername)
		{
			string guestFolderPath = Path.Combine(GraphiteDataPath, guestUsername);

			try
			{
				if (Directory.Exists(guestFolderPath))
				{
					Directory.Delete(guestFolderPath, true);
				}

				await using (var connection = new SqliteConnection($"Data Source={MainDbPath}"))
				{
					await connection.OpenAsync();

					var command = connection.CreateCommand();
					command.CommandText = @"
                    UPDATE Users 
                    SET Email = $email, 
                        IsFirstLaunch = 0, 
                        ProfileImagePath = NULL
                    WHERE Username = $username";
					command.Parameters.AddWithValue("$email", "guest@app.com");
					command.Parameters.AddWithValue("$username", guestUsername);
					await command.ExecuteNonQueryAsync();
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to reset guest data: {ex.Message}", ex);
			}
		}

		private static async Task CreateGuestFolderStructureAsync(string guestUsername)
		{
			string guestFolderPath = Path.Combine(GraphiteDataPath, guestUsername);

			try
			{
				Directory.CreateDirectory(guestFolderPath);

				string[] subfolders = { "Browser", "Database", "Permissions", "Settings" };
				foreach (var folder in subfolders)
				{
					Directory.CreateDirectory(Path.Combine(guestFolderPath, folder));
				}

				await CreateEmptyFileAsync(Path.Combine(guestFolderPath, "Settings", "user_settings.json"));
				await CreateEmptyFileAsync(Path.Combine(guestFolderPath, "Permissions", "permissions.json"));

				await SettingsManager.InitializeUserSettingsAsync(guestUsername);

				string readmePath = Path.Combine(guestFolderPath, "Browser", "README.txt");
				await File.WriteAllTextAsync(readmePath, "This folder will store README.txt");
				await File.WriteAllTextAsync(readmePath, "This folder will store browser-related data for the guest user.");
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to create guest folder structure: {ex.Message}", ex);
			}
		}

		private static async Task CreateEmptyFileAsync(string filePath)
		{
			try
			{
				using (FileStream fs = File.Create(filePath))
				{
					await fs.FlushAsync();
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to create file {filePath}: {ex.Message}", ex);
			}
		}

		public static async Task UpdateEmailAsync(string username, string newEmail)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = @"
                    UPDATE Users 
                    SET Email = $email
                    WHERE Username = $username";
				command.Parameters.AddWithValue("$email", newEmail ?? (object)DBNull.Value);
				command.Parameters.AddWithValue("$username", username);

				await command.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to update email: {ex.Message}", ex);
			}
		}

		public static async Task<List<string>> GetUserProfilesAsync(string username)
		{
			await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = "SELECT ProfileName FROM Profiles WHERE Username = $username";
			command.Parameters.AddWithValue("$username", username);

			var profiles = new List<string>();
			await using var reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				profiles.Add(reader.GetString(0));
			}

			return profiles;
		}

		public static async Task<bool> AddProfileAsync(string username, string profileName)
		{
			await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = "INSERT OR IGNORE INTO Profiles (Username, ProfileName) VALUES ($username, $profileName)";
			command.Parameters.AddWithValue("$username", username);
			command.Parameters.AddWithValue("$profileName", profileName);

			int rowsAffected = await command.ExecuteNonQueryAsync();

			if (rowsAffected > 0)
			{
				string profileFolderPath = Path.Combine(GraphiteDataPath, username, profileName);
				Directory.CreateDirectory(profileFolderPath);
				foreach (var folder in new[] { "Browser", "Database", "Permissions", "Settings" })
				{
					Directory.CreateDirectory(Path.Combine(profileFolderPath, folder));
				}

				await SettingsManager.InitializeUserSettingsAsync(profileName);
			}

			return rowsAffected > 0;
		}

		public static async Task<bool> SwitchProfileAsync(string username, string profileName)
		{
			await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
			await connection.OpenAsync();

			var deactivateCommand = connection.CreateCommand();
			deactivateCommand.CommandText = "UPDATE Profiles SET IsActive = 0 WHERE Username = $username";
			deactivateCommand.Parameters.AddWithValue("$username", username);
			await deactivateCommand.ExecuteNonQueryAsync();

			var activateCommand = connection.CreateCommand();
			activateCommand.CommandText = "UPDATE Profiles SET IsActive = 1 WHERE Username = $username AND ProfileName = $profileName";
			activateCommand.Parameters.AddWithValue("$username", username);
			activateCommand.Parameters.AddWithValue("$profileName", profileName);

			int rowsAffected = await activateCommand.ExecuteNonQueryAsync();

			if (rowsAffected > 0)
			{
				await SettingsManager.GetAllSettingsAsync(profileName);
			}

			return rowsAffected > 0;
		}

		public static async Task<string> GetActiveProfileAsync(string username)
		{
			await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = "SELECT ProfileName FROM Profiles WHERE Username = $username AND IsActive = 1";
			command.Parameters.AddWithValue("$username", username);

			var result = await command.ExecuteScalarAsync();
			return result?.ToString();
		}

		private static async Task StoreSecurityInfoAsync(string username, string passwordHash, string passwordSalt)
		{
			await using var connection = new SqliteConnection($"Data Source={SecurityDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = @"
                INSERT OR REPLACE INTO UserSecurity (Username, Hash, Salt)
                VALUES ($username, $hash, $salt)";
			command.Parameters.AddWithValue("$username", username);
			command.Parameters.AddWithValue("$hash", passwordHash);
			command.Parameters.AddWithValue("$salt", passwordSalt);

			await command.ExecuteNonQueryAsync();
		}

		private static async Task<(string Hash, string Salt)?> GetSecurityInfoAsync(string username)
		{
			await using var connection = new SqliteConnection($"Data Source={SecurityDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = "SELECT Hash, Salt FROM UserSecurity WHERE Username = $username";
			command.Parameters.AddWithValue("$username", username);

			await using var reader = await command.ExecuteReaderAsync();
			if (await reader.ReadAsync())
			{
				return (reader.GetString(0), reader.GetString(1));
			}

			return null;
		}

		public static async Task<string> GetProfileImagePathAsync(string username)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = "SELECT ProfileImagePath FROM Users WHERE Username = $username";
				command.Parameters.AddWithValue("$username", username);

				var result = await command.ExecuteScalarAsync();
				return result?.ToString();
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to retrieve profile image path for user: {username}", ex);
			}
		}

		private static bool VerifyPassword(string inputPassword, string storedHash, string storedSalt)
		{
			byte[] saltBytes = Convert.FromBase64String(storedSalt);
			using (var pbkdf2 = new Rfc2898DeriveBytes(inputPassword, saltBytes, 10000))
			{
				byte[] hashBytes = pbkdf2.GetBytes(20);
				return Convert.ToBase64String(hashBytes) == storedHash;
			}
		}

		public static string GetCurrentUsername()
		{
			var sessionId = _activeSessions.FirstOrDefault(s => s.Value.ExpirationTime > DateTime.UtcNow).Key;
			return sessionId != null ? _activeSessions[sessionId].Username : null;
		}

		public static async Task UpdateProfileImageAsync(string username, string newImagePath)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();
				command.CommandText = @"
        UPDATE Users 
        SET ProfileImagePath = $profileImagePath
        WHERE Username = $username";
				command.Parameters.AddWithValue("$profileImagePath", newImagePath);
				command.Parameters.AddWithValue("$username", username);

				await command.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to update profile image: {ex.Message}", ex);
			}
		}
	}
}