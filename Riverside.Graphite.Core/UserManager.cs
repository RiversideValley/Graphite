using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using Windows.Storage;
using System.Security;
using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Core.Helper.Logging;
using Windows.UI.WindowManagement;
using Windows.System;

namespace Riverside.Graphite.Core
{
	public static class UserManager
	{
		// Use fixed names for key files
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

		public static readonly string GraphiteDataPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath);//
																																					  //
																				//Path.Combine(GetFullPathToExe(), "V2_UserData");	

		//Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),@"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");

		private const string MainDbName = "UserCore.db";
		public static string MainDbPath = Path.Combine(GraphiteDataPath, MainDbName);

		// Login attempt tracking for brute force protection
		private static readonly ConcurrentDictionary<string, (int Attempts, DateTime LastAttempt)> _loginAttempts =
			new ConcurrentDictionary<string, (int Attempts, DateTime LastAttempt)>();
		private const int MaxLoginAttempts = 5;
		private const int LockoutMinutes = 15;

		// Session management
		private static readonly ConcurrentDictionary<string, (string Username, DateTime ExpirationTime)> _activeSessions =
			new ConcurrentDictionary<string, (string Username, DateTime ExpirationTime)>();
		private const int SessionExpirationMinutes = 30;

		public static UIElement? ActiveElement { get; set; }	

