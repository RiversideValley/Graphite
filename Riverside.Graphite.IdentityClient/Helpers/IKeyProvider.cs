using Riverside.Graphite.IdentityClient.Enums;

namespace Riverside.Graphite.IdentityClient.Helpers;

public interface IKeyProvider
{
	byte[] ComputeHmac(OtpHashMode mode, byte[] data);
}