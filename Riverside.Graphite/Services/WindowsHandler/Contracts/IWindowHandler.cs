using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Riverside.Graphite.Services.WindowsHandler;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Riverside.Graphite.Services.WindowsHandler.Contracts;

public interface IWindowHandler
{
	Window MainWindow { get; }
	nint Hwnd { get; }
	AppWindow AppWindow { get; }
	TitleBar TitleBar { get; }
	void Initialize(Window window);
	void SetWindowSize(int width, int height);
	void SetWindowPosition(int x, int y);
	void CenterOnScreen();
	void Maximize();
	void Minimize();
	void Restore();
	void SetWindowStyle(WindowStyle style);
	void SetWindowBackdrop(BackdropType backdropType);
	void SetIcon(string iconPath);
	void SetTitle(string title);
	Task<StorageFile> PickSaveFileAsync(string suggestedFileName = null);
	Task<StorageFile> PickOpenFileAsync(params string[] fileTypes);
	void EnableDragToMove();
	void SaveWindowPosition();
	void RestoreWindowPosition();
	void Cleanup();
}