		public static string GetFullPathToExe()
		{
			string path = AppDomain.CurrentDomain.BaseDirectory;
			int pos = path.LastIndexOf("\\");
			return path[..pos];
		}

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
                    CREATE TABLE IF NOT EXISTS UserSecurity (
                        Username TEXT PRIMARY KEY,
                        Hash TEXT,
                        Salt TEXT,
                        FOREIGN KEY(Username) REFERENCES Users(Username) ON DELETE CASCADE ON UPDATE CASCADE
                    );
                    CREATE TABLE IF NOT EXISTS blobs (
                        Username TEXT PRIMARY KEY,
                        Metadata TEXT,
                        FOREIGN KEY(Username) REFERENCES Users(Username) ON DELETE CASCADE ON UPDATE CASCADE
                    );";
				await command.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to initialize application.", ex);
			}
		}

		private static async Task EnsureEncryptionKeysAsync()
		{
			if (_cachedKey != null && _cachedIV != null)
			{
				return; // Keys already loaded
			}

			try
			{
                string securityFolder = Path.GetDirectoryName(KeyFilePath) ?? throw new InvalidOperationException("KeyFilePath is invalid.");

				// Ensure the security folder exists
				if (!Directory.Exists(securityFolder))
				{
					Directory.CreateDirectory(securityFolder);
				}

				if (File.Exists(KeyFilePath) && File.Exists(IVFilePath))
				{
					try
					{
						// Load existing keys
						_cachedKey = File.ReadAllBytes(KeyFilePath);
						_cachedIV = File.ReadAllBytes(IVFilePath);

						// Validate key and IV lengths
						if (_cachedKey.Length != 32 || _cachedIV.Length != 16)
						{
							throw new Exception("Invalid key or IV length detected.");
						}
					}
					catch
					{
						// If there's any error reading or validating the keys, treat as if they don't exist
						_cachedKey = null;
						_cachedIV = null;

						// Delete potentially corrupted files
						if (File.Exists(KeyFilePath)) File.Delete(KeyFilePath);
						if (File.Exists(IVFilePath)) File.Delete(IVFilePath);
					}
				}

				// Generate new keys if they don't exist or were invalid
				if (_cachedKey == null || _cachedIV == null)
				{
					using (var aes = Aes.Create())
					{
						aes.KeySize = 256;
						aes.GenerateKey();
						aes.GenerateIV();

						_cachedKey = aes.Key;
						_cachedIV = aes.IV;

						// Save keys
						File.WriteAllBytes(KeyFilePath, _cachedKey);
						File.WriteAllBytes(IVFilePath, _cachedIV);

						// Set file attributes using Windows.Storage API
						try
						{
							StorageFile keyFile = await StorageFile.GetFileFromPathAsync(KeyFilePath);
							StorageFile ivFile = await StorageFile.GetFileFromPathAsync(IVFilePath);

							// Set the files as hidden
							await keyFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });
							await ivFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });

							var keyProps = await keyFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });
							var ivProps = await ivFile.Properties.RetrievePropertiesAsync(new string[] { "System.FileAttributes" });

							uint keyAttributes = (uint)keyProps["System.FileAttributes"];
							uint ivAttributes = (uint)ivProps["System.FileAttributes"];

							keyAttributes |= 0x2; // FILE_ATTRIBUTE_HIDDEN
							ivAttributes |= 0x2; // FILE_ATTRIBUTE_HIDDEN

							await keyFile.Properties.SavePropertiesAsync(new Dictionary<string, object> { { "System.FileAttributes", keyAttributes } });
							await ivFile.Properties.SavePropertiesAsync(new Dictionary<string, object> { { "System.FileAttributes", ivAttributes } });
						}
						catch (Exception ex)
						{
							// Log but don't throw - the keys are still saved even if we can't set attributes
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

		private async static Task<string> EncryptPassword(string password)
		{
			if (string.IsNullOrEmpty(password))
				return string.Empty;

			try
			{
				await EnsureEncryptionKeysAsync();

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

		public static async Task<List<UserV2>> GetAllUsersAsync()
		{
			var users = new List<UserV2>();

			try
			{
				await using (var connection = new SqliteConnection($"Data Source={MainDbPath}"))
				{ 
					await connection.OpenAsync();

					var command = connection.CreateCommand();
					command.CommandText = "SELECT Username, Email, ProfileImagePath, HasPassword FROM Users";

					await using (var reader = await command.ExecuteReaderAsync()) { 
						while (await reader.ReadAsync())
						{
							users.Add(new UserV2
							{
								Username = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
								Email = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
								ProfileImagePath = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
								HasPassword = reader.GetInt32(3) == 1,
								WindowsUserName = Environment.UserName,
								IsFirstLaunch = false
							});
						}
					};
				};
				
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to retrieve users.", ex);
			}
			
			return users;
		}

		public static async Task<UserV2> GetUserAsync(string username)
		{
			try
			{
				await using (var connection = new SqliteConnection($"Data Source={MainDbPath}")) { 
					await connection.OpenAsync();

					var command = connection.CreateCommand();
					command.CommandText = "SELECT Username, Email, ProfileImagePath, HasPassword, WindowsUserName, IsFirstLaunch FROM Users WHERE Username = $username";
					command.Parameters.AddWithValue("$username", username);

					await using (var reader = await command.ExecuteReaderAsync()) { 
						if (await reader.ReadAsync())
						{
							return new UserV2
							{
								Username = reader.GetString(0),
								Email = reader.IsDBNull(1) ? null : reader.GetString(1),
								ProfileImagePath = reader.IsDBNull(2) ? null : reader.GetString(2),
								HasPassword = reader.GetInt32(3) == 1,
								WindowsUserName = reader.IsDBNull(4) ? null : reader.GetString(4),
								IsFirstLaunch = reader.GetInt32(5) == 1
							};
						}
					};
				};
				
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to retrieve user: {username}", ex);
			}

			return null;
		}

		public static async Task<UserV2> UpdateUserInUserData(UserV2 user, string encryptedPassword) {

			try
			{
				
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();

				try
				{
					var command = connection.CreateCommand();
				    command.CommandText = @"UPDATE Users 
                    SET PasswordHash = $passwordHash, 
                    Email = $email, 
                    WindowsUserName = $windowsUserName, 
                    IsFirstLaunch = $isFirstLaunch, 
                    ProfileImagePath = $profileImagePath, 
                    HasPassword = $hasPassword
                    WHERE Username = $username";

					command.Parameters.AddWithValue("$username", user.Username);
					command.Parameters.AddWithValue("$passwordHash", string.IsNullOrEmpty(encryptedPassword) ? DBNull.Value : encryptedPassword);
					command.Parameters.AddWithValue("$email", (object)user.Email ?? DBNull.Value);
					command.Parameters.AddWithValue("$windowsUserName", user.WindowsUserName);
					command.Parameters.AddWithValue("$isFirstLaunch", user.IsFirstLaunch ? 1 : 0);
					command.Parameters.AddWithValue("$profileImagePath", string.IsNullOrEmpty(user.ProfileImagePath) ? DBNull.Value : user.ProfileImagePath);
					command.Parameters.AddWithValue("$hasPassword", string.IsNullOrEmpty(encryptedPassword) ? 0 : 1);

					await command.ExecuteNonQueryAsync();

					// Insert metadata into blobs table
					var metadataCommand = connection.CreateCommand();
                    metadataCommand.CommandText = @"UPDATE blobs SET Metadata = $metadata WHERE Username = $username";
					metadataCommand.Parameters.AddWithValue("$username", user.Username);
					metadataCommand.Parameters.AddWithValue("$metadata", JsonSerializer.Serialize(new
					{
						CreationDate = DateTime.UtcNow,
						LastLogin = DateTime.UtcNow,
						HasPassword = user.HasPassword
					}));
					await metadataCommand.ExecuteNonQueryAsync();

					await transaction.CommitAsync();

					connection.Close();
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
				throw new Exception($"Failed to create user in database: {ex.Message}", ex);
			}
			

		}
		public static async Task<UserV2> CreateUserInUserData(UserV2 user, string encryptedPassword) {

			try
			{
				var isUser = await GetUserAsync(user.Username);
				if (isUser is UserV2)
					return await UpdateUserInUserData(isUser, encryptedPassword); 

				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				using var transaction = await connection.BeginTransactionAsync();

				try
				{
					var command = connection.CreateCommand();
					command.CommandText = @"INSERT INTO Users (Username, PasswordHash, Email, WindowsUserName, IsFirstLaunch, ProfileImagePath, HasPassword)
								VALUES ($username, $passwordHash, $email, $windowsUserName, $isFirstLaunch, $profileImagePath, $hasPassword)";

					command.Parameters.AddWithValue("$username", user.Username);
					command.Parameters.AddWithValue("$passwordHash", string.IsNullOrEmpty(encryptedPassword) ? DBNull.Value : encryptedPassword);
					command.Parameters.AddWithValue("$email", (object)user.Email ?? DBNull.Value);
					command.Parameters.AddWithValue("$windowsUserName", user.WindowsUserName);
					command.Parameters.AddWithValue("$isFirstLaunch", user.IsFirstLaunch ? 1 : 0);
					command.Parameters.AddWithValue("$profileImagePath", string.IsNullOrEmpty(user.ProfileImagePath) ? DBNull.Value : user.ProfileImagePath);
					command.Parameters.AddWithValue("$hasPassword", user.HasPassword ? 1 : 0);

					await command.ExecuteNonQueryAsync();



					// Insert metadata into blobs table
					var metadataCommand = connection.CreateCommand();
					metadataCommand.CommandText = @"INSERT INTO blobs (Username, Metadata) VALUES ($username, $metadata)";
					metadataCommand.Parameters.AddWithValue("$username", user.Username);
					metadataCommand.Parameters.AddWithValue("$metadata", JsonSerializer.Serialize(new
					{
						CreationDate = DateTime.UtcNow,
						LastLogin = DateTime.UtcNow,
						HasPassword = user.HasPassword
					}));
					await metadataCommand.ExecuteNonQueryAsync();

					await transaction.CommitAsync();

					// Initialize user's settings
					await SettingsManager.InitializeUserSettingsAsync(user.Username);
					connection.Close();
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
				throw new Exception($"Failed to create user in database: {ex.Message}", ex);
			}
		}
		public static async Task<UserV2> CreateValidateUserFolders(UserV2 user,  string sanitizedUsername, Stream profileImageStream = null) {

			string userFolderPath = Path.Combine(GraphiteDataPath, sanitizedUsername);
			if (Directory.Exists(userFolderPath))
			{
				return user; 
			}

			Directory.CreateDirectory(userFolderPath);
			foreach (var folder in new[] { "Browser", "Database", "Permissions", "Settings" })
			{
				Directory.CreateDirectory(Path.Combine(userFolderPath, folder));
			}

			// Create necessary .db files
			string userDbPath = Path.Combine(userFolderPath, "Database", "user_data.db");
			await using (var connection = new SqliteConnection($"Data Source={userDbPath}"))
			{
				await connection.OpenAsync(); // This will create the file if it doesn't exist
			}

			// Handle profile image
			string profileImagePath = Path.Combine(userFolderPath, "profile_image.jpg");
			user.ProfileImagePath = profileImagePath;
			if (profileImageStream != null)
			{
				try
				{
					using (var fileStream = File.Create(profileImagePath))
					{
						await profileImageStream.CopyToAsync(fileStream);
						profileImageStream.Close(); 
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
						// Create an empty file if default image doesn't exist
						File.WriteAllBytes(profileImagePath, new byte[0]);
					}
				}
				catch (Exception ex)
				{
					throw new Exception($"Failed to create default profile image: {ex.Message}", ex);
				}
			}

			return user; 

		}

        public static async Task<UserV2> CreateUserAsync(string username, string password = null, string email = null, Stream profileImageStream = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    throw new ArgumentException("Username is required.", nameof(username));
                }

                // Sanitize username for folder name
                string sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));

                var user = new UserV2
                {
                    Username = username,
                    Email = email,
                    WindowsUserName = Environment.UserName,
                    IsFirstLaunch = true,
                    HasPassword = !string.IsNullOrEmpty(password)
                };

                // Only encrypt password if it's provided
                string? encryptedPassword = default;
                string? passwordHash = default;
                string? passwordSalt = default;

                if (!string.IsNullOrEmpty(password))
                {
                    (passwordHash, passwordSalt) = HashPassword(password);
                    encryptedPassword = await EncryptPassword(password);
                    // Store hash and salt in a separate, more secure database
                    await StoreSecurityInfoAsync(username, passwordHash, passwordSalt);
                }

                // Create user folder structure
				var UserAndInfo =  await CreateValidateUserFolders(user, sanitizedUsername, profileImageStream);
				// Create user in database
				return await CreateUserInUserData(UserAndInfo, encryptedPassword ?? string.Empty);

            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create user: {ex.Message}", ex);
            }
        }

        public static (string Hash, string Salt) HashPassword(string password)
        {
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(20);
                return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
            }
        }

		public static async Task<UserV2> AuthenticateAsync(string username, string password)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				throw new ArgumentException("Username cannot be empty", nameof(username));
			}

			username = username.Trim();

			// Check if the account is locked
			if (IsAccountLocked(username))
			{
				throw new SecurityException($"Account is locked. Please try again after {LockoutMinutes} minutes.");
			}

			try
			{
				await SettingsManager.InitializeUserSettingsAsync(username);
				
				var existingUser = await GetUserAsync(username);
				if (existingUser is UserV2 user)
				{
					if (user.HasPassword)
					{
						var securityInfo = await GetSecurityInfoAsync(username);
						if (securityInfo.HasValue)
						{
							var (storedHash, storedSalt) = securityInfo.Value;
							if (VerifyPassword(password, storedHash, storedSalt))
							{
								// Reset login attempts on successful login
								ResetLoginAttempts(username);
								// Generate and store session ID
								user.SessionId = GenerateSessionId(username);
								return user;
							}
						}
					}
					else
					{
						//if (!await MigrateUserToMandatoryPassword(existingUser))
						//	throw new SecurityException("Database integrity compromised. Please contact support.");
						//// No password set, allow login
						ResetLoginAttempts(username);
						user.SessionId = GenerateSessionId(username);
						return user;
					}
				}	

				IncrementLoginAttempts(username);

				return null;
			}
			catch (SecurityException)
			{
				// Rethrow SecurityException to be handled by the caller
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
						// Reset attempts after lockout period
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
					// Extend session
					_activeSessions[sessionId] = (sessionInfo.Username, DateTime.UtcNow.AddMinutes(SessionExpirationMinutes));
					return true;
				}
				else
				{
					// Session expired, remove it
					_activeSessions.TryRemove(sessionId, out _);
				}
			}
			return false;
		}

		public static void EndSession(string sessionId)
		{
			_activeSessions.TryRemove(sessionId, out _);
		}

		public static async Task<bool> MigrateUserToNewPassword(UserV2 user) {

			var passwordBox = new PasswordBox { PlaceholderText = "New password" };
			var dialog = new ContentDialog
			{
				Title = $"Password Manager",
				PrimaryButtonText = "Create",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
				Content = passwordBox,
				XamlRoot = ActiveElement?.XamlRoot
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				// add to security table
				var securityUser = await CreateUserAsync(
					user.Username,
					passwordBox.Password,
					user.Email
				);

				if (securityUser is UserV2 userV2) {
					var authenticatedUser = await UserManager.AuthenticateAsync(userV2.Username, passwordBox.Password);
					if (authenticatedUser != null)
						return true;
				}
				
			}

			return false;

		}
		public static async Task<bool> ValidatePassWord(UserV2 user) {
			
			var passwordBox = new PasswordBox { PlaceholderText = "Enter password" };
			var dialog = new ContentDialog
			{
				Title = $"Enter password for {user.Username}",
				PrimaryButtonText = "Login",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
				Content = passwordBox,
				XamlRoot = ActiveElement?.XamlRoot
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				var authenticatedUser = await UserManager.AuthenticateAsync(user.Username, passwordBox.Password);
				if (authenticatedUser != null)
					return true; 
			}

			return false;
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
					// Check if the user exists and has a password
					var user = await UserManager.GetUserAsync(username);

					if (user is null) return false;

					// If the user has a password, verify it
					if (user.HasPassword)
					{
						if (!await ValidatePassWord(new UserV2 { Username = username }))
							throw new UnauthorizedAccessException("Password required for user deletion.");
					}
					
					// Delete the user
					var deleteCommand = connection.CreateCommand();
					deleteCommand.CommandText = "DELETE FROM Users WHERE Username = $username";
					deleteCommand.Parameters.AddWithValue("$username", username);

					int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

					if (rowsAffected > 0)
					{
						await transaction.CommitAsync();

						// Delete user folder
						try
						{
							await UserDataManager.DeleteUser(username);
						}
						catch (Exception ex)
						{
							ExceptionLogger.LogException(new("Can't delete user data folder", ex));
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
				finally {
				
					connection.Close();
				}

			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to delete user: {ex.Message}", ex);
			}
			
		}

        private static bool VerifyPassword(string inputPassword, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(inputPassword, saltBytes, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(20);
                return Convert.ToBase64String(hashBytes) == storedHash;
            }
        }

		public static async Task SetPasswordAsync(string username, string newPassword)
		{
			try
			{
				await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
				await connection.OpenAsync();

				string encryptedPassword = await EncryptPassword(newPassword);

				var command = connection.CreateCommand();
				command.CommandText = @"
                    UPDATE Users 
                    SET PasswordHash = $passwordHash, HasPassword = 1
                    WHERE Username = $username";
				command.Parameters.AddWithValue("$passwordHash", encryptedPassword);
				command.Parameters.AddWithValue("$username", username);

				await command.ExecuteNonQueryAsync();
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
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to remove password: {ex.Message}", ex);
			}
		}

		public static async Task<UserV2> LoginAsGuestAsync()
		{
			const string guestUsername = "Guest";
			const string guestEmail = "guest@app.com";

			try
			{
				// Check if Guest user already exists
				var existingGuest = await GetUserAsync(guestUsername);
				if (existingGuest != null)
				{
					// If Guest user exists, reset the data
					await ResetGuestDataAsync(guestUsername);
				}
				else
				{
					// Guest user doesn't exist, create it without a profile image
					await CreateUserAsync(guestUsername, null, guestEmail, null);
				}

				// Set IsFirstLaunch to false for Guest user
				await using (var connection = new SqliteConnection($"Data Source={MainDbPath}"))
				{
					await connection.OpenAsync();

					var command = connection.CreateCommand();
					command.CommandText = "UPDATE Users SET IsFirstLaunch = 0 WHERE Username = $username";
					command.Parameters.AddWithValue("$username", guestUsername);
					await command.ExecuteNonQueryAsync();
				}

				// Create or reset guest folder structure
				await CreateGuestFolderStructureAsync(guestUsername);

				// Authenticate and return the guest user
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
				// Delete existing guest folder if it exists
				if (Directory.Exists(guestFolderPath))
				{
					Directory.Delete(guestFolderPath, true);
				}

				// Reset any guest-specific data in the database
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
				// Create main guest folder
				Directory.CreateDirectory(guestFolderPath);

				// Create subfolders
				string[] subfolders = { "Browser", "Database", "Permissions", "Settings" };
				foreach (var folder in subfolders)
				{
					Directory.CreateDirectory(Path.Combine(guestFolderPath, folder));
				}

				// Create necessary files
				await CreateEmptyFileAsync(Path.Combine(guestFolderPath, "Settings", "user_settings.json"));
				await CreateEmptyFileAsync(Path.Combine(guestFolderPath, "Permissions", "permissions.json"));
				await CreateEmptyFileAsync(Path.Combine(guestFolderPath, "Database", "user_data.db"));

				// Initialize user settings
				await SettingsManager.InitializeUserSettingsAsync(guestUsername);

				// Create a README file in the Browser folder
				string readmePath = Path.Combine(guestFolderPath, "Browser", "README.txt");
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
			reader.Close();
			connection.Close(); 

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
				// Create profile-specific folder structure
				string profileFolderPath = Path.Combine(GraphiteDataPath, username, profileName);
				Directory.CreateDirectory(profileFolderPath);
				foreach (var folder in new[] { "Browser", "Database", "Permissions", "Settings" })
				{
					Directory.CreateDirectory(Path.Combine(profileFolderPath, folder));
				}

				// Initialize profile settings
				await SettingsManager.InitializeUserSettingsAsync(profileName);
			}

			return rowsAffected > 0;
		}

		public static async Task<bool> SwitchProfileAsync(string username, string profileName)
		{
			await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
			await connection.OpenAsync();

			// Deactivate all profiles for the user
			var deactivateCommand = connection.CreateCommand();
			deactivateCommand.CommandText = "UPDATE Profiles SET IsActive = 0 WHERE Username = $username";
			deactivateCommand.Parameters.AddWithValue("$username", username);
			await deactivateCommand.ExecuteNonQueryAsync();

			// Activate the selected profile
			var activateCommand = connection.CreateCommand();
			activateCommand.CommandText = "UPDATE Profiles SET IsActive = 1 WHERE Username = $username AND ProfileName = $profileName";
			activateCommand.Parameters.AddWithValue("$username", username);
			activateCommand.Parameters.AddWithValue("$profileName", profileName);

			int rowsAffected = await activateCommand.ExecuteNonQueryAsync();

			if (rowsAffected > 0)
			{                // Load profile-specific settings
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

		
		public static async Task<bool> ValidateSecurityDatabase()
		{

			try
			{
				string securityDbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"Graphite",
				"Security",
				"UserSecurity.db");

				await using var connection = new SqliteConnection($"Data Source={securityDbPath}");
				await connection.OpenAsync();

				var command = connection.CreateCommand();

				command.CommandText = @"
					CREATE TABLE IF NOT EXISTS UserSecurity (
						Username TEXT PRIMARY KEY,
						Hash TEXT,
						Salt TEXT
					);";

				var result = await command.ExecuteNonQueryAsync();
				connection.Close();

				if (result >= 0) return true;

			}
			catch (Exception ex)
			{
				Helper.Logging.ExceptionLogger.LogException(ex);

			}
			
			return false; 

		}

		private static async Task StoreSecurityInfoAsync(string username, string passwordHash, string passwordSalt)
		{

			if (! await ValidateSecurityDatabase()) throw new NotImplementedException("Security Database isn't available");

			string securityDbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"Graphite",
				"Security",
				"UserSecurity.db");

			await using var connection = new SqliteConnection($"Data Source={securityDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = @"
					CREATE TABLE IF NOT EXISTS UserSecurity (
						Username TEXT PRIMARY KEY,
						Hash TEXT,
						Salt TEXT
					);
					INSERT OR REPLACE INTO UserSecurity (Username, Hash, Salt)
					VALUES ($username, $hash, $salt)";
			command.Parameters.AddWithValue("$username", username);
			command.Parameters.AddWithValue("$hash", passwordHash);
			command.Parameters.AddWithValue("$salt", passwordSalt);

			await command.ExecuteNonQueryAsync();
		}

		private static async Task<(string Hash, string Salt)?> GetSecurityInfoAsync(string username)
		{
			string securityDbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"Graphite",
				"Security",
				"UserSecurity.db");

			await using var connection = new SqliteConnection($"Data Source={securityDbPath}");
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

		public static async Task<Task> UpdateSecurityInfoAsync(UserV2 user, string oldName = null)
		{
			string securityDbPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"Graphite",
				"Security",
				"UserSecurity.db");

			await using var connection = new SqliteConnection($"Data Source={securityDbPath}");
			await connection.OpenAsync();

			var command = connection.CreateCommand();
			command.CommandText = "UPDATE UserSecurity SET Username = $name WHERE Username = $username"; 
			command.Parameters.AddWithValue("$username", oldName ?? user.Username);
			command.Parameters.AddWithValue("$name", user.Username);

			await command.ExecuteNonQueryAsync();

			return Task.CompletedTask; 
		}

		public static async Task<UserV2> UpdateUserPropertiesAsync(UserV2 user, string oldName = null)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    var command = connection.CreateCommand();
                    var updateFields = new List<string>();
                    var parameters = new Dictionary<string, object>();

					if (!string.IsNullOrEmpty(oldName)) {

						updateFields.Add("UserName = $name");
						parameters.Add("$name", user.Username);
					}

                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        updateFields.Add("Email = $email");
                        parameters.Add("$email", user.Email);
                    }

                    if (!string.IsNullOrEmpty(user.WindowsUserName))
                    {
                        updateFields.Add("WindowsUserName = $windowsUserName");
                        parameters.Add("$windowsUserName", user.WindowsUserName);
                    }

                    if (user.IsFirstLaunch)
                    {
                        updateFields.Add("IsFirstLaunch = $isFirstLaunch");
                        parameters.Add("$isFirstLaunch", user.IsFirstLaunch ? 1 : 0);
                    }

                    if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        updateFields.Add("ProfileImagePath = $profileImagePath");
                        parameters.Add("$profileImagePath", user.ProfileImagePath);
                    }

                    if (user.HasPassword)
                    {
                        updateFields.Add("HasPassword = $hasPassword");
                        parameters.Add("$hasPassword", user.HasPassword ? 1 : 0);
                    }

                    if (updateFields.Count == 0)
                    {
                        throw new ArgumentException("No properties to update", nameof(user));
                    }

                    command.CommandText = $"UPDATE Users SET {string.Join(", ", updateFields)} WHERE Username = $username";
                    command.Parameters.AddWithValue("$username", oldName ?? user.Username);

                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }

					var answer = await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    connection.Close();
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
                throw new Exception($"Failed to update user properties: {ex.Message}", ex);
            }
        }

		
	}

}

