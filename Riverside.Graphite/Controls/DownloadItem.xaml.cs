using Riverside.Graphite.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Riverside.Graphite.Controls;

public sealed partial class DownloadItem : ListViewItem
{
	private readonly CoreWebView2DownloadOperation _downloadOperation;
	private readonly string _filePath;

	private int Progress { get; set; } = 0;
	private string EstimatedEnd { get; set; } = string.Empty;
	public DownloadService ServiceDownloads { get; set; }

	public DownloadItem(string filepath)
	{
		InitializeComponent();

		_filePath = filepath;
		fileName.Text = Path.GetFileName(_filePath);

		progressRing.Visibility = Visibility.Collapsed;
		ResourceLoader resourceLoader = new();
		subtitle.Text = resourceLoader.GetString("OpenDownload");

		SetIcon();
	}

	public DownloadItem(CoreWebView2DownloadOperation downloadOperation)
	{
		InitializeComponent();

		_downloadOperation = downloadOperation;
		_filePath = _downloadOperation.ResultFilePath;
		_downloadOperation.BytesReceivedChanged += _downloadOperation_BytesReceivedChanged;
		_downloadOperation.StateChanged += _downloadOperation_StateChanged;
		_downloadOperation.EstimatedEndTimeChanged += _downloadOperation_EstimatedEndTimeChanged;

		fileName.Text = Path.GetFileName(_filePath);

		SetIcon();
	}

	private void _downloadOperation_EstimatedEndTimeChanged(CoreWebView2DownloadOperation sender, object args)
	{
		EstimatedEnd = sender.EstimatedEndTime;
	}

	private void _downloadOperation_StateChanged(CoreWebView2DownloadOperation sender, object args)
	{
		switch (sender.State)
		{
			case CoreWebView2DownloadState.Completed:
				progressRing.Visibility = Visibility.Collapsed;
				ResourceLoader resourceLoader = new();
				subtitle.Text = resourceLoader.GetString("OpenDownload");
				SetIcon();
				break;

			case CoreWebView2DownloadState.Interrupted:
				// TODO: Handle interrupted state
				break;

			case CoreWebView2DownloadState.InProgress:
				progressRing.Visibility = Visibility.Visible;
				break;
		}
	}

	private void _downloadOperation_BytesReceivedChanged(CoreWebView2DownloadOperation sender, object args)
	{
		try
		{
			long progress = 100 * sender.BytesReceived / sender.TotalBytesToReceive;
			subtitle.Text = $"{(int)progress}%";
			progressRing.Value = (int)progress;

			if (progress >= 100)
			{
				progressRing.Visibility = Visibility.Collapsed;
				SetIcon();
				ResourceLoader resourceLoader = new();
				subtitle.Text = resourceLoader.GetString("OpenDownload");

				SimulateDownloadCompletion(sender);
			}
		}
		catch
		{
			// TODO: Handle exceptions
		}
	}

	private async void SimulateDownloadCompletion(CoreWebView2DownloadOperation downloadOperation)
	{
		await Task.Delay(1000);

		_ = (downloadOperation.GetType().GetMethod("OnDownloadStateChange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			?.Invoke(downloadOperation, new object[] { CoreWebView2DownloadState.Completed }));

		if (File.Exists(_filePath))
		{
			ServiceDownloads ??= App.GetService<DownloadService>();
			await ServiceDownloads.InsertAsync(_filePath, downloadOperation.EstimatedEndTime.ToString(), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		}
	}

	private void SetIcon()
	{
		try
		{
			Icon icon = GetSmallFileIcon(_filePath);

			if (icon != null)
			{
				using MemoryStream stream = new();
				icon.Save(stream);
				stream.Position = 0;

				BitmapImage bitmapImage = new();
				bitmapImage.SetSource(stream.AsRandomAccessStream());
				iconImage.Source = bitmapImage;
			}
		}
		catch (FileNotFoundException)
		{
			// TODO: Handle file not found
		}
	}

	private Icon GetSmallFileIcon(string filePath)
	{
		SHFILEINFO shinfo = new();
		IntPtr hImgSmall = Win32.SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON);

		if (hImgSmall != IntPtr.Zero)
		{
			Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
			_ = Win32.DestroyIcon(shinfo.hIcon);
			return icon;
		}

		return null;
	}

	private void ListViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		if (_downloadOperation?.State == CoreWebView2DownloadState.Completed || _downloadOperation == null)
		{
			FlyoutShowOptions flyoutShowOptions = new()
			{
				Position = e.GetPosition(this)
			};
			contextMenu.ShowAt(this, flyoutShowOptions);
		}
	}

	private static class Win32
	{
		public const uint SHGFI_ICON = 0x100;
		public const uint SHGFI_SMALLICON = 0x1;

		[DllImport("shell32.dll")]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct SHFILEINFO
	{
		public IntPtr hIcon;
		public int iIcon;
		public uint dwAttributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}
}