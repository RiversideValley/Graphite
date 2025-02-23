using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;

namespace Graphite.Setup.OOBE
{
	public sealed partial class OOBEFinish : Page
	{
		private DispatcherTimer _countdownTimer;
		private int _remainingSeconds = 5;

		public OOBEFinish()
		{
			this.InitializeComponent();
			StartCountdown();
		}


		private void StartCountdown()
		{
			_countdownTimer = new DispatcherTimer();
			_countdownTimer.Tick += CountdownTimer_Tick;
			_countdownTimer.Interval = TimeSpan.FromSeconds(1);
			_countdownTimer.Start();
		}

		private void CountdownTimer_Tick(object sender, object e)
		{
			_remainingSeconds--;
			UpdateCountdown();

			if (_remainingSeconds <= 0)
			{
				_countdownTimer.Stop();
				RestartAppAsync();
			}
		}

		private void UpdateCountdown()
		{
			CountdownText.Text = $"Restarting in {_remainingSeconds} seconds...";
			CountdownProgressBar.Value = _remainingSeconds;
		}

		public async Task RestartAppAsync()
		{
			Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
		}


		private async void ShowErrorDialog(string message)
		{
			ContentDialog errorDialog = new ContentDialog
			{
				Title = "Error",
				Content = message,
				CloseButtonText = "OK",
				XamlRoot = this.XamlRoot
			};

			await errorDialog.ShowAsync();
		}
	}
}