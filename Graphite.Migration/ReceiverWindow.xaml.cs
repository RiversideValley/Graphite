using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using Windows.Graphics;
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Collections.Generic;

namespace Graphite.Migration
{
	public sealed partial class ReceiverWindow : Window
	{
		private StreamSocketListener listener;
		private StreamSocket connectedSocket;
		private GraphiteTransferProtocol protocol;
		private GraphiteDataTransfer dataTransfer;
		private AppWindow m_AppWindow;
		private CancellationTokenSource _discoveryServiceCts;
		private UdpClient _udpClient;
		private const int DiscoveryPort = 8080;
		private const string BroadcastMessage = "GRAPHITE_DISCOVERY";
		private const string ResponseMessage = "GRAPHITE_RECEIVER";

		public ReceiverWindow()
		{
			this.InitializeComponent();
			InitializeWindow();
			StartDiscoveryService();
			StartListening();
			dataTransfer = new GraphiteDataTransfer();
			dataTransfer.ProgressChanged += DataTransfer_ProgressChanged;
		}

		private void StartDiscoveryService()
		{
			_discoveryServiceCts = new CancellationTokenSource();
			_ = Task.Run(() => StartReceiverDiscoveryServiceAsync(_discoveryServiceCts.Token));
			UpdateStatus("Broadcasting presence and listening for senders...", StatusType.Info);
		}

