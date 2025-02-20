using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Graphite.UserSys;

namespace Graphite.Pages
{
	public sealed partial class TabSettings : Page
	{
		private User USR;

		public TabSettings()  // Remove the constructor parameter
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// Get the user object from navigation parameters
			if (e.Parameter is User user)
			{
				USR = user;
				LoadUserSettings(); // Optional: method to load user-specific settings
			}
			else
			{
				// Handle the case where no user is provided
				throw new ArgumentException("User parameter is required for TabSettings");
			}
		}

		private void LoadUserSettings()
		{
			// Initialize your UI elements with user settings here
			// For example:
			// MaxTabsSlider.Value = USR.TabSettings.MaxTabs;
			// AutoCloseSwitch.IsOn = USR.TabSettings.AutoCloseInactive;
		}
	}
}

