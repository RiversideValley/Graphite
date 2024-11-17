namespace Riverside.Graphite.Services.BarcodeHost;
public abstract class AbstractQRCode
{
	protected QRCodeData QrCodeData { get; set; }

	protected AbstractQRCode()
	{
	}

	protected AbstractQRCode(QRCodeData data)
	{
		QrCodeData = data;
	}

	public virtual void SetQRCodeData(QRCodeData data)
	{
		QrCodeData = data;
	}

	public void Dispose()
	{
		QrCodeData?.Dispose();
		QrCodeData = null;
	}
}