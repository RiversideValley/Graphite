using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static FireBrowserWinUi3QrCore.Base64QRCode;
using static FireBrowserWinUi3QrCore.QRCodeGenerator;

namespace FireBrowserWinUi3QrCore;

public class Base64QRCode : AbstractQRCode, IDisposable
{
    private readonly QRCode _qr;

    public Base64QRCode()
    {
        _qr = new QRCode();
    }

    public Base64QRCode(QRCodeData data) : base(data)
    {
        _qr = new QRCode(data);
    }

    public override void SetQRCodeData(QRCodeData data)
    {
        _qr.SetQRCodeData(data);
    }

    public string GetGraphic(int pixelsPerModule)
        => GetGraphic(pixelsPerModule, Color.Black, Color.White, true);

    public string GetGraphic(int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex, bool drawQuietZones = true, ImageType imgType = ImageType.Png)
        => GetGraphic(pixelsPerModule, ColorTranslator.FromHtml(darkColorHtmlHex), ColorTranslator.FromHtml(lightColorHtmlHex), drawQuietZones, imgType);

    public string GetGraphic(int pixelsPerModule, Color darkColor, Color lightColor, bool drawQuietZones = true, ImageType imgType = ImageType.Png)
    {
        using Bitmap bmp = _qr.GetGraphic(pixelsPerModule, darkColor, lightColor, drawQuietZones);
        return BitmapToBase64(bmp, imgType);
    }

    public string GetGraphic(int pixelsPerModule, Color darkColor, Color lightColor, Bitmap icon, int iconSizePercent = 15, int iconBorderWidth = 6, bool drawQuietZones = true, ImageType imgType = ImageType.Png)
    {
        using Bitmap bmp = _qr.GetGraphic(pixelsPerModule, darkColor, lightColor, icon, iconSizePercent, iconBorderWidth, drawQuietZones);
        return BitmapToBase64(bmp, imgType);
    }

    private string BitmapToBase64(Bitmap bmp, ImageType imgType)
    {
        var iFormat = imgType switch
        {
            ImageType.Jpeg => ImageFormat.Jpeg,
            ImageType.Gif => ImageFormat.Gif,
            _ => ImageFormat.Png
        };

        using MemoryStream memoryStream = new();
        bmp.Save(memoryStream, iFormat);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public enum ImageType
    {
        Gif,
        Jpeg,
        Png
    }
}

public static class Base64QRCodeHelper
{
    public static string GetQRCode(string plainText, int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex, ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false, EciMode eciMode = EciMode.Default, int requestedVersion = -1, bool drawQuietZones = true, ImageType imgType = ImageType.Png)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(plainText, eccLevel, forceUtf8, utf8BOM, eciMode, requestedVersion);
        using var qrCode = new Base64QRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule, darkColorHtmlHex, lightColorHtmlHex, drawQuietZones, imgType);
    }
}
