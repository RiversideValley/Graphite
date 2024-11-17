using Riverside.Graphite.Services.BarcodeHost;
using System.Drawing;
using static Riverside.Graphite.Services.BarcodeHost.QRCodeGenerator;

namespace Riverside.Graphite.Helpers;

#if NET6_0_WINDOWS
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
public static class BarcodeHelper
{
	public static Bitmap GetQRCode(string plainText, int pixelsPerModule, Color darkColor, Color lightColor, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false, EciMode eciMode = EciMode.Default, int requestedVersion = -1, Bitmap icon = null, int iconSizePercent = 15, int iconBorderWidth = 0, bool drawQuietZones = true)
	{
		using QRCodeGenerator qrGenerator = new();
		using QRCodeData qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
		using QRCode qrCode = new(qrCodeData);
		return qrCode.GetGraphic(pixelsPerModule, darkColor, lightColor, icon, iconSizePercent, iconBorderWidth, drawQuietZones);
	}
}