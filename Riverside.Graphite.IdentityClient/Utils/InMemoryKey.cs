using Riverside.Graphite.IdentityClient.Enums;
using Riverside.Graphite.IdentityClient.Helpers;
using System;
using System.Security.Cryptography;

namespace Riverside.Graphite.IdentityClient.Utils;

public class InMemoryKey : IKeyProvider
{
	private readonly byte[] _keyData;

	public InMemoryKey(byte[] key)
	{
		_keyData = key ?? throw new ArgumentNullException(nameof(key));
		if (_keyData.Length <= 0)
		{
			throw new ArgumentException("The key must not be empty");
		}
	}

	internal byte[] GetCopyOfKey() => (byte[])_keyData.Clone();

	public byte[] ComputeHmac(OtpHashMode mode, byte[] data)
	{
		using var hmac = CreateHmacHash(mode);
		var key = GetCopyOfKey();
		try
		{
			hmac.Key = key;
			return hmac.ComputeHash(data);
		}
		finally
		{
			KeyUtilities.Destroy(key);
		}
	}

	private static HMAC CreateHmacHash(OtpHashMode otpHashMode) =>
		otpHashMode switch
		{
			OtpHashMode.Sha256 => new HMACSHA256(),
			OtpHashMode.Sha512 => new HMACSHA512(),
			_ => new HMACSHA1() // OtpHashMode.Sha1 or default
		};
}