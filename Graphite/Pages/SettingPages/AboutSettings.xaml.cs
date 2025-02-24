using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using CommunityToolkit.WinUI.Controls;
using Windows.Storage;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Graphite.Pages.SettingPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutSettings : Page
    {
        public AboutSettings()
        {
            this.InitializeComponent();
			SetAppVersion();

		}

		private void SetAppVersion()
		{
			Package package = Package.Current;
			PackageId packageId = package.Id;
			PackageVersion version = packageId.Version;

			AppVer.Text = $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
		}

		private async void AboutCardClicked(object sender, RoutedEventArgs e)
		{
			if (sender is not SettingsCard card)
			{
				return;
			}

			string url = card.Tag switch
			{
				"Discord" => "https://discord.gg/windows-apps-hub-714581497222398064",
				"GitHub" => "https://github.com/RiversideValley/Graphite",
				"License" => "https://github.com/RiversideValley/Graphite?tab=GPL-3.0-1-ov-file",
				_ => "https://example.com"
			};

			Launcher.LaunchUriAsync(new Uri(url));
		}
	}
}
