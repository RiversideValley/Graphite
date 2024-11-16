using Riverside.Graphite.Services.BarcodeHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Riverside.Graphite.Services.BarcodeHost.QRCodeGenerator;

namespace Riverside.Graphite.Helpers;

public static class PngByteQRCodeHelper
{
	public static byte[] GetQRCode(string plainText, int pixelsPerModule, byte[] darkColorRgba, byte[] lightColorRgba, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false, EciMode eciMode = EciMode.Default, int requestedVersion = -1, bool drawQuietZones = true)
	{
		using var qrGenerator = new QRCodeGenerator();
		using var qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
		using var qrCode = new PngByteQRCode(qrCodeData);
		return qrCode.GetGraphic(pixelsPerModule, darkColorRgba, lightColorRgba, drawQuietZones);
	}

	public static byte[] GetQRCode(string txt, ECCLevel eccLevel, int size, bool drawQuietZones = true)
	{
		using var qrGen = new QRCodeGenerator();
		using var qrCode = qrGen.CreateQrCode(txt, eccLevel);
		using var qrPng = new PngByteQRCode(qrCode);
		return qrPng.GetGraphic(size, drawQuietZones);
	}
}
