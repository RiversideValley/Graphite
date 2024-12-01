using System;
using System.Text;

namespace Riverside.Graphite.IdentityClient.Helpers
{
	public static class Base32Encoding
	{
		private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

		public static byte[] ToBytes(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				throw new ArgumentException("Input cannot be null or empty.", nameof(input));
			}

			input = input.Trim().Replace(" ", "").ToUpperInvariant();

			if (input.Length == 0)
			{
				return Array.Empty<byte>();
			}

			int paddingCount = input.Length % 8 == 0 ? 0 : 8 - (input.Length % 8);
			input = input.PadRight(input.Length + paddingCount, '=');

			int byteCount = input.Length * 5 / 8;
			byte[] returnArray = new byte[byteCount];

			int curByte = 0, bitsRemaining = 8;
			int arrayIndex = 0;

			foreach (char c in input)
			{
				int cValue = CharToValue(c);
				if (bitsRemaining > 5)
				{
					curByte = (curByte << 5) | cValue;
					bitsRemaining -= 5;
				}
				else
				{
					int bitsToAdd = 5 - bitsRemaining;
					curByte = (curByte << bitsRemaining) | (cValue >> bitsToAdd);
					returnArray[arrayIndex++] = (byte)curByte;
					curByte = cValue & ((1 << bitsToAdd) - 1);
					bitsRemaining = 8 - bitsToAdd;
				}
			}

			if (arrayIndex != byteCount)
			{
				returnArray[arrayIndex] = (byte)(curByte << bitsRemaining);
			}

			return returnArray;
		}

		private static int CharToValue(char c)
		{
			if (c == '=')
			{
				return 0;
			}

			int value = Base32Alphabet.IndexOf(c);
			if (value == -1)
			{
				throw new ArgumentException($"Character '{c}' is not a valid Base32 character.", nameof(c));
			}

			return value;
		}

		public static string ToString(byte[] input)
		{
			if (input == null || input.Length == 0)
			{
				return string.Empty;
			}

			StringBuilder result = new StringBuilder();
			int bitsLeft = 0;
			int currentByte = 0;

			foreach (byte b in input)
			{
				currentByte = (currentByte << 8) | b;
				bitsLeft += 8;
				while (bitsLeft >= 5)
				{
					bitsLeft -= 5;
					result.Append(Base32Alphabet[(currentByte >> bitsLeft) & 31]);
				}
			}

			if (bitsLeft > 0)
			{
				result.Append(Base32Alphabet[(currentByte << (5 - bitsLeft)) & 31]);
			}

			int paddingCount = result.Length % 8 == 0 ? 0 : 8 - (result.Length % 8);
			result.Append('=', paddingCount);

			return result.ToString();
		}
	}
}

