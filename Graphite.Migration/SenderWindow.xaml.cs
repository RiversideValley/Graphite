using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.UI;
using System;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using System.Linq;
using Microsoft.UI;

namespace Graphite.Migration
{
	public sealed partial class SenderWindow : Window
	{
		private AppWindow m_AppWindow;
		private string verificationCode;
		private StreamSocket socket;
		private GraphiteTransferProtocol protocol;
		private GraphiteDataTransfer dataTransfer;
		private bool isConnected = false;

		public SenderWindow()
		{
			this.InitializeComponent();
			InitializeWindow();
			dataTransfer = new GraphiteDataTransfer();
			dataTransfer.ProgressChanged += DataTransfer_ProgressChanged;
		}

		private void InitializeWindow()
		{
			m_AppWindow = GetAppWindowForCurrentWindow();

			// Set the window size to 350x1000
			Windows.Graphics.PointInt32 position = m_AppWindow.Position;
			m_AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(position.X, position.Y, 700, 1100));

			if (AppWindowTitleBar.IsCustomizationSupported())
			{
				var titleBar = m_AppWindow.TitleBar;
				titleBar.ExtendsContentIntoTitleBar = true;
				titleBar.ButtonBackgroundColor = Colors.Transparent;
				titleBar.ButtonForegroundColor = Colors.Transparent;
				titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
				titleBar.ButtonInactiveForegroundColor = Colors.Transparent;
				AppTitleBar.Loaded += AppTitleBar_Loaded;
				AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
			}
			else
			{
				AppTitleBar.Visibility = Visibility.Collapsed;
			}

			UpdateStatus("Ready to begin", StatusType.Info);
		}

		private AppWindow GetAppWindowForCurrentWindow()
		{
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			return AppWindow.GetFromWindowId(wndId);
		}

		private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
		{
			if (AppWindowTitleBar.IsCustomizationSupported())
				SetDragRegionForCustomTitleBar(m_AppWindow);
		}

