using System;

namespace Riverside.Graphite.IdentityClient.Helpers;

internal static class KeyUtilities
{
	internal static void Destroy(byte[] sensitiveData)
	{
		ArgumentNullException.ThrowIfNull(sensitiveData);

		var rng = new Random();
		rng.NextBytes(sensitiveData);
	}

	internal static byte[] GetBigEndianBytes(long input)
	{
		var data = BitConverter.GetBytes(input);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(data);
		}
		return data;
	}

	internal static byte[] GetBigEndianBytes(int input)
	{
		var data = BitConverter.GetBytes(input);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(data);
		}
		return data;
	}
}