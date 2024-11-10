using Fire.Browser.Core;
using FireBrowserWinUi3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Pages.SettingsPages
{
	public sealed partial class SettingsEnqryption : Page
	{
		private SettingsService SettingsService { get; }

		public SettingsEnqryption()
		{
			SettingsService = App.GetService<SettingsService>();
			InitializeComponent();
			LoadSettings();
		}

		private void LoadSettings()
		{
			EnqSets.IsOn = SettingsService.CoreSettings.Eqsets;
			Enq2fa.IsOn = SettingsService.CoreSettings.Eq2fa;
			EnqHis.IsOn = SettingsService.CoreSettings.EqHis;
			EnqFav.IsOn = SettingsService.CoreSettings.Eqfav;
		}

		private async void EnqSets_Toggled(object sender, RoutedEventArgs e) =>
			await UpdateSetting(nameof(SettingsService.CoreSettings.Eqsets), ((ToggleSwitch)sender).IsOn);

		private async void Enq2fa_Toggled(object sender, RoutedEventArgs e) =>
			await UpdateSetting(nameof(SettingsService.CoreSettings.Eq2fa), ((ToggleSwitch)sender).IsOn);

		private async void EnqHis_Toggled(object sender, RoutedEventArgs e) =>
			await UpdateSetting(nameof(SettingsService.CoreSettings.EqHis), ((ToggleSwitch)sender).IsOn);

		private async void EnqFav_Toggled(object sender, RoutedEventArgs e) =>
			await UpdateSetting(nameof(SettingsService.CoreSettings.Eqfav), ((ToggleSwitch)sender).IsOn);

		private async Task UpdateSetting(string propertyName, bool value)
		{
			var property = SettingsService.CoreSettings.GetType().GetProperty(propertyName);
			if (property != null)
			{
				property.SetValue(SettingsService.CoreSettings, value);
				await SettingsService.SaveChangesToSettings(AuthService.CurrentUser, SettingsService.CoreSettings);
			}
		}
	}
}