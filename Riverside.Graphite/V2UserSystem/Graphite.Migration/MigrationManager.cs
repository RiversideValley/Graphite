using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Graphite.UserSys;

namespace Graphite.Migration
{
    public class MigrationManager
    {
        private const int PORT = 8888;
        private readonly string _graphiteDataPath;
        private readonly string _userCoreDbPath;

        public MigrationManager(string graphiteDataPath)
        {
            _graphiteDataPath = graphiteDataPath;
            _userCoreDbPath = Path.Combine(graphiteDataPath, "UserCore.db");
        }

        public async Task<List<UserV2>> GetAvailableUsersAsync()
        {
            // Use the provided method to get all Graphite users
            return await UserManager.GetAllUsersAsync();
        }

      
        public async Task<bool> SendMigrationData(string username, string password, string destinationIp)
        {
            if (!AuthenticateUser(username, password))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var userData = await CollectUserData(username);
            var encryptedData = EncryptData(userData, password);

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(destinationIp, PORT);
                using (var stream = client.GetStream())
                {
                    var writer = new BinaryWriter(stream);
                    writer.Write(encryptedData.Length);
                    await stream.WriteAsync(encryptedData, 0, encryptedData.Length);
                }
            }

            return true;
        }

        public async Task<bool> ReceiveMigrationData(string username, string password)
        {
            using (var listener = new TcpListener(IPAddress.Any, PORT))
            {
                listener.Start();
                using (var client = await listener.AcceptTcpClientAsync())
                using (var stream = client.GetStream())
                {
                    var reader = new BinaryReader(stream);
                    int dataLength = reader.ReadInt32();
                    byte[] encryptedData = new byte[dataLength];
                    await stream.ReadAsync(encryptedData, 0, dataLength);

                    var decryptedData = DecryptData(encryptedData, password);
                    await RestoreUserData(username, decryptedData);
                }
            }

            return true;
        }

        private bool AuthenticateUser(string username, string password)
        {
            // Implement user authentication logic here
            // You might want to use a method from UserManager to verify the password
            // For now, we'll just check if the user has a password
            var user = GetAvailableUsersAsync().Result.Find(u => u.Username == username);
            return user != null && user.HasPassword;
        }

        private async Task<string> CollectUserData(string username)
        {
            var userData = new Dictionary<string, string>();

            string userFolderPath = Path.Combine(_graphiteDataPath, username);
            string[] dbFiles = { "History.db", "Favorites.db", "Downloads.db" };
            foreach (var dbFile in dbFiles)
            {
                string dbPath = Path.Combine(userFolderPath, dbFile);
                if (File.Exists(dbPath))
                {
                    userData[dbFile] = Convert.ToBase64String(File.ReadAllBytes(dbPath));
                }
            }

            return JsonSerializer.Serialize(userData);
        }

        private async Task RestoreUserData(string username, string userData)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(userData);

            string userFolderPath = Path.Combine(_graphiteDataPath, username);
            Directory.CreateDirectory(userFolderPath);

            foreach (var kvp in data)
            {
                string dbPath = Path.Combine(userFolderPath, kvp.Key);
                File.WriteAllBytes(dbPath, Convert.FromBase64String(kvp.Value));
            }
        }

        private byte[] EncryptData(string data, string password)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKey(password, aes.KeySize / 8);
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(data);
                    }
                    return ms.ToArray();
                }
            }
        }

        private string DecryptData(byte[] encryptedData, string password)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKey(password, aes.KeySize / 8);
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(encryptedData, iv, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(encryptedData, iv.Length, encryptedData.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private byte[] DeriveKey(string password, int keySize)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 10000))
            {
                return deriveBytes.GetBytes(keySize);
            }
        }
    }
}

