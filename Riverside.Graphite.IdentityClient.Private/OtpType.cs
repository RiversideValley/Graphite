namespace Riverside.Graphite.IdentityClient.Private;
public enum OtpType
{
	/// <summary>
	/// Time-based OTP
	/// see https://datatracker.ietf.org/doc/html/rfc6238
	/// </summary>
	Totp,
	/// <summary>
	/// HMAC-based (counter) OTP
	/// see https://datatracker.ietf.org/doc/html/rfc4226
	/// </summary>
	Hotp
}