		private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (AppWindowTitleBar.IsCustomizationSupported())
				SetDragRegionForCustomTitleBar(m_AppWindow);
		}

		private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
		{
			if (AppTitleBar != null)
			{
				Windows.Graphics.RectInt32[] dragRects = new Windows.Graphics.RectInt32[] {
					new Windows.Graphics.RectInt32(
						0,
						0,
						(int)(AppTitleBar.ActualWidth ),
						(int)(AppTitleBar.ActualHeight )
					)
				};
				appWindow.TitleBar.SetDragRectangles(dragRects);
			}
		}

	

		private void CreateCodeButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				verificationCode = GenerateVerificationCode();
				VerificationCodeTextBox.Text = verificationCode;
				ConnectButton.IsEnabled = true;
				UpdateStatus("Verification code created. Click 'Connect to Receiver' to start.", StatusType.Success);
			}
			catch (Exception ex)
			{
				UpdateStatus($"Error creating verification code: {ex.Message}", StatusType.Error);
			}
		}

		private async void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(verificationCode))
			{
				UpdateStatus("Please create a verification code first.", StatusType.Warning);
				return;
			}

			if (isConnected)
			{
				await DisconnectAsync();
				return;
			}

			await ConnectToReceiverAsync();
		}

		private async Task ConnectToReceiverAsync()
		{
			UpdateStatus("Connecting to receiver...", StatusType.Progress);
			DisableControls();

			try
			{
				socket = new StreamSocket();

				var icp = NetworkInformation.GetInternetConnectionProfile();
				var hostNames = NetworkInformation.GetHostNames();
				var localHostName = hostNames.FirstOrDefault(hn =>
					hn.IPInformation?.NetworkAdapter != null &&
					hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId);

				if (localHostName == null)
				{
					throw new Exception("Unable to find a suitable local IP address.");
				}

				// Use port 8080 directly instead of scanning
				await socket.ConnectAsync(localHostName, "8080");
				protocol = new GraphiteTransferProtocol(socket);

				UpdateStatus("Connected to receiver. Waiting for verification...", StatusType.Progress);
				isConnected = true;
				UpdateConnectButtonState();

				await HandleVerificationAndTransfer();
			}
			catch (Exception ex)
			{
				UpdateStatus($"Connection error: {ex.Message}", StatusType.Error);
				await DisconnectAsync();
			}
			finally
			{
				EnableControls();
			}
		}


		private async Task<int> FindAvailablePortAsync()
		{
			for (int port = 8080; port <= 8888; port++)
			{
				try
				{
					var listener = new StreamSocketListener();
					await listener.BindServiceNameAsync(port.ToString());
					listener.Dispose();
					return port;
				}
				catch
				{
					// Port is not available, continue to the next one
				}
			}
			throw new Exception("No available ports found between 8080 and 8888.");
		}

		private async Task HandleVerificationAndTransfer()
		{
			try
			{
				string receivedCode = await protocol.ReceiveMessageAsync();

				if (receivedCode == verificationCode)
				{
					await protocol.SendMessageAsync("Verification successful");
					StatusTextBlock.Text = "Verification successful. Collecting data...";

					await dataTransfer.CollectDataAsync();
					StatusTextBlock.Text = "Data collected. Sending data...";

					await TransferData();
				}
				else
				{
					await protocol.SendMessageAsync("Verification failed");
					StatusTextBlock.Text = "Verification failed. Please try again.";
				}
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"Error during verification: {ex.Message}";
			}
		}

		private async Task TransferData()
		{
			try
			{
				StatusTextBlock.Text = "Sending Graphite data...";
				ProgressBar.Visibility = Visibility.Visible;
				ProgressTextBlock.Visibility = Visibility.Visible;

				await dataTransfer.SendDataAsync(protocol);

				StatusTextBlock.Text = "Data sent successfully.";
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"Error sending data: {ex.Message}";
			}
			finally
			{
				ProgressBar.Visibility = Visibility.Collapsed;
				ProgressTextBlock.Visibility = Visibility.Collapsed;
			}
		}

		private void DataTransfer_ProgressChanged(double percentage)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				ProgressBar.Value = percentage;
				ProgressTextBlock.Text = $"{percentage:F1}%";
			});
		}


		private async Task DisconnectAsync()
		{
			if (protocol != null)
			{
				protocol.Dispose();
				protocol = null;
			}

			if (socket != null)
			{
				socket.Dispose();
				socket = null;
			}

			isConnected = false;
			UpdateConnectButtonState();
		}

		private string GenerateVerificationCode()
		{
			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
			{
				byte[] randomBytes = new byte[4];
				rng.GetBytes(randomBytes);
				uint randomNumber = BitConverter.ToUInt32(randomBytes, 0) % 100000000;
				return randomNumber.ToString("D8");
			}
		}

		private void UpdateStatus(string message, StatusType type)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				StatusTextBlock.Text = message;

				switch (type)
				{
					case StatusType.Success:
						StatusIcon.Glyph = "\uE73E"; // Checkmark
						StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
						break;
					case StatusType.Error:
						StatusIcon.Glyph = "\uE783"; // Error
						StatusIcon.Foreground = new SolidColorBrush(Colors.Red);
						break;
					case StatusType.Warning:
						StatusIcon.Glyph = "\uE7BA"; // Warning
						StatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
						break;
					case StatusType.Progress:
						StatusIcon.Glyph = "\uE895"; // Sync
						StatusIcon.Foreground = new SolidColorBrush(Colors.Blue);
						break;
					case StatusType.Info:
					default:
						StatusIcon.Glyph = "\uE946"; // Info
						StatusIcon.Foreground = new SolidColorBrush(Colors.Gray);
						break;
				}
			});
		}

		private void UpdateConnectButtonState()
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				ConnectButton.Content = isConnected ? "Disconnect" : "Connect to Receiver";
				ConnectButton.Style = isConnected ?
					Application.Current.Resources["AccentButtonStyle"] as Style :
					Application.Current.Resources["DefaultButtonStyle"] as Style;
			});
		}

		private void DisableControls()
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				CreateCodeButton.IsEnabled = false;
				ConnectButton.IsEnabled = false;
			});
		}

		private void EnableControls()
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				CreateCodeButton.IsEnabled = !isConnected;
				ConnectButton.IsEnabled = true;
			});
		}
	}

	public enum StatusType
	{
		Info,
		Success,
		Error,
		Warning,
		Progress
	}
}

