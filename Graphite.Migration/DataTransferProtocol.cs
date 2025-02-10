using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Graphite.Migration
{
    public class DataTransferProtocol
    {
        private StreamSocket socket;

        public DataTransferProtocol(StreamSocket socket)
        {
            this.socket = socket;
        }

        public async Task SendMessageAsync(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            uint messageLength = (uint)messageBytes.Length;

            using (var writer = new DataWriter(socket.OutputStream))
            {
                writer.WriteUInt32(messageLength);
                writer.WriteBytes(messageBytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }
        }

        public async Task<string> ReceiveMessageAsync()
        {
            using (var reader = new DataReader(socket.InputStream))
            {
                uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                if (sizeFieldCount != sizeof(uint))
                {
                    throw new Exception("Unexpected end of stream while reading message size.");
                }

                uint messageLength = reader.ReadUInt32();
                uint actualMessageLength = await reader.LoadAsync(messageLength);
                if (messageLength != actualMessageLength)
                {
                    throw new Exception("Unexpected end of stream while reading message content.");
                }

                byte[] messageBytes = new byte[messageLength];
                reader.ReadBytes(messageBytes);
                return Encoding.UTF8.GetString(messageBytes);
            }
        }

        public async Task SendDataAsync(byte[] data)
        {
            using (var writer = new DataWriter(socket.OutputStream))
            {
                writer.WriteUInt32((uint)data.Length);
                writer.WriteBytes(data);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }
        }

        public async Task<byte[]> ReceiveDataAsync()
        {
            using (var reader = new DataReader(socket.InputStream))
            {
                uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                if (sizeFieldCount != sizeof(uint))
                {
                    throw new Exception("Unexpected end of stream while reading data size.");
                }

                uint dataLength = reader.ReadUInt32();
                uint actualDataLength = await reader.LoadAsync(dataLength);
                if (dataLength != actualDataLength)
                {
                    throw new Exception("Unexpected end of stream while reading data content.");
                }

                byte[] data = new byte[dataLength];
                reader.ReadBytes(data);
                return data;
            }
        }
    }
}

