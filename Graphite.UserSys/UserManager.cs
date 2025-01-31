using System;
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

namespace Graphite.UserSys
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

        public static readonly string GraphiteDataPath = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
       @"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");

        private const string MainDbName = "UserCore.db";
        private static readonly string MainDbPath = Path.Combine(GraphiteDataPath, MainDbName);

        private static async Task EnsureEncryptionKeysAsync()
        {
            if (_cachedKey != null && _cachedIV != null)
            {
                return; // Keys already loaded
            }

            try
            {
                string securityFolder = Path.GetDirectoryName(KeyFilePath);

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
                    );";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize application.", ex);
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

                // Sanitize username for folder name
                string sanitizedUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));

                var user = new User
                {
                    Username = username,
                    Email = email,
                    WindowsUserName = Environment.UserName,
                    IsFirstLaunch = true,
                    HasPassword = !string.IsNullOrEmpty(password)
                };

                // Only encrypt password if it's provided
                string encryptedPassword = null;
                if (!string.IsNullOrEmpty(password))
                {
                    encryptedPassword = EncryptPassword(password);
                }

                // Create user folder structure
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

                // Handle profile image
                string profileImagePath = Path.Combine(userFolderPath, "profile_image.jpg");
                if (profileImageStream != null)
                {
                    try
                    {
                        using (var randomAccessStream = new InMemoryRandomAccessStream())
                        {
                            await RandomAccessStream.CopyAsync(profileImageStream.AsInputStream(), randomAccessStream);
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

                            using (var resizedStream = new InMemoryRandomAccessStream())
                            {
                                BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                                encoder.BitmapTransform.ScaledWidth = 200;
                                encoder.BitmapTransform.ScaledHeight = 200;
                                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                                await encoder.FlushAsync();

                                StorageFolder userFolder = await StorageFolder.GetFolderFromPathAsync(userFolderPath);
                                StorageFile profileFile = await userFolder.CreateFileAsync("profile_image.jpg", CreationCollisionOption.ReplaceExisting);

                                using (var fileStream = await profileFile.OpenAsync(FileAccessMode.ReadWrite))
                                {
                                    await RandomAccessStream.CopyAsync(resizedStream, fileStream);
                                }
                            }
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

                user.ProfileImagePath = profileImagePath;

                try
                {
                    await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
                    await connection.OpenAsync();

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

                    // Initialize user's settings
                    await SettingsManager.InitializeUserSettingsAsync(username);

                    return user;
                }
                catch (Exception ex)
                {
                    // If database operations fail, clean up the created directories
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

        public static async Task<User> AuthenticateAsync(string username, string password = null)
        {
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
                    string storedEncryptedPassword = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash"));

                    if (!hasPassword || (hasPassword && password != null && DecryptPassword(storedEncryptedPassword) == password))
                    {
                        var user = new User
                        {
                            Username = username,
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                            WindowsUserName = reader.GetString(reader.GetOrdinal("WindowsUserName")),
                            IsFirstLaunch = reader.GetInt32(reader.GetOrdinal("IsFirstLaunch")) == 1,
                            ProfileImagePath = reader.GetString(reader.GetOrdinal("ProfileImagePath")),
                            HasPassword = hasPassword
                        };

                        // Get user profiles
                        user.Profiles = await GetUserProfilesAsync(username);

                        return user;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Authentication failed: {ex.Message}", ex);
            }
        }

        public static async Task<bool> DeleteUserAsync(string username, string password = null)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={MainDbPath}");
                await connection.OpenAsync();

                // First, check if the user exists and has a password
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT HasPassword, PasswordHash FROM Users WHERE Username = $username";
                checkCommand.Parameters.AddWithValue("$username", username);

                await using var reader = await checkCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    bool hasPassword = reader.GetInt32(0) == 1;
                    string storedEncryptedPassword = reader.IsDBNull(1) ? null : reader.GetString(1);

                    // If the user has a password, verify it
                    if (hasPassword)
                    {
                        if (password == null || DecryptPassword(storedEncryptedPassword) != password)
                        {
                            throw new UnauthorizedAccessException("Incorrect password provided for user deletion.");
                        }
                    }
                }
                else
                {
                    return false; // User not found
                }

                // If we've made it here, either the user doesn't have a password or the correct password was provided
                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM Users WHERE Username = $username";
                deleteCommand.Parameters.AddWithValue("$username", username);

                int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    string userFolderPath = Path.Combine(GraphiteDataPath, username);
                    if (Directory.Exists(userFolderPath))
                    {
                        Directory.Delete(userFolderPath, true);
                    }
                    return true;
                }

                return false;
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

        public static async Task<User> LoginAsGuestAsync()
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
                return await AuthenticateAsync(guestUsername);
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
                await SettingsManager.InitializeUserSettingsAsync(username);
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
            {
                // Load profile-specific settings
                await SettingsManager.GetAllSettingsAsync(username);
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
    }  
}