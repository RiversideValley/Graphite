namespace FireBrowserWinUi3AuthCore;

public interface IKeyProvider
{
    byte[] ComputeHmac(OtpHashMode mode, byte[] data);
}