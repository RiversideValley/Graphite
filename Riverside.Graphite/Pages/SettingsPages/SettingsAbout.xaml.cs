using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using static Riverside.Graphite.MainWindow;

namespace Riverside.Graphite.Pages.SettingsPages;

public sealed partial class SettingsAbout : Page
{
	private Passer? param;

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		param = e.Parameter as Passer;
	}

	public SettingsAbout()
	{
		InitializeComponent();
	}

	private void AboutCardClicked(object sender, RoutedEventArgs e)
	{
		if (sender is not SettingsCard card)
		{
			return;
		}

		string url = card.Tag switch
		{
			"Discord" => "https://discord.gg/windows-apps-hub-714581497222398064",
			"GitHub" => "https://github.com/FirebrowserDevs/Riverside.Graphite",
			"License" => "https://github.com/FirebrowserDevs/Riverside.Graphite/blob/main/License.lic",
			_ => "https://example.com"
		};

		MainWindow window = (Application.Current as App)?.m_window as MainWindow;
		window.NavigateToUrl(url);
	}
}