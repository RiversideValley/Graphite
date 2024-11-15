using Microsoft.UI.Xaml.Controls;


namespace Riverside.Graphite.Runtime.CoreUi;

public sealed partial class PermissionDialog : ContentDialog
{
	public string DialogTitle { get; set; }
	public string ManageText { get; set; }

	public PermissionDialog(string title, string manageText)
	{
		this.InitializeComponent();
		DialogTitle = title;
		ManageText = manageText;
	}
}
