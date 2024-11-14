using System;
using System.Security.Cryptography;

namespace Riverside.Graphite.IdentityClient.Private;

public static class KeyGeneration
{
	public static byte[] GenerateRandomKey(int length)
	{
		byte[] key = new byte[length];
		using var rnd = RandomNumberGenerator.Create();
		rnd.GetBytes(key);
		return key;
	}

	public static byte[] GenerateRandomKey(OtpHashMode mode = OtpHashMode.Sha1)
	{
		return GenerateRandomKey(LengthForMode(mode));
	}

	public static byte[] DeriveKeyFromMaster(IKeyProvider masterKey, byte[] identifier, OtpHashMode mode = OtpHashMode.Sha1)
	{
		if (masterKey == null)
		{
			throw new ArgumentNullException(nameof(masterKey));
		}
		return masterKey.ComputeHmac(mode, identifier);
	}

	public static byte[] DeriveKeyFromMaster(IKeyProvider masterKey, int serialNumber, OtpHashMode mode = OtpHashMode.Sha1) =>
		DeriveKeyFromMaster(masterKey, KeyUtilities.GetBigEndianBytes(serialNumber), mode);

	private static int LengthForMode(OtpHashMode mode)
	{
		return mode switch
		{
			OtpHashMode.Sha256 => 32,
			OtpHashMode.Sha512 => 64,
			_ => 20 // OtpHashMode.Sha1 or default
		};
	}
}