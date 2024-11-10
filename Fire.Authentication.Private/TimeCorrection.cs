using System;

namespace FireBrowserWinUi3AuthCore;

public class TimeCorrection
{
    public static readonly TimeCorrection UncorrectedInstance = new TimeCorrection();

    private TimeCorrection() => CorrectionFactor = TimeSpan.FromSeconds(0);

    public TimeCorrection(DateTime correctUtc) => CorrectionFactor = DateTime.UtcNow - correctUtc;

    public TimeCorrection(DateTime correctTime, DateTime referenceTime) => CorrectionFactor = referenceTime - correctTime;

    public DateTime GetCorrectedTime(DateTime referenceTime) => referenceTime - CorrectionFactor;

    public DateTime CorrectedUtcNow => GetCorrectedTime(DateTime.UtcNow);

    public TimeSpan CorrectionFactor { get; }
}