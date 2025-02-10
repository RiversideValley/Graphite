using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using WinRT.Interop;

namespace Graphite.Migration
{
	public sealed partial class ReceiverWindow : Window
	{
		private StreamSocketListener listener;
		private StreamSocket connectedSocket;
		private GraphiteTransferProtocol protocol;
		private GraphiteDataTransfer dataTransfer;
		private AppWindow m_AppWindow;

		public ReceiverWindow()
		{
			this.InitializeComponent();
			StartListening();
			dataTransfer = new GraphiteDataTransfer();
			dataTransfer.ProgressChanged += DataTransfer_ProgressChanged;
			InitializeWindow();
		}

		private void InitializeWindow()
		{
			m_AppWindow = GetAppWindowForCurrentWindow();

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
			StatusTextBlock.Text = "Waiting for sender...";

			try
			{
				listener = new StreamSocketListener();
				listener.ConnectionReceived += Listener_ConnectionReceived;

				// Use port 8080 directly
				await listener.BindServiceNameAsync("8080");

				DispatcherQueue.TryEnqueue(() =>
				{
					StatusTextBlock.Text = "Listening for senders on port 8080...";
				});
			}
			catch (Exception ex)
			{
				DispatcherQueue.TryEnqueue(() =>
				{
					StatusTextBlock.Text = $"Error: {ex.Message}";
					LoadingProgressRing.IsActive = false;
					LoadingProgressRing.Visibility = Visibility.Collapsed;
				});
			}
		}


		private async Task<int> FindAvailablePortAsync()
		{
			for (int port = 8080; port <= 8888; port++)
			{
				try
				{
					var tempListener = new StreamSocketListener();
					await tempListener.BindServiceNameAsync(port.ToString());
					tempListener.Dispose();
					return port;
				}
				catch
				{
					// Port is not available, continue to the next one
				}
			}
			throw new Exception("No available ports found between 8080 and 8888.");
		}

		private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
		{
			connectedSocket = args.Socket;
			protocol = new GraphiteTransferProtocol(connectedSocket);

			DispatcherQueue.TryEnqueue(() =>
			{
				LoadingProgressRing.IsActive = false;
				LoadingProgressRing.Visibility = Visibility.Collapsed;

				StatusIcon.Glyph = "\uE73E"; // Checkmark
				StatusIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
				StatusTextBlock.Text = "Connected to sender. Enter verification code and click 'Verify & Receive Data'.";

				VerificationCodeTextBox.IsEnabled = true;
				VerifyButton.IsEnabled = true;
			});
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

			LoadingProgressRing.IsActive = true;
			StatusTextBlock.Text = "Verifying code and receiving data...";
			VerifyButton.IsEnabled = false;

			try
			{
				await protocol.SendMessageAsync(verificationCode);

				string verificationResult = await protocol.ReceiveMessageAsync();

				if (verificationResult == "Verification successful")
				{
					StatusTextBlock.Text = "Verification successful. Receiving data...";

					await ReceiveAndRestoreData();
				}
				else
				{
					StatusTextBlock.Text = "Verification failed. Please check the code and try again.";
				}
			}
			catch (Exception ex)
			{
				StatusTextBlock.Text = $"Error: {ex.Message}";
			}
			finally
			{
				LoadingProgressRing.IsActive = false;
				VerifyButton.IsEnabled = true;
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
	}
}

