using System;
using System.IO;

namespace Riverside.Graphite.Core
{
    public static class DirectoryHelper
    {
		public static void DeleteAllFilesRecursive(string directoryPath)
		{
			if (Directory.Exists(directoryPath))
			{
				// Get all files in the directory and subdirectories
				string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

				// Delete each file
				foreach (string file in files)
				{
					try
					{
						File.Delete(file);
						Console.WriteLine($"Deleted file: {file}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error deleting file {file}: {ex.Message}");
					}
				}

				try
				{
					Directory.Delete(directoryPath, true);
					Console.WriteLine($"Deleted root directory: {directoryPath}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error deleting root directory {directoryPath}: {ex.Message}");
				}

			}
			else
			{
				Console.WriteLine($"Directory not found: {directoryPath}");
			}
		}
		public static void DeleteAllFiles(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(directoryPath);

                // Delete each file
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Directory not found: {directoryPath}");
            }
        }
    }
}
