using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Networking.Sockets;

namespace Graphite.Migration
{
    public static class ErrorHandler
    {
        public static async Task<bool> HandleExceptionAsync(Exception ex, string operation, XamlRoot xamlRoot)
        {
            string title = "Error";
            string message = "An unexpected error occurred.";

            if (ex is IOException ioEx)
            {
                if (ioEx.HResult == -2147014836) // 0x8007274C - WSAEADDRINUSE
                {
                    message = "A network port is already in use. Please wait a moment and try again.";
                }
                else
                {
                    message = $"Network error: {ioEx.Message}";
                }
            }
            else if (ex is FileNotFoundException fileEx)
            {
                message = $"Required file not found: {fileEx.Message}";
            }
            else if (ex is COMException comEx)
            {
                message = $"System component error: {comEx.Message}";
            }
            else if (ex is SocketException sockEx)
            {
                switch (sockEx.SocketErrorCode)
                {
                    case System.Net.Sockets.SocketError.AddressAlreadyInUse:
                        message = "Network address is already in use. Please wait a moment and try again.";
                        break;
                    case System.Net.Sockets.SocketError.ConnectionReset:
                        message = "The connection was reset by the remote device.";
                        break;
                    default:
                        message = $"Network error: {sockEx.Message}";
                        break;
                }
            }

            try
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = title,
                    Content = $"Error during {operation}: {message}",
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };

                await dialog.ShowAsync();
                return false;
            }
            catch
            {
                // If we can't show the dialog, at least write to debug
                System.Diagnostics.Debug.WriteLine($"Error during {operation}: {message}");
                return false;
            }
        }
    }
}

