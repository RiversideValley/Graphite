using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;
using System;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;

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
		private CancellationTokenSource _discoveryCts;
		private ObservableCollection<ReceiverInfo> _discoveredReceivers;
		private const int DiscoveryPort = 8080;
		private const string BroadcastMessage = "GRAPHITE_DISCOVERY";
		private const string ResponsePrefix = "GRAPHITE_RECEIVER";

		public SenderWindow()
		{
			this.InitializeComponent();
			InitializeWindow();
			dataTransfer = new GraphiteDataTransfer();
			dataTransfer.ProgressChanged += DataTransfer_ProgressChanged;
			_discoveredReceivers = new ObservableCollection<ReceiverInfo>();
			ReceiverListView.ItemsSource = _discoveredReceivers;
			StartAutomaticDiscovery();
		}

		private void InitializeWindow()
		{
			m_AppWindow = GetAppWindowForCurrentWindow();

			// Set the window size
			Windows.Graphics.PointInt32 position = m_AppWindow.Position;
			m_AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(position.X, position.Y, 350, 1000));

			if (AppWindowTitleBar.IsCustomizationSupported())
			{
				var titleBar = m_AppWindow.TitleBar;
				titleBar.ExtendsContentIntoTitleBar = true;
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
						(int)(AppTitleBar.ActualWidth),
						(int)(AppTitleBar.ActualHeight)
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
				VerificationCodeTextBlock.Text = verificationCode;
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
			if (ReceiverListView.SelectedItem is ReceiverInfo selectedReceiver)
			{
				await ConnectToReceiverAsync(selectedReceiver);
			}
			else
			{
				UpdateStatus("Please select a receiver.", StatusType.Warning);
			}
		}

		private async Task ConnectToReceiverAsync(ReceiverInfo receiver)
		{
			try
			{
				UpdateStatus($"Connecting to {receiver.DeviceName} ({receiver.IP})...", StatusType.Progress);

				socket = new StreamSocket();
				await socket.ConnectAsync(new HostName(receiver.IP), DiscoveryPort.ToString());
				protocol = new GraphiteTransferProtocol(socket);

				UpdateStatus("Connected to receiver. Sending verification code...", StatusType.Progress);
				isConnected = true;
				UpdateConnectButtonState();

				// Stop discovery process
				_discoveryCts?.Cancel();

				await HandleVerificationAndTransfer();
			}
			catch (Exception ex)
			{
				UpdateStatus($"Connection error: {ex.Message}", StatusType.Error);
				await DisconnectAsync();
			}
		}


		private async Task HandleVerificationAndTransfer()
		{
			try
			{
				await protocol.SendMessageAsync(verificationCode);
				UpdateStatus("Verification code sent. Waiting for receiver to verify and start transfer...", StatusType.Info);

				string response = await protocol.ReceiveMessageAsync();

				if (response == "VERIFY_AND_TRANSFER")
				{
					UpdateStatus("Receiver verified. Starting data transfer...", StatusType.Success);
					await TransferData();
				}
				else
				{
					UpdateStatus("Verification failed or cancelled by receiver. Please try again.", StatusType.Error);
					await DisconnectAsync();
				}
			}
			catch (Exception ex)
			{
				UpdateStatus($"Error during verification: {ex.Message}", StatusType.Error);
				await DisconnectAsync();
			}
		}

		private async Task TransferData()
		{
			try
			{
				StatusTextBlock.Text = "Collecting Graphite data...";
				await dataTransfer.CollectDataAsync();

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

			UpdateStatus("Disconnected. Firewall rules remain in place for future use.", StatusType.Info);
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

		private async Task ShowErrorDialogAsync(string title, string message)
		{
			ContentDialog dialog = new ContentDialog
			{
				Title = title,
				Content = message,
				PrimaryButtonText = "Continue",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = Content.XamlRoot
			};

			var result = await dialog.ShowAsync();
			if (result != ContentDialogResult.Primary)
			{
				throw new OperationCanceledException("User chose not to continue after firewall rule creation failure.");
			}
		}

		private void StartDiscoveryButton_Click(object sender, RoutedEventArgs e)
		{
			StartAutomaticDiscovery();
		}


		private void StartAutomaticDiscovery()
		{
			if (!isConnected)
			{
				_discoveryCts?.Cancel();
				_discoveryCts = new CancellationTokenSource();
				_discoveredReceivers.Clear();
				UpdateStatus("Starting automatic discovery...", StatusType.Progress);
				_ = DiscoverReceiversAsync(_discoveryCts.Token);
			}
			else
			{
				UpdateStatus("Already connected to a receiver.", StatusType.Info);
			}
		}

		private async Task DiscoverReceiversAsync(CancellationToken cancellationToken)
		{
			using (var udpClient = new UdpClient())
			{
				udpClient.EnableBroadcast = true;
				var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
				var message = Encoding.UTF8.GetBytes(BroadcastMessage);

				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						await udpClient.SendAsync(message, message.Length, broadcastEndpoint);

						while (udpClient.Available > 0)
						{
							var result = await udpClient.ReceiveAsync(cancellationToken);
							var response = Encoding.UTF8.GetString(result.Buffer);

							if (response.StartsWith(ResponsePrefix))
							{
								var parts = response.Split(',');
								if (parts.Length == 3)
								{
									var ip = parts[1];
									var deviceName = parts[2];
									await AddOrUpdateReceiverAsync(ip, deviceName);
								}
							}
						}
					}
					catch (OperationCanceledException)
					{
						// Normal cancellation, break the loop
						break;
					}
					catch (Exception ex)
					{
						await UpdateStatusAsync($"Discovery error: {ex.Message}", StatusType.Error);
					}

					await Task.Delay(5000, cancellationToken); // Wait 5 seconds before next broadcast
				}
			}
		}

		private async Task AddOrUpdateReceiverAsync(string ip, string deviceName)
		{
			 DispatcherQueue.TryEnqueue(() =>
			{
				var existingReceiver = _discoveredReceivers.FirstOrDefault(r => r.IP == ip);
				if (existingReceiver != null)
				{
					existingReceiver.LastSeen = DateTime.Now;
				}
				else
				{
					_discoveredReceivers.Add(new ReceiverInfo { IP = ip, DeviceName = deviceName, LastSeen = DateTime.Now });
				}

				// Remove receivers that haven't been seen in the last 30 seconds
				var outdatedReceivers = _discoveredReceivers.Where(r => (DateTime.Now - r.LastSeen).TotalSeconds > 30).ToList();
				foreach (var receiver in outdatedReceivers)
				{
					_discoveredReceivers.Remove(receiver);
				}

				UpdateStatus($"Found {_discoveredReceivers.Count} receiver(s)", StatusType.Info);
			});
		}

		private void EnableDataTransferControls()
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				StartTransferButton.IsEnabled = true;
			});
		}

		private async void StartTransferButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateStatus("Collecting data...", StatusType.Progress);
			await dataTransfer.CollectDataAsync();
			UpdateStatus("Data collected. Starting transfer...", StatusType.Progress);
			await TransferData();
		}
	}

	public class ReceiverInfo
	{
		public string IP { get; set; }
		public string DeviceName { get; set; }
		public DateTime LastSeen { get; set; }
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

