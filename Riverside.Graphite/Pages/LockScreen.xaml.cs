using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Riverside.Graphite.Services.ViewModels;

namespace Riverside.Graphite.Pages
{
	public sealed partial class LockScreen : Page
	{
		public LockScreenViewModel ViewModel { get; } = new LockScreenViewModel();

		public LockScreen()
		{
			InitializeComponent();
			ViewModel.LoadUserData();
			DataContext = ViewModel;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			((Application.Current as App)?.m_window as MainWindow)?.GoFullScreenLock(false);
		}
	}
}
