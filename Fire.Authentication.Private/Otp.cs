using System;

namespace FireBrowserWinUi3AuthCore;

public abstract class Otp
{
    protected readonly IKeyProvider _secretKey;
    protected readonly OtpHashMode _hashMode;

    public OtpHashMode HashMode => _hashMode;

    public Otp(byte[] secretKey, OtpHashMode mode)
    {
        _secretKey = secretKey?.Length > 0 ? new InMemoryKey(secretKey) : throw new ArgumentNullException(nameof(secretKey), "Secret key is empty");
        _hashMode = mode;
    }

    public Otp(IKeyProvider key, OtpHashMode mode)
    {
        _secretKey = key ?? throw new ArgumentNullException(nameof(key));
        _hashMode = mode;
    }

    protected abstract string Compute(long counter, OtpHashMode mode);

    protected internal long CalculateOtp(byte[] data, OtpHashMode mode)
    {
        var hmacComputedHash = _secretKey.ComputeHmac(mode, data);

        int offset = hmacComputedHash[hmacComputedHash.Length - 1] & 0x0F;
        return (hmacComputedHash[offset] & 0x7f) << 24
            | (hmacComputedHash[offset + 1] & 0xff) << 16
            | (hmacComputedHash[offset + 2] & 0xff) << 8
            | (hmacComputedHash[offset + 3] & 0xff) % 1000000;
    }

    protected internal static string Digits(long input, int digitCount) =>
        (input % (long)Math.Pow(10, digitCount)).ToString().PadLeft(digitCount, '0');

    protected bool Verify(long initialStep, string valueToVerify, out long matchedStep, VerificationWindow window)
    {
        window ??= new VerificationWindow();

        foreach (var frame in window.ValidationCandidates(initialStep))
        {
            var comparisonValue = Compute(frame, _hashMode);
            if (ValuesEqual(comparisonValue, valueToVerify))
            {
                matchedStep = frame;
                return true;
            }
        }

        matchedStep = 0;
        return false;
    }

    private bool ValuesEqual(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];

        return result == 0;
    }
}