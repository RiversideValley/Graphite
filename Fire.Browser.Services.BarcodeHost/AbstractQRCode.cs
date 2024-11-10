namespace FireBrowserWinUi3QrCore;
public abstract class AbstractQRCode
{
    protected QRCodeData QrCodeData { get; set; }

    protected AbstractQRCode()
    {
    }

    protected AbstractQRCode(QRCodeData data)
    {
        this.QrCodeData = data;
    }

    virtual public void SetQRCodeData(QRCodeData data)
    {
        this.QrCodeData = data;
    }

    public void Dispose()
    {
        this.QrCodeData?.Dispose();
        this.QrCodeData = null;
    }
}