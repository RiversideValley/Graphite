namespace Riverside.Graphite.IdentityClient.Private;

public interface IKeyProvider
{
	byte[] ComputeHmac(OtpHashMode mode, byte[] data);
}