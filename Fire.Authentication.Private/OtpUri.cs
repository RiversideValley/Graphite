using System;
using System.Collections.Generic;
using System.Text;

namespace Fire.Authentication.Private;

public class OtpUri
{
	public readonly OtpType Type;
	public readonly string Secret;
	public readonly string User;
	public readonly string Issuer;
	public readonly OtpHashMode Algorithm;
	public readonly int Digits;
	public readonly int Period;
	public readonly int Counter;

	public OtpUri(
		OtpType schema,
		string secret,
		string user,
		string issuer = null,
		OtpHashMode algorithm = OtpHashMode.Sha1,
		int digits = 6,
		int period = 30,
		int counter = 0)
	{
		_ = secret ?? throw new ArgumentNullException(nameof(secret));
		_ = user ?? throw new ArgumentNullException(nameof(user));
		if (digits < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(digits));
		}

		Type = schema;
		Secret = secret;
		User = user;
		Issuer = issuer;
		Algorithm = algorithm;
		Digits = digits;

		switch (Type)
		{
			case OtpType.Totp:
				Period = period;
				break;
			case OtpType.Hotp:
				Counter = counter;
				break;
		}
	}

	public OtpUri(
		OtpType schema,
		byte[] secret,
		string user,
		string issuer = null,
		OtpHashMode algorithm = OtpHashMode.Sha1,
		int digits = 6,
		int period = 30,
		int counter = 0)
		: this(schema, Base32Encoding.ToString(secret), user, issuer, algorithm, digits, period, counter)
	{ }

	public Uri ToUri()
	{
		return new Uri(ToString());
	}

	public override string ToString()
	{
		Dictionary<string, string> parameters = new()
		{
			{ "secret", Secret.TrimEnd('=') }
		};

		if (!string.IsNullOrWhiteSpace(Issuer))
		{
			parameters.Add("issuer", Uri.EscapeDataString(Issuer));
		}

		parameters.Add("algorithm", Algorithm.ToString().ToUpper());
		parameters.Add("digits", Digits.ToString());

		switch (Type)
		{
			case OtpType.Totp:
				parameters.Add("period", Period.ToString());
				break;
			case OtpType.Hotp:
				parameters.Add("counter", Counter.ToString());
				break;
		}

		StringBuilder uriBuilder = new("otpauth://");
		_ = uriBuilder.Append(Type.ToString().ToLowerInvariant());

		if (!string.IsNullOrWhiteSpace(Issuer))
		{
			_ = uriBuilder.Append("/");
			_ = uriBuilder.Append(Uri.EscapeDataString(Issuer));
		}

		_ = uriBuilder.Append(":");
		_ = uriBuilder.Append(Uri.EscapeDataString(User));
		_ = uriBuilder.Append("?");

		foreach (KeyValuePair<string, string> pair in parameters)
		{
			_ = uriBuilder.Append(pair.Key);
			_ = uriBuilder.Append("=");
			_ = uriBuilder.Append(pair.Value);
			_ = uriBuilder.Append("&");
		}

		_ = uriBuilder.Remove(uriBuilder.Length - 1, 1);
		return uriBuilder.ToString();
	}
}