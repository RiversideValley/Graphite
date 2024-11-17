using System;

namespace Riverside.Graphite.IdentityClient.Helpers;

public class TimeCorrection
{
	public static readonly TimeCorrection UncorrectedInstance = new();

	private TimeCorrection()
	{
		CorrectionFactor = TimeSpan.FromSeconds(0);
	}

	public TimeCorrection(DateTime correctUtc)
	{
		CorrectionFactor = DateTime.UtcNow - correctUtc;
	}

	public TimeCorrection(DateTime correctTime, DateTime referenceTime)
	{
		CorrectionFactor = referenceTime - correctTime;
	}

	public DateTime GetCorrectedTime(DateTime referenceTime)
	{
		return referenceTime - CorrectionFactor;
	}

	public DateTime CorrectedUtcNow => GetCorrectedTime(DateTime.UtcNow);

	public TimeSpan CorrectionFactor { get; }
}