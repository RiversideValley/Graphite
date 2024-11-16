using Riverside.Graphite.Services.BarcodeHost;
using static Riverside.Graphite.Services.BarcodeHost.QRCodeGenerator;
using static Riverside.Graphite.Services.BarcodeHost.SvgQRCode;

namespace Riverside.Graphite.Helpers;

#if NET6_0_WINDOWS
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
public static class VectorBarcodeHelper
{
	public static string GetQRCode(string plainText, int pixelsPerModule, string darkColorHex, string lightColorHex, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false, EciMode eciMode = EciMode.Default, int requestedVersion = -1, bool drawQuietZones = true, SizingMode sizingMode = SizingMode.WidthHeightAttribute, SvgLogo logo = null)
	{
		using var qrGenerator = new QRCodeGenerator();
		using var qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
		using var qrCode = new SvgQRCode(qrCodeData);
		return qrCode.GetGraphic(pixelsPerModule, darkColorHex, lightColorHex, drawQuietZones, sizingMode, logo);
	}
}