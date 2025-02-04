using System;
using System.Collections.Generic;
using System.Linq;

namespace Riverside.Graphite.Services.BarcodeHost;
public class BitmapByteQRCode : AbstractQRCode, IDisposable
{
	/// <summary>
	/// Constructor without params to be used in COM Objects connections
	/// </summary>
	public BitmapByteQRCode() { }

	public BitmapByteQRCode(QRCodeData data) : base(data) { }

	public byte[] GetGraphic(int pixelsPerModule)
	{
		return GetGraphic(pixelsPerModule, new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0xFF, 0xFF, 0xFF });
	}

	public byte[] GetGraphic(int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex)
	{
		return GetGraphic(pixelsPerModule, HexColorToByteArray(darkColorHtmlHex), HexColorToByteArray(lightColorHtmlHex));
	}

	public byte[] GetGraphic(int pixelsPerModule, byte[] darkColorRgb, byte[] lightColorRgb)
	{
		int sideLength = QrCodeData.ModuleMatrix.Count * pixelsPerModule;

		IEnumerable<byte> moduleDark = darkColorRgb.Reverse();
		IEnumerable<byte> moduleLight = lightColorRgb.Reverse();

		List<byte> bmp = new();

		//header
		bmp.AddRange(new byte[] { 0x42, 0x4D, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00 });

		//width
		bmp.AddRange(IntTo4Byte(sideLength));
		//height
		bmp.AddRange(IntTo4Byte(sideLength));

		//header end
		bmp.AddRange(new byte[] { 0x01, 0x00, 0x18, 0x00 });

		//draw qr code
		for (int x = sideLength - 1; x >= 0; x = x - pixelsPerModule)
		{
			for (int pm = 0; pm < pixelsPerModule; pm++)
			{
				for (int y = 0; y < sideLength; y = y + pixelsPerModule)
				{
					bool module =
						QrCodeData.ModuleMatrix[((x + pixelsPerModule) / pixelsPerModule) - 1][((y + pixelsPerModule) / pixelsPerModule) - 1];
					for (int i = 0; i < pixelsPerModule; i++)
					{
						bmp.AddRange(module ? moduleDark : moduleLight);
					}
				}
				if (sideLength % 4 != 0)
				{
					for (int i = 0; i < sideLength % 4; i++)
					{
						bmp.Add(0x00);
					}
				}
			}
		}

		//finalize with terminator
		bmp.AddRange(new byte[] { 0x00, 0x00 });

		return bmp.ToArray();
	}

	private byte[] HexColorToByteArray(string colorString)
	{
		if (colorString.StartsWith("#"))
		{
			colorString = colorString[1..];
		}

		byte[] byteColor = new byte[colorString.Length / 2];
		for (int i = 0; i < byteColor.Length; i++)
		{
			byteColor[i] = byte.Parse(colorString.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
		}

		return byteColor;
	}

	private byte[] IntTo4Byte(int inp)
	{
		byte[] bytes = new byte[2];
		unchecked
		{
			bytes[1] = (byte)(inp >> 8);
			bytes[0] = (byte)inp;
		}
		return bytes;
	}
}
