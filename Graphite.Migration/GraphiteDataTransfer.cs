using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Networking.Sockets;
using System.Linq;

namespace Graphite.Migration
{
	public class GraphiteDataTransfer
	{
		public static readonly string GraphiteDataPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			@"Packages\9617Riverside.Graphite_5272ve26\LocalState\GraphiteData");

		public static readonly string GraphiteSecurityPath = @"C:\ProgramData\Graphite\Security";

		private const string TransferFolderName = "GraphiteTransfer";

		public delegate void ProgressChangedHandler(double percentage);
		public event ProgressChangedHandler ProgressChanged;

		public async Task SendDataAsync(GraphiteTransferProtocol protocol)
		{
			long totalSize = CalculateTotalSize();
			long transferredSize = 0;

			transferredSize += await SendFromPath(GraphiteDataPath, protocol, transferredSize, totalSize);
			transferredSize += await SendFromPath(GraphiteSecurityPath, protocol, transferredSize, totalSize);

			await protocol.SendMessageAsync("TRANSFER_COMPLETE");
		}

		private async Task<long> SendFromPath(string sourcePath, GraphiteTransferProtocol protocol, long currentTransferredSize, long totalSize)
		{
			long pathTransferredSize = 0;

			if (Directory.Exists(sourcePath))
			{
				foreach (string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
				{
					string relativePath = Path.GetRelativePath(sourcePath, filePath);
					byte[] fileContent = await File.ReadAllBytesAsync(filePath);

					await protocol.SendMessageAsync(sourcePath);
					await protocol.SendMessageAsync(relativePath);
					await protocol.SendDataAsync(fileContent);

					pathTransferredSize += fileContent.Length;
					double progress = (double)(currentTransferredSize + pathTransferredSize) / totalSize * 100;
					ProgressChanged?.Invoke(progress);
				}
			}

			return pathTransferredSize;
		}

		public async Task ReceiveDataAsync(GraphiteTransferProtocol protocol)
		{
			string transferPath = Path.Combine(Path.GetTempPath(), TransferFolderName);
			Directory.CreateDirectory(transferPath);

			while (true)
			{
				string sourcePath = await protocol.ReceiveMessageAsync();
				if (sourcePath == "TRANSFER_COMPLETE")
					break;

				string relativePath = await protocol.ReceiveMessageAsync();
				byte[] fileContent = await protocol.ReceiveDataAsync();

				string tempFilePath = Path.Combine(transferPath, relativePath);
				string tempDirectory = Path.GetDirectoryName(tempFilePath);

				if (!Directory.Exists(tempDirectory))
				{
					Directory.CreateDirectory(tempDirectory);
				}

				await File.WriteAllBytesAsync(tempFilePath, fileContent);

				double progress = CalculateProgress(transferPath);
				ProgressChanged?.Invoke(progress);
			}

			await MoveFilesToFinalLocation(transferPath);
		}

		private long CalculateTotalSize()
		{
			long totalSize = 0;
			totalSize += CalculateDirectorySize(GraphiteDataPath);
			totalSize += CalculateDirectorySize(GraphiteSecurityPath);
			return totalSize;
		}

		private long CalculateDirectorySize(string path)
		{
			if (!Directory.Exists(path))
				return 0;

			return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
				.Sum(file => new FileInfo(file).Length);
		}

		private double CalculateProgress(string transferPath)
		{
			long transferredSize = CalculateDirectorySize(transferPath);
			long totalSize = CalculateTotalSize();
			return (double)transferredSize / totalSize * 100;
		}

		private async Task MoveFilesToFinalLocation(string transferPath)
		{
			await MoveFiles(transferPath, GraphiteDataPath);
			await MoveFiles(transferPath, GraphiteSecurityPath);

			Directory.Delete(transferPath, true);
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
				if (File.Exists(newPath))
				{
					File.Delete(newPath);
				}
				File.Move(filePath, newPath);
			}
		}
	}
}

