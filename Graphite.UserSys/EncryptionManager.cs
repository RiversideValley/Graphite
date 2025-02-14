using Graphite.UserSys;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Graphite.UserSys
{
    public static class EncryptionManager
    {
        public static async Task HandleEncryptionSettingChangeAsync(string username, string settingKey, bool newValue)
        {
            string folderName = settingKey.Replace("Encrypt", "");
            string folderPath = Path.Combine(UserManager.GraphiteDataPath, username, folderName);

            if (newValue)
            {
                await EncryptDirectoryAsync(folderPath, GetEncryptionKey(username));
            }
            else
            {
                await DecryptDirectoryAsync(folderPath, GetEncryptionKey(username));
            }
        }

        public static async Task DecryptUserDataIfNeededAsync(string username)
        {
            var encryptDatabase = await SettingsManager.GetSettingAsync<bool>(username, "EncryptDatabase");
            var encryptPermissions = await SettingsManager.GetSettingAsync<bool>(username, "EncryptPermissions");
            var encryptSettings = await SettingsManager.GetSettingAsync<bool>(username, "EncryptSettings");

            string userFolderPath = Path.Combine(UserManager.GraphiteDataPath, username);

            if (encryptDatabase)
                await DecryptDirectoryAsync(Path.Combine(userFolderPath, "Database"), GetEncryptionKey(username));

            if (encryptPermissions)
                await DecryptDirectoryAsync(Path.Combine(userFolderPath, "Permissions"), GetEncryptionKey(username));

            if (encryptSettings)
                await DecryptDirectoryAsync(Path.Combine(userFolderPath, "Settings"), GetEncryptionKey(username));
        }

        private static async Task EncryptDirectoryAsync(string directoryPath, string encryptionKey)
        {
            foreach (string filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                await EncryptFileAsync(filePath, encryptionKey);
            }
        }

        private static async Task DecryptDirectoryAsync(string directoryPath, string encryptionKey)
        {
            foreach (string filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                await DecryptFileAsync(filePath, encryptionKey);
            }
        }

        private static async Task EncryptFileAsync(string filePath, string encryptionKey)
        {
            byte[] fileContent = await File.ReadAllBytesAsync(filePath);
            byte[] encryptedContent = EncryptBytes(fileContent, encryptionKey);
            await File.WriteAllBytesAsync(filePath, encryptedContent);
        }

        private static async Task DecryptFileAsync(string filePath, string encryptionKey)
        {
            byte[] encryptedContent = await File.ReadAllBytesAsync(filePath);
            byte[] decryptedContent = DecryptBytes(encryptedContent, encryptionKey);
            await File.WriteAllBytesAsync(filePath, decryptedContent);
        }

        private static byte[] EncryptBytes(byte[] plainBytes, string key)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = System.Text.Encoding.UTF8.GetBytes(key);
            aesAlg.GenerateIV();

            using MemoryStream msEncrypt = new MemoryStream();
            msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
            {
                csEncrypt.Write(plainBytes, 0, plainBytes.Length);
            }

            return msEncrypt.ToArray();
        }

        private static byte[] DecryptBytes(byte[] cipherBytes, string key)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = System.Text.Encoding.UTF8.GetBytes(key);

            byte[] iv = new byte[aesAlg.BlockSize / 8];
            Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            using MemoryStream msDecrypt = new MemoryStream();
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Write))
            {
                csDecrypt.Write(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
            }

            return msDecrypt.ToArray();
        }

        private static string GetEncryptionKey(string username)
        {
            // In a real-world scenario, you should use a secure key management system
            // This is a simplified example and should not be used in production
            return $"EncryptionKey_{username}";
        }
    }
}