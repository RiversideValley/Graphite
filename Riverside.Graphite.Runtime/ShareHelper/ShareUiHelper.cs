using System;
using Windows.ApplicationModel.DataTransfer;

namespace Riverside.Graphite.Runtime.ShareHelper;
public class ShareUIHelper
{
	[System.Runtime.InteropServices.ComImport]
	[System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[System.Runtime.InteropServices.InterfaceType(
	System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
	interface IDataTransferManagerInterop
	{
		IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
			[System.Runtime.InteropServices.In] ref Guid riid);
		void ShowShareUIForWindow(IntPtr appWindow);
	}

	static readonly Guid _dtm_iid =
		new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

	public static void ShowShareUIURL(string title, string url, IntPtr hWnd)
	{
		IDataTransferManagerInterop interop = DataTransferManager.As<IDataTransferManagerInterop>();

		IntPtr result = interop.GetForWindow(hWnd, _dtm_iid);
		var dataTransferManager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);

		dataTransferManager.DataRequested += (sender, args) =>
		{
			args.Request.Data.Properties.Title = title;
			args.Request.Data.SetWebLink(new Uri(url));
		};

		interop.ShowShareUIForWindow(hWnd);
	}
}