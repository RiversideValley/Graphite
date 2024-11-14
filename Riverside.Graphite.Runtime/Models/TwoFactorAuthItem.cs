namespace Riverside.Graphite.Runtime.Models;

public class TwoFactorAuthItem
{
	public int OtpHashMode { get; set; } = 0; // 0 = sha1, 1 = sha256, 2 = sha512
	public int Size { get; set; } = 6;
	public int Step { get; set; } = 30;
	public string Name { get; set; }
	public string Issuer { get; set; }
	public byte[] Secret { get; set; }
}