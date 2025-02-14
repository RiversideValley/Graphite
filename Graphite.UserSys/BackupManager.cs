using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.UserSys
{
    public static class BackupManager
    {
        public static async Task<string> CreateBackupAsync(string userId)
        {
            try
            {
                string currentDate = DateTime.Now.ToString("yyyyMMdd");
                string randomGuid = Guid.NewGuid().ToString("N")[..3];
                string backupFileName = $"graphite_backup_{currentDate}_{randomGuid}.zip";
                string backupFilePath = Path.Combine(UserManager.GraphiteDataPath, backupFileName);

                string userFolderPath = Path.Combine(UserManager.GraphiteDataPath, userId);

                if (!Directory.Exists(userFolderPath))
                {
                    throw new DirectoryNotFoundException($"User folder not found: {userFolderPath}");
                }

                await Task.Run(() => ZipDirectoryExcludingSubfolder(userFolderPath, backupFilePath, "Browser"));

                return !File.Exists(backupFilePath) ? throw new FileNotFoundException("Backup file was not created successfully.") : backupFilePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create backup: {ex.Message}", ex);
            }
        }

        private static void ZipDirectoryExcludingSubfolder(string sourceDirectory, string destinationZipFile, string subfolderToExclude)
        {
            using FileStream zipToOpen = new(destinationZipFile, FileMode.Create);
            using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
            IEnumerable<string> files = EnumerateFilesExcludingSubfolder(sourceDirectory, subfolderToExclude);

            foreach (string file in files)
            {
                string entryName = Path.GetRelativePath(sourceDirectory, file);
                _ = archive.CreateEntryFromFile(file, entryName);
            }
        }

        private static IEnumerable<string> EnumerateFilesExcludingSubfolder(string sourceDirectory, string subfolderToExclude)
        {
            List<string> files = new();

            foreach (string directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                if (!directory.Contains(subfolderToExclude, StringComparison.OrdinalIgnoreCase))
                {
                    files.AddRange(Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly));
                }
            }

            files.AddRange(Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly));

            return files;
        }

        public static string ReadBackupFile(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    Console.WriteLine("Backup file does not exist.");
                    return null;
                }

                Console.WriteLine("Backup file found successfully.");
                return backupFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading backup file: {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> RestoreBackupAsync(string userId, string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    Console.WriteLine("Backup file does not exist.");
                    return false;
                }

                string userFolderPath = Path.Combine(UserManager.GraphiteDataPath, userId);

                if (Directory.Exists(userFolderPath))
                {
                    Directory.Delete(userFolderPath, true);
                }

                Directory.CreateDirectory(userFolderPath);

                await Task.Run(() => ZipFile.ExtractToDirectory(backupFilePath, userFolderPath, true));

                Console.WriteLine($"Backup restored successfully to: {userFolderPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring backup: {ex.Message}");
                return false;
            }
        }
    }
}
