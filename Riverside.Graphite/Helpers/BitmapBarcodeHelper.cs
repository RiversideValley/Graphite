using Riverside.Graphite.Services.BarcodeHost;
using static Riverside.Graphite.Services.BarcodeHost.QRCodeGenerator;

namespace Riverside.Graphite.Helpers;

public static class BitmapBarcodeHelper
{
	public static byte[] GetQRCode(string plainText, int pixelsPerModule, string darkColorHtmlHex,
		string lightColorHtmlHex, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false,
		EciMode eciMode = EciMode.Default, int requestedVersion = -1)
	{
		using var qrGenerator = new QRCodeGenerator();
		using var qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode,
				requestedVersion);
		using var qrCode = new BitmapByteQRCode(qrCodeData);
		return qrCode.GetGraphic(pixelsPerModule, darkColorHtmlHex, lightColorHtmlHex);
	}

	public static byte[] GetQRCode(string txt, QRCodeGenerator.ECCLevel eccLevel, int size)
	{
		using var qrGen = new QRCodeGenerator();
		using var qrCode = qrGen.CreateQrCode(txt, eccLevel);
		using var qrBmp = new BitmapByteQRCode(qrCode);
		return qrBmp.GetGraphic(size);
	}
}