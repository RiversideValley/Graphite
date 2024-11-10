namespace Fire.Authentication.Private;

public interface IKeyProvider
{
	byte[] ComputeHmac(OtpHashMode mode, byte[] data);
}