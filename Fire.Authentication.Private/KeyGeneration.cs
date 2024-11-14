﻿using System;
using System.Security.Cryptography;

namespace Fire.Authentication.Private;

public static class KeyGeneration
{
	public static byte[] GenerateRandomKey(int length)
	{
		byte[] key = new byte[length];
		using RandomNumberGenerator rnd = RandomNumberGenerator.Create();
		rnd.GetBytes(key);
		return key;
	}

	public static byte[] GenerateRandomKey(OtpHashMode mode = OtpHashMode.Sha1)
	{
		return GenerateRandomKey(LengthForMode(mode));
	}

	public static byte[] DeriveKeyFromMaster(IKeyProvider masterKey, byte[] identifier, OtpHashMode mode = OtpHashMode.Sha1)
	{
		return masterKey == null ? throw new ArgumentNullException(nameof(masterKey)) : masterKey.ComputeHmac(mode, identifier);
	}

	public static byte[] DeriveKeyFromMaster(IKeyProvider masterKey, int serialNumber, OtpHashMode mode = OtpHashMode.Sha1)
	{
		return DeriveKeyFromMaster(masterKey, KeyUtilities.GetBigEndianBytes(serialNumber), mode);
	}

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