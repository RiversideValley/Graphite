using System;

namespace Riverside.Graphite.IdentityClient.Models
{
	public class TwoFactorAuthItem
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Name { get; set; }
		public string Issuer { get; set; }
		public byte[] Secret { get; set; }
		public int OtpHashMode { get; set; } = 0; // 0 = SHA1, 1 = SHA256, 2 = SHA512
		public int Size { get; set; } = 6;
		public int Step { get; set; } = 30;
	}
}
