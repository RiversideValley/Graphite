using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Riverside.Graphite.Runtime.Helpers;
public static class EncryptionHelpers
{
	// Your additional entropy
	private static readonly byte[] AdditionalEntropy = { 9, 6, 4, 1, 5, 7, 8, 2 };

	public static byte[] ProtectString(string str)
	{
		try
		{
			string callingAppName = GetCallingAppName();
			return IsAllowedApp(callingAppName)
				? ProtectedData.Protect(Encoding.UTF8.GetBytes(str), AdditionalEntropy, DataProtectionScope.CurrentUser)
				: throw new UnauthorizedAccessException($"Access denied for the calling application '{callingAppName}'.");
		}
		catch (CryptographicException)
		{
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}



	private static string GetCallingAppName()
	{
		return Process.GetCurrentProcess().ProcessName;
	}

	private static bool IsAllowedApp(string appName)
	{
		return appName is (nameof(Riverside.Graphite)) or "FireVault" or "Protecc";
	}

	public static string UnprotectToString(byte[] data)
	{
		try
		{
			string callingAppName = GetCallingAppName();
			return IsAllowedApp(callingAppName)
				? Encoding.UTF8.GetString(ProtectedData.Unprotect(data, AdditionalEntropy, DataProtectionScope.CurrentUser))
				: throw new UnauthorizedAccessException($"Access denied for the calling application '{callingAppName}'.");
		}
		catch (CryptographicException)
		{
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}
}