using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Graphite.Pages.SettingPages
{
	public sealed partial class AccessibilitySettings : Page
	{
		public AccessibilitySettings()
		{
			this.InitializeComponent();
			// Initialize controls here (e.g., populate ComboBoxes)
		}

		

		private void LiteMode_Toggled(object sender, RoutedEventArgs e)
		{
			// Handle Lite Mode toggle
		}

		private void WelcomeMesg_Toggled(object sender, RoutedEventArgs e)
		{
			// Handle Welcome Message toggle
		}

		private void ConfirmDialog_Toggled(object sender, RoutedEventArgs e)
		{
			// Handle Confirm Dialog toggle
		}

		private void Langue_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Handle Language selection change
		}

		private void Gender_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Handle Gender selection change
		}

		
	}
}