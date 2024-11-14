using System;

namespace Fire.Authentication.Private;

internal static class KeyUtilities
{
	internal static void Destroy(byte[] sensitiveData)
	{
		if (sensitiveData == null)
		{
			throw new ArgumentNullException(nameof(sensitiveData));
		}

		Random rng = new();
		rng.NextBytes(sensitiveData);
	}

	internal static byte[] GetBigEndianBytes(long input)
	{
		byte[] data = BitConverter.GetBytes(input);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(data);
		}
		return data;
	}

	internal static byte[] GetBigEndianBytes(int input)
	{
		byte[] data = BitConverter.GetBytes(input);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(data);
		}
		return data;
	}
}