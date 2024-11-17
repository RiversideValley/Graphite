using Riverside.Graphite.Services.BarcodeHost;
using static Riverside.Graphite.Services.BarcodeHost.QRCodeGenerator;

namespace Riverside.Graphite.Helpers;

public static class BitmapBarcodeHelper
{
	public static byte[] GetQRCode(string plainText, int pixelsPerModule, string darkColorHtmlHex,
		string lightColorHtmlHex, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false,
		EciMode eciMode = EciMode.Default, int requestedVersion = -1)
	{
		using QRCodeGenerator qrGenerator = new();
		using QRCodeData qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode,
				requestedVersion);
		using BitmapByteQRCode qrCode = new(qrCodeData);
		return qrCode.GetGraphic(pixelsPerModule, darkColorHtmlHex, lightColorHtmlHex);
	}

	public static byte[] GetQRCode(string txt, QRCodeGenerator.ECCLevel eccLevel, int size)
	{
		using QRCodeGenerator qrGen = new();
		using QRCodeData qrCode = qrGen.CreateQrCode(txt, eccLevel);
		using BitmapByteQRCode qrBmp = new(qrCode);
		return qrBmp.GetGraphic(size);
	}
}