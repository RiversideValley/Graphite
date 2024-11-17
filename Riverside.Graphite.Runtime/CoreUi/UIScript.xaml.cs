using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Riverside.Graphite.Runtime.CoreUi;

public sealed partial class UIScript : ContentDialog
{
	public UIScript(string title, string content, XamlRoot root)
	{
		InitializeComponent();
		Title = title;
		XamlRoot = root;
		Content = content;
		PrimaryButtonText = "Okay";
		DefaultButton = ContentDialogButton.Primary;
	}
}