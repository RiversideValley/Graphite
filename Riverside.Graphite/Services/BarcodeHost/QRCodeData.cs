using Riverside.Graphite.Services.BarcodeHost.FrameworkMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Riverside.Graphite.Services.BarcodeHost;
public class QRCodeData : IDisposable
{
	public List<BitArray> ModuleMatrix { get; set; }

	public QRCodeData(int version)
	{
		Version = version;
		int size = ModulesPerSideFromVersion(version);
		ModuleMatrix = new List<BitArray>();
		for (int i = 0; i < size; i++)
		{
			ModuleMatrix.Add(new BitArray(size));
		}
	}
#if NETFRAMEWORK || NETSTANDARD2_0 || NET5_0
    public QRCodeData(string pathToRawData, Compression compressMode) : this(File.ReadAllBytes(pathToRawData), compressMode)
    {
    }
#endif
	public QRCodeData(byte[] rawData, Compression compressMode)
	{
		List<byte> bytes = new(rawData);

		//Decompress
		if (compressMode == Compression.Deflate)
		{
			using MemoryStream input = new(bytes.ToArray());
			using MemoryStream output = new();
			using (DeflateStream dstream = new(input, CompressionMode.Decompress))
			{
				Stream4Methods.CopyTo(dstream, output);
			}
			bytes = new List<byte>(output.ToArray());
		}
		else if (compressMode == Compression.GZip)
		{
			using MemoryStream input = new(bytes.ToArray());
			using MemoryStream output = new();
			using (GZipStream dstream = new(input, CompressionMode.Decompress))
			{
				Stream4Methods.CopyTo(dstream, output);
			}
			bytes = new List<byte>(output.ToArray());
		}

		if (bytes[0] != 0x51 || bytes[1] != 0x52 || bytes[2] != 0x52)
		{
			throw new Exception("Invalid raw data file. Filetype doesn't match \"QRR\".");
		}

		//Set QR code version
		int sideLen = bytes[4];
		bytes.RemoveRange(0, 5);
		Version = ((sideLen - 21 - 8) / 4) + 1;

		//Unpack
		Queue<bool> modules = new(8 * bytes.Count);
		foreach (byte b in bytes)
		{
			_ = new BitArray(new byte[] { b });
			for (int i = 7; i >= 0; i--)
			{
				modules.Enqueue((b & (1 << i)) != 0);
			}
		}

		//Build module matrix
		ModuleMatrix = new List<BitArray>(sideLen);
		for (int y = 0; y < sideLen; y++)
		{
			ModuleMatrix.Add(new BitArray(sideLen));
			for (int x = 0; x < sideLen; x++)
			{
				ModuleMatrix[y][x] = modules.Dequeue();
			}
		}
	}

	public byte[] GetRawData(Compression compressMode)
	{
		List<byte> bytes = new();

		//Add header - signature ("QRR")
		bytes.AddRange(new byte[] { 0x51, 0x52, 0x52, 0x00 });

		//Add header - rowsize
		bytes.Add((byte)ModuleMatrix.Count);

		//Build data queue
		Queue<int> dataQueue = new();
		foreach (BitArray row in ModuleMatrix)
		{
			foreach (object module in row)
			{
				dataQueue.Enqueue((bool)module ? 1 : 0);
			}
		}
		for (int i = 0; i < 8 - ((ModuleMatrix.Count * ModuleMatrix.Count) % 8); i++)
		{
			dataQueue.Enqueue(0);
		}

		//Process queue
		while (dataQueue.Count > 0)
		{
			byte b = 0;
			for (int i = 7; i >= 0; i--)
			{
				b += (byte)(dataQueue.Dequeue() << i);
			}
			bytes.Add(b);
		}
		byte[] rawData = bytes.ToArray();

		//Compress stream (optional)
		if (compressMode == Compression.Deflate)
		{
			using MemoryStream output = new();
			using (DeflateStream dstream = new(output, CompressionMode.Compress))
			{
				dstream.Write(rawData, 0, rawData.Length);
			}
			rawData = output.ToArray();
		}
		else if (compressMode == Compression.GZip)
		{
			using MemoryStream output = new();
			using (GZipStream gzipStream = new(output, CompressionMode.Compress, true))
			{
				gzipStream.Write(rawData, 0, rawData.Length);
			}
			rawData = output.ToArray();
		}
		return rawData;
	}

#if NETFRAMEWORK || NETSTANDARD2_0 || NET5_0
    public void SaveRawData(string filePath, Compression compressMode)
    {
        File.WriteAllBytes(filePath, GetRawData(compressMode));
    }
#endif

	public int Version { get; private set; }

	private static int ModulesPerSideFromVersion(int version)
	{
		return 21 + ((version - 1) * 4);
	}

	public void Dispose()
	{
		ModuleMatrix = null;
		Version = 0;
	}

	public enum Compression
	{
		Uncompressed,
		Deflate,
		GZip
	}
}