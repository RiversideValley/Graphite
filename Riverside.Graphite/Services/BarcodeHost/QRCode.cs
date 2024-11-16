using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Riverside.Graphite.Services.BarcodeHost;

#if NET6_0_WINDOWS
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
public partial class QRCode : AbstractQRCode, IDisposable
{
	/// <summary>
	/// Constructor without params to be used in COM Objects connections
	/// </summary>
	public QRCode() { }

	public QRCode(QRCodeData data) : base(data) { }

	public Bitmap GetGraphic(int pixelsPerModule)
	{
		return GetGraphic(pixelsPerModule, Color.Black, Color.White, true);
	}

	public Bitmap GetGraphic(int pixelsPerModule, string darkColorHtmlHex, string lightColorHtmlHex, bool drawQuietZones = true)
	{
		return GetGraphic(pixelsPerModule, ColorTranslator.FromHtml(darkColorHtmlHex), ColorTranslator.FromHtml(lightColorHtmlHex), drawQuietZones);
	}

	public Bitmap GetGraphic(int pixelsPerModule, Color darkColor, Color lightColor, bool drawQuietZones = true)
	{
		int size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
		int offset = drawQuietZones ? 0 : 4 * pixelsPerModule;

		Bitmap bmp = new(size, size);
		using (Graphics gfx = Graphics.FromImage(bmp))
		using (SolidBrush lightBrush = new(lightColor))
		using (SolidBrush darkBrush = new(darkColor))
		{
			for (int x = 0; x < size + offset; x = x + pixelsPerModule)
			{
				for (int y = 0; y < size + offset; y = y + pixelsPerModule)
				{
					bool module = QrCodeData.ModuleMatrix[((y + pixelsPerModule) / pixelsPerModule) - 1][((x + pixelsPerModule) / pixelsPerModule) - 1];

					if (module)
					{
						gfx.FillRectangle(darkBrush, new Rectangle(x - offset, y - offset, pixelsPerModule, pixelsPerModule));
					}
					else
					{
						gfx.FillRectangle(lightBrush, new Rectangle(x - offset, y - offset, pixelsPerModule, pixelsPerModule));
					}
				}
			}

			_ = gfx.Save();
		}

		return bmp;
	}

	public Bitmap GetGraphic(int pixelsPerModule, Color darkColor, Color lightColor, Bitmap icon = null, int iconSizePercent = 15, int iconBorderWidth = 0, bool drawQuietZones = true, Color? iconBackgroundColor = null)
	{
		int size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
		int offset = drawQuietZones ? 0 : 4 * pixelsPerModule;

		Bitmap bmp = new(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

		using (Graphics gfx = Graphics.FromImage(bmp))
		using (SolidBrush lightBrush = new(lightColor))
		using (SolidBrush darkBrush = new(darkColor))
		{
			gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
			gfx.CompositingQuality = CompositingQuality.HighQuality;
			gfx.Clear(lightColor);
			bool drawIconFlag = icon != null && iconSizePercent > 0 && iconSizePercent <= 100;

			for (int x = 0; x < size + offset; x = x + pixelsPerModule)
			{
				for (int y = 0; y < size + offset; y = y + pixelsPerModule)
				{
					SolidBrush moduleBrush = QrCodeData.ModuleMatrix[((y + pixelsPerModule) / pixelsPerModule) - 1][((x + pixelsPerModule) / pixelsPerModule) - 1] ? darkBrush : lightBrush;
					gfx.FillRectangle(moduleBrush, new Rectangle(x - offset, y - offset, pixelsPerModule, pixelsPerModule));
				}
			}

			if (drawIconFlag)
			{
				float iconDestWidth = iconSizePercent * bmp.Width / 100f;
				float iconDestHeight = drawIconFlag ? iconDestWidth * icon.Height / icon.Width : 0;
				float iconX = (bmp.Width - iconDestWidth) / 2;
				float iconY = (bmp.Height - iconDestHeight) / 2;
				RectangleF centerDest = new(iconX - iconBorderWidth, iconY - iconBorderWidth, iconDestWidth + (iconBorderWidth * 2), iconDestHeight + (iconBorderWidth * 2));
				RectangleF iconDestRect = new(iconX, iconY, iconDestWidth, iconDestHeight);
				SolidBrush iconBgBrush = iconBackgroundColor != null ? new SolidBrush((Color)iconBackgroundColor) : lightBrush;
				//Only render icon/logo background, if iconBorderWith is set > 0
				if (iconBorderWidth > 0)
				{
					using GraphicsPath iconPath = CreateRoundedRectanglePath(centerDest, iconBorderWidth * 2);
					gfx.FillPath(iconBgBrush, iconPath);
				}
				gfx.DrawImage(icon, iconDestRect, new RectangleF(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
			}

			_ = gfx.Save();
		}

		return bmp;
	}

	internal GraphicsPath CreateRoundedRectanglePath(RectangleF rect, int cornerRadius)
	{
		GraphicsPath roundedRect = new();
		roundedRect.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
		roundedRect.AddLine(rect.X + cornerRadius, rect.Y, rect.Right - (cornerRadius * 2), rect.Y);
		roundedRect.AddArc(rect.X + rect.Width - (cornerRadius * 2), rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
		roundedRect.AddLine(rect.Right, rect.Y + (cornerRadius * 2), rect.Right, rect.Y + rect.Height - (cornerRadius * 2));
		roundedRect.AddArc(rect.X + rect.Width - (cornerRadius * 2), rect.Y + rect.Height - (cornerRadius * 2), cornerRadius * 2, cornerRadius * 2, 0, 90);
		roundedRect.AddLine(rect.Right - (cornerRadius * 2), rect.Bottom, rect.X + (cornerRadius * 2), rect.Bottom);
		roundedRect.AddArc(rect.X, rect.Bottom - (cornerRadius * 2), cornerRadius * 2, cornerRadius * 2, 90, 90);
		roundedRect.AddLine(rect.X, rect.Bottom - (cornerRadius * 2), rect.X, rect.Y + (cornerRadius * 2));
		roundedRect.CloseFigure();
		return roundedRect;
	}
}