		private async Task StartReceiverDiscoveryServiceAsync(CancellationToken cancellationToken)
		{
			_udpClient = new UdpClient(DiscoveryPort);
			_udpClient.EnableBroadcast = true;

			var deviceName = Environment.MachineName;
			var ipAddresses = GetLocalIPAddresses();

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var result = await _udpClient.ReceiveAsync(cancellationToken);
					var message = Encoding.UTF8.GetString(result.Buffer);

					if (message == BroadcastMessage)
					{
						foreach (var ip in ipAddresses)
						{
							var responseData = $"{ResponseMessage},{ip},{deviceName}";
							var responseBytes = Encoding.UTF8.GetBytes(responseData);
							await _udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
						}
					}
				}
				catch (OperationCanceledException)
				{
					// Normal cancellation
				}
				catch (Exception ex)
				{
					await UpdateStatusAsync($"Error in discovery service: {ex.Message}", StatusType.Error);
					await Task.Delay(1000, cancellationToken);
				}
			}
		}

		private List<string> GetLocalIPAddresses()
		{
			var ipAddresses = new List<string>();
			foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus == OperationalStatus.Up)
				{
					foreach (var unicastAddress in nic.GetIPProperties().UnicastAddresses)
					{
						if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							ipAddresses.Add(unicastAddress.Address.ToString());
						}
					}
				}
			}
			return ipAddresses;
		}

		private void StopDiscoveryService()
		{
			_discoveryServiceCts?.Cancel();
			_discoveryServiceCts = null;
			_udpClient?.Close();
			_udpClient = null;
			UpdateStatus("Discovery service stopped.", StatusType.Info);
		}

		private void InitializeWindow()
		{
			m_AppWindow = GetAppWindowForCurrentWindow();

			if (AppWindowTitleBar.IsCustomizationSupported())
			{
				var titleBar = m_AppWindow.TitleBar;
				titleBar.ExtendsContentIntoTitleBar = true;
				titleBar.ButtonBackgroundColor = Colors.Transparent;
				titleBar.ButtonForegroundColor = Colors.Black;
				titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
				titleBar.ButtonInactiveForegroundColor = Colors.Gray;
				AppTitleBar.Loaded += AppTitleBar_Loaded;
				AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
				m_AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(100, 100, 700, 1100));
			}
			else
			{
				AppTitleBar.Visibility = Visibility.Collapsed;
			}
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
						(int)(AppTitleBar.ActualWidth),
						(int)(AppTitleBar.ActualHeight)
					)
				};
				appWindow.TitleBar.SetDragRectangles(dragRects);
			}
		}

		private async void StartListening()
		{
			LoadingProgressRing.IsActive = true;
			LoadingProgressRing.Visibility = Visibility.Visible;
			StatusTextBlock.Text = "Creating firewall rules...";

			try
			{
				await FirewallManager.EnsureFirewallRulesExistAsync();
				StatusTextBlock.Text = "Firewall rules created. Waiting for sender...";

				listener = new StreamSocketListener();
				listener.ConnectionReceived += Listener_ConnectionReceived;

				await listener.BindServiceNameAsync(DiscoveryPort.ToString());

				await UpdateStatusAsync("Listening for senders...", StatusType.Info);
			}
			catch (Exception ex)
			{
				await UpdateStatusAsync($"Error: {ex.Message}", StatusType.Error);
				LoadingProgressRing.IsActive = false;
				LoadingProgressRing.Visibility = Visibility.Collapsed;
			}
		}

		private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
		{
			connectedSocket = args.Socket;
			protocol = new GraphiteTransferProtocol(connectedSocket);

			await UpdateStatusAsync("Connected to sender. Waiting for verification code...", StatusType.Success);
			LoadingProgressRing.IsActive = false;
			LoadingProgressRing.Visibility = Visibility.Collapsed;

			try
			{
				string receivedCode = await protocol.ReceiveMessageAsync();
				 DispatcherQueue.TryEnqueue(() =>
				{
					VerificationCodeTextBox.Text = receivedCode;
					VerifyButton.IsEnabled = true;
				});
			}
			catch (Exception ex)
			{
				await UpdateStatusAsync($"Error receiving verification code: {ex.Message}", StatusType.Error);
			}
		}

		private async void VerifyButton_Click(object sender, RoutedEventArgs e)
		{
			string verificationCode = VerificationCodeTextBox.Text;
			if (string.IsNullOrWhiteSpace(verificationCode) || verificationCode.Length != 8)
			{
				StatusTextBlock.Text = "Please enter a valid 8-digit verification code.";
				return;
			}

			if (connectedSocket == null || protocol == null)
			{
				StatusTextBlock.Text = "No sender connected.";
				return;
			}

			try
			{
				await protocol.SendMessageAsync("VERIFY_AND_TRANSFER");
				UpdateStatus("Verification sent. Starting data transfer...", StatusType.Progress);
				await ReceiveAndRestoreData();
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"Error: {ex.Message}";
			}
		}

		private async Task ReceiveAndRestoreData()
		{
			try
			{
				StatusTextBlock.Text = "Receiving data...";
				ProgressBar.Visibility = Visibility.Visible;
				ProgressTextBlock.Visibility = Visibility.Visible;

				await dataTransfer.ReceiveDataAsync(protocol);

				StatusTextBlock.Text = "Graphite data restored successfully.";
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"Error restoring data: {ex.Message}";
			}
			finally
			{
				ProgressBar.Visibility = Visibility.Collapsed;
				ProgressTextBlock.Visibility = Visibility.Collapsed;
				await DisconnectAsync();
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

			if (connectedSocket != null)
			{
				connectedSocket.Dispose();
				connectedSocket = null;
			}

			 DispatcherQueue.TryEnqueue(() =>
			{
				VerifyButton.IsEnabled = false;
				VerificationCodeTextBox.Text = string.Empty;
			});

			UpdateStatus("Disconnected. Waiting for new connection.", StatusType.Info);
			StartListening();
		}

		private async Task UpdateStatusAsync(string message, StatusType type)
		{
			 DispatcherQueue.TryEnqueue(() => UpdateStatus(message, type));
		}

		private void UpdateStatus(string message, StatusType type)
		{
			StatusTextBlock.Text = message;

			switch (type)
			{
				case StatusType.Success:
					StatusIcon.Glyph = "\uE73E"; // Checkmark
					StatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Green);
					break;
				case StatusType.Error:
					StatusIcon.Glyph = "\uE783"; // Error
					StatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red);
					break;
				case StatusType.Warning:
					StatusIcon.Glyph = "\uE7BA"; // Warning
					StatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Orange);
					break;
				case StatusType.Progress:
					StatusIcon.Glyph = "\uE895"; // Sync
					StatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Blue);
					break;
				case StatusType.Info:
				default:
					StatusIcon.Glyph = "\uE946"; // Info
					StatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Gray);
					break;
			}
		}

	}


}

