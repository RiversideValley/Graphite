using Riverside.Graphite.Services.BarcodeHost;
using static Riverside.Graphite.Services.BarcodeHost.QRCodeGenerator;

namespace Riverside.Graphite.Helpers;

public static class PngByteQRCodeHelper
{
	public static byte[] GetQRCode(string plainText, int pixelsPerModule, byte[] darkColorRgba, byte[] lightColorRgba, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false, EciMode eciMode = EciMode.Default, int requestedVersion = -1, bool drawQuietZones = true)
	{
		using QRCodeGenerator qrGenerator = new();
		using QRCodeData qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
		using PngByteQRCode qrCode = new(qrCodeData);
		return qrCode.GetGraphic(pixelsPerModule, darkColorRgba, lightColorRgba, drawQuietZones);
	}

	public static byte[] GetQRCode(string txt, ECCLevel eccLevel, int size, bool drawQuietZones = true)
	{
		using QRCodeGenerator qrGen = new();
		using QRCodeData qrCode = qrGen.CreateQrCode(txt, eccLevel);
		using PngByteQRCode qrPng = new(qrCode);
		return qrPng.GetGraphic(size, drawQuietZones);
	}
}
