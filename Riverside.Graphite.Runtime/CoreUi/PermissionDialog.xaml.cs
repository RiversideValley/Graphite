using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Riverside.Graphite.Runtime.CoreUi
{
	public sealed partial class PermissionDialog : ContentDialog
	{
		public string DialogTitle { get; set; }
		public string ManageText { get; set; }

		public event TypedEventHandler<PermissionDialog, object> AllowClicked;
		public event TypedEventHandler<PermissionDialog, object> DenyClicked;
		public event TypedEventHandler<PermissionDialog, object> CancelClicked;

		public PermissionDialog(string title, string manageText)
		{
			InitializeComponent();
			DialogTitle = title;
			ManageText = manageText;

			this.PrimaryButtonClick += (sender, args) => AllowClicked?.Invoke(this, args);
			this.SecondaryButtonClick += (sender, args) => DenyClicked?.Invoke(this, args);
			this.CloseButtonClick += (sender, args) => CancelClicked?.Invoke(this, args);
		}
	}
}

