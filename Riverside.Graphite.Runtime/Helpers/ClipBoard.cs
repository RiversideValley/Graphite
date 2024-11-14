using Windows.ApplicationModel.DataTransfer;

namespace Riverside.Graphite.Runtime.Helpers;
public class ClipBoard
{
	public static void WriteStringToClipboard(string text)
	{
		text ??= string.Empty; // Simplified null check using the null-coalescing assignment

		var dataPackage = new DataPackage()
		{
			RequestedOperation = DataPackageOperation.Copy
		};
		dataPackage.SetText(text);
		Clipboard.SetContent(dataPackage);
	}
}