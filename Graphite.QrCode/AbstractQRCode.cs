using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.QrCode;

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
