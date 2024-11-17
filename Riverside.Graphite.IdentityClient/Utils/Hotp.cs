using Riverside.Graphite.IdentityClient.Enums;
using Riverside.Graphite.IdentityClient.Helpers;
using System;

namespace Riverside.Graphite.IdentityClient.Utils;
public class Hotp : Otp
{
	private readonly int _hotpSize;

	public int HotpSize => _hotpSize;

	public Hotp(byte[] secretKey, OtpHashMode mode = OtpHashMode.Sha1, int hotpSize = 6)
		: base(secretKey, mode)
	{
		VerifyParameters(hotpSize);
		_hotpSize = hotpSize;
	}

	public Hotp(IKeyProvider key, OtpHashMode mode = OtpHashMode.Sha1, int hotpSize = 6)
		: base(key, mode)
	{
		VerifyParameters(hotpSize);
		_hotpSize = hotpSize;
	}

	private static void VerifyParameters(int hotpSize)
	{
		if (hotpSize is < 6 or > 8)
		{
			throw new ArgumentOutOfRangeException(nameof(hotpSize));
		}
	}

	public string ComputeHOTP(long counter)
	{
		return Compute(counter, _hashMode);
	}

	public bool VerifyHotp(string hotp, long counter)
	{
		return hotp == ComputeHOTP(counter);
	}

	protected override string Compute(long counter, OtpHashMode mode)
	{
		long otp = CalculateOtp(KeyUtilities.GetBigEndianBytes(counter), mode);
		return Digits(otp, _hotpSize);
	}
}