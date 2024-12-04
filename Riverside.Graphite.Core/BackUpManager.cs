using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Riverside.Graphite.Core
{
	public static class BackupManager
	{
		public static string CreateBackup()
		{
			try
			{
				string currentDate = DateTime.Now.ToString("yyyyMMdd"); // Only year, month, and day
				string randomGuid = Guid.NewGuid().ToString("N")[..5]; // Generate a 5-character substring from GUID
				string backupFileName = $"firebrowserbackup_{currentDate}_{randomGuid}.firebackup";
				string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string fireBrowserUserCorePath = Path.Combine(documentsPath, "FireBrowserUserCore");
				string backupFilePath = Path.Combine(documentsPath, backupFileName);

				if (!Directory.Exists(fireBrowserUserCorePath))
				{
					throw new DirectoryNotFoundException($"FireBrowserUserCore folder not found in Documents: {fireBrowserUserCorePath}");
				}

				// Create zip file directly from the FireBrowserUserCore folder
				//ZipFile.CreateFromDirectory(fireBrowserUserCorePath, backupFilePath, CompressionLevel.Optimal, false);
				ZipDirectoryExcludingSubfolder(fireBrowserUserCorePath, backupFilePath, @"\EBWebView");

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

			// Include files in the root of the source directory
			files.AddRange(Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly));

			return files;
		}
		public static string ReadBackupFile()
		{
			try
			{
				// Set the backup file path (restore.fireback in the temp directory)
				string restoreFilePath = Path.Combine(Path.GetTempPath(), "restore.fireback");

				// Check if the file exists
				if (!File.Exists(restoreFilePath))
				{
					Console.WriteLine("Restore file does not exist.");
					return null; // Return null if the file does not exist
				}

				// Read the file's contents and return it as a string
				string fileContents = File.ReadAllText(restoreFilePath);
				Console.WriteLine("File read successfully.");
				return fileContents;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading backup file: {ex.Message}");
				return null; // Return null if an error occurs
			}
		}

		public static Task<bool> RestoreBackup()
		{
			try
			{
				// Set the backup file path
				string restoreFilePath = Path.Combine(Path.GetTempPath(), "restore.fireback");
				string restorefile = ReadBackupFile();
				if (!File.Exists(restoreFilePath))
				{
					Console.WriteLine("Restore file does not exist.");
					return Task.FromResult(false);
				}

				// Set the target restore directory (FireBrowserUserCore)
				string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string restorePath = Path.Combine(documentsPath, "FireBrowserUserCore");

				// If FireBrowserUserCore exists, delete it 
				if (Directory.Exists(restorePath))
				{
					Directory.Delete(restorePath, true);
					// Create the FireBrowserUserCore folder
					_ = Directory.CreateDirectory(restorePath);

					Console.WriteLine("Existing FireBrowserUserCore folder deleted.");
				}
				
				// Extract the backup file to FireBrowserUserCore
				ZipFile.ExtractToDirectory(restorefile, restorePath, true);

				Console.WriteLine($"Backup restored successfully to: {restorePath}");
				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error restoring backup: {ex.Message}");
				return Task.FromResult(false);
			}
		}
	}
}