using System;

namespace Fire.Authentication.Private;

public class Totp : Otp
{
	private const long UnicEpocTicks = 621355968000000000L;
	private const long TicksToSeconds = 10000000L;

	public int Step { get; }
	public int TotpSize { get; }
	public TimeCorrection TimeCorrection { get; }

	public Totp(byte[] secretKey, int step = 30, OtpHashMode mode = OtpHashMode.Sha1, int totpSize = 6, TimeCorrection timeCorrection = null)
		: base(secretKey, mode)
	{
		VerifyParameters(step, totpSize);
		Step = step;
		TotpSize = totpSize;
		TimeCorrection = timeCorrection ?? TimeCorrection.UncorrectedInstance;
	}

	public Totp(IKeyProvider key, int step = 30, OtpHashMode mode = OtpHashMode.Sha1, int totpSize = 6, TimeCorrection timeCorrection = null)
		: base(key, mode)
	{
		VerifyParameters(step, totpSize);
		Step = step;
		TotpSize = totpSize;
		TimeCorrection = timeCorrection ?? TimeCorrection.UncorrectedInstance;
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
		return ComputeTotpFromSpecificTime(TimeCorrection.GetCorrectedTime(timestamp));
	}

	public string ComputeTotp()
	{
		return ComputeTotpFromSpecificTime(TimeCorrection.CorrectedUtcNow);
	}

	private string ComputeTotpFromSpecificTime(DateTime timestamp)
	{
		return Compute(CalculateTimeStepFromTimestamp(timestamp), _hashMode);
	}

	public bool VerifyTotp(string totp, out long timeStepMatched, VerificationWindow window = null)
	{
		return VerifyTotpForSpecificTime(TimeCorrection.CorrectedUtcNow, totp, window, out timeStepMatched);
	}

	public bool VerifyTotp(DateTime timestamp, string totp, out long timeStepMatched, VerificationWindow window = null)
	{
		return VerifyTotpForSpecificTime(TimeCorrection.GetCorrectedTime(timestamp), totp, window, out timeStepMatched);
	}

	private bool VerifyTotpForSpecificTime(DateTime timestamp, string totp, VerificationWindow window, out long timeStepMatched)
	{
		return Verify(CalculateTimeStepFromTimestamp(timestamp), totp, out timeStepMatched, window);
	}

	private long CalculateTimeStepFromTimestamp(DateTime timestamp)
	{
		return (timestamp.Ticks - UnicEpocTicks) / TicksToSeconds / Step;
	}

	public int RemainingSeconds()
	{
		return RemainingSecondsForSpecificTime(TimeCorrection.CorrectedUtcNow);
	}

	public int RemainingSeconds(DateTime timestamp)
	{
		return RemainingSecondsForSpecificTime(TimeCorrection.GetCorrectedTime(timestamp));
	}

	private int RemainingSecondsForSpecificTime(DateTime timestamp)
	{
		return Step - (int)((timestamp.Ticks - UnicEpocTicks) / TicksToSeconds % Step);
	}

	public DateTime WindowStart()
	{
		return WindowStartForSpecificTime(TimeCorrection.CorrectedUtcNow);
	}

	public DateTime WindowStart(DateTime timestamp)
	{
		return WindowStartForSpecificTime(TimeCorrection.GetCorrectedTime(timestamp));
	}

	private DateTime WindowStartForSpecificTime(DateTime timestamp)
	{
		return timestamp.AddTicks(-(timestamp.Ticks - UnicEpocTicks) % (TicksToSeconds * Step));
	}

	protected override string Compute(long counter, OtpHashMode mode)
	{
		return Digits(CalculateOtp(KeyUtilities.GetBigEndianBytes(counter), mode), TotpSize);
	}
}