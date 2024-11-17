using Riverside.Graphite.IdentityClient.Enums;
using Riverside.Graphite.IdentityClient.Helpers;
using System;

namespace Riverside.Graphite.IdentityClient.Utils;

public class Totp : Otp
{
	private const long UnicEpocTicks = 621355968000000000L;
	private const long TicksToSeconds = 10000000L;

	private readonly int _step;
	private readonly int _totpSize;
	private readonly TimeCorrection _correctedTime;

	public int Step => _step;
	public int TotpSize => _totpSize;
	public TimeCorrection TimeCorrection => _correctedTime;

	public Totp(byte[] secretKey, int step = 30, OtpHashMode mode = OtpHashMode.Sha1, int totpSize = 6, TimeCorrection timeCorrection = null)
		: base(secretKey, mode)
	{
		VerifyParameters(step, totpSize);
		_step = step;
		_totpSize = totpSize;
		_correctedTime = timeCorrection ?? TimeCorrection.UncorrectedInstance;
	}

	public Totp(IKeyProvider key, int step = 30, OtpHashMode mode = OtpHashMode.Sha1, int totpSize = 6, TimeCorrection timeCorrection = null)
		: base(key, mode)
	{
		VerifyParameters(step, totpSize);
		_step = step;
		_totpSize = totpSize;
		_correctedTime = timeCorrection ?? TimeCorrection.UncorrectedInstance;
	}

	private static void VerifyParameters(int step, int totpSize)
	{
		if (step <= 0 || totpSize <= 0 || totpSize > 10)
		{
			throw new ArgumentOutOfRangeException(step <= 0 ? nameof(step) : nameof(totpSize));
		}
	}

	public string ComputeTotp(DateTime timestamp)
	{
		return ComputeTotpFromSpecificTime(_correctedTime.GetCorrectedTime(timestamp));
	}

	public string ComputeTotp()
	{
		return ComputeTotpFromSpecificTime(_correctedTime.CorrectedUtcNow);
	}

	private string ComputeTotpFromSpecificTime(DateTime timestamp)
	{
		return Compute(CalculateTimeStepFromTimestamp(timestamp), _hashMode);
	}

	public bool VerifyTotp(string totp, out long timeStepMatched, VerificationWindow window = null)
	{
		return VerifyTotpForSpecificTime(_correctedTime.CorrectedUtcNow, totp, window, out timeStepMatched);
	}

	public bool VerifyTotp(DateTime timestamp, string totp, out long timeStepMatched, VerificationWindow window = null)
	{
		return VerifyTotpForSpecificTime(_correctedTime.GetCorrectedTime(timestamp), totp, window, out timeStepMatched);
	}

	private bool VerifyTotpForSpecificTime(DateTime timestamp, string totp, VerificationWindow window, out long timeStepMatched)
	{
		return Verify(CalculateTimeStepFromTimestamp(timestamp), totp, out timeStepMatched, window);
	}

	private long CalculateTimeStepFromTimestamp(DateTime timestamp)
	{
		return (timestamp.Ticks - UnicEpocTicks) / TicksToSeconds / _step;
	}

	public int RemainingSeconds()
	{
		return RemainingSecondsForSpecificTime(_correctedTime.CorrectedUtcNow);
	}

	public int RemainingSeconds(DateTime timestamp)
	{
		return RemainingSecondsForSpecificTime(_correctedTime.GetCorrectedTime(timestamp));
	}

	private int RemainingSecondsForSpecificTime(DateTime timestamp)
	{
		return _step - (int)(((timestamp.Ticks - UnicEpocTicks) / TicksToSeconds) % _step);
	}

	public DateTime WindowStart()
	{
		return WindowStartForSpecificTime(_correctedTime.CorrectedUtcNow);
	}

	public DateTime WindowStart(DateTime timestamp)
	{
		return WindowStartForSpecificTime(_correctedTime.GetCorrectedTime(timestamp));
	}

	private DateTime WindowStartForSpecificTime(DateTime timestamp)
	{
		return timestamp.AddTicks(-(timestamp.Ticks - UnicEpocTicks) % (TicksToSeconds * _step));
	}

	protected override string Compute(long counter, OtpHashMode mode)
	{
		return Digits(CalculateOtp(KeyUtilities.GetBigEndianBytes(counter), mode), _totpSize);
	}
}