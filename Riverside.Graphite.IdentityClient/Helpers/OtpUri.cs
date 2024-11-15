using Riverside.Graphite.IdentityClient.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Graphite.IdentityClient.Helpers;

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
		ArgumentOutOfRangeException.ThrowIfNegative(digits);

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

	public Uri ToUri() => new Uri(ToString());

	public override string ToString()
	{
		var parameters = new Dictionary<string, string>
		{
			{ "secret", Secret.TrimEnd('=') }
		};

		if (!string.IsNullOrWhiteSpace(Issuer))
			parameters.Add("issuer", Uri.EscapeDataString(Issuer));

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

		var uriBuilder = new StringBuilder("otpauth://");
		uriBuilder.Append(Type.ToString().ToLowerInvariant());

		if (!string.IsNullOrWhiteSpace(Issuer))
		{
			uriBuilder.Append("/");
			uriBuilder.Append(Uri.EscapeDataString(Issuer));
		}

		uriBuilder.Append(":");
		uriBuilder.Append(Uri.EscapeDataString(User));
		uriBuilder.Append("?");

		foreach (var pair in parameters)
		{
			uriBuilder.Append(pair.Key);
			uriBuilder.Append("=");
			uriBuilder.Append(pair.Value);
			uriBuilder.Append("&");
		}

		uriBuilder.Remove(uriBuilder.Length - 1, 1);
		return uriBuilder.ToString();
	}
}