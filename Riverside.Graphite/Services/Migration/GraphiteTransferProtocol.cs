using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Riverside.Graphite.Services.Migration
{
	public class GraphiteTransferProtocol
	{
		private StreamSocket socket;
		private DataReader reader;
		private DataWriter writer;
		private const int CHUNK_SIZE = (int)(2.5 * 1024 * 1024); // 2.5 MB chunks

		public GraphiteTransferProtocol(StreamSocket socket)
		{
			this.socket = socket;
			reader = new DataReader(socket.InputStream);
			writer = new DataWriter(socket.OutputStream);
		}

		public async Task SendMessageAsync(string message)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(message);
			await SendDataAsync(messageBytes);
		}

		public async Task<string> ReceiveMessageAsync()
		{
			byte[] data = await ReceiveDataAsync();
			return Encoding.UTF8.GetString(data);
		}

		public async Task SendDataAsync(byte[] data)
		{
			// Send data length
			writer.WriteUInt32((uint)data.Length);
			await writer.StoreAsync();

			// Send data in chunks
			for (int i = 0; i < data.Length; i += CHUNK_SIZE)
			{
				int chunkSize = Math.Min(CHUNK_SIZE, data.Length - i);
				writer.WriteBytes(data.AsSpan(i, chunkSize).ToArray());
				await writer.StoreAsync();
			}

			// Send checksum
			byte[] checksum = ComputeChecksum(data);
			writer.WriteBytes(checksum);
			await writer.StoreAsync();
		}

		public async Task<byte[]> ReceiveDataAsync()
		{
			// Receive data length
			uint dataLength = await ReceiveUInt32Async();

			// Receive data in chunks
			using MemoryStream memoryStream = new MemoryStream();
			uint remainingBytes = dataLength;
			while (remainingBytes > 0)
			{
				uint chunkSize = Math.Min(CHUNK_SIZE, remainingBytes);
				byte[] chunk = new byte[chunkSize];
				await reader.LoadAsync(chunkSize);
				reader.ReadBytes(chunk);
				await memoryStream.WriteAsync(chunk);
				remainingBytes -= chunkSize;
			}

			byte[] receivedData = memoryStream.ToArray();

			// Receive and verify checksum
			await reader.LoadAsync(32);
			byte[] receivedChecksum = new byte[32];
			reader.ReadBytes(receivedChecksum);

			byte[] computedChecksum = ComputeChecksum(receivedData);
			if (!CompareChecksums(receivedChecksum, computedChecksum))
			{
				throw new Exception("Checksum verification failed");
			}

			return receivedData;
		}

		private async Task<uint> ReceiveUInt32Async()
		{
			await reader.LoadAsync(sizeof(uint));
			return reader.ReadUInt32();
		}

		private byte[] ComputeChecksum(byte[] data)
		{
			using SHA256 sha256 = SHA256.Create();
			return sha256.ComputeHash(data);
		}

		private bool CompareChecksums(byte[] checksum1, byte[] checksum2)
		{
			return CryptographicEqual(checksum1, checksum2);
		}

		private bool CryptographicEqual(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				return false;

			int result = 0;
			for (int i = 0; i < a.Length; i++)
			{
				result |= a[i] ^ b[i];
			}
			return result == 0;
		}

		public void Dispose()
		{
			reader?.Dispose();
			writer?.Dispose();
			socket?.Dispose();
		}
	}
}

