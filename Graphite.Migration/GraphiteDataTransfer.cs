using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Windows.Networking.Sockets;
using System.Threading;

namespace Graphite.Migration
{
	public class GraphiteDataTransfer
	{
		public static readonly string GraphiteDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			@"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");

		public static readonly string GraphiteSecurityPath = @"C:\ProgramData\Graphite\Security";

		private const string TransferFolderName = "GraphiteTransfer";
		private const int MaxRetries = 3;
		private const int RetryDelayMs = 1000;

		public delegate void ProgressChangedHandler(double percentage);
		public event ProgressChangedHandler ProgressChanged;

		private Dictionary<string, byte[]> collectedData = new Dictionary<string, byte[]>();

		public async Task CollectDataAsync()
		{
			await CollectFromPath(GraphiteDataPath);
			await CollectFromPath(GraphiteSecurityPath);
		}

		private async Task CollectFromPath(string sourcePath)
		{
			if (Directory.Exists(sourcePath))
			{
				foreach (string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
				{
					string relativePath = Path.GetRelativePath(sourcePath, filePath);
					byte[] fileContent = await ReadFileWithRetryAsync(filePath);
					if (fileContent != null)
					{
						collectedData[Path.Combine(sourcePath, relativePath)] = fileContent;
					}
				}
			}
		}

		private async Task<byte[]> ReadFileWithRetryAsync(string filePath)
		{
			for (int attempt = 0; attempt < MaxRetries; attempt++)
			{
				try
				{
					if (!IsFileLocked(filePath))
					{
						using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							byte[] buffer = new byte[fileStream.Length];
							await fileStream.ReadAsync(buffer, 0, buffer.Length);
							return buffer;
						}
					}
				}
				catch (IOException)
				{
					if (attempt < MaxRetries - 1)
					{
						await Task.Delay(RetryDelayMs);
						continue;
					}
				}
				catch (Exception)
				{
					// Skip this file if we can't read it
					return null;
				}
			}
			return null;
		}

		private bool IsFileLocked(string filePath)
		{
			try
			{
				using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					stream.Close();
				}
			}
			catch (IOException)
			{
				return true;
			}
			return false;
		}

		private async Task WriteFileWithRetryAsync(string filePath, byte[] content)
		{
			for (int attempt = 0; attempt < MaxRetries; attempt++)
			{
				try
				{
					string directory = Path.GetDirectoryName(filePath);
					if (!Directory.Exists(directory))
					{
						Directory.CreateDirectory(directory);
					}

					if (!IsFileLocked(filePath))
					{
						using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
						{
							await fileStream.WriteAsync(content, 0, content.Length);
							await fileStream.FlushAsync();
						}
						return;
					}
				}
				catch (IOException)
				{
					if (attempt < MaxRetries - 1)
					{
						await Task.Delay(RetryDelayMs);
						continue;
					}
					throw;
				}
			}
			throw new IOException($"Unable to write to file {filePath} after {MaxRetries} attempts");
		}

		public async Task SendDataAsync(GraphiteTransferProtocol protocol)
		{
			long totalSize = collectedData.Sum(kvp => kvp.Value.Length);
			long transferredSize = 0;

			foreach (var kvp in collectedData)
			{
				try
				{
					await protocol.SendMessageAsync(kvp.Key);
					await protocol.SendDataAsync(kvp.Value);

					transferredSize += kvp.Value.Length;
					double progress = (double)transferredSize / totalSize * 100;
					ProgressChanged?.Invoke(progress);
				}
				catch (Exception ex)
				{
					throw new Exception($"Error sending file {kvp.Key}: {ex.Message}", ex);
				}
			}

			await protocol.SendMessageAsync("TRANSFER_COMPLETE");
		}

		public async Task ReceiveDataAsync(GraphiteTransferProtocol protocol)
		{
			string transferPath = Path.Combine(Path.GetTempPath(), TransferFolderName);
			Directory.CreateDirectory(transferPath);

			try
			{
				while (true)
				{
					string filePath = await protocol.ReceiveMessageAsync();
					if (filePath == "TRANSFER_COMPLETE")
						break;

					byte[] fileContent = await protocol.ReceiveDataAsync();
					string tempFilePath = Path.Combine(transferPath, Path.GetFileName(filePath));

					await WriteFileWithRetryAsync(tempFilePath, fileContent);

					double progress = CalculateProgress(transferPath);
					ProgressChanged?.Invoke(progress);
				}

				await MoveFilesToFinalLocation(transferPath);
			}
			finally
			{
				if (Directory.Exists(transferPath))
				{
					try
					{
						Directory.Delete(transferPath, true);
					}
					catch
					{
						// Ignore cleanup errors
					}
				}
			}
		}

		private double CalculateProgress(string transferPath)
		{
			long transferredSize = CalculateDirectorySize(transferPath);
			long totalSize = collectedData.Sum(kvp => kvp.Value.Length);
			return totalSize > 0 ? (double)transferredSize / totalSize * 100 : 0;
		}

		private long CalculateDirectorySize(string path)
		{
			if (!Directory.Exists(path))
				return 0;

			try
			{
				return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
					.Sum(file =>
					{
						try
						{
							return new FileInfo(file).Length;
						}
						catch
						{
							return 0;
						}
					});
			}
			catch
			{
				return 0;
			}
		}

		private async Task MoveFilesToFinalLocation(string transferPath)
		{
			foreach (var sourcePath in new[] { GraphiteDataPath, GraphiteSecurityPath })
			{
				string sourceTransferPath = Path.Combine(transferPath, Path.GetFileName(sourcePath));
				if (Directory.Exists(sourceTransferPath))
				{
					await MoveFiles(sourceTransferPath, sourcePath);
				}
			}
		}

		private async Task MoveFiles(string sourcePath, string destinationPath)
		{
			foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
			{
				string newPath = dirPath.Replace(sourcePath, destinationPath);
				Directory.CreateDirectory(newPath);
			}

			foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
			{
				string newPath = filePath.Replace(sourcePath, destinationPath);

				for (int attempt = 0; attempt < MaxRetries; attempt++)
				{
					try
					{
						if (File.Exists(newPath))
						{
							if (!IsFileLocked(newPath))
							{
								File.Delete(newPath);
							}
							else
							{
								if (attempt < MaxRetries - 1)
								{
									await Task.Delay(RetryDelayMs);
									continue;
								}
								throw new IOException($"Destination file is locked: {newPath}");
							}
						}

						File.Move(filePath, newPath);
						break;
					}
					catch (IOException) when (attempt < MaxRetries - 1)
					{
						await Task.Delay(RetryDelayMs);
					}
				}
			}
		}
	}
}

