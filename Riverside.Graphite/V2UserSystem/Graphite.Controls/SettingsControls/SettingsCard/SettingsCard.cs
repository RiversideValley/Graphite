using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Graphite.Controls.SettingsControls
{
	[ContentProperty(Name = nameof(Content))]
	public partial class SettingsCard : Control
	{
		public SettingsCard()
		{
			this.DefaultStyleKey = typeof(SettingsCard);
		}

		public Visibility HeaderVisibility =>
			string.IsNullOrEmpty(Header) && HeaderIcon == null ?
			Visibility.Collapsed :
			Visibility.Visible;
	}
}

